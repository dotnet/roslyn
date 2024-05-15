' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Represents a type parameter symbol defined in source.
    ''' </summary>
    Friend MustInherit Class SourceTypeParameterSymbol
        Inherits SubstitutableTypeParameterSymbol

        Private ReadOnly _ordinal As Integer ' 0 is first type parameter, etc.
        Private ReadOnly _name As String
        Private _lazyConstraints As ImmutableArray(Of TypeParameterConstraint)
        Private _lazyConstraintTypes As ImmutableArray(Of TypeSymbol)

        Protected Sub New(ordinal As Integer, name As String)
            _ordinal = ordinal
            _name = name
        End Sub

        Public Overrides ReadOnly Property Ordinal As Integer
            Get
                Return _ordinal
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return _name
            End Get
        End Property

        Public Overloads Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return ImmutableArray(Of VisualBasicAttributeData).Empty
        End Function

        Public Overrides ReadOnly Property HasConstructorConstraint As Boolean
            Get
                EnsureAllConstraintsAreResolved()
                For Each constraint In _lazyConstraints
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
                For Each constraint In _lazyConstraints
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
                For Each constraint In _lazyConstraints
                    If constraint.IsValueTypeConstraint Then
                        Return True
                    End If
                Next
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property AllowsRefLikeType As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property ConstraintTypesNoUseSiteDiagnostics As ImmutableArray(Of TypeSymbol)
            Get
                EnsureAllConstraintsAreResolved()
                Return _lazyConstraintTypes
            End Get
        End Property

        Friend Overrides Function GetConstraints() As ImmutableArray(Of TypeParameterConstraint)
            EnsureAllConstraintsAreResolved()
            Return _lazyConstraints
        End Function

        Friend Overrides Sub ResolveConstraints(inProgress As ConsList(Of TypeParameterSymbol))
            Debug.Assert(Not inProgress.Contains(Me))
            Debug.Assert(Not inProgress.Any() OrElse inProgress.Head.ContainingSymbol Is ContainingSymbol)

            If _lazyConstraintTypes.IsDefault Then
                Dim diagnostics = BindingDiagnosticBag.GetInstance()
                Dim constraints = GetDeclaredConstraints(diagnostics)
                Dim reportConflicts = DirectConstraintConflictKind.DuplicateTypeConstraint Or
                    If(ReportRedundantConstraints(), DirectConstraintConflictKind.RedundantConstraint, DirectConstraintConflictKind.None)

                Dim diagnosticsBuilder = ArrayBuilder(Of TypeParameterDiagnosticInfo).GetInstance()
                constraints = Me.RemoveDirectConstraintConflicts(constraints, inProgress.Prepend(Me), reportConflicts, diagnosticsBuilder)

                ImmutableInterlocked.InterlockedInitialize(_lazyConstraints, constraints)

                If ImmutableInterlocked.InterlockedInitialize(_lazyConstraintTypes, GetConstraintTypesOnly(constraints)) Then
                    Dim useSiteDiagnosticsBuilder As ArrayBuilder(Of TypeParameterDiagnosticInfo) = Nothing
                    Me.ReportIndirectConstraintConflicts(diagnosticsBuilder, useSiteDiagnosticsBuilder)

                    If useSiteDiagnosticsBuilder IsNot Nothing Then
                        diagnosticsBuilder.AddRange(useSiteDiagnosticsBuilder)
                    End If

                    For Each diagnostic In diagnosticsBuilder
                        Dim loc = GetLocation(diagnostic)
                        Debug.Assert(loc.IsInSource)
                        diagnostics.Add(diagnostic.UseSiteInfo, loc)
                    Next

                    CheckConstraintTypeConstraints(constraints, diagnostics)
                    Dim sourceModule = DirectCast(ContainingModule, SourceModuleSymbol)
                    sourceModule.AddDeclarationDiagnostics(diagnostics)
                End If

                diagnosticsBuilder.Free()
                diagnostics.Free()
            End If
        End Sub

        Friend Overrides Sub EnsureAllConstraintsAreResolved()
            If _lazyConstraintTypes.IsDefault Then
                EnsureAllConstraintsAreResolved(ContainerTypeParameters)
            End If
        End Sub

        Protected MustOverride ReadOnly Property ContainerTypeParameters As ImmutableArray(Of TypeParameterSymbol)

        Protected MustOverride Overloads Function GetDeclaredConstraints(diagnostics As BindingDiagnosticBag) As ImmutableArray(Of TypeParameterConstraint)

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
        Private Sub CheckConstraintTypeConstraints(constraints As ImmutableArray(Of TypeParameterConstraint), diagnostics As BindingDiagnosticBag)
            Dim containingAssembly As AssemblySymbol = Me.ContainingAssembly

            For Each constraint In constraints
                Dim constraintType = constraint.TypeConstraint
                If constraintType IsNot Nothing Then
                    Dim location = constraint.LocationOpt
                    Debug.Assert(location IsNot Nothing)

                    Dim useSiteInfo As New CompoundUseSiteInfo(Of AssemblySymbol)(diagnostics, containingAssembly)
                    constraintType.AddUseSiteInfo(useSiteInfo)

                    If Not diagnostics.Add(location, useSiteInfo) Then
                        constraintType.CheckAllConstraints(location, diagnostics, template:=New CompoundUseSiteInfo(Of AssemblySymbol)(diagnostics, containingAssembly))
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

        Private ReadOnly _container As SourceNamedTypeSymbol
        Private ReadOnly _syntaxRefs As ImmutableArray(Of SyntaxReference)
        Private _lazyVariance As VarianceKind

        Public Sub New(container As SourceNamedTypeSymbol, ordinal As Integer, name As String, syntaxRefs As ImmutableArray(Of SyntaxReference))
            MyBase.New(ordinal, name)
            Debug.Assert(Not syntaxRefs.IsEmpty)
            Debug.Assert(name IsNot Nothing)

            _container = container
            _syntaxRefs = syntaxRefs
        End Sub

        Public Overrides ReadOnly Property TypeParameterKind As TypeParameterKind
            Get
                Return TypeParameterKind.Type
            End Get
        End Property

        Public Overrides ReadOnly Property Variance As VarianceKind
            Get
                EnsureAllConstraintsAreResolved()
                Return _lazyVariance
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Dim builder = ArrayBuilder(Of Location).GetInstance()
                For Each syntaxRef In _syntaxRefs
                    builder.Add(GetSymbolLocation(syntaxRef))
                Next
                Return builder.ToImmutableAndFree()
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return GetDeclaringSyntaxReferenceHelper(_syntaxRefs)
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return _container
            End Get
        End Property

        Protected Overrides ReadOnly Property ContainerTypeParameters As ImmutableArray(Of TypeParameterSymbol)
            Get
                Return _container.TypeParameters
            End Get
        End Property

        Protected Overrides Function GetDeclaredConstraints(diagnostics As BindingDiagnosticBag) As ImmutableArray(Of TypeParameterConstraint)
            Dim variance As VarianceKind
            Dim constraints As ImmutableArray(Of TypeParameterConstraint) = Nothing
            _container.BindTypeParameterConstraints(Me, variance, constraints, diagnostics)
            _lazyVariance = variance
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

        Private ReadOnly _container As SourceMemberMethodSymbol
        Private ReadOnly _syntaxRef As SyntaxReference

        Public Sub New(container As SourceMemberMethodSymbol,
                       ordinal As Integer,
                       name As String,
                       syntaxRef As SyntaxReference)
            MyBase.New(ordinal, name)
            _container = container
            _syntaxRef = syntaxRef
        End Sub

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return _container
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
                Return ImmutableArray.Create(GetSymbolLocation(_syntaxRef))
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return GetDeclaringSyntaxReferenceHelper(_syntaxRef)
            End Get
        End Property

        Protected Overrides ReadOnly Property ContainerTypeParameters As ImmutableArray(Of TypeParameterSymbol)
            Get
                Return _container.TypeParameters
            End Get
        End Property

        Protected Overrides Function GetDeclaredConstraints(diagnostics As BindingDiagnosticBag) As ImmutableArray(Of TypeParameterConstraint)
            Dim syntax = DirectCast(_syntaxRef.GetSyntax(), TypeParameterSyntax)
            Return _container.BindTypeParameterConstraints(syntax, diagnostics)
        End Function

        Protected Overrides Function ReportRedundantConstraints() As Boolean
            ' If the container is an overridden method, allow redundant constraints.
            If _container.IsOverrides Then
                Return False
            End If

            ' If the container is a private, explicit interface
            ' implementation, allow redundant constraints.
            If (_container.DeclaredAccessibility = Accessibility.Private) AndAlso
                    _container.HasExplicitInterfaceImplementations Then
                Return False
            End If

            Return True
        End Function

    End Class
End Namespace

