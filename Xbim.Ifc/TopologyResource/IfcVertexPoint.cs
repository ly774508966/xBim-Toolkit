﻿#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcVertexPoint.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using Xbim.Ifc.GeometryResource;
using Xbim.Ifc.SelectTypes;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Parser;

#endregion

namespace Xbim.Ifc.TopologyResource
{
    [IfcPersistedEntity, Serializable]
    public class IfcVertexPoint : IfcVertex, IfcPointOrVertexPoint
    {
        private IfcPoint _vertexGeometry;

        /// <summary>
        ///   The geometric point, which defines the position in geometric space of the vertex.
        /// </summary>
        [IfcAttribute(1, IfcAttributeState.Mandatory)]
        public IfcPoint VertexGeometry
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _vertexGeometry;
            }
            set { ModelManager.SetModelValue(this, ref _vertexGeometry, value, v => VertexGeometry = v, "VertexGeometry"); }
        }


        public override void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                    _vertexGeometry = (IfcPoint) value.EntityVal;
                    break;
                default:
                    this.HandleUnexpectedAttribute(propIndex, value); break;
            }
        }
    }
}