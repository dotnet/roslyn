// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.UseCollectionInitializer
{
    internal abstract class AbstractUseCollectionInitializerDiagnosticAnalyzer<
        TSyntaxKind,
        TExpressionSyntax,
        TStatementSyntax,
        TObjectCreationExpressionSyntax,
        TMemberAccessExpressionSyntax,
        TInvocationExpressionSyntax,
        TExpressionStatementSyntax,
        TVariableDeclarator>
        : AbstractCodeStyleDiagnosticAnalyzer
        where TSyntaxKind : struct
        where TExpressionSyntax : SyntaxNode
        where TStatementSyntax : SyntaxNode
        where TObjectCreationExpressionSyntax : TExpressionSyntax
        where TMemberAccessExpressionSyntax : TExpressionSyntax
        where TInvocationExpressionSyntax : TExpressionSyntax
        where TExpressionStatementSyntax : TStatementSyntax
        where TVariableDeclarator : SyntaxNode
    {
        public bool OpenFileOnly(Workspace workspace) => false;

        protected AbstractUseCollectionInitializerDiagnosticAnalyzer() 
            : base(IDEDiagnosticIds.UseCollectionInitializerDiagnosticId,
                   new LocalizableResourceString(nameof(FeaturesResources.Simplify_collection_initialization), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Collection_initialization_can_be_simplified), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterCompilationStartAction(OnCompilationStart);

        private void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            var ienumerableType = context.Compilation.GetTypeByMetadataName("System.Collections.IEnumerable") as INamedTypeSymbol;
            if (ienumerableType != null)
            {
                context.RegisterSyntaxNodeAction(
                    nodeContext => AnalyzeNode(nodeContext, ienumerableType),
                    GetObjectCreationSyntaxKind());
            }
        }

        protected abstract bool AreCollectionInitializersSupported(SyntaxNodeAnalysisContext context);
        protected abstract TSyntaxKind GetObjectCreationSyntaxKind();

        private void AnalyzeNode(SyntaxNodeAnalysisContext context, INamedTypeSymbol ienumerableType)
        {
            if (!AreCollectionInitializersSupported(context))
            {
                return;
            }

            var cancellationToken = context.CancellationToken;
            var objectCreationExpression = (TObjectCreationExpressionSyntax)context.Node;
            var language = objectCreationExpression.Language;

            var optionSet = context.Options.GetOptionSet();
            var option = optionSet.GetOption(CodeStyleOptions.PreferCollectionInitializer, language);
            if (!option.Value)
            {
                // not point in analyzing if the option is off.
                return;
            }

            // Object creation can only be converted to collection initializer if it
            // implements the IEnumerable type.
            var objectType = context.SemanticModel.GetTypeInfo(objectCreationExpression, cancellationToken);
            if (objectType.Type == null || !objectType.Type.AllInterfaces.Contains(ienumerableType))
            {
                return;
            }

            var syntaxFacts = GetSyntaxFactsService();
            var analyzer = new Analyzer<
                TExpressionSyntax, TStatementSyntax, TObjectCreationExpressionSyntax, 
                TMemberAccessExpressionSyntax, TInvocationExpressionSyntax,
                TExpressionStatementSyntax, TVariableDeclarator>(syntaxFacts, objectCreationExpression);
            var matches = analyzer.Analyze();
            if (matches.Length == 0)
            {
                return;
            }

            var locations = ImmutableArray.Create(objectCreationExpression.GetLocation());

            var severity = option.Notification.Value;
            context.ReportDiagnostic(Diagnostic.Create(
                CreateDescriptorWithSeverity(severity),
                objectCreationExpression.GetLocation(),
                additionalLocations: locations));

            FadeOutCode(context, optionSet, matches, locations);
        }

        private void FadeOutCode(
            SyntaxNodeAnalysisContext context,
            OptionSet optionSet,
            ImmutableArray<TExpressionStatementSyntax> matches,
            ImmutableArray<Location> locations)
        {
            var syntaxTree = context.Node.SyntaxTree;

            var fadeOutCode = optionSet.GetOption(
                CodeStyleOptions.PreferCollectionInitializer_FadeOutCode, context.Node.Language);
            if (!fadeOutCode)
            {
                return;
            }

            var syntaxFacts = GetSyntaxFactsService();

            foreach (var match in matches)
            {
                var expression = syntaxFacts.GetExpressionOfExpressionStatement(match);

                if (syntaxFacts.IsInvocationExpression(expression))
                {
                    var arguments = syntaxFacts.GetArgumentsOfInvocationExpression(expression);
                    var location1 = Location.Create(syntaxTree, TextSpan.FromBounds(
                        match.SpanStart, arguments[0].SpanStart));

                    context.ReportDiagnostic(Diagnostic.Create(
                        UnnecessaryWithSuggestionDescriptor, location1, additionalLocations: locations));

                    context.ReportDiagnostic(Diagnostic.Create(
                        UnnecessaryWithoutSuggestionDescriptor,
                        Location.Create(syntaxTree, TextSpan.FromBounds(
                            arguments.Last().FullSpan.End,
                            match.Span.End)),
                        additionalLocations: locations));
                }
            }
        }

        protected abstract ISyntaxFactsService GetSyntaxFactsService();
    }

    internal struct Analyzer<
            TExpressionSyntax,
            TStatementSyntax,
            TObjectCreationExpressionSyntax, 
            TMemberAccessExpressionSyntax,
            TInvocationExpressionSyntax,
            TExpressionStatementSyntax,
            TVariableDeclaratorSyntax>
        where TExpressionSyntax : SyntaxNode
        where TStatementSyntax : SyntaxNode
        where TObjectCreationExpressionSyntax : TExpressionSyntax
        where TMemberAccessExpressionSyntax : TExpressionSyntax
        where TInvocationExpressionSyntax : TExpressionSyntax
        where TExpressionStatementSyntax : TStatementSyntax
        where TVariableDeclaratorSyntax : SyntaxNode
    {
        private readonly ISyntaxFactsService _syntaxFacts;
        private readonly TObjectCreationExpressionSyntax _objectCreationExpression;

        private TStatementSyntax _containingStatement;
        private SyntaxNodeOrToken _valuePattern;

        public Analyzer(
            ISyntaxFactsService syntaxFacts, 
            TObjectCreationExpressionSyntax objectCreationExpression) : this()
        {
            _syntaxFacts = syntaxFacts;
            _objectCreationExpression = objectCreationExpression;
        }

        internal ImmutableArray<TExpressionStatementSyntax> Analyze()
        {
            if (_syntaxFacts.GetObjectCreationInitializer(_objectCreationExpression) != null)
            {
                // Don't bother if this already has an initializer.
                return ImmutableArray<TExpressionStatementSyntax>.Empty;
            }

            _containingStatement = _objectCreationExpression.FirstAncestorOrSelf<TStatementSyntax>();
            if (_containingStatement == null)
            {
                return ImmutableArray<TExpressionStatementSyntax>.Empty;
            }

            if (!TryInitializeVariableDeclarationCase() &&
                !TryInitializeAssignmentCase())
            {
                return ImmutableArray<TExpressionStatementSyntax>.Empty;
            }

            var matches = ArrayBuilder<TExpressionStatementSyntax>.GetInstance();
            AddMatches(matches);
            return matches.ToImmutableAndFree(); ;
        }

        private void AddMatches(ArrayBuilder<TExpressionStatementSyntax> matches)
        {
            var containingBlock = _containingStatement.Parent;
            var foundStatement = false;

            var seenInvocation = false;
            var seenIndexAssignment = false;
            
            foreach (var child in containingBlock.ChildNodesAndTokens())
            {
                if (!foundStatement)
                {
                    if (child == _containingStatement)
                    {
                        foundStatement = true;
                    }

                    continue;
                }

                if (child.IsToken)
                {
                    return;
                }

                var statement = child.AsNode() as TExpressionStatementSyntax;
                if (statement == null)
                {
                    return;
                }

                SyntaxNode instance = null;
                if (!seenIndexAssignment)
                {
                    if (TryAnalyzeAddInvocation(statement, out instance))
                    {
                        seenInvocation = true;
                    }
                }

                if (!seenInvocation)
                {
                    if (TryAnalyzeIndexAssignment(statement, out instance))
                    {
                        seenIndexAssignment = true;
                    }
                }

                if (instance == null)
                {
                    return;
                }

                if (!ValuePatternMatches((TExpressionSyntax)instance))
                {
                    return;
                }

                matches.Add(statement);
            }
        }

        private bool TryAnalyzeIndexAssignment(
            TExpressionStatementSyntax statement,
            out SyntaxNode instance)
        {
            instance = null;
            if (!_syntaxFacts.SupportsIndexingInitializer(statement.SyntaxTree.Options))
            {
                return false;
            }

            if (!_syntaxFacts.IsSimpleAssignmentStatement(statement))
            {
                return false;
            }

            SyntaxNode left, right;
            _syntaxFacts.GetPartsOfAssignmentStatement(statement, out left, out right);

            if (!_syntaxFacts.IsElementAccessExpression(left))
            {
                return false;
            }

            instance = _syntaxFacts.GetExpressionOfElementAccessExpression(left);
            return true;
        }

        private bool TryAnalyzeAddInvocation(
            TExpressionStatementSyntax statement,
            out SyntaxNode instance)
        {
            instance = null;
            var invocationExpression = _syntaxFacts.GetExpressionOfExpressionStatement(statement) as TInvocationExpressionSyntax;
            if (invocationExpression == null)
            {
                return false;
            }

            var arguments = _syntaxFacts.GetArgumentsOfInvocationExpression(invocationExpression);
            if (arguments.Count < 1)
            {
                return false;
            }

            foreach (var argument in arguments)
            {
                if (!_syntaxFacts.IsSimpleArgument(argument))
                {
                    return false;
                }
            }

            var memberAccess = _syntaxFacts.GetExpressionOfInvocationExpression(invocationExpression) as TMemberAccessExpressionSyntax;
            if (memberAccess == null)
            {
                return false;
            }

            if (!_syntaxFacts.IsSimpleMemberAccessExpression(memberAccess))
            {
                return false;
            }

            SyntaxNode memberName;
            _syntaxFacts.GetPartsOfMemberAccessExpression(memberAccess, out instance, out memberName);

            string name;
            int arity;
            _syntaxFacts.GetNameAndArityOfSimpleName(memberName, out name, out arity);

            if (arity != 0 || !name.Equals(nameof(IList.Add)))
            {
                return false;
            }

            return true;
        }

        private bool ValuePatternMatches(TExpressionSyntax expression)
        {
            if (_valuePattern.IsToken)
            {
                return _syntaxFacts.IsIdentifierName(expression) &&
                    _syntaxFacts.AreEquivalent(
                        _valuePattern.AsToken(),
                        _syntaxFacts.GetIdentifierOfSimpleName(expression));
            }
            else
            {
                return _syntaxFacts.AreEquivalent(
                    _valuePattern.AsNode(), expression);
            }
        }

        private bool TryInitializeAssignmentCase()
        {
            if (!_syntaxFacts.IsSimpleAssignmentStatement(_containingStatement))
            {
                return false;
            }

            SyntaxNode left, right;
            _syntaxFacts.GetPartsOfAssignmentStatement(_containingStatement, out left, out right);
            if (right != _objectCreationExpression)
            {
                return false;
            }

            _valuePattern = left;
            return true;
        }

        private bool TryInitializeVariableDeclarationCase()
        {
            if (!_syntaxFacts.IsLocalDeclarationStatement(_containingStatement))
            {
                return false;
            }

            var containingDeclarator = _objectCreationExpression.FirstAncestorOrSelf<TVariableDeclaratorSyntax>();
            if (containingDeclarator == null)
            {
                return false;
            }

            if (!_syntaxFacts.IsDeclaratorOfLocalDeclarationStatement(containingDeclarator, _containingStatement))
            {
                return false;
            }

            _valuePattern = _syntaxFacts.GetIdentifierOfVariableDeclarator(containingDeclarator);
            return true;
        }
    }
}