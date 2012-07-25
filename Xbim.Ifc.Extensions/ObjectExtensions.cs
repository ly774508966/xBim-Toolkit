﻿#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc.Extensions
// Filename:    ObjectExtensions.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.PropertyResource;
using Xbim.Ifc2x3.QuantityResource;
using Xbim.XbimExtensions.SelectTypes;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;

#endregion

namespace Xbim.Ifc2x3.Extensions
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// Specific type information  that is common to all instances of IfcObject refering to the same type.
        /// </summary>
        /// <param name="tObj"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        public static IfcTypeObject GetDefiningType(this IfcObject tObj, IModel model)
        {
            IfcRelDefinesByType def =  model.InstancesWhere<IfcRelDefinesByType>(rd => rd.RelatedObjects.Contains(tObj)).FirstOrDefault();
            if (def != null)
                return def.RelatingType;
            else
                return null;
        }

        public static IfcTypeObject GetDefiningType(this IfcObject tObj)
        {
            IfcRelDefinesByType def = tObj.ModelOf.InstancesWhere<IfcRelDefinesByType>(rd => rd.RelatedObjects.Contains(tObj)).FirstOrDefault();
            if (def != null)
                return def.RelatingType;
            else
                return null;
           
        }
        //Removes the current object from any RelDefinesByType relationships and adds a relationship to the specified Type 
        public static void SetDefiningType(this IfcObject obj, IfcTypeObject typeObj, IModel model)
        {

            //divorce any exisitng related types
            IEnumerable<IfcRelDefinesByType> rels = model.InstancesWhere<IfcRelDefinesByType>(rd => rd.RelatedObjects.Contains(obj));
            foreach (var rel in rels)
            {
                rel.RelatedObjects.Remove_Reversible(obj);
            }
            //find any existing relationships to this type
            IfcRelDefinesByType typeRel = model.InstancesWhere<IfcRelDefinesByType>(rd => rd.RelatingType==typeObj).FirstOrDefault();
            if (typeRel == null) //none defined create the relationship
            {
                IfcRelDefinesByType relSub = model.New<IfcRelDefinesByType>();
                relSub.RelatingType = typeObj;
                relSub.RelatedObjects.Add_Reversible(obj);
            }
            else //we have the type
            {

                typeRel.RelatedObjects.Add_Reversible(obj);
            }
        }

        /// <summary>
        /// Returns the propertyset of the specified name, null if it does not exist
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="pSetName"></param>
        /// <returns></returns>
        public static IfcPropertySet GetPropertySet(this Xbim.Ifc2x3.Kernel.IfcObject obj, string pSetName)
        {
            IfcRelDefinesByProperties rel= obj.IsDefinedByProperties.Where(r => r.RelatingPropertyDefinition.Name == pSetName).FirstOrDefault();
            if (rel != null) return rel.RelatingPropertyDefinition as IfcPropertySet;
            else return null;
        }
        public static IfcPropertySingleValue GetPropertySingleValue(this Xbim.Ifc2x3.Kernel.IfcObject obj, string pSetName, string propertyName)
        {
            IfcPropertySet pset = GetPropertySet(obj, pSetName);
            if (pset != null)
                return pset.HasProperties.Where<IfcPropertySingleValue>(p => p.Name == propertyName).FirstOrDefault();
            return null;
        }
        public static VType GetPropertySingleValue<VType>(this Xbim.Ifc2x3.Kernel.IfcObject obj, string pSetName, string propertyName) where VType : IfcValue
        {
            IfcPropertySet pset = GetPropertySet(obj, pSetName);
            if (pset != null)
            {
                IfcPropertySingleValue pVal = pset.HasProperties.Where<IfcPropertySingleValue>(p => p.Name == propertyName).FirstOrDefault();
                if (pVal != null && typeof(VType).IsAssignableFrom(pVal.NominalValue.GetType())) return (VType)pVal.NominalValue;
            }
            return default(VType);
        }

        /// <summary>
        /// If the property value exists, returns the Nominal Value of the contents
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="pSetName"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static IfcValue GetPropertySingleNominalValue (this Xbim.Ifc2x3.Kernel.IfcObject obj, string pSetName, string propertyName)
        {
            IfcPropertySingleValue psv = GetPropertySingleValue(obj, pSetName, propertyName);
            return psv == null ? null : psv.NominalValue;
        }

        public static List<IfcPropertySet> GetAllPropertySets(this Xbim.Ifc2x3.Kernel.IfcObject obj)
        {
            List<IfcPropertySet> result = new List<IfcPropertySet>();
            IEnumerable<IfcRelDefinesByProperties> rels = obj.IsDefinedByProperties;
            foreach (IfcRelDefinesByProperties rel in rels)
            {
                IfcPropertySet pSet = rel.RelatingPropertyDefinition as IfcPropertySet;
                if (pSet != null) result.Add(pSet);
            }

            return result;
        }

        public static Dictionary<IfcLabel, Dictionary<IfcIdentifier, IfcValue>> GetAllPropertySingleValues(this Xbim.Ifc2x3.Kernel.IfcObject obj)
        {
            Dictionary<IfcLabel, Dictionary<IfcIdentifier, IfcValue>> result = new Dictionary<IfcLabel, Dictionary<IfcIdentifier, IfcValue>>();
            IEnumerable<IfcRelDefinesByProperties> relations = obj.IsDefinedByProperties.OfType<IfcRelDefinesByProperties>();
            foreach (IfcRelDefinesByProperties rel in relations)
            {
                Dictionary<IfcIdentifier, IfcValue> value = new Dictionary<IfcIdentifier, IfcValue>();
                IfcLabel psetName = rel.RelatingPropertyDefinition.Name??null;
                IfcPropertySet pSet = rel.RelatingPropertyDefinition as IfcPropertySet;
                if (pSet == null) continue;
                foreach (IfcProperty prop in pSet.HasProperties)
                {
                    IfcPropertySingleValue singleVal = prop as IfcPropertySingleValue;
                    if (singleVal == null) continue;
                    value.Add(prop.Name, singleVal.NominalValue);
                }
                if (!result.ContainsKey(psetName))
                    result.Add(psetName, value);
            }
            return result;
        }

        public static void DeletePropertySingleValueValue(this Xbim.Ifc2x3.Kernel.IfcObject obj, string pSetName, string propertyName)
        {
            IfcPropertySingleValue psv = GetPropertySingleValue(obj, pSetName, propertyName);
            if (psv == null) return;
            psv.NominalValue = null;
        }

        public static IfcPropertyTableValue GetPropertyTableValue(this Xbim.Ifc2x3.Kernel.IfcObject obj, string pSetName, string propertyTableName)
        {
            IfcPropertySet pset = GetPropertySet(obj, pSetName);
            if (pset != null)
                return pset.HasProperties.Where<IfcPropertyTableValue>(p => p.Name == propertyTableName).FirstOrDefault();
            return null;
        }

        public static IfcValue GetPropertyTableItemValue(this Xbim.Ifc2x3.Kernel.IfcObject obj, string pSetName, string propertyTableName, IfcValue definingValue)
        {
            IfcPropertyTableValue table = GetPropertyTableValue(obj, pSetName, propertyTableName);
            if (table == null) return null;
            XbimList<IfcValue> definingValues = table.DefiningValues;
            if (definingValues == null) return null;
            if (!definingValues.Contains(definingValue)) return null;
            int index = definingValues.IndexOf(definingValue);

            if (table.DefinedValues.Count < index + 1) return null;
            return table.DefinedValues[index];
        }

        public static void SetPropertyTableItemValue(this Xbim.Ifc2x3.Kernel.IfcObject obj, string pSetName, string propertyTableName, IfcValue definingValue, IfcValue definedValue)
        {
            SetPropertyTableItemValue(obj, pSetName, propertyTableName, definingValue, definedValue, null, null);
        }

        public static void SetPropertyTableItemValue(this Xbim.Ifc2x3.Kernel.IfcObject obj, string pSetName, string propertyTableName, IfcValue definingValue, IfcValue definedValue, IfcUnit definingUnit, IfcUnit definedUnit)
        {
            IfcPropertySet pset = GetPropertySet(obj, pSetName);
            IModel model = null;
            if (pset == null)
            {
                model = obj.ModelOf;
                pset = model.New<IfcPropertySet>();
                pset.Name = pSetName;
                IfcRelDefinesByProperties relDef = model.New<IfcRelDefinesByProperties>();
                relDef.RelatingPropertyDefinition = pset;
                relDef.RelatedObjects.Add_Reversible(obj);
            }
            IfcPropertyTableValue table = GetPropertyTableValue(obj, pSetName, propertyTableName);
            if (table == null)
            {
                model = obj.ModelOf;
                table = model.New<IfcPropertyTableValue>(tb => { tb.Name = propertyTableName; });
                pset.HasProperties.Add_Reversible(table);
                table.DefinedUnit = definedUnit;
                table.DefiningUnit = definingUnit;
            }
            if (table.DefiningUnit != definingUnit || table.DefinedUnit != definedUnit)
                throw new Exception("Inconsistent definition of the units in the property table.");

            IfcValue itemValue = GetPropertyTableItemValue(obj, pSetName, propertyTableName, definingValue);
            if (itemValue != null)
            {
                itemValue = definedValue;
            }
            else
            {
                //if (table.DefiningValues == null) table.DefiningValues = new XbimList<IfcValue>();
                table.DefiningValues.Add_Reversible(definingValue);
                //if (table.DefinedValues == null) table.DefinedValues = new XbimList<IfcValue>();
                table.DefinedValues.Add_Reversible(definedValue);

                //check of integrity
                if (table.DefinedValues.Count != table.DefiningValues.Count)
                    throw new Exception("Inconsistent state of the property table. Number of defined and defining values are not the same.");
            }
        }

        /// <summary>
        /// Creates property single value with specified type and default value of this type (0 for numeric types, empty string tor string types and false for bool types)
        /// </summary>
        /// <param name="pSetName">Property set name</param>
        /// <param name="propertyName">Property name</param>
        /// <param name="type">Type of the property</param>
        /// <returns>Property single value with default value of the specified type</returns>
        public static IfcPropertySingleValue SetPropertySingleValue(this Xbim.Ifc2x3.Kernel.IfcObject obj, string pSetName, string propertyName, Type type)
        {
            if (typeof(IfcValue).IsAssignableFrom(type))
            {
                IfcValue value;
                if (typeof(IfcPositiveLengthMeasure).IsAssignableFrom(type))
                    value = Activator.CreateInstance(type, 1.0) as IfcValue;
                else
                    value = Activator.CreateInstance(type) as IfcValue;

                if (value != null)
                    return SetPropertySingleValue(obj, pSetName, propertyName, value);
                else
                    throw new Exception("Type '" + type.Name + "' can't be initialized.");
            }
            else
                throw new ArgumentException("Type '" + type.Name + "' is not compatible with IfcValue type.");
        }

        public static IfcPropertySingleValue SetPropertySingleValue(this Xbim.Ifc2x3.Kernel.IfcObject obj, string pSetName, string propertyName, IfcValue value)
        {
            IfcPropertySet pset = GetPropertySet(obj, pSetName);
            IModel model = null;
            if (pset == null)    
            {
                model = obj.ModelOf;
                pset = model.New<IfcPropertySet>();
                pset.Name = pSetName;
                IfcRelDefinesByProperties relDef = model.New<IfcRelDefinesByProperties>();
                relDef.RelatingPropertyDefinition = pset;
                relDef.RelatedObjects.Add_Reversible(obj);
            }

            //change existing property of the same name from the property set
            IfcPropertySingleValue singleVal = GetPropertySingleValue(obj, pSetName, propertyName);
            if (singleVal != null)
            {
                singleVal.NominalValue = value;
            }
            else
            {
                model = obj.ModelOf;
                singleVal = model.New<IfcPropertySingleValue>(psv => { psv.Name = propertyName; psv.NominalValue = value; });
                pset.HasProperties.Add_Reversible(singleVal);
            }

            return singleVal;
        }


        /// <summary>
        /// Returns a list of all the elements that bound the external of the building 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        static public IEnumerable<IfcElement> GetExternalElements(IModel model)
        {
            return model.InstancesOfType<IfcRelSpaceBoundary>().Where(r => r.InternalOrExternalBoundary == IfcInternalOrExternalEnum.EXTERNAL
                && r.PhysicalOrVirtualBoundary == IfcPhysicalOrVirtualEnum.PHYSICAL
                && r.RelatedBuildingElement != null).Select(rsb => rsb.RelatedBuildingElement).Distinct();
        }

        public static IfcElementQuantity GetElementQuantity(this IfcObject elem, string pSetName)
        {
            IfcRelDefinesByProperties rel = elem.IsDefinedByProperties.Where(r => r.RelatingPropertyDefinition.Name == pSetName && r.RelatingPropertyDefinition is IfcElementQuantity).FirstOrDefault();
            if (rel != null) return rel.RelatingPropertyDefinition as IfcElementQuantity;
            else return null;
        }
        /// <summary>
        /// Returns the first quantity in the property set pSetName of name qName
        /// </summary>
        /// <typeparam name="QType"></typeparam>
        /// <param name="elem"></param>
        /// <param name="pSetName"></param>
        /// <param name="qName"></param>
        /// <returns></returns>
        public static QType GetQuantity<QType>(this IfcObject elem, string pSetName, string qName) where QType : IfcPhysicalQuantity
        {
            IfcRelDefinesByProperties rel = elem.IsDefinedByProperties.Where(r => r.RelatingPropertyDefinition.Name == pSetName && r.RelatingPropertyDefinition is IfcElementQuantity).FirstOrDefault();
            if (rel != null)
            {
                IfcElementQuantity eQ =  rel.RelatingPropertyDefinition as IfcElementQuantity;
                if (eQ != null)
                {
                    QType result = eQ.Quantities.Where<QType>(q => q.Name == qName).FirstOrDefault();
                    return result;
                }
            }
            return default(QType);
        }

        /// <summary>
        /// Returns the first quantity that matches the quantity name
        /// </summary>
        /// <typeparam name="QType"></typeparam>
        /// <param name="elem"></param>
        /// <param name="qName"></param>
        /// <returns></returns>
        public static QType GetQuantity<QType>(this IfcObject elem, string qName) where QType : IfcPhysicalQuantity
        {
            IfcRelDefinesByProperties rel = elem.IsDefinedByProperties.Where(r => r.RelatingPropertyDefinition is IfcElementQuantity).FirstOrDefault();
            if (rel != null)
            {
                IfcElementQuantity eQ = rel.RelatingPropertyDefinition as IfcElementQuantity;
                if (eQ != null)
                {
                    QType result = eQ.Quantities.Where<QType>(q => q.Name == qName).FirstOrDefault();
                    return result;
                }
            }
            return default(QType);
        }

        /// <summary>
        /// Adds a new IfcPhysicalQuantity to the IfcElementQuantity called propertySetName
        /// </summary>
        /// <param name="elem"></param>
        /// <param name="propertySetName">Name of the IfcElementQuantity property set</param>
        /// <param name="quantity">quantity to be added</param>
        /// <param name="methodOfMeasurement">Sets the method of measurement, if not null overrides previous value</param>
        public static IfcElementQuantity AddQuantity(this IfcObject elem, string propertySetName, IfcPhysicalQuantity quantity, string methodOfMeasurement)
        {
            IfcElementQuantity pset = elem.GetElementQuantity(propertySetName);

            if (pset == null)
            {
                IModel model = elem.ModelOf;
                pset = model.New<IfcElementQuantity>();
                pset.Name = propertySetName;
                IfcRelDefinesByProperties relDef = model.New<IfcRelDefinesByProperties>();
                relDef.RelatingPropertyDefinition = pset;
                relDef.RelatedObjects.Add_Reversible(elem);
            }
            pset.Quantities.Add_Reversible(quantity);
            if (!string.IsNullOrEmpty(methodOfMeasurement)) pset.MethodOfMeasurement = methodOfMeasurement;
            return pset;
        }
        /// <summary>
        /// Adds a new IfcPhysicalQuantity to the IfcElementQuantity called propertySetName
        /// </summary>
        /// <param name="elem"></param>
        /// <param name="propertySetName">Name of the IfcElementQuantity property set</param>
        /// <param name="quantity">quantity to be added</param>
        static public IfcElementQuantity AddQuantity(this IfcObject elem, string propertySetName, IfcPhysicalQuantity quantity)
        {
            return AddQuantity(elem, propertySetName, quantity, null);
        }

        /// <summary>
        /// Returns simple physical quality of the element.
        /// </summary>
        /// <param name="elem"></param>
        /// <param name="pSetName"></param>
        /// <param name="qualityName"></param>
        /// <returns></returns>
        public static IfcPhysicalSimpleQuantity GetElementPhysicalSimpleQuantity(this IfcObject elem, string pSetName, string qualityName)
        {
            IfcElementQuantity elementQuality = GetElementQuantity(elem, pSetName);
            if (elementQuality != null)
            {
                return elementQuality.Quantities.Where<IfcPhysicalSimpleQuantity>(sq => sq.Name == qualityName).FirstOrDefault();
            }
            else
                return null;
        }

        public static void SetElementPhysicalSimpleQuantity(this IfcObject elem, string qSetName, string qualityName, double value, XbimQuantityTypeEnum quantityType,IfcNamedUnit unit)
        {
            IModel model = elem.ModelOf;

            IfcElementQuantity qset = GetElementQuantity(elem, qSetName);
            if (qset == null)
            {
                qset = model.New<IfcElementQuantity>();
                qset.Name = qSetName;
                IfcRelDefinesByProperties relDef = model.New<IfcRelDefinesByProperties>();
                relDef.RelatingPropertyDefinition = qset;
                relDef.RelatedObjects.Add_Reversible(elem);
            }

            //remove existing simple quality
            IfcPhysicalSimpleQuantity simpleQuality = GetElementPhysicalSimpleQuantity(elem, qSetName, qualityName);
            if (simpleQuality != null)
            {
                IfcElementQuantity elementQuality = GetElementQuantity(elem, qSetName);
                elementQuality.Quantities.Remove_Reversible(simpleQuality);
                model.Delete(simpleQuality);
            }

            switch (quantityType)
            {
                case XbimQuantityTypeEnum.AREA:
                    simpleQuality = model.New<IfcQuantityArea>(sq => sq.AreaValue = (IfcAreaMeasure)value);
                    break;
                case XbimQuantityTypeEnum.COUNT:
                    simpleQuality = model.New<IfcQuantityCount>(sq => sq.CountValue = (IfcCountMeasure)value);
                    break;
                case XbimQuantityTypeEnum.LENGTH:
                    simpleQuality = model.New<IfcQuantityLength>(sq => sq.LengthValue = (IfcLengthMeasure)value);
                    break;
                case XbimQuantityTypeEnum.TIME:
                    simpleQuality = model.New<IfcQuantityTime>(sq => sq.TimeValue = (IfcTimeMeasure)value);
                    break;
                case XbimQuantityTypeEnum.VOLUME:
                    simpleQuality = model.New<IfcQuantityVolume>(sq => sq.VolumeValue = (IfcVolumeMeasure)value);
                    break;
                case XbimQuantityTypeEnum.WEIGHT:
                    simpleQuality = model.New<IfcQuantityWeight>(sq => sq.WeightValue = (IfcMassMeasure)value);
                    break;
                default:
                    return;
            }

            simpleQuality.Unit = unit;
            simpleQuality.Name = qualityName;

            qset.Quantities.Add_Reversible(simpleQuality);
        }

        public static void RemovePropertySingleValue(this Xbim.Ifc2x3.Kernel.IfcObject obj, string pSetName, string propertyName)
        {
            IfcPropertySet pset = GetPropertySet(obj, pSetName);
            if (pset != null)
            {
                IfcPropertySingleValue singleValue = pset.HasProperties.Where<IfcPropertySingleValue>(p => p.Name == propertyName).FirstOrDefault();
                if (singleValue != null)
                {
                    pset.HasProperties.Remove_Reversible(singleValue);
                }
            }
               
        }

        public static void RemoveElementPhysicalSimpleQuantity(this IfcObject elem, string pSetName, string qualityName)
        {
            IfcElementQuantity elementQuality = GetElementQuantity(elem, pSetName);
            if (elementQuality != null)
            {
                IfcPhysicalSimpleQuantity simpleQuality = elementQuality.Quantities.Where<IfcPhysicalSimpleQuantity>(sq => sq.Name == qualityName).FirstOrDefault();
                if (simpleQuality != null)
                {
                    elementQuality.Quantities.Remove_Reversible(simpleQuality);
                }
            }
        }
    }

    public enum XbimQuantityTypeEnum
    {
        LENGTH,
        AREA,
        VOLUME,
        COUNT,
        WEIGHT,
        TIME
    }
}

