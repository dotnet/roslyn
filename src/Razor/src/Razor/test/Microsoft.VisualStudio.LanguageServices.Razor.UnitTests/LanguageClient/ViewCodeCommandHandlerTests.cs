// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.CodeDom.Compiler;
using System.IO;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
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
        Assert.True(result.IsEnabled);
        Assert.True(result.IsVisible);
    }

    [UIFact]
    public void CsHtmlFile_Available()
    {
        using var _ = CreateTestFiles("test.cshtml", out var razorFilePath);

        var (handler, args) = CreateHandlerAndArgs(razorFilePath);

        var result = handler.GetCommandState(args);

        Assert.True(result.IsAvailable);
        Assert.True(result.IsEnabled);
        Assert.True(result.IsVisible);
    }

    [UIFact]
    public void RazorFile_Cached_Available()
    {
        using var files = CreateTestFiles("test.razor", out var razorFilePath);

        var (handler, args) = CreateHandlerAndArgs(razorFilePath);

        var result = handler.GetCommandState(args);

        Assert.True(result.IsAvailable);
        Assert.True(result.IsEnabled);
        Assert.True(result.IsVisible);

        files.Delete();

        // Even though the file doesn't exist now, we should still be available because the result is cached
        result = handler.GetCommandState(args);

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

    [UIFact]
    public void RazorCodeBehindFile_Available()
    {
        using var _ = CreateTestFiles("test.razor", out var razorFilePath);

        var (handler, args) = CreateHandlerAndArgs(razorFilePath + ".cs");

        var result = handler.GetCommandState(args);

        Assert.True(result.IsAvailable);
        Assert.True(result.IsEnabled);
        Assert.False(result.IsVisible);
    }

    [UIFact]
    public void CsHtmlCodeBehindFile_Available()
    {
        using var _ = CreateTestFiles("test.cshtml", out var razorFilePath);

        var (handler, args) = CreateHandlerAndArgs(razorFilePath + ".cs");

        var result = handler.GetCommandState(args);

        Assert.True(result.IsAvailable);
        Assert.True(result.IsEnabled);
        Assert.False(result.IsVisible);
    }

    [UIFact]
    public void RazorCodeBehindFile_NoRazorFile_NotAvailable()
    {
        using var files = new TempFileCollection();
        var razorFilePath = Path.Combine(files.TempDir, "test.razor");
        var csharpFilePath = razorFilePath + ".cs";
        File.WriteAllText(csharpFilePath, "");
        files.AddFile(csharpFilePath, false);

        var (handler, args) = CreateHandlerAndArgs(csharpFilePath);

        var result = handler.GetCommandState(args);

        Assert.False(result.IsAvailable);
    }

    [UIFact]
    public void CSharpFile_NotAvailable()
    {
        using var files = new TempFileCollection();
        var csharpFilePath = Path.Combine(files.TempDir, "test.cs");
        File.WriteAllText(csharpFilePath, "");
        files.AddFile(csharpFilePath, false);

        var (handler, args) = CreateHandlerAndArgs(csharpFilePath);

        var result = handler.GetCommandState(args);

        Assert.False(result.IsAvailable);
    }

    private (ViewCodeCommandHandler, ViewCodeCommandArgs) CreateHandlerAndArgs(string filePath)
    {
        var textBuffer = StrictMock.Of<ITextBuffer>();
        var textDocument = StrictMock.Of<ITextDocument>(doc
            => doc.FilePath == filePath);
        var textDocumentFactory = StrictMock.Of<ITextDocumentFactoryService>(factory =>
            factory.TryGetTextDocument(textBuffer, out textDocument) == true);

        var serviceProvider = StrictMock.Of<IServiceProvider>();

        var handler = new ViewCodeCommandHandler(serviceProvider, textDocumentFactory, JoinableTaskContext);

        var textView = StrictMock.Of<ITextView>();
        var args = new ViewCodeCommandArgs(textView, textBuffer);

        return (handler, args);
    }

    private static TempFileCollection CreateTestFiles(string razorFileName, out string razorFilePath)
    {
        var files = new TempFileCollection();
        razorFilePath = Path.Combine(files.TempDir, razorFileName);
        var csharpFilePath = razorFilePath + ".cs";

        // Create our temp files
        File.WriteAllText(razorFilePath, "");
        File.WriteAllText(csharpFilePath, "");

        // Add them to the list so they get cleaned up
        files.AddFile(razorFilePath, false);
        files.AddFile(csharpFilePath, false);

        return files;
    }
}
