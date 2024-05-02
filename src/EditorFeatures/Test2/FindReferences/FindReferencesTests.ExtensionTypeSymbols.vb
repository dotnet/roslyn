' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Remote.Testing

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    <Trait(Traits.Feature, Traits.Features.FindReferences)>
    Partial Public Class FindReferencesTests
        <WpfTheory, CombinatorialData>
        Public Async Function TestExtensionType1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
using System.Linq;
 
implicit extension E for Console
{
    public static void $${|Definition:M|}() { }
}

class Program
{
    static void Main()
    {
        Console.[|M|]();
    }
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestExtensionType2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
using System.Linq;
 
implicit extension E for Console
{
    public static void {|Definition:M|}() { }
}

class Program
{
    static void Main()
    {
        Console.[|$$M|]();
    }
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestExtensionType3(kind As TestKind, host As TestHost) As Task
            Dim input =
    <Workspace>
        <Project Language="C#" CommonReferences="true">
            <Document><![CDATA[
using System;
using System.Linq;
 
implicit extension E for $$[|Console|]
{
    public static void M() { }
}

class Program
{
    static void Main()
    {
        [|Console|].M();
    }
}]]>
            </Document>
        </Project>
    </Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function
    End Class
End Namespace
