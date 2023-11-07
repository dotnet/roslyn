' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.IO
Imports System.Reflection
Imports System.Reflection.Metadata
Imports System.Reflection.PortableExecutable
Imports System.Text
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Emit
    Public Class CompilationEmitTests
        Inherits BasicTestBase

        <Fact>
        Public Sub CompilationEmitDiagnostics()
            ' Check that Compilation.Emit actually produces compilation errors.
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Module M1
    Sub Main()
        NoSuchMethod("hello")
    End Sub
End Module
    </file>
</compilation>)

            Dim emitResult As EmitResult

            Using output = New MemoryStream()
                emitResult = compilation.Emit(output, Nothing, Nothing, Nothing)
            End Using

            CompilationUtils.AssertTheseDiagnostics(emitResult.Diagnostics,
<expected>
BC30451: 'NoSuchMethod' is not declared. It may be inaccessible due to its protection level.
        NoSuchMethod("hello")
        ~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub CompilationEmitWithQuotedMainTypeAndRootnamespace()
            ' Check that compilation with quoted main argument and rootnamespace switches produce diagnostics.
            ' MSBuild can return quoted value which is removed from the command line arguments or by parsing
            ' command line arguments , but we DO NOT unquote arguments which are 
            ' provided by the WithMainTypeName function Or WithRootNamespace (was originally exposed through using 
            ' a Cyrillic Namespace And building Using MSBuild.)

            Dim source = <compilation>
                             <file name="a.vb">
Module Module1
    Sub Main()        
    End Sub
End Module
    </file>
                         </compilation>

            'Compilation with unquote Rootnamespace and MainTypename.
            CreateCompilationWithMscorlib40AndVBRuntime(source, options:=TestOptions.ReleaseExe.WithRootNamespace("Test").WithMainTypeName("Test.Module1")).VerifyDiagnostics()

            ' Compilation with quoted Rootnamespace and MainTypename still produces diagnostics.
            ' we do not unquote the options on WithRootnamespace or WithMainTypeName functions 
            CreateCompilationWithMscorlib40AndVBRuntime(source, options:=TestOptions.ReleaseExe.WithRootNamespace("""Test""").WithMainTypeName("""Test.Module1""")).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("RootNamespace", """Test""").WithLocation(1, 1),
                Diagnostic(ERRID.ERR_StartupCodeNotFound1).WithArguments("""Test.Module1""").WithLocation(1, 1))

            ' Use of Cyrillic rootnamespace and maintypename
            CreateCompilationWithMscorlib40AndVBRuntime(source, options:=TestOptions.ReleaseExe.WithRootNamespace("решения").WithMainTypeName("решения.Module1")).VerifyDiagnostics()

            CreateCompilationWithMscorlib40AndVBRuntime(source, options:=TestOptions.ReleaseExe.WithRootNamespace("""решения""").WithMainTypeName("""решения.Module1""")).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("RootNamespace", """решения""").WithLocation(1, 1),
                Diagnostic(ERRID.ERR_StartupCodeNotFound1).WithArguments("""решения.Module1""").WithLocation(1, 1))

        End Sub

        <Fact>
        Public Sub CompilationGetDeclarationDiagnostics()
            ' Check that Compilation.GetDeclarationDiagnostics and Bindings.GetDeclarationDiagnostics work as expected.
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Class C1
    Public Sub Goo1(x as Blech) 
    End Sub
    Public Sub Bar1()
        NoSuchMethod("from C1")
    End Sub
End Class

Partial Class C2
    Public Sub Bar2()
        NoSuchMethod("from C2")
    End Sub
End Class
    </file>
    <file name="b.vb">
Partial Class C2
    Public Sub Goo2(x as Blech) 
    End Sub
End Class

Class C3
    Public Sub Goo3(x as Blech) 
    End Sub
    Public Sub Bar3()
        NoSuchMethod("from C3")
    End Sub
End Class
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation.GetDeclarationDiagnostics(),
<expected>
BC30002: Type 'Blech' is not defined.
    Public Sub Goo1(x as Blech) 
                         ~~~~~
BC30002: Type 'Blech' is not defined.
    Public Sub Goo2(x as Blech) 
                         ~~~~~
BC30002: Type 'Blech' is not defined.
    Public Sub Goo3(x as Blech) 
                         ~~~~~
</expected>)

            CompilationUtils.AssertTheseDiagnostics(compilation.GetSemanticModel(CompilationUtils.GetTree(compilation, "a.vb")).GetDeclarationDiagnostics(),
<expected>
BC30002: Type 'Blech' is not defined.
    Public Sub Goo1(x as Blech) 
                         ~~~~~
</expected>)

            CompilationUtils.AssertTheseDiagnostics(compilation.GetSemanticModel(CompilationUtils.GetTree(compilation, "b.vb")).GetDeclarationDiagnostics(),
<expected>
BC30002: Type 'Blech' is not defined.
    Public Sub Goo2(x as Blech) 
                         ~~~~~
BC30002: Type 'Blech' is not defined.
    Public Sub Goo3(x as Blech) 
                         ~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub CompilationGetDiagnostics()
            ' Check that Compilation.GetDiagnostics and Bindings.GetDiagnostics work as expected.
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Class C1
    Public Sub Goo1(x as Blech) 
    End Sub
    Public Sub Bar1()
        NoSuchMethod("from C1")
    End Sub
End Class

Partial Class C2
    Public Sub Bar2()
        NoSuchMethod("from C2")
    End Sub
End Class
    </file>
    <file name="b.vb">
Partial Class C2
    Public Sub Goo2(x as Blech) 
    End Sub
End Class

Class C3
    Public Sub Goo3(x as Blech) 
    End Sub
    Public Sub Bar3()
        NoSuchMethod("from C3")
    End Sub
End Class
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation.GetDiagnostics(),
<expected>
BC30002: Type 'Blech' is not defined.
    Public Sub Goo1(x as Blech) 
                         ~~~~~
BC30451: 'NoSuchMethod' is not declared. It may be inaccessible due to its protection level.
        NoSuchMethod("from C1")
        ~~~~~~~~~~~~
BC30451: 'NoSuchMethod' is not declared. It may be inaccessible due to its protection level.
        NoSuchMethod("from C2")
        ~~~~~~~~~~~~
BC30002: Type 'Blech' is not defined.
    Public Sub Goo2(x as Blech) 
                         ~~~~~
BC30002: Type 'Blech' is not defined.
    Public Sub Goo3(x as Blech) 
                         ~~~~~
BC30451: 'NoSuchMethod' is not declared. It may be inaccessible due to its protection level.
        NoSuchMethod("from C3")
        ~~~~~~~~~~~~
</expected>)

            CompilationUtils.AssertTheseDiagnostics(compilation.GetSemanticModel(CompilationUtils.GetTree(compilation, "a.vb")).GetDiagnostics(),
<expected>
BC30002: Type 'Blech' is not defined.
    Public Sub Goo1(x as Blech) 
                         ~~~~~
BC30451: 'NoSuchMethod' is not declared. It may be inaccessible due to its protection level.
        NoSuchMethod("from C1")
        ~~~~~~~~~~~~
BC30451: 'NoSuchMethod' is not declared. It may be inaccessible due to its protection level.
        NoSuchMethod("from C2")
        ~~~~~~~~~~~~
</expected>)

            CompilationUtils.AssertTheseDiagnostics(compilation.GetSemanticModel(CompilationUtils.GetTree(compilation, "b.vb")).GetDiagnostics(),
<expected>
BC30002: Type 'Blech' is not defined.
    Public Sub Goo2(x as Blech) 
                         ~~~~~
BC30002: Type 'Blech' is not defined.
    Public Sub Goo3(x as Blech) 
                         ~~~~~
BC30451: 'NoSuchMethod' is not declared. It may be inaccessible due to its protection level.
        NoSuchMethod("from C3")
        ~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub EmitMetadataOnly()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System        

Namespace Goo.Bar
    Public Class X
        Public x As Integer
        Private y As String
        Public Shared Sub SayHello()
            Console.WriteLine("hello")
        End Sub
        Public Sub New()
            x = 7
        End Sub
        Friend Function goo(arg as String) as Integer
            Return x
        End Function
    End Class
End Namespace
    </file>
</compilation>)

            Dim emitResult As EmitResult
            Dim mdOnlyImage As Byte()

            Using output = New MemoryStream()
                emitResult = compilation.Emit(output, options:=New EmitOptions(metadataOnly:=True))
                mdOnlyImage = output.ToArray()
            End Using

            Assert.True(emitResult.Success)
            CompilationUtils.AssertNoErrors(emitResult.Diagnostics)
            Assert.True(mdOnlyImage.Length > 0, "no metadata emitted")

            Dim srcUsing =
<compilation>
    <file name="a.vb">
Imports System
Imports Goo.Bar        
Class M1
    Sub Main()
        X.SayHello()
    End Sub
End Class

    </file>
</compilation>

            Dim usingComp = CreateCompilationWithMscorlib40(srcUsing, references:={MetadataReference.CreateFromImage(mdOnlyImage.AsImmutableOrNull())})

            Using output = New MemoryStream()
                emitResult = usingComp.Emit(output)

                Assert.True(emitResult.Success)
                CompilationUtils.AssertNoErrors(emitResult.Diagnostics)
                Assert.True(output.ToArray().Length > 0, "no metadata emitted")
            End Using
        End Sub

        <Fact>
        Public Sub EmitMetadataOnly_XmlDocs_NoDocMode_Success()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System        

Namespace Goo.Bar
    ''' &lt;summary&gt;This should be emitted&lt;/summary&gt;
    Public Class X
    End Class
End Namespace
    </file>
</compilation>, assemblyName:="test", parseOptions:=VisualBasicParseOptions.Default.WithDocumentationMode(DocumentationMode.None))

            Dim emitResult As EmitResult
            Dim mdOnlyImage As Byte()
            Dim xmlDocBytes As Byte()

            Using output = New MemoryStream()
                Using xmlStream = New MemoryStream()
                    emitResult = compilation.Emit(output, xmlDocumentationStream:=xmlStream, options:=New EmitOptions(metadataOnly:=True))
                    mdOnlyImage = output.ToArray()
                    xmlDocBytes = xmlStream.ToArray()
                End Using
            End Using

            Assert.True(emitResult.Success)
            emitResult.Diagnostics.Verify()

            Assert.True(mdOnlyImage.Length > 0, "no metadata emitted")
            Assert.Equal(
"﻿<?xml version=""1.0""?>
<doc>
<assembly>
<name>
test
</name>
</assembly>
<members>
</members>
</doc>
",
                Encoding.UTF8.GetString(xmlDocBytes))
        End Sub

        <Fact>
        Public Sub EmitMetadataOnly_XmlDocs_NoDocMode_SyntaxWarning()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System        

Namespace Goo.Bar
    ''' &lt;summary&gt;This should still emit
    Public Class X
    End Class
End Namespace
    </file>
</compilation>, assemblyName:="test", parseOptions:=VisualBasicParseOptions.Default.WithDocumentationMode(DocumentationMode.None))

            Dim emitResult As EmitResult
            Dim mdOnlyImage As Byte()
            Dim xmlDocBytes As Byte()

            Using output = New MemoryStream()
                Using xmlStream = New MemoryStream()
                    emitResult = compilation.Emit(output, xmlDocumentationStream:=xmlStream, options:=New EmitOptions(metadataOnly:=True))
                    mdOnlyImage = output.ToArray()
                    xmlDocBytes = xmlStream.ToArray()
                End Using
            End Using

            Assert.True(emitResult.Success)
            emitResult.Diagnostics.Verify()

            Assert.True(mdOnlyImage.Length > 0, "no metadata emitted")
            Assert.Equal(
                "﻿<?xml version=""1.0""?>
<doc>
<assembly>
<name>
test
</name>
</assembly>
<members>
</members>
</doc>
",
                Encoding.UTF8.GetString(xmlDocBytes))
        End Sub

        <Fact>
        Public Sub EmitMetadataOnly_XmlDocs_DiagnoseDocMode_SyntaxWarning()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System        

Namespace Goo.Bar
    ''' &lt;summary&gt;This should still emit
    Public Class X
    End Class
End Namespace
    </file>
</compilation>, assemblyName:="test", parseOptions:=VisualBasicParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose))

            Dim emitResult As EmitResult
            Dim mdOnlyImage As Byte()
            Dim xmlDocBytes As Byte()

            Using output = New MemoryStream()
                Using xmlStream = New MemoryStream()
                    emitResult = compilation.Emit(output, xmlDocumentationStream:=xmlStream, options:=New EmitOptions(metadataOnly:=True))
                    mdOnlyImage = output.ToArray()
                    xmlDocBytes = xmlStream.ToArray()
                End Using
            End Using

            Assert.True(emitResult.Success)
            emitResult.Diagnostics.Verify(
                Diagnostic(ERRID.WRN_XMLDocParseError1, "<summary>").WithArguments("Element is missing an end tag.").WithLocation(4, 9),
                Diagnostic(ERRID.WRN_XMLDocParseError1, "").WithArguments("Expected beginning '<' for an XML tag.").WithLocation(4, 40),
                Diagnostic(ERRID.WRN_XMLDocParseError1, "").WithArguments("'>' expected.").WithLocation(4, 40))

            Assert.True(mdOnlyImage.Length > 0, "no metadata emitted")
            Assert.Equal(
                "﻿<?xml version=""1.0""?>
<doc>
<assembly>
<name>
test
</name>
</assembly>
<members>
</members>
</doc>
",
                Encoding.UTF8.GetString(xmlDocBytes))
        End Sub

        <Fact>
        Public Sub EmitMetadataOnly_XmlDocs_DiagnoseDocMode_SemanticWarning()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System        

Namespace Goo.Bar
    ''' &lt;summary&gt;&lt;see cref="T"/&gt;&lt;/summary&gt;
    Public Class X
    End Class
End Namespace
    </file>
</compilation>, assemblyName:="test", parseOptions:=VisualBasicParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose))

            Dim emitResult As EmitResult
            Dim mdOnlyImage As Byte()
            Dim xmlDocBytes As Byte()

            Using output = New MemoryStream()
                Using xmlStream = New MemoryStream()
                    emitResult = compilation.Emit(output, xmlDocumentationStream:=xmlStream, options:=New EmitOptions(metadataOnly:=True))
                    mdOnlyImage = output.ToArray()
                    xmlDocBytes = xmlStream.ToArray()
                End Using
            End Using

            Assert.True(emitResult.Success)
            emitResult.Diagnostics.Verify(
                Diagnostic(ERRID.WRN_XMLDocCrefAttributeNotFound1, "cref=""T""").WithArguments("T").WithLocation(4, 23))

            Assert.True(mdOnlyImage.Length > 0, "no metadata emitted")
            Assert.Equal(
                "﻿<?xml version=""1.0""?>
<doc>
<assembly>
<name>
test
</name>
</assembly>
<members>
<member name=""T:Goo.Bar.X"">
 <summary><see cref=""!:T""/></summary>
</member>
</members>
</doc>
",
                Encoding.UTF8.GetString(xmlDocBytes))
        End Sub

        <Fact>
        Public Sub EmitMetadataOnly_XmlDocs_DiagnoseDocMode_Success()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System        

Namespace Goo.Bar
    ''' &lt;summary&gt;This should emit&lt;/summary&gt;
    Public Class X
    End Class
End Namespace
    </file>
</compilation>, assemblyName:="test", parseOptions:=VisualBasicParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose))

            Dim emitResult As EmitResult
            Dim mdOnlyImage As Byte()
            Dim xmlDocBytes As Byte()

            Using output = New MemoryStream()
                Using xmlStream = New MemoryStream()
                    emitResult = compilation.Emit(output, xmlDocumentationStream:=xmlStream, options:=New EmitOptions(metadataOnly:=True))
                    mdOnlyImage = output.ToArray()
                    xmlDocBytes = xmlStream.ToArray()
                End Using
            End Using

            Assert.True(emitResult.Success)
            emitResult.Diagnostics.Verify()

            Assert.True(mdOnlyImage.Length > 0, "no metadata emitted")
            Assert.Equal(
                "﻿<?xml version=""1.0""?>
<doc>
<assembly>
<name>
test
</name>
</assembly>
<members>
<member name=""T:Goo.Bar.X"">
 <summary>This should emit</summary>
</member>
</members>
</doc>
",
                Encoding.UTF8.GetString(xmlDocBytes))
        End Sub

        <Fact>
        Public Sub EmitMetadataOnly_XmlDocs_ParseDocMode_Success()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System        

Namespace Goo.Bar
    ''' &lt;summary&gt;This should emit&lt;/summary&gt;
    Public Class X
    End Class
End Namespace
    </file>
</compilation>, assemblyName:="test", parseOptions:=VisualBasicParseOptions.Default.WithDocumentationMode(DocumentationMode.Parse))

            Dim emitResult As EmitResult
            Dim mdOnlyImage As Byte()
            Dim xmlDocBytes As Byte()

            Using output = New MemoryStream()
                Using xmlStream = New MemoryStream()
                    emitResult = compilation.Emit(output, xmlDocumentationStream:=xmlStream, options:=New EmitOptions(metadataOnly:=True))
                    mdOnlyImage = output.ToArray()
                    xmlDocBytes = xmlStream.ToArray()
                End Using
            End Using

            Assert.True(emitResult.Success)
            emitResult.Diagnostics.Verify()

            Assert.True(mdOnlyImage.Length > 0, "no metadata emitted")
            Assert.Equal(
                "﻿<?xml version=""1.0""?>
<doc>
<assembly>
<name>
test
</name>
</assembly>
<members>
<member name=""T:Goo.Bar.X"">
 <summary>This should emit</summary>
</member>
</members>
</doc>
",
                Encoding.UTF8.GetString(xmlDocBytes))
        End Sub

        <Fact>
        Private Sub EmitRefAssembly_PrivatePropertyGetter()
            Dim source As String = "
Public Class C
    Property P As Integer
        Private Get
            Return 0
        End Get
        Set
        End Set
    End Property
End Class"

            Dim comp = CreateCompilationWithMscorlib40(source, options:=TestOptions.DebugDll.WithDeterministic(True))

            Using output As New MemoryStream()
                Using metadataOutput As New MemoryStream()
                    Dim emitResult = comp.Emit(output, metadataPEStream:=metadataOutput,
                                               options:=EmitOptions.Default.WithIncludePrivateMembers(False))
                    Assert.True(emitResult.Success)
                    emitResult.Diagnostics.Verify()

                    VerifyMethod(output, {"Sub C..ctor()", "Function C.get_P() As System.Int32", "Sub C.set_P(Value As System.Int32)", "Property C.P As System.Int32"})
                    VerifyMethod(metadataOutput, {"Sub C..ctor()", "Sub C.set_P(Value As System.Int32)", "WriteOnly Property C.P As System.Int32"})
                End Using
            End Using
        End Sub

        <Fact>
        Private Sub EmitRefAssembly_PrivatePropertySetter()
            Dim source As String = "
Public Class C
    Property P As Integer
        Get
            Return 0
        End Get
        Private Set
        End Set
    End Property
