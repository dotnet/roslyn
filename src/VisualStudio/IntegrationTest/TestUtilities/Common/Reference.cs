// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Common
{
    /// <summary>
    /// Represents a result of a Find References operation.
    /// </summary>
    [Serializable]
    public class Reference : IEquatable<Reference>
    {
        public string FilePath { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
        public string Code { get; set; }

        public Reference() { }

        public bool Equals(Reference other)
        {
            if (other == null)
            {
                return false;
            }

            return FilePath.Equals(other.FilePath, StringComparison.OrdinalIgnoreCase)
                && Line == other.Line
                && Column == other.Column
                && Code.Equals(other.Code);
        }

        public override bool Equals(object obj)
            => Equals(obj as Reference);

        public override int GetHashCode()
        {
            return Hash.Combine(FilePath,
                Hash.Combine(Line,
                    Hash.Combine(Column,
                        Hash.Combine(Code, 0))));
        }

        public override string ToString()
            => $"{FilePath} ({Line}, {Column}): {Code}";
    }
}
