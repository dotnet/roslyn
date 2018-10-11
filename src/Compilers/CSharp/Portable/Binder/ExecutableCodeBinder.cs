// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using System;
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
        private readonly SyntaxNode _root;
        private readonly Action<Binder, SyntaxNode> _binderUpdatedHandler;
        private SmallDictionary<SyntaxNode, Binder> _lazyBinderMap;
        private ImmutableArray<MethodSymbol> _methodSymbolsWithYield;

        internal ExecutableCodeBinder(SyntaxNode root, Symbol memberSymbol, Binder next, Action<Binder, SyntaxNode> binderUpdatedHandler = null)
            : this(root, memberSymbol, next, next.Flags)
        {
            _binderUpdatedHandler = binderUpdatedHandler;
        }

        internal ExecutableCodeBinder(SyntaxNode root, Symbol memberSymbol, Binder next, BinderFlags additionalFlags)
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

        protected override bool InExecutableBinder
            => true;

        internal Symbol MemberSymbol { get { return _memberSymbol; } }

        internal override Binder GetBinder(SyntaxNode node)
        {
            Binder binder;
            return this.BinderMap.TryGetValue(node, out binder) ? binder : Next.GetBinder(node);
        }

        private void ComputeBinderMap()
        {
            SmallDictionary<SyntaxNode, Binder> map;
            ImmutableArray<MethodSymbol> methodSymbolsWithYield;

            // Ensure that the member symbol is a method symbol.
            if ((object)_memberSymbol != null && _root != null)
            {
                var methodsWithYield = ArrayBuilder<SyntaxNode>.GetInstance();
                var symbolsWithYield = ArrayBuilder<MethodSymbol>.GetInstance();
                map = LocalBinderFactory.BuildMap(_memberSymbol, _root, this, methodsWithYield, _binderUpdatedHandler);
                foreach (var methodWithYield in methodsWithYield)
                {
                    Binder binder = this;
                    if (methodWithYield.Kind() != SyntaxKind.GlobalStatement &&
                        (methodWithYield == _root || map.TryGetValue(methodWithYield, out binder)))
                    {
                        Symbol containing = binder.ContainingMemberOrLambda;

                        // get the closest enclosing InMethodBinder and make it an iterator
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
                map = SmallDictionary<SyntaxNode, Binder>.Empty;
                methodSymbolsWithYield = ImmutableArray<MethodSymbol>.Empty;
            }

            Interlocked.CompareExchange(ref _lazyBinderMap, map, null);
            ImmutableInterlocked.InterlockedCompareExchange(ref _methodSymbolsWithYield, methodSymbolsWithYield, default(ImmutableArray<MethodSymbol>));
        }

        private SmallDictionary<SyntaxNode, Binder> BinderMap
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

        private ImmutableArray<MethodSymbol> MethodSymbolsWithYield
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

        public void ValidateIteratorMethods(DiagnosticBag diagnostics)
        {
            foreach (var iterator in MethodSymbolsWithYield)
            {
                foreach (var parameter in iterator.Parameters)
                {
                    if (parameter.RefKind != RefKind.None)
                    {
                        diagnostics.Add(ErrorCode.ERR_BadIteratorArgType, parameter.Locations[0]);
                    }
                    else if (parameter.Type.IsUnsafe())
                    {
                        diagnostics.Add(ErrorCode.ERR_UnsafeIteratorArgType, parameter.Locations[0]);
                    }
                }

                if (iterator.IsVararg)
                {
                    // error CS1636: __arglist is not allowed in the parameter list of iterators
                    diagnostics.Add(ErrorCode.ERR_VarargsIterator, iterator.Locations[0]);
                }

                if (((iterator as SourceMemberMethodSymbol)?.IsUnsafe == true || (iterator as LocalFunctionSymbol)?.IsUnsafe == true)
                    && Compilation.Options.AllowUnsafe) // Don't cascade
                {
                    diagnostics.Add(ErrorCode.ERR_IllegalInnerUnsafe, iterator.Locations[0]);
                }
            }
        }
    }
}
