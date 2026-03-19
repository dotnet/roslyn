' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp
    Public Class CodeVariableTests
        Inherits AbstractCodeVariableTests

#Region "GetStartPoint() tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint_Field()
            Dim code =
<Code>
class C
{
    int $$goo;
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint_EnumMember()
            Dim code =
<Code>
enum E
{
    $$Goo
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint_Field()
            Dim code =
<Code>
class C
{
    int $$goo;
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint_EnumMember()
            Dim code =
<Code>
enum E
{
    $$Goo
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess1()
            Dim code =
<Code>
class C
{
    int $$x;
}
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPrivate)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess2()
            Dim code =
<Code>
class C
{
    private int $$x;
}
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPrivate)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess3()
            Dim code =
<Code>
class C
{
    protected int $$x;
}
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProtected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess4()
            Dim code =
<Code>
class C
{
    protected internal int $$x;
}
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess5()
            Dim code =
<Code>
class C
{
    internal int $$x;
}
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess6()
            Dim code =
<Code>
class C
{
    public int $$x;
}
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess7()
            Dim code =
<Code>
enum E
{
    $$Goo
}
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

#End Region

#Region "Attributes tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAttributes1()
            Dim code =
<Code>
class C
{
    int $$Goo;
}
</Code>

            TestAttributes(code, NoElements)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAttributes2()
            Dim code =
<Code>
using System;

class C
{
    [Serializable]
    int $$Goo;
}
</Code>

