using SanAndreasUnity.Importing.Paths;
using SanAndreasUnity.Utilities;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SanAndreasUnity.Behaviours
{
    public class MovementAgent
    {
        private float m_lastTimeWhenSearchedForPath = 0f;
        private List<PathNodeId> m_path = null;
        private int m_pathIndex = -1;
        private bool m_isSearchingForPath = false;
        public Vector3? Destination { get; set; } = null;
        private Vector3? m_lastAssignedDestination = null;
        private Vector3? m_lastPositionWhenAssignedDestination = null;

        private float m_lastTimeWhenWarped = 0f;

        public Vector3? NextMovementPos { get; private set; } = null;



        public void Update(Ped ped)
        {
            /*
                        // check if arrived to next position
                        if (m_path != null && Vector2.Distance(ped.transform.position.ToVec2WithXAndZ(), NodeReader.GetNodeById(m_path[m_pathIndex]).Position.ToVec2WithXAndZ())
                            < NodeReader.GetNodeById(m_path[m_pathIndex]).PathWidth / 2f)
                        {
                            // arrived at next position
                            m_pathIndex ++;
                            if (m_pathIndex >= m_path.Count)
                            {
                                // arrived at destination
                                m_path = null;
                                m_pathIndex = -1;
                                NextMovementPos = null;
                            }
                            else
                            {
                                // move-on to next path node
                                NextMovementPos = NodeReader.GetNodeById(m_path[m_pathIndex]).Position;
                            }
                        }
            */

            /*this.NextMovementPos = ped.NavMeshAgent.hasPath
                ? (Vector3?) ped.NavMeshAgentNextPosition
                : null;*/


            Vector3 myPosition = ped.transform.position;

            ped.NavMeshAgent.nextPosition = myPosition;
            if (ped.NavMeshAgent.nextPosition.WithXAndZ() != myPosition.WithXAndZ()
                && Time.time - m_lastTimeWhenWarped > 1f)
            {
                m_lastTimeWhenWarped = Time.time;
                ped.NavMeshAgent.Warp(myPosition);

                if (this.Destination.HasValue)
                    this.SetDestination(ped);
            }
            //this.NavMeshAgent.velocity = this.Velocity;

            if (!this.Destination.HasValue)
            {
                m_lastAssignedDestination = null;
                m_lastPositionWhenAssignedDestination = null;

                if (ped.NavMeshAgent.hasPath)
                    ped.NavMeshAgent.ResetPath();
                
                return;
            }

            if (Time.time - m_lastTimeWhenSearchedForPath < 0.4f)
                return;
            
            if (ped.NavMeshAgent.pathPending)
                return;

            if (!ped.NavMeshAgent.isOnNavMesh)
                return;

            if (!m_lastAssignedDestination.HasValue)
            {
                this.SetDestination(ped);
                return;
            }

            // check if target position changed by some delta value (this value should depend on distance to target
            // - if target is too far away, value should be higher)

            Vector3 diffToTarget = this.Destination.Value - myPosition;
            float distanceToTarget = diffToTarget.magnitude;
            Vector3 deltaPos = this.Destination.Value - m_lastAssignedDestination.Value;
            float deltaPosLength = deltaPos.magnitude;

            // we require 10% change, with 1.5 as min
            float requiredPosChange = Mathf.Max(distanceToTarget * 0.1f, 1.5f);
            
            if (deltaPosLength > requiredPosChange)
            {
                this.SetDestination(ped);
                return;
            }

            // check if angle to target changed by some delta value (eg. 25 degrees)
            // - this will make the ped turn fast in response to target changing movement direction

            Vector3 lastDiffToTarget = m_lastAssignedDestination.Value - m_lastPositionWhenAssignedDestination.Value;
            float angleDelta = Vector3.Angle(this.Destination.Value - m_lastPositionWhenAssignedDestination.Value, lastDiffToTarget);
            if (angleDelta > 25f)
            {
                this.SetDestination(ped);
                return;
            }

            // regularly update path on some higher interval (eg. 5s)
            // - this interval could also depend on distance to target

            // from 5 to 12, with sqrt function, 150 as max distance
            float regularUpdateInterval = 5 + 7 * Mathf.Clamp01(Mathf.Sqrt(Mathf.Min(distanceToTarget, 150f) / 150f));

            if (Time.time - m_lastTimeWhenSearchedForPath > regularUpdateInterval
                && this.Destination.Value != m_lastAssignedDestination.Value)
            {
                this.SetDestination(ped);
                return;
            }

        }

        void SetDestination(Ped ped)
        {
            m_lastTimeWhenSearchedForPath = Time.time;
            m_lastAssignedDestination = this.Destination.Value;
            m_lastPositionWhenAssignedDestination = ped.transform.position;

            // here we need to sample position on navmesh first, because otherwise agent will fail
            // to calculate path if target position is not on navmesh, and as a result he will be stopped

            // there is a performance problem: if target position is on isolated part of navmesh,
            // path calculation will take too long because the algorithm tries to go through all
            // surrounding nodes

            // TODO: maybe try to manually calculate path and assign it to agent ?

            if (NavMesh.SamplePosition(this.Destination.Value, out var hit, 100f, ped.NavMeshAgent.areaMask))
            {
                var navMeshPath = new NavMeshPath();
                NavMesh.CalculatePath(ped.NavMeshAgent.nextPosition, hit.position, ped.NavMeshAgent.areaMask, navMeshPath);
                ped.NavMeshAgent.path = navMeshPath;

                //ped.NavMeshAgent.SetDestination(hit.position);
            }
        }

        void OnPathFinished(PathfindingManager.PathResult pathResult)
        {
            m_isSearchingForPath = false;
            m_lastTimeWhenSearchedForPath = Time.time;

            if (null == pathResult || !pathResult.IsSuccess)
            {
                m_path = null;
                m_pathIndex = -1;
                NextMovementPos = null;

                return;
            }

            m_path = pathResult.Nodes;
            m_pathIndex = 0;
            NextMovementPos = NodeReader.GetNodeById(m_path[m_pathIndex]).Position;
        }
    }
}
