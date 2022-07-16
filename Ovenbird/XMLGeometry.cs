﻿using System;
using System.Collections.Generic;
using ClipperLib;
using System.Text.RegularExpressions;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace Ovenbird
{
    class XMLGeometry
    {
        public static void Generate(
            Dictionary<int, Tuple<string, double>> dictElevation, 
            Dictionary<int, List<List<gbXYZ>>> dictLoop, 
            Dictionary<int, List<gbXYZ>> dictShell, 
            Dictionary<int, List<List<string>>> dictMatch, 
            Dictionary<int, List<Tuple<gbXYZ, string>>> dictWindow, 
            Dictionary<int, List<Tuple<gbXYZ, string>>> dictDoor, 
            Dictionary<int, List<Tuple<gbSeg, string>>> dictCurtain, 
            out List<gbZone> zones, 
            out List<gbFloor> floors, 
            out List<gbSurface> surfaces)
        {
            List<gbLevel> levels = new List<gbLevel>();
            int numLevels = dictElevation.Keys.Count;
            foreach (KeyValuePair<int, Tuple<string, double>> kvp in dictElevation)
                levels.Add(new gbLevel(kvp.Key, kvp.Value.Item1, kvp.Value.Item2, numLevels));
            for (int i = 0; i < levels.Count - 1; i++)
                levels[i].height = levels[i + 1].elevation - levels[i].elevation;

            // cached intermediate data
            zones = new List<gbZone>();
            surfaces = new List<gbSurface>();
            floors = new List<gbFloor>();
            // cached spaces by floor for surface matching across levels
            Dictionary<int, List<gbZone>> dictZone = new Dictionary<int, List<gbZone>>();


            //List<List<string>> srfIds = new List<List<string>>();
            //List<List<Line>> boundaryLines = new List<List<Line>>();

            // global opening size regex pattern
            // 0000 x 0000 is the default naming for all opening family types, for now
            string sizeMockup = @"\d+";

            // first loop to add spaces, walls, adjacencies and adhering openings
            foreach (gbLevel level in levels)
            {
                if (level.isTop) break;

                List<gbZone> thisZone = new List<gbZone>();
                List<gbSurface> thisSurface = new List<gbSurface>();
                for (int j = 0; j < dictLoop[level.id].Count; j++)
                {
                    if (dictLoop[level.id][j].Count == 0)
                        continue;
                    gbZone newZone = new gbZone("Level_" + level.id + "::Zone_" + j, level, dictLoop[level.id][j]);
                    thisZone.Add(newZone);
                    //List<string> srfId = new List<string>();
                    //List<Line> boundaryLine = new List<Line>();
                    for (int k = 0; k < dictLoop[level.id][j].Count - 1; k++)
                    {
                        //srfId.Add(newZone.walls[k].id);
                        //boundaryLine.Add(new Line(dictLoop[levelLabel[i]][j][k], dictLoop[levelLabel[i]][j][k + 1]));
                        string adjacency = dictMatch[level.id][j][k];
                        newZone.walls[k].adjSrfId = adjacency;
                        if (adjacency == "Outside")
                            newZone.walls[k].type = surfaceTypeEnum.ExteriorWall;
                        else
                            newZone.walls[k].type = surfaceTypeEnum.InteriorWall;
                    }
                    //srfIds.Add(srfId);
                    //boundaryLines.Add(boundaryLine);
                    thisSurface.AddRange(newZone.walls);
                }
                dictZone.Add(level.id, thisZone);
                surfaces.AddRange(thisSurface);

                // adhere openings to the surface. Note that the overlapping openings are forbidden, 
                // so following this order we create windows first because they are least likely to be mis-drawn 
                // by people and of higher importance in building simulation. Then if the curtain wall has a 
                // collision with windows, cancel its generation. 
                foreach (Tuple<gbXYZ, string> opening in dictWindow[level.id])
                {
                    double minDistance = Double.PositiveInfinity;
                    gbXYZ minPlummet = opening.Item1;
                    int hostId = 0;

                    for (int k = 0; k < thisSurface.Count; k++)
                    {
                        double distance = Basic.PtDistanceToSeg(opening.Item1, thisSurface[k].locationLine,
                            out gbXYZ plummet, out double sectParam);
                        if (distance < minDistance && sectParam > 0 && sectParam < 1)
                        {
                            minDistance = distance;
                            minPlummet = plummet;
                            hostId = k;
                        }
                    }
                    List<double> sizes = new List<double>();
                    List<gbXYZ> openingLoop = new List<gbXYZ>();
                    gbXYZ vec = -thisSurface[hostId].locationLine.Direction;
                    vec.Unitize();

                    foreach (Match match in Regex.Matches(opening.Item2, sizeMockup))
                    {
                        double size = Convert.ToInt32(match.Value) / 1000.0;
                        sizes.Add(size);
                    }
                    //Rhino.RhinoApp.WriteLine("Size: " + sizes[0].ToString() + " / " + sizes[1].ToString());
                    double elevation = opening.Item1.Z;
                    openingLoop.Add(minPlummet - sizes[0] / 2 * vec + new gbXYZ(0, 0, elevation));
                    openingLoop.Add(minPlummet + sizes[0] / 2 * vec + new gbXYZ(0, 0, elevation));
                    openingLoop.Add(minPlummet + sizes[0] / 2 * vec + new gbXYZ(0, 0, elevation + sizes[1]));
                    openingLoop.Add(minPlummet - sizes[0] / 2 * vec + new gbXYZ(0, 0, elevation + sizes[1]));
                    gbOpening newOpening = new gbOpening(thisSurface[hostId].id + "::Opening_" +
                       thisSurface[hostId].openings.Count, openingLoop);
                    newOpening.width = sizes[0];
                    newOpening.height = sizes[1];
                    newOpening.type = openingTypeEnum.FixedWindow;
                    if (!IsOpeningOverlap(thisSurface[hostId].openings, newOpening))
                        thisSurface[hostId].openings.Add(newOpening);
                }

                // as to doors
                foreach (Tuple<gbXYZ, string> opening in dictDoor[level.id])
                {
                    double minDistance = Double.PositiveInfinity;
                    gbXYZ minPlummet = opening.Item1;
                    int hostId = 0;

                    for (int k = 0; k < thisSurface.Count; k++)
                    {
                        double distance = Basic.PtDistanceToSeg(opening.Item1, thisSurface[k].locationLine,
                            out gbXYZ plummet, out double sectParam);
                        if (distance < minDistance && sectParam > 0 && sectParam < 1)
                        {
                            minDistance = distance;
                            minPlummet = plummet;
                            hostId = k;
                        }
                    }
                    List<double> sizes = new List<double>();
                    List<gbXYZ> openingLoop = new List<gbXYZ>();
                    gbXYZ vec = thisSurface[hostId].locationLine.Direction;
                    vec.Unitize();

                    foreach (Match match in Regex.Matches(opening.Item2, sizeMockup))
                    {
                        double size = Convert.ToInt32(match.Value) / 1000.0;
                        sizes.Add(size);
                    }

                    double elevation = opening.Item1.Z;
                    openingLoop.Add(minPlummet - sizes[0] / 2 * vec + new gbXYZ(0, 0, elevation));
                    openingLoop.Add(minPlummet + sizes[0] / 2 * vec + new gbXYZ(0, 0, elevation));
                    openingLoop.Add(minPlummet + sizes[0] / 2 * vec + new gbXYZ(0, 0, elevation + sizes[1]));
                    openingLoop.Add(minPlummet - sizes[0] / 2 * vec + new gbXYZ(0, 0, elevation + sizes[1]));
                    gbOpening newOpening = new gbOpening(thisSurface[hostId].id + "::Opening_" +
                       thisSurface[hostId].openings.Count, openingLoop);
                    newOpening.width = sizes[0];
                    newOpening.height = sizes[1];
                    newOpening.type = openingTypeEnum.NonSlidingDoor;
                    if (!IsOpeningOverlap(thisSurface[hostId].openings, newOpening))
                        thisSurface[hostId].openings.Add(newOpening);
                }

                // curtain wall is the most likely to go wild
                foreach (Tuple<gbSeg, string> opening in dictCurtain[level.id])
                {
                    gbSeg projection;

                    for (int k = 0; k < thisSurface.Count; k++)
                    {
                        // note that the second segment is the baseline
                        // the projection has the same direction as the second segment
                        projection = Basic.SegProjection(opening.Item1, thisSurface[k].locationLine,
                            out double distance);
                        if (projection.Length > 0.5 && distance < 0.1)
                        {
                            List<gbXYZ> openingLoop = new List<gbXYZ>();

                            double elevation = thisSurface[k].loop[0].Z;
                            double height = thisSurface[k].height;
                            openingLoop.Add(projection.PointAt(0) + new gbXYZ(0, 0, elevation));
                            openingLoop.Add(projection.PointAt(1) + new gbXYZ(0, 0, elevation));
                            openingLoop.Add(projection.PointAt(1) + new gbXYZ(0, 0, elevation + height));
                            openingLoop.Add(projection.PointAt(0) + new gbXYZ(0, 0, elevation + height));

                            gbOpening newOpening = new gbOpening(thisSurface[k].id + "::Opening_" +
                               thisSurface[k].openings.Count, Basic.PolyOffset(openingLoop, 0.1, true))
                            {
                                width = projection.Length,
                                height = height,
                                type = openingTypeEnum.FixedWindow
                            };
                            if (!IsOpeningOverlap(thisSurface[k].openings, newOpening))
                                thisSurface[k].openings.Add(newOpening);
                        }
                    }
                }

                floors.Add(new gbFloor("F" + level.id, level, dictShell[level.id]));
            }

            // second loop solve adjacencies among floors
            // perform on already created zones
            foreach (gbLevel level in levels)
            {
                if (level.isTop) break;

                foreach (gbZone zone in dictZone[level.id])
                {
                    // ground slab or roof check
                    if (level.isBottom)
                    {
                        List<gbXYZ> revLoop = zone.loop;
                        revLoop.Reverse();
                        gbSurface floor = new gbSurface(zone.id + "::Floor_0", zone.id, revLoop, 180);
                        floor.type = surfaceTypeEnum.SlabOnGrade;
                        floor.adjSrfId = "Outside";
                        zone.floors.Add(floor);
                    }
                    if (level.id == levels.Count - 2)
                    {
                        gbSurface ceiling = new gbSurface(zone.id + "::Ceil_0", zone.id,
                            Basic.ElevatePtsLoop(zone.loop, level.height), 0);
                        ceiling.type = surfaceTypeEnum.Roof;
                        ceiling.adjSrfId = "Outside";
                        zone.ceilings.Add(ceiling);
                    }

                    // exposed floor or offset roof check
                    if (level.id != levels.Count - 2)
                        if (!Basic.IsPolyInPoly(Basic.ElevatePtsLoop(zone.loop, 0), dictShell[level.nextId]))
                        {
                            List<List<gbXYZ>> sectLoops = Basic.ClipPoly(zone.loop, dictShell[level.nextId], ClipType.ctDifference);
                            if (sectLoops.Count != 0)
                            {
                                for (int j = 0; j < sectLoops.Count; j++)
                                {
                                    gbSurface splitCeil = new gbSurface(zone.id + "::Ceil_" + zone.ceilings.Count, zone.id,
                                        Basic.ElevatePtsLoop(sectLoops[j], level.elevation + level.height), 0);
                                    splitCeil.adjSrfId = "Outside";
                                    splitCeil.type = surfaceTypeEnum.Roof;
                                    zone.ceilings.Add(splitCeil);
                                }
                            }
                        }
                    if (!level.isBottom)
                        if (!Basic.IsPolyInPoly(Basic.ElevatePtsLoop(zone.loop, 0), dictShell[level.prevId]))
                        {
                            List<List<gbXYZ>> sectLoops = Basic.ClipPoly(zone.loop, dictShell[level.prevId], ClipType.ctDifference);
                            if (sectLoops.Count != 0)
                            {
                                for (int j = 0; j < sectLoops.Count; j++)
                                {
                                    List<gbXYZ> revLoop = Basic.ElevatePtsLoop(sectLoops[j], level.elevation);
                                    revLoop.Reverse();
                                    gbSurface splitFloor = new gbSurface(zone.id + "::Floor_" + zone.floors.Count, zone.id,
                                        revLoop, 180);
                                    splitFloor.adjSrfId = "Outside";
                                    splitFloor.type = surfaceTypeEnum.ExposedFloor;
                                    zone.floors.Add(splitFloor);
                                }
                            }
                        }

                    // interior floor adjacency check
                    if (level.id != levels.Count - 2)
                        foreach (gbZone adjZone in dictZone[level.id + 1])
                        {
                            List<List<gbXYZ>> sectLoops = Basic.ClipPoly(zone.loop, adjZone.loop, ClipType.ctIntersection);
                            if (sectLoops.Count == 0)
                                continue;
                            for (int j = 0; j < sectLoops.Count; j++)
                            {
                                // the name does not matter
                                // they only have to stay coincident so the adjacent spaces can be tracked
                                string splitCeilId = zone.id + "::Ceil_" + zone.ceilings.Count;
                                string splitFloorId = adjZone.id + "::Floor_" + zone.floors.Count;
                                // be cautious here
                                // the ceiling here mean the shadowing floor, so the tile is still 180
                                List<gbXYZ> revLoop = Basic.ElevatePtsLoop(sectLoops[j], adjZone.level.elevation);
                                revLoop.Reverse();
                                gbSurface splitCeil = new gbSurface(splitCeilId, zone.id, revLoop, 180);
                                gbSurface splitFloor = new gbSurface(splitFloorId, adjZone.id, revLoop, 180);

                                splitCeil.adjSrfId = splitFloorId;
                                splitCeil.type = surfaceTypeEnum.InteriorFloor;
                                zone.ceilings.Add(splitCeil);
                                
                                splitFloor.adjSrfId = splitCeilId;
                                splitFloor.type = surfaceTypeEnum.InteriorFloor;
                                adjZone.floors.Add(splitFloor);

                            }
                        }
                    surfaces.AddRange(zone.floors);
                    surfaces.AddRange(zone.ceilings);
                }

                zones.AddRange(dictZone[level.id]);
            }

            // third loop summarize all faces
            foreach (gbZone zone in zones)
                zone.Summarize();
        }

        static bool IsOpeningOverlap(List<gbOpening> openings, gbOpening newOpening)
        {
            List<gbXYZ> loop2d = Basic.PolyToPoly2D(newOpening.loop);
            foreach (gbOpening opening in openings)
            {
                List<gbXYZ> opening2d = Basic.PolyToPoly2D(opening.loop);
                if (Basic.IsPolyOverlap(loop2d, opening2d))
                    return true;
            }
            return false;
        }
    }
}
