using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIScript : MonoBehaviour
{
    public UserInput UserInput;
    private TextMeshProUGUI titleText; 

    public RectTransform scaleBarUI; // The visual bar (e.g., a black line)
    public TextMeshProUGUI scaleLabel; // The label showing real-world length
    public float referenceLengthInMeters = 1f; // How long the bar represents, in real-world meters
    public Camera orthoCamera;

    [SerializeField] private Animator animator;
    [SerializeField] private Animator animator2;
    private bool menu;

    public Dictionary<GameObject, float> Opacities = new Dictionary<GameObject, float>(); 
    public Dictionary<GameObject, bool> OffOn = new Dictionary<GameObject, bool>(); 
    
    public void Start()
    {
        titleText = GameObject.Find("Title").GetComponent<TextMeshProUGUI>();

        scaleBarUI = GameObject.Find("Scale").GetComponent<RectTransform>();
        scaleLabel = GameObject.Find("ScaleValue").GetComponent<TextMeshProUGUI>();
        orthoCamera = Camera.main;

        menu = false;
    }

    public void Update()
    {
        if(UserInput)
            titleText.text = UserInput.Name;

        UpdateScaleBar();
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
        animator2.SetTrigger("Move");
        menu = true;  // Set the menu state to open
    }

    private void CloseSideMenu()
    {
        animator.SetTrigger("Move"); // Trigger the closing animation
        animator2.SetTrigger("Move");
        menu = false;  // Set the menu state to closed
    } 

    private void UpdateScaleBar()
    {
        GameObject currentPart = UserInput.GetCurrentPart();
        if (currentPart == null) return;

        float modelScale = currentPart.transform.localScale.x * UserInput.scaleFactor; // Assume uniform scale
        float effectiveLength = referenceLengthInMeters * modelScale;

        // World units per pixel = (orthographic size * 2) / screen height
        float unitsPerPixel = (orthoCamera.orthographicSize * 2f) / Screen.height;
        float pixelsPerUnit = 1f / unitsPerPixel;

        float barLengthInPixels = effectiveLength * pixelsPerUnit;

        // Update UI element
        Vector2 size = scaleBarUI.sizeDelta;
        size.x = barLengthInPixels;
        scaleBarUI.sizeDelta = size;

        // Update the label
        if (referenceLengthInMeters >= 1f)
            scaleLabel.text = $"{referenceLengthInMeters * modelScale:F2} m";
        else
            scaleLabel.text = $"{referenceLengthInMeters * modelScale * 100f:F0} cm";
    }
}
