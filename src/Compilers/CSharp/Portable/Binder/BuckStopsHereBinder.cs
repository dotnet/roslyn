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
    /// A binder that knows no symbols and will not delegate further.
    /// </summary>
    internal partial class BuckStopsHereBinder : Binder
    {
        internal BuckStopsHereBinder(CSharpCompilation compilation)
            : base(compilation)
        {
        }

        public override ConsList<LocalSymbol> ImplicitlyTypedLocalsBeingBound
        {
            get
            {
                return ConsList<LocalSymbol>.Empty;
            }
        }

        internal override ImportChain ImportChain
        {
            get
            {
                return null;
            }
        }

        internal override Imports GetImports(ConsList<Symbol> basesBeingResolved)
        {
            return Imports.Empty;
        }

        protected override SourceLocalSymbol LookupLocal(SyntaxToken nameToken)
        {
            return null;
        }

        protected override LocalFunctionSymbol LookupLocalFunction(SyntaxToken nameToken)
        {
            return null;
        }

        internal override bool IsAccessibleHelper(Symbol symbol, TypeSymbol accessThroughType, out bool failedThroughTypeCheck, ref HashSet<DiagnosticInfo> useSiteDiagnostics, ConsList<Symbol> basesBeingResolved)
        {
            failedThroughTypeCheck = false;
            return this.IsSymbolAccessibleConditional(symbol, Compilation.Assembly, ref useSiteDiagnostics);
        }

        internal override ConstantFieldsInProgress ConstantFieldsInProgress
        {
            get
            {
                return ConstantFieldsInProgress.Empty;
            }
        }

        internal override ConsList<FieldSymbol> FieldsBeingBound
        {
            get
            {
                return ConsList<FieldSymbol>.Empty;
            }
        }

        internal override LocalSymbol LocalInProgress
        {
            get
            {
                return null;
            }
        }

        protected override bool IsUnboundTypeAllowed(GenericNameSyntax syntax)
        {
            return false;
        }

        internal override bool IsInMethodBody
        {
            get
            {
                return false;
            }
        }

        internal override bool IsDirectlyInIterator
        {
            get
            {
                return false;
            }
        }

        internal override bool IsIndirectlyInIterator
        {
            get
            {
                return false;
            }
        }

        internal override GeneratedLabelSymbol BreakLabel
        {
            get
            {
                return null;
            }
        }

        internal override GeneratedLabelSymbol ContinueLabel
        {
            get
            {
                return null;
            }
        }

        internal override BoundExpression ConditionalReceiverExpression
        {
            get
            {
                return null;
            }
        }

        // This should only be called in the context of syntactically incorrect programs.  In other
        // contexts statements are surrounded by some enclosing method or lambda.
        internal override TypeSymbol GetIteratorElementType(YieldStatementSyntax node, DiagnosticBag diagnostics)
        {
            // There's supposed to be an enclosing method or lambda.
            throw ExceptionUtilities.Unreachable;
        }

        internal override Symbol ContainingMemberOrLambda
        {
            get
            {
                return null;
            }
        }

        internal override Binder GetBinder(CSharpSyntaxNode node)
        {
            return null;
        }

        internal override ImmutableArray<LocalSymbol> GetDeclaredLocalsForScope(CSharpSyntaxNode scopeDesignator)
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override ImmutableArray<LocalFunctionSymbol> GetDeclaredLocalFunctionsForScope(CSharpSyntaxNode scopeDesignator)
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override BoundStatement BindSwitchExpressionAndSections(SwitchStatementSyntax node, Binder originalBinder, DiagnosticBag diagnostics)
        {
            // There's supposed to be a SwitchBinder (or other overrider of this method) in the chain.
            throw ExceptionUtilities.Unreachable;
        }

        internal override BoundForStatement BindForParts(DiagnosticBag diagnostics, Binder originalBinder)
        {
            // There's supposed to be a ForLoopBinder (or other overrider of this method) in the chain.
            throw ExceptionUtilities.Unreachable;
        }

        internal override BoundStatement BindForEachParts(DiagnosticBag diagnostics, Binder originalBinder)
        {
            // There's supposed to be a ForEachLoopBinder (or other overrider of this method) in the chain.
            throw ExceptionUtilities.Unreachable;
        }

        internal override BoundWhileStatement BindWhileParts(DiagnosticBag diagnostics, Binder originalBinder)
        {
            // There's supposed to be a WhileBinder (or other overrider of this method) in the chain.
            throw ExceptionUtilities.Unreachable;
        }

        internal override BoundDoStatement BindDoParts(DiagnosticBag diagnostics, Binder originalBinder)
        {
            // There's supposed to be a WhileBinder (or other overrider of this method) in the chain.
            throw ExceptionUtilities.Unreachable;
        }

        internal override BoundStatement BindUsingStatementParts(DiagnosticBag diagnostics, Binder originalBinder)
        {
            // There's supposed to be a UsingStatementBinder (or other overrider of this method) in the chain.
            throw ExceptionUtilities.Unreachable;
        }

        internal override BoundStatement BindLockStatementParts(DiagnosticBag diagnostics, Binder originalBinder)
        {
            // There's supposed to be a LockBinder (or other overrider of this method) in the chain.
            throw ExceptionUtilities.Unreachable;
        }

        internal override ImmutableHashSet<Symbol> LockedOrDisposedVariables
        {
            get { return ImmutableHashSet.Create<Symbol>(); }
        }
    }
}
