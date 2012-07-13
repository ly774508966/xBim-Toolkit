﻿#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcTextStyleTextModel.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.ComponentModel;
using Xbim.Ifc.SelectTypes;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;

#endregion

namespace Xbim.Ifc.PresentationAppearanceResource
{
    [IfcPersistedEntity, Serializable]
    public class IfcTextStyleTextModel : INotifyPropertyChanged, ISupportChangeNotification, IPersistIfcEntity,
                                         INotifyPropertyChanging
    {

        #region IPersistIfcEntity Members

        private long _entityLabel;
        private IModel _model;

        public IModel ModelOf
        {
            get { return _model; }
        }

        void IPersistIfcEntity.Bind(IModel model, long entityLabel)
        {
            _model = model;
            _entityLabel = entityLabel;
        }

        bool IPersistIfcEntity.Activated
        {
            get { return _entityLabel > 0; }
        }

        public long EntityLabel
        {
            get { return _entityLabel; }
        }

        void IPersistIfcEntity.Activate(bool write)
        {
            if (_model != null && _entityLabel <= 0) _entityLabel = _model.Activate(this, false);
            if (write) _model.Activate(this, write);
        }

        #endregion

        #region Fields

        private IfcSizeSelect _textIndent;
        private IfcTextAlignment _textAlign;
        private IfcTextDecoration _textDecoration;
        private IfcSizeSelect _letterSpacing;
        private IfcSizeSelect _wordSpacing;
        private IfcTextTransformation _textTransform;
        private IfcSizeSelect _lineHeight;

        #endregion

        #region Properties

        [IfcAttribute(1, IfcAttributeState.Optional)]
        public IfcSizeSelect TextIndent
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _textIndent;
            }
            set { ModelHelper.SetModelValue(this, ref _textIndent, value, v => TextIndent = v, "TextIndent"); }
        }

        [IfcAttribute(2, IfcAttributeState.Optional)]
        public IfcTextAlignment TextAlign
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _textAlign;
            }
            set { ModelHelper.SetModelValue(this, ref _textAlign, value, v => TextAlign = v, "TextAlign"); }
        }

        [IfcAttribute(3, IfcAttributeState.Optional)]
        public IfcTextDecoration TextDecoration
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _textDecoration;
            }
            set { ModelHelper.SetModelValue(this, ref _textDecoration, value, v => TextDecoration = v, "TextDecoration"); }
        }

        [IfcAttribute(4, IfcAttributeState.Optional)]
        public IfcSizeSelect LetterSpacing
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _letterSpacing;
            }
            set { ModelHelper.SetModelValue(this, ref _letterSpacing, value, v => LetterSpacing = v, "LetterSpacing"); }
        }

        [IfcAttribute(5, IfcAttributeState.Optional)]
        public IfcSizeSelect WordSpacing
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _wordSpacing;
            }
            set { ModelHelper.SetModelValue(this, ref _wordSpacing, value, v => WordSpacing = v, "WordSpacing"); }
        }

        [IfcAttribute(6, IfcAttributeState.Optional)]
        public IfcTextTransformation TextTransform
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _textTransform;
            }
            set { ModelHelper.SetModelValue(this, ref _textTransform, value, v => TextTransform = v, "TextTransform"); }
        }

        [IfcAttribute(7, IfcAttributeState.Optional)]
        public IfcSizeSelect LineHeight
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _lineHeight;
            }
            set { ModelHelper.SetModelValue(this, ref _lineHeight, value, v => LineHeight = v, "LineHeight"); }
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

        public virtual void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                    _textIndent = (IfcSizeSelect) value.EntityVal;
                    break;
                case 1:
                    _textAlign = (IfcTextAlignment) value.EntityVal;
                    break;
                case 2:
                    _textDecoration = (IfcTextDecoration) value.EntityVal;
                    break;
                case 3:
                    _letterSpacing = (IfcSizeSelect) value.EntityVal;
                    break;
                case 4:
                    _wordSpacing = (IfcSizeSelect) value.EntityVal;
                    break;
                case 5:
                    _textTransform = (IfcTextTransformation) value.EntityVal;
                    break;
                case 6:
                    _lineHeight = (IfcSizeSelect) value.EntityVal;
                    break;
                default:
                    this.HandleUnexpectedAttribute(propIndex, value); break;
            }
        }

        #region ISupportIfcParser Members

        public string WhereRule()
        {
            return "";
        }

        #endregion
    }
}