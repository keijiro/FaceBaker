using UnityEditor;
using UnityEngine;
using UnityEngine.XR.iOS;

namespace FaceBaker
{
    public class BakeToBlendShape : MonoBehaviour
    {
        #region Editable attributes

        [SerializeField, HideInInspector] Material _defaultMaterial;

        #endregion

        #region Private objects

        UnityARSessionNativeInterface _session;

        Mesh _displayMesh;
        Mesh _outputMesh;
        int _outputCount;

        #endregion

        #region MonoBehaviour implementation

        void Start()
        {
            _session = UnityARSessionNativeInterface.GetARSessionNativeInterface();
            _session.RunWithConfig(new ARKitFaceTrackingConfiguration());

			UnityARSessionNativeInterface.ARFaceAnchorAddedEvent += FaceAdded;
			UnityARSessionNativeInterface.ARFaceAnchorUpdatedEvent += FaceUpdated;
			UnityARSessionNativeInterface.ARFaceAnchorRemovedEvent += FaceRemoved;
        }

        void OnDestroy()
        {
            if (_displayMesh != null) Destroy(_displayMesh);
            if (_outputMesh != null) Destroy(_outputMesh);
        }

        void Update()
        {
            if (_displayMesh == null) return;

            Graphics.DrawMesh(
                _displayMesh, transform.localToWorldMatrix,
                _defaultMaterial, gameObject.layer
            );

            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (_outputMesh == null)
                    StartRecording();
                else
                    AddBlendShape();
            }

            if (Input.GetKeyDown(KeyCode.Return))
            {
                if (_outputMesh != null)
                    FinishRecording();
            }
        }

        #endregion

        #region ARKit Delegates

        void FaceAdded(ARFaceAnchor anchorData)
        {
            if (_displayMesh != null) Destroy(_displayMesh);

            _displayMesh = new Mesh();

            FaceUpdated(anchorData);
        }

        void FaceUpdated(ARFaceAnchor anchorData)
        {
            if (_displayMesh == null) return;

            transform.localPosition = UnityARMatrixOps.GetPosition(anchorData.transform);
            transform.localRotation = UnityARMatrixOps.GetRotation(anchorData.transform);

            _displayMesh.vertices = anchorData.faceGeometry.vertices;
            _displayMesh.uv = anchorData.faceGeometry.textureCoordinates;
            _displayMesh.triangles = anchorData.faceGeometry.triangleIndices;

            _displayMesh.RecalculateBounds();
            _displayMesh.RecalculateNormals();
        }

        void FaceRemoved(ARFaceAnchor anchorData)
        {
            if (_displayMesh != null)
            {
                Destroy(_displayMesh);
                _displayMesh = null;
            }
        }

        #endregion

        #region Blend shape operations

        void StartRecording()
        {
            _outputMesh = new Mesh();

            _outputMesh.vertices = _displayMesh.vertices;
            _outputMesh.uv = _displayMesh.uv;
            _outputMesh.triangles = _displayMesh.triangles;

            _outputMesh.RecalculateBounds();
            _outputMesh.RecalculateNormals();

            _outputCount = 1;
        }

        void AddBlendShape()
        {
            var original = _outputMesh.vertices;
            var delta = _displayMesh.vertices;

            for (var i = 0; i < original.Length; i++)
                delta[i] -= original[i];

            _outputMesh.AddBlendShapeFrame(
                "Shape" + _outputCount, 100,
                delta, null, null
            );

            _outputCount++;
        }

        void FinishRecording()
        {
            var path = AssetDatabase.GenerateUniqueAssetPath("Assets/Face.asset");
            AssetDatabase.CreateAsset(_outputMesh, path);
            AssetDatabase.SaveAssets();
            _outputMesh = null;
        }

        #endregion
    }
}
