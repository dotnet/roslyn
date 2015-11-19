// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis
{
    internal partial class ErrorLogger
    {
        /// <summary>
        /// Represents a value for a key-value pair to be emitted into the error log file.
        /// This could be a simple string or an integer OR could be a list of identical values OR a group of heterogeneous key-value pairs.
        /// </summary>
        private abstract class Value
        {
            protected readonly ErrorLogger Owner;

            protected Value(ErrorLogger owner)
            {
                Owner = owner;
            }

            public static Value Create(string value, ErrorLogger owner)
            {
                return new StringValue(value, owner);
            }

            public static Value Create(int value, ErrorLogger owner)
            {
                return new IntegerValue(value, owner);
            }

            public static Value Create(ImmutableArray<KeyValuePair<string, Value>> values, ErrorLogger owner)
            {
                return new GroupValue(values, owner);
            }

            public static Value Create(ImmutableArray<Value> values, ErrorLogger owner)
            {
                return new ListValue(values, owner);
            }

            public abstract void Write();

            private class StringValue : Value
            {
                private readonly string _value;

                public StringValue(string value, ErrorLogger owner)
                    : base(owner)
                {
                    _value = value;
                }

                public override void Write()
                {
                    Owner.WriteValue(_value);
                }
            }

            private class IntegerValue : Value
            {
                private readonly int _value;

                public IntegerValue(int value, ErrorLogger owner)
                    : base(owner)
                {
                    _value = value;
                }

                public override void Write()
                {
                    Owner.WriteValue(_value);
                }
            }

            private class GroupValue : Value
            {
                private readonly ImmutableArray<KeyValuePair<string, Value>> _keyValuePairs;

                public GroupValue(ImmutableArray<KeyValuePair<string, Value>> keyValuePairs, ErrorLogger owner)
                    : base(owner)
                {
                    _keyValuePairs = keyValuePairs;
                }

                public override void Write()
                {
                    Owner.StartGroup();

                    bool isFirst = true;
                    foreach (var kvp in _keyValuePairs)
                    {
                        Owner.WriteKeyValuePair(kvp, isFirst);
                        isFirst = false;
                    }

                    Owner.EndGroup();
                }
            }

            private class ListValue : Value
            {
                private readonly ImmutableArray<Value> _values;

                public ListValue(ImmutableArray<Value> values, ErrorLogger owner)
                    : base(owner)
                {
                    _values = values;
                }

                public override void Write()
                {
                    Owner.StartList();

                    bool isFirst = true;
                    foreach (var value in _values)
                    {
                        Owner.WriteValue(value, isFirst, valueInList: true);
                        isFirst = false;
                    }

                    Owner.EndList();
                }
            }
        }
    }
}
