' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' Groups the information computed by MakeOverriddenMembers.
    ''' </summary>
    ''' <remarks>
    ''' In C# this class also stores hidden members (and is named OverriddenOrHiddenMembersResult). 
    ''' The way VB warns on hidden members, this did not turn out to be useful.
    ''' </remarks>
    ''' <typeparam name="TSymbol">Expected to be a member symbol type (e.g. method, property).</typeparam>
    Friend NotInheritable Class OverriddenMembersResult(Of TSymbol As Symbol)

        Public Shared ReadOnly Empty As OverriddenMembersResult(Of TSymbol) =
            New OverriddenMembersResult(Of TSymbol)(ImmutableArray(Of TSymbol).Empty,
                                                    ImmutableArray(Of TSymbol).Empty,
                                                    ImmutableArray(Of TSymbol).Empty)

        Private ReadOnly _overriddenMembers As ImmutableArray(Of TSymbol)
        Private ReadOnly _inexactOverriddenMembers As ImmutableArray(Of TSymbol)
        Private ReadOnly _inaccessibleMembers As ImmutableArray(Of TSymbol)

        ''' <summary>
        ''' The member(s) that are potentially being overridden. This collection only contains 
        ''' candidates having signature 'exactly' matching the signature of the method/property. 
        ''' 
        ''' 'Exact' signature match is defined as 'general' signature match plus NO
        ''' mismatches in total number of parameters or optional parameter types.
        ''' 
        ''' See comments on InaccessibleMembers for more details on 'general' signature match.
        ''' </summary>
        Public ReadOnly Property OverriddenMembers As ImmutableArray(Of TSymbol)
            Get
                Return _overriddenMembers
            End Get
        End Property

        ''' <summary>
        ''' The member(s) that are potentially being overridden. This collection only contains 
        ''' candidates having signature 'generally' matching the signature of the method/property. 
        ''' 
        ''' Two signatures 'generally' match if DetailedSignatureCompare (...) returns no 
        ''' mismatches defined in SymbolComparisonResults.AllMismatches ignoring mismatches 
        ''' grouped in SymbolComparisonResults.MismatchesForConflictingMethods.
        ''' </summary>
        Public ReadOnly Property InexactOverriddenMembers As ImmutableArray(Of TSymbol)
            Get
                Return _inexactOverriddenMembers
            End Get
        End Property

        ''' <summary>
        ''' Members that would be in OverriddenMembers if they were accessible.
        ''' </summary>
        Public ReadOnly Property InaccessibleMembers As ImmutableArray(Of TSymbol)
            Get
                Return _inaccessibleMembers
            End Get
        End Property

        Private Sub New(overriddenMembers As ImmutableArray(Of TSymbol),
                        inexactOverriddenMembers As ImmutableArray(Of TSymbol),
                        inaccessibleMembers As ImmutableArray(Of TSymbol))
            Me._overriddenMembers = overriddenMembers
            Me._inexactOverriddenMembers = inexactOverriddenMembers
            Me._inaccessibleMembers = inaccessibleMembers
        End Sub

        Public Shared Function Create(overriddenMembers As ImmutableArray(Of TSymbol),
                                      inexactOverriddenMembers As ImmutableArray(Of TSymbol),
                                      inaccessibleMembers As ImmutableArray(Of TSymbol)) As OverriddenMembersResult(Of TSymbol)
            If overriddenMembers.IsEmpty AndAlso inexactOverriddenMembers.IsEmpty AndAlso inaccessibleMembers.IsEmpty Then
                Return Empty
            Else
                Return New OverriddenMembersResult(Of TSymbol)(overriddenMembers, inexactOverriddenMembers, inaccessibleMembers)
            End If
        End Function

        Public Shared Function GetOverriddenMember(substitutedOverridingMember As TSymbol, overriddenByDefinitionMember As TSymbol) As TSymbol
            Debug.Assert(Not substitutedOverridingMember.IsDefinition)

            If overriddenByDefinitionMember IsNot Nothing Then
                Dim overriddenByDefinitionContaining As NamedTypeSymbol = overriddenByDefinitionMember.ContainingType
                Dim overriddenByDefinitionContainingTypeDefinition As NamedTypeSymbol = overriddenByDefinitionContaining.OriginalDefinition
                Dim baseType As NamedTypeSymbol = substitutedOverridingMember.ContainingType.BaseTypeNoUseSiteDiagnostics
                While baseType IsNot Nothing
                    If baseType.OriginalDefinition = overriddenByDefinitionContainingTypeDefinition Then
                        If baseType = overriddenByDefinitionContaining Then
                            Return overriddenByDefinitionMember
                        End If

                        Return DirectCast(overriddenByDefinitionMember.OriginalDefinition.AsMember(baseType), TSymbol)
                    End If

                    baseType = baseType.BaseTypeNoUseSiteDiagnostics
                End While

                Throw ExceptionUtilities.Unreachable
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' It Is Not suitable to call this method on a <see cref="OverriddenMembersResult"/> object
        ''' associated with a member within substituted type, <see cref="GetOverriddenMember(TSymbol, TSymbol)"/>
        ''' should be used instead.
        ''' </summary>
        Public ReadOnly Property OverriddenMember As TSymbol
            Get
                For Each overridden In OverriddenMembers
                    If overridden.IsMustOverride OrElse overridden.IsOverridable OrElse overridden.IsOverrides Then
                        Return overridden
                    End If
                Next

                Return Nothing
            End Get
        End Property
    End Class
End Namespace

