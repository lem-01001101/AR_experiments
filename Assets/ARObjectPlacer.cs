using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;

public class ARObjectPlacer : NetworkBehaviour
{
    public NetworkPrefabRef objectToPlacePrefab;
    public TextMeshProUGUI touchCountText;
    private ARRaycastManager arRaycastManager;
    private ARPlaneManager arPlaneManager;
    static List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private int touchCount = 0;

    private InputAction touchPositionAction;
    private NetworkRunner _networkRunner;

    void Awake()
    {
        arRaycastManager = GetComponent<ARRaycastManager>();
        _networkRunner = FindObjectOfType<NetworkRunner>();

        // Create and enable the touch action
        touchPositionAction = new InputAction("TouchPosition", binding: "<Touchscreen>/primaryTouch/position");
        touchPositionAction.Enable();
    }

    private void OnEnable()
    {
        // Assign ARPlaneManager from the XR Origin (make sure XR Origin has the ARPlaneManager attached)
        arPlaneManager = GetComponent<ARPlaneManager>();

        // Check if arPlaneManager is not null to avoid errors
        if (arPlaneManager != null)
        {
            arPlaneManager.planesChanged += PlanesChanged;
        }
        else
        {
            Debug.LogError("ARPlaneManager component is missing from XR Origin. Please add ARPlaneManager to XR Origin.");
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from event to avoid memory leaks
        if (arPlaneManager != null)
        {
            arPlaneManager.planesChanged -= PlanesChanged;
        }

        touchPositionAction.Disable();
    }

    private void PlanesChanged(ARPlanesChangedEventArgs args)
    {
        if (args.added != null && args.added.Count > 0)
        {
            Debug.Log("Planes detected: " + args.added.Count);
        }
    }

    void Update()
    {
        // Ensure the NetworkRunner is running
        if (_networkRunner == null || !_networkRunner.IsRunning)
        {
            Debug.LogWarning("NetworkRunner is not running.");
            return;
        }

        Debug.Log("Update is running...");

        // Check if there are any active touches
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            Vector2 touchPosition = touchPositionAction.ReadValue<Vector2>();
            Debug.Log("Touch detected: Position = " + touchPosition);

            // Track touch count regardless of raycast result
            touchCount++;
            touchCountText.text = "Touches: " + touchCount;

            // Perform raycast against all types of planes
            if (arRaycastManager.Raycast(touchPosition, hits, TrackableType.Planes))
            {
                Pose hitPose = hits[0].pose;
                Debug.Log("Raycast hit at position: " + hitPose.position + " with rotation: " + hitPose.rotation);

                // Spawn the object over the network
                NetworkObject placedObject = _networkRunner.Spawn(objectToPlacePrefab, hitPose.position, hitPose.rotation);

                // Optionally, set the parent to the detected plane's transform (ensure synchronization)
                placedObject.transform.SetParent(hits[0].trackable.transform);
            }
            else
            {
                Debug.Log("No plane detected at touch position.");
            }
        }
    }
}
