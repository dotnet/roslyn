// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
{
    /// <summary>
    /// Uniquely identifies a top-level syntax declaration within a SyntaxTree. This is achieved by combining the
    /// qualified name of the declaration and an ordinal value. The ordinal value is used to distinguish nodes which
    /// have the same qualified name -- for example, across partial classes within the same tree.
    /// </summary>
    internal readonly record struct SyntaxNodeKey
    {
        public string Name { get; }
        public int Ordinal { get; }

        public static readonly SyntaxNodeKey Empty = new();

        public SyntaxNodeKey(string name, int ordinal)
        {
            if (ordinal < -1)
            {
                // Note: An ordinal value of -1 is special -- it means that this is the node
                // key for an "unknown" code model element.
                throw new ArgumentOutOfRangeException(nameof(ordinal));
            }

            Name = name ?? throw new ArgumentNullException(nameof(name));
            Ordinal = ordinal;
        }

        public bool IsEmpty => Name is null && Ordinal == 0;
    }
}
