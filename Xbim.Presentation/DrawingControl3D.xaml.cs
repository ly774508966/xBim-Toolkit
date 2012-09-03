﻿#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Presentation
// Filename:    DrawingControl3D.xaml.cs
// Published:   01, 2012
// Last Edited: 9:05 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Xbim.Ifc2x3.Extensions;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.ModelGeometry;
using Xbim.ModelGeometry.Scene;
using Xbim.XbimExtensions;
using Xbim.Ifc2x3.SharedComponentElements;
using Xbim.XbimExtensions.Interfaces;
using Xbim.IO;

#endregion

namespace Xbim.Presentation
{
    public class DrawingControl3DItem
    {
        public ModelVisual3D ModelVisual { get; set; }
        public long ProductLabel { get; set; }
        
       

        public DrawingControl3DItem(long product, ModelVisual3D modelVisual)
        {
            ProductLabel = product;
            ModelVisual = modelVisual;
            
        }
    }

    public delegate XbimMaterialProvider SetMaterialEventHandler(IfcProduct product);

    public delegate Func<IfcProduct, bool> SetFilterEventHandler(int pass);

    /// <summary>
    ///   Interaction logic for DrawingControl3D.xaml
    /// </summary>
    public partial class DrawingControl3D : UserControl
    {
        public DrawingControl3D()
        {
            InitializeComponent();
           
            _defaultMaterial = new XbimMaterialProvider(new DiffuseMaterial(Brushes.WhiteSmoke));
            _selectedVisualMaterial = new DiffuseMaterial(Brushes.LightGreen);
            SolidColorBrush transparentBrush = new SolidColorBrush(Colors.Red);
            transparentBrush.Opacity = 0.5;
            _defaultTransparentMaterial = new XbimMaterialProvider(new DiffuseMaterial(transparentBrush));

            //this.DataContextChanged += new DependencyPropertyChangedEventHandler(DrawingControl3D_DataContextChanged);
           
        }

        void DrawingControl3D_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            
        }

        #region Statics

        private static Dictionary<Type, int> _zOrders;

        static DrawingControl3D()
        {
            _zOrders = new Dictionary<Type, int>();
            _zOrders.Add(typeof (IfcSpace), 50);
            _zOrders.Add(typeof (IfcWall), 200);
            _zOrders.Add(typeof (IfcWallStandardCase), 200);
            _zOrders.Add(typeof (IfcWindow), 180);
            _zOrders.Add(typeof (IfcDoor), 170);
            _zOrders.Add(typeof (IfcSlab), 100);
        }

        /// <summary>
        ///   Returns the Z order for the specified product type
        /// </summary>
        /// <param name = "product"></param>
        /// <returns></returns>
        public static int ZOrder(IfcProduct product, bool external)
        {
            int z;
            if (_zOrders.TryGetValue(product.GetType(), out z))
                return external ? z + 1000 : z;
            else
                return 10;
        }

        #endregion

        #region Fields

        private BackgroundWorker _worker;
        private Dictionary<long, DrawingControl3DItem> _items = new Dictionary<long, DrawingControl3DItem>();
        private Dictionary<ModelVisual3D, ModelVisual3D> _hidden = new Dictionary<ModelVisual3D, ModelVisual3D>();
        private List<Type> _hiddenTypes = new List<Type>();
        private Rect3D _viewSize;
        private XbimMaterialProvider _defaultMaterial;
        private XbimMaterialProvider _defaultTransparentMaterial;



        protected ModelVisual3D _selectedVisual;
        protected Material _selectedVisualMaterial;
        protected Material _selectedVisualPreviousMaterial;

   
        private event ProgressChangedEventHandler _progressChanged;

        public event ProgressChangedEventHandler ProgressChanged
        {
            add { _progressChanged += value; }
            remove { _progressChanged -= value; }
        }

        private event SetMaterialEventHandler _onSetMaterial;

        public event SetMaterialEventHandler OnSetMaterial
        {
            add { _onSetMaterial += value; }
            remove { _onSetMaterial -= value; }
        }

        private event SetFilterEventHandler _onSetFilter;

        public event SetFilterEventHandler OnSetFilter
        {
            add { _onSetFilter += value; }
            remove { _onSetFilter -= value; }
        }

      
       

        #endregion

        #region Events

        public static readonly RoutedEvent SelectionChangedEvent =
            EventManager.RegisterRoutedEvent("SelectionChangedEvent", RoutingStrategy.Bubble,
                                             typeof (SelectionChangedEventHandler), typeof (DrawingControl3D));

