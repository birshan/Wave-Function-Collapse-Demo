using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreadthFirstExpansion : MonoBehaviour
{
    public JSONReaderWithRotations jsonReader; // Reference to the JSONReaderWithRotations script
    private Dictionary<string, NamedObject> tiles; // Dictionary of all tile data from JSON
    private Dictionary<string, GameObject> prefabMap; // Dictionary mapping tile names to prefabs
    private Dictionary<Vector3Int, (GameObject instance, int rotation)> placedTiles; // Tracks placed tiles and their rotations
    // private Vector3Int gridSize = new Vector3Int(10, 10, 10); // Define the grid size
    
    // VAriables for the UI
    public int NumberOfTiles = 40; // How many number of tiles will be placed
    private int tileCount = 0;
    public float PlacementDelay = 0.5f; // Delay between tile placements
    public string StartingTile = "wall"; // Name of the starting tile
    public float AnimationSpeed = 1.0f; // Speed of the tile placement animation
    private AudioSource audioSource;
    
    private void Awake() {
        audioSource = GetComponent<AudioSource>();
    }
    void Start()
    {
        LoadTileDataFromJSONReader(); // Load tile data from JSON
        LoadPrefabs(); // Load prefabs from Resources
        StartCoroutine(GenerateLevel()); // Start the WFC algorithm
    }

    // plug to ui later
    public void BeginWFC()
    {
        StartCoroutine(GenerateLevel());
    }

    /// Load tile data from the JSONReaderWithRotations
    private void LoadTileDataFromJSONReader()
    {
        if (jsonReader == null || jsonReader.jsonFile == null)
        {
            Debug.LogError("JSONReader or its JSON file is not assigned!");
            return;
        }

        tiles = new Dictionary<string, NamedObject>();
        foreach (var obj in jsonReader.rootObject.objects)
        {
            tiles.Add(obj.name, obj);
        }
    }


    /// Load all prefabs into the prefabMap dictionary
    private void LoadPrefabs()
    {
        prefabMap = new Dictionary<string, GameObject>();
        GameObject[] prefabs = Resources.LoadAll<GameObject>("Prefabs"); // Prefabs should be in a Resources/Prefabs folder

        foreach (GameObject prefab in prefabs)
        {
            prefabMap.Add(prefab.name, prefab);
        }
    }


    // Coroutine to generate the grid using the WFC algorithm
    private IEnumerator GenerateLevel()
    {
        placedTiles = new Dictionary<Vector3Int, (GameObject instance, int rotation)>(); // Initialize placed tiles dictionary

        // Step 1: Place the first tile
        Vector3Int origin = Vector3Int.zero;
        // NamedObject firstTile = GetRandomTile();
        //get wall tile as first tile
        NamedObject firstTile = tiles["wall"];
        PlaceTile(origin, firstTile, 0);

        // Step 2: Process neighbors
        Queue<Vector3Int> cellsToProcess = new Queue<Vector3Int>();
        cellsToProcess.Enqueue(origin);

        while (cellsToProcess.Count > 0 && tileCount < NumberOfTiles)
        {
            tileCount++;
            Vector3Int currentCell = cellsToProcess.Dequeue();
            (GameObject instance, int rotation) currentTileInfo = placedTiles[currentCell];

            // Get the rotated tile data for the current tile
            NamedObject currentTileData = GetRotatedTileData(currentTileInfo.instance.name, currentTileInfo.rotation);

            foreach (var direction in GetDirections())
            {
                Vector3Int neighborPosition = currentCell + DirectionToVector(direction);

                // Skip already placed tiles
                if (placedTiles.ContainsKey(neighborPosition))
                    continue;

                // Get possible neighbors for the current tile in the given direction
                List<ObjectData> possibleNeighbors = currentTileData.data.GetNeighbors(direction);
                if (possibleNeighbors.Count == 0)
                    continue;

                // Randomly select a neighbor and place it
                ObjectData selectedNeighbor = possibleNeighbors[Random.Range(0, possibleNeighbors.Count)];
                NamedObject neighborTile = tiles[selectedNeighbor.@object];
                PlaceTile(neighborPosition, neighborTile, selectedNeighbor.rotation);
                cellsToProcess.Enqueue(neighborPosition);

                // Add delay for animation effect
                yield return new WaitForSeconds(0.5f);
            }
        }
    }


    //get random tile from the available unrotated tile data.
    private NamedObject GetRandomTile()
    {
        List<NamedObject> unrotatedTiles = new List<NamedObject>();

        foreach (var tile in tiles.Values)
        {
            // Only include tiles without rotation in their name
            if (!tile.name.Contains("_rot"))
            {
                unrotatedTiles.Add(tile);
            }
        }

        return unrotatedTiles[Random.Range(0, unrotatedTiles.Count)];
    }

    // place a tile in the grid at the specified position with the specified rotation
    // handles rotated variants correctly by using the base prefab
    private void PlaceTile(Vector3Int position, NamedObject tile, int rotation)
    {
        // Extract base prefab name (remove `_rot` suffix if present)
        string basePrefabName = tile.name.Contains("_rot") ? tile.name.Split("_rot")[0] : tile.name;

        if (!prefabMap.ContainsKey(basePrefabName))
        {
            Debug.LogError($"Prefab not found for tile: {basePrefabName}");
            return;
        }

        GameObject prefab = prefabMap[basePrefabName];
        GameObject instance = Instantiate(prefab, position, Quaternion.Euler(0, rotation, 0));

        //animation
        instance.transform.localScale = Vector3.zero;
        float audioDuration = audioSource.clip.length;
        StartCoroutine(AnimateTilePlacement(instance, audioDuration));
        placedTiles[position] = (instance, rotation); // Store the placed tile and its rotation

        //sound effect
        audioSource.Play();
    }

    private IEnumerator AnimateTilePlacement(GameObject instance, float duration)
    {
        float t = 0;
        Vector3 startScale = Vector3.zero;
        Vector3 targetScale = Vector3.one;

        while (t < duration)
        {
            // t += Time.deltaTime * AnimationSpeed;
            // instance.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            // yield return null;

            t += Time.deltaTime;
            float normalizedTime = t/(duration-0.2f); //convert to 0 to 1 range
            instance.transform.localScale = Vector3.Lerp(startScale, targetScale, normalizedTime);
            yield return null;
        }
    }
    //Get the rotated variant of a tile based on its base name and rotation.
    private NamedObject GetRotatedTileData(string baseName, int rotation)
    {
        // string rotatedName = rotation == 0 ? baseName : $"{baseName}_rot{rotation}";
        // Remove "(Clone)" if present in the base name
        string cleanName = baseName.Replace("(Clone)", "").Trim();

        // Construct the rotated variant name
        string rotatedName = rotation == 0 ? cleanName : $"{cleanName}_rot{rotation}";
        if (tiles.TryGetValue(rotatedName, out NamedObject tileData))
        {
            return tileData;
        }

        Debug.LogError($"Tile data not found for: {rotatedName}");
        return null;
    }
    // Convert a direction string to a Vector3Int offset
    private Vector3Int DirectionToVector(string direction)
    {
        return direction switch
        {
            "positiveX" => Vector3Int.right,
            "negativeX" => Vector3Int.left,
            "positiveY" => Vector3Int.up,
            "negativeY" => Vector3Int.down,
            "positiveZ" => Vector3Int.forward,
            "negativeZ" => Vector3Int.back,
            _ => Vector3Int.zero,
        };
    }

    /// Get all possible directions as an array
    private string[] GetDirections()
    {
        return new[] { "positiveX", "negativeX", "positiveY", "negativeY", "positiveZ", "negativeZ" };
    }
}
