﻿#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcColourRgb.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using Xbim.Ifc.MeasureResource;
using Xbim.Ifc.SelectTypes;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;

#endregion

namespace Xbim.Ifc.PresentationResource
{
    [IfcPersistedEntity, Serializable]
    public class IfcColourRgb : IfcColourSpecification, IfcFillStyleSelect, IfcColourOrFactor
    {
        #region Fields

        private IfcNormalisedRatioMeasure _red = 0;
        private IfcNormalisedRatioMeasure _green = 0;
        private IfcNormalisedRatioMeasure _blue = 0;

        #endregion

        #region Part 21 Step file Parse routines

        [IfcAttribute(2, IfcAttributeState.Mandatory)]
        public IfcNormalisedRatioMeasure Red
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _red;
            }
            set { ModelHelper.SetModelValue(this, ref _red, value, v => Red = v, "Red"); }
        }


        [IfcAttribute(3, IfcAttributeState.Mandatory)]
        public IfcNormalisedRatioMeasure Green
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _green;
            }
            set { ModelHelper.SetModelValue(this, ref _green, value, v => Green = v, "Green"); }
        }


        [IfcAttribute(4, IfcAttributeState.Mandatory)]
        public IfcNormalisedRatioMeasure Blue
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _blue;
            }
            set { ModelHelper.SetModelValue(this, ref _blue, value, v => Blue = v, "Blue"); }
        }


        public override void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                    base.IfcParse(propIndex, value);
                    break;
                case 1:
                    _red = value.RealVal;
                    break;
                case 2:
                    _green = value.RealVal;
                    break;
                case 3:
                    _blue = value.RealVal;
                    break;
                default:
                    this.HandleUnexpectedAttribute(propIndex, value); break;
            }
        }

        #endregion

        public override string ToString()
        {
            return string.Format("{0}, {1}, {2}", Red, Green, Blue);
        }


        public override string WhereRule()
        {
            return "";
        }
    }
}