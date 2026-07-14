// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Utilities;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Razor.Extensions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Razor.LanguageClient;

[Name(nameof(ViewCodeCommandHandler))]
[Export(typeof(ICommandHandler))]
[ContentType(RazorConstants.RazorLSPContentTypeName)]
[ContentType(RazorLSPConstants.CSharpContentTypeName)]
[method: ImportingConstructor]
internal sealed partial class ViewCodeCommandHandler(
    [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
    ITextDocumentFactoryService textDocumentFactoryService,
    JoinableTaskContext joinableTaskContext) : ICommandHandler<ViewCodeCommandArgs>
{
    private static readonly CommandState s_availableCommandState = new(isAvailable: true, displayText: SR.View_Code);
    private static readonly CommandState s_hiddenAvailableCommandState = new(
        isAvailable: true,
        isChecked: false,
        isEnabled: true,
        isVisible: false);

    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ITextDocumentFactoryService _textDocumentFactoryService = textDocumentFactoryService;
    private readonly JoinableTaskContext _joinableTaskContext = joinableTaskContext;

    private readonly FileExistsHelper _helper = new();

    public string DisplayName => nameof(ViewCodeCommandHandler);

    public CommandState GetCommandState(ViewCodeCommandArgs args)
    {
        if (TryGetTargetFilePath(args.SubjectBuffer, out _, out var isVisible))
        {
            return isVisible ? s_availableCommandState : s_hiddenAvailableCommandState;
        }

        return CommandState.Unavailable;
    }

    public bool ExecuteCommand(ViewCodeCommandArgs args, CommandExecutionContext executionContext)
    {
        if (TryGetTargetFilePath(args.SubjectBuffer, out var targetFilePath, out _))
        {
            VsShellUtilities.OpenDocument(_serviceProvider, targetFilePath);
            return true;
        }

        return false;
    }

    private bool TryGetTargetFilePath(
        ITextBuffer buffer,
        [NotNullWhen(true)] out string? targetFilePath,
        out bool isVisible)
    {
        // Command state checks and execution should always happen on the main thread.
        // However, if that changes, we should assert because our FileExistsHelper will likely be corrupted.
        _joinableTaskContext.AssertUIThread();

        if (_textDocumentFactoryService.TryGetTextDocument(buffer, out var document) &&
            document?.FilePath is string filePath)
        {
            if (TryGetCSharpFilePath(filePath, out targetFilePath) ||
                TryGetRazorFilePath(filePath, out targetFilePath))
            {
                isVisible = FileUtilities.IsAnyRazorFilePath(filePath, StringComparison.OrdinalIgnoreCase);
                return true;
            }
        }

        targetFilePath = null;
        isVisible = false;
        return false;
    }

    private bool TryGetCSharpFilePath(string filePath, [NotNullWhen(true)] out string? codeFilePath)
    {
        // Exclude imports files — they don't have nested code files.
        if (FileUtilities.IsAnyRazorFilePath(filePath, StringComparison.OrdinalIgnoreCase)
            && Path.GetFileName(filePath) is string fileName
            && !string.Equals(fileName, ComponentHelpers.ImportsFileName, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(fileName, MvcImportProjectFeature.ImportsFileName, StringComparison.OrdinalIgnoreCase))
        {
            codeFilePath = filePath + RazorLSPConstants.CSharpFileExtension;
            return _helper.FileExists(codeFilePath);
        }

        codeFilePath = null;
        return false;
    }

    private bool TryGetRazorFilePath(string filePath, [NotNullWhen(true)] out string? razorFilePath)
    {
        if (!filePath.EndsWith(RazorLSPConstants.RazorFileExtension + RazorLSPConstants.CSharpFileExtension, StringComparison.OrdinalIgnoreCase) &&
            !filePath.EndsWith(RazorLSPConstants.CSHTMLFileExtension + RazorLSPConstants.CSharpFileExtension, StringComparison.OrdinalIgnoreCase))
        {
            razorFilePath = null;
            return false;
        }

        razorFilePath = filePath[..^RazorLSPConstants.CSharpFileExtension.Length];
        if (FileUtilities.IsAnyRazorFilePath(razorFilePath, StringComparison.OrdinalIgnoreCase) &&
            _helper.FileExists(razorFilePath))
        {
            return true;
        }

        razorFilePath = null;
        return false;
    }
}
