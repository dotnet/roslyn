// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a local variable in a method body.
    /// </summary>
    internal class SourceLocalSymbol : LocalSymbol
    {
        protected readonly Binder binder;

        /// <summary>
        /// Might not be a method symbol.
        /// </summary>
        private readonly Symbol containingSymbol;

        private readonly SyntaxToken identifierToken;
        private readonly ImmutableArray<Location> locations;
        private readonly TypeSyntax typeSyntax;
        private readonly LocalDeclarationKind declarationKind;
        protected TypeSymbol type;

        /// <summary>
        /// There are three ways to initialize a fixed statement local:
        ///   1) with an address;
        ///   2) with an array (or fixed-size buffer); or
        ///   3) with a string.
        /// 
        /// In the first two cases, the resulting local will be emitted with a "pinned" modifier.
        /// In the third case, it is not the fixed statement local but a synthesized temp that is pinned.  
        /// Unfortunately, we can't distinguish these cases when the local is declared; we only know
        /// once we have bound the initializer.
        /// </summary>
        /// <remarks>
        /// CompareExchange doesn't support bool, so use an int.  First bit is true/false, second bit 
        /// is read/unread (debug-only).
        /// </remarks>
        private int isSpecificallyNotPinned;

        public static SourceLocalSymbol MakeForeachLocal(
            MethodSymbol containingMethod,
            ForEachLoopBinder binder,
            TypeSyntax typeSyntax,
            SyntaxToken identifierToken,
            ExpressionSyntax collection)
        {
            return new ForEachLocal(containingMethod, binder, typeSyntax, identifierToken, collection, LocalDeclarationKind.ForEachIterationVariable);
        }

        public static SourceLocalSymbol MakeLocalWithInitializer(
            Symbol containingSymbol,
            Binder binder,
            TypeSyntax typeSyntax,
            SyntaxToken identifierToken,
            EqualsValueClauseSyntax initializer,
            LocalDeclarationKind declarationKind)
        {
            Debug.Assert(declarationKind != LocalDeclarationKind.ForEachIterationVariable);
            return new LocalWithInitializer(containingSymbol, binder, typeSyntax, identifierToken, initializer, declarationKind);
        }

        public static SourceLocalSymbol MakePossibleOutVarLocalWithoutInitializer(
            Symbol containingSymbol,
            Binder binder,
            TypeSyntax typeSyntax,
            SyntaxToken identifierToken,
            CSharpSyntaxNode scopeSegmentRoot,
            LocalDeclarationKind declarationKind)
        {
            Debug.Assert(declarationKind != LocalDeclarationKind.ForEachIterationVariable);
            return new PossibleOutVarLocalWithoutInitializer(containingSymbol, binder, typeSyntax, identifierToken, scopeSegmentRoot, declarationKind);
        }

        public static SourceLocalSymbol MakeLocal(
            Symbol containingSymbol,
            Binder binder,
            TypeSyntax typeSyntax,
            SyntaxToken identifierToken,
            LocalDeclarationKind declarationKind)
        {
            Debug.Assert(declarationKind != LocalDeclarationKind.ForEachIterationVariable);
            return new SourceLocalSymbol(containingSymbol, binder, typeSyntax, identifierToken, declarationKind);
        }

        private SourceLocalSymbol(
            Symbol containingSymbol,
            Binder binder,
            TypeSyntax typeSyntax,
            SyntaxToken identifierToken,
            LocalDeclarationKind declarationKind)
        {
            Debug.Assert(identifierToken.CSharpKind() != SyntaxKind.None);
            Debug.Assert(declarationKind != LocalDeclarationKind.None);

            this.binder = binder;
            this.containingSymbol = containingSymbol;
            this.identifierToken = identifierToken;
            this.typeSyntax = typeSyntax;
            this.declarationKind = declarationKind;

            // create this eagerly as it will always be needed for the EnsureSingleDefinition
            this.locations = ImmutableArray.Create<Location>(identifierToken.GetLocation());
        }

        internal override LocalDeclarationKind DeclarationKind
        {
            get { return this.declarationKind; }
        }

        internal override SynthesizedLocalKind SynthesizedLocalKind
        {
            get { return SynthesizedLocalKind.None; }
        }

        internal override bool IsPinned
        {
            get
            {
#if DEBUG
                if ((this.isSpecificallyNotPinned & 2) == 0)
                {
                    Interlocked.CompareExchange(ref this.isSpecificallyNotPinned, this.isSpecificallyNotPinned | 2, this.isSpecificallyNotPinned);
                    Debug.Assert((this.isSpecificallyNotPinned & 2) == 2, "Regardless of which thread won, the read bit should be set.");
                }
#endif
                return this.declarationKind == LocalDeclarationKind.FixedVariable && (isSpecificallyNotPinned & 1) == 0;
            }
        }

        internal void SetSpecificallyNotPinned()
        {
            Debug.Assert((this.isSpecificallyNotPinned & 2) == 0, "Shouldn't be writing after first read.");
            Interlocked.CompareExchange(ref this.isSpecificallyNotPinned, this.isSpecificallyNotPinned | 1, this.isSpecificallyNotPinned);
            Debug.Assert((this.isSpecificallyNotPinned & 1) == 1, "Regardless of which thread won, the flag bit should be set.");
        }

        public override Symbol ContainingSymbol
        {
            get { return this.containingSymbol; }
        }

        /// <summary>
        /// Gets the name of the local variable.
        /// </summary>
        public override string Name
        {
            get
            {
                return this.identifierToken.ValueText;
            }
        }

        // Get the identifier token that defined this local symbol. This is useful for robustly
        // checking if a local symbol actually matches a particular definition, even in the presence
        // of duplicates.
        internal override SyntaxToken IdentifierToken
        {
            get
            {
                return identifierToken;
            }
        }

        public override TypeSymbol Type
        {
            get
            {
                if ((object)this.type == null)
                {
                    TypeSymbol localType = GetTypeSymbol();
                    SetTypeSymbol(localType);
                }

                return this.type;
            }
        }

        public bool IsVar
        {
            get
            {
                if (typeSyntax.IsVar)
                {
                    bool isVar;
                    TypeSymbol declType = this.binder.BindType(typeSyntax, new DiagnosticBag(), out isVar);
                    return isVar;
                }

                return false;
            }
        }

        private TypeSymbol GetTypeSymbol()
        {
            var diagnostics = DiagnosticBag.GetInstance();

            Binder typeBinder = this.binder;

            bool isVar;
            TypeSymbol declType = typeBinder.BindType(typeSyntax, diagnostics, out isVar);

            if (isVar)
            {
                TypeSymbol inferredType = InferTypeOfVarVariable(diagnostics);

                // If we got a valid result that was not void then use the inferred type
                // else create an error type.
                if ((object)inferredType != null &&
                    inferredType.SpecialType != SpecialType.System_Void)
                {
                    declType = inferredType;
                }
                else
                {
                    declType = typeBinder.CreateErrorType("var");
                }
            }

            Debug.Assert((object)declType != null);

            diagnostics.Free();
            return declType;
        }

        protected virtual TypeSymbol InferTypeOfVarVariable(DiagnosticBag diagnostics)
        {
            return null;
        }

        internal void SetTypeSymbol(TypeSymbol newType)
        {
            TypeSymbol originalType = this.type;

            // In the event that we race to set the type of a local, we should
            // always deduce the same type, or deduce that the type is an error.

            Debug.Assert((object)originalType == null ||
                originalType.IsErrorType() && newType.IsErrorType() ||
                originalType == newType);

            if ((object)originalType == null)
            {
                Interlocked.CompareExchange(ref this.type, newType, null);
            }
        }

        /// <summary>
        /// Gets the locations where the local symbol was originally defined in source.
        /// There should not be local symbols from metadata, and there should be only one local variable declared.
        /// TODO: check if there are multiple same name local variables - error symbol or local symbol?
        /// </summary>
        public override ImmutableArray<Location> Locations
        {
            get
            {
                return locations;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                CSharpSyntaxNode node;

                switch (declarationKind)
                {
                    case LocalDeclarationKind.RegularVariable:
                    case LocalDeclarationKind.Constant:
                    case LocalDeclarationKind.FixedVariable:
                    case LocalDeclarationKind.UsingVariable:
                    case LocalDeclarationKind.CatchVariable:
                    case LocalDeclarationKind.ForInitializerVariable:
                        node = identifierToken.Parent.FirstAncestorOrSelf<VariableDeclaratorSyntax>();
                        break;

                    case LocalDeclarationKind.ForEachIterationVariable:
                        node = identifierToken.Parent.FirstAncestorOrSelf<ForEachStatementSyntax>();
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(declarationKind);
                }

                return (node == null) ? ImmutableArray<SyntaxReference>.Empty : ImmutableArray.Create<SyntaxReference>(node.GetReference());
            }
        }

        internal override bool IsCompilerGenerated
        {
            get { return false; }
        }

        internal override ConstantValue GetConstantValue(LocalSymbol inProgress)
        {
            return null;
        }

        internal override ImmutableArray<Diagnostic> GetConstantValueDiagnostics(BoundExpression boundInitValue)
        {
            return default(ImmutableArray<Diagnostic>);
        }

        internal override RefKind RefKind
        {
            get { return RefKind.None; }
        }

        public sealed override bool Equals(object obj)
        {
            if (obj == (object)this)
            {
                return true;
            }

            var symbol = obj as SourceLocalSymbol;
            return (object)symbol != null
                && symbol.identifierToken.Equals(this.identifierToken)
                && Equals(symbol.containingSymbol, this.containingSymbol);
        }

        public sealed override int GetHashCode()
        {
            return Hash.Combine(this.identifierToken.GetHashCode(), this.containingSymbol.GetHashCode());
        }

        private class LocalWithInitializer : SourceLocalSymbol
        {
            private readonly EqualsValueClauseSyntax initializer;

            /// <summary>
            /// Store the constant value and the corresponding diagnostics together
            /// to avoid having the former set by one thread and the latter set by
            /// another.
            /// </summary>
            private EvaluatedConstant constantTuple;

            public LocalWithInitializer(
                Symbol containingSymbol,
                Binder binder,
                TypeSyntax typeSyntax,
                SyntaxToken identifierToken,
                EqualsValueClauseSyntax initializer,
                LocalDeclarationKind declarationKind) :
                    base(containingSymbol, binder, typeSyntax, identifierToken, declarationKind)
            {
                if (initializer == null)
                {
                    throw ExceptionUtilities.Unreachable;
                }
                
                this.initializer = initializer;
            }

            protected override TypeSymbol InferTypeOfVarVariable(DiagnosticBag diagnostics)
            {
                var newBinder = new ImplicitlyTypedLocalBinder(this.binder, this);
                var initializerOpt = newBinder.BindInferredVariableInitializer(diagnostics, initializer, initializer);
                if (initializerOpt != null)
                {
                    return initializerOpt.Type;
        }

                return null;
            }

        /// <summary>
        /// Determine the constant value of this local and the corresponding diagnostics.
        /// Set both to constantTuple in a single operation for thread safety.
        /// </summary>
        /// <param name="inProgress">Null for the initial call, non-null if we are in the process of evaluating a constant.</param>
        /// <param name="boundInitValue">If we already have the bound node for the initial value, pass it in to avoid recomputing it.</param>
        private void MakeConstantTuple(LocalSymbol inProgress, BoundExpression boundInitValue)
        {
                if (this.IsConst && this.constantTuple == null)
            {
                var value = Microsoft.CodeAnalysis.ConstantValue.Bad;
                var initValueNodeLocation = this.initializer.Value.Location;
                var diagnostics = DiagnosticBag.GetInstance();
                if (inProgress == this)
                {
                    // The problem is circularity, but Dev12 reports ERR_NotConstantExpression instead of ERR_CircConstValue.
                    // Also, the native compiler squiggles the RHS for ERR_CircConstValue but the LHS for ERR_CircConstValue.
                    diagnostics.Add(ErrorCode.ERR_NotConstantExpression, initValueNodeLocation, this);
                }
                else
                {
                    var type = this.Type;
                    if (boundInitValue == null)
                    {
                        var inProgressBinder = new LocalInProgressBinder(this, this.binder);
                        boundInitValue = inProgressBinder.BindVariableOrAutoPropInitializer(this.initializer, type, diagnostics);
                    }

                    value = ConstantValueUtils.GetAndValidateConstantValue(boundInitValue, this, type, initValueNodeLocation, diagnostics);
                }
                Interlocked.CompareExchange(ref this.constantTuple, new EvaluatedConstant(value, diagnostics.ToReadOnlyAndFree()), null);
            }
        }

            internal override ConstantValue GetConstantValue(LocalSymbol inProgress)
        {
                MakeConstantTuple(inProgress, boundInitValue: null);
                return this.constantTuple == null ? null : this.constantTuple.Value;
            }

            internal override ImmutableArray<Diagnostic> GetConstantValueDiagnostics(BoundExpression boundInitValue)
            {
                Debug.Assert(boundInitValue != null);
                MakeConstantTuple(inProgress: null, boundInitValue: boundInitValue);
                return this.constantTuple == null ? default(ImmutableArray<Diagnostic>) : this.constantTuple.Diagnostics;
            }
        }

        private class ForEachLocal : SourceLocalSymbol
        {
            private readonly ExpressionSyntax collection;

            public ForEachLocal(
                Symbol containingSymbol,
                Binder binder,
                TypeSyntax typeSyntax,
                SyntaxToken identifierToken,
                ExpressionSyntax collection,
                LocalDeclarationKind declarationKind) :
                    base(containingSymbol, binder, typeSyntax, identifierToken, declarationKind)
            {
                Debug.Assert(declarationKind == LocalDeclarationKind.ForEachIterationVariable);
                this.collection = collection;
            }

            protected override TypeSymbol InferTypeOfVarVariable(DiagnosticBag diagnostics)
            {
                // Normally, it would not be safe to cast to a specific binder type.  However, we verified the type
                // in the factory method call for this symbol.
                return ((ForEachLoopBinder)this.binder).InferCollectionElementType(diagnostics, collection);
            }
        }

        private class PossibleOutVarLocalWithoutInitializer : SourceLocalSymbol
        {
            private readonly CSharpSyntaxNode scopeSegmentRoot;

            public PossibleOutVarLocalWithoutInitializer(
                Symbol containingSymbol,
                Binder binder,
                TypeSyntax typeSyntax,
                SyntaxToken identifierToken,
                CSharpSyntaxNode scopeSegmentRoot,
                LocalDeclarationKind declarationKind) :
                    base(containingSymbol, binder, typeSyntax, identifierToken, declarationKind)
            {
                Debug.Assert(identifierToken.Parent.CSharpKind() == SyntaxKind.VariableDeclarator && ((VariableDeclaratorSyntax)identifierToken.Parent).Identifier == identifierToken);
                Debug.Assert(identifierToken.Parent.Parent.CSharpKind() == SyntaxKind.DeclarationExpression);

                if (scopeSegmentRoot == null)
                {
                    throw ExceptionUtilities.Unreachable;
                }

                this.scopeSegmentRoot = scopeSegmentRoot;
        }

            protected override TypeSymbol InferTypeOfVarVariable(DiagnosticBag diagnostics)
            {
                // Try binding immediately enclosing invocation expression, this should force the inference.
                SyntaxNode node = identifierToken.Parent.Parent;
                Debug.Assert(node.CSharpKind() == SyntaxKind.DeclarationExpression);

                // Skip parenthesized and checked/unchecked expressions
                while (node != scopeSegmentRoot && node.Parent != null)
        {
                    switch (node.Parent.CSharpKind())
            {
                        case SyntaxKind.ParenthesizedExpression:
                        case SyntaxKind.CheckedExpression:
                        case SyntaxKind.UncheckedExpression:
                            node = node.Parent;
                            continue;
                    }

                    break;
            }

                if (node != scopeSegmentRoot && node.Parent != null && node.Parent.CSharpKind() == SyntaxKind.Argument)
                {
                    node = node.Parent;

                    if (node != scopeSegmentRoot && node.Parent != null && node.Parent.CSharpKind() == SyntaxKind.ArgumentList)
                    {
                        node = node.Parent;

                        if (node != scopeSegmentRoot)
                        {
                            if (node.Parent != null)
                            {
                                node = node.Parent;

                                switch (node.CSharpKind())
                                {
                                    case SyntaxKind.InvocationExpression:
                                    case SyntaxKind.ObjectCreationExpression:
                                        this.binder.BindExpression((ExpressionSyntax)node, diagnostics);
                                        var result = this.type;
                                        Debug.Assert((object)result != null);
                                        return result;
                                }
                            }
        }
                        else if (node.Parent != null)
                        {
                            Debug.Assert(node.CSharpKind() == SyntaxKind.ArgumentList);
                            Debug.Assert(node == scopeSegmentRoot);
                            // This could be an argument list for a constructor initializer

                            switch (node.Parent.CSharpKind())
        {
                                case SyntaxKind.ThisConstructorInitializer:
                                case SyntaxKind.BaseConstructorInitializer:
                                    this.binder.BindConstructorInitializer((ArgumentListSyntax)node, (MethodSymbol)this.binder.ContainingMember(), diagnostics);
                                    var result = this.type;
                                    Debug.Assert((object)result != null);
                                    return result;
                            }
                        }
                    }
                }

                return null;
            }
        }
    }
}
