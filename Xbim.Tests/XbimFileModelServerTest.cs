﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xbim.IO;
using Xbim.Ifc.Extensions;
using Xbim.XbimExtensions.Parser;
using System.IO;
using System.Reflection;
using Xbim.XbimExtensions;
using System.Collections;
using System.Globalization;
using System.Threading;
using Xbim.Ifc.GeometryResource;

namespace Xbim.Tests
{
    // Deploy our sample files to the correct TestResults folder, so we can access under the Test Runner
    [DeploymentItem(TestSourceFileIfc, Root)]
    [DeploymentItem(TestSourceFileIfcXml, Root)]
    [DeploymentItem(TestSourceFileXbim, Root)]
    [TestClass]
    public class XbimFileModelServerTest
    {
        const string Root = "TestSourceFiles";
        const string Temp = "Temp";

        // 3 reference files, under version control
        const string TestSourceFileIfc = Root + "/Ref.ifc";
        const string TestSourceFileIfcXml = Root + "/Ref_Xbim_IfcXml.ifcxml";
        const string TestSourceFileXbim = Root + "/Ref_Xbim.xbim";

        #region "Init and Cleanup"

        [ClassInitialize]
        public static void LoadModel(TestContext context)
        {
            Directory.CreateDirectory(Path.Combine(Root, Temp));
        }

        [TestInitialize]
        public void TestInit()
        {
            // this method runs before start of every test
            // any init code
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // this method runs after finishes every test

        }

        #endregion

        #region "Tests: Converts ifc reference file to various formats successfully"

        [TestMethod]
        public void Test_01_CreateTestSuite()
        {
            // This method expects a Ref.ifc file to be converted to various formats in "TestSourceFiles" folder
            // Original Ref.ifc -> Ref_Xbim.xbim -> Ref_Xbim_IfcXml.ifcxml -> Ref_Xbim_IfcXml_Xbim.xbim
            // -> Ref_Xbim_IfcXml_Xbim_Ifc.ifc -> Ref_Xbim_IfcXml_Xbim_ifc_Xbim.xbim

            // By the end of all conversion we will have 3 xbim files. Compare each file with other 2 and they all
            // should have same binary data for each entity.

            string Ref = Root + "/Ref.ifc";
            string Ref_Xbim = Root + "/Ref_Xbim.xbim";
            string Ref_Xbim_IfcXml = Root + "/Ref_Xbim_IfcXml.ifcxml";
            string Ref_Xbim_IfcXml_Xbim = Root + "/Ref_Xbim_IfcXml_Xbim.xbim";
            string Ref_Xbim_IfcXml_Xbim_Ifc = Root + "/Ref_Xbim_IfcXml_Xbim_Ifc.ifc";
            string Ref_Xbim_IfcXml_Xbim_ifc_Xbim = Root + "/Ref_Xbim_IfcXml_Xbim_ifc_Xbim.xbim";

            // create reference xbim file from original ifc
            CreateXbimFile(Ref, Ref_Xbim);

            // create ref ifcxml file from previous xbim
            CreateIfcXmlFile(Ref_Xbim, Ref_Xbim_IfcXml);

            // previous ifcxml to new xbim
            CreateXbimFile(Ref_Xbim_IfcXml, Ref_Xbim_IfcXml_Xbim);

            CreateIfcFile(Ref_Xbim_IfcXml_Xbim, Ref_Xbim_IfcXml_Xbim_Ifc);

            CreateXbimFile(Ref_Xbim_IfcXml_Xbim_Ifc, Ref_Xbim_IfcXml_Xbim_ifc_Xbim);

            // compare xbim files
            CompareXbimFiles(Ref_Xbim, Ref_Xbim_IfcXml_Xbim);
            CompareXbimFiles(Ref_Xbim_IfcXml_Xbim, Ref_Xbim_IfcXml_Xbim_ifc_Xbim);
            CompareXbimFiles(Ref_Xbim_IfcXml_Xbim_ifc_Xbim, Ref_Xbim);
            Assert.IsTrue(true);
        }

