﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' The QuickAttributeChecker applies a simple fast heuristic for determining probable
    ''' attributes without binding attribute types, just by looking at the final syntax of an 
    ''' attribute usage. It is accessed via the QuickAttributeChecker property on Binder.
    ''' </summary>
    ''' <remarks>
    ''' It works by maintaining a dictionary of all possible simple names that might map to a particular
    ''' attribute.
    ''' </remarks>
    Friend Class QuickAttributeChecker
        ' Dictionary mapping a name to quick attribute(s)
        Private ReadOnly _nameToAttributeMap As Dictionary(Of String, QuickAttributes)

        ' If true, can no longer add new names.
        Private _sealed As Boolean

        Public Sub New()
            _nameToAttributeMap = New Dictionary(Of String, QuickAttributes)(IdentifierComparison.Comparer)
        End Sub

        Public Sub New(other As QuickAttributeChecker)
            _nameToAttributeMap = New Dictionary(Of String, QuickAttributes)(other._nameToAttributeMap, IdentifierComparison.Comparer)
        End Sub

        ''' <summary>
        ''' Add a mapping from name to some attributes.
        ''' </summary>
        Public Sub AddName(name As String, newAttributes As QuickAttributes)
            Debug.Assert(Not _sealed)

            Dim current = QuickAttributes.None
            _nameToAttributeMap.TryGetValue(name, current)

            _nameToAttributeMap(name) = newAttributes Or current

            ' We allow "Foo" to bind to "FooAttribute".
            If name.EndsWith("Attribute", StringComparison.OrdinalIgnoreCase) Then
                _nameToAttributeMap(name.Substring(0, name.Length - "Attribute".Length)) = newAttributes Or current
            End If
        End Sub

        ''' <summary>
        ''' Process an alias clause and any imported mappings from it.
        ''' E.g., If you have an alias Ex=Blah.Extension, add any mapping for Extension to those for Ex.
        ''' Note that although, in VB, an alias cannot reference another alias, this code doesn't not attempt
        ''' to distinguish between aliases and regular names, as that would add complexity to the data structure
        ''' and would be unlikely to matter. This entire class is probabilistic anyone and is only used for quick
        ''' checks.
        ''' </summary>
        Public Sub AddAlias(aliasSyntax As SimpleImportsClauseSyntax)
            Debug.Assert(Not _sealed)
            Debug.Assert(aliasSyntax.Alias IsNot Nothing)

            Dim finalName = GetFinalName(aliasSyntax.Name)

            If finalName IsNot Nothing Then
                Dim current As QuickAttributes = QuickAttributes.None
                If _nameToAttributeMap.TryGetValue(finalName, current) Then
                    AddName(aliasSyntax.Alias.Identifier.ValueText, current)
                End If
            End If
        End Sub

        Public Sub Seal()
            _sealed = True
        End Sub

        ''' <summary>
        ''' Check attribute lists quickly to see what attributes might be referenced.
        ''' </summary>
        Public Function CheckAttributes(attributeLists As SyntaxList(Of AttributeListSyntax)) As QuickAttributes
            Debug.Assert(_sealed)

            Dim quickAttrs As QuickAttributes = QuickAttributes.None

            If attributeLists.Count > 0 Then
                For Each attrList In attributeLists
                    For Each attr In attrList.Attributes
                        quickAttrs = quickAttrs Or CheckAttribute(attr)
                    Next
                Next
            End If

            Return quickAttrs
        End Function

        Public Function CheckAttribute(attr As AttributeSyntax) As QuickAttributes
            Dim attrTypeSyntax = attr.Name
            Dim finalName = GetFinalName(attrTypeSyntax)
            If finalName IsNot Nothing Then
                Dim quickAttributes As QuickAttributes
                If _nameToAttributeMap.TryGetValue(finalName, quickAttributes) Then
                    Return quickAttributes
                End If
            End If

            Return QuickAttributes.None
        End Function

        ' Return the last name in a TypeSyntax, or Nothing if there isn't one.
        Private Function GetFinalName(typeSyntax As TypeSyntax) As String
            Dim node As VisualBasicSyntaxNode = typeSyntax
            Do
                Select Case node.Kind
                    Case SyntaxKind.IdentifierName
                        Return DirectCast(node, IdentifierNameSyntax).Identifier.ValueText
                    Case SyntaxKind.QualifiedName
                        node = DirectCast(node, QualifiedNameSyntax).Right
                    Case Else
                        Return Nothing
                End Select
            Loop
        End Function
    End Class

    ''' <summary>
    ''' Indicate which attributes might be present. Could be extended to other attributes 
    ''' if desired.
    ''' </summary>
    <Flags>
    Friend Enum QuickAttributes As Byte
        None = 0
        Extension = 1 << 0
        Obsolete = 1 << 1
        MyGroupCollection = 1 << 2
    End Enum
End Namespace
