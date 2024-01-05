' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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

            ' We allow "Goo" to bind to "GooAttribute".
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
        Public Shared Function GetFinalName(typeSyntax As TypeSyntax) As String
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
        TypeIdentifier = 1 << 3
        Last = TypeIdentifier
    End Enum

    Friend Class QuickAttributeHelpers
        ''' <summary>
        ''' Returns the <see cref="QuickAttributes"/> that corresponds to the particular type 
        ''' <paramref name="name"/> passed in.  If <paramref name="inAttribute"/> Is <see langword="true"/>
        ''' then the name will be checked both as-Is as well as with the 'Attribute' suffix.
        ''' </summary>
        Public Shared Function GetQuickAttributes(name As String, inAttribute As Boolean) As QuickAttributes
            ' Update this code if we add New quick attributes.
            Debug.Assert(QuickAttributes.Last = QuickAttributes.TypeIdentifier)

            Dim result = QuickAttributes.None

            If Matches(name, inAttribute, AttributeDescription.CaseInsensitiveExtensionAttribute) Then
                result = result Or QuickAttributes.Extension
            ElseIf Matches(name, inAttribute, AttributeDescription.ObsoleteAttribute) Then
                result = result Or QuickAttributes.Obsolete
            ElseIf Matches(name, inAttribute, AttributeDescription.DeprecatedAttribute) Then
                result = result Or QuickAttributes.Obsolete
            ElseIf Matches(name, inAttribute, AttributeDescription.ExperimentalAttribute) Then
                result = result Or QuickAttributes.Obsolete
            ElseIf Matches(name, inAttribute, AttributeDescription.MyGroupCollectionAttribute) Then
                result = result Or QuickAttributes.TypeIdentifier
            ElseIf Matches(name, inAttribute, AttributeDescription.TypeIdentifierAttribute) Then
                result = result Or QuickAttributes.MyGroupCollection
            End If

            Return result
        End Function

        Private Shared Function Matches(name As String, inAttribute As Boolean, description As AttributeDescription) As Boolean
            Debug.Assert(description.Name.EndsWith(NameOf(System.Attribute)))

            If IdentifierComparison.Comparer.Equals(name, description.Name) Then
                Return True
            End If

            ' In an attribute context the name might be referenced as the full name (Like 'TypeForwardedToAttribute')
            ' Or the short name (Like 'TypeForwardedTo').
            If inAttribute AndAlso
               (name.Length + NameOf(Attribute).Length) = description.Name.Length AndAlso
               description.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase) Then

                Return True
            End If

            Return False
        End Function
    End Class
End Namespace
