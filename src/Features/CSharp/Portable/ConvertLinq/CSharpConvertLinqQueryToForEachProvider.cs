// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.ConvertLinq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.ConvertLinq;

using static CSharpSyntaxTokens;
using static SyntaxFactory;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ConvertLinqQueryToForEach), Shared]
internal sealed class CSharpConvertLinqQueryToForEachProvider : AbstractConvertLinqQueryToForEachProvider<QueryExpressionSyntax, StatementSyntax>
{
    private static readonly TypeSyntax VarNameIdentifier = IdentifierName("var");

    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public CSharpConvertLinqQueryToForEachProvider()
    {
    }

    protected override string Title => CSharpFeaturesResources.Convert_to_foreach;

    protected override bool TryConvert(
        QueryExpressionSyntax queryExpression,
        SemanticModel semanticModel,
        ISemanticFactsService semanticFacts,
        CancellationToken cancellationToken,
        out DocumentUpdateInfo documentUpdateInfo)
            => new Converter(semanticModel, semanticFacts, queryExpression, cancellationToken).TryConvert(out documentUpdateInfo);

    /// <summary>
    /// Finds a QueryExpressionSyntax node for the span.
    /// </summary>
    protected override Task<QueryExpressionSyntax> FindNodeToRefactorAsync(CodeRefactoringContext context)
        => context.TryGetRelevantNodeAsync<QueryExpressionSyntax>();

    private sealed class Converter(SemanticModel semanticModel, ISemanticFactsService semanticFacts, QueryExpressionSyntax source, CancellationToken cancellationToken)
    {
        private readonly SemanticModel _semanticModel = semanticModel;
        private readonly ISemanticFactsService _semanticFacts = semanticFacts;
        private readonly CancellationToken _cancellationToken = cancellationToken;
        private readonly QueryExpressionSyntax _source = source;
        private readonly List<string> _introducedLocalNames = [];

        public bool TryConvert(out DocumentUpdateInfo documentUpdateInfo)
        {
            // Do not try refactoring queries with comments or conditional compilation in them.
            // We can consider supporting queries with comments in the future.
            if (_source.DescendantTrivia().Any(trivia => trivia is (kind:
                    SyntaxKind.SingleLineCommentTrivia or
                    SyntaxKind.MultiLineCommentTrivia or
                    SyntaxKind.MultiLineDocumentationCommentTrivia) ||
                _source.ContainsDirectives))
            {
                documentUpdateInfo = null;
                return false;
            }

            // Bail out if there is no chance to convert it even with a local function.
            if (!CanTryConvertToLocalFunction() ||
                !TryCreateStackFromQueryExpression(out var queryExpressionProcessingInfo))
            {
                documentUpdateInfo = null;
                return false;
            }

            // GetDiagnostics is expensive. Move it to the end if there were no bail outs from the algorithm.
            // TODO likely adding more semantic checks will perform checks we expect from GetDiagnostics
            // We may consider removing GetDiagnostics.
            // https://github.com/dotnet/roslyn/issues/25639
            if ((TryConvertInternal(queryExpressionProcessingInfo, out documentUpdateInfo) ||
                TryReplaceWithLocalFunction(queryExpressionProcessingInfo, out documentUpdateInfo)) &&  // second attempt: at least to a local function
                !_semanticModel.GetDiagnostics(_source.Span, _cancellationToken).Any(static diagnostic => diagnostic.DefaultSeverity == DiagnosticSeverity.Error))
            {
                if (!documentUpdateInfo.Source.IsParentKind(SyntaxKind.Block) &&
                    documentUpdateInfo.Destinations.Length > 1)
                {
                    documentUpdateInfo = new DocumentUpdateInfo(documentUpdateInfo.Source, Block(documentUpdateInfo.Destinations));
                }

                return true;
            }

            documentUpdateInfo = null;
            return false;
        }

