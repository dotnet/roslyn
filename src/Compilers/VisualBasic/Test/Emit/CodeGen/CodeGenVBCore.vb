' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class CodeGenVBCore
        Inherits BasicTestBase

        ' The Embedded attribute should only be available
        ' if other embedded code is included.
        <Fact()>
        Public Sub EmbeddedAttributeRequiresOtherEmbeddedCode()
            Dim sources = <compilation>
                              <file name="c.vb"><![CDATA[
Option Strict On
<Microsoft.VisualBasic.Embedded()>
Class C
End Class
    ]]></file>
                          </compilation>

            ' With InternalXmlHelper.
            Dim compilation = CreateCompilationWithMscorlib40AndReferences(sources,
                references:=NoVbRuntimeReferences.Concat(XmlReferences),
                options:=TestOptions.ReleaseDll.WithEmbedVbCoreRuntime(True))
            compilation.AssertNoErrors()

            ' With VBCore.
            compilation = CreateCompilationWithMscorlib40AndReferences(sources,
                references:=NoVbRuntimeReferences,
                options:=TestOptions.ReleaseDll.WithEmbedVbCoreRuntime(True))
            compilation.AssertNoErrors()

            ' No embedded code.
            compilation = CreateCompilationWithMscorlib40AndReferences(sources,
                references:=NoVbRuntimeReferences,
                options:=TestOptions.ReleaseDll)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC30002: Type 'Microsoft.VisualBasic.Embedded' is not defined.
<Microsoft.VisualBasic.Embedded()>
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></errors>)
        End Sub

        ' The Embedded attribute should only be available for 
        ' user-define code if vb runtime is included.
        <WorkItem(546059, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546059")>
        <Fact()>
        Public Sub EmbeddedAttributeRequiresOtherEmbeddedCode2()
            Dim sources = <compilation>
                              <file name="c.vb"><![CDATA[
Option Strict On
<Microsoft.VisualBasic.Embedded()>
Class C
End Class
    ]]></file>
                          </compilation>

            ' No embedded code.
            Dim compilation = CreateCompilationWithMscorlib40AndReferences(sources,
                references:=NoVbRuntimeReferences.Concat({MsvbRef, SystemXmlRef, SystemXmlLinqRef}),
                options:=TestOptions.ReleaseDll)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC30002: Type 'Microsoft.VisualBasic.Embedded' is not defined.
<Microsoft.VisualBasic.Embedded()>
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></errors>)
        End Sub

        ' The Embedded attribute should only be available for 
        ' user-define code if vb runtime is included.
        <WorkItem(546059, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546059")>
        <Fact()>
        Public Sub EmbeddedAttributeRequiresOtherEmbeddedCode3()
            Dim sources = <compilation>
                              <file name="c.vb"><![CDATA[
Option Strict On
Class C
    Public x As Microsoft.VisualBasic.Embedded
End Class
    ]]></file>
                          </compilation>

            ' No embedded code.
            Dim compilation = CreateCompilationWithMscorlib40AndReferences(sources,
                references:=NoVbRuntimeReferences.Concat({MsvbRef, SystemXmlRef, SystemXmlLinqRef}),
                options:=TestOptions.ReleaseDll)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC30002: Type 'Microsoft.VisualBasic.Embedded' is not defined.
    Public x As Microsoft.VisualBasic.Embedded
                ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub InternalXmlHelper_NoReferences()
            Dim compilationVerifier = MyBase.CompileAndVerify(source:=
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Class C
End Class
    ]]></file>
</compilation>,
            allReferences:=NoVbRuntimeReferences,
            sourceSymbolValidator:=Sub([module]) ValidateSourceSymbols([module]),
            symbolValidator:=Sub([module])
                                 ValidateSymbols([module],
            <expected>
Namespace Global
  Class C
    >  Sub C..ctor()
  End Class
End Namespace
</expected>.Value)
                             End Sub,
                options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal))
            compilationVerifier.Compilation.AssertNoErrors()
        End Sub

        <Fact()>
        Public Sub InternalXmlHelper_NoSymbols()
            Dim compilationVerifier = MyBase.CompileAndVerify(source:=
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Class C
End Class
    ]]></file>
</compilation>,
            allReferences:=NoVbRuntimeReferences.Concat(XmlReferences),
            sourceSymbolValidator:=Sub([module]) ValidateSourceSymbols([module]),
            symbolValidator:=Sub([module])
                                 ValidateSymbols([module],
            <expected>
Namespace Global
  Class C
    >  Sub C..ctor()
  End Class
End Namespace
</expected>.Value)
                             End Sub,
                options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal))
            compilationVerifier.Compilation.AssertNoErrors()
        End Sub

        <Fact()>
        Public Sub InternalXmlHelper_CreateNamespaceAttribute_NoDebug()
            Dim symbols = <expected>
  Namespace Global
    Class C
      >  C.F As System.Object
      >  Sub C..ctor()
    End Class
    Namespace Microsoft
      Namespace Microsoft.VisualBasic
        [Microsoft.VisualBasic.Embedded]
        [System.AttributeUsageAttribute]
        [System.ComponentModel.EditorBrowsableAttribute]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        Class Microsoft.VisualBasic.Embedded
          >  Sub Microsoft.VisualBasic.Embedded..ctor()
        End Class
      End Namespace
    End Namespace
    Namespace My
      [Microsoft.VisualBasic.Embedded]
      [System.Diagnostics.DebuggerNonUserCodeAttribute]
      [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
      [System.ComponentModel.EditorBrowsableAttribute]
      Class My.InternalXmlHelper
           [System.ComponentModel.EditorBrowsableAttribute]
        >  Function My.InternalXmlHelper.CreateNamespaceAttribute(name As System.Xml.Linq.XName, ns As System.Xml.Linq.XNamespace) As System.Xml.Linq.XAttribute
      End Class
    End Namespace
  End Namespace
</expected>.Value
            Dim compilationVerifier = MyBase.CompileAndVerify(source:=
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports <xmlns:p="http://roslyn/">
Class C
    Public Shared F As Object = <p:x/>
End Class
    ]]></file>
</compilation>,
            allReferences:=NoVbRuntimeReferences.Concat(XmlReferences),
            sourceSymbolValidator:=Sub([module]) ValidateSourceSymbols([module]),
            symbolValidator:=Sub([module]) ValidateSymbols([module], symbols),
            options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal))
            compilationVerifier.Compilation.AssertNoErrors()
        End Sub

        <WorkItem(545438, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545438"), WorkItem(546887, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546887")>
        <Fact()>
        Public Sub InternalXmlHelper_ValueProperty()
            Dim symbols = <expected>
  Namespace Global
    Class C
      >  Sub C..ctor()
      >  Sub C.M(x As System.Xml.Linq.XElement)
    End Class
    Namespace Microsoft
      Namespace Microsoft.VisualBasic
        [Microsoft.VisualBasic.Embedded]
        [System.AttributeUsageAttribute]
        [System.ComponentModel.EditorBrowsableAttribute]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        Class Microsoft.VisualBasic.Embedded
          >  Sub Microsoft.VisualBasic.Embedded..ctor()
        End Class
      End Namespace
    End Namespace
    Namespace My
      [Microsoft.VisualBasic.Embedded]
      [System.Diagnostics.DebuggerNonUserCodeAttribute]
      [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
      [System.ComponentModel.EditorBrowsableAttribute]
      Class My.InternalXmlHelper
        >  Function My.InternalXmlHelper.get_AttributeValue(source As System.Xml.Linq.XElement, name As System.Xml.Linq.XName) As System.String
        >  Function My.InternalXmlHelper.get_Value(source As System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)) As System.String
        >  Property My.InternalXmlHelper.AttributeValue(source As System.Xml.Linq.XElement, name As System.Xml.Linq.XName) As System.String
        >  Property My.InternalXmlHelper.Value(source As System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)) As System.String
        >  Sub My.InternalXmlHelper.set_AttributeValue(source As System.Xml.Linq.XElement, name As System.Xml.Linq.XName, value As System.String)
        >  Sub My.InternalXmlHelper.set_Value(source As System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement), value As System.String)
      End Class
    End Namespace
  End Namespace
</expected>.Value
            Dim compilationVerifier = MyBase.CompileAndVerify(source:=
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Class C
    Shared Sub M(x As System.Xml.Linq.XElement)
        x.@a = x.<y>.Value
    End Sub
End Class
    ]]></file>
</compilation>,
            allReferences:=NoVbRuntimeReferences.Concat(XmlReferences),
            symbolValidator:=Sub([module]) ValidateSymbols([module], symbols),
            sourceSymbolValidator:=Sub([module]) ValidateSourceSymbols([module]),
            options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal))
            compilationVerifier.Compilation.AssertNoErrors()
        End Sub

        <Fact()>
        Public Sub InternalXmlHelper_Locations()
            Dim compilation = CreateCompilationWithMscorlib40AndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Class C
    Public Shared F As Object = <x xmlns:p="http://roslyn"/>
End Class
    ]]></file>
</compilation>,
                references:=NoVbRuntimeReferences.Concat(XmlReferences),
                options:=TestOptions.ReleaseDll.WithEmbedVbCoreRuntime(True))
            compilation.AssertNoErrors()
            Dim globalNamespace = compilation.SourceModule.GlobalNamespace
            Assert.Equal(globalNamespace.Locations.Length, 4)
            Dim [namespace] = globalNamespace.GetMember(Of NamespaceSymbol)("My")
            Assert.Equal([namespace].Locations.Length, 1)
            Dim type = [namespace].GetMember(Of NamedTypeSymbol)("InternalXmlHelper")
            Assert.Equal(type.Locations.Length, 1)
        End Sub

        <Fact()>
        Public Sub VbCore_NoSymbols()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq
Imports Microsoft.VisualBasic

Class Program
    Shared Sub Main(args As String())
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:="",
sourceSymbolValidator:=Sub([module]) ValidateSourceSymbols([module]),
symbolValidator:=Sub([module])
                     ValidateSymbols([module],
<expected>
    Namespace Global
      Class Program
        >  Sub Program..ctor()
           [System.STAThreadAttribute]
        >  Sub Program.Main(args As System.String())
      End Class
    End Namespace
</expected>.Value)
                 End Sub)
        End Sub

        <Fact()>
        Public Sub VbCore_SingleSymbol_SymbolInGetType()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq
Imports System.Collections.Generic
Imports Microsoft.VisualBasic
Imports Microsoft.VisualBasic.CompilerServices

Class Program
    Shared Sub Main(args As String())
        Console.Write(GetType(List(Of List(Of StandardModuleAttribute))).ToString())
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:="System.Collections.Generic.List`1[System.Collections.Generic.List`1[Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute]]",
sourceSymbolValidator:=Sub([module]) ValidateSourceSymbols([module]),
symbolValidator:=Sub([module])
                     ValidateSymbols([module],
<expected>
  Namespace Global
    Class Program
      >  Sub Program..ctor()
         [System.STAThreadAttribute]
      >  Sub Program.Main(args As System.String())
    End Class
    Namespace Microsoft
      Namespace Microsoft.VisualBasic
        [Microsoft.VisualBasic.Embedded]
        [System.AttributeUsageAttribute]
        [System.ComponentModel.EditorBrowsableAttribute]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        Class Microsoft.VisualBasic.Embedded
          >  Sub Microsoft.VisualBasic.Embedded..ctor()
        End Class
        Namespace Microsoft.VisualBasic.CompilerServices
          [Microsoft.VisualBasic.Embedded]
          [System.AttributeUsageAttribute]
          [System.ComponentModel.EditorBrowsableAttribute]
          [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
          Class Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute
            >  Sub Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute..ctor()
          End Class
        End Namespace
      End Namespace
    End Namespace
  End Namespace
</expected>.Value)
                 End Sub)
        End Sub

        <Fact()>
        Public Sub VbCore_SingleSymbol_Constants()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq
Imports Microsoft.VisualBasic

