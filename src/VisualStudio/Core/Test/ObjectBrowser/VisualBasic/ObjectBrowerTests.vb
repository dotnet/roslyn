' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Globalization
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.ComponentModelHost
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.ObjectBrowser
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ObjectBrowser.VisualBasic
    <Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
    Public Class ObjectBrowserTests
        Inherits AbstractObjectBrowserTests

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

        Friend Overrides Function CreateLibraryManager(serviceProvider As IServiceProvider, componentModel As IComponentModel, workspace As VisualStudioWorkspace) As AbstractObjectBrowserLibraryManager
            Return New ObjectBrowserLibraryManager(serviceProvider, componentModel, workspace)
        End Function

        <WpfFact>
        Public Sub TestSimpleContent_NamespaceClassAndMethod()
            Dim code =
<Code>
Namespace N
    Class C
        Sub M()
        End Sub
    End Class
End Namespace
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()

                Dim list = library.GetProjectList()
                list.VerifyNames("VisualBasicAssembly1")

                list = list.GetNamespaceList(0)
                list.VerifyNames("N")

                list = list.GetTypeList(0)
                list.VerifyNames("C")

                list = list.GetMemberList(0)
                list.VerifyNames(AddressOf IsImmediateMember, "M()")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestSimpleContent_NoNamespaceWithoutType()
            Dim code =
<Code>
Namespace N
End Namespace
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetNamespaceList(0)

                list.VerifyEmpty()
            End Using
        End Sub

        <WpfFact>
        Public Sub TestSimpleContent_InlcudePrivateNestedTypeMembersInSourceCode()
            Dim code =
<Code>
Namespace N
	Class C
		Private Enum PrivateEnumTest
		End Enum
		Private Class PrivateClassTest
		End Class
		Private Structure PrivateStructTest
		End Structure
	End Class
End Namespace
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetNamespaceList(0).GetTypeList(0)
                list.VerifyNames("C", "C.PrivateStructTest", "C.PrivateClassTest", "C.PrivateEnumTest")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestSimpleContent_InlcudePrivateDoubleNestedTypeMembersInSourceCode()
            Dim code =
<Code>
Namespace N
	Friend Class C
		Private Class NestedClass
			Private Class NestedNestedClass
			End Class
		End Class
	End Class
End Namespace
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetNamespaceList(0).GetTypeList(0)
                list.VerifyNames("C", "C.NestedClass", "C.NestedClass.NestedNestedClass")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestSimpleContent_InlcudeNoPrivateNestedTypeOfMetaData()
            Dim metaDatacode =
<Code>
Namespace N
	Public Class C
		Public Enum PublicEnumTest
            V1
		End Enum
		Private Class PrivateClassTest
		End Class
		Private Structure PrivateStructTest
		End Structure
	End Class
End Namespace
</Code>
            Dim code = <Code></Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code, metaDatacode, False))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList().GetReferenceList(0).GetNamespaceList(0).GetTypeList(0)
                list.VerifyNames("C", "C.PublicEnumTest")
            End Using
        End Sub

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/932387")>
        <WpfFact>
        Public Sub TestContent_InheritedMembers1()
            Dim code =
<Code>
Class A
    Protected Overridable Sub Goo()

    End Sub
End Class

Class B
    Inherits A

    Protected Overrides Sub Goo()
        MyBase.Goo()
    End Sub

    Overridable Sub Bar()

    End Sub
End Class

Class C
    Inherits B

    Protected Overrides Sub Goo()
        MyBase.Goo()
    End Sub

    Public Overrides Sub Bar()
        MyBase.Bar()
    End Sub
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyNames(
                    "Goo()",
                    "ToString() As String",
                    "Equals(Object) As Boolean",
                    "Equals(Object, Object) As Boolean",
                    "ReferenceEquals(Object, Object) As Boolean",
                    "GetHashCode() As Integer",
                    "GetType() As System.Type",
                    "Finalize()",
                    "MemberwiseClone() As Object")
            End Using
        End Sub

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/932387")>
        <WpfFact>
        Public Sub TestContent_InheritedMembers2()
            Dim code =
