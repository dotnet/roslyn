// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Describes anonymous type in terms of fields
    /// </summary>
    internal struct AnonymousTypeDescriptor : IEquatable<AnonymousTypeDescriptor>
    {
        /// <summary> Anonymous type location </summary>
        public readonly Location Location;

        /// <summary> Anonymous type fields </summary>
        public readonly ImmutableArray<AnonymousTypeField> Fields;

        /// <summary>
        /// Anonymous type descriptor Key 
        /// 
        /// The key is to be used to separate anonymous type templates in an anonymous type symbol cache. 
        /// The type descriptors with the same keys are supposed to map to 'the same' anonymous type 
        /// template in terms of the same generic type being used for their implementation.
        /// </summary>
        public readonly string Key;

        public AnonymousTypeDescriptor(ImmutableArray<AnonymousTypeField> fields, Location location)
        {
            this.Fields = fields;
            this.Location = location;
            this.Key = ComputeKey(fields, f => f.Name);
        }

        internal static string ComputeKey<T>(ImmutableArray<T> fields, Func<T, string> getName)
        {
            var key = PooledStringBuilder.GetInstance();
            foreach (var field in fields)
            {
                key.Builder.Append('|');
                key.Builder.Append(getName(field));
            }
            return key.ToStringAndFree();
        }

        [Conditional("DEBUG")]
        internal void AssertIsGood()
        {
            Debug.Assert(!this.Fields.IsDefault);

            foreach (var field in this.Fields)
            {
                field.AssertIsGood();
            }
        }

        public bool Equals(AnonymousTypeDescriptor desc)
        {
            return this.Equals(desc, TypeSymbolEqualityOptions.None);
        }

        /// <summary>
        /// Compares two anonymous type descriptors, takes into account fields names and types, not locations.
        /// </summary>
        internal bool Equals(AnonymousTypeDescriptor other, TypeSymbolEqualityOptions options)
        {
            // Comparing keys ensures field count and field names are the same
            if (this.Key != other.Key)
            {
                return false;
            }

            // Compare field types
            ImmutableArray<AnonymousTypeField> myFields = this.Fields;
            int count = myFields.Length;
            ImmutableArray<AnonymousTypeField> otherFields = other.Fields;
            for (int i = 0; i < count; i++)
            {
                if (!myFields[i].Type.TypeSymbol.Equals(otherFields[i].Type.TypeSymbol, options))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Compares two anonymous type descriptors, takes into account fields names and types, not locations.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is AnonymousTypeDescriptor && this.Equals((AnonymousTypeDescriptor)obj, TypeSymbolEqualityOptions.None);
        }

        public override int GetHashCode()
        {
            return this.Key.GetHashCode();
        }

        /// <summary>
        /// Creates a new anonymous type descriptor based on 'this' one, 
        /// but having field types passed as an argument.
        /// </summary>
        internal AnonymousTypeDescriptor WithNewFieldsTypes(ImmutableArray<TypeSymbolWithAnnotations> newFieldTypes)
        {
            Debug.Assert(!newFieldTypes.IsDefault);
            Debug.Assert(newFieldTypes.Length == this.Fields.Length);

            AnonymousTypeField[] newFields = new AnonymousTypeField[this.Fields.Length];
            for (int i = 0; i < newFields.Length; i++)
            {
                var field = this.Fields[i];
                newFields[i] = new AnonymousTypeField(field.Name, field.Location, newFieldTypes[i]);
            }

            return new AnonymousTypeDescriptor(newFields.AsImmutable(), this.Location);
        }
    }
}
