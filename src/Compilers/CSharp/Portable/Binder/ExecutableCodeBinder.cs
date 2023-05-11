// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
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

            if (_memberSymbol is SynthesizedSimpleProgramEntryPointSymbol entryPoint && _root == entryPoint.SyntaxNode)
            {
                var scopeOwner = new SimpleProgramBinder(this, entryPoint);
                map = LocalBinderFactory.BuildMap(_memberSymbol, _root, scopeOwner, _binderUpdatedHandler);
                map.Add(_root, scopeOwner);
            }
            else
            {
                // Ensure that the member symbol is a method symbol.
                if ((object)_memberSymbol != null && _root != null)
                {
                    map = LocalBinderFactory.BuildMap(_memberSymbol, _root, this, _binderUpdatedHandler);
                }
                else
                {
                    map = SmallDictionary<SyntaxNode, Binder>.Empty;
                }
            }

            Interlocked.CompareExchange(ref _lazyBinderMap, map, null);
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

        public static void ValidateIteratorMethod(CSharpCompilation compilation, MethodSymbol iterator, BindingDiagnosticBag diagnostics)
        {
            if (!iterator.IsIterator)
            {
                return;
            }

            foreach (var parameter in iterator.Parameters)
            {
                if (parameter.RefKind != RefKind.None)
                {
                    diagnostics.Add(ErrorCode.ERR_BadIteratorArgType, parameter.GetFirstLocation());
                }
                else if (parameter.Type.IsPointerOrFunctionPointer())
                {
                    diagnostics.Add(ErrorCode.ERR_UnsafeIteratorArgType, parameter.GetFirstLocation());
                }
            }

            Location errorLocation = (iterator as SynthesizedSimpleProgramEntryPointSymbol)?.ReturnTypeSyntax.GetLocation() ?? iterator.GetFirstLocation();
            if (iterator.IsVararg)
            {
                // error CS1636: __arglist is not allowed in the parameter list of iterators
                diagnostics.Add(ErrorCode.ERR_VarargsIterator, errorLocation);
            }

            if (((iterator as SourceMemberMethodSymbol)?.IsUnsafe == true || (iterator as LocalFunctionSymbol)?.IsUnsafe == true)
                && compilation.Options.AllowUnsafe) // Don't cascade
            {
                diagnostics.Add(ErrorCode.ERR_IllegalInnerUnsafe, errorLocation);
            }

            var returnType = iterator.ReturnType;
            RefKind refKind = iterator.RefKind;
            TypeWithAnnotations elementType = InMethodBinder.GetIteratorElementTypeFromReturnType(compilation, refKind, returnType, errorLocation, diagnostics);

            if (elementType.IsDefault)
            {
                if (refKind != RefKind.None)
                {
                    Error(diagnostics, ErrorCode.ERR_BadIteratorReturnRef, errorLocation, iterator);
                }
                else if (!returnType.IsErrorType())
                {
                    Error(diagnostics, ErrorCode.ERR_BadIteratorReturn, errorLocation, iterator, returnType);
                }
            }

            bool asyncInterface = InMethodBinder.IsAsyncStreamInterface(compilation, refKind, returnType);
            if (asyncInterface && !iterator.IsAsync)
            {
                diagnostics.Add(ErrorCode.ERR_IteratorMustBeAsync, errorLocation, iterator, returnType);
            }
        }
    }
}
