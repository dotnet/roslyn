' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.InlineHints
    <Trait(Traits.Feature, Traits.Features.InlineHints)>
    Public Class CSharpInlineParameterNameHintsTests
        Inherits AbstractInlineHintsTests

        <WpfFact>
        Public Async Function TestNoParameterSimpleCase() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    int testMethod()
    {
        return 5;
    }
    void Main() 
    {
        testMethod();
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, input)
        End Function

        <WpfFact>
        Public Async Function TestOneParameterSimpleCase() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    int testMethod(int x)
    {
        return x;
    }
    void Main() 
    {
        testMethod({|x:|}5);
    }
}
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    int testMethod(int x)
    {
        return x;
    }
    void Main() 
    {
        testMethod(x: 5);
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, output)
        End Function

        <WpfFact>
        Public Async Function TestTwoParametersSimpleCase() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    int testMethod(int x, double y)
    {
        return x;
    }
    void Main() 
    {
        testMethod({|x:|}5, {|y:|}2);
    }
}
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    int testMethod(int x, double y)
    {
        return x;
    }
    void Main() 
    {
        testMethod(x: 5, y: 2);
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, output)
        End Function

        <WpfFact>
        Public Async Function TestNegativeNumberParametersSimpleCase() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    int testMethod(int x, double y)
    {
        return x;
    }
    void Main() 
    {
        testMethod({|x:|}-5, {|y:|}2);
    }
}
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    int testMethod(int x, double y)
    {
        return x;
    }
    void Main() 
    {
        testMethod(x: -5, y: 2);
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, output)
        End Function

        <WpfFact>
        Public Async Function TestLiteralNestedCastParametersSimpleCase() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    int testMethod(int x, double y)
    {
        return x;
    }
    void Main() 
    {
        testMethod({|x:|}(int)(double)(int)5.5, {|y:|}2);
    }
}
                    </Document>
                </Project>
            </Workspace>

            Dim output =
           <Workspace>
               <Project Language="C#" CommonReferences="true">
                   <Document>
class A
{
    int testMethod(int x, double y)
    {
        return x;
    }
    void Main() 
    {
        testMethod(x: (int)(double)(int)5.5, y: 2);
    }
}
                    </Document>
               </Project>
           </Workspace>

            Await VerifyParamHints(input, output)
        End Function

        <WpfFact>
        Public Async Function TestObjectCreationParametersSimpleCase() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    int testMethod(int x, object y)
    {
        return x;
    }
    void Main() 
    {
        testMethod({|x:|}(int)5.5, {|y:|}new object());
    }
}
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    int testMethod(int x, object y)
    {
        return x;
    }
    void Main() 
    {
        testMethod(x: (int)5.5, y: new object());
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, output)
        End Function

        <WpfFact>
        Public Async Function TestCastingANegativeSimpleCase() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    int testMethod(int x, object y)
    {
        return x;
    }
    void Main() 
    {
        testMethod({|x:|}(int)-5.5, {|y:|}new object());
    }
}
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    int testMethod(int x, object y)
    {
        return x;
    }
    void Main() 
    {
        testMethod(x: (int)-5.5, y: new object());
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, output)
        End Function

        <WpfFact>
        Public Async Function TestNegatingACastSimpleCase() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    int testMethod(int x, object y)
    {
        return x;
    }
    void Main() 
    {
        testMethod({|x:|}-(int)5.5, {|y:|}new object());
    }
}
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    int testMethod(int x, object y)
    {
        return x;
    }
    void Main() 
    {
        testMethod(x: -(int)5.5, y: new object());
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, output)
        End Function

        <WpfFact>
        Public Async Function TestMissingParameterNameSimpleCase() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    int testMethod(int)
    {
        return 5;
    }
    void Main() 
    {
        testMethod();
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, input)
        End Function

        <WpfFact>
        Public Async Function TestDelegateParameter() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
delegate void D(int x);

class C
{
    public static void M1(int i) { }
}

class Test
{
    static void Main()
    {
        D cd1 = new D(C.M1);
        cd1({|x:|}-1);
    }
}
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
delegate void D(int x);

class C
{
    public static void M1(int i) { }
}

class Test
{
    static void Main()
    {
        D cd1 = new D(C.M1);
        cd1(x: -1);
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, output)
        End Function

