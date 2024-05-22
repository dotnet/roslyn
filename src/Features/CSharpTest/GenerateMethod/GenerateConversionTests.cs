// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.GenerateMethod;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.GenerateMethod
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
    public class GenerateConversionTest : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
    {
        public GenerateConversionTest(ITestOutputHelper logger)
             : base(logger)
        {
        }

        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new GenerateConversionCodeFixProvider());

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")]
        public async Task TestGenerateImplicitConversionGenericClass()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Test(int[] a)
    {
        C<int> x1 = [|1|];
    }
}

class C<T>
{
}",
@"using System;

class Program
{
    void Test(int[] a)
    {
        C<int> x1 = 1;
    }
}

class C<T>
{
    public static implicit operator C<T>(int v)
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")]
        public async Task TestGenerateImplicitConversionClass()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Test(int[] a)
    {
        C x1 = [|1|];
    }
}

class C
{
}",
@"using System;

class Program
{
    void Test(int[] a)
    {
        C x1 = 1;
    }
}

class C
{
    public static implicit operator C(int v)
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact]
        public async Task TestGenerateImplicitConversionClass_CodeStyle()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Test(int[] a)
    {
        C x1 = [|1|];
    }
}

class C
{
}",
@"using System;

class Program
{
    void Test(int[] a)
    {
        C x1 = 1;
    }
}

class C
{
    public static implicit operator C(int v) => throw new NotImplementedException();
}",
options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedOperators, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")]
        public async Task TestGenerateImplicitConversionAwaitExpression()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Threading.Tasks;

class Program
{
    async void Test()
    {
        var a = Task.FromResult(1);
        Program x1 = [|await a|];
    }
}",
@"using System;
using System.Threading.Tasks;

class Program
{
    async void Test()
    {
        var a = Task.FromResult(1);
        Program x1 = await a;
    }

    public static implicit operator Program(int v)
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")]
        public async Task TestGenerateImplicitConversionTargetTypeNotInSource()
        {
            await TestInRegularAndScriptAsync(
@"class Digit
{
    public Digit(double d)
    {
        val = d;
    }

    public double val;
}

class Program
{
    static void Main(string[] args)
    {
        Digit dig = new Digit(7);
        double num = [|dig|];
    }
}",
@"using System;

class Digit
{
    public Digit(double d)
    {
        val = d;
    }

    public double val;

    public static implicit operator double(Digit v)
    {
        throw new NotImplementedException();
    }
}

class Program
{
    static void Main(string[] args)
    {
        Digit dig = new Digit(7);
        double num = dig;
    }
}");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")]
        public async Task TestGenerateExplicitConversionGenericClass()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Test(int[] a)
    {
        C<int> x1 = [|(C<int>)1|];
    }
}

class C<T>
{
}",
@"using System;

class Program
{
    void Test(int[] a)
    {
        C<int> x1 = (C<int>)1;
    }
}

class C<T>
{
    public static explicit operator C<T>(int v)
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")]
        public async Task TestGenerateExplicitConversionClass()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Test(int[] a)
    {
        C x1 = [|(C)1|];
    }
}

class C
{
}",
@"using System;

class Program
{
    void Test(int[] a)
    {
        C x1 = (C)1;
    }
}

class C
{
    public static explicit operator C(int v)
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")]
        public async Task TestGenerateExplicitConversionAwaitExpression()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Threading.Tasks;

class Program
{
    async void Test()
    {
        var a = Task.FromResult(1);
        Program x1 = [|(Program)await a|];
    }
}",
@"using System;
using System.Threading.Tasks;

class Program
{
    async void Test()
    {
        var a = Task.FromResult(1);
        Program x1 = (Program)await a;
    }

    public static explicit operator Program(int v)
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")]
        public async Task TestGenerateExplicitConversionTargetTypeNotInSource()
        {
            await TestInRegularAndScriptAsync(
@"class Digit
{
    public Digit(double d)
    {
        val = d;
    }

    public double val;
}

class Program
{
    static void Main(string[] args)
    {
        Digit dig = new Digit(7);
        double num = [|(double)dig|];
    }
}",
@"using System;

class Digit
{
    public Digit(double d)
    {
        val = d;
    }

    public double val;

    public static explicit operator double(Digit v)
    {
        throw new NotImplementedException();
    }
}

class Program
{
    static void Main(string[] args)
    {
        Digit dig = new Digit(7);
        double num = (double)dig;
    }
}");
        }
    }
}
