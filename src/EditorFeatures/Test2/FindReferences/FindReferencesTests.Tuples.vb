' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Remote.Testing

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    <Trait(Traits.Feature, Traits.Features.FindReferences)>
    Partial Public Class FindReferencesTests
        Private ReadOnly tuple2 As XCData =
        <![CDATA[
namespace System
{
    // struct with two values
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }

        public override string ToString()
        {
            return '{' + Item1?.ToString() + "", "" + Item2?.ToString() + '}';
        }
    }
}
]]>

        <WpfTheory, CombinatorialData>
        Public Async Function TestTupleFieldSameTuples01(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[

    class Program
    {
        static void Main(string[] args)
        {
            var x = ({|Definition:[|Alice|]|}:1, Bob: 2);
            var y = ([|Alice|]:1, Bob: 2);

            var z = x.[|$$Alice|];
        }
    }

        ]]>
            <%= tuple2 %>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestTupleFieldSameTuples02(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[

    class Program
    {
        static void Main(string[] args)
        {
            var x = ({|Definition:[|$$Alice|]|}:1, Bob: 2);
            var y = ([|Alice|]:1, Bob: 2);

            var z = x.[|Alice|];
        }
    }

        ]]>
            <%= tuple2 %>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestTupleFieldSameTuples03(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[

    class Program
    {
        static void Main(string[] args)
        {
            var x = ([|Alice|]:1, Bob: 2);
            var y = ({|Definition:[|$$Alice|]|}:1, Bob: 2);

            var z = x.[|Alice|];
        }
    }

        ]]>
            <%= tuple2 %>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestTupleFieldSameTuplesMultidocument(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[

    class Program1
    {
        static void Main(string[] args)
        {
            // this is odd that these are considered references, but that is 
            // how it works for now.
            // NOTE: types like (int Alice, int Bob) may have distinct declaring references,
            //       but are also equal, in the symbol equality sense, types.
            var x = ([|Alice|]:1, Bob: 2);
            var y = ([|Alice|]:1, Bob: 2);

            var z = x.[|Alice|];
        }
    }

        ]]>
            <%= tuple2 %>
        </Document>
        <Document><![CDATA[

    class Program
    {
        static void Main(string[] args)
        {
            var x = ([|Alice|]:1, Bob: 2);
            var y = ({|Definition:[|$$Alice|]|}:1, Bob: 2);

            var z = x.[|Alice|];
        }
    }

        ]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestTupleFieldDifferentTuples01(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[

    class Program
    {
        static void Main(string[] args)
        {
            var x = ({|Definition:[|Alice|]|}:1, Bob: 2);
            var y = (Alice:1.1, Bob: 2);

            var z = x.[|$$Alice|];
        }
    }

        ]]>
            <%= tuple2 %>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestTupleFieldSameTuplesMatchOuterSymbols01(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[

    class Program
    {
        static void Main(string[] args)
        {
            var x = ({|Definition:[|Program|]|}:1, Main: 2);
            var y = ([|Program|]:1, Main: 2);

            var z = x.[|$$Program|];
        }
    }

        ]]>
            <%= tuple2 %>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestTupleFieldSameTuplesMatchOuterSymbols02(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[

    class Program
    {
        static void Main(string[] args)
        {
            var x = (1,2,3,4,5,6,7,8,9,10,11,{|Definition:[|Program|]|}:1, Main: 2);
            var x1 = (1,2,3,4,5,6,7,8,9,10,11,[|Program|]:1, Main: 2);
            var y = (Program:1, Main: 2);

            var z = x.[|$$Program|];
        }
    }

        ]]>
            <%= tuple2 %>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestTupleFieldItem01(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[

    class Program
    {
        static void Main(string[] args)
        {
            var x = ({|Definition:Alice|}:1, Bob: 2);
            var y = (Alice:1, Bob: 2);

            var z = x.$$[|Item1|];
        }
    }

        ]]>
            <%= tuple2 %>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestTupleFieldItem02(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[

    class Program
    {
        static void Main(string[] args)
        {
            var x = ($$[|{|Definition:Alice|}|]:1, Bob: 2);
            var y = ([|Alice|]:1, Bob: 2);

            var z = x.Item1;
        }
    }

        ]]>
            <%= tuple2 %>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestTupleFieldItem03(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[

    class Program
    {
        static void Main(string[] args)
        {
            var x = ({|Definition:1|}, Bob: 2);
            var y = (Alice:1, Bob: 2);

            var z = x.$$[|Item1|];
            var z1 = x.[|Item1|];
        }
    }

        ]]>
            <%= tuple2 %>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestTupleFieldItem04(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[

    class Program
    {
        static void Main(string[] args)
        {
            System.ValueTuple<int, int> x = (1, Bob: 2);
            var y = (Alice:1, Bob: 2);

            var z = x.$$[|Item1|];
            var z1 = x.[|Item1|];
        }
    }

        ]]>
            <%= tuple2 %>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/41598")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestTuplesAcrossCoreAndStandard1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferencesNetCoreApp="true">
        <Document><![CDATA[
using System;

class Program
{
    static void Main(string[] args)
    {
    }

    public [|ValueTuple|]<int, int> Method() => default;
}
]]>
        </Document>
    </Project>
    <Project Language="C#" CommonReferencesNetStandard20="true">
        <Document><![CDATA[
using System;

class Program
{
    static void Test()
    {
        $$var a = (1, 1);
    }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestTuplesUseInSourceGeneratedDocument(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferencesNetCoreApp="true">
        <Document><![CDATA[
using System;

partial class Program
{
    static void Main(string[] args)
    {
    }

    public [|ValueTuple|]<int, int> Method() => default;
}
]]>
        </Document>
        <DocumentFromSourceGenerator><![CDATA[
using System;

partial class Program
{
    static void Test()
    {
        $$var a = (1, 1);
    }
}
]]>
        </DocumentFromSourceGenerator>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function
    End Class
End Namespace
