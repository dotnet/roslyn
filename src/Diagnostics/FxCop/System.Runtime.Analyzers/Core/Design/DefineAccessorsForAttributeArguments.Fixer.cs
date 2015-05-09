// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;

namespace System.Runtime.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic), Shared]
    public class DefineAccessorsForAttributeArgumentsFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DefineAccessorsForAttributeArgumentsAnalyzer.RuleId);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var generator = SyntaxGenerator.GetGenerator(context.Document);
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var node = root.FindNode(context.Span);

            // We cannot have multiple overlapping diagnostics of this id.
            var diagnostic = context.Diagnostics.Single();
            string fixCase;
            if (diagnostic.Properties.TryGetValue("case", out fixCase))
            {
                switch (fixCase)
                {
                    case DefineAccessorsForAttributeArgumentsAnalyzer.AddAccessorCase:
                        var parameter = generator.GetDeclaration(node, DeclarationKind.Parameter);
                        if (parameter != null)
                        {
                            context.RegisterCodeFix(new MyCodeAction(SystemRuntimeAnalyzersResources.CreatePropertyAccessorForParameter,
                                                         async ct => await AddAccessor(context.Document, parameter, ct).ConfigureAwait(false)),
                                                    diagnostic);
                        }
                        return;

                    case DefineAccessorsForAttributeArgumentsAnalyzer.MakePublicCase:
                        var property = generator.GetDeclaration(node, DeclarationKind.Property);
                        if (property != null)
                        {
                            context.RegisterCodeFix(new MyCodeAction(SystemRuntimeAnalyzersResources.MakeGetterPublic,
                                                             async ct => await MakePublic(context.Document, node, property, ct).ConfigureAwait(false)),
                                                    diagnostic);
                        }
                        return;

                    case DefineAccessorsForAttributeArgumentsAnalyzer.RemoveSetterCase:
                        context.RegisterCodeFix(new MyCodeAction(SystemRuntimeAnalyzersResources.MakeSetterNonPublic,
                                                     async ct => await RemoveSetter(context.Document, node, ct).ConfigureAwait(false)),
                                                diagnostic);
                        return;

                    default:
                        return;
                }
            }
        }

        private async Task<Document> AddAccessor(Document document, SyntaxNode parameter, CancellationToken cancellationToken)
        {
            SymbolEditor symbolEditor = SymbolEditor.Create(document);
            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var parameterSymbol = model.GetDeclaredSymbol(parameter, cancellationToken) as IParameterSymbol;
            if (parameterSymbol == null)
            {
                return document;
            }

            // Make the first character uppercase since we are generating a property.
            var propName = char.ToUpper(parameterSymbol.Name[0]).ToString() + parameterSymbol.Name.Substring(1);

            var typeSymbol = parameterSymbol.ContainingType;
            var propertySymbol = typeSymbol.GetMembers(propName).Where(m => m.Kind == SymbolKind.Property).FirstOrDefault();

            // Add a new property
            if (propertySymbol == null)
            {
                await symbolEditor.EditOneDeclarationAsync(typeSymbol,
                                                           parameter.GetLocation(), // edit the partial declaration that has this parameter symbol.
                                                           (editor, typeDeclaration) =>
                                                           {
                                                               var newProperty = editor.Generator.PropertyDeclaration(propName,
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
                                                              editor.SetGetAccessorStatements(propertyDeclaration, null);
                                                              editor.SetModifiers(propertyDeclaration, editor.Generator.GetModifiers(propertyDeclaration) - DeclarationModifiers.WriteOnly);
                                                          },
                                                          cancellationToken).ConfigureAwait(false);
            }

            return symbolEditor.GetChangedDocuments().First();
        }

        private async Task<Document> MakePublic(Document document, SyntaxNode getMethod, SyntaxNode property, CancellationToken cancellationToken)
        {
            // Clear the accessibility on the getter.
            DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            editor.SetAccessibility(getMethod, Accessibility.NotApplicable);

            // If the containing property is not public, make it so
            var propertyAccessibility = editor.Generator.GetAccessibility(property);
            if (propertyAccessibility != Accessibility.Public)
            {
                editor.SetAccessibility(property, Accessibility.Public);

                // Having just made the property public, if it has a setter with no accesibility set, then we've just made the setter public. 
                // Instead restore the setter's original accessibility so that we don't fire a violation with the generated code.
                var setter = editor.Generator.GetAccessor(property, DeclarationKind.SetAccessor);
                if (setter != null)
                {
                    var setterAccesibility = editor.Generator.GetAccessibility(setter);
                    if (setterAccesibility == Accessibility.NotApplicable)
                    {
                        editor.SetAccessibility(setter, propertyAccessibility);
                    }
                }
            }

            return editor.GetChangedDocument();
        }

        private async Task<Document> RemoveSetter(Document document, SyntaxNode setMethod, CancellationToken cancellationToken)
        {
            DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            editor.SetAccessibility(setMethod, Accessibility.Internal);
            return editor.GetChangedDocument();
        }

        private class MyCodeAction : DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}
