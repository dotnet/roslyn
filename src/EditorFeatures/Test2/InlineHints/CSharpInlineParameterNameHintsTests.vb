' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.InlineHints
    Public Class CSharpInlineParameterNameHintsTests
        Inherits AbstractInlineHintsTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineHints)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineHints)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineHints)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineHints)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineHints)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineHints)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineHints)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineHints)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineHints)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineHints)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineHints)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineHints)>
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
        UseParams(list: 1, 2, 3, 4, 5, 6); 
    } 
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, output)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineHints)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineHints)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineHints)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineHints)>
        <WorkItem(47696, "https://github.com/dotnet/roslyn/issues/47696")>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineHints)>
        <WorkItem(47696, "https://github.com/dotnet/roslyn/issues/47696")>
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

        <WorkItem(47597, "https://github.com/dotnet/roslyn/issues/47597")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineHints)>
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

        <WorkItem(47597, "https://github.com/dotnet/roslyn/issues/47597")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineHints)>
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
        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineHints)>
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

        <WorkItem(47597, "https://github.com/dotnet/roslyn/issues/47597")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineHints)>
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

        <WorkItem(47597, "https://github.com/dotnet/roslyn/issues/47597")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineHints)>
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

        <WorkItem(47597, "https://github.com/dotnet/roslyn/issues/47597")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineHints)>
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

        <WorkItem(47597, "https://github.com/dotnet/roslyn/issues/47597")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineHints)>
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

        <WorkItem(47597, "https://github.com/dotnet/roslyn/issues/47597")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineHints)>
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

        <WorkItem(47597, "https://github.com/dotnet/roslyn/issues/47597")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineHints)>
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

        <WorkItem(47597, "https://github.com/dotnet/roslyn/issues/47597")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineHints)>
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

        <WorkItem(47597, "https://github.com/dotnet/roslyn/issues/47597")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineHints)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineHints)>
        <WorkItem(48910, "https://github.com/dotnet/roslyn/issues/48910")>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineHints)>
        <WorkItem(46614, "https://github.com/dotnet/roslyn/issues/46614")>
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
    End Class
End Namespace
