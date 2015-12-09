' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp
    Public Class CodeVariableTests
        Inherits AbstractCodeVariableTests

#Region "GetStartPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint_Field() As Task
            Dim code =
<Code>
class C
{
    int $$foo;
}
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
                     TextPoint(line:=3, lineOffset:=9, absoluteOffset:=19, lineLength:=12)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=15, lineLength:=12)))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint_EnumMember() As Task
            Dim code =
<Code>
enum E
{
    $$Foo
}
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
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=14, lineLength:=7)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=14, lineLength:=7)))
        End Function

#End Region

#Region "GetEndPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint_Field() As Task
            Dim code =
<Code>
class C
{
    int $$foo;
}
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
                     TextPoint(line:=3, lineOffset:=12, absoluteOffset:=22, lineLength:=12)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=13, absoluteOffset:=23, lineLength:=12)))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint_EnumMember() As Task
            Dim code =
<Code>
enum E
{
    $$Foo
}
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
                     TextPoint(line:=3, lineOffset:=8, absoluteOffset:=17, lineLength:=7)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=8, absoluteOffset:=17, lineLength:=7)))
        End Function

#End Region

#Region "Access tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess1() As Task
            Dim code =
<Code>
class C
{
    int $$x;
}
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPrivate)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess2() As Task
            Dim code =
<Code>
class C
{
    private int $$x;
}
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPrivate)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess3() As Task
            Dim code =
<Code>
class C
{
    protected int $$x;
}
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProtected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess4() As Task
            Dim code =
<Code>
class C
{
    protected internal int $$x;
}
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess5() As Task
            Dim code =
<Code>
class C
{
    internal int $$x;
}
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess6() As Task
            Dim code =
<Code>
class C
{
    public int $$x;
}
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess7() As Task
            Dim code =
<Code>
enum E
{
    $$Foo
}
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

#End Region

#Region "Attributes tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAttributes1() As Task
            Dim code =
<Code>
class C
{
    int $$Foo;
}
</Code>

            Await TestAttributes(code, NoElements)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAttributes2() As Task
            Dim code =
<Code>
using System;

class C
{
    [Serializable]
    int $$Foo;
}
</Code>

            Await TestAttributes(code, IsElement("Serializable"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAttributes3() As Task
            Dim code =
<Code>using System;
    
class C
{
    [Serializable]
    [CLSCompliant(true)]
    int $$Foo;
}
</Code>

            Await TestAttributes(code, IsElement("Serializable"), IsElement("CLSCompliant"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAttributes4() As Task
            Dim code =
<Code>using System;

class C
{
    [Serializable, CLSCompliant(true)]
    int $$Foo;
}
</Code>

            Await TestAttributes(code, IsElement("Serializable"), IsElement("CLSCompliant"))
        End Function
#End Region

#Region "AddAttribute tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute1() As Task
            Dim code =
<Code>
using System;

class C
{
    int $$F;
}
</Code>

            Dim expected =
<Code>
using System;

class C
{
    [Serializable()]
    int F;
}
</Code>

            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute2() As Task
            Dim code =
<Code>
using System;

class C
{
    [Serializable]
    int $$F;
}
</Code>

            Dim expected =
<Code>
using System;

class C
{
    [Serializable]
    [CLSCompliant(true)]
    int F;
}
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "true", .Position = 1})
        End Function

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute_BelowDocComment() As Task
            Dim code =
<Code>
using System;

class C
{
    /// &lt;summary&gt;&lt;/summary&gt;
    int $$F;
}
</Code>

            Dim expected =
<Code>
using System;

class C
{
    /// &lt;summary&gt;&lt;/summary&gt;
    [CLSCompliant(true)]
    int F;
}
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "true"})
        End Function

#End Region

#Region "ConstKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestConstKind1() As Task
            Dim code =
<Code>
enum E
{
    $$Foo
}
</Code>

            Await TestConstKind(code, EnvDTE80.vsCMConstKind.vsCMConstKindConst)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestConstKind2() As Task
            Dim code =
<Code>
class C
{
    int $$x;
}
</Code>

            Await TestConstKind(code, EnvDTE80.vsCMConstKind.vsCMConstKindNone)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestConstKind3() As Task
            Dim code =
<Code>
class C
{
    const int $$x;
}
</Code>

            Await TestConstKind(code, EnvDTE80.vsCMConstKind.vsCMConstKindConst)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestConstKind4() As Task
            Dim code =
<Code>
class C
{
    readonly int $$x;
}
</Code>

            Await TestConstKind(code, EnvDTE80.vsCMConstKind.vsCMConstKindReadOnly)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestConstKind5() As Task
            Dim code =
