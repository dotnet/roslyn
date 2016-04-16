' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Reflection.Metadata
Imports System.Reflection.Metadata.Ecma335
Imports System.Text
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class ComClassTests
        Inherits BasicTestBase

        Private Function ReflectComClass(
            pm As PEModuleSymbol, comClassName As String,
            Optional memberFilter As Func(Of Symbol, Boolean) = Nothing
        ) As XElement
            Dim type As PENamedTypeSymbol = DirectCast(pm.ContainingAssembly.GetTypeByMetadataName(comClassName), PENamedTypeSymbol)

            Dim combinedFilter = Function(m As Symbol)
                                     Return (memberFilter Is Nothing OrElse memberFilter(m)) AndAlso
                                         (m.ContainingSymbol IsNot type OrElse
                                         m.Kind <> SymbolKind.NamedType OrElse
                                         Not DirectCast(m, NamedTypeSymbol).IsDelegateType())
                                 End Function

            Return ReflectType(type, combinedFilter)
        End Function

        Private Function ReflectType(type As PENamedTypeSymbol, Optional memberFilter As Func(Of Symbol, Boolean) = Nothing) As XElement

            Dim result = <<%= type.TypeKind.ToString() %> Name=<%= type.Name %>></>

            Dim typeDefFlags = New StringBuilder()
            MetadataSignatureHelper.AppendTypeAttributes(typeDefFlags, type.TypeDefFlags)
            result.Add(<TypeDefFlags><%= typeDefFlags %></TypeDefFlags>)

            If type.GetAttributes().Length > 0 Then
                result.Add(ReflectAttributes(type.GetAttributes()))
            End If

            For Each [interface] In type.Interfaces
                result.Add(<Implements><%= [interface].ToTestDisplayString() %></Implements>)
            Next

            For Each member In type.GetMembers

                If memberFilter IsNot Nothing AndAlso Not memberFilter(member) Then
                    Continue For
                End If

                Select Case member.Kind
                    Case SymbolKind.NamedType
                        result.Add(ReflectType(DirectCast(member, PENamedTypeSymbol), memberFilter))

                    Case SymbolKind.Method
                        result.Add(ReflectMethod(DirectCast(member, PEMethodSymbol)))

                    Case SymbolKind.Property
                        result.Add(ReflectProperty(DirectCast(member, PEPropertySymbol)))

                    Case SymbolKind.Event
                        result.Add(ReflectEvent(DirectCast(member, PEEventSymbol)))

                    Case SymbolKind.Field
                        result.Add(ReflectField(DirectCast(member, PEFieldSymbol)))

                    Case Else
                        Throw TestExceptionUtilities.UnexpectedValue(member.Kind)
                End Select
            Next

            Return result
        End Function

        Private Function ReflectAttributes(attrData As ImmutableArray(Of VisualBasicAttributeData)) As XElement
            Dim result = <Attributes></Attributes>

            For Each attr In attrData
                Dim application = <<%= attr.AttributeClass.ToTestDisplayString() %>/>
                result.Add(application)

                application.Add(<ctor><%= attr.AttributeConstructor.ToTestDisplayString() %></ctor>)

                For Each arg In attr.CommonConstructorArguments
                    application.Add(<a><%= arg.Value.ToString() %></a>)
                Next

                For Each named In attr.CommonNamedArguments
                    application.Add(<Named Name=<%= named.Key %>><%= named.Value.Value.ToString() %></Named>)
                Next
            Next

            Return result
        End Function


        Private Function ReflectMethod(m As PEMethodSymbol) As XElement
            Dim result = <Method Name=<%= m.Name %> CallingConvention=<%= m.CallingConvention %>/>

            Dim methodFlags = New StringBuilder()
            Dim methodImplFlags = New StringBuilder()
            MetadataSignatureHelper.AppendMethodAttributes(methodFlags, m.MethodFlags)
            MetadataSignatureHelper.AppendMethodImplAttributes(methodImplFlags, m.MethodImplFlags)

            result.Add(<MethodFlags><%= methodFlags %></MethodFlags>)
            result.Add(<MethodImplFlags><%= methodImplFlags %></MethodImplFlags>)

            If m.GetAttributes().Length > 0 Then
                result.Add(ReflectAttributes(m.GetAttributes()))
            End If

            For Each impl In m.ExplicitInterfaceImplementations
                result.Add(<Implements><%= impl.ToTestDisplayString() %></Implements>)
            Next

            For Each param In m.Parameters
                result.Add(ReflectParameter(DirectCast(param, PEParameterSymbol)))
            Next

            Dim ret = <Return><Type><%= m.ReturnType %></Type></Return>
            result.Add(ret)

            Dim retFlags = m.ReturnParam.ParamFlags

            If retFlags <> 0 Then
                Dim paramFlags = New StringBuilder()
                MetadataSignatureHelper.AppendParameterAttributes(paramFlags, retFlags)
                ret.Add(<ParamFlags><%= paramFlags %></ParamFlags>)
            End If

            If m.GetReturnTypeAttributes().Length > 0 Then
                ret.Add(ReflectAttributes(m.GetReturnTypeAttributes()))
            End If

            Return result
        End Function

        Private Function ReflectParameter(p As PEParameterSymbol) As XElement
            Dim result = <Parameter Name=<%= p.Name %>/>

            Dim paramFlags = New StringBuilder()
            MetadataSignatureHelper.AppendParameterAttributes(paramFlags, p.ParamFlags)
            result.Add(<ParamFlags><%= paramFlags %></ParamFlags>)

            If p.GetAttributes().Length > 0 Then
                result.Add(ReflectAttributes(p.GetAttributes()))
            End If

            If p.IsParamArray Then
                Dim peModule = DirectCast(p.ContainingModule, PEModuleSymbol).Module
                Dim numParamArray = peModule.GetParamArrayCountOrThrow(p.Handle)
                result.Add(<ParamArray count=<%= numParamArray %>/>)
            End If

            Dim type = <Type><%= p.Type %></Type>
            result.Add(type)

            If p.IsByRef Then
                type.@ByRef = "True"
            End If

            If p.HasExplicitDefaultValue Then
                result.Add(<Default><%= p.ExplicitDefaultValue %></Default>)
            End If

            ' TODO (tomat): add MarshallingInformation

            Return result
        End Function

        Private Function ReflectProperty(p As PEPropertySymbol) As XElement
            Dim result = <Property Name=<%= p.Name %>/>

            Dim propertyFlags As New StringBuilder()
            MetadataSignatureHelper.AppendPropertyAttributes(propertyFlags, p.PropertyFlags)
            result.Add(<PropertyFlags><%= propertyFlags %></PropertyFlags>)

            If p.GetAttributes().Length > 0 Then
                result.Add(ReflectAttributes(p.GetAttributes()))
            End If

            If p.GetMethod IsNot Nothing Then
                result.Add(<Get><%= p.GetMethod.ToTestDisplayString() %></Get>)
            End If

            If p.SetMethod IsNot Nothing Then
                result.Add(<Set><%= p.SetMethod.ToTestDisplayString() %></Set>)
            End If

            Return result
        End Function

        Private Function ReflectEvent(e As PEEventSymbol) As XElement
            Dim result = <Event Name=<%= e.Name %>/>

            Dim eventFlags = New StringBuilder()
            MetadataSignatureHelper.AppendEventAttributes(eventFlags, e.EventFlags)
            result.Add(<EventFlags><%= eventFlags %></EventFlags>)

            If e.GetAttributes().Length > 0 Then
                result.Add(ReflectAttributes(e.GetAttributes()))
            End If

            If e.AddMethod IsNot Nothing Then
                result.Add(<Add><%= e.AddMethod.ToTestDisplayString() %></Add>)
            End If

            If e.RemoveMethod IsNot Nothing Then
                result.Add(<Remove><%= e.RemoveMethod.ToTestDisplayString() %></Remove>)
            End If

            If e.RaiseMethod IsNot Nothing Then
                result.Add(<Raise><%= e.RaiseMethod.ToTestDisplayString() %></Raise>)
            End If

            Return result
        End Function

        Private Function ReflectField(f As PEFieldSymbol) As XElement
            Dim result = <Field Name=<%= f.Name %>/>

            Dim fieldFlags = New StringBuilder()
            MetadataSignatureHelper.AppendFieldAttributes(fieldFlags, f.FieldFlags)
            result.Add(<FieldFlags><%= fieldFlags %></FieldFlags>)

            If f.GetAttributes().Length > 0 Then
                result.Add(ReflectAttributes(f.GetAttributes()))
            End If

            result.Add(<Type><%= f.Type %></Type>)

            Return result
        End Function

        Private Sub AssertReflection(expected As XElement, actual As XElement)
            Dim expectedStr = expected.ToString().Trim()
            Dim actualStr = actual.ToString().Trim()

            Assert.True(expectedStr.Equals(actualStr), AssertEx.GetAssertMessage(expectedStr, actualStr))
        End Sub

        <Fact>
        Public Sub SimpleTest1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices

Public Class TestAttribute1
    Inherits System.Attribute

    Sub New(x As String)
    End Sub
End Class

<System.AttributeUsage(System.AttributeTargets.All And Not System.AttributeTargets.Method)>
Public Class TestAttribute2
    Inherits System.Attribute

    Sub New(x As String)
    End Sub
End Class

<TestAttribute1("EventDelegate")>
Public Delegate Sub EventDelegate(<TestAttribute1("EventDelegate_x")> x As Byte, ByRef y As String, <MarshalAs(UnmanagedType.BStr)> z As String)

Public MustInherit Class ComClassTestBase
    MustOverride Sub M4(Optional x As Date = #8/23/1970#, Optional y As Decimal = 4.5D)
    MustOverride Sub M5(ParamArray z As Integer())
End Class

<Microsoft.VisualBasic.ComClass("", "", "")>
Public Class ComClassTest
    Inherits ComClassTestBase

    Sub M1()
    End Sub

    Property P1 As Integer
        Get
            Return Nothing
        End Get
        Set(value As Integer)

        End Set
    End Property

    Function M2(x As Integer, ByRef y As Double) As Object
        Return Nothing
    End Function

    Event E1 As EventDelegate

    <TestAttribute1("TestAttribute1_E2"), TestAttribute2("TestAttribute2_E2")>
    Event E2(<TestAttribute1("E2_x")> x As Byte, ByRef y As String, <MarshalAs(UnmanagedType.AnsiBStr)> z As String)

    <TestAttribute1("TestAttribute1_M3")>
    Function M3(<TestAttribute2("TestAttribute2_M3"), [In], Out, MarshalAs(UnmanagedType.AnsiBStr)> Optional ByRef x As String = "M3_x"
    ) As <TestAttribute1("Return_M3"), MarshalAs(UnmanagedType.BStr)> String
        Return Nothing
    End Function

    Public Overrides Sub M4(Optional x As Date = #8/23/1970#, Optional y As Decimal = 4.5D)
    End Sub

    Public NotOverridable Overrides Sub M5(ParamArray z() As Integer)
    End Sub

    Public ReadOnly Property P2 As String
        Get
            Return Nothing
        End Get
    End Property

    Public WriteOnly Property P3 As String
        Set(value As String)
        End Set
    End Property

    <TestAttribute1("TestAttribute1_P4")>
    Public Property P4(<TestAttribute2("TestAttribute2_P4_x"), [In], MarshalAs(UnmanagedType.AnsiBStr)> x As String, Optional y As Decimal = 5.5D
    ) As <TestAttribute1("Return_M4"), MarshalAs(UnmanagedType.BStr)> String
        <TestAttribute1("TestAttribute1_P4_Get")>
        Get
            Return Nothing
        End Get
        <TestAttribute1("TestAttribute1_P4_Set")>
        Set(<TestAttribute2("TestAttribute2_P4_value"), [In], MarshalAs(UnmanagedType.LPWStr)> value As String)
        End Set
    End Property

    Public Property P5 As Byte
        Friend Get
            Return Nothing
        End Get
        Set(value As Byte)
        End Set
    End Property

    Public Property P6 As Byte
        Get
            Return Nothing
        End Get
        Friend Set(value As Byte)
        End Set
    End Property

    Friend Sub M6()
    End Sub

    Public Shared Sub M7()
    End Sub

    Friend Property P7 As Long
        Get
            Return 0
        End Get
        Set(value As Long)
        End Set
    End Property

    Public Shared Property P8 As Long
        Get
            Return 0
        End Get
        Set(value As Long)
        End Set
    End Property

    Friend Event E3 As EventDelegate
    Public Shared Event E4 As EventDelegate

    Public WithEvents WithEvents1 As ComClassTest

    Friend Sub Handler(x As Byte, ByRef y As String, z As String) Handles WithEvents1.E1
    End Sub

    Public F1 As Integer
End Class
    ]]></file>
</compilation>

            Dim expected =
<Class Name="ComClassTest">
    <TypeDefFlags>public auto ansi</TypeDefFlags>
    <Attributes>
        <System.Runtime.InteropServices.ClassInterfaceAttribute>
            <ctor>Sub System.Runtime.InteropServices.ClassInterfaceAttribute..ctor(classInterfaceType As System.Runtime.InteropServices.ClassInterfaceType)</ctor>
            <a>0</a>
        </System.Runtime.InteropServices.ClassInterfaceAttribute>
        <System.Runtime.InteropServices.ComSourceInterfacesAttribute>
            <ctor>Sub System.Runtime.InteropServices.ComSourceInterfacesAttribute..ctor(sourceInterfaces As System.String)</ctor>
            <a>ComClassTest+__ComClassTest</a>
        </System.Runtime.InteropServices.ComSourceInterfacesAttribute>
        <Microsoft.VisualBasic.ComClassAttribute>
            <ctor>Sub Microsoft.VisualBasic.ComClassAttribute..ctor(_ClassID As System.String, _InterfaceID As System.String, _EventId As System.String)</ctor>
            <a></a><a></a><a></a>
        </Microsoft.VisualBasic.ComClassAttribute>
    </Attributes>
    <Implements>ComClassTest._ComClassTest</Implements>
    <Field Name="F1">
        <FieldFlags>public instance</FieldFlags>
        <Type>Integer</Type>
    </Field>
    <Method Name=".ctor" CallingConvention="HasThis">
        <MethodFlags>public specialname rtspecialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="M1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest._ComClassTest.M1()</Implements>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="get_P1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict specialname virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Function ComClassTest._ComClassTest.get_P1() As System.Int32</Implements>
        <Return>
            <Type>Integer</Type>
        </Return>
    </Method>
    <Method Name="set_P1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict specialname virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest._ComClassTest.set_P1(value As System.Int32)</Implements>
        <Parameter Name="value">
            <ParamFlags></ParamFlags>
            <Type>Integer</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="M2" CallingConvention="HasThis">
        <MethodFlags>public newslot strict virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Function ComClassTest._ComClassTest.M2(x As System.Int32, ByRef y As System.Double) As System.Object</Implements>
        <Parameter Name="x">
            <ParamFlags></ParamFlags>
            <Type>Integer</Type>
        </Parameter>
        <Parameter Name="y">
            <ParamFlags></ParamFlags>
            <Type ByRef="True">Double</Type>
        </Parameter>
        <Return>
            <Type>Object</Type>
        </Return>
    </Method>
    <Method Name="add_E1" CallingConvention="HasThis">
        <MethodFlags>public specialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.CompilerServices.CompilerGeneratedAttribute>
                <ctor>Sub System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor()</ctor>
            </System.Runtime.CompilerServices.CompilerGeneratedAttribute>
        </Attributes>
        <Parameter Name="obj">
            <ParamFlags></ParamFlags>
            <Type>EventDelegate</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="remove_E1" CallingConvention="HasThis">
        <MethodFlags>public specialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.CompilerServices.CompilerGeneratedAttribute>
                <ctor>Sub System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor()</ctor>
            </System.Runtime.CompilerServices.CompilerGeneratedAttribute>
        </Attributes>
        <Parameter Name="obj">
            <ParamFlags></ParamFlags>
            <Type>EventDelegate</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="add_E2" CallingConvention="HasThis">
        <MethodFlags>public specialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.CompilerServices.CompilerGeneratedAttribute>
                <ctor>Sub System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor()</ctor>
            </System.Runtime.CompilerServices.CompilerGeneratedAttribute>
        </Attributes>
        <Parameter Name="obj">
            <ParamFlags></ParamFlags>
            <Type>ComClassTest.E2EventHandler</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="remove_E2" CallingConvention="HasThis">
        <MethodFlags>public specialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.CompilerServices.CompilerGeneratedAttribute>
                <ctor>Sub System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor()</ctor>
            </System.Runtime.CompilerServices.CompilerGeneratedAttribute>
        </Attributes>
        <Parameter Name="obj">
            <ParamFlags></ParamFlags>
            <Type>ComClassTest.E2EventHandler</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="M3" CallingConvention="HasThis">
        <MethodFlags>public newslot strict virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <TestAttribute1>
                <ctor>Sub TestAttribute1..ctor(x As System.String)</ctor>
                <a>TestAttribute1_M3</a>
            </TestAttribute1>
        </Attributes>
        <Implements>Function ComClassTest._ComClassTest.M3([ByRef x As System.String = "M3_x"]) As System.String</Implements>
        <Parameter Name="x">
            <ParamFlags>[opt] [in] [out] marshal default</ParamFlags>
            <Attributes>
                <TestAttribute2>
                    <ctor>Sub TestAttribute2..ctor(x As System.String)</ctor>
                    <a>TestAttribute2_M3</a>
                </TestAttribute2>
            </Attributes>
            <Type ByRef="True">String</Type>
            <Default>M3_x</Default>
        </Parameter>
        <Return>
            <Type>String</Type>
            <ParamFlags>marshal</ParamFlags>
            <Attributes>
                <TestAttribute1>
                    <ctor>Sub TestAttribute1..ctor(x As System.String)</ctor>
                    <a>Return_M3</a>
                </TestAttribute1>
            </Attributes>
        </Return>
    </Method>
    <Method Name="M4" CallingConvention="HasThis">
        <MethodFlags>public hidebysig strict virtual instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest._ComClassTest.M4([x As System.DateTime = #8/23/1970 12:00:00 AM#], [y As System.Decimal = 4.5])</Implements>
        <Parameter Name="x">
            <ParamFlags>[opt]</ParamFlags>
            <Type>Date</Type>
            <Default>1970-08-23T00:00:00</Default>
        </Parameter>
        <Parameter Name="y">
            <ParamFlags>[opt]</ParamFlags>
            <Type>Decimal</Type>
            <Default>4.5</Default>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="M5" CallingConvention="HasThis">
        <MethodFlags>public hidebysig strict virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest._ComClassTest.M5(ParamArray z As System.Int32())</Implements>
        <Parameter Name="z">
            <ParamFlags></ParamFlags>
            <ParamArray count="1"/>
            <Type>Integer()</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="get_P2" CallingConvention="HasThis">
        <MethodFlags>public newslot strict specialname virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Function ComClassTest._ComClassTest.get_P2() As System.String</Implements>
        <Return>
            <Type>String</Type>
        </Return>
    </Method>
    <Method Name="set_P3" CallingConvention="HasThis">
        <MethodFlags>public newslot strict specialname virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest._ComClassTest.set_P3(value As System.String)</Implements>
        <Parameter Name="value">
            <ParamFlags></ParamFlags>
            <Type>String</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="get_P4" CallingConvention="HasThis">
        <MethodFlags>public newslot strict specialname virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <TestAttribute1>
                <ctor>Sub TestAttribute1..ctor(x As System.String)</ctor>
                <a>TestAttribute1_P4_Get</a>
            </TestAttribute1>
        </Attributes>
        <Implements>Function ComClassTest._ComClassTest.get_P4(x As System.String, [y As System.Decimal = 5.5]) As System.String</Implements>
        <Parameter Name="x">
            <ParamFlags>[in] marshal</ParamFlags>
            <Attributes>
                <TestAttribute2>
                    <ctor>Sub TestAttribute2..ctor(x As System.String)</ctor>
                    <a>TestAttribute2_P4_x</a>
                </TestAttribute2>
            </Attributes>
            <Type>String</Type>
        </Parameter>
        <Parameter Name="y">
            <ParamFlags>[opt]</ParamFlags>
            <Type>Decimal</Type>
            <Default>5.5</Default>
        </Parameter>
        <Return>
            <Type>String</Type>
            <ParamFlags>marshal</ParamFlags>
            <Attributes>
                <TestAttribute1>
                    <ctor>Sub TestAttribute1..ctor(x As System.String)</ctor>
                    <a>Return_M4</a>
                </TestAttribute1>
            </Attributes>
        </Return>
    </Method>
    <Method Name="set_P4" CallingConvention="HasThis">
        <MethodFlags>public newslot strict specialname virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <TestAttribute1>
                <ctor>Sub TestAttribute1..ctor(x As System.String)</ctor>
                <a>TestAttribute1_P4_Set</a>
            </TestAttribute1>
        </Attributes>
        <Implements>Sub ComClassTest._ComClassTest.set_P4(x As System.String, [y As System.Decimal = 5.5], value As System.String)</Implements>
        <Parameter Name="x">
            <ParamFlags>[in] marshal</ParamFlags>
            <Attributes>
                <TestAttribute2>
                    <ctor>Sub TestAttribute2..ctor(x As System.String)</ctor>
                    <a>TestAttribute2_P4_x</a>
                </TestAttribute2>
            </Attributes>
            <Type>String</Type>
        </Parameter>
        <Parameter Name="y">
            <ParamFlags>[opt]</ParamFlags>
            <Type>Decimal</Type>
            <Default>5.5</Default>
        </Parameter>
        <Parameter Name="value">
            <ParamFlags>[in] marshal</ParamFlags>
            <Attributes>
                <TestAttribute2>
                    <ctor>Sub TestAttribute2..ctor(x As System.String)</ctor>
                    <a>TestAttribute2_P4_value</a>
                </TestAttribute2>
            </Attributes>
            <Type>String</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="get_P5" CallingConvention="HasThis">
        <MethodFlags>assembly specialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Return>
            <Type>Byte</Type>
        </Return>
    </Method>
    <Method Name="set_P5" CallingConvention="HasThis">
        <MethodFlags>public newslot strict specialname virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest._ComClassTest.set_P5(value As System.Byte)</Implements>
        <Parameter Name="value">
            <ParamFlags></ParamFlags>
            <Type>Byte</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="get_P6" CallingConvention="HasThis">
        <MethodFlags>public newslot strict specialname virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Function ComClassTest._ComClassTest.get_P6() As System.Byte</Implements>
        <Return>
            <Type>Byte</Type>
        </Return>
    </Method>
    <Method Name="set_P6" CallingConvention="HasThis">
        <MethodFlags>assembly specialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Parameter Name="value">
            <ParamFlags></ParamFlags>
            <Type>Byte</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="M6" CallingConvention="HasThis">
        <MethodFlags>assembly instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="M7" CallingConvention="Default">
        <MethodFlags>public static</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="get_P7" CallingConvention="HasThis">
        <MethodFlags>assembly specialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Return>
            <Type>Long</Type>
        </Return>
    </Method>
    <Method Name="set_P7" CallingConvention="HasThis">
        <MethodFlags>assembly specialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Parameter Name="value">
            <ParamFlags></ParamFlags>
            <Type>Long</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="get_P8" CallingConvention="Default">
        <MethodFlags>public specialname static</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Return>
            <Type>Long</Type>
        </Return>
    </Method>
    <Method Name="set_P8" CallingConvention="Default">
        <MethodFlags>public specialname static</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Parameter Name="value">
            <ParamFlags></ParamFlags>
            <Type>Long</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="add_E3" CallingConvention="HasThis">
        <MethodFlags>assembly specialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.CompilerServices.CompilerGeneratedAttribute>
                <ctor>Sub System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor()</ctor>
            </System.Runtime.CompilerServices.CompilerGeneratedAttribute>
        </Attributes>
        <Parameter Name="obj">
            <ParamFlags></ParamFlags>
            <Type>EventDelegate</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="remove_E3" CallingConvention="HasThis">
        <MethodFlags>assembly specialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.CompilerServices.CompilerGeneratedAttribute>
                <ctor>Sub System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor()</ctor>
            </System.Runtime.CompilerServices.CompilerGeneratedAttribute>
        </Attributes>
        <Parameter Name="obj">
            <ParamFlags></ParamFlags>
            <Type>EventDelegate</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="add_E4" CallingConvention="Default">
        <MethodFlags>public specialname static</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.CompilerServices.CompilerGeneratedAttribute>
                <ctor>Sub System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor()</ctor>
            </System.Runtime.CompilerServices.CompilerGeneratedAttribute>
        </Attributes>
        <Parameter Name="obj">
            <ParamFlags></ParamFlags>
            <Type>EventDelegate</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="remove_E4" CallingConvention="Default">
        <MethodFlags>public specialname static</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.CompilerServices.CompilerGeneratedAttribute>
                <ctor>Sub System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor()</ctor>
            </System.Runtime.CompilerServices.CompilerGeneratedAttribute>
        </Attributes>
        <Parameter Name="obj">
            <ParamFlags></ParamFlags>
            <Type>EventDelegate</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="get_WithEvents1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict specialname virtual instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.CompilerServices.CompilerGeneratedAttribute>
                <ctor>Sub System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor()</ctor>
            </System.Runtime.CompilerServices.CompilerGeneratedAttribute>
        </Attributes>
        <Return>
            <Type>ComClassTest</Type>
        </Return>
    </Method>
    <Method Name="set_WithEvents1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict specialname virtual instance</MethodFlags>
        <MethodImplFlags>cil managed synchronized</MethodImplFlags>
        <Attributes>
            <System.Runtime.CompilerServices.CompilerGeneratedAttribute>
                <ctor>Sub System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor()</ctor>
            </System.Runtime.CompilerServices.CompilerGeneratedAttribute>
        </Attributes>
        <Parameter Name="WithEventsValue">
            <ParamFlags></ParamFlags>
            <Type>ComClassTest</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="Handler" CallingConvention="HasThis">
        <MethodFlags>assembly instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Parameter Name="x">
            <ParamFlags></ParamFlags>
            <Type>Byte</Type>
        </Parameter>
        <Parameter Name="y">
            <ParamFlags></ParamFlags>
            <Type ByRef="True">String</Type>
        </Parameter>
        <Parameter Name="z">
            <ParamFlags></ParamFlags>
            <Type>String</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Property Name="P1">
        <PropertyFlags></PropertyFlags>
        <Get>Function ComClassTest.get_P1() As System.Int32</Get>
        <Set>Sub ComClassTest.set_P1(value As System.Int32)</Set>
    </Property>
    <Property Name="P2">
        <PropertyFlags></PropertyFlags>
        <Get>Function ComClassTest.get_P2() As System.String</Get>
    </Property>
    <Property Name="P3">
        <PropertyFlags></PropertyFlags>
        <Set>Sub ComClassTest.set_P3(value As System.String)</Set>
    </Property>
    <Property Name="P4">
        <PropertyFlags></PropertyFlags>
        <Attributes>
            <TestAttribute1>
                <ctor>Sub TestAttribute1..ctor(x As System.String)</ctor>
                <a>TestAttribute1_P4</a>
            </TestAttribute1>
        </Attributes>
        <Get>Function ComClassTest.get_P4(x As System.String, [y As System.Decimal = 5.5]) As System.String</Get>
        <Set>Sub ComClassTest.set_P4(x As System.String, [y As System.Decimal = 5.5], value As System.String)</Set>
    </Property>
    <Property Name="P5">
        <PropertyFlags></PropertyFlags>
        <Get>Function ComClassTest.get_P5() As System.Byte</Get>
        <Set>Sub ComClassTest.set_P5(value As System.Byte)</Set>
    </Property>
    <Property Name="P6">
        <PropertyFlags></PropertyFlags>
        <Get>Function ComClassTest.get_P6() As System.Byte</Get>
        <Set>Sub ComClassTest.set_P6(value As System.Byte)</Set>
    </Property>
    <Property Name="P7">
        <PropertyFlags></PropertyFlags>
        <Get>Function ComClassTest.get_P7() As System.Int64</Get>
        <Set>Sub ComClassTest.set_P7(value As System.Int64)</Set>
    </Property>
    <Property Name="P8">
        <PropertyFlags></PropertyFlags>
        <Get>Function ComClassTest.get_P8() As System.Int64</Get>
        <Set>Sub ComClassTest.set_P8(value As System.Int64)</Set>
    </Property>
    <Property Name="WithEvents1">
        <PropertyFlags></PropertyFlags>
        <Get>Function ComClassTest.get_WithEvents1() As ComClassTest</Get>
        <Set>Sub ComClassTest.set_WithEvents1(WithEventsValue As ComClassTest)</Set>
    </Property>
    <Event Name="E1">
        <EventFlags></EventFlags>
        <Add>Sub ComClassTest.add_E1(obj As EventDelegate)</Add>
        <Remove>Sub ComClassTest.remove_E1(obj As EventDelegate)</Remove>
    </Event>
    <Event Name="E2">
        <EventFlags></EventFlags>
        <Attributes>
            <TestAttribute1>
                <ctor>Sub TestAttribute1..ctor(x As System.String)</ctor>
                <a>TestAttribute1_E2</a>
            </TestAttribute1>
            <TestAttribute2>
                <ctor>Sub TestAttribute2..ctor(x As System.String)</ctor>
                <a>TestAttribute2_E2</a>
            </TestAttribute2>
        </Attributes>
        <Add>Sub ComClassTest.add_E2(obj As ComClassTest.E2EventHandler)</Add>
        <Remove>Sub ComClassTest.remove_E2(obj As ComClassTest.E2EventHandler)</Remove>
    </Event>
    <Event Name="E3">
        <EventFlags></EventFlags>
        <Add>Sub ComClassTest.add_E3(obj As EventDelegate)</Add>
        <Remove>Sub ComClassTest.remove_E3(obj As EventDelegate)</Remove>
    </Event>
    <Event Name="E4">
        <EventFlags></EventFlags>
        <Add>Sub ComClassTest.add_E4(obj As EventDelegate)</Add>
        <Remove>Sub ComClassTest.remove_E4(obj As EventDelegate)</Remove>
    </Event>
    <Interface Name="_ComClassTest">
        <TypeDefFlags>interface nested public abstract auto ansi</TypeDefFlags>
        <Attributes>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>True</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
        </Attributes>
        <Method Name="M1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>1</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Method Name="get_P1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict specialname abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>2</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Integer</Type>
            </Return>
        </Method>
        <Method Name="set_P1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict specialname abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>2</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Parameter Name="value">
                <ParamFlags></ParamFlags>
                <Type>Integer</Type>
            </Parameter>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Method Name="M2" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>3</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Parameter Name="x">
                <ParamFlags></ParamFlags>
                <Type>Integer</Type>
            </Parameter>
            <Parameter Name="y">
                <ParamFlags></ParamFlags>
                <Type ByRef="True">Double</Type>
            </Parameter>
            <Return>
                <Type>Object</Type>
            </Return>
        </Method>
        <Method Name="M3" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>4</a>
                </System.Runtime.InteropServices.DispIdAttribute>
                <TestAttribute1>
                    <ctor>Sub TestAttribute1..ctor(x As System.String)</ctor>
                    <a>TestAttribute1_M3</a>
                </TestAttribute1>
            </Attributes>
            <Parameter Name="x">
                <ParamFlags>[opt] [in] [out] marshal default</ParamFlags>
                <Attributes>
                    <TestAttribute2>
                        <ctor>Sub TestAttribute2..ctor(x As System.String)</ctor>
                        <a>TestAttribute2_M3</a>
                    </TestAttribute2>
                </Attributes>
                <Type ByRef="True">String</Type>
                <Default>M3_x</Default>
            </Parameter>
            <Return>
                <Type>String</Type>
                <ParamFlags>marshal</ParamFlags>
                <Attributes>
                    <TestAttribute1>
                        <ctor>Sub TestAttribute1..ctor(x As System.String)</ctor>
                        <a>Return_M3</a>
                    </TestAttribute1>
                </Attributes>
            </Return>
        </Method>
        <Method Name="M4" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>5</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Parameter Name="x">
                <ParamFlags>[opt]</ParamFlags>
                <Type>Date</Type>
                <Default>1970-08-23T00:00:00</Default>
            </Parameter>
            <Parameter Name="y">
                <ParamFlags>[opt]</ParamFlags>
                <Type>Decimal</Type>
                <Default>4.5</Default>
            </Parameter>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Method Name="M5" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>6</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Parameter Name="z">
                <ParamFlags></ParamFlags>
                <ParamArray count="1"/>
                <Type>Integer()</Type>
            </Parameter>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Method Name="get_P2" CallingConvention="HasThis">
            <MethodFlags>public newslot strict specialname abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>7</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>String</Type>
            </Return>
        </Method>
        <Method Name="set_P3" CallingConvention="HasThis">
            <MethodFlags>public newslot strict specialname abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>8</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Parameter Name="value">
                <ParamFlags></ParamFlags>
                <Type>String</Type>
            </Parameter>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Method Name="get_P4" CallingConvention="HasThis">
            <MethodFlags>public newslot strict specialname abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>9</a>
                </System.Runtime.InteropServices.DispIdAttribute>
                <TestAttribute1>
                    <ctor>Sub TestAttribute1..ctor(x As System.String)</ctor>
                    <a>TestAttribute1_P4_Get</a>
                </TestAttribute1>
            </Attributes>
            <Parameter Name="x">
                <ParamFlags>[in] marshal</ParamFlags>
                <Attributes>
                    <TestAttribute2>
                        <ctor>Sub TestAttribute2..ctor(x As System.String)</ctor>
                        <a>TestAttribute2_P4_x</a>
                    </TestAttribute2>
                </Attributes>
                <Type>String</Type>
            </Parameter>
            <Parameter Name="y">
                <ParamFlags>[opt]</ParamFlags>
                <Type>Decimal</Type>
                <Default>5.5</Default>
            </Parameter>
            <Return>
                <Type>String</Type>
                <ParamFlags>marshal</ParamFlags>
                <Attributes>
                    <TestAttribute1>
                        <ctor>Sub TestAttribute1..ctor(x As System.String)</ctor>
                        <a>Return_M4</a>
                    </TestAttribute1>
                </Attributes>
            </Return>
        </Method>
        <Method Name="set_P4" CallingConvention="HasThis">
            <MethodFlags>public newslot strict specialname abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>9</a>
                </System.Runtime.InteropServices.DispIdAttribute>
                <TestAttribute1>
                    <ctor>Sub TestAttribute1..ctor(x As System.String)</ctor>
                    <a>TestAttribute1_P4_Set</a>
                </TestAttribute1>
            </Attributes>
            <Parameter Name="x">
                <ParamFlags>[in] marshal</ParamFlags>
                <Attributes>
                    <TestAttribute2>
                        <ctor>Sub TestAttribute2..ctor(x As System.String)</ctor>
                        <a>TestAttribute2_P4_x</a>
                    </TestAttribute2>
                </Attributes>
                <Type>String</Type>
            </Parameter>
            <Parameter Name="y">
                <ParamFlags>[opt]</ParamFlags>
                <Type>Decimal</Type>
                <Default>5.5</Default>
            </Parameter>
            <Parameter Name="value">
                <ParamFlags>[in] marshal</ParamFlags>
                <Attributes>
                    <TestAttribute2>
                        <ctor>Sub TestAttribute2..ctor(x As System.String)</ctor>
                        <a>TestAttribute2_P4_value</a>
                    </TestAttribute2>
                </Attributes>
                <Type>String</Type>
            </Parameter>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Method Name="set_P5" CallingConvention="HasThis">
            <MethodFlags>public newslot strict specialname abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>10</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Parameter Name="value">
                <ParamFlags></ParamFlags>
                <Type>Byte</Type>
            </Parameter>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Method Name="get_P6" CallingConvention="HasThis">
            <MethodFlags>public newslot strict specialname abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>11</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Byte</Type>
            </Return>
        </Method>
        <Property Name="P1">
            <PropertyFlags></PropertyFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>2</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Get>Function ComClassTest._ComClassTest.get_P1() As System.Int32</Get>
            <Set>Sub ComClassTest._ComClassTest.set_P1(value As System.Int32)</Set>
        </Property>
        <Property Name="P2">
            <PropertyFlags></PropertyFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>7</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Get>Function ComClassTest._ComClassTest.get_P2() As System.String</Get>
        </Property>
        <Property Name="P3">
            <PropertyFlags></PropertyFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>8</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Set>Sub ComClassTest._ComClassTest.set_P3(value As System.String)</Set>
        </Property>
        <Property Name="P4">
            <PropertyFlags></PropertyFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>9</a>
                </System.Runtime.InteropServices.DispIdAttribute>
                <TestAttribute1>
                    <ctor>Sub TestAttribute1..ctor(x As System.String)</ctor>
                    <a>TestAttribute1_P4</a>
                </TestAttribute1>
            </Attributes>
            <Get>Function ComClassTest._ComClassTest.get_P4(x As System.String, [y As System.Decimal = 5.5]) As System.String</Get>
            <Set>Sub ComClassTest._ComClassTest.set_P4(x As System.String, [y As System.Decimal = 5.5], value As System.String)</Set>
        </Property>
        <Property Name="P5">
            <PropertyFlags></PropertyFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>10</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Set>Sub ComClassTest._ComClassTest.set_P5(value As System.Byte)</Set>
        </Property>
        <Property Name="P6">
            <PropertyFlags></PropertyFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>11</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Get>Function ComClassTest._ComClassTest.get_P6() As System.Byte</Get>
        </Property>
    </Interface>
    <Interface Name="__ComClassTest">
        <TypeDefFlags>interface nested public abstract auto ansi</TypeDefFlags>
        <Attributes>
            <System.Runtime.InteropServices.InterfaceTypeAttribute>
                <ctor>Sub System.Runtime.InteropServices.InterfaceTypeAttribute..ctor(interfaceType As System.Int16)</ctor>
                <a>2</a>
            </System.Runtime.InteropServices.InterfaceTypeAttribute>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>True</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
        </Attributes>
        <Method Name="E1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>1</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Parameter Name="x">
                <ParamFlags></ParamFlags>
                <Type>Byte</Type>
            </Parameter>
            <Parameter Name="y">
                <ParamFlags></ParamFlags>
                <Type ByRef="True">String</Type>
            </Parameter>
            <Parameter Name="z">
                <ParamFlags></ParamFlags>
                <Type>String</Type>
            </Parameter>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Method Name="E2" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>2</a>
                </System.Runtime.InteropServices.DispIdAttribute>
                <TestAttribute1>
                    <ctor>Sub TestAttribute1..ctor(x As System.String)</ctor>
                    <a>TestAttribute1_E2</a>
                </TestAttribute1>
            </Attributes>
            <Parameter Name="x">
                <ParamFlags></ParamFlags>
                <Type>Byte</Type>
            </Parameter>
            <Parameter Name="y">
                <ParamFlags></ParamFlags>
                <Type ByRef="True">String</Type>
            </Parameter>
            <Parameter Name="z">
                <ParamFlags></ParamFlags>
                <Type>String</Type>
            </Parameter>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
    </Interface>
</Class>

            ' Strip TODO comments from the base-line.
            For Each d In expected.DescendantNodes.ToArray()
                Dim comment = TryCast(d, XComment)
                If comment IsNot Nothing Then
                    comment.Remove()
                End If
            Next

            Dim verifier = CompileAndVerify(compilationDef,
                                            options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal),
                                            symbolValidator:=Sub(m As ModuleSymbol)
                                                                 Dim pe = DirectCast(m, PEModuleSymbol)
                                                                 AssertReflection(expected,
                                                                                  ReflectComClass(pe, "ComClassTest"))
                                                             End Sub).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub SimpleTest2()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
<Microsoft.VisualBasic.ComClass()>
Public Class ComClassTest
    Sub M1()
    End Sub
End Class
    ]]></file>
