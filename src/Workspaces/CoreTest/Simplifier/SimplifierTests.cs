using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Simplifier
{
    public class SimplifierTests : WorkspaceTestBase
    {
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void TestExpandAsync()
        {
            AssertThrows<ArgumentNullException>(
                () => { var expandedNode = Simplification.Simplifier.ExpandAsync<SyntaxNode>(null, null).Result; },
                exception => Assert.Equal(exception.ParamName, "node"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void TestExpandAsync2()
        {
            var node = GetSyntaxNode();
            AssertThrows<ArgumentNullException>(
                () => { var expandedNode = Simplification.Simplifier.ExpandAsync<SyntaxNode>(node, null).Result; },
                exception => Assert.Equal(exception.ParamName, "document"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void TestExpand()
        {
            AssertThrows<ArgumentNullException>(
                () => { var expandedNode = Simplification.Simplifier.Expand<SyntaxNode>(null, null, null); },
                exception => Assert.Equal(exception.ParamName, "node"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void TestExpand2()
        {
            var node = GetSyntaxNode();
            AssertThrows<ArgumentNullException>(
                () => { var expandedNode = Simplification.Simplifier.Expand<SyntaxNode>(node, null, null); },
                exception => Assert.Equal(exception.ParamName, "semanticModel"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void TestExpand3()
        {
            var node = GetSyntaxNode();
            var semanticModel = GetSemanticModel();
            AssertThrows<ArgumentNullException>(
                () => { var expandedNode = Simplification.Simplifier.Expand<SyntaxNode>(node, semanticModel, null); },
                exception => Assert.Equal(exception.ParamName, "workspace"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void TestTokenExpandAsync()
        {
            AssertThrows<ArgumentNullException>(
                () => { var expandedNode = Simplification.Simplifier.ExpandAsync(default(SyntaxToken), null, expandInsideNode: null, cancellationToken: default(CancellationToken)).Result; },
                exception => Assert.Equal(exception.ParamName, "document"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void TestTokenExpand()
        {
            AssertThrows<ArgumentNullException>(
                () => { var expandedNode = Simplification.Simplifier.Expand(default(SyntaxToken), null, null, expandInsideNode: null, cancellationToken: default(CancellationToken)); },
                exception => Assert.Equal(exception.ParamName, "semanticModel"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void TestTokenExpand2()
        {
            var semanticModel = GetSemanticModel();
            AssertThrows<ArgumentNullException>(
                () => { var expandedNode = Simplification.Simplifier.Expand(default(SyntaxToken), semanticModel, null, expandInsideNode: null, cancellationToken: default(CancellationToken)); },
                exception => Assert.Equal(exception.ParamName, "workspace"));
        }


        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void TestReduceAsync()
        {
            AssertThrows<ArgumentNullException>(
                () => { var simplifiedNode = Simplification.Simplifier.ReduceAsync(null).Result; },
                exception => Assert.Equal(exception.ParamName, "document"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void TestReduceAsync2()
        {
            AssertThrows<ArgumentNullException>(
                () => { var simplifiedNode = Simplification.Simplifier.ReduceAsync(null, (SyntaxAnnotation)null).Result; },
                exception => Assert.Equal(exception.ParamName, "document"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void TestReduceAsync3()
        {
            var document = GetDocument();
            AssertThrows<ArgumentNullException>(
                () => { var simplifiedNode = Simplification.Simplifier.ReduceAsync(document, (SyntaxAnnotation)null).Result; },
                exception => Assert.Equal(exception.ParamName, "annotation"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void TestReduceAsync4()
        {
            AssertThrows<ArgumentNullException>(
                () => { var simplifiedNode = Simplification.Simplifier.ReduceAsync(null, default(TextSpan)).Result; },
                exception => Assert.Equal(exception.ParamName, "document"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void TestReduceAsync5()
        {
            AssertThrows<ArgumentNullException>(
                () => { var simplifiedNode = Simplification.Simplifier.ReduceAsync(null, default(IEnumerable<TextSpan>)).Result; },
                exception => Assert.Equal(exception.ParamName, "document"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void TestReduceAsync6()
        {
            var document = GetDocument();
            AssertThrows<ArgumentNullException>(
                () => { var simplifiedNode = Simplification.Simplifier.ReduceAsync(document, default(IEnumerable<TextSpan>)).Result; },
                exception => Assert.Equal(exception.ParamName, "spans"));
        }

        private Document GetDocument()
        {
            CreateFiles(GetSimpleCSharpSolutionFiles());
            var sol = MSBuild.MSBuildWorkspace.Create(properties: new Dictionary<string, string> { { "Configuration", "Release" } })
                                      .OpenSolutionAsync(GetSolutionFileName("TestSolution.sln")).Result;
            var project = sol.Projects.First();
            return project.Documents.First();
        }

        private SemanticModel GetSemanticModel() => GetDocument().GetSemanticModelAsync().Result;

        private static SyntaxNode GetSyntaxNode() => CSharp.SyntaxFactory.IdentifierName(CSharp.SyntaxFactory.Identifier("Test"));
    }
}
