' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp
    Public Class CodeVariableTests
        Inherits AbstractCodeVariableTests

#Region "GetStartPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetStartPoint_Field()
            Dim code =
<Code>
class C
{
    int $$foo;
}
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
                     TextPoint(line:=3, lineOffset:=9, absoluteOffset:=19, lineLength:=12)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=15, lineLength:=12)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetStartPoint_EnumMember()
            Dim code =
<Code>
enum E
{
    $$Foo
}
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
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=14, lineLength:=7)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=14, lineLength:=7)))
        End Sub

#End Region

#Region "GetEndPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetEndPoint_Field()
            Dim code =
<Code>
class C
{
    int $$foo;
}
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
                     TextPoint(line:=3, lineOffset:=12, absoluteOffset:=22, lineLength:=12)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=13, absoluteOffset:=23, lineLength:=12)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetEndPoint_EnumMember()
            Dim code =
<Code>
enum E
{
    $$Foo
}
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
                     TextPoint(line:=3, lineOffset:=8, absoluteOffset:=17, lineLength:=7)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=8, absoluteOffset:=17, lineLength:=7)))
        End Sub

#End Region

#Region "Access tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access1()
            Dim code =
<Code>
class C
{
    int $$x;
}
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPrivate)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access2()
            Dim code =
<Code>
class C
{
    private int $$x;
}
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPrivate)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access3()
            Dim code =
<Code>
class C
{
    protected int $$x;
}
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProtected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access4()
            Dim code =
<Code>
class C
{
    protected internal int $$x;
}
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access5()
            Dim code =
<Code>
class C
{
    internal int $$x;
}
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access6()
            Dim code =
<Code>
class C
{
    public int $$x;
}
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access7()
            Dim code =
<Code>
enum E
{
    $$Foo
}
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

#End Region

#Region "Attributes tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Attributes1()
            Dim code =
<Code>
class C
{
    int $$Foo;
}
</Code>

            TestAttributes(code, NoElements)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Attributes2()
            Dim code =
<Code>
using System;

class C
{
    [Serializable]
    int $$Foo;
}
</Code>

            TestAttributes(code, IsElement("Serializable"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Attributes3()
            Dim code =
<Code>using System;
    
class C
{
    [Serializable]
    [CLSCompliant(true)]
    int $$Foo;
}
</Code>

            TestAttributes(code, IsElement("Serializable"), IsElement("CLSCompliant"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Attributes4()
            Dim code =
<Code>using System;

class C
{
    [Serializable, CLSCompliant(true)]
    int $$Foo;
}
</Code>

            TestAttributes(code, IsElement("Serializable"), IsElement("CLSCompliant"))
        End Sub
#End Region

#Region "AddAttribute tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute1()
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

            TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute2()
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
            TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "true", .Position = 1})
        End Sub

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute_BelowDocComment()
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
            TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "true"})
        End Sub

#End Region

#Region "ConstKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub ConstKind1()
            Dim code =
<Code>
enum E
{
    $$Foo
}
</Code>

            TestConstKind(code, EnvDTE80.vsCMConstKind.vsCMConstKindConst)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub ConstKind2()
            Dim code =
<Code>
class C
{
    int $$x;
}
</Code>

            TestConstKind(code, EnvDTE80.vsCMConstKind.vsCMConstKindNone)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub ConstKind3()
            Dim code =
<Code>
class C
{
    const int $$x;
}
</Code>

            TestConstKind(code, EnvDTE80.vsCMConstKind.vsCMConstKindConst)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub ConstKind4()
            Dim code =
<Code>
class C
{
    readonly int $$x;
}
</Code>

            TestConstKind(code, EnvDTE80.vsCMConstKind.vsCMConstKindReadOnly)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub ConstKind5()
            Dim code =
