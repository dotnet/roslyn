// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    /// <summary>
    /// This is a verbatim copy of Microsoft.VisualStudio.Shell.LocalizableProperties.
    /// http://index/#Microsoft.VisualStudio.Shell.12.0/LocalizableProperties.cs.html
    /// Unfortunately we can't reuse that class because the GetComponentName method on
    /// it is not virtual, so we can't provide a name string for the VS Property Grid's
    /// combo box (which shows ComponentName in bold and ClassName in regular to the
    /// right from it)
    /// </summary>
    [ComVisible(true)]
    public class LocalizableProperties : ICustomTypeDescriptor
    {
        public AttributeCollection GetAttributes()
        {
            var col = TypeDescriptor.GetAttributes(this, true);
            return col;
        }

        public EventDescriptor GetDefaultEvent()
        {
            var ed = TypeDescriptor.GetDefaultEvent(this, true);
            return ed;
        }

        public PropertyDescriptor GetDefaultProperty()
        {
            var pd = TypeDescriptor.GetDefaultProperty(this, true);
            return pd;
        }

        public object GetEditor(Type editorBaseType)
        {
            var o = TypeDescriptor.GetEditor(this, editorBaseType, true);
            return o;
        }

        public EventDescriptorCollection GetEvents()
        {
            var edc = TypeDescriptor.GetEvents(this, true);
            return edc;
        }

        public EventDescriptorCollection GetEvents(System.Attribute[] attributes)
        {
            var edc = TypeDescriptor.GetEvents(this, attributes, true);
            return edc;
        }

        public object GetPropertyOwner(PropertyDescriptor pd)
        {
            return this;
        }

        [Browsable(false)]
        public string ExtenderCATID
        {
            get
            {
                return "";
            }
        }

        public PropertyDescriptorCollection GetProperties()
        {
            var pcol = GetProperties(null);
            return pcol;
        }

        public PropertyDescriptorCollection GetProperties(System.Attribute[] attributes)
        {
            var newList = new ArrayList();
            var props = TypeDescriptor.GetProperties(this, attributes, true);

            for (var i = 0; i < props.Count; i++)
            {
                newList.Add(CreateDesignPropertyDescriptor(props[i]));
            }

            return new PropertyDescriptorCollection((PropertyDescriptor[])newList.ToArray(typeof(PropertyDescriptor)));
        }

        public virtual DesignPropertyDescriptor CreateDesignPropertyDescriptor(PropertyDescriptor p)
        {
            return new DesignPropertyDescriptor(p);
        }

        public virtual string GetComponentName()
        {
            var name = TypeDescriptor.GetComponentName(this, true);
            return name;
        }

        public virtual TypeConverter GetConverter()
        {
            var tc = TypeDescriptor.GetConverter(this, true);
            return tc;
        }

        public virtual string GetClassName()
        {
            return this.GetType().FullName;
        }
    }
}
