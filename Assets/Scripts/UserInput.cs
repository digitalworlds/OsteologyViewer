using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UserInput : MonoBehaviour
{   
    public float rotationSpeed = 100f; // Rotation speed
    public float movementSpeed = 0.2f; // Movement speed
    public float defaultZoom;

    // Adjustable parameters for the tip's behavior
    public float tipMovementFactor = 1f; // Factor for how far the tip moves relative to zoom
    public float tipMinScale = 0.1f; // Minimum scale for the tip
    public float tipMaxScale = 1f;  // Maximum scale for the tip

    public string Name;

    public Material DefaultMaterial;
    public Material SelectedMaterial;

    private Vector3 lastMousePosition;
    private bool isRightClickPressed = false;
    private bool isMiddleClickPressed = false;

    private GameObject Model;
    private GameObject Camera;
    private Camera CameraComponent;
    private float Zoom;

    private GameObject selectedPart;
    private GameObject Tip;
    private LineRenderer lineRenderer;

    // List of saved views (each saved view stores position, rotation, and orthographic size)
    private List<SavedView> SavedViews = new List<SavedView>();

    private UIScript uiScript;

    public void Start()
    {
        Zoom = defaultZoom;

        Model = GameObject.Find("Model");
        Camera = GameObject.Find("Main Camera");
        CameraComponent = Camera.GetComponent<Camera>(); // Get the Camera component

        LoadModel(Name);
        selectedPart = null; 

        Tip = GameObject.Find("Tip");
        //lineRenderer = GameObject.Find("Anchor").GetComponent<LineRenderer>();
        Tip.SetActive(false);

        uiScript = GameObject.Find("OverlayUI").GetComponent<UIScript>();
    }

    public void Update()
    {
        //turn on tooltip
        if(selectedPart != null)
        {
            Tip.SetActive(true);
            Tip.transform.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text = selectedPart.name;

            // Vector3 selectedPartCenter = selectedPart.GetComponent<MeshRenderer>().bounds.center;

            // // Set the start and end points
            // lineRenderer.SetPosition(0, selectedPartCenter); // Set the start point (index 0)
            // lineRenderer.SetPosition(1, lineRenderer.gameObject.transform.position); // Set the start point (index 0)
        }
        else
        {
            Tip.SetActive(false);
        }

        // Check for middle mouse button press
        if (Input.GetMouseButtonDown(1)) // 1 refers to the right mouse button
        {
            isRightClickPressed = true;
            lastMousePosition = Input.mousePosition;
            Cursor.visible = false;
        }

        if (Input.GetMouseButtonUp(1)) // right mouse button released
        {
            isRightClickPressed = false;
            Cursor.visible = true;
        }

        if (Input.GetMouseButtonDown(2)) // 1 refers to the middle mouse button
        {
            isMiddleClickPressed = true;
            lastMousePosition = Input.mousePosition;
            Cursor.visible = false;
        }

        if (Input.GetMouseButtonUp(2)) // Middle mouse button released
        {
            isMiddleClickPressed = false;
            Cursor.visible = true;
        }

        if (Input.GetMouseButtonDown(0))
        {
            SelectPart();
        }

        float scrollInput = Input.GetAxis("Mouse ScrollWheel"); // Get scroll input
        if (scrollInput > 0f && Zoom !> 0f)
        {
            ZoomIn();
        }
        else if (scrollInput < 0f && Zoom !< 10f)
        {
            ZoomOut();
        }

        // If middle mouse is held down, update rotation or movement
        if (isRightClickPressed)
        {
            // Rotate the Model based on mouse movement
            RotateModel();
        }

        if(isMiddleClickPressed)
        {
            MoveModel();
        }
    }

    public void ZoomIn()
    {
        Zoom--;
        Zoom = Mathf.Clamp(Zoom, 0.5f, 10f); // Clamp Zoom within range
        CameraComponent.orthographicSize = Zoom;

        // // Move the tip relative to zoom level using tipMovementFactor
        // Vector3 newTipPosition = Tip.transform.position;
        // newTipPosition.x -= tipMovementFactor * Zoom;  // Adjust the movement distance based on zoom
        // Tip.transform.position = newTipPosition;

        // // Scale the Tip based on zoom level
        // float scaleFactor = Mathf.Lerp(tipMinScale, tipMaxScale, 1f / Zoom); // Scale the tip based on zoom
        // Tip.transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
    }

    public void ZoomOut()
    {
        Zoom++;
        Zoom = Mathf.Clamp(Zoom, 0.5f, 10f); // Clamp Zoom within range
        CameraComponent.orthographicSize = Zoom;

        // // Move the tip relative to zoom level using tipMovementFactor
        // Vector3 newTipPosition = Tip.transform.position;
        // newTipPosition.x += tipMovementFactor * Zoom;  // Adjust the movement distance based on zoom
        // Tip.transform.position = newTipPosition;

        // // Scale the Tip based on zoom level
        // float scaleFactor = Mathf.Lerp(tipMinScale, tipMaxScale, 1f / Zoom); // Scale the tip based on zoom
        // Tip.transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
    }

    void RotateModel()
    {
        // Get the difference between the current and last mouse position
        Vector3 mouseDelta = Input.mousePosition - lastMousePosition;

        // Calculate rotation based on mouse movement
        float rotX = mouseDelta.x * rotationSpeed * Time.deltaTime;
        float rotY = mouseDelta.y * rotationSpeed * Time.deltaTime;

        // Rotate the Model around the X and Y axes
        Model.transform.Rotate(Vector3.up, -rotX, Space.World); // Rotate around Y axis (horizontal mouse movement)
        Model.transform.Rotate(Vector3.right, rotY, Space.World); // Rotate around X axis (vertical mouse movement)

        // Update last mouse position for the next frame
        lastMousePosition = Input.mousePosition;
    }

    void MoveModel()
    {
        // Get mouse movement in screen space and translate the object accordingly
        Vector3 mouseDelta = Input.mousePosition - lastMousePosition;

        // Convert the mouse movement into world space (movement in 3D)
        Vector3 movement = new Vector3(mouseDelta.x, mouseDelta.y, 0) * (movementSpeed * Zoom) * Time.deltaTime;

        // Move the Model (here the movement is applied in local space)
        Model.transform.Translate(movement, Space.World);

        // Update last mouse position for next frame
        lastMousePosition = Input.mousePosition;
    }

    public void ResetView()
    {
        // Reset the camera's orthographic size to the default zoom
        Zoom = defaultZoom;
        CameraComponent.orthographicSize = Zoom;

        // Reset the model's position to the origin (0, 0, 0)
        Model.transform.position = Vector3.zero;

        // Reset the model's rotation to the default rotation (no rotation)
        Model.transform.rotation = Quaternion.identity;
    }

    public GameObject getCurrentPart()
    {
        return selectedPart;
    }

    // Method to save the current view (position, rotation, and orthographic size)
    public void SaveView()
    {
        // Save the current position, rotation, and camera's orthographic size
        SavedView view = new SavedView(Model.transform.position, Model.transform.rotation, CameraComponent.orthographicSize);

        // Add the view to the list of saved views
        SavedViews.Add(view);
    }

    public void createView(Vector3 Pos, Quaternion Rot, float OrthographicSize)
    {
        SavedView view = new SavedView(Pos, Rot, OrthographicSize);
        SavedViews.Add(view);
    }

    // Method to apply a saved view (position, rotation, and orthographic size)
    public void OpenSavedView(int index)
    {        
        index--;
        if (index >= 0 && index < SavedViews.Count)
        {            
            SavedView savedView = SavedViews[index];

            // Apply the saved position, rotation, and orthographic size
            Model.transform.position = savedView.Position;
            Model.transform.rotation = savedView.Rotation;
            CameraComponent.orthographicSize = savedView.OrthographicSize;
        }
    }

    public void LoadModel(string Name)
    {
        // Load the model prefab from the Resources folder
        GameObject modelPrefab = Resources.Load<GameObject>("Prefabs/" + Name + "/" + Name);

        // Instantiate the model at the origin with the prefab's rotation
        GameObject instantiatedModel = Instantiate(modelPrefab, new Vector3(0, 0, 0), modelPrefab.transform.rotation);

        // Set the parent of the instantiated model to 'Model'
        instantiatedModel.transform.SetParent(Model.transform);

        // Optionally, reset the local position if you want to keep it relative to the parent
        instantiatedModel.transform.localPosition = Vector3.zero;

        foreach (Transform child in instantiatedModel.transform)
        {
            // Add a MeshCollider to the child if it has a MeshFilter (which indicates it has a mesh)
            MeshFilter meshFilter = child.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                // Ensure the child doesn't already have a MeshCollider
                if (child.GetComponent<MeshCollider>() == null)
                {
                    // Add a MeshCollider to the child
                    child.gameObject.AddComponent<MeshCollider>();
                }
            }
        }
    }

    void SelectPart()
    {
        // Step 1: Get the mouse position on screen
        Vector3 mousePosition = Input.mousePosition;

        // Step 2: Convert mouse position to a ray from the camera to the world space
        Ray ray = Camera.GetComponent<Camera>().ScreenPointToRay(mousePosition);

        // Step 3: Perform the raycast
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            // Step 4: If we hit something, output the name of the object
            GameObject hitObject = hit.collider.gameObject;

            if (selectedPart != null && selectedPart == hitObject)
            {
                // Deselect the part if it's already selected
                if (selectedPart.GetComponent<MeshRenderer>().material == SelectedMaterial)
                {
                    selectedPart.GetComponent<MeshRenderer>().material = DefaultMaterial;
                    selectedPart = null;
                }
            }
            else
            {
                // Deselect the previous part if there's one selected
                if (selectedPart != null)
                {
                    selectedPart.GetComponent<MeshRenderer>().material = DefaultMaterial;
                    if (uiScript.Opacities.ContainsKey(selectedPart))
                    {
                        float storedOpacity = uiScript.Opacities[selectedPart];
                        // Apply the stored opacity to the model's material
                        Renderer modelRenderer = selectedPart.GetComponentInChildren<Renderer>();
                        Color currentColor = modelRenderer.material.color;
                        currentColor.a = storedOpacity;  // Set the alpha value from stored opacity
                        modelRenderer.material.color = currentColor;

                        // Update the slider to reflect the stored opacity
                        uiScript.opacitySlider.value = storedOpacity;
                    }
                    else
                    {
                        // If no opacity was set, you can default it to 1 (full opacity)
                        uiScript.opacitySlider.value = 1f;
                    }
                }

                selectedPart = hitObject;

                // Set the selected part's material to the selected one
                selectedPart.GetComponent<MeshRenderer>().material = SelectedMaterial;

                // Restore opacity from the Opacities dictionary if available
                if (uiScript.Opacities.ContainsKey(selectedPart))
                {
                    float storedOpacity = uiScript.Opacities[selectedPart];
                    // Apply the stored opacity to the model's material
                    Renderer modelRenderer = selectedPart.GetComponentInChildren<Renderer>();
                    Color currentColor = modelRenderer.material.color;
                    currentColor.a = storedOpacity;  // Set the alpha value from stored opacity
                    modelRenderer.material.color = currentColor;

                    // Update the slider to reflect the stored opacity
                    uiScript.opacitySlider.value = storedOpacity;
                }
                else
                {
                    // If no opacity was set, you can default it to 1 (full opacity)
                    uiScript.opacitySlider.value = 1f;
                }
            }
        }
    }
}

// Class to store the position, rotation, and orthographic size
[System.Serializable]
public class SavedView
{
    public Vector3 Position;
    public Quaternion Rotation;
    public float OrthographicSize;

    // Constructor to initialize position, rotation, and orthographic size
    public SavedView(Vector3 position, Quaternion rotation, float orthographicSize)
    {
        Position = position;
        Rotation = rotation;
        OrthographicSize = orthographicSize;
    }
}


