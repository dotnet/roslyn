// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
{
    /// <summary>
    /// Uniquely identifies a top-level syntax declaration within a SyntaxTree.
    /// This is achieved by combining the qualified name of the declaration and an
    /// ordinal value. The ordinal value is used to distinguish nodes which have the same
    /// qualified name -- for example, across partial classes within the same tree.
    /// </summary>
    internal struct SyntaxNodeKey : IEquatable<SyntaxNodeKey>
    {
        private readonly string _name;
        private readonly int _ordinal;

        public static readonly SyntaxNodeKey Empty = new SyntaxNodeKey();

        public SyntaxNodeKey(string name, int ordinal)
        {
            if (ordinal < -1)
            {
                // Note: An ordinal value of -1 is special -- it means that this is the node
                // key for an "unknown" code model element.
                throw new ArgumentOutOfRangeException(nameof(ordinal));
            }

            _name = name ?? throw new ArgumentNullException(nameof(name));
            _ordinal = ordinal;
        }

        public bool Equals(SyntaxNodeKey other)
        {
            return _name == other._name
                && _ordinal == other._ordinal;
        }

        public override bool Equals(object obj)
        {
            if (obj is SyntaxNodeKey key)
            {
                return Equals(key);
            }

            return false;
        }

        public override int GetHashCode()
            => _name.GetHashCode() + _ordinal;

        public override string ToString()
            => $"{{{_name}, {_ordinal}}}";

        public string Name
        {
            get { return _name; }
        }

        public int Ordinal
        {
            get { return _ordinal; }
        }

        public bool IsEmpty
        {
            get { return _name == null && _ordinal == 0; }
        }

        public static bool operator ==(SyntaxNodeKey left, SyntaxNodeKey right)
            => left.Equals(right);

        public static bool operator !=(SyntaxNodeKey left, SyntaxNodeKey right)
            => !left.Equals(right);
    }
}
