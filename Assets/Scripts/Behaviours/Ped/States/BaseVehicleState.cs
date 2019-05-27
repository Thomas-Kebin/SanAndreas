using UnityEngine;
using SanAndreasUnity.Utilities;
using SanAndreasUnity.Behaviours.Vehicles;

namespace SanAndreasUnity.Behaviours.Peds.States
{

	public class BaseVehicleState : BaseScriptState, IVehicleState
	{
		private Vehicle m_currentVehicle;
		public Vehicle CurrentVehicle { get { return m_currentVehicle; } protected set { m_currentVehicle = value; } }

		public Vehicle.Seat CurrentVehicleSeat { get; protected set; }
		public Vehicle.SeatAlignment CurrentVehicleSeatAlignment { get { return this.CurrentVehicleSeat.Alignment; } }


		public override void OnSwitchedStateByServer(byte[] data)
		{
			// we need to wait for end of frame, because vehicle may not be spawned yet (which can happen if client
			// just connected to server)
			this.StartCoroutine(this.SwitchStateAtEndOfFrame(data));
		}

		protected void ReadNetworkData(byte[] data)
		{
			// extract vehicle and seat from data
			var reader = new Mirror.NetworkReader(data);
			GameObject vehicleGo = reader.ReadGameObject();
			Vehicle.SeatAlignment seatAlignment = (Vehicle.SeatAlignment) reader.ReadSByte();

			// assign params
			this.CurrentVehicle = vehicleGo != null ? vehicleGo.GetComponent<Vehicle>() : null;
			this.CurrentVehicleSeat = this.CurrentVehicle != null ? this.CurrentVehicle.GetSeat(seatAlignment) : null;

		}

		public override byte[] GetAdditionalNetworkData()
		{
			var writer = new Mirror.NetworkWriter();
			if (this.CurrentVehicle != null) {
				writer.Write(this.CurrentVehicle.gameObject);
				writer.Write((sbyte)this.CurrentVehicleSeatAlignment);
			} else {
				writer.Write((GameObject)null);
				writer.Write((sbyte)Vehicle.SeatAlignment.None);
			}
			
			return writer.ToArray();
		}

		System.Collections.IEnumerator SwitchStateAtEndOfFrame(byte[] data)
		{
			var oldState = m_ped.CurrentState;

			yield return new WaitForEndOfFrame();

			if (oldState != m_ped.CurrentState)
			{
				// state changed in the meantime
				// did server change it ? or syncvar hooks invoked twice ? either way, we should stop here

				// Debug.LogFormat("state changed in the meantime, old: {0}, new: {1}", oldState != null ? oldState.GetType().Name : "",
				// 	m_ped.CurrentState != null ? m_ped.CurrentState.GetType().Name : "");
				yield break;
			}

			// read current vehicle here - it should've been spawned by now
			this.ReadNetworkData(data);

		//	Debug.LogFormat("Switching to state {0}, vehicle: {1}, seat: {2}", this.GetType().Name, this.CurrentVehicle, this.CurrentVehicleSeat);

			// now we can enter this state
			m_ped.SwitchState(this.GetType());
		}

		protected void Cleanup()
		{
			if (!m_ped.IsInVehicle)
			{
				m_ped.characterController.enabled = true;
				m_ped.transform.SetParent(null, true);
				m_model.IsInVehicle = false;
				// enable network transform
				if (m_ped.NetTransform != null)
					m_ped.NetTransform.enabled = true;
			}

			if (this.CurrentVehicleSeat != null && this.CurrentVehicleSeat.OccupyingPed == m_ped)
				this.CurrentVehicleSeat.OccupyingPed = null;

			this.CurrentVehicle = null;
			this.CurrentVehicleSeat = null;
		}

		protected override void UpdateHeading()
		{
			
		}

		protected override void UpdateRotation()
		{
			
		}

		protected override void UpdateMovement()
		{
			
		}

		protected override void ConstrainRotation ()
		{
			
		}

		public bool CanEnterVehicle (Vehicle vehicle, Vehicle.SeatAlignment seatAlignment)
		{
			if (m_ped.IsInVehicle)
				return false;

			// this should be removed
			if (m_ped.IsAiming || m_ped.WeaponHolder.IsFiring)
				return false;

			var seat = vehicle.GetSeat (seatAlignment);
			if (null == seat)
				return false;

			// check if specified seat is taken
			if (seat.IsTaken)
				return false;

			// everything is ok, we can enter vehicle

			return true;
		}

	}

}
