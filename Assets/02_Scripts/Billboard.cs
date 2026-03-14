using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Transform _mainCameraTransform;

    private void Start()
    {
        if (Camera.main != null)
        {
            _mainCameraTransform = Camera.main.transform;
        }
    }

    private void LateUpdate()
    {
        if (_mainCameraTransform == null) return;

        // 카메라의 회전값과 동일하게 맞춤 (빌보드 효과)
        transform.LookAt(transform.position + _mainCameraTransform.rotation * Vector3.forward,
                         _mainCameraTransform.rotation * Vector3.up);
    }
}
