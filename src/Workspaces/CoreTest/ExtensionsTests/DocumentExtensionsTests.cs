using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.ExtensionsTests
{
    public class DocumentExtensionsTests
    {
        private readonly AdhocWorkspace _ws = new AdhocWorkspace();
        private readonly Project _emptyProject;

        public DocumentExtensionsTests()
        {
            _emptyProject = _ws.AddProject(
                ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                    VersionStamp.Default,
                    "test",
                    "test.dll",
                    LanguageNames.CSharp,
                    metadataReferences: new[] { TestReferences.NetFx.v4_0_30319.mscorlib }));
        }

        private Document GetDocument(string code)
        {
            return _emptyProject.AddDocument("test.cs", code);
        }

        [Fact]
        public async Task GetSemanticModelForSpanAsync_TextSpanOutsideDocument()
        {
            const string code = @"class Test { }";
            var document = GetDocument(code);
            var expected = await document.GetSemanticModelAsync();
            var actual = await document.GetSemanticModelForSpanAsync(new TextSpan(code.Length, 1), CancellationToken.None);
            Assert.Same(expected, actual);
        }
    }
}
