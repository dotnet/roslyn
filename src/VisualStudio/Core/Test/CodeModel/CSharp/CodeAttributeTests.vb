' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp
    Public Class CodeAttributeTests
        Inherits AbstractCodeAttributeTests

#Region "GetStartPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint1() As Task
            Dim code =
<Code>using System;

[$$Serializable]
class C { }
</Code>

            Await TestGetStartPoint(code,
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

        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint2() As Task
            Dim code =
<Code>using System;

[$$CLSCompliant(true)]
class C { }
</Code>

            Await TestGetStartPoint(code,
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

        End Function


#End Region

#Region "GetEndPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint1() As Task
            Dim code =
<Code>using System;

[$$Serializable]
class C { }
</Code>

            Await TestGetEndPoint(code,
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

        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint2() As Task
            Dim code =
<Code>using System;

[$$CLSCompliant(true)]
class C { }
</Code>

            Await TestGetEndPoint(code,
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

        End Function

#End Region

#Region "AttributeArgument GetStartPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetAttributeArgumentStartPoint1() As Task
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

            Await TestAttributeArgumentStartPoint(code, 1,
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
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetAttributeArgumentStartPoint2() As Task
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

            Await TestAttributeArgumentStartPoint(code, 2,
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
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetAttributeArgumentStartPoint3() As Task
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

            Await TestAttributeArgumentStartPoint(code, 3,
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
        End Function

#End Region

#Region "AttributeArgument GetEndPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetAttributeArgumentEndPoint1() As Task
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

            Await TestAttributeArgumentEndPoint(code, 1,
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
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetAttributeArgumentEndPoint2() As Task
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

            Await TestAttributeArgumentEndPoint(code, 2,
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
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetAttributeArgumentEndPoint3() As Task
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

            Await TestAttributeArgumentEndPoint(code, 3,
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
        End Function

#End Region

#Region "FullName tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetFullName1() As Task
            Dim code =
<Code>
using System;

[$$Serializable]
class C { }
</Code>

            Await TestFullName(code, "System.SerializableAttribute")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetFullName2() As Task
            Dim code =
<Code>
[$$System.Serializable]
class C { }
</Code>

            Await TestFullName(code, "System.SerializableAttribute")
        End Function

#End Region

#Region "Parent tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetParent1() As Task
            Dim code =
<Code>
using System;

[$$Serializable]
class C { }
</Code>

            Await TestParent(code, IsElement("C", kind:=EnvDTE.vsCMElement.vsCMElementClass))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetParent2() As Task
            Dim code =
<Code>
using System;

[Serializable, $$CLSCompliant(false)]
class C { }
</Code>

            Await TestParent(code, IsElement("C", kind:=EnvDTE.vsCMElement.vsCMElementClass))
        End Function
#End Region

#Region "Attribute argument tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetArguments1() As Task
            Dim code =
<Code>
using System;

[$$Serializable]
class C { }
</Code>

            Await TestAttributeArguments(code, NoElements)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetArguments2() As Task
            Dim code =
<Code>
using System;

[$$Serializable()]
class C { }
</Code>

            Await TestAttributeArguments(code, NoElements)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetArguments3() As Task
            Dim code =
<Code>
using System;

[$$CLSCompliant(true)]
class C { }
</Code>

            Await TestAttributeArguments(code, IsAttributeArgument(value:="true"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetArguments4() As Task
            Dim code =
<Code>
using System;

[$$AttributeUsage(AttributeTargets.All, AllowMultiple=false)]
class CAttribute : Attribute { }
</Code>

            Await TestAttributeArguments(code, IsAttributeArgument(value:="AttributeTargets.All"), IsAttributeArgument(name:="AllowMultiple", value:="false"))

        End Function
#End Region

#Region "Target tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetTarget1() As Task
            Dim code =
<Code>
using System;

[type:CLSCompliant$$(false)]
class C { }
</Code>

            Await TestTarget(code, "type")
        End Function
#End Region

#Region "Value tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetValue1() As Task
            Dim code =
<Code>
using System;

[$$Serializable]
class C { }
</Code>

            Await TestValue(code, "")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetValue2() As Task
            Dim code =
<Code>
using System;

[$$Serializable()]
class C { }
</Code>

            Await TestValue(code, "")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetValue3() As Task
            Dim code =
<Code>
using System;

[$$CLSCompliant(false)]
class C { }
</Code>

            Await TestValue(code, "false")

        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetValue4() As Task
            Dim code =
<Code>
using System;

[$$AttributeUsage(AttributeTargets.All, AllowMultiple=false)]
class CAttribute : Attribute { }
</Code>

            Await TestValue(code, "AttributeTargets.All, AllowMultiple=false")
        End Function
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
        Public Async Function TestAddAttributeArgumentStress() As Task
            Dim code =
<Code>
[$$A]
class C
{
}
</Code>

            Await TestElement(code,
                Sub(codeAttribute)
                    For i = 1 To 100
                        Dim value = i.ToString()
                        Dim codeAttributeArgument = codeAttribute.AddArgument(value, Position:=1)
                    Next
                End Sub)
        End Function

#End Region

#Region "Delete tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDelete1() As Task
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

            Await TestDelete(code, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDelete2() As Task
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

            Await TestDelete(code, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDelete3() As Task
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

            Await TestDelete(code, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDelete4() As Task
            Dim code =
<Code>
[assembly: $$Foo]
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
[assembly: $$Foo, Bar]
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
[assembly: Foo]
[assembly: $$Bar]
</Code>

            Dim expected =
<Code>
[assembly: Foo]
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

            Await TestDelete(code, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDelete8() As Task
            Dim code =
<Code><![CDATA[
[$$Foo] // Comment comment comment
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
        Public Async Function TestSetName1() As Task
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

            Await TestSetName(code, expected, "Bar", NoThrow(Of String)())
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
        Public Async Function TestTypeDescriptor_GetProperties() As Task
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

            Await TestPropertyDescriptors(code, expectedPropertyNames)
        End Function

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.CSharp
            End Get
        End Property
    End Class
End Namespace
