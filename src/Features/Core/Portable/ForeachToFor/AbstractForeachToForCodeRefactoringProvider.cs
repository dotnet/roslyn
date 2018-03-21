// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ForeachToFor
{
    internal abstract class AbstractForEachToForCodeRefactoringProvider
        : CodeRefactoringProvider
    {
        private const string get_Count = nameof(get_Count);
        private const string get_Item = nameof(get_Item);

        private const string Length = nameof(Array.Length);
        private const string Count = nameof(IList.Count);

        private static readonly ImmutableArray<string> s_KnownInterfaceNames =
            new string[] { typeof(IList<>).FullName, typeof(IReadOnlyList<>).FullName, typeof(IList).FullName }.ToImmutableArray();

        protected abstract SyntaxNode GetForEachStatement(TextSpan selelction, SyntaxToken token);
        protected abstract bool ValidLocation(ForEachInfo foreachInfo);
        protected abstract (SyntaxNode start, SyntaxNode end) GetForEachBody(SyntaxNode foreachStatement);
        protected abstract void ConvertToForStatement(SemanticModel model, ForEachInfo info, SyntaxEditor editor, CancellationToken cancellationToken);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(context.Span.Start);

            var foreachStatement = GetForEachStatement(context.Span, token);
            if (foreachStatement == null)
            {
                return;
            }

            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var semanticFact = document.GetLanguageService<ISemanticFactsService>();
            var foreachInfo = GetForeachInfo(semanticFact, model, foreachStatement, cancellationToken);
            if (foreachInfo == null)
            {
                return;
            }

            if (!ValidLocation(foreachInfo))
            {
                return;
            }

            context.RegisterRefactoring(
                new ForEachToForCodeAction(
                    FeaturesResources.Convert_foreach_to_for,
                    c => ConvertForeachToForAsync(document, foreachInfo, c)));
        }

        protected string CreateUniqueName(SemanticModel model, SyntaxNode contextNode, string baseName)
        {
            Contract.ThrowIfNull(contextNode);
            Contract.ThrowIfNull(baseName);

            return NameGenerator.GenerateUniqueName(baseName, string.Empty,
               name => model.LookupSymbols(contextNode.SpanStart, /*container*/null, name).Length == 0);
        }

        protected T AddElasticAnnotation<T>(T node, SyntaxTrivia elasticTrivia) where T : SyntaxNode
        {
            var first = node.GetFirstToken();
            node = node.ReplaceToken(first, first.WithPrependedLeadingTrivia(elasticTrivia));

            var last = node.GetLastToken();
            return node.ReplaceToken(last, last.WithAppendedTrailingTrivia(elasticTrivia));
        }

        protected T AddRenameAnnotation<T>(T node, string name) where T : SyntaxNode
        {
            var token = node.DescendantTokens().FirstOrDefault(t => t.Text == name);
            if (token == default)
            {
                return node;
            }

            return node.ReplaceToken(token, token.WithAdditionalAnnotations(RenameAnnotation.Create()));
        }

        protected string GetCollectionVariableName(SemanticModel model, ForEachInfo foreachInfo, SyntaxNode foreachCollectionExpression)
        {
            if (foreachInfo.RequireCollectionStatement)
            {
                return CreateUniqueName(model, foreachInfo.ForEachStatement, "list");
            }

            return foreachCollectionExpression.ToString();
        }

        protected void IntroduceCollectionStatement(
            SemanticModel model, ForEachInfo foreachInfo, SyntaxEditor editor, SyntaxNode foreachCollectionExpression, string collectionVariableName)
        {
            if (!foreachInfo.RequireCollectionStatement)
            {
                return;
            }

            // TODO: refactor introduce variable refactoring to real service and use that service here to introduce local variable
            var generator = editor.Generator;

            // this expression is from user code. don't simplify this.
            var expression = foreachCollectionExpression.WithoutAnnotations(SimplificationHelpers.DontSimplifyAnnotation);
            var collectionStatement = generator.LocalDeclarationStatement(
                collectionVariableName,
                foreachInfo.RequireExplicitCast
                ? generator.CastExpression(foreachInfo.ExplicitCastInterface, expression) : expression);

            collectionStatement = AddRenameAnnotation(
                collectionStatement.WithLeadingTrivia(foreachInfo.ForEachStatement.GetFirstToken().LeadingTrivia), collectionVariableName);

            editor.InsertBefore(foreachInfo.ForEachStatement, collectionStatement);
        }

        protected SyntaxNode AddItemVariableDeclaration(
            SyntaxGenerator generator, string foreachVariableString, string collectionVariableName, string indexString)
        {
            return generator.LocalDeclarationStatement(
                foreachVariableString,
                generator.ElementAccessExpression(
                    generator.IdentifierName(collectionVariableName), generator.IdentifierName(indexString)));
        }

        private ForEachInfo GetForeachInfo(ISemanticFactsService semanticFact, SemanticModel model, SyntaxNode foreachStatement, CancellationToken cancellationToken)
        {
            var operation = model.GetOperation(foreachStatement, cancellationToken) as IForEachLoopOperation;
            if (operation == null || operation.Locals.Length != 1)
            {
                return null;
            }

            var foreachVariable = operation.Locals[0];
            if (foreachVariable == null)
            {
                return null;
            }

            // VB can have Next variable. but we only support
            // simple 1 variable case.
            if (operation.NextVariables.Length > 1)
            {
                return null;
            }

            if (!operation.NextVariables.IsEmpty)
            {
                var nextVariable = operation.NextVariables[0] as ILocalReferenceOperation;
                if (nextVariable == null || nextVariable.Local?.Equals(foreachVariable) == false)
                {
                    // we do not support anything else than local reference for next variable
                    // operation
                    return null;
                }
            }

            if (!CheckForeachVariable(model, foreachVariable, foreachStatement))
            {
                return null;
            }

            var foreachCollection = RemoveImplicitConversion(operation.Collection);
            if (foreachCollection == null)
            {
                return null;
            }

            GetInterfaceInfo(semanticFact, model, foreachVariable, foreachCollection, out var explicitCast, out var countName);
            if (countName == null)
            {
                return null;
            }

            var requireCollectionStatement = CheckRequireCollectionStatement(foreachCollection);
            return new ForEachInfo(countName, explicitCast, requireCollectionStatement, foreachStatement);
        }

        private static void GetInterfaceInfo(
            ISemanticFactsService semanticFact, SemanticModel model, ILocalSymbol foreachVariable, IOperation foreachCollection,
            out ITypeSymbol explicitCast, out string countName)
        {
            explicitCast = default;
            countName = null;

            // go through list of types to find out right set;
            var foreachType = foreachVariable.Type;
            if (IsNullOrErrorType(foreachType))
            {
                return;
            }

            // check array case first.
            var collectionType = foreachCollection.Type;
            if (IsNullOrErrorType(collectionType))
            {
                return;
            }

            if (collectionType is IArrayTypeSymbol array &&
                array.Rank == 1 &&
                semanticFact.IsAssignableTo(array.ElementType, foreachType, model.Compilation))
            {
                explicitCast = null;
                countName = Length;
                return;
            }

            // check ImmutableArray case second
            if (collectionType.OriginalDefinition.Equals(model.Compilation.GetTypeByMetadataName(typeof(ImmutableArray<>).FullName)))
            {
                var indexer = GetInterfaceMembers(collectionType, get_Item);
                if (indexer != null && semanticFact.IsAssignableTo(indexer.ReturnType, foreachType, model.Compilation))
                {
                    explicitCast = null;
                    countName = Length;
                    return;
                }
            }

            // go through all known interfaces we support
            var knownCollectionInterfaces = s_KnownInterfaceNames.Select(
                s => model.Compilation.GetTypeByMetadataName(s)).WhereNotNull().Where(t => !IsNullOrErrorType(t));

            // check type itself is interface case
            if (collectionType.TypeKind == TypeKind.Interface && knownCollectionInterfaces.Contains(collectionType.OriginalDefinition))
            {
                var indexer = GetInterfaceMembers(collectionType, get_Item);
                if (indexer != null && semanticFact.IsAssignableTo(indexer.ReturnType, foreachType, model.Compilation))
                {
                    explicitCast = null;
                    countName = Count;
                    return;
                }
            }

            // check regular cases (implicitly implemented)
            ITypeSymbol explicitInterface = null;
            foreach (var current in collectionType.AllInterfaces)
            {
                if (!knownCollectionInterfaces.Contains(current.OriginalDefinition))
                {
                    continue;
                }

                // see how the type implements the interface
                var countSymbol = GetInterfaceMembers(current, get_Count);
                var indexerSymbol = GetInterfaceMembers(current, get_Item);
                if (countSymbol == null || indexerSymbol == null)
                {
                    continue;
                }

                var countImpl = collectionType.FindImplementationForInterfaceMember(countSymbol) as IMethodSymbol;
                var indexerImpl = collectionType.FindImplementationForInterfaceMember(indexerSymbol) as IMethodSymbol;
                if (countImpl == null || indexerImpl == null)
                {
                    continue;
                }

                if (!semanticFact.IsAssignableTo(indexerImpl.ReturnType, foreachType, model.Compilation))
                {
                    continue;
                }

                // implicitly implemented!
                if (countImpl.ExplicitInterfaceImplementations.IsEmpty &&
                    indexerImpl.ExplicitInterfaceImplementations.IsEmpty)
                {
                    explicitCast = null;
                    countName = Count;
                    return;
                }

                if (explicitInterface == null)
                {
                    explicitInterface = current;
                }
            }

            // okay, we don't have implicitly implemented one, but we do have explicitly implemented one
            if (explicitInterface != null)
            {
                explicitCast = explicitInterface;
                countName = Count;
            }
        }

        private static bool IsNullOrErrorType(ITypeSymbol type)
        {
            return type == null || type is IErrorTypeSymbol;
        }

        private static IMethodSymbol GetInterfaceMembers(ITypeSymbol interfaceType, string memberName)
        {
            var members = interfaceType.GetMembers(memberName);
            if (!members.IsEmpty)
            {
                return (IMethodSymbol)members[0];
            }

            foreach (var current in interfaceType.Interfaces)
            {
                var member = GetInterfaceMembers(current, memberName);
                if (member != null)
                {
                    return member;
                }
            }

            return null;
        }

        private static bool CheckRequireCollectionStatement(IOperation operation)
        {
            return operation.Kind != OperationKind.LocalReference &&
                   operation.Kind != OperationKind.FieldReference &&
                   operation.Kind != OperationKind.ParameterReference &&
                   operation.Kind != OperationKind.PropertyReference &&
                   operation.Kind != OperationKind.ArrayElementReference;
        }

        private IOperation RemoveImplicitConversion(IOperation collection)
        {
            if (collection is IConversionOperation conversion && conversion.IsImplicit)
            {
                return RemoveImplicitConversion(conversion.Operand);
            }

            return collection;
        }

        private bool CheckForeachVariable(SemanticModel semanticModel, ISymbol foreachVariable, SyntaxNode foreachStatement)
        {
            var (start, end) = GetForEachBody(foreachStatement);
            if (start == null || end == null)
            {
                // empty body. this can happen in VB
                return true;
            }

            var dataFlow = semanticModel.AnalyzeDataFlow(start, end);

            if (!dataFlow.Succeeded)
            {
                // if we can't get good analysis, assume it is written
                return false;
            }

            return !dataFlow.WrittenInside.Contains(foreachVariable);
        }

        private async Task<Document> ConvertForeachToForAsync(
            Document document,
            ForEachInfo foreachInfo,
            CancellationToken cancellationToken)
        {
            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var workspace = document.Project.Solution.Workspace;
            var editor = new SyntaxEditor(model.SyntaxTree.GetRoot(cancellationToken), workspace);

            ConvertToForStatement(model, foreachInfo, editor, cancellationToken);

            var newRoot = editor.GetChangedRoot();
            return document.WithSyntaxRoot(newRoot);
        }

        protected class ForEachInfo
        {
            public ForEachInfo(
                string countName, ITypeSymbol explicitCastInterface, bool requireCollectionStatement, SyntaxNode forEachStatement)
            {
                CountName = countName;

                // order of setting properties is important here
                ExplicitCastInterface = explicitCastInterface;
                RequireCollectionStatement = requireCollectionStatement || RequireExplicitCast;

                ForEachStatement = forEachStatement;
            }

            public bool RequireExplicitCast => ExplicitCastInterface != null;

            public string CountName { get; }
            public ITypeSymbol ExplicitCastInterface { get; }
            public bool RequireCollectionStatement { get; }
            public SyntaxNode ForEachStatement { get; }
        }

        private class ForEachToForCodeAction : CodeAction.DocumentChangeAction
        {
            public ForEachToForCodeAction(
                string title,
                Func<CancellationToken, Task<Document>> createChangedDocument) : base(title, createChangedDocument)
            {
            }
        }
    }
}
