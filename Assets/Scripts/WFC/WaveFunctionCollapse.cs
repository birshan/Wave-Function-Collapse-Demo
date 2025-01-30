using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WaveFunctionCollapse: MonoBehaviour
{
    public JSONReaderWithRotations jsonReader; // Reference to the JSONReaderWithRotations script
    private Dictionary<string, NamedObject> tiles; // Dictionary of all tile data from JSON
    private Dictionary<string, GameObject> prefabMap; // Dictionary mapping tile names to prefabs
    private Dictionary<Vector3Int, (GameObject instance, int rotation)> placedTiles; // Tracks placed tiles and their rotations
    // private Vector3Int gridSize = new Vector3Int(10, 10); // Define the grid size
    public int Dimension = 20; //10 by 10 grid 2Dimensional
    public int NumberOfTiles = 40; // How many number of tiles will be placed
    
    //store the possible tiles for each position in grid
    // private List<List<List<string>>> possibleTiles;
    // private NamedObject[,,] possibleTiles; //  A multidimensional array of fixed size of the grid size - it will store the superpositions of each cell 
    
    // Need a 3d Array that can store a list of NamedObjects at each position for superposition
    // private List<NamedObject>[,] possibleTiles;
    private Dictionary<Vector3Int, List<string>> cellPossibilities; // We dont need a 2d array as each cell is unit size in unity.. so we can just loop 3 times, 1 for each axis, and since each prefab is 1meter cube, we can just use the vector3int as the key
    private int tileCount = 0;
    private Queue<Vector3Int> cellsToProcess = new Queue<Vector3Int>(); // Queue of cells to process
    // Start is called before the first frame update
    void Start()
    {
        LoadTileDataFromJSONReader(); // Load tile data from JSON
        LoadPrefabs(); // Load prefabs from Resources

        // Initialize the possibleTiles array
        // InitializePossibleTileSuperPositions();
        InitializePossibleTileSuperPositions();
        StartCoroutine(GenerateWFC()); // Start the WFC algorithm
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    

    // private void InitializePossibleTileSuperPositions()
    // {
    //     // Initialize the possibleTiles array
    //     possibleTiles = new List<NamedObject>[dimension, dimension];
    //     //Iterate through the list and put all the possible tiles in each cell of the 2d possibleTiles array
    //     for (int x = 0; x < dimension; x++)
    //     {
    //         for (int y = 0; y < dimension; y++)
    //         {
    //             possibleTiles[x, y] = new List<NamedObject>(tiles.Values);
    //         }
    //     }
    // }

    private void InitializePossibleTileSuperPositions()
    {
        // Initialize the possibleTiles array
        cellPossibilities = new Dictionary<Vector3Int, List<string>>();
        //Iterate through the list and put all the possible tiles in each cell of the 2d possibleTiles array
        for (int x = 0; x < Dimension; x++)
        {
            for (int y = 0; y < Dimension; y++)
            {
                for (int z = 0; z < Dimension; z++)
                {
                    // if(y == 0)
                    cellPossibilities.Add(new Vector3Int(x, y, z), new List<string>(tiles.Keys)); //object names have the rotations appended iirc - otherwise would need to change this
                        
                    // }else{
                        //get the tower-square-arch object only cuz only the tower-square-arch object can be above the ground
                        // possibleTiles.Add(new Vector3Int(x, y, z), new List<string>{"tower-square-arch"}); // actually we dont need to do this. because once we place a wall, we will update the possible neighbors based on json, then tower-square-arch will be a valid tile for the wall on the positive Y axis.
                    // }
                    
                }
            }
        }
        Debug.Log("Initialized cell possibilities");
        // print out the cell possibilities
        foreach (var cell in cellPossibilities)
        {
            Debug.Log($"Cell: {cell.Key} has {cell.Value.Count} possibilities");
        }
    }
    private IEnumerator GenerateWFC()
    {
        placedTiles = new Dictionary<Vector3Int, (GameObject instance, int rotation)>(); // Initialize placed tiles dictionary

        //step 1: Place the first tile
        Vector3Int centerOfGrid = new Vector3Int(Dimension / 2, 0, Dimension / 2);
        NamedObject firstTile = tiles["wall"];
        PlaceTile(centerOfGrid, firstTile); //I removed the third parameter rotation because we are using named objects here, and we append the rot to the name in propagate constraints anyway. Named Objects has appended rotations.
        CollapseCell(centerOfGrid, firstTile);

        while (cellsToProcess.Count > 0 && tileCount < NumberOfTiles)
        {
            tileCount++;
            Vector3Int currentCell = GetLowestEntropyCell();
            NamedObject tile = GetValidTile(currentCell);
            PlaceTile(currentCell, tile);
            CollapseCell(currentCell, tile);
            yield return null;
        }
        
    }
    private NamedObject GetValidTile(Vector3Int cellPosition)
    {
        List<string> superposition = cellPossibilities[cellPosition];
        string tileName = superposition[Random.Range(0, superposition.Count)];
        return tiles[tileName];
    }


    //collapse cell method to collapse the superposition of the cell to a single tile that we placed already in that position
    private void CollapseCell(Vector3Int cellPosition, NamedObject tile)
    {
        //get the superposition of the cell
        List<string> superposition = cellPossibilities[cellPosition];
        //remove all the tiles that are not equal to the tile that we placed in that position
        superposition.RemoveAll(t => t != tile.name);

        //propagate the changes to the neighbors
        // Get the directions

        //--------------------------------Get current rotation and the data for the specific tile in that specific rotation------------------
        (GameObject instance, int rotation) currentTileInfo = placedTiles[cellPosition];
        NamedObject currentTileData = GetRotatedTileData(currentTileInfo.instance.name, currentTileInfo.rotation);
        //yES get rotated tiled data does append rotation to the name BUT the getNeighbours .. is getting from the object data so its just the object name. 
        // Therefore there wont be an issue in also appending name in PropagateChangesToCellPossibilities method ? 
        //current tile data uses the NameWithRotation to get the correct neighboirs.


        //Get valid neighbours in each direction
        foreach (var direction in GetDirections())
        {
            Vector3Int neighborPosition = cellPosition + DirectionToVector(direction);
            // Skip already placed tiles
            if (placedTiles.ContainsKey(neighborPosition))
                continue;

            List<ObjectData> possibleNeighbors = currentTileData.data.GetNeighbors(direction);
            //also get rotation for each possible neighbor
            
            PropagateChangesToCellPossibilities(neighborPosition, possibleNeighbors);

        }
        //--------------------------------Get current rotation and the data for the specific tile in that specific rotation------------------
    }

    private void PropagateChangesToCellPossibilities(Vector3Int neighbourCellPosition, List<ObjectData> possibileTilesForNeighbour)
    {
        // Get the superposition of the cell
        // List<string> superposition = cellPossibilities[neighbourCellPosition];
        // // Remove all tiles that are not in the possibleNeighbors list
        // superposition.RemoveAll(t => !possibileTilesForNeighbour.Any(n => n.@object == t));

        // superposition list should equal to the names in the possibileTilesForNeighbour list
        List<string> superposition = possibileTilesForNeighbour.Select(n => n.@object).ToList();
        List<int> rotations = possibileTilesForNeighbour.Select(n => n.rotation).ToList();
        
        //append the rotation to the name
        for (int i = 0; i < superposition.Count; i++)
        {
            if(rotations[i] != 0)
            superposition[i] = $"{superposition[i]}_rot{rotations[i]}";
        }
        // If the superposition is empty, we have a contradiction
        if (superposition.Count == 0)
        {
            Debug.Log("There were no valid tiles for the neighbor at position: " + neighbourCellPosition);
            // MAYBE WE CAN PLACE AN EMPTY TILE HERE ? OR JUST SKIP THIS CELL AND CONTINUE
            return;
        }
        //print the count of the superposition
        Debug.Log($"Superposition count for cell at position: {neighbourCellPosition} is {superposition.Count}");
        // If the superposition has changed, add the cell to the queue for processing
        if (superposition.Count < cellPossibilities[neighbourCellPosition].Count)
        {
            cellsToProcess.Enqueue(neighbourCellPosition);
            Debug.Log($"Adding cell at position: {neighbourCellPosition} to the queue for processing for propogation of constraints");
        }
        // Update the superposition for the cell
        cellPossibilities[neighbourCellPosition] = superposition;

    }

    private Vector3Int GetLowestEntropyCell()
    {
        // Find the cell with the fewest possible tiles
        // return cellsToProcess
        //     .OrderBy(cell => cellPossibilities[cell].Count) // Sort by number of possibilities (entropy)
        //     .ThenBy(_ => Random.value) // Break ties randomly
        //     .FirstOrDefault();

        //find the cell with fewest possible tiles amongst the empty cells
        return cellPossibilities
            .Where(cell => !placedTiles.ContainsKey(cell.Key)) // Filter out already placed tiles
            .OrderBy(cell => cell.Value.Count) // Sort by number of possibilities (entropy)
            .ThenBy(_ => Random.value) // Break ties randomly
            .FirstOrDefault().Key;


        
        // return cellsToProcess
        //     .OrderBy(cell => possibleTiles[cell].Count) // Sort by number of possibilities (entropy)
        //     .ThenBy(_ => Random.value) // Break ties randomly
        //     .FirstOrDefault();
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
    
    private void PlaceTile(Vector3Int Position, NamedObject tile)
    {
        // Extract base prefab name (remove `_rot` suffix if present)
        string basePrefabName = tile.name.Contains("_rot") ? tile.name.Split("_rot")[0] : tile.name;
        //extraxt rotation from the name
        int rotation = tile.name.Contains("_rot") ? int.Parse(tile.name.Split("_rot")[1]) : 0;

        GameObject prefab = prefabMap[basePrefabName];
        GameObject instance = Instantiate(prefab, Position, Quaternion.Euler(0, rotation, 0));

        //print out which key we are adding to placedTiles
        Debug.Log($"PLACEDTILES DICTIONARY - Adding tile at position: {Position} with rotation: {rotation}");
        placedTiles.Add(Position, (instance, rotation));

    }

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
}
