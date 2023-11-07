// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicOutlining : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicOutlining(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(BasicOutlining))
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
            MarkupTestFile.GetSpans(input, out var text, out var spans);
            VisualStudio.Editor.SetText(text);
            Assert.Equal(spans.OrderBy(s => s.Start), VisualStudio.Editor.GetOutliningSpans());
        }
    }
}