<Code>
class C
{
    readonly const int $$x;
}
</Code>

            Await TestConstKind(code, EnvDTE80.vsCMConstKind.vsCMConstKindConst Or EnvDTE80.vsCMConstKind.vsCMConstKindReadOnly)
        End Function

#End Region

#Region "FullName tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestFullName1() As Task
            Dim code =
<Code>
enum E
{
    $$Foo = 1,
    Bar
}
</Code>

            Await TestFullName(code, "E.Foo")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestFullName2() As Task
            Dim code =
<Code>
enum E
{
    Foo = 1,
    $$Bar
}
</Code>

            Await TestFullName(code, "E.Bar")
        End Function

#End Region

#Region "InitExpression tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestInitExpression1() As Task
            Dim code =
<Code>
class C
{
    int $$i = 42;
}
</Code>

            Await TestInitExpression(code, "42")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestInitExpression2() As Task
            Dim code =
<Code>
class C
{
    const int $$i = 19 + 23;
}
</Code>

            Await TestInitExpression(code, "19 + 23")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestInitExpression3() As Task
            Dim code =
<Code>
enum E
{
    $$i = 19 + 23
}
</Code>

            Await TestInitExpression(code, "19 + 23")
        End Function

#End Region

#Region "IsConstant tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestIsConstant1() As Task
            Dim code =
<Code>
enum E
{
    $$Foo
}
</Code>

            Await TestIsConstant(code, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestIsConstant2() As Task
            Dim code =
<Code>
class C
{
    const int $$x = 0;
}
</Code>

            Await TestIsConstant(code, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestIsConstant3() As Task
            Dim code =
<Code>
class C
{
    readonly int $$x = 0;
}
</Code>

            Await TestIsConstant(code, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestIsConstant4() As Task
            Dim code =
<Code>
class C
{
    int $$x = 0;
}
</Code>

            Await TestIsConstant(code, False)
        End Function

#End Region

#Region "IsShared tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestIsShared1() As Task
            Dim code =
<Code>
class C
{
    int $$x;
}
</Code>

            Await TestIsShared(code, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestIsShared2() As Task
            Dim code =
<Code>
class C
{
    static int $$x;
}
</Code>

            Await TestIsShared(code, True)
        End Function

#End Region

#Region "Name tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName1() As Task
            Dim code =
<Code>
enum E
{
    $$Foo = 1,
    Bar
}
</Code>

            Await TestName(code, "Foo")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName2() As Task
            Dim code =
<Code>
enum E
{
    Foo = 1,
    $$Bar
}
</Code>

            Await TestName(code, "Bar")
        End Function

#End Region

#Region "Prototype tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_ClassName() As Task
            Dim code =
<Code>
namespace N
{
    class C
    {
        int x$$ = 0;
    }
}
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName, "C.x")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_FullName() As Task
            Dim code =
<Code>
namespace N
{
    class C
    {
        int x$$ = 0;
    }
}
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeFullname, "N.C.x")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_InitExpression1() As Task
            Dim code =
<Code>
namespace N
{
    class C
    {
        int x$$ = 0;
    }
}
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeInitExpression, "x = 0")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_InitExpression2() As Task
            Dim code =
<Code>
namespace N
{
    enum E
    {
        A$$ = 42
    }
}
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeInitExpression, "A = 42")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_InitExpressionAndType1() As Task
            Dim code =
<Code>
namespace N
{
    class C
    {
        int x$$ = 0;
    }
}
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeInitExpression Or EnvDTE.vsCMPrototype.vsCMPrototypeType, "int x = 0")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_InitExpressionAndType2() As Task
            Dim code =
<Code>
namespace N
{
    enum E
    {
        A$$ = 42
    }
}
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeInitExpression Or EnvDTE.vsCMPrototype.vsCMPrototypeType, "N.E A = 42")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_ClassNameInitExpressionAndType() As Task
            Dim code =
<Code>
namespace N
{
    enum E
    {
        A$$ = 42
    }
}
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeInitExpression Or EnvDTE.vsCMPrototype.vsCMPrototypeType Or EnvDTE.vsCMPrototype.vsCMPrototypeClassName, "N.E E.A = 42")
        End Function

#End Region

#Region "Type tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestType1() As Task
            Dim code =
<Code>
class C
{
    int $$i;
}
</Code>

            Await TestTypeProp(code,
                         New CodeTypeRefData With {
                             .AsString = "int",
                             .AsFullName = "System.Int32",
                             .CodeTypeFullName = "System.Int32",
                             .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefInt
                         })
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestType2() As Task
            Dim code =
