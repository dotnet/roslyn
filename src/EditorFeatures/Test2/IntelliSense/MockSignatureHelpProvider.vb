' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.SignatureHelp
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    Friend Class MockSignatureHelpProvider
        Implements ISignatureHelpProvider

        Private _provideSignatures As Action(Of SignatureContext)

        Public Sub New(provideSignatures As Action(Of SignatureContext))
            Me._provideSignatures = provideSignatures
        End Sub

        Public Sub New(items As IList(Of SignatureHelpItem))
            Me._provideSignatures = CreateProvideSignaturesAction(items)
        End Sub

        Public Sub ResetProvideSignaturesAction(provideSignatures As Action(Of SignatureContext))
            Me._provideSignatures = provideSignatures
        End Sub

        Private Shared Function CreateProvideSignaturesAction(items As IList(Of SignatureHelpItem)) As Action(Of SignatureContext)
            Return Sub(context)
                       context.AddItems(items)
                       context.SetApplicableSpan(New TextSpan(context.Position, 0))
                       context.SetState(New SignatureHelpState(argumentIndex:=0, argumentCount:=0, argumentName:=Nothing, argumentNames:=Nothing))
                   End Sub
        End Function

        Public Property GetItemsCount As Integer

        Public Function ProvideSignaturesAsync(context As SignatureContext) As Task Implements ISignatureHelpProvider.ProvideSignaturesAsync
            Trace.WriteLine("MockSignatureHelpProvider.GetItemsAsync")

            GetItemsCount += 1
            _provideSignatures(context)

            Return SpecializedTasks.EmptyTask
        End Function

        Public Function IsTriggerCharacter(ch As Char) As Boolean Implements ISignatureHelpProvider.IsTriggerCharacter
            Return ch = "("c
        End Function

        Public Function IsRetriggerCharacter(ch As Char) As Boolean Implements ISignatureHelpProvider.IsRetriggerCharacter
            Return ch = ")"c
        End Function
    End Class
End Namespace