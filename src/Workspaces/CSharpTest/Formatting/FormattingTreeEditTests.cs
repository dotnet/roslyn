// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Formatting
{
    public class FormattingTreeEditTests : CSharpFormattingTestBase
    {
        private static Document GetDocument(string code)
        {
            var ws = new AdhocWorkspace();
            var project = ws.AddProject("project", LanguageNames.CSharp);
            return project.AddDocument("code", SourceText.From(code));
        }

        [Fact]
        public async Task SpaceAfterAttribute()
        {
            var code = @"
public class C
{
    void M(int? p) { }
}
";
            var document = GetDocument(code);
            var g = SyntaxGenerator.GetGenerator(document);
            var root = await document.GetSyntaxRootAsync();
            var attr = g.Attribute("MyAttr");
            var options = CSharpSyntaxFormattingOptions.Default;

            var param = root.DescendantNodes().OfType<ParameterSyntax>().First();
            var root1 = root.ReplaceNode(param, g.AddAttributes(param, g.Attribute("MyAttr")));

            var result1 = Formatter.Format(root1, document.Project.Solution.Workspace.Services, options, CancellationToken.None);

            Assert.Equal(@"
public class C
{
    void M([MyAttr] int? p) { }
}
", result1.ToFullString());

            // verify change doesn't affect how attributes appear before other kinds of declarations
            var method = root.DescendantNodes().OfType<MethodDeclarationSyntax>().First();

            var root2 = root.ReplaceNode(method, g.AddAttributes(method, g.Attribute("MyAttr")));
            var result2 = Formatter.Format(root2, document.Project.Solution.Workspace.Services, options, CancellationToken.None);

            Assert.Equal(@"
public class C
{
    [MyAttr]
    void M(int? p) { }
}
", result2.ToFullString());
        }
    }
}
