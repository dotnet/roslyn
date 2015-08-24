' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend Enum SymbolComparisonResults
        NameMismatch = 1 << 0
        ReturnTypeMismatch = 1 << 1
        ArityMismatch = 1 << 2
        ConstraintMismatch = 1 << 3
        CallingConventionMismatch = 1 << 4
        CustomModifierMismatch = 1 << 5
        ''' <summary> 
        ''' One of the methods has more parameters than the other 
        ''' AND 
        ''' at least one of the extra parameters is NOT optional
        ''' </summary>
        RequiredExtraParameterMismatch = 1 << 6
        ''' <summary> 
        ''' One of the methods has more parameters than the other 
        ''' AND at least one of the extra parameters IS optional
        ''' OR 
        ''' there is at least one parameter in one method with optionality (being optional or 
        ''' required) not equal to that of the matching parameter from the other method
        ''' </summary>
        OptionalParameterMismatch = 1 << 7
        RequiredParameterTypeMismatch = 1 << 8
        OptionalParameterTypeMismatch = 1 << 9
        OptionalParameterValueMismatch = 1 << 10
        ParameterByrefMismatch = 1 << 11
        ParamArrayMismatch = 1 << 12
        PropertyAccessorMismatch = 1 << 13
        VarargMismatch = 1 << 14
        ''' <summary>
        ''' Mismatch in total number of parameters, both required and optional
        ''' </summary>
        ''' <remarks></remarks>
        TotalParameterCountMismatch = 1 << 15

        AllParameterMismatches =
            OptionalParameterMismatch Or
            RequiredExtraParameterMismatch Or
            TotalParameterCountMismatch Or
            OptionalParameterTypeMismatch Or
            RequiredParameterTypeMismatch Or
            CustomModifierMismatch Or
            ParameterByrefMismatch Or
            ParamArrayMismatch Or
            OptionalParameterValueMismatch

        AllMismatches = (1 << 16) - 1

        ' The set of mismatches for DetailedCompare that are ignored
        ' when testing for conflicting method in a class, or first 
        ' pass of finding an overridden method. See 4.1.1 of language spec.
        MismatchesForConflictingMethods =
            ReturnTypeMismatch Or
            ParameterByrefMismatch Or
            ParamArrayMismatch Or
            ConstraintMismatch Or
            CustomModifierMismatch Or
            OptionalParameterMismatch Or
            OptionalParameterValueMismatch Or
            PropertyAccessorMismatch Or
            CallingConventionMismatch

        ' The set of mismatches for DetailedCompare that are ignored
        ' when finding the method/property that was implemented.
        ' Note that constraints are validated later, so are ignored
        ' when finding the method.
        MismatchesForExplicitInterfaceImplementations =
            NameMismatch Or
            ConstraintMismatch Or
            CustomModifierMismatch Or
            PropertyAccessorMismatch
    End Enum

    ''' <summary>
    ''' Implementation of IEqualityComparer for MethodSymbols, with options for various aspects
    ''' to compare.
    ''' </summary>
    Friend NotInheritable Class MethodSignatureComparer
        Implements IEqualityComparer(Of MethodSymbol)

        ''' <summary>
        ''' This instance is intended to reflect the definition of signature equality used by the runtime (ECMA 335 Section 8.6.1.6).
        ''' It considers return type, name, parameters, calling convention, and custom modifiers.
        ''' </summary>
        Public Shared ReadOnly RuntimeMethodSignatureComparer As MethodSignatureComparer =
            New MethodSignatureComparer(considerName:=True,
                                        considerReturnType:=True,
                                        considerTypeConstraints:=False,
                                        considerByRef:=True,
                                        considerCallingConvention:=True,
                                        considerCustomModifiers:=True)

        ''' <summary>
        ''' This instance is used to compare all aspects.
        ''' </summary>
        Public Shared ReadOnly AllAspectsSignatureComparer As MethodSignatureComparer =
            New MethodSignatureComparer(considerName:=True,
                                        considerReturnType:=True,
                                        considerTypeConstraints:=True,
                                        considerByRef:=True,
                                        considerCallingConvention:=True,
                                        considerCustomModifiers:=True)

        ''' <summary>
        ''' This instance is used to compare parameter and return types, including byref.
        ''' </summary>
        Public Shared ReadOnly ParametersAndReturnTypeSignatureComparer As MethodSignatureComparer =
            New MethodSignatureComparer(considerName:=False,
                                        considerReturnType:=True,
                                        considerTypeConstraints:=False,
                                        considerByRef:=True,
                                        considerCallingConvention:=False,
                                        considerCustomModifiers:=False)

        ''' <summary>
        ''' This instance is used to compare custom modifiers, parameter and return types, including byref.
        ''' </summary>
        Public Shared ReadOnly CustomModifiersAndParametersAndReturnTypeSignatureComparer As MethodSignatureComparer =
            New MethodSignatureComparer(considerName:=False,
                                        considerReturnType:=True,
                                        considerTypeConstraints:=False,
                                        considerByRef:=True,
                                        considerCallingConvention:=False,
                                        considerCustomModifiers:=True)

        ''' <summary>
        ''' This instance is used to search for methods that have the same signature, return type,
        ''' and constraints according to the VisualBasic definition.  Custom modifiers are ignored.
        ''' </summary>
        Public Shared ReadOnly VisualBasicSignatureAndConstraintsAndReturnTypeComparer As MethodSignatureComparer =
            New MethodSignatureComparer(considerName:=True,
                                        considerReturnType:=True,
                                        considerTypeConstraints:=True,
                                        considerByRef:=True,
                                        considerCallingConvention:=True,
                                        considerCustomModifiers:=False)

        ''' <summary>
        ''' This instance is used to search for methods that have identical signatures in every regard.
        ''' </summary>
        Public Shared ReadOnly RetargetedExplicitMethodImplementationComparer As MethodSignatureComparer =
            New MethodSignatureComparer(considerName:=True,
                                        considerReturnType:=True,
                                        considerTypeConstraints:=False,
                                        considerByRef:=True,
                                        considerCallingConvention:=True,
                                        considerCustomModifiers:=True)

        ''' <summary>
        ''' This instance is used to compare potential WinRT fake methods in type projection.
        ''' 
        ''' FIXME(angocke): This is almost certainly wrong. The semantics of WinRT conflict 
        ''' comparison should probably match overload resolution (i.e., we should not add a member
        '''  to lookup that would result in ambiguity), but this is closer to what Dev12 does.
        ''' 
        ''' The real fix here is to establish a spec for how WinRT conflict comparison should be
        ''' performed. Once this is done we should remove these comments.
        ''' </summary>
        Public Shared ReadOnly WinRTConflictComparer As MethodSignatureComparer =
            New MethodSignatureComparer(considerName:=True,
                                        considerReturnType:=False,
                                        considerTypeConstraints:=False,
                                        considerByRef:=False,
                                        considerCallingConvention:=False,
                                        considerCustomModifiers:=False)


