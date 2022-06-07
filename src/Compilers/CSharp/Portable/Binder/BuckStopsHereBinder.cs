// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A binder that knows no symbols and will not delegate further.
    /// </summary>
    internal class BuckStopsHereBinder : Binder
    {
        internal BuckStopsHereBinder(CSharpCompilation compilation)
            : base(compilation)
        {
        }

        internal override ImportChain? ImportChain
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Get <see cref="QuickAttributeChecker"/> that can be used to quickly
        /// check for certain attribute applications in context of this binder.
        /// </summary>
        internal override QuickAttributeChecker QuickAttributeChecker
        {
            get
            {
                return QuickAttributeChecker.Predefined;
            }
        }

        protected override SourceLocalSymbol? LookupLocal(SyntaxToken nameToken)
        {
            return null;
        }

        protected override LocalFunctionSymbol? LookupLocalFunction(SyntaxToken nameToken)
        {
            return null;
        }

        internal override uint LocalScopeDepth => Binder.ExternalScope;

        protected override bool InExecutableBinder => false;
        protected override SyntaxNode? EnclosingNameofArgument => null;
        internal override bool IsInsideNameof => false;

        internal override bool IsAccessibleHelper(Symbol symbol, TypeSymbol accessThroughType, out bool failedThroughTypeCheck, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo, ConsList<TypeSymbol> basesBeingResolved)
        {
            failedThroughTypeCheck = false;
            return IsSymbolAccessibleConditional(symbol, Compilation.Assembly, ref useSiteInfo);
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

        internal override LocalSymbol? LocalInProgress
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

        internal override GeneratedLabelSymbol? BreakLabel
        {
            get
            {
                return null;
            }
        }

        internal override GeneratedLabelSymbol? ContinueLabel
        {
            get
            {
                return null;
            }
        }

        internal override BoundExpression? ConditionalReceiverExpression
        {
            get
            {
                return null;
            }
        }

        // This should only be called in the context of syntactically incorrect programs.  In other
        // contexts statements are surrounded by some enclosing method or lambda.
        internal override TypeWithAnnotations GetIteratorElementType()
        {
            // There's supposed to be an enclosing method or lambda.
            throw ExceptionUtilities.Unreachable;
        }

        internal override Symbol? ContainingMemberOrLambda
        {
            get
            {
                return null;
            }
        }

        internal override bool AreNullableAnnotationsGloballyEnabled()
        {
            return GetGlobalAnnotationState();
        }

        internal override Binder? GetBinder(SyntaxNode node)
        {
            return null;
        }

        internal override ImmutableArray<LocalSymbol> GetDeclaredLocalsForScope(SyntaxNode scopeDesignator)
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override ImmutableArray<LocalFunctionSymbol> GetDeclaredLocalFunctionsForScope(CSharpSyntaxNode scopeDesignator)
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override BoundStatement BindSwitchStatementCore(SwitchStatementSyntax node, Binder originalBinder, BindingDiagnosticBag diagnostics)
        {
            // There's supposed to be a SwitchBinder (or other overrider of this method) in the chain.
            throw ExceptionUtilities.Unreachable;
        }

        internal override BoundExpression BindSwitchExpressionCore(SwitchExpressionSyntax node, Binder originalBinder, BindingDiagnosticBag diagnostics)
        {
            // There's supposed to be a SwitchExpressionBinder (or other overrider of this method) in the chain.
            throw ExceptionUtilities.Unreachable;
        }

        internal override void BindPatternSwitchLabelForInference(CasePatternSwitchLabelSyntax node, BindingDiagnosticBag diagnostics)
        {
            // There's supposed to be a SwitchBinder (or other overrider of this method) in the chain.
            throw ExceptionUtilities.Unreachable;
        }

        internal override BoundSwitchExpressionArm BindSwitchExpressionArm(SwitchExpressionArmSyntax node, TypeSymbol switchGoverningType, uint switchGoverningValEscape, BindingDiagnosticBag diagnostics)
        {
            // There's supposed to be an overrider of this method (e.g. SwitchExpressionArmBinder) for the arm in the chain.
            throw ExceptionUtilities.Unreachable;
        }

        internal override BoundForStatement BindForParts(BindingDiagnosticBag diagnostics, Binder originalBinder)
        {
            // There's supposed to be a ForLoopBinder (or other overrider of this method) in the chain.
            throw ExceptionUtilities.Unreachable;
        }

        internal override BoundStatement BindForEachParts(BindingDiagnosticBag diagnostics, Binder originalBinder)
        {
            // There's supposed to be a ForEachLoopBinder (or other overrider of this method) in the chain.
            throw ExceptionUtilities.Unreachable;
        }

        internal override BoundStatement BindForEachDeconstruction(BindingDiagnosticBag diagnostics, Binder originalBinder)
        {
            // There's supposed to be a ForEachLoopBinder (or other overrider of this method) in the chain.
            throw ExceptionUtilities.Unreachable;
        }

        internal override BoundWhileStatement BindWhileParts(BindingDiagnosticBag diagnostics, Binder originalBinder)
        {
            // There's supposed to be a WhileBinder (or other overrider of this method) in the chain.
            throw ExceptionUtilities.Unreachable;
        }

        internal override BoundDoStatement BindDoParts(BindingDiagnosticBag diagnostics, Binder originalBinder)
        {
            // There's supposed to be a WhileBinder (or other overrider of this method) in the chain.
            throw ExceptionUtilities.Unreachable;
        }

        internal override BoundStatement BindUsingStatementParts(BindingDiagnosticBag diagnostics, Binder originalBinder)
        {
            // There's supposed to be a UsingStatementBinder (or other overrider of this method) in the chain.
            throw ExceptionUtilities.Unreachable;
        }

        internal override BoundStatement BindLockStatementParts(BindingDiagnosticBag diagnostics, Binder originalBinder)
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
