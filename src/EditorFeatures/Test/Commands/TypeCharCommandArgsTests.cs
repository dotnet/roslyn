// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.EditorUtilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Commands
{
    [UseExportProvider]
    public class TypeCharCommandArgsTests
    {
        [WpfFact]
        public void TestTypedCharProperty()
        {
            using var disposableView = EditorFactory.CreateView(TestExportProvider.ExportProviderWithCSharpAndVisualBasic, "class C { }");
            var args = new TypeCharCommandArgs(disposableView.TextView, disposableView.TextView.TextBuffer, 'c');
            Assert.Equal('c', args.TypedChar);
        }
    }
}
