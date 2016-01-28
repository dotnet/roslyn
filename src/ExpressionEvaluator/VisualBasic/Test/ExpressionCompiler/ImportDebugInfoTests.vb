' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.IO
Imports System.Reflection
Imports System.Reflection.Metadata
Imports System.Reflection.Metadata.Ecma335
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Microsoft.DiaSymReader
Imports Roslyn.Test.PdbUtilities
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator.UnitTests
    Public Class ImportsDebugInfoTests
        Inherits ExpressionCompilerTestBase

#Region "Import strings"

        <Fact>
        Public Sub SimplestCase()
            Dim source = "
Imports System

Class C
    Sub M()
    End Sub
End Class
"

            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.ReleaseDll)
            comp.GetDiagnostics().Where(Function(d) d.Severity > DiagnosticSeverity.Info).Verify()

            Dim importStrings = GetImportStrings(comp, "M")
            AssertEx.SetEqual(importStrings, "@F:System", "")
        End Sub

        <Fact>
        Public Sub Forward()
            Dim source = "
Imports System.IO

Class C
    ' One of these methods will forward to the other since they're adjacent.
    Sub M1()
    End Sub
    Sub M2()
    End Sub
End Class
"

            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.ReleaseDll)
            comp.GetDiagnostics().Where(Function(d) d.Severity > DiagnosticSeverity.Info).Verify()

            Dim importStrings1 = GetImportStrings(comp, "M1")
            AssertEx.SetEqual(importStrings1, "@F:System.IO", "")

            Dim importStrings2 = GetImportStrings(comp, "M2")
            Assert.Equal(importStrings1.AsEnumerable(), importStrings2)
        End Sub

        <Fact>
        Public Sub ImportKinds()
            Dim source = "
Imports System
Imports System.IO.Path
Imports A = System.Collections
Imports B = System.Collections.ArrayList
Imports <xmlns=""http://xml0"">
Imports <xmlns:C=""http://xml1"">

Namespace N
    Class C
        Sub M()
        End Sub
    End Class
End Namespace
"

            Dim options As VisualBasicCompilationOptions = TestOptions.ReleaseDll.WithRootNamespace("root").WithGlobalImports(GlobalImport.Parse(
            {
                "System.Runtime",
                "System.Threading.Thread",
                "D=System.Threading.Tasks",
                "E=System.Threading.Timer",
                "<xmlns=""http://xml2"">",
                "<xmlns:F=""http://xml3"">"
            }))
            Dim comp = CreateCompilationWithMscorlib({source}, options:=options)
            comp.GetDiagnostics().Where(Function(d) d.Severity > DiagnosticSeverity.Info).Verify()

            Dim importStrings = GetImportStrings(comp, "M")
            AssertEx.SetEqual(
                importStrings,
                "@F:System", ' File-level namespace
                "@FT:System.IO.Path", ' File-level type
                "@FA:A=System.Collections", ' File-level aliased namespace
                "@FA:B=System.Collections.ArrayList", ' File-level aliased type
                "@FX:=http://xml0", ' File-level XML namespace
                "@FX:C=http://xml1", ' File-level aliased XML namespace
                "@P:System.Runtime", ' Project-level namespace
                "@PT:System.Threading.Thread", ' Project-level type
                "@PA:D=System.Threading.Tasks", ' Project-level aliased namespace
                "@PA:E=System.Threading.Timer", ' Project-level aliased type
                "@PX:=http://xml2", ' Project-level XML namespace
                "@PX:F=http://xml3", ' Project-level aliased XML namespace
                "*root", ' Root namespace
                "root.N") ' Containing namespace
        End Sub

        Private Shared Function GetImportStrings(compilation As Compilation, methodName As String) As ImmutableArray(Of String)
            Assert.NotNull(compilation)
            Assert.NotNull(methodName)

            Using exebits As New MemoryStream()
                Using pdbbits As New MemoryStream()
                    compilation.Emit(exebits, pdbbits)

                    exebits.Position = 0
                    Using metadata = ModuleMetadata.CreateFromStream(exebits, leaveOpen:=True)
                        Dim [module] = metadata.Module
                        Dim metadataReader = [module].MetadataReader
                        Dim methodHandle = metadataReader.MethodDefinitions.Single(Function(mh) metadataReader.GetString(metadataReader.GetMethodDefinition(mh).Name) = methodName)
                        Dim methodToken = metadataReader.GetToken(methodHandle)

                        pdbbits.Position = 0
                        Dim reader = SymReaderFactory.CreateReader(pdbbits)
                        Return reader.GetVisualBasicImportStrings(methodToken, methodVersion:=1)
                    End Using
                End Using
            End Using
        End Function

