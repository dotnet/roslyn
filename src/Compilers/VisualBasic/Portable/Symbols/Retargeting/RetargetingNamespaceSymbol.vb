' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Collections.ObjectModel
Imports System.Globalization
Imports System.Threading
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols.Retargeting

    ''' <summary>
    ''' Represents a namespace of a RetargetingModuleSymbol. Essentially this is a wrapper around 
    ''' another NamespaceSymbol that is responsible for retargeting symbols from one assembly to another. 
    ''' It can retarget symbols for multiple assemblies at the same time.
    ''' </summary>
    Friend NotInheritable Class RetargetingNamespaceSymbol
        Inherits NamespaceSymbol

        ''' <summary>
        ''' Owning RetargetingModuleSymbol.
        ''' </summary>
        Private ReadOnly _retargetingModule As RetargetingModuleSymbol

        ''' <summary>
        ''' The underlying NamespaceSymbol, cannot be another RetargetingNamespaceSymbol.
        ''' </summary>
        Private ReadOnly _underlyingNamespace As NamespaceSymbol

        Public Sub New(retargetingModule As RetargetingModuleSymbol, underlyingNamespace As NamespaceSymbol)
            Debug.Assert(retargetingModule IsNot Nothing)
            Debug.Assert(underlyingNamespace IsNot Nothing)

            If TypeOf underlyingNamespace Is RetargetingNamespaceSymbol Then
                Throw New ArgumentException()
            End If

            _retargetingModule = retargetingModule
            _underlyingNamespace = underlyingNamespace
        End Sub

        Private ReadOnly Property RetargetingTranslator As RetargetingModuleSymbol.RetargetingSymbolTranslator
            Get
                Return _retargetingModule.RetargetingTranslator
            End Get
        End Property

        Public ReadOnly Property UnderlyingNamespace As NamespaceSymbol
            Get
                Return _underlyingNamespace
            End Get
        End Property

        Friend Overrides ReadOnly Property Extent As NamespaceExtent
            Get
                Return New NamespaceExtent(_retargetingModule)
            End Get
        End Property

        Public Overrides Function GetMembers() As ImmutableArray(Of Symbol)
            Return RetargetMembers(_underlyingNamespace.GetMembers())
        End Function

        Private Function RetargetMembers(underlyingMembers As ImmutableArray(Of Symbol)) As ImmutableArray(Of Symbol)
            Dim builder = ArrayBuilder(Of Symbol).GetInstance()
            For Each s In underlyingMembers
                ' Skip explicitly declared local types.
                If s.Kind = SymbolKind.NamedType AndAlso DirectCast(s, NamedTypeSymbol).IsExplicitDefinitionOfNoPiaLocalType Then
                    Continue For
                End If
                builder.Add(RetargetingTranslator.Retarget(s))
            Next
            Return builder.ToImmutableAndFree()
        End Function

        Friend Overrides Function GetMembersUnordered() As ImmutableArray(Of Symbol)
            Return RetargetMembers(_underlyingNamespace.GetMembersUnordered())
        End Function

        Public Overrides Function GetMembers(name As String) As ImmutableArray(Of Symbol)
            Return RetargetMembers(_underlyingNamespace.GetMembers(name))
        End Function

        Friend Overrides Function GetTypeMembersUnordered() As ImmutableArray(Of NamedTypeSymbol)
            Return RetargetTypeMembers(_underlyingNamespace.GetTypeMembersUnordered())
        End Function

        Public Overrides Function GetTypeMembers() As ImmutableArray(Of NamedTypeSymbol)
            Return RetargetTypeMembers(_underlyingNamespace.GetTypeMembers())
        End Function

        Private Function RetargetTypeMembers(underlyingMembers As ImmutableArray(Of NamedTypeSymbol)) As ImmutableArray(Of NamedTypeSymbol)
            Dim builder = ArrayBuilder(Of NamedTypeSymbol).GetInstance()
            For Each t In underlyingMembers
                ' Skip explicitly declared local types.
                If t.IsExplicitDefinitionOfNoPiaLocalType Then
                    Continue For
                End If
                Debug.Assert(t.PrimitiveTypeCode = Cci.PrimitiveTypeCode.NotPrimitive)
                builder.Add(RetargetingTranslator.Retarget(t, RetargetOptions.RetargetPrimitiveTypesByName))
            Next
            Return builder.ToImmutableAndFree()
        End Function

        Public Overrides Function GetTypeMembers(name As String) As ImmutableArray(Of NamedTypeSymbol)
            Return RetargetTypeMembers(_underlyingNamespace.GetTypeMembers(name))
        End Function

        Public Overrides Function GetTypeMembers(name As String, arity As Integer) As ImmutableArray(Of NamedTypeSymbol)
            Return RetargetTypeMembers(_underlyingNamespace.GetTypeMembers(name, arity))
        End Function

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return RetargetingTranslator.Retarget(_underlyingNamespace.ContainingSymbol)
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return _underlyingNamespace.Locations
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return _underlyingNamespace.DeclaringSyntaxReferences
            End Get
        End Property

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

        Public Overrides ReadOnly Property IsGlobalNamespace As Boolean
            Get
                Return _underlyingNamespace.IsGlobalNamespace
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return _underlyingNamespace.Name
            End Get
        End Property

        Public Overrides ReadOnly Property MetadataName As String
            Get
                Return _underlyingNamespace.MetadataName
            End Get
        End Property

        Friend Overrides Function LookupMetadataType(ByRef fullEmittedName As MetadataTypeName) As NamedTypeSymbol
            ' This method is invoked when looking up a type by metadata type
            ' name through a RetargetingAssemblySymbol. For instance, in
            ' UnitTests.Symbols.Metadata.PE.NoPia.LocalTypeSubstitution2.

            Dim underlying As NamedTypeSymbol = _underlyingNamespace.LookupMetadataType(fullEmittedName)

            If underlying Is Nothing Then
                Return Nothing
            End If

            Debug.Assert(Not underlying.IsErrorType())
            Debug.Assert(underlying.ContainingModule Is _retargetingModule.UnderlyingModule)

            If underlying.IsExplicitDefinitionOfNoPiaLocalType Then
                ' Explicitly defined local types should be hidden.
                Return Nothing
            End If

            Return RetargetingTranslator.Retarget(underlying, RetargetOptions.RetargetPrimitiveTypesByName)
        End Function

        Public Overrides Function GetModuleMembers() As ImmutableArray(Of NamedTypeSymbol)
            Return RetargetingTranslator.Retarget(_underlyingNamespace.GetModuleMembers())
        End Function

        Public Overrides Function GetModuleMembers(name As String) As ImmutableArray(Of NamedTypeSymbol)
            Return RetargetingTranslator.Retarget(_underlyingNamespace.GetModuleMembers(name))
        End Function

        ''' <summary>
        ''' Calculate declared accessibility of most accessible type within this namespace or within a containing namespace recursively.
        ''' Expected to be called at most once per namespace symbol, unless there is a race condition.
        ''' 
        ''' Valid return values:
        '''     Friend,
        '''     Public,
        '''     NotApplicable - if there are no types.
        ''' </summary>
        Protected Overrides Function GetDeclaredAccessibilityOfMostAccessibleDescendantType() As Accessibility
            Throw ExceptionUtilities.Unreachable
        End Function

        Friend Overrides ReadOnly Property DeclaredAccessibilityOfMostAccessibleDescendantType As Accessibility
            Get
                Return _underlyingNamespace.DeclaredAccessibilityOfMostAccessibleDescendantType
            End Get
        End Property

        ''' <summary>
        ''' This method is called directly by a Binder when it uses this module level namespace.
        ''' </summary>
        Friend Overrides Sub AppendProbableExtensionMethods(name As String, methods As ArrayBuilder(Of MethodSymbol))
            Dim oldCount As Integer = methods.Count

            ' Delegate work to the underlying namespace in order to take advantage of its
            ' map of extension methods.
            _underlyingNamespace.AppendProbableExtensionMethods(name, methods)

            ' Retarget all method symbols appended by the underlying namespace.
            For i As Integer = oldCount To methods.Count - 1
                methods(i) = RetargetingTranslator.Retarget(methods(i))
            Next
        End Sub

        Friend Overrides ReadOnly Property TypesToCheckForExtensionMethods As ImmutableArray(Of NamedTypeSymbol)
            Get
                ' We should override all callers of this function and go through implementation
                ' provided by the underlying namespace symbol.
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        ''' <summary>
        ''' This method is called when this namespace is part of a merged namespace and we are trying to build
        ''' a map of extension methods for the whole merged namespace.
        ''' </summary>
        Friend Overrides Sub BuildExtensionMethodsMap(map As Dictionary(Of String, ArrayBuilder(Of MethodSymbol)))
            ' Delegate work to the types of the underlying namespace.
            For Each underlyingContainedType As NamedTypeSymbol In _underlyingNamespace.TypesToCheckForExtensionMethods
                underlyingContainedType.BuildExtensionMethodsMap(map, appendThrough:=Me)
            Next
        End Sub

        Friend Overrides Sub GetExtensionMethods(methods As ArrayBuilder(Of MethodSymbol), name As String)
            ' Delegate work to the types of the underlying namespace.
            For Each underlyingContainedType As NamedTypeSymbol In _underlyingNamespace.TypesToCheckForExtensionMethods
                underlyingContainedType.GetExtensionMethods(methods, appendThrough:=Me, Name:=name)
            Next
        End Sub

        ''' <summary>
        ''' Make sure we retarget methods when types of the underlying namespace add them to the map.
        ''' </summary>
        Friend Overrides Sub BuildExtensionMethodsMapBucket(bucket As ArrayBuilder(Of MethodSymbol), method As MethodSymbol)
            bucket.Add(RetargetingTranslator.Retarget(method))
        End Sub

        ''' <summary>
        ''' This method is called directly by a Binder when it uses this module level namespace.
        ''' </summary>
        Friend Overrides Sub AddExtensionMethodLookupSymbolsInfo(nameSet As LookupSymbolsInfo,
                                                                  options As LookupOptions,
                                                                  originalBinder As Binder)
            ' Delegate work to the underlying namespace in order to take advantage of its
            ' map of extension methods.
            _underlyingNamespace.AddExtensionMethodLookupSymbolsInfo(nameSet, options, originalBinder, appendThrough:=Me)
        End Sub

        ''' <summary>
        ''' Make sure we retarget methods when underlying namespace checks their viability.
        ''' </summary>
        Friend Overrides Function AddExtensionMethodLookupSymbolsInfoViabilityCheck(method As MethodSymbol, options As LookupOptions, nameSet As LookupSymbolsInfo, originalBinder As Binder) As Boolean
            Return MyBase.AddExtensionMethodLookupSymbolsInfoViabilityCheck(RetargetingTranslator.Retarget(method), options, nameSet, originalBinder)
        End Function

        Friend Overrides Sub AddExtensionMethodLookupSymbolsInfo(nameSet As LookupSymbolsInfo,
                                                                  options As LookupOptions,
                                                                  originalBinder As Binder,
                                                                  appendThrough As NamespaceSymbol)
            ' We should override all callers of this function and go through implementation
            ' provided by the underlying namespace symbol.
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

        Public Overrides Function GetDocumentationCommentXml(Optional preferredCulture As CultureInfo = Nothing, Optional expandIncludes As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As String
            Return _underlyingNamespace.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken)
        End Function
    End Class
End Namespace
