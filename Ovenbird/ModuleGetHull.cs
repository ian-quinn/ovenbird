using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace Ovenbird
{
    public class ModuleGetHull : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public ModuleGetHull()
          : base("Orthogonal Hull", "GetHull",
            "Get the orthogonal convex hull from a set of point cloud",
            "Ovenbird", "Measures")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "Pts",
                "List of points (List<Point3d>)", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Orthogonal Hull", "Ortho",
                "Return the orthogonal convex hull of the point cloud", GH_ParamAccess.list);
            pManager.AddPointParameter("Rectangular Hull", "Rect", 
                "Return the rectangular convex hull of the point cloud", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Point3d> pts = new List<Point3d>();

            if (!DA.GetDataList(0, pts))
                return;

            List<gbXYZ> _pts = Basic.PtsTransfer(pts);
            List<gbXYZ> orthoHull = OrthogonalHull.GetOrthoHull(_pts);
            List<gbXYZ> rectHull = OrthogonalHull.GetRectHull(_pts);
            List<Point3d> _orthoHull = Basic.PtsTransfer(orthoHull);
            List<Point3d> _rectHull = Basic.PtsTransfer(rectHull);

            DA.SetDataList(0, _orthoHull);
            DA.SetDataList(1, _rectHull);
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
                return Properties.Resources.Hull;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("8578CAE2-A352-4301-B67E-C86F83105AAC");
    }
}