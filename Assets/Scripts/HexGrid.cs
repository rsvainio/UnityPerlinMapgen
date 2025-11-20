using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class HexGrid : MonoBehaviour
{
    public int width, height;
    public int widthMax, widthMin, heightMax, heightMin;
    public Color defaultColor = Color.white;
    public Color touchedColor = Color.magenta;
    public HexTile tilePrefab;
    public Text tileLabelPrefab;
    public Terrain[] terrainList;

    //might make this into a dictionary with the key being a TerrainType object, and the value being the Mesh for that type of terrain
    //e.g. public Dictionary<TerrainType, Mesh> terrainMeshes = new Dictionary<(TerrainType, Mesh>());
    Mesh hexMesh = null;

    public Dictionary<(int, int, int), HexTile> tiles = new Dictionary<(int, int, int), HexTile>();
    public List<HexTile> borderTiles = new List<HexTile>();
    public List<List<HexTile>> rivers = new List<List<HexTile>>();
    public float waterLevel = 0.175f; // this will probably be moved elsewhere later

    public void Initialize()
    {
        if (hexMesh == null) { GenerateHexMesh(); }
        if (tiles.Count != 0) { DestroyGrid(); }

        GenerateGrid();
        BuildBorderTileList();
        //MapGeneration.GenerateCellularAutomataMap(this);
        //ReadTerrainTypes();
    }

    public void DestroyGrid()
    {
        foreach (HexTile tile in tiles.Values)
        {
            Destroy(tile.gameObject);
        }
        borderTiles.Clear();
        rivers.Clear();
        tiles.Clear();
    }

    public void ResetGrid()
    {
        foreach (HexTile tile in tiles.Values)
        {
            tile.SetTerrain(null);
            tile.SetBiomeAttributes(0f, 0f, 0f);
        }
        rivers.Clear();
    }
    
    private void Update()
    {
        if (Input.GetMouseButton(0))
        {
            HandleInput();
        }
    }
    

    public HexTile FetchTile((int, int, int) key) 
    {
        if (tiles.TryGetValue(key, out HexTile tile))
        {
            return tile;
        }
        else
        {
            throw new ArgumentException("No tile found at coordinates " + key.ToString());
        }
    }

    public HexTile[] GetTiles()
    {
        return tiles.Values.ToArray();
    }

    private void HandleInput()
    {
        Vector3 mousePosition = Input.mousePosition;
        Ray inputRay = Camera.main.ScreenPointToRay(mousePosition);
        if (Physics.Raycast(inputRay, out RaycastHit hit))
        {
            //ChangeColor(hit.collider.GetComponentInParent<HexTile>());
            //ChangeColor(hit.collider.GetComponentInParent<HexTile>());
            ChangeClusterColor(hit.collider.GetComponentInParent<HexTile>());
            //TouchTile(hit.point);
        }
    }

    private void ChangeClusterColor(HexTile tile)
    {
        ChangeColor(tile);
        foreach (HexTile neighborTile in tile.GetNeighbors())
        {
            ChangeColor(neighborTile);
        }
    }

    private void ChangeColor(HexTile tile)
    {
        //both of the color-changing methods work, not sure what the benefits to each are
        tile.GetComponentInChildren<MeshRenderer>().material.color = touchedColor;
        //tile.GetComponentInChildren<MeshFilter>().mesh = GenerateHexMesh(touchedColor);

        Debug.Log("changed color of hex at " + tile.GetCoordinates().ToString());
    }

    //not used but works as a reference on how to implement functionality to tiles
    private void TouchTile(Vector3 position)
    {
        position = transform.InverseTransformPoint(position);
        HexCoordinates coordinates = new HexCoordinates(position);
        Debug.Log("touched at " + coordinates.ToString());
    }

    private void GenerateGrid()
    {
        int rightBound =    (int) (width) / 2;
        int leftBound =     (int)-(width - 1 - rightBound);
        int bottomBound =   (int) (height) / 2;
        int topBound =      (int)-(height - 1 - bottomBound);

        Debug.Assert((Math.Abs(leftBound) + rightBound == width - 1) || (Math.Abs(topBound) + bottomBound == height - 1)); // replaces Exception throw below
        //if ((Math.Abs(leftBound) + rightBound != width - 1) || (Math.Abs(topBound) + bottomBound != height - 1)) throw new Exception("Map boundary mismatch");
        Debug.Log("Generating grid with bounds " + leftBound + ", " + rightBound + ", " + topBound + ", " + bottomBound);

        HexTile tile;
        Vector3 position = Vector3.zero;
        float hexRadius = HexMetrics.outerRadius;

        for (int q = leftBound; q <= rightBound; q++)
        {
            int qOff = q >> 1;
            for (int r = topBound - qOff; r <= bottomBound - qOff; r++)
            {
                int s = -q - r;
                Debug.Assert((q + r + s == 0)); // replaces Exception throw below
                //if (q + r + s != 0) throw new Exception("Hex coordinate sum not equal to 0");
                position.x = hexRadius * 3.0f / 2.0f * q;

                // should look into reversing this and using the original equation
                // currently R is negative on the upper bound and positive on the lower bound, which is counter-intuitive
                position.z = hexRadius * Mathf.Sqrt(3.0f) * (-r + -q / 2.0f); //original equation was (hexRadius * Mathf.Sqrt(3.0f) * (r + q / 2.0f)) but it resulted in the r and s coordinates being reversed
                
                tile = CreateTile(position, q, r, s);
                tiles.Add((q, r, s), tile);
            }
        }

        widthMax = rightBound;
        widthMin = leftBound;
        heightMax = bottomBound;
        heightMin = topBound;

        Debug.Log("Generated " + tiles.Count + " tiles");
    }

    private HexTile CreateTile(Vector3 position, int q, int r, int s)
    {
        HexTile tile = Instantiate<HexTile>(tilePrefab, position, Quaternion.identity, transform);
        tile.Initialize(q, r, s, null, this); // change how the terrain is assigned
        tile.GetComponentInChildren<MeshFilter>().mesh = hexMesh;
        tile.GetComponentInChildren<MeshCollider>().sharedMesh = hexMesh;
        
        /*
        Text label = Instantiate<Text>(tileLabelPrefab);
        label.rectTransform.SetParent(gridCanvas.transform, false);
        label.rectTransform.anchoredPosition =
            new Vector2(position.x, position.z);
        label.text = tile.coordinates.ToStringOnSeparateLines();
        */

        return tile;
    }

    private void GenerateHexMesh()
    {
        Mesh mesh = new Mesh();

        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        List<Color> colors = new List<Color>();
        List<Vector2> uvs = new List<Vector2>();

        //the corners are in reverse order, should probably fix this - reversing the array works though
        foreach (Vector3 vert in HexMetrics.cornersFlat.Reverse()) {
            verts.Add(vert);
        }

        tris.Add(0);
        tris.Add(2);
        tris.Add(1);

        tris.Add(0);
        tris.Add(5);
        tris.Add(2);

        tris.Add(2);
        tris.Add(5);
        tris.Add(3);

        tris.Add(3);
        tris.Add(5);
        tris.Add(4);

        for(int i = 0; i < 6; i++)
        {
            colors.Add(defaultColor);
        }

        uvs.Add(new Vector2(0.5f, 1f));
        uvs.Add(new Vector2(1, 0.75f));
        uvs.Add(new Vector2(1, 0.25f));
        uvs.Add(new Vector2(0.5f, 0));
        uvs.Add(new Vector2(0, 0.25f));
        uvs.Add(new Vector2(0, 0.75f));

        mesh.vertices = verts.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.colors = colors.ToArray();
        mesh.uv = uvs.ToArray();

        mesh.name = "Hexagonal Tile";

        mesh.RecalculateNormals();
        hexMesh = mesh;
    }

    private Mesh GenerateHexMesh(Color color)
    {
        Mesh mesh = new Mesh();

        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        List<Color> colors = new List<Color>();
        List<Vector2> uvs = new List<Vector2>();

        //the corners are in reverse order, should probably fix this - reversing the array works though
        foreach (Vector3 vert in HexMetrics.cornersFlat.Reverse())
        {
            verts.Add(vert);
        }

        tris.Add(0);
        tris.Add(2);
        tris.Add(1);

        tris.Add(0);
        tris.Add(5);
        tris.Add(2);

        tris.Add(2);
        tris.Add(5);
        tris.Add(3);

        tris.Add(3);
        tris.Add(5);
        tris.Add(4);

        for (int i = 0; i < 6; i++)
        {
            colors.Add(color);
        }

        uvs.Add(new Vector2(0.5f, 1f));
        uvs.Add(new Vector2(1, 0.75f));
        uvs.Add(new Vector2(1, 0.25f));
        uvs.Add(new Vector2(0.5f, 0));
        uvs.Add(new Vector2(0, 0.25f));
        uvs.Add(new Vector2(0, 0.75f));

        mesh.vertices = verts.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.colors = colors.ToArray();
        mesh.uv = uvs.ToArray();

        mesh.name = "Hexagonal Tile";

        mesh.RecalculateNormals();
        return mesh;
        //hexMesh = mesh;
    }

    private void BuildBorderTileList()
    {
        foreach (HexTile tile in GetTiles())
        {
            if (tile.GetNeighbors().Length != 6) // add this tile to the list of map border tiles if it has fewer than 6 neighbors
            {
                borderTiles.Add(tile);
            }
        }
    }

    public void ReadTerrainTypes(){
        String filepath = "Assets/PersistentData/TileTerrains";

        if (Directory.Exists(filepath)){
            DirectoryInfo d = new DirectoryInfo(filepath);
            foreach (var file in d.GetFiles("*.json")){
                if (!file.Name.ToLower().Contains("template")){
                    var jsonData = File.ReadAllText(file.ToString());
                    Terrain terrain = Terrain.CreateFromJSON(jsonData);
                    
                    //Debug.Log(terrain.id);
                    Debug.Log(terrain.baseMovementCost);
                    Debug.Log(terrain.baseColor);
                    Debug.Log(terrain.ToString());
                }
            }
        }
    }
}