        private StatementSyntax ProcessClause(
            CSharpSyntaxNode node,
            StatementSyntax statement,
            bool isLastClause,
            bool hasExtraDeclarations,
            out StatementSyntax extraStatementToAddAbove)
        {
            extraStatementToAddAbove = null;
            switch (node.Kind())
            {
                case SyntaxKind.WhereClause:
                    return Block(IfStatement(((WhereClauseSyntax)node).Condition.WithAdditionalAnnotations(Simplifier.Annotation).WithoutTrivia(), statement));
                case SyntaxKind.FromClause:
                    var fromClause = (FromClauseSyntax)node;

                    // If we are processing the first from and
                    // there were joins and some evaluations were moved into declarations above the foreach
                    // Check if the declaration on the first fromclause should be moved for the evaluation above declarations already moved upfront.
                    ExpressionSyntax expression1;
                    if (isLastClause && hasExtraDeclarations && !IsLocalOrParameterSymbol(_source.FromClause.Expression))
                    {
                        var expressionName = _semanticFacts.GenerateNameForExpression(
                            _semanticModel,
                            fromClause.Expression,
                            capitalize: false,
                            _cancellationToken);
                        var variable = GetFreeSymbolNameAndMarkUsed(expressionName);
                        extraStatementToAddAbove = CreateLocalDeclarationStatement(variable, fromClause.Expression, generateTypeFromExpression: false);
                        expression1 = IdentifierName(variable);
                    }
                    else
                    {
                        expression1 = fromClause.Expression.WithoutTrivia();
                    }

                    return ForEachStatement(
                        fromClause.Type ?? VarNameIdentifier,
                        fromClause.Identifier,
                        expression1,
                        WrapWithBlock(statement));
                case SyntaxKind.LetClause:
                    var letClause = (LetClauseSyntax)node;
                    return AddToBlockTop(CreateLocalDeclarationStatement(letClause.Identifier, letClause.Expression, generateTypeFromExpression: false), statement);
                case SyntaxKind.JoinClause:
                    var joinClause = (JoinClauseSyntax)node;
                    if (joinClause.Into != null)
                    {
                        // This must be caught on the validation step. Therefore, here is an exception.
                        throw new ArgumentException("GroupJoin is not supported");
                    }
                    else
                    {
                        ExpressionSyntax expression2;
                        if (IsLocalOrParameterSymbol(joinClause.InExpression))
                        {
                            expression2 = joinClause.InExpression;
                        }
                        else
                        {
                            // Input: var q = from x in XX() from z in ZZ() join y in YY() on x equals y select x + y;
                            // Add 
                            // var xx = XX();
                            // var yy = YY();
                            // Do not add for ZZ()
                            var expressionName = _semanticFacts.GenerateNameForExpression(
                                _semanticModel,
                                joinClause.InExpression,
                                capitalize: false,
                                _cancellationToken);
                            var variable = GetFreeSymbolNameAndMarkUsed(expressionName);
                            extraStatementToAddAbove = CreateLocalDeclarationStatement(variable, joinClause.InExpression, generateTypeFromExpression: false);

                            // Replace YY() with yy declared above.
                            expression2 = IdentifierName(variable);
                        }

                        // Output for the join
                        // var yy == YY(); this goes to extraStatementToAddAbove
                        // ...
                        // foreach (var y in yy)
                        // {
                        //  if (object.Equals(x, y))
                        //  {
                        //      ...
                        return Block(
                            ForEachStatement(
                                joinClause.Type ?? VarNameIdentifier,
                                joinClause.Identifier,
                                expression2,
                                Block(
                                    IfStatement(
                                        InvocationExpression(
                                            MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                PredefinedType(ObjectKeyword),
                                                IdentifierName(nameof(object.Equals))),
                                            ArgumentList([
                                                Argument(joinClause.LeftExpression),
                                                Argument(joinClause.RightExpression.WithoutTrailingTrivia())])),
                                        statement)))).WithAdditionalAnnotations(Simplifier.Annotation);
                    }
                case SyntaxKind.SelectClause:
                    // This is not the latest Select in the Query Expression
                    // There is a QueryBody with the Continuation as a parent.
                    var selectClause = (SelectClauseSyntax)node;
                    var identifier = ((QueryBodySyntax)selectClause.Parent).Continuation.Identifier;
                    return AddToBlockTop(CreateLocalDeclarationStatement(identifier, selectClause.Expression, generateTypeFromExpression: true), statement);
                default:
                    throw new ArgumentException($"Unexpected node kind {node.Kind()}");
            }
        }

        private bool TryConvertInternal(QueryExpressionProcessingInfo queryExpressionProcessingInfo, out DocumentUpdateInfo documentUpdateInfo)
        {
            // (from a in b select a); 
            var parent = _source.WalkUpParentheses().Parent;

            switch (parent.Kind())
            {
                // return from a in b select a;
                case SyntaxKind.ReturnStatement:
                    return TryConvertIfInReturnStatement((ReturnStatementSyntax)parent, queryExpressionProcessingInfo, out documentUpdateInfo);
                // foreach(var x in from a in b select a)
                case SyntaxKind.ForEachStatement:
                    return TryConvertIfInForEach((ForEachStatementSyntax)parent, queryExpressionProcessingInfo, out documentUpdateInfo);
                // (from a in b select a).ToList(), (from a in b select a).Count(), etc.
                case SyntaxKind.SimpleMemberAccessExpression:
                    return TryConvertIfInMemberAccessExpression((MemberAccessExpressionSyntax)parent, queryExpressionProcessingInfo, out documentUpdateInfo);
            }

            documentUpdateInfo = null;
            return false;
        }

