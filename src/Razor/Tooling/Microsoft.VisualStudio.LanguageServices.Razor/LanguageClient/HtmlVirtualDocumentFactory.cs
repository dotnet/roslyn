// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Telemetry;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Razor.LanguageClient;

[Export(typeof(VirtualDocumentFactory))]
[ContentType(RazorConstants.RazorLSPContentTypeName)]
internal class HtmlVirtualDocumentFactory : VirtualDocumentFactoryBase
{
    private static IContentType? s_htmlLSPContentType;
    private readonly ITelemetryReporter _telemetryReporter;

    [ImportingConstructor]
    public HtmlVirtualDocumentFactory(
        IContentTypeRegistryService contentTypeRegistry,
        ITextBufferFactoryService textBufferFactory,
        ITextDocumentFactoryService textDocumentFactory,
        FileUriProvider filePathProvider,
        ITelemetryReporter telemetryReporter)
        : base(contentTypeRegistry, textBufferFactory, textDocumentFactory, filePathProvider)
    {
        _telemetryReporter = telemetryReporter;
    }

    protected override IContentType LanguageContentType
    {
        get
        {
            s_htmlLSPContentType ??= ContentTypeRegistry.GetContentType(RazorLSPConstants.HtmlLSPDelegationContentTypeName);

            return s_htmlLSPContentType;
        }
    }

    protected override string HostDocumentContentTypeName => RazorConstants.RazorLSPContentTypeName;
    protected override string LanguageFileNameSuffix => LanguageServerConstants.HtmlVirtualDocumentSuffix;
    protected override VirtualDocument CreateVirtualDocument(Uri uri, ITextBuffer textBuffer) => new HtmlVirtualDocument(uri, textBuffer, _telemetryReporter);
}
