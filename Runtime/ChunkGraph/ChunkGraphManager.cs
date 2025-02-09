using UnityEngine;

namespace LibreFracture
{
    public class ChunkGraphManager : MonoBehaviour
    {
        [Header("Parameters")]
        public float totalMass;
        public float jointBreakForce;

        [Header("Behaviour")]
        [Tooltip("If true, chunks bordering other colliders nearby gets anchored to those ones.")]
        [SerializeField] private bool anchored;
        [Tooltip("If true, each chunk's rigidbody has its constraints set to frozen when still connected to the graph.")]
        [SerializeField] private bool freezeConnectedChunks;
        [Tooltip("If true, object at start disables all its chunks and enables its root mesh renderer and collider. Chunks will enable on first collision.")]
        [SerializeField] private bool startAsleep;

        private ChunkNode[] _nodes;
        private MeshRenderer _meshRenderer;
        private Collider _collider;

        private bool _asleep = false;
        public bool Asleep => _asleep;

        private void Awake()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            _collider = GetComponent<Collider>();
            _collider.enabled = false;
            _meshRenderer.enabled = false;

            _nodes = GetComponentsInChildren<ChunkNode>();
            Init();
        }

        private void Start()
        {
            SetAsleep(startAsleep);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!_asleep || collision.gameObject.layer == LibreFracture.FractureLayer)
                return;

            SetAsleep(false);
        }

        private void SetAsleep(bool value)
        {
            _asleep = value;
            _collider.enabled = value;
            _meshRenderer.enabled = value;
            foreach (ChunkNode node in _nodes)
            {
                node.enabled = !value;
            }
        }

        private void Init()
        {
            float boundingVolumesSum = 0f;

            foreach (ChunkNode node in _nodes)
            {
                // Setup Node
                node.freezeWhenConnected = freezeConnectedChunks;
                node.ConnectTouchingChunks(jointBreakForce);
                if (anchored)
                    node.TryAnchor();

                // Accumulate to distribute mass based on Chunk approximated dimension
                boundingVolumesSum += node.BoundingVolume;
            }

            // Distrubute mass
            // TODO: There may be cases in which computed mass for a Rigidbody is very small, resulting in weird chunk behaviour. Try normalize the distribution
            // allowing a min mass threshold value.
            foreach (ChunkNode node in _nodes)
            {
                node.Rigidbody.mass = totalMass * node.BoundingVolume / boundingVolumesSum;
            }
        }
    }
}