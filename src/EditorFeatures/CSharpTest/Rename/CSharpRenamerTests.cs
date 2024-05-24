// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Renamer;

public class CSharpRenamerTests : RenamerTests
{
    protected override string LanguageName => LanguageNames.CSharp;

    [Fact]
    public Task CSharp_TestEmptyDocument()
        => TestRenameDocument(
            "",
            "",
            newDocumentName: "NewDocumentName");

    [Fact]
    public Task CSharp_TestNullDocumentName()
    => TestEmptyActionSet(
        "class C {}",
        documentName: "C.cs");

    [Fact]
    public Task CSharp_RenameDocument_NoRenameType()
    => TestEmptyActionSet(
        @"class C {}",
        documentName: "NotC.cs",
        newDocumentName: "C.cs");

    [Fact]
    public Task CSharp_RenameDocument_RenameType()
    => TestRenameDocument(
        @"class OriginalName {}",
        @"class NewDocumentName {}",
        documentName: "OriginalName.cs",
        newDocumentName: "NewDocumentName.cs");

    [Fact]
    public Task CSharp_RenameDocument_RenameType_CaseInsensitive()
    => TestRenameDocument(
        @"class OriginalName {}",
        @"class NewDocumentName {}",
        documentName: "originalName.cs",
        newDocumentName: "NewDocumentName.cs");

    [Fact]
    public Task CSharp_RenameDocument_RenameInterface()
    => TestRenameDocument(
        @"interface IInterface {}",
        @"interface IInterface2 {}",
        documentName: "IInterface.cs",
        newDocumentName: "IInterface2.cs");

    [Fact]
    public Task CSharp_RenameDocument_RenameEnum()
    => TestRenameDocument(
        @"enum MyEnum {}",
        @"enum MyEnum2 {}",
        documentName: "MyEnum.cs",
        newDocumentName: "MyEnum2.cs");

    [Fact]
    public Task CSharp_RenameDocument_RenamePartialClass()
    {
        var originalDocuments = new[]
        {
            new DocumentWithInfo()
            {
                Text = """
                namespace Test
                {    
                    partial class C
                    {
                    }
                }
                """,
                DocumentFilePath = @"Test\Folder\Path\C.cs",
                DocumentName = "C.cs"
            },
            new DocumentWithInfo()
            {
                Text = """
                namespace Test
                {    
                    partial class C
                    {
                        class Other
                        {
                        }
                    }
                }
                """,
                DocumentFilePath = @"Test\Folder\Path\C.Other.cs",
                DocumentName = "C.Other.cs"
            }
        };

        var expectedDocuments = new[]
        {
            new DocumentWithInfo()
            {
                Text = """
                namespace Test
                {    
                    partial class C2
                    {
                    }
                }
                """,
                DocumentFilePath = @"Test\Folder\Path\C2.cs",
                DocumentName = "C2.cs"
            },
            new DocumentWithInfo()
            {
                Text = """
                namespace Test
                {    
                    partial class C2
                    {
                        class Other
                        {
                        }
                    }
                }
                """,
                DocumentFilePath = @"Test\Folder\Path\C.Other.cs",
                DocumentName = "C.Other.cs"
            }
        };

        return TestRenameDocument(originalDocuments, expectedDocuments);
    }

    [Fact]
    public Task CSharp_RenameDocument_NoRenameNamespace()
    => TestEmptyActionSet(
        """
        namespace Test.Path
        {
            class C
            {
            }
        }
        """,
    documentPath: @"Test\Path\Document.cs",
    documentName: @"Document.cs");

    [Fact]
    public Task CSharp_RenameDocument_RenameNamespace()
    => TestRenameDocument(
        """
        namespace Test.Path
        {
            class C
            {
            }
        }
        """,
        """
        namespace Test.Path.After.Test
        {
            class C
            {
            }
        }
        """,
    documentPath: @"Test\Path\Document.cs",
    documentName: @"Document.cs",
    newDocumentPath: @"Test\Path\After\Test\Document.cs");

