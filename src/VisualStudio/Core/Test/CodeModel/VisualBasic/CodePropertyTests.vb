' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel.Extenders
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel.Interop
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.VisualBasic
    Public Class CodePropertyTests
        Inherits AbstractCodePropertyTests

#Region "GetStartPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint1() As Task
            Dim code =
<Code>
Class C
    Public Property $$P As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            Await TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=1, absoluteOffset:=42, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=1, absoluteOffset:=42, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=2, lineOffset:=21, absoluteOffset:=29, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=1, absoluteOffset:=42, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=32)))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint_Attribute() As Task
            Dim code =
<Code>
Class C
    &lt;System.CLSCompliant(True)&gt;
    Public Property $$P As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            Await TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=4, lineOffset:=1, absoluteOffset:=74, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=4, lineOffset:=1, absoluteOffset:=74, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=45, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=21, absoluteOffset:=61, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=4, lineOffset:=1, absoluteOffset:=74, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=45, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=31)))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint_AutoProperty() As Task
            Dim code =
<Code>
Class C
    Public Property $$P As Integer
End Class
</Code>

            Await TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=1, absoluteOffset:=42, lineLength:=9)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=1, absoluteOffset:=42, lineLength:=9)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=2, lineOffset:=21, absoluteOffset:=29, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=1, absoluteOffset:=42, lineLength:=9)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=32)))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint_AutoProperty_Attribute() As Task
            Dim code =
<Code>
Class C
    &lt;System.CLSCompliant(True)&gt;
    Public Property $$P As Integer
End Class
</Code>

            Await TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=4, lineOffset:=1, absoluteOffset:=74, lineLength:=9)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=4, lineOffset:=1, absoluteOffset:=74, lineLength:=9)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=45, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=21, absoluteOffset:=61, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=4, lineOffset:=1, absoluteOffset:=74, lineLength:=9)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=45, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=31)))
        End Function

#End Region

#Region "GetEndPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint1() As Task
            Dim code =
<Code>
Class C
    Public Property $$P As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            Await TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=7, lineOffset:=5, absoluteOffset:=120, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=7, lineOffset:=5, absoluteOffset:=120, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=2, lineOffset:=33, absoluteOffset:=41, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=2, lineOffset:=33, absoluteOffset:=41, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=2, lineOffset:=22, absoluteOffset:=30, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=7, lineOffset:=5, absoluteOffset:=120, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=7, lineOffset:=17, absoluteOffset:=132, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=7, lineOffset:=17, absoluteOffset:=132, lineLength:=16)))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint_Attribute() As Task
            Dim code =
<Code>
Class C
    &lt;System.CLSCompliant(True)&gt;
    Public Property $$P As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            Await TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     TextPoint(line:=2, lineOffset:=32, absoluteOffset:=40, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=2, lineOffset:=32, absoluteOffset:=40, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=8, lineOffset:=5, absoluteOffset:=152, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=8, lineOffset:=5, absoluteOffset:=152, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=33, absoluteOffset:=73, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=33, absoluteOffset:=73, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=22, absoluteOffset:=62, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=8, lineOffset:=5, absoluteOffset:=152, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=8, lineOffset:=17, absoluteOffset:=164, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=8, lineOffset:=17, absoluteOffset:=164, lineLength:=16)))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint_AutoProperty() As Task
            Dim code =
<Code>
Class C
    Public Property $$P As Integer
End Class
</Code>

            Await TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=7)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=7)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=2, lineOffset:=33, absoluteOffset:=41, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=2, lineOffset:=33, absoluteOffset:=41, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=2, lineOffset:=22, absoluteOffset:=30, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=7)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=2, lineOffset:=33, absoluteOffset:=41, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=2, lineOffset:=33, absoluteOffset:=41, lineLength:=32)))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint_AutoProperty_Attribute() As Task
            Dim code =
<Code>
Class C
    &lt;System.CLSCompliant(True)&gt;
    Public Property $$P As Integer
End Class
</Code>

            Await TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     TextPoint(line:=2, lineOffset:=32, absoluteOffset:=40, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=2, lineOffset:=32, absoluteOffset:=40, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=7)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=7)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=33, absoluteOffset:=73, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=33, absoluteOffset:=73, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=22, absoluteOffset:=62, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=7)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=33, absoluteOffset:=73, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=33, absoluteOffset:=73, lineLength:=32)))
        End Function

