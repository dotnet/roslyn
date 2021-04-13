' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend Class Symbol

        ''' <summary>
        ''' Determine if two methods have the same signature according to section 4.1.1 of the VB language spec.
        ''' The name, number of type parameters, and number and types of the method's non-optional parameters are
        ''' considered. ByRef/Byval, parameter names, returns type, constraints, or optional parameters are not considered.
        ''' </summary>
        Friend Shared Function HaveSameSignature(method1 As MethodSymbol, method2 As MethodSymbol) As Boolean
            Dim comparisonResults As SymbolComparisonResults = MethodSignatureComparer.DetailedCompare(
                method1,
                method2,
                SymbolComparisonResults.AllMismatches And Not SymbolComparisonResults.MismatchesForConflictingMethods)
            Return comparisonResults = 0
        End Function

        Friend Shared Function HaveSameSignatureAndConstraintsAndReturnType(method1 As MethodSymbol, method2 As MethodSymbol) As Boolean
            Return MethodSignatureComparer.VisualBasicSignatureAndConstraintsAndReturnTypeComparer.Equals(method1, method2)
        End Function

        ''' <summary>
        ''' Checks if <paramref name="symbol"/> is accessible from within type <paramref name="within"/>.  
        ''' </summary>
        ''' <param name="symbol">The symbol for the accessibility check.</param>
        ''' <param name="within">The type to use as a context for the check.</param>
        ''' <param name="throughTypeOpt">
        ''' The type of an expression that <paramref name="symbol"/> is accessed off of, if any.
        ''' This is needed to properly check accessibility of protected members.
        ''' </param>
        ''' <returns></returns>
        Public Shared Function IsSymbolAccessible(symbol As Symbol,
                                                  within As NamedTypeSymbol,
                                                  Optional throughTypeOpt As NamedTypeSymbol = Nothing) As Boolean
            If symbol Is Nothing Then
                Throw New ArgumentNullException(NameOf(symbol))
            End If

            If within Is Nothing Then
                Throw New ArgumentNullException(NameOf(within))
            End If

            Return AccessCheck.IsSymbolAccessible(symbol, within, throughTypeOpt, useSiteInfo:=CompoundUseSiteInfo(Of AssemblySymbol).Discarded)
        End Function

        ''' <summary>
        ''' Checks if <paramref name="symbol"/> is accessible from within the assembly <paramref name="within"/>', but outside any 
        ''' type. Protected members are deemed inaccessible.
        ''' </summary>
        ''' <param name="symbol">The symbol to check accessibility.</param>
        ''' <param name="within">The assembly to check accessibility within.</param>
        ''' <returns>True if symbol is accessible. False otherwise.</returns>
        Public Shared Function IsSymbolAccessible(symbol As Symbol,
                                                  within As AssemblySymbol) As Boolean
            If symbol Is Nothing Then
                Throw New ArgumentNullException(NameOf(symbol))
            End If

            If within Is Nothing Then
                Throw New ArgumentNullException(NameOf(within))
            End If

            Return AccessCheck.IsSymbolAccessible(symbol, within, useSiteInfo:=CompoundUseSiteInfo(Of AssemblySymbol).Discarded)
        End Function

    End Class
End Namespace
