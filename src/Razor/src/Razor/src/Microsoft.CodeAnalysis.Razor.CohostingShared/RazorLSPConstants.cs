// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.VisualStudio.Razor.LanguageClient;

internal static class RazorLSPConstants
{
    public const string WebToolsWrapWithTagServerNameProperty = "_wrap_with_tag_lsp_server_name";

    public const string RazorCSharpLanguageServerName = "Razor C# Language Server Client";

    public const string RazorLanguageServerName = "Razor Language Server Client";

    public const string RoslynLanguageServerName = "Roslyn Language Server Client";

    public const string CohostLanguageServerName = "Cohosted Razor Language Server Client";

    public const string HtmlLanguageServerName = "HtmlDelegationLanguageServerClient";

    public const string CSHTMLFileExtension = ".cshtml";

    public const string RazorFileExtension = ".razor";

    public const string CSharpFileExtension = ".cs";

    public const string CSharpContentTypeName = "CSharp";

    public const string HtmlLSPDelegationContentTypeName = "html-delegation";

    public const string RoslynSimplifyMethodEndpointName = "roslyn/simplifyMethod";

    public const string RoslynFormatNewFileEndpointName = "roslyn/formatNewFile";

    public const string RoslynSemanticTokenRangesEndpointName = "roslyn/semanticTokenRanges";

    public const string ApplyRenameEditName = "razor/applyRenameEdit";

    public const string AddNestedFileName = "razor/addNestedFile";
}
