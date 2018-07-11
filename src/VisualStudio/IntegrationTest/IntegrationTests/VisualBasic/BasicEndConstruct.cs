// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicEndConstruct : AbstractIdeEditorTest
    {
        public BasicEndConstruct()
            : base(nameof(BasicEndConstruct))
        {
        }

        protected override string LanguageName => LanguageNames.VisualBasic;

        [IdeFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)]
        public async Task EndConstructAsync()
        {
            await SetUpEditorAsync(@"
Class Program
    Sub Main()
        If True Then $$
    End Sub
End Class");
            // Send a space to convert virtual whitespace into real whitespace
            await VisualStudio.Editor.SendKeysAsync(VirtualKey.Enter, " ");
            await VisualStudio.Editor.Verify.TextContainsAsync(@"
Class Program
    Sub Main()
        If True Then
             $$
        End If
    End Sub
End Class", assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)]
        public async Task IntelliSenseCompletedWhileAsync()
        {
            await SetUpEditorAsync(@"
Class Program
    Sub Main()
        $$
    End Sub
End Class");
            // Send a space to convert virtual whitespace into real whitespace
            await VisualStudio.Editor.SendKeysAsync("While True", VirtualKey.Enter, " ");
            await VisualStudio.Editor.Verify.TextContainsAsync(@"
Class Program
    Sub Main()
        While True
             $$
        End While
    End Sub
End Class", assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)]
        public async Task InterfaceToClassFixupAsync()
        {
            await SetUpEditorAsync(@"
Interface$$ C
End Interface");

            await VisualStudio.Editor.SendKeysAsync(new KeyPress(VirtualKey.Backspace, ShiftState.Ctrl));
            await VisualStudio.Editor.SendKeysAsync("Class", VirtualKey.Tab);
            await VisualStudio.Editor.Verify.TextContainsAsync(@"
Class C
End Class");
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)]
        public async Task CaseInsensitveSubToFunctionAsync()
        {
            await SetUpEditorAsync(@"
Class C
    Public Sub$$ Goo()
    End Sub
End Class");

            await VisualStudio.Editor.SendKeysAsync(new KeyPress(VirtualKey.Backspace, ShiftState.Ctrl));
            await VisualStudio.Editor.SendKeysAsync("fu", VirtualKey.Tab);
            await VisualStudio.Editor.Verify.TextContainsAsync(@"
Class C
    Public Function Goo()
    End Function
End Class");
        }
    }
}