    [Fact]
    public Task CSharp_RenameDocument_RenameMultipleNamespaces()
   => TestRenameDocument(
       """
       namespace Test.Path
       {
           class C
           {
           }
       }

       namespace Test.Path
       {
           class C2
           {
           }
       }
       """,
       """
       namespace Test.Path.After.Test
       {
           class C
           {
           }
       }

       namespace Test.Path.After.Test
       {
           class C2
           {
           }
       }
       """,
   documentPath: @"Test\Path\Document.cs",
   documentName: @"Document.cs",
   newDocumentPath: @"Test\Path\After\Test\Document.cs");

    [Fact]
    public Task CSharp_RenameDocument_RenameMultipleNamespaces2()
   => TestRenameDocument(
       """
       namespace Test.Path
       {
           class C
           {
           }
       }

       namespace Test.Path
       {
           class C2
           {
           }
       }

       namespace Other.Namespace
       {
           class C3
           {
           }
       }
       """,
       """
       namespace Test.Path.After.Test
       {
           class C
           {
           }
       }

       namespace Test.Path.After.Test
       {
           class C2
           {
           }
       }

       namespace Other.Namespace
       {
           class C3
           {
           }
       }
       """,
   documentPath: @"Test\Path\Document.cs",
   documentName: @"Document.cs",
   newDocumentPath: @"Test\Path\After\Test\Document.cs");

    [Fact]
    public Task CSharp_RenameDocument_RenameMultipleNamespaces3()
   => TestRenameDocument(
       """
       namespace Test.Path
       {
           class C
           {
           }
       }

       namespace Other.Namespace
       {
           class C2
           {
           }
       }

       namespace Test.Path
       {
           class C3
           {
           }
       }
       """,
       """
       namespace Test.Path.After.Test
       {
           class C
           {
           }
       }

       namespace Other.Namespace
       {
           class C2
           {
           }
       }

       namespace Test.Path.After.Test
       {
           class C3
           {
           }
       }
       """,
   documentPath: @"Test\Path\Document.cs",
   documentName: @"Document.cs",
   newDocumentPath: @"Test\Path\After\Test\Document.cs");

    [Fact]
    public Task CSharp_RenameDocument_RenameMultipleNamespaces_Nested()
=> TestRenameDocument(
"""
namespace Test.Path
{
    class C
    {
    }
}

namespace Test
{
    namespace Path
    {
        class C2
        {
        }
    }
}
""",
"""
namespace Test.Path.After.Test
{
    class C
    {
    }
}

namespace Test
{
    namespace Path
    {
        class C2
        {
        }
    }
}
""",
documentPath: @"Test\Path\Document.cs",
documentName: @"Document.cs",
newDocumentPath: @"Test\Path\After\Test\Document.cs");

    [Fact]
    public Task CSharp_RenameDocument_RenameNamespace2()
    => TestRenameDocument(
        """
        namespace Test.Path
        {
            class C
            {
            }
        }
        """,
        """
        namespace Test
        {
            class C
            {
            }
        }
        """,
    documentPath: @"Test\Path\Document.cs",
    documentName: @"Document.cs",
    newDocumentPath: @"Test\Document.cs");

    [Fact]
    public Task CSharp_RenameDocument_RenameNamespaceAndClass()
    => TestRenameDocument(
        """
        namespace Test.Path
        {
            class C
            {
            }
        }
        """,
        """
        namespace Test
        {
            class C2
            {
            }
        }
        """,
    documentPath: @"Test\Path\C2.cs",
    documentName: @"C.cs",
    newDocumentName: @"C2",
    newDocumentPath: @"Test\C2.cs");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46580")]
    public Task CSharp_RenameDocument_MappedDocumentHasNoResults()
    {
        var documentName = "Component1.razor";
        var documentText =
            """
            <h3>Component1</h3>
            @code {}
            """;

        return TestRenameMappedFile(documentText, documentName, newDocumentName: "MyComponent.razor");
    }
}
