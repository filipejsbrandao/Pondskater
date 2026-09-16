# Graph Report - Pondskater  (2026-09-12)

## Corpus Check
- 49 files · ~30,784 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 606 nodes · 1030 edges · 26 communities (24 shown, 2 thin omitted)
- Extraction: 98% EXTRACTED · 2% INFERRED · 0% AMBIGUOUS · INFERRED: 25 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `1496fd1a`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- GH_Component
- ConvexPolygon2D
- ComponentGeometry2D
- Pondskater
- SurferNative
- HamiltonianPath_Component
- EulerianPath
- ipe_generated.cs
- graphml_aux.cs
- BayazitPartition
- RandomHamilton__Component
- FiveSegmentArchComponent
- ExtrusionProfileComponent
- IceRayLatticeComponent
- ShiftPolyCurveSeamComponent
- BridgeCellsComponent
- ThreeSegmentArchComponent
- DirectedNode
- MetricSubdivisionComponent
- SymmetricSubdivisionComponent
- BridgeCellTreeComponent
- PondskaterInfo
- Pondskater.csproj
- TComponent
- Pondskater
- AGENTS.md

## God Nodes (most connected - your core abstractions)
1. `Pondskater` - 33 edges
2. `BayazitPartition` - 23 edges
3. `HamiltonianPath_Component` - 23 edges
4. `IceRayLatticeComponent` - 17 edges
5. `ComponentGeometry2D` - 16 edges
6. `BridgeCellTreeComponent` - 15 edges
7. `ConvexPolygon2D` - 15 edges
8. `EulerianPath` - 15 edges
9. `FiveSegmentArchComponent` - 15 edges
10. `ExtrusionProfileComponent` - 14 edges

## Surprising Connections (you probably didn't know these)
- `ipePageGroupGroup` --references--> `ipePageGroupGroupPath`  [EXTRACTED]
  IO/ipe_generated.cs → IO/ipe_extension.cs
- `ipePageGroup` --references--> `ipePageGroupPath`  [EXTRACTED]
  IO/ipe_generated.cs → IO/ipe_extension.cs

## Import Cycles
- None detected.

## Communities (26 total, 2 thin omitted)

### Community 0 - "GH_Component"
Cohesion: 0.04
Nodes (29): Bitmap, GH_InputParamManager, GH_OutputParamManager, Guid, IGH_DataAccess, _3DCPSpeed, Pondskater._3DCPUtils, Bitmap (+21 more)

### Community 1 - "ConvexPolygon2D"
Cohesion: 0.07
Nodes (27): List, Point3d, Polyline, Vector3d, BoundingRectangle2D, RectangleResult, Line, List (+19 more)

### Community 2 - "ComponentGeometry2D"
Cohesion: 0.09
Nodes (22): double, IList, List, Plane, Point3d, Vector3d, ComponentGeometry2D, IGH_DataAccess (+14 more)

### Community 3 - "Pondskater"
Cohesion: 0.06
Nodes (20): Pondskater.IO, Pondskater.Native, Pondskater, Exception, Bitmap, IconLoader, Bitmap, GH_InputParamManager (+12 more)

### Community 4 - "SurferNative"
Cohesion: 0.07
Nodes (21): DllImport, IntPtr, GraphmlSerializer, string, SurferNative, Bitmap, GH_InputParamManager, GH_OutputParamManager (+13 more)

### Community 5 - "HamiltonianPath_Component"
Cohesion: 0.13
Nodes (18): BeamCandidate, CompactGraph, GraphData, Bitmap, Curve, Dictionary, GH_InputParamManager, GH_OutputParamManager (+10 more)

### Community 6 - "EulerianPath"
Cohesion: 0.10
Nodes (20): AdjacentEdge, CurveGraph, Bitmap, Curve, Dictionary, GH_InputParamManager, GH_OutputParamManager, Guid (+12 more)

### Community 7 - "ipe_generated.cs"
Cohesion: 0.13
Nodes (23): float, Line, ipePageGroupGroupPath, ipePageGroupPath, bool, int, string, Ipe (+15 more)

### Community 8 - "graphml_aux.cs"
Cohesion: 0.13
Nodes (18): List, Polyline, Graphml, GraphmlGraph, GraphmlGraphEdge, GraphmlGraphEdgeData, GraphmlGraphNode, GraphmlGraphNodeData (+10 more)

### Community 9 - "BayazitPartition"
Cohesion: 0.28
Nodes (3): List, BayazitPartition, Vector2d

### Community 10 - "RandomHamilton__Component"
Cohesion: 0.13
Nodes (12): Bitmap, GH_InputParamManager, GH_OutputParamManager, Guid, IGH_DataAccess, RandomHamilton__Component, Polyline, Random (+4 more)

