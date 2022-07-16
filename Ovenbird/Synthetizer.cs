using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace Ovenbird
{
    public class Synthetizer : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public Synthetizer()
          : base("Synthetizer", "Synthetizer",
            "Temperal stuff",
            "Ovenbird", "Pack")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Loops", "L1", "Refined wall centerlines", GH_ParamAccess.item);
            pManager.AddGenericParameter("Loops", "S1", "Refined wall centerlines", GH_ParamAccess.item);
            pManager.AddGenericParameter("Loops", "M1", "Refined wall centerlines", GH_ParamAccess.item);
            pManager.AddGenericParameter("Loops", "L2", "Refined wall centerlines", GH_ParamAccess.item);
            pManager.AddGenericParameter("Loops", "S2", "Refined wall centerlines", GH_ParamAccess.item);
            pManager.AddGenericParameter("Loops", "M2", "Refined wall centerlines", GH_ParamAccess.item);
            pManager.AddGenericParameter("Loops", "L3", "Refined wall centerlines", GH_ParamAccess.item);
            pManager.AddGenericParameter("Loops", "S3", "Refined wall centerlines", GH_ParamAccess.item);
            pManager.AddGenericParameter("Loops", "M3", "Refined wall centerlines", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Goo Space Boundary", "dictL", "Nested space boundaries", GH_ParamAccess.item);
            pManager.AddGenericParameter("Goo Floorplan Boundary", "dictS", "Floorplan boundary", GH_ParamAccess.item);
            pManager.AddGenericParameter("Goo Matching Relations", "dictM", "Surface and its adjacency relations", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<string> labels = new List<string>();

            GH_ObjectWrapper loopGoo1 = null;
            GH_ObjectWrapper shellGoo1 = null;
            GH_ObjectWrapper matchGoo1 = null;
            GH_ObjectWrapper loopGoo2 = null;
            GH_ObjectWrapper shellGoo2 = null;
            GH_ObjectWrapper matchGoo2 = null;
            GH_ObjectWrapper loopGoo3 = null;
            GH_ObjectWrapper shellGoo3 = null;
            GH_ObjectWrapper matchGoo3 = null;


            if (!DA.GetData(0, ref loopGoo1) || !DA.GetData(1, ref shellGoo1) || !DA.GetData(2, ref matchGoo1) ||
                !DA.GetData(3, ref loopGoo2) || !DA.GetData(4, ref shellGoo2) || !DA.GetData(5, ref matchGoo2) ||
                !DA.GetData(6, ref loopGoo3) || !DA.GetData(7, ref shellGoo3) || !DA.GetData(8, ref matchGoo3))
                return;

            List<List<Point3d>> nestedLoop1 = loopGoo1.Value as List<List<Point3d>>;
            List<Point3d> nestedShell1 = shellGoo1.Value as List<Point3d>;
            List<List<string>> nestedMatch1 = matchGoo1.Value as List<List<string>>;
            List<List<Point3d>> nestedLoop2 = loopGoo2.Value as List<List<Point3d>>;
            List<Point3d> nestedShell2 = shellGoo2.Value as List<Point3d>;
            List<List<string>> nestedMatch2 = matchGoo2.Value as List<List<string>>;
            List<List<Point3d>> nestedLoop3 = loopGoo3.Value as List<List<Point3d>>;
            List<Point3d> nestedShell3 = shellGoo3.Value as List<Point3d>;
            List<List<string>> nestedMatch3 = matchGoo3.Value as List<List<string>>;

            Dictionary<int, List<List<Point3d>>> dictLoop = new Dictionary<int, List<List<Point3d>>>();
            Dictionary<int, List<Point3d>> dictShell = new Dictionary<int, List<Point3d>>();
            Dictionary<int, List<List<string>>> dictMatch = new Dictionary<int, List<List<string>>>();

            
            dictLoop.Add(0, nestedLoop1);
            dictShell.Add(0, nestedShell1);
            dictMatch.Add(0, nestedMatch1);
            dictLoop.Add(1, nestedLoop2);
            dictShell.Add(1, nestedShell2);
            dictMatch.Add(1, nestedMatch2);
            dictLoop.Add(2, nestedLoop3);
            dictShell.Add(2, nestedShell3);
            dictMatch.Add(2, nestedMatch3);


            IGH_Goo dictLGoo = new GH_ObjectWrapper(dictLoop);
            IGH_Goo dictSGoo = new GH_ObjectWrapper(dictShell);
            IGH_Goo dictMGoo = new GH_ObjectWrapper(dictMatch);

            DA.SetData(0, dictLGoo);
            DA.SetData(1, dictSGoo);
            DA.SetData(2, dictMGoo);
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
        public override Guid ComponentGuid => new Guid("AB568C8E-FD3C-4983-BE07-DCA3B7B0FF67");
    }
}