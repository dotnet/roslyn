// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Describes anonymous type in terms of fields
    /// </summary>
    internal readonly struct AnonymousTypeDescriptor : IEquatable<AnonymousTypeDescriptor>
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
            return this.Equals(desc, TypeCompareKind.ConsiderEverything);
        }

        /// <summary>
        /// Compares two anonymous type descriptors, takes into account fields names and types, not locations.
        /// </summary>
        internal bool Equals(AnonymousTypeDescriptor other, TypeCompareKind comparison)
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
                if (!myFields[i].TypeWithAnnotations.Equals(otherFields[i].TypeWithAnnotations, comparison))
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
            return obj is AnonymousTypeDescriptor && this.Equals((AnonymousTypeDescriptor)obj, TypeCompareKind.ConsiderEverything);
        }

        public override int GetHashCode()
        {
            return this.Key.GetHashCode();
        }

        /// <summary>
        /// Creates a new anonymous type descriptor based on 'this' one, 
        /// but having field types passed as an argument.
        /// </summary>
        internal AnonymousTypeDescriptor WithNewFieldsTypes(ImmutableArray<TypeWithAnnotations> newFieldTypes)
        {
            Debug.Assert(!newFieldTypes.IsDefault);
            Debug.Assert(newFieldTypes.Length == this.Fields.Length);

            var newFields = this.Fields.SelectAsArray((field, i, types) => new AnonymousTypeField(field.Name, field.Location, types[i]), newFieldTypes);
            return new AnonymousTypeDescriptor(newFields, this.Location);
        }
    }
}
