// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        /// Compares two anonymous type descriptors, takes into account fields and types, not locations.
        /// </summary>
        internal bool Equals(AnonymousTypeDescriptor other, TypeCompareKind comparison)
        {
            // Comparing keys ensures field count and field names are the same
            if (this.Key != other.Key)
            {
                return false;
            }

            // Compare field types
            return Fields.SequenceEqual(
                other.Fields,
                comparison,
                static (x, y, comparison) => AnonymousTypeField.Equals(x, y, comparison));
        }

        /// <summary>
        /// Compares two anonymous type descriptors, takes into account fields and types, not locations.
        /// </summary>
        public override bool Equals(object? obj)
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

            var newFields = Fields.ZipAsArray(newFieldTypes, static (field, type) => field.WithType(type));
            return new AnonymousTypeDescriptor(newFields, this.Location);
        }

        internal AnonymousTypeDescriptor SubstituteTypes(AbstractTypeMap map, out bool changed)
        {
            var oldFieldTypes = Fields.SelectAsArray(f => f.TypeWithAnnotations);
            var newFieldTypes = map.SubstituteTypes(oldFieldTypes);
            changed = (oldFieldTypes != newFieldTypes);
            return changed ? WithNewFieldsTypes(newFieldTypes) : this;
        }
    }
}
