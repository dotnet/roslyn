// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// This binder is responsible for introducing the 'cancellationToken' variable
    /// in async-iterator methods.
    /// </summary>
    internal sealed class CancellationTokenLocalScopeBinder : LocalScopeBinder
    {
        private readonly SourceMethodSymbol _methodSymbol;

        public CancellationTokenLocalScopeBinder(SourceMethodSymbol owner, Binder enclosing)
            : base(enclosing, enclosing.Flags & ~BinderFlags.AllClearedAtExecutableCodeBoundary)
        {
            Debug.Assert(!enclosing.Flags.Includes(BinderFlags.InCatchFilter));
            Debug.Assert((object)owner != null);
            Debug.Assert(enclosing is InMethodBinder);
            _methodSymbol = owner;
        }

        protected override SourceLocalSymbol LookupLocal(SyntaxToken nameToken)
            => throw ExceptionUtilities.Unreachable;

        protected override LocalFunctionSymbol LookupLocalFunction(SyntaxToken nameToken)
            => null;

        internal override uint LocalScopeDepth
            => Binder.TopLevelScope;

        protected override bool InExecutableBinder
            => true;

        internal override Symbol ContainingMemberOrLambda
            => _methodSymbol;

        internal override bool IsInMethodBody
            => true;

        internal override bool IsNestedFunctionBinder
            => _methodSymbol.MethodKind == MethodKind.LocalFunction;

        internal override bool IsDirectlyInIterator
            => Next.IsDirectlyInIterator;

        internal override bool IsIndirectlyInIterator
            => Next.IsIndirectlyInIterator;

        internal override GeneratedLabelSymbol BreakLabel
            => null;

        internal override GeneratedLabelSymbol ContinueLabel
            => null;

        internal override TypeWithAnnotations GetIteratorElementType(YieldStatementSyntax node, DiagnosticBag diagnostics)
            => Next.GetIteratorElementType(node, diagnostics);

        protected override ImmutableArray<LocalSymbol> BuildLocals()
        {
            if (_methodSymbol.GetCancellationTokenLocal() is { } cancellationTokenLocal)
            {
                return ImmutableArray.Create<LocalSymbol>(cancellationTokenLocal);
            }
            return ImmutableArray<LocalSymbol>.Empty;
        }

        internal override void LookupSymbolsInSingleBinder(
            LookupResult result, string name, int arity, ConsList<TypeSymbol> basesBeingResolved, LookupOptions options, Binder originalBinder, bool diagnose, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert(result.IsClear);

            if (name != SourceOrdinaryMethodSymbol.AsyncIteratorCancellationTokenLocal ||
                (options & LookupOptions.NamespaceAliasesOnly) != 0)
            {
                return;
            }

            if (_methodSymbol.GetCancellationTokenLocal() is { } cancellationTokenLocal)
            {
                result.MergeEqual(originalBinder.CheckViability(cancellationTokenLocal, arity, options, null, diagnose, ref useSiteDiagnostics));
            }
        }

        protected override void AddLookupSymbolsInfoInSingleBinder(LookupSymbolsInfo result, LookupOptions options, Binder originalBinder)
        {
            if (options.CanConsiderLocals() && _methodSymbol.GetCancellationTokenLocal() is { } cancellationTokenLocal)
            {
                if (originalBinder.CanAddLookupSymbolInfo(cancellationTokenLocal, options, result, null))
                {
                    result.AddSymbol(cancellationTokenLocal, cancellationTokenLocal.Name, 0);
                }
            }
        }

        internal override bool EnsureSingleDefinition(Symbol symbol, string name, Location location, DiagnosticBag diagnostics)
        {
            if (name == SourceOrdinaryMethodSymbol.AsyncIteratorCancellationTokenLocal &&
                _methodSymbol.GetCancellationTokenLocal() is { } existingSymbol)
            {
                if (symbol == existingSymbol)
                {
                    return false;
                }

                return ReportConflictWithLocal(existingSymbol, symbol, name, location, diagnostics);
            }

            return false;
        }
    }
}
