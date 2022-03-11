// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Analyzers.ForEachCast;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.ForEachCast;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddUsing;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ForEachCast
{
    using VerifyCS = CSharpCodeFixVerifier<
        CSharpForEachCastDiagnosticAnalyzer,
        CSharpForEachCastCodeFixProvider>;

    public class ForEachCastTests
    {
        private async Task TestWorkerAsync(
            string testCode, string fixedCode, string optionValue)
        {
            await new VerifyCS.Test
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                EditorConfig = "dotnet_style_prefer_foreach_explicit_cast_in_source=" + optionValue,
            }.RunAsync();
        }

        private Task TestAlwaysAsync(string markup, string alwaysMarkup)
            => TestWorkerAsync(markup, alwaysMarkup, "always");

        private Task TestNonLegacyAsync(string markup, string nonLegacyMarkup)
            => TestWorkerAsync(markup, nonLegacyMarkup, "non_legacy");

        [Fact]
        public async Task NonGenericIComparableCollection()
        {
            var test = @"
namespace ConsoleApplication1
{
    class Program
    {   
        void Main()
        {
            [|foreach|] (string item in new A())
            {
            }
        }
    }
    struct A
    {
        public Enumerator GetEnumerator() =>  new Enumerator();
        public struct Enumerator
        {
            public System.IComparable Current => 42;
            public bool MoveNext() => true;
        }
    }
}";

            await TestAlwaysAsync(test, test);
            await TestNonLegacyAsync(test, test);
        }

        [Fact]
        public async Task GenericObjectCollection()
        {
            var test = @"
using System.Collections.Generic;
namespace ConsoleApplication1
{
    class Program
    {   
        void Main()
        {
            var x = new List<object>();
            [|foreach|] (string item in x)
            {
            }
        }
    }
}";
            var fixedCode = @"
using System.Collections.Generic;
namespace ConsoleApplication1
{
    class Program
    {   
        void Main()
        {
            var x = new List<object>();
            foreach (string item in x.Cast<string>())
            {
            }
        }
    }
}";

            await TestAlwaysAsync(test, fixedCode);
            await TestNonLegacyAsync(test, fixedCode);
        }

        [Fact]
        public async Task NongenericObjectCollection()
        {
            var test = @"
using System.Collections;
namespace ConsoleApplication1
{
    class Program
    {   
        void Main()
        {
            var x = new ArrayList();
            foreach (string item in x)
            {
            }
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(test, test);
        }

        [Fact]
        public async Task SameType()
        {
            var test = @"
using System.Collections.Generic;
namespace ConsoleApplication1
{
    class Program
    {   
        void Main()
        {
            var x = new List<string>();
            foreach (string item in x)
            {
            }
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(test, test);
            This conversation was marked as resolved by maxkoshevoi
        }

        [Fact]
        public async Task CastBaseToChild()
        {
            var test = @"
using System.Collections.Generic;
namespace ConsoleApplication1
{
    class Program
    {   
        void Main()
        {
            var x = new List<A>();
            {|#0:foreach|} (B item in x)
This conversation was marked as resolved by maxkoshevoi
            {
            }
        }
    }
    class A { }
    class B : A { }
}";

            await VerifyCS.VerifyCodeFixAsync(test, GetCSharpResultAt(0).WithArguments("A", "B"), test);
        }

        [Fact]
        public async Task ImplicitConversion()
        {
            var test = @"
using System.Collections.Generic;
namespace ConsoleApplication1
{
    class Program
    {   
        void Main()
        {
            var x = new List<int>();
            foreach (long item in x)
            {
            }
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(test, test);
        }

        [Fact]
        public async Task UserDefinedImplicitConversion()
        {
            var test = @"
using System.Collections.Generic;
namespace ConsoleApplication1
{
    class Program
    {   
        void Main()
        {
            var x = new List<A>();
            foreach (B item in x)
            {
            }
        }
    }
    class A { }
    class B 
    { 
        public static implicit operator B(A a) => new B();
    }
}";

            await VerifyCS.VerifyCodeFixAsync(test, test);
        }

        [Fact]
        public async Task ExplicitConversion()
        {
            var test = @"
using System.Collections.Generic;
namespace ConsoleApplication1
{
    class Program
    {   
        void Main()
        {
            var x = new List<long>();
            {|#0:foreach|} (int item in x)
            {
            }
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(test, GetCSharpResultAt(0).WithArguments("Int64", "Int32"), test);
        }

        [Fact]
        public async Task UserDefinedExplicitConversion()
        {
            var test = @"
using System.Collections.Generic;
namespace ConsoleApplication1
{
    class Program
    {   
        void Main()
        {
            var x = new List<A>();
            {|#0:foreach|} (B item in x)
            {
            }
        }
    }
    class A { }
    class B 
    { 
        public static explicit operator B(A a) => new B();
    }
}";

            await VerifyCS.VerifyCodeFixAsync(test, GetCSharpResultAt(0).WithArguments("A", "B"), test);
        }

        [Fact]
        public async Task CastChildToBase()
        {
            var test = @"
using System.Collections.Generic;
namespace ConsoleApplication1
{
    class Program
    {   
        void Main()
        {
            var x = new List<B>();
            foreach (A item in x)
            {
            }
        }
    }
    class A { }
    class B : A { }
}";

            await VerifyCS.VerifyCodeFixAsync(test, test);
        }

        [Fact]
        public async Task InterfaceToClass()
        {
            var test = @"
using System;
using System.Collections.Generic;
namespace ConsoleApplication1
{
    class Program
    {   
        void Main()
        {
            var x = new List<IComparable>();
            {|#0:foreach|} (string s in x)
            {
            }
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(test, GetCSharpResultAt(0).WithArguments("IComparable", "String"), test);
        }

        [Fact]
        public async Task ClassToInterfase()
        {
            var test = @"
using System;
using System.Collections.Generic;
namespace ConsoleApplication1
{
    class Program
    {   
        void Main()
        {
            var x = new List<string>();
            foreach (IComparable s in x)
            {
            }
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(test, test);
        }

        [Fact]
        public async Task GenericTypes_Unrelated()
        {
            var test = @"
using System.Collections.Generic;
namespace ConsoleApplication1
{
    class Program
    {   
        void Main<A, B>()
        {
            var x = new List<A>();
            {|#0:foreach|} (B s in x)
            {
            }
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(test, DiagnosticResult.CompilerError("CS0030").WithLocation(0).WithArguments("A", "B"), test);
        }

        [Fact]
        public async Task GenericTypes_Valid_Relationship()
        {
            var test = @"
using System.Collections.Generic;
namespace ConsoleApplication1
{
    class Program
    {   
        void Main<A, B>() where A : B
        {
            var x = new List<A>();
            foreach (B s in x)
            {
            }
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(test, test);
        }

        [Fact]
        public async Task GenericTypes_Invalid_Relationship()
        {
            var test = @"
using System.Collections.Generic;
namespace ConsoleApplication1
{
    class Program
    {   
        void Main<A, B>() where B : A
        {
            var x = new List<A>();
            {|#0:foreach|} (B s in x)
            {
            }
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(test, GetCSharpResultAt(0).WithArguments("A", "B"), test);
        }

        [Fact]
        public async Task CollectionFromMethodResult_Invalid()
        {
            var test = @"
using System;
using System.Collections.Generic;
namespace ConsoleApplication1
{
    class Program
    {   
        void Main()
        {
            {|#0:foreach|} (string item in GenerateSequence())
            {
            }
            IEnumerable<IComparable> GenerateSequence()
            {
                throw new NotImplementedException();
            }
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(test, GetCSharpResultAt(0).WithArguments("IComparable", "String"), test);
        }

        [Fact]
        public async Task CollectionFromMethodResult_Valid()
        {
            var test = @"
using System;
using System.Collections.Generic;
namespace ConsoleApplication1
{
    class Program
    {   
        void Main()
        {
            foreach (IComparable item in GenerateSequence())
            {
            }
            IEnumerable<IComparable> GenerateSequence()
            {
                throw new NotImplementedException();
            }
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(test, test);
        }

        [Fact]
        public async Task DynamicSameType()
        {
            var test = @"
using System.Collections.Generic;
namespace ConsoleApplication1
{
    class Program
    {   
        void Main()
        {
            var x = new List<dynamic>();
            foreach (dynamic s in x)
            {
            }
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(test, test);
        }

        [Fact]
        public async Task DynamicToObject()
        {
            var test = @"
using System.Collections.Generic;
namespace ConsoleApplication1
{
    class Program
    {   
        void Main()
        {
            var x = new List<dynamic>();
            foreach (object s in x)
            {
            }
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(test, test);
        }

        [Fact]
        public async Task DynamicToString()
        {
            var test = @"
using System.Collections.Generic;
namespace ConsoleApplication1
{
    class Program
    {   
        void Main()
        {
            var x = new List<dynamic>();
            {|#0:foreach|} (string s in x)
            {
            }
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(test, GetCSharpResultAt(0).WithArguments("dynamic", "String"), test);
        }

        [Fact]
        public async Task DynamicToVar()
        {
            var test = @"
using System.Collections.Generic;
namespace ConsoleApplication1
{
    class Program
    {   
        void Main()
        {
            var x = new List<dynamic>();
            foreach (var s in x)
            {
            }
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(test, test);
        }

        [Fact]
        public async Task TupleToVarTuple()
        {
            var test = @"
using System;
using System.Collections.Generic;
namespace ConsoleApplication1
{
    class Program
    {   
        void Main()
        {
            var x = new List<(int, IComparable)>();
            foreach (var (i, j) in x)
            {
            }
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(test, test);
        }

        [Fact]
        public async Task TupleToSameTuple()
        {
            var test = @"
using System;
using System.Collections.Generic;
namespace ConsoleApplication1
{
    class Program
    {   
        void Main()
        {
            var x = new List<(int, IComparable)>();
            foreach ((int i,  IComparable j) in x)
            {
            }
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(test, test);
        }

        [Fact]
        public async Task TupleToChildTuple()
        {
            var test = @"
using System;
using System.Collections.Generic;
namespace ConsoleApplication1
{
    class Program
    {   
        void Main()
        {
            var x = new List<(int, IComparable)>();
            foreach ((int i,  {|#0:int j|}) in x)
            {
            }
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(test, DiagnosticResult.CompilerError("CS0266").WithLocation(0).WithArguments("System.IComparable", "int"), test);
        }
    }
}
