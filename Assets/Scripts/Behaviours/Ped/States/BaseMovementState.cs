using UnityEngine;
using SanAndreasUnity.Utilities;
using SanAndreasUnity.Importing.Animation;

namespace SanAndreasUnity.Behaviours.Peds.States
{

	/// <summary>
	/// Base class for all movement states.
	/// </summary>
	public abstract class BaseMovementState : BaseScriptState
	{
		public abstract AnimId movementAnim { get; }
		public abstract AnimId movementWeaponAnim { get; }



		public override void UpdateState() {

			base.UpdateState ();

			if (!this.IsActiveState)
				return;


			if (m_isServer)
				this.SwitchToMovementState ();

			if (!this.IsActiveState)
				return;

			if (m_isServer)
				this.SwitchToAimState ();

		}

		protected virtual void SwitchToMovementState()
		{
			BaseMovementState.SwitchToMovementStateBasedOnInput (m_ped);
		}

		public static void SwitchToMovementStateBasedOnInput (Ped ped)
		{

			if (ped.IsJumpOn && ped.GetStateOrLogError<JumpState>().CanJump())
			{
				ped.GetState<JumpState>().Jump();
			}
			else if (ped.IsWalkOn)
			{
				ped.SwitchState<WalkState> ();
			}
			else if (ped.IsRunOn)
			{
				ped.SwitchState<RunState> ();
			}
			else if (ped.IsSprintOn)
			{
				if (ped.CurrentWeapon != null && !ped.CurrentWeapon.CanSprintWithIt)
					ped.SwitchState<RunState> ();
				else
					ped.SwitchState<SprintState> ();
			}
			else
			{
				ped.SwitchState<StandState> ();
			}

		}

		protected virtual void SwitchToAimState()
		{
			if (m_ped.IsAimOn && m_ped.IsHoldingWeapon)
			{
				BaseAimMovementState.SwitchToAimMovementStateBasedOnInput (m_ped);
			}
		}

		protected override void UpdateAnims ()
		{
			if (m_ped.CurrentWeapon != null)
			{
				m_ped.PlayerModel.PlayAnim (this.movementWeaponAnim);
			}
			else
			{
				m_ped.PlayerModel.PlayAnim (this.movementAnim);
			}
		}

		public override void OnSubmitPressed() {

			// try to enter vehicle
			if (m_isServer)
				m_ped.TryEnterVehicleInRange ();
			else
				base.OnSubmitPressed();

		}

		public override void OnCrouchButtonPressed ()
		{
			if (m_isServer)
				m_ped.SwitchState<CrouchState>();
			else
				base.OnCrouchButtonPressed();
		}

		public override void OnFlyButtonPressed ()
		{
			if (m_isServer)
				m_ped.GetStateOrLogError<FlyState> ().EnterState (false);
		}

		public override void OnFlyThroughButtonPressed ()
		{
			if (m_isServer)
				m_ped.GetStateOrLogError<FlyState> ().EnterState (true);
		}

	}

}
