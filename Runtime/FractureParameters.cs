using System;
using UnityEngine;
using NvBlast;

namespace LibreFracture
{
    //TODO: Add all params documentation. Consider serialization for usage in Editor.
    public enum FractureType
    {
        Voronoi,
        Clustered,
        Slicing,
        Skinned,
        Plane,
        Cutout
    }

    [Serializable]
    public abstract class FractureParameters
    {
        public float jointBreakForce;
        public float totalObjectMass;
        public Material insideMaterial;
        public bool enableIslands;

        public abstract NvVoronoiSitesGenerator GetFractureSitesGenerator(NvMesh nvMesh);
        public abstract void ApplyParamsTo(NvFractureTool nvFractureTool, NvMesh nvMesh);
    }

    [Serializable]
    public class VoronoiParameters : FractureParameters
    {
        [Range(2, 1000)]
        public int totalChunks = 5;

        public override NvVoronoiSitesGenerator GetFractureSitesGenerator(NvMesh nvMesh)
        {
            NvVoronoiSitesGenerator sites = new NvVoronoiSitesGenerator(nvMesh);
            sites.uniformlyGenerateSitesInMesh(totalChunks);
            return sites;
        }

        public override void ApplyParamsTo(NvFractureTool nvFractureTool, NvMesh nvMesh)
        {
            nvFractureTool.voronoiFracturing(0, GetFractureSitesGenerator(nvMesh));
        }
    }

    [Serializable]
    public class ClusteredParameters : FractureParameters
    {
        [Range(1, 50)]
        public int clusters = 1;
        [Range(1, 200)]
        public int sitesPerCluster = 30;
        public float clusterRadius = .2f;

        public override NvVoronoiSitesGenerator GetFractureSitesGenerator(NvMesh nvMesh)
        {
            NvVoronoiSitesGenerator sites = new NvVoronoiSitesGenerator(nvMesh);
            sites.clusteredSitesGeneration(clusters, sitesPerCluster, clusterRadius);
            return sites;
        }

        public override void ApplyParamsTo(NvFractureTool nvFractureTool, NvMesh nvMesh)
        {
            nvFractureTool.voronoiFracturing(0, GetFractureSitesGenerator(nvMesh));
        }
    }

    [Serializable]
    public class SlicingParameters : FractureParameters
    {
        public Vector3Int slices = Vector3Int.one;
        [Range(0f, 1f)]
        public float offsetVariations = 0f;
        [Range(0f, 1f)]
        public float angleVariations = 0f;

        public float amplitude = 0f;
        public float frequency = 1f;

        public int octaveNumber = 1;
        public int surfaceResolution = 2;

        public override NvVoronoiSitesGenerator GetFractureSitesGenerator(NvMesh nvMesh)
        {
            Debug.LogWarning($"[{GetType().Name}] Sites Generator is not available for Slicing fracture type.");
            return new NvVoronoiSitesGenerator(nvMesh);
        }

        public override void ApplyParamsTo(NvFractureTool nvFractureTool, NvMesh nvMesh)
        {
            SlicingConfiguration conf = new SlicingConfiguration();
            conf.slices = slices;
            conf.offset_variations = offsetVariations;
            conf.angle_variations = angleVariations;

            conf.noise.amplitude = amplitude;
            conf.noise.frequency = frequency;
            conf.noise.octaveNumber = octaveNumber;
            conf.noise.surfaceResolution = surfaceResolution;

            nvFractureTool.slicing(0, conf, false);
        }
    }

    [Serializable]
    public class SkinnedParameters : FractureParameters
    {
        public GameObject skinnedMeshRendererObject;

        public override NvVoronoiSitesGenerator GetFractureSitesGenerator(NvMesh nvMesh)
        {
            if(skinnedMeshRendererObject == null)
            {
                Debug.LogError($"No object reference se to skinnedMeshRendererObject");
                return null;
            }
            if (!skinnedMeshRendererObject.TryGetComponent<SkinnedMeshRenderer>(out var smr))
            {
                Debug.LogError($"No SkinnedMeshRenderer found on {skinnedMeshRendererObject.name} object");
                return null;
            }

            NvVoronoiSitesGenerator sites = new NvVoronoiSitesGenerator(nvMesh);
            sites.boneSiteGeneration(smr);
            return sites;
        }

        public override void ApplyParamsTo(NvFractureTool nvFractureTool, NvMesh nvMesh)
        {
            nvFractureTool.voronoiFracturing(0, GetFractureSitesGenerator(nvMesh));
        }
    }
}

