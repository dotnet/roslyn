// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

public sealed class XmlDocumentationProviderTests
{
    [Fact]
    public void XmlDocumentationProviderReturnsEntireMemberNode()
    {
        var roslynCompilersLocation = typeof(Compilation).Assembly.Location;
        var roslynCompilersXmlFilePath = Path.ChangeExtension(roslynCompilersLocation, ".xml");
        var documentationProvider = XmlDocumentationProvider.CreateFromBytes(Encoding.UTF8.GetBytes("""
<?xml version="1.0"?>
<doc>
    <assembly>
        <name>Microsoft.CodeAnalysis</name>
    </assembly>
    <members>
        <member name="T:Microsoft.CodeAnalysis.AdditionalTextFile">
            <summary>
            Represents a non source code file.
            </summary>
        </member>
    </members>
</doc>
"""));
        var portableExecutableReference = MetadataReference.CreateFromFile(roslynCompilersLocation, documentation: documentationProvider);

        var compilation = CSharpCompilation.Create(nameof(XmlDocumentationProviderReturnsEntireMemberNode), references: [portableExecutableReference]);

        // Verify we can parse it and it contains a single node
        var xml = compilation.GetTypeByMetadataName("Microsoft.CodeAnalysis.AdditionalTextFile")!.GetDocumentationCommentXml();
        AssertEx.NotNull(xml);
        var xmlDocument = XDocument.Parse(xml);
        Assert.Equal("member", xmlDocument.Root!.Name.LocalName);
        Assert.Equal("T:Microsoft.CodeAnalysis.AdditionalTextFile", xmlDocument.Root!.Attribute("name")!.Value);
    }
}
