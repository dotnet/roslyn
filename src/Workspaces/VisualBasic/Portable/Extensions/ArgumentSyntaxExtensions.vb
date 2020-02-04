' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Partial Friend Module ArgumentSyntaxExtensions
        <Extension()>
        Public Function DetermineType(argument As ArgumentSyntax,
                                      semanticModel As SemanticModel,
                                      cancellationToken As CancellationToken) As ITypeSymbol
            ' If a parameter appears to have a void return type, then just use 'object' instead.
            Return argument.GetArgumentExpression().DetermineType(semanticModel, cancellationToken)
        End Function
    End Module
End Namespace
