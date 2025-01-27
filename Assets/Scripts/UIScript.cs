using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class UIScript : MonoBehaviour
{
    private UserInput UserInput;
    public Slider opacitySlider; // Slider for opacity adjustment
    private TextMeshProUGUI titleText; 

    private int numOfSavedViews;
    private List<GameObject> savedViews = new List<GameObject>(); // This should be initialized
    private List<GameObject> defaultViews = new List<GameObject>();
    private Transform viewsTransform; // This is a local variable, not needed as a class field
    private Transform defaultViewsTransform;

    private Animator animator;
    private bool menu;

    public bool xrayOn;

    public Dictionary<GameObject, float> Opacities = new Dictionary<GameObject, float>(); 
    public Dictionary<GameObject, bool> OffOn = new Dictionary<GameObject, bool>(); 
    
    public void Start()
    {
        opacitySlider = GameObject.Find("Opacity").GetComponent<Slider>();
        
        UserInput = GameObject.Find("Manager").GetComponent<UserInput>();
        animator = GameObject.Find("SideMenu").GetComponent<Animator>();
        titleText = GameObject.Find("Title").GetComponent<TextMeshProUGUI>();

        numOfSavedViews = -1;
        
        menu = false;

        // Find the parent object "Views" and get its children
        defaultViewsTransform = GameObject.Find("DefaultViews").transform;

        // Populate the savedViews list with all child GameObjects of the "Views" object
        foreach (Transform child in defaultViewsTransform)
        {
            defaultViews.Add(child.gameObject);
        }

        quaternion rot = new quaternion(300, 45, 285, 0);
        UserInput.CreateView(Vector3.zero, rot, 4);
        rot = new quaternion(20, 305, 335, 0);
        UserInput.CreateView(Vector3.zero, rot, 4);
        rot = new quaternion(40, 260, 250, 0);
        UserInput.CreateView(Vector3.zero, rot, 4);

        // Find the parent object "Views" and get its children
        viewsTransform = GameObject.Find("Views").transform;

        // Populate the savedViews list with all child GameObjects of the "Views" object
        foreach (Transform child in viewsTransform)
        {
            savedViews.Add(child.gameObject);
            child.gameObject.SetActive(false);
        }
    }

    public void Update()
    {
        titleText.text = UserInput.Name;
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
        GameObject currentPart = UserInput.GetCurrentPart();
        if (currentPart != null)
        {
            Renderer modelRenderer = currentPart.GetComponentInChildren<Renderer>();

            if (xrayOn)
            {
                // Get the material's color and change the alpha value based on the slider
                Color currentColor = modelRenderer.material.color;
                currentColor.a = opacitySlider.value;  // Set the alpha value based on the slider
                modelRenderer.material.color = currentColor; // Apply the new color to the material

                // Update the opacity in the dictionary
                if (Opacities.ContainsKey(currentPart))
                {
                    // Update existing opacity value for this part
                    Opacities[currentPart] = opacitySlider.value;
                }
                else
                {
                    // Add a new opacity entry if not already present
                    Opacities.Add(currentPart, opacitySlider.value);
                }
            }
            else
            {
                if (opacitySlider.value < 0.5f)
                {
                    modelRenderer.enabled = false;
                }
                else
                {
                    modelRenderer.enabled = true;
                }

                // Update the state in the dictionary
                if (OffOn.ContainsKey(currentPart))
                {
                    // Update existing opacity value for this part
                    OffOn[currentPart] = modelRenderer.enabled;
                }
                else
                {
                    // Add a new opacity entry if not already present
                    OffOn.Add(currentPart, modelRenderer.enabled);
                }
            }
        }
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
}
