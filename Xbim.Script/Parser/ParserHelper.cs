﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using QUT.Xbim.Gppg;
using Xbim.IO;
using Xbim.XbimExtensions.Interfaces;
using Xbim.Ifc2x3.Kernel;
using System.Linq.Expressions;
using System.Reflection;
using Xbim.XbimExtensions.SelectTypes;
using Xbim.Ifc2x3.MaterialResource;
using Xbim.Ifc2x3.Extensions;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.Ifc2x3.ProductExtension;
using System.IO;
using Xbim.Ifc2x3.PropertyResource;
using Xbim.Ifc2x3.MaterialPropertyResource;

namespace Xbim.Script
{
    internal partial class Parser
    {
        private XbimModel _model;
        private XbimVariables _variables;
        private ParameterExpression _input = Expression.Parameter(typeof(IPersistIfcEntity), "Input");

        //public properties of the parser
        public XbimVariables Variables { get { return _variables; } }
        public XbimModel Model { get { return _model; } }

        internal Parser(Scanner lex, XbimModel model): base(lex)
        {
            _model = model;
            _variables = new XbimVariables();
            if (_model == null) throw new ArgumentNullException("Model is NULL");
        }

        #region Objects creation
        private IPersistIfcEntity CreateObject(Type type, string name, string description = null)
        {
            if (_model == null) throw new ArgumentNullException("Model is NULL");
            if (name == null)
            {
                Scanner.yyerror("Name must be defined for creation of the " + type.Name + ".");
            } 

            Func<IPersistIfcEntity> create = () => {
                var result = _model.Instances.New(type);

                //set name and description
                if (result == null) return null;
                IfcRoot root = result as IfcRoot;
                if (root != null)
                {
                    root.Name = name;
                    root.Description = description;
                }
                IfcMaterial material = result as IfcMaterial;
                if (material != null)
                {
                    material.Name = name;
                }

                return result;
            };

            IPersistIfcEntity entity = null;
            if (_model.IsTransacting)
            {
                entity = create();
            }
            else
            {
                using (var txn = _model.BeginTransaction("Object creation"))
                {
                    entity = create();
                    txn.Commit();
                }
            }
            return entity;
        }

        #endregion

        #region Attribute and property conditions
        private Expression GenerateAttributeCondition(string attribute, object value, Tokens condition)
        {
            var attrNameExpr = Expression.Constant(attribute);
            var valExpr = Expression.Constant(value, typeof(object));
            var condExpr = Expression.Constant(condition);
            var scannExpr = Expression.Constant(Scanner);

            var evaluateMethod = GetType().GetMethod("EvaluateAttributeCondition", BindingFlags.Static | BindingFlags.NonPublic);

            return Expression.Call(null, evaluateMethod, _input, attrNameExpr, valExpr, condExpr, scannExpr);
        }

        private Expression GeneratePropertyCondition(string property, object value, Tokens condition)
        {
            var propNameExpr = Expression.Constant(property);
            var valExpr = Expression.Constant(value, typeof(object));
            var condExpr = Expression.Constant(condition);
            var scannExpr = Expression.Constant(Scanner);

            var evaluateMethod = GetType().GetMethod("EvaluatePropertyCondition", BindingFlags.Static | BindingFlags.NonPublic);

            return Expression.Call(null, evaluateMethod, _input, propNameExpr, valExpr, condExpr, scannExpr);
        }

        private static bool EvaluatePropertyCondition(IPersistIfcEntity input, string propertyName, object value, Tokens condition, AbstractScanner<ValueType, LexLocation> scanner)
        {
            var prop = GetProperty(propertyName, input);
            //try to get attribute if any exist with this name
            if (prop == null)
            {
                var attr = GetAttributeValue(propertyName, input);
                prop = attr as IfcValue;
            }
            return EvaluateValueCondition(prop, value, condition, scanner);
        }

        private static bool EvaluateAttributeCondition(IPersistIfcEntity input, string attribute, object value, Tokens condition, AbstractScanner<ValueType, LexLocation> scanner)
        {
            var attr = GetAttributeValue(attribute, input);
            return EvaluateValueCondition(attr, value, condition, scanner);
        }
        #endregion

        #region Property and attribute conditions helpers
        private static bool EvaluateNullCondition(object expected, object actual, Tokens condition)
        {
            if (expected != null && actual != null)
                throw new ArgumentException("One of the values is expected to be null.");
            switch (condition)
            {
                case Tokens.OP_EQ:
                    return expected == null && actual == null;
                case Tokens.OP_NEQ:
                    if (expected == null && actual != null) return true;
                    if (expected != null && actual == null) return true;
                    return false;
                default:
                    return false;
            }
        }