<Code>
Class A
    Protected Overridable Sub Goo()

    End Sub
End Class

Class B
    Inherits A

    Protected Overrides Sub Goo()
        MyBase.Goo()
    End Sub

    Overridable Sub Bar()

    End Sub
End Class

Class C
    Inherits B

    Protected Overrides Sub Goo()
        MyBase.Goo()
    End Sub

    Public Overrides Sub Bar()
        MyBase.Bar()
    End Sub
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(1)

                list.VerifyNames(
                    "Goo()",
                    "Bar()",
                    "ToString() As String",
                    "Equals(Object) As Boolean",
                    "Equals(Object, Object) As Boolean",
                    "ReferenceEquals(Object, Object) As Boolean",
                    "GetHashCode() As Integer",
                    "GetType() As System.Type",
                    "Finalize()",
                    "MemberwiseClone() As Object")
            End Using
        End Sub

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/932387")>
        <WpfFact>
        Public Sub TestContent_InheritedMembers3()
            Dim code =
<Code>
Class A
    Protected Overridable Sub Goo()

    End Sub
End Class

Class B
    Inherits A

    Protected Overrides Sub Goo()
        MyBase.Goo()
    End Sub

    Overridable Sub Bar()

    End Sub
End Class

Class C
    Inherits B

    Protected Overrides Sub Goo()
        MyBase.Goo()
    End Sub

    Public Overrides Sub Bar()
        MyBase.Bar()
    End Sub
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(2)

                list.VerifyNames(
                    "Goo()",
                    "Bar()",
                    "ToString() As String",
                    "Equals(Object) As Boolean",
                    "Equals(Object, Object) As Boolean",
                    "ReferenceEquals(Object, Object) As Boolean",
                    "GetHashCode() As Integer",
                    "GetType() As System.Type",
                    "Finalize()",
                    "MemberwiseClone() As Object")
            End Using
        End Sub

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/932387")>
        <WpfFact>
        Public Sub TestContent_HelpKeyword_Ctor()
            Dim code =
<Code>
Namespace N
    Class C
        Sub New()
        End Sub
    End Class
End Namespace
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetNamespaceList(0)
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyHelpKeywords("N.C.New")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Project()
            Dim code =
<Code>
Namespace N
End Namespace
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()

                list.VerifyDescriptions($"{ServicesVSResources.Project}VisualBasicAssembly1")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Namespace()
            Dim code =
<Code>
Namespace N
    Class C
    End Class
End Namespace
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetNamespaceList(0)

                list.VerifyDescriptions(
"Namespace N" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "VisualBasicAssembly1")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Delegate1()
            Dim code =
<Code>
Delegate Function D(x As Integer) As Boolean
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)

                list.VerifyDescriptions(
"Friend Delegate Function D(x As Integer) As Boolean" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "VisualBasicAssembly1")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Delegate2()
            Dim code =
<Code>
Delegate Sub D(y As String)
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)

                list.VerifyDescriptions(
"Friend Delegate Sub D(y As String)" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "VisualBasicAssembly1")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Delegate3()
            Dim code =
<Code>
Delegate Function F(Of T As {Class, New}, U As V, V As List(Of T))(x As T, y As U) As V
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)

                list.VerifyDescriptions(
"Friend Delegate Function F(Of T As {Class, New}, U As V, V As System.Collections.Generic.List(Of T))(x As T, y As U) As V" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "VisualBasicAssembly1")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Class1()
            Dim code =
<Code>
Class C
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)

                list.VerifyDescriptions(
"Friend Class C" & vbCrLf &
"        Inherits Object" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "VisualBasicAssembly1")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Class2()
            Dim code =
<Code>
MustInherit Class C
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)

                list.VerifyDescriptions(
"Friend MustInherit Class C" & vbCrLf &
"        Inherits Object" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "VisualBasicAssembly1")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Class3()
            Dim code =
<Code>
NotInheritable Class C
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)

                list.VerifyDescriptions(
"Friend NotInheritable Class C" & vbCrLf &
"        Inherits Object" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "VisualBasicAssembly1")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Class4()
            Dim code =