#End Region

#Region "Attributes"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAttributes_AutoProperty() As Task
            Dim code =
<Code>
Imports System

Class C1
    &lt;CLSCompliant(False)&gt;
    Public Property $$P As String
End Class
</Code>

            Await TestAttributes(code, IsElement("CLSCompliant"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAttributes_Property() As Task
            Dim code =
<Code>
Imports System

Class C1
    &lt;CLSCompliant(False)&gt;
    Public Property $$P As String
        Get
        End Get
        Set(value As String)
        End Set
    End Property
End Class
</Code>

            Await TestAttributes(code, IsElement("CLSCompliant"))
        End Function

#End Region

#Region "Getter tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetterIsNothingForAutoProp() As Task
            Dim code =
<Code>
Class C
    Public Property $$P As Integer
End Class
</Code>

            Await TestGetter(code,
                       Sub(accessor)
                           Assert.Null(accessor)
                       End Sub)
        End Function

#End Region

#Region "IsDefault tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestIsDefault1() As Task
            Dim code =
<Code>
Class C
    Public Default Property $$P(index as Integer) As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            Await TestIsDefault(code, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestIsDefault2() As Task
            Dim code =
<Code>
Class C
    Public Property $$P As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            Await TestIsDefault(code, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestIsDefault3() As Task
            Dim code =
<Code>
Class C
    Public Property $$P As Integer
End Class
</Code>

            Await TestIsDefault(code, False)
        End Function

#End Region

#Region "Name tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName1() As Task
            Dim code =
<Code>
Class C
    Public Property $$P As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            Await TestName(code, "P")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName2() As Task
            Dim code =
<Code>
Class C
    Public Property $$P As Integer
End Class
</Code>

            Await TestName(code, "P")
        End Function

#End Region

#Region "OverrideKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestOverrideKind1() As Task
            Dim code =
<Code>
Class C
    Public Property $$P As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            Await TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindNone)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestOverrideKind2() As Task
            Dim code =
<Code>
Class C
    Public MustOverride Property $$P As Integer
End Class
</Code>

            Await TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindAbstract)
        End Function

#End Region

#Region "Prototype tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_UniqueSignature() As Task
            Dim code =
<Code>
Namespace N
    Class C
        Property P$$ As Integer = 42
    End Class
End Namespace
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeUniqueSignature, "P:N.C.P")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_FullName() As Task
            Dim code =
<Code>
Namespace N
    Class C
        Property P$$ As Integer = 42
    End Class
End Namespace
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeFullname, "N.C.P()")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_ClassName1() As Task
            Dim code =
<Code>
Namespace N
    Class C
        Property P$$ As Integer = 42
    End Class
End Namespace
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName, "C.P()")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_ClassName2() As Task
            Dim code =
<Code>
Namespace N
    Class C
        Property P$$ As Integer
            Get
            End Get
            Set(value As Integer)
            End Set
        End Property
    End Class
End Namespace
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName, "C.P()")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_ClassName3() As Task
            Dim code =
<Code>
Namespace N
    Class C
        Property P$$(Optional index As String = "") As Integer
            Get
            End Get
            Set(value As Integer)
            End Set
        End Property
    End Class
End Namespace
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName, "C.P()")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_ClassName_ParamNames() As Task
            Dim code =
<Code>
Namespace N
    Class C
        Property P$$(Optional index As String = "") As Integer
            Get
            End Get
            Set(value As Integer)
            End Set
        End Property
    End Class
End Namespace
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName Or EnvDTE.vsCMPrototype.vsCMPrototypeParamNames, "C.P(index )")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_ClassName_ParamNames_ParamTypes() As Task
            Dim code =
<Code>
Namespace N
    Class C
        Property P$$(Optional index As String = "") As Integer
            Get
            End Get
            Set(value As Integer)
            End Set
        End Property
    End Class
End Namespace
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName Or EnvDTE.vsCMPrototype.vsCMPrototypeParamNames Or EnvDTE.vsCMPrototype.vsCMPrototypeParamTypes, "C.P(index As String)")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_ClassName_ParamNames_ParamDefaultValues() As Task
            Dim code =
