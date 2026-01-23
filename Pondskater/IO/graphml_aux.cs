using System;
using System.Collections.Generic;
using System.Globalization;
using Rhino.Geometry;

namespace Pondskater.IO
{
    public partial class Graphml
    {
        public Graphml()
        {
        }

        /// <summary>
        /// Construct a graphml from a Rhino polyline
        /// </summary>
        /// <param name="poly">A planar rhino polyline</param>
        public Graphml(Polyline poly)
        {
            Key = new GraphmlKey[]
            {
                new GraphmlKey("vertex-coordinate-x", "string", "node", "x"),
                new GraphmlKey("vertex-coordinate-y", "string", "node", "y"),
                new GraphmlKey("edge-weight", "string", "edge", "w"),
                new GraphmlKey("edge-weight-additive", "string", "edge", "wa")
            };
            Key[2].@default = 1.0;
            Key[2].DefaultSpecified = true;
            Key[3].@default = 0.0;
            Key[3].DefaultSpecified = true;

            Graph = new GraphmlGraph(poly);
        }

        /// <summary>
        /// Construct a graphml with weights from a Rhino polyline
        /// </summary>
        /// <param name="poly">A planar polyline</param>
        /// <param name="weights">A list of edge weights</param>
        /// <param name="type">true for multiplicative weights and false for additive</param>
        public Graphml(Polyline poly, List<double> weights, bool type)
        {
            Key = new GraphmlKey[]
             {
                new GraphmlKey("vertex-coordinate-x", "string", "node", "x"),
                new GraphmlKey("vertex-coordinate-y", "string", "node", "y"),
                new GraphmlKey("edge-weight", "string", "edge", "w"),
                new GraphmlKey("edge-weight-additive", "string", "edge", "wa")
             };
            Key[2].@default = 1.0;
            Key[2].DefaultSpecified = true;
            Key[3].@default = 0.0;
            Key[3].DefaultSpecified = true;

            Graph = new GraphmlGraph(poly, weights, type);
        }

        public Polyline GraphmlToPolyline()
        {
            Polyline poly = new Polyline();

            var nodes = this.Graph.Node;

            // we visit each node and get its x and y value
            foreach(GraphmlGraphNode node in nodes)
            {
                // If GraphmlGraphNodeData is changed to a string we must parse that string to double here
                double x = Double.Parse(node.Data[0].Value, CultureInfo.InvariantCulture);
                double y = Double.Parse(node.Data[1].Value, CultureInfo.InvariantCulture);
                //double x = node.Data[0].Value;
                //double y = node.Data[1].Value;
                poly.Add(new Point3d(x, y, 0));
            }

            // if the number of edges is equal to the number of vertex the polyline is closed
            if(nodes.Length == this.Graph.Edge.Length)
            {
                poly.Add(poly[0]);
            }

            return poly;
        }

        public List<double> GetGraphmlWeights()
        {
            List<double> weights = new List<double>();

            var edges = this.Graph.Edge;

            foreach (GraphmlGraphEdge edge in edges)
            {
                weights.Add(edge.Data.Value);
            }

                return weights;
        }
    }

    public partial class GraphmlKey
    {
        public GraphmlKey()
        {
        }

        public GraphmlKey(string name, string type, string _for, string _id)
        {
            Attrname = name;
            Attrtype = type;
            @for = _for;
            Id = _id;
        }
    }

    public partial class GraphmlGraph
    {
        public GraphmlGraph()
        {

        }

