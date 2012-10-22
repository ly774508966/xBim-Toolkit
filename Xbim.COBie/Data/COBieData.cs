﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc.UtilityResource;
using Xbim.Ifc.MeasureResource;
using Xbim.XbimExtensions;
using Xbim.Ifc.ActorResource;
using Xbim.COBie.Rows;
using Xbim.Ifc.PropertyResource;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.Extensions;
using Xbim.Ifc.ExternalReferenceResource;
using Xbim.Ifc.SelectTypes;
using Xbim.Ifc.ProductExtension;
using Xbim.Ifc.SharedBldgElements;
using Xbim.Ifc.HVACDomain;
using Xbim.Ifc.ElectricalDomain;
using Xbim.Ifc.SharedComponentElements;
using Xbim.Ifc.StructuralElementsDomain;
using Xbim.Ifc.SharedBldgServiceElements;
using System.Globalization;
using Xbim.COBie.Resources;


namespace Xbim.COBie.Data
{
    /// <summary>
    /// Base class for the input of data into the Excel worksheets
    /// </summary>
    public abstract class COBieData<T> where T : COBieRow
    {

        protected const string DEFAULT_STRING = Constants.DEFAULT_STRING;
        protected const string DEFAULT_NUMERIC = Constants.DEFAULT_NUMERIC;

        protected COBieContext Context { get; set; }
        private COBieProgress _progressStatus;
        //private static Dictionary<long, string> _eMails = new Dictionary<long, string>();

        protected COBieData()
        { }

        
        public COBieData(COBieContext context)
        {
            Context = context;
            _progressStatus = new COBieProgress(context);
        }

        protected IModel Model
        {
            get
            {
                return Context.Model;
            }
        }

        protected COBieProgress ProgressIndicator
        {
            get
            {
                return _progressStatus;
            }
        }


        

        #region Methods

        public abstract COBieSheet<T> Fill();

        /// <summary>
        /// Extract the Created On date from the passed entity
        /// </summary>
        /// <param name="rootEntity">Entity to extract the Create On date</param>
        /// <returns></returns>
        protected string GetCreatedOnDateAsFmtString(IfcOwnerHistory ownerHistory)
        {
            if (ownerHistory != null)
            {
                int createdOnTStamp = (int)ownerHistory.CreationDate;
                if (createdOnTStamp != 0) //assume not set, but could it be 1970/1/1 00:00:00!!!
                {
                    //to remove trailing decimal seconds use a set format string as "o" option is to long.

                    //We have a day light saving problem with the comparison with other COBie Export Programs. if we convert to local time we get a match
                    //but if the time stamp is Coordinated Universal Time (UTC), then daylight time should be ignored. see http://msdn.microsoft.com/en-us/library/bb546099.aspx
                    //IfcTimeStamp.ToDateTime(CreatedOnTStamp).ToLocalTime()...; //test to see if corrects 1 hour difference, and yes it did, but should we?

                    return IfcTimeStamp.ToDateTime(createdOnTStamp).ToString(Constants.DATETIME_FORMAT); 
                }
                
            }
            //return default date of now
            return Context.RunDate;
        }


        /// <summary>
        /// Gets the name of the application that is linked with the supplied item
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public string GetExternalSystem(IfcRoot item)
        {
            string appName = "";
            if(item.OwnerHistory.LastModifyingApplication != null)
                appName = item.OwnerHistory.LastModifyingApplication.ApplicationFullName;
            if (string.IsNullOrEmpty(appName))
                appName = item.OwnerHistory.OwningApplication.ApplicationFullName;
            return string.IsNullOrEmpty(appName) ? DEFAULT_STRING : appName;
        }

        /// <summary>
        /// Extract the email address lists for the owner of the IfcOwnerHistory passed
        /// </summary>
        /// <param name="ifcOwnerHistory">Entity to extract the email addresses for</param>
        /// <returns>string of comma delimited addresses</returns>
        protected string GetTelecomTelephoneNumber(IfcPersonAndOrganization ifcPersonAndOrganization)
        {
            string telephoneNo = "";
            IfcOrganization ifcOrganization = ifcPersonAndOrganization.TheOrganization;
            IfcPerson ifcPerson = ifcPersonAndOrganization.ThePerson;
                
            if (ifcPerson.Addresses != null)
            {
                telephoneNo = ifcPerson.Addresses.TelecomAddresses.Select(address => address.TelephoneNumbers).Where(item => item != null).SelectMany(em => em).Where(em => !string.IsNullOrEmpty(em)).FirstOrDefault();

                if (string.IsNullOrEmpty(telephoneNo))
                {
                    if (ifcOrganization.Addresses != null)
                    {
                        telephoneNo = ifcOrganization.Addresses.TelecomAddresses.Select(address => address.TelephoneNumbers).Where(item => item != null).SelectMany(em => em).Where(em => !string.IsNullOrEmpty(em)).FirstOrDefault();
                    }
                } 
            }
            
            //if still no email lets make one up
            if (string.IsNullOrEmpty(telephoneNo)) telephoneNo = DEFAULT_STRING;
            

            return telephoneNo;
        }