<Code>
Public Class C(Of T As Class)
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)

                list.VerifyDescriptions(
"Public Class C(Of T As Class)" & vbCrLf &
"        Inherits Object" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "VisualBasicAssembly1")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Class5()
            Dim code =
<Code>
Class C(Of T As Class)
    Inherits B
End Class

Class B
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)

                list.VerifyDescriptions(
"Friend Class C(Of T As Class)" & vbCrLf &
"        Inherits B" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "VisualBasicAssembly1")}",
"Friend Class B" & vbCrLf &
"        Inherits Object" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "VisualBasicAssembly1")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Class6()
            Dim code =
<Code>
MustInherit Class B
End Class

NotInheritable Class C
    Inherits B
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)

                list.VerifyDescriptions(
"Friend MustInherit Class B" & vbCrLf &
"        Inherits Object" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "VisualBasicAssembly1")}",
"Friend NotInheritable Class C" & vbCrLf &
"        Inherits B" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "VisualBasicAssembly1")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Structure1()
            Dim code =
<Code>
Structure S
End Structure
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)

                list.VerifyDescriptions(
"Friend Structure S" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "VisualBasicAssembly1")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Structure2()
            Dim code =
<Code>
Public Structure S
End Structure
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)

                list.VerifyDescriptions(
"Public Structure S" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "VisualBasicAssembly1")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Module1()
            Dim code =
<Code>
Module M
End Module
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)

                list.VerifyDescriptions(
"Friend Module M" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "VisualBasicAssembly1")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Module2()
            Dim code =
<Code>
Public Module M
End Module
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)

                list.VerifyDescriptions(
"Public Module M" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "VisualBasicAssembly1")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Enum1()
            Dim code =
<Code>
Enum E
    A
    B
    C
End Enum
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)

                list.VerifyDescriptions(
"Friend Enum E" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "VisualBasicAssembly1")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Enum2()
            Dim code =
<Code>
Enum E As Byte
    A
    B
    C
End Enum
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)

                list.VerifyDescriptions(
"Friend Enum E As Byte" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "VisualBasicAssembly1")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Interfaces()
            Dim code =
<Code>
Interface I1
End Interface

Interface I2
    Inherits I1
End Interface

Interface I3
    Inherits I1
    Inherits I2
End Interface
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)

                list.VerifyDescriptions(
"Friend Interface I1" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "VisualBasicAssembly1")}",
"Friend Interface I2" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "VisualBasicAssembly1")}",
"Friend Interface I3" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "VisualBasicAssembly1")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Sub1()
            Dim code =
<Code>
Namespace N
    Class C
        Sub M()
        End Sub
    End Class
End Namespace
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetNamespaceList(0)
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public Sub M()" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "N.C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Sub2()
            Dim code =
<Code>
Namespace N
    Class C
        Sub M(x As Integer)
        End Sub
    End Class
End Namespace
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetNamespaceList(0)
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public Sub M(x As Integer)" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "N.C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Sub3()
            Dim code =
<Code>
Namespace N
    Class C
        Sub M(ByRef x As Integer)
        End Sub
    End Class
End Namespace
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetNamespaceList(0)
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public Sub M(ByRef x As Integer)" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "N.C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Sub4()
            Dim code =
<Code>
Namespace N
    Class C
        Sub M(Optional x As Integer = 42)
        End Sub
    End Class
End Namespace
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetNamespaceList(0)
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public Sub M(Optional x As Integer = 42)" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "N.C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Sub5()
            Dim code =
<Code>
Imports System
Namespace N
    Class C
        Sub M(Optional x As Double = Math.PI)
        End Sub
    End Class
End Namespace
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code)),
                    testCulture As New CultureContext(New CultureInfo("en-US", useUserOverride:=False))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetNamespaceList(0)
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public Sub M(Optional x As Double = 3.14159265358979)" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "N.C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Sub6()
            Dim code =
<Code>
Namespace N
    Class C
        Sub M(Optional x As Double? = Nothing)
        End Sub
    End Class
End Namespace
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetNamespaceList(0)
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public Sub M(Optional x As Double? = Nothing)" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "N.C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Sub7()
            Dim code =
