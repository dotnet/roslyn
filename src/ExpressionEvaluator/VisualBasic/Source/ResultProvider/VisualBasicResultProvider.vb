' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.VisualStudio.Debugger.ComponentInterfaces
Imports Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation
Imports Type = Microsoft.VisualStudio.Debugger.Metadata.Type

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    ''' <summary>
    ''' Computes expansion of <see cref="DkmClrValue"/> instances.
    ''' </summary>
    ''' <remarks>
    ''' This class provides implementation for the Visual Basic ResultProvider component.
    ''' </remarks>
    <DkmReportNonFatalWatsonException(ExcludeExceptionType:=GetType(NotImplementedException)), DkmContinueCorruptingException>
    Friend NotInheritable Class VisualBasicResultProvider
        Inherits ResultProvider

        Public Sub New()
            MyClass.New(New VisualBasicFormatter())
        End Sub

        Private Sub New(formatter As VisualBasicFormatter)
            MyClass.New(formatter, formatter)
        End Sub

        Friend Sub New(formatter2 As IDkmClrFormatter2, fullNameProvider As IDkmClrFullNameProvider)
            MyBase.New(formatter2, fullNameProvider)
        End Sub

        Friend Overrides ReadOnly Property StaticMembersString As String
            Get
                Return Resources.SharedMembers
            End Get
        End Property

        Friend Overrides Function IsPrimitiveType(type As Type) As Boolean
            Return type.IsPredefinedType()
        End Function

        Friend Overrides Function TryGetGeneratedMemberDisplay(metadataName As String, <Out> ByRef displayName As String) As Boolean
            displayName = Nothing
            Return False
        End Function
    End Class
End Namespace
