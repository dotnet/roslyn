' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel.Extenders
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel.Interop
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.VisualBasic
    Public Class CodeClassTests
        Inherits AbstractCodeClassTests

#Region "Access tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access1()
            Dim code =
<Code>
Class $$C : End Class
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access2()
            Dim code =
<Code>
Friend Class $$C : End Class
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access3()
            Dim code =
<Code>
Public Class $$C : End Class
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access4()
            Dim code =
<Code>
Class C
    Class $$D
    End Class
End Class
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access5()
            Dim code =
<Code>
Class C
    Private Class $$D : End Class
End Class
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPrivate)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access6()
            Dim code =
<Code>
Class C
    Protected Class $$D : End Class
End Class
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProtected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access7()
            Dim code =
<Code>
Class C
    Protected Friend Class $$D : End Class
End Class
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access8()
            Dim code =
<Code>
Class C
    Friend Class $$D : End Class
End Class
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access9()
            Dim code =
<Code>
Class C
    Public Class $$D : End Class
End Class
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

#End Region

#Region "AddBase tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddBase1()
            Dim code =
<Code>
Class C$$
End Class

Class B
End Class
</Code>

            Dim expected =
<Code>
Class C
    Inherits B
End Class

Class B
End Class
</Code>
            TestAddBase(code, "B", Nothing, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddBase2()
            Dim code =
<Code>
Class C$$
    Inherits B

End Class
</Code>
            TestAddBaseThrows(Of COMException)(code, "A", Nothing)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddBase3()
            Dim code =
<Code>
Class $$C
</Code>

            Dim expected =
<Code>
Class C
    Inherits B
</Code>
            TestAddBase(code, "B", Nothing, expected)
        End Sub

#End Region

#Region "ClassKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub ClassKind_MainClass()
            Dim code =
<Code>
Class $$C
End Class
</Code>

            TestClassKind(code, EnvDTE80.vsCMClassKind.vsCMClassKindMainClass)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub ClassKind_Module()
            Dim code =
<Code>
Module $$M
End Module
</Code>

            TestClassKind(code, EnvDTE80.vsCMClassKind.vsCMClassKindModule)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub ClassKind_PartialClass1()
            Dim code =
<Code>
Partial Class $$C
End Class
</Code>

            TestClassKind(code, EnvDTE80.vsCMClassKind.vsCMClassKindPartialClass)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub ClassKind_PartialClass2()
            Dim code =
<Code>
Class $$C
End Class

Partial Class C
End Class
</Code>

            TestClassKind(code, EnvDTE80.vsCMClassKind.vsCMClassKindPartialClass)
        End Sub

#End Region

#Region "Comment tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Comment1()
            Dim code =
<Code>
' Foo
Class $$C
End Class
</Code>

            Dim result = " Foo"

            TestComment(code, result)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Comment2()
            Dim code =
<Code>
' Foo
' Bar
Class $$C
End Class
</Code>

            Dim result = " Foo" & vbCrLf &
                         " Bar"

            TestComment(code, result)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Comment3()
            Dim code =
<Code>
' Foo

' Bar
Class $$C
End Class
</Code>

            Dim result = " Bar"

            TestComment(code, result)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Comment4()
            Dim code =
<Code>
Class B
End Class ' Foo

' Bar
Class $$C
End Class
</Code>

            Dim result = " Bar"

            TestComment(code, result)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Comment5()
            Dim code =
<Code>
' Foo
''' &lt;summary&gt;Bar&lt;/summary&gt;
Class $$C
End Class
</Code>

            Dim result = ""

            TestComment(code, result)
        End Sub

#End Region

#Region "DocComment tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub DocComment1()
            Dim code =
<Code>
''' &lt;summary&gt;
''' Foo
''' &lt;/summary&gt;
''' &lt;remarks&gt;&lt;/remarks&gt;
Class $$C
End Class
</Code>

            Dim result =
" <summary>" & vbCrLf &
" Foo" & vbCrLf &
" </summary>" & vbCrLf &
" <remarks></remarks>"

            TestDocComment(code, result)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub DocComment2()
            Dim code =
<Code>
'''     &lt;summary&gt;
''' Hello World
''' &lt;/summary&gt;
Class $$C
End Class
</Code>

            Dim result =
"     <summary>" & vbCrLf &
" Hello World" & vbCrLf &
" </summary>"

            TestDocComment(code, result)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub DocComment3()
            Dim code =
<Code>
''' &lt;summary&gt;
''' Foo
''' &lt;/summary&gt;
' Bar
''' &lt;remarks&gt;&lt;/remarks&gt;
Class $$C
End Class
</Code>

            Dim result =
" <remarks></remarks>"

            TestDocComment(code, result)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub DocComment4()
            Dim code =
<Code>
Namespace N
    ''' &lt;summary&gt;
    ''' Foo
    ''' &lt;/summary&gt;
    ''' &lt;remarks&gt;&lt;/remarks&gt;
    Class $$C
    End Class
End Namespace
</Code>

            Dim result =
" <summary>" & vbCrLf &
" Foo" & vbCrLf &
" </summary>" & vbCrLf &
" <remarks></remarks>"

            TestDocComment(code, result)
        End Sub

#End Region

#Region "InheritanceKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub InheritanceKind_None()
            Dim code =
<Code>
Class $$C
End Class
</Code>

            TestInheritanceKind(code, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindNone)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub InheritanceKind_Abstract()
            Dim code =
<Code>
MustInherit Class $$C
End Class
</Code>

            TestInheritanceKind(code, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindAbstract)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub InheritanceKind_Sealed()
            Dim code =
<Code>
NotInheritable Class $$C
End Class
</Code>

            TestInheritanceKind(code, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindSealed)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub InheritanceKind_New()
            Dim code =
<Code>
Class Outer
    Protected Class Inner
    End Class
End Class

Class Derived
    Protected Shadows Class $$Inner
    End Class
End Class
</Code>

            TestInheritanceKind(code, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindNew)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub InheritanceKind_AbstractAndNew()
            Dim code =
<Code>
Public Class Outer
    Protected Class Inner

    End Class
End Class

Public Class Derived
    Inherits Outer

    Protected MustInherit Shadows Class $$Inner

    End Class
End Class
</Code>

            TestInheritanceKind(code, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindAbstract Or EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindNew)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub InheritanceKind_AbstractAndNew_Partial1()
            Dim code =
<Code>
Public Class Outer
    Protected Class Inner

    End Class
End Class

Partial Public Class Derived
    Inherits Outer

    Partial Protected MustInherit Class $$Inner

    End Class
End Class

Partial Public Class Derived

    Protected Shadows Class Inner

    End Class
End Class
</Code>

            TestInheritanceKind(code, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindAbstract Or EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindNew)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub InheritanceKind_AbstractAndNew_Partial2()
            Dim code =
<Code>
Public Class Outer
    Protected Class Inner

    End Class
End Class

Partial Public Class Derived
    Inherits Outer

    Partial Protected MustInherit Class Inner

    End Class
End Class

Partial Public Class Derived

    Protected Shadows Class $$Inner

    End Class
End Class
</Code>

            TestInheritanceKind(code, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindAbstract Or EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindNew)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub InheritanceKind_AbstractAndNew_Partial3()
            Dim code =
<Code>
Public Class Outer
    Protected Class Inner

    End Class
End Class

Partial Public Class Derived
    Inherits Outer

    Protected MustInherit Class $$Inner

    End Class
End Class

Partial Public Class Derived

    Partial Protected Shadows Class Inner

    End Class
End Class
</Code>

            TestInheritanceKind(code, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindAbstract Or EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindNew)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub InheritanceKind_AbstractAndNew_Partial4()
            Dim code =
<Code>
Public Class Outer
    Protected Class Inner

    End Class
End Class

Partial Public Class Derived
    Inherits Outer

    Protected MustInherit Class Inner

    End Class
End Class

Partial Public Class Derived

    Partial Protected Shadows Class $$Inner

    End Class
End Class
</Code>

            TestInheritanceKind(code, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindAbstract Or EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindNew)
        End Sub

#End Region

#Region "IsAbstract tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub IsAbstract1()
            Dim code =
<Code>
Class $$C
End Class
</Code>

            TestIsAbstract(code, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub IsAbstract2()
            Dim code =
<Code>
MustInherit Class $$C
End Class
</Code>

            TestIsAbstract(code, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub IsAbstract3()
            Dim code =
<Code>
Partial MustInherit Class $$C
End Class

Partial Class C
End Class
</Code>

            TestIsAbstract(code, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub IsAbstract4()
            Dim code =
<Code>
Partial Class $$C
End Class

Partial MustInherit Class C
End Class
</Code>

            TestIsAbstract(code, True)
        End Sub

#End Region

#Region "IsShared tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub IsShared1()
            Dim code =
<Code>
Class $$C
End Class
</Code>

            TestIsShared(code, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub IsShared2()
            Dim code =
<Code>
Module $$M
End Module
</Code>

            TestIsShared(code, True)
        End Sub

#End Region

#Region "Kind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Kind1()
            Dim code =
<Code>
Class $$C
End Class
</Code>

            TestKind(code, EnvDTE.vsCMElement.vsCMElementClass)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Kind2()
            Dim code =
<Code>
Module $$M
End Module
</Code>

            TestKind(code, EnvDTE.vsCMElement.vsCMElementModule)
        End Sub

#End Region

#Region "Parts tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Parts1()
            Dim code =
<Code>
Class $$C
End Class
</Code>

            TestParts(code, 1)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Parts2()
            Dim code =
<Code>
Partial Class $$C
End Class
</Code>

            TestParts(code, 1)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Parts3()
            Dim code =
<Code>
Partial Class $$C
End Class

Partial Class C
End Class
</Code>

            TestParts(code, 2)
        End Sub
#End Region

#Region "AddFunction tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddFunction1()
            Dim code =
<Code>
Class $$C
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub Foo()

    End Sub
End Class
</Code>

            TestAddFunction(code, expected, New FunctionData With {.Name = "Foo", .Kind = EnvDTE.vsCMFunction.vsCMFunctionSub})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddFunction2()
            Dim code =
<Code>
Class $$C
End Class
</Code>

            Dim expected =
<Code>
Class C
    Private Function Foo() As Integer

    End Function
End Class
</Code>

            TestAddFunction(code, expected, New FunctionData With {.Name = "Foo", .Access = EnvDTE.vsCMAccess.vsCMAccessPrivate, .Type = "Integer"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddFunction_ConstructorFailure()
            ' Note: Adding a constructor by specifying vsCMFunctionConstructor is not supported by VB code model.

            Dim code =
<Code>
Module $$M
End Module
</Code>

            Dim expected =
<Code>
Module M
    Sub New()
    End Sub
End Module
</Code>

            Assert.Throws(Of ArgumentException)(
                Sub()
                    TestAddFunction(code, expected, New FunctionData With {.Kind = EnvDTE.vsCMFunction.vsCMFunctionConstructor})
                End Sub)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddFunction_Constructor1()
            Dim code =
<Code>
Module $$M
End Module
</Code>

            Dim expected =
<Code>
Module M
    Sub New()

    End Sub
End Module
</Code>

            TestAddFunction(code, expected, New FunctionData With {.Name = "New", .Kind = EnvDTE.vsCMFunction.vsCMFunctionSub})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddFunction_Constructor2()
            Dim code =
<Code>
Class $$C
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub New()

    End Sub
End Class
</Code>

            TestAddFunction(code, expected, New FunctionData With {.Name = "New", .Kind = EnvDTE.vsCMFunction.vsCMFunctionSub})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddFunction_Destructor()
            Dim code =
<Code>
Class $$C
End Class
</Code>

            Dim expected =
<Code>
Class C
End Class
</Code>

            Assert.Throws(Of ArgumentException)(
                Sub()
                    TestAddFunction(code, expected, New FunctionData With {.Name = "C", .Kind = EnvDTE.vsCMFunction.vsCMFunctionDestructor})
                End Sub)
        End Sub

        <WorkItem(1172038)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddFunction_AfterIncompleteMember()
            Dim code =
<Code>
Class $$C
    Private Sub M1()
    End Sub

    Private Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Private Sub M1()
    End Sub

    Private Sub
Private Sub M2()

    End Sub
End Class
</Code>

            TestAddFunction(code, expected, New FunctionData With {.Name = "M2", .Type = "void", .Position = -1, .Access = EnvDTE.vsCMAccess.vsCMAccessPrivate})
        End Sub

#End Region

#Region "AddProperty tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddProperty1()
            Dim code =
<Code>
Class $$C
End Class
</Code>

            Dim expected =
<Code>
Class C
    Private Property Name As String
        Get
            Return Nothing
        End Get
        Set(value As String)
        End Set
    End Property
End Class
</Code>

            TestAddProperty(code, expected, New PropertyData With {.GetterName = "Name", .PutterName = "Name", .Type = EnvDTE.vsCMTypeRef.vsCMTypeRefString})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddProperty2()
            Dim code =
<Code>
Class $$C
End Class
</Code>

            Dim expected =
<Code>
Class C
    Private ReadOnly Property Name As String
        Get
            Return Nothing
        End Get
    End Property
End Class
</Code>

            TestAddProperty(code, expected, New PropertyData With {.GetterName = "Name", .PutterName = Nothing, .Type = EnvDTE.vsCMTypeRef.vsCMTypeRefString})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddProperty3()
            Dim code =
<Code>
Class $$C
End Class
</Code>

            Dim expected =
<Code>
Class C
    Private WriteOnly Property Name As String
        Set(value As String)
        End Set
    End Property
End Class
</Code>

            TestAddProperty(code, expected, New PropertyData With {.GetterName = Nothing, .PutterName = "Name", .Type = EnvDTE.vsCMTypeRef.vsCMTypeRefString})
        End Sub

#End Region

#Region "AddImplementedInterface tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddImplementedInterface1()
            Dim code =
<Code>
Class $$C
End Class
</Code>

            Dim expected =
<Code>
Class C
    Implements I
End Class
</Code>

            TestAddImplementedInterface(code, "I", Nothing, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddImplementedInterface2()
            Dim code =
<Code>
Class $$C
    Implements I
End Class
</Code>

            Dim expected =
<Code>
Class C
    Implements J
    Implements I
End Class
</Code>

            TestAddImplementedInterface(code, "J", Nothing, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddImplementedInterface3()
            Dim code =
<Code>
Class $$C
    Implements I
End Class
</Code>

            Dim expected =
<Code>
Class C
    Implements I
    Implements J
End Class
</Code>

            TestAddImplementedInterface(code, "J", -1, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddImplementedInterface4()
            Dim code =
<Code>
Class $$C
End Class
</Code>

            TestAddImplementedInterfaceThrows(Of ArgumentException)(code, "I", 1)
        End Sub

#End Region

#Region "AddVariable tests"

        ' NOTE: Several of these tests have non-ideal expected results, but they are there
        ' to ensure that we are backward compatible with the existing behavior.

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddVariable1()
            Dim code =
<Code>
Class $$C
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim i As Integer
End Class
</Code>

            TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddVariable2()
            Dim code =
<Code>
Class $$C : End Class
</Code>

            Dim expected =
<Code>
Class C :
    Dim i As Integer
End Class
</Code>

            TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddVariable3()
            Dim code =
<Code>
Class $$C
</Code>

            Dim expected =
<Code>
Class C
    Dim i As Integer
</Code>

            TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddVariable4()
            Dim code =
<Code>
Class $$C

End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim i As Integer
End Class
</Code>

            TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddVariable5()
            Dim code =
<Code>
Class $$C
    Inherits B
End Class
</Code>

            Dim expected =
<Code>
Class C
    Inherits B

    Dim i As Integer
End Class
</Code>

            TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddVariable6()
            Dim code =
<Code>
Class $$C
    Inherits B

End Class
</Code>

            Dim expected =
<Code>
Class C
    Inherits B

    Dim i As Integer
End Class
</Code>

            TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddVariable7()
            Dim code =
<Code>
Class $$C
    Sub Foo()
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub Foo()
    End Sub

    Dim i As Integer
End Class
</Code>

            TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = "Foo"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddVariable8()
            Dim code =
<Code>
Class $$C
    Sub Foo()
    End Sub
</Code>

            Dim expected =
<Code>
Class C
    Sub Foo()
    End Sub

    Dim i As Integer
</Code>

            TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = "Foo"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddVariable9()
            Dim code =
<Code>
Class $$C
    Dim x As Integer
    Sub Foo()
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim x As Integer
    Dim i As Integer

    Sub Foo()
    End Sub
End Class
</Code>

            TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = "x"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddVariable10()
            Dim code =
<Code>
Class $$C
    Dim x As Integer
    ' Foo
    Sub Foo()
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim x As Integer
    Dim i As Integer
    ' Foo
    Sub Foo()
    End Sub
End Class
</Code>

            TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = "x"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddVariable11()
            Dim code =
<Code>
Class $$C
    Dim x As Integer

    Sub Foo()
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim x As Integer
    Dim i As Integer

    Sub Foo()
    End Sub
End Class
</Code>

            TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = "x"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddVariable12()
            Dim code =
<Code>
Class $$C
    Dim x, y As New Object

    Sub Foo()
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim x, y As New Object
    Dim i As Integer

    Sub Foo()
    End Sub
End Class
</Code>

            TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = "x"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddVariable13()
            Dim code =
<Code>
Class $$C
    Dim x, y As New Object

    Sub Foo()
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim x, y As New Object
    Dim i As Integer

    Sub Foo()
    End Sub
End Class
</Code>

            TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = "y"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddVariable14()
            Dim code =
<Code>
Class $$C
    Sub Foo()
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim i As Integer

    Sub Foo()
    End Sub
End Class
</Code>

            TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = 0})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddVariable15()
            Dim code =
<Code>
Class $$C
    Sub Foo()
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub Foo()
    End Sub

    Dim i As Integer
End Class
</Code>

            TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = -1})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddVariable16()
            Dim code =
<Code>
Class $$C
    Dim x As Integer
    Dim y As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim x As Integer
    Dim i As Integer
    Dim y As Integer
End Class
</Code>

            TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = "x"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddVariable17()
            Dim code =
<Code>
Class $$C
    Dim x, y As New Object
    Dim z As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim x, y As New Object
    Dim i As Integer
    Dim z As Integer
End Class
</Code>

            TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = "x"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddVariable18()
            Dim code =
<Code>
Class $$C
    Dim x, y As New Object
    Dim z As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim x, y As New Object
    Dim i As Integer
    Dim z As Integer
End Class
</Code>

            TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = "y"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddVariable19()
            Dim code =
<Code>
Class $$C
End Class
</Code>

            Dim expected =
<Code>
Class C
    Public i As Integer
End Class
</Code>

            TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Access = EnvDTE.vsCMAccess.vsCMAccessPublic})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddVariable20()
            Dim code =
<Code>
Class $$C

End Class
</Code>

            Dim expected =
<Code>
Class C
    Private i As Integer
End Class
</Code>

            TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Access = EnvDTE.vsCMAccess.vsCMAccessPrivate})
        End Sub

        <WorkItem(546556)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddVariable21()
            Dim code =
<Code>
Class $$C

End Class
</Code>

            Dim expected =
<Code>
Class C
    Friend i As Integer
End Class
</Code>

            TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Access = EnvDTE.vsCMAccess.vsCMAccessProject})
        End Sub

        <WorkItem(546556)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddVariable22()
            Dim code =