<Code>
Namespace N
    Class C
        Sub M(Optional x As Double? = 42)
        End Sub
    End Class
End Namespace
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetNamespaceList(0)
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public Sub M(Optional x As Double? = 42)" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "N.C")}")
            End Using
        End Sub

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/939739")>
        <WpfFact>
        Public Sub TestDescription_SubInInterface()
            Dim code =
<Code>
Namespace N
    interface I
        Sub M()
    End Interface
End Namespace
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetNamespaceList(0)
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Sub M()" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "N.I")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Function1()
            Dim code =
<Code>
Namespace N
    Class C
        Function M() As Integer
        End Function
    End Class
End Namespace
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetNamespaceList(0)
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public Function M() As Integer" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "N.C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Function2()
            Dim code =
<Code>
Namespace N
    Class C
        Function M%()
        End Function
    End Class
End Namespace
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetNamespaceList(0)
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public Function M() As Integer" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "N.C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Function3()
            Dim code =
<Code>
Namespace N
    MustInherit Class C
        MustOverride Function M() As Integer
    End Class
End Namespace
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetNamespaceList(0)
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public MustOverride Function M() As Integer" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "N.C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Function4()
            Dim code =
<Code>
Namespace N
    MustInherit Class C
        Protected Overridable Function M() As Integer
    End Class
End Namespace
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetNamespaceList(0)
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Protected Overridable Function M() As Integer" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "N.C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Function5()
            Dim code =
<Code>
Namespace N
    MustInherit Class C
        NotOverridable Function M() As Integer
    End Class
End Namespace
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetNamespaceList(0)
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public NotOverridable Function M() As Integer" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "N.C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_IteratorFunction()
            Dim code =
<Code>
Imports System.Collections.Generic
Namespace N
    Class C
        Iterator Function M() As IEnumerable(Of Integer)
        End Function
    End Class
End Namespace
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetNamespaceList(0)
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public Function M() As System.Collections.Generic.IEnumerable(Of Integer)" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "N.C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Constructor()
            Dim code =
<Code>
Imports System.Collections.Generic
Namespace N
    Class C
        Sub New()
        End Sub
    End Class
End Namespace
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetNamespaceList(0)
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public Sub New()" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "N.C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_SharedConstructor()
            Dim code =
<Code>
Imports System.Collections.Generic
Namespace N
    Class C
        Shared Sub New()
        End Sub
    End Class
End Namespace
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetNamespaceList(0)
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Private Shared Sub New()" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "N.C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Property1()
            Dim code =
<Code>
Class C
    Property P As Integer
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public Property P As Integer" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Property2()
            Dim code =
<Code>
Class C
    Property P() As Integer
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public Property P As Integer" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Property3()
            Dim code =
<Code>
Class C
    Property P%
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public Property P As Integer" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Property4()
            Dim code =
<Code>
Class C
    Property P As Integer = 42
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public Property P As Integer" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Property5()
            Dim code =
<Code>
Class C
    ReadOnly Property P As Integer
        Get
            Return 42
        End Get
    End Property
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public ReadOnly Property P As Integer" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Property6()
            Dim code =
<Code>
Class C
    ReadOnly Property P As Integer
        Protected Get
            Return 42
        End Get
    End Property
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public ReadOnly Property P As Integer" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Property7()
            Dim code =
<Code>
Class C
    WriteOnly Property P As Integer
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public WriteOnly Property P As Integer" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Property8()
            Dim code =
<Code>
Class C
    Property P As Integer
        Get
        End Get
        Protected Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public Property P As Integer" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Property9()
            Dim code =
<Code>
Class C
    Property P(index As Integer) As Integer
        Get
        End Get
        Protected Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public Property P(index As Integer) As Integer" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_IteratorProperty()
            Dim code =
<Code>
Imports System.Collections.Generic
Class C
    ReadOnly Iterator Property P As IEnumerable(Of Integer)
        Get
        End Get
    End Property
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public ReadOnly Property P As System.Collections.Generic.IEnumerable(Of Integer)" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Const1()
            Dim code =
