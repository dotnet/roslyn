using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.ExtensionsTests
{
    public class DocumentExtensionsTests
    {
        private readonly AdhocWorkspace _workspace;
        private readonly Project _project;

        public DocumentExtensionsTests()
        {
            _workspace = new AdhocWorkspace();
            _project = _workspace.AddProject("test", LanguageNames.CSharp);
        }

        private Document GetDocument(string code)
        {
            return _project.AddDocument("test.cs", code);
        }

        [Fact]
        public async Task GetSemanticModelForSpanAsync_TextSpanOutsideDocumentThrows()
        {
            const string code = @"class Test { }";
            var document = GetDocument(code);
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => 
                await document.GetSemanticModelForSpanAsync(new TextSpan(code.Length, 1), CancellationToken.None));
        }
    }
}
