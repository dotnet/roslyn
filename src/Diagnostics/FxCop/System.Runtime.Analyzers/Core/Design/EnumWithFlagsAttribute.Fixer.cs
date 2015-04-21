// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace System.Runtime.Analyzers
{
    /// <summary>
    /// CA1027: Mark enums with FlagsAttribute
    /// CA2217: Do not mark enums with FlagsAttribute
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic), Shared]
    public class EnumWithFlagsAttributeFixer : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(EnumWithFlagsAttributeAnalyzer.RuleIdMarkEnumsWithFlags,
                                                                                   EnumWithFlagsAttributeAnalyzer.RuleIdDoNotMarkEnumsWithFlags);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            
            var flagsAttributeType = WellKnownTypes.FlagsAttribute(model.Compilation);
            if (flagsAttributeType == null)
            {
                return;
            }

            // We cannot have multiple overlapping diagnostics of this id.
            var diagnostic = context.Diagnostics.Single();
            var fixTitle = diagnostic.Id == EnumWithFlagsAttributeAnalyzer.RuleIdMarkEnumsWithFlags ?
                                                    SystemRuntimeAnalyzersResources.MarkEnumsWithFlagsCodeFix :
                                                    SystemRuntimeAnalyzersResources.DoNotMarkEnumsWithFlagsCodeFix;
            context.RegisterCodeFix(new MyCodeAction(fixTitle,
                                         async ct => await AddOrRemoveFlagsAttribute(context.Document, context.Span, diagnostic.Id, flagsAttributeType, ct).ConfigureAwait(false)),
                        diagnostic);
        }

        private async Task<Document> AddOrRemoveFlagsAttribute(Document document, TextSpan span, string diagnosticId, INamedTypeSymbol flagsAttributeType, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var node = root.FindNode(span);

            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var newEnumBlockSyntax = diagnosticId == EnumWithFlagsAttributeAnalyzer.RuleIdMarkEnumsWithFlags ?
                AddFlagsAttribute(editor.Generator, node, flagsAttributeType) :
                RemoveFlagsAttribute(editor.Generator, model, node, flagsAttributeType, cancellationToken);

            editor.ReplaceNode(node, newEnumBlockSyntax);
            return editor.GetChangedDocument();
        }

        private static SyntaxNode AddFlagsAttribute(SyntaxGenerator generator, SyntaxNode enumTypeSyntax, INamedTypeSymbol flagsAttributeType)
        {
            return generator.AddAttributes(enumTypeSyntax, generator.Attribute(generator.TypeExpression(flagsAttributeType)));
        }

        private static SyntaxNode RemoveFlagsAttribute(SyntaxGenerator generator, SemanticModel model, SyntaxNode enumTypeSyntax, INamedTypeSymbol flagsAttributeType, CancellationToken cancellationToken)
        {
            var enumType = model.GetDeclaredSymbol(enumTypeSyntax, cancellationToken) as INamedTypeSymbol;
            Debug.Assert(enumType != null);

            var flagsAttribute = enumType.GetAttributes().First(a => a.AttributeClass == flagsAttributeType);
            var attributeNode = flagsAttribute.ApplicationSyntaxReference.GetSyntax(cancellationToken);

            return generator.RemoveNode(enumTypeSyntax, attributeNode);
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
