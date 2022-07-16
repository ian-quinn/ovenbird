using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace Ovenbird
{
    public class ModuleClipPoly : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public ModuleClipPoly()
          : base("Polygon Clipper", "ClipPoly",
            "This component implements Weiler-Atheton algorithm to clip the polygon. " +
                "Boolean union, intersect, difference are supported.",
            "Ovenbird", "Measures")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("The polygon to be clipped", "PolyA",
                "Point loop of the target polygon (List<Point3d>)", GH_ParamAccess.item);
            pManager.AddGenericParameter("The polygon as the clipper", "PolyB",
                "Point loop of the polygon to be used (List<Point3d>)", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Boolean Operation", "XOR",
                "Choose from the operations:\n 0 : Union \n 1 : Intersect \n 2 : Difference", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGeometryParameter("Boolean results", "R", "Boolean results", GH_ParamAccess.list);
            //pManager.AddGeometryParameter("Intersections", "X", "Intersection points of the two polygons", GH_ParamAccess.list);
            //pManager.AddGeometryParameter("Enters", "In", "Points entering polygon", GH_ParamAccess.list);
            //pManager.AddGeometryParameter("Exits", "Out", "Points exiting polygon", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_ObjectWrapper targetPolyGoo = null;
            GH_ObjectWrapper clipperPolyGoo = null;
            int operationId = 0;

            if (!DA.GetData(0, ref targetPolyGoo) || !DA.GetData(1, ref clipperPolyGoo) || !DA.GetData(2, ref operationId))
                return;

            List<Point3d> targetLoop = targetPolyGoo.Value as List<Point3d>;
            List<Point3d> clipperLoop = clipperPolyGoo.Value as List<Point3d>;

            //BooleanOperation operation = BooleanOperation.Union;
            ClipperLib.ClipType operation = ClipperLib.ClipType.ctIntersection;
            switch (operationId)
            {
                //case 0: operation = BooleanOperation.Union; break;
                //case 1: operation = BooleanOperation.Intersect; break;
                //case 2: operation = BooleanOperation.Difference; break;
                case 0: operation = ClipperLib.ClipType.ctUnion; break;
                case 1: operation = ClipperLib.ClipType.ctIntersection; break;
                case 2: operation = ClipperLib.ClipType.ctDifference; break;
            }
            List<List<Point3d>> nestedRegions = new List<List<Point3d>>();
            List<Point3d> sectPts = null;
            List<Point3d> enterPts = null;
            List<Point3d> exitPts = null;


            //nestedRegions = PolygonClip.Process(targetLoop, clipperLoop, operation, 
            //    out sectPts, out enterPts, out exitPts);

            //nestedRegions = PolygonClip.Process(targetLoop, clipperLoop, operation);
            List<gbXYZ> _targetLoop = Basic.PtsTransfer(targetLoop);
            List<gbXYZ> _clipperLoop = Basic.PtsTransfer(clipperLoop);
            List<List<gbXYZ>> _nestedRegions = Basic.ClipPoly(_targetLoop, _clipperLoop, operation);
            foreach (List<gbXYZ> XYZs in _nestedRegions)
                nestedRegions.Add(Basic.PtsTransfer(XYZs));


            List<Polyline> nestedPoly = new List<Polyline>();
            foreach (List<Point3d> region in nestedRegions)
            {
                List<Point3d> ptsLoop = new List<Point3d>();
                foreach (Point3d pt in region)
                    ptsLoop.Add(pt);
                ptsLoop.Add(ptsLoop[0]);
                nestedPoly.Add(new Polyline(ptsLoop));
            }

            DA.SetDataList(0, nestedPoly);
            //DA.SetDataList(1, sectPts);
            //DA.SetDataList(2, enterPts);
            //DA.SetDataList(3, exitPts);
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
                return Properties.Resources.Clip;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("89C2BE58-1E26-499A-9EDF-9E85E582FCEB");
    }
}