﻿#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcResourceApprovalRelationship.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.ComponentModel;
using Xbim.Ifc2x3.Kernel;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;

#endregion

namespace Xbim.Ifc2x3.ApprovalResource
{
    [IfcPersistedEntityAttribute, Serializable]
    public class IfcResourceApprovalRelationship : IPersistIfcEntity, ISupportChangeNotification, INotifyPropertyChanged,
                                                   INotifyPropertyChanging
    {
        public override bool Equals(object obj)
        {
            // Check for null
            if (obj == null) return false;

            // Check for type
            if (this.GetType() != obj.GetType()) return false;

            // Cast as IfcRoot
            IfcResourceApprovalRelationship root = (IfcResourceApprovalRelationship)obj;
            return this == root;
        }
        public override int GetHashCode()
        {
            return Math.Abs(_entityLabel); //good enough as most entities will be in collections of  only one model, equals distinguishes for model
        }

        public static bool operator ==(IfcResourceApprovalRelationship left, IfcResourceApprovalRelationship right)
        {
            // If both are null, or both are same instance, return true.
            if (System.Object.ReferenceEquals(left, right))
                return true;

            // If one is null, but not both, return false.
            if (((object)left == null) || ((object)right == null))
                return false;

            return (Math.Abs(left.EntityLabel) == Math.Abs(right.EntityLabel)) && (left.ModelOf == right.ModelOf);

        }

        public static bool operator !=(IfcResourceApprovalRelationship left, IfcResourceApprovalRelationship right)
        {
            return !(left == right);
        }
        #region IPersistIfcEntity Members

        private int _entityLabel;
        private IModel _model;

        public IModel ModelOf
        {
            get { return _model; }
        }

        void IPersistIfcEntity.Bind(IModel model, int entityLabel)
        {
            _model = model;
            _entityLabel = entityLabel;
        }

        bool IPersistIfcEntity.Activated
        {
            get { return _entityLabel > 0; }
        }

        public int EntityLabel
        {
            get { return _entityLabel; }
        }

        void IPersistIfcEntity.Activate(bool write)
        {
            if (_model != null && _entityLabel <= 0) _entityLabel = _model.Activate(this, false);
            if (write) _model.Activate(this, write);
        }

        #endregion


        public IfcResourceApprovalRelationship()
        {
            _approvedResources = new XbimSet<IfcResource>(this);
        }

        #region Fields

        private XbimSet<IfcResource> _approvedResources;
        private IfcApproval _approval;

        #endregion

        #region Properties

        /// <summary>
        ///   Resources approved by the approval.
        /// </summary>
        [IfcAttribute(1, IfcAttributeState.Mandatory, IfcAttributeType.Set, 1)]
        public XbimSet<IfcResource> ApprovedResources
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _approvedResources;
            }
            set
            {
                this.SetModelValue(this, ref _approvedResources, value, v => ApprovedResources = v,
                                           "ApprovedResources");
            }
        }

        /// <summary>
        ///   The approval for the resources selected.
        /// </summary>
        [IfcAttribute(2, IfcAttributeState.Mandatory)]
        public IfcApproval Approval
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _approval;
            }
            set { this.SetModelValue(this, ref _approval, value, v => Approval = v, "Approval"); }
        }

        #endregion

        #region INotifyPropertyChanged Members

        [field: NonSerialized] //don't serialize events
            private event PropertyChangedEventHandler PropertyChanged;

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add { PropertyChanged += value; }
            remove { PropertyChanged -= value; }
        }

        void ISupportChangeNotification.NotifyPropertyChanging(string propertyName)
        {
            PropertyChangingEventHandler handler = PropertyChanging;
            if (handler != null)
            {
                handler(this, new PropertyChangingEventArgs(propertyName));
            }
        }

        [field: NonSerialized] //don't serialize events
            private event PropertyChangingEventHandler PropertyChanging;

        event PropertyChangingEventHandler INotifyPropertyChanging.PropertyChanging
        {
            add { PropertyChanging += value; }
            remove { PropertyChanging -= value; }
        }

        #endregion

        #region ISupportChangeNotification Members

        void ISupportChangeNotification.NotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        #region ISupportIfcParser Members

        public virtual void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                    _approvedResources.Add((IfcResource) value.EntityVal);
                    break;
                case 1:
                    _approval = (IfcApproval) value.EntityVal;
                    break;
                default:
                    this.HandleUnexpectedAttribute(propIndex, value); break;
            }
        }

        public string WhereRule()
        {
            return "";
        }

        #endregion
    }
}