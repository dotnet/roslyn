' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.IO
Imports System.Reflection
Imports System.Reflection.Metadata
Imports System.Reflection.Metadata.Ecma335
Imports System.Reflection.PortableExecutable
Imports System.Text
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.PDB
    Public Class PDBTests
        Inherits BasicTestBase

#Region "General"

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub EmitDebugInfoForSourceTextWithoutEncoding1()
            Dim tree1 = SyntaxFactory.ParseSyntaxTree("Class A : End Class", path:="Goo.vb", encoding:=Nothing)
            Dim tree2 = SyntaxFactory.ParseSyntaxTree("Class B : End Class", path:="", encoding:=Nothing)
            Dim tree3 = SyntaxFactory.ParseSyntaxTree(SourceText.From("Class C : End Class", encoding:=Nothing), path:="Bar.vb")
            Dim tree4 = SyntaxFactory.ParseSyntaxTree("Class D : End Class", path:="Baz.vb", encoding:=Encoding.UTF8)

            Dim comp = VisualBasicCompilation.Create("Compilation", {tree1, tree2, tree3, tree4}, {MscorlibRef}, options:=TestOptions.ReleaseDll)

            Dim result = comp.Emit(New MemoryStream(), pdbStream:=New MemoryStream())
            result.Diagnostics.Verify(
                Diagnostic(ERRID.ERR_EncodinglessSyntaxTree, "Class A : End Class").WithLocation(1, 1),
                Diagnostic(ERRID.ERR_EncodinglessSyntaxTree, "Class C : End Class").WithLocation(1, 1))

            Assert.False(result.Success)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub EmitDebugInfoForSourceTextWithoutEncoding2()
            Dim tree1 = SyntaxFactory.ParseSyntaxTree("Class A" & vbCrLf & "Sub F() : End Sub : End Class", path:="Goo.vb", encoding:=Encoding.Unicode)
            Dim tree2 = SyntaxFactory.ParseSyntaxTree("Class B" & vbCrLf & "Sub F() : End Sub : End Class", path:="", encoding:=Nothing)
            Dim tree3 = SyntaxFactory.ParseSyntaxTree("Class C" & vbCrLf & "Sub F() : End Sub : End Class", path:="Bar.vb", encoding:=New UTF8Encoding(True, False))
            Dim tree4 = SyntaxFactory.ParseSyntaxTree(SourceText.From("Class D" & vbCrLf & "Sub F() : End Sub : End Class", New UTF8Encoding(False, False)), path:="Baz.vb")

            Dim comp = VisualBasicCompilation.Create("Compilation", {tree1, tree2, tree3, tree4}, {MscorlibRef}, options:=TestOptions.ReleaseDll)

            Dim result = comp.Emit(New MemoryStream(), pdbStream:=New MemoryStream())
            result.Diagnostics.Verify()
            Assert.True(result.Success)

            Dim hash1 = CryptographicHashProvider.ComputeSha1(Encoding.Unicode.GetBytesWithPreamble(tree1.ToString())).ToArray()
            Dim hash3 = CryptographicHashProvider.ComputeSha1(New UTF8Encoding(True, False).GetBytesWithPreamble(tree3.ToString())).ToArray()
            Dim hash4 = CryptographicHashProvider.ComputeSha1(New UTF8Encoding(False, False).GetBytesWithPreamble(tree4.ToString())).ToArray()

            comp.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="Goo.vb" language="VB" checksumAlgorithm="SHA1" checksum=<%= BitConverter.ToString(hash1) %>/>
        <file id="2" name="" language="VB"/>
        <file id="3" name="Bar.vb" language="VB" checksumAlgorithm="SHA1" checksum=<%= BitConverter.ToString(hash3) %>/>
        <file id="4" name="Baz.vb" language="VB" checksumAlgorithm="SHA1" checksum=<%= BitConverter.ToString(hash4) %>/>
    </files>
