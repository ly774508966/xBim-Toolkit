﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions.Interfaces;
using Microsoft.Isam.Esent.Interop;
using Xbim.IO.Parser;
using System.IO;
using Xbim.Common.Exceptions;
using System.Xml;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Transactions.Extensions;
using Xbim.XbimExtensions.SelectTypes;
using System.Linq.Expressions;
using System.Reflection;
using Xbim.Ifc2x3.Kernel;
using System.Diagnostics;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.GeometryResource;
using Microsoft.Isam.Esent.Interop.Windows7;
using System.Globalization;
using ICSharpCode.SharpZipLib.Zip;


namespace Xbim.IO
{
    
    public class IfcPersistedInstanceCache : IDisposable
    {
        #region ESE Database 

        private static Instance _jetInstance;
        private Session _session;
        private JET_DBID _databaseId;
        static int cacheSizeInBytes = 64 * 1024 * 1024;
        /// <summary>
        /// Holds the session and transaction state
        /// </summary>
        private readonly object lockObject;
        private readonly XbimEntityCursor[] _entityTables;
        private readonly XbimGeometryCursor[] _geometryTables;
        private const int MaxCachedEntityTables = 32;
        private const int MaxCachedGeometryTables = 32;
        private XbimDBAccess _accessMode;
       

        const int _transactionBatchSize = 100;


       
        #endregion
        #region Cached data
        protected Dictionary<int, IPersistIfcEntity> read = new Dictionary<int, IPersistIfcEntity>();
        protected Dictionary<int, IPersistIfcEntity> modified = new Dictionary<int, IPersistIfcEntity>();
        //protected HashSet<IPersistIfcEntity> ToDelete = new HashSet<IPersistIfcEntity>();
        //protected HashSet<IPersistIfcEntity> ToCreate = new HashSet<IPersistIfcEntity>();
        protected int CacheDefaultSize = 5000;

        #endregion

        private string _databaseName;
        private XbimModel _model;
        private bool disposed = false;
        static private ComparePropertyInfo comparePropInfo = new ComparePropertyInfo();
        private bool caching = false;
        private class ComparePropertyInfo : IEqualityComparer<PropertyInfo>
        {

            public bool Equals(PropertyInfo x, PropertyInfo y)
            {
                return x.Name == y.Name;
            }

            public int GetHashCode(PropertyInfo obj)
            {
                return obj.Name.GetHashCode();
            }
        }

        static IfcPersistedInstanceCache()
        {
            SystemParameters.DatabasePageSize = 4096;
            SystemParameters.CacheSizeMin = cacheSizeInBytes / SystemParameters.DatabasePageSize;
            SystemParameters.CacheSizeMax = cacheSizeInBytes / SystemParameters.DatabasePageSize;
            _jetInstance = CreateInstance("XbimInstance", XbimModel.XbimTempDirectory);
        }

        public IfcPersistedInstanceCache(XbimModel model)
        {
            this.lockObject = new Object();
            _model = model;
            _entityTables = new XbimEntityCursor[MaxCachedEntityTables];
            _geometryTables = new XbimGeometryCursor[MaxCachedGeometryTables];
        }
        
       
        /// <summary>
        /// Creates an empty xbim file, overwrites any existing file of the same name
        /// throw a create failed exception if unsuccessful
        /// </summary>
        /// <returns></returns>
        static internal void CreateDatabase(string fileName)
        {
            string logDirectory = Path.GetFullPath(fileName);
            logDirectory = Path.ChangeExtension(logDirectory, Guid.NewGuid().ToString());
            try
            {
                using (Instance createInstance = CreateInstance("XbimCreateInstance", logDirectory, false))
                {
                    using (var session = new Session(createInstance))
                    {
                        JET_DBID dbid;
                        Api.JetCreateDatabase(session, fileName, null, out dbid, CreateDatabaseGrbit.OverwriteExisting);
                        try
                        {
                            XbimEntityCursor.CreateTable(session, dbid);
                            XbimCursor.CreateGlobalsTable(session, dbid); //create the gobals table
                            XbimGeometryCursor.CreateTable(session, dbid);
                        }
                        catch (Exception e)
                        {
                            Api.JetCloseDatabase(session, dbid, CloseDatabaseGrbit.None);
                            Api.JetDetachDatabase(session, fileName);
                            File.Delete(fileName);
                            throw e;
                        }
                    }
                }
            }
            finally
            {
                if (Directory.Exists(logDirectory)) 
                    Directory.Delete(logDirectory, true);
            }
        }

     


        #region Table functions

        /// <summary>
        /// Returns a cached or new entity table, assumes the database filename has been specified
        /// </summary>
        /// <returns></returns>
        internal XbimEntityCursor GetEntityTable()
        {
            Debug.Assert(!string.IsNullOrEmpty(_databaseName));
            lock (this.lockObject)
            {
                for (int i = 0; i < this._entityTables.Length; ++i)
                {
                    if (null != this._entityTables[i] )
                    {
                        var table = this._entityTables[i];
                        this._entityTables[i] = null;
                        return table;
                    }
                }
            }
            OpenDatabaseGrbit openMode = OpenDatabaseGrbit.None;
            if (_accessMode == XbimDBAccess.Read)
                openMode = OpenDatabaseGrbit.ReadOnly;
            if (_session == null)
            {
                _session = new Session(_jetInstance);
                Api.JetAttachDatabase(_session, _databaseName, openMode == OpenDatabaseGrbit.ReadOnly ? AttachDatabaseGrbit.ReadOnly : AttachDatabaseGrbit.None);
                //Api.JetOpenDatabase(_session, _databaseName, String.Empty, out _databaseId, openMode); 
            }
            return new XbimEntityCursor(_jetInstance, _databaseName, openMode);
        }

        /// <summary>
        /// Returns a cached or new Geometry Table, assumes the database filename has been specified
        /// </summary>
        /// <returns></returns>
        internal XbimGeometryCursor GetGeometryTable()
        {
            Debug.Assert(!string.IsNullOrEmpty(_databaseName));
            lock (this.lockObject)
            {
                for (int i = 0; i < this._geometryTables.Length; ++i)
                {
                    if (null != this._geometryTables[i])
                    {
                        var table = this._geometryTables[i];
                        this._geometryTables[i] = null;
                        return table;
                    }
                }
            }
            OpenDatabaseGrbit openMode = OpenDatabaseGrbit.None;
            if (_accessMode == XbimDBAccess.Read)
                openMode = OpenDatabaseGrbit.ReadOnly;
            if (_session == null)
            {
                _session = new Session(_jetInstance);
                Api.JetAttachDatabase(_session, _databaseName, openMode == OpenDatabaseGrbit.ReadOnly ? AttachDatabaseGrbit.ReadOnly : AttachDatabaseGrbit.None);
                //Api.JetOpenDatabase(_session, _databaseName, String.Empty, out _databaseId, openMode);
               
            }
            return new XbimGeometryCursor(_jetInstance, _databaseName, openMode);
        }

