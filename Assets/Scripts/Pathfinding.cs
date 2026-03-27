using System;
using System.Collections.Generic;
using System.Linq;
using Terrain;
using UnityEngine;

public class Pathfinding
{
    private HexGrid _grid;
    private Dictionary<(int, int, int), PathNode> _nodes = new();

    public Pathfinding(HexGrid grid)
    {
        _grid = grid;
        BuildNodeMap();
    }

    public List<HexTile> FindPath(HexTile startTile, HexTile endTile, IPathFindingStrategy strategy = null)
    {
        PathNode startNode = _nodes[startTile.coordinates.ToTuple()];
        PathNode endNode = _nodes[endTile.coordinates.ToTuple()];
        strategy ??= new AStar();

        List<PathNode> nodePath = DoPathfinding(startNode, endNode, strategy);
        List<HexTile> tilePath = new List<HexTile>();
        if (nodePath.Count > 0)
        {
            nodePath.ForEach(x => tilePath.Add(x.tile));
            Debug.Assert(tilePath[0] == startTile && tilePath[tilePath.Count - 1] == endTile, "Returned path mismatch with parameter tiles");
        }
        ResetNodes();
        return tilePath;
    }

    private List<PathNode> DoPathfinding(PathNode startNode, PathNode goal, IPathFindingStrategy strategy)
    {
        Debug.Log("Starting pathfinding...", startNode.tile);
        Heap<PathNode> openSet = new Heap<PathNode>(_grid.width * _grid.height);
        Dictionary<PathNode, bool> closedSet = new Dictionary<PathNode, bool>(_grid.width * _grid.height);
        openSet.Insert(startNode);
        startNode.gScore = 0;
        startNode.hScore = strategy.Heuristic(startNode, goal);

        while (openSet.Count > 0)
        {
            PathNode current = openSet.ExtractFirst();
            closedSet[current] = true;
            if (strategy.IsGoal(current, goal))
            {
                // if this is made to work with multiple PathNode layers then a check is required here to see if this current iteration is the lowest layer
                Debug.Log("Finished pathfinding", current.tile);
                return ReconstructPath(goal);
            }

            foreach (PathNode neighbor in current.neighbors)
            {
                if (closedSet.ContainsKey(neighbor) || strategy.forbiddenTerrains.Contains(neighbor.tile.terrain)) 
                {
                    continue;
                }

                float moveCost = strategy.StepCost(current, neighbor);
                if (moveCost < neighbor.gScore)
                {
                    neighbor.cameFrom = current;
                    neighbor.gScore = moveCost;
                    neighbor.hScore = strategy.Heuristic(neighbor, goal);

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Insert(neighbor);
                    }
                    else
                    {
                        openSet.UpdateItem(neighbor);
                    }
                }
            }
        }

        Debug.LogWarning("Failed to find a valid path from tile:", startNode.tile);
        Debug.LogWarning("Target tile:", goal.tile);
        return new List<PathNode>(); // failed to find a valid path from startNode to endNode
    }

    private List<PathNode> ReconstructPath(PathNode endNode)
    {
        List<PathNode> path = new List<PathNode>();
        PathNode current = endNode;
        while (current != null)
        {
            path.Add(current);
            current = current.cameFrom;
        }

        path.Reverse();
        return path;
    }

    private void ResetNodes()
    {
        foreach (PathNode node in _nodes.Values)
        {
            node.gScore = int.MaxValue;
            node.hScore = 0;
            node.cameFrom = null;
        }
    }

    private void BuildNodeMap()
    {
        // construct a PathNode for each HexTile in the HexGrid
        foreach (KeyValuePair<(int, int, int), HexTile> entry in _grid.tiles)
        {
            _nodes.Add(entry.Key, new PathNode(entry.Value));
        }

        // calculate the neighbors for each PathNode
        foreach (PathNode node in _nodes.Values)
        {
            PathNode[] neighbors = new PathNode[node.tile.neighbors.Length];
            for (int i = 0; i < neighbors.Length; i++)
            {
                neighbors[i] = _nodes[node.tile.neighbors[i].coordinates.ToTuple()];
            }

            node.neighbors = neighbors;
        }
    }
}

// this could be made more efficient by making it work in layers of decreasing coarseness: first layer
// consists of nodes which cover a lot of tiles; when the most efficient path through these large nodes is calculated
// calculate the most efficient path in the smaller nodes, and repeat until you have a path consisting of actual tiles
public class PathNode : IHeapItem<PathNode>
{
    public HexTile tile { get; }
    public int heapIndex { get; set; }
    public PathNode[] neighbors { get; set; }

    // A* properties
    public PathNode cameFrom { get; set; } = null;
    public float gScore { get; set; } = int.MaxValue; // the cost of the cheapest known path from start to this node
    public float hScore { get; set; } = 0; // this node's heuristic score, which naturally depends on the heuristic function used
    public float fScore => gScore + hScore; // the best guess as to how cheap a path from start to finish could be, if it passes through this node
    public float movementCost { get; private set; } // equal to the underlying HexTile's terrain movement cost, if no HexTile is present likely a calculated average of all the movement costs of this PathNode's HexTiles