        /// <summary>
        /// Clear the email dictionary for next file
        /// </summary>
        public void ClearEMails()
        {
            Context.EMails.Clear();
        }



        /// <summary>
        /// Extract the email address lists for the owner of the IfcOwnerHistory passed
        /// </summary>
        /// <param name="ifcOwnerHistory">Entity to extract the email addresses for</param>
        /// <returns>string of comma delimited addresses</returns>
        protected string GetTelecomEmailAddress(IfcOwnerHistory ifcOwnerHistory)
        {
            if (ifcOwnerHistory != null)
            {
                IfcPerson ifcPerson = ifcOwnerHistory.OwningUser.ThePerson;
                if (Context.EMails.ContainsKey(ifcPerson.EntityLabel))
                {
                    return Context.EMails[ifcPerson.EntityLabel];
                }
                else
                {
                    IfcOrganization ifcOrganization = ifcOwnerHistory.OwningUser.TheOrganization;
                    return GetEmail(ifcOrganization, ifcPerson);
                }
            }
            else
                return Constants.DEFAULT_EMAIL;
        }
        /// <summary>
        /// Extract the email address lists for the owner of the IfcOwnerHistory passed
        /// </summary>
        /// <param name="ifcOwnerHistory">Entity to extract the email addresses for</param>
        /// <returns>string of comma delimited addresses</returns>
        protected string GetTelecomEmailAddress(IfcPersonAndOrganization ifcPersonAndOrganization)
        {
            if (ifcPersonAndOrganization != null)
            {
                IfcPerson ifcPerson = ifcPersonAndOrganization.ThePerson;
                if (Context.EMails.ContainsKey(ifcPerson.EntityLabel))
                {
                    return Context.EMails[ifcPerson.EntityLabel];
                }
                else
                {
                    IfcOrganization ifcOrganization = ifcPersonAndOrganization.TheOrganization;
                    return GetEmail(ifcOrganization, ifcPerson);
                }
            }
            else
                return Constants.DEFAULT_EMAIL;
        }



        /// <summary>
        /// Get email address from IfcPerson 
        /// </summary>
        /// <param name="ifcOrganization"></param>
        /// <param name="ifcPerson"></param>
        /// <returns></returns>
        protected string GetEmail( IfcOrganization ifcOrganization, IfcPerson ifcPerson)
        {
            string email = "";
            IEnumerable<IfcLabel> emails = Enumerable.Empty<IfcLabel>();
            if ((ifcPerson != null) && (ifcPerson.Addresses != null))
            {
                emails = ifcPerson.Addresses.TelecomAddresses.Select(address => address.ElectronicMailAddresses).Where(item => item != null).SelectMany(em => em).Where(em => !string.IsNullOrEmpty(em));
                
            }
            if ((emails == null) || (emails.Count() == 0))
            {
                if ((ifcOrganization != null) && (ifcOrganization.Addresses != null))
                {
                    emails = ifcOrganization.Addresses.TelecomAddresses.Select(address => address.ElectronicMailAddresses).Where(item => item != null).SelectMany(em => em).Where(em => !string.IsNullOrEmpty(em));
                }
            }


            //if still no email lets make one up
            if ((emails != null) && (emails.Count() > 0))
            {
                email = string.Join(" : ", emails);
            }
            else
            {
                string first = "";
                string lastName = "";
                string organization = "";
                if (ifcPerson != null)
                {
                    first = ifcPerson.GivenName.ToString();
                    lastName = ifcPerson.FamilyName.ToString();
                }
                if (ifcOrganization != null)
                    organization = ifcOrganization.Name.ToString();
                string domType = "";
                if (!string.IsNullOrEmpty(first))
                {
                    string[] split = first.Split('.');
                    if (split.Length > 1) first = split[0]; //assume first
                }

                if (!string.IsNullOrEmpty(lastName))
                {
                    string[] split = lastName.Split('.');
                    if (split.Length > 1) lastName = split.Last(); //assume last
                }
                

                if (!string.IsNullOrEmpty(organization))
                {
                    string[] split = organization.Split('.');
                    int index = 1;
                    foreach (string item in split)
                    {
                        if (index == 1) 
                            organization = item; //first item always
                        else if (index < split.Length) //all the way up to the last item
                            organization += "." + item;
                        else
                            domType = "." + item; //last item assume domain type
                        index++;
                    }
                    
                }
                
                email += (string.IsNullOrEmpty(first)) ? "unknown" : first;
                email += ".";
                email += (string.IsNullOrEmpty(lastName)) ? "unknown" : lastName;
                email += "@";
                email += (string.IsNullOrEmpty(organization)) ? "unknown" : organization;
                email += (string.IsNullOrEmpty(domType)) ? ".com" : domType; 
            }
            //save to the email directory for quick retrieval
            Context.EMails.Add(ifcPerson.EntityLabel, email);

            return email;
        }