<Code>
class C
{
    int i, $$j;
}
</Code>

            Await TestTypeProp(code,
                         New CodeTypeRefData With {
                             .AsString = "int",
                             .AsFullName = "System.Int32",
                             .CodeTypeFullName = "System.Int32",
                             .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefInt
                         })
        End Function

        <WorkItem(888785, "devdiv")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestArrayTypeName() As Task
            Dim code =
<Code>
class C
{
    int[] $$array;
}
</Code>

            Await TestTypeProp(code,
                         New CodeTypeRefData With {
                             .AsString = "int[]",
                             .AsFullName = "System.Int32[]",
                             .CodeTypeFullName = "System.Int32[]",
                             .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefArray
                         })
        End Function

#End Region

#Region "Set Access tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetEnumAccess1() As Task
            Dim code =
<Code>
enum E
{
    $$Foo
}
</Code>

            Dim expected =
<Code>
enum E
{
    Foo
}
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetEnumAccess2() As Task
            Dim code =
<Code>
enum E
{
    $$Foo
}
</Code>

            Dim expected =
<Code>
enum E
{
    Foo
}
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessDefault)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetEnumAccess3() As Task
            Dim code =
<Code>
enum E
{
    $$Foo
}
</Code>

            Dim expected =
<Code>
enum E
{
    Foo
}
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPrivate, ThrowsArgumentException(Of EnvDTE.vsCMAccess)())
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess1() As Task
            Dim code =
<Code>
class C
{
    int $$i;
}
</Code>

            Dim expected =
<Code>
class C
{
    public int i;
}
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess2() As Task
            Dim code =
<Code>
class C
{
    public int $$i;
}
</Code>

            Dim expected =
<Code>
class C
{
    int i;
}
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessDefault)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess3() As Task
            Dim code =
<Code>
class C
{
    private int $$i;
}
</Code>

            Dim expected =
<Code>
class C
{
    int i;
}
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessDefault)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess4() As Task
            Dim code =
<Code>
class C
{
    int $$i;
}
</Code>

            Dim expected =
<Code>
class C
{
    int i;
}
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessDefault)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess5() As Task
            Dim code =
<Code>
class C
{
    public int $$i;
}
</Code>

            Dim expected =
<Code>
class C
{
    protected internal int i;
}
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess6() As Task
            Dim code =
<Code>
class C
{
    #region Foo
    
    int x;

    #endregion

    int $$i;
}
</Code>

            Dim expected =
<Code>
class C
{
    #region Foo
    
    int x;

    #endregion

    public int i;
}
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess7() As Task
            Dim code =
<Code>
class C
{
    #region Foo
    
    int x;

    #endregion

    public int $$i;
}
</Code>

            Dim expected =
<Code>
class C
{
    #region Foo
    
    int x;

    #endregion

    protected internal int i;
}
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess8() As Task
            Dim code =
<Code>
class C
{
    #region Foo
    
    int x;

    #endregion

    public int $$i;
}
</Code>

            Dim expected =
<Code>
class C
{
    #region Foo
    
    int x;

    #endregion

    int i;
}
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessDefault)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess9() As Task
            Dim code =
<Code>
class C
{
    #region Foo

    int $$x;

    #endregion
}
</Code>

            Dim expected =
<Code>
class C
{
    #region Foo

    public int x;

    #endregion
}
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess10() As Task
            Dim code =
<Code>
class C
{
    #region Foo

    public int $$x;

    #endregion
}
</Code>

            Dim expected =
<Code>
class C
{
    #region Foo

    int x;

    #endregion
}
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessDefault)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess11() As Task
            Dim code =
<Code>
class C
{
    #region Foo

    public int $$x;

    #endregion
}
</Code>

            Dim expected =
<Code>
class C
{
    #region Foo

    protected internal int x;

    #endregion
}
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess12() As Task
            Dim code =
<Code>
class C
{
    #region Foo

    [Foo]
    public int $$x;

    #endregion
}
</Code>

            Dim expected =
<Code>
class C
{
    #region Foo

    [Foo]
    protected internal int x;

    #endregion
}
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess13() As Task
            Dim code =
<Code>
class C
{
    #region Foo

    // Comment comment comment
    public int $$x;

    #endregion
}
</Code>

            Dim expected =
<Code>
class C
{
    #region Foo

    // Comment comment comment
    protected internal int x;

    #endregion
}
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess14() As Task
            Dim code =
<Code><![CDATA[
class C
{
    #region Foo

    /// <summary>
    /// Comment comment comment
    /// </summary>
    public int $$x;

    #endregion
}
]]></Code>

            Dim expected =
