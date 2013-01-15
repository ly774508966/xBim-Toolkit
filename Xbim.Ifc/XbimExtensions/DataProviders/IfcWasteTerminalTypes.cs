#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcWasteTerminalTypes.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System.Collections.Generic;
using Xbim.Ifc.PlumbingFireProtectionDomain;

#endregion

namespace Xbim.XbimExtensions.DataProviders
{
    public class IfcWasteTerminalTypes
    {
        private readonly IModel _model;

        public IfcWasteTerminalTypes(IModel model)
        {
            this._model = model;
        }

        public IEnumerable<IfcWasteTerminalType> Items
        {
            get { return this._model.InstancesOfType<IfcWasteTerminalType>(); }
        }
    }
}