        /// <summary>
        /// Converts string to formatted string and if it fails then passes 0 back, mainly to check that we always have a number returned
        /// </summary>
        /// <param name="num">string to convert</param>
        /// <returns>string converted to a formatted string using "F2" as formatter</returns>
        protected string ConvertNumberOrDefault(string num)
        {
            double temp;

            if (double.TryParse(num, out temp))
            {
                return temp.ToString("F2"); // two decimal places
            }
            else
            {
                return DEFAULT_NUMERIC; 
            }

        }

        /// <summary>
        /// Get Category method
        /// </summary>
        /// <param name="obj">Object to try and extract method from</param>
        /// <returns></returns>
        public string GetCategory(IfcObject obj)
        {
            //Try by relationship first
           
            IfcRelAssociatesClassification ifcRAC = obj.HasAssociations.OfType<IfcRelAssociatesClassification>().FirstOrDefault();
                            
            if (ifcRAC != null)
            {
                IfcClassificationReference ifcCR = (IfcClassificationReference)ifcRAC.RelatingClassification;
                if (!string.IsNullOrEmpty(ifcCR.Location))
                    return ifcCR.Location;
                else
                    return ifcCR.Name;
                
                
            }
            //Try by PropertySet as fallback
            var query = from pSet in obj.PropertySets
                        from props in pSet.HasProperties
                        where props.Name.ToString() == "OmniClass Table 13 Category" || props.Name.ToString() == "Category Code" || props.Name.ToString() == "Omniclass Title"
                        select props.ToString().TrimEnd();
            string val = query.FirstOrDefault();

            if (!String.IsNullOrEmpty(val))
            {
                return val;
            }
            return Constants.DEFAULT_STRING;
        }

        

        /// <summary>
        /// Extract the unit name
        /// </summary>
        /// <param name="ifcUnit">ifcUnit object to get unit name from</param>
        /// <returns>string holding unit name</returns>
        public static string GetUnitName(IfcUnit ifcUnit)
        {
            string value = "";
            string sqText = "";
            string prefixUnit = "";

            if (ifcUnit is IfcSIUnit)
            {
                IfcSIUnit ifcSIUnit = ifcUnit as IfcSIUnit;

                prefixUnit = (ifcSIUnit.Prefix != null) ? ifcSIUnit.Prefix.ToString() : "";  //see IfcSIPrefix
                value = ifcSIUnit.Name.ToString();                                             //see IfcSIUnitName

                if (!string.IsNullOrEmpty(value))
                {
                    if (value.Contains("_"))
                    {
                        string[] split = value.Split('_');
                        if (split.Length > 1) sqText = split.First(); //see if _ delimited value such as SQUARE_METRE
                        value = sqText + prefixUnit + split.Last(); //combine to give full unit name 
                    }
                    else
                        value = prefixUnit + value; //combine to give length name
                }

            }
            else if (ifcUnit is IfcConversionBasedUnit)
            {
                IfcConversionBasedUnit IfcConversionBasedUnit = ifcUnit as IfcConversionBasedUnit;
                value = (IfcConversionBasedUnit.Name != null) ? IfcConversionBasedUnit.Name.ToString() : "";

                if (!string.IsNullOrEmpty(value))
                {
                    if (value.Contains("_"))
                    {
                        string[] split = value.Split('_');
                        if (split.Length > 1) sqText = split.First(); //see if _ delimited value such as SQUARE_METRE
                        value = sqText + split.Last(); //combine to give full unit name 
                    }
                }
            }
            else if (ifcUnit is IfcContextDependentUnit)
            {
                IfcContextDependentUnit ifcContextDependentUnit = ifcUnit as IfcContextDependentUnit;
                value = ifcContextDependentUnit.Name;
                if (string.IsNullOrEmpty(value)) //fall back to UnitType enumeration
                    value = ifcContextDependentUnit.UnitType.ToString();
            }
            else if (ifcUnit is IfcDerivedUnit)
            {
                IfcDerivedUnit ifcDerivedUnit = ifcUnit as IfcDerivedUnit;
                value = ifcDerivedUnit.UnitType.ToString();
                if ((string.IsNullOrEmpty(value)) && (ifcDerivedUnit.UserDefinedType != null)) //fall back to user defined
                    value = ifcDerivedUnit.UserDefinedType;
            }
            else if (ifcUnit is IfcMonetaryUnit)
            {
                value = GetMonetaryUnitName(ifcUnit as IfcMonetaryUnit);
                return (string.IsNullOrEmpty(value)) ? DEFAULT_STRING : value; //don't want to lower case so return here
            }
            value = (string.IsNullOrEmpty(value)) ? DEFAULT_STRING : value.ToLower();

            //check for unit spelling on meter/metre
            if (value.Contains("metre") || value.Contains("meter"))
            {
                string culturemetre = ErrorDescription.meter;
                if (!string.IsNullOrEmpty(culturemetre))
                    if (value.Contains("metre"))
                        value = value.Replace("metre", culturemetre);
                    else
                        value = value.Replace("meter", culturemetre);
                    
            }
            return value;
        }

