using System;
using System.Collections.Generic;
using ClipperLib;
using System.Text.RegularExpressions;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace Ovenbird
{
    public class ModuleZipXML : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public ModuleZipXML()
          : base("gbXML Serializer", "ZipXML",
            "Convert nested space boundary lines and adjacency dictionary to the gbXML",
            "Ovenbird", "Pack")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Floor elevation", "dictE",
                "Floor elevation Dictionary<string, double>", GH_ParamAccess.item);

            pManager.AddGenericParameter("Spaceloop dictionary", "dictL",
                "Nested loops of space vertice", GH_ParamAccess.item);
            pManager.AddGenericParameter("Floorplan dictionary", "dictS",
                "Generic list of floorplan vertice", GH_ParamAccess.item);
            pManager.AddGenericParameter("Matching relation dictionary", "dictM",
                "Generic list of matched surface id", GH_ParamAccess.item);
            
            pManager.AddGenericParameter("Window dictionary", "dictW",
                "Dictionary of location point and name info", GH_ParamAccess.item);
            pManager.AddGenericParameter("Door dictionary", "dictD",
                "Dictionary of location point and name info", GH_ParamAccess.item);
            pManager.AddGenericParameter("Curtainwall dictionary", "dictC",
                "Dictionary of location line and name info", GH_ParamAccess.item);

            pManager.AddTextParameter("Output path", "Path",
                "The output path for generated gbXML file", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Run", "Run",
                "Run export scripts", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.AddTextParameter("Surface Id", "I", "Surface Id for matching check", GH_ParamAccess.tree);
            //pManager.AddTextParameter("Adjacent Id", "RefI", "The adjacent surface Id of the current surface", GH_ParamAccess.tree);
            //pManager.AddLineParameter("Surface baseline", "L", "Surface baselines for debugging", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_ObjectWrapper nestedSpaceGoo = null;
            GH_ObjectWrapper nestedShellGoo = null;
            GH_ObjectWrapper nestedMatchGoo = null;
            GH_ObjectWrapper nestedElevationGoo = null;
            GH_ObjectWrapper nestedWindowGoo = null;
            GH_ObjectWrapper nestedDoorGoo = null;
            GH_ObjectWrapper nestedCurtainGoo = null;
            string XMLPath = null;
            bool toggle = false;

            if (!DA.GetData(0, ref nestedElevationGoo) || !DA.GetData(1, ref nestedSpaceGoo)
                || !DA.GetData(2, ref nestedShellGoo) || !DA.GetData(3, ref nestedMatchGoo) 
                || !DA.GetData(4, ref nestedWindowGoo) || !DA.GetData(5, ref nestedDoorGoo)
                || !DA.GetData(6, ref nestedCurtainGoo) || !DA.GetData(7, ref XMLPath))
                return;

            DA.GetData(8, ref toggle);
            if (!toggle)
                return;

            Dictionary<int, Tuple<string, double>> dictElevation = nestedElevationGoo.Value as Dictionary<int, Tuple<string, double>>;
            Dictionary<int, List<List<Point3d>>> dictLoop = nestedSpaceGoo.Value as Dictionary<int, List<List<Point3d>>>;
            Dictionary<int, List<Point3d>> dictShell = nestedShellGoo.Value as Dictionary<int, List<Point3d>>;
            Dictionary<int, List<List<string>>> dictMatch = nestedMatchGoo.Value as Dictionary<int, List<List<string>>>;
            Dictionary<int, List<Tuple<Point3d, string>>> dictWindow = nestedWindowGoo.Value as Dictionary<int, List<Tuple<Point3d, string>>>;
            Dictionary<int, List<Tuple<Point3d, string>>> dictDoor = nestedDoorGoo.Value as Dictionary<int, List<Tuple<Point3d, string>>>;
            Dictionary<int, List<Tuple<Line, string>>> dictCurtain = nestedCurtainGoo.Value as Dictionary<int, List<Tuple<Line, string>>>;

            // temperal format transform
            // will be removed soon
            Dictionary<int, List<List<gbXYZ>>> _dictLoop = new Dictionary<int, List<List<gbXYZ>>>();
            Dictionary<int, List<gbXYZ>> _dictShell = new Dictionary<int, List<gbXYZ>>();
            Dictionary<int, List<Tuple<gbXYZ, string>>> _dictWindow = new Dictionary<int, List<Tuple<gbXYZ, string>>>();
            Dictionary<int, List<Tuple<gbXYZ, string>>> _dictDoor = new Dictionary<int, List<Tuple<gbXYZ, string>>>();
            Dictionary<int, List<Tuple<gbSeg, string>>> _dictCurtain = new Dictionary<int, List<Tuple<gbSeg, string>>>();
            foreach (KeyValuePair<int, List<List<Point3d>>> kvp in dictLoop)
            {
                List<List<gbXYZ>> tempLoop = new List<List<gbXYZ>>();
                foreach (List<Point3d> pts in kvp.Value)
                    tempLoop.Add(Basic.PtsTransfer(pts));
                _dictLoop.Add(kvp.Key, tempLoop);
            }
            foreach (KeyValuePair<int, List<Point3d>> kvp in dictShell)
                _dictShell.Add(kvp.Key, Basic.PtsTransfer(kvp.Value));
            foreach (KeyValuePair<int, List<Tuple<Point3d, string>>> kvp in dictWindow)
            {
                List<Tuple<gbXYZ, string>> tempLoop = new List<Tuple<gbXYZ, string>>();
                foreach (Tuple<Point3d, string> pair in kvp.Value)
                    tempLoop.Add(new Tuple<gbXYZ, string>(Basic.PtTransfer(pair.Item1), pair.Item2));
                _dictWindow.Add(kvp.Key, tempLoop);
            }
            foreach (KeyValuePair<int, List<Tuple<Point3d, string>>> kvp in dictDoor)
            {
                List<Tuple<gbXYZ, string>> tempLoop = new List<Tuple<gbXYZ, string>>();
                foreach (Tuple<Point3d, string> pair in kvp.Value)
                    tempLoop.Add(new Tuple<gbXYZ, string>(Basic.PtTransfer(pair.Item1), pair.Item2));
                _dictDoor.Add(kvp.Key, tempLoop);
            }
            foreach (KeyValuePair<int, List<Tuple<Line, string>>> kvp in dictCurtain)
            {
                List<Tuple<gbSeg, string>> tempLoop = new List<Tuple<gbSeg, string>>();
                foreach (Tuple<Line, string> pair in kvp.Value)
                    tempLoop.Add(new Tuple<gbSeg, string>(Basic.LineTransfer(pair.Item1), pair.Item2));
                _dictCurtain.Add(kvp.Key, tempLoop);
            }


            XMLGeometry.Generate(dictElevation, 
                _dictLoop, _dictShell, dictMatch, 
                _dictWindow, _dictDoor, _dictCurtain, 
                out List<gbZone> zones,
                out List<gbFloor> floors,
                out List<gbSurface> surfaces);

            XMLSerialize.Generate(XMLPath + "//SampleXML.xml", zones, floors, surfaces);
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
                return Properties.Resources.box_closed;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("1CB4BBF6-BC89-4C28-A30B-3662ADEB80B1");
    }
}