' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.VisualBasic
    Public Class CodeAttributeTests
        Inherits AbstractCodeAttributeTests

#Region "GetStartPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint1() As Task
            Dim code =
<Code>Imports System

&lt;$$Serializable()&gt;
Class C
End Class
</Code>

            Await TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=2, absoluteOffset:=18, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=2, absoluteOffset:=18, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=2, absoluteOffset:=18, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=2, absoluteOffset:=18, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=2, absoluteOffset:=18, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=2, absoluteOffset:=18, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=2, absoluteOffset:=18, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=2, absoluteOffset:=18, lineLength:=16)))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint2() As Task
            Dim code =
<Code>Imports System

&lt;$$CLSCompliant(True)&gt;
Class C
End Class
</Code>

            Await TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=2, absoluteOffset:=18, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=2, absoluteOffset:=18, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=2, absoluteOffset:=18, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=2, absoluteOffset:=18, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=2, absoluteOffset:=18, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=2, absoluteOffset:=18, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=2, absoluteOffset:=18, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=2, absoluteOffset:=18, lineLength:=20)))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint3() As Task
            Dim code =
<Code>
&lt;$$Assembly: CLSCompliant(True)&gt;
</Code>

            Await TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=1, lineOffset:=2, absoluteOffset:=2, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=1, lineOffset:=2, absoluteOffset:=2, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=1, lineOffset:=2, absoluteOffset:=2, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=1, lineOffset:=2, absoluteOffset:=2, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=1, lineOffset:=12, absoluteOffset:=12, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=1, lineOffset:=2, absoluteOffset:=2, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=1, lineOffset:=2, absoluteOffset:=2, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=1, lineOffset:=2, absoluteOffset:=2, lineLength:=30)))
        End Function

#End Region

#Region "GetEndPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint1() As Task
            Dim code =
<Code>Imports System

&lt;$$Serializable()&gt;
Class C
End Class
</Code>

            Await TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=16, absoluteOffset:=32, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=16, absoluteOffset:=32, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=16, absoluteOffset:=32, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=16, absoluteOffset:=32, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=14, absoluteOffset:=30, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=16, absoluteOffset:=32, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=16, absoluteOffset:=32, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=16, absoluteOffset:=32, lineLength:=16)))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint2() As Task
            Dim code =
<Code>Imports System

&lt;$$CLSCompliant(True)&gt;
Class C
End Class
</Code>

            Await TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=20, absoluteOffset:=36, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=20, absoluteOffset:=36, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=20, absoluteOffset:=36, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=20, absoluteOffset:=36, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=14, absoluteOffset:=30, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=20, absoluteOffset:=36, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=20, absoluteOffset:=36, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=20, absoluteOffset:=36, lineLength:=20)))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint3() As Task
            Dim code =
<Code>
&lt;$$Assembly: CLSCompliant(True)&gt;
</Code>

            Await TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=1, lineOffset:=30, absoluteOffset:=30, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=1, lineOffset:=30, absoluteOffset:=30, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=1, lineOffset:=30, absoluteOffset:=30, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=1, lineOffset:=30, absoluteOffset:=30, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=1, lineOffset:=24, absoluteOffset:=24, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=1, lineOffset:=30, absoluteOffset:=30, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=1, lineOffset:=30, absoluteOffset:=30, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=1, lineOffset:=30, absoluteOffset:=30, lineLength:=30)))
        End Function

#End Region

#Region "AttributeArgument GetStartPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetAttributeArgumentStartPoint1() As Task
            Dim code =
<Code>
Imports System

&lt;$$Assembly: Foo(0,,Z:=42)&gt;

Class FooAttribute
    Inherits Attribute

    Sub New(x As Integer, Optional y As Integer = 0)

    End Sub

    Property Z As integer
End Class
</Code>

            Await TestAttributeArgumentStartPoint(code, 1,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=16, absoluteOffset:=32, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=16, absoluteOffset:=32, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=16, absoluteOffset:=32, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=16, absoluteOffset:=32, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=16, absoluteOffset:=32, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=16, absoluteOffset:=32, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=16, absoluteOffset:=32, lineLength:=25)))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetAttributeArgumentStartPoint2() As Task
            Dim code =