Class Program
    Shared Sub Main(args As String())
        Console.Write(GetType(Constants).ToString())
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:="Microsoft.VisualBasic.Constants",
sourceSymbolValidator:=Sub([module]) ValidateSourceSymbols([module]),
symbolValidator:=Sub([module])
                     ValidateSymbols([module],
<expected>
  Namespace Global
    Class Program
      >  Sub Program..ctor()
         [System.STAThreadAttribute]
      >  Sub Program.Main(args As System.String())
    End Class
    Namespace Microsoft
      Namespace Microsoft.VisualBasic
        [Microsoft.VisualBasic.Embedded]
        [System.Diagnostics.DebuggerNonUserCodeAttribute]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        Module Microsoft.VisualBasic.Constants
        End Module
        [Microsoft.VisualBasic.Embedded]
        [System.AttributeUsageAttribute]
        [System.ComponentModel.EditorBrowsableAttribute]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        Class Microsoft.VisualBasic.Embedded
          >  Sub Microsoft.VisualBasic.Embedded..ctor()
        End Class
        Namespace Microsoft.VisualBasic.CompilerServices
          [Microsoft.VisualBasic.Embedded]
          [System.AttributeUsageAttribute]
          [System.ComponentModel.EditorBrowsableAttribute]
          [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
          Class Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute
            >  Sub Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute..ctor()
          End Class
        End Namespace
      End Namespace
    End Namespace
  End Namespace
</expected>.Value)
                 End Sub)
        End Sub

        <Fact()>
        Public Sub VbCore_SingleSymbol_Constants_WithVbRuntime()
            MyBase.CompileAndVerify(source:=
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq
Imports Microsoft.VisualBasic

Class Program
    Shared Sub Main(args As String())
        Console.Write(GetType(Constants).ToString())
    End Sub
End Class
    </file>
</compilation>,
            allReferences:=NoVbRuntimeReferences.Concat(MsvbRef),
            expectedOutput:="Microsoft.VisualBasic.Constants",
            sourceSymbolValidator:=Sub([module]) ValidateSourceSymbols([module]),
            symbolValidator:=Sub([module])
                                 ValidateSymbols([module],
            <expected>
  Namespace Global
    Class Program
      >  Sub Program..ctor()
         [System.STAThreadAttribute]
      >  Sub Program.Main(args As System.String())
    End Class
    Namespace Microsoft
      Namespace Microsoft.VisualBasic
        [Microsoft.VisualBasic.Embedded]
        [System.Diagnostics.DebuggerNonUserCodeAttribute]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        Module Microsoft.VisualBasic.Constants
        End Module
        [Microsoft.VisualBasic.Embedded]
        [System.AttributeUsageAttribute]
        [System.ComponentModel.EditorBrowsableAttribute]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        Class Microsoft.VisualBasic.Embedded
          >  Sub Microsoft.VisualBasic.Embedded..ctor()
        End Class
        Namespace Microsoft.VisualBasic.CompilerServices
          [Microsoft.VisualBasic.Embedded]
          [System.AttributeUsageAttribute]
          [System.ComponentModel.EditorBrowsableAttribute]
          [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
          Class Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute
            >  Sub Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute..ctor()
          End Class
        End Namespace
      End Namespace
    End Namespace
  End Namespace
</expected>.Value)
                             End Sub,
                options:=TestOptions.DebugExe.WithEmbedVbCoreRuntime(True).WithMetadataImportOptions(MetadataImportOptions.Internal))
        End Sub

        <Fact()>
        Public Sub VbCore_SingleSymbol_Constants_All()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq
Imports Microsoft.VisualBasic

Class Program
    Shared Sub Main(args As String())
        Dim s = String.Format("|{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}|{8}|{9}|",
                              vbCrLf, vbNewLine, vbCr, vbLf, vbBack, vbFormFeed, 
                              vbTab, vbVerticalTab, vbNullChar, vbNullString)
        s = s.Replace(vbCr, "vbCr")
        s = s.Replace(vbLf, "vbLf")
        s = s.Replace(vbBack, "vbBack")
        s = s.Replace(vbFormFeed, "vbFormFeed")
        s = s.Replace(vbTab, "vbTab")
        s = s.Replace(vbVerticalTab, "vbVerticalTab")
        s = s.Replace(vbNullChar, "vbNullChar")
        Console.Write(s)
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:="|vbCrvbLf|vbCrvbLf|vbCr|vbLf|vbBack|vbFormFeed|vbTab|vbVerticalTab|vbNullChar||",
sourceSymbolValidator:=Sub([module]) ValidateSourceSymbols([module]),
symbolValidator:=Sub([module])
                     ValidateSymbols([module],
<expected>
    Namespace Global
      Class Program
        >  Sub Program..ctor()
           [System.STAThreadAttribute]
        >  Sub Program.Main(args As System.String())
      End Class
    End Namespace
</expected>.Value)
                 End Sub)
        End Sub

        <Fact()>
        Public Sub VbCore_SingleSymbol_CompilerServices_EmbeddedOperators()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq
Imports Microsoft.VisualBasic

Class Program
    Shared Sub Main(args As String())
        Console.Write(GetType(CompilerServices.EmbeddedOperators).ToString())
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:="Microsoft.VisualBasic.CompilerServices.EmbeddedOperators",
sourceSymbolValidator:=Sub([module]) ValidateSourceSymbols([module]),
symbolValidator:=Sub([module])
                     ValidateSymbols([module],
<expected>
  Namespace Global
    Class Program
      >  Sub Program..ctor()
         [System.STAThreadAttribute]
      >  Sub Program.Main(args As System.String())
    End Class
    Namespace Microsoft
      Namespace Microsoft.VisualBasic
        [Microsoft.VisualBasic.Embedded]
        [System.AttributeUsageAttribute]
        [System.ComponentModel.EditorBrowsableAttribute]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        Class Microsoft.VisualBasic.Embedded
          >  Sub Microsoft.VisualBasic.Embedded..ctor()
        End Class
        Namespace Microsoft.VisualBasic.CompilerServices
          [Microsoft.VisualBasic.Embedded]
          [System.Diagnostics.DebuggerNonUserCodeAttribute]
          [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
          [System.ComponentModel.EditorBrowsableAttribute]
          Class Microsoft.VisualBasic.CompilerServices.EmbeddedOperators
          End Class
        End Namespace
      End Namespace
    End Namespace
  End Namespace
</expected>.Value)
                 End Sub)
        End Sub

        <Fact()>
        Public Sub VbCore_SingleSymbol_CompilerServices_EmbeddedOperators_CompareString()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq
Imports Microsoft.VisualBasic

