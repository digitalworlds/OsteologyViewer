using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserInput : MonoBehaviour
{   
    public float rotationSpeed = 100f; // Rotation speed
    public float movementSpeed = 0.2f; // Movement speed
    public float defaultZoom;

    public string Name;

    private Vector3 lastMousePosition;
    private bool isMiddleClickPressed = false;

    private GameObject Model;
    private GameObject Camera;
    private Camera CameraComponent;
    private float Zoom;

    // List of saved views (each saved view stores position, rotation, and field of view)
    private List<SavedView> SavedViews = new List<SavedView>();

    void Start()
    {
        Zoom = defaultZoom;

        Model = GameObject.Find("Model");
        Camera = GameObject.Find("Main Camera");
        CameraComponent = Camera.GetComponent<Camera>(); // Get the Camera component

        LoadModel(Name);
    }

    void Update()
    {
        // Check for middle mouse button press
        if (Input.GetMouseButtonDown(1)) // 1 refers to the middle mouse button
        {
            isMiddleClickPressed = true;
            lastMousePosition = Input.mousePosition;
            Cursor.visible = false;
        }

        if (Input.GetMouseButtonUp(1)) // Middle mouse button released
        {
            isMiddleClickPressed = false;
            Cursor.visible = true;
        }

        float scrollInput = Input.GetAxis("Mouse ScrollWheel"); // Get scroll input
        if (scrollInput > 0f)
        {
            ZoomIn();
        }
        else if (scrollInput < 0f)
        {
            ZoomOut();
        }

        // If middle mouse is held down, update rotation or movement
        if (isMiddleClickPressed)
        {
            // Rotate the Model based on mouse movement
            RotateModel();

            // Move the Model based on mouse movement
            MoveModel();
        }
    }

    public void ZoomIn()
    {
        Zoom--;
        Zoom = Mathf.Clamp(Zoom, 9f, 80f); // Ensure Zoom is within the valid range
        CameraComponent.fieldOfView = Zoom;
    }

    public void ZoomOut()
    {
        Zoom++;
        Zoom = Mathf.Clamp(Zoom, 9f, 80f); // Ensure Zoom is within the valid range
        CameraComponent.fieldOfView = Zoom;
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
        Vector3 movement = new Vector3(mouseDelta.x, mouseDelta.y, 0) * movementSpeed * Time.deltaTime;

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

    public GameObject getCurrentModel()
    {
        return Model;
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


