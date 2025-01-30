using System.Collections.Generic;
using UnityEngine;

public class JSONReaderWithRotations : MonoBehaviour
{
    public TextAsset jsonFile; // Drag and drop your JSON file here
    public RootObject rootObject;

    private void Awake()
    {
        if (jsonFile != null)
        {
            rootObject = JsonUtility.FromJson<RootObject>(jsonFile.text);

            if (rootObject != null && rootObject.objects.Count > 0)
            {
                Debug.Log("JSON file parsed successfully!");
                GenerateRotatedObjects(); // Add rotated objects to the data
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

    private void GenerateRotatedObjects()
    {
        List<NamedObject> extendedObjects = new List<NamedObject>();

        foreach (var obj in rootObject.objects)
        {
            // Add the original object
            extendedObjects.Add(obj);

            // Generate rotated versions
            for (int i = 1; i < 4; i++) // 90°, 180°, 270°
            {
                var rotatedObject = RotateObject(obj, i * 90);
                extendedObjects.Add(rotatedObject);
            }
        }

        rootObject.objects = extendedObjects;
        Debug.Log("Rotated objects generated successfully!");
    }

    private NamedObject RotateObject(NamedObject original, int rotation)
    {
        NamedObject rotated = new NamedObject
        {
            name = $"{original.name}_rot{rotation}",
            data = RotateWallData(original.data, rotation)
        };

        return rotated;
    }

    private WallData RotateWallData(WallData original, int rotation)
    {
        WallData rotated = new WallData();

        foreach (var direction in new[] { "positiveX", "negativeX", "positiveY", "negativeY", "positiveZ", "negativeZ" })
        {
            List<ObjectData> neighbors = original.GetNeighbors(direction);
            string newDirection = RotateDirection(direction, rotation);
            List<ObjectData> adjustedNeighbors = AdjustNeighborRotations(neighbors, rotation);

            // Assign neighbors to the rotated direction
            rotated.GetNeighbors(newDirection).AddRange(adjustedNeighbors);
        }

        return rotated;
    }

    private List<ObjectData> AdjustNeighborRotations(List<ObjectData> neighbors, int rotation)
    {
        List<ObjectData> adjusted = new List<ObjectData>();

        foreach (var neighbor in neighbors)
        {
            adjusted.Add(new ObjectData
            {
                @object = neighbor.@object,
                rotation = (neighbor.rotation + rotation) % 360
            });
        }

        return adjusted;
    }

    private string RotateDirection(string direction, int rotation)
    {
        Dictionary<string, string[]> rotationMap = new Dictionary<string, string[]>
        {
            { "positiveX", new[] { "positiveX", "positiveZ", "negativeX", "negativeZ" } },
            { "negativeX", new[] { "negativeX", "negativeZ", "positiveX", "positiveZ" } },
            { "positiveZ", new[] { "positiveZ", "negativeX", "negativeZ", "positiveX" } },
            { "negativeZ", new[] { "negativeZ", "positiveX", "positiveZ", "negativeX" } },
            { "positiveY", new[] { "positiveY", "positiveY", "positiveY", "positiveY" } },
            { "negativeY", new[] { "negativeY", "negativeY", "negativeY", "negativeY" } }
        };

        int index = (rotation / 90) % 4;
        return rotationMap[direction][index];
    }
}
