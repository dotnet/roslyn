// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Formatting
{
    public class FormattingTreeEditTests : CSharpFormattingTestBase
    {
        private Document GetDocument(string code)
        {
            var ws = new AdhocWorkspace();
            var project = ws.AddProject("project", LanguageNames.CSharp);
            return project.AddDocument("code", SourceText.From(code));
        }

        [Fact]
        public async Task SpaceAfterAttribute()
        {
            string code = @"
public class C
{
    void M(int? p) { }
}
";
            var document = GetDocument(code);
            var g = SyntaxGenerator.GetGenerator(document);
            var root = document.GetSyntaxRootAsync().Result;
            var attr = g.Attribute("MyAttr");

            var param = root.DescendantNodes().OfType<ParameterSyntax>().First();
            Assert.Equal(@"
public class C
{
    void M([MyAttr] int? p) { }
}
", (await Formatter.FormatAsync(root.ReplaceNode(param, g.AddAttributes(param, g.Attribute("MyAttr"))),
    document.Project.Solution.Workspace)).ToFullString());

            // verify change doesn't affect how attributes appear before other kinds of declarations
            var method = root.DescendantNodes().OfType<MethodDeclarationSyntax>().First();
            Assert.Equal(@"
public class C
{
    [MyAttr]
    void M(int? p) { }
}
", (await Formatter.FormatAsync(root.ReplaceNode(method, g.AddAttributes(method, g.Attribute("MyAttr"))),
    document.Project.Solution.Workspace)).ToFullString());
        }
    }
}