<Code>
class C
{
    readonly const int $$x;
}
</Code>

            TestConstKind(code, EnvDTE80.vsCMConstKind.vsCMConstKindConst Or EnvDTE80.vsCMConstKind.vsCMConstKindReadOnly)
        End Sub

#End Region

#Region "FullName tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub FullName1()
            Dim code =
<Code>
enum E
{
    $$Foo = 1,
    Bar
}
</Code>

            TestFullName(code, "E.Foo")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub FullName2()
            Dim code =
<Code>
enum E
{
    Foo = 1,
    $$Bar
}
</Code>

            TestFullName(code, "E.Bar")
        End Sub

#End Region

#Region "InitExpression tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub InitExpression1()
            Dim code =
<Code>
class C
{
    int $$i = 42;
}
</Code>

            TestInitExpression(code, "42")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub InitExpression2()
            Dim code =
<Code>
class C
{
    const int $$i = 19 + 23;
}
</Code>

            TestInitExpression(code, "19 + 23")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub InitExpression3()
            Dim code =
<Code>
enum E
{
    $$i = 19 + 23
}
</Code>

            TestInitExpression(code, "19 + 23")
        End Sub

#End Region

#Region "IsConstant tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub IsConstant1()
            Dim code =
<Code>
enum E
{
    $$Foo
}
</Code>

            TestIsConstant(code, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub IsConstant2()
            Dim code =
<Code>
class C
{
    const int $$x = 0;
}
</Code>

            TestIsConstant(code, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub IsConstant3()
            Dim code =
<Code>
class C
{
    readonly int $$x = 0;
}
</Code>

            TestIsConstant(code, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub IsConstant4()
            Dim code =
<Code>
class C
{
    int $$x = 0;
}
</Code>

            TestIsConstant(code, False)
        End Sub

#End Region

#Region "IsShared tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub IsShared1()
            Dim code =
<Code>
class C
{
    int $$x;
}
</Code>

            TestIsShared(code, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub IsShared2()
            Dim code =
<Code>
class C
{
    static int $$x;
}
</Code>

            TestIsShared(code, True)
        End Sub

#End Region

#Region "Name tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Name1()
            Dim code =
<Code>
enum E
{
    $$Foo = 1,
    Bar
}
</Code>

            TestName(code, "Foo")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Name2()
            Dim code =
<Code>
enum E
{
    Foo = 1,
    $$Bar
}
</Code>

            TestName(code, "Bar")
        End Sub

#End Region

#Region "Prototype tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_ClassName()
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

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName, "C.x")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_FullName()
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

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeFullname, "N.C.x")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_InitExpression1()
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

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeInitExpression, "x = 0")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_InitExpression2()
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

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeInitExpression, "A = 42")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_InitExpressionAndType1()
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

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeInitExpression Or EnvDTE.vsCMPrototype.vsCMPrototypeType, "int x = 0")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_InitExpressionAndType2()
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

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeInitExpression Or EnvDTE.vsCMPrototype.vsCMPrototypeType, "N.E A = 42")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_ClassNameInitExpressionAndType()
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

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeInitExpression Or EnvDTE.vsCMPrototype.vsCMPrototypeType Or EnvDTE.vsCMPrototype.vsCMPrototypeClassName, "N.E E.A = 42")
        End Sub

#End Region

#Region "Type tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Type1()
            Dim code =
<Code>
class C
{
    int $$i;
}
</Code>

            TestTypeProp(code,
                         New CodeTypeRefData With {
                             .AsString = "int",
                             .AsFullName = "System.Int32",
                             .CodeTypeFullName = "System.Int32",
                             .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefInt
                         })
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Type2()
            Dim code =
<Code>
class C
{
    int i, $$j;
}
</Code>

            TestTypeProp(code,
                         New CodeTypeRefData With {
                             .AsString = "int",
                             .AsFullName = "System.Int32",
                             .CodeTypeFullName = "System.Int32",
                             .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefInt
                         })
        End Sub

        <WorkItem(888785, "devdiv")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub ArrayTypeName()
            Dim code =