</symbols>, options:=PdbValidationOptions.ExcludeMethods)

        End Sub

        <Fact>
        Public Sub EmitDebugInfoForSynthesizedSyntaxTree()
            Dim tree1 = SyntaxFactory.ParseCompilationUnit("
#ExternalSource(""test.vb"", 1)  
Class C
  Sub M
  End Sub
End Class
#End ExternalSource
").SyntaxTree
            Dim tree2 = SyntaxFactory.ParseCompilationUnit("
Class D
  Sub M
  End Sub
End Class
").SyntaxTree

            Dim comp = VisualBasicCompilation.Create("test", {tree1, tree2}, TargetFrameworkUtil.StandardReferences, TestOptions.DebugDll)

            Dim result = comp.Emit(New MemoryStream(), pdbStream:=New MemoryStream())
            result.Diagnostics.Verify()

            comp.VerifyPdb("
<symbols>
  <files>
    <file id=""1"" name="""" language=""VB"" />
    <file id=""2"" name=""test.vb"" language=""VB"" />
  </files>
</symbols>
", format:=DebugInformationFormat.PortablePdb, options:=PdbValidationOptions.ExcludeMethods)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub CustomDebugEntryPoint_DLL()
            Dim source = "
Class C 
  Shared Sub F()
  End Sub
End Class
"
            Dim c = CreateCompilationWithMscorlib40({source}, options:=TestOptions.DebugDll)

            Dim f = c.GetMember(Of MethodSymbol)("C.F")
            c.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <entryPoint declaringType="C" methodName="F"/>
    <methods/>
</symbols>, debugEntryPoint:=f, options:=PdbValidationOptions.ExcludeScopes Or PdbValidationOptions.ExcludeSequencePoints Or PdbValidationOptions.ExcludeCustomDebugInformation)

            Dim peReader = New PEReader(c.EmitToArray(debugEntryPoint:=f))
            Dim peEntryPointToken = peReader.PEHeaders.CorHeader.EntryPointTokenOrRelativeVirtualAddress
            Assert.Equal(0, peEntryPointToken)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub CustomDebugEntryPoint_EXE()
            Dim source = "
Class M 
  Shared Sub Main() 
  End Sub
End Class

Class C 
  Shared Sub F(Of S)()
  End Sub
End Class
"
            Dim c = CreateCompilationWithMscorlib40({source}, options:=TestOptions.DebugExe)

            Dim f = c.GetMember(Of MethodSymbol)("C.F")
            c.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <entryPoint declaringType="C" methodName="F"/>
    <methods/>
</symbols>, debugEntryPoint:=f, options:=PdbValidationOptions.ExcludeScopes Or PdbValidationOptions.ExcludeSequencePoints Or PdbValidationOptions.ExcludeCustomDebugInformation)

            Dim peReader = New PEReader(c.EmitToArray(debugEntryPoint:=f))
            Dim peEntryPointToken = peReader.PEHeaders.CorHeader.EntryPointTokenOrRelativeVirtualAddress

            Dim mdReader = peReader.GetMetadataReader()
            Dim methodDef = mdReader.GetMethodDefinition(CType(MetadataTokens.Handle(peEntryPointToken), MethodDefinitionHandle))

            Assert.Equal("Main", mdReader.GetString(methodDef.Name))
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub CustomDebugEntryPoint_Errors()
            Dim source1 = "
Class C 
  Shared Sub F
  End Sub
End Class 

Class D(Of T)
  Shared Sub G(Of S)()
  End Sub
End Class
"
            Dim source2 = "
Class C 
  Shared Sub F() 
  End Sub
End Class
"
            Dim c1 = CreateCompilationWithMscorlib40({source1}, options:=TestOptions.DebugDll)
            Dim c2 = CreateCompilationWithMscorlib40({source2}, options:=TestOptions.DebugDll)

            Dim f1 = c1.GetMember(Of MethodSymbol)("C.F")
            Dim f2 = c2.GetMember(Of MethodSymbol)("C.F")
            Dim g = c1.GetMember(Of MethodSymbol)("D.G")
            Dim d = c1.GetMember(Of NamedTypeSymbol)("D")

            Assert.NotNull(f1)
            Assert.NotNull(f2)
            Assert.NotNull(g)
            Assert.NotNull(d)

            Dim stInt = c1.GetSpecialType(SpecialType.System_Int32)
            Dim d_t_g_int = g.Construct(stInt)
            Dim d_int = d.Construct(stInt)
            Dim d_int_g = d_int.GetMember(Of MethodSymbol)("G")
            Dim d_int_g_int = d_int_g.Construct(stInt)

            Dim result = c1.Emit(New MemoryStream(), New MemoryStream(), debugEntryPoint:=f2)
            result.Diagnostics.Verify(Diagnostic(ERRID.ERR_DebugEntryPointNotSourceMethodDefinition))

            result = c1.Emit(New MemoryStream(), New MemoryStream(), debugEntryPoint:=d_t_g_int)
            result.Diagnostics.Verify(Diagnostic(ERRID.ERR_DebugEntryPointNotSourceMethodDefinition))

            result = c1.Emit(New MemoryStream(), New MemoryStream(), debugEntryPoint:=d_int_g)
            result.Diagnostics.Verify(Diagnostic(ERRID.ERR_DebugEntryPointNotSourceMethodDefinition))

            result = c1.Emit(New MemoryStream(), New MemoryStream(), debugEntryPoint:=d_int_g_int)
            result.Diagnostics.Verify(Diagnostic(ERRID.ERR_DebugEntryPointNotSourceMethodDefinition))
        End Sub

#End Region

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub TestBasic()
            Dim source =
<compilation>
    <file><![CDATA[
Class C1
    Sub Method()
        System.Console.WriteLine("Hello, world.")
    End Sub
End Class
]]></file>
</compilation>

            Dim defines = PredefinedPreprocessorSymbols.AddPredefinedPreprocessorSymbols(OutputKind.ConsoleApplication)
            defines = defines.Add(KeyValuePairUtil.Create("_MyType", CObj("Console")))

            Dim parseOptions = New VisualBasicParseOptions(preprocessorSymbols:=defines)

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.DebugDll.WithParseOptions(parseOptions))

            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="My.MyComputer" name=".ctor">
            <sequencePoints>
                <entry offset="0x0" startLine="109" startColumn="9" endLine="109" endColumn="25" document="1"/>
                <entry offset="0x1" startLine="110" startColumn="13" endLine="110" endColumn="25" document="1"/>
                <entry offset="0x8" startLine="111" startColumn="9" endLine="111" endColumn="16" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x9">
                <currentnamespace name="My"/>
            </scope>
        </method>
        <method containingType="My.MyProject" name=".cctor">
            <sequencePoints>
                <entry offset="0x0" startLine="128" startColumn="26" endLine="128" endColumn="97" document="1"/>
                <entry offset="0xa" startLine="139" startColumn="26" endLine="139" endColumn="95" document="1"/>
                <entry offset="0x14" startLine="150" startColumn="26" endLine="150" endColumn="136" document="1"/>
                <entry offset="0x1e" startLine="286" startColumn="26" endLine="286" endColumn="105" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x29">
                <importsforward declaringType="My.MyComputer" methodName=".ctor"/>
            </scope>
        </method>
        <method containingType="My.MyProject" name="get_Computer">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="123" startColumn="13" endLine="123" endColumn="16" document="1"/>
                <entry offset="0x1" startLine="124" startColumn="17" endLine="124" endColumn="62" document="1"/>
                <entry offset="0xe" startLine="125" startColumn="13" endLine="125" endColumn="20" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <importsforward declaringType="My.MyComputer" methodName=".ctor"/>
                <local name="Computer" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject" name="get_Application">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="135" startColumn="13" endLine="135" endColumn="16" document="1"/>
                <entry offset="0x1" startLine="136" startColumn="17" endLine="136" endColumn="57" document="1"/>
                <entry offset="0xe" startLine="137" startColumn="13" endLine="137" endColumn="20" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <importsforward declaringType="My.MyComputer" methodName=".ctor"/>
                <local name="Application" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject" name="get_User">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="146" startColumn="13" endLine="146" endColumn="16" document="1"/>
                <entry offset="0x1" startLine="147" startColumn="17" endLine="147" endColumn="58" document="1"/>
                <entry offset="0xe" startLine="148" startColumn="13" endLine="148" endColumn="20" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <importsforward declaringType="My.MyComputer" methodName=".ctor"/>
                <local name="User" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject" name="get_WebServices">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="239" startColumn="14" endLine="239" endColumn="17" document="1"/>
                <entry offset="0x1" startLine="240" startColumn="17" endLine="240" endColumn="67" document="1"/>
                <entry offset="0xe" startLine="241" startColumn="13" endLine="241" endColumn="20" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <importsforward declaringType="My.MyComputer" methodName=".ctor"/>
                <local name="WebServices" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="C1" name="Method">
            <sequencePoints>
                <entry offset="0x0" startLine="2" startColumn="5" endLine="2" endColumn="17" document="1"/>
                <entry offset="0x1" startLine="3" startColumn="9" endLine="3" endColumn="50" document="1"/>
                <entry offset="0xc" startLine="4" startColumn="5" endLine="4" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xd">
                <currentnamespace name=""/>
            </scope>
        </method>
        <method containingType="My.MyProject+MyWebServices" name="Equals" parameterNames="o">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="249" startColumn="13" endLine="249" endColumn="75" document="1"/>
                <entry offset="0x1" startLine="250" startColumn="17" endLine="250" endColumn="40" document="1"/>
                <entry offset="0x10" startLine="251" startColumn="13" endLine="251" endColumn="25" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x12">
                <currentnamespace name="My"/>
                <local name="Equals" il_index="0" il_start="0x0" il_end="0x12" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject+MyWebServices" name="GetHashCode">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="253" startColumn="13" endLine="253" endColumn="63" document="1"/>
                <entry offset="0x1" startLine="254" startColumn="17" endLine="254" endColumn="42" document="1"/>
                <entry offset="0xa" startLine="255" startColumn="13" endLine="255" endColumn="25" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xc">
                <importsforward declaringType="My.MyProject+MyWebServices" methodName="Equals" parameterNames="o"/>
                <local name="GetHashCode" il_index="0" il_start="0x0" il_end="0xc" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject+MyWebServices" name="GetType">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="257" startColumn="13" endLine="257" endColumn="72" document="1"/>
                <entry offset="0x1" startLine="258" startColumn="17" endLine="258" endColumn="46" document="1"/>
                <entry offset="0xe" startLine="259" startColumn="13" endLine="259" endColumn="25" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <importsforward declaringType="My.MyProject+MyWebServices" methodName="Equals" parameterNames="o"/>
                <local name="GetType" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject+MyWebServices" name="ToString">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="261" startColumn="13" endLine="261" endColumn="59" document="1"/>
                <entry offset="0x1" startLine="262" startColumn="17" endLine="262" endColumn="39" document="1"/>
                <entry offset="0xa" startLine="263" startColumn="13" endLine="263" endColumn="25" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xc">
                <importsforward declaringType="My.MyProject+MyWebServices" methodName="Equals" parameterNames="o"/>
                <local name="ToString" il_index="0" il_start="0x0" il_end="0xc" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject+MyWebServices" name="Create__Instance__" parameterNames="instance">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                    <slot kind="1" offset="0"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="266" startColumn="12" endLine="266" endColumn="95" document="1"/>
                <entry offset="0x1" startLine="267" startColumn="17" endLine="267" endColumn="44" document="1"/>
                <entry offset="0xb" hidden="true" document="1"/>
                <entry offset="0xe" startLine="268" startColumn="21" endLine="268" endColumn="35" document="1"/>
                <entry offset="0x16" startLine="269" startColumn="17" endLine="269" endColumn="21" document="1"/>
                <entry offset="0x17" startLine="270" startColumn="21" endLine="270" endColumn="36" document="1"/>
                <entry offset="0x1b" startLine="272" startColumn="13" endLine="272" endColumn="25" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x1d">
                <importsforward declaringType="My.MyProject+MyWebServices" methodName="Equals" parameterNames="o"/>
                <local name="Create__Instance__" il_index="0" il_start="0x0" il_end="0x1d" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject+MyWebServices" name="Dispose__Instance__" parameterNames="instance">
            <sequencePoints>
                <entry offset="0x0" startLine="275" startColumn="13" endLine="275" endColumn="71" document="1"/>
                <entry offset="0x1" startLine="276" startColumn="17" endLine="276" endColumn="35" document="1"/>
                <entry offset="0x8" startLine="277" startColumn="13" endLine="277" endColumn="20" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x9">
                <importsforward declaringType="My.MyProject+MyWebServices" methodName="Equals" parameterNames="o"/>
            </scope>
        </method>
        <method containingType="My.MyProject+MyWebServices" name=".ctor">
            <sequencePoints>
                <entry offset="0x0" startLine="281" startColumn="13" endLine="281" endColumn="29" document="1"/>
                <entry offset="0x1" startLine="282" startColumn="16" endLine="282" endColumn="28" document="1"/>
                <entry offset="0x8" startLine="283" startColumn="13" endLine="283" endColumn="20" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x9">
                <importsforward declaringType="My.MyProject+MyWebServices" methodName="Equals" parameterNames="o"/>
            </scope>
        </method>
        <method containingType="My.MyProject+ThreadSafeObjectProvider`1" name="get_GetInstance">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                    <slot kind="1" offset="0"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="343" startColumn="17" endLine="343" endColumn="20" document="1"/>
                <entry offset="0x1" startLine="344" startColumn="21" endLine="344" endColumn="59" document="1"/>
                <entry offset="0xf" hidden="true" document="1"/>
                <entry offset="0x12" startLine="344" startColumn="60" endLine="344" endColumn="87" document="1"/>
                <entry offset="0x1c" startLine="345" startColumn="21" endLine="345" endColumn="47" document="1"/>
                <entry offset="0x24" startLine="346" startColumn="17" endLine="346" endColumn="24" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x26">
                <importsforward declaringType="My.MyProject+MyWebServices" methodName="Equals" parameterNames="o"/>
                <local name="GetInstance" il_index="0" il_start="0x0" il_end="0x26" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject+ThreadSafeObjectProvider`1" name=".ctor">
            <sequencePoints>
                <entry offset="0x0" startLine="352" startColumn="13" endLine="352" endColumn="29" document="1"/>
                <entry offset="0x1" startLine="353" startColumn="17" endLine="353" endColumn="29" document="1"/>
                <entry offset="0x8" startLine="354" startColumn="13" endLine="354" endColumn="20" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x9">
                <importsforward declaringType="My.MyProject+MyWebServices" methodName="Equals" parameterNames="o"/>
            </scope>
        </method>
    </methods>
</symbols>, options:=PdbValidationOptions.SkipConversionValidation) ' TODO: https://github.com/dotnet/roslyn/issues/18004
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub ConstructorsWithoutInitializers()
            Dim source =
<compilation>
    <file><![CDATA[
Class C
    Sub New()
        Dim o As Object
    End Sub
    Sub New(x As Object)
        Dim y As Object = x
    End Sub
End Class
]]></file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.DebugDll)
            compilation.VerifyPdb("C..ctor",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="C" name=".ctor">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="2" startColumn="5" endLine="2" endColumn="14" document="1"/>
                <entry offset="0x8" startLine="4" startColumn="5" endLine="4" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x9">
                <currentnamespace name=""/>
                <local name="o" il_index="0" il_start="0x0" il_end="0x9" attributes="0"/>
            </scope>
        </method>
        <method containingType="C" name=".ctor" parameterNames="x">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="5" startColumn="5" endLine="5" endColumn="25" document="1"/>
                <entry offset="0x8" startLine="6" startColumn="13" endLine="6" endColumn="28" document="1"/>
                <entry offset="0xf" startLine="7" startColumn="5" endLine="7" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <importsforward declaringType="C" methodName=".ctor"/>
                <local name="y" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub ConstructorsWithInitializers()
            Dim source =
<compilation>
    <file><![CDATA[
Class C
    Shared G As Object = 1
    Private F As Object = G
    Sub New()
        Dim o As Object
    End Sub
    Sub New(x As Object)
        Dim y As Object = x
    End Sub
End Class
]]></file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.DebugDll)
            compilation.VerifyPdb("C..ctor",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="C" name=".ctor">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="4" startColumn="5" endLine="4" endColumn="14" document="1"/>
                <entry offset="0x8" startLine="3" startColumn="13" endLine="3" endColumn="28" document="1"/>
                <entry offset="0x18" startLine="6" startColumn="5" endLine="6" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x19">
                <importsforward declaringType="C" methodName=".cctor"/>
                <local name="o" il_index="0" il_start="0x0" il_end="0x19" attributes="0"/>
            </scope>
        </method>
        <method containingType="C" name=".ctor" parameterNames="x">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="7" startColumn="5" endLine="7" endColumn="25" document="1"/>
                <entry offset="0x8" startLine="3" startColumn="13" endLine="3" endColumn="28" document="1"/>
                <entry offset="0x18" startLine="8" startColumn="13" endLine="8" endColumn="28" document="1"/>
                <entry offset="0x1f" startLine="9" startColumn="5" endLine="9" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x20">
                <importsforward declaringType="C" methodName=".cctor"/>
                <local name="y" il_index="0" il_start="0x0" il_end="0x20" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub TryCatchFinally()
            Dim source =
<compilation>
    <file><![CDATA[
Option Strict On
Imports System

Module M1
    Public Sub Main()
        Dim x As Integer = 0
        Try
            Dim y As String = "y"
label1:
label2:
            If x = 0 Then
                Throw New Exception()
            End If
        Catch ex As Exception
            Dim z As String = "z"
            Console.WriteLine(x)
            x = 1
            GoTo label1
        Finally
            Dim q As String = "q"
            Console.WriteLine(x)
        End Try

        Console.WriteLine(x)

    End Sub
End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.DebugExe)

            compilation.VerifyPdb("M1.Main",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <entryPoint declaringType="M1" methodName="Main"/>
    <methods>
        <method containingType="M1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="51"/>
                    <slot kind="1" offset="100"/>
                    <slot kind="0" offset="182"/>
                    <slot kind="0" offset="221"/>
                    <slot kind="0" offset="351"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="5" startColumn="5" endLine="5" endColumn="22" document="1"/>
                <entry offset="0x1" startLine="6" startColumn="13" endLine="6" endColumn="29" document="1"/>
                <entry offset="0x3" startLine="7" startColumn="9" endLine="7" endColumn="12" document="1"/>
                <entry offset="0x4" startLine="8" startColumn="17" endLine="8" endColumn="34" document="1"/>
                <entry offset="0xa" startLine="9" startColumn="1" endLine="9" endColumn="8" document="1"/>
                <entry offset="0xb" startLine="10" startColumn="1" endLine="10" endColumn="8" document="1"/>
                <entry offset="0xc" startLine="11" startColumn="13" endLine="11" endColumn="26" document="1"/>
                <entry offset="0x11" hidden="true" document="1"/>
                <entry offset="0x14" startLine="12" startColumn="17" endLine="12" endColumn="38" document="1"/>
                <entry offset="0x1a" startLine="13" startColumn="13" endLine="13" endColumn="19" document="1"/>
                <entry offset="0x1d" hidden="true" document="1"/>
                <entry offset="0x24" startLine="14" startColumn="9" endLine="14" endColumn="30" document="1"/>
                <entry offset="0x25" startLine="15" startColumn="17" endLine="15" endColumn="34" document="1"/>
                <entry offset="0x2c" startLine="16" startColumn="13" endLine="16" endColumn="33" document="1"/>
                <entry offset="0x33" startLine="17" startColumn="13" endLine="17" endColumn="18" document="1"/>
                <entry offset="0x35" startLine="18" startColumn="13" endLine="18" endColumn="24" document="1"/>
                <entry offset="0x3c" hidden="true" document="1"/>
                <entry offset="0x3e" startLine="19" startColumn="9" endLine="19" endColumn="16" document="1"/>
                <entry offset="0x3f" startLine="20" startColumn="17" endLine="20" endColumn="34" document="1"/>
                <entry offset="0x46" startLine="21" startColumn="13" endLine="21" endColumn="33" document="1"/>
                <entry offset="0x4e" startLine="22" startColumn="9" endLine="22" endColumn="16" document="1"/>
                <entry offset="0x4f" startLine="24" startColumn="9" endLine="24" endColumn="29" document="1"/>
                <entry offset="0x56" startLine="26" startColumn="5" endLine="26" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x57">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="x" il_index="0" il_start="0x0" il_end="0x57" attributes="0"/>
                <scope startOffset="0x4" endOffset="0x1a">
                    <local name="y" il_index="1" il_start="0x4" il_end="0x1a" attributes="0"/>
                </scope>
                <scope startOffset="0x1d" endOffset="0x3b">
                    <local name="ex" il_index="3" il_start="0x1d" il_end="0x3b" attributes="0"/>
                    <scope startOffset="0x25" endOffset="0x3b">
                        <local name="z" il_index="4" il_start="0x25" il_end="0x3b" attributes="0"/>
                    </scope>
                </scope>
                <scope startOffset="0x3f" endOffset="0x4c">
                    <local name="q" il_index="5" il_start="0x3f" il_end="0x4c" attributes="0"/>
                </scope>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub TryCatchWhen_Debug()
            Dim source =
<compilation>
    <file>
Option Strict On
Imports System

Module M1
    Public Sub Main()
        Dim x As Integer = 0
        Try
            Dim y As String = "y"
label1:
label2:
            x = x \ x
        Catch ex As Exception When ex.Message IsNot Nothing
            Dim z As String = "z"
            Console.WriteLine(x)
            x = 1
            GoTo label1
        Finally
            Dim q As String = "q"
            Console.WriteLine(x)
        End Try

        Console.WriteLine(x)

    End Sub
End Module
</file>
</compilation>

            ' ILVerify: Leave into try block. { Offset = 75 }
            Dim v = CompileAndVerify(CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.DebugExe), verify:=Verification.FailsILVerify)

            v.VerifyIL("M1.Main", "
{
  // Code size      104 (0x68)
  .maxstack  2
  .locals init (Integer V_0, //x
                String V_1, //y
                System.Exception V_2, //ex
                Boolean V_3,
                String V_4, //z
                String V_5) //q
 -IL_0000:  nop
 -IL_0001:  ldc.i4.0
  IL_0002:  stloc.0
  .try
  {
    .try
    {
     -IL_0003:  nop
     -IL_0004:  ldstr      ""y""
      IL_0009:  stloc.1
     -IL_000a:  nop
     -IL_000b:  nop
     -IL_000c:  ldloc.0
      IL_000d:  ldloc.0
      IL_000e:  div
      IL_000f:  stloc.0
      IL_0010:  leave.s    IL_004d
    }
    filter
    {
     ~IL_0012:  isinst     ""System.Exception""
      IL_0017:  dup
      IL_0018:  brtrue.s   IL_001e
      IL_001a:  pop
      IL_001b:  ldc.i4.0
      IL_001c:  br.s       IL_0033
      IL_001e:  dup
      IL_001f:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
      IL_0024:  stloc.2
     -IL_0025:  ldloc.2
      IL_0026:  callvirt   ""Function System.Exception.get_Message() As String""
      IL_002b:  ldnull
      IL_002c:  cgt.un
      IL_002e:  stloc.3
     ~IL_002f:  ldloc.3
      IL_0030:  ldc.i4.0
      IL_0031:  cgt.un
      IL_0033:  endfilter
    }  // end filter
    {  // handler
     ~IL_0035:  pop
     -IL_0036:  ldstr      ""z""
      IL_003b:  stloc.s    V_4
     -IL_003d:  ldloc.0
      IL_003e:  call       ""Sub System.Console.WriteLine(Integer)""
      IL_0043:  nop
     -IL_0044:  ldc.i4.1
      IL_0045:  stloc.0
     -IL_0046:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
      IL_004b:  leave.s    IL_000a
    }
   ~IL_004d:  leave.s    IL_005f
  }
  finally
  {
   -IL_004f:  nop
   -IL_0050:  ldstr      ""q""
    IL_0055:  stloc.s    V_5
   -IL_0057:  ldloc.0
    IL_0058:  call       ""Sub System.Console.WriteLine(Integer)""
    IL_005d:  nop
    IL_005e:  endfinally
  }
 -IL_005f:  nop
 -IL_0060:  ldloc.0
  IL_0061:  call       ""Sub System.Console.WriteLine(Integer)""
  IL_0066:  nop
 -IL_0067:  ret
}
", sequencePoints:="M1.Main")

            v.VerifyPdb("M1.Main",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <entryPoint declaringType="M1" methodName="Main"/>
    <methods>
        <method containingType="M1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="51"/>
                    <slot kind="0" offset="119"/>
                    <slot kind="1" offset="141"/>
                    <slot kind="0" offset="188"/>
                    <slot kind="0" offset="318"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="5" startColumn="5" endLine="5" endColumn="22" document="1"/>
                <entry offset="0x1" startLine="6" startColumn="13" endLine="6" endColumn="29" document="1"/>
                <entry offset="0x3" startLine="7" startColumn="9" endLine="7" endColumn="12" document="1"/>
                <entry offset="0x4" startLine="8" startColumn="17" endLine="8" endColumn="34" document="1"/>
                <entry offset="0xa" startLine="9" startColumn="1" endLine="9" endColumn="8" document="1"/>
                <entry offset="0xb" startLine="10" startColumn="1" endLine="10" endColumn="8" document="1"/>
                <entry offset="0xc" startLine="11" startColumn="13" endLine="11" endColumn="22" document="1"/>
                <entry offset="0x12" hidden="true" document="1"/>
                <entry offset="0x25" startLine="12" startColumn="9" endLine="12" endColumn="60" document="1"/>
                <entry offset="0x2f" hidden="true" document="1"/>
                <entry offset="0x35" hidden="true" document="1"/>
                <entry offset="0x36" startLine="13" startColumn="17" endLine="13" endColumn="34" document="1"/>
                <entry offset="0x3d" startLine="14" startColumn="13" endLine="14" endColumn="33" document="1"/>
                <entry offset="0x44" startLine="15" startColumn="13" endLine="15" endColumn="18" document="1"/>
                <entry offset="0x46" startLine="16" startColumn="13" endLine="16" endColumn="24" document="1"/>
                <entry offset="0x4d" hidden="true" document="1"/>
                <entry offset="0x4f" startLine="17" startColumn="9" endLine="17" endColumn="16" document="1"/>
                <entry offset="0x50" startLine="18" startColumn="17" endLine="18" endColumn="34" document="1"/>
                <entry offset="0x57" startLine="19" startColumn="13" endLine="19" endColumn="33" document="1"/>
                <entry offset="0x5f" startLine="20" startColumn="9" endLine="20" endColumn="16" document="1"/>
                <entry offset="0x60" startLine="22" startColumn="9" endLine="22" endColumn="29" document="1"/>
                <entry offset="0x67" startLine="24" startColumn="5" endLine="24" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x68">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="x" il_index="0" il_start="0x0" il_end="0x68" attributes="0"/>
                <scope startOffset="0x4" endOffset="0xf">
                    <local name="y" il_index="1" il_start="0x4" il_end="0xf" attributes="0"/>
                </scope>
                <scope startOffset="0x12" endOffset="0x4c">
                    <local name="ex" il_index="2" il_start="0x12" il_end="0x4c" attributes="0"/>
                    <scope startOffset="0x36" endOffset="0x4c">
                        <local name="z" il_index="4" il_start="0x36" il_end="0x4c" attributes="0"/>
                    </scope>
                </scope>
                <scope startOffset="0x50" endOffset="0x5d">
                    <local name="q" il_index="5" il_start="0x50" il_end="0x5d" attributes="0"/>
                </scope>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub TryCatchWhen_Release()
            Dim source =
<compilation>
    <file>
Imports System
Imports System.IO

Module M1
    Function filter(e As Exception)
        Return True
    End Function

    Public Sub Main()
        Try
            Throw New InvalidOperationException()
        Catch e As IOException When filter(e)
            Console.WriteLine()
        Catch e As Exception When filter(e)
            Console.WriteLine()
        End Try
    End Sub
End Module
</file>
</compilation>

            Dim v = CompileAndVerify(CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe))

            v.VerifyIL("M1.Main", "
{
  // Code size      103 (0x67)
  .maxstack  2
  .locals init (System.IO.IOException V_0, //e
                System.Exception V_1) //e
  .try
  {
   -IL_0000:  newobj     ""Sub System.InvalidOperationException..ctor()""
    IL_0005:  throw
  }
  filter
  {
   ~IL_0006:  isinst     ""System.IO.IOException""
    IL_000b:  dup
    IL_000c:  brtrue.s   IL_0012
    IL_000e:  pop
    IL_000f:  ldc.i4.0
    IL_0010:  br.s       IL_0027
    IL_0012:  dup
    IL_0013:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_0018:  stloc.0
   -IL_0019:  ldloc.0
    IL_001a:  call       ""Function M1.filter(System.Exception) As Object""
    IL_001f:  call       ""Function Microsoft.VisualBasic.CompilerServices.Conversions.ToBoolean(Object) As Boolean""
    IL_0024:  ldc.i4.0
    IL_0025:  cgt.un
    IL_0027:  endfilter
  }  // end filter
  {  // handler
   ~IL_0029:  pop
   -IL_002a:  call       ""Sub System.Console.WriteLine()""
    IL_002f:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_0034:  leave.s    IL_0066
  }
  filter
  {
   ~IL_0036:  isinst     ""System.Exception""
    IL_003b:  dup
    IL_003c:  brtrue.s   IL_0042
    IL_003e:  pop
    IL_003f:  ldc.i4.0
    IL_0040:  br.s       IL_0057
    IL_0042:  dup
    IL_0043:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_0048:  stloc.1
   -IL_0049:  ldloc.1
    IL_004a:  call       ""Function M1.filter(System.Exception) As Object""
    IL_004f:  call       ""Function Microsoft.VisualBasic.CompilerServices.Conversions.ToBoolean(Object) As Boolean""
    IL_0054:  ldc.i4.0
    IL_0055:  cgt.un
    IL_0057:  endfilter
  }  // end filter
  {  // handler
   ~IL_0059:  pop
   -IL_005a:  call       ""Sub System.Console.WriteLine()""
    IL_005f:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_0064:  leave.s    IL_0066
  }
 -IL_0066:  ret
}
", sequencePoints:="M1.Main")
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub TestBasic1()
            Dim source =
<compilation>
    <file><![CDATA[
Option Strict On

Module Module1
    Sub Main()
        Dim x As Integer = 3
        Do While (x <= 3)
            Dim y As Integer = x + 1
            x = y
        Loop
    End Sub
End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
                    source,
                    TestOptions.DebugExe)

            compilation.VerifyPdb("Module1.Main",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <entryPoint declaringType="Module1" methodName="Main"/>
    <methods>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="65"/>
                    <slot kind="1" offset="30"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="4" startColumn="5" endLine="4" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="5" startColumn="13" endLine="5" endColumn="29" document="1"/>
                <entry offset="0x3" hidden="true" document="1"/>
                <entry offset="0x5" startLine="7" startColumn="17" endLine="7" endColumn="37" document="1"/>
                <entry offset="0x9" startLine="8" startColumn="13" endLine="8" endColumn="18" document="1"/>
                <entry offset="0xb" startLine="9" startColumn="9" endLine="9" endColumn="13" document="1"/>
                <entry offset="0xc" startLine="6" startColumn="9" endLine="6" endColumn="26" document="1"/>
                <entry offset="0x14" hidden="true" document="1"/>
                <entry offset="0x17" startLine="10" startColumn="5" endLine="10" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x18">
                <currentnamespace name=""/>
                <local name="x" il_index="0" il_start="0x0" il_end="0x18" attributes="0"/>
                <scope startOffset="0x5" endOffset="0xb">
                    <local name="y" il_index="1" il_start="0x5" il_end="0xb" attributes="0"/>
                </scope>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub TestBasicCtor()
            Dim source =
<compilation>
    <file><![CDATA[
Class C1
    Sub New()
        System.Console.WriteLine("Hello, world.")
    End Sub
End Class
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.DebugDll)

            compilation.VerifyPdb("C1..ctor",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="C1" name=".ctor">
            <sequencePoints>
                <entry offset="0x0" startLine="2" startColumn="5" endLine="2" endColumn="14" document="1"/>
                <entry offset="0x8" startLine="3" startColumn="9" endLine="3" endColumn="50" document="1"/>
                <entry offset="0x13" startLine="4" startColumn="5" endLine="4" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x14">
                <currentnamespace name=""/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub TestLabels()
            Dim source =
<compilation>
    <file><![CDATA[
Class C1
    Sub New()
        label1:
        label2:
        label3:

        goto label2:
    End Sub
End Class
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.DebugDll)

            compilation.VerifyPdb("C1..ctor",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="C1" name=".ctor">
            <sequencePoints>
                <entry offset="0x0" startLine="2" startColumn="5" endLine="2" endColumn="14" document="1"/>
                <entry offset="0x8" startLine="3" startColumn="9" endLine="3" endColumn="16" document="1"/>
                <entry offset="0x9" startLine="4" startColumn="9" endLine="4" endColumn="16" document="1"/>
                <entry offset="0xa" startLine="5" startColumn="9" endLine="5" endColumn="16" document="1"/>
                <entry offset="0xb" startLine="7" startColumn="9" endLine="7" endColumn="20" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xd">
                <currentnamespace name=""/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub IfStatement()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System

Class C
    Sub F()
        If G() Then
            Console.WriteLine(1)
        Else
            Console.WriteLine(2)
        End If
        
        Console.WriteLine(3)
    End Sub

    Function G() As Boolean
        Return False
    End Function
End Class
]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.DebugDll)

            Dim v = CompileAndVerify(compilation)

            v.VerifyIL("C.F", "
{
  // Code size       38 (0x26)
  .maxstack  1
  .locals init (Boolean V_0)
 -IL_0000:  nop
 -IL_0001:  ldarg.0
  IL_0002:  call       ""Function C.G() As Boolean""
  IL_0007:  stloc.0
 ~IL_0008:  ldloc.0
  IL_0009:  brfalse.s  IL_0015
 -IL_000b:  ldc.i4.1
  IL_000c:  call       ""Sub System.Console.WriteLine(Integer)""
  IL_0011:  nop
 -IL_0012:  nop
  IL_0013:  br.s       IL_001e
 -IL_0015:  nop
 -IL_0016:  ldc.i4.2
  IL_0017:  call       ""Sub System.Console.WriteLine(Integer)""
  IL_001c:  nop
 -IL_001d:  nop
 -IL_001e:  ldc.i4.3
  IL_001f:  call       ""Sub System.Console.WriteLine(Integer)""
  IL_0024:  nop
 -IL_0025:  ret
}
", sequencePoints:="C.F")

            v.VerifyPdb("C.F",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="C" name="F">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="1" offset="0"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="4" startColumn="5" endLine="4" endColumn="12" document="1"/>
                <entry offset="0x1" startLine="5" startColumn="9" endLine="5" endColumn="20" document="1"/>
                <entry offset="0x8" hidden="true" document="1"/>
                <entry offset="0xb" startLine="6" startColumn="13" endLine="6" endColumn="33" document="1"/>
                <entry offset="0x12" startLine="9" startColumn="9" endLine="9" endColumn="15" document="1"/>
                <entry offset="0x15" startLine="7" startColumn="9" endLine="7" endColumn="13" document="1"/>
                <entry offset="0x16" startLine="8" startColumn="13" endLine="8" endColumn="33" document="1"/>
                <entry offset="0x1d" startLine="9" startColumn="9" endLine="9" endColumn="15" document="1"/>
                <entry offset="0x1e" startLine="11" startColumn="9" endLine="11" endColumn="29" document="1"/>
                <entry offset="0x25" startLine="12" startColumn="5" endLine="12" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x26">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
            </scope>
        </method>
    </methods>
</symbols>)

        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub DoWhileStatement()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System

Class C
    Sub F()
        Do While G()
            Console.WriteLine(1)
        Loop
    End Sub

    Function G() As Boolean
        Return False
    End Function
End Class
]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.DebugDll)

            Dim v = CompileAndVerify(compilation)

            v.VerifyIL("C.F", "
{
  // Code size       22 (0x16)
  .maxstack  1
  .locals init (Boolean V_0)
 -IL_0000:  nop
 ~IL_0001:  br.s       IL_000b
 -IL_0003:  ldc.i4.1
  IL_0004:  call       ""Sub System.Console.WriteLine(Integer)""
  IL_0009:  nop
 -IL_000a:  nop
 -IL_000b:  ldarg.0
  IL_000c:  call       ""Function C.G() As Boolean""
  IL_0011:  stloc.0
 ~IL_0012:  ldloc.0
  IL_0013:  brtrue.s   IL_0003
 -IL_0015:  ret
}
", sequencePoints:="C.F")

            v.VerifyPdb("C.F",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="C" name="F">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="1" offset="0"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="4" startColumn="5" endLine="4" endColumn="12" document="1"/>
                <entry offset="0x1" hidden="true" document="1"/>
                <entry offset="0x3" startLine="6" startColumn="13" endLine="6" endColumn="33" document="1"/>
                <entry offset="0xa" startLine="7" startColumn="9" endLine="7" endColumn="13" document="1"/>
                <entry offset="0xb" startLine="5" startColumn="9" endLine="5" endColumn="21" document="1"/>
                <entry offset="0x12" hidden="true" document="1"/>
                <entry offset="0x15" startLine="8" startColumn="5" endLine="8" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x16">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
            </scope>
        </method>
    </methods>
</symbols>)

        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub DoLoopWhileStatement()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System

Class C
    Sub F()
        Do  
            Console.WriteLine(1)
        Loop While G()
    End Sub

    Function G() As Boolean
        Return False
    End Function
End Class
]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.DebugDll)

            Dim v = CompileAndVerify(compilation)

            v.VerifyIL("C.F", "
{
  // Code size       21 (0x15)
  .maxstack  1
  .locals init (Boolean V_0)
 -IL_0000:  nop
 -IL_0001:  nop
 -IL_0002:  ldc.i4.1
  IL_0003:  call       ""Sub System.Console.WriteLine(Integer)""
  IL_0008:  nop
 -IL_0009:  nop
  IL_000a:  ldarg.0
  IL_000b:  call       ""Function C.G() As Boolean""
  IL_0010:  stloc.0
 ~IL_0011:  ldloc.0
  IL_0012:  brtrue.s   IL_0001
 -IL_0014:  ret
}
", sequencePoints:="C.F")

            v.VerifyPdb("C.F",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="C" name="F">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="1" offset="0"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="4" startColumn="5" endLine="4" endColumn="12" document="1"/>
                <entry offset="0x1" startLine="5" startColumn="9" endLine="5" endColumn="11" document="1"/>
                <entry offset="0x2" startLine="6" startColumn="13" endLine="6" endColumn="33" document="1"/>
                <entry offset="0x9" startLine="7" startColumn="9" endLine="7" endColumn="23" document="1"/>
                <entry offset="0x11" hidden="true" document="1"/>
                <entry offset="0x14" startLine="8" startColumn="5" endLine="8" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x15">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
            </scope>
        </method>
    </methods>
</symbols>)

        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub ForStatement()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System

Class C
    Sub F()
        For a = G(0) To G(1) Step G(2)
            Console.WriteLine(1)
        Next
    End Sub

    Function G(a As Integer) As Integer
        Return 10
    End Function
End Class
]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.DebugDll)

            Dim v = CompileAndVerify(compilation)

            v.VerifyIL("C.F", "
{
  // Code size       55 (0x37)
  .maxstack  3
  .locals init (Integer V_0,
                Integer V_1,
                Integer V_2,
                Integer V_3) //a
 -IL_0000:  nop
 -IL_0001:  ldarg.0
  IL_0002:  ldc.i4.0
  IL_0003:  call       ""Function C.G(Integer) As Integer""
  IL_0008:  stloc.0
  IL_0009:  ldarg.0
  IL_000a:  ldc.i4.1
  IL_000b:  call       ""Function C.G(Integer) As Integer""
  IL_0010:  stloc.1
  IL_0011:  ldarg.0
  IL_0012:  ldc.i4.2
  IL_0013:  call       ""Function C.G(Integer) As Integer""
  IL_0018:  stloc.2
  IL_0019:  ldloc.0
  IL_001a:  stloc.3
 ~IL_001b:  br.s       IL_0028
 -IL_001d:  ldc.i4.1
  IL_001e:  call       ""Sub System.Console.WriteLine(Integer)""
  IL_0023:  nop
 -IL_0024:  ldloc.3
  IL_0025:  ldloc.2
  IL_0026:  add.ovf
  IL_0027:  stloc.3
 ~IL_0028:  ldloc.2
  IL_0029:  ldc.i4.s   31
  IL_002b:  shr
  IL_002c:  ldloc.3
  IL_002d:  xor
  IL_002e:  ldloc.2
  IL_002f:  ldc.i4.s   31
  IL_0031:  shr
  IL_0032:  ldloc.1
  IL_0033:  xor
  IL_0034:  ble.s      IL_001d
 -IL_0036:  ret
}", sequencePoints:="C.F")

            v.VerifyPdb("C.F",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="C" name="F">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="13" offset="0"/>
                    <slot kind="11" offset="0"/>
                    <slot kind="12" offset="0"/>
                    <slot kind="0" offset="0"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="4" startColumn="5" endLine="4" endColumn="12" document="1"/>
                <entry offset="0x1" startLine="5" startColumn="9" endLine="5" endColumn="39" document="1"/>
                <entry offset="0x1b" hidden="true" document="1"/>
                <entry offset="0x1d" startLine="6" startColumn="13" endLine="6" endColumn="33" document="1"/>
                <entry offset="0x24" startLine="7" startColumn="9" endLine="7" endColumn="13" document="1"/>
                <entry offset="0x28" hidden="true" document="1"/>
                <entry offset="0x36" startLine="8" startColumn="5" endLine="8" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x37">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <scope startOffset="0x1" endOffset="0x35">
                    <local name="a" il_index="3" il_start="0x1" il_end="0x35" attributes="0"/>
                </scope>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub ForStatement_LateBound()
            Dim v = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict On
Public Class MyClass1
    Public Shared Sub Main()

        Dim ctrlVar As Object
        Dim initValue As Object = 0
        Dim limit As Object = 2
        Dim stp As Object = 1

        For ctrlVar = initValue To limit Step stp
            System.Console.WriteLine(ctrlVar)
        Next

    End Sub
End Class
    </file>
</compilation>, options:=TestOptions.DebugDll)

            v.VerifyIL("MyClass1.Main", "
{
  // Code size       70 (0x46)
  .maxstack  6
  .locals init (Object V_0, //ctrlVar
                Object V_1, //initValue
                Object V_2, //limit
                Object V_3, //stp
                Object V_4,
                Boolean V_5,
                Boolean V_6)
 -IL_0000:  nop
 -IL_0001:  ldc.i4.0
  IL_0002:  box        ""Integer""
  IL_0007:  stloc.1
 -IL_0008:  ldc.i4.2
  IL_0009:  box        ""Integer""
  IL_000e:  stloc.2
 -IL_000f:  ldc.i4.1
  IL_0010:  box        ""Integer""
  IL_0015:  stloc.3
 -IL_0016:  ldloc.0
  IL_0017:  ldloc.1
  IL_0018:  ldloc.2
  IL_0019:  ldloc.3
  IL_001a:  ldloca.s   V_4
  IL_001c:  ldloca.s   V_0
  IL_001e:  call       ""Function Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.ForLoopControl.ForLoopInitObj(Object, Object, Object, Object, ByRef Object, ByRef Object) As Boolean""
  IL_0023:  stloc.s    V_5
 ~IL_0025:  ldloc.s    V_5
  IL_0027:  brfalse.s  IL_0045
 -IL_0029:  ldloc.0
  IL_002a:  call       ""Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object""
  IL_002f:  call       ""Sub System.Console.WriteLine(Object)""
  IL_0034:  nop
 -IL_0035:  ldloc.0
  IL_0036:  ldloc.s    V_4
  IL_0038:  ldloca.s   V_0
  IL_003a:  call       ""Function Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.ForLoopControl.ForNextCheckObj(Object, Object, ByRef Object) As Boolean""
  IL_003f:  stloc.s    V_6
 ~IL_0041:  ldloc.s    V_6
  IL_0043:  brtrue.s   IL_0029
 -IL_0045:  ret
}
", sequencePoints:="MyClass1.Main")

            v.VerifyPdb("MyClass1.Main",
<symbols>
    <files>
        <file id="1" name="a.vb" language="VB" checksumAlgorithm="SHA1" checksum="87-56-72-A9-63-E5-5D-0C-F3-97-85-44-CF-51-55-8E-76-E7-1D-F1"/>
    </files>
    <methods>
        <method containingType="MyClass1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="35"/>
                    <slot kind="0" offset="72"/>
                    <slot kind="0" offset="105"/>
                    <slot kind="13" offset="134"/>
                    <slot kind="1" offset="134"/>
                    <slot kind="1" offset="134" ordinal="1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="3" startColumn="5" endLine="3" endColumn="29" document="1"/>
                <entry offset="0x1" startLine="6" startColumn="13" endLine="6" endColumn="36" document="1"/>
                <entry offset="0x8" startLine="7" startColumn="13" endLine="7" endColumn="32" document="1"/>
                <entry offset="0xf" startLine="8" startColumn="13" endLine="8" endColumn="30" document="1"/>
                <entry offset="0x16" startLine="10" startColumn="9" endLine="10" endColumn="50" document="1"/>
                <entry offset="0x25" hidden="true" document="1"/>
                <entry offset="0x29" startLine="11" startColumn="13" endLine="11" endColumn="46" document="1"/>
                <entry offset="0x35" startLine="12" startColumn="9" endLine="12" endColumn="13" document="1"/>
                <entry offset="0x41" hidden="true" document="1"/>
                <entry offset="0x45" startLine="14" startColumn="5" endLine="14" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x46">
                <currentnamespace name=""/>
                <local name="ctrlVar" il_index="0" il_start="0x0" il_end="0x46" attributes="0"/>
                <local name="initValue" il_index="1" il_start="0x0" il_end="0x46" attributes="0"/>
                <local name="limit" il_index="2" il_start="0x0" il_end="0x46" attributes="0"/>
                <local name="stp" il_index="3" il_start="0x0" il_end="0x46" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub SelectCaseStatement()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System

Class C
    Sub F()
        Select Case G(1)
            Case G(2)
                Console.WriteLine(4)
            Case G(3)
                Console.WriteLine(5)
        End Select
    End Sub

    Function G(a As Integer) As Integer
        Return a
    End Function
End Class
]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.DebugDll)

            Dim v = CompileAndVerify(compilation)

            v.VerifyIL("C.F", "
{
  // Code size       56 (0x38)
  .maxstack  3
  .locals init (Integer V_0,
                Boolean V_1)
 -IL_0000:  nop
 -IL_0001:  nop
  IL_0002:  ldarg.0
  IL_0003:  ldc.i4.1
  IL_0004:  call       ""Function C.G(Integer) As Integer""
  IL_0009:  stloc.0
 -IL_000a:  ldloc.0
  IL_000b:  ldarg.0
  IL_000c:  ldc.i4.2
  IL_000d:  call       ""Function C.G(Integer) As Integer""
  IL_0012:  ceq
  IL_0014:  stloc.1
 ~IL_0015:  ldloc.1
  IL_0016:  brfalse.s  IL_0021
 -IL_0018:  ldc.i4.4
  IL_0019:  call       ""Sub System.Console.WriteLine(Integer)""
  IL_001e:  nop
  IL_001f:  br.s       IL_0036
 -IL_0021:  ldloc.0
  IL_0022:  ldarg.0
  IL_0023:  ldc.i4.3
  IL_0024:  call       ""Function C.G(Integer) As Integer""
  IL_0029:  ceq
  IL_002b:  stloc.1
 ~IL_002c:  ldloc.1
  IL_002d:  brfalse.s  IL_0036
 -IL_002f:  ldc.i4.5
  IL_0030:  call       ""Sub System.Console.WriteLine(Integer)""
  IL_0035:  nop
 -IL_0036:  nop
 -IL_0037:  ret
}
", sequencePoints:="C.F")

            v.VerifyPdb("C.F",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="C" name="F">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="15" offset="0"/>
                    <slot kind="1" offset="0"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="4" startColumn="5" endLine="4" endColumn="12" document="1"/>
                <entry offset="0x1" startLine="5" startColumn="9" endLine="5" endColumn="25" document="1"/>
                <entry offset="0xa" startLine="6" startColumn="13" endLine="6" endColumn="22" document="1"/>
                <entry offset="0x15" hidden="true" document="1"/>
                <entry offset="0x18" startLine="7" startColumn="17" endLine="7" endColumn="37" document="1"/>
                <entry offset="0x21" startLine="8" startColumn="13" endLine="8" endColumn="22" document="1"/>
                <entry offset="0x2c" hidden="true" document="1"/>
                <entry offset="0x2f" startLine="9" startColumn="17" endLine="9" endColumn="37" document="1"/>
                <entry offset="0x36" startLine="10" startColumn="9" endLine="10" endColumn="19" document="1"/>
                <entry offset="0x37" startLine="11" startColumn="5" endLine="11" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x38">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
            </scope>
        </method>
    </methods>
</symbols>)

        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub TestIfThenAndBlocks()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System

Module Module1
    Sub Main(args As String())

        Dim x As Integer = 0, xx = New Integer()

        If x < 10 Then Dim s As String = "hi" : Console.WriteLine(s) Else Console.WriteLine("bye") : Console.WriteLine("bye1")
        If x > 10 Then Console.WriteLine("hi") : Console.WriteLine("hi1") Else Dim s As String = "bye" : Console.WriteLine(s)

        Do While x < 5
            If x < 1 Then
                Console.WriteLine("<1")
            ElseIf x < 2 Then
                Dim s2 As String = "<2"
                Console.WriteLine(s2)
            ElseIf x < 3 Then
                Dim s3 As String = "<3"
                Console.WriteLine(s3)
            Else
                Dim e1 As String = "Else"
                Console.WriteLine(e1)
            End If

            Dim newX As Integer = x + 1
            x = newX
        Loop

    End Sub
End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.DebugExe)

            compilation.VerifyPdb("Module1.Main",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <entryPoint declaringType="Module1" methodName="Main" parameterNames="args"/>
    <methods>
        <method containingType="Module1" name="Main" parameterNames="args">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="22"/>
                    <slot kind="1" offset="52"/>
                    <slot kind="0" offset="71"/>
                    <slot kind="1" offset="180"/>
                    <slot kind="0" offset="255"/>
                    <slot kind="0" offset="753"/>
                    <slot kind="1" offset="337"/>
                    <slot kind="1" offset="405"/>
                    <slot kind="0" offset="444"/>
                    <slot kind="1" offset="516"/>
                    <slot kind="0" offset="555"/>
                    <slot kind="0" offset="653"/>
                    <slot kind="1" offset="309"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="4" startColumn="5" endLine="4" endColumn="31" document="1"/>
                <entry offset="0x1" startLine="6" startColumn="13" endLine="6" endColumn="29" document="1"/>
                <entry offset="0x3" startLine="6" startColumn="31" endLine="6" endColumn="49" document="1"/>
                <entry offset="0xb" startLine="8" startColumn="9" endLine="8" endColumn="23" document="1"/>
                <entry offset="0x11" hidden="true" document="1"/>
                <entry offset="0x14" startLine="8" startColumn="28" endLine="8" endColumn="46" document="1"/>
                <entry offset="0x1a" startLine="8" startColumn="49" endLine="8" endColumn="69" document="1"/>
                <entry offset="0x23" startLine="8" startColumn="70" endLine="8" endColumn="74" document="1"/>
                <entry offset="0x24" startLine="8" startColumn="75" endLine="8" endColumn="99" document="1"/>
                <entry offset="0x2f" startLine="8" startColumn="102" endLine="8" endColumn="127" document="1"/>
                <entry offset="0x3a" startLine="9" startColumn="9" endLine="9" endColumn="23" document="1"/>
                <entry offset="0x41" hidden="true" document="1"/>
                <entry offset="0x45" startLine="9" startColumn="24" endLine="9" endColumn="47" document="1"/>
                <entry offset="0x50" startLine="9" startColumn="50" endLine="9" endColumn="74" document="1"/>
                <entry offset="0x5d" startLine="9" startColumn="75" endLine="9" endColumn="79" document="1"/>
                <entry offset="0x5e" startLine="9" startColumn="84" endLine="9" endColumn="103" document="1"/>
                <entry offset="0x65" startLine="9" startColumn="106" endLine="9" endColumn="126" document="1"/>
                <entry offset="0x6d" hidden="true" document="1"/>
                <entry offset="0x6f" startLine="12" startColumn="13" endLine="12" endColumn="26" document="1"/>
                <entry offset="0x75" hidden="true" document="1"/>
                <entry offset="0x79" startLine="13" startColumn="17" endLine="13" endColumn="40" document="1"/>
                <entry offset="0x84" startLine="23" startColumn="13" endLine="23" endColumn="19" document="1"/>
                <entry offset="0x87" startLine="14" startColumn="13" endLine="14" endColumn="30" document="1"/>
                <entry offset="0x8d" hidden="true" document="1"/>
                <entry offset="0x91" startLine="15" startColumn="21" endLine="15" endColumn="40" document="1"/>
                <entry offset="0x98" startLine="16" startColumn="17" endLine="16" endColumn="38" document="1"/>
                <entry offset="0xa0" startLine="23" startColumn="13" endLine="23" endColumn="19" document="1"/>
                <entry offset="0xa3" startLine="17" startColumn="13" endLine="17" endColumn="30" document="1"/>
                <entry offset="0xa9" hidden="true" document="1"/>
                <entry offset="0xad" startLine="18" startColumn="21" endLine="18" endColumn="40" document="1"/>
                <entry offset="0xb4" startLine="19" startColumn="17" endLine="19" endColumn="38" document="1"/>
                <entry offset="0xbc" startLine="23" startColumn="13" endLine="23" endColumn="19" document="1"/>
                <entry offset="0xbf" startLine="20" startColumn="13" endLine="20" endColumn="17" document="1"/>
                <entry offset="0xc0" startLine="21" startColumn="21" endLine="21" endColumn="42" document="1"/>
                <entry offset="0xc7" startLine="22" startColumn="17" endLine="22" endColumn="38" document="1"/>
                <entry offset="0xcf" startLine="23" startColumn="13" endLine="23" endColumn="19" document="1"/>
                <entry offset="0xd0" startLine="25" startColumn="17" endLine="25" endColumn="40" document="1"/>
                <entry offset="0xd5" startLine="26" startColumn="13" endLine="26" endColumn="21" document="1"/>
                <entry offset="0xd8" startLine="27" startColumn="9" endLine="27" endColumn="13" document="1"/>
                <entry offset="0xd9" startLine="11" startColumn="9" endLine="11" endColumn="23" document="1"/>
                <entry offset="0xdf" hidden="true" document="1"/>
                <entry offset="0xe3" startLine="29" startColumn="5" endLine="29" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xe4">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="x" il_index="0" il_start="0x0" il_end="0xe4" attributes="0"/>
                <local name="xx" il_index="1" il_start="0x0" il_end="0xe4" attributes="0"/>
                <scope startOffset="0x14" endOffset="0x20">
                    <local name="s" il_index="3" il_start="0x14" il_end="0x20" attributes="0"/>
                </scope>
                <scope startOffset="0x5e" endOffset="0x6c">
                    <local name="s" il_index="5" il_start="0x5e" il_end="0x6c" attributes="0"/>
                </scope>
                <scope startOffset="0x6f" endOffset="0xd8">
                    <local name="newX" il_index="6" il_start="0x6f" il_end="0xd8" attributes="0"/>
                    <scope startOffset="0x91" endOffset="0xa0">
                        <local name="s2" il_index="9" il_start="0x91" il_end="0xa0" attributes="0"/>
                    </scope>
                    <scope startOffset="0xad" endOffset="0xbc">
                        <local name="s3" il_index="11" il_start="0xad" il_end="0xbc" attributes="0"/>
                    </scope>
                    <scope startOffset="0xc0" endOffset="0xcf">
                        <local name="e1" il_index="12" il_start="0xc0" il_end="0xcf" attributes="0"/>
                    </scope>
                </scope>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub TestTopConditionDoLoop()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System

Module Module1
    Sub Main(args As String())

        Dim x As Integer = 0
        Do While x < 5
            If x < 1 Then
                Console.WriteLine("<1")
            ElseIf x < 2 Then
                Dim s2 As String = "<2"
                Console.WriteLine(s2)
            ElseIf x < 3 Then
                Dim s3 As String = "<3"
                Console.WriteLine(s3)
            Else
                Dim e1 As String = "Else"
                Console.WriteLine(e1)
            End If

            Dim newX As Integer = x + 1
            x = newX
        Loop

    End Sub
End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.DebugExe)

            compilation.VerifyPdb("Module1.Main",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <entryPoint declaringType="Module1" methodName="Main" parameterNames="args"/>
    <methods>
        <method containingType="Module1" name="Main" parameterNames="args">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="474"/>
                    <slot kind="1" offset="58"/>
                    <slot kind="1" offset="126"/>
                    <slot kind="0" offset="165"/>
                    <slot kind="1" offset="237"/>
                    <slot kind="0" offset="276"/>
                    <slot kind="0" offset="374"/>
                    <slot kind="1" offset="30"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="4" startColumn="5" endLine="4" endColumn="31" document="1"/>
                <entry offset="0x1" startLine="6" startColumn="13" endLine="6" endColumn="29" document="1"/>
                <entry offset="0x3" hidden="true" document="1"/>
                <entry offset="0x5" startLine="8" startColumn="13" endLine="8" endColumn="26" document="1"/>
                <entry offset="0xa" hidden="true" document="1"/>
                <entry offset="0xd" startLine="9" startColumn="17" endLine="9" endColumn="40" document="1"/>
                <entry offset="0x18" startLine="19" startColumn="13" endLine="19" endColumn="19" document="1"/>
                <entry offset="0x1b" startLine="10" startColumn="13" endLine="10" endColumn="30" document="1"/>
                <entry offset="0x20" hidden="true" document="1"/>
                <entry offset="0x23" startLine="11" startColumn="21" endLine="11" endColumn="40" document="1"/>
                <entry offset="0x2a" startLine="12" startColumn="17" endLine="12" endColumn="38" document="1"/>
                <entry offset="0x32" startLine="19" startColumn="13" endLine="19" endColumn="19" document="1"/>
                <entry offset="0x35" startLine="13" startColumn="13" endLine="13" endColumn="30" document="1"/>
                <entry offset="0x3b" hidden="true" document="1"/>
                <entry offset="0x3f" startLine="14" startColumn="21" endLine="14" endColumn="40" document="1"/>
                <entry offset="0x46" startLine="15" startColumn="17" endLine="15" endColumn="38" document="1"/>
                <entry offset="0x4e" startLine="19" startColumn="13" endLine="19" endColumn="19" document="1"/>
                <entry offset="0x51" startLine="16" startColumn="13" endLine="16" endColumn="17" document="1"/>
                <entry offset="0x52" startLine="17" startColumn="21" endLine="17" endColumn="42" document="1"/>
                <entry offset="0x59" startLine="18" startColumn="17" endLine="18" endColumn="38" document="1"/>
                <entry offset="0x61" startLine="19" startColumn="13" endLine="19" endColumn="19" document="1"/>
                <entry offset="0x62" startLine="21" startColumn="17" endLine="21" endColumn="40" document="1"/>
                <entry offset="0x66" startLine="22" startColumn="13" endLine="22" endColumn="21" document="1"/>
                <entry offset="0x68" startLine="23" startColumn="9" endLine="23" endColumn="13" document="1"/>
                <entry offset="0x69" startLine="7" startColumn="9" endLine="7" endColumn="23" document="1"/>
                <entry offset="0x6f" hidden="true" document="1"/>
                <entry offset="0x73" startLine="25" startColumn="5" endLine="25" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x74">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="x" il_index="0" il_start="0x0" il_end="0x74" attributes="0"/>
                <scope startOffset="0x5" endOffset="0x68">
                    <local name="newX" il_index="1" il_start="0x5" il_end="0x68" attributes="0"/>
                    <scope startOffset="0x23" endOffset="0x32">
                        <local name="s2" il_index="4" il_start="0x23" il_end="0x32" attributes="0"/>
                    </scope>
                    <scope startOffset="0x3f" endOffset="0x4e">
                        <local name="s3" il_index="6" il_start="0x3f" il_end="0x4e" attributes="0"/>
                    </scope>
                    <scope startOffset="0x52" endOffset="0x61">
                        <local name="e1" il_index="7" il_start="0x52" il_end="0x61" attributes="0"/>
                    </scope>
                </scope>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub TestBottomConditionDoLoop()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System

Module Module1
    Sub Main(args As String())

        Dim x As Integer = 0

        Do
            If x < 1 Then
                Console.WriteLine("<1")
            ElseIf x < 2 Then
                Dim s2 As String = "<2"
                Console.WriteLine(s2)
            ElseIf x < 3 Then
                Dim s3 As String = "<3"
                Console.WriteLine(s3)
            Else
                Dim e1 As String = "Else"
                Console.WriteLine(e1)
            End If

            Dim newX As Integer = x + 1
            x = newX
        Loop While x < 5

    End Sub
End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
                    source,
                    TestOptions.DebugExe)

            compilation.VerifyPdb("Module1.Main",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <entryPoint declaringType="Module1" methodName="Main" parameterNames="args"/>
    <methods>
        <method containingType="Module1" name="Main" parameterNames="args">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="464"/>
                    <slot kind="1" offset="48"/>
                    <slot kind="1" offset="116"/>
                    <slot kind="0" offset="155"/>
                    <slot kind="1" offset="227"/>
                    <slot kind="0" offset="266"/>
                    <slot kind="0" offset="364"/>
                    <slot kind="1" offset="32"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="4" startColumn="5" endLine="4" endColumn="31" document="1"/>
                <entry offset="0x1" startLine="6" startColumn="13" endLine="6" endColumn="29" document="1"/>
                <entry offset="0x3" startLine="8" startColumn="9" endLine="8" endColumn="11" document="1"/>
                <entry offset="0x4" startLine="9" startColumn="13" endLine="9" endColumn="26" document="1"/>
                <entry offset="0x9" hidden="true" document="1"/>
                <entry offset="0xc" startLine="10" startColumn="17" endLine="10" endColumn="40" document="1"/>
                <entry offset="0x17" startLine="20" startColumn="13" endLine="20" endColumn="19" document="1"/>
                <entry offset="0x1a" startLine="11" startColumn="13" endLine="11" endColumn="30" document="1"/>
                <entry offset="0x1f" hidden="true" document="1"/>
                <entry offset="0x22" startLine="12" startColumn="21" endLine="12" endColumn="40" document="1"/>
                <entry offset="0x29" startLine="13" startColumn="17" endLine="13" endColumn="38" document="1"/>
                <entry offset="0x31" startLine="20" startColumn="13" endLine="20" endColumn="19" document="1"/>
                <entry offset="0x34" startLine="14" startColumn="13" endLine="14" endColumn="30" document="1"/>
                <entry offset="0x3a" hidden="true" document="1"/>
                <entry offset="0x3e" startLine="15" startColumn="21" endLine="15" endColumn="40" document="1"/>
                <entry offset="0x45" startLine="16" startColumn="17" endLine="16" endColumn="38" document="1"/>
                <entry offset="0x4d" startLine="20" startColumn="13" endLine="20" endColumn="19" document="1"/>
                <entry offset="0x50" startLine="17" startColumn="13" endLine="17" endColumn="17" document="1"/>
                <entry offset="0x51" startLine="18" startColumn="21" endLine="18" endColumn="42" document="1"/>
                <entry offset="0x58" startLine="19" startColumn="17" endLine="19" endColumn="38" document="1"/>
                <entry offset="0x60" startLine="20" startColumn="13" endLine="20" endColumn="19" document="1"/>
                <entry offset="0x61" startLine="22" startColumn="17" endLine="22" endColumn="40" document="1"/>
                <entry offset="0x65" startLine="23" startColumn="13" endLine="23" endColumn="21" document="1"/>
                <entry offset="0x67" startLine="24" startColumn="9" endLine="24" endColumn="25" document="1"/>
                <entry offset="0x6e" hidden="true" document="1"/>
                <entry offset="0x72" startLine="26" startColumn="5" endLine="26" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x73">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="x" il_index="0" il_start="0x0" il_end="0x73" attributes="0"/>
                <scope startOffset="0x4" endOffset="0x67">
                    <local name="newX" il_index="1" il_start="0x4" il_end="0x67" attributes="0"/>
                    <scope startOffset="0x22" endOffset="0x31">
                        <local name="s2" il_index="4" il_start="0x22" il_end="0x31" attributes="0"/>
                    </scope>
                    <scope startOffset="0x3e" endOffset="0x4d">
                        <local name="s3" il_index="6" il_start="0x3e" il_end="0x4d" attributes="0"/>
                    </scope>
                    <scope startOffset="0x51" endOffset="0x60">
                        <local name="e1" il_index="7" il_start="0x51" il_end="0x60" attributes="0"/>
                    </scope>
                </scope>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub TestInfiniteLoop()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System

Module Module1
    Sub Main(args As String())

        Dim x As Integer = 0

        Do
            Dim newX As Integer = x + 1
            x = newX
        Loop

    End Sub
End Module
]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.DebugExe)

            compilation.VerifyPdb("Module1.Main",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <entryPoint declaringType="Module1" methodName="Main" parameterNames="args"/>
    <methods>
        <method containingType="Module1" name="Main" parameterNames="args">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="52"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="4" startColumn="5" endLine="4" endColumn="31" document="1"/>
                <entry offset="0x1" startLine="6" startColumn="13" endLine="6" endColumn="29" document="1"/>
                <entry offset="0x3" startLine="8" startColumn="9" endLine="8" endColumn="11" document="1"/>
                <entry offset="0x4" startLine="9" startColumn="17" endLine="9" endColumn="40" document="1"/>
                <entry offset="0x8" startLine="10" startColumn="13" endLine="10" endColumn="21" document="1"/>
                <entry offset="0xa" startLine="11" startColumn="9" endLine="11" endColumn="13" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xd">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="x" il_index="0" il_start="0x0" il_end="0xd" attributes="0"/>
                <scope startOffset="0x4" endOffset="0xa">
                    <local name="newX" il_index="1" il_start="0x4" il_end="0xa" attributes="0"/>
                </scope>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(527647, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527647")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub ExtraSequencePointForEndIf()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System

Public Module MyMod

    Public Sub Main(args As String())
        If (args IsNot Nothing) Then
            Console.WriteLine("Then")
        Else
            Console.WriteLine("Else")
        End If
    End Sub

End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
                    source,
                    TestOptions.DebugExe)

            ' By Design (better than Dev10): <entry offset="0x19" startLine="10" startColumn="9" endLine="10" endColumn="15" document="1"/>
            compilation.VerifyPdb("MyMod.Main",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <entryPoint declaringType="MyMod" methodName="Main" parameterNames="args"/>
    <methods>
        <method containingType="MyMod" name="Main" parameterNames="args">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="1" offset="0"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="5" startColumn="5" endLine="5" endColumn="38" document="1"/>
                <entry offset="0x1" startLine="6" startColumn="9" endLine="6" endColumn="37" document="1"/>
                <entry offset="0x6" hidden="true" document="1"/>
                <entry offset="0x9" startLine="7" startColumn="13" endLine="7" endColumn="38" document="1"/>
                <entry offset="0x14" startLine="10" startColumn="9" endLine="10" endColumn="15" document="1"/>
                <entry offset="0x17" startLine="8" startColumn="9" endLine="8" endColumn="13" document="1"/>
                <entry offset="0x18" startLine="9" startColumn="13" endLine="9" endColumn="38" document="1"/>
                <entry offset="0x23" startLine="10" startColumn="9" endLine="10" endColumn="15" document="1"/>
                <entry offset="0x24" startLine="11" startColumn="5" endLine="11" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x25">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(538821, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538821")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub MissingSequencePointForOptimizedIfThen()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System

Public Module MyMod

    Public Sub Main()
        Console.WriteLine("B")

        If "x"c = "X"c Then
            Console.WriteLine("=")
        End If

        If "z"c <> "z"c Then
            Console.WriteLine("<>")
        End If

        Console.WriteLine("E")
    End Sub

End Module
]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.DebugExe)

            CompileAndVerify(compilation).VerifyIL("MyMod.Main", "
{
  // Code size       34 (0x22)
  .maxstack  1
  .locals init (Boolean V_0,
                Boolean V_1)
 -IL_0000:  nop
 -IL_0001:  ldstr      ""B""
  IL_0006:  call       ""Sub System.Console.WriteLine(String)""
  IL_000b:  nop
 -IL_000c:  ldc.i4.0
  IL_000d:  stloc.0
  IL_000e:  br.s       IL_0010
 -IL_0010:  nop
 -IL_0011:  ldc.i4.0
  IL_0012:  stloc.1
  IL_0013:  br.s       IL_0015
 -IL_0015:  nop
 -IL_0016:  ldstr      ""E""
  IL_001b:  call       ""Sub System.Console.WriteLine(String)""
  IL_0020:  nop
 -IL_0021:  ret
}
", sequencePoints:="MyMod.Main")

            compilation.VerifyPdb("MyMod.Main",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <entryPoint declaringType="MyMod" methodName="Main"/>
    <methods>
        <method containingType="MyMod" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="1" offset="34"/>
                    <slot kind="1" offset="117"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="5" startColumn="5" endLine="5" endColumn="22" document="1"/>
                <entry offset="0x1" startLine="6" startColumn="9" endLine="6" endColumn="31" document="1"/>
                <entry offset="0xc" startLine="8" startColumn="9" endLine="8" endColumn="28" document="1"/>
                <entry offset="0x10" startLine="10" startColumn="9" endLine="10" endColumn="15" document="1"/>
                <entry offset="0x11" startLine="12" startColumn="9" endLine="12" endColumn="29" document="1"/>
                <entry offset="0x15" startLine="14" startColumn="9" endLine="14" endColumn="15" document="1"/>
                <entry offset="0x16" startLine="16" startColumn="9" endLine="16" endColumn="31" document="1"/>
                <entry offset="0x21" startLine="17" startColumn="5" endLine="17" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x22">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub MissingSequencePointForTrivialIfThen()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System

Module Module1

    Sub Main()

        ' one
        If (False) Then
            Dim x As String = "hello"
            Show(x)
        End If

        ' two
        If (False) Then Show("hello")

        Try
        Catch ex As Exception
        Finally
            ' three
            If (False) Then Show("hello")
        End Try

    End Sub


    Function Show(s As String) As Integer
        Console.WriteLine(s)

        Return 1
    End Function

End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
                    source,
                    TestOptions.DebugExe)

            compilation.VerifyPdb("Module1.Main",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <entryPoint declaringType="Module1" methodName="Main"/>
    <methods>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="1" offset="0"/>
                    <slot kind="0" offset="33"/>
                    <slot kind="1" offset="118"/>
                    <slot kind="0" offset="172"/>
                    <slot kind="1" offset="245"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="5" startColumn="5" endLine="5" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="8" startColumn="9" endLine="8" endColumn="24" document="1"/>
                <entry offset="0x5" startLine="11" startColumn="9" endLine="11" endColumn="15" document="1"/>
                <entry offset="0x6" startLine="14" startColumn="9" endLine="14" endColumn="24" document="1"/>
                <entry offset="0xa" hidden="true" document="1"/>
                <entry offset="0xb" startLine="16" startColumn="9" endLine="16" endColumn="12" document="1"/>
                <entry offset="0xe" hidden="true" document="1"/>
                <entry offset="0x15" startLine="17" startColumn="9" endLine="17" endColumn="30" document="1"/>
                <entry offset="0x1d" hidden="true" document="1"/>
                <entry offset="0x1f" startLine="18" startColumn="9" endLine="18" endColumn="16" document="1"/>
                <entry offset="0x20" startLine="20" startColumn="13" endLine="20" endColumn="28" document="1"/>
                <entry offset="0x25" hidden="true" document="1"/>
                <entry offset="0x26" startLine="21" startColumn="9" endLine="21" endColumn="16" document="1"/>
                <entry offset="0x27" startLine="23" startColumn="5" endLine="23" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x28">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <scope startOffset="0xe" endOffset="0x1c">
                    <local name="ex" il_index="3" il_start="0xe" il_end="0x1c" attributes="0"/>
                </scope>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(538944, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538944")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub MissingEndWhileSequencePoint()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System


Module MyMod

    Sub Main(args As String())
        Dim x, y, z As ULong, a, b, c As SByte
        x = 10
        y = 20
        z = 30
        a = 1
        b = 2
        c = 3
        Dim ct As Integer = 100
        Do
            Console.WriteLine("Out={0}", y)
            y = y + 2
            While (x > a)
                Do While ct - 50 > a + b * 10
                    b = b + 1
                    Console.Write("b={0} | ", b)
                    Do Until z <= ct / 4
                        Console.Write("z={0} | ", z)
                        Do
                            Console.Write("c={0} | ", c)
                            c = c * 2
                        Loop Until c > ct / 10
                        z = z - 4
                    Loop
                Loop
                x = x - 5
                Console.WriteLine("x={0}", x)
            End While
        Loop While (y < 25)
    End Sub
End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
                    source,
                    TestOptions.DebugExe)

            ' startLine="33"
            compilation.VerifyPdb("MyMod.Main",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <entryPoint declaringType="MyMod" methodName="Main" parameterNames="args"/>
    <methods>
        <method containingType="MyMod" name="Main" parameterNames="args">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="7"/>
                    <slot kind="0" offset="10"/>
                    <slot kind="0" offset="22"/>
                    <slot kind="0" offset="25"/>
                    <slot kind="0" offset="28"/>
                    <slot kind="0" offset="145"/>
                    <slot kind="1" offset="521"/>
                    <slot kind="1" offset="421"/>
                    <slot kind="1" offset="289"/>
                    <slot kind="1" offset="258"/>
                    <slot kind="1" offset="174"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="31" document="1"/>
                <entry offset="0x1" startLine="8" startColumn="9" endLine="8" endColumn="15" document="1"/>
                <entry offset="0x5" startLine="9" startColumn="9" endLine="9" endColumn="15" document="1"/>
                <entry offset="0x9" startLine="10" startColumn="9" endLine="10" endColumn="15" document="1"/>
                <entry offset="0xd" startLine="11" startColumn="9" endLine="11" endColumn="14" document="1"/>
                <entry offset="0xf" startLine="12" startColumn="9" endLine="12" endColumn="14" document="1"/>
                <entry offset="0x12" startLine="13" startColumn="9" endLine="13" endColumn="14" document="1"/>
                <entry offset="0x15" startLine="14" startColumn="13" endLine="14" endColumn="32" document="1"/>
                <entry offset="0x19" startLine="15" startColumn="9" endLine="15" endColumn="11" document="1"/>
                <entry offset="0x1a" startLine="16" startColumn="13" endLine="16" endColumn="44" document="1"/>
                <entry offset="0x2b" startLine="17" startColumn="13" endLine="17" endColumn="22" document="1"/>
                <entry offset="0x43" hidden="true" document="1"/>
                <entry offset="0x48" hidden="true" document="1"/>
                <entry offset="0x4d" startLine="20" startColumn="21" endLine="20" endColumn="30" document="1"/>
                <entry offset="0x54" startLine="21" startColumn="21" endLine="21" endColumn="49" document="1"/>
                <entry offset="0x66" hidden="true" document="1"/>
                <entry offset="0x68" startLine="23" startColumn="25" endLine="23" endColumn="53" document="1"/>
                <entry offset="0x79" startLine="24" startColumn="25" endLine="24" endColumn="27" document="1"/>
                <entry offset="0x7a" startLine="25" startColumn="29" endLine="25" endColumn="57" document="1"/>
                <entry offset="0x8c" startLine="26" startColumn="29" endLine="26" endColumn="38" document="1"/>
                <entry offset="0x93" startLine="27" startColumn="25" endLine="27" endColumn="47" document="1"/>
                <entry offset="0xa8" hidden="true" document="1"/>
                <entry offset="0xac" startLine="28" startColumn="25" endLine="28" endColumn="34" document="1"/>
                <entry offset="0xc4" startLine="29" startColumn="21" endLine="29" endColumn="25" document="1"/>
                <entry offset="0xc5" startLine="22" startColumn="21" endLine="22" endColumn="41" document="1"/>
                <entry offset="0xdc" hidden="true" document="1"/>
                <entry offset="0xe0" startLine="30" startColumn="17" endLine="30" endColumn="21" document="1"/>
                <entry offset="0xe1" startLine="19" startColumn="17" endLine="19" endColumn="46" document="1"/>
                <entry offset="0xf1" hidden="true" document="1"/>
                <entry offset="0xf8" startLine="31" startColumn="17" endLine="31" endColumn="26" document="1"/>
                <entry offset="0x110" startLine="32" startColumn="17" endLine="32" endColumn="46" document="1"/>
                <entry offset="0x121" startLine="33" startColumn="13" endLine="33" endColumn="22" document="1"/>
                <entry offset="0x122" startLine="18" startColumn="13" endLine="18" endColumn="26" document="1"/>
                <entry offset="0x138" hidden="true" document="1"/>
                <entry offset="0x13f" startLine="34" startColumn="9" endLine="34" endColumn="28" document="1"/>
                <entry offset="0x158" hidden="true" document="1"/>
                <entry offset="0x15f" startLine="35" startColumn="5" endLine="35" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x160">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="x" il_index="0" il_start="0x0" il_end="0x160" attributes="0"/>
                <local name="y" il_index="1" il_start="0x0" il_end="0x160" attributes="0"/>
                <local name="z" il_index="2" il_start="0x0" il_end="0x160" attributes="0"/>
                <local name="a" il_index="3" il_start="0x0" il_end="0x160" attributes="0"/>
                <local name="b" il_index="4" il_start="0x0" il_end="0x160" attributes="0"/>
                <local name="c" il_index="5" il_start="0x0" il_end="0x160" attributes="0"/>
                <local name="ct" il_index="6" il_start="0x0" il_end="0x160" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub TestImplicitLocals()
            Dim source =
<compilation>
    <file>
Option Explicit Off
Option Strict On
Imports System

Module Module1
    Sub Main()
        x = "Hello"
        dim y as string = "world"
        i% = 3
        While i &gt; 0
            Console.WriteLine("{0}, {1}", x, y)
            Console.WriteLine(i)
            q$ = "string"
            i = i% - 1
        End While
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
                                source,
                                TestOptions.DebugExe)

            compilation.VerifyPdb("Module1.Main",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <entryPoint declaringType="Module1" methodName="Main"/>
    <methods>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="0"/>
                    <slot kind="0" offset="56"/>
                    <slot kind="0" offset="180"/>
                    <slot kind="0" offset="25"/>
                    <slot kind="1" offset="72"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="20" document="1"/>
                <entry offset="0x7" startLine="8" startColumn="13" endLine="8" endColumn="34" document="1"/>
                <entry offset="0xd" startLine="9" startColumn="9" endLine="9" endColumn="15" document="1"/>
                <entry offset="0xf" hidden="true" document="1"/>
                <entry offset="0x11" startLine="11" startColumn="13" endLine="11" endColumn="48" document="1"/>
                <entry offset="0x23" startLine="12" startColumn="13" endLine="12" endColumn="33" document="1"/>
                <entry offset="0x2a" startLine="13" startColumn="13" endLine="13" endColumn="26" document="1"/>
                <entry offset="0x30" startLine="14" startColumn="13" endLine="14" endColumn="23" document="1"/>
                <entry offset="0x34" startLine="15" startColumn="9" endLine="15" endColumn="18" document="1"/>
                <entry offset="0x35" startLine="10" startColumn="9" endLine="10" endColumn="20" document="1"/>
                <entry offset="0x3b" hidden="true" document="1"/>
                <entry offset="0x3f" startLine="16" startColumn="5" endLine="16" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x40">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="x" il_index="0" il_start="0x0" il_end="0x40" attributes="0"/>
                <local name="i" il_index="1" il_start="0x0" il_end="0x40" attributes="0"/>
                <local name="q" il_index="2" il_start="0x0" il_end="0x40" attributes="0"/>
                <local name="y" il_index="3" il_start="0x0" il_end="0x40" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub AddRemoveHandler()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System

Module Module1
    Sub Main(args As String())
        Dim del As System.EventHandler =
            Sub(sender As Object, a As EventArgs) Console.Write("unload")

        Dim v = AppDomain.CreateDomain("qq")

        AddHandler (v.DomainUnload), del
        RemoveHandler (v.DomainUnload), del

        AppDomain.Unload(v)    
    End Sub
End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
                    source,
                    TestOptions.DebugExe)

            compilation.VerifyPdb("Module1.Main",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <entryPoint declaringType="Module1" methodName="Main" parameterNames="args"/>
    <methods>
        <method containingType="Module1" name="Main" parameterNames="args">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="123"/>
                </encLocalSlotMap>
                <encLambdaMap>
                    <methodOrdinal>0</methodOrdinal>
                    <lambda offset="46"/>
                </encLambdaMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="4" startColumn="5" endLine="4" endColumn="31" document="1"/>
                <entry offset="0x1" startLine="5" startColumn="13" endLine="6" endColumn="74" document="1"/>
                <entry offset="0x26" startLine="8" startColumn="13" endLine="8" endColumn="45" document="1"/>
                <entry offset="0x31" startLine="10" startColumn="9" endLine="10" endColumn="41" document="1"/>
                <entry offset="0x39" startLine="11" startColumn="9" endLine="11" endColumn="44" document="1"/>
                <entry offset="0x41" startLine="13" startColumn="9" endLine="13" endColumn="28" document="1"/>
                <entry offset="0x48" startLine="14" startColumn="5" endLine="14" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x49">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="del" il_index="0" il_start="0x0" il_end="0x49" attributes="0"/>
                <local name="v" il_index="1" il_start="0x0" il_end="0x49" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub SelectCase_NoCaseBlocks()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System
Module Module1
    Sub Main()
        Dim num As Integer = 0
        Select Case num
        End Select
    End Sub
End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.DebugExe)

            compilation.VerifyPdb("Module1.Main",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <entryPoint declaringType="Module1" methodName="Main"/>
    <methods>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="3" startColumn="5" endLine="3" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="4" startColumn="13" endLine="4" endColumn="31" document="1"/>
                <entry offset="0x3" startLine="5" startColumn="9" endLine="5" endColumn="24" document="1"/>
                <entry offset="0x4" startLine="6" startColumn="9" endLine="6" endColumn="19" document="1"/>
                <entry offset="0x5" startLine="7" startColumn="5" endLine="7" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x6">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="num" il_index="0" il_start="0x0" il_end="0x6" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)

            CompileAndVerify(compilation, expectedOutput:="")
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub SelectCase_SingleCaseStatement()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System
Module Module1
    Sub Main()
        Dim num As Integer = 0
        Select Case num
            Case 1
        End Select

        Select Case num
            Case Else
        End Select
    End Sub
End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.DebugExe)

            compilation.VerifyPdb("Module1.Main",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <entryPoint declaringType="Module1" methodName="Main"/>
    <methods>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="3" startColumn="5" endLine="3" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="4" startColumn="13" endLine="4" endColumn="31" document="1"/>
                <entry offset="0x3" startLine="5" startColumn="9" endLine="5" endColumn="24" document="1"/>
                <entry offset="0xa" startLine="6" startColumn="13" endLine="6" endColumn="19" document="1"/>
                <entry offset="0xd" startLine="7" startColumn="9" endLine="7" endColumn="19" document="1"/>
                <entry offset="0xe" startLine="9" startColumn="9" endLine="9" endColumn="24" document="1"/>
                <entry offset="0x11" startLine="10" startColumn="13" endLine="10" endColumn="22" document="1"/>
                <entry offset="0x14" startLine="11" startColumn="9" endLine="11" endColumn="19" document="1"/>
                <entry offset="0x15" startLine="12" startColumn="5" endLine="12" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x16">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="num" il_index="0" il_start="0x0" il_end="0x16" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)

            CompileAndVerify(compilation, expectedOutput:="")
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub SelectCase_OnlyCaseStatements()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System
Module Module1
    Sub Main()
        Dim num As Integer = 0
        Select Case num
            Case 1
            Case 2
            Case 0, 3 To 8
            Case Else
        End Select
    End Sub
End Module
]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.DebugExe)

            CompileAndVerify(compilation).VerifyIL("Module1.Main", "
{
  // Code size       55 (0x37)
  .maxstack  2
  .locals init (Integer V_0, //num
                Integer V_1,
                Boolean V_2)
 -IL_0000:  nop
 -IL_0001:  ldc.i4.0
  IL_0002:  stloc.0
 -IL_0003:  nop
  IL_0004:  ldloc.0
  IL_0005:  stloc.1
 -IL_0006:  ldloc.1
  IL_0007:  ldc.i4.1
  IL_0008:  ceq
  IL_000a:  stloc.2
 ~IL_000b:  ldloc.2
  IL_000c:  brfalse.s  IL_0010
  IL_000e:  br.s       IL_0035
 -IL_0010:  ldloc.1
  IL_0011:  ldc.i4.2
  IL_0012:  ceq
  IL_0014:  stloc.2
 ~IL_0015:  ldloc.2
  IL_0016:  brfalse.s  IL_001a
  IL_0018:  br.s       IL_0035
 -IL_001a:  ldloc.1
  IL_001b:  brfalse.s  IL_002d
  IL_001d:  ldloc.1
  IL_001e:  ldc.i4.3
  IL_001f:  blt.s      IL_002a
  IL_0021:  ldloc.1
  IL_0022:  ldc.i4.8
  IL_0023:  cgt
  IL_0025:  ldc.i4.0
  IL_0026:  ceq
  IL_0028:  br.s       IL_002b
  IL_002a:  ldc.i4.0
  IL_002b:  br.s       IL_002e
  IL_002d:  ldc.i4.1
  IL_002e:  stloc.2
 ~IL_002f:  ldloc.2
  IL_0030:  brfalse.s  IL_0034
  IL_0032:  br.s       IL_0035
 -IL_0034:  nop
 -IL_0035:  nop
 -IL_0036:  ret
}
", sequencePoints:="Module1.Main")

            compilation.VerifyPdb("Module1.Main",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <entryPoint declaringType="Module1" methodName="Main"/>
    <methods>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="15" offset="32"/>
                    <slot kind="1" offset="32"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="3" startColumn="5" endLine="3" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="4" startColumn="13" endLine="4" endColumn="31" document="1"/>
                <entry offset="0x3" startLine="5" startColumn="9" endLine="5" endColumn="24" document="1"/>
                <entry offset="0x6" startLine="6" startColumn="13" endLine="6" endColumn="19" document="1"/>
                <entry offset="0xb" hidden="true" document="1"/>
                <entry offset="0x10" startLine="7" startColumn="13" endLine="7" endColumn="19" document="1"/>
                <entry offset="0x15" hidden="true" document="1"/>
                <entry offset="0x1a" startLine="8" startColumn="13" endLine="8" endColumn="27" document="1"/>
                <entry offset="0x2f" hidden="true" document="1"/>
                <entry offset="0x34" startLine="9" startColumn="13" endLine="9" endColumn="22" document="1"/>
                <entry offset="0x35" startLine="10" startColumn="9" endLine="10" endColumn="19" document="1"/>
                <entry offset="0x36" startLine="11" startColumn="5" endLine="11" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x37">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="num" il_index="0" il_start="0x0" il_end="0x37" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)
            CompileAndVerify(compilation, expectedOutput:="")
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub SelectCase_SwitchTable()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System
Module Module1
    Sub Main()
        Dim num As Integer = 0
        Select Case num
            Case 1
                Console.WriteLine("1")
            Case 2
                Console.WriteLine("2")
            Case 0, 3, 4, 5, 6, Is = 7, 8
            Case Else
                Console.WriteLine("Else")
        End Select
    End Sub
End Module
]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.DebugExe)

            compilation.VerifyPdb("Module1.Main",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <entryPoint declaringType="Module1" methodName="Main"/>
    <methods>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="3" startColumn="5" endLine="3" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="4" startColumn="13" endLine="4" endColumn="31" document="1"/>
                <entry offset="0x3" startLine="5" startColumn="9" endLine="5" endColumn="24" document="1"/>
                <entry offset="0x30" startLine="6" startColumn="13" endLine="6" endColumn="19" document="1"/>
                <entry offset="0x31" startLine="7" startColumn="17" endLine="7" endColumn="39" document="1"/>
                <entry offset="0x3e" startLine="8" startColumn="13" endLine="8" endColumn="19" document="1"/>
                <entry offset="0x3f" startLine="9" startColumn="17" endLine="9" endColumn="39" document="1"/>
                <entry offset="0x4c" startLine="10" startColumn="13" endLine="10" endColumn="42" document="1"/>
                <entry offset="0x4f" startLine="11" startColumn="13" endLine="11" endColumn="22" document="1"/>
                <entry offset="0x50" startLine="12" startColumn="17" endLine="12" endColumn="42" document="1"/>
                <entry offset="0x5d" startLine="13" startColumn="9" endLine="13" endColumn="19" document="1"/>
                <entry offset="0x5e" startLine="14" startColumn="5" endLine="14" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x5f">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="num" il_index="0" il_start="0x0" il_end="0x5f" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)

            CompileAndVerify(compilation, expectedOutput:="")
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub SelectCase_SwitchTable_TempUsed()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System
Module Module1
    Sub Main()
        Dim num As Integer = 0
        Select Case num + 1
            Case 1
                Console.WriteLine("")
            Case 2
                Console.WriteLine("2")
            Case 0, 3, 4, 5, 6, Is = 7, 8
                Console.WriteLine("0")
            Case Else
                Console.WriteLine("Else")
        End Select
    End Sub
End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.DebugExe)

            compilation.VerifyPdb("Module1.Main",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <entryPoint declaringType="Module1" methodName="Main"/>
    <methods>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="15" offset="32"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="3" startColumn="5" endLine="3" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="4" startColumn="13" endLine="4" endColumn="31" document="1"/>
                <entry offset="0x3" startLine="5" startColumn="9" endLine="5" endColumn="28" document="1"/>
                <entry offset="0x34" startLine="6" startColumn="13" endLine="6" endColumn="19" document="1"/>
                <entry offset="0x35" startLine="7" startColumn="17" endLine="7" endColumn="38" document="1"/>
                <entry offset="0x42" startLine="8" startColumn="13" endLine="8" endColumn="19" document="1"/>
                <entry offset="0x43" startLine="9" startColumn="17" endLine="9" endColumn="39" document="1"/>
                <entry offset="0x50" startLine="10" startColumn="13" endLine="10" endColumn="42" document="1"/>
                <entry offset="0x51" startLine="11" startColumn="17" endLine="11" endColumn="39" document="1"/>
                <entry offset="0x5e" startLine="12" startColumn="13" endLine="12" endColumn="22" document="1"/>
                <entry offset="0x5f" startLine="13" startColumn="17" endLine="13" endColumn="42" document="1"/>
                <entry offset="0x6c" startLine="14" startColumn="9" endLine="14" endColumn="19" document="1"/>
                <entry offset="0x6d" startLine="15" startColumn="5" endLine="15" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x6e">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="num" il_index="0" il_start="0x0" il_end="0x6e" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)

            CompileAndVerify(compilation, expectedOutput:="")
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub SelectCase_IfList()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System
Module Module1
    Sub Main()
        Dim num As Integer = 0
        Select Case num
            Case 1
                Console.WriteLine("1")
            Case 2
                Console.WriteLine("2")
            Case 0, >= 3, <= 8
            Case Else
                Console.WriteLine("Else")
        End Select
    End Sub
End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.DebugExe)

            compilation.VerifyPdb("Module1.Main",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <entryPoint declaringType="Module1" methodName="Main"/>
    <methods>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="15" offset="32"/>
                    <slot kind="1" offset="32"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="3" startColumn="5" endLine="3" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="4" startColumn="13" endLine="4" endColumn="31" document="1"/>
                <entry offset="0x3" startLine="5" startColumn="9" endLine="5" endColumn="24" document="1"/>
                <entry offset="0x6" startLine="6" startColumn="13" endLine="6" endColumn="19" document="1"/>
                <entry offset="0xb" hidden="true" document="1"/>
                <entry offset="0xe" startLine="7" startColumn="17" endLine="7" endColumn="39" document="1"/>
                <entry offset="0x1b" startLine="8" startColumn="13" endLine="8" endColumn="19" document="1"/>
                <entry offset="0x20" hidden="true" document="1"/>
                <entry offset="0x23" startLine="9" startColumn="17" endLine="9" endColumn="39" document="1"/>
                <entry offset="0x30" startLine="10" startColumn="13" endLine="10" endColumn="31" document="1"/>
                <entry offset="0x42" hidden="true" document="1"/>
                <entry offset="0x47" startLine="12" startColumn="17" endLine="12" endColumn="42" document="1"/>
                <entry offset="0x52" startLine="13" startColumn="9" endLine="13" endColumn="19" document="1"/>
                <entry offset="0x53" startLine="14" startColumn="5" endLine="14" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x54">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="num" il_index="0" il_start="0x0" il_end="0x54" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)

            CompileAndVerify(compilation, expectedOutput:="")
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub SelectCase_IfList_TempUsed()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System
Module Module1
    Sub Main()
        Dim num As Integer = 0
        Select Case num + 1
            Case 1
                Console.WriteLine("")
            Case 2
                Console.WriteLine("2")
            Case 0, >= 3, <= 8
                Console.WriteLine("0")
            Case Else
                Console.WriteLine("Else")
        End Select
    End Sub
End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.DebugExe)

            compilation.VerifyPdb("Module1.Main",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <entryPoint declaringType="Module1" methodName="Main"/>
    <methods>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="15" offset="32"/>
                    <slot kind="1" offset="32"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="3" startColumn="5" endLine="3" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="4" startColumn="13" endLine="4" endColumn="31" document="1"/>
                <entry offset="0x3" startLine="5" startColumn="9" endLine="5" endColumn="28" document="1"/>
                <entry offset="0x8" startLine="6" startColumn="13" endLine="6" endColumn="19" document="1"/>
                <entry offset="0xd" hidden="true" document="1"/>
                <entry offset="0x10" startLine="7" startColumn="17" endLine="7" endColumn="38" document="1"/>
                <entry offset="0x1d" startLine="8" startColumn="13" endLine="8" endColumn="19" document="1"/>
                <entry offset="0x22" hidden="true" document="1"/>
                <entry offset="0x25" startLine="9" startColumn="17" endLine="9" endColumn="39" document="1"/>
                <entry offset="0x32" startLine="10" startColumn="13" endLine="10" endColumn="31" document="1"/>
                <entry offset="0x44" hidden="true" document="1"/>
                <entry offset="0x47" startLine="11" startColumn="17" endLine="11" endColumn="39" document="1"/>
                <entry offset="0x54" startLine="13" startColumn="17" endLine="13" endColumn="42" document="1"/>
                <entry offset="0x5f" startLine="14" startColumn="9" endLine="14" endColumn="19" document="1"/>
                <entry offset="0x60" startLine="15" startColumn="5" endLine="15" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x61">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="num" il_index="0" il_start="0x0" il_end="0x61" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)

            CompileAndVerify(compilation, expectedOutput:="")
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub SelectCase_String_SwitchTable_Hash()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System
Module Module1
    Sub Main()
        Dim str As String = "00"
        Select Case str
            Case "01"
                Console.WriteLine("01")
            Case "02"
                Console.WriteLine("02")
            Case "00", "03", "04", "05", "06", "07", "08"
            Case Else
                Console.WriteLine("Else")
        End Select
    End Sub
End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.DebugExe)

            compilation.VerifyPdb("Module1.Main",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <entryPoint declaringType="Module1" methodName="Main"/>
    <methods>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="temp"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="3" startColumn="5" endLine="3" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="4" startColumn="13" endLine="4" endColumn="33" document="1"/>
                <entry offset="0x7" startLine="5" startColumn="9" endLine="5" endColumn="24" document="1"/>
                <entry offset="0x135" startLine="6" startColumn="13" endLine="6" endColumn="22" document="1"/>
                <entry offset="0x136" startLine="7" startColumn="17" endLine="7" endColumn="40" document="1"/>
                <entry offset="0x143" startLine="8" startColumn="13" endLine="8" endColumn="22" document="1"/>
                <entry offset="0x144" startLine="9" startColumn="17" endLine="9" endColumn="40" document="1"/>
                <entry offset="0x151" startLine="10" startColumn="13" endLine="10" endColumn="58" document="1"/>
                <entry offset="0x154" startLine="11" startColumn="13" endLine="11" endColumn="22" document="1"/>
                <entry offset="0x155" startLine="12" startColumn="17" endLine="12" endColumn="42" document="1"/>
                <entry offset="0x162" startLine="13" startColumn="9" endLine="13" endColumn="19" document="1"/>
                <entry offset="0x163" startLine="14" startColumn="5" endLine="14" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x164">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="str" il_index="0" il_start="0x0" il_end="0x164" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)

            CompileAndVerify(compilation, expectedOutput:="")
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub SelectCase_String_SwitchTable_NonHash()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System
Module Module1
    Sub Main()
        Dim str As String = "00"
        Select Case str
            Case "01"
                Console.WriteLine("01")
            Case "02"
            Case "00"
                Console.WriteLine("00")
            Case Else
        End Select
    End Sub
End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.DebugExe)

            compilation.VerifyPdb("Module1.Main",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <entryPoint declaringType="Module1" methodName="Main"/>
    <methods>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="3" startColumn="5" endLine="3" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="4" startColumn="13" endLine="4" endColumn="33" document="1"/>
                <entry offset="0x7" startLine="5" startColumn="9" endLine="5" endColumn="24" document="1"/>
                <entry offset="0x34" startLine="6" startColumn="13" endLine="6" endColumn="22" document="1"/>
                <entry offset="0x35" startLine="7" startColumn="17" endLine="7" endColumn="40" document="1"/>
                <entry offset="0x42" startLine="8" startColumn="13" endLine="8" endColumn="22" document="1"/>
                <entry offset="0x45" startLine="9" startColumn="13" endLine="9" endColumn="22" document="1"/>
                <entry offset="0x46" startLine="10" startColumn="17" endLine="10" endColumn="40" document="1"/>
                <entry offset="0x53" startLine="11" startColumn="13" endLine="11" endColumn="22" document="1"/>
                <entry offset="0x56" startLine="12" startColumn="9" endLine="12" endColumn="19" document="1"/>
                <entry offset="0x57" startLine="13" startColumn="5" endLine="13" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x58">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="str" il_index="0" il_start="0x0" il_end="0x58" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)

            CompileAndVerify(compilation, expectedOutput:="00")
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub SelectCase_String_IfList()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System
Module Module1
    Sub Main()
        Dim str As String = "00"
        Select Case str
            Case "01"
                Console.WriteLine("01")
            Case "02", 3.ToString()
            Case "00"
                Console.WriteLine("00")
        End Select
    End Sub
End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.DebugExe)

            compilation.VerifyPdb("Module1.Main",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <entryPoint declaringType="Module1" methodName="Main"/>
    <methods>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="15" offset="34"/>
                    <slot kind="1" offset="34"/>
                    <slot kind="temp"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="3" startColumn="5" endLine="3" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="4" startColumn="13" endLine="4" endColumn="33" document="1"/>
                <entry offset="0x7" startLine="5" startColumn="9" endLine="5" endColumn="24" document="1"/>
                <entry offset="0xa" startLine="6" startColumn="13" endLine="6" endColumn="22" document="1"/>
                <entry offset="0x1a" hidden="true" document="1"/>
                <entry offset="0x1d" startLine="7" startColumn="17" endLine="7" endColumn="40" document="1"/>
                <entry offset="0x2a" startLine="8" startColumn="13" endLine="8" endColumn="36" document="1"/>
                <entry offset="0x4f" hidden="true" document="1"/>
                <entry offset="0x54" startLine="9" startColumn="13" endLine="9" endColumn="22" document="1"/>
                <entry offset="0x64" hidden="true" document="1"/>
                <entry offset="0x67" startLine="10" startColumn="17" endLine="10" endColumn="40" document="1"/>
                <entry offset="0x72" startLine="11" startColumn="9" endLine="11" endColumn="19" document="1"/>
                <entry offset="0x73" startLine="12" startColumn="5" endLine="12" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x74">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="str" il_index="0" il_start="0x0" il_end="0x74" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)

            CompileAndVerify(compilation, expectedOutput:="00")
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub DontEmit_AnonymousType_NoKeys()
            Dim source =
<compilation>
    <file><![CDATA[
Class C1
    Sub Method()
        Dim o = New With { .a = 1 }
    End Sub
End Class
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.DebugDll)

            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="C1" name="Method">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="2" startColumn="5" endLine="2" endColumn="17" document="1"/>
                <entry offset="0x1" startLine="3" startColumn="13" endLine="3" endColumn="36" document="1"/>
                <entry offset="0x8" startLine="4" startColumn="5" endLine="4" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x9">
                <currentnamespace name=""/>
                <local name="o" il_index="0" il_start="0x0" il_end="0x9" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub DontEmit_AnonymousType_WithKeys()
            Dim source =
<compilation>
    <file><![CDATA[
Class C1
    Sub Method()
        Dim o = New With { Key .a = 1 }
    End Sub
End Class
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.DebugDll)

            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="C1" name="Method">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="2" startColumn="5" endLine="2" endColumn="17" document="1"/>
                <entry offset="0x1" startLine="3" startColumn="13" endLine="3" endColumn="40" document="1"/>
                <entry offset="0x8" startLine="4" startColumn="5" endLine="4" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x9">
                <currentnamespace name=""/>
                <local name="o" il_index="0" il_start="0x0" il_end="0x9" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(727419, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/727419")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub Bug727419()
            Dim source =
<compilation>
    <file><![CDATA[
Option Strict Off
Option Explicit Off
Imports System

Class GooDerived
    Public Sub ComputeMatrix(ByVal rank As Integer)
        Dim I As Integer
        Dim J As Long
        Dim q() As Long
        Dim count As Long
        Dim dims() As Long

        ' allocate space for arrays
        ReDim q(rank)
        ReDim dims(rank)

        ' create the dimensions
        count = 1
        For I = 0 To rank - 1
            q(I) = 0
            dims(I) = CLng(2 ^ I)
            count *= dims(I)
        Next I
    End Sub

End Class

Module Variety
    Sub Main()
        Dim a As New GooDerived()
        a.ComputeMatrix(2)
    End Sub
End Module
' End of File
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
                    source,
                    TestOptions.DebugExe)

            compilation.VerifyPdb("GooDerived.ComputeMatrix",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <entryPoint declaringType="Variety" methodName="Main"/>
    <methods>
        <method containingType="GooDerived" name="ComputeMatrix" parameterNames="rank">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="30"/>
                    <slot kind="0" offset="53"/>
                    <slot kind="0" offset="78"/>
                    <slot kind="0" offset="105"/>
                    <slot kind="11" offset="271"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="52" document="1"/>
                <entry offset="0x1" startLine="14" startColumn="15" endLine="14" endColumn="22" document="1"/>
                <entry offset="0xa" startLine="15" startColumn="15" endLine="15" endColumn="25" document="1"/>
                <entry offset="0x14" startLine="18" startColumn="9" endLine="18" endColumn="18" document="1"/>
                <entry offset="0x17" startLine="19" startColumn="9" endLine="19" endColumn="30" document="1"/>
                <entry offset="0x1e" hidden="true" document="1"/>
                <entry offset="0x20" startLine="20" startColumn="13" endLine="20" endColumn="21" document="1"/>
                <entry offset="0x25" startLine="21" startColumn="13" endLine="21" endColumn="34" document="1"/>
                <entry offset="0x3f" startLine="22" startColumn="13" endLine="22" endColumn="29" document="1"/>
                <entry offset="0x46" startLine="23" startColumn="9" endLine="23" endColumn="15" document="1"/>
                <entry offset="0x4a" hidden="true" document="1"/>
                <entry offset="0x4f" startLine="24" startColumn="5" endLine="24" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x50">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="I" il_index="0" il_start="0x0" il_end="0x50" attributes="0"/>
                <local name="J" il_index="1" il_start="0x0" il_end="0x50" attributes="0"/>
                <local name="q" il_index="2" il_start="0x0" il_end="0x50" attributes="0"/>
                <local name="count" il_index="3" il_start="0x0" il_end="0x50" attributes="0"/>
                <local name="dims" il_index="4" il_start="0x0" il_end="0x50" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(722627, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/722627")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub Bug722627()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System
Friend Module SubMod
    Sub Main()
L0:
        GoTo L2
L1:
        Exit Sub
L2:
        GoTo L1
    End Sub
End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
                    source,
                    TestOptions.DebugExe)

            compilation.VerifyPdb("SubMod.Main",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <entryPoint declaringType="SubMod" methodName="Main"/>
    <methods>
        <method containingType="SubMod" name="Main">
            <sequencePoints>
                <entry offset="0x0" startLine="3" startColumn="5" endLine="3" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="4" startColumn="1" endLine="4" endColumn="4" document="1"/>
                <entry offset="0x2" startLine="5" startColumn="9" endLine="5" endColumn="16" document="1"/>
                <entry offset="0x4" startLine="6" startColumn="1" endLine="6" endColumn="4" document="1"/>
                <entry offset="0x5" startLine="7" startColumn="9" endLine="7" endColumn="17" document="1"/>
                <entry offset="0x7" startLine="8" startColumn="1" endLine="8" endColumn="4" document="1"/>
                <entry offset="0x8" startLine="9" startColumn="9" endLine="9" endColumn="16" document="1"/>
                <entry offset="0xa" startLine="10" startColumn="5" endLine="10" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xb">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(543703, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543703")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub DontIncludeMethodAttributesInSeqPoint()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System
Module M1
    Sub Main()
        S()
    End Sub

    <System.Runtime.InteropServices.PreserveSigAttribute()>
    <CLSCompliantAttribute(False)>
    Public Sub S()

    End Sub
End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.DebugDll)

            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="M1" name="Main">
            <sequencePoints>
                <entry offset="0x0" startLine="3" startColumn="5" endLine="3" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="4" startColumn="9" endLine="4" endColumn="12" document="1"/>
                <entry offset="0x7" startLine="5" startColumn="5" endLine="5" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x8">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
            </scope>
        </method>
        <method containingType="M1" name="S">
            <sequencePoints>
                <entry offset="0x0" startLine="9" startColumn="5" endLine="9" endColumn="19" document="1"/>
                <entry offset="0x1" startLine="11" startColumn="5" endLine="11" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="M1" methodName="Main"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(529300, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529300")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub DontShowOperatorNameCTypeInLocals()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System

Module Module1

    Class B2
        Public f As Integer

        Public Sub New(x As Integer)
            f = x
        End Sub

        Shared Widening Operator CType(x As Integer) As B2
            Return New B2(x)
        End Operator
    End Class

    Sub Main()
        Dim x As Integer = 11
        Dim b2 As B2 = x
    End Sub
End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.DebugDll)

            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="35"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="17" startColumn="5" endLine="17" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="18" startColumn="13" endLine="18" endColumn="30" document="1"/>
                <entry offset="0x4" startLine="19" startColumn="13" endLine="19" endColumn="25" document="1"/>
                <entry offset="0xb" startLine="20" startColumn="5" endLine="20" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xc">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="x" il_index="0" il_start="0x0" il_end="0xc" attributes="0"/>
                <local name="b2" il_index="1" il_start="0x0" il_end="0xc" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+B2" name=".ctor" parameterNames="x">
            <sequencePoints>
                <entry offset="0x0" startLine="8" startColumn="9" endLine="8" endColumn="37" document="1"/>
                <entry offset="0x8" startLine="9" startColumn="13" endLine="9" endColumn="18" document="1"/>
                <entry offset="0xf" startLine="10" startColumn="9" endLine="10" endColumn="16" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <importsforward declaringType="Module1" methodName="Main"/>
            </scope>
        </method>
        <method containingType="Module1+B2" name="op_Implicit" parameterNames="x">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="21" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="12" startColumn="9" endLine="12" endColumn="59" document="1"/>
                <entry offset="0x1" startLine="13" startColumn="13" endLine="13" endColumn="29" document="1"/>
                <entry offset="0xa" startLine="14" startColumn="9" endLine="14" endColumn="21" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xc">
                <importsforward declaringType="Module1" methodName="Main"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(760994, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/760994")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub Bug760994()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System

Class CLAZZ
    Public FLD1 As Integer = 1
    Public Event Load As Action
    Public FLD2 As Integer = 1

    Public Sub New()

    End Sub

    Private Sub frmMain_Load() Handles Me.Load
    End Sub
End Class


Module Program
    Sub Main(args As String())
        Dim c As New CLAZZ
    End Sub
End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.DebugDll)

            compilation.VerifyPdb("CLAZZ..ctor",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="CLAZZ" name=".ctor">
            <sequencePoints>
                <entry offset="0x0" startLine="8" startColumn="5" endLine="8" endColumn="21" document="1"/>
                <entry offset="0x1b" startLine="4" startColumn="12" endLine="4" endColumn="31" document="1"/>
                <entry offset="0x22" startLine="6" startColumn="12" endLine="6" endColumn="31" document="1"/>
                <entry offset="0x29" startLine="10" startColumn="5" endLine="10" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2a">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub WRN_PDBConstantStringValueTooLong()
            Dim longStringValue = New String("a"c, 2050)

            Dim source =
            <compilation>
                <file>
Imports System

Module Module1

    Sub Main()
        Const goo as String = "<%= longStringValue %>"

        Console.WriteLine("Hello Word.")
        Console.WriteLine(goo)
    End Sub
End Module
</file>
            </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.DebugExe)

            Dim exebits = New IO.MemoryStream()
            Dim pdbbits = New IO.MemoryStream()
            Dim result = compilation.Emit(exebits, pdbbits)
            result.Diagnostics.Verify()

            'this new warning was abandoned

            'result.Diagnostics.Verify(Diagnostic(ERRID.WRN_PDBConstantStringValueTooLong).WithArguments("goo", longStringValue.Substring(0, 20) & "..."))

            ''ensure that the warning is suppressable
            'compilation = CreateCompilationWithMscorlibAndVBRuntime(source, OptionsExe.WithDebugInformationKind(Common.DebugInformationKind.Full).WithOptimizations(False).
            '    WithSpecificDiagnosticOptions(New Dictionary(Of Integer, ReportWarning) From {{CInt(ERRID.WRN_PDBConstantStringValueTooLong), ReportWarning.Suppress}}))
            'result = compilation.Emit(exebits, Nothing, "DontCare", pdbbits, Nothing)
            'result.Diagnostics.Verify()

            ''ensure that the warning can be turned into an error
            'compilation = CreateCompilationWithMscorlibAndVBRuntime(source, OptionsExe.WithDebugInformationKind(Common.DebugInformationKind.Full).WithOptimizations(False).
            '    WithSpecificDiagnosticOptions(New Dictionary(Of Integer, ReportWarning) From {{CInt(ERRID.WRN_PDBConstantStringValueTooLong), ReportWarning.Error}}))
            'result = compilation.Emit(exebits, Nothing, "DontCare", pdbbits, Nothing)
            'Assert.False(result.Success)
            'result.Diagnostics.Verify(Diagnostic(ERRID.WRN_PDBConstantStringValueTooLong).WithArguments("goo", longStringValue.Substring(0, 20) & "...").WithWarningAsError(True),
            '                              Diagnostic(ERRID.ERR_WarningTreatedAsError).WithArguments("The value assigned to the constant string 'goo' is too long to be used in a PDB file. Consider shortening the value, otherwise the string's value will not be visible in the debugger. Only the debug experience is affected."))

        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub NoDebugInfoForEmbeddedSymbols()
            Dim source =
<compilation>
    <file>
Imports Microsoft.VisualBasic.Strings

Public Class C
    Public Shared Function F(z As Integer) As Char
        Return ChrW(z)
    End Function
End Class
    </file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.DebugDll.WithEmbedVbCoreRuntime(True))

            ' Dev11 generates debug info for embedded symbols. There is no reason to do so since the source code is not available to the user.

            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="C" name="F" parameterNames="z">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="4" startColumn="5" endLine="4" endColumn="51" document="1"/>
                <entry offset="0x1" startLine="5" startColumn="9" endLine="5" endColumn="23" document="1"/>
                <entry offset="0xa" startLine="6" startColumn="5" endLine="6" endColumn="17" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xc">
                <type name="Microsoft.VisualBasic.Strings" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="F" il_index="0" il_start="0x0" il_end="0xc" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(797482, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/797482")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub Bug797482()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System

Module Module1
    Sub Main()
        Console.WriteLine(MakeIncrementer(5)(2))
    End Sub
    Function MakeIncrementer(n As Integer) As Func(Of Integer, Integer)
        Return Function(i)
            Return i + n
        End Function
    End Function
End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.DebugDll)

            compilation.VerifyPdb("Module1.MakeIncrementer",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="Module1" name="MakeIncrementer" parameterNames="n">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="30" offset="-1"/>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
                <encLambdaMap>
                    <methodOrdinal>1</methodOrdinal>
                    <closure offset="-1"/>
                    <lambda offset="7" closure="0"/>
                </encLambdaMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="7" startColumn="5" endLine="7" endColumn="72" document="1"/>
                <entry offset="0x1" hidden="true" document="1"/>
                <entry offset="0xe" startLine="8" startColumn="9" endLine="10" endColumn="21" document="1"/>
                <entry offset="0x1d" startLine="11" startColumn="5" endLine="11" endColumn="17" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x1f">
                <importsforward declaringType="Module1" methodName="Main"/>
                <local name="$VB$Closure_0" il_index="0" il_start="0x0" il_end="0x1f" attributes="0"/>
                <local name="MakeIncrementer" il_index="1" il_start="0x0" il_end="0x1f" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        ''' <summary>
        ''' If a synthesized .ctor contains user code (field initializers),
        ''' the method must have a sequence point at
        ''' offset 0 for correct stepping behavior.
        ''' </summary>
        <WorkItem(804681, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/804681")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub DefaultConstructorWithInitializer()
            Dim source =
<compilation>
    <file><![CDATA[
Class C
    Private o As Object = New Object()
End Class
]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.DebugDll)

            compilation.VerifyPdb("C..ctor",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="C" name=".ctor">
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="1"/>
                <entry offset="0x7" startLine="2" startColumn="13" endLine="2" endColumn="39" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x18">
                <currentnamespace name=""/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        ''' <summary>
        ''' If a synthesized method contains any user code,
        ''' the method must have a sequence point at
        ''' offset 0 for correct stepping behavior.
        ''' </summary>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub SequencePointAtOffset0()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System
Module M
    Private Fn As Func(Of Object, Integer) = Function(x)
            Dim f As Func(Of Object, Integer) = Function(o) 1
            Dim g As Func(Of Func(Of Object, Integer), Func(Of Object, Integer)) = Function(h) Function(y) h(y)
            Return g(f)(Nothing)
        End Function
End Module
]]></file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.DebugDll)

            compilation.VerifyPdb(
 <symbols>
     <files>
         <file id="1" name="" language="VB"/>
     </files>
     <methods>
         <method containingType="M" name=".cctor">
             <customDebugInfo>
                 <encLambdaMap>
                     <methodOrdinal>0</methodOrdinal>
                     <closure offset="-84"/>
                     <lambda offset="-243"/>
                     <lambda offset="-182"/>
                     <lambda offset="-84"/>
                     <lambda offset="-72" closure="0"/>
                 </encLambdaMap>
             </customDebugInfo>
             <sequencePoints>
                 <entry offset="0x0" startLine="3" startColumn="13" endLine="7" endColumn="21" document="1"/>
             </sequencePoints>
             <scope startOffset="0x0" endOffset="0x16">
                 <namespace name="System" importlevel="file"/>
                 <currentnamespace name=""/>
             </scope>
         </method>
         <method containingType="M+_Closure$__" name="_Lambda$__0-0" parameterNames="x">
             <customDebugInfo>
                 <encLocalSlotMap>
                     <slot kind="21" offset="-243"/>
                     <slot kind="0" offset="-214"/>
                     <slot kind="0" offset="-151"/>
                 </encLocalSlotMap>
             </customDebugInfo>
             <sequencePoints>
                 <entry offset="0x0" startLine="3" startColumn="46" endLine="3" endColumn="57" document="1"/>
                 <entry offset="0x1" startLine="4" startColumn="17" endLine="4" endColumn="62" document="1"/>
                 <entry offset="0x26" startLine="5" startColumn="17" endLine="5" endColumn="112" document="1"/>
                 <entry offset="0x4b" startLine="6" startColumn="13" endLine="6" endColumn="33" document="1"/>
                 <entry offset="0x5b" startLine="7" startColumn="9" endLine="7" endColumn="21" document="1"/>
             </sequencePoints>
             <scope startOffset="0x0" endOffset="0x5d">
                 <importsforward declaringType="M" methodName=".cctor"/>
                 <local name="f" il_index="1" il_start="0x0" il_end="0x5d" attributes="0"/>
                 <local name="g" il_index="2" il_start="0x0" il_end="0x5d" attributes="0"/>
             </scope>
         </method>
         <method containingType="M+_Closure$__" name="_Lambda$__0-1" parameterNames="o">
             <customDebugInfo>
                 <encLocalSlotMap>
                     <slot kind="21" offset="-182"/>
                 </encLocalSlotMap>
             </customDebugInfo>
             <sequencePoints>
                 <entry offset="0x0" startLine="4" startColumn="49" endLine="4" endColumn="60" document="1"/>
                 <entry offset="0x1" startLine="4" startColumn="61" endLine="4" endColumn="62" document="1"/>
             </sequencePoints>
             <scope startOffset="0x0" endOffset="0x7">
                 <importsforward declaringType="M" methodName=".cctor"/>
             </scope>
         </method>
         <method containingType="M+_Closure$__" name="_Lambda$__0-2" parameterNames="h">
             <customDebugInfo>
                 <encLocalSlotMap>
                     <slot kind="30" offset="-84"/>
                     <slot kind="21" offset="-84"/>
                 </encLocalSlotMap>
             </customDebugInfo>
             <sequencePoints>
                 <entry offset="0x0" startLine="5" startColumn="84" endLine="5" endColumn="95" document="1"/>
                 <entry offset="0x1" hidden="true" document="1"/>
                 <entry offset="0xe" startLine="5" startColumn="96" endLine="5" endColumn="112" document="1"/>
             </sequencePoints>
             <scope startOffset="0x0" endOffset="0x1f">
                 <importsforward declaringType="M" methodName=".cctor"/>
                 <local name="$VB$Closure_0" il_index="0" il_start="0x0" il_end="0x1f" attributes="0"/>
             </scope>
         </method>
         <method containingType="M+_Closure$__0-0" name="_Lambda$__3" parameterNames="y">
             <customDebugInfo>
                 <encLocalSlotMap>
                     <slot kind="21" offset="-72"/>
                 </encLocalSlotMap>
             </customDebugInfo>
             <sequencePoints>
                 <entry offset="0x0" startLine="5" startColumn="96" endLine="5" endColumn="107" document="1"/>
                 <entry offset="0x1" startLine="5" startColumn="108" endLine="5" endColumn="112" document="1"/>
             </sequencePoints>
             <scope startOffset="0x0" endOffset="0x17">
                 <importsforward declaringType="M" methodName=".cctor"/>
             </scope>
         </method>
     </methods>
 </symbols>)
        End Sub

        <WorkItem(846228, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/846228")>
        <WorkItem(845078, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/845078")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub RaiseEvent001()
            Dim source =
<compilation>
    <file><![CDATA[
Public Class IntervalUpdate
    Public Shared Sub Update()
        RaiseEvent IntervalElapsed()
    End Sub
    
    Shared Sub Main()
        Update()
    End Sub

    Public Shared Event IntervalElapsed()
End Class
]]></file>
</compilation>

            Dim defines = PredefinedPreprocessorSymbols.AddPredefinedPreprocessorSymbols(OutputKind.ConsoleApplication)
            defines = defines.Add(KeyValuePairUtil.Create("_MyType", CObj("Console")))

            Dim parseOptions = New VisualBasicParseOptions(preprocessorSymbols:=defines)

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.DebugDll.WithParseOptions(parseOptions))

            CompileAndVerify(compilation).VerifyIL("IntervalUpdate.Update", "
{
  // Code size       18 (0x12)
  .maxstack  1
  .locals init (IntervalUpdate.IntervalElapsedEventHandler V_0)
 -IL_0000:  nop
 -IL_0001:  ldsfld     ""IntervalUpdate.IntervalElapsedEvent As IntervalUpdate.IntervalElapsedEventHandler""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0011
  IL_000a:  ldloc.0
  IL_000b:  callvirt   ""Sub IntervalUpdate.IntervalElapsedEventHandler.Invoke()""
  IL_0010:  nop
 -IL_0011:  ret
}
", sequencePoints:="IntervalUpdate.Update")

            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="My.MyComputer" name=".ctor">
            <sequencePoints>
                <entry offset="0x0" startLine="109" startColumn="9" endLine="109" endColumn="25" document="1"/>
                <entry offset="0x1" startLine="110" startColumn="13" endLine="110" endColumn="25" document="1"/>
                <entry offset="0x8" startLine="111" startColumn="9" endLine="111" endColumn="16" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x9">
                <currentnamespace name="My"/>
            </scope>
        </method>
        <method containingType="My.MyProject" name=".cctor">
            <sequencePoints>
                <entry offset="0x0" startLine="128" startColumn="26" endLine="128" endColumn="97" document="1"/>
                <entry offset="0xa" startLine="139" startColumn="26" endLine="139" endColumn="95" document="1"/>
                <entry offset="0x14" startLine="150" startColumn="26" endLine="150" endColumn="136" document="1"/>
                <entry offset="0x1e" startLine="286" startColumn="26" endLine="286" endColumn="105" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x29">
                <importsforward declaringType="My.MyComputer" methodName=".ctor"/>
            </scope>
        </method>
        <method containingType="My.MyProject" name="get_Computer">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="123" startColumn="13" endLine="123" endColumn="16" document="1"/>
                <entry offset="0x1" startLine="124" startColumn="17" endLine="124" endColumn="62" document="1"/>
                <entry offset="0xe" startLine="125" startColumn="13" endLine="125" endColumn="20" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <importsforward declaringType="My.MyComputer" methodName=".ctor"/>
                <local name="Computer" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject" name="get_Application">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="135" startColumn="13" endLine="135" endColumn="16" document="1"/>
                <entry offset="0x1" startLine="136" startColumn="17" endLine="136" endColumn="57" document="1"/>
                <entry offset="0xe" startLine="137" startColumn="13" endLine="137" endColumn="20" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <importsforward declaringType="My.MyComputer" methodName=".ctor"/>
                <local name="Application" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject" name="get_User">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="146" startColumn="13" endLine="146" endColumn="16" document="1"/>
                <entry offset="0x1" startLine="147" startColumn="17" endLine="147" endColumn="58" document="1"/>
                <entry offset="0xe" startLine="148" startColumn="13" endLine="148" endColumn="20" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <importsforward declaringType="My.MyComputer" methodName=".ctor"/>
                <local name="User" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject" name="get_WebServices">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="239" startColumn="14" endLine="239" endColumn="17" document="1"/>
                <entry offset="0x1" startLine="240" startColumn="17" endLine="240" endColumn="67" document="1"/>
                <entry offset="0xe" startLine="241" startColumn="13" endLine="241" endColumn="20" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <importsforward declaringType="My.MyComputer" methodName=".ctor"/>
                <local name="WebServices" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="IntervalUpdate" name="Update">
            <sequencePoints>
                <entry offset="0x0" startLine="2" startColumn="5" endLine="2" endColumn="31" document="1"/>
                <entry offset="0x1" startLine="3" startColumn="9" endLine="3" endColumn="37" document="1"/>
                <entry offset="0x11" startLine="4" startColumn="5" endLine="4" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x12">
                <currentnamespace name=""/>
            </scope>
        </method>
        <method containingType="IntervalUpdate" name="Main">
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="22" document="1"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="17" document="1"/>
                <entry offset="0x7" startLine="8" startColumn="5" endLine="8" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x8">
                <importsforward declaringType="IntervalUpdate" methodName="Update"/>
            </scope>
        </method>
        <method containingType="My.MyProject+MyWebServices" name="Equals" parameterNames="o">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="249" startColumn="13" endLine="249" endColumn="75" document="1"/>
                <entry offset="0x1" startLine="250" startColumn="17" endLine="250" endColumn="40" document="1"/>
                <entry offset="0x10" startLine="251" startColumn="13" endLine="251" endColumn="25" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x12">
                <currentnamespace name="My"/>
                <local name="Equals" il_index="0" il_start="0x0" il_end="0x12" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject+MyWebServices" name="GetHashCode">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="253" startColumn="13" endLine="253" endColumn="63" document="1"/>
                <entry offset="0x1" startLine="254" startColumn="17" endLine="254" endColumn="42" document="1"/>
                <entry offset="0xa" startLine="255" startColumn="13" endLine="255" endColumn="25" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xc">
                <importsforward declaringType="My.MyProject+MyWebServices" methodName="Equals" parameterNames="o"/>
                <local name="GetHashCode" il_index="0" il_start="0x0" il_end="0xc" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject+MyWebServices" name="GetType">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="257" startColumn="13" endLine="257" endColumn="72" document="1"/>
                <entry offset="0x1" startLine="258" startColumn="17" endLine="258" endColumn="46" document="1"/>
                <entry offset="0xe" startLine="259" startColumn="13" endLine="259" endColumn="25" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <importsforward declaringType="My.MyProject+MyWebServices" methodName="Equals" parameterNames="o"/>
                <local name="GetType" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject+MyWebServices" name="ToString">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="261" startColumn="13" endLine="261" endColumn="59" document="1"/>
                <entry offset="0x1" startLine="262" startColumn="17" endLine="262" endColumn="39" document="1"/>
                <entry offset="0xa" startLine="263" startColumn="13" endLine="263" endColumn="25" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xc">
                <importsforward declaringType="My.MyProject+MyWebServices" methodName="Equals" parameterNames="o"/>
                <local name="ToString" il_index="0" il_start="0x0" il_end="0xc" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject+MyWebServices" name="Create__Instance__" parameterNames="instance">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                    <slot kind="1" offset="0"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="266" startColumn="12" endLine="266" endColumn="95" document="1"/>
                <entry offset="0x1" startLine="267" startColumn="17" endLine="267" endColumn="44" document="1"/>
                <entry offset="0xb" hidden="true" document="1"/>
                <entry offset="0xe" startLine="268" startColumn="21" endLine="268" endColumn="35" document="1"/>
                <entry offset="0x16" startLine="269" startColumn="17" endLine="269" endColumn="21" document="1"/>
                <entry offset="0x17" startLine="270" startColumn="21" endLine="270" endColumn="36" document="1"/>
                <entry offset="0x1b" startLine="272" startColumn="13" endLine="272" endColumn="25" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x1d">
                <importsforward declaringType="My.MyProject+MyWebServices" methodName="Equals" parameterNames="o"/>
                <local name="Create__Instance__" il_index="0" il_start="0x0" il_end="0x1d" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject+MyWebServices" name="Dispose__Instance__" parameterNames="instance">
            <sequencePoints>
                <entry offset="0x0" startLine="275" startColumn="13" endLine="275" endColumn="71" document="1"/>
                <entry offset="0x1" startLine="276" startColumn="17" endLine="276" endColumn="35" document="1"/>
                <entry offset="0x8" startLine="277" startColumn="13" endLine="277" endColumn="20" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x9">
                <importsforward declaringType="My.MyProject+MyWebServices" methodName="Equals" parameterNames="o"/>
            </scope>
        </method>
        <method containingType="My.MyProject+MyWebServices" name=".ctor">
            <sequencePoints>
                <entry offset="0x0" startLine="281" startColumn="13" endLine="281" endColumn="29" document="1"/>
                <entry offset="0x1" startLine="282" startColumn="16" endLine="282" endColumn="28" document="1"/>
                <entry offset="0x8" startLine="283" startColumn="13" endLine="283" endColumn="20" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x9">
                <importsforward declaringType="My.MyProject+MyWebServices" methodName="Equals" parameterNames="o"/>
            </scope>
        </method>
        <method containingType="My.MyProject+ThreadSafeObjectProvider`1" name="get_GetInstance">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                    <slot kind="1" offset="0"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="343" startColumn="17" endLine="343" endColumn="20" document="1"/>
                <entry offset="0x1" startLine="344" startColumn="21" endLine="344" endColumn="59" document="1"/>
                <entry offset="0xf" hidden="true" document="1"/>
                <entry offset="0x12" startLine="344" startColumn="60" endLine="344" endColumn="87" document="1"/>
                <entry offset="0x1c" startLine="345" startColumn="21" endLine="345" endColumn="47" document="1"/>
                <entry offset="0x24" startLine="346" startColumn="17" endLine="346" endColumn="24" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x26">
                <importsforward declaringType="My.MyProject+MyWebServices" methodName="Equals" parameterNames="o"/>
                <local name="GetInstance" il_index="0" il_start="0x0" il_end="0x26" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject+ThreadSafeObjectProvider`1" name=".ctor">
            <sequencePoints>
                <entry offset="0x0" startLine="352" startColumn="13" endLine="352" endColumn="29" document="1"/>
                <entry offset="0x1" startLine="353" startColumn="17" endLine="353" endColumn="29" document="1"/>
                <entry offset="0x8" startLine="354" startColumn="13" endLine="354" endColumn="20" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x9">
                <importsforward declaringType="My.MyProject+MyWebServices" methodName="Equals" parameterNames="o"/>
            </scope>
        </method>
    </methods>
</symbols>, options:=PdbValidationOptions.SkipConversionValidation) ' TODO: https://github.com/dotnet/roslyn/issues/18004
        End Sub

        <WorkItem(876518, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/876518")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub WinFormMain()
            Dim source =
<compilation>
    <file>
&lt;Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()&gt; _
Partial Class Form1
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    &lt;System.Diagnostics.DebuggerNonUserCode()&gt; _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    &lt;System.Diagnostics.DebuggerStepThrough()&gt; _
    Private Sub InitializeComponent()
        components = New System.ComponentModel.Container()
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Text = "Form1"
    End Sub

End Class
    </file>
</compilation>
            Dim defines = PredefinedPreprocessorSymbols.AddPredefinedPreprocessorSymbols(
                OutputKind.WindowsApplication,
                KeyValuePairUtil.Create(Of String, Object)("_MyType", "WindowsForms"),
                KeyValuePairUtil.Create(Of String, Object)("Config", "Debug"),
                KeyValuePairUtil.Create(Of String, Object)("DEBUG", -1),
                KeyValuePairUtil.Create(Of String, Object)("TRACE", -1),
                KeyValuePairUtil.Create(Of String, Object)("PLATFORM", "AnyCPU"))

            Dim parseOptions As VisualBasicParseOptions = New VisualBasicParseOptions(preprocessorSymbols:=defines)
            Dim compOptions As VisualBasicCompilationOptions = New VisualBasicCompilationOptions(
                OutputKind.WindowsApplication,
                optimizationLevel:=OptimizationLevel.Debug,
                parseOptions:=parseOptions,
                mainTypeName:="My.MyApplication")
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {SystemWindowsFormsRef}, compOptions)
            comp.VerifyDiagnostics()

            ' Just care that there's at least one non-hidden sequence point.
            comp.VerifyPdb("My.MyApplication.Main",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <entryPoint declaringType="My.MyApplication" methodName="Main" parameterNames="Args"/>
    <methods>
        <method containingType="My.MyApplication" name="Main" parameterNames="Args">
            <sequencePoints>
                <entry offset="0x0" startLine="78" startColumn="9" endLine="78" endColumn="55" document="1"/>
                <entry offset="0x1" startLine="79" startColumn="13" endLine="79" endColumn="16" document="1"/>
                <entry offset="0x2" startLine="80" startColumn="16" endLine="80" endColumn="133" document="1"/>
                <entry offset="0xf" startLine="81" startColumn="13" endLine="81" endColumn="20" document="1"/>
                <entry offset="0x11" startLine="82" startColumn="13" endLine="82" endColumn="20" document="1"/>
                <entry offset="0x12" startLine="83" startColumn="13" endLine="83" endColumn="37" document="1"/>
                <entry offset="0x1e" startLine="84" startColumn="9" endLine="84" endColumn="16" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x1f">
                <currentnamespace name="My"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub SynthesizedVariableForSelectCastValue()
            Dim source =
<compilation>
    <file>
Imports System
Class C
    Sub F(args As String())
        Select Case args(0)
            Case "a"
                Console.WriteLine(1)
            Case "b"
                Console.WriteLine(2)
            Case "c"
                Console.WriteLine(3)
        End Select
    End Sub
End Class
    </file>
</compilation>

            Dim c = CreateCompilationWithMscorlib40AndVBRuntime(source, options:=TestOptions.DebugDll)
            c.VerifyDiagnostics()
            c.VerifyPdb("C.F",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="C" name="F" parameterNames="args">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="15" offset="0"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="3" startColumn="5" endLine="3" endColumn="28" document="1"/>
                <entry offset="0x1" startLine="4" startColumn="9" endLine="4" endColumn="28" document="1"/>
                <entry offset="0x32" startLine="5" startColumn="13" endLine="5" endColumn="21" document="1"/>
                <entry offset="0x33" startLine="6" startColumn="17" endLine="6" endColumn="37" document="1"/>
                <entry offset="0x3c" startLine="7" startColumn="13" endLine="7" endColumn="21" document="1"/>
                <entry offset="0x3d" startLine="8" startColumn="17" endLine="8" endColumn="37" document="1"/>
                <entry offset="0x46" startLine="9" startColumn="13" endLine="9" endColumn="21" document="1"/>
                <entry offset="0x47" startLine="10" startColumn="17" endLine="10" endColumn="37" document="1"/>
                <entry offset="0x50" startLine="11" startColumn="9" endLine="11" endColumn="19" document="1"/>
                <entry offset="0x51" startLine="12" startColumn="5" endLine="12" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x52">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub Constant_AllTypes()
            Dim source =
<compilation>
    <file>
Imports System
Imports System.Collections.Generic
'Imports Microsoft.VisualBasic.Strings

Class X 
End Class

Public Class C(Of S)
    Enum EnumI1 As SByte    : A : End Enum
    Enum EnumU1 As Byte     : A : End Enum 
    Enum EnumI2 As Short    : A : End Enum 
    Enum EnumU2 As UShort   : A : End Enum
    Enum EnumI4 As Integer  : A : End Enum
    Enum EnumU4 As UInteger : A : End Enum
    Enum EnumI8 As Long     : A : End Enum
    Enum EnumU8 As ULong    : A : End Enum

    Public Sub F(Of T)()
        Const B As Boolean = Nothing
        Const C As Char = Nothing
        Const I1 As SByte = 0
        Const U1 As Byte = 0
        Const I2 As Short = 0
        Const U2 As UShort = 0
        Const I4 As Integer = 0
        Const U4 As UInteger = 0
        Const I8 As Long = 0
        Const U8 As ULong = 0
        Const R4 As Single = 0
        Const R8 As Double = 0

        Const EI1 As C(Of Integer).EnumI1 = 0
        Const EU1 As C(Of Integer).EnumU1 = 0
        Const EI2 As C(Of Integer).EnumI2 = 0
        Const EU2 As C(Of Integer).EnumU2 = 0
        Const EI4 As C(Of Integer).EnumI4 = 0
        Const EU4 As C(Of Integer).EnumU4 = 0
        Const EI8 As C(Of Integer).EnumI8 = 0
        Const EU8 As C(Of Integer).EnumU8 = 0

        'Const StrWithNul As String = ChrW(0)
        Const EmptyStr As String = ""
        Const NullStr As String = Nothing
        Const NullObject As Object = Nothing
       
        Const D As Decimal = Nothing
        Const DT As DateTime = #1-1-2015#
    End Sub
End Class
    </file>
</compilation>

            Dim c = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {SystemCoreRef}, options:=TestOptions.DebugDll.WithEmbedVbCoreRuntime(True))

            c.VerifyPdb("C`1.F",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="C`1" name="F">
            <sequencePoints>
                <entry offset="0x0" startLine="18" startColumn="5" endLine="18" endColumn="25" document="1"/>
                <entry offset="0x1" startLine="48" startColumn="5" endLine="48" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2">
                <namespace name="System" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <currentnamespace name=""/>
                <constant name="B" value="0" type="Boolean"/>
                <constant name="C" value="0" type="Char"/>
                <constant name="I1" value="0" type="SByte"/>
                <constant name="U1" value="0" type="Byte"/>
                <constant name="I2" value="0" type="Int16"/>
                <constant name="U2" value="0" type="UInt16"/>
                <constant name="I4" value="0" type="Int32"/>
                <constant name="U4" value="0" type="UInt32"/>
                <constant name="I8" value="0" type="Int64"/>
                <constant name="U8" value="0" type="UInt64"/>
                <constant name="R4" value="0x00000000" type="Single"/>
                <constant name="R8" value="0x0000000000000000" type="Double"/>
                <constant name="EI1" value="0" signature="EnumI1{Int32}"/>
                <constant name="EU1" value="0" signature="EnumU1{Int32}"/>
                <constant name="EI2" value="0" signature="EnumI2{Int32}"/>
                <constant name="EU2" value="0" signature="EnumU2{Int32}"/>
                <constant name="EI4" value="0" signature="EnumI4{Int32}"/>
                <constant name="EU4" value="0" signature="EnumU4{Int32}"/>
                <constant name="EI8" value="0" signature="EnumI8{Int32}"/>
                <constant name="EU8" value="0" signature="EnumU8{Int32}"/>
                <constant name="EmptyStr" value="" type="String"/>
                <constant name="NullStr" value="null" type="String"/>
                <constant name="NullObject" value="null" type="Object"/>
                <constant name="D" value="0" type="Decimal"/>
                <constant name="DT" value="01/01/2015 00:00:00" type="DateTime"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub ImportsInAsync()
            Dim source =
"Imports System.Linq
Imports System.Threading.Tasks
Class C
    Shared Async Function F() As Task
        Dim c = {1, 2, 3}
        c.Select(Function(i) i)
    End Function
End Class"
            Dim c = CreateCompilationWithMscorlib45AndVBRuntime({Parse(source)}, options:=TestOptions.DebugDll, references:={SystemCoreRef})

            ' Note: since the method is first, it is recording the imports (rather than using an importsforward)
            c.VerifyPdb("C+VB$StateMachine_1_F.MoveNext",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="C+VB$StateMachine_1_F" name="MoveNext">
            <customDebugInfo>
                <hoistedLocalScopes format="portable">
                    <slot startOffset="0x0" endOffset="0x8b"/>
                </hoistedLocalScopes>
                <encLocalSlotMap>
                    <slot kind="27" offset="-1"/>
                    <slot kind="temp"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="1"/>
                <entry offset="0x7" startLine="4" startColumn="5" endLine="4" endColumn="38" document="1"/>
                <entry offset="0x8" startLine="5" startColumn="13" endLine="5" endColumn="26" document="1"/>
                <entry offset="0x1f" startLine="6" startColumn="9" endLine="6" endColumn="32" document="1"/>
                <entry offset="0x4f" startLine="7" startColumn="5" endLine="7" endColumn="17" document="1"/>
                <entry offset="0x51" hidden="true" document="1"/>
                <entry offset="0x58" hidden="true" document="1"/>
                <entry offset="0x74" startLine="7" startColumn="5" endLine="7" endColumn="17" document="1"/>
                <entry offset="0x7e" hidden="true" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x8b">
                <namespace name="System.Linq" importlevel="file"/>
                <namespace name="System.Threading.Tasks" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="$VB$ResumableLocal_c$0" il_index="0" il_start="0x0" il_end="0x8b" attributes="0"/>
            </scope>
            <asyncInfo>
                <kickoffMethod declaringType="C" methodName="F"/>
            </asyncInfo>
        </method>
    </methods>
</symbols>)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub ImportsInAsyncLambda()
            Dim source =
"Imports System.Linq
Class C
    Shared Sub M()
        Dim f As System.Action =
            Async Sub()
                Dim c = {1, 2, 3}
                c.Select(Function(i) i)
            End Sub
    End Sub
End Class"
            Dim c = CreateCompilationWithMscorlib45AndVBRuntime({Parse(source)}, options:=TestOptions.DebugDll, references:={SystemCoreRef})
            c.VerifyPdb("C+_Closure$__+VB$StateMachine___Lambda$__1-0.MoveNext",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="C+_Closure$__+VB$StateMachine___Lambda$__1-0" name="MoveNext">
            <customDebugInfo>
                <hoistedLocalScopes format="portable">
                    <slot startOffset="0x0" endOffset="0x8b"/>
                </hoistedLocalScopes>
                <encLocalSlotMap>
                    <slot kind="27" offset="38"/>
                    <slot kind="temp"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="1"/>
                <entry offset="0x7" startLine="5" startColumn="13" endLine="5" endColumn="24" document="1"/>
                <entry offset="0x8" startLine="6" startColumn="21" endLine="6" endColumn="34" document="1"/>
                <entry offset="0x1f" startLine="7" startColumn="17" endLine="7" endColumn="40" document="1"/>
                <entry offset="0x4f" startLine="8" startColumn="13" endLine="8" endColumn="20" document="1"/>
                <entry offset="0x51" hidden="true" document="1"/>
                <entry offset="0x58" hidden="true" document="1"/>
                <entry offset="0x74" hidden="true" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x8b">
                <importsforward declaringType="C" methodName="M"/>
                <local name="$VB$ResumableLocal_c$0" il_index="0" il_start="0x0" il_end="0x8b" attributes="0"/>
            </scope>
            <asyncInfo>
                <catchHandler offset="0x51"/>
                <kickoffMethod declaringType="C+_Closure$__" methodName="_Lambda$__1-0"/>
            </asyncInfo>
        </method>
    </methods>
</symbols>)
        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        <WorkItem(23525, "https://github.com/dotnet/roslyn/issues/23525")>
        Public Sub InvalidCharacterInPdbPath()
            Using outStream = Temp.CreateFile().Open()
                Dim Compilation = CreateEmptyCompilation("")
                Dim result = Compilation.Emit(outStream, options:=New EmitOptions(pdbFilePath:="test\\?.pdb", debugInformationFormat:=DebugInformationFormat.Embedded))

                ' This is fine because EmitOptions just controls what is written into the PE file and it's 
                ' valid for this to be an illegal file name (path map can easily create these).
                Assert.True(result.Success)
            End Using
        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        <WorkItem(38954, "https://github.com/dotnet/roslyn/issues/38954")>
        Public Sub FilesOneWithNoMethodBody()
            Dim source1 =
"Imports System

Class C
    Public Shared Sub Main()
        Console.WriteLine()
    End Sub
End Class
"
            Dim source2 =
"
' no code
"

            Dim tree1 = Parse(source1, "f:/build/goo.vb")
            Dim tree2 = Parse(source2, "f:/build/nocode.vb")
            Dim c = CreateCompilation({tree1, tree2}, options:=TestOptions.DebugDll)

            c.VerifyPdb("
<symbols>
  <files>
    <file id=""1"" name=""f:/build/goo.vb"" language=""VB"" checksumAlgorithm=""SHA1"" checksum=""48-27-3C-50-9D-24-D4-0D-51-87-6C-E2-FB-2F-AA-1C-80-96-0B-B7"" />
    <file id=""2"" name=""f:/build/nocode.vb"" language=""VB"" checksumAlgorithm=""SHA1"" checksum=""40-43-2C-44-BA-1C-C7-1A-B3-F3-68-E5-96-7C-65-9D-61-85-D5-44"" />
  </files>
  <methods>
    <method containingType=""C"" name=""Main"">
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""5"" endLine=""4"" endColumn=""29"" document=""1"" />
        <entry offset=""0x1"" startLine=""5"" startColumn=""9"" endLine=""5"" endColumn=""28"" document=""1"" />
        <entry offset=""0x7"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""12"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x8"">
        <namespace name=""System"" importlevel=""file"" />
        <currentnamespace name="""" />
      </scope>
    </method>
  </methods>
</symbols>
")
        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        <WorkItem(38954, "https://github.com/dotnet/roslyn/issues/38954")>
        Public Sub SingleFileWithNoMethodBody()
            Dim source =
"
' no code
"

            Dim tree = Parse(source, "f:/build/nocode.vb")
            Dim c = CreateCompilation({tree}, options:=TestOptions.DebugDll)

            c.VerifyPdb("
<symbols>
  <files>
    <file id=""1"" name=""f:/build/nocode.vb"" language=""VB"" checksumAlgorithm=""SHA1"" checksum=""40-43-2C-44-BA-1C-C7-1A-B3-F3-68-E5-96-7C-65-9D-61-85-D5-44"" />
  </files>
  <methods />
</symbols>
")
        End Sub

        <Fact>
        Public Sub CompilerInfo_WindowsPdb()
            Dim compilerAssembly = GetType(Compilation).Assembly
            Dim fileVersion = Version.Parse(compilerAssembly.GetCustomAttribute(Of AssemblyFileVersionAttribute)().Version).ToString()
            Dim versionString = compilerAssembly.GetCustomAttribute(Of AssemblyInformationalVersionAttribute)().InformationalVersion

            Dim source = "
Class C 
    Sub F
    End Sub
End CLass"

            Dim c = CreateCompilation({Parse(source, "a.cs")}, options:=TestOptions.DebugDll)

            c.VerifyPdb("
<symbols>
  <files>
    <file id=""1"" name=""a.cs"" language=""VB"" checksumAlgorithm=""SHA1"" checksum=""D1-16-CD-EB-E1-D0-E0-7B-86-B4-47-40-75-8E-0D-53-E7-3B-10-0D"" />
  </files>
  <methods>
    <method containingType=""C"" name=""F"">
      <sequencePoints>
        <entry offset=""0x0"" startLine=""3"" startColumn=""5"" endLine=""3"" endColumn=""10"" document=""1"" />
        <entry offset=""0x1"" startLine=""4"" startColumn=""5"" endLine=""4"" endColumn=""12"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <currentnamespace name="""" />
      </scope>
    </method>
  </methods>
  <compilerInfo version=""" & fileVersion & """ name=""Visual Basic - " & versionString & """ />
</symbols>
", options:=PdbValidationOptions.IncludeModuleDebugInfo, format:=DebugInformationFormat.Pdb)
        End Sub
    End Class
End Namespace
