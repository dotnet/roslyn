// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public partial class NodeInfo
    {
        //Package of information containing the name, type, and value of a field on a syntax node.
        public class FieldInfo
        {
            private readonly string _propertyName;
            private readonly Type _fieldType;
            private readonly object _value;
            public string PropertyName
            {
                get
                {
                    return _propertyName;
                }
            }

            public Type FieldType
            {
                get
                {
                    return _fieldType;
                }
            }

            public object Value
            {
                get
                {
                    return _value;
                }
            }

            public FieldInfo(string propertyName, Type fieldType, object value)
            {
                _propertyName = propertyName;
                _fieldType = fieldType;
                _value = value;
            }
        }
    }
}
