' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Test.Utilities.TextStructureNavigation

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.TextStructureNavigation
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.TextStructureNavigator)>
    Public Class TextStructureNavigatorTests
        Inherits AbstractTextStructureNavigatorTests

        Protected Overrides ReadOnly Property ContentType As String = ContentTypeNames.VisualBasicContentType

        Protected Overrides Function CreateWorkspace(code As String) As EditorTestWorkspace
            Return EditorTestWorkspace.CreateVisualBasic(code)
        End Function

        <Fact>
        Public Sub TestEmpty()
            AssertExtent(
                "{|Insignificant:$$|}")
        End Sub

        <WpfFact>
        Public Sub TestWhitespace()
            AssertExtent(
                "{|Insignificant:$$   |}")

            AssertExtent(
                "{|Insignificant: $$  |}")

            AssertExtent(
                "{|Insignificant:   |}$$")
        End Sub

        <WpfFact>
        Public Sub TestEndOfFile()
            AssertExtent(
                "Imports {|Significant:System|}$$")
        End Sub

        <Fact>
        Public Sub TestNewLine()
            AssertExtent(
                "Module Module1{|Insignificant:$$
|}
End Module")

            AssertExtent(
                "Module Module1
{|Insignificant:$$
|}End Module")
        End Sub

        <WpfFact>
        Public Sub TestComment()
            AssertExtent(
                " {|Significant:$$' Comment  |}")

            AssertExtent(
                " ' {|Significant:Co$$mment|}  ")

            AssertExtent(
                " ' {|Significant:($$)|} test")

            AssertExtent(
                " {|Significant:$$REM () test|}")

            AssertExtent(
                " rem {|Significant:($$)|} test")
        End Sub

        <WpfFact>
        Public Sub TestKeyword()
            AssertExtent(
                "Public {|Significant:$$Module|} Module1")

            AssertExtent(
                "Public {|Significant:M$$odule|} Module1")

            AssertExtent(
                "Public {|Significant:Mo$$dule|} Module1")

            AssertExtent(
                "Public {|Significant:Mod$$ule|} Module1")

            AssertExtent(
                "Public {|Significant:Modu$$le|} Module1")

            AssertExtent(
                "Public {|Significant:Modul$$e|} Module1")
        End Sub

        <WpfFact>
        Public Sub TestIdentifier()
            AssertExtent(
                "Public Class {|Significant:$$SomeClass|} : Inherits Object")

            AssertExtent(
                "Public Class {|Significant:S$$omeClass|} : Inherits Object")

            AssertExtent(
                "Public Class {|Significant:So$$meClass|} : Inherits Object")

            AssertExtent(
                "Public Class {|Significant:Som$$eClass|} : Inherits Object")

            AssertExtent(
                "Public Class {|Significant:Some$$Class|} : Inherits Object")

            AssertExtent(
                "Public Class {|Significant:SomeC$$lass|} : Inherits Object")

            AssertExtent(
                "Public Class {|Significant:SomeCl$$ass|} : Inherits Object")

            AssertExtent(
                "Public Class {|Significant:SomeCla$$ss|} : Inherits Object")

            AssertExtent(
                "Public Class {|Significant:SomeClas$$s|} : Inherits Object")
        End Sub

        <WpfFact>
        Public Sub TestEscapedIdentifier()
            AssertExtent(
                "Friend Enum {|Significant:$$[Module]|} As Long")

            AssertExtent(
                "Friend Enum {|Significant:[$$Module]|} As Long")

            AssertExtent(
                "Friend Enum {|Significant:[M$$odule]|} As Long")

            AssertExtent(
                "Friend Enum {|Significant:[Mo$$dule]|} As Long")

            AssertExtent(
                "Friend Enum {|Significant:[Mod$$ule]|} As Long")

            AssertExtent(
                "Friend Enum {|Significant:[Modu$$le]|} As Long")

            AssertExtent(
                "Friend Enum {|Significant:[Modul$$e]|} As Long")

            AssertExtent(
                "Friend Enum {|Significant:[Module$$]|} As Long")
        End Sub

        <WpfFact>
        Public Sub TestNumber()
            AssertExtent(
                "Class Test : Dim number As Double = -{|Significant:$$1.234678E-120|} : End Class")

            AssertExtent(
                "Class Test : Dim number As Double = -{|Significant:1$$.234678E-120|} : End Class")

            AssertExtent(
                "Class Test : Dim number As Double = -{|Significant:1.$$234678E-120|} : End Class")

            AssertExtent(
                "Class Test : Dim number As Double = -{|Significant:1.2$$34678E-120|} : End Class")

            AssertExtent(
                "Class Test : Dim number As Double = -{|Significant:1.23$$4678E-120|} : End Class")

            AssertExtent(
                "Class Test : Dim number As Double = -{|Significant:1.234$$678E-120|} : End Class")

            AssertExtent(
                "Class Test : Dim number As Double = -{|Significant:1.2346$$78E-120|} : End Class")

            AssertExtent(
                "Class Test : Dim number As Double = -{|Significant:1.23467$$8E-120|} : End Class")

            AssertExtent(
                "Class Test : Dim number As Double = -{|Significant:1.234678$$E-120|} : End Class")

            AssertExtent(
                "Class Test : Dim number As Double = -{|Significant:1.234678E$$-120|} : End Class")

            AssertExtent(
                "Class Test : Dim number As Double = -{|Significant:1.234678E-$$120|} : End Class")

            AssertExtent(
                "Class Test : Dim number As Double = -{|Significant:1.234678E-1$$20|} : End Class")

            AssertExtent(
                "Class Test : Dim number As Double = -{|Significant:1.234678E-12$$0|} : End Class")
        End Sub

        <WpfFact>
        Public Sub TestString()
            AssertExtent(
                "Class Test : Dim str As String = {|Significant:$$""|} () test  "" : End Class")

            AssertExtent(
                "Class Test : Dim str As String = ""{|Insignificant:$$ |}() test  "" : End Class")

            AssertExtent(
                "Class Test : Dim str As String = "" {|Significant:$$()|} test  "" : End Class")

            AssertExtent(
                "Class Test : Dim str As String = "" () test{|Insignificant: $$ |}"" : End Class")

            AssertExtent(
                "Class Test : Dim str As String = "" () test  {|Significant:$$""|} : End Class")
        End Sub

        <WpfFact>
        Public Sub TestInterpolatedString()
            AssertExtent(
                "Class Test : Dim str As String = {|Significant:$$$""|} () test  "" : End Class")

            AssertExtent(
                "Class Test : Dim str As String = $""{|Insignificant:$$ |}() test  "" : End Class")

            AssertExtent(
                "Class Test : Dim str As String = $"" {|Significant:$$()|} test  "" : End Class")

            AssertExtent(
                "Class Test : Dim str As String = $"" () test{|Insignificant: $$ |}"" : End Class")

            AssertExtent(
                "Class Test : Dim str As String = "" () test  ""{|Insignificant:$$ |}: End Class")
        End Sub
    End Class
End Namespace
