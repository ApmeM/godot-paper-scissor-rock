using BrainAI.Pathfinding.AStar;
using Godot;
using System;
using System.Collections.Generic;

namespace IsometricGame.Business.Utils
{
    public class MapGraphData : IAstarGraph<Vector2>
    {
        public HashSet<Vector2> WeightedNodes = new HashSet<Vector2>();
        public int DefaultWeight = 1;
        public int WeightedNodeWeight = 5;

        public static readonly Vector2[] CardinalDirs = {
            new Vector2( 1, 0 ),
            new Vector2( 0, -1 ),
            new Vector2( -1, 0 ),
            new Vector2( 0, 1 ),
        };

        public static readonly Vector2[] CompassDirs = {
            new Vector2( 1, 0 ),
            new Vector2( 1, -1 ),
            new Vector2( 0, -1 ),
            new Vector2( -1, -1 ),
            new Vector2( -1, 0 ),
            new Vector2( -1, 1 ),
            new Vector2( 0, 1 ),
            new Vector2( 1, 1 ),
        };

        public HashSet<Vector2> Walls = new HashSet<Vector2>();

        public Rect2 MapSize { get; set; } = new Rect2();

        private readonly Vector2[] dirs = CardinalDirs;
        private readonly List<Vector2> neighbors = new List<Vector2>(4);

        public bool IsNodeInBounds(Vector2 node)
        {
            return MapSize.HasPoint(node);
        }

        public bool IsNodePassable(Vector2 node)
        {
            return !this.Walls.Contains(node);
        }

        public IEnumerable<Vector2> GetNeighbors(Vector2 node)
        {
            this.neighbors.Clear();

            foreach (var dir in this.dirs)
            {
                var next = new Vector2(node.x + dir.x, node.y + dir.y);
                if (this.IsNodeInBounds(next) && this.IsNodePassable(next))
                    this.neighbors.Add(next);
            }

            return this.neighbors;
        }

        public int Cost(Vector2 from, Vector2 to)
        {
            return this.WeightedNodes.Contains(to) ? this.WeightedNodeWeight : this.DefaultWeight;
        }

        public int Heuristic(Vector2 node, Vector2 goal)
        {
            return (int)Math.Abs(node.x - goal.x) + (int)Math.Abs(node.y - goal.y);
        }

    }
}