<Code>
Namespace N
    Class C
        Property P$$(Optional index As String = "") As Integer
            Get
            End Get
            Set(value As Integer)
            End Set
        End Property
    End Class
End Namespace
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName Or EnvDTE.vsCMPrototype.vsCMPrototypeParamNames Or EnvDTE.vsCMPrototype.vsCMPrototypeParamDefaultValues, "C.P(index  = """")")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_ClassName_ParamTypes() As Task
            Dim code =
<Code>
Namespace N
    Class C
        Property P$$(Optional index As String = "") As Integer
            Get
            End Get
            Set(value As Integer)
            End Set
        End Property
    End Class
End Namespace
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName Or EnvDTE.vsCMPrototype.vsCMPrototypeParamTypes, "C.P(String)")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_ClassName_ParamTypes_ParamDefaultValues() As Task
            Dim code =
<Code>
Namespace N
    Class C
        Property P$$(Optional index As String = "") As Integer
            Get
            End Get
            Set(value As Integer)
            End Set
        End Property
    End Class
End Namespace
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName Or EnvDTE.vsCMPrototype.vsCMPrototypeParamTypes Or EnvDTE.vsCMPrototype.vsCMPrototypeParamDefaultValues, "C.P(String = """")")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_ClassName_ParamNames_ParamTypes_ParamDefaultValues() As Task
            Dim code =
<Code>
Namespace N
    Class C
        Property P$$(Optional index As String = "") As Integer
            Get
            End Get
            Set(value As Integer)
            End Set
        End Property
    End Class
