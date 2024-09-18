' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.IO
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Xml.Linq
Imports Basic.Reference.Assemblies
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Test.Utilities
Imports Roslyn.Test.Utilities.TestBase
Imports Roslyn.Test.Utilities.TestMetadata
Imports Xunit

Friend Module CompilationUtils

    Private Function ParseSources(source As IEnumerable(Of String), parseOptions As VisualBasicParseOptions) As IEnumerable(Of SyntaxTree)
        Return source.Select(Function(s) VisualBasicSyntaxTree.ParseText(SourceText.From(s, encoding:=Nothing, SourceHashAlgorithms.Default), parseOptions))
    End Function

    Public Function CreateCompilation(
            source As BasicTestSource,
            Optional references As IEnumerable(Of MetadataReference) = Nothing,
            Optional options As VisualBasicCompilationOptions = Nothing,
            Optional parseOptions As VisualBasicParseOptions = Nothing,
            Optional targetFramework As TargetFramework = TargetFramework.StandardAndVBRuntime,
            Optional assemblyName As String = Nothing) As VisualBasicCompilation
        references = TargetFrameworkUtil.GetReferences(targetFramework, references)
        Return CreateEmptyCompilation(source, references, options, parseOptions, assemblyName)
    End Function

    Public Function CreateCompilationWithIdentity(
            identity As AssemblyIdentity,
            source As BasicTestSource,
            Optional references As IEnumerable(Of MetadataReference) = Nothing,
            Optional targetFramework As TargetFramework = TargetFramework.StandardAndVBRuntime) As VisualBasicCompilation

        Dim c = CreateCompilation(source, references, assemblyName:=identity.Name)
        Assert.NotNull(c.Assembly) ' force creation Of SourceAssemblySymbol
        DirectCast(c.Assembly, SourceAssemblySymbol).m_lazyIdentity = identity

        Return c
    End Function

    Public Function CreateEmptyCompilation(
            source As BasicTestSource,
            Optional references As IEnumerable(Of MetadataReference) = Nothing,
            Optional options As VisualBasicCompilationOptions = Nothing,
            Optional parseOptions As VisualBasicParseOptions = Nothing,
            Optional assemblyName As String = Nothing) As VisualBasicCompilation

        If options Is Nothing Then
            options = TestOptions.ReleaseDll
        End If

        ' Using single-threaded build if debugger attached, to simplify debugging.
        If Debugger.IsAttached Then
            options = options.WithConcurrentBuild(False)
        End If

        Dim trees = source.GetSyntaxTrees(parseOptions, assemblyName)
        Dim createCompilationLambda = Function()
                                          Return VisualBasicCompilation.Create(
                                            If(assemblyName, GetUniqueName()),
                                            trees,
                                            references,
                                            options)
                                      End Function
        ValidateCompilation(createCompilationLambda)
        Return createCompilationLambda()
    End Function

    Private Sub ValidateCompilation(createCompilationLambda As Func(Of VisualBasicCompilation))
        CompilationExtensions.ValidateIOperations(createCompilationLambda)
        VerifyUsedAssemblyReferences(createCompilationLambda)
    End Sub

    Private Sub VerifyUsedAssemblyReferences(createCompilationLambda As Func(Of VisualBasicCompilation))

        If Not CompilationExtensions.EnableVerifyUsedAssemblies Then
            Return
        End If

        Dim comp = createCompilationLambda()
        Dim used = comp.GetUsedAssemblyReferences()

        Dim compileDiagnostics = comp.GetDiagnostics()
        Dim emitDiagnostics = comp.GetEmitDiagnostics()

        Dim resolvedReferences = comp.References.Where(Function(r) r.Properties.Kind = MetadataImageKind.Assembly)

        If Not compileDiagnostics.Any(Function(d) d.DefaultSeverity = DiagnosticSeverity.Error) Then

            If resolvedReferences.Count() > used.Length Then
                AssertSubset(used, resolvedReferences)

                If Not compileDiagnostics.Any(Function(d) d.Code = ERRID.HDN_UnusedImportClause OrElse d.Code = ERRID.HDN_UnusedImportStatement) Then
                    Dim comp2 = comp.RemoveAllReferences().AddReferences(used.Concat(comp.References.Where(Function(r) r.Properties.Kind = MetadataImageKind.Module)))
                    comp2.GetEmitDiagnostics().Verify(
                        emitDiagnostics.Select(Function(d) New DiagnosticDescription(d, errorCodeOnly:=False, includeDefaultSeverity:=False, includeEffectiveSeverity:=False)).ToArray())
                End If
            Else
                AssertEx.Equal(resolvedReferences, used)
            End If
        Else
            AssertSubset(used, resolvedReferences)
        End If
    End Sub

    Private Sub AssertSubset(used As ImmutableArray(Of MetadataReference), resolvedReferences As IEnumerable(Of MetadataReference))
        For Each reference In used
            Assert.Contains(reference, resolvedReferences)
        Next
    End Sub

    Public Function CreateEmptyCompilation(
            identity As AssemblyIdentity,
            source As BasicTestSource,
            Optional references As MetadataReference() = Nothing,
            Optional options As VisualBasicCompilationOptions = Nothing) As VisualBasicCompilation

        Dim trees = source.GetSyntaxTrees()
        Dim createCompilationLambda = Function()
                                          Return VisualBasicCompilation.Create(identity.Name, trees, references, options)
                                      End Function
        ValidateCompilation(createCompilationLambda)
        Dim c = createCompilationLambda()
        Assert.NotNull(c.Assembly) ' force creation of SourceAssemblySymbol

        DirectCast(c.Assembly, SourceAssemblySymbol).m_lazyIdentity = identity
        Return c
    End Function

    Public Function CreateCompilationWithMscorlib40(
            source As BasicTestSource,
            Optional references As IEnumerable(Of MetadataReference) = Nothing,
            Optional options As VisualBasicCompilationOptions = Nothing,
            Optional parseOptions As VisualBasicParseOptions = Nothing,
            Optional assemblyName As String = Nothing) As VisualBasicCompilation
        Return CreateCompilation(source, references, options, parseOptions, TargetFramework.Mscorlib40, assemblyName)
    End Function

    Public Function CreateCompilationWithMscorlib461(
            source As BasicTestSource,
            Optional references As IEnumerable(Of MetadataReference) = Nothing,
            Optional options As VisualBasicCompilationOptions = Nothing,
            Optional parseOptions As VisualBasicParseOptions = Nothing,
            Optional assemblyName As String = Nothing) As VisualBasicCompilation
        Return CreateCompilation(source, references, options, parseOptions, TargetFramework.Mscorlib461, assemblyName)
    End Function

    Public Function CreateCompilationWithMscorlib461AndVBRuntime(
            source As BasicTestSource,
            Optional references As IEnumerable(Of MetadataReference) = Nothing,
            Optional options As VisualBasicCompilationOptions = Nothing,
            Optional parseOptions As VisualBasicParseOptions = Nothing,
            Optional assemblyName As String = Nothing) As VisualBasicCompilation
        Return CreateCompilation(source, references, options, parseOptions, TargetFramework.Mscorlib461AndVBRuntime, assemblyName)
    End Function

    Public Function CreateCompilationWithWinRt(source As XElement) As VisualBasicCompilation
        Return CreateEmptyCompilationWithReferences(source, WinRtRefs)
    End Function

    Public Function CreateCompilationWithMscorlib40AndReferences(source As XElement,
                                                               references As IEnumerable(Of MetadataReference),
                                                               Optional options As VisualBasicCompilationOptions = Nothing,
                                                               Optional parseOptions As VisualBasicParseOptions = Nothing) As VisualBasicCompilation
        Return CreateEmptyCompilationWithReferences(source, {CType(Net40.References.mscorlib, MetadataReference)}.Concat(references), options, parseOptions:=parseOptions)
    End Function

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="source">The sources compile according to the following schema        
    ''' &lt;compilation name="assemblyname[optional]"&gt;
    ''' &lt;file name="file1.vb[optional]"&gt;
    ''' source
    ''' &lt;/file&gt;
    ''' &lt;/compilation&gt;
    ''' </param>
    Public Function CreateCompilationWithMscorlib40(source As XElement,
                                                  outputKind As OutputKind,
                                                  Optional parseOptions As VisualBasicParseOptions = Nothing) As VisualBasicCompilation
        Return CreateEmptyCompilationWithReferences(source, {Net40.References.mscorlib}, New VisualBasicCompilationOptions(outputKind), parseOptions:=parseOptions)
    End Function

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="source">The sources compile according to the following schema        
    ''' &lt;compilation name="assemblyname[optional]"&gt;
    ''' &lt;file name="file1.vb[optional]"&gt;
    ''' source
    ''' &lt;/file&gt;
    ''' &lt;/compilation&gt;
    ''' </param>
    Public Function CreateCompilationWithMscorlib40AndVBRuntime(
        source As XElement,
        Optional additionalRefs As MetadataReference() = Nothing,
        Optional options As VisualBasicCompilationOptions = Nothing,
        Optional parseOptions As VisualBasicParseOptions = Nothing,
        Optional assemblyName As String = Nothing) As VisualBasicCompilation

        If additionalRefs Is Nothing Then additionalRefs = {}
        Dim references = {CType(Net40.References.mscorlib, MetadataReference), Net40.References.System, Net40.References.MicrosoftVisualBasic}.Concat(additionalRefs)

        Return CreateEmptyCompilationWithReferences(source, references, options, parseOptions:=parseOptions, assemblyName:=assemblyName)
    End Function

    Public Function CreateCompilationWithMscorlib40AndVBRuntime(source As IEnumerable(Of SyntaxTree),
                                                              options As VisualBasicCompilationOptions,
                                                              Optional assemblyName As String = Nothing) As VisualBasicCompilation

        Dim references = {MscorlibRef, SystemRef, MsvbRef}
        Return CreateEmptyCompilation(source.ToArray(), references, options:=options, assemblyName:=assemblyName)
    End Function

    Public Function CreateCompilationWithMscorlib40AndVBRuntime(source As XElement,
                                                              options As VisualBasicCompilationOptions) As VisualBasicCompilation
        Return CreateCompilationWithMscorlib40AndVBRuntime(source, Nothing, options, parseOptions:=If(options Is Nothing, Nothing, options.ParseOptions))
    End Function

    Public ReadOnly XmlReferences As MetadataReference() = {SystemRef, SystemCoreRef, SystemXmlRef, SystemXmlLinqRef}

    Public ReadOnly Net40XmlReferences As MetadataReference() = {Net40.References.SystemCore, Net40.References.SystemXml, Net40.References.SystemXmlLinq}

    Public ReadOnly Net461XmlReferences As MetadataReference() = {Net461.References.SystemCore, Net461.References.SystemXml, Net461.References.SystemXmlLinq}

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="source">The sources compile according to the following schema        
    ''' &lt;compilation name="assemblyname[optional]"&gt;
    ''' &lt;file name="file1.vb[optional]"&gt;
    ''' source
    ''' &lt;/file&gt;
    ''' &lt;/compilation&gt;
    ''' </param>
    Public Function CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
        source As XElement,
        Optional references As IEnumerable(Of MetadataReference) = Nothing,
        Optional options As VisualBasicCompilationOptions = Nothing,
        Optional parseOptions As VisualBasicParseOptions = Nothing) As VisualBasicCompilation

        If references Is Nothing Then references = {}
        Dim allReferences = {CType(Net40.References.mscorlib, MetadataReference), Net40.References.System, Net40.References.MicrosoftVisualBasic}.Concat(references)
        If parseOptions Is Nothing AndAlso options IsNot Nothing Then
            parseOptions = options.ParseOptions
        End If

        Return CreateEmptyCompilationWithReferences(source, allReferences, options, parseOptions:=parseOptions)
    End Function

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="source">The sources compile according to the following schema        
    ''' &lt;compilation name="assemblyname[optional]"&gt;
    ''' &lt;file name="file1.vb[optional]"&gt;
    ''' source
    ''' &lt;/file&gt;
    ''' &lt;/compilation&gt;
    ''' </param>
    Public Function CreateCompilationWithMscorlib45AndVBRuntime(
        source As XElement,
        Optional references As IEnumerable(Of MetadataReference) = Nothing,
        Optional options As VisualBasicCompilationOptions = Nothing,
        Optional parseOptions As VisualBasicParseOptions = Nothing) As VisualBasicCompilation

        Dim allReferences = {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}.Concat(If(references, {}))
        Return CreateEmptyCompilationWithReferences(source, allReferences, options, parseOptions:=parseOptions)
    End Function

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="source">The sources compile according to the following schema        
    ''' &lt;compilation name="assemblyname[optional]"&gt;
    ''' &lt;file name="file1.vb[optional]"&gt;
    ''' source
    ''' &lt;/file&gt;
    ''' &lt;/compilation&gt;
    ''' </param>
    Public Function CreateEmptyCompilationWithReferences(source As XElement,
                                                    references As IEnumerable(Of MetadataReference),
                                                    Optional options As VisualBasicCompilationOptions = Nothing,
                                                    Optional parseOptions As VisualBasicParseOptions = Nothing,
                                                    Optional assemblyName As String = Nothing) As VisualBasicCompilation
        Dim sourceTrees = ParseSourceXml(source, parseOptions, assemblyName)
        Return CreateEmptyCompilationWithReferences(sourceTrees, references, options, assemblyName)
    End Function

    Public Function ParseSourceXml(sources As XElement,
                                  parseOptions As VisualBasicParseOptions,
                                  Optional ByRef assemblyName As String = Nothing,
                                  Optional ByRef spans As IEnumerable(Of IEnumerable(Of TextSpan)) = Nothing) As IEnumerable(Of SyntaxTree)

        If sources.@name IsNot Nothing Then
            assemblyName = sources.@name
        End If

        Dim sourcesTreesAndSpans = From f In sources.<file> Select CreateParseTreeAndSpans(f, parseOptions)
        spans = From t In sourcesTreesAndSpans Select s = t.spans
        Return From t In sourcesTreesAndSpans Select t.tree
    End Function

    Public Function ToSourceTrees(compilationSources As XElement, Optional parseOptions As VisualBasicParseOptions = Nothing) As IEnumerable(Of SyntaxTree)
        Dim sourcesTreesAndSpans = From f In compilationSources.<file> Select CreateParseTreeAndSpans(f, parseOptions)
        Return From t In sourcesTreesAndSpans Select t.tree
    End Function

    Public Function CreateEmptyCompilationWithReferences(source As SyntaxTree,
                                                    references As IEnumerable(Of MetadataReference),
                                                    Optional options As VisualBasicCompilationOptions = Nothing,
                                                    Optional assemblyName As String = Nothing) As VisualBasicCompilation
        Return CreateEmptyCompilationWithReferences({source}, references, options, assemblyName)
    End Function

    Public Function CreateEmptyCompilationWithReferences(source As IEnumerable(Of SyntaxTree),
                                                    references As IEnumerable(Of MetadataReference),
                                                    Optional options As VisualBasicCompilationOptions = Nothing,
                                                    Optional assemblyName As String = Nothing) As VisualBasicCompilation
        If options Is Nothing Then
            options = TestOptions.ReleaseDll

            ' Using single-threaded build if debugger attached, to simplify debugging.
            If Debugger.IsAttached Then
                options = options.WithConcurrentBuild(False)
            End If
        End If
        Dim createCompilationLambda = Function()
                                          Return VisualBasicCompilation.Create(If(assemblyName, GetUniqueName()), source, references, options)
                                      End Function
        ValidateCompilation(createCompilationLambda)
        Return createCompilationLambda()
    End Function

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="sources">The sources compile according to the following schema        
    ''' &lt;compilation name="assemblyname[optional]"&gt;
    ''' &lt;file name="file1.vb[optional]"&gt;
    ''' source
    ''' &lt;/file&gt;
    ''' &lt;/compilation&gt;
    ''' </param>
    ''' <param name="ilSource"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function CreateCompilationWithCustomILSource(sources As XElement, ilSource As XCData) As VisualBasicCompilation
        Return CreateCompilationWithCustomILSource(sources, ilSource.Value)
    End Function

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="sources">The sources compile according to the following schema        
    ''' &lt;compilation name="assemblyname[optional]"&gt;
    ''' &lt;file name="file1.vb[optional]"&gt;
    ''' source
    ''' &lt;/file&gt;
    ''' &lt;/compilation&gt;
    ''' </param>
    ''' <param name="ilSource"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function CreateCompilationWithCustomILSource(sources As XElement,
                                                        ilSource As String,
                                                        Optional options As VisualBasicCompilationOptions = Nothing,
                                                        Optional ByRef spans As IEnumerable(Of IEnumerable(Of TextSpan)) = Nothing,
                                                        Optional includeVbRuntime As Boolean = False,
                                                        Optional includeSystemCore As Boolean = False,
                                                        Optional appendDefaultHeader As Boolean = True,
                                                        Optional parseOptions As VisualBasicParseOptions = Nothing,
                                                        Optional additionalReferences As IEnumerable(Of MetadataReference) = Nothing,
                                                        <Out> Optional ByRef ilReference As MetadataReference = Nothing,
                                                        <Out> Optional ByRef ilImage As ImmutableArray(Of Byte) = Nothing
    ) As VisualBasicCompilation
        Dim references = If(additionalReferences IsNot Nothing, New List(Of MetadataReference)(additionalReferences), New List(Of MetadataReference))
        If includeVbRuntime Then
            references.Add(MsvbRef)
        End If
        If includeSystemCore Then
            references.Add(SystemCoreRef)
        End If

        If ilSource Is Nothing Then
            Return CreateCompilationWithMscorlib40AndReferences(sources, references, options, parseOptions)
        End If

        ilReference = CreateReferenceFromIlCode(ilSource, appendDefaultHeader, ilImage)
        references.Add(ilReference)

        Return CreateCompilationWithMscorlib40AndReferences(sources, references, options, parseOptions)
    End Function

    Public Function CreateReferenceFromIlCode(ilSource As String, Optional appendDefaultHeader As Boolean = True, <Out> Optional ByRef ilImage As ImmutableArray(Of Byte) = Nothing) As MetadataReference
        Using reference = IlasmUtilities.CreateTempAssembly(ilSource, appendDefaultHeader)
            ilImage = ImmutableArray.Create(File.ReadAllBytes(reference.Path))
        End Using
        Return MetadataReference.CreateFromImage(ilImage)
    End Function

    Public Function GetUniqueName() As String
        Return Guid.NewGuid().ToString("D")
    End Function

    ' Filter text from within an XElement
    Public Function FilterString(s As String) As String
        s = s.Replace(vbCrLf, vbLf) ' If there are already "0d0a", don't replace them with "0d0a0a"
        s = s.Replace(vbLf, vbCrLf)
        Dim needToAddBackNewline = s.EndsWith(vbCrLf, StringComparison.Ordinal)
        s = s.Trim()
        If needToAddBackNewline Then s &= vbCrLf
        Return s
    End Function

    Public Function FindBindingText(Of TNode As SyntaxNode)(compilation As Compilation, Optional fileName As String = Nothing, Optional which As Integer = 0, Optional prefixMatch As Boolean = False) As TNode
        Dim trees = If(fileName Is Nothing, compilation.SyntaxTrees, compilation.SyntaxTrees.Where(Function(t) t.FilePath = fileName))
        Dim tree = trees.Single()

        Dim bindText As String = Nothing
        Dim bindPoint = FindBindingTextPosition(compilation, fileName, bindText, which)
        Dim token = tree.GetRoot().FindToken(bindPoint, True)
        Dim node = token.Parent
        Dim hasMatchingText As Func(Of SyntaxNode, Boolean) = Function(n) n.ToString = bindText OrElse
            (prefixMatch AndAlso TryCast(n, TNode) IsNot Nothing AndAlso n.ToString.StartsWith(bindText))

        While (node IsNot Nothing AndAlso Not hasMatchingText(node))
            node = node.Parent
        End While

        If node IsNot Nothing Then
            While TryCast(node, TNode) Is Nothing
                If node.Parent IsNot Nothing AndAlso hasMatchingText(node.Parent) Then
                    node = node.Parent
                Else
                    Exit While
                End If
            End While
        End If

        Assert.NotNull(node)  ' If this trips, then node  wasn't found
        Assert.IsAssignableFrom(GetType(TNode), node)
        If Not prefixMatch Then
            Assert.Equal(bindText, node.ToString())
        Else
            Assert.StartsWith(bindText, node.ToString)
        End If

        Return DirectCast(node, TNode)
    End Function

    Public Function FindBindingTextPosition(compilation As Compilation, fileName As String, ByRef bindText As String, Optional which As Integer = 0) As Integer
        Dim tree = (From t In compilation.SyntaxTrees Where t.FilePath = fileName).Single()

        Dim bindMarker As String
        If which > 0 Then
            bindMarker = "'BIND" & which.ToString() & ":"""
        Else
            bindMarker = "'BIND:"""
        End If

        Dim text As String = tree.GetRoot().ToFullString()
        Dim startCommentIndex As Integer = text.IndexOf(bindMarker, StringComparison.Ordinal) + bindMarker.Length

        Dim endCommentIndex As Integer = text.Length
        Dim endOfLineIndex = text.IndexOfAny({CChar(vbLf), CChar(vbCr)}, startCommentIndex)
        If endOfLineIndex > -1 Then
            endCommentIndex = endOfLineIndex
        End If

        ' There may be more than one 'BIND{1234...} marker per line
        Dim nextMarkerIndex = text.IndexOf("'BIND", startCommentIndex, endCommentIndex - startCommentIndex, StringComparison.Ordinal)
        If nextMarkerIndex > -1 Then
            endCommentIndex = nextMarkerIndex
        End If

        Dim commentText = text.Substring(startCommentIndex, endCommentIndex - startCommentIndex)

        Dim endBindCommentLength = commentText.LastIndexOf(""""c)
        If endBindCommentLength = 0 Then
            ' This cannot be 0 so it must be text that is quoted.  Look for double ending quote
            ' 'Bind:""some quoted string""
            endBindCommentLength = commentText.LastIndexOf("""""", 1, StringComparison.Ordinal)
        End If

        bindText = commentText.Substring(0, endBindCommentLength)
        Dim bindPoint = text.LastIndexOf(bindText, startCommentIndex - bindMarker.Length, StringComparison.Ordinal)
        Return bindPoint
    End Function

    Public Function FindBindingTextPosition(compilation As Compilation, fileName As String) As Integer
        Dim bindText As String = Nothing
        Return FindBindingTextPosition(compilation, fileName, bindText)
    End Function

    Public Function FindBindingStartText(Of TNode As SyntaxNode)(compilation As Compilation, fileName As String, Optional which As Integer = 0) As TNode
        Dim tree = (From t In compilation.SyntaxTrees Where t.FilePath = fileName).Single()

        Dim bindText As String = Nothing
        Dim bindPoint = FindBindingTextPosition(compilation, fileName, bindText, which)
        Dim token As SyntaxToken = tree.GetRoot().FindToken(bindPoint)
        Dim node = token.Parent

        While (node IsNot Nothing AndAlso node.ToString.StartsWith(bindText, StringComparison.Ordinal) AndAlso Not (TypeOf node Is TNode))
            node = node.Parent
        End While

        Assert.NotNull(node)  ' If this trips, then node  wasn't found
        Assert.IsAssignableFrom(GetType(TNode), node)
        Assert.Contains(bindText, node.ToString(), StringComparison.Ordinal)

        Return DirectCast(node, TNode)
    End Function

    Friend Class SemanticInfoSummary
        Public Symbol As Symbol = Nothing
        Public CandidateReason As CandidateReason = CandidateReason.None
        Public CandidateSymbols As ImmutableArray(Of ISymbol) = ImmutableArray.Create(Of ISymbol)()
        Public AllSymbols As ImmutableArray(Of ISymbol) = ImmutableArray.Create(Of ISymbol)()
        Public [Type] As ITypeSymbol = Nothing
        Public ConvertedType As ITypeSymbol = Nothing
        Public ImplicitConversion As Conversion = Nothing
        Public MemberGroup As ImmutableArray(Of ISymbol) = ImmutableArray.Create(Of ISymbol)()
        Public [Alias] As IAliasSymbol = Nothing
        Public ConstantValue As [Optional](Of Object) = Nothing
    End Class

    <Extension()>
    Public Function GetSemanticInfoSummary(model As SemanticModel, node As SyntaxNode) As SemanticInfoSummary
        Dim summary As New SemanticInfoSummary

        ' The information that is available varies by the type of the syntax node.
        Dim semanticModel = DirectCast(model, VBSemanticModel)
        Dim symbolInfo As SymbolInfo
        If TypeOf node Is ExpressionSyntax Then
            symbolInfo = semanticModel.GetSymbolInfo(DirectCast(node, ExpressionSyntax))
            summary.MemberGroup = semanticModel.GetMemberGroup(DirectCast(node, ExpressionSyntax))
            summary.ConstantValue = semanticModel.GetConstantValue(DirectCast(node, ExpressionSyntax))
            Dim typeInfo = semanticModel.GetTypeInfo(DirectCast(node, ExpressionSyntax))
            summary.Type = DirectCast(typeInfo.Type, TypeSymbol)
            summary.ConvertedType = DirectCast(typeInfo.ConvertedType, TypeSymbol)
            summary.ImplicitConversion = semanticModel.GetConversion(DirectCast(node, ExpressionSyntax))
        ElseIf TypeOf node Is AttributeSyntax Then
            symbolInfo = semanticModel.GetSymbolInfo(DirectCast(node, AttributeSyntax))
            summary.MemberGroup = semanticModel.GetMemberGroup(DirectCast(node, AttributeSyntax))
            Dim typeInfo = semanticModel.GetTypeInfo(DirectCast(node, AttributeSyntax))
            summary.Type = DirectCast(typeInfo.Type, TypeSymbol)
            summary.ConvertedType = DirectCast(typeInfo.ConvertedType, TypeSymbol)
            summary.ImplicitConversion = semanticModel.GetConversion(DirectCast(node, AttributeSyntax))
        ElseIf TypeOf node Is QueryClauseSyntax Then
            symbolInfo = semanticModel.GetSymbolInfo(DirectCast(node, QueryClauseSyntax))
        Else
            Throw New NotSupportedException("Type of syntax node is not supported by GetSemanticInfo")
        End If
        Assert.NotNull(symbolInfo)
        summary.Symbol = DirectCast(symbolInfo.Symbol, Symbol)
        summary.CandidateReason = symbolInfo.CandidateReason
        summary.CandidateSymbols = symbolInfo.CandidateSymbols
        summary.AllSymbols = symbolInfo.GetAllSymbols()

        If TypeOf node Is IdentifierNameSyntax Then
            summary.Alias = semanticModel.GetAliasInfo(DirectCast(node, IdentifierNameSyntax))
        End If

        Return summary
    End Function

    <Extension()>
    Public Function GetSpeculativeSemanticInfoSummary(model As SemanticModel, position As Integer, expression As ExpressionSyntax, bindingOption As SpeculativeBindingOption) As SemanticInfoSummary
        Dim summary As New SemanticInfoSummary

        ' The information that is available varies by the type of the syntax node.

        Dim semanticModel = DirectCast(model, VBSemanticModel)
        Dim symbolInfo As SymbolInfo
        symbolInfo = semanticModel.GetSpeculativeSymbolInfo(position, expression, bindingOption)
        summary.MemberGroup = semanticModel.GetSpeculativeMemberGroup(position, expression)
        summary.ConstantValue = semanticModel.GetSpeculativeConstantValue(position, expression)
        Dim typeInfo = semanticModel.GetSpeculativeTypeInfo(position, expression, bindingOption)
        summary.Type = DirectCast(typeInfo.Type, TypeSymbol)
        summary.ConvertedType = DirectCast(typeInfo.ConvertedType, TypeSymbol)
        summary.ImplicitConversion = semanticModel.GetSpeculativeConversion(position, expression, bindingOption)

        Assert.NotNull(symbolInfo)
        summary.Symbol = DirectCast(symbolInfo.Symbol, Symbol)
        summary.CandidateReason = symbolInfo.CandidateReason
        summary.CandidateSymbols = symbolInfo.CandidateSymbols
        summary.AllSymbols = symbolInfo.GetAllSymbols()

        If TypeOf expression Is IdentifierNameSyntax Then
            summary.Alias = semanticModel.GetSpeculativeAliasInfo(position, DirectCast(expression, IdentifierNameSyntax), bindingOption)
        End If

        Return summary
    End Function

    Public Function GetSemanticInfoSummary(compilation As Compilation, node As SyntaxNode) As SemanticInfoSummary
        Dim tree = node.SyntaxTree
        Dim semanticModel = DirectCast(compilation.GetSemanticModel(tree), VBSemanticModel)
        Return GetSemanticInfoSummary(semanticModel, node)
    End Function

    Public Function GetSemanticInfoSummary(Of TSyntax As SyntaxNode)(compilation As Compilation, fileName As String, Optional which As Integer = 0) As SemanticInfoSummary
        Dim node As TSyntax = CompilationUtils.FindBindingText(Of TSyntax)(compilation, fileName, which)
        Return GetSemanticInfoSummary(compilation, node)
    End Function

    Public Function GetPreprocessingSymbolInfo(compilation As Compilation, fileName As String, Optional which As Integer = 0) As VisualBasicPreprocessingSymbolInfo
        Dim node = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, fileName, which)
        Dim semanticModel = DirectCast(compilation.GetSemanticModel(node.SyntaxTree), VBSemanticModel)
        Return semanticModel.GetPreprocessingSymbolInfo(node)
    End Function

    Public Function GetSemanticModel(compilation As Compilation, fileName As String) As VBSemanticModel
        Dim tree = (From t In compilation.SyntaxTrees Where t.FilePath = fileName).Single()
        Return DirectCast(compilation.GetSemanticModel(tree), VBSemanticModel)
    End Function

    ''' <summary>
    ''' Create a parse tree from the data inside an XElement
    ''' </summary>
    ''' <param name="programElement">The program element to create the tree from according to the following schema        
    ''' &lt;file name="filename.vb[optional]"&gt;
    ''' source
    ''' &lt;/file&gt;
    ''' </param>
    Public Function CreateParseTree(programElement As XElement) As SyntaxTree
        Return VisualBasicSyntaxTree.ParseText(SourceText.From(FilterString(programElement.Value), Encoding.UTF8, SourceHashAlgorithms.Default), path:=If(programElement.@name, ""))
    End Function

    ''' <summary>
    ''' Create a parse tree from the data inside an XElement
    ''' </summary>
    ''' <param name="programElement">The program element to create the tree from according to the following schema        
    ''' &lt;file name="filename.vb[optional]"&gt;
    ''' source
    ''' &lt;/file&gt;
    ''' </param>
    Public Function CreateParseTreeAndSpans(programElement As XElement, Optional parseOptions As VisualBasicParseOptions = Nothing) As (tree As SyntaxTree, spans As IList(Of TextSpan))
        Dim codeWithMarker As String = FilterString(programElement.Value)
        Dim codeWithoutMarker As String = Nothing
        Dim spans As ImmutableArray(Of TextSpan) = Nothing
        MarkupTestFile.GetSpans(codeWithMarker, codeWithoutMarker, spans)

        Dim text = SourceText.From(codeWithoutMarker, Encoding.UTF8)
        Return (VisualBasicSyntaxTree.ParseText(text, parseOptions, If(programElement.@name, "")), spans)
    End Function

    ' Find a node inside a tree.
    Public Function FindTokenFromText(tree As SyntaxTree, textToFind As String) As SyntaxToken
        Dim text As String = tree.GetText().ToString()
        Dim position As Integer = text.IndexOf(textToFind, StringComparison.Ordinal)
        Dim node = tree.GetRoot().FindToken(position)
        Return node
    End Function

    ' Find a position inside a tree.
    Public Function FindPositionFromText(tree As SyntaxTree, textToFind As String) As Integer
        Dim text As String = tree.GetText().ToString()
        Dim position As Integer = text.IndexOf(textToFind, StringComparison.Ordinal)
        Return position
    End Function

    ' Find a node inside a tree.
    Public Function FindNodeFromText(tree As SyntaxTree, textToFind As String) As SyntaxNode
        Return FindTokenFromText(tree, textToFind).Parent
    End Function

    ' Find a node of a type inside a tree.
    Public Function FindNodeOfTypeFromText(Of TNode As SyntaxNode)(tree As SyntaxTree, textToFind As String) As TNode
        Dim node = FindNodeFromText(tree, textToFind)
        While node IsNot Nothing AndAlso Not TypeOf node Is TNode
            node = node.Parent
        End While
        Return DirectCast(node, TNode)
    End Function

    ' Get the syntax tree with a given name.
    Public Function GetTree(compilation As Compilation, name As String) As SyntaxTree
        Return (From t In compilation.SyntaxTrees Where t.FilePath = name).First()
    End Function

    ' Get the symbol with a given full name. It must be unambiguous.
    Public Function GetSymbolByFullName(compilation As VisualBasicCompilation, methodName As String) As Symbol

        Dim names = New List(Of String)()
        Dim offset = 0
        While True
            ' Find the next "."c separator but skip the first character since
            ' the name may begin with "."c (in ".ctor" for instance).
            Dim separator = methodName.IndexOf("."c, offset + 1)
            If separator < 0 Then
                names.Add(methodName.Substring(offset))
                Exit While
            End If

            names.Add(methodName.Substring(offset, separator - offset))
            offset = separator + 1
        End While

        Dim currentSymbol As Symbol = compilation.GlobalNamespace

        For Each name In names
            Assert.True(TypeOf currentSymbol Is NamespaceOrTypeSymbol, String.Format("{0} does not have members", currentSymbol.ToDisplayString(SymbolDisplayFormat.TestFormat)))
            Dim currentContainer = DirectCast(currentSymbol, NamespaceOrTypeSymbol)
            Dim members = currentContainer.GetMembers(name)
            Assert.True(members.Any(), String.Format("No members named {0} inside {1}", name, currentSymbol.ToDisplayString(SymbolDisplayFormat.TestFormat)))
            Assert.True(members.Length() <= 1, String.Format("Multiple members named {0} inside {1}", name, currentSymbol.ToDisplayString(SymbolDisplayFormat.TestFormat)))
            currentSymbol = members.First()
        Next

        Return currentSymbol
    End Function

    ' Check that the compilation has no parse or declaration errors.
    Public Sub AssertNoDeclarationDiagnostics(compilation As VisualBasicCompilation, Optional suppressInfos As Boolean = True)
        AssertNoDiagnostics(compilation.GetDeclarationDiagnostics(), suppressInfos)
    End Sub

    ''' <remarks>
    ''' Does not consider INFO diagnostics.
    ''' </remarks>
    <Extension()>
    Public Sub AssertNoDiagnostics(compilation As VisualBasicCompilation, Optional suppressInfos As Boolean = True)
        AssertNoDiagnostics(compilation.GetDiagnostics(), suppressInfos)
    End Sub

    ' Check that the compilation has no parse, declaration errors/warnings, or compilation errors/warnings.
    ''' <remarks>
    ''' Does not consider INFO and HIDDEN diagnostics.
    ''' </remarks>
    Private Sub AssertNoDiagnostics(diags As ImmutableArray(Of Diagnostic), suppressInfos As Boolean)
        If suppressInfos Then
            diags = diags.WhereAsArray(Function(d) d.Severity > DiagnosticSeverity.Info)
        End If

        If diags.Length > 0 Then
            Console.WriteLine("Unexpected diagnostics found:")
            For Each d In diags
                Console.WriteLine(ErrorText(d))
            Next
            Assert.True(False, "Should not have any diagnostics")
        End If
    End Sub

    ' Check that the compilation has no parse, declaration errors, or compilation errors.
    <Extension()>
    Public Sub AssertNoErrors(compilation As Compilation)
        AssertNoErrors(compilation.GetDiagnostics())
    End Sub

    ' Check that the compilation has no parse, declaration errors, or compilation errors.
    <Extension()>
    Public Sub AssertNoErrors(errors As ImmutableArray(Of Diagnostic))
        Dim diags As ImmutableArray(Of Diagnostic) = errors.WhereAsArray(Function(e) e.Severity = DiagnosticSeverity.Error)

        If diags.Length > 0 Then
            Console.WriteLine("Unexpected errors found:")
            For Each d In diags
                Console.WriteLine(ErrorText(d))
            Next
            Assert.True(False, "Should not have any errors")
        End If
    End Sub

    ''' <summary>
    ''' Check that a compilation has these declaration errors.
    ''' </summary>
    ''' <param name="compilation"></param>
    ''' <param name="errs">Expected errors according to this schema
    ''' &lt;error&gt;[full errors text]&lt;/error&gt;</param>
    ''' <param name="suppressInfos">True to ignore info-severity diagnostics.</param>
    ''' <remarks></remarks>
    <Extension()>
    Public Sub AssertTheseDeclarationDiagnostics(compilation As VisualBasicCompilation, errs As XElement, Optional suppressInfos As Boolean = True)
        AssertTheseDiagnostics(compilation.GetDeclarationDiagnostics(), errs, suppressInfos)
    End Sub

    <Extension()>
    Public Sub AssertTheseParseDiagnostics(compilation As VisualBasicCompilation, errs As XElement, Optional suppressInfos As Boolean = True)
        AssertTheseDiagnostics(compilation.GetParseDiagnostics(), errs, suppressInfos)
    End Sub

    ''' <summary>
    ''' Check that a compilation has these errors at Compile stage or before.
    ''' </summary>
    ''' <param name="compilation"></param>
    ''' <param name="errs">Expected errors according to this schema
    ''' &lt;error&gt;[full errors text]&lt;/error&gt;</param>
    ''' <param name="suppressInfos">True to ignore info-severity diagnostics.</param>
    ''' <remarks></remarks>
    <Extension()>
    Public Sub AssertTheseCompileDiagnostics(compilation As Compilation, Optional errs As XElement = Nothing, Optional suppressInfos As Boolean = True)
        If errs Is Nothing Then
            errs = <errors/>
        End If
        AssertTheseDiagnostics(DirectCast(compilation, VisualBasicCompilation).GetDiagnostics(CompilationStage.Compile), errs, suppressInfos)
    End Sub

    ''' <summary>
    ''' Check that a compilation has these errors during Emit.
    ''' </summary>
    ''' <param name="compilation"></param>
    ''' <param name="errs">Expected errors according to this schema
    ''' &lt;error&gt;[full errors text]&lt;/error&gt;</param>
    ''' <param name="suppressInfos">True to ignore info-severity diagnostics.</param>
    ''' <remarks></remarks>
    <Extension()>
    Public Sub AssertTheseEmitDiagnostics(compilation As Compilation, Optional errs As XElement = Nothing, Optional suppressInfos As Boolean = True)
        If errs Is Nothing Then
            errs = <errors/>
        End If
        Using assemblyStream As New MemoryStream()
            Using pdbStream As New MemoryStream()
                Dim diagnostics = compilation.Emit(assemblyStream, pdbStream:=pdbStream).Diagnostics
                AssertTheseDiagnostics(diagnostics, errs, suppressInfos)
            End Using
        End Using
    End Sub

    <Extension()>
    Public Sub AssertTheseDiagnostics(tree As SyntaxTree, errs As XElement, Optional suppressInfos As Boolean = True)
        AssertTheseDiagnostics(tree.GetDiagnostics().AsImmutable(), errs, suppressInfos)
    End Sub

    ''' <summary>
    ''' Check that a compilation has these declaration or compilation errors.
    ''' </summary>
    ''' <param name="compilation"></param>
    ''' <param name="errs">Expected errors according to this schema
    ''' &lt;error&gt;[full errors text]&lt;/error&gt;</param>
    ''' <param name="suppressInfos">True to ignore info-severity diagnostics.</param>
    ''' <remarks></remarks>
    <Extension()>
    Public Sub AssertTheseDiagnostics(compilation As Compilation, Optional errs As XElement = Nothing, Optional suppressInfos As Boolean = True)
        If errs Is Nothing Then
            errs = <errors/>
        End If
        AssertTheseDiagnostics(DirectCast(compilation, VisualBasicCompilation).GetDiagnostics(CompilationStage.Compile), errs, suppressInfos)
    End Sub

    <Extension()>
    Public Sub AssertTheseDiagnostics(compilation As Compilation, errs As XCData, Optional suppressInfos As Boolean = True)
        AssertTheseDiagnostics(DirectCast(compilation, VisualBasicCompilation).GetDiagnostics(CompilationStage.Compile), errs, suppressInfos)
    End Sub

    ' Check that a compilation has these declaration or compilation errors.
    <Extension()>
    Public Sub AssertTheseDiagnostics(compilation As Compilation, errs As String, Optional suppressInfos As Boolean = True)
        AssertTheseDiagnostics(DirectCast(compilation, VisualBasicCompilation).GetDiagnostics(CompilationStage.Compile), errs, suppressInfos)
    End Sub

    ''' <param name="errors"></param>
    ''' <param name="errs">Expected errors according to this schema
    ''' &lt;expected&gt;[full errors text]&lt;/expected&gt;</param>
    ''' <param name="suppressInfos">True to ignore info-severity diagnostics.</param>
    ''' <remarks></remarks>
    <Extension()>
    Public Sub AssertTheseDiagnostics(errors As ImmutableArray(Of Diagnostic), errs As XElement, Optional suppressInfos As Boolean = True)
        If errs Is Nothing Then
            errs = <errors/>
        End If
        Dim expectedText = FilterString(errs.Value)
        AssertTheseDiagnostics(errors, expectedText, suppressInfos)
    End Sub

    <Extension()>
    Public Sub AssertTheseDiagnostics(errors As ImmutableArray(Of Diagnostic), errs As XCData, Optional suppressInfos As Boolean = True)
        Dim expectedText = FilterString(errs.Value)
        AssertTheseDiagnostics(errors, expectedText, suppressInfos)
    End Sub

    Private Sub AssertTheseDiagnostics(errors As ImmutableArray(Of Diagnostic), expectedText As String, suppressInfos As Boolean)
        Dim actualText = DumpAllDiagnostics(errors.ToArray(), suppressInfos)
        If expectedText <> actualText Then
            Dim messages = ParserTestUtilities.PooledStringBuilderPool.Allocate()
            With messages.Builder
                .AppendLine()

                If actualText.StartsWith(expectedText, StringComparison.Ordinal) AndAlso actualText.Substring(expectedText.Length).Trim().Length > 0 Then
                    .AppendLine("UNEXPECTED ERROR MESSAGES:")
                    .AppendLine(actualText.Substring(expectedText.Length))

                    Assert.True(False, .ToString())
                Else
                    Dim expectedLines = expectedText.Split({vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
                    Dim actualLines = actualText.Split({vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)

                    Dim appendedLines As Integer = 0

                    .AppendLine("MISSING ERROR MESSAGES:")
                    For Each l In expectedLines
                        If Not actualLines.Contains(l) Then
                            .AppendLine(l)
                            appendedLines += 1
                        End If
                    Next

                    .AppendLine("UNEXPECTED ERROR MESSAGES:")
                    For Each l In actualLines
                        If Not expectedLines.Contains(l) Then
                            .AppendLine(l)
                            appendedLines += 1
                        End If
                    Next

                    If appendedLines > 0 Then
                        Assert.True(False, .ToString())
                    Else
                        CompareLineByLine(expectedText, actualText)
                    End If
                End If
            End With
            messages.Free()
        End If
    End Sub

    Private Sub CompareLineByLine(expected As String, actual As String)
        Dim expectedReader = New StringReader(expected)
        Dim actualReader = New StringReader(actual)

        Dim expectedPooledBuilder = PooledStringBuilderPool.Allocate()
        Dim actualPooledBuilder = PooledStringBuilderPool.Allocate()

        Dim expectedBuilder = expectedPooledBuilder.Builder
        Dim actualBuilder = actualPooledBuilder.Builder

        Dim expectedLine = expectedReader.ReadLine()
        Dim actualLine = actualReader.ReadLine()

        While expectedLine IsNot Nothing AndAlso actualLine IsNot Nothing
            If Not expectedLine.Equals(actualLine) Then
                expectedBuilder.AppendLine("<! " & expectedLine)
                actualBuilder.AppendLine("!> " & actualLine)
            Else
                expectedBuilder.AppendLine(expectedLine)
                actualBuilder.AppendLine(actualLine)
            End If

            expectedLine = expectedReader.ReadLine()
            actualLine = actualReader.ReadLine()

        End While

        While expectedLine IsNot Nothing
            expectedBuilder.AppendLine("<! " & expectedLine)
            expectedLine = expectedReader.ReadLine()
        End While

        While actualLine IsNot Nothing
            actualBuilder.AppendLine("!> " & actualLine)
            actualLine = actualReader.ReadLine()
        End While

        AssertEx.Equal(expectedPooledBuilder.ToStringAndFree(), actualPooledBuilder.ToStringAndFree())
    End Sub

    ' There are certain cases where multiple distinct errors are
    ' reported where the error code and text span are the same. When
    ' sorting such cases, we should preserve the original order.
    Private Structure DiagnosticAndIndex
        Public Sub New(diagnostic As Diagnostic, index As Integer)
            Me.Diagnostic = diagnostic
            Me.Index = index
        End Sub
        Public ReadOnly Diagnostic As Diagnostic
        Public ReadOnly Index As Integer
    End Structure

    Private Function DumpAllDiagnostics(allDiagnostics As Diagnostic(), suppressInfos As Boolean) As String
        Return DumpAllDiagnostics(allDiagnostics.ToImmutableArray(), suppressInfos)
    End Function

    Friend Function DumpAllDiagnostics(allDiagnostics As ImmutableArray(Of Diagnostic), suppressInfos As Boolean) As String

        Dim diagnosticsAndIndices(allDiagnostics.Length - 1) As DiagnosticAndIndex
        For i = 0 To allDiagnostics.Length - 1
            diagnosticsAndIndices(i) = New DiagnosticAndIndex(allDiagnostics(i), i)
        Next

        Array.Sort(diagnosticsAndIndices, Function(diag1, diag2) CompareErrors(diag1, diag2))

        Dim builder = PooledStringBuilderPool.Allocate()
        With builder.Builder
            For Each e In diagnosticsAndIndices
                If Not suppressInfos OrElse e.Diagnostic.Severity > DiagnosticSeverity.Info Then
                    .Append(ErrorText(e.Diagnostic))
                End If
            Next
        End With
        Return builder.ToStringAndFree()
    End Function

    ' Get the text of a diagnostic. For source error, includes the text of the line itself, with the 
    ' span underlined.
    Private Function ErrorText(e As Diagnostic) As String
        Dim message = e.Id + ": " + e.GetMessage(EnsureEnglishUICulture.PreferredOrNull)
        If e.Location.IsInSource Then
            Dim sourceLocation = e.Location
            Dim offsetInLine As Integer = 0
            Dim lineText As String = GetLineText(sourceLocation.SourceTree.GetText(), sourceLocation.SourceSpan.Start, offsetInLine)
            Return message + Environment.NewLine +
                lineText + Environment.NewLine +
                New String(" "c, offsetInLine) +
                New String("~"c, Math.Max(Math.Min(sourceLocation.SourceSpan.Length, lineText.Length - offsetInLine + 1), 1)) + Environment.NewLine
        ElseIf e.Location.IsInMetadata Then
            Return message + Environment.NewLine +
                String.Format("in metadata assembly '{0}'" + Environment.NewLine,
                              e.Location.MetadataModule.ContainingAssembly.Identity.Name)
        Else
            Return message + Environment.NewLine
        End If
    End Function

    ' Get the text of a line that contains the offset, and return the offset within that line.
    Private Function GetLineText(text As SourceText, position As Integer, ByRef offsetInLine As Integer) As String
        Dim textLine = text.Lines.GetLineFromPosition(position)
        offsetInLine = position - textLine.Start
        Return textLine.ToString()
    End Function

    Private Function CompareErrors(diagAndIndex1 As DiagnosticAndIndex, diagAndIndex2 As DiagnosticAndIndex) As Integer
        ' Sort by no location, then source, then metadata. Sort within each group.

        Dim diag1 = diagAndIndex1.Diagnostic
        Dim diag2 = diagAndIndex2.Diagnostic
        Dim loc1 = diag1.Location
        Dim loc2 = diag2.Location
        Dim comparer = StringComparer.Ordinal

        If Not (loc1.IsInSource Or loc1.IsInMetadata) Then
            If Not (loc2.IsInSource Or loc2.IsInMetadata) Then
                ' Both have no location. Sort by code, then by message.
                If diag1.Code < diag2.Code Then Return -1
                If diag1.Code > diag2.Code Then Return 1
                Return comparer.Compare(diag1.GetMessage(EnsureEnglishUICulture.PreferredOrNull), diag2.GetMessage(EnsureEnglishUICulture.PreferredOrNull))
            Else
                Return -1
            End If
        ElseIf Not (loc2.IsInSource Or loc2.IsInMetadata) Then
            Return 1
        ElseIf loc1.IsInSource AndAlso loc2.IsInSource Then
            ' source by tree, then span start, then span end, then error code, then message
            Dim sourceTree1 = loc1.SourceTree
            Dim sourceTree2 = loc2.SourceTree

            If sourceTree1.FilePath <> sourceTree2.FilePath Then Return comparer.Compare(sourceTree1.FilePath, sourceTree2.FilePath)
            If loc1.SourceSpan.Start < loc2.SourceSpan.Start Then Return -1
            If loc1.SourceSpan.Start > loc2.SourceSpan.Start Then Return 1
            If loc1.SourceSpan.Length < loc2.SourceSpan.Length Then Return -1
            If loc1.SourceSpan.Length > loc2.SourceSpan.Length Then Return 1
            If diag1.Code < diag2.Code Then Return -1
            If diag1.Code > diag2.Code Then Return 1

            Return comparer.Compare(diag1.GetMessage(EnsureEnglishUICulture.PreferredOrNull), diag2.GetMessage(EnsureEnglishUICulture.PreferredOrNull))
        ElseIf loc1.IsInMetadata AndAlso loc2.IsInMetadata Then
            ' sort by assembly name, then by error code
            Dim name1 = loc1.MetadataModule.ContainingAssembly.Name
            Dim name2 = loc2.MetadataModule.ContainingAssembly.Name
            If name1 <> name2 Then Return comparer.Compare(name1, name2)
            If diag1.Code < diag2.Code Then Return -1
            If diag1.Code > diag2.Code Then Return 1

            Return comparer.Compare(diag1.GetMessage(EnsureEnglishUICulture.PreferredOrNull), diag2.GetMessage(EnsureEnglishUICulture.PreferredOrNull))
        ElseIf loc1.IsInSource Then
            Return -1
        ElseIf loc2.IsInSource Then
            Return 1
        End If

        ' Preserve original order.
        Return diagAndIndex1.Index - diagAndIndex2.Index
    End Function

    Public Function GetTypeSymbol(compilation As Compilation,
                                  semanticModel As SemanticModel,
                                  treeName As String,
                                  stringInDecl As String) As INamedTypeSymbol
        Dim tree = CompilationUtils.GetTree(compilation, treeName)
        Dim node = CompilationUtils.FindTokenFromText(tree, stringInDecl).Parent
        While Not (TypeOf node Is TypeStatementSyntax)
            node = node.Parent
            Assert.NotNull(node)
        End While

        Return DirectCast(semanticModel, VBSemanticModel).GetDeclaredSymbol(DirectCast(node, TypeStatementSyntax))
    End Function

    Public Function GetEnumSymbol(compilation As Compilation,
                                  semanticModel As SemanticModel,
                                  treeName As String,
                                  stringInDecl As String) As INamedTypeSymbol
        Dim tree = CompilationUtils.GetTree(compilation, treeName)
        Dim node = CompilationUtils.FindTokenFromText(tree, stringInDecl).Parent
        While Not (TypeOf node Is EnumStatementSyntax)
            node = node.Parent
            Assert.NotNull(node)
        End While

        Return DirectCast(semanticModel, VBSemanticModel).GetDeclaredSymbol(DirectCast(node, EnumStatementSyntax))
    End Function

    Public Function GetDelegateSymbol(compilation As Compilation,
                                  semanticModel As SemanticModel,
                                  treeName As String,
                                  stringInDecl As String) As NamedTypeSymbol
        Dim tree = CompilationUtils.GetTree(compilation, treeName)
        Dim node = CompilationUtils.FindTokenFromText(tree, stringInDecl).Parent
        While Not (TypeOf node Is MethodBaseSyntax)
            node = node.Parent
            Assert.NotNull(node)
        End While

        Return DirectCast(semanticModel.GetDeclaredSymbol(DirectCast(node, MethodBaseSyntax)), NamedTypeSymbol)
    End Function

    Public Function GetTypeSymbol(compilation As Compilation,
                                  treeName As String,
                                  stringInDecl As String,
                                  Optional isDistinct As Boolean = True) As List(Of INamedTypeSymbol)
        Dim tree = CompilationUtils.GetTree(compilation, treeName)
        Dim bindings = DirectCast(compilation.GetSemanticModel(tree), VBSemanticModel)
        Dim text As String = tree.GetText().ToString()
        Dim pos = 0
        Dim node As SyntaxNode
        Dim symType = New List(Of INamedTypeSymbol)

        Do
            pos = text.IndexOf(stringInDecl, pos + 1, StringComparison.Ordinal)
            If pos >= 0 Then
                node = tree.GetRoot().FindToken(pos).Parent
                While Not (TypeOf node Is TypeStatementSyntax OrElse TypeOf node Is EnumStatementSyntax)
                    If node Is Nothing Then
                        Exit While
                    End If
                    node = node.Parent
                End While
                If Not node Is Nothing Then
                    If TypeOf node Is TypeStatementSyntax Then
                        Dim temp = bindings.GetDeclaredSymbol(DirectCast(node, TypeStatementSyntax))
                        symType.Add(temp)
                    Else
                        Dim temp = bindings.GetDeclaredSymbol(DirectCast(node, EnumStatementSyntax))
                        symType.Add(temp)
                    End If
                End If
            Else
                Exit Do
            End If
        Loop

        If (isDistinct) Then
            symType = symType.Distinct().OrderBy(Function(x) x.ToDisplayString(), StringComparer.OrdinalIgnoreCase).ToList()
        Else
            symType = symType.OrderBy(Function(x) x.ToDisplayString(), StringComparer.OrdinalIgnoreCase).ToList()
        End If
        Return symType

    End Function

    Public Function VerifyGlobalNamespace(compilation As Compilation,
                                 treeName As String,
                                 symbolName As String,
                                  ExpectedDispName() As String,
                                  isDistinct As Boolean) As List(Of INamedTypeSymbol)
        Dim tree = CompilationUtils.GetTree(compilation, treeName)
        Dim bindings1 = compilation.GetSemanticModel(tree)
        Dim symbols = GetTypeSymbol(compilation, treeName, symbolName, isDistinct)
        Assert.Equal(ExpectedDispName.Count, symbols.Count)
        ExpectedDispName = ExpectedDispName.OrderBy(StringComparer.OrdinalIgnoreCase).ToArray()
        Dim count = 0
        For Each item In symbols
            Assert.NotNull(item)
            Assert.Equal(ExpectedDispName(count), item.ToDisplayString())
            count += 1
        Next
        Return symbols
    End Function

    Public Function VerifyGlobalNamespace(compilation As VisualBasicCompilation,
                                 treeName As String,
                                 symbolName As String,
                                  ParamArray expectedDisplayNames() As String) As List(Of INamedTypeSymbol)
        Return VerifyGlobalNamespace(compilation, treeName, symbolName, expectedDisplayNames, True)
    End Function

    Public Function VerifyIsGlobal(globalNS1 As ISymbol, Optional expected As Boolean = True) As NamespaceSymbol
        Dim nsSymbol = DirectCast(globalNS1, NamespaceSymbol)
        Assert.NotNull(nsSymbol)
        If (expected) Then
            Assert.True(nsSymbol.IsGlobalNamespace)
        Else
            Assert.False(nsSymbol.IsGlobalNamespace)
        End If
        Return nsSymbol
    End Function

    Public Sub CheckSymbols(Of TSymbol As ISymbol)(symbols As ImmutableArray(Of TSymbol), ParamArray descriptions As String())
        Assert.Equal(symbols.Length, descriptions.Length)

        Dim symbolDescriptions As String() = (From s In symbols Select s.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)).ToArray()
        Array.Sort(descriptions)
        Array.Sort(symbolDescriptions)

        For i = 0 To descriptions.Length - 1
            Assert.Equal(symbolDescriptions(i), descriptions(i))
        Next
    End Sub

    Public Sub CheckSymbols(Of TSymbol As ISymbol)(symbols As TSymbol(), ParamArray descriptions As String())
        CheckSymbols(symbols.AsImmutableOrNull(), descriptions)
    End Sub

    Public Sub CheckSymbol(symbol As ISymbol, description As String)
        Assert.Equal(symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat), description)
    End Sub

    Public Function SortAndMergeStrings(ParamArray strings As String()) As String
        Array.Sort(strings)
        Dim builder = PooledStringBuilderPool.Allocate()
        With builder.Builder
            For Each item In strings
                If .Length > 0 Then
                    .AppendLine()
                End If
                .Append(item)
            Next
        End With
        Return builder.ToStringAndFree()
    End Function

    Public Sub CheckSymbolsUnordered(Of TSymbol As ISymbol)(symbols As ImmutableArray(Of TSymbol), ParamArray descriptions As String())
        Assert.Equal(symbols.Length, descriptions.Length)
        Assert.Equal(SortAndMergeStrings(descriptions),
                     SortAndMergeStrings(symbols.Select(Function(s) s.ToDisplayString()).ToArray()))
    End Sub

    <Extension>
    Friend Function LookupNames(model As SemanticModel,
                                position As Integer,
                                Optional container As INamespaceOrTypeSymbol = Nothing,
                                Optional namespacesAndTypesOnly As Boolean = False) As List(Of String)

        Dim result = If(namespacesAndTypesOnly, model.LookupNamespacesAndTypes(position, container), model.LookupSymbols(position, container))
        Return result.Select(Function(s) s.Name).Distinct().ToList()
    End Function
End Module
