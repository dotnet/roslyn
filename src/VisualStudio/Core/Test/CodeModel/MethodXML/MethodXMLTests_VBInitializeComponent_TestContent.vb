' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.MethodXML
    Partial Public Class MethodXMLTests

        Private Shared ReadOnly s_initializeComponentXML1 As XElement =
<Block>
    <ExpressionStatement line="24">
        <Expression>
            <Assignment>
                <Expression>
                    <NameRef variablekind="field">
                        <Expression>
                            <ThisReference/>
                        </Expression>
                        <Name>components</Name>
                    </NameRef>
                </Expression>
                <Expression>
                    <NewClass>
                        <Type>System.ComponentModel.Container</Type>
                    </NewClass>
                </Expression>
            </Assignment>
        </Expression>
    </ExpressionStatement>
    <ExpressionStatement line="25">
        <Expression>
            <Assignment>
                <Expression>
                    <NameRef variablekind="property">
                        <Expression>
                            <ThisReference/>
                        </Expression>
                        <Name>AutoScaleMode</Name>
                    </NameRef>
                </Expression>
                <Expression>
                    <NameRef variablekind="field">
                        <Expression>
                            <Literal>
                                <Type>System.Windows.Forms.AutoScaleMode</Type>
                            </Literal>
                        </Expression>
                        <Name>Font</Name>
                    </NameRef>
                </Expression>
            </Assignment>
        </Expression>
    </ExpressionStatement>
    <ExpressionStatement line="26">
        <Expression>
            <Assignment>
                <Expression>
                    <NameRef variablekind="property">
                        <Expression>
                            <ThisReference/>
                        </Expression>
                        <Name>Text</Name>
                    </NameRef>
                </Expression>
                <Expression>
                    <Literal>
                        <String>Form1</String>
                    </Literal>
                </Expression>
            </Assignment>
        </Expression>
    </ExpressionStatement>
</Block>

        Private Shared ReadOnly s_initializeComponentXML2 As XElement =
