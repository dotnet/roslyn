// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicOrganizing : AbstractIdeEditorTest
    {
        public BasicOrganizing()
            : base(nameof(BasicOrganizing))
        {
        }

        protected override string LanguageName => LanguageNames.VisualBasic;

        [IdeFact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task RemoveAndSortAsync()
        {
            await SetUpEditorAsync(@"Imports System.Linq$$
Imports System
Imports System.Runtime.InteropServices
Imports System.Runtime.CompilerServices
Class Test
    Sub Method(<CallerMemberName> Optional str As String = Nothing)
        Dim data As COMException
    End Sub
End Class");
            await VisualStudio.VisualStudio.ExecuteCommandAsync(WellKnownCommandNames.Edit_RemoveAndSort);
            await VisualStudio.Editor.Verify.TextContainsAsync(@"Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Class Test
    Sub Method(<CallerMemberName> Optional str As String = Nothing)
        Dim data As COMException
    End Sub
End Class");

        }
    }
}
