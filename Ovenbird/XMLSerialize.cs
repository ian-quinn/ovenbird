﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Globalization;
using System.Text.RegularExpressions;

using Rhino.Geometry;

namespace Ovenbird
{
    class XMLSerialize
    {
        public static void Generate(string XMLpath, List<gbZone> zones, List<gbFloor> floors, List<gbSurface> faces)
        //    Dictionary<string, string> adjDict)
        {
            gb.gbci = new CultureInfo(String.Empty);

            //the basics
            //constructor to define the basics
            gbXML gbx = new gbXML();
            gbx.lengthUnit = lengthUnitEnum.Feet;
            gbx.temperatureUnit = temperatureUnitEnum.F;

            Campus cmp = CreateCampus("sample_0");
            
            cmp.Buildings = new Building[10000];
            gbx.Campus = cmp; // backward mapping

            //where does this location information from?  it could be smartly inferred somehow, but otherwise specified by the user/programmer
            Location zeloc = new Location();
            zeloc.Name = "???";
            zeloc.Latitude = "00.00";
            zeloc.Longitude = "00.00";
            cmp.Location = zeloc; // backward mapping

            // set an array as big as possible, revise here
            cmp.Buildings[0] = MakeBuilding(10000, "bldg_0", buildingTypeEnum.AutomotiveFacility);

            // STOREY
            for (int i = 0; i < floors.Count; i++)
                cmp.Buildings[0].bldgStories[i] = MakeStorey(floors[i].level, floors[i].loop);

            // SPACE
            for (int i = 0; i < zones.Count; i++)
                cmp.Buildings[0].Spaces[i] = MakeSpace(zones[i], i);

            // SURFACE
            List<gbSurface> uniqueSrfs = new List<gbSurface>();
            // i think we just do not declare this dictionary as an attribute of the class
            cmp.Surface = new Surface[faces.Count];
            int srfCounter = 0;
            for (int i = 0; i < faces.Count; i++)
            {
                if (faces[i].loop.Count < 4)
                    Util.LogPrint("Degenerated surface detected at: " + i);
                //Util.LogPrint(faces[i].id + "-" + faces[i].adjSrfId);
                if (IsDuplicateSrf(faces[i], uniqueSrfs))
                    continue;
                uniqueSrfs.Add(faces[i]);

                cmp.Surface[i] = MakeSurface(faces[i], srfCounter);
                srfCounter++;
            }


            //write xml to the file
            XmlSerializer writer = new XmlSerializer(typeof(gbXML));
            FileStream file = File.Create(XMLpath);
            writer.Serialize(file, gbx);
            file.Close();
        }

        #region geometric info translate
        public static CartesianPoint PtToCartesianPoint(gbXYZ pt)
        {
            CartesianPoint cpt = new CartesianPoint();
            cpt.Coordinate = new string[3];
            CultureInfo ci = new CultureInfo(String.Empty);
            string xformat = string.Format(ci, "{0:0.000000}", pt.X);
            string yformat = string.Format(ci, "{0:0.000000}", pt.Y);
            string zformat = string.Format(ci, "{0:0.000000}", pt.Z);
            cpt.Coordinate[0] = xformat;
            cpt.Coordinate[1] = yformat;
            cpt.Coordinate[2] = zformat;
            return cpt;
        }

        // note that all polyloops are not enclosed
        // also the input ptsLoop here is not closed
        public static PolyLoop PtsToPolyLoop(List<gbXYZ> ptsLoop)
        {
            PolyLoop pl = new PolyLoop();
            pl.Points = new CartesianPoint[ptsLoop.Count];
            for (int i = 0; i < ptsLoop.Count; i++)
            {
                CartesianPoint cpt = PtToCartesianPoint(ptsLoop[i]);
                pl.Points[i] = cpt;
            }
            return pl;
        }
        #endregion

        #region XML class translate
        public static Campus CreateCampus(string id)
        {
            Campus cmp = new Campus();
            cmp.id = id;
            return cmp;
        }

        public static Building MakeBuilding(double bldarea, string bldgname, buildingTypeEnum bldgType)
        {
            Building zeb = new Building();
            zeb.Area = bldarea.ToString();
            zeb.id = bldgname;
            zeb.buildingType = bldgType;
            //this has been arbitrarily defined and could be changed
            zeb.bldgStories = new BuildingStorey[1000];
            zeb.Spaces = new Space[10000];
            return zeb;
        }

