# Pondskater — Component Reference

This document lists the Grasshopper components provided by the Pondskater plugin with a concise description, inputs and outputs.

---

## Polygon Partition (PP)
- Category: Pondskater / Subdivision
- Description: Divides a simple planar polygon into non-overlapping convex components using Bayazit's convex decomposition algorithm.
- Inputs: `Polygon` — a simple closed planar polyline
- Outputs: `Convex Polygons` — list of convex subdivision polylines

## Read GraphML (GML)
- Category: Pondskater / Analysis
- Description: Convert a GraphML polyline to a Rhino polyline and extract per-edge weights.
- Inputs: `Path` — path to a .graphml file
- Outputs: `Polyline` — converted Rhino polyline; `Weights` — list of weights

## Write GraphML (Write GML)
- Category: Pondskater / Analysis
- Description: Serialize a polyline (and optional per-edge weights) to GraphML and optionally save to disk.
- Inputs: `Polyline`, `Weights`, `Type` (weight mode), `Dir`, `Filename`, `Save`
- Outputs: `Xml` — GraphML string

## Eulerian Path (E_Path)
- Category: Pondskater / Paths
- Description: Finds an Eulerian path through a connected network of curves.
- Inputs: `Curves` (list), `Start` (point), `Tolerance`
- Outputs: `Path` (ordered curves), `Exists` (bool)

## Hamiltonian Path (H_Path)
- Category: Pondskater / Paths
- Description: Finds a Hamiltonian vertex path in a connected curve network (exact DP for small graphs, beam search for larger).
- Inputs: `Curves`, `Start`, `Use Exact`, `Tolerance`
- Outputs: `Points` (vertex order), `Path` (polyline), `Full` (bool)

## Random Hamilton Path (RH_Path)
- Category: Pondskater / Paths
- Description: Generates a randomized Hamiltonian path on a grid using seed and randomization steps.
- Inputs: `Columns`, `Rows`, `Steps`, `Increment X`, `Increment Y`, `Horizontal`, `Seed`
- Outputs: `Path` (curve)

## Roofer (Rf)
- Category: Pondskater / Analysis
- Description: Build a roof mesh from a closed planar polygon using the native Surfer library.
- Inputs: `Polyline`, `Slopes` (list), `Plane`, `Type` (weight), optional `Slopes`
- Outputs: `Roof` (mesh list)

## Skeleton2d (Sk2d)
- Category: Pondskater / Analysis
- Description: Compute the 2D skeleton of a planar polygon (uses native Surfer backend); returns skeleton lines and ipe output.
- Inputs: `Polyline`, `Weights` (list), `Type` (weight), `Direction`
- Outputs: `Skeleton` (lines), `Ipe output` (string)

## Offset (O)
- Category: Pondskater / Analysis
- Description: Compute weighted offsets and skeletons for a polyline (supports complex offset specifications).
- Inputs: `Polyline`, `Weights` (list), `Plane`, `Type`, `Direction`, `Distances` (offset-spec string)
- Outputs: `Skeleton` (lines), `Offsets` (list of polylines)

## Edge Aligned Bounding Rectangle (EABR)
- Category: Pondskater / Analysis
- Description: Computes a bounding rectangle aligned to a chosen input polyline edge (from convex hull extents).
- Inputs: `Polyline`, `Plane`, `Edge Index`
- Outputs: `Rectangle`, `Area`, `Length`, `Width`, `Convex Hull`

## Minimum Bounding Rectangle (MBR)
- Category: Pondskater / Analysis
- Description: Computes a robust minimum-area bounding rectangle for a planar polygon by evaluating hull-edge-aligned candidates.
- Inputs: `Polyline`, `Plane`
- Outputs: `Rectangle`, `Area`, `Length`, `Width`, `Frame`, `Convex Hull`

## Polygon Width (PWidth)
- Category: Pondskater / Analysis
- Description: Computes the minimum width of a planar polygon and returns the two support lines for the antipodal configuration.
- Inputs: `Polyline`, `Plane`
- Outputs: `Width`, `Support A`, `Support B`, `Convex Hull`

## Metric Subdivision (MSub)
- Category: Pondskater / Subdivision
- Description: Subdivides a length maximizing use of a preferred maximum module while enforcing minimum remainder.
- Inputs: `Length`, `Max Length`, `Min Length`
- Outputs: `Centers`, `Lengths`, `Count`

## Symmetric Subdivision (SSub)
- Category: Pondskater / Subdivision
- Description: Subdivides a length distributing the remainder symmetrically to both ends.
- Inputs: `Length`, `Max Length`, `Min Length`
- Outputs: `Centers`, `Lengths`, `Count`

## N Component (NComp)
- Category: Pondskater / Subdivision
- Description: Creates a closed polyline for a multi-wall intersection component from a construction plane, wall directions, widths and minimum member lengths.
- Inputs: `Plane`, `Vectors`, `Min Member Lengths`, `Widths`, `Min Angle`
- Outputs: `Polyline`, `Leg Points`

## T Component (TComp)
- Category: Pondskater / Subdivision
- Description: Closed polyline of a T corner connection from plane, three arm directions, widths and minimum member lengths.
- Inputs: `Plane`, `Vector A`, `Vector B`, `Vector C`, `Widths`, `Min Member Lengths`
- Outputs: `Polyline`, `Vertices`

## L Component (LComp)
- Category: Pondskater / Subdivision
- Description: Closed polyline of an L corner connection from plane, two arm directions, widths and minimum member lengths.
- Inputs: `Plane`, `Vector A`, `Vector B`, `Widths`, `Min Member Lengths`
- Outputs: `Polyline`, `Vertices`

## Miter Component (Miter)
- Category: Pondskater / Subdivision
- Description: Creates closed polyline miter-joint components per wall direction from plane, wall directions, arm lengths and widths.
- Inputs: `Plane`, `Vectors`, `Arm Lengths`, `Widths`, `Min Angle`
- Outputs: `Components` (polylines), `Leg Points`

## Ice Ray Lattice (IceRay)
- Category: Pondskater / Subdivision
- Description: Subdivides a convex planar polygon into smaller polygons using an iterative Ice Ray splitting strategy.
- Inputs: `Polygon`, `Max Area`, `Cut Ratio`, `Side Ratio`, `Seed`
- Outputs: `Polygons` (list)

## 3DCP Velocity (3DCPSpeed)
- Category: Pondskater / Paths
- Description: Compute nozzle velocity and related ratios for 3D concrete printing from material flow velocity and nozzle geometry.
- Inputs: `Material Extrusion Velocity`, `Nozzle Diameter`, `Filament Width`, `Nozzle Height`
- Outputs: `Nozzle Velocity`, `Contact Width`, `Height Ratio`, `Width Ratio`

---

If you want, I can:

- add short usage examples for selected components
- generate a table of component names and categories for quick reference
- extract full input/output parameter signatures programmatically into the doc

Next: I'll mark the documentation task complete and update the todo list.
