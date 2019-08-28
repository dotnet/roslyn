// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicEndConstruct : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicEndConstruct(VisualStudioInstanceFactory instanceFactory, ITestOutputHelper testOutputHelper)
            : base(instanceFactory, testOutputHelper, nameof(BasicEndConstruct))
        {
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)]
        public void EndConstruct()
        {
            SetUpEditor(@"
Class Program
    Sub Main()
        If True Then $$
    End Sub
End Class");
            // Send a space to convert virtual whitespace into real whitespace
            VisualStudio.Editor.SendKeys(VirtualKey.Enter, " ");
            VisualStudio.Editor.Verify.TextContains(@"
Class Program
    Sub Main()
        If True Then
             $$
        End If
    End Sub
End Class", assertCaretPosition: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)]
        public void IntelliSenseCompletedWhile()
        {
            SetUpEditor(@"
Class Program
    Sub Main()
        $$
    End Sub
End Class");
            // Send a space to convert virtual whitespace into real whitespace
            VisualStudio.Editor.SendKeys("While True", VirtualKey.Enter, " ");
            VisualStudio.Editor.Verify.TextContains(@"
Class Program
    Sub Main()
        While True
             $$
        End While
    End Sub
End Class", assertCaretPosition: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)]
        public void InterfaceToClassFixup()
        {
            SetUpEditor(@"
Interface$$ C
End Interface");

            VisualStudio.Editor.SendKeys(new KeyPress(VirtualKey.Backspace, ShiftState.Ctrl));
            VisualStudio.Editor.SendKeys("Class", VirtualKey.Tab);
            VisualStudio.Editor.Verify.TextContains(@"
Class C
End Class");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)]
        public void CaseInsensitveSubToFunction()
        {
            SetUpEditor(@"
Class C
    Public Sub$$ Goo()
    End Sub
End Class");

            VisualStudio.Editor.SendKeys(new KeyPress(VirtualKey.Backspace, ShiftState.Ctrl));
            VisualStudio.Editor.SendKeys("fu", VirtualKey.Tab);
            VisualStudio.Editor.Verify.TextContains(@"
Class C
    Public Function Goo()
    End Function
End Class");
        }
    }
}
