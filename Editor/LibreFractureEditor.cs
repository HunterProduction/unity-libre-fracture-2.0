using System;
using UnityEditor;
using UnityEngine;

namespace LibreFracture.Editor
{
    public class LibreFractureEditor : EditorWindow
    {
        private enum GenerateType
        {
            Preview,
            Generate,
            CreatePrefab
        }

        private const string k_PrefabsFolderName = "LibreFracture";

        [MenuItem("Tools/Libre Fracture 2.0")]
        public static void OpenEditor()
        {
            GetWindow<LibreFractureEditor>("Libre Fracture 2.0");
        }

        [SerializeField] private GameObject source;

        [SerializeField] private FractureType fractureType = FractureType.Voronoi;

        [SerializeField] private bool previewColliders = false;
        [SerializeField] private float previewDistance = 0.5f;

        // General
        [SerializeField] private float objectMass = 100;
        [SerializeField] private float jointBreakForce = 100;
        [SerializeField] private Material insideMaterial;
        [SerializeField] private bool islands = false;

        [SerializeField] private VoronoiParameters voronoiParams;
        [SerializeField] private ClusteredParameters clusteredParams;
        [SerializeField] private SkinnedParameters skinnedParams;
        [SerializeField] private SlicingParameters slicingParams;
        private FractureParameters CurrentSelectedParams
        {
            get
            {
                return fractureType switch
                {
                    FractureType.Voronoi => voronoiParams,
                    FractureType.Clustered => clusteredParams,
                    FractureType.Skinned => skinnedParams,
                    FractureType.Slicing => slicingParams,
                    _ => throw new NotImplementedException($"Selected FractureType not implemented yet.")
                };
            }
        }

        private GameObject _point, _previewObject, _tempSource;

        private void OnEnable()
        {
            _point = (GameObject)Resources.Load("Point");
            voronoiParams = new VoronoiParameters();
            slicingParams = new SlicingParameters();
            clusteredParams = new ClusteredParameters();
            skinnedParams = new SkinnedParameters();
        }

        private void OnSelectionChange()
        {
            Repaint();
        }

        protected void OnGUI()
        {
            GUILayout.Space(6);
            EditorGUILayout.LabelField("Libre Fracture 2.0", CustomStyles.TitleStyle);
            GUILayout.Space(3);

            if (Application.isPlaying)
            {
                GUILayout.Label("PLAY MODE ACTIVE", GUI.skin.box, GUILayout.ExpandWidth(true));
                return;
            }

            EditorGUILayout.LabelField("Source Object", CustomStyles.HeaderStyleLeft);

            EditorGUI.BeginChangeCheck();
            _tempSource  = EditorGUILayout.ObjectField("Source", _tempSource, typeof(GameObject), true) as GameObject;
            if (EditorGUI.EndChangeCheck())
            {
                SetSource(_tempSource);
            }

            var clicked = GUILayout.Button("Set selected object as Source");
            var selected = Selection.activeGameObject;
            if (clicked && selected != null)
            {
                SetSource(selected);
            }

            if (!source) 
                return;

            GUILayout.Space(15);
            GeneralParamsGUI();

            GUILayout.Space(10);
            EditorGUI.BeginChangeCheck();

            bool canCreate = fractureType switch
            {
                FractureType.Voronoi => VoronoiGUI(),
                FractureType.Clustered => ClusteredGUI(),
                FractureType.Slicing => SlicingGUI(),
                FractureType.Skinned => SkinnedGUI(),
                FractureType.Plane => PlaneGUI(),
                FractureType.Cutout => CutoutGUI(),
                _ => false
            };

            if (EditorGUI.EndChangeCheck())
            {
                _previewObject = LibreFracture.CreateFracturedPreviewOf(source, CurrentSelectedParams, previewColliders);
            }

            GUILayout.Space(10);
            ExperimentalGUI();

            GUILayout.Space(20);

            if (canCreate)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Generate", GUILayout.Height(30)))
                {
                    if (_previewObject == null)
                        _previewObject = LibreFracture.CreateFracturedCopyOf(source, CurrentSelectedParams);

                    LibreFracture.FinalizeFracturedPreview(_previewObject, CurrentSelectedParams);
                }

                if (GUILayout.Button("Create Prefab", GUILayout.Height(30)))
                {
                    if (LibreFracture.UseVHACDColliders)
                    {
                        Debug.LogWarning("[LibreFractureEditor] 'Create Prefab' when using Advanced Convex Colliders is not supported yet.");
                    }
                    else
                    {
                        if (_previewObject == null)
                            _previewObject = LibreFracture.CreateFracturedCopyOf(source, CurrentSelectedParams);

                        LibreFracture.FinalizeFracturedPreview(_previewObject, CurrentSelectedParams);
                        SaveFracturedPrefab(source, _previewObject);
                    }
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10);
            PreviewGUI();
        }