Class Program
    Shared Sub Main(args As String())
        Console.Write(CompilerServices.EmbeddedOperators.CompareString("a", "A", True))
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:="0",
sourceSymbolValidator:=Sub([module]) ValidateSourceSymbols([module]),
symbolValidator:=Sub([module])
                     ValidateSymbols([module],
         <expected>
  Namespace Global
    Class Program
      >  Sub Program..ctor()
         [System.STAThreadAttribute]
      >  Sub Program.Main(args As System.String())
    End Class
    Namespace Microsoft
      Namespace Microsoft.VisualBasic
        [Microsoft.VisualBasic.Embedded]
        [System.AttributeUsageAttribute]
        [System.ComponentModel.EditorBrowsableAttribute]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        Class Microsoft.VisualBasic.Embedded
          >  Sub Microsoft.VisualBasic.Embedded..ctor()
        End Class
        Namespace Microsoft.VisualBasic.CompilerServices
          [Microsoft.VisualBasic.Embedded]
          [System.Diagnostics.DebuggerNonUserCodeAttribute]
          [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
          [System.ComponentModel.EditorBrowsableAttribute]
          Class Microsoft.VisualBasic.CompilerServices.Conversions
            >  Function Microsoft.VisualBasic.CompilerServices.Conversions.GetCultureInfo() As System.Globalization.CultureInfo
          End Class
          [Microsoft.VisualBasic.Embedded]
          [System.Diagnostics.DebuggerNonUserCodeAttribute]
          [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
          [System.ComponentModel.EditorBrowsableAttribute]
          Class Microsoft.VisualBasic.CompilerServices.EmbeddedOperators
            >  Function Microsoft.VisualBasic.CompilerServices.EmbeddedOperators.CompareString(Left As System.String, Right As System.String, TextCompare As System.Boolean) As System.Int32
          End Class
        End Namespace
      End Namespace
    End Namespace
  End Namespace
</expected>.Value)
                 End Sub)
        End Sub

        <Fact()>
        Public Sub VbCore_SingleSymbol_CompilerServices_EmbeddedOperators_CompareString2()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq
Imports Microsoft.VisualBasic

Class Program
    Shared Sub Main(args As String())
        Dim x As String = "Goo"
        Console.WriteLine(If(x = "Goo", "y", x))
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:="y",
sourceSymbolValidator:=Sub([module]) ValidateSourceSymbols([module]),
symbolValidator:=Sub([module])
                     ValidateSymbols([module],
<expected>
  Namespace Global
    Class Program
      >  Sub Program..ctor()
         [System.STAThreadAttribute]
      >  Sub Program.Main(args As System.String())
    End Class
    Namespace Microsoft
      Namespace Microsoft.VisualBasic
        [Microsoft.VisualBasic.Embedded]
        [System.AttributeUsageAttribute]
        [System.ComponentModel.EditorBrowsableAttribute]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        Class Microsoft.VisualBasic.Embedded
          >  Sub Microsoft.VisualBasic.Embedded..ctor()
        End Class
        Namespace Microsoft.VisualBasic.CompilerServices
          [Microsoft.VisualBasic.Embedded]
          [System.Diagnostics.DebuggerNonUserCodeAttribute]
          [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
          [System.ComponentModel.EditorBrowsableAttribute]
          Class Microsoft.VisualBasic.CompilerServices.Conversions
            >  Function Microsoft.VisualBasic.CompilerServices.Conversions.GetCultureInfo() As System.Globalization.CultureInfo
          End Class
          [Microsoft.VisualBasic.Embedded]
          [System.Diagnostics.DebuggerNonUserCodeAttribute]
          [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
          [System.ComponentModel.EditorBrowsableAttribute]
          Class Microsoft.VisualBasic.CompilerServices.EmbeddedOperators
            >  Function Microsoft.VisualBasic.CompilerServices.EmbeddedOperators.CompareString(Left As System.String, Right As System.String, TextCompare As System.Boolean) As System.Int32
          End Class
        End Namespace
      End Namespace
    End Namespace
  End Namespace
</expected>.Value)
                 End Sub)
        End Sub

        <Fact()>
        Public Sub VbCore_SingleSymbol_CompilerServices_Conversions()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq
Imports Microsoft.VisualBasic

Class Program
    Shared Sub Main(args As String())
        Console.Write(GetType(CompilerServices.Conversions).ToString())
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:="Microsoft.VisualBasic.CompilerServices.Conversions",
sourceSymbolValidator:=Sub([module]) ValidateSourceSymbols([module]),
symbolValidator:=Sub([module])
                     ValidateSymbols([module],
<expected>
  Namespace Global
    Class Program
      >  Sub Program..ctor()
         [System.STAThreadAttribute]
      >  Sub Program.Main(args As System.String())
    End Class
    Namespace Microsoft
      Namespace Microsoft.VisualBasic
        [Microsoft.VisualBasic.Embedded]
        [System.AttributeUsageAttribute]
        [System.ComponentModel.EditorBrowsableAttribute]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        Class Microsoft.VisualBasic.Embedded
          >  Sub Microsoft.VisualBasic.Embedded..ctor()
        End Class
        Namespace Microsoft.VisualBasic.CompilerServices
          [Microsoft.VisualBasic.Embedded]
          [System.Diagnostics.DebuggerNonUserCodeAttribute]
          [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
          [System.ComponentModel.EditorBrowsableAttribute]
          Class Microsoft.VisualBasic.CompilerServices.Conversions
          End Class
        End Namespace
      End Namespace
    End Namespace
  End Namespace
</expected>.Value)
                 End Sub)
        End Sub

        <Fact()>
        Public Sub VbCore_SingleSymbol_CompilerServices_Conversions_ToBoolean_String()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq
Imports Microsoft.VisualBasic

Class Program
    Shared Sub Main(args As String())
        Console.Write(CompilerServices.Conversions.ToBoolean("True").ToString())
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:="True",
sourceSymbolValidator:=Sub([module]) ValidateSourceSymbols([module]),
symbolValidator:=Sub([module])
                     ValidateSymbols([module],
         <expected>
  Namespace Global
    Class Program
      >  Sub Program..ctor()
         [System.STAThreadAttribute]
      >  Sub Program.Main(args As System.String())
    End Class
    Namespace Microsoft
      Namespace Microsoft.VisualBasic
        [Microsoft.VisualBasic.Embedded]
        [System.AttributeUsageAttribute]
        [System.ComponentModel.EditorBrowsableAttribute]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        Class Microsoft.VisualBasic.Embedded
          >  Sub Microsoft.VisualBasic.Embedded..ctor()
        End Class
        Namespace Microsoft.VisualBasic.CompilerServices
          [Microsoft.VisualBasic.Embedded]
          [System.Diagnostics.DebuggerNonUserCodeAttribute]
          [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
          [System.ComponentModel.EditorBrowsableAttribute]
          Class Microsoft.VisualBasic.CompilerServices.Conversions
            >  Function Microsoft.VisualBasic.CompilerServices.Conversions.GetCultureInfo() As System.Globalization.CultureInfo
            >  Function Microsoft.VisualBasic.CompilerServices.Conversions.IsHexOrOctValue(Value As System.String, ByRef i64Value As System.Int64) As System.Boolean
            >  Function Microsoft.VisualBasic.CompilerServices.Conversions.ToBoolean(Value As System.String) As System.Boolean
            >  Function Microsoft.VisualBasic.CompilerServices.Conversions.ToHalfwidthNumbers(s As System.String, culture As System.Globalization.CultureInfo) As System.String
          End Class
          [Microsoft.VisualBasic.Embedded]
          [System.Diagnostics.DebuggerNonUserCodeAttribute]
          [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
          [System.ComponentModel.EditorBrowsableAttribute]
          Class Microsoft.VisualBasic.CompilerServices.ProjectData
            >  Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()
            >  Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(ex As System.Exception)
          End Class
        End Namespace
      End Namespace
    End Namespace
  End Namespace
</expected>.Value)
                 End Sub)
        End Sub

        <Fact()>
        Public Sub VbCore_SingleSymbol_CompilerServices_Conversions_ToBoolean_Object()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq
Imports Microsoft.VisualBasic

Class Program
    Shared Sub Main(args As String())
        Console.Write(CompilerServices.Conversions.ToBoolean(directcast("True".ToString(), Object)).ToString())
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:="True",
sourceSymbolValidator:=Sub([module]) ValidateSourceSymbols([module]),
symbolValidator:=Sub([module])
                     ValidateSymbols([module],
         <expected>
  Namespace Global
    Class Program
      >  Sub Program..ctor()
         [System.STAThreadAttribute]
      >  Sub Program.Main(args As System.String())
    End Class
    Namespace Microsoft
      Namespace Microsoft.VisualBasic
        [Microsoft.VisualBasic.Embedded]
        [System.AttributeUsageAttribute]
        [System.ComponentModel.EditorBrowsableAttribute]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        Class Microsoft.VisualBasic.Embedded
          >  Sub Microsoft.VisualBasic.Embedded..ctor()
        End Class
        Namespace Microsoft.VisualBasic.CompilerServices
          [Microsoft.VisualBasic.Embedded]
          [System.Diagnostics.DebuggerNonUserCodeAttribute]
          [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
          [System.ComponentModel.EditorBrowsableAttribute]
          Class Microsoft.VisualBasic.CompilerServices.Conversions
            >  Function Microsoft.VisualBasic.CompilerServices.Conversions.GetCultureInfo() As System.Globalization.CultureInfo
            >  Function Microsoft.VisualBasic.CompilerServices.Conversions.IsHexOrOctValue(Value As System.String, ByRef i64Value As System.Int64) As System.Boolean
            >  Function Microsoft.VisualBasic.CompilerServices.Conversions.ToBoolean(Value As System.Object) As System.Boolean
            >  Function Microsoft.VisualBasic.CompilerServices.Conversions.ToBoolean(Value As System.String) As System.Boolean
            >  Function Microsoft.VisualBasic.CompilerServices.Conversions.ToHalfwidthNumbers(s As System.String, culture As System.Globalization.CultureInfo) As System.String
          End Class
          [Microsoft.VisualBasic.Embedded]
          [System.Diagnostics.DebuggerNonUserCodeAttribute]
          [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
          [System.ComponentModel.EditorBrowsableAttribute]
          Class Microsoft.VisualBasic.CompilerServices.ProjectData
            >  Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()
            >  Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(ex As System.Exception)
          End Class
        End Namespace
      End Namespace
    End Namespace
  End Namespace
</expected>.Value)
                 End Sub)
        End Sub

        <Fact()>
        Public Sub VbCore_SingleSymbol_CompilerServices_Conversions_ToSByte_String()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq
Imports Microsoft.VisualBasic

Class Program
    Shared Sub Main(args As String())
        Console.Write(CompilerServices.Conversions.ToSByte("77").ToString())
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:="77",
sourceSymbolValidator:=Sub([module]) ValidateSourceSymbols([module]),
symbolValidator:=Sub([module])
                     ValidateSymbols([module],
         <expected>
  Namespace Global
    Class Program
      >  Sub Program..ctor()
         [System.STAThreadAttribute]
      >  Sub Program.Main(args As System.String())
    End Class
    Namespace Microsoft
      Namespace Microsoft.VisualBasic
        [Microsoft.VisualBasic.Embedded]
        [System.AttributeUsageAttribute]
        [System.ComponentModel.EditorBrowsableAttribute]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        Class Microsoft.VisualBasic.Embedded
          >  Sub Microsoft.VisualBasic.Embedded..ctor()
        End Class
        Namespace Microsoft.VisualBasic.CompilerServices
          [Microsoft.VisualBasic.Embedded]
          [System.Diagnostics.DebuggerNonUserCodeAttribute]
          [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
          [System.ComponentModel.EditorBrowsableAttribute]
          Class Microsoft.VisualBasic.CompilerServices.Conversions
            >  Function Microsoft.VisualBasic.CompilerServices.Conversions.GetCultureInfo() As System.Globalization.CultureInfo
            >  Function Microsoft.VisualBasic.CompilerServices.Conversions.IsHexOrOctValue(Value As System.String, ByRef i64Value As System.Int64) As System.Boolean
            >  Function Microsoft.VisualBasic.CompilerServices.Conversions.ToHalfwidthNumbers(s As System.String, culture As System.Globalization.CultureInfo) As System.String
               [System.CLSCompliantAttribute]
            >  Function Microsoft.VisualBasic.CompilerServices.Conversions.ToSByte(Value As System.String) As System.SByte
          End Class
          [Microsoft.VisualBasic.Embedded]
          [System.Diagnostics.DebuggerNonUserCodeAttribute]
          [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
          [System.ComponentModel.EditorBrowsableAttribute]
          Class Microsoft.VisualBasic.CompilerServices.ProjectData
            >  Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()
            >  Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(ex As System.Exception)
          End Class
        End Namespace
      End Namespace
    End Namespace
  End Namespace
</expected>.Value)
                 End Sub)
        End Sub

        <Fact()>
        Public Sub VbCore_SingleSymbol_CompilerServices_Conversions_ToSByte_Object()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq
Imports Microsoft.VisualBasic

Class Program
    Shared Sub Main(args As String())
        Console.Write(CompilerServices.Conversions.ToSByte(directcast("77", Object)).ToString())
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:="77",
sourceSymbolValidator:=Sub([module]) ValidateSourceSymbols([module]),
symbolValidator:=Sub([module])
                     ValidateSymbols([module],
         <expected>
  Namespace Global
    Class Program
      >  Sub Program..ctor()
         [System.STAThreadAttribute]
      >  Sub Program.Main(args As System.String())
    End Class
    Namespace Microsoft
      Namespace Microsoft.VisualBasic
        [Microsoft.VisualBasic.Embedded]
        [System.AttributeUsageAttribute]
        [System.ComponentModel.EditorBrowsableAttribute]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        Class Microsoft.VisualBasic.Embedded
          >  Sub Microsoft.VisualBasic.Embedded..ctor()
        End Class
        Namespace Microsoft.VisualBasic.CompilerServices
          [Microsoft.VisualBasic.Embedded]
          [System.Diagnostics.DebuggerNonUserCodeAttribute]
          [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
          [System.ComponentModel.EditorBrowsableAttribute]
          Class Microsoft.VisualBasic.CompilerServices.Conversions
            >  Function Microsoft.VisualBasic.CompilerServices.Conversions.GetCultureInfo() As System.Globalization.CultureInfo
            >  Function Microsoft.VisualBasic.CompilerServices.Conversions.IsHexOrOctValue(Value As System.String, ByRef i64Value As System.Int64) As System.Boolean
            >  Function Microsoft.VisualBasic.CompilerServices.Conversions.ToHalfwidthNumbers(s As System.String, culture As System.Globalization.CultureInfo) As System.String
               [System.CLSCompliantAttribute]
            >  Function Microsoft.VisualBasic.CompilerServices.Conversions.ToSByte(Value As System.Object) As System.SByte
               [System.CLSCompliantAttribute]
            >  Function Microsoft.VisualBasic.CompilerServices.Conversions.ToSByte(Value As System.String) As System.SByte
          End Class
          [Microsoft.VisualBasic.Embedded]
          [System.Diagnostics.DebuggerNonUserCodeAttribute]
          [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
          [System.ComponentModel.EditorBrowsableAttribute]
          Class Microsoft.VisualBasic.CompilerServices.ProjectData
            >  Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()
            >  Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(ex As System.Exception)
          End Class
        End Namespace
      End Namespace
    End Namespace
  End Namespace
</expected>.Value)
                 End Sub)
        End Sub

        <Fact()>
        Public Sub VbCore_SingleSymbol_CompilerServices_Utils_CopyArray()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq
Imports Microsoft.VisualBasic

Class Program
    Shared Sub Main(args As String())
        Dim a(10) As String
        Redim Preserve a(12)
        Console.Write(a.Length.ToString)
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:="13",
sourceSymbolValidator:=Sub([module]) ValidateSourceSymbols([module]),
symbolValidator:=Sub([module])
                     ValidateSymbols([module],
         <expected>
  Namespace Global
    Class Program
      >  Sub Program..ctor()
         [System.STAThreadAttribute]
      >  Sub Program.Main(args As System.String())
    End Class
    Namespace Microsoft
      Namespace Microsoft.VisualBasic
        [Microsoft.VisualBasic.Embedded]
        [System.AttributeUsageAttribute]
        [System.ComponentModel.EditorBrowsableAttribute]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        Class Microsoft.VisualBasic.Embedded
          >  Sub Microsoft.VisualBasic.Embedded..ctor()
        End Class
        Namespace Microsoft.VisualBasic.CompilerServices
          [Microsoft.VisualBasic.Embedded]
          [System.Diagnostics.DebuggerNonUserCodeAttribute]
          [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
          [System.ComponentModel.EditorBrowsableAttribute]
          Class Microsoft.VisualBasic.CompilerServices.Utils
            >  Function Microsoft.VisualBasic.CompilerServices.Utils.CopyArray(arySrc As System.Array, aryDest As System.Array) As System.Array
          End Class
        End Namespace
      End Namespace
    End Namespace
  End Namespace
</expected>.Value)
                 End Sub)
        End Sub

        <Fact()>
        Public Sub VbCore_SingleSymbol_CompilerServices_ObjectFlowControl()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq
Imports Microsoft.VisualBasic

Class Program
    Shared Sub Main(args As String())
        Console.Write(GetType(CompilerServices.ObjectFlowControl).ToString())
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:="Microsoft.VisualBasic.CompilerServices.ObjectFlowControl",
sourceSymbolValidator:=Sub([module]) ValidateSourceSymbols([module]),
symbolValidator:=Sub([module])
                     ValidateSymbols([module],
<expected>
  Namespace Global
    Class Program
      >  Sub Program..ctor()
         [System.STAThreadAttribute]
      >  Sub Program.Main(args As System.String())
    End Class
    Namespace Microsoft
      Namespace Microsoft.VisualBasic
        [Microsoft.VisualBasic.Embedded]
        [System.AttributeUsageAttribute]
        [System.ComponentModel.EditorBrowsableAttribute]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        Class Microsoft.VisualBasic.Embedded
          >  Sub Microsoft.VisualBasic.Embedded..ctor()
        End Class
        Namespace Microsoft.VisualBasic.CompilerServices
          [Microsoft.VisualBasic.Embedded]
          [System.Diagnostics.DebuggerNonUserCodeAttribute]
          [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
          [System.ComponentModel.EditorBrowsableAttribute]
          Class Microsoft.VisualBasic.CompilerServices.ObjectFlowControl
            >  Sub Microsoft.VisualBasic.CompilerServices.ObjectFlowControl..ctor()
          End Class
        End Namespace
      End Namespace
    End Namespace
  End Namespace
</expected>.Value)
                 End Sub)
        End Sub

        <Fact()>
        Public Sub VbCore_SingleSymbol_CompilerServices_ObjectFlowControl_ForLoopControl()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq
Imports Microsoft.VisualBasic

Class Program
    Shared Sub Main(args As String())
        Console.Write(GetType(CompilerServices.ObjectFlowControl.ForLoopControl).ToString())
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:="Microsoft.VisualBasic.CompilerServices.ObjectFlowControl+ForLoopControl",
sourceSymbolValidator:=Sub([module]) ValidateSourceSymbols([module]),
symbolValidator:=Sub([module])
                     ValidateSymbols([module],
<expected>
  Namespace Global
    Class Program
      >  Sub Program..ctor()
         [System.STAThreadAttribute]
      >  Sub Program.Main(args As System.String())
    End Class
    Namespace Microsoft
      Namespace Microsoft.VisualBasic
        [Microsoft.VisualBasic.Embedded]
        [System.AttributeUsageAttribute]
        [System.ComponentModel.EditorBrowsableAttribute]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        Class Microsoft.VisualBasic.Embedded
          >  Sub Microsoft.VisualBasic.Embedded..ctor()
        End Class
        Namespace Microsoft.VisualBasic.CompilerServices
          [Microsoft.VisualBasic.Embedded]
          [System.Diagnostics.DebuggerNonUserCodeAttribute]
          [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
          [System.ComponentModel.EditorBrowsableAttribute]
          Class Microsoft.VisualBasic.CompilerServices.ObjectFlowControl
            >  Sub Microsoft.VisualBasic.CompilerServices.ObjectFlowControl..ctor()
            Class Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.ForLoopControl
              >  Sub Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.ForLoopControl..ctor()
            End Class
          End Class
        End Namespace
      End Namespace
    End Namespace
  End Namespace
</expected>.Value)
                 End Sub)
        End Sub

        <Fact()>
        Public Sub VbCore_SingleSymbol_CompilerServices_ObjectFlowControl_ForLoopControl_ForNextCheckR8()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq
Imports Microsoft.VisualBasic

Class Program
    Shared Sub Main(args As String())
        Console.Write(CompilerServices.ObjectFlowControl.ForLoopControl.ForNextCheckR8(CDbl(100), 1, 1))
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:="False",
sourceSymbolValidator:=Sub([module]) ValidateSourceSymbols([module]),
symbolValidator:=Sub([module])
                     ValidateSymbols([module],
<expected>
  Namespace Global
    Class Program
      >  Sub Program..ctor()
         [System.STAThreadAttribute]
      >  Sub Program.Main(args As System.String())
    End Class
    Namespace Microsoft
      Namespace Microsoft.VisualBasic
        [Microsoft.VisualBasic.Embedded]
        [System.AttributeUsageAttribute]
        [System.ComponentModel.EditorBrowsableAttribute]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        Class Microsoft.VisualBasic.Embedded
          >  Sub Microsoft.VisualBasic.Embedded..ctor()
        End Class
        Namespace Microsoft.VisualBasic.CompilerServices
          [Microsoft.VisualBasic.Embedded]
          [System.Diagnostics.DebuggerNonUserCodeAttribute]
          [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
          [System.ComponentModel.EditorBrowsableAttribute]
          Class Microsoft.VisualBasic.CompilerServices.ObjectFlowControl
            >  Sub Microsoft.VisualBasic.CompilerServices.ObjectFlowControl..ctor()
            Class Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.ForLoopControl
              >  Function Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.ForLoopControl.ForNextCheckR8(count As System.Double, limit As System.Double, StepValue As System.Double) As System.Boolean
              >  Sub Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.ForLoopControl..ctor()
            End Class
          End Class
        End Namespace
      End Namespace
    End Namespace
  End Namespace
</expected>.Value)
                 End Sub)
        End Sub

        <Fact()>
        Public Sub VbCore_SingleSymbol_CompilerServices_StaticLocalInitFlag()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq
Imports Microsoft.VisualBasic

Class Program
    Shared Sub Main(args As String())
        Console.Write(GetType(CompilerServices.StaticLocalInitFlag).ToString())
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:="Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag",
sourceSymbolValidator:=Sub([module]) ValidateSourceSymbols([module]),
symbolValidator:=Sub([module])
                     ValidateSymbols([module],
<expected>
  Namespace Global
    Class Program
      >  Sub Program..ctor()
         [System.STAThreadAttribute]
      >  Sub Program.Main(args As System.String())
    End Class
    Namespace Microsoft
      Namespace Microsoft.VisualBasic
        [Microsoft.VisualBasic.Embedded]
        [System.AttributeUsageAttribute]
        [System.ComponentModel.EditorBrowsableAttribute]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        Class Microsoft.VisualBasic.Embedded
          >  Sub Microsoft.VisualBasic.Embedded..ctor()
        End Class
        Namespace Microsoft.VisualBasic.CompilerServices
          [Microsoft.VisualBasic.Embedded]
          [System.Diagnostics.DebuggerNonUserCodeAttribute]
          [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
          [System.ComponentModel.EditorBrowsableAttribute]
          Class Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag
            >  Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag.State As System.Int16
            >  Sub Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag..ctor()
          End Class
        End Namespace
      End Namespace
    End Namespace
  End Namespace
</expected>.Value)
                 End Sub)
        End Sub

        <Fact()>
        Public Sub VbCore_SingleSymbol_CompilerServices_StaticLocalInitFlag_State()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq
Imports Microsoft.VisualBasic

Class Program
    Shared Sub Main(args As String())
        Dim v As New CompilerServices.StaticLocalInitFlag
        v.State = 1
        Console.Write(v.State.ToString())
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:="1",
sourceSymbolValidator:=Sub([module]) ValidateSourceSymbols([module]),
symbolValidator:=Sub([module])
                     ValidateSymbols([module],
<expected>
  Namespace Global
    Class Program
      >  Sub Program..ctor()
         [System.STAThreadAttribute]
      >  Sub Program.Main(args As System.String())
    End Class
    Namespace Microsoft
      Namespace Microsoft.VisualBasic
        [Microsoft.VisualBasic.Embedded]
        [System.AttributeUsageAttribute]
        [System.ComponentModel.EditorBrowsableAttribute]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        Class Microsoft.VisualBasic.Embedded
          >  Sub Microsoft.VisualBasic.Embedded..ctor()
        End Class
        Namespace Microsoft.VisualBasic.CompilerServices
          [Microsoft.VisualBasic.Embedded]
          [System.Diagnostics.DebuggerNonUserCodeAttribute]
          [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
          [System.ComponentModel.EditorBrowsableAttribute]
          Class Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag
            >  Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag.State As System.Int16
            >  Sub Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag..ctor()
          End Class
        End Namespace
      End Namespace
    End Namespace
  End Namespace
</expected>.Value)
                 End Sub)
        End Sub

        <Fact()>
        Public Sub VbCore_SingleSymbol_CompilerServices_IncompleteInitialization()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq
Imports Microsoft.VisualBasic

Class Program
    Shared Sub Main(args As String())
        Console.Write(GetType(CompilerServices.IncompleteInitialization).ToString())
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:="Microsoft.VisualBasic.CompilerServices.IncompleteInitialization",
sourceSymbolValidator:=Sub([module]) ValidateSourceSymbols([module]),
symbolValidator:=Sub([module])
                     ValidateSymbols([module],
<expected>
  Namespace Global
    Class Program
      >  Sub Program..ctor()
         [System.STAThreadAttribute]
      >  Sub Program.Main(args As System.String())
    End Class
    Namespace Microsoft
      Namespace Microsoft.VisualBasic
        [Microsoft.VisualBasic.Embedded]
        [System.AttributeUsageAttribute]
        [System.ComponentModel.EditorBrowsableAttribute]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        Class Microsoft.VisualBasic.Embedded
          >  Sub Microsoft.VisualBasic.Embedded..ctor()
        End Class
        Namespace Microsoft.VisualBasic.CompilerServices
          [Microsoft.VisualBasic.Embedded]
          [System.Diagnostics.DebuggerNonUserCodeAttribute]
          [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
          [System.ComponentModel.EditorBrowsableAttribute]
          Class Microsoft.VisualBasic.CompilerServices.IncompleteInitialization
            >  Sub Microsoft.VisualBasic.CompilerServices.IncompleteInitialization..ctor()
          End Class
        End Namespace
      End Namespace
    End Namespace
  End Namespace
</expected>.Value)
                 End Sub)
        End Sub

        <Fact()>
        Public Sub VbCore_SingleSymbol_CompilerServices_IncompleteInitialization_Throw()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq
Imports Microsoft.VisualBasic

Class Program
    Shared Sub Main(args As String())
        Try
            Throw New CompilerServices.IncompleteInitialization()
        Catch ex As Exception
            Console.Write(ex.GetType().ToString())
        End Try
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:="Microsoft.VisualBasic.CompilerServices.IncompleteInitialization",
sourceSymbolValidator:=Sub([module]) ValidateSourceSymbols([module]),
symbolValidator:=Sub([module])
                     ValidateSymbols([module],
<expected>
  Namespace Global
    Class Program
      >  Sub Program..ctor()
         [System.STAThreadAttribute]
      >  Sub Program.Main(args As System.String())
    End Class
    Namespace Microsoft
      Namespace Microsoft.VisualBasic
        [Microsoft.VisualBasic.Embedded]
        [System.AttributeUsageAttribute]
        [System.ComponentModel.EditorBrowsableAttribute]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        Class Microsoft.VisualBasic.Embedded
          >  Sub Microsoft.VisualBasic.Embedded..ctor()
        End Class
        Namespace Microsoft.VisualBasic.CompilerServices
          [Microsoft.VisualBasic.Embedded]
          [System.Diagnostics.DebuggerNonUserCodeAttribute]
          [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
          [System.ComponentModel.EditorBrowsableAttribute]
          Class Microsoft.VisualBasic.CompilerServices.IncompleteInitialization
            >  Sub Microsoft.VisualBasic.CompilerServices.IncompleteInitialization..ctor()
          End Class
          [Microsoft.VisualBasic.Embedded]
          [System.Diagnostics.DebuggerNonUserCodeAttribute]
          [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
          [System.ComponentModel.EditorBrowsableAttribute]
          Class Microsoft.VisualBasic.CompilerServices.ProjectData
            >  Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()
            >  Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(ex As System.Exception)
          End Class
        End Namespace
      End Namespace
    End Namespace
  End Namespace
</expected>.Value)
                 End Sub)
        End Sub

        <Fact()>
        Public Sub VbCore_SingleSymbol_CompilerServices_StandardModuleAttribute()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq
Imports Microsoft.VisualBasic
Imports Microsoft.VisualBasic.CompilerServices

&lt;StandardModuleAttribute()&gt;
Class Program
    Shared Sub Main(args As String())
        Console.Write("")
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:="",
sourceSymbolValidator:=Sub([module]) ValidateSourceSymbols([module]),
symbolValidator:=Sub([module])
                     ValidateSymbols([module],
<expected>
  Namespace Global
    Module Program
      >  Sub Program..ctor()
         [System.STAThreadAttribute]
      >  Sub Program.Main(args As System.String())
    End Module
    Namespace Microsoft
      Namespace Microsoft.VisualBasic
        [Microsoft.VisualBasic.Embedded]
        [System.AttributeUsageAttribute]
        [System.ComponentModel.EditorBrowsableAttribute]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        Class Microsoft.VisualBasic.Embedded
          >  Sub Microsoft.VisualBasic.Embedded..ctor()
        End Class
        Namespace Microsoft.VisualBasic.CompilerServices
          [Microsoft.VisualBasic.Embedded]
          [System.AttributeUsageAttribute]
          [System.ComponentModel.EditorBrowsableAttribute]
          [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
          Class Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute
            >  Sub Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute..ctor()
          End Class
        End Namespace
      End Namespace
    End Namespace
  End Namespace
</expected>.Value)
                 End Sub)
        End Sub

        <Fact()>
        Public Sub VbCore_SingleSymbol_CompilerServices_DesignerGeneratedAttribute()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq
Imports Microsoft.VisualBasic
Imports Microsoft.VisualBasic.CompilerServices

&lt;DesignerGeneratedAttribute()&gt;
Class Program
    Shared Sub Main(args As String())
        Console.Write("")
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:="",
sourceSymbolValidator:=Sub([module]) ValidateSourceSymbols([module]),
symbolValidator:=Sub([module])
                     ValidateSymbols([module],
<expected>
  Namespace Global
    [Microsoft.VisualBasic.CompilerServices.DesignerGeneratedAttribute]
    Class Program
      >  Sub Program..ctor()
         [System.STAThreadAttribute]
      >  Sub Program.Main(args As System.String())
    End Class
    Namespace Microsoft
      Namespace Microsoft.VisualBasic
        [Microsoft.VisualBasic.Embedded]
        [System.AttributeUsageAttribute]
        [System.ComponentModel.EditorBrowsableAttribute]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        Class Microsoft.VisualBasic.Embedded
          >  Sub Microsoft.VisualBasic.Embedded..ctor()
        End Class
        Namespace Microsoft.VisualBasic.CompilerServices
          [Microsoft.VisualBasic.Embedded]
          [System.AttributeUsageAttribute]
          [System.ComponentModel.EditorBrowsableAttribute]
          [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
          Class Microsoft.VisualBasic.CompilerServices.DesignerGeneratedAttribute
            >  Sub Microsoft.VisualBasic.CompilerServices.DesignerGeneratedAttribute..ctor()
          End Class
        End Namespace
      End Namespace
    End Namespace
  End Namespace
</expected>.Value)
                 End Sub)
        End Sub

        <Fact()>
        Public Sub VbCore_SingleSymbol_CompilerServices_OptionCompareAttribute()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq
Imports Microsoft.VisualBasic
Imports Microsoft.VisualBasic.CompilerServices

Class Program
    Shared Sub Main(&lt;OptionCompareAttribute()&gt;args As String())
        Console.Write("")
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:="",
sourceSymbolValidator:=Sub([module]) ValidateSourceSymbols([module]),
symbolValidator:=Sub([module])
                     ValidateSymbols([module],
<expected>
  Namespace Global
    Class Program
      >  Sub Program..ctor()
         [System.STAThreadAttribute]
      >  Sub Program.Main(args As System.String())
    End Class
    Namespace Microsoft
      Namespace Microsoft.VisualBasic
        [Microsoft.VisualBasic.Embedded]
        [System.AttributeUsageAttribute]
        [System.ComponentModel.EditorBrowsableAttribute]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        Class Microsoft.VisualBasic.Embedded
          >  Sub Microsoft.VisualBasic.Embedded..ctor()
        End Class
        Namespace Microsoft.VisualBasic.CompilerServices
          [Microsoft.VisualBasic.Embedded]
          [System.AttributeUsageAttribute]
          [System.ComponentModel.EditorBrowsableAttribute]
          [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
          Class Microsoft.VisualBasic.CompilerServices.OptionCompareAttribute
            >  Sub Microsoft.VisualBasic.CompilerServices.OptionCompareAttribute..ctor()
          End Class
        End Namespace
      End Namespace
    End Namespace
  End Namespace
</expected>.Value)
                 End Sub)
        End Sub

        <Fact()>
        Public Sub VbCore_SingleSymbol_CompilerServices_OptionTextAttribute()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq
Imports Microsoft.VisualBasic
Imports Microsoft.VisualBasic.CompilerServices

&lt;OptionTextAttribute()&gt;
Class Program
    Shared Sub Main(args As String())
        Console.Write("")
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:="",
sourceSymbolValidator:=Sub([module]) ValidateSourceSymbols([module]),
symbolValidator:=Sub([module])
                     ValidateSymbols([module],
<expected>
  Namespace Global
    [Microsoft.VisualBasic.CompilerServices.OptionTextAttribute]
    Class Program
      >  Sub Program..ctor()
         [System.STAThreadAttribute]
      >  Sub Program.Main(args As System.String())
    End Class
    Namespace Microsoft
      Namespace Microsoft.VisualBasic
        [Microsoft.VisualBasic.Embedded]
        [System.AttributeUsageAttribute]
        [System.ComponentModel.EditorBrowsableAttribute]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        Class Microsoft.VisualBasic.Embedded
          >  Sub Microsoft.VisualBasic.Embedded..ctor()
        End Class
        Namespace Microsoft.VisualBasic.CompilerServices
          [Microsoft.VisualBasic.Embedded]
          [System.AttributeUsageAttribute]
          [System.ComponentModel.EditorBrowsableAttribute]
          [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
          Class Microsoft.VisualBasic.CompilerServices.OptionTextAttribute
            >  Sub Microsoft.VisualBasic.CompilerServices.OptionTextAttribute..ctor()
          End Class
        End Namespace
      End Namespace
    End Namespace
  End Namespace
</expected>.Value)
                 End Sub)
        End Sub

        <Fact()>
        Public Sub VbCore_SingleSymbol_HideModuleNameAttribute()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq
Imports Microsoft.VisualBasic
Imports Microsoft.VisualBasic.CompilerServices

&lt;HideModuleNameAttribute()&gt;
Class Program
    Shared Sub Main(args As String())
        Console.Write("")
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:="",
sourceSymbolValidator:=Sub([module]) ValidateSourceSymbols([module]),
symbolValidator:=Sub([module])
                     ValidateSymbols([module],
<expected>
  Namespace Global
    [Microsoft.VisualBasic.HideModuleNameAttribute]
    Class Program
      >  Sub Program..ctor()
         [System.STAThreadAttribute]
      >  Sub Program.Main(args As System.String())
    End Class
    Namespace Microsoft
      Namespace Microsoft.VisualBasic
        [Microsoft.VisualBasic.Embedded]
        [System.AttributeUsageAttribute]
        [System.ComponentModel.EditorBrowsableAttribute]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        Class Microsoft.VisualBasic.Embedded
          >  Sub Microsoft.VisualBasic.Embedded..ctor()
        End Class
        [Microsoft.VisualBasic.Embedded]
        [System.AttributeUsageAttribute]
        [System.ComponentModel.EditorBrowsableAttribute]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        Class Microsoft.VisualBasic.HideModuleNameAttribute
          >  Sub Microsoft.VisualBasic.HideModuleNameAttribute..ctor()
        End Class
      End Namespace
    End Namespace
  End Namespace
</expected>.Value)
                 End Sub)
        End Sub

        <Fact()>
        <WorkItem(544511, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544511")>
        Public Sub VbCore_SingleSymbol_Strings_AscW_Char()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq
Imports Microsoft.VisualBasic

Class Program
    Shared Sub Main(args As String())
        Dim ch As Char = "A"c
        Console.Write(AscW(ch))
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:="65",
sourceSymbolValidator:=Sub([module]) ValidateSourceSymbols([module]),
symbolValidator:=Sub([module])
                     ValidateSymbols([module],
         <expected>
  Namespace Global
    Class Program
      >  Sub Program..ctor()
         [System.STAThreadAttribute]
      >  Sub Program.Main(args As System.String())
    End Class
  End Namespace
</expected>.Value)
                 End Sub)
        End Sub

        <Fact()>
        Public Sub VbCore_SingleSymbol_Strings_ChrW_Char()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports Microsoft.VisualBasic

Class Program
    Shared Sub Main(args As String())
        Dim ch As Integer = 65
        Console.Write(ChrW(ch))
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:="A",
sourceSymbolValidator:=Sub([module]) ValidateSourceSymbols([module]),
symbolValidator:=Sub([module])
                     ValidateSymbols([module],
         <expected>
  Namespace Global
    Class Program
      >  Sub Program..ctor()
         [System.STAThreadAttribute]
      >  Sub Program.Main(args As System.String())
    End Class
    Namespace Microsoft
      Namespace Microsoft.VisualBasic
        [Microsoft.VisualBasic.Embedded]
        [System.AttributeUsageAttribute]
        [System.ComponentModel.EditorBrowsableAttribute]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        Class Microsoft.VisualBasic.Embedded
          >  Sub Microsoft.VisualBasic.Embedded..ctor()
        End Class
        [Microsoft.VisualBasic.Embedded]
        [System.Diagnostics.DebuggerNonUserCodeAttribute]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        Module Microsoft.VisualBasic.Strings
          >  Function Microsoft.VisualBasic.Strings.ChrW(CharCode As System.Int32) As System.Char
        End Module
        Namespace Microsoft.VisualBasic.CompilerServices
          [Microsoft.VisualBasic.Embedded]
          [System.AttributeUsageAttribute]
          [System.ComponentModel.EditorBrowsableAttribute]
          [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
          Class Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute
            >  Sub Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute..ctor()
          End Class
        End Namespace
      End Namespace
    End Namespace
  End Namespace
</expected>.Value)
                 End Sub)
        End Sub

        <Fact()>
        Public Sub VbCore_SingleSymbol_Strings_ChrW_Char_MultipleEmits()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports Microsoft.VisualBasic

Class Program
    Shared Sub Main(args As String())
        Dim ch As Integer = 65
        Console.Write(ChrW(ch))
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:="A",
sourceSymbolValidator:=Sub([module]) ValidateSourceSymbols([module]),
symbolValidator:=Sub([module])
                     ValidateSymbols([module],
         <expected>
  Namespace Global
    Class Program
      >  Sub Program..ctor()
         [System.STAThreadAttribute]
      >  Sub Program.Main(args As System.String())
    End Class
    Namespace Microsoft
      Namespace Microsoft.VisualBasic
        [Microsoft.VisualBasic.Embedded]
        [System.AttributeUsageAttribute]
        [System.ComponentModel.EditorBrowsableAttribute]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        Class Microsoft.VisualBasic.Embedded
          >  Sub Microsoft.VisualBasic.Embedded..ctor()
        End Class
        [Microsoft.VisualBasic.Embedded]
        [System.Diagnostics.DebuggerNonUserCodeAttribute]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        Module Microsoft.VisualBasic.Strings
          >  Function Microsoft.VisualBasic.Strings.ChrW(CharCode As System.Int32) As System.Char
        End Module
        Namespace Microsoft.VisualBasic.CompilerServices
          [Microsoft.VisualBasic.Embedded]
          [System.AttributeUsageAttribute]
          [System.ComponentModel.EditorBrowsableAttribute]
          [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
          Class Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute
            >  Sub Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute..ctor()
          End Class
        End Namespace
      End Namespace
    End Namespace
  End Namespace
</expected>.Value)
                 End Sub).Compilation

            For i = 0 To 10
                Using memory As New MemoryStream()
                    compilation.Emit(memory)
                End Using
            Next
        End Sub

        <Fact()>
        Public Sub VbCore_TypesReferencedFromAttributes()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports Microsoft.VisualBasic
Imports Microsoft.VisualBasic.CompilerServices

&lt;AttributeUsage(AttributeTargets.All)&gt;
Class Attr
    Inherits Attribute
    Public Sub New(_type As Type)
    End Sub
    Public Type As Type
End Class

&lt;Attr(GetType(Strings), Type:=GetType(Microsoft.VisualBasic.CompilerServices.Conversions))&gt;
Module Program
    Sub Main(args As String())
    End Sub
End Module

    </file>
</compilation>,
expectedOutput:="",
sourceSymbolValidator:=Sub([module]) ValidateSourceSymbols([module]),
symbolValidator:=Sub([module])
                     ValidateSymbols([module],
         <expected>
  Namespace Global
    [System.AttributeUsageAttribute]
    Class Attr
      >  Attr.Type As System.Type
      >  Sub Attr..ctor(_type As System.Type)
    End Class
    [Attr]
    Module Program
         [System.STAThreadAttribute]
      >  Sub Program.Main(args As System.String())
    End Module
    Namespace Microsoft
      Namespace Microsoft.VisualBasic
        [Microsoft.VisualBasic.Embedded]
        [System.AttributeUsageAttribute]
        [System.ComponentModel.EditorBrowsableAttribute]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        Class Microsoft.VisualBasic.Embedded
          >  Sub Microsoft.VisualBasic.Embedded..ctor()
        End Class
        [Microsoft.VisualBasic.Embedded]
        [System.Diagnostics.DebuggerNonUserCodeAttribute]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        Module Microsoft.VisualBasic.Strings
        End Module
        Namespace Microsoft.VisualBasic.CompilerServices
          [Microsoft.VisualBasic.Embedded]
          [System.Diagnostics.DebuggerNonUserCodeAttribute]
          [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
          [System.ComponentModel.EditorBrowsableAttribute]
          Class Microsoft.VisualBasic.CompilerServices.Conversions
          End Class
          [Microsoft.VisualBasic.Embedded]
          [System.AttributeUsageAttribute]
          [System.ComponentModel.EditorBrowsableAttribute]
          [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
          Class Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute
            >  Sub Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute..ctor()
          End Class
        End Namespace
      End Namespace
    End Namespace
  End Namespace
</expected>.Value)
                 End Sub)
        End Sub

        <Fact()>
        Public Sub VbCore_TypesReferencedFromAttributes_Array()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports Microsoft.VisualBasic
Imports Microsoft.VisualBasic.CompilerServices

&lt;AttributeUsage(AttributeTargets.All)&gt;
Class Attr
    Inherits Attribute
    Public Types() As Type
End Class

&lt;Attr(Types:= New Type() {GetType(Conversions), GetType(Strings)})&gt;
Module Program
    Sub Main(args As String())
    End Sub
End Module

    </file>
</compilation>,
expectedOutput:="",
sourceSymbolValidator:=Sub([module]) ValidateSourceSymbols([module]),
symbolValidator:=Sub([module])
                     ValidateSymbols([module],
         <expected>
  Namespace Global
    [System.AttributeUsageAttribute]
    Class Attr
      >  Attr.Types As System.Type()
      >  Sub Attr..ctor()
    End Class
    [Attr]
    Module Program
         [System.STAThreadAttribute]
      >  Sub Program.Main(args As System.String())
    End Module
    Namespace Microsoft
      Namespace Microsoft.VisualBasic
        [Microsoft.VisualBasic.Embedded]
        [System.AttributeUsageAttribute]
        [System.ComponentModel.EditorBrowsableAttribute]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        Class Microsoft.VisualBasic.Embedded
          >  Sub Microsoft.VisualBasic.Embedded..ctor()
        End Class
        [Microsoft.VisualBasic.Embedded]
        [System.Diagnostics.DebuggerNonUserCodeAttribute]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        Module Microsoft.VisualBasic.Strings
        End Module
        Namespace Microsoft.VisualBasic.CompilerServices
          [Microsoft.VisualBasic.Embedded]
          [System.Diagnostics.DebuggerNonUserCodeAttribute]
          [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
          [System.ComponentModel.EditorBrowsableAttribute]
          Class Microsoft.VisualBasic.CompilerServices.Conversions
          End Class
          [Microsoft.VisualBasic.Embedded]
          [System.AttributeUsageAttribute]
          [System.ComponentModel.EditorBrowsableAttribute]
          [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
          Class Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute
            >  Sub Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute..ctor()
          End Class
        End Namespace
      End Namespace
    End Namespace
  End Namespace
</expected>.Value)
                 End Sub)
        End Sub

        <Fact()>
        Public Sub VbCore_EnsurePrivateConstructorsEmitted()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Reflection
Imports Microsoft.VisualBasic

Module Program
    Sub Main(args As String())
        PrintConstructorInfo(GetType(CompilerServices.EmbeddedOperators))
        PrintConstructorInfo(GetType(CompilerServices.Conversions))
        PrintConstructorInfo(GetType(CompilerServices.ProjectData))
        PrintConstructorInfo(GetType(CompilerServices.Utils))
    End Sub
    Sub PrintConstructorInfo(type As Type)
        Dim constructor = type.GetConstructors(BindingFlags.Instance Or BindingFlags.NonPublic)
        Console.Write(type.ToString())
        Console.Write(" ")
        Console.WriteLine(constructor(0).ToString())
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:=
<output>
Microsoft.VisualBasic.CompilerServices.EmbeddedOperators Void .ctor()
Microsoft.VisualBasic.CompilerServices.Conversions Void .ctor()
Microsoft.VisualBasic.CompilerServices.ProjectData Void .ctor()
Microsoft.VisualBasic.CompilerServices.Utils Void .ctor()
</output>.Value.Replace(vbLf, Environment.NewLine),
sourceSymbolValidator:=Sub([module]) ValidateSourceSymbols([module]),
symbolValidator:=Sub([module])
                     ValidateSymbols([module],
<expected>
  Namespace Global
    Module Program
         [System.STAThreadAttribute]
      >  Sub Program.Main(args As System.String())
      >  Sub Program.PrintConstructorInfo(type As System.Type)
    End Module
    Namespace Microsoft
      Namespace Microsoft.VisualBasic
        [Microsoft.VisualBasic.Embedded]
        [System.AttributeUsageAttribute]
        [System.ComponentModel.EditorBrowsableAttribute]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        Class Microsoft.VisualBasic.Embedded
          >  Sub Microsoft.VisualBasic.Embedded..ctor()
        End Class
        Namespace Microsoft.VisualBasic.CompilerServices
          [Microsoft.VisualBasic.Embedded]
          [System.Diagnostics.DebuggerNonUserCodeAttribute]
          [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
          [System.ComponentModel.EditorBrowsableAttribute]
          Class Microsoft.VisualBasic.CompilerServices.Conversions
          End Class
          [Microsoft.VisualBasic.Embedded]
          [System.Diagnostics.DebuggerNonUserCodeAttribute]
          [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
          [System.ComponentModel.EditorBrowsableAttribute]
          Class Microsoft.VisualBasic.CompilerServices.EmbeddedOperators
          End Class
          [Microsoft.VisualBasic.Embedded]
          [System.Diagnostics.DebuggerNonUserCodeAttribute]
          [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
          [System.ComponentModel.EditorBrowsableAttribute]
          Class Microsoft.VisualBasic.CompilerServices.ProjectData
          End Class
          [Microsoft.VisualBasic.Embedded]
          [System.AttributeUsageAttribute]
          [System.ComponentModel.EditorBrowsableAttribute]
          [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
          Class Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute
            >  Sub Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute..ctor()
          End Class
          [Microsoft.VisualBasic.Embedded]
          [System.Diagnostics.DebuggerNonUserCodeAttribute]
          [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
          [System.ComponentModel.EditorBrowsableAttribute]
          Class Microsoft.VisualBasic.CompilerServices.Utils
          End Class
        End Namespace
      End Namespace
    End Namespace
  End Namespace
</expected>.Value)
                 End Sub)
        End Sub

        <Fact()>
        Public Sub VbCore_EmbeddedAttributeOnAssembly_NoReferences()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq
Imports Microsoft.VisualBasic

Class Program
    Shared Sub Main(args As String())
        For Each attr In GetType(Program).Assembly.GetCustomAttributes(True).ToArray()
            Dim name = attr.ToString()
            If name.IndexOf("Embedded") >= 0 Then
                Console.WriteLine(attr.GetType().ToString())
            End If
            dim x = vbNewLine
        Next
    End Sub
End Class

    </file>
</compilation>,
expectedOutput:="",
sourceSymbolValidator:=Sub([module]) ValidateSourceSymbols([module]),
symbolValidator:=Sub([module])
                     ValidateSymbols([module],
         <expected>
  Namespace Global
    Class Program
      >  Sub Program..ctor()
         [System.STAThreadAttribute]
      >  Sub Program.Main(args As System.String())
    End Class
  End Namespace
</expected>.Value)
                 End Sub)
        End Sub

        <Fact()>
        Public Sub VbCore_EmbeddedAttributeOnAssembly_References_NoDebug()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq
Imports Microsoft.VisualBasic

Class Program
    Shared Sub Main(args As String())
        For Each attr In GetType(Program).Assembly.GetCustomAttributes(True).ToArray()
            Dim name = attr.ToString()
            If name.IndexOf("Embedded") >= 0 Then
                Console.WriteLine(attr.GetType().ToString())
            End If
            dim x = GetType(Strings)
        Next
    End Sub
End Class

    </file>
</compilation>,
expectedOutput:="Microsoft.VisualBasic.Embedded",
sourceSymbolValidator:=Sub([module]) ValidateSourceSymbols([module]),
symbolValidator:=Sub([module])
                     ValidateSymbols([module],
         <expected>
  Namespace Global
    Class Program
      >  Sub Program..ctor()
         [System.STAThreadAttribute]
      >  Sub Program.Main(args As System.String())
    End Class
    Namespace Microsoft
      Namespace Microsoft.VisualBasic
        [Microsoft.VisualBasic.Embedded]
        [System.AttributeUsageAttribute]
        [System.ComponentModel.EditorBrowsableAttribute]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        Class Microsoft.VisualBasic.Embedded
          >  Sub Microsoft.VisualBasic.Embedded..ctor()
        End Class
        [Microsoft.VisualBasic.Embedded]
        [System.Diagnostics.DebuggerNonUserCodeAttribute]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        Module Microsoft.VisualBasic.Strings
        End Module
        Namespace Microsoft.VisualBasic.CompilerServices
          [Microsoft.VisualBasic.Embedded]
          [System.AttributeUsageAttribute]
          [System.ComponentModel.EditorBrowsableAttribute]
          [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
          Class Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute
            >  Sub Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute..ctor()
          End Class
        End Namespace
      End Namespace
    End Namespace
  End Namespace
</expected>.Value)
                 End Sub)
        End Sub

        <Fact>
        Public Sub VbCore_InvisibleViaInternalsVisibleTo()
            Dim other As VisualBasicCompilation = CompilationUtils.CreateEmptyCompilationWithReferences(
    <compilation name="HasIVTToCompilationVbCore">
        <file name="a.vb"><![CDATA[
Imports System
Imports Microsoft.VisualBasic
Imports Microsoft.VisualBasic.CompilerServices

<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("WantsIVTAccessVbCoreAndStillCannot")>

Friend Class SourceLibrary
    Shared Sub Main(args As String())
        Console.Write(ChrW(123)) ' Forces Microsoft.VisualBasic.Strings to be embedded into the assembly
    End Sub
    Public Shared U As Utils
End Class
]]>
        </file>
    </compilation>,
            references:=NoVbRuntimeReferences,
            options:=TestOptions.ReleaseDll.WithEmbedVbCoreRuntime(True))

            CompilationUtils.AssertNoErrors(other)

            Dim c As VisualBasicCompilation = CompilationUtils.CreateEmptyCompilationWithReferences(
    <compilation name="WantsIVTAccessVbCoreButCantHave">
        <file name="a.vb"><![CDATA[
Public Class A
    Friend Class B
        Protected Sub New()
          Dim a = GetType(Microsoft.VisualBasic.Strings)
        End Sub
    End Class
End Class
]]>
        </file>
    </compilation>,
            references:=NoVbRuntimeReferences.Concat({New VisualBasicCompilationReference(other)}),
            options:=TestOptions.ReleaseDll)

            'compilation should not succeed, and internals should not be imported.
            c.GetDiagnostics()

            CompilationUtils.AssertTheseDiagnostics(c,
<error>
BC30002: Type 'Microsoft.VisualBasic.Strings' is not defined.
          Dim a = GetType(Microsoft.VisualBasic.Strings)
                          ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</error>)

            Dim c2 As VisualBasicCompilation = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
    <compilation name="WantsIVTAccessVbCoreAndStillCannot">
        <file name="a.vb"><![CDATA[
Public Class A
    Friend Class B
        Protected Sub New()
          Dim a = GetType(Microsoft.VisualBasic.Strings)
        End Sub
    End Class
End Class
]]>
        </file>
    </compilation>,
            references:=NoVbRuntimeReferences.Concat({New VisualBasicCompilationReference(other)}),
            options:=TestOptions.ReleaseDll)

            CompilationUtils.AssertTheseDiagnostics(c2,
<error>
BC30002: Type 'Microsoft.VisualBasic.Strings' is not defined.
          Dim a = GetType(Microsoft.VisualBasic.Strings)
                          ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</error>)

        End Sub

        <Fact>
        Public Sub VbCore_InvisibleViaInternalsVisibleTo2()
            Dim other As VisualBasicCompilation = CompilationUtils.CreateEmptyCompilationWithReferences(
    <compilation name="VbCore_InvisibleViaInternalsVisibleTo2">
        <file name="a.vb"><![CDATA[
Imports System
Imports Microsoft.VisualBasic
Imports Microsoft.VisualBasic.CompilerServices

<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("WantsIVTAccessVbCoreAndStillCannot2")>

Friend Class SourceLibrary
    Shared Sub Main(args As String())
        Dim a() As String
        Redim Preserve a(2)
    End Sub
    Public Shared U As Utils
End Class

]]>
        </file>
    </compilation>,
            references:=NoVbRuntimeReferences,
            options:=TestOptions.ReleaseDll.WithEmbedVbCoreRuntime(True))

            CompilationUtils.AssertNoErrors(other)

            Dim c As VisualBasicCompilation = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
    <compilation name="WantsIVTAccessVbCoreAndStillCannot2">
        <file name="a.vb"><![CDATA[
Imports System
Imports Microsoft.VisualBasic
Imports Microsoft.VisualBasic.CompilerServices

Class Program
    Shared Sub Main(args As String())
        SourceLibrary.U.CopyArray(Nothing, Nothing)
    End Sub
End Class
]]>
        </file>
    </compilation>,
            references:=NoVbRuntimeReferences.Concat({New VisualBasicCompilationReference(other)}),
            options:=TestOptions.ReleaseDll)

            CompilationUtils.AssertTheseDiagnostics(c,
<error>
BC30456: 'CopyArray' is not a member of 'Utils'.
        SourceLibrary.U.CopyArray(Nothing, Nothing)
        ~~~~~~~~~~~~~~~~~~~~~~~~~
</error>)

        End Sub

        <Fact>
        Public Sub VbCore_InvisibleViaInternalsVisibleTo3()
            Dim other As VisualBasicCompilation = CompilationUtils.CreateEmptyCompilationWithReferences(
    <compilation name="VbCore_InvisibleViaInternalsVisibleTo3">
        <file name="a.vb"><![CDATA[
Imports System
Imports Microsoft.VisualBasic

<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("WantsIVTAccessVbCoreAndStillCannot3")>

Friend Class SourceLibrary
    Shared Sub Main(args As String())
        Console.Write(ChrW(args.Length))
    End Sub
End Class

]]>
        </file>
    </compilation>,
            references:=NoVbRuntimeReferences,
            options:=TestOptions.ReleaseDll.WithEmbedVbCoreRuntime(True))

            CompilationUtils.AssertNoErrors(other)

            Dim c As VisualBasicCompilation = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
    <compilation name="WantsIVTAccessVbCoreAndStillCannot3">
        <file name="a.vb"><![CDATA[
Imports System
Imports Microsoft.VisualBasic
Imports Microsoft.VisualBasic.CompilerServices

Class Program
    Shared Sub Main(args As String())
        Console.Write(ChrW(123))
    End Sub
End Class
]]>
        </file>
    </compilation>,
            references:=NoVbRuntimeReferences.Concat({New VisualBasicCompilationReference(other)}),
            options:=TestOptions.ReleaseDll)

            CompilationUtils.AssertTheseDiagnostics(c,
<error>
BC30451: 'ChrW' is not declared. It may be inaccessible due to its protection level.
        Console.Write(ChrW(123))
                      ~~~~
</error>)

        End Sub

        <Fact>
        Public Sub VbCore_InvisibleViaInternalsVisibleTo3_ViaBinary()
            Dim other As VisualBasicCompilation = CompilationUtils.CreateEmptyCompilationWithReferences(
    <compilation name="VbCore_InvisibleViaInternalsVisibleTo3">
        <file name="a.vb"><![CDATA[
Imports System
Imports Microsoft.VisualBasic

<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("WantsIVTAccessVbCoreAndStillCannot3")>

Friend Class SourceLibrary
    Shared Sub Main(args As String())
        Console.Write(ChrW(args.Length))
    End Sub
End Class

]]>
        </file>
    </compilation>,
            references:=NoVbRuntimeReferences,
            options:=TestOptions.ReleaseDll.WithEmbedVbCoreRuntime(True))

            CompilationUtils.AssertNoErrors(other)

            Dim memory As New MemoryStream()
            other.Emit(memory)

            Dim c As VisualBasicCompilation = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
    (<compilation name="WantsIVTAccessVbCoreAndStillCannot3">
         <file name="a.vb"><![CDATA[
Imports System
Imports Microsoft.VisualBasic

Class Program
    Shared Sub Main(args As String())
        Console.Write(ChrW(123))
    End Sub
End Class
]]>
         </file>
     </compilation>),
            references:=NoVbRuntimeReferences.Concat({MetadataReference.CreateFromImage(memory.ToImmutable())}),
            options:=TestOptions.ReleaseDll)

            CompilationUtils.AssertTheseDiagnostics(c,
<error>
BC30451: 'ChrW' is not declared. It may be inaccessible due to its protection level.
        Console.Write(ChrW(123))
                      ~~~~
</error>)

        End Sub

        <Fact>
        Public Sub VbCore_EmbeddedVbCoreWithIVToAndRuntime()
            Dim other As VisualBasicCompilation = CompilationUtils.CreateEmptyCompilationWithReferences(
    <compilation name="VbCore_EmbeddedVbCoreWithIVToAndRuntime">
        <file name="a.vb"><![CDATA[
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("WantsIVTAccessVbCoreAndStillCannot3")>
]]>
        </file>
    </compilation>,
            references:=NoVbRuntimeReferences,
            options:=TestOptions.ReleaseDll.WithEmbedVbCoreRuntime(True))

            CompilationUtils.AssertNoErrors(other)

            MyBase.CompileAndVerify(source:=
<compilation>
    <file name="a.vb">
Imports System
Imports Microsoft.VisualBasic
Imports Microsoft.VisualBasic.CompilerServices

Class Program
    Shared Sub Main(args As String())
        Try
            Dim s As String = "123"
            Dim i As Integer = s  ' This should use Conversions.ToInteger(String)
        Catch e As Exception  ' This should use ProjectData.SetProjectError()/ClearProjectError()
        End Try
    End Sub
End Class
    </file>
</compilation>,
            allReferences:=NoVbRuntimeReferences.Concat(MsvbRef).Concat(New VisualBasicCompilationReference(other)),
            expectedOutput:="",
            sourceSymbolValidator:=Sub([module]) ValidateSourceSymbols([module]),
            symbolValidator:=Sub([module])
                                 ValidateSymbols([module],
<expected>
  Namespace Global
    Class Program
      >  Sub Program..ctor()
         [System.STAThreadAttribute]
      >  Sub Program.Main(args As System.String())
    End Class
  End Namespace
</expected>.Value)
                             End Sub,
                options:=TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.Internal))
        End Sub

        <Fact()>
        Public Sub VbCore_CompilationOptions()
            Dim withoutVbCore As VisualBasicCompilation = CompilationUtils.CreateEmptyCompilationWithReferences(
    <compilation name="VbCore_CompilationOptions1">
        <file name="a.vb"><![CDATA[
Imports System
Imports Microsoft.VisualBasic
Friend Class SourceLibrary
    Shared Sub Main(args As String())
        Console.Write(ChrW(123))
    End Sub
End Class

]]>
        </file>
    </compilation>,
            references:=NoVbRuntimeReferences,
            options:=TestOptions.ReleaseDll.WithEmbedVbCoreRuntime(False))

            CompilationUtils.AssertTheseDiagnostics(withoutVbCore,
<error>
BC30451: 'ChrW' is not declared. It may be inaccessible due to its protection level.
        Console.Write(ChrW(123))
                      ~~~~
</error>)

            Dim withVbCore As VisualBasicCompilation = withoutVbCore.WithOptions(withoutVbCore.Options.WithEmbedVbCoreRuntime(True))
            CompilationUtils.AssertNoErrors(withVbCore)

            Dim withoutVbCore2 As VisualBasicCompilation = withVbCore.WithOptions(withVbCore.Options.WithEmbedVbCoreRuntime(False))
            CompilationUtils.AssertTheseDiagnostics(withoutVbCore2,
<error>
BC30451: 'ChrW' is not declared. It may be inaccessible due to its protection level.
        Console.Write(ChrW(123))
                      ~~~~
</error>)

            Dim withVbCore2 As VisualBasicCompilation = withoutVbCore.WithOptions(withoutVbCore2.Options.WithEmbedVbCoreRuntime(True))
            CompilationUtils.AssertNoErrors(withVbCore2)

        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub NoDebugInfoForVbCoreSymbols()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports Microsoft.VisualBasic

Class Program
    Shared Sub Main(args As String())
        Dim ch As Integer = 65
        Console.Write(ChrW(ch))
    End Sub
End Class
]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.DebugExe.WithEmbedVbCoreRuntime(True))
            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="a.vb" language="VB" checksumAlgorithm="SHA1" checksum="43-DF-02-C2-F5-5F-6A-CB-08-D3-1F-D2-8E-4F-FE-0A-8F-C2-76-D7"/>
    </files>
    <entryPoint declaringType="Program" methodName="Main" parameterNames="args"/>
    <methods>
        <method containingType="Program" name="Main" parameterNames="args">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="5" startColumn="5" endLine="5" endColumn="38" document="1"/>
                <entry offset="0x1" startLine="6" startColumn="13" endLine="6" endColumn="31" document="1"/>
                <entry offset="0x4" startLine="7" startColumn="9" endLine="7" endColumn="32" document="1"/>
                <entry offset="0x10" startLine="8" startColumn="5" endLine="8" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x11">
                <namespace name="System" importlevel="file"/>
                <namespace name="Microsoft.VisualBasic" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="ch" il_index="0" il_start="0x0" il_end="0x11" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <Fact>
        Public Sub VbCoreTypeAndUserPartialTypeConflict()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
<compilation>
    <file name="a.vb">
Imports System
Imports Microsoft.VisualBasic

Namespace Global.Microsoft.VisualBasic
    Partial Friend Class HideModuleNameAttribute
        Public Property A As String
    End Class
End Namespace
    </file>
</compilation>,
            references:={SystemRef, SystemCoreRef},
            options:=TestOptions.ReleaseDll.WithEmbedVbCoreRuntime(True))

            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
BC31210: class 'HideModuleNameAttribute' conflicts with a Visual Basic Runtime class 'HideModuleNameAttribute'.
    Partial Friend Class HideModuleNameAttribute
                         ~~~~~~~~~~~~~~~~~~~~~~~
</errors>)
        End Sub

        <Fact>
        Public Sub VbCoreTypeAndUserTypeConflict()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
<compilation>
    <file name="a.vb">
Imports System
Imports Microsoft.VisualBasic

Namespace Global.Microsoft.VisualBasic
    Friend Class HideModuleNameAttribute
        Public Property A As String
    End Class
End Namespace
    </file>
</compilation>,
            references:={SystemRef, SystemCoreRef},
            options:=TestOptions.ReleaseDll.WithEmbedVbCoreRuntime(True))

            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
BC31210: class 'HideModuleNameAttribute' conflicts with a Visual Basic Runtime class 'HideModuleNameAttribute'.
    Friend Class HideModuleNameAttribute
                 ~~~~~~~~~~~~~~~~~~~~~~~
</errors>)
        End Sub

        <Fact>
        Public Sub VbCoreNamespaceAndUserTypeConflict()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
