' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.VisualBasic
    Public Class CodeVariableTests
        Inherits AbstractCodeVariableTests

#Region "GetStartPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint1() As Task
            Dim code =
<Code>
Class C
    Dim i$$ As Integer
End Class
</Code>

            Await TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=2, lineOffset:=9, absoluteOffset:=17, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=20)))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint_Attribute() As Task
            Dim code =
<Code>
Class C
    &lt;System.CLSCompliant(True)&gt;
    Dim i$$ As Integer
End Class
</Code>

            Await TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=45, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=45, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=45, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=9, absoluteOffset:=49, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=45, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=45, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=31)))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint_EnumMember() As Task
            Dim code =
<Code>
Enum E
    A$$
End Enum
</Code>

            Await TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=12, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=12, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=12, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=12, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=12, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=12, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=12, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=12, lineLength:=5)))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint_EnumMember_Attribute() As Task
            Dim code =
<Code>
Enum E
    &lt;System.CLSCompliant(True)&gt;
    A$$
End Enum
</Code>

            Await TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=12, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=12, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=44, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=44, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=44, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=12, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=44, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=44, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=44, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=12, lineLength:=31)))
        End Function

#End Region

#Region "GetEndPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint1() As Task
            Dim code =
<Code>
Class C
    Dim i$$ As Integer
End Class
</Code>

            Await TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=2, lineOffset:=21, absoluteOffset:=29, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=2, lineOffset:=21, absoluteOffset:=29, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=2, lineOffset:=21, absoluteOffset:=29, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=2, lineOffset:=21, absoluteOffset:=29, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=2, lineOffset:=10, absoluteOffset:=18, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=2, lineOffset:=21, absoluteOffset:=29, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=2, lineOffset:=21, absoluteOffset:=29, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=2, lineOffset:=21, absoluteOffset:=29, lineLength:=20)))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint_Attribute() As Task
            Dim code =
<Code>
Class C
    &lt;System.CLSCompliant(True)&gt;
    Dim i$$ As Integer
End Class
</Code>

            Await TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     TextPoint(line:=2, lineOffset:=32, absoluteOffset:=40, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=2, lineOffset:=32, absoluteOffset:=40, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=21, absoluteOffset:=61, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=21, absoluteOffset:=61, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=21, absoluteOffset:=61, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=21, absoluteOffset:=61, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=10, absoluteOffset:=50, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=21, absoluteOffset:=61, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=21, absoluteOffset:=61, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=21, absoluteOffset:=61, lineLength:=20)))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint_EnumMember() As Task
            Dim code =
<Code>
Enum E
    A$$
End Enum
</Code>

            Await TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=2, lineOffset:=6, absoluteOffset:=13, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=2, lineOffset:=6, absoluteOffset:=13, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=2, lineOffset:=6, absoluteOffset:=13, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=2, lineOffset:=6, absoluteOffset:=13, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=2, lineOffset:=6, absoluteOffset:=13, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=2, lineOffset:=6, absoluteOffset:=13, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=2, lineOffset:=6, absoluteOffset:=13, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=2, lineOffset:=6, absoluteOffset:=13, lineLength:=5)))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint_EnumMember_Attribute() As Task
            Dim code =
<Code>
Enum E
    &lt;System.CLSCompliant(True)&gt;
    A$$
End Enum
</Code>

            Await TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     TextPoint(line:=2, lineOffset:=32, absoluteOffset:=39, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=2, lineOffset:=32, absoluteOffset:=39, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=6, absoluteOffset:=45, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=6, absoluteOffset:=45, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=6, absoluteOffset:=45, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=6, absoluteOffset:=45, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=6, absoluteOffset:=45, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=6, absoluteOffset:=45, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=6, absoluteOffset:=45, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=6, absoluteOffset:=45, lineLength:=5)))
        End Function

#End Region

#Region "Access tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess1() As Task
            Dim code =
    <Code>
Class C
    Dim $$x as Integer
