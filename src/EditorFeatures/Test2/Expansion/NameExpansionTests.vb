' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Expansion
    Public Class NameExpansionTests
        Inherits AbstractExpansionTest

#Region "C# Tests"

        <WorkItem(604392)>
        <Fact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Sub NoExpansionForPropertyNamesOfObjectInitializers()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class Program
{
    static void Main()
    {
        int z = 1;
        var c = new C { {|Expand:X|} = { Y = { z } } };
    }
}
 
class C
{
    public dynamic X;
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class Program
{
    static void Main()
    {
        int z = 1;
        var c = new C { X = { Y = { z } } };
    }
}

class C
{
    public dynamic X;
}
</code>

            Test(input, expected)
        End Sub

#End Region

    End Class
End Namespace
