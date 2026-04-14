using System;
using System.Collections;
using System.Collections.Generic;
using GLTFast;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UserInput_Dual : MonoBehaviour
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

    public string Name1;
    public string Name2;
    public string Model1URL;
    public string Model2URL;
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

    private GameObject VisualModel1;
    private GameObject VisualModel2;
    private GameObject Camera1;
    private GameObject Camera2;
    private Camera CameraComponent1;
    private Camera CameraComponent2;
    private float Zoom;

    private GameObject selectedPart;
    private GameObject Tip;

    [SerializeField] private UIScript_Dual uiScript1;
     [SerializeField] private UIScript_Dual uiScript2;
    private TextMeshProUGUI scaleValue;

    private Model ModelData1;
    private Model ModelData2;

    public GameObject VideoPlayer;

    public GameObject TogglePrefab;
    [SerializeField] public Transform ToggleListParent1; // the VerticalLayoutGroup parent
    [SerializeField] public Transform ToggleListParent2;

    [SerializeField] private GameObject CVisualsButton1;
    [SerializeField] private GameObject DVisualsButton1;
    [SerializeField] private GameObject CVisualsButton2;
    [SerializeField] private GameObject DVisualsButton2;

    public void LoadFromHTML(string url1, string url2)
    {
        StartCoroutine(LoadModel(url1, VisualModel1,ToggleListParent1));
        StartCoroutine(LoadModel(url2, VisualModel2, ToggleListParent2));
        Name1 = "Loading...";
        Name2 = "Loading...";
    }

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

    public void Start()
    {
        Name1 = "Loading...";
        Name2 = "Loading...";
        Zoom = defaultZoom;

        VisualModel1 = GameObject.Find("Model1");
        VisualModel2 = GameObject.Find("Model2");

        Camera1 = GameObject.Find("Cam1");
        CameraComponent1 = Camera1.GetComponent<Camera>(); // Get the Camera component

        Camera2 = GameObject.Find("Cam2");
        CameraComponent2 = Camera2.GetComponent<Camera>(); // Get the Camera component

        scaleValue = GameObject.Find("ScaleValue").GetComponent<TextMeshProUGUI>();

        StartCoroutine(LoadModel(Model1URL, VisualModel1,ToggleListParent1));
        StartCoroutine(LoadModel(Model2URL, VisualModel2, ToggleListParent2));
        selectedPart = null;
    }

    void Update()
    {
        HandleMouseInput();
        HandleTouchInput();

        uiScript1.titleText.text = Name1;
        uiScript2.titleText.text = Name2;
    }

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
                VisualModel1.transform.Rotate(Vector3.up, -delta.x * rotationSpeed * Time.deltaTime, Space.World);
                VisualModel1.transform.Rotate(Vector3.right, delta.y * rotationSpeed * Time.deltaTime, Space.World);

                VisualModel2.transform.Rotate(Vector3.up, -delta.x * rotationSpeed * Time.deltaTime, Space.World);
                VisualModel2.transform.Rotate(Vector3.right, delta.y * rotationSpeed * Time.deltaTime, Space.World);
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

        VisualModel1.transform.Rotate(Vector3.up, -rotX, Space.World);
        VisualModel1.transform.Rotate(Vector3.right, rotY, Space.World);
        VisualModel2.transform.Rotate(Vector3.up, -rotX, Space.World);
        VisualModel2.transform.Rotate(Vector3.right, rotY, Space.World);

        lastMousePosition = currentPos;
    }

    void MoveModel()
    {
        Vector2 currentPos = Mouse.current.position.ReadValue();
        Vector2 delta = currentPos - (Vector2)lastMousePosition;

        Vector3 movement = new Vector3(delta.x, delta.y, 0) * (movementSpeed * Zoom) * Time.deltaTime;
        VisualModel1.transform.Translate(movement, Space.World);
        VisualModel2.transform.Translate(movement, Space.World);

        lastMousePosition = currentPos;
    }

    public void ZoomIn()
    {
        Zoom--;
        Zoom = Mathf.Clamp(Zoom, 0.5f, 10f); // Clamp Zoom within range
        CameraComponent1.orthographicSize = Zoom;
        CameraComponent2.orthographicSize = Zoom;
        scaleValue.text = Zoom.ToString() + "mm";
    }

    public void ZoomOut()
    {
        Zoom++;
        Zoom = Mathf.Clamp(Zoom, 0.5f, 10f); // Clamp Zoom within range
        CameraComponent1.orthographicSize = Zoom;
        CameraComponent2.orthographicSize = Zoom;
        scaleValue.text = Zoom.ToString() + "mm";
    }

    public void ResetView()
    {
        // Reset the camera's orthographic size to the default zoom
        Zoom = defaultZoom;
        CameraComponent1.orthographicSize = Zoom;
        CameraComponent2.orthographicSize = Zoom;

        // Reset the VisualModel's position to the origin (0, 0, 0)
        VisualModel1.transform.position = Vector3.zero;

        // Reset the VisualModel's rotation to the default rotation (no rotation)
        VisualModel1.transform.rotation = Quaternion.identity;

        // Reset the VisualModel's position to the origin (0, 0, 0)
        VisualModel2.transform.position = Vector3.zero;

        // Reset the VisualModel's rotation to the default rotation (no rotation)
        VisualModel2.transform.rotation = Quaternion.identity;
    }

    public GameObject GetCurrentPart()
    {
        return selectedPart;
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

    public IEnumerator LoadModel(string JsonURL, GameObject visual, Transform ToggleListParent)
    {
        // First, load the JSON metadata
        UnityWebRequest jsonRequest = UnityWebRequest.Get(JsonURL);
        yield return jsonRequest.SendWebRequest();

        if (jsonRequest.result == UnityWebRequest.Result.Success)
        {
            string url;
            if (visual == VisualModel1)
            {
                string json = jsonRequest.downloadHandler.text;
                ModelData1 = JsonUtility.FromJson<Model>(json);
                Name1 = ModelData1.ModelName;

                
                //Debug.Log(ModelData.Orientation);

                ModelData1.OrientationVector = new Vector3(
                    ModelData1.Orientation[0],
                    ModelData1.Orientation[1],
                    ModelData1.Orientation[2]
                );

                // foreach(ModelPart part in ModelData.Parts)
                // {
                //     Debug.Log("Part Name: " + part.PartName);
                //     Debug.Log("Diplay Name: " + part.DisplayName);
                //     Debug.Log("Part Description: " + part.PartDescription);
                // }
                //Debug.Log(ModelData.URL);
                url = JsonURL.Substring(0, JsonURL.LastIndexOf("/") + 1) + ModelData1.URL;
            }
            else
            {
                string json = jsonRequest.downloadHandler.text;
                ModelData2 = JsonUtility.FromJson<Model>(json);
                Name2 = ModelData2.ModelName;
                
                //Debug.Log(ModelData.Orientation);

                ModelData2.OrientationVector = new Vector3(
                    ModelData2.Orientation[0],
                    ModelData2.Orientation[1],
                    ModelData2.Orientation[2]
                );

                // foreach(ModelPart part in ModelData.Parts)
                // {
                //     Debug.Log("Part Name: " + part.PartName);
                //     Debug.Log("Diplay Name: " + part.DisplayName);
                //     Debug.Log("Part Description: " + part.PartDescription);
                // }
                //Debug.Log(ModelData.URL);
                url = JsonURL.Substring(0, JsonURL.LastIndexOf("/") + 1) + ModelData2.URL;
            }
            ImportModel(url, visual, ToggleListParent);
            StartCoroutine(LoadColorDictionary(ColorDictionaryURL));
        }
    }

    async void ImportModel(string ModelURL, GameObject visual, Transform ToggleListParent)
    {
        //Debug.Log("Importing");
        var gltfImport = new GltfImport();
        await gltfImport.Load(ModelURL);
        var instantiator = new GameObjectInstantiator(gltfImport, visual.transform);
        var success = await gltfImport.InstantiateMainSceneAsync(instantiator);

        if (success)
        {
            foreach (Transform child in ToggleListParent)
            {
                Destroy(child.gameObject);
            }

            //Debug.Log("GLTF file is loaded.");

            visual.transform.localScale /= 5;

            foreach (Transform child in visual.transform.GetChild(0))
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
                    
                    if (visual == VisualModel1)
                    {
                        child.gameObject.layer = LayerMask.NameToLayer("Model1");
                    }
                    else
                    {
                        child.gameObject.layer = LayerMask.NameToLayer("Model2");
                    }

                    GameObject toggleGO = Instantiate(TogglePrefab, ToggleListParent);
                    Toggle toggle = toggleGO.GetComponent<Toggle>();
                    Text label = toggleGO.GetComponentInChildren<Text>();

                    if (visual == VisualModel1)
                    {   
                        foreach (ModelPart part in ModelData1.Parts)
                        {
                            if (part.PartName == child.name)
                            {
                                label.text = part.DisplayName;
                                toggle.isOn = true;
                            }
                        }
                    }
                    else
                    {
                        foreach (ModelPart part in ModelData2.Parts)
                        {
                            if (part.PartName == child.name)
                            {
                                label.text = part.DisplayName;
                                toggle.isOn = true;
                            }
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
            Bounds combinedBounds = new Bounds(visual.transform.position, Vector3.zero);
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>();

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
            //Debug.Log(ModelData.BiologicalScaleMM);
            if (visual == VisualModel1)
            {
                scaleFactor = targetSize / (largestDimension * ModelData1.BiologicalScaleMM);
                visual.transform.localEulerAngles = new Vector3(ModelData1.OrientationVector[0], ModelData1.OrientationVector[1], ModelData1.OrientationVector[2]);
            }
            else
            {
                scaleFactor = targetSize / (largestDimension * ModelData2.BiologicalScaleMM);
                visual.transform.localEulerAngles = new Vector3(ModelData2.OrientationVector[0], ModelData2.OrientationVector[1], ModelData2.OrientationVector[2]);

            }
            visual.transform.localScale *= scaleFactor;

            //Debug.Log($"Scaled model by {scaleFactor} to fit within {targetSize} unit bounding box.");

            float value = Mathf.Round(Zoom * scaleFactor * 10.0f) * 0.1f;
            scaleValue.text = value.ToString() + "mm";

            VideoPlayer.SetActive(false);
            uiScript1.enabled = true;
            uiScript2.enabled = true;
            UpdateCurrentMaterials();
        }
        else
        {
            Debug.Log("Could not import");
        }
    }

    void SelectPart()
    {
        LayerMask layerMask;
        Ray ray;

        if (Mouse.current.position.ReadValue().x < Screen.width / 2f)
        {
            layerMask = LayerMask.GetMask("Model1");
            ray = CameraComponent1.ScreenPointToRay(Mouse.current.position.ReadValue());
        }
        else
        {
            layerMask = LayerMask.GetMask("Model2");
            ray = CameraComponent2.ScreenPointToRay(Mouse.current.position.ReadValue());
        }

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask))
        {
            GameObject hitObject = hit.collider.gameObject;

            if (selectedPart == hitObject)
            {
                selectedPart.GetComponent<Renderer>().material = DefaultMaterials[selectedPart.name];
                selectedPart = null;
                if(selectedPart.layer == LayerMask.NameToLayer("Model1"))
                {
                    uiScript1.UpdateSideMenu(selectedPart, ModelData1);
                }
                else
                {
                    uiScript2.UpdateSideMenu(selectedPart, ModelData2);
                }
                return;
            }

            if (selectedPart != null)
            {
                selectedPart.GetComponent<Renderer>().material = DefaultMaterials[selectedPart.name];
            }

            selectedPart = hitObject;
            selectedPart.GetComponent<Renderer>().material = SelectedMaterial;
        }
        else
        {
            // Deselect the previous part if there's one selected and a new one hasnt been selected
            if (selectedPart != null)
            {
                selectedPart.GetComponent<Renderer>().material = DefaultMaterials[selectedPart.name];
                selectedPart = null;
            }
        }

        if(selectedPart.layer == LayerMask.NameToLayer("Model1"))
        {
            Debug.Log("Left");
            uiScript1.UpdateSideMenu(selectedPart, ModelData1);
        }
        else
        {
            Debug.Log("Right");
            uiScript2.UpdateSideMenu(selectedPart, ModelData2);
        }
    }

    public void UpdateCurrentMaterials()
    {
        // Initialize the DefaultMaterials dictionary by iterating through VisualModel children
        foreach (Transform child in VisualModel1.transform)
        {
            // Check if child has a Renderer and add its material to the dictionary
            Renderer renderer = child.GetComponent<Renderer>();
            if (renderer != null)
            {
                DefaultMaterials[child.name] = renderer.material;
            }
        }

        // Initialize the DefaultMaterials dictionary by iterating through VisualModel children
        foreach (Transform child in VisualModel2.transform)
        {
            // Check if child has a Renderer and add its material to the dictionary
            Renderer renderer = child.GetComponent<Renderer>();
            if (renderer != null)
            {
                DefaultMaterials[child.name] = renderer.material;
            }
        }
    }

    public void SetVisuals()
    {
        if (colorsOn)
        {
            DVisualsButton1.SetActive(false);
            CVisualsButton1.SetActive(true);
            DVisualsButton2.SetActive(false);
            CVisualsButton2.SetActive(true);
            SetDefault();
        }
        else
        {
            CVisualsButton1.SetActive(false);
            DVisualsButton1.SetActive(true);
            CVisualsButton2.SetActive(false);
            DVisualsButton2.SetActive(true);
            SetColors();
        }
    }
    
    public void SetDefault()
    {
        colorsOn = false;
        foreach (Transform child in VisualModel1.transform.GetChild(0))
        {
            child.GetComponent<Renderer>().material = DefaultMaterial;
            DefaultMaterials[child.name] = child.GetComponent<Renderer>().material;
        }

        foreach (Transform child in VisualModel2.transform.GetChild(0))
        {
            child.GetComponent<Renderer>().material = DefaultMaterial;
            DefaultMaterials[child.name] = child.GetComponent<Renderer>().material;
        }

        selectedPart = null;

        UpdateCurrentMaterials();
    }

    public void SetColors()
    {
        colorsOn = true;
        foreach (Transform child in VisualModel1.transform.GetChild(0))
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

        foreach (Transform child in VisualModel2.transform.GetChild(0))
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

        UpdateCurrentMaterials();
    }

    public void SetBoneAndTeeth()
    {
        foreach (Transform child in VisualModel1.transform.GetChild(0))
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
        foreach (Transform child in VisualModel2.transform.GetChild(0))
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

    // public void HideMandible()
    // {
    //     foreach (Transform child in VisualModel1.transform.GetChild(0))
    //     {
    //         foreach (ModelPart i in ModelData1.Parts + ModelData2.Parts)
    //         {
    //             if (child.name.Contains(i.PartName) && i.PartName.Contains("Mandible"))
    //             {
    //                 if (child.gameObject.activeSelf)
    //                 {
    //                     child.gameObject.SetActive(false);
    //                 }
    //                 else
    //                 {
    //                     child.gameObject.SetActive(true);
    //                 }
    //             }
    //         }
    //     }
        
    //     foreach(Transform child in VisualModel2.transform.GetChild(0))
    //     {
    //         foreach (ModelPart i in ModelData1.Parts + ModelData2.Parts)
    //         {
    //             if(child.name.Contains(i.PartName) && i.PartName.Contains("Mandible"))
    //             {
    //                 if(child.gameObject.activeSelf)
    //                 {
    //                     child.gameObject.SetActive(false);
    //                 }
    //                 else
    //                 {
    //                     child.gameObject.SetActive(true);
    //                 }
    //             }
    //         }
    //     }
    // }
    // public void HideCranialVault()
    // {
    //     foreach (Transform child in VisualModel1.transform.GetChild(0))
    //     {
    //         foreach (ModelPart i in ModelData1.Parts + ModelData2.Parts)
    //         {
    //             if (child.name.Contains(i.PartName) && child.name.Contains("Calotte"))
    //             {
    //                 if (child.gameObject.activeSelf)
    //                 {
    //                     child.gameObject.SetActive(false);
    //                 }
    //                 else
    //                 {
    //                     child.gameObject.SetActive(true);
    //                 }
    //             }
    //         }
    //     }
        
    //     foreach(Transform child in VisualModel2.transform.GetChild(0))
    //     {
    //         foreach (ModelPart i in ModelData1.Parts + ModelData2.Parts)
    //         {
    //             if(child.name.Contains(i.PartName) && child.name.Contains("Calotte"))
    //             {
    //                 if(child.gameObject.activeSelf)
    //                 {
    //                     child.gameObject.SetActive(false);
    //                 }
    //                 else
    //                 {
    //                     child.gameObject.SetActive(true);
    //                 }
    //             }
    //         }
    //     }
    // }
}