        /// <summary>
        /// Free a table. This will cache the table if the cache isn't full
        /// and dispose of it otherwise.
        /// </summary>
        /// <param name="table">The cursor to free.</param>
        internal void FreeTable(XbimEntityCursor table)
        {
            Debug.Assert(null != table, "Freeing a null table");

            lock (this.lockObject)
            {
                for (int i = 0; i < this._entityTables.Length; ++i)
                {
                    if (null == this._entityTables[i])
                    {
                        this._entityTables[i] = table;
                        return;
                    }
                }
            }

            // Didn't find a slot to cache the cursor in, throw it away
            table.Dispose();
        }

        /// <summary>
        /// Free a table. This will cache the table if the cache isn't full
        /// and dispose of it otherwise.
        /// </summary>
        /// <param name="table">The cursor to free.</param>
        public void FreeTable(XbimGeometryCursor table)
        {
            Debug.Assert(null != table, "Freeing a null table");

            lock (this.lockObject)
            {
                for (int i = 0; i < this._geometryTables.Length; ++i)
                {
                    if (null == this._geometryTables[i])
                    {
                        this._geometryTables[i] = table;
                        return;
                    }
                }
            }

            // Didn't find a slot to cache the cursor in, throw it away
            table.Dispose();
        }
        #endregion


        /// <summary>
        ///  Opens an xbim model server file, exception is thrown if errors are encountered
        /// </summary>
        /// <param name="filename"></param>
        internal void Open(string filename, XbimDBAccess accessMode = XbimDBAccess.Read)
        {
            Close();
            _databaseName = Path.GetFullPath(filename); //success store the name of the DB file
            _accessMode = accessMode;
            caching = false;  
            XbimEntityCursor entTable = GetEntityTable();
            try
            {
                using (var transaction = entTable.BeginReadOnlyTransaction())
                {
                    _model.Header = entTable.ReadHeader();
                }
            }
            catch (Exception e)
            {
                Close();
                throw new XbimException("Failed to open " + filename, e);
            }
            finally
            {
                FreeTable(entTable);
            }
        }

        /// <summary>
        /// Clears all contents from the cache and closes any connections
        /// </summary>
        public void Close()
        {

           
            for (int i = 0; i < this._entityTables.Length; ++i)
            {
                if (null != this._entityTables[i])
                {
                    this._entityTables[i].Dispose();
                    this._entityTables[i] = null;
                }
            }
            for (int i = 0; i < this._geometryTables.Length; ++i)
            {
                if (null != this._geometryTables[i])
                {
                    this._geometryTables[i].Dispose();
                    this._geometryTables[i] = null;
                }
            }
            EndCaching();


            if (_session != null)
            {
                //Api.JetCloseDatabase(_session, _databaseId, CloseDatabaseGrbit.None);
                Api.JetDetachDatabase(_session, _databaseName);
                this._databaseName = null;
                _session.Dispose();
                _session = null;
            }
        }


        

        /// <summary>
        /// Imports the contents of the ifc file into the named database, the resulting database is closed after success, use Open to access
        /// </summary>
        /// <param name="progressHandler"></param>
        /// <returns></returns>
        public void ImportIfc(string xbimDbName, string toImportIfcFilename, ReportProgressDelegate progressHandler = null)
        {
            CreateDatabase(xbimDbName);
            Open(xbimDbName, XbimDBAccess.Exclusive);
            var table = GetEntityTable();
            try
            {
                using (var transaction = table.BeginLazyTransaction())
                {
                    using (FileStream reader = new FileStream(toImportIfcFilename, FileMode.Open, FileAccess.Read))
                    {
                        using (P21toIndexParser part21Parser = new P21toIndexParser(reader, table, transaction))
                        {
                            if (progressHandler != null) part21Parser.ProgressStatus += progressHandler;
                            part21Parser.Parse();
                            _model.Header = part21Parser.Header;
                            table.WriteHeader(part21Parser.Header);
                            if (progressHandler != null) part21Parser.ProgressStatus -= progressHandler;
                        }
                    }
                    transaction.Commit();
                }
                FreeTable(table);
                Close();
            }
            catch (Exception e)
            {
                FreeTable(table);
                Close();
                File.Delete(xbimDbName);
                throw e;
            }
        }
        /// <summary>
        /// Imports an Ifc Zip file
        /// </summary>
        /// <param name="toImportFilename"></param>
        /// <param name="progressHandler"></param>
        public void ImportIfcZip(string xbimDbName, string toImportFilename, ReportProgressDelegate progressHandler = null)
        {
            CreateDatabase(xbimDbName);
            Open(xbimDbName, XbimDBAccess.Exclusive);
            var table = GetEntityTable();
            try 
            {
                using (FileStream fileStream = File.OpenRead(toImportFilename))
                {
                    // used because - The ZipInputStream has one major advantage over using ZipFile to read a zip: 
                    // it can read from an unseekable input stream - such as a WebClient download
                    using (ZipInputStream zipStream = new ZipInputStream(fileStream))
                    {
                        ZipEntry entry = zipStream.GetNextEntry();
                        while (entry != null)
                        {
                            string ext = Path.GetExtension(entry.Name);
                            //look for a valid ifc supported file
                            if (entry.IsFile &&
                                (string.Compare(ext, ".ifc", true) == 0)
                                )
                            {
                                using (ZipFile zipFile = new ZipFile(toImportFilename))
                                {
                                    using (var transaction = table.BeginLazyTransaction())
                                    {
                                        using (Stream reader = zipFile.GetInputStream(entry))
                                        {
                                            using (P21toIndexParser part21Parser = new P21toIndexParser(reader, table, transaction))
                                            {
                                                if (progressHandler != null) part21Parser.ProgressStatus += progressHandler;
                                                part21Parser.Parse();
                                                _model.Header = part21Parser.Header;
                                                table.WriteHeader(part21Parser.Header);
                                                if (progressHandler != null) part21Parser.ProgressStatus -= progressHandler;
                                            }
                                        }
                                        transaction.Commit();
                                    }
                                    FreeTable(table);
                                    Close();
                                    return; // we only want the first file
                                }
                            }
                            else if(string.Compare(ext, ".ifcxml") == 0)
                            {
                                using (ZipFile zipFile = new ZipFile(toImportFilename))
                                {
                                    using (var transaction = table.BeginLazyTransaction())
                                    {
                                       // XmlReaderSettings settings = new XmlReaderSettings() { IgnoreComments = true, IgnoreWhitespace = false };
                                        using (Stream xmlInStream = zipFile.GetInputStream(entry))
                                        {
                                            using (XmlTextReader xmlReader = new XmlTextReader(xmlInStream))
                                            {
                                                IfcXmlReader reader = new IfcXmlReader();
                                                _model.Header = reader.Read(this, table, xmlReader);
                                                table.WriteHeader(_model.Header);
                                            }
                                        }
                                        transaction.Commit();
                                    }
                                    FreeTable(table);
                                    Close();
                                    return;
                                }
                            }

                            entry = zipStream.GetNextEntry(); //get next entry
                        }
                    }
                }
                FreeTable(table);
                Close();
                File.Delete(xbimDbName);
            }
            catch (Exception e)
            {
                FreeTable(table);
                Close();
                File.Delete(xbimDbName);
                throw e;
            }

        }

          
                

            
           
           