        private static bool EvaluateValueCondition(object ifcVal, object val, Tokens condition, AbstractScanner<ValueType, LexLocation> scanner)
        {
            //special handling for null value comparison
            if (val == null || ifcVal == null)
            {
                try
                {
                    return EvaluateNullCondition(ifcVal, val, condition);
                }
                catch (Exception e)
                {
                    scanner.yyerror(e.Message);
                    return false;
                }
            }

            //try to get values to the same level; none of the values can be null for this operation
            object left = null;
            object right = null;
            try
            {
                left = UnWrapType(ifcVal);
                right = PromoteType(GetNonNullableType(left.GetType()), val);
            }
            catch (Exception)
            {

                scanner.yyerror(val.ToString() + " is not compatible type with type of " + ifcVal.GetType());
                return false;
            }
            

            //create expression
            bool? result = null;
            switch (condition)
            {
                case Tokens.OP_EQ:
                    return left.Equals(right);
                case Tokens.OP_NEQ:
                    return !left.Equals(right);
                case Tokens.OP_GT:
                    result = GreaterThan(left, right);
                    if (result != null) return result ?? false;
                    break;
                case Tokens.OP_LT:
                    result = LessThan(left, right);
                    if (result != null) return result ?? false;
                    break;
                case Tokens.OP_GTE:
                    result = !LessThan(left, right);
                    if (result != null) return result ?? false;
                    break;
                case Tokens.OP_LTQ:
                    result = !GreaterThan(left, right);
                    if (result != null) return result ?? false;
                    break;
                case Tokens.OP_CONTAINS:
                    return Contains(left, right);
                case Tokens.OP_NOT_CONTAINS:
                    return !Contains(left, right);
                default:
                    throw new ArgumentOutOfRangeException("Unexpected token used as a condition");
            }
            scanner.yyerror("Can't compare " + left + " and " + right + ".");
            return false;
        }

        private static object UnWrapType(object value)
        { 
            //enumeration
            if (value.GetType().IsEnum)
                return Enum.GetName(value.GetType(), value);

            //express type
            ExpressType express = value as ExpressType;
            if (express != null)
                return express.Value;

            return value;
        }

        private static bool IsNullableType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        private static Type GetNonNullableType(Type type)
        {
            return IsNullableType(type) ? type.GetGenericArguments()[0] : type;
        }

        private static bool IsOfType(Type type, IPersistIfcEntity entity)
        {
            return type.IsAssignableFrom(entity.GetType());
        }