<compilation>
    <file name="a.vb">
Imports System
Imports Microsoft.VisualBasic

Namespace Global.Microsoft
    Friend Class VisualBasic
        Public Property A As String
    End Class
End Namespace
    </file>
</compilation>,
            references:={SystemRef, SystemCoreRef},
            options:=TestOptions.ReleaseDll.WithEmbedVbCoreRuntime(True))

            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
BC31210: class 'VisualBasic' conflicts with a Visual Basic Runtime namespace 'VisualBasic'.
    Friend Class VisualBasic
                 ~~~~~~~~~~~
</errors>)
        End Sub

        <Fact>
        Public Sub VbCoreTypeAndUserNamespaceConflict()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
<compilation>
    <file name="a.vb">
Imports System
Imports Microsoft.VisualBasic

Namespace Global.Microsoft.VisualBasic.Strings
    Partial Friend Class VisualBasic
        Public Property A As String
    End Class
End Namespace
    </file>
</compilation>,
            references:={SystemRef, SystemCoreRef},
            options:=TestOptions.ReleaseDll.WithEmbedVbCoreRuntime(True))

            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
BC31210: namespace 'Strings' conflicts with a Visual Basic Runtime module 'Strings'.
Namespace Global.Microsoft.VisualBasic.Strings
                                       ~~~~~~~