</compilation>

            Dim expected =
<Class Name="ComClassTest">
    <TypeDefFlags>public auto ansi</TypeDefFlags>
    <Attributes>
        <System.Runtime.InteropServices.ClassInterfaceAttribute>
            <ctor>Sub System.Runtime.InteropServices.ClassInterfaceAttribute..ctor(classInterfaceType As System.Runtime.InteropServices.ClassInterfaceType)</ctor>
            <a>0</a>
        </System.Runtime.InteropServices.ClassInterfaceAttribute>
        <Microsoft.VisualBasic.ComClassAttribute>
            <ctor>Sub Microsoft.VisualBasic.ComClassAttribute..ctor()</ctor>
        </Microsoft.VisualBasic.ComClassAttribute>
    </Attributes>
    <Implements>ComClassTest._ComClassTest</Implements>
    <Method Name=".ctor" CallingConvention="HasThis">
        <MethodFlags>public specialname rtspecialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="M1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest._ComClassTest.M1()</Implements>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Interface Name="_ComClassTest">
        <TypeDefFlags>interface nested public abstract auto ansi</TypeDefFlags>
        <Attributes>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>True</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
        </Attributes>
        <Method Name="M1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>1</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
    </Interface>
</Class>


            Dim verifier = CompileAndVerify(compilationDef,
                                            options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal),
                                            symbolValidator:=Sub(m As ModuleSymbol)
                                                                 Dim pe = DirectCast(m, PEModuleSymbol)
                                                                 AssertReflection(expected,
                                                                                  ReflectComClass(pe, "ComClassTest"))
                                                             End Sub).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub SimpleTest3()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
<Microsoft.VisualBasic.ComClass("7666AC25-855F-4534-BC55-27BF09D49D46")>
Public Class ComClassTest
    Event E1 As System.Action
End Class
    ]]></file>
</compilation>

            Dim expected =
<Class Name="ComClassTest">
    <TypeDefFlags>public auto ansi</TypeDefFlags>
    <Attributes>
        <System.Runtime.InteropServices.GuidAttribute>
            <ctor>Sub System.Runtime.InteropServices.GuidAttribute..ctor(guid As System.String)</ctor>
            <a>7666AC25-855F-4534-BC55-27BF09D49D46</a>
        </System.Runtime.InteropServices.GuidAttribute>
        <System.Runtime.InteropServices.ClassInterfaceAttribute>
            <ctor>Sub System.Runtime.InteropServices.ClassInterfaceAttribute..ctor(classInterfaceType As System.Runtime.InteropServices.ClassInterfaceType)</ctor>
            <a>0</a>
        </System.Runtime.InteropServices.ClassInterfaceAttribute>
        <System.Runtime.InteropServices.ComSourceInterfacesAttribute>
            <ctor>Sub System.Runtime.InteropServices.ComSourceInterfacesAttribute..ctor(sourceInterfaces As System.String)</ctor>
            <a>ComClassTest+__ComClassTest</a>
        </System.Runtime.InteropServices.ComSourceInterfacesAttribute>
        <Microsoft.VisualBasic.ComClassAttribute>
            <ctor>Sub Microsoft.VisualBasic.ComClassAttribute..ctor(_ClassID As System.String)</ctor>
            <a>7666AC25-855F-4534-BC55-27BF09D49D46</a>
        </Microsoft.VisualBasic.ComClassAttribute>
    </Attributes>
    <Implements>ComClassTest._ComClassTest</Implements>
    <Method Name=".ctor" CallingConvention="HasThis">
        <MethodFlags>public specialname rtspecialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="add_E1" CallingConvention="HasThis">
        <MethodFlags>public specialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.CompilerServices.CompilerGeneratedAttribute>
                <ctor>Sub System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor()</ctor>
            </System.Runtime.CompilerServices.CompilerGeneratedAttribute>
        </Attributes>
        <Parameter Name="obj">
            <ParamFlags></ParamFlags>
            <Type>System.Action</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="remove_E1" CallingConvention="HasThis">
        <MethodFlags>public specialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.CompilerServices.CompilerGeneratedAttribute>
                <ctor>Sub System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor()</ctor>
            </System.Runtime.CompilerServices.CompilerGeneratedAttribute>
        </Attributes>
        <Parameter Name="obj">
            <ParamFlags></ParamFlags>
            <Type>System.Action</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Event Name="E1">
        <EventFlags></EventFlags>
        <Add>Sub ComClassTest.add_E1(obj As System.Action)</Add>
        <Remove>Sub ComClassTest.remove_E1(obj As System.Action)</Remove>
    </Event>
    <Interface Name="_ComClassTest">
        <TypeDefFlags>interface nested public abstract auto ansi</TypeDefFlags>
        <Attributes>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>True</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
        </Attributes>
    </Interface>
    <Interface Name="__ComClassTest">
        <TypeDefFlags>interface nested public abstract auto ansi</TypeDefFlags>
        <Attributes>
            <System.Runtime.InteropServices.InterfaceTypeAttribute>
                <ctor>Sub System.Runtime.InteropServices.InterfaceTypeAttribute..ctor(interfaceType As System.Int16)</ctor>
                <a>2</a>
            </System.Runtime.InteropServices.InterfaceTypeAttribute>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>True</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
        </Attributes>
        <Method Name="E1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>1</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
    </Interface>
</Class>

            Dim verifier = CompileAndVerify(compilationDef,
                                            options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal),
                                            symbolValidator:=Sub(m As ModuleSymbol)
                                                                 Dim pe = DirectCast(m, PEModuleSymbol)
                                                                 AssertReflection(expected,
                                                                                  ReflectComClass(pe, "ComClassTest"))
                                                             End Sub).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub SimpleTest4()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
<Microsoft.VisualBasic.ComClass("7666AC25-855F-4534-BC55-27BF09D49D46", "54388137-8A76-491e-AA3A-853E23AC1217")>
Public Class ComClassTest
    Sub M1()
    End Sub
End Class
    ]]></file>
</compilation>

            Dim expected =
<Class Name="ComClassTest">
    <TypeDefFlags>public auto ansi</TypeDefFlags>
    <Attributes>
        <System.Runtime.InteropServices.GuidAttribute>
            <ctor>Sub System.Runtime.InteropServices.GuidAttribute..ctor(guid As System.String)</ctor>
            <a>7666AC25-855F-4534-BC55-27BF09D49D46</a>
        </System.Runtime.InteropServices.GuidAttribute>
        <System.Runtime.InteropServices.ClassInterfaceAttribute>
            <ctor>Sub System.Runtime.InteropServices.ClassInterfaceAttribute..ctor(classInterfaceType As System.Runtime.InteropServices.ClassInterfaceType)</ctor>
            <a>0</a>
        </System.Runtime.InteropServices.ClassInterfaceAttribute>
        <Microsoft.VisualBasic.ComClassAttribute>
            <ctor>Sub Microsoft.VisualBasic.ComClassAttribute..ctor(_ClassID As System.String, _InterfaceID As System.String)</ctor>
            <a>7666AC25-855F-4534-BC55-27BF09D49D46</a>
            <a>54388137-8A76-491e-AA3A-853E23AC1217</a>
        </Microsoft.VisualBasic.ComClassAttribute>
    </Attributes>
    <Implements>ComClassTest._ComClassTest</Implements>
    <Method Name=".ctor" CallingConvention="HasThis">
        <MethodFlags>public specialname rtspecialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="M1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest._ComClassTest.M1()</Implements>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Interface Name="_ComClassTest">
        <TypeDefFlags>interface nested public abstract auto ansi</TypeDefFlags>
        <Attributes>
            <System.Runtime.InteropServices.GuidAttribute>
                <ctor>Sub System.Runtime.InteropServices.GuidAttribute..ctor(guid As System.String)</ctor>
                <a>54388137-8A76-491e-AA3A-853E23AC1217</a>
            </System.Runtime.InteropServices.GuidAttribute>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>True</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
        </Attributes>
        <Method Name="M1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>1</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
    </Interface>
</Class>

            Dim verifier = CompileAndVerify(compilationDef,
                                            options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal),
                                            symbolValidator:=Sub(m As ModuleSymbol)
                                                                 Dim pe = DirectCast(m, PEModuleSymbol)
                                                                 AssertReflection(expected,
                                                                                  ReflectComClass(pe, "ComClassTest"))
                                                             End Sub).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub SimpleTest5()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

<Microsoft.VisualBasic.ComClass()>
Public Class ComClassTest
End Class
    ]]></file>
</compilation>

            Dim expected =
<Class Name="ComClassTest">
    <TypeDefFlags>public auto ansi</TypeDefFlags>
    <Attributes>
        <System.Runtime.InteropServices.ClassInterfaceAttribute>
            <ctor>Sub System.Runtime.InteropServices.ClassInterfaceAttribute..ctor(classInterfaceType As System.Runtime.InteropServices.ClassInterfaceType)</ctor>
            <a>0</a>
        </System.Runtime.InteropServices.ClassInterfaceAttribute>
        <Microsoft.VisualBasic.ComClassAttribute>
            <ctor>Sub Microsoft.VisualBasic.ComClassAttribute..ctor()</ctor>
        </Microsoft.VisualBasic.ComClassAttribute>
    </Attributes>
    <Method Name=".ctor" CallingConvention="HasThis">
        <MethodFlags>public specialname rtspecialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
</Class>

            Dim verifier = CompileAndVerify(compilationDef,
                                            options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal),
                                            symbolValidator:=Sub(m As ModuleSymbol)
                                                                 Dim pe = DirectCast(m, PEModuleSymbol)
                                                                 AssertReflection(expected,
                                                                                  ReflectComClass(pe, "ComClassTest"))
                                                             End Sub)

            AssertTheseDiagnostics(verifier.Compilation,
<expected>
BC40011: 'Microsoft.VisualBasic.ComClassAttribute' is specified for class 'ComClassTest' but 'ComClassTest' has no public members that can be exposed to COM; therefore, no COM interfaces are generated.
Public Class ComClassTest
             ~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub SimpleTest6()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
<Microsoft.VisualBasic.ComClass("7666AC25-855F-4534-BC55-27BF09D49D46", "54388137-8A76-491e-AA3A-853E23AC1217", "EA329A13-16A0-478d-B41F-47583A761FF2", InterfaceShadows:=True)>
Public Class ComClassTest
    Sub M1()
        Dim x as Integer = 12

        Dim y = Function() x
    End Sub
End Class
    ]]></file>
</compilation>

            Dim expected =
<Class Name="ComClassTest">
    <TypeDefFlags>public auto ansi</TypeDefFlags>
    <Attributes>
        <System.Runtime.InteropServices.GuidAttribute>
            <ctor>Sub System.Runtime.InteropServices.GuidAttribute..ctor(guid As System.String)</ctor>
            <a>7666AC25-855F-4534-BC55-27BF09D49D46</a>
        </System.Runtime.InteropServices.GuidAttribute>
        <System.Runtime.InteropServices.ClassInterfaceAttribute>
            <ctor>Sub System.Runtime.InteropServices.ClassInterfaceAttribute..ctor(classInterfaceType As System.Runtime.InteropServices.ClassInterfaceType)</ctor>
            <a>0</a>
        </System.Runtime.InteropServices.ClassInterfaceAttribute>
        <Microsoft.VisualBasic.ComClassAttribute>
            <ctor>Sub Microsoft.VisualBasic.ComClassAttribute..ctor(_ClassID As System.String, _InterfaceID As System.String, _EventId As System.String)</ctor>
            <a>7666AC25-855F-4534-BC55-27BF09D49D46</a>
            <a>54388137-8A76-491e-AA3A-853E23AC1217</a>
            <a>EA329A13-16A0-478d-B41F-47583A761FF2</a>
            <Named Name="InterfaceShadows">True</Named>
        </Microsoft.VisualBasic.ComClassAttribute>
    </Attributes>
    <Implements>ComClassTest._ComClassTest</Implements>
    <Method Name=".ctor" CallingConvention="HasThis">
        <MethodFlags>public specialname rtspecialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="M1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest._ComClassTest.M1()</Implements>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Interface Name="_ComClassTest">
        <TypeDefFlags>interface nested public abstract auto ansi</TypeDefFlags>
        <Attributes>
            <System.Runtime.InteropServices.GuidAttribute>
                <ctor>Sub System.Runtime.InteropServices.GuidAttribute..ctor(guid As System.String)</ctor>
                <a>54388137-8A76-491e-AA3A-853E23AC1217</a>
            </System.Runtime.InteropServices.GuidAttribute>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>True</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
        </Attributes>
        <Method Name="M1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>1</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
    </Interface>
    <Class Name="_Closure$__1-0">
        <TypeDefFlags>nested assembly auto ansi sealed</TypeDefFlags>
        <Attributes>
            <System.Runtime.CompilerServices.CompilerGeneratedAttribute>
                <ctor>Sub System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor()</ctor>
            </System.Runtime.CompilerServices.CompilerGeneratedAttribute>
        </Attributes>
        <Field Name="$VB$Local_x">
            <FieldFlags>public instance</FieldFlags>
            <Type>Integer</Type>
        </Field>
        <Method Name=".ctor" CallingConvention="HasThis">
            <MethodFlags>public specialname rtspecialname instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Method Name="_Lambda$__0" CallingConvention="HasThis">
            <MethodFlags>assembly specialname instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Return>
                <Type>Integer</Type>
            </Return>
        </Method>
    </Class>
</Class>

            Dim verifier = CompileAndVerify(compilationDef,
                                            options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal),
                                            symbolValidator:=Sub(m As ModuleSymbol)
                                                                 Dim pe = DirectCast(m, PEModuleSymbol)
                                                                 AssertReflection(expected,
                                                                                  ReflectComClass(pe, "ComClassTest"))
                                                             End Sub).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub Test_ERR_ComClassOnGeneric()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

