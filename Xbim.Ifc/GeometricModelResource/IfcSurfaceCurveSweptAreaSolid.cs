﻿#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcSurfaceCurveSweptAreaSolid.cs
// Published:   01, 2012
// Last Edited: 18:12 PM on 06 06 2012
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using Xbim.XbimExtensions;
using Xbim.Ifc.GeometryResource;
using Xbim.Ifc.MeasureResource;
using Xbim.XbimExtensions.Parser;
using System.Runtime.Serialization;
using System.Xml.Serialization;

#endregion

namespace Xbim.Ifc.GeometricModelResource
{
    [IfcPersistedEntity, Serializable]
    public class IfcSurfaceCurveSweptAreaSolid : IfcSweptAreaSolid
    {
        
        #region Fields

        private IfcCurve _directrix;
        private IfcParameterValue _startParam;
        private IfcParameterValue _endParam;
        private IfcSurface _referenceSurface;

        #endregion

        #region Constructors

        #endregion

        #region Part 21 Step file Parse routines

        /// <summary>
        /// The curve used to define the sweeping operation. The solid is generated by sweeping the SELF\IfcSweptAreaSolid.SweptArea along the Directrix.
        /// </summary>
        [DataMember(Order = 2)]
        [XmlElement(typeof(IfcCurve))]
        [IfcAttribute(3, IfcAttributeState.Mandatory)]
        public IfcCurve Directrix
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity)this).Activate(false);
#endif
                return _directrix;
            }
            set { ModelManager.SetModelValue(this, ref _directrix, value, v => Directrix = v, "Directrix"); }
        }

        /// <summary>
        /// The parameter value on the Directrix at which the sweeping operation commences.
        /// </summary>
        [DataMember(Order = 3)]
        [XmlElement(typeof(IfcParameterValue))]
        [IfcAttribute(4, IfcAttributeState.Mandatory)]
        public IfcParameterValue StartParam
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity)this).Activate(false);
#endif
                return _startParam;
            }
            set { ModelManager.SetModelValue(this, ref _startParam, value, v => StartParam = v, "StartParam"); }
        }

        /// <summary>
        /// The parameter value on the Directrix at which the sweeping operation ends.
        /// </summary>
        [DataMember(Order = 4)]
        [XmlElement(typeof(IfcParameterValue))]
        [IfcAttribute(5, IfcAttributeState.Mandatory)]
        public IfcParameterValue EndParam
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity)this).Activate(false);
#endif
                return _endParam;
            }
            set { ModelManager.SetModelValue(this, ref _endParam, value, v => EndParam = v, "EndParam"); }
        }

        /// <summary>
        /// 	 The parameter value on the Directrix at which the sweeping operation ends.
        /// </summary>
        [DataMember(Order = 5)]
        [XmlElement(typeof(IfcSurface))]
        [IfcAttribute(6, IfcAttributeState.Mandatory)]
        public IfcSurface ReferenceSurface
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity)this).Activate(false);
#endif
                return _referenceSurface;
            }
            set { ModelManager.SetModelValue(this, ref _referenceSurface, value, v => ReferenceSurface = v, "ReferenceSurface"); }
        }
        


        public override void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                case 1:
                    base.IfcParse(propIndex, value);
                    break;

                case 2:
                    _directrix = (IfcCurve)value.EntityVal;
                    break;
                case 3:
                    _startParam = (IfcParameterValue)value.EntityVal;
                    break;
                case 4:
                    _endParam = (IfcParameterValue)value.EntityVal;
                    break;
                case 5:
                    _referenceSurface = (IfcSurface)value.EntityVal;
                    break;
                default:
                    throw new Exception(string.Format("Attribute index {0} is out of range for {1}", propIndex + 1,
                                                      this.GetType().Name.ToUpper()));
            }
        }

        #endregion

        #region Methods

        #endregion

        #region Ifc Schema Validation Methods

        #endregion
        
    }
}