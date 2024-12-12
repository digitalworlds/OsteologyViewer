using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIScript : MonoBehaviour
{
    private UserInput UserInput;
    private Slider opacitySlider; // Slider for opacity adjustment
    private TextMeshProUGUI titleText; 

    private int numOfSavedViews;
    private List<GameObject> savedViews; // This should be initialized
    private Transform viewsTransform; // This is a local variable, not needed as a class field

    private Animator animator;
    private bool menu;
    

    void Start()
    {
        UserInput = GameObject.Find("Manager").GetComponent<UserInput>();
        animator = GameObject.Find("SideMenu").GetComponent<Animator>();
        titleText = GameObject.Find("Title").GetComponent<TextMeshProUGUI>();

        // Initialize the list to avoid NullReferenceException
        savedViews = new List<GameObject>();

        numOfSavedViews = -1;
        
        menu = false;

        // Find the parent object "Views" and get its children
        viewsTransform = GameObject.Find("Views").transform;

        // Populate the savedViews list with all child GameObjects of the "Views" object
        foreach (Transform child in viewsTransform)
        {
            savedViews.Add(child.gameObject);
        }

        // Optionally hide all saved views at the start
        foreach (GameObject view in savedViews)
        {
            view.SetActive(false);
        }
    }

    public void Update()
    {
        titleText.text = UserInput.Name;
        SelectPart();
        ChangeOpacity();
    }

    public void ZoomIn()
    {
        UserInput.ZoomIn();
    }

    public void ZoomOut()
    {
        UserInput.ZoomOut();
    }

    public void ResetView()
    {
        UserInput.ResetView();
    }

    public void ChangeOpacity()
    {
        // Find the slider for opacity
        opacitySlider = GameObject.Find("Opacity").GetComponent<Slider>(); // "OpacitySlider" is the name of the slider GameObject in the scene

        // Get the current model's material
        Renderer modelRenderer = UserInput.getCurrentModel().GetComponentInChildren<Renderer>();
        
        // Get the material's color and change the alpha value based on the slider
        Color currentColor = modelRenderer.material.color;
        currentColor.a = opacitySlider.value;  // Set the alpha value based on the slider
        modelRenderer.material.color = currentColor; // Apply the new color to the material
    }

    public void SaveView()
    {
        numOfSavedViews++;
        UserInput.SaveView();
        UpdateSavedViews();
    }

    public void UpdateSavedViews()
    {
        // Ensure currentView is within the bounds of the savedViews list
        if (numOfSavedViews < savedViews.Count)
        {
            savedViews[numOfSavedViews].SetActive(true);
        }
    }

    public void OpenSavedView(GameObject viewChoice)
    {
        // Log the name of the button that was clicked
        UserInput.OpenSavedView(int.Parse(viewChoice.name));
    }

    public void SelectMenuButton()
    {
        if (!menu)
        {
            OpenSideMenu();
        }
        else
        {
            CloseSideMenu();
        }
    }

    private void OpenSideMenu()
    {
        animator.SetTrigger("Move");  // Trigger the opening animation
        menu = true;  // Set the menu state to open
    }

    private void CloseSideMenu()
    {
        animator.SetTrigger("Move"); // Trigger the closing animation
        menu = false;  // Set the menu state to closed
    }

    void SelectPart()
    {
        // Step 1: Get the mouse position on screen
        Vector3 mousePosition = Input.mousePosition;
        
        // Debug: Check if the mouse position is within the screen bounds
        if (mousePosition.x < 0 || mousePosition.x > Screen.width || mousePosition.y < 0 || mousePosition.y > Screen.height)
        {
            Debug.LogWarning("Mouse is outside the screen bounds: " + mousePosition);
            return;
        }

        // Step 2: Create a ray from the camera through the mouse position
        // For orthographic cameras, the ray is cast straight, so we can just use the mouse position
        Ray ray = Camera.main.ScreenPointToRay(mousePosition);

        // Step 3: Perform the raycast
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            // Step 4: If we hit something, output the name of the object
            GameObject hitObject = hit.collider.gameObject;
            Debug.Log("Hit object: " + hitObject.name);

            // Optionally, do something with the selected object
            hitObject.GetComponent<Renderer>().material.color = Color.red;
        }
    }
}
