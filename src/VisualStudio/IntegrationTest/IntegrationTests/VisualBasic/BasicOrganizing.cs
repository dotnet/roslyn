// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicOrganizing : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicOrganizing(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(BasicOrganizing))
        {
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public void RemoveAndSort()
        {
            SetUpEditor(@"Imports System.Linq$$
Imports System
Imports System.Runtime.InteropServices
Imports System.Runtime.CompilerServices
Class Test
    Sub Method(<CallerMemberName> Optional str As String = Nothing)
        Dim data As COMException
    End Sub
End Class");
            VisualStudio.ExecuteCommand("Edit.RemoveAndSort");
            VisualStudio.Workspace.WaitForAsyncOperations(
                Helper.HangMitigatingTimeout,
                FeatureAttribute.OrganizeDocument);
            VisualStudio.Editor.Verify.TextContains(@"Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Class Test
    Sub Method(<CallerMemberName> Optional str As String = Nothing)
        Dim data As COMException
    End Sub
End Class");

        }
    }
}