<Code>
Imports System

&lt;$$Assembly: Foo(0,,Z:=42)&gt;

Class FooAttribute
    Inherits Attribute

    Sub New(x As Integer, Optional y As Integer = 0)

    End Sub

    Property Z As integer
End Class
</Code>

            Await TestAttributeArgumentStartPoint(code, 2,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=17, absoluteOffset:=33, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=17, absoluteOffset:=33, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=17, absoluteOffset:=33, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=17, absoluteOffset:=33, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=17, absoluteOffset:=33, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=17, absoluteOffset:=33, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=17, absoluteOffset:=33, lineLength:=25)))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetAttributeArgumentStartPoint3() As Task
            Dim code =
<Code>
Imports System

&lt;$$Assembly: Foo(,Z:=42)&gt;

Class FooAttribute
    Inherits Attribute

    Sub New(Optional y As Integer = 0)

    End Sub

    Property Z As integer
End Class
</Code>

            Await TestAttributeArgumentStartPoint(code, 1,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=15, absoluteOffset:=31, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=15, absoluteOffset:=31, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=15, absoluteOffset:=31, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=15, absoluteOffset:=31, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=15, absoluteOffset:=31, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=15, absoluteOffset:=31, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=15, absoluteOffset:=31, lineLength:=23)))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetAttributeArgumentStartPoint4() As Task
            Dim code =
<Code>
Imports System

&lt;$$Assembly: Foo(,Z:=42)&gt;

Class FooAttribute
    Inherits Attribute

    Sub New(Optional y As Integer = 0)

    End Sub

    Property Z As integer
End Class
</Code>

            Await TestAttributeArgumentStartPoint(code, 2,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=17, absoluteOffset:=33, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=17, absoluteOffset:=33, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=17, absoluteOffset:=33, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=17, absoluteOffset:=33, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=17, absoluteOffset:=33, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=17, absoluteOffset:=33, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=17, absoluteOffset:=33, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=17, absoluteOffset:=33, lineLength:=23)))
        End Function

#End Region

#Region "AttributeArgument GetEndPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetAttributeArgumentEndPoint1() As Task
            Dim code =
<Code>
Imports System

&lt;$$Assembly: Foo(0,,Z:=42)&gt;

Class FooAttribute
    Inherits Attribute

    Sub New(x As Integer, Optional y As Integer = 0)

    End Sub

    Property Z As integer
End Class
</Code>

            Await TestAttributeArgumentEndPoint(code, 1,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=17, absoluteOffset:=33, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=17, absoluteOffset:=33, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=17, absoluteOffset:=33, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=17, absoluteOffset:=33, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=17, absoluteOffset:=33, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=17, absoluteOffset:=33, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=17, absoluteOffset:=33, lineLength:=25)))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetAttributeArgumentEndPoint2() As Task
            Dim code =
<Code>
Imports System

&lt;$$Assembly: Foo(0,,Z:=42)&gt;

Class FooAttribute
    Inherits Attribute

    Sub New(x As Integer, Optional y As Integer = 0)

    End Sub

    Property Z As integer
End Class
</Code>

            Await TestAttributeArgumentEndPoint(code, 2,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=34, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=34, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=34, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=34, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=34, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=34, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=34, lineLength:=25)))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetAttributeArgumentEndPoint3() As Task
            Dim code =
<Code>
Imports System

&lt;$$Assembly: Foo(,Z:=42)&gt;

Class FooAttribute
    Inherits Attribute

    Sub New(Optional y As Integer = 0)

    End Sub

    Property Z As integer
End Class
</Code>

            Await TestAttributeArgumentEndPoint(code, 1,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=16, absoluteOffset:=32, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=16, absoluteOffset:=32, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=16, absoluteOffset:=32, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=16, absoluteOffset:=32, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=16, absoluteOffset:=32, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=16, absoluteOffset:=32, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=16, absoluteOffset:=32, lineLength:=23)))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetAttributeArgumentEndPoint4() As Task
            Dim code =