        private static PropertyInfo GetAttributeInfo(string name, IPersistIfcEntity entity)
        {
            Type type = entity.GetType();
            return type.GetProperty(name, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
        }

        private static object GetAttributeValue(string name, IPersistIfcEntity entity)
        {
            PropertyInfo pInfo = GetAttributeInfo(name, entity);
            if (pInfo == null)
                return null;
            return pInfo.GetValue(entity, null);
        }

        private static IfcValue GetProperty(string name, IPersistIfcEntity entity)
        {
            IfcObject obj = entity as IfcObject;
            Dictionary<IfcLabel, Dictionary<IfcIdentifier, IfcValue>> pSets = null;
            if (obj != null)
            {
                pSets = obj.GetAllPropertySingleValues();
            }
            IfcTypeObject typeObj = entity as IfcTypeObject;
            if (typeObj != null)
            {
                pSets = typeObj.GetAllPropertySingleValues();
            }
            IfcMaterial material = entity as IfcMaterial;
            if (material != null)
            {
                pSets = material.GetAllPropertySingleValues();
            }

            if (pSets != null)
                foreach (var pSet in pSets)
                {
                    foreach (var prop in pSet.Value)
                    {
                        if (prop.Key.ToString().ToLower() == name.ToLower()) return prop.Value;
                    }
                }
            return null;
        }

        private static object PromoteType(Type targetType, object value)
        {

            if (targetType == typeof(Boolean)) return Convert.ToBoolean(value);
            if (targetType == typeof(Byte)) return Convert.ToByte(value);
            if (targetType == typeof(DateTime)) return Convert.ToDateTime(value);
            if (targetType == typeof(Decimal)) return Convert.ToDecimal(value);
            if (targetType == typeof(Double)) return Convert.ToDouble(value);
            if (targetType == typeof(float)) return Convert.ToDouble(value);
            if (targetType == typeof(Char)) return Convert.ToChar(value);
            if (targetType == typeof(Int16)) return Convert.ToInt16(value);
            if (targetType == typeof(Int32)) return Convert.ToInt32(value);
            if (targetType == typeof(Int64)) return Convert.ToInt64(value);
            if (targetType == typeof(SByte)) return Convert.ToSByte(value);
            if (targetType == typeof(Single)) return Convert.ToSingle(value);
            if (targetType == typeof(String)) return Convert.ToString(value);
            if (targetType == typeof(UInt16)) return Convert.ToUInt16(value);
            if (targetType == typeof(UInt32)) return Convert.ToUInt32(value);
            if (targetType == typeof(UInt64)) return Convert.ToUInt64(value);

            throw new Exception("Unexpected type");
        }

        private static bool? GreaterThan(object left, object right) 
        {
            try
            {
                var leftD = Convert.ToDouble(left);
                var rightD = Convert.ToDouble(right);
                return leftD > rightD;
            }
            catch (Exception)
            {
                return null;   
            }
           
        }

        private static bool? LessThan(object left, object right)
        {
            try
            {
                var leftD = Convert.ToDouble(left);
                var rightD = Convert.ToDouble(right);
                return leftD < rightD;
            }
            catch (Exception)
            {
                return null;
            }

        }

        private static bool Contains(object left, object right)
        {
            string leftS = Convert.ToString(left);
            string rightS = Convert.ToString(right);

            return leftS.ToLower().Contains(rightS.ToLower());
        }
        #endregion

        #region Select statements
        private IEnumerable<IPersistIfcEntity> Select(Type type, string name)
        {
            if (!typeof(IfcRoot).IsAssignableFrom(type)) return new IPersistIfcEntity[]{};
            Expression expression = GenerateAttributeCondition("Name", name, Tokens.OP_EQ);
            return Select(type, expression);
        }

        private IEnumerable<IPersistIfcEntity> Select(Type type, Expression condition = null)
        {
            //create type expression
            var evaluateMethod = GetType().GetMethod("IsOfType", BindingFlags.Static | BindingFlags.NonPublic);
            Expression typeExpr = Expression.Call(null, evaluateMethod, Expression.Constant(type), _input);

            //create body expression
            Expression exprBody = typeExpr;
            if (condition != null)
                exprBody = Expression.AndAlso(typeExpr, condition);

            return _model.Instances.Where(Expression.Lambda<Func<IPersistIfcEntity, bool>>(exprBody, _input).Compile());
        }
        #endregion

        #region TypeObject conditions 
        private Expression GenerateTypeObjectNameCondition(string typeName, Tokens condition)
        {
            var typeNameExpr = Expression.Constant(typeName);
            var condExpr = Expression.Constant(condition);
            var scanExpr = Expression.Constant(Scanner);

            var evaluateMethod = GetType().GetMethod("EvaluateTypeObjectName", BindingFlags.Static | BindingFlags.NonPublic);
            return Expression.Call(null, evaluateMethod, _input, typeNameExpr, condExpr, scanExpr);
        }

        private Expression GenerateTypeObjectTypeCondition(Type type, Tokens condition)
        {
            var typeExpr = Expression.Constant(type, typeof(Type));
            var condExpr = Expression.Constant(condition);
            var scanExpr = Expression.Constant(Scanner);

            var evaluateMethod = GetType().GetMethod("EvaluateTypeObjectType", BindingFlags.Static | BindingFlags.NonPublic);
            return Expression.Call(null, evaluateMethod, _input, typeExpr, condExpr, scanExpr);
        }

        private static bool EvaluateTypeObjectName(IPersistIfcEntity input, string typeName, Tokens condition, AbstractScanner<ValueType, LexLocation> scanner)
        {
            IfcObject obj = input as IfcObject;
            if (obj == null) return false;

            var type = obj.GetDefiningType();
           
            //null variant
            if (type == null)
            {
                return false;
            }

            switch (condition)
            {
                case Tokens.OP_EQ:
                    return type.Name == typeName;
                case Tokens.OP_NEQ:
                    return type.Name != typeName;
                case Tokens.OP_CONTAINS:
                    return type.Name.ToString().ToLower().Contains(typeName.ToLower());
                case Tokens.OP_NOT_CONTAINS:
                    return !type.Name.ToString().ToLower().Contains(typeName.ToLower());
                default:
                    scanner.yyerror("Unexpected Token in this function. Only equality or containment expected.");
                    return false;
            }
        }

        private static bool EvaluateTypeObjectType(IPersistIfcEntity input, Type type, Tokens condition, AbstractScanner<ValueType, LexLocation> scanner)
        {
            IfcObject obj = input as IfcObject;
            if (obj == null) return false;

            var typeObj = obj.GetDefiningType();
            
            //null variant
            if (typeObj == null || type == null)
            {
                try
                {
                    return EvaluateNullCondition(typeObj, type, condition);
                }
                catch (Exception e)
                {
                    scanner.yyerror(e.Message);
                    return false;
                }
            }

            switch (condition)
            {
                case Tokens.OP_EQ:
                    return typeObj.GetType() == type;
                case Tokens.OP_NEQ:
                    return typeObj.GetType() != type;
                default:
                    scanner.yyerror("Unexpected Token in this function. Only OP_EQ or OP_NEQ expected.");
                    return false;
            }
        }

        private Expression GenerateTypeCondition(Expression expression) 
        {
            var function = Expression.Lambda<Func<IPersistIfcEntity, bool>>(expression, _input).Compile();
            var fceExpr = Expression.Constant(function);

            var evaluateMethod = GetType().GetMethod("EvaluateTypeCondition", BindingFlags.Static | BindingFlags.NonPublic);

            return Expression.Call(null, evaluateMethod, _input, fceExpr);
        }

        private static bool EvaluateTypeCondition(IPersistIfcEntity input, Func<IPersistIfcEntity, bool> function)
        {
            var obj = input as IfcObject;
            if (obj == null) return false;

            var defObj = obj.GetDefiningType();
            if (defObj == null) return false;

            return function(defObj);
        }

        #endregion

        #region Material conditions
        private Expression GenerateMaterialCondition(string materialName, Tokens condition)
        {
            Expression nameExpr = Expression.Constant(materialName);
            Expression condExpr = Expression.Constant(condition);

            var evaluateMethod = GetType().GetMethod("EvaluateMaterialCondition", BindingFlags.Static | BindingFlags.NonPublic);
            return Expression.Call(null, evaluateMethod, _input, nameExpr, condExpr);
        }

        private static bool EvaluateMaterialCondition(IPersistIfcEntity input, string materialName, Tokens condition) 
        {
            IfcRoot root = input as IfcRoot;
            if (root == null) return false;
            IModel model = root.ModelOf;

            var materialRelations = model.Instances.Where<IfcRelAssociatesMaterial>(r => r.RelatedObjects.Contains(root));
            List<string> names = new List<string>();
            foreach (var mRel in materialRelations)
            {
                names.AddRange(GetMaterialNames(mRel.RelatingMaterial));    
            }

            //convert to lower case
            for (int i = 0; i < names.Count; i++)
                names[i] = names[i].ToLower();

            switch (condition)
            {

                case Tokens.OP_EQ:
                    return names.Contains(materialName.ToLower());
                case Tokens.OP_NEQ:
                    return !names.Contains(materialName.ToLower());
                case Tokens.OP_CONTAINS:
                    foreach (var name in names)
                    {
                        if (name.Contains(materialName.ToLower())) return true;
                    }
                    break;
                case Tokens.OP_NOT_CONTAINS:
                    foreach (var name in names)
                    {
                        if (name.Contains(materialName.ToLower())) return false;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Unexpected Token value.");
            }
            return false;
        }

        /// <summary>
        /// Get names of all materials involved
        /// </summary>
        /// <param name="materialSelect">Possible types of material</param>
        /// <returns>List of names</returns>
        private static List<string> GetMaterialNames(IfcMaterialSelect materialSelect)
        {
            List<string> names = new List<string>();
            
            IfcMaterial material = materialSelect as IfcMaterial;
            if (material != null) names.Add( material.Name);

            IfcMaterialList materialList = materialSelect as IfcMaterialList;
            if (materialList != null)
                foreach (var m in materialList.Materials)
                {
                    names.Add(m.Name);
                }
            
            IfcMaterialLayerSetUsage materialUsage = materialSelect as IfcMaterialLayerSetUsage;
            if (materialUsage != null)
                names.AddRange(GetMaterialNames(materialUsage.ForLayerSet));
            
            IfcMaterialLayerSet materialLayerSet = materialSelect as IfcMaterialLayerSet;
            if (materialLayerSet != null)
                foreach (var m in materialLayerSet.MaterialLayers)
                {
                    names.AddRange(GetMaterialNames(m));
                }
            
            IfcMaterialLayer materialLayer = materialSelect as IfcMaterialLayer;
            if (materialLayer != null)
                if (materialLayer.Material != null)
                    names.Add(materialLayer.Material.Name);

            return names;
        }
        #endregion

        #region Variables manipulation
        private void AddOrRemoveFromSelection(string variableName, Tokens operation, object entities)
        {
            IEnumerable<IPersistIfcEntity> ent = entities as IEnumerable<IPersistIfcEntity>;
            if (ent == null) throw new ArgumentException("Entities should be IEnumerable<IPersistIfcEntity>");
            switch (operation)
            {
                case Tokens.OP_EQ:
                    _variables.AddEntities(variableName, ent);
                    break;
                case Tokens.OP_NEQ:
                    _variables.RemoveEntities(variableName, ent);
                    break;
                default:
                    throw new ArgumentException("Unexpected token. OP_EQ or OP_NEQ expected only.");
            }
        }

        private void DumpIdentifier(string identifier, string outputPath = null)
        {
            TextWriter output = null;
            if (outputPath != null)
            {
                output = new StreamWriter(outputPath, false);
            }

            StringBuilder str = new StringBuilder();
            if (Variables.IsDefined(identifier))
            {
                foreach (var entity in Variables[identifier])
                {
                    if (entity != null)
                    {
                        var name = GetAttributeValue("Name", entity);
                        str.AppendLine(String.Format("{1} #{0}: {2}", entity.EntityLabel, entity.GetType().Name, name != null ? name.ToString() : "No name defined"));
                    }
                    else
                        throw new Exception("Null entity in the dictionary");
                }
            }
            else
                str.AppendLine(String.Format("Variable {0} is not defined.", identifier));

            if (output != null)
                output.Write(str.ToString());
            else
                Console.Write(str.ToString());

            if (output != null) output.Close();
        }

        private void DumpAttributes(string identifier,IEnumerable<string> attrNames, string outputPath = null)
        {

            TextWriter output = null;
            try
            {
                if (outputPath != null)
                {
                    output = new StreamWriter(outputPath, false);
                }

                StringBuilder str = new StringBuilder();
                if (Variables.IsDefined(identifier))
                {
                    var header = "";
                    foreach (var name in attrNames)
                    {
                        header += name + "; ";
                    }
                    str.AppendLine(header);

                    foreach (var entity in Variables[identifier])
                    {
                        var line = "";
                        foreach (var name in attrNames)
                        {
                            //get attribute
                            var attr = GetAttributeValue(name, entity);
                            if (attr == null)
                                attr = GetProperty(name, entity);
                            if (attr != null)
                                line += attr.ToString() + "; ";
                            else
                                line += " - ; ";
                        }
                        str.AppendLine(line);
                    }
                }
                else
                    str.AppendLine(String.Format("Variable {0} is not defined.", identifier));

                if (output != null)
                    output.Write(str.ToString());
                else
                    Console.Write(str.ToString());

            }
            catch (Exception e)
            {
                Scanner.yyerror("It was not possible to dump specified content of the " + identifier + ": " + e.Message);
            }
            finally
            {
                //make sure output will not stay opened
                if (output != null) output.Close();
            }
            
        }

        private void ClearIdentifier(string identifier)
        {
            if (Variables.IsDefined(identifier))
            {
                Variables.Clear(identifier);
            }
        }
        #endregion

        #region Add or remove elements to and from group or type or spatial element
        private void AddOrRemove(Tokens action, string productsIdentifier, string aggregation)
        { 
        //conditions
            if (!Variables.IsDefined(productsIdentifier))
            {
                Scanner.yyerror("Variable '" + productsIdentifier + "' is not defined and doesn't contain any products.");
                return;
            }
            if (!Variables.IsDefined(aggregation))
            {
                Scanner.yyerror("Variable '" + aggregation + "' is not defined and doesn't contain any products.");
                return;
            }
            if (Variables[aggregation].Count() != 1)
            {
                Scanner.yyerror("Exactly one group, system, type or spatial element should be in '" + aggregation + "'.");
                return;
            }


            IfcGroup group = Variables[aggregation].FirstOrDefault() as IfcGroup;
            IfcTypeObject typeObject = Variables[aggregation].FirstOrDefault() as IfcTypeObject;
            IfcSpatialStructureElement spatialStructure = Variables[aggregation].FirstOrDefault() as IfcSpatialStructureElement;


            if (group == null && typeObject == null && spatialStructure == null)
            {
                Scanner.yyerror("Only 'group', 'system', 'spatial element' or 'type object' should be in '" + aggregation + "'.");
                return;
            }
            
            //Action which will be performed
            Action perform = null;

            if (group != null)
            {
                var objects = Variables[productsIdentifier].OfType<IfcObjectDefinition>().Cast<IfcObjectDefinition>();
                if (objects.Count() != Variables[productsIdentifier].Count())
                    Scanner.yyerror("Only objects which are subtypes of 'IfcObjectDefinition' can be assigned to group '" + aggregation + "'.");

                perform = () =>
                {
                    foreach (var obj in objects)
                    {

                        switch (action)
                        {
                            case Tokens.ADD:
                                group.AddObjectToGroup(obj);
                                break;
                            case Tokens.REMOVE:
                                group.RemoveObjectFromGroup(obj);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException("Unexpected action. Only ADD or REMOVE can be used in this context.");
                        }
                    }
                };
            }

            if (typeObject != null)
            {
                var objects = Variables[productsIdentifier].OfType<IfcObject>().Cast<IfcObject>();
                if (objects.Count() != Variables[productsIdentifier].Count())
                    Scanner.yyerror("Only objects which are subtypes of 'IfcObject' can be assigned to 'IfcTypeObject' '" + aggregation + "'.");

                perform = () => {
                    foreach (var obj in objects)
                    {
                        switch (action)
                        {
                            case Tokens.ADD:
                                obj.SetDefiningType(typeObject, _model);
                                break;
                            case Tokens.REMOVE:
                                IfcRelDefinesByType rel = _model.Instances.Where<IfcRelDefinesByType>(r => r.RelatingType == typeObject && r.RelatedObjects.Contains(obj)).FirstOrDefault();
                                if (rel != null) rel.RelatedObjects.Remove_Reversible(obj);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException("Unexpected action. Only ADD or REMOVE can be used in this context.");
                        }
                    }
                };
            }

            if (spatialStructure != null)
            {
                var objects = Variables[productsIdentifier].OfType<IfcProduct>().Cast<IfcProduct>();
                if (objects.Count() != Variables[productsIdentifier].Count())
                    Scanner.yyerror("Only objects which are subtypes of 'IfcProduct' can be assigned to 'IfcSpatialStructureElement' '" + aggregation + "'.");

                perform = () =>
                {
                    foreach (var obj in objects)
                    {
                        switch (action)
                        {
                            case Tokens.ADD:
                                spatialStructure.AddElement(obj);
                                break;
                            case Tokens.REMOVE:
                                IfcRelContainedInSpatialStructure rel = _model.Instances.Where<IfcRelContainedInSpatialStructure>(r => r.RelatingStructure == spatialStructure && r.RelatedElements.Contains(obj)).FirstOrDefault();
                                if (rel != null) rel.RelatedElements.Remove_Reversible(obj);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException("Unexpected action. Only ADD or REMOVE can be used in this context.");
                        }
                    }
                };
            }

            if (perform == null) return;

            //perform action
            if (!_model.IsTransacting)
            {
                using (var txn = _model.BeginTransaction("Group manipulation"))
                {
                    perform();
                    txn.Commit();
                }
            }
            else
                perform();
        }


        private void AddOrRemoveToType(Tokens action, IEnumerable<IPersistIfcEntity> objects, IfcTypeObject type)
        {
            foreach (var obj in objects)
            {
                switch (action)
                {
                    case Tokens.ADD:
                        (obj as IfcObject).SetDefiningType(type, _model);
                        break;
                    case Tokens.REMOVE:
                        IfcRelDefinesByType rel = _model.Instances.Where<IfcRelDefinesByType>(r => r.RelatingType == type&& r.RelatedObjects.Contains(obj as IfcObject)).FirstOrDefault();
                        if (rel != null) rel.RelatedObjects.Remove_Reversible(obj as IfcObject);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("Unexpected action. Only ADD or REMOVE can be used in this context.");
                }
            }
        }
        #endregion

        #region Model manipulation
        public void OpenModel(string path)
        {
            try
            {
                if (Path.GetExtension(path).ToLower() != ".xbim")
                    _model.CreateFrom(path, null, null, true);
                else
                    _model.Open(path, XbimExtensions.XbimDBAccess.ReadWrite);
            }
            catch (Exception e)
            {
                Scanner.yyerror("File '"+path+"' can't be used as an input file. Model was not opened: " + e.Message);
            }
        }

        public void CloseModel()
        {
            try
            {
                _model.Close();
                _variables.Clear();
                _model = XbimModel.CreateTemporaryModel();
            }
            catch (Exception e)
            {

                Scanner.yyerror("Model could not have been closed: " + e.Message);
            }
            
        }

        public void SaveModel(string path)
        {
            try
            {
                _model.SaveAs(path);
            }
            catch (Exception e)
            {
                Scanner.yyerror("Model was not saved: " + e.Message);   
            }
        }
        #endregion

        #region Objects manipulation
        private void EvaluateSetExpression(string identifier, IEnumerable<Expression> expressions)
        {
            if (identifier == null || expressions == null) return;

            if (_model.IsTransacting)
                PerformEvaluateSetExpression(identifier, expressions);
            else
                using (var txn = _model.BeginTransaction("Setting properties and attribues"))
                {
                    PerformEvaluateSetExpression(identifier, expressions);
                    txn.Commit();
                }
        }

        private void PerformEvaluateSetExpression(string identifier, IEnumerable<Expression> expressions)
        {
            var entities = _variables.GetEntities(identifier);
            if (entities == null) return;
            foreach (var expression in expressions)
            {
                try
                {
                    var action = Expression.Lambda<Action<IPersistIfcEntity>>(expression, _input).Compile();
                    entities.ToList().ForEach(action);
                }
                catch (Exception e)
                {
                    Scanner.yyerror(e.Message);
                }
            }
        }

        private Expression GenerateSetExpression(string attrName, object newVal)
        {
            var nameExpr = Expression.Constant(attrName);
            var valExpr = Expression.Convert(Expression.Constant(newVal), typeof(object));

            var evaluateMethod = GetType().GetMethod("SetAttribute", BindingFlags.Static | BindingFlags.NonPublic);
            return Expression.Call(null, evaluateMethod, _input, nameExpr, valExpr);
        }

        private static void SetAttribute(IPersistIfcEntity input, string attrName, object newVal)
        {
            if (input == null) return;

            var attr = input.GetType().GetProperty(attrName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (attr == null)
            {
                SetProperty(input, attrName, newVal);
                return;
            }
            SetValue(attr, input, newVal);
        }

        private static void SetProperty(IPersistIfcEntity entity, string name, object newVal)
        {
            List<IfcPropertySet> pSets = null;
            List<IfcExtendedMaterialProperties> pSetsMaterial = null;
            IfcPropertySingleValue property = null;
            PropertyInfo info = null;

            IfcObject obj = entity as IfcObject;
            if (obj != null)
            {
                pSets = obj.GetAllPropertySets();
            }
            IfcTypeObject typeObj = entity as IfcTypeObject;
            if (typeObj != null)
            {
                pSets = typeObj.GetAllPropertySets();
            }
            IfcMaterial material = entity as IfcMaterial;
            if (material != null)
            {
                pSetsMaterial = material.GetAllPropertySets();
            }

            if (pSets != null)
                foreach (var pSet in pSets)
                {
                    foreach (var prop in pSet.HasProperties)
                    {
                        if (prop.Name.ToString().ToLower() == name.ToLower()) property = prop as IfcPropertySingleValue;
                    }
                }
            if (pSetsMaterial != null)
                foreach (var pSet in pSetsMaterial)
                {
                    foreach (var prop in pSet.ExtendedProperties)
                    {
                        if (prop.Name.ToString().ToLower() == name.ToLower()) property = prop as IfcPropertySingleValue;
                    }
                }

            //set property
            if (property != null)
            {
                info = property.GetType().GetProperty("NominalValue");
                SetValue(info, property, newVal);
            }

            //create new property if no such a property exists
            else
            {
                string pSetName = "xbim_extended_properties";
                IfcValue val = null;
                if (newVal != null)
                    val = CreateIfcValueFromBasicValue(newVal, name);
                
                if (obj != null)
                {
                    obj.SetPropertySingleValue(pSetName, name, val);
                }
                if (typeObj != null)
                {
                    typeObj.SetPropertySingleValue(pSetName, name, val);
                }
                if (material != null)
                {
                    material.SetExtendedSingleValue(pSetName, name, val);
                }
            }
        }

        private static void SetValue(PropertyInfo info, object instance, object value)
        {
            try
            {
                if (value != null)
                {
                    var targetType = info.PropertyType.IsNullableType()
                        ? Nullable.GetUnderlyingType(info.PropertyType)
                        : info.PropertyType;

                    object newValue = null;
                    if (!targetType.IsInterface && !targetType.IsAbstract && !targetType.IsEnum)
                        newValue = Activator.CreateInstance(targetType, value);
                    else if (targetType.IsEnum)
                    {
                        //this can throw exception if the name is not correct
                        newValue = Enum.Parse(targetType, value.ToString(), true);
                    }
                    else
                        newValue = CreateIfcValueFromBasicValue(value, info.Name);
                    //this will throw exception if the types are not compatible
                    info.SetValue(instance, newValue, null);
                }
                else
                    //this can throw error if the property can't be null (like structure)
                    info.SetValue(instance, null, null);
            }
            catch (Exception e)
            {
                throw new Exception("Value "+ (value != null ? value.ToString() : "NULL") +" could not be set to "+ info.Name+" of type"+ instance.GetType().Name + ". Type should be compatible with " + info.MemberType);
            }
            
        }

        private static IfcValue CreateIfcValueFromBasicValue(object value, string propName)
        {
            Type type = value.GetType();
            if (type == typeof(int))
                return new IfcInteger((int)value);
            if (type == typeof(string))
                return new IfcLabel((string)value);
            if (type == typeof(double))
                return new IfcNumericMeasure((double)value);
            if (type == typeof(bool))
                return new IfcBoolean((bool)value);

            throw new Exception("Unexpected type of the new value " + type.Name + " for property " + propName);
        }

        #endregion

        #region Thickness conditions
        private Expression GenerateThicknessCondition(double thickness, Tokens condition)
        {
            var thickExpr = Expression.Constant(thickness);
            var condExpr = Expression.Constant(condition);

            var evaluateMethod = GetType().GetMethod("EvaluateThicknessCondition", BindingFlags.Static | BindingFlags.NonPublic);
            return Expression.Call(null, evaluateMethod, _input, thickExpr, condExpr);
        }

        private static bool EvaluateThicknessCondition(IPersistIfcEntity input, double thickness, Tokens condition)
        {
            IfcRoot root = input as IfcRoot;
            if (input == null) return false;

            double? value = null;
            var materSel = root.GetMaterial();
            IfcMaterialLayerSetUsage usage = materSel as IfcMaterialLayerSetUsage;
            if (usage != null)
                if (usage.ForLayerSet != null) 
                    value = usage.ForLayerSet.MaterialLayers.Aggregate(0.0, (current, layer) => current + layer.LayerThickness);
            IfcMaterialLayerSet set = materSel as IfcMaterialLayerSet;
            if (set != null)
                value = set.TotalThickness;
            if (value == null)
                return false;
            switch (condition)
            {
                case Tokens.OP_EQ:
                    return thickness.AlmostEquals(value ?? 0);
                case Tokens.OP_NEQ:
                    return !thickness.AlmostEquals(value ?? 0);
                case Tokens.OP_GT:
                    return value > thickness;
                case Tokens.OP_LT:
                    return value < thickness;
                case Tokens.OP_GTE:
                    return value >= thickness;
                case Tokens.OP_LTQ:
                    return value <= thickness;
                default:
                    throw new ArgumentException("Unexpected value of the condition");
            }
        }
        #endregion

        #region Creation of classification systems
        private void CreateClassification(string name)
        {
            SystemsCreator creator = new SystemsCreator();

            if (name.ToLower() == "uniclass")
            {
                creator.CreateSystem(_model, SYSTEM.UNICLASS);
            }
            if (name.ToLower() == "nrm")
            {
                creator.CreateSystem(_model, SYSTEM.NRM);
            }
        }
        #endregion

        #region Group conditions
        private Expression GenerateGroupCondition(Expression expression) 
        {
            var function = Expression.Lambda<Func<IPersistIfcEntity, bool>>(expression, _input).Compile();
            var fceExpr = Expression.Constant(function);

            var evaluateMethod = GetType().GetMethod("EvaluateGroupCondition", BindingFlags.Static | BindingFlags.NonPublic);

            return Expression.Call(null, evaluateMethod, _input, fceExpr);
        }

        private static bool EvaluateGroupCondition(IPersistIfcEntity input, Func<IPersistIfcEntity, bool> function)
        {
            foreach (var item in GetGroups(input))
            {
                if (function(item)) return true;
            }
            return false;
        }

        private static IEnumerable<IfcGroup> GetGroups(IPersistIfcEntity input)
        {
            IModel model = input.ModelOf;
            var obj = input as IfcObjectDefinition;
            if (obj != null)
            {
                var rels = model.Instances.Where<IfcRelAssignsToGroup>(r => r.RelatedObjects.Contains(input));
                foreach (var rel in rels)
                {
                    yield return rel.RelatingGroup;

                    //recursive search for upper groups in the hierarchy
                    foreach (var gr in GetGroups(rel.RelatingGroup))
                    {
                        yield return gr;
                    }
                }
            }
        }
        #endregion
    }

    public static class TypeExtensions
    {
        public static bool IsNullableType(this Type type)
        {
            return type.IsGenericType
            && type.GetGenericTypeDefinition().Equals(typeof(Nullable<>));
        }

        public static bool AlmostEquals(this double number, double value)
        {
            return Math.Abs(number - value) < 0.000000001;
        }
    }
}