        /// <summary>
        /// Sets up the Esent directories, can only be call before the Init method of the instance
        /// </summary>
        
        static string GetXbimTempDirectory()
        {
            //Directories are setup using the following strategy
            //First look in the config file, then try and use windows temporary directory, then the current working directory
            string tempDirectory = System.Configuration.ConfigurationManager.AppSettings["XbimTempDirectory"];
            if (!IsValidDirectory(ref tempDirectory))
            {
                tempDirectory = Path.Combine(Path.GetTempPath(), "Xbim");
                if (!IsValidDirectory(ref tempDirectory))
                {
                    tempDirectory = Path.Combine(Directory.GetCurrentDirectory(),"Xbim");
                    if (!IsValidDirectory(ref tempDirectory))
                        throw new XbimException("Unable to initialise the Xbim database engine, no write access. Please set a location for the XbimTempDirectory in the config file");
                }
            }
            return tempDirectory;
        }

        /// <summary>
        /// Checks the directory is writeable and modifies to be the full path
        /// </summary>
        /// <param name="tempDirectory"></param>
        /// <returns></returns>
        private static bool IsValidDirectory(ref string tempDirectory)
        {
            string tmpFileName = Guid.NewGuid().ToString();
            string fullTmpFileName = "";
            if (!string.IsNullOrWhiteSpace(tempDirectory))
            {
                tempDirectory = Path.GetFullPath(tempDirectory);
                bool deleteDir = false;
                try
                {

                    fullTmpFileName = Path.Combine(tempDirectory, tmpFileName);
                    if (!Directory.Exists(tempDirectory))
                    {
                        Directory.CreateDirectory(tempDirectory);
                        deleteDir = true;
                    }
                    using (FileStream fs = File.Create(fullTmpFileName)) { };
                    return true;
                }
                catch (Exception)
                {
                    tempDirectory = null;
                }
                finally
                {
                    File.Delete(fullTmpFileName);
                    if (deleteDir) Directory.Delete(tempDirectory);
                }
            }
            return false;
        }

        static private Instance CreateInstance(string instanceName, string tempDirectory = null,  bool recovery = false)
        {
 
            var jetInstance = new Instance(instanceName+Guid.NewGuid().ToString());
            if (string.IsNullOrWhiteSpace(tempDirectory))
                tempDirectory = GetXbimTempDirectory();
            jetInstance.Parameters.BaseName = "XBM";
            jetInstance.Parameters.SystemDirectory = tempDirectory;
            jetInstance.Parameters.LogFileDirectory = tempDirectory;
            jetInstance.Parameters.TempDirectory = tempDirectory;
            jetInstance.Parameters.AlternateDatabaseRecoveryDirectory = tempDirectory;
            jetInstance.Parameters.CreatePathIfNotExist = true;
            jetInstance.Parameters.EnableIndexChecking = false;       // TODO: fix unicode indexes
            jetInstance.Parameters.CircularLog = true;
            jetInstance.Parameters.CheckpointDepthMax = cacheSizeInBytes;
            jetInstance.Parameters.LogFileSize = 1024;    // 1MB logs
            jetInstance.Parameters.LogBuffers = 1024;     // buffers = 1/2 of logfile
            jetInstance.Parameters.MaxTemporaryTables = 20;
            jetInstance.Parameters.MaxVerPages = 2048;
            jetInstance.Parameters.NoInformationEvent = true;
            jetInstance.Parameters.WaypointLatency = 1;
            jetInstance.Parameters.MaxSessions = 256;
            jetInstance.Parameters.MaxOpenTables = 256;
           
            InitGrbit grbit = EsentVersion.SupportsWindows7Features
                                  ? Windows7Grbits.ReplayIgnoreLostLogs
                                  : InitGrbit.None;
            jetInstance.Parameters.Recovery = recovery; 
            jetInstance.Init(grbit);

            return jetInstance;
        }

        /// <summary>
        ///   Imports an Xml file memory model into the model server, only call when the database instances table is empty
        /// </summary>
        public void ImportIfcXml(string xbimDbName, string xmlFilename, ReportProgressDelegate progressHandler = null)
        {
            CreateDatabase(xbimDbName);
            Open(xbimDbName, XbimDBAccess.Exclusive);
            var table = GetEntityTable();
            try
            {
                using (var transaction = table.BeginLazyTransaction())
                {
                    //XmlReaderSettings settings = new XmlReaderSettings() { IgnoreComments = true, IgnoreWhitespace = false };
                    using (Stream xmlInStream = new FileStream(xmlFilename, FileMode.Open, FileAccess.Read))
                    {
                        using (XmlTextReader xmlReader = new XmlTextReader(xmlInStream))
                        {
                            IfcXmlReader reader = new IfcXmlReader();
                            _model.Header = reader.Read(this, table, xmlReader);
                            table.WriteHeader(_model.Header);
                        }
                    }
                    transaction.Commit();
                }
                FreeTable(table);
                Close();
            }
            catch (Exception e)
            {
                FreeTable(table);
                Close();
                File.Delete(xbimDbName);
                throw new Exception("Error importing IfcXml File " + xmlFilename, e);
            }
        }


        public bool Contains(IPersistIfcEntity instance)
        {
            return Contains(Math.Abs(instance.EntityLabel));
        }

        public bool Contains(int posLabel)
        {
            if (caching && this.read.ContainsKey(posLabel)) //check if it is cached
                return true;
            else //look in the database
            {
                var entityTable = GetEntityTable();
                try
                {
                    return entityTable.TrySeekEntityLabel(posLabel);
                }
                finally
                {
                    FreeTable(entityTable);
                }
            }
        }

       
       /// <summary>
        /// returns the number of instances of the specified type and its sub types
       /// </summary>
       /// <typeparam name="TIfcType"></typeparam>
       /// <returns></returns>
        public long CountOf<TIfcType>() where TIfcType : IPersistIfcEntity
        {
            return CountOf(typeof(TIfcType));
           
        }
        /// <summary>
        /// returns the number of instances of the specified type and its sub types
        /// </summary>
        /// <param name="theType"></param>
        /// <returns></returns>
        private long CountOf(Type theType)
        {
            HashSet<int> entityLabels = new HashSet<int>();
            IfcType ifcType = IfcMetaData.IfcType(theType);
            var entityTable = GetEntityTable();
            HashSet<short> typeIds = new HashSet<short>();
            //get all the type ids we are going to check for
            foreach (Type t in ifcType.NonAbstractSubTypes)
                typeIds.Add(IfcMetaData.IfcTypeId(t));
            try
            {

                XbimInstanceHandle ih;
                if (ifcType.IndexedClass)
                {
                    foreach (var typeId in typeIds)
                    {


                        if (entityTable.TrySeekEntityType(typeId, out ih))
                        {
                            do
                            {
                                entityLabels.Add(ih.EntityLabel);
                            } while (entityTable.TryMoveNextEntityType(out ih));
                        }
                    }
                }
                else
                {

                    entityTable.MoveBeforeFirst();
                    while (entityTable.TryMoveNext())
                    {
                        ih = entityTable.GetInstanceHandle();
                        if (typeIds.Contains(ih.EntityTypeId))
                            entityLabels.Add(ih.EntityLabel);
                    }

                }
            }
            finally
            {
                FreeTable(entityTable);
            }
            if (caching) //look in the modified cache and find the new ones only
            {
                foreach (var entity in Modified().Where(m => m.GetType() == theType))
                    entityLabels.Add(entity.EntityLabel);
                  
            }
            return entityLabels.Count;
        }

