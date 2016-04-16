' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Collections.ObjectModel
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Reflection.Metadata

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend NotInheritable Class UnsupportedMetadataTypeSymbol
        Inherits ErrorTypeSymbol

        Private ReadOnly _mrEx As BadImageFormatException

        Public Sub New(Optional mrEx As BadImageFormatException = Nothing)
            _mrEx = mrEx
        End Sub

        Public Sub New(explanation As String)
            ' TODO: Do we want to do anything with the "explanation". C# uses it in error messages
            ' TODO: and it seems worth no losing it in cases.
            ' TODO: If so, we need to go back and fill in cases that call the parameterless
            ' TODO: constructor.
        End Sub

        Friend Overrides ReadOnly Property MangleName As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property ErrorInfo As DiagnosticInfo
            Get
                Return ErrorFactory.ErrorInfo(ERRID.ERR_UnsupportedType1, String.Empty)
            End Get
        End Property
    End Class

End Namespace
