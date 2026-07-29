using System.Collections.Generic;
using UnityEngine;
using Mirror;
using AdminToys;
using System.Linq;

namespace GrannySCPSL.Graph
{
    public static class Visualizer
    {
        private static List<GameObject> _visuals = new List<GameObject>();

        public static void SpawnNodeVisual(Node node)
        {
            var prim = CreatePrimitive(new Vector3(node.X, node.Y, node.Z), new Vector3(0.3f, 0.3f, 0.3f), Color.green, PrimitiveType.Sphere);
            if (prim != null) _visuals.Add(prim.gameObject);
        }

        public static void SpawnConnectionVisual(Node n1, Node n2)
        {
            Vector3 p1 = new Vector3(n1.X, n1.Y, n1.Z);
            Vector3 p2 = new Vector3(n2.X, n2.Y, n2.Z);
            
            float distance = Vector3.Distance(p1, p2);
            Vector3 midpoint = (p1 + p2) / 2f;
            
            var prim = CreatePrimitive(midpoint, new Vector3(0.1f, distance / 2f, 0.1f), Color.yellow, PrimitiveType.Cylinder);
            if (prim != null)
            {
                prim.transform.rotation = Quaternion.LookRotation(p2 - p1) * Quaternion.Euler(90, 0, 0);
                // In Vanilla, NetworkRotation isn't LowPrecisionQuaternion? It might just be LowPrecisionQuaternion but inside Mirror or AdminToys. We can just skip updating NetworkRotation manually and let NetworkTransform do it, but PrimitiveObjectToy handles its own sync. 
                // We'll use reflection if needed or just skip. PrimitiveObjectToy inherits from AdminToyBase which uses NetworkRotation.
                _visuals.Add(prim.gameObject);
            }
        }

        private static PrimitiveObjectToy? CreatePrimitive(Vector3 pos, Vector3 scale, Color color, PrimitiveType type)
        {
            var prefab = NetworkClient.prefabs.Values.Select(x => x.GetComponent<PrimitiveObjectToy>()).FirstOrDefault(x => x != null);
            if (prefab == null) return null;
            
            var obj = UnityEngine.Object.Instantiate(prefab.gameObject, pos, Quaternion.identity);
            var prim = obj.GetComponent<PrimitiveObjectToy>();
            prim.NetworkMovementSmoothing = 60;
            prim.NetworkPrimitiveType = type;
            prim.NetworkMaterialColor = color;
            prim.NetworkScale = scale;
            NetworkServer.Spawn(obj);
            return prim;
        }

        public static void ClearAllVisuals()
        {
            foreach (var prim in _visuals)
            {
                if (prim != null) NetworkServer.Destroy(prim);
            }
            _visuals.Clear();
        }
    }
}
