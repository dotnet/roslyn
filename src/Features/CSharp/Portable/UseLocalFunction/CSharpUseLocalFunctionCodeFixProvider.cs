// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseLocalFunction
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseLocalFunction), Shared]
    internal class CSharpUseLocalFunctionCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        private static readonly TypeSyntax s_objectType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword));

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpUseLocalFunctionCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.UseLocalFunctionDiagnosticId);

        protected override bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic)
            => !diagnostic.IsSuppressed;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            RegisterCodeFix(context, CSharpAnalyzersResources.Use_local_function, nameof(CSharpAnalyzersResources.Use_local_function));
            return Task.CompletedTask;
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var nodesFromDiagnostics = new List<(
                LocalDeclarationStatementSyntax declaration,
                AnonymousFunctionExpressionSyntax function,
                List<ExpressionSyntax> references)>(diagnostics.Length);

            var nodesToTrack = new HashSet<SyntaxNode>();

            foreach (var diagnostic in diagnostics)
            {
                var localDeclaration = (LocalDeclarationStatementSyntax)diagnostic.AdditionalLocations[0].FindNode(cancellationToken);
                var anonymousFunction = (AnonymousFunctionExpressionSyntax)diagnostic.AdditionalLocations[1].FindNode(cancellationToken);

                var references = new List<ExpressionSyntax>(diagnostic.AdditionalLocations.Count - 2);

                for (var i = 2; i < diagnostic.AdditionalLocations.Count; i++)
                {
                    references.Add((ExpressionSyntax)diagnostic.AdditionalLocations[i].FindNode(getInnermostNodeForTie: true, cancellationToken));
                }

                nodesFromDiagnostics.Add((localDeclaration, anonymousFunction, references));

                nodesToTrack.Add(localDeclaration);
                nodesToTrack.Add(anonymousFunction);
                nodesToTrack.AddRange(references);
            }

            var root = editor.OriginalRoot;
            var currentRoot = root.TrackNodes(nodesToTrack);

            var optionSet = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var languageVersion = semanticModel.SyntaxTree.Options.LanguageVersion();
            var makeStaticIfPossible = languageVersion >= LanguageVersion.CSharp8 &&
                optionSet.GetOption(CSharpCodeStyleOptions.PreferStaticLocalFunction).Value;

            // Process declarations in reverse order so that we see the effects of nested
            // declarations befor processing the outer decls.
            foreach (var (localDeclaration, anonymousFunction, references) in nodesFromDiagnostics.OrderByDescending(nodes => nodes.function.SpanStart))
            {
                var delegateType = (INamedTypeSymbol)semanticModel.GetTypeInfo(anonymousFunction, cancellationToken).ConvertedType;
                var parameterList = GenerateParameterList(anonymousFunction, delegateType.DelegateInvokeMethod);
                var makeStatic = MakeStatic(semanticModel, makeStaticIfPossible, localDeclaration, cancellationToken);

                var currentLocalDeclaration = currentRoot.GetCurrentNode(localDeclaration);
                var currentAnonymousFunction = currentRoot.GetCurrentNode(anonymousFunction);

                currentRoot = ReplaceAnonymousWithLocalFunction(
                    document.Project.Solution.Workspace.Services, currentRoot,
                    currentLocalDeclaration, currentAnonymousFunction,
                    delegateType.DelegateInvokeMethod, parameterList, makeStatic);

                // these invocations might actually be inside the local function! so we have to do this separately
                currentRoot = ReplaceReferences(
                    document, currentRoot,
                    delegateType, parameterList,
                    references.Select(node => currentRoot.GetCurrentNode(node)).ToImmutableArray());
            }

            editor.ReplaceNode(root, currentRoot);
        }

        private static bool MakeStatic(
            SemanticModel semanticModel,
            bool makeStaticIfPossible,
            LocalDeclarationStatementSyntax localDeclaration,
            CancellationToken cancellationToken)
        {
            // Determines if we can make the local function 'static'.  We can make it static
            // if the original lambda did not capture any variables (other than the local 
            // variable itself).  it's ok for the lambda to capture itself as a static-local
            // function can reference itself without any problems.
            if (makeStaticIfPossible)
            {
                var localSymbol = semanticModel.GetDeclaredSymbol(
                    localDeclaration.Declaration.Variables[0], cancellationToken);

                var dataFlow = semanticModel.AnalyzeDataFlow(localDeclaration);
                if (dataFlow.Succeeded)
                {
                    var capturedVariables = dataFlow.Captured.Remove(localSymbol);
                    if (capturedVariables.IsEmpty)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static SyntaxNode ReplaceAnonymousWithLocalFunction(
            HostWorkspaceServices services, SyntaxNode currentRoot,
            LocalDeclarationStatementSyntax localDeclaration, AnonymousFunctionExpressionSyntax anonymousFunction,
            IMethodSymbol delegateMethod, ParameterListSyntax parameterList, bool makeStatic)
        {
            var newLocalFunctionStatement = CreateLocalFunctionStatement(localDeclaration, anonymousFunction, delegateMethod, parameterList, makeStatic)
                .WithTriviaFrom(localDeclaration)
                .WithAdditionalAnnotations(Formatter.Annotation);

            var editor = new SyntaxEditor(currentRoot, services);
            editor.ReplaceNode(localDeclaration, newLocalFunctionStatement);

            var anonymousFunctionStatement = anonymousFunction.GetAncestor<StatementSyntax>();
            if (anonymousFunctionStatement != localDeclaration)
            {
                // This is the split decl+init form.  Remove the second statement as we're
                // merging into the first one.
                editor.RemoveNode(anonymousFunctionStatement);
            }

            return editor.GetChangedRoot();
        }

        private static SyntaxNode ReplaceReferences(
            Document document, SyntaxNode currentRoot,
            INamedTypeSymbol delegateType, ParameterListSyntax parameterList,
            ImmutableArray<ExpressionSyntax> references)
        {
            return currentRoot.ReplaceNodes(references, (_ /* nested invocations! */, reference) =>
            {
                if (reference is InvocationExpressionSyntax invocation)
                {
                    var directInvocation = invocation.Expression is MemberAccessExpressionSyntax memberAccess // it's a .Invoke call
                        ? invocation.WithExpression(memberAccess.Expression).WithTriviaFrom(invocation) // remove it
                        : invocation;

                    return WithNewParameterNames(directInvocation, delegateType.DelegateInvokeMethod, parameterList);
                }

                // It's not an invocation. Wrap the identifier in a cast (which will be remove by the simplifier if unnecessary)
                // to ensure we preserve semantics in cases like overload resolution or generic type inference.
                return SyntaxGenerator.GetGenerator(document).CastExpression(delegateType, reference);
            });
        }

        private static LocalFunctionStatementSyntax CreateLocalFunctionStatement(
            LocalDeclarationStatementSyntax localDeclaration,
            AnonymousFunctionExpressionSyntax anonymousFunction,
            IMethodSymbol delegateMethod,
            ParameterListSyntax parameterList,
            bool makeStatic)
        {
            var modifiers = new SyntaxTokenList();
            if (makeStatic)
            {
                modifiers = modifiers.Add(SyntaxFactory.Token(SyntaxKind.StaticKeyword));
            }

            if (anonymousFunction.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword))
            {
                modifiers = modifiers.Add(anonymousFunction.AsyncKeyword);
            }

            var returnType = delegateMethod.GenerateReturnTypeSyntax();

            var identifier = localDeclaration.Declaration.Variables[0].Identifier;
            var typeParameterList = (TypeParameterListSyntax)null;

            var constraintClauses = default(SyntaxList<TypeParameterConstraintClauseSyntax>);

            var body = anonymousFunction.Body.IsKind(SyntaxKind.Block, out BlockSyntax block)
                ? block
                : null;

            var expressionBody = anonymousFunction.Body is ExpressionSyntax expression
                ? SyntaxFactory.ArrowExpressionClause(((LambdaExpressionSyntax)anonymousFunction).ArrowToken, expression)
                : null;

            var semicolonToken = anonymousFunction.Body is ExpressionSyntax
                ? localDeclaration.SemicolonToken
                : default;

            return SyntaxFactory.LocalFunctionStatement(
                modifiers, returnType, identifier, typeParameterList, parameterList,
                constraintClauses, body, expressionBody, semicolonToken);
        }

        private static ParameterListSyntax GenerateParameterList(
            AnonymousFunctionExpressionSyntax anonymousFunction, IMethodSymbol delegateMethod)
        {
            var parameterList = TryGetOrCreateParameterList(anonymousFunction);
            var i = 0;

            return parameterList != null
                ? parameterList.ReplaceNodes(parameterList.Parameters, (parameterNode, _) => PromoteParameter(parameterNode, delegateMethod.Parameters.ElementAtOrDefault(i++)))
                : SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(delegateMethod.Parameters.Select(parameter =>
                    PromoteParameter(SyntaxFactory.Parameter(parameter.Name.ToIdentifierToken()), parameter))));

            static ParameterSyntax PromoteParameter(ParameterSyntax parameterNode, IParameterSymbol delegateParameter)
            {
                // delegateParameter may be null, consider this case: Action x = (a, b) => { };
                // we will still fall back to object

                if (parameterNode.Type == null)
                {
                    parameterNode = parameterNode.WithType(delegateParameter?.Type.GenerateTypeSyntax() ?? s_objectType);
                }

                if (delegateParameter?.HasExplicitDefaultValue == true)
                {
                    parameterNode = parameterNode.WithDefault(GetDefaultValue(delegateParameter));
                }

                return parameterNode;
            }
        }

        private static ParameterListSyntax TryGetOrCreateParameterList(AnonymousFunctionExpressionSyntax anonymousFunction)
        {
            switch (anonymousFunction)
            {
                case SimpleLambdaExpressionSyntax simpleLambda:
                    return SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(simpleLambda.Parameter));
                case ParenthesizedLambdaExpressionSyntax parenthesizedLambda:
                    return parenthesizedLambda.ParameterList;
                case AnonymousMethodExpressionSyntax anonymousMethod:
                    return anonymousMethod.ParameterList; // may be null!
                default:
                    throw ExceptionUtilities.UnexpectedValue(anonymousFunction);
            }
        }

        private static InvocationExpressionSyntax WithNewParameterNames(InvocationExpressionSyntax invocation, IMethodSymbol method, ParameterListSyntax newParameterList)
        {
            return invocation.ReplaceNodes(invocation.ArgumentList.Arguments, (argumentNode, _) =>
            {
                if (argumentNode.NameColon == null)
                {
                    return argumentNode;
                }

                var parameterIndex = TryDetermineParameterIndex(argumentNode.NameColon, method);
                if (parameterIndex == -1)
                {
                    return argumentNode;
                }

                var newParameter = newParameterList.Parameters.ElementAtOrDefault(parameterIndex);
                if (newParameter == null || newParameter.Identifier.IsMissing)
                {
                    return argumentNode;
                }

                return argumentNode.WithNameColon(argumentNode.NameColon.WithName(SyntaxFactory.IdentifierName(newParameter.Identifier)));
            });
        }

        private static int TryDetermineParameterIndex(NameColonSyntax argumentNameColon, IMethodSymbol method)
        {
            var name = argumentNameColon.Name.Identifier.ValueText;
            return method.Parameters.IndexOf(p => p.Name == name);
        }

        private static EqualsValueClauseSyntax GetDefaultValue(IParameterSymbol parameter)
            => SyntaxFactory.EqualsValueClause(ExpressionGenerator.GenerateExpression(parameter.Type, parameter.ExplicitDefaultValue, canUseFieldReference: true));
    }
}
