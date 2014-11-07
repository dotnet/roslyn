' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Concurrent
Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' This class is used to generate the namespace scopes used by CCI when writing out the import lists.
    ''' Because the content is nearly the same for each method (most of it is file and project level) this class
    ''' has an internal cache.
    ''' </summary>
    Friend Class NamespaceScopeBuilder

        ' lazy embedded PIA imports
        Private m_lazyEmbeddedPIAImports As Cci.NamespaceScope

        ' lazy project level imports
        Private m_lazyProjectLevelImports As Cci.NamespaceScope

        ' lazy default/root namespace
        Private m_lazyDefaultNamespaceImport As Cci.NamespaceScope

        ' delegate for adding an element to the file level imports cache
        Private ReadOnly m_buildFileLevelImports As Func(Of SourceFile, Cci.NamespaceScope)

        ' delegate for adding an element to the name string cache
        Private ReadOnly m_buildNamespaceOrTypeString As Func(Of NamespaceOrTypeSymbol, String)

        ' cache to map from source file to namespace scopes
        Private ReadOnly m_sourceLevelImportsCache As ConcurrentDictionary(Of SourceFile, Cci.NamespaceScope)

        ' Cache to map from namespace or type to the string used to represent that namespace/type in the debug info.
        Private ReadOnly m_stringCache As ConcurrentDictionary(Of NamespaceOrTypeSymbol, String)

        Private Shared ReadOnly m_debugFormat As New SymbolDisplayFormat(globalNamespaceStyle:=SymbolDisplayGlobalNamespaceStyle.Omitted,
                                                                         typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces)

        Public Sub New()
            m_sourceLevelImportsCache = New ConcurrentDictionary(Of SourceFile, Cci.NamespaceScope)()
            m_stringCache = New ConcurrentDictionary(Of NamespaceOrTypeSymbol, String)()

            m_buildFileLevelImports = AddressOf BuildFileLevelImports
            m_buildNamespaceOrTypeString = AddressOf BuildNamespaceOrTypeString
        End Sub

        Public Function GetNamespaceScopes(method As MethodSymbol) As ImmutableArray(Of Cci.NamespaceScope)

            Dim sourceModule = DirectCast(method.ContainingModule, SourceModuleSymbol)

            If m_lazyEmbeddedPIAImports Is Nothing Then
                Interlocked.CompareExchange(m_lazyEmbeddedPIAImports, BuildEmbeddedPiaImports(sourceModule), Nothing)
            End If

            If m_lazyProjectLevelImports Is Nothing Then
                Interlocked.CompareExchange(m_lazyProjectLevelImports, BuildProjectLevelImports(sourceModule), Nothing)
            End If

            If m_lazyDefaultNamespaceImport Is Nothing Then
                Interlocked.CompareExchange(m_lazyDefaultNamespaceImport, BuildDefaultNamespace(sourceModule), Nothing)
            End If

            ' Dev11 outputs them in LIFO order, which we can't do this exactly the same way because we store parts of the 
            ' needed information in trees.
            ' The order should be irrelevant because at the end it's a flat list, however we still output file level imports 
            ' before project level imports the same way as Dev11 did.

            Dim sourceLevelImports As Cci.NamespaceScope
            sourceLevelImports = m_sourceLevelImportsCache.GetOrAdd(sourceModule.GetSourceFile(method.Syntax.SyntaxTree),
                                                                    m_buildFileLevelImports)

            Return ImmutableArray.Create(sourceLevelImports,
                                         m_lazyDefaultNamespaceImport,
                                         m_lazyEmbeddedPIAImports,
                                         m_lazyProjectLevelImports,
                                         BuildCurrentNamespace(method))
        End Function

        ''' <remarks>
        ''' Roslyn does not consume this information - it is only emitted for the benefit of legacy EEs.
        ''' See Builder::WriteNoPiaPdbList.
        ''' </remarks>
        Private Function BuildEmbeddedPiaImports([module] As SourceModuleSymbol) As Cci.NamespaceScope
            Dim embeddedPiasBuilder As ArrayBuilder(Of Cci.UsedNamespaceOrType) = Nothing

            For Each referencedAssembly In [module].ReferencedAssemblySymbols
                If referencedAssembly.IsLinked Then
                    If embeddedPiasBuilder Is Nothing Then
                        embeddedPiasBuilder = ArrayBuilder(Of Cci.UsedNamespaceOrType).GetInstance()
                    End If

                    ' NOTE: Dev12 does not seem to emit anything but the name (i.e. no version, token, etc).
                    embeddedPiasBuilder.Add(Cci.UsedNamespaceOrType.CreateVisualBasicEmbeddedPia(referencedAssembly.Name))
                End If
            Next

            Dim embeddedPias = If(embeddedPiasBuilder Is Nothing, ImmutableArray(Of Cci.UsedNamespaceOrType).Empty, embeddedPiasBuilder.ToImmutableAndFree())
            Return New Cci.NamespaceScope(embeddedPias)
        End Function

        Private Function BuildProjectLevelImports([module] As SourceModuleSymbol) As Cci.NamespaceScope
            Return BuildNamespaceScope([module].XmlNamespaces,
                                       [module].AliasImports,
                                       [module].MemberImports,
                                       isProjectLevel:=True)
        End Function

        Private Function BuildFileLevelImports(file As SourceFile) As Cci.NamespaceScope
            Return BuildNamespaceScope(file.XmlNamespaces,
                                       If(file.AliasImports IsNot Nothing, file.AliasImports.Values, Nothing),
                                       file.MemberImports,
                                       isProjectLevel:=False)
        End Function

        Private Function BuildCurrentNamespace(method As MethodSymbol) As Cci.NamespaceScope
            Return New Cci.NamespaceScope(ImmutableArray.Create(
                Cci.UsedNamespaceOrType.CreateVisualBasicCurrentNamespace(GetNamespaceOrTypeString(method.ContainingNamespace))))
        End Function

        Private Function BuildDefaultNamespace([module] As SourceModuleSymbol) As Cci.NamespaceScope
            Dim rootNamespace = [module].RootNamespace
            If rootNamespace IsNot Nothing AndAlso Not rootNamespace.IsGlobalNamespace Then
                Return New Cci.NamespaceScope(ImmutableArray.Create(
                    Cci.UsedNamespaceOrType.CreateVisualBasicDefaultNamespace(GetNamespaceOrTypeString(rootNamespace))))
            Else
                Return Cci.NamespaceScope.Empty
            End If
        End Function

        Private Function BuildNamespaceScope(
            xmlNamespaces As Dictionary(Of String, XmlNamespaceAndImportsClausePosition),
            aliasImports As IEnumerable(Of AliasAndImportsClausePosition),
            memberImports As ImmutableArray(Of NamespaceOrTypeAndImportsClausePosition),
            isProjectLevel As Boolean
        ) As Cci.NamespaceScope
            Dim scopeBuilder = ArrayBuilder(Of Cci.UsedNamespaceOrType).GetInstance

            ' first come xml imports
            If xmlNamespaces IsNot Nothing Then
                For Each xmlImport In xmlNamespaces
                    scopeBuilder.Add(Cci.UsedNamespaceOrType.CreateVisualBasicXmlNamespace(xmlImport.Value.XmlNamespace,
                                                                                       xmlImport.Key,
                                                                                       isProjectLevel))
                Next
            End If

            ' then come alias imports
            If aliasImports IsNot Nothing Then
                For Each aliasImport In aliasImports
                    Dim target = aliasImport.Alias.Target
                    If target.IsNamespace OrElse DirectCast(target, NamedTypeSymbol).Arity = 0 Then
                        scopeBuilder.Add(Cci.UsedNamespaceOrType.CreateVisualBasicNamespaceOrTypeAlias(GetNamespaceOrTypeString(target),
                                                                                                   aliasImport.Alias.Name,
                                                                                                   isProjectLevel))
                    End If
                Next
            End If

            ' then come the imports
            If Not memberImports.IsEmpty Then
                For Each import In memberImports
                    If import.NamespaceOrType.IsNamespace Then
                        scopeBuilder.Add(Cci.UsedNamespaceOrType.CreateVisualBasicNamespace(GetNamespaceOrTypeString(import.NamespaceOrType),
                                                                                        isProjectLevel))

                    Else
                        If DirectCast(import.NamespaceOrType, NamedTypeSymbol).Arity = 0 Then
                            scopeBuilder.Add(Cci.UsedNamespaceOrType.CreateVisualBasicType(GetNamespaceOrTypeString(import.NamespaceOrType),
                                                                                       isProjectLevel))

                        End If
                    End If
                Next
            End If

            Dim scope = If(scopeBuilder.Count = 0, Cci.NamespaceScope.Empty, New Cci.NamespaceScope(scopeBuilder.ToImmutable()))
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