### Community 11 - "FiveSegmentArchComponent"
Cohesion: 0.15
Nodes (12): Arc, Bitmap, double, GH_InputParamManager, GH_OutputParamManager, Guid, IGH_DataAccess, IList (+4 more)

### Community 12 - "ExtrusionProfileComponent"
Cohesion: 0.16
Nodes (13): Bitmap, GH_InputParamManager, GH_OutputParamManager, Guid, ICollection, IGH_DataAccess, IList, List (+5 more)

### Community 13 - "IceRayLatticeComponent"
Cohesion: 0.14
Nodes (11): Bitmap, GH_InputParamManager, GH_OutputParamManager, Guid, IGH_DataAccess, int, List, Point3d (+3 more)

### Community 14 - "ShiftPolyCurveSeamComponent"
Cohesion: 0.14
Nodes (10): Bitmap, Curve, GH_InputParamManager, GH_OutputParamManager, Guid, ICollection, IGH_DataAccess, IList (+2 more)

### Community 15 - "BridgeCellsComponent"
Cohesion: 0.18
Nodes (8): Bitmap, Curve, GH_InputParamManager, GH_OutputParamManager, Guid, IGH_DataAccess, Transform, BridgeCellsComponent

### Community 16 - "ThreeSegmentArchComponent"
Cohesion: 0.13
Nodes (11): Bitmap, double, GH_InputParamManager, GH_OutputParamManager, Guid, IGH_DataAccess, IList, Point3d (+3 more)

### Community 17 - "DirectedNode"
Cohesion: 0.31
Nodes (4): int, Random, DirectedNode, GameBoard

### Community 18 - "MetricSubdivisionComponent"
Cohesion: 0.18
Nodes (7): Bitmap, GH_InputParamManager, GH_OutputParamManager, Guid, IGH_DataAccess, List, MetricSubdivisionComponent

### Community 19 - "SymmetricSubdivisionComponent"
Cohesion: 0.18
Nodes (7): Bitmap, GH_InputParamManager, GH_OutputParamManager, Guid, IGH_DataAccess, List, SymmetricSubdivisionComponent

### Community 20 - "BridgeCellTreeComponent"
Cohesion: 0.13
Nodes (13): Bitmap, Curve, GH_InputParamManager, GH_OutputParamManager, Guid, IGH_DataAccess, List, Point3d (+5 more)

### Community 21 - "PondskaterInfo"
Cohesion: 0.22
Nodes (7): GH_AssemblyInfo, GH_AssemblyPriority, GH_LoadingInstruction, Bitmap, Guid, PondskaterCategoryIcon, PondskaterInfo

### Community 22 - "Pondskater.csproj"
Cohesion: 0.33
Nodes (5): net48, net7.0, Grasshopper (8.0.23164.14305-wip), System.Drawing.Common (7.0.0), Microsoft.NET.Sdk

### Community 23 - "TComponent"
Cohesion: 0.25
Nodes (5): Bitmap, GH_InputParamManager, GH_OutputParamManager, Guid, TComponent

## Knowledge Gaps
- **7 isolated node(s):** `net7.0`, `net48`, `Grasshopper (8.0.23164.14305-wip)`, `System.Drawing.Common (7.0.0)`, `Microsoft.NET.Sdk` (+2 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **2 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Pondskater` connect `Pondskater` to `GH_Component`, `ConvexPolygon2D`, `ComponentGeometry2D`, `EulerianPath`, `graphml_aux.cs`, `RandomHamilton__Component`, `IceRayLatticeComponent`, `ShiftPolyCurveSeamComponent`, `BridgeCellsComponent`, `ThreeSegmentArchComponent`, `DirectedNode`, `MetricSubdivisionComponent`, `SymmetricSubdivisionComponent`, `BridgeCellTreeComponent`, `PondskaterInfo`, `TComponent`?**
  _High betweenness centrality (0.280) - this node is a cross-community bridge._
- **Why does `HamiltonianPath_Component` connect `HamiltonianPath_Component` to `GH_Component`, `Pondskater`?**
  _High betweenness centrality (0.102) - this node is a cross-community bridge._
- **Why does `EulerianPath` connect `EulerianPath` to `GH_Component`?**
  _High betweenness centrality (0.096) - this node is a cross-community bridge._
- **What connects `net7.0`, `net48`, `Grasshopper (8.0.23164.14305-wip)` to the rest of the system?**
  _7 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `GH_Component` be split into smaller, more focused modules?**
  _Cohesion score 0.0425531914893617 - nodes in this community are weakly interconnected._
- **Should `ConvexPolygon2D` be split into smaller, more focused modules?**
  _Cohesion score 0.06859903381642513 - nodes in this community are weakly interconnected._
- **Should `ComponentGeometry2D` be split into smaller, more focused modules?**
  _Cohesion score 0.09494949494949495 - nodes in this community are weakly interconnected._