<Code>
Class C
    Const F As Integer = 42
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Private Const F As Integer = 42" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Const2()
            Dim code =
<Code>
Class C
    Const F = 42
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Private Const F As Integer = 42" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Field1()
            Dim code =
<Code>
Class C
    Dim x As Integer
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Private x As Integer" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Field2()
            Dim code =
<Code>
Class C
    Private x As Integer
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Private x As Integer" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Field3()
            Dim code =
<Code>
Class C
    ReadOnly x As Integer
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Private ReadOnly x As Integer" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Field4()
            Dim code =
<Code>
Class C
    Shared ReadOnly x As Integer
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Private Shared ReadOnly x As Integer" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_EnumMembers()
            Dim code =
<Code>
Enum E
    A
    B
    C
End Enum
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public Const A As E = 0" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "E")}",
"Public Const B As E = 1" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "E")}",
"Public Const C As E = 2" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "E")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Events()
            Dim code =
<Code>
Imports System
Class C
    Event E1()
    Event E2(i As Integer)
    Event E3 As EventHandler
    Custom Event E4 As EventHandler
        AddHandler(value As EventHandler)
        End AddHandler
        RemoveHandler(value As EventHandler)
        End RemoveHandler
        RaiseEvent(sender As Object, e As EventArgs)
        End RaiseEvent
    End Event
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public Event E1()" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}",
"Public Event E2(i As Integer)" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}",
"Public Event E3(sender As Object, e As System.EventArgs)" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}",
"Public Event E4(sender As Object, e As System.EventArgs)" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Operator_Add()
            Dim code =
<Code>
Class C
    Shared Operator +(c As C, x As Integer) As Integer
    End Operator
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public Shared Operator +(c As C, x As Integer) As Integer" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Operator_Subtract()
            Dim code =
<Code>
Class C
    Shared Operator -(c As C, x As Integer) As Integer
    End Operator
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public Shared Operator -(c As C, x As Integer) As Integer" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Operator_Multiply()
            Dim code =
<Code>
Class C
    Shared Operator *(c As C, x As Integer) As Integer
    End Operator
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public Shared Operator *(c As C, x As Integer) As Integer" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Operator_Divide()
            Dim code =
<Code>
Class C
    Shared Operator /(c As C, x As Integer) As Integer
    End Operator
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public Shared Operator /(c As C, x As Integer) As Integer" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Operator_IntegerDivide()
            Dim code =
<Code>
Class C
    Shared Operator \(c As C, x As Integer) As Integer
    End Operator
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public Shared Operator \(c As C, x As Integer) As Integer" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Operator_Modulus()
            Dim code =
<Code>
Class C
    Shared Operator Mod(c As C, x As Integer) As Integer
    End Operator
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public Shared Operator Mod(c As C, x As Integer) As Integer" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Operator_Power()
            Dim code =
<Code>
Class C
    Shared Operator ^(c As C, x As Integer) As Integer
    End Operator
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public Shared Operator ^(c As C, x As Integer) As Integer" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Operator_Concatenate()
            Dim code =
<Code>
Class C
    Shared Operator &amp;(c As C, x As Integer) As Integer
    End Operator
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public Shared Operator &(c As C, x As Integer) As Integer" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Operator_Equality()
            Dim code =
<Code>
Class C
    Shared Operator =(c As C, x As Integer) As Boolean
    End Operator
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public Shared Operator =(c As C, x As Integer) As Boolean" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Operator_Inequality()
            Dim code =
<Code>
Class C
    Shared Operator &lt;&gt;(c As C, x As Integer) As Boolean
    End Operator
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public Shared Operator <>(c As C, x As Integer) As Boolean" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Operator_LessThan()
            Dim code =
<Code>
Class C
    Shared Operator &lt;(c As C, x As Integer) As Boolean
    End Operator
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public Shared Operator <(c As C, x As Integer) As Boolean" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Operator_LessThanOrEqualTo()
            Dim code =
<Code>
Class C
    Shared Operator &lt;=(c As C, x As Integer) As Boolean
    End Operator
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public Shared Operator <=(c As C, x As Integer) As Boolean" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Operator_GreaterThan()
            Dim code =
