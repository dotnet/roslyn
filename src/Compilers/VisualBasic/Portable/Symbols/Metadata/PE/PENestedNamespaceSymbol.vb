' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Threading
Imports System.Reflection
Imports System.Reflection.Metadata
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE

    ''' <summary>
    ''' The class to represent all, but Global, namespaces imported from a PE/module.
    ''' Namespaces that differ only by casing in name are merged.
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class PENestedNamespaceSymbol
        Inherits PENamespaceSymbol

        ''' <summary>
        ''' The parent namespace. There is always one, Global namespace contains all
        ''' top level namespaces. 
        ''' </summary>
        ''' <remarks></remarks>
        Friend ReadOnly m_ContainingNamespaceSymbol As PENamespaceSymbol

        ''' <summary>
        ''' The name of the namespace.
        ''' </summary>
        ''' <remarks></remarks>
        Protected ReadOnly m_Name As String

        ''' <summary>
        ''' The sequence of groups of TypeDef row ids for types contained within the namespace, 
        ''' recursively including those from nested namespaces. The row ids are grouped by the 
        ''' fully-qualified namespace name in case-sensitive manner. There could be multiple groups 
        ''' for each fully-qualified namespace name. The groups are sorted by their key  
        ''' in case-insensitive manner. Empty string is used as namespace name for types 
        ''' immediately contained within Global namespace. Therefore, all types in this namespace, if any, 
        ''' will be in several first IGroupings.
        ''' 
        ''' This member is initialized by constructor and is cleared in EnsureAllMembersLoaded 
        ''' as soon as symbols for children are created.
        ''' </summary>
        ''' <remarks></remarks>
        Private _typesByNS As IEnumerable(Of IGrouping(Of String, TypeDefinitionHandle))

        ''' <summary>
        ''' Constructor.
        ''' </summary>
        ''' <param name="name">
        ''' Name of the namespace, must be not empty.
        ''' </param>
        ''' <param name="containingNamespace">
        ''' Containing namespace.
        ''' </param>
        ''' <param name="typesByNS">
        ''' The sequence of groups of TypeDef row ids for types contained within the namespace, 
        ''' recursively including those from nested namespaces. The row ids are grouped by the 
        ''' fully-qualified namespace name in case-sensitive manner. There could be multiple groups 
        ''' for each fully-qualified namespace name. The groups are sorted by their key  
        ''' in case-insensitive manner. Empty string is used as namespace name for types 
        ''' immediately contained within Global namespace. Therefore, all types in this namespace, if any, 
        ''' will be in several first IGroupings.
        ''' </param>
        ''' <remarks></remarks>
        Friend Sub New(
            name As String,
            containingNamespace As PENamespaceSymbol,
            typesByNS As IEnumerable(Of IGrouping(Of String, TypeDefinitionHandle))
        )
            Debug.Assert(name IsNot Nothing)
            Debug.Assert(containingNamespace IsNot Nothing)
            Debug.Assert(typesByNS IsNot Nothing)

            m_ContainingNamespaceSymbol = containingNamespace
            m_Name = name
            _typesByNS = typesByNS
        End Sub

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return m_ContainingNamespaceSymbol
            End Get
        End Property

        Friend Overrides ReadOnly Property ContainingPEModule As PEModuleSymbol
            Get
                Return m_ContainingNamespaceSymbol.ContainingPEModule
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return m_Name
            End Get
        End Property

        Public Overrides ReadOnly Property IsGlobalNamespace As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingAssembly As AssemblySymbol
            Get
                Return ContainingPEModule.ContainingAssembly
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingModule As ModuleSymbol
            Get
                Return m_ContainingNamespaceSymbol.ContainingPEModule
            End Get
        End Property

        Protected Overrides Sub EnsureAllMembersLoaded()
            Dim typesByNS = _typesByNS

            If m_lazyTypes Is Nothing OrElse m_lazyMembers Is Nothing Then
                Debug.Assert(typesByNS IsNot Nothing)
                LoadAllMembers(typesByNS)
                Interlocked.Exchange(_typesByNS, Nothing)
            End If
        End Sub

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
            Dim typesByNS As IEnumerable(Of IGrouping(Of String, TypeDefinitionHandle)) = _typesByNS

            If typesByNS IsNot Nothing AndAlso m_lazyTypes Is Nothing Then
                ' Calculate this without creating symbols for children
                Dim [module] = ContainingPEModule.Module
                Dim result As Accessibility = Accessibility.NotApplicable

                For Each group As IGrouping(Of String, TypeDefinitionHandle) In typesByNS
                    For Each typeDef As TypeDefinitionHandle In group
                        Dim flags As TypeAttributes

                        Try
                            flags = [module].GetTypeDefFlagsOrThrow(typeDef)
                        Catch mrEx As BadImageFormatException
                        End Try

                        Select Case (flags And TypeAttributes.VisibilityMask)
                            Case TypeAttributes.Public
                                Return Accessibility.Public

                            Case TypeAttributes.NotPublic
                                result = Accessibility.Friend

                            Case Else
                                Debug.Assert(False, "Unexpected!!!")
                        End Select
                    Next
                Next

                Return result
            Else
                Return MyBase.GetDeclaredAccessibilityOfMostAccessibleDescendantType()
            End If
        End Function

        ''' <remarks>
        ''' This is for perf, not for correctness.
        ''' </remarks>
        Friend Overrides ReadOnly Property DeclaringCompilation As VisualBasicCompilation
            Get
                Return Nothing
            End Get
        End Property
    End Class

End Namespace
