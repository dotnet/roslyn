' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Helper methods that exist to share code between properties and events.
    ''' </summary>
    Friend Class PEPropertyOrEventHelpers
        Friend Shared Function GetPropertiesForExplicitlyImplementedAccessor(accessor As MethodSymbol) As ISet(Of PropertySymbol)
            Return GetSymbolsForExplicitlyImplementedAccessor(Of PropertySymbol)(accessor)
        End Function

        Friend Shared Function GetEventsForExplicitlyImplementedAccessor(accessor As MethodSymbol) As ISet(Of EventSymbol)
            Return GetSymbolsForExplicitlyImplementedAccessor(Of EventSymbol)(accessor)
        End Function

        ' CONSIDER: the 99% case is a very small set.  A list might be more efficient in such cases.
        Private Shared Function GetSymbolsForExplicitlyImplementedAccessor(Of T As Symbol)(accessor As MethodSymbol) As ISet(Of T)
            If accessor Is Nothing Then
                Return SpecializedCollections.EmptySet(Of T)()
            End If

            Dim implementedAccessors As ImmutableArray(Of MethodSymbol) = accessor.ExplicitInterfaceImplementations
            If implementedAccessors.Length = 0 Then
                Return SpecializedCollections.EmptySet(Of T)()
            End If

            Dim symbolsForExplicitlyImplementedAccessors = New HashSet(Of T)()
            For Each implementedAccessor In implementedAccessors
                Dim associatedProperty = TryCast(implementedAccessor.AssociatedSymbol, T)
                If associatedProperty IsNot Nothing Then
                    symbolsForExplicitlyImplementedAccessors.Add(associatedProperty)
                End If

            Next

            Return symbolsForExplicitlyImplementedAccessors
        End Function

        ' Properties and events from metadata do not have explicit accessibility. Instead,
        ' the accessibility reported for the PEPropertySymbol or PEEventSymbol is the most
        ' restrictive level that is no more restrictive than the getter/adder and setter/remover.
        Friend Shared Function GetDeclaredAccessibilityFromAccessors(accessor1 As MethodSymbol, accessor2 As MethodSymbol) As Accessibility
            If accessor1 Is Nothing Then
                Return If((accessor2 Is Nothing), Accessibility.NotApplicable, accessor2.DeclaredAccessibility)
            ElseIf accessor2 Is Nothing Then
                Return accessor1.DeclaredAccessibility
            End If

            Dim accessibility1 = accessor1.DeclaredAccessibility
            Dim accessibility2 = accessor2.DeclaredAccessibility
            Dim minAccessibility = If((accessibility1 > accessibility2), accessibility2, accessibility1)
            Dim maxAccessibility = If((accessibility1 > accessibility2), accessibility1, accessibility2)
            Return If(((minAccessibility = Accessibility.[Protected]) AndAlso (maxAccessibility = Accessibility.Friend)), Accessibility.ProtectedOrFriend, maxAccessibility)
        End Function

    End Class
End Namespace

