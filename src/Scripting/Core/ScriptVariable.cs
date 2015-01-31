// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Scripting
{
    /// <summary>
    /// A variable declared by the script.
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    public class ScriptVariable
    {
        private readonly object _instance;
        private readonly MemberInfo _member;

        internal ScriptVariable(object instance, MemberInfo member)
        {
            _instance = instance;
            _member = member;
        }

        /// <summary>
        /// The name of the variable.
        /// </summary>
        public string Name
        {
            get { return _member.Name; }
        }

        /// <summary>
        /// The type of the variable.
        /// </summary>
        public Type Type
        {
            get
            {
                if (_member.MemberType == MemberTypes.Field)
                {
                    return ((FieldInfo)_member).FieldType;
                }
                else
                {
                    return ((PropertyInfo)_member).PropertyType;
                }
            }
        }

        /// <summary>
        /// The value of the variable after running the script.
        /// </summary>
        public object Value
        {
            get
            {
                if (_member.MemberType == MemberTypes.Field)
                {
                    return ((FieldInfo)_member).GetValue(_instance);
                }
                else
                {
                    return ((PropertyInfo)_member).GetValue(_instance);
                }
            }
        }

        private string GetDebuggerDisplay()
        {
            return string.Format("{0}: {1}", this.Name, (this.Value != null) ? this.Value : "<null>");
        }
    }
}