<Microsoft.VisualBasic.ComClass()>
Public Class ComClassTest(Of T)
End Class

Public Class ComClassTest1(Of T)
    <Microsoft.VisualBasic.ComClass()>
    Public Class ComClassTest2
    End Class
End Class

    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim expected =
<expected>
BC31527: 'Microsoft.VisualBasic.ComClassAttribute' cannot be applied to a class that is generic or contained inside a generic type.
Public Class ComClassTest(Of T)
             ~~~~~~~~~~~~
BC31527: 'Microsoft.VisualBasic.ComClassAttribute' cannot be applied to a class that is generic or contained inside a generic type.
    Public Class ComClassTest2
                 ~~~~~~~~~~~~~
</expected>

            AssertTheseDeclarationDiagnostics(compilation, expected)
            AssertTheseDiagnostics(compilation, expected)
        End Sub

        <Fact()>
        Public Sub Test_ERR_BadAttributeUuid2()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

<Microsoft.VisualBasic.ComClass("1", "2", "3")>
Public Class ComClassTest
    Public Sub Foo()
    End Sub
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim expected =
<expected>
BC32500: 'ComClassAttribute' cannot be applied because the format of the GUID '1' is not correct.
Public Class ComClassTest
             ~~~~~~~~~~~~
BC32500: 'ComClassAttribute' cannot be applied because the format of the GUID '2' is not correct.
Public Class ComClassTest
             ~~~~~~~~~~~~
BC32500: 'ComClassAttribute' cannot be applied because the format of the GUID '3' is not correct.
Public Class ComClassTest
             ~~~~~~~~~~~~
</expected>

            AssertTheseDeclarationDiagnostics(compilation, expected)
            AssertTheseDiagnostics(compilation, expected)
        End Sub

        <Fact>
        Public Sub Test_ERR_ComClassDuplicateGuids1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

<Microsoft.VisualBasic.ComClass("7666AC25-855F-4534-BC55-27BF09D49D46", "7666AC25-855F-4534-BC55-27BF09D49D46", "7666AC25-855F-4534-BC55-27BF09D49D46")>
Public Class ComClassTest1
    Public Sub Foo()
    End Sub
End Class

<Microsoft.VisualBasic.ComClass("7666AC25-855F-4534-BC55-27BF09D49D46", "7666AC25-855F-4534-BC55-27BF09D49D46", "")>
Public Class ComClassTest2
    Public Sub Foo()
    End Sub
End Class

<Microsoft.VisualBasic.ComClass("7666AC25-855F-4534-BC55-27BF09D49D46", "", "7666AC25-855F-4534-BC55-27BF09D49D46")>
Public Class ComClassTest3
    Public Sub Foo()
    End Sub
End Class

<Microsoft.VisualBasic.ComClass("", "00000000-0000-0000-0000-000000000000", "")>
Public Class ComClassTest4
    Public Sub Foo()
    End Sub
End Class

<Microsoft.VisualBasic.ComClass("", "", "00000000-0000-0000-0000-000000000000")>
Public Class ComClassTest5
    Public Sub Foo()
    End Sub
End Class

<Microsoft.VisualBasic.ComClass("", "00000000-0000-0000-0000-000000000000", "0")>
Public Class ComClassTest6
    Public Sub Foo()
    End Sub
End Class

<Microsoft.VisualBasic.ComClass("", "0", "00000000-0000-0000-0000-000000000000")>
Public Class ComClassTest7
    Public Sub Foo()
    End Sub
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim expected =
<expected>
BC32507: 'InterfaceId' and 'EventsId' parameters for 'Microsoft.VisualBasic.ComClassAttribute' on 'ComClassTest1' cannot have the same value.
Public Class ComClassTest1
             ~~~~~~~~~~~~~
BC32500: 'ComClassAttribute' cannot be applied because the format of the GUID '0' is not correct.
Public Class ComClassTest6
             ~~~~~~~~~~~~~
BC32500: 'ComClassAttribute' cannot be applied because the format of the GUID '0' is not correct.
Public Class ComClassTest7
             ~~~~~~~~~~~~~
</expected>

            AssertTheseDeclarationDiagnostics(compilation, expected)
            AssertTheseDiagnostics(compilation, expected)
        End Sub

        <Fact>
        Public Sub Test_ERR_ComClassAndReservedAttribute1_Guid()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

<Microsoft.VisualBasic.ComClass(), Guid("7666AC25-855F-4534-BC55-27BF09D49D46")>
Public Class ComClassTest
    Public Sub Foo()
    End Sub
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim expected =
<expected>
BC32501: 'Microsoft.VisualBasic.ComClassAttribute' and 'GuidAttribute' cannot both be applied to the same class.
Public Class ComClassTest
             ~~~~~~~~~~~~
</expected>

            AssertTheseDeclarationDiagnostics(compilation, expected)
            AssertTheseDiagnostics(compilation, expected)
        End Sub

        <Fact>
        Public Sub Test_ERR_ComClassAndReservedAttribute1_ClassInterface()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

<Microsoft.VisualBasic.ComClass(), ClassInterface(0)>
Public Class ComClassTest1
    Public Sub Foo()
    End Sub
End Class

<Microsoft.VisualBasic.ComClass(), ClassInterface(ClassInterfaceType.None)>
Public Class ComClassTest2
    Public Sub Foo()
    End Sub
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim expected =
<expected>
BC32501: 'Microsoft.VisualBasic.ComClassAttribute' and 'ClassInterfaceAttribute' cannot both be applied to the same class.
Public Class ComClassTest1
             ~~~~~~~~~~~~~
BC32501: 'Microsoft.VisualBasic.ComClassAttribute' and 'ClassInterfaceAttribute' cannot both be applied to the same class.
Public Class ComClassTest2
             ~~~~~~~~~~~~~
</expected>

            AssertTheseDeclarationDiagnostics(compilation, expected)
            AssertTheseDiagnostics(compilation, expected)
        End Sub

        <Fact>
        Public Sub Test_ERR_ComClassAndReservedAttribute1_ComSourceInterfaces()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

<Microsoft.VisualBasic.ComClass(), ComSourceInterfaces("x")>
Public Class ComClassTest1
    Public Sub Foo()
    End Sub
End Class

<Microsoft.VisualBasic.ComClass(), ComSourceInterfaces(GetType(ComClassTest1))>
Public Class ComClassTest2
    Public Sub Foo()
    End Sub
End Class

<Microsoft.VisualBasic.ComClass(), ComSourceInterfaces(GetType(ComClassTest1), GetType(ComClassTest1))>
Public Class ComClassTest3
    Public Sub Foo()
    End Sub
End Class

<Microsoft.VisualBasic.ComClass(), ComSourceInterfaces(GetType(ComClassTest1), GetType(ComClassTest1), GetType(ComClassTest1))>
Public Class ComClassTest4
    Public Sub Foo()
    End Sub
End Class

<Microsoft.VisualBasic.ComClass(), ComSourceInterfaces(GetType(ComClassTest1), GetType(ComClassTest1), GetType(ComClassTest1), GetType(ComClassTest1))>
Public Class ComClassTest5
    Public Sub Foo()
    End Sub
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim expected =
<expected>
BC32501: 'Microsoft.VisualBasic.ComClassAttribute' and 'ComSourceInterfacesAttribute' cannot both be applied to the same class.
Public Class ComClassTest1
             ~~~~~~~~~~~~~
BC32501: 'Microsoft.VisualBasic.ComClassAttribute' and 'ComSourceInterfacesAttribute' cannot both be applied to the same class.
Public Class ComClassTest2
             ~~~~~~~~~~~~~
BC32501: 'Microsoft.VisualBasic.ComClassAttribute' and 'ComSourceInterfacesAttribute' cannot both be applied to the same class.
Public Class ComClassTest3
             ~~~~~~~~~~~~~
BC32501: 'Microsoft.VisualBasic.ComClassAttribute' and 'ComSourceInterfacesAttribute' cannot both be applied to the same class.
Public Class ComClassTest4
             ~~~~~~~~~~~~~
BC32501: 'Microsoft.VisualBasic.ComClassAttribute' and 'ComSourceInterfacesAttribute' cannot both be applied to the same class.
Public Class ComClassTest5
             ~~~~~~~~~~~~~
</expected>

            AssertTheseDeclarationDiagnostics(compilation, expected)
            AssertTheseDiagnostics(compilation, expected)
        End Sub

        <Fact>
        Public Sub Test_ERR_ComClassAndReservedAttribute1_ComVisible()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

<Microsoft.VisualBasic.ComClass(), ComVisible(False)>
Public Class ComClassTest1
    Public Sub Foo()
    End Sub
End Class

<Microsoft.VisualBasic.ComClass(), ComVisible(True)>
Public Class ComClassTest2
    Public Sub Foo()
    End Sub
End Class

<Microsoft.VisualBasic.ComClass(), ComVisible()>
Public Class ComClassTest3
    Public Sub Foo()
    End Sub
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim expected =
<expected><![CDATA[
BC32501: 'Microsoft.VisualBasic.ComClassAttribute' and 'ComVisibleAttribute(False)' cannot both be applied to the same class.
Public Class ComClassTest1
             ~~~~~~~~~~~~~
BC30455: Argument not specified for parameter 'visibility' of 'Public Overloads Sub New(visibility As Boolean)'.
<Microsoft.VisualBasic.ComClass(), ComVisible()>
                                   ~~~~~~~~~~
]]></expected>

            AssertTheseDeclarationDiagnostics(compilation, expected)
            AssertTheseDiagnostics(compilation, expected)
        End Sub

        <Fact>
        Public Sub Test_ERR_ComClassRequiresPublicClass1_ERR_ComClassRequiresPublicClass2()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

<Microsoft.VisualBasic.ComClass()>
Friend Class ComClassTest1
    Public Sub Foo()
    End Sub
End Class

Friend Class ComClassTest2
    Friend Class ComClassTest3
        <Microsoft.VisualBasic.ComClass()>
        Public Class ComClassTest4
            Public Sub Foo()
            End Sub
        End Class
    End Class
End Class

Friend Class ComClassTest5
    Public Class ComClassTest6
        <Microsoft.VisualBasic.ComClass()>
        Public Class ComClassTest7
            Public Sub Foo()
            End Sub
        End Class
    End Class
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim expected =
<expected>
BC32509: 'Microsoft.VisualBasic.ComClassAttribute' cannot be applied to 'ComClassTest1' because it is not declared 'Public'.
Friend Class ComClassTest1
             ~~~~~~~~~~~~~
BC32504: 'Microsoft.VisualBasic.ComClassAttribute' cannot be applied to 'ComClassTest4' because its container 'ComClassTest3' is not declared 'Public'.
        Public Class ComClassTest4
                     ~~~~~~~~~~~~~
BC32504: 'Microsoft.VisualBasic.ComClassAttribute' cannot be applied to 'ComClassTest7' because its container 'ComClassTest5' is not declared 'Public'.
        Public Class ComClassTest7
                     ~~~~~~~~~~~~~
</expected>

            AssertTheseDeclarationDiagnostics(compilation, expected)
            AssertTheseDiagnostics(compilation, expected)
        End Sub

        <Fact>
        Public Sub Test_ERR_ComClassCantBeAbstract0()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

<Microsoft.VisualBasic.ComClass()>
Public MustInherit Class ComClassTest1
    Public Sub Foo()
    End Sub
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim expected =
<expected>
BC32508: 'Microsoft.VisualBasic.ComClassAttribute' cannot be applied to a class that is declared 'MustInherit'.
Public MustInherit Class ComClassTest1
                         ~~~~~~~~~~~~~
</expected>

            AssertTheseDeclarationDiagnostics(compilation, expected)
            AssertTheseDiagnostics(compilation, expected)
        End Sub

        <Fact>
        Public Sub Test_ERR_MemberConflictWithSynth4()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

<Microsoft.VisualBasic.ComClass()>
Public Class ComClassTest1
    Public Sub Foo()
    End Sub

    Public Event E1()

    WithEvents ComClassTest1 As ComClassTest1

    Private Sub __ComClassTest1()
    End Sub

    Protected Sub __ComClassTest1(x As Integer)
    End Sub
End Class

<Microsoft.VisualBasic.ComClass(InterfaceShadows:=False)>
Public Class ComClassTest2
    Public Sub Foo()
    End Sub

    Public Event E1()

    WithEvents ComClassTest2 As ComClassTest2

    Private Sub __ComClassTest2()
    End Sub

    Protected Sub __ComClassTest2(x As Integer)
    End Sub
End Class

<Microsoft.VisualBasic.ComClass(InterfaceShadows:=True)>
Public Class ComClassTest3
    Public Sub Foo()
    End Sub

    Public Event E1()

    WithEvents ComClassTest3 As ComClassTest3

    Private Sub __ComClassTest3()
    End Sub

    Protected Sub __ComClassTest3(x As Integer)
    End Sub
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim expected =
<expected>
BC31058: Conflicts with 'Interface _ComClassTest1', which is implicitly declared for 'ComClassAttribute' in Class 'ComClassTest1'.
    WithEvents ComClassTest1 As ComClassTest1
               ~~~~~~~~~~~~~
BC31058: Conflicts with 'Interface __ComClassTest1', which is implicitly declared for 'ComClassAttribute' in Class 'ComClassTest1'.
    Private Sub __ComClassTest1()
                ~~~~~~~~~~~~~~~
BC31058: Conflicts with 'Interface __ComClassTest1', which is implicitly declared for 'ComClassAttribute' in Class 'ComClassTest1'.
    Protected Sub __ComClassTest1(x As Integer)
                  ~~~~~~~~~~~~~~~
BC31058: Conflicts with 'Interface _ComClassTest2', which is implicitly declared for 'ComClassAttribute' in Class 'ComClassTest2'.
    WithEvents ComClassTest2 As ComClassTest2
               ~~~~~~~~~~~~~
BC31058: Conflicts with 'Interface __ComClassTest2', which is implicitly declared for 'ComClassAttribute' in Class 'ComClassTest2'.
    Private Sub __ComClassTest2()
                ~~~~~~~~~~~~~~~
BC31058: Conflicts with 'Interface __ComClassTest2', which is implicitly declared for 'ComClassAttribute' in Class 'ComClassTest2'.
    Protected Sub __ComClassTest2(x As Integer)
                  ~~~~~~~~~~~~~~~
BC31058: Conflicts with 'Interface _ComClassTest3', which is implicitly declared for 'ComClassAttribute' in Class 'ComClassTest3'.
    WithEvents ComClassTest3 As ComClassTest3
               ~~~~~~~~~~~~~
BC31058: Conflicts with 'Interface __ComClassTest3', which is implicitly declared for 'ComClassAttribute' in Class 'ComClassTest3'.
    Private Sub __ComClassTest3()
                ~~~~~~~~~~~~~~~
BC31058: Conflicts with 'Interface __ComClassTest3', which is implicitly declared for 'ComClassAttribute' in Class 'ComClassTest3'.
    Protected Sub __ComClassTest3(x As Integer)
                  ~~~~~~~~~~~~~~~
</expected>

            AssertTheseDeclarationDiagnostics(compilation, expected)
            AssertTheseDiagnostics(compilation, expected)
        End Sub


        <Fact>
        Public Sub Test_WRN_ComClassInterfaceShadows5_1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

Public Class ComClassBase
    Private Sub _ComClassTest1()
    End Sub

    Private Sub __ComClassTest1()
    End Sub

    Protected Sub _ComClassTest1(x As Integer)
    End Sub
    Protected Sub __ComClassTest1(x As Integer)
    End Sub

    Friend Sub _ComClassTest1(x As Integer, y As Integer)
    End Sub
    Friend Sub __ComClassTest1(x As Integer, y As Integer)
    End Sub

    Protected Friend Sub _ComClassTest1(x As Integer, y As Integer, z As Integer)
    End Sub
    Protected Friend Sub __ComClassTest1(x As Integer, y As Integer, z As Integer)
    End Sub

    Public Sub _ComClassTest1(x As Long)
    End Sub
    Public Sub __ComClassTest1(x As Long)
    End Sub
End Class

<Microsoft.VisualBasic.ComClass()>
Public Class ComClassTest1
    Inherits ComClassBase

    Public Sub Foo()
    End Sub

    Public Event E1()
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim expected =
<expected>
BC42101: 'Microsoft.VisualBasic.ComClassAttribute' on class 'ComClassTest1' implicitly declares Interface '__ComClassTest1', which conflicts with a member of the same name in Class 'ComClassBase'. Use 'Microsoft.VisualBasic.ComClassAttribute(InterfaceShadows:=True)' if you want to hide the name on the base ComClassBase.
Public Class ComClassTest1
             ~~~~~~~~~~~~~
BC42101: 'Microsoft.VisualBasic.ComClassAttribute' on class 'ComClassTest1' implicitly declares Interface '__ComClassTest1', which conflicts with a member of the same name in Class 'ComClassBase'. Use 'Microsoft.VisualBasic.ComClassAttribute(InterfaceShadows:=True)' if you want to hide the name on the base ComClassBase.
Public Class ComClassTest1
             ~~~~~~~~~~~~~
BC42101: 'Microsoft.VisualBasic.ComClassAttribute' on class 'ComClassTest1' implicitly declares Interface '__ComClassTest1', which conflicts with a member of the same name in Class 'ComClassBase'. Use 'Microsoft.VisualBasic.ComClassAttribute(InterfaceShadows:=True)' if you want to hide the name on the base ComClassBase.
Public Class ComClassTest1
             ~~~~~~~~~~~~~
BC42101: 'Microsoft.VisualBasic.ComClassAttribute' on class 'ComClassTest1' implicitly declares Interface '__ComClassTest1', which conflicts with a member of the same name in Class 'ComClassBase'. Use 'Microsoft.VisualBasic.ComClassAttribute(InterfaceShadows:=True)' if you want to hide the name on the base ComClassBase.
Public Class ComClassTest1
             ~~~~~~~~~~~~~
BC42101: 'Microsoft.VisualBasic.ComClassAttribute' on class 'ComClassTest1' implicitly declares Interface '_ComClassTest1', which conflicts with a member of the same name in Class 'ComClassBase'. Use 'Microsoft.VisualBasic.ComClassAttribute(InterfaceShadows:=True)' if you want to hide the name on the base ComClassBase.
Public Class ComClassTest1
             ~~~~~~~~~~~~~
BC42101: 'Microsoft.VisualBasic.ComClassAttribute' on class 'ComClassTest1' implicitly declares Interface '_ComClassTest1', which conflicts with a member of the same name in Class 'ComClassBase'. Use 'Microsoft.VisualBasic.ComClassAttribute(InterfaceShadows:=True)' if you want to hide the name on the base ComClassBase.
Public Class ComClassTest1
             ~~~~~~~~~~~~~
BC42101: 'Microsoft.VisualBasic.ComClassAttribute' on class 'ComClassTest1' implicitly declares Interface '_ComClassTest1', which conflicts with a member of the same name in Class 'ComClassBase'. Use 'Microsoft.VisualBasic.ComClassAttribute(InterfaceShadows:=True)' if you want to hide the name on the base ComClassBase.
Public Class ComClassTest1
             ~~~~~~~~~~~~~
BC42101: 'Microsoft.VisualBasic.ComClassAttribute' on class 'ComClassTest1' implicitly declares Interface '_ComClassTest1', which conflicts with a member of the same name in Class 'ComClassBase'. Use 'Microsoft.VisualBasic.ComClassAttribute(InterfaceShadows:=True)' if you want to hide the name on the base ComClassBase.
Public Class ComClassTest1
             ~~~~~~~~~~~~~
</expected>

            AssertTheseDeclarationDiagnostics(compilation, expected)
            AssertTheseDiagnostics(compilation, expected)
        End Sub

        <Fact>
        Public Sub Test_WRN_ComClassInterfaceShadows5_2()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

Public Class ComClassBase
    Private Sub _ComClassTest1()
    End Sub

    Private Sub __ComClassTest1()
    End Sub

    Protected Sub _ComClassTest1(x As Integer)
    End Sub
    Protected Sub __ComClassTest1(x As Integer)
    End Sub

    Friend Sub _ComClassTest1(x As Integer, y As Integer)
    End Sub
    Friend Sub __ComClassTest1(x As Integer, y As Integer)
    End Sub

    Protected Friend Sub _ComClassTest1(x As Integer, y As Integer, z As Integer)
    End Sub
    Protected Friend Sub __ComClassTest1(x As Integer, y As Integer, z As Integer)
    End Sub

    Public Sub _ComClassTest1(x As Long)
    End Sub
    Public Sub __ComClassTest1(x As Long)
    End Sub
End Class

<Microsoft.VisualBasic.ComClass(InterfaceShadows:=False)>
Public Class ComClassTest1
    Inherits ComClassBase

    Public Sub Foo()
    End Sub

    Public Event E1()
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim expected =
<expected>
BC42101: 'Microsoft.VisualBasic.ComClassAttribute' on class 'ComClassTest1' implicitly declares Interface '__ComClassTest1', which conflicts with a member of the same name in Class 'ComClassBase'. Use 'Microsoft.VisualBasic.ComClassAttribute(InterfaceShadows:=True)' if you want to hide the name on the base ComClassBase.
Public Class ComClassTest1
             ~~~~~~~~~~~~~
BC42101: 'Microsoft.VisualBasic.ComClassAttribute' on class 'ComClassTest1' implicitly declares Interface '__ComClassTest1', which conflicts with a member of the same name in Class 'ComClassBase'. Use 'Microsoft.VisualBasic.ComClassAttribute(InterfaceShadows:=True)' if you want to hide the name on the base ComClassBase.
Public Class ComClassTest1
             ~~~~~~~~~~~~~
BC42101: 'Microsoft.VisualBasic.ComClassAttribute' on class 'ComClassTest1' implicitly declares Interface '__ComClassTest1', which conflicts with a member of the same name in Class 'ComClassBase'. Use 'Microsoft.VisualBasic.ComClassAttribute(InterfaceShadows:=True)' if you want to hide the name on the base ComClassBase.
Public Class ComClassTest1
             ~~~~~~~~~~~~~
BC42101: 'Microsoft.VisualBasic.ComClassAttribute' on class 'ComClassTest1' implicitly declares Interface '__ComClassTest1', which conflicts with a member of the same name in Class 'ComClassBase'. Use 'Microsoft.VisualBasic.ComClassAttribute(InterfaceShadows:=True)' if you want to hide the name on the base ComClassBase.
Public Class ComClassTest1
             ~~~~~~~~~~~~~
BC42101: 'Microsoft.VisualBasic.ComClassAttribute' on class 'ComClassTest1' implicitly declares Interface '_ComClassTest1', which conflicts with a member of the same name in Class 'ComClassBase'. Use 'Microsoft.VisualBasic.ComClassAttribute(InterfaceShadows:=True)' if you want to hide the name on the base ComClassBase.
Public Class ComClassTest1
             ~~~~~~~~~~~~~
BC42101: 'Microsoft.VisualBasic.ComClassAttribute' on class 'ComClassTest1' implicitly declares Interface '_ComClassTest1', which conflicts with a member of the same name in Class 'ComClassBase'. Use 'Microsoft.VisualBasic.ComClassAttribute(InterfaceShadows:=True)' if you want to hide the name on the base ComClassBase.
Public Class ComClassTest1
             ~~~~~~~~~~~~~
BC42101: 'Microsoft.VisualBasic.ComClassAttribute' on class 'ComClassTest1' implicitly declares Interface '_ComClassTest1', which conflicts with a member of the same name in Class 'ComClassBase'. Use 'Microsoft.VisualBasic.ComClassAttribute(InterfaceShadows:=True)' if you want to hide the name on the base ComClassBase.
Public Class ComClassTest1
             ~~~~~~~~~~~~~
BC42101: 'Microsoft.VisualBasic.ComClassAttribute' on class 'ComClassTest1' implicitly declares Interface '_ComClassTest1', which conflicts with a member of the same name in Class 'ComClassBase'. Use 'Microsoft.VisualBasic.ComClassAttribute(InterfaceShadows:=True)' if you want to hide the name on the base ComClassBase.
Public Class ComClassTest1
             ~~~~~~~~~~~~~
</expected>

            AssertTheseDeclarationDiagnostics(compilation, expected)
            AssertTheseDiagnostics(compilation, expected)
        End Sub

        <Fact>
        Public Sub Test_WRN_ComClassInterfaceShadows5_3()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

Public Class ComClassBase
    Private Sub _ComClassTest1()
    End Sub

    Private Sub __ComClassTest1()
    End Sub

    Protected Sub _ComClassTest1(x As Integer)
    End Sub
    Protected Sub __ComClassTest1(x As Integer)
    End Sub

    Friend Sub _ComClassTest1(x As Integer, y As Integer)
    End Sub
    Friend Sub __ComClassTest1(x As Integer, y As Integer)
    End Sub

    Protected Friend Sub _ComClassTest1(x As Integer, y As Integer, z As Integer)
    End Sub
    Protected Friend Sub __ComClassTest1(x As Integer, y As Integer, z As Integer)
    End Sub

    Public Sub _ComClassTest1(x As Long)
    End Sub
    Public Sub __ComClassTest1(x As Long)
    End Sub
End Class

<Microsoft.VisualBasic.ComClass(InterfaceShadows:=True)>
Public Class ComClassTest1
    Inherits ComClassBase

    Public Sub Foo()
    End Sub

    Public Event E1()
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim expected =
<expected>
</expected>

            AssertTheseDeclarationDiagnostics(compilation, expected)
            AssertTheseDiagnostics(compilation, expected)
        End Sub

        <Fact>
        Public Sub Test_WRN_ComClassPropertySetObject1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

<Microsoft.VisualBasic.ComClass()>
Public Class ComClassTest1

    Public Property P1 As Object
        Get
            Return Nothing
        End Get
        Set(value As Object)
        End Set
    End Property

    Public WriteOnly Property P2 As Object
        Set(value As Object)
        End Set
    End Property

    Public ReadOnly Property P3 As Object
        Get
            Return Nothing
        End Get
    End Property
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim expected =
<expected>
BC42102: 'Public Property P1 As Object' cannot be exposed to COM as a property 'Let'. You will not be able to assign non-object values (such as numbers or strings) to this property from Visual Basic 6.0 using a 'Let' statement.
    Public Property P1 As Object
                    ~~
BC42102: 'Public WriteOnly Property P2 As Object' cannot be exposed to COM as a property 'Let'. You will not be able to assign non-object values (such as numbers or strings) to this property from Visual Basic 6.0 using a 'Let' statement.
    Public WriteOnly Property P2 As Object
                              ~~
</expected>

            AssertTheseDeclarationDiagnostics(compilation, expected)
            AssertTheseDiagnostics(compilation, expected)
        End Sub


        <Fact>
        Public Sub Test_ERR_ComClassGenericMethod()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

<Microsoft.VisualBasic.ComClass()>
Public Class ComClassTest1
    Public Sub Foo(Of T)()
    End Sub
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim expected =
<expected>
BC30943: Generic methods cannot be exposed to COM.
    Public Sub Foo(Of T)()
               ~~~