        <WpfFact>
        Public Async Function TestFunctionPointerNoParameter() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true" AllowUnsafe="true">
                    <Document>
unsafe class Example {
    void Example(delegate*&lt;int, void&gt; f) {
        f(42);
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, input)
        End Function

        <WpfFact>
        Public Async Function TestParamsArgument() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    public void UseParams(params int[] list)
    {
    }

    public void Main(string[] args)
    {
        UseParams({|list:|}1, 2, 3, 4, 5, 6); 
    } 
}
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    public void UseParams(params int[] list)
    {
    }

    public void Main(string[] args)
    {
        UseParams(1, 2, 3, 4, 5, 6); 
    } 
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, output)
        End Function

        <WpfFact>
        Public Async Function TestAttributesArgument() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
using System;

[Obsolete({|message:|}"test")]
class Foo
{
        

}
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
using System;

[Obsolete(message: "test")]
class Foo
{
        

}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, output)
        End Function

        <WpfFact>
        Public Async Function TestIncompleteFunctionCall() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    int testMethod(int x, object y)
    {
        return x;
    }
    void Main() 
    {
        testMethod({|x:|}-(int)5.5,);
    }
}
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    int testMethod(int x, object y)
    {
        return x;
    }
    void Main() 
    {
        testMethod(x: -(int)5.5,);
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, output)
        End Function

        <WpfFact>
        Public Async Function TestInterpolatedString() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    string testMethod(string x)
    {
        return x;
    }
    void Main() 
    {
        testMethod({|x:|}$"");
    }
}
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    string testMethod(string x)
    {
        return x;
    }
    void Main() 
    {
        testMethod(x: $"");
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, output)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47696")>
        Public Async Function TestRecordBaseType() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
record Base(int Alice, int Bob);
record Derived(int Other) : Base({|Alice:|}2, {|Bob:|}2);
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
record Base(int Alice, int Bob);
record Derived(int Other) : Base(Alice: 2, Bob: 2);
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, output)
        End Function

        <WpfFact>
        Public Async Function TestClassBaseType_01() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class Base(int Alice, int Bob);
class Derived(int Other) : Base({|Alice:|}2, {|Bob:|}2);
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class Base(int Alice, int Bob);
class Derived(int Other) : Base(Alice: 2, Bob: 2);
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, output)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47696")>
        Public Async Function TestClassBaseType() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class Base
{
    public Base(int paramName) {}
}
class Derived : Base
{
    public Derived() : base({|paramName:|}20) {}
}
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class Base
{
    public Base(int paramName) {}
}
class Derived : Base
{
    public Derived() : base(paramName: 20) {}
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, output)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47597")>
        Public Async Function TestNotOnEnableDisableBoolean1() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    void EnableLogging(bool value)
    {
    }

    void Main() 
    {
        EnableLogging(true);
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, input)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47597")>
        Public Async Function TestNotOnEnableDisableBoolean2() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    void DisableLogging(bool value)
    {
    }

    void Main() 
    {
        DisableLogging(true);
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, input)
        End Function

        <WorkItem(47597, "https://github.com/dotnet/roslyn/issues/60145")>
        <WpfFact>
        Public Async Function TestNotOnEnableDisableBoolean3() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    void Main() 
    {
        EnableLogging(true);

        void EnableLogging(bool value)
        {
        }
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, input)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47597")>
        Public Async Function TestOnEnableDisableNonBoolean1() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    void EnableLogging(string value)
    {
    }

    void Main() 
    {
        EnableLogging({|value:|}"IO");
    }
}
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    void EnableLogging(string value)
    {
    }

    void Main() 
    {
        EnableLogging(value: "IO");
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, output)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47597")>
        Public Async Function TestOnEnableDisableNonBoolean2() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    void DisableLogging(string value)
    {
    }

    void Main() 
    {
        DisableLogging({|value:|}"IO");
    }
}
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    void DisableLogging(string value)
    {
    }

    void Main() 
    {
        DisableLogging(value: "IO");
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, output)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47597")>
        Public Async Function TestOnSetMethodWithClearContext() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    void SetClassification(string classification)
    {
    }

    void Main() 
    {
        SetClassification("IO");
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, input)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47597")>
        Public Async Function TestOnSetMethodWithUnclearContext() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    void SetClassification(string values)
    {
    }

    void Main() 
    {
        SetClassification({|values:|}"IO");
    }
}
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    void SetClassification(string values)
    {
    }

    void Main() 
    {
        SetClassification(values: "IO");
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, output)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47597")>
        Public Async Function TestMethodWithAlphaSuffix1() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    void Goo(int objA, int objB, int objC)
    {
    }

    void Main() 
    {
        Goo(1, 2, 3);
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, input)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47597")>
        Public Async Function TestMethodWithNonAlphaSuffix1() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    void Goo(int objA, int objB, int nonobjC)
    {
    }

    void Main() 
    {
        Goo({|objA:|}1, {|objB:|}2, {|nonobjC:|}3);
    }
}
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    void Goo(int objA, int objB, int nonobjC)
    {
    }

    void Main() 
    {
        Goo(objA: 1, objB: 2, nonobjC: 3);
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, output)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47597")>
        Public Async Function TestMethodWithNumericSuffix1() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    void Goo(int obj1, int obj2, int obj3)
    {
    }

    void Main() 
    {
        Goo(1, 2, 3);
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, input)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47597")>
        Public Async Function TestMethodWithNonNumericSuffix1() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    void Goo(int obj1, int obj2, int nonobj3)
    {
    }

    void Main() 
    {
        Goo({|obj1:|}1, {|obj2:|}2, {|nonobj3:|}3);
    }
}
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    void Goo(int obj1, int obj2, int nonobj3)
    {
    }

    void Main() 
    {
        Goo(obj1: 1, obj2: 2, nonobj3: 3);
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, output)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/48910")>
        Public Async Function TestNullableSuppression() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
