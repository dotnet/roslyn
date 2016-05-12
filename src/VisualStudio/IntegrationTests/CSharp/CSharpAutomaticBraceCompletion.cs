// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.IntegrationTests
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpAutomaticBraceCompletion : IDisposable
    {
        private readonly VisualStudioInstanceContext _visualStudio;
        private readonly Workspace _workspace;
        private readonly Solution _solution;
        private readonly Project _project;
        private readonly EditorWindow _editorWindow;

        public CSharpAutomaticBraceCompletion(VisualStudioInstanceFactory instanceFactory)
        {
            _visualStudio = instanceFactory.GetNewOrUsedInstance();

            _solution = _visualStudio.Instance.SolutionExplorer.CreateSolution(nameof(CSharpAutomaticBraceCompletion));
            _project = _solution.AddProject("TestProj", ProjectTemplate.ClassLibrary, ProjectLanguage.CSharp);

            _workspace = _visualStudio.Instance.Workspace;
            _workspace.UseSuggestionMode = false;

            _editorWindow = _visualStudio.Instance.EditorWindow;
        }

        public void Dispose()
        {
            _visualStudio.Dispose();
        }

        [Fact]
        public async Task BracesInsertionAndTabCompleting()
        {
            _editorWindow.Text = @"class C {
    void Foo() {
        // Marker
    }
}";

            _editorWindow.PlaceCursor("// Marker");

            await _editorWindow.TypeTextAsync("if (true) {");
            
            Assert.Equal("        if (true) { ", _editorWindow.CurrentLineTextBeforeCursor);
            Assert.Equal("}", _editorWindow.CurrentLineTextAfterCursor);

            await _editorWindow.TypeTextAsync($"{EditorWindow.TAB}");

            Assert.Equal("        if (true) { }", _editorWindow.CurrentLineTextBeforeCursor);
            Assert.Equal(string.Empty, _editorWindow.CurrentLineTextAfterCursor);
        }

        [Fact]
        public async Task BracesOvertyping()
        {
            _editorWindow.Text = @"class C {
    void Foo() {
        // Marker
    }
}";

            _editorWindow.PlaceCursor("// Marker");

            await _editorWindow.TypeTextAsync("if (true) {");
            await _editorWindow.TypeTextAsync("}");

            Assert.Equal("        if (true) { }", _editorWindow.CurrentLineTextBeforeCursor);
            Assert.Equal(string.Empty, _editorWindow.CurrentLineTextAfterCursor);
        }

        [Fact]
        public async Task BracesOnReturnNoFormattingOnlyIndentationBeforeCloseBrace()
        {
            _editorWindow.Text = @"class C {
    void Foo() {
        // Marker
    }
}";

            _editorWindow.PlaceCursor("// Marker");

            await _editorWindow.TypeTextAsync("if (true) {");
            await _editorWindow.TypeTextAsync($"{EditorWindow.ENTER}");
            await _editorWindow.TypeTextAsync("var a = 1;");

            Assert.Equal("            var a = 1;", _editorWindow.CurrentLineTextBeforeCursor);
            Assert.Equal(string.Empty, _editorWindow.CurrentLineTextAfterCursor);
        }

        [Fact]
        public async Task BracesOnReturnOvertypingTheClosingBrace()
        {
            _editorWindow.Text = @"class C {
    void Foo() {
        // Marker
    }
}";

            _editorWindow.PlaceCursor("// Marker");

            await _editorWindow.TypeTextAsync("if (true) {");
            await _editorWindow.TypeTextAsync($"{EditorWindow.ENTER}");
            await _editorWindow.TypeTextAsync("var a = 1;}");

            Assert.Equal("        }", _editorWindow.CurrentLineTextBeforeCursor);
            Assert.Equal(string.Empty, _editorWindow.CurrentLineTextAfterCursor);

            Assert.Contains(@"if (true)
        {
            var a = 1;
        }
", _editorWindow.Text);
        }

        [Fact]
        [WorkItem(653540, "DevDiv")]
        public async Task BracesOnReturnWithNonWhitespaceSpanInside()
        {
            _editorWindow.Text = string.Empty;

            await _editorWindow.TypeTextAsync("class A { int i;");
            await _editorWindow.TypeTextAsync($"{EditorWindow.ENTER}");

            Assert.Equal(string.Empty, _editorWindow.CurrentLineTextBeforeCursor);
            Assert.Equal("}", _editorWindow.CurrentLineTextAfterCursor);

            Assert.Contains(@"class A { int i;
}", _editorWindow.Text);
        }

        [Fact]
        public async Task ParenInsertionAndTabCompleting()
        {
            _editorWindow.Text = @"class C {
    //Marker
}";

            _editorWindow.PlaceCursor("// Marker");

            await _editorWindow.TypeTextAsync("void Foo(");

            Assert.Equal("    void Foo(", _editorWindow.CurrentLineTextBeforeCursor);
            Assert.Equal(")", _editorWindow.CurrentLineTextAfterCursor);

            await _editorWindow.TypeTextAsync("int x");
            await _editorWindow.TypeTextAsync($"{EditorWindow.TAB}");

            Assert.Equal("    void Foo(int x)", _editorWindow.CurrentLineTextBeforeCursor);
            Assert.Equal(string.Empty, _editorWindow.CurrentLineTextAfterCursor);
        }

        [Fact]
        public async Task ParenOvertyping()
        {
            _editorWindow.Text = @"class C {
    //Marker
}";

            _editorWindow.PlaceCursor("// Marker");

            await _editorWindow.TypeTextAsync("void Foo(");
            await _editorWindow.TypeTextAsync($"{EditorWindow.ESC}");
            await _editorWindow.TypeTextAsync(")");

            Assert.Equal("    void Foo()", _editorWindow.CurrentLineTextBeforeCursor);
            Assert.Equal(")", _editorWindow.CurrentLineTextAfterCursor);
        }

        [Fact]
        public async Task SquareBracketInsertion()
        {
            _editorWindow.Text = @"class C {
    //Marker
}";

            _editorWindow.PlaceCursor("// Marker");

            await _editorWindow.TypeTextAsync("int [");

            Assert.Equal("    int [", _editorWindow.CurrentLineTextBeforeCursor);
            Assert.Equal("]", _editorWindow.CurrentLineTextAfterCursor);
        }

        [Fact]
        public async Task SquareBracketOvertyping()
        {
            _editorWindow.Text = @"class C {
    //Marker
}";

            _editorWindow.PlaceCursor("// Marker");

            await _editorWindow.TypeTextAsync("int [");
            await _editorWindow.TypeTextAsync("]");

            Assert.Equal("    int []", _editorWindow.CurrentLineTextBeforeCursor);
            Assert.Equal(string.Empty, _editorWindow.CurrentLineTextAfterCursor);
        }
    }
}