</expected>

            AssertTheseDeclarationDiagnostics(compilation, expected)
            AssertTheseDiagnostics(compilation, expected)
        End Sub

        <Fact()>
        Public Sub ComClassWithWarnings()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

Public Class ComClassBase
    Public Sub _ComClassTest1()
    End Sub
    Public Sub __ComClassTest1()
    End Sub
End Class

<Microsoft.VisualBasic.ComClass()>
Public Class ComClassTest1
    Inherits ComClassBase

    Public Sub M1()
    End Sub

    Public Event E1()

    Public WriteOnly Property P2 As Object
        Set(value As Object)
        End Set
    End Property

End Class
    ]]></file>
</compilation>

            Dim expected =
<Class Name="ComClassTest1">
    <TypeDefFlags>public auto ansi</TypeDefFlags>
    <Attributes>
        <System.Runtime.InteropServices.ClassInterfaceAttribute>
            <ctor>Sub System.Runtime.InteropServices.ClassInterfaceAttribute..ctor(classInterfaceType As System.Runtime.InteropServices.ClassInterfaceType)</ctor>
            <a>0</a>
        </System.Runtime.InteropServices.ClassInterfaceAttribute>
        <System.Runtime.InteropServices.ComSourceInterfacesAttribute>
            <ctor>Sub System.Runtime.InteropServices.ComSourceInterfacesAttribute..ctor(sourceInterfaces As System.String)</ctor>
            <a>ComClassTest1+__ComClassTest1</a>
        </System.Runtime.InteropServices.ComSourceInterfacesAttribute>
        <Microsoft.VisualBasic.ComClassAttribute>
            <ctor>Sub Microsoft.VisualBasic.ComClassAttribute..ctor()</ctor>
        </Microsoft.VisualBasic.ComClassAttribute>
    </Attributes>
    <Implements>ComClassTest1._ComClassTest1</Implements>
    <Method Name=".ctor" CallingConvention="HasThis">
        <MethodFlags>public specialname rtspecialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="M1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest1._ComClassTest1.M1()</Implements>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="add_E1" CallingConvention="HasThis">
        <MethodFlags>public specialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.CompilerServices.CompilerGeneratedAttribute>
                <ctor>Sub System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor()</ctor>
            </System.Runtime.CompilerServices.CompilerGeneratedAttribute>
        </Attributes>
        <Parameter Name="obj">
            <ParamFlags></ParamFlags>
            <Type>ComClassTest1.E1EventHandler</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="remove_E1" CallingConvention="HasThis">
        <MethodFlags>public specialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.CompilerServices.CompilerGeneratedAttribute>
                <ctor>Sub System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor()</ctor>
            </System.Runtime.CompilerServices.CompilerGeneratedAttribute>
        </Attributes>
        <Parameter Name="obj">
            <ParamFlags></ParamFlags>
            <Type>ComClassTest1.E1EventHandler</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="set_P2" CallingConvention="HasThis">
        <MethodFlags>public newslot strict specialname virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest1._ComClassTest1.set_P2(value As System.Object)</Implements>
        <Parameter Name="value">
            <ParamFlags></ParamFlags>
            <Type>Object</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Property Name="P2">
        <PropertyFlags></PropertyFlags>
        <Set>Sub ComClassTest1.set_P2(value As System.Object)</Set>
    </Property>
    <Event Name="E1">
        <EventFlags></EventFlags>
        <Add>Sub ComClassTest1.add_E1(obj As ComClassTest1.E1EventHandler)</Add>
        <Remove>Sub ComClassTest1.remove_E1(obj As ComClassTest1.E1EventHandler)</Remove>
    </Event>
    <Interface Name="_ComClassTest1">
        <TypeDefFlags>interface nested public abstract auto ansi</TypeDefFlags>
        <Attributes>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>True</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
        </Attributes>
        <Method Name="M1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>1</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Method Name="set_P2" CallingConvention="HasThis">
            <MethodFlags>public newslot strict specialname abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>2</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Parameter Name="value">
                <ParamFlags></ParamFlags>
                <Type>Object</Type>
            </Parameter>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Property Name="P2">
            <PropertyFlags></PropertyFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>2</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Set>Sub ComClassTest1._ComClassTest1.set_P2(value As System.Object)</Set>
        </Property>
    </Interface>
    <Interface Name="__ComClassTest1">
        <TypeDefFlags>interface nested public abstract auto ansi</TypeDefFlags>
        <Attributes>
            <System.Runtime.InteropServices.InterfaceTypeAttribute>
                <ctor>Sub System.Runtime.InteropServices.InterfaceTypeAttribute..ctor(interfaceType As System.Int16)</ctor>
                <a>2</a>
            </System.Runtime.InteropServices.InterfaceTypeAttribute>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>True</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
        </Attributes>
        <Method Name="E1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>1</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
    </Interface>
</Class>


            Dim verifier = CompileAndVerify(compilationDef,
                                            options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal),
                                            symbolValidator:=Sub(m As ModuleSymbol)
                                                                 Dim pe = DirectCast(m, PEModuleSymbol)
                                                                 AssertReflection(expected,
                                                                                  ReflectComClass(pe, "ComClassTest1"))
                                                             End Sub)

            Dim warnings =
<expected>
BC42101: 'Microsoft.VisualBasic.ComClassAttribute' on class 'ComClassTest1' implicitly declares Interface '__ComClassTest1', which conflicts with a member of the same name in Class 'ComClassBase'. Use 'Microsoft.VisualBasic.ComClassAttribute(InterfaceShadows:=True)' if you want to hide the name on the base ComClassBase.
Public Class ComClassTest1
             ~~~~~~~~~~~~~
BC42101: 'Microsoft.VisualBasic.ComClassAttribute' on class 'ComClassTest1' implicitly declares Interface '_ComClassTest1', which conflicts with a member of the same name in Class 'ComClassBase'. Use 'Microsoft.VisualBasic.ComClassAttribute(InterfaceShadows:=True)' if you want to hide the name on the base ComClassBase.
Public Class ComClassTest1
             ~~~~~~~~~~~~~
BC42102: 'Public WriteOnly Property P2 As Object' cannot be exposed to COM as a property 'Let'. You will not be able to assign non-object values (such as numbers or strings) to this property from Visual Basic 6.0 using a 'Let' statement.
    Public WriteOnly Property P2 As Object
                              ~~
</expected>

            AssertTheseDiagnostics(verifier.Compilation, warnings)
        End Sub

        <Fact>
        Public Sub Test_ERR_InvalidAttributeUsage2()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

<Microsoft.VisualBasic.ComClass()>
Public Module ComClassTest1

    Public Sub M1()
    End Sub

End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim expected =
<expected>
BC30662: Attribute 'ComClassAttribute' cannot be applied to 'ComClassTest1' because the attribute is not valid on this declaration type.
Public Module ComClassTest1
              ~~~~~~~~~~~~~
</expected>

            AssertTheseDeclarationDiagnostics(compilation, expected)
            AssertTheseDiagnostics(compilation, expected)
        End Sub

        <Fact()>
        Public Sub ComInvisibleMembers()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices

<Microsoft.VisualBasic.ComClass()>
Public Class ComClassTest

    <ComVisible(False)>
    Public Sub M1(Of T)()
    End Sub

    Public Sub M2()
    End Sub

    <ComVisible(False)>
    Public Property P1 As Integer
        Get
            Return 0
        End Get
        Set(value As Integer)
        End Set
    End Property

    Public Property P2 As Integer
        <ComVisible(False)>
        Get
            Return 0
        End Get
        Set(value As Integer)
        End Set
    End Property

    Public Property P3 As Integer
        Get
            Return 0
        End Get
        <ComVisible(False)>
        Set(value As Integer)
        End Set
    End Property

    Public ReadOnly Property P4 As Integer
        <ComVisible(False)>
        Get
            Return 0
        End Get
    End Property

    Public WriteOnly Property P5 As Integer
        <ComVisible(False)>
        Set(value As Integer)
        End Set
    End Property
End Class
    ]]></file>
</compilation>

            Dim expected =
<Class Name="ComClassTest">
    <TypeDefFlags>public auto ansi</TypeDefFlags>
    <Attributes>
        <System.Runtime.InteropServices.ClassInterfaceAttribute>
            <ctor>Sub System.Runtime.InteropServices.ClassInterfaceAttribute..ctor(classInterfaceType As System.Runtime.InteropServices.ClassInterfaceType)</ctor>
            <a>0</a>
        </System.Runtime.InteropServices.ClassInterfaceAttribute>
        <Microsoft.VisualBasic.ComClassAttribute>
            <ctor>Sub Microsoft.VisualBasic.ComClassAttribute..ctor()</ctor>
        </Microsoft.VisualBasic.ComClassAttribute>
    </Attributes>
    <Implements>ComClassTest._ComClassTest</Implements>
    <Method Name=".ctor" CallingConvention="HasThis">
        <MethodFlags>public specialname rtspecialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="M1" CallingConvention="Generic, HasThis">
        <MethodFlags>public instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>False</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
        </Attributes>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="M2" CallingConvention="HasThis">
        <MethodFlags>public newslot strict virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest._ComClassTest.M2()</Implements>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="get_P1" CallingConvention="HasThis">
        <MethodFlags>public specialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Return>
            <Type>Integer</Type>
        </Return>
    </Method>
    <Method Name="set_P1" CallingConvention="HasThis">
        <MethodFlags>public specialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Parameter Name="value">
            <ParamFlags></ParamFlags>
            <Type>Integer</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="get_P2" CallingConvention="HasThis">
        <MethodFlags>public specialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>False</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
        </Attributes>
        <Return>
            <Type>Integer</Type>
        </Return>
    </Method>
    <Method Name="set_P2" CallingConvention="HasThis">
        <MethodFlags>public newslot strict specialname virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest._ComClassTest.set_P2(value As System.Int32)</Implements>
        <Parameter Name="value">
            <ParamFlags></ParamFlags>
            <Type>Integer</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="get_P3" CallingConvention="HasThis">
        <MethodFlags>public newslot strict specialname virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Function ComClassTest._ComClassTest.get_P3() As System.Int32</Implements>
        <Return>
            <Type>Integer</Type>
        </Return>
    </Method>
    <Method Name="set_P3" CallingConvention="HasThis">
        <MethodFlags>public specialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>False</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
        </Attributes>
        <Parameter Name="value">
            <ParamFlags></ParamFlags>
            <Type>Integer</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="get_P4" CallingConvention="HasThis">
        <MethodFlags>public specialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>False</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
        </Attributes>
        <Return>
            <Type>Integer</Type>
        </Return>
    </Method>
    <Method Name="set_P5" CallingConvention="HasThis">
        <MethodFlags>public specialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>False</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
        </Attributes>
        <Parameter Name="value">
            <ParamFlags></ParamFlags>
            <Type>Integer</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Property Name="P1">
        <PropertyFlags></PropertyFlags>
        <Attributes>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>False</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
        </Attributes>
        <Get>Function ComClassTest.get_P1() As System.Int32</Get>
        <Set>Sub ComClassTest.set_P1(value As System.Int32)</Set>
    </Property>
    <Property Name="P2">
        <PropertyFlags></PropertyFlags>
        <Get>Function ComClassTest.get_P2() As System.Int32</Get>
        <Set>Sub ComClassTest.set_P2(value As System.Int32)</Set>
    </Property>
    <Property Name="P3">
        <PropertyFlags></PropertyFlags>
        <Get>Function ComClassTest.get_P3() As System.Int32</Get>
        <Set>Sub ComClassTest.set_P3(value As System.Int32)</Set>
    </Property>
    <Property Name="P4">
        <PropertyFlags></PropertyFlags>
        <Get>Function ComClassTest.get_P4() As System.Int32</Get>
    </Property>
    <Property Name="P5">
        <PropertyFlags></PropertyFlags>
        <Set>Sub ComClassTest.set_P5(value As System.Int32)</Set>
    </Property>
    <Interface Name="_ComClassTest">
        <TypeDefFlags>interface nested public abstract auto ansi</TypeDefFlags>
        <Attributes>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>True</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
        </Attributes>
        <Method Name="M2" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>1</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Method Name="set_P2" CallingConvention="HasThis">
            <MethodFlags>public newslot strict specialname abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>2</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Parameter Name="value">
                <ParamFlags></ParamFlags>
                <Type>Integer</Type>
            </Parameter>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Method Name="get_P3" CallingConvention="HasThis">
            <MethodFlags>public newslot strict specialname abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>3</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Integer</Type>
            </Return>
        </Method>
        <Property Name="P2">
            <PropertyFlags></PropertyFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>2</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Set>Sub ComClassTest._ComClassTest.set_P2(value As System.Int32)</Set>
        </Property>
        <Property Name="P3">
            <PropertyFlags></PropertyFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>3</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Get>Function ComClassTest._ComClassTest.get_P3() As System.Int32</Get>
        </Property>
    </Interface>
</Class>


            Dim verifier = CompileAndVerify(compilationDef,
                                            options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal),
                                            symbolValidator:=Sub(m As ModuleSymbol)
                                                                 Dim pe = DirectCast(m, PEModuleSymbol)
                                                                 AssertReflection(expected,
                                                                                  ReflectComClass(pe, "ComClassTest"))
                                                             End Sub).VerifyDiagnostics()

        End Sub

        <Fact()>
        Public Sub GuidAttributeTest1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
<Microsoft.VisualBasic.ComClass("", "7666AC25-855F-4534-BC55-27BF09D49D46", "")>
Public Class ComClassTest

    Public Sub M1()
    End Sub

    Public Event E1()

End Class
    ]]></file>
</compilation>

            Dim expected =
<Class Name="ComClassTest">
    <TypeDefFlags>public auto ansi</TypeDefFlags>
    <Attributes>
        <System.Runtime.InteropServices.ClassInterfaceAttribute>
            <ctor>Sub System.Runtime.InteropServices.ClassInterfaceAttribute..ctor(classInterfaceType As System.Runtime.InteropServices.ClassInterfaceType)</ctor>
            <a>0</a>
        </System.Runtime.InteropServices.ClassInterfaceAttribute>
        <System.Runtime.InteropServices.ComSourceInterfacesAttribute>
            <ctor>Sub System.Runtime.InteropServices.ComSourceInterfacesAttribute..ctor(sourceInterfaces As System.String)</ctor>
            <a>ComClassTest+__ComClassTest</a>
        </System.Runtime.InteropServices.ComSourceInterfacesAttribute>
        <Microsoft.VisualBasic.ComClassAttribute>
            <ctor>Sub Microsoft.VisualBasic.ComClassAttribute..ctor(_ClassID As System.String, _InterfaceID As System.String, _EventId As System.String)</ctor>
            <a></a>
            <a>7666AC25-855F-4534-BC55-27BF09D49D46</a>
            <a></a>
        </Microsoft.VisualBasic.ComClassAttribute>
    </Attributes>
    <Implements>ComClassTest._ComClassTest</Implements>
    <Method Name=".ctor" CallingConvention="HasThis">
        <MethodFlags>public specialname rtspecialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="M1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest._ComClassTest.M1()</Implements>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="add_E1" CallingConvention="HasThis">
        <MethodFlags>public specialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.CompilerServices.CompilerGeneratedAttribute>
                <ctor>Sub System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor()</ctor>
            </System.Runtime.CompilerServices.CompilerGeneratedAttribute>
        </Attributes>
        <Parameter Name="obj">
            <ParamFlags></ParamFlags>
            <Type>ComClassTest.E1EventHandler</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="remove_E1" CallingConvention="HasThis">
        <MethodFlags>public specialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.CompilerServices.CompilerGeneratedAttribute>
                <ctor>Sub System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor()</ctor>
            </System.Runtime.CompilerServices.CompilerGeneratedAttribute>
        </Attributes>
        <Parameter Name="obj">
            <ParamFlags></ParamFlags>
            <Type>ComClassTest.E1EventHandler</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Event Name="E1">
        <EventFlags></EventFlags>
        <Add>Sub ComClassTest.add_E1(obj As ComClassTest.E1EventHandler)</Add>
        <Remove>Sub ComClassTest.remove_E1(obj As ComClassTest.E1EventHandler)</Remove>
    </Event>
    <Interface Name="_ComClassTest">
        <TypeDefFlags>interface nested public abstract auto ansi</TypeDefFlags>
        <Attributes>
            <System.Runtime.InteropServices.GuidAttribute>
                <ctor>Sub System.Runtime.InteropServices.GuidAttribute..ctor(guid As System.String)</ctor>
                <a>7666AC25-855F-4534-BC55-27BF09D49D46</a>
            </System.Runtime.InteropServices.GuidAttribute>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>True</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
        </Attributes>
        <Method Name="M1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>1</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
    </Interface>
    <Interface Name="__ComClassTest">
        <TypeDefFlags>interface nested public abstract auto ansi</TypeDefFlags>
        <Attributes>
            <System.Runtime.InteropServices.InterfaceTypeAttribute>
                <ctor>Sub System.Runtime.InteropServices.InterfaceTypeAttribute..ctor(interfaceType As System.Int16)</ctor>
                <a>2</a>
            </System.Runtime.InteropServices.InterfaceTypeAttribute>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>True</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
        </Attributes>
        <Method Name="E1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>1</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
    </Interface>
</Class>


            Dim verifier = CompileAndVerify(compilationDef,
                                            options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal),
                                            symbolValidator:=Sub(m As ModuleSymbol)
                                                                 Dim pe = DirectCast(m, PEModuleSymbol)
                                                                 AssertReflection(expected,
                                                                                  ReflectComClass(pe, "ComClassTest"))
                                                             End Sub).VerifyDiagnostics()

        End Sub

        <Fact()>
        Public Sub GuidAttributeTest2()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
<Microsoft.VisualBasic.ComClass("", "", "7666AC25-855F-4534-BC55-27BF09D49D46")>
Public Class ComClassTest

    Public Sub M1()
    End Sub

    Public Event E1()

End Class
    ]]></file>
</compilation>

            Dim expected =
<Class Name="ComClassTest">
    <TypeDefFlags>public auto ansi</TypeDefFlags>
    <Attributes>
        <System.Runtime.InteropServices.ClassInterfaceAttribute>
            <ctor>Sub System.Runtime.InteropServices.ClassInterfaceAttribute..ctor(classInterfaceType As System.Runtime.InteropServices.ClassInterfaceType)</ctor>
            <a>0</a>
        </System.Runtime.InteropServices.ClassInterfaceAttribute>
        <System.Runtime.InteropServices.ComSourceInterfacesAttribute>
            <ctor>Sub System.Runtime.InteropServices.ComSourceInterfacesAttribute..ctor(sourceInterfaces As System.String)</ctor>
            <a>ComClassTest+__ComClassTest</a>
        </System.Runtime.InteropServices.ComSourceInterfacesAttribute>
        <Microsoft.VisualBasic.ComClassAttribute>
            <ctor>Sub Microsoft.VisualBasic.ComClassAttribute..ctor(_ClassID As System.String, _InterfaceID As System.String, _EventId As System.String)</ctor>
            <a></a>
            <a></a>
            <a>7666AC25-855F-4534-BC55-27BF09D49D46</a>
        </Microsoft.VisualBasic.ComClassAttribute>
    </Attributes>
    <Implements>ComClassTest._ComClassTest</Implements>
    <Method Name=".ctor" CallingConvention="HasThis">
        <MethodFlags>public specialname rtspecialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="M1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest._ComClassTest.M1()</Implements>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="add_E1" CallingConvention="HasThis">
        <MethodFlags>public specialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.CompilerServices.CompilerGeneratedAttribute>
                <ctor>Sub System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor()</ctor>
            </System.Runtime.CompilerServices.CompilerGeneratedAttribute>
        </Attributes>
        <Parameter Name="obj">
            <ParamFlags></ParamFlags>
            <Type>ComClassTest.E1EventHandler</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="remove_E1" CallingConvention="HasThis">
        <MethodFlags>public specialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.CompilerServices.CompilerGeneratedAttribute>
                <ctor>Sub System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor()</ctor>
            </System.Runtime.CompilerServices.CompilerGeneratedAttribute>
        </Attributes>
        <Parameter Name="obj">
            <ParamFlags></ParamFlags>
            <Type>ComClassTest.E1EventHandler</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Event Name="E1">
        <EventFlags></EventFlags>
        <Add>Sub ComClassTest.add_E1(obj As ComClassTest.E1EventHandler)</Add>
        <Remove>Sub ComClassTest.remove_E1(obj As ComClassTest.E1EventHandler)</Remove>
    </Event>
    <Interface Name="_ComClassTest">
        <TypeDefFlags>interface nested public abstract auto ansi</TypeDefFlags>
        <Attributes>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>True</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
        </Attributes>
        <Method Name="M1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>1</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
    </Interface>
    <Interface Name="__ComClassTest">
        <TypeDefFlags>interface nested public abstract auto ansi</TypeDefFlags>
        <Attributes>
            <System.Runtime.InteropServices.GuidAttribute>
                <ctor>Sub System.Runtime.InteropServices.GuidAttribute..ctor(guid As System.String)</ctor>
                <a>7666AC25-855F-4534-BC55-27BF09D49D46</a>
            </System.Runtime.InteropServices.GuidAttribute>
            <System.Runtime.InteropServices.InterfaceTypeAttribute>
                <ctor>Sub System.Runtime.InteropServices.InterfaceTypeAttribute..ctor(interfaceType As System.Int16)</ctor>
                <a>2</a>
            </System.Runtime.InteropServices.InterfaceTypeAttribute>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>True</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
        </Attributes>
        <Method Name="E1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>1</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
    </Interface>
</Class>


            Dim verifier = CompileAndVerify(compilationDef,
                                            options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal),
                                            symbolValidator:=Sub(m As ModuleSymbol)
                                                                 Dim pe = DirectCast(m, PEModuleSymbol)
                                                                 AssertReflection(expected,
                                                                                  ReflectComClass(pe, "ComClassTest"))
                                                             End Sub).VerifyDiagnostics()

        End Sub

        <Fact()>
        Public Sub GuidAttributeTest3()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

<Microsoft.VisualBasic.ComClass("{7666AC25-855F-4534-BC55-27BF09D49D44}", "(7666AC25-855F-4534-BC55-27BF09D49D45)", "7666AC25855F4534BC5527BF09D49D46")>
Public Class ComClassTest

    Public Sub M1()
    End Sub

    Public Event E1()

End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim expected =
<expected>
BC32500: 'ComClassAttribute' cannot be applied because the format of the GUID '(7666AC25-855F-4534-BC55-27BF09D49D45)' is not correct.
Public Class ComClassTest
             ~~~~~~~~~~~~
BC32500: 'ComClassAttribute' cannot be applied because the format of the GUID '{7666AC25-855F-4534-BC55-27BF09D49D44}' is not correct.
Public Class ComClassTest
             ~~~~~~~~~~~~
BC32500: 'ComClassAttribute' cannot be applied because the format of the GUID '7666AC25855F4534BC5527BF09D49D46' is not correct.
Public Class ComClassTest
             ~~~~~~~~~~~~
</expected>

            AssertTheseDeclarationDiagnostics(compilation, expected)
            AssertTheseDiagnostics(compilation, expected)
        End Sub

        <Fact()>
        Public Sub ComSourceInterfacesAttribute1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

Namespace nS
    Class Test2
    End Class
End Namespace

Namespace NS
    Public Class ComClassTest1
        Class ComClassTest2
            <Microsoft.VisualBasic.ComClass()>
            Public Class ComClassTest3
                Public Event E1()
            End Class
        End Class
    End Class
End Namespace

Namespace ns
    Class Test1
    End Class
End Namespace
    ]]></file>
</compilation>

            Dim expected =
<Class Name="ComClassTest3">
    <TypeDefFlags>nested public auto ansi</TypeDefFlags>
    <Attributes>
        <System.Runtime.InteropServices.ClassInterfaceAttribute>
            <ctor>Sub System.Runtime.InteropServices.ClassInterfaceAttribute..ctor(classInterfaceType As System.Runtime.InteropServices.ClassInterfaceType)</ctor>
            <a>0</a>
        </System.Runtime.InteropServices.ClassInterfaceAttribute>
        <System.Runtime.InteropServices.ComSourceInterfacesAttribute>
            <ctor>Sub System.Runtime.InteropServices.ComSourceInterfacesAttribute..ctor(sourceInterfaces As System.String)</ctor>
            <a>NS.ComClassTest1+ComClassTest2+ComClassTest3+__ComClassTest3</a>
        </System.Runtime.InteropServices.ComSourceInterfacesAttribute>
        <Microsoft.VisualBasic.ComClassAttribute>
            <ctor>Sub Microsoft.VisualBasic.ComClassAttribute..ctor()</ctor>
        </Microsoft.VisualBasic.ComClassAttribute>
    </Attributes>
    <Implements>NS.ComClassTest1.ComClassTest2.ComClassTest3._ComClassTest3</Implements>
    <Method Name=".ctor" CallingConvention="HasThis">
        <MethodFlags>public specialname rtspecialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="add_E1" CallingConvention="HasThis">
        <MethodFlags>public specialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.CompilerServices.CompilerGeneratedAttribute>
                <ctor>Sub System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor()</ctor>
            </System.Runtime.CompilerServices.CompilerGeneratedAttribute>
        </Attributes>
        <Parameter Name="obj">
            <ParamFlags></ParamFlags>
            <Type>NS.ComClassTest1.ComClassTest2.ComClassTest3.E1EventHandler</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="remove_E1" CallingConvention="HasThis">
        <MethodFlags>public specialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.CompilerServices.CompilerGeneratedAttribute>
                <ctor>Sub System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor()</ctor>
            </System.Runtime.CompilerServices.CompilerGeneratedAttribute>
        </Attributes>
        <Parameter Name="obj">
            <ParamFlags></ParamFlags>
            <Type>NS.ComClassTest1.ComClassTest2.ComClassTest3.E1EventHandler</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Event Name="E1">
        <EventFlags></EventFlags>
        <Add>Sub NS.ComClassTest1.ComClassTest2.ComClassTest3.add_E1(obj As NS.ComClassTest1.ComClassTest2.ComClassTest3.E1EventHandler)</Add>
        <Remove>Sub NS.ComClassTest1.ComClassTest2.ComClassTest3.remove_E1(obj As NS.ComClassTest1.ComClassTest2.ComClassTest3.E1EventHandler)</Remove>
    </Event>
    <Interface Name="_ComClassTest3">
        <TypeDefFlags>interface nested public abstract auto ansi</TypeDefFlags>
        <Attributes>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>True</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
        </Attributes>
    </Interface>
    <Interface Name="__ComClassTest3">
        <TypeDefFlags>interface nested public abstract auto ansi</TypeDefFlags>
        <Attributes>
            <System.Runtime.InteropServices.InterfaceTypeAttribute>
                <ctor>Sub System.Runtime.InteropServices.InterfaceTypeAttribute..ctor(interfaceType As System.Int16)</ctor>
                <a>2</a>
            </System.Runtime.InteropServices.InterfaceTypeAttribute>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>True</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
        </Attributes>
        <Method Name="E1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>1</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
    </Interface>
