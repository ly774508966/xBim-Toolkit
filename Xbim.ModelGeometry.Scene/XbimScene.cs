﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xbim.ModelGeometry.Scene
{
    public class XbimScene<TVISIBLE, TMATERIAL>
        where TVISIBLE : IXbimMeshGeometry3D, new()
        where TMATERIAL : IXbimRenderMaterial, new()
    {
        XbimMeshLayerCollection<TVISIBLE, TMATERIAL> layers = new XbimMeshLayerCollection<TVISIBLE, TMATERIAL>();
        XbimColourMap layerColourMap;

        /// <summary>
        /// The colour map for this scene
        /// </summary>
        public XbimColourMap LayerColourMap
        {
            get { return layerColourMap; }
        }
        /// <summary>
        /// Constructs a scene using the default IfcProductType colour map
        /// </summary>
        public XbimScene()
        {
            layerColourMap = new XbimColourMap();
        }

        /// <summary>
        /// Constructs a scene, using the specfified colourmap
        /// </summary>
        /// <param name="colourMap"></param>
        public XbimScene(XbimColourMap colourMap)
        {
            this.layerColourMap = colourMap;
        }

        public IEnumerable<XbimMeshLayer<TVISIBLE, TMATERIAL>> Layers
        {
            get
            {
                foreach (var layer in layers)
                {
                    yield return layer;
                    foreach (var subLayer in layer.SubLayers)
                    {
                        yield return subLayer;
                    }
                }
                
            }
        }
        /// <summary>
        /// Returns all layers and sublayers that have got some graphic content that is visible
        /// </summary>
        public IEnumerable<XbimMeshLayer<TVISIBLE, TMATERIAL>> VisibleLayers
        {
            get
            {
                foreach (var layer in layers)
                {
                    if (layer.Visible.Meshes.Any()) yield return layer;
                    foreach (var subLayer in layer.SubLayers)
                    {
                        if (subLayer.Visible.Meshes.Any()) yield return subLayer;
                    }
                }

            }
        }
        /// <summary>
        /// Add the layer to the scene
        /// </summary>
        /// <param name="layer"></param>
        public void Add(XbimMeshLayer<TVISIBLE, TMATERIAL> layer)
        {
            if (string.IsNullOrEmpty(layer.Name)) //ensure a layer has a unique name if the user has not defined one
                layer.Name = "Layer " + layers.Count();
            layers.Add(layer);
        }

        /// <summary>
        /// Makes all meshes in all layers in the scene Hidden
        /// </summary>
        public void HideAll()
        {
            foreach (var layer in layers)
                layer.HideAll();
        }

        public void ShowAll()
        {
            foreach (var layer in layers)
                layer.ShowAll();
        }

       
    }
}