End Class"

            Dim comp = CreateCompilationWithMscorlib40(source, options:=TestOptions.DebugDll.WithDeterministic(True))

            Using output As New MemoryStream()
                Using metadataOutput As New MemoryStream()
                    Dim emitResult = comp.Emit(output, metadataPEStream:=metadataOutput,
                                               options:=EmitOptions.Default.WithIncludePrivateMembers(False))
                    Assert.True(emitResult.Success)
                    emitResult.Diagnostics.Verify()

                    VerifyMethod(output, {"Sub C..ctor()", "Function C.get_P() As System.Int32", "Sub C.set_P(Value As System.Int32)", "Property C.P As System.Int32"})
                    VerifyMethod(metadataOutput, {"Sub C..ctor()", "Function C.get_P() As System.Int32", "ReadOnly Property C.P As System.Int32"})
                End Using
            End Using
        End Sub

        Private Shared Sub VerifyMethod(stream As MemoryStream, expectedMethods As String())
            stream.Position = 0
            Dim metadataRef = AssemblyMetadata.CreateFromImage(stream.ToArray()).GetReference()

            Dim compWithMetadata = CreateEmptyCompilation("", references:={MscorlibRef, metadataRef},
                                                     options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))

            AssertEx.Equal(expectedMethods,
                           compWithMetadata.GetMember(Of NamedTypeSymbol)("C").GetMembers().Select(Function(m) m.ToTestDisplayString()))
        End Sub

        <Fact>
        Public Sub RefAssembly_HasReferenceAssemblyAttribute()
            Dim emitRefAssembly = EmitOptions.Default.WithEmitMetadataOnly(True).WithIncludePrivateMembers(False)

            Dim assemblyValidator As Action(Of PEAssembly) =
                Sub(assembly)
                    Dim reader = assembly.GetMetadataReader()
                    Dim attributes = reader.GetAssemblyDefinition().GetCustomAttributes()
                    AssertEx.Equal(
                        {"MemberReference:Void System.Runtime.CompilerServices.CompilationRelaxationsAttribute..ctor(Int32)",
                            "MemberReference:Void System.Runtime.CompilerServices.RuntimeCompatibilityAttribute..ctor()",
                            "MemberReference:Void System.Diagnostics.DebuggableAttribute..ctor(DebuggingModes)",
                            "MemberReference:Void System.Runtime.CompilerServices.ReferenceAssemblyAttribute..ctor()"
                        },
                        attributes.Select(Function(a) MetadataReaderUtils.Dump(reader, reader.GetCustomAttribute(a).Constructor)))
                End Sub

            Dim source = <compilation>
                             <file name="a.vb"></file>
                         </compilation>
            CompileAndVerify(source, emitOptions:=emitRefAssembly, verify:=Verification.Passes, validator:=assemblyValidator)
        End Sub

        <Fact>
        Public Sub RefAssembly_HandlesMissingReferenceAssemblyAttribute()
            Dim emitRefAssembly = EmitOptions.Default.WithEmitMetadataOnly(True).WithIncludePrivateMembers(False)

            Dim assemblyValidator As Action(Of PEAssembly) =
                Sub(assembly)
                    Dim reader = assembly.GetMetadataReader()
                    Dim attributes = reader.GetAssemblyDefinition().GetCustomAttributes()
                    AssertEx.Empty(attributes.Select(Function(a) MetadataReaderUtils.Dump(reader, reader.GetCustomAttribute(a).Constructor)))
                End Sub

            Dim comp = CreateEmptyCompilation({Parse("")})
            comp.MakeMemberMissing(WellKnownMember.System_Runtime_CompilerServices_ReferenceAssemblyAttribute__ctor)
            ' ILVerify: Failed to load type 'System.String' from assembly ...
            CompileAndVerify(comp, emitOptions:=emitRefAssembly, verify:=Verification.FailsILVerify, validator:=assemblyValidator)
        End Sub

        <Fact>
        Public Sub RefAssembly_ReferenceAssemblyAttributeAlsoInSource()
            Dim emitRefAssembly = EmitOptions.Default.WithEmitMetadataOnly(True).WithIncludePrivateMembers(False)

            Dim assemblyValidator As Action(Of PEAssembly) =
                Sub(assembly)
                    Dim reader = assembly.GetMetadataReader()
                    Dim attributes = reader.GetAssemblyDefinition().GetCustomAttributes()
                    AssertEx.Equal(
                        {"MemberReference:Void System.Runtime.CompilerServices.CompilationRelaxationsAttribute..ctor(Int32)",
                            "MemberReference:Void System.Runtime.CompilerServices.RuntimeCompatibilityAttribute..ctor()",
                            "MemberReference:Void System.Diagnostics.DebuggableAttribute..ctor(DebuggingModes)",
                            "MemberReference:Void System.Runtime.CompilerServices.ReferenceAssemblyAttribute..ctor()"
                        },
                        attributes.Select(Function(a) MetadataReaderUtils.Dump(reader, reader.GetCustomAttribute(a).Constructor)))
                End Sub

            Dim source = <compilation>
                             <file name="a.vb"><![CDATA[
<assembly:System.Runtime.CompilerServices.ReferenceAssembly()>
                         ]]></file>
                         </compilation>
            CompileAndVerify(source, emitOptions:=emitRefAssembly, verify:=Verification.Passes, validator:=assemblyValidator)
        End Sub

        <Fact>
        Public Sub RefAssembly_InvariantToSomeChanges()
            Dim sourceTemplate As String = "
Public Class C
    CHANGE
End Class"

            CompareAssemblies(sourceTemplate,
"Public Function M() As Integer
    Return 1
End Function",
"Public Function M() As Integer
    Return 2
End Function", Match.BothMetadataAndRefOut)

            CompareAssemblies(sourceTemplate,
"Public Function M() As Integer
    Return 1
End Function",
"Public Function M() As Integer
    Return Bad()
End Function", Match.BothMetadataAndRefOut)

            CompareAssemblies(sourceTemplate,
"Private Sub M()
End Sub",
"", Match.RefOut)

            CompareAssemblies(sourceTemplate,
"Friend Sub M()
End Sub",
"", Match.RefOut)

            CompareAssemblies(sourceTemplate,
"Private Protected Sub M()
End Sub",
"", Match.RefOut)

            CompareAssemblies(sourceTemplate,
"Private Sub M()
Dim product = New With {Key .Id = 1}
End Sub",
"", Match.RefOut)

            CompareAssemblies(sourceTemplate,
"Private Property P As Integer
    Get
        Bad()
    End Get
    Set
        Bad()
    End Set
End Property",
"", Match.RefOut) ' Errors in method bodies don't matter

            CompareAssemblies(sourceTemplate,
"Public Property P As Integer",
"", Match.Different)

            CompareAssemblies(sourceTemplate,
"Protected Property P As Integer",
"", Match.Different)

            CompareAssemblies(sourceTemplate,
"Private Property P As Integer",
"", Match.RefOut) ' Private auto-property and underlying field are removed

            CompareAssemblies(sourceTemplate,
"Friend Property P As Integer",
"", Match.RefOut)

            CompareAssemblies(sourceTemplate,
"Private Event DoSomething()",
"", Match.Different) ' VB events add nested types (C.DoSomethingEventHandler in this case)

            CompareAssemblies(sourceTemplate,
"Friend Event DoSomething()",
"", Match.Different)

            CompareAssemblies(sourceTemplate,
"Private Class C2
End Class",
"", Match.Different) ' All types are included

            CompareAssemblies(sourceTemplate,
"Private Structure S
End Structure",
"", Match.Different)

            CompareAssemblies(sourceTemplate,
"Public Structure S
    Private Dim i As Integer
End Structure",
"Public Structure S
End Structure", Match.Different)

            CompareAssemblies(sourceTemplate,
"Private Dim i As Integer",
"", Match.RefOut)

            CompareAssemblies(sourceTemplate,
"Public Sub New()
End Sub",
"", Match.BothMetadataAndRefOut)

            CompareAssemblies(sourceTemplate,
"Public Function NoBody() As Integer
End Function",
"Public Function NoBody() As Integer
    Return 1
End Function", Match.BothMetadataAndRefOut)

        End Sub

        <Fact()>
        Public Sub RefAssemblyNoPia()
            Dim piaSource = <compilation name="Pia"><file name="a.vb"><![CDATA[
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
<Assembly: ImportedFromTypeLib("Pia1.dll")>

Public Structure S
    Public Dim field As Integer
End Structure
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58280")>
Public Interface ITest1
    Function M() As S
End Interface
]]></file></compilation>
            Dim pia = CreateCompilationWithMscorlib40(piaSource)
            CompileAndVerify(pia)
            Dim source = <compilation name="LocalTypes2"><file name="a.vb"><![CDATA[
Public Class D
    Implements ITest1

    Function M() As S Implements ITest1.M
        Throw New System.Exception()
    End Function
End Class
]]></file></compilation>

            Dim piaImageReference = pia.EmitToImageReference(embedInteropTypes:=True)
            RefAssemblyNoPia_VerifyRefOnly(source, piaImageReference)
            RefAssemblyNoPia_VerifyRefOut(source, piaImageReference)

            Dim piaMetadataReference = pia.ToMetadataReference(embedInteropTypes:=True)
            RefAssemblyNoPia_VerifyRefOnly(source, piaMetadataReference)
            RefAssemblyNoPia_VerifyRefOut(source, piaMetadataReference)
        End Sub

        Private Sub RefAssemblyNoPia_VerifyRefOnly(source As Xml.Linq.XElement, reference As MetadataReference)
            Dim comp = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseDll, references:={reference})
            Dim refOnlyImage = EmitRefOnly(comp)
            RefAssemblyNoPia_VerifyNoPia(refOnlyImage)
        End Sub

        Private Sub RefAssemblyNoPia_VerifyRefOut(source As Xml.Linq.XElement, reference As MetadataReference)
            Dim comp = CreateCompilationWithMscorlib40(source, options:=TestOptions.DebugDll, references:={reference})
            Dim pair = EmitRefOut(comp)
            RefAssemblyNoPia_VerifyNoPia(pair.image)
            RefAssemblyNoPia_VerifyNoPia(pair.refImage)
        End Sub

        Private Sub RefAssemblyNoPia_VerifyNoPia(image As ImmutableArray(Of Byte))
            Dim reference = CompilationVerifier.LoadTestEmittedExecutableForSymbolValidation(image, OutputKind.DynamicallyLinkedLibrary)
            Dim comp = CreateCompilationWithMscorlib40("", references:={reference})
            Dim referencedAssembly = comp.GetReferencedAssemblySymbol(reference)
            Dim [module] = DirectCast(referencedAssembly.Modules(0), PEModuleSymbol)

            Dim itest1 = [module].GlobalNamespace.GetMember(Of NamedTypeSymbol)("ITest1")
            Assert.NotNull(itest1.GetAttributes().Where(Function(a) a.IsTargetAttribute("System.Runtime.InteropServices", "TypeIdentifierAttribute")).Single())

            Dim method = DirectCast(itest1.GetMember("M"), PEMethodSymbol)
            Assert.Equal("Function ITest1.M() As S", method.ToTestDisplayString())

            Dim s = DirectCast(method.ReturnType, NamedTypeSymbol)
            Assert.Equal("S", s.ToTestDisplayString())
            Assert.NotNull(s.GetAttributes().Where(Function(a) a.IsTargetAttribute("System.Runtime.InteropServices", "TypeIdentifierAttribute")).Single())

            Dim field = s.GetMember("field")
            Assert.Equal("S.field As System.Int32", field.ToTestDisplayString())
        End Sub

        <Fact()>
        Public Sub RefAssemblyNoPiaReferenceFromMethodBody()

            Dim piaSource = <compilation name="Pia"><file name="a.vb"><![CDATA[
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
<Assembly: ImportedFromTypeLib("Pia1.dll")>

Public Structure S
    Public Dim field As Integer
End Structure
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58280")>
Public Interface ITest1
    Function M() As S
End Interface
]]></file></compilation>
            Dim pia = CreateCompilationWithMscorlib40(piaSource)
            CompileAndVerify(pia)
            Dim source = <compilation name="LocalTypes2"><file name="a.vb"><![CDATA[
Public Class D
    Sub M2()
        Dim x As ITest1 = Nothing
        Dim s As S = x.M()
    End Sub
End Class
]]></file></compilation>

            Dim piaImageReference = pia.EmitToImageReference(embedInteropTypes:=True)
            RefAssemblyNoPiaReferenceFromMethodBody_VerifyRefOnly(source, piaImageReference)
            RefAssemblyNoPiaReferenceFromMethodBody_VerifyRefOut(source, piaImageReference)

            Dim piaMetadataReference = pia.ToMetadataReference(embedInteropTypes:=True)
            RefAssemblyNoPiaReferenceFromMethodBody_VerifyRefOnly(source, piaMetadataReference)
            RefAssemblyNoPiaReferenceFromMethodBody_VerifyRefOut(source, piaMetadataReference)
        End Sub

        Private Sub RefAssemblyNoPiaReferenceFromMethodBody_VerifyRefOnly(source As Xml.Linq.XElement, reference As MetadataReference)
            Dim comp = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseDll, references:={reference})
            Dim refOnlyImage = EmitRefOnly(comp)
            RefAssemblyNoPiaReferenceFromMethodBody_VerifyNoPia(refOnlyImage, expectMissing:=True)
        End Sub

        Private Sub RefAssemblyNoPiaReferenceFromMethodBody_VerifyRefOut(source As Xml.Linq.XElement, reference As MetadataReference)
            Dim comp = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseDll, references:={reference})
            Dim pair = EmitRefOut(comp)
            RefAssemblyNoPiaReferenceFromMethodBody_VerifyNoPia(pair.image, expectMissing:=False)
            RefAssemblyNoPiaReferenceFromMethodBody_VerifyNoPia(pair.refImage, expectMissing:=False)
        End Sub

        ' The ref assembly produced by refout has more types than that produced by refonly,
        ' because refout will bind the method bodies (and therefore populate more referenced types).
        ' This will be refined in the future. Follow-up issue: https://github.com/dotnet/roslyn/issues/19403
        Private Sub RefAssemblyNoPiaReferenceFromMethodBody_VerifyNoPia(image As ImmutableArray(Of Byte), expectMissing As Boolean)
            Dim reference = CompilationVerifier.LoadTestEmittedExecutableForSymbolValidation(image, OutputKind.DynamicallyLinkedLibrary)
            Dim comp = CreateCompilationWithMscorlib40("", references:={reference})
            Dim referencedAssembly = comp.GetReferencedAssemblySymbol(reference)
            Dim [module] = DirectCast(referencedAssembly.Modules(0), PEModuleSymbol)

            Dim itest1Array = [module].GlobalNamespace.GetMembers("ITest1")
            If expectMissing Then
                Assert.Empty(itest1Array)
                Assert.Empty([module].GlobalNamespace.GetMembers("S"))
                Return
            End If

            Dim itest1 = DirectCast(itest1Array.Single(), PENamedTypeSymbol)
            Assert.NotNull(itest1.GetAttributes().Where(Function(a) a.IsTargetAttribute("System.Runtime.InteropServices", "TypeIdentifierAttribute")).Single())

            Dim method = DirectCast(itest1.GetMembers("M").Single(), PEMethodSymbol)
            Assert.Equal("Function ITest1.M() As S", method.ToTestDisplayString())

            Dim s = DirectCast(method.ReturnType, NamedTypeSymbol)
            Assert.Equal("S", s.ToTestDisplayString())
            Assert.NotNull(s.GetAttributes().Where(Function(a) a.IsTargetAttribute("System.Runtime.InteropServices", "TypeIdentifierAttribute")).Single())

            Dim field = s.GetMember("field")
            Assert.Equal("S.field As System.Int32", field.ToTestDisplayString())
        End Sub

        Private Shared Function EmitRefOut(comp As VisualBasicCompilation) As (image As ImmutableArray(Of Byte), refImage As ImmutableArray(Of Byte))
            Using output = New MemoryStream()
                Using metadataOutput = New MemoryStream()
                    Dim options = EmitOptions.Default.WithIncludePrivateMembers(False)
                    comp.VerifyEmitDiagnostics()
                    Dim result = comp.Emit(output, metadataPEStream:=metadataOutput, options:=options)
                    Return (output.ToImmutable(), metadataOutput.ToImmutable())
                End Using
            End Using
        End Function

        Private Shared Function EmitRefOnly(comp As VisualBasicCompilation) As ImmutableArray(Of Byte)
            Using output = New MemoryStream()
                Dim options = EmitOptions.Default.WithEmitMetadataOnly(True).WithIncludePrivateMembers(False)
                comp.VerifyEmitDiagnostics()
                Dim result = comp.Emit(output, options:=options)
                Return output.ToImmutable()
            End Using
        End Function

        <Fact>
        Public Sub RefAssembly_InvariantToSomeChangesWithInternalsVisibleTo_01()
            Dim sourceTemplate As String = "
Imports System.Runtime.CompilerServices
<assembly:InternalsVisibleToAttribute(""Friend"")>
Public Class C
    CHANGE
End Class"

            CompareAssemblies(sourceTemplate,
"Friend Function M() As Integer
End Function",
"", Match.Different)

        End Sub

        <Fact>
        Public Sub RefAssembly_InvariantToSomeChangesWithInternalsVisibleTo_02()
            Dim sourceTemplate As String = "
Imports System.Runtime.CompilerServices
<assembly:InternalsVisibleToAttribute(""Friend"")>
Public Class C
    CHANGE
End Class"

            CompareAssemblies(sourceTemplate,
"Private Protected Function M() As Integer
End Function",
"", Match.Different)

        End Sub

        Private Sub CompareAssemblies(sourceTemplate As String, left As String, right As String, expectedMatch As Match)
            CompareAssemblies(sourceTemplate, left, right, expectedMatch, includePrivateMembers:=True)
            CompareAssemblies(sourceTemplate, left, right, expectedMatch, includePrivateMembers:=False)
        End Sub

        Public Enum Match
            BothMetadataAndRefOut
            RefOut
            Different
        End Enum

        Private Sub CompareAssemblies(sourceTemplate As String, change1 As String, change2 As String, expectedMatch As Match, includePrivateMembers As Boolean)
            Dim expectMatch = If(includePrivateMembers,
                expectedMatch = Match.BothMetadataAndRefOut,
                expectedMatch = Match.BothMetadataAndRefOut OrElse expectedMatch = Match.RefOut)

            Dim name As String = GetUniqueName()
            Dim source1 As String = sourceTemplate.Replace("CHANGE", change1)
            Dim comp1 = CreateCompilationWithMscorlib40(source1,
                options:=TestOptions.DebugDll.WithDeterministic(True), assemblyName:=name, parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest))
            Dim image1 As ImmutableArray(Of Byte) = comp1.EmitToArray(EmitOptions.Default.WithEmitMetadataOnly(True).WithIncludePrivateMembers(includePrivateMembers))

            Dim source2 = sourceTemplate.Replace("CHANGE", change2)
            Dim comp2 = CreateCompilationWithMscorlib40(source2,
                            options:=TestOptions.DebugDll.WithDeterministic(True), assemblyName:=name, parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest))
            Dim image2 As ImmutableArray(Of Byte) = comp2.EmitToArray(EmitOptions.Default.WithEmitMetadataOnly(True).WithIncludePrivateMembers(includePrivateMembers))

            If expectMatch Then
                AssertEx.Equal(image1, image2)
            Else
                AssertEx.NotEqual(image1, image2)
            End If
        End Sub

#If NET472 Then
        <ConditionalFact(GetType(WindowsDesktopOnly))>
        <WorkItem(31197, "https://github.com/dotnet/roslyn/issues/31197")>
        Public Sub RefAssembly_InvariantToResourceChanges_RefOut()
            Dim arrayOfEmbeddedData1 = New Byte() {1, 2, 3, 4, 5}
            Dim arrayOfEmbeddedData2 = New Byte() {1, 2, 3, 4, 5, 6}

            Dim manifestResources1 As IEnumerable(Of ResourceDescription) =
                {New ResourceDescription(resourceName:="A", fileName:="x.goo", Function() New MemoryStream(arrayOfEmbeddedData1), isPublic:=True)}
            Dim manifestResources2 As IEnumerable(Of ResourceDescription) =
                {New ResourceDescription(resourceName:="A", fileName:="x.goo", Function() New MemoryStream(arrayOfEmbeddedData2), isPublic:=True)}
            RefAssembly_InvariantToResourceChanges_RefOut_verify(manifestResources1, manifestResources2)

            manifestResources1 = {New ResourceDescription(resourceName:="A", Function() New MemoryStream(arrayOfEmbeddedData1), isPublic:=True)}
            manifestResources2 = {New ResourceDescription(resourceName:="A", Function() New MemoryStream(arrayOfEmbeddedData2), isPublic:=True)}
            RefAssembly_InvariantToResourceChanges_RefOut_verify(manifestResources1, manifestResources2)
        End Sub

        Private Function RefAssembly_InvariantToResourceChanges_RefOut_emit(ByVal manifestResources As IEnumerable(Of ResourceDescription), ByVal name As String) _
            As (peStream As ImmutableArray(Of Byte), metadataPEStream As ImmutableArray(Of Byte))

            Dim source = Parse("")
            Dim comp = CreateCompilation(source, options:=TestOptions.DebugDll.WithDeterministic(True), assemblyName:=name)
            comp.VerifyDiagnostics()

            Dim metadataPEStream = New MemoryStream()
            Dim refoutOptions = EmitOptions.[Default].WithEmitMetadataOnly(False).WithIncludePrivateMembers(False)
            Dim peStream = comp.EmitToArray(refoutOptions, metadataPEStream:=metadataPEStream, manifestResources:=manifestResources)

            Return (peStream, metadataPEStream.ToImmutable())
        End Function

        Private Sub RefAssembly_InvariantToResourceChanges_RefOut_verify(ByVal manifestResources1 As IEnumerable(Of ResourceDescription), ByVal manifestResources2 As IEnumerable(Of ResourceDescription))
            Dim name As String = GetUniqueName()
            Dim emitted1 = RefAssembly_InvariantToResourceChanges_RefOut_emit(manifestResources1, name)
            Dim emitted2 = RefAssembly_InvariantToResourceChanges_RefOut_emit(manifestResources2, name)

            AssertEx.NotEqual(emitted1.peStream, emitted2.peStream, message:="Expecting different main assemblies produced by refout")
            AssertEx.Equal(emitted1.metadataPEStream, emitted2.metadataPEStream, message:="Expecting identical ref assemblies produced by refout")

            Dim refAssembly1 = Assembly.ReflectionOnlyLoad(emitted1.metadataPEStream.ToArray())
            Assert.DoesNotContain("A", refAssembly1.GetManifestResourceNames())
        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly))>
        <WorkItem(31197, "https://github.com/dotnet/roslyn/issues/31197")>
        Public Sub RefAssembly_SensitiveToResourceChanges_RefOnly()
            Dim arrayOfEmbeddedData1 = New Byte() {1, 2, 3, 4, 5}
            Dim arrayOfEmbeddedData2 = New Byte() {1, 2, 3, 4, 5, 6}

            Dim manifestResources1 As IEnumerable(Of ResourceDescription) =
                {New ResourceDescription(resourceName:="A", fileName:="x.goo", Function() New MemoryStream(arrayOfEmbeddedData1), isPublic:=True)}
            Dim manifestResources2 As IEnumerable(Of ResourceDescription) =
                {New ResourceDescription(resourceName:="A", fileName:="x.goo", Function() New MemoryStream(arrayOfEmbeddedData2), isPublic:=True)}

            Dim name As String = GetUniqueName()
            Dim image1 = RefAssembly_SensitiveToResourceChanges_RefOnly_emit(manifestResources1, name)
            Dim image2 = RefAssembly_SensitiveToResourceChanges_RefOnly_emit(manifestResources2, name)
            AssertEx.Equal(image1, image2, message:="Expecting different ref assembly produced by refonly")

            Dim refAssembly1 = Assembly.ReflectionOnlyLoad(image1.ToArray())
            Assert.DoesNotContain("A", refAssembly1.GetManifestResourceNames())
        End Sub

        Private Function RefAssembly_SensitiveToResourceChanges_RefOnly_emit(ByVal manifestResources As IEnumerable(Of ResourceDescription), name As String) _
            As ImmutableArray(Of Byte)

            Dim source = Parse("")
            Dim comp = CreateCompilation(source, options:=TestOptions.DebugDll.WithDeterministic(True), assemblyName:=name)
            comp.VerifyDiagnostics()

            Dim refonlyOptions = EmitOptions.[Default].WithEmitMetadataOnly(True).WithIncludePrivateMembers(False)
            Return comp.EmitToArray(refonlyOptions, metadataPEStream:=Nothing, manifestResources:=manifestResources)
        End Function