</Class>


            Dim verifier = CompileAndVerify(compilationDef,
                                            options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal),
                                            symbolValidator:=Sub(m As ModuleSymbol)
                                                                 Dim pe = DirectCast(m, PEModuleSymbol)
                                                                 AssertReflection(expected,
                                                                                  ReflectComClass(pe, "NS.ComClassTest1+ComClassTest2+ComClassTest3"))
                                                             End Sub)

        End Sub

        <Fact()>
        Public Sub OrderOfAccessors()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

<Microsoft.VisualBasic.ComClass("", "", "")>
Public Class ComClassTest
    Property P1 As Integer
        Set(value As Integer)

        End Set
        Get
            Return Nothing
        End Get
    End Property
End Class
    ]]></file>
</compilation>

            Dim expected =
<Class Name="ComClassTest">
    <TypeDefFlags>public auto ansi</TypeDefFlags>
    <Attributes>
        <System.Runtime.InteropServices.ClassInterfaceAttribute>
            <ctor>Sub System.Runtime.InteropServices.ClassInterfaceAttribute..ctor(classInterfaceType As System.Runtime.InteropServices.ClassInterfaceType)</ctor>
            <a>0</a>
        </System.Runtime.InteropServices.ClassInterfaceAttribute>
        <Microsoft.VisualBasic.ComClassAttribute>
            <ctor>Sub Microsoft.VisualBasic.ComClassAttribute..ctor(_ClassID As System.String, _InterfaceID As System.String, _EventId As System.String)</ctor>
            <a></a>
            <a></a>
            <a></a>
        </Microsoft.VisualBasic.ComClassAttribute>
    </Attributes>
    <Implements>ComClassTest._ComClassTest</Implements>
    <Method Name=".ctor" CallingConvention="HasThis">
        <MethodFlags>public specialname rtspecialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="set_P1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict specialname virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest._ComClassTest.set_P1(value As System.Int32)</Implements>
        <Parameter Name="value">
            <ParamFlags></ParamFlags>
            <Type>Integer</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="get_P1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict specialname virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Function ComClassTest._ComClassTest.get_P1() As System.Int32</Implements>
        <Return>
            <Type>Integer</Type>
        </Return>
    </Method>
    <Property Name="P1">
        <PropertyFlags></PropertyFlags>
        <Get>Function ComClassTest.get_P1() As System.Int32</Get>
        <Set>Sub ComClassTest.set_P1(value As System.Int32)</Set>
    </Property>
    <Interface Name="_ComClassTest">
        <TypeDefFlags>interface nested public abstract auto ansi</TypeDefFlags>
        <Attributes>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>True</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
        </Attributes>
        <Method Name="set_P1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict specialname abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>1</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Parameter Name="value">
                <ParamFlags></ParamFlags>
                <Type>Integer</Type>
            </Parameter>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Method Name="get_P1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict specialname abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>1</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Integer</Type>
            </Return>
        </Method>
        <Property Name="P1">
            <PropertyFlags></PropertyFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>1</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Get>Function ComClassTest._ComClassTest.get_P1() As System.Int32</Get>
            <Set>Sub ComClassTest._ComClassTest.set_P1(value As System.Int32)</Set>
        </Property>
    </Interface>
</Class>

            Dim verifier = CompileAndVerify(compilationDef,
                                            options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal),
                                            symbolValidator:=Sub(m As ModuleSymbol)
                                                                 Dim pe = DirectCast(m, PEModuleSymbol)
                                                                 AssertReflection(expected,
                                                                                  ReflectComClass(pe, "ComClassTest"))
                                                             End Sub)

        End Sub

        <Fact()>
        Public Sub DispId1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

<Microsoft.VisualBasic.ComClass()>
Public Class ComClassTest

    Sub M1()
    End Sub

    <DispId(15)>
    Sub M2()
    End Sub

    Sub M3()
    End Sub

    Event E1 As Action

    <DispId(16)>
    Event E2 As Action

    Event E3 As Action
End Class
    ]]></file>
</compilation>

            Dim expected =
<Class Name="ComClassTest">
    <TypeDefFlags>public auto ansi</TypeDefFlags>
    <Attributes>
        <System.Runtime.InteropServices.ClassInterfaceAttribute>
            <ctor>Sub System.Runtime.InteropServices.ClassInterfaceAttribute..ctor(classInterfaceType As System.Runtime.InteropServices.ClassInterfaceType)</ctor>
            <a>0</a>
        </System.Runtime.InteropServices.ClassInterfaceAttribute>
        <System.Runtime.InteropServices.ComSourceInterfacesAttribute>
            <ctor>Sub System.Runtime.InteropServices.ComSourceInterfacesAttribute..ctor(sourceInterfaces As System.String)</ctor>
            <a>ComClassTest+__ComClassTest</a>
        </System.Runtime.InteropServices.ComSourceInterfacesAttribute>
        <Microsoft.VisualBasic.ComClassAttribute>
            <ctor>Sub Microsoft.VisualBasic.ComClassAttribute..ctor()</ctor>
        </Microsoft.VisualBasic.ComClassAttribute>
    </Attributes>
    <Implements>ComClassTest._ComClassTest</Implements>
    <Method Name=".ctor" CallingConvention="HasThis">
        <MethodFlags>public specialname rtspecialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="M1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest._ComClassTest.M1()</Implements>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="M2" CallingConvention="HasThis">
        <MethodFlags>public newslot strict virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.InteropServices.DispIdAttribute>
                <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                <a>15</a>
            </System.Runtime.InteropServices.DispIdAttribute>
        </Attributes>
        <Implements>Sub ComClassTest._ComClassTest.M2()</Implements>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="M3" CallingConvention="HasThis">
        <MethodFlags>public newslot strict virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest._ComClassTest.M3()</Implements>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="add_E1" CallingConvention="HasThis">
        <MethodFlags>public specialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.CompilerServices.CompilerGeneratedAttribute>
                <ctor>Sub System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor()</ctor>
            </System.Runtime.CompilerServices.CompilerGeneratedAttribute>
        </Attributes>
        <Parameter Name="obj">
            <ParamFlags></ParamFlags>
            <Type>System.Action</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="remove_E1" CallingConvention="HasThis">
        <MethodFlags>public specialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.CompilerServices.CompilerGeneratedAttribute>
                <ctor>Sub System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor()</ctor>
            </System.Runtime.CompilerServices.CompilerGeneratedAttribute>
        </Attributes>
        <Parameter Name="obj">
            <ParamFlags></ParamFlags>
            <Type>System.Action</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="add_E2" CallingConvention="HasThis">
        <MethodFlags>public specialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.CompilerServices.CompilerGeneratedAttribute>
                <ctor>Sub System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor()</ctor>
            </System.Runtime.CompilerServices.CompilerGeneratedAttribute>
        </Attributes>
        <Parameter Name="obj">
            <ParamFlags></ParamFlags>
            <Type>System.Action</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="remove_E2" CallingConvention="HasThis">
        <MethodFlags>public specialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.CompilerServices.CompilerGeneratedAttribute>
                <ctor>Sub System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor()</ctor>
            </System.Runtime.CompilerServices.CompilerGeneratedAttribute>
        </Attributes>
        <Parameter Name="obj">
            <ParamFlags></ParamFlags>
            <Type>System.Action</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="add_E3" CallingConvention="HasThis">
        <MethodFlags>public specialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.CompilerServices.CompilerGeneratedAttribute>
                <ctor>Sub System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor()</ctor>
            </System.Runtime.CompilerServices.CompilerGeneratedAttribute>
        </Attributes>
        <Parameter Name="obj">
            <ParamFlags></ParamFlags>
            <Type>System.Action</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="remove_E3" CallingConvention="HasThis">
        <MethodFlags>public specialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.CompilerServices.CompilerGeneratedAttribute>
                <ctor>Sub System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor()</ctor>
            </System.Runtime.CompilerServices.CompilerGeneratedAttribute>
        </Attributes>
        <Parameter Name="obj">
            <ParamFlags></ParamFlags>
            <Type>System.Action</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Event Name="E1">
        <EventFlags></EventFlags>
        <Add>Sub ComClassTest.add_E1(obj As System.Action)</Add>
        <Remove>Sub ComClassTest.remove_E1(obj As System.Action)</Remove>
    </Event>
    <Event Name="E2">
        <EventFlags></EventFlags>
        <Attributes>
            <System.Runtime.InteropServices.DispIdAttribute>
                <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                <a>16</a>
            </System.Runtime.InteropServices.DispIdAttribute>
        </Attributes>
        <Add>Sub ComClassTest.add_E2(obj As System.Action)</Add>
        <Remove>Sub ComClassTest.remove_E2(obj As System.Action)</Remove>
    </Event>
    <Event Name="E3">
        <EventFlags></EventFlags>
        <Add>Sub ComClassTest.add_E3(obj As System.Action)</Add>
        <Remove>Sub ComClassTest.remove_E3(obj As System.Action)</Remove>
    </Event>
    <Interface Name="_ComClassTest">
        <TypeDefFlags>interface nested public abstract auto ansi</TypeDefFlags>
        <Attributes>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>True</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
        </Attributes>
        <Method Name="M1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>1</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Method Name="M2" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>15</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Method Name="M3" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>2</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
    </Interface>
    <Interface Name="__ComClassTest">
        <TypeDefFlags>interface nested public abstract auto ansi</TypeDefFlags>
        <Attributes>
            <System.Runtime.InteropServices.InterfaceTypeAttribute>
                <ctor>Sub System.Runtime.InteropServices.InterfaceTypeAttribute..ctor(interfaceType As System.Int16)</ctor>
                <a>2</a>
            </System.Runtime.InteropServices.InterfaceTypeAttribute>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>True</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
        </Attributes>
        <Method Name="E1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>1</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Method Name="E2" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>16</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Method Name="E3" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>2</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
    </Interface>
</Class>

            Dim verifier = CompileAndVerify(compilationDef,
                                            options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal),
                                            symbolValidator:=Sub(m As ModuleSymbol)
                                                                 Dim pe = DirectCast(m, PEModuleSymbol)
                                                                 AssertReflection(expected,
                                                                                  ReflectComClass(pe, "ComClassTest"))
                                                             End Sub)

        End Sub

        <Fact()>
        Public Sub DispId2()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

<Microsoft.VisualBasic.ComClass()>
Public Class ComClassTest

    Sub M1()
    End Sub

    <DispId(1)>
    Sub M2()
    End Sub

    <DispId(3)>
    Friend Sub M3()
    End Sub

    Sub M4()
    End Sub

    Event E1 As Action

    <DispId(2)>
    Event E2 As Action

    <DispId(3)>
    Friend Event E3 As Action

    Event E4 As Action
End Class
    ]]></file>
</compilation>

            Dim expected =
<Class Name="ComClassTest">
    <TypeDefFlags>public auto ansi</TypeDefFlags>
    <Attributes>
        <System.Runtime.InteropServices.ClassInterfaceAttribute>
            <ctor>Sub System.Runtime.InteropServices.ClassInterfaceAttribute..ctor(classInterfaceType As System.Runtime.InteropServices.ClassInterfaceType)</ctor>
            <a>0</a>
        </System.Runtime.InteropServices.ClassInterfaceAttribute>
        <System.Runtime.InteropServices.ComSourceInterfacesAttribute>
            <ctor>Sub System.Runtime.InteropServices.ComSourceInterfacesAttribute..ctor(sourceInterfaces As System.String)</ctor>
            <a>ComClassTest+__ComClassTest</a>
        </System.Runtime.InteropServices.ComSourceInterfacesAttribute>
        <Microsoft.VisualBasic.ComClassAttribute>
            <ctor>Sub Microsoft.VisualBasic.ComClassAttribute..ctor()</ctor>
        </Microsoft.VisualBasic.ComClassAttribute>
    </Attributes>
    <Implements>ComClassTest._ComClassTest</Implements>
    <Method Name=".ctor" CallingConvention="HasThis">
        <MethodFlags>public specialname rtspecialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="M1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest._ComClassTest.M1()</Implements>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="M2" CallingConvention="HasThis">
        <MethodFlags>public newslot strict virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.InteropServices.DispIdAttribute>
                <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                <a>1</a>
            </System.Runtime.InteropServices.DispIdAttribute>
        </Attributes>
        <Implements>Sub ComClassTest._ComClassTest.M2()</Implements>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="M3" CallingConvention="HasThis">
        <MethodFlags>assembly instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.InteropServices.DispIdAttribute>
                <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                <a>3</a>
            </System.Runtime.InteropServices.DispIdAttribute>
        </Attributes>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="M4" CallingConvention="HasThis">
        <MethodFlags>public newslot strict virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest._ComClassTest.M4()</Implements>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="add_E1" CallingConvention="HasThis">
        <MethodFlags>public specialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.CompilerServices.CompilerGeneratedAttribute>
                <ctor>Sub System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor()</ctor>
            </System.Runtime.CompilerServices.CompilerGeneratedAttribute>
        </Attributes>
        <Parameter Name="obj">
            <ParamFlags></ParamFlags>
            <Type>System.Action</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="remove_E1" CallingConvention="HasThis">
        <MethodFlags>public specialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.CompilerServices.CompilerGeneratedAttribute>
                <ctor>Sub System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor()</ctor>
            </System.Runtime.CompilerServices.CompilerGeneratedAttribute>
        </Attributes>
        <Parameter Name="obj">
            <ParamFlags></ParamFlags>
            <Type>System.Action</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="add_E2" CallingConvention="HasThis">
        <MethodFlags>public specialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.CompilerServices.CompilerGeneratedAttribute>
                <ctor>Sub System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor()</ctor>
            </System.Runtime.CompilerServices.CompilerGeneratedAttribute>
        </Attributes>
        <Parameter Name="obj">
            <ParamFlags></ParamFlags>
            <Type>System.Action</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="remove_E2" CallingConvention="HasThis">
        <MethodFlags>public specialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.CompilerServices.CompilerGeneratedAttribute>
                <ctor>Sub System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor()</ctor>
            </System.Runtime.CompilerServices.CompilerGeneratedAttribute>
        </Attributes>
        <Parameter Name="obj">
            <ParamFlags></ParamFlags>
            <Type>System.Action</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="add_E3" CallingConvention="HasThis">
        <MethodFlags>assembly specialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.CompilerServices.CompilerGeneratedAttribute>
                <ctor>Sub System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor()</ctor>
            </System.Runtime.CompilerServices.CompilerGeneratedAttribute>
        </Attributes>
        <Parameter Name="obj">
            <ParamFlags></ParamFlags>
            <Type>System.Action</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="remove_E3" CallingConvention="HasThis">
        <MethodFlags>assembly specialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.CompilerServices.CompilerGeneratedAttribute>
                <ctor>Sub System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor()</ctor>
            </System.Runtime.CompilerServices.CompilerGeneratedAttribute>
        </Attributes>
        <Parameter Name="obj">
            <ParamFlags></ParamFlags>
            <Type>System.Action</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="add_E4" CallingConvention="HasThis">
        <MethodFlags>public specialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.CompilerServices.CompilerGeneratedAttribute>
                <ctor>Sub System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor()</ctor>
            </System.Runtime.CompilerServices.CompilerGeneratedAttribute>
        </Attributes>
        <Parameter Name="obj">
            <ParamFlags></ParamFlags>
            <Type>System.Action</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="remove_E4" CallingConvention="HasThis">
        <MethodFlags>public specialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.CompilerServices.CompilerGeneratedAttribute>
                <ctor>Sub System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor()</ctor>
            </System.Runtime.CompilerServices.CompilerGeneratedAttribute>
        </Attributes>
        <Parameter Name="obj">
            <ParamFlags></ParamFlags>
            <Type>System.Action</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Event Name="E1">
        <EventFlags></EventFlags>
        <Add>Sub ComClassTest.add_E1(obj As System.Action)</Add>
        <Remove>Sub ComClassTest.remove_E1(obj As System.Action)</Remove>
    </Event>
    <Event Name="E2">
        <EventFlags></EventFlags>
        <Attributes>
            <System.Runtime.InteropServices.DispIdAttribute>
                <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                <a>2</a>
            </System.Runtime.InteropServices.DispIdAttribute>
        </Attributes>
        <Add>Sub ComClassTest.add_E2(obj As System.Action)</Add>
        <Remove>Sub ComClassTest.remove_E2(obj As System.Action)</Remove>
    </Event>
    <Event Name="E3">
        <EventFlags></EventFlags>
        <Attributes>
            <System.Runtime.InteropServices.DispIdAttribute>
                <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                <a>3</a>
            </System.Runtime.InteropServices.DispIdAttribute>
        </Attributes>
        <Add>Sub ComClassTest.add_E3(obj As System.Action)</Add>
        <Remove>Sub ComClassTest.remove_E3(obj As System.Action)</Remove>
    </Event>
    <Event Name="E4">
        <EventFlags></EventFlags>
        <Add>Sub ComClassTest.add_E4(obj As System.Action)</Add>
        <Remove>Sub ComClassTest.remove_E4(obj As System.Action)</Remove>
    </Event>
    <Interface Name="_ComClassTest">
        <TypeDefFlags>interface nested public abstract auto ansi</TypeDefFlags>
        <Attributes>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>True</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
        </Attributes>
        <Method Name="M1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>2</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Method Name="M2" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>1</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Method Name="M4" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>3</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
    </Interface>
    <Interface Name="__ComClassTest">
        <TypeDefFlags>interface nested public abstract auto ansi</TypeDefFlags>
        <Attributes>
            <System.Runtime.InteropServices.InterfaceTypeAttribute>
                <ctor>Sub System.Runtime.InteropServices.InterfaceTypeAttribute..ctor(interfaceType As System.Int16)</ctor>
                <a>2</a>
            </System.Runtime.InteropServices.InterfaceTypeAttribute>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>True</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
        </Attributes>
        <Method Name="E1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>1</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Method Name="E2" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>2</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Method Name="E4" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>3</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
    </Interface>
</Class>

            Dim verifier = CompileAndVerify(compilationDef,
                                            options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal),
                                            symbolValidator:=Sub(m As ModuleSymbol)
                                                                 Dim pe = DirectCast(m, PEModuleSymbol)
                                                                 AssertReflection(expected,
                                                                                  ReflectComClass(pe, "ComClassTest"))
                                                             End Sub)

        End Sub

        <Fact()>
        Public Sub DispId3()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

<Microsoft.VisualBasic.ComClass()>
Public Class ComClassTest

    Sub M1()
    End Sub

    <DispId(15)>
    Property P1 As Integer
        <DispId(16)>
        Get
            Return 0
        End Get
        Set(value As Integer)
        End Set
    End Property

    Sub M3()
    End Sub

End Class
    ]]></file>
</compilation>

            Dim expected =
<Class Name="ComClassTest">
    <TypeDefFlags>public auto ansi</TypeDefFlags>
    <Attributes>
        <System.Runtime.InteropServices.ClassInterfaceAttribute>
            <ctor>Sub System.Runtime.InteropServices.ClassInterfaceAttribute..ctor(classInterfaceType As System.Runtime.InteropServices.ClassInterfaceType)</ctor>
            <a>0</a>
        </System.Runtime.InteropServices.ClassInterfaceAttribute>
        <Microsoft.VisualBasic.ComClassAttribute>
            <ctor>Sub Microsoft.VisualBasic.ComClassAttribute..ctor()</ctor>
        </Microsoft.VisualBasic.ComClassAttribute>
    </Attributes>
    <Implements>ComClassTest._ComClassTest</Implements>
    <Method Name=".ctor" CallingConvention="HasThis">
        <MethodFlags>public specialname rtspecialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="M1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest._ComClassTest.M1()</Implements>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="get_P1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict specialname virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.InteropServices.DispIdAttribute>
                <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                <a>16</a>
            </System.Runtime.InteropServices.DispIdAttribute>
        </Attributes>
        <Implements>Function ComClassTest._ComClassTest.get_P1() As System.Int32</Implements>
        <Return>
            <Type>Integer</Type>
        </Return>
    </Method>
    <Method Name="set_P1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict specialname virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest._ComClassTest.set_P1(value As System.Int32)</Implements>
        <Parameter Name="value">
            <ParamFlags></ParamFlags>
            <Type>Integer</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="M3" CallingConvention="HasThis">
        <MethodFlags>public newslot strict virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest._ComClassTest.M3()</Implements>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Property Name="P1">
        <PropertyFlags></PropertyFlags>
        <Attributes>
            <System.Runtime.InteropServices.DispIdAttribute>
                <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                <a>15</a>
            </System.Runtime.InteropServices.DispIdAttribute>
        </Attributes>
        <Get>Function ComClassTest.get_P1() As System.Int32</Get>
        <Set>Sub ComClassTest.set_P1(value As System.Int32)</Set>
    </Property>
    <Interface Name="_ComClassTest">
        <TypeDefFlags>interface nested public abstract auto ansi</TypeDefFlags>
        <Attributes>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>True</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
        </Attributes>
        <Method Name="M1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>1</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Method Name="get_P1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict specialname abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>16</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Integer</Type>
            </Return>
        </Method>
        <Method Name="set_P1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict specialname abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>15</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Parameter Name="value">
                <ParamFlags></ParamFlags>
                <Type>Integer</Type>
            </Parameter>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Method Name="M3" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>3</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Property Name="P1">
            <PropertyFlags></PropertyFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>15</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Get>Function ComClassTest._ComClassTest.get_P1() As System.Int32</Get>
            <Set>Sub ComClassTest._ComClassTest.set_P1(value As System.Int32)</Set>
        </Property>
    </Interface>
</Class>

            Dim verifier = CompileAndVerify(compilationDef,
                                            options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal),
                                            symbolValidator:=Sub(m As ModuleSymbol)
                                                                 Dim pe = DirectCast(m, PEModuleSymbol)
                                                                 AssertReflection(expected,
                                                                                  ReflectComClass(pe, "ComClassTest"))
                                                             End Sub)

        End Sub

        <Fact()>
        Public Sub DispId4()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

<Microsoft.VisualBasic.ComClass()>
Public Class ComClassTest

    Sub M1()
    End Sub

    <DispId(15)>
    Property P1 As Integer
        Get
            Return 0
        End Get
        <DispId(16)>
        Set(value As Integer)
        End Set
    End Property

    Sub M3()
    End Sub

End Class
    ]]></file>
</compilation>

            Dim expected =
<Class Name="ComClassTest">
    <TypeDefFlags>public auto ansi</TypeDefFlags>
    <Attributes>
        <System.Runtime.InteropServices.ClassInterfaceAttribute>
            <ctor>Sub System.Runtime.InteropServices.ClassInterfaceAttribute..ctor(classInterfaceType As System.Runtime.InteropServices.ClassInterfaceType)</ctor>
            <a>0</a>
        </System.Runtime.InteropServices.ClassInterfaceAttribute>
        <Microsoft.VisualBasic.ComClassAttribute>
            <ctor>Sub Microsoft.VisualBasic.ComClassAttribute..ctor()</ctor>
        </Microsoft.VisualBasic.ComClassAttribute>
    </Attributes>
    <Implements>ComClassTest._ComClassTest</Implements>
    <Method Name=".ctor" CallingConvention="HasThis">
        <MethodFlags>public specialname rtspecialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="M1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest._ComClassTest.M1()</Implements>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="get_P1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict specialname virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Function ComClassTest._ComClassTest.get_P1() As System.Int32</Implements>
        <Return>
            <Type>Integer</Type>
        </Return>
    </Method>
    <Method Name="set_P1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict specialname virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.InteropServices.DispIdAttribute>
                <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                <a>16</a>
            </System.Runtime.InteropServices.DispIdAttribute>
        </Attributes>
        <Implements>Sub ComClassTest._ComClassTest.set_P1(value As System.Int32)</Implements>
        <Parameter Name="value">
            <ParamFlags></ParamFlags>
            <Type>Integer</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="M3" CallingConvention="HasThis">
        <MethodFlags>public newslot strict virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest._ComClassTest.M3()</Implements>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Property Name="P1">
        <PropertyFlags></PropertyFlags>
        <Attributes>
            <System.Runtime.InteropServices.DispIdAttribute>
                <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                <a>15</a>
            </System.Runtime.InteropServices.DispIdAttribute>
        </Attributes>
        <Get>Function ComClassTest.get_P1() As System.Int32</Get>
        <Set>Sub ComClassTest.set_P1(value As System.Int32)</Set>
    </Property>
    <Interface Name="_ComClassTest">
        <TypeDefFlags>interface nested public abstract auto ansi</TypeDefFlags>
        <Attributes>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>True</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
        </Attributes>
        <Method Name="M1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>1</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Method Name="get_P1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict specialname abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>15</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Integer</Type>
            </Return>
        </Method>
        <Method Name="set_P1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict specialname abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>16</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Parameter Name="value">
                <ParamFlags></ParamFlags>
                <Type>Integer</Type>
            </Parameter>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Method Name="M3" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>3</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Property Name="P1">
            <PropertyFlags></PropertyFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>15</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Get>Function ComClassTest._ComClassTest.get_P1() As System.Int32</Get>
            <Set>Sub ComClassTest._ComClassTest.set_P1(value As System.Int32)</Set>
        </Property>
    </Interface>
</Class>

            Dim verifier = CompileAndVerify(compilationDef,
                                            options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal),
                                            symbolValidator:=Sub(m As ModuleSymbol)
                                                                 Dim pe = DirectCast(m, PEModuleSymbol)
                                                                 AssertReflection(expected,
                                                                                  ReflectComClass(pe, "ComClassTest"))
                                                             End Sub)

        End Sub

        <Fact()>
        Public Sub DispId5()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

<Microsoft.VisualBasic.ComClass()>
Public Class ComClassTest

    Sub M1()
    End Sub

    Property P1 As Integer
        <DispId(15)>
        Get
            Return 0
        End Get
        <DispId(16)>
        Set(value As Integer)
        End Set
    End Property

    Sub M3()
    End Sub

End Class
    ]]></file>
</compilation>

            Dim expected =