<Code>
Class C
    Shared Operator &gt;(c As C, x As Integer) As Boolean
    End Operator
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public Shared Operator >(c As C, x As Integer) As Boolean" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Operator_GreaterThanOrEqualTo()
            Dim code =
<Code>
Class C
    Shared Operator &gt;=(c As C, x As Integer) As Boolean
    End Operator
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public Shared Operator >=(c As C, x As Integer) As Boolean" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Operator_Like()
            Dim code =
<Code>
Class C
    Shared Operator Like(c As C, i As Integer) As Boolean
    End Operator
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public Shared Operator Like(c As C, i As Integer) As Boolean" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Operator_Not()
            Dim code =
<Code>
Class C
    Shared Operator Not(c As C) As Boolean
    End Operator
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public Shared Operator Not(c As C) As Boolean" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Operator_And()
            Dim code =
<Code>
Class C
    Shared Operator And(c As C, i As Integer) As Boolean
    End Operator
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public Shared Operator And(c As C, i As Integer) As Boolean" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Operator_Or()
            Dim code =
<Code>
Class C
    Shared Operator Or(c As C, i As Integer) As Boolean
    End Operator
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public Shared Operator Or(c As C, i As Integer) As Boolean" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Operator_Xor()
            Dim code =
<Code>
Class C
    Shared Operator Xor(c As C, i As Integer) As Boolean
    End Operator
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public Shared Operator Xor(c As C, i As Integer) As Boolean" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Operator_ShiftLeft()
            Dim code =
<Code>
Class C
    Shared Operator &lt;&lt;(c As C, x As Integer) As Integer
    End Operator
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public Shared Operator <<(c As C, x As Integer) As Integer" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Operator_Right()
            Dim code =
<Code>
Class C
    Shared Operator &gt;&gt;(c As C, x As Integer) As Integer
    End Operator
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public Shared Operator >>(c As C, x As Integer) As Integer" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Operator_IsTrue()
            Dim code =
<Code>
Class C
    Shared Operator IsTrue(c As C) As Boolean
    End Operator
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public Shared Operator IsTrue(c As C) As Boolean" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Operator_IsFalse()
            Dim code =
<Code>
Class C
    Shared Operator IsFalse(c As C) As Boolean
    End Operator
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public Shared Operator IsFalse(c As C) As Boolean" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Operator_CType1()
            Dim code =
<Code>
Class C
    Shared Narrowing Operator CType(c As C) As Boolean
    End Operator
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public Shared Narrowing Operator CType(c As C) As Boolean" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Operator_CType2()
            Dim code =
<Code>
Class C
    Shared Widening Operator CType(c As C) As Boolean
    End Operator
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public Shared Widening Operator CType(c As C) As Boolean" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_DeclareFunction1()
            ' Note: DllImport functions that are not Declare should appear as normal functions
            Dim code =
<Code>
Imports System.Runtime.InteropServices
Class C
    &lt;DllImportAttribute("kernel32.dll", EntryPoint:="MoveFileW",
     SetLastError:=True, CharSet:=CharSet.Unicode,
     ExactSpelling:=True,
     CallingConvention:=CallingConvention.StdCall)&gt;
    Public Shared Function moveFile(ByVal src As String, ByVal dst As String) As Boolean
        ' This function copies a file from the path src to the path dst.
        ' Leave this function empty. The DLLImport attribute forces calls
        ' to moveFile to be forwarded to MoveFileW in KERNEL32.DLL.
    End Function
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public Shared Function moveFile(src As String, dst As String) As Boolean" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_DeclareFunction2()
            Dim code =
<Code>
Class C
    Declare Function getUserName Lib "advapi32.dll" Alias "GetUserNameA"(lpBuffer As String, ByRef nSize As Integer) As Integer
End Class
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public Declare Ansi Function getUserName Lib ""advapi32.dll"" Alias ""GetUserNameA""(ByRef lpBuffer As String, ByRef nSize As Integer) As Integer" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_XmlDocComments()
            Dim code =