        private void OnDestroy()
        {
            LibreFracture.CleanUpPreview(source, true);
        }

        private void GeneralParamsGUI()
        {
            EditorGUILayout.LabelField("Object Properties", CustomStyles.HeaderStyleLeft);

            EditorGUI.BeginChangeCheck();

            objectMass = EditorGUILayout.FloatField("Object Mass", objectMass);
            jointBreakForce = EditorGUILayout.FloatField("Joint Break Force", jointBreakForce);

            if (EditorGUI.EndChangeCheck())
            {
                AssignGeneralParamsToAll();
            }

            EditorGUI.BeginChangeCheck();

            insideMaterial = (Material)EditorGUILayout.ObjectField("Inside Material", insideMaterial, typeof(Material), false);
            if (!insideMaterial)
                EditorGUILayout.HelpBox("If inside material is not assigned, the same material as material of selected object will be used", MessageType.Info);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Fracture Properties", CustomStyles.HeaderStyleLeft);

            fractureType = (FractureType)EditorGUILayout.EnumPopup("Fracture Type", fractureType);
            
            if (EditorGUI.EndChangeCheck())
            {
                AssignGeneralParamsToAll();
                _previewObject = LibreFracture.CreateFracturedPreviewOf(source, CurrentSelectedParams, previewColliders);
            }
        }

        private bool _experimentalFoldout;
        private void ExperimentalGUI()
        {
            _experimentalFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_experimentalFoldout, "Experimental");
            if (_experimentalFoldout)
            {
                EditorGUI.BeginChangeCheck();

                islands = EditorGUILayout.Toggle("Islands", islands);

                LibreFracture.UseVHACDColliders = EditorGUILayout.Toggle("Advanced Convex Colliders", LibreFracture.UseVHACDColliders);
                if (LibreFracture.UseVHACDColliders)
                {
                    ConvexMeshUtils.VHACDResolution = (uint)EditorGUILayout.IntSlider(new GUIContent("VHACD Resolution"), (int)ConvexMeshUtils.VHACDResolution, 100000, 64000000);
                    EditorGUILayout.HelpBox("Use Approximate Convex Decomposition (VHACD) to generate more precise mesh colliders.\n" +
                        "This method is more performance heavy, as it may generate more than one MeshCollider for each chunk. " +
                        "'Create Prefab' operation with this option enabled is not supported yet.", MessageType.Warning);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    AssignGeneralParamsToAll();
                }
            }
        }

