' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Linq
Imports System.Threading
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ' An invariant of a merged type declaration is that all of its children are also merged
    ' declarations.
    Friend NotInheritable Class MergedTypeDeclaration
        Inherits MergedNamespaceOrTypeDeclaration

        Private _declarations As ImmutableArray(Of SingleTypeDeclaration)
        Private _children As MergedTypeDeclaration()
        Private _memberNames As ICollection(Of String)

        Public Property Declarations As ImmutableArray(Of SingleTypeDeclaration)
            Get
                Return _declarations
            End Get
            Private Set(value As ImmutableArray(Of SingleTypeDeclaration))
                _declarations = value
            End Set
        End Property

        Friend Sub New(declarations As ImmutableArray(Of SingleTypeDeclaration))
            MyBase.New(SingleNamespaceOrTypeDeclaration.BestName(Of SingleTypeDeclaration)(declarations))
            Me.Declarations = declarations
        End Sub

        Public ReadOnly Property SyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get

                Dim builder = ArrayBuilder(Of SyntaxReference).GetInstance()

                For Each decl In Declarations
                    Dim syn = decl.SyntaxReference
                    builder.Add(syn)
                Next

                Return builder.ToImmutableAndFree()
            End Get
        End Property

        Public Overrides ReadOnly Property Kind As DeclarationKind
            Get
                Return Me.Declarations(0).Kind
            End Get
        End Property

        Public ReadOnly Property Arity As Integer
            Get
                Return Me.Declarations(0).Arity
            End Get
        End Property

        Public Function GetAttributeDeclarations(Optional quickAttributes As QuickAttributes? = Nothing) As ImmutableArray(Of SyntaxList(Of AttributeListSyntax))
            Dim attributeSyntaxBuilder = ArrayBuilder(Of SyntaxList(Of AttributeListSyntax)).GetInstance()

            For Each decl In Declarations
                If Not decl.HasAnyAttributes Then
                    Continue For
                End If

                ' if caller is asking for particular quick attributes, don't bother going to syntax
                ' unless the type actually could expose that attribute.
                If quickAttributes IsNot Nothing AndAlso
                   (decl.QuickAttributes And quickAttributes.Value) <> 0 Then
                    Continue For
                End If

                Dim syntaxRef = decl.SyntaxReference
                Dim node = syntaxRef.GetSyntax()
                Dim attributeSyntaxList As SyntaxList(Of AttributeListSyntax)

                Select Case node.Kind
                    Case SyntaxKind.ClassBlock,
                         SyntaxKind.ModuleBlock,
                         SyntaxKind.StructureBlock,
                         SyntaxKind.InterfaceBlock
                        attributeSyntaxList = DirectCast(node, TypeBlockSyntax).BlockStatement.AttributeLists

                    Case SyntaxKind.DelegateFunctionStatement,
                         SyntaxKind.DelegateSubStatement
                        attributeSyntaxList = DirectCast(node, DelegateStatementSyntax).AttributeLists

                    Case SyntaxKind.EnumBlock
                        attributeSyntaxList = DirectCast(node, EnumBlockSyntax).EnumStatement.AttributeLists

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(node.Kind)
                End Select

                attributeSyntaxBuilder.Add(attributeSyntaxList)
            Next

            Return attributeSyntaxBuilder.ToImmutableAndFree()
        End Function

        Public Function GetLexicalSortKey(compilation As VisualBasicCompilation) As LexicalSortKey
            ' Return first sort key from all declarations.
            Dim sortKey As LexicalSortKey = New LexicalSortKey(_declarations(0).NameLocation, compilation)
            For i = 1 To _declarations.Length - 1
                sortKey = LexicalSortKey.First(sortKey, New LexicalSortKey(_declarations(i).NameLocation, compilation))
            Next
            Return sortKey
        End Function

        Public ReadOnly Property NameLocations As ImmutableArray(Of Location)
            Get
                If Declarations.Length = 1 Then
                    Return ImmutableArray.Create(Declarations(0).NameLocation)
                End If

                Dim builder = ArrayBuilder(Of Location).GetInstance()

                For Each decl In Declarations
                    Dim loc = decl.NameLocation
                    If loc IsNot Nothing Then
                        builder.Add(loc)
                    End If
                Next

                Return builder.ToImmutableAndFree()
            End Get
        End Property

        Private Shared ReadOnly s_identityFunc As Func(Of SingleTypeDeclaration, SingleTypeDeclaration) =
            Function(t) t

        Private Shared ReadOnly s_mergeFunc As Func(Of IEnumerable(Of SingleTypeDeclaration), MergedTypeDeclaration) =
            Function(g) New MergedTypeDeclaration(ImmutableArray.CreateRange(Of SingleTypeDeclaration)(g))

        Private Function MakeChildren() As MergedTypeDeclaration()
            Dim allSingleTypeDecls As IEnumerable(Of SingleTypeDeclaration)

            If Declarations.Length = 1 Then
                allSingleTypeDecls = Declarations(0).Children.OfType(Of SingleTypeDeclaration)()
            Else
                allSingleTypeDecls = Declarations.SelectMany(Function(d) d.Children.OfType(Of SingleTypeDeclaration)())
            End If

            Return MakeMergedTypes(allSingleTypeDecls).ToArray()
        End Function

        Friend Shared Function MakeMergedTypes(types As IEnumerable(Of SingleTypeDeclaration)) As IEnumerable(Of MergedTypeDeclaration)
            Return types.
                GroupBy(s_identityFunc, SingleTypeDeclaration.EqualityComparer).
                Select(s_mergeFunc)
        End Function

        Public Overloads ReadOnly Property Children As ImmutableArray(Of MergedTypeDeclaration)
            Get
                If Me._children Is Nothing Then
                    Interlocked.CompareExchange(Me._children, MakeChildren(), Nothing)
                End If
                Return Me._children.AsImmutableOrNull()
            End Get
        End Property

        Protected Overrides Function GetDeclarationChildren() As ImmutableArray(Of Declaration)
            Return StaticCast(Of Declaration).From(Me.Children)
        End Function

        Public ReadOnly Property MemberNames As ICollection(Of String)
            Get
                If _memberNames Is Nothing Then
                    Dim names = UnionCollection(Of String).Create(Me.Declarations, Function(d) d.MemberNames)
                    Interlocked.CompareExchange(_memberNames, names, Nothing)
                End If

                Return _memberNames
            End Get
        End Property

        Public ReadOnly Property AnyMemberHasAttributes As Boolean
            Get
                For Each decl In Me.Declarations
                    If decl.AnyMemberHasAttributes Then
                        Return True
                    End If
                Next
                Return False
            End Get
        End Property

    End Class
End Namespace
