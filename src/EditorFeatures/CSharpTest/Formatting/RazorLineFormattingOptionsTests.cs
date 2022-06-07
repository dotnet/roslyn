// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.TodoComments;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.CodeAnalysis.VisualBasic.Formatting;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Formatting;

[UseExportProvider]
public class RazorLineFormattingOptionsTests
{
    private static readonly TestComposition s_composition = EditorTestCompositions.EditorFeatures;

    private class TestRazorDocumentServiceProvider : IDocumentServiceProvider
    {
        public TService? GetService<TService>() where TService : class, IDocumentService
            => typeof(TService) == typeof(DocumentPropertiesService) ? (TService?)(object)new PropertiesService() : null;

        internal sealed class PropertiesService : DocumentPropertiesService
        {
            public override string? DiagnosticsLspClientName => "RazorCSharp";
        }
    }

    [Fact]
    public async Task FormatAsync()
    {
        var hostServices = s_composition.GetHostServices();

        using var workspace = new AdhocWorkspace(hostServices);

        var globalOptions = ((IMefHostExportProvider)hostServices).GetExportedValue<IGlobalOptionService>();
        globalOptions.SetGlobalOption(new OptionKey(RazorLineFormattingOptionsStorage.UseTabs), true);
        globalOptions.SetGlobalOption(new OptionKey(RazorLineFormattingOptionsStorage.TabSize), 10);

        var project = workspace.AddProject("Test", LanguageNames.CSharp);

        var source = @"
class C
   {
void F   () {}
       }
";

        var documentInfo = DocumentInfo.Create(
            DocumentId.CreateNewId(project.Id),
            name: "file.razor.g.cs",
            folders: Array.Empty<string>(),
            sourceCodeKind: SourceCodeKind.Regular,
            loader: TextLoader.From(TextAndVersion.Create(SourceText.From(source), VersionStamp.Create(), "file.razor.g.cs")),
            filePath: "file.razor.g.cs",
            isGenerated: false,
            designTimeOnly: true,
            documentServiceProvider: new TestRazorDocumentServiceProvider());

        var document = workspace.AddDocument(documentInfo);

#pragma warning disable RS0030 // Do not used banned APIs
        var formattedDocument = await Formatter.FormatAsync(document, spans: null, options: null, CancellationToken.None);
#pragma warning restore RS0030 // Do not used banned APIs

        var formattedText = await formattedDocument.GetTextAsync();

        // document options override solution options:
        AssertEx.Equal(@"
class C
{
" + "\t" + @"void F() { }
}
", formattedText.ToString());
    }
}
