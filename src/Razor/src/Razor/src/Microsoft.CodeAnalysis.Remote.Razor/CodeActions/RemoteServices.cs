// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.CodeActions;
using Microsoft.CodeAnalysis.Razor.CodeActions.Razor;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Razor.Workspaces.Settings;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Remote.Razor.CodeActions;

// Services

[Export(typeof(ICodeActionsService)), Shared]
[method: ImportingConstructor]
internal sealed class OOPCodeActionsService(
    IDocumentMappingService documentMappingService,
    [ImportMany] IEnumerable<IRazorCodeActionProvider> razorCodeActionProviders,
    [ImportMany] IEnumerable<ICSharpCodeActionProvider> csharpCodeActionProviders,
    [ImportMany] IEnumerable<IHtmlCodeActionProvider> htmlCodeActionProviders,
    LanguageServerFeatureOptions languageServerFeatureOptions)
    : CodeActionsService(documentMappingService, razorCodeActionProviders, csharpCodeActionProviders, htmlCodeActionProviders, languageServerFeatureOptions);

[Export(typeof(ICodeActionResolveService)), Shared]
[method: ImportingConstructor]
internal sealed class OOPCodeActionResolveService(
    [ImportMany] IEnumerable<IRazorCodeActionResolver> razorCodeActionResolvers,
    [ImportMany] IEnumerable<ICSharpCodeActionResolver> csharpCodeActionResolvers,
    [ImportMany] IEnumerable<IHtmlCodeActionResolver> htmlCodeActionResolvers,
    IClientSettingsManager clientSettingsManager,
    ILoggerFactory loggerFactory)
    : CodeActionResolveService(razorCodeActionResolvers, csharpCodeActionResolvers, htmlCodeActionResolvers, clientSettingsManager, loggerFactory);

// Code Action Providers

[Export(typeof(IRazorCodeActionProvider)), Shared]
[method: ImportingConstructor]
internal sealed class OOPExtractToCssCodeActionProvider(ILoggerFactory loggerFactory) : ExtractToCssCodeActionProvider(loggerFactory);

[Export(typeof(IRazorCodeActionProvider)), Shared]
[method: ImportingConstructor]
internal sealed class OOPExtractToCodeBehindCodeActionProvider(ILoggerFactory loggerFactory) : ExtractToCodeBehindCodeActionProvider(loggerFactory);

[Export(typeof(IRazorCodeActionProvider)), Shared]
internal sealed class OOPExtractToComponentCodeActionProvider : ExtractToComponentCodeActionProvider;

[Export(typeof(IRazorCodeActionProvider)), Shared]
internal sealed class OOPSimplifyTagToSelfClosingCodeActionProvider : SimplifyTagToSelfClosingCodeActionProvider;

[Export(typeof(IRazorCodeActionProvider)), Shared]
internal sealed class OOPSimplifyFullyQualifiedComponentCodeActionProvider : SimplifyFullyQualifiedComponentCodeActionProvider;

[Export(typeof(IRazorCodeActionProvider)), Shared]
[method: ImportingConstructor]
internal sealed class OOPComponentAccessibilityCodeActionProvider(IFileSystem fileSystem) : ComponentAccessibilityCodeActionProvider(fileSystem);

[Export(typeof(IRazorCodeActionProvider)), Shared]
internal sealed class OOPUnboundDirectiveAttributeAddUsingCodeActionProvider : UnboundDirectiveAttributeAddUsingCodeActionProvider;

[Export(typeof(IRazorCodeActionProvider)), Shared]
internal sealed class OOPGenerateEventHandlerCodeActionProvider : GenerateEventHandlerCodeActionProvider;

[Export(typeof(IRazorCodeActionProvider)), Shared]
internal sealed class OOPPromoteUsingDirectiveCodeActionProvider : PromoteUsingCodeActionProvider;

[Export(typeof(IRazorCodeActionProvider)), Shared]
internal sealed class OOPRemoveUnnecessaryDirectivesCodeActionProvider : RemoveUnnecessaryDirectivesCodeActionProvider;

[Export(typeof(IRazorCodeActionProvider)), Shared]
internal sealed class OOPWrapAttributesCodeActionProvider : WrapAttributesCodeActionProvider;

[Export(typeof(IRazorCodeActionProvider)), Shared]
internal sealed class OOPSortAndConsolidateUsingsCodeActionProvider : SortAndConsolidateUsingsCodeActionProvider;

[Export(typeof(ICSharpCodeActionProvider)), Shared]
internal sealed class OOPTypeAccessibilityCodeActionProvider : TypeAccessibilityCodeActionProvider;

[Export(typeof(ICSharpCodeActionProvider)), Shared]
[method: ImportingConstructor]
internal sealed class OOPDefaultCSharpCodeActionProvider(LanguageServerFeatureOptions languageServerFeatureOptions) : CSharpCodeActionProvider(languageServerFeatureOptions);

[Export(typeof(IHtmlCodeActionProvider)), Shared]
[method: ImportingConstructor]
internal sealed class OOPDefaultHtmlCodeActionProvider(IRazorEditService razorEditService) : HtmlCodeActionProvider(razorEditService);

