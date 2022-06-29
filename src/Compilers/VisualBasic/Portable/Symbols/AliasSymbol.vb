' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Linq
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' Symbol representing a using alias appearing in a compilation unit. 
    ''' Generally speaking, these symbols do not appear in the set of symbols reachable
    ''' from the unnamed namespace declaration.  In other words, when a using alias is used in a
    ''' program, it acts as a transparent alias, and the symbol to which it is an alias is used in
    ''' the symbol table.  For example, in the source code
    ''' <pre>
    ''' Imports o = System.Object
    ''' Namespace NS
    '''     partial class C : Inherits o : End Class
    '''     partial class C : Inherits Object : End Class
    '''     partial class C : Inherits System.Object : End Class
    ''' End Namespace
    ''' 
    ''' </pre>
    ''' all three declarations for class C are equivalent and result in the same symbol table object for C. 
    ''' However, these alias symbols do appear in the results of certain SemanticModel APIs. 
    ''' Specifically, for the base clause of the first of C's class declarations, the
    ''' following APIs may produce a result that contains an AliasSymbol:
    ''' <pre>
    '''     SemanticInfo SemanticModel.GetSemanticInfo(ExpressionSyntax expression);
    '''     SemanticInfo SemanticModel.BindExpression(SyntaxNode location, ExpressionSyntax expression);
    '''     SemanticInfo SemanticModel.BindType(SyntaxNode location, ExpressionSyntax type);
    '''     SemanticInfo SemanticModel.BindNamespaceOrType(SyntaxNode location, ExpressionSyntax type);
    ''' </pre>
    ''' Also, the following are affected if container=Nothing (and, for the latter, when container=Nothing or arity=0):
    ''' <pre>
    '''     Public Function LookupNames(position As Integer, Optional container As NamespaceOrTypeSymbol = Nothing, Optional options As LookupOptions = LookupOptions.Default, Optional results As List(Of String) = Nothing) As IList(Of String)
    '''     Public Function LookupSymbols(position As Integer,
    '''                                  Optional container As NamespaceOrTypeSymbol = Nothing,
    '''                                  Optional name As String = Nothing,
    '''                                  Optional arity As Integer? = Nothing,
    '''                                  Optional options As LookupOptions = LookupOptions.Default,
    '''                                  Optional results As List(Of Symbol) = Nothing) As IList(Of Symbol)
    ''' </pre>
    ''' </summary>
    Friend NotInheritable Class AliasSymbol
        Inherits Symbol
        Implements IAliasSymbol

        Private ReadOnly _aliasTarget As NamespaceOrTypeSymbol
        Private ReadOnly _aliasName As String
        Private ReadOnly _aliasLocations As ImmutableArray(Of Location)
        Private ReadOnly _aliasContainer As Symbol

        Friend Sub New(compilation As VisualBasicCompilation,
                       aliasContainer As Symbol,
                       aliasName As String,
                       aliasTarget As NamespaceOrTypeSymbol,
                       aliasLocation As Location)

            Dim merged = TryCast(aliasContainer, MergedNamespaceSymbol)
            Dim sourceNs As NamespaceSymbol = Nothing
            If merged IsNot Nothing Then
                sourceNs = merged.GetConstituentForCompilation(compilation)
            End If

            Me._aliasContainer = If(sourceNs, aliasContainer)

            Me._aliasTarget = aliasTarget
            Me._aliasName = aliasName
            Me._aliasLocations = ImmutableArray.Create(aliasLocation)
        End Sub

        ''' <summary>
        ''' The alias name.
        ''' </summary>
        Public Overrides ReadOnly Property Name As String
            Get
                Return _aliasName
            End Get
        End Property

        ''' <summary>
        ''' Gets the kind of this symbol.
        ''' </summary>
        ''' <returns><see cref="SymbolKind.Alias"/></returns>
        Public Overrides ReadOnly Property Kind As SymbolKind
            Get
                Return SymbolKind.Alias
            End Get
        End Property

        ''' <summary>
        ''' Gets the <see cref="NamespaceOrTypeSymbol"/> for the
        ''' namespace or type referenced by the alias.
        ''' </summary>
        Public ReadOnly Property Target As NamespaceOrTypeSymbol
            Get
                Return Me._aliasTarget
            End Get
        End Property

        Private ReadOnly Property IAliasSymbol_Target As INamespaceOrTypeSymbol Implements IAliasSymbol.Target
            Get
                Return Target
            End Get
        End Property

        ''' <summary>
        ''' Gets the locations where this symbol was originally defined.
        ''' </summary>
        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return _aliasLocations
            End Get
        End Property

        ''' <summary>
        ''' Get the syntax node(s) where this symbol was declared in source.
        ''' </summary>
        ''' <returns>
        ''' The syntax node(s) that declared the symbol.
        ''' </returns>
        ''' <remarks>
        ''' To go the opposite direction (from syntax node to symbol), see <see cref="VBSemanticModel.GetDeclaredSymbol"/>.
        ''' </remarks>
        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return GetDeclaringSyntaxReferenceHelper(Of SimpleImportsClauseSyntax)(Locations)
            End Get
        End Property

        ''' <summary>
        ''' Returns true if this symbol was declared to override a base class members and was
        ''' also restricted from further overriding; i.e., declared with the "NotOverridable"
        ''' modifier. Never returns true for types.
        ''' </summary>
        ''' <returns>False</returns>
        Public Overrides ReadOnly Property IsNotOverridable As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Returns true if this symbol was declared as requiring an override; i.e., declared
        ''' with the "MustOverride" modifier. Never returns true for types. 
        ''' </summary>
        ''' <returns>False</returns>
        Public Overrides ReadOnly Property IsMustOverride As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Returns true if this symbol was declared to override a base class members; i.e., declared
        ''' with the "Overrides" modifier. Still returns true if the members was declared
        ''' to override something, but (erroneously) no member to override exists.
        ''' </summary>
        ''' <returns>False</returns>
        Public Overrides ReadOnly Property IsOverrides As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Returns true if this member is overridable, has an implementation,
        ''' and does not override a base class member; i.e., declared with the "Overridable"
        ''' modifier. Does not return true for members declared as MustOverride or Overrides.
        ''' </summary>
        ''' <returns>False</returns>
        Public Overrides ReadOnly Property IsOverridable As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Returns true if this symbol is "shared"; i.e., declared with the "Shared"
        ''' modifier or implicitly always shared.
        ''' </summary>
        ''' <returns>False</returns>
        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Get this accessibility that was declared on this symbol. For symbols that do
        ''' not have accessibility declared on them, returns NotApplicable.
        ''' </summary>
        ''' <returns><see cref="Accessibility.NotApplicable"/></returns>
        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return Accessibility.NotApplicable
            End Get
        End Property

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return Nothing
            End Get
        End Property

        ''' <summary>
        ''' Get the symbol that logically contains this symbol. 
        ''' </summary>
        ''' <returns>
        ''' Using aliases in VB are always at the top
        ''' level within a compilation unit, within the [Global] namespace declaration.  We
        ''' return that as the "containing" symbol, even though the alias isn't a member of the
        ''' namespace as such.
        ''' </returns>
        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return _aliasContainer
            End Get
        End Property

        ''' <summary>
        ''' Determines whether the specified object is equal to the current object.
        ''' </summary>
        ''' <param name="obj">
        ''' The object to compare with the current object. 
        ''' </param>
        Public Overrides Function Equals(obj As Object) As Boolean
            If obj Is Me Then
                Return True
            ElseIf obj Is Nothing Then
                Return False
            End If

            Dim other As AliasSymbol = TryCast(obj, AliasSymbol)

            Return other IsNot Nothing AndAlso
                Equals(Me.Locations.FirstOrDefault(), other.Locations.FirstOrDefault()) AndAlso
                Me.ContainingAssembly Is other.ContainingAssembly
        End Function

        ''' <summary>
        ''' Returns a hash code for the current object.
        ''' </summary>
        Public Overrides Function GetHashCode() As Integer
            Return If(Me.Locations.Length > 0, Me.Locations(0).GetHashCode(), Me.Name.GetHashCode())
        End Function

        Friend Overrides Function Accept(Of TArg, TResult)(visitor As VisualBasicSymbolVisitor(Of TArg, TResult), a As TArg) As TResult
            Return visitor.VisitAlias(Me, a)
        End Function

        Public Overrides Sub Accept(visitor As SymbolVisitor)
            visitor.VisitAlias(Me)
        End Sub

        Public Overrides Function Accept(Of TResult)(visitor As SymbolVisitor(Of TResult)) As TResult
            Return visitor.VisitAlias(Me)
        End Function

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As SymbolVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitAlias(Me, argument)
        End Function

        Public Overrides Sub Accept(visitor As VisualBasicSymbolVisitor)
            visitor.VisitAlias(Me)
        End Sub

        Public Overrides Function Accept(Of TResult)(visitor As VisualBasicSymbolVisitor(Of TResult)) As TResult
            Return visitor.VisitAlias(Me)
        End Function
    End Class
End Namespace
