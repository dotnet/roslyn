// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class BinderFactory
    {
        // key in the binder cache.
        // PERF: we are not using ValueTuple because its Equals is relatively slow.
        internal readonly struct BinderCacheKey : IEquatable<BinderCacheKey>
        {
            public readonly CSharpSyntaxNode syntaxNode;
            public readonly NodeUsage usage;

            public BinderCacheKey(CSharpSyntaxNode syntaxNode, NodeUsage usage)
            {
                this.syntaxNode = syntaxNode;
                this.usage = usage;
            }

            bool IEquatable<BinderCacheKey>.Equals(BinderCacheKey other)
            {
                return syntaxNode == other.syntaxNode && this.usage == other.usage;
            }

            public override int GetHashCode()
            {
                return Hash.Combine(syntaxNode.GetHashCode(), (int)usage);
            }

            public override bool Equals(object obj)
            {
                throw new NotSupportedException();
            }
        }

        // This dictionary stores contexts so we don't have to recreate them, which can be
        // expensive. 
        private readonly ConcurrentCache<BinderCacheKey, Binder> _binderCache;
        private readonly CSharpCompilation _compilation;
        private readonly SyntaxTree _syntaxTree;
        private readonly BuckStopsHereBinder _buckStopsHereBinder;
        private readonly bool _ignoreAccessibility;

        // In a typing scenario, GetBinder is regularly called with a non-zero position.
        // This results in a lot of allocations of BinderFactoryVisitors. Pooling them
        // reduces this churn to almost nothing.
        private static readonly ObjectPool<BinderFactoryVisitor> s_binderFactoryVisitorPool
            = new ObjectPool<BinderFactoryVisitor>(static () => new BinderFactoryVisitor(), 64);

        private readonly ObjectPool<BinderFactoryVisitor> _binderFactoryVisitorPool;

        internal BinderFactory(CSharpCompilation compilation, SyntaxTree syntaxTree, bool ignoreAccessibility, ObjectPool<BinderFactoryVisitor> binderFactoryVisitorPoolOpt = null)
        {
            _compilation = compilation;
            _syntaxTree = syntaxTree;
            _ignoreAccessibility = ignoreAccessibility;
            _binderFactoryVisitorPool = binderFactoryVisitorPoolOpt ?? s_binderFactoryVisitorPool;

            // 50 is more or less a guess, but it seems to work fine for scenarios that I tried.
            // we need something big enough to keep binders for most classes and some methods 
            // in a typical syntax tree.
            // On the other side, note that the whole factory is weakly referenced and therefore short lived, 
            // making this cache big is not very useful.
            // I noticed that while compiling Roslyn C# compiler most caches never see 
            // more than 50 items added before getting collected.
            _binderCache = new ConcurrentCache<BinderCacheKey, Binder>(50);

            _buckStopsHereBinder = new BuckStopsHereBinder(compilation, FileIdentifier.Create(syntaxTree, compilation.Options.SourceReferenceResolver));
        }

        internal SyntaxTree SyntaxTree
        {
            get
            {
                return _syntaxTree;
            }
        }

        private bool InScript
        {
            get
            {
                return _syntaxTree.Options.Kind == SourceCodeKind.Script;
            }
        }

        /// <summary>
        /// Return binder for binding at node.
        /// <paramref name="memberDeclarationOpt"/> and <paramref name="memberOpt"/>
        /// are optional syntax and symbol for the member containing <paramref name="node"/>.
        /// If provided, the <see cref="BinderFactoryVisitor"/> will use the member symbol rather
        /// than looking up the member in the containing type, allowing this method to be called
        /// while calculating the member list.
        /// </summary>
        /// <remarks>
        /// Note, there is no guarantee that the factory always gives back the same binder instance for the same node.
        /// </remarks>
        internal Binder GetBinder(SyntaxNode node, CSharpSyntaxNode memberDeclarationOpt = null, Symbol memberOpt = null)
        {
            int position = node.SpanStart;

            // Unless this is interactive retrieving a binder for global statements
            // at the very top-level (i.e. in a completely empty file) use
            // node.Parent to maintain existing behavior.
            if ((!InScript || node.Kind() != SyntaxKind.CompilationUnit) && node.Parent != null)
            {
                node = node.Parent;
            }

            return GetBinder(node, position, memberDeclarationOpt, memberOpt);
        }

        internal Binder GetBinder(SyntaxNode node, int position, CSharpSyntaxNode memberDeclarationOpt = null, Symbol memberOpt = null)
        {
            Debug.Assert(node != null);

#if DEBUG
            if (memberOpt is { ContainingSymbol: SourceMemberContainerTypeSymbol container })
            {
                container.AssertMemberExposure(memberOpt);
            }
#endif
            BinderFactoryVisitor visitor = GetBinderFactoryVisitor(position, memberDeclarationOpt, memberOpt);
            Binder result = visitor.Visit(node);
            ClearBinderFactoryVisitor(visitor);

            return result;
        }

        private BinderFactoryVisitor GetBinderFactoryVisitor(int position, CSharpSyntaxNode memberDeclarationOpt, Symbol memberOpt)
        {
            BinderFactoryVisitor visitor = _binderFactoryVisitorPool.Allocate();
            visitor.Initialize(factory: this, position, memberDeclarationOpt, memberOpt);

            return visitor;
        }

        private void ClearBinderFactoryVisitor(BinderFactoryVisitor visitor)
        {
            visitor.Clear();
            _binderFactoryVisitorPool.Free(visitor);
        }

        internal InMethodBinder GetPrimaryConstructorInMethodBinder(SynthesizedPrimaryConstructor constructor)
        {
            var typeDecl = constructor.GetSyntax();
            Debug.Assert(typeDecl.ParameterList is not null);

            var extraInfo = NodeUsage.ConstructorBodyOrInitializer;
            var key = BinderFactoryVisitor.CreateBinderCacheKey(typeDecl, extraInfo);

            if (!_binderCache.TryGetValue(key, out Binder resultBinder))
            {
                // Ctors cannot be generic
                Debug.Assert(constructor.Arity == 0, "Generic Ctor, What to do?");
                resultBinder = new InMethodBinder(constructor, GetInTypeBodyBinder(typeDecl));

                _binderCache.TryAdd(key, resultBinder);
            }

            return (InMethodBinder)resultBinder;
        }

        internal Binder GetInTypeBodyBinder(TypeDeclarationSyntax typeDecl)
        {
            BinderFactoryVisitor visitor = GetBinderFactoryVisitor(position: typeDecl.SpanStart, memberDeclarationOpt: null, memberOpt: null);
            Binder resultBinder = visitor.VisitTypeDeclarationCore(typeDecl, NodeUsage.NamedTypeBodyOrTypeParameters);
            ClearBinderFactoryVisitor(visitor);

            return resultBinder;
        }

        internal Binder GetInNamespaceBinder(CSharpSyntaxNode unit)
        {
            switch (unit.Kind())
            {
                case SyntaxKind.NamespaceDeclaration:
                case SyntaxKind.FileScopedNamespaceDeclaration:
                    {
                        BinderFactoryVisitor visitor = GetBinderFactoryVisitor(0, null, null);
                        Binder result = visitor.VisitNamespaceDeclaration((BaseNamespaceDeclarationSyntax)unit, unit.SpanStart, inBody: true, inUsing: false);
                        ClearBinderFactoryVisitor(visitor);
                        return result;
                    }

                case SyntaxKind.CompilationUnit:
                    // imports are bound by the Script class binder:
                    {
                        BinderFactoryVisitor visitor = GetBinderFactoryVisitor(0, null, null);
                        Binder result = visitor.VisitCompilationUnit((CompilationUnitSyntax)unit, inUsing: false, inScript: InScript);
                        ClearBinderFactoryVisitor(visitor);
                        return result;
                    }

                default:
                    return null;
            }
        }
    }
}
