' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestParameterInMethod1()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            void Foo(int {|Definition:$$i|})
            {
                Console.WriteLine([|i|]);
            }
        }
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestParameterInMethod2()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            void Foo(int {|Definition:$$i|})
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
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestParameterInMethod3()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            void Foo(int {|Definition:$$i|})
            {
                Console.WriteLine([|i|]);
            }

            void Bar()
            {
                Foo([|i|]: 0);
            }
        }
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestParameterCaseSensitivity1()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            void Foo(int {|Definition:$$i|})
            {
                Console.WriteLine([|i|]);
                Console.WriteLine(I);
            }
        }
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestParameterCaseSensitivity2()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
        class C
            sub Foo(byval {|Definition:$$i|} as Integer)
                Console.WriteLine([|i|])
                Console.WriteLine([|I|])
            end sub
        end class
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WorkItem(542475)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestPartialParameter1()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
partial class program
{
    static partial void foo(string {|Definition:$$name|}, int age, bool sex, int index1 = 1) { }
}
partial class program
{
    static partial void foo(string {|Definition:name|}, int age, bool sex, int index1 = 1);
}
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WorkItem(542475)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestPartialParameter2()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
partial class program
{
    static partial void foo(string {|Definition:name|}, int age, bool sex, int index1 = 1) { }
}
partial class program
{
    static partial void foo(string {|Definition:$$name|}, int age, bool sex, int index1 = 1);
}
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

#Region "FAR on partial methods"

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestParameter_CSharpWithSignaturesMatchFARParameterOnDefDecl()
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
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestParameter_VBWithSignaturesMatchFARParameterOnDefDecl()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
        Class C                
           partial sub PM(x as Integer, y as Integer)
              
           End Sub
           partial sub PM({|Definition:x|} as Integer, y as Integer)
                Dim y as Integer = [|$$x|];;
           End Sub
        End Class
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

#End Region

        <WorkItem(543276)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestAnonymousFunctionParameter1()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
 
Module Program
    Sub Main
        Foo(Sub({|Definition:$$x|} As Integer) Return, Sub({|Definition:x|} As Integer) Return)
    End Sub
 
    Sub Foo(Of T)(x As T, y As T)
    End Sub
End Module
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WorkItem(624310)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestAnonymousFunctionParameter3()
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
            Test(input)
        End Sub

        <WorkItem(624310)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestAnonymousFunctionParameter4()
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
            Test(input)
        End Sub

        <WorkItem(543276)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestAnonymousFunctionParameter2()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
 
Module Program
    Sub Main
        Foo(Sub({|Definition:x|} As Integer) Return, Sub({|Definition:$$x|} As Integer) Return)
    End Sub
 
    Sub Foo(Of T)(x As T, y As T)
    End Sub
End Module
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WorkItem(529688)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestAnonymousFunctionParameter5()
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
            Test(input)
        End Sub

        <WorkItem(545654)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestReducedExtensionNamedParameter1()
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
        Dim y = x.Foo(0, $$[|defaultValue|]:="")
    End Sub

    <Extension>
    Function Foo(x As Stack(Of String), index As Integer, {|Definition:defaultValue|} As String) As String
    End Function
End Module
        ]]></Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WorkItem(545654)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestReducedExtensionNamedParameter2()
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
        Dim y = x.Foo(0, [|defaultValue|]:="")
    End Sub

    <Extension>
    Function Foo(x As Stack(Of String), index As Integer, {|Definition:$$defaultValue|} As String) As String
    End Function
End Module
        ]]></Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WorkItem(545618)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub CSharp_TestAnonymousMethodParameter1()
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
            Test(input)
        End Sub

        <WorkItem(545618)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub CSharp_TestAnonymousMethodParameter2()
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
            Test(input)
        End Sub

        <WorkItem(545618)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub CSharp_TestAnonymousMethodParameter3()
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
            Test(input)
        End Sub

        <WorkItem(545618)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub CSharp_TestAnonymousMethodParameter4()
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
            Test(input)
        End Sub

        <WorkItem(545618)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub VB_TestAnonymousMethodParameter1()
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
            Test(input)
        End Sub

        <WorkItem(545618)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub VB_TestAnonymousMethodParameter2()
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
            Test(input)
        End Sub

        <WorkItem(545618)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub VB_TestAnonymousMethodParameter3()
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
            Test(input)
        End Sub

        <WorkItem(545618)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub VB_TestAnonymousMethodParameter4()
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
            Test(input)
        End Sub
    End Class
End Namespace
