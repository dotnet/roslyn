' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestLambdaParameterDefinition()
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
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestLambdaParameterReferencesInDifferentLambdas1()
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
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestLambdaParameterReferencesInDifferentLambdas2()
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
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestLambdaParameterReferencesInDifferentLambdas3()
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
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestLambdaParameterReferencesInDifferentLambdas4()
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
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestLambdaParameterReferencesInDifferentLambdas5()
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
            Test(input)
        End Sub
    End Class
End Namespace
