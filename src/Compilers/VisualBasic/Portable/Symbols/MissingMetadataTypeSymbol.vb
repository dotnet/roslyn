' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
                    Return ErrorFactory.ErrorInfo(ERRID.ERR_UnreferencedAssembly3,
                                                  CustomSymbolDisplayFormatter.DefaultErrorFormatIfErrorSymbol(containingAssembly.Identity),
                                                  CustomSymbolDisplayFormatter.DefaultErrorFormatIfErrorSymbol(Me))
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

            Private ReadOnly _namespaceName As String
            Private ReadOnly _containingModule As ModuleSymbol
            Private _lazyContainingNamespace As NamespaceSymbol

            ''' <summary>
            ''' SpecialType.TypeId
            ''' </summary>
            Private _lazyTypeId As Integer = (-1)

            Public Sub New([module] As ModuleSymbol, [namespace] As String, name As String, arity As Integer, mangleName As Boolean)
                MyBase.New(name, arity, mangleName)
                Debug.Assert([module] IsNot Nothing)
                Debug.Assert([namespace] IsNot Nothing)

                _namespaceName = [namespace]
                _containingModule = [module]
            End Sub

            Public Sub New([module] As ModuleSymbol, ByRef fullname As MetadataTypeName, Optional typeId As SpecialType = CType(-1, SpecialType))
                Me.New([module], fullname, fullname.ForcedArity = -1 OrElse fullname.ForcedArity = fullname.InferredArity)
                Debug.Assert(typeId = CType(-1, SpecialType) OrElse typeId = SpecialType.None OrElse Arity = 0 OrElse MangleName)
                _lazyTypeId = typeId
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
                    Return _namespaceName
                End Get
            End Property

            Public Overrides ReadOnly Property ContainingModule As ModuleSymbol
                Get
                    Return _containingModule
                End Get
            End Property

            Public Overrides ReadOnly Property ContainingAssembly As AssemblySymbol
                Get
                    Return _containingModule.ContainingAssembly
                End Get
            End Property

            Public Overrides ReadOnly Property ContainingSymbol As Symbol
                Get
                    If _lazyContainingNamespace Is Nothing Then
                        Dim container As NamespaceSymbol = _containingModule.GlobalNamespace

                        If _namespaceName.Length > 0 Then
                            Dim namespaces = MetadataHelpers.SplitQualifiedName(_namespaceName)
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

                        Interlocked.CompareExchange(_lazyContainingNamespace, container, Nothing)
                    End If

                    Return _lazyContainingNamespace
                End Get
            End Property

            Public Overrides ReadOnly Property SpecialType As SpecialType
                Get
                    If _lazyTypeId = -1 Then
                        Dim typeId As SpecialType = SpecialType.None
                        Dim containingAssembly As AssemblySymbol = _containingModule.ContainingAssembly

                        If (Arity = 0 OrElse MangleName) AndAlso
                           containingAssembly IsNot Nothing AndAlso containingAssembly Is containingAssembly.CorLibrary AndAlso _containingModule.Ordinal = 0 Then
                            ' Check the name 
                            Dim emittedName As String = MetadataHelpers.BuildQualifiedName(_namespaceName, MetadataName)
                            typeId = SpecialTypes.GetTypeFromMetadataName(emittedName)
                        End If

                        Interlocked.CompareExchange(_lazyTypeId, typeId, -1)
                    End If

                    Return CType(_lazyTypeId, SpecialType)
                End Get
            End Property

            Public Overrides Function GetHashCode() As Integer
                Return Hash.Combine(_containingModule, Hash.Combine(MetadataName, Hash.Combine(_namespaceName, Arity)))
            End Function

            Public Overrides Function Equals(obj As Object) As Boolean
                If Me Is obj Then
                    Return True
                End If

                Dim other = TryCast(obj, TopLevel)

                Return other IsNot Nothing AndAlso String.Equals(MetadataName, other.MetadataName, StringComparison.Ordinal) AndAlso
                    Arity = other.Arity AndAlso
                    String.Equals(_namespaceName, other._namespaceName, StringComparison.Ordinal) AndAlso
                    _containingModule.Equals(other._containingModule)
            End Function

            Friend Overrides Function GetEmittedNamespaceName() As String
                Return _namespaceName
            End Function

            Private Function GetDebuggerDisplay() As String
                Dim fullName As String = MetadataHelpers.BuildQualifiedName(_namespaceName, m_Name)

                If _arity > 0 Then
                    fullName = fullName & "(Of " & New String(","c, _arity - 1) & ")"
                End If

                Return fullName & "[missing]"
            End Function

        End Class

        Friend Class TopLevelWithCustomErrorInfo
            Inherits TopLevel

            Private ReadOnly _errorInfo As DiagnosticInfo

            Public Sub New(moduleSymbol As ModuleSymbol, ByRef emittedName As MetadataTypeName, errorInfo As DiagnosticInfo, Optional typeId As SpecialType = CType(-1, SpecialType))
                MyBase.New(moduleSymbol, emittedName, typeId)

                Debug.Assert(errorInfo IsNot Nothing)
                Me._errorInfo = errorInfo
            End Sub

            Friend Overrides ReadOnly Property ErrorInfo As DiagnosticInfo
                Get
                    Return _errorInfo
                End Get
            End Property
        End Class

        ''' <summary>
        ''' Represents nested missing type.
        ''' </summary>
        Friend Class Nested
            Inherits MissingMetadataTypeSymbol

            Private ReadOnly _containingType As NamedTypeSymbol

            Public Sub New(containingType As NamedTypeSymbol, name As String, arity As Integer, mangleName As Boolean)
                MyBase.New(name, arity, mangleName)
                Debug.Assert(containingType IsNot Nothing)

                _containingType = containingType
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
                    Return _containingType
                End Get
            End Property

            Public Overrides ReadOnly Property SpecialType As SpecialType
                Get
                    Return SpecialType.None ' do not have nested types among CORE types yet.
                End Get
            End Property

            Public Overrides Function GetHashCode() As Integer
                Return Hash.Combine(_containingType, Hash.Combine(MetadataName, Arity))
            End Function

            Public Overrides Function Equals(obj As Object) As Boolean
                If Me Is obj Then
                    Return True
                End If

                Dim other = TryCast(obj, Nested)

                Return other IsNot Nothing AndAlso String.Equals(MetadataName, other.MetadataName, StringComparison.Ordinal) AndAlso
                    Arity = other.Arity AndAlso
                    _containingType.Equals(other._containingType)
            End Function

            Private Function GetDebuggerDisplay() As String
                Dim fullName As String

                fullName = _containingType.ToString() & "." & Me.Name

                If _arity > 0 Then
                    fullName = fullName & "(Of " & New String(","c, _arity - 1) & ")"
                End If

                Return fullName & "[missing]"
            End Function

        End Class

    End Class

End Namespace
