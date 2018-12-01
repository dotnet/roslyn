// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.Test.Utilities;

namespace Roslyn.VisualStudio.IntegrationTests.Basic
{
    [TestClass]
    public class BasicOutlining : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicOutlining() : base(nameof(BasicOutlining)) { }

        [TestMethod, TestCategory(Traits.Features.Outlining)]
        public void Outlining()
        {
            var input = @"
[|Imports System
Imports System.Text|]

[|Namespace Acme
    [|Module Module1
        [|Sub Main()
            
        End Sub|]
    End Module|]
End Namespace|]";
            MarkupTestFile.GetSpans(input, out var text, out ImmutableArray<TextSpan> spans);
            VisualStudioInstance.Editor.SetText(text);
            Assert.AreEqual(spans.OrderBy(s => s.Start), VisualStudioInstance.Editor.GetOutliningSpans());
        }
    }
}
