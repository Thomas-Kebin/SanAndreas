﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SanAndreasUnity.Behaviours.Vehicles;
using SanAndreasUnity.Behaviours.World;
using SanAndreasUnity.Importing.Animation;
using System.Linq;
using SanAndreasUnity.Behaviours.Peds.States;
using SanAndreasUnity.Net;

namespace SanAndreasUnity.Behaviours
{
	
	public partial class Ped {

		[SerializeField] private float m_enterVehicleRadius = 2.0f;
		public float EnterVehicleRadius { get { return m_enterVehicleRadius; } set { m_enterVehicleRadius = value; } }

		public Vehicle CurrentVehicle {
			get {
				if (this.CurrentState != null && this.CurrentState is IVehicleState)
				{
					return ((IVehicleState)this.CurrentState).CurrentVehicle;
				}
				return null;
			}
		}

		public Vehicle.Seat CurrentVehicleSeat {
			get {
				if (this.CurrentState != null && this.CurrentState is IVehicleState)
				{
					return ((IVehicleState)this.CurrentState).CurrentVehicleSeat;
				}
				return null;
			}
		}

		public bool IsInVehicle { get { return this.CurrentVehicle != null; } }

		public bool IsInVehicleSeat { get { return this.CurrentState != null && this.CurrentState.RepresentsState (typeof(VehicleSittingState)); } }

		public bool IsDrivingVehicle { get { var seat = this.CurrentVehicleSeat; return seat != null && seat.IsDriver; } }

		public Vehicle.SeatAlignment CurrentVehicleSeatAlignment { get { return this.CurrentVehicleSeat.Alignment; } }



		public void EnterVehicle(Vehicle vehicle, Vehicle.SeatAlignment seatAlignment, bool immediate = false)
		{
			NetStatus.ThrowIfNotOnServer();
			// find state script, and call it's method
			this.GetStateOrLogError<VehicleEnteringState>().TryEnterVehicle( vehicle, seatAlignment, immediate );
		}

		public void ExitVehicle(bool immediate = false)
		{
			NetStatus.ThrowIfNotOnServer();
			this.GetStateOrLogError<VehicleExitingState> ().ExitVehicle (immediate);
		}


		public static List<Vehicle.SeatAlignment> GetFreeSeats( Vehicle vehicle )
		{
			return vehicle.Seats.Where (s => !s.IsTaken).Select (s => s.Alignment).ToList ();

//			var freeSeats = new List<Vehicle.SeatAlignment> (vehicle.Seats.Select (s => s.Alignment));
//
//			var players = FindObjectsOfType<Player> ();
//
//			foreach (var p in players) {
//				if (p.IsInVehicle && p.CurrentVehicle == vehicle) {
//					freeSeats.Remove (p.CurrentVehicleSeatAlignment);
//				}
//			}
//
//			return freeSeats;
		}

		private void UpdateWheelTurning()
		{
			
		}


		public Vehicle FindVehicleInRange ()
		{

			// find any vehicles that have a seat inside the checking radius and sort by closest seat
			return Vehicle.AllVehicles
				.Where(x => x.Seats.Count > 0)
				.Where(x => Vector3.Distance(transform.position, x.FindClosestSeatTransform(transform.position).position) < EnterVehicleRadius)
				.OrderBy(x => Vector3.Distance(transform.position, x.FindClosestSeatTransform(transform.position).position))
				.FirstOrDefault();
			
		}

		public Vehicle TryEnterVehicleInRange ()
		{
			var vehicle = this.FindVehicleInRange ();
			if (null == vehicle)
				return null;

			var seat = vehicle.GetSeatAlignmentOfClosestSeat(this.transform.position);

			this.EnterVehicle(vehicle, seat);

			return vehicle;
		}

	}

}
