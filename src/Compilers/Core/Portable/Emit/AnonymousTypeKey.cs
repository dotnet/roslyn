// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Emit
{
    internal readonly struct AnonymousTypeKeyField : IEquatable<AnonymousTypeKeyField>
    {
        /// <summary>
        /// Name of the anonymous type field.
        /// </summary>
        internal readonly string Name;

        /// <summary>
        /// True if the anonymous type field was marked as 'Key' in VB.
        /// </summary>
        internal readonly bool IsKey;

        /// <summary>
        /// <see cref="Name"/> is case insensitive.
        /// </summary>
        internal readonly bool IgnoreCase;

        public AnonymousTypeKeyField(string name, bool isKey, bool ignoreCase)
        {
            Debug.Assert(name != null);

            Name = name;
            IsKey = isKey;
            IgnoreCase = ignoreCase;
        }

        public bool Equals(AnonymousTypeKeyField other)
        {
            return IsKey == other.IsKey &&
                   IgnoreCase == other.IgnoreCase &&
                   (IgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal).Equals(Name, other.Name);
        }

        public override bool Equals(object obj)
        {
            return Equals((AnonymousTypeKeyField)obj);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(IsKey,
                   Hash.Combine(IgnoreCase,
                   (IgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal).GetHashCode(Name)));
        }
    }

    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    internal readonly struct AnonymousTypeKey : IEquatable<AnonymousTypeKey>
    {
        internal readonly bool IsDelegate;
        internal readonly ImmutableArray<AnonymousTypeKeyField> Fields;

        internal AnonymousTypeKey(ImmutableArray<AnonymousTypeKeyField> fields, bool isDelegate = false)
        {
            this.IsDelegate = isDelegate;
            this.Fields = fields;
        }

        public bool Equals(AnonymousTypeKey other)
        {
            return (this.IsDelegate == other.IsDelegate) && this.Fields.SequenceEqual(other.Fields);
        }

        public override bool Equals(object obj)
        {
            return this.Equals((AnonymousTypeKey)obj);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(this.IsDelegate.GetHashCode(), Hash.CombineValues(this.Fields));
        }

        private string GetDebuggerDisplay()
        {
            var pooledBuilder = PooledStringBuilder.GetInstance();
            var builder = pooledBuilder.Builder;
            for (int i = 0; i < this.Fields.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append("|");
                }
                builder.Append(this.Fields[i].Name);
            }
            return pooledBuilder.ToStringAndFree();
        }
    }
}
