// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Razor.LanguageClient.Cohost;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test.AutoInsert;

public class PreferHtmlInAttributeValuesDocumentPositionInfoStrategyTest(ITestOutputHelper testOutput) : CohostEndpointTestBase(testOutput)
{
    [Theory]
    [InlineData(
        """
        @page "/"
        @using Microsoft.AspNetCore.Components.Forms
        <InputText ValueChanged=$$></InputText>
        """,
        RazorLanguageKind.Html)]
    [InlineData(
        """
        @page "/"
        @using Microsoft.AspNetCore.Components.Forms
        <InputText ValueChanged="$$"></InputText>
        """,
        RazorLanguageKind.CSharp)]
    [InlineData(
        """
        @page "/"
        @using Microsoft.AspNetCore.Components.Forms
        <InputText ValueChanged="@DateTime.$$"></InputText>
        """,
        RazorLanguageKind.CSharp)]
    internal async Task TryGetPositionInfoAsync_AtVariousPosition_ReturnsCorrectLanguage(string documentText, RazorLanguageKind expectedLanguage)
    {
        // Arrange
        TestFileMarkupParser.GetPosition(documentText, out documentText, out var cursorPosition);

        var document = CreateProjectAndRazorDocument(documentText);

        var snapshotManager = OOPExportProvider.GetExportedValue<RemoteSnapshotManager>();
        var codeDocument = await snapshotManager.GetSnapshot(document).GetGeneratedOutputAsync(DisposalToken);

        var position = codeDocument.Source.Text.GetPosition(cursorPosition);

        var documentMappingService = OOPExportProvider.GetExportedValue<IDocumentMappingService>();

        // Act
        var result = PreferHtmlInAttributeValuesDocumentPositionInfoStrategy.Instance.GetPositionInfo(documentMappingService, codeDocument, cursorPosition);

        // Assert
        Assert.NotEqual(default, result);
        Assert.Equal(expectedLanguage, result.LanguageKind);

        if (expectedLanguage != RazorLanguageKind.CSharp)
        {
            Assert.Equal(cursorPosition, result.HostDocumentIndex);
            Assert.Equal(position, result.Position);
        }
    }
}
