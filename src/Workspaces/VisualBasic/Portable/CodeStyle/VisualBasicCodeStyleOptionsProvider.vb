' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Options.Providers

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeStyle
    <ExportOptionProvider, [Shared]>
    Friend NotInheritable Class VisualBasicCodeStyleOptionsProvider
        Implements IOptionProvider

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Public ReadOnly Property Options As ImmutableArray(Of IOption) Implements IOptionProvider.Options
            Get
                Return VisualBasicCodeStyleOptions.AllOptions
            End Get
        End Property
    End Class
End Namespace
