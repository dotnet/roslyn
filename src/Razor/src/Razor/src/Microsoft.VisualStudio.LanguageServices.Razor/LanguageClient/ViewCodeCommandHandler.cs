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
[method: ImportingConstructor]
internal sealed partial class ViewCodeCommandHandler(
    [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
    ITextDocumentFactoryService textDocumentFactoryService,
    JoinableTaskContext joinableTaskContext) : ICommandHandler<ViewCodeCommandArgs>
{
    private static readonly CommandState s_availableCommandState = new(isAvailable: true, displayText: SR.View_Code);

    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ITextDocumentFactoryService _textDocumentFactoryService = textDocumentFactoryService;
    private readonly JoinableTaskContext _joinableTaskContext = joinableTaskContext;

    private readonly FileExistsHelper _helper = new();

    public string DisplayName => nameof(ViewCodeCommandHandler);

    public CommandState GetCommandState(ViewCodeCommandArgs args)
    {
        if (TryGetCSharpFilePath(args.SubjectBuffer, out _))
        {
            return s_availableCommandState;
        }

        return CommandState.Unavailable;
    }

    public bool ExecuteCommand(ViewCodeCommandArgs args, CommandExecutionContext executionContext)
    {
        if (TryGetCSharpFilePath(args.SubjectBuffer, out var csharpFilePath))
        {
            VsShellUtilities.OpenDocument(_serviceProvider, csharpFilePath);
            return true;
        }

        return false;
    }

    private bool TryGetCSharpFilePath(ITextBuffer buffer, [NotNullWhen(true)] out string? codeFilePath)
    {
        // Command state checks and execution should always happen on the main thread.
        // However, if that changes, we should assert because our FileExistsHelper will likely be corrupted.
        _joinableTaskContext.AssertUIThread();

        // Exclude imports files — they don't have nested code files.
        if (_textDocumentFactoryService.TryGetTextDocument(buffer, out var document)
            && document?.FilePath is string filePath
            && FileUtilities.IsAnyRazorFilePath(filePath, StringComparison.OrdinalIgnoreCase)
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
}
