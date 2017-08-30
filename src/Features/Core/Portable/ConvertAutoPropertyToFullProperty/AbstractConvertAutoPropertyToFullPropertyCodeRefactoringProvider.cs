// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using static Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles.SymbolSpecification;

namespace Microsoft.CodeAnalysis.ConvertAutoPropertyToFullProperty
{
    internal abstract class AbstractConvertAutoPropertyToFullPropertyCodeRefactoringProvider
        : CodeRefactoringProvider
    {
        internal abstract SyntaxNode GetProperty(SyntaxToken token);
        internal abstract string GetUniqueName(string fieldName, IPropertySymbol property);
        internal abstract SyntaxNode GetInitializerValue(SyntaxNode property);
        internal abstract SyntaxNode GetPropertyWithoutInitializer(SyntaxNode property);
        internal abstract Task<(SyntaxNode newGetAccessor, SyntaxNode newSetAccessor)> GetNewAccessorsAsync(
            Document document, SyntaxNode property, string fieldName, SyntaxGenerator generator, 
            CancellationToken cancellationToken);
        internal abstract SyntaxNode GetTypeBlock(SyntaxNode syntaxNode);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(context.Span.Start);

            var property = GetProperty(token);
            if (property == null)
            {
                return;
            }

            if (!(await IsValidAutoProperty(property, document, cancellationToken).ConfigureAwait(false)))
            {
                return;
            }

            context.RegisterRefactoring(
                new ConvertAutoPropertyToFullPropertyCodeAction(
                    FeaturesResources.Convert_to_full_property,
                    c => ExpandToFullPropertyAsync(
                        document,
                        property,
                        root,
                        cancellationToken)));
        }

        internal async Task<bool> IsValidAutoProperty(
            SyntaxNode property, Document document, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken)
                .ConfigureAwait(false);

            var propertySymbol = semanticModel.GetDeclaredSymbol(property) as IPropertySymbol;

            var members = propertySymbol.ContainingType.GetMembers()
                .Where(n => n.Kind == SymbolKind.Field);

            IFieldSymbol fieldSymbol;
            foreach (var member in members)
            {
                fieldSymbol = (IFieldSymbol)member;
                if (fieldSymbol.AssociatedSymbol?.Name == propertySymbol.Name)
                {
                    return true;
                }
            }

            return false;
        }

        private async Task<Document> ExpandToFullPropertyAsync(
            Document document,
            SyntaxNode property,
            SyntaxNode root,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var propertySymbol = semanticModel.GetDeclaredSymbol(property) as IPropertySymbol;

            var rules = await GetNamingRulesAsync(
                document, 
                cancellationToken).ConfigureAwait(false);
            var fieldName = GenerateFieldName(propertySymbol, rules);

            var generator = SyntaxGenerator.GetGenerator(document);
            var newRoot = await ExpandPropertyAndAddFieldAsync(
                document,
                property,
                root,
                propertySymbol,
                fieldName,
                generator,
                cancellationToken).ConfigureAwait(false);

            return document.WithSyntaxRoot(newRoot);
        }

        /// <summary>
        /// Get the user-specified naming rules, then add standard default naming rules 
        /// for both static and non-static fields.  The standard naming rules are added at the end 
        /// so they will only be used if the user hasn't specified a preference.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private static async Task<ImmutableArray<NamingRule>> GetNamingRulesAsync(
            Document document,
            CancellationToken cancellationToken)
        {
            var optionSet = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var namingPreferencesOption = optionSet.GetOption(SimplificationOptions.NamingPreferences);
            var rules = namingPreferencesOption.CreateRules().NamingRules
                .AddRange(GetDefaultRule(ImmutableArray.Create(new ModifierKind(ModifierKindEnum.IsStatic)), "s_"))
                .AddRange(GetDefaultRule(ImmutableArray.Create<ModifierKind>(), "_"));
            return rules;
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

        private string GenerateFieldName(
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

            return GetUniqueName(fieldName, property);
        }

        private async Task<SyntaxNode> ExpandPropertyAndAddFieldAsync(
            Document document,
            SyntaxNode property,
            SyntaxNode root,
            IPropertySymbol propertySymbol,
            string fieldName,
            SyntaxGenerator generator,
            CancellationToken cancellationToken)
        {
            var workspace = document.Project.Solution.Workspace;

            // Create full property. If the auto property had an initial value
            // we need to remove it and later add it to the backing field
            var accessorTuple = await GetNewAccessorsAsync(
                document,
                property,
                fieldName,
                generator,
                cancellationToken)
                    .ConfigureAwait(false);

            var fullProperty = generator
                    .WithAccessorDeclarations(GetPropertyWithoutInitializer(property), accessorTuple.newSetAccessor == null ? new SyntaxNode[] { accessorTuple.newGetAccessor } : new SyntaxNode[] { accessorTuple.newGetAccessor, accessorTuple.newSetAccessor })
                    .WithLeadingTrivia(property.GetLeadingTrivia())
                    .WithAdditionalAnnotations(Formatter.Annotation);
            var editor = new SyntaxEditor(root, workspace);
            editor.ReplaceNode(property, fullProperty);

            // add backing field, plus initializer if it exists 
            var newField = CodeGenerationSymbolFactory.CreateFieldSymbol(default, Accessibility.Private, DeclarationModifiers.From(propertySymbol), propertySymbol.Type, fieldName, initializer: GetInitializerValue(property));
            var containingType = GetTypeBlock(propertySymbol.ContainingType.DeclaringSyntaxReferences.FirstOrDefault().GetSyntax(cancellationToken));

            editor.ReplaceNode(containingType, (currentTypeDecl, _) =>
                {
                    return CodeGenerator.AddFieldDeclaration(currentTypeDecl, newField, workspace);
                });

            return editor.GetChangedRoot();
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
