using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace Ovenbird
{

    class PointAlignment
    {
        /// <summary>
        /// Alignment step 1: get the joints and their reaching hands
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="hands"></param>
        /// <returns></returns>
        public static List<Point3d> GetJoints(List<Curve> lines, out List<List<Vector3d>> hands)
        {
            List<Curve> crvs = SkimOut(ShatterCrvs(lines), 0.001);

            List<Point3d> Vtc = new List<Point3d>(); // all unique vertices
            List<Curve> HC = new List<Curve>(); // list of all shattered half-curves
            List<int> HCI = new List<int>(); // half curve indices
            List<int> HCO = new List<int>(); // half curve reversed
            List<int> HCV = new List<int>(); // vertex representing this half-curve
                                             // (if it either starts or ends hanging, but does not exclude redundant curves that not exclosing a room)
            Dictionary<int, List<int>> VOut = new Dictionary<int, List<int>>(); // data tree of outgoing half-curves from each vertex
            Dictionary<int, List<Vector3d>> VJoint = new Dictionary<int, List<Vector3d>>();

            foreach (Curve crv in crvs) // cycle through each curve
            {
                for (int CRun = 0; CRun <= 2; CRun += 2) // create two half-curves: first in one direction, and then the other...
                {
                    Point3d testedPt = new Point3d();

                    HC.Add(crv);
                    testedPt = crv.PointAtStart;
                    HCI.Add(HCI.Count); // count this iteration
                    HCO.Add(HCI.Count - CRun); // a little index trick

                    int VtcSet = -1;

                    for (int VtxCheck = 0; VtxCheck <= Vtc.Count - 1; VtxCheck++)
                    {
                        if (Vtc[VtxCheck].DistanceTo(testedPt) < 0.000001)
                        {
                            VtcSet = VtxCheck; // get the vertex index, if it already exists
                            break;
                        }
                    }

                    Vector3d paleVec = RoundVec(HC[HCI.Last()].TangentAtStart, 0.0001);
                    if (VtcSet > -1)
                    {
                        HCV.Add(VtcSet); // If the vertex already exists, set the half-curve vertex
                        VOut[VtcSet].Add(HCI.Last());
                        if (!VJoint[VtcSet].Contains(paleVec))
                            VJoint[VtcSet].Add(paleVec);
                    }
                    else
                    {
                        HCV.Add(Vtc.Count); // if the vertex doesn't already exist, add a new vertex index
                        VOut.Add(Vtc.Count, new List<int>() { HCI.Last() });
                        VJoint.Add(Vtc.Count, new List<Vector3d>() { paleVec });
                        // add the new half-curve index to the list of outgoing half-curves associated with the vertex
                        Vtc.Add(testedPt);
                        // add the new vertex to the vertex list
                    }
                    crv.Reverse(); // reverse the curve for creating the opposite half-curve in the second part of the loop
                                   //Debug.Print("Tested point is (" + testedPt.X.ToString() + ", " + testedPt.Y.ToString() + ")");
                }
            }
            List<List<Vector3d>> vecList = new List<List<Vector3d>>();
            foreach (List<Vector3d> vecs in VJoint.Values)
            {
                vecList.Add(vecs);
            }

            // OUTPUT
            hands = vecList;
            return Vtc;
        }









        /// <summary>
        /// Main function of this component to align points on certain direction
        /// </summary>
        /// <param name="pts"></param>
        /// <param name="vecList"></param>
        /// <param name="theta"></param>
        /// <param name="delta"></param>
        /// <param name="anchorInfo"></param>
        /// <returns></returns>
        public static List<Point3d> AlignPts(
          List<Point3d> pts, List<List<Vector3d>> vecList,
          double theta, double delta, double tolerance,
          out List<List<Vector3d>> anchorInfo)
        //out List<Line> axes, out List<List<Point3d>> ptGroups)
        {
            // Copy the original points for iteration
            List<Point3d> ptPool = new List<Point3d>();
            List<int> ptIdPool = new List<int>();
            for (int i = 0; i < pts.Count; i++)
            {
                ptPool.Add(pts[i]);
                ptIdPool.Add(i);
            }


            // Results declearation
            List<Point3d> ptRastered = new List<Point3d>();
            List<int> ptIdRastered = new List<int>();
            //axes = new List<Line>();
            //ptGroups = new List<List<Point3d>>();


            // Define exact alignment direction
            Vector3d scanRay = new Vector3d(Math.Cos(theta), Math.Sin(theta), 0);

            while (ptPool.Count > 0)
            {
                List<Point3d> ptGroup = new List<Point3d>() { ptPool[0] };
                List<int> ptIdGroup = new List<int>() { ptIdPool[0] };
                ptPool.RemoveAt(0);
                ptIdPool.RemoveAt(0);

                // Generate the group of points almost in line
                for (int i = 0; i < ptGroup.Count; i++)
                {
                    for (int j = ptPool.Count - 1; j >= 0; j--)
                    {
                        if (ptGroup[i] != ptPool[j])
                        {
                            double distance = DistanceToRay(ptGroup[i], ptPool[j], scanRay);
                            if (distance < delta)
                            {
                                ptGroup.Add(ptPool[j]);
                                ptIdGroup.Add(ptIdPool[j]);
                                ptPool.RemoveAt(j);
                                ptIdPool.RemoveAt(j);
                            }
                        }
                    }
                }
                //ptGroups.Add(ptGroup);

                List<int> escapeIdx = new List<int>();

                // as to orphan point just ignore it
                if (ptGroup.Count == 1)
                {
                    ptRastered.Add(ptGroup[0]);
                    ptIdRastered.Add(ptIdGroup[0]);
                }

                // if more than one generate the axis
                if (ptGroup.Count > 1)
                {
                    // rotate the world plane so the alignment happens on Y axis
                    List<Point3d> transPts = new List<Point3d>();
                    List<int> transIdx = new List<int>();
                    for (int i = 0; i < ptGroup.Count; i++)
                    {
                        transPts.Add(new Point3d(
                          ptGroup[i].X * Math.Cos(theta) + ptGroup[i].Y * Math.Sin(theta),
                          ptGroup[i].Y * Math.Cos(theta) - ptGroup[i].X * Math.Sin(theta),
                          0));
                        transIdx.Add(i);
                    }

                    // these can be recoded to something with LINQ
                    List<List<int>> tempIdGroups = new List<List<int>>();
                    while (transIdx.Count > 0)
                    {
                        List<int> tempIdGroup = new List<int>() { transIdx[0] };
                        transIdx.RemoveAt(0);
                        for (int i = 0; i < tempIdGroup.Count; i++)
                        {
                            for (int j = transIdx.Count - 1; j >= 0; j--)
                            {
                                if (transPts[tempIdGroup[i]].Y == transPts[transIdx[j]].Y)
                                {
                                    tempIdGroup.Add(transIdx[j]);
                                    transIdx.RemoveAt(j);
                                }
                            }
                        }
                        tempIdGroups.Add(tempIdGroup);
                    }

                    int axisIdx = 0;
                    int maxMember = 0;
                    for (int i = 0; i < tempIdGroups.Count; i++)
                    {
                        if (tempIdGroups[i].Count > maxMember)
                        {
                            maxMember = tempIdGroups[i].Count;
                            axisIdx = i;
                        }
                    }


                    List<int> axisIdPts = tempIdGroups[axisIdx];
                    foreach (int idx in axisIdPts)
                        escapeIdx.Add(ptIdGroup[idx]);

                    List<Point3d> axisPts = new List<Point3d>();
                    foreach (int idx in axisIdPts)
                        axisPts.Add(transPts[idx]);
                    BubbleSortPtsOnX(axisPts, axisIdPts);
                    double axisY = axisPts[0].Y;

                    // DEBUG REGION
                    /*
                    Rhino.RhinoApp.WriteLine("-------------------within one group-------------------");
                    Rhino.RhinoApp.Write("SeqAxisPts: ");
                    foreach (int idx in tempIdGroups[axisIdx])
                    {
                    Rhino.RhinoApp.Write(ptIdGroup[idx].ToString() + ", ");
                    }
                    Rhino.RhinoApp.Write("\n");
                    */

                    // transform reversal and get the axis
                    // no digit rounding here
                    Point3d p1 = new Point3d(
                      axisPts[0].X * Math.Cos(theta) - axisY * Math.Sin(theta),
                      axisPts[0].X * Math.Sin(theta) + axisY * Math.Cos(theta),
                      0);
                    Point3d p2 = new Point3d(
                      axisPts.Last().X * Math.Cos(theta) - axisY * Math.Sin(theta),
                      axisPts.Last().X * Math.Sin(theta) + axisY * Math.Cos(theta),
                      0);

                    List<double> splits = new List<double>(); // section point position
                    List<bool> isConnected = new List<bool>(); // connectivity at each section
                    Line axis = new Line();

                    if (maxMember == 1) // one point as axis
                    {
                        p2 = p1 + scanRay;
                        axis = new Line(p1, p2);
                        splits.Add(0);
                        isConnected.Add(false);
                    }
                    else if (maxMember > 1) // multiple points on the same axis
                    {
                        axis = new Line(p1, p2);
                        for (int i = 0; i < axisIdPts.Count; i++)
                        {
                            double split = (axisPts[i].X - axisPts[0].X) / (axisPts.Last().X - axisPts[0].X);
                            splits.Add(split);
                            if (!IsLoadable(vecList[ptIdGroup[axisIdPts[i]]], scanRay, tolerance))
                            {
                                isConnected.Add(true);
                            }
                            else
                            {
                                isConnected.Add(false);
                            }
                        }
                    }

                    // DEBUG REGION
                    /*
                    Rhino.RhinoApp.WriteLine("CHECK ESCAPING IDX: " + escapeIdx.Count.ToString());
                    Rhino.RhinoApp.WriteLine("ANCHOR POINT ({0}, {1})", axis.PointAt(0).X, axis.PointAt(0).Y);
                    Rhino.RhinoApp.WriteLine("ANCHOR POINT NUM: " + axisPts.Count.ToString());
                    DisplayList(splits);
                    DisplayBoolList(isConnected);
                    */

                    //AXES.Add(axis);


                    // travers each point group and align the joints to anchors
                    for (int i = 0; i < ptGroup.Count; i++)
                    {
                        if (tempIdGroups[axisIdx].Contains(i))
                        {
                            //Rhino.RhinoApp.WriteLine("One point belongs to the axis");
                            ptRastered.Add(ptGroup[i]);
                            ptIdRastered.Add(ptIdGroup[i]);
                            continue;
                        }

                        double t;
                        Point3d plummet;
                        double distance = DistanceToLine(ptGroup[i], axis, out plummet, out t);
                        //Rhino.RhinoApp.WriteLine("I GOT T! " + t.ToString());

                        int insertIdx = -1;
                        for (int j = 0; j < splits.Count; j++)
                        {
                            if (t >= splits[j])
                                insertIdx++;
                        }

                        //Rhino.RhinoApp.WriteLine("Insert point location: " + insertIdx.ToString());

                        // here the Rhino global tolerance should be used, very tiny
                        Vector3d moveDirection = RoundVec(new Vector3d(
                          plummet.X - ptGroup[i].X,
                          plummet.Y - ptGroup[i].Y,
                          0), 0.00000001);
                        moveDirection.Unitize();
                        //Rhino.RhinoApp.WriteLine("EVIL VEC IS ({0}, {1})", moveDirection.X, moveDirection.Y);

                        // t value between 0 - 1 means the point moves onto the axis
                        // these may happen at the same time
                        if (t >= 0 && t <= 0)
                        {
                            //
                            if (!IsLoadable(vecList[ptIdGroup[i]], moveDirection, tolerance))
                            {
                                //Rhino.RhinoApp.WriteLine("HERE WE DELETE AT: " + ptIdGroup[i].ToString());
                                vecList[ptIdGroup[i]].Remove(moveDirection);
                            }
                        }

                        if (t > 0 && t < 1)
                        {
                            //Rhino.RhinoApp.WriteLine("More vectors are added!");
                            if (isConnected[insertIdx])
                            {
                                if (IsLoadable(vecList[ptIdGroup[i]], scanRay, tolerance))
                                {
                                    vecList[ptIdGroup[i]].Add(scanRay);
                                }
                                if (IsLoadable(vecList[ptIdGroup[i]], -scanRay, tolerance))
                                {
                                    vecList[ptIdGroup[i]].Add(-scanRay);
                                }
                            }
                        }

                        ptRastered.Add(plummet);
                        ptIdRastered.Add(ptIdGroup[i]);
                    }
                }
            }

            // Future LINQ here
            //ptRastered = ptRastered.OrderBy(d => ptIdRastered.IndexOf(d.Id)).ToList();
            List<Point3d> ptAfter = new List<Point3d>();
            for (int i = 0; i < ptRastered.Count; i++)
            {
                ptAfter.Add(ptRastered[ptIdRastered.IndexOf(i)]);
            }

            // generate a vector list for all transform directions
            List<Vector3d> vecTrans = new List<Vector3d>();
            for (int i = 0; i < pts.Count; i++)
            {
                Vector3d vec = new Vector3d(ptAfter[i].X - pts[i].X,
                  ptAfter[i].Y - pts[i].Y, 0);
                vec = RoundVec(vec, 0.0000001);
                vec.Unitize();
                vecTrans.Add(vec);
            }

            // DEBUG REGION
            //Rhino.RhinoApp.WriteLine("Alignement check: {0} - {1} - {2}",
            //  ptAfter.Count, vecList.Count, vecTrans.Count);

            // merge coincident points to an anchor
            List<Point3d> anchors = new List<Point3d>();
            anchorInfo = new List<List<Vector3d>>();
            while (ptAfter.Count > 0)
            {
                //Rhino.RhinoApp.Write("Loop {0}: ", counter);
                anchors.Add(ptAfter[0]);
                List<Vector3d> pileVec = vecList[0];
                for (int i = ptAfter.Count - 1; i > 0; i--)
                {
                    // Here the coincident point event happens
                    if (PtAlmostTheSame(ptAfter[0], ptAfter[i], tolerance))
                    {
                        for (int j = pileVec.Count - 1; j >= 0; j--)
                        {
                            // if a point receive another point,
                            // it should delete the corresponding vector of that direction
                            if (VecAlmostTheSame(pileVec[j], -vecTrans[i], tolerance))
                            {
                                pileVec.RemoveAt(j);
                            }
                        }
                        // pile vectors of merged points if they are different
                        foreach (Vector3d vec in vecList[i])
                        {
                            if (IsLoadable(pileVec, vec, tolerance))
                                pileVec.Add(vec);
                        }
                        ptAfter.RemoveAt(i);
                        vecList.RemoveAt(i);
                        vecTrans.RemoveAt(i);
                    }
                }
                anchorInfo.Add(pileVec);

                ptAfter.RemoveAt(0);
                vecList.RemoveAt(0);
                vecTrans.RemoveAt(0);
            }


            // DEBUG REGION
            //Rhino.RhinoApp.WriteLine("After merging check: {0} - {1} - {2}",
            //  ptAfter.Count, vecList.Count, vecTrans.Count);


            // after fixation
            for (int i = anchors.Count - 1; i >= 0; i--)
            {
                // skim out points on the line that are redundant
                if (anchorInfo[i].Count == 2)
                {
                    if (VecAlmostTheSame(anchorInfo[i][0], -anchorInfo[i][1], tolerance))
                    {
                        anchors.RemoveAt(i);
                        anchorInfo.RemoveAt(i);
                        //vecTrans.RemoveAt(i);
                    }
                }
                // skim out points with no outgoing vectors
                // highly not possible
                // add these lines will cause termination sometimes
                // still don't know how this function malfunctions
                /*
                if (anchorInfo[i].Count == 0)
                {
                    anchors.RemoveAt(i);
                    anchorInfo.RemoveAt(i);
                }
                */
            }

            // DEBUG REGION
            //Rhino.RhinoApp.WriteLine("Final check: anchor/{0} - info/{1}",
            //  anchors.Count, anchorInfo.Count, vecTrans.Count);

            return anchors;
        }


        /// <summary>
        /// Alignment step 3: Recreate lattice from the aligned anchors
        /// </summary>
        /// <param name="anchors"></param>
        /// <param name="anchorInfo"></param>
        /// <param name="tolerance"></param>
        /// <param name="strays"></param>
        /// <returns></returns>
        public static List<List<Line>> GetLattice(List<Point3d> anchors, List<List<Vector3d>> anchorInfo,
            double tolerance, out List<Line> strays)
        {
            // loop to get all stray lines
            List<int> anchorVecCount = new List<int>();
            strays = new List<Line>();
            foreach (List<Vector3d> vecs in anchorInfo)
            {
                anchorVecCount.Add(vecs.Count);
            }
            int counter = 0;

            // loop until the vector with only one outgoing vector cannot be found
            // or ?
            while (anchorVecCount.IndexOf(1) != -1)
            {
                counter++;
                //Rhino.RhinoApp.WriteLine(anchorVecCount.IndexOf(1).ToString());
                int pointer = anchorVecCount.IndexOf(1);
                //Rhino.RhinoApp.WriteLine("Found orphan point at " + pointer.ToString());
                double max = double.PositiveInfinity;
                int endId = -1;
                for (int i = anchors.Count - 1; i >= 0; i--)
                {
                    if (pointer != i)
                    {
                        double stretch;
                        double distance = DistanceToRay(anchors[i], anchors[pointer],
                          anchorInfo[pointer][0], out stretch);
                        if (distance < tolerance && stretch > 0)
                        {
                            if (stretch < max)
                            {
                                max = stretch;
                                endId = i;
                            }
                        }
                    }
                }
                if (endId >= 0)
                {
                    strays.Add(new Line(anchors[pointer], anchors[endId]));
                    anchorInfo[endId].Remove(-anchorInfo[pointer][0]);
                    anchorVecCount[endId] = anchorVecCount[endId] - 1;
                    anchorVecCount.RemoveAt(pointer);
                    anchors.RemoveAt(pointer);
                    anchorInfo.RemoveAt(pointer);
                }
                // a safe lock
                if (counter > 200)
                    break;
            }
            //Rhino.RhinoApp.WriteLine("Iterate {0} times!", counter);

            List<List<Line>> grids = new List<List<Line>>();

            for (int i = 0; i < anchors.Count; i++)
            {
                List<Line> grid = new List<Line>();
                foreach (Vector3d vec in anchorInfo[i])
                {
                    double max = double.PositiveInfinity;
                    int endId = -1;
                    for (int j = 0; j < anchors.Count; j++)
                    {
                        if (i != j)
                        {
                            //Rhino.RhinoApp.Write("Loop: {0} | ", j);
                            double stretch;

                            double distance = DistanceToRay(anchors[j], anchors[i], vec,
                              out stretch);
                            //Rhino.RhinoApp.Write(stretch.ToString() + "\n");
                            if (distance < tolerance && stretch > 0)
                            {
                                if (stretch < max)
                                {
                                    max = stretch;
                                    endId = j;
                                }
                            }
                        }
                    }

                    if (endId >= 0)
                    {
                        grid.Add(new Line(anchors[i], anchors[endId]));
                        //anchorInfo[endId].Remove(-vec);
                        for (int j = anchorInfo[endId].Count - 1; j >= 0; j--)
                        {
                            if (VecAlmostTheSame(anchorInfo[endId][j], -vec, tolerance))
                                anchorInfo[endId].RemoveAt(j);
                        }
                    }
                }
                grids.Add(grid);
            }

            return grids;
        }

        #region support functions
        public static List<Curve> SkimOut(List<Curve> crvs, double tolerance)
        {
            for (int i = crvs.Count - 1; i >= 0; i--)
            {
                if (crvs[i].GetLength() < tolerance)
                    crvs.RemoveAt(i);
            }
            return crvs;
        }

        public static Vector3d RoundVec(Vector3d vec, double tolerance)
        {
            if (Math.Abs(vec.X) < tolerance)
                vec.X = 0;
            if (Math.Abs(vec.Y) < tolerance)
                vec.Y = 0;
            if (Math.Abs(vec.Z) < tolerance)
                vec.Z = 0;
            return vec;
        }


        public static List<Curve> ShatterCrvs(List<Curve> crvs)
        {
            List<Curve> shatteredCrvs = new List<Curve>();

            for (int i = 0; i <= crvs.Count - 1; i++)
            {
                List<double> breakParams = new List<double>();
                for (int j = 0; j <= crvs.Count - 1; j++)
                {
                    if (i != j)
                    {
                        CurveIntersections CI = Intersection.CurveCurve(
                          crvs[i], crvs[j], 0.00001, 0.00001);
                        foreach (IntersectionEvent IE in CI)
                        {
                            breakParams.Add(IE.ParameterA);
                        }
                    }
                    else
                    {
                        continue;
                    }
                }
                shatteredCrvs.AddRange(crvs[i].Split(breakParams));
            }
            return shatteredCrvs;
        }


        public static double DistanceToRay(Point3d pt, Point3d origin, Vector3d vec)
        {
            double dx = vec.X;
            double dy = vec.Y;

            // Calculate the t that minimizes the distance.
            double t = ((pt.X - origin.X) * dx + (pt.Y - origin.Y) * dy) /
              (dx * dx + dy * dy);

            dx = pt.X - (origin.X + t * dx);
            dy = pt.Y - (origin.Y + t * dy);
            return Math.Sqrt(dx * dx + dy * dy);
        }


        public static double DistanceToLine(Point3d pt, Line line,
          out Point3d plummet, out double stretch)
        {
            double dx = line.PointAt(1).X - line.PointAt(0).X;
            double dy = line.PointAt(1).Y - line.PointAt(0).Y;
            Point3d origin = line.PointAt(0);

            if ((dx == 0) && (dy == 0)) // zero length segment
            {
                plummet = origin;
                stretch = 0;
                dx = pt.X - origin.X;
                dy = pt.Y - origin.Y;
                return Math.Sqrt(dx * dx + dy * dy);
            }

            // Calculate the t that minimizes the distance.
            stretch = ((pt.X - origin.X) * dx + (pt.Y - origin.Y) * dy) /
              (dx * dx + dy * dy);

            plummet = new Point3d(origin.X + stretch * dx, origin.Y + stretch * dy, 0);
            dx = pt.X - plummet.X;
            dy = pt.Y - plummet.Y;

            return Math.Sqrt(dx * dx + dy * dy);
        }


        public static void BubbleSortPtsOnX(List<Point3d> pts, List<int> idx)
        {
            Point3d tempPt;
            int tempId;
            for (int i = 0; i < pts.Count; i++)
            {
                for (int j = 0; j < pts.Count - i - 1; j++)
                {
                    if (pts[j].X > pts[j + 1].X)
                    {
                        tempPt = pts[j + 1];
                        pts[j + 1] = pts[j];
                        pts[j] = tempPt;
                        tempId = idx[j + 1];
                        idx[j + 1] = idx[j];
                        idx[j] = tempId;
                    }
                }
            }
        }

        public static bool PtAlmostTheSame(Point3d pt1, Point3d pt2, double tolerance)
        {
            if (Math.Abs(pt1.X - pt2.X) < tolerance &&
              Math.Abs(pt1.Y - pt2.Y) < tolerance &&
              Math.Abs(pt1.Z - pt2.Z) < tolerance)
                return true;
            else
                return false;
        }

        public static bool VecAlmostTheSame(Vector3d vec1, Vector3d vec2, double tolerance)
        {
            if (Math.Abs(vec1.X - vec2.X) < tolerance &&
              Math.Abs(vec1.Y - vec2.Y) < tolerance &&
              Math.Abs(vec1.Z - vec2.Z) < tolerance)
                return true;
            else
                return false;
        }

        public static bool IsLoadable(List<Vector3d> vecs, Vector3d newVec, double tolerance)
        {
            int counter = 0;
            foreach (Vector3d vec in vecs)
            {
                if (VecAlmostTheSame(vec, newVec, tolerance))
                    counter++;
            }
            if (counter > 0)
                return false;
            else
                return true;
        }

        public static double DistanceToRay(
    Point3d pt, Point3d origin, Vector3d vec, out double stretch)
        {
            double dx = vec.X;
            double dy = vec.Y;

            // Calculate the t that minimizes the distance.
            double t = ((pt.X - origin.X) * dx + (pt.Y - origin.Y) * dy) /
              (dx * dx + dy * dy);

            Point3d closest = new Point3d(origin.X + t * dx, origin.Y + t * dy, 0);
            dx = pt.X - (origin.X + t * dx);
            dy = pt.Y - (origin.Y + t * dy);
            //stretch = t * Math.Sqrt(dx * dx + dy * dy);
            stretch = closest.DistanceTo(origin);
            if (t < 0)
                stretch = -stretch;
            //Rhino.RhinoApp.WriteLine("this distance is: " + stretch.ToString());
            return Math.Sqrt(dx * dx + dy * dy);
        }

        #endregion
    }
}