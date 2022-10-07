// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{

    internal sealed class SpeculativeSemanticModelWithMemberModel : PublicSemanticModel
    {
        private readonly SyntaxTreeSemanticModel _parentSemanticModel;
        private readonly int _position;
        private readonly NullableWalker.SnapshotManager? _parentSnapshotManagerOpt;
        private readonly MemberSemanticModel _memberModel;

        private SpeculativeSemanticModelWithMemberModel(
            SyntaxTreeSemanticModel parentSemanticModel,
            int position,
            NullableWalker.SnapshotManager? snapshotManagerOpt)
        {
            Debug.Assert(parentSemanticModel is not null);

            _parentSemanticModel = parentSemanticModel;
            _position = position;
            _parentSnapshotManagerOpt = snapshotManagerOpt;
            _memberModel = null!;
        }

        public SpeculativeSemanticModelWithMemberModel(
            SyntaxTreeSemanticModel parentSemanticModel,
            int position,
            AttributeSyntax syntax,
            NamedTypeSymbol attributeType,
            AliasSymbol aliasOpt,
            Binder rootBinder,
            ImmutableDictionary<Symbol, Symbol>? parentRemappedSymbolsOpt)
            : this(parentSemanticModel, position, snapshotManagerOpt: null)
        {
            Debug.Assert(syntax != null);
            Debug.Assert(rootBinder != null);
            Debug.Assert(rootBinder.IsSemanticModelBinder);

            _memberModel = new AttributeSemanticModel(syntax, attributeType, aliasOpt, rootBinder, containingPublicSemanticModel: this, parentRemappedSymbolsOpt: parentRemappedSymbolsOpt);
        }

        public SpeculativeSemanticModelWithMemberModel(
            SyntaxTreeSemanticModel parentSemanticModel,
            int position,
            Symbol owner,
            EqualsValueClauseSyntax syntax,
            Binder rootBinder,
            ImmutableDictionary<Symbol, Symbol>? parentRemappedSymbolsOpt)
            : this(parentSemanticModel, position, snapshotManagerOpt: null)
        {
            Debug.Assert(syntax != null);
            Debug.Assert(rootBinder != null);
            Debug.Assert(rootBinder.IsSemanticModelBinder);

            _memberModel = new InitializerSemanticModel(syntax, owner, rootBinder, containingPublicSemanticModel: this, parentRemappedSymbolsOpt: parentRemappedSymbolsOpt);
        }

        public SpeculativeSemanticModelWithMemberModel(
            SyntaxTreeSemanticModel parentModel,
            int position,
            Symbol owner,
            TypeSyntax type,
            Binder rootBinder,
            ImmutableDictionary<Symbol, Symbol>? parentRemappedSymbolsOpt,
            NullableWalker.SnapshotManager? snapshotManagerOpt)
            : this(parentModel, position, snapshotManagerOpt)
        {
            _memberModel = new MemberSemanticModel.SpeculativeMemberSemanticModel(this, owner, type, rootBinder, parentRemappedSymbolsOpt);
        }

        public SpeculativeSemanticModelWithMemberModel(
            SyntaxTreeSemanticModel parentSemanticModel,
            int position,
            MethodSymbol owner,
            CSharpSyntaxNode syntax,
            Binder rootBinder,
            ImmutableDictionary<Symbol, Symbol>? parentRemappedSymbolsOpt,
            NullableWalker.SnapshotManager? snapshotManagerOpt)
            : this(parentSemanticModel, position, snapshotManagerOpt)
        {
            Debug.Assert(syntax != null);
            Debug.Assert(rootBinder != null);
            Debug.Assert(rootBinder.IsSemanticModelBinder);

            _memberModel = new MethodBodySemanticModel(owner, rootBinder, syntax, containingPublicSemanticModel: this, parentRemappedSymbolsOpt: parentRemappedSymbolsOpt);
        }

        internal NullableWalker.SnapshotManager? ParentSnapshotManagerOpt => _parentSnapshotManagerOpt;

        public override bool IsSpeculativeSemanticModel => true;

        public override int OriginalPositionForSpeculation => _position;

        public override CSharpSemanticModel ParentModel => _parentSemanticModel;

        public override CSharpCompilation Compilation => _parentSemanticModel.Compilation;

        internal override CSharpSyntaxNode Root => _memberModel.Root;

        public override SyntaxTree SyntaxTree => _memberModel.SyntaxTree;

        public override bool IgnoresAccessibility => _parentSemanticModel.IgnoresAccessibility;

        internal sealed override SemanticModel ContainingModelOrSelf => this;

        internal override MemberSemanticModel GetMemberModel(SyntaxNode node)
        {
            return _memberModel.GetMemberModel(node);
        }

        public override Conversion ClassifyConversion(
            ExpressionSyntax expression,
            ITypeSymbol destination,
            bool isExplicitInSource = false)
        {
            return _memberModel.ClassifyConversion(expression, destination, isExplicitInSource);
        }

        internal override Conversion ClassifyConversionForCast(
            ExpressionSyntax expression,
            TypeSymbol destination)
        {
            return _memberModel.ClassifyConversionForCast(expression, destination);
        }

        public override ImmutableArray<Diagnostic> GetSyntaxDiagnostics(TextSpan? span = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _memberModel.GetSyntaxDiagnostics(span, cancellationToken);
        }

        public override ImmutableArray<Diagnostic> GetDeclarationDiagnostics(TextSpan? span = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _memberModel.GetDeclarationDiagnostics(span, cancellationToken);
        }

        public override ImmutableArray<Diagnostic> GetMethodBodyDiagnostics(TextSpan? span = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _memberModel.GetMethodBodyDiagnostics(span, cancellationToken);
        }

        public override ImmutableArray<Diagnostic> GetDiagnostics(TextSpan? span = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _memberModel.GetDiagnostics(span, cancellationToken);
        }

        public override INamespaceSymbol GetDeclaredSymbol(NamespaceDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _memberModel.GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        public override INamespaceSymbol GetDeclaredSymbol(FileScopedNamespaceDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _memberModel.GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        public override INamedTypeSymbol GetDeclaredSymbol(BaseTypeDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _memberModel.GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        public override INamedTypeSymbol GetDeclaredSymbol(DelegateDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _memberModel.GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        public override IFieldSymbol GetDeclaredSymbol(EnumMemberDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _memberModel.GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        public override ISymbol GetDeclaredSymbol(LocalFunctionStatementSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _memberModel.GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        public override ISymbol GetDeclaredSymbol(MemberDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _memberModel.GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        public override IMethodSymbol GetDeclaredSymbol(CompilationUnitSyntax declarationSyntax, CancellationToken cancellationToken = default)
        {
            return _memberModel.GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        public override IMethodSymbol GetDeclaredSymbol(BaseMethodDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _memberModel.GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        public override ISymbol GetDeclaredSymbol(BasePropertyDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _memberModel.GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        public override IPropertySymbol GetDeclaredSymbol(PropertyDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _memberModel.GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        public override IPropertySymbol GetDeclaredSymbol(IndexerDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _memberModel.GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        public override IEventSymbol GetDeclaredSymbol(EventDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _memberModel.GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        public override IMethodSymbol GetDeclaredSymbol(AccessorDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _memberModel.GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        public override IMethodSymbol GetDeclaredSymbol(ArrowExpressionClauseSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _memberModel.GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        public override ISymbol GetDeclaredSymbol(VariableDeclaratorSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _memberModel.GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        public override ISymbol GetDeclaredSymbol(SingleVariableDesignationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _memberModel.GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        internal override LocalSymbol GetAdjustedLocalSymbol(SourceLocalSymbol local)
        {
            return _memberModel.GetAdjustedLocalSymbol(local);
        }

        public override ILabelSymbol GetDeclaredSymbol(LabeledStatementSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _memberModel.GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        public override ILabelSymbol GetDeclaredSymbol(SwitchLabelSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _memberModel.GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        public override IAliasSymbol GetDeclaredSymbol(UsingDirectiveSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _memberModel.GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        public override IAliasSymbol GetDeclaredSymbol(ExternAliasDirectiveSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _memberModel.GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        public override IParameterSymbol GetDeclaredSymbol(ParameterSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _memberModel.GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        internal override ImmutableArray<ISymbol> GetDeclaredSymbols(BaseFieldDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _memberModel.GetDeclaredSymbols(declarationSyntax, cancellationToken);
        }

        public override ITypeParameterSymbol GetDeclaredSymbol(TypeParameterSyntax typeParameter, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _memberModel.GetDeclaredSymbol(typeParameter, cancellationToken);
        }

        public override IRangeVariableSymbol GetDeclaredSymbol(JoinIntoClauseSyntax node, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _memberModel.GetDeclaredSymbol(node, cancellationToken);
        }

        public override IRangeVariableSymbol GetDeclaredSymbol(QueryClauseSyntax queryClause, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _memberModel.GetDeclaredSymbol(queryClause, cancellationToken);
        }

        public override IRangeVariableSymbol GetDeclaredSymbol(QueryContinuationSyntax node, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _memberModel.GetDeclaredSymbol(node, cancellationToken);
        }

        public override AwaitExpressionInfo GetAwaitExpressionInfo(AwaitExpressionSyntax node)
        {
            return _memberModel.GetAwaitExpressionInfo(node);
        }

        public override ForEachStatementInfo GetForEachStatementInfo(ForEachStatementSyntax node)
        {
            return _memberModel.GetForEachStatementInfo(node);
        }

        public override ForEachStatementInfo GetForEachStatementInfo(CommonForEachStatementSyntax node)
        {
            return _memberModel.GetForEachStatementInfo(node);
        }

        public override DeconstructionInfo GetDeconstructionInfo(AssignmentExpressionSyntax node)
        {
            return _memberModel.GetDeconstructionInfo(node);
        }

        public override DeconstructionInfo GetDeconstructionInfo(ForEachVariableStatementSyntax node)
        {
            return _memberModel.GetDeconstructionInfo(node);
        }

        public override QueryClauseInfo GetQueryClauseInfo(QueryClauseSyntax node, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _memberModel.GetQueryClauseInfo(node, cancellationToken);
        }

        public override IPropertySymbol GetDeclaredSymbol(AnonymousObjectMemberDeclaratorSyntax declaratorSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _memberModel.GetDeclaredSymbol(declaratorSyntax, cancellationToken);
        }

        public override INamedTypeSymbol GetDeclaredSymbol(AnonymousObjectCreationExpressionSyntax declaratorSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _memberModel.GetDeclaredSymbol(declaratorSyntax, cancellationToken);
        }

        public override INamedTypeSymbol GetDeclaredSymbol(TupleExpressionSyntax declaratorSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _memberModel.GetDeclaredSymbol(declaratorSyntax, cancellationToken);
        }

        public override ISymbol GetDeclaredSymbol(ArgumentSyntax declaratorSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _memberModel.GetDeclaredSymbol(declaratorSyntax, cancellationToken);
        }

        internal override IOperation? GetOperationWorker(CSharpSyntaxNode node, CancellationToken cancellationToken)
        {
            return _memberModel.GetOperationWorker(node, cancellationToken);
        }

        internal override SymbolInfo GetSymbolInfoWorker(CSharpSyntaxNode node, SymbolInfoOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _memberModel.GetSymbolInfoWorker(node, options, cancellationToken);
        }

        internal override CSharpTypeInfo GetTypeInfoWorker(CSharpSyntaxNode node, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _memberModel.GetTypeInfoWorker(node, cancellationToken);
        }

        internal override ImmutableArray<Symbol> GetMemberGroupWorker(CSharpSyntaxNode node, SymbolInfoOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _memberModel.GetMemberGroupWorker(node, options, cancellationToken);
        }

        internal override ImmutableArray<IPropertySymbol> GetIndexerGroupWorker(CSharpSyntaxNode node, SymbolInfoOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _memberModel.GetIndexerGroupWorker(node, options, cancellationToken);
        }

        internal override Optional<object> GetConstantValueWorker(CSharpSyntaxNode node, CancellationToken cancellationToken)
        {
            return _memberModel.GetConstantValueWorker(node, cancellationToken);
        }

        internal override SymbolInfo GetCollectionInitializerSymbolInfoWorker(InitializerExpressionSyntax collectionInitializer, ExpressionSyntax node, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _memberModel.GetCollectionInitializerSymbolInfoWorker(collectionInitializer, node, cancellationToken);
        }

        public override SymbolInfo GetSymbolInfo(OrderingSyntax node, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _memberModel.GetSymbolInfo(node, cancellationToken);
        }

        public override SymbolInfo GetSymbolInfo(SelectOrGroupClauseSyntax node, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _memberModel.GetSymbolInfo(node, cancellationToken);
        }

        public override TypeInfo GetTypeInfo(SelectOrGroupClauseSyntax node, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _memberModel.GetTypeInfo(node, cancellationToken);
        }

        internal override Binder GetEnclosingBinderInternal(int position)
        {
            return _memberModel.GetEnclosingBinderInternal(position);
        }

        internal override Symbol RemapSymbolIfNecessaryCore(Symbol symbol)
        {
            return _memberModel.RemapSymbolIfNecessaryCore(symbol);
        }

        internal sealed override Func<SyntaxNode, bool> GetSyntaxNodesToAnalyzeFilter(SyntaxNode declaredNode, ISymbol declaredSymbol)
        {
            return _memberModel.GetSyntaxNodesToAnalyzeFilter(declaredNode, declaredSymbol);
        }

        internal override bool ShouldSkipSyntaxNodeAnalysis(SyntaxNode node, ISymbol containingSymbol)
        {
            return _memberModel.ShouldSkipSyntaxNodeAnalysis(node, containingSymbol);
        }

        internal override BoundNode Bind(Binder binder, CSharpSyntaxNode node, BindingDiagnosticBag diagnostics)
        {
            return _memberModel.Bind(binder, node, diagnostics);
        }

        internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, ConstructorInitializerSyntax constructorInitializer, out PublicSemanticModel? speculativeModel)
        {
            return _memberModel.TryGetSpeculativeSemanticModelCore(parentModel, position, constructorInitializer, out speculativeModel);
        }

        internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, PrimaryConstructorBaseTypeSyntax constructorInitializer, out PublicSemanticModel? speculativeModel)
        {
            return _memberModel.TryGetSpeculativeSemanticModelCore(parentModel, position, constructorInitializer, out speculativeModel);
        }

        internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, EqualsValueClauseSyntax initializer, out PublicSemanticModel? speculativeModel)
        {
            return _memberModel.TryGetSpeculativeSemanticModelCore(parentModel, position, initializer, out speculativeModel);
        }

        internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, ArrowExpressionClauseSyntax expressionBody, out PublicSemanticModel? speculativeModel)
        {
            return _memberModel.TryGetSpeculativeSemanticModelCore(parentModel, position, expressionBody, out speculativeModel);
        }

        internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, StatementSyntax statement, out PublicSemanticModel? speculativeModel)
        {
            return _memberModel.TryGetSpeculativeSemanticModelCore(parentModel, position, statement, out speculativeModel);
        }

        internal override bool TryGetSpeculativeSemanticModelForMethodBodyCore(SyntaxTreeSemanticModel parentModel, int position, BaseMethodDeclarationSyntax method, out PublicSemanticModel? speculativeModel)
        {
            return _memberModel.TryGetSpeculativeSemanticModelForMethodBodyCore(parentModel, position, method, out speculativeModel);
        }

        internal override bool TryGetSpeculativeSemanticModelForMethodBodyCore(SyntaxTreeSemanticModel parentModel, int position, AccessorDeclarationSyntax accessor, out PublicSemanticModel? speculativeModel)
        {
            return _memberModel.TryGetSpeculativeSemanticModelForMethodBodyCore(parentModel, position, accessor, out speculativeModel);
        }

        internal sealed override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, TypeSyntax type, SpeculativeBindingOption bindingOption, out PublicSemanticModel speculativeModel)
        {
            return _memberModel.TryGetSpeculativeSemanticModelCore(parentModel, position, type, bindingOption, out speculativeModel);
        }

        internal sealed override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, CrefSyntax crefSyntax, out PublicSemanticModel speculativeModel)
        {
            return _memberModel.TryGetSpeculativeSemanticModelCore(parentModel, position, crefSyntax, out speculativeModel);
        }

        internal override BoundExpression GetSpeculativelyBoundExpression(int position, ExpressionSyntax expression, SpeculativeBindingOption bindingOption, out Binder binder, out ImmutableArray<Symbol> crefSymbols)
        {
            return _memberModel.GetSpeculativelyBoundExpression(position, expression, bindingOption, out binder, out crefSymbols);
        }
    }
}
