' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UseAutoProperty

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.UseAutoProperty
    Public Class UseAutoPropertyTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
            Return Tuple.Create(Of DiagnosticAnalyzer, CodeFixProvider)(New UseAutoPropertyAnalyzer(), New UseAutoPropertyCodeFixProvider())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Sub TestSingleGetter1()
            Test(
NewLines("class Class1 \n [|dim i as integer|] \n readonly property P as integer \n get \n return i \n end get \n end property \n end class"),
NewLines("class Class1 \n readonly property P as integer \n end class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Sub TestSingleGetter2()
            Test(
NewLines("class Class1 \n dim i as Integer \n [|readonly property P as integer \n get \n return i \n end get \n end property|] \n end class"),
NewLines("class Class1 \n readonly property P as integer \n end class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Sub TestSingleSetter()
            TestMissing(
NewLines("class Class1 \n [|dim i as integer|] \n property P as Integer \n set \n i = value \n end set \end property \end class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Sub TestGetterAndSetter()
            Test(
NewLines("class Class1 \n [|dim i as integer|] \n property P as Integer \n get \n return i \n end get \n set \n i = value \n end set \n end property \n end class"),
NewLines("class Class1 \n property P as Integer \n end class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Sub TestInitializer()
            Test(
NewLines("class Class1 \n dim i as Integer = 1 \n [|readonly property P as integer \n get \n return i \n end get \n end property|] \n end class"),
NewLines("class Class1 \n readonly property P as integer = 1 \n end class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Sub TestInitializer_VB9()
            TestMissing(
NewLines("class Class1 \n dim [|i|] as Integer = 1 \n readonly property P as integer \n get \n return i \n end get \n end property \n end class"),
            VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.VisualBasic9))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Sub TestReadOnlyField()
            Test(
NewLines("class Class1 \n [|readonly dim i as integer|] \n property P as integer \n get \n return i \n end get \n end property \n end class"),
NewLines("class Class1 \n ReadOnly property P as integer \n end class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Sub TestReadOnlyField_VB12()
            TestMissing(
NewLines("class Class1 \n [|readonly dim i as integer|] \n property P as integer \n get \n return i \n end get \n end property \n end class"),
            VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.VisualBasic12))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Sub TestDifferentValueName()
            Test(
NewLines("class Class1 \n [|dim i as integer|] \n property P as Integer \n get \n return i \n end get \n set(v as integer) \n i = v \n end set \n end property \n end class"),
NewLines("class Class1 \n property P as Integer \n end class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Sub TestSingleGetterWithMe()
            Test(
NewLines("class Class1 \n [|dim i as integer|] \n property P as Integer \n get \n return me.i \n end get \n end property \n end class"),
NewLines("class Class1 \n ReadOnly property P as Integer \n end class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Sub TestSingleSetterWithMe()
            TestMissing(
NewLines("class Class1 \n [|dim i as integer|] \n property P as Integer \n set \n me.i = value \n end set \n end property \n end class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Sub TestGetterAndSetterWithMe()
            Test(
NewLines("class Class1 \n [|dim i as integer|] \n property P as Integer \n get \n return me.i \n end get \n set \n me.i = value \n end property \n end class"),
NewLines("class Class1 \n property P as Integer \n end class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Sub TestGetterWithMutipleStatements()
            TestMissing(
NewLines("class Class1 \n [|dim i as integer|] \n property P as Integer \n get \n Foo() \n return i \n end get \n end property \n end class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Sub TestSetterWithMutipleStatements()
            TestMissing(
NewLines("class Class1 \n [|dim i as integer|] \n property P as Integer \n set \n Foo() \n i = value \n end set \n end property \n end class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Sub TestSetterWithMutipleStatementsAndGetterWithSingleStatement()
            TestMissing(
NewLines("class Class1 \n [|dim i as integer|] \n property P as Integer \n get \n Return i \n end get \n \n set \n Foo() \n i = value \n end set \n end property \n end class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Sub TestGetterAndSetterUseDifferentFields()
            TestMissing(
NewLines("class Class1 \n [|dim i as integer|] \n dim j as Integer \n property P as Integer \n get \n return i \n end get \n set \n j = value \n end set \n end property \n end class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Sub TestFieldAndPropertyHaveDifferentStaticInstance()
            TestMissing(
NewLines("class Class1 \n [|shared i a integer|] \n property P as Integer \n get \n return i \n end property \n end class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Sub TestFieldUseInRefArgument1()
            TestMissing(
NewLines("class Class1 \n [|dim i as integer|] \n property P as Integer \n get \n return i \n end property \n sub M(byref x as integer) \n M(i) \n end sub \n end class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Sub TestFieldUseInRefArgument2()
            TestMissing(
NewLines("class Class1 \n [|dim i as integer|] \n property P as Integer \n get \n return i \n end property \n sub M(byref x as integer) \n M(me.i) \end sub \n end class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Sub TestNotWithVirtualProperty()
            TestMissing(
NewLines("class Class1 \n [|dim i as integer|] \n public virtual property P as Integer \n get \n return i \n end property \n end class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Sub TestNotWithConstField()
            TestMissing(
NewLines("class Class1 \n [|const int i|] \n property P as Integer \n get \n return i \n end property \n end class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Sub TestFieldWithMultipleDeclarators1()
            Test(
NewLines("class Class1 \n dim [|i|] as integer, j, k \n property P as Integer \n get \n return i \n end property \n end class"),
NewLines("class Class1 \n dim j, k \n ReadOnly property P as Integer \n end class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Sub TestFieldWithMultipleDeclarators2()
            Test(
NewLines("class Class1 \n dim i, [|j|] as integer, k \n property P as Integer \n get \n return j \n end get \n end property \n end class"),
NewLines("class Class1 \n dim i as integer, k \n ReadOnly property P as Integer \n end class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Sub TestFieldWithMultipleDeclarators3()
            Test(
NewLines("class Class1 \n dim i, j, [|k|] as integer \n property P as Integer \n get \n return k \n end get \n end property \n end class"),
NewLines("class Class1 \n dim i, j as integer \n ReadOnly property P as Integer \n end class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Sub TestFieldWithMultipleDeclarators4()
            Test(
NewLines("class Class1 \n dim i as integer, [|k|] as integer \n property P as Integer \n get \n return k \n end get \n end property \n end class"),
NewLines("class Class1 \n dim i as integer \n ReadOnly property P as Integer \n end class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Sub TestFieldAndPropertyInDifferentParts()
            Test(
NewLines("partial class Class1 \n [|dim i as integer|] \n end class \n partial class Class1 \n property P as Integer \n get \n return i \n end property \n end class"),
NewLines("partial class Class1 \n end class \n partial class Class1 \n ReadOnly property P as Integer \n end class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Sub TestNotWithFieldWithAttribute()
            TestMissing(
NewLines("class Class1 \n [|<A>dim i as integer|] \n property P as Integer \n get \n return i \n end property \n end class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Sub TestUpdateReferences()
            Test(
NewLines("class Class1 \n [|dim i as integer|] \n property P as Integer \n get \n return i \n end get \n end property \n public sub new() \n i = 1 \n end sub \n end class"),
NewLines("class Class1 \n ReadOnly property P as Integer \n public sub new() \n P = 1 \n end sub \n end class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Sub TestUpdateReferencesConflictResolution()
            Test(
NewLines("class Class1 \n [|dim i as integer|] \n property P as Integer \n get \n return i \n end get \n public sub new(dim P as integer) \n i = 1 \n end sub \n end class"),
NewLines("class Class1 \n ReadOnly property P as Integer \n public sub new(dim P as integer) \n Me.P = 1 \n end sub \n end class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Sub TestWriteInConstructor()
            Test(
NewLines("class Class1 \n [|dim i as integer|] \n property P as Integer \n get \n return i \n end get \n end property \n public sub new() \n i = 1 \n end sub \n end class"),
NewLines("class Class1 \n ReadOnly property P as Integer \n public sub new() \n P = 1 \n end sub \n end class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Sub TestWriteInNotInConstructor1()
            TestMissing(
NewLines("class Class1 \n [|dim i as integer|] \n property P as Integer \n get \n return i \n end property \n public sub Foo() \n i = 1 \n end sub \n end class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Sub TestWriteInNotInConstructor2()
            TestMissing(
NewLines("class Class1 \n [|dim i as integer|] \n public property P as Integer \n get \n return i \n \end property \n public sub Foo() \n i = 1 \n end sub \n end class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Sub TestWriteInNotInConstructor3()
            Test(
NewLines("class Class1 \n [|dim i as integer|] \n public property P as Integer \n get \n return i \n end get \n set \n i = value \n end set \n end property \n public sub Foo() \n i = 1 \n end sub \n end class"),
NewLines("class Class1 \n public property P as Integer \n public sub Foo() P = 1 \n end sub \n end class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Sub TestAlreadyAutoProperty()
            TestMissing(NewLines("Class Class1 \n Public Property [|P|] As Integer \n End Class"))
        End Sub
    End Class
End Namespace