<Code>
Class $$C

End Class
</Code>

            Dim expected =
<Code>
Class C
    Protected Friend i As Integer
End Class
</Code>

            TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Access = EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected})
        End Sub

        <WorkItem(546556)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddVariable23()
            Dim code =
<Code>
Class $$C

End Class
</Code>

            Dim expected =
<Code>
Class C
    Protected i As Integer
End Class
</Code>

            TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Access = EnvDTE.vsCMAccess.vsCMAccessProtected})
        End Sub

        <WorkItem(529865)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddVariableAfterComment()
            Dim code =
<Code>
Class $$C
    Dim i As Integer = 0 ' Foo
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim i As Integer = 0 ' Foo
    Dim j As Integer
End Class
</Code>

            TestAddVariable(code, expected, New VariableData With {.Name = "j", .Type = EnvDTE.vsCMTypeRef.vsCMTypeRefInt, .Position = "i"})
        End Sub

#End Region

#Region "AddAttribute tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute1()
            Dim code =
<Code>
Imports System

Class $$C
End Class
</Code>

            Dim expected =
<Code>
Imports System

&lt;Serializable()&gt;
Class C
End Class
</Code>
            TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute2()
            Dim code =
<Code>
Imports System

&lt;Serializable&gt;
Class $$C
End Class
</Code>

            Dim expected =