        public event SelectionChangedEventHandler SelectionChanged
        {
            add { AddHandler(SelectionChangedEvent, value); }
            remove { RemoveHandler(SelectionChangedEvent, value); }
        }

        public new static readonly RoutedEvent LoadedEvent =
            EventManager.RegisterRoutedEvent("LoadedEvent", RoutingStrategy.Bubble, typeof (RoutedEventHandler),
                                             typeof (DrawingControl3D));

        public new event RoutedEventHandler Loaded
        {
            add { AddHandler(LoadedEvent, value); }
            remove { RemoveHandler(LoadedEvent, value); }
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            long prod = Canvas.GetProductAt(e);
            IList remove = new long[] {};
            IList add = new long[] {prod};
            SelectionChangedEventArgs selEv = new SelectionChangedEventArgs(SelectionChangedEvent, remove, add);
            RaiseEvent(selEv);
        }

        #endregion

        #region Dependency Properties





        public XbimModel Model
        {
            get { return (XbimModel)GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Model.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register("Model", typeof(XbimModel), typeof(DrawingControl3D), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits,
                                                                      new PropertyChangedCallback(OnModelChanged)));

        private static void OnModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DrawingControl3D d3d = d as DrawingControl3D;
            if (d3d != null)
            {
                XbimModel model = e.NewValue as XbimModel;
                d3d.LoadGeometry(model);
            }

        }
        

