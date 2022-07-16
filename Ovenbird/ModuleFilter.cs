using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace Ovenbird
{
    public class ModuleFilter : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public ModuleFilter()
          : base("Line Bonder", "Bonder",
            "Sort out and group wall centerlines for rectification and region detection",
            "Ovenbird", "Measures")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddLineParameter("Lines", "Lines",
                "List of curves (nurb curve, arc, polyline, line segment", GH_ParamAccess.list);
            pManager.AddNumberParameter("Grouping Tolerance", "Gtol",
                "The expansion distance of a line during intersection check", GH_ParamAccess.item);
            pManager.AddNumberParameter("Extension Tolerance", "Etol",
                "The maximum extrusion distance of a line", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddLineParameter("Line Groups", "Groups",
                "Return the plausible intersected line groups for region detection", GH_ParamAccess.tree);
            pManager.AddLineParameter("Orphan lines", "Orphans",
                "Return the orphan lines for further shading creation or indoor partition", GH_ParamAccess.list);
            pManager.AddGenericParameter("Nested Line Groups", "_G",
                "Return the nested line groups (not exposed to GH environment)", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Line> lines = new List<Line>();
            double gtol = 0.1;
            double etol = 0.1;

            if (!DA.GetDataList(0, lines) || !DA.GetData(1, ref gtol) || !DA.GetData(2, ref etol))
                return;

            List<gbSeg> segs = Basic.LinesTransfer(lines);

            List<gbSeg> flatLines = Basic.FlattenLines(segs);

            gbSeg extLine;
            for (int i = 0; i < flatLines.Count; i++)
            {
                for (int j = 0; j < flatLines.Count; j++)
                {
                    if (i != j)
                    {
                        flatLines[i] = Basic.SegExtension(flatLines[i], flatLines[j], etol);
                        //Rhino.RhinoApp.WriteLine(flatLines[i].Start.Serialize() + " / " + flatLines[i].End.Serialize());
                    }
                }
            }

            List<List<gbSeg>> lineGroups = Basic.SegClusterByFuzzyIntersection(flatLines, gtol);
            List<gbSeg> orphans = new List<gbSeg>();
            for (int i = lineGroups.Count - 1; i >= 0; i--)
            {
                if (lineGroups[i].Count <= 3)
                {
                    orphans.AddRange(lineGroups[i]);
                    lineGroups.RemoveAt(i);
                }
            }
            Rhino.RhinoApp.WriteLine("############" + lineGroups[0].Count.ToString());
            Rhino.RhinoApp.WriteLine("############" + orphans.Count.ToString());

            List<List<Line>> _lineGroups = new List<List<Line>>();
            foreach (List<gbSeg> lineGroup in lineGroups)
                _lineGroups.Add(Basic.LinesTransfer(lineGroup));
            List<Line> _orphans = Basic.LinesTransfer(orphans);

            DataTree<Line> lineTree = Util.ListToTree(_lineGroups);

            DA.SetDataTree(0, lineTree);
            DA.SetDataList(1, _orphans);
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// You can add image files to your project resources and access them like this:
        /// return Resources.IconForThisComponent;
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.Cluster;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("E8BAD5EE-2279-4140-94F2-C28A8444A2FF");
    }
}