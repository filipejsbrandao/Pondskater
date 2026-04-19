using System;
using System.Collections.Generic;
using System.Drawing;

using Grasshopper.Kernel;

namespace Pondskater
{
    public class SymmetricSubdivisionComponent : GH_Component
    {
        public SymmetricSubdivisionComponent()
          : base("Symmetric Subdivision", "SSub",
              "Subdivides a length by maximizing the use of a preferred maximum module length while distributing the remainder symmetrically to both ends.",
              "Pondskater", "Components")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Length", "L", "Total length to subdivide.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Max Length", "Max", "Preferred maximum module length.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Min Length", "Min", "Minimum acceptable end-segment length.", GH_ParamAccess.item);
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

            int initialFullCount = (int)Math.Floor(totalLength / maxLength);
            int fullCount = initialFullCount;
            double remainder = totalLength - fullCount * maxLength;

            while (fullCount > 0 && !NearlyZero(remainder) && remainder / 2.0 < minLength)
            {
                fullCount--;
                remainder = totalLength - fullCount * maxLength;
            }

            if (fullCount == 0)
            {
                if (totalLength >= minLength && totalLength <= maxLength)
                {
                    centers.Add(totalLength / 2.0);
                    lengths.Add(totalLength);
                    DA.SetDataList(0, centers);
                    DA.SetDataList(1, lengths);
                    DA.SetData(2, 1);
                    return;
                }

                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No valid subdivision exists under the selected subdivision policy.");
                return;
            }

            if (NearlyZero(remainder))
            {
                AddSegments(centers, lengths, maxLength, fullCount, 0.0);
            }
            else
            {
                double endLength = remainder / 2.0;
                if (endLength < minLength)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The remainder cannot be distributed symmetrically into valid end segments under the selected subdivision policy.");
                    return;
                }

                centers.Add(endLength / 2.0);
                lengths.Add(endLength);
                AddSegments(centers, lengths, maxLength, fullCount, endLength);
                centers.Add(endLength + fullCount * maxLength + endLength / 2.0);
                lengths.Add(endLength);
            }

            if (fullCount < initialFullCount)
            {
                AddRuntimeMessage(
                    GH_RuntimeMessageLevel.Warning,
                    "One or more full segments were absorbed so the remainder could be distributed symmetrically to both ends.");
            }

            DA.SetDataList(0, centers);
            DA.SetDataList(1, lengths);
            DA.SetData(2, lengths.Count);
        }

        protected override Bitmap Icon => IconLoader.SymmetricSubdivision;

        public override Guid ComponentGuid
        {
            get { return new Guid("97013CF8-9F60-4B2A-9A5A-11813F041E73"); }
        }

        private static void AddSegments(List<double> centers, List<double> lengths, double segmentLength, int count, double offset)
        {
            for (int i = 0; i < count; i++)
            {
                lengths.Add(segmentLength);
                centers.Add(offset + i * segmentLength + segmentLength / 2.0);
            }
        }

        private static bool NearlyZero(double value)
        {
            return Math.Abs(value) <= 1e-9;
        }
    }
}