#nullable enable

class A
{
    void M(string x)
    {
    }

    void Main() 
    {
        M({|x:|}null!);
    }
}
                    </Document>
                </Project>
            </Workspace>

            Dim output =
           <Workspace>
               <Project Language="C#" CommonReferences="true">
                   <Document>
#nullable enable

class A
{
    void M(string x)
    {
    }

    void Main() 
    {
        M(x: null!);
    }
}
                    </Document>
               </Project>
           </Workspace>

            Await VerifyParamHints(input, output)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/46614")>
        Public Async Function TestIndexerParameter() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
public class TempRecord
{
    // Array of temperature values
    float[] temps = new float[10]
    {
        56.2F, 56.7F, 56.5F, 56.9F, 58.8F,
        61.3F, 65.9F, 62.1F, 59.2F, 57.5F
    };

    // To enable client code to validate input
    // when accessing your indexer.
    public int Length => temps.Length;
    
    // Indexer declaration.
    // If index is out of range, the temps array will throw the exception.
    public float this[int index]
    {
        get => temps[index];
        set => temps[index] = value;
    }
}

class Program
{
    static void Main()
    {
        var tempRecord = new TempRecord();

        // Use the indexer's set accessor
        var temp = tempRecord[{|index:|}3];
    }
}
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
public class TempRecord
{
    // Array of temperature values
    float[] temps = new float[10]
    {
        56.2F, 56.7F, 56.5F, 56.9F, 58.8F,
        61.3F, 65.9F, 62.1F, 59.2F, 57.5F
    };

    // To enable client code to validate input
    // when accessing your indexer.
    public int Length => temps.Length;
    
    // Indexer declaration.
    // If index is out of range, the temps array will throw the exception.
    public float this[int index]
    {
        get => temps[index];
        set => temps[index] = value;
    }
}

