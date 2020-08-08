// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A distinct scope that may expose extension methods. For a particular Binder,  there
    /// are two possible scopes: one for the namespace, and another for any using statements
    /// in the namespace. The namespace scope is searched before the using scope.
    /// </summary>
    internal struct ExtensionMethodScope
    {
        public readonly Binder Binder;
        public readonly bool SearchUsingsNotNamespace;

        public ExtensionMethodScope(Binder binder, bool searchUsingsNotNamespace)
        {
            this.Binder = binder;
            this.SearchUsingsNotNamespace = searchUsingsNotNamespace;
        }
    }

    /// <summary>
    /// An enumerable collection of extension method scopes in search
    /// order, from the given Binder, out through containing Binders.
    /// </summary>
    internal struct ExtensionMethodScopes
    {
        private readonly Binder _binder;

        public ExtensionMethodScopes(Binder binder)
        {
            _binder = binder;
        }

        public ExtensionMethodScopeEnumerator GetEnumerator()
        {
            return new ExtensionMethodScopeEnumerator(_binder);
        }
    }

    /// <summary>
    /// An enumerator over ExtensionMethodScopes.
    /// </summary>
    internal struct ExtensionMethodScopeEnumerator
    {
        private readonly Binder _binder;
        private ExtensionMethodScope _current;

        public ExtensionMethodScopeEnumerator(Binder binder)
        {
            _binder = binder;
            _current = new ExtensionMethodScope();
        }

        public ExtensionMethodScope Current
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
                if (!_current.SearchUsingsNotNamespace)
                {
                    // Return a scope for the same Binder that was previously exposed
                    // for the namespace, this time exposed for the usings.
                    _current = new ExtensionMethodScope(binder, searchUsingsNotNamespace: true);
                }
                else
                {
                    // Return a scope for the next Binder that supports extension methods.
                    _current = GetNextScope(binder.Next);
                }
            }

            return (_current.Binder != null);
        }

        private static ExtensionMethodScope GetNextScope(Binder binder)
        {
            for (var scope = binder; scope != null; scope = scope.Next)
            {
                if (scope.SupportsExtensionMethods)
                {
                    return new ExtensionMethodScope(scope, searchUsingsNotNamespace: false);
                }
            }

            return new ExtensionMethodScope();
        }
    }
}