<Class Name="ComClassTest">
    <TypeDefFlags>public auto ansi</TypeDefFlags>
    <Attributes>
        <System.Runtime.InteropServices.ClassInterfaceAttribute>
            <ctor>Sub System.Runtime.InteropServices.ClassInterfaceAttribute..ctor(classInterfaceType As System.Runtime.InteropServices.ClassInterfaceType)</ctor>
            <a>0</a>
        </System.Runtime.InteropServices.ClassInterfaceAttribute>
        <Microsoft.VisualBasic.ComClassAttribute>
            <ctor>Sub Microsoft.VisualBasic.ComClassAttribute..ctor()</ctor>
        </Microsoft.VisualBasic.ComClassAttribute>
    </Attributes>
    <Implements>ComClassTest._ComClassTest</Implements>
    <Method Name=".ctor" CallingConvention="HasThis">
        <MethodFlags>public specialname rtspecialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="M1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest._ComClassTest.M1()</Implements>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="get_P1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict specialname virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.InteropServices.DispIdAttribute>
                <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                <a>15</a>
            </System.Runtime.InteropServices.DispIdAttribute>
        </Attributes>
        <Implements>Function ComClassTest._ComClassTest.get_P1() As System.Int32</Implements>
        <Return>
            <Type>Integer</Type>
        </Return>
    </Method>
    <Method Name="set_P1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict specialname virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.InteropServices.DispIdAttribute>
                <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                <a>16</a>
            </System.Runtime.InteropServices.DispIdAttribute>
        </Attributes>
        <Implements>Sub ComClassTest._ComClassTest.set_P1(value As System.Int32)</Implements>
        <Parameter Name="value">
            <ParamFlags></ParamFlags>
            <Type>Integer</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="M3" CallingConvention="HasThis">
        <MethodFlags>public newslot strict virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest._ComClassTest.M3()</Implements>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Property Name="P1">
        <PropertyFlags></PropertyFlags>
        <Get>Function ComClassTest.get_P1() As System.Int32</Get>
        <Set>Sub ComClassTest.set_P1(value As System.Int32)</Set>
    </Property>
    <Interface Name="_ComClassTest">
        <TypeDefFlags>interface nested public abstract auto ansi</TypeDefFlags>
        <Attributes>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>True</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
        </Attributes>
        <Method Name="M1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>1</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Method Name="get_P1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict specialname abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>15</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Integer</Type>
            </Return>
        </Method>
        <Method Name="set_P1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict specialname abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>16</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Parameter Name="value">
                <ParamFlags></ParamFlags>
                <Type>Integer</Type>
            </Parameter>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Method Name="M3" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>3</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Property Name="P1">
            <PropertyFlags></PropertyFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>2</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Get>Function ComClassTest._ComClassTest.get_P1() As System.Int32</Get>
            <Set>Sub ComClassTest._ComClassTest.set_P1(value As System.Int32)</Set>
        </Property>
    </Interface>
</Class>

            Dim verifier = CompileAndVerify(compilationDef,
                                            options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal),
                                            symbolValidator:=Sub(m As ModuleSymbol)
                                                                 Dim pe = DirectCast(m, PEModuleSymbol)
                                                                 AssertReflection(expected,
                                                                                  ReflectComClass(pe, "ComClassTest"))
                                                             End Sub)

        End Sub

        <Fact()>
        Public Sub DispId6()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

<Microsoft.VisualBasic.ComClass()>
Public Class ComClassTest

    Sub M1()
    End Sub

    <DispId(17)>
    Property P1 As Integer
        <DispId(15)>
        Get
            Return 0
        End Get
        <DispId(16)>
        Set(value As Integer)
        End Set
    End Property

    Sub M3()
    End Sub

End Class
    ]]></file>
</compilation>

            Dim expected =
<Class Name="ComClassTest">
    <TypeDefFlags>public auto ansi</TypeDefFlags>
    <Attributes>
        <System.Runtime.InteropServices.ClassInterfaceAttribute>
            <ctor>Sub System.Runtime.InteropServices.ClassInterfaceAttribute..ctor(classInterfaceType As System.Runtime.InteropServices.ClassInterfaceType)</ctor>
            <a>0</a>
        </System.Runtime.InteropServices.ClassInterfaceAttribute>
        <Microsoft.VisualBasic.ComClassAttribute>
            <ctor>Sub Microsoft.VisualBasic.ComClassAttribute..ctor()</ctor>
        </Microsoft.VisualBasic.ComClassAttribute>
    </Attributes>
    <Implements>ComClassTest._ComClassTest</Implements>
    <Method Name=".ctor" CallingConvention="HasThis">
        <MethodFlags>public specialname rtspecialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="M1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest._ComClassTest.M1()</Implements>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="get_P1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict specialname virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.InteropServices.DispIdAttribute>
                <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                <a>15</a>
            </System.Runtime.InteropServices.DispIdAttribute>
        </Attributes>
        <Implements>Function ComClassTest._ComClassTest.get_P1() As System.Int32</Implements>
        <Return>
            <Type>Integer</Type>
        </Return>
    </Method>
    <Method Name="set_P1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict specialname virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.InteropServices.DispIdAttribute>
                <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                <a>16</a>
            </System.Runtime.InteropServices.DispIdAttribute>
        </Attributes>
        <Implements>Sub ComClassTest._ComClassTest.set_P1(value As System.Int32)</Implements>
        <Parameter Name="value">
            <ParamFlags></ParamFlags>
            <Type>Integer</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="M3" CallingConvention="HasThis">
        <MethodFlags>public newslot strict virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest._ComClassTest.M3()</Implements>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Property Name="P1">
        <PropertyFlags></PropertyFlags>
        <Attributes>
            <System.Runtime.InteropServices.DispIdAttribute>
                <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                <a>17</a>
            </System.Runtime.InteropServices.DispIdAttribute>
        </Attributes>
        <Get>Function ComClassTest.get_P1() As System.Int32</Get>
        <Set>Sub ComClassTest.set_P1(value As System.Int32)</Set>
    </Property>
    <Interface Name="_ComClassTest">
        <TypeDefFlags>interface nested public abstract auto ansi</TypeDefFlags>
        <Attributes>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>True</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
        </Attributes>
        <Method Name="M1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>1</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Method Name="get_P1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict specialname abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>15</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Integer</Type>
            </Return>
        </Method>
        <Method Name="set_P1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict specialname abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>16</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Parameter Name="value">
                <ParamFlags></ParamFlags>
                <Type>Integer</Type>
            </Parameter>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Method Name="M3" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>2</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Property Name="P1">
            <PropertyFlags></PropertyFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>17</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Get>Function ComClassTest._ComClassTest.get_P1() As System.Int32</Get>
            <Set>Sub ComClassTest._ComClassTest.set_P1(value As System.Int32)</Set>
        </Property>
    </Interface>
</Class>

            Dim verifier = CompileAndVerify(compilationDef,
                                            options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal),
                                            symbolValidator:=Sub(m As ModuleSymbol)
                                                                 Dim pe = DirectCast(m, PEModuleSymbol)
                                                                 AssertReflection(expected,
                                                                                  ReflectComClass(pe, "ComClassTest"))
                                                             End Sub)

        End Sub

        <Fact()>
        Public Sub DispId7()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

<Microsoft.VisualBasic.ComClass()>
Public Class ComClassTest

    Sub M1()
    End Sub

    <DispId(15)>
    Default Property P1(x As Integer) As Integer
        <DispId(16)>
        Get
            Return 0
        End Get
        Set(value As Integer)
        End Set
    End Property

    Sub M3()
    End Sub

End Class
    ]]></file>
</compilation>

            Dim expected =
<Class Name="ComClassTest">
    <TypeDefFlags>public auto ansi</TypeDefFlags>
    <Attributes>
        <System.Reflection.DefaultMemberAttribute>
            <ctor>Sub System.Reflection.DefaultMemberAttribute..ctor(memberName As System.String)</ctor>
            <a>P1</a>
        </System.Reflection.DefaultMemberAttribute>
        <System.Runtime.InteropServices.ClassInterfaceAttribute>
            <ctor>Sub System.Runtime.InteropServices.ClassInterfaceAttribute..ctor(classInterfaceType As System.Runtime.InteropServices.ClassInterfaceType)</ctor>
            <a>0</a>
        </System.Runtime.InteropServices.ClassInterfaceAttribute>
        <Microsoft.VisualBasic.ComClassAttribute>
            <ctor>Sub Microsoft.VisualBasic.ComClassAttribute..ctor()</ctor>
        </Microsoft.VisualBasic.ComClassAttribute>
    </Attributes>
    <Implements>ComClassTest._ComClassTest</Implements>
    <Method Name=".ctor" CallingConvention="HasThis">
        <MethodFlags>public specialname rtspecialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="M1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest._ComClassTest.M1()</Implements>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="get_P1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict specialname virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.InteropServices.DispIdAttribute>
                <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                <a>16</a>
            </System.Runtime.InteropServices.DispIdAttribute>
        </Attributes>
        <Implements>Function ComClassTest._ComClassTest.get_P1(x As System.Int32) As System.Int32</Implements>
        <Parameter Name="x">
            <ParamFlags></ParamFlags>
            <Type>Integer</Type>
        </Parameter>
        <Return>
            <Type>Integer</Type>
        </Return>
    </Method>
    <Method Name="set_P1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict specialname virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest._ComClassTest.set_P1(x As System.Int32, value As System.Int32)</Implements>
        <Parameter Name="x">
            <ParamFlags></ParamFlags>
            <Type>Integer</Type>
        </Parameter>
        <Parameter Name="value">
            <ParamFlags></ParamFlags>
            <Type>Integer</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="M3" CallingConvention="HasThis">
        <MethodFlags>public newslot strict virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest._ComClassTest.M3()</Implements>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Property Name="P1">
        <PropertyFlags></PropertyFlags>
        <Attributes>
            <System.Runtime.InteropServices.DispIdAttribute>
                <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                <a>15</a>
            </System.Runtime.InteropServices.DispIdAttribute>
        </Attributes>
        <Get>Function ComClassTest.get_P1(x As System.Int32) As System.Int32</Get>
        <Set>Sub ComClassTest.set_P1(x As System.Int32, value As System.Int32)</Set>
    </Property>
    <Interface Name="_ComClassTest">
        <TypeDefFlags>interface nested public abstract auto ansi</TypeDefFlags>
        <Attributes>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>True</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
            <System.Reflection.DefaultMemberAttribute>
                <ctor>Sub System.Reflection.DefaultMemberAttribute..ctor(memberName As System.String)</ctor>
                <a>P1</a>
            </System.Reflection.DefaultMemberAttribute>
        </Attributes>
        <Method Name="M1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>1</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Method Name="get_P1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict specialname abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>16</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Parameter Name="x">
                <ParamFlags></ParamFlags>
                <Type>Integer</Type>
            </Parameter>
            <Return>
                <Type>Integer</Type>
            </Return>
        </Method>
        <Method Name="set_P1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict specialname abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>0</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Parameter Name="x">
                <ParamFlags></ParamFlags>
                <Type>Integer</Type>
            </Parameter>
            <Parameter Name="value">
                <ParamFlags></ParamFlags>
                <Type>Integer</Type>
            </Parameter>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Method Name="M3" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>3</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Property Name="P1">
            <PropertyFlags></PropertyFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>15</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Get>Function ComClassTest._ComClassTest.get_P1(x As System.Int32) As System.Int32</Get>
            <Set>Sub ComClassTest._ComClassTest.set_P1(x As System.Int32, value As System.Int32)</Set>
        </Property>
    </Interface>
</Class>

            Dim verifier = CompileAndVerify(compilationDef,
                                            options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal),
                                                        symbolValidator:=Sub(m As ModuleSymbol)
                                                                             Dim pe = DirectCast(m, PEModuleSymbol)
                                                                             AssertReflection(expected,
                                                                                              ReflectComClass(pe, "ComClassTest"))
                                                                         End Sub)

        End Sub

        <Fact()>
        Public Sub DispId8()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

<Microsoft.VisualBasic.ComClass()>
Public Class ComClassTest

    Sub M1()
    End Sub

    <DispId(15)>
    Default Property P1(x As Integer) As Integer
        Get
            Return 0
        End Get
        <DispId(16)>
        Set(value As Integer)
        End Set
    End Property

    Sub M3()
    End Sub

End Class
    ]]></file>
</compilation>

            Dim expected =
<Class Name="ComClassTest">
    <TypeDefFlags>public auto ansi</TypeDefFlags>
    <Attributes>
        <System.Reflection.DefaultMemberAttribute>
            <ctor>Sub System.Reflection.DefaultMemberAttribute..ctor(memberName As System.String)</ctor>
            <a>P1</a>
        </System.Reflection.DefaultMemberAttribute>
        <System.Runtime.InteropServices.ClassInterfaceAttribute>
            <ctor>Sub System.Runtime.InteropServices.ClassInterfaceAttribute..ctor(classInterfaceType As System.Runtime.InteropServices.ClassInterfaceType)</ctor>
            <a>0</a>
        </System.Runtime.InteropServices.ClassInterfaceAttribute>
        <Microsoft.VisualBasic.ComClassAttribute>
            <ctor>Sub Microsoft.VisualBasic.ComClassAttribute..ctor()</ctor>
        </Microsoft.VisualBasic.ComClassAttribute>
    </Attributes>
    <Implements>ComClassTest._ComClassTest</Implements>
    <Method Name=".ctor" CallingConvention="HasThis">
        <MethodFlags>public specialname rtspecialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="M1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest._ComClassTest.M1()</Implements>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="get_P1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict specialname virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Function ComClassTest._ComClassTest.get_P1(x As System.Int32) As System.Int32</Implements>
        <Parameter Name="x">
            <ParamFlags></ParamFlags>
            <Type>Integer</Type>
        </Parameter>
        <Return>
            <Type>Integer</Type>
        </Return>
    </Method>
    <Method Name="set_P1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict specialname virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.InteropServices.DispIdAttribute>
                <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                <a>16</a>
            </System.Runtime.InteropServices.DispIdAttribute>
        </Attributes>
        <Implements>Sub ComClassTest._ComClassTest.set_P1(x As System.Int32, value As System.Int32)</Implements>
        <Parameter Name="x">
            <ParamFlags></ParamFlags>
            <Type>Integer</Type>
        </Parameter>
        <Parameter Name="value">
            <ParamFlags></ParamFlags>
            <Type>Integer</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="M3" CallingConvention="HasThis">
        <MethodFlags>public newslot strict virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest._ComClassTest.M3()</Implements>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Property Name="P1">
        <PropertyFlags></PropertyFlags>
        <Attributes>
            <System.Runtime.InteropServices.DispIdAttribute>
                <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                <a>15</a>
            </System.Runtime.InteropServices.DispIdAttribute>
        </Attributes>
        <Get>Function ComClassTest.get_P1(x As System.Int32) As System.Int32</Get>
        <Set>Sub ComClassTest.set_P1(x As System.Int32, value As System.Int32)</Set>
    </Property>
    <Interface Name="_ComClassTest">
        <TypeDefFlags>interface nested public abstract auto ansi</TypeDefFlags>
        <Attributes>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>True</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
            <System.Reflection.DefaultMemberAttribute>
                <ctor>Sub System.Reflection.DefaultMemberAttribute..ctor(memberName As System.String)</ctor>
                <a>P1</a>
            </System.Reflection.DefaultMemberAttribute>
        </Attributes>
        <Method Name="M1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>1</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Method Name="get_P1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict specialname abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>0</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Parameter Name="x">
                <ParamFlags></ParamFlags>
                <Type>Integer</Type>
            </Parameter>
            <Return>
                <Type>Integer</Type>
            </Return>
        </Method>
        <Method Name="set_P1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict specialname abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>16</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Parameter Name="x">
                <ParamFlags></ParamFlags>
                <Type>Integer</Type>
            </Parameter>
            <Parameter Name="value">
                <ParamFlags></ParamFlags>
                <Type>Integer</Type>
            </Parameter>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Method Name="M3" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>3</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Property Name="P1">
            <PropertyFlags></PropertyFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>15</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Get>Function ComClassTest._ComClassTest.get_P1(x As System.Int32) As System.Int32</Get>
            <Set>Sub ComClassTest._ComClassTest.set_P1(x As System.Int32, value As System.Int32)</Set>
        </Property>
    </Interface>
</Class>

            Dim verifier = CompileAndVerify(compilationDef,
                                            options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal),
                                            symbolValidator:=Sub(m As ModuleSymbol)
                                                                 Dim pe = DirectCast(m, PEModuleSymbol)
                                                                 AssertReflection(expected,
                                                                                  ReflectComClass(pe, "ComClassTest"))
                                                             End Sub)

        End Sub

        <Fact()>
        Public Sub DispId9()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

<Microsoft.VisualBasic.ComClass()>
Public Class ComClassTest

    Sub M1()
    End Sub

    Default Property P1(x As Integer) As Integer
        <DispId(15)>
        Get
            Return 0
        End Get
        <DispId(16)>
        Set(value As Integer)
        End Set
    End Property

    Sub M3()
    End Sub

End Class
    ]]></file>
</compilation>

            Dim expected =
<Class Name="ComClassTest">
    <TypeDefFlags>public auto ansi</TypeDefFlags>
    <Attributes>
        <System.Reflection.DefaultMemberAttribute>
            <ctor>Sub System.Reflection.DefaultMemberAttribute..ctor(memberName As System.String)</ctor>
            <a>P1</a>
        </System.Reflection.DefaultMemberAttribute>
        <System.Runtime.InteropServices.ClassInterfaceAttribute>
            <ctor>Sub System.Runtime.InteropServices.ClassInterfaceAttribute..ctor(classInterfaceType As System.Runtime.InteropServices.ClassInterfaceType)</ctor>
            <a>0</a>
        </System.Runtime.InteropServices.ClassInterfaceAttribute>
        <Microsoft.VisualBasic.ComClassAttribute>
            <ctor>Sub Microsoft.VisualBasic.ComClassAttribute..ctor()</ctor>
        </Microsoft.VisualBasic.ComClassAttribute>
    </Attributes>
    <Implements>ComClassTest._ComClassTest</Implements>
    <Method Name=".ctor" CallingConvention="HasThis">
        <MethodFlags>public specialname rtspecialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="M1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest._ComClassTest.M1()</Implements>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="get_P1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict specialname virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.InteropServices.DispIdAttribute>
                <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                <a>15</a>
            </System.Runtime.InteropServices.DispIdAttribute>
        </Attributes>
        <Implements>Function ComClassTest._ComClassTest.get_P1(x As System.Int32) As System.Int32</Implements>
        <Parameter Name="x">
            <ParamFlags></ParamFlags>
            <Type>Integer</Type>
        </Parameter>
        <Return>
            <Type>Integer</Type>
        </Return>
    </Method>
    <Method Name="set_P1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict specialname virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.InteropServices.DispIdAttribute>
                <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                <a>16</a>
            </System.Runtime.InteropServices.DispIdAttribute>
        </Attributes>
        <Implements>Sub ComClassTest._ComClassTest.set_P1(x As System.Int32, value As System.Int32)</Implements>
        <Parameter Name="x">
            <ParamFlags></ParamFlags>
            <Type>Integer</Type>
        </Parameter>
        <Parameter Name="value">
            <ParamFlags></ParamFlags>
            <Type>Integer</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="M3" CallingConvention="HasThis">
        <MethodFlags>public newslot strict virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest._ComClassTest.M3()</Implements>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Property Name="P1">
        <PropertyFlags></PropertyFlags>
        <Get>Function ComClassTest.get_P1(x As System.Int32) As System.Int32</Get>
        <Set>Sub ComClassTest.set_P1(x As System.Int32, value As System.Int32)</Set>
    </Property>
    <Interface Name="_ComClassTest">
        <TypeDefFlags>interface nested public abstract auto ansi</TypeDefFlags>
        <Attributes>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>True</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
            <System.Reflection.DefaultMemberAttribute>
                <ctor>Sub System.Reflection.DefaultMemberAttribute..ctor(memberName As System.String)</ctor>
                <a>P1</a>
            </System.Reflection.DefaultMemberAttribute>
        </Attributes>
        <Method Name="M1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>1</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Method Name="get_P1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict specialname abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>15</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Parameter Name="x">
                <ParamFlags></ParamFlags>
                <Type>Integer</Type>
            </Parameter>
            <Return>
                <Type>Integer</Type>
            </Return>
        </Method>
        <Method Name="set_P1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict specialname abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>16</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Parameter Name="x">
                <ParamFlags></ParamFlags>
                <Type>Integer</Type>
            </Parameter>
            <Parameter Name="value">
                <ParamFlags></ParamFlags>
                <Type>Integer</Type>
            </Parameter>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Method Name="M3" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>3</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Property Name="P1">
            <PropertyFlags></PropertyFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>0</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Get>Function ComClassTest._ComClassTest.get_P1(x As System.Int32) As System.Int32</Get>
            <Set>Sub ComClassTest._ComClassTest.set_P1(x As System.Int32, value As System.Int32)</Set>
        </Property>
    </Interface>
</Class>

            Dim verifier = CompileAndVerify(compilationDef,
                                            options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal),
                                                                                        symbolValidator:=Sub(m As ModuleSymbol)
                                                                                                             Dim pe = DirectCast(m, PEModuleSymbol)
                                                                                                             AssertReflection(expected,
                                                                                                                              ReflectComClass(pe, "ComClassTest"))
                                                                                                         End Sub)

        End Sub

        <Fact()>
        Public Sub DispId10()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

<Microsoft.VisualBasic.ComClass()>
Public Class ComClassTest

    Sub M1()
    End Sub

    <DispId(17)>
    Default Property P1(x As Integer) As Integer
        <DispId(15)>
        Get
            Return 0
        End Get
        <DispId(16)>
        Set(value As Integer)
        End Set
    End Property

    Sub M3()
    End Sub

End Class
    ]]></file>
</compilation>

            Dim expected =
<Class Name="ComClassTest">
    <TypeDefFlags>public auto ansi</TypeDefFlags>
    <Attributes>
        <System.Reflection.DefaultMemberAttribute>
            <ctor>Sub System.Reflection.DefaultMemberAttribute..ctor(memberName As System.String)</ctor>
            <a>P1</a>
        </System.Reflection.DefaultMemberAttribute>
        <System.Runtime.InteropServices.ClassInterfaceAttribute>
            <ctor>Sub System.Runtime.InteropServices.ClassInterfaceAttribute..ctor(classInterfaceType As System.Runtime.InteropServices.ClassInterfaceType)</ctor>
            <a>0</a>
        </System.Runtime.InteropServices.ClassInterfaceAttribute>
        <Microsoft.VisualBasic.ComClassAttribute>
            <ctor>Sub Microsoft.VisualBasic.ComClassAttribute..ctor()</ctor>
        </Microsoft.VisualBasic.ComClassAttribute>
    </Attributes>
    <Implements>ComClassTest._ComClassTest</Implements>
    <Method Name=".ctor" CallingConvention="HasThis">
        <MethodFlags>public specialname rtspecialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="M1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest._ComClassTest.M1()</Implements>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="get_P1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict specialname virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.InteropServices.DispIdAttribute>
                <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                <a>15</a>
            </System.Runtime.InteropServices.DispIdAttribute>
        </Attributes>
        <Implements>Function ComClassTest._ComClassTest.get_P1(x As System.Int32) As System.Int32</Implements>
        <Parameter Name="x">
            <ParamFlags></ParamFlags>
            <Type>Integer</Type>
        </Parameter>
        <Return>
            <Type>Integer</Type>
        </Return>
    </Method>
    <Method Name="set_P1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict specialname virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.InteropServices.DispIdAttribute>
                <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                <a>16</a>
            </System.Runtime.InteropServices.DispIdAttribute>
        </Attributes>
        <Implements>Sub ComClassTest._ComClassTest.set_P1(x As System.Int32, value As System.Int32)</Implements>
        <Parameter Name="x">
            <ParamFlags></ParamFlags>
            <Type>Integer</Type>
        </Parameter>
        <Parameter Name="value">
            <ParamFlags></ParamFlags>
            <Type>Integer</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="M3" CallingConvention="HasThis">
        <MethodFlags>public newslot strict virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest._ComClassTest.M3()</Implements>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Property Name="P1">
        <PropertyFlags></PropertyFlags>
        <Attributes>
            <System.Runtime.InteropServices.DispIdAttribute>
                <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                <a>17</a>
            </System.Runtime.InteropServices.DispIdAttribute>
        </Attributes>
        <Get>Function ComClassTest.get_P1(x As System.Int32) As System.Int32</Get>
        <Set>Sub ComClassTest.set_P1(x As System.Int32, value As System.Int32)</Set>
    </Property>
    <Interface Name="_ComClassTest">
        <TypeDefFlags>interface nested public abstract auto ansi</TypeDefFlags>
        <Attributes>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>True</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
            <System.Reflection.DefaultMemberAttribute>
                <ctor>Sub System.Reflection.DefaultMemberAttribute..ctor(memberName As System.String)</ctor>
                <a>P1</a>
            </System.Reflection.DefaultMemberAttribute>
        </Attributes>
        <Method Name="M1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>1</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Method Name="get_P1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict specialname abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>15</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Parameter Name="x">
                <ParamFlags></ParamFlags>
                <Type>Integer</Type>
            </Parameter>
            <Return>
                <Type>Integer</Type>
            </Return>
        </Method>
        <Method Name="set_P1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict specialname abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>16</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Parameter Name="x">
                <ParamFlags></ParamFlags>
                <Type>Integer</Type>
            </Parameter>
            <Parameter Name="value">
                <ParamFlags></ParamFlags>
                <Type>Integer</Type>
            </Parameter>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Method Name="M3" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>2</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Property Name="P1">
            <PropertyFlags></PropertyFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>17</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Get>Function ComClassTest._ComClassTest.get_P1(x As System.Int32) As System.Int32</Get>
            <Set>Sub ComClassTest._ComClassTest.set_P1(x As System.Int32, value As System.Int32)</Set>
        </Property>
    </Interface>
</Class>

            Dim verifier = CompileAndVerify(compilationDef,
                                            options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal),
                                            symbolValidator:=Sub(m As ModuleSymbol)
                                                                 Dim pe = DirectCast(m, PEModuleSymbol)
                                                                 AssertReflection(expected,
                                                                                  ReflectComClass(pe, "ComClassTest"))
                                                             End Sub)

        End Sub

        <Fact()>
        Public Sub DispId11()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

<Microsoft.VisualBasic.ComClass()>
Public Class ComClassTest

    Sub M1()
    End Sub

    <DispId(17)>
    Default ReadOnly Property P1(x As Integer) As Integer
        <DispId(15)>
        Get
            Return 0
        End Get
    End Property

    Sub M3()
    End Sub

End Class
    ]]></file>
</compilation>

            Dim expected =
