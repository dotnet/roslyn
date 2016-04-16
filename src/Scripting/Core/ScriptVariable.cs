// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Reflection;

namespace Microsoft.CodeAnalysis.Scripting
{
    /// <summary>
    /// A variable declared by the script.
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    public sealed class ScriptVariable
    {
        private readonly object _instance;
        private readonly FieldInfo _field;

        internal ScriptVariable(object instance, FieldInfo field)
        {
            Debug.Assert(instance != null);
            Debug.Assert(field != null);

            _instance = instance;
            _field = field;
        }

        /// <summary>
        /// The name of the variable.
        /// </summary>
        public string Name => _field.Name;

        /// <summary>
        /// The type of the variable.
        /// </summary>
        public Type Type => _field.FieldType;

        /// <summary>
        /// True if the variable can't be written to (it's declared as readonly or a constant).
        /// </summary>
        public bool IsReadOnly => _field.IsInitOnly || _field.IsLiteral;

        /// <summary>
        /// The value of the variable after running the script.
        /// </summary>
        /// <exception cref="InvalidOperationException">Variable is read-only or a constant.</exception>
        /// <exception cref="ArgumentException">The type of the specified <paramref name="value"/> isn't assignable to the type of the variable.</exception>
        public object Value
        {
            get
            {
                return _field.GetValue(_instance);
            }

            set
            {
                if (_field.IsInitOnly)
                {
                    throw new InvalidOperationException(ScriptingResources.CannotSetReadOnlyVariable);
                }

                if (_field.IsLiteral)
                {
                    throw new InvalidOperationException(ScriptingResources.CannotSetConstantVariable);
                }

                _field.SetValue(_instance, value);
            }
        }

        private string GetDebuggerDisplay() => $"{Name}: {Value ?? "<null>"}";
    }
}