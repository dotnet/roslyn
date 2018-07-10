// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicF1Help : AbstractIdeEditorTest
    {
        public BasicF1Help()
            : base(nameof(BasicF1Help))
        {
        }

        protected override string LanguageName => LanguageNames.VisualBasic;

        [IdeFact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task F1HelpAsync()
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

            await SetUpEditorAsync(text);
            await VerifyAsync("Linq", "System.Linq");
            await VerifyAsync("String", "vb.String");
            await VerifyAsync("Any", "System.Linq.Enumerable.Any");
            await VerifyAsync("From", "vb.QueryFrom");
            await VerifyAsync("+=", "vb.+=");
            await VerifyAsync("Nothing", "vb.Nothing");

        }

        private async Task VerifyAsync(string word, string expectedKeyword)
        {
            await VisualStudio.Editor.PlaceCaretAsync(word, charsOffset: -1);
            Assert.Contains(expectedKeyword, await VisualStudio.Editor.GetF1KeywordsAsync());
        }
    }
}
