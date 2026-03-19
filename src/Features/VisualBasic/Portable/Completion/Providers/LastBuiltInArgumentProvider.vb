' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    ''' <summary>
    ''' Provides an argument provider that always appears after all built-in argument providers. This argument
    ''' provider does not provide any argument values.
    ''' </summary>
    <ExportArgumentProvider(NameOf(LastBuiltInArgumentProvider), LanguageNames.VisualBasic)>
    <ExtensionOrder(After:=NameOf(DefaultArgumentProvider))>
    <[Shared]>
    Friend Class LastBuiltInArgumentProvider
        Inherits ArgumentProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Overrides Function ProvideArgumentAsync(context As ArgumentContext) As Task
            Return Task.CompletedTask
        End Function
    End Class
End Namespace
