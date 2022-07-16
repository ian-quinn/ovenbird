using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace Ovenbird
{
    public class ModuleTesselation : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public ModuleTesselation()
          : base("Polygon Tesselation", "GetTiles",
            "dd",
            "Ovenbird", "Measures")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("MCR", "MCR", "Point loops representing a MCR", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Tile", "Tile", "Vertice of the shell loop.", GH_ParamAccess.tree);
            pManager.AddPointParameter("Shell", "Shell", "Vertice of the shell loop.", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_ObjectWrapper nestedLoopGoo = null;

            if (!DA.GetData(0, ref nestedLoopGoo))
                return;

            List<List<Point3d>> nestedLoop = nestedLoopGoo.Value as List<List<Point3d>>;
            List<List<gbXYZ>> mcr = new List<List<gbXYZ>>();
            foreach (List<Point3d> loop in nestedLoop)
            {
                List<gbXYZ> _mcr = new List<gbXYZ>();
                foreach (Point3d pt in loop)
                    _mcr.Add(Basic.PtTransfer(pt));
                mcr.Add(_mcr);
            }

            List<List<gbXYZ>> tiles = new List<List<gbXYZ>>();
            List<List<List<gbXYZ>>> remains = new List<List<List<gbXYZ>>>();
            RecTessellation.Tessellate(mcr, out tiles, out remains);

            List<List<Point3d>> _tiles = new List<List<Point3d>>();
            foreach (List<gbXYZ> tile in tiles)
                _tiles.Add(Basic.PtsTransfer(tile));
            List<List<List<Point3d>>> _remains = new List<List<List<Point3d>>>();
            foreach (List<List<gbXYZ>> remain in remains)
            {
                List<List<Point3d>> _remain = new List<List<Point3d>>();
                foreach (List<gbXYZ> loop in remain)
                    _remain.Add(Basic.PtsTransfer(loop));
                _remains.Add(_remain);
            }

            DA.SetDataTree(0, Util.ListToTree(_tiles));
            DA.SetDataTree(1, Util.ListToTree(_remains));
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
        public override Guid ComponentGuid => new Guid("76BABE92-7A96-4C49-ABA0-655E35CB979F");
    }
}