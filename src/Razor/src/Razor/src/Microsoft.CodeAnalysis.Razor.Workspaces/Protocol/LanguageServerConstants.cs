// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.CodeAnalysis.Razor.Protocol;

internal static class LanguageServerConstants
{
    public const string RazorDiagnosticSource = "Razor";

    public const string HtmlVirtualDocumentSuffix = "__virtual.html";

    public const string RazorLanguageServerName = "Razor Language Server";

    public const string RazorCodeActionRunnerCommand = "razor/runCodeAction";

    // This needs to be the same as in Web Tools, that is used by the HTML editor, because
    // we actually respond to the Web Tools "Wrap With Div" command handler, which sends this message
    // to all servers. We then take the message, get the HTML virtual document, and send it
    // straight back to Web Tools for them to do the work.
    public const string RazorWrapWithTagEndpoint = "textDocument/_vsweb_wrapWithTag";

    public static class CodeActions
    {
        public const string AddUsing = nameof(AddUsing);

        public const string CodeActionFromVSCode = nameof(CodeActionFromVSCode);

        public const string CreateComponentFromTag = nameof(CreateComponentFromTag);

        public const string EditBasedCodeActionCommand = nameof(EditBasedCodeActionCommand);

        public const string ExtractToCodeBehind = nameof(ExtractToCodeBehind);

        public const string ExtractToCss = nameof(ExtractToCss);

        public const string ExtractToNewComponent = nameof(ExtractToNewComponent);

        public const string FullyQualify = nameof(FullyQualify);

        public const string GenerateAsyncEventHandler = nameof(GenerateAsyncEventHandler);

        public const string GenerateEventHandler = nameof(GenerateEventHandler);

        public const string PromoteUsingDirective = nameof(PromoteUsingDirective);

        public const string RemoveUnnecessaryDirectives = nameof(RemoveUnnecessaryDirectives);

        public const string SimplifyFullyQualifiedComponent = nameof(SimplifyFullyQualifiedComponent);

        public const string SimplifyTagToSelfClosing = nameof(SimplifyTagToSelfClosing);

        public const string SortAndConsolidateUsings = nameof(SortAndConsolidateUsings);

        public const string WrapAttributes = nameof(WrapAttributes);

        /// <summary>
        /// Remaps without formatting the resolved code action edit
        /// </summary>
        public const string UnformattedRemap = nameof(UnformattedRemap);

        /// <summary>
        /// Remaps and formats the resolved code action edit
        /// </summary>
        public const string Default = nameof(Default);
    }
}
