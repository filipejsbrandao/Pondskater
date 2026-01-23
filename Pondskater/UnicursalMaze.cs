using System;
using Rhino;
using Rhino.Geometry;

namespace Pondskater{

    public class DirectedNode {
        
        public int Number {get; set; } //each node will be initialized to 0, to be filled later
        public DirectedNode Next;
        public DirectedNode Previous;
        public bool IsStart { get; set; }
        public bool IsEnd { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        
        
        public DirectedNode(){
            Next = null;
            Previous = null;
            this.Number=0;
            IsStart=false;
            IsEnd=false;
        }
        
        public DirectedNode(DirectedNode next, DirectedNode previous){
            this.Next = next;
            this.Previous = previous;
            this.Number=0;
            IsStart=false;
            IsEnd=false;
        }
        
        /*
        * Need to check if it is the last, or else null pointer exception perhaps
        */
        public DirectedNode getNext(){
            return Next;
        }
        
        /*
        * Need to check if it is the first, or else null pointer exception perhaps
        */
        public DirectedNode getPrevious(){
            return Previous;
            
        }
        
        public void setStart(){
            IsStart=true;
        }

        public void setEnd(){
            IsEnd=true;
        }
        
        public void setNext(DirectedNode next){
            this.Next=next;
        }
        
        public void setPrevious(DirectedNode previous){
            this.Previous=previous;
        }
        
    }

    public class GameBoard {

        Random r= new Random();
        public int Size;
        public int SizeX { get; set; }
        public int SizeY { get; set; }
        public DirectedNode[,] Board;
        public DirectedNode Start,End;
        
        public GameBoard(int sizeX, int sizeY){
            this.SizeX = sizeX;
            this.SizeY = sizeY;
            Board = new DirectedNode[sizeX,sizeY];

        }
        public GameBoard(int size){
            this.Size=size;
            this.SizeX = size;
            this.SizeY = size;
            Board = new DirectedNode[size,size];
            InitializeBoard();
            PopulateBoard();
        }
        
        /*
        * Initially this method will create a hamiltonian walk starting at the top of the array, moving
        * right in a zig zag pattern as such :
        * 
        * 
        *            1 -- 2 -- 3 -- 4
        *                           |
        *            8 -- 7 -- 6 -- 5
        * 			  |
        * 			  9 -- 10-- 11-- 12   and so on...
        * 
        * The contents will remain null, and a populate board function will fill them, hence we can test 
        * variants of the board and print it as we go.
        */
        private void InitializeBoard(){
            bool forward = true;
            DirectedNode previous = null;
            int NodeCount = 0;
            for(int rows=0;rows<Size;rows++){
                for(int cols=0;cols<Size;cols++){
                    Board[rows,cols] = new DirectedNode();
                    Board[rows,cols].X = rows;
                    Board[rows,cols].Y = cols;
                }
            }
            Board[0,0].setStart();
            Start = Board[0,0];
            for(int rows=0;rows<Size;rows++){
                for(int cols=0;cols<Size;cols++){
                    //setting forward
                    if(forward){
                        //as long as we aren't at the end or the edge
                        if(cols<Size-1){
                            Board[rows,cols].setNext(Board[rows,cols+1]);
                            Board[rows,cols].setPrevious(previous);
                            NodeCount++;
                            
                        }
                        //now we are at the edge check to make sure we aren't at the bottom too
                        else if(rows<Size-1){
                            Board[rows,cols].setNext(Board[rows+1,cols]);
                            Board[rows,cols].setPrevious(previous);
                            NodeCount++;
                            forward = false;
                        }
                        //last case is we are at an edge and the bottom, the last node
                        else{
                            Board[rows,cols].setPrevious(previous);
                            Board[rows,cols].setEnd();
                            NodeCount++;
                            End = Board[rows,cols];
                        }
                        previous = Board[rows,cols];
                    }
                    //going other way through the columns
                    else{
                        if(cols<Size-1){
                            Board[rows,Size-1-cols].setNext(Board[rows,Size-cols-2]);
                            Board[rows,Size-1-cols].setPrevious(previous);
                            NodeCount++;
                        }
                        //now we are at the edge check to make sure we aren't at the bottom too
                        else if(rows<Size-1){
                            Board[rows,Size-1-cols].setNext(Board[rows+1,Size-1-cols]);
                            Board[rows,Size-1-cols].setPrevious(previous);
                            NodeCount++;
                            forward = true;
                        }
                        //last case is we are at an edge and the bottom, the last node
                        else{
                            Board[rows,Size-1-cols].setPrevious(previous);
                            Board[rows,Size-1-cols].setEnd();
                            NodeCount++;
                            End = Board[rows,cols];
                        }
                        previous = Board[rows,Size-1-cols];
                    }
                }
            }
        }
        
