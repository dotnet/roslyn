// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles.SymbolSpecification;

namespace Microsoft.CodeAnalysis.ConvertAutoPropertyToFullProperty
{
    internal abstract class AbstractConvertAutoPropertyToFullPropertyCodeRefactoringProvider 
        : CodeRefactoringProvider
    {
        internal abstract SyntaxNode GetPropertyDeclaration(SyntaxToken token);
        internal abstract bool isAbstract(SyntaxNode property);
        internal abstract bool TryGetEmptyAccessors(SyntaxNode propertyDeclarationSyntax, 
            out SyntaxNode emptyGetAccessor, out SyntaxNode emptySetAccessor);
        internal abstract SyntaxNode UpdateAccessor(SyntaxNode accessor, SyntaxNode[] statements);
        internal abstract Task<SyntaxNode> ConvertToExpressionBodyIfDesiredAsync(Document document, 
            SyntaxNode getAccessor, CancellationToken cancellationToken);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(context.Span.Start);

            var property = GetPropertyDeclaration(token);
            if (property == null)
            {
                return;
            }

            if (isAbstract(property))
            {
                return;
            }

            // check to see if both property accessors exist and are empty
            if (!TryGetEmptyAccessors(property, out var emptyGetAccessor, out var emptySetAccessor))
            {
                return;
            }

            context.RegisterRefactoring(
                new ConvertAutoPropertyToFullPropertyCodeAction(
                    FeaturesResources.Convert_to_full_property,
                    c => ExpandToFullPropertyAsync(
                        document,
                        property,
                        emptyGetAccessor,
                        emptySetAccessor,
                        root,
                        context.CancellationToken)));
        }

        private async Task<Document> ExpandToFullPropertyAsync(
            Document document, 
            SyntaxNode property, 
            SyntaxNode emptyGetAccessor, 
            SyntaxNode emptySetAccessor, 
            SyntaxNode root, 
            CancellationToken cancellationToken)
        {
            // Get the symbol representing the property
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var propertySymbol = semanticModel.GetDeclaredSymbol(property) as IPropertySymbol;

            // generate a name for the new field based on naming preferences
            var rules = await GetNamingRulesAsync(
                document, 
                cancellationToken).ConfigureAwait(false);
            var fieldName = GenerateFieldName(propertySymbol, rules);

            // expand the property and add the field
            var generator = SyntaxGenerator.GetGenerator(document);
            var newRoot = await ExpandPropertyAndAddFieldAsync(
                document, 
                property, 
                emptyGetAccessor, 
                emptySetAccessor, 
                root, 
                propertySymbol, 
                fieldName, 
                generator, 
                cancellationToken).ConfigureAwait(false);

            return document.WithSyntaxRoot(newRoot);
        }

        private static async Task<ImmutableArray<NamingRule>> GetNamingRulesAsync(
            Document document,
            CancellationToken cancellationToken)
        {
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var namingStyleOptions = options.GetOption(SimplificationOptions.NamingPreferences);
            var rules = namingStyleOptions.CreateRules().NamingRules
                .AddRange(GetDefaultRule(ImmutableArray.Create(new ModifierKind(ModifierKindEnum.IsStatic)), "s_"))
                .AddRange(GetDefaultRule(ImmutableArray.Create<ModifierKind>(), "_"));
            return rules;
        }

        private static string GenerateFieldName(
            IPropertySymbol property, 
            ImmutableArray<NamingRule> rules)
        {
            var propertyName = property.Name;
            var fieldName = "";
            var isStatic = property.IsStatic;
            foreach (var rule in rules)
            {
                if (rule.SymbolSpecification.AppliesTo(
                    new SymbolKindOrTypeKind(SymbolKind.Field), 
                    isStatic ? DeclarationModifiers.Static : DeclarationModifiers.None, 
                    Accessibility.Private))
                {
                    fieldName = rule.NamingStyle.MakeCompliant(propertyName).Single();
                    break;
                }
            }

            var uniqueName = NameGenerator.GenerateUniqueName(
                fieldName, n => property.ContainingType.GetMembers(n).IsEmpty);
            return uniqueName;
        }

