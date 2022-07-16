//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Grasshopper;
//using Grasshopper.Kernel;
//using Grasshopper.Kernel.Types;
//using Rhino.Geometry;

//namespace Ovenbird
//{
//    enum BooleanOperation
//    {
//        Union,
//        Intersect,
//        Difference
//    }

//    struct Pair<T> : IEquatable<Pair<T>>
//    {
//        readonly T first;
//        readonly T second;

//        public Pair(T first, T second)
//        {
//            this.first = first;
//            this.second = second;
//        }

//        public T First { get { return first; } }
//        public T Second { get { return second; } }

//        public override int GetHashCode()
//        {
//            return first.GetHashCode() ^ second.GetHashCode();
//        }

//        public override bool Equals(object obj)
//        {
//            if (obj == null || GetType() != obj.GetType())
//            {
//                return false;
//            }
//            return Equals((Pair<T>)obj);
//        }

//        public bool Equals(Pair<T> other)
//        {
//            return other.first.Equals(first) && other.second.Equals(second) ||
//                    other.first.Equals(second) && other.second.Equals(first);
//        }
//    }

//    class PolygonClip
//    {
//        public static List<List<Point3d>> Process(List<Point3d> subjectLoop,
//            List<Point3d> clipLoop, BooleanOperation operation, 
//            out List<Point3d> sectPts, out List<Point3d> enterPts, out List<Point3d> exitPts)
//        {

//            CircularLinkedList<Point3d> subject = new CircularLinkedList<Point3d>();
//            CircularLinkedList<Point3d> clip = new CircularLinkedList<Point3d>();
//            foreach (Point3d pt in subjectLoop)
//                subject.AddLast(pt);
//            foreach (Point3d pt in clipLoop)
//                clip.AddLast(pt);

//            List<CircularLinkedList<Point3d>> polygons = new List<CircularLinkedList<Point3d>>();
//            List<List<Point3d>> polyLoops = new List<List<Point3d>>();
//            CircularLinkedList<Point3d> entering = new CircularLinkedList<Point3d>();
//            CircularLinkedList<Point3d> exiting = new CircularLinkedList<Point3d>();

//            sectPts = new List<Point3d>();
//            enterPts = new List<Point3d>();
//            exitPts = new List<Point3d>();

//            if (AreEqual<Point3d>(subject, clip))
//            {
//                switch (operation)
//                {
//                    case BooleanOperation.Union:
//                        polyLoops.Add(subjectLoop);
//                        return polyLoops;
//                        break;
//                    case BooleanOperation.Intersect:
//                        polyLoops.Add(subjectLoop);
//                        return polyLoops;
//                        break;
//                    case BooleanOperation.Difference:
//                        return polyLoops;
//                        break;
//                    default:
//                        break;
//                }
//            }

//            LinkedListNode<Point3d> curSubject = subject.First;
//            while (curSubject != subject.Last)
//            {
//                Util.LogPrint(string.Format("Subject step at ({0}, {1}) - ({2}, {3})", 
//                    curSubject.Value.X, curSubject.Value.Y, curSubject.Next.Value.X, curSubject.Next.Value.Y));
//                LinkedListNode<Point3d> curClip = clip.First;
//                while (curClip != clip.Last)
//                {
//                    Util.LogPrint(string.Format("Clip step at ({0}, {1}) - ({2}, {3})", 
//                        curClip.Value.X, curClip.Value.Y, curClip.Next.Value.X, curClip.Next.Value.Y));
//                    Point3d intersectionPoint;
//                    if (Basic.SegIntersection(curSubject.Value, curSubject.Next.Value, 
//                        curClip.Value, curClip.Next.Value, out intersectionPoint) == segIntersectEnum.IntersectOnBoth)
//                    {
//                        sectPts.Add(intersectionPoint);
//                        Util.LogPrint(string.Format("Intersected at ({0}, {1})", intersectionPoint.X, intersectionPoint.Y));
//                        if (curSubject.Next.Value != intersectionPoint && curSubject.Value != intersectionPoint && 
//                            !subject.Contains(intersectionPoint))
//                        {
//                            subject.AddAfter(curSubject, intersectionPoint);
//                             if you step forward the pointer you will miss out intersections sometime
//                            curSubject = curSubject.Next;
//                        }
//                        if (curClip.Next.Value != intersectionPoint && curClip.Value != intersectionPoint &&
//                            !clip.Contains(intersectionPoint))
//                        {
//                            clip.AddAfter(curClip, intersectionPoint);
//                            curClip = curClip.Next;
//                        }
//                         in case the vertice falls on the edge, in which situation there will be two identical
//                         intersection points.
//                        bool isEntering = IsEntering(curSubject.Value, curSubject.Next.Value,
//                            curClip.Value, curClip.Next.Value);
//                        if (isEntering && !entering.Contains(intersectionPoint))
//                            entering.AddLast(intersectionPoint);
//                        else if (!isEntering && !exiting.Contains(intersectionPoint))
//                            exiting.AddLast(intersectionPoint);
//                    }
//                    curClip = curClip.Next;
//                }
//                curSubject = curSubject.Next;
//            }

//            MakeEnterExitList(subject, clip, intersections, entering, exiting);
//            List<Point3d> duplicates = new List<Point3d>();
//            foreach (Point3d pt in entering)
//            {
//                if (exiting.Contains(pt))
//                    duplicates.Add(pt);
//            }
//            foreach (Point3d pt in duplicates)
//            {
//                entering.Remove(pt);
//                exiting.Remove(pt);
//            }

//            subject.RemoveLast();
//            clip.RemoveLast();

