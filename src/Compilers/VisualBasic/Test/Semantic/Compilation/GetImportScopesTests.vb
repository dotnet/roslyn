' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class GetImportScopesTests
        Inherits SemanticModelTestBase

        <Fact>
        Public Sub TestEmptyFile()
            Dim text = "'pos"
            Dim tree = Parse(Text)
            Dim comp = CreateCompilation(tree)
            Dim model = comp.GetSemanticModel(tree)
            Dim scopes = model.GetImportScopes(FindPositionFromText(tree, "'pos"))
            Assert.Empty(scopes)
        End Sub

        <Fact>
        Public Sub TestNoImportsBeforeMemberDeclaration()
            Dim Text = "'pos
class C
end class"
            Dim tree = Parse(Text)
            Dim comp = CreateCompilation(tree)
            Dim model = comp.GetSemanticModel(tree)
            Dim scopes = model.GetImportScopes(FindPositionFromText(tree, "'pos"))
            Assert.Empty(scopes)
        End Sub
    End Class
End Namespace
