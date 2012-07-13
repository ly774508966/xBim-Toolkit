﻿#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcSweptAreaSolid.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using Xbim.Ifc.GeometryResource;
using Xbim.Ifc.ProfileResource;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;

#endregion

namespace Xbim.Ifc.GeometricModelResource
{
    [IfcPersistedEntity, Serializable]
    public abstract class IfcSweptAreaSolid : IfcSolidModel
    {
        #region Fields

        private IfcProfileDef _sweptArea;
        private IfcAxis2Placement3D _position;

        #endregion

        #region Constructors

        #endregion

        #region Part 21 Step file Parse routines

        /// <summary>
        ///   The surface defining the area to be swept. It is given as a profile definition within the xy plane of the position coordinate system.
        /// </summary>

        [XmlElement(typeof (IfcRectangleProfileDef))]
        [IfcAttribute(1, IfcAttributeState.Mandatory)]
        public IfcProfileDef SweptArea
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _sweptArea;
            }
            set { ModelManager.SetModelValue(this, ref _sweptArea, value, v => SweptArea = v, "SweptArea"); }
        }

        /// <summary>
        ///   Position coordinate system for the swept area.
        /// </summary>

        [IfcAttribute(2, IfcAttributeState.Mandatory)]
        public IfcAxis2Placement3D Position
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _position;
            }
            set { ModelManager.SetModelValue(this, ref _position, value, v => Position = v, "Position"); }
        }

        public override void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                    _sweptArea = (IfcProfileDef) value.EntityVal;
                    break;
                case 1:
                    _position = (IfcAxis2Placement3D) value.EntityVal;
                    break;
                default:
                    this.HandleUnexpectedAttribute(propIndex, value); break;
            }
        }

        #endregion

        public override string WhereRule()
        {
            if (_sweptArea == null || _sweptArea.ProfileType != IfcProfileTypeEnum.AREA)
                return "WR22 SweptAreaSolid : The profile definition for the swept area solid shall be of type AREA.\n";
            else
                return "";
        }
    }
}