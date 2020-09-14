' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SourceGeneration
    Partial Friend Module VisualBasicCodeGenerator
        Private Function GenerateAttributeLists(attributes As ImmutableArray(Of AttributeData)) As SyntaxList(Of AttributeListSyntax)
            If attributes.IsDefaultOrEmpty Then
                Return Nothing
            End If

            Throw New NotImplementedException()
        End Function
    End Module
End Namespace
