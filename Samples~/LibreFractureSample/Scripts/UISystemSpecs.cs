using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class UISystemSpecs : MonoBehaviour
{
    private TextMeshProUGUI _textReference;

    private float _deltaTime, _memoryAmount;
    private string _gpuModel, _cpuModel;

    private void Awake()
    {
        _textReference = GetComponent<TextMeshProUGUI>();

        _gpuModel = SystemInfo.graphicsDeviceName;
        _cpuModel = SystemInfo.processorModel;
        _memoryAmount = SystemInfo.systemMemorySize;

        _textReference.text = $"GPU:\t{_gpuModel}\n" +
            $"CPU:\t{_cpuModel}\n" +
            $"RAM:\t{_memoryAmount:0.}MB";
    }
}