</errors>)
        End Sub

        <Fact>
        Public Sub VbCoreTypeAndUserNamespaceConflict2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
<compilation>
    <file name="a.vb">
Imports System
Imports Microsoft.VisualBasic

Module Module1
    Sub Main()
        Call Console.WriteLine(GetType(Strings).ToString())
    End Sub
End Module

Namespace Global.Microsoft.VisualBasic.Strings
    Partial Friend Class VisualBasic
        Public Property A As String
    End Class
End Namespace
    </file>
</compilation>,
            references:={SystemRef, SystemCoreRef},
            options:=TestOptions.ReleaseDll.WithEmbedVbCoreRuntime(True))

            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
BC30560: 'Strings' is ambiguous in the namespace 'Microsoft.VisualBasic'.
        Call Console.WriteLine(GetType(Strings).ToString())
                                       ~~~~~~~
BC31210: namespace 'Strings' conflicts with a Visual Basic Runtime module 'Strings'.
Namespace Global.Microsoft.VisualBasic.Strings
                                       ~~~~~~~
</errors>)
        End Sub

        <Fact>
        Public Sub VbCoreTypeAndUserNamespaceConflict3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
<compilation>
    <file name="a.vb">
Imports System
Imports Microsoft.VisualBasic

Module Module1
    Sub Main()
    End Sub