<Class Name="ComClassTest">
    <TypeDefFlags>public auto ansi</TypeDefFlags>
    <Attributes>
        <System.Reflection.DefaultMemberAttribute>
            <ctor>Sub System.Reflection.DefaultMemberAttribute..ctor(memberName As System.String)</ctor>
            <a>P1</a>
        </System.Reflection.DefaultMemberAttribute>
        <System.Runtime.InteropServices.ClassInterfaceAttribute>
            <ctor>Sub System.Runtime.InteropServices.ClassInterfaceAttribute..ctor(classInterfaceType As System.Runtime.InteropServices.ClassInterfaceType)</ctor>
            <a>0</a>
        </System.Runtime.InteropServices.ClassInterfaceAttribute>
        <Microsoft.VisualBasic.ComClassAttribute>
            <ctor>Sub Microsoft.VisualBasic.ComClassAttribute..ctor()</ctor>
        </Microsoft.VisualBasic.ComClassAttribute>
    </Attributes>
    <Implements>ComClassTest._ComClassTest</Implements>
    <Method Name=".ctor" CallingConvention="HasThis">
        <MethodFlags>public specialname rtspecialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="M1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest._ComClassTest.M1()</Implements>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="get_P1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict specialname virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.InteropServices.DispIdAttribute>
                <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                <a>15</a>
            </System.Runtime.InteropServices.DispIdAttribute>
        </Attributes>
        <Implements>Function ComClassTest._ComClassTest.get_P1(x As System.Int32) As System.Int32</Implements>
        <Parameter Name="x">
            <ParamFlags></ParamFlags>
            <Type>Integer</Type>
        </Parameter>
        <Return>
            <Type>Integer</Type>
        </Return>
    </Method>
    <Method Name="M3" CallingConvention="HasThis">
        <MethodFlags>public newslot strict virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest._ComClassTest.M3()</Implements>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Property Name="P1">
        <PropertyFlags></PropertyFlags>
        <Attributes>
            <System.Runtime.InteropServices.DispIdAttribute>
                <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                <a>17</a>
            </System.Runtime.InteropServices.DispIdAttribute>
        </Attributes>
        <Get>Function ComClassTest.get_P1(x As System.Int32) As System.Int32</Get>
    </Property>
    <Interface Name="_ComClassTest">
        <TypeDefFlags>interface nested public abstract auto ansi</TypeDefFlags>
        <Attributes>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>True</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
            <System.Reflection.DefaultMemberAttribute>
                <ctor>Sub System.Reflection.DefaultMemberAttribute..ctor(memberName As System.String)</ctor>
                <a>P1</a>
            </System.Reflection.DefaultMemberAttribute>
        </Attributes>
        <Method Name="M1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>1</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Method Name="get_P1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict specialname abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>15</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Parameter Name="x">
                <ParamFlags></ParamFlags>
                <Type>Integer</Type>
            </Parameter>
            <Return>
                <Type>Integer</Type>
            </Return>
        </Method>
        <Method Name="M3" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>2</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Property Name="P1">
            <PropertyFlags></PropertyFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>17</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Get>Function ComClassTest._ComClassTest.get_P1(x As System.Int32) As System.Int32</Get>
        </Property>
    </Interface>
</Class>

            Dim verifier = CompileAndVerify(compilationDef,
                                            options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal),
                                            symbolValidator:=Sub(m As ModuleSymbol)
                                                                 Dim pe = DirectCast(m, PEModuleSymbol)
                                                                 AssertReflection(expected,
                                                                                  ReflectComClass(pe, "ComClassTest"))
                                                             End Sub)

        End Sub

        <Fact()>
        Public Sub DispId12()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

<Microsoft.VisualBasic.ComClass()>
Public Class ComClassTest

    Sub M1()
    End Sub

    <DispId(17)>
    Default WriteOnly Property P1(x As Integer) As Integer
        <DispId(16)>
        Set(value As Integer)
        End Set
    End Property

    Sub M3()
    End Sub

End Class
    ]]></file>
</compilation>

            Dim expected =
<Class Name="ComClassTest">
    <TypeDefFlags>public auto ansi</TypeDefFlags>
    <Attributes>
        <System.Reflection.DefaultMemberAttribute>
            <ctor>Sub System.Reflection.DefaultMemberAttribute..ctor(memberName As System.String)</ctor>
            <a>P1</a>
        </System.Reflection.DefaultMemberAttribute>
        <System.Runtime.InteropServices.ClassInterfaceAttribute>
            <ctor>Sub System.Runtime.InteropServices.ClassInterfaceAttribute..ctor(classInterfaceType As System.Runtime.InteropServices.ClassInterfaceType)</ctor>
            <a>0</a>
        </System.Runtime.InteropServices.ClassInterfaceAttribute>
        <Microsoft.VisualBasic.ComClassAttribute>
            <ctor>Sub Microsoft.VisualBasic.ComClassAttribute..ctor()</ctor>
        </Microsoft.VisualBasic.ComClassAttribute>
    </Attributes>
    <Implements>ComClassTest._ComClassTest</Implements>
    <Method Name=".ctor" CallingConvention="HasThis">
        <MethodFlags>public specialname rtspecialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="M1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest._ComClassTest.M1()</Implements>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="set_P1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict specialname virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.InteropServices.DispIdAttribute>
                <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                <a>16</a>
            </System.Runtime.InteropServices.DispIdAttribute>
        </Attributes>
        <Implements>Sub ComClassTest._ComClassTest.set_P1(x As System.Int32, value As System.Int32)</Implements>
        <Parameter Name="x">
            <ParamFlags></ParamFlags>
            <Type>Integer</Type>
        </Parameter>
        <Parameter Name="value">
            <ParamFlags></ParamFlags>
            <Type>Integer</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="M3" CallingConvention="HasThis">
        <MethodFlags>public newslot strict virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest._ComClassTest.M3()</Implements>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Property Name="P1">
        <PropertyFlags></PropertyFlags>
        <Attributes>
            <System.Runtime.InteropServices.DispIdAttribute>
                <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                <a>17</a>
            </System.Runtime.InteropServices.DispIdAttribute>
        </Attributes>
        <Set>Sub ComClassTest.set_P1(x As System.Int32, value As System.Int32)</Set>
    </Property>
    <Interface Name="_ComClassTest">
        <TypeDefFlags>interface nested public abstract auto ansi</TypeDefFlags>
        <Attributes>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>True</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
            <System.Reflection.DefaultMemberAttribute>
                <ctor>Sub System.Reflection.DefaultMemberAttribute..ctor(memberName As System.String)</ctor>
                <a>P1</a>
            </System.Reflection.DefaultMemberAttribute>
        </Attributes>
        <Method Name="M1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>1</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Method Name="set_P1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict specialname abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>16</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Parameter Name="x">
                <ParamFlags></ParamFlags>
                <Type>Integer</Type>
            </Parameter>
            <Parameter Name="value">
                <ParamFlags></ParamFlags>
                <Type>Integer</Type>
            </Parameter>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Method Name="M3" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>2</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Property Name="P1">
            <PropertyFlags></PropertyFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>17</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Set>Sub ComClassTest._ComClassTest.set_P1(x As System.Int32, value As System.Int32)</Set>
        </Property>
    </Interface>
</Class>

            Dim verifier = CompileAndVerify(compilationDef,
                                            options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal),
                                            symbolValidator:=Sub(m As ModuleSymbol)
                                                                 Dim pe = DirectCast(m, PEModuleSymbol)
                                                                 AssertReflection(expected,
                                                                                  ReflectComClass(pe, "ComClassTest"))
                                                             End Sub)

        End Sub

        <Fact()>
        Public Sub DispId13()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

<Microsoft.VisualBasic.ComClass()>
Public Class ComClassTest

    Sub M1()
    End Sub

    Function GetEnumerator() As Collections.IEnumerator
        Return Nothing
    End Function

    Sub M3()
    End Sub

End Class
    ]]></file>
</compilation>

            Dim expected =
<Class Name="ComClassTest">
    <TypeDefFlags>public auto ansi</TypeDefFlags>
    <Attributes>
        <System.Runtime.InteropServices.ClassInterfaceAttribute>
            <ctor>Sub System.Runtime.InteropServices.ClassInterfaceAttribute..ctor(classInterfaceType As System.Runtime.InteropServices.ClassInterfaceType)</ctor>
            <a>0</a>
        </System.Runtime.InteropServices.ClassInterfaceAttribute>
        <Microsoft.VisualBasic.ComClassAttribute>
            <ctor>Sub Microsoft.VisualBasic.ComClassAttribute..ctor()</ctor>
        </Microsoft.VisualBasic.ComClassAttribute>
    </Attributes>
    <Implements>ComClassTest._ComClassTest</Implements>
    <Method Name=".ctor" CallingConvention="HasThis">
        <MethodFlags>public specialname rtspecialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="M1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest._ComClassTest.M1()</Implements>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="GetEnumerator" CallingConvention="HasThis">
        <MethodFlags>public newslot strict virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Function ComClassTest._ComClassTest.GetEnumerator() As System.Collections.IEnumerator</Implements>
        <Return>
            <Type>System.Collections.IEnumerator</Type>
        </Return>
    </Method>
    <Method Name="M3" CallingConvention="HasThis">
        <MethodFlags>public newslot strict virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest._ComClassTest.M3()</Implements>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Interface Name="_ComClassTest">
        <TypeDefFlags>interface nested public abstract auto ansi</TypeDefFlags>
        <Attributes>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>True</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
        </Attributes>
        <Method Name="M1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>1</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Method Name="GetEnumerator" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>-4</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>System.Collections.IEnumerator</Type>
            </Return>
        </Method>
        <Method Name="M3" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>3</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
    </Interface>
</Class>

            Dim verifier = CompileAndVerify(compilationDef,
                                            options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal),
                                            symbolValidator:=Sub(m As ModuleSymbol)
                                                                 Dim pe = DirectCast(m, PEModuleSymbol)
                                                                 AssertReflection(expected,
                                                                                  ReflectComClass(pe, "ComClassTest"))
                                                             End Sub)

        End Sub

        <Fact()>
        Public Sub DispId14()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

<Microsoft.VisualBasic.ComClass()>
Public Class ComClassTest

    Sub M1()
    End Sub

    <DispId(13)>
    Function GetEnumerator() As Collections.IEnumerator
        Return Nothing
    End Function

    Sub M3()
    End Sub

End Class
    ]]></file>
</compilation>

            Dim expected =
<Class Name="ComClassTest">
    <TypeDefFlags>public auto ansi</TypeDefFlags>
    <Attributes>
        <System.Runtime.InteropServices.ClassInterfaceAttribute>
            <ctor>Sub System.Runtime.InteropServices.ClassInterfaceAttribute..ctor(classInterfaceType As System.Runtime.InteropServices.ClassInterfaceType)</ctor>
            <a>0</a>
        </System.Runtime.InteropServices.ClassInterfaceAttribute>
        <Microsoft.VisualBasic.ComClassAttribute>
            <ctor>Sub Microsoft.VisualBasic.ComClassAttribute..ctor()</ctor>
        </Microsoft.VisualBasic.ComClassAttribute>
    </Attributes>
    <Implements>ComClassTest._ComClassTest</Implements>
    <Method Name=".ctor" CallingConvention="HasThis">
        <MethodFlags>public specialname rtspecialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="M1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest._ComClassTest.M1()</Implements>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="GetEnumerator" CallingConvention="HasThis">
        <MethodFlags>public newslot strict virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.InteropServices.DispIdAttribute>
                <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                <a>13</a>
            </System.Runtime.InteropServices.DispIdAttribute>
        </Attributes>
        <Implements>Function ComClassTest._ComClassTest.GetEnumerator() As System.Collections.IEnumerator</Implements>
        <Return>
            <Type>System.Collections.IEnumerator</Type>
        </Return>
    </Method>
    <Method Name="M3" CallingConvention="HasThis">
        <MethodFlags>public newslot strict virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest._ComClassTest.M3()</Implements>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Interface Name="_ComClassTest">
        <TypeDefFlags>interface nested public abstract auto ansi</TypeDefFlags>
        <Attributes>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>True</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
        </Attributes>
        <Method Name="M1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>1</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Method Name="GetEnumerator" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>13</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>System.Collections.IEnumerator</Type>
            </Return>
        </Method>
        <Method Name="M3" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>2</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
    </Interface>
</Class>

            Dim verifier = CompileAndVerify(compilationDef,
                                            options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal),
                                            symbolValidator:=Sub(m As ModuleSymbol)
                                                                 Dim pe = DirectCast(m, PEModuleSymbol)
                                                                 AssertReflection(expected,
                                                                                  ReflectComClass(pe, "ComClassTest"))
                                                             End Sub)

        End Sub

        <Fact()>
        Public Sub DispId15()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

<Microsoft.VisualBasic.ComClass()>
Public Class ComClassTest

    Function GetEnumerator(Optional x As Integer = 0) As Collections.IEnumerator
        Return Nothing
    End Function

    Sub GetEnumerator()
    End Sub
End Class
    ]]></file>
</compilation>

            Dim expected =
<Class Name="ComClassTest">
    <TypeDefFlags>public auto ansi</TypeDefFlags>
    <Attributes>
        <System.Runtime.InteropServices.ClassInterfaceAttribute>
            <ctor>Sub System.Runtime.InteropServices.ClassInterfaceAttribute..ctor(classInterfaceType As System.Runtime.InteropServices.ClassInterfaceType)</ctor>
            <a>0</a>
        </System.Runtime.InteropServices.ClassInterfaceAttribute>
        <Microsoft.VisualBasic.ComClassAttribute>
            <ctor>Sub Microsoft.VisualBasic.ComClassAttribute..ctor()</ctor>
        </Microsoft.VisualBasic.ComClassAttribute>
    </Attributes>
    <Implements>ComClassTest._ComClassTest</Implements>
    <Method Name=".ctor" CallingConvention="HasThis">
        <MethodFlags>public specialname rtspecialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="GetEnumerator" CallingConvention="HasThis">
        <MethodFlags>public newslot strict virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Function ComClassTest._ComClassTest.GetEnumerator([x As System.Int32 = 0]) As System.Collections.IEnumerator</Implements>
        <Parameter Name="x">
            <ParamFlags>[opt] default</ParamFlags>
            <Type>Integer</Type>
            <Default>0</Default>
        </Parameter>
        <Return>
            <Type>System.Collections.IEnumerator</Type>
        </Return>
    </Method>
    <Method Name="GetEnumerator" CallingConvention="HasThis">
        <MethodFlags>public newslot strict virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest._ComClassTest.GetEnumerator()</Implements>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Interface Name="_ComClassTest">
        <TypeDefFlags>interface nested public abstract auto ansi</TypeDefFlags>
        <Attributes>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>True</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
        </Attributes>
        <Method Name="GetEnumerator" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>1</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Parameter Name="x">
                <ParamFlags>[opt] default</ParamFlags>
                <Type>Integer</Type>
                <Default>0</Default>
            </Parameter>
            <Return>
                <Type>System.Collections.IEnumerator</Type>
            </Return>
        </Method>
        <Method Name="GetEnumerator" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>2</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
    </Interface>
</Class>

            Dim verifier = CompileAndVerify(compilationDef,
                                            options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal),
                                            symbolValidator:=Sub(m As ModuleSymbol)
                                                                 Dim pe = DirectCast(m, PEModuleSymbol)
                                                                 AssertReflection(expected,
                                                                                  ReflectComClass(pe, "ComClassTest"))
                                                             End Sub)

        End Sub

        <Fact()>
        Public Sub DispId16()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

<Microsoft.VisualBasic.ComClass()>
Public Class ComClassTest

    Function GetEnumerator() As Integer
        Return Nothing
    End Function
End Class
    ]]></file>
</compilation>

            Dim expected =
<Class Name="ComClassTest">
    <TypeDefFlags>public auto ansi</TypeDefFlags>
    <Attributes>
        <System.Runtime.InteropServices.ClassInterfaceAttribute>
            <ctor>Sub System.Runtime.InteropServices.ClassInterfaceAttribute..ctor(classInterfaceType As System.Runtime.InteropServices.ClassInterfaceType)</ctor>
            <a>0</a>
        </System.Runtime.InteropServices.ClassInterfaceAttribute>
        <Microsoft.VisualBasic.ComClassAttribute>
            <ctor>Sub Microsoft.VisualBasic.ComClassAttribute..ctor()</ctor>
        </Microsoft.VisualBasic.ComClassAttribute>
    </Attributes>
    <Implements>ComClassTest._ComClassTest</Implements>
    <Method Name=".ctor" CallingConvention="HasThis">
        <MethodFlags>public specialname rtspecialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="GetEnumerator" CallingConvention="HasThis">
        <MethodFlags>public newslot strict virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Function ComClassTest._ComClassTest.GetEnumerator() As System.Int32</Implements>
        <Return>
            <Type>Integer</Type>
        </Return>
    </Method>
    <Interface Name="_ComClassTest">
        <TypeDefFlags>interface nested public abstract auto ansi</TypeDefFlags>
        <Attributes>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>True</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
        </Attributes>
        <Method Name="GetEnumerator" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>1</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Integer</Type>
            </Return>
        </Method>
    </Interface>
</Class>

            Dim verifier = CompileAndVerify(compilationDef,
                                            options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal),
                                            symbolValidator:=Sub(m As ModuleSymbol)
                                                                 Dim pe = DirectCast(m, PEModuleSymbol)
                                                                 AssertReflection(expected,
                                                                                  ReflectComClass(pe, "ComClassTest"))
                                                             End Sub)

        End Sub

        <Fact()>
        Public Sub DispId17()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

<Microsoft.VisualBasic.ComClass()>
Public Class ComClassTest
    ReadOnly Property GetEnumerator() As Collections.IEnumerator
        Get
            Return Nothing
        End Get
    End Property

    ReadOnly Property GetEnumerator(Optional x As Integer = 0) As Collections.IEnumerator
        Get
            Return Nothing
        End Get
    End Property
End Class
    ]]></file>
</compilation>

            Dim expected =
<Class Name="ComClassTest">
    <TypeDefFlags>public auto ansi</TypeDefFlags>
    <Attributes>
        <System.Runtime.InteropServices.ClassInterfaceAttribute>
            <ctor>Sub System.Runtime.InteropServices.ClassInterfaceAttribute..ctor(classInterfaceType As System.Runtime.InteropServices.ClassInterfaceType)</ctor>
            <a>0</a>
        </System.Runtime.InteropServices.ClassInterfaceAttribute>
        <Microsoft.VisualBasic.ComClassAttribute>
            <ctor>Sub Microsoft.VisualBasic.ComClassAttribute..ctor()</ctor>
        </Microsoft.VisualBasic.ComClassAttribute>
    </Attributes>
    <Implements>ComClassTest._ComClassTest</Implements>
    <Method Name=".ctor" CallingConvention="HasThis">
        <MethodFlags>public specialname rtspecialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="get_GetEnumerator" CallingConvention="HasThis">
        <MethodFlags>public newslot strict specialname virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Function ComClassTest._ComClassTest.get_GetEnumerator() As System.Collections.IEnumerator</Implements>
        <Return>
            <Type>System.Collections.IEnumerator</Type>
        </Return>
    </Method>
    <Method Name="get_GetEnumerator" CallingConvention="HasThis">
        <MethodFlags>public newslot strict specialname virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Function ComClassTest._ComClassTest.get_GetEnumerator([x As System.Int32 = 0]) As System.Collections.IEnumerator</Implements>
        <Parameter Name="x">
            <ParamFlags>[opt] default</ParamFlags>
            <Type>Integer</Type>
            <Default>0</Default>
        </Parameter>
        <Return>
            <Type>System.Collections.IEnumerator</Type>
        </Return>
    </Method>
    <Property Name="GetEnumerator">
        <PropertyFlags></PropertyFlags>
        <Get>Function ComClassTest.get_GetEnumerator() As System.Collections.IEnumerator</Get>
    </Property>
    <Property Name="GetEnumerator">
        <PropertyFlags></PropertyFlags>
        <Get>Function ComClassTest.get_GetEnumerator([x As System.Int32 = 0]) As System.Collections.IEnumerator</Get>
    </Property>
    <Interface Name="_ComClassTest">
        <TypeDefFlags>interface nested public abstract auto ansi</TypeDefFlags>
        <Attributes>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>True</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
        </Attributes>
        <Method Name="get_GetEnumerator" CallingConvention="HasThis">
            <MethodFlags>public newslot strict specialname abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>-4</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>System.Collections.IEnumerator</Type>
            </Return>
        </Method>
        <Method Name="get_GetEnumerator" CallingConvention="HasThis">
            <MethodFlags>public newslot strict specialname abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>2</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Parameter Name="x">
                <ParamFlags>[opt] default</ParamFlags>
                <Type>Integer</Type>
                <Default>0</Default>
            </Parameter>
            <Return>
                <Type>System.Collections.IEnumerator</Type>
            </Return>
        </Method>
        <Property Name="GetEnumerator">
            <PropertyFlags></PropertyFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>-4</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Get>Function ComClassTest._ComClassTest.get_GetEnumerator() As System.Collections.IEnumerator</Get>
        </Property>
        <Property Name="GetEnumerator">
            <PropertyFlags></PropertyFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>2</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Get>Function ComClassTest._ComClassTest.get_GetEnumerator([x As System.Int32 = 0]) As System.Collections.IEnumerator</Get>
        </Property>
    </Interface>
</Class>

            Dim verifier = CompileAndVerify(compilationDef,
                                            options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal),
                                            symbolValidator:=Sub(m As ModuleSymbol)
                                                                 Dim pe = DirectCast(m, PEModuleSymbol)
                                                                 AssertReflection(expected,
                                                                                  ReflectComClass(pe, "ComClassTest"))
                                                             End Sub)

        End Sub

        <Fact()>
        Public Sub DefaultProperty1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

<Microsoft.VisualBasic.ComClass()> <Reflection.DefaultMember("p1")>
Public Class ComClassTest
    Default ReadOnly Property P1(x As Integer) As Integer
        Get
            Return Nothing
        End Get
    End Property

    Event E1 As Action
End Class
    ]]></file>
</compilation>

            Dim expected =
<Class Name="ComClassTest">
    <TypeDefFlags>public auto ansi</TypeDefFlags>
    <Attributes>
        <System.Runtime.InteropServices.ClassInterfaceAttribute>
            <ctor>Sub System.Runtime.InteropServices.ClassInterfaceAttribute..ctor(classInterfaceType As System.Runtime.InteropServices.ClassInterfaceType)</ctor>
            <a>0</a>
        </System.Runtime.InteropServices.ClassInterfaceAttribute>
        <System.Runtime.InteropServices.ComSourceInterfacesAttribute>
            <ctor>Sub System.Runtime.InteropServices.ComSourceInterfacesAttribute..ctor(sourceInterfaces As System.String)</ctor>
            <a>ComClassTest+__ComClassTest</a>
        </System.Runtime.InteropServices.ComSourceInterfacesAttribute>
        <Microsoft.VisualBasic.ComClassAttribute>
            <ctor>Sub Microsoft.VisualBasic.ComClassAttribute..ctor()</ctor>
        </Microsoft.VisualBasic.ComClassAttribute>
        <System.Reflection.DefaultMemberAttribute>
            <ctor>Sub System.Reflection.DefaultMemberAttribute..ctor(memberName As System.String)</ctor>
            <a>p1</a>
        </System.Reflection.DefaultMemberAttribute>
    </Attributes>
    <Implements>ComClassTest._ComClassTest</Implements>
    <Method Name=".ctor" CallingConvention="HasThis">
        <MethodFlags>public specialname rtspecialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="get_P1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict specialname virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Function ComClassTest._ComClassTest.get_P1(x As System.Int32) As System.Int32</Implements>
        <Parameter Name="x">
            <ParamFlags></ParamFlags>
            <Type>Integer</Type>
        </Parameter>
        <Return>
            <Type>Integer</Type>
        </Return>
    </Method>
    <Method Name="add_E1" CallingConvention="HasThis">
        <MethodFlags>public specialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.CompilerServices.CompilerGeneratedAttribute>
                <ctor>Sub System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor()</ctor>
            </System.Runtime.CompilerServices.CompilerGeneratedAttribute>
        </Attributes>
        <Parameter Name="obj">
            <ParamFlags></ParamFlags>
            <Type>System.Action</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="remove_E1" CallingConvention="HasThis">
        <MethodFlags>public specialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.CompilerServices.CompilerGeneratedAttribute>
                <ctor>Sub System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor()</ctor>
            </System.Runtime.CompilerServices.CompilerGeneratedAttribute>
        </Attributes>
        <Parameter Name="obj">
            <ParamFlags></ParamFlags>
            <Type>System.Action</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Property Name="P1">
        <PropertyFlags></PropertyFlags>
        <Get>Function ComClassTest.get_P1(x As System.Int32) As System.Int32</Get>
    </Property>
    <Event Name="E1">
        <EventFlags></EventFlags>
        <Add>Sub ComClassTest.add_E1(obj As System.Action)</Add>
        <Remove>Sub ComClassTest.remove_E1(obj As System.Action)</Remove>
    </Event>
    <Interface Name="_ComClassTest">
        <TypeDefFlags>interface nested public abstract auto ansi</TypeDefFlags>
        <Attributes>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>True</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
            <System.Reflection.DefaultMemberAttribute>
                <ctor>Sub System.Reflection.DefaultMemberAttribute..ctor(memberName As System.String)</ctor>
                <a>P1</a>
            </System.Reflection.DefaultMemberAttribute>
        </Attributes>
        <Method Name="get_P1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict specialname abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>0</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Parameter Name="x">
                <ParamFlags></ParamFlags>
                <Type>Integer</Type>
            </Parameter>
            <Return>
                <Type>Integer</Type>
            </Return>
        </Method>
        <Property Name="P1">
            <PropertyFlags></PropertyFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>0</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Get>Function ComClassTest._ComClassTest.get_P1(x As System.Int32) As System.Int32</Get>
        </Property>
    </Interface>
    <Interface Name="__ComClassTest">
        <TypeDefFlags>interface nested public abstract auto ansi</TypeDefFlags>
        <Attributes>
            <System.Runtime.InteropServices.InterfaceTypeAttribute>
                <ctor>Sub System.Runtime.InteropServices.InterfaceTypeAttribute..ctor(interfaceType As System.Int16)</ctor>
                <a>2</a>
            </System.Runtime.InteropServices.InterfaceTypeAttribute>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>True</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
        </Attributes>
        <Method Name="E1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>1</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
    </Interface>
</Class>

            Dim verifier = CompileAndVerify(compilationDef,
                                            options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal),
                                            symbolValidator:=Sub(m As ModuleSymbol)
                                                                 Dim pe = DirectCast(m, PEModuleSymbol)
                                                                 AssertReflection(expected,
                                                                                  ReflectComClass(pe, "ComClassTest"))
                                                             End Sub).VerifyDiagnostics()

        End Sub

        <Fact()>
        Public Sub DispId18()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