<Code>
Imports System

&lt;$$Assembly: Foo(,Z:=42)&gt;

Class FooAttribute
    Inherits Attribute

    Sub New(Optional y As Integer = 0)

    End Sub

    Property Z As integer
End Class
</Code>

            Await TestAttributeArgumentEndPoint(code, 2,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=22, absoluteOffset:=38, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=22, absoluteOffset:=38, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=22, absoluteOffset:=38, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=22, absoluteOffset:=38, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=34, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=22, absoluteOffset:=38, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=22, absoluteOffset:=38, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=22, absoluteOffset:=38, lineLength:=23)))
        End Function

#End Region

#Region "FullName tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetFullName1() As Task
            Dim code =
<Code>
Imports System

&lt;$$Serializable&gt;
Class C
End Class
</Code>

            Await TestFullName(code, "System.SerializableAttribute")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetFullName2() As Task
            Dim code =
<Code>
&lt;$$System.Serializable&gt;
Class C
End Class
</Code>

            Await TestFullName(code, "System.SerializableAttribute")
        End Function

#End Region

#Region "Parent tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetParent1() As Task
            Dim code =
<Code>
Imports System

&lt;$$Serializable&gt;
Class C
End Class
</Code>

            Await TestParent(code, IsElement("C", kind:=EnvDTE.vsCMElement.vsCMElementClass))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetParent2() As Task
            Dim code =
<Code>
Imports System

&lt;Serializable, $$CLSCompliant(False)&gt;
Class C
End Class
</Code>

            Await TestParent(code, IsElement("C", kind:=EnvDTE.vsCMElement.vsCMElementClass))
        End Function
#End Region

#Region "Attribute argument tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetArguments1() As Task
            Dim code =
<Code>
Imports System

&lt;$$Serializable&gt;
Class C
End Class
</Code>

            Await TestAttributeArguments(code, NoElements)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetArguments2() As Task
            Dim code =
<Code>
Imports System

&lt;$$Serializable()&gt;
Class C
End Class
</Code>

            Await TestAttributeArguments(code, NoElements)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetArguments3() As Task
            Dim code =
<Code>
Imports System

