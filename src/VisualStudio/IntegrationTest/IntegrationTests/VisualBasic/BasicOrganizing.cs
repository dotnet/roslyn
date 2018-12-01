// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.Test.Utilities;

namespace Roslyn.VisualStudio.IntegrationTests.Basic
{
    [TestClass]
    public class BasicOrganizing : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicOrganizing( )
            : base( nameof(BasicOrganizing))
        {
        }

        [TestMethod, TestCategory(Traits.Features.Organizing)]
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
            VisualStudioInstance.ExecuteCommand("Edit.RemoveAndSort");
            VisualStudioInstance.Editor.Verify.TextContains(@"Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Class Test
    Sub Method(<CallerMemberName> Optional str As String = Nothing)
        Dim data As COMException
    End Sub
End Class");

        }
    }
}