        /// <summary>
        /// Get Monetary Unit
        /// </summary>
        /// <param name="ifcMonetaryUnit">IfcMonetaryUnit object</param>
        /// <returns>string holding the Monetary Unit</returns>
        private static string GetMonetaryUnitName(IfcMonetaryUnit ifcMonetaryUnit)
        {
            string value = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
               .Where(c => new RegionInfo(c.LCID).ISOCurrencySymbol == ifcMonetaryUnit.Currency.ToString())
               .Select(c => new RegionInfo(c.LCID).CurrencyEnglishName)
               .FirstOrDefault();
            //TODO: Convert currency to match pick list
            //convert to pick list hard coded for now
            if (!string.IsNullOrEmpty(value))
            {
                if (value.Contains("Dollar"))
                    value = "Dollars";
                else if (value.Contains("Euro"))
                    value = "Euros";
                else if (value.Contains("Pound"))
                    value = "Pounds";
                else
                    value = DEFAULT_STRING;
            }
            else
                value = DEFAULT_STRING;
            return value;
        }

        /// <summary>
        /// Determined the sheet the IfcRoot will have come from using the object type
        /// </summary>
        /// <param name="ifcRoot">object which inherits from IfcRoot </param>
        /// <returns>string holding sheet name</returns>
        public string GetSheetByObjectType(IfcRoot ifcRoot)
        {
            string value = DEFAULT_STRING;
            if (ifcRoot is IfcTypeObject) value = "Type";
            else if (ifcRoot is IfcRelAggregates) value = "Component";
            else if (ifcRoot is IfcRelContainedInSpatialStructure) value = "Component";
            else if (ifcRoot is IfcElement) value = "Component";
            //more sheets as tests date becomes available
            return value;
        }
        
       
        /// <summary>
        /// Get the associated Type for a IfcObject, so a Door can be of type "Door Type A"
        /// </summary>
        /// <param name="obj">IfcObject to get associated type information from</param>
        /// <returns>string holding the type information</returns>
        protected string GetTypeName(IfcObject obj)
        {
            string value = "";
            var elType = obj.IsDefinedBy.OfType<IfcRelDefinesByType>().FirstOrDefault();
            if ((elType != null) && (elType.RelatingType.Name != null))
                value = elType.RelatingType.Name.ToString();
            if ((string.IsNullOrEmpty(value)) && (obj.ObjectType != null))
                value = obj.ObjectType.ToString();
            return (string.IsNullOrEmpty(value)) ?  DEFAULT_STRING : value;
            //var elType = obj.IsDefinedBy.OfType<IfcRelDefinesByType>().FirstOrDefault();
            //return (elType != null) ? elType.RelatingType.Name.ToString() : DEFAULT_STRING;
        }

        /// <summary>
        /// Check if a string represents a date time
        /// </summary>
        /// <param name="date">string holding date</param>
        /// <returns>bool</returns>
        public bool IsDate (string date)
        {
            DateTime test;
            return DateTime.TryParse(date, out test);
        }

