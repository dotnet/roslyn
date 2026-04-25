// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.CodeActions;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.NestedFiles;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Remote.Razor;

internal sealed class RemoteAddNestedFileService(in ServiceArgs args)
    : RazorDocumentServiceBase(in args), IRemoteAddNestedFileService
{
    internal sealed class Factory : FactoryBase<IRemoteAddNestedFileService>
    {
        protected override IRemoteAddNestedFileService CreateService(in ServiceArgs args)
            => new RemoteAddNestedFileService(in args);
    }

    private readonly IRoslynCodeActionHelpers _roslynCodeActionHelpers =
        args.ExportProvider.GetExportedValue<IRoslynCodeActionHelpers>();

    private readonly LanguageServerFeatureOptions _languageServerFeatureOptions =
        args.ExportProvider.GetExportedValue<LanguageServerFeatureOptions>();

    public ValueTask<WorkspaceEdit?> GetNewNestedFileWorkspaceEditAsync(
        JsonSerializableRazorPinnedSolutionInfoWrapper solutionInfo,
        JsonSerializableDocumentId documentId,
        NestedFileKind fileKind,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            documentId,
            context => GetNewNestedFileWorkspaceEditAsync(context, fileKind, cancellationToken),
            cancellationToken);

    private async ValueTask<WorkspaceEdit?> GetNewNestedFileWorkspaceEditAsync(
        RemoteDocumentContext context,
        NestedFileKind fileKind,
        CancellationToken cancellationToken)
    {
        var razorFilePath = context.Snapshot.FilePath;
        if (GetNestedFilePath(razorFilePath, fileKind) is not string nestedFilePath)
        {
            return null;
        }

        var nestedFileUri = LspFactory.CreateFilePathUri(nestedFilePath, _languageServerFeatureOptions);

        var content = await GenerateContentAsync(
            fileKind, context, razorFilePath, nestedFileUri, cancellationToken).ConfigureAwait(false);

        var nestedFileDocumentIdentifier = new OptionalVersionedTextDocumentIdentifier
        {
            DocumentUri = new DocumentUri(nestedFileUri)
        };

        var documentChanges = new SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>[]
        {
            new CreateFile { DocumentUri = nestedFileDocumentIdentifier.DocumentUri },
            new TextDocumentEdit
            {
                TextDocument = nestedFileDocumentIdentifier,
                Edits = [LspFactory.CreateTextEdit(position: (0, 0), content)]
            }
        };

        return new WorkspaceEdit
        {
            DocumentChanges = documentChanges,
        };
    }

    private static string? GetNestedFilePath(string razorFilePath, NestedFileKind fileKind)
    {
        return fileKind switch
        {
            NestedFileKind.Css => razorFilePath + ".css",
            NestedFileKind.CSharp => razorFilePath + ".cs",
            NestedFileKind.JavaScript => razorFilePath + ".js",
            _ => null
        };
    }

    private async Task<string> GenerateContentAsync(
        NestedFileKind fileKind,
        RemoteDocumentContext documentContext,
        string razorFilePath,
        Uri nestedFileUri,
        CancellationToken cancellationToken)
    {
        return fileKind switch
        {
            NestedFileKind.CSharp => await GenerateCSharpContentAsync(
                documentContext, razorFilePath, nestedFileUri, cancellationToken).ConfigureAwait(false),
            NestedFileKind.Css => GenerateCssContent(razorFilePath),
            NestedFileKind.JavaScript => GenerateJavaScriptContent(razorFilePath),
            _ => string.Empty
        };
    }

    private static string GenerateCssContent(string razorFilePath)
    {
        var componentName = Path.GetFileNameWithoutExtension(razorFilePath);
        var fileType = FileKinds.GetFileKindFromPath(razorFilePath).IsLegacy() ? "view" : "component";
        return $"/* CSS for {componentName} {fileType} */\r\n";
    }

    private static string GenerateJavaScriptContent(string razorFilePath)
    {
        var componentName = Path.GetFileNameWithoutExtension(razorFilePath);
        var fileType = FileKinds.GetFileKindFromPath(razorFilePath).IsLegacy() ? "view" : "component";
        return $"// JavaScript for {componentName} {fileType}\r\n";
    }

    private async Task<string> GenerateCSharpContentAsync(
        RemoteDocumentContext documentContext,
        string razorFilePath,
        Uri nestedFileUri,
        CancellationToken cancellationToken)
    {
        var codeDocument = await documentContext.GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);
        var className = Path.GetFileNameWithoutExtension(razorFilePath);

        // Use the Razor compiler's namespace resolution which handles @namespace directives,
        // _Imports.razor, and SDK-provided root namespace
        if (!codeDocument.TryGetNamespace(fallbackToRootNamespace: true, out var ns) || ns.IsNullOrEmpty())
        {
            Logger.LogWarning($"Could not determine namespace for: {razorFilePath}");
            ns = "Unknown";
        }

        var content = GenerateCodeBehindClass(className, ns);

        // Format via Roslyn (handles file-scoped namespaces, indentation, etc.)
        content = await _roslynCodeActionHelpers.GetFormattedNewFileContentsAsync(
            documentContext.Snapshot.Project,
            nestedFileUri,
            content,
            cancellationToken).ConfigureAwait(false);

        return content;
    }

    private static string GenerateCodeBehindClass(string className, string namespaceName)
    {
        using var _ = StringBuilderPool.GetPooledObject(out var builder);

        builder.AppendLine($"namespace {namespaceName}");
        builder.AppendLine("{");
        builder.AppendLine($"public partial class {className}");
        builder.AppendLine("{");
        builder.AppendLine("}");
        builder.AppendLine("}");

        return builder.ToString();
    }
}