        public IfcProduct SelectedItem
        {
            get { return (IfcProduct) GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedItem.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof (IfcProduct), typeof (DrawingControl3D),
                                        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits,
                                                                      new PropertyChangedCallback(OnSelectedItemChanged)));

        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DrawingControl3D d3d = d as DrawingControl3D;
            if (d3d != null)
            {
                long? oldProd = e.OldValue as long?;
                long? newProd = e.NewValue as long?;
                if (oldProd.HasValue) //unhighlight last one
                {
                    DrawingControl3DItem item;
                    if (d3d._items.TryGetValue(oldProd.Value, out item))
                    {
                        Material oldMat = null;
                        d3d._selectedVisual = item.ModelVisual;
                        d3d.SwapMaterial(item.ModelVisual.Content, d3d._selectedVisualPreviousMaterial, ref oldMat);
                    }
                }
                if (newProd.HasValue) //highlight new one
                {
                    DrawingControl3DItem item;
                    if (d3d._items.TryGetValue(newProd.Value, out item))
                    {
                        d3d._selectedVisual = item.ModelVisual;
                        d3d.SwapMaterial(item.ModelVisual.Content, d3d._selectedVisualMaterial,
                                         ref d3d._selectedVisualPreviousMaterial);
                    }
                }
            }
        }


        private void SwapMaterial(Model3D m3d, Material newMat, ref Material oldMat)
        {
            if (m3d is Model3DGroup)
            {
                foreach (var item in ((Model3DGroup) m3d).Children)
                {
                    SwapMaterial(item, newMat, ref oldMat);
                }
            }
            else if (m3d is GeometryModel3D)
            {
                GeometryModel3D g3d = (GeometryModel3D) m3d;
                oldMat = g3d.Material;
                g3d.Material = newMat;
                g3d.BackMaterial = newMat;
            }
        }


        //public double Transparency
        //{
        //    get { return (double) GetValue(TransparencyProperty); }
        //    set { SetValue(TransparencyProperty, value); }
        //}

        //// Using a DependencyProperty as the backing store for Transparency.  This enables animation, styling, binding, etc...
        //public static readonly DependencyProperty TransparencyProperty =
        //    DependencyProperty.Register("Transparency", typeof (double), typeof (DrawingControl3D),
        //                                new UIPropertyMetadata(1.0, new PropertyChangedCallback(OnTransparencyChanged)));


        /// <summary>
        ///   Typical length of a Step when moving in the model, default = 0.7 metre
        /// </summary>
        public double Stride
        {
            get { return (double) GetValue(StrideProperty); }
            set
            {
                SetValue(StrideProperty, value);
                TrackBall.StepFactor = Stride;
            }
        }

        // Using a DependencyProperty as the backing store for Stride.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StrideProperty =
            DependencyProperty.Register("Stride", typeof (double), typeof (DrawingControl3D),
                                        new UIPropertyMetadata(0.7));


    

        #endregion

        public IDictionary<long, DrawingControl3DItem> Items
        {
            get { return _items; }
        }

       
        public double PercentageLoaded
        {
            get { return (double) GetValue(PercentageLoadedProperty); }
            set { SetValue(PercentageLoadedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PercentageLoaded.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PercentageLoadedProperty =
            DependencyProperty.Register("PercentageLoaded", typeof (double), typeof (DrawingControl3D),
                                        new UIPropertyMetadata(0.0));



        private void ClearGraphics()
        {
            PercentageLoaded = 0;
            Transparents.Children.Clear();
            Solids.Children.Clear();
            _hidden.Clear();
            _hiddenTypes.Clear();
            //BuildingModel.Content = null;
            _items.Clear();
            TrackBall.CameraMoved = false;
            _viewSize = new Rect3D();
        }

        private MeshGeometry3D MakeBoundingBox(Rect3D r3D)
        {
            MeshGeometry3D mesh = new MeshGeometry3D();
            Point3D p0 = r3D.Location;
            Point3D p1 = p0;
            p1.X += r3D.SizeX;
            Point3D p2 = p1;
            p2.Z += r3D.SizeZ;
            Point3D p3 = p2;
            p3.X -= r3D.SizeX;
            Point3D p4 = p3;
            p4.Y += r3D.SizeY;
            Point3D p5 = p4;
            p5.Z -= r3D.SizeZ;
            Point3D p6 = p5;
            p6.X += r3D.SizeX;
            Point3D p7 = p6;
            p7.Z += r3D.SizeZ;

            List<Point3D> points = new List<Point3D>();
            points.Add(p0);
            points.Add(p1);
            points.Add(p2);
            points.Add(p3);
            points.Add(p4);
            points.Add(p5);
            points.Add(p6);
            points.Add(p7);

            AddVertex(3, mesh, points);
            AddVertex(0, mesh, points);
            AddVertex(2, mesh, points);

            AddVertex(0, mesh, points);
            AddVertex(1, mesh, points);
            AddVertex(2, mesh, points);

            AddVertex(4, mesh, points);
            AddVertex(5, mesh, points);
            AddVertex(3, mesh, points);

            AddVertex(5, mesh, points);
            AddVertex(0, mesh, points);
            AddVertex(3, mesh, points);

            AddVertex(7, mesh, points);
            AddVertex(6, mesh, points);
            AddVertex(4, mesh, points);

            AddVertex(6, mesh, points);
            AddVertex(5, mesh, points);
            AddVertex(4, mesh, points);

            AddVertex(2, mesh, points);
            AddVertex(1, mesh, points);
            AddVertex(7, mesh, points);

            AddVertex(1, mesh, points);
            AddVertex(6, mesh, points);
            AddVertex(7, mesh, points);

            AddVertex(4, mesh, points);
            AddVertex(3, mesh, points);
            AddVertex(7, mesh, points);

            AddVertex(3, mesh, points);
            AddVertex(2, mesh, points);
            AddVertex(7, mesh, points);

            AddVertex(6, mesh, points);
            AddVertex(1, mesh, points);
            AddVertex(5, mesh, points);

            AddVertex(1, mesh, points);
            AddVertex(0, mesh, points);
            AddVertex(5, mesh, points);

            return mesh;
        }

        private void AddVertex(int index, MeshGeometry3D mesh, List<Point3D> points)
        {
            mesh.TriangleIndices.Add(mesh.Positions.Count);
            mesh.Positions.Add(points[index]);
        }

        private void LoadGeometry(XbimModel model)
        {

            //reset all the visuals
            ClearGraphics();
           
            _worker = new BackgroundWorker();
            _worker.DoWork += new DoWorkEventHandler(GenerateGeometry);

            _worker.WorkerReportsProgress = true;
            _worker.WorkerSupportsCancellation = false;
            _worker.ProgressChanged += new ProgressChangedEventHandler(GenerateGeometry_ProgressChanged);
            _worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(GenerateGeometry_RunWorkerCompleted);
            _worker.RunWorkerAsync(model);

           
        }


        private void GenerateGeometry_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _worker = null;

            RoutedEventArgs ev = new RoutedEventArgs(LoadedEvent);
            RaiseEvent(ev);
            InitialiseView(e.Result as XbimModel);
        }

        private void GenerateGeometry_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            XbimGeometryData shapeToDraw = e.UserState as XbimGeometryData;
            if (shapeToDraw != null)
            {
                DrawShape(shapeToDraw);
            }
            ProgressChangedEventHandler handler = _progressChanged;
            if (handler != null)
            {
                handler(this, e);
            }

        }

        private void DrawShape( XbimGeometryData geom)
        {
            
            XbimMaterialProvider mat = null;
            DrawingControl3DItem d3D;

            if (!_items.TryGetValue(geom.IfcProductLabel, out d3D))
            {
                _items.Add(geom.IfcProductLabel, d3D = new DrawingControl3DItem(geom.IfcProductLabel, new ModelVisual3D()));
                Solids.Children.Add(d3D.ModelVisual);
                Matrix3D matrix3d = new Matrix3D().FromArray(geom.TransformData);
                d3D.ModelVisual.Transform = new MatrixTransform3D(matrix3d);
            }
           
            ModelVisual3D mv = d3D.ModelVisual;

            IfcType ifcType = IfcInstances.IfcIdIfcTypeLookup[geom.IfcTypeId];
            bool transparent = (ifcType.Type == typeof(IfcWindow));
            //if (_onSetMaterial != null)
            //    mat = _onSetMaterial(ifcType);
            if (mat == null) //set it just in case
            {
                if (transparent)
                    mat = _defaultTransparentMaterial;
                else
                    mat = _defaultMaterial;
            }

            mv.SetValue(TagProperty, geom.IfcProductLabel);

            XbimTriangulatedModelStream tMod = new XbimTriangulatedModelStream(geom.ShapeData);
            Binding bf = new Binding("FaceMaterial");
            bf.Source = mat;
            Binding bb = new Binding("BackgroundMaterial");
            bb.Source = mat;

            Model3D m3d = tMod.AsModel3D();
            Model3DGroup grp = m3d as Model3DGroup;
            if (grp != null)
            {
                foreach (var item in grp.Children)
                {
                    BindingOperations.SetBinding(item, GeometryModel3D.MaterialProperty, bf); // mat;
                    BindingOperations.SetBinding(item, GeometryModel3D.BackMaterialProperty, bb); // mat;
                }
            }
            if(mv.Content==null)
                mv.Content = m3d;
            else
            {
                ModelVisual3D child = new ModelVisual3D();
                child.Content = m3d;
                mv.Children.Add(child);
            }
           
        }

        //private void DrawNode(TransformNode node)
        //{
        //    XbimMaterialProvider mat = null;
        //    ModelVisual3D mv = new ModelVisual3D();
        //    Matrix3D matrix3d = node.WorldMatrix();
        //    mv.Transform = new MatrixTransform3D(matrix3d);
        //    IfcProduct prodId = node.NearestProduct;
        //    bool transparent = node.IsWindow;
        //    if (_onSetMaterial != null)
        //        mat = _onSetMaterial(prodId);
        //    if (mat == null) //set it just in case
        //    {
        //        if (transparent)
        //            mat = _defaultTransparentMaterial;
        //        else
        //            mat = _defaultMaterial;
        //    }

        //    _items.Add(prodId, new DrawingControl3DItem(prodId, mv, matrix3d));

           
        //    mv.SetValue(TagProperty, prodId);

        //    XbimTriangulatedModelStream tMod = node.TriangulatedModel;
        //    Binding bf = new Binding("FaceMaterial");
        //    bf.Source = mat;
        //    Binding bb = new Binding("BackgroundMaterial");
        //    bb.Source = mat;

        //    if ( !tMod.IsEmpty)
        //    {
        //        Model3D m3d = tMod.AsModel3D();
        //        Model3DGroup grp = m3d as Model3DGroup;
        //        if (grp != null)
        //        {
        //            foreach (var item in grp.Children)
        //            {
        //                  BindingOperations.SetBinding(item, GeometryModel3D.MaterialProperty, bf); // mat;
        //                  BindingOperations.SetBinding(item, GeometryModel3D.BackMaterialProperty, bb); // mat;
        //            }
        //        }
        //        mv.Content = m3d;
        //        if (transparent)
        //            Transparents.Children.Add(mv);
        //        else
        //            Solids.Children.Add(mv);
        //    }
        //    //else //we have children as well
        //    //{
        //    //    Model3DGroup grp = new Model3DGroup();
        //    //    if (!tMod.IsEmpty) // we have a mesh at the parent level
        //    //    {
        //    //        Model3D m3d = tMod.AsModel3D();
                   
        //    //        BindingOperations.SetBinding(m3d, GeometryModel3D.MaterialProperty, bf); // mat;
        //    //        BindingOperations.SetBinding(m3d, GeometryModel3D.BackMaterialProperty, bb); // mat;
        //    //        grp.Children.Add(m3d);
        //    //    }
        //    //    //now do the children
        //    //    foreach (XbimTriangulatedModelStream cMod in tMod.Children)
        //    //    {

        //    //        Model3D m3d = cMod.AsModel3D();
                   
        //    //        BindingOperations.SetBinding(m3d, GeometryModel3D.MaterialProperty, bf); // mat;
        //    //        BindingOperations.SetBinding(m3d, GeometryModel3D.BackMaterialProperty, bb); // mat;
        //    //        grp.Children.Add(m3d);
        //    //    }
        //    //    mv.Content = grp;
        //    //    if (transparent)
        //    //        Transparents.Children.Add(mv);
        //    //    else
        //    //        Solids.Children.Add(mv);
        //    //}

        //    node.TriangulatedModel = null; //drop triangulation data
        //    node.Visible = true;
        //    TransformGraph tg = node.TransformGraph;
        //    if (tg.ModelTransform.IsIdentity)
        //        InitialiseView(tg);
        //    else
        //    {
        //        BuildingModel.Transform = new MatrixTransform3D(tg.ModelTransform);
        //        PerspectiveCamera pCam = Canvas.Camera as PerspectiveCamera;
        //        if (pCam != null)
        //        {
        //            //TODO: put this back in
        //            //pCam.LookDirection = tg.PerspectiveCameraLookDirection;
        //            //pCam.Position = tg.PerspectiveCameraPosition;
        //            //pCam.UpDirection = tg.PerspectiveCameraUpDirection;
        //        }
        //    }
        //}


        private void GenerateGeometry(object s, DoWorkEventArgs args)
        {
            BackgroundWorker worker = s as BackgroundWorker;
            XbimModel model = args.Argument as XbimModel;
            double processed = 0;
            int _percentageParsed = 0;
            if (worker != null && model != null)
            {
                worker.ReportProgress(0, "Reading Geometry");
                foreach (var shape in model.Shapes(XbimGeometryType.TriangulatedMesh))
                {
                    worker.ReportProgress(0, shape);
                }
            }
            worker.ReportProgress(101, "Complete");
            args.Result = model;
        }


        private void InitialiseView(XbimModel model)
        {
            Rect3D b = VisualTreeHelper.GetDescendantBounds(BuildingModel);

            //if the view size id empty draw at elast once, if it has been drawn and the camera hasn't moved and the a resize is needed resize
            if (_viewSize.IsEmpty || (!b.IsEmpty && !_viewSize.Contains(b) && !TrackBall.CameraMoved))
            {
               
                IfcProject proj = null;
               
                IfcBuilding building = null;
                IfcSite site = null;
                double scaleFactor = 1;
                //srl use the X and Z component as these relate to the screen mapping after the rotation
                double len = Math.Max(b.SizeX, b.SizeZ)*1.2;
                //make this view 20% bigger than the widest part of the model
                //calculate how far to move the canvas to be centred on 0,0
                double xOffset = b.X + (b.SizeX/2);
                double yOffset = b.Y + (b.SizeY/2);

                double zOffset = b.Z + (b.SizeZ/2);


                double refHeight = 0;
                double terrainHeight = -0.150;
               
                if (model != null)
                {
                    proj = model.IfcProject as IfcProject;
                    if (proj != null && proj.UnitsInContext!=null) scaleFactor = proj.UnitsInContext.LengthUnitPower();
                    building = model.InstancesOfType<IfcBuilding>().FirstOrDefault();
                    site = model.InstancesOfType<IfcSite>().FirstOrDefault();
                    if (building != null)
                    {
                        if (building.ElevationOfRefHeight.HasValue)
                        {
                            refHeight = building.ElevationOfRefHeight.Value;
                            terrainHeight = refHeight - 0.150;
                        }

                        if (building.ElevationOfTerrain.HasValue) terrainHeight = building.ElevationOfTerrain.Value;
                    }
                    if (site != null)
                    {
                        if (site.RefElevation.HasValue) terrainHeight = site.RefElevation.Value;
                    }
                    //we have no site terrain so draw in default
                    //if (site == null || site.Representation == null)
                    //{
                    //    double groundDepth = .05; //assume we are by default in metres
                    //    groundDepth *= scaleFactor;
                    //    Transform3DGroup tg3d = new Transform3DGroup();
                    //    tg3d.Children.Add(new ScaleTransform3D(len * scaleFactor, len * scaleFactor, groundDepth));
                    //    tg3d.Children.Add(new TranslateTransform3D(0, 0, terrainHeight * scaleFactor));
                    //    Ground.Transform = tg3d;
                    //}
                }
                
                _viewSize = b;
                Transform3DGroup t3d = new Transform3DGroup();
                t3d.Children.Add(new TranslateTransform3D(-xOffset, -yOffset, -zOffset));
                t3d.Children.Add(new ScaleTransform3D(scaleFactor, scaleFactor, scaleFactor));
                t3d.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), -90)));
                BuildingModel.Transform = t3d;

              //  tg.ModelTransform = BuildingModel.Transform.Value;
                double aspect = Canvas.ActualWidth/Canvas.ActualHeight;
               
                PerspectiveCamera pCam = Canvas.Camera as PerspectiveCamera;

                if (pCam != null)
                {
                   
                    double dist = len*(aspect/(2*Math.Tan(pCam.FieldOfView*Math.PI/360)));
                    pCam.Position = new Point3D(-dist * scaleFactor/2, refHeight + (2*scaleFactor), dist * scaleFactor /* refHeight + 2*/);
                    pCam.LookDirection =  new Vector3D(-pCam.Position.X, 0, -pCam.Position.Z);
                    pCam.LookDirection.Normalize();
                    pCam.UpDirection = new Vector3D(0, 1, 0);
                   
                }
                OrthographicCamera orthoCam = Canvas.Camera as OrthographicCamera;
                if (orthoCam != null)
                {
                    //double dist = b.SizeY * Math.Sin(90 - (pCam.FieldOfView / 2) * Math.PI / 180) / Math.Sin(pCam.FieldOfView / 2 * Math.PI / 180);
                    //Vector3D camVec = new Vector3D(-dist * Math.Cos(45 * Math.PI / 180), -dist * Math.Sin(45 * Math.PI / 180), b.SizeZ * 2);
                    //orthoCam.Transform = null; //clear any previous view transforms
                    //orthoCam.Position = new Point3D(camVec.X, camVec.Y, camVec.Z);
                    //orthoCam.LookDirection = camVec * -1;
                    //orthoCam.UpDirection = new Vector3D(0, 0, 1);
                }
            }

            //if (mdp != null) mdp.Transparency = Transparency;
        }

        #region Query methods

        public long GetProductAt(MouseButtonEventArgs e)
        {
            return Canvas.GetProductAt(e);
        }

        #endregion

        /// <summary>
        ///   Hides all instances of the specified type
        /// </summary>
        /// <param name = "type"></param>
        public void HideAllTypesOf(long product)
        {
            Type typeToHide = Model.GetInstance(Math.Abs(product)).GetType();
            foreach (var placement in _items)
            {
                long prod = placement.Key;
                Type type = Model.GetInstance(Math.Abs(prod)).GetType();
                if (type == typeToHide)
                {
                    ModelVisual3D parent = VisualTreeHelper.GetParent(placement.Value.ModelVisual) as ModelVisual3D;
                    if (parent != null)
                    {
                        _hidden.Add(placement.Value.ModelVisual, parent);
                        parent.Children.Remove(placement.Value.ModelVisual);
                    }
                }
            }
            _hiddenTypes.Add(typeToHide);
        }

        public void Hide(long hideProduct)
        {
            DrawingControl3DItem item;
            if (_items.TryGetValue(hideProduct, out item))
            {
                ModelVisual3D parent = VisualTreeHelper.GetParent(item.ModelVisual) as ModelVisual3D;
                if (parent != null)
                {
                    _hidden.Add(item.ModelVisual, parent);
                    parent.Children.Remove(item.ModelVisual);
                }
                return;
            }
        }

        public void Show(Type type)
        {
            List<ModelVisual3D> alive = new List<ModelVisual3D>();
            foreach (var item in _hidden)
            {
                IfcProduct prod = item.Key.GetValue(TagProperty) as IfcProduct;
                if (prod != null && prod.GetType() == type)
                {
                    item.Value.Children.Add(item.Key);
                    alive.Add(item.Key);
                }
            }
            foreach (var bornAgain in alive)
            {
                _hidden.Remove(bornAgain);
            }
            _hiddenTypes.Remove(type);
        }

        public void ShowAll()
        {
            foreach (var item in _hidden)
            {
                item.Value.Children.Add(item.Key);
            }
            _hiddenTypes.Clear();
            _hidden.Clear();
        }


    }
}
