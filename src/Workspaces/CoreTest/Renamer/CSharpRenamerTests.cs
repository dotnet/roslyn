// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Renamer
{
    public class CSharpRenamerTests : RenamerTests
    {
        protected override string LanguageName => LanguageNames.CSharp;

        [Fact]
        public Task CSharp_TestEmptyDocument()
            => TestRenameDocument(
                MakeSingleDocumentWithInfoArray(""),
                MakeSingleDocumentWithInfoArray("", "NewDocumentName"));

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
            MakeSingleDocumentWithInfoArray(@"class OriginalName {}", "OriginalName.cs"),
            MakeSingleDocumentWithInfoArray(@"class NewDocumentName {}", "NewDocumentName.cs"),
            RenameHelpers.MakeSymbolPairs("OriginalName", "NewDocumentName"));

        [Fact]
        public Task CSharp_RenameDocument_RenameType_CaseInsensitive()
        => TestRenameDocument(
            MakeSingleDocumentWithInfoArray(@"class OriginalName {}", "originalName.cs"),
            MakeSingleDocumentWithInfoArray(@"class NewDocumentName {}", "NewDocumentName.cs"),
            RenameHelpers.MakeSymbolPairs("OriginalName", "NewDocumentName"));

        [Fact]
        public Task CSharp_RenameDocument_RenameInterface()
        => TestRenameDocument(
            MakeSingleDocumentWithInfoArray(@"interface IInterface {}", "IInterface.cs"),
            MakeSingleDocumentWithInfoArray(@"interface IInterface2 {}", "IInterface2.cs"),
            RenameHelpers.MakeSymbolPairs("IInterface", "IInterface2"));

        [Fact]
        public Task CSharp_RenameDocument_RenameEnum()
        => TestRenameDocument(
            MakeSingleDocumentWithInfoArray(@"enum MyEnum {}", "MyEnum.cs"),
            MakeSingleDocumentWithInfoArray(@"enum MyEnum2 {}", "MyEnum2.cs"),
            RenameHelpers.MakeSymbolPairs("MyEnum", "MyEnum2"));

        [Fact]
        public Task CSharp_RenameDocument_RenamePartialClass()
        {
            var originalDocuments = new[]
            {
                new DocumentWithInfo()
                {
                    Text = @"
namespace Test
{    
    partial class C
    {
    }
}",
                    DocumentFilePath = @"Test\Folder\Path\C.cs",
                    DocumentName = "C.cs"
                },
                new DocumentWithInfo()
                {
                    Text = @"
namespace Test
{    
    partial class C
    {
        class Other
        {
        }
    }
}",
                    DocumentFilePath = @"Test\Folder\Path\C.Other.cs",
                    DocumentName = "C.Other.cs"
                }
            };

            var expectedDocuments = new[]
            {
                new DocumentWithInfo()
                {
                    Text = @"
namespace Test
{    
    partial class C2
    {
    }
}",
                    DocumentFilePath = @"Test\Folder\Path\C2.cs",
                    DocumentName = "C2.cs"
                },
                new DocumentWithInfo()
                {
                    Text = @"
namespace Test
{    
    partial class C2
    {
        class Other
        {
        }
    }
}",
                    DocumentFilePath = @"Test\Folder\Path\C.Other.cs",
                    DocumentName = "C.Other.cs"
                }
            };

            return TestRenameDocument(originalDocuments, expectedDocuments, RenameHelpers.MakeSymbolPairs("Test.C", "Test.C2"));
        }

        [Fact]
        public Task CSharp_RenameDocument_NoRenameNamespace()
        => TestEmptyActionSet(
@"namespace Test.Path
{
    class C
    {
    }
}",
        documentPath: @"Test\Path\Document.cs",
        newDocumentPath: @"Test\Path\Document.cs");

        [Fact]
        public Task CSharp_RenameDocument_RenameNamespace()
        {

            var originalDocuments = MakeSingleDocumentWithInfoArray(
@"namespace Test.Path
{
    class C
    {
    }
}",
                path: @"Test\Path\Document.cs",
                name: "Document.cs");

            var newDocuments = MakeSingleDocumentWithInfoArray(
@"namespace Test.Path.After.Test
{
    class C
    {
    }
}",
                path: @"Test\Path\After\Test\Document.cs",
                name: "Document.cs");

            return TestRenameDocument(
                originalDocuments,
                newDocuments,
                RenameHelpers.MakeSymbolPairs("Test.Path.C", "Test.Path.After.Test.C"));
        }

        [Fact]
        public Task CSharp_RenameDocument_RenameMultipleNamespaces()
        {

            var originalDocuments = MakeSingleDocumentWithInfoArray(
@"namespace Test.Path
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
}",
                path: @"Test\Path\Document.cs",
                name: "Document.cs");

            var newDocuments = MakeSingleDocumentWithInfoArray(
@"namespace Test.Path.After.Test
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
}",
                path: @"Test\Path\After\Test\Document.cs",
                name: "Document.cs");

            return TestRenameDocument(
                originalDocuments,
                newDocuments,
                RenameHelpers.MakeSymbolPairs("Test.Path.C", "Test.Path.After.Test.C", "Test.Path.C2", "Test.Path.After.Test.C2"));
        }

        [Fact]
        public Task CSharp_RenameDocument_RenameMultipleNamespaces2()
        {
            var originalDocuments = MakeSingleDocumentWithInfoArray(
@"namespace Test.Path
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
}",
                name: "Document.cs",
                path: @"Test\Path\Document.cs");

            var newDocuments = MakeSingleDocumentWithInfoArray(
@"namespace Test.Path.After.Test
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
}",
                name: "Document.cs",
                path: @"Test\Path\After\Test\Document.cs");

            return TestRenameDocument(
                originalDocuments,
                newDocuments,
                RenameHelpers.MakeSymbolPairs("Test.Path.C", "Test.Path.After.Test.C", "Test.Path.C2", "Test.Path.After.Test.C2"));
        }

        [Fact]
        public Task CSharp_RenameDocument_RenameMultipleNamespaces3()
        {
            var originalDocuments = MakeSingleDocumentWithInfoArray(
@"namespace Test.Path
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
}",
                name: "Document.cs",
                path: @"Test\Path\Document.cs");

            var newDocuments = MakeSingleDocumentWithInfoArray(
@"namespace Test.Path.After.Test
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
}",
                name: "Document.cs",
                path: @"Test\Path\After\Test\Document.cs");

            return TestRenameDocument(
                originalDocuments,
                newDocuments,
                RenameHelpers.MakeSymbolPairs("Test.Path.C", "Test.Path.After.Test.C", "Test.Path.C3", "Test.Path.After.Test.C3"));
        }

        [Fact]
        public Task CSharp_RenameDocument_RenameMultipleNamespaces_Nested()
        {
            var originalDocuments = MakeSingleDocumentWithInfoArray(
@"namespace Test.Path
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
}",
                path: @"Test\Path\Document.cs",
                name: "Document.cs");

            var newDocuments = MakeSingleDocumentWithInfoArray(
@"namespace Test.Path.After.Test
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
}",
                name: @"Document.cs",
                path: @"Test\Path\After\Test\Document.cs");

            return TestRenameDocument(
                originalDocuments,
                newDocuments,
                RenameHelpers.MakeSymbolPairs("Test.Path.C", "Test.Path.After.Test.C"));
        }

        [Fact]
        public Task CSharp_RenameDocument_RenameNamespace2()
        {
            var originalDocuments = MakeSingleDocumentWithInfoArray(@"namespace Test.Path
{
    class C
    {
    }
}",
                path: @"Test\Path\Document.cs",
                name: "Document.cs");

            var newDocuments = MakeSingleDocumentWithInfoArray(
@"namespace Test
{
    class C
    {
    }
}",
                name: @"Document.cs",
                path: @"Test\Document.cs");

            return TestRenameDocument(
                originalDocuments,
                newDocuments,
                RenameHelpers.MakeSymbolPairs("Test.Path.C", "Test.C"));
        }

        [Fact]
        public Task CSharp_RenameDocument_RenameNamespaceAndClass()
        {
            var originalDocuments = MakeSingleDocumentWithInfoArray(
@"namespace Test.Path
{
    class C
    {
    }
}",
                path: @"Test\Path\C.cs",
                name: "C.cs");

            var newDocuments = MakeSingleDocumentWithInfoArray(
@"namespace Test
{
    class C2
    {
    }
}",
        documentPath: @"Test\Path\C2.cs",
        documentName: @"C.cs",
        newDocumentName: @"C2",
        newDocumentPath: @"Test\C2.cs");

         return TestRenameDocument(
                originalDocuments,
                newDocuments,
                RenameHelpers.MakeSymbolPairs("Test.Path.C", "Test.C2"));
        }

        [Fact]
        [WorkItem(46580, "https://github.com/dotnet/roslyn/issues/46580")]
        public Task CSharp_RenameDocument_MappedDocumentHasNoResults()
        {
            var documentName = "Component1.razor";
            var documentText =
@"<h3>Component1</h3>
@code {}";

            return TestRenameMappedFile(documentText, documentName, newDocumentName: "MyComponent.razor");
                path: @"Test\C2.cs",
                name: "C2.cs");
        }
    }
}
