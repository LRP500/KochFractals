using AudioVisualization;
using UnityEngine;

public class RotateOnAmplitude : MonoBehaviour
{
    [SerializeField]
    private AudioPeer _audioPeer = null;

    [SerializeField]
    private Vector3 _rotationAmplitude = default;

    [SerializeField]
    private float _rotationSpeed = 1f;

    private float _rotationY = 0f;

    private void LateUpdate()
    {
        Vector3 eulerAngles = transform.localEulerAngles;
        _rotationY = Mathf.PingPong(Time.time * _rotationSpeed, _rotationAmplitude.y);
        eulerAngles.y = _rotationY - (_rotationAmplitude.y / 2);
        transform.localEulerAngles = eulerAngles;
    }
}
