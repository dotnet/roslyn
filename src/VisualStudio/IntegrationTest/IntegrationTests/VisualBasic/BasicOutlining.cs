// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicOutlining : AbstractIdeEditorTest
    {
        public BasicOutlining()
            : base(nameof(BasicOutlining))
        {
        }

        protected override string LanguageName => LanguageNames.VisualBasic;

        [IdeFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task OutliningAsync()
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
            await VisualStudio.Editor.SetTextAsync(text);
            Assert.Equal(spans.OrderBy(s => s.Start), await VisualStudio.Editor.GetOutliningSpansAsync());
        }
    }
}
