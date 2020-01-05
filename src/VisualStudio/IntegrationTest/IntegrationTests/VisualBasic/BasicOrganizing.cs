// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.IntegrationTests.Basic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicOrganizing : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicOrganizing(VisualStudioInstanceFactory instanceFactory, ITestOutputHelper testOutputHelper)
            : base(instanceFactory, testOutputHelper, nameof(BasicOrganizing))
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
