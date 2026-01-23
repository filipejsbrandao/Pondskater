using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Rhino;
using Rhino.Geometry;

namespace Pondskater{
    
    #region Helper Classes
    public class Node {
        
        public int X { get; set; }
        public int Y { get; set; }
        public Node Next { get; set; }
        public Node Previous { get; set; }
        public bool IsStart { get; set; }
        public bool IsEnd { get; set; }
        
        public Node(int x, int y) {
            this.X = x;
            this.Y = y;
            Next = null;
            Previous = null;
            IsStart = false;
            IsEnd = false;
        }      
    }

    public enum From
    {
        LEFT,
        BOTTOM,
        RIGHT,
        UP
    }
    /// <summary>
    /// An utility 2d vector like class for comparing nodes 
    /// </summary>
    public class Direction{
        public int X { get; set; }
        public int Y { get; set; }
        public From Orientation { get; private set; }
        /// <summary>
        /// Constructs a direction object given two neighbour nodes
        /// </summary>
        /// <param name="a">Node a</param>
        /// <param name="b">Node b</param>
        public Direction(Node a, Node b){
            X = a.X - b.X;
            Y = a.Y - b.Y;
            Orientation = (X == 0)? (Y > 0)? From.BOTTOM : From.UP : (X > 0)? From.LEFT : From.RIGHT;
        }
        /// <summary>
        /// Private constructor for creating a direction object from coordinates
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private Direction(int x, int y){
            X = x;
            Y = y;
            Orientation = (X == 0)? (Y > 0)? From.BOTTOM : From.UP : (X > 0)? From.LEFT : From.RIGHT;
        }
        /// <summary>
        /// Determines if two directions are parallel, anti-parallel or not parallel
        /// Parallel indicator: +1 = both vectors are parallel. 
        /// 0 = vectors are not parallel or at least one of the vectors is zero. 
        /// -1 = vectors are anti-parallel.
        /// </summary>
        /// <param name="a">Direction a</param>
        /// <param name="b">Direction b</param>
        /// <returns></returns>
        public int IsParallel(Direction a, Direction b){
            // Check if either vector is a zero vector
            if ((a.X == 0 && a.Y == 0) || (b.X == 0 && b.Y == 0))
            {
                return 0;  // Zero vector case
            }

            // Calculate the cross product of a and b
            int crossProduct = a.X * b.Y - a.Y * b.X;

            // If cross product is not zero, vectors are not parallel
            if (crossProduct != 0)
            {
                return 0;  // Vectors are not parallel
            }

            // Calculate the dot product of a and b
            int dotProduct = a.X * b.X + a.Y * b.Y;

            // If dot product is positive, vectors are parallel
            if (dotProduct > 0)
            {
                return 1;  // Vectors are parallel
            }

            // If dot product is negative, vectors are anti-parallel
            if (dotProduct < 0)
            {
                return -1;  // Vectors are anti-parallel
            }

            return 0;  // Should not reach here
        }
        /// <summary>
        /// Returns the Counter-Clockwise perpendicular direction to a given direction in 2d
        /// </summary>
        /// <returns>ccw perpendicular direction of this direction</returns>
        public Direction CCwPerpVector(){
            return new Direction(-this.Y, this.X);
        }
        /// <summary>
        /// Returns the Clockwise perpendicular direction to a given direction in 2d
        /// </summary>
        /// <returns>cw perpendicular direction of this direction</returns>
        public Direction CwPerpVector(){
            return new Direction(this.Y, -this.X);
        }
    }
    #endregion

    #region Hamilton Path Generator class
    public class HPath {
        public Node Start {get; set;}
        public Node End {get;set;}
        public Node[,] Grid {get;set;}
        
