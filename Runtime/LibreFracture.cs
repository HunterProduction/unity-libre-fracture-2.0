using UnityEngine;
using NvBlast;



#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LibreFracture
{
    public static class LibreFracture
    {
        private const string k_FracturedObjectPostfix = "_Fractured";
        private const string k_LogPrefix = "[LibreFracture]";

        private const string k_FractureLayerName = "FractureChunk";
        public static int FractureLayer
        {
            get
            {
                var value = LayerMask.NameToLayer(k_FractureLayerName);
                if(value == -1)
                {
                    Debug.LogError($"{k_LogPrefix} No FractureChunk layer has been found. Please add it to the Layers panel.");
                }
                return LayerMask.NameToLayer(k_FractureLayerName);
            }
        }

        public static bool UseVHACDColliders { get; set; }

        private static int m_Seed
        {
            get
            {
            #if UNITY_EDITOR
                return (int)EditorApplication.timeSinceStartup;
            #else
                return (int)Time.realtimeSinceStartup;
            #endif
            }
        }

        public static GameObject CreateFracturedCopyOf(GameObject gameObject, FractureParameters parameters)
        {
            // Instantiate new gameObject copy to fracture
            GameObject fracturedObject = GameObject.Instantiate(gameObject, gameObject.transform.position, gameObject.transform.rotation);
            fracturedObject.transform.localScale = gameObject.transform.localScale;
            fracturedObject.name = $"{gameObject.name}{k_FracturedObjectPostfix}";

            Fracture(fracturedObject, parameters);

            return fracturedObject;
        }

        public static void Fracture(GameObject gameObject, FractureParameters parameters, bool isPreview = false, bool bypassColliderGeneration = false)
        {
            gameObject.transform.GetPositionAndRotation(out var originalPosition, out var originalRotation);
            var originalScale = gameObject.transform.localScale;

            gameObject.SetActive(true);
            gameObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            gameObject.transform.localScale = Vector3.one;

            // Retrieve mesh and set outside and inside materials
            var mesh = GetMesh(gameObject, out var outsideMaterial);
            if (mesh == null)
                return;

            var materials = new Material[2];
            materials[0] = outsideMaterial;
            materials[1] = parameters.insideMaterial != null ? parameters.insideMaterial : materials[0];

            // Fracture mesh in chunks
            var fractureTool = CreateChunks(mesh, parameters);
            NvLogger.Log($"{k_LogPrefix} {gameObject.name} fractured with Chunks count: " + fractureTool.getChunkCount());
            
            // For each mesh chunk
            for (int i = 1; i < fractureTool.getChunkCount(); i++)
            {
                // Instantiate chunk GameObject
                GameObject chunk = new GameObject($"{gameObject.name}_Chunk{i-1}");
                chunk.transform.parent = gameObject.transform;

                MeshFilter chunkMeshFilter = chunk.AddComponent<MeshFilter>();
                MeshRenderer chunkMeshRenderer = chunk.AddComponent<MeshRenderer>();

                chunkMeshRenderer.sharedMaterials = materials;

                // Generate Unity mesh from NvFractureTool
                NvMesh nvOutsideMesh = fractureTool.getChunkMesh(i, false);
                NvMesh nvInsideMesh = fractureTool.getChunkMesh(i, true);

                Mesh chunkMesh = nvOutsideMesh.toUnityMesh();
                chunkMesh.subMeshCount = 2;
                chunkMesh.SetIndices(nvInsideMesh.getIndexes(), MeshTopology.Triangles, 1);
                chunkMeshFilter.sharedMesh = chunkMesh;

                if (!bypassColliderGeneration)
                {
                    CreateColliderForChunk(chunk);
                }

                chunk.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                chunk.layer = FractureLayer;

                if (isPreview)
                {
                    chunk.AddComponent<ChunkDistancePreview>();
                }
                else
                {
                    chunk.AddComponent<ChunkNode>();
                }
            }
            
            gameObject.transform.SetPositionAndRotation(originalPosition, originalRotation);
            gameObject.transform.localScale = originalScale;

            var fracture = gameObject.AddComponent<ChunkGraphManager>();
            fracture.jointBreakForce = parameters.jointBreakForce;
            fracture.totalMass = parameters.totalObjectMass;
        }

        public static bool IsFracturable(GameObject gameObject)
        {
            return gameObject.hideFlags != HideFlags.NotEditable && 
                gameObject.layer != FractureLayer &&
                !gameObject.TryGetComponent<ChunkGraphManager>(out _) &&
                !gameObject.TryGetComponent<ChunkDistancePreview>(out _) &&
                GetMesh(gameObject, out _) != null;
        }

        #region Fractured Preview

        public static GameObject CreateFracturedPreviewOf(GameObject gameObject, FractureParameters parameters, bool previewColliders)
        {
            CleanUpPreview(gameObject, false);

            // Instantiate new gameObject copy to fracture
            GameObject fracturedPreview = GameObject.Instantiate(gameObject, gameObject.transform.position, gameObject.transform.rotation);
            fracturedPreview.transform.localScale = gameObject.transform.localScale;
            fracturedPreview.name = $"{gameObject.name}{k_FracturedObjectPostfix}Preview";

            Fracture(fracturedPreview, parameters, true, !previewColliders);

            //TODO: Consider SkinnedMeshRenderer case
            fracturedPreview.GetComponent<MeshRenderer>().enabled = false;

            return fracturedPreview;
        }

        //TODO: Consider renaming for best semantic generalization
        public static GameObject FinalizeFracturedPreview(GameObject fracturedObject, FractureParameters parameters)
        {
            foreach (Transform chunk in fracturedObject.transform)
            {
                if (chunk.TryGetComponent<ChunkDistancePreview>(out var chunkDistancePreview))
                {
                    chunkDistancePreview.UpdatePreview(0);
                    GameObject.DestroyImmediate(chunkDistancePreview);
                }
                chunk.gameObject.AddComponent<ChunkNode>();

                CreateColliderForChunk(chunk.gameObject);
            }

            //TODO: Consider SkinnedMeshRenderer case
            fracturedObject.GetComponent<MeshRenderer>().enabled = true;

            if(!fracturedObject.TryGetComponent<Collider>(out _))
                fracturedObject.AddComponent<MeshCollider>();

            var fracture = fracturedObject.GetComponent<ChunkGraphManager>();
            if (fracture == null) fracture = fracturedObject.AddComponent<ChunkGraphManager>();
            fracture.jointBreakForce = parameters.jointBreakForce;
            fracture.totalMass = parameters.totalObjectMass;

            fracturedObject.name = fracturedObject.name.Replace("Preview", "");
            fracturedObject.SetActive(true);

            return fracturedObject;
        }

        public static GameObject CreateFractureVisualizationOf(GameObject gameObject, FractureParameters parameters, GameObject pointPrefab)
        {
            CleanUpPreview(gameObject, false);

            GameObject pointsRoot = new GameObject($"{gameObject.name}{k_FracturedObjectPostfix}Visualization");
            pointsRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            pointsRoot.transform.localScale = Vector3.one;

            var mesh = GetMesh(gameObject, out _);
            if (mesh == null)
                return null;

            NvBlastExtUnity.setSeed(m_Seed);

            NvMesh nvMesh = new NvMesh(mesh.vertices,
                mesh.normals, 
                mesh.uv, 
                mesh.vertexCount, 
                mesh.GetIndices(0), 
                (int)mesh.GetIndexCount(0));

            var sites = parameters.GetFractureSitesGenerator(nvMesh).getSites();

            for(int i = 0; i < sites.Length; i++)
            {
                GameObject point = GameObject.Instantiate(pointPrefab, sites[i], Quaternion.identity, pointsRoot.transform);
                point.hideFlags = HideFlags.NotEditable;
            }

            pointsRoot.transform.rotation = gameObject.transform.rotation;
            pointsRoot.transform.position = gameObject.transform.position;

            return pointsRoot;
        }

        public static void UpdatePreviewDistance(GameObject fracturedPreviewObject, float previewDistance)
        {
            if (fracturedPreviewObject == null) return;

            foreach (var chunkDistancePreview in fracturedPreviewObject.GetComponentsInChildren<ChunkDistancePreview>())
            {
                chunkDistancePreview.UpdatePreview(previewDistance);
            }
        }

        public static void CleanUpPreview(GameObject sourceObject, bool leaveSourceObjectEnabled)
        {
            if (sourceObject != null)
            {
                GameObject.DestroyImmediate(GameObject.Find($"{sourceObject.name}{k_FracturedObjectPostfix}Preview"));
                GameObject.DestroyImmediate(GameObject.Find($"{sourceObject.name}{k_FracturedObjectPostfix}Visualization"));

                sourceObject.SetActive(leaveSourceObjectEnabled);
            }
        }

        #endregion

        #region Helper Private Methods
        private static Mesh GetMesh(GameObject gameObject, out Material outsideMaterial)
        {
            Mesh mesh = null;
            outsideMaterial = null;

            if (gameObject.TryGetComponent<SkinnedMeshRenderer>(out var smr))
            {
                outsideMaterial = smr.sharedMaterial;
                mesh = new Mesh();
                smr.BakeMesh(mesh);
            }
            else if (gameObject.TryGetComponent<MeshFilter>(out var meshFilter))
            {
                outsideMaterial = gameObject.GetComponent<MeshRenderer>().sharedMaterial;
                mesh = meshFilter.sharedMesh;
            }

            if (mesh == null)
            {
                Debug.LogError($"{k_LogPrefix} No MeshFilter or SkinneMeshRenderer found on {gameObject.name} object.");
                return null;
            }

            return mesh;
        }

        private static NvFractureTool CreateChunks(Mesh mesh, FractureParameters parameters)
        {
            NvBlastExtUnity.setSeed(m_Seed);

            NvMesh nvMesh = new NvMesh(mesh.vertices, mesh.normals, mesh.uv, mesh.vertexCount, mesh.GetIndices(0), (int)mesh.GetIndexCount(0));

            NvFractureTool fractureTool = new NvFractureTool();
            fractureTool.setRemoveIslands(parameters.enableIslands);
            fractureTool.setSourceMesh(nvMesh);
            parameters.ApplyParamsTo(fractureTool, nvMesh);
            fractureTool.finalizeFracturing();

            return fractureTool;
        }

        private static void CreateColliderForChunk(GameObject chunk)
        {
            if (UseVHACDColliders)
                ConvexMeshUtils.AttachApproximateConvexMeshCollidersTo(chunk);
            else
            {
                var collider = chunk.AddComponent<MeshCollider>();
                collider.sharedMesh = chunk.GetComponent<MeshFilter>().sharedMesh;
                collider.convex = true;
            }
        }
        #endregion
    }
}
