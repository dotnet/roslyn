' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    <ExportArgumentProvider(NameOf(ContextVariableArgumentProvider), LanguageNames.VisualBasic)>
    <ExtensionOrder(After:=NameOf(FirstBuiltInArgumentProvider))>
    <[Shared]>
    Friend Class ContextVariableArgumentProvider
        Inherits ArgumentProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Overrides Async Function ProvideArgumentAsync(context As ArgumentContext) As Task
            If context.PreviousValue IsNot Nothing Then
                Return
            End If

            Dim semanticModel = Await context.Document.GetRequiredSemanticModelAsync(context.CancellationToken).ConfigureAwait(False)
            Dim symbols = semanticModel.LookupSymbols(context.Position, name:=context.Parameter.Name)
            For Each symbol In symbols
                If SymbolEqualityComparer.Default.Equals(context.Parameter.Type, symbol.GetSymbolType()) Then
                    context.DefaultValue = context.Parameter.Name
                    Return
                End If
            Next
        End Function
    End Class
End Namespace