        #endregion

        #region "Tests: Convert files from one format to another"

        [TestMethod]
        public void Test_ConvertIfcToXbim()
        {
            string ifcFileName = TestSourceFileIfc;
            string xbimFileName = CreateXbimFile(ifcFileName);

            // check if file is created 
            bool fileExist = File.Exists(xbimFileName);
            Assert.IsTrue(fileExist);

            // check file created is not blank
            byte[] data = File.ReadAllBytes(xbimFileName);
            bool isValidFile = (data.Length > 0);
            Assert.IsTrue(isValidFile);

            // check the created xbim file is same as reference xbim file
            // get any labels in xbimFile1 that are not in xbimFile2
            List<long> badLabels1 = GetFileBadLabels(xbimFileName, TestSourceFileXbim);

            // get any labels in xbimFile2 that are not in xbimFile1
            List<long> badLabels2 = GetFileBadLabels(TestSourceFileXbim, xbimFileName);

            // both files are created using different procedures from same ifc, so should be same and have 0 badlabels
            Assert.AreEqual(badLabels1.Count, 0);
            Assert.AreEqual(badLabels2.Count, 0);

            
        }

        [TestMethod]
        public void Test_ConvertIfcXmlToXbim()
        {
            string ifcXmlFileName = TestSourceFileIfcXml;
            string xbimFileName = CreateXbimFile(ifcXmlFileName);

            // check if file is created 
            bool fileExist = File.Exists(xbimFileName);
            Assert.IsTrue(fileExist);

            // check file created is not blank
            byte[] data = File.ReadAllBytes(xbimFileName);
            bool isValidFile = (data.Length > 0);
            Assert.IsTrue(isValidFile);

            // check the created xbim file is same as reference xbim file
            // get any labels in xbimFile1 that are not in xbimFile2
            List<long> badLabels1 = GetFileBadLabels(xbimFileName, TestSourceFileXbim);

            // get any labels in xbimFile2 that are not in xbimFile1
            List<long> badLabels2 = GetFileBadLabels(TestSourceFileXbim, xbimFileName);

            // both files are created using different procedures from same ifc, so should be same and have 0 badlabels
            Assert.AreEqual(badLabels1.Count, 0);
            Assert.AreEqual(badLabels2.Count, 0);

            
        }

        [TestMethod]
        public void Test_ConvertXbimToIfcXml()
        {
            string xbimFileName = TestSourceFileXbim;
            string ifcXmlFileName = CreateIfcXmlFile(xbimFileName);

            // check if file is created 
            bool fileExist = File.Exists(ifcXmlFileName);
            Assert.IsTrue(fileExist);

            // check file created is not blank
            byte[] data = File.ReadAllBytes(ifcXmlFileName);
            bool isValidFile = (data.Length > 0);
            Assert.IsTrue(isValidFile);

            
        }

        [TestMethod]
        public void Test_ConvertXbimToIfc()
        {
            string xbimFileName = TestSourceFileXbim;
            string ifcFileName = CreateIfcFile(xbimFileName);

            // check if file is created 
            bool fileExist = File.Exists(ifcFileName);
            Assert.IsTrue(fileExist);

            // check file created is not blank
            byte[] data = File.ReadAllBytes(ifcFileName);
            bool isValidFile = (data.Length > 0);
            Assert.IsTrue(isValidFile);

            
        }

        [TestMethod]
        public void Test_ConvertIfcxmlToIfc()
        {
            string ifcXmlFileName = TestSourceFileIfcXml;
            string ifcFileName = CreateIfcFile(ifcXmlFileName);

            // check if file is created 
            bool fileExist = File.Exists(ifcXmlFileName);
            Assert.IsTrue(fileExist);

            // check file created is not blank
            byte[] data = File.ReadAllBytes(ifcXmlFileName);
            bool isValidFile = (data.Length > 0);
            Assert.IsTrue(isValidFile);


        }

