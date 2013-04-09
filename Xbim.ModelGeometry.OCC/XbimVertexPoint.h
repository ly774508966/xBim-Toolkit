#pragma once
#include <TopoDS_Vertex.hxx>
using namespace Xbim::XbimExtensions::Interfaces;
using namespace Xbim::Ifc2x3::GeometryResource;
using namespace System::Windows::Media::Media3D;

namespace Xbim
{
	namespace ModelGeometry
	{
		namespace OCC
		{
			public ref class XbimVertexPoint 
			{

			private:
				TopoDS_Vertex * pVertex;
				static double _precision = 1.E-005;
			public:
				static property double Precision
				{
					double get(){return _precision;};
					void set(double value){ _precision = value;};
				}

				XbimVertexPoint(const TopoDS_Vertex & vertex);
				XbimVertexPoint(double x, double y, double z);
				~XbimVertexPoint()
				{
					InstanceCleanup();
				}

				!XbimVertexPoint()
				{
					InstanceCleanup();
				}
				void InstanceCleanup()
				{   
					int temp = System::Threading::Interlocked::Exchange((int)(void*)pVertex, 0);
					if(temp!=0)
					{
						if (pVertex)
						{
							delete pVertex;
							pVertex=0;
							System::GC::SuppressFinalize(this);
						}
					}
				}

				virtual property IfcCartesianPoint^ VertexGeometry
				{
					IfcCartesianPoint^ get();
				}
				virtual property System::Windows::Media::Media3D::Point3D Point3D
				{
					System::Windows::Media::Media3D::Point3D get();
				}

				property TopoDS_Vertex * Handle
				{
					TopoDS_Vertex* get(){return pVertex;};			
				}

			};
		}
	}
}