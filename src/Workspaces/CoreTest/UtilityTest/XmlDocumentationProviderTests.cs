// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
        Assert.NotNull(xml);
        var xmlDocument = XDocument.Parse(xml);
        Assert.Equal("member", xmlDocument.Root!.Name.LocalName);
        Assert.Equal("T:Microsoft.CodeAnalysis.AdditionalTextFile", xmlDocument.Root!.Attribute("name")!.Value);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/82258")]
    public void XmlDocumentationProviderResolvesModernExtensionMemberFromEmittedXml()
    {
        var declaringSource = """
public static class E
{
    /// <summary>Legacy summary.</summary>
    public static void Legacy(this object o) { }

    extension(object o)
    {
        /// <summary>Modern summary.</summary>
        public void Modern() { }
    }
}
""";
        var parseOptions = new CSharpParseOptions(documentationMode: DocumentationMode.Diagnose, languageVersion: LanguageVersion.Preview);
        var declaringComp = CSharpCompilation.Create(
            "Lib",
            [CSharpSyntaxTree.ParseText(declaringSource, parseOptions)],
            NetCoreApp.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var peStream = new MemoryStream();
        using var xmlStream = new MemoryStream();
        var emitResult = declaringComp.Emit(peStream, xmlDocumentationStream: xmlStream);
        Assert.True(emitResult.Success, string.Join("\r\n", emitResult.Diagnostics));

        var xmlBytes = xmlStream.ToArray();
        var emittedXml = Encoding.UTF8.GetString(xmlBytes);
        var reference = AssemblyMetadata.CreateFromImage(peStream.ToArray()).GetReference(documentation: XmlDocumentationProvider.CreateFromBytes(xmlBytes));

        var consumingSource = """
class C
{
    void M()
    {
        new object().Legacy();
        new object().Modern();
    }
}
""";
        var consumingTree = CSharpSyntaxTree.ParseText(consumingSource, parseOptions);
        var consumingComp = CSharpCompilation.Create(
            "Consumer",
            [consumingTree],
            NetCoreApp.References.Concat([reference]),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var diagnostics = consumingComp.GetDiagnostics();
        Assert.True(diagnostics.All(diagnostic => diagnostic.Severity != DiagnosticSeverity.Error), string.Join("\r\n", diagnostics));

        var model = consumingComp.GetSemanticModel(consumingTree);
        var invocations = consumingTree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();
        var legacy = model.GetSymbolInfo(invocations.Single(invocation => invocation.ToString() == "new object().Legacy()")).Symbol!;
        var modern = model.GetSymbolInfo(invocations.Single(invocation => invocation.ToString() == "new object().Modern()")).Symbol!;

        // The compiler emits the modern extension member's summary under the (grouping-named) skeleton member
        // and an <inheritdoc> entry for the synthesized implementation method.
        Assert.Contains("""<summary>Legacy summary.</summary>""", emittedXml);
        Assert.Contains("<inheritdoc cref=", emittedXml);
        Assert.Contains("""<summary>Modern summary.</summary>""", emittedXml);

        // Both the legacy and the modern extension member resolve their documentation across the metadata
        // boundary from the real on-disk XML file (the NuGet package scenario from issue #82258).
        Assert.Contains("""<summary>Legacy summary.</summary>""", legacy.GetDocumentationCommentXml());
        Assert.Contains("""<summary>Modern summary.</summary>""", modern.GetDocumentationCommentXml());

        Assert.True(modern.ContainingType.IsExtension);
    }
}
