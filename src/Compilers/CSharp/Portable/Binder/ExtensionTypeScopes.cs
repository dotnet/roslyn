// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// An enumerable collection of extension type scopes in search
    /// order, from the given Binder, out through containing Binders.
    /// </summary>
    internal readonly struct ExtensionTypeScopes
    {
        private readonly Binder _binder;

        public ExtensionTypeScopes(Binder binder)
        {
            _binder = binder;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_binder);
        }

        /// <summary>
        /// An enumerator over ExtensionTypeScopes.
        /// </summary>
        internal struct Enumerator
        {
            private readonly Binder _startingBinder;
            private Binder? _current;

            public Enumerator(Binder binder)
            {
                Debug.Assert(binder is not null);
                _startingBinder = binder;
                _current = null;
            }

            public readonly Binder Current
            {
                get
                {
                    Debug.Assert(_current is not null);
                    return _current;
                }
            }

            [MemberNotNullWhen(true, nameof(Current))]
            public bool MoveNext()
            {
                _current = getNextScope(_current is null ? _startingBinder : _current.Next);
                return (_current is not null);

                static Binder? getNextScope(Binder? binder)
                {
                    for (var scope = binder; scope != null; scope = scope.Next)
                    {
                        if (scope.SupportsExtensionTypes)
                        {
                            return scope;
                        }
                    }

                    return null;
                }
            }
        }
    }
}
