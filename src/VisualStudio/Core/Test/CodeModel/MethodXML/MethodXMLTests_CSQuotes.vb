' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.MethodXML
    Partial Public Class MethodXMLTests

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestCSQuotes_ForLoopAndComments()
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
public class C
{
    $$void M()
    { // Goo
        int i = 0; // comment after local
        // hello comment!
        for (int i = 0; i &lt; 10; i++) // Goo
        {

        } // Goo2
        // Goo3
    }
}
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <Local line="5">
        <Type>System.Int32</Type>
        <Name>i</Name>
        <Expression>
            <Literal>
                <Number type="System.Int32">0</Number>
            </Literal>
        </Expression>
    </Local>
    <Comment> hello comment!</Comment>
    <Quote line="7">for (int i = 0; i &lt; 10; i++) // Goo
        {

        }</Quote>
    <Comment> Goo3</Comment>
</Block>

            Test(definition, expected)
        End Sub

    End Class
End Namespace