<Block>
    <ExpressionStatement line="24">
        <Expression>
            <Assignment>
                <Expression>
                    <NameRef variablekind="field">
                        <Expression>
                            <ThisReference/>
                        </Expression>
                        <Name>Button1</Name>
                    </NameRef>
                </Expression>
                <Expression>
                    <NewClass>
                        <Type>System.Windows.Forms.Button</Type>
                    </NewClass>
                </Expression>
            </Assignment>
        </Expression>
    </ExpressionStatement>
    <ExpressionStatement line="25">
        <Expression>
            <MethodCall>
                <Expression>
                    <NameRef variablekind="method">
                        <Expression>
                            <ThisReference/>
                        </Expression>
                        <Name>SuspendLayout</Name>
                    </NameRef>
                </Expression>
            </MethodCall>
        </Expression>
    </ExpressionStatement>
    <ExpressionStatement line="29">
        <Expression>
            <Assignment>
                <Expression>
                    <NameRef variablekind="property">
                        <Expression>
                            <NameRef variablekind="field">
                                <Expression>
                                    <ThisReference/>
                                </Expression>
                                <Name>Button1</Name>
                            </NameRef>
                        </Expression>
                        <Name>Location</Name>
                    </NameRef>
                </Expression>
                <Expression>
                    <NewClass>
                        <Type>System.Drawing.Point</Type>
                        <Argument>
                            <Expression>
                                <Literal>
                                    <Number type="System.Int32">87</Number>
                                </Literal>
                            </Expression>
                        </Argument>
                        <Argument>
                            <Expression>
                                <Literal>
                                    <Number type="System.Int32">56</Number>
                                </Literal>
                            </Expression>
                        </Argument>
                    </NewClass>
                </Expression>
            </Assignment>
        </Expression>
    </ExpressionStatement>
    <ExpressionStatement line="30">
        <Expression>
            <Assignment>
                <Expression>
                    <NameRef variablekind="property">
                        <Expression>
                            <NameRef variablekind="field">
                                <Expression>
                                    <ThisReference/>
                                </Expression>
                                <Name>Button1</Name>
                            </NameRef>
                        </Expression>
                        <Name>Name</Name>
                    </NameRef>
                </Expression>
                <Expression>
                    <Literal>
                        <String>Button1</String>
                    </Literal>
                </Expression>
            </Assignment>
        </Expression>
    </ExpressionStatement>
    <ExpressionStatement line="31">
        <Expression>
            <Assignment>
                <Expression>
                    <NameRef variablekind="property">
                        <Expression>
                            <NameRef variablekind="field">
                                <Expression>
                                    <ThisReference/>
                                </Expression>
                                <Name>Button1</Name>
                            </NameRef>
                        </Expression>
                        <Name>Size</Name>
                    </NameRef>
                </Expression>
                <Expression>
                    <NewClass>
                        <Type>System.Drawing.Size</Type>
                        <Argument>
                            <Expression>
                                <Literal>
                                    <Number type="System.Int32">75</Number>
                                </Literal>
                            </Expression>
                        </Argument>
                        <Argument>
                            <Expression>
                                <Literal>
                                    <Number type="System.Int32">23</Number>
                                </Literal>
                            </Expression>
                        </Argument>
                    </NewClass>
                </Expression>
            </Assignment>
        </Expression>
    </ExpressionStatement>
    <ExpressionStatement line="32">
        <Expression>
            <Assignment>
                <Expression>
                    <NameRef variablekind="property">
                        <Expression>
                            <NameRef variablekind="field">
                                <Expression>
                                    <ThisReference/>
                                </Expression>
                                <Name>Button1</Name>
                            </NameRef>
                        </Expression>
                        <Name>TabIndex</Name>
                    </NameRef>
                </Expression>
                <Expression>
                    <Literal>
                        <Number type="System.Int32">0</Number>
                    </Literal>
                </Expression>
            </Assignment>
        </Expression>
    </ExpressionStatement>
    <ExpressionStatement line="33">
        <Expression>
            <Assignment>
                <Expression>
                    <NameRef variablekind="property">
                        <Expression>
                            <NameRef variablekind="field">
                                <Expression>
                                    <ThisReference/>
                                </Expression>
                                <Name>Button1</Name>
                            </NameRef>
                        </Expression>
                        <Name>Text</Name>
                    </NameRef>
                </Expression>
                <Expression>
                    <Literal>
                        <String>Button1</String>
                    </Literal>
                </Expression>
            </Assignment>
        </Expression>
    </ExpressionStatement>
    <ExpressionStatement line="34">
        <Expression>
            <Assignment>
                <Expression>
                    <NameRef variablekind="property">
                        <Expression>
                            <NameRef variablekind="field">
                                <Expression>
                                    <ThisReference/>
                                </Expression>
                                <Name>Button1</Name>
                            </NameRef>
                        </Expression>
                        <Name>UseVisualStyleBackColor</Name>
                    </NameRef>
                </Expression>
                <Expression>
                    <Literal>
                        <Boolean>true</Boolean>
                    </Literal>
                </Expression>
            </Assignment>
        </Expression>
    </ExpressionStatement>
    <ExpressionStatement line="38">
        <Expression>
            <Assignment>
                <Expression>
                    <NameRef variablekind="property">
                        <Expression>
                            <ThisReference/>
                        </Expression>
                        <Name>AutoScaleDimensions</Name>
                    </NameRef>
                </Expression>
                <Expression>
                    <NewClass>
                        <Type>System.Drawing.SizeF</Type>
                        <Argument>
                            <Expression>
                                <Literal>
                                    <Number type="System.Single">6</Number>
                                </Literal>
                            </Expression>
                        </Argument>
                        <Argument>
                            <Expression>
                                <Literal>
                                    <Number type="System.Single">13</Number>
                                </Literal>
                            </Expression>
                        </Argument>
                    </NewClass>
                </Expression>
            </Assignment>
        </Expression>
    </ExpressionStatement>
    <ExpressionStatement line="39">
        <Expression>
            <Assignment>
                <Expression>
                    <NameRef variablekind="property">
                        <Expression>
                            <ThisReference/>
                        </Expression>
                        <Name>AutoScaleMode</Name>
                    </NameRef>
                </Expression>
                <Expression>
                    <NameRef variablekind="field">
                        <Expression>
                            <Literal>
                                <Type>System.Windows.Forms.AutoScaleMode</Type>
                            </Literal>
                        </Expression>
                        <Name>Font</Name>
                    </NameRef>
                </Expression>
            </Assignment>
        </Expression>
    </ExpressionStatement>
    <ExpressionStatement line="40">
        <Expression>
            <Assignment>
                <Expression>
                    <NameRef variablekind="property">
                        <Expression>
                            <ThisReference/>
                        </Expression>
                        <Name>ClientSize</Name>
                    </NameRef>
                </Expression>
                <Expression>
                    <NewClass>
                        <Type>System.Drawing.Size</Type>
                        <Argument>
                            <Expression>
                                <Literal>
                                    <Number type="System.Int32">284</Number>
                                </Literal>
                            </Expression>
                        </Argument>
                        <Argument>
                            <Expression>
                                <Literal>
                                    <Number type="System.Int32">262</Number>
                                </Literal>
                            </Expression>
                        </Argument>
                    </NewClass>
                </Expression>
            </Assignment>
        </Expression>
    </ExpressionStatement>
    <ExpressionStatement line="41">
        <Expression>
            <MethodCall>
                <Expression>
                    <NameRef variablekind="method">
                        <Expression>
                            <NameRef variablekind="property">
                                <Expression>
                                    <ThisReference/>
                                </Expression>
                                <Name>Controls</Name>
                            </NameRef>
                        </Expression>
                        <Name>Add</Name>
                    </NameRef>
                </Expression>
                <Argument>
                    <Expression>
                        <NameRef variablekind="field">
                            <Expression>
                                <ThisReference/>
                            </Expression>
                            <Name>Button1</Name>
                        </NameRef>
                    </Expression>
                </Argument>
            </MethodCall>
        </Expression>
    </ExpressionStatement>
    <ExpressionStatement line="42">
        <Expression>
            <Assignment>
                <Expression>
                    <NameRef variablekind="property">
                        <Expression>
                            <ThisReference/>
                        </Expression>
                        <Name>Name</Name>
                    </NameRef>
                </Expression>
                <Expression>
                    <Literal>
                        <String>Form1</String>
                    </Literal>
                </Expression>
            </Assignment>
        </Expression>
    </ExpressionStatement>
    <ExpressionStatement line="43">
        <Expression>
            <Assignment>
                <Expression>
                    <NameRef variablekind="property">
                        <Expression>
                            <ThisReference/>
                        </Expression>
                        <Name>Text</Name>
                    </NameRef>
                </Expression>
                <Expression>
                    <Literal>
                        <String>Form1</String>
                    </Literal>
                </Expression>
            </Assignment>
        </Expression>
    </ExpressionStatement>
    <ExpressionStatement line="44">
        <Expression>
            <MethodCall>
                <Expression>
                    <NameRef variablekind="method">
                        <Expression>
                            <ThisReference/>
                        </Expression>
                        <Name>ResumeLayout</Name>
                    </NameRef>
                </Expression>
                <Argument>
                    <Expression>
                        <Literal>
                            <Boolean>false</Boolean>
                        </Literal>
                    </Expression>
                </Argument>
            </MethodCall>
        </Expression>
    </ExpressionStatement>
</Block>

    End Class
End Namespace
