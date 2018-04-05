' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp
    Public Class CodeAttributeTests
        Inherits AbstractCodeAttributeTests

#Region "GetStartPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint1()
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
        Public Sub TestGetStartPoint2()
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
        Public Sub TestGetEndPoint1()
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
        Public Sub TestGetEndPoint2()
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
        Public Sub TestGetAttributeArgumentStartPoint1()
            Dim code =
<Code>
using System;

[assembly: $$Goo(0, y: 42, Z = 42)]

class GooAttribute : Attribute
{
    public GooAttribute(int x, int y = 0) { }

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
        Public Sub TestGetAttributeArgumentStartPoint2()
            Dim code =
<Code>
using System;

[assembly: $$Goo(0, y: 42, Z = 42)]

class GooAttribute : Attribute
{
    public GooAttribute(int x, int y = 0) { }

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
        Public Sub TestGetAttributeArgumentStartPoint3()
            Dim code =
<Code>
using System;

[assembly: $$Goo(0, y: 42, Z = 42)]

class GooAttribute : Attribute
{
    public GooAttribute(int x, int y = 0) { }

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
        Public Sub TestGetAttributeArgumentEndPoint1()
            Dim code =
<Code>
using System;

[assembly: $$Goo(0, y: 42, Z = 42)]

class GooAttribute : Attribute
{
    public GooAttribute(int x, int y = 0) { }

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
        Public Sub TestGetAttributeArgumentEndPoint2()
            Dim code =
<Code>
using System;

[assembly: $$Goo(0, y: 42, Z = 42)]

class GooAttribute : Attribute
{
    public GooAttribute(int x, int y = 0) { }

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
        Public Sub TestGetAttributeArgumentEndPoint3()
            Dim code =
<Code>
using System;

[assembly: $$Goo(0, y: 42, Z = 42)]

class GooAttribute : Attribute
{
    public GooAttribute(int x, int y = 0) { }

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
        Public Sub TestGetFullName1()
            Dim code =
<Code>
using System;

[$$Serializable]
class C { }
</Code>

            TestFullName(code, "System.SerializableAttribute")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetFullName2()
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
        Public Sub TestGetParent1()
            Dim code =
<Code>
using System;

[$$Serializable]
class C { }
</Code>

            TestParent(code, IsElement("C", kind:=EnvDTE.vsCMElement.vsCMElementClass))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetParent2()
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
        Public Sub TestGetArguments1()
            Dim code =
<Code>
using System;

[$$Serializable]
class C { }
</Code>

            TestAttributeArguments(code, NoElements)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetArguments2()
            Dim code =
<Code>
using System;

[$$Serializable()]
class C { }
</Code>

            TestAttributeArguments(code, NoElements)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetArguments3()
            Dim code =
<Code>
using System;

[$$CLSCompliant(true)]
class C { }
</Code>

