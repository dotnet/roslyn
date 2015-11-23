' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp
    Public Class CodeAttributeTests
        Inherits AbstractCodeAttributeTests

#Region "GetStartPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetStartPoint1()
            Dim code =
<Code>using System;

[$$Serializable]
class C { }
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     ThrowsCOMException(E_FAIL)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     ThrowsCOMException(E_FAIL)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=2, absoluteOffset:=17, lineLength:=14)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=2, absoluteOffset:=17, lineLength:=14)))

        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetStartPoint2()
            Dim code =
<Code>using System;

[$$CLSCompliant(true)]
class C { }
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     ThrowsCOMException(E_FAIL)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     ThrowsCOMException(E_FAIL)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=2, absoluteOffset:=17, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=2, absoluteOffset:=17, lineLength:=20)))

        End Sub


#End Region

#Region "GetEndPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetEndPoint1()
            Dim code =
<Code>using System;

[$$Serializable]
class C { }
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     ThrowsCOMException(E_FAIL)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     ThrowsCOMException(E_FAIL)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=14, absoluteOffset:=29, lineLength:=14)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=14, absoluteOffset:=29, lineLength:=14)))

        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetEndPoint2()
            Dim code =
<Code>using System;

[$$CLSCompliant(true)]
class C { }
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     ThrowsCOMException(E_FAIL)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     ThrowsCOMException(E_FAIL)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=14, absoluteOffset:=29, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=20, absoluteOffset:=35, lineLength:=20)))

        End Sub

#End Region

#Region "AttributeArgument GetStartPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetAttributeArgumentStartPoint1()
            Dim code =
<Code>
using System;

[assembly: $$Foo(0, y: 42, Z = 42)]

class FooAttribute : Attribute
{
    public FooAttribute(int x, int y = 0) { }

    public int Z { get; set; }
}
</Code>

            TestAttributeArgumentStartPoint(code, 1,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     ThrowsCOMException(E_FAIL)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     ThrowsCOMException(E_FAIL)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     ThrowsCOMException(E_FAIL)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=16, absoluteOffset:=31, lineLength:=33)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetAttributeArgumentStartPoint2()
            Dim code =
<Code>
using System;

[assembly: $$Foo(0, y: 42, Z = 42)]

class FooAttribute : Attribute
{
    public FooAttribute(int x, int y = 0) { }

    public int Z { get; set; }
}
</Code>

            TestAttributeArgumentStartPoint(code, 2,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     ThrowsCOMException(E_FAIL)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     ThrowsCOMException(E_FAIL)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     ThrowsCOMException(E_FAIL)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=19, absoluteOffset:=34, lineLength:=33)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetAttributeArgumentStartPoint3()
            Dim code =
<Code>
using System;

[assembly: $$Foo(0, y: 42, Z = 42)]

class FooAttribute : Attribute
{
    public FooAttribute(int x, int y = 0) { }

    public int Z { get; set; }
}
</Code>

            TestAttributeArgumentStartPoint(code, 3,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     ThrowsCOMException(E_FAIL)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     ThrowsCOMException(E_FAIL)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     ThrowsCOMException(E_FAIL)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=26, absoluteOffset:=41, lineLength:=33)))
        End Sub

#End Region

#Region "AttributeArgument GetEndPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetAttributeArgumentEndPoint1()
            Dim code =
<Code>
using System;

[assembly: $$Foo(0, y: 42, Z = 42)]

class FooAttribute : Attribute
{
    public FooAttribute(int x, int y = 0) { }

    public int Z { get; set; }
}
End Class
</Code>

            TestAttributeArgumentEndPoint(code, 1,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     ThrowsCOMException(E_FAIL)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     ThrowsCOMException(E_FAIL)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     ThrowsCOMException(E_FAIL)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=17, absoluteOffset:=32, lineLength:=33)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetAttributeArgumentEndPoint2()
            Dim code =
<Code>
using System;

[assembly: $$Foo(0, y: 42, Z = 42)]

class FooAttribute : Attribute
{
    public FooAttribute(int x, int y = 0) { }

    public int Z { get; set; }
}
End Class
</Code>

            TestAttributeArgumentEndPoint(code, 2,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     ThrowsCOMException(E_FAIL)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     ThrowsCOMException(E_FAIL)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     ThrowsCOMException(E_FAIL)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=24, absoluteOffset:=39, lineLength:=33)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetAttributeArgumentEndPoint3()
            Dim code =
<Code>
using System;

[assembly: $$Foo(0, y: 42, Z = 42)]

class FooAttribute : Attribute
{
    public FooAttribute(int x, int y = 0) { }

    public int Z { get; set; }
}
End Class
</Code>

            TestAttributeArgumentEndPoint(code, 3,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     ThrowsCOMException(E_FAIL)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     ThrowsCOMException(E_FAIL)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     ThrowsCOMException(E_FAIL)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=32, absoluteOffset:=47, lineLength:=33)))
        End Sub

#End Region