End Class
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPrivate)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess2() As Task
            Dim code =
    <Code>
Class C
    Private $$x as Integer
End Class
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPrivate)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess3() As Task
            Dim code =
    <Code>
Class C
    Protected $$x as Integer
End Class
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProtected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess4() As Task
            Dim code =
    <Code>
Class C
    Protected Friend $$x as Integer
End Class
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess5() As Task
            Dim code =
    <Code>
Class C
    Friend $$x as Integer
End Class
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess6() As Task
            Dim code =
    <Code>
Class C
    Public $$x as Integer
End Class
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess7() As Task
            Dim code =
    <Code>
Enum E
    $$Foo
End Enum
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

#End Region

#Region "Comment tests"

        <WorkItem(638909)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestComment1() As Task
            Dim code =
<Code>
Class C
    ' Foo
    Dim $$i As Integer
End Class
</Code>

            Dim result = " Foo"

            Await TestComment(code, result)
        End Function

#End Region

#Region "ConstKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestConstKind1() As Task
            Dim code =
<Code>
Enum E
    $$Foo
End Enum
</Code>

            Await TestConstKind(code, EnvDTE80.vsCMConstKind.vsCMConstKindConst)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestConstKind2() As Task
            Dim code =
<Code>
Class C
    Dim $$x As Integer
End Class
</Code>

            Await TestConstKind(code, EnvDTE80.vsCMConstKind.vsCMConstKindNone)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestConstKind3() As Task
            Dim code =
<Code>
Class C
    Const $$x As Integer
End Class
</Code>

            Await TestConstKind(code, EnvDTE80.vsCMConstKind.vsCMConstKindConst)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestConstKind4() As Task
            Dim code =
<Code>
Class C
    ReadOnly $$x As Integer
End Class
</Code>

            Await TestConstKind(code, EnvDTE80.vsCMConstKind.vsCMConstKindReadOnly)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestConstKind5() As Task
            Dim code =
<Code>
Class C
    ReadOnly Const $$x As Integer
End Class
</Code>

            Await TestConstKind(code, EnvDTE80.vsCMConstKind.vsCMConstKindConst)
        End Function

#End Region

#Region "InitExpression tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestInitExpression1() As Task
            Dim code =
<Code>
Class C
    Dim i$$ As Integer = 42
End Class
</Code>

            Await TestInitExpression(code, "42")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestInitExpression2() As Task
            Dim code =
<Code>
Class C
    Const $$i As Integer = 19 + 23
End Class
</Code>

            Await TestInitExpression(code, "19 + 23")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestInitExpression3() As Task
            Dim code =
<Code>
Enum E
    $$i = 19 + 23
End Enum
</Code>

            Await TestInitExpression(code, "19 + 23")
        End Function

#End Region

#Region "IsConstant tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestIsConstant1() As Task
            Dim code =
    <Code>
Enum E
    $$Foo
