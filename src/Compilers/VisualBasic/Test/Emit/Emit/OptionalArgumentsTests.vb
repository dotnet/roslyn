' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.IO
Imports System.Linq
Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Emit
    Public Class OptionalArgumentsTests
        Inherits BasicTestBase

        Private ReadOnly _librarySource As XElement =
            <compilation>
                <file name="library.vb">
                    <![CDATA[
Imports System
Imports System.Runtime.InteropServices
Imports System.Runtime.CompilerServices
Imports System.Reflection

Public Class CBase
    Public Overridable Sub SubWithOptionalInteger(Optional i As Integer = 13)
    End Sub
End Class

Public Module Library
    Dim cul = System.Globalization.CultureInfo.InvariantCulture
    Sub DumpMetadata(a As [Assembly], memberName As String)
        If a Is Nothing Then
            a = Assembly.GetExecutingAssembly()
        End If

        Dim t As Type
        For Each t In a.GetExportedTypes()

            ' For each type, show its members & their custom attributes.
            For Each mi In t.GetMembers()
                If memberName Is Nothing OrElse mi.Name = memberName Then
                    ' If the member is a method, display information about its parameters.
                    If mi.MemberType = MemberTypes.Method Then
                        Console.WriteLine("Member: {0}", mi.Name)
                        For Each pi In CType(mi, MethodInfo).GetParameters()
                            Dim defval As Object = pi.DefaultValue
                            If pi.DefaultValue Is Nothing Then
                                defval = "No Default"
                            Else
                                Dim vType = pi.DefaultValue.GetType()
                                If vType Is GetType(DateTime) Then
                                    defval = DirectCast(pi.DefaultValue, DateTime).ToString("M/d/yyyy h:mm:ss tt", cul)
                                ElseIf vType Is GetType(Single) Then
                                    defval = DirectCast(pi.DefaultValue, Single).ToString(cul)
                                ElseIf vType Is GetType(Double) Then
                                    defval = DirectCast(pi.DefaultValue, Double).ToString(cul)
                                ElseIf vType Is GetType(Decimal) Then
                                    defval = DirectCast(pi.DefaultValue, Decimal).ToString(cul)
                                End If
                            End If
                            Console.WriteLine("Parameter: Type={0}, Name={1}, Optional={2}, DefaultValue={3}", pi.ParameterType, pi.Name, pi.IsOptional, defval)
                            DisplayAttributes(pi.GetCustomAttributes(False))
                        Next
                        If memberName IsNot Nothing Then
                            Exit For
                        End If
                    End If
                End If
            Next
        Next
    End Sub

    Sub DisplayAttributes(attrs() As Object)
        If attrs.Length = 0 Then Return

        ' Display the custom attributes applied to this member.
        Dim o As Object
        For Each o In attrs
            Dim dateTimeAttribute = TryCast(o, DateTimeConstantAttribute)
            Dim decimalAttribute = TryCast(o, DecimalConstantAttribute)

            If dateTimeAttribute IsNot Nothing Then
                Console.WriteLine("Attribute: {0}({1})", o.ToString(), DirectCast(dateTimeAttribute.Value, DateTime).ToString("M/d/yyyy h:mm:ss tt", cul))
            ElseIf decimalAttribute IsNot Nothing Then
                Console.WriteLine("Attribute: {0}({1})", o.ToString(), decimalAttribute.Value.ToString(cul))
            End If

        Next
    End Sub

    ' 634631328000000000 = #1/26/2012#.Ticks. Optional pseudo attribute is missing.
    Sub DateTimeUsingConstantAttribute(<DateTimeConstantAttribute(634631328000000000)> i As DateTime)
        Console.WriteLine(i.ToString("M/d/yyyy h:mm:ss tt", cul))
    End Sub

    Sub DateTimeUsingOptionalAttribute(<[Optional]()> i As DateTime)
        Console.WriteLine(i.ToString("M/d/yyyy h:mm:ss tt", cul))
    End Sub

    ' Optional and default value specified with attributes.  This should work when called from another assembly
    Sub DateTimeUsingOptionalAndConstantAttributes(<[Optional]()> <DateTimeConstantAttribute(634631328000000000)> i As DateTime)
        Console.WriteLine(i.ToString("M/d/yyyy h:mm:ss tt", cul))
    End Sub

    ' Optional and default value specified with attributes.  This should work when called from another assembly
    Sub DecimalUsingOptionalAndConstantAttributes(<[Optional]()> <DecimalConstant(2, 0, 0, 0, 99999)> i As Decimal)
        Console.WriteLine(i.ToString(cul))
    End Sub

    Sub IntegerUsingOptionalAttribute(<[Optional]()> i As Integer)
        Console.WriteLine(i)
    End Sub

    Sub StringUsingOptionalAttribute(<[Optional]()> i As String)
        Console.WriteLine(i IsNot Nothing)
    End Sub

    ' DateTime constant with a string parameter.
    ' Valid to call with strict off. Error with strict on
    Sub StringWithOptionalDateTimeValue(<[Optional]()> <DateTimeConstantAttribute(634631328000000000)> i As String)
        Console.WriteLine(i)
    End Sub

    ' DateTime constant with a integer parameter.
    ' Always an error
    Sub IntegerWithDateTimeOptionalValue(<[Optional]()> <DateTimeConstantAttribute(634631328000000000)> i As Integer)
        Console.WriteLine(i.ToString("M/d/yyyy h:mm:ss tt", cul))
    End Sub

    ' Property with optional parameter
    Property PropertyIntegerOptionalDouble(i As Integer, Optional j As Double = 100)
        Get
            Return j
        End Get
        Set(ByVal value)
        End Set
    End Property

    Public Enum Animal
        Dog
        Cat
        Fish
    End Enum

    Sub TestWithMultipleOptionalEnumValues(Optional e1 As Animal = Animal.Dog, Optional e2 As Animal = Animal.Cat)
    End Sub

End Module
]]></file>
            </compilation>

        Private ReadOnly _classLibrary As MetadataReference = CreateHelperLibrary(_librarySource.Value)

        Public Function CreateHelperLibrary(source As String) As MetadataReference
            Dim libraryCompilation = VisualBasicCompilation.Create("library",
                                                        {VisualBasicSyntaxTree.ParseText(source)},
                                                        {MsvbRef, MscorlibRef, SystemCoreRef},
                                                        TestOptions.ReleaseDll)

            Return MetadataReference.CreateFromImage(libraryCompilation.EmitToArray())
        End Function

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28046")>
        Public Sub TestOptionalInteger()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

Public Class C1
    Shared Sub OptionalArg(Optional i As Integer = 1)
        Console.WriteLine("i = {0}", i)
    End Sub
End Class

