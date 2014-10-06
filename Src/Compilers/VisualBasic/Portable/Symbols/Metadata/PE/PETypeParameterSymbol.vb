' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Threading
Imports System.Reflection.Metadata
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Reflection

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE

    ''' <summary>
    ''' The class to represent all generic type parameters imported from a PE/module.
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class PETypeParameterSymbol
        Inherits TypeParameterSymbol

        Private ReadOnly m_ContainingSymbol As Symbol ' Could be PENamedType or a PEMethod
        Private ReadOnly m_Handle As GenericParameterHandle
        Private m_lazyCustomAttributes As ImmutableArray(Of VisualBasicAttributeData)

#Region "Metadata"
        Private ReadOnly m_Name As String
        Private ReadOnly m_Ordinal As UShort ' 0 for first, 1 for second, ...
        Private ReadOnly m_Flags As GenericParameterAttributes
#End Region

        Private m_lazyConstraintTypes As ImmutableArray(Of TypeSymbol)

        ''' <summary>
        ''' First error calculating bounds.
        ''' </summary>
        Private m_lazyBoundsErrorInfo As DiagnosticInfo = ErrorFactory.EmptyErrorInfo ' Indicates unknown state. 

        Friend Sub New(
            moduleSymbol As PEModuleSymbol,
            definingNamedType As PENamedTypeSymbol,
            ordinal As UShort,
            handle As GenericParameterHandle
        )
            Me.New(moduleSymbol, DirectCast(definingNamedType, Symbol), ordinal, handle)
        End Sub

        Friend Sub New(
            moduleSymbol As PEModuleSymbol,
            definingMethod As PEMethodSymbol,
            ordinal As UShort,
            handle As GenericParameterHandle
        )
            Me.New(moduleSymbol, DirectCast(definingMethod, Symbol), ordinal, handle)
        End Sub

        Private Sub New(
            moduleSymbol As PEModuleSymbol,
            definingSymbol As Symbol,
            ordinal As UShort,
            handle As GenericParameterHandle
        )
            Debug.Assert(moduleSymbol IsNot Nothing)
            Debug.Assert(definingSymbol IsNot Nothing)
            Debug.Assert(ordinal >= 0)
            Debug.Assert(Not handle.IsNil)

            m_ContainingSymbol = definingSymbol

            Dim flags As GenericParameterAttributes

            Try
                moduleSymbol.Module.GetGenericParamPropsOrThrow(handle, m_Name, flags)
            Catch mrEx As BadImageFormatException
                If m_Name Is Nothing Then
                    m_Name = String.Empty
                End If

                m_lazyBoundsErrorInfo = ErrorFactory.ErrorInfo(ERRID.ERR_UnsupportedType1, Me)
            End Try

            ' Clear the '.ctor' flag if both '.ctor' and 'valuetype' are
            ' set since '.ctor' is redundant in that case.
            m_Flags = If((flags And GenericParameterAttributes.NotNullableValueTypeConstraint) = 0, flags, flags And Not GenericParameterAttributes.DefaultConstructorConstraint)

            m_Ordinal = ordinal
            m_Handle = handle
        End Sub

        Public Overrides ReadOnly Property TypeParameterKind As TypeParameterKind
            Get
                Return If(Me.ContainingSymbol.Kind = SymbolKind.Method,
                          TypeParameterKind.Method,
                          TypeParameterKind.Type)
            End Get
        End Property

        Public Overrides ReadOnly Property Ordinal As Integer
            Get
                Return m_Ordinal
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return m_Name
            End Get
        End Property

        Friend ReadOnly Property Handle As GenericParameterHandle
            Get
                Return Me.m_Handle
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return m_ContainingSymbol
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingAssembly As AssemblySymbol
            Get
                Return m_ContainingSymbol.ContainingAssembly
            End Get
        End Property

        Public Overloads Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            If m_lazyCustomAttributes.IsDefault Then
                Dim containingPEModuleSymbol = DirectCast(ContainingModule(), PEModuleSymbol)
                containingPEModuleSymbol.LoadCustomAttributes(m_Handle, m_lazyCustomAttributes)
            End If
            Return m_lazyCustomAttributes
        End Function

        Friend Overrides ReadOnly Property ConstraintTypesNoUseSiteDiagnostics As ImmutableArray(Of TypeSymbol)
            Get
                EnsureAllConstraintsAreResolved()
                Return m_lazyConstraintTypes
            End Get
        End Property

        Private Function GetDeclaredConstraints() As ImmutableArray(Of TypeParameterConstraint)
            Dim constraintsBuilder = ArrayBuilder(Of TypeParameterConstraint).GetInstance()

            If HasConstructorConstraint Then
                constraintsBuilder.Add(New TypeParameterConstraint(TypeParameterConstraintKind.Constructor, Nothing))
            End If

            If HasReferenceTypeConstraint Then
                constraintsBuilder.Add(New TypeParameterConstraint(TypeParameterConstraintKind.ReferenceType, Nothing))
            End If

            If HasValueTypeConstraint Then
                constraintsBuilder.Add(New TypeParameterConstraint(TypeParameterConstraintKind.ValueType, Nothing))
            End If

            Dim containingMethod As PEMethodSymbol = Nothing
            Dim containingType As PENamedTypeSymbol

            If m_ContainingSymbol.Kind = SymbolKind.Method Then
                containingMethod = DirectCast(m_ContainingSymbol, PEMethodSymbol)
                containingType = DirectCast(containingMethod.ContainingSymbol, PENamedTypeSymbol)
            Else
                containingType = DirectCast(m_ContainingSymbol, PENamedTypeSymbol)
            End If

            Dim moduleSymbol = containingType.ContainingPEModule
            Dim constraints() As Handle

            Try
                constraints = moduleSymbol.Module.GetGenericParamConstraintsOrThrow(m_Handle)
            Catch mrEx As BadImageFormatException
                constraints = Nothing
                Interlocked.CompareExchange(m_lazyBoundsErrorInfo, ErrorFactory.ErrorInfo(ERRID.ERR_UnsupportedType1, Me), ErrorFactory.EmptyErrorInfo)
            End Try

            If constraints IsNot Nothing AndAlso constraints.Length > 0 Then
                Dim tokenDecoder As MetadataDecoder
                If containingMethod IsNot Nothing Then
                    tokenDecoder = New MetadataDecoder(moduleSymbol, containingMethod)
                Else
                    tokenDecoder = New MetadataDecoder(moduleSymbol, containingType)
                End If

                For Each constraint In constraints
                    Dim typeSymbol As typeSymbol = tokenDecoder.GetTypeOfToken(constraint)

                    ' Drop 'System.ValueType' constraint type if the 'valuetype' constraint was also specified.
                    If ((m_Flags And GenericParameterAttributes.NotNullableValueTypeConstraint) <> 0) AndAlso
                        (typeSymbol.SpecialType = Microsoft.CodeAnalysis.SpecialType.System_ValueType) Then
                        Continue For
                    End If

                    constraintsBuilder.Add(New TypeParameterConstraint(typeSymbol, Nothing))
                Next
            End If

            Return constraintsBuilder.ToImmutableAndFree()
        End Function

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return m_ContainingSymbol.Locations
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return ImmutableArray(Of SyntaxReference).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property HasConstructorConstraint As Boolean
            Get
                Return (m_Flags And GenericParameterAttributes.DefaultConstructorConstraint) <> 0
            End Get
        End Property

        Public Overrides ReadOnly Property HasReferenceTypeConstraint As Boolean
            Get
                Return (m_Flags And GenericParameterAttributes.ReferenceTypeConstraint) <> 0
            End Get
        End Property

        Public Overrides ReadOnly Property HasValueTypeConstraint As Boolean
            Get
                Return (m_Flags And GenericParameterAttributes.NotNullableValueTypeConstraint) <> 0
            End Get
        End Property

        Public Overrides ReadOnly Property Variance As VarianceKind
            Get
                Return CType((m_Flags And GenericParameterAttributes.VarianceMask), VarianceKind)
            End Get
        End Property

        Friend Overrides Sub EnsureAllConstraintsAreResolved()
            If m_lazyConstraintTypes.IsDefault Then
                Dim typeParameters = If(m_ContainingSymbol.Kind = SymbolKind.Method,
                                        DirectCast(m_ContainingSymbol, PEMethodSymbol).TypeParameters,
                                        DirectCast(m_ContainingSymbol, PENamedTypeSymbol).TypeParameters)
                EnsureAllConstraintsAreResolved(typeParameters)
            End If
        End Sub

        Friend Overrides Sub ResolveConstraints(inProgress As ConsList(Of TypeParameterSymbol))
            Debug.Assert(Not inProgress.Contains(Me))
            Debug.Assert(Not inProgress.Any() OrElse inProgress.Head.ContainingSymbol Is ContainingSymbol)

            If m_lazyConstraintTypes.IsDefault Then
                Dim diagnosticsBuilder = ArrayBuilder(Of TypeParameterDiagnosticInfo).GetInstance()
                Dim inherited = (m_ContainingSymbol.Kind = SymbolKind.Method) AndAlso DirectCast(m_ContainingSymbol, MethodSymbol).IsOverrides

                ' Check direct constraints on the type parameter to generate any use-site errors
                ' (for example, the cycle in ".class public A<(!T)T>"). It's necessary to check for such
                ' errors because invalid constraints are dropped in those cases so use of such
                ' types/methods is otherwise valid. It shouldn't be necessary to check indirect
                ' constraints because any indirect constraints are retained, even if invalid, so
                ' references to such types/methods cannot be satisfied for specific type arguments.
                ' (An example of an indirect constraint conflict, U in ".class public C<(A)T, (!T, B)U>",
                ' which cannot be satisfied if A and B are from different hierarchies.) It also isn't
                ' necessary to report redundant constraints since redundant constraints are still
                ' valid. Redundant constraints are dropped silently.
                Dim constraints = Me.RemoveDirectConstraintConflicts(GetDeclaredConstraints(), inProgress.Prepend(Me), DirectConstraintConflictKind.None, diagnosticsBuilder)
                Dim errorInfo = If(diagnosticsBuilder.Count > 0, diagnosticsBuilder(0).DiagnosticInfo, Nothing)
                diagnosticsBuilder.Free()

                Interlocked.CompareExchange(m_lazyBoundsErrorInfo, errorInfo, ErrorFactory.EmptyErrorInfo)
                ImmutableInterlocked.InterlockedInitialize(m_lazyConstraintTypes, GetConstraintTypesOnly(constraints))
            End If

            Debug.Assert(m_lazyBoundsErrorInfo IsNot ErrorFactory.EmptyErrorInfo)
        End Sub

        Friend Overrides Function GetConstraintsUseSiteErrorInfo() As DiagnosticInfo
            EnsureAllConstraintsAreResolved()
            Debug.Assert(m_lazyBoundsErrorInfo IsNot ErrorFactory.EmptyErrorInfo)
            Return m_lazyBoundsErrorInfo
        End Function

        ''' <remarks>
        ''' This is for perf, not for correctness.
        ''' </remarks>
        Friend Overrides ReadOnly Property DeclaringCompilation As VBCompilation
            Get
                Return Nothing
            End Get
        End Property

    End Class

End Namespace
