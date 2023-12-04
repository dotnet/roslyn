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
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Instances of this type represent user-facing speculative semantic models that are backed by 
    /// internal <see cref="MemberSemanticModel"/>.
    /// </summary>
    internal sealed class SpeculativeSemanticModelWithMemberModel : PublicSemanticModel
    {
        private readonly SyntaxTreeSemanticModel _parentSemanticModel;
        private readonly int _position;
        private readonly NullableWalker.SnapshotManager? _parentSnapshotManagerOpt;
        private readonly MemberSemanticModel _memberModel;
        private ImmutableDictionary<CSharpSyntaxNode, MemberSemanticModel> _childMemberModels = ImmutableDictionary<CSharpSyntaxNode, MemberSemanticModel>.Empty;

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

            _memberModel = new AttributeSemanticModel(syntax, attributeType, getAttributeTargetFromPosition(position, parentSemanticModel), aliasOpt, rootBinder, containingPublicSemanticModel: this, parentRemappedSymbolsOpt: parentRemappedSymbolsOpt);

            static Symbol? getAttributeTargetFromPosition(int position, SyntaxTreeSemanticModel model)
            {
                var attributedNode = model.SyntaxTree.GetRoot().FindToken(position).Parent;
                attributedNode = attributedNode?.FirstAncestorOrSelf<AttributeListSyntax>()?.Parent;

                if (attributedNode is not null)
                {
                    return model.GetDeclaredSymbolForNode(attributedNode).GetSymbol();
                }

                return null;
            }
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

        private MemberSemanticModel GetEnclosingMemberModel(int position)
        {
            AssertPositionAdjusted(position);
            SyntaxNode? node = Root.FindTokenIncludingCrefAndNameAttributes(position).Parent;
            return node is null ? _memberModel : GetEnclosingMemberModel(node);
        }

        private MemberSemanticModel GetEnclosingMemberModel(SyntaxNode node)
        {
            if (node.SyntaxTree != SyntaxTree)
            {
                return _memberModel;
            }

            var attributeOrParameter = node.FirstAncestorOrSelf<SyntaxNode>(static n => n.Kind() is SyntaxKind.Attribute or SyntaxKind.Parameter);

            if (attributeOrParameter is null ||
                attributeOrParameter == Root ||
                attributeOrParameter.Parent is null ||
                !Root.Span.Contains(attributeOrParameter.Span))
            {
                return _memberModel;
            }

            MemberSemanticModel containing = GetEnclosingMemberModel(attributeOrParameter.Parent);

            switch (attributeOrParameter)
            {
                case AttributeSyntax attribute:

                    return GetOrAddModelForAttribute(containing, attribute);

                case ParameterSyntax paramDecl:

                    return GetOrAddModelForParameter(node, containing, paramDecl);

                default:
                    ExceptionUtilities.UnexpectedValue(attributeOrParameter);
                    return containing;
            }
        }

        private MemberSemanticModel GetOrAddModelForAttribute(MemberSemanticModel containing, AttributeSyntax attribute)
        {
            return ImmutableInterlocked.GetOrAdd(ref _childMemberModels, attribute,
                                                 (node, binderAndModel) => CreateModelForAttribute(binderAndModel.binder, (AttributeSyntax)node, binderAndModel.model),
                                                 (binder: containing.GetEnclosingBinder(attribute.SpanStart), model: containing));
        }

        private MemberSemanticModel GetOrAddModelForParameter(SyntaxNode node, MemberSemanticModel containing, ParameterSyntax paramDecl)
        {
            EqualsValueClauseSyntax? defaultValueSyntax = paramDecl.Default;

            if (defaultValueSyntax != null && defaultValueSyntax.FullSpan.Contains(node.Span))
            {
                var parameterSymbol = containing.GetDeclaredSymbol(paramDecl).GetSymbol<ParameterSymbol>();
                if ((object)parameterSymbol != null)
                {
                    return ImmutableInterlocked.GetOrAdd(ref _childMemberModels, defaultValueSyntax,
                                                         (equalsValue, tuple) =>
                                                            InitializerSemanticModel.Create(
                                                                this,
                                                                tuple.paramDecl,
                                                                tuple.parameterSymbol,
                                                                tuple.containing.GetEnclosingBinder(tuple.paramDecl.SpanStart).
                                                                    CreateBinderForParameterDefaultValue(tuple.parameterSymbol,
                                                                                            (EqualsValueClauseSyntax)equalsValue),
                                                                tuple.containing.GetRemappedSymbols()),
                                                         (compilation: this.Compilation,
                                                          paramDecl,
                                                          parameterSymbol,
                                                          containing)
                                                         );
                }
            }

            return containing;
        }

        internal override MemberSemanticModel GetMemberModel(SyntaxNode node)
        {
            return GetEnclosingMemberModel(node).GetMemberModel(node);
        }

        public override Conversion ClassifyConversion(
            ExpressionSyntax expression,
            ITypeSymbol destination,
            bool isExplicitInSource = false)
        {
            return GetEnclosingMemberModel(expression).ClassifyConversion(expression, destination, isExplicitInSource);
        }

        internal override Conversion ClassifyConversionForCast(
            ExpressionSyntax expression,
            TypeSymbol destination)
        {
            return GetEnclosingMemberModel(expression).ClassifyConversionForCast(expression, destination);
        }

        public override ImmutableArray<Diagnostic> GetSyntaxDiagnostics(TextSpan? span = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotSupportedException();
        }

        public override ImmutableArray<Diagnostic> GetDeclarationDiagnostics(TextSpan? span = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotSupportedException();
        }

        public override ImmutableArray<Diagnostic> GetMethodBodyDiagnostics(TextSpan? span = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotSupportedException();
        }

        public override ImmutableArray<Diagnostic> GetDiagnostics(TextSpan? span = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotSupportedException();
        }

        public override INamespaceSymbol GetDeclaredSymbol(NamespaceDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetEnclosingMemberModel(declarationSyntax).GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        public override INamespaceSymbol GetDeclaredSymbol(FileScopedNamespaceDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetEnclosingMemberModel(declarationSyntax).GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        public override INamedTypeSymbol GetDeclaredSymbol(BaseTypeDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetEnclosingMemberModel(declarationSyntax).GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        public override INamedTypeSymbol GetDeclaredSymbol(DelegateDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetEnclosingMemberModel(declarationSyntax).GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        public override IFieldSymbol GetDeclaredSymbol(EnumMemberDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetEnclosingMemberModel(declarationSyntax).GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        public override IMethodSymbol GetDeclaredSymbol(LocalFunctionStatementSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetEnclosingMemberModel(declarationSyntax).GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        public override ISymbol GetDeclaredSymbol(MemberDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetEnclosingMemberModel(declarationSyntax).GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        public override IMethodSymbol GetDeclaredSymbol(CompilationUnitSyntax declarationSyntax, CancellationToken cancellationToken = default)
        {
            return GetEnclosingMemberModel(declarationSyntax).GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        public override IMethodSymbol GetDeclaredSymbol(BaseMethodDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetEnclosingMemberModel(declarationSyntax).GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        public override ISymbol GetDeclaredSymbol(BasePropertyDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetEnclosingMemberModel(declarationSyntax).GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        public override IPropertySymbol GetDeclaredSymbol(PropertyDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetEnclosingMemberModel(declarationSyntax).GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        public override IPropertySymbol GetDeclaredSymbol(IndexerDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetEnclosingMemberModel(declarationSyntax).GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        public override IEventSymbol GetDeclaredSymbol(EventDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetEnclosingMemberModel(declarationSyntax).GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        public override IMethodSymbol GetDeclaredSymbol(AccessorDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetEnclosingMemberModel(declarationSyntax).GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        public override IMethodSymbol GetDeclaredSymbol(ArrowExpressionClauseSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetEnclosingMemberModel(declarationSyntax).GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        public override ISymbol GetDeclaredSymbol(VariableDeclaratorSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetEnclosingMemberModel(declarationSyntax).GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        public override ISymbol GetDeclaredSymbol(SingleVariableDesignationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetEnclosingMemberModel(declarationSyntax).GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        internal override LocalSymbol GetAdjustedLocalSymbol(SourceLocalSymbol local)
        {
            return _memberModel.GetAdjustedLocalSymbol(local);
        }

        public override ILabelSymbol GetDeclaredSymbol(LabeledStatementSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetEnclosingMemberModel(declarationSyntax).GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        public override ILabelSymbol GetDeclaredSymbol(SwitchLabelSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetEnclosingMemberModel(declarationSyntax).GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        public override IAliasSymbol GetDeclaredSymbol(UsingDirectiveSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetEnclosingMemberModel(declarationSyntax).GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        public override IAliasSymbol GetDeclaredSymbol(ExternAliasDirectiveSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetEnclosingMemberModel(declarationSyntax).GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        public override IParameterSymbol GetDeclaredSymbol(ParameterSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetEnclosingMemberModel(declarationSyntax).GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        internal override ImmutableArray<ISymbol> GetDeclaredSymbols(BaseFieldDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetEnclosingMemberModel(declarationSyntax).GetDeclaredSymbols(declarationSyntax, cancellationToken);
        }

        public override ITypeParameterSymbol GetDeclaredSymbol(TypeParameterSyntax typeParameter, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetEnclosingMemberModel(typeParameter).GetDeclaredSymbol(typeParameter, cancellationToken);
        }

        public override IRangeVariableSymbol GetDeclaredSymbol(JoinIntoClauseSyntax node, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetEnclosingMemberModel(node).GetDeclaredSymbol(node, cancellationToken);
        }

        public override IRangeVariableSymbol GetDeclaredSymbol(QueryClauseSyntax queryClause, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetEnclosingMemberModel(queryClause).GetDeclaredSymbol(queryClause, cancellationToken);
        }

        public override IRangeVariableSymbol GetDeclaredSymbol(QueryContinuationSyntax node, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetEnclosingMemberModel(node).GetDeclaredSymbol(node, cancellationToken);
        }

        public override AwaitExpressionInfo GetAwaitExpressionInfo(AwaitExpressionSyntax node)
        {
            return GetEnclosingMemberModel(node).GetAwaitExpressionInfo(node);
        }

        public override ForEachStatementInfo GetForEachStatementInfo(ForEachStatementSyntax node)
        {
            return GetEnclosingMemberModel(node).GetForEachStatementInfo(node);
        }

        public override ForEachStatementInfo GetForEachStatementInfo(CommonForEachStatementSyntax node)
        {
            return GetEnclosingMemberModel(node).GetForEachStatementInfo(node);
        }

        public override DeconstructionInfo GetDeconstructionInfo(AssignmentExpressionSyntax node)
        {
            return GetEnclosingMemberModel(node).GetDeconstructionInfo(node);
        }

        public override DeconstructionInfo GetDeconstructionInfo(ForEachVariableStatementSyntax node)
        {
            return GetEnclosingMemberModel(node).GetDeconstructionInfo(node);
        }

        public override QueryClauseInfo GetQueryClauseInfo(QueryClauseSyntax node, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetEnclosingMemberModel(node).GetQueryClauseInfo(node, cancellationToken);
        }

        public override IPropertySymbol GetDeclaredSymbol(AnonymousObjectMemberDeclaratorSyntax declaratorSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetEnclosingMemberModel(declaratorSyntax).GetDeclaredSymbol(declaratorSyntax, cancellationToken);
        }

        public override INamedTypeSymbol GetDeclaredSymbol(AnonymousObjectCreationExpressionSyntax declaratorSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetEnclosingMemberModel(declaratorSyntax).GetDeclaredSymbol(declaratorSyntax, cancellationToken);
        }

        public override INamedTypeSymbol GetDeclaredSymbol(TupleExpressionSyntax declaratorSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetEnclosingMemberModel(declaratorSyntax).GetDeclaredSymbol(declaratorSyntax, cancellationToken);
        }

        public override ISymbol GetDeclaredSymbol(ArgumentSyntax declaratorSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetEnclosingMemberModel(declaratorSyntax).GetDeclaredSymbol(declaratorSyntax, cancellationToken);
        }

        internal override IOperation? GetOperationWorker(CSharpSyntaxNode node, CancellationToken cancellationToken)
        {
            return GetEnclosingMemberModel(node).GetOperationWorker(node, cancellationToken);
        }

        internal override SymbolInfo GetSymbolInfoWorker(CSharpSyntaxNode node, SymbolInfoOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetEnclosingMemberModel(node).GetSymbolInfoWorker(node, options, cancellationToken);
        }

        internal override CSharpTypeInfo GetTypeInfoWorker(CSharpSyntaxNode node, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetEnclosingMemberModel(node).GetTypeInfoWorker(node, cancellationToken);
        }

        internal override ImmutableArray<Symbol> GetMemberGroupWorker(CSharpSyntaxNode node, SymbolInfoOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetEnclosingMemberModel(node).GetMemberGroupWorker(node, options, cancellationToken);
        }

        internal override ImmutableArray<IPropertySymbol> GetIndexerGroupWorker(CSharpSyntaxNode node, SymbolInfoOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetEnclosingMemberModel(node).GetIndexerGroupWorker(node, options, cancellationToken);
        }

        internal override Optional<object> GetConstantValueWorker(CSharpSyntaxNode node, CancellationToken cancellationToken)
        {
            return GetEnclosingMemberModel(node).GetConstantValueWorker(node, cancellationToken);
        }

        internal override SymbolInfo GetCollectionInitializerSymbolInfoWorker(InitializerExpressionSyntax collectionInitializer, ExpressionSyntax node, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetEnclosingMemberModel(collectionInitializer).GetCollectionInitializerSymbolInfoWorker(collectionInitializer, node, cancellationToken);
        }

        public override SymbolInfo GetSymbolInfo(OrderingSyntax node, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetEnclosingMemberModel(node).GetSymbolInfo(node, cancellationToken);
        }

        public override SymbolInfo GetSymbolInfo(SelectOrGroupClauseSyntax node, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetEnclosingMemberModel(node).GetSymbolInfo(node, cancellationToken);
        }

        public override TypeInfo GetTypeInfo(SelectOrGroupClauseSyntax node, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetEnclosingMemberModel(node).GetTypeInfo(node, cancellationToken);
        }

        internal override Binder GetEnclosingBinderInternal(int position)
        {
            return GetEnclosingMemberModel(position).GetEnclosingBinderInternal(position);
        }

        internal override Symbol RemapSymbolIfNecessaryCore(Symbol symbol)
        {
            return _memberModel.RemapSymbolIfNecessaryCore(symbol);
        }

        internal sealed override Func<SyntaxNode, bool> GetSyntaxNodesToAnalyzeFilter(SyntaxNode declaredNode, ISymbol declaredSymbol)
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override bool ShouldSkipSyntaxNodeAnalysis(SyntaxNode node, ISymbol containingSymbol)
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override BoundNode Bind(Binder binder, CSharpSyntaxNode node, BindingDiagnosticBag diagnostics)
        {
            return GetEnclosingMemberModel(node).Bind(binder, node, diagnostics);
        }

        internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, ConstructorInitializerSyntax constructorInitializer, out PublicSemanticModel? speculativeModel)
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, PrimaryConstructorBaseTypeSyntax constructorInitializer, out PublicSemanticModel? speculativeModel)
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, EqualsValueClauseSyntax initializer, out PublicSemanticModel? speculativeModel)
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, ArrowExpressionClauseSyntax expressionBody, out PublicSemanticModel? speculativeModel)
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, StatementSyntax statement, out PublicSemanticModel? speculativeModel)
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override bool TryGetSpeculativeSemanticModelForMethodBodyCore(SyntaxTreeSemanticModel parentModel, int position, BaseMethodDeclarationSyntax method, out PublicSemanticModel? speculativeModel)
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override bool TryGetSpeculativeSemanticModelForMethodBodyCore(SyntaxTreeSemanticModel parentModel, int position, AccessorDeclarationSyntax accessor, out PublicSemanticModel? speculativeModel)
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal sealed override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, TypeSyntax type, SpeculativeBindingOption bindingOption, out PublicSemanticModel speculativeModel)
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal sealed override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, CrefSyntax crefSyntax, out PublicSemanticModel speculativeModel)
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override BoundExpression GetSpeculativelyBoundExpression(int position, ExpressionSyntax expression, SpeculativeBindingOption bindingOption, out Binder binder, out ImmutableArray<Symbol> crefSymbols)
        {
            return GetEnclosingMemberModel(CheckAndAdjustPosition(position)).GetSpeculativelyBoundExpression(position, expression, bindingOption, out binder, out crefSymbols);
        }
    }
}
