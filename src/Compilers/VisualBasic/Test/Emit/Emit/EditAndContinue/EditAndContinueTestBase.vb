' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.IO
Imports System.Reflection.Metadata
Imports System.Reflection.Metadata.Ecma335
Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.EditAndContinue.UnitTests
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.Metadata.Tools
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class EditAndContinueTestBase
        Inherits BasicTestBase

        Public Shared ReadOnly MetadataUpdateDeletedAttributeSource As String = "
            Namespace System.Runtime.CompilerServices
                <AttributeUsage(AttributeTargets.All, AllowMultiple:=False, Inherited:=False)>
                Public Class MetadataUpdateDeletedAttribute
                    Inherits Attribute
                End Class
            End Namespace
        "

        ' PDB reader can only be accessed from a single thread, so avoid concurrent compilation:
        Friend Shared ReadOnly ComSafeDebugDll As VisualBasicCompilationOptions = TestOptions.DebugDll.WithConcurrentBuild(False)

        Protected Shared ReadOnly ValueTupleRefs As MetadataReference() = {SystemRuntimeFacadeRef, ValueTupleRef}

        Friend Shared ReadOnly EmptyLocalsProvider As Func(Of MethodDefinitionHandle, EditAndContinueMethodDebugInformation) = Function(token) Nothing

        Friend Shared Function Visualize(baseline As ModuleMetadata, ParamArray deltas As PinnedMetadata()) As String
            Dim result = New StringWriter()
            Dim visualizer = New MetadataVisualizer({baseline.MetadataReader}.Concat(deltas.Select(Function(d) d.Reader)).ToArray(), result)
            visualizer.VisualizeAllGenerations()
            Return result.ToString()
        End Function

        Public Shared Function CreateInitialBaseline(compilation As Compilation, [module] As ModuleMetadata, debugInformationProvider As Func(Of MethodDefinitionHandle, EditAndContinueMethodDebugInformation)) As EmitBaseline
            Return EditAndContinueTestUtilities.CreateInitialBaseline(compilation, [module], debugInformationProvider)
        End Function

        Friend Shared Function MarkedSource(source As XElement, Optional fileName As String = "", Optional options As VisualBasicParseOptions = Nothing) As SourceWithMarkedNodes
            Return New SourceWithMarkedNodes(WithWindowsLineBreaks(source.Value), Function(s) Parse(s, fileName, options), Function(s) CInt(GetType(SyntaxKind).GetField(s).GetValue(Nothing)))
        End Function

        Friend Shared Function MarkedSource(source As String, Optional fileName As String = "", Optional options As VisualBasicParseOptions = Nothing) As SourceWithMarkedNodes
            Return New SourceWithMarkedNodes(WithWindowsLineBreaks(source), Function(s) Parse(s, fileName, options), Function(s) CInt(GetType(SyntaxKind).GetField(s).GetValue(Nothing)))
        End Function

        Friend Shared Function GetSyntaxMapFromMarkers(source0 As SourceWithMarkedNodes, source1 As SourceWithMarkedNodes) As Func(Of SyntaxNode, SyntaxNode)
            Return SourceWithMarkedNodes.GetSyntaxMap(source0, source1)
        End Function

        Friend Shared Function Edit(
            kind As SemanticEditKind,
            symbolProvider As Func(Of Compilation, ISymbol),
            Optional newSymbolProvider As Func(Of Compilation, ISymbol) = Nothing,
            Optional rudeEdits As Func(Of SyntaxNode, RuntimeRudeEdit?) = Nothing,
            Optional preserveLocalVariables As Boolean = False) As SemanticEditDescription
            Return New SemanticEditDescription(kind, symbolProvider, newSymbolProvider, rudeEdits, preserveLocalVariables)
        End Function

        Friend Function ToLocalInfo(local As Cci.ILocalDefinition) As ILVisualizer.LocalInfo
            Dim signature = local.Signature
            If signature Is Nothing Then
                Return New ILVisualizer.LocalInfo(local.Name, local.Type, local.IsPinned, local.IsReference)
            Else
                ' Decode simple types only.
                Dim typeName = If(signature.Length = 1, GetTypeName(CType(signature(0), SignatureTypeCode)), Nothing)
                Return New ILVisualizer.LocalInfo(Nothing, If(typeName, "[unchanged]"), False, False)
            End If
        End Function

        Private Function GetTypeName(typeCode As SignatureTypeCode) As String
            Select Case typeCode
                Case SignatureTypeCode.Boolean
                    Return "Boolean"
                Case SignatureTypeCode.Int32
                    Return "Integer"
                Case SignatureTypeCode.String
                    Return "String"
                Case SignatureTypeCode.Object
                    Return "Object"
                Case Else
                    Return Nothing
            End Select
        End Function

        Friend Shared Function GetAllLocals(compilation As VisualBasicCompilation, method As MethodSymbol) As ImmutableArray(Of LocalSymbol)
            Dim methodSyntax = method.DeclaringSyntaxReferences(0).GetSyntax().Parent
            Dim model = compilation.GetSemanticModel(methodSyntax.SyntaxTree)
            Dim locals = ArrayBuilder(Of LocalSymbol).GetInstance()

            For Each node In methodSyntax.DescendantNodes()
                If node.Kind = SyntaxKind.VariableDeclarator Then
                    For Each name In DirectCast(node, VariableDeclaratorSyntax).Names
                        Dim local = DirectCast(model.GetDeclaredSymbol(name), LocalSymbol)
                        locals.Add(local)
                    Next
                End If
            Next

            Return locals.ToImmutableAndFree()
        End Function

        Friend Shared Function GetAllLocals(compilation As VisualBasicCompilation, method As IMethodSymbol) As ImmutableArray(Of KeyValuePair(Of ILocalSymbol, Integer))
            Dim locals = GetAllLocals(compilation, DirectCast(method, MethodSymbol))
            Return locals.SelectAsArray(Function(local, index, arg) New KeyValuePair(Of ILocalSymbol, Integer)(local, index), DirectCast(Nothing, Object))
        End Function

        Friend Shared Function GetAllLocals(method As SourceMethodSymbol) As ImmutableArray(Of VisualBasicSyntaxNode)
            Dim names = From name In LocalVariableDeclaratorsCollector.GetDeclarators(method).OfType(Of ModifiedIdentifierSyntax)
                        Select DirectCast(name, VisualBasicSyntaxNode)

            Return names.AsImmutableOrEmpty
        End Function

        Friend Shared Function GetLocalName(node As SyntaxNode) As String
            If node.Kind = SyntaxKind.ModifiedIdentifier Then
                Return DirectCast(node, ModifiedIdentifierSyntax).Identifier.ToString()
            End If

            Throw New NotImplementedException()
        End Function

        Friend Shared Function GetSyntaxMapByKind(method As MethodSymbol, ParamArray kinds As SyntaxKind()) As Func(Of SyntaxNode, SyntaxNode)
            Return Function(node As SyntaxNode)
                       For Each k In kinds
                           If node.IsKind(k) Then
                               Return method.DeclaringSyntaxReferences.Single().SyntaxTree.GetRoot().DescendantNodes().Single(Function(n) n.IsKind(k))
                           End If
                       Next

                       Return Nothing
                   End Function
        End Function

        Friend Shared Function GetEquivalentNodesMap(method1 As MethodSymbol, method0 As MethodSymbol) As Func(Of SyntaxNode, SyntaxNode)
            Dim tree1 = method1.Locations(0).SourceTree
            Dim tree0 = method0.Locations(0).SourceTree
            Assert.NotEqual(tree1, tree0)

            Dim sourceMethod0 = DirectCast(method0, SourceMethodSymbol)

            Dim locals0 = GetAllLocals(sourceMethod0)
            Return Function(s As SyntaxNode)
                       Dim s1 = s
                       Assert.Equal(s1.SyntaxTree, tree1)

                       ' add mapping for result variable (it's declarator is the Function Statement)
                       If s.IsKind(SyntaxKind.FunctionStatement) Then
                           Assert.True(sourceMethod0.BlockSyntax.BlockStatement.IsKind(SyntaxKind.FunctionStatement))
                           Return sourceMethod0.BlockSyntax.BlockStatement
                       ElseIf s.IsKind(SyntaxKind.PropertyStatement) Then
                           Assert.True(sourceMethod0.BlockSyntax.IsKind(SyntaxKind.GetAccessorBlock))
                           Return DirectCast(sourceMethod0.BlockSyntax.Parent, PropertyBlockSyntax).PropertyStatement
                       ElseIf s.IsKind(SyntaxKind.EventStatement) Then
                           Assert.True(sourceMethod0.BlockSyntax.IsKind(SyntaxKind.AddHandlerAccessorBlock))
                           Return DirectCast(sourceMethod0.BlockSyntax.Parent, PropertyBlockSyntax).PropertyStatement
                       End If

                       For Each s0 In locals0
                           If Not SyntaxFactory.AreEquivalent(s0, s1) Then
                               Continue For
                           End If
                           ' Make sure the containing statements are the same.
                           Dim p0 = GetNearestStatement(s0)
                           Dim p1 = GetNearestStatement(s1)
                           If SyntaxFactory.AreEquivalent(p0, p1) Then
                               Return s0
                           End If
                       Next
                       Return Nothing
                   End Function
        End Function

        Friend Shared Function GetNearestStatement(node As SyntaxNode) As StatementSyntax
            While node IsNot Nothing
                Dim statement = TryCast(node, StatementSyntax)
                If statement IsNot Nothing Then
                    Return statement
                End If

                node = node.Parent
            End While
            Return Nothing
        End Function

        Friend Shared Function Row(rowNumber As Integer, table As TableIndex, operation As EditAndContinueOperation) As EditAndContinueLogEntry
            Return New EditAndContinueLogEntry(MetadataTokens.Handle(table, rowNumber), operation)
        End Function

        Friend Shared Function Handle(rowNumber As Integer, table As TableIndex) As EntityHandle
            Return MetadataTokens.Handle(table, rowNumber)
        End Function

        Friend Shared Function IsDefinition(kind As HandleKind) As Boolean
            Select Case kind
                Case HandleKind.AssemblyReference, HandleKind.ModuleReference, HandleKind.TypeReference, HandleKind.MemberReference, HandleKind.TypeSpecification, HandleKind.MethodSpecification
                    Return False
                Case Else
                    Return True
            End Select
        End Function

        ''' <summary>
        ''' Checks that the EncLog contains specified rows.
        ''' Any default values in the expected <paramref name="rows"/> are ignored to facilitate conditional code.
        ''' </summary>
        Friend Shared Sub CheckEncLog(reader As MetadataReader, ParamArray rows As EditAndContinueLogEntry())
            AssertEx.Equal(
                rows.Where(Function(r) r.Handle <> Nothing),
                reader.GetEditAndContinueLogEntries(), itemInspector:=AddressOf EditAndContinueTestUtilities.EncLogRowToString)
        End Sub

        ''' <summary>
        ''' Checks that the EncLog contains specified definition rows. References are ignored as they are usually not interesting to validate. They are emitted as needed.
        ''' Any default values in the expected <paramref name="rows"/> are ignored to facilitate conditional code.
        ''' </summary>
        Friend Shared Sub CheckEncLogDefinitions(reader As MetadataReader, ParamArray rows As EditAndContinueLogEntry())
            AssertEx.Equal(
                rows.Where(Function(r) r.Handle <> Nothing),
                reader.GetEditAndContinueLogEntries().Where(Function(entry) IsDefinition(entry.Handle.Kind)), itemInspector:=AddressOf EditAndContinueTestUtilities.EncLogRowToString)
        End Sub

        ''' <summary>
        ''' Checks that the EncMap contains specified handles.
        ''' Any default values in the expected <paramref name="handles"/> are ignored to facilitate conditional code.
        ''' </summary>
        Friend Shared Sub CheckEncMap(reader As MetadataReader, ParamArray [handles] As EntityHandle())
            AssertEx.Equal(
                [handles].Where(Function(h) h <> Nothing),
                reader.GetEditAndContinueMapEntries(), itemInspector:=AddressOf EditAndContinueTestUtilities.EncMapRowToString)
        End Sub

        ''' <summary>
        ''' Checks that the EncMap contains specified definition handles. References are ignored as they are usually Not interesting to validate. They are emitted as needed.
        ''' Any default values in the expected <paramref name="handles"/> are ignored to facilitate conditional code.
        ''' </summary>
        Friend Shared Sub CheckEncMapDefinitions(reader As MetadataReader, ParamArray [handles] As EntityHandle())
            AssertEx.Equal(
                [handles].Where(Function(h) h <> Nothing),
                reader.GetEditAndContinueMapEntries().Where(Function(e) IsDefinition(e.Kind)), itemInspector:=AddressOf EditAndContinueTestUtilities.EncMapRowToString)
        End Sub

        Friend Shared Sub CheckNames(reader As MetadataReader, [handles] As IEnumerable(Of StringHandle), ParamArray expectedNames As String())
            CheckNames({reader}, [handles], expectedNames)
        End Sub

        Friend Shared Sub CheckNames(readers As MetadataReader(), [handles] As IEnumerable(Of StringHandle), ParamArray expectedNames As String())
            Dim actualNames = readers.GetStrings([handles])
            AssertEx.Equal(expectedNames, actualNames)
        End Sub

        Public Shared Sub CheckNames(readers As IList(Of MetadataReader), typeHandles As IEnumerable(Of TypeDefinitionHandle), ParamArray expectedNames As String())
            CheckNames(readers, typeHandles, Function(reader, handle) reader.GetTypeDefinition(CType(handle, TypeDefinitionHandle)).Name, Function(handle) handle, expectedNames)
        End Sub

        Friend Shared Sub CheckNamesSorted(readers As MetadataReader(), [handles] As IEnumerable(Of StringHandle), ParamArray expectedNames As String())
            Dim actualNames = readers.GetStrings([handles])
            Array.Sort(actualNames)
            Array.Sort(expectedNames)
            AssertEx.Equal(expectedNames, actualNames)
        End Sub

        Private Shared Sub CheckNames(Of THandle)(
            readers As IList(Of MetadataReader),
            entityHandles As IEnumerable(Of THandle),
            getName As Func(Of MetadataReader, Handle, StringHandle),
            toHandle As Func(Of THandle, Handle),
            expectedNames As String())
            Dim aggregator = GetAggregator(readers)

            AssertEx.Equal(expectedNames, entityHandles.Select(
                Function(handle)
                    Dim typeGeneration As Integer
                    Dim genEntityHandle = aggregator.GetGenerationHandle(toHandle(handle), typeGeneration)
                    Dim nameHandle = getName(readers(typeGeneration), genEntityHandle)

                    Dim nameGeneration As Integer
                    Dim genNameHandle = CType(aggregator.GetGenerationHandle(nameHandle, nameGeneration), StringHandle)
                    Return readers(nameGeneration).GetString(genNameHandle)
                End Function))
        End Sub

        Public Shared Sub CheckBlobValue(readers As IList(Of MetadataReader), valueHandle As BlobHandle, expectedValue As Byte())
            Dim aggregator = GetAggregator(readers)

            Dim generation As Integer
            Dim genHandle = CType(aggregator.GetGenerationHandle(valueHandle, generation), BlobHandle)
            Dim attributeData = readers(generation).GetBlobBytes(genHandle)
            AssertEx.Equal(expectedValue, attributeData)
        End Sub

        Public Shared Sub CheckStringValue(readers As IList(Of MetadataReader), valueHandle As StringHandle, expectedValue As String)
            Dim aggregator = GetAggregator(readers)

            Dim generation As Integer
            Dim genHandle = CType(aggregator.GetGenerationHandle(valueHandle, generation), StringHandle)
            Dim attributeData = readers(generation).GetString(genHandle)
            AssertEx.Equal(expectedValue, attributeData)
        End Sub

        Public Shared Function GetAggregator(readers As IList(Of MetadataReader)) As MetadataAggregator
            Return New MetadataAggregator(readers(0), readers.Skip(1).ToArray())
        End Function

        Friend Shared Function CreateMatcher(fromCompilation As VisualBasicCompilation, toCompilation As VisualBasicCompilation) As VisualBasicSymbolMatcher
            Return New VisualBasicSymbolMatcher(
                sourceAssembly:=fromCompilation.SourceAssembly,
                otherAssembly:=toCompilation.SourceAssembly,
                otherSynthesizedTypes:=SynthesizedTypeMaps.Empty,
                otherSynthesizedMembers:=SpecializedCollections.EmptyReadOnlyDictionary(Of ISymbolInternal, ImmutableArray(Of ISymbolInternal)),
                otherDeletedMembers:=SpecializedCollections.EmptyReadOnlyDictionary(Of ISymbolInternal, ImmutableArray(Of ISymbolInternal)))
        End Function
    End Class

    Public Module EditAndContinueTestExtensions
        <Extension>
        Public Function WithSource(compilation As VisualBasicCompilation, newSource As String) As VisualBasicCompilation
            Return compilation.RemoveAllSyntaxTrees().AddSyntaxTrees(VisualBasicSyntaxTree.ParseText(newSource))
        End Function

        <Extension>
        Public Function WithSource(compilation As VisualBasicCompilation, newSource As XElement) As VisualBasicCompilation
            Return compilation.RemoveAllSyntaxTrees().AddSyntaxTrees(ToSourceTrees(newSource))
        End Function

        <Extension>
        Public Function WithSource(compilation As VisualBasicCompilation, newTree As SyntaxTree) As VisualBasicCompilation
            Return compilation.RemoveAllSyntaxTrees().AddSyntaxTrees(newTree)
        End Function
    End Module
End Namespace
