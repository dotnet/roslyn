// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.IntegrationTests.Basic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicOutlining : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicOutlining(VisualStudioInstanceFactory instanceFactory, ITestOutputHelper testOutputHelper)
            : base(instanceFactory, testOutputHelper, nameof(BasicOutlining))
        {
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
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
            VisualStudio.Editor.SetText(text);
            Assert.Equal(spans.OrderBy(s => s.Start), VisualStudio.Editor.GetOutliningSpans());
        }
    }
}
