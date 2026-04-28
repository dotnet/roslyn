// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language;

internal class DefaultRazorCSharpLoweringPhase : RazorEnginePhaseBase, IRazorCSharpLoweringPhase
{
    protected override RazorCodeDocument ExecuteCore(RazorCodeDocument codeDocument, CancellationToken cancellationToken)
    {
        var documentNode = codeDocument.GetDocumentNode();
        ThrowForMissingDocumentDependency(documentNode);

        var target = documentNode.Target;
        if (target == null)
        {
            var message = Resources.FormatDocumentMissingTarget(
                documentNode.DocumentKind,
                nameof(CodeTarget),
                nameof(DocumentIntermediateNode.Target));
            throw new InvalidOperationException(message);
        }

        var csharpDocument = WriteDocument(codeDocument, cancellationToken);
        return codeDocument.WithCSharpDocument(csharpDocument);
    }

    private static RazorCSharpDocument WriteDocument(RazorCodeDocument codeDocument, CancellationToken cancellationToken = default)
    {
        ArgHelper.ThrowIfNull(codeDocument);

        var documentNode = codeDocument.GetRequiredDocumentNode();
        var codeTarget = documentNode.Target;

        using var context = new CodeRenderingContext(
            codeTarget.CreateNodeWriter(),
            codeDocument.Source,
            documentNode,
            codeDocument.CodeGenerationOptions);

        context.SetVisitor(new Visitor(context, codeTarget, cancellationToken));

        context.Visitor.VisitDocument(documentNode);

        var text = context.CodeWriter.GetText();

        return new RazorCSharpDocument(
            codeDocument,
            text,
            context.GetDiagnostics(),
            context.GetSourceMappings(),
            context.GetLinePragmas());
    }

    private sealed class Visitor(
        CodeRenderingContext context,
        CodeTarget codeTarget,
        CancellationToken cancellationToken) : IntermediateNodeVisitor
    {
        private readonly CodeRenderingContext _context = context;
        private readonly CodeTarget _codeTarget = codeTarget;
        private readonly CancellationToken _cancellationToken = cancellationToken;

        private CodeWriter CodeWriter => _context.CodeWriter;
        private IntermediateNodeWriter NodeWriter => _context.NodeWriter;
        private RazorCodeGenerationOptions Options => _context.Options;

        public override void VisitDocument(DocumentIntermediateNode node)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var writer = CodeWriter;

            if (!Options.SuppressChecksum)
            {
                // See http://msdn.microsoft.com/en-us/library/system.codedom.codechecksumpragma.checksumalgorithmid.aspx
                // And https://github.com/dotnet/roslyn/blob/614299ff83da9959fa07131c6d0ffbc58873b6ae/src/Compilers/Core/Portable/PEWriter/DebugSourceDocument.cs#L67
                //
                // We only support algorithms that the debugger understands, which is currently SHA1 and SHA256.

                string algorithmId;
                var algorithm = _context.SourceDocument.Text.ChecksumAlgorithm;
                if (algorithm == CodeAnalysis.Text.SourceHashAlgorithm.Sha256)
                {
                    algorithmId = "{8829d00f-11b8-4213-878b-770e8597ac16}";
                }
                else if (algorithm == CodeAnalysis.Text.SourceHashAlgorithm.Sha1)
                {
                    algorithmId = "{ff1816ec-aa5e-4d10-87f7-6f4963833460}";
                }
                else
                {
                    // CodeQL [SM02196] This is supported by the underlying Roslyn APIs and as consumers we must also support it.
                    var message = Resources.FormatUnsupportedChecksumAlgorithm(
                        algorithm,
                        $"{HashAlgorithmName.SHA1.Name} {HashAlgorithmName.SHA256.Name}",
                        $"{nameof(RazorCodeGenerationOptions)}.{nameof(RazorCodeGenerationOptions.SuppressChecksum)}",
                        bool.TrueString);

                    throw new InvalidOperationException(message);
                }

                var sourceDocument = _context.SourceDocument;

                var checksum = ChecksumUtilities.BytesToString(sourceDocument.Text.GetChecksum());
                var filePath = sourceDocument.FilePath.AssumeNotNull();

                if (checksum.Length > 0)
                {
                    writer.WriteLine($"#pragma checksum \"{filePath}\" \"{algorithmId}\" \"{checksum}\"");
                }
            }

            writer
                .WriteLine("// <auto-generated/>")
                .WriteLine("#pragma warning disable 1591");

            VisitDefault(node);

            writer.WriteLine("#pragma warning restore 1591");
        }

