using System;
using System.Collections.Generic;
using UnityEngine;

public class Data : MonoBehaviour
{
    
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
    public string ViewerType;
    public string URL;
    public ModelPart[] Parts;
    public Vector3 OrientationVector;
    public int[] Orientation;
}

[Serializable]
public class ModelPart
{
    public string PartName;
    public string DisplayName;
    public string PartDescription;
}

[Serializable]
public class DictionaryWrapper
{
    public List<Colors> ColorDictionary;
}

[Serializable]
public class Colors
{
    public string key;
    public string value;
}