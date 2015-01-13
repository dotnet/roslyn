' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Reflection
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' A MissingMetadataSymbol is a special kind of ErrorSymbol that represents
    ''' a type symbol that was attempted to be read from metadata, but couldn't be
    ''' found, because:
    '''   a) The metadata file it lives in wasn't referenced
    '''   b) The metadata file was referenced, but didn't contain the type
    '''   c) The metadata file was referenced, contained the correct outer type, but
    '''      didn't contains a nested type in that outer type.
    ''' </summary>
    <DebuggerDisplay("{GetDebuggerDisplay(), nq}")>
    Friend MustInherit Class MissingMetadataTypeSymbol
        Inherits InstanceErrorTypeSymbol

        Protected ReadOnly m_Name As String
        Protected ReadOnly m_MangleName As Boolean

        Private Sub New(name As String, arity As Integer, mangleName As Boolean)
            MyBase.New(arity)
            Debug.Assert(name IsNot Nothing)

            m_Name = name
            m_MangleName = mangleName AndAlso arity > 0
        End Sub

        Public Overrides ReadOnly Property Name As String
            Get
                Return m_Name
            End Get
        End Property

        Friend Overrides ReadOnly Property MangleName As Boolean
            Get
                Return m_MangleName
            End Get
        End Property

        Friend Overrides ReadOnly Property ErrorInfo As DiagnosticInfo
            Get
                Dim containingAssembly As AssemblySymbol = Me.ContainingAssembly

                If containingAssembly.IsMissing Then
                    Return ErrorFactory.ErrorInfo(ERRID.ERR_UnreferencedAssembly3, containingAssembly.Identity, Me)
                Else
                    Dim containingModule As ModuleSymbol = Me.ContainingModule

                    If containingModule.IsMissing Then
                        Return ErrorFactory.ErrorInfo(ERRID.ERR_UnreferencedModule3, containingModule.Name, Me)
                    End If

                    Return ErrorFactory.ErrorInfo(ERRID.ERR_TypeRefResolutionError3, Me, containingModule.Name)
                End If
            End Get
        End Property

        ''' <summary>
        ''' Represents not nested missing type.
        ''' </summary>
        <DebuggerDisplay("{GetDebuggerDisplay(), nq}")>
        Friend Class TopLevel
            Inherits MissingMetadataTypeSymbol

            Private ReadOnly m_NamespaceName As String
            Private ReadOnly m_ContainingModule As ModuleSymbol
            Private m_LazyContainingNamespace As NamespaceSymbol

            ''' <summary>
            ''' SpecialType.TypeId
            ''' </summary>
            Private m_LazyTypeId As Integer = (-1)

            Public Sub New([module] As ModuleSymbol, [namespace] As String, name As String, arity As Integer, mangleName As Boolean)
                MyBase.New(name, arity, mangleName)
                Debug.Assert([module] IsNot Nothing)
                Debug.Assert([namespace] IsNot Nothing)

                m_NamespaceName = [namespace]
                m_ContainingModule = [module]
            End Sub

            Public Sub New([module] As ModuleSymbol, ByRef fullname As MetadataTypeName, Optional typeId As SpecialType = CType(-1, SpecialType))
                Me.New([module], fullname, fullname.ForcedArity = -1 OrElse fullname.ForcedArity = fullname.InferredArity)
                Debug.Assert(typeId = CType(-1, SpecialType) OrElse typeId = SpecialType.None OrElse Arity = 0 OrElse MangleName)
                m_LazyTypeId = typeId
            End Sub

            Private Sub New([module] As ModuleSymbol, ByRef fullname As MetadataTypeName, mangleName As Boolean)
                Me.New([module], fullname.NamespaceName,
                       If(mangleName, fullname.UnmangledTypeName, fullname.TypeName),
                       If(mangleName, fullname.InferredArity, fullname.ForcedArity),
                       mangleName)
            End Sub

            ''' <summary>
            ''' This is the FULL namespace name (e.g., "System.Collections.Generic")
            ''' of the type that couldn't be found.
            ''' </summary>
            Public ReadOnly Property NamespaceName As String
                Get
                    Return m_NamespaceName
                End Get
            End Property

            Public Overrides ReadOnly Property ContainingModule As ModuleSymbol
                Get
                    Return m_ContainingModule
                End Get
            End Property

            Public Overrides ReadOnly Property ContainingAssembly As AssemblySymbol
                Get
                    Return m_ContainingModule.ContainingAssembly
                End Get
            End Property

            Public Overrides ReadOnly Property ContainingSymbol As Symbol
                Get
                    If m_LazyContainingNamespace Is Nothing Then
                        Dim container As NamespaceSymbol = m_ContainingModule.GlobalNamespace

                        If m_NamespaceName.Length > 0 Then
                            Dim namespaces = MetadataHelpers.SplitQualifiedName(m_NamespaceName)
                            Dim i As Integer

                            For i = 0 To namespaces.Length - 1 Step 1
                                Dim newContainer As NamespaceSymbol = Nothing
                                Dim nsName As String = namespaces(i)

                                For Each symbol As NamespaceOrTypeSymbol In container.GetMembers(nsName)
                                    If symbol.Kind = SymbolKind.Namespace AndAlso
                                       String.Equals(symbol.Name, nsName, StringComparison.Ordinal) Then

                                        newContainer = DirectCast(symbol, NamespaceSymbol)
                                        Exit For
                                    End If
                                Next

                                If newContainer Is Nothing Then
                                    Exit For
                                End If

                                container = newContainer
                            Next

                            ' now create symbols we couldn't find.
                            While (i < namespaces.Length)
                                container = New MissingNamespaceSymbol(container, namespaces(i))
                                i += 1
                            End While
                        End If

                        Interlocked.CompareExchange(m_LazyContainingNamespace, container, Nothing)
                    End If

                    Return m_LazyContainingNamespace
                End Get
            End Property

            Public Overrides ReadOnly Property SpecialType As SpecialType
                Get
                    If m_LazyTypeId = -1 Then
                        Dim typeId As SpecialType = SpecialType.None
                        Dim containingAssembly As AssemblySymbol = m_ContainingModule.ContainingAssembly

                        If (Arity = 0 OrElse MangleName) AndAlso
                           containingAssembly IsNot Nothing AndAlso containingAssembly Is containingAssembly.CorLibrary AndAlso m_ContainingModule.Ordinal = 0 Then
                            ' Check the name 
                            Dim emittedName As String = MetadataHelpers.BuildQualifiedName(m_NamespaceName, MetadataName)
                            typeId = SpecialTypes.GetTypeFromMetadataName(emittedName)
                        End If

                        Interlocked.CompareExchange(m_LazyTypeId, typeId, -1)
                    End If

                    Return CType(m_LazyTypeId, SpecialType)
                End Get
            End Property

            Public Overrides Function GetHashCode() As Integer
                Return Hash.Combine(m_ContainingModule, Hash.Combine(MetadataName, Hash.Combine(m_NamespaceName, Arity)))
            End Function

            Public Overrides Function Equals(obj As Object) As Boolean
                If Me Is obj Then
                    Return True
                End If

                Dim other = TryCast(obj, TopLevel)

                Return other IsNot Nothing AndAlso String.Equals(MetadataName, other.MetadataName, StringComparison.Ordinal) AndAlso
                    Arity = other.Arity AndAlso
                    String.Equals(m_NamespaceName, other.m_NamespaceName, StringComparison.Ordinal) AndAlso
                    m_ContainingModule.Equals(other.m_ContainingModule)
            End Function

            Friend Overrides Function GetEmittedNamespaceName() As String
                Return m_NamespaceName
            End Function

            Private Function GetDebuggerDisplay() As String
                Dim fullName As String = MetadataHelpers.BuildQualifiedName(m_NamespaceName, m_Name)

                If _arity > 0 Then
                    fullName = fullName & "(Of " & New String(","c, _arity - 1) & ")"
                End If

                Return fullName & "[missing]"
            End Function

        End Class

        Friend Class TopLevelWithCustomErrorInfo
            Inherits TopLevel

            Private ReadOnly m_ErrorInfo As DiagnosticInfo

            Public Sub New(moduleSymbol As ModuleSymbol, ByRef emittedName As MetadataTypeName, errorInfo As DiagnosticInfo, Optional typeId As SpecialType = CType(-1, SpecialType))
                MyBase.New(moduleSymbol, emittedName, typeId)

                Debug.Assert(errorInfo IsNot Nothing)
                Me.m_ErrorInfo = errorInfo
            End Sub

            Friend Overrides ReadOnly Property ErrorInfo As DiagnosticInfo
                Get
                    Return m_ErrorInfo
                End Get
            End Property
        End Class

        ''' <summary>
        ''' Represents nested missing type.
        ''' </summary>
        Friend Class Nested
            Inherits MissingMetadataTypeSymbol

            Private ReadOnly m_ContainingType As NamedTypeSymbol

            Public Sub New(containingType As NamedTypeSymbol, name As String, arity As Integer, mangleName As Boolean)
                MyBase.New(name, arity, mangleName)
                Debug.Assert(containingType IsNot Nothing)

                m_ContainingType = containingType
            End Sub

            Public Sub New(containingType As NamedTypeSymbol, ByRef emittedName As MetadataTypeName)
                Me.New(containingType, emittedName, emittedName.ForcedArity = -1 OrElse emittedName.ForcedArity = emittedName.InferredArity)
            End Sub

            Private Sub New(containingType As NamedTypeSymbol, ByRef emittedName As MetadataTypeName, mangleName As Boolean)
                Me.New(containingType,
                       If(mangleName, emittedName.UnmangledTypeName, emittedName.TypeName),
                       If(mangleName, emittedName.InferredArity, emittedName.ForcedArity),
                       mangleName)
            End Sub

            Public Overrides ReadOnly Property ContainingSymbol As Symbol
                Get
                    Return m_ContainingType
                End Get
            End Property

            Public Overrides ReadOnly Property SpecialType As SpecialType
                Get
                    Return SpecialType.None ' do not have nested types among CORE types yet.
                End Get
            End Property

            Public Overrides Function GetHashCode() As Integer
                Return Hash.Combine(m_ContainingType, Hash.Combine(MetadataName, Arity))
            End Function

            Public Overrides Function Equals(obj As Object) As Boolean
                If Me Is obj Then
                    Return True
                End If

                Dim other = TryCast(obj, Nested)

                Return other IsNot Nothing AndAlso String.Equals(MetadataName, other.MetadataName, StringComparison.Ordinal) AndAlso
                    Arity = other.Arity AndAlso
                    m_ContainingType.Equals(other.m_ContainingType)
            End Function

            Private Function GetDebuggerDisplay() As String
                Dim fullName As String

                fullName = m_ContainingType.ToString() & "." & Me.Name

                If _arity > 0 Then
                    fullName = fullName & "(Of " & New String(","c, _arity - 1) & ")"
                End If

                Return fullName & "[missing]"
            End Function

        End Class

    End Class

End Namespace