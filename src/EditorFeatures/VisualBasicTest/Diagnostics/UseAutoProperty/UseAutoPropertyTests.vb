' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
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
        Public Async Function TestSingleGetter1() As Task
            Await TestAsync(
NewLines("class Class1 \n [|dim i as integer|] \n readonly property P as integer \n get \n return i \n end get \n end property \n end class"),
NewLines("class Class1 \n readonly property P as integer \n end class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestSingleGetter2() As Task
            Await TestAsync(
NewLines("class Class1 \n dim i as Integer \n [|readonly property P as integer \n get \n return i \n end get \n end property|] \n end class"),
NewLines("class Class1 \n readonly property P as integer \n end class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestSingleSetter() As Task
            Await TestMissingAsync(
NewLines("class Class1 \n [|dim i as integer|] \n property P as Integer \n set \n i = value \n end set \end property \end class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestGetterAndSetter() As Task
            Await TestAsync(
NewLines("class Class1 \n [|dim i as integer|] \n property P as Integer \n get \n return i \n end get \n set \n i = value \n end set \n end property \n end class"),
NewLines("class Class1 \n property P as Integer \n end class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestInitializer() As Task
            Await TestAsync(
NewLines("class Class1 \n dim i as Integer = 1 \n [|readonly property P as integer \n get \n return i \n end get \n end property|] \n end class"),
NewLines("class Class1 \n readonly property P as integer = 1 \n end class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestInitializer_VB9() As Task
            Await TestMissingAsync(
NewLines("class Class1 \n dim [|i|] as Integer = 1 \n readonly property P as integer \n get \n return i \n end get \n end property \n end class"),
            VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.VisualBasic9))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestReadOnlyField() As Task
            Await TestAsync(
NewLines("class Class1 \n [|readonly dim i as integer|] \n property P as integer \n get \n return i \n end get \n end property \n end class"),
NewLines("class Class1 \n ReadOnly property P as integer \n end class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestReadOnlyField_VB12() As Task
            Await TestMissingAsync(
NewLines("class Class1 \n [|readonly dim i as integer|] \n property P as integer \n get \n return i \n end get \n end property \n end class"),
            VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.VisualBasic12))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestDifferentValueName() As Task
            Await TestAsync(
NewLines("class Class1 \n [|dim i as integer|] \n property P as Integer \n get \n return i \n end get \n set(v as integer) \n i = v \n end set \n end property \n end class"),
NewLines("class Class1 \n property P as Integer \n end class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestSingleGetterWithMe() As Task
            Await TestAsync(
NewLines("class Class1 \n [|dim i as integer|] \n property P as Integer \n get \n return me.i \n end get \n end property \n end class"),
NewLines("class Class1 \n ReadOnly property P as Integer \n end class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestSingleSetterWithMe() As Task
            Await TestMissingAsync(
NewLines("class Class1 \n [|dim i as integer|] \n property P as Integer \n set \n me.i = value \n end set \n end property \n end class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestGetterAndSetterWithMe() As Task
            Await TestAsync(
NewLines("class Class1 \n [|dim i as integer|] \n property P as Integer \n get \n return me.i \n end get \n set \n me.i = value \n end property \n end class"),
NewLines("class Class1 \n property P as Integer \n end class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestGetterWithMutipleStatements() As Task
            Await TestMissingAsync(
NewLines("class Class1 \n [|dim i as integer|] \n property P as Integer \n get \n Foo() \n return i \n end get \n end property \n end class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestSetterWithMutipleStatements() As Task
            Await TestMissingAsync(
NewLines("class Class1 \n [|dim i as integer|] \n property P as Integer \n set \n Foo() \n i = value \n end set \n end property \n end class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestSetterWithMutipleStatementsAndGetterWithSingleStatement() As Task
            Await TestMissingAsync(
NewLines("class Class1 \n [|dim i as integer|] \n property P as Integer \n get \n Return i \n end get \n \n set \n Foo() \n i = value \n end set \n end property \n end class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestGetterAndSetterUseDifferentFields() As Task
            Await TestMissingAsync(
NewLines("class Class1 \n [|dim i as integer|] \n dim j as Integer \n property P as Integer \n get \n return i \n end get \n set \n j = value \n end set \n end property \n end class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestFieldAndPropertyHaveDifferentStaticInstance() As Task
            Await TestMissingAsync(
NewLines("class Class1 \n [|shared i a integer|] \n property P as Integer \n get \n return i \n end property \n end class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestFieldUseInRefArgument1() As Task
            Await TestMissingAsync(
NewLines("class Class1 \n [|dim i as integer|] \n property P as Integer \n get \n return i \n end property \n sub M(byref x as integer) \n M(i) \n end sub \n end class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestFieldUseInRefArgument2() As Task
            Await TestMissingAsync(
NewLines("class Class1 \n [|dim i as integer|] \n property P as Integer \n get \n return i \n end property \n sub M(byref x as integer) \n M(me.i) \end sub \n end class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestNotWithVirtualProperty() As Task
            Await TestMissingAsync(
NewLines("class Class1 \n [|dim i as integer|] \n public virtual property P as Integer \n get \n return i \n end property \n end class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestNotWithConstField() As Task
            Await TestMissingAsync(
NewLines("class Class1 \n [|const int i|] \n property P as Integer \n get \n return i \n end property \n end class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestFieldWithMultipleDeclarators1() As Task
            Await TestAsync(
NewLines("class Class1 \n dim [|i|] as integer, j, k \n property P as Integer \n get \n return i \n end property \n end class"),
NewLines("class Class1 \n dim j, k \n ReadOnly property P as Integer \n end class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestFieldWithMultipleDeclarators2() As Task
            Await TestAsync(
NewLines("class Class1 \n dim i, [|j|] as integer, k \n property P as Integer \n get \n return j \n end get \n end property \n end class"),
NewLines("class Class1 \n dim i as integer, k \n ReadOnly property P as Integer \n end class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestFieldWithMultipleDeclarators3() As Task
            Await TestAsync(
NewLines("class Class1 \n dim i, j, [|k|] as integer \n property P as Integer \n get \n return k \n end get \n end property \n end class"),
NewLines("class Class1 \n dim i, j as integer \n ReadOnly property P as Integer \n end class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestFieldWithMultipleDeclarators4() As Task
            Await TestAsync(
NewLines("class Class1 \n dim i as integer, [|k|] as integer \n property P as Integer \n get \n return k \n end get \n end property \n end class"),
NewLines("class Class1 \n dim i as integer \n ReadOnly property P as Integer \n end class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestFieldAndPropertyInDifferentParts() As Task
            Await TestAsync(
NewLines("partial class Class1 \n [|dim i as integer|] \n end class \n partial class Class1 \n property P as Integer \n get \n return i \n end property \n end class"),
NewLines("partial class Class1 \n end class \n partial class Class1 \n ReadOnly property P as Integer \n end class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestNotWithFieldWithAttribute() As Task
            Await TestMissingAsync(
NewLines("class Class1 \n [|<A>dim i as integer|] \n property P as Integer \n get \n return i \n end property \n end class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestUpdateReferences() As Task
            Await TestAsync(
NewLines("class Class1 \n [|dim i as integer|] \n property P as Integer \n get \n return i \n end get \n end property \n public sub new() \n i = 1 \n end sub \n end class"),
NewLines("class Class1 \n ReadOnly property P as Integer \n public sub new() \n P = 1 \n end sub \n end class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestUpdateReferencesConflictResolution() As Task
            Await TestAsync(
NewLines("class Class1 \n [|dim i as integer|] \n property P as Integer \n get \n return i \n end get \n public sub new(dim P as integer) \n i = 1 \n end sub \n end class"),
NewLines("class Class1 \n ReadOnly property P as Integer \n public sub new(dim P as integer) \n Me.P = 1 \n end sub \n end class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestWriteInConstructor() As Task
            Await TestAsync(
NewLines("class Class1 \n [|dim i as integer|] \n property P as Integer \n get \n return i \n end get \n end property \n public sub new() \n i = 1 \n end sub \n end class"),
NewLines("class Class1 \n ReadOnly property P as Integer \n public sub new() \n P = 1 \n end sub \n end class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestWriteInNotInConstructor1() As Task
            Await TestMissingAsync(
NewLines("class Class1 \n [|dim i as integer|] \n property P as Integer \n get \n return i \n end property \n public sub Foo() \n i = 1 \n end sub \n end class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestWriteInNotInConstructor2() As Task
            Await TestMissingAsync(
NewLines("class Class1 \n [|dim i as integer|] \n public property P as Integer \n get \n return i \n \end property \n public sub Foo() \n i = 1 \n end sub \n end class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestWriteInNotInConstructor3() As Task
            Await TestAsync(
NewLines("class Class1 \n [|dim i as integer|] \n public property P as Integer \n get \n return i \n end get \n set \n i = value \n end set \n end property \n public sub Foo() \n i = 1 \n end sub \n end class"),
NewLines("class Class1 \n public property P as Integer \n public sub Foo() P = 1 \n end sub \n end class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestAlreadyAutoProperty() As Task
            Await TestMissingAsync(NewLines("Class Class1 \n Public Property [|P|] As Integer \n End Class"))
        End Function
    End Class
End Namespace