&lt;$$CLSCompliant(True)&gt;
Class C
End Class
</Code>

            Await TestAttributeArguments(code, IsAttributeArgument(value:="True"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetArguments4() As Task
            Dim code =
<Code>
Imports System

&lt;$$AttributeUsage(AttributeTargets.All, AllowMultiple := False)&gt;
Class CAttribute
    Inherits Attribute
End Class
</Code>

            Await TestAttributeArguments(code, IsAttributeArgument(value:="AttributeTargets.All"), IsAttributeArgument(name:="AllowMultiple", value:="False"))

        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetArguments5_Omitted() As Task
            Dim code =
<Code>
&lt;$$Foo(, Baz:=True)&gt;
Class FooAttribute
    Inherits Attribute

    Sub New(Optional bar As String = Nothing)

    End Sub

    Public Property Baz As Boolean
        Get

        End Get
        Set(value As Boolean)

        End Set
    End Property
End Class
</Code>

            Await TestAttributeArguments(code, IsAttributeArgument(name:=""), IsAttributeArgument(name:="Baz", value:="True"))

        End Function

#End Region

#Region "Target tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetTarget1() As Task
            Dim code =
<Code>
Imports System

&lt;Assembly: CLSCompliant$$(False)&gt;
</Code>

            Await TestTarget(code, "Assembly")
        End Function
#End Region

#Region "Value tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetValue1() As Task
            Dim code =
<Code>
Imports System

&lt;$$Serializable&gt;
Class C
End Class
</Code>

            Await TestValue(code, "")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetValue2() As Task
            Dim code =
<Code>
Imports System

&lt;$$Serializable()&gt;
Class C
End Class
</Code>

            Await TestValue(code, "")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetValue3() As Task
            Dim code =
<Code>
Imports System

&lt;$$CLSCompliant(False)&gt;
Class C
End Class
</Code>

            Await TestValue(code, "False")

        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetValue4() As Task
            Dim code =
<Code>
Imports System

&lt;$$AttributeUsage(AttributeTargets.All, AllowMultiple = False)&gt;
Class CAttribute
    Inherits Attribute
End Class
</Code>

            Await TestValue(code, "AttributeTargets.All, AllowMultiple = False")
        End Function
#End Region

#Region "AddAttributeArgument tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttributeArgument1() As Task
            Dim code =
<Code>
Imports System

&lt;$$CLSCompliant&gt;
Class C
End Class
</Code>

            Dim expectedCode =
<Code>
Imports System

&lt;CLSCompliant(True)&gt;
Class C
End Class
</Code>

            Await TestAddAttributeArgument(code, expectedCode, New AttributeArgumentData With {.Value = "True"})

        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttributeArgument2() As Task
            Dim code =
<Code>
Imports System

&lt;$$CLSCompliant()&gt;
Class C
End Class
</Code>

            Dim expectedCode =
<Code>
Imports System

&lt;CLSCompliant(True)&gt;
Class C
End Class
</Code>

            Await TestAddAttributeArgument(code, expectedCode, New AttributeArgumentData With {.Value = "True"})

        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddArgument3() As Task
            Dim code =
<Code>
Imports System

&lt;$$AttributeUsage(AttributeTargets.All)&gt;
Class CAttribute
    Inherits Attribute
End Class
</Code>

            Dim expectedCode =
<Code>
Imports System

&lt;AttributeUsage(AttributeTargets.All, AllowMultiple:=False)&gt;
Class CAttribute
    Inherits Attribute
End Class
</Code>

            Await TestAddAttributeArgument(code, expectedCode, New AttributeArgumentData With {.Name = "AllowMultiple", .Value = "False", .Position = 1})

        End Function
#End Region

#Region "Delete tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDelete1() As Task
            Dim code =
<Code><![CDATA[
<$$Foo>
Class C
End Class
]]></Code>

            Dim expected =
<Code><![CDATA[
Class C
End Class
]]></Code>

            Await TestDelete(code, expected)

        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDelete2() As Task
            Dim code =
<Code><![CDATA[
<$$Foo, Bar>
Class C
End Class
]]></Code>

            Dim expected =
<Code><![CDATA[
<Bar>
Class C
End Class
]]></Code>

            Await TestDelete(code, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDelete3() As Task
            Dim code =
<Code><![CDATA[
<Foo>
<$$Bar>
Class C
End Class
]]></Code>

            Dim expected =
<Code><![CDATA[
<Foo>
Class C
End Class
]]></Code>

            Await TestDelete(code, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDelete4() As Task
            Dim code =
<Code><![CDATA[
<Assembly: $$Foo>
]]></Code>

            Dim expected =
<Code><![CDATA[
]]></Code>

            Await TestDelete(code, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDelete5() As Task
            Dim code =
<Code><![CDATA[
<Assembly: $$Foo, Assembly: Bar>
]]></Code>

            Dim expected =
<Code><![CDATA[
<Assembly: Bar>
]]></Code>

            Await TestDelete(code, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDelete6() As Task
            Dim code =
<Code><![CDATA[
<Assembly: Foo>
<Assembly: $$Bar>
]]></Code>

            Dim expected =
<Code><![CDATA[
<Assembly: Foo>
]]></Code>

            Await TestDelete(code, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDelete7() As Task
            Dim code =
<Code><![CDATA[
''' <summary>
''' Doc comment
''' </summary>
<$$Foo>
Class C
End Class
]]></Code>

            Dim expected =
<Code><![CDATA[
''' <summary>
''' Doc comment
''' </summary>
Class C
End Class
]]></Code>

            Await TestDelete(code, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDelete8() As Task
            Dim code =
<Code><![CDATA[
<$$Foo> ' Comment comment comment
Class C
End Class
]]></Code>

            Dim expected =
<Code><![CDATA[
Class C
End Class
]]></Code>

            Await TestDelete(code, expected)
        End Function

#End Region

#Region "Delete attribute argument tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDeleteAttributeArgument1() As Task
            Dim code =
<Code><![CDATA[
<$$System.CLSCompliant(True)>
Class C
End Class
]]></Code>

            Dim expected =
<Code><![CDATA[
<System.CLSCompliant()>
Class C
End Class
]]></Code>

            Await TestDeleteAttributeArgument(code, expected, 1)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDeleteAttributeArgument2() As Task
            Dim code =
<Code><![CDATA[
<$$AttributeUsage(AttributeTargets.All, AllowMultiple:=False)>
Class CAttribute
    Inherits Attribute
End Class
]]></Code>

            Dim expected =
<Code><![CDATA[
<AttributeUsage(AllowMultiple:=False)>
Class CAttribute
    Inherits Attribute
End Class
]]></Code>

            Await TestDeleteAttributeArgument(code, expected, 1)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDeleteAttributeArgument3() As Task
            Dim code =
<Code><![CDATA[
<$$AttributeUsage(AttributeTargets.All, AllowMultiple:=False)>
Class CAttribute
    Inherits Attribute
End Class
]]></Code>

            Dim expected =
<Code><![CDATA[
<AttributeUsage(AttributeTargets.All)>
Class CAttribute
    Inherits Attribute
End Class
]]></Code>

            Await TestDeleteAttributeArgument(code, expected, 2)
        End Function

#End Region

#Region "Set Name tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetName1() As Task
            Dim code =
<Code><![CDATA[
<$$Foo()>
Class C
End Class
]]></Code>

            Dim expected =
<Code><![CDATA[
<Bar()>
Class C
End Class
]]></Code>

            Await TestSetName(code, expected, "Bar", NoThrow(Of String)())
        End Function
#End Region

#Region "Set Target tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetTarget1() As Task
            Dim code =
<Code>
Imports System

&lt;Assembly: CLSCompliant$$(False)&gt;
</Code>

            Dim expected =
<Code>
Imports System

&lt;Module: CLSCompliant(False)&gt;
</Code>

            Await TestSetTarget(code, expected, "Module")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetTarget2() As Task
            Dim code =
<Code>
Imports System

&lt;CLSCompliant$$(False)&gt;
Class C
End Class
</Code>

            Dim expected =
<Code>
Imports System

&lt;Assembly: CLSCompliant(False)&gt;
Class C
End Class
</Code>

            Await TestSetTarget(code, expected, "Assembly")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetTarget3() As Task
            Dim code =
<Code>
Imports System

&lt;Assembly: CLSCompliant$$(False)&gt;
</Code>

            Dim expected =
<Code>
Imports System

&lt;CLSCompliant(False)&gt;
</Code>

            Await TestSetTarget(code, expected, "")
        End Function
#End Region

#Region "Set Value tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetValue1() As Task
            Dim code =
<Code>
Imports System

&lt;Assembly: CLSCompliant$$(False)&gt;
</Code>

            Dim expected =
<Code>
Imports System

&lt;Assembly: CLSCompliant(True)&gt;
</Code>

            Await TestSetValue(code, expected, "True")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetValue2() As Task
            Dim code =
<Code>
Imports System

&lt;Assembly: CLSCompliant$$()&gt;
</Code>

            Dim expected =
<Code>
Imports System

&lt;Assembly: CLSCompliant(True)&gt;
</Code>

            Await TestSetValue(code, expected, "True")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetValue3() As Task
            Dim code =
<Code>
Imports System

&lt;Assembly: CLSCompliant$$&gt;
</Code>

            Dim expected =
<Code>
Imports System

&lt;Assembly: CLSCompliant(True)&gt;
</Code>

            Await TestSetValue(code, expected, "True")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetValue4() As Task
            Dim code =
<Code>
Imports System

&lt;Assembly: CLSCompliant$$(False)&gt;
</Code>

            Dim expected =
<Code>
Imports System

&lt;Assembly: CLSCompliant()&gt;
</Code>

            Await TestSetValue(code, expected, "")
        End Function
#End Region

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property
    End Class
End Namespace


