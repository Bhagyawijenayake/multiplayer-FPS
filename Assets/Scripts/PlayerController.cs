using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Transform viewPoint;
    public float mouseSensitivity = 1f;
    private float verticalRotStore;
    private Vector2 mouseInput;
    public bool invertLook;
    public float moveSpeed = 5f, runSpeed = 8f;
    private float activeMoveSpeed;
    private Vector3 moveDir, movement;

    public CharacterController charCon;

    private Camera cam;

    private float yVel;

    public float jumpForce = 12f, gravityMod = 2.5f;

    public Transform groundCheckPoint;
    private bool isGrounded;
    public LayerMask groundLayers;

    public GameObject bulletImpact;
    public float timeBetweenShots;
    private float shotCounter;

    public float maxHeat = 10f, heatPerShot = 1f, coolRate = 4f, overheatCoolRate = 5f;
    private float heatCounter;
    private bool overHeated;

    // Start is called before the first frame update
    void Start()
    {
        // cursor is locked to the center of the screen
        Cursor.lockState = CursorLockMode.Locked;

        // Get the camera component
        cam = Camera.main;

        timeBetweenShots = 0.1f;
    }

    // Update is called once per frame
    void Update()
    {
        // Get the mouse input (how much the mouse moved) in the X and Y directions
        mouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * mouseSensitivity;

        // Update the rotation of the player based on the mouse input
        // Only rotate around the Y-axis (left and right)
        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y + mouseInput.x, transform.rotation.eulerAngles.z);

        verticalRotStore += mouseInput.y;
        verticalRotStore = Mathf.Clamp(verticalRotStore, -60f, 60f);

        // Check if the look controls are inverted
        if (invertLook)
        {
            // If they are, rotate the viewpoint based on the vertical rotation store
            viewPoint.rotation = Quaternion.Euler(verticalRotStore, viewPoint.rotation.eulerAngles.y, viewPoint.rotation.eulerAngles.z);
        }
        else
        {
            // If they're not, rotate the viewpoint in the opposite direction
            viewPoint.rotation = Quaternion.Euler(-verticalRotStore, viewPoint.rotation.eulerAngles.y, viewPoint.rotation.eulerAngles.z);
        }

        moveDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));

        if (Input.GetKey(KeyCode.LeftShift))
        {
            activeMoveSpeed = runSpeed;
        }
        else
        {
            activeMoveSpeed = moveSpeed;
        }

        float yVel = movement.y;

        // Calculate the player's movement direction by adding the forward movement (transform.forward * moveDir.z) and the rightward movement (transform.right * moveDir.x). The result is then normalized to ensure the movement speed stays consistent regardless of the input direction.
        movement = ((transform.forward * moveDir.z) + (transform.right * moveDir.x)).normalized * activeMoveSpeed;
        movement.y = yVel;

        if (charCon.isGrounded)
        {
            movement.y = 0f;
        }

        // Check if the player is on the ground
        isGrounded = Physics.Raycast(groundCheckPoint.position, Vector3.down, .25f, groundLayers);
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            movement.y = jumpForce;
        }

        //gravity
        movement.y += Physics.gravity.y * Time.deltaTime * gravityMod;

        charCon.Move(movement * Time.deltaTime);

        if (!overHeated)
        {
            if (Input.GetMouseButtonDown(0))
            {
                shoot();
            }

            if (Input.GetMouseButton(0))
            {
                shotCounter -= Time.deltaTime;
                Debug.Log(shotCounter);
                if (shotCounter <= 0)
                {
                    shoot();
                }
            }

            heatCounter -= coolRate * Time.deltaTime;

        }
        else
        {
            heatCounter -= overheatCoolRate * Time.deltaTime;
            if (heatCounter <= 0)
            {

                overHeated = false;
            }
        }

        if (heatCounter < 0)
        {
            heatCounter = 0f;
        }














        // Unlock the cursor if the player presses the escape key
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
        }
        else if (Cursor.lockState == CursorLockMode.None)
        {
            // Lock the cursor if the player clicks the left mouse button
            // 0 means the left mouse button
            if (Input.GetMouseButtonDown(0))
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }

    private void shoot()
    {

        Ray ray = cam.ViewportPointToRay(new Vector3(.5f, .5f, 0));
        ray.origin = cam.transform.position;


        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Debug.Log("I hit " + hit.collider.gameObject.name);

            GameObject bulletImpactObject = Instantiate(bulletImpact, hit.point + (hit.normal * .002f), Quaternion.LookRotation(hit.normal, Vector3.up));
            Destroy(bulletImpactObject, 5f);
        }
        shotCounter = timeBetweenShots;

        Debug.Log(heatCounter);

        heatCounter += heatPerShot;

        if (heatCounter >= maxHeat)
        {
            heatCounter = maxHeat;
            overHeated = true;
        }
    }

    // LateUpdate is called after Update
    private void LateUpdate()
    {
        // Set the camera's position to the player's position
        cam.transform.position = viewPoint.position;

        // Set the camera's rotation to the player's rotation
        cam.transform.rotation = viewPoint.rotation;
    }
}