using System.Collections.Generic;
using UnityEngine;

namespace LibreFracture
{
    [RequireComponent(typeof(Rigidbody))]
    public class ChunkNode : MonoBehaviour
    {
        public enum ChunkState
        {
            Connected, Anchored, Broken, Detached
        }

        public Dictionary<ChunkNode, Joint> connections = new Dictionary<ChunkNode, Joint>();
        public bool freezeWhenConnected;

        private Rigidbody _rigidBody;
        public Rigidbody Rigidbody
        {
            get
            {
                if (_rigidBody is null)
                    _rigidBody = GetComponent<Rigidbody>();
                return _rigidBody;
            }
        }

        //TODO: If we use VHACD Colliders generation, there may be more than 1 collider for chunk. Consider refactor this to be a collection.
        private MeshCollider _collider;
        private MeshCollider Collider
        {
            get
            {
                if (_collider is null)
                    _collider = GetComponent<MeshCollider>();
                return _collider;
            }
        }

        private float _boundingVolume;
        public float BoundingVolume
        {
            get
            {
                if(_boundingVolume == 0)
                {
                    var size = Collider.bounds.size;
                    _boundingVolume = size.x * size.y * size.z;
                }
                return _boundingVolume;
            }
        }

        private ChunkState _state;
        private MeshRenderer _renderer;

        private RigidbodyConstraints _backupConstraints = RigidbodyConstraints.None;
        private void OnEnable()
        {
            _renderer = GetComponent<MeshRenderer>();
            Rigidbody.constraints = _backupConstraints;
            _renderer.enabled = true;
            Collider.enabled = true;
        }

        private void OnDisable()
        {
            _backupConstraints = _rigidBody.constraints;
            Rigidbody.constraints = RigidbodyConstraints.FreezeAll;
            _renderer.enabled = false;
            Collider.enabled = false;
        }

        #region Joint Break Detection

        private bool _pendingCleanup;

        private void OnJointBreak(float breakForce)
        {
            _pendingCleanup = true;
        }

        /* NOTE: This approach using flag is needed because launching CleanBrokenConnections method directly inside the OnJoinBreak event
         * callback leads to the problem where the broken Joints are still not totally disposed. If this happens, checking for null Joints
         * will always give a true result.
         */
        private void FixedUpdate()
        {
            if(_pendingCleanup)
            {
                CleanBrokenConnections();
                _pendingCleanup = false;
            }
        }

        #endregion  

        private void CleanBrokenConnections()
        {
            var brokenNodes = new List<ChunkNode>();

            foreach (var pair in connections)
            {
                //Debug.Log($"[{GetType().Name}] {gameObject.name} Checking joint with {pair.Key.gameObject.name} = {pair.Value != null}");
                if (pair.Value == null)
                {
                    brokenNodes.Add(pair.Key);
                }
            }

            foreach (var brokenNode in brokenNodes)
            {
                connections.Remove(brokenNode);
                brokenNode.connections.Remove(this);

                brokenNode.UpdateState();

                //Debug.Log($"[{GetType().Name}] {gameObject.name} broke connection with {brokenNode.gameObject.name}");
            }

            UpdateState();
        }

        private void UpdateState()
        {
            if (connections.Count > 0)
            {
                if (_state != ChunkState.Anchored)
                {
                    SwitchState(ChunkState.Broken);
                }
            } 
            else
                SwitchState(ChunkState.Detached);
        }

