// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicQuickInfo : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicQuickInfo(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(BasicQuickInfo))
        {
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/19914"), Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public void QuickInfo1()
        {
            SetUpEditor(@"
''' <summary>Hello!</summary>
Class Program
    Sub Main(ByVal args As String$$())
    End Sub
End Class");
            VisualStudio.Editor.InvokeQuickInfo();
            Assert.Equal("Class\u200e System.String\r\nRepresents text as a sequence of UTF-16 code units.To browse the .NET Framework source code for this type, see the Reference Source.", VisualStudio.Editor.GetQuickInfo());
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/19914"), Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public void International()
        {
            SetUpEditor(@"
''' <summary>
''' This is an XML doc comment defined in code.
''' </summary>
Class العربية123
    Shared Sub Goo()
         Dim goo as العربية123$$
    End Sub
End Class");
            VisualStudio.Editor.InvokeQuickInfo();
            Assert.Equal(@"Class" + '\u200e' + @" TestProj.العربية123
This is an XML doc comment defined in code.", VisualStudio.Editor.GetQuickInfo());
        }
    }
}
