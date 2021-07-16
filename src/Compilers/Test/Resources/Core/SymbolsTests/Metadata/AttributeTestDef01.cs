// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;

namespace CustomAttribute
{
    [AttributeUsage(AttributeTargets.All, Inherited = true, AllowMultiple = true)]
    public class AllInheritMultipleAttribute : Attribute
    {
        public AllInheritMultipleAttribute() { UIntField = 1; }
        public AllInheritMultipleAttribute(object p1, BindingFlags p2 = BindingFlags.Static) { UIntField = 2; }
        public AllInheritMultipleAttribute(object p1, byte p2, sbyte p3 = -1) { UIntField = 3; }
        public AllInheritMultipleAttribute(object p1, long p2, float p3 = 0.123f, short p4 = -2) { UIntField = 4; }
        // Char array
        public AllInheritMultipleAttribute(char[] ary1, params string[] ary2) { }

        public uint UIntField;
        public ulong[] AryField;
        // uint16 jagged array
        object[] propField;
        public object[] AryProp
        {
            get { return propField; }
            set { propField = value; }
        }
    }

    // default: Inherited = true, AllowMultiple = false
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property)]
    public class BaseAttribute : Attribute
    {
        public BaseAttribute(object p) { ObjectField = p; }
        public object ObjectField;
    }

    // target (not inherit): 1 same, 2 diff from base
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Parameter)]
    public class DerivedAttribute : BaseAttribute
    {
        public DerivedAttribute(object p) : base(p) { }
        Type _prop;
        public Type TypeProp
        {
            get { return _prop; }
            set { _prop = value; }
        }
    }

    // C# - @AttrName
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
    public class AttrName : Attribute
    {
        public ushort UShortField;
    }

    [AttributeUsage(AttributeTargets.Module, Inherited = false, AllowMultiple = false)]
    public class AttrNameAttribute : Attribute
    {
        public Type TypeField;
    }
}