<Code><![CDATA[
class C
{
    #region Foo

    /// <summary>
    /// Comment comment comment
    /// </summary>
    protected internal int x;

    #endregion
}
]]></Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Function

#End Region

#Region "Set ConstKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetConstKind1() As Task
            Dim code =
<Code>
enum E
{
    $$Foo
}
</Code>

            Dim expected =
<Code>
enum E
{
    Foo
}
</Code>

            Await TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindConst)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetConstKind2() As Task
            Dim code =
<Code>
enum E
{
    $$Foo
}
</Code>

            Dim expected =
<Code>
enum E
{
    Foo
}
</Code>

            Await TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindReadOnly)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetConstKind3() As Task
            Dim code =
<Code>
enum E
{
    $$Foo
}
</Code>

            Dim expected =
<Code>
enum E
{
    Foo
}
</Code>

            Await TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindNone, ThrowsArgumentException(Of EnvDTE80.vsCMConstKind))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetConstKind4() As Task
            Dim code =
<Code>
class C
{
    int $$x;
}
</Code>

            Dim expected =
<Code>
class C
{
    int x;
}
</Code>

            Await TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindNone)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetConstKind5() As Task
            Dim code =
<Code>
class C
{
    int $$x;
}
</Code>

            Dim expected =
<Code>
class C
{
    const int x;
}
</Code>

            Await TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindConst)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetConstKind6() As Task
            Dim code =
<Code>
class C
{
    const int $$x;
}
</Code>

            Dim expected =
<Code>
class C
{
    int x;
}
</Code>

            Await TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindNone)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetConstKind7() As Task
            Dim code =
<Code>
class C
{
    int $$x;
}
</Code>

            Dim expected =
<Code>
class C
{
    readonly int x;
}
</Code>

            Await TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindReadOnly)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetConstKind8() As Task
            Dim code =
<Code>
class C
{
    readonly int $$x;
}
</Code>

            Dim expected =
<Code>
class C
{
    int x;
}
</Code>

            Await TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindNone)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetConstKindWhenVolatileIsPresent1() As Task
            Dim code =
<Code>
class C
{
    volatile int $$x;
}
</Code>

            Dim expected =
<Code>
class C
{
    volatile const int x;
}
</Code>

            Await TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindConst)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetConstKindWhenVolatileIsPresent2() As Task
            Dim code =
<Code>
class C
{
    volatile int $$x;
}
</Code>

            Dim expected =
<Code>
class C
{
    volatile readonly int x;
}
</Code>

            Await TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindReadOnly)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetConstKindWhenVolatileIsPresent3() As Task
            Dim code =
<Code>
class C
{
    volatile readonly int $$x;
}
</Code>

            Dim expected =
<Code>
class C
{
    volatile const int x;
}
</Code>

            Await TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindConst)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetConstKindWhenVolatileIsPresent4() As Task
            Dim code =
<Code>
class C
{
    volatile readonly int $$x;
}
</Code>

            Dim expected =
<Code>
class C
{
    volatile int x;
}
</Code>

            Await TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindNone)
        End Function

#End Region

#Region "Set InitExpression tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetInitExpression1() As Task
            Dim code =
<Code>
class C
{
    int $$i;
}
</Code>

            Dim expected =
<Code>
class C
{
    int i = 42;
}
</Code>

            Await TestSetInitExpression(code, expected, "42")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetInitExpression2() As Task
            Dim code =
<Code>
class C
{
    int $$i = 42;
}
</Code>

            Dim expected =
<Code>
class C
{
    int i;
}
</Code>

            Await TestSetInitExpression(code, expected, Nothing)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetInitExpression3() As Task
            Dim code =
<Code>
class C
{
    int $$i, j;
}
</Code>

            Dim expected =
<Code>
class C
{
    int i = 42, j;
}
</Code>

            Await TestSetInitExpression(code, expected, "42")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetInitExpression4() As Task
            Dim code =
<Code>
class C
{
    int i, $$j;
}
</Code>

            Dim expected =
<Code>
class C
{
    int i, j = 42;
}
</Code>

            Await TestSetInitExpression(code, expected, "42")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetInitExpression5() As Task
            Dim code =
<Code>
class C
{
    const int $$i = 0;
}
</Code>

            Dim expected =
<Code>
class C
{
    const int i = 42;
}
</Code>

            Await TestSetInitExpression(code, expected, "42")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetInitExpression6() As Task
            Dim code =
<Code>
enum E
{
    $$Foo
}
</Code>

            Dim expected =
<Code>
enum E
{
    Foo = 42
}
</Code>

            Await TestSetInitExpression(code, expected, "42")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetInitExpression7() As Task
            Dim code =
