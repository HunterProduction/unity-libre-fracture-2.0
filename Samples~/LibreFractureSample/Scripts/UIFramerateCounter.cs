using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class UIFramerateCounter : MonoBehaviour
{
    private TextMeshProUGUI _textReference;
    private float _deltaTime;

    private void Awake()
    {
        _textReference = GetComponent<TextMeshProUGUI>();   
    }

    // Update is called once per frame
    void Update()
    {
        _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;

        var fps = 1f / _deltaTime;
        _textReference.text = $"{fps:0.} FPS\n";
    }
}
