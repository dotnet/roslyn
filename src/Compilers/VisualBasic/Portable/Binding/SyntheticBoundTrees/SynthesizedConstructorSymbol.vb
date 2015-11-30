' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend Partial Class SynthesizedConstructorSymbol
        Inherits SynthesizedConstructorBase

        Friend Overrides Function GetBoundMethodBody(diagnostics As DiagnosticBag, Optional ByRef methodBodyBinder As Binder = Nothing) As BoundBlock
            methodBodyBinder = Nothing
            Dim returnStmt = New BoundReturnStatement(Syntax, Nothing, Nothing, Nothing)
            returnStmt.SetWasCompilerGenerated()
            Return New BoundBlock(Syntax, Nothing, ImmutableArray(Of LocalSymbol).Empty, ImmutableArray.Create(Of BoundStatement)(returnStmt))
        End Function

    End Class

End Namespace
