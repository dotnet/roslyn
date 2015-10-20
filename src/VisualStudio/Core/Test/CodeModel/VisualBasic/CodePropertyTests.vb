' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel.Extenders
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel.Interop
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.VisualBasic
    Public Class CodePropertyTests
        Inherits AbstractCodePropertyTests

#Region "GetStartPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetStartPoint()
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

            TestGetStartPoint(code,
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
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetStartPoint_Attribute()
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

            TestGetStartPoint(code,
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
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetStartPoint_AutoProperty()
            Dim code =
<Code>
Class C
    Public Property $$P As Integer
End Class
</Code>

            TestGetStartPoint(code,
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
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetStartPoint_AutoProperty_Attribute()
            Dim code =
<Code>
Class C
    &lt;System.CLSCompliant(True)&gt;
    Public Property $$P As Integer
End Class
</Code>

            TestGetStartPoint(code,
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
        End Sub

#End Region

#Region "GetEndPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetEndPoint()
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

            TestGetEndPoint(code,
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
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetEndPoint_Attribute()
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

            TestGetEndPoint(code,
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
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetEndPoint_AutoProperty()
            Dim code =
<Code>
Class C
    Public Property $$P As Integer
End Class
</Code>

            TestGetEndPoint(code,
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
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetEndPoint_AutoProperty_Attribute()
            Dim code =
<Code>
Class C
    &lt;System.CLSCompliant(True)&gt;
    Public Property $$P As Integer
End Class
</Code>

            TestGetEndPoint(code,
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
        End Sub

#End Region

#Region "Attributes"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Attributes_AutoProperty()
            Dim code =
<Code>
Imports System

Class C1
    &lt;CLSCompliant(False)&gt;
    Public Property $$P As String
End Class
</Code>

            TestAttributes(code, IsElement("CLSCompliant"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Attributes_Property()
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

            TestAttributes(code, IsElement("CLSCompliant"))
        End Sub

#End Region

#Region "Getter tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetterIsNothingForAutoProp()
            Dim code =
<Code>
Class C
    Public Property $$P As Integer
End Class
</Code>

            TestGetter(code,
                       Sub(accessor)
                           Assert.Null(accessor)
                       End Sub)
        End Sub

#End Region

#Region "IsDefault tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub IsDefault1()
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

            TestIsDefault(code, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub IsDefault2()
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

            TestIsDefault(code, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub IsDefault3()
            Dim code =
<Code>
Class C
    Public Property $$P As Integer
End Class
</Code>

            TestIsDefault(code, False)
        End Sub

#End Region

#Region "Name tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Name1()
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

            TestName(code, "P")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Name2()
            Dim code =
<Code>
Class C
    Public Property $$P As Integer
End Class
</Code>

            TestName(code, "P")
        End Sub

#End Region

#Region "OverrideKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub OverrideKind1()
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

            TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindNone)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub OverrideKind2()
            Dim code =
<Code>
Class C
    Public MustOverride Property $$P As Integer
End Class
</Code>

            TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindAbstract)
        End Sub

#End Region

#Region "Prototype tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_UniqueSignature()
            Dim code =
<Code>
Namespace N
    Class C
        Property P$$ As Integer = 42
    End Class
End Namespace
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeUniqueSignature, "P:N.C.P")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_FullName()
            Dim code =
<Code>
Namespace N
    Class C
        Property P$$ As Integer = 42
    End Class
End Namespace
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeFullname, "N.C.P()")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_ClassName1()
            Dim code =
<Code>
Namespace N
    Class C
        Property P$$ As Integer = 42
    End Class
End Namespace
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName, "C.P()")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_ClassName2()
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

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName, "C.P()")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_ClassName3()
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

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName, "C.P()")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_ClassName_ParamNames()
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

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName Or EnvDTE.vsCMPrototype.vsCMPrototypeParamNames, "C.P(index )")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_ClassName_ParamNames_ParamTypes()
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

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName Or EnvDTE.vsCMPrototype.vsCMPrototypeParamNames Or EnvDTE.vsCMPrototype.vsCMPrototypeParamTypes, "C.P(index As String)")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_ClassName_ParamNames_ParamDefaultValues()
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

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName Or EnvDTE.vsCMPrototype.vsCMPrototypeParamNames Or EnvDTE.vsCMPrototype.vsCMPrototypeParamDefaultValues, "C.P(index  = """")")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_ClassName_ParamTypes()
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

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName Or EnvDTE.vsCMPrototype.vsCMPrototypeParamTypes, "C.P(String)")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_ClassName_ParamTypes_ParamDefaultValues()
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

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName Or EnvDTE.vsCMPrototype.vsCMPrototypeParamTypes Or EnvDTE.vsCMPrototype.vsCMPrototypeParamDefaultValues, "C.P(String = """")")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_ClassName_ParamNames_ParamTypes_ParamDefaultValues()
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

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName Or EnvDTE.vsCMPrototype.vsCMPrototypeParamNames Or EnvDTE.vsCMPrototype.vsCMPrototypeParamTypes Or EnvDTE.vsCMPrototype.vsCMPrototypeParamDefaultValues, "C.P(index As String = """")")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_Type()
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

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeType, "P()")
        End Sub

#End Region

#Region "ReadWrite tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub ReadWrite1()
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

            TestReadWrite(code, EnvDTE80.vsCMPropertyKind.vsCMPropertyKindReadWrite)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub ReadWrite2()
            Dim code =
<Code>
Class C
    Public ReadOnly Property $$P As Integer
        Get
        End Get
    End Property
End Class
</Code>

            TestReadWrite(code, EnvDTE80.vsCMPropertyKind.vsCMPropertyKindReadOnly)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub ReadWrite3()
            Dim code =
<Code>
Class C
    Public WriteOnly Property $$P As Integer
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            TestReadWrite(code, EnvDTE80.vsCMPropertyKind.vsCMPropertyKindWriteOnly)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub ReadWrite4()
            Dim code =
<Code>
Class C
    Public Property $$P As Integer
End Class
</Code>

            TestReadWrite(code, EnvDTE80.vsCMPropertyKind.vsCMPropertyKindReadWrite)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub ReadWrite5()
            Dim code =
<Code>
Class C
    Public ReadOnly Property $$P As Integer
End Class
</Code>

            TestReadWrite(code, EnvDTE80.vsCMPropertyKind.vsCMPropertyKindReadOnly)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub ReadWrite6()
            Dim code =
<Code>
Class C
    Public WriteOnly Property $$P As Integer
End Class
</Code>

            TestReadWrite(code, EnvDTE80.vsCMPropertyKind.vsCMPropertyKindWriteOnly)
        End Sub

#End Region

#Region "Setter tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetterIsNothingForAutoProp()
            Dim code =
<Code>
Class C
    Public Property $$P As Integer
End Class
</Code>

            TestSetter(code,
                       Sub(accessor)
                           Assert.Null(accessor)
                       End Sub)
        End Sub

#End Region

#Region "Type tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Type1()
            Dim code =
<Code>
Class C
    Property $$Foo As Integer
End Class
</Code>

            TestTypeProp(code,
                         New CodeTypeRefData With {
                             .AsFullName = "System.Int32",
                             .AsString = "Integer",
                             .CodeTypeFullName = "System.Int32",
                             .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefInt
                         })
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Type2()
            Dim code =
<Code>
Class C
    Property $$Foo As New System.Text.StringBuilder
End Class
</Code>

            TestTypeProp(code,
                         New CodeTypeRefData With {
                             .AsFullName = "System.Text.StringBuilder",
                             .AsString = "System.Text.StringBuilder",
                             .CodeTypeFullName = "System.Text.StringBuilder",
                             .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefCodeType
                         })
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Type3()
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

            TestTypeProp(code,
                         New CodeTypeRefData With {
                             .AsFullName = "System.String",
                             .AsString = "String",
                             .CodeTypeFullName = "System.String",
                             .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefString
                         })
        End Sub

#End Region

#Region "AddAttribute tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute_NormalProperty()
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
            TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute_AutoProperty()
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
            TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Sub

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute_NormalProperty_BelowDocComment()
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
            TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Sub

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute_AutoProperty_BelowDocComment()
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
            TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Sub

#End Region

#Region "AddParameter tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddParameter1()
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

            TestAddParameter(code, expected, New ParameterData With {.Name = "index", .Type = "Integer"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddParameter2()
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

            TestAddParameter(code, expected, New ParameterData With {.Name = "index", .Type = "Integer"})
        End Sub

#End Region

#Region "RemoveParameter tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveParameter1()
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

            TestRemoveChild(code, expected, "index")
        End Sub

#End Region

#Region "Set IsDefault tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsDefault1()
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

            TestSetIsDefault(code, expected, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsDefault2()
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

            TestSetIsDefault(code, expected, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsDefault3()
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

            TestSetIsDefault(code, expected, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsDefault4()
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

            TestSetIsDefault(code, expected, False)
        End Sub

#End Region

#Region "Set OverrideKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetOverrideKind1()
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

            TestSetOverrideKind(code, expected, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindAbstract)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetOverrideKind2()
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

            TestSetOverrideKind(code, expected, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindNone)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetOverrideKind3()
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

            TestSetOverrideKind(code, expected, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindNone)
        End Sub

#End Region

#Region "Set Type tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetType1()
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

            TestSetTypeProp(code, expected, "String")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetType2()
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

            TestSetTypeProp(code, expected, "System.Int32")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetType3()
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

            TestSetTypeProp(code, expected, "System.Int32")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetType4()
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

            TestSetTypeProp(code, expected, "System.Int32")
        End Sub

#End Region

#Region "AutoImplementedPropertyExtender"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AutoImplementedPropertyExtender_IsAutoImplemented1()
            Dim code =
<Code>
Public Class C
    Property $$P As Integer
End Class
</Code>

            TestAutoImplementedPropertyExtender_IsAutoImplemented(code, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AutoImplementedPropertyExtender_IsAutoImplemented2()
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

            TestAutoImplementedPropertyExtender_IsAutoImplemented(code, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AutoImplementedPropertyExtender_IsAutoImplemented3()
            Dim code =
<Code>
Public Interface I
    Property $$P As Integer
End Interface
</Code>

            TestAutoImplementedPropertyExtender_IsAutoImplemented(code, False)
        End Sub

#End Region

#Region "Parameter name tests"

        <WorkItem(1147885)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestParameterNameWithEscapeCharacters()
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

            TestAllParameterNames(code, "[integer]")
        End Sub

        <WorkItem(1147885)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestParameterNameWithEscapeCharacters_2()
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

            TestAllParameterNames(code, "[integer]", "[string]")
        End Sub

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
