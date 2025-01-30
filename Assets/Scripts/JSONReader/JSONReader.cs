using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ObjectData
{
    [SerializeField]
    public string @object; // Handle C# keyword clash with "@object"

    [SerializeField]
    public int rotation;
}

[System.Serializable]
public class WallData
{
    public List<ObjectData> positiveY = new List<ObjectData>();
    public List<ObjectData> negativeY = new List<ObjectData>();
    public List<ObjectData> positiveX = new List<ObjectData>();
    public List<ObjectData> negativeX = new List<ObjectData>();
    public List<ObjectData> positiveZ = new List<ObjectData>();
    public List<ObjectData> negativeZ = new List<ObjectData>();

    public List<ObjectData> GetNeighbors(string direction)
    {
        return direction switch
        {
            "positiveX" => positiveX,
            "negativeX" => negativeX,
            "positiveY" => positiveY,
            "negativeY" => negativeY,
            "positiveZ" => positiveZ,
            "negativeZ" => negativeZ,
            _ => new List<ObjectData>()
        };
    }
    
}

[System.Serializable]
public class NamedObject
{
    public string name; // Object name, e.g., "wall"
    public WallData data; // Neighbor data

    
}

[System.Serializable]
public class RootObject
{
    public List<NamedObject> objects;
}

public class JSONReader : MonoBehaviour
{
    public TextAsset jsonFile; // Drag and drop your JSON file here
    public RootObject rootObject;

    private void Awake() {
        if (jsonFile != null)
        {
            // Deserialize JSON into the data structure
            rootObject = JsonUtility.FromJson<RootObject>(jsonFile.text); //This is where the data is extracted and put into the rootObject - The root object has the whole structure of the JSON file

            if (rootObject != null && rootObject.objects.Count > 0)
            {
              
                Debug.Log("JSON file parsed successfully!");
            }
            else
            {
                Debug.LogWarning("JSON parsing failed or no data available!");
            }
        }
        else
        {
            Debug.LogError("JSON file not assigned!");
        }
    }
   
}


