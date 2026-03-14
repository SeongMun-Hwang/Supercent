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

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (joystick == null) return;

        Vector2 input = joystick.InputDirection;
        Vector3 moveDirection = new Vector3(input.x, 0, input.y);

        if (moveDirection.magnitude > 0.1f)
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