Module Module1
    Sub Main()
        Console.WriteLine()
        C1.OptionalArg()
        DumpMetadata(Assembly.GetExecutingAssembly(), "OptionalArg")
    End Sub
End Module
]]></file>
</compilation>
            CompileAndVerify(source,
                             references:={_classLibrary},
                             expectedOutput:=<![CDATA[
i = 1
Member: OptionalArg
Parameter: Type=System.Int32, Name=i, Optional=True, DefaultValue=1
]]>)
        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28046")>
        Public Sub TestIntegerOptionalAttribute()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

Public Class C1
    Shared Sub OptionalArg(<[Optional]()> i As Integer)
    End Sub
End Class

Module Module1
    Sub Main()
        Console.WriteLine()
        DumpMetadata(Assembly.GetExecutingAssembly(), "OptionalArg")
    End Sub
End Module
]]>
    </file>
</compilation>

            CompileAndVerify(source,
                             references:={_classLibrary},
                             expectedOutput:=<![CDATA[
Member: OptionalArg
Parameter: Type=System.Int32, Name=i, Optional=True, DefaultValue=System.Reflection.Missing
]]>)
        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28046")>
        Public Sub TestOptionalString()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

Public Class C1
    Shared Sub OptionalArg(Optional i As String = "hello world")
        Console.WriteLine("i = {0}", i)
    End Sub
End Class

Module Module1
    Sub Main()
        Console.WriteLine()
        C1.OptionalArg()
        DumpMetadata(Assembly.GetExecutingAssembly(), "OptionalArg")
    End Sub
End Module
]]>
    </file>
</compilation>

            CompileAndVerify(source,
                             references:={_classLibrary},
                             expectedOutput:=<![CDATA[
i = hello world
Member: OptionalArg
Parameter: Type=System.String, Name=i, Optional=True, DefaultValue=hello world
]]>)
        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28046")>
        Public Sub TestOptionalDateTime()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

Public Class C1
    Shared Sub OptionalArg(Optional i As DateTime = #1/26/2012#)
        Console.WriteLine("i = {0}", i.ToString("M/d/yyyy h:mm:ss tt", System.Globalization.CultureInfo.InvariantCulture))
    End Sub
End Class

Module Module1
    Sub Main()
        Console.WriteLine()
        C1.OptionalArg()
        DumpMetadata(Assembly.GetExecutingAssembly(), "OptionalArg")
    End Sub
End Module
]]>
    </file>
</compilation>

            CompileAndVerify(source,
                             references:={_classLibrary},
                             expectedOutput:=<![CDATA[
i = 1/26/2012 12:00:00 AM
Member: OptionalArg
Parameter: Type=System.DateTime, Name=i, Optional=True, DefaultValue=1/26/2012 12:00:00 AM
Attribute: System.Runtime.CompilerServices.DateTimeConstantAttribute(1/26/2012 12:00:00 AM)
]]>)
        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28046")>
        Public Sub TestOptionalDecimal()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

Public Class C1
    Shared Sub OptionalArg(Optional i As Decimal = 999.99D)
        Console.WriteLine("i = {0}", i.ToString(System.Globalization.CultureInfo.InvariantCulture))
    End Sub
End Class

Module Module1
    Sub Main()
        Console.WriteLine()
        C1.OptionalArg()
        DumpMetadata(Assembly.GetExecutingAssembly(), "OptionalArg")
    End Sub
End Module
]]>
    </file>
</compilation>

            CompileAndVerify(source,
                             references:={_classLibrary},
                             expectedOutput:=<![CDATA[
i = 999.99
Member: OptionalArg
Parameter: Type=System.Decimal, Name=i, Optional=True, DefaultValue=999.99
Attribute: System.Runtime.CompilerServices.DecimalConstantAttribute(999.99)
]]>)
        End Sub

        <WorkItem(543530, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543530")>
        <Fact()>
        Public Sub OptionalForConstructorofAttribute()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System
Class Base1
    Inherits Attribute
    Sub New(Optional x As System.Type = Nothing)
        Me.Result = If(x Is Nothing, "Nothing", x.ToString())
    End Sub
    Public Result As String
End Class

Class C1
    &lt;Base1()&gt;
    Shared Sub A()
    End Sub
    &lt;Base1(Nothing)&gt;
    Shared Sub B(name As String)
        Dim m = GetType(C1).GetMethod(name)
        Console.Write(DirectCast(m.GetCustomAttributes(GetType(Base1), False)(0), Base1).Result)
        Console.Write(";")
    End Sub
    &lt;Base1(GetType(C1))&gt;
    Shared Sub Main()
        B("A")
        B("B")
        B("Main")
    End Sub
End Class
    </file>
</compilation>

            CompileAndVerify(source,
                             references:={_classLibrary},
                             expectedOutput:="Nothing;Nothing;C1;")
        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28046")>
        Public Sub TestDateTimeConstantAttribute()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

Public Class C1
    ' 634631328000000000 = #1/26/2012#.Ticks
    Shared Sub OptionalArg(<DateTimeConstantAttribute(634631328000000000)> i As DateTime)
        Console.WriteLine(i)
    End Sub
End Class

Module Module1
    Sub Main()
        Console.WriteLine()
        DumpMetadata(Assembly.GetExecutingAssembly(), "OptionalArg")
    End Sub
End Module
]]>
    </file>
</compilation>

            CompileAndVerify(source,
                             references:={_classLibrary},
                             expectedOutput:=<![CDATA[
Member: OptionalArg
Parameter: Type=System.DateTime, Name=i, Optional=False, DefaultValue=1/26/2012 12:00:00 AM
Attribute: System.Runtime.CompilerServices.DateTimeConstantAttribute(1/26/2012 12:00:00 AM)
]]>)
        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28046")>
        Public Sub TestDateTimeOptionalAttributeConstantAttribute()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

Public Class C1
    ' 634631328000000000 = #1/26/2012#.Ticks
    Shared Sub OptionalArg(<[Optional]()> <DateTimeConstantAttribute(634631328000000000)> i As DateTime)
    End Sub
End Class

Module Module1
    Sub Main()
        Console.WriteLine()
        DumpMetadata(Assembly.GetExecutingAssembly(), "OptionalArg")
    End Sub
End Module
]]>
    </file>
</compilation>

            CompileAndVerify(source,
                             references:={_classLibrary},
                             expectedOutput:=<![CDATA[
Member: OptionalArg
Parameter: Type=System.DateTime, Name=i, Optional=True, DefaultValue=1/26/2012 12:00:00 AM
Attribute: System.Runtime.CompilerServices.DateTimeConstantAttribute(1/26/2012 12:00:00 AM)
]]>)
        End Sub

        <Fact()>
        Public Sub TestDateTimeMissingOptionalFromMetadata()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Module Module1
    Sub Main()
        DateTimeUsingConstantAttribute() ' This should error. Optional pseudo attribute is missing from metadata.
    End Sub
