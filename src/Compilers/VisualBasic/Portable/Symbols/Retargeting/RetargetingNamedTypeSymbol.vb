' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols.Retargeting

    ''' <summary>
    ''' Represents a type of a RetargetingModuleSymbol. Essentially this is a wrapper around 
    ''' another NamedTypeSymbol that is responsible for retargeting referenced symbols from one assembly to another. 
    ''' It can retarget symbols for multiple assemblies at the same time.
    ''' </summary>
    Friend NotInheritable Class RetargetingNamedTypeSymbol
        Inherits InstanceTypeSymbol

        ''' <summary>
        ''' Owning RetargetingModuleSymbol.
        ''' </summary>
        Private ReadOnly _retargetingModule As RetargetingModuleSymbol

        ''' <summary>
        ''' The underlying NamedTypeSymbol, cannot be another RetargetingNamedTypeSymbol.
        ''' </summary>
        Private ReadOnly _underlyingType As NamedTypeSymbol

        Private _lazyTypeParameters As ImmutableArray(Of TypeParameterSymbol)

        Private _lazyCoClass As TypeSymbol = ErrorTypeSymbol.UnknownResultType

        ''' <summary>
        ''' Retargeted custom attributes
        ''' </summary>
        Private _lazyCustomAttributes As ImmutableArray(Of VisualBasicAttributeData)

        Private _lazyUseSiteErrorInfo As DiagnosticInfo = ErrorFactory.EmptyErrorInfo ' Indicates unknown state. 

        Public Sub New(retargetingModule As RetargetingModuleSymbol, underlyingType As NamedTypeSymbol)

            Debug.Assert(retargetingModule IsNot Nothing)
            Debug.Assert(underlyingType IsNot Nothing)

            If TypeOf underlyingType Is RetargetingNamedTypeSymbol Then
                Throw New ArgumentException()
            End If

            _retargetingModule = retargetingModule
            _underlyingType = underlyingType
        End Sub

        Private ReadOnly Property RetargetingTranslator As RetargetingModuleSymbol.RetargetingSymbolTranslator
            Get
                Return _retargetingModule.RetargetingTranslator
            End Get
        End Property

        Public ReadOnly Property UnderlyingNamedType As NamedTypeSymbol
            Get
                Return _underlyingType
            End Get
        End Property

        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return _underlyingType.IsImplicitlyDeclared
            End Get
        End Property

        Public Overrides ReadOnly Property Arity As Integer
            Get
                Return _underlyingType.Arity
            End Get
        End Property

        Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
            Get
                If _lazyTypeParameters.IsDefault Then
                    If Me.Arity = 0 Then
                        _lazyTypeParameters = ImmutableArray(Of TypeParameterSymbol).Empty
                    Else
                        ImmutableInterlocked.InterlockedCompareExchange(_lazyTypeParameters,
                            RetargetingTranslator.Retarget(_underlyingType.TypeParameters), Nothing)
                    End If
                End If

                Return _lazyTypeParameters
            End Get
        End Property

        Public Overrides ReadOnly Property ConstructedFrom As NamedTypeSymbol
            Get
                Return Me
            End Get
        End Property

        Public Overrides ReadOnly Property EnumUnderlyingType As NamedTypeSymbol
            Get
                Dim underlying = _underlyingType.EnumUnderlyingType
                Return If(underlying Is Nothing, Nothing, RetargetingTranslator.Retarget(underlying, RetargetOptions.RetargetPrimitiveTypesByTypeCode)) ' comes from field's signature.
            End Get
        End Property

        Public Overrides ReadOnly Property MightContainExtensionMethods As Boolean
            Get
                Return _underlyingType.MightContainExtensionMethods
            End Get
        End Property

        Friend Overrides ReadOnly Property HasCodeAnalysisEmbeddedAttribute As Boolean
            Get
                Return _underlyingType.HasCodeAnalysisEmbeddedAttribute
            End Get
        End Property

        Friend Overrides ReadOnly Property HasVisualBasicEmbeddedAttribute As Boolean
            Get
                Return _underlyingType.HasVisualBasicEmbeddedAttribute
            End Get
        End Property

        Friend Overrides ReadOnly Property IsExtensibleInterfaceNoUseSiteDiagnostics As Boolean
            Get
                Return _underlyingType.IsExtensibleInterfaceNoUseSiteDiagnostics
            End Get
        End Property

        Friend Overrides ReadOnly Property IsWindowsRuntimeImport As Boolean
            Get
                Return _underlyingType.IsWindowsRuntimeImport
            End Get
        End Property

        Friend Overrides ReadOnly Property ShouldAddWinRTMembers As Boolean
            Get
                Return _underlyingType.ShouldAddWinRTMembers
            End Get
        End Property

        Friend Overrides ReadOnly Property IsComImport As Boolean
            Get
                Return _underlyingType.IsComImport
            End Get
        End Property

        Friend Overrides ReadOnly Property CoClassType As TypeSymbol
            Get
                If _lazyCoClass Is ErrorTypeSymbol.UnknownResultType Then
                    Dim coClass As TypeSymbol = _underlyingType.CoClassType
                    If coClass IsNot Nothing Then
                        coClass = RetargetingTranslator.Retarget(coClass, RetargetOptions.RetargetPrimitiveTypesByName)
                    End If
                    Interlocked.CompareExchange(_lazyCoClass, coClass, DirectCast(ErrorTypeSymbol.UnknownResultType, TypeSymbol))
                End If
                Return _lazyCoClass
            End Get
        End Property

        Friend Overrides Function GetAppliedConditionalSymbols() As ImmutableArray(Of String)
            Return _underlyingType.GetAppliedConditionalSymbols()
        End Function

        Friend Overrides Function GetAttributeUsageInfo() As AttributeUsageInfo
            Return _underlyingType.GetAttributeUsageInfo()
        End Function

        Friend Overrides ReadOnly Property HasDeclarativeSecurity As Boolean
            Get
                Return _underlyingType.HasDeclarativeSecurity
            End Get
        End Property

        Friend Overrides Function GetSecurityInformation() As IEnumerable(Of Microsoft.Cci.SecurityAttribute)
            Return _underlyingType.GetSecurityInformation()
        End Function

        ''' <summary>
        ''' This method is called directly by a Binder when it uses this type.
        ''' </summary>
        Friend Overrides Sub AppendProbableExtensionMethods(name As String, methods As ArrayBuilder(Of MethodSymbol))
            Dim oldCount As Integer = methods.Count

            ' Delegate work to the underlying type.
            _underlyingType.AppendProbableExtensionMethods(name, methods)

            ' Retarget all method symbols appended by the underlying type.
            For i As Integer = oldCount To methods.Count - 1
                methods(i) = RetargetingTranslator.Retarget(methods(i))
            Next
        End Sub

        Friend Overrides Sub BuildExtensionMethodsMap(
            map As Dictionary(Of String, ArrayBuilder(Of MethodSymbol)),
            appendThrough As NamespaceSymbol
        )
            Throw ExceptionUtilities.Unreachable
        End Sub

        Friend Overrides Sub GetExtensionMethods(methods As ArrayBuilder(Of MethodSymbol), appendThrough As NamespaceSymbol, Name As String)
            Throw ExceptionUtilities.Unreachable
        End Sub

        ''' <summary>
        ''' This method is called directly by a Binder when it uses this type.
        ''' </summary>
        Friend Overrides Sub AddExtensionMethodLookupSymbolsInfo(nameSet As LookupSymbolsInfo,
                                                                  options As LookupOptions,
                                                                  originalBinder As Binder)
            _underlyingType.AddExtensionMethodLookupSymbolsInfo(nameSet, options, originalBinder, appendThrough:=Me)
        End Sub

        ''' <summary>
        ''' Make sure we retarget methods when underlying type checks their viability.
        ''' </summary>
        Friend Overrides Function AddExtensionMethodLookupSymbolsInfoViabilityCheck(method As MethodSymbol, options As LookupOptions, nameSet As LookupSymbolsInfo, originalBinder As Binder) As Boolean
            Return MyBase.AddExtensionMethodLookupSymbolsInfoViabilityCheck(RetargetingTranslator.Retarget(method), options, nameSet, originalBinder)
        End Function

        Friend Overrides Sub AddExtensionMethodLookupSymbolsInfo(nameSet As LookupSymbolsInfo,
                                                                  options As LookupOptions,
                                                                  originalBinder As Binder,
                                                                  appendThrough As NamedTypeSymbol)
            Throw ExceptionUtilities.Unreachable
        End Sub

        Public Overrides ReadOnly Property Name As String
            Get
                Return _underlyingType.Name
            End Get
        End Property

        Public Overrides ReadOnly Property MetadataName As String
            Get
                Return _underlyingType.MetadataName
            End Get
        End Property

        Friend Overrides ReadOnly Property MangleName As Boolean
            Get
                Return _underlyingType.MangleName
            End Get
        End Property

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return _underlyingType.HasSpecialName
            End Get
        End Property

        Public Overrides ReadOnly Property IsSerializable As Boolean
            Get
                Return _underlyingType.IsSerializable
            End Get
        End Property

        Friend Overrides ReadOnly Property Layout As TypeLayout
            Get
                Return _underlyingType.Layout
            End Get
        End Property

        Friend Overrides ReadOnly Property MarshallingCharSet As CharSet
            Get
                Return _underlyingType.MarshallingCharSet
            End Get
        End Property

        Friend Overrides Function GetEmittedNamespaceName() As String
            Return _underlyingType.GetEmittedNamespaceName()
        End Function

        Public Overrides ReadOnly Property MemberNames As IEnumerable(Of String)
            Get
                Return _underlyingType.MemberNames
            End Get
        End Property

        Public Overrides Function GetMembers() As ImmutableArray(Of Symbol)
            Return RetargetingTranslator.Retarget(_underlyingType.GetMembers())
        End Function

        Friend Overrides Function GetMembersUnordered() As ImmutableArray(Of Symbol)
            Return RetargetingTranslator.Retarget(_underlyingType.GetMembersUnordered())
        End Function

        Public Overrides Function GetMembers(name As String) As ImmutableArray(Of Symbol)
            Return RetargetingTranslator.Retarget(_underlyingType.GetMembers(name))
        End Function

        Friend Overrides Iterator Function GetFieldsToEmit() As IEnumerable(Of FieldSymbol)
            For Each field In _underlyingType.GetFieldsToEmit()
                Yield RetargetingTranslator.Retarget(field)
            Next
        End Function

        Friend Overrides Iterator Function GetMethodsToEmit() As IEnumerable(Of MethodSymbol)
            Dim isInterface = _underlyingType.IsInterfaceType()

            For Each method In _underlyingType.GetMethodsToEmit()
                Debug.Assert(method IsNot Nothing)

                Dim gapSize = If(isInterface, Microsoft.CodeAnalysis.ModuleExtensions.GetVTableGapSize(method.MetadataName), 0)
                If gapSize > 0 Then
                    Do
                        Yield Nothing
                        gapSize -= 1
                    Loop While gapSize > 0
                Else
                    Yield RetargetingTranslator.Retarget(method)
                End If
            Next
        End Function

        Friend Overrides Iterator Function GetPropertiesToEmit() As IEnumerable(Of PropertySymbol)
            For Each [property] In _underlyingType.GetPropertiesToEmit()
                Yield RetargetingTranslator.Retarget([property])
            Next
        End Function

        Friend Overrides Iterator Function GetEventsToEmit() As IEnumerable(Of EventSymbol)
            For Each [event] In _underlyingType.GetEventsToEmit()
                Yield RetargetingTranslator.Retarget([event])
            Next
        End Function

        Friend Overrides Function GetTypeMembersUnordered() As ImmutableArray(Of NamedTypeSymbol)
            Return RetargetingTranslator.Retarget(_underlyingType.GetTypeMembersUnordered())
        End Function

        Public Overrides Function GetTypeMembers() As ImmutableArray(Of NamedTypeSymbol)
            Return RetargetingTranslator.Retarget(_underlyingType.GetTypeMembers())
        End Function

        Public Overrides Function GetTypeMembers(name As String) As ImmutableArray(Of NamedTypeSymbol)
            Return RetargetingTranslator.Retarget(_underlyingType.GetTypeMembers(name))
        End Function

        Public Overrides Function GetTypeMembers(name As String, arity As Integer) As ImmutableArray(Of NamedTypeSymbol)
            Return RetargetingTranslator.Retarget(_underlyingType.GetTypeMembers(name, arity))
        End Function

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return _underlyingType.DeclaredAccessibility
            End Get
        End Property

        Friend Overrides Function MakeDeclaredBase(basesBeingResolved As BasesBeingResolved, diagnostics As DiagnosticBag) As NamedTypeSymbol
            Dim underlyingBase = _underlyingType.GetDeclaredBase(basesBeingResolved)

            Return If(underlyingBase IsNot Nothing,
                       RetargetingTranslator.Retarget(underlyingBase, RetargetOptions.RetargetPrimitiveTypesByName),
                       Nothing)
        End Function

        Friend Overrides Function GetInterfacesToEmit() As IEnumerable(Of NamedTypeSymbol)
            Return RetargetingTranslator.Retarget(_underlyingType.GetInterfacesToEmit())
        End Function

        Friend Overrides Function MakeDeclaredInterfaces(basesBeingResolved As BasesBeingResolved, diagnostics As DiagnosticBag) As ImmutableArray(Of NamedTypeSymbol)
            Dim underlyingBaseInterfaces = _underlyingType.GetDeclaredInterfacesNoUseSiteDiagnostics(basesBeingResolved)

            Return RetargetingTranslator.Retarget(underlyingBaseInterfaces)
        End Function

        Private Shared Function CyclicInheritanceError(diag As DiagnosticInfo) As ErrorTypeSymbol
            Return New ExtendedErrorTypeSymbol(diag, True)
        End Function

        Friend Overrides Function MakeAcyclicBaseType(diagnostics As DiagnosticBag) As NamedTypeSymbol
            Dim diag = BaseTypeAnalysis.GetDependencyDiagnosticsForImportedClass(Me)
            If diag IsNot Nothing Then
                Return CyclicInheritanceError(diag)
            End If

            Dim acyclicBase = GetDeclaredBase(Nothing)

            If acyclicBase Is Nothing Then
                ' if base was not declared, get it from BaseType that should set it to some default
                Dim underlyingBase = _underlyingType.BaseTypeNoUseSiteDiagnostics
                If underlyingBase IsNot Nothing Then
                    acyclicBase = RetargetingTranslator.Retarget(underlyingBase, RetargetOptions.RetargetPrimitiveTypesByName)
                End If
            End If

            Return acyclicBase
        End Function

        Friend Overrides Function MakeAcyclicInterfaces(diagnostics As DiagnosticBag) As ImmutableArray(Of NamedTypeSymbol)
            Dim declaredInterfaces As ImmutableArray(Of NamedTypeSymbol) = GetDeclaredInterfacesNoUseSiteDiagnostics(Nothing)
            If (Not Me.IsInterface) Then
                ' only interfaces needs to check for inheritance cycles via interfaces.
                Return declaredInterfaces
            End If

            Return (From t In declaredInterfaces
                    Let diag = BaseTypeAnalysis.GetDependencyDiagnosticsForImportedBaseInterface(Me, t)
                    Select If(diag Is Nothing, t, CyclicInheritanceError(diag))).AsImmutable

        End Function

        Public Overrides ReadOnly Property TypeKind As TypeKind
            Get
                Return _underlyingType.TypeKind
            End Get
        End Property

        Friend Overrides ReadOnly Property IsInterface As Boolean
            Get
                Return _underlyingType.IsInterface
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return RetargetingTranslator.Retarget(_underlyingType.ContainingSymbol)
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return _underlyingType.Locations
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return _underlyingType.DeclaringSyntaxReferences
            End Get
        End Property

        Public Overrides ReadOnly Property IsMustInherit As Boolean
            Get
                Return _underlyingType.IsMustInherit
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMetadataAbstract As Boolean
            Get
                Return _underlyingType.IsMetadataAbstract
            End Get
        End Property

        Public Overrides ReadOnly Property IsNotInheritable As Boolean
            Get
                Return _underlyingType.IsNotInheritable
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMetadataSealed As Boolean
            Get
                Return _underlyingType.IsMetadataSealed
            End Get
        End Property

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return _underlyingType.ObsoleteAttributeData
            End Get
        End Property

        Public Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return RetargetingTranslator.GetRetargetedAttributes(_underlyingType, _lazyCustomAttributes)
        End Function

        Friend Overrides Function GetCustomAttributesToEmit(compilationState As ModuleCompilationState) As IEnumerable(Of VisualBasicAttributeData)
            Return RetargetingTranslator.RetargetAttributes(_underlyingType.GetCustomAttributesToEmit(compilationState))
        End Function

        Public Overrides ReadOnly Property ContainingAssembly As AssemblySymbol
            Get
                Return _retargetingModule.ContainingAssembly
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingModule As ModuleSymbol
            Get
                Return _retargetingModule
            End Get
        End Property

        Friend Overrides Function LookupMetadataType(ByRef emittedTypeName As MetadataTypeName) As NamedTypeSymbol
            Return RetargetingTranslator.Retarget(_underlyingType.LookupMetadataType(emittedTypeName), RetargetOptions.RetargetPrimitiveTypesByName)
        End Function

        Friend Overrides Function GetUseSiteErrorInfo() As DiagnosticInfo

            If _lazyUseSiteErrorInfo Is ErrorFactory.EmptyErrorInfo Then
                _lazyUseSiteErrorInfo = CalculateUseSiteErrorInfo()
            End If

            Return _lazyUseSiteErrorInfo
        End Function

        Friend Overrides ReadOnly Property DefaultPropertyName As String
            Get
                Return _underlyingType.DefaultPropertyName
            End Get
        End Property

        ''' <summary>
        ''' Force all declaration errors to be generated.
        ''' </summary>
        Friend Overrides Sub GenerateDeclarationErrors(cancellationToken As CancellationToken)
            Throw ExceptionUtilities.Unreachable
        End Sub

        ''' <remarks>
        ''' This is for perf, not for correctness.
        ''' </remarks>
        Friend Overrides ReadOnly Property DeclaringCompilation As VisualBasicCompilation
            Get
                Return Nothing
            End Get
        End Property

        Friend Overrides Function GetGuidString(ByRef guidString As String) As Boolean
            Return _underlyingType.GetGuidString(guidString)
        End Function

        Public Overrides Function GetDocumentationCommentXml(Optional preferredCulture As CultureInfo = Nothing, Optional expandIncludes As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As String
            Return _underlyingType.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken)
        End Function

        Friend Overrides Iterator Function GetSynthesizedWithEventsOverrides() As IEnumerable(Of PropertySymbol)
            For Each underlying As PropertySymbol In _underlyingType.GetSynthesizedWithEventsOverrides()
                Yield RetargetingTranslator.Retarget(underlying)
            Next
        End Function
    End Class
End Namespace