        public static BuildingStorey MakeStorey(gbLevel level, List<gbXYZ> ptsLoop)
        {
            BuildingStorey bs = new BuildingStorey();
            bs.id = level.label;
            bs.Name = "Story-" + level.id;
            bs.Level = level.elevation.ToString();

            //there is only one plane per storey
            PlanarGeometry pg = new PlanarGeometry();
            pg.PolyLoop = PtsToPolyLoop(ptsLoop);
            bs.PlanarGeo = pg;
            return bs;
        }


        // currently only the default settings added to the space
        public static Space AddSpaceProgram(Space space)
        {
            space.lightScheduleIdRef = "lightSchedule-1";
            space.equipmentScheduleIdRef = "equipmentSchedule-1";
            space.peopleScheduleIdRef = "peopleSchedule-1";
            space.conditionType = "HeatedAndCooled";
            space.buildingStoreyIdRef = "bldg-story-1";
            space.peoplenum = 12;
            space.totalpeoplegain = 450;
            space.senspeoplegain = 250;
            space.latpeoplegain = 200;
            space.PeopleHeatGains = new PeopleHeatGain[3];
            space.lpd = 1.2;
            space.epd = 1.5;

            PeopleNumber pn = new PeopleNumber();
            pn.unit = peopleNumberUnitEnum.NumberOfPeople;

            string people = gb.FormatDoubleToString(space.peoplenum);
            pn.valuefield = people;
            space.PeopleNumber = pn;

            PeopleHeatGain phg = new PeopleHeatGain();
            phg.unit = peopleHeatGainUnitEnum.BtuPerHourPerson;
            phg.heatGainType = peopleHeatGainTypeEnum.Total;
            string totalpopload = gb.FormatDoubleToString(space.totalpeoplegain);
            phg.value = totalpopload;
            space.PeopleHeatGains[0] = phg;

            PeopleHeatGain shg = new PeopleHeatGain();
            shg.unit = peopleHeatGainUnitEnum.BtuPerHourPerson;
            shg.heatGainType = peopleHeatGainTypeEnum.Sensible;
            string senspopload = gb.FormatDoubleToString(space.senspeoplegain);
            shg.value = senspopload;
            space.PeopleHeatGains[1] = shg;

            PeopleHeatGain lhg = new PeopleHeatGain();
            lhg.unit = peopleHeatGainUnitEnum.BtuPerHourPerson;
            lhg.heatGainType = peopleHeatGainTypeEnum.Latent;
            string latpopload = gb.FormatDoubleToString(space.latpeoplegain);
            lhg.value = latpopload;
            space.PeopleHeatGains[2] = lhg;

            LightPowerPerArea lpd = new LightPowerPerArea();
            lpd.unit = powerPerAreaUnitEnum.WattPerSquareFoot;
            lpd.lpd = gb.FormatDoubleToString(space.lpd);
            space.LightPowerPerArea = lpd;

            EquipPowerPerArea epd = new EquipPowerPerArea();
            epd.unit = powerPerAreaUnitEnum.WattPerSquareFoot;
            epd.epd = gb.FormatDoubleToString(space.epd);
            space.EquipPowerPerArea = epd;

            return space;
        }

        public static Space MakeSpace(gbZone zone, int GUID)
        {
            Space space = new Space();

            // SEMANTIC SETTINGS
            space = AddSpaceProgram(space);

            space.id = zone.id;
            space.Name = "Space-" + GUID + "-" + zone.function;
            space.buildingStoreyIdRef = zone.level.label;
            space.Area = zone.area;
            space.Volume = zone.volume;
            space.PlanarGeo = new PlanarGeometry();
            space.ShellGeo = new ShellGeometry();
            space.cadid = new CADObjectId();
            space.cadid.id = "???????";

            Area spacearea = new Area();
            spacearea.val = gb.FormatDoubleToString(space.Area);
            space.spacearea = spacearea;

            Volume spacevol = new Volume();
            spacevol.val = gb.FormatDoubleToString(space.Volume);
            space.spacevol = spacevol;

            // /PLANARGEOMETRY
            PlanarGeometry spaceplpoly = new PlanarGeometry();
            spaceplpoly.PolyLoop = PtsToPolyLoop(zone.loop);
            space.PlanarGeo = spaceplpoly;

            // /SHELLGEOMETRY
            ShellGeometry sg = new ShellGeometry();
            sg.unit = lengthUnitEnum.Meters;
            sg.id = "sg_" + space.Name;

            // /SHELLGEOMETRY /CLOSEDSHELL
            sg.ClosedShell = new ClosedShell();
            sg.ClosedShell.PolyLoops = new PolyLoop[zone.numFaces];
            for (int i = 0; i < zone.numFaces; i++)
            {
                sg.ClosedShell.PolyLoops[i] = PtsToPolyLoop(zone.faces[i].loop);
            }
            space.ShellGeo = sg;

            // SPACEBOUNDARY
            space.spbound = new SpaceBoundary[zone.numFaces];
            for (int i = 0; i < zone.numFaces; i++)
            {
                SpaceBoundary sb = new SpaceBoundary();
                sb.surfaceIdRef = zone.faces[i].id;
                PlanarGeometry pg = new PlanarGeometry();
                pg.PolyLoop = PtsToPolyLoop(zone.faces[i].loop);
                sb.PlanarGeometry = pg;
                space.spbound[i] = sb;
            }

            return space;
        }

