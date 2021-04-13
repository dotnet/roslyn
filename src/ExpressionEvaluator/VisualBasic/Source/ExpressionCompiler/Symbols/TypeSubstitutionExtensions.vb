' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    Friend Module TypeSubstitutionExtensions
        <Extension>
        Friend Function SubstituteType(typeMap As TypeSubstitution, type As TypeSymbol) As TypeSymbol
            Return type.InternalSubstituteTypeParameters(typeMap).Type
        End Function

        <Extension>
        Friend Function SubstituteNamedType(typeMap As TypeSubstitution, type As NamedTypeSymbol) As NamedTypeSymbol
            Return DirectCast(type.InternalSubstituteTypeParameters(typeMap).AsTypeSymbolOnly(), NamedTypeSymbol)
        End Function
    End Module

End Namespace
