' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.MethodXML
    Partial Public Class MethodXMLTests

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub CSAssignments_FieldWithThis()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
public class C
{
    $$void M()
    {
        this.x = 42;
    }

    int x;
}
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <ExpressionStatement line="5">
        <Expression>
            <Assignment>
                <Expression>
                    <NameRef variablekind="field">
                        <Expression>
                            <ThisReference/>
                        </Expression>
                        <Name>x</Name>
                    </NameRef>
                </Expression>
                <Expression>
                    <Literal>
                        <Number type="System.Int32">42</Number>
                    </Literal>
                </Expression>
            </Assignment>
        </Expression>
    </ExpressionStatement>
</Block>

            Test(definition, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub CSAssignments_FieldWithoutThis()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
public class C
{
    $$void M()
    {
        x = 42;
    }

    int x;
}
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <ExpressionStatement line="5">
        <Expression>
            <Assignment>
                <Expression>
                    <NameRef variablekind="field">
                        <Expression>
                            <ThisReference/>
                        </Expression>
                        <Name>x</Name>
                    </NameRef>
                </Expression>
                <Expression>
                    <Literal>
                        <Number type="System.Int32">42</Number>
                    </Literal>
                </Expression>
            </Assignment>
        </Expression>
    </ExpressionStatement>
</Block>

            Test(definition, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub CSAssignments_FieldWithObjectCreation()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
public class C
{
    $$void M()
    {
        this.x = new System.Object();
    }

    System.Object x;
}
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <ExpressionStatement line="5">
        <Expression>
            <Assignment>
                <Expression>
                    <NameRef variablekind="field">
                        <Expression>
                            <ThisReference/>
                        </Expression>
                        <Name>x</Name>
                    </NameRef>
                </Expression>
                <Expression>
                    <NewClass>
                        <Type>System.Object</Type>
                    </NewClass>
                </Expression>
            </Assignment>
        </Expression>
    </ExpressionStatement>
</Block>

            Test(definition, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub CSAssignments_FieldWithEnumMember()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
public class C
{
    $$void M()
    {
        this.x = System.DayOfWeek.Friday;
    }

    private System.DayOfWeek x;
}
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <ExpressionStatement line="5">
        <Expression>
            <Assignment>
                <Expression>
                    <NameRef variablekind="field">
                        <Expression>
                            <ThisReference/>
                        </Expression>
                        <Name>x</Name>
                    </NameRef>
                </Expression>
                <Expression>
                    <NameRef variablekind="field">
                        <Expression>
                            <Literal>
                                <Type>System.DayOfWeek</Type>
                            </Literal>
                        </Expression>
                        <Name>Friday</Name>
                    </NameRef>
                </Expression>
            </Assignment>
        </Expression>
    </ExpressionStatement>
</Block>

            Test(definition, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub CSAssignments_PropertyWithThis()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
public class C
{
    $$void M()
    {
        this.X = 42;
    }

    public int X { get; set; }
}
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <ExpressionStatement line="5">
        <Expression>
            <Assignment>
                <Expression>
                    <NameRef variablekind="property">
                        <Expression>
                            <ThisReference/>
                        </Expression>
                        <Name>X</Name>
                    </NameRef>
                </Expression>
                <Expression>
                    <Literal>
                        <Number type="System.Int32">42</Number>
                    </Literal>
                </Expression>
            </Assignment>
        </Expression>
    </ExpressionStatement>
</Block>

            Test(definition, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub CSAssignments_PropertyWithoutThis()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
public class C
{
    $$void M()
    {
        X = 42;
    }

    public int X { get; set; }
}
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <ExpressionStatement line="5">
        <Expression>
            <Assignment>
                <Expression>
                    <NameRef variablekind="property">
                        <Expression>
                            <ThisReference/>
                        </Expression>
                        <Name>X</Name>
                    </NameRef>
                </Expression>
                <Expression>
                    <Literal>
                        <Number type="System.Int32">42</Number>
                    </Literal>
                </Expression>
            </Assignment>
        </Expression>
    </ExpressionStatement>
</Block>

            Test(definition, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub CSAssignments_FieldThroughPropertyWithThis()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
public class C
{
    $$void M()
    {
        this.X.x = 42;
    }

    public C X { get; set; }
    private int x;
}
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <ExpressionStatement line="5">
        <Expression>
            <Assignment>
                <Expression>
                    <NameRef variablekind="field">
                        <Expression>
                            <NameRef variablekind="property">
                                <Expression>
                                    <ThisReference/>
                                </Expression>
                                <Name>X</Name>
                            </NameRef>
                        </Expression>
                        <Name>x</Name>
                    </NameRef>
                </Expression>
                <Expression>
                    <Literal>
                        <Number type="System.Int32">42</Number>
                    </Literal>
                </Expression>
            </Assignment>
        </Expression>
    </ExpressionStatement>
</Block>

            Test(definition, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub CSAssignments_FieldThroughPropertyWithoutThis()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
public class C
{
    $$void M()
    {
        X.x = 42;
    }

    public C X { get; set; }
    private int x;
}
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <ExpressionStatement line="5">
        <Expression>
            <Assignment>
                <Expression>
                    <NameRef variablekind="field">
                        <Expression>
                            <NameRef variablekind="property">
                                <Expression>
                                    <ThisReference/>
                                </Expression>
                                <Name>X</Name>
                            </NameRef>
                        </Expression>
                        <Name>x</Name>
                    </NameRef>
                </Expression>
                <Expression>
                    <Literal>
                        <Number type="System.Int32">42</Number>
                    </Literal>
                </Expression>
            </Assignment>
        </Expression>
    </ExpressionStatement>
</Block>

            Test(definition, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub CSAssignments_AssignLocalsWithField()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
public class C
{
    $$void M()
    {
        int x = 42;
        x = y;
    }

    private int y = 100;
}
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <Local line="5">
        <Type>System.Int32</Type>
        <Name>x</Name>
        <Expression>
            <Literal>
                <Number type="System.Int32">42</Number>
            </Literal>
        </Expression>
    </Local>
    <ExpressionStatement line="6">
        <Expression>
            <Assignment>
                <Expression>
                    <NameRef variablekind="local">
                        <Name>x</Name>
                    </NameRef>
                </Expression>
                <Expression>
                    <NameRef variablekind="field">
                        <Expression>
                            <ThisReference/>
                        </Expression>
                        <Name>y</Name>
                    </NameRef>
                </Expression>
            </Assignment>
        </Expression>
    </ExpressionStatement>
</Block>

            Test(definition, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub CSAssignments_CompoundAdd()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
public class C
{
    $$void M()
    {
        int x = 1;
        x += 41;
    }
}
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <Local line="5">
        <Type>System.Int32</Type>
        <Name>x</Name>
        <Expression>
            <Literal>
                <Number type="System.Int32">1</Number>
            </Literal>
        </Expression>
    </Local>
    <ExpressionStatement line="6">
        <Expression>
            <Assignment binaryoperator="adddelegate">
                <Expression>
                    <NameRef variablekind="local">
                        <Name>x</Name>
                    </NameRef>
                </Expression>
                <Expression>
                    <Literal>
                        <Number type="System.Int32">41</Number>
                    </Literal>
                </Expression>
            </Assignment>
        </Expression>
    </ExpressionStatement>
</Block>

            Test(definition, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub CSAssignments_CompoundSubtract()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
public class C
{
    $$void M()
    {
        int x = 42;
        x -= 41;
    }
}
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <Local line="5">
        <Type>System.Int32</Type>
        <Name>x</Name>
        <Expression>
            <Literal>
                <Number type="System.Int32">42</Number>
            </Literal>
        </Expression>
    </Local>
    <Quote line="6">x -= 41;</Quote>
</Block>

            Test(definition, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub CSAssignments_ArrayElementAccess()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
public class C
{
    $$void M()
    {
        int[] x = new int[42];
        x[0] = 10;
        var y = x[1];
    }
}
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <Local line="5">
        <ArrayType rank="1">
            <Type>System.Int32</Type>
        </ArrayType>
        <Name>x</Name>
        <Expression>
            <NewArray>
                <ArrayType rank="1">
                    <Type>System.Int32</Type>
                </ArrayType>
                <Bound>
                    <Expression>
                        <Literal>
                            <Number type="System.Int32">42</Number>
                        </Literal>
                    </Expression>
                </Bound>
            </NewArray>
        </Expression>
    </Local>
    <ExpressionStatement line="6">
        <Expression>
            <Assignment>
                <Expression>
                    <ArrayElementAccess>
                        <Expression>
                            <NameRef variablekind="local">
                                <Name>x</Name>
                            </NameRef>
                        </Expression>
                        <Expression>
                            <Literal>
                                <Number type="System.Int32">0</Number>
                            </Literal>
                        </Expression>
                    </ArrayElementAccess>
                </Expression>
                <Expression>
                    <Literal>
                        <Number type="System.Int32">10</Number>
                    </Literal>
                </Expression>
            </Assignment>
        </Expression>
    </ExpressionStatement>
    <Local line="7">
        <Type>System.Int32</Type>
        <Name>y</Name>
        <Expression>
            <ArrayElementAccess>
                <Expression>
                    <NameRef variablekind="local">
                        <Name>x</Name>
                    </NameRef>
                </Expression>
                <Expression>
                    <Literal>
                        <Number type="System.Int32">1</Number>
                    </Literal>
                </Expression>
            </ArrayElementAccess>
        </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <WorkItem(743120)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub CSAssignments_PropertyOffParameter()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
public class C
{
    $$void M(System.Text.StringBuilder builder)
    {
        builder.Capacity = 10;
    }
}
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <ExpressionStatement line="5">
        <Expression>
            <Assignment>
                <Expression>
                    <NameRef variablekind="property">
                        <Expression>
                            <NameRef variablekind="local">
                                <Name>builder</Name>
                            </NameRef>
                        </Expression>
                        <Name>Capacity</Name>
                    </NameRef>
                </Expression>
                <Expression>
                    <Literal>
                        <Number type="System.Int32">10</Number>
                    </Literal>
                </Expression>
            </Assignment>
        </Expression>
    </ExpressionStatement>
</Block>

            Test(definition, expected)
        End Sub

        <WorkItem(831374)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub CSAssignments_NullableValue()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
public class C
{
    $$void M()
    {
        int? i = 0;
        int? j = null;
    }
}
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <Local line="5">
        <Type>System.Nullable`1[System.Int32]</Type>
        <Name>i</Name>
        <Expression>
            <Literal>
                <Number type="System.Int32">0</Number>
            </Literal>
        </Expression>
    </Local>
    <Local line="6">
        <Type>System.Nullable`1[System.Int32]</Type>
        <Name>j</Name>
        <Expression>
            <Literal>
                <Null/>
            </Literal>
        </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <WorkItem(831374)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub CSAssignments_ClosedGeneric1()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
using System.Collections.Generic;

public class C
{
    $$void M()
    {
        var l = new List&lt;int&gt;();
    }
}
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <Local line="7">
        <Type>System.Collections.Generic.List`1[System.Int32]</Type>
        <Name>l</Name>
        <Expression>
            <NewClass>
                <Type>System.Collections.Generic.List`1[System.Int32]</Type>
            </NewClass>
        </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <WorkItem(831374)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub CSAssignments_ClosedGeneric2()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
using System.Collections.Generic;

public class C
{
    $$void M()
    {
        var l = new Dictionary&lt;string, List&lt;int&gt;&gt;();
    }
}
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <Local line="7">
        <Type>System.Collections.Generic.Dictionary`2[System.String,System.Collections.Generic.List`1[System.Int32]]</Type>
        <Name>l</Name>
        <Expression>
            <NewClass>
                <Type>System.Collections.Generic.Dictionary`2[System.String,System.Collections.Generic.List`1[System.Int32]]</Type>
            </NewClass>
        </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <WorkItem(831374)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub CSAssignments_ClosedGeneric3()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
using System.Collections.Generic;

public class C
{
    $$void M()
    {
        var l = new List&lt;string[]&gt;();
    }
}
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <Local line="7">
        <Type>System.Collections.Generic.List`1[System.String[]]</Type>
        <Name>l</Name>
        <Expression>
            <NewClass>
                <Type>System.Collections.Generic.List`1[System.String[]]</Type>
            </NewClass>
        </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <WorkItem(831374)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub CSAssignments_ClosedGeneric4()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
using System.Collections.Generic;

public class C
{
    $$void M()
    {
        var l = new List&lt;string[,,]&gt;();
    }
}
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <Local line="7">
        <Type>System.Collections.Generic.List`1[System.String[,,]]</Type>
        <Name>l</Name>
        <Expression>
            <NewClass>
                <Type>System.Collections.Generic.List`1[System.String[,,]]</Type>
            </NewClass>
        </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <WorkItem(831374)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub CSAssignments_Pointer1()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
public class C
{
    $$unsafe void M(int x)
    {
        int* i = &amp;x;
    }
}
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <Quote line="5">int* i = &amp;x;</Quote>
</Block>

            Test(definition, expected)
        End Sub

        <WorkItem(831374)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub CSAssignments_Pointer2()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
public class C
{
    $$unsafe void M(int* x)
    {
        int* i = x;
    }
}
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <Local line="5">
        <Type>System.Int32*</Type>
        <Name>i</Name>
        <Expression>
            <NameRef variablekind="local">
                <Name>x</Name>
            </NameRef>
        </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <WorkItem(831374)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub CSAssignments_Pointer3()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
public class C
{
    $$unsafe void M(int** x)
    {
        int** i = x;
    }
}
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <Local line="5">
        <Type>System.Int32**</Type>
        <Name>i</Name>
        <Expression>
            <NameRef variablekind="local">
                <Name>x</Name>
            </NameRef>
        </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <WorkItem(831374)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub CSAssignments_TypeConfluence()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
using L = System.Collections.Generic.List&lt;byte*[]&gt;;

class C
{
    $$unsafe void M()
    {
        var l = new L();
    }
}
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <Local line="7">
        <Type>System.Collections.Generic.List`1[System.Byte*[]]</Type>
        <Name>l</Name>
        <Expression>
            <NewClass>
                <Type>System.Collections.Generic.List`1[System.Byte*[]]</Type>
            </NewClass>
        </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <WorkItem(887584)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub CSAssignments_EscapedNames()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
enum E
{
    @true,
    @false
}
class C
{
    private E e;

    void $$M()
    {
        e = E.@true;
    }
}
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <ExpressionStatement line="12">
        <Expression>
            <Assignment>
                <Expression>
                    <NameRef variablekind="field">
                        <Expression>
                            <ThisReference/>
                        </Expression>
                        <Name>e</Name>
                    </NameRef>
                </Expression>
                <Expression>
                    <NameRef variablekind="field">
                        <Expression>
                            <Literal>
                                <Type>E</Type>
                            </Literal>
                        </Expression>
                        <Name>true</Name>
                    </NameRef>
                </Expression>
            </Assignment>
        </Expression>
    </ExpressionStatement>
</Block>

            Test(definition, expected)
        End Sub

        <WorkItem(1126037)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub CSAssignments_ControlChar()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
class C
{
    public char Char { get; set; }

    void $$M()
    {
        Char = '\u0011';
    }
}
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <ExpressionStatement line="7">
        <Expression>
            <Assignment>
                <Expression>
                    <NameRef variablekind="property">
                        <Expression>
                            <ThisReference/>
                        </Expression>
                        <Name>Char</Name>
                    </NameRef>
                </Expression>
                <Expression>
                    <Cast>
                        <Type>System.Char</Type>
                        <Expression>
                            <Literal>
                                <Number type="System.UInt16">17</Number>
                            </Literal>
                        </Expression>
                    </Cast>
                </Expression>
            </Assignment>
        </Expression>
    </ExpressionStatement>
</Block>

            Test(definition, expected)
        End Sub

        <WorkItem(4312, "https://github.com/dotnet/roslyn/issues/4312")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub CSAssignments_PropertyAssignedWithEmptyArray()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
class C
{
    private object[] Series { get; set }

    $$void M()
    {
        this.Series = new object[0] {};
    }
}
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <ExpressionStatement line="7">
        <Expression>
            <Assignment>
                <Expression>
                    <NameRef variablekind="property">
                        <Expression>
                            <ThisReference/>
                        </Expression>
                        <Name>Series</Name>
                    </NameRef>
                </Expression>
                <Expression>
                    <NewArray>
                        <ArrayType rank="1">
                            <Type>System.Object</Type>
                        </ArrayType>
                        <Bound>
                            <Expression>
                                <Literal>
                                    <Number>0</Number>
                                </Literal>
                            </Expression>
                        </Bound>
                        <Expression>
                            <Literal>
                                <Array></Array>
                            </Literal>
                        </Expression>
                    </NewArray>
                </Expression>
            </Assignment>
        </Expression>
    </ExpressionStatement>
</Block>

            Test(definition, expected)
        End Sub

        <WorkItem(4149, "https://github.com/dotnet/roslyn/issues/4149")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub CSAssignments_RoundTrippedDoubles()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
class C
{
    void $$M()
    {
        double d = 9.2233720368547758E+18D;
    }
}
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <Local line="5">
        <Type>System.Double</Type>
        <Name>d</Name>
        <Expression>
            <Literal>
                <Number type="System.Double">9.2233720368547758E+18</Number>
            </Literal>
        </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <WorkItem(4149, "https://github.com/dotnet/roslyn/issues/4149")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub CSAssignments_RoundTrippedSingles()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
class C
{
    void $$M()
    {
        float s = 0.333333343F;
    }
}
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <Local line="5">
        <Type>System.Single</Type>
        <Name>s</Name>
        <Expression>
            <Literal>
                <Number type="System.Single">0.333333343</Number>
            </Literal>
        </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

    End Class
End Namespace