End Module
]]>
    </file>
</compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, references:={_classLibrary})
            comp.VerifyDiagnostics(Diagnostic(ERRID.ERR_OmittedArgument2, "DateTimeUsingConstantAttribute").WithArguments("i", "Public Sub DateTimeUsingConstantAttribute(i As Date)"))
        End Sub

        <Fact()>
        Public Sub TestDateTimeFromMetadataAttributes()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

Module Module1
    Sub Main()
        DateTimeUsingOptionalAndConstantAttributes() ' This should work both optional and datetimeconstant attributes are in metadata.
    End Sub
End Module
]]>
    </file>
</compilation>

            CompileAndVerify(source,
                             references:={_classLibrary},
                             expectedOutput:=<![CDATA[
   1/26/2012 12:00:00 AM
]]>)
        End Sub

        <Fact()>
        Public Sub TestDecimalFromMetadataAttributes()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

Module Module1
    Sub Main()
        DecimalUsingOptionalAndConstantAttributes() 
        ' Dev10 does not pick up the DecimalConstantAttribute because it uses the 
        ' Integer constructor instead of the UInteger constructor. 
        ' Roslyn honours both constructors.
    End Sub
End Module
]]>
    </file>
</compilation>

            CompileAndVerify(source,
                             references:={_classLibrary},
                             expectedOutput:=<![CDATA[
   999.99
]]>)
        End Sub

        <Fact()>
        Public Sub TestDateTimeFromMetadataOptionalAttributeOnly()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

Module Module1
    Sub Main()
        DateTimeUsingOptionalAttribute() ' Metadata only has the optional attribute.
    End Sub
End Module
]]>
    </file>
</compilation>

            CompileAndVerify(source,
                             references:={_classLibrary},
                             expectedOutput:=<![CDATA[
1/1/0001 12:00:00 AM
]]>)
        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28046")>
        Public Sub TestOptionalWithNothing()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

Class C1
End Class

Structure S1
    dim i as integer
End Structure

Module Module1
    Sub s1(optional s as byte = nothing)
        Console.WriteLine(s = nothing)
    end sub

    Sub s2(optional s as boolean = nothing)
        Console.WriteLine(s = nothing)
    end sub

    Sub s3(optional s as integer = nothing)
        Console.WriteLine(s = nothing)
    end sub

    Sub s4(optional s as long = nothing)
        Console.WriteLine(s = nothing)
    end sub

   Sub s5(optional s as double = nothing)
        Console.WriteLine(s = nothing)
    end sub

    Sub s6(optional s as datetime = nothing)
        Console.WriteLine(s = nothing)
    end sub

    Sub s7(optional s as decimal = nothing)
        Console.WriteLine(s = nothing)
    end sub

    Sub s8(optional s as string = nothing)
        Console.WriteLine(s = nothing)
    end Sub

    Sub s9(optional s as C1 = nothing)
        Console.WriteLine(s is nothing)
    end Sub

    Sub s10(optional s as S1 = nothing)
        dim t as S1 = nothing
        Console.WriteLine(s.Equals(t))
    end Sub

    Sub Main()
        s1()
        s2()
        s3()
        s4()
        s5()
        s6()
        s7()
        s8()
        s9()
        s10()
    End Sub
End Module
]]>
    </file>
</compilation>

            CompileAndVerify(source,
                             references:={_classLibrary},
                             expectedOutput:=<![CDATA[
True
True
True
True
True
True
True
True
True
True
]]>)
        End Sub

        <Fact()>
        Public Sub TestBadDefaultValue()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Module Module1
    Sub s(optional i as integer = p) ' p is undefined and will be a BadExpression.
    End Sub

    Sub Main()
        s() 
    End Sub
End Module
]]>
    </file>
</compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, references:={_classLibrary})
            comp.VerifyDiagnostics(Diagnostic(ERRID.ERR_NameNotDeclared1, "p").WithArguments("p"))
        End Sub

        <Fact()>
        Public Sub ParamArrayAndAttribute()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Interface I
    Sub M1(ParamArray o As Object())
    Sub M2(<[ParamArray]()> o As Object())
    Sub M3(<[ParamArray]()> ParamArray o As Object())
    Property P1(ParamArray o As Object())
    Property P2(<[ParamArray]()> o As Object())
    Property P3(<[ParamArray]()> ParamArray o As Object())
End Interface
]]>
    </file>
