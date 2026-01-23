# Limitations
Surfer does not support overlapping points. If a polyline contains two points with the same coordinates these are ignored, even if they are coherently connected. 
Currently, the roofer component can only handle at most one vertical slope (0 weight).
Although the possibility of additive weights is discussed in published articles by the authors, it is not clear how these work. The feature appears to be implemented but adding additive weights in the graphml input file does not produce any visible results. According the 2017, the additive weights would have the effect of "delaying" the start of the expansion of the wavefront. In pratical terms, this would allow the possibility of having multiple vertical walls and combining different starting heights for the roof.

# Implementation 
Attempting to offset open polylines inwards will cause an error.
spiral.graphml causes surfgui to crash...

# How do multiplicative weights work?
Multiplicative weights determine the slopes of the roof. A weight of 1 is equal to a 45degrees slope, while weight 0 represents a vertical wall or a 90 degree angle with the floor. The weights can be obtained from the roof angle by the cotangent function. If more than one slope has a weight of 0, the algorithm will not work.
Another way of understanding the weight is to think of it as the proportion between the height risen and the distance ran. It implies the number of units that rise in one meter.

# How do addtive weights work?
Who knows?

## TODO
1. Implement the offsetting capabilites of surfer2 in 2D -> The component must parse the a string.../ currently we can't see the offsetted polylines
2. Implement offsetting coupled with slopes -> in a separate component
3. Create the c++ project and the PInvoke calls to the surf library
4. Add a plane input to the GH components to control the orientation of the polylines - This is important for the roof component

1. Offseting is implemented but the string parsing is not implemented yet

# PInvoke
1. First step is to be able to compile the surfer project with Visual Studio
2. Create a solution with 3 projects a C++ project, a C# project for win and a C# project for Mac
3. Add PInvoke methods to C++ project following McNeel tutorial.

