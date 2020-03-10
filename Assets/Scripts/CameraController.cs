using AudioVisualization;
using UnityEngine;

namespace KochFractals
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField]
        private Camera _mainCamera = null;

        [SerializeField]
        private AudioPeer _audioPeer = null;

        [SerializeField]
        private Vector3 _rotateSpeed = default;

        private void Update()
        {
            _mainCamera.transform.LookAt(transform);

            float amplitude = Time.deltaTime * _audioPeer.AmplitudeBuffer;
            transform.Rotate(_rotateSpeed.x * amplitude, _rotateSpeed.y * amplitude, _rotateSpeed.z * amplitude);
        }
    }
}
