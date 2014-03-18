' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Concurrent
Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' This class is used to generate the namespace scopes used by CCI when writing out the import lists.
    ''' Because the content is nearly the same for each method (most of it is file and project level) this class
    ''' has an internal cache.
    ''' </summary>
    Friend Class NamespaceScopeBuilder

        ' lazy project level imports
        Private m_lazyProjectLevelImports As NamespaceScope

        ' lazy default/root namespace
        Private m_lazyDefaultNamespaceImport As NamespaceScope

        ' delegate for adding an element to the file level imports cache
        Private ReadOnly m_buildFileLevelImports As Func(Of SourceFile, NamespaceScope)

        ' delegate for adding an element to the name string cache
        Private ReadOnly m_buildNamespaceOrTypeString As Func(Of NamespaceOrTypeSymbol, String)

        ' cache to map from source file to namespace scopes
        Private ReadOnly m_sourceLevelImportsCache As ConcurrentDictionary(Of SourceFile, NamespaceScope)

        ' Cache to map from namespace or type to the string used to represent that namespace/type in the debug info.
        Private ReadOnly m_stringCache As ConcurrentDictionary(Of NamespaceOrTypeSymbol, String)

        Private Shared ReadOnly m_debugFormat As New SymbolDisplayFormat(globalNamespaceStyle:=SymbolDisplayGlobalNamespaceStyle.Omitted,
                                                                         typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces)

        Public Sub New()
            m_sourceLevelImportsCache = New ConcurrentDictionary(Of SourceFile, NamespaceScope)()
            m_stringCache = New ConcurrentDictionary(Of NamespaceOrTypeSymbol, String)()

            m_buildFileLevelImports = AddressOf BuildFileLevelImports
            m_buildNamespaceOrTypeString = AddressOf BuildNamespaceOrTypeString
        End Sub

        Public Function GetNamespaceScopes(method As MethodSymbol) As ImmutableArray(Of NamespaceScope)

            Dim sourceModule = DirectCast(method.ContainingModule, SourceModuleSymbol)

            If m_lazyProjectLevelImports Is Nothing Then
                Interlocked.CompareExchange(m_lazyProjectLevelImports, BuildProjectLevelImports(sourceModule), Nothing)
            End If

            If m_lazyDefaultNamespaceImport Is Nothing Then
                Interlocked.CompareExchange(m_lazyDefaultNamespaceImport, BuildDefaultNamespace(sourceModule), Nothing)
            End If

            ' Dev11 outputs them in LIFO order, which we can't do this exactly the same way because we store parts of the 
            ' needed information in tree's.
            ' The order should be irrelevant because at the end it's a flat list, however we still output file level imports 
            ' before project level imports the same way as Dev11 did.

            Dim sourceLevelImports As NamespaceScope
            sourceLevelImports = m_sourceLevelImportsCache.GetOrAdd(sourceModule.GetSourceFile(method.Syntax.SyntaxTree),
                                                                    m_buildFileLevelImports)

            Return ImmutableArray.Create(Of NamespaceScope)(sourceLevelImports,
                                                           m_lazyDefaultNamespaceImport,
                                                           m_lazyProjectLevelImports,
                                                           BuildCurrentNamespace(method))
        End Function

        Private Function BuildProjectLevelImports([module] As SourceModuleSymbol) As NamespaceScope
            Return BuildNamespaceScope([module].XmlNamespaces,
                                       [module].AliasImports,
                                       [module].MemberImports,
                                       isProjectLevel:=True)
        End Function

        Private Function BuildFileLevelImports(file As SourceFile) As NamespaceScope
            Return BuildNamespaceScope(file.XmlNamespaces,
                                       If(file.AliasImports IsNot Nothing, file.AliasImports.Values, Nothing),
                                       file.MemberImports,
                                       isProjectLevel:=False)
        End Function

        Private Function BuildCurrentNamespace(method As MethodSymbol) As NamespaceScope
            Return New NamespaceScope(ImmutableArray.Create(Of UsedNamespaceOrType)(
                                        UsedNamespaceOrType.CreateVisualBasicCurrentNamespace(
                                            GetNamespaceOrTypeString(method.ContainingNamespace))))
        End Function

        Private Function BuildDefaultNamespace([module] As SourceModuleSymbol) As NamespaceScope
            Dim rootNamespace = [module].RootNamespace
            If rootNamespace IsNot Nothing AndAlso Not rootNamespace.IsGlobalNamespace Then
                Return New NamespaceScope(ImmutableArray.Create(Of UsedNamespaceOrType)(
                                            UsedNamespaceOrType.CreateVisualBasicDefaultNamespace(
                                                GetNamespaceOrTypeString(rootNamespace))))
            Else
                Return NamespaceScope.Empty
            End If
        End Function

        Private Function BuildNamespaceScope(
            xmlNamespaces As Dictionary(Of String, XmlNamespaceAndImportsClausePosition),
            aliasImports As IEnumerable(Of AliasAndImportsClausePosition),
            memberImports As ImmutableArray(Of NamespaceOrTypeAndImportsClausePosition),
            isProjectLevel As Boolean
        ) As NamespaceScope
            Dim scopeBuilder = ArrayBuilder(Of UsedNamespaceOrType).GetInstance

            ' first come xml imports
            If xmlNamespaces IsNot Nothing Then
                For Each xmlImport In xmlNamespaces
                    scopeBuilder.Add(UsedNamespaceOrType.CreateVisualBasicXmlNamespace(xmlImport.Value.XmlNamespace,
                                                                                       xmlImport.Key,
                                                                                       isProjectLevel))
                Next
            End If

            ' then come alias imports
            If aliasImports IsNot Nothing Then
                For Each aliasImport In aliasImports
                    Dim target = aliasImport.Alias.Target
                    If target.IsNamespace OrElse DirectCast(target, NamedTypeSymbol).Arity = 0 Then
                        scopeBuilder.Add(UsedNamespaceOrType.CreateVisualBasicNamespaceOrTypeAlias(GetNamespaceOrTypeString(target),
                                                                                                   aliasImport.Alias.Name,
                                                                                                   isProjectLevel))
                    End If
                Next
            End If

            ' then come the imports
            If Not memberImports.IsEmpty Then
                For Each import In memberImports
                    If import.NamespaceOrType.IsNamespace Then
                        scopeBuilder.Add(UsedNamespaceOrType.CreateVisualBasicNamespace(GetNamespaceOrTypeString(import.NamespaceOrType),
                                                                                        isProjectLevel))

                    Else
                        If DirectCast(import.NamespaceOrType, NamedTypeSymbol).Arity = 0 Then
                            scopeBuilder.Add(UsedNamespaceOrType.CreateVisualBasicType(GetNamespaceOrTypeString(import.NamespaceOrType),
                                                                                       isProjectLevel))

                        End If
                    End If
                Next
            End If

            Dim scope = If(scopeBuilder.Count = 0, NamespaceScope.Empty, New NamespaceScope(scopeBuilder.ToImmutable()))
            scopeBuilder.Free()

            Return scope
        End Function

        Private Function GetNamespaceOrTypeString(symbol As NamespaceOrTypeSymbol) As String
            Return m_stringCache.GetOrAdd(symbol, m_buildNamespaceOrTypeString)
        End Function

        Private Function BuildNamespaceOrTypeString(symbol As NamespaceOrTypeSymbol) As String
            Return symbol.ToDisplayString(m_debugFormat)
        End Function
    End Class
End Namespace