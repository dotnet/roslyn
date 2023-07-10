// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Allows asking semantic questions about any node in a SyntaxTree within a Compilation.
    /// </summary>
    internal partial class SyntaxTreeSemanticModel : PublicSemanticModel
    {
        private readonly CSharpCompilation _compilation;
        private readonly SyntaxTree _syntaxTree;

        /// <summary>
        /// Note, the name of this field could be somewhat confusing because it is also 
        /// used to store models for attributes and default parameter values, which are
        /// not members.
        /// </summary>
        private ImmutableDictionary<CSharpSyntaxNode, MemberSemanticModel> _memberModels = ImmutableDictionary<CSharpSyntaxNode, MemberSemanticModel>.Empty;

        private readonly BinderFactory _binderFactory;
        private Func<CSharpSyntaxNode, MemberSemanticModel> _createMemberModelFunction;
        private readonly bool _ignoresAccessibility;
        private ScriptLocalScopeBinder.Labels _globalStatementLabels;

        private static readonly Func<CSharpSyntaxNode, bool> s_isMemberDeclarationFunction = IsMemberDeclaration;

#nullable enable
        internal SyntaxTreeSemanticModel(CSharpCompilation compilation, SyntaxTree syntaxTree, bool ignoreAccessibility = false)
        {
            _compilation = compilation;
            _syntaxTree = syntaxTree;
            _ignoresAccessibility = ignoreAccessibility;

            _binderFactory = compilation.GetBinderFactory(SyntaxTree, ignoreAccessibility);
        }

        internal SyntaxTreeSemanticModel(CSharpCompilation parentCompilation, SyntaxTree parentSyntaxTree, SyntaxTree speculatedSyntaxTree, bool ignoreAccessibility)
        {
            _compilation = parentCompilation;
            _syntaxTree = speculatedSyntaxTree;
            _binderFactory = _compilation.GetBinderFactory(parentSyntaxTree, ignoreAccessibility);
            _ignoresAccessibility = ignoreAccessibility;
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
#nullable disable

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

        internal override IOperation GetOperationWorker(CSharpSyntaxNode node, CancellationToken cancellationToken)
        {
            MemberSemanticModel model;

            switch (node)
            {
                case ConstructorDeclarationSyntax constructor:
                    model = (constructor.HasAnyBody() || constructor.Initializer != null) ? GetOrAddModel(node) : null;
                    break;
                case BaseMethodDeclarationSyntax method:
                    model = method.HasAnyBody() ? GetOrAddModel(node) : null;
                    break;
                case AccessorDeclarationSyntax accessor:
                    model = (accessor.Body != null || accessor.ExpressionBody != null) ? GetOrAddModel(node) : null;
                    break;
                case TypeDeclarationSyntax { ParameterList: { }, PrimaryConstructorBaseTypeIfClass: { } } typeDeclaration when TryGetSynthesizedPrimaryConstructor(typeDeclaration) is SynthesizedPrimaryConstructor:
                    model = GetOrAddModel(typeDeclaration);
                    break;
                default:
                    model = this.GetMemberModel(node);
                    break;
            }

            if (model != null)
            {
                return model.GetOperationWorker(node, cancellationToken);
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

                        BoundExpression bound = binder.BindExpression((ExpressionSyntax)node, BindingDiagnosticBag.Discarded);

                        SymbolInfo info = GetSymbolInfoForNode(options, bound, bound, boundNodeForSyntacticParent: null, binderOpt: null);
                        if ((object)info.Symbol != null)
                        {
                            result = new SymbolInfo(ImmutableArray.Create<ISymbol>(info.Symbol), CandidateReason.NotATypeOrNamespace);
                        }
                        else if (!info.CandidateSymbols.IsEmpty)
                        {
                            result = new SymbolInfo(info.CandidateSymbols, CandidateReason.NotATypeOrNamespace);
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
                    var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                    var symbols = binder.BindXmlNameAttribute(attrSyntax, ref discardedUseSiteInfo);

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

                    if (SyntaxFacts.IsNamespaceAliasQualifier(type))
                    {
                        return binder.BindNamespaceAliasSymbol(node as IdentifierNameSyntax, BindingDiagnosticBag.Discarded);
                    }
                    else if (SyntaxFacts.IsInTypeOnlyContext(type))
                    {
                        if (!type.IsVar)
                        {
                            return binder.BindTypeOrAlias(type, BindingDiagnosticBag.Discarded, basesBeingResolved).Symbol;
                        }

                        Symbol result = bindVarAsAliasFirst
                            ? binder.BindTypeOrAlias(type, BindingDiagnosticBag.Discarded, basesBeingResolved).Symbol
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

                            var variableDecl = type.ModifyingScopedOrRefTypeOrSelf().Parent as VariableDeclarationSyntax;
                            if (variableDecl != null && variableDecl.Variables.Any())
                            {
                                var fieldSymbol = GetDeclaredFieldSymbol(variableDecl.Variables.First());
                                if ((object)fieldSymbol != null)
                                {
                                    result = fieldSymbol.Type;
                                }
                            }
                        }

                        return result ?? binder.BindTypeOrAlias(type, BindingDiagnosticBag.Discarded, basesBeingResolved).Symbol;
                    }
                    else
                    {
                        return binder.BindNamespaceOrTypeOrAliasSymbol(type, BindingDiagnosticBag.Discarded, basesBeingResolved, basesBeingResolved != null).Symbol;
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

        internal override ImmutableArray<IPropertySymbol> GetIndexerGroupWorker(CSharpSyntaxNode node, SymbolInfoOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            // in case this is right side of a qualified name or member access (or part of a cref)
            node = SyntaxFactory.GetStandaloneNode(node);

            var model = this.GetMemberModel(node);
            return model == null ? ImmutableArray<IPropertySymbol>.Empty : model.GetIndexerGroupWorker(node, options, cancellationToken);
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
            return (model == null) ? SymbolInfo.None : model.GetSymbolInfo(node, cancellationToken);
        }

        public override TypeInfo GetTypeInfo(SelectOrGroupClauseSyntax node, CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckSyntaxNode(node);
            var model = this.GetMemberModel(node);
            return (model == null) ? CSharpTypeInfo.None : model.GetTypeInfo(node, cancellationToken);
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

        public override INamedTypeSymbol GetDeclaredSymbol(TupleExpressionSyntax declaratorSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckSyntaxNode(declaratorSyntax);
            var model = this.GetMemberModel(declaratorSyntax);
            return (model == null) ? null : model.GetDeclaredSymbol(declaratorSyntax, cancellationToken);
        }

        public override ISymbol GetDeclaredSymbol(ArgumentSyntax declaratorSyntax, CancellationToken cancellationToken = default(CancellationToken))
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
            return (model == null) ? SymbolInfo.None : model.GetSymbolInfo(node, cancellationToken);
        }

        private ConsList<TypeSymbol> GetBasesBeingResolved(TypeSyntax expression)
        {
            // if the expression is the child of a base-list node, then the expression should be
            // bound in the context of the containing symbols base being resolved.
            for (; expression != null && expression.Parent != null; expression = expression.Parent as TypeSyntax)
            {
                var parent = expression.Parent;
                if (parent is BaseTypeSyntax baseType && parent.Parent != null && parent.Parent.Kind() == SyntaxKind.BaseList && baseType.Type == expression)
                {
                    // we have a winner
                    var decl = (BaseTypeDeclarationSyntax)parent.Parent.Parent;
                    var symbol = this.GetDeclaredSymbol(decl);
                    return ConsList<TypeSymbol>.Empty.Prepend(symbol.GetSymbol().OriginalDefinition);
                }
            }

            return null;
        }

        public override Conversion ClassifyConversion(ExpressionSyntax expression, ITypeSymbol destination, bool isExplicitInSource = false)
        {
            TypeSymbol csdestination = destination.EnsureCSharpSymbolOrNull(nameof(destination));

            if (expression.Kind() == SyntaxKind.DeclarationExpression)
            {
                // Conversion from a declaration is unspecified.
                return Conversion.NoConversion;
            }

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

        internal sealed override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, TypeSyntax type, SpeculativeBindingOption bindingOption, out PublicSemanticModel speculativeModel)
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

        internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, CrefSyntax crefSyntax, out PublicSemanticModel speculativeModel)
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

        internal sealed override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, StatementSyntax statement, out PublicSemanticModel speculativeModel)
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

        internal sealed override bool TryGetSpeculativeSemanticModelForMethodBodyCore(SyntaxTreeSemanticModel parentModel, int position, BaseMethodDeclarationSyntax method, out PublicSemanticModel speculativeModel)
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

        internal sealed override bool TryGetSpeculativeSemanticModelForMethodBodyCore(SyntaxTreeSemanticModel parentModel, int position, AccessorDeclarationSyntax accessor, out PublicSemanticModel speculativeModel)
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

        internal sealed override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, EqualsValueClauseSyntax initializer, out PublicSemanticModel speculativeModel)
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

        internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, ArrowExpressionClauseSyntax expressionBody, out PublicSemanticModel speculativeModel)
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

        internal sealed override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, ConstructorInitializerSyntax constructorInitializer, out PublicSemanticModel speculativeModel)
        {
            position = CheckAndAdjustPosition(position);

            var existingConstructorInitializer = this.Root.FindToken(position).Parent.AncestorsAndSelf().OfType<ConstructorInitializerSyntax>().FirstOrDefault();

            if (existingConstructorInitializer != null)
            {
                var model = this.GetMemberModel(position);
                if (model != null)
                {
                    return model.TryGetSpeculativeSemanticModelCore(parentModel, position, constructorInitializer, out speculativeModel);
                }
            }

            speculativeModel = null;
            return false;
        }

        internal sealed override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, PrimaryConstructorBaseTypeSyntax constructorInitializer, out PublicSemanticModel speculativeModel)
        {
            position = CheckAndAdjustPosition(position);

            var existingConstructorInitializer = this.Root.FindToken(position).Parent.AncestorsAndSelf().OfType<PrimaryConstructorBaseTypeSyntax>().FirstOrDefault();

            if (existingConstructorInitializer != null)
            {
                var model = this.GetMemberModel(existingConstructorInitializer);
                if (model != null)
                {
                    return model.TryGetSpeculativeSemanticModelCore(parentModel, position, constructorInitializer, out speculativeModel);
                }
            }

            speculativeModel = null;
            return false;
        }

        internal override BoundExpression GetSpeculativelyBoundExpression(int position, ExpressionSyntax expression, SpeculativeBindingOption bindingOption, out Binder binder, out ImmutableArray<Symbol> crefSymbols)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            // If the given position is in a member that we can get a semantic model for, we want to defer to that implementation
            // of GetSpeculativelyBoundExpression so it can take nullability into account.
            if (bindingOption == SpeculativeBindingOption.BindAsExpression)
            {
                position = CheckAndAdjustPosition(position);
                var model = GetMemberModel(position);
                if (model is object)
                {
                    return model.GetSpeculativelyBoundExpression(position, expression, bindingOption, out binder, out crefSymbols);
                }
            }

            return GetSpeculativelyBoundExpressionWithoutNullability(position, expression, bindingOption, out binder, out crefSymbols);
        }

        internal PublicSemanticModel CreateSpeculativeAttributeSemanticModel(int position, AttributeSyntax attribute, Binder binder, AliasSymbol aliasOpt, NamedTypeSymbol attributeType)
        {
            var memberModel = IsNullableAnalysisEnabledAtSpeculativePosition(position, attribute) ? GetMemberModel(position) : null;
            return AttributeSemanticModel.CreateSpeculative(this, attribute, attributeType, aliasOpt, binder, memberModel?.GetRemappedSymbols(), position);
        }

        internal bool IsNullableAnalysisEnabledAtSpeculativePosition(int position, SyntaxNode speculativeSyntax)
        {
            Debug.Assert(speculativeSyntax.SyntaxTree != SyntaxTree);

            // https://github.com/dotnet/roslyn/issues/50234: CSharpSyntaxTree.IsNullableAnalysisEnabled() does not differentiate
            // between no '#nullable' directives and '#nullable restore' - it returns null in both cases. Since we fallback to the
            // directives in the original syntax tree, we're not handling '#nullable restore' correctly in the speculative text.
            return ((CSharpSyntaxTree)speculativeSyntax.SyntaxTree).IsNullableAnalysisEnabled(speculativeSyntax.Span) ??
                Compilation.IsNullableAnalysisEnabledIn((CSharpSyntaxTree)SyntaxTree, new TextSpan(position, 0));
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
                    case SyntaxKind.InitAccessorDeclaration:
                        // NOTE: not UnknownAccessorDeclaration since there's no corresponding method symbol from which to build a member model.
                        outsideMemberDecl = !LookupPosition.IsInBody(position, (AccessorDeclarationSyntax)memberDecl);
                        break;
                    case SyntaxKind.ConstructorDeclaration:
                        var constructorDecl = (ConstructorDeclarationSyntax)memberDecl;
                        outsideMemberDecl =
                            !LookupPosition.IsInConstructorParameterScope(position, constructorDecl) &&
                            !LookupPosition.IsInParameterList(position, constructorDecl);
                        break;
                    case SyntaxKind.ClassDeclaration:
                    case SyntaxKind.RecordDeclaration:
                        {
                            var typeDecl = (TypeDeclarationSyntax)memberDecl;

                            if (typeDecl.ParameterList is null)
                            {
                                outsideMemberDecl = true;
                            }
                            else
                            {
                                var argumentList = typeDecl.PrimaryConstructorBaseTypeIfClass?.ArgumentList;
                                outsideMemberDecl = argumentList is null || !LookupPosition.IsBetweenTokens(position, argumentList.OpenParenToken, argumentList.CloseParenToken);
                            }
                        }
                        break;
                    case SyntaxKind.ConversionOperatorDeclaration:
                    case SyntaxKind.DestructorDeclaration:
                    case SyntaxKind.MethodDeclaration:
                    case SyntaxKind.OperatorDeclaration:
                        var methodDecl = (BaseMethodDeclarationSyntax)memberDecl;
                        outsideMemberDecl =
                            !LookupPosition.IsInBody(position, methodDecl) &&
                            !LookupPosition.IsInParameterList(position, methodDecl);
                        break;
                }
            }

            return outsideMemberDecl ? null : GetMemberModel(node);
        }

        // Try to get a member semantic model that encloses "node". If there is not an enclosing
        // member semantic model, return null.
        internal override MemberSemanticModel GetMemberModel(SyntaxNode node)
        {
            // Documentation comments can never legally appear within members, so there's no point
            // in building out the MemberSemanticModel to handle them.  Instead, just say have
            // SyntaxTreeSemanticModel handle them, regardless of location.
            if (IsInDocumentationComment(node))
            {
                return null;
            }

            var memberDecl = GetMemberDeclaration(node) ?? (node as CompilationUnitSyntax);
            if (memberDecl != null)
            {
                var span = node.Span;

                switch (memberDecl.Kind())
                {
                    case SyntaxKind.MethodDeclaration:
                    case SyntaxKind.ConversionOperatorDeclaration:
                    case SyntaxKind.OperatorDeclaration:
                        {
                            var methodDecl = (BaseMethodDeclarationSyntax)memberDecl;
                            var expressionBody = methodDecl.GetExpressionBodySyntax();
                            return (expressionBody?.FullSpan.Contains(span) == true || methodDecl.Body?.FullSpan.Contains(span) == true) ?
                                   GetOrAddModel(methodDecl) : null;
                        }

                    case SyntaxKind.ConstructorDeclaration:
                        {
                            ConstructorDeclarationSyntax constructorDecl = (ConstructorDeclarationSyntax)memberDecl;
                            var expressionBody = constructorDecl.GetExpressionBodySyntax();
                            return (constructorDecl.Initializer?.FullSpan.Contains(span) == true ||
                                    expressionBody?.FullSpan.Contains(span) == true ||
                                    constructorDecl.Body?.FullSpan.Contains(span) == true) ?
                                   GetOrAddModel(constructorDecl) : null;
                        }

                    case SyntaxKind.ClassDeclaration:
                    case SyntaxKind.RecordDeclaration:
                        {
                            var typeDecl = (TypeDeclarationSyntax)memberDecl;
                            return typeDecl.ParameterList is object &&
                                   typeDecl.PrimaryConstructorBaseTypeIfClass is PrimaryConstructorBaseTypeSyntax baseWithArguments &&
                                   (node == baseWithArguments || baseWithArguments.ArgumentList.FullSpan.Contains(span)) ? GetOrAddModel(memberDecl) : null;
                        }

                    case SyntaxKind.DestructorDeclaration:
                        {
                            DestructorDeclarationSyntax destructorDecl = (DestructorDeclarationSyntax)memberDecl;
                            var expressionBody = destructorDecl.GetExpressionBodySyntax();
                            return (expressionBody?.FullSpan.Contains(span) == true || destructorDecl.Body?.FullSpan.Contains(span) == true) ?
                                   GetOrAddModel(destructorDecl) : null;
                        }

                    case SyntaxKind.GetAccessorDeclaration:
                    case SyntaxKind.SetAccessorDeclaration:
                    case SyntaxKind.InitAccessorDeclaration:
                    case SyntaxKind.AddAccessorDeclaration:
                    case SyntaxKind.RemoveAccessorDeclaration:
                        // NOTE: not UnknownAccessorDeclaration since there's no corresponding method symbol from which to build a member model.
                        {
                            var accessorDecl = (AccessorDeclarationSyntax)memberDecl;
                            return (accessorDecl.ExpressionBody?.FullSpan.Contains(span) == true || accessorDecl.Body?.FullSpan.Contains(span) == true) ?
                                   GetOrAddModel(accessorDecl) : null;
                        }

                    case SyntaxKind.IndexerDeclaration:
                        {
                            var indexerDecl = (IndexerDeclarationSyntax)memberDecl;
                            return GetOrAddModelIfContains(indexerDecl.ExpressionBody, span);
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

                    case SyntaxKind.GlobalStatement:
                        if (SyntaxFacts.IsSimpleProgramTopLevelStatement((GlobalStatementSyntax)memberDecl))
                        {
                            return GetOrAddModel((CompilationUnitSyntax)memberDecl.Parent);
                        }

                        return GetOrAddModel(memberDecl);

                    case SyntaxKind.CompilationUnit:
                        if (SynthesizedSimpleProgramEntryPointSymbol.GetSimpleProgramEntryPoint(Compilation, (CompilationUnitSyntax)memberDecl, fallbackToMainEntryPoint: false) is object)
                        {
                            return GetOrAddModel(memberDecl);
                        }
                        break;

                    case SyntaxKind.Attribute:
                        return GetOrAddModelForAttribute((AttributeSyntax)memberDecl);

                    case SyntaxKind.Parameter:
                        if (node != memberDecl)
                        {
                            return GetOrAddModelForParameter((ParameterSyntax)memberDecl, span);
                        }
                        else
                        {
                            return GetMemberModel(memberDecl.Parent);
                        }
                }
            }

            return null;
        }

        /// <summary>
        /// Internal for test purposes only
        /// </summary>
        internal ImmutableDictionary<CSharpSyntaxNode, MemberSemanticModel> TestOnlyMemberModels => _memberModels;

        private MemberSemanticModel GetOrAddModelForAttribute(AttributeSyntax attribute)
        {
            MemberSemanticModel containing = attribute.Parent != null ? GetMemberModel(attribute.Parent) : null;

            if (containing == null)
            {
                return GetOrAddModel(attribute);
            }

            return ImmutableInterlocked.GetOrAdd(ref _memberModels, attribute,
                                                 (node, binderAndModel) => CreateModelForAttribute(binderAndModel.binder, (AttributeSyntax)node, binderAndModel.model),
                                                 (binder: containing.GetEnclosingBinder(attribute.SpanStart), model: containing));
        }

        private static bool IsInDocumentationComment(SyntaxNode node)
        {
            for (SyntaxNode curr = node; curr != null; curr = curr.Parent)
            {
                if (SyntaxFacts.IsDocumentationCommentTrivia(curr.Kind()))
                {
                    return true;
                }
            }

            return false;
        }

        // Check parameter for a default value containing span, and create an InitializerSemanticModel for binding the default value if so.
        // Otherwise, return model for enclosing context.
        private MemberSemanticModel GetOrAddModelForParameter(ParameterSyntax paramDecl, TextSpan span)
        {
            EqualsValueClauseSyntax defaultValueSyntax = paramDecl.Default;
            MemberSemanticModel containing = paramDecl.Parent != null ? GetMemberModel(paramDecl.Parent) : null;

            if (containing == null)
            {
                return GetOrAddModelIfContains(defaultValueSyntax, span);
            }

            if (defaultValueSyntax != null && defaultValueSyntax.FullSpan.Contains(span))
            {
                var parameterSymbol = containing.GetDeclaredSymbol(paramDecl).GetSymbol<ParameterSymbol>();
                if ((object)parameterSymbol != null)
                {
                    return ImmutableInterlocked.GetOrAdd(ref _memberModels, defaultValueSyntax,
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

        private static CSharpSyntaxNode GetMemberDeclaration(SyntaxNode node)
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

            return GetOrAddModel(node, createMemberModelFunction);
        }

        internal MemberSemanticModel GetOrAddModel(CSharpSyntaxNode node, Func<CSharpSyntaxNode, MemberSemanticModel> createMemberModelFunction)
        {
            return ImmutableInterlocked.GetOrAdd(ref _memberModels, node, createMemberModelFunction);
        }

        // Create a member model for the given declaration syntax. In certain very malformed
        // syntax trees, there may not be a symbol that can have a member model associated with it
        // (although we try to minimize such cases). In such cases, null is returned.
        private MemberSemanticModel CreateMemberModel(CSharpSyntaxNode node)
        {
            Binder defaultOuter() => _binderFactory.GetBinder(node).WithAdditionalFlags(this.IgnoresAccessibility ? BinderFlags.IgnoreAccessibility : BinderFlags.None);

            switch (node.Kind())
            {
                case SyntaxKind.CompilationUnit:
                    return createMethodBodySemanticModel(node, SynthesizedSimpleProgramEntryPointSymbol.GetSimpleProgramEntryPoint(Compilation, (CompilationUnitSyntax)node, fallbackToMainEntryPoint: false));

                case SyntaxKind.MethodDeclaration:
                case SyntaxKind.ConversionOperatorDeclaration:
                case SyntaxKind.OperatorDeclaration:
                case SyntaxKind.ConstructorDeclaration:
                case SyntaxKind.DestructorDeclaration:
                    {
                        var memberDecl = (MemberDeclarationSyntax)node;
                        var symbol = GetDeclaredSymbol(memberDecl).GetSymbol<SourceMemberMethodSymbol>();
                        return createMethodBodySemanticModel(memberDecl, symbol);
                    }

                case SyntaxKind.ClassDeclaration:
                case SyntaxKind.RecordDeclaration:
                    {
                        SynthesizedPrimaryConstructor symbol = TryGetSynthesizedPrimaryConstructor((TypeDeclarationSyntax)node);

                        if (symbol is null)
                        {
                            return null;
                        }

                        return createMethodBodySemanticModel(node, symbol);
                    }

                case SyntaxKind.GetAccessorDeclaration:
                case SyntaxKind.SetAccessorDeclaration:
                case SyntaxKind.InitAccessorDeclaration:
                case SyntaxKind.AddAccessorDeclaration:
                case SyntaxKind.RemoveAccessorDeclaration:
                    {
                        var accessorDecl = (AccessorDeclarationSyntax)node;
                        var symbol = GetDeclaredSymbol(accessorDecl).GetSymbol<SourceMemberMethodSymbol>();
                        return createMethodBodySemanticModel(accessorDecl, symbol);
                    }

                case SyntaxKind.Block:
                    // Don't throw, just use for the assert
                    ExceptionUtilities.UnexpectedValue(node.Parent);
                    break;

                case SyntaxKind.EqualsValueClause:
                    switch (node.Parent.Kind())
                    {
                        case SyntaxKind.VariableDeclarator:
                            {
                                var variableDecl = (VariableDeclaratorSyntax)node.Parent;
                                FieldSymbol fieldSymbol = GetDeclaredFieldSymbol(variableDecl);

                                return InitializerSemanticModel.Create(
                                    this,
                                    variableDecl,   //pass in the entire field initializer to permit region analysis. 
                                    fieldSymbol,
                                    //if we're in regular C#, then insert an extra binder to perform field initialization checks
                                    GetFieldOrPropertyInitializerBinder(fieldSymbol, defaultOuter(), variableDecl.Initializer));
                            }

                        case SyntaxKind.PropertyDeclaration:
                            {
                                var propertyDecl = (PropertyDeclarationSyntax)node.Parent;
                                var propertySymbol = GetDeclaredSymbol(propertyDecl).GetSymbol<SourcePropertySymbol>();
                                return InitializerSemanticModel.Create(
                                    this,
                                    propertyDecl,
                                    propertySymbol,
                                    GetFieldOrPropertyInitializerBinder(propertySymbol.BackingField, defaultOuter(), propertyDecl.Initializer));
                            }

                        case SyntaxKind.Parameter:
                            {
                                // NOTE: we don't need to create a member model for lambda parameter default value
                                // because lambdas only appear in code with associated member models.
                                ParameterSyntax parameterDecl = (ParameterSyntax)node.Parent;
                                ParameterSymbol parameterSymbol = GetDeclaredNonLambdaParameterSymbol(parameterDecl);
                                if ((object)parameterSymbol == null)
                                    return null;

                                return InitializerSemanticModel.Create(
                                    this,
                                    parameterDecl,
                                    parameterSymbol,
                                    defaultOuter().CreateBinderForParameterDefaultValue(parameterSymbol, (EqualsValueClauseSyntax)node),
                                    parentRemappedSymbolsOpt: null);
                            }

                        case SyntaxKind.EnumMemberDeclaration:
                            {
                                var enumDecl = (EnumMemberDeclarationSyntax)node.Parent;
                                var enumSymbol = GetDeclaredSymbol(enumDecl).GetSymbol<FieldSymbol>();
                                if ((object)enumSymbol == null)
                                    return null;

                                return InitializerSemanticModel.Create(
                                    this,
                                    enumDecl,
                                    enumSymbol,
                                    GetFieldOrPropertyInitializerBinder(enumSymbol, defaultOuter(), enumDecl.EqualsValue));
                            }
                        default:
                            throw ExceptionUtilities.UnexpectedValue(node.Parent.Kind());
                    }

                case SyntaxKind.ArrowExpressionClause:
                    {
                        SourceMemberMethodSymbol symbol = null;

                        var exprDecl = (ArrowExpressionClauseSyntax)node;

                        if (node.Parent is BasePropertyDeclarationSyntax)
                        {
                            symbol = GetDeclaredSymbol(exprDecl).GetSymbol<SourceMemberMethodSymbol>();
                        }
                        else
                        {
                            // Don't throw, just use for the assert
                            ExceptionUtilities.UnexpectedValue(node.Parent);
                        }

                        ExecutableCodeBinder binder = symbol?.TryGetBodyBinder(_binderFactory, this.IgnoresAccessibility);

                        if (binder == null)
                        {
                            return null;
                        }

                        return MethodBodySemanticModel.Create(this, symbol, new MethodBodySemanticModel.InitialState(exprDecl, binder: binder));
                    }

                case SyntaxKind.GlobalStatement:
                    {
                        var parent = node.Parent;
                        // TODO (tomat): handle misplaced global statements
                        if (parent.Kind() == SyntaxKind.CompilationUnit &&
                            !this.IsRegularCSharp &&
                            (object)_compilation.ScriptClass != null)
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
                                this,
                                scriptInitializer,
                                new MethodBodySemanticModel.InitialState(node, binder: new ExecutableCodeBinder(node, scriptInitializer, new ScriptLocalScopeBinder(_globalStatementLabels, defaultOuter()))));
                        }
                    }
                    break;

                case SyntaxKind.Attribute:
                    return CreateModelForAttribute(defaultOuter(), (AttributeSyntax)node, containingModel: null);
            }

            return null;

            MemberSemanticModel createMethodBodySemanticModel(CSharpSyntaxNode memberDecl, SourceMemberMethodSymbol symbol)
            {
                ExecutableCodeBinder binder = symbol?.TryGetBodyBinder(_binderFactory, this.IgnoresAccessibility);

                if (binder == null)
                {
                    return null;
                }

                return MethodBodySemanticModel.Create(this, symbol, new MethodBodySemanticModel.InitialState(memberDecl, binder: binder));
            }
        }

        private SynthesizedPrimaryConstructor TryGetSynthesizedPrimaryConstructor(TypeDeclarationSyntax node)
            => TryGetSynthesizedPrimaryConstructor(node, GetDeclaredType(node));

        private FieldSymbol GetDeclaredFieldSymbol(VariableDeclaratorSyntax variableDecl)
        {
            var declaredSymbol = GetDeclaredSymbol(variableDecl);

            if ((object)declaredSymbol != null)
            {
                switch (variableDecl.Parent.Parent.Kind())
                {
                    case SyntaxKind.FieldDeclaration:
                        return declaredSymbol.GetSymbol<FieldSymbol>();

                    case SyntaxKind.EventFieldDeclaration:
                        return (declaredSymbol.GetSymbol<EventSymbol>()).AssociatedField;
                }
            }

            return null;
        }

        private Binder GetFieldOrPropertyInitializerBinder(FieldSymbol symbol, Binder outer, EqualsValueClauseSyntax initializer)
        {
            // NOTE: checking for a containing script class is sufficient, but the regular C# test is quick and easy.
            outer = outer.GetFieldInitializerBinder(symbol, suppressBinderFlagsFieldInitializer: !this.IsRegularCSharp && symbol.ContainingType.IsScriptClass);

            if (initializer != null)
            {
                outer = new ExecutableCodeBinder(initializer, symbol, outer);
            }

            return outer;
        }

        private static bool IsMemberDeclaration(CSharpSyntaxNode node)
        {
            return (node is MemberDeclarationSyntax) || (node is AccessorDeclarationSyntax) ||
                   (node.Kind() == SyntaxKind.Attribute) || (node.Kind() == SyntaxKind.Parameter);
        }

        private bool IsRegularCSharp
        {
            get
            {
                return this.SyntaxTree.Options.Kind == SourceCodeKind.Regular;
            }
        }

        #region "GetDeclaredSymbol overloads for MemberDeclarationSyntax and its subtypes"

        /// <inheritdoc/>
        public override INamespaceSymbol GetDeclaredSymbol(NamespaceDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckSyntaxNode(declarationSyntax);

            return GetDeclaredNamespace(declarationSyntax).GetPublicSymbol();
        }

        /// <inheritdoc/>
        public override INamespaceSymbol GetDeclaredSymbol(FileScopedNamespaceDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default)
        {
            CheckSyntaxNode(declarationSyntax);

            return GetDeclaredNamespace(declarationSyntax).GetPublicSymbol();
        }

        private NamespaceSymbol GetDeclaredNamespace(BaseNamespaceDeclarationSyntax declarationSyntax)
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
            var symbol = GetDeclaredNamespace(container, declarationSyntax.Span, declarationSyntax.Name);
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

            return GetDeclaredType(declarationSyntax).GetPublicSymbol();
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

            return GetDeclaredType(declarationSyntax).GetPublicSymbol();
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
            return GetDeclaredMember(container, declarationSyntax.Span, isKnownToBeANamespace: false, name) as NamedTypeSymbol;
        }

        private NamespaceOrTypeSymbol GetDeclaredNamespaceOrType(CSharpSyntaxNode declarationSyntax)
        {
            var namespaceDeclarationSyntax = declarationSyntax as BaseNamespaceDeclarationSyntax;
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
        /// Given a member declaration syntax, get the corresponding symbol.
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
                    return (GetDeclaredNamespaceOrType(declarationSyntax) ?? GetDeclaredMemberSymbol(declarationSyntax)).GetPublicSymbol();
            }
        }

        public override IMethodSymbol GetDeclaredSymbol(CompilationUnitSyntax declarationSyntax, CancellationToken cancellationToken = default)
        {
            CheckSyntaxNode(declarationSyntax);

            return SynthesizedSimpleProgramEntryPointSymbol.GetSimpleProgramEntryPoint(Compilation, declarationSyntax, fallbackToMainEntryPoint: false).GetPublicSymbol();
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
            return ((FieldSymbol)GetDeclaredMemberSymbol(declarationSyntax)).GetPublicSymbol();
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
            return ((MethodSymbol)GetDeclaredMemberSymbol(declarationSyntax)).GetPublicSymbol();
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
            return GetDeclaredMemberSymbol(declarationSyntax).GetPublicSymbol();
        }

        /// <summary>
        /// Given a syntax node that declares a property, get the corresponding declared symbol.
        /// </summary>
        /// <param name="declarationSyntax">The syntax node that declares a property, indexer or an event.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The symbol that was declared.</returns>
        public override IPropertySymbol GetDeclaredSymbol(PropertyDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((PropertySymbol)GetDeclaredMemberSymbol(declarationSyntax)).GetPublicSymbol();
        }

        /// <summary>
        /// Given a syntax node that declares an indexer, get the corresponding declared symbol.
        /// </summary>
        /// <param name="declarationSyntax">The syntax node that declares an indexer.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The symbol that was declared.</returns>
        public override IPropertySymbol GetDeclaredSymbol(IndexerDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((PropertySymbol)GetDeclaredMemberSymbol(declarationSyntax)).GetPublicSymbol();
        }

        /// <summary>
        /// Given a syntax node that declares a (custom) event, get the corresponding event symbol.
        /// </summary>
        /// <param name="declarationSyntax">The syntax node that declares a event.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The symbol that was declared.</returns>
        public override IEventSymbol GetDeclaredSymbol(EventDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((EventSymbol)GetDeclaredMemberSymbol(declarationSyntax)).GetPublicSymbol();
        }

        #endregion

        #endregion

        /// <summary>
        /// Given a syntax node that declares a property or member accessor, get the corresponding symbol.
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
                    return (this.GetDeclaredMember(container, declarationSyntax.Span, isKnownToBeANamespace: false) as MethodSymbol).GetPublicSymbol();

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
                    return (this.GetDeclaredMember(container, declarationSyntax.Span, isKnownToBeANamespace: false) as MethodSymbol).GetPublicSymbol();

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
                case SyntaxKind.RecordDeclaration:
                case SyntaxKind.RecordStructDeclaration:
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
                    {
                        var operatorDecl = (OperatorDeclarationSyntax)declaration;
                        return GetDeclarationName(declaration, operatorDecl.ExplicitInterfaceSpecifier, OperatorFacts.OperatorNameFromDeclaration(operatorDecl));
                    }

                case SyntaxKind.ConversionOperatorDeclaration:
                    {
                        var operatorDecl = (ConversionOperatorDeclarationSyntax)declaration;
                        return GetDeclarationName(declaration, operatorDecl.ExplicitInterfaceSpecifier, OperatorFacts.OperatorNameFromDeclaration(operatorDecl));
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

        private NamespaceSymbol GetDeclaredNamespace(NamespaceOrTypeSymbol container, TextSpan declarationSpan, NameSyntax name)
        {
            switch (name.Kind())
            {
                case SyntaxKind.GenericName:
                case SyntaxKind.IdentifierName:
                    return (NamespaceSymbol)GetDeclaredMember(container, declarationSpan, isKnownToBeANamespace: true, ((SimpleNameSyntax)name).Identifier.ValueText);

                case SyntaxKind.QualifiedName:
                    var qn = (QualifiedNameSyntax)name;
                    var left = GetDeclaredNamespace(container, declarationSpan, qn.Left) as NamespaceOrTypeSymbol;
                    Debug.Assert((object)left != null);
                    return GetDeclaredNamespace(left, declarationSpan, qn.Right);

                case SyntaxKind.AliasQualifiedName:
                    // this is not supposed to happen, but we allow for errors don't we!
                    var an = (AliasQualifiedNameSyntax)name;
                    return GetDeclaredNamespace(container, declarationSpan, an.Name);

                default:
                    throw ExceptionUtilities.UnexpectedValue(name.Kind());
            }
        }

        /// <summary>
        /// Finds the member in the containing symbol which is inside the given declaration span.
        /// </summary>
        /// <param name="isKnownToBeANamespace"><see langword="true"/> if the result is known to be a
        /// <see cref="NamespaceSymbol"/> (e.g. when the caller is <see cref="GetDeclaredNamespace(BaseNamespaceDeclarationSyntax)"/>;
        /// otherwise, <see langword="false"/> if the symbol kind is either unknown or known to not be a
        /// <see cref="NamespaceSymbol"/>.</param>
        private Symbol GetDeclaredMember(NamespaceOrTypeSymbol container, TextSpan declarationSpan, bool isKnownToBeANamespace, string name = null)
        {
            if ((object)container == null)
            {
                return null;
            }

            // look for any member with same declaration location
            var collection = name != null ? container.GetMembers(name) : container.GetMembersUnordered();
            if (isKnownToBeANamespace)
            {
                // Filter the collection to only include namespace symbols. This will not allocate a new instance for
                // the common case where all symbols in the collection are already namespace symbols.
                var namespaces = collection.WhereAsArray(symbol => symbol is NamespaceSymbol);

                Debug.Assert(name is not null, "Should only be looking for a known namespace by name.");
                Debug.Assert(namespaces is [NamespaceSymbol], "Namespace declarations of the same name are expected to appear as a single merged symbol.");

                if (name != null && namespaces is [NamespaceSymbol knownNamespace])
                {
                    Debug.Assert(knownNamespace.HasLocationContainedWithin(SyntaxTree, declarationSpan, out _), "Namespace symbols should include all syntax declaration locations.");

                    // Avoid O(n²) lookup for merged namespaces with a large number of parts
                    // https://github.com/dotnet/roslyn/issues/49769
                    return knownNamespace;
                }
            }

            Symbol zeroWidthMatch = null;
            foreach (var symbol in collection)
            {
                var namedType = symbol as ImplicitNamedTypeSymbol;
                if ((object)namedType != null && namedType.IsImplicitClass)
                {
                    // look inside wrapper around illegally placed members in namespaces
                    var result = GetDeclaredMember(namedType, declarationSpan, isKnownToBeANamespace, name);
                    if ((object)result != null)
                    {
                        return result;
                    }
                }

                if (symbol.HasLocationContainedWithin(this.SyntaxTree, declarationSpan, out var wasZeroWidthMatch))
                {
                    if (!wasZeroWidthMatch)
                        return symbol;

                    // exclude decls created via syntax recovery
                    zeroWidthMatch = symbol;
                }

                // Handle the case of the implementation of a partial method.
                var partial = symbol.Kind == SymbolKind.Method
                    ? ((MethodSymbol)symbol).PartialImplementationPart
                    : null;
                if ((object)partial != null)
                {
                    var loc = partial.GetFirstLocation();
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
                (name != null ? GetDeclaredMember(container, declarationSpan, isKnownToBeANamespace, name: null) : null);
        }

        /// <summary>
        /// Given a variable declarator syntax, get the corresponding symbol.
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

                var result = this.GetDeclaredMember(container, declarationSyntax.Span, isKnownToBeANamespace: false, declarationSyntax.Identifier.ValueText);
                Debug.Assert((object)result != null);

                return result.GetPublicSymbol();
            }

            // Might be a local variable.
            var memberModel = this.GetMemberModel(declarationSyntax);
            return memberModel?.GetDeclaredSymbol(declarationSyntax, cancellationToken);
        }

        public override ISymbol GetDeclaredSymbol(SingleVariableDesignationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Might be a local variable.
            var memberModel = this.GetMemberModel(declarationSyntax);
            ISymbol local = memberModel?.GetDeclaredSymbol(declarationSyntax, cancellationToken);

            if (local != null)
            {
                return local;
            }

            // Might be a field
            Binder binder = GetEnclosingBinder(declarationSyntax.Position);
            return binder?.LookupDeclaredField(declarationSyntax).GetPublicSymbol();
        }

        internal override LocalSymbol GetAdjustedLocalSymbol(SourceLocalSymbol originalSymbol)
        {
            var position = originalSymbol.IdentifierToken.SpanStart;
            return GetMemberModel(position)?.GetAdjustedLocalSymbol(originalSymbol) ?? originalSymbol;
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

            Binder binder = _binderFactory.GetInNamespaceBinder(declarationSyntax.Parent);

            for (; binder != null; binder = binder.Next)
            {
                var usingAliases = binder.UsingAliases;

                if (!usingAliases.IsDefault)
                {
                    foreach (var alias in usingAliases)
                    {
                        if (alias.Alias.GetFirstLocation().SourceSpan == declarationSyntax.Alias.Name.Span)
                        {
                            return alias.Alias.GetPublicSymbol();
                        }
                    }

                    break;
                }
            }

            return null;
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

            var binder = _binderFactory.GetInNamespaceBinder(declarationSyntax.Parent);

            for (; binder != null; binder = binder.Next)
            {
                var externAliases = binder.ExternAliases;

                if (!externAliases.IsDefault)
                {
                    foreach (var alias in externAliases)
                    {
                        if (alias.Alias.GetFirstLocation().SourceSpan == declarationSyntax.Identifier.Span)
                        {
                            return alias.Alias.GetPublicSymbol();
                        }
                    }

                    break;
                }
            }

            return null;
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

            if (memberDecl is TypeDeclarationSyntax typeDecl && typeDecl.ParameterList == paramList)
            {
                method = TryGetSynthesizedPrimaryConstructor(typeDecl);
            }
            else
            {
                method = (GetDeclaredSymbol(memberDecl, cancellationToken) as IMethodSymbol).GetSymbol();
            }

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

            var property = (GetDeclaredSymbol(memberDecl, cancellationToken) as IPropertySymbol).GetSymbol();
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

            var delegateType = (GetDeclaredSymbol(memberDecl, cancellationToken) as INamedTypeSymbol).GetSymbol();
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
        /// Given a parameter declaration syntax node, get the corresponding symbol.
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

            return GetDeclaredNonLambdaParameterSymbol(declarationSyntax, cancellationToken).GetPublicSymbol();
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

            if (typeParameter.Parent is TypeParameterListSyntax typeParamList)
            {
                ISymbol parameterizedSymbol = null;
                switch (typeParamList.Parent)
                {
                    case MemberDeclarationSyntax memberDecl:
                        parameterizedSymbol = GetDeclaredSymbol(memberDecl, cancellationToken);
                        break;
                    case LocalFunctionStatementSyntax localDecl:
                        parameterizedSymbol = GetDeclaredSymbol(localDecl, cancellationToken);
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(typeParameter.Parent.Kind());
                }

                switch (parameterizedSymbol.GetSymbol())
                {
                    case NamedTypeSymbol typeSymbol:
                        return this.GetTypeParameterSymbol(typeSymbol.TypeParameters, typeParameter).GetPublicSymbol();

                    case MethodSymbol methodSymbol:
                        return (this.GetTypeParameterSymbol(methodSymbol.TypeParameters, typeParameter) ??
                            ((object)methodSymbol.PartialDefinitionPart == null
                                ? null
                                : this.GetTypeParameterSymbol(methodSymbol.PartialDefinitionPart.TypeParameters, typeParameter))).GetPublicSymbol();
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

            // Global statements don't have their parent in common, but should belong to the same compilation unit
            bool isGlobalStatement = firstStatement.Parent is GlobalStatementSyntax;
            if (isGlobalStatement && (lastStatement.Parent is not GlobalStatementSyntax || firstStatement.Parent.Parent != lastStatement.Parent.Parent))
            {
                throw new ArgumentException("global statements not within the same compilation unit");
            }

            // Non-global statements, the parents should be the same
            if (!isGlobalStatement && (firstStatement.Parent == null || firstStatement.Parent != lastStatement.Parent))
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

        public override DataFlowAnalysis AnalyzeDataFlow(ConstructorInitializerSyntax constructorInitializer)
        {
            if (constructorInitializer == null)
            {
                throw new ArgumentNullException(nameof(constructorInitializer));
            }

            if (!IsInTree(constructorInitializer))
            {
                throw new ArgumentException("node not within tree");
            }

            var context = RegionAnalysisContext(constructorInitializer);
            var result = new CSharpDataFlowAnalysis(context);
            return result;
        }

        public override DataFlowAnalysis AnalyzeDataFlow(PrimaryConstructorBaseTypeSyntax primaryConstructorBaseType)
        {
            if (primaryConstructorBaseType == null)
            {
                throw new ArgumentNullException(nameof(primaryConstructorBaseType));
            }

            if (!IsInTree(primaryConstructorBaseType))
            {
                throw new ArgumentException("node not within tree");
            }

            var context = RegionAnalysisContext(primaryConstructorBaseType);
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
                if (memberDeclaration.Kind() is SyntaxKind.NamespaceDeclaration or SyntaxKind.FileScopedNamespaceDeclaration)
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
            if (memberDeclaration.Kind() is SyntaxKind.NamespaceDeclaration or SyntaxKind.FileScopedNamespaceDeclaration
                || SyntaxFacts.IsTypeDeclaration(memberDeclaration.Kind()))
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
            return this.GetDeclaredMember(container, declarationSyntax.Span, isKnownToBeANamespace: false, name);
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

        public override ForEachStatementInfo GetForEachStatementInfo(CommonForEachStatementSyntax node)
        {
            MemberSemanticModel memberModel = GetMemberModel(node);
            return memberModel == null ? default(ForEachStatementInfo) : memberModel.GetForEachStatementInfo(node);
        }

        public override DeconstructionInfo GetDeconstructionInfo(AssignmentExpressionSyntax node)
        {
            MemberSemanticModel memberModel = GetMemberModel(node);
            return memberModel?.GetDeconstructionInfo(node) ?? default;
        }

        public override DeconstructionInfo GetDeconstructionInfo(ForEachVariableStatementSyntax node)
        {
            MemberSemanticModel memberModel = GetMemberModel(node);
            return memberModel?.GetDeconstructionInfo(node) ?? default;
        }

        internal override Symbol RemapSymbolIfNecessaryCore(Symbol symbol)
        {
            Debug.Assert(symbol is LocalSymbol or ParameterSymbol or MethodSymbol { MethodKind: MethodKind.LambdaMethod });

            if (symbol.TryGetFirstLocation() is not Location location)
            {
                return symbol;
            }

            // The symbol may be from a distinct syntax tree - perhaps the
            // symbol was returned from LookupSymbols() for instance.
            if (location.SourceTree != this.SyntaxTree)
            {
                return symbol;
            }

            var position = CheckAndAdjustPosition(location.SourceSpan.Start);
            var memberModel = GetMemberModel(position);
            return memberModel?.RemapSymbolIfNecessaryCore(symbol) ?? symbol;
        }

        internal override Func<SyntaxNode, bool> GetSyntaxNodesToAnalyzeFilter(SyntaxNode declaredNode, ISymbol declaredSymbol)
        {
            switch (declaredNode)
            {
                case CompilationUnitSyntax unit when SynthesizedSimpleProgramEntryPointSymbol.GetSimpleProgramEntryPoint(Compilation, unit, fallbackToMainEntryPoint: false) is SynthesizedSimpleProgramEntryPointSymbol entryPoint:
                    switch (declaredSymbol.Kind)
                    {
                        case SymbolKind.Namespace:
                            Debug.Assert(((INamespaceSymbol)declaredSymbol).IsGlobalNamespace);
                            // Do not include top level global statements into a global namespace
                            return (node) => node.Kind() != SyntaxKind.GlobalStatement || node.Parent != unit;

                        case SymbolKind.Method:
                            Debug.Assert((object)declaredSymbol.GetSymbol() == (object)entryPoint);
                            // Include only global statements at the top level
                            return (node) => node.Parent != unit || node.Kind() == SyntaxKind.GlobalStatement;

                        case SymbolKind.NamedType:
                            Debug.Assert((object)declaredSymbol.GetSymbol() == (object)entryPoint.ContainingSymbol);
                            return (node) => false;

                        default:
                            ExceptionUtilities.UnexpectedValue(declaredSymbol.Kind);
                            break;
                    }
                    break;

                case TypeDeclarationSyntax typeDeclaration when TryGetSynthesizedPrimaryConstructor(typeDeclaration) is SynthesizedPrimaryConstructor ctor:
                    if (typeDeclaration.Kind() is (SyntaxKind.RecordDeclaration or SyntaxKind.ClassDeclaration))
                    {
                        switch (declaredSymbol.Kind)
                        {
                            case SymbolKind.Method:
                                Debug.Assert((object)declaredSymbol.GetSymbol() == (object)ctor);
                                return (node) =>
                                       {
                                           // Accept only nodes that either match, or above/below of a 'parameter list'/'base arguments list'.
                                           if (node.Parent == typeDeclaration)
                                           {
                                               return node == typeDeclaration.ParameterList || node == typeDeclaration.BaseList;
                                           }
                                           else if (node.Parent is BaseListSyntax baseList)
                                           {
                                               return node == typeDeclaration.PrimaryConstructorBaseTypeIfClass;
                                           }
                                           else if (node.Parent is PrimaryConstructorBaseTypeSyntax baseType && baseType == typeDeclaration.PrimaryConstructorBaseTypeIfClass)
                                           {
                                               return node == baseType.ArgumentList;
                                           }

                                           return true;
                                       };

                            case SymbolKind.NamedType:
                                Debug.Assert((object)declaredSymbol.GetSymbol() == (object)ctor.ContainingSymbol);
                                // Accept nodes that do not match a 'parameter list'/'base arguments list'.
                                return (node) => node != typeDeclaration.ParameterList &&
                                                 !(node.Kind() == SyntaxKind.ArgumentList && node == typeDeclaration.PrimaryConstructorBaseTypeIfClass?.ArgumentList);

                            default:
                                ExceptionUtilities.UnexpectedValue(declaredSymbol.Kind);
                                break;
                        }
                    }
                    else
                    {
                        switch (declaredSymbol.Kind)
                        {
                            case SymbolKind.Method:
                                Debug.Assert((object)declaredSymbol.GetSymbol() == (object)ctor);
                                return (node) =>
                                {
                                    // Accept only nodes that either match, or above/below of a 'parameter list'.
                                    if (node.Parent == typeDeclaration)
                                    {
                                        return node == typeDeclaration.ParameterList;
                                    }

                                    return true;
                                };

                            case SymbolKind.NamedType:
                                Debug.Assert((object)declaredSymbol.GetSymbol() == (object)ctor.ContainingSymbol);
                                // Accept nodes that do not match a 'parameter list'.
                                return (node) => node != typeDeclaration.ParameterList;

                            default:
                                ExceptionUtilities.UnexpectedValue(declaredSymbol.Kind);
                                break;
                        }
                    }
                    break;

                case PrimaryConstructorBaseTypeSyntax { Parent: BaseListSyntax { Parent: TypeDeclarationSyntax typeDeclaration } } baseType
                        when typeDeclaration.PrimaryConstructorBaseTypeIfClass == declaredNode && TryGetSynthesizedPrimaryConstructor(typeDeclaration) is SynthesizedPrimaryConstructor ctor:
                    if ((object)declaredSymbol.GetSymbol() == (object)ctor)
                    {
                        // Only 'base arguments list' or nodes below it
                        return (node) => node != baseType.Type;
                    }
                    break;

                case ParameterSyntax param when declaredSymbol.Kind == SymbolKind.Property && param.Parent?.Parent is RecordDeclarationSyntax recordDeclaration && recordDeclaration.ParameterList == param.Parent:
                    Debug.Assert(declaredSymbol.GetSymbol() is SynthesizedRecordPropertySymbol);
                    return (node) => false;
            }

            return null;
        }

        internal override bool ShouldSkipSyntaxNodeAnalysis(SyntaxNode node, ISymbol containingSymbol)
        {
            if (containingSymbol.Kind is SymbolKind.Method)
            {
                switch (node)
                {
                    case TypeDeclarationSyntax:
                        // Skip the topmost type declaration syntax node when analyzing primary constructor
                        // to avoid duplicate syntax node callbacks.
                        // We will analyze this node when analyzing the type declaration type symbol.
                        return true;

                    case CompilationUnitSyntax:
                        // Skip compilation unit syntax node when analyzing synthesized top level entry point method
                        // to avoid duplicate syntax node callbacks.
                        // We will analyze this node when analyzing the global namespace symbol.
                        return true;
                }
            }

            return false;
        }
    }
}
