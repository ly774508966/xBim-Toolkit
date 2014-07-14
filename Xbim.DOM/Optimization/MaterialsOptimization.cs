﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.MaterialResource;
using Xbim.IO;

namespace Xbim.DOM.Optimization
{
    public static class MaterialsOptimization
    {
        /// <summary>
        /// This function provides a tool to recognise IfcMaterialList that are composed of the same materials.
        /// </summary>
        /// <param name="Model">The model to extract material lists from</param>
        /// <returns>Dictionary associating the entitylabel (value) of an IfcMaterialList that duplicates the composition of the one identified by the entitylabel (Key).</returns>
        public static Dictionary<uint, uint> IfcMaterialListReplacementDictionary(this XbimModel Model)
        {
            // the resulting dictionary contains information on the replacing ID of any IfcMaterialList that duplicates another of the same composition.
            
            Dictionary<uint, uint> dic = new Dictionary<uint, uint>();
            Dictionary<string, uint> CompositionDic = new Dictionary<string, uint>();
            foreach (var matList in Model.Instances.OfType<IfcMaterialList>())
            {
                List<uint> mlist = new List<uint>();
                foreach (var item in matList.Materials)
                {
                    mlist.Add(item.EntityLabel);
                }
                mlist.Sort();
                string stSignature = string.Join(",", mlist.ToArray());
                if (CompositionDic.ContainsKey(stSignature))
                {
                    dic.Add(matList.EntityLabel, CompositionDic[stSignature]);
                }
                else
                {
                    CompositionDic.Add(stSignature, matList.EntityLabel);
                    dic.Add(matList.EntityLabel, matList.EntityLabel);
                }
            }
            return dic;
        }
    }
}
