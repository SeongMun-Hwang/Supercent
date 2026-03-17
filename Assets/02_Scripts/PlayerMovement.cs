using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private Joystick joystick;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;

    private CharacterController _characterController;
    private Transform _cameraTransform;
    private Animator _animator;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _animator = GetComponentInChildren<Animator>(); // 참조 추가

        if (Camera.main != null)
        {
            _cameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        if (joystick == null || _cameraTransform == null) return;

        Vector2 input = joystick.InputDirection;
        
        // Calculate camera-relative direction
        Vector3 forward = _cameraTransform.forward;
        Vector3 right = _cameraTransform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = (forward * input.y) + (right * input.x);
        float moveMagnitude = moveDirection.magnitude;

        // 애니메이터에 이동 값 전달 (0 ~ 1)
        if (_animator != null)
        {
            _animator.SetFloat("moveSpeed", moveMagnitude);
        }

        if (moveMagnitude > 0.1f)
        {
            // Move
            _characterController.Move(moveDirection * moveSpeed * Time.deltaTime);

            // Rotate
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        
        // Simple Gravity
        if (!_characterController.isGrounded)
        {
            _characterController.Move(Vector3.down * 9.81f * Time.deltaTime);
        }
    }
}
