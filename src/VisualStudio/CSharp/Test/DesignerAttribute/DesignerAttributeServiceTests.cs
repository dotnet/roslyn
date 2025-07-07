// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.DesignerAttribute;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.UnitTests.DesignerAttributes;

[UseExportProvider]
public sealed class DesignerAttributeServiceTests
{
    [Fact]
    public Task NoDesignerTest1()
        => TestAsync(@"class Test { }", category: null);

    [Fact]
    public Task NoDesignerOnSecondClass()
        => TestAsync(
            """
            class Test1 { }

            [System.ComponentModel.DesignerCategory("Form")]
            class Test2 { }
            """, category: null);

    [Fact]
    public Task NoDesignerOnStruct()
        => TestAsync(
            """

            [System.ComponentModel.DesignerCategory("Form")]
            struct Test1 { }
            """, category: null);

    [Fact]
    public Task NoDesignerOnNestedClass()
        => TestAsync(
            """
            class Test1
            {
                [System.ComponentModel.DesignerCategory("Form")]
                class Test2 { }
            }
            """, category: null);

    [Fact]
    public Task SimpleDesignerTest()
        => TestAsync(
            """
            [System.ComponentModel.DesignerCategory("Form")]
            class Test { }
            """, "Form");

    [Fact]
    public Task SimpleDesignerTest2()
        => TestAsync(
            """
            using System.ComponentModel;

            [DesignerCategory("Form")]
            class Test { }
            """, "Form");

    [Theory]
    [InlineData(null)]
    [InlineData("Form")]
    [InlineData("Form1")]
    public Task TestUnboundBase1(string? existingCategory)
        => TestAsync(
            """
            namespace System.Windows.Forms
            {
                [System.ComponentModel.DesignerCategory("Form")]
                public class Form { }
            }

            // The base type won't bind.  That's ok.  We should fallback to looking it up in a particular namespace.
            // This should always work and not be impacted by the existing category.
            class Test : Form { }
            """, "Form", existingCategory);

    [Theory]
    [InlineData(null)]
    [InlineData("Form")]
    public Task TestUnboundBaseUseOldValueIfNotFound(string? category)
        => TestAsync(
            """
            // The base type won't bind.  Return existing category if we have one.
            class Test : Form { }
            """, category: category, existingCategory: category);

    private static async Task TestAsync(string codeWithMarker, string? category, string? existingCategory = null)
    {
        using var workspace = TestWorkspace.CreateCSharp(codeWithMarker, openDocuments: false);

        var hostDocument = workspace.Documents.First();
        var documentId = hostDocument.Id;
        var document = workspace.CurrentSolution.GetRequiredDocument(documentId);

        var compilation = await document.Project.GetRequiredCompilationAsync(CancellationToken.None);
        var actual = await DesignerAttributeDiscoveryService.ComputeDesignerAttributeCategoryAsync(
            compilation.DesignerCategoryAttributeType() != null, document.Project, document.Id, existingCategory, CancellationToken.None);

        Assert.Equal(category, actual);
    }
}
