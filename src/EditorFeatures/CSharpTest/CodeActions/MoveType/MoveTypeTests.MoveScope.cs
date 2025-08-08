// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings.MoveType;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.MoveType;

[Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
public partial class MoveTypeTests : CSharpMoveTypeTestsBase
{
    [Fact]
    public Task MoveType_NamespaceScope_SingleItem()
        => TestNamespaceMove("""
            namespace N1
            {
                class [||]Class1
                {
                }
            }
            """, """
            namespace N1
            {
                class Class1
                {
                }
            }
            """, expectOperation: false);

    [Fact]
    public Task MoveType_NamespaceScope_SingleItemNamespaceComment()
        => TestNamespaceMove("""
            // Comment on the namespace
            namespace N1
            {
                class [||]Class1
                {
                }
            }
            """, """
            // Comment on the namespace
            namespace N1
            {
                class Class1
                {
                }
            }
            """, expectOperation: false);

    [Fact]
    public Task MoveType_NamespaceScope_ItemAtTop()
        => TestNamespaceMove("""
            namespace N1
            {
                class [||]Class1
                {
                }

                class Class2
                {
                }
            }
            """, """
            namespace N1
            {
                class Class1
                {
                }
            }

            namespace N1
            {
                class Class2
                {
                }
            }
            """);

    [Fact]
    public Task MoveType_NamespaceScope_ItemAtTopNamespaceComment()
        => TestNamespaceMove("""
            // Comment on the namespace
            namespace N1
            {
                class [||]Class1
                {
                }

                class Class2
                {
                }
            }
            """, """
            // Comment on the namespace
            namespace N1
            {
                class Class1
                {
                }
            }

            namespace N1
            {
                class Class2
                {
                }
            }
            """);

    [Fact]
    public Task MoveType_NamespaceScope_ItemAtTopWithComments()
        => TestNamespaceMove("""
            namespace N1
            {
                // Class1 Comment
                class [||]Class1
                {
                }

                // Class2 Comment
                class Class2
                {
                }
            }
            """, """
            namespace N1
            {
                // Class1 Comment
                class Class1
                {
                }
            }

            namespace N1
            {
                // Class2 Comment
                class Class2
                {
                }
            }
            """);

    [Fact]
    public Task MoveType_NamespaceScope_ItemAtTopWithXmlComments()
        => TestNamespaceMove("""
            namespace N1
            {
                /// <summary>
                /// Class1 summary
                /// </summary>
                class [||]Class1
                {
                }

                /// <summary>
                /// Class2 summary
                /// </summary>
                class Class2
                {
                }
            }
            """, """
            namespace N1
            {
                /// <summary>
                /// Class1 summary
                /// </summary>
                class Class1
                {
                }
            }

            namespace N1
            {
                /// <summary>
                /// Class2 summary
                /// </summary>
                class Class2
                {
                }
            }
            """);

    [Fact]
    public Task MoveType_NamespaceScope_ItemAtBottom()
        => TestNamespaceMove("""
            namespace N1
            {
                class Class1
                {
                }

                class [||]Class2
                {
                }
            }
            """, """
            namespace N1
            {
                class Class1
                {
                }
            }

            namespace N1
            {
                class Class2
                {
                }
            }
            """);

    [Fact]
    public Task MoveType_NamespaceScope_ItemAtBottomNamespaceComments()
        => TestNamespaceMove("""
            // Comment on the namespace
            namespace N1
            {
                class Class1
                {
                }

                class [||]Class2
                {
                }
            }
            """, """
            // Comment on the namespace
            namespace N1
            {
                class Class1
                {
                }
            }

            namespace N1
            {
                class Class2
                {
                }
            }
            """);

    [Fact]
    public Task MoveType_NamespaceScope_ItemAtBottomWithComments()
        => TestNamespaceMove("""
            namespace N1
            {
                // Class1 comment
                class Class1
                {
                }

                // Class2 comment
                class [||]Class2
                {
                }
            }
            """, """
            namespace N1
            {
                // Class1 comment
                class Class1
                {
                }
            }

            namespace N1
            {
                // Class2 comment
                class Class2
                {
                }
            }
            """);

    [Fact]
    public Task MoveType_NamespaceScope_ItemAtBottomWithXmlComments()
        => TestNamespaceMove("""
            namespace N1
            {
                /// <summary>
                /// Class1 summary
                /// </summary>
                class Class1
                {
                }

                /// <summary>
                /// Class2 summary
                /// </summary>
                class [||]Class2
                {
                }
            }
            """, """
            namespace N1
            {
                /// <summary>
                /// Class1 summary
                /// </summary>
                class Class1
                {
                }
            }

            namespace N1
            {
                /// <summary>
                /// Class2 summary
                /// </summary>
                class Class2
                {
                }
            }
            """);

    [Fact]
    public Task MoveType_NamespaceScope_ItemInMiddle()
        => TestNamespaceMove("""
            namespace N1
            {
                class Class1
                {
                }

                class Class2
                {
                }

                class [||]Class3
                {
                }

                class Class4
                {
                }

                class Class5
                {
                }
            }
            """, """
            namespace N1
            {
                class Class1
                {
                }

                class Class2
                {
                }
            }

            namespace N1
            {
                class Class3
                {
                }
            }

            namespace N1
            {
                class Class4
                {
                }

                class Class5
                {
                }
            }
            """);

    [Fact]
    public Task MoveType_NamespaceScope_ItemInMiddleNamespaceComment()
        => TestNamespaceMove("""
            // Comment on the namespace
            namespace N1
            {
                class Class1
                {
                }

                class Class2
                {
                }

                class [||]Class3
                {
                }

                class Class4
                {
                }

                class Class5
                {
                }
            }
            """, """
            // Comment on the namespace
            namespace N1
            {
                class Class1
                {
                }

                class Class2
                {
                }
            }

            namespace N1
            {
                class Class3
                {
                }
            }

            namespace N1
            {
                class Class4
                {
                }

                class Class5
                {
                }
            }
            """);

    [Fact]
    public Task MoveType_NamespaceScope_ItemInMiddleWithComments()
        => TestNamespaceMove("""
            namespace N1
            {
                // Class1 comment
                class Class1
                {
                }

                // Class2 comment
                class Class2
                {
                }

                // Class3 comment
                class [||]Class3
                {
                }

                // Class4 comment
                class Class4
                {
                }

                // Class5 comment
                class Class5
                {
                }
            }
            """, """
            namespace N1
            {
                // Class1 comment
                class Class1
                {
                }

                // Class2 comment
                class Class2
                {
                }
            }

            namespace N1
            {
                // Class3 comment
                class Class3
                {
                }
            }

            namespace N1
            {
                // Class4 comment
                class Class4
                {
                }

                // Class5 comment
                class Class5
                {
                }
            }
            """);

    [Fact]
    public Task MoveType_NamespaceScope_ItemInMiddleWithXmlComments()
        => TestNamespaceMove("""
            namespace N1
            {
                /// <summary>
                /// Class1 summary
                /// </summary>
                class Class1
                {
                }

                /// <summary>
                /// Class2 summary
                /// </summary>
                class Class2
                {
                }

                /// <summary>
                /// Class3 summary
                /// </summary>
                class [||]Class3
                {
                }

                /// <summary>
                /// Class4 summary
                /// </summary>
                class Class4
                {
                }

                /// <summary>
                /// Class5 summary
                /// </summary>
                class Class5
                {
                }
            }
            """, """
            namespace N1
            {
                /// <summary>
                /// Class1 summary
                /// </summary>
                class Class1
                {
                }

                /// <summary>
                /// Class2 summary
                /// </summary>
                class Class2
                {
                }
            }

            namespace N1
            {
                /// <summary>
                /// Class3 summary
                /// </summary>
                class Class3
                {
                }
            }

            namespace N1
            {
                /// <summary>
                /// Class4 summary
                /// </summary>
                class Class4
                {
                }

                /// <summary>
                /// Class5 summary
                /// </summary>
                class Class5
                {
                }
            }
            """);

    [Fact]
    public Task MoveType_NamespaceScope_ItemInMiddleWithInterface()
        => TestNamespaceMove("""
            namespace N1
            {
                // Class1 comment
                class Class1
                {
                }

                // IClass3 comment
                interface IClass3
                {
                    void DoStuff();
                }

                // Class3 comment
                class [||]Class3 : IClass3
                {
                    public void DoStuff() { }
                }

                // Class4 comment
                class Class4
                {
                }

                // Class5 comment
                class Class5
                {
                }
            }
            """, """
            namespace N1
            {
                // Class1 comment
                class Class1
                {
                }

                // IClass3 comment
                interface IClass3
                {
                    void DoStuff();
                }
            }

            namespace N1
            {
                // Class3 comment
                class Class3 : IClass3
                {
                    public void DoStuff() { }
                }
            }

            namespace N1
            {
                // Class4 comment
                class Class4
                {
                }

                // Class5 comment
                class Class5
                {
                }
            }
            """);

    [Fact]
    public Task MoveType_NamespaceScope_TwoItemsInDifferentNamespace()
        => TestNamespaceMove("""
            namespace N1
            {
                class [||]Class1
                {
                }
            }

            namespace N2
            {
                class Class2
                {
                }
            }
            """, """
            namespace N1
            {
                class Class1
                {
                }
            }

            namespace N2
            {
                class Class2
                {
                }
            }
            """, expectOperation: false);

    [Fact]
    public Task MoveType_NamespaceScope_ItemsInDifferentNamespace()
        => TestNamespaceMove("""
            namespace N1
            {
                interface IClass1
                {
                }

                class [||]Class1 : IClass1
                {
                }
            }

            namespace N2
            {
                class Class2
                {
                }
            }
            """, """
            namespace N1
            {
                interface IClass1
                {
                }
            }

            namespace N1
            {
                class Class1 : IClass1
                {
                }
            }

            namespace N2
            {
                class Class2
                {
                }
            }
            """);

    [Fact]
    public Task MoveType_NamespaceScope_NestedNamespaces()
        => TestNamespaceMove("""
            namespace N1
            {
                namespace N2
                {
                    class [||]C1
                    {
                    }

                    class C2
                    {
                    }
                }

                class C3
                {
                }
            }
            """, """
            namespace N1
            {
                namespace N1.N2
                {
                    class C1
                    {
                    }
                }

                namespace N2
                {
                    class C2
                    {
                    }
                }

                class C3
                {
                }
            }
            """);

    [Fact]
    public Task MoveType_NamespaceScope_NestedNamespaces2()
        => TestNamespaceMove("""
            namespace N1
            {
                namespace N2
                {
                    class C1
                    {
                    }

                    class C2
                    {
                    }
                }

                class [||]C3
                {
                }

                namespace N3
                {
                    class C4
                    {
                    }
                }
            }
            """, """
            namespace N1
            {
                namespace N2
                {
                    class C1
                    {
                    }

                    class C2
                    {
                    }
                }
            }

            namespace N1
            {
                class C3
                {
                }
            }

            namespace N1
            {
                namespace N3
                {
                    class C4
                    {
                    }
                }
            }
            """);

    private async Task TestNamespaceMove(string originalCode, string expectedCode, bool expectOperation = true)
    {
        using var workspace = CreateWorkspaceFromOptions(originalCode);
        var documentToModifyId = workspace.Documents[0].Id;
        var textSpan = workspace.Documents[0].SelectedSpans[0];
        var documentToModify = workspace.CurrentSolution.GetDocument(documentToModifyId);

        var moveTypeService = documentToModify.GetLanguageService<IMoveTypeService>();
        Assert.NotNull(moveTypeService);

        var modifiedSolution = await moveTypeService.GetModifiedSolutionAsync(documentToModify, textSpan, MoveTypeOperationKind.MoveTypeNamespaceScope, CancellationToken.None).ConfigureAwait(false);

        if (expectOperation)
        {
            Assert.NotEqual(documentToModify.Project.Solution, modifiedSolution);
        }
        else
        {
            Assert.Equal(documentToModify.Project.Solution, modifiedSolution);
        }

        var modifiedDocument = modifiedSolution.GetDocument(documentToModifyId);
        var formattedDocument = await Formatter.FormatAsync(modifiedDocument, CSharpSyntaxFormattingOptions.Default, CancellationToken.None).ConfigureAwait(false);

        var formattedText = await formattedDocument.GetTextAsync().ConfigureAwait(false);
        Assert.Equal(expectedCode, formattedText.ToString());
    }
}
