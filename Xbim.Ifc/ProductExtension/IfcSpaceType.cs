﻿#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcSpaceType.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Parser;

#endregion

namespace Xbim.Ifc.ProductExtension
{
    /// <summary>
    ///   The element type (IfcSpaceType) defines a list of commonly shared property set definitions of a space and an optional set of product representations.
    /// </summary>
    /// <remarks>
    ///   Definition from IAI: The element type (IfcSpaceType) defines a list of commonly shared property set definitions of a space and an optional set of product representations. It is used to define an space specification (i.e. the specific space information, that is common to all occurrences of that space type).
    ///   NOTE  The product representations are defined as representation maps (at the level of the supertype IfcTypeProduct, which gets assigned by an element occurrence instance through the IfcShapeRepresentation.Item[1] being an IfcMappedItem.
    ///   A space type is used to define the common properties of a certain type of space that may be applied to many instances of that type to assign a specific style. Space types may be exchanged without being already assigned to occurrences.
    ///   NOTE  The space types are often used to represent space catalogues, less so for sharing a common representation map. Space types in a space catalogue share same space classification and a common set of space requirement properties.
    ///   The occurrences of IfcSpaceType are represented by instances of IfcSpace.
    ///   HISTORY  New entity in Release IFC2x Edition 3.
    /// </remarks>
    [IfcPersistedEntity, Serializable]
    public class IfcSpaceType : IfcSpatialStructureElementType
    {
        #region Fields

        private IfcSpaceTypeEnum _predefinedType;

        #endregion

        #region Part 21 Step file Parse routines

        [IfcAttribute(10, IfcAttributeState.Mandatory, IfcAttributeType.Enum)]
        public IfcSpaceTypeEnum PredefinedType
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _predefinedType;
            }
            set { ModelManager.SetModelValue(this, ref _predefinedType, value, v => PredefinedType = v, "PredefinedType"); }
        }

        public override void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                case 1:
                case 2:
                case 3:
                case 4:
                case 5:
                case 6:
                case 7:
                case 8:
                    base.IfcParse(propIndex, value);
                    break;
                case 9:
                    _predefinedType = (IfcSpaceTypeEnum) Enum.Parse(typeof (IfcSpaceTypeEnum), value.EnumVal, true);
                    break;
                default:
                    this.HandleUnexpectedAttribute(propIndex, value); break;
            }
        }

        #endregion
    }
}