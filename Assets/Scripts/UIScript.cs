using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIScript : MonoBehaviour
{
    private UserInput UserInput;
    private Slider opacitySlider; // Slider for opacity adjustment

    private int numOfSavedViews;
    private List<GameObject> savedViews; // This should be initialized
    private Transform viewsTransform; // This is a local variable, not needed as a class field

    void Start()
    {
        UserInput = GameObject.Find("Manager").GetComponent<UserInput>();

        // Initialize the list to avoid NullReferenceException
        savedViews = new List<GameObject>();

        numOfSavedViews = -1;

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

    void Update()
    {
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

    public void OpenMenu()
    {
        // Open menu logic (if you have any)
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
}