        /// <summary>
        /// Construct a graphml from a Rhino polyline
        /// </summary>
        /// <param name="poly"></param>
        public GraphmlGraph(Polyline poly)
        {

            // If the polyline is closed we don't want repeated points
            int count = (poly.IsClosed) ? poly.SegmentCount : poly.Count;
            GraphmlGraphNode[] nodes = new GraphmlGraphNode[count];
            for(int i = 0; i < count; i++) 
            {
                var coords = new GraphmlGraphNodeData[]
                {
                    new GraphmlGraphNodeData("x", poly[i].X.ToString("g17", CultureInfo.InvariantCulture)),
                    new GraphmlGraphNodeData("y", poly[i].Y.ToString("g17", CultureInfo.InvariantCulture))
                    //new GraphmlGraphNodeData("x", poly[i].X),
                    //new GraphmlGraphNodeData("y", poly[i].Y)
                };
                GraphmlGraphNode vertex = new GraphmlGraphNode();
                vertex.Id = i.ToString();
                vertex.Data = coords;
                nodes[i] = vertex;
            }

            GraphmlGraphEdge[] edges = new GraphmlGraphEdge[poly.SegmentCount];
            for(int i = 0; i < poly.SegmentCount; i++)
            {
                //Should it be possible to have two weights?
                //graphmlGraphEdgeData[] weights = new graphmlGraphEdgeData[2];
                GraphmlGraphEdgeData weights = new GraphmlGraphEdgeData();
                weights.Key = "w";
                weights.Value = 1.0;
                //weights[1].key = "wa";
                //weights[1].Value = 0.0;
                GraphmlGraphEdge edge = new GraphmlGraphEdge();
                edge.Data = weights;
                edge.Source = (poly.IsClosed && i == poly.SegmentCount - 1) ? "0" : i.ToString();
                edge.Target = (poly.IsClosed && i == poly.SegmentCount - 1) ? i.ToString() : (i + 1).ToString();
                edges[i] = edge;

            }
            Edgedefault = "undirected";
            Node = nodes;
            Edge = edges;

        }

        /// <summary>
        /// Create a graph from a Polyline and a list of weights per edge
        /// </summary>
        /// <param name="poly"></param>
        /// <param name="weights">A list of weights with the same lenght as the number of edges</param>
        /// <param name="type">True for multiplicative weights and false for additive</param>
        public GraphmlGraph(Polyline poly, List<double> weights, bool type)
        {
            // If the polyline is closed we don't want repeated points
            int count = (poly.IsClosed) ? poly.SegmentCount : poly.Count;
            GraphmlGraphNode[] nodes = new GraphmlGraphNode[count];
            for (int i = 0; i < count; i++)
            {
                var coords = new GraphmlGraphNodeData[]
                {
                    // r - roundtrip will sometimes fail to produce lowercase exponent symbol
                    // g17 - although it is recommended for doubles surfer fails on very small numbers
                    // e16 - defaults to always using exponents with 3 characters and also fails
                    new GraphmlGraphNodeData("x", poly[i].X.ToString("g17", CultureInfo.InvariantCulture)),
                    new GraphmlGraphNodeData("y", poly[i].Y.ToString("g17", CultureInfo.InvariantCulture))
                    //new GraphmlGraphNodeData("x", poly[i].X),
                    //new GraphmlGraphNodeData("y", poly[i].Y)
                };
                GraphmlGraphNode vertex = new GraphmlGraphNode();
                vertex.Id = i.ToString();
                vertex.Data = coords;
                nodes[i] = vertex;
            }
            GraphmlGraphEdge[] edges = new GraphmlGraphEdge[poly.SegmentCount];
            for (int i = 0; i < poly.SegmentCount; i++)
            {
                //Should it be possible to have two weights?
                //graphmlGraphEdgeData[] weights = new graphmlGraphEdgeData[2];
                GraphmlGraphEdgeData _weights = new GraphmlGraphEdgeData();
                _weights.Key = (type) ? "w" : "wa";
                _weights.Value = weights[i];
                GraphmlGraphEdge edge = new GraphmlGraphEdge();
                edge.Data = _weights;
                edge.Source = (poly.IsClosed && i == poly.SegmentCount -1) ? "0" : i.ToString();
                edge.Target = (poly.IsClosed && i == poly.SegmentCount -1) ? i.ToString() : (i + 1).ToString();
                edges[i] = edge;

            }
            this.Edgedefault = "undirected";
            this.Node = nodes;
            this.Edge = edges;
        }
    }

    public partial class GraphmlGraphNode
    {
        public GraphmlGraphNode()
        {
            Id = String.Empty;
        }
    }

    public partial class GraphmlGraphEdge
    {
        public GraphmlGraphEdge()
        {
            Data = new GraphmlGraphEdgeData();
            Source = String.Empty;
            Target = String.Empty;
        }
    }

    public partial class GraphmlGraphNodeData
    {
        public GraphmlGraphNodeData()
        {
            Key = String.Empty;
            Value = String.Empty;
        }

        public GraphmlGraphNodeData(string key, string value)
        {
            Key = key;
            Value = value;
        }
    }

    public partial class GraphmlGraphEdgeData
    {
        public GraphmlGraphEdgeData()
        {
            Key = String.Empty;
            Value = 0D;
        }
    }
}
