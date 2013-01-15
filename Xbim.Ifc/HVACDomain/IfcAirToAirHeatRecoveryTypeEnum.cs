﻿#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcAirToAirHeatRecoveryTypeEnum.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

namespace Xbim.Ifc.HVACDomain
{
    public enum IfcAirToAirHeatRecoveryTypeEnum
    {
        FIXEDPLATECOUNTERFLOWEXCHANGER,
        FIXEDPLATECROSSFLOWEXCHANGER,
        FIXEDPLATEPARALLELFLOWEXCHANGER,
        ROTARYWHEEL,
        RUNAROUNDCOILLOOP,
        HEATPIPE,
        TWINTOWERENTHALPYRECOVERYLOOPS,
        THERMOSIPHONSEALEDTUBEHEATEXCHANGERS,
        THERMOSIPHONCOILTYPEHEATEXCHANGERS,
        USERDEFINED,
        NOTDEFINED
    }
}