// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
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
        private readonly Binder binder;

        public ExtensionMethodScopes(Binder binder)
        {
            this.binder = binder;
        }

        public ExtensionMethodScopeEnumerator GetEnumerator()
        {
            return new ExtensionMethodScopeEnumerator(this.binder);
        }
    }

    /// <summary>
    /// An enumerator over ExtensionMethodScopes.
    /// </summary>
    internal struct ExtensionMethodScopeEnumerator
    {
        private readonly Binder binder;
        private ExtensionMethodScope current;

        public ExtensionMethodScopeEnumerator(Binder binder)
        {
            this.binder = binder;
            this.current = new ExtensionMethodScope();
        }

        public ExtensionMethodScope Current
        {
            get { return this.current; }
        }

        public bool MoveNext()
        {
            if (this.current.Binder == null)
            {
                this.current = GetNextScope(this.binder);
            }
            else
            {
                var binder = this.current.Binder;
                if (!this.current.SearchUsingsNotNamespace)
                {
                    // Return a scope for the same Binder that was previously exposed
                    // for the namespace, this time exposed for the usings.
                    this.current = new ExtensionMethodScope(binder, searchUsingsNotNamespace: true);
                }
                else
                {
                    // Return a scope for the next Binder that supports extension methods.
                    this.current = GetNextScope(binder.Next);
                }
            }

            return (this.current.Binder != null);
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
