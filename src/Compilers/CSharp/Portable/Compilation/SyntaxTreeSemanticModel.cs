// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Semantics;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Allows asking semantic questions about any node in a SyntaxTree within a Compilation.
    /// </summary>
    internal partial class SyntaxTreeSemanticModel : CSharpSemanticModel
    {
        private readonly CSharpCompilation _compilation;
        private readonly SyntaxTree _syntaxTree;
        private ImmutableDictionary<CSharpSyntaxNode, MemberSemanticModel> _memberModels = ImmutableDictionary<CSharpSyntaxNode, MemberSemanticModel>.Empty;

        private readonly BinderFactory _binderFactory;
        private Func<CSharpSyntaxNode, MemberSemanticModel> _createMemberModelFunction;
        private readonly bool _ignoresAccessibility;
        private ScriptLocalScopeBinder.Labels _globalStatementLabels;

        private static readonly Func<CSharpSyntaxNode, bool> s_isMemberDeclarationFunction = IsMemberDeclaration;

        internal SyntaxTreeSemanticModel(CSharpCompilation compilation, SyntaxTree syntaxTree, bool ignoreAccessibility = false)
        {
            _compilation = compilation;
            _syntaxTree = syntaxTree;
            _ignoresAccessibility = ignoreAccessibility;

            if (!this.Compilation.SyntaxTrees.Contains(syntaxTree))
            {
                throw new ArgumentOutOfRangeException(nameof(syntaxTree), CSharpResources.TreeNotPartOfCompilation);
            }

            _binderFactory = compilation.GetBinderFactory(SyntaxTree);
        }

        internal SyntaxTreeSemanticModel(CSharpCompilation parentCompilation, SyntaxTree parentSyntaxTree, SyntaxTree speculatedSyntaxTree)
        {
            _compilation = parentCompilation;
            _syntaxTree = speculatedSyntaxTree;
            _binderFactory = _compilation.GetBinderFactory(parentSyntaxTree);
        }

        /// <summary>
        /// The compilation this object was obtained from.
        /// </summary>
        public override CSharpCompilation Compilation
        {
            get
            {
                return _compilation;
            }
        }

        /// <summary>
        /// The root node of the syntax tree that this object is associated with.
        /// </summary>
        internal override CSharpSyntaxNode Root
        {
            get
            {
                return (CSharpSyntaxNode)_syntaxTree.GetRoot();
            }
        }

        /// <summary>
        /// The SyntaxTree that this object is associated with.
        /// </summary>
        public override SyntaxTree SyntaxTree
        {
            get
            {
                return _syntaxTree;
            }
        }

        /// <summary>
        /// Returns true if this is a SemanticModel that ignores accessibility rules when answering semantic questions.
        /// </summary>
        public override bool IgnoresAccessibility
        {
            get { return _ignoresAccessibility; }
        }

        private void VerifySpanForGetDiagnostics(TextSpan? span)
        {
            if (span.HasValue && !this.Root.FullSpan.Contains(span.Value))
            {
                throw new ArgumentException("span");
            }
        }

        public override ImmutableArray<Diagnostic> GetSyntaxDiagnostics(TextSpan? span = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            VerifySpanForGetDiagnostics(span);
            return Compilation.GetDiagnosticsForSyntaxTree(
            CompilationStage.Parse, this.SyntaxTree, span, includeEarlierStages: false, cancellationToken: cancellationToken);
        }

        public override ImmutableArray<Diagnostic> GetDeclarationDiagnostics(TextSpan? span = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            VerifySpanForGetDiagnostics(span);
            return Compilation.GetDiagnosticsForSyntaxTree(
            CompilationStage.Declare, this.SyntaxTree, span, includeEarlierStages: false, cancellationToken: cancellationToken);
        }

        public override ImmutableArray<Diagnostic> GetMethodBodyDiagnostics(TextSpan? span = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            VerifySpanForGetDiagnostics(span);
            return Compilation.GetDiagnosticsForSyntaxTree(
            CompilationStage.Compile, this.SyntaxTree, span, includeEarlierStages: false, cancellationToken: cancellationToken);
        }

        public override ImmutableArray<Diagnostic> GetDiagnostics(TextSpan? span = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            VerifySpanForGetDiagnostics(span);
            return Compilation.GetDiagnosticsForSyntaxTree(
            CompilationStage.Compile, this.SyntaxTree, span, includeEarlierStages: true, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Gets the enclosing binder associated with the node
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        internal override Binder GetEnclosingBinderInternal(int position)
        {
            AssertPositionAdjusted(position);
            SyntaxToken token = this.Root.FindTokenIncludingCrefAndNameAttributes(position);

            // If we're before the start of the first token, just return
            // the binder for the compilation unit.
            if (position == 0 && position != token.SpanStart)
            {
                return _binderFactory.GetBinder(this.Root, position).WithAdditionalFlags(GetSemanticModelBinderFlags());
            }

            MemberSemanticModel memberModel = GetMemberModel(position);
            if (memberModel != null)
            {
                return memberModel.GetEnclosingBinder(position);
            }

            return _binderFactory.GetBinder((CSharpSyntaxNode)token.Parent, position).WithAdditionalFlags(GetSemanticModelBinderFlags());
        }

        internal override IOperation GetOperationWorker(CSharpSyntaxNode node, GetOperationOptions options, CancellationToken cancellationToken)
        {
            // in case this is right side of a qualified name or member access (or part of a cref)
            node = SyntaxFactory.GetStandaloneNode(node);

            var model = this.GetMemberModel(node);
            if (model != null)
            {
                return model.GetOperationWorker(node, options, cancellationToken);
            }
            else
            {
                return null;
            }
        }

        internal override SymbolInfo GetSymbolInfoWorker(CSharpSyntaxNode node, SymbolInfoOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            ValidateSymbolInfoOptions(options);

            // in case this is right side of a qualified name or member access (or part of a cref)
            node = SyntaxFactory.GetStandaloneNode(node);

            var model = this.GetMemberModel(node);
            SymbolInfo result;

            XmlNameAttributeSyntax attrSyntax;
            CrefSyntax crefSyntax;

            if (model != null)
            {
                // Expression occurs in an executable code (method body or initializer) context. Use that
                // model to get the information.
                result = model.GetSymbolInfoWorker(node, options, cancellationToken);

                // If we didn't get anything and were in Type/Namespace only context, let's bind normally and see
                // if any symbol comes out.
                if ((object)result.Symbol == null && result.CandidateReason == CandidateReason.None && node is ExpressionSyntax && SyntaxFacts.IsInNamespaceOrTypeContext((ExpressionSyntax)node))
                {
                    var binder = this.GetEnclosingBinder(GetAdjustedNodePosition(node));

                    if (binder != null)
                    {
                        // Wrap the binder in a LocalScopeBinder because Binder.BindExpression assumes there
                        // will be one in the binder chain and one isn't necessarily required for the batch case.
                        binder = new LocalScopeBinder(binder);

                        var diagnostics = DiagnosticBag.GetInstance();
                        BoundExpression bound = binder.BindExpression((ExpressionSyntax)node, diagnostics);
                        diagnostics.Free();

                        SymbolInfo info = GetSymbolInfoForNode(options, bound, bound, boundNodeForSyntacticParent: null, binderOpt: null);
                        if ((object)info.Symbol != null)
                        {
                            result = new SymbolInfo(null, ImmutableArray.Create<ISymbol>(info.Symbol), CandidateReason.NotATypeOrNamespace);
                        }
                        else if (!info.CandidateSymbols.IsEmpty)
                        {
                            result = new SymbolInfo(null, info.CandidateSymbols, CandidateReason.NotATypeOrNamespace);
                        }
                    }
                }
            }
            else if (node.Parent.Kind() == SyntaxKind.XmlNameAttribute && (attrSyntax = (XmlNameAttributeSyntax)node.Parent).Identifier == node)
            {
                result = SymbolInfo.None;

                var binder = this.GetEnclosingBinder(GetAdjustedNodePosition(node));
                if (binder != null)
                {
                    HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                    var symbols = binder.BindXmlNameAttribute(attrSyntax, ref useSiteDiagnostics);

                    // NOTE: We don't need to call GetSymbolInfoForSymbol because the symbols
                    // can only be parameters or type parameters.
                    Debug.Assert(symbols.All(s => s.Kind == SymbolKind.TypeParameter || s.Kind == SymbolKind.Parameter));

                    switch (symbols.Length)
                    {
                        case 0:
                            result = SymbolInfo.None;
                            break;
                        case 1:
                            result = SymbolInfoFactory.Create(symbols, LookupResultKind.Viable, isDynamic: false);
                            break;
                        default:
                            result = SymbolInfoFactory.Create(symbols, LookupResultKind.Ambiguous, isDynamic: false);
                            break;
                    }
                }
            }
            else if ((crefSyntax = node as CrefSyntax) != null)
            {
                int adjustedPosition = GetAdjustedNodePosition(crefSyntax);
                result = GetCrefSymbolInfo(adjustedPosition, crefSyntax, options, HasParameterList(crefSyntax));
            }
            else
            {
                // if expression is not part of a member context then caller may really just have a
                // reference to a type or namespace name
                var symbol = GetSemanticInfoSymbolInNonMemberContext(node, bindVarAsAliasFirst: (options & SymbolInfoOptions.PreserveAliases) != 0);
                result = (object)symbol != null ? GetSymbolInfoForSymbol(symbol, options) : SymbolInfo.None;
            }

            return result;
        }

        internal override SymbolInfo GetCollectionInitializerSymbolInfoWorker(InitializerExpressionSyntax collectionInitializer, ExpressionSyntax node, CancellationToken cancellationToken = default(CancellationToken))
        {
            var model = this.GetMemberModel(collectionInitializer);

            if (model != null)
            {
                // Expression occurs in an executable code (method body or initializer) context. Use that
                // model to get the information.
                return model.GetCollectionInitializerSymbolInfoWorker(collectionInitializer, node, cancellationToken);
            }

            return SymbolInfo.None;
        }

        internal override CSharpTypeInfo GetTypeInfoWorker(CSharpSyntaxNode node, CancellationToken cancellationToken = default(CancellationToken))
        {
            // in case this is right side of a qualified name or member access (or part of a cref)
            node = SyntaxFactory.GetStandaloneNode(node);

            var model = this.GetMemberModel(node);

            if (model != null)
            {
                // Expression occurs in an executable code (method body or initializer) context. Use that
                // model to get the information.
                return model.GetTypeInfoWorker(node, cancellationToken);
            }
            else
            {
                // if expression is not part of a member context then caller may really just have a
                // reference to a type or namespace name
                var symbol = GetSemanticInfoSymbolInNonMemberContext(node, bindVarAsAliasFirst: false); // Don't care about aliases here.
                return (object)symbol != null ? GetTypeInfoForSymbol(symbol) : CSharpTypeInfo.None;
            }
        }

        // Common helper method for GetSymbolInfoWorker and GetTypeInfoWorker, which is called when there is no member model for the given syntax node.
        // Even if the  expression is not part of a member context, the caller may really just have a reference to a type or namespace name.
        // If so, the methods binds the syntax as a namespace or type or alias symbol. Otherwise, it returns null.
        private Symbol GetSemanticInfoSymbolInNonMemberContext(CSharpSyntaxNode node, bool bindVarAsAliasFirst)
        {
            Debug.Assert(this.GetMemberModel(node) == null);

            var binder = this.GetEnclosingBinder(GetAdjustedNodePosition(node));
            if (binder != null)
            {
                // if expression is not part of a member context then caller may really just have a
                // reference to a type or namespace name
                var type = node as TypeSyntax;
                if ((object)type != null)
                {
                    // determine if this type is part of a base declaration being resolved
                    var basesBeingResolved = GetBasesBeingResolved(type);

                    var diagnostics = DiagnosticBag.GetInstance();
                    try
                    {
                        if (SyntaxFacts.IsNamespaceAliasQualifier(type))
                        {
                            return binder.BindNamespaceAliasSymbol(node as IdentifierNameSyntax, diagnostics);
                        }
                        else if (SyntaxFacts.IsInTypeOnlyContext(type))
                        {
                            if (!type.IsVar)
                            {
                                return binder.BindTypeOrAlias(type, diagnostics, basesBeingResolved);
                            }

                            Symbol result = bindVarAsAliasFirst
                                ? binder.BindTypeOrAlias(type, diagnostics, basesBeingResolved)
                                : null;

                            // CONSIDER: we might bind "var" twice - once to see if it is an alias and again
                            // as the type of a declared field.  This should only happen for GetAliasInfo
                            // calls on implicitly-typed fields (rare?).  If it becomes a problem, then we
                            // probably need to have the FieldSymbol retain alias info when it does its own
                            // binding and expose it to us here.

                            if ((object)result == null || result.Kind == SymbolKind.ErrorType)
                            {
                                // We might be in a field declaration with "var" keyword as the type name.
                                // Implicitly typed field symbols are not allowed in regular C#,
                                // but they are allowed in interactive scenario.
                                // However, we can return fieldSymbol.Type for implicitly typed field symbols in both cases.
                                // Note that for regular C#, fieldSymbol.Type would be an error type.

                                var variableDecl = type.Parent as VariableDeclarationSyntax;
                                if (variableDecl != null && variableDecl.Variables.Any())
                                {
                                    var fieldSymbol = GetDeclaredFieldSymbol(variableDecl.Variables.First());
                                    if ((object)fieldSymbol != null)
                                    {
                                        result = fieldSymbol.Type;
                                    }
                                }
                            }

                            return result ?? binder.BindTypeOrAlias(type, diagnostics, basesBeingResolved);
                        }
                        else
                        {
                            return binder.BindNamespaceOrTypeOrAliasSymbol(type, diagnostics, basesBeingResolved, basesBeingResolved != null);
                        }
                    }
                    finally
                    {
                        diagnostics.Free();
                    }
                }
            }

            return null;
        }

        internal override ImmutableArray<Symbol> GetMemberGroupWorker(CSharpSyntaxNode node, SymbolInfoOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            // in case this is right side of a qualified name or member access (or part of a cref)
            node = SyntaxFactory.GetStandaloneNode(node);

            var model = this.GetMemberModel(node);
            return model == null ? ImmutableArray<Symbol>.Empty : model.GetMemberGroupWorker(node, options, cancellationToken);
        }

        internal override ImmutableArray<PropertySymbol> GetIndexerGroupWorker(CSharpSyntaxNode node, SymbolInfoOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            // in case this is right side of a qualified name or member access (or part of a cref)
            node = SyntaxFactory.GetStandaloneNode(node);

            var model = this.GetMemberModel(node);
            return model == null ? ImmutableArray<PropertySymbol>.Empty : model.GetIndexerGroupWorker(node, options, cancellationToken);
        }

        internal override Optional<object> GetConstantValueWorker(CSharpSyntaxNode node, CancellationToken cancellationToken)
        {
            // in case this is right side of a qualified name or member access
            node = SyntaxFactory.GetStandaloneNode(node);

            var model = this.GetMemberModel(node);
            return model == null ? default(Optional<object>) : model.GetConstantValueWorker(node, cancellationToken);
        }

        public override QueryClauseInfo GetQueryClauseInfo(QueryClauseSyntax node, CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckSyntaxNode(node);
            var model = this.GetMemberModel(node);
            return (model == null) ? default(QueryClauseInfo) : model.GetQueryClauseInfo(node, cancellationToken);
        }

        public override SymbolInfo GetSymbolInfo(SelectOrGroupClauseSyntax node, CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckSyntaxNode(node);
            var model = this.GetMemberModel(node);
            return (model == null) ? default(SymbolInfo) : model.GetSymbolInfo(node, cancellationToken);
        }

        public override TypeInfo GetTypeInfo(SelectOrGroupClauseSyntax node, CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckSyntaxNode(node);
            var model = this.GetMemberModel(node);
            return (model == null) ? default(TypeInfo) : model.GetTypeInfo(node, cancellationToken);
        }

        public override SymbolInfo GetSymbolInfo(SubPropertyPatternSyntax node, CancellationToken cancellationToken)
        {
            CheckSyntaxNode(node);
            var model = this.GetMemberModel(node);
            return (model == null) ? default(SymbolInfo) : model.GetSymbolInfo(node, cancellationToken);
        }

        public override IPropertySymbol GetDeclaredSymbol(AnonymousObjectMemberDeclaratorSyntax declaratorSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckSyntaxNode(declaratorSyntax);
            var model = this.GetMemberModel(declaratorSyntax);
            return (model == null) ? null : model.GetDeclaredSymbol(declaratorSyntax, cancellationToken);
        }

        public override INamedTypeSymbol GetDeclaredSymbol(AnonymousObjectCreationExpressionSyntax declaratorSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckSyntaxNode(declaratorSyntax);
            var model = this.GetMemberModel(declaratorSyntax);
            return (model == null) ? null : model.GetDeclaredSymbol(declaratorSyntax, cancellationToken);
        }

        public override IRangeVariableSymbol GetDeclaredSymbol(QueryClauseSyntax node, CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckSyntaxNode(node);
            var model = this.GetMemberModel(node);
            return (model == null) ? null : model.GetDeclaredSymbol(node, cancellationToken);
        }

        public override IRangeVariableSymbol GetDeclaredSymbol(JoinIntoClauseSyntax node, CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckSyntaxNode(node);
            var model = this.GetMemberModel(node);
            return (model == null) ? null : model.GetDeclaredSymbol(node, cancellationToken);
        }

        public override IRangeVariableSymbol GetDeclaredSymbol(QueryContinuationSyntax node, CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckSyntaxNode(node);
            var model = this.GetMemberModel(node);
            return (model == null) ? null : model.GetDeclaredSymbol(node, cancellationToken);
        }

        public override SymbolInfo GetSymbolInfo(OrderingSyntax node, CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckSyntaxNode(node);
            var model = this.GetMemberModel(node);
            return (model == null) ? default(SymbolInfo) : model.GetSymbolInfo(node, cancellationToken);
        }

        private ConsList<Symbol> GetBasesBeingResolved(TypeSyntax expression)
        {
            // if the expression is the child of a base-list node, then the expression should be
            // bound in the context of the containing symbols base being resolved.
            for (; expression != null && expression.Parent != null; expression = expression.Parent as TypeSyntax)
            {
                var parent = expression.Parent;
                if (parent is BaseTypeSyntax && parent.Parent != null && parent.Parent.Kind() == SyntaxKind.BaseList && ((BaseTypeSyntax)parent).Type == expression)
                {
                    // we have a winner
                    var decl = (BaseTypeDeclarationSyntax)parent.Parent.Parent;
                    var symbol = this.GetDeclaredSymbol(decl);
                    return ConsList<Symbol>.Empty.Prepend((Symbol)symbol.OriginalDefinition);
                }
            }

            return null;
        }

        public override Conversion ClassifyConversion(ExpressionSyntax expression, ITypeSymbol destination, bool isExplicitInSource = false)
        {
            var csdestination = destination.EnsureCSharpSymbolOrNull<ITypeSymbol, TypeSymbol>("destination");

            if (isExplicitInSource)
            {
                return ClassifyConversionForCast(expression, csdestination);
            }

            CheckSyntaxNode(expression);

            if ((object)destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            // TODO(cyrusn): Check arguments. This is a public entrypoint, so we must do appropriate
            // checks here. However, no other methods in this type do any checking currently. So I'm
            // going to hold off on this until we do a full sweep of the API.

            var model = this.GetMemberModel(expression);
            if (model == null)
            {
                // 'expression' must just be reference to a type or namespace name outside of an
                // expression context.  Currently we bail in that case.  However, is this a question
                // that a client would be asking and would expect sensible results for?
                return Conversion.NoConversion;
            }

            return model.ClassifyConversion(expression, destination);
        }

        internal override Conversion ClassifyConversionForCast(ExpressionSyntax expression, TypeSymbol destination)
        {
            CheckSyntaxNode(expression);

            if ((object)destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            var model = this.GetMemberModel(expression);
            if (model == null)
            {
                // 'expression' must just be reference to a type or namespace name outside of an
                // expression context.  Currently we bail in that case.  However, is this a question
                // that a client would be asking and would expect sensible results for?
                return Conversion.NoConversion;
            }

            return model.ClassifyConversionForCast(expression, destination);
        }

        public override bool IsSpeculativeSemanticModel
        {
            get { return false; }
        }

        public override int OriginalPositionForSpeculation
        {
            get { return 0; }
        }

        public override CSharpSemanticModel ParentModel
        {
            get { return null; }
        }

        internal sealed override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, TypeSyntax type, SpeculativeBindingOption bindingOption, out SemanticModel speculativeModel)
        {
            position = CheckAndAdjustPosition(position);

            var model = this.GetMemberModel(position);
            if (model != null)
            {
                return model.TryGetSpeculativeSemanticModelCore(parentModel, position, type, bindingOption, out speculativeModel);
            }

            Binder binder = GetSpeculativeBinder(position, type, bindingOption);
            if (binder != null)
            {
                speculativeModel = SpeculativeSyntaxTreeSemanticModel.Create(parentModel, type, binder, position, bindingOption);
                return true;
            }

            speculativeModel = null;
            return false;
        }

        internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, CrefSyntax crefSyntax, out SemanticModel speculativeModel)
        {
            position = CheckAndAdjustPosition(position);

            Binder binder = GetEnclosingBinder(position);
            if (binder?.InCref == true)
            {
                speculativeModel = SpeculativeSyntaxTreeSemanticModel.Create(parentModel, crefSyntax, binder, position);
                return true;
            }

            speculativeModel = null;
            return false;
        }

        internal sealed override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, StatementSyntax statement, out SemanticModel speculativeModel)
        {
            position = CheckAndAdjustPosition(position);

            var model = this.GetMemberModel(position);
            if (model != null)
            {
                return model.TryGetSpeculativeSemanticModelCore(parentModel, position, statement, out speculativeModel);
            }

            speculativeModel = null;
            return false;
        }

        internal sealed override bool TryGetSpeculativeSemanticModelForMethodBodyCore(SyntaxTreeSemanticModel parentModel, int position, BaseMethodDeclarationSyntax method, out SemanticModel speculativeModel)
        {
            position = CheckAndAdjustPosition(position);

            var model = this.GetMemberModel(position);
            if (model != null)
            {
                return model.TryGetSpeculativeSemanticModelForMethodBodyCore(parentModel, position, method, out speculativeModel);
            }

            speculativeModel = null;
            return false;
        }

        internal sealed override bool TryGetSpeculativeSemanticModelForMethodBodyCore(SyntaxTreeSemanticModel parentModel, int position, AccessorDeclarationSyntax accessor, out SemanticModel speculativeModel)
        {
            position = CheckAndAdjustPosition(position);

            var model = this.GetMemberModel(position);
            if (model != null)
            {
                return model.TryGetSpeculativeSemanticModelForMethodBodyCore(parentModel, position, accessor, out speculativeModel);
            }

            speculativeModel = null;
            return false;
        }

        internal sealed override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, EqualsValueClauseSyntax initializer, out SemanticModel speculativeModel)
        {
            position = CheckAndAdjustPosition(position);

            var model = this.GetMemberModel(position);
            if (model != null)
            {
                return model.TryGetSpeculativeSemanticModelCore(parentModel, position, initializer, out speculativeModel);
            }

            speculativeModel = null;
            return false;
        }

        internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, ArrowExpressionClauseSyntax expressionBody, out SemanticModel speculativeModel)
        {
            position = CheckAndAdjustPosition(position);

            var model = this.GetMemberModel(position);
            if (model != null)
            {
                return model.TryGetSpeculativeSemanticModelCore(parentModel, position, expressionBody, out speculativeModel);
            }

            speculativeModel = null;
            return false;
        }

        internal sealed override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, ConstructorInitializerSyntax constructorInitializer, out SemanticModel speculativeModel)
        {
            position = CheckAndAdjustPosition(position);

            var model = this.GetMemberModel(position);
            if (model != null)
            {
                return model.TryGetSpeculativeSemanticModelCore(parentModel, position, constructorInitializer, out speculativeModel);
            }

            speculativeModel = null;
            return false;
        }

        private MemberSemanticModel GetMemberModel(int position)
        {
            AssertPositionAdjusted(position);
            CSharpSyntaxNode node = (CSharpSyntaxNode)Root.FindTokenIncludingCrefAndNameAttributes(position).Parent;
            CSharpSyntaxNode memberDecl = GetMemberDeclaration(node);

            bool outsideMemberDecl = false;
            if (memberDecl != null)
            {
                switch (memberDecl.Kind())
                {
                    case SyntaxKind.AddAccessorDeclaration:
                    case SyntaxKind.RemoveAccessorDeclaration:
                    case SyntaxKind.GetAccessorDeclaration:
                    case SyntaxKind.SetAccessorDeclaration:
                        // NOTE: not UnknownAccessorDeclaration since there's no corresponding method symbol from which to build a member model.
                        outsideMemberDecl = !LookupPosition.IsInBlock(position, ((AccessorDeclarationSyntax)memberDecl).Body);
                        break;
                    case SyntaxKind.ConstructorDeclaration:
                        var constructorDecl = (ConstructorDeclarationSyntax)memberDecl;
                        outsideMemberDecl =
                            !LookupPosition.IsInConstructorParameterScope(position, constructorDecl) &&
                            !LookupPosition.IsInParameterList(position, constructorDecl);
                        break;
                    case SyntaxKind.ConversionOperatorDeclaration:
                    case SyntaxKind.DestructorDeclaration:
                    case SyntaxKind.MethodDeclaration:
                    case SyntaxKind.OperatorDeclaration:
                        var methodDecl = (BaseMethodDeclarationSyntax)memberDecl;
                        outsideMemberDecl =
                            !LookupPosition.IsInBlock(position, methodDecl.Body) &&
                            !LookupPosition.IsInParameterList(position, methodDecl) &&
                            !LookupPosition.IsInExpressionBody(position, methodDecl.GetExpressionBodySyntax(), methodDecl.SemicolonToken);
                        break;
                }
            }

            return outsideMemberDecl ? null : GetMemberModel(node);
        }

        // Try to get a member semantic model that encloses "node". If there is not an enclosing
        // member semantic model, return null.
        internal override MemberSemanticModel GetMemberModel(CSharpSyntaxNode node)
        {
            // Documentation comments can never legally appear within members, so there's no point
            // in building out the MemberSemanticModel to handle them.  Instead, just say have
            // SyntaxTreeSemanticModel handle them, regardless of location.
            if (IsInDocumentationComment(node))
            {
                return null;
            }

            var memberDecl = GetMemberDeclaration(node);
            if (memberDecl != null)
            {
                var span = node.Span;

                switch (memberDecl.Kind())
                {
                    case SyntaxKind.DelegateDeclaration:
                        {
                            DelegateDeclarationSyntax delegateDecl = (DelegateDeclarationSyntax)memberDecl;
                            return GetOrAddModelForParameterDefaultValue(delegateDecl.ParameterList.Parameters, span);
                        }
                    case SyntaxKind.MethodDeclaration:
                    case SyntaxKind.ConversionOperatorDeclaration:
                    case SyntaxKind.OperatorDeclaration:
                        {
                            var methodDecl = (BaseMethodDeclarationSyntax)memberDecl;
                            var expressionBody = methodDecl.GetExpressionBodySyntax();
                            return GetOrAddModelForParameterDefaultValue(methodDecl.ParameterList.Parameters, span) ??
                                   GetOrAddModelIfContains(expressionBody, span) ??
                                   GetOrAddModelIfContains(methodDecl.Body, span);
                        }

                    case SyntaxKind.ConstructorDeclaration:
                        {
                            ConstructorDeclarationSyntax constructorDecl = (ConstructorDeclarationSyntax)memberDecl;
                            return GetOrAddModelForParameterDefaultValue(constructorDecl.ParameterList.Parameters, span) ??
                                GetOrAddModelIfContains(constructorDecl.Initializer, span) ??
                                GetOrAddModelIfContains(constructorDecl.Body, span);
                        }

                    case SyntaxKind.DestructorDeclaration:
                        {
                            DestructorDeclarationSyntax destructorDecl = (DestructorDeclarationSyntax)memberDecl;
                            return GetOrAddModelIfContains(destructorDecl.Body, span);
                        }

                    case SyntaxKind.IndexerDeclaration:
                        {
                            var indexerDecl = (IndexerDeclarationSyntax)memberDecl;
                            return GetOrAddModelForParameterDefaultValue(
                                indexerDecl.ParameterList.Parameters, span) ??
                                GetOrAddModelIfContains(indexerDecl.ExpressionBody, span);
                        }

                    case SyntaxKind.FieldDeclaration:
                    case SyntaxKind.EventFieldDeclaration:
                        {
                            var fieldDecl = (BaseFieldDeclarationSyntax)memberDecl;
                            foreach (var variableDecl in fieldDecl.Declaration.Variables)
                            {
                                var binding = GetOrAddModelIfContains(variableDecl.Initializer, span);
                                if (binding != null)
                                {
                                    return binding;
                                }
                            }
                        }
                        break;

                    case SyntaxKind.EnumMemberDeclaration:
                        {
                            var enumDecl = (EnumMemberDeclarationSyntax)memberDecl;
                            return (enumDecl.EqualsValue != null) ?
                                GetOrAddModelIfContains(enumDecl.EqualsValue, span) :
                                null;
                        }

                    case SyntaxKind.PropertyDeclaration:
                        {
                            var propertyDecl = (PropertyDeclarationSyntax)memberDecl;
                            return GetOrAddModelIfContains(propertyDecl.Initializer, span) ??
                                GetOrAddModelIfContains(propertyDecl.ExpressionBody, span);
                        }

                    case SyntaxKind.GetAccessorDeclaration:
                    case SyntaxKind.SetAccessorDeclaration:
                    case SyntaxKind.AddAccessorDeclaration:
                    case SyntaxKind.RemoveAccessorDeclaration:
                        // NOTE: not UnknownAccessorDeclaration since there's no corresponding method symbol from which to build a member model.
                        return GetOrAddModelIfContains(((AccessorDeclarationSyntax)memberDecl).Body, span);

                    case SyntaxKind.GlobalStatement:
                        return GetOrAddModel(memberDecl);

                    case SyntaxKind.Attribute:
                        return GetOrAddModel(memberDecl);
                }
            }

            return null;
        }

        private static bool IsInDocumentationComment(CSharpSyntaxNode node)
        {
            for (CSharpSyntaxNode curr = node; curr != null; curr = curr.Parent)
            {
                if (SyntaxFacts.IsDocumentationCommentTrivia(curr.Kind()))
                {
                    return true;
                }
            }

            return false;
        }

        // Check each parameter for a default value containing span, and create an InitializerSemanticModel for binding the default value if so.
        // Otherwise, return null.
        private MemberSemanticModel GetOrAddModelForParameterDefaultValue(SeparatedSyntaxList<ParameterSyntax> parameterList, TextSpan span)
        {
            foreach (ParameterSyntax paramDecl in parameterList)
            {
                MemberSemanticModel model = GetOrAddModelIfContains(paramDecl.Default, span);
                if (model != null)
                    return model;
            }

            return null;
        }

        private static CSharpSyntaxNode GetMemberDeclaration(CSharpSyntaxNode node)
        {
            return node.FirstAncestorOrSelf(s_isMemberDeclarationFunction);
        }

        private MemberSemanticModel GetOrAddModelIfContains(CSharpSyntaxNode node, TextSpan span)
        {
            if (node != null && node.FullSpan.Contains(span))
            {
                return GetOrAddModel(node);
            }
            return null;
        }

        private MemberSemanticModel GetOrAddModel(CSharpSyntaxNode node)
        {
            var createMemberModelFunction = _createMemberModelFunction ??
                                            (_createMemberModelFunction = this.CreateMemberModel);

            return ImmutableInterlocked.GetOrAdd(ref _memberModels, node, createMemberModelFunction);
        }

        // Create a member model for the given declaration syntax. In certain very malformed
        // syntax trees, there may not be a symbol that can have a member model associated with it
        // (although we try to minimize such cases). In such cases, null is returned.
        private MemberSemanticModel CreateMemberModel(CSharpSyntaxNode node)
        {
            var outer = _binderFactory.GetBinder(node);

            if (this.IgnoresAccessibility)
            {
                outer = outer.WithAdditionalFlags(BinderFlags.IgnoreAccessibility);
            }

            switch (node.Kind())
            {
                case SyntaxKind.Block:

                    MemberDeclarationSyntax memberDecl;
                    AccessorDeclarationSyntax accessorDecl;
                    if ((memberDecl = node.Parent as MemberDeclarationSyntax) != null)
                    {
                        var symbol = (SourceMethodSymbol)GetDeclaredSymbol(memberDecl);
                        if ((object)symbol == null)
                            return null;

                        return MethodBodySemanticModel.Create(this.Compilation, symbol, outer, memberDecl);
                    }
                    else if ((accessorDecl = node.Parent as AccessorDeclarationSyntax) != null)
                    {
                        var symbol = (SourceMethodSymbol)GetDeclaredSymbol(accessorDecl);
                        if ((object)symbol == null)
                            return null;

                        return MethodBodySemanticModel.Create(this.Compilation, symbol, outer, accessorDecl);
                    }
                    else
                    {
                        Debug.Assert(false, "Unexpected node: " + node.Parent);
                        return null;
                    }

                case SyntaxKind.EqualsValueClause:
                    switch (node.Parent.Kind())
                    {
                        case SyntaxKind.VariableDeclarator:
                            {
                                var variableDecl = (VariableDeclaratorSyntax)node.Parent;
                                SourceMemberFieldSymbol fieldSymbol = GetDeclaredFieldSymbol(variableDecl);

                                return InitializerSemanticModel.Create(
                                    this.Compilation,
                                    variableDecl,   //pass in the entire field initializer to permit region analysis. 
                                    fieldSymbol,
                                    //if we're in regular C#, then insert an extra binder to perform field initialization checks
                                    GetFieldOrPropertyInitializerBinder(fieldSymbol, outer, variableDecl.Initializer?.Value));
                            }

                        case SyntaxKind.PropertyDeclaration:
                            {
                                var propertyDecl = (PropertyDeclarationSyntax)node.Parent;
                                var propertySymbol = (SourcePropertySymbol)GetDeclaredSymbol(propertyDecl);
                                return InitializerSemanticModel.Create(
                                    this.Compilation,
                                    propertyDecl,
                                    propertySymbol,
                                    GetFieldOrPropertyInitializerBinder(propertySymbol.BackingField, outer, propertyDecl.Initializer?.Value));
                            }

                        case SyntaxKind.Parameter:
                            {
                                // NOTE: we don't need to create a member model for lambda parameter default value
                                // (which is bad code anyway) because lambdas only appear in code with associated
                                // member models.
                                ParameterSyntax parameterDecl = (ParameterSyntax)node.Parent;
                                ParameterSymbol parameterSymbol = GetDeclaredNonLambdaParameterSymbol(parameterDecl);
                                if ((object)parameterSymbol == null)
                                    return null;

                                return InitializerSemanticModel.Create(
                                    this.Compilation,
                                    parameterDecl,
                                    parameterSymbol,
                                    outer.CreateBinderForParameterDefaultValue(parameterSymbol, (EqualsValueClauseSyntax)node));
                            }

                        case SyntaxKind.EnumMemberDeclaration:
                            {
                                var enumDecl = (EnumMemberDeclarationSyntax)node.Parent;
                                var enumSymbol = (FieldSymbol)GetDeclaredSymbol(enumDecl);
                                if ((object)enumSymbol == null)
                                    return null;

                                return InitializerSemanticModel.Create(
                                    this.Compilation,
                                    enumDecl,
                                    enumSymbol,
                                    GetFieldOrPropertyInitializerBinder(enumSymbol, outer, enumDecl.EqualsValue?.Value));
                            }
                        default:
                            throw ExceptionUtilities.UnexpectedValue(node.Parent.Kind());
                    }

                case SyntaxKind.ArrowExpressionClause:
                    {
                        SourceMethodSymbol symbol = null;
                        MemberDeclarationSyntax memberSyntax;
                        var exprDecl = (ArrowExpressionClauseSyntax)node;

                        if (node.Parent is BasePropertyDeclarationSyntax)
                        {
                            symbol = (SourceMethodSymbol)GetDeclaredSymbol(exprDecl);
                        }
                        else if ((memberSyntax = node.Parent as MemberDeclarationSyntax) != null)
                        {
                            symbol = (SourceMethodSymbol)GetDeclaredSymbol(node.Parent as MemberDeclarationSyntax);
                        }
                        else
                        {
                            // Don't throw, just use for the assert
                            ExceptionUtilities.UnexpectedValue(node.Parent);
                        }

                        if ((object)symbol == null)
                            return null;
                        return MethodBodySemanticModel.Create(
                            _compilation, symbol, outer.WithContainingMemberOrLambda(symbol), exprDecl);
                    }

                case SyntaxKind.GlobalStatement:
                    {
                        Debug.Assert(!this.IsRegularCSharp);
                        var parent = node.Parent;
                        // TODO (tomat): handle misplaced global statements
                        if (parent.Kind() == SyntaxKind.CompilationUnit)
                        {
                            var scriptInitializer = _compilation.ScriptClass.GetScriptInitializer();
                            Debug.Assert((object)scriptInitializer != null);
                            if ((object)scriptInitializer == null)
                            {
                                return null;
                            }

                            // Share labels across all global statements.
                            if (_globalStatementLabels == null)
                            {
                                Interlocked.CompareExchange(ref _globalStatementLabels, new ScriptLocalScopeBinder.Labels(scriptInitializer, (CompilationUnitSyntax)parent), null);
                            }

                            return MethodBodySemanticModel.Create(
                                this.Compilation,
                                scriptInitializer,
                                new ScriptLocalScopeBinder(_globalStatementLabels, outer),
                                node);
                        }
                    }
                    break;

                case SyntaxKind.BaseConstructorInitializer:
                case SyntaxKind.ThisConstructorInitializer:
                    {
                        var constructorDecl = (ConstructorDeclarationSyntax)node.Parent;
                        var constructorSymbol = (SourceMethodSymbol)GetDeclaredSymbol(constructorDecl);
                        if ((object)constructorSymbol == null)
                            return null;

                        return InitializerSemanticModel.Create(
                            this.Compilation,
                            (ConstructorInitializerSyntax)node,
                            constructorSymbol,
                            //insert an extra binder to perform constructor initialization checks
                            outer.WithAdditionalFlagsAndContainingMemberOrLambda(BinderFlags.ConstructorInitializer, constructorSymbol));
                    }

                case SyntaxKind.Attribute:
                    {
                        var attribute = (AttributeSyntax)node;
                        AliasSymbol aliasOpt;
                        DiagnosticBag discarded = DiagnosticBag.GetInstance();
                        var attributeType = (NamedTypeSymbol)outer.BindType(attribute.Name, discarded, out aliasOpt);
                        discarded.Free();

                        return AttributeSemanticModel.Create(
                            _compilation,
                            attribute,
                            attributeType,
                            aliasOpt,
                            outer.WithAdditionalFlags(BinderFlags.AttributeArgument));
                    }
            }

            return null;
        }

        private SourceMemberFieldSymbol GetDeclaredFieldSymbol(VariableDeclaratorSyntax variableDecl)
        {
            var declaredSymbol = GetDeclaredSymbol(variableDecl);

            if ((object)declaredSymbol != null)
            {
                switch (variableDecl.Parent.Parent.Kind())
                {
                    case SyntaxKind.FieldDeclaration:
                        return (SourceMemberFieldSymbol)declaredSymbol;

                    case SyntaxKind.EventFieldDeclaration:
                        return (SourceMemberFieldSymbol)((EventSymbol)declaredSymbol).AssociatedField;
                }
            }

            return null;
        }

        private Binder GetFieldOrPropertyInitializerBinder(FieldSymbol symbol, Binder outer, ExpressionSyntax valueSyntaxOpt)
        {
            BinderFlags flags = BinderFlags.None;

            // NOTE: checking for a containing script class is sufficient, but the regular C# test is quick and easy.
            if (this.IsRegularCSharp || !symbol.ContainingType.IsScriptClass)
            {
                flags |= BinderFlags.FieldInitializer;
            }

            Binder result = new LocalScopeBinder(outer).WithAdditionalFlagsAndContainingMemberOrLambda(flags, symbol);
            if (valueSyntaxOpt != null)
            {
                result = result.WithPatternVariablesIfAny(valueSyntaxOpt);
            }

            return result;
        }

        private static bool IsMemberDeclaration(CSharpSyntaxNode node)
        {
            return (node is MemberDeclarationSyntax) || (node is AccessorDeclarationSyntax) || (node is AttributeSyntax);
        }

        private bool IsRegularCSharp
        {
            get
            {
                return this.SyntaxTree.Options.Kind == SourceCodeKind.Regular;
            }
        }

        #region "GetDeclaredSymbol overloads for MemberDeclarationSyntax and its subtypes"

        /// <summary>
        /// Given a namespace declaration syntax node, get the corresponding namespace symbol for the declaration
        /// assembly.
        /// </summary>
        /// <param name="declarationSyntax">The syntax node that declares a namespace.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The namespace symbol that was declared by the namespace declaration.</returns>
        public override INamespaceSymbol GetDeclaredSymbol(NamespaceDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckSyntaxNode(declarationSyntax);

            return GetDeclaredNamespace(declarationSyntax);
        }

        private NamespaceSymbol GetDeclaredNamespace(NamespaceDeclarationSyntax declarationSyntax)
        {
            Debug.Assert(declarationSyntax != null);

            NamespaceOrTypeSymbol container;
            if (declarationSyntax.Parent.Kind() == SyntaxKind.CompilationUnit)
            {
                container = _compilation.Assembly.GlobalNamespace;
            }
            else
            {
                container = GetDeclaredNamespaceOrType(declarationSyntax.Parent);
            }

            Debug.Assert((object)container != null);

            // We should get a namespace symbol since we match the symbol location with a namespace declaration syntax location.
            var symbol = (NamespaceSymbol)GetDeclaredMember(container, declarationSyntax.Span, declarationSyntax.Name);
            Debug.Assert((object)symbol != null);

            // Map to compilation-scoped namespace (Roslyn bug 9538)
            symbol = _compilation.GetCompilationNamespace(symbol);
            Debug.Assert((object)symbol != null);

            return symbol;
        }

        /// <summary>
        /// Given a type declaration, get the corresponding type symbol.
        /// </summary>
        /// <param name="declarationSyntax">The syntax node that declares a type.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The type symbol that was declared.</returns>
        /// <remarks>
        /// NOTE:   We have no GetDeclaredSymbol overloads for subtypes of BaseTypeDeclarationSyntax as all of them return a NamedTypeSymbol.
        /// </remarks>
        public override INamedTypeSymbol GetDeclaredSymbol(BaseTypeDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckSyntaxNode(declarationSyntax);

            return GetDeclaredType(declarationSyntax);
        }

        /// <summary>
        /// Given a delegate declaration, get the corresponding type symbol.
        /// </summary>
        /// <param name="declarationSyntax">The syntax node that declares a delegate.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The type symbol that was declared.</returns>
        public override INamedTypeSymbol GetDeclaredSymbol(DelegateDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckSyntaxNode(declarationSyntax);

            return GetDeclaredType(declarationSyntax);
        }

        private NamedTypeSymbol GetDeclaredType(BaseTypeDeclarationSyntax declarationSyntax)
        {
            Debug.Assert(declarationSyntax != null);

            var name = declarationSyntax.Identifier.ValueText;
            return GetDeclaredNamedType(declarationSyntax, name);
        }

        private NamedTypeSymbol GetDeclaredType(DelegateDeclarationSyntax declarationSyntax)
        {
            Debug.Assert(declarationSyntax != null);

            var name = declarationSyntax.Identifier.ValueText;
            return GetDeclaredNamedType(declarationSyntax, name);
        }

        private NamedTypeSymbol GetDeclaredNamedType(CSharpSyntaxNode declarationSyntax, string name)
        {
            Debug.Assert(declarationSyntax != null);

            var container = GetDeclaredTypeMemberContainer(declarationSyntax);
            Debug.Assert((object)container != null);

            // try cast as we might get a non-type in error recovery scenarios:
            return GetDeclaredMember(container, declarationSyntax.Span, name) as NamedTypeSymbol;
        }

        private NamespaceOrTypeSymbol GetDeclaredNamespaceOrType(CSharpSyntaxNode declarationSyntax)
        {
            var namespaceDeclarationSyntax = declarationSyntax as NamespaceDeclarationSyntax;
            if (namespaceDeclarationSyntax != null)
            {
                return GetDeclaredNamespace(namespaceDeclarationSyntax);
            }

            var typeDeclarationSyntax = declarationSyntax as BaseTypeDeclarationSyntax;
            if (typeDeclarationSyntax != null)
            {
                return GetDeclaredType(typeDeclarationSyntax);
            }

            var delegateDeclarationSyntax = declarationSyntax as DelegateDeclarationSyntax;
            if (delegateDeclarationSyntax != null)
            {
                return GetDeclaredType(delegateDeclarationSyntax);
            }

            return null;
        }

        /// <summary>
        /// Given an member declaration syntax, get the corresponding symbol.
        /// </summary>
        /// <param name="declarationSyntax">The syntax node that declares a member.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The symbol that was declared.</returns>
        /// <remarks>
        /// NOTE:   We have no GetDeclaredSymbol overloads for following subtypes of MemberDeclarationSyntax:
        /// NOTE:   (1) GlobalStatementSyntax as they don't declare any symbols.
        /// NOTE:   (2) IncompleteMemberSyntax as there are no symbols for incomplete members.
        /// NOTE:   (3) BaseFieldDeclarationSyntax or its subtypes as these declarations can contain multiple variable declarators.
        /// NOTE:       GetDeclaredSymbol should be called on the variable declarators directly.
        /// </remarks>
        public override ISymbol GetDeclaredSymbol(MemberDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckSyntaxNode(declarationSyntax);

            switch (declarationSyntax.Kind())
            {
                // Few subtypes of MemberDeclarationSyntax don't declare any symbols or declare multiple symbols, return null for these cases.

                case SyntaxKind.GlobalStatement:
                    // Global statements don't declare anything, even though they inherit from MemberDeclarationSyntax.
                    return null;

                case SyntaxKind.IncompleteMember:
                    // Incomplete members don't declare any symbols.
                    return null;

                case SyntaxKind.EventFieldDeclaration:
                case SyntaxKind.FieldDeclaration:
                    // these declarations can contain multiple variable declarators. GetDeclaredSymbol should be called on them directly.
                    return null;

                default:
                    return GetDeclaredNamespaceOrType(declarationSyntax) ?? GetDeclaredMemberSymbol(declarationSyntax);
            }
        }

        /// <summary>
        /// Given a local function declaration syntax, get the corresponding symbol.
        /// </summary>
        /// <param name="declarationSyntax">The syntax node that declares a member.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The symbol that was declared.</returns>
        public override ISymbol GetDeclaredSymbol(LocalFunctionStatementSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckSyntaxNode(declarationSyntax);

            return this.GetMemberModel(declarationSyntax)?.GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        /// <summary>
        /// Given a enum member declaration, get the corresponding field symbol.
        /// </summary>
        /// <param name="declarationSyntax">The syntax node that declares an enum member.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The symbol that was declared.</returns>
        public override IFieldSymbol GetDeclaredSymbol(EnumMemberDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return (IFieldSymbol)GetDeclaredMemberSymbol(declarationSyntax);
        }

        /// <summary>
        /// Given a base method declaration syntax, get the corresponding method symbol.
        /// </summary>
        /// <param name="declarationSyntax">The syntax node that declares a method.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The symbol that was declared.</returns>
        /// <remarks>
        /// NOTE:   We have no GetDeclaredSymbol overloads for subtypes of BaseMethodDeclarationSyntax as all of them return a MethodSymbol.
        /// </remarks>
        public override IMethodSymbol GetDeclaredSymbol(BaseMethodDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return (IMethodSymbol)GetDeclaredMemberSymbol(declarationSyntax);
        }

        #region "GetDeclaredSymbol overloads for BasePropertyDeclarationSyntax and its subtypes"

        /// <summary>
        /// Given a syntax node that declares a property, indexer or an event, get the corresponding declared symbol.
        /// </summary>
        /// <param name="declarationSyntax">The syntax node that declares a property, indexer or an event.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The symbol that was declared.</returns>
        public override ISymbol GetDeclaredSymbol(BasePropertyDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetDeclaredMemberSymbol(declarationSyntax);
        }

        /// <summary>
        /// Given a syntax node that declares a property, get the corresponding declared symbol.
        /// </summary>
        /// <param name="declarationSyntax">The syntax node that declares a property, indexer or an event.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The symbol that was declared.</returns>
        public override IPropertySymbol GetDeclaredSymbol(PropertyDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return (IPropertySymbol)GetDeclaredMemberSymbol(declarationSyntax);
        }

        /// <summary>
        /// Given a syntax node that declares an indexer, get the corresponding declared symbol.
        /// </summary>
        /// <param name="declarationSyntax">The syntax node that declares an indexer.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The symbol that was declared.</returns>
        public override IPropertySymbol GetDeclaredSymbol(IndexerDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return (IPropertySymbol)GetDeclaredMemberSymbol(declarationSyntax);
        }

        /// <summary>
        /// Given a syntax node that declares a (custom) event, get the corresponding event symbol.
        /// </summary>
        /// <param name="declarationSyntax">The syntax node that declares a event.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The symbol that was declared.</returns>
        public override IEventSymbol GetDeclaredSymbol(EventDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return (IEventSymbol)GetDeclaredMemberSymbol(declarationSyntax);
        }

        #endregion

        #endregion

        /// <summary>
        /// Given an syntax node that declares a property or member accessor, get the corresponding symbol.
        /// </summary>
        /// <param name="declarationSyntax">The syntax node that declares an accessor.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The symbol that was declared.</returns>
        public override IMethodSymbol GetDeclaredSymbol(AccessorDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckSyntaxNode(declarationSyntax);

            if (declarationSyntax.Kind() == SyntaxKind.UnknownAccessorDeclaration)
            {
                // this is not a real accessor, so we shouldn't return anything.
                return null;
            }

            var propertyOrEventDecl = declarationSyntax.Parent.Parent;

            switch (propertyOrEventDecl.Kind())
            {
                case SyntaxKind.PropertyDeclaration:
                case SyntaxKind.IndexerDeclaration:
                case SyntaxKind.EventDeclaration:
                case SyntaxKind.EventFieldDeclaration:
                    // NOTE: it's an error for field-like events to have accessors, 
                    // but we want to bind them anyway for error tolerance reasons.
                    var container = GetDeclaredTypeMemberContainer(propertyOrEventDecl);
                    Debug.Assert((object)container != null);
                    Debug.Assert(declarationSyntax.Keyword.Kind() != SyntaxKind.IdentifierToken);
                    return this.GetDeclaredMember(container, declarationSyntax.Span) as MethodSymbol;

                default:
                    throw ExceptionUtilities.UnexpectedValue(propertyOrEventDecl.Kind());
            }
        }

        public override IMethodSymbol GetDeclaredSymbol(ArrowExpressionClauseSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckSyntaxNode(declarationSyntax);

            var containingMemberSyntax = declarationSyntax.Parent;

            NamespaceOrTypeSymbol container;
            switch (containingMemberSyntax.Kind())
            {
                case SyntaxKind.PropertyDeclaration:
                case SyntaxKind.IndexerDeclaration:
                    container = GetDeclaredTypeMemberContainer(containingMemberSyntax);
                    Debug.Assert((object)container != null);
                    // We are looking for the SourcePropertyAccessorSymbol here,
                    // not the SourcePropertySymbol, so use declarationSyntax
                    // to exclude the property symbol from being retrieved.
                    return this.GetDeclaredMember(container, declarationSyntax.Span) as MethodSymbol;

                default:
                    // Don't throw, use only for the assert
                    ExceptionUtilities.UnexpectedValue(containingMemberSyntax.Kind());
                    return null;
            }
        }

        private string GetDeclarationName(CSharpSyntaxNode declaration)
        {
            switch (declaration.Kind())
            {
                case SyntaxKind.MethodDeclaration:
                    {
                        var methodDecl = (MethodDeclarationSyntax)declaration;
                        return GetDeclarationName(declaration, methodDecl.ExplicitInterfaceSpecifier, methodDecl.Identifier.ValueText);
                    }

                case SyntaxKind.PropertyDeclaration:
                    {
                        var propertyDecl = (PropertyDeclarationSyntax)declaration;
                        return GetDeclarationName(declaration, propertyDecl.ExplicitInterfaceSpecifier, propertyDecl.Identifier.ValueText);
                    }

                case SyntaxKind.IndexerDeclaration:
                    {
                        var indexerDecl = (IndexerDeclarationSyntax)declaration;
                        return GetDeclarationName(declaration, indexerDecl.ExplicitInterfaceSpecifier, WellKnownMemberNames.Indexer);
                    }

                case SyntaxKind.EventDeclaration:
                    {
                        var eventDecl = (EventDeclarationSyntax)declaration;
                        return GetDeclarationName(declaration, eventDecl.ExplicitInterfaceSpecifier, eventDecl.Identifier.ValueText);
                    }

                case SyntaxKind.DelegateDeclaration:
                    return ((DelegateDeclarationSyntax)declaration).Identifier.ValueText;

                case SyntaxKind.InterfaceDeclaration:
                case SyntaxKind.StructDeclaration:
                case SyntaxKind.ClassDeclaration:
                case SyntaxKind.EnumDeclaration:
                    return ((BaseTypeDeclarationSyntax)declaration).Identifier.ValueText;

                case SyntaxKind.VariableDeclarator:
                    return ((VariableDeclaratorSyntax)declaration).Identifier.ValueText;

                case SyntaxKind.EnumMemberDeclaration:
                    return ((EnumMemberDeclarationSyntax)declaration).Identifier.ValueText;

                case SyntaxKind.DestructorDeclaration:
                    return WellKnownMemberNames.DestructorName;

                case SyntaxKind.ConstructorDeclaration:
                    if (((ConstructorDeclarationSyntax)declaration).Modifiers.Any(SyntaxKind.StaticKeyword))
                    {
                        return WellKnownMemberNames.StaticConstructorName;
                    }
                    else
                    {
                        return WellKnownMemberNames.InstanceConstructorName;
                    }

                case SyntaxKind.OperatorDeclaration:
                    var operatorDecl = (OperatorDeclarationSyntax)declaration;

                    return OperatorFacts.OperatorNameFromDeclaration(operatorDecl);

                case SyntaxKind.ConversionOperatorDeclaration:
                    if (((ConversionOperatorDeclarationSyntax)declaration).ImplicitOrExplicitKeyword.Kind() == SyntaxKind.ExplicitKeyword)
                    {
                        return WellKnownMemberNames.ExplicitConversionName;
                    }
                    else
                    {
                        return WellKnownMemberNames.ImplicitConversionName;
                    }

                case SyntaxKind.EventFieldDeclaration:
                case SyntaxKind.FieldDeclaration:
                    throw new ArgumentException(CSharpResources.InvalidGetDeclarationNameMultipleDeclarators);

                case SyntaxKind.IncompleteMember:
                    // There is no name - that's why it's an incomplete member.
                    return null;

                default:
                    throw ExceptionUtilities.UnexpectedValue(declaration.Kind());
            }
        }

        private string GetDeclarationName(CSharpSyntaxNode declaration, ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifierOpt, string memberName)
        {
            if (explicitInterfaceSpecifierOpt == null)
            {
                return memberName;
            }

            // For an explicit interface implementation, we actually have several options:
            //  Option 1: do nothing - it will retry without the name
            //  Option 2: detect explicit impl and return null
            //  Option 3: get a binder and figure out the name
            // For now, we're going with Option 3
            return ExplicitInterfaceHelpers.GetMemberName(_binderFactory.GetBinder(declaration), explicitInterfaceSpecifierOpt, memberName);
        }

        private Symbol GetDeclaredMember(NamespaceOrTypeSymbol container, TextSpan declarationSpan, NameSyntax name)
        {
            switch (name.Kind())
            {
                case SyntaxKind.GenericName:
                case SyntaxKind.IdentifierName:
                    return GetDeclaredMember(container, declarationSpan, ((SimpleNameSyntax)name).Identifier.ValueText);

                case SyntaxKind.QualifiedName:
                    var qn = (QualifiedNameSyntax)name;
                    var left = GetDeclaredMember(container, declarationSpan, qn.Left) as NamespaceOrTypeSymbol;
                    Debug.Assert((object)left != null);
                    return GetDeclaredMember(left, declarationSpan, qn.Right);

                case SyntaxKind.AliasQualifiedName:
                    // this is not supposed to happen, but we allow for errors don't we!
                    var an = (AliasQualifiedNameSyntax)name;
                    return GetDeclaredMember(container, declarationSpan, an.Name);

                default:
                    throw ExceptionUtilities.UnexpectedValue(name.Kind());
            }
        }

        /// <summary>
        /// Finds the member in the containing symbol which is inside the given declaration span.
        /// </summary>
        private Symbol GetDeclaredMember(NamespaceOrTypeSymbol container, TextSpan declarationSpan, string name = null)
        {
            if ((object)container == null)
            {
                return null;
            }

            // look for any member with same declaration location
            var collection = name != null ? container.GetMembers(name) : container.GetMembersUnordered();

            Symbol zeroWidthMatch = null;
            foreach (var symbol in collection)
            {
                var namedType = symbol as ImplicitNamedTypeSymbol;
                if ((object)namedType != null && namedType.IsImplicitClass)
                {
                    // look inside wrapper around illegally placed members in namespaces
                    var result = GetDeclaredMember(namedType, declarationSpan, name);
                    if ((object)result != null)
                    {
                        return result;
                    }
                }

                foreach (var loc in symbol.Locations)
                {
                    if (loc.IsInSource && loc.SourceTree == this.SyntaxTree && declarationSpan.Contains(loc.SourceSpan))
                    {
                        if (loc.SourceSpan.IsEmpty && loc.SourceSpan.End == declarationSpan.Start)
                        {
                            // exclude decls created via syntax recovery
                            zeroWidthMatch = symbol;
                        }
                        else
                        {
                            return symbol;
                        }
                    }
                }

                // Handle the case of the implementation of a partial method.
                var partial = symbol.Kind == SymbolKind.Method
                    ? ((MethodSymbol)symbol).PartialImplementationPart
                    : null;
                if ((object)partial != null)
                {
                    var loc = partial.Locations[0];
                    if (loc.IsInSource && loc.SourceTree == this.SyntaxTree && declarationSpan.Contains(loc.SourceSpan))
                    {
                        return partial;
                    }
                }
            }

            // If we didn't find anything better than the symbol that matched because of syntax error recovery, then return that.
            // Otherwise, if there's a name, try again without a name.
            // Otherwise, give up.
            return zeroWidthMatch ??
                (name != null ? GetDeclaredMember(container, declarationSpan) : null);
        }

        /// <summary>
        /// Given an variable declarator syntax, get the corresponding symbol.
        /// </summary>
        /// <param name="declarationSyntax">The syntax node that declares a variable.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The symbol that was declared.</returns>
        public override ISymbol GetDeclaredSymbol(VariableDeclaratorSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckSyntaxNode(declarationSyntax);

            var field = declarationSyntax.Parent == null ? null : declarationSyntax.Parent.Parent as BaseFieldDeclarationSyntax;
            if (field != null)
            {
                var container = GetDeclaredTypeMemberContainer(field);
                Debug.Assert((object)container != null);

                var result = this.GetDeclaredMember(container, declarationSyntax.Span, declarationSyntax.Identifier.ValueText);
                Debug.Assert((object)result != null);

                return result;
            }

            // Might be a local variable.
            var memberModel = this.GetMemberModel(declarationSyntax);
            return memberModel == null ? null : memberModel.GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        /// <summary>
        /// Given a declaration pattern syntax, get the corresponding symbol.
        /// </summary>
        /// <param name="declarationSyntax">The syntax node that declares a variable.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The symbol that was declared.</returns>
        public override ISymbol GetDeclaredSymbol(DeclarationPatternSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            var memberModel = this.GetMemberModel(declarationSyntax);
            return memberModel == null ? null : memberModel.GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        /// <summary>
        /// Given a let statement syntax, get the corresponding symbol if the statement uses [let &lt;identifier&gt; = ...] form.
        /// </summary>
        /// <param name="declarationSyntax">The syntax node that declares a variable.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The symbol that was declared.</returns>
        public override ISymbol GetDeclaredSymbol(LetStatementSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            var memberModel = this.GetMemberModel(declarationSyntax);
            return memberModel == null ? null : memberModel.GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        /// <summary>
        /// Given a labeled statement syntax, get the corresponding label symbol.
        /// </summary>
        /// <param name="declarationSyntax">The syntax node of the labeled statement.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The label symbol for that label.</returns>
        public override ILabelSymbol GetDeclaredSymbol(LabeledStatementSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckSyntaxNode(declarationSyntax);

            var memberModel = this.GetMemberModel(declarationSyntax);
            return memberModel == null ? null : memberModel.GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        /// <summary>
        /// Given a switch label syntax, get the corresponding label symbol.
        /// </summary>
        /// <param name="declarationSyntax">The syntax node of the switch label.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The label symbol for that label.</returns>
        public override ILabelSymbol GetDeclaredSymbol(SwitchLabelSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckSyntaxNode(declarationSyntax);

            var memberModel = this.GetMemberModel(declarationSyntax);
            return memberModel == null ? null : memberModel.GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        /// <summary>
        /// Given a using declaration get the corresponding symbol for the using alias that was introduced.  
        /// </summary>
        /// <param name="declarationSyntax"></param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The alias symbol that was declared.</returns>
        /// <remarks>
        /// If the using directive is an error because it attempts to introduce an alias for which an existing alias was
        /// previously declared in the same scope, the result is a newly-constructed AliasSymbol (i.e. not one from the
        /// symbol table).
        /// </remarks>
        public override IAliasSymbol GetDeclaredSymbol(
            UsingDirectiveSyntax declarationSyntax,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckSyntaxNode(declarationSyntax);

            if (declarationSyntax.Alias == null)
            {
                return null;
            }

            InContainerBinder binder = _binderFactory.GetImportsBinder(declarationSyntax.Parent);
            var imports = binder.GetImports(basesBeingResolved: null);
            var alias = imports.UsingAliases[declarationSyntax.Alias.Name.Identifier.ValueText];

            if ((object)alias.Alias == null)
            {
                // Case: no aliases
                return null;
            }
            else if (alias.Alias.Locations[0].SourceSpan == declarationSyntax.Alias.Name.Span)
            {
                // Case: first alias (there may be others)
                return alias.Alias;
            }
            else
            {
                // Case: multiple aliases, not the first (see DevDiv #9368)
                return new AliasSymbol(binder, declarationSyntax);
            }
        }

        /// <summary>
        /// Given an extern alias declaration get the corresponding symbol for the alias that was introduced.
        /// </summary>
        /// <param name="declarationSyntax"></param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The alias symbol that was declared, or null if a duplicate alias symbol was declared.</returns>
        public override IAliasSymbol GetDeclaredSymbol(ExternAliasDirectiveSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckSyntaxNode(declarationSyntax);

            var binder = _binderFactory.GetImportsBinder(declarationSyntax.Parent);
            var imports = binder.GetImports(basesBeingResolved: null);

            // TODO: If this becomes a bottleneck, put the extern aliases in a dictionary, as for using aliases.
            foreach (var alias in imports.ExternAliases)
            {
                if (alias.Alias.Locations[0].SourceSpan == declarationSyntax.Identifier.Span)
                {
                    return alias.Alias;
                }
            }

            return new AliasSymbol(binder, declarationSyntax);
        }

        /// <summary>
        /// Given a base field declaration syntax, get the corresponding symbols.
        /// </summary>
        /// <param name="declarationSyntax">The syntax node that declares one or more fields or events.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The field symbols that were declared.</returns>
        internal override ImmutableArray<ISymbol> GetDeclaredSymbols(BaseFieldDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckSyntaxNode(declarationSyntax);

            var builder = new ArrayBuilder<ISymbol>();

            foreach (var declarator in declarationSyntax.Declaration.Variables)
            {
                var field = this.GetDeclaredSymbol(declarator, cancellationToken) as ISymbol;
                if (field != null)
                {
                    builder.Add(field);
                }
            }

            return builder.ToImmutableAndFree();
        }

        private ParameterSymbol GetMethodParameterSymbol(
            ParameterSyntax parameter,
            CancellationToken cancellationToken)
        {
            Debug.Assert(parameter != null);

            var paramList = parameter.Parent as ParameterListSyntax;
            if (paramList == null)
            {
                return null;
            }

            var memberDecl = paramList.Parent as MemberDeclarationSyntax;
            if (memberDecl == null)
            {
                return null;
            }

            MethodSymbol method;

            method = GetDeclaredSymbol(memberDecl, cancellationToken) as MethodSymbol;

            if ((object)method == null)
            {
                return null;
            }

            return
                GetParameterSymbol(method.Parameters, parameter, cancellationToken) ??
                ((object)method.PartialDefinitionPart == null ? null : GetParameterSymbol(method.PartialDefinitionPart.Parameters, parameter, cancellationToken));
        }

        private ParameterSymbol GetIndexerParameterSymbol(
            ParameterSyntax parameter,
            CancellationToken cancellationToken)
        {
            Debug.Assert(parameter != null);

            var paramList = parameter.Parent as BracketedParameterListSyntax;
            if (paramList == null)
            {
                return null;
            }

            var memberDecl = paramList.Parent as MemberDeclarationSyntax;
            if (memberDecl == null)
            {
                return null;
            }

            var property = GetDeclaredSymbol(memberDecl, cancellationToken) as PropertySymbol;
            if ((object)property == null)
            {
                return null;
            }

            return GetParameterSymbol(property.Parameters, parameter, cancellationToken);
        }

        private ParameterSymbol GetDelegateParameterSymbol(
            ParameterSyntax parameter,
            CancellationToken cancellationToken)
        {
            Debug.Assert(parameter != null);

            var paramList = parameter.Parent as ParameterListSyntax;
            if (paramList == null)
            {
                return null;
            }

            var memberDecl = paramList.Parent as DelegateDeclarationSyntax;
            if (memberDecl == null)
            {
                return null;
            }

            var delegateType = GetDeclaredSymbol(memberDecl, cancellationToken) as NamedTypeSymbol;
            if ((object)delegateType == null)
            {
                return null;
            }

            var delegateInvoke = delegateType.DelegateInvokeMethod;
            if ((object)delegateInvoke == null || delegateInvoke.HasUseSiteError)
            {
                return null;
            }

            return GetParameterSymbol(delegateInvoke.Parameters, parameter, cancellationToken);
        }

        /// <summary>
        /// Given an parameter declaration syntax node, get the corresponding symbol.
        /// </summary>
        /// <param name="declarationSyntax">The syntax node that declares a parameter.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The parameter that was declared.</returns>
        public override IParameterSymbol GetDeclaredSymbol(ParameterSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckSyntaxNode(declarationSyntax);

            MemberSemanticModel memberModel = this.GetMemberModel(declarationSyntax);
            if (memberModel != null)
            {
                // Could be parameter of lambda.
                return memberModel.GetDeclaredSymbol(declarationSyntax, cancellationToken);
            }

            return GetDeclaredNonLambdaParameterSymbol(declarationSyntax, cancellationToken);
        }

        private ParameterSymbol GetDeclaredNonLambdaParameterSymbol(ParameterSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return
                GetMethodParameterSymbol(declarationSyntax, cancellationToken) ??
                GetIndexerParameterSymbol(declarationSyntax, cancellationToken) ??
                GetDelegateParameterSymbol(declarationSyntax, cancellationToken);
        }

        /// <summary>
        /// Given a type parameter declaration (field or method), get the corresponding symbol
        /// </summary>
        /// <param name="typeParameter"></param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public override ITypeParameterSymbol GetDeclaredSymbol(TypeParameterSyntax typeParameter, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (typeParameter == null)
            {
                throw new ArgumentNullException(nameof(typeParameter));
            }

            if (!IsInTree(typeParameter))
            {
                throw new ArgumentException("typeParameter not within tree");
            }

            var typeParamList = typeParameter.Parent as TypeParameterListSyntax;
            if (typeParamList != null)
            {
                var memberDecl = typeParamList.Parent as MemberDeclarationSyntax;
                if (memberDecl != null)
                {
                    var symbol = GetDeclaredSymbol(memberDecl, cancellationToken);
                    if ((object)symbol != null)
                    {
                        var typeSymbol = symbol as NamedTypeSymbol;
                        if ((object)typeSymbol != null)
                        {
                            return this.GetTypeParameterSymbol(typeSymbol.TypeParameters, typeParameter);
                        }

                        var methodSymbol = symbol as MethodSymbol;
                        if ((object)methodSymbol != null)
                        {
                            return
                                this.GetTypeParameterSymbol(methodSymbol.TypeParameters, typeParameter) ??
                                ((object)methodSymbol.PartialDefinitionPart == null ? null : this.GetTypeParameterSymbol(methodSymbol.PartialDefinitionPart.TypeParameters, typeParameter));
                        }
                    }
                }
            }

            return null;
        }

        private TypeParameterSymbol GetTypeParameterSymbol(ImmutableArray<TypeParameterSymbol> parameters, TypeParameterSyntax parameter)
        {
            foreach (var symbol in parameters)
            {
                foreach (var location in symbol.Locations)
                {
                    if (location.SourceTree == this.SyntaxTree && parameter.Span.Contains(location.SourceSpan))
                    {
                        return symbol;
                    }
                }
            }

            return null;
        }

        public override ControlFlowAnalysis AnalyzeControlFlow(StatementSyntax firstStatement, StatementSyntax lastStatement)
        {
            ValidateStatementRange(firstStatement, lastStatement);
            var context = RegionAnalysisContext(firstStatement, lastStatement);
            var result = new CSharpControlFlowAnalysis(context);
            return result;
        }

        private void ValidateStatementRange(StatementSyntax firstStatement, StatementSyntax lastStatement)
        {
            if (firstStatement == null)
            {
                throw new ArgumentNullException(nameof(firstStatement));
            }

            if (lastStatement == null)
            {
                throw new ArgumentNullException(nameof(lastStatement));
            }

            if (!IsInTree(firstStatement))
            {
                throw new ArgumentException("statements not within tree");
            }

            if (firstStatement.Parent == null || firstStatement.Parent != lastStatement.Parent)
            {
                throw new ArgumentException("statements not within the same statement list");
            }

            if (firstStatement.SpanStart > lastStatement.SpanStart)
            {
                throw new ArgumentException("first statement does not precede last statement");
            }
        }

        public override DataFlowAnalysis AnalyzeDataFlow(ExpressionSyntax expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            if (!IsInTree(expression))
            {
                throw new ArgumentException("expression not within tree");
            }

            var context = RegionAnalysisContext(expression);
            var result = new CSharpDataFlowAnalysis(context);
            return result;
        }

        public override DataFlowAnalysis AnalyzeDataFlow(StatementSyntax firstStatement, StatementSyntax lastStatement)
        {
            ValidateStatementRange(firstStatement, lastStatement);
            var context = RegionAnalysisContext(firstStatement, lastStatement);
            var result = new CSharpDataFlowAnalysis(context);
            return result;
        }

        private static BoundNode GetBoundRoot(MemberSemanticModel memberModel, out Symbol member)
        {
            member = memberModel.MemberSymbol;
            return memberModel.GetBoundRoot();
        }

        private NamespaceOrTypeSymbol GetDeclaredTypeMemberContainer(CSharpSyntaxNode memberDeclaration)
        {
            if (memberDeclaration.Parent.Kind() == SyntaxKind.CompilationUnit)
            {
                // top-level namespace:
                if (memberDeclaration.Kind() == SyntaxKind.NamespaceDeclaration)
                {
                    return _compilation.Assembly.GlobalNamespace;
                }

                // top-level members in script or interactive code:
                if (this.SyntaxTree.Options.Kind != SourceCodeKind.Regular)
                {
                    return this.Compilation.ScriptClass;
                }

                // top-level type in an explicitly declared namespace:
                if (SyntaxFacts.IsTypeDeclaration(memberDeclaration.Kind()))
                {
                    return _compilation.Assembly.GlobalNamespace;
                }

                // other top-level members:
                return _compilation.Assembly.GlobalNamespace.ImplicitType;
            }

            var container = GetDeclaredNamespaceOrType(memberDeclaration.Parent);
            Debug.Assert((object)container != null);

            // member in a type:
            if (!container.IsNamespace)
            {
                return container;
            }

            // a namespace or a type in an explicitly declared namespace:
            if (memberDeclaration.Kind() == SyntaxKind.NamespaceDeclaration || SyntaxFacts.IsTypeDeclaration(memberDeclaration.Kind()))
            {
                return container;
            }

            // another member in a namespace:
            return ((NamespaceSymbol)container).ImplicitType;
        }

        private Symbol GetDeclaredMemberSymbol(CSharpSyntaxNode declarationSyntax)
        {
            CheckSyntaxNode(declarationSyntax);

            var container = GetDeclaredTypeMemberContainer(declarationSyntax);
            var name = GetDeclarationName(declarationSyntax);
            return this.GetDeclaredMember(container, declarationSyntax.Span, name);
        }

        public override AwaitExpressionInfo GetAwaitExpressionInfo(AwaitExpressionSyntax node)
        {
            MemberSemanticModel memberModel = GetMemberModel(node);
            return memberModel == null ? default(AwaitExpressionInfo) : memberModel.GetAwaitExpressionInfo(node);
        }

        public override ForEachStatementInfo GetForEachStatementInfo(ForEachStatementSyntax node)
        {
            MemberSemanticModel memberModel = GetMemberModel(node);
            return memberModel == null ? default(ForEachStatementInfo) : memberModel.GetForEachStatementInfo(node);
        }
    }
}
