﻿#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcTopologicalRepresentationItem.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using Xbim.Ifc.GeometryResource;
using Xbim.XbimExtensions;

#endregion

namespace Xbim.Ifc.TopologyResource
{
    /// <summary>
    ///   The topological representation item is the supertype for all the topological representation items in the geometry resource.
    /// </summary>
    [IfcPersistedEntityAttribute, Serializable]
    public abstract class IfcTopologicalRepresentationItem : IfcRepresentationItem
    {
    }
}