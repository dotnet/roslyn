' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Debugging
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities
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

            Dim comp = CreateCompilationWithMscorlib40({source}, options:=TestOptions.ReleaseDll)

            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim info = GetMethodDebugInfo(runtime, "C.M")

                    If runtime.DebugFormat = DebugInformationFormat.PortablePdb Then
                        info.ImportRecordGroups.Verify("
                        {
                            Namespace: string='System'
                        }
                        {
                        }")
                    Else
                        info.ImportRecordGroups.Verify("
                        {
                            Namespace: string='System'
                            CurrentNamespace: string=''
                        }
                        {
                        }")
                    End If
                End Sub)
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

            Dim comp = CreateCompilationWithMscorlib40({source}, options:=TestOptions.ReleaseDll)

            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim info1 = GetMethodDebugInfo(runtime, "C.M1")

                    If runtime.DebugFormat = DebugInformationFormat.PortablePdb Then
                        info1.ImportRecordGroups.Verify("
                        {
                            Namespace: string='System.IO'
                        }
                        {
                        }")
                    Else
                        info1.ImportRecordGroups.Verify("
                        {
                            Namespace: string='System.IO'
                            CurrentNamespace: string=''
                        }
                        {
                        }")
                    End If

                    Assert.Equal("", info1.DefaultNamespaceName)

                    Dim info2 = GetMethodDebugInfo(runtime, "C.M2")

                    If runtime.DebugFormat = DebugInformationFormat.PortablePdb Then
                        info2.ImportRecordGroups.Verify("
                        {
                            Namespace: string='System.IO'
                        }
                        {
                        }")
                    Else
                        info2.ImportRecordGroups.Verify("
                        {
                            Namespace: string='System.IO'
                            CurrentNamespace: string=''
                        }
                        {
                        }")
                    End If

                    Assert.Equal("", info2.DefaultNamespaceName)
                End Sub)
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

            Dim comp = CreateCompilationWithMscorlib40({source}, options:=options)

            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim info = GetMethodDebugInfo(runtime, "root.N.C.M")

                    If runtime.DebugFormat = DebugInformationFormat.PortablePdb Then
                        info.ImportRecordGroups.Verify("
                        {
                            XmlNamespace: alias='' string='http://xml0'
                            XmlNamespace: alias='C' string='http://xml1'
                            Namespace: alias='A' string='System.Collections'
                            Type: alias='B' type='System.Collections.ArrayList'
                            Namespace: string='System'
                            Type: type='System.IO.Path'
                        }
                        {
                            XmlNamespace: alias='' string='http://xml2'
                            XmlNamespace: alias='F' string='http://xml3'
                            Namespace: alias='D' string='System.Threading.Tasks'
                            Type: alias='E' type='System.Threading.Timer'
                            Namespace: string='System.Runtime'
                            Type: type='System.Threading.Thread'
                        }")
                    Else
                        info.ImportRecordGroups.Verify("
                        {
                            XmlNamespace: alias='' string='http://xml0'
                            XmlNamespace: alias='C' string='http://xml1'
                            NamespaceOrType: alias='A' string='System.Collections'
                            NamespaceOrType: alias='B' string='System.Collections.ArrayList'
                            Namespace: string='System'
                            Type: string='System.IO.Path'
                            CurrentNamespace: string='root.N'
                        }
                        {
                            XmlNamespace: alias='' string='http://xml2'
                            XmlNamespace: alias='F' string='http://xml3'
                            NamespaceOrType: alias='D' string='System.Threading.Tasks'
                            NamespaceOrType: alias='E' string='System.Threading.Timer'
                            Namespace: string='System.Runtime'
                            Type: string='System.Threading.Thread'
                        }")
                    End If

                    Assert.Equal("root", info.DefaultNamespaceName)
                End Sub)
        End Sub

#End Region

#Region "Invalid PDBs"

        <Fact>
        Public Sub BadPdb_ForwardChain()
            Const methodToken1 = &H600057A ' Forwards to 2
            Const methodToken2 = &H600055D ' Forwards to 3
            Const methodToken3 = &H6000540 ' Has an import
            Const importString = "@F:System"

            Dim getMethodImportStrings =
                Function(token As Integer, arg As Integer)
                    Select Case token
                        Case methodToken1
                            Return ImmutableArray.Create("@" & methodToken2)
                        Case methodToken2
                            Return ImmutableArray.Create("@" & methodToken3)
                        Case methodToken3
                            Return ImmutableArray.Create(importString)
                        Case Else
                            Throw ExceptionUtilities.Unreachable
                    End Select
                End Function

            Dim importStrings = CustomDebugInfoReader.GetVisualBasicImportStrings(methodToken1, 0, getMethodImportStrings)
            Assert.Equal("@" & methodToken3, importStrings.Single())

            importStrings = CustomDebugInfoReader.GetVisualBasicImportStrings(methodToken2, 0, getMethodImportStrings)
            Assert.Equal(importString, importStrings.Single())

            importStrings = CustomDebugInfoReader.GetVisualBasicImportStrings(methodToken3, 0, getMethodImportStrings)
            Assert.Equal(importString, importStrings.Single())
        End Sub

        <Fact>
        Public Sub BadPdb_ForwardCycle()
            Const methodToken1 = &H600057A ' Forwards to itself

            Dim getMethodImportStrings =
              Function(token As Integer, arg As Integer)
                  Select Case token
                      Case methodToken1
                          Return ImmutableArray.Create("@" & methodToken1)
                      Case Else
                          Throw ExceptionUtilities.Unreachable
                  End Select
              End Function

            Dim importStrings = CustomDebugInfoReader.GetVisualBasicImportStrings(methodToken1, 0, getMethodImportStrings)
            Assert.Equal("@" & methodToken1, importStrings.Single())
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/999086")>
        Public Sub BadPdb_InvalidAliasTarget()
            Const source = "
Public Class C
    Public Shared Sub Main()
    End Sub
End Class
"

            Dim comp = CreateCompilationWithMscorlib40({source})
            Dim exeBytes = comp.EmitToArray()

            Dim symReader = ExpressionCompilerTestHelpers.ConstructSymReaderWithImports(
                exeBytes,
                "Main",
                "@F:System", ' Valid
                "@FA:O=1", ' Invalid
                "@FA:SC=System.Collections") ' Valid

            Dim exeModule = ModuleInstance.Create(exeBytes, symReader)
            Dim runtime = CreateRuntimeInstance(exeModule, {MscorlibRef})
            Dim evalContext = CreateMethodContext(runtime, "C.Main")
            Dim compContext = evalContext.CreateCompilationContext(withSyntax:=True) ' Used to throw.

            Dim rootNamespace As NamespaceSymbol = Nothing
            Dim currentNamespace As NamespaceSymbol = Nothing
            Dim typesAndNamespaces As ImmutableArray(Of NamespaceOrTypeAndImportsClausePosition) = Nothing
            Dim aliases As Dictionary(Of String, AliasAndImportsClausePosition) = Nothing
            Dim xmlNamespaces As Dictionary(Of String, XmlNamespaceAndImportsClausePosition) = Nothing
            GetImports(compContext, rootNamespace, currentNamespace, typesAndNamespaces, aliases, xmlNamespaces)

            Assert.Equal("System", typesAndNamespaces.Single().NamespaceOrType.ToTestDisplayString())
            Assert.Equal("SC", aliases.Keys.Single())
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/999086")>
        Public Sub BadPdb_InvalidAliasName()
            Const source = "
Public Class C
    Public Shared Sub Main()
    End Sub
End Class
"

            Dim comp = CreateCompilationWithMscorlib40({source})
            Dim exeBytes = comp.EmitToArray()

            Dim symReader = ExpressionCompilerTestHelpers.ConstructSymReaderWithImports(
                exeBytes,
                "Main",
                "@F:System", ' Valid
                "@FA:S.I=System.IO", ' Invalid
                "@FA:SC=System.Collections") ' Valid

            Dim exeModule = ModuleInstance.Create(exeBytes, symReader)
            Dim runtime = CreateRuntimeInstance(exeModule, {MscorlibRef})
            Dim evalContext = CreateMethodContext(runtime, "C.Main")
            Dim compContext = evalContext.CreateCompilationContext(withSyntax:=True) ' Used to throw.

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
            Const methodToken = &H6000540 ' Has an import
            Const importString = "&MyPia"

            Dim getMethodImportStrings =
                Function(token As Integer, arg As Integer)
                    Select Case token
                        Case methodToken
                            Return ImmutableArray.Create(importString)
                        Case Else
                            Throw ExceptionUtilities.Unreachable
                    End Select
                End Function

            Dim importStrings = CustomDebugInfoReader.GetVisualBasicImportStrings(methodToken, 0, getMethodImportStrings)
            Assert.Equal(importString, importStrings.Single())
        End Sub

        <Fact>
        Public Sub OldPdb_DefunctKinds()
            Const methodToken = &H6000540 ' Has an import
            Const importString1 = "#NotSureWhatGoesHere"
            Const importString2 = "$NotSureWhatGoesHere"

            Dim getMethodImportStrings =
                Function(token As Integer, arg As Integer)
                    Select Case token
                        Case methodToken
                            Return ImmutableArray.Create(importString1, importString2)
                        Case Else
                            Throw ExceptionUtilities.Unreachable
                    End Select
                End Function

            Dim importStrings = CustomDebugInfoReader.GetVisualBasicImportStrings(methodToken, 0, getMethodImportStrings)
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

            Dim comp = CreateCompilationWithMscorlib40({source}, options:=options)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim info = GetMethodDebugInfo(runtime, "root.N.C.M")

                    If runtime.DebugFormat = DebugInformationFormat.PortablePdb Then
                        info.ImportRecordGroups.Verify("
                        {
                            XmlNamespace: alias='' string='http://xml0'
                            XmlNamespace: alias='C' string='http://xml1'
                            Namespace: alias='A' string='System.Collections'
                            Type: alias='B' type='System.Collections.ArrayList'
                            Namespace: string='System'
                            Type: type='System.IO.Path'
                        }
                        {
                            XmlNamespace: alias='' string='http://xml2'
                            XmlNamespace: alias='F' string='http://xml3'
                            Namespace: alias='D' string='System.Threading.Tasks'
                            Type: alias='E' type='System.Threading.Timer'
                            Namespace: string='System.Runtime'
                            Type: type='System.Threading.Thread'
                        }")
                    Else
                        info.ImportRecordGroups.Verify("
                        {
                            XmlNamespace: alias='' string='http://xml0'
                            XmlNamespace: alias='C' string='http://xml1'
                            NamespaceOrType: alias='A' string='System.Collections'
                            NamespaceOrType: alias='B' string='System.Collections.ArrayList'
                            Namespace: string='System'
                            Type: string='System.IO.Path'
                            CurrentNamespace: string='root.N'
                        }
                        {
                            XmlNamespace: alias='' string='http://xml2'
                            XmlNamespace: alias='F' string='http://xml3'
                            NamespaceOrType: alias='D' string='System.Threading.Tasks'
                            NamespaceOrType: alias='E' string='System.Threading.Timer'
                            Namespace: string='System.Runtime'
                            Type: string='System.Threading.Thread'
                        }")
                    End If

                    Dim rootNamespace As NamespaceSymbol = Nothing
                    Dim currentNamespace As NamespaceSymbol = Nothing
                    Dim typesAndNamespaces As ImmutableArray(Of NamespaceOrTypeAndImportsClausePosition) = Nothing
                    Dim aliases As Dictionary(Of String, AliasAndImportsClausePosition) = Nothing
                    Dim xmlNamespaces As Dictionary(Of String, XmlNamespaceAndImportsClausePosition) = Nothing

                    GetImports(
                        runtime,
                        "root.N.C.M",
                        rootNamespace,
                        currentNamespace,
                        typesAndNamespaces,
                        aliases,
                        xmlNamespaces)

                    Assert.Equal("root", rootNamespace.ToTestDisplayString())
                    Assert.Equal("root.N", currentNamespace.ToTestDisplayString())

                    Dim expectedNamespaces = If(runtime.DebugFormat = DebugInformationFormat.PortablePdb,
                        {"System", "System.IO.Path", "System.Runtime", "System.Threading.Thread"},
                        {"System", "System.IO.Path", "System.Runtime", "System.Threading.Thread", "root.N"})

                    AssertEx.SetEqual(expectedNamespaces, typesAndNamespaces.Select(Function(i) i.NamespaceOrType.ToTestDisplayString()))

                    AssertEx.SetEqual(aliases.Keys, "A", "B", "D", "E")
                    Assert.Equal("System.Collections", aliases("A").Alias.Target.ToTestDisplayString())
                    Assert.Equal("System.Collections.ArrayList", aliases("B").Alias.Target.ToTestDisplayString())
                    Assert.Equal("System.Threading.Tasks", aliases("D").Alias.Target.ToTestDisplayString())
                    Assert.Equal("System.Threading.Timer", aliases("E").Alias.Target.ToTestDisplayString())

                    AssertEx.SetEqual(xmlNamespaces.Keys, "", "C", "F")
                    Assert.Equal("http://xml0", xmlNamespaces("").XmlNamespace)
                    Assert.Equal("http://xml1", xmlNamespaces("C").XmlNamespace)
                    Assert.Equal("http://xml3", xmlNamespaces("F").XmlNamespace)
                End Sub)
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
                Dim comp = CreateCompilationWithMscorlib40({source}, options:=TestOptions.ReleaseDll.WithRootNamespace(rootNamespaceName))
                comp.GetDiagnostics().Where(Function(d) d.Severity > DiagnosticSeverity.Info).Verify()

                WithRuntimeInstance(comp,
                    Sub(runtime)
                        Dim rootNamespace As NamespaceSymbol = Nothing
                        Dim currentNamespace As NamespaceSymbol = Nothing
                        Dim typesAndNamespaces As ImmutableArray(Of NamespaceOrTypeAndImportsClausePosition) = Nothing
                        Dim aliases As Dictionary(Of String, AliasAndImportsClausePosition) = Nothing
                        Dim xmlNamespaces As Dictionary(Of String, XmlNamespaceAndImportsClausePosition) = Nothing

                        GetImports(
                            runtime,
                            "N.C.M",
                            rootNamespace,
                            currentNamespace,
                            typesAndNamespaces,
                            aliases,
                            xmlNamespaces)

                        Assert.True(rootNamespace.IsGlobalNamespace)
                        Assert.Equal("N", currentNamespace.ToTestDisplayString())

                        ' Portable PDB doesn't include CurrentNamespace:
                        If runtime.DebugFormat = DebugInformationFormat.PortablePdb Then
                            Assert.True(typesAndNamespaces.IsDefault)
                        Else
                            Assert.Equal("N", typesAndNamespaces.Single().NamespaceOrType.ToTestDisplayString())
                        End If

                        Assert.Null(aliases)
                        Assert.Null(xmlNamespaces)
                    End Sub)
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
            Dim comp = CreateCompilationWithMscorlib40({source}, options:=options)
            comp.GetDiagnostics().Where(Function(d) d.Severity > DiagnosticSeverity.Info).Verify()

            Dim rootNamespace As NamespaceSymbol = Nothing
            Dim currentNamespace As NamespaceSymbol = Nothing
            Dim typesAndNamespaces As ImmutableArray(Of NamespaceOrTypeAndImportsClausePosition) = Nothing
            Dim aliases As Dictionary(Of String, AliasAndImportsClausePosition) = Nothing
            Dim xmlNamespaces As Dictionary(Of String, XmlNamespaceAndImportsClausePosition) = Nothing

            WithRuntimeInstance(comp,
                Sub(runtime)
                    GetImports(
                        runtime,
                        "root.N.C.M",
                        rootNamespace,
                        currentNamespace,
                        typesAndNamespaces,
                        aliases,
                        xmlNamespaces)

                    Assert.Equal("root", rootNamespace.ToTestDisplayString())
                    Assert.Equal("root.N", currentNamespace.ToTestDisplayString())

                    ' CONSIDER: We could de-dup unaliased imports as well.
                    Dim expectedNamespaces = If(runtime.DebugFormat = DebugInformationFormat.PortablePdb,
                        {"System", "System.IO.Path", "System", "System.IO.Path"},
                        {"System", "System.IO.Path", "System", "System.IO.Path", "root.N"})

                    AssertEx.SetEqual(expectedNamespaces, typesAndNamespaces.Select(Function(i) i.NamespaceOrType.ToTestDisplayString()))

                    AssertEx.SetEqual(aliases.Keys, "A", "B")
                    Assert.Equal("System.Collections", aliases("A").Alias.Target.ToTestDisplayString())
                    Assert.Equal("System.Collections.ArrayList", aliases("B").Alias.Target.ToTestDisplayString())

                    AssertEx.SetEqual(xmlNamespaces.Keys, "", "C")
                    Assert.Equal("http://xml0", xmlNamespaces("").XmlNamespace)
                    Assert.Equal("http://xml1", xmlNamespaces("C").XmlNamespace)
                End Sub)
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2441")>
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

            Dim comp1 = CreateEmptyCompilationWithReferences(VisualBasicSyntaxTree.ParseText(source1), {MscorlibRef_v20}, TestOptions.DebugDll)
            Dim module1 = comp1.ToModuleInstance()

            Dim comp2 = CreateEmptyCompilationWithReferences(VisualBasicSyntaxTree.ParseText(source2), {MscorlibRef_v4_0_30316_17626, module1.GetReference()}, TestOptions.DebugDll)
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

        Private Shared Sub GetImports(
            runtime As RuntimeInstance,
            methodName As String,
            <Out> ByRef rootNamespace As NamespaceSymbol,
            <Out> ByRef currentNamespace As NamespaceSymbol,
            <Out> ByRef typesAndNamespaces As ImmutableArray(Of NamespaceOrTypeAndImportsClausePosition),
            <Out> ByRef aliases As Dictionary(Of String, AliasAndImportsClausePosition),
            <Out> ByRef xmlNamespaces As Dictionary(Of String, XmlNamespaceAndImportsClausePosition))

            Dim evalContext = CreateMethodContext(runtime, methodName)
            Dim compContext = evalContext.CreateCompilationContext(withSyntax:=True)

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
