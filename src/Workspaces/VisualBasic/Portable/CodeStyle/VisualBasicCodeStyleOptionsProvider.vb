' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Options.Providers

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeStyle
    <ExportOptionProvider(LanguageNames.VisualBasic), [Shared]>
    Friend NotInheritable Class VisualBasicCodeStyleOptionsProvider
        Implements IOptionProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public ReadOnly Property Options As ImmutableArray(Of IOption) Implements IOptionProvider.Options
            Get
                Return VisualBasicCodeStyleOptions.AllOptions.As(Of IOption)
            End Get
        End Property
    End Class
End Namespace