#End If
        <Fact, WorkItem(31197, "https://github.com/dotnet/roslyn/issues/31197")>
        Public Sub RefAssembly_CryptoHashFailedIsOnlyReportedOnce()
            Dim hash_resources =
                {New ResourceDescription("hash_resource", "snKey.snk", Function() New MemoryStream(TestResources.General.snKey, writable:=False), True)}

            Dim moduleComp =
                CreateEmptyCompilation("", options:=TestOptions.DebugDll.WithDeterministic(True).WithOutputKind(OutputKind.NetModule))

            Dim reference = ModuleMetadata.CreateFromImage(moduleComp.EmitToArray()).GetReference()

            Dim compilation = CreateCompilation("
<assembly: System.Reflection.AssemblyAlgorithmIdAttribute(12345ui)>

Class Program
End Class
", references:={reference}, options:=TestOptions.ReleaseDll)

            Dim refonlyOptions = EmitOptions.[Default].WithEmitMetadataOnly(True).WithIncludePrivateMembers(False)
            Dim refonlyDiagnostics = compilation.Emit(New MemoryStream(), pdbStream:=Nothing,
                                                      options:=refonlyOptions, manifestResources:=hash_resources).Diagnostics

            refonlyDiagnostics.Verify(Diagnostic(ERRID.ERR_CryptoHashFailed))

            Dim refoutOptions = EmitOptions.[Default].WithEmitMetadataOnly(False).WithIncludePrivateMembers(False)
            Dim refoutDiagnostics = compilation.Emit(peStream:=New MemoryStream(), metadataPEStream:=New MemoryStream(),
                                                     pdbStream:=Nothing, options:=refoutOptions, manifestResources:=hash_resources).Diagnostics

            refoutDiagnostics.Verify(Diagnostic(ERRID.ERR_CryptoHashFailed))
        End Sub

        <Fact>
        Public Sub RefAssemblyClient_EmitAllNestedTypes()
            VerifyRefAssemblyClient("
Public Interface I1(Of T)
End Interface
Public Interface I2
End Interface
Public Class A
    Implements I1(Of A.X)

    Private Class X
        Implements I2

    End Class
End Class
", "
Public Class C
    Public Function M(a As A) As I1(Of I2)
        Return DirectCast(a, I1(Of I2))
    End Function
End Class
",
Sub(comp) comp.AssertNoDiagnostics())
        End Sub

        <Fact>
        Public Sub RefAssemblyClient_ExplicitPropertyImplementation()
            VerifyRefAssemblyClient("
Public Interface I
    Property P As Integer
End Interface
Public Class C
    Implements I

    Private Property P As Integer Implements I.P
        Get
            Throw New System.Exception()
        End Get
        Set
            Throw New System.Exception()
        End Set
    End Property
End Class
", "
Public Class D
    Inherits C
    Implements I
End Class
",
Sub(comp) comp.AssertNoDiagnostics())
        End Sub

        <Fact>
        Public Sub RefAssemblyClient_EmitAllTypes()
            VerifyRefAssemblyClient("
Public Interface I1(Of T)
End Interface
Public Interface I2
End Interface
Public Class A
    Implements I1(Of X)
End Class
Friend Class X
    Implements I2
End Class
", "
Public Class C
    Public Function M(a As A) As I1(Of I2)
        Return DirectCast(a, I1(Of I2))
    End Function
End Class
",
Sub(comp) comp.AssertNoDiagnostics())
        End Sub

        <Fact>
        Public Sub RefAssemblyClient_StructWithPrivateGenericField()
            VerifyRefAssemblyClient("
Public Structure Container(Of T)
    Private Dim contained As T
End Structure
", "
Public Structure Usage
    Public Dim x As Container(Of Usage)
End Structure
",
Sub(comp)
    comp.AssertTheseDiagnostics(<errors>
BC30294: Structure 'Usage' cannot contain an instance of itself: 
    'Usage' contains 'Container(Of Usage)' (variable 'x').
    'Container(Of Usage)' contains 'Usage' (variable 'contained').
    Public Dim x As Container(Of Usage)
               ~
                                </errors>)
End Sub)
        End Sub

        <Fact>
        Public Sub RefAssemblyClient_EmitAllVirtualMethods()
            Dim comp1 = CreateVisualBasicCompilation("
<assembly:System.Runtime.CompilerServices.InternalsVisibleTo(""VB2"")>
<assembly:System.Runtime.CompilerServices.InternalsVisibleTo(""VB3"")>
Public MustInherit Class C1
    Friend Sub M()
    End Sub
End Class
", assemblyName:="VB1", referencedAssemblies:={MscorlibRef})
            comp1.AssertTheseDiagnostics()
            Dim image1 = comp1.EmitToImageReference(EmitOptions.Default)

            Dim comp2 = CreateVisualBasicCompilation("
Public MustInherit Class C2
    Inherits C1

    Friend Overloads Sub M()
    End Sub
End Class
", assemblyName:="VB2", referencedAssemblies:={MscorlibRef, image1})
            comp2.AssertTheseDiagnostics()
            Dim image2 = comp2.EmitToImageReference(EmitOptions.Default.WithEmitMetadataOnly(True).WithIncludePrivateMembers(False))

            ' If internal virtual methods were not included in ref assemblies, then C3 could not be concrete

            Dim comp3 = CreateVisualBasicCompilation("
Public Class C3
    Inherits C2
End Class
", assemblyName:="VB3", referencedAssemblies:={MscorlibRef, image1, image2})
            comp3.AssertTheseDiagnostics()

        End Sub

        <Fact>
        Public Sub RefAssemblyClient_StructWithPrivateIntField()
            VerifyRefAssemblyClient("
Public Structure S
    Private Dim i As Integer
End Structure
", "
Public Class C
    Public Function M() As String
        Dim s As S
        Return s.ToString()
    End Function
End Class
",
Sub(comp) comp.AssertTheseDiagnostics())
        End Sub

        <Fact, WorkItem(49470, "https://github.com/dotnet/roslyn/issues/49470")>
        Public Sub RefAssemblyClient_EventBackingField()
            Dim lib_vb = "
Imports System

Public Class Button
    Public Event Click As EventHandler
End Class

Public Class Base
    Protected WithEvents Button1 As Button
End Class
"
            Dim source_vb = "
Imports System

Public Class Derived
    Inherits Base

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
    End Sub
End Class
"
            Dim libComp = CreateCompilationWithMscorlib40({Parse(lib_vb)}, options:=TestOptions.DebugDll.WithDeterministic(True))

            Assert.Equal({"Sub Base..ctor()",
                         "Base._Button1 As Button",
                         "Function Base.get_Button1() As Button",
                         "Sub Base.set_Button1(WithEventsValue As Button)",
                         "WithEvents Base.Button1 As Button"},
                         libComp.GlobalNamespace.GetTypeMember("Base").GetMembers().Select(Function(m) m.ToTestDisplayString()))

            Dim options = EmitOptions.Default.WithEmitMetadataOnly(True).WithIncludePrivateMembers(False)
            Dim libImage = libComp.EmitToImageReference(options)

            Dim comp = CreateCompilationWithMscorlib40(source_vb, references:={libImage})
            AssertNoDiagnostics(comp)

            ' The logic in PENamedTypeSymbol.CreateFields chooses not to import the field, but its presence still matters.
            Assert.Equal({"Sub Base..ctor()",
                         "Function Base.get_Button1() As Button",
                         "Sub Base.set_Button1(WithEventsValue As Button)",
                         "WithEvents Base.Button1 As Button"},
                         comp.GlobalNamespace.GetTypeMember("Base").GetMembers().Select(Function(m) m.ToTestDisplayString()))
        End Sub

        Private Sub VerifyRefAssemblyClient(lib_vb As String, client_vb As String, validator As Action(Of VisualBasicCompilation), Optional debugFlag As Integer = -1)
            ' Whether the library is compiled in full, as metadata-only, or as a ref assembly should be transparent
            ' to the client and the validator should be able to verify the same expectations.

            If debugFlag = -1 OrElse debugFlag = 0 Then
                VerifyRefAssemblyClient(lib_vb, client_vb, validator,
                                        EmitOptions.Default.WithEmitMetadataOnly(False))
            End If
            If debugFlag = -1 OrElse debugFlag = 1 Then
                VerifyRefAssemblyClient(lib_vb, client_vb, validator,
                                        EmitOptions.Default.WithEmitMetadataOnly(True).WithIncludePrivateMembers(True))
            End If
            If debugFlag = -1 OrElse debugFlag = 2 Then
                VerifyRefAssemblyClient(lib_vb, client_vb, validator,
                                        EmitOptions.Default.WithEmitMetadataOnly(False).WithIncludePrivateMembers(False))
            End If
        End Sub

        Private Sub VerifyRefAssemblyClient(lib_vb As String, source As String, validator As Action(Of VisualBasicCompilation), emitOptions As EmitOptions)
            Dim name = GetUniqueName()
            Dim libComp = CreateCompilationWithMscorlib40({Parse(lib_vb)},
                options:=TestOptions.DebugDll.WithDeterministic(True), assemblyName:=name)
            Dim libImage = libComp.EmitToImageReference(emitOptions)

            Dim comp = CreateCompilationWithMscorlib40(source, references:={libImage}, options:=TestOptions.DebugDll)
            validator(comp)
        End Sub

        <Fact>
        Public Sub RefAssembly_IgnoresSomeDiagnostics()

            RefAssembly_IgnoresSomeDiagnostics(
"Public Function M() As Integer
End Function", True)

            RefAssembly_IgnoresSomeDiagnostics(
"Public Function M() As Integer
    Error(
End Function", False) ' Should be true. See follow-up issue https://github.com/dotnet/roslyn/issues/17612

            RefAssembly_IgnoresSomeDiagnostics(
"Public Function M() As Error
End Function", False) ' This may get relaxed. See follow-up issue https://github.com/dotnet/roslyn/issues/17612
        End Sub

        Private Sub RefAssembly_IgnoresSomeDiagnostics(change As String, expectSuccess As Boolean)
            Dim sourceTemplate As String = "
Public Class C
    CHANGE
End Class"

            Dim name As String = GetUniqueName()
            Dim source As String = sourceTemplate.Replace("CHANGE", change)
            Dim comp = CreateCompilationWithMscorlib40(source,
                options:=TestOptions.DebugDll.WithDeterministic(True), assemblyName:=name)

            Using output As New MemoryStream()
                Dim EmitResult = comp.Emit(output, options:=EmitOptions.Default.WithEmitMetadataOnly(True))
                Assert.Equal(expectSuccess, EmitResult.Success)
                Assert.Equal(Not expectSuccess, EmitResult.Diagnostics.Any())
            End Using

        End Sub

        <Fact>
        Public Sub RefAssembly_VerifyTypesAndMembers()
            Dim source = "
Public MustInherit Class PublicClass
    Public Sub PublicMethod()
        System.Console.Write(""Hello"")
    End Sub
    Private Sub PrivateMethod()
        System.Console.Write(""Hello"")
    End Sub
    Protected Sub ProtectedMethod()
        System.Console.Write(""Hello"")
    End Sub
    Friend Sub InternalMethod()
        System.Console.Write(""Hello"")
    End Sub
    Protected Friend Sub ProtectedFriendMethod()
        System.Console.Write(""Hello"")
    End Sub
    Private Protected Sub PrivateProtectedMethod()
        System.Console.Write(""Hello"")
    End Sub
    Public MustOverride Sub AbstractMethod()
    Public Event PublicEvent As System.Action
    Friend Event InternalEvent As System.Action
End Class"
            Dim comp As Compilation = CreateEmptyCompilation(source, references:={MscorlibRef},
                            parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5),
                            options:=TestOptions.DebugDll.WithDeterministic(True))

            Dim verifier = CompileAndVerify(comp, emitOptions:=EmitOptions.Default.WithEmitMetadataOnly(True), verify:=Verification.Passes)

            ' verify metadata (types, members, attributes) of the regular assembly
            Dim realImage = comp.EmitToImageReference(EmitOptions.Default)
            Dim compWithReal = CreateEmptyCompilation("", references:={MscorlibRef, realImage},
                            options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            Dim realAssembly = compWithReal.SourceModule.GetReferencedAssemblySymbols().Last()
            AssertEx.SetEqual(
                {"<Module>", "PublicClass"},
                realAssembly.GlobalNamespace.GetMembers().Select(Function(m) m.ToDisplayString()))

            AssertEx.Equal(
                {"PublicClass.PublicEventEvent As System.Action", "PublicClass.InternalEventEvent As System.Action",
                    "Sub PublicClass..ctor()", "Sub PublicClass.PublicMethod()",
                    "Sub PublicClass.PrivateMethod()", "Sub PublicClass.ProtectedMethod()",
                    "Sub PublicClass.InternalMethod()",
                    "Sub PublicClass.ProtectedFriendMethod()",
                    "Sub PublicClass.PrivateProtectedMethod()",
                    "Sub PublicClass.AbstractMethod()",
                    "Sub PublicClass.add_PublicEvent(obj As System.Action)", "Sub PublicClass.remove_PublicEvent(obj As System.Action)",
                    "Sub PublicClass.add_InternalEvent(obj As System.Action)", "Sub PublicClass.remove_InternalEvent(obj As System.Action)",
                    "Event PublicClass.PublicEvent As System.Action", "Event PublicClass.InternalEvent As System.Action"},
                compWithReal.GetMember(Of NamedTypeSymbol)("PublicClass").GetMembers().
                    Select(Function(m) m.ToTestDisplayString()))

            AssertEx.SetEqual(
                {"System.Runtime.CompilerServices.CompilationRelaxationsAttribute",
                    "System.Runtime.CompilerServices.RuntimeCompatibilityAttribute",
                    "System.Diagnostics.DebuggableAttribute"},
                realAssembly.GetAttributes().Select(Function(a) a.AttributeClass.ToTestDisplayString()))

            ' verify metadata (types, members, attributes) of the metadata-only assembly
            Dim emitMetadataOnly = EmitOptions.Default.WithEmitMetadataOnly(True)
            CompileAndVerify(comp, emitOptions:=emitMetadataOnly, verify:=Verification.Passes)

            Dim metadataImage = comp.EmitToImageReference(emitMetadataOnly)
            Dim compWithMetadata = CreateEmptyCompilation("", references:={MscorlibRef, metadataImage},
                            options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            Dim metadataAssembly As AssemblySymbol = compWithMetadata.SourceModule.GetReferencedAssemblySymbols().Last()
            AssertEx.SetEqual(
                {"<Module>", "PublicClass"},
                metadataAssembly.GlobalNamespace.GetMembers().Select(Function(m) m.ToDisplayString()))

            AssertEx.Equal(
                {"PublicClass.PublicEventEvent As System.Action", "PublicClass.InternalEventEvent As System.Action",
                    "Sub PublicClass..ctor()", "Sub PublicClass.PublicMethod()",
                    "Sub PublicClass.PrivateMethod()", "Sub PublicClass.ProtectedMethod()",
                    "Sub PublicClass.InternalMethod()",
                    "Sub PublicClass.ProtectedFriendMethod()",
                    "Sub PublicClass.PrivateProtectedMethod()",
                    "Sub PublicClass.AbstractMethod()",
                    "Sub PublicClass.add_PublicEvent(obj As System.Action)", "Sub PublicClass.remove_PublicEvent(obj As System.Action)",
                    "Sub PublicClass.add_InternalEvent(obj As System.Action)", "Sub PublicClass.remove_InternalEvent(obj As System.Action)",
                    "Event PublicClass.PublicEvent As System.Action", "Event PublicClass.InternalEvent As System.Action"},
                compWithMetadata.GetMember(Of NamedTypeSymbol)("PublicClass").GetMembers().
                    Select(Function(m) m.ToTestDisplayString()))

            AssertEx.SetEqual(
                {"System.Runtime.CompilerServices.CompilationRelaxationsAttribute",
                    "System.Runtime.CompilerServices.RuntimeCompatibilityAttribute",
                    "System.Diagnostics.DebuggableAttribute"},
                metadataAssembly.GetAttributes().Select(Function(a) a.AttributeClass.ToTestDisplayString()))

            MetadataReaderUtils.AssertEmptyOrThrowNull(comp.EmitToArray(emitMetadataOnly))

            ' verify metadata (types, members, attributes) of the ref assembly
            Dim emitRefOnly = EmitOptions.Default.WithEmitMetadataOnly(True).WithIncludePrivateMembers(False)
            CompileAndVerify(comp, emitOptions:=emitRefOnly, verify:=Verification.Passes)

            Dim refImage = comp.EmitToImageReference(emitRefOnly)
            Dim compWithRef = CreateEmptyCompilation("", references:={MscorlibRef, refImage},
                            options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            Dim refAssembly As AssemblySymbol = compWithRef.SourceModule.GetReferencedAssemblySymbols().Last()
            AssertEx.SetEqual(
                {"<Module>", "PublicClass"},
                refAssembly.GlobalNamespace.GetMembers().Select(Function(m) m.ToDisplayString()))

            AssertEx.SetEqual(
                {"Sub PublicClass..ctor()", "Sub PublicClass.PublicMethod()",
                    "Sub PublicClass.ProtectedMethod()",
                    "Sub PublicClass.ProtectedFriendMethod()",
                    "Sub PublicClass.AbstractMethod()",
                    "Sub PublicClass.add_PublicEvent(obj As System.Action)", "Sub PublicClass.remove_PublicEvent(obj As System.Action)",
                    "Event PublicClass.PublicEvent As System.Action"},
                compWithRef.GetMember(Of NamedTypeSymbol)("PublicClass").GetMembers().
                    Select(Function(m) m.ToTestDisplayString()))

            AssertEx.SetEqual(
                {"System.Runtime.CompilerServices.CompilationRelaxationsAttribute",
                    "System.Runtime.CompilerServices.RuntimeCompatibilityAttribute",
                    "System.Diagnostics.DebuggableAttribute",
                    "System.Runtime.CompilerServices.ReferenceAssemblyAttribute"},
                refAssembly.GetAttributes().Select(Function(a) a.AttributeClass.ToTestDisplayString()))

            MetadataReaderUtils.AssertEmptyOrThrowNull(comp.EmitToArray(emitRefOnly))
        End Sub

        <Fact>
        Public Sub RefAssembly_VerifyTypesAndMembersOnImplementedProperty()
            Dim source = "
Public Interface I
    Property P As Integer
End Interface
Public Class C
    Implements I
    Private Property P As Integer Implements I.P
        Get
            Throw New System.Exception()
        End Get
        Set
            Throw New System.Exception()
        End Set
    End Property
End Class"
            Dim comp As Compilation = CreateEmptyCompilation(source, references:={MscorlibRef},
                            options:=TestOptions.DebugDll.WithDeterministic(True))

            Dim verifier = CompileAndVerify(comp, emitOptions:=EmitOptions.Default.WithEmitMetadataOnly(True), verify:=Verification.Passes)

            ' verify metadata (types, members, attributes) of the regular assembly
            Dim realImage = comp.EmitToImageReference(EmitOptions.Default)
            Dim compWithReal = CreateEmptyCompilation("", references:={MscorlibRef, realImage},
                            options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            Dim realAssembly = compWithReal.SourceModule.GetReferencedAssemblySymbols().Last()
            AssertEx.SetEqual(
                {"<Module>", "I", "C"},
                realAssembly.GlobalNamespace.GetMembers().Select(Function(m) m.ToDisplayString()))

            AssertEx.Equal(
                {"Sub C..ctor()", "Function C.get_P() As System.Int32", "Sub C.set_P(Value As System.Int32)", "Property C.P As System.Int32"},
                compWithReal.GetMember(Of NamedTypeSymbol)("C").GetMembers().
                    Select(Function(m) m.ToTestDisplayString()))

            ' verify metadata (types, members, attributes) of the metadata-only assembly
            Dim emitMetadataOnly = EmitOptions.Default.WithEmitMetadataOnly(True)
            CompileAndVerify(comp, emitOptions:=emitMetadataOnly, verify:=Verification.Passes)

            Dim metadataImage = comp.EmitToImageReference(emitMetadataOnly)
            Dim compWithMetadata = CreateEmptyCompilation("", references:={MscorlibRef, metadataImage},
                            options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            Dim metadataAssembly As AssemblySymbol = compWithMetadata.SourceModule.GetReferencedAssemblySymbols().Last()
            AssertEx.SetEqual(
                {"<Module>", "I", "C"},
                metadataAssembly.GlobalNamespace.GetMembers().Select(Function(m) m.ToDisplayString()))

            AssertEx.Equal(
                {"Sub C..ctor()", "Function C.get_P() As System.Int32", "Sub C.set_P(Value As System.Int32)", "Property C.P As System.Int32"},
                compWithMetadata.GetMember(Of NamedTypeSymbol)("C").GetMembers().
                    Select(Function(m) m.ToTestDisplayString()))

            MetadataReaderUtils.AssertEmptyOrThrowNull(comp.EmitToArray(emitMetadataOnly))

            ' verify metadata (types, members, attributes) of the ref assembly
            Dim emitRefOnly = EmitOptions.Default.WithEmitMetadataOnly(True).WithIncludePrivateMembers(False)
            CompileAndVerify(comp, emitOptions:=emitRefOnly, verify:=Verification.Passes)

            Dim refImage = comp.EmitToImageReference(emitRefOnly)
            Dim compWithRef = CreateEmptyCompilation("", references:={MscorlibRef, refImage},
                            options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            Dim refAssembly As AssemblySymbol = compWithRef.SourceModule.GetReferencedAssemblySymbols().Last()
            AssertEx.SetEqual(
                {"<Module>", "I", "C"},
                refAssembly.GlobalNamespace.GetMembers().Select(Function(m) m.ToDisplayString()))

            AssertEx.SetEqual(
                {"Sub C..ctor()", "Function C.get_P() As System.Int32", "Sub C.set_P(Value As System.Int32)", "Property C.P As System.Int32"},
                compWithRef.GetMember(Of NamedTypeSymbol)("C").GetMembers().
                    Select(Function(m) m.ToTestDisplayString()))

            MetadataReaderUtils.AssertEmptyOrThrowNull(comp.EmitToArray(emitRefOnly))
        End Sub

        <Fact>
        Public Sub RefAssembly_VerifyTypesAndMembersOnImplementedEvent()
            Dim source = "
Public Interface I
    Event E As System.Action
End Interface
Public Class C
    Implements I
    Private Custom Event E As System.Action Implements I.E
        AddHandler(Value As System.Action)
            Throw New System.Exception()
        End AddHandler
        RemoveHandler(Value as System.Action)
            Throw New System.Exception()
        End RemoveHandler
        RaiseEvent()
            Throw New System.Exception()
        End RaiseEvent
    End Event
End Class"
            Dim comp As Compilation = CreateEmptyCompilation(source, references:={MscorlibRef},
                            options:=TestOptions.DebugDll.WithDeterministic(True))

            Dim verifier = CompileAndVerify(comp, emitOptions:=EmitOptions.Default.WithEmitMetadataOnly(True), verify:=Verification.Passes)

            ' verify metadata (types, members, attributes) of the regular assembly
            Dim realImage = comp.EmitToImageReference(EmitOptions.Default)
            Dim compWithReal = CreateEmptyCompilation("", references:={MscorlibRef, realImage},
                            options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            Dim realAssembly = compWithReal.SourceModule.GetReferencedAssemblySymbols().Last()
            AssertEx.SetEqual(
                {"<Module>", "I", "C"},
                realAssembly.GlobalNamespace.GetMembers().Select(Function(m) m.ToDisplayString()))

            AssertEx.Equal(
                {"Sub C..ctor()", "Sub C.add_E(Value As System.Action)", "Sub C.remove_E(Value As System.Action)",
                    "Sub C.raise_E()", "Event C.E As System.Action"},
                compWithReal.GetMember(Of NamedTypeSymbol)("C").GetMembers().
                    Select(Function(m) m.ToTestDisplayString()))

            ' verify metadata (types, members, attributes) of the metadata-only assembly
            Dim emitMetadataOnly = EmitOptions.Default.WithEmitMetadataOnly(True)
            CompileAndVerify(comp, emitOptions:=emitMetadataOnly, verify:=Verification.Passes)

            Dim metadataImage = comp.EmitToImageReference(emitMetadataOnly)
            Dim compWithMetadata = CreateEmptyCompilation("", references:={MscorlibRef, metadataImage},
                            options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            Dim metadataAssembly As AssemblySymbol = compWithMetadata.SourceModule.GetReferencedAssemblySymbols().Last()
            AssertEx.SetEqual(
                {"<Module>", "I", "C"},
                metadataAssembly.GlobalNamespace.GetMembers().Select(Function(m) m.ToDisplayString()))

            AssertEx.Equal(
                {"Sub C..ctor()", "Sub C.add_E(Value As System.Action)", "Sub C.remove_E(Value As System.Action)",
                    "Sub C.raise_E()", "Event C.E As System.Action"},
                compWithMetadata.GetMember(Of NamedTypeSymbol)("C").GetMembers().
                    Select(Function(m) m.ToTestDisplayString()))

            MetadataReaderUtils.AssertEmptyOrThrowNull(comp.EmitToArray(emitMetadataOnly))

            ' verify metadata (types, members, attributes) of the ref assembly
            Dim emitRefOnly = EmitOptions.Default.WithEmitMetadataOnly(True).WithIncludePrivateMembers(False)
            CompileAndVerify(comp, emitOptions:=emitRefOnly, verify:=Verification.Passes)

            Dim refImage = comp.EmitToImageReference(emitRefOnly)
            Dim compWithRef = CreateEmptyCompilation("", references:={MscorlibRef, refImage},
                            options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            Dim refAssembly As AssemblySymbol = compWithRef.SourceModule.GetReferencedAssemblySymbols().Last()
            AssertEx.SetEqual(
                {"<Module>", "I", "C"},
                refAssembly.GlobalNamespace.GetMembers().Select(Function(m) m.ToDisplayString()))

            AssertEx.SetEqual(
                {"Sub C..ctor()", "Sub C.add_E(Value As System.Action)", "Sub C.remove_E(Value As System.Action)",
                    "Sub C.raise_E()", "Event C.E As System.Action"},
                compWithRef.GetMember(Of NamedTypeSymbol)("C").GetMembers().
                    Select(Function(m) m.ToTestDisplayString()))

            MetadataReaderUtils.AssertEmptyOrThrowNull(comp.EmitToArray(emitRefOnly))
        End Sub

        <Fact>
        Public Sub RefAssembly_VerifyTypesAndMembersOnStruct()
            Dim source = "
Friend Structure InternalStruct
    Friend Property P As Integer
End Structure"
            Dim comp As Compilation = CreateEmptyCompilation(source, references:={MscorlibRef},
                            options:=TestOptions.DebugDll.WithDeterministic(True))

            Dim verifier = CompileAndVerify(comp, emitOptions:=EmitOptions.Default.WithEmitMetadataOnly(True), verify:=Verification.Passes)

            ' verify metadata (types, members, attributes) of the ref assembly
            Dim emitRefOnly = EmitOptions.Default.WithEmitMetadataOnly(True).WithIncludePrivateMembers(False)
            CompileAndVerify(comp, emitOptions:=emitRefOnly, verify:=Verification.Passes)

            Dim refImage = comp.EmitToImageReference(emitRefOnly)
            Dim compWithRef = CreateEmptyCompilation("", references:={MscorlibRef, refImage},
                            options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            Dim refAssembly As AssemblySymbol = compWithRef.SourceModule.GetReferencedAssemblySymbols().Last()
            AssertEx.SetEqual(
                {"<Module>", "InternalStruct"},
                refAssembly.GlobalNamespace.GetMembers().Select(Function(m) m.ToDisplayString()))

            AssertEx.SetEqual(
                {"Sub InternalStruct..ctor()", "InternalStruct._P As System.Int32"},
                compWithRef.GetMember(Of NamedTypeSymbol)("InternalStruct").GetMembers().
                    Select(Function(m) m.ToTestDisplayString()))
        End Sub

        <Fact>
        Public Sub EmitMetadataOnly_DisallowPdbs()
            Dim comp = CreateEmptyCompilation("", references:={MscorlibRef},
                            options:=TestOptions.DebugDll.WithDeterministic(True))

            Using output As New MemoryStream()
                Using pdbOutput = New MemoryStream()
                    Assert.Throws(Of ArgumentException)(Function() comp.Emit(output, pdbOutput,
                                        options:=EmitOptions.Default.WithEmitMetadataOnly(True)))
                End Using
            End Using
        End Sub

        <Fact>
        Public Sub EmitMetadataOnly_DisallowMetadataPeStream()
            Dim comp = CreateEmptyCompilation("", references:={MscorlibRef},
                            options:=TestOptions.DebugDll.WithDeterministic(True))

            Using output As New MemoryStream()
                Using metadataPeOutput As New MemoryStream()
                    Assert.Throws(Of ArgumentException)(Function() comp.Emit(output, metadataPEStream:=metadataPeOutput,
                                        options:=EmitOptions.Default.WithEmitMetadataOnly(True)))
                End Using
            End Using
        End Sub

        <Fact>
        Public Sub EmitMetadata()
            Dim source =
"Public MustInherit Class PublicClass
    Public Sub PublicMethod
        System.Console.Write(""Hello"")
    End Sub
End Class "
            Dim comp = CreateEmptyCompilation(source, references:={MscorlibRef},
                            options:=TestOptions.DebugDll.WithDeterministic(True))

            Using output As New MemoryStream()
                Using pdbOutput As New MemoryStream()
                    Using metadataOutput As New MemoryStream()
                        Dim result = comp.Emit(output, pdbOutput, metadataPEStream:=metadataOutput)
                        Assert.True(result.Success)
                        Assert.NotEqual(0, output.Position)
                        Assert.NotEqual(0, pdbOutput.Position)
                        Assert.NotEqual(0, metadataOutput.Position)
                        MetadataReaderUtils.AssertNotThrowNull(ImmutableArray.CreateRange(output.GetBuffer()))
                        MetadataReaderUtils.AssertEmptyOrThrowNull(ImmutableArray.CreateRange(metadataOutput.GetBuffer()))
                    End Using
                End Using
            End Using

            Dim peImage = comp.EmitToArray()
            MetadataReaderUtils.AssertNotThrowNull(peImage)
        End Sub

        <WorkItem(4344, "DevDiv_Projects/Roslyn")>
        <Fact>
        Public Sub Bug4344()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Module M
  Sub Main()
    Dim a as String = "A"
    Dim b as String = "B"
    System.Console.WriteLine(a &lt; b)
  End Sub
End Module
    </file>
</compilation>)

            CompileAndVerify(compilation)
        End Sub

        <WorkItem(540643, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540643")>
        <Fact>
        Public Sub Bug6981()
            ' tests different casing of the method
            Dim source =
<compilation>
    <file name="a.vb">
Imports System

MustInherit Class Base
    Public MustOverride Sub Goo()
End Class

Class Derived
    Inherits Base
    
    Public Overrides Sub goO()
        Console.WriteLine("Keep calm and carry on.")
    End Sub

    Shared Sub Main()
        Dim d as New Derived()
        d.goo()
    End Sub
End Class

    </file>
</compilation>

            CompileAndVerify(source,
                             expectedOutput:=<![CDATA[
Keep calm and carry on.]]>)

            source =
            <compilation>
                <file name="a.vb">
Imports System

    MustInherit Class Base
        Public MustOverride Sub Goo()
    End Class

    Class Derived
        Inherits Base
        Public overrides Sub GOo()
        End Sub
    End Class
    
    Class DerivedDerived
        Inherits Derived

        Public Overrides Sub GOO()
            Console.WriteLine("Keep calm and carry on.")
        End Sub

        Shared Sub Main()
            Dim d As New DerivedDerived()
            d.goo()
        End Sub
    End Class

    </file>
            </compilation>

            CompileAndVerify(source,
                                 expectedOutput:=<![CDATA[
Keep calm and carry on.]]>)

        End Sub

        <Fact>
        Public Sub MatchMultipleOverrides()
            ' tests different casing of the method

            Dim source =
            <compilation>
                <file name="a.vb">
Imports System

    MustInherit Class Base
        Public MustOverride Sub Goo()
        Public MustOverride Sub goO(x as integer)
    End Class

    MustInherit Class Derived
        Inherits Base
        Public Overrides Sub GOo()
        End Sub
        Public Overloads MustOverride Sub GoO(z As String)
    End Class
    
    Class DerivedDerived
        Inherits Derived

        Public Overrides Sub GOO()
            Console.WriteLine("ABC.")
        End Sub

        Public Overrides Sub gOO(x as Integer)
            Console.WriteLine("Life is {0}.", x)
        End Sub
 
        Public Overrides Sub gOo(y as String)
            Console.WriteLine("This is a {0}.", y)
        End Sub

        Public Overloads Sub goo(x as integer, y as String)
            Console.WriteLine("All done.")
        End Sub

        Shared Sub Main()
            Dim d As Base = New DerivedDerived()
            d.Goo()
            d.goO(42)
            DirectCast(d, Derived).GoO("elderberries")
            DirectCast(d, DerivedDerived).goo(42, "elderberries")
        End Sub
    End Class

    </file>
            </compilation>

            CompileAndVerify(source,
                                 expectedOutput:=<![CDATA[
ABC.
Life is 42.
This is a elderberries.
All done.]]>)

        End Sub

        <Fact>
        Public Sub MatchMultipleOverridesCrossLang()
            'Referenced class:
            'public class Base
            '{
            '    public virtual void BANana(int x) { }
            '    public virtual void banANA(string x) { }
            '}

            Dim source =
            <compilation>
                <file name="a.vb">
Imports System

    Class Derived
        Inherits Base

        Public Overloads Sub bAnAnA()
        End Sub

        Public Overrides Sub banana(x as integer)
            Console.WriteLine("Keep calm and carry on.")
        End Sub

        Public Overrides Sub BANANA(xyz as String)
            Console.WriteLine("The authorities have been called.")
        End Sub
 
        Shared Sub Main()
            Dim d As Base = New Derived()
            d.banana(1)
            d.banana("hello")
        End Sub
    End Class

    </file>
            </compilation>

            Dim verifier = CompileAndVerify(source,
                                            references:={TestReferences.SymbolsTests.DifferByCase.CsharpDifferCaseOverloads},
                                 expectedOutput:=<![CDATA[
Keep calm and carry on.
The authorities have been called.]]>)

            Dim compilation = verifier.Compilation
            Dim derivedClass = compilation.GetTypeByMetadataName("Derived")
            Dim allMethods = derivedClass.GetMembers("baNANa").OfType(Of MethodSymbol)()

            AssertNoErrors(compilation)

            ' All methods in Derived should have metadata name "BANana" except the 2nd override.
            Dim count = 0
            For Each m In allMethods
                count = count + 1
                If m.ParameterCount = 1 AndAlso m.Parameters(0).Name = "xyz" Then
                    Assert.Equal("banANA", m.MetadataName)
                Else
                    Assert.Equal("BANana", m.MetadataName)
                End If
            Next
            Assert.Equal(3, count)

        End Sub

        <Fact>
        Public Sub EnsureModulesRoundTrip()
            Dim source =
            <compilation>
                <file name="a.vb">
Module M
  Sub F()
  End Sub
End Module
    </file>
            </compilation>

            Dim c As Compilation = CreateCompilationWithMscorlib40AndVBRuntime(source)
            Dim s = New MemoryStream()
            Assert.True(c.Emit(s).Success)
            c = VisualBasicCompilation.Create("Nothing", references:={MetadataReference.CreateFromImage(s.ToImmutable())})

            Dim m = c.GlobalNamespace.GetModuleMembers()
            Assert.Equal(m.Single().TypeKind, TypeKind.Module)

            Assert.Equal(1, c.GlobalNamespace.GetModuleMembers("M").Length)
            Assert.Equal(0, c.GlobalNamespace.GetModuleMembers("Z").Length)
        End Sub

#Region "PE and metadata bits"

        <Fact()>
        Public Sub TestRuntimeMetaDataVersion()
            Dim source =
            <compilation>
                <file name="a.vb">
        Class C1
            Public Shared Sub Main()
            End Sub
        End Class
                </file>
            </compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {TestMetadata.Net20.mscorlib}, Nothing)
            Dim metadata = ModuleMetadata.CreateFromImage(compilation.EmitToArray())

            ' this is built with a 2.0 mscorlib. The runtimeMetadataVersion should be the same as the runtimeMetadataVersion stored in the assembly
            ' that contains System.Object.
            Dim metadataReader = metadata.Module.GetMetadataReader()
            Assert.Equal("v2.0.50727", metadataReader.MetadataVersion)
        End Sub

        <Fact()>
        Public Sub TestCorFlags()
            Dim source =
            <compilation>
                <file name="a.vb">
        Class C1
            Public Shared Sub Main()
            End Sub
        End Class
                </file>
            </compilation>

            Dim compilation = CreateCompilationWithMscorlib40(source, options:=Nothing)
            Dim peHeaders = New PEHeaders(compilation.EmitToStream())
            Assert.Equal(CorFlags.ILOnly, peHeaders.CorHeader.Flags)

            compilation = CreateCompilationWithMscorlib40(source, options:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithPlatform(Platform.X86))
            peHeaders = New PEHeaders(compilation.EmitToStream())
            Assert.Equal(CorFlags.ILOnly Or CorFlags.Requires32Bit, peHeaders.CorHeader.Flags)

            compilation = CreateCompilationWithMscorlib40(source, options:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithPlatform(Platform.X64))
            peHeaders = New PEHeaders(compilation.EmitToStream())
            Assert.Equal(CorFlags.ILOnly, peHeaders.CorHeader.Flags)
            Assert.True(peHeaders.Requires64Bits)
            Assert.True(peHeaders.RequiresAmdInstructionSet)

            compilation = CreateCompilationWithMscorlib40(source, options:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithPlatform(Platform.AnyCpu32BitPreferred))
            peHeaders = New PEHeaders(compilation.EmitToStream())
            Assert.False(peHeaders.Requires64Bits)
            Assert.False(peHeaders.RequiresAmdInstructionSet)
            Assert.Equal(CorFlags.ILOnly Or CorFlags.Requires32Bit Or CorFlags.Prefers32Bit, peHeaders.CorHeader.Flags)
        End Sub

        <Fact()>
        Public Sub TestCOFFAndPEOptionalHeaders32()
            Dim source =
            <compilation>
                <file name="a.vb">
        Class C1
            Public Shared Sub Main()
            End Sub
        End Class
                </file>
            </compilation>

            Dim compilation = CreateCompilationWithMscorlib40(source, options:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithPlatform(Platform.AnyCpu))
            Dim peHeaders = New PEHeaders(compilation.EmitToStream())

            'interesting COFF bits
            Assert.False(peHeaders.Requires64Bits)
            Assert.True(peHeaders.IsDll)
            Assert.False(peHeaders.IsExe)
            Assert.True(peHeaders.CoffHeader.Characteristics.HasFlag(Characteristics.LargeAddressAware))

            'interesting Optional PE header bits
            'We will use a range beginning with &H50 to identify the Roslyn VB compiler family.
            Assert.Equal(&H50, peHeaders.PEHeader.MajorLinkerVersion)
            Assert.Equal(0, peHeaders.PEHeader.MinorLinkerVersion)
            Assert.Equal(&H10000000UL, peHeaders.PEHeader.ImageBase)

            'section alignment is fixed at 0x2000. 
            Assert.Equal(&H200, peHeaders.PEHeader.FileAlignment)
            Assert.Equal(CType(&H8540, UShort), peHeaders.PEHeader.DllCharacteristics)  'DYNAMIC_BASE | NX_COMPAT | NO_SEH | TERMINAL_SERVER_AWARE

            Assert.Equal(&H100000UL, peHeaders.PEHeader.SizeOfStackReserve)
            Assert.Equal(&H1000UL, peHeaders.PEHeader.SizeOfStackCommit)
            Assert.Equal(&H100000UL, peHeaders.PEHeader.SizeOfHeapReserve)
            Assert.Equal(&H1000UL, peHeaders.PEHeader.SizeOfHeapCommit)

            ' test an exe as well:
            compilation = CreateCompilationWithMscorlib40(source, options:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithPlatform(Platform.AnyCpu))
            peHeaders = New PEHeaders(compilation.EmitToStream())

            'interesting COFF bits
            Assert.False(peHeaders.Requires64Bits)
            Assert.False(peHeaders.IsDll)
            Assert.True(peHeaders.IsExe)

            'interesting Optional PE header bits
            'We will use a range beginning with &H50 to identify the Roslyn VB compiler family.
            Assert.Equal(&H50, peHeaders.PEHeader.MajorLinkerVersion)
            Assert.Equal(0, peHeaders.PEHeader.MinorLinkerVersion)
            Assert.Equal(CType(&H400000, ULong), peHeaders.PEHeader.ImageBase)

            Assert.Equal(&H200, peHeaders.PEHeader.FileAlignment)
            Assert.True(peHeaders.IsConsoleApplication)
            Assert.Equal(CType(&H8540, UShort), peHeaders.PEHeader.DllCharacteristics)  'DYNAMIC_BASE | NX_COMPAT | NO_SEH | TERMINAL_SERVER_AWARE

            Assert.Equal(&H100000UL, peHeaders.PEHeader.SizeOfStackReserve)
            Assert.Equal(&H1000UL, peHeaders.PEHeader.SizeOfStackCommit)
            Assert.Equal(&H100000UL, peHeaders.PEHeader.SizeOfHeapReserve)
            Assert.Equal(&H1000UL, peHeaders.PEHeader.SizeOfHeapCommit)
        End Sub

        <Fact()>
        Public Sub TestCOFFAndPEOptionalHeadersArm()
            Dim source =
            <compilation>
                <file name="a.vb">
        Class C1
            Public Shared Sub Main()
            End Sub
        End Class
                </file>
            </compilation>

            Dim compilation = CreateCompilationWithMscorlib40(source, options:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithPlatform(Platform.Arm))
            Dim peHeaders = New PEHeaders(compilation.EmitToStream())

            'interesting COFF bits
            Assert.False(peHeaders.Requires64Bits)
            Assert.True(peHeaders.IsDll)
            Assert.False(peHeaders.IsExe)
            Assert.True(peHeaders.CoffHeader.Characteristics.HasFlag(Characteristics.LargeAddressAware))

            'interesting Optional PE header bits
            'We will use a range beginning with &H50 to identify the Roslyn VB compiler family.
            Assert.Equal(&H50, peHeaders.PEHeader.MajorLinkerVersion)
            Assert.Equal(0, peHeaders.PEHeader.MinorLinkerVersion)
            Assert.Equal(&H10000000UL, peHeaders.PEHeader.ImageBase)

            'section alignment is fixed at 0x2000. 
            Assert.Equal(&H200, peHeaders.PEHeader.FileAlignment)
            Assert.Equal(CType(&H8540, UShort), peHeaders.PEHeader.DllCharacteristics)  'DYNAMIC_BASE | NX_COMPAT | NO_SEH | TERMINAL_SERVER_AWARE

            Assert.Equal(&H100000UL, peHeaders.PEHeader.SizeOfStackReserve)
            Assert.Equal(&H1000UL, peHeaders.PEHeader.SizeOfStackCommit)
            Assert.Equal(&H100000UL, peHeaders.PEHeader.SizeOfHeapReserve)
            Assert.Equal(&H1000UL, peHeaders.PEHeader.SizeOfHeapCommit)

            ' test an exe as well:
            compilation = CreateCompilationWithMscorlib40(source, options:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithPlatform(Platform.AnyCpu))
            peHeaders = New PEHeaders(compilation.EmitToStream())

            'interesting COFF bits
            Assert.False(peHeaders.Requires64Bits)
            Assert.False(peHeaders.IsDll)
            Assert.True(peHeaders.IsExe)

            'interesting Optional PE header bits
            'We will use a range beginning with &H50 to identify the Roslyn VB compiler family.
            Assert.Equal(&H50, peHeaders.PEHeader.MajorLinkerVersion)
            Assert.Equal(0, peHeaders.PEHeader.MinorLinkerVersion)
            Assert.Equal(CType(&H400000, ULong), peHeaders.PEHeader.ImageBase)

            Assert.Equal(&H200, peHeaders.PEHeader.FileAlignment)
            Assert.True(peHeaders.IsConsoleApplication)
            Assert.Equal(CType(&H8540, UShort), peHeaders.PEHeader.DllCharacteristics)  'DYNAMIC_BASE | NX_COMPAT | NO_SEH | TERMINAL_SERVER_AWARE

            Assert.Equal(&H100000UL, peHeaders.PEHeader.SizeOfStackReserve)
            Assert.Equal(&H1000UL, peHeaders.PEHeader.SizeOfStackCommit)
            Assert.Equal(&H100000UL, peHeaders.PEHeader.SizeOfHeapReserve)
            Assert.Equal(&H1000UL, peHeaders.PEHeader.SizeOfHeapCommit)
        End Sub

        <Fact()>
        Public Sub TestCOFFAndPEOptionalHeaders64()
            Dim source =
            <compilation>
                <file name="a.vb">
        Class C1
            Public Shared Sub Main()
            End Sub
        End Class
                </file>
            </compilation>

            Dim compilation = CreateCompilationWithMscorlib40(source, options:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithPlatform(Platform.X64))
            Dim peHeaders = New PEHeaders(compilation.EmitToStream())

            'interesting COFF bits
            Assert.True(peHeaders.Requires64Bits)
            Assert.True(peHeaders.IsDll)
            Assert.False(peHeaders.IsExe)
            Assert.True(peHeaders.CoffHeader.Characteristics.HasFlag(Characteristics.LargeAddressAware))

            'interesting Optional PE header bits
            'We will use a range beginning with &H50 to identify the Roslyn VB compiler family.
            Assert.Equal(&H50, peHeaders.PEHeader.MajorLinkerVersion)
            Assert.Equal(0, peHeaders.PEHeader.MinorLinkerVersion)
            Assert.Equal(&H180000000UL, peHeaders.PEHeader.ImageBase)

            'section alignment is fixed at 0x2000. 
            Assert.Equal(&H200, peHeaders.PEHeader.FileAlignment)   'doesn't change based on architecture
            Assert.Equal(CType(&H8540, UShort), peHeaders.PEHeader.DllCharacteristics)  'DYNAMIC_BASE | NX_COMPAT | NO_SEH | TERMINAL_SERVER_AWARE

            ' test an exe as well:
            compilation = CreateCompilationWithMscorlib40(source, options:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithPlatform(Platform.X64))
            peHeaders = New PEHeaders(compilation.EmitToStream())

            'interesting COFF bits
            Assert.True(peHeaders.Requires64Bits)
            Assert.False(peHeaders.IsDll)
            Assert.True(peHeaders.IsExe)

            'interesting Optional PE header bits
            'We will use a range beginning with &H50 to identify the Roslyn VB compiler family.
            Assert.Equal(&H50, peHeaders.PEHeader.MajorLinkerVersion)
            Assert.Equal(0, peHeaders.PEHeader.MinorLinkerVersion)
            Assert.Equal(&H140000000UL, peHeaders.PEHeader.ImageBase)

            Assert.Equal(&H200, peHeaders.PEHeader.FileAlignment)
            Assert.True(peHeaders.IsConsoleApplication)
            Assert.Equal(CType(&H8540, UShort), peHeaders.PEHeader.DllCharacteristics)  'DYNAMIC_BASE | NX_COMPAT | NO_SEH | TERMINAL_SERVER_AWARE

            'Verify additional items
            Assert.Equal(&H400000UL, peHeaders.PEHeader.SizeOfStackReserve)
            Assert.Equal(&H4000UL, peHeaders.PEHeader.SizeOfStackCommit)
            Assert.Equal(&H100000UL, peHeaders.PEHeader.SizeOfHeapReserve) ' is the 32bit value!
            Assert.Equal(&H2000UL, peHeaders.PEHeader.SizeOfHeapCommit)
        End Sub

        <Fact()>
        Public Sub CheckDllCharacteristicsHighEntropyVA()
            Dim source =
            <compilation>
                <file name="a.vb">
        Class C1
            Public Shared Sub Main()
            End Sub
        End Class
                </file>
            </compilation>

            Dim compilation = CreateCompilationWithMscorlib40(source)
            Dim peHeaders = New PEHeaders(compilation.EmitToStream(options:=New EmitOptions(highEntropyVirtualAddressSpace:=True)))
            Assert.Equal(CType(&H8560, UShort), peHeaders.PEHeader.DllCharacteristics)  'DYNAMIC_BASE | NX_COMPAT | NO_SEH | TERMINAL_SERVER_AWARE | HIGH_ENTROPY_VA (0x20)
        End Sub

        <WorkItem(764418, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/764418")>
        <Fact()>
        Public Sub CheckDllCharacteristicsWinRtApp()
            Dim source =
            <compilation>
                <file name="a.vb">
        Class C1
            Public Shared Sub Main()
            End Sub
        End Class
                </file>
            </compilation>

            Dim compilation = CreateCompilationWithMscorlib40(source, options:=New VisualBasicCompilationOptions(OutputKind.WindowsRuntimeApplication))
            Dim peHeaders = New PEHeaders(compilation.EmitToStream())
            Assert.Equal(CType(&H9540, UShort), peHeaders.PEHeader.DllCharacteristics)  'DYNAMIC_BASE | NX_COMPAT | NO_SEH | TERMINAL_SERVER_AWARE | HIGH_ENTROPY_VA (0x20)
        End Sub

        <Fact()>
        Public Sub TestBaseAddress()
            Dim source =
            <compilation>
                <file name="a.vb">
        Class C1
            Public Shared Sub Main()
            End Sub
        End Class
                </file>
            </compilation>

            ' last four hex digits get zero'ed out
            Dim compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe)
            Dim peHeaders = New PEHeaders(compilation.EmitToStream(options:=New EmitOptions(baseAddress:=&H10111111)))
            Assert.Equal(CType(&H10110000, ULong), peHeaders.PEHeader.ImageBase)

            ' rounded up by 0x8000
            compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe)
            peHeaders = New PEHeaders(compilation.EmitToStream(options:=New EmitOptions(baseAddress:=&H8000)))
            Assert.Equal(CType(&H10000, ULong), peHeaders.PEHeader.ImageBase)

            ' valued less than 0x8000 are being ignored
            compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe)
            peHeaders = New PEHeaders(compilation.EmitToStream(options:=New EmitOptions(baseAddress:=&H7FFF)))
            Assert.Equal(&H400000UL, peHeaders.PEHeader.ImageBase)

            ' default for 32 bit
            compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe.WithPlatform(Platform.X86))
            peHeaders = New PEHeaders(compilation.EmitToStream())
            Assert.Equal(&H400000UL, peHeaders.PEHeader.ImageBase)

            compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe.WithPlatform(Platform.X64))
            peHeaders = New PEHeaders(compilation.EmitToStream())
            Assert.Equal(&H140000000UL, peHeaders.PEHeader.ImageBase)

            compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseDll.WithPlatform(Platform.X64))
            peHeaders = New PEHeaders(compilation.EmitToStream())
            Assert.Equal(&H180000000UL, peHeaders.PEHeader.ImageBase)

        End Sub

        <Fact()>
        Public Sub TestFileAlignment()
            Dim source =
            <compilation>
                <file name="a.vb">
        Class C1
            Public Shared Sub Main()
            End Sub
        End Class
                </file>
            </compilation>

            Dim compilation = CreateCompilationWithMscorlib40(source)
            Dim peHeaders = New PEHeaders(compilation.EmitToStream(options:=New EmitOptions(fileAlignment:=1024)))
            Assert.Equal(1024, peHeaders.PEHeader.FileAlignment)

            compilation = CreateCompilationWithMscorlib40(source)
            peHeaders = New PEHeaders(compilation.EmitToStream(options:=New EmitOptions(fileAlignment:=4096)))
            Assert.Equal(4096, peHeaders.PEHeader.FileAlignment)
        End Sub

#End Region

        <Fact()>
        Public Sub Bug10273()

            Dim source =
            <compilation name="C">
                <file name="a.vb">
Imports System

    Public Structure C
        Public C As Integer
        Public Shared B As Integer = 12

        Public Sub F()
        End Sub

        Public A As Integer
    End Structure

    Public Delegate Sub B()

    Public Class A

        Public C As Integer
        Public Shared B As Integer = 12

        Public Sub F()
        End Sub

        Public A As Integer

        Public Property I As Integer
        Public Sub E()
        End Sub
        Public Property H As Integer
        Public Property G As Integer

        Public Event L As System.Action
        Public Sub D()
        End Sub
        Public Event K As System.Action
        Public Event J As System.Action

        Public Class O
        End Class
        Public Class N
        End Class
        Public Class M
        End Class

        Partial Public Class N
        End Class
        Partial Public Class M
        End Class
        Partial Public Class O
        End Class

        Public Sub F(x as Integer)
        End Sub
        Public Sub E(x as Integer)
        End Sub
        Public Sub D(x as Integer)
        End Sub
    End Class

    Namespace F
    End Namespace

    Public Class G
    End Class

    Namespace E
    End Namespace
    Namespace D
    End Namespace
    </file>
            </compilation>

            Dim sourceSymbolValidator = Sub(m As ModuleSymbol)
                                            Dim expectedGlobalMembers = {"C", "B", "A", "F", "G", "E", "D"}
                                            Dim actualGlobalMembers = DirectCast(m, SourceModuleSymbol).GlobalNamespace.GetMembers().ToArray()

                                            Assert.NotNull(m.GlobalNamespace.ContainingModule)
                                            Assert.Equal(Of String)("C.dll", m.GlobalNamespace.ContainingModule.ToString)

                                            For i As Integer = 0 To Math.Max(expectedGlobalMembers.Length, actualGlobalMembers.Length) - 1
                                                Assert.Equal(expectedGlobalMembers(i), actualGlobalMembers(i).Name)
                                            Next

                                            Dim expectedAMembers = {".cctor", ".ctor",
                                                                    "C", "B", "F", "A",
                                                                    "_I", "get_I", "set_I", "I",
                                                                    "E",
                                                                    "_H", "get_H", "set_H", "H",
                                                                    "_G", "get_G", "set_G", "G",
                                                                    "LEvent", "add_L", "remove_L", "L",
                                                                    "D",
                                                                    "KEvent", "add_K", "remove_K", "K",
                                                                    "JEvent", "add_J", "remove_J", "J",
                                                                    "O", "N", "M",
                                                                    "F", "E", "D"}
                                            Dim actualAMembers = DirectCast(m, SourceModuleSymbol).GlobalNamespace.GetTypeMembers("A").Single().GetMembers().ToArray()

                                            For i As Integer = 0 To Math.Max(expectedAMembers.Length, actualAMembers.Length) - 1
                                                Assert.Equal(expectedAMembers(i), actualAMembers(i).Name)
                                            Next

                                            Dim expectedBMembers = {".ctor", "BeginInvoke", "EndInvoke", "Invoke"}
                                            Dim actualBMembers = DirectCast(m, SourceModuleSymbol).GlobalNamespace.GetTypeMembers("B").Single().GetMembers().ToArray()

                                            For i As Integer = 0 To Math.Max(expectedBMembers.Length, actualBMembers.Length) - 1
                                                Assert.Equal(expectedBMembers(i), actualBMembers(i).Name)
                                            Next

                                            Dim expectedCMembers = {".cctor", ".ctor",
                                                                    "C", "B", "F", "A"}
                                            Dim actualCMembers = DirectCast(m, SourceModuleSymbol).GlobalNamespace.GetTypeMembers("C").Single().GetMembers().ToArray()

                                            For i As Integer = 0 To Math.Max(expectedCMembers.Length, actualCMembers.Length) - 1
                                                Assert.Equal(expectedCMembers(i), actualCMembers(i).Name)
                                            Next
                                        End Sub

            Dim peSymbolValidator = Sub(m As ModuleSymbol)
                                        Dim expectedAMembers = {"C", "B", "A",
                                                                ".ctor",
                                                                "F",
                                                                "get_I", "set_I",
                                                                "E",
                                                                "get_H", "set_H",
                                                                "get_G", "set_G",
                                                                "add_L", "remove_L",
                                                                "D",
                                                                "add_K", "remove_K",
                                                                "add_J", "remove_J",
                                                                "F", "E", "D",
                                                                "I", "H", "G",
                                                                "L", "K", "J",
                                                                "O", "N", "M"}
                                        Dim actualAMembers = m.GlobalNamespace.GetTypeMembers("A").Single().GetMembers().ToArray()

                                        For i As Integer = 0 To Math.Max(expectedAMembers.Length, actualAMembers.Length) - 1
                                            Assert.Equal(expectedAMembers(i), actualAMembers(i).Name)
                                        Next

                                        Dim expectedBMembers = {".ctor", "BeginInvoke", "EndInvoke", "Invoke"}
                                        Dim actualBMembers = m.GlobalNamespace.GetTypeMembers("B").Single().GetMembers().ToArray()

                                        For i As Integer = 0 To Math.Max(expectedBMembers.Length, actualBMembers.Length) - 1
                                            Assert.Equal(expectedBMembers(i), actualBMembers(i).Name)
                                        Next

                                        Dim expectedCMembers = {"C", "B", "A", ".ctor", "F"}
                                        Dim actualCMembers = m.GlobalNamespace.GetTypeMembers("C").Single().GetMembers().ToArray()

                                        For i As Integer = 0 To Math.Max(expectedCMembers.Length, actualCMembers.Length) - 1
                                            Assert.Equal(expectedCMembers(i), actualCMembers(i).Name)
                                        Next
                                    End Sub

            CompileAndVerify(source,
                             sourceSymbolValidator:=sourceSymbolValidator,
                             symbolValidator:=peSymbolValidator)

        End Sub

        ''' <summary>
        ''' Validate the contents of the DeclSecurity metadata table.
        ''' </summary>
        Private Shared Sub ValidateDeclSecurity([module] As ModuleSymbol, ParamArray expectedEntries As DeclSecurityEntry())
            Dim metadataReader = [module].GetMetadata().MetadataReader
            Assert.Equal(expectedEntries.Length, metadataReader.DeclarativeSecurityAttributes.Count)

            Dim i = 0
            For Each actualHandle In metadataReader.DeclarativeSecurityAttributes
                Dim actual = metadataReader.GetDeclarativeSecurityAttribute(actualHandle)
                Dim expected = expectedEntries(CInt(i))

                Assert.Equal(expected.ActionFlags, actual.Action)
                Assert.Equal(GetExpectedParentToken(metadataReader, expected), actual.Parent)

                Dim actualPermissionSetBytes = metadataReader.GetBlobBytes(actual.PermissionSet)
                Dim actualPermissionSet = New String(actualPermissionSetBytes.Select(Function(b) ChrW(b)).ToArray())

                Assert.Equal(expected.PermissionSet, actualPermissionSet)

                i = i + 1
            Next
        End Sub

        Private Shared Function GetExpectedParentToken(metadataReader As MetadataReader, entry As DeclSecurityEntry) As EntityHandle
            Select Case entry.ParentKind
                Case SymbolKind.Assembly
                    Return EntityHandle.AssemblyDefinition

                Case SymbolKind.NamedType
                    Return GetTokenForType(metadataReader, entry.ParentNameOpt)

                Case SymbolKind.Method
                    Return GetTokenForMethod(metadataReader, entry.ParentNameOpt)

                Case Else
                    Throw TestExceptionUtilities.UnexpectedValue(entry.ParentKind)
            End Select
        End Function

        Private Shared Function GetTokenForType(metadataReader As MetadataReader, typeName As String) As TypeDefinitionHandle
            Assert.NotNull(typeName)
            Assert.NotEmpty(typeName)

            For Each typeDef In metadataReader.TypeDefinitions
                Dim typeDefRow = metadataReader.GetTypeDefinition(typeDef)
                Dim name As String = metadataReader.GetString(typeDefRow.Name)

                If typeName.Equals(name) Then
                    Return typeDef
                End If
            Next

            AssertEx.Fail("Unable to find type:" + typeName)
            Return Nothing
        End Function

        Private Shared Function GetTokenForMethod(metadataReader As MetadataReader, methodName As String) As MethodDefinitionHandle
            Assert.NotNull(methodName)
            Assert.NotEmpty(methodName)

            For Each methodDef In metadataReader.MethodDefinitions
                Dim name = metadataReader.GetString(metadataReader.GetMethodDefinition(methodDef).Name)

                If methodName.Equals(name) Then
                    Return methodDef
                End If
            Next

            AssertEx.Fail("Unable to find method:" + methodName)
            Return Nothing
        End Function

        Private Class DeclSecurityEntry
            Public ActionFlags As DeclarativeSecurityAction
            Public ParentKind As SymbolKind
            Public ParentNameOpt As String
            Public PermissionSet As String
        End Class

        <Fact()>
        Public Sub TestSecurityPseudoCustomAttributeOnType()
            Dim source =
<compilation name="Test">
    <file name="a.vb">
Imports System.Security
Imports System.Security.Permissions

class MySecurityAttribute 
    inherits SecurityAttribute

    public sub new (a as SecurityAction)
        mybase.new(a)
    end sub

    public overrides function CreatePermission() as IPermission 
        return nothing
    end function
end class

&lt;PrincipalPermission(SecurityAction.Demand, Role:="User1")&gt;
&lt;MySecurityAttribute(SecurityAction.Assert)&gt;
public class C
end class

Module Program
    Sub Main()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source)
            compilation.VerifyDiagnostics()

            CompileAndVerify(compilation, symbolValidator:=
                             Sub([module] As ModuleSymbol)
                                 ValidateDeclSecurity([module],
                                                      New DeclSecurityEntry() With {
                                                         .ActionFlags = DeclarativeSecurityAction.Demand,
                                                         .ParentKind = SymbolKind.NamedType,
                                                         .ParentNameOpt = "C",
                                                         .PermissionSet =
                                                             "." &
                                                             ChrW(1) &
                                                             ChrW(&H80) &
                                                             ChrW(&H85) &
                                                             "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" &
                                                             ChrW(&HE) &
                                                             ChrW(&H1) &
                                                             ChrW(&H54) &
                                                             ChrW(&HE) &
                                                             ChrW(&H4) &
                                                             "Role" &
                                                             ChrW(&H5) &
                                                             "User1"},
                                                         New DeclSecurityEntry() With {
                                                         .ActionFlags = DeclarativeSecurityAction.Assert,
                                                         .ParentKind = SymbolKind.NamedType,
                                                         .ParentNameOpt = "C",
                                                         .PermissionSet =
                                                             "." &
                                                             ChrW(1) &
                                                             ChrW(&H50) &
                                                             "MySecurityAttribute, Test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null" &
                                                             ChrW(1) &
                                                             ChrW(0)})
                             End Sub)
        End Sub

        <Fact()>
        Public Sub TestSecurityPseudoCustomAttributeOnMethod()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System.Security.Permissions
    
public class C
    &lt;PrincipalPermission(SecurityAction.Demand, Role:="User1")&gt;
    public sub goo()
    end sub
end class

Module Program
    Sub Main()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source)
            compilation.VerifyDiagnostics()

            CompileAndVerify(compilation, symbolValidator:=
                             Sub([module] As ModuleSymbol)
                                 ValidateDeclSecurity([module],
                                 New DeclSecurityEntry() With {
                                    .ActionFlags = DeclarativeSecurityAction.Demand,
                                    .ParentKind = SymbolKind.Method,
                                    .ParentNameOpt = "goo",
                                    .PermissionSet =
                                        "." &
                                        ChrW(1) &
                                        ChrW(&H80) &
                                        ChrW(&H85) &
                                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" &
                                        ChrW(&HE) &
                                        ChrW(&H1) &
                                        ChrW(&H54) &
                                        ChrW(&HE) &
                                        ChrW(&H4) &
                                        "Role" &
                                        ChrW(&H5) &
                                        "User1"})
                             End Sub)
        End Sub

        <Fact()>
        Public Sub TestSecurityPseudoCustomAttributeSameTypeSameAction()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System.Security.Permissions

&lt;PrincipalPermission(SecurityAction.Demand, Role:="User1")&gt;
&lt;PrincipalPermission(SecurityAction.Demand, Role:="User1")&gt;
public class C
end class

Module Program
    Sub Main()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source)
            compilation.VerifyDiagnostics()

            CompileAndVerify(compilation, symbolValidator:=
                             Sub([module] As ModuleSymbol)
                                 ValidateDeclSecurity([module],
                                                      New DeclSecurityEntry() With {
                                                         .ActionFlags = DeclarativeSecurityAction.Demand,
                                                         .ParentKind = SymbolKind.NamedType,
                                                         .ParentNameOpt = "C",
                                                         .PermissionSet =
                                                             "." &
                                                             ChrW(2) &
                                                                      _
                                                             ChrW(&H80) &
                                                             ChrW(&H85) &
                                                             "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" &
                                                             ChrW(&HE) &
                                                             ChrW(&H1) &
                                                             ChrW(&H54) &
                                                             ChrW(&HE) &
                                                             ChrW(&H4) &
                                                             "Role" &
                                                             ChrW(&H5) &
                                                             "User1" &
                                                                      _
                                                             ChrW(&H80) &
                                                             ChrW(&H85) &
                                                             "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" &
                                                             ChrW(&HE) &
                                                             ChrW(&H1) &
                                                             ChrW(&H54) &
                                                             ChrW(&HE) &
                                                             ChrW(&H4) &
                                                             "Role" &
                                                             ChrW(&H5) &
                                                             "User1"})
                             End Sub)
        End Sub

        <Fact()>
        Public Sub TestSecurityPseudoCustomAttributeSameMethodSameAction()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System.Security.Permissions
    
public class C
    &lt;PrincipalPermission(SecurityAction.Demand, Role:="User1")&gt;
    &lt;PrincipalPermission(SecurityAction.Demand, Role:="User1")&gt;
    public sub goo()
    end sub
end class

Module Program
    Sub Main()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source)
            compilation.VerifyDiagnostics()

            CompileAndVerify(compilation, symbolValidator:=
                             Sub([module] As ModuleSymbol)
                                 ValidateDeclSecurity([module],
                                                      New DeclSecurityEntry() With {
                                                         .ActionFlags = DeclarativeSecurityAction.Demand,
                                                         .ParentKind = SymbolKind.Method,
                                                         .ParentNameOpt = "goo",
                                                         .PermissionSet =
                                                             "." &
                                                             ChrW(2) &
                                                                      _
                                                             ChrW(&H80) &
                                                             ChrW(&H85) &
                                                             "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" &
                                                             ChrW(&HE) &
                                                             ChrW(&H1) &
                                                             ChrW(&H54) &
                                                             ChrW(&HE) &
                                                             ChrW(&H4) &
                                                             "Role" &
                                                             ChrW(&H5) &
                                                             "User1" &
                                                                      _
                                                     ChrW(&H80) &
                                                             ChrW(&H85) &
                                                             "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" &
                                                             ChrW(&HE) &
                                                             ChrW(&H1) &
                                                             ChrW(&H54) &
                                                             ChrW(&HE) &
                                                             ChrW(&H4) &
                                                             "Role" &
                                                             ChrW(&H5) &
                                                             "User1"})
                             End Sub)
        End Sub

        <Fact()>
        Public Sub TestSecurityPseudoCustomAttributeSameTypeDifferentAction()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System.Security.Permissions

&lt;PrincipalPermission(SecurityAction.Demand, Role:="User1")&gt;
&lt;PrincipalPermission(SecurityAction.Assert, Role:="User2")&gt;
public class C
end class

Module Program
    Sub Main()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source)
            compilation.VerifyDiagnostics()

            CompileAndVerify(compilation, symbolValidator:=
                             Sub([module] As ModuleSymbol)
                                 ValidateDeclSecurity([module],
                                 New DeclSecurityEntry() With {
                                    .ActionFlags = DeclarativeSecurityAction.Demand,
                                    .ParentKind = SymbolKind.NamedType,
                                    .ParentNameOpt = "C",
                                    .PermissionSet =
                                        "." &
                                        ChrW(1) &
                                        ChrW(&H80) &
                                        ChrW(&H85) &
                                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" &
                                        ChrW(&HE) &
                                        ChrW(&H1) &
                                        ChrW(&H54) &
                                        ChrW(&HE) &
                                        ChrW(&H4) &
                                        "Role" &
                                        ChrW(&H5) &
                                        "User1"},
                                New DeclSecurityEntry() With {
                                    .ActionFlags = DeclarativeSecurityAction.Assert,
                                    .ParentKind = SymbolKind.NamedType,
                                    .ParentNameOpt = "C",
                                    .PermissionSet =
                                        "." &
                                        ChrW(1) &
                                        ChrW(&H80) &
                                        ChrW(&H85) &
                                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" &
                                        ChrW(&HE) &
                                        ChrW(&H1) &
                                        ChrW(&H54) &
                                        ChrW(&HE) &
                                        ChrW(&H4) &
                                        "Role" &
                                        ChrW(&H5) &
                                        "User2"})
                             End Sub)
        End Sub

        <Fact()>
        Public Sub TestSecurityPseudoCustomAttributeSameMethodDifferentAction()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System.Security.Permissions

public class C
    &lt;PrincipalPermission(SecurityAction.Demand, Role:="User1")&gt;
    &lt;PrincipalPermission(SecurityAction.Assert, Role:="User2")&gt;
    public sub goo()
    end sub
end class

Module Program
    Sub Main()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source)
            compilation.VerifyDiagnostics()

            CompileAndVerify(compilation, symbolValidator:=
                             Sub([module] As ModuleSymbol)
                                 ValidateDeclSecurity([module],
                                 New DeclSecurityEntry() With {
                                    .ActionFlags = DeclarativeSecurityAction.Demand,
                                    .ParentKind = SymbolKind.Method,
                                    .ParentNameOpt = "goo",
                                    .PermissionSet =
                                        "." &
                                        ChrW(1) &
                                        ChrW(&H80) &
                                        ChrW(&H85) &
                                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" &
                                        ChrW(&HE) &
                                        ChrW(&H1) &
                                        ChrW(&H54) &
                                        ChrW(&HE) &
                                        ChrW(&H4) &
                                        "Role" &
                                        ChrW(&H5) &
                                        "User1"},
                                New DeclSecurityEntry() With {
                                    .ActionFlags = DeclarativeSecurityAction.Assert,
                                    .ParentKind = SymbolKind.Method,
                                    .ParentNameOpt = "goo",
                                    .PermissionSet =
                                        "." &
                                        ChrW(1) &
                                        ChrW(&H80) &
                                        ChrW(&H85) &
                                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" &
                                        ChrW(&HE) &
                                        ChrW(&H1) &
                                        ChrW(&H54) &
                                        ChrW(&HE) &
                                        ChrW(&H4) &
                                        "Role" &
                                        ChrW(&H5) &
                                        "User2"})
                             End Sub)
        End Sub

        <Fact()>
        Public Sub TestSecurityPseudoCustomAttributeDifferentTypeSameAction()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System.Security.Permissions
    
public class C1
    &lt;PrincipalPermission(SecurityAction.Demand, Role:="User1")&gt;
    public sub goo1()
    end sub
end class

    
public class C2
    &lt;PrincipalPermission(SecurityAction.Demand, Role:="User1")&gt;
    public sub goo2()
    end sub
end class

Module Program
    Sub Main()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source)
            compilation.VerifyDiagnostics()

            CompileAndVerify(compilation, symbolValidator:=
                             Sub([module] As ModuleSymbol)
                                 ValidateDeclSecurity([module],
                                 New DeclSecurityEntry() With {
                                    .ActionFlags = DeclarativeSecurityAction.Demand,
                                    .ParentKind = SymbolKind.Method,
                                    .ParentNameOpt = "goo1",
                                    .PermissionSet =
                                        "." &
                                        ChrW(1) &
                                        ChrW(&H80) &
                                        ChrW(&H85) &
                                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" &
                                        ChrW(&HE) &
                                        ChrW(&H1) &
                                        ChrW(&H54) &
                                        ChrW(&HE) &
                                        ChrW(&H4) &
                                        "Role" &
                                        ChrW(&H5) &
                                        "User1"},
                                New DeclSecurityEntry() With {
                                    .ActionFlags = DeclarativeSecurityAction.Demand,
                                    .ParentKind = SymbolKind.Method,
                                    .ParentNameOpt = "goo2",
                                    .PermissionSet =
                                        "." &
                                        ChrW(1) &
                                        ChrW(&H80) &
                                        ChrW(&H85) &
                                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" &
                                        ChrW(&HE) &
                                        ChrW(&H1) &
                                        ChrW(&H54) &
                                        ChrW(&HE) &
                                        ChrW(&H4) &
                                        "Role" &
                                        ChrW(&H5) &
                                        "User1"})
                             End Sub)
        End Sub

        <Fact()>
        Public Sub TestSecurityPseudoCustomAttributeDifferentMethodSameAction()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System.Security.Permissions

&lt;PrincipalPermission(SecurityAction.Demand, Role:="User1")&gt;
public class C1
end class

&lt;PrincipalPermission(SecurityAction.Demand, Role:="User1")&gt;
public class C2
end class

Module Program
    Sub Main()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source)
            compilation.VerifyDiagnostics()

            CompileAndVerify(compilation, symbolValidator:=
                             Sub([module] As ModuleSymbol)
                                 ValidateDeclSecurity([module],
                                                      New DeclSecurityEntry() With {
                                                         .ActionFlags = DeclarativeSecurityAction.Demand,
                                                         .ParentKind = SymbolKind.NamedType,
                                                         .ParentNameOpt = "C1",
                                                         .PermissionSet =
                                                             "." &
                                                             ChrW(1) &
                                                             ChrW(&H80) &
                                                             ChrW(&H85) &
                                                             "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" &
                                                             ChrW(&HE) &
                                                             ChrW(&H1) &
                                                             ChrW(&H54) &
                                                             ChrW(&HE) &
                                                             ChrW(&H4) &
                                                             "Role" &
                                                             ChrW(&H5) &
                                                             "User1"},
                                                     New DeclSecurityEntry() With {
                                                         .ActionFlags = DeclarativeSecurityAction.Demand,
                                                         .ParentKind = SymbolKind.NamedType,
                                                         .ParentNameOpt = "C2",
                                                         .PermissionSet =
                                                             "." &
                                                             ChrW(1) &
                                                             ChrW(&H80) &
                                                             ChrW(&H85) &
                                                             "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" &
                                                             ChrW(&HE) &
                                                             ChrW(&H1) &
                                                             ChrW(&H54) &
                                                             ChrW(&HE) &
                                                             ChrW(&H4) &
                                                             "Role" &
                                                             ChrW(&H5) &
                                                             "User1"})
                             End Sub)
        End Sub

        <Fact()>
        Public Sub TestSecurityPseudoCustomAttributesMultipleAssemblyTypeMethod()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System.Security.Permissions

&lt;assembly: SecurityPermission(SecurityAction.RequestOptional, RemotingConfiguration:=true)&gt;
&lt;assembly: SecurityPermission(SecurityAction.RequestMinimum, UnmanagedCode:=true)&gt;

&lt;PrincipalPermission(SecurityAction.Demand, Role:="User1")&gt;        
public class C1
    &lt;PrincipalPermission(SecurityAction.Assert, Role:="User2")&gt;
    public sub goo1()
    end sub
end class

Module Program
    Sub Main()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source)
            compilation.VerifyDiagnostics(
                Diagnostic(ERRID.WRN_UseOfObsoleteSymbol2, "SecurityAction.RequestOptional").WithArguments("RequestOptional", "Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."),
                Diagnostic(ERRID.WRN_UseOfObsoleteSymbol2, "SecurityAction.RequestMinimum").WithArguments("RequestMinimum", "Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."))

            CompileAndVerify(compilation, symbolValidator:=
                             Sub([module] As ModuleSymbol)
                                 ValidateDeclSecurity([module], New DeclSecurityEntry() With {
                                    .ActionFlags = DeclarativeSecurityAction.RequestOptional,
                                    .ParentKind = SymbolKind.Assembly,
                                    .PermissionSet =
                                        "." &
                                        ChrW(1) &
                                        ChrW(&H80) &
                                        ChrW(&H84) &
                                        "System.Security.Permissions.SecurityPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" &
                                        ChrW(&H1A) &
                                        ChrW(&H1) &
                                        ChrW(&H54) &
                                        ChrW(&H2) &
                                        ChrW(&H15) &
                                        "RemotingConfiguration" &
                                        ChrW(&H1)},
                                New DeclSecurityEntry() With {
                                    .ActionFlags = DeclarativeSecurityAction.RequestMinimum,
                                    .ParentKind = SymbolKind.Assembly,
                                    .PermissionSet =
                                        "." &
                                        ChrW(1) &
                                        ChrW(&H80) &
                                        ChrW(&H84) &
                                        "System.Security.Permissions.SecurityPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" &
                                        ChrW(&H12) &
                                        ChrW(&H1) &
                                        ChrW(&H54) &
                                        ChrW(&H2) &
                                        ChrW(&HD) &
                                        "UnmanagedCode" &
                                        ChrW(&H1)},
                                 New DeclSecurityEntry() With {
                                    .ActionFlags = DeclarativeSecurityAction.Demand,
                                    .ParentKind = SymbolKind.NamedType,
                                    .ParentNameOpt = "C1",
                                    .PermissionSet =
                                        "." &
                                        ChrW(1) &
                                        ChrW(&H80) &
                                        ChrW(&H85) &
                                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" &
                                        ChrW(&HE) &
                                        ChrW(&H1) &
                                        ChrW(&H54) &
                                        ChrW(&HE) &
                                        ChrW(&H4) &
                                        "Role" &
                                        ChrW(&H5) &
                                        "User1"},
                                New DeclSecurityEntry() With {
                                    .ActionFlags = DeclarativeSecurityAction.Assert,
                                    .ParentKind = SymbolKind.Method,
                                    .ParentNameOpt = "goo1",
                                    .PermissionSet =
                                        "." &
                                        ChrW(1) &
                                        ChrW(&H80) &
                                        ChrW(&H85) &
                                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" &
                                        ChrW(&HE) &
                                        ChrW(&H1) &
                                        ChrW(&H54) &
                                        ChrW(&HE) &
                                        ChrW(&H4) &
                                        "Role" &
                                        ChrW(&H5) &
                                        "User2"})
                             End Sub)
        End Sub

        <Fact()>
        Public Sub TestSecurityPseudoCustomAttributesMultipleAssemblyTypeMethodSymbolApi()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System
Imports System.Security
Imports System.Security.Permissions
Imports System.Security.Principal

    &lt;assembly: SecurityPermission(SecurityAction.RequestOptional, RemotingConfiguration:=true)&gt;
    &lt;assembly: SecurityPermission(SecurityAction.RequestMinimum, UnmanagedCode:=true)&gt;
namespace n
    &lt;PrincipalPermission(SecurityAction.Demand, Role:="User1")&gt;        
    &lt;PrincipalPermission(SecurityAction.Assert, Role:="User2")&gt;        
	public class C1
        &lt;PrincipalPermission(SecurityAction.Demand, Role:="User3")&gt;
        &lt;PrincipalPermission(SecurityAction.Assert, Role:="User4")&gt;
        public sub goo1()
        end sub
	end class

Module Program
    Sub Main()
    End Sub
End Module
end namespace
    </file>
</compilation>

            Dim attributeValidator As Action(Of ModuleSymbol) =
                Sub([module] As ModuleSymbol)
                    Dim assembly = [module].ContainingAssembly
                    Dim ns = DirectCast([module].GlobalNamespace.GetMembers("N").Single, NamespaceSymbol)
                    Dim namedType = DirectCast(ns.GetMembers("C1").Single, NamedTypeSymbol)
                    Dim type = DirectCast(namedType.GetCciAdapter(), Microsoft.Cci.ITypeDefinition)
                    Dim method = DirectCast(namedType.GetMembers("goo1").Single.GetCciAdapter(), Microsoft.Cci.IMethodDefinition)

                    Dim sourceAssembly = DirectCast(assembly, SourceAssemblySymbol)
                    Dim compilation = sourceAssembly.DeclaringCompilation

                    Dim emitOptions = New EmitOptions(outputNameOverride:=sourceAssembly.Name)

                    Dim assemblyBuilder = New PEAssemblyBuilder(sourceAssembly, emitOptions, OutputKind.DynamicallyLinkedLibrary, GetDefaultModulePropertiesForSerialization(), SpecializedCollections.EmptyEnumerable(Of ResourceDescription), Nothing)
                    Dim assemblySecurityAttributes As IEnumerable(Of Cci.SecurityAttribute) = assemblyBuilder.GetSourceAssemblySecurityAttributes()

                    ' Verify Assembly security attributes
                    Assert.Equal(2, assemblySecurityAttributes.Count)
                    Dim emittedName = MetadataTypeName.FromNamespaceAndTypeName("System.Security.Permissions", "SecurityPermissionAttribute")
                    Dim securityPermissionAttr As NamedTypeSymbol = sourceAssembly.CorLibrary.LookupDeclaredTopLevelMetadataType(emittedName)

                    ' Verify <assembly: SecurityPermission(SecurityAction.RequestOptional, RemotingConfiguration:=true)>
                    Dim securityAttribute As Cci.SecurityAttribute = assemblySecurityAttributes.First()
                    Assert.Equal(DeclarativeSecurityAction.RequestOptional, securityAttribute.Action)
                    Dim typeAttribute = DirectCast(securityAttribute.Attribute, VisualBasicAttributeData)
                    Assert.Equal(securityPermissionAttr, typeAttribute.AttributeClass)
                    Assert.Equal(1, typeAttribute.CommonConstructorArguments.Length)
                    typeAttribute.VerifyValue(0, TypedConstantKind.Enum, CInt(DeclarativeSecurityAction.RequestOptional))
                    Assert.Equal(1, typeAttribute.CommonNamedArguments.Length)
                    typeAttribute.VerifyNamedArgumentValue(0, "RemotingConfiguration", TypedConstantKind.Primitive, True)

                    ' Verify <assembly: SecurityPermission(SecurityAction.RequestMinimum, UnmanagedCode:=true)>
                    securityAttribute = assemblySecurityAttributes.Last()
                    Assert.Equal(DeclarativeSecurityAction.RequestMinimum, securityAttribute.Action)
                    typeAttribute = DirectCast(securityAttribute.Attribute, VisualBasicAttributeData)
                    Assert.Equal(securityPermissionAttr, typeAttribute.AttributeClass)
                    Assert.Equal(1, typeAttribute.CommonConstructorArguments.Length)
                    typeAttribute.VerifyValue(0, TypedConstantKind.Enum, CInt(DeclarativeSecurityAction.RequestMinimum))
                    Assert.Equal(1, typeAttribute.CommonNamedArguments.Length)
                    typeAttribute.VerifyNamedArgumentValue(0, "UnmanagedCode", TypedConstantKind.Primitive, True)

                    ' Get System.Security.Permissions.PrincipalPermissionAttribute
                    emittedName = MetadataTypeName.FromNamespaceAndTypeName("System.Security.Permissions", "PrincipalPermissionAttribute")
                    Dim principalPermAttr As NamedTypeSymbol = sourceAssembly.CorLibrary.LookupDeclaredTopLevelMetadataType(emittedName)
                    Assert.NotNull(principalPermAttr)

                    ' Verify type security attributes: different security action
                    Debug.Assert(type.HasDeclarativeSecurity)
                    Dim typeSecurityAttributes As IEnumerable(Of Microsoft.Cci.SecurityAttribute) = type.SecurityAttributes
                    Assert.Equal(2, typeSecurityAttributes.Count)

                    ' Verify <PrincipalPermission(SecurityAction.Demand, Role:="User1")>
                    securityAttribute = typeSecurityAttributes.First()
                    Assert.Equal(DeclarativeSecurityAction.Demand, securityAttribute.Action)
                    typeAttribute = DirectCast(securityAttribute.Attribute, VisualBasicAttributeData)
                    Assert.Equal(principalPermAttr, typeAttribute.AttributeClass)
                    Assert.Equal(1, typeAttribute.CommonConstructorArguments.Length)
                    typeAttribute.VerifyValue(0, TypedConstantKind.Enum, CInt(DeclarativeSecurityAction.Demand))
                    Assert.Equal(1, typeAttribute.CommonNamedArguments.Length)
                    typeAttribute.VerifyNamedArgumentValue(0, "Role", TypedConstantKind.Primitive, "User1")

                    ' Verify <PrincipalPermission(SecurityAction.RequestOptional, Role:="User2")>
                    securityAttribute = typeSecurityAttributes.Last()
                    Assert.Equal(DeclarativeSecurityAction.Assert, securityAttribute.Action)
                    typeAttribute = DirectCast(securityAttribute.Attribute, VisualBasicAttributeData)
                    Assert.Equal(principalPermAttr, typeAttribute.AttributeClass)
                    Assert.Equal(1, typeAttribute.CommonConstructorArguments.Length)
                    typeAttribute.VerifyValue(0, TypedConstantKind.Enum, CInt(DeclarativeSecurityAction.Assert))
                    Assert.Equal(1, typeAttribute.CommonNamedArguments.Length)
                    typeAttribute.VerifyNamedArgumentValue(0, "Role", TypedConstantKind.Primitive, "User2")

                    ' Verify method security attributes: same security action
                    Debug.Assert(method.HasDeclarativeSecurity)
                    Dim methodSecurityAttributes As IEnumerable(Of Microsoft.Cci.SecurityAttribute) = method.SecurityAttributes
                    Assert.Equal(2, methodSecurityAttributes.Count)

                    ' Verify <PrincipalPermission(SecurityAction.Demand, Role:="User3")>
                    securityAttribute = methodSecurityAttributes.First()
                    Assert.Equal(DeclarativeSecurityAction.Demand, securityAttribute.Action)
                    Dim methodAttribute = DirectCast(securityAttribute.Attribute, VisualBasicAttributeData)
                    Assert.Equal(principalPermAttr, methodAttribute.AttributeClass)
                    Assert.Equal(1, methodAttribute.CommonConstructorArguments.Length)
                    methodAttribute.VerifyValue(0, TypedConstantKind.Enum, CInt(DeclarativeSecurityAction.Demand))
                    Assert.Equal(1, methodAttribute.CommonNamedArguments.Length)
                    methodAttribute.VerifyNamedArgumentValue(0, "Role", TypedConstantKind.Primitive, "User3")

                    ' Verify PrincipalPermission(SecurityAction.RequestOptional, Role:="User4")
                    securityAttribute = methodSecurityAttributes.Last()
                    Assert.Equal(DeclarativeSecurityAction.Assert, securityAttribute.Action)
                    methodAttribute = DirectCast(securityAttribute.Attribute, VisualBasicAttributeData)
                    Assert.Equal(principalPermAttr, methodAttribute.AttributeClass)
                    Assert.Equal(1, methodAttribute.CommonConstructorArguments.Length)
                    methodAttribute.VerifyValue(0, TypedConstantKind.Enum, CInt(DeclarativeSecurityAction.Assert))
                    Assert.Equal(1, methodAttribute.CommonNamedArguments.Length)
                    methodAttribute.VerifyNamedArgumentValue(0, "Role", TypedConstantKind.Primitive, "User4")
                End Sub

            CompileAndVerify(source, sourceSymbolValidator:=attributeValidator)
        End Sub

        ' a.netmodule to b.netmodule
        <Fact()>
        Public Sub EmitModuleWithDifferentName()
            Dim name = "a"
            Dim extension = ".netmodule"
            Dim nameOverride = "b"

            Dim source =
<compilation name=<%= name %>>
    <file name="a.vb">
Class A
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseModule)
            compilation.VerifyDiagnostics()

            Dim assembly = compilation.Assembly
            Assert.Equal("a", assembly.Name)

            Dim [module] = assembly.Modules.Single()
            Assert.Equal(name & extension, [module].Name)

            Dim stream As New MemoryStream()
            Assert.True(compilation.Emit(stream, options:=New EmitOptions(outputNameOverride:=nameOverride & extension)).Success)

            Using metadata = ModuleMetadata.CreateFromImage(stream.ToImmutable())
                Dim metadataReader = metadata.Module.GetMetadataReader()

                Assert.False(metadataReader.IsAssembly)

                Assert.Equal([module].Name, metadataReader.GetString(metadataReader.GetModuleDefinition().Name))
            End Using
        End Sub

        ' a.dll to b.dll - expected use case
        <Fact()>
        Public Sub EmitAssemblyWithDifferentName1()
            Dim name = "a"
            Dim extension = ".dll"
            Dim nameOverride = "b"

            Dim source =
<compilation name=<%= name %>>
    <file name="a.vb">
Class A
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseDll)
            compilation.VerifyDiagnostics()

            Dim assembly = compilation.Assembly
            Assert.Equal(name, assembly.Name)

            Dim [module] = assembly.Modules.Single()
            Assert.Equal(name & extension, [module].Name)

            Dim stream As New MemoryStream()
            Assert.True(compilation.Emit(stream, , options:=New EmitOptions(outputNameOverride:=nameOverride & extension)).Success)

            Using metadata = ModuleMetadata.CreateFromImage(stream.ToImmutable())
                Dim metadataReader = metadata.Module.GetMetadataReader()

                Assert.True(metadataReader.IsAssembly)

                Assert.Equal(nameOverride, metadataReader.GetString(metadataReader.GetAssemblyDefinition().Name))
                Assert.Equal([module].Name, metadataReader.GetString(metadataReader.GetModuleDefinition().Name))
            End Using
        End Sub

        ' a.dll to b - allowable, but odd
        <Fact()>
        Public Sub EmitAssemblyWithDifferentName2()
            Dim name = "a"
            Dim extension = ".dll"
            Dim nameOverride = "b"

            Dim source =
<compilation name=<%= name %>>
    <file name="a.vb">
Class A
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseDll)
            compilation.VerifyDiagnostics()

            Dim assembly = compilation.Assembly
            Assert.Equal(name, assembly.Name)

            Dim [module] = assembly.Modules.Single()
            Assert.Equal(name & extension, [module].Name)

            Dim stream As New MemoryStream()
            Assert.True(compilation.Emit(stream, , options:=New EmitOptions(outputNameOverride:=nameOverride)).Success)

            Using metadata = ModuleMetadata.CreateFromImage(stream.ToImmutable())
                Dim metadataReader = metadata.Module.GetMetadataReader()

                Assert.True(metadataReader.IsAssembly)

                Assert.Equal(nameOverride, metadataReader.GetString(metadataReader.GetAssemblyDefinition().Name))
                Assert.Equal([module].Name, metadataReader.GetString(metadataReader.GetModuleDefinition().Name))
            End Using
        End Sub

        ' a to b.dll - allowable, but odd
        <Fact()>
        Public Sub EmitAssemblyWithDifferentName3()
            Dim name = "a"
            Dim extension = ".dll"
            Dim nameOverride = "b"

            Dim source =
<compilation name=<%= name %>>
    <file name="a.vb">
Class A
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseDll)
            compilation.VerifyDiagnostics()

            Dim assembly = compilation.Assembly
            Assert.Equal(name, assembly.Name)

            Dim [module] = assembly.Modules.Single()
            Assert.Equal(name & extension, [module].Name)

            Dim stream As New MemoryStream()
            Assert.True(compilation.Emit(stream, , options:=New EmitOptions(outputNameOverride:=nameOverride & extension)).Success)

            Using metadata = ModuleMetadata.CreateFromImage(stream.ToImmutable())
                Dim metadataReader = metadata.Module.GetMetadataReader()

                Assert.True(metadataReader.IsAssembly)

                Assert.Equal(nameOverride, metadataReader.GetString(metadataReader.GetAssemblyDefinition().Name))
                Assert.Equal([module].Name, metadataReader.GetString(metadataReader.GetModuleDefinition().Name))
            End Using
        End Sub

        ' a to b - allowable, but odd
        <Fact()>
        Public Sub EmitAssemblyWithDifferentName4()
            Dim name = "a"
            Dim extension = ".dll"
            Dim nameOverride = "b"

            Dim source =
<compilation name=<%= name %>>
    <file name="a.vb">
Class A
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseDll)
            compilation.VerifyDiagnostics()

            Dim assembly = compilation.Assembly
            Assert.Equal(name, assembly.Name)

            Dim [module] = assembly.Modules.Single()
            Assert.Equal(name & extension, [module].Name)

            Dim stream As New MemoryStream()
            Assert.True(compilation.Emit(stream, , options:=New EmitOptions(outputNameOverride:=nameOverride)).Success)

            Using metadata = ModuleMetadata.CreateFromImage(stream.ToImmutable())
                Dim metadataReader = metadata.Module.GetMetadataReader()

                Assert.True(metadataReader.IsAssembly)

                Assert.Equal(nameOverride, metadataReader.GetString(metadataReader.GetAssemblyDefinition().Name))
                Assert.Equal([module].Name, metadataReader.GetString(metadataReader.GetModuleDefinition().Name))
            End Using
        End Sub

        <Fact()>
        Public Sub IllegalNameOverride()
            Dim source =
<compilation>
    <file name="a.vb">
Class A
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseDll)
            compilation.VerifyDiagnostics()

            Dim result = compilation.Emit(New MemoryStream(), options:=New EmitOptions(outputNameOverride:=" "))
        End Sub

        <WorkItem(545084, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545084"), WorkItem(529492, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529492")>
        <Fact>
        Public Sub PermissionSetAttribute_Fixup()
            Dim tempDir = Temp.CreateDirectory()
            Dim tempFile = tempDir.CreateFile("pset.xml")

            Dim text = <![CDATA[
<PermissionSet class="System.Security.PermissionSet" version="1">
    <Permission class="System.Security.Permissions.FileIOPermission, mscorlib" version="1"><AllWindows/></Permission>
    <Permission class="System.Security.Permissions.RegistryPermission, mscorlib" version="1"><Unrestricted/></Permission>
</PermissionSet>]]>.Value

            tempFile.WriteAllText(text)

            Dim hexFileContent = PermissionSetAttributeWithFileReference.ConvertToHex(New MemoryStream(Encoding.UTF8.GetBytes(text)))

            Dim source =
<compilation>
    <file name="a.vb">
imports System.Security.Permissions

&lt;PermissionSetAttribute(SecurityAction.Deny, File:="pset.xml")&gt;
public class AClass
    public shared sub Main() 
        GetType(AClass).GetCustomAttributes(false)
    end sub
End Class
</file>
</compilation>

            Dim syntaxTree = CreateParseTree(source)
            Dim resolver = New XmlFileResolver(tempDir.Path)
            Dim comp = VisualBasicCompilation.Create(
                GetUniqueName(),
                {syntaxTree},
                {MscorlibRef},
                TestOptions.ReleaseDll.WithXmlReferenceResolver(resolver))

            comp.VerifyDiagnostics(
                Diagnostic(ERRID.WRN_UseOfObsoleteSymbol2, "SecurityAction.Deny").WithArguments(
                    "Deny",
                    "Deny is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."))

            Dim lengthHi = &H82
            Dim lengthLo = &H86

            CompileAndVerify(comp, symbolValidator:=
                             Sub([module] As ModuleSymbol)
                                 ValidateDeclSecurity([module], New DeclSecurityEntry() With {
                    .ActionFlags = DeclarativeSecurityAction.Deny,
                    .ParentKind = SymbolKind.NamedType,
                    .ParentNameOpt = "AClass",
                    .PermissionSet =
                        "." &
                        ChrW(&H1) &
                        ChrW(&H7F) &
                        "System.Security.Permissions.PermissionSetAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" &
                        ChrW(&H82) & ChrW(&H8F) &
                        ChrW(&H1) &
                        ChrW(&H54) &
                        ChrW(&HE) &
                        ChrW(&H3) &
                        "Hex" &
                        ChrW(lengthHi) & ChrW(lengthLo) &
                        hexFileContent})
                             End Sub)
        End Sub

        <WorkItem(545084, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545084"), WorkItem(529492, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529492")>
        <Fact>
        Public Sub PermissionSetAttributeInvalidFile()
            Dim source =
<compilation>
    <file name="a.vb">
imports System.Security.Permissions

&lt;PermissionSetAttribute(SecurityAction.Deny, File:="NonExistentFile.xml")&gt;
&lt;PermissionSetAttribute(SecurityAction.Deny, File:=nothing)&gt;
public class AClass 
end class
    </file>
</compilation>

            CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseDll.WithXmlReferenceResolver(XmlFileResolver.Default)).VerifyDiagnostics(
                    Diagnostic(ERRID.WRN_UseOfObsoleteSymbol2, "SecurityAction.Deny").WithArguments("Deny", "Deny is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."),
                    Diagnostic(ERRID.WRN_UseOfObsoleteSymbol2, "SecurityAction.Deny").WithArguments("Deny", "Deny is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."),
                    Diagnostic(ERRID.ERR_PermissionSetAttributeInvalidFile, "File:=""NonExistentFile.xml""").WithArguments("NonExistentFile.xml", "File"),
                    Diagnostic(ERRID.ERR_PermissionSetAttributeInvalidFile, "File:=nothing").WithArguments("<empty>", "File"))
        End Sub

        <Fact>
        Public Sub PermissionSetAttribute_NoResolver()
            Dim source =
<compilation>
    <file name="a.vb">
imports System.Security.Permissions

&lt;PermissionSetAttribute(SecurityAction.Deny, File:="NonExistentFile.xml")&gt;
public class AClass 
end class
    </file>
</compilation>

            CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseDll.WithXmlReferenceResolver(Nothing)).VerifyDiagnostics(
                Diagnostic(ERRID.WRN_UseOfObsoleteSymbol2, "SecurityAction.Deny").WithArguments("Deny", "Deny is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.").WithLocation(3, 25),
                Diagnostic(ERRID.ERR_PermissionSetAttributeInvalidFile, "File:=""NonExistentFile.xml""").WithArguments("NonExistentFile.xml", "File").WithLocation(3, 46))
        End Sub

        <WorkItem(546074, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546074")>
        <Fact()>
        Public Sub ReDimPreserve()
            Dim source =
<compilation>
    <file name="a.vb">
Module MainModule
    Sub Main()
        Dim o As Object
        ReDim Preserve o.M1(15)
    End Sub
End Module
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(source)
            CompileAndVerify(comp)
        End Sub

        <WorkItem(546074, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546074")>
        <Fact()>
        Public Sub ReDimPreserve2()
            Dim source =
<compilation>
    <file name="a.vb">
Module MainModule
    Sub Main()
        Dim oo(123) As Object
        ReDim Preserve oo(1).M1(15)
    End Sub
End Module
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(source)
            CompileAndVerify(comp)
        End Sub

        <WorkItem(546074, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546074")>
        <Fact()>
        Public Sub ReDimPreserve3()
            Dim source =
<compilation>
    <file name="a.vb">
Module MainModule
    Sub Main()
        ReDim Preserve F().M1(15)
    End Sub
    Function F() As Object
        Return Nothing
    End Function
End Module
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(source)
            CompileAndVerify(comp)
        End Sub

        <WorkItem(545084, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545084"), WorkItem(529492, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529492")>
        <Fact>
        Public Sub PermissionSetAttributeFileReadError()
            Dim tempDir = Temp.CreateDirectory()
            Dim filePath = Path.Combine(tempDir.Path, "pset_01.xml")

            Dim source =
<compilation>
    <file name="a.vb">
imports System.Security.Permissions

&lt;PermissionSetAttribute(SecurityAction.Deny, File:="pset_01.xml")&gt;
public class AClass
    public shared sub Main()
        GetType(AClass).GetCustomAttributes(false)
    end Sub
End Class
</file>
</compilation>

            Dim syntaxTree = CreateParseTree(source)
            Dim comp As VisualBasicCompilation

            ' create file with no file sharing allowed and verify ERR_PermissionSetAttributeFileReadError during emit
            Using File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None)
                comp = VisualBasicCompilation.Create(
                    GetUniqueName(),
                    {syntaxTree},
                    {MscorlibRef},
                    TestOptions.ReleaseDll.WithXmlReferenceResolver(New XmlFileResolver(Path.GetDirectoryName(filePath))))

                comp.VerifyDiagnostics(Diagnostic(ERRID.WRN_UseOfObsoleteSymbol2, "SecurityAction.Deny").WithArguments(
                    "Deny",
                    "Deny is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."))

                Using output = New MemoryStream()
                    Dim emitResult = comp.Emit(output)
                    Assert.False(emitResult.Success)

                    emitResult.Diagnostics.VerifyErrorCodes(
                        Diagnostic(ERRID.WRN_UseOfObsoleteSymbol2),
                        Diagnostic(ERRID.ERR_PermissionSetAttributeFileReadError))
                End Using
            End Using

            ' emit succeeds now since we closed the file:

            Using output = New MemoryStream()
                Dim emitResult = comp.Emit(output)
                Assert.True(emitResult.Success)
            End Using
        End Sub

        <WorkItem(654522, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/654522")>
        <Fact>
        Public Sub Bug654522()
            Dim refSource =
<compilation>
    <file name="a.vb">
Public Interface I(Of W As Structure) : End Interface
</file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40(refSource).VerifyDiagnostics()

            Dim metadataValidator As Action(Of ModuleSymbol) =
                Sub([module] As ModuleSymbol)
                    Dim metadata = DirectCast([module], PEModuleSymbol).Module

                    Dim typeI = DirectCast([module].GlobalNamespace.GetTypeMembers("I").Single(), PENamedTypeSymbol)
                    Assert.Equal(1, typeI.TypeParameters.Length)

                    Dim tp = DirectCast(typeI.TypeParameters(0), PETypeParameterSymbol)

                    Dim name As String = Nothing
                    Dim flags As GenericParameterAttributes = GenericParameterAttributes.None
                    metadata.GetGenericParamPropsOrThrow(tp.Handle, name, flags)

                    Assert.Equal(GenericParameterAttributes.DefaultConstructorConstraint,
                                 flags And GenericParameterAttributes.DefaultConstructorConstraint)

                    Dim metadataReader = metadata.MetadataReader
                    Dim constraints = metadataReader.GetGenericParameter(tp.Handle).GetConstraints()
                    Assert.Equal(1, constraints.Count)

                    Dim tokenDecoder = New MetadataDecoder(DirectCast([module], PEModuleSymbol), typeI)
                    Dim constraintTypeHandle = metadataReader.GetGenericParameterConstraint(constraints(0)).Type
                    Dim typeSymbol = tokenDecoder.GetTypeOfToken(constraintTypeHandle)
                    Assert.Equal(SpecialType.System_ValueType, typeSymbol.SpecialType)
                End Sub

            CompileAndVerify(comp, symbolValidator:=metadataValidator)
        End Sub

        <WorkItem(546450, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546450")>
        <Fact>
        Public Sub EmitNetModuleWithReferencedNetModule()
            Dim source1 =
<compilation>
    <file name="Source1.vb">
Public Class A
End Class
</file>
</compilation>

            Dim source2 =
<compilation>
    <file name="Source2.vb">
Public Class B
  Inherits A
End Class
</file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40(source1, OutputKind.NetModule)
            Dim metadataRef = comp.EmitToImageReference()
            CompileAndVerify(source2, references:={metadataRef}, options:=TestOptions.ReleaseModule, verify:=Verification.Fails)
        End Sub

        <Fact>
        Public Sub PlatformMismatch_01()
            Dim refSource =
<compilation name="PlatformMismatch">
    <file name="a.vb">
public interface ITestPlatform
End interface
</file>
</compilation>

            Dim refCompilation = CreateEmptyCompilationWithReferences(refSource, New MetadataReference() {}, TestOptions.ReleaseDll.WithPlatform(Platform.Itanium))

            refCompilation.VerifyEmitDiagnostics()
            Dim compRef = New VisualBasicCompilationReference(refCompilation)
            Dim imageRef = refCompilation.EmitToImageReference()

            Dim useSource =
<compilation>
    <file name="b.vb">
public interface IUsePlatform
    Function M() As ITestPlatform
End interface
</file>
</compilation>

            Dim useCompilation = CreateEmptyCompilationWithReferences(useSource,
                {compRef},
                TestOptions.ReleaseDll.WithPlatform(Platform.AnyCpu))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics, <expected></expected>)

            useCompilation = CreateEmptyCompilationWithReferences(useSource,
                {imageRef},
                TestOptions.ReleaseDll.WithPlatform(Platform.AnyCpu))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics, <expected></expected>)

            useCompilation = CreateEmptyCompilationWithReferences(useSource,
                {compRef},
                TestOptions.ReleaseModule.WithPlatform(Platform.AnyCpu))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics, <expected></expected>)

            useCompilation = CreateEmptyCompilationWithReferences(useSource,
                {imageRef},
                TestOptions.ReleaseModule.WithPlatform(Platform.AnyCpu))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics, <expected></expected>)

            useCompilation = CreateEmptyCompilationWithReferences(useSource,
                {compRef},
                TestOptions.ReleaseDll.WithPlatform(Platform.X86))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics,
<expected>
BC42372: Referenced assembly 'PlatformMismatch, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' targets a different processor.
</expected>)

            useCompilation = CreateEmptyCompilationWithReferences(useSource,
                {imageRef},
                TestOptions.ReleaseDll.WithPlatform(Platform.X86))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics,
<expected>
BC42372: Referenced assembly 'PlatformMismatch, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' targets a different processor.
</expected>)

            useCompilation = CreateEmptyCompilationWithReferences(useSource,
                {compRef},
                TestOptions.ReleaseModule.WithPlatform(Platform.X86))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics,
<expected>
BC42372: Referenced assembly 'PlatformMismatch, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' targets a different processor.
</expected>)

            useCompilation = CreateEmptyCompilationWithReferences(useSource,
                {imageRef},
                TestOptions.ReleaseModule.WithPlatform(Platform.X86))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics,
<expected>
BC42372: Referenced assembly 'PlatformMismatch, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' targets a different processor.
</expected>)

            'confirm that shutting off 40010, turns off the new alink warning 42372
            Dim warns = New Dictionary(Of String, ReportDiagnostic)
            warns.Add(MessageProvider.Instance.GetIdForErrorCode(40010), ReportDiagnostic.Suppress)
            useCompilation = useCompilation.WithOptions(useCompilation.Options.WithSpecificDiagnosticOptions(warns))
            Assert.Empty(useCompilation.Emit(New MemoryStream()).Diagnostics)
        End Sub

        <Fact>
        Public Sub PlatformMismatch_02()
            Dim refSource =
<compilation name="PlatformMismatch">
    <file name="a.vb">
public interface ITestPlatform
End interface
</file>
</compilation>

            Dim refCompilation = CreateEmptyCompilationWithReferences(refSource, New MetadataReference() {}, TestOptions.ReleaseModule.WithPlatform(Platform.Itanium))

            refCompilation.VerifyEmitDiagnostics()
            Dim imageRef = refCompilation.EmitToImageReference()

            Dim useSource =
<compilation>
    <file name="b.vb">
public interface IUsePlatform
    Function M() As ITestPlatform
End interface
</file>
</compilation>

            Dim useCompilation = CreateEmptyCompilationWithReferences(useSource,
                {imageRef},
                TestOptions.ReleaseDll.WithPlatform(Platform.AnyCpu))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics,
<expected>
BC37213: Agnostic assembly cannot have a processor specific module 'PlatformMismatch.netmodule'.
</expected>)

            useCompilation = CreateEmptyCompilationWithReferences(useSource,
                {imageRef},
                TestOptions.ReleaseDll.WithPlatform(Platform.X86))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics,
<expected>
BC37214: Assembly and module 'PlatformMismatch.netmodule' cannot target different processors.
</expected>)

            useCompilation = CreateEmptyCompilationWithReferences(useSource,
                {imageRef},
                TestOptions.ReleaseModule.WithPlatform(Platform.X86))

            ' No BC37213 when building a module and adding a conflicting one
            useCompilation.VerifyEmitDiagnostics()

        End Sub

        <Fact>
        Public Sub PlatformMismatch_03()
            Dim refSource =
<compilation name="PlatformMismatch">
    <file name="a.vb">
public interface ITestPlatform
End interface
</file>
</compilation>

            Dim refCompilation = CreateEmptyCompilationWithReferences(refSource, New MetadataReference() {}, TestOptions.ReleaseDll.WithPlatform(Platform.X86))

            refCompilation.VerifyEmitDiagnostics()
            Dim compRef = New VisualBasicCompilationReference(refCompilation)
            Dim imageRef = refCompilation.EmitToImageReference()

            Dim useSource =
<compilation>
    <file name="b.vb">
public interface IUsePlatform
    Function M() As ITestPlatform
End interface
</file>
</compilation>

            Dim useCompilation = CreateEmptyCompilationWithReferences(useSource,
                {compRef},
                TestOptions.ReleaseDll.WithPlatform(Platform.Itanium))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics,
<expected>
BC42372: Referenced assembly 'PlatformMismatch, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' targets a different processor.
</expected>)

            useCompilation = CreateEmptyCompilationWithReferences(useSource,
                {imageRef},
                TestOptions.ReleaseDll.WithPlatform(Platform.Itanium))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics,
<expected>
BC42372: Referenced assembly 'PlatformMismatch, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' targets a different processor.
</expected>)

            useCompilation = CreateEmptyCompilationWithReferences(useSource,
                {compRef},
                TestOptions.ReleaseModule.WithPlatform(Platform.Itanium))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics,
<expected>
BC42372: Referenced assembly 'PlatformMismatch, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' targets a different processor.
</expected>)

            useCompilation = CreateEmptyCompilationWithReferences(useSource,
                {imageRef},
                TestOptions.ReleaseModule.WithPlatform(Platform.Itanium))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics,
<expected>
BC42372: Referenced assembly 'PlatformMismatch, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' targets a different processor.
</expected>)
        End Sub

        <Fact>
        Public Sub PlatformMismatch_04()
            Dim refSource =
<compilation name="PlatformMismatch">
    <file name="a.vb">
public interface ITestPlatform
End interface
</file>
</compilation>

            Dim refCompilation = CreateEmptyCompilationWithReferences(refSource, New MetadataReference() {}, TestOptions.ReleaseModule.WithPlatform(Platform.X86))

            refCompilation.VerifyEmitDiagnostics()
            Dim imageRef = refCompilation.EmitToImageReference()

            Dim useSource =
<compilation>
    <file name="b.vb">
public interface IUsePlatform
    Function M() As ITestPlatform
End interface
</file>
</compilation>

            Dim useCompilation = CreateEmptyCompilationWithReferences(useSource,
                {imageRef},
                TestOptions.ReleaseDll.WithPlatform(Platform.Itanium))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics,
<expected>
BC37214: Assembly and module 'PlatformMismatch.netmodule' cannot target different processors.
</expected>)
        End Sub

        <Fact>
        Public Sub PlatformMismatch_05()
            Dim refSource =
<compilation name="PlatformMismatch">
    <file name="a.vb">
public interface ITestPlatform
End interface
</file>
</compilation>

            Dim refCompilation = CreateEmptyCompilationWithReferences(refSource, New MetadataReference() {}, TestOptions.ReleaseDll.WithPlatform(Platform.AnyCpu))

            refCompilation.VerifyEmitDiagnostics()
            Dim compRef = New VisualBasicCompilationReference(refCompilation)
            Dim imageRef = refCompilation.EmitToImageReference()

            Dim useSource =
<compilation>
    <file name="b.vb">
public interface IUsePlatform
    Function M() As ITestPlatform
End interface
</file>
</compilation>

            Dim useCompilation = CreateEmptyCompilationWithReferences(useSource,
                {compRef},
                TestOptions.ReleaseDll.WithPlatform(Platform.Itanium))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics,
<expected>
</expected>)

            useCompilation = CreateEmptyCompilationWithReferences(useSource,
                {imageRef},
                TestOptions.ReleaseDll.WithPlatform(Platform.Itanium))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics,
<expected>
</expected>)

            useCompilation = CreateEmptyCompilationWithReferences(useSource,
                {compRef},
                TestOptions.ReleaseModule.WithPlatform(Platform.Itanium))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics,
<expected>
</expected>)

            useCompilation = CreateEmptyCompilationWithReferences(useSource,
                {imageRef},
                TestOptions.ReleaseModule.WithPlatform(Platform.Itanium))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics,
<expected>
</expected>)
        End Sub

        <Fact>
        Public Sub PlatformMismatch_06()
            Dim refSource =
<compilation name="PlatformMismatch">
    <file name="a.vb">
public interface ITestPlatform
End interface
</file>
</compilation>

            Dim refCompilation = CreateEmptyCompilationWithReferences(refSource, New MetadataReference() {}, TestOptions.ReleaseModule.WithPlatform(Platform.AnyCpu))

            refCompilation.VerifyEmitDiagnostics()
            Dim imageRef = refCompilation.EmitToImageReference()

            Dim useSource =
<compilation>
    <file name="b.vb">
public interface IUsePlatform
    Function M() As ITestPlatform
End interface
</file>
</compilation>

            Dim useCompilation = CreateEmptyCompilationWithReferences(useSource,
                {imageRef},
                TestOptions.ReleaseDll.WithPlatform(Platform.Itanium))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics,
<expected>
</expected>)
        End Sub

        <Fact>
        Public Sub PlatformMismatch_07()
            Dim refSource =
<compilation name="PlatformMismatch">
    <file name="a.vb">
public interface ITestPlatform
End interface
</file>
</compilation>

            Dim refCompilation = CreateEmptyCompilationWithReferences(refSource, New MetadataReference() {}, TestOptions.ReleaseDll.WithPlatform(Platform.Itanium))

            refCompilation.VerifyEmitDiagnostics()
            Dim compRef = New VisualBasicCompilationReference(refCompilation)
            Dim imageRef = refCompilation.EmitToImageReference()

            Dim useSource =
<compilation>
    <file name="b.vb">
public interface IUsePlatform
    Function M() As ITestPlatform
End interface
</file>
</compilation>

            Dim useCompilation = CreateEmptyCompilationWithReferences(useSource,
                {compRef},
                TestOptions.ReleaseDll.WithPlatform(Platform.Itanium))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics,
<expected>
</expected>)

            useCompilation = CreateEmptyCompilationWithReferences(useSource,
                {imageRef},
                TestOptions.ReleaseDll.WithPlatform(Platform.Itanium))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics,
<expected>
</expected>)

            useCompilation = CreateEmptyCompilationWithReferences(useSource,
                {compRef},
                TestOptions.ReleaseModule.WithPlatform(Platform.Itanium))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics,
<expected>
</expected>)

            useCompilation = CreateEmptyCompilationWithReferences(useSource,
                {imageRef},
                TestOptions.ReleaseModule.WithPlatform(Platform.Itanium))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics,
<expected>
</expected>)
        End Sub

        <Fact>
        Public Sub PlatformMismatch_08()
            Dim refSource =
<compilation name="PlatformMismatch">
    <file name="a.vb">
public interface ITestPlatform
End interface
</file>
</compilation>

            Dim refCompilation = CreateEmptyCompilationWithReferences(refSource, New MetadataReference() {}, TestOptions.ReleaseModule.WithPlatform(Platform.Itanium))

            refCompilation.VerifyEmitDiagnostics()
            Dim imageRef = refCompilation.EmitToImageReference()

            Dim useSource =
<compilation>
    <file name="b.vb">
public interface IUsePlatform
    Function M() As ITestPlatform
End interface
</file>
</compilation>

            Dim useCompilation = CreateEmptyCompilationWithReferences(useSource,
                {imageRef},
                TestOptions.ReleaseDll.WithPlatform(Platform.Itanium))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics,
<expected>
</expected>)
        End Sub

        <Fact, WorkItem(769741, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/769741"), WorkItem(1001945, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1001945")>
        Public Sub Bug769741()
            Dim source =
<compilation>
    <file name="a.vb">
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {TestReferences.SymbolsTests.netModule.x64COFF}, TestOptions.DebugDll)

            CompileAndVerify(compilation, verify:=Verification.Fails)
            Assert.NotSame(compilation.Assembly.CorLibrary, compilation.Assembly)
            compilation.GetSpecialType(SpecialType.System_Int32)
        End Sub

        <Fact>
        Public Sub FoldMethods()

            Dim useSource =
<compilation>
    <file name="b.vb">
        Class Viewable
            Sub Maine()
                Dim v = New Viewable()
                Dim x = v.P1
                Dim y = x AndAlso v.P2
            End Sub


            ReadOnly Property P1 As Boolean
                Get
                    Return True
                End Get
            End Property

            ReadOnly Property P2 As Boolean
                Get
                    Return True
                End Get
            End Property
        End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40(useSource, options:=TestOptions.ReleaseDll)
            Dim metadataReader = ModuleMetadata.CreateFromImage(compilation.EmitToArray()).Module.GetMetadataReader()

            Dim P1RVA = 0
            Dim P2RVA = 0

            For Each handle In metadataReader.TypeDefinitions

                Dim typeDef = metadataReader.GetTypeDefinition(handle)

                If (metadataReader.StringComparer.Equals(typeDef.Name, "Viewable")) Then
                    For Each m In typeDef.GetMethods()
                        Dim method = metadataReader.GetMethodDefinition(m)
                        If (metadataReader.StringComparer.Equals(method.Name, "get_P1")) Then
                            P1RVA = method.RelativeVirtualAddress
                        End If
                        If (metadataReader.StringComparer.Equals(method.Name, "get_P2")) Then
                            P2RVA = method.RelativeVirtualAddress
                        End If
                    Next
                End If
            Next

            Assert.NotEqual(0, P1RVA)
            Assert.Equal(P2RVA, P1RVA)
        End Sub

        Private Shared Function SequenceMatches(buffer As Byte(), startIndex As Integer, pattern As Byte()) As Boolean
            For i = 0 To pattern.Length - 1
                If buffer(startIndex + i) <> pattern(i) Then Return False
            Next
            Return True
        End Function

        Private Shared Function IndexOfPattern(buffer As Byte(), startIndex As Integer, pattern As Byte()) As Integer
            Dim [end] = buffer.Length - pattern.Length
            For i = startIndex To [end] - 1
                If SequenceMatches(buffer, i, pattern) Then Return i
            Next
            Return -1
        End Function

        <Fact, WorkItem(1669, "https://github.com/dotnet/roslyn/issues/1669")>
        Public Sub FoldMethods2()
            ' Verifies that IL folding eliminates duplicate copies of small method bodies by
            ' examining the emitted binary.
            Dim source =
<compilation>
    <file name="a.vb">
Class C
    Function M() As ULong
        Return &amp;H8675309ABCDE4225UL
    End Function
    ReadOnly Property P As Long 
        Get
            Return -8758040459200282075
        End Get
    End Property
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseDll)
            Using stream As Stream = compilation.EmitToStream()
                Dim len As Integer = CType(stream.Length, Integer)
                Dim bytes(len) As Byte
                Assert.Equal(len, stream.Read(bytes, 0, len))

                ' The constant should appear exactly once
                Dim pattern() As Byte = {&H25, &H42, &HDE, &HBC, &H9A, &H30, &H75, &H86}
                Dim firstMatch = IndexOfPattern(bytes, 0, pattern)
                Assert.True(firstMatch >= 0, "Couldn't find the expected byte pattern in the output.")
                Dim secondMatch = IndexOfPattern(bytes, firstMatch + 1, pattern)
                Assert.True(secondMatch < 0, "Expected to find just one occurrence of the pattern in the output.")
            End Using

        End Sub

        ''' <summary>
        ''' Ordering of anonymous type definitions
        ''' in metadata should be deterministic.
        ''' </summary>
        <Fact>
        Public Sub AnonymousTypeMetadataOrder()
            Dim source =
<compilation>
    <file name="a.vb">
Class C1
    Private F As Object = New With {.C = 1, .D = 2}
End Class
Class C2
    Private F As Object = New With {.A = 3, .B = 4}
End Class
Class C3
    Private F As Object = New With {.AB = 5}
End Class
Class C4
    Private F As Object = New With {.E = 6, .F = 2}
End Class
Class C5
    Private F As Object = New With {.E = 7, .F = 8}
End Class
Class C6
    Private F As Object = New With {.AB = 9}
End Class
    </file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseDll)
            Dim bytes = compilation.EmitToArray()
            Using metadata = ModuleMetadata.CreateFromImage(bytes)
                Dim reader = metadata.MetadataReader
                Dim actualNames = reader.GetTypeDefNames().Select(Function(h) reader.GetString(h))
                Dim expectedNames = {
                    "<Module>",
                    "VB$AnonymousType_0`2",
                    "VB$AnonymousType_1`2",
                    "VB$AnonymousType_2`1",
                    "VB$AnonymousType_3`2",
                    "C1",
                    "C2",
                    "C3",
                    "C4",
                    "C5",
                    "C6"
                    }
                AssertEx.Equal(expectedNames, actualNames)
            End Using
        End Sub

        <Fact>
        Public Sub FailingEmitter()
            ' Check that Compilation.Emit actually produces compilation errors.
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Module M1
    Sub Main()
    End Sub
End Module
    </file>
    </compilation>)

            Dim emitResult As EmitResult

            Using output = New BrokenStream()
                output.BreakHow = BrokenStream.BreakHowType.ThrowOnWrite
                emitResult = compilation.Emit(output, Nothing, Nothing, Nothing)

                CompilationUtils.AssertTheseDiagnostics(emitResult.Diagnostics,
<expected>
BC37256: An error occurred while writing the output file: <%= output.ThrownException.ToString() %>
</expected>)
            End Using
        End Sub

        <Fact>
        <WorkItem(11691, "https://github.com/dotnet/roslyn/issues/11691")>
        Public Sub ObsoleteAttributeOverride()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib45AndVBRuntime(
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
imports System

Public MustInherit Class BaseClass(of T)
    Public MustOverride Function Method(input As T) As Integer
End Class
    
Public Class DerivingClass(Of T) 
    Inherits BaseClass(Of T)
    <Obsolete("Deprecated")>
    Public Overrides Sub Method(input As T)
        Throw New NotImplementedException()
    End Sub
End Class
]]>
                    </file>
                </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation.GetDiagnostics(),
<expected>
    BC30437: 'Public Overrides Sub Method(input As T)' cannot override 'Public MustOverride Function Method(input As T) As Integer' because they differ by their return types.
    Public Overrides Sub Method(input As T)
                         ~~~~~~
</expected>)
        End Sub
    End Class
End Namespace
