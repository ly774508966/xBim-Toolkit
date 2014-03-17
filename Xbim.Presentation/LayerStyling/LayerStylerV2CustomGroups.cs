﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;

namespace Xbim.Presentation.LayerStyling
{
    public class LayerStylerV2CustomGroups : ILayerStylerV2
    {
        public List<LayerStyler2CustomSetAppearence> GroupSpecs = new List<LayerStyler2CustomSetAppearence>();
        public XbimTexture DefaultAppearence;

        public void Init()
        {
            _layers = new ConcurrentDictionary<ModelGeometry.Scene.XbimTexture, ModelGeometry.Scene.XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial>>();
        }

        ConcurrentDictionary<ModelGeometry.Scene.XbimTexture, ModelGeometry.Scene.XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial>> _layers;

        public ConcurrentDictionary<ModelGeometry.Scene.XbimTexture, ModelGeometry.Scene.XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial>> Layers
        {
            get { return _layers; }
        }

        XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial> _CurrentLayer;
        public void NewProduct(ModelGeometry.Converter.XbimProductShape productShape, IO.XbimModel model)
        {
            XbimTexture CurrentTexture = null;
            if (GroupSpecs != null)
            {
                foreach (var GroupSpec in GroupSpecs)
                {
                    if (GroupSpec.Set.Contains(productShape.Product))
                    {
                        CurrentTexture = GroupSpec.Appearence;
                        break;
                    }
                }
            }
            else
            {
                CurrentTexture = DefaultAppearence;
            }

            if (CurrentTexture == null)
            {
                _CurrentLayer = null;
            }
            else
            {
                 _CurrentLayer = _layers.GetOrAdd(CurrentTexture, new XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial>(model, CurrentTexture));    
            }
        }

        public ModelGeometry.Scene.XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial> GetLayer( XbimModel model, ModelGeometry.Converter.XbimShape shape, ModelGeometry.Converter.XbimProductShape productShape)
        {
            return _CurrentLayer;
        }
    }
}
