using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class HexGrid : MonoBehaviour
{
    public int width, height;
    public Dictionary<(int, int, int), HexTile> tiles { get; private set; } = new Dictionary<(int, int, int), HexTile>();
    public HexTile[] tilesArray => tiles.Values.ToArray();
    public List<HexTile> borderTiles { get; private set; } = new List<HexTile>();
    public List<List<HexTile>> rivers { get; set; } = new List<List<HexTile>>();
    public readonly float waterLevel = 0.175f; // this will probably be moved elsewhere later
    public Color defaultColor { get; set; } = Color.white ;
    public Color touchedColor { get; set; } = Color.magenta;
    public HexTile tilePrefab;
    public Text tileLabelPrefab;

    private Mesh _hexMesh = null;
    
    public void Initialize()
    {
        if (_hexMesh == null) { GenerateHexMesh(); }
        if (tiles.Count != 0) { DestroyGrid(); }

        GenerateGrid();
        BuildBorderTileList();
        //MapGeneration.GenerateCellularAutomataMap(this);
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
            tile.ResetTile();
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
        foreach (HexTile neighborTile in tile.neighbors)
        {
            ChangeColor(neighborTile);
        }
    }

    private void ChangeColor(HexTile tile)
    {
        //both of the color-changing methods work, not sure what the benefits to each are
        tile.GetComponentInChildren<MeshRenderer>().material.color = touchedColor;
        //tile.GetComponentInChildren<MeshFilter>().mesh = GenerateHexMesh(touchedColor);

        Debug.Log("changed color of hex at " + tile.coordinates.ToString());
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

        Debug.Assert((Math.Abs(leftBound) + rightBound == width - 1) || (Math.Abs(topBound) + bottomBound == height - 1));
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
                Debug.Assert((q + r + s == 0));

                position.x = hexRadius * 3.0f / 2.0f * q;
                position.z = (hexRadius * Mathf.Sqrt(3.0f) * (r + q / 2.0f));
                tile = CreateTile(position, q, s, r);
                tiles.Add((q, s, r), tile);
                // the above SHOULD be given as (q, r, s), but the current order results in being able to use the original equation of hexRadius * Mathf.Sqrt(3.0f) * (r + q / 2.0f) for calculating the z position
                // instead of the previously used hexRadius * Mathf.Sqrt(3.0f) * (-r + -q / 2.0f). This likely shouldn't cause issues since the only thing that matters is that the coordinates stay consistent
            }
        }

        Debug.Log("Generated " + tiles.Count + " tiles");
    }

    private HexTile CreateTile(Vector3 position, int q, int r, int s)
    {
        HexTile tile = Instantiate<HexTile>(tilePrefab, position, Quaternion.identity, transform);
        tile.Initialize(q, r, s, null, this); // change how the terrain is assigned
        tile.GetComponentInChildren<MeshFilter>().mesh = _hexMesh;
        tile.GetComponentInChildren<MeshCollider>().sharedMesh = _hexMesh;
        
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

        // iterate through each triangle of the cell
        for (int i = 0; i < 6; i++)
        {
            AddTriangle(HexMetrics.cornersFlat[i], HexMetrics.cornersFlat[i + 1]);
        }

        //uvs.Add(new Vector2(0.5f, 1f));
        //uvs.Add(new Vector2(1, 0.75f));
        //uvs.Add(new Vector2(1, 0.25f));
        //uvs.Add(new Vector2(0.5f, 0));
        //uvs.Add(new Vector2(0, 0.25f));
        //uvs.Add(new Vector2(0, 0.75f));

        mesh.vertices = verts.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.colors = colors.ToArray();
        //mesh.uv = uvs.ToArray();
        mesh.name = "Hex Tile";
        mesh.RecalculateNormals();
        _hexMesh = mesh;

        void AddTriangle(Vector3 v2, Vector3 v3)
        {
            int vertexIndex = verts.Count;
            verts.Add(Vector3.zero); // center of the hex
            verts.Add(v2);
            verts.Add(v3);
            tris.Add(vertexIndex);
            tris.Add(vertexIndex + 1);
            tris.Add(vertexIndex + 2);
            colors.Add(defaultColor);
            colors.Add(defaultColor);
            colors.Add(defaultColor);
        }
    }

    private void BuildBorderTileList()
    {
        foreach (HexTile tile in tilesArray)
        {
            if (tile.neighbors.Length != 6) // add this tile to the list of map border tiles if it has fewer than 6 neighbors
            {
                borderTiles.Add(tile);
            }
        }
    }
}
