' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.MethodXML
    Partial Public Class MethodXMLTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestCSInvocations_InvocationWithThis()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
public class C
{
    $$void M()
    {
        this.Goo();
    }

    void Goo()
    {
    }
}
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <ExpressionStatement line="5">
        <Expression>
            <MethodCall>
                <Expression>
                    <NameRef variablekind="method">
                        <Expression>
                            <ThisReference/>
                        </Expression>
                        <Name>Goo</Name>
                    </NameRef>
                </Expression>
            </MethodCall>
        </Expression>
    </ExpressionStatement>
</Block>

            Test(definition, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestCSInvocations_InvocationWithThisAndArgs()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
public class C
{
    $$void M()
    {
        this.Goo(1, 2);
    }

    void Goo(int arg1, int arg2)
    {
    }
}
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <ExpressionStatement line="5">
        <Expression>
            <MethodCall>
                <Expression>
                    <NameRef variablekind="method">
                        <Expression>
                            <ThisReference/>
                        </Expression>
                        <Name>Goo</Name>
                    </NameRef>
                </Expression>
                <Argument>
                    <Expression>
                        <Literal>
                            <Number type="System.Int32">1</Number>
                        </Literal>
                    </Expression>
                </Argument>
                <Argument>
                    <Expression>
                        <Literal>
                            <Number type="System.Int32">2</Number>
                        </Literal>
                    </Expression>
                </Argument>
            </MethodCall>
        </Expression>
    </ExpressionStatement>
</Block>

            Test(definition, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestCSInvocations_InvocationWithoutThis()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
public class C
{
    $$void M()
    {
        Goo();
    }

    void Goo()
    {
    }
}
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <ExpressionStatement line="5">
        <Expression>
            <MethodCall>
                <Expression>
                    <NameRef variablekind="method">
                        <Expression>
                            <ThisReference/>
                        </Expression>
                        <Name>Goo</Name>
                    </NameRef>
                </Expression>
            </MethodCall>
        </Expression>
    </ExpressionStatement>
</Block>

            Test(definition, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestCSInvocations_WithArrayInitializer()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
public class C
{
    $$void M()
    {
        this.list.AddRange(new object[] { "goo", "bar", "baz" });
    }

    System.Collections.ArrayList list;
}
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <ExpressionStatement line="5">
        <Expression>
            <MethodCall>
                <Expression>
                    <NameRef variablekind="method">
                        <Expression>
                            <NameRef variablekind="field">
                                <Expression>
                                    <ThisReference/>
                                </Expression>
                                <Name>list</Name>
                            </NameRef>
                        </Expression>
                        <Name>AddRange</Name>
                    </NameRef>
                </Expression>
                <Argument>
                    <Expression>
                        <NewArray>
                            <ArrayType rank="1">
                                <Type>System.Object</Type>
                            </ArrayType>
                            <Bound>
                                <Expression>
                                    <Literal>
                                        <Number>3</Number>
                                    </Literal>
                                </Expression>
                            </Bound>
                            <Expression>
                                <Literal>
                                    <Array>
                                        <Expression>
                                            <Literal>
                                                <String>goo</String>
                                            </Literal>
                                        </Expression>
                                        <Expression>
                                            <Literal>
                                                <String>bar</String>
                                            </Literal>
                                        </Expression>
                                        <Expression>
                                            <Literal>
                                                <String>baz</String>
                                            </Literal>
                                        </Expression>
                                    </Array>
                                </Literal>
                            </Expression>
                        </NewArray>
                    </Expression>
                </Argument>
            </MethodCall>
        </Expression>
    </ExpressionStatement>
</Block>

            Test(definition, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestCSInvocations_CastOfParenthesizedExpression()
            Dim definition =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
public class C
{
    $$void M()
    {
        object o = new string('.', 10);
        var s = ((System.String)(o)).ToString();
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
            <NewClass>
                <Type>System.String</Type>
                <Argument>
                    <Expression>
                        <Cast>
                            <Type>System.Char</Type>
                            <Expression>
                                <Literal>
                                    <Number type="System.UInt16">46</Number>
                                </Literal>
                            </Expression>
                        </Cast>
                    </Expression>
                </Argument>
                <Argument>
                    <Expression>
                        <Literal>
                            <Number type="System.Int32">10</Number>
                        </Literal>
                    </Expression>
                </Argument>
            </NewClass>
        </Expression>
    </Local>
    <Local line="6">
        <Type>System.String</Type>
        <Name>s</Name>
        <Expression>
            <MethodCall>
                <Expression>
                    <NameRef variablekind="method">
                        <Expression>
                            <Parentheses>
                                <Expression>
                                    <Cast>
                                        <Type>System.String</Type>
                                        <Expression>
                                            <Parentheses>
                                                <Expression>
                                                    <NameRef variablekind="local">
                                                        <Name>o</Name>
                                                    </NameRef>
                                                </Expression>
                                            </Parentheses>
                                        </Expression>
                                    </Cast>
                                </Expression>
                            </Parentheses>
                        </Expression>
                        <Name>ToString</Name>
                    </NameRef>
                </Expression>
            </MethodCall>
        </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

    End Class
End Namespace
