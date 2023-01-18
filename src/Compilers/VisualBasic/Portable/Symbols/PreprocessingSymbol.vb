' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Represents a preprocessing conditional compilation symbol.
    ''' </summary>
    Friend NotInheritable Class PreprocessingSymbol
        Inherits Symbol
        Implements IPreprocessingSymbol

        Private ReadOnly _name As String
        Private ReadOnly _assembly As IAssemblySymbol
        Private ReadOnly _module As IModuleSymbol

        Friend Sub New(name As String)
            Me.New(name, Nothing, Nothing)
        End Sub

        Friend Sub New(name As String, assembly As IAssemblySymbol, moduleSymbol As IModuleSymbol)
            MyBase.New()
            _name = name
            _assembly = assembly
            _module = moduleSymbol
        End Sub

        Public Overrides ReadOnly Property Name As String
            Get
                Return _name
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return ImmutableArray(Of Location).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return GetDeclaringSyntaxReferenceHelper(Of VisualBasicSyntaxNode)(Locations)
            End Get
        End Property

        Public Overrides ReadOnly Property Kind As SymbolKind
            Get
                Return SymbolKind.Preprocessing
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return Accessibility.NotApplicable
            End Get
        End Property

        Public Overrides ReadOnly Property IsMustOverride As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsNotOverridable As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverridable As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverrides As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingAssembly As AssemblySymbol
            Get
                Return TryCast(ISymbolContainingAssembly, AssemblySymbol)
            End Get
        End Property

        Friend Overloads ReadOnly Property ISymbolContainingAssembly As IAssemblySymbol Implements ISymbol.ContainingAssembly
            Get
                Return _assembly
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingModule As ModuleSymbol
            Get
                Return TryCast(ISymbolContainingModule, ModuleSymbol)
            End Get
        End Property

        Friend Overloads ReadOnly Property ISymbolContainingModule As IModuleSymbol Implements ISymbol.ContainingModule
            Get
                Return _module
            End Get
        End Property

        Public Overrides Function Equals(obj As Object) As Boolean
            If obj Is Me Then
                Return True
            ElseIf obj Is Nothing Then
                Return False
            End If

            ' If we're comparing against a C# preprocessing symbol, we still refer to the same
            ' symbol name. If there exists a different C# preprocessing symbol with a different
            ' capitalization variance, we also bind to that one. This is not a concern, as our
            ' VB preprocessing symbols only apply within the same project, and we only support
            ' this operation for finding all references of the given preprocessing symbol name.
            Dim other As IPreprocessingSymbol = TryCast(obj, IPreprocessingSymbol)

            Return other IsNot Nothing AndAlso
                IdentifierComparison.Equals(Me.Name, other.Name)
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return Me.Name.GetHashCode()
        End Function

        Public Overloads Overrides Sub Accept(visitor As SymbolVisitor)
            visitor.VisitPreprocessing(Me)
        End Sub

        Public Overloads Overrides Sub Accept(visitor As VisualBasicSymbolVisitor)
            visitor.VisitPreprocessing(Me)
        End Sub

        Public Overloads Overrides Function Accept(Of TResult)(visitor As SymbolVisitor(Of TResult)) As TResult
            Return visitor.VisitPreprocessing(Me)
        End Function

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As SymbolVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitPreprocessing(Me, argument)
        End Function

        Public Overloads Overrides Function Accept(Of TResult)(visitor As VisualBasicSymbolVisitor(Of TResult)) As TResult
            Return visitor.VisitPreprocessing(Me)
        End Function

        Friend Overloads Overrides Function Accept(Of TArgument, TResult)(visitor As VisualBasicSymbolVisitor(Of TArgument, TResult), arg As TArgument) As TResult
            Return visitor.VisitPreprocessing(Me, arg)
        End Function
    End Class

End Namespace
