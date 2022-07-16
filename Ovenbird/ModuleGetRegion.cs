using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace Ovenbird
{
    public class ModuleGetRegion : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public ModuleGetRegion()
          : base("Region Generator", "GetRegion",
            "Sort out space boundaries and surface relations from a bunch of fixed wall centerlines",
            "Ovenbird", "Measures")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddLineParameter("Lines", "Lines",
                "Refined wall centerlines", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Level", "Level",
                "Refined wall centerlines", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            // for debugging
            pManager.AddPointParameter("Space Boundary Points", "Loops", "Nested space boundaries", GH_ParamAccess.tree);
            pManager.AddLineParameter("Space Boundary Lines", "Lines", "Nested space boundary lines", GH_ParamAccess.tree);
            pManager.AddPointParameter("Floorplan Boundary", "Shell", "Floorplan boundary", GH_ParamAccess.list);
            pManager.AddTextParameter("Matching Relations", "Match", "Surface and its adjacency relations", GH_ParamAccess.tree);
            // private use (prefix by _)
            pManager.AddGenericParameter("Goo Space Boundary", "_Loops", "Nested space boundaries", GH_ParamAccess.item);
            pManager.AddGenericParameter("Goo Floorplan Boundary", "_Shell", "Floorplan boundary", GH_ParamAccess.item);
            pManager.AddGenericParameter("Goo Matching Relations", "_Match", "Surface and its adjacency relations", GH_ParamAccess.item);
            pManager.AddLineParameter("Abandoned Lines", "Orphans", "des", GH_ParamAccess.tree);

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Line> lines = new List<Line>();
            int levelId = 0; ;

            if (!DA.GetDataList(0, lines) || !DA.GetData(1, ref levelId))
                return;

            if (lines == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please check if the inputs are qualified");
                return;
            }

            List<gbSeg> segs = Basic.LinesTransfer(lines);

            List<List<gbXYZ>> nestedSpace;
            List<gbXYZ> nestedShell;
            List<List<string>> nestedMatch;
            List<List<gbSeg>> nestedOrphans;

            SpaceDetection.GetBoundary(segs, levelId, out nestedSpace, out nestedShell, out nestedMatch, out nestedOrphans);

            List<List<Point3d>> _nestedSpace = new List<List<Point3d>>();
            foreach (List<gbXYZ> pts in nestedSpace)
                _nestedSpace.Add(Basic.PtsTransfer(pts));
            List<List<Line>> _nestedOrphans = new List<List<Line>>();
            foreach (List<gbSeg> orphans in nestedOrphans)
                _nestedOrphans.Add(Basic.LinesTransfer(orphans));
            List<Point3d> _nestedShell = Basic.PtsTransfer(nestedShell);


            List<List<Line>> nestedBoundary = new List<List<Line>>();
            foreach (List<Point3d> space in _nestedSpace)
            {
                List<Line> boundary = new List<Line>();
                for (int i = 0; i < space.Count - 1; i++)
                    boundary.Add(new Line(space[i], space[i + 1]));
                nestedBoundary.Add(boundary);
            }

            DataTree<Point3d> spaceTree = Util.ListToTree<Point3d>(_nestedSpace);
            DataTree<Line> lineTree = Util.ListToTree<Line>(nestedBoundary);
            DataTree<string> matchTree = Util.ListToTree<string>(nestedMatch);
            IGH_Goo spaceGoo = new GH_ObjectWrapper(_nestedSpace);
            IGH_Goo shellGoo = new GH_ObjectWrapper(_nestedShell);
            IGH_Goo matchGoo = new GH_ObjectWrapper(nestedMatch);
            DataTree<Line> orphanTree = Util.ListToTree<Line>(_nestedOrphans);

            DA.SetDataTree(0, spaceTree);
            DA.SetDataTree(1, lineTree);
            DA.SetDataList(2, _nestedShell);
            DA.SetDataTree(3, matchTree);
            DA.SetData(4, spaceGoo);
            DA.SetData(5, shellGoo);
            DA.SetData(6, matchGoo);
            DA.SetDataTree(7, orphanTree);
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
                return Properties.Resources.Region;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("4A49D194-5074-41A6-8C08-CAFE769B54B9");
    }
}