<Microsoft.VisualBasic.ComClass()>
Public Class ComClassTest

    <DispId(0)>
    ReadOnly Property P1(x As Integer) As Integer
        Get
            Return Nothing
        End Get
    End Property

    <DispId(-1)>
    Sub M1()
    End Sub

    <DispId(-2)>
    Event E1 As Action
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            AssertTheseDeclarationDiagnostics(compilation,
<expected>
BC32505: 'System.Runtime.InteropServices.DispIdAttribute' cannot be applied to 'P1' because 'Microsoft.VisualBasic.ComClassAttribute' reserves zero for the default property.
    ReadOnly Property P1(x As Integer) As Integer
                      ~~
BC32506: 'System.Runtime.InteropServices.DispIdAttribute' cannot be applied to 'M1' because 'Microsoft.VisualBasic.ComClassAttribute' reserves values less than zero.
    Sub M1()
        ~~
BC32506: 'System.Runtime.InteropServices.DispIdAttribute' cannot be applied to 'E1' because 'Microsoft.VisualBasic.ComClassAttribute' reserves values less than zero.
    Event E1 As Action
          ~~
</expected>)

        End Sub

        <Fact()>
        Public Sub DispId19()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

<Microsoft.VisualBasic.ComClass()>
Public Class ComClassTest

    <DispId(0)>
    Sub M1()
    End Sub

    <DispId(0)>
    Event E1 As Action
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            AssertTheseDeclarationDiagnostics(compilation,
<expected>
BC32505: 'System.Runtime.InteropServices.DispIdAttribute' cannot be applied to 'M1' because 'Microsoft.VisualBasic.ComClassAttribute' reserves zero for the default property.
    Sub M1()
        ~~
BC32505: 'System.Runtime.InteropServices.DispIdAttribute' cannot be applied to 'E1' because 'Microsoft.VisualBasic.ComClassAttribute' reserves zero for the default property.
    Event E1 As Action
          ~~
</expected>)

        End Sub

        <Fact()>
        Public Sub DefaultProperty2()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices

<Microsoft.VisualBasic.ComClass()>
Public Class ComClassTest
    <DispId(0)>
    Default ReadOnly Property P1(x As Integer) As Integer
        Get
            Return Nothing
        End Get
    End Property
End Class
    ]]></file>
</compilation>

            Dim expected =
<Class Name="ComClassTest">
    <TypeDefFlags>public auto ansi</TypeDefFlags>
    <Attributes>
        <System.Reflection.DefaultMemberAttribute>
            <ctor>Sub System.Reflection.DefaultMemberAttribute..ctor(memberName As System.String)</ctor>
            <a>P1</a>
        </System.Reflection.DefaultMemberAttribute>
        <System.Runtime.InteropServices.ClassInterfaceAttribute>
            <ctor>Sub System.Runtime.InteropServices.ClassInterfaceAttribute..ctor(classInterfaceType As System.Runtime.InteropServices.ClassInterfaceType)</ctor>
            <a>0</a>
        </System.Runtime.InteropServices.ClassInterfaceAttribute>
        <Microsoft.VisualBasic.ComClassAttribute>
            <ctor>Sub Microsoft.VisualBasic.ComClassAttribute..ctor()</ctor>
        </Microsoft.VisualBasic.ComClassAttribute>
    </Attributes>
    <Implements>ComClassTest._ComClassTest</Implements>
    <Method Name=".ctor" CallingConvention="HasThis">
        <MethodFlags>public specialname rtspecialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="get_P1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict specialname virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Function ComClassTest._ComClassTest.get_P1(x As System.Int32) As System.Int32</Implements>
        <Parameter Name="x">
            <ParamFlags></ParamFlags>
            <Type>Integer</Type>
        </Parameter>
        <Return>
            <Type>Integer</Type>
        </Return>
    </Method>
    <Property Name="P1">
        <PropertyFlags></PropertyFlags>
        <Attributes>
            <System.Runtime.InteropServices.DispIdAttribute>
                <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                <a>0</a>
            </System.Runtime.InteropServices.DispIdAttribute>
        </Attributes>
        <Get>Function ComClassTest.get_P1(x As System.Int32) As System.Int32</Get>
    </Property>
    <Interface Name="_ComClassTest">
        <TypeDefFlags>interface nested public abstract auto ansi</TypeDefFlags>
        <Attributes>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>True</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
            <System.Reflection.DefaultMemberAttribute>
                <ctor>Sub System.Reflection.DefaultMemberAttribute..ctor(memberName As System.String)</ctor>
                <a>P1</a>
            </System.Reflection.DefaultMemberAttribute>
        </Attributes>
        <Method Name="get_P1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict specialname abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>0</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Parameter Name="x">
                <ParamFlags></ParamFlags>
                <Type>Integer</Type>
            </Parameter>
            <Return>
                <Type>Integer</Type>
            </Return>
        </Method>
        <Property Name="P1">
            <PropertyFlags></PropertyFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>0</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Get>Function ComClassTest._ComClassTest.get_P1(x As System.Int32) As System.Int32</Get>
        </Property>
    </Interface>
</Class>

            Dim verifier = CompileAndVerify(compilationDef,
                                            options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal),
                                            symbolValidator:=Sub(m As ModuleSymbol)
                                                                 Dim pe = DirectCast(m, PEModuleSymbol)
                                                                 AssertReflection(expected,
                                                                                  ReflectComClass(pe, "ComClassTest"))
                                                             End Sub).VerifyDiagnostics()

        End Sub

        <Fact()>
        Public Sub Serializable_and_SpecialName()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

<Microsoft.VisualBasic.ComClass()> <Serializable()> <System.Runtime.CompilerServices.SpecialName()>
Public Class ComClassTest
    <System.Runtime.CompilerServices.SpecialName()>
    Sub M1()
    End Sub

    <System.Runtime.CompilerServices.SpecialName()>
    Event E1 As Action

    <System.Runtime.CompilerServices.SpecialName()>
    Property P1 As Integer
        Get
            Return 0
        End Get
        Set(value As Integer)
        End Set
    End Property

    Property P2 As Integer
        <System.Runtime.CompilerServices.SpecialName()>
        Get
            Return 0
        End Get
        <System.Runtime.CompilerServices.SpecialName()>
        Set(value As Integer)
        End Set
    End Property
End Class
    ]]></file>
</compilation>

            Dim expected =
<Class Name="ComClassTest">
    <TypeDefFlags>public auto ansi serializable specialname</TypeDefFlags>
    <Attributes>
        <System.Runtime.InteropServices.ClassInterfaceAttribute>
            <ctor>Sub System.Runtime.InteropServices.ClassInterfaceAttribute..ctor(classInterfaceType As System.Runtime.InteropServices.ClassInterfaceType)</ctor>
            <a>0</a>
        </System.Runtime.InteropServices.ClassInterfaceAttribute>
        <System.Runtime.InteropServices.ComSourceInterfacesAttribute>
            <ctor>Sub System.Runtime.InteropServices.ComSourceInterfacesAttribute..ctor(sourceInterfaces As System.String)</ctor>
            <a>ComClassTest+__ComClassTest</a>
        </System.Runtime.InteropServices.ComSourceInterfacesAttribute>
        <Microsoft.VisualBasic.ComClassAttribute>
            <ctor>Sub Microsoft.VisualBasic.ComClassAttribute..ctor()</ctor>
        </Microsoft.VisualBasic.ComClassAttribute>
    </Attributes>
    <Implements>ComClassTest._ComClassTest</Implements>
    <Method Name=".ctor" CallingConvention="HasThis">
        <MethodFlags>public specialname rtspecialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="M1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict specialname virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest._ComClassTest.M1()</Implements>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="add_E1" CallingConvention="HasThis">
        <MethodFlags>public specialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.CompilerServices.CompilerGeneratedAttribute>
                <ctor>Sub System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor()</ctor>
            </System.Runtime.CompilerServices.CompilerGeneratedAttribute>
        </Attributes>
        <Parameter Name="obj">
            <ParamFlags></ParamFlags>
            <Type>System.Action</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="remove_E1" CallingConvention="HasThis">
        <MethodFlags>public specialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Attributes>
            <System.Runtime.CompilerServices.CompilerGeneratedAttribute>
                <ctor>Sub System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor()</ctor>
            </System.Runtime.CompilerServices.CompilerGeneratedAttribute>
        </Attributes>
        <Parameter Name="obj">
            <ParamFlags></ParamFlags>
            <Type>System.Action</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="get_P1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict specialname virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Function ComClassTest._ComClassTest.get_P1() As System.Int32</Implements>
        <Return>
            <Type>Integer</Type>
        </Return>
    </Method>
    <Method Name="set_P1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict specialname virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest._ComClassTest.set_P1(value As System.Int32)</Implements>
        <Parameter Name="value">
            <ParamFlags></ParamFlags>
            <Type>Integer</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="get_P2" CallingConvention="HasThis">
        <MethodFlags>public newslot strict specialname virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Function ComClassTest._ComClassTest.get_P2() As System.Int32</Implements>
        <Return>
            <Type>Integer</Type>
        </Return>
    </Method>
    <Method Name="set_P2" CallingConvention="HasThis">
        <MethodFlags>public newslot strict specialname virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClassTest._ComClassTest.set_P2(value As System.Int32)</Implements>
        <Parameter Name="value">
            <ParamFlags></ParamFlags>
            <Type>Integer</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Property Name="P1">
        <PropertyFlags>specialname</PropertyFlags>
        <Get>Function ComClassTest.get_P1() As System.Int32</Get>
        <Set>Sub ComClassTest.set_P1(value As System.Int32)</Set>
    </Property>
    <Property Name="P2">
        <PropertyFlags></PropertyFlags>
        <Get>Function ComClassTest.get_P2() As System.Int32</Get>
        <Set>Sub ComClassTest.set_P2(value As System.Int32)</Set>
    </Property>
    <Event Name="E1">
        <EventFlags>specialname</EventFlags>
        <Add>Sub ComClassTest.add_E1(obj As System.Action)</Add>
        <Remove>Sub ComClassTest.remove_E1(obj As System.Action)</Remove>
    </Event>
    <Interface Name="_ComClassTest">
        <TypeDefFlags>interface nested public abstract auto ansi</TypeDefFlags>
        <Attributes>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>True</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
        </Attributes>
        <Method Name="M1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict specialname abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>1</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Method Name="get_P1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict specialname abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>2</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Integer</Type>
            </Return>
        </Method>
        <Method Name="set_P1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict specialname abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>2</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Parameter Name="value">
                <ParamFlags></ParamFlags>
                <Type>Integer</Type>
            </Parameter>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Method Name="get_P2" CallingConvention="HasThis">
            <MethodFlags>public newslot strict specialname abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>3</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Integer</Type>
            </Return>
        </Method>
        <Method Name="set_P2" CallingConvention="HasThis">
            <MethodFlags>public newslot strict specialname abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>3</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Parameter Name="value">
                <ParamFlags></ParamFlags>
                <Type>Integer</Type>
            </Parameter>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Property Name="P1">
            <PropertyFlags>specialname</PropertyFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>2</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Get>Function ComClassTest._ComClassTest.get_P1() As System.Int32</Get>
            <Set>Sub ComClassTest._ComClassTest.set_P1(value As System.Int32)</Set>
        </Property>
        <Property Name="P2">
            <PropertyFlags></PropertyFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>3</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Get>Function ComClassTest._ComClassTest.get_P2() As System.Int32</Get>
            <Set>Sub ComClassTest._ComClassTest.set_P2(value As System.Int32)</Set>
        </Property>
    </Interface>
    <Interface Name="__ComClassTest">
        <TypeDefFlags>interface nested public abstract auto ansi</TypeDefFlags>
        <Attributes>
            <System.Runtime.InteropServices.InterfaceTypeAttribute>
                <ctor>Sub System.Runtime.InteropServices.InterfaceTypeAttribute..ctor(interfaceType As System.Int16)</ctor>
                <a>2</a>
            </System.Runtime.InteropServices.InterfaceTypeAttribute>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>True</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
        </Attributes>
        <Method Name="E1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict specialname abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>1</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
    </Interface>
</Class>

            Dim verifier = CompileAndVerify(compilationDef,
                                            options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal),
                                            symbolValidator:=Sub(m As ModuleSymbol)
                                                                 Dim pe = DirectCast(m, PEModuleSymbol)
                                                                 AssertReflection(expected,
                                                                                  ReflectComClass(pe, "ComClassTest"))
                                                             End Sub).VerifyDiagnostics()

        End Sub

        <Fact(), WorkItem(531506, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531506")>
        Public Sub Bug18218()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices

<Assembly: GuidAttribute("5F025F24-FAEA-4C2F-9EF6-C89A8FC90101")>
<Assembly: ComVisible(True)>

    <Microsoft.VisualBasic.ComClass(ComClass1.ClassId, ComClass1.InterfaceId, ComClass1.EventsId)>
    Public Class ComClass1
        Implements I6
#Region "COM GUIDs"
        ' These  GUIDs provide the COM identity for this class 
        ' and its COM interfaces. If you change them, existing 
        ' clients will no longer be able to access the class.
        Public Const ClassId As String = "5D025F24-FAEA-4C2F-9EF6-C89A8FC9667F"
        Public Const InterfaceId As String = "5FDA4272-D6AD-4FA4-AD89-FAB8F0A04512"
        Public Const EventsId As String = "33241EB2-DFC5-4164-998E-A6577B0DA960"
#End Region
        Public Interface I6
        End Interface

        Public Property Scen1 As String
            Get
                Return Nothing
            End Get
            Set(ByVal Value As String)
            End Set
        End Property
    End Class
    ]]></file>
</compilation>

            Dim expected =
<Class Name="ComClass1">
    <TypeDefFlags>public auto ansi</TypeDefFlags>
    <Attributes>
        <System.Runtime.InteropServices.GuidAttribute>
            <ctor>Sub System.Runtime.InteropServices.GuidAttribute..ctor(guid As System.String)</ctor>
            <a>5D025F24-FAEA-4C2F-9EF6-C89A8FC9667F</a>
        </System.Runtime.InteropServices.GuidAttribute>
        <System.Runtime.InteropServices.ClassInterfaceAttribute>
            <ctor>Sub System.Runtime.InteropServices.ClassInterfaceAttribute..ctor(classInterfaceType As System.Runtime.InteropServices.ClassInterfaceType)</ctor>
            <a>0</a>
        </System.Runtime.InteropServices.ClassInterfaceAttribute>
        <Microsoft.VisualBasic.ComClassAttribute>
            <ctor>Sub Microsoft.VisualBasic.ComClassAttribute..ctor(_ClassID As System.String, _InterfaceID As System.String, _EventId As System.String)</ctor>
            <a>5D025F24-FAEA-4C2F-9EF6-C89A8FC9667F</a>
            <a>5FDA4272-D6AD-4FA4-AD89-FAB8F0A04512</a>
            <a>33241EB2-DFC5-4164-998E-A6577B0DA960</a>
        </Microsoft.VisualBasic.ComClassAttribute>
    </Attributes>
    <Implements>ComClass1._ComClass1</Implements>
    <Implements>ComClass1.I6</Implements>
    <Field Name="ClassId">
        <FieldFlags>public static literal default</FieldFlags>
        <Type>String</Type>
    </Field>
    <Field Name="InterfaceId">
        <FieldFlags>public static literal default</FieldFlags>
        <Type>String</Type>
    </Field>
    <Field Name="EventsId">
        <FieldFlags>public static literal default</FieldFlags>
        <Type>String</Type>
    </Field>
    <Method Name=".ctor" CallingConvention="HasThis">
        <MethodFlags>public specialname rtspecialname instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Method Name="get_Scen1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict specialname virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Function ComClass1._ComClass1.get_Scen1() As System.String</Implements>
        <Return>
            <Type>String</Type>
        </Return>
    </Method>
    <Method Name="set_Scen1" CallingConvention="HasThis">
        <MethodFlags>public newslot strict specialname virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Sub ComClass1._ComClass1.set_Scen1(Value As System.String)</Implements>
        <Parameter Name="Value">
            <ParamFlags></ParamFlags>
            <Type>String</Type>
        </Parameter>
        <Return>
            <Type>Void</Type>
        </Return>
    </Method>
    <Property Name="Scen1">
        <PropertyFlags></PropertyFlags>
        <Get>Function ComClass1.get_Scen1() As System.String</Get>
        <Set>Sub ComClass1.set_Scen1(Value As System.String)</Set>
    </Property>
    <Interface Name="I6">
        <TypeDefFlags>interface nested public abstract auto ansi</TypeDefFlags>
    </Interface>
    <Interface Name="_ComClass1">
        <TypeDefFlags>interface nested public abstract auto ansi</TypeDefFlags>
        <Attributes>
            <System.Runtime.InteropServices.GuidAttribute>
                <ctor>Sub System.Runtime.InteropServices.GuidAttribute..ctor(guid As System.String)</ctor>
                <a>5FDA4272-D6AD-4FA4-AD89-FAB8F0A04512</a>
            </System.Runtime.InteropServices.GuidAttribute>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>True</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
        </Attributes>
        <Method Name="get_Scen1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict specialname abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>1</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Return>
                <Type>String</Type>
            </Return>
        </Method>
        <Method Name="set_Scen1" CallingConvention="HasThis">
            <MethodFlags>public newslot strict specialname abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>1</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Parameter Name="Value">
                <ParamFlags></ParamFlags>
                <Type>String</Type>
            </Parameter>
            <Return>
                <Type>Void</Type>
            </Return>
        </Method>
        <Property Name="Scen1">
            <PropertyFlags></PropertyFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>1</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Get>Function ComClass1._ComClass1.get_Scen1() As System.String</Get>
            <Set>Sub ComClass1._ComClass1.set_Scen1(Value As System.String)</Set>
        </Property>
    </Interface>
</Class>

            Dim verifier = CompileAndVerify(compilationDef,
                                            options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal),
                                            symbolValidator:=Sub(m As ModuleSymbol)
                                                                 Dim pe = DirectCast(m, PEModuleSymbol)
                                                                 AssertReflection(expected,
                                                                                  ReflectComClass(pe, "ComClass1"))
                                                             End Sub).VerifyDiagnostics()

        End Sub

        <Fact, WorkItem(664574, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/664574")>
        Public Sub Bug664574()
            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices
 
<Microsoft.VisualBasic.ComClass(ComClass1.ClassId, ComClass1.InterfaceId, ComClass1.EventsId)>
Public Class ComClass1
 
#Region "COM GUIDs"
    ' These  GUIDs provide the COM identity for this class 
    ' and its COM interfaces. If you change them, existing 
    ' clients will no longer be able to access the class.
    Public Const ClassId As String = "C1F1CEC8-2BDD-4AFC-8E86-FDC8DBEE951B"
    Public Const InterfaceId As String = "E4174EC8-7EDD-4672-90BA-3D1CFFF76C14"
    Public Const EventsId As String = "8F12C15B-4CA9-450C-9C85-37E9B74164B8"
#End Region
    Public Function dfoo(<MarshalAs(UnmanagedType.Currency)> ByVal x As Decimal) As <MarshalAs(UnmanagedType.Currency)> Decimal
        Return x + 1.1D
    End Function
End Class
    ]]></file>
</compilation>

            Dim expected =
<Class Name="ComClass1">
    <TypeDefFlags>public auto ansi</TypeDefFlags>
    <Attributes>
        <System.Runtime.InteropServices.GuidAttribute>
            <ctor>Sub System.Runtime.InteropServices.GuidAttribute..ctor(guid As System.String)</ctor>
            <a>C1F1CEC8-2BDD-4AFC-8E86-FDC8DBEE951B</a>
        </System.Runtime.InteropServices.GuidAttribute>
        <System.Runtime.InteropServices.ClassInterfaceAttribute>
            <ctor>Sub System.Runtime.InteropServices.ClassInterfaceAttribute..ctor(classInterfaceType As System.Runtime.InteropServices.ClassInterfaceType)</ctor>
            <a>0</a>
        </System.Runtime.InteropServices.ClassInterfaceAttribute>
        <Microsoft.VisualBasic.ComClassAttribute>
            <ctor>Sub Microsoft.VisualBasic.ComClassAttribute..ctor(_ClassID As System.String, _InterfaceID As System.String, _EventId As System.String)</ctor>
            <a>C1F1CEC8-2BDD-4AFC-8E86-FDC8DBEE951B</a>
            <a>E4174EC8-7EDD-4672-90BA-3D1CFFF76C14</a>
            <a>8F12C15B-4CA9-450C-9C85-37E9B74164B8</a>
        </Microsoft.VisualBasic.ComClassAttribute>
    </Attributes>
    <Implements>ComClass1._ComClass1</Implements>
    <Method Name="dfoo" CallingConvention="HasThis">
        <MethodFlags>public newslot strict virtual final instance</MethodFlags>
        <MethodImplFlags>cil managed</MethodImplFlags>
        <Implements>Function ComClass1._ComClass1.dfoo(x As System.Decimal) As System.Decimal</Implements>
        <Parameter Name="x">
            <ParamFlags>marshal</ParamFlags>
            <Type>Decimal</Type>
        </Parameter>
        <Return>
            <Type>Decimal</Type>
            <ParamFlags>marshal</ParamFlags>
        </Return>
    </Method>
    <Interface Name="_ComClass1">
        <TypeDefFlags>interface nested public abstract auto ansi</TypeDefFlags>
        <Attributes>
            <System.Runtime.InteropServices.GuidAttribute>
                <ctor>Sub System.Runtime.InteropServices.GuidAttribute..ctor(guid As System.String)</ctor>
                <a>E4174EC8-7EDD-4672-90BA-3D1CFFF76C14</a>
            </System.Runtime.InteropServices.GuidAttribute>
            <System.Runtime.InteropServices.ComVisibleAttribute>
                <ctor>Sub System.Runtime.InteropServices.ComVisibleAttribute..ctor(visibility As System.Boolean)</ctor>
                <a>True</a>
            </System.Runtime.InteropServices.ComVisibleAttribute>
        </Attributes>
        <Method Name="dfoo" CallingConvention="HasThis">
            <MethodFlags>public newslot strict abstract virtual instance</MethodFlags>
            <MethodImplFlags>cil managed</MethodImplFlags>
            <Attributes>
                <System.Runtime.InteropServices.DispIdAttribute>
                    <ctor>Sub System.Runtime.InteropServices.DispIdAttribute..ctor(dispId As System.Int32)</ctor>
                    <a>1</a>
                </System.Runtime.InteropServices.DispIdAttribute>
            </Attributes>
            <Parameter Name="x">
                <ParamFlags>marshal</ParamFlags>
                <Type>Decimal</Type>
            </Parameter>
            <Return>
                <Type>Decimal</Type>
                <ParamFlags>marshal</ParamFlags>
            </Return>
        </Method>
    </Interface>
</Class>
            ' Strip TODO comments from the base-line.
            For Each d In expected.DescendantNodes.ToArray()
                Dim comment = TryCast(d, XComment)
                If comment IsNot Nothing Then
                    comment.Remove()
                End If
            Next

            Dim verifier = CompileAndVerify(compilationDef,
                                            options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal),
                                            symbolValidator:=Sub(m As ModuleSymbol)
                                                                 Dim pe = DirectCast(m, PEModuleSymbol)
                                                                 AssertReflection(expected,
                                                                                  ReflectComClass(pe, "ComClass1",
                                                                                                  Function(s)
                                                                                                      Return s.Kind = SymbolKind.NamedType OrElse s.Name = "dfoo"
                                                                                                  End Function))
                                                             End Sub).VerifyDiagnostics()
        End Sub

        <Fact, WorkItem(664583, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/664583")>
        Public Sub Bug664583()
            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
<Microsoft.VisualBasic.ComClass(ComClass1.ClassId, ComClass1.InterfaceId, ComClass1.EventsId)>
Public Class ComClass1 
#Region "COM GUIDs"
    ' These  GUIDs provide the COM identity for this class 
    ' and its COM interfaces. If you change them, existing 
    ' clients will no longer be able to access the class.
    Public Const ClassId As String = "C1F1CEC8-2BDD-4AFC-8E86-FDC8DBEE951B"
    Public Const InterfaceId As String = "E4174EC8-7EDD-4672-90BA-3D1CFFF76C14"
    Public Const EventsId As String = "8F12C15B-4CA9-450C-9C85-37E9B74164B8"
#End Region

    Public Readonly Property Foo As Integer
        Get
            Return 0
        End Get
    End Property

    Structure Struct1
        Public member1 As Integer
        Structure Struct2
            Public member12 As Integer
        End Structure
    End Structure
    Structure struct2
        Public member2 As Integer
    End Structure
End Class
    ]]></file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef,
                                            options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal),
                                            symbolValidator:=Sub(m As ModuleSymbol)
                                                                 Dim ComClass1_Struct1 = DirectCast(m.ContainingAssembly.GetTypeByMetadataName("ComClass1+Struct1"), PENamedTypeSymbol)
                                                                 Dim ComClass1_Struct1_Struct2 = DirectCast(m.ContainingAssembly.GetTypeByMetadataName("ComClass1+Struct1+Struct2"), PENamedTypeSymbol)
                                                                 Dim ComClass1_struct2 = DirectCast(m.ContainingAssembly.GetTypeByMetadataName("ComClass1+struct2"), PENamedTypeSymbol)

                                                                 Assert.True(MetadataTokens.GetRowNumber(ComClass1_Struct1.Handle) < MetadataTokens.GetRowNumber(ComClass1_Struct1_Struct2.Handle))
                                                                 Assert.True(MetadataTokens.GetRowNumber(ComClass1_Struct1.Handle) < MetadataTokens.GetRowNumber(ComClass1_struct2.Handle))
                                                                 Assert.True(MetadataTokens.GetRowNumber(ComClass1_struct2.Handle) < MetadataTokens.GetRowNumber(ComClass1_Struct1_Struct2.Handle))
                                                             End Sub).VerifyDiagnostics()
        End Sub

        <Fact, WorkItem(700050, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700050")>
        Public Sub Bug700050()
            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1
    Sub Main()

    End Sub
End Module

<Microsoft.VisualBasic.ComClass(ComClass1.ClassId, ComClass1.InterfaceId, ComClass1.EventsId)>
Public Class ComClass1
    Public Const ClassId As String = ""
    Public Const InterfaceId As String = ""
    Public Const EventsId As String = ""
    Public Sub New()
    End Sub
    Public Sub Foo()
    End Sub
    Public Property oBrowser As Object ' cannot be exposed to COM as a property 'Let'. You will not be able to assign non-object values (such as numbers or strings) to this property from Visual Basic 6.0 using a 'Let' statement. 
End Class
    ]]></file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef,
                                            options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal),
                                            symbolValidator:=Sub(m As ModuleSymbol)
                                                                 Dim _ComClass1 = DirectCast(m.ContainingAssembly.GetTypeByMetadataName("ComClass1+_ComClass1"), PENamedTypeSymbol)
                                                                 Assert.Equal(0, _ComClass1.GetMembers("oBrowser").Length)
                                                             End Sub).VerifyDiagnostics()
        End Sub

    End Class
End Namespace
