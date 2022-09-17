// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.VisualBasic;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.ExtractInterface
{
    [UseExportProvider]
    public abstract class AbstractExtractInterfaceTests
    {
        public static async Task TestExtractInterfaceCommandCSharpAsync(
            string markup,
            bool expectedSuccess,
            string expectedMemberName = null,
            string expectedInterfaceName = null,
            string expectedNamespaceName = null,
            string expectedTypeParameterSuffix = null,
            string expectedUpdatedOriginalDocumentCode = null,
            string expectedInterfaceCode = null)
        {
            await TestExtractInterfaceCommandAsync(
                markup,
                LanguageNames.CSharp,
                expectedSuccess,
                expectedMemberName,
                expectedInterfaceName,
                expectedNamespaceName,
                expectedTypeParameterSuffix,
                expectedUpdatedOriginalDocumentCode,
                expectedInterfaceCode);
        }

        public static async Task TestExtractInterfaceCodeActionCSharpAsync(
            string markup,
            string expectedMarkup)
        {
            await TestExtractInterfaceCodeActionAsync(
                markup,
                LanguageNames.CSharp,
                expectedMarkup);
        }

        public static async Task TestExtractInterfaceCommandVisualBasicAsync(
            string markup,
            bool expectedSuccess,
            string expectedMemberName = null,
            string expectedInterfaceName = null,
            string expectedNamespaceName = null,
            string expectedTypeParameterSuffix = null,
            string expectedUpdatedOriginalDocumentCode = null,
            string expectedInterfaceCode = null,
            string rootNamespace = null)
        {
            await TestExtractInterfaceCommandAsync(
                markup,
                LanguageNames.VisualBasic,
                expectedSuccess,
                expectedMemberName,
                expectedInterfaceName,
                expectedNamespaceName,
                expectedTypeParameterSuffix,
                expectedUpdatedOriginalDocumentCode,
                expectedInterfaceCode,
                new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, rootNamespace: rootNamespace));
        }

        public static async Task TestExtractInterfaceCodeActionVisualBasicAsync(
            string markup,
            string expectedMarkup)
        {
            await TestExtractInterfaceCodeActionAsync(
                markup,
                LanguageNames.VisualBasic,
                expectedMarkup);
        }

        private static async Task TestExtractInterfaceCommandAsync(
            string markup,
            string languageName,
            bool expectedSuccess,
            string expectedMemberName = null,
            string expectedInterfaceName = null,
            string expectedNamespaceName = null,
            string expectedTypeParameterSuffix = null,
            string expectedUpdatedOriginalDocumentCode = null,
            string expectedInterfaceCode = null,
            CompilationOptions compilationOptions = null)
        {
            using var testState = ExtractInterfaceTestState.Create(markup, languageName, compilationOptions,
                options: new OptionsCollection(languageName)
                {
                    { CodeStyleOptions2.AccessibilityModifiersRequired, AccessibilityModifiersRequired.Never, NotificationOption2.Silent }
                });

            var result = await testState.ExtractViaCommandAsync();

            if (expectedSuccess)
            {
                Assert.True(result.Succeeded);
                Assert.False(testState.Workspace.Documents.Select(d => d.Id).Contains(result.NavigationDocumentId));
                Assert.NotNull(result.UpdatedSolution.GetDocument(result.NavigationDocumentId));

                if (expectedMemberName != null)
                {
                    Assert.Equal(1, testState.TestExtractInterfaceOptionsService.AllExtractableMembers.Count());
                    Assert.Equal(expectedMemberName, testState.TestExtractInterfaceOptionsService.AllExtractableMembers.Single().Name);
                }

                if (expectedInterfaceName != null)
                {
                    Assert.Equal(expectedInterfaceName, testState.TestExtractInterfaceOptionsService.DefaultInterfaceName);
                }

                if (expectedNamespaceName != null)
                {
                    Assert.Equal(expectedNamespaceName, testState.TestExtractInterfaceOptionsService.DefaultNamespace);
                }

                if (expectedTypeParameterSuffix != null)
                {
                    Assert.Equal(expectedTypeParameterSuffix, testState.TestExtractInterfaceOptionsService.GeneratedNameTypeParameterSuffix);
                }

                if (expectedUpdatedOriginalDocumentCode != null)
                {
                    var updatedOriginalDocument = result.UpdatedSolution.GetDocument(testState.ExtractFromDocument.Id);
                    var updatedCode = (await updatedOriginalDocument.GetTextAsync()).ToString();
                    Assert.Equal(expectedUpdatedOriginalDocumentCode, updatedCode);
                }

                if (expectedInterfaceCode != null)
                {
                    var interfaceDocument = result.UpdatedSolution.GetDocument(result.NavigationDocumentId);
                    var interfaceCode = (await interfaceDocument.GetTextAsync()).ToString();
                    Assert.Equal(expectedInterfaceCode, interfaceCode);
                }
            }
            else
            {
                Assert.False(result.Succeeded);
            }
        }

        private static async Task TestExtractInterfaceCodeActionAsync(
            string markup,
            string languageName,
            string expectedMarkup,
            CompilationOptions compilationOptions = null)
        {
            using var testState = ExtractInterfaceTestState.Create(markup, languageName, compilationOptions);

            var updatedSolution = await testState.ExtractViaCodeAction();
            var updatedDocument = updatedSolution.GetDocument(testState.ExtractFromDocument.Id);
            var updatedCode = (await updatedDocument.GetTextAsync()).ToString();
            Assert.Equal(expectedMarkup, updatedCode);
        }
    }
}