<Code>
class C
{
    int[] $$array;
}
</Code>

            TestTypeProp(code,
                         New CodeTypeRefData With {
                             .AsString = "int[]",
                             .AsFullName = "System.Int32[]",
                             .CodeTypeFullName = "System.Int32[]",
                             .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefArray
                         })
        End Sub

#End Region

#Region "Set Access tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetEnumAccess1()
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

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetEnumAccess2()
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

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessDefault)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetEnumAccess3()
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

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPrivate, ThrowsArgumentException(Of EnvDTE.vsCMAccess)())
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess1()
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

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess2()
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

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessDefault)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess3()
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

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessDefault)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess4()
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

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessDefault)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess5()
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

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess6()
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

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess7()
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

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess8()
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

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessDefault)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess9()
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

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess10()
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

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessDefault)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess11()
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

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess12()
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

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess13()
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

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess14()
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

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Sub

#End Region

#Region "Set ConstKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetConstKind1()
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

            TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindConst)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetConstKind2()
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

            TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindReadOnly)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetConstKind3()
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

            TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindNone, ThrowsArgumentException(Of EnvDTE80.vsCMConstKind))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetConstKind4()
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

            TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindNone)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetConstKind5()
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

            TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindConst)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetConstKind6()
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

            TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindNone)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetConstKind7()
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

            TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindReadOnly)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetConstKind8()
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

            TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindNone)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetConstKindWhenVolatileIsPresent1()
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

            TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindConst)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetConstKindWhenVolatileIsPresent2()
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

            TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindReadOnly)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetConstKindWhenVolatileIsPresent3()
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

            TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindConst)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetConstKindWhenVolatileIsPresent4()
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

            TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindNone)
        End Sub

#End Region

#Region "Set InitExpression tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetInitExpression1()
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

            TestSetInitExpression(code, expected, "42")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetInitExpression2()
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

            TestSetInitExpression(code, expected, Nothing)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetInitExpression3()
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

            TestSetInitExpression(code, expected, "42")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetInitExpression4()
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

            TestSetInitExpression(code, expected, "42")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetInitExpression5()
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

            TestSetInitExpression(code, expected, "42")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetInitExpression6()
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

            TestSetInitExpression(code, expected, "42")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetInitExpression7()
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

            TestSetInitExpression(code, expected, Nothing)
        End Sub

#End Region

#Region "Set IsConstant tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsConstant1()
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

            TestSetIsConstant(code, expected, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsConstant2()
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

            TestSetIsConstant(code, expected, False, ThrowsCOMException(Of Boolean)(E_FAIL))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsConstant3()
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

            TestSetIsConstant(code, expected, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsConstant4()
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

            TestSetIsConstant(code, expected, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsConstant5()
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

            TestSetIsConstant(code, expected, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsConstant6()
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

            TestSetIsConstant(code, expected, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsConstant7()
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

            TestSetIsConstant(code, expected, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsConstant8()
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

            TestSetIsConstant(code, expected, True)
        End Sub

#End Region

#Region "Set IsShared tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsShared1()
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

            TestSetIsShared(code, expected, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsShared2()
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

            TestSetIsShared(code, expected, False)
        End Sub

#End Region

#Region "Set Name tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetName1()
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

            TestSetName(code, expected, "Bar", NoThrow(Of String)())
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetName2()
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

            TestSetName(code, expected, "Bar", NoThrow(Of String)())
        End Sub

#End Region

#Region "Set Type tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetType1()
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

            TestSetTypeProp(code, expected, "double")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetType2()
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

            TestSetTypeProp(code, expected, "double")
        End Sub

#End Region

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TypeDescriptor_GetProperties()
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

            TestPropertyDescriptors(code, expectedPropertyNames)
        End Sub

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.CSharp
            End Get
        End Property

    End Class
End Namespace
