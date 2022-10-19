' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Remote.Testing

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestParameterInMethod1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            void Goo(int {|Definition:$$i|})
            {
                Console.WriteLine([|i|]);
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestParameterInMethod2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            void Goo(int {|Definition:$$i|})
            {
                Console.WriteLine([|i|]);
            }

            void Bar(int i)
            {
                Console.WriteLine(i);
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestParameterInMethod3(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            void Goo(int {|Definition:$$i|})
            {
                Console.WriteLine([|i|]);
            }

            void Bar()
            {
                Goo([|i|]: 0);
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestParameterCaseSensitivity1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            void Goo(int {|Definition:$$i|})
            {
                Console.WriteLine([|i|]);
                Console.WriteLine(I);
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestParameterCaseSensitivity2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
        class C
            sub Goo(byval {|Definition:$$i|} as Integer)
                Console.WriteLine([|i|])
                Console.WriteLine([|I|])
            end sub
        end class
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(542475, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542475")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestPartialParameter1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
partial class program
{
    static partial void goo(string {|Definition:$$name|}, int age, bool sex, int index1 = 1) { }
}
partial class program
{
    static partial void goo(string {|Definition:name|}, int age, bool sex, int index1 = 1);
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(542475, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542475")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestPartialParameter2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
partial class program
{
    static partial void goo(string {|Definition:name|}, int age, bool sex, int index1 = 1) { }
}
partial class program
{
    static partial void goo(string {|Definition:$$name|}, int age, bool sex, int index1 = 1);
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

#Region "FAR on partial methods"

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestParameter_CSharpWithSignaturesMatchFARParameterOnDefDecl(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        partial class C
        {          
            partial void PM(int {|Definition:$$x|}, int y);
            partial void PM(int {|Definition:x|}, int y)
            {
                int s = [|x|];
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestParameter_VBWithSignaturesMatchFARParameterOnDefDecl(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
        Class C                
           partial sub PM({|Definition:x|} as Integer, y as Integer)
              
           End Sub

           sub PM({|Definition:x|} as Integer, y as Integer)
                Dim y as Integer = [|$$x|];;
           End Sub
        End Class
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

#End Region

        <WorkItem(543276, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543276")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAnonymousFunctionParameter1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
 
Module Program
    Sub Main
        Goo(Sub({|Definition:$$x|} As Integer) Return, Sub({|Definition:x|} As Integer) Return)
    End Sub
 
    Sub Goo(Of T)(x As T, y As T)
    End Sub
End Module
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(624310, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/624310")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAnonymousFunctionParameter3(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
 
Module Program
    
    Dim field As Object = If(True, Function({|Definition:$$x|} As String) [|x|].ToUpper(), Function({|Definition:x|} As String) [|x|].ToLower())

End Module
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(624310, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/624310")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAnonymousFunctionParameter4(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
class Program
{
    public object O = true ? (Func<string, string>)((string {|Definition:$$x|}) => {return [|x|].ToUpper(); }) : (Func<string, string>)((string x) => {return x.ToLower(); });
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(543276, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543276")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAnonymousFunctionParameter2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
 
Module Program
    Sub Main
        Goo(Sub({|Definition:x|} As Integer) Return, Sub({|Definition:$$x|} As Integer) Return)
    End Sub
 
    Sub Goo(Of T)(x As T, y As T)
    End Sub
End Module
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(529688, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529688")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAnonymousFunctionParameter5(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Module M
    Sub Main()
        Dim s = Sub({|Definition:$$x|}) Return
        s([|x|]:=1)
    End Sub
End Module
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(545654, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545654")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestReducedExtensionNamedParameter1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Option Strict On

Imports System
Imports System.Collections.Generic
Imports System.Runtime.CompilerServices

Module M
    Sub Main()
        Dim x As New Stack(Of String)
        Dim y = x.Goo(0, $$[|defaultValue|]:="")
    End Sub

    <Extension>
    Function Goo(x As Stack(Of String), index As Integer, {|Definition:defaultValue|} As String) As String
    End Function
End Module
        ]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(545654, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545654")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestReducedExtensionNamedParameter2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Option Strict On

Imports System
Imports System.Collections.Generic
Imports System.Runtime.CompilerServices

Module M
    Sub Main()
        Dim x As New Stack(Of String)
        Dim y = x.Goo(0, [|defaultValue|]:="")
    End Sub

    <Extension>
    Function Goo(x As Stack(Of String), index As Integer, {|Definition:$$defaultValue|} As String) As String
    End Function
End Module
        ]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(545618, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545618")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharp_TestAnonymousMethodParameter1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;

class X
{
    void Main()
    {
        Func<int, int> f = {|Definition:$$a|} => [|a|];
        Converter<int, int> c = a => a;
    }
}
        ]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(545618, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545618")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharp_TestAnonymousMethodParameter2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;

class X
{
    void Main()
    {
        Func<int, int> f = {|Definition:a|} => [|$$a|];
        Converter<int, int> c = a => a;
    }
}
        ]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(545618, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545618")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharp_TestAnonymousMethodParameter3(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;

class X
{
    void Main()
    {
        Func<int, int> f = a => a;
        Converter<int, int> c = {|Definition:$$a|} => [|a|];
    }
}
        ]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(545618, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545618")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharp_TestAnonymousMethodParameter4(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;

class X
{
    void Main()
    {
        Func<int, int> f = a => a;
        Converter<int, int> c = {|Definition:a|} => [|$$a|];
    }
}
        ]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(545618, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545618")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestVB_TestAnonymousMethodParameter1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Imports System

class X
    sub Main()
        dim f as Func(of integer, integer) = Function({|Definition:$$a|}) [|a|]
        dim c as Converter(of integer, integer) = Function(a) a
    end sub
end class
        ]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(545618, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545618")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestVB_TestAnonymousMethodParameter2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Imports System

class X
    sub Main()
        dim f as Func(of integer, integer) = Function({|Definition:a|}) [|$$a|]
        dim c as Converter(of integer, integer) = Function(a) a
    end sub
end class
        ]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(545618, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545618")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestVB_TestAnonymousMethodParameter3(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Imports System

class X
    sub Main()
        dim f as Func(of integer, integer) = Function(a) a
        dim c as Converter(of integer, integer) = Function({|Definition:$$a|}) [|a|]
    end sub
end class
        ]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(545618, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545618")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestVB_TestAnonymousMethodParameter4(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Imports System

class X
    sub Main()
        dim f as Func(of integer, integer) = Function(a) a
        dim c as Converter(of integer, integer) = Function({|Definition:a|}) [|$$a|]
    end sub
end class
        ]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(16757, "https://github.com/dotnet/roslyn/issues/16757")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        <CompilerTrait(CompilerFeature.LocalFunctions)>
        Public Async Function TestParameterInLocalFunction1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            void Main()
            {
                void Local(int {|Definition:$$i|})
                {
                    Console.WriteLine([|i|]);
                }

                Local(1);
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(16757, "https://github.com/dotnet/roslyn/issues/16757")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        <CompilerTrait(CompilerFeature.LocalFunctions)>
        Public Async Function TestParameterInLocalFunction2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            void Main()
            {
                void Local(int {|Definition:$$i|})
                {
                }

                Local([|i|]:1);
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(16757, "https://github.com/dotnet/roslyn/issues/16757")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        <CompilerTrait(CompilerFeature.LocalFunctions)>
        Public Async Function TestParameterInLocalFunction3(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            void Main()
            {
                void Local(int {|Definition:i|})
                {
                    Console.WriteLine([|$$i|]);
                }

                Local(1);
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(16757, "https://github.com/dotnet/roslyn/issues/16757")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        <CompilerTrait(CompilerFeature.LocalFunctions)>
        Public Async Function TestParameterInLocalFunction4(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            void Main()
            {
                void Local(int {|Definition:i|})
                {
                }

                Local([|$$i|]:1);
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestParameter_ValueUsageInfo(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            void Goo(int {|Definition:$$i|})
            {
                Console.WriteLine({|ValueUsageInfo.Read:[|i|]|});
                {|ValueUsageInfo.Write:[|i|]|} = 0;
                {|ValueUsageInfo.ReadWrite:[|i|]|}++;
                Goo2(in {|ValueUsageInfo.ReadableReference:[|i|]|}, ref {|ValueUsageInfo.ReadableWritableReference:[|i|]|});
                Goo3(out {|ValueUsageInfo.WritableReference:[|i|]|});
                Console.WriteLine(nameof({|ValueUsageInfo.Name:[|i|]|}));
            }

            void Goo2(in int j, ref int k)
            {
            }

            void Goo3(out int i)
            {
                i = 0;
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestParameterReferencedInSourceGeneratedDocument(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            public void Goo(int {|Definition:$$i|})
            {
            }
        }
        </Document>
        <DocumentFromSourceGenerator>
        class D
        {
            public void M()
            {
                new C().Goo([|i|]: 42);
            }
        }

        </DocumentFromSourceGenerator>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestParameterDefinedInSourceGeneratedDocument(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <DocumentFromSourceGenerator>
        class C
        {
            public void Goo(int {|Definition:$$i|})
            {
            }
        }
        </DocumentFromSourceGenerator>
        <Document>
        class D
        {
            public void M()
            {
                new C().Goo([|i|]: 42);
            }
        }

        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestRecordParameter1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

record Goo(int x, int {|Definition:$$y|})
{

}

class P
{
    static void Main()
    {
        var f = new Goo(0, [|y|]: 1);
        Console.WriteLine(f.[|y|]);
    }
}

        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestRecordParameter2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

record Goo(int x, int {|Definition:y|})
{

}

class P
{
    static void Main()
    {
        var f = new Goo(0, [|$$y|]: 1);
        Console.WriteLine(f.[|y|]);
    }
}

        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestRecordParameterWithExplicitProperty1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

record Goo(int x, int {|Definition:$$y|})
{
    public int y { get; } = [|y|];
}

class P
{
    static void Main()
    {
        var f = new Goo(0, [|y|]: 1);
        Console.WriteLine(f.y);
    }
}

        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestRecordParameterWithExplicitProperty2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

record Goo(int x, int {|Definition:y|})
{
    public int y { get; } = [|$$y|];
}

class P
{
    static void Main()
    {
        var f = new Goo(0, [|y|]: 1);
        Console.WriteLine(f.y);
    }
}

        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestRecordParameterWithExplicitProperty3(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

record Goo(int x, int {|Definition:y|})
{
    public int y { get; } = [|y|];
}

class P
{
    static void Main()
    {
        var f = new Goo(0, [|$$y|]: 1);
        Console.WriteLine(f.y);
    }
}

        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestRecordParameterWithNotPrimaryConstructor(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

record Goo(int x, int y)
{
    public Goo(int {|Definition:$$y|}) { }
}

class P
{
    static void Main()
    {
        var f = new Goo([|y|]: 1);
        Console.WriteLine(f.y);
    }
}

        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function
    End Class
End Namespace