        public override void VisitUsingDirective(UsingDirectiveIntermediateNode node)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            NodeWriter.WriteUsingDirective(_context, node);
        }

        public override void VisitNamespaceDeclaration(NamespaceDeclarationIntermediateNode node)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var writer = CodeWriter;

            using (writer.BuildNamespace(node.Name, node.Source, _context))
            {
                var hasUsingDirectives = false;

                foreach (var child in node.Children)
                {
                    if (child is UsingDirectiveIntermediateNode)
                    {
                        hasUsingDirectives = true;
                        break;
                    }
                }

                if (hasUsingDirectives)
                {
                    // Tooling needs at least one line directive before using directives, otherwise Roslyn will
                    // not offer to create a new one. The last using in the group will output a hidden line
                    // directive after itself.
                    writer.WriteLine("#line default");
                }
                else
                {
                    // If there are no using directives, we output the hidden directive here.
                    writer.WriteLine("#line hidden");
                }

                VisitDefault(node);
            }
        }

        public override void VisitClassDeclaration(ClassDeclarationIntermediateNode node)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            using (CodeWriter.BuildClassDeclaration(
                node.Modifiers,
                node.Name,
                node.BaseType,
                node.Interfaces,
                node.TypeParameters,
                _context,
                useNullableContext: !Options.SuppressNullabilityEnforcement && node.NullableContext))
            {
                VisitDefault(node);
            }
        }

        public override void VisitMethodDeclaration(MethodDeclarationIntermediateNode node)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var writer = CodeWriter;

            writer.WriteLine("#pragma warning disable 1998");

            using (CodeWriter.BuildMethodDeclaration(
                node.Modifiers,
                node.ReturnType,
                node.Name,
                node.Parameters))
            {
                VisitDefault(node);
            }

            writer.WriteLine("#pragma warning restore 1998");
        }

        public override void VisitFieldDeclaration(FieldDeclarationIntermediateNode node)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            CodeWriter.WriteField(node.SuppressWarnings, node.Modifiers, node.Type, node.Name);
        }

        public override void VisitPropertyDeclaration(PropertyDeclarationIntermediateNode node)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            CodeWriter.WritePropertyDeclaration(node.Modifiers, node.Type, node.Name, node.ExpressionBody, _context);
        }

        public override void VisitExtension(ExtensionIntermediateNode node)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            node.WriteNode(_codeTarget, _context);
        }

        public override void VisitCSharpExpression(CSharpExpressionIntermediateNode node)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            NodeWriter.WriteCSharpExpression(_context, node);
        }

        public override void VisitCSharpCode(CSharpCodeIntermediateNode node)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            NodeWriter.WriteCSharpCode(_context, node);
        }

        public override void VisitHtmlAttribute(HtmlAttributeIntermediateNode node)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            NodeWriter.WriteHtmlAttribute(_context, node);
        }

        public override void VisitHtmlAttributeValue(HtmlAttributeValueIntermediateNode node)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            NodeWriter.WriteHtmlAttributeValue(_context, node);
        }

        public override void VisitCSharpExpressionAttributeValue(CSharpExpressionAttributeValueIntermediateNode node)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            NodeWriter.WriteCSharpExpressionAttributeValue(_context, node);
        }

        public override void VisitCSharpCodeAttributeValue(CSharpCodeAttributeValueIntermediateNode node)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            NodeWriter.WriteCSharpCodeAttributeValue(_context, node);
        }

        public override void VisitHtml(HtmlContentIntermediateNode node)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            NodeWriter.WriteHtmlContent(_context, node);
        }

        public override void VisitTagHelper(TagHelperIntermediateNode node)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            VisitDefault(node);
        }

        public override void VisitComponent(ComponentIntermediateNode node)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            NodeWriter.WriteComponent(_context, node);
        }

        public override void VisitComponentAttribute(ComponentAttributeIntermediateNode node)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            NodeWriter.WriteComponentAttribute(_context, node);
        }

        public override void VisitComponentChildContent(ComponentChildContentIntermediateNode node)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            NodeWriter.WriteComponentChildContent(_context, node);
        }

        public override void VisitComponentTypeArgument(ComponentTypeArgumentIntermediateNode node)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            NodeWriter.WriteComponentTypeArgument(_context, node);
        }

        public override void VisitComponentTypeInferenceMethod(ComponentTypeInferenceMethodIntermediateNode node)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            NodeWriter.WriteComponentTypeInferenceMethod(_context, node);
        }

        public override void VisitMarkupElement(MarkupElementIntermediateNode node)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            NodeWriter.WriteMarkupElement(_context, node);
        }

        public override void VisitMarkupBlock(MarkupBlockIntermediateNode node)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            NodeWriter.WriteMarkupBlock(_context, node);
        }

        public override void VisitReferenceCapture(ReferenceCaptureIntermediateNode node)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            NodeWriter.WriteReferenceCapture(_context, node);
        }

        public override void VisitSetKey(SetKeyIntermediateNode node)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            NodeWriter.WriteSetKey(_context, node);
        }

        public override void VisitSplat(SplatIntermediateNode node)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            NodeWriter.WriteSplat(_context, node);
        }

        public override void VisitRenderMode(RenderModeIntermediateNode node)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            NodeWriter.WriteRenderMode(_context, node);
        }

        public override void VisitFormName(FormNameIntermediateNode node)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            NodeWriter.WriteFormName(_context, node);
        }

        public override void VisitDefault(IntermediateNode node)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            _context.RenderChildren(node);
        }
    }
}
