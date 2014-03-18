' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Represents a type parameter symbol defined in source.
    ''' </summary>
    Friend MustInherit Class SourceTypeParameterSymbol
        Inherits TypeParameterSymbol

        Private ReadOnly m_ordinal As Integer ' 0 is first type parameter, etc.
        Private ReadOnly m_name As String
        Private m_lazyConstraints As ImmutableArray(Of TypeParameterConstraint)
        Private m_lazyConstraintTypes As ImmutableArray(Of TypeSymbol)

        Protected Sub New(ordinal As Integer, name As String)
            m_ordinal = ordinal
            m_name = name
        End Sub

        Public Overrides ReadOnly Property Ordinal As Integer
            Get
                Return m_ordinal
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return m_name
            End Get
        End Property

        Public Overloads Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return ImmutableArray(Of VisualBasicAttributeData).Empty
        End Function

        Public Overrides ReadOnly Property HasConstructorConstraint As Boolean
            Get
                EnsureAllConstraintsAreResolved()
                For Each constraint In m_lazyConstraints
                    If constraint.IsConstructorConstraint Then
                        Return True
                    End If
                Next
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property HasReferenceTypeConstraint As Boolean
            Get
                EnsureAllConstraintsAreResolved()
                For Each constraint In m_lazyConstraints
                    If constraint.IsReferenceTypeConstraint Then
                        Return True
                    End If
                Next
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property HasValueTypeConstraint As Boolean
            Get
                EnsureAllConstraintsAreResolved()
                For Each constraint In m_lazyConstraints
                    If constraint.IsValueTypeConstraint Then
                        Return True
                    End If
                Next
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property ConstraintTypesNoUseSiteDiagnostics As ImmutableArray(Of TypeSymbol)
            Get
                EnsureAllConstraintsAreResolved()
                Return m_lazyConstraintTypes
            End Get
        End Property

        Friend Overrides Function GetConstraints() As ImmutableArray(Of TypeParameterConstraint)
            EnsureAllConstraintsAreResolved()
            Return m_lazyConstraints
        End Function

        Friend Overrides Sub ResolveConstraints(inProgress As ConsList(Of TypeParameterSymbol))
            Debug.Assert(Not inProgress.Contains(Me))
            Debug.Assert(Not inProgress.Any() OrElse inProgress.Head.ContainingSymbol Is ContainingSymbol)

            If m_lazyConstraintTypes.IsDefault Then
                Dim diagnostics = DiagnosticBag.GetInstance()
                Dim constraints = GetDeclaredConstraints(diagnostics)
                Dim reportConflicts = DirectConstraintConflictKind.DuplicateTypeConstraint Or
                    If(ReportRedundantConstraints(), DirectConstraintConflictKind.RedundantConstraint, DirectConstraintConflictKind.None)

                Dim diagnosticsBuilder = ArrayBuilder(Of TypeParameterDiagnosticInfo).GetInstance()
                constraints = Me.RemoveDirectConstraintConflicts(constraints, inProgress.Prepend(Me), reportConflicts, diagnosticsBuilder)

                ImmutableInterlocked.InterlockedInitialize(m_lazyConstraints, constraints)

                If ImmutableInterlocked.InterlockedInitialize(m_lazyConstraintTypes, GetConstraintTypesOnly(constraints)) Then
                    Dim useSiteDiagnosticsBuilder As ArrayBuilder(Of TypeParameterDiagnosticInfo) = Nothing
                    Me.ReportIndirectConstraintConflicts(diagnosticsBuilder, useSiteDiagnosticsBuilder)

                    If useSiteDiagnosticsBuilder IsNot Nothing Then
                        diagnosticsBuilder.AddRange(useSiteDiagnosticsBuilder)
                    End If

                    For Each diagnostic In diagnosticsBuilder
                        Dim loc = GetLocation(diagnostic)
                        Debug.Assert(loc.IsInSource)
                        diagnostics.Add(diagnostic.DiagnosticInfo, loc)
                    Next

                    CheckConstraintTypeConstraints(constraints, diagnostics)
                    Dim sourceModule = DirectCast(ContainingModule, SourceModuleSymbol)
                    sourceModule.AddDiagnostics(diagnostics, CompilationStage.Declare)
                End If

                diagnosticsBuilder.Free()
                diagnostics.Free()
            End If
        End Sub

        Friend Overrides Sub EnsureAllConstraintsAreResolved()
            If m_lazyConstraintTypes.IsDefault Then
                EnsureAllConstraintsAreResolved(ContainerTypeParameters)
            End If
        End Sub

        Protected MustOverride ReadOnly Property ContainerTypeParameters As ImmutableArray(Of TypeParameterSymbol)

        Protected MustOverride Overloads Function GetDeclaredConstraints(diagnostics As DiagnosticBag) As ImmutableArray(Of TypeParameterConstraint)

        ''' <summary>
        ''' True if the redundant type parameter constraints should be reported as
        ''' errors. For overridden methods, this is False since type substitution of type
        ''' arguments for the base/interface may result in redundant constraints.
        ''' </summary>
        ''' <remarks>
        ''' This is a method rather than a property since the
        ''' implementation may be expensive.
        ''' </remarks>
        Protected MustOverride Function ReportRedundantConstraints() As Boolean

        ' Given a syntax ref, get the symbol location to return. We return
        ' the location of the Identifier of the type parameter.
        Protected Shared Function GetSymbolLocation(syntaxRef As SyntaxReference) As Location
            Dim syntaxNode = syntaxRef.GetSyntax()
            Dim syntaxTree = syntaxRef.SyntaxTree

            Return syntaxTree.GetLocation(DirectCast(syntaxNode, TypeParameterSyntax).Identifier.Span)
        End Function

        ''' <summary>
        ''' Check constraints of generic types referenced in constraint types. For instance,
        ''' with "Interface I(Of T As I(Of T))", check T satisfies constraints on I(Of T). Those
        ''' constraints are not checked when binding ConstraintTypes since ConstraintTypes
        ''' has not been set on I(Of T) at that point.
        ''' </summary>
        Private Shared Sub CheckConstraintTypeConstraints(constraints As ImmutableArray(Of TypeParameterConstraint), diagnostics As DiagnosticBag)
            For Each constraint In constraints
                Dim constraintType = constraint.TypeConstraint
                If constraintType IsNot Nothing Then
                    Dim location = constraint.LocationOpt
                    Debug.Assert(location IsNot Nothing)

                    Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
                    constraintType.AddUseSiteDiagnostics(useSiteDiagnostics)

                    If Not diagnostics.Add(location, useSiteDiagnostics) Then
                        constraintType.CheckAllConstraints(location, diagnostics)
                    End If
                End If
            Next
        End Sub

        ''' <summary>
        ''' Return the source location of the error, if any. If there error was
        ''' from a constraint, and that constraint was from source, its location
        ''' is returned. Otherwise if the type parameter was from source, its
        ''' location is returned. If neither is from source, Nothing is returned.
        ''' </summary>
        Private Shared Function GetLocation(diagnostic As TypeParameterDiagnosticInfo) As Location
            Dim loc = diagnostic.Constraint.LocationOpt
            If loc IsNot Nothing Then
                Return loc
            End If

            Dim locations = diagnostic.TypeParameter.Locations
            If locations.Length > 0 Then
                Return locations(0)
            End If

            Return Nothing
        End Function

        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return Me.ContainingSymbol.IsImplicitlyDeclared
            End Get
        End Property

    End Class

    ''' <summary>
    ''' Represents a type parameter on a source type (as opposed to a method).
    ''' </summary>
    Friend NotInheritable Class SourceTypeParameterOnTypeSymbol
        Inherits SourceTypeParameterSymbol

        Private ReadOnly m_container As SourceNamedTypeSymbol
        Private ReadOnly m_syntaxRefs As ImmutableArray(Of SyntaxReference)
        Private m_lazyVariance As VarianceKind

        Public Sub New(container As SourceNamedTypeSymbol, ordinal As Integer, name As String, syntaxRefs As ImmutableArray(Of SyntaxReference))
            MyBase.New(ordinal, name)
            Debug.Assert(Not syntaxRefs.IsEmpty)
            Debug.Assert(name IsNot Nothing)

            m_container = container
            m_syntaxRefs = syntaxRefs
        End Sub

        Public Overrides ReadOnly Property TypeParameterKind As TypeParameterKind
            Get
                Return TypeParameterKind.Type
            End Get
        End Property

        Public Overrides ReadOnly Property Variance As VarianceKind
            Get
                EnsureAllConstraintsAreResolved()
                Return m_lazyVariance
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Dim builder = ArrayBuilder(Of Location).GetInstance()
                For Each syntaxRef In m_syntaxRefs
                    builder.Add(GetSymbolLocation(syntaxRef))
                Next
                Return builder.ToImmutableAndFree()
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return GetDeclaringSyntaxReferenceHelper(m_syntaxRefs)
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return m_container
            End Get
        End Property

        Protected Overrides ReadOnly Property ContainerTypeParameters As ImmutableArray(Of TypeParameterSymbol)
            Get
                Return m_container.TypeParameters
            End Get
        End Property

        Protected Overrides Function GetDeclaredConstraints(diagnostics As DiagnosticBag) As ImmutableArray(Of TypeParameterConstraint)
            Dim variance As VarianceKind
            Dim constraints As ImmutableArray(Of TypeParameterConstraint) = Nothing
            m_container.BindTypeParameterConstraints(Me, variance, constraints, diagnostics)
            m_lazyVariance = variance
            Return constraints
        End Function

        Protected Overrides Function ReportRedundantConstraints() As Boolean
            Return True
        End Function

    End Class

    ''' <summary>
    ''' Represents a type parameter on a source method (as opposed to a type).
    ''' </summary>
    Friend NotInheritable Class SourceTypeParameterOnMethodSymbol
        Inherits SourceTypeParameterSymbol

        Private ReadOnly m_container As SourceMemberMethodSymbol
        Private ReadOnly m_syntaxRef As SyntaxReference

        Public Sub New(container As SourceMemberMethodSymbol,
                       ordinal As Integer,
                       name As String,
                       syntaxRef As SyntaxReference)
            MyBase.New(ordinal, name)
            m_container = container
            m_syntaxRef = syntaxRef
        End Sub

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return m_container
            End Get
        End Property

        Public Overrides ReadOnly Property TypeParameterKind As TypeParameterKind
            Get
                Return TypeParameterKind.Method
            End Get
        End Property

        Public Overrides ReadOnly Property Variance As VarianceKind
            Get
                Return VarianceKind.None
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return ImmutableArray.Create(GetSymbolLocation(m_syntaxRef))
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return GetDeclaringSyntaxReferenceHelper(m_syntaxRef)
            End Get
        End Property

        Protected Overrides ReadOnly Property ContainerTypeParameters As ImmutableArray(Of TypeParameterSymbol)
            Get
                Return m_container.TypeParameters
            End Get
        End Property

        Protected Overrides Function GetDeclaredConstraints(diagnostics As DiagnosticBag) As ImmutableArray(Of TypeParameterConstraint)
            Dim syntax = DirectCast(m_syntaxRef.GetSyntax(), TypeParameterSyntax)
            Return m_container.BindTypeParameterConstraints(syntax, diagnostics)
        End Function

        Protected Overrides Function ReportRedundantConstraints() As Boolean
            ' If the container is an overridden method, allow redundant constraints.
            If m_container.IsOverrides Then
                Return False
            End If

            ' If the container is a private, explicit interface
            ' implementation, allow redundant constraints.
            If (m_container.DeclaredAccessibility = Accessibility.Private) AndAlso
                    m_container.HasExplicitInterfaceImplementations Then
                Return False
            End If

            Return True
        End Function

    End Class
End Namespace