</compilation>)
            comp.AssertTheseDiagnostics(<errors/>)
            CompileAndVerify(comp, symbolValidator:=Sub([module])
                                                        Dim type = [module].GlobalNamespace.GetMember(Of NamedTypeSymbol)("I")
                                                        Assert.Equal(1, CountParamArrayAttributes(type.GetMember(Of MethodSymbol)("M1").Parameters(0)))
                                                        Assert.Equal(1, CountParamArrayAttributes(type.GetMember(Of MethodSymbol)("M2").Parameters(0)))
                                                        Assert.Equal(1, CountParamArrayAttributes(type.GetMember(Of MethodSymbol)("M3").Parameters(0)))
                                                        Assert.Equal(1, CountParamArrayAttributes(type.GetMember(Of PropertySymbol)("P1").Parameters(0)))
                                                        Assert.Equal(1, CountParamArrayAttributes(type.GetMember(Of MethodSymbol)("get_P1").Parameters(0)))
                                                        Assert.Equal(1, CountParamArrayAttributes(type.GetMember(Of MethodSymbol)("set_P1").Parameters(0)))
                                                        Assert.Equal(1, CountParamArrayAttributes(type.GetMember(Of PropertySymbol)("P2").Parameters(0)))
                                                        Assert.Equal(1, CountParamArrayAttributes(type.GetMember(Of MethodSymbol)("get_P2").Parameters(0)))
                                                        Assert.Equal(1, CountParamArrayAttributes(type.GetMember(Of MethodSymbol)("set_P2").Parameters(0)))
                                                        Assert.Equal(1, CountParamArrayAttributes(type.GetMember(Of PropertySymbol)("P3").Parameters(0)))
                                                        Assert.Equal(1, CountParamArrayAttributes(type.GetMember(Of MethodSymbol)("get_P3").Parameters(0)))
                                                        Assert.Equal(1, CountParamArrayAttributes(type.GetMember(Of MethodSymbol)("set_P3").Parameters(0)))
                                                    End Sub)
        End Sub

        Private Shared Function CountParamArrayAttributes(parameter As ParameterSymbol) As Integer
            Dim [module] = DirectCast(parameter.ContainingModule, PEModuleSymbol)
            Dim attributes = [module].GetCustomAttributesForToken(DirectCast(parameter, PEParameterSymbol).Handle)
            Return attributes.Where(Function(a) a.AttributeClass.Name = "ParamArrayAttribute").Count()
        End Function

        <WorkItem(529684, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529684")>
        <Fact()>
        Public Sub TestDuplicateConstantAttributesMetadata()
            Dim ilSource = <![CDATA[
.assembly extern System {}
.class public C
{
  .method public static object F0([opt] object o)
  {
    .param [1]
    .custom instance void [System]System.Runtime.InteropServices.DefaultParameterValueAttribute::.ctor(object) = {string('s')} // [DefaultParameterValue('s')]
    ldarg.0
    ret
  }
  .method public static object F1([opt] object o)
  {
    .param [1]
    .custom instance void [System]System.Runtime.InteropServices.DefaultParameterValueAttribute::.ctor(object) = {string('s')} // [DefaultParameterValue('s')]
    .custom instance void [System]System.Runtime.CompilerServices.DecimalConstantAttribute::.ctor(uint8, uint8, uint32, uint32, uint32) = ( 01 00 00 00 00 00 00 00 00 00 00 00 02 00 00 00 00 00 ) // [DecimalConstant(2)]
    .custom instance void [System]System.Runtime.CompilerServices.DateTimeConstantAttribute::.ctor(int64) = ( 01 00 03 00 00 00 00 00 00 00 00 00 ) // [DateTimeConstant(3)]
    ldarg.0
    ret
  }
  .method public static object F2([opt] object o)
  {
    .param [1]
    .custom instance void [System]System.Runtime.CompilerServices.DecimalConstantAttribute::.ctor(uint8, uint8, uint32, uint32, uint32) = ( 01 00 00 00 00 00 00 00 00 00 00 00 02 00 00 00 00 00 ) // [DecimalConstant(2)]
    .custom instance void [System]System.Runtime.CompilerServices.DateTimeConstantAttribute::.ctor(int64) = ( 01 00 03 00 00 00 00 00 00 00 00 00 ) // [DateTimeConstant(3)]
    .custom instance void [System]System.Runtime.InteropServices.DefaultParameterValueAttribute::.ctor(object) = {string('s')} // [DefaultParameterValue('s')]
    ldarg.0
    ret
  }
  .method public static object F3([opt] object o)
  {
    .param [1]
    .custom instance void [System]System.Runtime.CompilerServices.DateTimeConstantAttribute::.ctor(int64) = ( 01 00 03 00 00 00 00 00 00 00 00 00 ) // [DateTimeConstant(3)]
    .custom instance void [System]System.Runtime.InteropServices.DefaultParameterValueAttribute::.ctor(object) = {string('s')} // [DefaultParameterValue('s')]
    .custom instance void [System]System.Runtime.CompilerServices.DecimalConstantAttribute::.ctor(uint8, uint8, uint32, uint32, uint32) = ( 01 00 00 00 00 00 00 00 00 00 00 00 02 00 00 00 00 00 ) // [DecimalConstant(2)]
    ldarg.0
    ret
  }
  .method public static int32 F4([opt] int32 i)
  {
    .param [1]
    .custom instance void [System]System.Runtime.InteropServices.DefaultParameterValueAttribute::.ctor(object) = ( 01 00 08 01 00 00 00 00 00 ) // [DefaultParameterValue(1)]
    .custom instance void [System]System.Runtime.InteropServices.DefaultParameterValueAttribute::.ctor(object) = ( 01 00 08 02 00 00 00 00 00 ) // [DefaultParameterValue(2)]
    .custom instance void [System]System.Runtime.InteropServices.DefaultParameterValueAttribute::.ctor(object) = ( 01 00 08 03 00 00 00 00 00 ) // [DefaultParameterValue(3)]
    ldarg.0
    ret
  }
  .method public static valuetype [mscorlib]System.DateTime F5([opt] valuetype [mscorlib]System.DateTime d)
  {
    .param [1]
    .custom instance void [System]System.Runtime.CompilerServices.DateTimeConstantAttribute::.ctor(int64) = ( 01 00 01 00 00 00 00 00 00 00 00 00 ) // [DateTimeConstant(3)]
    .custom instance void [System]System.Runtime.CompilerServices.DateTimeConstantAttribute::.ctor(int64) = ( 01 00 02 00 00 00 00 00 00 00 00 00 ) // [DateTimeConstant(3)]
    .custom instance void [System]System.Runtime.CompilerServices.DateTimeConstantAttribute::.ctor(int64) = ( 01 00 03 00 00 00 00 00 00 00 00 00 ) // [DateTimeConstant(3)]
    ldarg.0
    ret
  }
  .method public static valuetype [mscorlib]System.Decimal F6([opt] valuetype [mscorlib]System.Decimal d)
  {
    .param [1]
    .custom instance void [System]System.Runtime.CompilerServices.DecimalConstantAttribute::.ctor(uint8, uint8, uint32, uint32, uint32) = ( 01 00 00 00 00 00 00 00 00 00 00 00 01 00 00 00 00 00 ) // [DecimalConstant(2)]
    .custom instance void [System]System.Runtime.CompilerServices.DecimalConstantAttribute::.ctor(uint8, uint8, uint32, uint32, uint32) = ( 01 00 00 00 00 00 00 00 00 00 00 00 02 00 00 00 00 00 ) // [DecimalConstant(2)]
    .custom instance void [System]System.Runtime.CompilerServices.DecimalConstantAttribute::.ctor(uint8, uint8, uint32, uint32, uint32) = ( 01 00 00 00 00 00 00 00 00 00 00 00 03 00 00 00 00 00 ) // [DecimalConstant(2)]
    ldarg.0
    ret
  }
}
]]>.Value
            Dim vbSource =
<compilation>
    <file name="c.vb"><![CDATA[
Class P
    Shared Sub Main()
        Report(C.F0())
        Report(C.F1())
        Report(C.F2())
        Report(C.F3())
        Report(C.F4())
        Report(C.F5().Ticks)
        Report(C.F6())
    End Sub
    Shared Sub Report(o As Object)
        Dim value As Object = If (TypeOf o is Date, DirectCast(o, Date).ToString("yyyy-MM-dd HH:mm:ss"), o)
        System.Console.WriteLine("{0}: {1}", o.GetType(), value)
    End Sub
End Class
]]>
    </file>
</compilation>
            Dim comp = CreateCompilationWithCustomILSource(vbSource, ilSource, options:=TestOptions.DebugExe)
            comp.AssertTheseDiagnostics(<errors/>)
            CompileAndVerify(comp, expectedOutput:=<![CDATA[
System.Reflection.Missing: System.Reflection.Missing
System.DateTime: 0001-01-01 00:00:00
System.DateTime: 0001-01-01 00:00:00
System.DateTime: 0001-01-01 00:00:00
System.Int32: 0
System.Int64: 3
System.Decimal: 3
]]>)
        End Sub

        <WorkItem(529684, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529684")>
        <Fact()>
        Public Sub TestDuplicateConstantAttributesSameValues()
            Dim source1 =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Public Class C
    Public Shared Function F1(Optional o As Integer = 1)
        Return o
    End Function
    Public Shared Function F2(<[Optional](), DefaultParameterValue(2)> o As Integer)
        Return o
    End Function
    Public Shared Function F3(<DefaultParameterValue(3)> Optional o As Integer = 3)
        Return o
    End Function
    Public Shared Function F4(Optional o As Decimal = 4)
        Return o
    End Function
    Public Shared Function F5(<[Optional](), DecimalConstant(0, 0, 0, 0, 5)> o As Decimal)
        Return o
    End Function
    Public Shared Function F6(<DecimalConstant(0, 0, 0, 0, 6)> Optional o As Decimal = 6)
        Return o
    End Function
    Public Shared Function F7(Optional o As DateTime = #7/24/2013#)
        Return o
    End Function
    Public Shared Function F8(<[Optional](), DateTimeConstant(635102208000000000)> o As DateTime)
        Return o
    End Function
    Public Shared Function F9(<DateTimeConstant(635102208000000000)> Optional o As DateTime = #7/24/2013#)
        Return o
    End Function
    Public Shared Property P(<DecimalConstant(0, 0, 0, 0, 10)> Optional o As Decimal = 10)
        Get
            Return o
        End Get
        Set
        End Set
    End Property
End Class
]]>
    </file>
</compilation>
            Dim comp1 = CreateCompilationWithMscorlib40AndVBRuntime(source1)
            comp1.AssertTheseDiagnostics(<errors/>)
            CompileAndVerify(comp1, symbolValidator:=Sub([module])
                                                         Dim type = [module].GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")
                                                         VerifyDefaultValueAttribute(type.GetMember(Of MethodSymbol)("F1").Parameters(0), Nothing, 1, True)
                                                         VerifyDefaultValueAttribute(type.GetMember(Of MethodSymbol)("F2").Parameters(0), "DefaultParameterValueAttribute", 2, False)
                                                         VerifyDefaultValueAttribute(type.GetMember(Of MethodSymbol)("F3").Parameters(0), "DefaultParameterValueAttribute", 3, True)
                                                         VerifyDefaultValueAttribute(type.GetMember(Of MethodSymbol)("F4").Parameters(0), "DecimalConstantAttribute", 4UI, False)
                                                         VerifyDefaultValueAttribute(type.GetMember(Of MethodSymbol)("F5").Parameters(0), "DecimalConstantAttribute", 5, False)
                                                         VerifyDefaultValueAttribute(type.GetMember(Of MethodSymbol)("F6").Parameters(0), "DecimalConstantAttribute", 6, False)
                                                         VerifyDefaultValueAttribute(type.GetMember(Of MethodSymbol)("F7").Parameters(0), "DateTimeConstantAttribute", 635102208000000000L, False)
                                                         VerifyDefaultValueAttribute(type.GetMember(Of MethodSymbol)("F8").Parameters(0), "DateTimeConstantAttribute", 635102208000000000L, False)
                                                         VerifyDefaultValueAttribute(type.GetMember(Of MethodSymbol)("F9").Parameters(0), "DateTimeConstantAttribute", 635102208000000000L, False)
                                                         VerifyDefaultValueAttribute(type.GetMember(Of PropertySymbol)("P").Parameters(0), "DecimalConstantAttribute", 10, False)
                                                         VerifyDefaultValueAttribute(type.GetMember(Of MethodSymbol)("get_P").Parameters(0), "DecimalConstantAttribute", 10, False)
                                                         VerifyDefaultValueAttribute(type.GetMember(Of MethodSymbol)("set_P").Parameters(0), "DecimalConstantAttribute", 10, False)
                                                     End Sub)
            Dim source2 =
<compilation>
    <file name="a.vb"><![CDATA[
Class P
    Shared Sub Main()
        Report(C.F1())
        Report(C.F2())
        Report(C.F3())
        Report(C.F4())
        Report(C.F5())
        Report(C.F6())
        Report(C.F7().Ticks)
        Report(C.F8().Ticks)
        Report(C.F9().Ticks)
        Report(C.P)
    End Sub
    Shared Sub Report(o As Object)
        System.Console.WriteLine(o)
    End Sub
End Class
]]>
    </file>
</compilation>
            Dim comp2a = CreateCompilationWithMscorlib40AndVBRuntime(
                source2,
                additionalRefs:={New VisualBasicCompilationReference(comp1)},
                options:=TestOptions.DebugExe)
            comp2a.AssertTheseDiagnostics(
<errors>
BC30455: Argument not specified for parameter 'o' of 'Public Shared Function F2(o As Integer) As Object'.
        Report(C.F2())
                 ~~
BC30455: Argument not specified for parameter 'o' of 'Public Shared Function F5(o As Decimal) As Object'.
        Report(C.F5())
                 ~~
BC30455: Argument not specified for parameter 'o' of 'Public Shared Function F8(o As Date) As Object'.
        Report(C.F8().Ticks)
                 ~~
</errors>)
            Dim comp2b = CreateCompilationWithMscorlib40AndVBRuntime(
                source2,
                additionalRefs:={MetadataReference.CreateFromImage(comp1.EmitToArray())},
                options:=TestOptions.DebugExe)
            comp2b.AssertTheseDiagnostics(<errors/>)
            CompileAndVerify(comp2b, expectedOutput:=
            <![CDATA[
1
0
3
4
5
6
635102208000000000
635102208000000000
635102208000000000
10
]]>)
        End Sub

        <WorkItem(529684, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529684")>
        <Fact()>
        Public Sub TestDuplicateConstantAttributesSameValues_PartialMethods()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Partial Class C
    Private Sub F1(<DefaultParameterValue(1)> Optional o As Integer = 1)
    End Sub
    Private Sub F2(Optional o As Decimal = 2)
    End Sub
    Private Partial Sub F3(<DateTimeConstant(635102208000000000)> Optional o As DateTime = #7/24/2013#)
    End Sub
End Class
Partial Class C
    Private Partial Sub F1(Optional o As Integer = 1)
    End Sub
    Private Partial Sub F2(<DecimalConstant(0, 0, 0, 0, 2)> Optional o As Decimal = 2)
    End Sub
    Private Sub F3(Optional o As DateTime = #7/24/2013#)
    End Sub
End Class
]]>
    </file>
</compilation>,
                options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All))

            CompileAndVerify(comp, symbolValidator:=Sub([module])
                                                        Dim type = [module].GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")
                                                        VerifyDefaultValueAttribute(type.GetMember(Of MethodSymbol)("F1").Parameters(0), "DefaultParameterValueAttribute", 1, True)
                                                        VerifyDefaultValueAttribute(type.GetMember(Of MethodSymbol)("F2").Parameters(0), "DecimalConstantAttribute", 2, False)
                                                        VerifyDefaultValueAttribute(type.GetMember(Of MethodSymbol)("F3").Parameters(0), "DateTimeConstantAttribute", 635102208000000000L, False)
                                                    End Sub)
        End Sub

        Private Shared Sub VerifyDefaultValueAttribute(parameter As ParameterSymbol, expectedAttributeName As String, expectedDefault As Object, hasDefault As Boolean)
            Dim attributes = DirectCast(parameter.ContainingModule, PEModuleSymbol).
                GetCustomAttributesForToken(DirectCast(parameter, PEParameterSymbol).Handle).
                Where(Function(attr) attr.AttributeClass.Name = expectedAttributeName).
                ToArray()

            If expectedAttributeName Is Nothing Then
                Assert.Equal(attributes.Length, 0)
            Else
                Assert.Equal(attributes.Length, 1)
                Dim attribute = DirectCast(attributes(0), VisualBasicAttributeData)
                Dim argument = attribute.ConstructorArguments.Last()
                Assert.Equal(expectedDefault, argument.Value)
            End If
            If hasDefault Then
                Assert.Equal(expectedDefault, parameter.ExplicitDefaultValue)
            End If
        End Sub

        <WorkItem(529684, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529684")>
        <Fact()>
        Public Sub TestDuplicateConstantAttributesDifferentValues()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Interface I
    Sub F1(<DefaultParameterValue(1)> Optional o As Integer = 2)
    Sub F2(<DefaultParameterValue(1)> Optional o As Decimal = 2)
    Sub F3(<DefaultParameterValue(0)> Optional o As DateTime = #7/24/2013#)
    Sub F4(<DecimalConstant(0, 0, 0, 0, 1)> Optional o As Decimal = 2)
    Sub F5(<DateTimeConstant(0)> Optional o As DateTime = #7/24/2013#)
    Sub F6(<DefaultParameterValue(1), DateTimeConstant(1), DecimalConstant(0, 0, 0, 0, 1)> Optional o As Integer = 1)
    Sub F7(<DateTimeConstant(2), DecimalConstant(0, 0, 0, 0, 2), DefaultParameterValue(2)> Optional o As Decimal = 2)
    Sub F8(<DecimalConstant(0, 0, 0, 0, 3), DateTimeConstant(3), DefaultParameterValue(3)> Optional o As DateTime = #1/1/2000#)
    Property P(<DefaultParameterValue(1)> Optional a As Integer = 2,
        <DefaultParameterValue(3)> Optional b As Decimal = 4,
        <DefaultParameterValue(5)> Optional c As DateTime = #7/24/2013#)
    Property Q(<DefaultParameterValue(0), DecimalConstant(0, 0, 0, 0, 0), DateTimeConstant(0)> o As Integer)
End Interface
Delegate Sub D(<DateTimeConstant(1), DefaultParameterValue(2)> o As DateTime)
]]>
    </file>
</compilation>)
            comp.AssertTheseDiagnostics(<errors><![CDATA[
BC37226: The parameter has multiple distinct default values.
    Sub F1(<DefaultParameterValue(1)> Optional o As Integer = 2)
                                                              ~
BC37226: The parameter has multiple distinct default values.
    Sub F2(<DefaultParameterValue(1)> Optional o As Decimal = 2)
                                                              ~
BC37226: The parameter has multiple distinct default values.
    Sub F3(<DefaultParameterValue(0)> Optional o As DateTime = #7/24/2013#)
                                                               ~~~~~~~~~~~
BC37226: The parameter has multiple distinct default values.
    Sub F4(<DecimalConstant(0, 0, 0, 0, 1)> Optional o As Decimal = 2)
                                                                    ~
BC37226: The parameter has multiple distinct default values.
    Sub F5(<DateTimeConstant(0)> Optional o As DateTime = #7/24/2013#)
                                                          ~~~~~~~~~~~
BC37226: The parameter has multiple distinct default values.
    Sub F6(<DefaultParameterValue(1), DateTimeConstant(1), DecimalConstant(0, 0, 0, 0, 1)> Optional o As Integer = 1)
                                      ~~~~~~~~~~~~~~~~~~~
BC37226: The parameter has multiple distinct default values.
    Sub F6(<DefaultParameterValue(1), DateTimeConstant(1), DecimalConstant(0, 0, 0, 0, 1)> Optional o As Integer = 1)
                                                           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37226: The parameter has multiple distinct default values.
    Sub F7(<DateTimeConstant(2), DecimalConstant(0, 0, 0, 0, 2), DefaultParameterValue(2)> Optional o As Decimal = 2)
                                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37226: The parameter has multiple distinct default values.
    Sub F7(<DateTimeConstant(2), DecimalConstant(0, 0, 0, 0, 2), DefaultParameterValue(2)> Optional o As Decimal = 2)
                                                                 ~~~~~~~~~~~~~~~~~~~~~~~~
BC37226: The parameter has multiple distinct default values.
    Sub F7(<DateTimeConstant(2), DecimalConstant(0, 0, 0, 0, 2), DefaultParameterValue(2)> Optional o As Decimal = 2)
                                                                                                                   ~
BC37226: The parameter has multiple distinct default values.
    Sub F8(<DecimalConstant(0, 0, 0, 0, 3), DateTimeConstant(3), DefaultParameterValue(3)> Optional o As DateTime = #1/1/2000#)
                                            ~~~~~~~~~~~~~~~~~~~
BC37226: The parameter has multiple distinct default values.
    Sub F8(<DecimalConstant(0, 0, 0, 0, 3), DateTimeConstant(3), DefaultParameterValue(3)> Optional o As DateTime = #1/1/2000#)
                                                                 ~~~~~~~~~~~~~~~~~~~~~~~~
BC37226: The parameter has multiple distinct default values.
    Sub F8(<DecimalConstant(0, 0, 0, 0, 3), DateTimeConstant(3), DefaultParameterValue(3)> Optional o As DateTime = #1/1/2000#)
                                                                                                                    ~~~~~~~~~~
BC37226: The parameter has multiple distinct default values.
    Property P(<DefaultParameterValue(1)> Optional a As Integer = 2,
                                                                  ~
BC37226: The parameter has multiple distinct default values.
        <DefaultParameterValue(3)> Optional b As Decimal = 4,
                                                           ~
BC37226: The parameter has multiple distinct default values.
        <DefaultParameterValue(5)> Optional c As DateTime = #7/24/2013#)
                                                            ~~~~~~~~~~~
BC37226: The parameter has multiple distinct default values.
    Property Q(<DefaultParameterValue(0), DecimalConstant(0, 0, 0, 0, 0), DateTimeConstant(0)> o As Integer)
                                          ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37226: The parameter has multiple distinct default values.
    Property Q(<DefaultParameterValue(0), DecimalConstant(0, 0, 0, 0, 0), DateTimeConstant(0)> o As Integer)
                                                                          ~~~~~~~~~~~~~~~~~~~
BC37226: The parameter has multiple distinct default values.
Delegate Sub D(<DateTimeConstant(1), DefaultParameterValue(2)> o As DateTime)
                                     ~~~~~~~~~~~~~~~~~~~~~~~~                
]]></errors>)
        End Sub

        <WorkItem(529684, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529684")>
        <Fact()>
        Public Sub TestDuplicateConstantAttributesDifferentValues_PartialMethods()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Partial Class C
    Private Sub F1(<DefaultParameterValue(1)> Optional o As Integer = 2)
    End Sub
    Private Partial Sub F9(<DefaultParameterValue(0)> o As Integer)
    End Sub
End Class
Partial Class C
    Private Partial Sub F1(Optional o As Integer = 2)
    End Sub
    Private Sub F9(<DecimalConstant(0, 0, 0, 0, 0), DateTimeConstant(0)> o As Integer)
    End Sub
End Class
]]>
    </file>
</compilation>)
            comp.AssertTheseDiagnostics(<errors><![CDATA[
BC37226: The parameter has multiple distinct default values.
    Private Sub F1(<DefaultParameterValue(1)> Optional o As Integer = 2)
                                                                      ~
BC37226: The parameter has multiple distinct default values.
    Private Partial Sub F1(Optional o As Integer = 2)
                                                   ~
BC37226: The parameter has multiple distinct default values.
    Private Sub F9(<DecimalConstant(0, 0, 0, 0, 0), DateTimeConstant(0)> o As Integer)
                    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37226: The parameter has multiple distinct default values.
    Private Sub F9(<DecimalConstant(0, 0, 0, 0, 0), DateTimeConstant(0)> o As Integer)
                                                    ~~~~~~~~~~~~~~~~~~~
]]></errors>)
        End Sub

        ''' <summary>
        ''' Should not report differences if either value is bad.
        ''' </summary>
        <WorkItem(529684, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529684")>
        <Fact()>
        Public Sub TestDuplicateConstantAttributesDifferentValues_BadValue()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Interface I
    Sub M1(<DefaultParameterValue(GetType(C)), DecimalConstant(0, 0, 0, 0, 0)> o As Decimal)
    Sub M2(<DefaultParameterValue(0), DecimalConstant(0, 0, 0, 0, GetType(C))> o As Decimal)
    Sub M3(<DefaultParameterValue(0), DecimalConstant(0, 0, 0, 0, 0)> o As Decimal)
End Interface
]]>
    </file>
</compilation>)
            comp.AssertTheseDiagnostics(<errors><![CDATA[
BC30002: Type 'C' is not defined.
    Sub M1(<DefaultParameterValue(GetType(C)), DecimalConstant(0, 0, 0, 0, 0)> o As Decimal)
                                          ~
BC37226: The parameter has multiple distinct default values.
    Sub M1(<DefaultParameterValue(GetType(C)), DecimalConstant(0, 0, 0, 0, 0)> o As Decimal)
                                               ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30002: Type 'C' is not defined.
    Sub M2(<DefaultParameterValue(0), DecimalConstant(0, 0, 0, 0, GetType(C))> o As Decimal)
                                                                          ~
BC37226: The parameter has multiple distinct default values.
    Sub M3(<DefaultParameterValue(0), DecimalConstant(0, 0, 0, 0, 0)> o As Decimal)
                                      ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub TestOptionalAttributeWithoutDefaultValue()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

Module Module1
    Sub Main()
        IntegerUsingOptionalAttribute()
        StringUsingOptionalAttribute() 
    End Sub
End Module
]]>
    </file>
</compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, references:={_classLibrary})
            CompileAndVerify(source,
                 references:={_classLibrary},
                 expectedOutput:=<![CDATA[
   0
False
]]>)
        End Sub

        <WorkItem(543076, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543076")>
        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28046")>
        Public Sub TestPropertyIntegerOptionalDouble()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

Module Module1
    Sub Main()
        DumpMetadata(nothing, "get_PropertyIntegerOptionalDouble")
    End Sub
End Module
]]></file>
</compilation>
            CompileAndVerify(source,
                             references:={_classLibrary},
                             expectedOutput:=<![CDATA[
Member: get_PropertyIntegerOptionalDouble
Parameter: Type=System.Int32, Name=i, Optional=False, DefaultValue=
Parameter: Type=System.Double, Name=j, Optional=True, DefaultValue=100
]]>)
        End Sub

        <WorkItem(543093, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543093")>
        <Fact()>
        Public Sub TestIntegerWithDateTimeOptionalValue()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Module Module1
    Sub Main()
        IntegerWithDateTimeOptionalValue() 
    End Sub
End Module
]]>
    </file>
</compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, references:={_classLibrary})
            comp.VerifyDiagnostics(Diagnostic(ERRID.ERR_TypeMismatch2, "IntegerWithDateTimeOptionalValue()").WithArguments("Date", "Integer"))
        End Sub

        <WorkItem(543093, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543093")>
        <ConditionalFact(GetType(NoIOperationValidation))> ' Disabling for IOperation run due to https://github.com/dotnet/roslyn/issues/26895
        Public Sub TestStringWithOptionalDateTimeValue()
            ' Error when option strict is on
            ' No error when option strict is off
            Dim expectedDiagnostics = {
                Diagnostic(ERRID.ERR_NarrowingConversionDisallowed2, "StringWithOptionalDateTimeValue()").WithArguments("Date", "String"),
                Nothing}

            Dim i = 0
            For Each o In {"On", "Off"}
                Dim source =
                    <compilation>
                        <file name="a.vb">
                            Option Strict <%= o %>

                            Module Module1
                                Sub Main()
                                    StringWithOptionalDateTimeValue() 
                                End Sub
                            End Module
                        </file>
                    </compilation>

                Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, references:={_classLibrary})
                Dim diag = expectedDiagnostics(i)
                If diag IsNot Nothing Then
                    comp.VerifyDiagnostics(diag)
                Else
                    comp.VerifyDiagnostics()
                End If
                i += 1
            Next
        End Sub

        <WorkItem(543139, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543139")>
        <Fact()>
        Public Sub TestOverrideOptionalArgumentFromMetadata()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Public Class CDerived
    Inherits CBase
    Public Overrides Sub SubWithOptionalInteger(Optional i As Integer = 13)
    End Sub
End Class

Module Module1
    Sub Main()
    End Sub
End Module
]]>
    </file>
</compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, references:={_classLibrary})
            comp.VerifyDiagnostics()
            CompileAndVerify(source,
                 references:={_classLibrary},
                 expectedOutput:=<![CDATA[
]]>)
        End Sub

        <WorkItem(543227, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543227")>
        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28046")>
        Public Sub TestMultipleEnumDefaultValuesFromMetadata()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Module Module1
    Sub Main()
        TestWithMultipleOptionalEnumValues()
        DumpMetadata(nothing, "TestWithMultipleOptionalEnumValues")
    End Sub
End Module
]]>
    </file>
</compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, references:={_classLibrary})
            comp.VerifyDiagnostics()
            CompileAndVerify(source,
                 references:={_classLibrary},
                 expectedOutput:=<![CDATA[
Member: TestWithMultipleOptionalEnumValues
Parameter: Type=Library+Animal, Name=e1, Optional=True, DefaultValue=Dog
Parameter: Type=Library+Animal, Name=e2, Optional=True, DefaultValue=Cat
]]>)
        End Sub

        ' Test with omitted argument syntax and an error
        ' Test without omitted argument syntax and an error

        <Fact()>
        Public Sub TestExplicitConstantAttributesOnFields_Error()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

Class C
    <DecimalConstant(0, 0, 0, 0, 0)> Public F0 As DateTime

    <DateTimeConstant(0)> Public F1 As DateTime

    <DecimalConstant(0, 0, 0, 0, 0), DecimalConstant(0, 0, 0, 0, 0)> Public F2 As DateTime

    <DateTimeConstant(0), DateTimeConstant(0)> Public F3 As DateTime

    <DecimalConstant(0, 0, 0, 0, 0), DecimalConstant(0, 0, 0, 0, 1)> Public F4 As DateTime

    <DateTimeConstant(1), DateTimeConstant(0)> Public F5 As DateTime

    <DecimalConstant(0, 0, 0, 0, 0), DateTimeConstant(0)> Public F6 As DateTime

    <DecimalConstant(0, 0, 0, 0, 0), DateTimeConstant(0)> Public F7 As Decimal

    <DecimalConstant(0, 0, 0, 0, 0), DateTimeConstant(0)> Public F8 As Integer

    <DecimalConstant(0, 0, 0, 0, 0)> Public Const F9 As Integer = 0

    <DateTimeConstant(0)> Public Const F10 As Integer = 0

    <DateTimeConstant(0)> Public Const F11 As DateTime = #1/1/2013#

    <DateTimeConstant(0)> Public Const F12 As Decimal = 0

    <DecimalConstant(0, 0, 0, 0, 0)> Public Const F13 As DateTime = #1/1/2013#

    <DecimalConstant(0, 0, 0, 0, 0)> Public Const F14 As Decimal = 1
End Class
]]>
    </file>
</compilation>)
            comp.AssertTheseDiagnostics(<errors><![CDATA[
BC30663: Attribute 'DecimalConstantAttribute' cannot be applied multiple times.
    <DecimalConstant(0, 0, 0, 0, 0), DecimalConstant(0, 0, 0, 0, 0)> Public F2 As DateTime
                                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30663: Attribute 'DateTimeConstantAttribute' cannot be applied multiple times.
    <DateTimeConstant(0), DateTimeConstant(0)> Public F3 As DateTime
                          ~~~~~~~~~~~~~~~~~~~
BC30663: Attribute 'DecimalConstantAttribute' cannot be applied multiple times.
    <DecimalConstant(0, 0, 0, 0, 0), DecimalConstant(0, 0, 0, 0, 1)> Public F4 As DateTime
                                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30663: Attribute 'DateTimeConstantAttribute' cannot be applied multiple times.
    <DateTimeConstant(1), DateTimeConstant(0)> Public F5 As DateTime
                          ~~~~~~~~~~~~~~~~~~~
BC37228: The field has multiple distinct constant values.
    <DecimalConstant(0, 0, 0, 0, 0), DateTimeConstant(0)> Public F6 As DateTime
                                     ~~~~~~~~~~~~~~~~~~~
BC37228: The field has multiple distinct constant values.
    <DecimalConstant(0, 0, 0, 0, 0), DateTimeConstant(0)> Public F7 As Decimal
                                     ~~~~~~~~~~~~~~~~~~~
BC37228: The field has multiple distinct constant values.
    <DecimalConstant(0, 0, 0, 0, 0), DateTimeConstant(0)> Public F8 As Integer
                                     ~~~~~~~~~~~~~~~~~~~
BC37228: The field has multiple distinct constant values.
    <DecimalConstant(0, 0, 0, 0, 0)> Public Const F9 As Integer = 0
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37228: The field has multiple distinct constant values.
    <DateTimeConstant(0)> Public Const F10 As Integer = 0
     ~~~~~~~~~~~~~~~~~~~
BC37228: The field has multiple distinct constant values.
    <DateTimeConstant(0)> Public Const F11 As DateTime = #1/1/2013#
     ~~~~~~~~~~~~~~~~~~~
BC37228: The field has multiple distinct constant values.
    <DateTimeConstant(0)> Public Const F12 As Decimal = 0
     ~~~~~~~~~~~~~~~~~~~
BC37228: The field has multiple distinct constant values.
    <DecimalConstant(0, 0, 0, 0, 0)> Public Const F13 As DateTime = #1/1/2013#
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37228: The field has multiple distinct constant values.
    <DecimalConstant(0, 0, 0, 0, 0)> Public Const F14 As Decimal = 1
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub TestExplicitConstantAttributesOnFields_Valid()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

Class C
    <DecimalConstantAttribute(0, 128, 0, 0, 7)> Public Const F1 as Decimal = -7

    <DateTimeConstantAttribute(634925952000000000)> Public Const F2 as Date = #1/1/2013#
End Class
]]>
    </file>
</compilation>)

            CompileAndVerify(comp, symbolValidator:=Sub([module] As ModuleSymbol)
                                                        Dim peModule = DirectCast([module], PEModuleSymbol)
                                                        Dim type = peModule.GlobalNamespace.GetTypeMember("C")

                                                        Dim f1 = DirectCast(type.GetMember("F1"), PEFieldSymbol)
                                                        Assert.Equal(1, peModule.GetCustomAttributesForToken(f1.Handle).Length)

                                                        Dim f2 = DirectCast(type.GetMember("F2"), PEFieldSymbol)
                                                        Assert.Equal(1, peModule.GetCustomAttributesForToken(f2.Handle).Length)
                                                    End Sub)
        End Sub

    End Class
End Namespace
