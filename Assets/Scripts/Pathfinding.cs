using System;
using System.Collections.Generic;

public class Pathfinding
{
    private HexGrid _grid;
    private Dictionary<(int, int, int), PathNode> _nodes;

    public Pathfinding(HexGrid grid)
    {
        _grid = grid;
        _nodes = BuildNodeMap(grid);
    }

    public List<HexTile> A_star(HexTile startTile, HexTile endTile, Func<PathNode, PathNode, int> heuristic)
    {
        PathNode startNode = new PathNode(startTile);
        PathNode endNode = new PathNode(endTile);
        Heap<PathNode> openSet = new Heap<PathNode>(_grid.width * _grid.height);
        openSet.Insert(startNode);
        startNode.gScore = 0;
        startNode.hScore = heuristic(startNode, endNode);

        while (openSet.Count < 0)
        {
            PathNode current = openSet.ExtractFirst();
            if (current == endNode)
            {
                List<PathNode> nodePath = ReconstructPath(endNode);
                List<HexTile> tilePath = new List<HexTile>();
                nodePath.ForEach(x => tilePath.Add(x.tile));
                return tilePath;
            }

            foreach (PathNode neighbor in GetNeighbors(current))
            {
                // continue here
            }
        }
    }

    // different heuristic functions could be collected into a static class or something like that
    public static int Heuristic(PathNode a, PathNode b)
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
    public PathNode cameFrom { get; set; }
    public int gScore { get; set; } // the cost of the cheapest known path from start to this node
    public int hScore { get; set; } // this node's heuristic score, which naturally depends on the heuristic function used
    public int fScore => gScore + hScore; // the best guess as to how cheap a path from start to finish could be, if it passes through this node

    public PathNode(HexTile tile)
    {
        this.tile = tile;
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