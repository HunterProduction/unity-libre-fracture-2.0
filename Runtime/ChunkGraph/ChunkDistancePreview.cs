using UnityEngine;

namespace LibreFracture
{
    public class ChunkDistancePreview : MonoBehaviour
    {
        private Vector3 _defaultPosition;
        private Quaternion _defaultRotation;
        private Bounds _defaultBounds;

        void Reset()
        {
            MeshRenderer renderer = GetComponent<MeshRenderer>();

            _defaultBounds = renderer.bounds;
            _defaultPosition = transform.position;
            _defaultRotation = transform.rotation;
        }

        public void UpdatePreview(float distance)
        {
            transform.position = transform.parent.TransformPoint(_defaultBounds.center * distance);
        }
    }
}