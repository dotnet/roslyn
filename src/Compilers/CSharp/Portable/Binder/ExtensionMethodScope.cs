// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.CSharp
{
    // PROTOTYPE consider renaming the file
    /// <summary>
    /// A distinct scope that may expose extension methods or types. For a particular Binder,  there
    /// are two possible scopes: one for the namespace, and another for any using statements
    /// in the namespace. The namespace scope is searched before the using scope.
    /// </summary>
    internal readonly struct ExtensionScope
    {
        public readonly Binder Binder;

        public ExtensionScope(Binder binder)
        {
            this.Binder = binder;
        }
    }

    /// <summary>
    /// An enumerable collection of extension method scopes in search
    /// order, from the given Binder, out through containing Binders.
    /// </summary>
    internal readonly struct ExtensionScopes
    {
        private readonly Binder _binder;

        public ExtensionScopes(Binder binder)
        {
            _binder = binder;
        }

        public ExtensionScopeEnumerator GetEnumerator()
        {
            return new ExtensionScopeEnumerator(_binder);
        }
    }

    /// <summary>
    /// An enumerator over ExtensionScopes.
    /// </summary>
    internal struct ExtensionScopeEnumerator
    {
        private readonly Binder _binder;
        private ExtensionScope _current;

        public ExtensionScopeEnumerator(Binder binder)
        {
            _binder = binder;
            _current = new ExtensionScope();
        }

        public ExtensionScope Current
        {
            get { return _current; }
        }

        public bool MoveNext()
        {
            if (_current.Binder == null)
            {
                _current = GetNextScope(_binder);
            }
            else
            {
                var binder = _current.Binder;
                // Return a scope for the next Binder that supports extension methods or types.
                _current = GetNextScope(binder.Next);
            }

            return (_current.Binder != null);
        }

        private static ExtensionScope GetNextScope(Binder binder)
        {
            for (var scope = binder; scope != null; scope = scope.Next)
            {
                if (scope.SupportsExtensions)
                {
                    return new ExtensionScope(scope);
                }
            }

            return new ExtensionScope();
        }
    }
}
