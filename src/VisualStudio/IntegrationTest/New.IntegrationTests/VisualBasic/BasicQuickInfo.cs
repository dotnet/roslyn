// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Trait(Traits.Feature, Traits.Features.QuickInfo)]
    public class BasicQuickInfo : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicQuickInfo()
            : base(nameof(BasicQuickInfo))
        {
        }

        [IdeFact]
        public async Task QuickInfo1()
        {
            await SetUpEditorAsync(@"
''' <summary>Hello!</summary>
Class Program
    Sub Main(ByVal args As String$$())
    End Sub
End Class", HangMitigatingCancellationToken);
            await TestServices.Editor.InvokeQuickInfoAsync(HangMitigatingCancellationToken);
            var quickInfo = await TestServices.Editor.GetQuickInfoAsync(HangMitigatingCancellationToken);
            Assert.Equal("Class System.String\r\nRepresents text as a sequence of UTF-16 code units.To browse the .NET Framework source code for this type, see the Reference Source.", quickInfo);
        }

        [IdeFact]
        public async Task International()
        {
            await SetUpEditorAsync(@"
''' <summary>
''' This is an XML doc comment defined in code.
''' </summary>
Class العربية123
    Shared Sub Goo()
         Dim goo as العربية123$$
    End Sub
End Class", HangMitigatingCancellationToken);
            await TestServices.Editor.InvokeQuickInfoAsync(HangMitigatingCancellationToken);
            var quickInfo = await TestServices.Editor.GetQuickInfoAsync(HangMitigatingCancellationToken);
            Assert.Equal(@"Class TestProj.العربية123
This is an XML doc comment defined in code.", quickInfo);
        }
    }
}