        /// <summary>
        /// Checks if the location of the query expression allows to convert it at least to a local function.
        /// It still does not guarantees that the conversion can be performed. There can be bail outs of later stages.
        /// </summary>
        private bool CanTryConvertToLocalFunction()
        {
            SyntaxNode currentNode = _source;
            while (currentNode != null)
            {
                if (currentNode is StatementSyntax)
                {
                    return true;
                }

                if (currentNode is ExpressionSyntax or
                    ArgumentSyntax or
                    ArgumentListSyntax or
                    EqualsValueClauseSyntax or
                    VariableDeclaratorSyntax or
                    VariableDeclarationSyntax)
                {
                    currentNode = currentNode.Parent;
                }
                else
                {
                    return false;
                }
            }

            return false;
        }

        private bool TryConvertIfInMemberAccessExpression(
           MemberAccessExpressionSyntax memberAccessExpression,
           QueryExpressionProcessingInfo queryExpressionProcessingInfo,
           out DocumentUpdateInfo documentUpdateInfo)
        {
            if (memberAccessExpression.Parent is InvocationExpressionSyntax invocationExpression)
            {
                // This also covers generic names (i.e. with type arguments) like 'ToList<int>'. 
                // The ValueText is still just 'ToList'. 
                switch (memberAccessExpression.Name.Identifier.ValueText)
                {
                    case nameof(Enumerable.ToList):
                        return TryConvertIfInToListInvocation(invocationExpression, queryExpressionProcessingInfo, out documentUpdateInfo);
                    case nameof(Enumerable.Count):
                        return TryConvertIfInCountInvocation(invocationExpression, queryExpressionProcessingInfo, out documentUpdateInfo);
                }
            }

            documentUpdateInfo = null;
            return false;
        }

