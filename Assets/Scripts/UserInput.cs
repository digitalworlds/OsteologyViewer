using System;
using System.Collections;
using System.Collections.Generic;
using GLTFast;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class UserInput : MonoBehaviour
{   
    public float rotationSpeed = 100f; // Rotation speed
    public float movementSpeed = 0.2f; // Movement speed
    public float defaultZoom;

    public string Name;

    // Dictionary to store materials for each part of the VisualModel
    public Dictionary<string, Material> DefaultMaterials = new Dictionary<string, Material>();
    public Material SelectedMaterial;
    public Material DefaultMaterialXray;
    public Material SelectedMaterialXray;

    private Vector3 lastMousePosition;
    private bool isRightClickPressed = false;
    private bool isMiddleClickPressed = false;

    private GameObject VisualModel;
    private GameObject Camera;
    private Camera CameraComponent;
    private float Zoom;

    private GameObject selectedPart;
    private GameObject Tip;
    private LineRenderer lineRenderer;

    // List of saved views (each saved view stores position, rotation, and orthographic size)
    private List<SavedView> SavedViews = new List<SavedView>();

    private UIScript uiScript;
    private TextMeshProUGUI scaleValue;

    private Model ModelData;

    public GameObject VideoPlayer;

    public void LoadFromHTML(string url)
    {
        StartCoroutine(LoadModel(url));
        Name = "Loading...";
    }

    public void Start()
    {
        Zoom = defaultZoom;

        VisualModel = GameObject.Find("Model");
        Camera = GameObject.Find("Main Camera");
        CameraComponent = Camera.GetComponent<Camera>(); // Get the Camera component

        scaleValue = GameObject.Find("ScaleValue").GetComponent<TextMeshProUGUI>();

        //LoadModel(Name);
        selectedPart = null;

        // Initialize the DefaultMaterials dictionary by iterating through VisualModel children
        foreach (Transform child in VisualModel.transform)
        {
            // Check if child has a Renderer and add its material to the dictionary
            Renderer renderer = child.GetComponent<Renderer>();
            if (renderer != null)
            {
                DefaultMaterials[child.name] = renderer.material;
            }
        }

        Tip = GameObject.Find("Tip");
        Tip.SetActive(false);

        uiScript = GameObject.Find("OverlayUI").GetComponent<UIScript>();
    }

    public void Update()
    {
        // Turn on tooltip
        if (selectedPart != null)
        {
            Tip.SetActive(true);
            foreach (ModelPart i in ModelData.Parts)
            {
                if(selectedPart.name.Contains(i.PartName))
                {
                    Tip.transform.Find("BG").Find("Text").GetComponent<TextMeshProUGUI>().text = i.DisplayName;
                }
            }
        }
        else
        {
            Tip.SetActive(false);
        }

        // Check for mouse button presses and handle movement/rotation
        if (Input.GetMouseButtonDown(1)) // Right mouse button
        {
            isRightClickPressed = true;
            lastMousePosition = Input.mousePosition;
            Cursor.visible = false;
        }

        if (Input.GetMouseButtonUp(1)) // Right mouse button released
        {
            isRightClickPressed = false;
            Cursor.visible = true;
        }

        if (Input.GetMouseButtonDown(2)) // Middle mouse button
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

        if (Input.GetMouseButtonDown(0)) // Left mouse button
        {
            SelectPart();
        }

        // Zoom in/out
        float scrollInput = Input.GetAxis("Mouse ScrollWheel"); // Get scroll input
        if (scrollInput > 0f && Zoom > 0f)
        {
            ZoomIn();
        }
        else if (scrollInput < 0f && Zoom < 10f)
        {
            ZoomOut();
        }

        // If right mouse is held down, rotate the VisualModel
        if (isRightClickPressed)
        {
            RotateModel();
        }

        // If middle mouse is held down, move the VisualModel
        if (isMiddleClickPressed)
        {
            MoveModel();
        }
    }

    public void ZoomIn()
    {
        Zoom--;
        Zoom = Mathf.Clamp(Zoom, 0.5f, 10f); // Clamp Zoom within range
        CameraComponent.orthographicSize = Zoom;
        scaleValue.text = Zoom.ToString() + "mm";
    }

    public void ZoomOut()
    {
        Zoom++;
        Zoom = Mathf.Clamp(Zoom, 0.5f, 10f); // Clamp Zoom within range
        CameraComponent.orthographicSize = Zoom;
        scaleValue.text = Zoom.ToString() + "mm";
    }

    void RotateModel()
    {
        // Get the difference between the current and last mouse position
        Vector3 mouseDelta = Input.mousePosition - lastMousePosition;

        // Calculate rotation based on mouse movement
        float rotX = mouseDelta.x * rotationSpeed * Time.deltaTime;
        float rotY = mouseDelta.y * rotationSpeed * Time.deltaTime;

        // Rotate the VisualModel around the X and Y axes
        VisualModel.transform.Rotate(Vector3.up, -rotX, Space.World); // Rotate around Y axis (horizontal mouse movement)
        VisualModel.transform.Rotate(Vector3.right, rotY, Space.World); // Rotate around X axis (vertical mouse movement)

        // Update last mouse position for the next frame
        lastMousePosition = Input.mousePosition;
    }

    void MoveModel()
    {
        // Get mouse movement in screen space and translate the object accordingly
        Vector3 mouseDelta = Input.mousePosition - lastMousePosition;

        // Convert the mouse movement into world space (movement in 3D)
        Vector3 movement = new Vector3(mouseDelta.x, mouseDelta.y, 0) * (movementSpeed * Zoom) * Time.deltaTime;

        // Move the VisualModel (here the movement is applied in local space)
        VisualModel.transform.Translate(movement, Space.World);

        // Update last mouse position for next frame
        lastMousePosition = Input.mousePosition;
    }

    public void ResetView()
    {
        // Reset the camera's orthographic size to the default zoom
        Zoom = defaultZoom;
        CameraComponent.orthographicSize = Zoom;

        // Reset the VisualModel's position to the origin (0, 0, 0)
        VisualModel.transform.position = Vector3.zero;

        // Reset the VisualModel's rotation to the default rotation (no rotation)
        VisualModel.transform.rotation = Quaternion.identity;
    }

    public GameObject GetCurrentPart()
    {
        return selectedPart;
    }

    // Method to save the current view (position, rotation, and orthographic size)
    public void SaveView()
    {
        // Save the current position, rotation, and camera's orthographic size
        SavedView view = new SavedView(VisualModel.transform.position, VisualModel.transform.rotation, CameraComponent.orthographicSize);

        // Add the view to the list of saved views
        SavedViews.Add(view);
    }

    // Method to create and save a custom view (position, rotation, and orthographic size)
    public void CreateView(Vector3 position, Quaternion rotation, float orthographicSize)
    {
        // Create a new SavedView instance with the provided parameters
        SavedView view = new SavedView(position, rotation, orthographicSize);
        
        // Add this custom view to the saved views list
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
            VisualModel.transform.position = savedView.Position;
            VisualModel.transform.rotation = savedView.Rotation;
            CameraComponent.orthographicSize = savedView.OrthographicSize;
        }
    }

    public IEnumerator LoadModel(string JsonURL)
    {
        // First, load the JSON metadata
        UnityWebRequest jsonRequest = UnityWebRequest.Get(JsonURL);
        yield return  jsonRequest.SendWebRequest();
        
        if (jsonRequest.result == UnityWebRequest.Result.Success)
        {
            string json = jsonRequest.downloadHandler.text;
            ModelData = JsonUtility.FromJson<Model>(json);
            //Debug.Log("Model Name: " + ModelData.ModelName);
            //Debug.Log("Description: " + ModelData.Description);

            Name = ModelData.ModelName;
            
            foreach(ModelPart part in ModelData.Parts)
            {
                Debug.Log("Part Name: " + part.PartName);
                Debug.Log("Diplay Name: " + part.DisplayName);
                Debug.Log("Part Description: " + part.PartDescription);
            }
            ImportModel(ModelData.URL);
        }
    }

    async void ImportModel(string ModelURL)
    {
        var gltfImport = new GltfImport();
        await gltfImport.Load(ModelURL);
        var instantiator = new GameObjectInstantiator(gltfImport, VisualModel.transform);
        var success = await gltfImport.InstantiateMainSceneAsync(instantiator);

        if (success) 
        {
            //Debug.Log("GLTF file is loaded.");

            VisualModel.transform.localScale /= 5;

            foreach (Transform child in VisualModel.transform.GetChild(0))
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
                        DefaultMaterials.Add(child.name, child.gameObject.GetComponent<Renderer>().material);
                    }
                }
            }

            VideoPlayer.SetActive(false);
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
                if (uiScript.xrayOn)
                {
                    // Deselect the part if it's already selected
                    if (selectedPart.GetComponent<Renderer>().material == SelectedMaterialXray)
                    {
                        // Use DefaultMaterials dictionary to revert the material
                        Color withOpacity = DefaultMaterialXray.color;
                        withOpacity.a = SelectedMaterialXray.color.a;

                        selectedPart.GetComponent<Renderer>().material = DefaultMaterialXray;
                        selectedPart.GetComponent<Renderer>().material.color = withOpacity;

                        selectedPart = null;
                        // Reset the slider when nothing is selected
                        uiScript.opacitySlider.value = 1f; // Reset slider to default value (fully opaque)
                    }
                }
                else
                {
                    // Deselect the part if it's already selected
                    if (selectedPart.GetComponent<Renderer>().material == SelectedMaterial)
                    {
                        // Use DefaultMaterials dictionary to revert the material
                        selectedPart.GetComponent<Renderer>().material = DefaultMaterials[selectedPart.name];
                        selectedPart = null;
                        // Reset the slider when nothing is selected
                        uiScript.opacitySlider.value = 1f; // Reset slider to default value (fully opaque)
                    }
                }
            }
            else
            {
                // Deselect the previous part if there's one selected
                if (selectedPart != null)
                {
                    if (uiScript.xrayOn)
                    {
                        selectedPart.GetComponent<Renderer>().material = DefaultMaterialXray;
                    }
                    else
                    {
                        selectedPart.GetComponent<Renderer>().material = DefaultMaterials[selectedPart.name];
                    }
                }

                selectedPart = hitObject;

                // Set the selected part's material to the selected one
                if (uiScript.xrayOn)
                {
                    selectedPart.GetComponent<Renderer>().material = SelectedMaterialXray;
                }
                else
                {
                    selectedPart.GetComponent<Renderer>().material = SelectedMaterial;
                }

                // Update the slider value based on the opacity dictionary (if exists)
                if (uiScript.Opacities.ContainsKey(selectedPart))
                {
                    // Apply the stored opacity for the selected part
                    uiScript.opacitySlider.value = uiScript.Opacities[selectedPart];
                }
                else
                {
                    // If no opacity is stored yet, default it to 1 (fully opaque)
                    uiScript.opacitySlider.value = 1f;
                }
            }

            UpdateSideMenu();
        }
    }

    public void UpdateSideMenu()
    {
        GameObject SideMenu = GameObject.Find("SideMenu");
        foreach (ModelPart i in ModelData.Parts)
        {
            if(selectedPart.name.Contains(i.PartName))
            {
                SideMenu.transform.Find("Part").GetComponent<TextMeshProUGUI>().text = i.DisplayName;
                SideMenu.transform.Find("Details").GetComponent<TextMeshProUGUI>().text = i.PartDescription;
            }
        }
        
    }

    public void SetXray()
    {
        foreach(Transform child in VisualModel.transform.GetChild(0))
        {
            child.GetComponent<Renderer>().enabled = true;
            child.GetComponent<Renderer>().material = DefaultMaterialXray;
        }
        selectedPart = null;
        uiScript.xrayOn = true;
        uiScript.opacitySlider.wholeNumbers = false;
    }
    public void SetDefault()
    {
        foreach(Transform child in VisualModel.transform.GetChild(0))
        {
            child.GetComponent<Renderer>().material = DefaultMaterials[child.name];
        }
        selectedPart = null;
        uiScript.xrayOn = false;
        uiScript.opacitySlider.wholeNumbers = true;
    }
}

// Class to store the position, rotation, and orthographic size
[Serializable]
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

[Serializable]
public class Model
{
    public string ModelName;
    public string Description;
    public string URL;
    public ModelPart[] Parts;

}

[Serializable]
public class ModelPart
{
    public string PartName;
    public string DisplayName;
    public string PartDescription;
}