End Module

Namespace Global.Microsoft.VisualBasic.Strings
    Partial Friend Class VisualBasic
        Public Property A As String
    End Class
End Namespace
    </file>
    <file name="b.vb">
Imports System
Namespace Global.Microsoft
    Namespace VisualBasic
        Namespace Strings
            Partial Friend Class Other
            End Class
        End Namespace
    End Namespace
End Namespace
    </file>
</compilation>,
            references:={SystemRef, SystemCoreRef},
            options:=TestOptions.ReleaseDll.WithEmbedVbCoreRuntime(True))

            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
BC31210: namespace 'Strings' conflicts with a Visual Basic Runtime module 'Strings'.
Namespace Global.Microsoft.VisualBasic.Strings
                                       ~~~~~~~
</errors>)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:="https://github.com/dotnet/roslyn/issues/29531")>
        Public Sub VbRuntimeTypeAndUserNamespaceConflictOutsideOfVBCore()
            ' This verifies the diagnostic BC31210 scenario outsides of using VB Core which
            ' is triggered by the Embedded Attribute.  This occurs on the command line compilers
            ' when the reference to system.xml.linq is added

            Dim compilationOptions = TestOptions.ReleaseExe.WithGlobalImports(GlobalImport.Parse({"System", "Microsoft.VisualBasic"}))

            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="testa.vb">
