// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions;

public sealed class MvcViewDocumentClassifierPass : DocumentClassifierPassBase
{
    private readonly bool _useConsolidatedMvcViews;

    public static readonly string MvcViewDocumentKind = "mvc.1.0.view";

    protected override string DocumentKind => MvcViewDocumentKind;

    protected override bool IsMatch(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode) => true;

    public MvcViewDocumentClassifierPass() : this(false) { }

    public MvcViewDocumentClassifierPass(bool useConsolidatedMvcViews)
    {
        _useConsolidatedMvcViews = useConsolidatedMvcViews;
    }

    protected override void OnDocumentStructureCreated(
        RazorCodeDocument codeDocument,
        NamespaceDeclarationIntermediateNode @namespace,
        ClassDeclarationIntermediateNode @class,
        MethodDeclarationIntermediateNode method)
    {
        base.OnDocumentStructureCreated(codeDocument, @namespace, @class, method);

        if (!codeDocument.TryGetNamespace(fallbackToRootNamespace: false, out var namespaceName))
        {
            @namespace.Name = _useConsolidatedMvcViews ? "AspNetCoreGeneratedDocument" : "AspNetCore";
        }
        else
        {
            @namespace.Name = namespaceName;
        }

        if (!codeDocument.TryComputeClassName(out var className))
        {
            // It's possible for a Razor document to not have a file path.
            // Eg. When we try to generate code for an in memory document like default imports.
            var checksum = ChecksumUtilities.BytesToString(codeDocument.Source.Text.GetChecksum());
            @class.Name = "AspNetCore_" + checksum;
        }
        else
        {
            @class.Name = className;
        }
        @class.BaseType = new BaseTypeWithModel("global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<TModel>", location: null);

        @class.Modifiers = _useConsolidatedMvcViews
            ? CommonModifiers.InternalSealed
            : CommonModifiers.Public;

        @class.NullableContext = true;

        method.Name = "ExecuteAsync";
        method.Modifiers = CommonModifiers.PublicAsyncOverride;
        method.ReturnType = $"global::{typeof(System.Threading.Tasks.Task).FullName}";
    }
}