<Code>
enum E
{
    $$Foo = 42
}
</Code>

            Dim expected =
<Code>
enum E
{
    Foo
}
</Code>

            Await TestSetInitExpression(code, expected, Nothing)
        End Function

#End Region

#Region "Set IsConstant tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsConstant1() As Task
            Dim code =
<Code>
enum E
{
    $$Foo
}
</Code>

            Dim expected =
<Code>
enum E
{
    Foo
}
</Code>

            Await TestSetIsConstant(code, expected, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsConstant2() As Task
            Dim code =
<Code>
enum E
{
    $$Foo
}
</Code>

            Dim expected =
<Code>
enum E
{
    Foo
}
</Code>

            Await TestSetIsConstant(code, expected, False, ThrowsCOMException(Of Boolean)(E_FAIL))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsConstant3() As Task
            Dim code =
<Code>
class C
{
    int $$i;
}
</Code>

            Dim expected =
<Code>
class C
{
    const int i;
}
</Code>

            Await TestSetIsConstant(code, expected, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsConstant4() As Task
            Dim code =
<Code>
class C
{
    int $$i;
}
</Code>

            Dim expected =
<Code>
class C
{
    int i;
}
</Code>

            Await TestSetIsConstant(code, expected, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsConstant5() As Task
            Dim code =
<Code>
class C
{
    const int $$i;
}
</Code>

            Dim expected =
<Code>
class C
{
    int i;
}
</Code>

            Await TestSetIsConstant(code, expected, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsConstant6() As Task
            Dim code =
<Code>
class C
{
    const int $$i;
}
</Code>

            Dim expected =
<Code>
class C
{
    const int i;
}
</Code>

            Await TestSetIsConstant(code, expected, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsConstant7() As Task
            Dim code =
<Code>
class C
{
    readonly int $$i;
}
</Code>

            Dim expected =
<Code>
class C
{
    int i;
}
</Code>

            Await TestSetIsConstant(code, expected, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsConstant8() As Task
            Dim code =
<Code>
class C
{
    readonly int $$i;
}
</Code>

            Dim expected =
<Code>
class C
{
    readonly int i;
}
</Code>

            Await TestSetIsConstant(code, expected, True)
        End Function

#End Region

#Region "Set IsShared tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsShared1() As Task
            Dim code =
<Code>
class C
{
    int $$i;
}
</Code>

            Dim expected =
<Code>
class C
{
    static int i;
}
</Code>

            Await TestSetIsShared(code, expected, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsShared2() As Task
            Dim code =
<Code>
class C
{
    static int $$i;
}
</Code>

            Dim expected =
<Code>
class C
{
    int i;
}
</Code>

            Await TestSetIsShared(code, expected, False)
        End Function

#End Region

#Region "Set Name tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetName1() As Task
            Dim code =
<Code>
class C
{
    int $$Foo;
}
</Code>

            Dim expected =
<Code>
class C
{
    int Bar;
}
</Code>

            Await TestSetName(code, expected, "Bar", NoThrow(Of String)())
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetName2() As Task
            Dim code =
<Code>
class C
{
    #region Foo
    int $$Foo;
    #endregion
}
</Code>

            Dim expected =
<Code>
class C
{
    #region Foo
    int Bar;
    #endregion
}
</Code>

            Await TestSetName(code, expected, "Bar", NoThrow(Of String)())
        End Function

#End Region

#Region "Set Type tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType1() As Task
            Dim code =
<Code>
class C
{
    int $$i;
}
</Code>

            Dim expected =
<Code>
class C
{
    double i;
}
</Code>

            Await TestSetTypeProp(code, expected, "double")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType2() As Task
            Dim code =
<Code>
class C
{
    int i, $$j;
}
</Code>

            Dim expected =
<Code>
class C
{
    double i, j;
}
</Code>

            Await TestSetTypeProp(code, expected, "double")
        End Function

#End Region

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestTypeDescriptor_GetProperties() As Task
            Dim code =
<Code>
class S
{
    int $$x;
}
</Code>

            Dim expectedPropertyNames =
                {"DTE", "Collection", "Name", "FullName", "ProjectItem", "Kind", "IsCodeType",
                 "InfoLocation", "Children", "Language", "StartPoint", "EndPoint", "ExtenderNames",
                 "ExtenderCATID", "Parent", "InitExpression", "Type", "Access", "IsConstant", "Attributes",
                 "DocComment", "Comment", "IsShared", "ConstKind", "IsGeneric"}

            Await TestPropertyDescriptors(code, expectedPropertyNames)
        End Function

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.CSharp
            End Get
        End Property

    End Class
End Namespace
