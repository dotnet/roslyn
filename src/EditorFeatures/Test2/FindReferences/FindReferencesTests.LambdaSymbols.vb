' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Remote.Testing

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    <Trait(Traits.Feature, Traits.Features.FindReferences)>
    Partial Public Class FindReferencesTests
        <WpfTheory, CombinatorialData>
        Public Async Function TestLambdaParameterDefinition(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
using System;

class Program
{
    static void Main(string[] args)
    {
        Func<int, int> f = {|Definition:$$x|} => [|x|] + 1;
    }
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestLambdaParameterReferencesInDifferentLambdas1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;
using System.Linq;

class Program
{
    void Main()
    {
        string csv = "";
        var prices = csv.Split('\n').Skip(1)
                        .Select(line => line.Split(','))
                        .Where({|Definition:$$values|} => [|values|].Length == 7)
                        .Select(values => 
                            Tuple.Create(DateTime.Parse(values[0]),
                                         float.Parse(values[6])));

    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestLambdaParameterReferencesInDifferentLambdas2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;
using System.Linq;

class Program
{
    void Main()
    {
        string csv = "";
        var prices = csv.Split('\n').Skip(1)
                        .Select(line => line.Split(','))
                        .Where({|Definition:values|} => [|$$values|].Length == 7)
                        .Select(values => 
                            Tuple.Create(DateTime.Parse(values[0]),
                                         float.Parse(values[6])));

    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestLambdaParameterReferencesInDifferentLambdas3(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;
using System.Linq;

class Program
{
    void Main()
    {
        string csv = "";
        var prices = csv.Split('\n').Skip(1)
                        .Select(line => line.Split(','))
                        .Where(values => values.Length == 7)
                        .Select({|Definition:$$values|} => 
                            Tuple.Create(DateTime.Parse([|values|][0]),
                                         float.Parse([|values|][6])));

    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestLambdaParameterReferencesInDifferentLambdas4(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;
using System.Linq;

class Program
{
    void Main()
    {
        string csv = "";
        var prices = csv.Split('\n').Skip(1)
                        .Select(line => line.Split(','))
                        .Where(values => values.Length == 7)
                        .Select({|Definition:values|} => 
                            Tuple.Create(DateTime.Parse([|$$values|][0]),
                                         float.Parse([|values|][6])));

    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestLambdaParameterReferencesInDifferentLambdas5(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;
using System.Linq;

class Program
{
    void Main()
    {
        string csv = "";
        var prices = csv.Split('\n').Skip(1)
                        .Select(line => line.Split(','))
                        .Where(values => values.Length == 7)
                        .Select({|Definition:values|} => 
                            Tuple.Create(DateTime.Parse([|values|][0]),
                                         float.Parse([|$$values|][6])));

    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function
    End Class
End Namespace
