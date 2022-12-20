' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.
Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class RequiredMembersTests
        Inherits BasicTestBase

        <Fact>
        Public Sub CannotInheritFromTypesWithRequiredMembers()

        End Sub

        <Theory>
        Public Sub EnforcedRequiredMembers_NoInheritance_NoneSet(<CombinatorialValues("As New C()", "= new C()")> constructor As String)

        End Sub

        <Theory>
        Public Sub EnforcedRequiredMembers_NoInheritance_PartialSet(<CombinatorialValues("As New C()", "= new C()")> constructor As String)

        End Sub

        <Theory>
        Public Sub EnforcedRequiredMembers_NoInheritance_AllSet(<CombinatorialValues("As New C()", "= new C()")> constructor As String)

        End Sub

        <Theory>
        Public Sub EnforcedRequiredMembers_NoInheritance_HasSetsRequiredMembers(<CombinatorialValues("As New C()", "= new C()")> constructor As String)

        End Sub

        <Theory>
        Public Sub EnforcedRequiredMembers_NoInheritance_Unsettable(<CombinatorialValues("As New C()", "= new C()")> constructor As String)

        End Sub

        <Theory>
        Public Sub EnforcedRequiredMembers_Inheritance_NoneSet(<CombinatorialValues("As New Derived()", "= new Derived()")> constructor As String)

        End Sub

        <Theory>
        Public Sub EnforcedRequiredMembers_Inheritance_PartialSet(<CombinatorialValues("As New Derived()", "= new Derived()")> constructor As String)

        End Sub

        <Theory>
        Public Sub EnforcedRequiredMembers_Inheritance_AllSet(<CombinatorialValues("As New Derived()", "= new Derived()")> constructor As String)

        End Sub

        <Theory>
        Public Sub EnforcedRequiredMembers_Inheritance_NoneSet_HasSetsRequiredMembers(<CombinatorialValues("As New Derived()", "= new Derived()")> constructor As String)

        End Sub

        <Theory>
        Public Sub EnforcedRequiredMembers_Inheritance_NoMembersOnDerivedType(<CombinatorialValues("As New Derived()", "= new Derived()")> constructor As String)

        End Sub

        <Theory>
        Public Sub EnforcedRequiredMembers_ThroughRetargeting_NoneSet(<CombinatorialValues("As New Derived()", "= new Derived()")> constructor As String)

        End Sub

        <Theory>
        Public Sub EnforcedRequiredMembers_ThroughRetargeting_AllSet(<CombinatorialValues("As New Derived()", "= new Derived()")> constructor As String)

        End Sub

        <Theory>
        Public Sub EnforcedRequiredMembers_ThroughRetargeting_HasSetsRequiredMembers(<CombinatorialValues("As New Derived()", "= new Derived()")> constructor As String)

        End Sub

        <Theory>
        Public Sub EnforcedRequiredMembers_Override_NoneSet(<CombinatorialValues("As New Derived()", "= new Derived()")> constructor As String)

        End Sub

        <Theory>
        Public Sub EnforcedRequiredMembers_Override_AllSet(<CombinatorialValues("As New Derived()", "= new Derived()")> constructor As String)

        End Sub

        <Theory>
        Public Sub EnforcedRequiredMembers_Override_HasSetsRequiredMembers(<CombinatorialValues("As New Derived()", "= new Derived()")> constructor As String)

        End Sub

        <Fact>
        Public Sub EnforcedRequiredMembers_ShadowedFromMetadata_01()

        End Sub

        <Fact>
        Public Sub CoClassWithRequiredMembers_NoneSet()

        End Sub

        <Fact>
        Public Sub CoClassWithRequiredMembers_AllSet()

        End Sub

        <Fact>
        Public Sub RequiredMemberInAttribute_NoneSet()

        End Sub

        <Fact>
        Public Sub RequiredMemberInAttribute_AllSet()

        End Sub

        <Theory>
        <InlineData("Structure")>
        <InlineData("Class")>
        Public Sub ForbidRequiredAsNew_NoInheritance(typeKind As String)

        End Sub

        <Fact>
        Public Sub ForbidRequiredAsNew_Inheritance()

        End Sub

        <Theory>
        <InlineData("Structure")>
        <InlineData("Class")>
        Public Sub AllowRequiredAsNew_SetsRequiredMembersOnConstructor(typeKind As String)

        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub PublicAPITests(isRequired As Boolean)

        End Sub
    End Class
End Namespace