End Namespace
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName Or EnvDTE.vsCMPrototype.vsCMPrototypeParamNames Or EnvDTE.vsCMPrototype.vsCMPrototypeParamTypes Or EnvDTE.vsCMPrototype.vsCMPrototypeParamDefaultValues, "C.P(index As String = """")")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_Type() As Task
            Dim code =
<Code>
Namespace N
    Class C
        Property P$$(Optional index As String = "") As Integer
            Get
            End Get
            Set(value As Integer)
            End Set
        End Property
    End Class
End Namespace
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeType, "P()")
        End Function

#End Region

#Region "ReadWrite tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestReadWrite1() As Task
            Dim code =
<Code>
Class C
    Public Property $$P As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            Await TestReadWrite(code, EnvDTE80.vsCMPropertyKind.vsCMPropertyKindReadWrite)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestReadWrite2() As Task
            Dim code =
<Code>
Class C
    Public ReadOnly Property $$P As Integer
        Get
        End Get
    End Property
End Class
</Code>

            Await TestReadWrite(code, EnvDTE80.vsCMPropertyKind.vsCMPropertyKindReadOnly)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestReadWrite3() As Task
            Dim code =
<Code>
Class C
    Public WriteOnly Property $$P As Integer
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            Await TestReadWrite(code, EnvDTE80.vsCMPropertyKind.vsCMPropertyKindWriteOnly)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestReadWrite4() As Task
            Dim code =
<Code>
Class C
    Public Property $$P As Integer
End Class
</Code>

            Await TestReadWrite(code, EnvDTE80.vsCMPropertyKind.vsCMPropertyKindReadWrite)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestReadWrite5() As Task
            Dim code =
<Code>
Class C
    Public ReadOnly Property $$P As Integer
End Class
</Code>

            Await TestReadWrite(code, EnvDTE80.vsCMPropertyKind.vsCMPropertyKindReadOnly)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestReadWrite6() As Task
            Dim code =
<Code>
Class C
    Public WriteOnly Property $$P As Integer
End Class
</Code>

            Await TestReadWrite(code, EnvDTE80.vsCMPropertyKind.vsCMPropertyKindWriteOnly)
        End Function

#End Region

#Region "Setter tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetterIsNothingForAutoProp() As Task
            Dim code =
<Code>
Class C
    Public Property $$P As Integer
End Class
</Code>

            Await TestSetter(code,
                       Sub(accessor)
                           Assert.Null(accessor)
                       End Sub)
        End Function

#End Region

#Region "Type tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestType1() As Task
            Dim code =
<Code>
Class C
    Property $$Foo As Integer
End Class
</Code>

            Await TestTypeProp(code,
                         New CodeTypeRefData With {
                             .AsFullName = "System.Int32",
                             .AsString = "Integer",
                             .CodeTypeFullName = "System.Int32",
                             .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefInt
                         })
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestType2() As Task
            Dim code =
<Code>
Class C
    Property $$Foo As New System.Text.StringBuilder
End Class
</Code>

            Await TestTypeProp(code,
                         New CodeTypeRefData With {
                             .AsFullName = "System.Text.StringBuilder",
                             .AsString = "System.Text.StringBuilder",
                             .CodeTypeFullName = "System.Text.StringBuilder",
                             .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefCodeType
                         })
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestType3() As Task
            Dim code =
<Code>
Class C
    Property $$Foo As String
        Get

        End Get
        Set(value As String)

        End Set
    End Property
End Class
</Code>

            Await TestTypeProp(code,
                         New CodeTypeRefData With {
                             .AsFullName = "System.String",
                             .AsString = "String",
                             .CodeTypeFullName = "System.String",
                             .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefString
                         })
        End Function

#End Region

#Region "AddAttribute tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute_NormalProperty() As Task
            Dim code =
<Code>
Imports System

Class C
    Property $$P As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            Dim expected =
<Code>
Imports System

Class C
    &lt;Serializable()&gt;
    Property P As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute_AutoProperty() As Task
            Dim code =
<Code>
Imports System

Class C
    Property $$P As Integer
End Class
</Code>

            Dim expected =
<Code>
Imports System

Class C
    &lt;Serializable()&gt;
    Property P As Integer
End Class
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Function

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute_NormalProperty_BelowDocComment() As Task
            Dim code =
<Code>
Imports System

Class C
    ''' &lt;summary&gt;&lt;/summary&gt;
    Property $$P As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            Dim expected =
<Code>
Imports System

Class C
    ''' &lt;summary&gt;&lt;/summary&gt;
    &lt;Serializable()&gt;
    Property P As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Function

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute_AutoProperty_BelowDocComment() As Task
            Dim code =
<Code>
Imports System

Class C
    ''' &lt;summary&gt;&lt;/summary&gt;
    Property $$P As Integer
End Class
</Code>

            Dim expected =
<Code>
Imports System

Class C
    ''' &lt;summary&gt;&lt;/summary&gt;
    &lt;Serializable()&gt;
    Property P As Integer
End Class
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Function

#End Region

#Region "AddParameter tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddParameter1() As Task
            Dim code =
<Code>
Class C
    Property $$P As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            Dim expected =
<Code>
Class C
    Property P(index As Integer) As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            Await TestAddParameter(code, expected, New ParameterData With {.Name = "index", .Type = "Integer"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddParameter2() As Task
            Dim code =
<Code>
Class C
    Property $$P() As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            Dim expected =
<Code>
Class C
    Property P(index As Integer) As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            Await TestAddParameter(code, expected, New ParameterData With {.Name = "index", .Type = "Integer"})
        End Function

#End Region

#Region "RemoveParameter tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveParameter1() As Task
            Dim code =
<Code>
Class C
    Property $$P(index As Integer) As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            Dim expected =
<Code>
Class C
    Property P() As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            Await TestRemoveChild(code, expected, "index")
        End Function

#End Region

#Region "Set IsDefault tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsDefault1() As Task
            Dim code =
<Code>
Class C
    Public Property $$P As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            Dim expected =
<Code>
Class C
    Default Public Property P As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            Await TestSetIsDefault(code, expected, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsDefault2() As Task
            Dim code =
<Code>
Class C
    Default Public Property $$P As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            Dim expected =
<Code>
Class C
    Public Property P As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            Await TestSetIsDefault(code, expected, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsDefault3() As Task
            Dim code =
<Code>
Class C
    Public Property $$P As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Default Public Property P As Integer
End Class
</Code>

            Await TestSetIsDefault(code, expected, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsDefault4() As Task
            Dim code =
<Code>
Class C
    Default Public Property $$P As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Public Property P As Integer
End Class
</Code>

            Await TestSetIsDefault(code, expected, False)
        End Function

#End Region

#Region "Set OverrideKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetOverrideKind1() As Task
            Dim code =
<Code>
Class C
    Public Property $$P As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            Dim expected =
<Code>
Class C
    Public MustOverride Property P As Integer
End Class
</Code>

            Await TestSetOverrideKind(code, expected, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindAbstract)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetOverrideKind2() As Task
            Dim code =
<Code>
Class C
    Public MustOverride Property $$P As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Public Property P As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            Await TestSetOverrideKind(code, expected, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindNone)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetOverrideKind3() As Task
            Dim code =
<Code>
Class C
    Public MustOverride Property $$P$
End Class
</Code>

            Dim expected =
<Code>
Class C
    Public Property P$
        Get
        End Get
        Set(value$)
        End Set
    End Property
End Class
</Code>

            Await TestSetOverrideKind(code, expected, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindNone)
        End Function

#End Region

#Region "Set Type tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType1() As Task
            Dim code =
<Code>
Class C
    Property $$Foo As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Property Foo As String
End Class
</Code>

            Await TestSetTypeProp(code, expected, "String")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType2() As Task
            Dim code =
<Code>
Class C
    Property $$Foo As New System.Text.StringBuilder
End Class
</Code>

            Dim expected =
<Code>
Class C
    Property Foo As New Integer
End Class
</Code>

            Await TestSetTypeProp(code, expected, "System.Int32")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType3() As Task
            Dim code =
<Code>
Class C
    Property $$Foo As String
        Get

        End Get
        Set(value As String)

        End Set
    End Property
End Class
</Code>

            Dim expected =
<Code>
Class C
    Property Foo As Integer
        Get

        End Get
        Set(value As Integer)

        End Set
    End Property
End Class
</Code>

            Await TestSetTypeProp(code, expected, "System.Int32")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType4() As Task
            Dim code =
<Code>
Class C
    Property $$Foo$
        Get

        End Get
        Set(value As String)

        End Set
    End Property
End Class
</Code>

            Dim expected =
<Code>
Class C
    Property Foo$ As Integer
        Get

        End Get
        Set(value As Integer)

        End Set
    End Property
End Class
</Code>

            Await TestSetTypeProp(code, expected, "System.Int32")
        End Function

#End Region

#Region "AutoImplementedPropertyExtender"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAutoImplementedPropertyExtender_IsAutoImplemented1() As Task
            Dim code =
<Code>
Public Class C
    Property $$P As Integer
End Class
</Code>

            Await TestAutoImplementedPropertyExtender_IsAutoImplemented(code, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAutoImplementedPropertyExtender_IsAutoImplemented2() As Task
            Dim code =
<Code>
Public Class C
    Property $$P As Integer
        Get
            Return 0
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            Await TestAutoImplementedPropertyExtender_IsAutoImplemented(code, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAutoImplementedPropertyExtender_IsAutoImplemented3() As Task
            Dim code =
<Code>
Public Interface I
    Property $$P As Integer
End Interface
</Code>

            Await TestAutoImplementedPropertyExtender_IsAutoImplemented(code, False)
        End Function

#End Region

#Region "Parameter name tests"

        <WorkItem(1147885)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestParameterNameWithEscapeCharacters() As Task
            Dim code =
<Code>
Class Program
    Property $$P([integer] As Integer) As Integer
        Get
            Return [integer]
        End Get
        Set(value As Integer)

        End Set
    End Property
    Sub Main(args As String())

    End Sub
End Class
</Code>

            Await TestAllParameterNames(code, "[integer]")
        End Function

        <WorkItem(1147885)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestParameterNameWithEscapeCharacters_2() As Task
            Dim code =
<Code>
Class Program
    Property $$P([integer] As Integer, [string] as String) As Integer
        Get
            Return [integer]
        End Get
        Set(value As Integer)

        End Set
    End Property
    Sub Main(args As String())

    End Sub
End Class
</Code>

            Await TestAllParameterNames(code, "[integer]", "[string]")
        End Function

#End Region

        Private Function GetAutoImplementedPropertyExtender(codeElement As EnvDTE80.CodeProperty2) As IVBAutoPropertyExtender
            Return CType(codeElement.Extender(ExtenderNames.VBAutoPropertyExtender), IVBAutoPropertyExtender)
        End Function

        Protected Overrides Function AutoImplementedPropertyExtender_GetIsAutoImplemented(codeElement As EnvDTE80.CodeProperty2) As Boolean
            Return GetAutoImplementedPropertyExtender(codeElement).IsAutoImplemented
        End Function

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property
    End Class
End Namespace
