using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UserInput : MonoBehaviour
{   
    public float rotationSpeed = 100f; // Rotation speed
    public float movementSpeed = 0.2f; // Movement speed
    public float defaultZoom;

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

    // List of saved views (each saved view stores position, rotation, and field of view)
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
        lineRenderer = GameObject.Find("Anchor").GetComponent<LineRenderer>();
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

            Vector3 selectedPartCenter = selectedPart.GetComponent<MeshRenderer>().bounds.center;

            // Set the start and end points
            lineRenderer.SetPosition(0, selectedPartCenter); // Set the start point (index 0)
            lineRenderer.SetPosition(1, lineRenderer.gameObject.transform.position); // Set the start point (index 0)
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
        Zoom = Mathf.Clamp(Zoom, 0.5f, 10f); // Ensure Zoom is within the valid range
        CameraComponent.orthographicSize = Zoom;

        // Gradually move the tip closer to x = 0 based on zoom level
        Vector3 newTipPosition = Tip.transform.position;
        newTipPosition.x = Mathf.Lerp(newTipPosition.x - Zoom, 1f, 0f); // Gradual transition towards x = 0

        // Apply the new position to the Tip
        Tip.transform.position = newTipPosition;

        // Scale the Tip based on the zoom level
        float scaleFactor = Mathf.Lerp(0.1f, 1.0f, 1f / Zoom); // Adjust the scale based on the zoom level
        Tip.transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
    }

    public void ZoomOut()
    {
        Zoom++;
        Zoom = Mathf.Clamp(Zoom, 0.5f, 10f); // Ensure Zoom is within the valid range
        CameraComponent.orthographicSize = Zoom;

        // Gradually move the tip farther from x = 0 as Zoom increases
        Vector3 newTipPosition = Tip.transform.position;
        newTipPosition.x = Mathf.Lerp(newTipPosition.x + Zoom, 1f, 0f); // Move farther from 0 as zoom increases

        // Apply the new position to the Tip
        Tip.transform.position = newTipPosition;

        // Scale the Tip based on the zoom level
        float scaleFactor = Mathf.Lerp(1f / Zoom, 1f / Zoom, 1f / Zoom); // Adjust the scale based on the zoom level
        Tip.transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);


        // MAP DISTANCE AND SCALE BETWEEN 0-5 and 0-1
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
        // Reset the camera's field of view to the default zoom
        Zoom = defaultZoom;
        CameraComponent.fieldOfView = Zoom;

        // Reset the model's position to the origin (0, 0, 0)
        Model.transform.position = Vector3.zero;

        // Reset the model's rotation to the default rotation (no rotation)
        Model.transform.rotation = Quaternion.identity;
    }

    public GameObject getCurrentPart()
    {
        return selectedPart;
    }

    // Method to save the current view (position, rotation, and field of view)
    public void SaveView()
    {
        // Save the current position, rotation, and camera's field of view
        SavedView view = new SavedView(Model.transform.position, Model.transform.rotation, CameraComponent.fieldOfView);

        // Add the view to the list of saved views
        SavedViews.Add(view);
    }

    // Method to apply a saved view (position, rotation, and field of view)
    public void OpenSavedView(int index)
    {        
        index--;
        if (index >= 0 && index < SavedViews.Count)
        {            
            SavedView savedView = SavedViews[index];

            // Apply the saved position, rotation, and field of view
            Model.transform.position = savedView.Position;
            Model.transform.rotation = savedView.Rotation;
            CameraComponent.fieldOfView = savedView.FieldOfView;
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

            if(selectedPart != null && selectedPart == hitObject)
            {
                if(selectedPart.GetComponent<MeshRenderer>().material = SelectedMaterial)
                {
                    selectedPart.GetComponent<MeshRenderer>().material = DefaultMaterial;
                }
                selectedPart = null;
            }
            else
            {
                selectedPart = hitObject;

                for(int i = 0; i < Model.transform.GetChild(0).childCount; i++)
                {
                    Model.transform.GetChild(0).GetChild(i).GetComponent<MeshRenderer>().material = DefaultMaterial;
                }
                selectedPart.GetComponent<MeshRenderer>().material = SelectedMaterial;

                if(uiScript.Opacities.ContainsKey(selectedPart))
                {
                    uiScript.opacitySlider.value = uiScript.Opacities[selectedPart];
                }
            }
        }
    }
}

// Class to store the position, rotation, and field of view
[System.Serializable]
public class SavedView
{
    public Vector3 Position;
    public Quaternion Rotation;
    public float FieldOfView;

    // Constructor to initialize position, rotation, and field of view
    public SavedView(Vector3 position, Quaternion rotation, float fieldOfView)
    {
        Position = position;
        Rotation = rotation;
        FieldOfView = fieldOfView;
    }
}


