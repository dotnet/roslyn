// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version2_X;

public sealed class MvcViewDocumentClassifierPass : DocumentClassifierPassBase
{
    public static readonly string MvcViewDocumentKind = "mvc.1.0.view";

    protected override string DocumentKind => MvcViewDocumentKind;

    protected override bool IsMatch(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode) => true;

    protected override void OnDocumentStructureCreated(
        RazorCodeDocument codeDocument,
        NamespaceDeclarationIntermediateNode @namespace,
        ClassDeclarationIntermediateNode @class,
        MethodDeclarationIntermediateNode method)
    {
        base.OnDocumentStructureCreated(codeDocument, @namespace, @class, method);

        @namespace.Name = "AspNetCore";

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

        @class.BaseType = new BaseTypeWithModel("global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<TModel>", location: null);
        @class.Modifiers = CommonModifiers.Public;

        method.Name = "ExecuteAsync";
        method.Modifiers = CommonModifiers.PublicAsyncOverride;
        method.ReturnType = $"global::{typeof(System.Threading.Tasks.Task).FullName}";
    }
}
