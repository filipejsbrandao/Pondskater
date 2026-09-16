using System;
using System.Collections.Generic;
using System.Drawing;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Pondskater
{
    public class ShiftPolyCurveSeamComponent : GH_Component
    {
        public ShiftPolyCurveSeamComponent()
          : base("Shift PolyCurve Seam", "ShiftSeam",
              "Snaps the seam of a closed polycurve to a kink and shifts it by a number of kinks.",
              "Pondskater", "Curves")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("PolyCurve", "C", "Closed polycurve whose seam will be shifted.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Steps", "S", "Kinks to move in curve direction. Zero snaps to the nearest kink.", GH_ParamAccess.item, 0);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("PolyCurve", "C", "Polycurve with its seam at the selected kink.", GH_ParamAccess.item);
            pManager.AddPointParameter("Seam", "P", "New seam point.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve source = null;
            int steps = 0;

            if (!DA.GetData(0, ref source)) return;
            if (!DA.GetData(1, ref steps)) return;

            if (!source.IsValid)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The input curve is invalid.");
                return;
            }

            if (!source.IsClosed)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "A closed polycurve must be supplied.");
                return;
            }

            List<double> kinkParameters = FindKinks(source);
            if (kinkParameters.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The curve has no tangent-discontinuous kinks.");
                return;
            }

            int snappedIndex = FindNearestKink(source, kinkParameters);
            int selectedIndex = Mod(snappedIndex + steps, kinkParameters.Count);
            double seamParameter = kinkParameters[selectedIndex];

            Curve shifted = source.DuplicateCurve();
            if (shifted == null || !shifted.ChangeClosedCurveSeam(seamParameter))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Rhino could not move the curve seam to the selected kink.");
                return;
            }

            DA.SetData(0, shifted);
            DA.SetData(1, shifted.PointAtStart);
        }

        private static List<double> FindKinks(Curve curve)
        {
            var parameters = new List<double>();
            Interval domain = curve.Domain;
            double cursor = domain.Min;
            double discontinuity;

            while (curve.GetNextDiscontinuity(
                Continuity.G1_locus_continuous,
                cursor,
                domain.Max,
                out discontinuity))
            {
                // A closed curve's end is the same locus as its start. Store it
                // as Domain.Min so the parameters remain ordered and unique.
                double parameter = Math.Abs(discontinuity - domain.Max) <= Rhino.RhinoMath.ZeroTolerance
                    ? domain.Min
                    : discontinuity;

                AddUnique(parameters, parameter);

                if (discontinuity >= domain.Max - Rhino.RhinoMath.ZeroTolerance)
                    break;

                cursor = discontinuity;
            }

            parameters.Sort();
            return parameters;
        }

        private static int FindNearestKink(Curve curve, IList<double> kinkParameters)
        {
            double totalLength = curve.GetLength();
            int nearestIndex = 0;
            double nearestDistance = double.MaxValue;

            for (int i = 0; i < kinkParameters.Count; i++)
            {
                double forwardLength = kinkParameters[i] == curve.Domain.Min
                    ? 0.0
                    : curve.GetLength(new Interval(curve.Domain.Min, kinkParameters[i]));
                double distance = Math.Min(forwardLength, totalLength - forwardLength);

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestIndex = i;
                }
            }

            return nearestIndex;
        }

        private static void AddUnique(ICollection<double> parameters, double parameter)
        {
            foreach (double existing in parameters)
            {
                if (Math.Abs(existing - parameter) <= Rhino.RhinoMath.ZeroTolerance)
                    return;
            }

            parameters.Add(parameter);
        }

        private static int Mod(int value, int modulus)
        {
            int result = value % modulus;
            return result < 0 ? result + modulus : result;
        }

        protected override Bitmap Icon => IconLoader.ShiftSeam;

        public override Guid ComponentGuid => new Guid("7A618A58-7DBD-4F70-92AD-46A751BFEC07");
    }
}
