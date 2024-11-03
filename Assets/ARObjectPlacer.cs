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
    private InputAction clickPositionAction; // For mouse clicks in the editor
    private NetworkRunner _networkRunner;

    void Awake()
    {
        arRaycastManager = GetComponent<ARRaycastManager>();
        _networkRunner = FindObjectOfType<NetworkRunner>();

        // Create and enable the touch action
        touchPositionAction = new InputAction("TouchPosition", binding: "<Touchscreen>/primaryTouch/position");
        touchPositionAction.Enable();

        // Create and enable the mouse click action (for testing in editor)
        clickPositionAction = new InputAction("ClickPosition", binding: "<Mouse>/position");
        clickPositionAction.Enable();
    }

    private void OnEnable()
    {
        arPlaneManager = GetComponent<ARPlaneManager>();

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
        if (arPlaneManager != null)
        {
            arPlaneManager.planesChanged -= PlanesChanged;
        }

        touchPositionAction.Disable();
        clickPositionAction.Disable();
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
        if (_networkRunner == null || !_networkRunner.IsRunning)
        {
            Debug.LogWarning("NetworkRunner is not running.");
            return;
        }

        Debug.Log("Update is running...");

#if UNITY_EDITOR
        // Use mouse click in editor
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 clickPosition = clickPositionAction.ReadValue<Vector2>();

            // Perform raycast to detect planes
            if (arRaycastManager.Raycast(clickPosition, hits, TrackableType.Planes))
            {
                Pose hitPose = hits[0].pose;
                Debug.Log("Raycast hit at position: " + hitPose.position + " with rotation: " + hitPose.rotation);

                // Spawn the object over the network
                NetworkObject placedObject = _networkRunner.Spawn(objectToPlacePrefab, hitPose.position, hitPose.rotation);
                placedObject.transform.SetParent(hits[0].trackable.transform);

                // Update touch count for testing purposes
                touchCount++;
                touchCountText.text = "Touches: " + touchCount;
            }
            else
            {
                Debug.Log("No plane detected at click position.");
            }
        }
#else
        // Use touch input on mobile
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            Vector2 touchPosition = touchPositionAction.ReadValue<Vector2>();
            Debug.Log("Touch detected: Position = " + touchPosition);

            touchCount++;
            touchCountText.text = "Touches: " + touchCount;

            if (arRaycastManager.Raycast(touchPosition, hits, TrackableType.Planes))
            {
                Pose hitPose = hits[0].pose;
                Debug.Log("Raycast hit at position: " + hitPose.position + " with rotation: " + hitPose.rotation);

                NetworkObject placedObject = _networkRunner.Spawn(objectToPlacePrefab, hitPose.position, hitPose.rotation);
                placedObject.transform.SetParent(hits[0].trackable.transform);
            }
            else
            {
                Debug.Log("No plane detected at touch position.");
            }
        }
#endif
    }
}
