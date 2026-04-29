// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Test;
using Microsoft.AspNetCore.Razor.Test.Common.Mef;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.SemanticTokens;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Razor.Workspaces.Settings;
using Microsoft.CodeAnalysis.Remote.Razor;
using Microsoft.VisualStudioCode.RazorExtension.Configuration;
using Microsoft.VisualStudioCode.RazorExtension.Services;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

public abstract class CohostEndpointTestBase(ITestOutputHelper testOutputHelper) : CohostTestBase(testOutputHelper)
{
    private VSCodeRemoteServiceInvoker? _remoteServiceInvoker;
    private IFilePathService? _filePathService;
    private ISemanticTokensLegendService? _semanticTokensLegendService;

    private protected override IRemoteServiceInvoker RemoteServiceInvoker => _remoteServiceInvoker.AssumeNotNull();
    private protected override IFilePathService FilePathService => _filePathService.AssumeNotNull();
    private protected ISemanticTokensLegendService SemanticTokensLegendService => _semanticTokensLegendService.AssumeNotNull();

    private protected override TestComposition LocalComposition => TestComposition.RoslynFeatures;

    protected override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        InProcServiceFactory.TestAccessor.SetExportProvider(OOPExportProvider);

        var workspaceProvider = new VSCodeWorkspaceProvider();
        workspaceProvider.SetWorkspace(LocalWorkspace);

        _remoteServiceInvoker = new VSCodeRemoteServiceInvoker(workspaceProvider, LoggerFactory);
        AddDisposable(_remoteServiceInvoker);

        _filePathService = new VSCodeFilePathService();

        _semanticTokensLegendService = new CohostSemanticTokensLegendService(new TestClientCapabilitiesService(new VSInternalClientCapabilities() { SupportsVisualStudioExtensions = false }));
    }

    private protected override IClientSettingsManager CreateClientSettingsManager() => new ClientSettingsManager();

    private protected override RemoteClientLSPInitializationOptions GetRemoteClientLSPInitializationOptions()
    {
        return new()
        {
            ClientCapabilities = new ClientCapabilities()
            {
                TextDocument = new TextDocumentClientCapabilities
                {
                    Completion = new CompletionSetting
                    {
                        CompletionItem = new CompletionItemSetting(),
                        CompletionItemKind = new CompletionItemKindSetting()
                        {
                            ValueSet = Enum.GetValues<CompletionItemKind>(),
                        },
                        CompletionListSetting = new CompletionListSetting()
                        {
                            ItemDefaults = ["commitCharacters", "editRange", "insertTextFormat", "data"]
                        },
                        ContextSupport = false,
                        InsertTextMode = InsertTextMode.AsIs,
                    }
                }
            }.ToVSInternalClientCapabilities(),
            TokenModifiers = [],
            TokenTypes = []
        };
    }

    private protected override TextDocument CreateProjectAndRazorDocument(
        string contents,
        RazorFileKind? fileKind = null,
        string? documentFilePath = null,
        (string fileName, string contents)[]? additionalFiles = null,
        bool inGlobalNamespace = false,
        bool miscellaneousFile = false,
        bool addDefaultImports = true,
        Action<RazorProjectBuilder>? projectConfigure = null)
    {
        return CreateProjectAndRazorDocument(LocalWorkspace, contents, fileKind, documentFilePath, additionalFiles, inGlobalNamespace, miscellaneousFile, addDefaultImports, projectConfigure);
    }
}