End Enum
</Code>
            Await TestIsConstant(code, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestIsConstant2() As Task
            Dim code =
    <Code>
Class C
    Dim $$x As Integer
End Class
</Code>
            Await TestIsConstant(code, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestIsConstant3() As Task
            Dim code =
    <Code>
Class C
    Const $$x As Integer = 0
End Class
</Code>
            Await TestIsConstant(code, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestIsConstant4() As Task
            Dim code =
    <Code>
Class C
    ReadOnly $$x As Integer = 0
End Class
</Code>
            Await TestIsConstant(code, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestIsConstant5() As Task
            Dim code =
    <Code>
Class C
    WithEvents $$x As Integer
End Class
</Code>
            Await TestIsConstant(code, False)
        End Function

#End Region

#Region "IsShared tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestIsShared1() As Task
            Dim code =
<Code>
Class C
    Dim $$x As Integer
End Class
</Code>

            Await TestIsShared(code, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestIsShared2() As Task
            Dim code =
<Code>
Class C
    Shared $$x As Integer
End Class
</Code>

            Await TestIsShared(code, True)
        End Function

#End Region

#Region "Name tests"

        <WorkItem(638224)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName_EnumMember() As Task
            Dim code =
<Code>
Enum SomeEnum
    A$$
End Enum
</Code>

            Await TestName(code, "A")
        End Function

#End Region

#Region "Prototype tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_UniqueSignature() As Task
            Dim code =
<Code>
Namespace N
    Class C
        Dim $$x As Integer = 42
    End Class
End Namespace
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeUniqueSignature, "F:N.C.x")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_ClassName1() As Task
            Dim code =
<Code>
Namespace N
    Class C
        Dim $$x As Integer = 42
    End Class
End Namespace
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName, "Private C.x")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_ClassName2() As Task
            Dim code =
<Code>
Namespace N
    Class C(Of T)
        Dim $$x As Integer = 42
    End Class
End Namespace
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName, "Private C(Of T).x")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_ClassName3() As Task
            Dim code =
<Code>
Namespace N
    Class C
        Public ReadOnly $$x As Integer = 42
    End Class
End Namespace
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName, "Public C.x")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_ClassName_InitExpression() As Task
            Dim code =
<Code>
Namespace N
    Class C
        Dim $$x As Integer = 42
    End Class
End Namespace
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName Or EnvDTE.vsCMPrototype.vsCMPrototypeInitExpression, "Private C.x = 42")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_FullName1() As Task
            Dim code =
<Code>
Namespace N
    Class C
        Dim $$x As Integer = 42
    End Class
End Namespace
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeFullname, "Private N.C.x")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_FullName2() As Task
            Dim code =
<Code>
Namespace N
    Class C(Of T)
        Dim $$x As Integer = 42
    End Class
End Namespace
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeFullname, "Private N.C(Of T).x")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_FullName_InitExpression() As Task
            Dim code =
<Code>
Namespace N
    Class C
        Dim $$x As Integer = 42
    End Class
End Namespace
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeFullname Or EnvDTE.vsCMPrototype.vsCMPrototypeInitExpression, "Private N.C.x = 42")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_NoName() As Task
            Dim code =
<Code>
Namespace N
    Class C
        Dim $$x As Integer = 42
    End Class
End Namespace
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeNoName, "Private ")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_NoName_InitExpression() As Task
            Dim code =
<Code>
Namespace N
    Class C
        Dim $$x As Integer = 42
    End Class
End Namespace
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeNoName Or EnvDTE.vsCMPrototype.vsCMPrototypeInitExpression, "Private  = 42")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_NoName_InitExpression_Type() As Task
            Dim code =
<Code>
Namespace N
    Class C
        Dim $$x As Integer = 42
    End Class
End Namespace
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeNoName Or EnvDTE.vsCMPrototype.vsCMPrototypeInitExpression Or EnvDTE.vsCMPrototype.vsCMPrototypeType, "Private  As Integer = 42")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_InitExpression_Type_ForAsNew() As Task
            ' Amusingly, this will *crash* Dev10.

            Dim code =
<Code>
Namespace N
    Class C
        Dim $$x As New System.Text.StringBuilder
    End Class
End Namespace
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeInitExpression Or EnvDTE.vsCMPrototype.vsCMPrototypeType, "Private x As System.Text.StringBuilder")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_Type() As Task
            Dim code =
<Code>
Namespace N
    Class C
        Dim $$x As Integer = 42
    End Class
End Namespace
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeType, "Private x As Integer")
        End Function

#End Region

#Region "Type tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestType1() As Task
            Dim code =
<Code>
Class C
    Dim $$a As Integer
End Class
</Code>

            Await TestTypeProp(code,
                         New CodeTypeRefData With {
                             .AsString = "Integer",
                             .AsFullName = "System.Int32",
                             .CodeTypeFullName = "System.Int32",
                             .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefInt
                         })
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestType2() As Task
            Dim code =
<Code>
Class C
    WithEvents $$a As Object