        public bool Any<TIfcType>() where TIfcType : IPersistIfcEntity
        {
            IfcType ifcType = IfcMetaData.IfcType(typeof(TIfcType));
            var entityTable = GetEntityTable();
            try
            {
                foreach (Type t in ifcType.NonAbstractSubTypes)
                {
                    short typeId = IfcMetaData.IfcTypeId(t);
                    XbimInstanceHandle ih;
                    if (!entityTable.TrySeekEntityType(typeId,out ih))
                        return true;
                }
            }
            finally
            {
                FreeTable(entityTable);
            }
            return false;
        }
        /// <summary>
        /// returns the number of instances in the model
        /// </summary>
        /// <returns></returns>
        public long Count 
        {
            get
            {
                var entityTable = GetEntityTable();
                try
                {
                    return entityTable.RetrieveCount();
                }
                finally
                {
                    FreeTable(entityTable);
                }
            }
        }

        /// <summary>
        /// returns the value of the highest current entity label
        /// </summary>
        public int HighestLabel
        {
            get
            {
                var entityTable = GetEntityTable();
                try
                {
                    return entityTable.RetrieveHighestLabel();
                }
                finally
                {
                    FreeTable(entityTable);
                }
                
            }
        }


        /// <summary>
        /// Creates a new instance this is a reversable action and should be used typically
        /// </summary>
        /// <param name="t"></param>
        /// <param name="label"></param>
        /// <returns></returns>
        public IPersistIfcEntity CreateNew_Reversable(Type t)
        {
            Debug.Assert(caching);
            XbimEntityCursor cursor = _model.GetTransactingCursor();
            XbimInstanceHandle h = cursor.AddEntity(t);
            IPersistIfcEntity entity = (IPersistIfcEntity)Activator.CreateInstance(t);
            entity.Bind(_model, h.EntityLabel); //bind it, the object is new and empty so the label is positive
            this.read.Add_Reversible(new KeyValuePair<int, IPersistIfcEntity>(h.EntityLabel, entity));
            modified.Add_Reversible(new KeyValuePair<int, IPersistIfcEntity>(h.EntityLabel, entity));
            return entity;
        }

        /// <summary>
        /// Creates a new instance, this is not a reversable action, and the instance is not cached
        /// It is for performance in import and export routines and should not be used in normal code
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal IPersistIfcEntity CreateNew(Type type, int label)
        {
            int posLabel = Math.Abs(label);
            IPersistIfcEntity entity = (IPersistIfcEntity)Activator.CreateInstance(type);
            entity.Bind(_model, posLabel); //bind it, the object is new and empty so the label is positive
            //this.Add(posLabel, entity);
            //ToCreate.Add(entity);
            return entity;
        }

        /// <summary>
        /// Deprecated. Use CountOf, returns the number of instances of the specified type
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public long InstancesOfTypeCount(Type t)
        {
            return CountOf(t);
        }

       
        /// <summary>
        /// Returns an enumeration of handles to all instances in the database and in the cache
        /// </summary>
        public IEnumerable<XbimInstanceHandle> InstanceHandles
        {
            get
            {
                var entityTable = GetEntityTable();
                try
                {
                    
                    if (entityTable.TryMoveFirst()) // we have something
                    {
                        do
                        {
                            yield return entityTable.GetInstanceHandle();
                        }
                        while (entityTable.TryMoveNext());
                    }
                }
                finally
                {
                    FreeTable(entityTable);
                }
            }
        }
        /// <summary>
        /// Returns an enumeration of handles to all instances in the database or the cache of specified type
        /// </summary>
        /// <returns></returns>
        public IEnumerable<XbimInstanceHandle> InstanceHandlesOfType<TIfcType>()
        {
            Type reqType = typeof(TIfcType);
            IfcType ifcType = IfcMetaData.IfcType(reqType);
            var entityTable = GetEntityTable();
            try
            {
 
                foreach (Type t in ifcType.NonAbstractSubTypes)
                {
                    short typeId = IfcMetaData.IfcTypeId(t);
                    XbimInstanceHandle ih;
                    if (entityTable.TrySeekEntityType(typeId, out ih))
                    {
                        yield return ih;
                        while (entityTable.TryMoveNext())
                        {
                            ih = entityTable.GetInstanceHandle();
                            yield return ih;
                        }
                    }
                }
            }
            finally
            {
                FreeTable(entityTable);
            }
        }

        /// <summary>
        /// Returns an instance of the entity with the specified label,
        /// if the instance has already been loaded it is returned from the cache
        /// if it has not been loaded a blank instance is loaded, i.e. will not have been activated
        /// </summary>
        /// <param name="label"></param>
        /// <returns></returns>
        public IPersistIfcEntity GetInstance(int label, bool loadProperties = false, bool unCached = false)
        {
            int posLabel = Math.Abs(label);
            IPersistIfcEntity entity;
            if (caching && this.read.TryGetValue(posLabel, out entity))
                return entity;
            else
                return GetInstanceFromStore(posLabel, loadProperties, unCached);
        }

