' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Threading
Imports System.Reflection.Metadata
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Reflection
Imports System.Reflection.Metadata.Ecma335
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Collections

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE

    ''' <summary>
    ''' The class to represent all generic type parameters imported from a PE/module.
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class PETypeParameterSymbol
        Inherits SubstitutableTypeParameterSymbol

        Private ReadOnly _containingSymbol As Symbol ' Could be PENamedType or a PEMethod
        Private ReadOnly _handle As GenericParameterHandle
        Private _lazyCustomAttributes As ImmutableArray(Of VisualBasicAttributeData)

#Region "Metadata"
        Private ReadOnly _name As String
        Private ReadOnly _ordinal As UShort ' 0 for first, 1 for second, ...
        Private ReadOnly _flags As GenericParameterAttributes
#End Region

        Private _lazyConstraintTypes As ImmutableArray(Of TypeSymbol)

        ''' <summary>
        ''' Actually stores <see cref="ThreeState"/>
        ''' </summary>
        Private _lazyHasIsUnmanagedConstraint As Byte

        ''' <summary>
        ''' First error calculating bounds.
        ''' </summary>
        Private _lazyCachedBoundsUseSiteInfo As CachedUseSiteInfo(Of AssemblySymbol) = CachedUseSiteInfo(Of AssemblySymbol).Uninitialized ' Indicates unknown state. 

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

            _containingSymbol = definingSymbol

            Dim flags As GenericParameterAttributes

            Try
                moduleSymbol.Module.GetGenericParamPropsOrThrow(handle, _name, flags)
            Catch mrEx As BadImageFormatException
                If _name Is Nothing Then
                    _name = String.Empty
                End If

                _lazyCachedBoundsUseSiteInfo.Initialize(ErrorFactory.ErrorInfo(ERRID.ERR_UnsupportedType1, Me))
            End Try

            ' Clear the '.ctor' flag if both '.ctor' and 'valuetype' are
            ' set since '.ctor' is redundant in that case.
            _flags = If((flags And GenericParameterAttributes.NotNullableValueTypeConstraint) = 0, flags, flags And Not GenericParameterAttributes.DefaultConstructorConstraint)

            _ordinal = ordinal
            _handle = handle
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
                Return _ordinal
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return _name
            End Get
        End Property

        Public Overrides ReadOnly Property MetadataToken As Integer
            Get
                Return MetadataTokens.GetToken(_handle)
            End Get
        End Property

        Friend ReadOnly Property Handle As GenericParameterHandle
            Get
                Return Me._handle
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return _containingSymbol
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingAssembly As AssemblySymbol
            Get
                Return _containingSymbol.ContainingAssembly
            End Get
        End Property

        Public Overloads Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            If _lazyCustomAttributes.IsDefault Then
                Dim containingPEModuleSymbol = DirectCast(ContainingModule(), PEModuleSymbol)
                containingPEModuleSymbol.LoadCustomAttributes(_handle, _lazyCustomAttributes)
            End If
            Return _lazyCustomAttributes
        End Function

        Friend Overrides ReadOnly Property ConstraintTypesNoUseSiteDiagnostics As ImmutableArray(Of TypeSymbol)
            Get
                EnsureAllConstraintsAreResolved()
                Return _lazyConstraintTypes
            End Get
        End Property

        Friend Overrides ReadOnly Property HasUnmanagedTypeConstraint As Boolean
            Get
                EnsureAllConstraintsAreResolved()
                Return CType(Volatile.Read(_lazyHasIsUnmanagedConstraint), ThreeState).Value()
            End Get
        End Property

        Private Function GetDeclaredConstraints(<Out> ByRef hasUnmanagedModreqPattern As Boolean) As ImmutableArray(Of TypeParameterConstraint)
            Dim constraintsBuilder = ArrayBuilder(Of TypeParameterConstraint).GetInstance()

            hasUnmanagedModreqPattern = False

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

            If _containingSymbol.Kind = SymbolKind.Method Then
                containingMethod = DirectCast(_containingSymbol, PEMethodSymbol)
                containingType = DirectCast(containingMethod.ContainingSymbol, PENamedTypeSymbol)
            Else
                containingType = DirectCast(_containingSymbol, PENamedTypeSymbol)
            End If

            Dim moduleSymbol = containingType.ContainingPEModule
            Dim metadataReader = moduleSymbol.Module.MetadataReader
            Dim constraints As GenericParameterConstraintHandleCollection

            Try
                constraints = metadataReader.GetGenericParameter(_handle).GetConstraints()
            Catch mrEx As BadImageFormatException
                constraints = Nothing
                _lazyCachedBoundsUseSiteInfo.InterlockedInitializeFromSentinel(primaryDependency:=Nothing, New UseSiteInfo(Of AssemblySymbol)(ErrorFactory.ErrorInfo(ERRID.ERR_UnsupportedType1, Me)))
            End Try

            If constraints.Count > 0 Then
                Dim tokenDecoder As MetadataDecoder
                If containingMethod IsNot Nothing Then
                    tokenDecoder = New MetadataDecoder(moduleSymbol, containingMethod)
                Else
                    tokenDecoder = New MetadataDecoder(moduleSymbol, containingType)
                End If

                For Each constraintHandle In constraints
                    Dim typeSymbol As TypeSymbol = GetConstraintType(metadataReader, tokenDecoder, constraintHandle, hasUnmanagedModreqPattern)

                    ' Drop 'System.ValueType' constraint type if the 'valuetype' constraint was also specified.
                    If ((_flags And GenericParameterAttributes.NotNullableValueTypeConstraint) <> 0) AndAlso
                        (typeSymbol.SpecialType = Microsoft.CodeAnalysis.SpecialType.System_ValueType) Then
                        Continue For
                    End If

                    typeSymbol = TupleTypeDecoder.DecodeTupleTypesIfApplicable(typeSymbol,
                                                                               constraintHandle,
                                                                               moduleSymbol)

                    constraintsBuilder.Add(New TypeParameterConstraint(typeSymbol, Nothing))
                Next
            End If

            ' - presence of unmanaged pattern has to be matched with `valuetype`
            ' - IsUnmanagedAttribute is allowed if there is an unmanaged pattern
            If (hasUnmanagedModreqPattern AndAlso (_flags And GenericParameterAttributes.NotNullableValueTypeConstraint) = 0) OrElse
               hasUnmanagedModreqPattern <> moduleSymbol.Module.HasIsUnmanagedAttribute(_handle) Then
                ' we do not recognize these combinations as "unmanaged"
                hasUnmanagedModreqPattern = False
                _lazyCachedBoundsUseSiteInfo.InterlockedInitializeFromSentinel(primaryDependency:=Nothing, New UseSiteInfo(Of AssemblySymbol)(ErrorFactory.ErrorInfo(ERRID.ERR_UnsupportedType1, Me)))
            End If

            Return constraintsBuilder.ToImmutableAndFree()
        End Function

        Private Shared Function GetConstraintType(
            metadataReader As MetadataReader,
            tokenDecoder As MetadataDecoder,
            constraintHandle As GenericParameterConstraintHandle,
            ByRef hasUnmanagedModreqPattern As Boolean
        ) As TypeSymbol

            Dim constraint = metadataReader.GetGenericParameterConstraint(constraintHandle)
            Dim modifiers As ImmutableArray(Of ModifierInfo(Of TypeSymbol)) = Nothing
            Dim typeSymbol = tokenDecoder.DecodeGenericParameterConstraint(constraint.Type, modifiers)

            If Not modifiers.IsDefaultOrEmpty AndAlso modifiers.Length > 1 Then
                typeSymbol = New UnsupportedMetadataTypeSymbol()
            ElseIf typeSymbol.SpecialType = SpecialType.System_ValueType Then
                ' recognize "(class [mscorlib]System.ValueType modreq([mscorlib]System.Runtime.InteropServices.UnmanagedType" pattern as "unmanaged"
                If Not modifiers.IsDefaultOrEmpty Then
                    Dim m As ModifierInfo(Of TypeSymbol) = modifiers.Single()
                    If Not m.IsOptional AndAlso m.Modifier.IsWellKnownTypeUnmanagedType() Then
                        hasUnmanagedModreqPattern = True
                    Else
                        ' Any other modifiers, optional or not, are not allowed
                        typeSymbol = New UnsupportedMetadataTypeSymbol()
                    End If
                End If
            ElseIf Not modifiers.IsDefaultOrEmpty Then
                ' Any other modifiers, optional or not, are not allowed
                typeSymbol = New UnsupportedMetadataTypeSymbol()
            End If

            Return typeSymbol
        End Function

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return _containingSymbol.Locations
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return ImmutableArray(Of SyntaxReference).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property HasConstructorConstraint As Boolean
            Get
                Return (_flags And GenericParameterAttributes.DefaultConstructorConstraint) <> 0
            End Get
        End Property

        Public Overrides ReadOnly Property HasReferenceTypeConstraint As Boolean
            Get
                Return (_flags And GenericParameterAttributes.ReferenceTypeConstraint) <> 0
            End Get
        End Property

        Public Overrides ReadOnly Property HasValueTypeConstraint As Boolean
            Get
                Return (_flags And GenericParameterAttributes.NotNullableValueTypeConstraint) <> 0
            End Get
        End Property

        Public Overrides ReadOnly Property AllowsRefLikeType As Boolean
            Get
                Return (_flags And MetadataHelpers.GenericParameterAttributesAllowByRefLike) <> 0
            End Get
        End Property

        Public Overrides ReadOnly Property Variance As VarianceKind
            Get
                Return CType((_flags And GenericParameterAttributes.VarianceMask), VarianceKind)
            End Get
        End Property

        Friend Overrides Sub EnsureAllConstraintsAreResolved()
            If RoslynImmutableInterlocked.VolatileRead(_lazyConstraintTypes).IsDefault Then
                Dim typeParameters = If(_containingSymbol.Kind = SymbolKind.Method,
                                        DirectCast(_containingSymbol, PEMethodSymbol).TypeParameters,
                                        DirectCast(_containingSymbol, PENamedTypeSymbol).TypeParameters)
                EnsureAllConstraintsAreResolved(typeParameters)
            End If
        End Sub

        Friend Overrides Sub ResolveConstraints(inProgress As ConsList(Of TypeParameterSymbol))
            Debug.Assert(Not inProgress.Contains(Me))
            Debug.Assert(Not inProgress.Any() OrElse inProgress.Head.ContainingSymbol Is ContainingSymbol)

            If RoslynImmutableInterlocked.VolatileRead(_lazyConstraintTypes).IsDefault Then
                Dim diagnosticsBuilder = ArrayBuilder(Of TypeParameterDiagnosticInfo).GetInstance()
                Dim inherited = (_containingSymbol.Kind = SymbolKind.Method) AndAlso DirectCast(_containingSymbol, MethodSymbol).IsOverrides
                Dim hasUnmanagedModreqPattern As Boolean = False

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
                Dim constraints = Me.RemoveDirectConstraintConflicts(GetDeclaredConstraints(hasUnmanagedModreqPattern), inProgress.Prepend(Me), DirectConstraintConflictKind.None, diagnosticsBuilder)
                Dim primaryDependency As AssemblySymbol = Me.PrimaryDependency

                Dim useSiteInfo As New UseSiteInfo(Of AssemblySymbol)(primaryDependency)

                For Each pair In diagnosticsBuilder
                    MergeUseSiteInfo(useSiteInfo, pair.UseSiteInfo)
                    If useSiteInfo.DiagnosticInfo IsNot Nothing Then
                        Exit For
                    End If
                Next

                diagnosticsBuilder.Free()

                _lazyCachedBoundsUseSiteInfo.InterlockedInitializeFromSentinel(primaryDependency, useSiteInfo)
                _lazyHasIsUnmanagedConstraint = hasUnmanagedModreqPattern.ToThreeState()

                ' Note, we are relying on the fact that _lazyConstraintTypes is initialized last, and
                ' we depend on the memory barrier from this interlocked operation to prevent write reordering
                ImmutableInterlocked.InterlockedInitialize(_lazyConstraintTypes, GetConstraintTypesOnly(constraints))
            End If

            Debug.Assert(_lazyCachedBoundsUseSiteInfo.IsInitialized)
        End Sub

        Friend Overrides Function GetConstraintsUseSiteInfo() As UseSiteInfo(Of AssemblySymbol)
            EnsureAllConstraintsAreResolved()
            Debug.Assert(_lazyCachedBoundsUseSiteInfo.IsInitialized)
            Return _lazyCachedBoundsUseSiteInfo.ToUseSiteInfo(PrimaryDependency)
        End Function

        Friend Function DeriveCompilerFeatureRequiredDiagnostic(decoder As MetadataDecoder) As DiagnosticInfo
            Return DeriveCompilerFeatureRequiredAttributeDiagnostic(Me, DirectCast(ContainingModule, PEModuleSymbol), Handle, CompilerFeatureRequiredFeatures.None, decoder)
        End Function

        ''' <remarks>
        ''' This is for perf, not for correctness.
        ''' </remarks>
        Friend Overrides ReadOnly Property DeclaringCompilation As VisualBasicCompilation
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property HasUnsupportedMetadata As Boolean
            Get
                Dim containingModule = DirectCast(Me.ContainingModule, PEModuleSymbol)
                Dim containingMethod = TryCast(Me.ContainingSymbol, PEMethodSymbol)
                Dim decoder = If(containingMethod IsNot Nothing,
                    New MetadataDecoder(containingModule, containingMethod),
                    New MetadataDecoder(containingModule, DirectCast(ContainingSymbol, PENamedTypeSymbol)))
                Dim info As DiagnosticInfo = DeriveCompilerFeatureRequiredDiagnostic(decoder)

                Return info IsNot Nothing AndAlso info.Code = DirectCast(ERRID.ERR_UnsupportedCompilerFeature, Integer) OrElse MyBase.HasUnsupportedMetadata
            End Get
        End Property

    End Class

End Namespace
