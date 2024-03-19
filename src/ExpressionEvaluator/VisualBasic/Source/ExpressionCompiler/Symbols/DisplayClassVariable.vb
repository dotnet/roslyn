' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator
    Friend Enum DisplayClassVariableKind
        Local
        Parameter
        [Me]
    End Enum

    ''' <summary>
    ''' A field in a display class that represents a captured variable:
    ''' either a local, a parameter, or "me".
    ''' </summary>
    <DebuggerDisplay("{GetDebuggerDisplay(), nq}")>
    Friend NotInheritable Class DisplayClassVariable

        Friend ReadOnly Name As String
        Friend ReadOnly Kind As DisplayClassVariableKind
        Friend ReadOnly DisplayClassInstance As DisplayClassInstance
        Friend ReadOnly DisplayClassFields As ConsList(Of FieldSymbol)

        Friend Sub New(
            name As String,
            kind As DisplayClassVariableKind,
            displayClassInstance As DisplayClassInstance,
            displayClassFields As ConsList(Of FieldSymbol))

            Debug.Assert(displayClassFields.Any())

            Me.Name = name
            Me.Kind = kind
            Me.DisplayClassInstance = displayClassInstance
            Me.DisplayClassFields = displayClassFields

            ' Verify all type parameters are substituted.
            Debug.Assert(Me.ContainingSymbol.IsContainingSymbolOfAllTypeParameters(Me.Type))
        End Sub

        Friend ReadOnly Property Type As TypeSymbol
            Get
                Return Me.DisplayClassFields.Head.Type
            End Get
        End Property

        Friend ReadOnly Property ContainingSymbol As Symbol
            Get
                Return Me.DisplayClassInstance.ContainingSymbol
            End Get
        End Property

        Friend Function ToOtherMethod(method As MethodSymbol, typeMap As TypeSubstitution) As DisplayClassVariable
            Dim otherInstance = Me.DisplayClassInstance.ToOtherMethod(method, typeMap)
            Return SubstituteFields(otherInstance, typeMap)
        End Function

        Friend Function ToBoundExpression(syntax As SyntaxNode, isLValue As Boolean, suppressVirtualCalls As Boolean) As BoundExpression
            Dim expr = Me.DisplayClassInstance.ToBoundExpression(syntax)
            Dim fields = ArrayBuilder(Of FieldSymbol).GetInstance()
            fields.AddRange(Me.DisplayClassFields)
            fields.ReverseContents()
            For Each field In fields
                expr = New BoundFieldAccess(syntax, expr, field, isLValue, suppressVirtualCalls, constantsInProgressOpt:=Nothing, type:=field.Type).MakeCompilerGenerated()
            Next
            fields.Free()
            Return expr
        End Function

        Friend Function SubstituteFields(otherInstance As DisplayClassInstance, typeMap As TypeSubstitution) As DisplayClassVariable
            Dim otherFields = SubstituteFields(Me.DisplayClassFields, typeMap)
            Return New DisplayClassVariable(Me.Name, Me.Kind, otherInstance, otherFields)
        End Function

        Private Function GetDebuggerDisplay() As String
            Return DisplayClassInstance.GetDebuggerDisplay(DisplayClassFields)
        End Function

        Private Shared Function SubstituteFields(fields As ConsList(Of FieldSymbol), typeMap As TypeSubstitution) As ConsList(Of FieldSymbol)
            If Not fields.Any() Then
                Return ConsList(Of FieldSymbol).Empty
            End If
            Dim head = SubstituteField(fields.Head, typeMap)
            Dim tail = SubstituteFields(fields.Tail, typeMap)
            Return tail.Prepend(head)
        End Function

        Private Shared Function SubstituteField(field As FieldSymbol, typeMap As TypeSubstitution) As FieldSymbol
            Debug.Assert(Not field.IsShared)
            Debug.Assert(Not field.IsReadOnly OrElse field.IsAnonymousTypeField(Nothing))
            Debug.Assert(field.CustomModifiers.Length = 0)
            Debug.Assert(Not field.HasConstantValue)
            Return New EEDisplayClassFieldSymbol(typeMap.SubstituteNamedType(field.ContainingType), field.Name, typeMap.SubstituteType(field.Type), field.DeclaredAccessibility)
        End Function

        Private NotInheritable Class EEDisplayClassFieldSymbol
            Inherits FieldSymbol

            Private ReadOnly _container As NamedTypeSymbol
            Private ReadOnly _name As String
            Private ReadOnly _type As TypeSymbol
            Private ReadOnly _accessibility As Accessibility

            Friend Sub New(container As NamedTypeSymbol, name As String, type As TypeSymbol, accessibility As Accessibility)
                _container = container
                _name = name
                _type = type
                _accessibility = accessibility
            End Sub

            Public Overrides ReadOnly Property Name As String
                Get
                    Return _name
                End Get
            End Property

            Public Overrides ReadOnly Property ContainingSymbol As Symbol
                Get
                    Return _container
                End Get
            End Property

            Public Overrides ReadOnly Property AssociatedSymbol As Symbol
                Get
                    Throw ExceptionUtilities.Unreachable
                End Get
            End Property

            Public Overrides ReadOnly Property CustomModifiers As ImmutableArray(Of CustomModifier)
                Get
                    Return ImmutableArray(Of CustomModifier).Empty
                End Get
            End Property

            Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
                Get
                    Return _accessibility ' Used by SymbolDisplay
                End Get
            End Property

            Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
                Get
                    Throw ExceptionUtilities.Unreachable
                End Get
            End Property

            Public Overrides ReadOnly Property IsConst As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property HasConstantValue As Boolean
                Get
                    Return False
                End Get
            End Property

            Friend Overrides Function GetConstantValue(inProgress As ConstantFieldsInProgress) As ConstantValue
                Return Nothing
            End Function

            Public Overrides ReadOnly Property IsReadOnly As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property IsShared As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
                Get
                    Throw ExceptionUtilities.Unreachable
                End Get
            End Property

            Public Overrides ReadOnly Property Type As TypeSymbol
                Get
                    Return _type
                End Get
            End Property

            Friend Overrides ReadOnly Property HasRuntimeSpecialName As Boolean
                Get
                    Throw ExceptionUtilities.Unreachable
                End Get
            End Property

            Friend Overrides ReadOnly Property HasSpecialName As Boolean
                Get
                    Throw ExceptionUtilities.Unreachable
                End Get
            End Property

            Friend Overrides ReadOnly Property IsNotSerialized As Boolean
                Get
                    Throw ExceptionUtilities.Unreachable
                End Get
            End Property

            Friend Overrides ReadOnly Property MarshallingInformation As MarshalPseudoCustomAttributeData
                Get
                    Throw ExceptionUtilities.Unreachable
                End Get
            End Property

            Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
                Get
                    Throw ExceptionUtilities.Unreachable
                End Get
            End Property

            Friend Overrides ReadOnly Property TypeLayoutOffset As Integer?
                Get
                    Throw ExceptionUtilities.Unreachable
                End Get
            End Property

            Public Overrides ReadOnly Property IsRequired As Boolean
                Get
                    Return False
                End Get
            End Property
        End Class

    End Class
End Namespace