        private async Task<SyntaxNode> ExpandPropertyAndAddFieldAsync(
            Document document, 
            SyntaxNode property, 
            SyntaxNode getAccessor, 
            SyntaxNode setAccessor, 
            SyntaxNode root, 
            IPropertySymbol propertySymbol, 
            string fieldName, 
            SyntaxGenerator generator, 
            CancellationToken cancellationToken)
        {
            var workspace = document.Project.Solution.Workspace;

            // add statements to existing accessors
            var getAccessorStatements = new SyntaxNode[] {
                generator.ReturnStatement(
                    generator.IdentifierName(fieldName)) };
            var newGetAccessor = await AddStatementsToAccessorAsync(
                document, 
                getAccessor, 
                getAccessorStatements, 
                generator, 
                cancellationToken).ConfigureAwait(false);

            var setAccessorStatements = new SyntaxNode[] {
                generator.ExpressionStatement(generator.AssignmentStatement(
                    generator.IdentifierName(fieldName),
                    generator.IdentifierName("value"))) };
            var newSetAccessor = await AddStatementsToAccessorAsync(
                document, 
                setAccessor, 
                setAccessorStatements, 
                generator, 
                cancellationToken).ConfigureAwait(false);

            var newProperty = generator
                .WithAccessorDeclarations(property, new SyntaxNode[] { newGetAccessor, newSetAccessor})
                .WithAdditionalAnnotations(Formatter.Annotation)
                .WithAdditionalAnnotations(new SyntaxAnnotation("property"));
            newProperty = await Formatter.FormatAsync(newProperty, workspace).ConfigureAwait(false);
            var newRoot = root.ReplaceNode(property, newProperty);

            var newField = generator.FieldDeclaration(
                fieldName, 
                generator.TypeExpression(propertySymbol.Type), 
                Accessibility.Private,
                DeclarationModifiers.From(propertySymbol)).
                    WithAdditionalAnnotations(Formatter.Annotation);
            var newFieldNodes = SpecializedCollections.SingletonEnumerable(newField);
            newProperty = newRoot.GetAnnotatedNodes("property").Single();
            newRoot = newRoot.InsertNodesBefore(newProperty, newFieldNodes);
            return newRoot;
        }

        private async Task<SyntaxNode> AddStatementsToAccessorAsync(
            Document document,  
            SyntaxNode accessor,
            SyntaxNode[] statements,
            SyntaxGenerator generator, 
            CancellationToken cancellationToken)
        {
            // shell to lang specific to update the accessor.
            var newAccessor = UpdateAccessor(accessor, statements);

            // then conver to expression bod
            newAccessor= await ConvertToExpressionBodyIfDesiredAsync(
                document,
                newAccessor,
                cancellationToken).ConfigureAwait(false);

            return await Formatter.FormatAsync(newAccessor, document.Project.Solution.Workspace).ConfigureAwait(false);
        }

        private static ImmutableArray<NamingRule> GetDefaultRule(
            ImmutableArray<ModifierKind> modifiers, 
            string prefix)
        {
            return ImmutableArray.Create(
                new NamingRule(
                    new SymbolSpecification(
                        Guid.NewGuid(),
                        "Field",
                        ImmutableArray.Create(new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Field)),
                        modifiers: modifiers),
                    new NamingStyles.NamingStyle(
                        Guid.NewGuid(),
                        prefix: prefix,
                        capitalizationScheme: Capitalization.CamelCase),
                    DiagnosticSeverity.Hidden));
        }

        private class ConvertAutoPropertyToFullPropertyCodeAction : CodeAction.DocumentChangeAction
        {
            public ConvertAutoPropertyToFullPropertyCodeAction(
                string Title, 
                Func<CancellationToken, 
                Task<Document>> createChangedDocument) : base(Title, createChangedDocument)
            {
            }
        }
    }
}