Imports Microsoft.VisualBasic
Module Module1
    Sub Main()
        Dim x1 = Microsoft.VisualBasic.InStr("abcd", 1) 
        Dim x2 = &lt;test/&gt;
   End Sub
End Module
 
Namespace global.Microsoft
    Public Module VisualBasic  
    End Module
End Namespace
    </file>
</compilation>,
            options:=compilationOptions,
references:={SystemCoreRef, SystemXmlLinqRef, SystemXmlRef})

            CompilationUtils.AssertTheseDiagnostics(compilation1,
            <errors>BC30560: Error in project-level import 'Microsoft.VisualBasic' at 'Microsoft.VisualBasic' : 'VisualBasic' is ambiguous in the namespace 'Microsoft'.
BC30560: 'VisualBasic' is ambiguous in the namespace 'Microsoft'.
BC30560: 'VisualBasic' is ambiguous in the namespace 'Microsoft'.
BC30560: 'VisualBasic' is ambiguous in the namespace 'Microsoft'.
Imports Microsoft.VisualBasic
        ~~~~~~~~~~~~~~~~~~~~~
BC30560: 'VisualBasic' is ambiguous in the namespace 'Microsoft'.
        Dim x1 = Microsoft.VisualBasic.InStr("abcd", 1) 
                 ~~~~~~~~~~~~~~~~~~~~~
BC31210: module 'VisualBasic' conflicts with a Visual Basic Runtime namespace 'VisualBasic'.
    Public Module VisualBasic  
                  ~~~~~~~~~~~

</errors>)

            ' Remove the reference to System.XML.Linq and verify compilation behavior that the 
            ' diagnostic is not produced.
            compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="testa.vb">
                        Imports Microsoft.VisualBasic
                        Module Module1
                            Sub Main()
                                Dim x1 = Microsoft.VisualBasic.InStr("abcd", 1) 
                           End Sub
                        End Module

                        Namespace global.Microsoft
                            Public Module VisualBasic  
                            End Module
                        End Namespace
                            </file>
</compilation>,
  options:=compilationOptions)

            CompilationUtils.AssertTheseDiagnostics(compilation1,
            <errors>BC30456: 'InStr' is not a member of 'VisualBasic'.
                                Dim x1 = Microsoft.VisualBasic.InStr("abcd", 1) 
                                         ~~~~~~~~~~~~~~~~~~~~~~~~~~~

                        </errors>)
        End Sub


        <Fact>
        Public Sub VbCore_IsImplicitlyDeclaredSymbols()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
<compilation>
    <file name="a.vb">
    </file>
</compilation>,
            references:={SystemRef, SystemCoreRef},
            options:=TestOptions.ReleaseDll.WithEmbedVbCoreRuntime(True))

            CompilationUtils.AssertTheseDiagnostics(compilation, <errors></errors>)

            Dim vbCoreType = compilation.GetTypeByMetadataName("Microsoft.VisualBasic.Embedded")
            Assert.NotNull(vbCoreType)

            Dim namespacesToCheck As New Queue(Of NamespaceSymbol)()
            namespacesToCheck.Enqueue(vbCoreType.ContainingNamespace)
            While namespacesToCheck.Count > 0
                Dim ns = namespacesToCheck.Dequeue()
                For Each member In ns.GetMembers()
                    Select Case member.Kind
                        Case SymbolKind.NamedType
                            AssertTypeAndItsMembersAreImplicitlyDeclared(DirectCast(member, NamedTypeSymbol))
                        Case SymbolKind.Namespace
                            namespacesToCheck.Enqueue(DirectCast(member, NamespaceSymbol))
                    End Select
                Next
            End While
        End Sub

        <Fact()>
        Public Sub InternalXmlHelper_IsImplicitlyDeclaredSymbols()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Module M
    Dim x = <x/>.<y>.Value
End Module
    ]]></file>
