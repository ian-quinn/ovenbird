using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace Ovenbird
{
    public class ModuleGetLattice : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public ModuleGetLattice()
          : base("Point Alignment", "GetLattice",
            "Split intersected lines, trim the strays and get a neat space lattice for region detection",
            "Ovenbird", "Measures")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curves", "Crvs", "Refined wall centerlines", GH_ParamAccess.list);
            pManager.AddNumberParameter("Rotation angle", "Theta", "By default the component only align points on " +
                "X-axis and Y-axis. If the drawing has an angle with the world plane axes, a rotation is needed. " +
                "The function is still under test so just take 0 rotation and play with rectangle floorplans.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Aggregation threshold", "Delta", "Defines the threshold to describe if a " +
                "bunch of points are almost co-lined. This better goes with the doubled thickness of regular walls.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Tolerance", "TOL", "Global tolerance to avoid double precision issues.", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddLineParameter("Wall Centerlines", "Lattice", "Wall line grids.", GH_ParamAccess.list);
            pManager.AddCurveParameter("Stray lines", "Strays", "Cached lines for further space split or shading creation.", GH_ParamAccess.list);
            pManager.AddPointParameter("TEMP", "TEMP", "...", GH_ParamAccess.list);
            pManager.AddVectorParameter("TEMP2", "TEMP2", "...", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Curve> crvs = new List<Curve>();
            double theta = 0;
            double delta = 0.2;
            double tolerance = 0.000001;

            if (!DA.GetDataList(0, crvs) || !DA.GetData(1, ref theta) || 
                !DA.GetData(2, ref delta) || !DA.GetData(3, ref tolerance))
                return;

            if (crvs == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "There seems no input lines");
                return;
            }
            if (delta < 0 || tolerance < 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please input parameters > 0");
                return;
            }


            List<Point3d> joints;
            List<List<Vector3d>> hands;
            List<List<Vector3d>> phantomHands = new List<List<Vector3d>>();
            joints = PointAlignment.GetJoints(crvs, out hands);
            foreach (List<Vector3d> hand in hands)
            {
                List<Vector3d> phantomHand = new List<Vector3d>();
                foreach (Vector3d h in hand)
                    phantomHand.Add(h);
                phantomHands.Add(phantomHand);
            }


            List<List<Vector3d>> anchorInfo_temp;
            List<List<Vector3d>> anchorInfo;
            List<Point3d> ptAlign_temp = PointAlignment.AlignPts(joints, hands,
              theta, delta, tolerance, out anchorInfo_temp);
            List<Point3d> ptAlign = PointAlignment.AlignPts(ptAlign_temp, anchorInfo_temp,
              theta - Math.PI / 2, delta, tolerance, out anchorInfo);
            //List<Point3d> ptAlign = PointAlignment.AlignPts(joints, hands,
            //  theta, delta, tolerance, out anchorInfo);

            List<Line> strays;
            List<List<Line>> nestedLattice = PointAlignment.GetLattice(ptAlign, anchorInfo, tolerance, out strays);
            List<Line> lattice = Util.FlattenList(nestedLattice);
            //DataTree<Line> latticeTree = Util.ListToTree(nestedLattice);


            DA.SetDataList(0, lattice);
            DA.SetDataList(1, strays);
            DA.SetDataList(2, ptAlign);
            DA.SetDataTree(3, Util.ListToTree(anchorInfo));
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
                return Properties.Resources.Align;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("7D22BB83-4A78-4E3E-B895-5CE4F6FD5FA0");
    }
}