<Code>
    <![CDATA[
Class C
    ''' <summary>
    ''' The is my summary!
    ''' </summary>
    ''' <typeparam name="T">Hello from a type parameter</typeparam>
    ''' <param name="i">The parameter i</param>
    ''' <param name="s">The parameter t</param>
    ''' <remarks>Takes <paramref name="i"/> and <paramref name="s"/>.</remarks>
    Sub M(Of T)(i As Integer, s As String)

    End Sub
End Class
]]>
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public Sub M(Of T)(i As Integer, s As String)" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}" & vbCrLf &
"" & vbCrLf &
ServicesVSResources.Summary_colon & vbCrLf &
"The is my summary!" & vbCrLf &
"" & vbCrLf &
ServicesVSResources.Type_Parameters_colon & vbCrLf &
"T: Hello from a type parameter" & vbCrLf &
"" & vbCrLf &
ServicesVSResources.Parameters_colon1 & vbCrLf &
"i: The parameter i" & vbCrLf &
"s: The parameter t" & vbCrLf &
"" & vbCrLf &
ServicesVSResources.Remarks_colon & vbCrLf &
"Takes i and s.")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_XmlDocComments_Returns1()
            Dim code =
<Code>
    <![CDATA[
Class C
    ''' <summary>
    ''' Describes the method.
    ''' </summary>
    ''' <returns>Returns a value.</returns>
    Function M() As Integer
        Return 0
    End Function
End Class
]]>
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public Function M() As Integer" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}" & vbCrLf &
"" & vbCrLf &
ServicesVSResources.Summary_colon & vbCrLf &
"Describes the method." & vbCrLf &
"" & vbCrLf &
ServicesVSResources.Returns_colon & vbCrLf &
"Returns a value.")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_XmlDocComments_Returns2()
            Dim code =
<Code>
    <![CDATA[
Class C
    ''' <summary>
    ''' Gets a value.
    ''' </summary>
    ''' <returns>Returns a value.</returns>
    ReadOnly Property M As Integer
End Class
]]>
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public ReadOnly Property M As Integer" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}" & vbCrLf &
"" & vbCrLf &
ServicesVSResources.Summary_colon & vbCrLf &
"Gets a value." & vbCrLf &
"" & vbCrLf &
ServicesVSResources.Returns_colon & vbCrLf &
"Returns a value.")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_XmlDocComments_Value()
            Dim code =
<Code>
    <![CDATA[
Class C
    ''' <summary>
    ''' Gets a value.
    ''' </summary>
    ''' <value>An integer value.</value>
    ReadOnly Property M As Integer
End Class
]]>
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"Public ReadOnly Property M As Integer" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}" & vbCrLf &
"" & vbCrLf &
ServicesVSResources.Summary_colon & vbCrLf &
"Gets a value." & vbCrLf &
"" & vbCrLf &
ServicesVSResources.Value_colon & vbCrLf &
"An integer value.")
            End Using
        End Sub

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942021")>
        <WpfFact>
        Public Sub TestNavInfo_Class()
            Dim code =
<Code>
Namespace EditorFunctionalityHelper
    Public Class EditorFunctionalityHelper
    End Class
End Namespace
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetNamespaceList(0)
                list = list.GetTypeList(0)

                list.VerifyCanonicalNodes(0,
                    ProjectNode("VisualBasicAssembly1"),
                    NamespaceNode("EditorFunctionalityHelper"),
                    TypeNode("EditorFunctionalityHelper"))
            End Using
        End Sub

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942021")>
        <WpfFact>
        Public Sub TestNavInfo_NestedEnum()
            Dim code =
<Code>
Namespace EditorFunctionalityHelper
    Public Class EditorFunctionalityHelper
        Public Enum Mapping
            EnumOne
            EnumTwo
            EnumThree
        End Enum
    End Class
End Namespace
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetNamespaceList(0)
                list = list.GetTypeList(0)

                list.VerifyCanonicalNodes(1, ' Mapping
                    ProjectNode("VisualBasicAssembly1"),
                    NamespaceNode("EditorFunctionalityHelper"),
                    TypeNode("EditorFunctionalityHelper"),
                    TypeNode("Mapping"))
            End Using
        End Sub

    End Class
End Namespace
