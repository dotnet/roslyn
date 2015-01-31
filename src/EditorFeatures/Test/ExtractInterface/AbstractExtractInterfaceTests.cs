// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.VisualBasic;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.ExtractInterface
{
    public abstract class AbstractExtractInterfaceTests
    {
        public static void TestExtractInterfaceCommandCSharp(
            string markup,
            bool expectedSuccess,
            string expectedMemberName = null,
            string expectedInterfaceName = null,
            string expectedNamespaceName = null,
            string expectedTypeParameterSuffix = null,
            string expectedUpdatedOriginalDocumentCode = null,
            string expectedInterfaceCode = null)
        {
            TestExtractInterfaceCommand(
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

        public static void TestExtractInterfaceCommandVisualBasic(
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
            TestExtractInterfaceCommand(
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

        private static void TestExtractInterfaceCommand(
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
            using (var testState = new ExtractInterfaceTestState(markup, languageName, compilationOptions))
            {
                var result = testState.ExtractViaCommand();

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
                        var updatedCode = updatedOriginalDocument.GetTextAsync(CancellationToken.None).Result.ToString();
                        Assert.Equal(expectedUpdatedOriginalDocumentCode, updatedCode);
                    }

                    if (expectedInterfaceCode != null)
                    {
                        var interfaceDocument = result.UpdatedSolution.GetDocument(result.NavigationDocumentId);
                        var interfaceCode = interfaceDocument.GetTextAsync(CancellationToken.None).Result.ToString();
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
