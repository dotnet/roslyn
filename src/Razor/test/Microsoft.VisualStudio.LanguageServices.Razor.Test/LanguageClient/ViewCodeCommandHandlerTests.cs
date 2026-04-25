// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.CodeDom.Compiler;
using System.IO;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient;

public class ViewCodeCommandHandlerTests(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    [UIFact]
    public void RazorFile_Available()
    {
        using var _ = CreateTestFiles("test.razor", out var razorFilePath);

        var (handler, args) = CreateHandlerAndArgs(razorFilePath);

        var result = handler.GetCommandState(args);

        Assert.True(result.IsAvailable);
    }

    [UIFact]
    public void CsHtmlFile_Available()
    {
        using var _ = CreateTestFiles("test.cshtml", out var razorFilePath);

        var (handler, args) = CreateHandlerAndArgs(razorFilePath);

        var result = handler.GetCommandState(args);

        Assert.True(result.IsAvailable);
    }

    [UIFact]
    public void RazorFile_Cached_Available()
    {
        using var files = CreateTestFiles("test.razor", out var razorFilePath);

        var (handler, args) = CreateHandlerAndArgs(razorFilePath);

        var result = handler.GetCommandState(args);

        Assert.True(result.IsAvailable);

        files.Delete();

        // Even though the file doesn't exist now, we should still be available because the result is cached
        Assert.True(result.IsAvailable);
        Assert.False(File.Exists(razorFilePath + ".cs"), "The premise of this test is bad and it should feel bad");
    }

    [UIFact]
    public void NonRazorFile_NotAvailable()
    {
        using var _ = CreateTestFiles("test.daveswebframework", out var razorFilePath);

        var (handler, args) = CreateHandlerAndArgs(razorFilePath);

        var result = handler.GetCommandState(args);

        Assert.False(result.IsAvailable);
    }

    [UIFact]
    public void RazorFile_NoCSharpFile_NotAvailable()
    {
        var razorFilePath = "nonexistent.razor";

        var (handler, args) = CreateHandlerAndArgs(razorFilePath);

        var result = handler.GetCommandState(args);

        Assert.False(result.IsAvailable);
    }

    [UIFact]
    public void ImportsRazorFile_NotAvailable()
    {
        using var _ = CreateTestFiles("_Imports.razor", out var razorFilePath);

        var (handler, args) = CreateHandlerAndArgs(razorFilePath);

        var result = handler.GetCommandState(args);

        Assert.False(result.IsAvailable);
    }

    [UIFact]
    public void ViewImportsCshtmlFile_NotAvailable()
    {
        using var _ = CreateTestFiles("_ViewImports.cshtml", out var razorFilePath);

        var (handler, args) = CreateHandlerAndArgs(razorFilePath);

        var result = handler.GetCommandState(args);

        Assert.False(result.IsAvailable);
    }

    private (ViewCodeCommandHandler, ViewCodeCommandArgs) CreateHandlerAndArgs(string razorFilePath)
    {
        var textBuffer = StrictMock.Of<ITextBuffer>();
        var textDocument = StrictMock.Of<ITextDocument>(doc
            => doc.FilePath == razorFilePath);
        var textDocumentFactory = StrictMock.Of<ITextDocumentFactoryService>(factory =>
            factory.TryGetTextDocument(textBuffer, out textDocument) == true);

        var serviceProvider = StrictMock.Of<IServiceProvider>();

        var handler = new ViewCodeCommandHandler(serviceProvider, textDocumentFactory, JoinableTaskContext);

        var textView = Mock.Of<ITextView>(MockBehavior.Strict);
        var args = new ViewCodeCommandArgs(textView, textBuffer);

        return (handler, args);
    }

    private static TempFileCollection CreateTestFiles(string razorFileName, out string razorFilePath)
    {
        var files = new TempFileCollection();
        razorFilePath = Path.Combine(files.TempDir, razorFileName);
        var csharpFilePath = razorFilePath + ".cs";

        // Create our temp file
        File.WriteAllText(csharpFilePath, "");

        // Add it to the list so it gets cleaned up
        files.AddFile(csharpFilePath, false);

        return files;
    }
}