        private void PreviewGUI()
        {
            EditorGUILayout.LabelField("Preview", CustomStyles.HeaderStyleLeft);
            EditorGUI.BeginChangeCheck();
            previewColliders = EditorGUILayout.Toggle("Preview Colliders", previewColliders);
            if (EditorGUI.EndChangeCheck())
            {
                _previewObject = LibreFracture.CreateFracturedPreviewOf(source, CurrentSelectedParams, previewColliders);
            }
            EditorGUI.BeginChangeCheck();
            previewDistance = EditorGUILayout.Slider("Preview Chunks Distance", previewDistance, 0, 5);
            if (EditorGUI.EndChangeCheck())
            {
                LibreFracture.UpdatePreviewDistance(_previewObject, previewDistance);
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Regenerate Preview", GUILayout.Height(25), GUILayout.Width(135)))
            {
                _previewObject = LibreFracture.CreateFracturedPreviewOf(source, CurrentSelectedParams, previewColliders);
            }
            if (GUILayout.Button("Clean Up", GUILayout.Height(25), GUILayout.Width(135)))
                LibreFracture.CleanUpPreview(source, true);

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            if (_previewObject != null)
            {
                EditorGUILayout.LabelField($"Chunks Count: {_previewObject.transform.childCount}", EditorStyles.miniLabel);
            }

            LibreFracture.UpdatePreviewDistance(_previewObject, previewDistance);
        }

        private bool VoronoiGUI()
        {
            EditorGUILayout.LabelField("Voronoi Fracture", CustomStyles.HeaderStyleCentered);

            voronoiParams.totalChunks = EditorGUILayout.IntSlider("Total Chunks", voronoiParams.totalChunks, 2, 1000);

            if (GUILayout.Button("Visualize Points"))
            {
                LibreFracture.CreateFractureVisualizationOf(source,voronoiParams, _point);
            }
            return true;
        }

        private bool ClusteredGUI()
        {
            EditorGUILayout.LabelField("Clustered Fracture", CustomStyles.HeaderStyleCentered);

            clusteredParams.clusters = EditorGUILayout.IntSlider("Clusters", clusteredParams.clusters, 1, 100);
            clusteredParams.sitesPerCluster = EditorGUILayout.IntSlider("Sites", clusteredParams.sitesPerCluster, 1, 100);
            clusteredParams.clusterRadius = EditorGUILayout.FloatField("Radius", clusteredParams.clusterRadius);

            if (GUILayout.Button("Visualize Points"))
            {
                LibreFracture.CreateFractureVisualizationOf(source, clusteredParams, _point);
            }

            return true;
        }

        private bool SkinnedGUI()
        {
            EditorGUILayout.LabelField("Skinned Fracture", CustomStyles.HeaderStyleCentered);

            if (source.GetComponent<SkinnedMeshRenderer>() == null)
            {
                EditorGUILayout.HelpBox("Skinned Mesh Not Selected", MessageType.Error);
                return false;
            }

            if (source.transform.root.position != Vector3.zero)
            {
                EditorGUILayout.HelpBox("Root must be at 0,0,0 for Skinned Meshes", MessageType.Info);
                if (GUILayout.Button("FIX"))
                {
                    source.transform.root.position = Vector3.zero;
                    source.transform.root.rotation = Quaternion.identity;
                    source.transform.root.localScale = Vector3.one;
                }

                return false;
            }

            skinnedParams.skinnedMeshRendererObject = source;
            if (GUILayout.Button("Visualize Points"))
            {
                LibreFracture.CreateFractureVisualizationOf(source, skinnedParams, _point);
            }

            return true;
        }

        private bool SlicingGUI()
        {
            EditorGUILayout.LabelField("Slicing Fracture", CustomStyles.HeaderStyleCentered);

            slicingParams.slices = EditorGUILayout.Vector3IntField("Slices", slicingParams.slices);
            slicingParams.offsetVariations = EditorGUILayout.Slider("Offset", slicingParams.offsetVariations, 0, 1);
            slicingParams.angleVariations = EditorGUILayout.Slider("Angle", slicingParams.angleVariations, 0, 1);

            GUILayout.BeginHorizontal();
            slicingParams.amplitude = EditorGUILayout.FloatField("Amplitude", slicingParams.amplitude);
            slicingParams.frequency = EditorGUILayout.FloatField("Frequency", slicingParams.frequency);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            slicingParams.octaveNumber = EditorGUILayout.IntField("Octave", slicingParams.octaveNumber);
            slicingParams.surfaceResolution = EditorGUILayout.IntField("Resolution", slicingParams.surfaceResolution);
            GUILayout.EndHorizontal();

            return true;
        }

        private bool PlaneGUI()
        {
            EditorGUILayout.LabelField("Plane Fracture", CustomStyles.HeaderStyleCentered);

            GUILayout.Label("Not Implemented...");
            return false;
        }

        private bool CutoutGUI()
        {
            EditorGUILayout.LabelField("Cutout Fracture", CustomStyles.HeaderStyleCentered);

            GUILayout.Label("Not Implemented...");
            return false;
        }

        private void SetSource(GameObject target)
        {
            if (LibreFracture.IsFracturable(target))
            {
                LibreFracture.CleanUpPreview(source, false);

                _tempSource = target;
                source = target;

                if (source.activeInHierarchy)
                    source.SetActive(false);

                _previewObject = LibreFracture.CreateFracturedPreviewOf(source, CurrentSelectedParams, previewColliders);

                Selection.activeGameObject = _previewObject;
            }
        }

        private void AssignGeneralParamsToAll()
        {
            AssignGeneralParams(voronoiParams);
            AssignGeneralParams(clusteredParams);
            AssignGeneralParams(skinnedParams);
            AssignGeneralParams(slicingParams);
        }

        private void AssignGeneralParams(FractureParameters generalParams)
        {
            generalParams.insideMaterial = insideMaterial;
            generalParams.jointBreakForce = jointBreakForce;
            generalParams.totalObjectMass = objectMass;
            generalParams.enableIslands = islands;
        }

        private void SaveFracturedPrefab(GameObject originalObject, GameObject fracturedObject)
        {
            // Prepare directories
            if (!AssetDatabase.IsValidFolder($"Assets/{k_PrefabsFolderName}")) AssetDatabase.CreateFolder("Assets", $"{k_PrefabsFolderName}");
            if (!AssetDatabase.IsValidFolder($"Assets/{k_PrefabsFolderName}/Meshes")) AssetDatabase.CreateFolder($"Assets/{k_PrefabsFolderName}", "Meshes");
            if (!AssetDatabase.IsValidFolder($"Assets/{k_PrefabsFolderName}/Prefabs")) AssetDatabase.CreateFolder($"Assets/{k_PrefabsFolderName}", "Prefabs");

            FileUtil.DeleteFileOrDirectory($"Assets/{k_PrefabsFolderName}/Meshes/{originalObject.name}");
            FileUtil.DeleteFileOrDirectory($"Assets/{k_PrefabsFolderName}/Meshes/{originalObject.name}.meta");
            AssetDatabase.Refresh();
            AssetDatabase.CreateFolder($"Assets/{k_PrefabsFolderName}/Meshes", originalObject.name);

            fracturedObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            // Save all chunk meshes
            int i = 0;
            foreach (Transform chunk in fracturedObject.transform)
            {
                var meshFilter = chunk.GetComponent<MeshFilter>();
                AssetDatabase.CreateAsset(meshFilter.sharedMesh, $"Assets/{k_PrefabsFolderName}/Meshes/{originalObject.name}/{originalObject.name}_Chunk{i}.asset");
                i++;
            }

            //TODO: If UseVHCDColliders is enabled, colliders meshes should be saved too (may be not worth it).

            AssetDatabase.Refresh();

            // Save Fractured prefab
            GameObject fracturedPrefab = PrefabUtility.SaveAsPrefabAsset(fracturedObject, $"Assets/{k_PrefabsFolderName}/Prefabs/{originalObject.name}_Fractured.prefab");

            GameObject instantiated = PrefabUtility.InstantiatePrefab(fracturedPrefab) as GameObject;
            instantiated.transform.SetPositionAndRotation(originalObject.transform.position, originalObject.transform.rotation);
            DestroyImmediate(fracturedObject);

            originalObject.SetActive(true);
        }
    }
}
