﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;
using Xbim.ModelGeometry;

using System.IO;
using Xbim.Common.Logging;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.SharedBldgElements;


namespace XbimConvert
{
    class Program
    {
        private static readonly ILogger Logger = LoggerFactory.GetLogger();
        private static Params arguments;

        static int Main(string[] args)
        {
            // We need to use the logger early to initialise before we use EventTrace
            Logger.Debug("XbimConvert starting...");
            using (EventTrace eventTrace = LoggerFactory.CreateEventTrace())
            {
                arguments = Params.ParseParams(args);

                if (!arguments.IsValid)
                {
                    return -1;
                }
                
                try
                {

                    Logger.InfoFormat("Starting conversion of {0}", args[0]);

                    string xbimFileName = BuildFileName(arguments.IfcFileName, ".xbim");
                    string xbimGeometryFileName = BuildFileName(arguments.IfcFileName, ".xbimGC");
                    System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();

                    watch.Start();

                    using(XbimFileModelServer model = ParseModelFile(xbimFileName))
                    {
                        
                        model.Open(xbimFileName);
                        //GenerateGeometry(xbimGeometryFileName, model);
                        IEnumerable < IfcProduct > doors = model.InstancesOfType<IfcProduct>();
                        int i = 1;
                        foreach (var door in doors)
                        {
                            Console.WriteLine(door.Name);
                           
                        }
                        model.Close();
                    }
                    watch.Stop();

                    ResetCursor(Console.CursorTop + 1);
                    Console.WriteLine("Success. Processed in " + watch.ElapsedMilliseconds + " ms");
                    GetInput();
                }
                catch (Exception e)
                {
                    ResetCursor(Console.CursorTop + 1);
                    Console.WriteLine(string.Format("Error converting {0}, {1}", arguments.IfcFileName, e.Message));
                    Logger.Error(String.Format("Error converting {0}", arguments.IfcFileName), e);
                    GetInput();
                    
                    return -1;
                }
                int errors = (from e in eventTrace.Events
                             where (e.EventLevel > EventLevel.INFO)
                             select e).Count();

                if (errors > 0)
                {
                    CreateLogFile(arguments.IfcFileName, eventTrace.Events);
                }

                Logger.Debug("XbimConvert finished...");
                return errors;
            }
            
        }

        private static void GenerateGeometry(string xbimGeometryFileName, XbimFileModelServer model)
        {
            //now convert the geometry

            IEnumerable<IfcProduct> toDraw = GetProducts(model);

            XbimScene scene = new XbimScene(model, toDraw);
            //TransformGraph graph = new TransformGraph(model, scene);
            //add everything with a representation
            //graph.AddProducts(model.IfcProducts.Items);

            using (FileStream sceneStream = new FileStream(xbimGeometryFileName, FileMode.Create, FileAccess.ReadWrite))
            {
                BinaryWriter bw = new BinaryWriter(sceneStream);
                scene.Graph.Write(bw);
                bw.Flush();
            }
        }

        private static IEnumerable<IfcProduct> GetProducts(XbimFileModelServer model)
        {
            IEnumerable<IfcProduct> result = null;

            switch (arguments.FilterType)
            {
                case FilterType.None:
                    result = model.IfcProducts.Items;
                    Logger.Debug("All geometry items will be generated");
                    break;

                case FilterType.ElementID:
                    List<IfcProduct> list = new List<IfcProduct>();
                    list.Add(model.GetInstance(arguments.ElementIdFilter) as IfcProduct);
                    result = list;
                    Logger.DebugFormat("Only generating product element ID {0}", arguments.ElementIdFilter);
                    break;

                case FilterType.ElementType:
                    Type theType = arguments.ElementTypeFilter.Type;
                    result = model.InstancesWhere<IfcProduct>(i=> i.GetType() == theType);
                    Logger.DebugFormat("Only generating product elements of type '{0}'", arguments.ElementTypeFilter);
                    break;

                default:
                    throw new NotImplementedException();
            }
            return result;
        }

        private static XbimFileModelServer ParseModelFile(string xbimFileName)
        {
            XbimFileModelServer model = new XbimFileModelServer();
            //create a callback for progress
            switch (Path.GetExtension(arguments.IfcFileName).ToLowerInvariant())
            {
                case ".ifc":
                    model.ImportIfc(arguments.IfcFileName,
                        xbimFileName,
                        delegate(int percentProgress, object userState)
                        {
                            if (!arguments.IsQuiet)
                            {
                                Console.Write(string.Format("{0:D2}% Converted", percentProgress));
                                ResetCursor(Console.CursorTop);
                            }
                        }
                        );
                    break;
                case ".xbim":
                    // TODO: check this
                    model.Open(xbimFileName);
                    break;

                case ".ifcxml":
                    model.ImportXml(arguments.IfcFileName, xbimFileName);
                    break;
                default:
                    throw new NotImplementedException(String.Format("XbimConvert does not support {0} file formats currently", Path.GetExtension(arguments.IfcFileName)));
            }
            
            return model;
        }

        private static void CreateLogFile(string ifcFile, IList<Event> events)
        {
            try
            {
                string logfile = String.Concat(ifcFile, ".log");
                using (StreamWriter writer = new StreamWriter(logfile, false))
                {
                    foreach (Event logEvent in events)
                    {
                        writer.WriteLine("{0:yyyy-MM-dd HH:mm:ss} : {1:-5} {2}.{3} - {4}",
                            logEvent.EventTime,
                            logEvent.EventLevel.ToString(),
                            logEvent.Logger,
                            logEvent.Method,
                            logEvent.Message
                            );
                    }
                    writer.Flush();
                    writer.Close();
                }
            }
            catch (Exception e)
            {
                Logger.Error(String.Format("Failed to create Log File for {0}", ifcFile), e);
            }
        }

        private static string BuildFileName(string file, string extension)
        {
            if (arguments.KeepFileExtension)
            {
                return String.Concat(file, extension);
            }
            else
            {
                return Path.ChangeExtension(file, extension);
            }
        }


        private static void GetInput()
        {
            if (!arguments.IsQuiet)
            {
                Console.WriteLine("Press Enter to continue...");
                Console.ReadLine();
            }
        }

        private static void ResetCursor(int top)
        {
            try
            {
                // Can't reset outside of buffer, and should ignore when in quiet mode
                if (top >= Console.BufferHeight || arguments.IsQuiet == true)
                    return;
                Console.SetCursorPosition(0, top);
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }


    }

     
}
