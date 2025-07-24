// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeQuality.Analyzers.ApiDesignGuidelines
{
    static class Extensions
    {
        public static IEnumerable<SyntaxNode> DefaultMethodBody(
    this SyntaxGenerator generator, Compilation compilation)
        {
            yield return DefaultMethodStatement(generator, compilation);
        }

        public static SyntaxNode DefaultMethodStatement(this SyntaxGenerator generator, Compilation compilation)
        {
            return generator.ThrowStatement(generator.ObjectCreationExpression(
                generator.TypeExpression(
                    compilation.GetTypeByMetadataName("System.NotImplementedException"))));
        }
    }

    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic), Shared]
    internal sealed class DefineAccessorsForAttributeArgumentsFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(DefineAccessorsForAttributeArgumentsAnalyzer.RuleId);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SyntaxGenerator generator = SyntaxGenerator.GetGenerator(context.Document);
            SyntaxNode root = await context.Document.GetRequiredSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            SyntaxNode node = root.FindNode(context.Span);

            foreach (var diagnostic in context.Diagnostics)
            {
                if (diagnostic.Properties.TryGetValue("case", out var fixCase))
                {
                    string title;
                    switch (fixCase)
                    {
                        case DefineAccessorsForAttributeArgumentsAnalyzer.AddAccessorCase:
                            SyntaxNode parameter = generator.GetDeclaration(node, DeclarationKind.Parameter);
                            if (parameter != null)
                            {
                                title = "MicrosoftCodeQualityAnalyzersResources.CreatePropertyAccessorForParameter";
                                context.RegisterCodeFix(CodeAction.Create(title,
                                                             async ct => await AddAccessorAsync(context.Document, parameter, ct).ConfigureAwait(false),
                                                             equivalenceKey: title),
                                                        diagnostic);
                            }

                            return;

                        case DefineAccessorsForAttributeArgumentsAnalyzer.MakePublicCase:
                            SyntaxNode property = generator.GetDeclaration(node, DeclarationKind.Property);
                            if (property != null)
                            {
                                title = "MicrosoftCodeQualityAnalyzersResources.MakeGetterPublic";
                                context.RegisterCodeFix(CodeAction.Create(title,
                                                                 async ct => await MakePublicAsync(context.Document, node, property, ct).ConfigureAwait(false),
                                                                 equivalenceKey: title),
                                                        diagnostic);
                            }

                            return;

                        case DefineAccessorsForAttributeArgumentsAnalyzer.RemoveSetterCase:
                            title = "MicrosoftCodeQualityAnalyzersResources.MakeSetterNonPublic";
                            context.RegisterCodeFix(CodeAction.Create(title,
                                                         async ct => await RemoveSetterAsync(context.Document, node, ct).ConfigureAwait(false),
                                                         equivalenceKey: title),
                                                    diagnostic);
                            return;

                        default:
                            return;
                    }
                }
            }
        }

        private static async Task<Document> AddAccessorAsync(Document document, SyntaxNode parameter, CancellationToken cancellationToken)
        {
            SymbolEditor symbolEditor = SymbolEditor.Create(document);
            SemanticModel model = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            if (model.GetDeclaredSymbol(parameter, cancellationToken) is not IParameterSymbol parameterSymbol)
            {
                return document;
            }

            // Make the first character uppercase since we are generating a property.
            string propName = char.ToUpper(parameterSymbol.Name[0], CultureInfo.InvariantCulture).ToString() + parameterSymbol.Name[1..];

            INamedTypeSymbol typeSymbol = parameterSymbol.ContainingType;
            ISymbol? propertySymbol = typeSymbol.GetMembers(propName).FirstOrDefault(m => m.Kind == SymbolKind.Property);

            // Add a new property
            if (propertySymbol == null)
            {
                await symbolEditor.EditOneDeclarationAsync(typeSymbol,
                                                           parameter.GetLocation(), // edit the partial declaration that has this parameter symbol.
                                                           (editor, typeDeclaration) =>
                                                           {
                                                               SyntaxNode newProperty = editor.Generator.PropertyDeclaration(propName,
                                                                                                                      editor.Generator.TypeExpression(parameterSymbol.Type),
                                                                                                                      Accessibility.Public,
                                                                                                                      DeclarationModifiers.ReadOnly);
                                                               newProperty = editor.Generator.WithGetAccessorStatements(newProperty, null);
                                                               editor.AddMember(typeDeclaration, newProperty);
                                                           },
                                                           cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await symbolEditor.EditOneDeclarationAsync(propertySymbol,
                                                          (editor, propertyDeclaration) =>
                                                          {
                                                              editor.SetGetAccessorStatements(propertyDeclaration, editor.Generator.DefaultMethodBody(model.Compilation));
                                                              editor.SetModifiers(propertyDeclaration, editor.Generator.GetModifiers(propertyDeclaration) - DeclarationModifiers.WriteOnly);
                                                          },
                                                          cancellationToken).ConfigureAwait(false);
            }

            return symbolEditor.GetChangedDocuments().First();
        }

        private static async Task<Document> MakePublicAsync(Document document, SyntaxNode getMethod, SyntaxNode property, CancellationToken cancellationToken)
        {
            // Clear the accessibility on the getter.
            DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            editor.SetAccessibility(getMethod, Accessibility.NotApplicable);

            // If the containing property is not public, make it so
            Accessibility propertyAccessibility = editor.Generator.GetAccessibility(property);
            if (propertyAccessibility != Accessibility.Public)
            {
                editor.SetAccessibility(property, Accessibility.Public);

                // Having just made the property public, if it has a setter with no Accessibility set, then we've just made the setter public.
                // Instead restore the setter's original accessibility so that we don't fire a violation with the generated code.
                SyntaxNode setter = editor.Generator.GetAccessor(property, DeclarationKind.SetAccessor);
                if (setter != null)
                {
                    Accessibility setterAccessibility = editor.Generator.GetAccessibility(setter);
                    if (setterAccessibility == Accessibility.NotApplicable)
                    {
                        editor.SetAccessibility(setter, propertyAccessibility);
                    }
                }
            }

            return editor.GetChangedDocument();
        }

        private static async Task<Document> RemoveSetterAsync(Document document, SyntaxNode setMethod, CancellationToken cancellationToken)
        {
            DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            editor.SetAccessibility(setMethod, Accessibility.Internal);
            return editor.GetChangedDocument();
        }

        public override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }
    }
}
