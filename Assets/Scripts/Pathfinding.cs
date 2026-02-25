using System;
using System.Collections.Generic;
using UnityEngine;

public class Pathfinding
{
    private HexGrid _grid;
    private Dictionary<(int, int, int), PathNode> _nodes;

    public Pathfinding(HexGrid grid)
    {
        _grid = grid;
        _nodes = BuildNodeMap(grid);
    }

    public List<HexTile> FindPath(HexTile startTile, HexTile endTile, Func<PathNode, PathNode, int> heuristic = null)
    {
        // if this is made to work with multiple PathNode layers then 
        PathNode startNode = _nodes[startTile.coordinates.ToTuple()];
        PathNode endNode = _nodes[endTile.coordinates.ToTuple()];
        heuristic ??= CubeDistanceHeuristic;

        List<PathNode> nodePath = A_star(startNode, endNode, heuristic);
        List<HexTile> tilePath = new List<HexTile>();
        nodePath.ForEach(x => tilePath.Add(x.tile));
        ResetNodes();
        return tilePath;
    }

    private List<PathNode> A_star(PathNode startNode, PathNode endNode, Func<PathNode, PathNode, int> heuristic)
    {
        Heap<PathNode> openSet = new Heap<PathNode>(_grid.width * _grid.height);
        openSet.Insert(startNode);
        startNode.gScore = 0;
        startNode.hScore = heuristic(startNode, endNode);

        while (openSet.Count > 0)
        {
            PathNode current = openSet.ExtractFirst();
            if (current == endNode)
            {
                // if this is made to work with multiple PathNode layers then a check is required here to see if this current iteration is the lowest layer
                return ReconstructPath(endNode);
            }

            foreach (PathNode neighbor in GetNeighbors(current))
            {
                int moveCost = current.gScore + neighbor.movementCost;
                if (moveCost < neighbor.gScore || !openSet.Contains(neighbor))
                {
                    neighbor.cameFrom = current;
                    neighbor.gScore = moveCost;
                    neighbor.hScore = heuristic(neighbor, endNode);

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
        Debug.LogWarning("Target tile:", endNode.tile);
        return new List<PathNode>(); // failed to find a valid path from startNode to endNode
    }

    // different heuristic functions could be collected into a static class or something like that
    public static int CubeDistanceHeuristic(PathNode a, PathNode b)
    {
        return HexCoordinates.HexDistance(a.tile, b.tile);
    }

    private List<PathNode> ReconstructPath(PathNode endNode)
    {
        List<PathNode> path = new List<PathNode>();
        path.Add(endNode);

        PathNode current = endNode;
        while (current.cameFrom != null)
        {
            path.Add(current.cameFrom);
            current = current.cameFrom;
        }

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

    // TODO: implement this
    private Dictionary<(int, int, int), PathNode> BuildNodeMap(HexGrid grid)
    {
        throw new NotImplementedException();
    }
}

// this could be made more efficient by making it work in layers of decreasing coarseness: first layer
// consists of nodes which cover a lot of tiles, then when the most efficient path through these large nodes is calculated,
// calculate the most efficient path in the smaller nodes, and so on until you have a path consisting of actual tiles
public class PathNode : IHeapItem<PathNode>
{
    public HexTile tile { get; }
    public int heapIndex { get; set; }
    public List<PathNode> neighbors { get; set; }

    // A* properties
    public PathNode cameFrom { get; set; } = null;
    public int gScore { get; set; } = int.MaxValue; // the cost of the cheapest known path from start to this node
    public int hScore { get; set; } = 0; // this node's heuristic score, which naturally depends on the heuristic function used
    public int fScore => gScore + hScore; // the best guess as to how cheap a path from start to finish could be, if it passes through this node
    public int movementCost { get; private set; } // equal to the underlying HexTile's terrain movement cost, if no HexTile is present likely a calculated average of all the movement costs of this PathNode's HexTiles 

    public PathNode(HexTile tile)
    {
        this.tile = tile;
        movementCost = tile.terrain.movementCost;
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
