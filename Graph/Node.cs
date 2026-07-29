using System;
using System.Collections.Generic;

namespace GrannySCPSL.Graph
{
    [Serializable]
    public class Node
    {
        public int Id;
        public float X;
        public float Y;
        public float Z;
        public List<int> ConnectedNodeIds = new List<int>();

        public Node() { }

        public Node(int id, float x, float y, float z)
        {
            Id = id;
            X = x;
            Y = y;
            Z = z;
        }
    }
}
