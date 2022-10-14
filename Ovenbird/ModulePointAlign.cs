using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace Ovenbird
{
    /*
    public class ModulePointAlign : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public ModulePointAlign()
          : base("Point Align", "GetLattice",
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
            pManager.AddPointParameter("PT1", "PT1", "...", GH_ParamAccess.list);
            pManager.AddVectorParameter("VEC1", "VEC1", "...", GH_ParamAccess.tree);
            pManager.AddPointParameter("PT2", "PT2", "...", GH_ParamAccess.list);
            pManager.AddVectorParameter("VEC2", "VEC2", "...", GH_ParamAccess.tree);
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

            List<gbSeg> segs = new List<gbSeg>();
            foreach (Curve crv in crvs)
                segs.Add(Basic.CrvTransfer(crv));

            List<gbXYZ> joints;
            List<List<gbXYZ>> hands;
            joints = PointAlign.GetJoints(segs, tolerance, out hands);
            List<List<Vector3d>> phantomHands = new List<List<Vector3d>>();
            foreach (List<gbXYZ> hand in hands)
            {
                List<Vector3d> phantomHand = new List<Vector3d>();
                foreach (gbXYZ h in hand)
                    phantomHand.Add(Basic.VecTransfer(h));
                phantomHands.Add(phantomHand);
            }

            List<List<gbXYZ>> anchorInfo_temp;
            List<List<gbXYZ>> anchorInfo;
            List<gbXYZ> ptAlign_temp = PointAlign.AlignPts(joints, hands,
              theta, delta, tolerance, out anchorInfo_temp);
            List<gbXYZ> ptAlign = PointAlign.AlignPts(ptAlign_temp, anchorInfo_temp,
              theta - Math.PI / 2, delta, tolerance, out anchorInfo);
            //List<gbXYZ> ptAlign = PointAlign.AlignPts(joints, hands,
            //  theta, delta, tolerance, out anchorInfo);

            List<gbSeg> strays;
            List<List<gbSeg>> nestedLattice = PointAlign.GetLattice(ptAlign, anchorInfo, tolerance, 1.5 * delta, out strays);
            List<gbSeg> lattice = Util.FlattenList(nestedLattice);


            List<Line> _lattice = Basic.LinesTransfer(lattice);
            List<Line> _strays = Basic.LinesTransfer(strays);
            List<Point3d> _ptAlign = Basic.PtsTransfer(ptAlign);
            List<Point3d> _joints = Basic.PtsTransfer(joints);
            List<List<Vector3d>> _hands = new List<List<Vector3d>>();
            foreach (List<gbXYZ> hand in anchorInfo)
            {
                _hands.Add(Basic.VecsTransfer(hand));
            }

            DA.SetDataList(0, _lattice);
            DA.SetDataList(1, _strays);
            DA.SetDataList(2, Basic.PtsTransfer(ptAlign));
            DA.SetDataTree(3, Util.ListToTree(_hands));
            DA.SetDataList(4, Basic.PtsTransfer(joints));
            DA.SetDataTree(5, Util.ListToTree(phantomHands));
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
        public override Guid ComponentGuid => new Guid("6BCE921A-B640-4795-AF49-80B6EFE5104E");
    }
    */
}