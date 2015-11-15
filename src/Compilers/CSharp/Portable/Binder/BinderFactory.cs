﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class BinderFactory
    {
        // key in the binder cache.
        // PERF: we are not using ValueTuple because its Equals is relatively slow.
        private struct BinderCacheKey : IEquatable<BinderCacheKey>
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

        // In a typing scenario, GetBinder is regularly called with a non-zero position.
        // This results in a lot of allocations of BinderFactoryVisitors. Pooling them
        // reduces this churn to almost nothing.
        private readonly ObjectPool<BinderFactoryVisitor> _binderFactoryVisitorPool;

        internal BinderFactory(CSharpCompilation compilation, SyntaxTree syntaxTree)
        {
            _compilation = compilation;
            _syntaxTree = syntaxTree;

            _binderFactoryVisitorPool = new ObjectPool<BinderFactoryVisitor>(() => new BinderFactoryVisitor(this), 64);

            // 50 is more or less a guess, but it seems to work fine for scenarios that I tried.
            // we need something big enough to keep binders for most classes and some methods 
            // in a typical syntax tree.
            // On the other side, note that the whole factory is weakly referenced and therefore short lived, 
            // making this cache big is not very useful.
            // I noticed that while compiling Roslyn C# compiler most caches never see 
            // more than 50 items added before getting collected.
            _binderCache = new ConcurrentCache<BinderCacheKey, Binder>(50);

            _buckStopsHereBinder = new BuckStopsHereBinder(compilation);
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
        /// Note, there is no guarantee that the factory always gives back the same binder instance for the same <param name="node"/>.
        /// </summary>
        internal Binder GetBinder(CSharpSyntaxNode node)
        {
            int position = node.SpanStart;

            // Special case: In interactive code, we may be trying to retrieve a binder for global statements
            // at the *very* top-level (i.e. in a completely empty file). In this case, we use the compilation unit
            // directly since it's parent would be null.
            if (InScript && node.Kind() == SyntaxKind.CompilationUnit)
            {
                return GetBinder(node, position);
            }

            // ACASEY: Using node.Parent here to maintain existing behavior,
            // but I have no idea why.
            return GetBinder(node.Parent, position);
        }

        internal Binder GetBinder(CSharpSyntaxNode node, int position)
        {
            Debug.Assert(node != null);

            Binder result = null;

            BinderFactoryVisitor visitor = _binderFactoryVisitorPool.Allocate();
            visitor.Position = position;
            result = node.Accept(visitor);
            _binderFactoryVisitorPool.Free(visitor);

            return result;
        }

        /// <summary>
        /// Returns binder that binds usings and aliases 
        /// </summary>
        /// <param name="unit">
        /// Specify <see cref="NamespaceDeclarationSyntax"/> imports in the corresponding namespace, or
        /// <see cref="CompilationUnitSyntax"/> for top-level imports.
        /// </param>
        /// <param name="inUsing">True if the binder will be used to bind a using directive.</param>
        internal InContainerBinder GetImportsBinder(CSharpSyntaxNode unit, bool inUsing = false)
        {
            switch (unit.Kind())
            {
                case SyntaxKind.NamespaceDeclaration:
                    {
                        BinderFactoryVisitor visitor = _binderFactoryVisitorPool.Allocate();
                        visitor.Position = 0;
                        var result = visitor.VisitNamespaceDeclaration((NamespaceDeclarationSyntax)unit, unit.SpanStart, inBody: true, inUsing: inUsing);
                        _binderFactoryVisitorPool.Free(visitor);
                        return result;
                    }

                case SyntaxKind.CompilationUnit:
                    // imports are bound by the Script class binder:
                    {
                        BinderFactoryVisitor visitor = _binderFactoryVisitorPool.Allocate();
                        visitor.Position = 0;
                        var result = visitor.VisitCompilationUnit((CompilationUnitSyntax)unit, inUsing: inUsing, inScript: InScript);
                        _binderFactoryVisitorPool.Free(visitor);
                        return result;
                    }

                default:
                    return null;
            }
        }
    }
}