        public void ConnectTouchingChunks(float jointBreakForce, float touchRadius = .01f)
        {
            connections = new Dictionary<ChunkNode, Joint>();
            
            var extents = Collider.bounds.extents;
            var maxExtent = Mathf.Max(Mathf.Max(extents.x, extents.y), extents.z);

            // Use a OverlapSphere to check surrounding chunks
            var overlaps = new HashSet<ChunkNode>();
            foreach (var hit in Physics.OverlapSphere(Rigidbody.worldCenterOfMass, maxExtent + touchRadius))
            {
                var hitChunkNode = hit.GetComponent<ChunkNode>();
                if (hitChunkNode != null && hitChunkNode != this)
                {
                    overlaps.Add(hitChunkNode);
                }
            }

            // Add Joints and create connections
            foreach (var overlap in overlaps)
            {
                // Avoid double FixedJoint component creation
                if (!overlap.connections.ContainsKey(this))
                {
                    var joint = gameObject.AddComponent<FixedJoint>();
                    joint.breakForce = jointBreakForce;
                    joint.connectedBody = overlap.Rigidbody;

                    connections.Add(overlap, joint);
                    // Bi-directional connection using the same FixedJoint
                    overlap.connections.Add(this, joint);

                    Debug.Log($"[{GetType().Name}] {gameObject.name} connected to {overlap.gameObject.name}");
                }
                else
                {
                    // Bi-directional connection using the already existing FixedJoint
                    connections.Add(overlap, overlap.connections[this]);
                    Debug.Log($"[{GetType().Name}] {gameObject.name} connected to {overlap.gameObject.name}. Joint already created.");
                }
            }

            SwitchState(ChunkState.Connected);

            Debug.Log($"[{GetType().Name}] {gameObject.name} has {connections.Count} connections");
        }

        public bool TryAnchor(float touchRadius = 0.001f)
        {
            var extents = Collider.bounds.extents;
            var maxExtent = Mathf.Max(Mathf.Max(extents.x, extents.y), extents.z);

            // Use a OverlapSphere to check surrounding colliders
            var overlaps = new HashSet<ChunkNode>();
            foreach (var hit in Physics.OverlapSphere(Rigidbody.worldCenterOfMass, maxExtent + touchRadius))
            {
                if (!hit.isTrigger && !hit.TryGetComponent<ChunkNode>(out _))
                {
                    SwitchState(ChunkState.Anchored);
                    return true;
                }
            }
            return false;
        }

        private void SwitchState(ChunkState newState)
        {
            switch (newState)
            {
                case ChunkState.Connected:
                    Freeze();
                    break;
                case ChunkState.Anchored:
                    Freeze();
                    _rigidBody.isKinematic = true;
                    break;
                case ChunkState.Broken:
                    UnFreeze();
                    break;
                case ChunkState.Detached:
                    _rigidBody.isKinematic = false;
                    UnFreeze();
                    break;
                default:
                    throw new System.NotImplementedException();

            }

            _state = newState;
        }

        private void Freeze()
        {
            if(freezeWhenConnected)
                _rigidBody.constraints = RigidbodyConstraints.FreezeAll;
            _rigidBody.Sleep();
        }

        private void UnFreeze()
        {
            _rigidBody.constraints = RigidbodyConstraints.None;
            _rigidBody.WakeUp();
        }

        #region Scene View Debug

        private Color _connectedColor = new Color32(0, 26, 255, 255);
        private Color _anchoredColor = new Color32(255, 153, 0, 255);
        private Color _brokenColor = new Color32(252, 3, 202, 255);
        private Color _detachedColor = Color.red;
        private void OnDrawGizmos()
        {
            // Draw a sphere in the chunk center of mass
            Gizmos.color = _state switch
            {
                ChunkState.Connected => _connectedColor,
                ChunkState.Anchored => _anchoredColor,
                ChunkState.Broken => _brokenColor,
                ChunkState.Detached => _detachedColor,
                _ => throw new System.NotImplementedException()
            };
            var centerOfMass = Rigidbody.worldCenterOfMass;
            Gizmos.DrawSphere(centerOfMass, .1f);

            // Draw connections
            if (connections == null)
                return;
            var connectionsCount = connections.Count;
            if (connectionsCount == 0)
                return;
            var points = new Vector3[2 * connectionsCount];
            int i = 0;
            Gizmos.color = Color.blue;
            foreach(var connectedNode in connections.Keys)
            {
                var otherCenterOfMass = connectedNode.Rigidbody.worldCenterOfMass;
                points[i++] = centerOfMass;
                points[i++] = otherCenterOfMass;

                //Draw gizmo line for connections
                Gizmos.DrawLine(points[i], points[i++]);
            }
        }

        #endregion
    }
}