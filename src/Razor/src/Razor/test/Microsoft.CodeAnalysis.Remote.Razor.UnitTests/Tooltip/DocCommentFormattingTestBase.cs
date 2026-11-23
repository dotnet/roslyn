// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Remote.Razor.Tooltip;

public abstract class DocCommentFormattingTestBase(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    /// <summary>
    ///  Creates a C# document to stand in for the generated Razor document. Tooltips use it so that Roslyn can format
    ///  doc comments, but the tests don't have a real generated document, and any C# document with corlib will do.
    /// </summary>
    private protected Document CreateGeneratedDocument(string source = "namespace MyApp { public class Component { } }")
    {
        var workspace = AddDisposable(new AdhocWorkspace(MefHostServices.DefaultHost));
        var project = workspace.AddProject("Test", LanguageNames.CSharp)
            .AddMetadataReference(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        return project.AddDocument("Test.cs", SourceText.From(source));
    }

    private protected Task<DocCommentFormattingContext> CreateFormattingContextAsync()
        => DocCommentFormattingContext.CreateAsync(CreateGeneratedDocument(), DisposalToken);
}
