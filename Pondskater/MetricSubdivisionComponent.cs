using System;
using System.Collections.Generic;
using System.Drawing;

using Grasshopper.Kernel;

namespace Pondskater
{
    public class MetricSubdivisionComponent : GH_Component
    {
        public MetricSubdivisionComponent()
          : base("Metric Subdivision", "MSub",
              "Subdivides a length by maximizing the use of a preferred maximum module length while enforcing a minimum allowable remainder length.",
              "Pondskater", "Subdivision")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Length", "L", "Total length to subdivide.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Max Length", "Max", "Preferred maximum module length.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Min Length", "Min", "Minimum acceptable remainder or end-segment length.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Centers", "C", "Segment center positions measured from zero.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Lengths", "Ls", "Segment lengths.", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Count", "N", "Number of segments.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double totalLength = 0.0;
            double maxLength = 0.0;
            double minLength = 0.0;

            if (!DA.GetData(0, ref totalLength)) return;
            if (!DA.GetData(1, ref maxLength)) return;
            if (!DA.GetData(2, ref minLength)) return;

            if (totalLength <= 0.0 || maxLength <= 0.0 || minLength <= 0.0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Length, Max Length, and Min Length must all be greater than zero.");
                return;
            }

            if (maxLength < minLength)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Max Length cannot be smaller than Min Length.");
                return;
            }

            if (totalLength < minLength)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Length is smaller than the minimum allowable segment length.");
                return;
            }

            var centers = new List<double>();
            var lengths = new List<double>();

            if (totalLength <= maxLength)
            {
                centers.Add(totalLength / 2.0);
                lengths.Add(totalLength);
                DA.SetDataList(0, centers);
                DA.SetDataList(1, lengths);
                DA.SetData(2, 1);
                return;
            }

            int fullCount = (int)Math.Floor(totalLength / maxLength);
            double remainder = totalLength - fullCount * maxLength;

            if (NearlyZero(remainder))
            {
                AddSegments(centers, lengths, 0.0, fullCount, maxLength);
                DA.SetDataList(0, centers);
                DA.SetDataList(1, lengths);
                DA.SetData(2, lengths.Count);
                return;
            }

            if (remainder >= minLength)
            {
                AddSegments(centers, lengths, 0.0, fullCount, maxLength);
                lengths.Add(remainder);
                centers.Add(fullCount * maxLength + remainder / 2.0);

                DA.SetDataList(0, centers);
                DA.SetDataList(1, lengths);
                DA.SetData(2, lengths.Count);
                return;
            }

            if (fullCount == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No valid subdivision exists under the selected subdivision policy.");
                return;
            }

            double tailLength = maxLength + remainder;
            double repairedEnd = tailLength / 2.0;
            if (repairedEnd < minLength)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The remainder cannot be repaired into valid end segments under the selected subdivision policy.");
                return;
            }

            AddSegments(centers, lengths, 0.0, fullCount - 1, maxLength);
            double offset = (fullCount - 1) * maxLength;
            centers.Add(offset + repairedEnd / 2.0);
            centers.Add(offset + repairedEnd + repairedEnd / 2.0);
            lengths.Add(repairedEnd);
            lengths.Add(repairedEnd);

            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "One full segment was absorbed so the remainder could be repaired into two equal end segments.");

            DA.SetDataList(0, centers);
            DA.SetDataList(1, lengths);
            DA.SetData(2, lengths.Count);
        }

        protected override Bitmap Icon => IconLoader.MetricSubdivision;

        public override Guid ComponentGuid
        {
            get { return new Guid("0D0B5BDC-6CAC-4347-9C69-7332E3785B2E"); }
        }

        private static void AddSegments(List<double> centers, List<double> lengths, double start, int count, double segmentLength)
        {
            for (int i = 0; i < count; i++)
            {
                lengths.Add(segmentLength);
                centers.Add(start + i * segmentLength + segmentLength / 2.0);
            }
        }

        private static bool NearlyZero(double value)
        {
            return Math.Abs(value) <= 1e-9;
        }
    }
}