#End Region

#Region "Invalid PDBs"

        <Fact>
        Public Sub BadPdb_ForwardChain()
            Const methodVersion = 1
            Const methodToken1 = &H600057A ' Forwards to 2
            Const methodToken2 = &H600055D ' Forwards to 3
            Const methodToken3 = &H6000540 ' Has an import
            Const importString = "@F:System"

            Dim reader As ISymUnmanagedReader = New MockSymUnmanagedReader(
                            New Dictionary(Of Integer, MethodDebugInfoBytes)() From
                            {
                                {methodToken1, New MethodDebugInfoBytes.Builder({({"@" & methodToken2})}).Build()},
                                {methodToken2, New MethodDebugInfoBytes.Builder({({"@" & methodToken3})}).Build()},
                                {methodToken3, New MethodDebugInfoBytes.Builder({({importString})}).Build()}
                            }.ToImmutableDictionary())

            Dim importStrings = reader.GetVisualBasicImportStrings(methodToken1, methodVersion)
            Assert.Equal("@" & methodToken3, importStrings.Single())

            importStrings = reader.GetVisualBasicImportStrings(methodToken2, methodVersion)
            Assert.Equal(importString, importStrings.Single())

            importStrings = reader.GetVisualBasicImportStrings(methodToken3, methodVersion)
            Assert.Equal(importString, importStrings.Single())
        End Sub

        <Fact>
        Public Sub BadPdb_ForwardCycle()
            Const methodVersion = 1
            Const methodToken1 = &H600057A ' Forwards to itself

            Dim reader As ISymUnmanagedReader = New MockSymUnmanagedReader(
                            New Dictionary(Of Integer, MethodDebugInfoBytes)() From
                            {
                                {methodToken1, New MethodDebugInfoBytes.Builder({({"@" & methodToken1})}).Build()}
                            }.ToImmutableDictionary())

            Dim importStrings = reader.GetVisualBasicImportStrings(methodToken1, methodVersion)
            Assert.Equal("@" & methodToken1, importStrings.Single())
        End Sub

        <WorkItem(999086, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/999086")>
        <Fact>
        Public Sub BadPdb_InvalidAliasTarget()
            Const source = "
Public Class C
    Public Shared Sub Main()
    End Sub
End Class
"

            Dim comp = CreateCompilationWithMscorlib({source})
            Dim exeBytes = comp.EmitToArray()

            Dim symReader = ExpressionCompilerTestHelpers.ConstructSymReaderWithImports(
                exeBytes,
                "Main",
                "@F:System", ' Valid
                "@FA:O=1", ' Invalid
                "@FA:SC=System.Collections") ' Valid

            Dim runtime = CreateRuntimeInstance({MscorlibRef}, exeBytes, symReader)
            Dim evalContext = CreateMethodContext(runtime, "C.Main")
            Dim compContext = evalContext.CreateCompilationContext(SyntaxHelpers.ParseDebuggerExpression("Nothing", consumeFullText:=True)) ' Used to throw.

            Dim rootNamespace As NamespaceSymbol = Nothing
            Dim currentNamespace As NamespaceSymbol = Nothing
            Dim typesAndNamespaces As ImmutableArray(Of NamespaceOrTypeAndImportsClausePosition) = Nothing
            Dim aliases As Dictionary(Of String, AliasAndImportsClausePosition) = Nothing
            Dim xmlNamespaces As Dictionary(Of String, XmlNamespaceAndImportsClausePosition) = Nothing
            GetImports(compContext, rootNamespace, currentNamespace, typesAndNamespaces, aliases, xmlNamespaces)

            Assert.Equal("System", typesAndNamespaces.Single().NamespaceOrType.ToTestDisplayString())
            Assert.Equal("SC", aliases.Keys.Single())
        End Sub

        <WorkItem(999086, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/999086")>
        <Fact>
        Public Sub BadPdb_InvalidAliasName()
            Const source = "
Public Class C
    Public Shared Sub Main()
    End Sub
End Class
"

            Dim comp = CreateCompilationWithMscorlib({source})
            Dim exeBytes = comp.EmitToArray()

            Dim symReader = ExpressionCompilerTestHelpers.ConstructSymReaderWithImports(
                exeBytes,
                "Main",
                "@F:System", ' Valid
                "@FA:S.I=System.IO", ' Invalid
                "@FA:SC=System.Collections") ' Valid

            Dim runtime = CreateRuntimeInstance({MscorlibRef}, exeBytes, symReader)
            Dim evalContext = CreateMethodContext(runtime, "C.Main")
            Dim compContext = evalContext.CreateCompilationContext(SyntaxHelpers.ParseDebuggerExpression("Nothing", consumeFullText:=True)) ' Used to throw.

            Dim rootNamespace As NamespaceSymbol = Nothing
            Dim currentNamespace As NamespaceSymbol = Nothing
            Dim typesAndNamespaces As ImmutableArray(Of NamespaceOrTypeAndImportsClausePosition) = Nothing
            Dim aliases As Dictionary(Of String, AliasAndImportsClausePosition) = Nothing
            Dim xmlNamespaces As Dictionary(Of String, XmlNamespaceAndImportsClausePosition) = Nothing
            GetImports(compContext, rootNamespace, currentNamespace, typesAndNamespaces, aliases, xmlNamespaces)

            Assert.Equal("System", typesAndNamespaces.Single().NamespaceOrType.ToTestDisplayString())
            Assert.Equal("SC", aliases.Keys.Single())
        End Sub

        <Fact>
        Public Sub OldPdb_EmbeddedPIA()
            Const methodVersion = 1
            Const methodToken = &H6000540 ' Has an import
            Const importString = "&MyPia"

            Dim reader As ISymUnmanagedReader = New MockSymUnmanagedReader(
                            New Dictionary(Of Integer, MethodDebugInfoBytes)() From
                            {
                                {methodToken, New MethodDebugInfoBytes.Builder({({importString})}).Build()}
                            }.ToImmutableDictionary())

            Dim importStrings = reader.GetVisualBasicImportStrings(methodToken, methodVersion)
            Assert.Equal(importString, importStrings.Single())
        End Sub

        <Fact>
        Public Sub OldPdb_DefunctKinds()
            Const methodVersion = 1
            Const methodToken = &H6000540 ' Has an import
            Const importString1 = "#NotSureWhatGoesHere"
            Const importString2 = "$NotSureWhatGoesHere"

            Dim reader As ISymUnmanagedReader = New MockSymUnmanagedReader(
                            New Dictionary(Of Integer, MethodDebugInfoBytes)() From
                            {
                                {methodToken, New MethodDebugInfoBytes.Builder({({importString1, importString2})}).Build()}
                            }.ToImmutableDictionary())

            Dim importStrings = reader.GetVisualBasicImportStrings(methodToken, methodVersion)
            AssertEx.Equal(importStrings, {importString1, importString2})
        End Sub

#End Region

#Region "Binder chain"

        <Fact>
        Public Sub ImportKindSymbols()
            Dim source = "
Imports System
Imports System.IO.Path
Imports A = System.Collections
Imports B = System.Collections.ArrayList
Imports <xmlns=""http://xml0"">
Imports <xmlns:C=""http://xml1"">

Namespace N
    Class C
        Sub M()
            Console.WriteLine()
        End Sub
    End Class
End Namespace
"

            Dim options As VisualBasicCompilationOptions = TestOptions.ReleaseDll.WithRootNamespace("root").WithGlobalImports(GlobalImport.Parse(
            {
                "System.Runtime",
                "System.Threading.Thread",
                "D=System.Threading.Tasks",
                "E=System.Threading.Timer",
                "<xmlns=""http://xml2"">",
                "<xmlns:F=""http://xml3"">"
            }))
            Dim comp = CreateCompilationWithMscorlib({source}, options:=options)
            comp.GetDiagnostics().Where(Function(d) d.Severity > DiagnosticSeverity.Info).Verify()

            Dim rootNamespace As NamespaceSymbol = Nothing
            Dim currentNamespace As NamespaceSymbol = Nothing
            Dim typesAndNamespaces As ImmutableArray(Of NamespaceOrTypeAndImportsClausePosition) = Nothing
            Dim aliases As Dictionary(Of String, AliasAndImportsClausePosition) = Nothing
            Dim xmlNamespaces As Dictionary(Of String, XmlNamespaceAndImportsClausePosition) = Nothing

            Dim runtime = CreateRuntimeInstance(comp)
            GetImports(
                runtime,
                "root.N.C.M",
                GetExpressionStatement(comp),
                rootNamespace,
                currentNamespace,
                typesAndNamespaces,
                aliases,
                xmlNamespaces)

            Assert.Equal("root", rootNamespace.ToTestDisplayString())
            Assert.Equal("root.N", currentNamespace.ToTestDisplayString())

            AssertEx.SetEqual(typesAndNamespaces.Select(Function(i) i.NamespaceOrType.ToTestDisplayString()), "System", "System.IO.Path", "System.Runtime", "System.Threading.Thread", "root.N")

            AssertEx.SetEqual(aliases.Keys, "A", "B", "D", "E")
            Assert.Equal("System.Collections", aliases("A").Alias.Target.ToTestDisplayString())
            Assert.Equal("System.Collections.ArrayList", aliases("B").Alias.Target.ToTestDisplayString())
            Assert.Equal("System.Threading.Tasks", aliases("D").Alias.Target.ToTestDisplayString())
            Assert.Equal("System.Threading.Timer", aliases("E").Alias.Target.ToTestDisplayString())

            AssertEx.SetEqual(xmlNamespaces.Keys, "", "C", "F")
            Assert.Equal("http://xml0", xmlNamespaces("").XmlNamespace)
            Assert.Equal("http://xml1", xmlNamespaces("C").XmlNamespace)
            Assert.Equal("http://xml3", xmlNamespaces("F").XmlNamespace)
        End Sub

        <Fact>
        Public Sub EmptyRootNamespace()
            Dim source = "
Namespace N
    Class C
        Sub M()
            System.Console.WriteLine()
        End Sub
    End Class
End Namespace
"

            For Each rootNamespaceName In {"", Nothing}
                Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.ReleaseDll.WithRootNamespace(rootNamespaceName))
                comp.GetDiagnostics().Where(Function(d) d.Severity > DiagnosticSeverity.Info).Verify()

                Dim rootNamespace As NamespaceSymbol = Nothing
                Dim currentNamespace As NamespaceSymbol = Nothing
                Dim typesAndNamespaces As ImmutableArray(Of NamespaceOrTypeAndImportsClausePosition) = Nothing
                Dim aliases As Dictionary(Of String, AliasAndImportsClausePosition) = Nothing
                Dim xmlNamespaces As Dictionary(Of String, XmlNamespaceAndImportsClausePosition) = Nothing

                Dim runtime = CreateRuntimeInstance(comp)
                GetImports(
                    runtime,
                    "N.C.M",
                    GetExpressionStatement(comp),
                    rootNamespace,
                    currentNamespace,
                    typesAndNamespaces,
                    aliases,
                    xmlNamespaces)

                Assert.True(rootNamespace.IsGlobalNamespace)
                Assert.Equal("N", currentNamespace.ToTestDisplayString())

                Assert.Equal("N", typesAndNamespaces.Single().NamespaceOrType.ToTestDisplayString())
                Assert.Null(aliases)
                Assert.Null(xmlNamespaces)
            Next
        End Sub

        <Fact>
        Public Sub TieBreaking()
            Dim source = "
Imports System
Imports System.IO.Path
Imports A = System.Collections
Imports B = System.Collections.ArrayList
Imports <xmlns=""http://xml0"">
Imports <xmlns:C=""http://xml1"">

Namespace N
    Class C
        Sub M()
            Console.WriteLine()
        End Sub
    End Class
End Namespace
"

            Dim options As VisualBasicCompilationOptions = TestOptions.ReleaseDll.WithRootNamespace("root").WithGlobalImports(GlobalImport.Parse(
            {
                "System",
                "System.IO.Path",
                "A=System.Threading.Tasks",
                "B=System.Threading.Timer",
                "<xmlns=""http://xml2"">",
                "<xmlns:C=""http://xml3"">"
            }))
            Dim comp = CreateCompilationWithMscorlib({source}, options:=options)
            comp.GetDiagnostics().Where(Function(d) d.Severity > DiagnosticSeverity.Info).Verify()

            Dim rootNamespace As NamespaceSymbol = Nothing
            Dim currentNamespace As NamespaceSymbol = Nothing
            Dim typesAndNamespaces As ImmutableArray(Of NamespaceOrTypeAndImportsClausePosition) = Nothing
            Dim aliases As Dictionary(Of String, AliasAndImportsClausePosition) = Nothing
            Dim xmlNamespaces As Dictionary(Of String, XmlNamespaceAndImportsClausePosition) = Nothing

            Dim runtime = CreateRuntimeInstance(comp)
            GetImports(
                runtime,
                "root.N.C.M",
                GetExpressionStatement(comp),
                rootNamespace,
                currentNamespace,
                typesAndNamespaces,
                aliases,
                xmlNamespaces)

            Assert.Equal("root", rootNamespace.ToTestDisplayString())
            Assert.Equal("root.N", currentNamespace.ToTestDisplayString())

            ' CONSIDER: We could de-dup unaliased imports as well.
            AssertEx.SetEqual(typesAndNamespaces.Select(Function(i) i.NamespaceOrType.ToTestDisplayString()), "System", "System.IO.Path", "System", "System.IO.Path", "root.N")

            AssertEx.SetEqual(aliases.Keys, "A", "B")
            Assert.Equal("System.Collections", aliases("A").Alias.Target.ToTestDisplayString())
            Assert.Equal("System.Collections.ArrayList", aliases("B").Alias.Target.ToTestDisplayString())

            AssertEx.SetEqual(xmlNamespaces.Keys, "", "C")
            Assert.Equal("http://xml0", xmlNamespaces("").XmlNamespace)
            Assert.Equal("http://xml1", xmlNamespaces("C").XmlNamespace)
        End Sub

        <WorkItem(2441, "https://github.com/dotnet/roslyn/issues/2441")>
        <Fact>
        Public Sub AssemblyQualifiedNameResolutionWithUnification()
            Const source1 = "
Imports SI = System.Int32

Public Class C1
    Sub M()
    End Sub
End Class
"

            Const source2 = "
Public Class C2 : Inherits C1
End Class
"

            Dim comp1 = CreateCompilationWithReferences(VisualBasicSyntaxTree.ParseText(source1), {MscorlibRef_v20}, TestOptions.DebugDll, assemblyName:="A")
            Dim module1 = comp1.ToModuleInstance()

            Dim comp2 = CreateCompilationWithReferences(VisualBasicSyntaxTree.ParseText(source2), {MscorlibRef_v4_0_30316_17626, module1.MetadataReference}, TestOptions.DebugDll, assemblyName:="B")
            Dim module2 = comp2.ToModuleInstance()

            Dim runtime = CreateRuntimeInstance({module1, module2, MscorlibRef_v4_0_30316_17626.ToModuleInstance(), ExpressionCompilerTestHelpers.IntrinsicAssemblyReference.ToModuleInstance()})
            Dim context = CreateMethodContext(runtime, "C1.M")

            Dim errorMessage As String = Nothing
            Dim testData As New CompilationTestData()
            context.CompileExpression("GetType(SI)", errorMessage, testData)
            Assert.Null(errorMessage)

            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
// Code size       11 (0xb)
.maxstack  1
IL_0000:  ldtoken    ""Integer""
IL_0005:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
IL_000a:  ret
}
")
        End Sub

        Private Shared Function GetExpressionStatement(compilation As Compilation) As ExpressionStatementSyntax
            Return DirectCast(compilation.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType(Of InvocationExpressionSyntax).Single().Parent, ExpressionStatementSyntax)
        End Function

        Private Shared Sub GetImports(
            runtime As RuntimeInstance,
            methodName As String,
            syntax As ExpressionStatementSyntax,
            <Out> ByRef rootNamespace As NamespaceSymbol,
            <Out> ByRef currentNamespace As NamespaceSymbol,
            <Out> ByRef typesAndNamespaces As ImmutableArray(Of NamespaceOrTypeAndImportsClausePosition),
            <Out> ByRef aliases As Dictionary(Of String, AliasAndImportsClausePosition),
            <Out> ByRef xmlNamespaces As Dictionary(Of String, XmlNamespaceAndImportsClausePosition))

            Dim evalContext = CreateMethodContext(
                runtime,
                methodName:=methodName)
            Dim compContext = evalContext.CreateCompilationContext(syntax)

            GetImports(compContext, rootNamespace, currentNamespace, typesAndNamespaces, aliases, xmlNamespaces)
        End Sub

        Friend Shared Sub GetImports(
            compContext As CompilationContext,
            <Out> ByRef rootNamespace As NamespaceSymbol,
            <Out> ByRef currentNamespace As NamespaceSymbol,
            <Out> ByRef typesAndNamespaces As ImmutableArray(Of NamespaceOrTypeAndImportsClausePosition),
            <Out> ByRef aliases As Dictionary(Of String, AliasAndImportsClausePosition),
            <Out> ByRef xmlNamespaces As Dictionary(Of String, XmlNamespaceAndImportsClausePosition))

            Dim binder = compContext.NamespaceBinder
            Assert.NotNull(binder)

            rootNamespace = compContext.Compilation.RootNamespace

            Dim containing = binder.ContainingNamespaceOrType
            Assert.NotNull(containing)
            currentNamespace = If(containing.Kind = SymbolKind.Namespace, DirectCast(containing, NamespaceSymbol), containing.ContainingNamespace)

            typesAndNamespaces = Nothing
            aliases = Nothing
            xmlNamespaces = Nothing

            Const bindingFlags As BindingFlags = BindingFlags.NonPublic Or BindingFlags.Instance
            Dim typesAndNamespacesField = GetType(ImportedTypesAndNamespacesMembersBinder).GetField("_importedSymbols", bindingFlags)
            Assert.NotNull(typesAndNamespacesField)
            Dim aliasesField = GetType(ImportAliasesBinder).GetField("_importedAliases", bindingFlags)
            Assert.NotNull(aliasesField)
            Dim xmlNamespacesField = GetType(XmlNamespaceImportsBinder).GetField("_namespaces", bindingFlags)
            Assert.NotNull(xmlNamespacesField)

            While binder IsNot Nothing

                If TypeOf binder Is ImportedTypesAndNamespacesMembersBinder Then
                    Assert.True(typesAndNamespaces.IsDefault)
                    typesAndNamespaces = DirectCast(typesAndNamespacesField.GetValue(binder), ImmutableArray(Of NamespaceOrTypeAndImportsClausePosition))
                    AssertEx.None(typesAndNamespaces, Function(tOrN) tOrN.NamespaceOrType.Kind = SymbolKind.ErrorType)
                    Assert.False(typesAndNamespaces.IsDefault)
                ElseIf TypeOf binder Is ImportAliasesBinder Then
                    Assert.Null(aliases)
                    aliases = DirectCast(aliasesField.GetValue(binder), Dictionary(Of String, AliasAndImportsClausePosition))
                    AssertEx.All(aliases, Function(pair) pair.Key = pair.Value.Alias.Name)
                    Assert.NotNull(aliases)
                ElseIf TypeOf binder Is XmlNamespaceImportsBinder Then
                    Assert.Null(xmlNamespaces)
                    xmlNamespaces = DirectCast(xmlNamespacesField.GetValue(binder), Dictionary(Of String, XmlNamespaceAndImportsClausePosition))
                    Assert.NotNull(xmlNamespaces)
                End If

                binder = binder.ContainingBinder
            End While
        End Sub

#End Region

    End Class
End Namespace