        /// <summary>
        /// Constructor for Hamiltonian path class
        /// </summary>
        /// <param name="length">the number of collumns in the pattern (Z direction)</param>
        /// <param name="width">the number of rows in the pattern (Y direction)</param>
        /// <param name="horizontal">Determines whether the seed simple hamiltonian path is horizontal (true) or vertical (false)</param>
        /// <param name="increasingX">If false the collumns are created before the start point</param>
        /// <param name="increasingY">If false the rows are created before the start point</param>
        public HPath(int length, int width, bool horizontal, bool increasingX, bool increasingY) {
            
            Grid = new Node[length, width];
            Start = new Node(0,0);
            Node currentNode = Start;
            Node previousNode = Start;
            
            int incrementX = (increasingX)? 1 : -1;
            int incrementY = (increasingY)? 1 : -1;
            
            if(horizontal){
            
                for(int j = 0; j < width; j++){
                    int y = j * incrementY;
                    for(int i = 0; i < length; i++){
                        int x = (j % 2 == 0)? i * incrementX : (length-1) * incrementX - (i * incrementX);
                        if(i == 0 && j == 0){
                            currentNode.IsStart = true;
                        }else{
                            currentNode = new Node(x,y);
                            currentNode.Previous = previousNode;
                            previousNode = currentNode;
                        }
                        
                        if(i == length -1 && j == width -1 ){
                            currentNode.IsEnd = true;
                            End = currentNode;
                        }
                        Grid[Math.Abs(x), Math.Abs(y)] = currentNode;
                    }
                }
            }else{
                for(int i = 0; i < length; i++){
                    int x = i * incrementX;
                    for(int j = 0; j < width; j++){
                        int y = (i % 2 == 0)? j * incrementY: (width-1) * incrementY - (j * incrementY);
                        if(i == 0 && j == 0){
                            currentNode.IsStart = true;
                        }else{
                            currentNode = new Node(x,y);
                            currentNode.Previous = previousNode;
                            previousNode = currentNode;
                        }

                        if(i == length -1 && j == width -1 ){
                            currentNode.IsEnd = true;
                            End = currentNode;
                        }
                        Grid[Math.Abs(x), Math.Abs(y)] = currentNode;
                    }
                }
            }
            currentNode = End;
            previousNode = End.Previous;

            while (previousNode != null)
            {
                previousNode.Next = currentNode;
                if (currentNode.IsStart) 
                    break;

                currentNode = previousNode;
                previousNode = currentNode.Previous;
            }
        }