        /// <summary>
        /// Test string for email address format
        /// </summary>
        /// <param name="email">string holding email address</param>
        /// <returns>bool</returns>
        public bool IsEmailAddress(string email)
        {
            try
            {
                System.Net.Mail.MailAddress address = new System.Net.Mail.MailAddress(email);
                return true;
            }
            catch (FormatException)
            {
                // Do nothing
            }
            return false;
        }

        public bool IsNumeric(string num)
        {
            double test;
            return double.TryParse(num, out test);
        }
        /// <summary>
        /// Get the global units used for this building
        /// </summary>
        /// <param name="model">model object</param>
        /// <param name="wBookUnits">GlobalUnits to place units into</param>
        /// <returns>return passed wBookUnits(GlobalUnits) with units added</returns>
        public static GlobalUnits GetGlobalUnits(IModel model, GlobalUnits wBookUnits )
        {
            string linearUnit = "";
            string areaUnit = "";
            string volumeUnit = "";
            string moneyUnit = "";
            foreach (IfcUnitAssignment ifcUnitAssignment in model.InstancesOfType<IfcUnitAssignment>()) //loop all IfcUnitAssignment
            {
                foreach (IfcUnit ifcUnit in ifcUnitAssignment.Units) //loop the UnitSet
                {
                    IfcNamedUnit ifcNamedUnit = ifcUnit as IfcNamedUnit;
                    if (ifcNamedUnit != null)
                    {
                        if ((ifcNamedUnit.UnitType == IfcUnitEnum.LENGTHUNIT) && string.IsNullOrEmpty(linearUnit)) //we want length units until we have value
                        {
                            linearUnit = GetUnitName(ifcUnit);
                            if ((!linearUnit.Contains("feet")) && (linearUnit.Last() != 's'))
                                linearUnit = linearUnit + "s";
                        }


                        if ((ifcNamedUnit.UnitType == IfcUnitEnum.AREAUNIT) && string.IsNullOrEmpty(areaUnit)) //we want area units until we have value
                        {
                            areaUnit = GetUnitName(ifcUnit);
                            if ((!areaUnit.Contains("feet")) && (areaUnit.Last() != 's'))
                                areaUnit = areaUnit + "s";

                        }


                        if ((ifcNamedUnit.UnitType == IfcUnitEnum.VOLUMEUNIT) && string.IsNullOrEmpty(volumeUnit)) //we want volume units until we have value
                        {
                            volumeUnit = GetUnitName(ifcUnit);
                            if ((!volumeUnit.Contains("feet")) && (volumeUnit.Last() != 's'))
                                volumeUnit = volumeUnit + "s";
                        }
                    }
                    //get the money unit
                    if ((ifcUnit is IfcMonetaryUnit) && string.IsNullOrEmpty(moneyUnit))
                    {
                        moneyUnit = GetUnitName(ifcUnit);
                        if (moneyUnit.Last() != 's')
                            moneyUnit = moneyUnit + "s";
                    }

                }
            }

            //ensure we have a value on each unit type, if not then default
            linearUnit = string.IsNullOrEmpty(linearUnit) ? DEFAULT_STRING : linearUnit;
            areaUnit = string.IsNullOrEmpty(areaUnit) ? DEFAULT_STRING : areaUnit;
            volumeUnit = string.IsNullOrEmpty(volumeUnit) ? DEFAULT_STRING : volumeUnit;
            moneyUnit = string.IsNullOrEmpty(moneyUnit) ? DEFAULT_STRING : moneyUnit;

            //save values for retrieval by other sheets
            wBookUnits.LengthUnit = linearUnit;
            wBookUnits.AreaUnit = areaUnit;
            wBookUnits.VolumeUnit = volumeUnit;
            wBookUnits.MoneyUnit = moneyUnit;

            return wBookUnits;

        }

       

        #endregion

    }

    #region IComparer Classes
    /// <summary>
    /// ICompare class for IfcLabels, used to order by 
    /// </summary>
    public class CompareIfcLabel : IComparer<IfcLabel?>
    {
        public int Compare(IfcLabel? x, IfcLabel? y)
        {
            return string.Compare(x.ToString(), y.ToString(), true); //ignore case set to true
        }
    }

    /// <summary>
    /// ICompare class for String, used to order by 
    /// </summary>
    public class CompareString : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            return string.Compare(x, y, true); //ignore case set to true
        }
    }

 

    #endregion
}