End Class
</Code>

            Await TestTypeProp(code,
             New CodeTypeRefData With {
                 .AsString = "Object",
                 .AsFullName = "System.Object",
                 .CodeTypeFullName = "System.Object",
                 .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefObject
             })
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestType3() As Task
            Dim code =
<Code>
Class C
    Private $$a As New Object
End Class
</Code>

            Await TestTypeProp(code,
             New CodeTypeRefData With {
                 .AsString = "Object",
                 .AsFullName = "System.Object",
                 .CodeTypeFullName = "System.Object",
                 .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefObject
             })
        End Function

#End Region

#Region "AddAttribute tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute1() As Task
            Dim code =
<Code>
Imports System

Class C
    Dim $$foo As Integer
End Class
</Code>

            Dim expected =
<Code><![CDATA[
Imports System

Class C
    <Serializable()>
    Dim foo As Integer
End Class
]]></Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Function

        <WorkItem(1087167)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute2() As Task
            Dim code =
<Code><![CDATA[
Imports System

Class C
    <Serializable>
    Dim $$foo As Integer
End Class
]]></Code>

            Dim expected =
<Code><![CDATA[
Imports System

Class C
    <Serializable>
    <CLSCompliant(True)>
    Dim foo As Integer
End Class
]]></Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "True", .Position = 1})
        End Function

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute_BelowDocComment() As Task
            Dim code =
<Code><![CDATA[
Imports System

Class C
    ''' &lt;summary&gt;&lt;/summary&gt;
    Dim $$foo As Integer
End Class
]]></Code>

            Dim expected =
<Code><![CDATA[
Imports System

Class C
    ''' &lt;summary&gt;&lt;/summary&gt;
    <CLSCompliant(True)>
    Dim foo As Integer
End Class
]]></Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "True"})
        End Function

#End Region

#Region "Set Access tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetEnumAccess1() As Task
            Dim code =
<Code>
Enum E
    $$Foo
End Enum
</Code>

            Dim expected =
<Code>
Enum E
    Foo
End Enum
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetEnumAccess2() As Task
            Dim code =
<Code>
Enum E
    $$Foo
End Enum
</Code>

            Dim expected =
<Code>
Enum E
    Foo
End Enum
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessDefault)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetEnumAccess3() As Task
            Dim code =
<Code>
Enum E
    $$Foo
End Enum
</Code>

            Dim expected =
<Code>
Enum E
    Foo
End Enum
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPrivate, ThrowsArgumentException(Of EnvDTE.vsCMAccess)())
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess1() As Task
            Dim code =
<Code>
Class C
    Dim $$i As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Public i As Integer
End Class
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess2() As Task
            Dim code =
<Code>
Class C
    Public $$i As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim i As Integer
End Class
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessDefault)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess3() As Task
            Dim code =
<Code>
Class C
    Private $$i As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim i As Integer
End Class
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessDefault)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess4() As Task
            Dim code =
<Code>
Class C
    Dim $$i As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim i As Integer
End Class
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessDefault)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess5() As Task
            Dim code =
<Code>
Class C
    Public $$i As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Protected Friend i As Integer
End Class
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess6() As Task
            Dim code =
<Code>
Class C

#Region "Foo"

    Dim x As Integer

#End Region

    Dim $$i As Integer

End Class
</Code>

            Dim expected =
<Code>
Class C

#Region "Foo"

    Dim x As Integer

#End Region

    Public i As Integer

End Class
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess7() As Task
            Dim code =
<Code>
Class C

#Region "Foo"

    Dim x As Integer

#End Region

    Public $$i As Integer

End Class
</Code>

            Dim expected =
<Code>
Class C

#Region "Foo"

    Dim x As Integer

#End Region

    Protected Friend i As Integer

End Class
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess8() As Task
            Dim code =
<Code>
Class C

#Region "Foo"

    Dim x As Integer

#End Region

    Public $$i As Integer

End Class
</Code>

            Dim expected =
