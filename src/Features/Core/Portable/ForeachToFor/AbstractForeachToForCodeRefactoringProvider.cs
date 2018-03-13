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
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ForeachToFor
{
    internal abstract class AbstractForeachToForCodeRefactoringProvider
        : CodeRefactoringProvider
    {
        private const string get_Count = nameof(get_Count);
        private const string get_Item = nameof(get_Item);

        private const string Length = nameof(Length);
        private const string Count = nameof(Count);

        private static readonly ImmutableArray<string> s_KnownInterfaceNames =
            new string[] { typeof(IList<>).FullName, typeof(IReadOnlyList<>).FullName, typeof(IList).FullName }.ToImmutableArray();

        protected abstract SyntaxNode GetForeachStatement(SyntaxToken token);
        protected abstract (SyntaxNode start, SyntaxNode end) GetForeachBody(SyntaxNode foreachStatement);
        protected abstract void ConvertToForStatement(SemanticModel model, ForeachInfo info, SyntaxEditor editor, CancellationToken cancellationToken);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(context.Span.Start);

            var foreachStatement = GetForeachStatement(token);
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

            context.RegisterRefactoring(
                new ForeachToForCodeAction(
                    FeaturesResources.Convert_foreach_to_for,
                    c => ConvertForeachToForAsync(document, foreachInfo, c)));
        }

        protected string CreateUniqueMethodName(SemanticModel model, SyntaxNode contextNode, string baseName)
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
            if (token == default(SyntaxToken))
            {
                return node;
            }

            return node.ReplaceToken(token, token.WithAdditionalAnnotations(RenameAnnotation.Create()));
        }

        private ForeachInfo GetForeachInfo(ISemanticFactsService semanticFact, SemanticModel model, SyntaxNode foreachStatement, CancellationToken cancellationToken)
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

            if (!CheckForeachVariable(model, foreachVariable, foreachStatement))
            {
                return null;
            }

            var foreachCollection = RemoveImplicitConversion(operation.Collection);
            if (foreachCollection == null)
            {
                return null;
            }

            string explicitCast;
            string countName;
            GetInterfaceInfo(semanticFact, model, foreachVariable, foreachCollection, out explicitCast, out countName);
            if (countName == null)
            {
                return null;
            }

            var requireCollectionStatement = CheckRequireCollectionStatement(foreachCollection);
            return new ForeachInfo(countName, explicitCast, requireCollectionStatement, foreachStatement);
        }

        private static void GetInterfaceInfo(
            ISemanticFactsService semanticFact, SemanticModel model, ILocalSymbol foreachVariable, IOperation foreachCollection, out string explicitCast, out string countName)
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
            string explicitInterface = null;
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
                    explicitInterface = current.ToDisplayString();
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

            foreach (var current in interfaceType.AllInterfaces)
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
            return operation.Kind != OperationKind.LocalReference && operation.Kind != OperationKind.FieldReference;
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
            var (start, end) = GetForeachBody(foreachStatement);
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
            ForeachInfo foreachInfo,
            CancellationToken cancellationToken)
        {
            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var workspace = document.Project.Solution.Workspace;
            var editor = new SyntaxEditor(model.SyntaxTree.GetRoot(cancellationToken), workspace);

            ConvertToForStatement(model, foreachInfo, editor, cancellationToken);

            var newRoot = editor.GetChangedRoot();
            return document.WithSyntaxRoot(newRoot);
        }

        protected class ForeachInfo
        {
            public ForeachInfo(
                string countName, string explicitCastInterface, bool requireCollectionStatement, SyntaxNode foreachStatement)
            {
                CountName = countName;

                ExplicitCastInterface = explicitCastInterface;
                RequireExplicitCast = explicitCastInterface != null;
                RequireCollectionStatement = requireCollectionStatement || RequireExplicitCast;

                ForeachStatement = foreachStatement;
            }

            public string CountName { get; }

            public bool RequireExplicitCast { get; }
            public string ExplicitCastInterface { get; internal set; }
            public bool RequireCollectionStatement { get; }
            public SyntaxNode ForeachStatement { get; }

            public SyntaxNode GetCurrentForeachStatement(SyntaxEditor editor)
            {
                return editor.GetChangedRoot().GetCurrentNode(ForeachStatement);
            }
        }

        private class ForeachToForCodeAction : CodeAction.DocumentChangeAction
        {
            public ForeachToForCodeAction(
                string title,
                Func<CancellationToken, Task<Document>> createChangedDocument) : base(title, createChangedDocument)
            {
            }
        }
    }
}
