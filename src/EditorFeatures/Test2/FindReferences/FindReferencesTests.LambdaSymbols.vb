' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
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

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
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

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
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

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
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

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
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

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
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