#Region "FullName tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetFullName1()
            Dim code =
<Code>
using System;

[$$Serializable]
class C { }
</Code>

            TestFullName(code, "System.SerializableAttribute")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetFullName2()
            Dim code =
<Code>
[$$System.Serializable]
class C { }
</Code>

            TestFullName(code, "System.SerializableAttribute")
        End Sub

#End Region

#Region "Parent tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetParent1()
            Dim code =
<Code>
using System;

[$$Serializable]
class C { }
</Code>

            TestParent(code, IsElement("C", kind:=EnvDTE.vsCMElement.vsCMElementClass))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetParent2()
            Dim code =
<Code>
using System;

[Serializable, $$CLSCompliant(false)]
class C { }
</Code>

            TestParent(code, IsElement("C", kind:=EnvDTE.vsCMElement.vsCMElementClass))
        End Sub
#End Region

#Region "Attribute argument tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetArguments1()
            Dim code =
<Code>
using System;

[$$Serializable]
class C { }
</Code>

            TestAttributeArguments(code, NoElements)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetArguments2()
            Dim code =
<Code>
using System;

[$$Serializable()]
class C { }
</Code>

            TestAttributeArguments(code, NoElements)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetArguments3()
            Dim code =
<Code>
using System;

[$$CLSCompliant(true)]
class C { }
</Code>

            TestAttributeArguments(code, IsAttributeArgument(value:="true"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetArguments4()
            Dim code =
<Code>
using System;

[$$AttributeUsage(AttributeTargets.All, AllowMultiple=false)]
class CAttribute : Attribute { }
</Code>

            TestAttributeArguments(code, IsAttributeArgument(value:="AttributeTargets.All"), IsAttributeArgument(name:="AllowMultiple", value:="false"))

        End Sub
#End Region

#Region "Target tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetTarget1()
            Dim code =
<Code>
using System;

[type:CLSCompliant$$(false)]
class C { }
</Code>

            TestTarget(code, "type")
        End Sub
#End Region

#Region "Value tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetValue1()
            Dim code =
<Code>
using System;

[$$Serializable]
class C { }
</Code>

            TestValue(code, "")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetValue2()
            Dim code =
<Code>
using System;

[$$Serializable()]
class C { }
</Code>

            TestValue(code, "")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetValue3()
            Dim code =
<Code>
using System;

[$$CLSCompliant(false)]
class C { }
</Code>

            TestValue(code, "false")

        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetValue4()
            Dim code =
<Code>
using System;

[$$AttributeUsage(AttributeTargets.All, AllowMultiple=false)]
class CAttribute : Attribute { }
</Code>

            TestValue(code, "AttributeTargets.All, AllowMultiple=false")
        End Sub
#End Region

#Region "AddAttributeArgument tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttributeArgument1()
            Dim code =
<Code>
using System;

[$$CLSCompliant]
class C { }
</Code>

            Dim expectedCode =
<Code>
using System;

[CLSCompliant(true)]
class C { }
</Code>

            TestAddAttributeArgument(code, expectedCode, New AttributeArgumentData With {.Value = "true"})

        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttributeArgument2()
            Dim code =
<Code>
using System;

[$$CLSCompliant()]
class C { }
</Code>

            Dim expectedCode =
<Code>
using System;

[CLSCompliant(true)]
class C { }
</Code>

            TestAddAttributeArgument(code, expectedCode, New AttributeArgumentData With {.Value = "true"})

        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttributeArgument3()
            Dim code =
<Code>
using System;

[$$AttributeUsage(AttributeTargets.All)]
class CAttribute : Attribute { }
</Code>

            Dim expectedCode =
<Code>
using System;

[AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
class CAttribute : Attribute { }
</Code>

            TestAddAttributeArgument(code, expectedCode, New AttributeArgumentData With {.Name = "AllowMultiple", .Value = "false", .Position = 1})

        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttributeArgumentStress()
            Dim code =
<Code>
[$$A]
class C
{
}
</Code>

            TestElement(code,
                Sub(codeAttribute)
                    For i = 1 To 100
                        Dim value = i.ToString()
                        Dim codeAttributeArgument = codeAttribute.AddArgument(value, Position:=1)
                    Next
                End Sub)
        End Sub

#End Region

#Region "Delete tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Delete1()
            Dim code =
<Code>
[$$Foo]
class C
{
}
</Code>

            Dim expected =
<Code>
class C
{
}
</Code>

            TestDelete(code, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Delete2()
            Dim code =
<Code>
[$$Foo, Bar]
class C { }
</Code>

            Dim expected =
<Code>
[Bar]
class C { }
</Code>

            TestDelete(code, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Delete3()
            Dim code =
<Code>
[Foo]
[$$Bar]
class C { }
</Code>

            Dim expected =
<Code>
[Foo]
class C { }
</Code>

            TestDelete(code, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Delete4()
            Dim code =
<Code>
[assembly: $$Foo]
</Code>

            Dim expected =
<Code>
</Code>

            TestDelete(code, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Delete5()
            Dim code =
<Code>
[assembly: $$Foo, Bar]
</Code>

            Dim expected =
<Code>
[assembly: Bar]
</Code>

            TestDelete(code, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Delete6()
            Dim code =
<Code>
[assembly: Foo]
[assembly: $$Bar]
</Code>

            Dim expected =
<Code>
[assembly: Foo]
</Code>

            TestDelete(code, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Delete7()
            Dim code =
<Code><![CDATA[
/// <summary>
/// Doc comment.
/// </summary>
[$$Foo]
class C { }
]]></Code>

            Dim expected =
<Code><![CDATA[
/// <summary>
/// Doc comment.
/// </summary>
class C { }
]]></Code>

            TestDelete(code, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Delete8()
            Dim code =
<Code><![CDATA[
[$$Foo] // Comment comment comment
class C { }
]]></Code>

            Dim expected =
<Code><![CDATA[
class C { }
]]></Code>

            TestDelete(code, expected)
        End Sub

#End Region

#Region "Delete attribute argument tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub DeleteAttributeArgument1()
            Dim code =
<Code>
[$$System.CLSCompliant(true)]
class C { }
</Code>

            Dim expected =
<Code>
[System.CLSCompliant()]
class C { }
</Code>

            TestDeleteAttributeArgument(code, expected, 1)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub DeleteAttributeArgument2()
            Dim code =
<Code>
[$$AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
class CAttribute : Attribute { }
</Code>

            Dim expected =
<Code>
[AttributeUsage(AllowMultiple = false)]
class CAttribute : Attribute { }
</Code>

            TestDeleteAttributeArgument(code, expected, 1)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub DeleteAttributeArgument3()
            Dim code =
<Code>
[$$AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
class CAttribute : Attribute { }
</Code>

            Dim expected =
<Code>
[AttributeUsage(AttributeTargets.All)]
class CAttribute : Attribute { }
</Code>

            TestDeleteAttributeArgument(code, expected, 2)
        End Sub

#End Region

#Region "Set Name tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetName1()
            Dim code =
<Code>
[$$Foo]
class C { }
</Code>

            Dim expected =
<Code>
[Bar]
class C { }
</Code>

            TestSetName(code, expected, "Bar", NoThrow(Of String)())
        End Sub
#End Region

#Region "Set Target tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetTarget1()
            Dim code =
<Code>
using System;

[type: CLSCompliant$$(false)]
class C { }
</Code>

            Dim expected =
<Code>
using System;

[assembly: CLSCompliant(false)]
class C { }
</Code>

            TestSetTarget(code, expected, "assembly")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetTarget2()
            Dim code =
<Code>
using System;

[CLSCompliant$$(false)]
class C { }
</Code>

            Dim expected =
<Code>
using System;

[assembly: CLSCompliant(false)]
class C { }
</Code>

            TestSetTarget(code, expected, "assembly")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetTarget3()
            Dim code =
<Code>
using System;

[assembly: CLSCompliant$$(false)]
class C { }
</Code>

            Dim expected =
<Code>
using System;

[CLSCompliant(false)]
class C { }
</Code>

            TestSetTarget(code, expected, "")
        End Sub
#End Region

#Region "Set Value tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetValue1()
            Dim code =
<Code>
using System;

[type: CLSCompliant$$(false)]
class C { }
</Code>

            Dim expected =
<Code>
using System;

[type: CLSCompliant(true)]
class C { }
</Code>

            TestSetValue(code, expected, "true")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetValue2()
            Dim code =
<Code>
using System;

[type: CLSCompliant$$()]
class C { }
</Code>

            Dim expected =
<Code>
using System;

[type: CLSCompliant(true)]
class C { }
</Code>

            TestSetValue(code, expected, "true")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetValue3()
            Dim code =
<Code>
using System;

[type: CLSCompliant$$]
class C { }
</Code>

            Dim expected =
<Code>
using System;

[type: CLSCompliant(true)]
class C { }
</Code>

            TestSetValue(code, expected, "true")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetValue4()
            Dim code =
<Code>
using System;

[type: CLSCompliant$$(false)]
class C { }
</Code>

            Dim expected =
<Code>
using System;

[type: CLSCompliant()]
class C { }
</Code>

            TestSetValue(code, expected, "")
        End Sub
#End Region

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TypeDescriptor_GetProperties()
            Dim code =
<Code>
[$$System.CLSCompliant(true)]
class C
{
}
</Code>

            Dim expectedPropertyNames =
                {"DTE", "Collection", "Name", "FullName", "ProjectItem", "Kind", "IsCodeType",
                 "InfoLocation", "Children", "Language", "StartPoint", "EndPoint", "ExtenderNames",
                 "ExtenderCATID", "Parent", "Value", "Target", "Arguments"}

            TestPropertyDescriptors(code, expectedPropertyNames)
        End Sub

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.CSharp
            End Get
        End Property
    End Class
End Namespace
