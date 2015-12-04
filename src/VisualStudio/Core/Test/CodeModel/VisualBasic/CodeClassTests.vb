' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel.Extenders
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel.Interop
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.VisualBasic
    Public Class CodeClassTests
        Inherits AbstractCodeClassTests

#Region "Access tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess1() As Task
            Dim code =
<Code>
Class $$C : End Class
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess2() As Task
            Dim code =
<Code>
Friend Class $$C : End Class
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess3() As Task
            Dim code =
<Code>
Public Class $$C : End Class
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess4() As Task
            Dim code =
<Code>
Class C
    Class $$D
    End Class
End Class
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess5() As Task
            Dim code =
<Code>
Class C
    Private Class $$D : End Class
End Class
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPrivate)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess6() As Task
            Dim code =
<Code>
Class C
    Protected Class $$D : End Class
End Class
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProtected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess7() As Task
            Dim code =
<Code>
Class C
    Protected Friend Class $$D : End Class
End Class
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess8() As Task
            Dim code =
<Code>
Class C
    Friend Class $$D : End Class
End Class
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess9() As Task
            Dim code =
<Code>
Class C
    Public Class $$D : End Class
End Class
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

#End Region

#Region "AddBase tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddBase1() As Task
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
            Await TestAddBase(code, "B", Nothing, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddBase2() As Task
            Dim code =
<Code>
Class C$$
    Inherits B

End Class
</Code>
            Await TestAddBaseThrows(Of COMException)(code, "A", Nothing)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddBase3() As Task
            Dim code =
<Code>
Class $$C
</Code>

            Dim expected =
<Code>
Class C
    Inherits B
</Code>
            Await TestAddBase(code, "B", Nothing, expected)
        End Function

#End Region

#Region "ClassKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestClassKind_MainClass() As Task
            Dim code =
<Code>
Class $$C
End Class
</Code>

            Await TestClassKind(code, EnvDTE80.vsCMClassKind.vsCMClassKindMainClass)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestClassKind_Module() As Task
            Dim code =
<Code>
Module $$M
End Module
</Code>

            Await TestClassKind(code, EnvDTE80.vsCMClassKind.vsCMClassKindModule)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestClassKind_PartialClass1() As Task
            Dim code =
<Code>
Partial Class $$C
End Class
</Code>

            Await TestClassKind(code, EnvDTE80.vsCMClassKind.vsCMClassKindPartialClass)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestClassKind_PartialClass2() As Task
            Dim code =
<Code>
Class $$C
End Class

Partial Class C
End Class
</Code>

            Await TestClassKind(code, EnvDTE80.vsCMClassKind.vsCMClassKindPartialClass)
        End Function

#End Region

#Region "Comment tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestComment1() As Task
            Dim code =
<Code>
' Foo
Class $$C
End Class
</Code>

            Dim result = " Foo"

            Await TestComment(code, result)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestComment2() As Task
            Dim code =
<Code>
' Foo
' Bar
Class $$C
End Class
</Code>

            Dim result = " Foo" & vbCrLf &
                         " Bar"

            Await TestComment(code, result)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestComment3() As Task
            Dim code =
<Code>
' Foo

' Bar
Class $$C
End Class
</Code>

            Dim result = " Bar"

            Await TestComment(code, result)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestComment4() As Task
            Dim code =
<Code>
Class B
End Class ' Foo

' Bar
Class $$C
End Class
</Code>

            Dim result = " Bar"

            Await TestComment(code, result)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestComment5() As Task
            Dim code =
<Code>
' Foo
''' &lt;summary&gt;Bar&lt;/summary&gt;
Class $$C
End Class
</Code>

            Dim result = ""

            Await TestComment(code, result)
        End Function

#End Region

#Region "DocComment tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDocComment1() As Task
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

            Await TestDocComment(code, result)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDocComment2() As Task
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

            Await TestDocComment(code, result)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDocComment3() As Task
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

            Await TestDocComment(code, result)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDocComment4() As Task
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

            Await TestDocComment(code, result)
        End Function

#End Region

#Region "InheritanceKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestInheritanceKind_None() As Task
            Dim code =
<Code>
Class $$C
End Class
</Code>

            Await TestInheritanceKind(code, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindNone)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestInheritanceKind_Abstract() As Task
            Dim code =
<Code>
MustInherit Class $$C
End Class
</Code>

            Await TestInheritanceKind(code, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindAbstract)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestInheritanceKind_Sealed() As Task
            Dim code =
<Code>
NotInheritable Class $$C
End Class
</Code>

            Await TestInheritanceKind(code, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindSealed)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestInheritanceKind_New() As Task
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

            Await TestInheritanceKind(code, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindNew)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestInheritanceKind_AbstractAndNew() As Task
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

            Await TestInheritanceKind(code, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindAbstract Or EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindNew)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestInheritanceKind_AbstractAndNew_Partial1() As Task
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

            Await TestInheritanceKind(code, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindAbstract Or EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindNew)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestInheritanceKind_AbstractAndNew_Partial2() As Task
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

            Await TestInheritanceKind(code, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindAbstract Or EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindNew)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestInheritanceKind_AbstractAndNew_Partial3() As Task
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

            Await TestInheritanceKind(code, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindAbstract Or EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindNew)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestInheritanceKind_AbstractAndNew_Partial4() As Task
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

            Await TestInheritanceKind(code, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindAbstract Or EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindNew)
        End Function

#End Region

#Region "IsAbstract tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestIsAbstract1() As Task
            Dim code =
<Code>
Class $$C
End Class
</Code>

            Await TestIsAbstract(code, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestIsAbstract2() As Task
            Dim code =
<Code>
MustInherit Class $$C
End Class
</Code>

            Await TestIsAbstract(code, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestIsAbstract3() As Task
            Dim code =
<Code>
Partial MustInherit Class $$C
End Class

Partial Class C
End Class
</Code>

            Await TestIsAbstract(code, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestIsAbstract4() As Task
            Dim code =
<Code>
Partial Class $$C
End Class

Partial MustInherit Class C
End Class
</Code>

            Await TestIsAbstract(code, True)
        End Function

#End Region

#Region "IsShared tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestIsShared1() As Task
            Dim code =
<Code>
Class $$C
End Class
</Code>

            Await TestIsShared(code, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestIsShared2() As Task
            Dim code =
<Code>
Module $$M
End Module
</Code>

            Await TestIsShared(code, True)
        End Function

#End Region

#Region "Kind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestKind1() As Task
            Dim code =
<Code>
Class $$C
End Class
</Code>

            Await TestKind(code, EnvDTE.vsCMElement.vsCMElementClass)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestKind2() As Task
            Dim code =
<Code>
Module $$M
End Module
</Code>

            Await TestKind(code, EnvDTE.vsCMElement.vsCMElementModule)
        End Function

#End Region

#Region "Parts tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestParts1() As Task
            Dim code =
<Code>
Class $$C
End Class
</Code>

            Await TestParts(code, 1)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestParts2() As Task
            Dim code =
<Code>
Partial Class $$C
End Class
</Code>

            Await TestParts(code, 1)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestParts3() As Task
            Dim code =
<Code>
Partial Class $$C
End Class

Partial Class C
End Class
</Code>

            Await TestParts(code, 2)
        End Function
#End Region

#Region "AddFunction tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddFunction1() As Task
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

            Await TestAddFunction(code, expected, New FunctionData With {.Name = "Foo", .Kind = EnvDTE.vsCMFunction.vsCMFunctionSub})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddFunction2() As Task
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

            Await TestAddFunction(code, expected, New FunctionData With {.Name = "Foo", .Access = EnvDTE.vsCMAccess.vsCMAccessPrivate, .Type = "Integer"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddFunction_ConstructorFailure() As Task
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

            Await Assert.ThrowsAsync(Of ArgumentException)(
                Async Function()
                    Await TestAddFunction(code, expected, New FunctionData With {.Kind = EnvDTE.vsCMFunction.vsCMFunctionConstructor})
                End Function)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddFunction_Constructor1() As Task
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

            Await TestAddFunction(code, expected, New FunctionData With {.Name = "New", .Kind = EnvDTE.vsCMFunction.vsCMFunctionSub})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddFunction_Constructor2() As Task
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

            Await TestAddFunction(code, expected, New FunctionData With {.Name = "New", .Kind = EnvDTE.vsCMFunction.vsCMFunctionSub})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddFunction_Destructor() As Task
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

            Await Assert.ThrowsAsync(Of ArgumentException)(
                Async Function()
                    Await TestAddFunction(code, expected, New FunctionData With {.Name = "C", .Kind = EnvDTE.vsCMFunction.vsCMFunctionDestructor})
                End Function)
        End Function

        <WorkItem(1172038)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddFunction_AfterIncompleteMember() As Task
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

            Await TestAddFunction(code, expected, New FunctionData With {.Name = "M2", .Type = "void", .Position = -1, .Access = EnvDTE.vsCMAccess.vsCMAccessPrivate})
        End Function

#End Region

#Region "AddProperty tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddProperty1() As Task
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

            Await TestAddProperty(code, expected, New PropertyData With {.GetterName = "Name", .PutterName = "Name", .Type = EnvDTE.vsCMTypeRef.vsCMTypeRefString})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddProperty2() As Task
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

            Await TestAddProperty(code, expected, New PropertyData With {.GetterName = "Name", .PutterName = Nothing, .Type = EnvDTE.vsCMTypeRef.vsCMTypeRefString})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddProperty3() As Task
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

            Await TestAddProperty(code, expected, New PropertyData With {.GetterName = Nothing, .PutterName = "Name", .Type = EnvDTE.vsCMTypeRef.vsCMTypeRefString})
        End Function

#End Region

#Region "AddImplementedInterface tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddImplementedInterface1() As Task
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

            Await TestAddImplementedInterface(code, "I", Nothing, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddImplementedInterface2() As Task
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

            Await TestAddImplementedInterface(code, "J", Nothing, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddImplementedInterface3() As Task
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

            Await TestAddImplementedInterface(code, "J", -1, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddImplementedInterface4() As Task
            Dim code =
<Code>
Class $$C
End Class
</Code>

            Await TestAddImplementedInterfaceThrows(Of ArgumentException)(code, "I", 1)
        End Function

#End Region

#Region "AddVariable tests"

        ' NOTE: Several of these tests have non-ideal expected results, but they are there
        ' to ensure that we are backward compatible with the existing behavior.

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariable1() As Task
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

            Await TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariable2() As Task
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

            Await TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariable3() As Task
            Dim code =
<Code>
Class $$C
</Code>

            Dim expected =
<Code>
Class C
    Dim i As Integer
</Code>

            Await TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariable4() As Task
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

            Await TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariable5() As Task
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

            Await TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariable6() As Task
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

            Await TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariable7() As Task
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

            Await TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = "Foo"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariable8() As Task
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

            Await TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = "Foo"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariable9() As Task
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

            Await TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = "x"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariable10() As Task
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

            Await TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = "x"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariable11() As Task
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

            Await TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = "x"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariable12() As Task
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

            Await TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = "x"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariable13() As Task
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

            Await TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = "y"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariable14() As Task
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

            Await TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = 0})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariable15() As Task
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

            Await TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = -1})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariable16() As Task
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

            Await TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = "x"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariable17() As Task
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

            Await TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = "x"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariable18() As Task
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

            Await TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = "y"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariable19() As Task
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

            Await TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Access = EnvDTE.vsCMAccess.vsCMAccessPublic})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariable20() As Task
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

            Await TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Access = EnvDTE.vsCMAccess.vsCMAccessPrivate})
        End Function

        <WorkItem(546556)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariable21() As Task
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

            Await TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Access = EnvDTE.vsCMAccess.vsCMAccessProject})
        End Function

        <WorkItem(546556)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariable22() As Task
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

            Await TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Access = EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected})
        End Function

        <WorkItem(546556)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariable23() As Task
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

            Await TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Access = EnvDTE.vsCMAccess.vsCMAccessProtected})
        End Function

        <WorkItem(529865)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariableAfterComment() As Task
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

            Await TestAddVariable(code, expected, New VariableData With {.Name = "j", .Type = EnvDTE.vsCMTypeRef.vsCMTypeRefInt, .Position = "i"})
        End Function

#End Region

#Region "AddAttribute tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute1() As Task
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
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute2() As Task
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
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "True", .Position = 1})
        End Function

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute_BelowDocComment1() As Task
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
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "True"})
        End Function

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute_BelowDocComment2() As Task
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
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "True"})
        End Function

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute_BelowDocComment3() As Task
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
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "True", .Position = 1})
        End Function

#End Region

#Region "RemoveBase tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveBase1() As Task
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
            Await TestRemoveBase(code, "B", expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveBase2() As Task
            Dim code =
<Code>
Class $$C
End Class
</Code>

            Await TestRemoveBaseThrows(Of COMException)(code, "B")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveBase3() As Task
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
            Await TestRemoveBase(code, "B", expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveBase4() As Task
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
            Await TestRemoveBase(code, "A", expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveBase5() As Task
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
            Await TestRemoveBase(code, "B", expected)
        End Function

#End Region

#Region "RemoveImplementedInterface tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveImplementedInterface1() As Task
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
            Await TestRemoveImplementedInterface(code, "I", expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveImplementedInterface2() As Task
            Dim code =
<Code>
Class $$C
End Class
</Code>

            Await TestRemoveImplementedInterfaceThrows(Of COMException)(code, "I")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveImplementedInterface3() As Task
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
            Await TestRemoveImplementedInterface(code, "J", expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveImplementedInterface4() As Task
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
            Await TestRemoveImplementedInterface(code, "I", expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveImplementedInterface5() As Task
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
            Await TestRemoveImplementedInterface(code, "J", expected)
        End Function

#End Region

#Region "RemoveMember tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveMember1() As Task
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
            Await TestRemoveChild(code, expected, "Foo")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveMember2() As Task
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

            Await TestRemoveChild(code, expected, "Foo")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveMember3() As Task
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

            Await TestRemoveChild(code, expected, "Foo")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveMember4() As Task
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

            Await TestRemoveChild(code, expected, "Foo")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveMember5() As Task
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

            Await TestRemoveChild(code, expected, "Foo")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveMember6() As Task
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

            Await TestRemoveChild(code, expected, "Foo")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveMember7() As Task
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

            Await TestRemoveChild(code, expected, "y")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveMember8() As Task
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

            Await TestRemoveChild(code, expected, "Foo")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveMember9() As Task
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

            Await TestRemoveChild(code, expected, "y")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveMember10() As Task
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

            Await TestRemoveChild(code, expected, "x")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveMember11() As Task
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

            Await TestRemoveChild(code, expected, "x")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveMember12() As Task
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

            Await TestRemoveChild(code, expected, "y")
        End Function

#End Region

#Region "Set Access tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess1() As Task
            Dim code =
<Code>
Class $$C : End Class
</Code>

            Dim expected =
<Code>
Public Class C : End Class
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess2() As Task
            Dim code =
<Code>
Public Class $$C : End Class
</Code>

            Dim expected =
<Code>
Friend Class C : End Class
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess3() As Task
            Dim code =
<Code>
Protected Friend Class $$C : End Class
</Code>

            Dim expected =
<Code>
Public Class C : End Class
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess4() As Task
            Dim code =
<Code>
Public Class $$C : End Class
</Code>

            Dim expected =
<Code>
Public Class C : End Class
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected, ThrowsArgumentException(Of EnvDTE.vsCMAccess)())
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess5() As Task
            Dim code =
<Code>
Public Class $$C : End Class
</Code>

            Dim expected =
<Code>
Class C : End Class
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessDefault)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess6() As Task
            Dim code =
<Code>
Public Class $$C : End Class
</Code>

            Dim expected =
<Code>
Public Class C : End Class
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPrivate, ThrowsArgumentException(Of EnvDTE.vsCMAccess)())
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess7() As Task
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

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPrivate)
        End Function

#End Region

#Region "Set ClassKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetClassKind1() As Task
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

            Await TestSetClassKind(code, expected, EnvDTE80.vsCMClassKind.vsCMClassKindMainClass)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetClassKind2() As Task
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

            Await TestSetClassKind(code, expected, EnvDTE80.vsCMClassKind.vsCMClassKindPartialClass)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetClassKind3() As Task
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

            Await TestSetClassKind(code, expected, EnvDTE80.vsCMClassKind.vsCMClassKindMainClass)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetClassKind4() As Task
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

            Await TestSetClassKind(code, expected, EnvDTE80.vsCMClassKind.vsCMClassKindMainClass, ThrowsNotImplementedException(Of EnvDTE80.vsCMClassKind))
        End Function

#End Region

#Region "Set DataTypeKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetDataTypeKind1() As Task
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

            Await TestSetDataTypeKind(code, expected, EnvDTE80.vsCMDataTypeKind.vsCMDataTypeKindMain)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetDataTypeKind2() As Task
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

            Await TestSetDataTypeKind(code, expected, EnvDTE80.vsCMDataTypeKind.vsCMDataTypeKindPartial)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetDataTypeKind3() As Task
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

            Await TestSetDataTypeKind(code, expected, EnvDTE80.vsCMDataTypeKind.vsCMDataTypeKindMain)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetDataTypeKind4() As Task
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

            Await TestSetDataTypeKind(code, expected, EnvDTE80.vsCMDataTypeKind.vsCMDataTypeKindMain)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetDataTypeKind5() As Task
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

            Await TestSetDataTypeKind(code, expected, EnvDTE80.vsCMDataTypeKind.vsCMDataTypeKindModule)
        End Function

#End Region

#Region "Set Comment tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetComment1() As Task
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

            Await TestSetComment(code, expected, Nothing)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetComment2() As Task
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

            Await TestSetComment(code, expected, "Bar")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetComment3() As Task
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

            Await TestSetComment(code, expected, "Blah")
        End Function

#End Region

#Region "Set DocComment tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetDocComment_Nothing1() As Task
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

            Await TestSetDocComment(code, expected, Nothing)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetDocComment_Nothing2() As Task
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

            Await TestSetDocComment(code, expected, Nothing)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetDocComment_InvalidXml1() As Task
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

            Await TestSetDocComment(code, expected, "<doc><summary>Blah</doc>")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetDocComment_InvalidXml2() As Task
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

            Await TestSetDocComment(code, expected, "<doc___><summary>Blah</summary></doc___>")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetDocComment1() As Task
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

            Await TestSetDocComment(code, expected, "<summary>Hello World</summary>")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetDocComment2() As Task
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

            Await TestSetDocComment(code, expected, "<summary>Blah</summary>")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetDocComment3() As Task
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

            Await TestSetDocComment(code, expected, "<summary>Blah</summary>")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetDocComment4() As Task
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

            Await TestSetDocComment(code, expected, "<summary>Blah</summary>")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetDocComment5() As Task
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

            Await TestSetDocComment(code, expected, "<summary>Hello World</summary>")
        End Function

#End Region

#Region "Set InheritanceKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetInheritanceKind1() As Task
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

            Await TestSetInheritanceKind(code, expected, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindAbstract)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetInheritanceKind2() As Task
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

            Await TestSetInheritanceKind(code, expected, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindSealed)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetInheritanceKind3() As Task
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

            Await TestSetInheritanceKind(code, expected, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindAbstract)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetInheritanceKind4() As Task
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

            Await TestSetInheritanceKind(code, expected, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindSealed Or EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindNew)
        End Function

#End Region

#Region "Set IsAbstract tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsAbstract1() As Task
            Dim code =
<Code>
Class $$C : End Class
</Code>

            Dim expected =
<Code>
MustInherit Class C : End Class
</Code>

            Await TestSetIsAbstract(code, expected, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsAbstract2() As Task
            Dim code =
<Code>
MustInherit $$Class C : End Class
</Code>

            Dim expected =
<Code>
Class C : End Class
</Code>

            Await TestSetIsAbstract(code, expected, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsAbstract3() As Task
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

            Await TestSetIsAbstract(code, expected, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsAbstract4() As Task
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

            Await TestSetIsAbstract(code, expected, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsAbstract5() As Task
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

            Await TestSetIsAbstract(code, expected, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsAbstract6() As Task
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

            Await TestSetIsAbstract(code, expected, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsAbstract7() As Task
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

            Await TestSetIsAbstract(code, expected, True)
        End Function

#End Region

#Region "Set IsShared tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsShared1() As Task
            Dim code =
<Code>
Class $$C : End Class
</Code>
            Dim expected =
<Code>
Class C : End Class
</Code>

            Await TestSetIsShared(code, expected, True, ThrowsNotImplementedException(Of Boolean))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsShared2() As Task
            Dim code =
<Code>
Module $$M : End Module
</Code>

            Dim expected =
<Code>
Module M : End Module
</Code>

            Await TestSetIsShared(code, expected, True, ThrowsNotImplementedException(Of Boolean))
        End Function

#End Region

#Region "Set Name tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetName1() As Task
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

            Await TestSetName(code, expected, "Bar", NoThrow(Of String)())
        End Function
#End Region

#Region "NameSpace Tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetNamespaceNameFromInnerClass() As Task
            Dim code =
    <Code>
Namespace NS1
    Class C1
        Class $$C2
        End Class
    End Class
End NameSpace
</Code>

            Await TestNamespaceName(code, "NS1")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetNamespaceNameFromOuterClass() As Task
            Dim code =
    <Code>
Namespace NS1
    Class $$C1
        Class C2
        End Class
    End Class
End NameSpace
</Code>

            Await TestNamespaceName(code, "NS1")
        End Function
#End Region

#Region "GenericExtender"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGenericExtender_GetBaseTypesCount_Class1() As Task
            Dim code =
<Code>
Class C$$
End Class
</Code>

            Await TestGenericNameExtender_GetBaseTypesCount(code, 1)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGenericExtender_GetBaseTypesCount_Class2() As Task
            Dim code =
<Code>
Class C$$
    Inherits B
End Class

Class B
End Class
</Code>

            Await TestGenericNameExtender_GetBaseTypesCount(code, 1)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGenericExtender_GetBaseGenericName_Class1() As Task
            Dim code =
<Code>
Class C$$
End Class
</Code>

            Await TestGenericNameExtender_GetBaseGenericName(code, 1, "Object")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGenericExtender_GetBaseGenericName_Class2() As Task
            Dim code =
<Code>
Class C$$
    Inherits B
End Class

Class B
End Class
</Code>

            Await TestGenericNameExtender_GetBaseGenericName(code, 1, "B")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGenericExtender_GetImplementedTypesCount_Class1() As Task
            Dim code =
<Code>
Class C$$
End Class
</Code>

            Await TestGenericNameExtender_GetImplementedTypesCount(code, 0)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGenericExtender_GetImplementedTypesCount_Class2() As Task
            Dim code =
<Code>
Class C$$
    Implements IFoo(Of String)
End Class

Interface IFoo(Of T)
End Interface
</Code>

            Await TestGenericNameExtender_GetImplementedTypesCount(code, 1)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGenericExtender_GetImplTypeGenericName_Class1() As Task
            Dim code =
<Code>
Class C$$
End Class
</Code>

            Await TestGenericNameExtender_GetImplTypeGenericName(code, 1, Nothing)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGenericExtender_GetImplTypeGenericName_Class2() As Task
            Dim code =
<Code>
Class C$$
    Implements IFoo(Of System.String)
End Class

Interface IFoo(Of T)
End Interface
</Code>

            Await TestGenericNameExtender_GetImplTypeGenericName(code, 1, "IFoo(Of String)")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGenericExtender_GetBaseTypesCount_Module() As Task
            Dim code =
<Code>
Module M$$
End Module
</Code>

            Await TestGenericNameExtender_GetBaseTypesCount(code, 1)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGenericExtender_GetBaseGenericName_Module() As Task
            Dim code =
<Code>
Module M$$
End Module
</Code>

            Await TestGenericNameExtender_GetBaseGenericName(code, 1, "Object")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGenericExtender_GetImplementedTypesCount_Module() As Task
            Dim code =
<Code>
Module M$$
End Module
</Code>

            Await TestGenericNameExtender_GetImplementedTypesCountThrows(Of ArgumentException)(code)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGenericExtender_GetImplTypeGenericName_Module() As Task
            Dim code =
<Code>
Module M$$
End Module
</Code>

            Await TestGenericNameExtender_GetImplTypeGenericNameThrows(Of ArgumentException)(code, 1)
        End Function

#End Region

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetBaseName1() As Task
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

            Await TestGetBaseName(code, "N.M.Generic(Of String)")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestExternalClass_ImplementedInterfaces() As Task
            Dim code =
<Code>
Class $$Foo
    Inherits System.Collections.Generic.List(Of Integer)
End Class
</Code>

            Await TestElement(code,
                Sub(codeClass)
                    Dim listType = TryCast(codeClass.Bases.Item(1), EnvDTE80.CodeClass2)
                    Assert.NotNull(listType)

                    Assert.Equal(8, listType.ImplementedInterfaces.Count)
                End Sub)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestClassMembersForWithEventsField() As Task
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

            Await TestElement(code,
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
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestClassIncludedDeclareMethods() As Task
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

            Await TestElement(code,
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
        End Function

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
