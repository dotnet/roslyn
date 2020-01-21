// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
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
            using (var testState = ExtractInterfaceTestState.Create(markup, languageName, compilationOptions))
            {
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
        }
    }
}