<Code>
Imports System

&lt;Serializable&gt;
&lt;CLSCompliant(True)&gt;
Class C
End Class
</Code>
            TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "True", .Position = 1})
        End Sub

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute_BelowDocComment1()
            Dim code =
<Code>
Imports System

''' &lt;summary&gt;&lt;/summary&gt;
Class $$C
End Class
</Code>

            Dim expected =
<Code>
Imports System

''' &lt;summary&gt;&lt;/summary&gt;
&lt;CLSCompliant(True)&gt;
Class C
End Class
</Code>
            TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "True"})
        End Sub

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute_BelowDocComment2()
            Dim code =
<Code>
Imports System

''' &lt;summary&gt;&lt;/summary&gt;
&lt;Serializable&gt;
Class $$C
End Class
</Code>

            Dim expected =
<Code>
Imports System

''' &lt;summary&gt;&lt;/summary&gt;
&lt;CLSCompliant(True)&gt;
&lt;Serializable&gt;
Class C
End Class
</Code>
            TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "True"})
        End Sub

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute_BelowDocComment3()
            Dim code =
<Code>
Imports System

''' &lt;summary&gt;&lt;/summary&gt;
&lt;Serializable&gt;
Class $$C
End Class
</Code>

            Dim expected =
<Code>
Imports System

''' &lt;summary&gt;&lt;/summary&gt;
&lt;Serializable&gt;
&lt;CLSCompliant(True)&gt;
Class C
End Class
</Code>
            TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "True", .Position = 1})
        End Sub

