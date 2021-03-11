// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Renamer
{
    public class CSharpRenamerTests : RenamerTests
    {
        protected override string LanguageName => LanguageNames.CSharp;

        [Fact]
        public Task CSharp_TestEmptyDocument()
            => TestRenameDocumentAsync(
                "",
                "",
                newDocumentName: "NewDocumentName");

        [Fact]
        public Task CSharp_TestNullDocumentName()
        => TestEmptyActionSetAsync(
            "class C {}",
            documentName: "C.cs");

        [Fact]
        public Task CSharp_RenameDocument_NoRenameType()
        => TestEmptyActionSetAsync(
            @"class C {}",
            documentName: "NotC.cs",
            newDocumentName: "C.cs");

        [Fact]
        public Task CSharp_RenameDocument_RenameType()
        => TestRenameDocumentAsync(
            @"class OriginalName {}",
            @"class NewDocumentName {}",
            documentName: "OriginalName.cs",
            newDocumentName: "NewDocumentName.cs");

        [Fact]
        public Task CSharp_RenameDocument_RenameType_CaseInsensitive()
        => TestRenameDocumentAsync(
            @"class OriginalName {}",
            @"class NewDocumentName {}",
            documentName: "originalName.cs",
            newDocumentName: "NewDocumentName.cs");

        [Fact]
        public Task CSharp_RenameDocument_RenameInterface()
        => TestRenameDocumentAsync(
            @"interface IInterface {}",
            @"interface IInterface2 {}",
            documentName: "IInterface.cs",
            newDocumentName: "IInterface2.cs");

        [Fact]
        public Task CSharp_RenameDocument_RenameEnum()
        => TestRenameDocumentAsync(
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

            return TestRenameDocumentAsync(originalDocuments, expectedDocuments);
        }

        [Fact]
        public Task CSharp_RenameDocument_NoRenameNamespace()
        => TestEmptyActionSetAsync(
@"namespace Test.Path
{
    class C
    {
    }
}",
        documentPath: @"Test\Path\Document.cs",
        documentName: @"Document.cs");

        [Fact]
        public Task CSharp_RenameDocument_RenameNamespace()
        => TestRenameDocumentAsync(
@"namespace Test.Path
{
    class C
    {
    }
}",
@"namespace Test.Path.After.Test
{
    class C
    {
    }
}",
        documentPath: @"Test\Path\Document.cs",
        documentName: @"Document.cs",
        newDocumentPath: @"Test\Path\After\Test\Document.cs");

        [Fact]
        public Task CSharp_RenameDocument_RenameMultipleNamespaces()
       => TestRenameDocumentAsync(
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
       documentPath: @"Test\Path\Document.cs",
       documentName: @"Document.cs",
       newDocumentPath: @"Test\Path\After\Test\Document.cs");

        [Fact]
        public Task CSharp_RenameDocument_RenameMultipleNamespaces2()
       => TestRenameDocumentAsync(
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
       documentPath: @"Test\Path\Document.cs",
       documentName: @"Document.cs",
       newDocumentPath: @"Test\Path\After\Test\Document.cs");

        [Fact]
        public Task CSharp_RenameDocument_RenameMultipleNamespaces3()
       => TestRenameDocumentAsync(
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
       documentPath: @"Test\Path\Document.cs",
       documentName: @"Document.cs",
       newDocumentPath: @"Test\Path\After\Test\Document.cs");

        [Fact]
        public Task CSharp_RenameDocument_RenameMultipleNamespaces_Nested()
=> TestRenameDocumentAsync(
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
documentPath: @"Test\Path\Document.cs",
documentName: @"Document.cs",
newDocumentPath: @"Test\Path\After\Test\Document.cs");

        [Fact]
        public Task CSharp_RenameDocument_RenameNamespace2()
        => TestRenameDocumentAsync(
@"namespace Test.Path
{
    class C
    {
    }
}",
@"namespace Test
{
    class C
    {
    }
}",
        documentPath: @"Test\Path\Document.cs",
        documentName: @"Document.cs",
        newDocumentPath: @"Test\Document.cs");

        [Fact]
        public Task CSharp_RenameDocument_RenameNamespaceAndClass()
        => TestRenameDocumentAsync(
@"namespace Test.Path
{
    class C
    {
    }
}",
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

        [Fact]
        [WorkItem(46580, "https://github.com/dotnet/roslyn/issues/46580")]
        public Task CSharp_RenameDocument_MappedDocumentHasNoResults()
        {
            var documentName = "Component1.razor";
            var documentText =
@"<h3>Component1</h3>
@code {}";

            return TestRenameMappedFileAsync(documentText, documentName, newDocumentName: "MyComponent.razor");
        }
    }
}