        /// <summary>
        /// Loads a blank instance from the database, do not call this before checking that the instance is in the instances cache
        /// If the entity has already been cached it will throw an exception
        /// This is not a undoable/reversable operation
        /// </summary>
        /// <param name="posLabel">Must be a positive value of the label</param>
        /// <param name="loadProperties">if true the properties of the object are loaded  at the same time</param>
        /// <param name="unCached">if true the object is not cached, this is dangerous and can lead to object duplicates</param>
        /// <returns></returns>
        private IPersistIfcEntity GetInstanceFromStore(int posLabel, bool loadProperties = false, bool unCached = false)
        {
            var entityTable = GetEntityTable();
            try
            {
                using (var transaction = entityTable.BeginReadOnlyTransaction())
                {
                    
                    if (entityTable.TrySeekEntityLabel(posLabel))
                    {
                        short currentIfcTypeId = entityTable.GetIfcType();
                        IPersistIfcEntity entity = (IPersistIfcEntity)Activator.CreateInstance(IfcMetaData.GetType(currentIfcTypeId));
                        if (loadProperties)
                        {
                            byte[] properties = entityTable.GetProperties();
                            entity.ReadEntityProperties(this, new BinaryReader(new MemoryStream(properties)), unCached);
                            entity.Bind(_model, posLabel); //a positive handle determines that the attributes of this entity have been loaded yet
                        }
                        else
                            entity.Bind(_model, -posLabel); //a negative handle determines that the attributes of this entity have not been loaded yet
                        if (caching && !unCached)
                            this.read.Add(posLabel, entity);
                        return entity;
                    }
                }
            }
            finally
            {
                FreeTable(entityTable);
            }
            return null;
            
        }

        public void Print()
        {
            Debug.WriteLine(InstanceHandles.Count());
                
            Debug.WriteLine(HighestLabel);
            Debug.WriteLine(Count);
            Debug.WriteLine(GeometriesCount());
           // Debug.WriteLine(Any<Xbim.Ifc2x3.SharedBldgElements.IfcWall>());
            //Debug.WriteLine(Count<Xbim.Ifc2x3.SharedBldgElements.IfcWall>());
            //IEnumerable<IfcElement> elems = OfType<IfcElement>();
            //foreach (var elem in elems)
            //{
            //    IEnumerable<IfcRelVoidsElement> rels = elem.HasOpenings;
            //    bool written = false;
            //    foreach (var rel in rels)
            //    {
            //        if (!written) { Debug.Write(elem.EntityLabel + " = "); written = true; }
            //        Debug.Write(rel.EntityLabel +", ");
            //    }
            //    if (written) Debug.WriteLine(";");
            //}
        }
        private IEnumerable<TIfcType> OfTypeUnindexed<TIfcType>(IfcType ifcType, bool activate = false) where TIfcType : IPersistIfcEntity
        {
            HashSet<int> entityLabels = new HashSet<int>();
            var entityTable = GetEntityTable();
            try
            {
                //get all the type ids we are going to check for
                HashSet<short> typeIds = new HashSet<short>();
                foreach (Type t in ifcType.NonAbstractSubTypes)
                    typeIds.Add(IfcMetaData.IfcTypeId(t));
                using (var transaction = entityTable.BeginReadOnlyTransaction())
                {
                    entityTable.MoveBeforeFirst();
                    while (entityTable.TryMoveNext())
                    {
                        XbimInstanceHandle ih = entityTable.GetInstanceHandle();
                        if (typeIds.Contains(ih.EntityTypeId))
                        {
                            IPersistIfcEntity entity;
                            if (caching && this.read.TryGetValue(ih.EntityLabel, out entity))
                            {
                                if (activate && !entity.Activated) //activate if required and not already done
                                {
                                    byte[] properties = entityTable.GetProperties();
                                    entity.ReadEntityProperties(this, new BinaryReader(new MemoryStream(properties)), false);
                                    entity.Bind(_model, ih.EntityLabel); //a positive handle determines that the attributes of this entity have been loaded yet
                                }
                                entityLabels.Add(entity.EntityLabel);
                                yield return (TIfcType)entity;
                            }
                            else
                            {
                                entity = (IPersistIfcEntity)Activator.CreateInstance(ih.EntityType);
                                if (activate)
                                {
                                    byte[] properties = entityTable.GetProperties();
                                    entity.ReadEntityProperties(this, new BinaryReader(new MemoryStream(properties)), false);
                                    entity.Bind(_model, ih.EntityLabel); //a positive handle determines that the attributes of this entity have been loaded yet
                                }
                                else
                                    entity.Bind(_model, -ih.EntityLabel); //a negative handle determines that the attributes of this entity have not been loaded yet

                                if (caching) this.read.Add(ih.EntityLabel, entity);
                                entityLabels.Add(entity.EntityLabel);
                                yield return (TIfcType)entity;
                            }

                        }
                    }
                }
                if (caching) //look in the modified cache and find the new ones only
                {
                    foreach (var entity in Modified().OfType<TIfcType>())
                    {
                        if (entityLabels.Add(entity.EntityLabel))
                        { 
                            yield return (TIfcType)entity;
                        }
                    }
                }
            }
            finally
            {
                FreeTable(entityTable);
            }
        }


        /// <summary>
        /// Enumerates of all instances of the specified type. The values are cached, if activate is true all the properties of the entity are loaded
        /// </summary>
        /// <typeparam name="TIfcType"></typeparam>
        /// <param name="activate">if true loads the properties of the entity</param>
        /// <param name="indexKey">if the entity has a key object, optimises to search for this handle</param>
        /// <returns></returns>
        public IEnumerable<TIfcType> OfType<TIfcType>(bool activate = false, int indexKey = -1) where TIfcType:IPersistIfcEntity 
        {
            IfcType ifcType = IfcMetaData.IfcType(typeof(TIfcType));
            if (!ifcType.IndexedClass)
            {
                Debug.Assert(indexKey==-1, "Trying to look a class up by index key, but the class is not indexed");
                foreach (var item in OfTypeUnindexed<TIfcType>(ifcType, activate))
                    yield return item;
            }
            else
            {
                //Set the IndexedClass Attribute of this class to ensure that seeking by index will work, this is a optimisation
                Debug.Assert(ifcType.IndexedClass, "Trying to look a class up by index that is not declared as indexeable");
                HashSet<int> entityLabels = new HashSet<int>();
                var entityTable = GetEntityTable();
                try
                {
                    using (var transaction = entityTable.BeginReadOnlyTransaction())
                    {

                        foreach (Type t in ifcType.NonAbstractSubTypes)
                        {
                            short typeId = IfcMetaData.IfcTypeId(t);
                            XbimInstanceHandle ih;
                            if (entityTable.TrySeekEntityType(typeId, out ih, indexKey) && entityTable.TrySeekEntityLabel(ih.EntityLabel)) //we have the first instance
                            {
                                do
                                {
                                    IPersistIfcEntity entity;
                                    if (caching && this.read.TryGetValue(ih.EntityLabel, out entity))
                                    {
                                        if (activate && !entity.Activated) //activate if required and not already done
                                        {
                                            byte[] properties = entityTable.GetProperties();
                                            entity.ReadEntityProperties(this, new BinaryReader(new MemoryStream(properties)), false);
                                            entity.Bind(_model, ih.EntityLabel); //a positive handle determines that the attributes of this entity have been loaded yet
                                        }
                                        entityLabels.Add(entity.EntityLabel);
                                        yield return (TIfcType)entity;
                                    }
                                    else
                                    {
                                        entity = (IPersistIfcEntity)Activator.CreateInstance(ih.EntityType);
                                        if (activate)
                                        {
                                            byte[] properties = entityTable.GetProperties();
                                            entity.ReadEntityProperties(this, new BinaryReader(new MemoryStream(properties)), false);
                                            entity.Bind(_model, ih.EntityLabel); //a positive handle determines that the attributes of this entity have been loaded yet
                                        }
                                        else
                                            entity.Bind(_model, -ih.EntityLabel); //a negative handle determines that the attributes of this entity have not been loaded yet

                                        if (caching) this.read.Add(ih.EntityLabel, entity);
                                        entityLabels.Add(entity.EntityLabel);
                                        yield return (TIfcType)entity;
                                    }
                                } while (entityTable.TryMoveNextEntityType(out ih) && entityTable.TrySeekEntityLabel(ih.EntityLabel));
                            }

                        }
                    }
                    if (caching) //look in the modified cache and find the new ones only
                    {

                        foreach (var entity in Modified().OfType<TIfcType>())
                        {
                            if (indexKey == -1) //get all of the type
                            {
                                if (!entityLabels.Contains(entity.EntityLabel))
                                {
                                    entityLabels.Add(entity.EntityLabel);
                                    yield return (TIfcType)entity;
                                }
                            }
                            else
                            {
                                // get all types that match the index key
                                if (ifcType.GetIndexedValues(entity).Contains(indexKey))
                                {
                                    if (!entityLabels.Contains(entity.EntityLabel))
                                    {
                                        entityLabels.Add(entity.EntityLabel);
                                        yield return (TIfcType)entity;
                                    }
                                }
                            }
                        }
                    }
                }
                finally
                {
                    FreeTable(entityTable);
                }
            }
        }


