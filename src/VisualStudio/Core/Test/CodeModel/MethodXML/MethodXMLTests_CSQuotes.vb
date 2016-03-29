' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.MethodXML
    Partial Public Class MethodXMLTests

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Async Function TestCSQuotes_ForLoopAndComments() As Task
            Dim definition =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document>
public class C
{
    $$void M()
    { // Foo
        int i = 0; // comment after local
        // hello comment!
        for (int i = 0; i &lt; 10; i++) // Foo
        {

        } // Foo2
        // Foo3
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
    <Quote line="7">for (int i = 0; i &lt; 10; i++) // Foo
        {

        }</Quote>
    <Comment> Foo3</Comment>
</Block>

            Await TestAsync(definition, expected)
        End Function

    End Class
End Namespace
