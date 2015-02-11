' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.VisualBasic
    Public Class CodeAccessorFunctionTests
        Inherits AbstractCodeFunctionTests

#Region "Access tests"

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access1()
            Dim code =
    <Code>
Class C
    Public Property P As Integer
        Get
            Return 0
        End Get
        $$Set(value As Integer)

        End Set
    End Property
End Class
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access2()
            Dim code =
    <Code>
Class C
    Public Property P As Integer
        Get
            Return 0
        End Get
        Private $$Set(value As Integer)

        End Set
    End Property
End Class
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPrivate)
        End Sub

#End Region

#Region "FunctionKind tests"

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub FunctionKind_AddHandler()
            Dim code =
<Code>
Imports System

Public Class C1

   Public Custom Event E1 As EventHandler

      $$AddHandler(ByVal value As EventHandler)
      End AddHandler

      RemoveHandler(ByVal value As EventHandler)
      End RemoveHandler

      RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
      End RaiseEvent

   End Event

End Clas
</Code>

            TestFunctionKind(code, EnvDTE80.vsCMFunction2.vsCMFunctionAddHandler)
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub FunctionKind_RemoveHandler()
            Dim code =
<Code>
Imports System

Public Class C1

   Public Custom Event E1 As EventHandler

      AddHandler(ByVal value As EventHandler)
      End AddHandler

      $$RemoveHandler(ByVal value As EventHandler)
      End RemoveHandler

      RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
      End RaiseEvent

   End Event

End Clas
</Code>

            TestFunctionKind(code, EnvDTE80.vsCMFunction2.vsCMFunctionRemoveHandler)
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub FunctionKind_RaiseEvent()
            Dim code =
<Code>
Imports System

Public Class C1

   Public Custom Event E1 As EventHandler

      AddHandler(ByVal value As EventHandler)
      End AddHandler

      RemoveHandler(ByVal value As EventHandler)
      End RemoveHandler

      $$RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
      End RaiseEvent

   End Event

End Clas
</Code>

            TestFunctionKind(code, EnvDTE80.vsCMFunction2.vsCMFunctionRaiseEvent)
        End Sub

#End Region

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property
    End Class
End Namespace


