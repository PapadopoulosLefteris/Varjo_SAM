using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]

public class SC_FPSController : MonoBehaviour
{
    
    public float walkingSpeed = 7.5f;
    public float runningSpeed = 11.5f;
    public float jumpSpeed = 8.0f;
    public float gravity = 20.0f;
    public Camera captureCamera;
    public float lookSpeed = 2.0f;
    public float lookXLimit = 45.0f;

  
    CharacterController characterController;
    public InputActionAsset inputActions; // Reference to the input actions asset
    private InputAction moveAction; // The move action itself
    private InputAction rotationAction;
    Vector3 moveDirection = Vector3.zero;

    [HideInInspector]
    public bool canMove = true;
    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            characterController = gameObject.AddComponent<CharacterController>();
            characterController.height = 0f;
            characterController.center = Vector3.zero;
        }
        var playerActions = inputActions.FindActionMap("Player"); // Get the 'Player' action map
        moveAction = playerActions.FindAction("Move");
        rotationAction = playerActions.FindAction("Rotation");
    }
        // Start is called before the first frame update
        void Start()
    {
        characterController = GetComponent<CharacterController>();

        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
    }
   
    // Update is called once per frame
    void Update()
    {

        //// We are grounded, so recalculate move direction based on axes
        Vector3 forward = captureCamera.transform.TransformDirection(Vector3.forward);
        Vector3 right = captureCamera.transform.TransformDirection(Vector3.right);

        Vector2 input = moveAction.ReadValue<Vector2>();

        //Quaternion controllerRotation = rotationAction.ReadValue<Quaternion>(); // Get rotation
        //Vector3 forward_rot = controllerRotation * Vector3.forward;
        //Vector3 screenPoint = captureCamera.WorldToScreenPoint(forward_rot * 10f);
        //float x = screenPoint.x / Screen.width * 1024;
        //float y = screenPoint.y / Screen.height * 1024;
        //Debug.Log($"x:{x}, y:{y}");
        // Convert input to world direction
        Vector3 move = forward * input.y + right * input.x;
        move.y = 0f; // Prevent moving up/down

        characterController.Move(move * Time.deltaTime*3);
    }


}
