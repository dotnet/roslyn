// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using System.Collections.Immutable;
using System.Diagnostics;

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
        private readonly MethodSymbol _owner;
        private SmallDictionary<CSharpSyntaxNode, Binder> _lazyBinderMap;
        private ImmutableArray<MethodSymbol> _methodSymbolsWithYield;

        internal ExecutableCodeBinder(CSharpSyntaxNode root, Symbol memberSymbol, Binder next)
            : this(root, memberSymbol, next, next.Flags)
        {
        }

        internal ExecutableCodeBinder(CSharpSyntaxNode root, Symbol memberSymbol, Binder next, BinderFlags additionalFlags)
            : base(next, (next.Flags | additionalFlags) & ~BinderFlags.AllClearedAtExecutableCodeBoundary)
        {
            _memberSymbol = memberSymbol;
            _root = root;
            _owner = memberSymbol as MethodSymbol;
        }

        internal override Symbol ContainingMemberOrLambda
        {
            get { return _owner ?? Next.ContainingMemberOrLambda; }
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
            var methodSymbol = _owner;

            // Ensure that the member symbol is a method symbol.
            if ((object)methodSymbol != null && _root != null)
            {
                var methodsWithYield = ArrayBuilder<CSharpSyntaxNode>.GetInstance();
                var symbolsWithYield = ArrayBuilder<MethodSymbol>.GetInstance();
                map = LocalBinderFactory.BuildMap(methodSymbol, _root, this, methodsWithYield);
                foreach (var methodWithYield in methodsWithYield)
                {
                    Binder binder;
                    if (map.TryGetValue(methodWithYield, out binder))
                    {
                        // get the closest inclosing InMethodBinder and make it an iterator
                        InMethodBinder inMethod = null;
                        while (binder != null)
                        {
                            inMethod = binder as InMethodBinder;
                            if (inMethod != null)
                                break;
                            binder = binder.Next;
                        }
                        if (inMethod != null)
                        {
                            inMethod.MakeIterator();
                            symbolsWithYield.Add((MethodSymbol)inMethod.ContainingMemberOrLambda);
                        }
                        else
                        {
                            Debug.Assert(false);
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
