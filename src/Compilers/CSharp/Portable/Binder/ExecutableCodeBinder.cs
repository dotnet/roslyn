﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// This binder owns and lazily creates the map of SyntaxNodes to Binders associated with
    /// the syntax with which it is created. This binder is not created in reaction to any
    /// specific syntax node type. It is inserted into the binder chain
    /// between the binder which it is constructed with and those that it constructs via
    /// the LocalBinderFactory. 
    /// </summary>
    internal sealed class ExecutableCodeBinder : Binder
    {
        private readonly Symbol _memberSymbol;
        private readonly CSharpSyntaxNode _root;
        private SmallDictionary<CSharpSyntaxNode, Binder> _lazyBinderMap;
        private ImmutableArray<MethodSymbol> _methodSymbolsWithYield;

        internal ExecutableCodeBinder(CSharpSyntaxNode root, Symbol memberSymbol, Binder next)
            : this(root, memberSymbol, next, next.Flags)
        {
        }

        internal ExecutableCodeBinder(CSharpSyntaxNode root, Symbol memberSymbol, Binder next, BinderFlags additionalFlags)
            : base(next, (next.Flags | additionalFlags) & ~BinderFlags.AllClearedAtExecutableCodeBoundary)
        {
            Debug.Assert((object)memberSymbol == null ||
                         (memberSymbol.Kind != SymbolKind.Local && memberSymbol.Kind != SymbolKind.RangeVariable && memberSymbol.Kind != SymbolKind.Parameter));

            _memberSymbol = memberSymbol;
            _root = root;
        }

        internal override Symbol ContainingMemberOrLambda
        {
            get { return _memberSymbol ?? Next.ContainingMemberOrLambda; }
        }

        internal Symbol MemberSymbol { get { return _memberSymbol; } }

        internal override Binder GetBinder(CSharpSyntaxNode node)
        {
            Binder binder;
            return this.BinderMap.TryGetValue(node, out binder) ? binder : Next.GetBinder(node);
        }

        private void ComputeBinderMap()
        {
            SmallDictionary<CSharpSyntaxNode, Binder> map;
            ImmutableArray<MethodSymbol> methodSymbolsWithYield;

            // Ensure that the member symbol is a method symbol.
            if ((object)_memberSymbol != null && _root != null)
            {
                var methodsWithYield = ArrayBuilder<CSharpSyntaxNode>.GetInstance();
                var symbolsWithYield = ArrayBuilder<MethodSymbol>.GetInstance();
                map = LocalBinderFactory.BuildMap(_memberSymbol, _root, this, methodsWithYield);
                foreach (var methodWithYield in methodsWithYield)
                {
                    Binder binder;
                    if (map.TryGetValue(methodWithYield, out binder))
                    {
                        Symbol containing = binder.ContainingMemberOrLambda;

                        // get the closest inclosing InMethodBinder and make it an iterator
                        InMethodBinder inMethod = null;
                        while (binder != null)
                        {
                            inMethod = binder as InMethodBinder;
                            if (inMethod != null)
                                break;
                            binder = binder.Next;
                        }
                        if (inMethod != null && (object)inMethod.ContainingMemberOrLambda == containing)
                        {
                            inMethod.MakeIterator();
                            symbolsWithYield.Add((MethodSymbol)inMethod.ContainingMemberOrLambda);
                        }
                        else
                        {
                            Debug.Assert(methodWithYield == _root && methodWithYield is ExpressionSyntax);
                        }
                    }
                    else
                    {
                        // skip over it, this is an error
                    }
                }
                methodsWithYield.Free();
                methodSymbolsWithYield = symbolsWithYield.ToImmutableAndFree();
            }
            else
            {
                map = SmallDictionary<CSharpSyntaxNode, Binder>.Empty;
                methodSymbolsWithYield = ImmutableArray<MethodSymbol>.Empty;
            }

            Interlocked.CompareExchange(ref _lazyBinderMap, map, null);
            ImmutableInterlocked.InterlockedCompareExchange(ref _methodSymbolsWithYield, methodSymbolsWithYield, default(ImmutableArray<MethodSymbol>));
        }

        private SmallDictionary<CSharpSyntaxNode, Binder> BinderMap
        {
            get
            {
                if (_lazyBinderMap == null)
                {
                    ComputeBinderMap();
                }

                return _lazyBinderMap;
            }
        }

        public ImmutableArray<MethodSymbol> MethodSymbolsWithYield
        {
            get
            {
                if (_methodSymbolsWithYield.IsDefault)
                {
                    ComputeBinderMap();
                }

                return _methodSymbolsWithYield;
            }
        }
    }
}