<Code>
Class C

#Region "Foo"

    Dim x As Integer

#End Region

    Dim i As Integer

End Class
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessDefault)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess9() As Task
            Dim code =
<Code>
Class C

#Region "Foo"

    Dim $$x As Integer

#End Region

End Class
</Code>

            Dim expected =
<Code>
Class C

#Region "Foo"

    Public x As Integer

#End Region

End Class
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess10() As Task
            Dim code =
<Code>
Class C

#Region "Foo"

    Public $$x As Integer

#End Region

End Class
</Code>

            Dim expected =
<Code>
Class C

#Region "Foo"

    Dim x As Integer

#End Region

End Class
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessDefault)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess11() As Task
            Dim code =
<Code>
Class C

#Region "Foo"

    Public $$x As Integer

#End Region

End Class
</Code>

            Dim expected =
<Code>
Class C

#Region "Foo"

    Protected Friend x As Integer

#End Region

End Class
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess12() As Task
            Dim code =
<Code><![CDATA[
Class C

#Region "Foo"

    <Bar>
    Public $$x As Integer

#End Region

End Class
]]></Code>

            Dim expected =
<Code><![CDATA[
Class C

#Region "Foo"

    <Bar>
    Protected Friend x As Integer

#End Region

End Class
]]></Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess13() As Task
            Dim code =
<Code>
Class C

#Region "Foo"

    ' Comment comment comment
    Public $$x As Integer

#End Region

End Class
</Code>

            Dim expected =
<Code>
Class C

#Region "Foo"

    ' Comment comment comment
    Protected Friend x As Integer

#End Region

End Class
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess14() As Task
            Dim code =
<Code><![CDATA[
Class C

#Region "Foo"

    ''' <summary>
    ''' Comment comment comment
    ''' </summary>
    Public $$x As Integer

#End Region

End Class
]]></Code>

            Dim expected =
<Code><![CDATA[
Class C

#Region "Foo"

    ''' <summary>
    ''' Comment comment comment
    ''' </summary>
    Protected Friend x As Integer

#End Region

End Class
]]></Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess15() As Task
            Dim code =
<Code>
Class C

    Dim $$x As Integer

End Class
</Code>

            Dim expected =
<Code>
Class C

    Private WithEvents x As Integer

End Class
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPrivate Or EnvDTE.vsCMAccess.vsCMAccessWithEvents)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess16() As Task
            Dim code =
<Code>
Class C

    Private WithEvents $$x As Integer

End Class
</Code>

            Dim expected =
<Code>
Class C

    Dim x As Integer

End Class
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessDefault)
        End Function

#End Region

#Region "Set ConstKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetConstKind1() As Task
            Dim code =
<Code>
Enum
    $$Foo
End Enum
</Code>

            Dim expected =
<Code>
Enum
    Foo
End Enum
</Code>

            Await TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindNone, ThrowsNotImplementedException(Of EnvDTE80.vsCMConstKind))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetConstKind2() As Task
            Dim code =
<Code>
Enum
    $$Foo
End Enum
</Code>

            Dim expected =
<Code>
Enum
    Foo
End Enum
</Code>

            Await TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindReadOnly, ThrowsNotImplementedException(Of EnvDTE80.vsCMConstKind))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetConstKind3() As Task
            Dim code =
<Code>
Enum
    $$Foo
End Enum
</Code>

            Dim expected =
<Code>
Enum
    Foo
End Enum
</Code>

            Await TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindConst, ThrowsNotImplementedException(Of EnvDTE80.vsCMConstKind))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetConstKind4() As Task
            Dim code =
<Code>
Class C
    Dim $$x As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim x As Integer
End Class
</Code>

            Await TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindNone)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetConstKind5() As Task
            Dim code =
<Code>
Class C
    Shared $$x As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Shared x As Integer
End Class
</Code>

            Await TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindNone)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetConstKind6() As Task
            Dim code =
