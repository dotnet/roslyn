' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.IO
Imports System.Reflection.Metadata
Imports System.Reflection.Metadata.Ecma335
Imports System.Runtime.CompilerServices
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.MetadataUtilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class EditAndContinueTestBase
        Inherits BasicTestBase

        ' PDB reader can only be accessed from a single thread, so avoid concurrent compilation:
        Protected Shared ReadOnly ComSafeDebugDll As VisualBasicCompilationOptions = TestOptions.DebugDll.WithConcurrentBuild(False)

        Friend Shared ReadOnly EmptyLocalsProvider As Func(Of MethodDefinitionHandle, EditAndContinueMethodDebugInformation) = Function(token) Nothing

        Friend Shared Function Visualize(baseline As ModuleMetadata, ParamArray deltas As PinnedMetadata()) As String
            Dim result = New StringWriter()
            Dim visualizer = New MetadataVisualizer({baseline.MetadataReader}.Concat(deltas.Select(Function(d) d.Reader)).ToArray(), result)
            visualizer.VisualizeAllGenerations()
            Return result.ToString()
        End Function

        Friend Shared Function MarkedSource(source As XElement, Optional fileName As String = "", Optional options As VisualBasicParseOptions = Nothing) As SourceWithMarkedNodes
            Return New SourceWithMarkedNodes(source.Value, Function(s) Parse(s, fileName, options), Function(s) CInt(GetType(SyntaxKind).GetField(s).GetValue(Nothing)))
        End Function

        Friend Shared Function MarkedSource(source As String, Optional fileName As String = "", Optional options As VisualBasicParseOptions = Nothing) As SourceWithMarkedNodes
            Return New SourceWithMarkedNodes(source, Function(s) Parse(s, fileName, options), Function(s) CInt(GetType(SyntaxKind).GetField(s).GetValue(Nothing)))
        End Function

        Friend Shared Function GetSyntaxMapFromMarkers(source0 As SourceWithMarkedNodes, source1 As SourceWithMarkedNodes) As Func(Of SyntaxNode, SyntaxNode)
            Return SourceWithMarkedNodes.GetSyntaxMap(source0, source1)
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

        Friend Shared Sub CheckEncLog(reader As MetadataReader, ParamArray rows As EditAndContinueLogEntry())
            AssertEx.Equal(rows, reader.GetEditAndContinueLogEntries(), itemInspector:=AddressOf EncLogRowToString)
        End Sub

        Friend Shared Sub CheckEncLogDefinitions(reader As MetadataReader, ParamArray rows As EditAndContinueLogEntry())
            AssertEx.Equal(rows, reader.GetEditAndContinueLogEntries().Where(Function(entry) IsDefinition(entry)), itemInspector:=AddressOf EncLogRowToString)
        End Sub

        Friend Shared Function IsDefinition(entry As EditAndContinueLogEntry) As Boolean
            Dim index As TableIndex
            Assert.True(MetadataTokens.TryGetTableIndex(entry.Handle.Kind, index))

            Select Case index
                Case TableIndex.MethodDef,
                     TableIndex.Field,
                     TableIndex.Constant,
                     TableIndex.GenericParam,
                     TableIndex.GenericParamConstraint,
                     TableIndex.[Event],
                     TableIndex.CustomAttribute,
                     TableIndex.DeclSecurity,
                     TableIndex.Assembly,
                     TableIndex.MethodImpl,
                     TableIndex.Param,
                     TableIndex.[Property],
                     TableIndex.TypeDef,
                     TableIndex.ExportedType,
                     TableIndex.StandAloneSig,
                     TableIndex.ClassLayout,
                     TableIndex.FieldLayout,
                     TableIndex.FieldMarshal,
                     TableIndex.File,
                     TableIndex.ImplMap,
                     TableIndex.InterfaceImpl,
                     TableIndex.ManifestResource,
                     TableIndex.MethodSemantics,
                     TableIndex.[Module],
                     TableIndex.NestedClass,
                     TableIndex.EventMap,
                     TableIndex.PropertyMap
                    Return True
            End Select

            Return False
        End Function


        Friend Shared Sub CheckEncMap(reader As MetadataReader, ParamArray [handles] As EntityHandle())
            AssertEx.Equal([handles], reader.GetEditAndContinueMapEntries(), itemInspector:=AddressOf EncMapRowToString)
        End Sub

        Friend Shared Sub CheckNames(reader As MetadataReader, [handles] As StringHandle(), ParamArray expectedNames As String())
            CheckNames({reader}, [handles], expectedNames)
        End Sub

        Friend Shared Sub CheckNames(readers As MetadataReader(), [handles] As StringHandle(), ParamArray expectedNames As String())
            Dim actualNames = readers.GetStrings([handles])
            AssertEx.Equal(expectedNames, actualNames)
        End Sub

        Friend Shared Sub CheckNamesSorted(readers As MetadataReader(), [handles] As StringHandle(), ParamArray expectedNames As String())
            Dim actualNames = readers.GetStrings([handles])
            Array.Sort(actualNames)
            Array.Sort(expectedNames)
            AssertEx.Equal(expectedNames, actualNames)
        End Sub

        Friend Shared Function EncLogRowToString(row As EditAndContinueLogEntry) As String
            Dim index As TableIndex = 0
            MetadataTokens.TryGetTableIndex(row.Handle.Kind, index)
            Return String.Format(
                "Row({0}, TableIndex.{1}, EditAndContinueOperation.{2})",
                MetadataTokens.GetRowNumber(row.Handle),
                index,
                row.Operation)
        End Function

        Friend Shared Function EncMapRowToString(handle As EntityHandle) As String
            Dim index As TableIndex = 0
            MetadataTokens.TryGetTableIndex(handle.Kind, index)
            Return String.Format(
                "Handle({0}, TableIndex.{1})",
                MetadataTokens.GetRowNumber(handle),
                index)
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