//            foreach (Point3d pt in entering)
//                enterPts.Add(pt);
//            foreach (Point3d pt in exiting)
//                exitPts.Add(pt);

//            Traverse(subject, clip, entering, exiting, polygons, operation);

//            if (polygons.Count == 0)
//            {
//                switch (operation)
//                {
//                    case BooleanOperation.Union:
//                        polygons.Add(subject);
//                        polygons.Add(clip);
//                        break;
//                    case BooleanOperation.Intersect:
//                        break;
//                    case BooleanOperation.Difference:
//                        polygons.Add(subject);
//                        break;
//                    default:
//                        break;
//                }
//            }

//            foreach (CircularLinkedList<Point3d> polygon in polygons)
//            {
//                List<Point3d> loop = new List<Point3d>();
//                foreach (Point3d pt in polygon)
//                    loop.Add(pt);
//                polyLoops.Add(loop);
//            }
//            return polyLoops;
//        }

//        public static List<List<Point3d>> Process(List<Point3d> subjectLoop,
//            List<Point3d> clipLoop, BooleanOperation operation)
//        {
//            List<Point3d> sectPts;
//            List<Point3d> enterPts;
//            List<Point3d> exitPts;
//            return Process(subjectLoop, clipLoop, operation,
//            out sectPts, out enterPts, out exitPts);
//        }


//        static bool AreEqual<T>(LinkedList<T> a, LinkedList<T> b) where T : IEquatable<T>
//        {
//            if (a.Count != b.Count)
//            {
//                return false;
//            }

//            LinkedListNode<T> currA = a.First;
//            LinkedListNode<T> currB = b.First;

//            while (currA != null)
//            {
//                if (!currA.Value.Equals(currB.Value))
//                    return false;

//                currA = currA.Next;
//                currB = currB.Next;
//            }

//            return true;
//        }

//        public static void Swap<T>(ref T left, ref T right) where T : class
//        {
//            T temp;
//            temp = left;
//            left = right;
//            right = temp;
//        }

//        private static void Traverse(CircularLinkedList<Point3d> subject,
//                                        CircularLinkedList<Point3d> clip,
//                                        CircularLinkedList<Point3d> entering,
//                                        CircularLinkedList<Point3d> exiting,
//                                        List<CircularLinkedList<Point3d>> polygons,
//                                        BooleanOperation operation)
//        {
//            if (operation == BooleanOperation.Intersect)
//                Swap<CircularLinkedList<Point3d>>(ref entering, ref exiting);

//            if (operation == BooleanOperation.Difference)
//                clip = clip.Reverse();

//            CircularLinkedList<Point3d> currentList;
//            CircularLinkedList<Point3d> otherList;

//            while (entering.Count > 0)
//            {
//                Util.LogPrint(string.Format("We still have {0} entering points in this iteration", entering.Count));
//                CircularLinkedList<Point3d> polygon = new CircularLinkedList<Point3d>();

//                 reset
//                currentList = subject;
//                otherList = clip;
//                Point3d start = entering.First.Value;
//                int count = 0;
//                LinkedListNode<Point3d> transitionNode = entering.First;
//                bool enteringCheck = true;

//                while (transitionNode != null && (count == 0 || (count > 0 && start != transitionNode.Value)))
//                {
//                    if (count == 0)
//                    {
//                        entering.Remove(transitionNode.Value);
//                        Util.LogPrint(string.Format("Starting a poly, erasing ({0}, {1})", transitionNode.Value.X, transitionNode.Value.Y));
//                    }
//                    transitionNode = TraverseList(currentList, entering, exiting, polygon, transitionNode, start, otherList);

//                    enteringCheck = !enteringCheck;

//                    if (currentList == subject)
//                    {
//                        currentList = clip;
//                        otherList = subject;
//                    }
//                    else
//                    {
//                        currentList = subject;
//                        otherList = clip;
//                    }
//                    count++;
//                }
//                polygons.Add(polygon);
//            }
//        }

//        private static LinkedListNode<Point3d> TraverseList(CircularLinkedList<Point3d> contour,
//                                                            CircularLinkedList<Point3d> entering,
//                                                            CircularLinkedList<Point3d> exiting,
//                                                            CircularLinkedList<Point3d> polygon,
//                                                            LinkedListNode<Point3d> currentNode,
//                                                            Point3d startNode,
//                                                            CircularLinkedList<Point3d> contour2)
//        {
//            LinkedListNode<Point3d> contourNode = contour.Find(currentNode.Value);
//            if (contourNode == null)
//                return null;

//            entering.Remove(currentNode.Value);

//            while ((contourNode != null && !entering.Contains(contourNode.Value) && !exiting.Contains(contourNode.Value)) 
//                || (entering.Contains(contourNode.Value) && exiting.Contains(contourNode.Value)) )
//            {
//                polygon.AddLast(contourNode.Value);
//                contourNode = contour.NextOrFirst(contourNode);

//                if (contourNode.Value == startNode)
//                    return null;
//            }

//            entering.Remove(contourNode.Value);
//            Util.LogPrint(string.Format("erasing ({0}, {1})", contourNode.Value.X, contourNode.Value.Y));
//            polygon.AddLast(contourNode.Value);

//            return contour2.NextOrFirst(contour2.Find(contourNode.Value));
//        }

//        private static bool IsEntering(Point3d startA, Point3d endA, Point3d startB, Point3d endB)
//        {
//            double x1 = endA.X - startA.X;
//            double x2 = endB.X - startB.X;
//            double y1 = endA.Y - startA.Y;
//            double y2 = endB.Y - startB.Y;
//            if ((x1 * y2 - x2 * y1) < 0)
//                return true;
//            return false;
//        }

//    }
//}
