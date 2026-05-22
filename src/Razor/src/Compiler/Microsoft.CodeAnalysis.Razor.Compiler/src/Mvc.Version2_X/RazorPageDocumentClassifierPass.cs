// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version2_X;

public sealed class RazorPageDocumentClassifierPass : DocumentClassifierPassBase
{
    public static readonly string RazorPageDocumentKind = "mvc.1.0.razor-page";
    public static readonly string RouteTemplateKey = "RouteTemplate";

    private static readonly RazorProjectEngine LeadingDirectiveParsingEngine = RazorProjectEngine.Create(
        new(RazorLanguageVersion.Version_2_1, "leading-directive-parser", Extensions: []),
        RazorProjectFileSystem.Create("/"),
        builder =>
        {
            for (var i = builder.Phases.Count - 1; i >= 0; i--)
            {
                var phase = builder.Phases[i];
                builder.Phases.RemoveAt(i);
                if (phase is IRazorDocumentClassifierPhase)
                {
                    break;
                }
            }

            RazorExtensions.Register(builder);

            builder.ConfigureParserOptions(builder =>
            {
                builder.ParseLeadingDirectives = true;
            });
        });

    protected override string DocumentKind => RazorPageDocumentKind;

    protected override bool IsMatch(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
    {
        return PageDirective.TryGetPageDirective(documentNode, out _);
    }

    protected override void OnDocumentStructureCreated(
        RazorCodeDocument codeDocument,
        NamespaceDeclarationIntermediateNode @namespace,
        ClassDeclarationIntermediateNode @class,
        MethodDeclarationIntermediateNode method)
    {
        base.OnDocumentStructureCreated(codeDocument, @namespace, @class, method);

        @namespace.Name = "AspNetCore";

        @class.BaseType = new BaseTypeWithModel("global::Microsoft.AspNetCore.Mvc.RazorPages.Page");

        var filePath = codeDocument.Source.RelativePath ?? codeDocument.Source.FilePath;
        if (string.IsNullOrEmpty(filePath))
        {
            // It's possible for a Razor document to not have a file path.
            // Eg. When we try to generate code for an in memory document like default imports.
            var checksum = ChecksumUtilities.BytesToString(codeDocument.Source.Text.GetChecksum());
            @class.Name = $"AspNetCore_{checksum}";
        }
        else
        {
            @class.Name = CSharpIdentifier.GetClassNameFromPath(filePath);
        }

        @class.Modifiers = CommonModifiers.Public;

        method.Name = "ExecuteAsync";
        method.Modifiers = CommonModifiers.PublicAsyncOverride;
        method.ReturnType = $"global::{typeof(System.Threading.Tasks.Task).FullName}";

        var document = codeDocument.GetRequiredDocumentNode();
        PageDirective.TryGetPageDirective(document, out var pageDirective);

        EnsureValidPageDirective(codeDocument, pageDirective);

        AddRouteTemplateMetadataAttribute(@namespace, @class, pageDirective);
    }

    private static void AddRouteTemplateMetadataAttribute(NamespaceDeclarationIntermediateNode @namespace, ClassDeclarationIntermediateNode @class, PageDirective pageDirective)
    {
        if (string.IsNullOrEmpty(pageDirective.RouteTemplate))
        {
            return;
        }

        var classIndex = @namespace.Children.IndexOf(@class);
        if (classIndex == -1)
        {
            return;
        }

        var metadataAttributeNode = new RazorCompiledItemMetadataAttributeIntermediateNode
        {
            Key = RouteTemplateKey,
            Value = pageDirective.RouteTemplate,
            Source = pageDirective.Source,
            ValueStringSyntax = "Route"
        };
        // Metadata attributes need to be inserted right before the class declaration.
        @namespace.Children.Insert(classIndex, metadataAttributeNode);
    }

    private void EnsureValidPageDirective(RazorCodeDocument codeDocument, PageDirective pageDirective)
    {
        Debug.Assert(pageDirective != null);

        if (pageDirective.DirectiveNode.IsImported)
        {
            pageDirective.DirectiveNode.AddDiagnostic(
                RazorExtensionsDiagnosticFactory.CreatePageDirective_CannotBeImported(pageDirective.DirectiveNode.Source.AssumeNotNull()));
        }
        else
        {
            // The document contains a page directive and it is not imported.
            // We now want to make sure this page directive exists at the top of the file.
            // We are going to do that by re-parsing the document until the very first line that is not Razor comment
            // or whitespace. We then make sure the page directive still exists in the re-parsed IR tree.
            var leadingDirectiveCodeDocument = LeadingDirectiveParsingEngine.CreateCodeDocument(codeDocument.Source);
            leadingDirectiveCodeDocument = LeadingDirectiveParsingEngine.Engine.Process(leadingDirectiveCodeDocument);

            var leadingDirectiveDocumentNode = leadingDirectiveCodeDocument.GetRequiredDocumentNode();
            if (!PageDirective.TryGetPageDirective(leadingDirectiveDocumentNode, out _))
            {
                // The page directive is not the leading directive. Add an error.
                pageDirective.DirectiveNode.AddDiagnostic(
                    RazorExtensionsDiagnosticFactory.CreatePageDirective_MustExistAtTheTopOfFile(pageDirective.DirectiveNode.Source.AssumeNotNull()));
            }
        }
    }
}
