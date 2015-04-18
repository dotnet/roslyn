using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;

namespace System.Runtime.InteropServices.Analyzers
{
    public abstract class SpecifyMarshalingForPInvokeStringArgumentsFixer : CodeFixProvider
    {
        protected const string CharSetText = "CharSet";
        protected const string LPWStrText = "LPWStr";
        protected const string UnicodeText = "Unicode";

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(PInvokeDiagnosticAnalyzer.CA2101);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var node = root.FindNode(context.Span);
            if (node == null)
            {
                return;
            }

            var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            var charSetType = model.Compilation.GetTypeByMetadataName("System.Runtime.InteropServices.CharSet");
            var dllImportType = model.Compilation.GetTypeByMetadataName("System.Runtime.InteropServices.DllImportAttribute");
            var marshalAsType = model.Compilation.GetTypeByMetadataName("System.Runtime.InteropServices.MarshalAsAttribute");
            var unmanagedType = model.Compilation.GetTypeByMetadataName("System.Runtime.InteropServices.UnmanagedType");
            if (charSetType == null || dllImportType == null || marshalAsType == null || unmanagedType == null)
            {
                return;
            }

            // We cannot have multiple overlapping diagnostics of this id.
            var diagnostic = context.Diagnostics.Single();

            if (IsAttribute(node))
            {
                context.RegisterCodeFix(new MyCodeAction(SystemRuntimeInteropServicesAnalyzersResources.SpecifyMarshalingForPInvokeStringArguments,
                                                         async ct => await FixAttributeArguments(context.Document, node, charSetType, dllImportType, marshalAsType, unmanagedType, ct).ConfigureAwait(false)),
                                        diagnostic);
            }
            else if (IsDeclareStatement(node))
            {
                context.RegisterCodeFix(new MyCodeAction(SystemRuntimeInteropServicesAnalyzersResources.SpecifyMarshalingForPInvokeStringArguments,
                                                         async ct => await FixDeclareStatement(context.Document, node, ct).ConfigureAwait(false)),
                                        diagnostic);
            }

        }

        protected abstract bool IsAttribute(SyntaxNode node);
        protected abstract bool IsDeclareStatement(SyntaxNode node);
        protected abstract Task<Document> FixDeclareStatement(Document document, SyntaxNode node, CancellationToken cancellationToken);
        protected abstract SyntaxNode FindNamedArgument(IReadOnlyList<SyntaxNode> arguments, string argumentName);

        private async Task<Document> FixAttributeArguments(Document document, SyntaxNode attributeDeclaration,
            INamedTypeSymbol charSetType, INamedTypeSymbol dllImportType, INamedTypeSymbol marshalAsType, INamedTypeSymbol unmanagedType, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var generator = editor.Generator;
            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            // could be either a [DllImport] or [MarshalAs] attribute
            var attributeType = model.GetSymbolInfo(attributeDeclaration, cancellationToken).Symbol;
            var arguments = generator.GetAttributeArguments(attributeDeclaration);

            if (dllImportType.Equals(attributeType.ContainingType))
            {
                // [DllImport] attribute, add or replace CharSet named parameter
                var argumentValue = generator.MemberAccessExpression(
                                        generator.TypeExpression(charSetType),
                                        generator.IdentifierName(UnicodeText));
                var newCharSetArgument = generator.AttributeArgument(CharSetText, argumentValue);

                var charSetArgument = FindNamedArgument(arguments, CharSetText);
                if (charSetArgument == null)
                {
                    // add the parameter
                    editor.AddAttributeArgument(attributeDeclaration, newCharSetArgument);
                }
                else
                {
                    // replace the parameter
                    editor.ReplaceNode(charSetArgument, newCharSetArgument);
                }
            }
            else if (marshalAsType.Equals(attributeType.ContainingType) && arguments.Count == 1)
            {
                // [MarshalAs] attribute, replace the only argument
                var newArgument = generator.AttributeArgument(
                                        generator.MemberAccessExpression(
                                            generator.TypeExpression(unmanagedType), 
                                            generator.IdentifierName(LPWStrText)));

                editor.ReplaceNode(arguments[0], newArgument);
            }

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
