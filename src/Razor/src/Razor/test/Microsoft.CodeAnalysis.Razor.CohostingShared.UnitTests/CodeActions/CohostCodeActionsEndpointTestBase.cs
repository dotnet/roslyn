// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Protocol.CodeActions;
using Microsoft.CodeAnalysis.Razor.Telemetry;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Remote.Razor;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost.CodeActions;

public abstract class CohostCodeActionsEndpointTestBase(ITestOutputHelper testOutputHelper) : CohostEndpointTestBase(testOutputHelper)
{
    private protected async Task VerifyCodeActionAsync(
        TestCode input,
        string? expected,
        string codeActionName,
        int? codeActionIndex = null,
        int childActionIndex = 0,
        RazorFileKind? fileKind = null,
        string? documentFilePath = null,
        (string filePath, string contents)[]? additionalFiles = null,
        (Uri fileUri, string contents)[]? additionalExpectedFiles = null,
        bool addDefaultImports = true,
        bool makeDiagnosticsRequest = false)
    {
        var document = CreateRazorDocument(input, fileKind, documentFilePath, additionalFiles, addDefaultImports: addDefaultImports);

        var codeAction = await VerifyCodeActionRequestAsync(document, input, codeActionName, codeActionIndex, childActionIndex, expectOffer: expected is not null, makeDiagnosticsRequest);

        if (codeAction is null)
        {
            Assert.Null(expected);
            return;
        }

        Assert.NotNull(expected);

        var workspaceEdit = codeAction.Data is null
            ? codeAction.Edit.AssumeNotNull()
            : await ResolveCodeActionAsync(document, codeAction);

        var expectedChanges = (additionalExpectedFiles ?? []).Concat([(document.CreateUri(), expected)]);
        await workspaceEdit.AssertWorkspaceEditAsync(document.Project.Solution, expectedChanges, DisposalToken);
    }

    private protected TextDocument CreateRazorDocument(TestCode input, RazorFileKind? fileKind = null, string? documentFilePath = null, (string filePath, string contents)[]? additionalFiles = null, bool addDefaultImports = true)
    {
        var fileSystem = (RemoteFileSystem)OOPExportProvider.GetExportedValue<IFileSystem>();
        fileSystem.GetTestAccessor().SetFileSystem(new TestFileSystem(additionalFiles));

        UpdateClientLSPInitializationOptions(options =>
        {
            options.ClientCapabilities.TextDocument = new()
            {
                CodeAction = new()
                {
                    ResolveSupport = new()
                }
            };

            return options;
        });

        return CreateProjectAndRazorDocument(input.Text, fileKind, documentFilePath, additionalFiles: additionalFiles, addDefaultImports: addDefaultImports);
    }

    private async Task<CodeAction?> VerifyCodeActionRequestAsync(TextDocument document, TestCode input, string codeActionName, int? codeActionIndex, int childActionIndex, bool expectOffer, bool makeDiagnosticsRequest)
    {
        var result = await GetCodeActionsAsync(document, input, makeDiagnosticsRequest);
        if (result is null)
        {
            return null;
        }

        var codeActions = result.Where(e => ((RazorVSInternalCodeAction)e.Value!).Name == codeActionName).ToArray();

        if (codeActions.Length > 1 && !codeActionIndex.HasValue)
        {
            Assert.Fail($"Multiple code actions with name '{codeActionName}' were found. Specify a codeActionIndex to disambiguate.");
            return null;
        }

        var index = codeActionIndex ?? 0;
        var codeActionToRun = codeActions.Length > index
            ? (VSInternalCodeAction?)codeActions[index]
            : null;

        if (!expectOffer)
        {
            Assert.Null(codeActionToRun);
            return null;
        }

        AssertEx.NotNull(codeActionToRun, $"""
            Could not find {(codeActionIndex is null ? "single" : $"index {codeActionIndex}")} code action with name '{codeActionName}'.

            Available:
                {string.Join(Environment.NewLine + "    ", result.Select(e => ((RazorVSInternalCodeAction)e.Value!).Name))}
            """);

        // In VS, child code actions use the children property, and are easy
        if (codeActionToRun.Children?.Length > 0)
        {
            codeActionToRun = codeActionToRun.Children[childActionIndex];
        }

        // In VS Code, the C# extension has some custom code to handle child code actions, which we mimic here
        if (codeActionToRun.Command is { CommandIdentifier: "roslyn.client.nestedCodeAction", Arguments: [JsonObject data] })
        {
            var nestedCodeAction = data["NestedCodeActions"].AssumeNotNull().AsArray()[childActionIndex];
            codeActionToRun = JsonSerializer.Deserialize<VSInternalCodeAction>(nestedCodeAction, JsonHelpers.JsonSerializerOptions);
        }

        Assert.NotNull(codeActionToRun);
        return codeActionToRun;
    }

    private protected async Task<SumType<Command, CodeAction>[]?> GetCodeActionsAsync(TextDocument document, TestCode input, bool makeDiagnosticsRequest = false)
    {
        var requestInvoker = new TestHtmlRequestInvoker();
        var endpoint = new CohostCodeActionsEndpoint(IncompatibleProjectService, RemoteServiceInvoker, ClientCapabilitiesService, requestInvoker, NoOpTelemetryReporter.Instance);
        var inputText = await document.GetTextAsync(DisposalToken);

        using var diagnostics = new PooledArrayBuilder<LspDiagnostic>();

        if (makeDiagnosticsRequest)
        {
            // If we're making a diagnostics request, we're going to ignore any hard coded diagnostics, so make sure there aren't
            // any to avoid false negatives/positives.
            Assert.DoesNotContain(input.NamedSpans, kvp => kvp.Key.Length > 0);

            var result = await CohostDocumentPullDiagnosticsTest.MakeDiagnosticsRequestAsync(document, taskListRequest: false, requestInvoker, IncompatibleProjectService, RemoteServiceInvoker, ClientCapabilitiesService, LoggerFactory, DisposalToken);

            diagnostics.AddRange(result);
        }
        else
        {
            foreach (var (code, spans) in input.NamedSpans)
            {
                if (code.Length == 0)
                {
                    continue;
                }

                foreach (var diagnosticSpan in spans)
                {
                    diagnostics.Add(new LspDiagnostic
                    {
                        Code = code,
                        Range = inputText.GetRange(diagnosticSpan)
                    });
                }
            }
        }

        var range = input.HasSpans
            ? inputText.GetRange(input.Span)
            : inputText.GetRange(input.Position, input.Position);

        var request = new VSCodeActionParams
        {
            TextDocument = new VSTextDocumentIdentifier { DocumentUri = document.CreateDocumentUri() },
            Range = range,
            Context = new VSInternalCodeActionContext() { Diagnostics = diagnostics.ToArray() }
        };

        if (input.TryGetNamedSpans("selection", out var selectionSpans))
        {
            // Simulate VS range vs selection range
            request.Context.SelectionRange = inputText.GetRange(selectionSpans.Single());
        }

        return await endpoint.GetTestAccessor().HandleRequestAsync(document, request, DisposalToken);
    }

    private async Task<WorkspaceEdit> ResolveCodeActionAsync(CodeAnalysis.TextDocument document, CodeAction codeAction)
    {
        var requestInvoker = new TestHtmlRequestInvoker();
        var endpoint = new CohostCodeActionsResolveEndpoint(IncompatibleProjectService, RemoteServiceInvoker, ClientCapabilitiesService, requestInvoker);

        var result = await endpoint.GetTestAccessor().HandleRequestAsync(document, codeAction, DisposalToken);

        Assert.NotNull(result?.Edit);
        return result.Edit;
    }
}
