' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' A type parameter constraint: a single 'New', 'Class',
    ''' 'Structure' constraint or a specific type constraint.
    ''' </summary>
    Friend Structure TypeParameterConstraint
        Public Sub New(kind As TypeParameterConstraintKind, loc As Location)
            Me.New(kind, Nothing, loc)
            Debug.Assert((kind = TypeParameterConstraintKind.Constructor) OrElse
                         (kind = TypeParameterConstraintKind.ReferenceType) OrElse
                         (kind = TypeParameterConstraintKind.ValueType))
        End Sub

        Public Sub New(type As TypeSymbol, loc As Location)
            Me.New(TypeParameterConstraintKind.None, type, loc)
            Debug.Assert(type IsNot Nothing)
        End Sub

        Private Sub New(kind As TypeParameterConstraintKind, type As TypeSymbol, loc As Location)
            ' The location should only be provided for type parameters from source.
            Debug.Assert((loc Is Nothing) OrElse loc.PossiblyEmbeddedOrMySourceTree IsNot Nothing)
            Me.Kind = kind
            Me.TypeConstraint = type
            Me.LocationOpt = loc
        End Sub

        Public Function AtLocation(loc As Location) As TypeParameterConstraint
            Return New TypeParameterConstraint(Kind, TypeConstraint, loc)
        End Function

        Public ReadOnly Kind As TypeParameterConstraintKind
        Public ReadOnly TypeConstraint As TypeSymbol
        Public ReadOnly LocationOpt As Location

        Public ReadOnly Property IsConstructorConstraint As Boolean
            Get
                Return Kind = TypeParameterConstraintKind.Constructor
            End Get
        End Property

        Public ReadOnly Property IsReferenceTypeConstraint As Boolean
            Get
                Return Kind = TypeParameterConstraintKind.ReferenceType
            End Get
        End Property

        Public ReadOnly Property IsValueTypeConstraint As Boolean
            Get
                Return Kind = TypeParameterConstraintKind.ValueType
            End Get
        End Property

        Public Function ToDisplayFormat() As Object
            If TypeConstraint IsNot Nothing Then
                Return CustomSymbolDisplayFormatter.ErrorNameWithKind(TypeConstraint)
            Else
                Return SyntaxFacts.GetText(ToSyntaxKind(Kind))
            End If
        End Function

        Public Overrides Function ToString() As String
            Return ToDisplayFormat().ToString()
        End Function

        Private Shared Function ToSyntaxKind(kind As TypeParameterConstraintKind) As SyntaxKind
            Select Case kind
                Case TypeParameterConstraintKind.Constructor
                    Return SyntaxKind.NewKeyword
                Case TypeParameterConstraintKind.ReferenceType
                    Return SyntaxKind.ClassKeyword
                Case TypeParameterConstraintKind.ValueType
                    Return SyntaxKind.StructureKeyword
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(kind)
            End Select
        End Function
    End Structure

End Namespace