            TestAttributes(code, IsElement("Serializable"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAttributes3()
            Dim code =
<Code>using System;
    
class C
{
    [Serializable]
    [CLSCompliant(true)]
    int $$Goo;
}
</Code>

            TestAttributes(code, IsElement("Serializable"), IsElement("CLSCompliant"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAttributes4()
            Dim code =
<Code>using System;

class C
{
    [Serializable, CLSCompliant(true)]
    int $$Goo;
}
</Code>

            TestAttributes(code, IsElement("Serializable"), IsElement("CLSCompliant"))
        End Sub
#End Region

#Region "AddAttribute tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/2825")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestConstKind1()
            Dim code =
<Code>
enum E
{
    $$Goo
}
</Code>

            TestConstKind(code, EnvDTE80.vsCMConstKind.vsCMConstKindConst)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestConstKind2()
            Dim code =
<Code>
class C
{
    int $$x;
}
</Code>

            TestConstKind(code, EnvDTE80.vsCMConstKind.vsCMConstKindNone)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestConstKind3()
            Dim code =
<Code>
class C
{
    const int $$x;
}
</Code>

            TestConstKind(code, EnvDTE80.vsCMConstKind.vsCMConstKindConst)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestConstKind4()
            Dim code =
<Code>
class C
{
    readonly int $$x;
}
</Code>

            TestConstKind(code, EnvDTE80.vsCMConstKind.vsCMConstKindReadOnly)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestConstKind5()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestFullName1()
            Dim code =
<Code>
enum E
{
    $$Goo = 1,
    Bar
}
</Code>

            TestFullName(code, "E.Goo")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestFullName2()
            Dim code =
<Code>
enum E
{
    Goo = 1,
    $$Bar
}
</Code>

            TestFullName(code, "E.Bar")
        End Sub

#End Region

#Region "InitExpression tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestInitExpression1()
            Dim code =
<Code>
class C
{
    int $$i = 42;
}
</Code>

            TestInitExpression(code, "42")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestInitExpression2()
            Dim code =
<Code>
class C
{
    const int $$i = 19 + 23;
}
</Code>

            TestInitExpression(code, "19 + 23")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestInitExpression3()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestIsConstant1()
            Dim code =
<Code>
enum E
{
    $$Goo
}
</Code>

            TestIsConstant(code, True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestIsConstant2()
            Dim code =
<Code>
class C
{
    const int $$x = 0;
}
</Code>

            TestIsConstant(code, True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestIsConstant3()
            Dim code =
<Code>
class C
{
    readonly int $$x = 0;
}
</Code>

            TestIsConstant(code, True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestIsConstant4()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestIsShared1()
            Dim code =
<Code>
class C
{
    int $$x;
}
</Code>

            TestIsShared(code, False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestIsShared2()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName1()
            Dim code =
<Code>
enum E
{
    $$Goo = 1,
    Bar
}
</Code>

            TestName(code, "Goo")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName2()
            Dim code =
<Code>
enum E
{
    Goo = 1,
    $$Bar
}
</Code>

            TestName(code, "Bar")
        End Sub

#End Region

#Region "Prototype tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_ClassName()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_FullName()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_InitExpression1()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_InitExpression2()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_InitExpressionAndType1()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_InitExpressionAndType2()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_ClassNameInitExpressionAndType()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestType1()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestType2()
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/888785")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestArrayTypeName()
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
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetEnumAccess1() As Task
            Dim code =
<Code>
enum E
{
    $$Goo
}
</Code>

            Dim expected =
<Code>
enum E
{
    Goo
}
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetEnumAccess2() As Task
            Dim code =
<Code>
enum E
{
    $$Goo
}
</Code>

            Dim expected =
<Code>
enum E
{
    Goo
}
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessDefault)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetEnumAccess3() As Task
            Dim code =
<Code>
enum E
{
    $$Goo
}
</Code>

            Dim expected =
<Code>
enum E
{
    Goo
}
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPrivate, ThrowsArgumentException(Of EnvDTE.vsCMAccess)())
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess6() As Task
            Dim code =
<Code>
class C
{
    #region Goo
    
    int x;

    #endregion

    int $$i;
}
</Code>

            Dim expected =
<Code>
class C
{
    #region Goo
    
    int x;

    #endregion

    public int i;
}
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess7() As Task
            Dim code =
<Code>
class C
{
    #region Goo
    
    int x;

    #endregion

    public int $$i;
}
</Code>

            Dim expected =
<Code>
class C
{
    #region Goo
    
    int x;

    #endregion

    protected internal int i;
}
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess8() As Task
            Dim code =
<Code>
class C
{
    #region Goo
    
    int x;

    #endregion

    public int $$i;
}
</Code>

            Dim expected =
<Code>
class C
{
    #region Goo
    
    int x;

    #endregion

    int i;
}
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessDefault)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess9() As Task
            Dim code =
<Code>
class C
{
    #region Goo

    int $$x;

    #endregion
}
</Code>

            Dim expected =
<Code>
class C
{
    #region Goo

    public int x;

    #endregion
}
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess10() As Task
            Dim code =
<Code>
class C
{
    #region Goo

    public int $$x;

    #endregion
}
</Code>

            Dim expected =
<Code>
class C
{
    #region Goo

    int x;

    #endregion
}
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessDefault)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess11() As Task
            Dim code =
<Code>
class C
{
    #region Goo

    public int $$x;

    #endregion
}
</Code>

            Dim expected =
<Code>
class C
{
    #region Goo

    protected internal int x;

    #endregion
}
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess12() As Task
            Dim code =
<Code>
class C
{
    #region Goo

    [Goo]
    public int $$x;

    #endregion
}
</Code>

            Dim expected =
<Code>
class C
{
    #region Goo

    [Goo]
    protected internal int x;

    #endregion
}
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess13() As Task
            Dim code =
<Code>
class C
{
    #region Goo

    // Comment comment comment
    public int $$x;

    #endregion
}
</Code>

            Dim expected =
<Code>
class C
{
    #region Goo

    // Comment comment comment
    protected internal int x;

    #endregion
}
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess14() As Task
            Dim code =
<Code><![CDATA[
class C
{
    #region Goo

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
    #region Goo

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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetConstKind1() As Task
            Dim code =
<Code>
enum E
{
    $$Goo
}
</Code>

            Dim expected =
<Code>
enum E
{
    Goo
}
</Code>

            Await TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindConst)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetConstKind2() As Task
            Dim code =
<Code>
enum E
{
    $$Goo
}
</Code>

            Dim expected =
<Code>
enum E
{
    Goo
}
</Code>

            Await TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindReadOnly)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetConstKind3() As Task
            Dim code =
<Code>
enum E
{
    $$Goo
}
</Code>

            Dim expected =
<Code>
enum E
{
    Goo
}
</Code>

            Await TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindNone, ThrowsArgumentException(Of EnvDTE80.vsCMConstKind))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetInitExpression6() As Task
            Dim code =
<Code>
enum E
{
    $$Goo
}
</Code>

            Dim expected =
<Code>
enum E
{
    Goo = 42
}
</Code>

            Await TestSetInitExpression(code, expected, "42")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetInitExpression7() As Task
            Dim code =
<Code>
enum E
{
    $$Goo = 42
}
</Code>

            Dim expected =
<Code>
enum E
{
    Goo
}
</Code>

            Await TestSetInitExpression(code, expected, Nothing)
        End Function

#End Region

#Region "Set IsConstant tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsConstant1() As Task
            Dim code =
<Code>
enum E
{
    $$Goo
}
</Code>

            Dim expected =
<Code>
enum E
{
    Goo
}
</Code>

            Await TestSetIsConstant(code, expected, True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsConstant2() As Task
            Dim code =
<Code>
enum E
{
    $$Goo
}
</Code>

            Dim expected =
<Code>
enum E
{
    Goo
}
</Code>

            Await TestSetIsConstant(code, expected, False, ThrowsCOMException(Of Boolean)(E_FAIL))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetName1() As Task
            Dim code =
<Code>
class C
{
    int $$Goo;
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetName2() As Task
            Dim code =
<Code>
class C
{
    #region Goo
    int $$Goo;
    #endregion
}
</Code>

            Dim expected =
<Code>
class C
{
    #region Goo
    int Bar;
    #endregion
}
</Code>

            Await TestSetName(code, expected, "Bar", NoThrow(Of String)())
        End Function

#End Region

#Region "Set Type tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestTypeDescriptor_GetProperties()
            Dim code =
<Code>
class S
{
    int $$x;
}
</Code>

            TestPropertyDescriptors(Of EnvDTE80.CodeVariable2)(code)
        End Sub

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.CSharp
            End Get
        End Property

    End Class
End Namespace