// Code Action Resolvers

[Export(typeof(IRazorCodeActionResolver)), Shared]
[method: ImportingConstructor]
internal sealed class OOPExtractToCssCodeActionResolver(LanguageServerFeatureOptions languageServerFeatureOptions, IFileSystem fileSystem)
    : ExtractToCssCodeActionResolver(languageServerFeatureOptions, fileSystem);

[Export(typeof(IRazorCodeActionResolver)), Shared]
[method: ImportingConstructor]
internal sealed class OOPExtractToCodeBehindCodeActionResolver(
    LanguageServerFeatureOptions languageServerFeatureOptions,
    IRoslynCodeActionHelpers roslynCodeActionHelpers)
    : ExtractToCodeBehindCodeActionResolver(languageServerFeatureOptions, roslynCodeActionHelpers);

[Export(typeof(IRazorCodeActionResolver)), Shared]
[method: ImportingConstructor]
internal sealed class OOPExtractToComponentCodeActionResolver(LanguageServerFeatureOptions languageServerFeatureOptions) : ExtractToComponentCodeActionResolver(languageServerFeatureOptions);

[Export(typeof(IRazorCodeActionResolver)), Shared]
internal sealed class OOPSimplifyTagToSelfClosingCodeActionResolver : SimplifyTagToSelfClosingCodeActionResolver;

[Export(typeof(IRazorCodeActionResolver)), Shared]
internal sealed class OOPSimplifyFullyQualifiedComponentCodeActionResolver : SimplifyFullyQualifiedComponentCodeActionResolver;

[Export(typeof(IRazorCodeActionResolver)), Shared]
[method: ImportingConstructor]
internal sealed class OOPCreateComponentCodeActionResolver(LanguageServerFeatureOptions languageServerFeatureOptions) : CreateComponentCodeActionResolver(languageServerFeatureOptions);

[Export(typeof(IRazorCodeActionResolver)), Shared]
internal sealed class OOPAddUsingsCodeActionResolver : AddUsingsCodeActionResolver;

[Export(typeof(IRazorCodeActionResolver)), Shared]
[method: ImportingConstructor]
internal sealed class OOPPromoteUsingDirectiveCodeActionResolver(IFileSystem fileSystem) : PromoteUsingCodeActionResolver(fileSystem);

[Export(typeof(IRazorCodeActionResolver)), Shared]
internal sealed class OOPRemoveUnnecessaryDirectivesCodeActionResolver : RemoveUnnecessaryDirectivesCodeActionResolver;

[Export(typeof(IRazorCodeActionResolver)), Shared]
internal sealed class OOPWrapAttributesCodeActionResolver : WrapAttributesCodeActionResolver;

[Export(typeof(IRazorCodeActionResolver)), Shared]
internal sealed class OOPSortAndConsolidateUsingsCodeActionResolver : SortAndConsolidateUsingsCodeActionResolver;

[Export(typeof(ICSharpCodeActionResolver)), Shared]
[method: ImportingConstructor]
internal sealed class OOPCSharpCodeActionResolver(
    IRazorFormattingService razorFormattingService,
    IClientSettingsManager clientSettingsManager,
    IFilePathService filePathService,
    RemoteSnapshotManager snapshotManager,
    ILoggerFactory loggerFactory)
    : CSharpCodeActionResolver(razorFormattingService, clientSettingsManager, filePathService, loggerFactory)
{
    private readonly RemoteSnapshotManager _snapshotManager = snapshotManager;

    protected override async Task<DocumentContext?> CreateDocumentContextAsync(IDocumentSnapshot originDocumentSnapshot, Uri generatedDocumentUri, CancellationToken cancellationToken)
    {
        if (originDocumentSnapshot is not RemoteDocumentSnapshot remoteDocumentSnapshot)
        {
            throw new InvalidOperationException($"{nameof(OOPCSharpCodeActionResolver)} can only be used with {nameof(RemoteDocumentSnapshot)} instances.");
        }

        var razorDocument = await _snapshotManager.TryGetRazorDocumentAsync(
            remoteDocumentSnapshot.TextDocument.Project.Solution,
            generatedDocumentUri,
            cancellationToken).ConfigureAwait(false);
        if (razorDocument is null)
        {
            return null;
        }

        var razorDocumentSnapshot = _snapshotManager.GetSnapshot(razorDocument);
        return new RemoteDocumentContext(razorDocument.CreateUri(), razorDocumentSnapshot);
    }
}

[Export(typeof(ICSharpCodeActionResolver)), Shared]
[method: ImportingConstructor]
internal sealed class OOPUnformattedRemappingCSharpCodeActionResolver(IDocumentMappingService documentMappingService) : UnformattedRemappingCSharpCodeActionResolver(documentMappingService);

[Export(typeof(IHtmlCodeActionResolver)), Shared]
[method: ImportingConstructor]
internal sealed class OOPHtmlCodeActionResolver(IRazorEditService razorEditService) : HtmlCodeActionResolver(razorEditService);