    public PathNode(HexTile tile)
    {
        this.tile = tile;
        movementCost = tile.movementCost;
    }

    public PathNode() 
    {
        movementCost = 0;
    }

    public int CompareTo(PathNode other)
    {
        int compare = fScore.CompareTo(other.fScore);
        if (compare == 0)
        {
            compare = hScore.CompareTo(other.hScore);
        }

        return -compare; // inverted since the heap implementation is a max heap, this should really be handled in the heap instead
    }
}

public interface IPathFindingStrategy
{
    TerrainType[] forbiddenTerrains { get; set; }

    float Heuristic(PathNode current, PathNode goal);
    float StepCost(PathNode current, PathNode neighbor);
    bool IsGoal(PathNode current, PathNode goal);
}

public class AStar : IPathFindingStrategy
{
    public TerrainType[] forbiddenTerrains { get; set; }

    public AStar(TerrainType[] forbiddenTerrains = null)
    {
        this.forbiddenTerrains = forbiddenTerrains ?? new TerrainType[0];
    }

    public virtual float Heuristic(PathNode current, PathNode goal)
    {
        return HexCoordinates.HexDistance(current.tile, goal.tile);
    }

    public virtual float StepCost(PathNode current, PathNode neighbor)
    {
        return current.gScore + neighbor.movementCost;
    }

    public virtual bool IsGoal(PathNode current, PathNode goal)
    {
        return current.tile == goal.tile;
    }
}

public class Dijkstra : AStar
{
    public override float Heuristic(PathNode current, PathNode goal)
    {
        return 0;
    }
}

public class AsTheCrowFlies : AStar
{
    public override float StepCost(PathNode current, PathNode neighbor)
    {
        return 1;
    }
}

public class MountainStrategy : AStar
{
    public MountainStrategy(TerrainType[] forbiddenTerrains) : base(forbiddenTerrains) { }
    private Dictionary<PathNode, float> _nodeNoises = new(); // the cost function needs to be deterministic so keep a single noise value for each tile

    public override float StepCost(PathNode current, PathNode neighbor)
    {
        if (!_nodeNoises.ContainsKey(neighbor))
        {
            _nodeNoises[neighbor] = UnityEngine.Random.Range(0, 5);
        }

        Vector3 dirFrom = current.cameFrom != null ? current.tile.coordinates.ToVec3() - current.cameFrom.tile.coordinates.ToVec3() : Vector3.zero;
        Vector3 dirTo = neighbor.tile.coordinates.ToVec3() - current.tile.coordinates.ToVec3();
        float dot = Vector3.Dot(dirFrom.normalized, dirTo.normalized);

        return dot > 0f ? _nodeNoises[neighbor] : 10;
    }
}

public class RiverStrategy : IPathFindingStrategy
{
    public TerrainType[] forbiddenTerrains { get; set; }

    public RiverStrategy(MapGeneration mapGen, TerrainType[] forbiddenTerrains = null)
    {
        _mapGen = mapGen;
        this.forbiddenTerrains = forbiddenTerrains ?? new TerrainType[0];
    }

    private MapGeneration _mapGen;

    public float Heuristic(PathNode current, PathNode goal)
    {
        //return 1f;
        return _mapGen.oceanDistanceMap[current.tile.coordinates.ToTuple()] * 0.2f;
    }

    // strongly penalise going uphill
    public float StepCost(PathNode current, PathNode neighbor)
    {
        float heightDifference = neighbor.tile.altitude - current.tile.altitude;
        float cost = 1f;

        if (heightDifference > 0)
        {
            cost += heightDifference * 10f;
        }
        else
        {
            cost += heightDifference;
        }

        Vector3 dirFrom = current.cameFrom != null ? current.tile.coordinates.ToVec3() - current.cameFrom.tile.coordinates.ToVec3() : Vector3.zero;
        Vector3 dirTo = neighbor.tile.coordinates.ToVec3() - current.tile.coordinates.ToVec3();
        float alignment = Vector3.Dot(dirFrom.normalized, dirTo.normalized);
        cost -= alignment * 0.2f;

        return Mathf.Max(0.1f, cost);
    }

    // TODO: figure out a way to implement an early-exit here, as currently it causes rivers generated with this strategy to go across each other and to travel through water bodies
    public bool IsGoal(PathNode current, PathNode goal)
    {
        return current == goal;
    }
}

public class BreadthFirst : IPathFindingStrategy
{
    public TerrainType[] forbiddenTerrains { get; set; }

    public float Heuristic(PathNode current, PathNode goal)
    {
        throw new NotImplementedException();
    }

    public float StepCost(PathNode current, PathNode neighbor)
    {
        throw new NotImplementedException();
    }

    public bool IsGoal(PathNode current, PathNode goal)
    {
        throw new NotImplementedException();
    }
}