#End Region

#Region "RemoveBase tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveBase1()
            Dim code =
<Code>
Class $$C
    Inherits B
End Class
</Code>

            Dim expected =
<Code>
Class C
End Class
</Code>
            TestRemoveBase(code, "B", expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveBase2()
            Dim code =
<Code>
Class $$C
End Class
</Code>

            TestRemoveBaseThrows(Of COMException)(code, "B")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveBase3()
            Dim code =
<Code>
Class $$C
    Inherits A, B
End Class
</Code>

            Dim expected =
<Code>
Class C
    Inherits A
End Class
</Code>
            TestRemoveBase(code, "B", expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveBase4()
            Dim code =
<Code>
Class $$C
    Inherits A, B
End Class
</Code>

            Dim expected =
<Code>
Class C
    Inherits B
End Class
</Code>
            TestRemoveBase(code, "A", expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveBase5()
            Dim code =
<Code>
Class $$C
    Inherits A, B, D
End Class
</Code>

            Dim expected =
<Code>
Class C
    Inherits A, D
End Class
</Code>
            TestRemoveBase(code, "B", expected)
        End Sub

#End Region

#Region "RemoveImplementedInterface tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveImplementedInterface1()
            Dim code =
<Code>
Class $$C
    Implements I
End Class
</Code>

            Dim expected =
<Code>
Class C
End Class
</Code>
            TestRemoveImplementedInterface(code, "I", expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveImplementedInterface2()
            Dim code =
<Code>
Class $$C
End Class
</Code>

            TestRemoveImplementedInterfaceThrows(Of COMException)(code, "I")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveImplementedInterface3()
            Dim code =
<Code>
Class $$C
    Implements I, J
End Class
</Code>

            Dim expected =
<Code>
Class C
    Implements I
End Class
</Code>
            TestRemoveImplementedInterface(code, "J", expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveImplementedInterface4()
            Dim code =
<Code>
Class $$C
    Implements I, J
End Class
</Code>

            Dim expected =
<Code>
Class C
    Implements J
End Class
</Code>
            TestRemoveImplementedInterface(code, "I", expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveImplementedInterface5()
            Dim code =
<Code>
Class $$C
    Implements I, J, K
End Class
</Code>

            Dim expected =
<Code>
Class C
    Implements I, K
End Class
</Code>
            TestRemoveImplementedInterface(code, "J", expected)
        End Sub

#End Region

#Region "RemoveMember tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveMember1()
            Dim code =
<Code>
Class $$C
    Sub Foo()
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
End Class
</Code>
            TestRemoveChild(code, expected, "Foo")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveMember2()
            Dim code =
<Code><![CDATA[
Class $$C
    ''' <summary>
    ''' Doc comment.
    ''' </summary>
    Sub Foo()
    End Sub
End Class
]]></Code>

            Dim expected =
<Code>
Class C
End Class
</Code>

            TestRemoveChild(code, expected, "Foo")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveMember3()
            Dim code =
<Code><![CDATA[
Class $$C
    ' Comment comment comment
    Sub Foo()
    End Sub
End Class
]]></Code>

            Dim expected =
<Code>
Class C
End Class
</Code>

            TestRemoveChild(code, expected, "Foo")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveMember4()
            Dim code =
<Code><![CDATA[
Class $$C
    ' Comment comment comment

    Sub Foo()
    End Sub
End Class
]]></Code>

            Dim expected =
<Code>
Class C
    ' Comment comment comment
End Class
</Code>

            TestRemoveChild(code, expected, "Foo")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveMember5()
            Dim code =
<Code><![CDATA[
Class $$C
#Region "A region"
    Dim a As Integer
#End Region
    ''' <summary>
    ''' Doc comment.
    ''' </summary>
    Sub Foo()
    End Sub
End Class
]]></Code>

            Dim expected =
<Code>
Class C
#Region "A region"
    Dim a As Integer
#End Region
End Class
</Code>

            TestRemoveChild(code, expected, "Foo")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveMember6()
            Dim code =
<Code><![CDATA[
Class $$C
    ' This comment remains.
    
    ' This comment is deleted.
    ''' <summary>
    ''' This comment is deleted.
    ''' </summary>
    Sub Foo()
    End Sub
End Class
]]></Code>

            Dim expected =
<Code>
Class C
    ' This comment remains.
End Class
</Code>

            TestRemoveChild(code, expected, "Foo")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveMember7()
            Dim code =
<Code>
Class $$C
    Dim x As Integer
    Dim y As Integer
    Dim z As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim x As Integer
    Dim z As Integer
End Class
</Code>

            TestRemoveChild(code, expected, "y")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveMember8()
            Dim code =
<Code>
Class $$C
    Sub Alpha()
    End Sub

    Sub Foo()
    End Sub

    Sub Beta()
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub Alpha()
    End Sub

    Sub Beta()
    End Sub
End Class
</Code>

            TestRemoveChild(code, expected, "Foo")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveMember9()
            Dim code =
<Code>
Class $$C
    Dim x, y As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim x As Integer
End Class
</Code>

            TestRemoveChild(code, expected, "y")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveMember10()
            Dim code =
<Code>
Class $$C
    Dim x, y As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim y As Integer
End Class
</Code>

            TestRemoveChild(code, expected, "x")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveMember11()
            Dim code =
<Code>
Class $$C
    Dim x As String, y As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim y As Integer
End Class
</Code>

            TestRemoveChild(code, expected, "x")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveMember12()
            Dim code =
<Code>
Class $$C
    Dim x As String, y As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim x As String
End Class
</Code>

            TestRemoveChild(code, expected, "y")
        End Sub

#End Region

#Region "Set Access tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess1()
            Dim code =
<Code>
Class $$C : End Class
</Code>

            Dim expected =
<Code>
Public Class C : End Class
</Code>

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess2()
            Dim code =
<Code>
Public Class $$C : End Class
</Code>

            Dim expected =
<Code>
Friend Class C : End Class
</Code>

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess3()
            Dim code =
<Code>
Protected Friend Class $$C : End Class
</Code>

            Dim expected =
<Code>
Public Class C : End Class
</Code>

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess4()
            Dim code =
<Code>
Public Class $$C : End Class
</Code>

            Dim expected =
<Code>
Public Class C : End Class
</Code>

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected, ThrowsArgumentException(Of EnvDTE.vsCMAccess)())
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess5()
            Dim code =
<Code>
Public Class $$C : End Class
</Code>

            Dim expected =
<Code>
Class C : End Class
</Code>

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessDefault)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess6()
            Dim code =
<Code>
Public Class $$C : End Class
</Code>

            Dim expected =
<Code>
Public Class C : End Class
</Code>

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPrivate, ThrowsArgumentException(Of EnvDTE.vsCMAccess)())
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess7()
            Dim code =
<Code>
Class C
    Class $$D : End Class
End Class
</Code>

            Dim expected =
<Code>
Class C
    Private Class D : End Class
End Class
</Code>

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPrivate)
        End Sub

#End Region

#Region "Set ClassKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetClassKind1()
            Dim code =
<Code>
Class $$C
End Class
</Code>

            Dim expected =
<Code>
Class C
End Class
</Code>

            TestSetClassKind(code, expected, EnvDTE80.vsCMClassKind.vsCMClassKindMainClass)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetClassKind2()
            Dim code =
<Code>
Class $$C
End Class
</Code>

            Dim expected =
<Code>
Partial Class C
End Class
</Code>

            TestSetClassKind(code, expected, EnvDTE80.vsCMClassKind.vsCMClassKindPartialClass)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetClassKind3()
            Dim code =
<Code>
Partial Class $$C
End Class
</Code>

            Dim expected =
<Code>
Class C
End Class
</Code>

            TestSetClassKind(code, expected, EnvDTE80.vsCMClassKind.vsCMClassKindMainClass)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetClassKind4()
            Dim code =
<Code>
Module $$M
End Module
</Code>

            Dim expected =
<Code>
Module M
End Module
</Code>

            TestSetClassKind(code, expected, EnvDTE80.vsCMClassKind.vsCMClassKindMainClass, ThrowsNotImplementedException(Of EnvDTE80.vsCMClassKind))
        End Sub

#End Region

#Region "Set DataTypeKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetDataTypeKind1()
            Dim code =
<Code>
Class $$C
End Class
</Code>

            Dim expected =
<Code>
Class C
End Class
</Code>

            TestSetDataTypeKind(code, expected, EnvDTE80.vsCMDataTypeKind.vsCMDataTypeKindMain)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetDataTypeKind2()
            Dim code =
<Code>
Class $$C
End Class
</Code>

            Dim expected =
<Code>
Partial Class C
End Class
</Code>

            TestSetDataTypeKind(code, expected, EnvDTE80.vsCMDataTypeKind.vsCMDataTypeKindPartial)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetDataTypeKind3()
            Dim code =
<Code>
Partial Class $$C
End Class
</Code>

            Dim expected =
<Code>
Class C
End Class
</Code>

            TestSetDataTypeKind(code, expected, EnvDTE80.vsCMDataTypeKind.vsCMDataTypeKindMain)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetDataTypeKind4()
            Dim code =
<Code>
Module $$M

    Sub Foo()
    End Sub

End Module
</Code>

            Dim expected =
<Code>
Class M

    Sub Foo()
    End Sub

End Class
</Code>

            TestSetDataTypeKind(code, expected, EnvDTE80.vsCMDataTypeKind.vsCMDataTypeKindMain)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetDataTypeKind5()
            Dim code =
<Code>
Partial Class $$C

    Sub Foo()
    End Sub

End Class
</Code>

            Dim expected =
<Code>
Module C

    Sub Foo()
    End Sub

End Module
</Code>

            TestSetDataTypeKind(code, expected, EnvDTE80.vsCMDataTypeKind.vsCMDataTypeKindModule)
        End Sub

#End Region

#Region "Set Comment tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetComment1()
            Dim code =
<Code>
' Foo

' Bar
Class $$C
End Class
</Code>

            Dim expected =
<Code>
' Foo

Class C
End Class
</Code>

            TestSetComment(code, expected, Nothing)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetComment2()
            Dim code =
<Code>
' Foo
''' &lt;summary&gt;Bar&lt;/summary&gt;
Class $$C
End Class
</Code>

            Dim expected =
<Code>
' Foo
''' &lt;summary&gt;Bar&lt;/summary&gt;
' Bar
Class C
End Class
</Code>

            TestSetComment(code, expected, "Bar")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetComment3()
            Dim code =
<Code>
' Foo

' Bar
Class $$C
End Class
</Code>

            Dim expected =
<Code>
' Foo

' Blah
Class C
End Class
</Code>

            TestSetComment(code, expected, "Blah")
        End Sub

#End Region

#Region "Set DocComment tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetDocComment_Nothing1()
            Dim code =
<Code>
Class $$C
End Class
</Code>

            Dim expected =
<Code>
Class C
End Class
</Code>

            TestSetDocComment(code, expected, Nothing)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetDocComment_Nothing2()
            Dim code =
<Code>
''' &lt;summary&gt;
''' Foo
''' &lt;/summary&gt;
Class $$C
End Class
</Code>

            Dim expected =
<Code>
Class C
End Class
</Code>

            TestSetDocComment(code, expected, Nothing)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetDocComment_InvalidXml1()
            Dim code =
<Code>
Class $$C
End Class
</Code>

            Dim expected =
<Code>
''' &lt;doc&gt;&lt;summary&gt;Blah&lt;/doc&gt;
Class C
End Class
</Code>

            TestSetDocComment(code, expected, "<doc><summary>Blah</doc>")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetDocComment_InvalidXml2()
            Dim code =
<Code>
Class $$C
End Class
</Code>

            Dim expected =
<Code>
''' &lt;doc___&gt;&lt;summary&gt;Blah&lt;/summary&gt;&lt;/doc___&gt;
Class C
End Class
</Code>

            TestSetDocComment(code, expected, "<doc___><summary>Blah</summary></doc___>")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetDocComment1()
            Dim code =
<Code>
Class $$C
End Class
</Code>

            Dim expected =
<Code>
''' &lt;summary&gt;Hello World&lt;/summary&gt;
Class C
End Class
</Code>

            TestSetDocComment(code, expected, "<summary>Hello World</summary>")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetDocComment2()
            Dim code =
<Code>
''' &lt;summary&gt;Hello World&lt;/summary&gt;
Class $$C
End Class
</Code>

            Dim expected =
<Code>
''' &lt;summary&gt;Blah&lt;/summary&gt;
Class C
End Class
</Code>

            TestSetDocComment(code, expected, "<summary>Blah</summary>")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetDocComment3()
            Dim code =
<Code>
' Foo
Class $$C
End Class
</Code>

            Dim expected =
<Code>
' Foo
''' &lt;summary&gt;Blah&lt;/summary&gt;
Class C
End Class
</Code>

            TestSetDocComment(code, expected, "<summary>Blah</summary>")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetDocComment4()
            Dim code =
<Code>
''' &lt;summary&gt;FogBar&lt;/summary&gt;
' Foo
Class $$C
End Class
</Code>

            Dim expected =
<Code>
''' &lt;summary&gt;Blah&lt;/summary&gt;
' Foo
Class C
End Class
</Code>

            TestSetDocComment(code, expected, "<summary>Blah</summary>")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetDocComment5()
            Dim code =
<Code>
Namespace N
    Class $$C
    End Class
End Namespace
</Code>

            Dim expected =
<Code>
Namespace N
    ''' &lt;summary&gt;Hello World&lt;/summary&gt;
    Class C
    End Class
End Namespace
</Code>

            TestSetDocComment(code, expected, "<summary>Hello World</summary>")
        End Sub

#End Region

#Region "Set InheritanceKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetInheritanceKind1()
            Dim code =
<Code>
Class $$C
End Class
</Code>

            Dim expected =
<Code>
MustInherit Class C
End Class
</Code>

            TestSetInheritanceKind(code, expected, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindAbstract)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetInheritanceKind2()
            Dim code =
<Code>
Class $$C
End Class
</Code>

            Dim expected =
<Code>
NotInheritable Class C
End Class
</Code>

            TestSetInheritanceKind(code, expected, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindSealed)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetInheritanceKind3()
            Dim code =
<Code>
Class C
    Class $$D
    End Class
End Class
</Code>

            Dim expected =
<Code>
Class C
    MustInherit Class D
    End Class
End Class
</Code>

            TestSetInheritanceKind(code, expected, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindAbstract)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetInheritanceKind4()
            Dim code =
<Code>
Class C
    Class $$D
    End Class
End Class
</Code>

            Dim expected =
<Code>
Class C
    NotInheritable Shadows Class D
    End Class
End Class
</Code>

            TestSetInheritanceKind(code, expected, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindSealed Or EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindNew)
        End Sub

#End Region

#Region "Set IsAbstract tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsAbstract1()
            Dim code =
<Code>
Class $$C : End Class
</Code>

            Dim expected =
<Code>
MustInherit Class C : End Class
</Code>

            TestSetIsAbstract(code, expected, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsAbstract2()
            Dim code =
<Code>
MustInherit $$Class C : End Class
</Code>

            Dim expected =
<Code>
Class C : End Class
</Code>

            TestSetIsAbstract(code, expected, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsAbstract3()
            Dim code =
<Code>
Class $$C
End Class
</Code>

            Dim expected =
<Code>
MustInherit Class C
End Class
</Code>

            TestSetIsAbstract(code, expected, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsAbstract4()
            Dim code =
<Code>
MustInherit Class $$C
End Class
</Code>

            Dim expected =
<Code>
Class C
End Class
</Code>

            TestSetIsAbstract(code, expected, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsAbstract5()
            ' Note: This is a behavior change from Dev11 where VB code model will blow
            ' out the Shadows modifier when setting IsAbstract.

            Dim code =
<Code>
Class C
    Shadows Class $$D
    End Class
End Class
</Code>

            Dim expected =
<Code>
Class C
    MustInherit Shadows Class D
    End Class
End Class
</Code>

            TestSetIsAbstract(code, expected, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsAbstract6()
            ' Note: This is a behavior change from Dev11 where VB code model will blow
            ' out the Shadows modifier when setting IsAbstract.

            Dim code =
<Code>
Class C
    MustInherit Shadows Class $$D
    End Class
End Class
</Code>

            Dim expected =
<Code>
Class C
    Shadows Class D
    End Class
End Class
</Code>

            TestSetIsAbstract(code, expected, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsAbstract7()
            Dim code =
<Code>
NotInheritable Class $$C
End Class
</Code>

            Dim expected =
<Code>
MustInherit Class C
End Class
</Code>

            TestSetIsAbstract(code, expected, True)
        End Sub

#End Region

#Region "Set IsShared tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsShared1()
            Dim code =
<Code>
Class $$C : End Class
</Code>
            Dim expected =
<Code>
Class C : End Class
</Code>

            TestSetIsShared(code, expected, True, ThrowsNotImplementedException(Of Boolean))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsShared2()
            Dim code =
<Code>
Module $$M : End Module
</Code>

            Dim expected =
<Code>
Module M : End Module
</Code>

            TestSetIsShared(code, expected, True, ThrowsNotImplementedException(Of Boolean))
        End Sub

#End Region

#Region "Set Name tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetName1()
            Dim code =
    <Code>
Class $$Foo
End Class
</Code>

            Dim expected =
    <Code>
Class Bar
End Class
</Code>

            TestSetName(code, expected, "Bar", NoThrow(Of String)())
        End Sub
#End Region

#Region "NameSpace Tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetNamespaceNameFromInnerClass()
            Dim code =
    <Code>
Namespace NS1
    Class C1
        Class $$C2
        End Class
    End Class
End NameSpace
</Code>

            TestNamespaceName(code, "NS1")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetNamespaceNameFromOuterClass()
            Dim code =
    <Code>
Namespace NS1
    Class $$C1
        Class C2
        End Class
    End Class
End NameSpace
</Code>

            TestNamespaceName(code, "NS1")
        End Sub
#End Region

#Region "GenericExtender"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GenericExtender_GetBaseTypesCount_Class1()
            Dim code =
<Code>
Class C$$
End Class
</Code>

            TestGenericNameExtender_GetBaseTypesCount(code, 1)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GenericExtender_GetBaseTypesCount_Class2()
            Dim code =
<Code>
Class C$$
    Inherits B
End Class

Class B
End Class
</Code>

            TestGenericNameExtender_GetBaseTypesCount(code, 1)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GenericExtender_GetBaseGenericName_Class1()
            Dim code =
<Code>
Class C$$
End Class
</Code>

            TestGenericNameExtender_GetBaseGenericName(code, 1, "Object")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GenericExtender_GetBaseGenericName_Class2()
            Dim code =
<Code>
Class C$$
    Inherits B
End Class

Class B
End Class
</Code>

            TestGenericNameExtender_GetBaseGenericName(code, 1, "B")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GenericExtender_GetImplementedTypesCount_Class1()
            Dim code =
<Code>
Class C$$
End Class
</Code>

            TestGenericNameExtender_GetImplementedTypesCount(code, 0)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GenericExtender_GetImplementedTypesCount_Class2()
            Dim code =
<Code>
Class C$$
    Implements IFoo(Of String)
End Class

Interface IFoo(Of T)
End Interface
</Code>

            TestGenericNameExtender_GetImplementedTypesCount(code, 1)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GenericExtender_GetImplTypeGenericName_Class1()
            Dim code =
<Code>
Class C$$
End Class
</Code>

            TestGenericNameExtender_GetImplTypeGenericName(code, 1, Nothing)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GenericExtender_GetImplTypeGenericName_Class2()
            Dim code =
<Code>
Class C$$
    Implements IFoo(Of System.String)
End Class

Interface IFoo(Of T)
End Interface
</Code>

            TestGenericNameExtender_GetImplTypeGenericName(code, 1, "IFoo(Of String)")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GenericExtender_GetBaseTypesCount_Module()
            Dim code =
<Code>
Module M$$
End Module
</Code>

            TestGenericNameExtender_GetBaseTypesCount(code, 1)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GenericExtender_GetBaseGenericName_Module()
            Dim code =
<Code>
Module M$$
End Module
</Code>

            TestGenericNameExtender_GetBaseGenericName(code, 1, "Object")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GenericExtender_GetImplementedTypesCount_Module()
            Dim code =
<Code>
Module M$$
End Module
</Code>

            TestGenericNameExtender_GetImplementedTypesCountThrows(Of ArgumentException)(code)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GenericExtender_GetImplTypeGenericName_Module()
            Dim code =
<Code>
Module M$$
End Module
</Code>

            TestGenericNameExtender_GetImplTypeGenericNameThrows(Of ArgumentException)(code, 1)
        End Sub

#End Region

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetBaseName1()
            Dim code =
<Code>
Imports N.M

Namespace N
    Namespace M
        Class Generic(Of T)
        End Class
    End Namespace
End Namespace

Class $$C 
    Inherits Generic(Of String)
End Class
</Code>

            TestGetBaseName(code, "N.M.Generic(Of String)")
        End Sub

        ' Note: This unit test has diverged and is not asynchronous in stabilization. If merged into master,
        ' take the master version and remove this comment.
        Public Sub TestAddDeleteManyTimes()
            Dim code =
<Code>
Class C$$
End Class
</Code>

            TestElement(code,
                Sub(codeClass)
                    For i = 1 To 100
                        Dim variable = codeClass.AddVariable("x", "System.Int32")
                        codeClass.RemoveMember(variable)
                    Next
                End Sub)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub ExternalClass_ImplementedInterfaces()
            Dim code =
<Code>
Class $$Foo
    Inherits System.Collections.Generic.List(Of Integer)
End Class
</Code>

            TestElement(code,
                Sub(codeClass)
                    Dim listType = TryCast(codeClass.Bases.Item(1), EnvDTE80.CodeClass2)
                    Assert.NotNull(listType)

                    Assert.Equal(8, listType.ImplementedInterfaces.Count)
                End Sub)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub ClassMembersForWithEventsField()
            Dim code =
<Code>
Class C
    Event E(x As Integer)
End Class

Class D$$
    Inherits C

    Private WithEvents x As C

    Private Sub D_E(x As Integer) Handles Me.E
    End Sub
End Class
</Code>

            TestElement(code,
                Sub(codeElement)
                    Dim members = codeElement.Members
                    Assert.Equal(2, members.Count)

                    Dim member1 = members.Item(1)
                    Assert.Equal("x", member1.Name)
                    Assert.Equal(EnvDTE.vsCMElement.vsCMElementVariable, member1.Kind)

                    Dim member2 = members.Item(2)
                    Assert.Equal("D_E", member2.Name)
                    Assert.Equal(EnvDTE.vsCMElement.vsCMElementFunction, member2.Kind)
                End Sub)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub ClassIncludedDeclareMethods()
            Dim code =
<Code>
Public Class $$C1
   Private Sub MethodA()
   End Sub
   Private Declare Sub MethodB Lib "MyDll.dll" ()
   Private Declare Function MethodC Lib "MyDll.dll" () As Integer
   Private Sub MethodD()
   End Sub
End Class
</Code>

            TestElement(code,
                Sub(codeClass)
                    Dim members = codeClass.Members
                    Assert.Equal(4, members.Count)

                    Dim member1 = TryCast(members.Item(1), EnvDTE.CodeFunction)
                    Assert.NotNull(member1)
                    Assert.Equal("MethodA", member1.Name)

                    Dim member2 = TryCast(members.Item(2), EnvDTE.CodeFunction)
                    Assert.NotNull(member2)
                    Assert.Equal("MethodB", member2.Name)

                    Dim member3 = TryCast(members.Item(3), EnvDTE.CodeFunction)
                    Assert.NotNull(member3)
                    Assert.Equal("MethodC", member3.Name)

                    Dim member4 = TryCast(members.Item(4), EnvDTE.CodeFunction)
                    Assert.NotNull(member4)
                    Assert.Equal("MethodD", member4.Name)
                End Sub)
        End Sub

        Private Function GetGenericExtender(codeElement As EnvDTE80.CodeClass2) As IVBGenericExtender
            Return CType(codeElement.Extender(ExtenderNames.VBGenericExtender), IVBGenericExtender)
        End Function

        Protected Overrides Function GenericNameExtender_GetBaseTypesCount(codeElement As EnvDTE80.CodeClass2) As Integer
            Return GetGenericExtender(codeElement).GetBaseTypesCount()
        End Function

        Protected Overrides Function GenericNameExtender_GetImplementedTypesCount(codeElement As EnvDTE80.CodeClass2) As Integer
            Return GetGenericExtender(codeElement).GetImplementedTypesCount()
        End Function

        Protected Overrides Function GenericNameExtender_GetBaseGenericName(codeElement As EnvDTE80.CodeClass2, index As Integer) As String
            Return GetGenericExtender(codeElement).GetBaseGenericName(index)
        End Function

        Protected Overrides Function GenericNameExtender_GetImplTypeGenericName(codeElement As EnvDTE80.CodeClass2, index As Integer) As String
            Return GetGenericExtender(codeElement).GetImplTypeGenericName(index)
        End Function

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property
    End Class
End Namespace
