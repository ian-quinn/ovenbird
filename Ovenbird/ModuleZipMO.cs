using System;
using System.Collections.Generic;
using System.IO;
using ClipperLib;
using System.Text.RegularExpressions;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace Ovenbird
{
    public class ModuleZipMO : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public ModuleZipMO()
          : base("MO Serializer", "ZipMO",
            "Convert one-floor space boundary to Modelica model",
            "Ovenbird", "Pack")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddLineParameter("Lines", "lines",
                "Refined wall centerlines", GH_ParamAccess.list);
            pManager.AddNumberParameter("Window to Wall Ratio", "wwr",
                "The window will be added automatically based on an uniform window to wall ratio value", GH_ParamAccess.item);
            pManager.AddNumberParameter("Floor height", "height",
                "The gross height of this floor", GH_ParamAccess.item);
            pManager.AddTextParameter("Labels", "labels",
                "Custom space function labels", GH_ParamAccess.list);
            pManager.AddTextParameter("Path", "path",
                "Path for the modelica model", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Run", "run",
                "Run export scripts", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Output log", "info", 
                "Translation note on this draft model", GH_ParamAccess.item);
            pManager.AddTextParameter("Modelica input file", ".mo", 
                "The Modelica code", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Line> lines = new List<Line>();
            double wwr = 0;
            double height = 3.0;
            List<string> labels = new List<string>();
            string path = "";
            bool run = false;

            if (!DA.GetDataList(0, lines) || !DA.GetData(4, ref path))
                return;

            DA.GetData(5, ref run); if (!run) return;
            DA.GetData(1, ref wwr);
            if (wwr > 1 || wwr < 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "WWR within a rational range?");
                return;
            }
            DA.GetData(2, ref height);
            if (height < 0 || height > 5)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Floor height within a rational range?");
                return;
            }

            List<gbSeg> segs = Basic.LinesTransfer(lines);

            List<List<gbXYZ>> nestedSpace;      // loop of points of each recognized space
            List<gbXYZ> nestedShell;            // outer shell, no need for this
            List<List<string>> nestedMatch;     // match relations in string
            List<List<gbSeg>> nestedOrphans;    // orphan lines, no need for this

            SpaceDetection.GetBoundary(segs, out nestedSpace, out nestedShell, out nestedMatch, out nestedOrphans);

            string log = "";
            string modelName = Path.GetFileName(path).Split('.')[0];
            string model = MoSerialize.Generate(modelName, nestedSpace, nestedMatch, wwr, height, out log);

            using (var writer = File.AppendText(path))
            {
                writer.WriteLine(model);
                log += "File written successfully.\n";
            }

            DA.SetData(0, log);
            DA.SetData(1, model);
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
        public override Guid ComponentGuid => new Guid("54702BFD-42ED-4325-BA5F-75C1889396CA");
    }
}