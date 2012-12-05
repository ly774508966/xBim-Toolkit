#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcFlowMovingDevices.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System.Collections.Generic;
using Xbim.Ifc.SharedBldgServiceElements;

#endregion

namespace Xbim.XbimExtensions.DataProviders
{
    public class IfcFlowMovingDevices
    {
        private readonly IModel _model;

        public IfcFlowMovingDevices(IModel model)
        {
            this._model = model;
        }

        public IEnumerable<IfcFlowMovingDevice> Items
        {
            get { return this._model.InstancesOfType<IfcFlowMovingDevice>(); }
        }
    }
}