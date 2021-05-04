' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.MethodXML
    Partial Public Class MethodXMLTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestCSLocalDeclarations_NoInitializer()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
public class C
{
    $$void M()
    {
        int x;
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
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestCSLocalDeclarations_WithInitializer()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
public class C
{
    $$void M()
    {
        int x = 42;
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
</Block>

            Test(definition, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestCSLocalDeclarations_EscapedKeywordName()
            Dim definition =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
public class C
{
    $$void M()
    {
        int @class = 0;
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<Block>
    <Local line="5">
        <Type>System.Int32</Type>
        <Name>@class</Name>
        <Expression>
            <Literal>
                <Number type="System.Int32">0</Number>
            </Literal>
        </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestCSLocalDeclarations_ArrayNoInitializer()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
public class C
{
    $$void M()
    {
        int[] x;
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
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestCSLocalDeclarations_ArrayWithInitializer()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
public class C
{
    $$void M()
    {
        int[] x = new int[42];
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
</Block>

            Test(definition, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestCSLocalDeclarations_WithBinaryPlusInitializer()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
public class C
{
    $$void M()
    {
        int goo = 1 + 1;
    }
}
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <Local line="5">
        <Type>System.Int32</Type>
        <Name>goo</Name>
        <Expression>
            <BinaryOperation binaryoperator="plus">
                <Expression>
                    <Literal>
                        <Number type="System.Int32">1</Number>
                    </Literal>
                </Expression>
                <Expression>
                    <Literal>
                        <Number type="System.Int32">1</Number>
                    </Literal>
                </Expression>
            </BinaryOperation>
        </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestCSLocalDeclarations_WithBitwiseOrInitializer()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
public class C
{
    $$void M()
    {
        int goo = 1 | 1;
    }
}
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <Local line="5">
        <Type>System.Int32</Type>
        <Name>goo</Name>
        <Expression>
            <BinaryOperation binaryoperator="bitor">
                <Expression>
                    <Literal>
                        <Number type="System.Int32">1</Number>
                    </Literal>
                </Expression>
                <Expression>
                    <Literal>
                        <Number type="System.Int32">1</Number>
                    </Literal>
                </Expression>
            </BinaryOperation>
        </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestCSLocalDeclarations_WithBitwiseAndInitializer()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
public class C
{
    $$void M()
    {
        int goo = 1 &amp; 1;
    }
}
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <Local line="5">
        <Type>System.Int32</Type>
        <Name>goo</Name>
        <Expression>
            <BinaryOperation binaryoperator="bitand">
                <Expression>
                    <Literal>
                        <Number type="System.Int32">1</Number>
                    </Literal>
                </Expression>
                <Expression>
                    <Literal>
                        <Number type="System.Int32">1</Number>
                    </Literal>
                </Expression>
            </BinaryOperation>
        </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestCSLocalDeclarations_WithCastInitializer()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
public class C
{
    $$void M()
    {
        long goo = (long)2;
    }
}
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <Local line="5">
        <Type>System.Int64</Type>
        <Name>goo</Name>
        <Expression>
            <Cast>
                <Type>System.Int64</Type>
                <Expression>
                    <Literal>
                        <Number type="System.Int32">2</Number>
                    </Literal>
                </Expression>
            </Cast>
        </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestCSLocalDeclarations_WithObjectCreationInitializer()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
using System;
public class C
{
    $$void M()
    {
        DateTime test = new DateTime(42);
    }
}
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <Local line="6">
        <Type>System.DateTime</Type>
        <Name>test</Name>
        <Expression>
            <NewClass>
                <Type>System.DateTime</Type>
                <Argument>
                    <Expression>
                        <Literal>
                            <Number type="System.Int32">42</Number>
                        </Literal>
                    </Expression>
                </Argument>
            </NewClass>
        </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestCSLocalDeclarations_WithParenthesizedInitializer()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
public class C
{
    $$void M()
    {
        int parenthesized = (1 + 1);
    }
}
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <Local line="5">
        <Type>System.Int32</Type>
        <Name>parenthesized</Name>
        <Expression>
            <Parentheses>
                <Expression>
                    <BinaryOperation binaryoperator="plus">
                        <Expression>
                            <Literal>
                                <Number type="System.Int32">1</Number>
                            </Literal>
                        </Expression>
                        <Expression>
                            <Literal>
                                <Number type="System.Int32">1</Number>
                            </Literal>
                        </Expression>
                    </BinaryOperation>
                </Expression>
            </Parentheses>
        </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestCSLocalDeclarations_WithNullInitializer()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
public class C
{
    $$void M()
    {
        object o = null;
    }
}
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <Local line="5">
        <Type>System.Object</Type>
        <Name>o</Name>
        <Expression>
            <Literal>
                <Null/>
            </Literal>
        </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestCSLocalDeclarations_WithNegativeInitializer()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
public class C
{
    $$void M()
    {
        int negative = -42;
    }
}
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <Local line="5">
        <Type>System.Int32</Type>
        <Name>negative</Name>
        <Expression>
            <Literal>
                <Number type="System.Int32">-42</Number>
            </Literal>
        </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestCSLocalDeclarations_WithBooleanInitializer()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
public class C
{
    $$void M()
    {
        bool t = true;
        bool f = false;
    }
}
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <Local line="5">
        <Type>System.Boolean</Type>
        <Name>t</Name>
        <Expression>
            <Literal>
                <Boolean>true</Boolean>
            </Literal>
        </Expression>
    </Local>
    <Local line="6">
        <Type>System.Boolean</Type>
        <Name>f</Name>
        <Expression>
            <Literal>
                <Boolean>false</Boolean>
            </Literal>
        </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestCSLocalDeclarations_WithStringInitializer()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
public class C
{
    $$void M()
    {
        string s = "Test";
    }
}
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <Local line="5">
        <Type>System.String</Type>
        <Name>s</Name>
        <Expression>
            <Literal>
                <String>Test</String>
            </Literal>
        </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestCSLocalDeclarations_WithCharInitializer()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
public class C
{
    $$void M()
    {
        char c = 'A';
    }
}
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <Local line="5">
        <Type>System.Char</Type>
        <Name>c</Name>
        <Expression>
            <Literal>
                <Char>A</Char>
            </Literal>
        </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestCSLocalDeclarations_WithArrayInitializer()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
public class C
{
    $$void M()
    {
        object[] o = new object[42];
    }
}
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <Local line="5">
        <ArrayType rank="1">
            <Type>System.Object</Type>
        </ArrayType>
        <Name>o</Name>
        <Expression>
            <NewArray>
                <ArrayType rank="1">
                    <Type>System.Object</Type>
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
</Block>

            Test(definition, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestCSLocalDeclarations_DifferentBlocks()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
public class C
{
    $$void M()
    {
        {
            int x = 51;
        }

        {
            int x = 42;
        }
    }
}
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <Block>
        <Local line="6">
            <Type>System.Int32</Type>
            <Name>x</Name>
            <Expression>
                <Literal>
                    <Number type="System.Int32">51</Number>
                </Literal>
            </Expression>
        </Local>
    </Block>
    <Block>
        <Local line="10">
            <Type>System.Int32</Type>
            <Name>x</Name>
            <Expression>
                <Literal>
                    <Number type="System.Int32">42</Number>
                </Literal>
            </Expression>
        </Local>
    </Block>
</Block>

            Test(definition, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestCSLocalDeclarations_TypeOfInitializer()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
public class C
{
    $$void M()
    {
        var t = typeof(string);
    }
}
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <Local line="5">
        <Type>System.Type</Type>
        <Name>t</Name>
        <Expression>
            <Type>System.String</Type>
        </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

    End Class
End Namespace
