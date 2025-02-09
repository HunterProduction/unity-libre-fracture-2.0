using LibreFracture.MeshProcess;
using System.Collections.Generic;
using UnityEngine;

namespace LibreFracture
{
    public static class ConvexMeshUtils
    {
        public static uint VHACDResolution { get; set; } = 100000;

        public static void AttachApproximateConvexMeshCollidersTo(GameObject gameObject)
        {
            // Generate convex meshes for colliders
            // NOTE: Performance Heavy
            VHACD vhacd = gameObject.AddComponent<VHACD>();
            vhacd.m_parameters.m_resolution = VHACDResolution;
            List<Mesh> colliderMeshes = vhacd.GenerateConvexMeshes();

            var previousColliders = gameObject.GetComponents<Collider>();
            foreach (var collider in previousColliders)
                GameObject.DestroyImmediate(collider);

            foreach (Mesh colliderMesh in colliderMeshes)
            {
                MeshCollider collider = gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = colliderMesh;
                collider.convex = true;
            }

            GameObject.DestroyImmediate(vhacd);
        }
    }
}
