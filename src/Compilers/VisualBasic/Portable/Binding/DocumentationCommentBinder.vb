' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Runtime.InteropServices

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' Binder used for interiors of documentation comment
    ''' </summary>
    Friend MustInherit Class DocumentationCommentBinder
        Inherits Binder

        Protected Sub New(containingBinder As Binder, commentedSymbol As Symbol)
            MyBase.New(containingBinder)

            CheckBinderSymbolRelationship(containingBinder, commentedSymbol)

            Me.CommentedSymbol = commentedSymbol
        End Sub

        ''' <summary>
        ''' Assuming there is one, the containing member of the binder is the commented symbol if and only if
        ''' the commented symbol is a non-delegate named type.  (Otherwise, it is the containing type or namespace of the commented symbol.)
        ''' </summary>
        ''' <remarks>
        ''' Delegates don't have user-defined members, so it makes more sense to treat
        ''' them like methods.
        ''' </remarks>
        <Conditional("DEBUG")>
        Private Shared Sub CheckBinderSymbolRelationship(containingBinder As Binder, commentedSymbol As Symbol)
            If commentedSymbol Is Nothing Then
                Return
            End If

            Dim commentedNamedType = TryCast(commentedSymbol, NamedTypeSymbol)
            Dim binderContainingMember As Symbol = containingBinder.ContainingMember
            If commentedNamedType IsNot Nothing AndAlso commentedNamedType.TypeKind <> TypeKind.Delegate Then
                Debug.Assert(binderContainingMember = commentedSymbol)
            ElseIf commentedSymbol.ContainingType IsNot Nothing Then
                Debug.Assert(TypeSymbol.Equals(DirectCast(binderContainingMember, TypeSymbol), commentedSymbol.ContainingType, TypeCompareKind.ConsiderEverything))
            Else
                ' It's not worth writing a complicated check that handles merged namespaces.
                Debug.Assert(binderContainingMember <> commentedSymbol)
                Debug.Assert(binderContainingMember.Kind = SymbolKind.Namespace)
            End If

        End Sub

        Friend Enum BinderType
            None
            Cref
            NameInTypeParamRef
            NameInTypeParam
            NameInParamOrParamRef
        End Enum

        Public Shared Function IsIntrinsicTypeForDocumentationComment(kind As SyntaxKind) As Boolean
            Select Case kind
                Case SyntaxKind.ShortKeyword,
                     SyntaxKind.UShortKeyword,
                     SyntaxKind.IntegerKeyword,
                     SyntaxKind.UIntegerKeyword,
                     SyntaxKind.LongKeyword,
                     SyntaxKind.ULongKeyword,
                     SyntaxKind.DecimalKeyword,
                     SyntaxKind.SingleKeyword,
                     SyntaxKind.DoubleKeyword,
                     SyntaxKind.SByteKeyword,
                     SyntaxKind.ByteKeyword,
                     SyntaxKind.BooleanKeyword,
                     SyntaxKind.CharKeyword,
                     SyntaxKind.DateKeyword,
                     SyntaxKind.StringKeyword
                    Return True

                Case Else
                    Return False
            End Select
        End Function

        Friend Shared Function GetBinderTypeForNameAttribute(node As BaseXmlAttributeSyntax) As DocumentationCommentBinder.BinderType
            Return GetBinderTypeForNameAttribute(GetParentXmlElementName(node))
        End Function

        Friend Shared Function GetBinderTypeForNameAttribute(parentNodeName As String) As DocumentationCommentBinder.BinderType
            If parentNodeName IsNot Nothing Then
                If DocumentationCommentXmlNames.ElementEquals(parentNodeName, DocumentationCommentXmlNames.ParameterElementName, True) OrElse
                        DocumentationCommentXmlNames.ElementEquals(parentNodeName, DocumentationCommentXmlNames.ParameterReferenceElementName, True) Then
                    Return DocumentationCommentBinder.BinderType.NameInParamOrParamRef

                ElseIf DocumentationCommentXmlNames.ElementEquals(parentNodeName, DocumentationCommentXmlNames.TypeParameterElementName, True) Then
                    Return DocumentationCommentBinder.BinderType.NameInTypeParam

                ElseIf DocumentationCommentXmlNames.ElementEquals(parentNodeName, DocumentationCommentXmlNames.TypeParameterReferenceElementName, True) Then
                    Return DocumentationCommentBinder.BinderType.NameInTypeParamRef
                End If
            End If

            Return DocumentationCommentBinder.BinderType.None
        End Function

        Friend Shared Function GetParentXmlElementName(attr As BaseXmlAttributeSyntax) As String
            Dim parent As VisualBasicSyntaxNode = attr.Parent
            If parent Is Nothing Then
                Return Nothing
            End If

            Select Case parent.Kind
                Case SyntaxKind.XmlEmptyElement
                    Dim element = DirectCast(parent, XmlEmptyElementSyntax)
                    If element.Name.Kind <> SyntaxKind.XmlName Then
                        Return Nothing
                    End If
                    Return DirectCast(element.Name, XmlNameSyntax).LocalName.ValueText

                Case SyntaxKind.XmlElementStartTag
                    Dim element = DirectCast(parent, XmlElementStartTagSyntax)
                    If element.Name.Kind <> SyntaxKind.XmlName Then
                        Return Nothing
                    End If
                    Return DirectCast(element.Name, XmlNameSyntax).LocalName.ValueText

            End Select

            Return Nothing
        End Function

        ''' <summary>
        ''' Symbol commented with the documentation comment handled by this binder. In general,
        ''' all name lookup is being performed in context of this symbol's containing symbol.
        ''' We still need this symbol, though, to be able to find type parameters or parameters
        ''' referenced from 'param', 'paramref', 'typeparam' and 'typeparamref' tags.
        ''' </summary>
        Protected ReadOnly CommentedSymbol As Symbol

        Friend Overrides Function BindXmlNameAttributeValue(identifier As IdentifierNameSyntax, <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)) As ImmutableArray(Of Symbol)
            Throw ExceptionUtilities.Unreachable
        End Function

        Friend Overrides Function BindInsideCrefAttributeValue(name As TypeSyntax, preserveAliases As Boolean, diagnosticBag As BindingDiagnosticBag, <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)) As ImmutableArray(Of Symbol)
            Throw ExceptionUtilities.Unreachable
        End Function

        Friend Overrides Function BindInsideCrefAttributeValue(reference As CrefReferenceSyntax, preserveAliases As Boolean, diagnosticBag As BindingDiagnosticBag, <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)) As ImmutableArray(Of Symbol)
            Throw ExceptionUtilities.Unreachable
        End Function

        Protected Shared Function FindSymbolInSymbolArray(Of TSymbol As Symbol)(
                                            name As String, symbols As ImmutableArray(Of TSymbol)) As ImmutableArray(Of Symbol)

            If Not symbols.IsEmpty Then
                For Each p In symbols
                    If IdentifierComparison.Equals(name, p.Name) Then
                        Return ImmutableArray.Create(Of Symbol)(p)
                    End If
                Next
            End If

            Return ImmutableArray(Of Symbol).Empty
        End Function

        Friend Overrides Function BinderSpecificLookupOptions(options As LookupOptions) As LookupOptions
            Return ContainingBinder.BinderSpecificLookupOptions(options) Or LookupOptions.UseBaseReferenceAccessibility
        End Function

        ''' <summary>
        ''' Removes from symbol collection overridden methods or properties
        ''' </summary>
        Protected Shared Sub RemoveOverriddenMethodsAndProperties(symbols As ArrayBuilder(Of Symbol))
            If symbols Is Nothing OrElse symbols.Count < 2 Then
                Return
            End If

            ' Do we have any method or property?
            Dim originalDef2Symbol As Dictionary(Of Symbol, Integer) = Nothing
            For i = 0 To symbols.Count - 1
                Dim sym As Symbol = symbols(i)
                Select Case sym.Kind
                    Case SymbolKind.Method, SymbolKind.Property
                        If originalDef2Symbol Is Nothing Then
                            originalDef2Symbol = New Dictionary(Of Symbol, Integer)()
                        End If
                        originalDef2Symbol.Add(sym.OriginalDefinition, i)
                End Select
            Next

            If originalDef2Symbol Is Nothing Then
                Return
            End If

            ' Do we need to remove any?
            Dim indices2remove As ArrayBuilder(Of Integer) = Nothing
            For i = 0 To symbols.Count - 1
                Dim index As Integer = -1
                Dim sym As Symbol = symbols(i)

                Select Case sym.Kind

                    Case SymbolKind.Method
                        ' Remove overridden methods
                        Dim method = DirectCast(sym.OriginalDefinition, MethodSymbol)
                        While True
                            method = method.OverriddenMethod
                            If method Is Nothing Then
                                Exit While
                            End If

                            If originalDef2Symbol.TryGetValue(method, index) Then
                                If indices2remove Is Nothing Then
                                    indices2remove = ArrayBuilder(Of Integer).GetInstance
                                End If
                                indices2remove.Add(index)
                            End If
                        End While

                    Case SymbolKind.Property
                        ' Remove overridden properties
                        Dim prop = DirectCast(sym.OriginalDefinition, PropertySymbol)
                        While True
                            prop = prop.OverriddenProperty
                            If prop Is Nothing Then
                                Exit While
                            End If

                            If originalDef2Symbol.TryGetValue(prop, index) Then
                                If indices2remove Is Nothing Then
                                    indices2remove = ArrayBuilder(Of Integer).GetInstance
                                End If
                                indices2remove.Add(index)
                            End If
                        End While

                End Select
            Next

            If indices2remove Is Nothing Then
                Return
            End If

            ' remove elements by indices from 'indices2remove'
            For i = 0 To indices2remove.Count - 1
                symbols(indices2remove(i)) = Nothing
            Next

            Dim target As Integer = 0
            For source = 0 To symbols.Count - 1
                Dim sym As Symbol = symbols(source)
                If sym IsNot Nothing Then
                    symbols(target) = sym
                    target += 1
                End If
            Next
            symbols.Clip(target)

            indices2remove.Free()
        End Sub

    End Class

End Namespace