        public void ImportXbim(string importFrom, ReportProgressDelegate progressHandler = null)
        {
            
            throw new NotImplementedException();
           
        }

       


        public void Activate(IPersistIfcEntity entity)
        {
            byte[] bytes = GetEntityBinaryData(entity);
            if (bytes != null)
                entity.ReadEntityProperties(this, new BinaryReader(new MemoryStream(bytes)));
        }

        public void Dispose()
        {
            Dispose(true);
            // Take yourself off the Finalization queue 
            // to prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        ~IfcPersistedInstanceCache()
        {
            Dispose(false);
        }

        protected void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                // If disposing equals true, dispose all managed 
                // and unmanaged resources.
                if (disposing)
                {
                    Close();
                    GC.SuppressFinalize(this);
                }

            }
            disposed = true;
        }


        /// <summary>
        /// Gets the entities propertyData on binary stream
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        internal byte[] GetEntityBinaryData(IPersistIfcEntity entity)
        {
            var entityTable = GetEntityTable();
            try
            {
                using (var transaction = entityTable.BeginReadOnlyTransaction())
                {
                    
                    int posLabel = Math.Abs(entity.EntityLabel);
                    if (entityTable.TrySeekEntityLabel(posLabel))
                        return entityTable.GetProperties();
                }
            }
            finally
            {
                FreeTable(entityTable);
            }
            return null;
        }




        public void SaveAs(XbimStorageType _storageType, string _storageFileName, ReportProgressDelegate progress = null)
        {
            switch (_storageType)
            {
                case XbimStorageType.IFCXML:
                    SaveAsIfcXml(_storageFileName);
                    break;
                case XbimStorageType.IFC:
                    SaveAsIfc(_storageFileName);
                    break;
                case XbimStorageType.IFCZIP:
                    SaveAsIfcZip(_storageFileName);
                    break;
                case XbimStorageType.XBIM:
                    Debug.Assert(false, "Incorrect call, see XbimModel.SaveAs");
                    break;
                case XbimStorageType.INVALID:
                default:
                    break;
            }

        }

        private void SaveAsIfcZip(string storageFileName)
        {
            if (string.IsNullOrWhiteSpace(Path.GetExtension(storageFileName))) //make sure we have an extension
                storageFileName = Path.ChangeExtension(storageFileName, "IfcZip");
            string fileBody = Path.ChangeExtension(Path.GetFileName(storageFileName),"ifc");
            var entityTable = GetEntityTable();
            FileStream fs = null;
            ZipOutputStream zipStream = null;
            try
            {
                fs = new FileStream(storageFileName, FileMode.Create, FileAccess.Write);
                zipStream = new ZipOutputStream(fs);
                zipStream.SetLevel(3); //0-9, 9 being the highest level of compression
                ZipEntry newEntry = new ZipEntry(fileBody);
                newEntry.DateTime = DateTime.Now;
                zipStream.PutNextEntry(newEntry);
                using (var transaction = entityTable.BeginReadOnlyTransaction())
                {
                    using (TextWriter tw = new StreamWriter(zipStream))
                    {
                        Part21FileWriter p21 = new Part21FileWriter();
                        p21.Write(_model, tw);
                        tw.Flush();
                    }

                }
            }
            catch (Exception e)
            {
                throw new XbimException("Failed to write IfcZip file " + storageFileName, e);
            }
            finally
            {
                if (fs != null) fs.Close();
                if (zipStream != null) zipStream.Close();
                FreeTable(entityTable);
            }
        }

       

        private void SaveAsIfc(string storageFileName)
        {
            if (string.IsNullOrWhiteSpace(Path.GetExtension(storageFileName))) //make sure we have an extension
                storageFileName = Path.ChangeExtension(storageFileName, "Ifc");
            var entityTable = GetEntityTable();
            try
            {
                using (var transaction = entityTable.BeginReadOnlyTransaction())
                {
                    using (TextWriter tw = new StreamWriter(storageFileName))
                    {
                        Part21FileWriter p21 = new Part21FileWriter();
                        p21.Write(_model, tw);
                        tw.Flush();
                    }
                   
                }
            }
            catch (Exception e)
            {
                throw new XbimException("Failed to write Ifc file " + storageFileName, e);
            }
            finally
            {
                FreeTable(entityTable);
            }

        }

        private void SaveAsIfcXml(string storageFileName)
        {
            if (string.IsNullOrWhiteSpace(Path.GetExtension(storageFileName))) //make sure we have an extension
                storageFileName = Path.ChangeExtension(storageFileName, "IfcXml");
            FileStream xmlOutStream = null;
            try
            {
                xmlOutStream = new FileStream(storageFileName, FileMode.Create, FileAccess.ReadWrite);
                XmlWriterSettings settings = new XmlWriterSettings { Indent = true };
                using (XmlWriter xmlWriter = XmlWriter.Create(xmlOutStream, settings))
                {
                    IfcXmlWriter writer = new IfcXmlWriter();
                    writer.Write(_model, xmlWriter);
                }
            }
            catch (Exception e)
            {
                throw new XbimException("Failed to write IfcXml file " + storageFileName, e);
            }
            finally
            {
                if (xmlOutStream != null) xmlOutStream.Close();
            }
        }



        public void Delete_Reversable(IPersistIfcEntity instance)
        {
            throw new NotImplementedException();
        }

