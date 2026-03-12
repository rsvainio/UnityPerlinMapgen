using System;
using System.Collections.Generic;
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
        openSet.Insert(startNode);
        startNode.gScore = 0;
        startNode.hScore = strategy.Heuristic(startNode, goal);

        while (openSet.Count > 0)
        {
            PathNode current = openSet.ExtractFirst();
            if (strategy.IsGoal(current, goal))
            {
                // if this is made to work with multiple PathNode layers then a check is required here to see if this current iteration is the lowest layer
                Debug.Log("Finished pathfinding", current.tile);
                return ReconstructPath(goal);
            }

            foreach (PathNode neighbor in GetNeighbors(current))
            {
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
        do
        {
            path.Add(current);
            current = current.cameFrom;
        } while (current != null);

        path.Reverse();
        return path;
    }

    // this would currently not work with a multilayer PathNode implementation as it uses HexTile coordinates
    private List<PathNode> GetNeighbors(PathNode node)
    {
        if (node.neighbors != null) { return node.neighbors; }

        List<PathNode> neighbors = new List<PathNode>();
        HexCoordinates[] neighborCoordinates = HexMetrics.neighborVectors;
        for (int i = 0; i < neighborCoordinates.Length; i++)
        {
            if (_nodes.TryGetValue(neighborCoordinates[i].HexAdd(node.tile.coordinates).ToTuple(), out PathNode newNode))
            {
                neighbors.Add(newNode);
            }
        }
        
        node.neighbors = neighbors;
        return neighbors;
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
        foreach(KeyValuePair<(int, int, int), HexTile> entry in _grid.tiles)
        {
            _nodes.Add(entry.Key, new PathNode(entry.Value));
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
    public List<PathNode> neighbors { get; set; }

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
    float Heuristic(PathNode current, PathNode goal);
    float StepCost(PathNode current, PathNode neighbor);
    bool IsGoal(PathNode current, PathNode goal);
}

public class AStar : IPathFindingStrategy
{
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
    public override float StepCost(PathNode current, PathNode goal)
    {
        Vector3 dirFrom = current.cameFrom != null ? current.tile.coordinates.ToVec3() - current.cameFrom.tile.coordinates.ToVec3() : current.tile.coordinates.ToVec3();
        Vector3 dirTo = goal.tile.coordinates.ToVec3() - current.tile.coordinates.ToVec3();
        float dot = Vector2.Dot(dirFrom.normalized, dirTo.normalized);
        float turnWeight = 4f;
        int turnPenalty = Mathf.RoundToInt((1f - dot) * turnWeight);
        return dot > 0f ? UnityEngine.Random.Range(1, 10) + turnPenalty : 11 + turnPenalty;
    }
}

public class BreadthFirst : IPathFindingStrategy
{
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