        [TestMethod]
        public void Test_ConvertXbimToGeometryCache()
        {
            string geomFileName = CreateGeometryCache(TestSourceFileXbim);

            // check if file is created 
            bool fileExist = File.Exists(geomFileName);
            Assert.IsTrue(fileExist);

            // check file created is not blank
            byte[] data = File.ReadAllBytes(geomFileName);
            bool isValidFile = (data.Length > 0);
            Assert.IsTrue(isValidFile);


        }

        [TestMethod]
        public void Test_ConvertXbimToSemanticContent()
        {
            string nonGeomFileName = CreateSemanticContent(TestSourceFileXbim);

            // check if file is created 
            bool fileExist = File.Exists(nonGeomFileName);
            Assert.IsTrue(fileExist);

            // check file created is not blank
            byte[] data = File.ReadAllBytes(nonGeomFileName);
            bool isValidFile = (data.Length > 0);
            Assert.IsTrue(isValidFile);

            using(XbimFileModelServer modelServer = new XbimFileModelServer())
            {
                modelServer.Open(nonGeomFileName);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public void Importing_Missing_File_Throws_FileNotFoundException()
        {
            using (XbimFileModelServer modelServer = new XbimFileModelServer())
            {
                modelServer.ImportIfc("DoesNotExist.ifc");
            }
        }

        [TestMethod]
        [ExpectedException(typeof(DirectoryNotFoundException))]
        public void Importing_From_Missing_Folder_Throws_DirectoryNotFoundException()
        {
            using (XbimFileModelServer modelServer = new XbimFileModelServer())
            {
                modelServer.ImportIfc("/BadPath/DoesNotExist.ifc");
            }
        }

        #endregion

        #region "Tests: Open and close files of different formats using FileModelServer and MemoryModel"

        [TestMethod]
        public void Test_OpenIfcXml_FileModel()
        {
            using (IModel model = new XbimFileModelServer())
            {
                model.Open(TestSourceFileIfcXml);
            }
        }

        [TestMethod]
        public void Test_OpenIfc_FileModel()
        {
            using (IModel model = new XbimFileModelServer())
            {
                model.Open(TestSourceFileIfc);
            }
        }

        [TestMethod]
        public void Test_OpenXbim_FileModel()
        {
            using (IModel model = new XbimFileModelServer())
            {
                model.Open(TestSourceFileXbim);
            }
        }

        [TestMethod]
        public void Test_OpenIfcXml_MemoryModel()
        {
            using (IModel model = new XbimMemoryModel())
            {
                model.Open(TestSourceFileIfcXml);
            }
        }

        [TestMethod]
        public void Test_OpenIfc_MemoryModel()
        {
            IModel model = new XbimMemoryModel();
            model.Open(TestSourceFileIfc);
            model.Close();
        }

        [TestMethod]
        public void Test_OpenXbim_MemoryModel()
        {
            using (IModel model = new XbimMemoryModel())
            {
                model.Open(TestSourceFileXbim);
            }
        }
#endregion

        #region "Private Methods"

            private void CompareXbimFiles(string source, string target)
            {
                List<long> badLabels1_3 = GetFileBadLabels(source, target);
                // get any labels in xbimFile2 that are not in xbimFile1
                List<long> badLabels2_3 = GetFileBadLabels(target, source);
                // both files are created using different procedures from same ifc, so should be same and have 0 badlabels
                Assert.AreEqual(badLabels1_3.Count, 0);
                Assert.AreEqual(badLabels2_3.Count, 0);
            }

        

            private static string CreateTempFile(string baseDir, string ext)
            {
                return Path.Combine(Path.Combine(baseDir, Temp), (Path.GetRandomFileName() + ext));

            }

            private string CreateSemanticContent(string xbimSourceFile)
            {
                string xbimFilePath = Path.GetDirectoryName(xbimSourceFile);

                string nonGeomXbimFileName = CreateTempFile(xbimFilePath, ".xbim");

                // create xbim semantic file without geometry
                using (XbimFileModelServer modelServer = new XbimFileModelServer())
                {
                    modelServer.Open(xbimSourceFile);
                    modelServer.ExtractSemantic(nonGeomXbimFileName, XbimStorageType.XBIM, null);
                }
                return nonGeomXbimFileName;
            }


            private string CreateGeometryCache(string xbimSourceFile)
            {
                string xbimFilePath = Path.GetDirectoryName(xbimSourceFile);
                string geomXbimFileName = CreateTempFile(xbimFilePath, ".xbimGC");

                // create xbim geometry file
                using (XbimFileModelServer modelServer = new XbimFileModelServer())
                {
                    modelServer.Open(xbimSourceFile);
                    modelServer.ExtractSemantic(geomXbimFileName, XbimStorageType.XBIM, null);
                }

                return geomXbimFileName;
            }

            private static string CreateXbimFile(string sourceFilePath, string targetPath = null)
            {
                string ext = Path.GetExtension(sourceFilePath);

                if (ext.ToLower() == ".ifc")
                {
                    string ifcFilePath = Path.GetDirectoryName(sourceFilePath);
                    string xbimFileName = CreateTempFile(ifcFilePath, ".xbim");
                    if (targetPath != null)
                        xbimFileName = targetPath;
                    using (XbimFileModelServer modelServer = new XbimFileModelServer())
                    {
                        modelServer.ImportIfc(sourceFilePath, xbimFileName);
                    }
                    return xbimFileName;
                }
                else if (ext.ToLower() == ".ifcxml" || ext.ToLower() == ".xml")
                {
                    string ifcxmlFilePath = Path.GetDirectoryName(sourceFilePath);
                    string xbimFileName = CreateTempFile(ifcxmlFilePath, ".xbim"); 
                    if (targetPath != null)
                        xbimFileName = targetPath;
                    using (XbimFileModelServer modelServer = new XbimFileModelServer())
                    {
                        modelServer.ImportXml(sourceFilePath, xbimFileName);
                    }
                    return xbimFileName;
                }
                return "";
            }

            private static string CreateIfcXmlFile(string sourceFilePath, string targetPath = null)
            {
                string ext = Path.GetExtension(sourceFilePath);

                if (ext.ToLower() == ".ifc")
                {
                    //string ifcFilePath = Path.GetDirectoryName(sourceFilePath);
                    //string xbimFileName = ifcFilePath + "\\Temp\\" + Path.GetRandomFileName() + ".xbim";
                    //XbimFileModelServer modelServer = new XbimFileModelServer();
                    //modelServer.ImportIfc(sourceFilePath, xbimFileName);
                    //modelServer.Close();
                    //modelServer.Dispose();

                    //// now use the xbim file to create ifcXml file
                    //string ifcXmlFileName = ifcFilePath + "\\Temp\\" + Path.GetRandomFileName() + ".ifcxml";
                    //modelServer = new XbimFileModelServer(xbimFileName, FileAccess.ReadWrite);
                    //modelServer.Export(XbimStorageType.IFCXML, ifcXmlFileName);
                    //modelServer.Close();
                    //modelServer.Dispose();

                    //return ifcXmlFileName;
                }
                else if (ext.ToLower() == ".xbim")
                {
                    string xbimFilePath = Path.GetDirectoryName(sourceFilePath);
                    string ifcXmlFileName = CreateTempFile(xbimFilePath, ".ifcxml"); 
                    if (targetPath != null)
                        ifcXmlFileName = targetPath;
                    using (XbimFileModelServer modelServer = new XbimFileModelServer(sourceFilePath, FileAccess.ReadWrite))
                    {
                        modelServer.Export(XbimStorageType.IFCXML, ifcXmlFileName);
                    }
                    return ifcXmlFileName;
                }
                return "";
            }

            private static string CreateIfcFile(string sourceFilePath, string targetPath = null)
            {
                string ext = Path.GetExtension(sourceFilePath);

                if (ext.ToLower() == ".ifcxml" || ext.ToLower() == ".xml")
                {
                    string ifcFilePath = Path.GetDirectoryName(sourceFilePath);
                    string ifcFileName = CreateTempFile(ifcFilePath, ".ifc"); 
                    string xbimFileName = CreateTempFile(ifcFilePath, ".xbim");

                    // convert xml to xbim first
                    using (XbimFileModelServer modelServer = new XbimFileModelServer())
                    {
                        modelServer.ImportXml(sourceFilePath, xbimFileName);
                    }
                    // export temp xbim to ifc
                    using (XbimFileModelServer modelServer = new XbimFileModelServer(xbimFileName, FileAccess.ReadWrite))
                    {
                        modelServer.ExportIfc(ifcFileName);
                    }
                    return ifcFileName;
                }
                else if (ext.ToLower() == ".xbim")
                {
                    string xbimFilePath = Path.GetDirectoryName(sourceFilePath);
                    string ifcFileName = CreateTempFile(xbimFilePath, ".ifc"); 
                    if (targetPath != null)
                        ifcFileName = targetPath;
                    using (XbimFileModelServer modelServer = new XbimFileModelServer(sourceFilePath, FileAccess.ReadWrite))
                    {
                        modelServer.Export(XbimStorageType.IFC, ifcFileName);
                    }
                    return ifcFileName;
                }
                return "";
            }

            private List<long> GetFileBadLabels(string fileName1, string fileName2)
            {
                List<long> badLabels = new List<long>();

                using (XbimFileModelServer modelServer = new XbimFileModelServer())
                {
                    modelServer.Open(fileName1);
                    
                    using (XbimFileModelServer modelServer2 = new XbimFileModelServer())
                    {
                        modelServer2.Open(fileName2);
                        foreach (XbimIndexEntry xi in modelServer.EntityOffsets)
                        {
                            IPersistIfcEntity entity = modelServer.GetInstance(xi.EntityLabel);
                            long entityLabel = entity.EntityLabel;
                            byte[] b1 = modelServer.GetEntityBinaryData(entity);

                            long posLabel = Math.Abs(entityLabel);

                            // we have entityLabel from 1st file, this should be in second file as well
                            IPersistIfcEntity entity2 = null;
                            bool isBadEntity = false;
                            entity2 = modelServer2.GetInstance(entityLabel); //GetInstance(modelServer2, entityLabel); //modelServer2.GetInstance(entityLabel);

                            if (entity2 != null)
                            {
                                byte[] b2 = modelServer2.GetEntityBinaryData(entity2);


                                IStructuralEquatable eqb1 = b1;
                                bool isEqual = eqb1.Equals(b2, StructuralComparisons.StructuralEqualityComparer);
                                if (!isEqual)
                                {
                                    // they may be equal but showing unequal because of decimal precision
                                    // check if its decimal and if yes, then lower the precision and compare

                                    //if (!CompareBytes(modelServer, entity, b1, b2))
                                    isBadEntity = true;
                                    throw new Exception("Entity mismatch: EntityLabel: " + entityLabel + " \n" + b1.ToString() + " \n" + b2.ToString());
                                }
                            }
                            else
                            {
                                // file1 entity dosent exist in file2
                                isBadEntity = true;

                            }
                            if (isBadEntity)
                            {
                                // add label to badLabels List
                                badLabels.Add(posLabel);
                            }
                        }
                    } // end modelServer2 using
                } // end modelServer using
                return badLabels;
            }

        #endregion
    }
}