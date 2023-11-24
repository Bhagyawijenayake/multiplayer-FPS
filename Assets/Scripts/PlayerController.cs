using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerController : MonoBehaviourPunCallbacks
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
    // public float timeBetweenShots;
    private float shotCounter;
    public float muzzleDisplayTime;
    private float muzzleDisplayCounter;

    public float maxHeat = 10f, /*heatPerShot = 1f,*/ coolRate = 4f, overheatCoolRate = 5f;
    private float heatCounter;
    private bool overHeated;
    public Gun[] allGuns;
    private int selectedGun;

    public GameObject playerHitImpact;

    // Start is called before the first frame update
    void Start()
    {
        // cursor is locked to the center of the screen
        Cursor.lockState = CursorLockMode.Locked;

        // Get the camera component
        cam = Camera.main;

        // timeBetweenShots = 0.1f;

        muzzleDisplayTime = 1 / 60f;

        UIController.instance.weaponTempSlider.maxValue = maxHeat;

        // Switch to the first gun in the array
        SwitchGun();

        // Transform newTrans = SpawnManager.instance.getSpawnPoint();
        // transform.position = newTrans.position;
        // transform.rotation = newTrans.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        if (photonView.IsMine)
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

            allGuns[selectedGun].muzzleFlash.SetActive(false);

            if (allGuns[selectedGun].muzzleFlash.activeInHierarchy)
            {
                muzzleDisplayCounter -= Time.deltaTime;
                if (muzzleDisplayCounter <= 0)
                {
                    allGuns[selectedGun].muzzleFlash.SetActive(false);
                }
            }

            if (!overHeated)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    shoot();
                }

                if (Input.GetMouseButton(0) && allGuns[selectedGun].isAutomatic)
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
                    UIController.instance.overheatedmessage.gameObject.SetActive(false);
                }
            }

            if (heatCounter < 0)
            {
                heatCounter = 0f;
            }

            UIController.instance.weaponTempSlider.value = heatCounter;


            if (Input.GetAxis("Mouse ScrollWheel") > 0f)
            {
                selectedGun++;
                if (selectedGun >= allGuns.Length)
                {
                    selectedGun = 0;
                }

                SwitchGun();

            }
            else if (Input.GetAxis("Mouse ScrollWheel") < 0f)
            {
                selectedGun--;
                if (selectedGun < 0)
                {
                    selectedGun = allGuns.Length - 1;
                }

                SwitchGun();
            }

            for (int i = 0; i < allGuns.Length; i++)
            {
                if (Input.GetKeyDown((i + 1).ToString()))
                {
                    selectedGun = i;
                    SwitchGun();
                }
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
    }

    private void shoot()
    {

        Ray ray = cam.ViewportPointToRay(new Vector3(.5f, .5f, 0));
        ray.origin = cam.transform.position;


        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Debug.Log("I hit " + hit.collider.gameObject.name);

            if (hit.collider.gameObject.CompareTag("Player"))
            {
                Debug.Log("I hit " + hit.collider.gameObject.GetPhotonView().Owner.NickName);
                PhotonNetwork.Instantiate(playerHitImpact.name, hit.point, Quaternion.identity);
            }
            else
            {

                GameObject bulletImpactObject = Instantiate(bulletImpact, hit.point + (hit.normal * .002f), Quaternion.LookRotation(hit.normal, Vector3.up));
                Destroy(bulletImpactObject, 5f);
            }
        }
        shotCounter = allGuns[selectedGun].timeBetweenShots;

        Debug.Log(heatCounter);

        heatCounter += allGuns[selectedGun].heatPerShot;

        if (heatCounter >= maxHeat)
        {
            heatCounter = maxHeat;
            overHeated = true;

            UIController.instance.overheatedmessage.gameObject.SetActive(true);
        }

        allGuns[selectedGun].muzzleFlash.SetActive(true);
        muzzleDisplayCounter = muzzleDisplayTime;
    }

    // LateUpdate is called after Update
    private void LateUpdate()
    {
        if (photonView.IsMine)
        {
            // Set the camera's position to the player's position
            cam.transform.position = viewPoint.position;

            // Set the camera's rotation to the player's rotation
            cam.transform.rotation = viewPoint.rotation;
        }
    }

    public void SwitchGun()
    {
        foreach (Gun gun in allGuns)
        {
            gun.gameObject.SetActive(false);
        }

        allGuns[selectedGun].gameObject.SetActive(true);

        allGuns[selectedGun].muzzleFlash.SetActive(false);

    }
}