        public bool Saved
        {
            get
            {
                throw new NotImplementedException();
            }

        }
        #region Support for Linq based indexed searching


        private static MemberExpression GetIndexablePropertyOnLeft<T>(Expression leftSide)
        {
            MemberExpression mex = leftSide as MemberExpression;
            if (leftSide.NodeType == ExpressionType.Call)
            {
                MethodCallExpression call = leftSide as MethodCallExpression;
                if (call.Method.Name == "CompareString")
                {
                    mex = call.Arguments[0] as MemberExpression;
                }
            }

            return mex;
        }


        private static object GetRight(Expression leftSide, Expression rightSide)
        {
            if (leftSide.NodeType == ExpressionType.Call)
            {
                MethodCallExpression call = leftSide as MethodCallExpression;
                if (call.Method.Name == "CompareString")
                {
                    LambdaExpression evalRight = Expression.Lambda(call.Arguments[1], null);
                    //Compile it, invoke it, and get the resulting hash
                    return (evalRight.Compile().DynamicInvoke(null));
                }
            }
            //rightside is where we get our hash...
            switch (rightSide.NodeType)
            {
                //shortcut constants, dont eval, will be faster
                case ExpressionType.Constant:
                    ConstantExpression constExp
                        = (ConstantExpression)rightSide;
                    return (constExp.Value);

                //if not constant (which is provably terminal in a tree), convert back to Lambda and eval to get the hash.
                default:
                    //Lambdas can be created from expressions... yay
                    LambdaExpression evalRight = Expression.Lambda(rightSide, null);
                    //Compile and invoke it, and get the resulting hash
                    return (evalRight.Compile().DynamicInvoke(null));
            }
        }

