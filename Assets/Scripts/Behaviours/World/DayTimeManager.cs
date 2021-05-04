﻿using SanAndreasUnity.Utilities;
using UnityEngine;

namespace SanAndreasUnity.Behaviours.World
{
    public class DayTimeManager : MonoBehaviour
    {
        public static DayTimeManager Singleton { get; private set; }

        public AnimationCurve lightAngleCurve;
        public AnimationCurve lightIntensityCurve;
        public AnimationCurve nightColorsIntensityCurve;

        public Light directionalLight;

        public byte startTimeHours = 12;
        public byte startTimeMinutes = 0;

        public byte CurrentTimeHours { get; private set; }
        public byte CurrentTimeMinutes { get; private set; }

        public float CurrentCurveTime => (this.CurrentTimeHours + this.CurrentTimeMinutes / 60f) / 24f;

        private float m_timeSinceTimeAdvanced = 0;

        public float timeScale = 1;

        public float nightColorsMultiplier = 0.1f;

        private float m_originalSkyboxExposure;

        public bool controlLightIntensity = true;
        public bool disableLightDuringNight = true;

        public Color moonColor = Color.blue;

        private Color m_originalLightColor;

        private static int s_exposurePropertyId = -1;
        public static int ExposurePropertyId => s_exposurePropertyId == -1 ? s_exposurePropertyId = Shader.PropertyToID("_Exposure") : s_exposurePropertyId;

        private static int s_nightMultiplierPropertyId = -1;
        public static int NightMultiplierPropertyId => s_nightMultiplierPropertyId == -1 ? s_nightMultiplierPropertyId = Shader.PropertyToID("_NightMultiplier") : s_nightMultiplierPropertyId;

        public event System.Action onTimeChanged = delegate {};
        public event System.Action onHourChanged = delegate {};


        private void Awake()
        {
            Singleton = this;

            m_originalLightColor = this.directionalLight.color;
            m_originalSkyboxExposure = RenderSettings.skybox.GetFloat(ExposurePropertyId);
        }

        private void OnDisable()
        {
            // restore material settings
            if (Application.isEditor)
                RenderSettings.skybox.SetFloat(ExposurePropertyId, m_originalSkyboxExposure);
        }

        private void Start()
        {
            if (NetUtils.IsServer)
                this.SetTime(this.startTimeHours, this.startTimeMinutes, false);
        }

        private void Update()
        {
            m_timeSinceTimeAdvanced += Time.deltaTime * this.timeScale;

            if (m_timeSinceTimeAdvanced >= 1 && NetUtils.IsServer)
            {
                m_timeSinceTimeAdvanced = 0;
                this.AdvanceTime();
            }
            else
            {
                // light angle should be updated every frame
                this.UpdateLightAngle(this.CurrentCurveTime + m_timeSinceTimeAdvanced / 60f / 24f);
            }
        }

        void AdvanceTime()
        {
            int newHours = this.CurrentTimeHours;
            int newMinutes = this.CurrentTimeMinutes + 1;
            if (newMinutes >= 60)
            {
                newMinutes = 0;

                newHours++;
                if (newHours >= 24)
                    newHours = 0;
            }

            this.SetTime((byte) newHours, (byte) newMinutes, false);
        }

        public void SetTime(byte hours, byte minutes, bool log)
        {
            hours = (byte) Mathf.Clamp(hours, 0, 23);
            minutes = (byte) Mathf.Clamp(minutes, 0, 59);

            byte oldHour = this.CurrentTimeHours;

            this.CurrentTimeHours = hours;
            this.CurrentTimeMinutes = minutes;

            m_timeSinceTimeAdvanced = 0;

            float curveTime = this.CurrentCurveTime;

            float lightIntensity = this.lightIntensityCurve.Evaluate(curveTime);
            bool isNight = lightIntensity <= 0;
            if (this.controlLightIntensity)
                this.directionalLight.intensity = Mathf.Abs(lightIntensity);
            if (this.disableLightDuringNight)
                this.directionalLight.enabled = !isNight;
            else
            {
                this.directionalLight.enabled = true;
                this.directionalLight.color = isNight ? this.moonColor : m_originalLightColor;
            }

            float skyboxExposure = isNight ? 0f : m_originalSkyboxExposure * lightIntensity;
            RenderSettings.skybox.SetFloat(ExposurePropertyId, skyboxExposure);

            float lightAngle = this.UpdateLightAngle(curveTime);

            float nightMultiplier = this.nightColorsIntensityCurve.Evaluate(curveTime) * this.nightColorsMultiplier;
            Shader.SetGlobalFloat(NightMultiplierPropertyId, nightMultiplier);

            if (log)
            {
                Debug.Log($"Time set to {hours}:{minutes}, curveTime {curveTime}, lightIntensity {lightIntensity}, lightAngle {lightAngle}, nightMultiplier {nightMultiplier}");
            }

            F.InvokeEventExceptionSafe(this.onTimeChanged);
            if (oldHour != this.CurrentTimeHours)
                F.InvokeEventExceptionSafe(this.onHourChanged);
        }

        float UpdateLightAngle(float curveTime)
        {
            float lightAngle = this.lightAngleCurve.Evaluate(curveTime) * 180f;
            this.directionalLight.transform.rotation = Quaternion.AngleAxis(lightAngle, Vector3.right);
            return lightAngle;
        }

        public static void CurveTimeToHoursAndMinutes(float curveTime, out byte hours, out byte minutes)
        {
            float hoursWithMinutes = curveTime * 24f;
            hours = (byte) Mathf.FloorToInt(hoursWithMinutes);
            float hourPerc = hoursWithMinutes - Mathf.Floor(hoursWithMinutes);
            minutes = (byte) Mathf.RoundToInt(60 * hourPerc);
        }
    }
}