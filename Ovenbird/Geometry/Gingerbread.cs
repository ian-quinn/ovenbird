﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace Ovenbird
{
    // class definition reserved by gingerbread with prefix gb-
    #region Gingerbread class
    /// <summary>
    /// Degenerated version of point exclusive for Gingerbread
    /// </summary>
    public class gbXYZ
    {
        // private info
        private double x;
        private double y;
        private double z;
        private static double eps = 1e-6;
        public double X { get { return x; } set { x = value; } }
        public double Y { get { return y; } set { y = value; } }
        public double Z { get { return z; } set { z = value; } }

        // constructor
        public gbXYZ()
        {
            x = 0; y = 0; z = 0;
        }

        public gbXYZ(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        // public methods
        public void Unitize()
        {
            double norm = Math.Sqrt(x * x + y * y + z * z);
            if (norm > 0)
            {
                x /= norm; y /= norm; z /= norm;
            }
        }
        public double Norm()
        {
            return Math.Sqrt(x * x + y * y + z * z);
        }
        public gbXYZ Copy()
        {
            return new gbXYZ(X, Y, Z);
        }
        public gbXYZ CrossProduct(gbXYZ b)
        {
            return new gbXYZ(
                Y * b.Z - Z * b.Y,
                Z * b.X - X * b.Z,
                X * b.Y - Y * b.X);
        }
        public double DotProduct(gbXYZ b)
        {
            return X * b.X + Y * b.Y + Z * b.Z;
        }
        public double DistanceTo(gbXYZ b)
        {
            return Math.Sqrt(
                (b.X - X) * (b.X - X) +
                (b.Y - Y) * (b.Y - Y) +
                (b.Z - Z) * (b.Z - Z));
        }
        // Define addition operation
        public static gbXYZ operator +(gbXYZ A, gbXYZ B)
        {
            return new gbXYZ(A.X + B.X, A.Y + B.Y, A.Z + B.Z);
        }
        public static gbXYZ operator -(gbXYZ A, gbXYZ B)
        {
            return new gbXYZ(A.X - B.X, A.Y - B.Y, A.Z - B.Z);
        }
        public static gbXYZ operator -(gbXYZ A)
        {
            //double u = A.X == 0 ? A.X : -A.X;
            //double v = A.Y == 0 ? A.Y : -A.Y;
            //double w = A.Z == 0 ? A.Z : -A.Z;
            //return new gbXYZ(u, v, w);
            return new gbXYZ(-A.X, -A.Y, -A.Z);
        }
        public static gbXYZ operator /(gbXYZ P, double a)
        {
            if (a == 0)
                return P;
            else
                return new gbXYZ(P.X / a, P.Y / a, P.Z / a);
            
        }
        public static gbXYZ operator *(double a, gbXYZ P)
        {
            return new gbXYZ(P.X * a, P.Y * a, P.Z * a);
        }
        public static bool operator ==(gbXYZ A, gbXYZ B)
        {
            if (Math.Abs(A.X - B.X) <= eps &&
                Math.Abs(A.Y - B.Y) <= eps &&
                Math.Abs(A.Z - B.Z) <= eps)
                return true;
            return false;
        }
        public static bool operator !=(gbXYZ A, gbXYZ B)
        {
            if (Math.Abs(A.X - B.X) <= eps &&
                Math.Abs(A.Y - B.Y) <= eps &&
                Math.Abs(A.Z - B.Z) <= eps)
                return false;
            return true;
        }
        // Define function to display a point
        public override string ToString()
        {
            return string.Format("({0:F4}, {1:F4}, {2:F4})", X, Y, Z);
        }
    }
    /*
    public class gbUV
    {
        public double X;
        public double Y;
        private static double eps = 1e-6;

        public gbUV()
        {
            X = 0;
            Y = 0;
        }
        public gbUV(double x, double y)
        {
            this.X = x;
            this.Y = y;
        }
        // Define addition operation
        public static gbUV operator +(gbUV A, gbUV B)
        {
            return new gbUV(A.X + B.X, A.Y + B.Y);
        }
        public static gbUV operator -(gbUV A, gbUV B)
        {
            return new gbUV(A.X - B.X, A.Y - B.Y);
        }
        public static bool operator ==(gbUV A, gbUV B)
        {
            if (Math.Abs(A.X - B.X) <= eps && Math.Abs(A.Y - B.Y) <= eps)
            {
                return true;
            }
            return false;
        }
        public static bool operator !=(gbUV A, gbUV B)
        {
            if (Math.Abs(A.X - B.X) <= eps && Math.Abs(A.Y - B.Y) <= eps)
            {
                return false;
            }
            return true;
        }
        // Define function to display a point
        public string serialize()
        {
            return string.Format("({0:F2}, {1:F2})", X, Y);
        }
    }
    */

    public class gbSeg
    {
        private gbXYZ start;
        private gbXYZ end;
        private readonly double length;
        private gbXYZ direction;
        public gbXYZ Start { get { return start; } }
        public gbXYZ End { get { return end; } }
        public double Length { get { return length; } }
        public gbXYZ Direction { get { return direction; } }
        //public gbXYZ Direction()
        //{
        //    return (end - start) / length;
        //}

        public gbSeg(gbXYZ start, gbXYZ end)
        {
            this.start = start;
            this.end = end;
            length = Math.Sqrt(
                (end.X - start.X) * (end.X - start.X) +
                (end.Y - start.Y) * (end.Y - start.Y) +
                (end.Z - start.Z) * (end.Z - start.Z));
            direction = (end - start) / length;
        }

        public gbXYZ PointAt(double ratio)
        {
            return start + ratio * length * direction;
        }
        public void Reverse()
        {
            gbXYZ temp = end;
            end = start;
            start = temp;
            direction = -direction;
        }
        public gbSeg Copy()
        {
            return new gbSeg(start, end);
        }
        public List<gbSeg> Split(List<double> intervals)
        {
            foreach (double interval in intervals)
                if (Math.Round(interval, 6) > 1 || Math.Round(interval, 6) < 0)
                    return new List<gbSeg>() { new gbSeg(start, end) };
            List<gbSeg> segments = new List<gbSeg>();
            intervals.Sort();
            intervals.Insert(0, 0);
            intervals.Add(1);
            for (int i = 0; i < intervals.Count - 1; i++)
                if (intervals[i] != intervals[i + 1])
                    segments.Add(new gbSeg(PointAt(intervals[i]), PointAt(intervals[i + 1])));
            return segments;
        }
        public override string ToString()
        {
            return string.Format("Line: {0} - {1}", Start.ToString(), End.ToString());
        }
    }


    // note these classes aim for Energyplus IDF structures
    // in gbXML there must be no coincident surface, but in IDF there must be two identical 
    // surfaces as the adjacent interior wall
    // for debugging we still use Point3d from Rhino.Geometry
    public class gbLevel
    {
        public int id;
        public int prevId;
        public int nextId;
        public string label;
        public double elevation;
        public double height;
        // in here there permits no gap between spaces and floors
        // so usually the space height equals the level capacity

        public bool isTop = false;
        public bool isBottom = false;
        public gbLevel(int id, string label, double elevation, int numAllLevels)
        {
            this.id = id;
            this.label = label;
            this.elevation = elevation;
            if (id == 0) isBottom = true; else prevId = id - 1;
            if (id == numAllLevels - 1) isTop = true; else nextId = id + 1;
        }
    }

    public class gbFloor
    {
        public string id;
        public gbLevel level;
        public List<gbXYZ> loop;
        // convert the 2D loop to 3D floor geometry
        public gbFloor(string id, gbLevel level, List<gbXYZ> loop)
        {
            this.id = id;
            this.level = level;
            this.loop = Basic.ElevatePtsLoop(loop, level.elevation);
        }
    }

    public class gbSurface
    {
        // initilazation attributes
        public string id;
        public string parentId;
        public gbLevel level;

        public double tilt;
        public double azimuth;  // pending right now
        public double area;
        public double width;
        public double height;

        public List<gbXYZ> loop;
        public gbSeg locationLine; // when the tilt is 90
        public List<gbOpening> openings;

        // modification attributes
        public string adjSrfId;
        public surfaceTypeEnum type;

        public gbSurface(string id, string parentId, List<gbXYZ> loop, double tilt)
        {
            this.id = id;
            this.loop = loop;
            this.parentId = parentId;
            this.tilt = tilt;
            // the azimuth is the angle (0-360) between the normal vector and the north axis (0, 1, 0)
            azimuth = Basic.VectorAngle(Basic.GetPolyNormal(loop), new gbXYZ(0, 1, 0));
            area = Basic.GetPolyArea3d(loop);
            openings = new List<gbOpening>();

            if (tilt == 90)
            {
                locationLine = new gbSeg(loop[0], loop[1]);
                // according to gbXML schema, the width is the length between
                // the left most and right most points of the polygon
                // the height is an equivalent value by area / width
                width = locationLine.Length;
                height = area / width;
            }
            // the GetRectHull function only works for 2D points
            if (tilt == 0 || tilt == 180)
            {
                List<gbXYZ> corners = OrthogonalHull.GetRectHull(loop);
                width = corners[1].X - corners[0].X;
                height = area / width;
            }
        }
    }

    /// <summary>
    /// Only including vertical openings: window/door/curtain wall
    /// </summary>
    public class gbOpening
    {
        public string id;
        public List<gbXYZ> loop;
        public int levelId;
        public double area;
        public double width;
        public double height;
        public openingTypeEnum type;

        public gbOpening(string id, List<gbXYZ> loop)
        {
            this.id = id;
            this.loop = loop;
            area = Basic.GetPolyArea3d(loop);
            width = loop[0].DistanceTo(loop[1]);
            height = area / width;
        }
    }

    public class gbZone
    {
        public string id; // structured relationships Floor_1::Zone_1::Srf_1
        public List<gbXYZ> loop;

        public gbLevel level;

        public double area;
        public double volume;
        public double height;
        public bool isFuzzySeperated = false; // goto isovist division if true
        public bool isMultiConnected = false; // goto multiconnected region separation if true

        // connect to program presets or space label
        public string function;

        // all need to be validated before faces are generated
        public List<gbSurface> faces;
        public int numFaces;
        public List<gbSurface> walls = new List<gbSurface>();
        public List<gbSurface> ceilings = new List<gbSurface>();
        public List<gbSurface> floors = new List<gbSurface>();

        // the input loop of points must be closed
        public gbZone(string id, gbLevel level, List<gbXYZ> loop)
        {
            this.id = id;
            this.loop = Basic.ElevatePtsLoop(loop, level.elevation);

            this.level = level;
            this.height = level.height;

            area = Basic.GetPolyArea(loop);
            volume = area * height;

            walls = new List<gbSurface>();
            for (int i = 0; i < loop.Count - 1; i++)
            {
                List<gbXYZ> subLoop = new List<gbXYZ>
                {
                    this.loop[i],
                    this.loop[i + 1],
                    this.loop[i + 1] + new gbXYZ(0, 0, height),
                    this.loop[i] + new gbXYZ(0, 0, height)
                };
                // only extrude on axis-z
                walls.Add(new gbSurface(id + "::Wall_" + i, id, subLoop, 90));
            }

        }
        /// <summary>
        /// Add up all surfaces of this zone
        /// </summary>
        public void Summarize()
        {
            faces = new List<gbSurface>();
            if (walls.Count != 0)
                faces.AddRange(walls);
            if (ceilings.Count != 0)
                faces.AddRange(ceilings);
            if (floors.Count != 0)
                faces.AddRange(floors);
            numFaces = faces.Count;
        }
    }
    #endregion
}