        public IEnumerable<T> Where<T>(Expression<Func<T, bool>> expr) where T : IPersistIfcEntity
        {
            bool indexFound = false;
            Type type = typeof(T);
            IfcType ifcType = IfcMetaData.IfcType(type);
           
            Func<T, bool> predicate = expr.Compile();
            if (ifcType.HasIndexedAttribute) //we can use a secondary index to look up
            {
                //our indexes work from the hash values of that which is indexed, regardless of type
                object hashRight = null;

                //indexes only work on equality expressions here
                //this  matches "Property" = "Value"
                if (expr.Body.NodeType == ExpressionType.Equal)
                {
                    //Equality is a binary expression
                    BinaryExpression binExp = (BinaryExpression)expr.Body;
                    //Get some aliases for either side
                    Expression leftSide = binExp.Left;
                    Expression rightSide = binExp.Right;

                    hashRight = GetRight(leftSide, rightSide);

                    //if we were able to create a hash from the right side (likely)
                    MemberExpression returnedEx = GetIndexablePropertyOnLeft<T>(leftSide);
                    if (returnedEx != null)
                    {
                        //cast to MemberExpression - it allows us to get the property
                        MemberExpression propExp = returnedEx;
                        
                        if (ifcType.IndexedProperties.Contains(propExp.Member)) //we have a primary key match
                        {
                            IPersistIfcEntity entity = hashRight as IPersistIfcEntity;
                            if (entity != null)
                            {
                                indexFound = true;
                                foreach (var item in OfType<T>(true, Math.Abs(entity.EntityLabel)))
                                {
                                    if (predicate(item))
                                        yield return item;
                                }
                            }
                        }
                    }
                }
                else if (expr.Body.NodeType == ExpressionType.Call)
                {
                    MethodCallExpression callExp = (MethodCallExpression)expr.Body;
                    if (callExp.Method.Name == "Contains")
                    {
                        Expression keyExpr = callExp.Arguments[0];
                        if (keyExpr.NodeType == ExpressionType.Constant)
                        {
                            ConstantExpression constExp = (ConstantExpression)keyExpr;
                            object key = constExp.Value;
                            if (callExp.Object.NodeType == ExpressionType.MemberAccess)
                            {
                                MemberExpression memExp = (MemberExpression)callExp.Object;
                                PropertyInfo pInfo = (PropertyInfo)(memExp.Member);
                                if (ifcType.IndexedProperties.Contains(pInfo, comparePropInfo)) //we have a primary key match
                                {
                                    IPersistIfcEntity entity = key as IPersistIfcEntity;
                                    if (entity != null)
                                    {
                                        indexFound = true;
                                        foreach (var item in OfType<T>(true, Math.Abs(entity.EntityLabel)))
                                        {
                                            if (predicate(item))
                                                yield return item;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
               
            }

            //we cannot optimise so just do it
            if (!indexFound)
            {
                foreach (var item in OfType<T>(true))
                {
                    if (predicate(item)) 
                        yield return item;
                }
            }
        }

       
        #endregion

      

        



        public XbimGeometryData GetGeometry(IfcProduct product, XbimGeometryType geomType)
        {
           
            XbimGeometryCursor geomTable = GetGeometryTable();
            using (var transaction = geomTable.BeginReadOnlyTransaction())
            {
                return geomTable.GeometryData(product, geomType);
            }
        }


        /// <summary>
        /// Iterates over all the shape geoemtry
        /// This is a thread safe operation and can be accessed in background threads
        /// </summary>
        /// <param name="ofType"></param>
        /// <returns></returns>
        public IEnumerable<XbimGeometryData> Shapes(XbimGeometryType ofType)
        {
            //Get a cached or open a new Table
            XbimGeometryCursor geometryTable = GetGeometryTable();
            foreach (var shape in geometryTable.Shapes(ofType))
                yield return shape;
            FreeTable(geometryTable);
        }

        internal long GeometriesCount()
        {
            var geomTable = GetGeometryTable();
            try
            {
                return geomTable.RetrieveCount();
            }
            finally
            {
                FreeTable(geomTable);
            }
        }

      

        internal T InsertCopy<T>(T toCopy, XbimInstanceHandleMap mappings, bool includeInverses) where T : IPersistIfcEntity
        {
            XbimInstanceHandle toCopyHandle;
            if (mappings.TryGetValue(toCopy.GetHandle(), out toCopyHandle))
                return (T)this.GetInstance(toCopyHandle);
           
            IfcType ifcType = IfcMetaData.IfcType(toCopy);
            int copyLabel = Math.Abs(toCopy.EntityLabel);
            XbimInstanceHandle copyHandle = InsertNew(ifcType.Type);
            mappings.Add(toCopyHandle, copyHandle);
            if (typeof(IfcCartesianPoint) == ifcType.Type || typeof(IfcDirection) == ifcType.Type)//special cases for cartesian point and direction for efficiency
            {
                IPersistIfcEntity v = (IPersistIfcEntity)Activator.CreateInstance(ifcType.Type, new object[] { toCopy });  
                return (T)v;
            }
            else
            {
                
                IPersistIfcEntity theCopy = (IPersistIfcEntity)Activator.CreateInstance(copyHandle.EntityType);
                theCopy.Bind(_model, copyHandle.EntityLabel);
                IfcRoot rt = theCopy as IfcRoot;
                IEnumerable<IfcMetaProperty> props = ifcType.IfcProperties.Values.Where(p => !p.IfcAttribute.IsDerivedOverride);
                if (includeInverses)
                    props = props.Union(ifcType.IfcInverses);
                foreach (IfcMetaProperty prop in props)
                {
                    if (rt != null && prop.PropertyInfo.Name == "OwnerHistory") //don't add the owner history in as this will be changed later
                        continue;
                    object value = prop.PropertyInfo.GetValue(toCopy, null);
                    if (value != null)
                    {
                        bool isInverse = (prop.IfcAttribute.Order == -1); //don't try and set the values for inverses
                        Type theType = value.GetType();
                        //if it is an express type or a value type, set the value
                        if (theType.IsValueType || typeof(ExpressType).IsAssignableFrom(theType))
                        {
                            prop.PropertyInfo.SetValue(theCopy, value, null);
                        }
                        //else 
                        else if (!isInverse && typeof(IPersistIfcEntity).IsAssignableFrom(theType))
                        {
                            prop.PropertyInfo.SetValue(theCopy, InsertCopy((IPersistIfcEntity)value, mappings, includeInverses), null);
                        }
                        else if (!isInverse && typeof(ExpressEnumerable).IsAssignableFrom(theType))
                        {
                            Type itemType = theType.GetItemTypeFromGenericType();

                            ExpressEnumerable copyColl;
                            if (!theType.IsGenericType) //we have a class that inherits from a generic type
                                copyColl = (ExpressEnumerable)Activator.CreateInstance(theType, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { theCopy }, null);
                            else
                            {
                                Type genericType = theType.GetGenericTypeDefinition();
                                Type gt = genericType.MakeGenericType(new Type[] { itemType });
                                copyColl = (ExpressEnumerable)Activator.CreateInstance(gt, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { theCopy }, null);
                            }
                            prop.PropertyInfo.SetValue(theCopy, copyColl, null);
                            foreach (var item in (ExpressEnumerable)value)
                            {
                                Type actualItemType = item.GetType();
                                if (actualItemType.IsValueType || typeof(ExpressType).IsAssignableFrom(actualItemType))
                                    copyColl.Add(item);
                                else if (typeof(IPersistIfcEntity).IsAssignableFrom(actualItemType))
                                    copyColl.Add(InsertCopy((IPersistIfcEntity)item, mappings, includeInverses));
                                else
                                    throw new XbimException(string.Format("Unexpected collection item type ({0}) found", itemType.Name));
                            }
                        }
                        else if (isInverse && value is IEnumerable<IPersistIfcEntity>) //just an enumeration of IPersistIfcEntity
                        {
                            foreach (var ent in (IEnumerable<IPersistIfcEntity>)value)
                                InsertCopy(ent, mappings, includeInverses);
                        }
                        else if (isInverse && value is IPersistIfcEntity) //it is an inverse and has a single value
                            InsertCopy((IPersistIfcEntity)value, mappings, includeInverses);
                        else
                            throw new XbimException(string.Format("Unexpected item type ({0})  found", theType.Name));
                    }
                }
              //  if (rt != null) rt.OwnerHistory = this.OwnerHistoryAddObject;
                return (T)theCopy;
            }
        }

        private IPersistIfcEntity GetInstance(XbimInstanceHandle map)
        {
            return GetInstance(map.EntityLabel);
        }


        private XbimInstanceHandle InsertNew(Type type)
        {

            XbimEntityCursor table = GetEntityTable();

            try
            {
                using (var txn = table.BeginLazyTransaction())
                {
                    XbimInstanceHandle handle = table.AddEntity(type);
                    txn.Commit();
                    return handle;
                }

            }
            catch (Exception)
            {
                throw;
            }
            finally
            {

                FreeTable(table);
            }
            
        }

        private int NextLabel()
        {
            return HighestLabel + 1;
        }

        /// <summary>
        /// Adds an entity to the modified cache, if the entity is not already being edited
        /// Throws an exception if an attempt is made to edit a duplicate reference to the entity
        /// </summary>
        /// <param name="entity"></param>
        internal void AddModified(IPersistIfcEntity entity)
        {
            IPersistIfcEntity editing;
            if (modified.TryGetValue(entity.EntityLabel, out editing)) //it  already exists as edited
            {
                if (!System.Object.ReferenceEquals(editing, entity)) //it is not the same object reference
                    throw new XbimException("An attempt to edit a duplicate reference for #" + entity.EntityLabel + " error has occurred");
            }
            else
                modified.Add(entity.EntityLabel, entity);
        }

        public string DatabaseName 
        {
            get
            {
                return _databaseName;
            }
        }

        /// <summary>
        /// Returns an enumeration of all the instance labels in the model
        /// </summary>
        public IEnumerable<int> InstanceLabels 
        {
            get
            {
                var entityTable = GetEntityTable();
                try
                {
                    
                    int label;
                    if (entityTable.TryMoveFirstLabel(out label)) // we have something
                    {
                        do
                        {
                            yield return label;
                        }
                        while (entityTable.TryMoveNextLabel(out label));
                    }
                }
                finally
                {
                    FreeTable(entityTable);
                }
            }
        }


        /// <summary>
        /// Clears any cached objects and starts a new caching session
        /// </summary>
        internal void  BeginCaching()
        {
            read.Clear();
            modified.Clear();
            caching = true;

        }
        /// <summary>
        /// Clears any cached objects and terminates further caching
        /// </summary>
        internal void  EndCaching()
        {
            read.Clear();
            modified.Clear();
            caching = false;
        }

        /// <summary>
        /// Writes the content of the modified cache to the table, assumes a transaction is in scope
        /// </summary>
        internal void Write(XbimEntityCursor entityTable)
        {
            foreach (var entity in modified.Values)
            {
                entityTable.UpdateEntity(entity);
            }
        }



        public static bool HasDatabaseInstance
        {
            get
            {
                return _jetInstance != null;
            }
        }

        internal static void Compact(string sourceFileName, string targetFileName)
        {
            using (Session sess = new Session(_jetInstance))
            {
                Api.JetAttachDatabase(sess, sourceFileName, AttachDatabaseGrbit.ReadOnly);
                try
                {
                    Api.JetCompact(sess, sourceFileName, targetFileName, null, null, CompactGrbit.None);
                }
                finally
                {
                    Api.JetDetachDatabase(sess, sourceFileName);
                }

            }

        }

        internal static void Terminate()
        {
            try
            {
                if (_jetInstance != null)
                    _jetInstance.Term();
            }
            finally
            {
                 _jetInstance = null;
            }
           
        }

        internal static void Initialize()
        {            
             if (_jetInstance == null)
                 _jetInstance = CreateInstance("XbimInstance", XbimModel.XbimTempDirectory);
        }

        internal IEnumerable<IPersistIfcEntity> Modified()
        {
            return modified.Values;
        }

     
    }

}


