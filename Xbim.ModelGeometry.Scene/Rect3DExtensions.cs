﻿#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.ModelGeometry.Scene
// Filename:    Rect3DExtensions.cs
// Published:   01, 2012
// Last Edited: 10:01 AM on 04 01 2012
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System.IO;
using System.Windows.Media.Media3D;

#endregion

namespace Xbim.ModelGeometry.Scene
{
    public static class Rect3DExtensions
    {
        /// <summary>
        /// Reinitialises the rectangle 3d from the byte array
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="array">6 doubles, definine, min and max values of the boudning box</param>
        public static Rect3D FromArray(this Rect3D rect, byte[] array)
        {
            MemoryStream ms = new MemoryStream(array);
            BinaryReader bw = new BinaryReader(ms);

            double srXmin = bw.ReadDouble();
            double srYmin = bw.ReadDouble();
            double srZmin = bw.ReadDouble();
            double srXmax = bw.ReadDouble();
            double srYmax = bw.ReadDouble();
            double srZmax = bw.ReadDouble();
            rect.Location = new Point3D(srXmin, srYmin, srZmin);
            rect.SizeX = srXmax - srXmin;
            rect.SizeY = srYmax - srYmin;
            rect.SizeZ = srZmax - srZmin;
            return rect;
        }

        static public Rect3D TransformBy(this Rect3D rect, Matrix3D matrix3d)
        {
            MatrixTransform3D m3d = new MatrixTransform3D(matrix3d);
            return m3d.TransformBounds(rect);
            
        }

        public static void Write(this Rect3D rect, BinaryWriter strm)
        {
            if (rect.IsEmpty)
                strm.Write('E');
            else
            {
                strm.Write('R');
                strm.Write(rect.X);
                strm.Write(rect.Y);
                strm.Write(rect.Z);
                strm.Write(rect.SizeX);
                strm.Write(rect.SizeY);
                strm.Write(rect.SizeZ);
            }
        }

        public static Rect3D Read(this Rect3D rect, BinaryReader strm)
        {
            char test = strm.ReadChar();
            if (test == 'E')
                return new Rect3D();
            else
            {
                rect.X = strm.ReadDouble();
                rect.Y = strm.ReadDouble();
                rect.Z = strm.ReadDouble();
                rect.SizeX = strm.ReadDouble();
                rect.SizeY = strm.ReadDouble();
                rect.SizeZ = strm.ReadDouble();
                return rect;
            }
        }
    }
}