<Code>
Class C
    Dim $$x As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Const x As Integer
End Class
</Code>

            Await TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindConst)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetConstKind7() As Task
            Dim code =
<Code>
Class C
    Const $$x As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim x As Integer
End Class
</Code>

            Await TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindNone)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetConstKind8() As Task
            Dim code =
<Code>
Class C
    Dim $$x As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    ReadOnly x As Integer
End Class
</Code>

            Await TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindReadOnly)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetConstKind9() As Task
            Dim code =
<Code>
Class C
    ReadOnly $$x As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim x As Integer
End Class
</Code>

            Await TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindNone)
        End Function

#End Region

#Region "Set InitExpression tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetInitExpression1() As Task
            Dim code =
<Code>
Class C
    Dim $$i As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim i As Integer = 42
End Class
</Code>

            Await TestSetInitExpression(code, expected, "42")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetInitExpression2() As Task
            Dim code =
<Code>
Class C
    Dim $$i As Integer, j As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim i As Integer = 42, j As Integer
End Class
</Code>

            Await TestSetInitExpression(code, expected, "42")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetInitExpression3() As Task
            Dim code =
<Code>
Class C
    Dim i As Integer, $$j As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim i As Integer, j As Integer = 42
End Class
</Code>

            Await TestSetInitExpression(code, expected, "42")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetInitExpression4() As Task
            ' The result below is a bit silly, but that's what the legacy Code Model does.

            Dim code =
<Code>
Class C
    Dim $$o As New Object
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim o As New Object = 42
End Class
</Code>

            Await TestSetInitExpression(code, expected, "42")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetInitExpression5() As Task
            Dim code =
<Code>
Class C
    Const $$i As Integer = 0
End Class
</Code>

            Dim expected =
<Code>
Class C
    Const i As Integer = 19 + 23
End Class
</Code>

            Await TestSetInitExpression(code, expected, "19 + 23")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetInitExpression6() As Task
            Dim code =
<Code>
Class C
    Const $$i As Integer = 0
End Class
</Code>

            Dim expected =
<Code>
Class C
    Const i As Integer
End Class
</Code>

            Await TestSetInitExpression(code, expected, "")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetInitExpression7() As Task
            Dim code =
<Code>
Enum E
    $$Foo
End Enum
</Code>

            Dim expected =
<Code>
Enum E
    Foo = 42
End Enum
</Code>

            Await TestSetInitExpression(code, expected, "42")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetInitExpression8() As Task
            Dim code =
<Code>
Enum E
    $$Foo = 0
End Enum
</Code>

            Dim expected =
<Code>
Enum E
    Foo = 42
End Enum
</Code>

            Await TestSetInitExpression(code, expected, "42")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetInitExpression9() As Task
            Dim code =
<Code>
Enum E
    $$Foo = 0
End Enum
</Code>

            Dim expected =
<Code>
Enum E
    Foo
End Enum
</Code>

            Await TestSetInitExpression(code, expected, Nothing)
        End Function

#End Region

#Region "Set IsConstant tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsConstant1() As Task
            Dim code =
<Code>
Class C
    Dim $$i As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Const i As Integer
