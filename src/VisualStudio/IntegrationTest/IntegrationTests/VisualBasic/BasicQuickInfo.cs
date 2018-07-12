// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicQuickInfo : AbstractIdeEditorTest
    {
        public BasicQuickInfo()
            : base(nameof(BasicQuickInfo))
        {
        }

        protected override string LanguageName => LanguageNames.VisualBasic;

        [IdeFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task QuickInfo1Async()
        {
            await SetUpEditorAsync(@"
''' <summary>Hello!</summary>
Class Program
    Function Main(ByVal args As String()) As Integer$$
        Return 0
    End Function
End Class");
            await VisualStudio.Editor.InvokeQuickInfoAsync();
            Assert.Equal("Structure\u200e System.Int32\r\nRepresents a 32-bit signed integer.To browse the .NET Framework source code for this type, see the Reference Source.", await VisualStudio.Editor.GetQuickInfoAsync());
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task InternationalAsync()
        {
            await SetUpEditorAsync(@"
''' <summary>
''' This is an XML doc comment defined in code.
''' </summary>
Class العربية123
    Shared Sub Goo()
         Dim goo as العربية123$$
    End Sub
End Class");
            await VisualStudio.Editor.InvokeQuickInfoAsync();
            Assert.Equal(@"Class" + '\u200e' + @" TestProj.العربية123
This is an XML doc comment defined in code.", await VisualStudio.Editor.GetQuickInfoAsync());
        }
    }
}
