' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' A tuple of TypeParameterSymbol and DiagnosticInfo, created for errors
    ''' reported from ConstraintsHelper rather than creating Diagnostics directly.
    ''' This decouples constraints checking from syntax and Locations, and supports
    ''' callers that may want to create Location instances lazily or not at all.
    ''' </summary>
    Friend Structure TypeParameterDiagnosticInfo
        Public Sub New(typeParameter As TypeParameterSymbol, diagnostic As DiagnosticInfo)
            Me.TypeParameter = typeParameter
            Me.DiagnosticInfo = diagnostic
        End Sub

        Public Sub New(typeParameter As TypeParameterSymbol, constraint As TypeParameterConstraint, diagnostic As DiagnosticInfo)
            Me.New(typeParameter, diagnostic)
            Me.Constraint = constraint
        End Sub

        Public ReadOnly TypeParameter As TypeParameterSymbol
        Public ReadOnly Constraint As TypeParameterConstraint
        Public ReadOnly DiagnosticInfo As DiagnosticInfo
    End Structure

    <Flags()>
    Friend Enum DirectConstraintConflictKind
        None = 0
        DuplicateTypeConstraint = 1 << 0
        RedundantConstraint = 1 << 1
        All = (1 << 2) - 1
    End Enum

    ''' <summary>
    ''' Helper methods for generic type parameter constraints. There are two sets of methods: one
    ''' set for resolving constraint "bounds" (that is, determining the effective base type, interface set,
    ''' etc.), and another set for checking for constraint violations in type and method references.
    ''' 
    ''' Bounds are resolved by calling one of the ResolveBounds overloads. Typically bounds are
    ''' resolved by each TypeParameterSymbol at, or before, one of the corresponding properties
    ''' (BaseType, Interfaces, etc.) is accessed. Resolving bounds may result in errors (cycles,
    ''' inconsistent constraints, etc.) and it is the responsibility of the caller to report any such
    ''' errors as declaration errors or use-site errors (depending on whether the type parameter
    ''' was from source or metadata) and to ensure bounds are resolved for source type parameters
    ''' even if the corresponding properties are never accessed directly.
    ''' 
    ''' Constraints are checked by calling one of the CheckConstraints or CheckAllConstraints
    ''' overloads for any generic type or method reference from source. In some circumstances,
    ''' references are checked at the time the generic type or generic method is bound and constructed
    ''' by the Binder. In those case, it is sufficient to call one of the CheckConstraints overloads
    ''' since compound types (such as A(Of T).B(Of U) or A(Of B(Of T))) are checked incrementally
    ''' as each part is bound. In other cases however, constraint checking needs to be delayed to
    ''' prevent cycles where checking constraints requires binding the syntax that is currently
    ''' being bound (such as the constraint in Class C(Of T As C(Of T)). In those cases, the caller
    ''' must lazily check constraints, and since the types may be compound types, it is necessary
    ''' to call CheckAllConstraints.
    ''' </summary>
    Friend Module ConstraintsHelper

        ''' <summary>
        ''' Enum used internally by RemoveDirectConstraintConflicts to
        ''' track what type constraint has been seen, to report conflicts
        ''' between { 'Structure', 'Class', [explicit type] }. The 'New'
        ''' constraint does not need to be tracked for those conflicts.
        ''' </summary>
        Private Enum DirectTypeConstraintKind
            None
            ReferenceTypeConstraint
            ValueTypeConstraint
            ExplicitType
        End Enum

        ''' <summary>
        ''' Return the constraints for the type parameter with any cycles
        ''' or conflicting constraints reported as errors and removed.
        ''' </summary>
        <Extension()>
        Public Function RemoveDirectConstraintConflicts(
                                     typeParameter As TypeParameterSymbol,
                                     constraints As ImmutableArray(Of TypeParameterConstraint),
                                     inProgress As ConsList(Of TypeParameterSymbol),
                                     reportConflicts As DirectConstraintConflictKind,
                                     diagnosticsBuilder As ArrayBuilder(Of TypeParameterDiagnosticInfo)) As ImmutableArray(Of TypeParameterConstraint)
            If constraints.Length > 0 Then
                Dim constraintsBuilder = ArrayBuilder(Of TypeParameterConstraint).GetInstance()
                Dim containingSymbol = typeParameter.ContainingSymbol
                Dim explicitKind = DirectTypeConstraintKind.None
                Dim reportRedundantConstraints = (reportConflicts And DirectConstraintConflictKind.RedundantConstraint) <> 0

                For Each constraint In constraints
                    ' See Bindable::ValidateDirectConstraint.
                    Select Case constraint.Kind
                        Case TypeParameterConstraintKind.ReferenceType
                            If reportRedundantConstraints Then
                                If explicitKind = DirectTypeConstraintKind.None Then
                                    explicitKind = DirectTypeConstraintKind.ReferenceTypeConstraint
                                Else
                                    ' Combinations of {Class, Structure} should have
                                    ' been caught and discarded during binding.
                                    Debug.Assert(explicitKind = DirectTypeConstraintKind.ExplicitType)

                                    ' "'Class' constraint and a specific class type constraint cannot be combined."
                                    diagnosticsBuilder.Add(New TypeParameterDiagnosticInfo(typeParameter,
                                                                                           constraint,
                                                                                           ErrorFactory.ErrorInfo(ERRID.ERR_RefAndClassTypeConstrCombined)))
                                    Continue For
                                End If
                            End If

                        Case TypeParameterConstraintKind.ValueType
                            If reportRedundantConstraints Then
                                If explicitKind = DirectTypeConstraintKind.None Then
                                    explicitKind = DirectTypeConstraintKind.ValueTypeConstraint
                                Else
                                    ' Combinations of {Class, Structure} should have
                                    ' been caught and discarded during binding.
                                    Debug.Assert(explicitKind = DirectTypeConstraintKind.ExplicitType)

                                    ' "'Structure' constraint and a specific class type constraint cannot be combined."
                                    diagnosticsBuilder.Add(New TypeParameterDiagnosticInfo(typeParameter,
                                                                                           constraint,
                                                                                           ErrorFactory.ErrorInfo(ERRID.ERR_ValueAndClassTypeConstrCombined)))
                                    Continue For
                                End If
                            End If

                        Case TypeParameterConstraintKind.None
                            Dim constraintType = constraint.TypeConstraint
                            Dim duplicate As Boolean = ContainsTypeConstraint(constraintsBuilder, constraintType)

                            ' Check for duplicate type constraints.
                            If duplicate Then
                                If (reportConflicts And DirectConstraintConflictKind.DuplicateTypeConstraint) <> 0 Then
                                    ' "Constraint type '{0}' already specified for this type parameter.'
                                    diagnosticsBuilder.Add(New TypeParameterDiagnosticInfo(typeParameter,
                                                                                           constraint,
                                                                                           ErrorFactory.ErrorInfo(ERRID.ERR_ConstraintAlreadyExists1, constraintType)))
                                End If

                                ' Continue with other checks for this constraint type, even
                                ' though it's a duplicate, for consistency with Dev10.
                            End If

                            Select Case constraintType.TypeKind
                                Case TypeKind.Class
                                    If reportRedundantConstraints Then
                                        Select Case explicitKind
                                            Case DirectTypeConstraintKind.None
                                                Dim classType = DirectCast(constraintType, NamedTypeSymbol)
                                                If classType.IsNotInheritable Then
                                                    ' "Type constraint cannot be a 'NotInheritable' class."
                                                    diagnosticsBuilder.Add(New TypeParameterDiagnosticInfo(typeParameter,
                                                                                                           constraint,
                                                                                                           ErrorFactory.ErrorInfo(ERRID.ERR_ClassConstraintNotInheritable1)))

                                                Else
                                                    Select Case constraintType.SpecialType
                                                        Case SpecialType.System_Object,
                                                            SpecialType.System_ValueType,
                                                            SpecialType.System_Enum,
                                                            SpecialType.System_Delegate,
                                                            SpecialType.System_MulticastDelegate,
                                                            SpecialType.System_Array
                                                            ' "'{0}' cannot be used as a type constraint."
                                                            diagnosticsBuilder.Add(New TypeParameterDiagnosticInfo(typeParameter,
                                                                                                                   constraint,
                                                                                                                   ErrorFactory.ErrorInfo(ERRID.ERR_ConstraintIsRestrictedType1, constraintType)))
                                                    End Select

                                                End If

                                                explicitKind = DirectTypeConstraintKind.ExplicitType

                                            Case DirectTypeConstraintKind.ReferenceTypeConstraint
                                                ' "'Class' constraint and a specific class type constraint cannot be combined."
                                                diagnosticsBuilder.Add(New TypeParameterDiagnosticInfo(typeParameter,
                                                                                                       constraint,
                                                                                                       ErrorFactory.ErrorInfo(ERRID.ERR_RefAndClassTypeConstrCombined)))
                                                Continue For

                                            Case DirectTypeConstraintKind.ValueTypeConstraint
                                                ' "'Structure' constraint and a specific class type constraint cannot be combined."
                                                diagnosticsBuilder.Add(New TypeParameterDiagnosticInfo(typeParameter,
                                                                                                       constraint,
                                                                                                       ErrorFactory.ErrorInfo(ERRID.ERR_ValueAndClassTypeConstrCombined)))
                                                Continue For

                                            Case DirectTypeConstraintKind.ExplicitType
                                                ' "Type parameter '{0}' can only have one constraint that is a class."
                                                diagnosticsBuilder.Add(New TypeParameterDiagnosticInfo(typeParameter,
                                                                                                       constraint,
                                                                                                       ErrorFactory.ErrorInfo(ERRID.ERR_MultipleClassConstraints1, typeParameter)))
                                                Continue For

                                        End Select
                                    End If

                                Case TypeKind.Interface,
                                    TypeKind.Error

                                Case TypeKind.Module
                                    ' No error reported for Module. If the type reference was in source, BC30371
                                    ' ERR_ModuleAsType1 will have been reported binding the type reference.

                                Case TypeKind.TypeParameter
                                    Dim constraintTypeParameter = DirectCast(constraintType, TypeParameterSymbol)

                                    If constraintTypeParameter.ContainingSymbol = containingSymbol Then
                                        ' The constraint type parameter is from the same containing type or method.
                                        If inProgress.ContainsReference(constraintTypeParameter) Then
                                            ' "Type parameter '{0}' cannot be constrained to itself: {1}"
                                            diagnosticsBuilder.Add(New TypeParameterDiagnosticInfo(constraintTypeParameter,
                                                                                                   constraint,
                                                                                                   ErrorFactory.ErrorInfo(ERRID.ERR_ConstraintCycle2, constraintTypeParameter, GetConstraintCycleInfo(inProgress))))
                                            Continue For
                                        Else
                                            ' Traverse constraint type parameter constraints to detect cycles.
                                            constraintTypeParameter.ResolveConstraints(inProgress)
                                        End If
                                    End If

                                    If reportRedundantConstraints AndAlso constraintTypeParameter.HasValueTypeConstraint Then
                                        ' "Type parameter with a 'Structure' constraint cannot be used as a constraint."
                                        diagnosticsBuilder.Add(New TypeParameterDiagnosticInfo(constraintTypeParameter,
                                                                                               constraint,
                                                                                               ErrorFactory.ErrorInfo(ERRID.ERR_TypeParamWithStructConstAsConst)))
                                        Continue For
                                    End If

                                Case TypeKind.Array,
                                    TypeKind.Delegate,
                                    TypeKind.Enum,
                                    TypeKind.Structure
                                    If reportRedundantConstraints Then
                                        ' "Type constraint '{0}' must be either a class, interface or type parameter."
                                        diagnosticsBuilder.Add(New TypeParameterDiagnosticInfo(typeParameter,
                                                                                               constraint,
                                                                                               ErrorFactory.ErrorInfo(ERRID.ERR_ConstNotClassInterfaceOrTypeParam1, constraintType)))
                                    End If

                                Case Else
                                    Throw ExceptionUtilities.UnexpectedValue(constraintType.TypeKind)

                            End Select

                            If duplicate Then
                                Continue For
                            End If

                    End Select

                    constraintsBuilder.Add(constraint)
                Next

                If constraintsBuilder.Count <> constraints.Length Then
                    constraints = constraintsBuilder.ToImmutable()
                End If

                constraintsBuilder.Free()
            End If

            Return constraints
        End Function

        ' Currently, this method should be called for SourceTypeParameterSymbols
        ' only, not PETypeParameterSymbols. If that changes, and this method is
        ' called for type parameters from metadata, we need to ensure a use-site
        ' error is generated if conflicts are found. Add a unit test if that's the case.
        ' See Bindable::ValidateIndirectConstraints.
        <Extension()>
        Public Sub ReportIndirectConstraintConflicts(
                                     typeParameter As SourceTypeParameterSymbol,
                                     diagnosticsBuilder As ArrayBuilder(Of TypeParameterDiagnosticInfo),
                                     <[In], Out> ByRef useSiteDiagnosticsBuilder As ArrayBuilder(Of TypeParameterDiagnosticInfo))

            Dim constraints = ArrayBuilder(Of TypeParameterAndConstraint).GetInstance()
            typeParameter.GetAllConstraints(constraints, fromConstraintOpt:=Nothing)

            Dim n = constraints.Count
            For i = 0 To n - 1
                Dim pair1 = constraints(i)
                If pair1.IsBad Then
                    Continue For
                End If

                For j = i + 1 To n - 1
                    Dim pair2 = constraints(j)
                    If pair2.IsBad Then
                        Continue For
                    End If

                    Dim bad = False

                    Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing

                    If (pair1.TypeParameter Is typeParameter) AndAlso (pair2.TypeParameter Is typeParameter) Then
                        ' Check direct constraints to handle inherited constraints on overridden methods.
                        If HasConflict(pair1.Constraint, pair2.Constraint, useSiteDiagnostics) Then
                            ' "Constraint '{0}' conflicts with the constraint '{1}' already specified for type parameter '{2}'."
                            diagnosticsBuilder.Add(New TypeParameterDiagnosticInfo(typeParameter,
                                                                                   pair2.Constraint,
                                                                                   ErrorFactory.ErrorInfo(
                                                                                       ERRID.ERR_ConflictingDirectConstraints3,
                                                                                       pair2.Constraint.ToDisplayFormat(),
                                                                                       pair1.Constraint.ToDisplayFormat(),
                                                                                       typeParameter)))
                            bad = True
                        End If

                    ElseIf (pair1.TypeParameter Is pair2.TypeParameter) Then
                        ' Skip other cases where both constraints are from the same type
                        ' parameter but not the current type parameter since those cases
                        ' will be reported directly on the other type parameter.

                    ElseIf HasConflict(pair1.Constraint, pair2.Constraint, useSiteDiagnostics) Then
                        If pair1.TypeParameter Is typeParameter Then
                            ' "Constraint '{0}' conflicts with the indirect constraint '{1}' obtained from the type parameter constraint '{2}'."
                            diagnosticsBuilder.Add(New TypeParameterDiagnosticInfo(typeParameter,
                                                                                   pair1.Constraint,
                                                                                   ErrorFactory.ErrorInfo(
                                                                                       ERRID.ERR_ConstraintClashDirectIndirect3,
                                                                                       pair1.Constraint.ToDisplayFormat(),
                                                                                       pair2.Constraint.ToDisplayFormat(),
                                                                                       pair2.TypeParameter)))
                            bad = True

                        ElseIf pair2.TypeParameter Is typeParameter Then
                            ' "Indirect constraint '{0}' obtained from the type parameter constraint '{1}' conflicts with the constraint '{2}'."
                            diagnosticsBuilder.Add(New TypeParameterDiagnosticInfo(typeParameter,
                                                                                   pair1.Constraint,
                                                                                   ErrorFactory.ErrorInfo(
                                                                                       ERRID.ERR_ConstraintClashIndirectDirect3,
                                                                                       pair1.Constraint.ToDisplayFormat(),
                                                                                       pair1.TypeParameter,
                                                                                       pair2.Constraint.ToDisplayFormat())))
                            bad = True

                        Else
                            ' "Indirect constraint '{0}' obtained from the type parameter constraint '{1}' conflicts with the indirect constraint '{2}' obtained from the type parameter constraint '{3}'."
                            diagnosticsBuilder.Add(New TypeParameterDiagnosticInfo(typeParameter,
                                                                                   pair2.Constraint,
                                                                                   ErrorFactory.ErrorInfo(
                                                                                       ERRID.ERR_ConstraintClashIndirectIndirect4,
                                                                                       pair2.Constraint.ToDisplayFormat(),
                                                                                       pair2.TypeParameter,
                                                                                       pair1.Constraint.ToDisplayFormat(),
                                                                                       pair1.TypeParameter)))
                            bad = True

                        End If
                    End If

                    If AppendUseSiteDiagnostics(useSiteDiagnostics, typeParameter, useSiteDiagnosticsBuilder) Then
                        bad = True
                    End If

                    If bad Then
                        constraints(j) = pair2.ToBad()
                    End If
                Next
            Next

            constraints.Free()
        End Sub

        <Extension()>
        Public Sub CheckAllConstraints(
                                        type As TypeSymbol,
                                        loc As Location,
                                        diagnostics As DiagnosticBag)
            Dim diagnosticsBuilder = ArrayBuilder(Of TypeParameterDiagnosticInfo).GetInstance()
            Dim useSiteDiagnosticsBuilder As ArrayBuilder(Of TypeParameterDiagnosticInfo) = Nothing
            type.CheckAllConstraints(diagnosticsBuilder, useSiteDiagnosticsBuilder)

            If useSiteDiagnosticsBuilder IsNot Nothing Then
                diagnosticsBuilder.AddRange(useSiteDiagnosticsBuilder)
            End If

            For Each diagnostic In diagnosticsBuilder
                diagnostics.Add(diagnostic.DiagnosticInfo, loc)
            Next

            diagnosticsBuilder.Free()
        End Sub

        ''' <summary>
        ''' Check all generic constraints on the given type and any containing types
        ''' (such as A(Of T) in A(Of T).B(Of U)). This includes checking constraints
        ''' on generic types within the type (such as B(Of T) in A(Of B(Of T)())).
        ''' </summary>
        <Extension()>
        Public Sub CheckAllConstraints(
                                        type As TypeSymbol,
                                        diagnosticsBuilder As ArrayBuilder(Of TypeParameterDiagnosticInfo),
                                        <[In], Out> ByRef useSiteDiagnosticsBuilder As ArrayBuilder(Of TypeParameterDiagnosticInfo))
            Dim diagnostics As New CheckConstraintsDiagnosticsBuilders()
            diagnostics.diagnosticsBuilder = diagnosticsBuilder
            diagnostics.useSiteDiagnosticsBuilder = useSiteDiagnosticsBuilder

            type.VisitType(s_checkConstraintsSingleTypeFunc, diagnostics)

            useSiteDiagnosticsBuilder = diagnostics.useSiteDiagnosticsBuilder
        End Sub

        Private Class CheckConstraintsDiagnosticsBuilders
            Public diagnosticsBuilder As ArrayBuilder(Of TypeParameterDiagnosticInfo)
            Public useSiteDiagnosticsBuilder As ArrayBuilder(Of TypeParameterDiagnosticInfo)
        End Class

        Private ReadOnly s_checkConstraintsSingleTypeFunc As Func(Of TypeSymbol, CheckConstraintsDiagnosticsBuilders, Boolean) = AddressOf CheckConstraintsSingleType

        Private Function CheckConstraintsSingleType(type As TypeSymbol, diagnostics As CheckConstraintsDiagnosticsBuilders) As Boolean
            If type.Kind = SymbolKind.NamedType Then
                DirectCast(type, NamedTypeSymbol).CheckConstraints(diagnostics.diagnosticsBuilder, diagnostics.useSiteDiagnosticsBuilder)
            End If
            Return False ' continue walking types
        End Function

        <Extension()>
        Public Function CheckConstraints(
                                        type As NamedTypeSymbol,
                                        typeArgumentsSyntax As SeparatedSyntaxList(Of TypeSyntax),
                                        diagnostics As DiagnosticBag) As Boolean
            Debug.Assert(typeArgumentsSyntax.Count = type.Arity)
            If Not RequiresChecking(type) Then
                Return True
            End If

            Dim diagnosticsBuilder = ArrayBuilder(Of TypeParameterDiagnosticInfo).GetInstance()
            Dim useSiteDiagnosticsBuilder As ArrayBuilder(Of TypeParameterDiagnosticInfo) = Nothing
            Dim result = CheckTypeConstraints(type, diagnosticsBuilder, useSiteDiagnosticsBuilder)

            If useSiteDiagnosticsBuilder IsNot Nothing Then
                diagnosticsBuilder.AddRange(useSiteDiagnosticsBuilder)
            End If

            For Each diagnostic In diagnosticsBuilder
                Dim ordinal = diagnostic.TypeParameter.Ordinal
                Dim location = typeArgumentsSyntax(ordinal).GetLocation()
                diagnostics.Add(diagnostic.DiagnosticInfo, location)
            Next

            diagnosticsBuilder.Free()
            Return result
        End Function

        <Extension()>
        Public Function CheckConstraints(
                                        type As NamedTypeSymbol,
                                        diagnosticsBuilder As ArrayBuilder(Of TypeParameterDiagnosticInfo),
                                        <[In], Out> ByRef useSiteDiagnosticsBuilder As ArrayBuilder(Of TypeParameterDiagnosticInfo)) As Boolean
            If Not RequiresChecking(type) Then
                Return True
            End If
            Return CheckTypeConstraints(type, diagnosticsBuilder, useSiteDiagnosticsBuilder)
        End Function

        <Extension()>
        Public Function CheckConstraints(
                                        method As MethodSymbol,
                                        diagnosticLocation As Location,
                                        diagnostics As DiagnosticBag) As Boolean
            If Not RequiresChecking(method) Then
                Return True
            End If

            Dim diagnosticsBuilder = ArrayBuilder(Of TypeParameterDiagnosticInfo).GetInstance()
            Dim useSiteDiagnosticsBuilder As ArrayBuilder(Of TypeParameterDiagnosticInfo) = Nothing
            Dim result = CheckMethodConstraints(method, diagnosticsBuilder, useSiteDiagnosticsBuilder)

            If useSiteDiagnosticsBuilder IsNot Nothing Then
                diagnosticsBuilder.AddRange(useSiteDiagnosticsBuilder)
            End If

            For Each diagnostic In diagnosticsBuilder
                diagnostics.Add(diagnostic.DiagnosticInfo, diagnosticLocation)
            Next

            diagnosticsBuilder.Free()
            Return result
        End Function

        <Extension()>
        Public Function CheckConstraints(
                                        method As MethodSymbol,
                                        diagnosticsBuilder As ArrayBuilder(Of TypeParameterDiagnosticInfo),
                                        <[In], Out> ByRef useSiteDiagnosticsBuilder As ArrayBuilder(Of TypeParameterDiagnosticInfo)) As Boolean
            If Not RequiresChecking(method) Then
                Return True
            End If
            Return CheckMethodConstraints(method, diagnosticsBuilder, useSiteDiagnosticsBuilder)
        End Function

        Private Function CheckTypeConstraints(
                                        type As NamedTypeSymbol,
                                        diagnosticsBuilder As ArrayBuilder(Of TypeParameterDiagnosticInfo),
                                        <[In], Out> ByRef useSiteDiagnosticsBuilder As ArrayBuilder(Of TypeParameterDiagnosticInfo)) As Boolean
            Dim substitution = type.TypeSubstitution
            Return CheckConstraints(type, substitution, type.OriginalDefinition.TypeParameters, type.TypeArgumentsNoUseSiteDiagnostics, diagnosticsBuilder, useSiteDiagnosticsBuilder)
        End Function

        Private Function CheckMethodConstraints(
                                        method As MethodSymbol,
                                        diagnosticsBuilder As ArrayBuilder(Of TypeParameterDiagnosticInfo),
                                        <[In], Out> ByRef useSiteDiagnosticsBuilder As ArrayBuilder(Of TypeParameterDiagnosticInfo)) As Boolean
            Dim substitution = DirectCast(method, SubstitutedMethodSymbol).TypeSubstitution
            Return CheckConstraints(method, substitution, method.OriginalDefinition.TypeParameters, method.TypeArguments, diagnosticsBuilder, useSiteDiagnosticsBuilder)
        End Function

        ''' <summary>
        ''' Check type parameters for the containing type or method symbol.
        ''' The type parameters are assumed to be the original definitions of type
        ''' parameters from the containing type or method, and the TypeSubstitution
        ''' instance is used for substituting type parameters within the constraints
        ''' of those type parameters, so the substitution should map from type
        ''' parameters to type arguments.
        ''' </summary>
        <Extension()>
        Public Function CheckConstraints(
                                         constructedSymbol As Symbol,
                                         substitution As TypeSubstitution,
                                         typeParameters As ImmutableArray(Of TypeParameterSymbol),
                                         typeArguments As ImmutableArray(Of TypeSymbol),
                                         diagnosticsBuilder As ArrayBuilder(Of TypeParameterDiagnosticInfo),
                                         <[In], Out> ByRef useSiteDiagnosticsBuilder As ArrayBuilder(Of TypeParameterDiagnosticInfo)) As Boolean
            Debug.Assert(typeParameters.Length = typeArguments.Length)

            Dim n = typeParameters.Length
            Dim succeeded = True

            For i = 0 To n - 1
                Dim typeArgument = typeArguments(i)
                Dim typeParameter = typeParameters(i)
                Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
                If Not CheckConstraints(constructedSymbol, substitution, typeParameter, typeArgument, diagnosticsBuilder, useSiteDiagnostics) Then
                    succeeded = False
                End If

                If AppendUseSiteDiagnostics(useSiteDiagnostics, typeParameter, useSiteDiagnosticsBuilder) Then
                    succeeded = False
                End If
            Next

            Return succeeded
        End Function

        Public Function CheckConstraints(
                                 constructedSymbol As Symbol,
                                 substitution As TypeSubstitution,
                                 typeParameter As TypeParameterSymbol,
                                 typeArgument As TypeSymbol,
                                 diagnosticsBuilder As ArrayBuilder(Of TypeParameterDiagnosticInfo),
                                 <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As Boolean
            ' The type parameters must be original definitions of type parameters from the containing symbol.
            Debug.Assert(((constructedSymbol Is Nothing) AndAlso (substitution Is Nothing)) OrElse
                         (typeParameter.ContainingSymbol Is constructedSymbol.OriginalDefinition))

            If typeArgument.IsErrorType() Then
                Return True
            End If

            Dim succeeded = True

            If typeArgument.IsRestrictedType() Then
                If diagnosticsBuilder IsNot Nothing Then
                    ' "'{0}' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement."
                    diagnosticsBuilder.Add(New TypeParameterDiagnosticInfo(typeParameter, ErrorFactory.ErrorInfo(ERRID.ERR_RestrictedType1, typeArgument)))
                End If
                succeeded = False
            End If

            If typeParameter.HasConstructorConstraint AndAlso Not SatisfiesConstructorConstraint(typeParameter, typeArgument, diagnosticsBuilder) Then
                succeeded = False
            End If

            If typeParameter.HasReferenceTypeConstraint AndAlso Not SatisfiesReferenceTypeConstraint(typeParameter, typeArgument, diagnosticsBuilder) Then
                succeeded = False
            End If

            If typeParameter.HasValueTypeConstraint AndAlso Not SatisfiesValueTypeConstraint(constructedSymbol, typeParameter, typeArgument, diagnosticsBuilder, useSiteDiagnostics) Then
                succeeded = False
            End If

            ' The type parameters for a constructed type/method are the type parameters of the ConstructedFrom
            ' type/method, so the constraint types are not substituted. For instance with "Class C(Of T As U, U)",
            ' the type parameter for T in "C(Of Object, Integer)" has constraint "U", not "Integer". We need to
            ' substitute the type parameter constraints from the original definition of the type parameters
            ' using the TypeSubstitution from the constructed type/method.
            For Each t In typeParameter.ConstraintTypesWithDefinitionUseSiteDiagnostics(useSiteDiagnostics)
                Dim constraintType = t.InternalSubstituteTypeParameters(substitution).Type

                If Not SatisfiesTypeConstraint(typeArgument, constraintType, useSiteDiagnostics) Then
                    If diagnosticsBuilder IsNot Nothing Then
                        ' "Type argument '{0}' does not inherit from or implement the constraint type '{1}'."
                        diagnosticsBuilder.Add(New TypeParameterDiagnosticInfo(typeParameter, ErrorFactory.ErrorInfo(ERRID.ERR_GenericConstraintNotSatisfied2, typeArgument, constraintType)))
                    End If
                    succeeded = False
                End If
            Next

            Return succeeded
        End Function

        Private Function AppendUseSiteDiagnostics(
            useSiteDiagnostics As HashSet(Of DiagnosticInfo),
            typeParameter As TypeParameterSymbol,
            <[In], Out> ByRef useSiteDiagnosticsBuilder As ArrayBuilder(Of TypeParameterDiagnosticInfo)
        ) As Boolean
            If useSiteDiagnostics.IsNullOrEmpty Then
                Return False
            End If

            If useSiteDiagnosticsBuilder Is Nothing Then
                useSiteDiagnosticsBuilder = New ArrayBuilder(Of TypeParameterDiagnosticInfo)()
            End If

            For Each info In useSiteDiagnostics
                Debug.Assert(info.Severity = DiagnosticSeverity.Error)
                useSiteDiagnosticsBuilder.Add(New TypeParameterDiagnosticInfo(typeParameter, info))
            Next

            Return True
        End Function

        ''' <summary>
        ''' Return the most derived type from the set of constraint types on this type
        ''' parameter and any type parameter it depends on. Returns Nothing if there
        ''' are no concrete constraint types. If there are multiple constraints, returns
        ''' the most derived, ignoring any subsequent constraints that are neither
        ''' more or less derived. This method assumes there are no constraint cycles.
        ''' </summary>
        <Extension()>
        Public Function GetNonInterfaceConstraint(typeParameter As TypeParameterSymbol, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As TypeSymbol
            Dim result As TypeSymbol = Nothing

            For Each constraint In typeParameter.ConstraintTypesWithDefinitionUseSiteDiagnostics(useSiteDiagnostics)

                Dim candidate As TypeSymbol = Nothing

                Select Case constraint.Kind
                    Case SymbolKind.ErrorType
                        Continue For

                    Case SymbolKind.TypeParameter
                        candidate = DirectCast(constraint, TypeParameterSymbol).GetNonInterfaceConstraint(useSiteDiagnostics)

                    Case Else
                        If Not constraint.IsInterfaceType() Then
                            candidate = constraint
                        End If
                End Select

                If result Is Nothing Then
                    result = candidate
                ElseIf candidate IsNot Nothing Then
                    ' Pick the most derived type
                    If result.IsClassType AndAlso Conversions.IsDerivedFrom(candidate, result, useSiteDiagnostics) Then
                        result = candidate
                    End If
                End If
            Next

            Return result
        End Function

        ''' <summary>
        ''' Return the most derived class type from the set of constraint types on this type
        ''' parameter and any type parameter it depends on. Returns Nothing if there are
        ''' no concrete constraint types. If there are multiple constraints, returns the most
        ''' derived, ignoring any subsequent constraints that are neither more or less derived.
        ''' This method assumes there are no constraint cycles. Unlike GetBaseConstraintType,
        ''' this method will always return a NamedTypeSymbol representing a class: returning
        ''' System.ValueType for value types, System.Array for arrays, and System.Enum for enums.
        ''' </summary>
        <Extension()>
        Public Function GetClassConstraint(typeParameter As TypeParameterSymbol, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As NamedTypeSymbol
            Dim baseType = typeParameter.GetNonInterfaceConstraint(useSiteDiagnostics)

            If baseType Is Nothing Then
                Return Nothing
            End If

            Select Case baseType.TypeKind
                Case TypeKind.Array,
                    TypeKind.Enum,
                    TypeKind.Structure
                    Return baseType.BaseTypeWithDefinitionUseSiteDiagnostics(useSiteDiagnostics)

                Case Else
                    Debug.Assert(Not baseType.IsInterfaceType())
                    Debug.Assert(baseType.TypeKind <> TypeKind.TypeParameter)
                    Return DirectCast(baseType, NamedTypeSymbol)

            End Select
        End Function

        ''' <summary>
        ''' Populate the collection with all constraints for the type parameter, traversing
        ''' any constraints that are also type parameters. The result is a collection of type
        ''' and flag constraints, with no type parameter references. This method assumes
        ''' there are no constraint cycles.
        ''' </summary>
        <Extension()>
        Private Sub GetAllConstraints(
                                     typeParameter As TypeParameterSymbol,
                                     constraintsBuilder As ArrayBuilder(Of TypeParameterAndConstraint),
                                     fromConstraintOpt As TypeParameterConstraint?)
            Dim constraints = ArrayBuilder(Of TypeParameterConstraint).GetInstance()
            typeParameter.GetConstraints(constraints)

            For Each constraint In constraints
                Dim type = constraint.TypeConstraint

                If type IsNot Nothing Then
                    Select Case type.TypeKind
                        Case TypeKind.TypeParameter
                            ' Add constraints from type parameter.
                            DirectCast(type, TypeParameterSymbol).GetAllConstraints(constraintsBuilder, If(fromConstraintOpt.HasValue, fromConstraintOpt.Value, constraint))
                            Continue For

                        Case TypeKind.Error
                            ' Skip error types.
                            Continue For

                    End Select
                End If

                constraintsBuilder.Add(
                    If(fromConstraintOpt.HasValue,
                       New TypeParameterAndConstraint(DirectCast(fromConstraintOpt.Value.TypeConstraint, TypeParameterSymbol), constraint.AtLocation(fromConstraintOpt.Value.LocationOpt)),
                       New TypeParameterAndConstraint(typeParameter, constraint)))
            Next

            constraints.Free()
        End Sub

        ''' <summary>
        ''' A tuple of type parameter and constraint type.
        ''' </summary>
        Private Structure TypeParameterAndConstraint
            Public Sub New(typeParameter As TypeParameterSymbol, constraint As TypeParameterConstraint, Optional isBad As Boolean = False)
                Me.TypeParameter = typeParameter
                Me.Constraint = constraint
                Me.IsBad = isBad
            End Sub

            Public ReadOnly TypeParameter As TypeParameterSymbol
            Public ReadOnly Constraint As TypeParameterConstraint
            Public ReadOnly IsBad As Boolean

            Public Function ToBad() As TypeParameterAndConstraint
                Debug.Assert(Not IsBad)
                Return New TypeParameterAndConstraint(TypeParameter, Constraint, True)
            End Function

            Public Overrides Function ToString() As String
                Dim result = String.Format("{0} : {1}", TypeParameter, Constraint)
                If IsBad Then
                    result = result & " (bad)"
                End If
                Return result
            End Function
        End Structure

        Private Function SatisfiesTypeConstraint(
            typeArgument As TypeSymbol,
            constraintType As TypeSymbol,
            <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
        ) As Boolean
            If constraintType.IsErrorType() Then
                constraintType.AddUseSiteDiagnostics(useSiteDiagnostics)
                Return False
            End If

            Return Conversions.HasWideningDirectCastConversionButNotEnumTypeConversion(typeArgument, constraintType, useSiteDiagnostics)
        End Function

        ' See Bindable::ValidateNewConstraintForType.
        Private Function SatisfiesConstructorConstraint(
                                                       typeParameter As TypeParameterSymbol,
                                                       typeArgument As TypeSymbol,
                                                       diagnosticsBuilder As ArrayBuilder(Of TypeParameterDiagnosticInfo)) As Boolean
            Debug.Assert(typeParameter.HasConstructorConstraint)

            Select Case typeArgument.TypeKind
                Case TypeKind.Enum,
                    TypeKind.Structure
                    Return True

                Case TypeKind.TypeParameter
                    If DirectCast(typeArgument, TypeParameterSymbol).HasConstructorConstraint OrElse typeArgument.IsValueType Then
                        Return True
                    Else
                        If diagnosticsBuilder IsNot Nothing Then
                            ' "Type parameter '{0}' must have either a 'New' constraint or a 'Structure' constraint to satisfy the 'New' constraint for type parameter '{1}'."
                            diagnosticsBuilder.Add(New TypeParameterDiagnosticInfo(typeParameter, ErrorFactory.ErrorInfo(ERRID.ERR_BadGenericParamForNewConstraint2, typeArgument, typeParameter)))
                        End If
                        Return False
                    End If

                Case Else
                    If typeArgument.TypeKind = TypeKind.Class Then
                        Dim classType = DirectCast(typeArgument, NamedTypeSymbol)

                        If HasPublicParameterlessConstructor(classType) Then
                            If classType.IsMustInherit Then
                                If diagnosticsBuilder IsNot Nothing Then
                                    ' "Type argument '{0}' is declared 'MustInherit' and does not satisfy the 'New' constraint for type parameter '{1}'."
                                    diagnosticsBuilder.Add(New TypeParameterDiagnosticInfo(typeParameter, ErrorFactory.ErrorInfo(ERRID.ERR_MustInheritForNewConstraint2, typeArgument, typeParameter)))
                                End If
                                Return False
                            Else
                                Return True
                            End If
                        End If

                    End If

                    If diagnosticsBuilder IsNot Nothing Then
                        ' "Type argument '{0}' must have a public parameterless instance constructor to satisfy the 'New' constraint for type parameter '{1}'."
                        diagnosticsBuilder.Add(New TypeParameterDiagnosticInfo(typeParameter, ErrorFactory.ErrorInfo(ERRID.ERR_NoSuitableNewForNewConstraint2, typeArgument, typeParameter)))
                    End If
                    Return False

            End Select
        End Function

        ' See Bindable::ValidateReferenceConstraintForType.
        Private Function SatisfiesReferenceTypeConstraint(
                                                       typeParameter As TypeParameterSymbol,
                                                       typeArgument As TypeSymbol,
                                                       diagnosticsBuilder As ArrayBuilder(Of TypeParameterDiagnosticInfo)) As Boolean
            Debug.Assert((typeParameter Is Nothing) OrElse typeParameter.HasReferenceTypeConstraint)

            If Not typeArgument.IsReferenceType Then
                If diagnosticsBuilder IsNot Nothing Then
                    ' "Type argument '{0}' does not satisfy the 'Class' constraint for type parameter '{1}'."
                    diagnosticsBuilder.Add(New TypeParameterDiagnosticInfo(typeParameter, ErrorFactory.ErrorInfo(ERRID.ERR_BadTypeArgForRefConstraint2, typeArgument, typeParameter)))
                End If

                Return False
            End If

            Return True
        End Function

        ' See Bindable::ValidateValueConstraintForType.
        Private Function SatisfiesValueTypeConstraint(
                                                     constructedSymbol As Symbol,
                                                     typeParameter As TypeParameterSymbol,
                                                     typeArgument As TypeSymbol,
                                                     diagnosticsBuilder As ArrayBuilder(Of TypeParameterDiagnosticInfo),
                                                     <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As Boolean
            Debug.Assert((typeParameter Is Nothing) OrElse typeParameter.HasValueTypeConstraint)

            If Not typeArgument.IsValueType Then
                If diagnosticsBuilder IsNot Nothing Then
                    Dim containingType = TryCast(constructedSymbol, TypeSymbol)

                    If (containingType IsNot Nothing) AndAlso containingType.IsNullableType() Then
                        ' "Type '{0}' must be a value type or a type argument constrained to 'Structure' in order to be used with 'Nullable' or nullable modifier '?'."
                        diagnosticsBuilder.Add(New TypeParameterDiagnosticInfo(typeParameter, ErrorFactory.ErrorInfo(ERRID.ERR_BadTypeArgForStructConstraintNull, typeArgument)))
                    Else
                        ' "Type argument '{0}' does not satisfy the 'Structure' constraint for type parameter '{1}'."
                        diagnosticsBuilder.Add(New TypeParameterDiagnosticInfo(typeParameter, ErrorFactory.ErrorInfo(ERRID.ERR_BadTypeArgForStructConstraint2, typeArgument, typeParameter)))
                    End If
                End If

                Return False

            ElseIf IsNullableTypeOrTypeParameter(typeArgument, useSiteDiagnostics) Then
                If diagnosticsBuilder IsNot Nothing Then
                    ' "'System.Nullable' does not satisfy the 'Structure' constraint for type parameter '{0}'. Only non-nullable 'Structure' types are allowed."
                    diagnosticsBuilder.Add(New TypeParameterDiagnosticInfo(typeParameter, ErrorFactory.ErrorInfo(ERRID.ERR_NullableDisallowedForStructConstr1, typeParameter)))
                End If

                Return False
            End If

            Return True
        End Function

        ' See Bindable::ConstraintsConflict.
        Private Function HasConflict(constraint1 As TypeParameterConstraint, constraint2 As TypeParameterConstraint, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As Boolean

            Dim constraintType1 = constraint1.TypeConstraint
            Dim constraintType2 = constraint2.TypeConstraint

            If (constraintType1 IsNot Nothing) AndAlso (constraintType1.IsInterfaceType()) Then
                Return False
            End If

            If (constraintType2 IsNot Nothing) AndAlso (constraintType2.IsInterfaceType()) Then
                Return False
            End If

            If constraint1.IsValueTypeConstraint Then
                If HasValueTypeConstraintConflict(constraint2, useSiteDiagnostics) Then
                    Return True
                End If
            ElseIf constraint2.IsValueTypeConstraint Then
                If HasValueTypeConstraintConflict(constraint1, useSiteDiagnostics) Then
                    Return True
                End If
            End If

            If constraint1.IsReferenceTypeConstraint Then
                If HasReferenceTypeConstraintConflict(constraint2) Then
                    Return True
                End If
            ElseIf constraint2.IsReferenceTypeConstraint Then
                If HasReferenceTypeConstraintConflict(constraint1) Then
                    Return True
                End If
            End If

            If (constraintType1 IsNot Nothing) AndAlso
                (constraintType2 IsNot Nothing) AndAlso
                Not SatisfiesTypeConstraint(constraintType1, constraintType2, useSiteDiagnostics) AndAlso
                Not SatisfiesTypeConstraint(constraintType2, constraintType1, useSiteDiagnostics) Then
                Return True
            End If

            Return False
        End Function

        Private Function HasValueTypeConstraintConflict(constraint As TypeParameterConstraint, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As Boolean
            Dim constraintType = constraint.TypeConstraint
            If constraintType Is Nothing Then
                Return False
            End If

            If SatisfiesValueTypeConstraint(constructedSymbol:=Nothing, typeParameter:=Nothing, typeArgument:=constraintType,
                                            diagnosticsBuilder:=Nothing,
                                            useSiteDiagnostics:=useSiteDiagnostics) Then
                Return False
            End If

            Select Case constraintType.SpecialType
                Case SpecialType.System_Object, SpecialType.System_ValueType
                    Return False
            End Select

            Return True
        End Function

        Private Function HasReferenceTypeConstraintConflict(constraint As TypeParameterConstraint) As Boolean
            Dim constraintType = constraint.TypeConstraint
            If constraintType Is Nothing Then
                Return False
            End If

            If SatisfiesReferenceTypeConstraint(typeParameter:=Nothing, typeArgument:=constraintType, diagnosticsBuilder:=Nothing) Then
                Return False
            End If

            Return True
        End Function

        Private Function IsNullableTypeOrTypeParameter(type As TypeSymbol, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As Boolean
            If type.TypeKind = TypeKind.TypeParameter Then
                Dim typeParameter = DirectCast(type, TypeParameterSymbol)

                Dim constraintTypes = typeParameter.ConstraintTypesWithDefinitionUseSiteDiagnostics(useSiteDiagnostics)
                For Each constraintType In constraintTypes
                    If IsNullableTypeOrTypeParameter(constraintType, useSiteDiagnostics) Then
                        Return True
                    End If
                Next
                Return False
            Else
                Return type.IsNullableType()
            End If
        End Function

        Private Function GetConstraintCycleInfo(cycle As ConsList(Of TypeParameterSymbol)) As CompoundDiagnosticInfo
            Debug.Assert(cycle.Any())
            Dim previous As TypeParameterSymbol = Nothing
            Dim builder = ArrayBuilder(Of DiagnosticInfo).GetInstance()
            builder.Add(Nothing) ' Placeholder for first entry added later.
            For Each typeParameter In cycle
                If previous IsNot Nothing Then
                    builder.Add(ErrorFactory.ErrorInfo(ERRID.ERR_ConstraintCycleLink2, typeParameter, previous))
                End If
                previous = typeParameter
            Next
            builder(0) = ErrorFactory.ErrorInfo(ERRID.ERR_ConstraintCycleLink2, cycle.Head, previous)
            Dim diagnostics = builder.ToArrayAndFree()
            Array.Reverse(diagnostics)
            Return New CompoundDiagnosticInfo(diagnostics)
        End Function

        ''' <summary>
        ''' Return true if the class type has a public parameterless constructor.
        ''' </summary>
        Public Function HasPublicParameterlessConstructor(type As NamedTypeSymbol) As Boolean
            type = type.OriginalDefinition
            Debug.Assert(type.TypeKind = TypeKind.Class)

            Dim sourceNamedType = TryCast(type, SourceNamedTypeSymbol)

            If sourceNamedType IsNot Nothing AndAlso Not sourceNamedType.MembersHaveBeenCreated Then
                ' When we are dealing with group classes and synthetic entry points,
                ' we can end up here while we are building the set of members for the type.
                ' Using InstanceConstructors property will send us into an infinite loop.
                Return sourceNamedType.InferFromSyntaxIfClassWillHavePublicParameterlessConstructor()
            End If

            For Each constructor In type.InstanceConstructors
                If constructor.ParameterCount = 0 Then
                    Return constructor.DeclaredAccessibility = Accessibility.Public
                End If
            Next
            Return False
        End Function

        ''' <summary>
        ''' Return true if the constraints collection contains the given type constraint.
        ''' </summary>
        Private Function ContainsTypeConstraint(constraints As ArrayBuilder(Of TypeParameterConstraint), constraintType As TypeSymbol) As Boolean
            Debug.Assert(constraintType IsNot Nothing)
            For Each constraint In constraints
                Dim type = constraint.TypeConstraint
                If (type IsNot Nothing) AndAlso constraintType.IsSameTypeIgnoringCustomModifiers(type) Then
                    Return True
                End If
            Next
            Return False
        End Function

        Private Function RequiresChecking(type As NamedTypeSymbol) As Boolean
            If type.Arity = 0 Then
                Return False
            End If

            ' If type is the original definition, there is no need
            ' to check constraints. In the following for instance:
            ' Class A(Of T As Structure)
            '     Dim F As New A(Of T)()
            ' End Class
            If type.OriginalDefinition Is type Then
                Return False
            End If

            Debug.Assert(type.ConstructedFrom <> type)
            Return True
        End Function

        Private Function RequiresChecking(method As MethodSymbol) As Boolean
            If Not method.IsGenericMethod Then
                Return False
            End If

            ' If method is the original definition, there is no need
            ' to check constraints. In the following for instance:
            ' Sub M(Of T As Class)()
            '     M(Of T)()
            ' End Function
            If method.OriginalDefinition Is method Then
                Return False
            End If

            Debug.Assert(method.ConstructedFrom <> method)
            Return True
        End Function

    End Module

End Namespace