        /*
        * This will make the board random, while maintaining the hamiltonian walk.
        * It is the heart of this thing and needs some refactoring and simplifications for surrrrre.
        * Each time it's run, it will do one cut.
        */
        public void RandomizeBoard(){
            DirectedNode index;
            DirectedNode swapPoint;
            DirectedNode newEndPoint;
            bool last;
            bool cut = false;
            //chose start node or end node (1 for start 0 for 
            int rand = r.Next(2);
            if (rand == 1){
                index = Start;
                last = false;
            }
            else{
                index = End;
                last = true;
            }
            // pick a direction to move in the grid
            // 0 = up
            // 1 = down
            // 2 = left
            // 3 = right
            while (!cut){
            rand = r.Next(4);
                switch(rand){
                
                case 0:
                    //check boundries
                    if(index.X > 0){
                        //check if not connected to the above node
                        if ( (index.Previous != Board[index.X-1,index.Y]) &&( index.Next !=Board[index.X-1,index.Y])){
                            cut = true;
                            //did we come from beg or end
                            if (last){
                                SwapForward(index, Board[index.X-1,index.Y].getNext(),Board[index.X-1,index.Y] );	
                            }
                            else{
                                SwapBackward(index, Board[index.X-1,index.Y].getPrevious(), Board[index.X-1,index.Y]);
                            }
                        }
                    }
                    break;
                case 1:
                    //check boundries
                    if(index.X<Size-1){
                        //check if not connected to the above node
                        if ( (index.Previous != Board[index.X+1,index.Y]) &&( index.Next !=Board[index.X+1,index.Y])){
                            cut = true;
                            //did we come from beg or end
                            if (last){
                                SwapForward(index, Board[index.X+1,index.Y].getNext(),Board[index.X+1,index.Y] );	
                            }
                            else{
                                SwapBackward(index, Board[index.X+1,index.Y].getPrevious(), Board[index.X+1,index.Y]);
                            }
                        }
                    }
                    break;
                case 2:
                    //check boundries
                    if(index.Y>0){
                        //check if not connected to the above node
                        if ( (index.Previous != Board[index.X,index.Y-1]) &&( index.Next !=Board[index.X,index.Y-1])){
                            cut = true;
                            //did we come from beg or end
                            if (last){
                                SwapForward(index, Board[index.X,index.Y-1].getNext(),Board[index.X,index.Y-1] );	
                            }
                            else{
                                SwapBackward(index, Board[index.X,index.Y-1].getPrevious(), Board[index.X,index.Y-1]);
                            }
                        }
                    }
                    break;
                case 3:
                    //check boundries
                    if(index.Y<Size-1){
                        //check if not connected to the above node
                        if ( (index.Previous != Board[index.X,index.Y+1]) &&( index.Next !=Board[index.X,index.Y+1])){
                            cut = true;
                            //did we come from beg or end
                            if (last){
                                SwapForward(index, Board[index.X,index.Y+1].getNext(),Board[index.X,index.Y+1] );	
                            }
                            else{
                                SwapBackward(index, Board[index.X,index.Y+1].getPrevious(), Board[index.X,index.Y+1]);
                            }
                        }
                    }
                    break;	
                }
            }
        
        }
        
        public void SwapForward(DirectedNode index, DirectedNode newEndPoint, DirectedNode swapPoint){
            newEndPoint.setEnd();
            newEndPoint.setPrevious(newEndPoint.getNext());
            newEndPoint.setNext(null);
            swapPoint.setNext(index);
            while (index != newEndPoint){
                index.setNext(index.getPrevious());
                index.setPrevious(swapPoint);
                swapPoint = index;
                index = index.getNext();
            }
            End = newEndPoint;
        }
        
        public void SwapBackward(DirectedNode index, DirectedNode newEndPoint, DirectedNode swapPoint){
            newEndPoint = swapPoint.getPrevious();
            newEndPoint.setStart();
            newEndPoint.setNext(newEndPoint.getPrevious());
            newEndPoint.setPrevious(null);
            
            swapPoint.setPrevious(index);
            while (index != newEndPoint){
                index.setPrevious(index.getNext());
                index.setNext(swapPoint);
                swapPoint = index;
                index = index.getPrevious();
            }
            Start = newEndPoint;
        }
        
        public void PopulateBoard(){
            DirectedNode index = Start;
            int count = 1;
            
            do{
                index.Number = count;
                count++;
                index=index.getNext();
            }while (index.getNext()!=null);
            //for the last node
            
            index.Number = count;
        }
        /*        
        public void printBoard(){
            for (int r=0; r<Size;r++){
                for(int c=0;c<Size;c++){
                    System.out.print(" " + Board[r,c].Number + "| \t");
                }
                System.out.print("\n");
            }
        }*/
    }

    /*
    public class NumberGame {

        public static void main(String[] args) {
            // TODO Auto-generated method stub
            GameBoard theBoard;
            theBoard = new GameBoard(9);
            System.out.println("Testing board initialization : ");
            theBoard.printBoard();
            
            for (int i = 0; i<10000; i++){
                System.out.println(" Test # " + i);
                theBoard.RandomizeBoard();
                theBoard.PopulateBoard();
                theBoard.printBoard();
            }
        }

    }*/

}