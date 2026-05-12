// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal abstract partial class GreenNode
{
    /// <summary>
    ///  Provides enumeration over only the tokens within a <see cref="GreenNode"/> syntax tree,
    ///  filtering out non-token nodes during traversal.
    /// </summary>
    public readonly ref struct TokenEnumerable(GreenNode node)
    {
        public Enumerator GetEnumerator()
            => new(node);

        /// <summary>
        ///  Enumerates only the tokens within a <see cref="GreenNode"/> syntax tree,
        ///  automatically filtering out non-token nodes during traversal.
        /// </summary>
        public ref struct Enumerator(GreenNode node)
        {
            private GreenNode.Enumerator _enumerator = node.GetEnumerator();
            private InternalSyntax.SyntaxToken? _current;

            public void Dispose()
            {
                _enumerator.Dispose();
            }

            public readonly InternalSyntax.SyntaxToken Current => _current!;

            public bool MoveNext()
            {
                while (_enumerator.MoveNext())
                {
                    if (_enumerator.Current.IsToken)
                    {
                        _current = (InternalSyntax.SyntaxToken)_enumerator.Current;
                        return true;
                    }
                }

                _current = null;
                return false;
            }
        }
    }
}
