﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Common;
using Xbim.Common.Geometry;

namespace Xbim.ModelGeometry.Scene
{
    public class XbimEmptyGeometryGroup : List<IXbimGeometryModel>, IXbimGeometryModelGroup
    {
        static XbimEmptyGeometryGroup _empty;
        public static XbimEmptyGeometryGroup Empty { get { return _empty; } }
        static XbimEmptyGeometryGroup()
        {
            _empty = new XbimEmptyGeometryGroup();
        }
        private  XbimEmptyGeometryGroup() //not intended to be constructed
        {

        }
        public int RepresentationLabel { get { return 0; } set { } }
        public int SurfaceStyleLabel { get { return 0; } set { } }
        public bool IsMap { get { return false; } }

        public XbimRect3D GetBoundingBox() { return XbimRect3D.Empty; }

        public double Volume { get { return 0; } }

        public XbimMatrix3D Transform { get { return XbimMatrix3D.Identity; } }

        public XbimTriangulatedModelCollection Mesh(double deflection) { return new XbimTriangulatedModelCollection(); }

        public String WriteAsString(XbimModelFactors modelFactors) { return "EMPTY"; }

        public XbimRect3D GetAxisAlignedBoundingBox()
        {
            return XbimRect3D.Empty;
        }

       
        public XbimMeshFragment MeshTo(IXbimMeshGeometry3D mesh3D, Ifc2x3.Kernel.IfcProduct product, XbimMatrix3D transform, double deflection, short modelId)
        {
            return default(XbimMeshFragment);
        }


        public XbimPoint3D CentreOfMass
        {
            get { return default(XbimPoint3D); }
        }
    }
}
