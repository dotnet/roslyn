' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic

Namespace System.Runtime.Analyzers
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class BasicCA1003DiagnosticAnalyzer
        Inherits UseGenericEventHandler

        Protected Overrides Function GetAnalyzer(
            compilation As Compilation,
            eventHandler As INamedTypeSymbol,
            genericEventHandler As INamedTypeSymbol,
            eventArgs As INamedTypeSymbol,
            comSourceInterfacesAttribute As INamedTypeSymbol) As AnalyzerBase

            Return New BasicAnalyzer(compilation, eventHandler, genericEventHandler, eventArgs, comSourceInterfacesAttribute)
        End Function

        Private NotInheritable Class BasicAnalyzer
            Inherits AnalyzerBase

            Public Sub New(
                compilation As Compilation,
                eventHandler As INamedTypeSymbol,
                genericEventHandler As INamedTypeSymbol,
                eventArgs As INamedTypeSymbol,
                comSourceInterfacesAttribute As INamedTypeSymbol)

                MyBase.New(compilation, eventHandler, genericEventHandler, eventArgs, comSourceInterfacesAttribute)
            End Sub

            Protected Overrides Function IsViolatingEventHandler(type As INamedTypeSymbol) As Boolean
                Return IsGenericEventHandlerInstance(type) AndAlso Not IsEventArgs(type.TypeArguments(0)) OrElse
                    IsEventHandler(type) AndAlso Not IsValidLibraryEventHandlerInstance(type)
            End Function

            Protected Overrides Function IsAssignableTo(compilation As Compilation, fromSymbol As ITypeSymbol, toSymbol As ITypeSymbol) As Boolean
                Return fromSymbol IsNot Nothing AndAlso toSymbol IsNot Nothing AndAlso DirectCast(compilation, VisualBasicCompilation).ClassifyConversion(fromSymbol, toSymbol).IsWidening
            End Function

            Private Function IsEventHandler(type As ITypeSymbol) As Boolean
                Dim delegateInvokeMethod = GetDelegateInvokeMethod(type)
                If delegateInvokeMethod Is Nothing Then
                    Return False
                End If

                Dim parameters = delegateInvokeMethod.Parameters
                Return delegateInvokeMethod.ReturnsVoid AndAlso
                    parameters.Length = 2 AndAlso
                    parameters(0).Type.SpecialType = SpecialType.System_Object AndAlso
                    IsEventArgs(parameters(1).Type)
            End Function

            Private Shared Function GetDelegateInvokeMethod(type As ITypeSymbol) As IMethodSymbol
                Return If(type.TypeKind = TypeKind.Delegate,
                    type.GetMembers(WellKnownMemberNames.DelegateInvokeName).OfType(Of IMethodSymbol)().FirstOrDefault(),
                    Nothing)
            End Function
        End Class
    End Class
End Namespace
