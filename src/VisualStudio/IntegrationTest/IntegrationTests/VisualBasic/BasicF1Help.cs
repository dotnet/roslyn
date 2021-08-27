// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicF1Help : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicF1Help(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(BasicF1Help))
        {
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)]
        private void F1Help()
        {
            var text = @"
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program$$
    Sub Main(args As String())
        Dim query = From arg In args
                    Select args.Any(Function(a) a.Length > 5)
        Dim x = 0
        x += 1
    End Sub
    Public Function F() As Object
        Return Nothing
    End Function
End Module";

            SetUpEditor(text);
            Verify("Linq", "System.Linq");
            Verify("String", "vb.String");
            Verify("Any", "System.Linq.Enumerable.Any");
            Verify("From", "vb.QueryFrom");
            Verify("+=", "vb.+=");
            Verify("Nothing", "vb.Nothing");

        }

        private void Verify(string word, string expectedKeyword)
        {
            VisualStudio.Editor.PlaceCaret(word, charsOffset: -1);
            Assert.Contains(expectedKeyword, VisualStudio.Editor.GetF1Keyword());
        }
    }
}