            TestAttributeArguments(code, IsAttributeArgument(value:="true"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetArguments4()
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
        Public Sub TestGetTarget1()
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
        Public Sub TestGetValue1()
            Dim code =
<Code>
using System;

[$$Serializable]
class C { }
</Code>

            TestValue(code, "")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetValue2()
            Dim code =
<Code>
using System;

[$$Serializable()]
class C { }
</Code>

            TestValue(code, "")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetValue3()
            Dim code =
<Code>
using System;

[$$CLSCompliant(false)]
class C { }
</Code>

            TestValue(code, "false")

        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetValue4()
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
        Public Async Function TestAddAttributeArgument1() As Task
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

            Await TestAddAttributeArgument(code, expectedCode, New AttributeArgumentData With {.Value = "true"})

        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttributeArgument2() As Task
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

            Await TestAddAttributeArgument(code, expectedCode, New AttributeArgumentData With {.Value = "true"})

        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttributeArgument3() As Task
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

            Await TestAddAttributeArgument(code, expectedCode, New AttributeArgumentData With {.Name = "AllowMultiple", .Value = "false", .Position = 1})

        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAddAttributeArgumentStress()
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
        Public Async Function TestDelete1() As Task
            Dim code =
<Code>
[$$Goo]
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

            Await TestDelete(code, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDelete2() As Task
            Dim code =
<Code>
[$$Goo, Bar]
class C { }
</Code>

            Dim expected =
<Code>
[Bar]
class C { }
</Code>

            Await TestDelete(code, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDelete3() As Task
            Dim code =
<Code>
[Goo]
[$$Bar]
class C { }
</Code>

            Dim expected =
<Code>
[Goo]
class C { }
</Code>

            Await TestDelete(code, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDelete4() As Task
            Dim code =
<Code>
[assembly: $$Goo]
</Code>

            Dim expected =
<Code>
</Code>

            Await TestDelete(code, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDelete5() As Task
            Dim code =
<Code>
[assembly: $$Goo, Bar]
</Code>

            Dim expected =
<Code>
[assembly: Bar]
</Code>

            Await TestDelete(code, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDelete6() As Task
            Dim code =
<Code>
[assembly: Goo]
[assembly: $$Bar]
</Code>

            Dim expected =
<Code>
[assembly: Goo]
</Code>

            Await TestDelete(code, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDelete7() As Task
            Dim code =
<Code><![CDATA[
/// <summary>
/// Doc comment.
/// </summary>
[$$Goo]
class C { }
]]></Code>

            Dim expected =
<Code><![CDATA[
/// <summary>
/// Doc comment.
/// </summary>
class C { }
]]></Code>

            Await TestDelete(code, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDelete8() As Task
            Dim code =
<Code><![CDATA[
[$$Goo] // Comment comment comment
class C { }
]]></Code>

            Dim expected =
<Code><![CDATA[
class C { }
]]></Code>

            Await TestDelete(code, expected)
        End Function

#End Region

#Region "Delete attribute argument tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDeleteAttributeArgument1() As Task
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

            Await TestDeleteAttributeArgument(code, expected, 1)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDeleteAttributeArgument2() As Task
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

            Await TestDeleteAttributeArgument(code, expected, 1)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDeleteAttributeArgument3() As Task
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

            Await TestDeleteAttributeArgument(code, expected, 2)
        End Function

#End Region

#Region "Set Name tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetName_NewName() As Task
            Dim code =
<Code>
[$$Goo]
class C { }
</Code>

            Dim expected =
<Code>
[Bar]
class C { }
</Code>

            Await TestSetName(code, expected, "Bar", NoThrow(Of String)())
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetName_SimpleNameToDottedName() As Task
            Dim code =
<Code>
[$$Goo]
class C { }
</Code>

            Dim expected =
<Code>
[Bar.Baz]
class C { }
</Code>

            Await TestSetName(code, expected, "Bar.Baz", NoThrow(Of String)())
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetName_DottedNameToSimpleName() As Task
            Dim code =
<Code>
[$$Goo.Bar]
class C { }
</Code>

            Dim expected =
<Code>
[Baz]
class C { }
</Code>

            Await TestSetName(code, expected, "Baz", NoThrow(Of String)())
        End Function
#End Region

#Region "Set Target tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetTarget1() As Task
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

            Await TestSetTarget(code, expected, "assembly")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetTarget2() As Task
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

            Await TestSetTarget(code, expected, "assembly")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetTarget3() As Task
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

            Await TestSetTarget(code, expected, "")
        End Function
#End Region

#Region "Set Value tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetValue1() As Task
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

            Await TestSetValue(code, expected, "true")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetValue2() As Task
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

            Await TestSetValue(code, expected, "true")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetValue3() As Task
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

            Await TestSetValue(code, expected, "true")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetValue4() As Task
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

            Await TestSetValue(code, expected, "")
        End Function
#End Region

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestTypeDescriptor_GetProperties()
            Dim code =
<Code>
[$$System.CLSCompliant(true)]
class C
{
}
</Code>

            TestPropertyDescriptors(Of EnvDTE80.CodeAttribute2)(code)
        End Sub

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.CSharp
            End Get
        End Property
    End Class
End Namespace
