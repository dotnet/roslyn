// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServer;
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
        (DocumentUri fileUri, string contents)[]? additionalExpectedFiles = null,
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

        var expectedChanges = (additionalExpectedFiles ?? []).Select(e => (e.fileUri, e.contents)).Concat([(document.GetURI(), expected)]);
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
        var (result, traceOutput) = await GetCodeActionsWithTraceAsync(document, input, makeDiagnosticsRequest);
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

            Code action trace:
            {Indent(traceOutput)}
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
        => (await GetCodeActionsWithTraceAsync(document, input, makeDiagnosticsRequest)).CodeActions;

    private async Task<CodeActionsWithTrace> GetCodeActionsWithTraceAsync(TextDocument document, TestCode input, bool makeDiagnosticsRequest = false)
    {
        var requestInvoker = new TestHtmlRequestInvoker();
        var endpoint = new CohostCodeActionsEndpoint(IncompatibleProjectService, RemoteServiceInvoker, ClientCapabilitiesService, requestInvoker, NoOpTelemetryReporter.Instance, LoggerFactory);
        var inputText = await document.GetTextAsync(DisposalToken);
        using var traceListener = new RecordingXunitTraceListener(TestOutputHelper);
        AddImportTraceTestAccessor.ClearBufferedMessages();

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
            TextDocument = new VSTextDocumentIdentifier { DocumentUri = document.GetURI() },
            Range = range,
            Context = new VSInternalCodeActionContext() { Diagnostics = diagnostics.ToArray() }
        };

        if (input.TryGetNamedSpans("selection", out var selectionSpans))
        {
            // Simulate VS range vs selection range
            request.Context.SelectionRange = inputText.GetRange(selectionSpans.Single());
        }

        Trace.Listeners.Add(traceListener);
        try
        {
            Trace.WriteLine($"RazorCodeActionTrace TestRequest: Document='{document.GetURI()}', MakeDiagnosticsRequest={makeDiagnosticsRequest}, Range='{FormatRange(request.Range)}', SelectionRange='{FormatRange(request.Context.SelectionRange)}', Diagnostics=[{FormatDiagnostics(request.Context.Diagnostics)}]");

            var result = await endpoint.GetTestAccessor().HandleRequestAsync(document, request, DisposalToken);

            Trace.WriteLine($"RazorCodeActionTrace TestResponse: Count={result?.Length.ToString() ?? "<null>"}, Actions=[{FormatCodeActions(result)}]");
            return new(result, AppendAddImportTrace(traceListener.TraceOutput, AddImportTraceTestAccessor.GetAndClearBufferedMessages()));
        }
        finally
        {
            Trace.Listeners.Remove(traceListener);
        }
    }

    private async Task<WorkspaceEdit> ResolveCodeActionAsync(CodeAnalysis.TextDocument document, CodeAction codeAction)
    {
        var requestInvoker = new TestHtmlRequestInvoker();
        var endpoint = new CohostCodeActionsResolveEndpoint(IncompatibleProjectService, RemoteServiceInvoker, ClientCapabilitiesService, requestInvoker);

        var result = await endpoint.GetTestAccessor().HandleRequestAsync(document, codeAction, DisposalToken);

        Assert.NotNull(result?.Edit);
        return result.Edit;
    }

    private readonly record struct CodeActionsWithTrace(SumType<Command, CodeAction>[]? CodeActions, string TraceOutput);

    private static string FormatCodeActions(SumType<Command, CodeAction>[]? codeActions)
    {
        if (codeActions is null)
        {
            return "<null>";
        }

        if (codeActions.Length == 0)
        {
            return "<empty>";
        }

        return string.Join("; ", codeActions.Select(static (codeAction, index) => $"{index}: {FormatCodeActionValue(codeAction.Value)}"));
    }

    private static string FormatCodeActionValue(object? value)
        => value switch
        {
            RazorVSInternalCodeAction razorAction => FormatRazorCodeAction(razorAction),
            VSInternalCodeAction vsAction => $"VSInternalCodeAction Title='{vsAction.Title}' Kind='{vsAction.Kind?.ToString() ?? "<null>"}' Data={FormatData(vsAction.Data)}",
            CodeAction action => $"CodeAction Title='{action.Title}' Kind='{action.Kind?.ToString() ?? "<null>"}' Data={FormatData(action.Data)}",
            Command command => $"Command Title='{command.Title}' Command='{command.CommandIdentifier}'",
            null => "<null>",
            _ => $"{value.GetType().FullName}: {value}"
        };

    private static string FormatRazorCodeAction(RazorVSInternalCodeAction action)
        => $"RazorVSInternalCodeAction Name='{action.Name ?? "<null>"}' Title='{action.Title}' Kind='{action.Kind?.ToString() ?? "<null>"}' Group='{action.Group ?? "<null>"}' Children={action.Children?.Length ?? 0} Command='{action.Command?.CommandIdentifier ?? "<null>"}' Data={FormatData(action.Data)}";

    private static string FormatData(object? data)
    {
        if (data is null)
        {
            return "<null>";
        }

        if (data is not JsonElement jsonData)
        {
            return data.GetType().FullName ?? data.GetType().Name;
        }

        var builder = new List<string>();
        builder.Add($"ValueKind={jsonData.ValueKind}");

        if (jsonData.TryGetProperty("CustomTags", out var customTags) &&
            customTags.ValueKind == JsonValueKind.Array)
        {
            builder.Add($"CustomTags=[{string.Join(", ", customTags.EnumerateArray().Select(static tag => tag.GetString()))}]");
        }

        if (jsonData.TryGetProperty("FixAllFlavors", out var fixAllFlavors) &&
            fixAllFlavors.ValueKind == JsonValueKind.Array)
        {
            builder.Add($"FixAllFlavors={fixAllFlavors.GetArrayLength()}");
        }

        if (jsonData.TryGetProperty("UniqueIdentifier", out var uniqueIdentifier))
        {
            builder.Add($"UniqueIdentifier='{uniqueIdentifier}'");
        }

        return $"JsonElement{{{string.Join(", ", builder)}}}";
    }

    private static string FormatDiagnostics(IEnumerable<LspDiagnostic>? diagnostics)
    {
        if (diagnostics is null)
        {
            return "<null>";
        }

        return string.Join("; ", diagnostics.Select(static (diagnostic, index) =>
            $"{index}: Code='{diagnostic.Code}', Severity='{diagnostic.Severity}', Range='{FormatRange(diagnostic.Range)}', Message='{diagnostic.Message}'"));
    }

    private static string FormatRange(LspRange? range)
        => range is null
            ? "<null>"
            : $"{range.Start.Line}:{range.Start.Character}-{range.End.Line}:{range.End.Character}";

    private static string Indent(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "    <no trace output captured>";
        }

        return "    " + value.Replace(Environment.NewLine, Environment.NewLine + "    ");
    }

    private static string AppendAddImportTrace(string traceOutput, string addImportTrace)
    {
        if (string.IsNullOrWhiteSpace(addImportTrace))
        {
            return traceOutput;
        }

        var builder = new StringBuilder(traceOutput);
        if (builder.Length > 0)
        {
            builder.AppendLine();
        }

        builder.AppendLine("Buffered AddImport trace:");
        builder.Append(addImportTrace);
        return builder.ToString();
    }

    private static class AddImportTraceTestAccessor
    {
        private const string TypeName = "Microsoft.CodeAnalysis.AddImport.AddImportTrace";
        private const BindingFlags MethodFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        public static void ClearBufferedMessages()
            => Invoke(nameof(ClearBufferedMessages));

        public static string GetAndClearBufferedMessages()
            => Invoke(nameof(GetAndClearBufferedMessages)) as string ?? string.Empty;

        private static object? Invoke(string methodName)
            => GetAddImportTraceType()?.GetMethod(methodName, MethodFlags)?.Invoke(null, null);

        private static Type? GetAddImportTraceType()
            => Type.GetType($"{TypeName}, Microsoft.CodeAnalysis.Features", throwOnError: false)
                ?? AppDomain.CurrentDomain.GetAssemblies()
                    .Select(static assembly => assembly.GetType(TypeName, throwOnError: false))
                    .FirstOrDefault(static type => type is not null);
    }

    private sealed class RecordingXunitTraceListener(ITestOutputHelper logger) : TraceListener
    {
        private readonly StringBuilder _traceOutput = new();
        private readonly StringBuilder _lineInProgress = new();
        private bool _disposed;

        public string TraceOutput => _traceOutput.ToString();

        public override bool IsThreadSafe
            => false;

        public override void Write(string? message)
            => _lineInProgress.Append(message);

        public override void WriteLine(string? message)
        {
            if (_disposed)
            {
                return;
            }

            var line = _lineInProgress.ToString() + message;
            logger.WriteLine(line);
            _traceOutput.AppendLine(line);
            _lineInProgress.Clear();
        }

        protected override void Dispose(bool disposing)
        {
            _disposed = true;
            base.Dispose(disposing);
        }
    }
}