</compilation>, references:=XmlReferences)
            compilation.AssertNoErrors()
            Dim type = compilation.GetTypeByMetadataName("My.InternalXmlHelper")
            AssertTypeAndItsMembersAreImplicitlyDeclared(type)
        End Sub

        Private Sub IsImplicitlyDeclaredSymbols([namespace] As NamespaceSymbol)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")> <Fact>
        Public Sub VbCoreWithStaticLocals_UsingEmbedVBCore()
            'Static Locals use types contained within VB Runtime so verify with VBCore option to ensure the feature works
            'using VBCore which would be the case with platforms such as Phone.
            Dim compilation As VisualBasicCompilation = CompilationUtils.CreateEmptyCompilationWithReferences(
      <compilation>
          <file name="a.vb">
              Imports System
        Imports Microsoft.VisualBasic

        Module Module1
            Sub Main()
                Goo()
                Goo()
            End Sub

            Sub Goo()
                Static x as integer = 1
                x+=1
            End Sub
        End Module
          </file>
      </compilation>,
              references:=NoVbRuntimeReferences,
              options:=TestOptions.ReleaseDll.WithEmbedVbCoreRuntime(True))

            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")> <Fact>
        Public Sub VbCoreWithStaticLocals_NoRequiredTypes()
            'Static Locals use types in VB Runtime so verify with no VBRuntime we generate applicable errors about missing types.  
            'This will include types for Module as well as static locals
            Dim compilation As VisualBasicCompilation = CompilationUtils.CreateEmptyCompilationWithReferences(
      <compilation>
          <file name="a.vb">
        Module Module1
            Sub Main()
                Goo()
                Goo()
            End Sub

            Sub Goo()
                Static x as integer = 1
                x+=1
            End Sub
        End Module
          </file>
      </compilation>,
              references:=NoVbRuntimeReferences,
              options:=TestOptions.ReleaseDll.WithEmbedVbCoreRuntime(False))

            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_MissingRuntimeHelper, "Module1").WithArguments("Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute..ctor"))

        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")> <Fact>
        Public Sub VbCoreWithStaticLocals_CorrectDefinedTypes()
            'Static Locals use types in VB Runtime so verify with no VBRuntime but appropriate types specified in Source the static
            'local scenarios should work correctly.
            Dim compilation As VisualBasicCompilation = CompilationUtils.CreateEmptyCompilationWithReferences(
      <compilation>
          <file name="a.vb">
        Imports System
        Imports Microsoft.VisualBasic

        Public Class Module1
            Public shared Sub Main()
                Goo()
                Goo()
            End Sub

            shared Sub Goo()
                Static x as integer = 1
                x+=1
            End Sub
        End Class

Namespace Global.Microsoft.VisualBasic.CompilerServices
        Friend Class StaticLocalInitFlag
            Public State As Short
        End Class

        Friend Class IncompleteInitialization
            Inherits System.Exception
            Public Sub New()
                MyBase.New()
            End Sub
        End Class
End Namespace
          </file>
      </compilation>,
              references:=NoVbRuntimeReferences,
              options:=TestOptions.ReleaseDll.WithEmbedVbCoreRuntime(False))

            compilation.AssertNoDiagnostics()
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")> <Fact>
        Public Sub VbCoreWithStaticLocals_IncorrectDefinedTypes()
            'Static Locals use types in VB Runtime so verify with no VBRuntime but appropriate types specified in Source the static
            'local scenarios should work correctly but if we define the types incorrectly we should generate errors although we 
            'should not crash.
            Dim compilation As VisualBasicCompilation = CompilationUtils.CreateEmptyCompilationWithReferences(
      <compilation>
          <file name="a.vb">
        Public Class Module1
            Public shared Sub Main()
                Goo()
                Goo()
            End Sub

            shared Sub Goo()
                Static x as integer = 1
                x+=1
            End Sub
        End Class

Namespace Global.Microsoft.VisualBasic.CompilerServices
        Friend Class StaticLocalInitFlag
            Public State As Short
        End Class

        Friend Structure IncompleteInitialization
            Inherits System.Exception
            Public Sub New()
                MyBase.New()
            End Sub
        End Structure
End Namespace
          </file>
      </compilation>,
              references:=NoVbRuntimeReferences,
              options:=TestOptions.ReleaseDll.WithEmbedVbCoreRuntime(False))

            compilation.VerifyDiagnostics(
    Diagnostic(ERRID.ERR_NewInStruct, "New").WithLocation(20, 24),
    Diagnostic(ERRID.ERR_StructCantInherit, "Inherits System.Exception").WithLocation(19, 13),
    Diagnostic(ERRID.ERR_UseOfKeywordFromStructure1, "MyBase").WithArguments("MyBase").WithLocation(21, 17)
                                    )
        End Sub

        Private Sub AssertTypeAndItsMembersAreImplicitlyDeclared(type As NamedTypeSymbol)
            Assert.True(type.IsImplicitlyDeclared)
            Assert.True(type.IsEmbedded)

            For Each member In type.GetMembers()
                Assert.True(member.IsEmbedded)
                Assert.True(member.IsImplicitlyDeclared)

                Select Case member.Kind
                    Case SymbolKind.Field,
                        SymbolKind.Property

                    Case SymbolKind.Method
                        For Each param In DirectCast(member, MethodSymbol).Parameters
                            Assert.True(param.IsEmbedded)
                            Assert.True(param.IsImplicitlyDeclared)
                        Next

                        For Each typeParam In DirectCast(member, MethodSymbol).TypeParameters
                            Assert.True(typeParam.IsEmbedded)
                            Assert.True(typeParam.IsImplicitlyDeclared)
                        Next

                    Case SymbolKind.NamedType
                        AssertTypeAndItsMembersAreImplicitlyDeclared(DirectCast(member, NamedTypeSymbol))

                    Case Else
                        Assert.False(True) ' Unexpected member.

                End Select
            Next
        End Sub

        <Fact, WorkItem(544291, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544291")>
        Public Sub VbCoreSyncLockOnObject()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Module Module1
    Private SyncObj As Object = New Object()
    Sub Main()
        SyncLock SyncObj
        End SyncLock
    End Sub
End Module
    </file>
</compilation>).VerifyIL("Module1.Main", <![CDATA[
{
  // Code size       29 (0x1d)
  .maxstack  2
  .locals init (Object V_0,
  Boolean V_1)
  IL_0000:  ldsfld     "Module1.SyncObj As Object"
  IL_0005:  stloc.0
  IL_0006:  ldc.i4.0
  IL_0007:  stloc.1
  .try
{
  IL_0008:  ldloc.0
  IL_0009:  ldloca.s   V_1
  IL_000b:  call       "Sub System.Threading.Monitor.Enter(Object, ByRef Boolean)"
  IL_0010:  leave.s    IL_001c
}
  finally
{
  IL_0012:  ldloc.1
  IL_0013:  brfalse.s  IL_001b
  IL_0015:  ldloc.0
  IL_0016:  call       "Sub System.Threading.Monitor.Exit(Object)"
  IL_001b:  endfinally
}
  IL_001c:  ret
}
]]>)
        End Sub

        <Fact(), WorkItem(545772, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545772")>
        Public Sub VbCoreNoStdLib()
            Dim source =
<compilation>
    <file name="a.vb">
Module Class1
        Public Sub Main()
        End Sub
End Module
    </file>
</compilation>

            CreateCompilationWithMscorlib40(
                source,
                options:=TestOptions.ReleaseExe.WithEmbedVbCoreRuntime(True)).
            VerifyDiagnostics(
                Diagnostic(ERRID.ERR_UndefinedType1).WithArguments("Global.System.ComponentModel.EditorBrowsable"),
                Diagnostic(ERRID.ERR_NameNotMember2).WithArguments("ComponentModel", "System"),
                Diagnostic(ERRID.ERR_UndefinedType1).WithArguments("Global.System.ComponentModel.EditorBrowsable"),
                Diagnostic(ERRID.ERR_NameNotMember2).WithArguments("ComponentModel", "System"),
                Diagnostic(ERRID.ERR_UndefinedType1).WithArguments("Global.System.ComponentModel.EditorBrowsable"),
                Diagnostic(ERRID.ERR_NameNotMember2).WithArguments("ComponentModel", "System"),
                Diagnostic(ERRID.ERR_UndefinedType1).WithArguments("Global.System.ComponentModel.EditorBrowsable"),
                Diagnostic(ERRID.ERR_NameNotMember2).WithArguments("ComponentModel", "System"),
                Diagnostic(ERRID.ERR_UndefinedType1).WithArguments("Global.System.ComponentModel.EditorBrowsable"),
                Diagnostic(ERRID.ERR_NameNotMember2).WithArguments("ComponentModel", "System"),
                Diagnostic(ERRID.ERR_UndefinedType1).WithArguments("Global.System.ComponentModel.EditorBrowsable"),
                Diagnostic(ERRID.ERR_NameNotMember2).WithArguments("ComponentModel", "System"),
                Diagnostic(ERRID.ERR_UndefinedType1).WithArguments("Global.System.ComponentModel.EditorBrowsable"),
                Diagnostic(ERRID.ERR_NameNotMember2).WithArguments("ComponentModel", "System"),
                Diagnostic(ERRID.ERR_UndefinedType1).WithArguments("Global.System.ComponentModel.EditorBrowsable"),
                Diagnostic(ERRID.ERR_NameNotMember2).WithArguments("ComponentModel", "System"),
                Diagnostic(ERRID.ERR_UndefinedType1).WithArguments("Global.System.ComponentModel.EditorBrowsable"),
                Diagnostic(ERRID.ERR_NameNotMember2).WithArguments("ComponentModel", "System"),
                Diagnostic(ERRID.ERR_UndefinedType1).WithArguments("Global.System.ComponentModel.EditorBrowsable"),
                Diagnostic(ERRID.ERR_NameNotMember2).WithArguments("ComponentModel", "System"),
                Diagnostic(ERRID.ERR_UndefinedType1).WithArguments("Global.System.ComponentModel.EditorBrowsable"),
                Diagnostic(ERRID.ERR_NameNotMember2).WithArguments("ComponentModel", "System"),
                Diagnostic(ERRID.ERR_UndefinedType1).WithArguments("Global.System.ComponentModel.EditorBrowsable"),
                Diagnostic(ERRID.ERR_NameNotMember2).WithArguments("ComponentModel", "System"),
                Diagnostic(ERRID.ERR_UndefinedType1).WithArguments("Global.System.ComponentModel.EditorBrowsable"),
                Diagnostic(ERRID.ERR_NameNotMember2).WithArguments("ComponentModel", "System"))
        End Sub

#Region "Symbols Validator"

        Private Shared ReadOnly s_normalizeRegex As New Regex("^(\s*)", RegexOptions.Multiline)

        Private Sub ValidateSourceSymbols([module] As ModuleSymbol)
            ValidateSourceSymbol([module].GlobalNamespace)
        End Sub

        Private Sub ValidateSourceSymbol(symbol As Symbol)
            For Each reference In symbol.DeclaringSyntaxReferences
                Assert.False(reference.SyntaxTree.IsEmbeddedOrMyTemplateTree())
            Next

            Select Case symbol.Kind

                Case SymbolKind.Namespace
                    Dim [namespace] = DirectCast(symbol, NamespaceSymbol)
                    For Each _type In From x In [namespace].GetTypeMembers()
                                      Select x
                                      Order By x.Name.ToLower()

                        ValidateSourceSymbol(_type)
                    Next
                    For Each _ns In From x In [namespace].GetNamespaceMembers()
                                    Select x
                                    Order By x.Name.ToLower()

                        ValidateSourceSymbol(_ns)
                    Next

                Case SymbolKind.NamedType
                    Dim type = DirectCast(symbol, NamedTypeSymbol)
                    For Each _member In From x In type.GetMembers()
                                        Where x.Kind <> SymbolKind.NamedType
                                        Select x
                                        Order By x.ToTestDisplayString()

                        ValidateSourceSymbol(_member)
                    Next

                    For Each _nested In From x In type.GetTypeMembers()
                                        Select x
                                        Order By x.Name.ToLower()

                        ValidateSourceSymbol(_nested)
                    Next

            End Select

        End Sub

        Private Sub ValidateSymbols([module] As ModuleSymbol, expected As String)

            Dim actualBuilder As New StringBuilder
            CollectAllTypesAndMembers([module].GlobalNamespace, actualBuilder, "")

            expected = expected.Trim()

            ' normalize
            Dim matches = s_normalizeRegex.Matches(expected)
            Dim captures = matches(matches.Count - 1).Groups(1).Captures
            Dim indent = captures(captures.Count - 1).Value
            If indent.Length > 0 Then
                expected = New Regex("^" + indent, RegexOptions.Multiline).Replace(expected, "")
            End If

            Dim actual = actualBuilder.ToString.Trim()

            If expected.Replace(vbLf, Environment.NewLine).CompareTo(actual) <> 0 Then
                Console.WriteLine("Actual:")
                Console.WriteLine(actual)
                Console.WriteLine()
                Console.WriteLine("Diff:")
                Console.WriteLine(DiffUtil.DiffReport(expected, actual))
                Console.WriteLine()
                Assert.True(False)
            End If
        End Sub

        Private Sub AddSymbolAttributes(symbol As Symbol, builder As StringBuilder, indent As String)
            For Each attribute In symbol.GetAttributes()
                builder.AppendLine(indent + "[" + attribute.AttributeClass.ToTestDisplayString() + "]")
            Next
        End Sub

        Private Sub CollectAllTypesAndMembers(symbol As Symbol, builder As StringBuilder, indent As String)
            Const IndentStep = "  "

            Select Case symbol.Kind

                Case SymbolKind.Namespace
                    Dim [namespace] = DirectCast(symbol, NamespaceSymbol)
                    builder.AppendLine(indent + "Namespace " + symbol.ToTestDisplayString)

                    For Each _type In From x In [namespace].GetTypeMembers()
                                      Select x
                                      Order By x.Name.ToLower()

                        CollectAllTypesAndMembers(_type, builder, indent + IndentStep)
                    Next
                    For Each _ns In From x In [namespace].GetNamespaceMembers()
                                    Select x
                                    Order By x.Name.ToLower()

                        CollectAllTypesAndMembers(_ns, builder, indent + IndentStep)
                    Next

                    builder.AppendLine(indent + "End Namespace")

                Case SymbolKind.NamedType
                    If symbol.Name <> "<Module>" Then
                        AddSymbolAttributes(symbol, builder, indent)

                        Dim type = DirectCast(symbol, NamedTypeSymbol)
                        builder.AppendLine(indent + type.TypeKind.ToString() + " " + symbol.ToTestDisplayString)

                        For Each _member In From x In type.GetMembers()
                                            Where x.Kind <> SymbolKind.NamedType
                                            Select x
                                            Order By x.ToTestDisplayString()

                            AddSymbolAttributes(_member, builder, indent + IndentStep + "   ")
                            builder.AppendLine(indent + IndentStep + ">  " + _member.ToTestDisplayString())
                        Next

                        For Each _nested In From x In type.GetTypeMembers()
                                            Select x
                                            Order By x.Name.ToLower()

                            CollectAllTypesAndMembers(_nested, builder, indent + IndentStep)
                        Next

                        builder.AppendLine(indent + "End " + type.TypeKind.ToString())
                    End If

            End Select
        End Sub

#End Region

#Region "Utilities"

        Protected NoVbRuntimeReferences As MetadataReference() = {MscorlibRef, SystemRef, SystemCoreRef}

        Friend Shadows Function CompileAndVerify(
            source As XElement,
            Optional expectedOutput As String = Nothing,
            Optional sourceSymbolValidator As Action(Of ModuleSymbol) = Nothing,
            Optional validator As Action(Of PEAssembly) = Nothing,
            Optional symbolValidator As Action(Of ModuleSymbol) = Nothing
        ) As CompilationVerifier

            Dim options = If(expectedOutput IsNot Nothing, TestOptions.ReleaseExe, TestOptions.ReleaseDll).
                WithMetadataImportOptions(MetadataImportOptions.Internal).
                WithEmbedVbCoreRuntime(True)

            Return MyBase.CompileAndVerify(source:=source,
                                           allReferences:=NoVbRuntimeReferences,
                                           expectedOutput:=expectedOutput,
                                           sourceSymbolValidator:=sourceSymbolValidator,
                                           validator:=validator,
                                           symbolValidator:=symbolValidator,
                                           options:=options)
        End Function

#End Region

    End Class

End Namespace
