// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.Settings;
using Microsoft.CodeAnalysis.Razor.Workspaces.Settings;
using Microsoft.VisualStudio.Razor.LanguageClient.Cohost;
using Microsoft.VisualStudioCode.RazorExtension.Services;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudioCode.RazorExtension.Test;

public class RemoteClientSettingsServiceTest(ITestOutputHelper testOutput) : CohostEndpointTestBase(testOutput)
{
    [Fact]
    public async Task UpdateAsync_UpdatesOOPClientSettings()
    {
        var workspaceProvider = new VSCodeWorkspaceProvider();
        workspaceProvider.SetWorkspace(LocalWorkspace);

        var service = await InProcServiceFactory.CreateServiceAsync<IRemoteClientSettingsService>(
            new VSCodeBrokeredServiceInterceptor(),
            workspaceProvider,
            LoggerFactory);

        var expectedSettings = CreateTestSettings();

        await service.UpdateAsync(expectedSettings, DisposalToken);

        var remoteSettingsManager = OOPExportProvider.GetExportedValue<IClientSettingsManager>();
        Assert.Equal(expectedSettings, remoteSettingsManager.GetClientSettings());
    }

    [Fact]
    public async Task InitializerClientSettingsChangedPath_SyncsUpdatedSettingsToOOP()
    {
        var workspaceProvider = new VSCodeWorkspaceProvider();
        workspaceProvider.SetWorkspace(LocalWorkspace);

        var initializer = new VSCodeRemoteServicesInitializer(
            new VSCodeLanguageServerFeatureOptions(),
            SemanticTokensLegendService,
            workspaceProvider,
            ClientSettingsManager,
            LoggerFactory);

        await initializer.StartupAsync(ClientCapabilitiesService.ClientCapabilities, requestContext: default, DisposalToken);

        var expectedSettings = CreateTestSettings();
        ApplySettings(ClientSettingsManager, expectedSettings);

        var remoteSettingsManager = OOPExportProvider.GetExportedValue<IClientSettingsManager>();
        await WaitForRemoteSettingsAsync(remoteSettingsManager, expectedSettings);
    }

    private static ClientSettings CreateTestSettings()
        => new(
            new ClientSpaceSettings(IndentWithTabs: true, IndentSize: 2),
            new ClientCompletionSettings(AutoShowCompletion: false, AutoListParams: false),
            new ClientAdvancedSettings(
                FormatOnType: false,
                AutoClosingTags: false,
                AutoInsertAttributeQuotes: false,
                ColorBackground: true,
                CodeBlockBraceOnNextLine: true,
                AttributeIndentStyle: AttributeIndentStyle.IndentByOne,
                CommitElementsWithSpace: false,
                SnippetSetting: SnippetSetting.None,
                LogLevel: LogLevel.Trace,
                FormatOnPaste: false,
                TaskListDescriptors: ["TODO", "HACK"]));

    private static void ApplySettings(IClientSettingsManager clientSettingsManager, ClientSettings settings)
    {
        clientSettingsManager.Update(settings.ClientSpaceSettings);
        clientSettingsManager.Update(settings.ClientCompletionSettings);
        clientSettingsManager.Update(settings.AdvancedSettings);
    }

    private async Task WaitForRemoteSettingsAsync(IClientSettingsManager remoteSettingsManager, ClientSettings expectedSettings)
    {
        for (var i = 0; i < 100; i++)
        {
            if (expectedSettings.Equals(remoteSettingsManager.GetClientSettings()))
            {
                return;
            }

            await Task.Delay(10, DisposalToken);
        }

        Assert.Equal(expectedSettings, remoteSettingsManager.GetClientSettings());
    }
}