class Program
{
    static void Main()
    {
        var tempRecord = new TempRecord();

        // Use the indexer's set accessor
        var temp = tempRecord[index: 3];
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, output)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/66817")>
        Public Async Function TestParameterNameIsReservedKeyword() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class C
{
    void M()
    {
        N({|int:|}0);
    }

    void N(int @int)
    {
    }
}
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class C
{
    void M()
    {
        N(@int: 0);
    }

    void N(int @int)
    {
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, output)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/66817")>
        Public Async Function TestParameterNameIsContextualKeyword() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class C
{
    void M()
    {
        N({|async:|}true);
    }

    void N(bool async)
    {
    }
}
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class C
{
    void M()
    {
        N(async: true);
    }

    void N(bool async)
    {
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, output)
        End Function

        <WpfFact>
        Public Async Function TestOnlyProduceTagsWithinSelection() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    int testMethod(int a, int b, int c, int d, int e)
    {
        return x;
    }
    void Main() 
    {
        testMethod(1, [|{|b:|}2, {|c:|}3, {|d:|}4|], 5);
    }
}
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    int testMethod(int a, int b, int c, int d, int e)
    {
        return x;
    }
    void Main() 
    {
        testMethod(1, b: 2, c: 3, d: 4, 5);
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, output)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/59317")>
        Public Async Function TestExistingNamedParameter1() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class C
{
    static void Main(string[] args)
    {
        Goo({|a:|}1, 2, b: 0);
    }

    static void Goo(int a, int b)
    {

    }
}
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class C
{
    static void Main(string[] args)
    {
        Goo(a: 1, 2, b: 0);
    }

    static void Goo(int a, int b)
    {

    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, output)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/59317")>
        Public Async Function TestExistingNamedParameter2() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class C
{
    static void Main(string[] args)
    {
        Goo({|a:|}1, {|b:|}2, c: 0);
    }

    static void Goo(int a, int b)
    {

    }
}
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class C
{
    static void Main(string[] args)
    {
        Goo(a: 1, b: 2, c: 0);
    }

    static void Goo(int a, int b)
    {

    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, output)
        End Function

        <WpfFact>
        Public Async Function TestSuppressForCaseInsensitiveMatch() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    void fn(int fooBar)
    {
    }

    void Main()
    {
        int FooBar = 5;
        fn(FooBar);
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, input)
        End Function

        <WpfFact>
        Public Async Function TestSuppressForUnderscorePrefixMatch() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    void fn(int fooBar)
    {
    }

    void Main()
    {
        int _fooBar = 5;
        fn(_fooBar);
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, input)
        End Function

        <WpfFact>
        Public Async Function TestSuppressForUnderscoreInNameMatch() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    void fn(int fooBar)
    {
    }

    void Main()
    {
        int FOO_BAR = 5;
        fn(FOO_BAR);
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, input)
        End Function

        <WpfFact>
        Public Async Function TestSuppressForParameterDifferingByTrailingUnderscore() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    void fn(int fooBar)
    {
    }

    void Main()
    {
        int fooBar_ = 5;
        fn(fooBar_);
    }
}
                    </Document>
                </Project>
            </Workspace>

            Dim options = New InlineParameterHintsOptions() With
            {
                .EnabledForParameters = True,
                .SuppressForParametersThatDifferOnlyBySuffix = True
            }

            Await VerifyParamHintsWithOptions(input, input, options)
        End Function

        <WpfFact>
        Public Async Function TestSuppressForArgumentDifferingByTrailingUnderscore() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    void fn(int fooBar_)
    {
    }

    void Main()
    {
        int fooBar = 5;
        fn(fooBar);
    }
}
                    </Document>
                </Project>
            </Workspace>

            Dim options = New InlineParameterHintsOptions() With
            {
                .EnabledForParameters = True,
                .SuppressForParametersThatDifferOnlyBySuffix = True
            }

            Await VerifyParamHintsWithOptions(input, input, options)
        End Function

        <WpfFact>
        Public Async Function TestSuppressForMemberAccessMatch() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class Vector2
{
    public int X;
    public int Y;
}

class A
{
    void Create(int x, int y)
    {
    }

    void Main()
    {
        var foo = new Vector2();
        Create(foo.X, foo.Y);
    }
}
                    </Document>
                </Project>
            </Workspace>

            Dim options = New InlineParameterHintsOptions() With
            {
                .EnabledForParameters = True,
                .SuppressForParametersThatMatchMemberName = True
            }

            Await VerifyParamHintsWithOptions(input, input, options)
        End Function

        <WpfFact>
        Public Async Function TestSuppressForMemberAccessMatchCaseInsensitive() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class Vector2
{
    public int x;
    public int y;
}

class A
{
    void Create(int X, int Y)
    {
    }

    void Main()
    {
        var foo = new Vector2();
        Create(foo.x, foo.y);
    }
}
                    </Document>
                </Project>
            </Workspace>

            Dim options = New InlineParameterHintsOptions() With
            {
                .EnabledForParameters = True,
                .SuppressForParametersThatMatchMemberName = True
            }

            Await VerifyParamHintsWithOptions(input, input, options)
        End Function
    End Class
End Namespace
