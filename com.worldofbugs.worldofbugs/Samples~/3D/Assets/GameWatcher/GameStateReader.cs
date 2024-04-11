using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class GameStateReader : MonoBehaviour
{
    private GameObject nearClippingPlane;
    private bool GameWatcherActive;
    private bool GameHelperActive;
    private string BugDataDirectory;
    
    [SerializeField] GameObject playerPositionInput = null;
    [SerializeField] GameObject playerCameraInput = null;
    [SerializeField] Button playerTeleportButton = null;
    [SerializeField] Text overlayText = null;
    [SerializeField] GameObject player = null;
    [SerializeField] Camera camera = null;

    // Start is called before the first frame update
    void Start()
    {
        // Creating Near Clipping Plane for tool use
        nearClippingPlane = CreateNearClippingPlane(camera, CalculateNearClipPlaneSize(camera));
        nearClippingPlane.gameObject.SetActive(false);

        // Add Near Clipping Plane to existing camera
        nearClippingPlane.transform.parent = camera.transform;

        // Setting variables
        overlayText = GameObject.FindGameObjectWithTag("GameWatcherOverlay").GetComponent<Text>();

        // Add listeners to buttons
        playerTeleportButton.onClick.AddListener(OnPlayerTeleportButtonClick);

        InvokeRepeating("WatchGame", 0.0f, 1.0f);
    }

    private void Update()
    {
        // Key Press activate and deactivate
        if (Input.GetKeyDown(KeyCode.Minus))
        {
            ToggleGameWatcher();
            if(GameHelperActive)
                ToggleGameHelper();

            // Create capture directory
            BugDataDirectory = DateTime.Now.ToString("yyyy-dd-M_HH-mm-ss");
            Directory.CreateDirectory(BugDataDirectory);
        }

        if (Input.GetKeyDown(KeyCode.Equals))
        {
            if (GameWatcherActive)
                ToggleGameWatcher();
            ToggleGameHelper();
        }
    }

    private void ToggleGameWatcher()
    {
        GameWatcherActive = !GameWatcherActive;
        nearClippingPlane.gameObject.SetActive(!nearClippingPlane.gameObject.active);
        var overlay = GameObject.FindGameObjectWithTag("GameWatcherOverlay").GetComponent<Text>();
        overlay.text = GameWatcherActive ? "GameWatcher is enabled, press - to disable!" : "Press - to enable GameWatcher.";
    }

    private void ToggleGameHelper()
    {
        GameHelperActive = !GameHelperActive;
        overlayText.text = GameHelperActive ? "GameHelper is enabled, press = to disable!" : "Press = to enable GameHelper.";

        // Activate Near Clipping Plane to facilitate bug detection
        nearClippingPlane.gameObject.SetActive(!nearClippingPlane.gameObject.active);

        // Activate teleporting
        playerTeleportButton.gameObject.SetActive(!playerTeleportButton.gameObject.active);
        
        // Player position
        playerPositionInput.gameObject.SetActive(!playerPositionInput.gameObject.active);
        
        // Player camera
        playerCameraInput.gameObject.SetActive(!playerCameraInput.gameObject.active);
    }

    public void OnPlayerTeleportButtonClick()
    {
        // Disabling conflicting Player Controller
        player.GetComponent<CharacterController>().enabled = false;
        player.GetComponent<PlayerController>().enabled = false;

        // Enter player position and go there
        var playerPosition = GameObject.Find("PlayerPositionInputText").GetComponentInChildren<Text>().text.Split(new char[] { ',' });
        player.transform.position = new Vector3(float.Parse(playerPosition[0]), float.Parse(playerPosition[1]), float.Parse(playerPosition[2]));

        // Enter camera direction and point to that
        var cameraPosition = GameObject.Find("CameraPositionInputText").GetComponentInChildren<Text>().text.Split(new char[] { ',' });
        camera.transform.forward = new Vector3(float.Parse(cameraPosition[0]), float.Parse(cameraPosition[1]), float.Parse(cameraPosition[2]));

        // Enabling conflicting Player Controller
        player.GetComponent<CharacterController>().enabled = true;
        player.GetComponent<PlayerController>().enabled = true;
    }

    private void WatchGame()
    {
        if (GameWatcherActive)
        {
            // Extract coordinates
            if (nearClippingPlane.GetComponent<CollisionChecker>().Colliding)
            {
                var camera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
                var cameraDirection = camera.transform.forward;
                var playerPosition = GameObject.FindGameObjectWithTag("Player").transform.position;

                var debugData = $"Debug data at {DateTime.Now}:\nCamera direction: {cameraDirection}\nPlayer position: {playerPosition}\n";
                File.AppendAllText(Path.Combine(BugDataDirectory, BugDataDirectory + "_DebugData.log"), debugData);
                
                // Capture screenshot
                UnityEngine.ScreenCapture.CaptureScreenshot(Path.Combine(Directory.GetCurrentDirectory(),BugDataDirectory, DateTime.Now.ToString("yyyy-dd-M_HH-mm-ss") + "_ScreenCapture.png"));
            }
        }
    }

    private GameObject CreateNearClippingPlane(Camera camera, Vector2 size)
    {
        // Create the plane GameObject
        GameObject planeObject = new GameObject("NearClippingPlane");

        // Position and rotate the plane to match the camera's near clipping plane
        planeObject.transform.position = camera.transform.position + camera.transform.forward * camera.nearClipPlane;
        planeObject.transform.rotation = camera.transform.rotation;

        // Add a Box Collider to represent the near clipping plane
        BoxCollider boxCollider = planeObject.AddComponent<BoxCollider>();
        boxCollider.size = new Vector3(size.x, size.y, 0.001f); // Adjust the Z size as needed
        boxCollider.isTrigger = true;

        // Add Collision checker
        planeObject.AddComponent<CollisionChecker>();

        return planeObject;
    }

    private Vector2 CalculateNearClipPlaneSize(Camera camera)
    {
        // Get the FOV of the camera in degrees
        float fov = camera.fieldOfView;

        // Get the distance from the camera to the near clipping plane
        float nearClipDistance = camera.nearClipPlane;

        // Full Width is the full width (or height) of the near clipping plane.
        float fullWidth = 2 * Mathf.Tan(Mathf.Deg2Rad * (fov / 2.0f)) * nearClipDistance;

        // Create a Vector2 with the width and height
        return new Vector2(1, fullWidth);
    }
}