        public static Surface MakeSurface(gbSurface face, int GUID)
        {
            Surface surface = new Surface();
            

            // SEMANTIC
            surface.id = face.id;
            surface.Name = "Surface-" + GUID; // false
            surface.surfaceType = face.type;
            if (face.type == surfaceTypeEnum.ExteriorWall ||
                face.type == surfaceTypeEnum.Roof)
                surface.exposedToSunField = true;
            else
                surface.exposedToSunField = false;

            //surface.constructionIdRef = face.id; // back projection to some construction dict

            // there can only be two adjacent spaces for an interior wall
            // this second boudnary split is mandantory for energy simulation
            AdjacentSpaceId adjspace1 = new AdjacentSpaceId();
            adjspace1.spaceIdRef = face.parentId;
            if (face.adjSrfId != "Outside")
            {
                AdjacentSpaceId adjspace2 = new AdjacentSpaceId();
                string[] tags = Regex.Split(face.adjSrfId, "::");
                adjspace2.spaceIdRef = tags[0] + "::" + tags[1];
                AdjacentSpaceId[] adjspaces = { adjspace1, adjspace2 };
                surface.AdjacentSpaceId = adjspaces;
            }
            else
            {
                AdjacentSpaceId[] adjspaces = { adjspace1 };
                surface.AdjacentSpaceId = adjspaces;
            }

            RectangularGeometry rg = new RectangularGeometry();
            rg.Azimuth = face.azimuth.ToString();
            rg.CartesianPoint = PtToCartesianPoint(face.loop[0]);
            rg.Tilt = face.tilt.ToString();
            
            rg.Width = string.Format("{0:0.000000}", face.width);
            rg.Height = string.Format("{0:0.000000}", face.height);
            surface.RectangularGeometry = rg;

            PlanarGeometry pg = new PlanarGeometry();
            pg.PolyLoop = PtsToPolyLoop(face.loop);
            surface.PlanarGeometry = pg;

            // openings
            if (face.openings.Count > 0)
            {
                surface.Opening = new Opening[face.openings.Count];
                for (int i = 0; i < face.openings.Count; i++)
                {
                    Opening op = new Opening();
                    op.id = face.openings[i].id;
                    op.openingType = face.openings[i].type;

                    RectangularGeometry op_rg = new RectangularGeometry();
                    op_rg.Azimuth = face.azimuth.ToString();
                    op_rg.Tilt = face.tilt.ToString();
                    // in gbXML schema, the point here represents the relative position
                    // of the opening and its parent surface. It is calculated by the points
                    // at the left down corner
                    op_rg.CartesianPoint = PtToCartesianPoint(
                        Basic.RelativePt(face.openings[i].loop[0], face.loop[0]));
                    op_rg.Width = string.Format("{0:0.000000}", face.openings[i].width);
                    op_rg.Height = string.Format("{0:0.000000}", face.openings[i].height);
                    op.rg = op_rg;

                    PlanarGeometry op_pg = new PlanarGeometry();
                    op_pg.PolyLoop = PtsToPolyLoop(face.openings[i].loop);
                    op.pg = op_pg;

                    surface.Opening[i] = op;
                }
            }

            return surface;
        }
        /*
        public static List<Space> MakeSpace(List<List<Point3d>> nestedSpace)
        {
            List<Space> spaces = new List<Space>();
            int spacecount = 0;
            foreach (List<Point3d> ptsLoop in nestedSpace)
            {
                Space space = new Space();

                // SEMANTIC SETTINGS
                space = AddSpaceProgram(space);

                space.id = "sp-" + spacecount;
                space.Name = "Space-" + spacecount;
                space.Area = 2450;
                space.Volume = 24500;
                space.PlanarGeo = new PlanarGeometry();
                space.ShellGeo = new ShellGeometry();
                space.cadid = new CADObjectId();
                space.cadid.id = "?????-" + spacecount;

                Area spacearea = new Area();
                spacearea.val = gb.FormatDoubleToString(space.Area);
                space.spacearea = spacearea;

                Volume spacevol = new Volume();
                spacevol.val = gb.FormatDoubleToString(space.Volume);
                space.spacevol = spacevol;

                // /PLANARGEOMETRY
                PlanarGeometry spaceplpoly = new PlanarGeometry();
                spaceplpoly.PolyLoop = PtsToPolyLoop(ptsLoop);
                space.PlanarGeo = spaceplpoly;

                // /SHELLGEOMETRY
                ShellGeometry sg = new ShellGeometry();
                sg.unit = lengthUnitEnum.Meters;
                sg.id = "sg_" + space.Name;

                // /SHELLGEOMETRY /CLOSEDSHELL
                sg.ClosedShell = new ClosedShell();
                sg.ClosedShell.PolyLoops = new PolyLoop[ptsLoop.Count - 1 + 2];
                for (int i = 0; i < ptsLoop.Count - 1; i++)
                {
                    sg.ClosedShell.PolyLoops[i] = ExtrudeLine(ptsLoop[i], ptsLoop[i + 1], 3);
                }
                sg.ClosedShell.PolyLoops[ptsLoop.Count - 1] = PtsToPolyLoop(ptsLoop);
                // following information should be added to the nestedSpace in the future
                List<Point3d> elevatedPts = new List<Point3d>();
                foreach (Point3d pt in ptsLoop)
                    elevatedPts.Add(pt + new Point3d(0, 0, 3));
                sg.ClosedShell.PolyLoops[ptsLoop.Count] = PtsToPolyLoop(elevatedPts);
                space.ShellGeo = sg;

                //make surface boundaries..special code needed so that space boundaries are not duplicated...
                //option 1 : the surfaces already declared as internal somehow and how shared.
                //option 2:  the api tries to figure it out
                space.spbound = new SpaceBoundary[ptsLoop.Count - 1 + 2];
                for (int i = 0; i < ptsLoop.Count - 1; i++)
                {
                    SpaceBoundary sb = new SpaceBoundary();
                    sb.surfaceIdRef = space.Name + "_Srf-" + i;
                    PlanarGeometry pg = new PlanarGeometry();
                    pg.PolyLoop = ExtrudeLine(ptsLoop[i], ptsLoop[i + 1], 3);
                    sb.PlanarGeometry = pg;

                    space.spbound[i] = sb;

                    Surface newsurface = new Surface();
                    cachedSrfGeo.Add(sb.surfaceIdRef, pg);
                }
                SpaceBoundary floor = new SpaceBoundary();
                floor.surfaceIdRef = space.Name + "_Srf-" + (ptsLoop.Count - 1).ToString();
                PlanarGeometry floorplan = new PlanarGeometry();
                floorplan.PolyLoop = PtsToPolyLoop(ptsLoop);
                floor.PlanarGeometry = floorplan;
                space.spbound[ptsLoop.Count - 1] = floor;

                cachedSrfGeo.Add(floor.surfaceIdRef, floorplan);

                SpaceBoundary roof = new SpaceBoundary();
                roof.surfaceIdRef = space.Name + "_Srf-" + ptsLoop.Count;
                PlanarGeometry roofplan = new PlanarGeometry();
                roofplan.PolyLoop = PtsToPolyLoop(elevatedPts);
                roof.PlanarGeometry = roofplan;
                space.spbound[ptsLoop.Count] = roof;

                cachedSrfGeo.Add(roof.surfaceIdRef, roofplan);

                spaces.Add(space);
                spacecount++;
            }
            return spaces;
        }
        */
        #endregion

        public static bool IsDuplicateSrf(gbSurface target, List<gbSurface> faces)
        {
            if (faces.Count == 0)
                return false;
            foreach (gbSurface face in faces)
                if (target.adjSrfId == face.id)
                    return true;
            return false;
        }
    }
}
