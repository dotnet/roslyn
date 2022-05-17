// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ConvertForEachToFor
{
    internal abstract class AbstractConvertForEachToForCodeRefactoringProvider<
        TStatementSyntax,
        TForEachStatement> : CodeRefactoringProvider
        where TStatementSyntax : SyntaxNode
        where TForEachStatement : TStatementSyntax
    {
        private const string get_Count = nameof(get_Count);
        private const string get_Item = nameof(get_Item);

        private const string Length = nameof(Array.Length);
        private const string Count = nameof(IList.Count);

        private static readonly ImmutableArray<string> s_KnownInterfaceNames =
            ImmutableArray.Create(typeof(IList<>).FullName!, typeof(IReadOnlyList<>).FullName!, typeof(IList).FullName!);

        protected bool IsForEachVariableWrittenInside { get; private set; }
        protected abstract string Title { get; }
        protected abstract bool ValidLocation(ForEachInfo foreachInfo);
        protected abstract (SyntaxNode start, SyntaxNode end) GetForEachBody(TForEachStatement foreachStatement);
        protected abstract void ConvertToForStatement(
            SemanticModel model, ForEachInfo info, SyntaxEditor editor, CancellationToken cancellationToken);
        protected abstract bool IsValid(TForEachStatement foreachNode);

        /// <summary>
        /// Perform language specific checks if the conversion is supported.
        /// C#: Currently nothing blocking a conversion
        /// VB: Nested foreach loops sharing a single Next statement, Next statements with multiple variables and next statements
        /// not using the loop variable are not supported.
        /// </summary>
        protected abstract bool IsSupported(ILocalSymbol foreachVariable, IForEachLoopOperation forEachOperation, TForEachStatement foreachStatement);

        protected static SyntaxAnnotation CreateWarningAnnotation()
            => WarningAnnotation.Create(FeaturesResources.Warning_colon_semantics_may_change_when_converting_statement);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, _, cancellationToken) = context;
            var foreachStatement = await context.TryGetRelevantNodeAsync<TForEachStatement>().ConfigureAwait(false);
            if (foreachStatement == null || !IsValid(foreachStatement))
            {
                return;
            }

            var model = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var semanticFact = document.GetRequiredLanguageService<ISemanticFactsService>();
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var foreachInfo = GetForeachInfo(semanticFact, model, foreachStatement, cancellationToken);
            if (foreachInfo == null || !ValidLocation(foreachInfo))
            {
                return;
            }

            context.RegisterRefactoring(
                CodeAction.Create(
                    Title,
                    c => ConvertForeachToForAsync(document, foreachInfo, c),
                    Title),
                foreachStatement.Span);
        }

        protected static SyntaxToken CreateUniqueName(
            ISemanticFactsService semanticFacts, SemanticModel model, SyntaxNode location, string baseName, CancellationToken cancellationToken)
            => semanticFacts.GenerateUniqueLocalName(model, location, containerOpt: null, baseName, cancellationToken);

        protected static SyntaxNode GetCollectionVariableName(
            SemanticModel model, SyntaxGenerator generator,
            ForEachInfo foreachInfo, SyntaxNode foreachCollectionExpression, CancellationToken cancellationToken)
        {
            if (foreachInfo.RequireCollectionStatement)
            {
                return generator.IdentifierName(
                    CreateUniqueName(foreachInfo.SemanticFacts,
                        model, foreachInfo.ForEachStatement, foreachInfo.CollectionNameSuggestion, cancellationToken));
            }

            return foreachCollectionExpression.WithoutTrivia().WithAdditionalAnnotations(Formatter.Annotation);
        }

        protected static void IntroduceCollectionStatement(
            ForEachInfo foreachInfo, SyntaxEditor editor,
            SyntaxNode type, SyntaxNode foreachCollectionExpression, SyntaxNode collectionVariable)
        {
            if (!foreachInfo.RequireCollectionStatement)
            {
                return;
            }

            // TODO: refactor introduce variable refactoring to real service and use that service here to introduce local variable
            var generator = editor.Generator;

            // attach rename annotation to control variable
            var collectionVariableToken = generator.Identifier(collectionVariable.ToString()).WithAdditionalAnnotations(RenameAnnotation.Create());

            // this expression is from user code. don't simplify this.
            var expression = foreachCollectionExpression.WithoutAnnotations(SimplificationHelpers.DontSimplifyAnnotation);
            var collectionStatement = generator.LocalDeclarationStatement(
                type,
                collectionVariableToken,
                (foreachInfo.ExplicitCastInterface != null) ? generator.CastExpression(foreachInfo.ExplicitCastInterface, expression) : expression);

            // attach trivia to right place
            collectionStatement = collectionStatement.WithLeadingTrivia(foreachInfo.ForEachStatement.GetFirstToken().LeadingTrivia);

            editor.InsertBefore(foreachInfo.ForEachStatement, collectionStatement);
        }

        protected static TStatementSyntax AddItemVariableDeclaration(
            SyntaxGenerator generator, SyntaxNode type, SyntaxToken foreachVariable,
            ITypeSymbol castType, SyntaxNode collectionVariable, SyntaxToken indexVariable)
        {
            var memberAccess = generator.ElementAccessExpression(
                    collectionVariable, generator.IdentifierName(indexVariable));

            if (castType != null)
            {
                memberAccess = generator.CastExpression(castType, memberAccess);
            }

            var localDecl = generator.LocalDeclarationStatement(
                type, foreachVariable, memberAccess);

            return (TStatementSyntax)localDecl.WithAdditionalAnnotations(Formatter.Annotation);
        }

        private ForEachInfo? GetForeachInfo(
            ISemanticFactsService semanticFact, SemanticModel model,
            TForEachStatement foreachStatement, CancellationToken cancellationToken)
        {
            if (model.GetOperation(foreachStatement, cancellationToken) is not IForEachLoopOperation operation || operation.Locals.Length != 1)
            {
                return null;
            }

            var foreachVariable = operation.Locals[0];
            if (foreachVariable == null)
            {
                return null;
            }

            // Perform language specific checks if the foreachStatement
            // is using unsupported features
            if (!IsSupported(foreachVariable, operation, foreachStatement))
            {
                return null;
            }

            IsForEachVariableWrittenInside = CheckIfForEachVariableIsWrittenInside(model, foreachVariable, foreachStatement);

            var foreachCollection = RemoveImplicitConversion(operation.Collection);
            if (foreachCollection == null)
            {
                return null;
            }

            GetInterfaceInfo(model, foreachVariable, foreachCollection,
                out var explicitCastInterface, out var collectionNameSuggestion, out var countName);
            if (collectionNameSuggestion == null || countName == null)
            {
                return null;
            }

            var requireCollectionStatement = CheckRequireCollectionStatement(foreachCollection);
            return new ForEachInfo(
                semanticFact, collectionNameSuggestion, countName, explicitCastInterface,
                foreachVariable.Type, requireCollectionStatement, foreachStatement);
        }

        private static void GetInterfaceInfo(
            SemanticModel model, ILocalSymbol foreachVariable, IOperation foreachCollection,
            out ITypeSymbol? explicitCastInterface, out string? collectionNameSuggestion, out string? countName)
        {
            explicitCastInterface = null;
            collectionNameSuggestion = null;
            countName = null;

            // go through list of types and interfaces to find out right set;
            var foreachType = foreachVariable.Type;
            if (IsNullOrErrorType(foreachType))
            {
                return;
            }

            var collectionType = foreachCollection.Type;
            if (IsNullOrErrorType(collectionType))
            {
                return;
            }

            // go through explicit types first.

            // check array case
            if (collectionType is IArrayTypeSymbol array)
            {
                if (array.Rank != 1)
                {
                    // array type supports IList and other interfaces, but implementation
                    // only supports Rank == 1 case. other case, it will throw on runtime
                    // even if there is no error on compile time.
                    // we explicitly mark that we only support Rank == 1 case
                    return;
                }

                if (!IsExchangable(array.ElementType, foreachType, model.Compilation))
                {
                    return;
                }

                collectionNameSuggestion = "array";
                explicitCastInterface = null;
                countName = Length;
                return;
            }

            // check string case
            if (collectionType.SpecialType == SpecialType.System_String)
            {
                var charType = model.Compilation.GetSpecialType(SpecialType.System_Char);
                if (!IsExchangable(charType, foreachType, model.Compilation))
                {
                    return;
                }

                collectionNameSuggestion = "str";
                explicitCastInterface = null;
                countName = Length;
                return;
            }

            // check ImmutableArray case 
            if (collectionType.OriginalDefinition.Equals(model.Compilation.GetTypeByMetadataName(typeof(ImmutableArray<>).FullName!)))
            {
                var indexer = GetInterfaceMember(collectionType, get_Item);
                if (indexer != null)
                {
                    if (!IsExchangable(indexer.ReturnType, foreachType, model.Compilation))
                    {
                        return;
                    }

                    collectionNameSuggestion = "array";
                    explicitCastInterface = null;
                    countName = Length;
                    return;
                }
            }

            // go through all known interfaces we support next.
            var knownCollectionInterfaces = s_KnownInterfaceNames.Select(
                s => model.Compilation.GetTypeByMetadataName(s)).Where(t => !IsNullOrErrorType(t));

            // for all interfaces, we suggest collection name as "list"
            collectionNameSuggestion = "list";

            // check type itself is interface case
            if (collectionType.TypeKind == TypeKind.Interface && knownCollectionInterfaces.Contains(collectionType.OriginalDefinition))
            {
                var indexer = GetInterfaceMember(collectionType, get_Item);
                if (indexer != null &&
                    IsExchangable(indexer.ReturnType, foreachType, model.Compilation))
                {
                    explicitCastInterface = null;
                    countName = Count;
                    return;
                }
            }

            // check regular cases (implicitly implemented)
            ITypeSymbol? explicitInterface = null;
            foreach (var current in collectionType.AllInterfaces)
            {
                if (!knownCollectionInterfaces.Contains(current.OriginalDefinition))
                {
                    continue;
                }

                // see how the type implements the interface
                var countSymbol = GetInterfaceMember(current, get_Count);
                var indexerSymbol = GetInterfaceMember(current, get_Item);
                if (countSymbol == null || indexerSymbol == null)
                {
                    continue;
                }

                if (collectionType.FindImplementationForInterfaceMember(countSymbol) is not IMethodSymbol countImpl ||
                    collectionType.FindImplementationForInterfaceMember(indexerSymbol) is not IMethodSymbol indexerImpl)
                {
                    continue;
                }

                if (!IsExchangable(indexerImpl.ReturnType, foreachType, model.Compilation))
                {
                    continue;
                }

                // implicitly implemented!
                if (countImpl.ExplicitInterfaceImplementations.IsEmpty &&
                    indexerImpl.ExplicitInterfaceImplementations.IsEmpty)
                {
                    explicitCastInterface = null;
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
                explicitCastInterface = explicitInterface;
                countName = Count;
            }
        }

        private static bool IsExchangable(
            ITypeSymbol type1, ITypeSymbol type2, Compilation compilation)
        {
            return compilation.HasImplicitConversion(type1, type2) ||
                   compilation.HasImplicitConversion(type2, type1);
        }

        private static bool IsNullOrErrorType([NotNullWhen(false)] ITypeSymbol? type)
            => type is null or IErrorTypeSymbol;

        private static IMethodSymbol? GetInterfaceMember(ITypeSymbol interfaceType, string memberName)
        {
            foreach (var current in interfaceType.GetAllInterfacesIncludingThis())
            {
                var members = current.GetMembers(memberName);
                if (!members.IsEmpty && members[0] is IMethodSymbol method)
                {
                    return method;
                }
            }

            return null;
        }

        private static bool CheckRequireCollectionStatement(IOperation operation)
        {
            // this lists type of references in collection part of foreach we will use
            // as it is in
            //    var element = reference[indexer];
            //
            // otherwise, we will introduce local variable for the expression first and then
            // do "foreach to for" refactoring
            //
            // foreach(var a in new int[] {....}) 
            // to
            // var array = new int[] { ... }
            // foreach(var a in array)
            switch (operation.Kind)
            {
                case OperationKind.LocalReference:
                case OperationKind.FieldReference:
                case OperationKind.ParameterReference:
                case OperationKind.PropertyReference:
                case OperationKind.ArrayElementReference:
                    return false;
                default:
                    return true;
            }
        }

        private IOperation RemoveImplicitConversion(IOperation collection)
        {
            return (collection is IConversionOperation conversion && conversion.IsImplicit)
                ? RemoveImplicitConversion(conversion.Operand) : collection;
        }

        private bool CheckIfForEachVariableIsWrittenInside(SemanticModel semanticModel, ISymbol foreachVariable, TForEachStatement foreachStatement)
        {
            var (start, end) = GetForEachBody(foreachStatement);
            if (start == null || end == null)
            {
                // empty body. this can happen in VB
                return false;
            }

            var dataFlow = semanticModel.AnalyzeDataFlow(start, end);

            if (!dataFlow.Succeeded)
            {
                // if we can't get good analysis, assume it is written
                return true;
            }

            return dataFlow.WrittenInside.Contains(foreachVariable);
        }

        private async Task<Document> ConvertForeachToForAsync(
            Document document,
            ForEachInfo foreachInfo,
            CancellationToken cancellationToken)
        {
            var model = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var services = document.Project.Solution.Workspace.Services;
            var editor = new SyntaxEditor(model.SyntaxTree.GetRoot(cancellationToken), services);

            ConvertToForStatement(model, foreachInfo, editor, cancellationToken);

            var newRoot = editor.GetChangedRoot();
            return document.WithSyntaxRoot(newRoot);
        }

        protected sealed class ForEachInfo
        {
            public ForEachInfo(
                ISemanticFactsService semanticFacts, string collectionNameSuggestion, string countName,
                ITypeSymbol? explicitCastInterface, ITypeSymbol forEachElementType,
                bool requireCollectionStatement, TForEachStatement forEachStatement)
            {
                SemanticFacts = semanticFacts;

                CollectionNameSuggestion = collectionNameSuggestion;
                CountName = countName;

                ExplicitCastInterface = explicitCastInterface;
                ForEachElementType = forEachElementType;

                RequireCollectionStatement = requireCollectionStatement || (explicitCastInterface != null);

                ForEachStatement = forEachStatement;
            }

            public ISemanticFactsService SemanticFacts { get; }

            public string CollectionNameSuggestion { get; }
            public string CountName { get; }
            public ITypeSymbol? ExplicitCastInterface { get; }
            public ITypeSymbol ForEachElementType { get; }
            public bool RequireCollectionStatement { get; }
            public TForEachStatement ForEachStatement { get; }
        }
    }
}
