' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' Represents enum constant field in source.
    ''' </summary>
    Friend MustInherit Class SourceEnumConstantSymbol
        Inherits SourceFieldSymbol

        Private _constantTuple As EvaluatedConstant

        Public Shared Function CreateExplicitValuedConstant(containingEnum As SourceNamedTypeSymbol, bodyBinder As Binder, syntax As EnumMemberDeclarationSyntax, diagnostics As BindingDiagnosticBag) As SourceEnumConstantSymbol
            Dim initializer = syntax.Initializer
            Debug.Assert(initializer IsNot Nothing)
            Return New ExplicitValuedEnumConstantSymbol(containingEnum, bodyBinder, syntax, initializer, diagnostics)
        End Function

        Public Shared Function CreateImplicitValuedConstant(containingEnum As SourceNamedTypeSymbol, bodyBinder As Binder, syntax As EnumMemberDeclarationSyntax, otherConstant As SourceEnumConstantSymbol, otherConstantOffset As Integer, diagnostics As BindingDiagnosticBag) As SourceEnumConstantSymbol
            If otherConstant Is Nothing Then
                Debug.Assert(otherConstantOffset = 0)
                Return New ZeroValuedEnumConstantSymbol(containingEnum, bodyBinder, syntax, diagnostics)
            Else
                Debug.Assert(otherConstantOffset > 0)
                Return New ImplicitValuedEnumConstantSymbol(containingEnum, bodyBinder, syntax, otherConstant, CType(otherConstantOffset, UInteger), diagnostics)
            End If
        End Function

        Protected Sub New(containingEnum As SourceNamedTypeSymbol, bodyBinder As Binder, syntax As EnumMemberDeclarationSyntax, diagnostics As BindingDiagnosticBag)
            MyBase.New(containingEnum,
                       bodyBinder.GetSyntaxReference(syntax),
                       name:=syntax.Identifier.ValueText,
                       memberFlags:=SourceMemberFlags.Const Or SourceMemberFlags.Shared Or SourceMemberFlags.AccessibilityPublic)

            If IdentifierComparison.Equals(Me.Name, WellKnownMemberNames.EnumBackingFieldName) Then
                diagnostics.Add(ERRID.ERR_ClashWithReservedEnumMember1, syntax.Identifier.GetLocation(), Me.Name)
            End If
        End Sub

        Friend NotOverridable Overrides ReadOnly Property DeclarationSyntax As VisualBasicSyntaxNode
            Get
                Return Syntax
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property GetAttributeDeclarations As OneOrMany(Of SyntaxList(Of AttributeListSyntax))
            Get
                Return OneOrMany.Create(DirectCast(Syntax, EnumMemberDeclarationSyntax).AttributeLists)
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return ImmutableArray.Create(DirectCast(Syntax, EnumMemberDeclarationSyntax).Identifier.GetLocation())
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property Type As TypeSymbol
            Get
                Return ContainingType
            End Get
        End Property

        Protected NotOverridable Overrides Function GetLazyConstantTuple() As EvaluatedConstant
            Return _constantTuple
        End Function

        Friend NotOverridable Overrides Function GetConstantValue(inProgress As ConstantFieldsInProgress) As ConstantValue
            Return GetConstantValueImpl(inProgress)
        End Function

        Protected NotOverridable Overrides Sub SetLazyConstantTuple(constantTuple As EvaluatedConstant, diagnostics As BindingDiagnosticBag)
            Dim sourceModule = DirectCast(Me.ContainingModule, SourceModuleSymbol)
            sourceModule.AtomicStoreReferenceAndDiagnostics(_constantTuple, constantTuple, diagnostics)
        End Sub

        Friend Overrides ReadOnly Property MeParameter As ParameterSymbol
            Get
                Return Nothing
            End Get
        End Property

        Protected MustOverride Overrides Function MakeConstantTuple(dependencies As ConstantFieldsInProgress.Dependencies, diagnostics As BindingDiagnosticBag) As EvaluatedConstant

        ' There are implementations for:
        ' 1) enum constant that is zero
        ' 2) enum constant that has explicit value
        ' 3) enum constant that has a value incrementally dependent on another enum constant
#Region "Concrete constant symbol implementations"

        Private NotInheritable Class ZeroValuedEnumConstantSymbol
            Inherits SourceEnumConstantSymbol

            Public Sub New(containingEnum As SourceNamedTypeSymbol, bodyBinder As Binder, syntax As EnumMemberDeclarationSyntax, diagnostics As BindingDiagnosticBag)
                MyBase.New(containingEnum, bodyBinder, syntax, diagnostics)
            End Sub

            Protected Overrides Function MakeConstantTuple(dependencies As ConstantFieldsInProgress.Dependencies, diagnostics As BindingDiagnosticBag) As EvaluatedConstant
                Dim underlyingType = ContainingType.EnumUnderlyingType
                Return New EvaluatedConstant(Microsoft.CodeAnalysis.ConstantValue.Default(underlyingType.SpecialType), underlyingType)
            End Function
        End Class

        Private NotInheritable Class ExplicitValuedEnumConstantSymbol
            Inherits SourceEnumConstantSymbol

            Private ReadOnly _equalsValueNodeRef As SyntaxReference

            Public Sub New(containingEnum As SourceNamedTypeSymbol, bodyBinder As Binder, syntax As EnumMemberDeclarationSyntax, initializer As EqualsValueSyntax, diagnostics As BindingDiagnosticBag)
                MyBase.New(containingEnum, bodyBinder, syntax, diagnostics)
                Me._equalsValueNodeRef = bodyBinder.GetSyntaxReference(initializer)
            End Sub

            Protected Overrides Function MakeConstantTuple(dependencies As ConstantFieldsInProgress.Dependencies, diagnostics As BindingDiagnosticBag) As EvaluatedConstant
                Return ConstantValueUtils.EvaluateFieldConstant(Me, Me._equalsValueNodeRef, dependencies, diagnostics)
            End Function
        End Class

        Private NotInheritable Class ImplicitValuedEnumConstantSymbol
            Inherits SourceEnumConstantSymbol

            Private ReadOnly _otherConstant As SourceEnumConstantSymbol
            Private ReadOnly _otherConstantOffset As UInteger

            Public Sub New(containingEnum As SourceNamedTypeSymbol, bodyBinder As Binder, syntax As EnumMemberDeclarationSyntax, otherConstant As SourceEnumConstantSymbol, otherConstantOffset As UInteger, diagnostics As BindingDiagnosticBag)
                MyBase.New(containingEnum, bodyBinder, syntax, diagnostics)
                Debug.Assert(otherConstant IsNot Nothing)
                Debug.Assert(otherConstantOffset > 0)
                Me._otherConstant = otherConstant
                Me._otherConstantOffset = otherConstantOffset
            End Sub

            Protected Overrides Function MakeConstantTuple(dependencies As ConstantFieldsInProgress.Dependencies, diagnostics As BindingDiagnosticBag) As EvaluatedConstant
#If DEBUG Then
                Debug.Assert(dependencies IsNot Nothing)
#End If
                Dim value As ConstantValue = Microsoft.CodeAnalysis.ConstantValue.Bad
                Dim otherValue = _otherConstant.GetConstantValue(New ConstantFieldsInProgress(Me, dependencies))
                If Not otherValue.IsBad Then
                    Dim overflowKind = EnumConstantHelper.OffsetValue(otherValue, _otherConstantOffset, value)
                    If overflowKind = EnumOverflowKind.OverflowReport Then
                        diagnostics.Add(ERRID.ERR_ExpressionOverflow1, GetFirstLocation(), Me)
                    End If
                End If

                Return New EvaluatedConstant(value, Type)
            End Function
        End Class

#End Region

    End Class
End Namespace

