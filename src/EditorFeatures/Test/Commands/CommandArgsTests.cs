// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.EditorUtilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Commands
{
    public class CommandArgsTests
    {
        [Fact]
        public void CreateBackspaceCommandArgsWithNullTextView()
        {
            var buffer = EditorFactory.CreateBuffer(TestExportProvider.ExportProviderWithCSharpAndVisualBasic, "class C { }");

            Assert.Throws<ArgumentNullException>(() =>
                new BackspaceKeyCommandArgs(null, buffer));
        }

        [WpfFact]
        public void CreateBackspaceCommandArgsWithNullSubjectBuffer()
        {
            using (var disposableView = EditorFactory.CreateView(TestExportProvider.ExportProviderWithCSharpAndVisualBasic, "class C { }"))
            {
                Assert.Throws<ArgumentNullException>(() =>
    new BackspaceKeyCommandArgs(disposableView.TextView, null));
            }
        }
#if false
        [WpfFact]
        public void TestTextViewProperty()
        {
            var view = new StandardBufferView(EditorFactory.CreateView(TestExportProvider.ExportProvider, "class C { }"));

            var args = new BackspaceKeyCommandArgs(view, view.TextBuffer);
            Assert.Equal(view, args.TextView);
        }

        [WpfFact]
        public void TestSubjectProperty()
        {
            var view = new StandardBufferView(EditorFactory.CreateView(TestExportProvider.ExportProvider, "class C { }"));

            var args = new BackspaceKeyCommandArgs(view, view.TextBuffer);
            Assert.Equal(view.TextBuffer, args.SubjectBuffer);
        }

        [WpfFact]
        public void TestInvokeQuickInfoCommandArgs()
        {
            var view = new StandardBufferView(EditorFactory.CreateView(TestExportProvider.ExportProvider, "class C { }"));
            new InvokeQuickInfoCommandArgs(view, view.TextBuffer);
        }
#endif
    }
}
