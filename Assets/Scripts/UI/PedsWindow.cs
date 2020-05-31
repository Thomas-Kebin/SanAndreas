﻿using System.Collections.Generic;
using UnityEngine;
using SanAndreasUnity.Utilities;
using SanAndreasUnity.Behaviours;
using SanAndreasUnity.Importing.Items;
using SanAndreasUnity.Importing.Items.Definitions;
using System.Linq;

namespace SanAndreasUnity.UI {

	public class PedsWindow : PauseMenuWindow {

		public int numPedsPerPage = 40;

		private Vector2 m_scrollPos = Vector2.zero;
		private List<PedestrianDef> m_pedDefs = new List<PedestrianDef> ();
		private int m_currentPageNumber = 1;
		private int m_currentPedIdWithOptions = -1;


		PedsWindow() {

			// set default parameters

			this.windowName = "Peds";
			this.useScrollView = false;

		}

		void Start () {

			this.RegisterButtonInPauseMenu ();

			// adjust rect
			this.windowRect = GUIUtils.GetCenteredRect( new Vector2(550, 450) );
		}

		void OnLoaderFinished ()
		{
			m_pedDefs = Item.GetDefinitions<PedestrianDef> ().ToList ();
		}


		protected override void OnWindowGUI ()
		{

			bool playerExists = Ped.Instance != null;


			float[] widthPercsLabels = new float[]{ 0.1f, 0.3f, 0.25f, 0.25f };
			float[] widthPercsButtons = new float[]{ 0.1f, 0.15f, 0.15f, 0.2f };
			float rowHeight = 40;
			float buttonSpacing = 3;


			// info about current ped
			if (playerExists) {
				GUILayout.Label ("Current ped:");
				this.DisplayPed( GetLayoutRect( rowHeight ), Ped.Instance.PedDef, false, true, widthPercsLabels, 
					widthPercsButtons, buttonSpacing );
			}

			// button to kill all peds
			if (GUILayout.Button ("Kill all peds", GUILayout.Width (100))) {
				KillAllPeds ();
			}
			GUILayout.Space (5);


			// page view numbers
			m_currentPageNumber = GUIUtils.DrawPagedViewNumbers( GetLayoutRect(20), m_currentPageNumber, m_pedDefs.Count, this.numPedsPerPage );
			GUILayout.Space (5);

			// column descriptions
			GUIUtils.DrawItemsInARowPerc (this.GetLayoutRect (rowHeight), (r, item) => GUI.Label (r, item),
				new string[]{ "Id", "Model name", "Default type", "Behaviour name" }, widthPercsLabels);
			GUILayout.Space (7);


			// scroll view with all peds
			m_scrollPos = GUILayout.BeginScrollView (m_scrollPos);

			foreach (var def in m_pedDefs.Skip ((m_currentPageNumber - 1) * this.numPedsPerPage).Take (this.numPedsPerPage)) {
				
				Rect rect = GetLayoutRect (rowHeight);

				this.DisplayPed (rect, def, true, playerExists, widthPercsLabels, widthPercsButtons, buttonSpacing);

				GUILayout.Space (12);
			}

			GUILayout.EndScrollView ();

		}

		public void DisplayPed (Rect rect, PedestrianDef def, bool displayOptions, bool playerExists, float[] widthPercsLabels,
			float[] widthPercsButtons, float buttonSpacing)
		{

			rect.height *= 0.5f;

			GUIUtils.DrawItemsInARowPerc( rect, 
				(r, item) => GUI.Label(r, item),
				new string[]{def.Id.ToString(), def.ModelName, def.DefaultType.ToString(), def.BehaviourName},
				widthPercsLabels);


			if (displayOptions) {

				rect.position += new Vector2 (0, rect.height);

				int i = 0;
				Rect itemRect;

				// display button which will open additional options
				itemRect = GUIUtils.GetNextRectInARowPerc (rect, ref i, buttonSpacing, widthPercsButtons);
				if (GUI.Button (itemRect, "...")) {
					m_currentPedIdWithOptions = def.Id;
				}

				if (m_currentPedIdWithOptions == def.Id) {
					// display additional options

					if (playerExists) {
						itemRect = GUIUtils.GetNextRectInARowPerc (rect, ref i, buttonSpacing, widthPercsButtons);
						if (GUI.Button (itemRect, "Switch")) {
							Ped.Instance.PlayerModel.Load (def.Id);
						}
					}

					itemRect = GUIUtils.GetNextRectInARowPerc (rect, ref i, buttonSpacing, widthPercsButtons);
					if (GUI.Button (itemRect, "Spawn")) {
						Ped.SpawnPed (def.Id);
					}

					itemRect = GUIUtils.GetNextRectInARowPerc (rect, ref i, buttonSpacing, widthPercsButtons);
					if (GUI.Button (itemRect, "Spawn stalker")) {
						Ped.SpawnPedStalker (def.Id);
					}

				}

			}

		}

		private Rect GetLayoutRect (float height)
		{
			return GUILayoutUtility.GetRect (this.WindowSize.x, height);
		}

		private static void KillAllPeds ()
		{

			foreach (var p in Ped.AllPeds) {
				if (p == Ped.Instance)
					continue;
				Destroy (p.gameObject);
			}

		}

	}

}
