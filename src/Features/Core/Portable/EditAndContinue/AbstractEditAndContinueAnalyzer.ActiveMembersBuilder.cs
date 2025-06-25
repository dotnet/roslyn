// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal partial class AbstractEditAndContinueAnalyzer
{
    /// <summary>
    /// Builds a set of active methods during EnC analysis.
    /// Active method is a method whose body directly contains an active statement (i.e. an active statement not in a lambda/function).
    /// Constructor to which field/property initializers are emitted is active if its body or any of the field/property initializers has an active statement outside of a lambda.
    /// Fields and properties with initializers are considered active if any of the constructors that these initializers are emitted to are active.
    /// </summary>
    private readonly struct ActiveMembersBuilder(AbstractEditAndContinueAnalyzer analyzer, DocumentSemanticModel oldModel, DocumentSemanticModel newModel, CancellationToken cancellationToken) : IDisposable
    {
        private readonly PooledHashSet<IMethodSymbol> _methods = PooledHashSet<IMethodSymbol>.GetInstance();
        private readonly PooledDictionary<SyntaxNode, SyntaxNode> _declarations = PooledDictionary<SyntaxNode, SyntaxNode>.GetInstance();

        public void Dispose()
        {
            _methods.Free();
            _declarations.Free();
        }

        public void Add(ISymbol member)
        {
            if (member is IFieldSymbol or IPropertySymbol or IEventSymbol)
            {
                foreach (var constructor in member.IsStatic ? member.ContainingType.StaticConstructors : member.ContainingType.Constructors)
                {
                    if (analyzer.IsConstructorWithMemberInitializers(constructor, cancellationToken))
                    {
                        _methods.Add(constructor);
                    }
                }
            }
            else
            {
                _methods.Add((IMethodSymbol)member);
            }
        }

        internal void Add(SyntaxNode oldMemberDeclaration, SyntaxNode newMemberDeclaration)
        {
            _ = _declarations.TryAdd(oldMemberDeclaration, newMemberDeclaration);
        }

        public bool IsActive(ISymbol member)
        {
            if (member is IMethodSymbol method)
            {
                return IsActiveMethod(method);
            }

            // field/property with initializer is active if any of the constructor is active:
            foreach (var constructor in member.IsStatic ? member.ContainingType.StaticConstructors : member.ContainingType.Constructors)
            {
                if (IsActiveMethod(constructor) && analyzer.IsConstructorWithMemberInitializers(constructor, cancellationToken))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsActiveMethod(IMethodSymbol method)
        {
            if (_methods.Contains(method))
            {
                return true;
            }

            ResolveSymbols();
            return _methods.Contains(method);
        }

        private void ResolveSymbols()
        {
            if (_declarations.Count == 0)
            {
                return;
            }

            foreach (var (oldDeclaration, newDeclaration) in _declarations)
            {
                var methods = analyzer.GetEditedSymbols(EditKind.Update, oldDeclaration, newDeclaration, oldModel, newModel, cancellationToken);
                foreach (var (oldMethod, _) in methods)
                {
                    // Active declarations are added based on location of current active statements, which might be invalid.
                    if (oldMethod is IMethodSymbol or IPropertySymbol or IFieldSymbol or IEventSymbol)
                    {
                        Add(oldMethod);
                    }
                }
            }

            _declarations.Clear();
        }
    }
}
