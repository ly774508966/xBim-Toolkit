#pragma once
#include "IXbimGeometryModel.h"
#include "XbimFaceEnumerator.h"
#include "XbimShell.h"
#include "XbimMeshedFace.h"
#include "XbimMeshedFaceEnumerator.h"
#include "IXbimMeshGeometry.h"
#include "XbimGeometryModel.h"
#include <TopoDS_Solid.hxx>
#include <BRepGProp.hxx>
#include <GProp_GProps.hxx> 
#include <BRep_Builder.hxx> 

using namespace Xbim::XbimExtensions::Interfaces;
using namespace System::Collections::Generic;

using namespace Xbim::XbimExtensions::SelectTypes;
using namespace Xbim::Ifc2x3::GeometricModelResource;
using namespace Xbim::Ifc2x3::GeometricConstraintResource;
using namespace Xbim::Common::Logging;

namespace Xbim
{
	namespace ModelGeometry
	{
		namespace OCC
		{
			public ref class XbimMeshedFaceEnumerable: IEnumerable<XbimMeshedFace^>
			{
			private:
				TopoDS_Shape* pSolid;

			public:
				XbimMeshedFaceEnumerable(const TopoDS_Solid&  solid)
				{
					pSolid = new TopoDS_Solid();
					*pSolid = solid;
				}

				XbimMeshedFaceEnumerable(const TopoDS_Shape&  solid)
				{
					TopoDS_Compound * pComp = new TopoDS_Compound();
					BRep_Builder b;
					b.MakeCompound(*pComp);
					b.Add(*pComp, solid);
					pSolid=pComp;

				}
				virtual System::Collections::Generic::IEnumerator<XbimMeshedFace^>^ GetEnumerator()
				{

					return gcnew XbimMeshedFaceEnumerator(*pSolid, 1);
				}
				virtual System::Collections::IEnumerator^ GetEnumerator2()  sealed = System::Collections::IEnumerable::GetEnumerator
				{
					return gcnew XbimMeshedFaceEnumerator(*pSolid, 1);
				}

				~XbimMeshedFaceEnumerable()
				{
					InstanceCleanup();
				}

				!XbimMeshedFaceEnumerable()
				{
					InstanceCleanup();
				}
				void InstanceCleanup()
				{   
					int temp = System::Threading::Interlocked::Exchange((int)(void*)pSolid, 0);
					if(temp!=0)
					{
						if (pSolid)
						{
							delete pSolid;
							pSolid=0;
							System::GC::SuppressFinalize(this);
						}
					}
				}
			};


