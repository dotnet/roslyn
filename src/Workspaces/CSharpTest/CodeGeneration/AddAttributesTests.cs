using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGeneration
{
    [UseExportProvider]
    public class AddAttributesTests
    {
        private Document GetDocument(string code)
        {
            var ws = new AdhocWorkspace();
            var emptyProject = ws.AddProject(
                ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                    VersionStamp.Default,
                    "test",
                    "test.dll",
                    LanguageNames.CSharp,
                    metadataReferences: new[] { TestReferences.NetFx.v4_0_30319.mscorlib }));

            return emptyProject.AddDocument("test.cs", code);
        }

        private async Task TestAsync(string initialText, string attributeAddedText)
        {
            var doc = GetDocument(initialText);
            var options = await doc.GetOptionsAsync();

            var attributeList =
                SyntaxFactory.AttributeList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Attribute(
                            SyntaxFactory.IdentifierName("System.Reflection.AssemblyVersion(\"1.0.0.0\")"))))
                .WithTarget(
                    SyntaxFactory.AttributeTargetSpecifier(
                        SyntaxFactory.Token(SyntaxKind.AssemblyKeyword)));

            var syntaxRoot = await doc.GetSyntaxRootAsync();
            var editor = await DocumentEditor.CreateAsync(doc);

            editor.AddAttribute(syntaxRoot, attributeList);

            var changedDoc = editor.GetChangedDocument();

            if (attributeAddedText != null)
            {
                var formatted = await Formatter.FormatAsync(changedDoc, SyntaxAnnotation.ElasticAnnotation, options);
                var actualText = (await formatted.GetTextAsync()).ToString();

                Assert.Equal(attributeAddedText, actualText);
            }
        }

        [Fact]
        public async Task TestAddAssemblyAttributeListToEmptyDocument()
        {
            await TestAsync(
string.Empty,
@"[assembly: System.Reflection.AssemblyVersion(""1.0.0.0"")]
");
        }

        [Fact]
        public async Task TestAddAssemblyAttributeListToDocumentWithOtherAssemblyAttribute()
        {
            await TestAsync(
@"[assembly: System.Reflection.AssemblyName(""Test"")]",
@"[assembly: System.Reflection.AssemblyName(""Test"")]
[assembly: System.Reflection.AssemblyVersion(""1.0.0.0"")]
");
        }
    }
}
