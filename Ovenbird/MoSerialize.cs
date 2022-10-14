using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Ovenbird
{
    class MoSerialize
    {

        private class Zone
        {
            public string name;
            public double volume;
            public int nConstructions;
            public int nHeatSources;
            public bool calcIdealLoads;
            public bool prescribedAirchange;
            public bool heatSources;

            public double height = 3.0;
            public Zone(string name, double area, double height, int nConstructions, int nHeatSources,
                bool calcIdealLoads, bool prescribedAirchange, bool heatSources)
            {
                this.name = name;
                this.volume = Math.Round(area * height, 2);
                this.nConstructions = nConstructions;
                this.nHeatSources = nHeatSources;
                this.calcIdealLoads = calcIdealLoads;
                this.prescribedAirchange = prescribedAirchange;
                this.heatSources = heatSources;
                this.height = Math.Round(height);
            }
            public override string ToString()
            {
                string text = "";
                text += $"  BuildingSystems.Buildings.Zones.ZoneTemplateAirvolumeMixed {name}(\n";
                text += $"    V = {volume}, \n";
                text += $"    nConstructions = {nConstructions}, \n";
                text += $"    calcIdealLoads = {calcIdealLoads.ToString().ToLower()}, \n";
                text += $"    prescribedAirchange = {prescribedAirchange.ToString().ToLower()}, \n";
                text += $"    heatSources = {heatSources.ToString().ToLower()}, \n";
                text += $"    nHeatSources = {nHeatSources}, \n";
                text += $"    height = {height});\n\n";
                return text;
            }
        }

        private class Surface
        {
            public string name;
            public double wwr;
            public string construction;
            public string boundaryOut;
            public double angleDegAzi;
            public double angleDegTil;
            public double AInnSur;
            public double height;
            public double width;
            public Surface(string name, string construction, string boundaryOut, 
                double angleDegAzi, double angleDegTil, double AInnSur, double height, double width)
            {
                this.name = name;
                this.construction = construction;
                this.boundaryOut = boundaryOut;
                this.angleDegAzi = Math.Round(angleDegAzi, 1);
                this.angleDegTil = Math.Round(angleDegTil, 1);
                this.AInnSur = Math.Round(AInnSur, 1);
                this.height = Math.Round(height, 1);
                this.width = Math.Round(width, 1);
            }
            public override string ToString()
            {
                string text = "";
                text += $"BuildingSystems.Buildings.Constructions.Walls.WallThermal1DNodes {name}(\n";
                text += $"  redeclare {construction} constructionData, \n";
                text += $"  angleDegAzi = {angleDegAzi}, \n";
                text += $"  angleDegTil = {angleDegTil}, \n";
                text += $"  AInnSur = {AInnSur}, \n";
                text += $"  height = {height}, \n";
                text += $"  width = {width});\n\n";
                return text;
            }
        }

        private class Aperture
        {
            // most attributes inherited from the hosting wall
            public string name;
            public bool calcAirchange;
            public string construction;
            public Surface hostSrf;
            public Aperture(string name, bool calcAirchange, string construction, Surface hostSrf)
            {
                this.name = name;
                this.calcAirchange = calcAirchange;
                this.construction = construction;
                this.hostSrf = hostSrf;
            }
            public override string ToString()
            {
                string text = "";
                text += $"BuildingSystems.Buildings.Constructions.Windows.Window {name}(\n";
                text += $"  calcAirchange = {calcAirchange.ToString().ToLower()}, \n";
                text += $"  redeclare {construction} constructionData, \n";
                text += $"  angleDegAzi = {hostSrf.angleDegAzi}, \n";
                text += $"  angleDegTil = {hostSrf.angleDegTil}, \n";
                text += $"  height = {hostSrf.height * hostSrf.wwr}, \n";
                text += $"  width = {hostSrf.width * hostSrf.wwr});\n\n";
                return text;
            }
        }

        private class Construction
        {
            public string name;
            public int nLayers;
            public List<double> thickness;
            public List<string> material;
            // constructor
            public Construction(string name, int nLayers, List<double> thickness, List<string> material)
            {
                this.name = name;
                this.nLayers = nLayers;
                this.thickness = thickness;
                this.material = material;
            }
            // serialization
            public override string ToString()
            {
                string text = "";
                text += $"  record {name}\n";
                text += "    extends BuildingSystems.Buildings.Data.Constructions.OpaqueThermalConstruction(\n";
                text += $"      nLayers = {nLayers}, \n";
                text += $"      thickness = {{{Util.DoubleListToString(thickness)}}}, \n";
                text += $"      material = {{{Util.StringListToString(material)}}} );\n";
                text += $"  end {name};\n\n";
                return text;
            }
        }

        public static string Generate(string projName, List<List<gbXYZ>> nestedSpace, List<List<string>> nestedMatch, 
            double wwr, double floorHeight, out string log)
        {
            log = "";
            string mo = "";

            // prepare for all components
            Construction construction_1 = new Construction("wall_basic", 3, new List<double>() { 0.015, 0.2, 0.02 },
                new List<string>() { 
                    "BuildingSystems.HAM.Data.MaterialProperties.Thermal.Masea.Concrete()",
                    "BuildingSystems.HAM.Data.MaterialProperties.Thermal.Masea.Concrete()",
                    "BuildingSystems.HAM.Data.MaterialProperties.Thermal.Masea.Concrete()" });

            List<Zone> zones = new List<Zone>() { };
            List<Surface> walls = new List<Surface>() { };
            List<Surface> slabs = new List<Surface>() { };
            List<string> wallBlocklist = new List<string>() { };
            int counter_ambientSrf = 0;
            for (int i = 0; i < nestedSpace.Count; i++)
            {
                double area = Basic.GetPolyArea(nestedSpace[i]);
                if (nestedSpace[i].Count < 4)
                    continue;
                Zone zone = new Zone($"zone{i}", area, floorHeight, 
                    nestedSpace[i].Count + 1, 1, true, true, true);
                // note that the nestedSpace represents closed loop has n(vertex) = n(edge) + 1
                zones.Add(zone);
                    
                for (int j = 0; j < nestedSpace[i].Count - 1; j++)
                {
                    gbXYZ vec = nestedSpace[i][j + 1] - nestedSpace[i][j];
                    double azimuth = Basic.VectorAngle(vec, new gbXYZ(0, 1, 0));
                    Surface srf = new Surface(zone.name + $"_wall{j}", construction_1.name, 
                        nestedMatch[i][j], azimuth, 90.0, 0.0, floorHeight, vec.Norm());
                    if (!wallBlocklist.Contains(srf.name))
                    {
                        walls.Add(srf);
                        if (nestedMatch[i][j] != "Outside")
                            wallBlocklist.Add(nestedMatch[i][j]);
                        else
                            counter_ambientSrf++;
                    }
                }
                List<gbXYZ> boundingbox = OrthogonalHull.GetRectHull(nestedSpace[i]);
                Surface floor = new Surface(zone.name + $"_slab{nestedSpace[i].Count}", construction_1.name, "Outside", 
                    0.0, 180.0, 0.0, boundingbox[0].DistanceTo(boundingbox[1]), boundingbox[1].DistanceTo(boundingbox[2]));
                Surface ceil = new Surface(zone.name + $"_slab{nestedSpace[i].Count + 1}", construction_1.name, "Outside",
                    0.0, 0.0, 0.0, boundingbox[0].DistanceTo(boundingbox[1]), boundingbox[1].DistanceTo(boundingbox[2]));
                slabs.Add(floor); slabs.Add(ceil);
                Debug.Print($"You want the slabs? -{slabs.Count}");
            }

            mo += $"model {projName}\n";
            mo += "  extends Modelica.Icons.Example;\n";
            mo += "  \n";
            mo += construction_1.ToString();

            mo += "  model Building\n";
            mo += "    extends BuildingSystems.Buildings.BaseClasses.BuildingTemplate(\n";
            mo += $"      nZones = {zones.Count}, \n";
            mo += $"      surfacesToAmbience(nSurfaces = {counter_ambientSrf + zones.Count * 2}), \n";
            mo += $"      nSurfacesSolid = 0, \n";
            mo += $"      surfacesToSolids(nSurfaces = nSurfacesSolid), \n";
            mo += $"      useAirPaths = false, \n";
            mo += $"      calcIdealLoads = true, \n";
            mo += $"      prescribedAirchange = true, \n";
            mo += $"      heatSources = true, \n";
            mo += $"      nHeatSources = {zones.Count}, \n";
            mo += $"      convectionOnSurfaces = BuildingSystems.HAM.ConvectiveHeatTransfer.Types.Convection.forced);\n\n";

            foreach (Zone zone in zones)
                mo += zone.ToString();

            foreach (Surface wall in walls)
                mo += wall.ToString();
            foreach (Surface slab in slabs)
                mo += slab.ToString();

            mo += "  equation\n\n";
            int counter_ambientPort = 0;
            foreach (Surface wall in walls)
            {
                mo += $"    connect({wall.name}.toSurfacePort_1, " + 
                    $"{wall.name.Split('_')[0]}.toConstructionPorts[{Convert.ToInt32(wall.name.Split('_')[1].Substring(4)) + 1}]);\n";
                if (wall.boundaryOut == "Outside")
                {
                    counter_ambientPort++;
                    mo += $"    connect({wall.name}.toSurfacePort_2, " +
                        $"surfacesToAmbience.toConstructionPorts[{counter_ambientPort}]);\n";
                }
                else
                {
                    mo += $"    connect({wall.name}.toSurfacePort_2, " +
                        $"{wall.boundaryOut.Split('_')[0]}.toConstructionPorts[{Convert.ToInt32(wall.boundaryOut.Split('_')[1].Substring(4)) + 1}]);\n";
                }
            }
            foreach (Surface slab in slabs)
            {
                counter_ambientPort++;
                mo += $"    connect({slab.name}.toSurfacePort_1, " +
                    $"{slab.name.Split('_')[0]}.toConstructionPorts[{slab.name.Split('_')[1].Substring(4)}]);\n";
                mo += $"    connect({slab.name}.toSurfacePort_2, " +
                    $"surfacesToAmbience.toConstructionPorts[{counter_ambientPort}]);\n";
            }

            foreach (Zone zone in zones)
            {
                mo += $"    connect({zone.name}.T_setHeating, T_setHeating[{zones.IndexOf(zone) + 1}]);\n";
                mo += $"    connect({zone.name}.T_setCooling, T_setCooling[{zones.IndexOf(zone) + 1}]);\n";
                mo += $"    connect({zone.name}.Q_flow_cooling, Q_flow_cooling[{zones.IndexOf(zone) + 1}]);\n";
                mo += $"    connect({zone.name}.Q_flow_heating, Q_flow_heating[{zones.IndexOf(zone) + 1}]);\n";
                mo += $"    connect({zone.name}.airchange, airchange[{zones.IndexOf(zone) + 1}]);\n";
                mo += $"    connect({zone.name}.TAirAmb, TAirAmb);\n";
                mo += $"    connect({zone.name}.xAirAmb, xAirAmb);\n";
                mo += $"    connect({zone.name}.radHeatSourcesPorts[1], radHeatSourcesPorts[{zones.IndexOf(zone) + 1}]);\n";
                mo += $"    connect({zone.name}.conHeatSourcesPorts[1], conHeatSourcesPorts[{zones.IndexOf(zone) + 1}]);\n";
            }
            mo += "  end Building;\n\n";

            mo += $"  Building building(nZones = {zones.Count});\n";

            mo += "  BuildingSystems.Buildings.Ambience ambience(\n" +
                "    nSurfaces = building.nSurfacesAmbience,\n" +
                "    redeclare block WeatherData = BuildingSystems.Climate.WeatherDataEPW.USA_Chicago_EPO_ASCII);\n";

            // zone settings initiation
            foreach (Zone zone in zones)
            {
                mo += $"    Modelica.Blocks.Sources.Constant TSetHeating_{zone.name}(k = 273.15 + 20.0);\n";
                mo += $"    Modelica.Blocks.Sources.Constant TSetCooling_{zone.name}(k = 273.15 + 24.0);\n";
                mo += $"    Modelica.Blocks.Sources.Constant airchange_{zone.name}(k = 0.5);\n";
                mo += $"    Modelica.Blocks.Sources.Constant heatsources_{zone.name}(k = 0.0);\n";
            }
            mo += $"    Modelica.Thermal.HeatTransfer.Sources.PrescribedHeatFlow heatFlow[{zones.Count}];\n";
            mo += $"    BuildingSystems.Buildings.BaseClasses.RelationRadiationConvection " +
                $"relationRadiationConvection[{zones.Count}](each radiationportion = 0.5);\n";

            mo += "  equation\n";
            mo += "    connect(ambience.toSurfacePorts, building.toAmbienceSurfacesPorts);\n" +
                "    connect(ambience.toAirPorts, building.toAmbienceAirPorts);\n" +
                "    connect(ambience.TAirRef, building.TAirAmb);\n" +
                "    connect(ambience.xAir, building.xAirAmb);\n\n";

            foreach (Zone zone in zones)
            {
                mo += $"    connect(airchange_{zone.name}.y, building.airchange[{zones.IndexOf(zone) + 1}]);\n";
                mo += $"    connect(TSetHeating_{zone.name}.y, building.T_setHeating[{zones.IndexOf(zone) + 1}]);\n";
                mo += $"    connect(TSetCooling_{zone.name}.y, building.T_setCooling[{zones.IndexOf(zone) + 1}]);\n";
                mo += $"    connect(heatsources_{zone.name}.y, heatFlow[{zones.IndexOf(zone) + 1}].Q_flow);\n";
            }

            mo += "    connect(relationRadiationConvection.heatPort, heatFlow.port);\n";
            mo += $"    connect(relationRadiationConvection.heatPortCv, building.conHeatSourcesPorts[1:{zones.Count}]);\n";
            mo += $"    connect(relationRadiationConvection.heatPortLw, building.radHeatSourcesPorts[1:{zones.Count}]);\n";

            mo += $"end {projName};\n";

            return mo;
        }
    }
}
