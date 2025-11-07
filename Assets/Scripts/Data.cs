using System;
using System.Collections.Generic;
using UnityEngine;

public class Data : MonoBehaviour
{
    
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
    public float BiologicalScaleMM;
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