﻿using Assets.Scripts.Importing.Paths;
using SanAndreasUnity.Importing.Items.Definitions;
using SanAndreasUnity.Net;
using SanAndreasUnity.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SanAndreasUnity.Behaviours
{
    public class PathsManager : StartupSingleton<PathsManager>
    {
        public const float MaxNPCDistance = 100.0f; // Max distance from each players before delete
        public const float MinNPCCreateDistance = 50.0f; // Min distance from each players to spawn ped
        public const float RefreshRate = 2f; // Number of seconds between each refresh
        public const int MaxNumberOfNPCAtSpawnPoint = 25;

        private float lastUpdateTime;

        protected override void OnSingletonAwake()
        {
            lastUpdateTime = Time.time;

            Ped.onStart += PedOnStart;
        }

        private void PedOnStart(Ped ped)
        {
            if (!NetStatus.IsServer)
                return;

            if (ped.PlayerOwner != null)
                this.StartCoroutine(SpawnPedWithAI(ped.transform.position));
        }

        private void Update()
        {
            if (NetStatus.IsServer)
            {
                if (Time.time > lastUpdateTime + RefreshRate)
                {
                    List<Ped> npcs = Ped.AllPeds.Where(ped => ped.PlayerOwner == null).ToList();
                    List<Ped> players = Ped.AllPeds.Where(ped => ped.PlayerOwner != null).ToList();

                    List<Vector3> playersPos = new List<Vector3>();

                    foreach (Ped player in players)
                    {
                        playersPos.Add(player.transform.position);
                    }

                    bool isNearPlayer; // If false, delete NPC
                    foreach (Ped npc in npcs)
                    {
                        isNearPlayer = false;
                        foreach (Ped player in players)
                        {
                            if (Vector3.Distance(npc.transform.position, player.transform.position) < MaxNPCDistance)
                                isNearPlayer = true;
                        }
                        if(!isNearPlayer)
                            Destroy(npc.gameObject);
                    }

                    int nbrOfNPCInZone;
                    foreach (Ped player in players)
                    {
                        nbrOfNPCInZone = 0;
                        foreach (Ped npc in npcs)
                        {
                            if (Vector3.Distance(npc.transform.position, player.transform.position) < MaxNPCDistance)
                                nbrOfNPCInZone++;
                        }
                        if (nbrOfNPCInZone < 5)
                        {
                            Vector3 targetZone = player.transform.position + player.transform.forward * MinNPCCreateDistance;
                            this.StartCoroutine(SpawnPedWithAI(targetZone));
                        }
                    }

                    lastUpdateTime = Time.time;
                }
            }
        }

        private static System.Collections.IEnumerator SpawnPedWithAI(Vector3 targetZone)
        {
            int currentArea = NodeFile.GetAreaFromPosition(targetZone);
            List<int> nearAreas = NodeFile.GetAreaNeighborhood(currentArea);
            List<Ped> pedList = new List<Ped>();

            foreach (NodeFile file in NodeReader.Nodes.Where(f => nearAreas.Contains(f.Id) || f.Id == currentArea))
            {
                foreach (PathNode node in file.PathNodes.Where(pn => pn.NodeType > 2
                                                                     && Vector3.Distance(pn.Position, targetZone) < MaxNPCDistance
                                                                     && Vector3.Distance(pn.Position, targetZone) > MinNPCCreateDistance))
                {
                    if (UnityEngine.Random.Range(0, 255) > node.Flags.SpawnProbability)
                    {
                        PathNode pedNode = node;
                        Vector3 spawnPos = new Vector3(pedNode.Position.x, pedNode.Position.y, pedNode.Position.z);

                        Ped newPed = Ped.SpawnPed(Ped.RandomPedId, spawnPos + new Vector3(0, 1, 0), Quaternion.identity, true);

                        var ai = newPed.gameObject.GetOrAddComponent<Ped_AI>();
                        ai.CurrentNode = pedNode;
                        ai.TargetNode = pedNode;

                        pedList.Add(newPed);
                        yield return null;
                    }

                    if (Ped_AI.AllPedAIs.Count(p => Math.Abs(Vector3.Distance(p.transform.position, targetZone)) < MaxNPCDistance) > MaxNumberOfNPCAtSpawnPoint)
                        break;
                }
            }

            yield return null;

            foreach (Ped ped in pedList)
            {
                Weapon weapon = null;

                switch (ped.PedDef.DefaultType)
                {
                    case PedestrianType.Cop:
                        weapon = ped.WeaponHolder.SetWeaponAtSlot(346, 0);
                        break;
                    case PedestrianType.Criminal:
                        weapon = ped.WeaponHolder.SetWeaponAtSlot(347, 0);
                        break;
                    case PedestrianType.GangMember:
                        weapon = ped.WeaponHolder.SetWeaponAtSlot(352, 0);
                        break;
                }

                if (weapon != null)
                {
                    ped.WeaponHolder.SwitchWeapon(weapon.SlotIndex);
                    WeaponHolder.AddRandomAmmoAmountToWeapon(weapon);
                }
            }
        }
        
        public static PathNode GetNextPathNode(PathNode origin, PathNode current)
        {
            List<int> areas = NodeFile.GetAreaNeighborhood(origin.AreaID);
            NodeFile file = NodeReader.Nodes.First(f => f.Id == origin.AreaID);
            List<PathNode> possibilities = new List<PathNode>();
            for (int i = 0; i < current.LinkCount; i++)
            {
                int linkArrayIndex = current.BaseLinkID + i;
                NodeFile nf = NodeReader.Nodes.Single(nf2 => nf2.Id == file.NodeLinks[linkArrayIndex].AreaID);
                PathNode target = nf.PathNodes.ElementAt(file.NodeLinks[linkArrayIndex].NodeID);
                if (!target.Equals(origin))
                    possibilities.Add(target);
            }

            if (possibilities.Count > 0)
            {
                return possibilities.ElementAt(UnityEngine.Random.Range(0, possibilities.Count - 1));
            }
            else
            {
                //No possibilities found, returning to origin
                return origin;
            }

        }
    }
}