#If False Then
        ' These are various method signature comparers that C# used. I will enable various ones
        ' with the correct options, on an as needed basis.

        ''' <summary>
        ''' This instance is used when trying to determine if one method implicitly implements another,
        ''' according to the C# definition.
        ''' The method signatures, type constraints, and return types must match.  Custom modifiers are ignored.
        ''' </summary>
        Public Shared ReadOnly CSharpImplicitMethodImplementationComparer As MethodSignatureComparer =
            New MethodSignatureComparer(considerName:=True,
                                        considerReturnType:=True,
                                        considerTypeConstraints:=True,
                                        considerCallingConvention:=True,
                                        considerCustomModifiers:=False)

        ''' <summary>
        ''' This instance is used to search for methods that would conflict with declared property accessors.
        ''' </summary>
        Public Shared ReadOnly PropertyAccessorComparer As MethodSignatureComparer =
            New MethodSignatureComparer(considerName:=True,
                                        considerReturnType:=False,
                                        considerTypeConstraints:=False,
                                        considerCallingConvention:=True,
                                        considerCustomModifiers:=False)

        ''' <summary>
        ''' This instance is used to check whether one method overrides another, according to the C# definition.
        ''' </summary>
        Public Shared ReadOnly CSharpMethodOverrideComparer As MethodSignatureComparer =
            New MethodSignatureComparer(considerName:=True,
                                        considerReturnType:=False,
                                        considerTypeConstraints:=False,
                                        considerCallingConvention:=False,
                                        considerCustomModifiers:=False)

        ''' <summary>
        ''' Same as CSharpMethodOverrideComparer except that it pays attention to custom modifiers.  
        ''' </summary>
        Public Shared ReadOnly CSharpCustomModifierMethodOverrideComparer As MethodSignatureComparer =
            New MethodSignatureComparer(considerName:=True,
                                        considerReturnType:=False,
                                        considerTypeConstraints:=False,
                                        considerCallingConvention:=False,
                                        considerCustomModifiers:=True)

        ''' <summary>
        ''' This instance is the same as RuntimeMethodSignatureComparer, except that it also considers
        ''' type constraints (as required by the C# definition of implicit method implementation).
        ''' CONSIDER: just use RuntimeMethodSignatureComparer?
        ''' </summary>
        Public Shared ReadOnly RuntimeImplicitMethodImplementationComparer As MethodSignatureComparer =
            New MethodSignatureComparer(considerName:=True,
                                        considerReturnType:=True,
                                        considerTypeConstraints:=True,
                                        considerCallingConvention:=True,
                                        considerCustomModifiers:=True)

#End If

        ' Compare the "unqualified" part of the method name (no explicit part)
        Private ReadOnly _considerName As Boolean

        ' Compare the type symbols of the return types
        Private ReadOnly _considerReturnType As Boolean

        ' Compare the type constraints
        Private ReadOnly _considerTypeConstraints As Boolean

        ' Compare the full calling conventions.  Still compares varargs if false.
        Private ReadOnly _considerCallingConvention As Boolean

        ' Consider byref during matching
        Private ReadOnly _considerByRef As Boolean

        ' Consider custom modifiers on/in parameters and return types (if return is considered).
        Private ReadOnly _considerCustomModifiers As Boolean

        Private Sub New(considerName As Boolean,
                        considerReturnType As Boolean,
                        considerTypeConstraints As Boolean,
                        considerCallingConvention As Boolean,
                        considerByRef As Boolean,
                        considerCustomModifiers As Boolean)
            Me._considerName = considerName
            Me._considerReturnType = considerReturnType
            Me._considerTypeConstraints = considerTypeConstraints
            Me._considerCallingConvention = considerCallingConvention
            Me._considerByRef = considerByRef
            Me._considerCustomModifiers = considerCustomModifiers
        End Sub

#Region "IEqualityComparer(Of MethodSymbol) Members"

        Public Overloads Function Equals(method1 As MethodSymbol, method2 As MethodSymbol) As Boolean _
            Implements IEqualityComparer(Of MethodSymbol).Equals

            If method1 Is method2 Then
                Return True
            End If

            If method1 Is Nothing OrElse method2 Is Nothing Then
                Return False
            End If

            If method1.Arity <> method2.Arity Then
                Return False
            End If

            If _considerName Then
                If Not IdentifierComparison.Equals(method1.Name, method2.Name) Then
                    Return False
                End If
            End If

            Dim typeSubstitution1 = GetTypeSubstitution(method1)
            Dim typeSubstitution2 = GetTypeSubstitution(method2)
            If _considerReturnType Then
                If Not HaveSameReturnTypes(method1, typeSubstitution1, method2, typeSubstitution2, _considerCustomModifiers) Then
                    Return False
                End If
            End If

            If method1.ParameterCount > 0 OrElse method2.ParameterCount > 0 Then
                If Not HaveSameParameterTypes(method1.Parameters, typeSubstitution1, method2.Parameters, typeSubstitution2,
                                              _considerByRef, _considerCustomModifiers) Then
                    Return False
                End If
            End If

            If _considerCallingConvention Then
                If method1.CallingConvention <> method2.CallingConvention Then
                    Return False
                End If
            Else
                If method1.IsVararg <> method2.IsVararg Then
                    Return False
                End If
            End If

            If _considerTypeConstraints Then
                If Not HaveSameConstraints(method1, typeSubstitution1, method2, typeSubstitution2) Then
                    Return False
                End If
            End If

            Return True
        End Function

        Public Overloads Function GetHashCode(method As MethodSymbol) As Integer _
            Implements IEqualityComparer(Of MethodSymbol).GetHashCode

            Dim _hash As Integer = 1
            If method IsNot Nothing Then
                If _considerName Then
                    _hash = Hash.Combine(method.Name, _hash)
                End If

                If _considerReturnType AndAlso Not method.IsGenericMethod AndAlso Not _considerCustomModifiers Then
                    _hash = Hash.Combine(method.ReturnType, _hash)
                End If

                ' CONSIDER: modify hash for constraints?

                _hash = Hash.Combine(_hash, method.Arity)
                _hash = Hash.Combine(_hash, method.ParameterCount)
                _hash = Hash.Combine(method.IsVararg, _hash)
            End If

            Return _hash
        End Function
#End Region

#Region "Detailed comparison functions"
        Public Shared Function DetailedCompare(
            method1 As MethodSymbol,
            method2 As MethodSymbol,
            comparisons As SymbolComparisonResults,
            Optional stopIfAny As SymbolComparisonResults = 0
        ) As SymbolComparisonResults
            Dim results As SymbolComparisonResults = Nothing

            If method1 = method2 Then
                Return Nothing
            End If

            If (comparisons And SymbolComparisonResults.ArityMismatch) <> 0 Then
                If method1.Arity <> method2.Arity Then
                    results = results Or SymbolComparisonResults.ArityMismatch
                    If (stopIfAny And SymbolComparisonResults.ArityMismatch) <> 0 Then
                        GoTo Done
                    End If
                End If
            End If

            If (stopIfAny And SymbolComparisonResults.TotalParameterCountMismatch) <> 0 Then
                If method1.ParameterCount <> method2.ParameterCount Then
                    results = results Or SymbolComparisonResults.TotalParameterCountMismatch
                    GoTo Done
                End If
            End If

            Dim typeSubstitution1 As New LazyTypeSubstitution(method1)
            Dim typeSubstitution2 As New LazyTypeSubstitution(method2)

            If (comparisons And (SymbolComparisonResults.ReturnTypeMismatch Or SymbolComparisonResults.CustomModifierMismatch)) <> 0 Then
                results = results Or DetailedReturnTypeCompare(New TypeWithModifiers(method1.OriginalDefinition.ReturnType, method1.OriginalDefinition.ReturnTypeCustomModifiers),
                                                               typeSubstitution1.Value,
                                                               New TypeWithModifiers(method2.OriginalDefinition.ReturnType, method2.OriginalDefinition.ReturnTypeCustomModifiers),
                                                               typeSubstitution2.Value,
                                                               comparisons)
                If (stopIfAny And results) <> 0 Then
                    GoTo Done
                End If
            End If

            If (comparisons And SymbolComparisonResults.AllParameterMismatches) <> 0 Then
                results = results Or DetailedParameterCompare(method1.Parameters, typeSubstitution1,
                                                              method2.Parameters, typeSubstitution2,
                                                              comparisons, stopIfAny)
                If (stopIfAny And results) <> 0 Then
                    GoTo Done
                End If
            End If

            If (comparisons And SymbolComparisonResults.CallingConventionMismatch) <> 0 Then
                If method1.CallingConvention <> method2.CallingConvention Then
                    results = results Or SymbolComparisonResults.CallingConventionMismatch
                    If (stopIfAny And SymbolComparisonResults.CallingConventionMismatch) <> 0 Then
                        GoTo Done
                    End If
                End If
            End If

            If (comparisons And SymbolComparisonResults.VarargMismatch) <> 0 Then
                If method1.IsVararg <> method2.IsVararg Then
                    results = results Or SymbolComparisonResults.VarargMismatch
                    If (stopIfAny And SymbolComparisonResults.VarargMismatch) <> 0 Then
                        GoTo Done
                    End If
                End If
            End If

            If (comparisons And SymbolComparisonResults.ConstraintMismatch) <> 0 Then
                ' If the arity is different, we cannot compare constraints. We can't just return
                ' ConstraintMismatch if the arity is different though since, if there are no
                ' constraints, then arguably the constraints do match. Therefore, the caller
                ' must check for ArityMismatch when checking for ConstraintMismatch.
                Debug.Assert((comparisons And SymbolComparisonResults.ArityMismatch) <> 0)

                If ((results And SymbolComparisonResults.ArityMismatch) = 0) AndAlso
                    Not HaveSameConstraints(method1, typeSubstitution1.Value, method2, typeSubstitution2.Value) Then
                    results = results Or SymbolComparisonResults.ConstraintMismatch
                    If (stopIfAny And SymbolComparisonResults.ConstraintMismatch) <> 0 Then
                        GoTo Done
                    End If
                End If
            End If

            ' It turns out name comparison is rather expensive relative to the other checks.
            If (comparisons And SymbolComparisonResults.NameMismatch) <> 0 Then
                If Not IdentifierComparison.Equals(method1.Name, method2.Name) Then
                    results = results Or SymbolComparisonResults.NameMismatch
                    If (stopIfAny And SymbolComparisonResults.NameMismatch) <> 0 Then
                        GoTo Done
                    End If
                End If
            End If

Done:
            Return results And comparisons
        End Function

        Public Structure LazyTypeSubstitution
            Private _typeSubstitution As TypeSubstitution
            Private _method As MethodSymbol

            Public Sub New(method As MethodSymbol)
                _method = method
            End Sub

            Public ReadOnly Property Value As TypeSubstitution
                Get
                    If _typeSubstitution Is Nothing AndAlso _method IsNot Nothing Then
                        _typeSubstitution = GetTypeSubstitution(_method)
                        _method = Nothing
                    End If

                    Return _typeSubstitution
                End Get
            End Property
        End Structure

        ' Compare two return types and return the detailed comparison of them.
        Public Shared Function DetailedReturnTypeCompare(
            type1 As TypeWithModifiers,
            typeSubstitution1 As TypeSubstitution,
            type2 As TypeWithModifiers,
            typeSubstitution2 As TypeSubstitution,
            comparisons As SymbolComparisonResults
        ) As SymbolComparisonResults
            type1 = SubstituteType(typeSubstitution1, type1)
            type2 = SubstituteType(typeSubstitution2, type2)

            If Not type1.Type.IsSameTypeIgnoringCustomModifiers(type2.Type) Then
                Return SymbolComparisonResults.ReturnTypeMismatch
            ElseIf (comparisons And SymbolComparisonResults.CustomModifierMismatch) <> 0 AndAlso
                   (type1 <> type2) Then
                Return SymbolComparisonResults.CustomModifierMismatch
            End If

            Return Nothing
        End Function

        Public Shared Function DetailedParameterCompare(
            params1 As ImmutableArray(Of ParameterSymbol),
            <[In]> ByRef lazyTypeSubstitution1 As LazyTypeSubstitution,
            params2 As ImmutableArray(Of ParameterSymbol),
            <[In]> ByRef lazyTypeSubstitution2 As LazyTypeSubstitution,
            comparisons As SymbolComparisonResults,
            Optional stopIfAny As SymbolComparisonResults = 0
        ) As SymbolComparisonResults
            Dim results As SymbolComparisonResults = Nothing

            Dim commonParamCount As Integer
            Dim longerParameters As ImmutableArray(Of ParameterSymbol)

            If params1.Length > params2.Length Then
                commonParamCount = params2.Length
                longerParameters = params1
            ElseIf params1.Length < params2.Length Then
                commonParamCount = params1.Length
                longerParameters = params2
            Else
                commonParamCount = params1.Length
                longerParameters = Nothing
            End If

            If Not longerParameters.IsDefault Then
                results = results Or SymbolComparisonResults.TotalParameterCountMismatch
                If (stopIfAny And SymbolComparisonResults.TotalParameterCountMismatch) <> 0 Then
                    GoTo Done
                End If

                For i As Integer = commonParamCount To longerParameters.Length - 1
                    If longerParameters(i).IsOptional Then
                        results = results Or SymbolComparisonResults.OptionalParameterMismatch
                        If (stopIfAny And SymbolComparisonResults.OptionalParameterMismatch) <> 0 Then
                            GoTo Done
                        End If
                    Else
                        results = results Or SymbolComparisonResults.RequiredExtraParameterMismatch
                        If (stopIfAny And SymbolComparisonResults.RequiredExtraParameterMismatch) <> 0 Then
                            GoTo Done
                        End If
                    End If
                Next
            End If

            If commonParamCount <> 0 Then

                Dim typeSubstitution1 As TypeSubstitution
                Dim typeSubstitution2 As TypeSubstitution
                Dim checkTypes As Boolean

                If (comparisons And
                    (SymbolComparisonResults.OptionalParameterTypeMismatch Or
                     SymbolComparisonResults.RequiredParameterTypeMismatch Or
                     SymbolComparisonResults.CustomModifierMismatch)) <> 0 Then
                    checkTypes = True
                    typeSubstitution1 = lazyTypeSubstitution1.Value
                    typeSubstitution2 = lazyTypeSubstitution2.Value
                Else
                    checkTypes = False
                    typeSubstitution1 = Nothing
                    typeSubstitution2 = Nothing
                End If

                For i As Integer = 0 To commonParamCount - 1
                    Dim param1 = params1(i)
                    Dim param2 = params2(i)
                    Dim bothOptional As Boolean = param1.IsOptional AndAlso param2.IsOptional

                    If param1.IsOptional <> param2.IsOptional Then
                        results = results Or SymbolComparisonResults.OptionalParameterMismatch
                        If (stopIfAny And SymbolComparisonResults.OptionalParameterMismatch) <> 0 Then
                            GoTo Done
                        End If
                    End If

                    If checkTypes Then
                        Dim type1 As TypeWithModifiers
                        If typeSubstitution1 IsNot Nothing Then
                            type1 = SubstituteType(typeSubstitution1, New TypeWithModifiers(param1.OriginalDefinition.Type, param1.OriginalDefinition.CustomModifiers))
                        Else
                            type1 = New TypeWithModifiers(param1.Type, param1.CustomModifiers)
                        End If

                        Dim type2 As TypeWithModifiers
                        If typeSubstitution2 IsNot Nothing Then
                            type2 = SubstituteType(typeSubstitution2, New TypeWithModifiers(param2.OriginalDefinition.Type, param2.OriginalDefinition.CustomModifiers))
                        Else
                            type2 = New TypeWithModifiers(param2.Type, param2.CustomModifiers)
                        End If

                        If Not type1.Type.IsSameTypeIgnoringCustomModifiers(type2.Type) Then
                            If bothOptional Then
                                results = results Or SymbolComparisonResults.OptionalParameterTypeMismatch
                                If (stopIfAny And SymbolComparisonResults.OptionalParameterTypeMismatch) <> 0 Then
                                    GoTo Done
                                End If
                            Else
                                results = results Or SymbolComparisonResults.RequiredParameterTypeMismatch
                                If (stopIfAny And SymbolComparisonResults.RequiredParameterTypeMismatch) <> 0 Then
                                    GoTo Done
                                End If
                            End If
                        ElseIf (comparisons And SymbolComparisonResults.CustomModifierMismatch) <> 0 AndAlso
                               (type1 <> type2 OrElse param1.CountOfCustomModifiersPrecedingByRef <> param2.CountOfCustomModifiersPrecedingByRef) Then
                            results = results Or SymbolComparisonResults.CustomModifierMismatch
                            If (stopIfAny And SymbolComparisonResults.CustomModifierMismatch) <> 0 Then
                                GoTo Done
                            End If
                        End If
                    End If

                    If param1.IsByRef <> param2.IsByRef Then
                        results = results Or SymbolComparisonResults.ParameterByrefMismatch
                        If (stopIfAny And SymbolComparisonResults.ParameterByrefMismatch) <> 0 Then
                            GoTo Done
                        End If
                    End If

                    If (comparisons And SymbolComparisonResults.ParamArrayMismatch) <> 0 Then
                        If param1.IsParamArray <> param2.IsParamArray Then
                            results = results Or SymbolComparisonResults.ParamArrayMismatch
                            If (stopIfAny And SymbolComparisonResults.ParamArrayMismatch) <> 0 Then
                                GoTo Done
                            End If
                        End If
                    End If

                    If bothOptional AndAlso
                       (comparisons And SymbolComparisonResults.OptionalParameterValueMismatch) <> 0 Then

                        Dim bothHaveExplicitDefaultValue = param1.HasExplicitDefaultValue AndAlso param2.HasExplicitDefaultValue
                        Dim optionalParameterMismatch As Boolean

                        ' For source symbols, parameter default values must match. i.e. if one has a default value then both must have a default value
                        ' and the two values must be equal. When one of the parameters is from metadata, we relax the check and only report
                        ' a mismatch if both symbols have default values. This is to handle the case that a metadata symbol has the opt flag set but no 
                        ' default value and a source parameter symbol in method that implements or overrides a metadata method specifies a default value.

                        If bothHaveExplicitDefaultValue Then
                            optionalParameterMismatch = ParameterDefaultValueMismatch(param1, param2)
                        Else
                            ' Strictly speaking, what we would like to check is that both parameters are from the "current" compilation.
                            ' However, the only way to know the current compilation at this point is to pass it into every method
                            ' signature comparison (tedious, since we can't change the signature while implementing IEqualityComparer,
                            ' so we'd have to give up having constant instances).  Fortunately, we can make a good approximation: we can
                            ' require that both parameters be from the same (non-nothing) compilation.  With this rule, an inexact result
                            ' can never change the interaction between two assemblies (i.e. there will never be an observable difference
                            ' between referencing a source assembly and referencing the corresponding metadata assembly).
                            Dim comp1 = param1.DeclaringCompilation
                            Dim comp2 = param2.DeclaringCompilation
                            optionalParameterMismatch = comp1 IsNot Nothing AndAlso comp1 Is comp2
                        End If

                        If optionalParameterMismatch Then
                            results = results Or SymbolComparisonResults.OptionalParameterValueMismatch
                            If (stopIfAny And SymbolComparisonResults.OptionalParameterValueMismatch) <> 0 Then
                                GoTo Done
                            End If
                        End If

                    End If
                Next
            End If

Done:
            Return results
        End Function

        Private Shared Function ParameterDefaultValueMismatch(param1 As ParameterSymbol, param2 As ParameterSymbol) As Boolean
            Dim constValue1 As ConstantValue = param1.ExplicitDefaultConstantValue
            Dim constValue2 As ConstantValue = param2.ExplicitDefaultConstantValue

            ' bad constants do not match
            If constValue1.IsBad OrElse constValue2.IsBad Then
                Return True
            End If

            ' Since Nothing literal essentially means the type's Default value it is equal 
            ' to zero value of types which allow zero values, for example for decimal 0;
            ' so, for signature comparison purpose we have to treat them same as zeroes.

            ' replace Nothing constants with corresponding Zeros if possible
            If constValue1.IsNothing Then
                Dim descriminator = ConstantValue.GetDiscriminator(param1.Type.GetEnumUnderlyingTypeOrSelf.SpecialType)
                If descriminator <> ConstantValueTypeDiscriminator.Bad Then
                    constValue1 = ConstantValue.Default(descriminator)
                End If
            End If

            If constValue2.IsNothing Then
                Dim descriminator = ConstantValue.GetDiscriminator(param2.Type.GetEnumUnderlyingTypeOrSelf.SpecialType)
                If descriminator <> ConstantValueTypeDiscriminator.Bad Then
                    constValue2 = ConstantValue.Default(descriminator)
                End If
            End If

            Return Not constValue1.Equals(constValue2)
        End Function

#End Region

        Public Shared Function HaveSameParameterTypes(params1 As ImmutableArray(Of ParameterSymbol), typeSubstitution1 As TypeSubstitution,
                                                       params2 As ImmutableArray(Of ParameterSymbol), typeSubstitution2 As TypeSubstitution,
                                                       considerByRef As Boolean,
                                                       considerCustomModifiers As Boolean) As Boolean
            Dim numParams = params1.Length

            If numParams <> params2.Length Then
                Return False
            End If

            For i As Integer = 0 To numParams - 1
                Dim param1 = params1(i)
                Dim param2 = params2(i)

                Dim type1 As TypeWithModifiers
                If typeSubstitution1 IsNot Nothing Then
                    type1 = SubstituteType(typeSubstitution1, New TypeWithModifiers(param1.OriginalDefinition.Type, param1.OriginalDefinition.CustomModifiers))
                Else
                    type1 = New TypeWithModifiers(param1.Type, param1.CustomModifiers)
                End If

                Dim type2 As TypeWithModifiers
                If typeSubstitution2 IsNot Nothing Then
                    type2 = SubstituteType(typeSubstitution2, New TypeWithModifiers(param2.OriginalDefinition.Type, param2.OriginalDefinition.CustomModifiers))
                Else
                    type2 = New TypeWithModifiers(param2.Type, param2.CustomModifiers)
                End If

                'the runtime compares custom modifiers using (effectively) SequenceEqual
                If considerCustomModifiers Then
                    If type1 <> type2 OrElse param1.CountOfCustomModifiersPrecedingByRef <> param2.CountOfCustomModifiersPrecedingByRef Then
                        Return False
                    End If
                ElseIf Not type1.Type.IsSameTypeIgnoringCustomModifiers(type2.Type) Then
                    Return False
                End If

                If considerByRef AndAlso param1.IsByRef <> param2.IsByRef Then
                    Return False
                End If
            Next

            Return True
        End Function

        Private Shared Function HaveSameReturnTypes(method1 As MethodSymbol, typeSubstitution1 As TypeSubstitution, method2 As MethodSymbol, typeSubstitution2 As TypeSubstitution, considerCustomModifiers As Boolean) As Boolean
            'short-circuit type map building in the easiest cases
            Dim isSub1 = method1.IsSub
            Dim isSub2 = method2.IsSub
            If isSub1 <> isSub2 Then
                Return False
            ElseIf isSub1 Then
                Return True
            End If

            Dim returnType1 = SubstituteType(typeSubstitution1, New TypeWithModifiers(method1.OriginalDefinition.ReturnType, method1.OriginalDefinition.ReturnTypeCustomModifiers))
            Dim returnType2 = SubstituteType(typeSubstitution2, New TypeWithModifiers(method2.OriginalDefinition.ReturnType, method2.OriginalDefinition.ReturnTypeCustomModifiers))

            ' the runtime compares custom modifiers using (effectively) SequenceEqual
            Return If(considerCustomModifiers,
                      returnType1 = returnType2,
                      returnType1.Type.IsSameTypeIgnoringCustomModifiers(returnType2.Type))
        End Function

        ' If this method is generic, get a TypeSubstitution that substitutes IndexedTypeParameterSymbols
        ' for each method type parameter. This allows correctly comparing parameter and return types
        ' between two signatures:
        '    Function foo(Of T)(p As T) As IEnumerable(Of T)
        '    Function foo(Of U)(p As U) As IEnumerable(Of U)
        '
        ' The substitution returned is to be applied to the ORIGINAL definition of the method.
        Private Shared Function GetTypeSubstitution(method As MethodSymbol) As TypeSubstitution
            Dim containingType As NamedTypeSymbol = method.ContainingType

            If method.Arity = 0 Then
                If containingType Is Nothing OrElse method.IsDefinition Then
                    ' [containingType Is Nothing] is for lambda case.
                    Return Nothing
                Else
                    Return containingType.TypeSubstitution
                End If
            Else
                Dim indexedTypeArguments = StaticCast(Of TypeSymbol).From(IndexedTypeParameterSymbol.Take(method.Arity))

                ' Checking method.IsDefinition instead of [containingSubstitution Is Nothing]
                ' because this condition works better for SignatureOnlyMethodSymbol, which 
                ' always reports itself as a definition, even when attached to a constructed/specialized
                ' type.
                If method.IsDefinition Then
                    Debug.Assert(containingType.TypeSubstitution Is Nothing OrElse TypeOf method Is SignatureOnlyMethodSymbol)
                    Return TypeSubstitution.Create(method, method.TypeParameters, indexedTypeArguments)
                Else
                    Return TypeSubstitution.Create(containingType.TypeSubstitution, method.OriginalDefinition, indexedTypeArguments)
                End If
            End If
        End Function

        ' Apply the substitution created in GetTypeSubstitution() to a particular Type. If the substitution is
        ' Nothing, just return the type.
        '
        ' WARNING: This must always be applied to a type obtained from the ORIGINAL DEFINITION of the method symbol
        ' being compared.
        Private Shared Function SubstituteType(typeSubstitution As TypeSubstitution, typeSymbol As TypeWithModifiers) As TypeWithModifiers
            Return typeSymbol.InternalSubstituteTypeParameters(typeSubstitution)
        End Function

        Friend Shared Function HaveSameConstraints(method1 As MethodSymbol, method2 As MethodSymbol) As Boolean
            Return HaveSameConstraints(method1, GetTypeSubstitution(method1), method2, GetTypeSubstitution(method2))
        End Function

        Private Shared Function HaveSameConstraints(method1 As MethodSymbol,
                                                    typeSubstitution1 As TypeSubstitution,
                                                    method2 As MethodSymbol,
                                                    typeSubstitution2 As TypeSubstitution) As Boolean
            Dim typeParameters1 = method1.OriginalDefinition.TypeParameters
            Dim typeParameters2 = method2.OriginalDefinition.TypeParameters
            Dim arity = typeParameters1.Length

            ' Caller must ensure arity matches since if there are no constraints,
            ' then one could argue that constraints match, even if arity does not.
            Debug.Assert(typeParameters2.Length = arity)

            For i = 0 To arity - 1
                If Not HaveSameConstraints(typeParameters1(i), typeSubstitution1, typeParameters2(i), typeSubstitution2) Then
                    Return False
                End If
            Next

            Return True
        End Function

        Friend Shared Function HaveSameConstraints(typeParameter1 As TypeParameterSymbol,
                                                    typeSubstitution1 As TypeSubstitution,
                                                    typeParameter2 As TypeParameterSymbol,
                                                    typeSubstitution2 As TypeSubstitution) As Boolean

            If (typeParameter1.HasConstructorConstraint <> typeParameter2.HasConstructorConstraint) OrElse
                (typeParameter1.HasReferenceTypeConstraint <> typeParameter2.HasReferenceTypeConstraint) OrElse
                (typeParameter1.HasValueTypeConstraint <> typeParameter2.HasValueTypeConstraint) OrElse
                (typeParameter1.Variance <> typeParameter2.Variance) Then
                Return False
            End If

            ' Check that constraintTypes1 is a subset of constraintTypes2 and
            ' also that constraintTypes2 is a subset of constraintTypes1.

            Dim constraintTypes1 = typeParameter1.ConstraintTypesNoUseSiteDiagnostics
            Dim constraintTypes2 = typeParameter2.ConstraintTypesNoUseSiteDiagnostics

            ' The two sets of constraints may differ in size but still be considered the same
            ' due to duplicated constraints, but if both are zero size, the sets must be equal.
            If (constraintTypes1.Length = 0) AndAlso (constraintTypes2.Length = 0) Then
                Return True
            End If

            Dim substitutedTypes1 = ArrayBuilder(Of TypeSymbol).GetInstance()
            Dim substitutedTypes2 = ArrayBuilder(Of TypeSymbol).GetInstance()

            SubstituteConstraintTypes(constraintTypes1, substitutedTypes1, typeSubstitution1)
            SubstituteConstraintTypes(constraintTypes2, substitutedTypes2, typeSubstitution2)

            ' After substitution, the sets are equal if all constraints in each set are present in the
            ' other. This is because VB requires all inherited constraints to be included explicitly
            ' in overriding or implementing methods. That is distinct from C# which allows
            ' redundant System.Object and System.ValueType constraints to be omitted.
            ' For instance if a base class C(Of T) contains method M(Of U As {Structure, T}),
            ' then the type parameter U in an override C(Of Object).M(Of U) must have
            ' {Structure, Object} constraints even though Object is redundant. Similarly, U in an
            ' override of C(Of System.ValueType).M(Of U) must have {Structure, System.ValueType}
            ' constraints even though System.ValueType is redundant.

            Dim result = AreConstraintTypesSubset(substitutedTypes1, substitutedTypes2) AndAlso
                AreConstraintTypesSubset(substitutedTypes2, substitutedTypes1)

            substitutedTypes1.Free()
            substitutedTypes2.Free()

            Return result
        End Function

        ''' <summary>
        ''' Returns true if the first set of constraint types
        ''' is a subset of the second set.
        ''' </summary>
        Private Shared Function AreConstraintTypesSubset(constraintTypes1 As ArrayBuilder(Of TypeSymbol), constraintTypes2 As ArrayBuilder(Of TypeSymbol)) As Boolean
            For Each constraintType In constraintTypes1
                If Not ContainsIgnoringCustomModifiers(constraintTypes2, constraintType) Then
                    Return False
                End If
            Next

            Return True
        End Function

        Private Shared Function ContainsIgnoringCustomModifiers(types As ArrayBuilder(Of TypeSymbol), type As TypeSymbol) As Boolean
            For Each t In types
                If t.IsSameTypeIgnoringCustomModifiers(type) Then
                    Return True
                End If
            Next
            Return False
        End Function

        Private Shared Sub SubstituteConstraintTypes(constraintTypes As ImmutableArray(Of TypeSymbol), result As ArrayBuilder(Of TypeSymbol), substitution As TypeSubstitution)
            For Each constraintType In constraintTypes
                result.Add(SubstituteType(substitution, New TypeWithModifiers(constraintType)).Type)
            Next
        End Sub

    End Class

End Namespace