End Class
</Code>

            Await TestSetIsConstant(code, expected, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsConstant2() As Task
            Dim code =
<Code>
Class C
    Const $$i As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim i As Integer
End Class
</Code>

            Await TestSetIsConstant(code, expected, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsConstant3() As Task
            Dim code =
<Code>
Class C
    ReadOnly $$i As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Const i As Integer
End Class
</Code>

            Await TestSetIsConstant(code, expected, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsConstant4() As Task
            Dim code =
<Code>
Class C
    ReadOnly $$i As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim i As Integer
End Class
</Code>

            Await TestSetIsConstant(code, expected, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsConstant5() As Task
            Dim code =
<Code>
Module C
    Dim $$i As Integer
End Module
</Code>

            Dim expected =
<Code>
Module C
    Const i As Integer
End Module
</Code>

            Await TestSetIsConstant(code, expected, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsConstant6() As Task
            Dim code =
<Code>
Enum E
    $$Foo
End Enum
</Code>

            Dim expected =
<Code>
Enum E
    Foo
End Enum
</Code>

            Await TestSetIsConstant(code, expected, True, ThrowsNotImplementedException(Of Boolean))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsConstant7() As Task
            Dim code =
<Code>
Enum E
    $$Foo
End Enum
</Code>

            Dim expected =
<Code>
Enum E
    Foo
End Enum
</Code>

            Await TestSetIsConstant(code, expected, False, ThrowsNotImplementedException(Of Boolean))
        End Function

#End Region

#Region "Set IsShared tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsShared1() As Task
            Dim code =
<Code>
Class C
    Dim $$i As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Shared i As Integer
End Class
</Code>

            Await TestSetIsShared(code, expected, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsShared2() As Task
            Dim code =
<Code>
Class C
    Shared $$i As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim i As Integer
End Class
</Code>

            Await TestSetIsShared(code, expected, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsShared3() As Task
            Dim code =
<Code>
Class C
    Private $$i As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Private Shared i As Integer
End Class
</Code>

            Await TestSetIsShared(code, expected, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsShared4() As Task
            Dim code =
<Code>
Class C
    Private Shared $$i As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Private i As Integer
End Class
</Code>

            Await TestSetIsShared(code, expected, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsShared5() As Task
            Dim code =
<Code>
Module C
    Dim $$i As Integer
End Module
</Code>

            Dim expected =
<Code>
Module C
    Dim i As Integer
End Module
</Code>

            Await TestSetIsShared(code, expected, True, ThrowsNotImplementedException(Of Boolean))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsShared6() As Task
            Dim code =
<Code>
Enum E
    $$Foo
End Enum
</Code>

            Dim expected =
<Code>
Enum E
    Foo
End Enum
</Code>

            Await TestSetIsShared(code, expected, True, ThrowsNotImplementedException(Of Boolean))
        End Function

#End Region

#Region "Set Name tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetName1() As Task
            Dim code =
<Code>
Class C
    Dim $$Foo As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim Bar As Integer
End Class
</Code>

            Await TestSetName(code, expected, "Bar", NoThrow(Of String)())
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetName2() As Task
            Dim code =
<Code>
Class C

#Region "Foo"

    Dim $$Foo As Integer

#End Region

End Class
</Code>

            Dim expected =
<Code>
Class C

#Region "Foo"

    Dim Bar As Integer

#End Region

End Class
</Code>

            Await TestSetName(code, expected, "Bar", NoThrow(Of String)())
        End Function

#End Region

#Region "Set Type tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType1() As Task
            Dim code =
<Code>
Class C
    Dim $$a As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim a As Double
End Class
</Code>

            Await TestSetTypeProp(code, expected, "double")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType2() As Task
            Dim code =
<Code>
Class C
    Dim $$a, b As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim a, b As Double
End Class
</Code>

            Await TestSetTypeProp(code, expected, "double")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType3() As Task
            Dim code =
<Code>
Class C
    Private $$a As New Object
End Class
</Code>

            Dim expected =
<Code>
Class C
    Private a As New String
End Class
</Code>

            Await TestSetTypeProp(code, expected, "String")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType4() As Task
            Dim code =
<Code>
Class C
    Private $$a As New Object, x As Integer = 0
End Class
</Code>

            Dim expected =
<Code>
Class C
    Private a As New String, x As Integer = 0
End Class
</Code>

            Await TestSetTypeProp(code, expected, "String")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType5() As Task
            Dim code =
<Code>
Class C
    Private a As New Object, x$$ As Integer = 0
End Class
</Code>

            Dim expected =
<Code>
Class C
    Private a As New Object, x As String = 0
End Class
</Code>

            Await TestSetTypeProp(code, expected, "String")
        End Function

#End Region

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property
    End Class
End Namespace
