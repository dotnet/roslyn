// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Semantics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.InitializeParameter
{
    internal abstract partial class AbstractInitializeParameterCodeRefactoringProvider<
        TParameterSyntax,
        TMemberDeclarationSyntax,
        TStatementSyntax,
        TExpressionSyntax,
        TBinaryExpressionSyntax> : CodeRefactoringProvider
        where TParameterSyntax : SyntaxNode
        where TMemberDeclarationSyntax : SyntaxNode
        where TStatementSyntax : SyntaxNode
        where TExpressionSyntax : SyntaxNode
        where TBinaryExpressionSyntax : TExpressionSyntax
    {
        // Standard field/property names we look for when we have a parameter with a given name.
        private static readonly ImmutableArray<NamingRule> s_builtInRules = ImmutableArray.Create(
                new NamingRule(new SymbolSpecification(
                    Guid.NewGuid(), "Property",
                    ImmutableArray.Create(new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Property))),
                    new NamingStyles.NamingStyle(Guid.NewGuid(), capitalizationScheme: Capitalization.PascalCase),
                    enforcementLevel: DiagnosticSeverity.Hidden),
                new NamingRule(new SymbolSpecification(
                    Guid.NewGuid(), "Field",
                    ImmutableArray.Create(new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Field))),
                    new NamingStyles.NamingStyle(Guid.NewGuid(), capitalizationScheme: Capitalization.CamelCase),
                    enforcementLevel: DiagnosticSeverity.Hidden),
                new NamingRule(new SymbolSpecification(
                    Guid.NewGuid(), "FieldWithUnderscore",
                    ImmutableArray.Create(new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Field))),
                    new NamingStyles.NamingStyle(Guid.NewGuid(), prefix: "_", capitalizationScheme: Capitalization.CamelCase),
                    enforcementLevel: DiagnosticSeverity.Hidden));

        private async Task<ImmutableArray<CodeAction>> GetMemberCreationAndInitializationRefactoringsAsync(
            Document document, IParameterSymbol parameter, IBlockStatement blockStatement, CancellationToken cancellationToken)
        {
            var methodSymbol = parameter.ContainingSymbol as IMethodSymbol;
            if (methodSymbol == null || methodSymbol.MethodKind != MethodKind.Constructor)
            {
                return ImmutableArray<CodeAction>.Empty;
            }

            var assignmentStatement = TryFindFieldOrPropertyAssignmentStatement(
                parameter, blockStatement);
            if (assignmentStatement != null)
            {
                // We're already assigning this parameter to a field/property in this type.
                // So there's nothing more for us to do.
                return ImmutableArray<CodeAction>.Empty;
            }

            // Haven't initialized any fields/properties with this parameter.  Offer to assign
            // to an existing matching field/prop if we can find one, or add a new field/prop
            // if we can't.

            var symbol = await TryFindMatchingUninitializedMemberSymbolAsync(
                document, parameter, blockStatement, cancellationToken).ConfigureAwait(false);

            if (symbol != null)
            {
                var resource = symbol.Kind == SymbolKind.Field
                    ? FeaturesResources.Initialize_field_0
                    : FeaturesResources.Initialize_property_0;

                var title = string.Format(resource, symbol.Name);

                return ImmutableArray.Create<CodeAction>(new MyCodeAction(
                    title,
                    c => AddSymbolInitializationAsync(document, parameter, blockStatement, symbol, c)));
            }
            else
            {
                // await RegisterMemberCreationRefactoringsAsync().ConfigureAwait(false);
            }

            return ImmutableArray<CodeAction>.Empty;
        }

        private async Task<Document> AddSymbolInitializationAsync(
            Document document, IParameterSymbol parameter, IBlockStatement blockStatement,
            ISymbol fieldOrProperty, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var statementToAddAfterOpt = TryGetStatementToAddInitializationAfter(
                semanticModel, parameter, blockStatement, cancellationToken);

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var editor = new SyntaxEditor(root, document.Project.Solution.Workspace);
            var generator = editor.Generator;

            var initializationStatement = (TStatementSyntax)generator.ExpressionStatement(
                generator.AssignmentStatement(
                    generator.MemberAccessExpression(
                        generator.ThisExpression(),
                        generator.IdentifierName(fieldOrProperty.Name)),
                    generator.IdentifierName(parameter.Name)));

            InsertStatement(editor, blockStatement.Syntax, statementToAddAfterOpt, initializationStatement);

            return document.WithSyntaxRoot(editor.GetChangedRoot());
        }

        private static bool IsParameterReferenceOrCoalesceOfParameterReference(
            IAssignmentExpression assignmentExpression, IParameterSymbol parameter)
        {
            if (IsParameterReference(assignmentExpression.Value, parameter))
            {
                // We already have a member initialized with this parameter like:
                //      this.field = parameter
                return true;
            }

            if (UnwrapConversion(assignmentExpression.Value) is INullCoalescingExpression coalesceExpression &&
                IsParameterReference(coalesceExpression.PrimaryOperand, parameter))
            {
                // We already have a member initialized with this parameter like:
                //      this.field = parameter ?? ...
                return true;
            }

            return false;
        }

        private IOperation TryGetStatementToAddInitializationAfter(
            SemanticModel semanticModel,
            IParameterSymbol parameter,
            IBlockStatement blockStatement,
            CancellationToken cancellationToken)
        {
            var methodSymbol = (IMethodSymbol)parameter.ContainingSymbol;

            var parameterIndex = methodSymbol.Parameters.IndexOf(parameter);

                // look for an existing assignment for a parameter that comes before us.
                // If we find one, we'll add ourselves after that parameter check.
                for (var i = parameterIndex - 1; i >= 0; i--)
                {
                    var statement = TryFindFieldOrPropertyAssignmentStatement(
                        methodSymbol.Parameters[i], blockStatement);
                    if (statement != null)
                    {
                        return statement;
                    }
                }

            // look for an existing check for a parameter that comes before us.
            // If we find one, we'll add ourselves after that parameter check.
            for (var i = parameterIndex + 1; i < methodSymbol.Parameters.Length; i++)
            {
                var statement = TryFindFieldOrPropertyAssignmentStatement(
                    methodSymbol.Parameters[i], blockStatement);
                if (statement != null)
                {
                    var statementIndex = blockStatement.Statements.IndexOf(statement);
                    return statementIndex > 0 ? blockStatement.Statements[statementIndex - 1] : null;
                }
            }

            return null;
        }

        private IOperation TryFindFieldOrPropertyAssignmentStatement(
            IParameterSymbol parameter, IBlockStatement blockStatement)
        {
            var containingType = parameter.ContainingType;
            foreach (var statement in blockStatement.Statements)
            {
                if (IsFieldOrPropertyAssignment(statement, containingType, out var assignmentExpression) &&
                    IsParameterReferenceOrCoalesceOfParameterReference(assignmentExpression, parameter))
                {
                    return statement;
                }
            }

            return null;
        }

        private async Task<ISymbol> TryFindMatchingUninitializedMemberSymbolAsync(
            Document document, IParameterSymbol parameter, IBlockStatement blockStatement, CancellationToken cancellationToken)
        {
            var rules = await GetNamingRulesAsync(document, cancellationToken).ConfigureAwait(false);
            var parameterWords = GetParameterWordParts(parameter);

            var containingType = parameter.ContainingType;
            var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            foreach (var rule in rules)
            {
                var memberName = rule.NamingStyle.CreateName(parameterWords);
                foreach (var memberWithName in containingType.GetMembers(memberName))
                {
                    if (memberWithName is IFieldSymbol field &&
                        !field.IsConst &&
                        IsImplicitConversion(compilation, source: parameter.Type, destination: field.Type) &&
                        !ContainsFieldAssignment(blockStatement, field))
                    {
                        return field;
                    }

                    if (memberWithName is IPropertySymbol property &&
                        property.IsWritableInConstructor() &&
                        IsImplicitConversion(compilation, source: parameter.Type, destination: property.Type) &&
                        !ContainsPropertyAssignment(blockStatement, property))
                    {
                        return property;
                    }
                }
            }

            return null;
        }

        private bool ContainsFieldAssignment(
            IBlockStatement blockStatement, IFieldSymbol field)
        {
            foreach (var statement in blockStatement.Statements)
            {
                if (IsFieldOrPropertyAssignment(statement, field.ContainingType, out var assignmentExpression) &&
                    UnwrapConversion(assignmentExpression.Target) is IFieldReferenceExpression fieldReference &&
                    field.Equals(fieldReference.Field))
                {
                    return true;
                }
            }

            return false;
        }

        private bool ContainsPropertyAssignment(
            IBlockStatement blockStatement, IPropertySymbol property)
        {
            foreach (var statement in blockStatement.Statements)
            {
                if (IsFieldOrPropertyAssignment(statement, property.ContainingType, out var assignmentExpression) &&
                    UnwrapConversion(assignmentExpression.Target) is IPropertyReferenceExpression propertyReference &&
                    property.Equals(propertyReference.Property))
                {
                    return true;
                }
            }

            return false;
        }

        private async Task<ImmutableArray<NamingRule>> GetNamingRulesAsync(
            Document document, CancellationToken cancellationToken)
        {
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var namingStyleOptions = options.GetOption(SimplificationOptions.NamingPreferences);

            var rules = namingStyleOptions.CreateRules().NamingRules.AddRange(s_builtInRules);
            return rules;
        }

        private List<string> GetParameterWordParts(IParameterSymbol parameter)
        {
            var parameterWordParts = StringBreaker.BreakIntoWordParts(parameter.Name);
            return CreateWords(parameterWordParts, parameter.Name);
        }

        private List<string> CreateWords(StringBreaks wordBreaks, string name)
        {
            var result = new List<string>(wordBreaks.Count);
            for (int i = 0; i < wordBreaks.Count; i++)
            {
                var br = wordBreaks[i];
                result.Add(name.Substring(br.Start, br.Length));
            }

            return result;
        }
    }
}