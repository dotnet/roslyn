// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis;

namespace Roslyn.Utilities
{
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    internal readonly struct EnumField
    {
        public static readonly IComparer<EnumField> Comparer = new EnumFieldComparer();

        public readonly string Name;
        public readonly ulong Value;
        public readonly object? IdentityOpt;

        public EnumField(string name, ulong value, object? identityOpt = null)
        {
            RoslynDebug.Assert(name != null);
            this.Name = name;
            this.Value = value;
            this.IdentityOpt = identityOpt;
        }

        public bool IsDefault
        {
            get { return this.Name == null; }
        }

        private string GetDebuggerDisplay()
        {
            return string.Format("{{{0} = {1}}}", this.Name, this.Value);
        }

        internal static EnumField FindValue(ArrayBuilder<EnumField> sortedFields, ulong value)
        {
            int start = 0;
            int end = sortedFields.Count;

            while (start < end)
            {
                int mid = start + (end - start) / 2;

                long diff = unchecked((long)value - (long)sortedFields[mid].Value); // NOTE: Has to match the comparer below.

                if (diff == 0)
                {
                    while (mid >= start && sortedFields[mid].Value == value)
                    {
                        mid--;
                    }
                    return sortedFields[mid + 1];
                }
                else if (diff > 0)
                {
                    end = mid; // Exclude mid.
                }
                else
                {
                    start = mid + 1; // Exclude mid.
                }
            }

            return default(EnumField);
        }

        private class EnumFieldComparer : IComparer<EnumField>
        {
            int IComparer<EnumField>.Compare(EnumField field1, EnumField field2)
            {
                // Sort order is descending value, then ascending name.
                int diff = unchecked(((long)field2.Value).CompareTo((long)field1.Value));
                return diff == 0
                    ? string.CompareOrdinal(field1.Name, field2.Name)
                    : diff;
            }
        }
    }
}