        /// <summary>
        /// Randomnly selects a node on the grid to cut towards one of its valid neighbors if any
        /// This method may fail to find a node with valid neighbours to cut
        /// </summary>
        /// <param name="rnd">An object of type Random</param>
        public void RandomCut(Random rnd){
            int length = Grid.GetLength(0);
            int width = Grid.GetLength(1);

            // - Step 1 Select several random nodes 
            int numberNodes = length * width;
            Node[] randomNodes = new Node[4];
            for(int i = 0; i < 4; i++){
                int k = rnd.Next(1, numberNodes -1); // Get a number excluding the first and the last
                randomNodes[i] = Grid[(int) k / width, k % width];
            }
            // - Step 2 For each random node select its backward neighbour and see if there is a paralell arc to remove
            // - Check for existing connections between Node n and its 
            foreach(Node n in randomNodes){
                Node prevNode = n.Previous;
                Direction thisDir = new Direction(n, prevNode);

                Node? nCcw = GetNodeFromDir(n, thisDir, true); // n neighbour
                if (nCcw != null)
                {
                    Node validNodeB = nCcw;
                    if(n.Next != validNodeB && n.Previous != validNodeB){
                        Node? prevNodeCcw = GetNodeFromDir(prevNode, thisDir, true);
                        if(prevNodeCcw != null){
                            Node validNodeA = prevNodeCcw;
                            if(prevNode.Previous != validNodeA && prevNode.Next != validNodeA && validNodeA.Previous.X == validNodeB.X && validNodeA.Previous.Y == validNodeB.Y){
                                //We found a parallel edge
                                // Cut the edges from n to n.Previous and from validNodeA to validNodeB and reconnect 
                                n.Previous = validNodeB; 
                                validNodeB.Next = n;

                                prevNode.Next = validNodeA;
                                validNodeA.Previous = prevNode;
                                Reconnect(n, prevNode, rnd);
                                break;
                            }
                        }
                    }
                }
                Node? nCw = GetNodeFromDir(n, thisDir, false); 
                if (nCw != null)
                {
                    Node validNodeB = nCw;
                    if(n.Next != validNodeB && n.Previous != validNodeB){
                        Node? prevNodeCw = GetNodeFromDir(n.Previous, thisDir, false);
                        if(prevNodeCw != null){
                            Node validNodeA = prevNodeCw;
                            if(prevNode.Previous != validNodeA && prevNode.Next != validNodeA && validNodeA.Previous.X == validNodeB.X && validNodeA.Previous.Y == validNodeB.Y){
                                //We found a parallel edge
                                // Cut the edges from n to n.Previous and from validNodeA to validNodeB and reconnect 
                                n.Previous = validNodeB; 
                                validNodeB.Next = n;

                                prevNode.Next = validNodeA;
                                validNodeA.Previous = prevNode;
                                Reconnect(n, prevNode, rnd);
                                break;
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Reconnect method rejoins the loop and the path created by the Cut method by creating a new cut
        /// It checks all the nodes at random for a place to cut
        /// </summary>
        /// <param name="n"></param>
        /// <param name="nPrev"></param>
        /// <param name="rnd"></param>        
        public void Reconnect(Node n, Node nPrev, Random rnd){

            HashSet<Node> loop = new HashSet<Node>();
            bool right = true;
            Node current = n;
            // Test the right chain
            while(loop.Add(current) && current != null){
                current = current.Previous;

                // If we reach the start try the left chain
                if(current.IsStart) {
                    right = false;
                    loop.Clear();
                    current = nPrev;
                }
            }

            // Copy loop, remove start, shuffle candidates, and reconnect nodes
            HashSet<Node> loopCopy = new HashSet<Node>(loop);
            if(right)
                loopCopy.Remove(n);
            else
                loopCopy.Remove(nPrev);
            
            List<Node> candidates = loopCopy.OrderBy(_ => rnd.Next()).ToList();
            //Add the removed node to the end of the list to minimize the risk of undoing the cut
            if(right)
                candidates.Add(n);
            else
                candidates.Add(nPrev);
            // Loop the candidates list
            foreach (var c in candidates)
            {
                Node thisNPrev = c.Previous;
                Direction thisDir = new Direction(c, thisNPrev);
                
                // Ccw direction
                Node? nCcw = GetNodeFromDir(c, thisDir, true); // n neighbour
                //Check if c Neighbour is part of the loop
                if( nCcw != null ){
                    Node validNodeB = nCcw;
                    if(c.Next != validNodeB && n.Previous != validNodeB && !loop.Contains(validNodeB)){
                        Node? prevNodeCcw = GetNodeFromDir(thisNPrev, thisDir, true);
                        if(prevNodeCcw != null){
                            Node validNodeA = prevNodeCcw;
                            if(thisNPrev.Previous != validNodeA && thisNPrev.Next != validNodeA && validNodeA.Previous.X == validNodeB.X && validNodeA.Previous.Y == validNodeB.Y){
                                //We found a parallel edge
                                // Cut the edges from n to n.Previous and from validNodeA to validNodeB and reconnect 
                                c.Previous = validNodeB; 
                                validNodeB.Next = c;
                                thisNPrev.Next = validNodeA;
                                validNodeA.Previous = thisNPrev;
                                break;
                            }
                        }
                    }
                }

                // Cw direction
                Node? nCw = GetNodeFromDir(c, thisDir, false); // n neighbour
                //Check if c Neighbour is part of the loop
                if( nCw != null ){
                    Node validNodeB = nCw;
                    if(c.Next != validNodeB && n.Previous != validNodeB && !loop.Contains(validNodeB)){
                        Node? prevNodeCw = GetNodeFromDir(thisNPrev, thisDir, false);
                        if(prevNodeCw != null){
                            Node validNodeA = prevNodeCw;
                            if(thisNPrev.Previous != validNodeA && thisNPrev.Next != validNodeA && validNodeA.Previous.X == validNodeB.X && validNodeA.Previous.Y == validNodeB.Y){
                                //We found a parallel edge
                                // Cut the edges from n to n.Previous and from validNodeA to validNodeB and reconnect 
                                c.Previous = validNodeB; 
                                validNodeB.Next = c;
                                thisNPrev.Next = validNodeA;
                                validNodeA.Previous = thisNPrev;
                                break;
                            }
                        }
                    }
                }
            }

        }

        /// <summary>
        /// Given a node N and the direction of the chain return a CCW or CW node of this node if it exists
        /// </summary>
        /// <param name="current"></param>
        /// <param name="dir"></param>
        /// <param name="CCW"></param>
        /// <returns>Returns the neighbour node or null if it is out of bounds of the Grid</returns>
        public Node? GetNodeFromDir(Node current, Direction dir, bool CCW){
            Direction perp = (CCW)? dir.CCwPerpVector() : dir.CwPerpVector();
            int nextNodeX = Math.Abs(current.X + perp.X);
            int nextNodeY = Math.Abs(current.Y + perp.Y);
            if(nextNodeX >= 0 && nextNodeX <= this.Grid.GetLength(0) - 1 && nextNodeY >= 0 && nextNodeY <= this.Grid.GetLength(1) -1){
                return Grid[nextNodeX, nextNodeY];
            } else {
                return null;
            }
        }
        /// <summary>
        /// Utility method to convert a path into a rhinocommon polyline
        /// </summary>
        /// <returns>a polyline</returns>
        public Polyline PathToPolyline(){
            Polyline poly = new Polyline();
            Node current = Start;

            do{
                poly.Add(current.X, current.Y, 0);
                if(current.IsEnd) break;
                current = current.Next;
            }while(current != null);

            return poly;
        }
    }
    #endregion


}
