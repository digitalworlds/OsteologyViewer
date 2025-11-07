using System.Collections;
using System.Collections.Generic;
using GLTFast;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class UserInput : MonoBehaviour
{
   [SerializeField] private InputActionAsset inputActions;
    private InputAction rotateAction, moveAction, zoomAction, selectAction;
  
    // Touch input tracking
    private float lastPinchDistance;
    private bool isPinching = false;

    public string ColorDictionaryURL;
    private Dictionary<string, string> colorDictionary = new Dictionary<string, string>();

    public float rotationSpeed = 100f; // Rotation speed
    public float movementSpeed = 0.2f; // Movement speed
    public float defaultZoom;

    public string Name;
    public string URL;
    public float targetSize = 6.0f;
    public float scaleFactor = 0;

    private bool colorsOn = false;

    // Dictionary to store materials for each part of the VisualModel
    public Dictionary<string, Material> DefaultMaterials = new Dictionary<string, Material>();
    public Material SelectedMaterial;
    public Material defaultMaterial;
    private Material DefaultMaterial;

    private Vector3 lastMousePosition;
    private bool isRightClickPressed = false;
    private bool isMiddleClickPressed = false;

    private GameObject VisualModel;
    private GameObject Camera;
    private Camera CameraComponent;
    private float Zoom;

    private GameObject selectedPart;
    //private GameObject Tip;

    private UIScript uiScript;
    private TextMeshProUGUI scaleValue;

    private Model ModelData;

    public GameObject VideoPlayer;

    //Toggle List
    public GameObject TogglePrefab; // assign in Inspector
    public Transform ToggleListParent; // the VerticalLayoutGroup parent

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();

        var map = inputActions.FindActionMap("Input");
        rotateAction = map.FindAction("Rotate");
        moveAction = map.FindAction("Move");
        zoomAction = map.FindAction("Zoom");
        selectAction = map.FindAction("Select");

        rotateAction.Enable();
        moveAction.Enable();
        zoomAction.Enable();
        selectAction.Enable();
    }

    void OnDisable()
    {
        rotateAction.Disable();
        moveAction.Disable();
        zoomAction.Disable();
        selectAction.Disable();
    }

    public void LoadFromHTML(string url)
    {
        StartCoroutine(LoadModel(url));
    }

    public void Start()
    {
        Name = "Loading...";
        Zoom = defaultZoom;

        VisualModel = GameObject.Find("Model");
        Camera = GameObject.Find("Main Camera");
        CameraComponent = Camera.GetComponent<Camera>(); // Get the Camera component

        scaleValue = GameObject.Find("ScaleValue").GetComponent<TextMeshProUGUI>();

        StartCoroutine(LoadModel(URL));
        selectedPart = null;

        //Tip = GameObject.Find("Tip");

        uiScript = GameObject.Find("OverlayUI").GetComponent<UIScript>();
    }
    void Update()
    {
        // if (Touch.activeTouches.Count >= 2)
        // {
        //     var t0 = Touch.activeTouches[0];
        //     var t1 = Touch.activeTouches[1];

        //     float currentDistance = Vector2.Distance(t0.screenPosition, t1.screenPosition);
        //     float prevDistance = Vector2.Distance(
        //         t0.screenPosition - t0.delta,
        //         t1.screenPosition - t1.delta
        //     );

        //     float pinchDelta = currentDistance - prevDistance;

        //     ZoomModel(pinchDelta * 0.01f);
        // }
    
        HandleMouseInput();
        HandleTouchInput();
    }
    // public void Update()
    // {
    //     // Check for mouse button presses and handle movement/rotation
    //     if (Input.GetMouseButtonDown(1)) // Right mouse button
    //     {
    //         isRightClickPressed = true;
    //         lastMousePosition = Input.mousePosition;
    //         Cursor.visible = false;
    //     }

    //     if (Input.GetMouseButtonUp(1)) // Right mouse button released
    //     {
    //         isRightClickPressed = false;
    //         Cursor.visible = true;
    //     }

    //     if (Input.GetMouseButtonDown(2)) // Middle mouse button
    //     {
    //         isMiddleClickPressed = true;
    //         lastMousePosition = Input.mousePosition;
    //         Cursor.visible = false;
    //     }

    //     if (Input.GetMouseButtonUp(2)) // Middle mouse button released
    //     {
    //         isMiddleClickPressed = false;
    //         Cursor.visible = true;
    //     }

    //     if (Input.GetMouseButtonDown(0)) // Left mouse button
    //     {
    //         SelectPart();
    //     }

    //     // Zoom in/out
    //     float scrollInput = Input.GetAxis("Mouse ScrollWheel"); // Get scroll input
    //     if (scrollInput > 0f && Zoom > 0f)
    //     {
    //         ZoomIn();
    //     }
    //     else if (scrollInput < 0f && Zoom < 10f)
    //     {
    //         ZoomOut();
    //     }

    //     // If right mouse is held down, rotate the VisualModel
    //     if (isRightClickPressed)
    //     {
    //         RotateModel();
    //     }

    //     // If middle mouse is held down, move the VisualModel
    //     if (isMiddleClickPressed)
    //     {
    //         MoveModel();
    //     }
    // }

    void HandleMouseInput()
    {
        // Right click rotate
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            isRightClickPressed = true;
            lastMousePosition = Mouse.current.position.ReadValue();
            Cursor.visible = false;
        }
        if (Mouse.current.rightButton.wasReleasedThisFrame)
        {
            isRightClickPressed = false;
            Cursor.visible = true;
        }

        // Middle click move
        if (Mouse.current.middleButton.wasPressedThisFrame)
        {
            isMiddleClickPressed = true;
            lastMousePosition = Mouse.current.position.ReadValue();
            Cursor.visible = false;
        }
        if (Mouse.current.middleButton.wasReleasedThisFrame)
        {
            isMiddleClickPressed = false;
            Cursor.visible = true;
        }

        // Left click select
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            SelectPart();
        }

        // Scroll zoom
        float scrollValue = Mouse.current.scroll.ReadValue().y;
        if (scrollValue > 0f)
            ZoomIn();
        else if (scrollValue < 0f)
            ZoomOut();

        // Rotate or move model
        if (isRightClickPressed)
            RotateModel();
        if (isMiddleClickPressed)
            MoveModel();
    }

    void HandleTouchInput()
    {
        if (Touchscreen.current == null || Touchscreen.current.touches.Count == 0)
            return;

        var touches = Touchscreen.current.touches;

        // Single finger drag = rotate
        if (touches.Count == 1)
        {
            var touch = touches[0];
            if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Moved)
            {
                Vector2 delta = touch.delta.ReadValue();
                VisualModel.transform.Rotate(Vector3.up, -delta.x * rotationSpeed * Time.deltaTime, Space.World);
                VisualModel.transform.Rotate(Vector3.right, delta.y * rotationSpeed * Time.deltaTime, Space.World);
            }

            // Tap to select
            if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Ended && touch.delta.ReadValue().magnitude < 5f)
            {
                SelectPart();
            }
        }
        // Two-finger pinch = zoom
        else if (touches.Count == 2)
        {
            var touch1 = touches[0];
            var touch2 = touches[1];

            float currentDistance = Vector2.Distance(touch1.position.ReadValue(), touch2.position.ReadValue());

            if (!isPinching)
            {
                lastPinchDistance = currentDistance;
                isPinching = true;
            }
            else
            {
                float delta = currentDistance - lastPinchDistance;

                if (Mathf.Abs(delta) > 5f)
                {
                    if (delta > 0)
                        ZoomIn();
                    else
                        ZoomOut();
                }

                lastPinchDistance = currentDistance;
            }
        }
        else
        {
            isPinching = false;
        }
    }

    void RotateModel()
    {
        Vector2 currentPos = Mouse.current.position.ReadValue();
        Vector2 delta = currentPos - (Vector2)lastMousePosition;

        float rotX = delta.x * rotationSpeed * Time.deltaTime;
        float rotY = delta.y * rotationSpeed * Time.deltaTime;

        VisualModel.transform.Rotate(Vector3.up, -rotX, Space.World);
        VisualModel.transform.Rotate(Vector3.right, rotY, Space.World);

        lastMousePosition = currentPos;
    }

    void MoveModel()
    {
        Vector2 currentPos = Mouse.current.position.ReadValue();
        Vector2 delta = currentPos - (Vector2)lastMousePosition;

        Vector3 movement = new Vector3(delta.x, delta.y, 0) * (movementSpeed * Zoom) * Time.deltaTime;
        VisualModel.transform.Translate(movement, Space.World);

        lastMousePosition = currentPos;
    }

    public void ZoomIn()
    {
        Zoom--;
        Zoom = Mathf.Clamp(Zoom, 0.5f, 10f); // Clamp Zoom within range
        CameraComponent.orthographicSize = Zoom;
        float value = Mathf.Round(Zoom * scaleFactor * 10.0f) * 0.1f;
        scaleValue.text = value.ToString()  + "mm";
    }

    public void ZoomOut()
    {
        Zoom++;
        Zoom = Mathf.Clamp(Zoom, 0.5f, 10f); // Clamp Zoom within range
        CameraComponent.orthographicSize = Zoom;
        float value = Mathf.Round(Zoom * scaleFactor * 10.0f) * 0.1f;
        scaleValue.text = value.ToString()  + "mm";
    }

    // void RotateModel()
    // {
    //     // Get the difference between the current and last mouse position
    //     Vector3 mouseDelta = Input.mousePosition - lastMousePosition;

    //     // Calculate rotation based on mouse movement
    //     float rotX = mouseDelta.x * rotationSpeed * Time.deltaTime;
    //     float rotY = mouseDelta.y * rotationSpeed * Time.deltaTime;

    //     // Rotate the VisualModel around the X and Y axes
    //     VisualModel.transform.Rotate(Vector3.up, -rotX, Space.World); // Rotate around Y axis (horizontal mouse movement)
    //     VisualModel.transform.Rotate(Vector3.right, rotY, Space.World); // Rotate around X axis (vertical mouse movement)

    //     // Update last mouse position for the next frame
    //     lastMousePosition = Input.mousePosition;
    // }

    // void MoveModel()
    // {
    //     // Get mouse movement in screen space and translate the object accordingly
    //     Vector3 mouseDelta = Input.mousePosition - lastMousePosition;

    //     // Convert the mouse movement into world space (movement in 3D)
    //     Vector3 movement = new Vector3(mouseDelta.x, mouseDelta.y, 0) * (movementSpeed * Zoom) * Time.deltaTime;

    //     // Move the VisualModel (here the movement is applied in local space)
    //     VisualModel.transform.Translate(movement, Space.World);

    //     // Update last mouse position for next frame
    //     lastMousePosition = Input.mousePosition;
    // }

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

    public IEnumerator LoadModel(string JsonURL)
    {
        // First, load the JSON metadata
        UnityWebRequest jsonRequest = UnityWebRequest.Get(JsonURL);
        yield return jsonRequest.SendWebRequest();

        if (jsonRequest.result == UnityWebRequest.Result.Success)
        {
            string json = jsonRequest.downloadHandler.text;
            ModelData = JsonUtility.FromJson<Model>(json);

            Name = ModelData.ModelName;
            Debug.Log(ModelData.Orientation);

            ModelData.OrientationVector = new Vector3(
                ModelData.Orientation[0],
                ModelData.Orientation[1],
                ModelData.Orientation[2]
            );

            // foreach(ModelPart part in ModelData.Parts)
            // {
            //     Debug.Log("Part Name: " + part.PartName);
            //     Debug.Log("Diplay Name: " + part.DisplayName);
            //     Debug.Log("Part Description: " + part.PartDescription);
            // }
            //Debug.Log(ModelData.URL);

            string url = JsonURL.Substring(0, JsonURL.LastIndexOf("/") + 1) + ModelData.URL.Substring(4);
            ImportModel(url);
            StartCoroutine(LoadColorDictionary(ColorDictionaryURL));
        }
    }

    public IEnumerator LoadColorDictionary(string colorDictionaryURL)
    {
        UnityWebRequest jsonRequest = UnityWebRequest.Get(colorDictionaryURL);
        yield return jsonRequest.SendWebRequest();

        if (jsonRequest.result == UnityWebRequest.Result.Success)
        {
            string json = jsonRequest.downloadHandler.text;
            DictionaryWrapper wrapper = JsonUtility.FromJson<DictionaryWrapper>(json);

            foreach (var kv in wrapper.ColorDictionary)
            {
                colorDictionary[kv.key] = kv.value;
                //Debug.Log(kv.key + ", " + kv.value);
            }
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
            foreach (Transform child in ToggleListParent)
            {
                Destroy(child.gameObject);
            }
            
            Debug.Log("GLTF file is loaded.");

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
                        child.gameObject.GetComponent<Renderer>().material = defaultMaterial;
                        DefaultMaterials.Add(child.name, child.gameObject.GetComponent<Renderer>().material);
                        DefaultMaterial = child.GetComponent<Renderer>().material;

                        GameObject toggleGO = Instantiate(TogglePrefab, ToggleListParent);
                        Toggle toggle = toggleGO.GetComponent<Toggle>();
                        Text label = toggleGO.GetComponentInChildren<Text>();
                        foreach(ModelPart part in ModelData.Parts)
                        {
                            if (part.PartName == child.name)
                            {
                                label.text = part.DisplayName;
                                toggle.isOn = true;
                            }
                        }

                        var thisChild = child;
                        toggle.onValueChanged.AddListener((bool isOn) =>
                        {
                            thisChild.gameObject.SetActive(isOn);
                        });
                    }
                }
            }

            // Calculate combined bounds
            Bounds combinedBounds = new Bounds(VisualModel.transform.position, Vector3.zero);
            Renderer[] renderers = VisualModel.GetComponentsInChildren<Renderer>();

            if (renderers.Length > 0)
            {
                combinedBounds = renderers[0].bounds;
                foreach (Renderer renderer in renderers)
                {
                    combinedBounds.Encapsulate(renderer.bounds);
                }
            }

            // Find the largest dimension
            float largestDimension = Mathf.Max(combinedBounds.size.x, Mathf.Max(combinedBounds.size.y, combinedBounds.size.z));

            // Calculate scale factor
            scaleFactor = targetSize / largestDimension * ModelData.BiologicalScaleMM;

            // Apply scale to root transform
            VisualModel.transform.localScale *= scaleFactor;
            //uiScript.referenceLengthInMeters /= scaleFactor;

            Debug.Log($"Scaled model by {scaleFactor} to fit within {targetSize} unit bounding box.");

            Debug.Log(ModelData.OrientationVector[0] + " " + ModelData.OrientationVector[1] + " " + ModelData.OrientationVector[2]);
            VisualModel.transform.localEulerAngles = new Vector3(ModelData.OrientationVector[0], ModelData.OrientationVector[1], ModelData.OrientationVector[2]);

            if (SceneManager.GetActiveScene().name.Contains("Taxon"))
            {
                SetBoneAndTeeth();
            }
            else if (SceneManager.GetActiveScene().name.Contains("Tooth"))
            {
                SetColors();
            }

            float value = Mathf.Round(Zoom * scaleFactor * 10.0f) * 0.1f;
            scaleValue.text = value.ToString()  + "mm";

            VideoPlayer.SetActive(false);
            uiScript.enabled = true;
            UpdateCurrentMaterials();
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

            // if (selectedPart != null && selectedPart == hitObject)
            // {
            //     // Deselect the part if it's already selected
            //     if (selectedPart.GetComponent<Renderer>().material == SelectedMaterial)
            //     {
            //         // Use DefaultMaterials dictionary to revert the material
            //         selectedPart.GetComponent<Renderer>().material = DefaultMaterials[selectedPart.name];
            //         selectedPart = null;
            //         // Reset the slider when nothing is selected
            //         uiScript.opacitySlider.value = 1f; // Reset slider to default value (fully opaque)
            //     }
            // }
            // else
            // {
                // Deselect the previous part if there's one selected
                if (selectedPart != null)
                {
                    selectedPart.GetComponent<Renderer>().material = DefaultMaterials[selectedPart.name];
                }

                selectedPart = hitObject;
                selectedPart.GetComponent<Renderer>().material = SelectedMaterial;

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
            //}
            UpdateSideMenuAndTip();
        }
        else
        {
            // Deselect the previous part if there's one selected
            if (selectedPart != null)
            {
                selectedPart.GetComponent<Renderer>().material = DefaultMaterials[selectedPart.name];

                GameObject SideMenu = GameObject.Find("SideMenu");
                SideMenu.transform.Find("Part").GetComponent<TextMeshProUGUI>().text = "No Part Selected";
                SideMenu.transform.Find("Details").GetComponent<TextMeshProUGUI>().text = "Click on a part to see more here!";
                //Tip.transform.Find("BG").Find("Text").GetComponent<TextMeshProUGUI>().text = "No Part Selected";
            }
        }
    }

    public void UpdateSideMenuAndTip()
    {
        GameObject SideMenu = GameObject.Find("SideMenu");
        foreach (ModelPart i in ModelData.Parts)
        {
            if (selectedPart.name.Contains(i.PartName))
            {
                SideMenu.transform.Find("Part").GetComponent<TextMeshProUGUI>().text = i.DisplayName;
                SideMenu.transform.Find("Details").GetComponent<TextMeshProUGUI>().text = i.PartDescription;
                //Tip.transform.Find("BG").Find("Text").GetComponent<TextMeshProUGUI>().text = i.DisplayName;
            }
        }
    }

    public void UpdateCurrentMaterials()
    {
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
    }

    public void SetVisuals(GameObject text)
    {
        if (colorsOn)
        {
            text.GetComponent<TextMeshProUGUI>().text = "C";
            SetDefault();
        }
        else
        {
            text.GetComponent<TextMeshProUGUI>().text = "D";
            SetColors();
        }
    }

    private void SetDefault()
    {
        colorsOn = false;
        foreach (Transform child in VisualModel.transform.GetChild(0))
        {
            child.GetComponent<Renderer>().material = defaultMaterial;
            DefaultMaterials[child.name] = child.GetComponent<Renderer>().material;
        }

        selectedPart = null;
        uiScript.opacitySlider.wholeNumbers = true;

        UpdateCurrentMaterials();
    }

    private void SetColors()
    {
        colorsOn = true;
        foreach (Transform child in VisualModel.transform.GetChild(0))
        {
            foreach (var entry in colorDictionary)
            {
                string key = entry.Key;
                string value = entry.Value;

                Debug.Log(key + ", " + value);

                if (child.name.Contains(key))
                {
                    child.GetComponent<Renderer>().enabled = true;

                    Material material;

                    if (ColorUtility.TryParseHtmlString(value, out Color color))
                    {
                        material = new Material(DefaultMaterial);
                        material.color = color;
                    }
                    else if (value == "Bone")
                    {
                        material = Resources.Load<Material>("Materials/BoneTexture");
                    }
                    else if (value == "Teeth")
                    {
                        material = Resources.Load<Material>("Materials/TeethTexture");
                    }
                    else
                    {
                        // Fallback to default material if value isn't a color or known type
                        material = new Material(DefaultMaterial);
                    }

                    child.GetComponent<Renderer>().material = material;
                    DefaultMaterials[child.name] = material;

                    break; // Stop checking once we've found a match
                }
            }
        }

        selectedPart = null;
        uiScript.opacitySlider.wholeNumbers = true;

        UpdateCurrentMaterials();
    }

    public void SetBoneAndTeeth()
    {
        foreach (Transform child in VisualModel.transform.GetChild(0))
        {
            foreach (var entry in colorDictionary)
            {
                string key = entry.Key;
                string value = entry.Value;

                if (child.name.Contains(key))
                {
                    if (value == "Bone")
                    {
                        child.GetComponent<Renderer>().material = Resources.Load<Material>("Materials/BoneTexture");
                    }
                    else if (value == "Teeth")
                    {
                        child.GetComponent<Renderer>().material = Resources.Load<Material>("Materials/TeethTexture");
                    }
                    // Break after match to avoid unnecessary checks
                    break;
                }
            }
        }

        selectedPart = null;

        UpdateCurrentMaterials();
    }

    public void HideMandible()
    {
        foreach (Transform child in VisualModel.transform.GetChild(0))
        {
            foreach (ModelPart i in ModelData.Parts)
            {
                if (child.name.Contains(i.PartName) && i.PartName.Contains("Mandible"))
                {
                    if (child.gameObject.activeSelf)
                    {
                        child.gameObject.SetActive(false);
                    }
                    else
                    {
                        child.gameObject.SetActive(true);
                    }
                }
            }
        }
    }
    public void HideCranialVault()
    {
        foreach (Transform child in VisualModel.transform.GetChild(0))
        {
            foreach (ModelPart i in ModelData.Parts)
            {
                if (child.name.Contains(i.PartName) && child.name.Contains("Calotte"))
                {
                    if (child.gameObject.activeSelf)
                    {
                        child.gameObject.SetActive(false);
                    }
                    else
                    {
                        child.gameObject.SetActive(true);
                    }
                }
            }
        }
    }
}