        private bool TryConvertIfInCountInvocation(
            InvocationExpressionSyntax invocationExpression,
            QueryExpressionProcessingInfo queryExpressionProcessingInfo,
            out DocumentUpdateInfo documentUpdateInfo)
        {
            if (_semanticModel.GetSymbolInfo(invocationExpression, _cancellationToken).Symbol is IMethodSymbol methodSymbol &&
                methodSymbol.Parameters.Length == 0 &&
                methodSymbol.ReturnType?.SpecialType == SpecialType.System_Int32 &&
                methodSymbol.RefKind == RefKind.None)
            {
                // before var count = (from a in b select a).Count();
                // after
                // var count = 0;
                // foreach (var a in b)
                // {
                //     count++;
                //  }
                return TryConvertIfInInvocation(
                        invocationExpression,
                        queryExpressionProcessingInfo,
                        IsInt,
                        (variableIdentifier, expression) => ExpressionStatement(
                            PostfixUnaryExpression(SyntaxKind.PostIncrementExpression, variableIdentifier)), // Generating 'count++'
                        LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0)), // count = 0
                        variableName: "count",
                        out documentUpdateInfo);
            }

            documentUpdateInfo = null;
            return false;
        }

        private bool TryConvertIfInToListInvocation(
            InvocationExpressionSyntax invocationExpression,
            QueryExpressionProcessingInfo queryExpressionProcessingInfo,
            out DocumentUpdateInfo documentUpdateInfo)
        {
            // before var list = (from a in b select a).ToList();
            // after
            // var list = new List<T>();
            // foreach (var a in b)
            // {
            //     list.Add(a)
            // }
            if (_semanticModel.GetSymbolInfo(invocationExpression, _cancellationToken).Symbol is IMethodSymbol methodSymbol &&
                methodSymbol.RefKind == RefKind.None &&
                IsList(methodSymbol.ReturnType) &&
                methodSymbol.Parameters.Length == 0)
            {
                return TryConvertIfInInvocation(
                           invocationExpression,
                           queryExpressionProcessingInfo,
                           IsList,
                          (listIdentifier, expression) => ExpressionStatement(InvocationExpression(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    listIdentifier,
                                    IdentifierName(nameof(IList.Add))),
                                ArgumentList([Argument(expression)]))),
                          ObjectCreationExpression(
                              methodSymbol.GenerateReturnTypeSyntax().WithAdditionalAnnotations(Simplifier.Annotation),
                              ArgumentList(),
                              initializer: null),
                           variableName: "list",
                          out documentUpdateInfo);
            }

            documentUpdateInfo = null;
            return false;
        }

        private bool IsInt(ITypeSymbol typeSymbol)
            => typeSymbol.SpecialType == SpecialType.System_Int32;

        private bool IsList(ITypeSymbol typeSymbol)
            => Equals(typeSymbol.OriginalDefinition, _semanticModel.Compilation.GetTypeByMetadataName(typeof(List<>).FullName));

        private bool TryConvertIfInInvocation(
            InvocationExpressionSyntax invocationExpression,
            QueryExpressionProcessingInfo queryExpressionProcessingInfo,
            Func<ITypeSymbol, bool> typeCheckMethod,
            Func<ExpressionSyntax, ExpressionSyntax, StatementSyntax> leafExpressionCreationMethod,
            ExpressionSyntax initializer,
            string variableName,
            out DocumentUpdateInfo documentUpdateInfo)
        {
            var parentStatement = invocationExpression.GetAncestorOrThis<StatementSyntax>();
            if (parentStatement != null)
            {
                if (TryConvertIfInInvocationInternal(
                    invocationExpression,
                    typeCheckMethod,
                    parentStatement,
                    initializer,
                    variableName,
                    out var variable,
                    out var nodesBefore,
                    out var nodesAfter))
                {
                    var statements = GenerateStatements(expression => leafExpressionCreationMethod(variable, expression), queryExpressionProcessingInfo);
                    var list = new List<StatementSyntax>();
                    list.AddRange(nodesBefore);
                    list.AddRange(statements);
                    list.AddRange(nodesAfter);
                    documentUpdateInfo = new DocumentUpdateInfo(parentStatement, list);
                    return true;
                }
            }

            documentUpdateInfo = null;
            return false;
        }

        private bool TryConvertIfInInvocationInternal(
            InvocationExpressionSyntax invocationExpression,
            Func<ITypeSymbol, bool> typeCheckMethod,
            StatementSyntax parentStatement,
            ExpressionSyntax initializer,
            string variableName,
            out ExpressionSyntax variable,
            out StatementSyntax[] nodesBefore,
            out StatementSyntax[] nodesAfter)
        {
            var invocationParent = invocationExpression.WalkUpParentheses().Parent;
            var symbolName = GetFreeSymbolNameAndMarkUsed(variableName);

            void Convert(
                ExpressionSyntax variableExpression,
                ExpressionSyntax expressionToVerifyType,
                bool checkForLocalOrParameter,
                out ExpressionSyntax variableLocal,
                out StatementSyntax[] nodesBeforeLocal,
                out StatementSyntax[] nodesAfterLocal)
            {
                // Check that we can re-use the local variable or parameter
                if (typeCheckMethod(_semanticModel.GetTypeInfo(expressionToVerifyType, _cancellationToken).Type) &&
                    (!checkForLocalOrParameter || IsLocalOrParameterSymbol(variableExpression)))
                {
                    // before
                    // a = (from a in b select a).ToList(); or var a = (from a in b select a).ToList()
                    // after 
                    // a = new List<T>(); or var a = new List<T>();
                    // foreach(...)
                    variableLocal = variableExpression;
                    nodesBeforeLocal = [parentStatement.ReplaceNode(invocationExpression, initializer.WithAdditionalAnnotations(Simplifier.Annotation))];
                    nodesAfterLocal = [];
                }
                else
                {
                    // before 
                    // IReadOnlyList<int> a = (from a in b select a).ToList(); or an assignment
                    // after 
                    // var list = new List<T>(); or assignment
                    // foreach(...)
                    // IReadOnlyList<int> a = list;
                    variableLocal = IdentifierName(symbolName);
                    nodesBeforeLocal = new[] { CreateLocalDeclarationStatement(symbolName, initializer, generateTypeFromExpression: false) };
                    nodesAfterLocal = [parentStatement.ReplaceNode(invocationExpression, variableLocal.WithAdditionalAnnotations(Simplifier.Annotation))];
                }
            }

            switch (invocationParent.Kind())
            {
                case SyntaxKind.EqualsValueClause:
                    // Avoid for(int i = (from x in a select x).Count(); i < 10; i++)
                    if (invocationParent?.Parent.Kind() is
                            SyntaxKind.VariableDeclarator or
                            SyntaxKind.VariableDeclaration or
                            SyntaxKind.LocalDeclarationStatement &&
                        // Avoid int i = (from x in a select x).Count(), j = i;
                        ((VariableDeclarationSyntax)invocationParent.Parent.Parent).Variables.Count == 1)
                    {
                        var variableDeclarator = ((VariableDeclaratorSyntax)invocationParent.Parent);
                        Convert(
                            IdentifierName(variableDeclarator.Identifier),
                            ((VariableDeclarationSyntax)variableDeclarator.Parent).Type,
                            checkForLocalOrParameter: false,
                            out variable,
                            out nodesBefore,
                            out nodesAfter);
                        return true;
                    }

                    break;
                case SyntaxKind.SimpleAssignmentExpression:
                    var assignmentExpression = (AssignmentExpressionSyntax)invocationParent;
                    if (assignmentExpression.Right.WalkDownParentheses() == invocationExpression)
                    {
                        Convert(
                            assignmentExpression.Left,
                            assignmentExpression.Left,
                            checkForLocalOrParameter: true,
                            out variable,
                            out nodesBefore,
                            out nodesAfter);
                        return true;
                    }

                    break;
                case SyntaxKind.ReturnStatement:
                    // before return (from a in b select a).ToList();
                    // after var list = new List<T>();
                    // foreach(...)
                    // return list;
                    variable = IdentifierName(symbolName);
                    nodesBefore = new[] { CreateLocalDeclarationStatement(symbolName, initializer, generateTypeFromExpression: false) };
                    nodesAfter = new[] { ReturnStatement(variable).WithAdditionalAnnotations(Simplifier.Annotation) };
                    return true;
                    // SyntaxKind.Argument:
                    // SyntaxKind.ArrowExpressionClause is not supported
            }

            // Will still try to replace with a local function above.
            nodesBefore = null;
            nodesAfter = null;
            variable = null;
            return false;
        }

        private LocalDeclarationStatementSyntax CreateLocalDeclarationStatement(
            SyntaxToken identifier,
            ExpressionSyntax expression,
            bool generateTypeFromExpression)
        {
            var typeSyntax = generateTypeFromExpression
                ? _semanticModel.GetTypeInfo(expression, _cancellationToken).ConvertedType.GenerateTypeSyntax()
                : VarNameIdentifier;
            return LocalDeclarationStatement(
                        VariableDeclaration(
                            typeSyntax,
                            [VariableDeclarator(
                                identifier,
                                argumentList: null,
                                EqualsValueClause(expression))])).WithAdditionalAnnotations(Simplifier.Annotation);
        }

        private bool TryReplaceWithLocalFunction(QueryExpressionProcessingInfo queryExpressionProcessingInfo, out DocumentUpdateInfo documentUpdateInfo)
        {
            var parentStatement = _source.GetAncestorOrThis<StatementSyntax>();
            if (parentStatement == null)
            {
                documentUpdateInfo = null;
                return false;
            }

            // before statement ... from a in select b ...
            // after
            // IEnumerable<T> localFunction()
            // {
            //   foreach(var a in b)
            //   {
            //       yield return a;
            //   }
            // }
            //  statement ... localFunction();
            var returnTypeInfo = _semanticModel.GetTypeInfo(_source, _cancellationToken);
            ITypeSymbol returnedType;

            if (returnTypeInfo.Type.OriginalDefinition?.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
            {
                returnedType = returnTypeInfo.Type;
            }
            else
            {
                if (returnTypeInfo.ConvertedType.OriginalDefinition?.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
                {
                    returnedType = returnTypeInfo.ConvertedType;
                }
                else
                {
                    documentUpdateInfo = null;
                    return false;
                }
            }

            static StatementSyntax internalNodeMethod(ExpressionSyntax expression)
                => YieldStatement(SyntaxKind.YieldReturnStatement, expression);

            var statements = GenerateStatements(internalNodeMethod, queryExpressionProcessingInfo);
            var localFunctionNamePrefix = _semanticFacts.GenerateNameForExpression(
                _semanticModel,
                _source,
                capitalize: false,
                _cancellationToken);
            var localFunctionToken = GetFreeSymbolNameAndMarkUsed(localFunctionNamePrefix);
            var localFunctionDeclaration = LocalFunctionStatement(
                modifiers: default,
                returnType: returnedType.GenerateTypeSyntax().WithAdditionalAnnotations(Simplifier.Annotation),
                identifier: localFunctionToken,
                typeParameterList: null,
                parameterList: ParameterList(),
                constraintClauses: default,
                body: Block(
                    Token(
                        [],
                        SyntaxKind.OpenBraceToken,
                        [EndOfLine(Environment.NewLine)]),
                    [.. statements],
                    CloseBraceToken),
                expressionBody: null);

            var localFunctionInvocation = InvocationExpression(IdentifierName(localFunctionToken)).WithAdditionalAnnotations(Simplifier.Annotation);
            var newParentExpressionStatement = parentStatement.ReplaceNode(_source.WalkUpParentheses(), localFunctionInvocation.WithAdditionalAnnotations(Simplifier.Annotation));
            documentUpdateInfo = new DocumentUpdateInfo(parentStatement, [localFunctionDeclaration, newParentExpressionStatement]);
            return true;
        }

        private SyntaxToken GetFreeSymbolNameAndMarkUsed(string prefix)
        {
            var freeToken = _semanticFacts.GenerateUniqueName(_semanticModel, _source, container: null, baseName: prefix, _introducedLocalNames, _cancellationToken);
            _introducedLocalNames.Add(freeToken.ValueText);
            return freeToken;
        }

        private bool TryConvertIfInForEach(
            ForEachStatementSyntax forEachStatement,
            QueryExpressionProcessingInfo queryExpressionProcessingInfo,
            out DocumentUpdateInfo documentUpdateInfo)
        {
            // before foreach(var x in from a in b select a)

            if (forEachStatement.Expression.WalkDownParentheses() != _source)
            {
                documentUpdateInfo = null;
                return false;
            }

            // check that the body of the forEach does not contain any identifiers from the query
            foreach (var identifierName in queryExpressionProcessingInfo.IdentifierNames)
            {
                // Identifier from the foreach can already be in scope of the foreach statement.
                if (forEachStatement.Identifier.ValueText != identifierName)
                {
                    if (_semanticFacts.GenerateUniqueName(
                            _semanticModel,
                            location: forEachStatement.Statement,
                            container: forEachStatement.Statement,
                            baseName: identifierName,
                            usedNames: [],
                            _cancellationToken).ValueText != identifierName)
                    {
                        documentUpdateInfo = null;
                        return false;
                    }
                }
            }

            // If query does not contains identifier with the same name as declared in the foreach,
            // declare this identifier in the body.
            if (!queryExpressionProcessingInfo.ContainsIdentifier(forEachStatement.Identifier))
            {
                documentUpdateInfo = ConvertIfInToForeachWithExtraVariableDeclaration(forEachStatement, queryExpressionProcessingInfo);
                return true;
            }
            else
            {
                // The last select expression in the query returns this identifier:
                // foreach(var thisIdentifier in from ....... select thisIdentifier)
                // if thisIdentifier in foreach is var, it is OK
                // if a type is specified for thisIdentifier in forEach, check that the type is the same as in the select expression
                // foreach(MyType thisIdentifier in from ....... from MyType thisIdentifier ... select thisIdentifier)

                // The last clause in query stack must be SelectClauseSyntax.
                var lastSelectExpression = ((SelectClauseSyntax)queryExpressionProcessingInfo.Stack.Peek()).Expression;
                if (lastSelectExpression is IdentifierNameSyntax identifierName &&
                    forEachStatement.Identifier.ValueText == identifierName.Identifier.ValueText &&
                    queryExpressionProcessingInfo.IdentifierNames.Contains(identifierName.Identifier.ValueText))
                {
                    var forEachStatementTypeSymbolType = _semanticModel.GetTypeInfo(forEachStatement.Type, _cancellationToken).Type;
                    var lastSelectExpressionTypeInfo = _semanticModel.GetTypeInfo(lastSelectExpression, _cancellationToken);
                    if (Equals(lastSelectExpressionTypeInfo.ConvertedType, lastSelectExpressionTypeInfo.Type) &&
                        Equals(lastSelectExpressionTypeInfo.ConvertedType, forEachStatementTypeSymbolType))
                    {
                        documentUpdateInfo = ConvertIfInToForeachWithoutExtraVariableDeclaration(forEachStatement, queryExpressionProcessingInfo);
                        return true;
                    }
                }
            }

            // in all other cases try to replace with a local function - this is called above.
            documentUpdateInfo = null;
            return false;
        }

        private DocumentUpdateInfo ConvertIfInToForeachWithExtraVariableDeclaration(
            ForEachStatementSyntax forEachStatement,
            QueryExpressionProcessingInfo queryExpressionProcessingInfo)
        {
            // before foreach(var x in from ... a) { dosomething(x); }
            // after 
            // foreach (var a in ...)
            // ...
            // {
            //      var x = a;
            //      dosomething(x); 
            //  }
            var statements = GenerateStatements(
                expression => AddToBlockTop(LocalDeclarationStatement(
                    VariableDeclaration(
                        forEachStatement.Type,
                        [VariableDeclarator(
                            forEachStatement.Identifier,
                            argumentList: null,
                            EqualsValueClause(expression))])),
                                forEachStatement.Statement).WithAdditionalAnnotations(Formatter.Annotation), queryExpressionProcessingInfo);
            return new DocumentUpdateInfo(forEachStatement, statements);
        }

        private DocumentUpdateInfo ConvertIfInToForeachWithoutExtraVariableDeclaration(
            ForEachStatementSyntax forEachStatement,
            QueryExpressionProcessingInfo queryExpressionProcessingInfo)
        {
            // before 
            //  foreach (var a in from a in b where a > 5 select a) 
            //  { 
            //      dosomething(a); 
            //  }
            // after 
            //  foreach (var a in b)
            //  {
            //      if (a > 5)
            //      {
            //          dosomething(a); 
            //      }
            //  }
            var statements = GenerateStatements(
                expression => forEachStatement.Statement.WithAdditionalAnnotations(Formatter.Annotation),
                queryExpressionProcessingInfo);
            return new DocumentUpdateInfo(forEachStatement, statements);
        }

        private bool TryConvertIfInReturnStatement(
            ReturnStatementSyntax returnStatement,
            QueryExpressionProcessingInfo queryExpressionProcessingInfo,
            out DocumentUpdateInfo documentUpdateInfo)
        {
            // The conversion requires yield return which cannot be added to lambdas and anonymous method declarations.
            if (IsWithinImmediateLambdaOrAnonymousMethod(returnStatement))
            {
                documentUpdateInfo = null;
                return false;
            }

            var memberDeclarationNode = FindParentMemberDeclarationNode(returnStatement, out var declaredSymbol);
            if (declaredSymbol is not IMethodSymbol methodSymbol)
            {
                documentUpdateInfo = null;
                return false;
            }

            if (methodSymbol.ReturnType.OriginalDefinition?.SpecialType != SpecialType.System_Collections_Generic_IEnumerable_T)
            {
                documentUpdateInfo = null;
                return false;
            }

            // if there are more than one return in the method, convert to local function.
            if (memberDeclarationNode.DescendantNodes().OfType<ReturnStatementSyntax>().Count() == 1)
            {
                // before: return from a in b select a;
                // after: 
                // foreach(var a in b)
                // {
                //      yield return a;
                // }
                //
                // yield break;
                var statements = GenerateStatements(expression => YieldStatement(SyntaxKind.YieldReturnStatement, expression), queryExpressionProcessingInfo);

                // add an yield break to avoid throws after the return.
                var yieldBreakStatement = YieldStatement(SyntaxKind.YieldBreakStatement);
                documentUpdateInfo = new DocumentUpdateInfo(returnStatement, statements.Concat([yieldBreakStatement]));
                return true;
            }

            documentUpdateInfo = null;
            return false;
        }

        // We may assume that the query is defined within a method, field, property and so on and it is declare just once.
        private SyntaxNode FindParentMemberDeclarationNode(SyntaxNode node, out ISymbol declaredSymbol)
        {
            declaredSymbol = _semanticModel.GetEnclosingSymbol(node.SpanStart, _cancellationToken);
            return declaredSymbol.DeclaringSyntaxReferences.Single().GetSyntax();
        }

        private bool TryCreateStackFromQueryExpression(out QueryExpressionProcessingInfo queryExpressionProcessingInfo)
        {
            queryExpressionProcessingInfo = new QueryExpressionProcessingInfo(_source.FromClause);
            return TryProcessQueryBody(_source.Body, queryExpressionProcessingInfo);
        }

        private StatementSyntax[] GenerateStatements(
            Func<ExpressionSyntax, StatementSyntax> leafExpressionCreationMethod,
            QueryExpressionProcessingInfo queryExpressionProcessingInfo)
        {
            StatementSyntax statement = null;
            var stack = queryExpressionProcessingInfo.Stack;
            // Executes syntax building methods from bottom to the top of the tree.
            // Process last clause
            if (stack.Any())
            {
                var node = stack.Pop();
                if (node is SelectClauseSyntax selectClause)
                {
                    statement = WrapWithBlock(leafExpressionCreationMethod(selectClause.Expression));
                }
                else
                {
                    throw new ArgumentException("Last node must me the select clause");
                }
            }

            // Process all other clauses
            var statements = new List<StatementSyntax>();
            while (stack.Any())
            {
                statement = ProcessClause(
                    stack.Pop(),
                    statement,
                    isLastClause: !stack.Any(),
                    hasExtraDeclarations: statements.Any(),
                    out var extraStatement);
                if (extraStatement != null)
                {
                    statements.Add(extraStatement);
                }
            }

            // The stack was processed in the reverse order, but the extra statements should be provided in the direct order.
            statements.Reverse();
            statements.Add(statement.WithAdditionalAnnotations(Simplifier.Annotation));
            return [.. statements];
        }

        private bool TryProcessQueryBody(QueryBodySyntax queryBody, QueryExpressionProcessingInfo queryExpressionProcessingInfo)
        {
            do
            {
                foreach (var queryClause in queryBody.Clauses)
                {
                    switch (queryClause.Kind())
                    {
                        case SyntaxKind.WhereClause:
                            queryExpressionProcessingInfo.Add(queryClause);
                            break;
                        case SyntaxKind.LetClause:
                            if (!queryExpressionProcessingInfo.TryAdd(queryClause, ((LetClauseSyntax)queryClause).Identifier))
                            {
                                return false;
                            }

                            break;
                        case SyntaxKind.FromClause:
                            var fromClause = (FromClauseSyntax)queryClause;
                            if (!queryExpressionProcessingInfo.TryAdd(queryClause, fromClause.Identifier))
                            {
                                return false;
                            }

                            break;
                        case SyntaxKind.JoinClause:
                            var joinClause = (JoinClauseSyntax)queryClause;
                            if (joinClause.Into == null) // GroupJoin is not supported
                            {
                                if (queryExpressionProcessingInfo.TryAdd(joinClause, joinClause.Identifier))
                                {
                                    break;
                                }
                            }

                            return false;
                        // OrderBy is not supported by foreach.
                        default:
                            return false;
                    }
                }

                // GroupClause is not supported by the conversion
                if (queryBody.SelectOrGroup is not SelectClauseSyntax selectClause)
                {
                    return false;
                }

                if (_semanticModel.GetTypeInfo(selectClause.Expression, _cancellationToken).Type.ContainsAnonymousType())
                {
                    return false;
                }

                queryExpressionProcessingInfo.Add(selectClause);
                queryBody = queryBody.Continuation?.Body;
            } while (queryBody != null);

            return true;
        }

        private static BlockSyntax AddToBlockTop(StatementSyntax newStatement, StatementSyntax statement)
        {
            if (statement is BlockSyntax block)
            {
                return block.WithStatements(block.Statements.Insert(0, newStatement));
            }
            else
            {
                return Block(newStatement, statement);
            }
        }

        private bool IsLocalOrParameterSymbol(ExpressionSyntax expression)
            => IsLocalOrParameterSymbol(_semanticModel.GetOperation(expression, _cancellationToken));

        private static bool IsLocalOrParameterSymbol(IOperation operation)
        {
            if (operation is IConversionOperation conversion && conversion.IsImplicit)
            {
                return IsLocalOrParameterSymbol(conversion.Operand);
            }

            return operation.Kind is OperationKind.LocalReference or OperationKind.ParameterReference;
        }

        private static BlockSyntax WrapWithBlock(StatementSyntax statement)
            => statement is BlockSyntax block ? block : Block(statement);

        // Checks if the node is within an immediate lambda or within an immediate anonymous method.
        // 'lambda => node' returns true
        // 'lambda => localfunction => node' returns false
        // 'member => node' returns false
        private static bool IsWithinImmediateLambdaOrAnonymousMethod(SyntaxNode node)
        {
            while (node != null)
            {
                switch (node.Kind())
                {
                    case SyntaxKind.AnonymousMethodExpression:
                    case SyntaxKind.ParenthesizedLambdaExpression:
                    case SyntaxKind.SimpleLambdaExpression:
                        return true;
                    case SyntaxKind.LocalFunctionStatement:
                        return false;
                    default:
                        if (node is MemberDeclarationSyntax)
                        {
                            return false;
                        }

                        break;
                }

                node = node.Parent;
            }

            return false;
        }

        private sealed class QueryExpressionProcessingInfo
        {
            public Stack<CSharpSyntaxNode> Stack { get; private set; }

            public HashSet<string> IdentifierNames { get; private set; }

            public QueryExpressionProcessingInfo(FromClauseSyntax fromClause)
            {
                Stack = new Stack<CSharpSyntaxNode>();
                Stack.Push(fromClause);
                IdentifierNames = [fromClause.Identifier.ValueText];
            }

            public bool TryAdd(CSharpSyntaxNode node, SyntaxToken identifier)
            {
                // Duplicate identifiers are not allowed.
                // var q = from x in new[] { 1 } select x + 2 into x where x > 0 select 7 into y let x = ""aaa"" select x;
                if (!IdentifierNames.Add(identifier.ValueText))
                {
                    return false;
                }

                Stack.Push(node);
                return true;
            }

            public void Add(CSharpSyntaxNode node) => Stack.Push(node);

            public bool ContainsIdentifier(SyntaxToken identifier)
                => IdentifierNames.Contains(identifier.ValueText);
        }
    }
}
