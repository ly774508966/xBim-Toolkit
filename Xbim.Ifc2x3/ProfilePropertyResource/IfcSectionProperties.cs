﻿#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcSectionProperties.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.ComponentModel;
using Xbim.Ifc2x3.ProfileResource;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;

#endregion

namespace Xbim.Ifc2x3.ProfilePropertyResource
{
    [IfcPersistedEntityAttribute]
    public class IfcSectionProperties : INotifyPropertyChanged, ISupportChangeNotification, IPersistIfcEntity,
                                        INotifyPropertyChanging
    {
        public override bool Equals(object obj)
        {
            // Check for null
            if (obj == null) return false;

            // Check for type
            if (this.GetType() != obj.GetType()) return false;

            // Cast as IfcRoot
            IfcSectionProperties root = (IfcSectionProperties)obj;
            return this == root;
        }
        public override int GetHashCode()
        {
            return _entityLabel.GetHashCode(); //good enough as most entities will be in collections of  only one model, equals distinguishes for model
        }

        public static bool operator ==(IfcSectionProperties left, IfcSectionProperties right)
        {
            // If both are null, or both are same instance, return true.
            if (System.Object.ReferenceEquals(left, right))
                return true;

            // If one is null, but not both, return false.
            if (((object)left == null) || ((object)right == null))
                return false;

            return (left.EntityLabel == right.EntityLabel) && (left.ModelOf == right.ModelOf);

        }

        public static bool operator !=(IfcSectionProperties left, IfcSectionProperties right)
        {
            return !(left == right);
        }
        #region IPersistIfcEntity Members

        private int _entityLabel;
		bool _activated;

        private IModel _model;

        public IModel ModelOf
        {
            get { return _model; }
        }

        void IPersistIfcEntity.Bind(IModel model, int entityLabel, bool activated)
        {
            _activated=activated;
			_model = model;
            _entityLabel = entityLabel;
        }

        bool IPersistIfcEntity.Activated
        {
            get { return _activated; }
        }

        public int EntityLabel
        {
            get { return _entityLabel; }
        }

        void IPersistIfcEntity.Activate(bool write)
        {
            lock(this) { if (_model != null && !_activated) _activated = _model.Activate(this, false)>0;  }
            if (write) _model.Activate(this, write);
        }

        #endregion

        #region Fields

        private IfcSectionTypeEnum _sectionType;
        private IfcProfileDef _startProfile;
        private IfcProfileDef _endProfile;

        #endregion

        #region Properties

        [IfcAttribute(1, IfcAttributeState.Mandatory)]
        public IfcSectionTypeEnum SectionType
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _sectionType;
            }
            set { this.SetModelValue(this, ref _sectionType, value, v => SectionType = v, "SectionType"); }
        }

        [IfcAttribute(2, IfcAttributeState.Mandatory)]
        public IfcProfileDef StartProfile
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _startProfile;
            }
            set { this.SetModelValue(this, ref _startProfile, value, v => StartProfile = v, "StartProfile"); }
        }

        [IfcAttribute(3, IfcAttributeState.Optional)]
        public IfcProfileDef EndProfile
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _endProfile;
            }
            set { this.SetModelValue(this, ref _endProfile, value, v => EndProfile = v, "EndProfile"); }
        }

        #endregion

        public virtual void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                    _sectionType = (IfcSectionTypeEnum) Enum.Parse(typeof (IfcSectionTypeEnum), value.EnumVal, true);
                    break;
                case 1:
                    _startProfile = (IfcProfileDef) value.EntityVal;
                    break;
                case 2:
                    _endProfile = (IfcProfileDef) value.EntityVal;
                    break;

                default:
                    this.HandleUnexpectedAttribute(propIndex, value); break;
            }
        }

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

        #region IPersistIfc Members

        public string WhereRule()
        {
            return "";
        }

        #endregion
    }
}