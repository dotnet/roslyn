// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;

namespace Roslyn.SyntaxVisualizer.Control.SymbolDisplay
{
    internal abstract class BasePropertyGridAdapter : ICustomTypeDescriptor
    {
        public virtual AttributeCollection GetAttributes() => TypeDescriptor.GetAttributes(this, true);
        public virtual string GetClassName() => TypeDescriptor.GetClassName(this, true);
        public virtual string GetComponentName() => TypeDescriptor.GetClassName(this, true);
        public virtual TypeConverter GetConverter() => TypeDescriptor.GetConverter(this, true);
        public virtual EventDescriptor GetDefaultEvent() => TypeDescriptor.GetDefaultEvent(this, true);
        public virtual PropertyDescriptor? GetDefaultProperty() => TypeDescriptor.GetDefaultProperty(this, true);
        public virtual object GetEditor(Type editorBaseType) => TypeDescriptor.GetEditor(this, editorBaseType, true);
        public virtual EventDescriptorCollection GetEvents() => TypeDescriptor.GetEvents(this, true);
        public virtual EventDescriptorCollection GetEvents(Attribute[] attributes) => TypeDescriptor.GetEvents(this, attributes, true);
        public virtual PropertyDescriptorCollection GetProperties() => TypeDescriptor.GetProperties(this, true);
        public virtual PropertyDescriptorCollection GetProperties(Attribute[] attributes) => TypeDescriptor.GetProperties(this, attributes, true);
        public virtual object? GetPropertyOwner(PropertyDescriptor pd) => null;
    }
}
