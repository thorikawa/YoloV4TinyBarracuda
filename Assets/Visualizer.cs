using UnityEngine;
using UnityEngine.UI;
using Klak.TestTools;
using YoloV4Tiny;
using UnityEngine.XR.ARFoundation;
using UnityEngine.Rendering;

sealed class Visualizer : MonoBehaviour
{
    #region Editable attributes

    [SerializeField, Range(0, 1)] float _threshold = 0.5f;
    [SerializeField] ResourceSet _resources = null;
    [SerializeField] RawImage _preview = null;
    [SerializeField] Marker _markerPrefab = null;
    [SerializeField] ARCameraBackground m_ARCameraBackground = null;
    [SerializeField] RenderTexture m_RenderTexture;


    #endregion

    #region Internal objects

    ObjectDetector _detector;
    Marker[] _markers = new Marker[50];

    #endregion

    #region MonoBehaviour implementation

    void Start()
    {
        _detector = new ObjectDetector(_resources);
        for (var i = 0; i < _markers.Length; i++)
            _markers[i] = Instantiate(_markerPrefab, _preview.transform);
    }

    void OnDisable()
      => _detector.Dispose();

    void OnDestroy()
    {
        for (var i = 0; i < _markers.Length; i++) Destroy(_markers[i]);
    }

    void Update()
    {
        GetCameraImage();

        _detector.ProcessImage(m_RenderTexture, _threshold);

        var i = 0;
        foreach (var d in _detector.Detections)
        {
            if (i == _markers.Length) break;
            _markers[i++].SetAttributes(d);
        }

        for (; i < _markers.Length; i++) _markers[i].Hide();

        _preview.texture = m_RenderTexture;
    }

    void GetCameraImage()
    {
        var commandBuffer = new CommandBuffer();
        commandBuffer.name = "AR Camera Background Blit Pass";

        // Get a reference to the AR Camera Background's main texture
        // We will copy this texture into our chosen render texture
        var texture = !m_ARCameraBackground.material.HasProperty("_MainTex") ?
            null : m_ARCameraBackground.material.GetTexture("_MainTex");
        Debug.Log($"texture is null?: {texture == null}");

        // if (m_RenderTexture == null)
        // {
        //     m_RenderTexture = new RenderTexture()
        // }

        // Save references to the active render target before we overwrite it
        var colorBuffer = Graphics.activeColorBuffer;
        var depthBuffer = Graphics.activeDepthBuffer;

        // Set Unity's render target to our render texture
        Graphics.SetRenderTarget(m_RenderTexture);

        // Clear the render target before we render new pixels into it
        commandBuffer.ClearRenderTarget(true, false, Color.clear);

        // Blit the AR Camera Background into the render target
        commandBuffer.Blit(
            texture,
            BuiltinRenderTextureType.CurrentActive,
            m_ARCameraBackground.material);

        // Execute the command buffer
        Graphics.ExecuteCommandBuffer(commandBuffer);

        // Set Unity's render target back to its previous value
        Graphics.SetRenderTarget(colorBuffer, depthBuffer);
    }

    #endregion
}