			public ref class XbimSolid  : IXbimGeometryModel,  IEnumerable<XbimFace^>
			{
			protected:
				TopoDS_Shape* nativeHandle;
				static ILogger^ Logger = LoggerFactory::GetLogger();

			private:
				Int32 _representationLabel;
				Int32 _surfaceStyleLabel;
				bool _hasCurvedEdges;



			public:
				XbimSolid(){};
				XbimSolid(const TopoDS_Solid&  solid);
				XbimSolid(const TopoDS_Solid&  solid, bool hasCurves);
				XbimSolid(const TopoDS_Shell&  shell);
				XbimSolid(const TopoDS_Shell&  shell, bool hasCurves);
				XbimSolid(const TopoDS_Shape&  shape);
				XbimSolid(const TopoDS_Shape&  shape, bool hasCurves);
				XbimSolid(IXbimGeometryModel^ solid, Matrix3D transform);
				XbimSolid(IXbimGeometryModel^ solid, bool hasCurves);
				virtual property XbimLocation ^ Location 
				{
					XbimLocation ^ get()
					{
						return gcnew XbimLocation(nativeHandle->Location());
					}
					void set(XbimLocation ^ location)
					{
						nativeHandle->Location(*(location->Handle));
					}
				};

				virtual property bool HasCurvedEdges
				{
					virtual bool get()
					{
						return _hasCurvedEdges;
					}
				}

				virtual XbimBoundingBox^ GetBoundingBox(bool precise)
				{
					return XbimGeometryModel::GetBoundingBox(this, precise);
				};

				XbimSolid(XbimSolid^ solid, IfcAxis2Placement^ origin, IfcCartesianTransformationOperator^ transform, bool hasCurves);
				XbimSolid(IfcExtrudedAreaSolid^ repItem);

				XbimSolid(IfcRevolvedAreaSolid^ repItem);

				XbimSolid(IfcFacetedBrep^ repItem);

				XbimSolid(IfcHalfSpaceSolid^ repItem);

				XbimSolid(IfcSolidModel^ repItem);

				XbimSolid(IfcClosedShell^ repItem);

				XbimSolid(IfcConnectedFaceSet^ repItem);
				XbimSolid(IfcBooleanResult^ repItem);
				XbimSolid(IfcCsgPrimitive3D^ repItem);
				XbimSolid(IfcVertexPoint^ pt);
				XbimSolid(IfcEdge^ edge);
				void Print();
				~XbimSolid()
				{
					InstanceCleanup();
				}

				!XbimSolid()
				{
					InstanceCleanup();
				}
				void InstanceCleanup()
				{   
					int temp = System::Threading::Interlocked::Exchange((int)(void*)nativeHandle, 0);
					if(temp!=0)
					{
						if (nativeHandle)
						{
							delete nativeHandle;
							nativeHandle=0;
							System::GC::SuppressFinalize(this);
						}
					}
				}

				virtual property TopoDS_Shape* Handle
				{
					TopoDS_Shape* get(){return nativeHandle;};			
				}

				virtual property double Volume
				{
					double get()
					{
						GProp_GProps System;
						BRepGProp::VolumeProperties(*nativeHandle, System);
						return System.Mass();
					}
				}

				virtual property Int32 RepresentationLabel
				{
					Int32 get(){return _representationLabel; }
					void set(Int32 value){ _representationLabel=value; }
				}

				virtual property Int32 SurfaceStyleLabel
				{
					Int32 get(){return _surfaceStyleLabel; }
					void set(Int32 value){ _surfaceStyleLabel=value; }
				}

				/*Interfaces*/


				virtual IXbimGeometryModel^ Cut(IXbimGeometryModel^ shape);
				virtual IXbimGeometryModel^ Union(IXbimGeometryModel^ shape);
				virtual IXbimGeometryModel^ Intersection(IXbimGeometryModel^ shape);
				// IEnumerable<IIfcFace^> Members
				virtual property IEnumerable<XbimFace^>^ Faces
				{
					IEnumerable<XbimFace^>^ get();
				}


				virtual IEnumerator<XbimFace^>^ GetEnumerator()
				{

					return gcnew XbimFaceEnumerator(*nativeHandle);
				}
				virtual System::Collections::IEnumerator^ GetEnumerator2() sealed = System::Collections::IEnumerable::GetEnumerator
				{
					return gcnew XbimFaceEnumerator(*nativeHandle);
				}

				// IEnumerable<XbimMeshedFace^> Members
				property System::Collections::Generic::IEnumerable<XbimMeshedFace^>^ MeshedFaces
				{
					System::Collections::Generic::IEnumerable<XbimMeshedFace^>^ get();
				}
				virtual List<XbimTriangulatedModel^>^Mesh(bool withNormals, double deflection, Matrix3D transform);
				virtual List<XbimTriangulatedModel^>^Mesh(bool withNormals, double deflection);
				virtual List<XbimTriangulatedModel^>^Mesh(bool withNormals);
				virtual List<XbimTriangulatedModel^>^Mesh();


				//solid operations

				virtual IXbimGeometryModel^ CopyTo(IfcObjectPlacement^ placement);
				virtual void Move(TopLoc_Location location);
				///static builders 

				static TopoDS_Shape Build(IfcCsgSolid^ csgSolid, bool% hasCurves);
				static TopoDS_Shape Build(IfcManifoldSolidBrep^ manifold, bool% hasCurves);

				static TopoDS_Solid Build(IfcSweptDiskSolid^ swdSolid, bool% hasCurves);

				static TopoDS_Solid Build(IfcSweptAreaSolid^ sweptAreaSolid, bool% hasCurves);
				static TopoDS_Solid Build(IfcExtrudedAreaSolid^ repItem, bool% hasCurves);
				static TopoDS_Solid Build(IfcRevolvedAreaSolid^ repItem, bool% hasCurves);
				static TopoDS_Solid Build(IfcSurfaceCurveSweptAreaSolid^ repItem, bool% hasCurves);
				static TopoDS_Shape Build(IfcFacetedBrep^ repItem, bool% hasCurves);
				static TopoDS_Solid Build(IfcBoxedHalfSpace^ bhs, bool% hasCurves);
				static TopoDS_Shape Build(IfcClosedShell^ cShell, bool% hasCurves);
				static TopoDS_Shape Build(IfcConnectedFaceSet^ cFaces, bool% hasCurves);
				static TopoDS_Solid Build(IfcHalfSpaceSolid^ repItem, bool% hasCurves);
				static TopoDS_Solid Build(IfcPolygonalBoundedHalfSpace^ pbhs, bool% hasCurves);
				static TopoDS_Shape Build(IfcBooleanResult^ repItem, bool% hasCurves);
			private:
				static TopoDS_Shell Build(const TopoDS_Wire & wire, IfcDirection^ dir, double depth, bool% hasCurves);
				static TopoDS_Solid Build(const TopoDS_Face & face, IfcDirection^ dir, double depth, bool% hasCurves);
				static TopoDS_Solid Build(const TopoDS_Wire & wire, gp_Dir dir, bool% hasCurves);
				static TopoDS_Solid Build(const TopoDS_Face & face, IfcAxis1Placement^ revolaxis, double angle, bool% hasCurves);
				static TopoDS_Solid MakeHalfSpace(IfcHalfSpaceSolid^ hs, bool% hasCurves, bool shift);
				//IXbimGeometryModel interface

			};
		}
	}
}