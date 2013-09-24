﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.SharedComponentElements;
using Xbim.Ifc2x3.StructuralElementsDomain;
using Xbim.Ifc2x3.SharedBldgServiceElements;
using Xbim.Ifc2x3.HVACDomain;
using Xbim.Ifc2x3.ElectricalDomain;
using Xbim.XbimExtensions.Interfaces;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;
using Xbim.COBie.Data;

namespace Xbim.COBie
{
	/// <summary>
	/// Context for generating COBie data from one or more IFC models
	/// </summary>
    public class COBieContext : ICOBieContext 
	{

        //Worksheet Global
        public Dictionary<long, string> EMails { get; private set; } 
        public string TemplateFileName { get; set; } //template used by the workbook
        public string RunDate { get; set; } //Date the Workbook was created 
        public bool ExcludeFromPickList { get; set; }
 
        private  GlobalUnits _workBookUnits;
        /// <summary>
        /// Global Units for the workbook
        /// </summary>
        public GlobalUnits WorkBookUnits
        {
            get
            {
                if (Model == null)
                {
                    throw new ArgumentException("COBieContext must contain a model before calling WorkBookUnits."); 
                }
                if (_workBookUnits == null) //set up global units
                {
                    _workBookUnits = new GlobalUnits();
                    COBieData<COBieRow>.GetGlobalUnits(Model, _workBookUnits);
                }
                return _workBookUnits;
            }
           
        }
        /// <summary>
        /// if set to true and no IfcZones or no IfcSpace property names of "ZoneName", we will list 
        /// any IfcSpace property names "Department" in the Zone sheet
        /// </summary>
        public bool DepartmentsUsedAsZones { get; set; } //indicate if we have taken departments as Zones
        public FilterValues Exclude { get; private set; } //filter values for attribute extraction in sheets
        public ErrorRowIndexBase ErrorRowStartIndex { get; set; } //set the error reporting to be either one (first row is labelled one) or two based (first row is labelled two) on the rows of the tables/excel sheet

        public COBieContext()
        {
            RunDate = DateTime.Now.ToString(Constants.DATE_FORMAT);
            EMails = new Dictionary<long, string>();
            Scene = null;
            Model = null;
            //if no IfcZones or no IfcSpace property names of "ZoneName" then if DepartmentsUsedAsZones is true we will list 
            //any IfcSpace property names "Department" in the Zone sheet and remove the "Department" property from the attribute sheet
            DepartmentsUsedAsZones = false;

            Exclude = new FilterValues();

            ExcludeFromPickList = false;

            //set the row index to report error rows on
            ErrorRowStartIndex = ErrorRowIndexBase.Two; //default for excel sheet
            
        }

        public COBieContext(ReportProgressDelegate progressHandler = null) : this() 
		{
            if (progressHandler != null)
            {
                _progress = progressHandler;
                this.ProgressStatus += progressHandler;
            }
		}

		/// /// <summary>
        /// Gets the model defined in this context to generate COBie data from
        /// </summary>
        public XbimModel Model { get; set; }
        

		/// <summary>
		/// The pick list to use to cross-reference fields in the COBie worksheets
		/// </summary>
		//public COBiePickList PickList { get; set; }
        public IXbimScene Scene  { get; set; }
       

        private ReportProgressDelegate _progress = null;

        public event ReportProgressDelegate ProgressStatus;

        /// <summary>
        /// Updates the delegates with the current percentage complete
        /// </summary>
        /// <param name="message"></param>
        /// <param name="total"></param>
        /// <param name="current"></param>
        public void UpdateStatus(string message, int total = 0, int current = 0)
        {
            decimal percent = 0;
            if (total != 0 && current > 0)
            {
                message = string.Format("{0} [{1}/{2}]", message, current, total);
                percent = (decimal)current / total * 100;
            }
            if(ProgressStatus != null)
                ProgressStatus((int)percent, message);
        }

        public void Dispose()
        {
            if (Scene != null)
            {
                Scene.Close();
                Scene = null;
            }

            if (_progress != null)
            {
                ProgressStatus -= _progress;
                _progress = null;
            }
        }
    }

    /// <summary>
    /// Index for the rows the errors are reported on, either one based (first row is labelled one) (Data Table etc...) 
    /// or two based (first row is labelled two) (Excel sheets)
    /// </summary>
    public enum ErrorRowIndexBase
    {
        One,
        Two
    }

    /// <summary>
    /// Global units
    /// </summary>
    public class GlobalUnits 
    {
        public string LengthUnit { get; set; }
        public string AreaUnit { get; set; }
        public string VolumeUnit { get; set; }
        public string MoneyUnit { get; set; }
    }

    public interface ICOBieContext : IDisposable
    {
        void UpdateStatus(string message, int total = 0, int current = 0);
    }
}
