# Component geometry smoke checks

Run from the repository root:

```sh
dotnet run --project tests/ComponentGeometrySmoke/ComponentGeometrySmoke.csproj -p:UseSharedCompilation=false
```

The harness compiles the production `ComponentGeometry2D.cs` and uses RhinoCommon's
managed vector operations without a running Rhino instance. It checks L-joint
extents for perpendicular and oblique arms, both windings, swapped arm/width
pairs, unequal widths and different construction-plane normals. The nearest of
the two joint corners must leave the requested minimum clearance to the arm cap.

This does not exercise Grasshopper data access or preview; verify the built L
component in Rhino separately.
