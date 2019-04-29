using UnityEngine;
using SanAndreasUnity.Utilities;
using SanAndreasUnity.Behaviours.Vehicles;
using SanAndreasUnity.Importing.Animation;

namespace SanAndreasUnity.Behaviours.Peds.States
{

	public class VehicleExitingState : BaseVehicleState
	{
		Coroutine m_coroutine;
		

		public override void OnBecameInactive()
		{
			this.Cleanup();

			if (m_coroutine != null)
				StopCoroutine(m_coroutine);
			m_coroutine = null;

			base.OnBecameInactive();
		}

		public void ExitVehicle(bool immediate = false)
		{
			if (!m_ped.IsInVehicle || !m_ped.IsInVehicleSeat)
				return;
			
			// obtain current vehicle from Ped
			this.CurrentVehicle = m_ped.CurrentVehicle;
			this.CurrentVehicleSeat = m_ped.CurrentVehicleSeat;

			// after obtaining parameters, switch to this state
			m_ped.SwitchState<VehicleExitingState> ();

			VehicleEnteringState.PreparePedForVehicle(m_ped, this.CurrentVehicle, this.CurrentVehicleSeat);

			if (this.CurrentVehicleSeat.IsDriver)
				this.CurrentVehicle.StopControlling();
			
			StartCoroutine (ExitVehicleAnimation (immediate));

		}

		private System.Collections.IEnumerator ExitVehicleAnimation(bool immediate)
		{

			var seat = this.CurrentVehicleSeat;

			var animIndex = seat.IsLeftHand ? AnimIndex.GetOutLeft : AnimIndex.GetOutRight;

			m_model.VehicleParentOffset = Vector3.Scale(m_model.GetAnim(AnimGroup.Car, animIndex).RootStart, new Vector3(-1, -1, -1));

			if (!immediate)
			{
				var animState = m_model.PlayAnim(AnimGroup.Car, animIndex, PlayMode.StopAll);
				animState.wrapMode = WrapMode.Once;

				// wait until anim finishes or stops
				while (animState.enabled)
					yield return new WaitForEndOfFrame();
			}

			// ped now completely exited the vehicle

			m_model.IsInVehicle = false;

			this.CurrentVehicle = null;
			this.CurrentVehicleSeat = null;
			seat.OccupyingPed = null;

			m_ped.transform.localPosition = m_model.VehicleParentOffset;
			m_ped.transform.localRotation = Quaternion.identity;

			m_ped.transform.SetParent(null);

			m_ped.characterController.enabled = true;

			m_model.VehicleParentOffset = Vector3.zero;

			// switch to stand state
			m_ped.SwitchState<StandState> ();

		}

	}

}
