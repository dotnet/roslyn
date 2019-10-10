using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using System;
using System.Linq;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public static class LocalFunctionTestsUtil
    {
        public static IMethodSymbol FindLocalFunction(this CompilationVerifier verifier, string localFunctionName)
        {
            localFunctionName = (char)GeneratedNameKind.LocalFunction + "__" + localFunctionName;
            var methods = verifier.TestData.GetMethodsByName();
            IMethodSymbol result = null;
            foreach (var kvp in methods)
            {
                if (kvp.Key.Contains(localFunctionName))
                {
                    Assert.Null(result); // more than one name matched
                    result = kvp.Value.Method;
                }
            }
            Assert.NotNull(result); // no methods matched
            return result;
        }
    }

    [CompilerTrait(CompilerFeature.LocalFunctions)]
    public class CodeGenLocalFunctionTests : CSharpTestBase
    {
        [Fact]
        [WorkItem(37459, "https://github.com/dotnet/roslyn/pull/37459")]
        public void StaticLocalFunctionCaptureConstants()
        {
            var src = @"
using System;
class C
{
    const int X = 1;

    void M()
    {
        const int Y = 5;

        local();
        return;
        static void local()
        {
            Console.WriteLine(X);
            Console.WriteLine(Y);
        }
    }

    public static void Main()
    {
        (new C()).M();
    }
}
";
            var verifier = CompileAndVerify(src, expectedOutput: @"
1
5");
            verifier.VerifyIL("C.<M>g__local|1_0", @"
{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  call       ""void System.Console.WriteLine(int)""
  IL_0006:  ldc.i4.5
  IL_0007:  call       ""void System.Console.WriteLine(int)""
  IL_000c:  ret
}");
        }

        [Fact]
        [WorkItem(481125, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=481125")]
        public void Repro481125()
        {
            var comp = CreateCompilation(@"
using System;
using System.Linq;

public class C
{
    static void Main()
    {
        var c = new C();
        Console.WriteLine(c.M(0).Count());
        Console.WriteLine(c.M(1).Count());
    }

    public IQueryable<E> M(int salesOrderId)
    {
        using (var uow = new D())
        {
            return Local();

            IQueryable<E> Local() => uow.ES.Where(so => so.Id == salesOrderId);
        }
    }
}

internal class D : IDisposable
{
    public IQueryable<E> ES => new[] { new E() }.AsQueryable();

    public void Dispose() { }
}

public class E
{
    public int Id;
}", options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: @"1
0");
        }

        [Fact]
        [CompilerTrait(CompilerFeature.IOperation)]
        [WorkItem(24647, "https://github.com/dotnet/roslyn/issues/24647")]
        public void Repro24647()
        {
            var comp = CreateCompilation(@"
class Program
{
    static void Main(string[] args)
    {
        void local() { } => new object();
    }
}");
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var localFunction = tree.GetRoot().DescendantNodes().OfType<LocalFunctionStatementSyntax>().Single();
            var creation = localFunction.DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Single();

            var objectCreationOperation = model.GetOperation(creation);
            var localFunctionOperation = (ILocalFunctionOperation)model.GetOperation(localFunction);
            Assert.NotNull(objectCreationOperation);

            comp.VerifyOperationTree(creation, expectedOperationTree:
@"
IObjectCreationOperation (Constructor: System.Object..ctor()) (OperationKind.ObjectCreation, Type: System.Object, IsInvalid) (Syntax: 'new object()')
  Arguments(0)
  Initializer: 
    null
");

            Assert.Equal(OperationKind.ExpressionStatement, objectCreationOperation.Parent.Kind);
            Assert.Equal(OperationKind.Block, objectCreationOperation.Parent.Parent.Kind);
            Assert.Same(localFunctionOperation.IgnoredBody, objectCreationOperation.Parent.Parent);

            var info = model.GetTypeInfo(creation);
            Assert.Equal("System.Object", info.Type.ToTestDisplayString());
            Assert.Equal("System.Object", info.ConvertedType.ToTestDisplayString());
        }

        [Fact]
        [WorkItem(22027, "https://github.com/dotnet/roslyn/issues/22027")]
        public void Repro22027()
        {
            CompileAndVerify(@"
class Program
{
static void Main(string[] args)
{

 }
 public object TestLocalFn(object inp)
 {
     try
     {
         var sr = new object();
         return sr;
         void Local1()
         {
             var copy = inp;
             Local2();
         }
         void Local2()
         {

         }
     }
     catch { throw; }
 }
}");
        }

        [Fact]
        [WorkItem(21768, "https://github.com/dotnet/roslyn/issues/21768")]
        public void Repro21768()
        {
            var comp = CreateCompilation(@"
using System;
using System.Linq;
class C
{
    void Function(int someField) //necessary to have a parameter
    {
        using (IInterface db = null) //necessary to have this using statement
        {
            void LocalFunction() //necessary
            {
                var results =
                    db.Query<Class1>() //need to call this method. using a constant array does not reproduce the bug.
                    .Where(cje => cje.SomeField >= someField) //need expression tree here referencing parameter
                    ;
            }
        }
    }
    interface IInterface : IDisposable
    {
        IQueryable<T> Query<T>();
    }
    class Class1
    {
        public int SomeField { get; set; }
    }
}");
            CompileAndVerify(comp);
        }

        [Fact]
        [WorkItem(21811, "https://github.com/dotnet/roslyn/issues/21811")]
        public void Repro21811()
        {
            var comp = CreateCompilation(@"
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        var history = new List<long>();
        Enumerable.Range(0, 5)
            .Select(i =>
            {
                history.Insert(0, i);
                return Test(i);

                bool Test(int v)
                {
                    history.Remove(0);
                    return Square(v) > 5;
                }

                int Square(int w)
                {
                    return w * w;
                }
            });
    }
}");
        }

        [Fact]
        [WorkItem(21645, "https://github.com/dotnet/roslyn/issues/21645")]
        public void Repro21645()
        {
            CompileAndVerify(@"
public class Class1
{
    private void Test()
    {
        bool outside = true;

        void Inner() //This can also be a lambda (ie. Action action = () => { ... };)
        {
            void Bar()
            {
            }

            void Foo()
            {
                Bar();

                bool captured = outside;
            }
        }
    }
}");
        }

        [Fact]
        [WorkItem(21543, "https://github.com/dotnet/roslyn/issues/21543")]
        public void Repro21543()
        {
            CompileAndVerify(@"
using System;

class Program
{
    static void Method(Action action) { }

    static void Main()
    {
        int value = 0;
        Method(() =>
        {
            local();
            void local()
            {
                Console.WriteLine(value);
                Method(() =>
                {
                    local();
                });
            }
        });
    }
}");
        }

        [Fact]
        [WorkItem(472056, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=472056")]
        public void Repro472056()
        {
            var comp = CreateCompilationWithMscorlib46(@"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConsoleApp2
{
    class Program
    {
        static void Main(string[] args)
        {
            var task = WhyYouBreaky(new List<string>());

            Console.WriteLine(task.Result);
        }

        static async Task<string> WhyYouBreaky(List<string> words)
        {
            await Task.Delay(1);
            var word = """"; // moving me before the 'await' will make it work

            words.Add(""Oh No!""); // I will crash here :(

            return ""Great success!""; // Not so much.

            void IDontEvenGetCalled()
            {
                // commenting out either of these lines will make it work
                var a = word;
                var b = words[0];
            }
        }
    }
}", options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: "Great success!");
        }

        [Fact]
        public void AsyncStructClosure()
        {
            var comp = CreateCompilationWithMscorlib46(@"
using System;
using System.Threading.Tasks;

class C
{
    static void Main() => M().Wait();

    static async Task M()
    {
        int x = 2;
        int y = 3;
        int L() => x + y;
        Console.WriteLine(L());
        await Task.FromResult(false);
    }
}", options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "5");
            // No field captures
            verifier.VerifySynthesizedFields("C.<M>d__1",
                "int <>1__state",
                "System.Runtime.CompilerServices.AsyncTaskMethodBuilder <>t__builder",
                "System.Runtime.CompilerServices.TaskAwaiter<bool> <>u__1");

            comp = CreateCompilationWithMscorlib46(@"
using System;
using System.Threading.Tasks;

class C
{
    static void Main() => M().Wait();

    static async Task M()
    {
        int x = 2;
        int y = 3;
        int L() => x + y;
        Console.WriteLine(L());
        await Task.FromResult(false);
        x++;
        Console.WriteLine(x);
    }
}", options: TestOptions.ReleaseExe);
            verifier = CompileAndVerify(comp, expectedOutput: @"5
3");
            verifier.VerifySynthesizedFields("C.<M>d__1",
                "int <>1__state",
                "System.Runtime.CompilerServices.AsyncTaskMethodBuilder <>t__builder",
                // Display class capture
                "C.<>c__DisplayClass1_0 <>8__1",
                "System.Runtime.CompilerServices.TaskAwaiter<bool> <>u__1");

            verifier.VerifySynthesizedFields("C.<>c__DisplayClass1_0",
                "int x",
                "int y");

            comp = CreateCompilationWithMscorlib46(@"
using System;
using System.Threading.Tasks;

class C
{
    static void Main() => M().Wait();

    static async Task M()
    {
        int x = 2;
        int y = 3;
        int L() => x + y;
        Console.WriteLine(L());
        await Task.FromResult(false);
        x = 5;
        y = 7;
        Console.WriteLine(L());
    }
}", options: TestOptions.ReleaseExe);
            verifier = CompileAndVerify(comp, expectedOutput: @"5
12");
            // Nothing captured across await
            verifier.VerifySynthesizedFields("C.<M>d__1",
                "int <>1__state",
                "System.Runtime.CompilerServices.AsyncTaskMethodBuilder <>t__builder",
                "System.Runtime.CompilerServices.TaskAwaiter<bool> <>u__1");
        }

        [Fact]
        public void IteratorStructClosure()
        {
            var verifier = CompileAndVerify(@"
using System;
using System.Collections.Generic;

class C
{
    static void Main()
    {
        foreach (var m in M())
        {
            Console.WriteLine(m);
        }
    }

    static IEnumerable<int> M()
    {
        int x = 2;
        int y = 3;
        int L() => x + y;
        yield return L();
    }
}", expectedOutput: "5");
            // No field captures
            verifier.VerifySynthesizedFields("C.<M>d__1",
                "int <>1__state",
                "int <>2__current",
                "int <>l__initialThreadId");

            verifier = CompileAndVerify(@"
using System;
using System.Collections.Generic;

class C
{
    static void Main()
    {
        foreach (var m in M())
        {
            Console.WriteLine(m);
        }
    }

    static IEnumerable<int> M()
    {
        int x = 2;
        int y = 3;
        int L() => x + y;
        yield return L();
        x++;
        yield return x;
    }
}", expectedOutput: @"5
3");
            verifier.VerifySynthesizedFields("C.<M>d__1",
                "int <>1__state",
                "int <>2__current",
                "int <>l__initialThreadId",
                // Display class capture
                "C.<>c__DisplayClass1_0 <>8__1");

            verifier.VerifySynthesizedFields("C.<>c__DisplayClass1_0",
                "int x",
                "int y");

            verifier = CompileAndVerify(@"
using System;
using System.Collections.Generic;

class C
{
    static void Main()
    {
        foreach (var m in M())
        {
            Console.WriteLine(m);
        }
    }

    static IEnumerable<int> M()
    {
        int x = 2;
        int y = 3;
        int L() => x + y;
        yield return L();
        x = 5;
        y = 7;
        yield return L();
    }
}", expectedOutput: @"5
12");
            // No captures
            verifier.VerifySynthesizedFields("C.<M>d__1",
                "int <>1__state",
                "int <>2__current",
                "int <>l__initialThreadId");
        }

        [Fact]
        [WorkItem(21409, "https://github.com/dotnet/roslyn/issues/21409")]
        public void Repro21409()
        {
            CompileAndVerify(
@"
using System;
using System.Collections.Generic;

namespace Buggles
{
    class Program
    {
        private static IEnumerable<int> Problem(IEnumerable<int> chunks)
        {
            var startOfChunk = 0;
            var pendingChunks = new List<int>();

            int GenerateChunk()
            {
                if (pendingChunks == null)
                {
                    Console.WriteLine(""impossible in local function"");
                    return -1;
                }
                while (pendingChunks.Count > 0)
                {
                    pendingChunks.RemoveAt(0);
                }
                return startOfChunk;
            }

            foreach (var chunk in chunks)
            {
                if (chunk - startOfChunk <= 0)
                {
                    pendingChunks.Insert(0, chunk);
                }
                else
                {
                    yield return GenerateChunk();
                }
                startOfChunk = chunk;
                if (pendingChunks == null)
                {
                    Console.WriteLine(""impossible in outer function"");
                }
                else
                {
                    pendingChunks.Insert(0, chunk);
                }
            }
        }

        private static void Main()
        {
            var xs = Problem(new[] { 0, 1, 2, 3 });
            foreach (var x in xs)
            {
                Console.WriteLine(x);
            }
        }
    }
}
", expectedOutput: @"
0
1
2");
        }

        [Fact]
        [WorkItem(294554, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=294554")]
        public void ThisOnlyClosureBetweenStructCaptures()
        {
            CompileAndVerify(@"
using System;
class C
{
    int _x = 0;
    void M()
    {
        void L1()
        {
            int x = 0;
            _x++;
            void L2()
            {
                Action a2 = L2;
                int y = 0;
                L3();
                void L3()
                {
                    _x++;
                    y++;
                }
            }
            L2();

            void L5() => x++;
            L5();
        }
        L1();
    }
}");
        }

        [Fact]
        public void CaptureThisInDifferentScopes()
        {
            CompileAndVerify(@"
using System;
class C
{
    int _x;
    void M()
    {
        {
            int y = 0;
            Func<int> f1 = () => _x + y;
        }
        {
            int y = 0;
            Func<int> f2 = () => _x + y;
        }
    }
}");
        }

        [Fact]
        public void CaptureThisInDifferentScopes2()
        {
            CompileAndVerify(@"
using System;
class C
{
    int _x;
    void M()
    {
        {
            int y = 0;
            int L1() => _x + y;
        }
        {
            int y = 0;
            int L2() => _x + y;
        }
    }
}");
        }

        [Fact]
        public void CaptureFramePointerInDifferentScopes()
        {
            CompileAndVerify(@"
using System;
class C
{
    void M(int x)
    {
        Func<int> f1 = () => x;
        {
            int z = 0;
            Func<int> f2 = () => x + z;
        }
        {
            int z = 0;
            Func<int> f3 = () => x + z;
        }
    }
}");
        }

        [Fact]
        public void EnvironmentChainContainsStructEnvironment()
        {
            CompileAndVerify(@"
using System;
class C
{
    void M(int x)
    {
        {
            int y = 10;
            void L() => Console.WriteLine(y);

            {
                int z = 5;
                Action f2 = () => Console.WriteLine(z + x);
                f2();
            }
            L();
        }
    }
    public static void Main() => new C().M(3);
}", expectedOutput: @"8
10");
        }

        [Fact]
        public void Repro20577()
        {
            var comp = CreateCompilation(@"
using System.Linq;

public class Program {
    public static void Main(string[] args) {
        object v;

        void AAA() {
            object BBB(object v2) {
                var a = v;
                ((object[])v2).Select(i => BBB(i));
                return null;
            }
        }
    }
}");
            CompileAndVerify(comp);
        }

        [Fact]
        public void Repro19033()
        {
            CompileAndVerify(@"
using System;

class Program
{
    void Q(int n = 0)
    {
        {
            object mc;

            string B(object map)
            {
                Action<int> a = _ => B(new object());
                return n.ToString();
            }
        }
    }
}");
        }

        [Fact]
        public void Repro19033_2()
        {
            CompileAndVerify(@"
using System;
class C
{
    static void F(Action a)
    {
        object x = null;
        {
            object y = null;
            void G(object z)
            {
                F(() => G(x));
            }
        }
    }
}");
        }

        [Fact]
        [WorkItem(18814, "https://github.com/dotnet/roslyn/issues/18814")]
        [WorkItem(18918, "https://github.com/dotnet/roslyn/issues/18918")]
        public void IntermediateStructClosures1()
        {
            var verifier = CompileAndVerify(@"
using System;
class C
{
    int _x = 0;

    public static void Main() => new C().M();

    public void M()
    {
        int var1 = 0;
        void L1()
        {
            void L2()
            {
                void L3()
                {   
                    void L4()
                    {
                        int var2 = 0;
                        void L5()
                        {
                            int L6() => var2 + _x++;
                            L6();
                        }
                        L5();
                    }
                    L4();
                }
                L3();
            }
            L2();
            int L8() => var1;
        }
        Console.WriteLine(_x);
        L1();
        Console.WriteLine(_x);
    }
}", expectedOutput:
@"0
1");
            verifier.VerifyIL("C.M()", @"
{
  // Code size       47 (0x2f)
  .maxstack  2
  .locals init (C.<>c__DisplayClass2_0 V_0) //CS$<>8__locals0
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldarg.0
  IL_0003:  stfld      ""C C.<>c__DisplayClass2_0.<>4__this""
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldc.i4.0
  IL_000b:  stfld      ""int C.<>c__DisplayClass2_0.var1""
  IL_0010:  ldarg.0
  IL_0011:  ldfld      ""int C._x""
  IL_0016:  call       ""void System.Console.WriteLine(int)""
  IL_001b:  ldarg.0
  IL_001c:  ldloca.s   V_0
  IL_001e:  call       ""void C.<M>g__L1|2_0(ref C.<>c__DisplayClass2_0)""
  IL_0023:  ldarg.0
  IL_0024:  ldfld      ""int C._x""
  IL_0029:  call       ""void System.Console.WriteLine(int)""
  IL_002e:  ret
}");

            // L1
            verifier.VerifyIL("C.<M>g__L1|2_0(ref C.<>c__DisplayClass2_0)", @"
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""void C.<M>g__L2|2_1(ref C.<>c__DisplayClass2_0)""
  IL_0007:  ret
}");
            // L2
            verifier.VerifyIL("C.<M>g__L2|2_1(ref C.<>c__DisplayClass2_0)", @"
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""void C.<M>g__L3|2_3(ref C.<>c__DisplayClass2_0)""
  IL_0007:  ret
}");
            // Skip some... L5
            verifier.VerifyIL("C.<M>g__L5|2_5(ref C.<>c__DisplayClass2_0, ref C.<>c__DisplayClass2_1)", @"
{
  // Code size       10 (0xa)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  ldarg.2
  IL_0003:  call       ""int C.<M>g__L6|2_6(ref C.<>c__DisplayClass2_0, ref C.<>c__DisplayClass2_1)""
  IL_0008:  pop
  IL_0009:  ret
}");
            // L6
            verifier.VerifyIL("C.<M>g__L6|2_6(ref C.<>c__DisplayClass2_0, ref C.<>c__DisplayClass2_1)", @"
{
  // Code size       25 (0x19)
  .maxstack  4
  .locals init (int V_0)
  IL_0000:  ldarg.2
  IL_0001:  ldfld      ""int C.<>c__DisplayClass2_1.var2""
  IL_0006:  ldarg.0
  IL_0007:  ldarg.0
  IL_0008:  ldfld      ""int C._x""
  IL_000d:  stloc.0
  IL_000e:  ldloc.0
  IL_000f:  ldc.i4.1
  IL_0010:  add
  IL_0011:  stfld      ""int C._x""
  IL_0016:  ldloc.0
  IL_0017:  add
  IL_0018:  ret
}");
        }

        [Fact]
        [WorkItem(18814, "https://github.com/dotnet/roslyn/issues/18814")]
        [WorkItem(18918, "https://github.com/dotnet/roslyn/issues/18918")]
        public void IntermediateStructClosures2()
        {
            CompileAndVerify(@"
class C
{
    int _x;
    void M()
    {
        int y = 0;
        void L1()
        {
            void L2()
            {
                int z = 0;
                int L3() => z + _x;
            }
            y++;
        }
    }
}");
        }

        [Fact]
        [WorkItem(18814, "https://github.com/dotnet/roslyn/issues/18814")]
        public void Repro18814()
        {
            CompileAndVerify(@"
class Program
{
    private void ResolvingPackages()
    {
        string outerScope(int a) => """";

        void C1(int cabinetIdx)
        {
            void modifyState()
            {
                var no = outerScope(cabinetIdx);
            }

            modifyState();
        }
    }
}");
        }

        [Fact]
        [WorkItem(18918, "https://github.com/dotnet/roslyn/issues/18918")]
        public void Repro18918()
        {
            CompileAndVerify(@"
public class Test
{
    private int _field;

    public void OuterMethod(int outerParam)
    {
        void InnerMethod1()
        {
            void InnerInnerMethod(int innerInnerParam)
            {
                InnerInnerInnerMethod();
                
                bool InnerInnerInnerMethod()
                {
                    return innerInnerParam != _field;
                }
            }

            void InnerMethod2()
            {
                var temp = outerParam;
            }  
        }
    }
}");
        }

        [Fact]
        [WorkItem(17719, "https://github.com/dotnet/roslyn/issues/17719")]
        public void Repro17719()
        {
            var comp = CompileAndVerify(@"
using System;
class C
{
    public static void Main()
    {
        T GetField<T>(string name, T @default = default(T))
        {
          return @default;
        }
        Console.WriteLine(GetField<int>(string.Empty));
    }
}", expectedOutput: "0");
        }

        [Fact]
        [WorkItem(17890, "https://github.com/dotnet/roslyn/issues/17890")]
        public void Repro17890()
        {
            var comp = CreateCompilationWithMscorlib46(@"
using System;
using System.Collections.Generic;
using System.Linq;

public class Class
{
   public class Item
   {
      public int Id { get; set; }
   }

   public class ItemsContainer : IDisposable
   {
      public List<Item> Items { get; set; }

      public void Dispose()
      {
      }
   }

   public static void CompilerError()
   {
      using (var itemsContainer = new ItemsContainer())
      {
         Item item = null;

         itemsContainer.Items.Where(x => x.Id == item.Id);

         void Local1()
         {
            itemsContainer.Items = null;
         }

         void Local2()
         {
            Local1();
         }
      }
   }
}", references: new[] { LinqAssemblyRef });
            CompileAndVerify(comp);
        }

        [Fact]
        [WorkItem(16783, "https://github.com/dotnet/roslyn/issues/16783")]
        public void GenericDefaultParams()
        {
            CompileAndVerify(@"
using System;
class C
{
    public void M()
    {
        void Local<T>(T t = default(T))
        {
            Console.WriteLine(t);
        }
        Local<int>();
    }
}

class C2
{
    public static void Main()
    {
        new C().M();
    }
}", expectedOutput: "0");
        }

        [Fact]
        public void GenericCaptureDefaultParams()
        {
            CompileAndVerify(@"
using System;
class C<T>
{
    public void M()
    {
        void Local(T t = default(T))
        {
            Console.WriteLine(t);
        }
        Local();
    }
}

class C2
{
    public static void Main()
    {
        new C<int>().M();
    }
}", expectedOutput: "0");
        }

        [Fact]
        public void NameofRecursiveDefaultParameter()
        {
            var comp = CreateCompilation(@"
using System;
class C
{
    public static void Main()
    {
        void Local(string s = nameof(Local))
        {
            Console.WriteLine(s);
        }
        Local();
    }
}", options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            comp.DeclarationDiagnostics.Verify();
            CompileAndVerify(comp, expectedOutput: "Local");
        }

        [Fact]
        [WorkItem(16895, "https://github.com/dotnet/roslyn/issues/16895")]
        public void CaptureVarNestedLambdaSkipScope()
        {
            var src = @"
using System;
class C
{

    public static void Main()
    {
        var d = """";
        {
            int x = 0;
            void M()
            {
                if (d != null)
                {
                    Action a = () => x++;
                    a();
                }
            }
            M();
            Console.WriteLine(x);
        }
    }
}";
            CompileAndVerify(src, expectedOutput: "1");
        }

        [Fact]
        [WorkItem(16895, "https://github.com/dotnet/roslyn/issues/16895")]
        public void CaptureVarNestedLambdaSkipScope2()
        {
            var src = @"
using System;
class C
{
    class D : IDisposable { public void Dispose() {} }

    public static void Main()
    {
        using (var d = new D())
        {
            int x = 0;
            void M()
            {
                if (d != null)
                {
                    Action a = () => x++;
                    a();
                }
            }
            M();
            Console.WriteLine(x);
        }
    }
}";
            CompileAndVerify(src, expectedOutput: "1");
        }

        [Fact]
        [WorkItem(16895, "https://github.com/dotnet/roslyn/issues/16895")]
        public void CaptureVarNestedLambdaSkipScope3()
        {
            var src = @"
using System;
class C
{

    public static void Main()
    {
        var d = """";
        {
            int x = 0;
            void M()
            {
                if (d != null)
                {
                    void Local() => x++;
                    Action a = Local;
                    a();
                }
            }
            M();
            Console.WriteLine(x);
        }
    }
}";
            CompileAndVerify(src, expectedOutput: "1");
        }

        [Fact]
        [WorkItem(16895, "https://github.com/dotnet/roslyn/issues/16895")]
        public void CaptureVarNestedLambdaSkipScope4()
        {
            var src = @"
using System;
class C
{

    public static void Main()
    {
        var d = """";
        {
            int y = 0;
            {
                int x = 0;
                void M()
                {
                    if (d != null)
                    {
                        Action a = () => x++;;
                        a();
                    }
                }
                M();
                Console.WriteLine(x);
            }
            y++;
        }
    }
}";
            CompileAndVerify(src, expectedOutput: "1");
        }

        [Fact]
        [WorkItem(16895, "https://github.com/dotnet/roslyn/issues/16895")]
        public void CaptureVarNestedLambdaSkipScope5()
        {
            var src = @"
using System;
class C
{

    public static void Main()
    {
        int x = 0;
        {
            int y = 0;
            void L()
            {
                int z = 0;
                void L2()
                {
                    if (x == 0 && z == 0)
                    {
                        Action a = () => y++;
                        a();
                    }
                }
                L2();
            }
            L();
            Console.WriteLine(y);
        }
    }
}";
            CompileAndVerify(src, expectedOutput: "1");
        }

        [Fact]
        [WorkItem(16895, "https://github.com/dotnet/roslyn/issues/16895")]
        public void CaptureVarNestedLambdaSkipScope6()
        {
            var src = @"
using System;
class C
{

    public static void Main()
    {
        int x = 0;
        {
            int y = 0;
            void L()
            {
                int z = 0;
                void L2()
                {
                    if (x == 0 && y == 0)
                    {
                        Action a = () => z++;
                        a();
                    }
                    y++;
                }
                L2();
                Console.WriteLine(z);
            }
            L();
            Console.WriteLine(y);
        }
        ((Action)(() => x++))();
        Console.WriteLine(x);
    }
}";
            CompileAndVerify(src, expectedOutput: @"1
1
1");
        }

        [ConditionalFact(typeof(DesktopOnly))]
        [WorkItem(16895, "https://github.com/dotnet/roslyn/issues/16895")]
        public void CaptureVarNestedLambdaSkipScope7()
        {
            var src = @"
using System;
using System.Threading.Tasks;
class C
{

    public static void Main()
    {
        int x = 0;
        {
            int y = 0;
            void L()
            {
                if (x == 0)
                {
                    async Task L2()
                    {
                        await Task.Delay(1);
                        y++;
                    }
                    L2().Wait();
                }
            }
            L();
            Console.WriteLine(y);
        }
        Console.WriteLine(x);
    }
}";
            CompileAndVerify(src,
                targetFramework: TargetFramework.Mscorlib46,
                expectedOutput: @"1
0");
        }

        [Fact]
        [WorkItem(16895, "https://github.com/dotnet/roslyn/issues/16895")]
        public void CaptureVarNestedLambdaSkipScope8()
        {
            var src = @"
using System;
using System.Collections.Generic;
class C
{

    public static void Main()
    {
        int x = 0;
        {
            int y = 0;
            void L()
            {
                if (x == 0)
                {
                    IEnumerable<int> L2()
                    {
                        yield return 0;
                        y++;
                    }
                    foreach (var i in L2()) { }
                }
            }
            L();
            Console.WriteLine(y);
        }
        Console.WriteLine(x);
    }
}";
            CompileAndVerifyWithMscorlib46(src,
                expectedOutput: @"1
0");
        }

        [Fact]
        [WorkItem(16895, "https://github.com/dotnet/roslyn/issues/16895")]
        public void LocalFunctionCaptureSkipScope()
        {
            var src = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        {
            int uncaptured = 0;
            uncaptured++;

            {
                int x = 0;
                bool Local(int y) => x == 0 && args == null && y == 0;
                Local(0);
            }
        }
    }
}";
            CompileAndVerify(src);
        }


        [Fact]
        [WorkItem(16399, "https://github.com/dotnet/roslyn/issues/16399")]
        public void RecursiveGenericLocalFunctionIterator()
        {
            var src = @"
using System;
using System.Collections.Generic;
using System.Linq;
public static class EnumerableExtensions
{
    static void Main(string[] args)
    {
        GetLeaves<object>(new List<object>(), list => null);

        var results = GetLeaves<object>(
            new object[] {
                new[] { ""a"", ""b""},
                new[] { ""c"" },
                new[] { new[] { ""d"" } }
            }, node => node is string ? null : (IEnumerable<object>)node);

        foreach (var i in results)
        {
            Console.WriteLine(i);
        }
    }


    public static IEnumerable<T> GetLeaves<T>(T root, Func<T, IEnumerable<T>> getChildren)
    {
        return GetLeaves(root);

        IEnumerable<T> GetLeaves(T node)
        {
            var children = getChildren(node);
            if (children == null)
            {
                return new[] { node };
            }
            else
            {
                return children.SelectMany(GetLeaves);
            }
        }
    }
}";
            VerifyOutput(src, @"a
b
c
d");
        }

        [Fact]
        [WorkItem(243633, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems/edit/243633")]
        public void CaptureGenericFieldAndParameter()
        {
            var src = @"
using System;
using System.Collections.Generic;

class Test<T>
{
    T Value;

    public bool Goo(IEqualityComparer<T> comparer)
    {
        bool local(T tmp)
        {
            return comparer.Equals(tmp, this.Value);
        }
        return local(this.Value);
    }
}
";
            var comp = CompileAndVerify(src);
        }

        [Fact]
        public void CaptureGenericField()
        {
            var src = @"
using System;
class C<T>
{
    T Value = default(T);
    public void M()
    {
        void L()
        {
            Console.WriteLine(Value);
        }
        var f = (Action)(() => L());
        f();
    }
}
class C2
{
    public static void Main(string[] args) => new C<int>().M();
}
";
            VerifyOutput(src, "0");
        }

        [Fact]
        public void CaptureGenericParam()
        {
            var src = @"
using System;
class C<T>
{
    T Value = default(T);
    public void M<U>(U val2)
    {
        void L()
        {
            Console.WriteLine(Value);
            Console.WriteLine(val2);
        }
        var f = (Action)(() => L());
        f();
    }
}
class C2
{
    public static void Main(string[] args) => new C<int>().M(10);
}
";
            VerifyOutput(src, @"0
10");
        }

        [Fact]
        public void CaptureGenericParamInGenericLocalFunc()
        {
            var src = @"
using System;
class C<T>
{
    T Value = default(T);
    public void M<U>(U v1)
    {
        void L<V>(V v2) where V : T
        {
            Console.WriteLine(Value);
            Console.WriteLine(v1);
            Console.WriteLine(v2);
        }
        var f = (Action)(() => L<T>(Value));
        f();
    }
}
class C2
{
    public static void Main(string[] args) => new C<int>().M(10);
}
";
            VerifyOutput(src, @"0
10
0");
        }

        [Fact]
        public void DeepNestedLocalFuncsWithDifferentCaptures()
        {
            var src = @"
using System;
class C
{
    int P = 100000;
    void M()
    {
        C Local1() => this;
        int capture1 = 1;
        Func<int> f1 = () => capture1 + Local1().P;
        Console.WriteLine(f1());
        {
            C Local2() => Local1();
            int capture2 = 10;
            Func<int> f2 = () => capture2 + Local2().P;
            Console.WriteLine(f2());
            {
                C Local3() => Local2();

                int capture3 = 100;
                Func<int> f3 = () => capture1 + capture2 + capture3 + Local3().P;
                Console.WriteLine(f3());

                Console.WriteLine(Local3().P);
            }
        }
    }
    public static void Main() => new C().M();
}";
            VerifyOutput(src, @"100001
100010
100111
100000");
        }

        [Fact]
        public void LotsOfMutuallyRecursiveLocalFunctions()
        {
            var src = @"
class C
{
    int P = 0;
    public void M()
    {
        int Local1() => this.P;
        int Local2() => Local12() + Local11() + Local10() + Local9() + Local8() + Local7() + Local6() + Local5() + Local4() + Local3() + Local2() + Local1();
        int Local3() => Local12() + Local11() + Local10() + Local9() + Local8() + Local7() + Local6() + Local5() + Local4() + Local3() + Local2() + Local1();
        int Local4() => Local12() + Local11() + Local10() + Local9() + Local8() + Local7() + Local6() + Local5() + Local4() + Local3() + Local2() + Local1();
        int Local5() => Local12() + Local11() + Local10() + Local9() + Local8() + Local7() + Local6() + Local5() + Local4() + Local3() + Local2() + Local1();
        int Local6() => Local12() + Local11() + Local10() + Local9() + Local8() + Local7() + Local6() + Local5() + Local4() + Local3() + Local2() + Local1();
        int Local7() => Local12() + Local11() + Local10() + Local9() + Local8() + Local7() + Local6() + Local5() + Local4() + Local3() + Local2() + Local1();
        int Local8() => Local12() + Local11() + Local10() + Local9() + Local8() + Local7() + Local6() + Local5() + Local4() + Local3() + Local2() + Local1();
        int Local9() => Local12() + Local11() + Local10() + Local9() + Local8() + Local7() + Local6() + Local5() + Local4() + Local3() + Local2() + Local1();
        int Local10() => Local12() + Local11() + Local10() + Local9() + Local8() + Local7() + Local6() + Local5() + Local4() + Local3() + Local2() + Local1();
        int Local11() => Local12() + Local11() + Local10() + Local9() + Local8() + Local7() + Local6() + Local5() + Local4() + Local3() + Local2() + Local1();
        int Local12() => Local12() + Local11() + Local10() + Local9() + Local8() + Local7() + Local6() + Local5() + Local4() + Local3() + Local2() + Local1();

        Local1();
        Local2();
        Local3();
        Local4();
        Local5();
        Local6();
        Local7();
        Local8();
        Local9();
        Local10();
        Local11();
        Local12();
    }
}
";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void LocalFuncAndLambdaWithDifferentThis()
        {
            var src = @"
using System;
class C
{
    private int P = 1;
    public void M()
    {
        int Local(int x) => x + this.P;

        int y = 10;
        var a = new Func<int>(() => Local(y));
        Console.WriteLine(a());
    }

    public static void Main(string[] args)
    {
        var c = new C();
        c.M();
    }
}";
            VerifyOutput(src, "11");
        }

        [Fact]
        public void LocalFuncAndLambdaWithDifferentThis2()
        {
            var src = @"
using System;
class C
{
    private int P = 1;
    public void M()
    {
        int Local() => 10 + this.P;
        int Local2(int x) => x + Local();

        int y = 100;
        var a = new Func<int>(() => Local2(y));
        Console.WriteLine(a());
    }

    public static void Main(string[] args)
    {
        var c = new C();
        c.M();
    }
}";
            VerifyOutput(src, "111");
        }

        [Fact]
        public void LocalFuncAndLambdaWithDifferentThis3()
        {
            var src = @"
using System;
class C
{
    private int P = 1;
    public void M()
    {
        int Local() 
        {
            if (this.P < 5)
            {
                return Local2(this.P++);
            }
            else
            {
                return 1;
            }
        }
        int Local2(int x) => x + Local();

        int y = 100;
        var a = new Func<int>(() => Local2(y));
        Console.WriteLine(a());
    }

    public static void Main(string[] args)
    {
        var c = new C();
        c.M();
    }
}";
            VerifyOutput(src, "111");

        }

        [Fact]
        public void LocalFuncAndLambdaWithDifferentThis4()
        {
            var src = @"
using System;
class C
{
    private int P = 1;
    public void M()
    {
        int Local(int x) => x + this.P;

        int y = 10;
        var a = new Func<int>(() =>
        {
            var b = (Func<int, int>)Local;
            return b(y);
        });
        Console.WriteLine(a());
    }

    public static void Main(string[] args)
    {
        var c = new C();
        c.M();
    }
}";
            VerifyOutput(src, "11");
        }

        [Fact]
        public void LocalFuncAndLambdaWithDifferentThis5()
        {
            var src = @"
using System;
class C
{
    private int P = 1;
    public void M()
    {
        int Local(int x) => x + this.P;

        int y = 10;
        var a = new Func<int>(() =>
        {
            var b = new Func<int, int>(Local);
            return b(y);
        });
        Console.WriteLine(a());
    }

    public static void Main(string[] args)
    {
        var c = new C();
        c.M();
    }
}";
            VerifyOutput(src, "11");
        }

        [Fact]
        public void TwoFrames()
        {
            var src = @"
using System;
class C
{
    private int P = 0;
    public void M()
    {
        int x = 0;

        var a = new Func<int>(() =>
        {
            int Local() => x + this.P;
            int z = 0;
            int Local3() => z + Local();
            return Local3();
        });
        Console.WriteLine(a());
    }

    public static void Main(string[] args)
    {
        var c = new C();
        c.M();
    }
}";
            VerifyOutput(src, "0");
        }

        [Fact]
        public void SameFrame()
        {
            var src = @"
using System;
class C
{
    private int P = 1;
    public void M()
    {
        int x = 10;
        int Local() => x + this.P;

        int y = 100;
        int Local2() => y + Local();
        Console.WriteLine(Local2());
    }

    public static void Main(string[] args)
    {
        var c = new C();
        c.M();
    }
}";
            VerifyOutput(src, "111");
        }

        [Fact]
        public void MutuallyRecursiveThisCapture()
        {
            var src = @"
using System;
class C
{
    private int P = 1;
    public void M()
    {
        int Local()
        {
            if (this.P < 5)
            {
                return Local2(this.P++);
            }
            else
            {
                return 1;
            }
        }
        int Local2(int x) => x + Local();
        Console.WriteLine(Local());
    }
    public static void Main() => new C().M();
}";
            VerifyOutput(src, "11");
        }

        [Fact]
        [CompilerTrait(CompilerFeature.Dynamic)]
        public void DynamicParameterLocalFunction()
        {
            var src = @"
using System;

class C
{
    static void Main(string[] args) => M(0);

    static void M(int x)
    {
        dynamic y = x + 1;
        Action a;
        Action local(dynamic z) 
        {
            Console.Write(z);
            Console.Write(y);
            return () => Console.Write(y + z + 1);
        }
        a = local(x);
        a();
    }
}";
            VerifyOutput(src, "012");
        }

        [Fact]
        public void EndToEnd()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        void Local()
        {
            Console.WriteLine(""Hello, world!"");
        }
        Local();
    }
}
";
            VerifyOutput(source, "Hello, world!");
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ExpressionBody)]
        public void ExpressionBody()
        {
            var source = @"
int Local() => 2;
Console.Write(Local());
Console.Write(' ');
void VoidLocal() => Console.Write(2);
VoidLocal();
";
            VerifyOutputInMain(source, "2 2", "System");
        }

        [Fact]
        public void EmptyStatementAfter()
        {
            var source = @"
void Local()
{
    Console.Write(2);
};
Local();
";
            VerifyOutputInMain(source, "2", "System");
        }

        [Fact]
        [CompilerTrait(CompilerFeature.Params)]
        public void Params()
        {
            var source = @"
void Params(params int[] x)
{
    Console.WriteLine(string.Join("","", x));
}
Params(2);
";
            VerifyOutputInMain(source, "2", "System");
        }

        [Fact]
        public void RefAndOut()
        {
            var source = @"
void RefOut(ref int x, out int y)
{
    y = ++x;
}
int a = 1;
int b;
RefOut(ref a, out b);
Console.Write(a);
Console.Write(' ');
Console.Write(b);
";
            VerifyOutputInMain(source, "2 2", "System");
        }

        [Fact]
        public void NamedAndOptional()
        {
            var source = @"
void NamedOptional(int x = 2)
{
    Console.Write(x);
}
NamedOptional(x: 3);
Console.Write(' ');
NamedOptional();
";
            VerifyOutputInMain(source, "3 2", "System");
        }


        [Fact]
        [CompilerTrait(CompilerFeature.Dynamic)]
        public void DynamicArgShadowing()
        {
            var src = @"
using System;
class C
{
    static void Shadow(int x) => Console.Write(x + 1);

    static void Main()
    {
        void Shadow(int x) => Console.Write(x);

        dynamic val = 2;
        Shadow(val);
    }
}";
            VerifyOutput(src, "2");
        }

        [Fact]
        [CompilerTrait(CompilerFeature.Dynamic)]
        public void DynamicParameter()
        {
            var source = @"
using System;
class Program
{
    static void Main()
    {
        void Local(dynamic x)
        {
            Console.Write(x);
        }
        Local(2);
    }
}
";
            VerifyOutput(source, "2");
        }

        [Fact]
        [CompilerTrait(CompilerFeature.Dynamic)]
        public void DynamicReturn()
        {
            var source = @"
dynamic RetDyn()
{
    return 2;
}
Console.Write(RetDyn());
";
            VerifyOutputInMain(source, "2", "System");
        }

        [Fact]
        [CompilerTrait(CompilerFeature.Dynamic)]
        public void DynamicDelegate()
        {
            var source = @"
using System;
class Program
{
    static void Main()
    {
        dynamic Local(dynamic x)
        {
            return x;
        }
        dynamic L2(int x)
        {
            void L2_1(int y)
            {
                Console.Write(x);
                Console.Write(y);
            }
            dynamic z = x + 1;
            void L2_2() => L2_1(z);
            return (Action)L2_2;
        } 
        dynamic local = (Func<dynamic, dynamic>)Local;
        Console.Write(local(2));
        L2(3)();
    }
}
";
            VerifyOutput(source, "234");
        }

        [Fact]
        public void Nameof()
        {
            var source = @"
void Local()
{
}
Console.Write(nameof(Local));
";
            VerifyOutputInMain(source, "Local", "System");
        }

        [Fact]
        public void ExpressionTreeParameter()
        {
            var source = @"
Expression<Func<int, int>> Local(Expression<Func<int, int>> f)
{
    return f;
}
Console.Write(Local(x => x));
";
            VerifyOutputInMain(source, "x => x", "System", "System.Linq.Expressions");
        }

        [Fact]
        public void LinqInLocalFunction()
        {
            var source = @"
IEnumerable<int> Query(IEnumerable<int> values)
{
    return from x in values where x < 5 select x * x;
}
Console.Write(string.Join("","", Query(Enumerable.Range(0, 10))));
";
            VerifyOutputInMain(source, "0,1,4,9,16", "System", "System.Linq", "System.Collections.Generic");
        }

        [Fact]
        public void ConstructorWithoutArg()
        {
            var source = @"
using System;

class Base
{
    public int x;
    public Base(int x)
    {
        this.x = x;
    }
}

class Program : Base
{
    Program() : base(2)
    {
        void Local()
        {
            Console.Write(x);
        }
        Local();
    }
    public static void Main()
    {
        new Program();
    }
}
";
            VerifyOutput(source, "2");
        }

        [Fact]
        public void ConstructorWithArg()
        {
            var source = @"
using System;

class Base
{
    public int x;
    public Base(int x)
    {
        this.x = x;
    }
}

class Program : Base
{
    Program(int x) : base(x + 2)
    {
        void Local()
        {
            Console.Write(x);
            Console.Write(' ');
            Console.Write(base.x);
        }
        Local();
    }
    public static void Main()
    {
        new Program(2);
    }
}
";
            VerifyOutput(source, "2 4");
        }

        [Fact]
        public void IfDef()
        {
            var source = @"
using System;

class Program
{
    public static void Main()
    {
        #if LocalFunc
        void Local()
        {
            Console.Write(2);
            Console.Write(' ');
        #endif
            Console.Write(4);
        #if LocalFunc
        }
        Local();
        #endif
    }
}
";
            VerifyOutput(source, "4");
            source = "#define LocalFunc" + source;
            VerifyOutput(source, "2 4");
        }

        [Fact]
        public void PragmaWarningDisableEntersLocfunc()
        {
            var source = @"
#pragma warning disable CS0168
void Local()
{
    int x; // unused
    Console.Write(2);
}
#pragma warning restore CS0168
Local();
";
            // No diagnostics is asserted in VerifyOutput, so if the warning happens, then we'll catch it
            VerifyOutputInMain(source, "2", "System");
        }

        [Fact]
        public void ObsoleteAttributeRecursion()
        {
            var source = @"
using System;

class Program
{
    [Obsolete]
    public void Obs()
    {
        void Local()
        {
            Obs(); // shouldn't emit warning
        }
        Local();
    }
    public static void Main()
    {
        Console.Write(2);
    }
}
";
            VerifyOutput(source, "2");
        }

        [Fact]
        public void MainLocfuncIsntEntry()
        {
            var source = @"
void Main()
{
    Console.Write(4);
}
Console.Write(2);
Console.Write(' ');
Main();
";
            VerifyOutputInMain(source, "2 4", "System");
        }

        [Fact]
        public void Shadows()
        {
            var source = @"
using System;

class Program
{
    static void Local()
    {
        Console.WriteLine(""bad"");
    }

    static void Main(string[] args)
    {
        void Local()
        {
            Console.Write(2);
        }
        Local();
    }
}
";
            VerifyOutput(source, "2");
        }

        [Fact]
        public void ExtensionMethodClosure()
        {
            var source = @"
using System;

static class Program
{
    public static void Ext(this int x)
    {
        void Local()
        {
            Console.Write(x);
        }
        Local();
    }
    public static void Main()
    {
        2.Ext();
    }
}
";
            // warning level 0 because extension method generates CS1685 (predefined type multiple definition) for ExtensionAttribute in System.Core and mscorlib
            VerifyOutput(source, "2", TestOptions.ReleaseExe.WithWarningLevel(0));
        }

        [Fact]
        public void Scoping()
        {
            var source = @"
void Local()
{
    Console.Write(2);
}
if (true)
{
    Local();
}
";
            VerifyOutputInMain(source, "2", "System");
        }


        [Fact]
        public void Property()
        {
            var source = @"
using System;

class Program
{
    static int Goo
    {
        get
        {
            int Local()
            {
                return 2;
            }
            return Local();
        }
    }
    static void Main(string[] args)
    {
        Console.Write(Goo);
    }
}";
            VerifyOutput(source, "2");
        }

        [Fact]
        public void PropertyIterator()
        {
            var source = @"
using System;
using System.Collections.Generic;

class Program
{
    static int Goo
    {
        get
        {
            int a = 2;
            IEnumerable<int> Local()
            {
                yield return a;
            }
            foreach (var x in Local())
            {
                return x;
            }
            return 0;
        }
    }
    static void Main(string[] args)
    {
        Console.Write(Goo);
    }
}";
            VerifyOutput(source, "2");
        }

        [Fact]
        public void DelegateFunc()
        {
            var source = @"
int Local(int x) => x;
Func<int, int> local = Local;
Console.Write(local(2));
";
            VerifyOutputInMain(source, "2", "System");
        }

        [Fact]
        public void DelegateFuncGenericImplicit()
        {
            var source = @"
T Local<T>(T x) => x;
Func<int, int> local = Local;
Console.Write(local(2));
";
            VerifyOutputInMain(source, "2", "System");
        }

        [Fact]
        public void DelegateFuncGenericExplicit()
        {
            var source = @"
T Local<T>(T x) => x;
Func<int, int> local = Local<int>;
Console.Write(local(2));
";
            VerifyOutputInMain(source, "2", "System");
        }

        [Fact]
        public void DelegateAction()
        {
            var source = @"
void Local()
{
    Console.Write(2);
}
var local = new Action(Local);
local();
Console.Write(' ');
local = (Action)Local;
local();
";
            VerifyOutputInMain(source, "2 2", "System");
        }

        [Fact]
        public void InterpolatedString()
        {
            var source = @"
int x = 1;
int Bar() => ++x;
var str = $@""{((Func<int>)(() => { int Goo() => Bar(); return Goo(); }))()}"";
Console.Write(str + ' ' + x);
";
            VerifyOutputInMain(source, "2 2", "System");
        }

        // StaticNoClosure*() are generic because the reference to the locfunc is constructed, and actual local function is not
        // (i.e. testing to make sure we use MethodSymbol.OriginalDefinition in LambdaRewriter.Analysis)
        [Fact]
        public void StaticNoClosure()
        {
            var source = @"
T Goo<T>(T x)
{
    return x;
}
Console.Write(Goo(2));
";
            var verify = VerifyOutputInMain(source, "2", "System");
            var goo = verify.FindLocalFunction("Goo");
            Assert.True(goo.IsStatic);
            Assert.Equal(verify.Compilation.GetTypeByMetadataName("Program"), goo.ContainingType);
        }

        [Fact]
        public void StaticNoClosureDelegate()
        {
            var source = @"
T Goo<T>(T x)
{
    return x;
}
Func<int, int> goo = Goo;
Console.Write(goo(2));
";
            var verify = VerifyOutputInMain(source, "2", "System");
            var goo = verify.FindLocalFunction("Goo");
            var program = verify.Compilation.GetTypeByMetadataName("Program");
            Assert.False(goo.IsStatic);
            Assert.Equal("<>c", goo.ContainingType.Name);
            Assert.Equal(program, goo.ContainingType.ContainingType);
        }

        [Fact]
        public void ClosureBasic()
        {
            var source = @"
using System;

class Program
{
    static void Print(int a)
    {
        Console.Write(' ');
        Console.Write(a);
    }
    static void A(int y)
    {
        int x = 1;
        void Local()
        {
            Print(x); Print(y);
        }
        Local();
        Print(x); Print(y);
        x = 3;
        y = 4;
        Local();
        Print(x); Print(y);
        void Local2()
        {
            Print(x); Print(y);
            x += 2;
            y += 2;
            Print(x); Print(y);
        }
        Local2();
        Print(x); Print(y);
    }
    static void Main(string[] args)
    {
        A(2);
    }
}
";
            VerifyOutput(source, "1 2 1 2 3 4 3 4 3 4 5 6 5 6");
        }

        [Fact]
        public void ClosureThisOnly()
        {
            var source = @"
using System;

class Program
{
    int _a;
    static void Print(int a)
    {
        Console.Write(' ');
        Console.Write(a);
    }
    void B()
    {
        _a = 2;
        void Local()
        {
            Print(_a);
            _a++;
            Print(_a);
        }
        Print(_a);
        Local();
        Print(_a);
    }
    static void Main(string[] args)
    {
        new Program().B();
    }
}";
            VerifyOutput(source, "2 2 3 3");
        }

        [Fact]
        public void ClosureGeneralThisOnly()
        {
            var source = @"
var x = 0;
void Outer()
{
    if (++x == 2)
    {
        Console.Write(x);
        return;
    }
    void Inner()
    {
        Outer();
    }
    Inner();
}
Outer();
";
            var verify = VerifyOutputInMain(source, "2", "System");
            var outer = verify.FindLocalFunction("Outer");
            var inner = verify.FindLocalFunction("Inner");
            Assert.Equal(outer.ContainingType, inner.ContainingType);
        }

        [Fact]
        public void ClosureStaticInInstance()
        {
            var source = @"
using System;

class Program
{
    static int _sa;
    static void Print(int a)
    {
        Console.Write(' ');
        Console.Write(a);
    }
    void C()
    {
        _sa = 2;
        void Local()
        {
            Print(_sa);
            _sa++;
            Print(_sa);
        }
        Print(_sa);
        Local();
        Print(_sa);
    }
    static void Main(string[] args)
    {
        new Program().C();
    }
}";
            VerifyOutput(source, "2 2 3 3");
        }

        [Fact]
        public void ClosureGeneric()
        {
            var source = @"
using System;

class Program
{
    static void Print(object a)
    {
        Console.Write(' ');
        Console.Write(a);
    }
    class Gen<T1>
    {
        T1 t1;

        public Gen(T1 t1)
        {
            this.t1 = t1;
        }

        public void D<T2>(T2 t2)
        {
            T2 Local(T1 x)
            {
                Print(x);
                Print(t1);
                t1 = (T1)(object)((int)(object)x + 2);
                t2 = (T2)(object)x;
                return (T2)(object)((int)(object)t2 + 4);
            }
            Print(t1);
            Print(t2);
            Print(Local(t1));
            Print(t1);
            Print(t2);
        }
    }
    static void Main(string[] args)
    {
        new Gen<int>(2).D<int>(3);
    }
}";
            VerifyOutput(source, "2 3 2 2 6 4 2");
        }

        [Fact]
        public void ClosureLambdasAndLocfuncs()
        {
            var source = @"
using System;

class Program
{
    static void Print(int a)
    {
        Console.Write(' ');
        Console.Write(a);
    }
    static void E()
    {
        int a = 2;
        void M1()
        {
            int b = a;
            Action M2 = () =>
            {
                int c = b;
                void M3()
                {
                    int d = c;
                    Print(d + b);
                }
                M3();
            };
            M2();
        }
        M1();
    }
    static void Main(string[] args)
    {
        E();
    }
}";
            VerifyOutput(source, "4");
        }

        [Fact]
        public void ClosureTripleNested()
        {
            var source = @"
using System;

class Program
{
    static void Print(int a)
    {
        Console.Write(' ');
        Console.Write(a);
    }

    static void A()
    {
        int a = 0;
        void M1()
        {
            int b = a;
            void M2()
            {
                int c = b;
                void M3()
                {
                    Print(c);
                    c = 2;
                }
                Print(b);
                M3();
                Print(c);
                b = 2;
            }
            Print(a);
            M2();
            Print(b);
            a = 2;
        }
        M1();
        Print(a);
    }

    static void B()
    {
        int a = 0;
        void M1()
        {
            int b = a;
            void M2()
            {
                void M3()
                {
                    Print(b);
                    b = 2;
                }
                M3();
                Print(b);
            }
            Print(a);
            M2();
            Print(b);
            a = 2;
        }
        M1();
        Print(a);
    }

    static void C()
    {
        int a = 0;
        void M1()
        {
            void M2()
            {
                int c = a;
                void M3()
                {
                    Print(c);
                    c = 2;
                }
                Print(a);
                M3();
                Print(c);
                a = 2;
            }
            M2();
            Print(a);
        }
        M1();
        Print(a);
    }

    static void D()
    {
        void M1()
        {
            int b = 0;
            void M2()
            {
                int c = b;
                void M3()
                {
                    Print(c);
                    c = 2;
                }
                Print(b);
                M3();
                Print(c);
                b = 2;
            }
            M2();
            Print(b);
        }
        M1();
    }

    static void E()
    {
        int a = 0;
        void M1()
        {
            void M2()
            {
                void M3()
                {
                    Print(a);
                    a = 2;
                }
                M3();
                Print(a);
            }
            M2();
            Print(a);
        }
        M1();
        Print(a);
    }

    static void F()
    {
        void M1()
        {
            int b = 0;
            void M2()
            {
                void M3()
                {
                    Print(b);
                    b = 2;
                }
                M3();
                Print(b);
            }
            M2();
            Print(b);
        }
        M1();
    }

    static void G()
    {
        void M1()
        {
            void M2()
            {
                int c = 0;
                void M3()
                {
                    Print(c);
                    c = 2;
                }
                M3();
                Print(c);
            }
            M2();
        }
        M1();
    }

    static void Main(string[] args)
    {
        A();
        Console.WriteLine();
        B();
        Console.WriteLine();
        C();
        Console.WriteLine();
        D();
        Console.WriteLine();
        E();
        Console.WriteLine();
        F();
        Console.WriteLine();
        G();
        Console.WriteLine();
    }
}
";
            VerifyOutput(source, @"
 0 0 0 2 2 2
 0 0 2 2 2
 0 0 2 2 2
 0 0 2 2
 0 2 2 2
 0 2 2
 0 2
");
        }

        [Fact]
        public void InstanceClosure()
        {
            var source = @"
using System;

class Program
{
    int w;

    int A(int y)
    {
        int x = 1;
        int Local1(int z)
        {
            int Local2()
            {
                return Local1(x + y + w);
            }
            return z != -1 ? z : Local2();
        }
        return Local1(-1);
    }

    static void Main(string[] args)
    {
        var prog = new Program();
        prog.w = 3;
        Console.WriteLine(prog.A(2));
    }
}
";
            VerifyOutput(source, "6");
        }

        [Fact]
        public void SelfClosure()
        {
            var source = @"
using System;

class Program
{
    static int Test()
    {
        int x = 2;
        int Local1(int y)
        {
            int Local2()
            {
                return Local1(x);
            }
            return y != 0 ? y : Local2();
        }
        return Local1(0);
    }

    static void Main(string[] args)
    {
        Console.WriteLine(Test());
    }
}
";
            VerifyOutput(source, "2");
        }

        [Fact]
        public void StructClosure()
        {
            var source = @"
int x = 2;
void Goo()
{
    Console.Write(x);
}
Goo();
";
            var verify = VerifyOutputInMain(source, "2", "System");
            var goo = verify.FindLocalFunction("Goo");
            var program = verify.Compilation.GetTypeByMetadataName("Program");
            Assert.Equal(program, goo.ContainingType);
            Assert.True(goo.IsStatic);
            Assert.Equal(RefKind.Ref, goo.Parameters[0].RefKind);
            Assert.True(goo.Parameters[0].Type.IsValueType);
        }

        [Fact]
        public void StructClosureGeneric()
        {
            var source = @"
int x = 2;
void Goo<T1>()
{
    int y = x;
    void Bar<T2>()
    {
        Console.Write(x + y);
    }
    Bar<T1>();
}
Goo<int>();
";
            var verify = VerifyOutputInMain(source, "4", "System");
            var goo = verify.FindLocalFunction("Goo");
            var bar = verify.FindLocalFunction("Bar");
            Assert.Equal(1, goo.Parameters.Length);
            Assert.Equal(2, bar.Parameters.Length);
            Assert.Equal(RefKind.Ref, goo.Parameters[0].RefKind);
            Assert.Equal(RefKind.Ref, bar.Parameters[0].RefKind);
            Assert.Equal(RefKind.Ref, bar.Parameters[1].RefKind);
            Assert.True(goo.Parameters[0].Type.IsValueType);
            Assert.True(bar.Parameters[0].Type.IsValueType);
            Assert.True(bar.Parameters[1].Type.IsValueType);
            Assert.Equal(goo.Parameters[0].Type.OriginalDefinition, bar.Parameters[0].Type.OriginalDefinition);
            var gooFrame = (INamedTypeSymbol)goo.Parameters[0].Type;
            var barFrame = (INamedTypeSymbol)bar.Parameters[1].Type;
            Assert.Equal(0, gooFrame.Arity);
            Assert.Equal(1, barFrame.Arity);
        }

        [Fact]
        public void ClosureOfStructClosure()
        {
            var source = @"
void Outer()
{
    int a = 0;
    void Middle()
    {
        int b = 0;
        void Inner()
        {
            a++;
            b++;
        }

        a++;
        Inner();
    }

    Middle();
    Console.WriteLine(a);
}

Outer();
";
            var verify = VerifyOutputInMain(source, "2", "System");
            var inner = verify.FindLocalFunction("Inner");
            var middle = verify.FindLocalFunction("Middle");
            var outer = verify.FindLocalFunction("Outer");
            var program = verify.Compilation.GetTypeByMetadataName("Program");
            Assert.Equal(program, inner.ContainingType);
            Assert.Equal(program, middle.ContainingType);
            Assert.Equal(program, outer.ContainingType);
            Assert.True(inner.IsStatic);
            Assert.True(middle.IsStatic);
            Assert.True(outer.IsStatic);
            Assert.Equal(2, inner.Parameters.Length);
            Assert.Equal(1, middle.Parameters.Length);
            Assert.Equal(0, outer.Parameters.Length);
            Assert.Equal(RefKind.Ref, inner.Parameters[0].RefKind);
            Assert.Equal(RefKind.Ref, inner.Parameters[1].RefKind);
            Assert.Equal(RefKind.Ref, middle.Parameters[0].RefKind);
            Assert.True(inner.Parameters[0].Type.IsValueType);
            Assert.True(inner.Parameters[1].Type.IsValueType);
            Assert.True(middle.Parameters[0].Type.IsValueType);
        }

        [Fact]
        public void ThisClosureCallingOtherClosure()
        {
            var source = @"
using System;

class Program
{
    int _x;
    int Test()
    {
        int First()
        {
            return ++_x;
        }
        int Second()
        {
            return First();
        }
        return Second();
    }
    static void Main()
    {
        Console.Write(new Program() { _x = 1 }.Test());
    }
}
";
            var verify = VerifyOutput(source, "2");
            var program = verify.Compilation.GetTypeByMetadataName("Program");
            Assert.Equal(program, verify.FindLocalFunction("First").ContainingType);
            Assert.Equal(program, verify.FindLocalFunction("Second").ContainingType);
        }

        [Fact]
        public void RecursiveStructClosure()
        {
            var source = @"
int x = 0;
void Goo()
{
    if (x != 2)
    {
        x++;
        Goo();
    }
    else
    {
        Console.Write(x);
    }
}
Goo();
";
            var verify = VerifyOutputInMain(source, "2", "System");
            var goo = verify.FindLocalFunction("Goo");
            var program = verify.Compilation.GetTypeByMetadataName("Program");
            Assert.Equal(program, goo.ContainingType);
            Assert.True(goo.IsStatic);
            Assert.Equal(RefKind.Ref, goo.Parameters[0].RefKind);
            Assert.True(goo.Parameters[0].Type.IsValueType);
        }

        [Fact]
        public void MutuallyRecursiveStructClosure()
        {
            var source = @"
int x = 0;
void Goo(int depth)
{
    int dummy = 0;
    void Bar(int depth2)
    {
        dummy++;
        Goo(depth2);
    }
    if (depth != 2)
    {
        x++;
        Bar(depth + 1);
    }
    else
    {
        Console.Write(x);
    }
}
Goo(0);
";
            var verify = VerifyOutputInMain(source, "2", "System");
            var program = verify.Compilation.GetTypeByMetadataName("Program");
            var goo = verify.FindLocalFunction("Goo");
            var bar = verify.FindLocalFunction("Bar");
            Assert.Equal(program, goo.ContainingType);
            Assert.Equal(program, bar.ContainingType);
            Assert.True(goo.IsStatic);
            Assert.True(bar.IsStatic);
            Assert.Equal(2, goo.Parameters.Length);
            Assert.Equal(3, bar.Parameters.Length);
            Assert.Equal(RefKind.Ref, goo.Parameters[1].RefKind);
            Assert.Equal(RefKind.Ref, bar.Parameters[1].RefKind);
            Assert.Equal(RefKind.Ref, bar.Parameters[2].RefKind);
            Assert.True(goo.Parameters[1].Type.IsValueType);
            Assert.True(bar.Parameters[1].Type.IsValueType);
            Assert.True(bar.Parameters[2].Type.IsValueType);
        }

        [Fact]
        public void Recursion()
        {
            var source = @"
void Goo(int depth)
{
    if (depth > 10)
    {
        Console.WriteLine(2);
        return;
    }
    Goo(depth + 1);
}
Goo(0);
";
            VerifyOutputInMain(source, "2", "System");
        }

        [Fact]
        public void MutualRecursion()
        {
            var source = @"
void Goo(int depth)
{
    if (depth > 10)
    {
        Console.WriteLine(2);
        return;
    }
    void Bar(int depth2)
    {
        Goo(depth2 + 1);
    }
    Bar(depth + 1);
}
Goo(0);
";
            VerifyOutputInMain(source, "2", "System");
        }

        [Fact]
        public void RecursionThisOnlyClosure()
        {
            var source = @"
using System;

class Program
{
    int _x;
    void Outer()
    {
        void Inner()
        {
            if (_x == 0)
            {
                return;
            }
            Console.Write(_x);
            Console.Write(' ');
            _x = 0;
            Inner();
        }
        Inner();
    }
    public static void Main()
    {
        new Program() { _x = 2 }.Outer();
    }
}
";
            var verify = VerifyOutput(source, "2");
            var program = verify.Compilation.GetTypeByMetadataName("Program");
            Assert.Equal(program, verify.FindLocalFunction("Inner").ContainingType);
        }

        [Fact]
        public void RecursionFrameCaptureTest()
        {
            // ensures that referring to a local function in an otherwise noncapturing Inner captures the frame of Outer.
            var source = @"
int x = 0;
int Outer(bool isRecursive)
{
    if (isRecursive)
    {
        return x;
    }
    x++;
    int Middle()
    {
        int Inner()
        {
            return Outer(true);
        }
        return Inner();
    }
    return Middle();
}
Console.Write(Outer(false));
Console.Write(' ');
Console.Write(x);
";
            VerifyOutputInMain(source, "1 1", "System");
        }

        [Fact]
        [CompilerTrait(CompilerFeature.Iterator)]
        public void IteratorBasic()
        {
            var source = @"
IEnumerable<int> Local()
{
    yield return 2;
}
Console.Write(string.Join("","", Local()));
";
            VerifyOutputInMain(source, "2", "System", "System.Collections.Generic");
        }

        [Fact]
        [CompilerTrait(CompilerFeature.Iterator)]
        public void IteratorGeneric()
        {
            var source = @"
IEnumerable<T> LocalGeneric<T>(T val)
{
    yield return val;
}
Console.Write(string.Join("","", LocalGeneric(2)));
";
            VerifyOutputInMain(source, "2", "System", "System.Collections.Generic");
        }

        [Fact]
        [CompilerTrait(CompilerFeature.Iterator)]
        public void IteratorNonGeneric()
        {
            var source = @"
IEnumerable LocalNongen()
{
    yield return 2;
}
foreach (int x in LocalNongen())
{
    Console.Write(x);
}
";
            VerifyOutputInMain(source, "2", "System", "System.Collections");
        }

        [Fact]
        [CompilerTrait(CompilerFeature.Iterator)]
        public void IteratorEnumerator()
        {
            var source = @"
IEnumerator LocalEnumerator()
{
    yield return 2;
}
var y = LocalEnumerator();
y.MoveNext();
Console.Write(y.Current);
";
            VerifyOutputInMain(source, "2", "System", "System.Collections");
        }

        [Fact]
        public void Generic()
        {
            var source = @"
using System;

class Program
{
    // No closure. Return 'valu'.
    static T A1<T>(T val)
    {
        T Local(T valu)
        {
            return valu;
        }
        return Local(val);
    }
    static int B1(int val)
    {
        T Local<T>(T valu)
        {
            return valu;
        }
        return Local(val);
    }
    static T1 C1<T1>(T1 val)
    {
        T2 Local<T2>(T2 valu)
        {
            return valu;
        }
        return Local<T1>(val);
    }
    // General closure. Return 'val'.
    static T A2<T>(T val)
    {
        T Local(T valu)
        {
            return val;
        }
        return Local(val);
    }
    static int B2(int val)
    {
        T Local<T>(T valu)
        {
            return (T)(object)val;
        }
        return Local(val);
    }
    static T1 C2<T1>(T1 val)
    {
        T2 Local<T2>(T2 valu)
        {
            return (T2)(object)val;
        }
        return Local<T1>(val);
    }
    // This-only closure. Return 'field'.
    int field = 2;
    T A3<T>(T val)
    {
        T Local(T valu)
        {
            return (T)(object)field;
        }
        return Local(val);
    }
    int B3(int val)
    {
        T Local<T>(T valu)
        {
            return (T)(object)field;
        }
        return Local(val);
    }
    T1 C3<T1>(T1 val)
    {
        T2 Local<T2>(T2 valu)
        {
            return (T2)(object)field;
        }
        return Local<T1>(val);
    }
    static void Main(string[] args)
    {
        var program = new Program();
        Console.WriteLine(Program.A1(2));
        Console.WriteLine(Program.B1(2));
        Console.WriteLine(Program.C1(2));
        Console.WriteLine(Program.A2(2));
        Console.WriteLine(Program.B2(2));
        Console.WriteLine(Program.C2(2));
        Console.WriteLine(program.A3(2));
        Console.WriteLine(program.B3(2));
        Console.WriteLine(program.C3(2));
    }
}
";
            var output = @"
2
2
2
2
2
2
2
2
2
";
            VerifyOutput(source, output);
        }

        [Fact]
        public void GenericConstraint()
        {
            var source = @"
using System;

class Program
{
    static T A<T>(T val) where T : struct
    {
        T Local(T valu)
        {
            return valu;
        }
        return Local(val);
    }
    static int B(int val)
    {
        T Local<T>(T valu) where T : struct
        {
            return valu;
        }
        return Local(val);
    }
    static T1 C<T1>(T1 val) where T1 : struct
    {
        T2 Local<T2>(T2 valu) where T2 : struct
        {
            return valu;
        }
        return Local(val);
    }
    static object D(object val)
    {
        T Local<T>(T valu) where T : class
        {
            return valu;
        }
        return Local(val);
    }
    static void Main(string[] args)
    {
        Console.WriteLine(A(2));
        Console.WriteLine(B(2));
        Console.WriteLine(C(2));
        Console.WriteLine(D(2));
    }
}
";
            var output = @"
2
2
2
2
";
            VerifyOutput(source, output);
        }

        [Fact]
        public void GenericTripleNestedNoClosure()
        {
            var source = @"
using System;

class Program
{
    // Name of method is T[outer][middle][inner] where brackets are g=generic n=nongeneric
    // One generic
    static T1 Tgnn<T1>(T1 a)
    {
        T1 Local(T1 aa)
        {
            T1 Local2(T1 aaa)
            {
                return aaa;
            }
            return Local2(aa);
        }
        return Local(a);
    }
    static int Tngn(int a)
    {
        T1 Local<T1>(T1 aa)
        {
            T1 Local2(T1 aaa)
            {
                return aaa;
            }
            return Local2(aa);
        }
        return Local(a);
    }
    static int Tnng(int a)
    {
        int Local(int aa)
        {
            T1 Local2<T1>(T1 aaa)
            {
                return aaa;
            }
            return Local2(aa);
        }
        return Local(a);
    }
    // Two generic
    static T1 Tggn<T1>(T1 a)
    {
        T2 Local<T2>(T2 aa)
        {
            T2 Local2(T2 aaa)
            {
                return aaa;
            }
            return Local2(aa);
        }
        return Local(a);
    }
    static T1 Tgng<T1>(T1 a)
    {
        T1 Local(T1 aa)
        {
            T2 Local2<T2>(T2 aaa)
            {
                return aaa;
            }
            return Local2(aa);
        }
        return Local(a);
    }
    static int Tngg(int a)
    {
        T1 Local<T1>(T1 aa)
        {
            T2 Local2<T2>(T2 aaa)
            {
                return aaa;
            }
            return Local2(aa);
        }
        return Local(a);
    }
    // Three generic
    static T1 Tggg<T1>(T1 a)
    {
        T2 Local<T2>(T2 aa)
        {
            T3 Local2<T3>(T3 aaa)
            {
                return aaa;
            }
            return Local2(aa);
        }
        return Local(a);
    }
    static void Main(string[] args)
    {
        Console.WriteLine(Program.Tgnn(2));
        Console.WriteLine(Program.Tngn(2));
        Console.WriteLine(Program.Tnng(2));
        Console.WriteLine(Program.Tggn(2));
        Console.WriteLine(Program.Tgng(2));
        Console.WriteLine(Program.Tngg(2));
        Console.WriteLine(Program.Tggg(2));
    }
}
";
            var output = @"
2
2
2
2
2
2
2
";
            VerifyOutput(source, output);
        }

        [Fact]
        public void GenericTripleNestedMiddleClosure()
        {
            var source = @"
using System;

class Program
{
    // Name of method is T[outer][middle][inner] where brackets are g=generic n=nongeneric
    // One generic
    static T1 Tgnn<T1>(T1 a)
    {
        T1 Local(T1 aa)
        {
            T1 Local2(T1 aaa)
            {
                return (T1)(object)aa;
            }
            return Local2(aa);
        }
        return Local(a);
    }
    static int Tngn(int a)
    {
        T1 Local<T1>(T1 aa)
        {
            T1 Local2(T1 aaa)
            {
                return (T1)(object)aa;
            }
            return Local2(aa);
        }
        return Local(a);
    }
    static int Tnng(int a)
    {
        int Local(int aa)
        {
            T1 Local2<T1>(T1 aaa)
            {
                return (T1)(object)aa;
            }
            return Local2(aa);
        }
        return Local(a);
    }
    // Two generic
    static T1 Tggn<T1>(T1 a)
    {
        T2 Local<T2>(T2 aa)
        {
            T2 Local2(T2 aaa)
            {
                return (T2)(object)aa;
            }
            return Local2(aa);
        }
        return Local(a);
    }
    static T1 Tgng<T1>(T1 a)
    {
        T1 Local(T1 aa)
        {
            T2 Local2<T2>(T2 aaa)
            {
                return (T2)(object)aa;
            }
            return Local2(aa);
        }
        return Local(a);
    }
    static int Tngg(int a)
    {
        T1 Local<T1>(T1 aa)
        {
            T2 Local2<T2>(T2 aaa)
            {
                return (T2)(object)aa;
            }
            return Local2(aa);
        }
        return Local(a);
    }
    // Three generic
    static T1 Tggg<T1>(T1 a)
    {
        T2 Local<T2>(T2 aa)
        {
            T3 Local2<T3>(T3 aaa)
            {
                return (T3)(object)aa;
            }
            return Local2(aa);
        }
        return Local(a);
    }
    static void Main(string[] args)
    {
        Console.WriteLine(Program.Tgnn(2));
        Console.WriteLine(Program.Tngn(2));
        Console.WriteLine(Program.Tnng(2));
        Console.WriteLine(Program.Tggn(2));
        Console.WriteLine(Program.Tgng(2));
        Console.WriteLine(Program.Tngg(2));
        Console.WriteLine(Program.Tggg(2));
    }
}
";
            var output = @"
2
2
2
2
2
2
2
";
            VerifyOutput(source, output);
        }

        [Fact]
        public void GenericTripleNestedOuterClosure()
        {
            var source = @"
using System;

class Program
{
    // Name of method is T[outer][middle][inner] where brackets are g=generic n=nongeneric
    // One generic
    static T1 Tgnn<T1>(T1 a)
    {
        T1 Local(T1 aa)
        {
            T1 Local2(T1 aaa)
            {
                return (T1)(object)a;
            }
            return Local2(aa);
        }
        return Local(a);
    }
    static int Tngn(int a)
    {
        T1 Local<T1>(T1 aa)
        {
            T1 Local2(T1 aaa)
            {
                return (T1)(object)a;
            }
            return Local2(aa);
        }
        return Local(a);
    }
    static int Tnng(int a)
    {
        int Local(int aa)
        {
            T1 Local2<T1>(T1 aaa)
            {
                return (T1)(object)a;
            }
            return Local2(aa);
        }
        return Local(a);
    }
    // Two generic
    static T1 Tggn<T1>(T1 a)
    {
        T2 Local<T2>(T2 aa)
        {
            T2 Local2(T2 aaa)
            {
                return (T2)(object)a;
            }
            return Local2(aa);
        }
        return Local(a);
    }
    static T1 Tgng<T1>(T1 a)
    {
        T1 Local(T1 aa)
        {
            T2 Local2<T2>(T2 aaa)
            {
                return (T2)(object)a;
            }
            return Local2(aa);
        }
        return Local(a);
    }
    static int Tngg(int a)
    {
        T1 Local<T1>(T1 aa)
        {
            T2 Local2<T2>(T2 aaa)
            {
                return (T2)(object)a;
            }
            return Local2(aa);
        }
        return Local(a);
    }
    // Three generic
    static T1 Tggg<T1>(T1 a)
    {
        T2 Local<T2>(T2 aa)
        {
            T3 Local2<T3>(T3 aaa)
            {
                return (T3)(object)a;
            }
            return Local2(aa);
        }
        return Local(a);
    }
    static void Main(string[] args)
    {
        Console.WriteLine(Program.Tgnn(2));
        Console.WriteLine(Program.Tngn(2));
        Console.WriteLine(Program.Tnng(2));
        Console.WriteLine(Program.Tggn(2));
        Console.WriteLine(Program.Tgng(2));
        Console.WriteLine(Program.Tngg(2));
        Console.WriteLine(Program.Tggg(2));
    }
}
";
            var output = @"
2
2
2
2
2
2
2
";
            VerifyOutput(source, output);
        }

        [Fact]
        public void GenericTripleNestedNoClosureLambda()
        {
            var source = @"
using System;

class Program
{
    // Name of method is T[outer][middle][inner] where brackets are g=generic n=nongeneric
    // One generic
    static T1 Tgnn<T1>(T1 a)
    {
        Func<T1, T1> Local = aa =>
        {
            Func<T1, T1> Local2 = aaa =>
            {
                return aaa;
            };
            return Local2(aa);
        };
        return Local(a);
    }
    static int Tngn(int a)
    {
        T1 Local<T1>(T1 aa)
        {
            Func<T1, T1> Local2 = aaa =>
            {
                return aaa;
            };
            return Local2(aa);
        }
        return Local(a);
    }
    static int Tnng(int a)
    {
        Func<int, int> Local = aa =>
        {
            T1 Local2<T1>(T1 aaa)
            {
                return aaa;
            }
            return Local2(aa);
        };
        return Local(a);
    }
    // Two generic
    static T1 Tggn<T1>(T1 a)
    {
        T2 Local<T2>(T2 aa)
        {
            Func<T2, T2> Local2 = aaa =>
            {
                return aaa;
            };
            return Local2(aa);
        }
        return Local(a);
    }
    static T1 Tgng<T1>(T1 a)
    {
        Func<T1, T1> Local = aa =>
        {
            T2 Local2<T2>(T2 aaa)
            {
                return aaa;
            }
            return Local2(aa);
        };
        return Local(a);
    }
    // Tngg and Tggg are impossible with lambdas
    static void Main(string[] args)
    {
        Console.WriteLine(Program.Tgnn(2));
        Console.WriteLine(Program.Tngn(2));
        Console.WriteLine(Program.Tnng(2));
        Console.WriteLine(Program.Tggn(2));
        Console.WriteLine(Program.Tgng(2));
    }
}
";
            var output = @"
2
2
2
2
2
";
            VerifyOutput(source, output);
        }

        [Fact]
        public void GenericUpperCall()
        {
            var source = @"
using System;

class Program
{
    static T1 InnerToOuter<T1>(T1 a)
    {
        T2 Local<T2>(T2 aa)
        {
            T3 Local2<T3>(T3 aaa)
            {
                if ((object)aaa == null)
                    return InnerToOuter((T3)new object());
                return aaa;
            }
            return Local2(aa);
        }
        return Local(a);
    }
    static T1 InnerToMiddle<T1>(T1 a)
    {
        T2 Local<T2>(T2 aa)
        {
            T3 Local2<T3>(T3 aaa)
            {
                if ((object)aaa == null)
                    return InnerToMiddle((T3)new object());
                return aaa;
            }
            return Local2(aa);
        }
        return Local(a);
    }
    static T1 InnerToOuterScoping<T1>(T1 a)
    {
        T2 Local<T2>(T2 aa)
        {
            T3 Local2<T3>(T3 aaa)
            {
                if ((object)aaa == null)
                    return (T3)(object)InnerToOuter((T1)new object());
                return aaa;
            }
            return Local2(aa);
        }
        return Local(a);
    }
    static T1 M1<T1>(T1 a)
    {
        T2 M2<T2>(T2 aa)
        {
            T2 x = aa;
            T3 M3<T3>(T3 aaa)
            {
                T4 M4<T4>(T4 aaaa)
                {
                    return (T4)(object)x;
                }
                return M4(aaa);
            }
            return M3(aa);
        }
        return M2(a);
    }
    static void Main(string[] args)
    {
        Console.WriteLine(Program.InnerToOuter((object)null));
        Console.WriteLine(Program.InnerToMiddle((object)null));
        Console.WriteLine(Program.InnerToOuterScoping((object)null));
        Console.WriteLine(Program.M1(2));
    }
}
";
            var output = @"
System.Object
System.Object
System.Object
2
";
            VerifyOutput(source, output);
        }

        [Fact]
        public void CompoundOperatorExecutesOnce()
        {
            var source = @"
using System;

class Program
{
    int _x = 2;
    public static void Main()
    {
        var prog = new Program();
        Program SideEffect()
        {
            Console.Write(prog._x);
            return prog;
        }
        SideEffect()._x += 2;
        Console.Write(' ');
        SideEffect();
    }
}
";
            VerifyOutput(source, "2 4");
        }

        [Fact]
        public void ConstValueDoesntMakeClosure()
        {
            var source = @"
const int x = 2;
void Local()
{
    Console.Write(x);
}
Local();
";
            // Should be a static method on "Program" itself, not a display class like "Program+<>c__DisplayClass0_0"
            var verify = VerifyOutputInMain(source, "2", "System");
            var goo = verify.FindLocalFunction("Local");
            Assert.True(goo.IsStatic);
            Assert.Equal(verify.Compilation.GetTypeByMetadataName("Program"), goo.ContainingType);
        }

        [Fact]
        [CompilerTrait(CompilerFeature.Dynamic)]
        public void DynamicArgument()
        {
            var source = @"
using System;
class Program
{
    static void Main()
    {
        int capture1 = 0;
        void L1(int x) => Console.Write(x);
        void L2(int x)
        {
            Console.Write(capture1);
            Console.Write(x);
        }
        dynamic L4(int x)
        {
            Console.Write(capture1);
            return x;
        }
        Action<int> L5(int x)
        {
            Console.Write(x);
            return L1;
        }

        dynamic val = 2;
        Console.WriteLine();
        L1(val);
        L2(val);
        Console.WriteLine();
        L2(L4(val));
        L5(val)(val);
    }
}
";
            VerifyOutput(source, output: @"202
00222");
        }

        [Fact]
        [WorkItem(21317, "https://github.com/dotnet/roslyn/issues/21317")]
        [CompilerTrait(CompilerFeature.Dynamic)]
        public void DynamicGenericArg()
        {
            var src = @"
void L1<T>(T x)
{
    Console.WriteLine($""{x}: {typeof(T)}"");
}
dynamic val = 2;
L1<object>(val);
L1<int>(val);
L1<dynamic>(val);
L1<dynamic>(4);

void L2<T>(int x, T y) => Console.WriteLine($""{x}, {y}: {typeof(T)}"");
L2<float>(val, 3.0f);

List<dynamic> listOfDynamic = new List<dynamic> { 1, 2, 3 };
void L3<T>(List<T> x) => Console.WriteLine($""{string.Join("", "", x)}: {typeof(T)}"");
L3(listOfDynamic);

void L4<T>(T x, params int[] y) => Console.WriteLine($""{x}, {string.Join("", "", y)}: {typeof(T)}"");
L4<dynamic>(val, 3, 4);
L4<int>(val, 3, 4);
L4<int>(1, 3, val);

void L5<T>(int x, params T[] y) => Console.WriteLine($""{x}, {string.Join("", "", y)}: {typeof(T)}"");
L5<int>(val, 3, 4);
L5<int>(1, 3, val);
L5<dynamic>(1, 3, val);
";
            var output = @"
2: System.Object
2: System.Int32
2: System.Object
4: System.Object
2, 3: System.Single
1, 2, 3: System.Object
2, 3, 4: System.Object
2, 3, 4: System.Int32
1, 3, 2: System.Int32
2, 3, 4: System.Int32
1, 3, 2: System.Int32
1, 3, 2: System.Object
";
            VerifyOutputInMain(src, output, "System", "System.Collections.Generic");
        }

        [Fact]
        [WorkItem(21317, "https://github.com/dotnet/roslyn/issues/21317")]
        [CompilerTrait(CompilerFeature.Dynamic)]
        public void DynamicGenericClassMethod()
        {
            var src = @"
using System;
class C1<T1>
{
    public static void M1<T2>()
    {
        void F(int x)
        {
            Console.WriteLine($""C1<{typeof(T1)}>.M1<{typeof(T2)}>.F({x})"");
        }
        F((dynamic)2);
    }
    public static void M2()
    {
        void F(int x)
        {
            Console.WriteLine($""C1<{typeof(T1)}>.M2.F({x})"");
        }
        F((dynamic)2);
    }
}
class C2
{
    public static void M1<T2>()
    {
        void F(int x)
        {
            Console.WriteLine($""C2.M1<{typeof(T2)}>.F({x})"");
        }
        F((dynamic)2);
    }
    public static void M2()
    {
        void F(int x)
        {
            Console.WriteLine($""C2.M2.F({x})"");
        }
        F((dynamic)2);
    }
}
class Program
{
    static void Main()
    {
        C1<int>.M1<float>();
        C1<int>.M2();
        C2.M1<float>();
        C2.M2();
    }
}
";
            var output = @"
C1<System.Int32>.M1<System.Single>.F(2)
C1<System.Int32>.M2.F(2)
C2.M1<System.Single>.F(2)
C2.M2.F(2)
";
            VerifyOutput(src, output);
        }

        [Fact]
        [CompilerTrait(CompilerFeature.Dynamic, CompilerFeature.Params)]
        public void DynamicArgsAndParams()
        {
            var src = @"
int capture1 = 0;
void L1(int x, params int[] ys)
{
    Console.Write(capture1);
    Console.Write(x);
    foreach (var y in ys)
    {
        Console.Write(y);
    }
}

dynamic val = 2;
int val2 = 3;
L1(val, val2);
L1(val);
L1(val, val, val);
";
            VerifyOutputInMain(src, "023020222", "System");
        }

        [Fact]
        public void Basic()
        {
            var source = @"
async Task<int> Local()
{
    return await Task.FromResult(2);
}
Console.Write(Local().Result);
";
            VerifyOutputInMain(source, "2", "System", "System.Threading.Tasks");
        }

        [Fact]
        public void Param()
        {
            var source = @"
async Task<int> LocalParam(int x)
{
    return await Task.FromResult(x);
}
Console.Write(LocalParam(2).Result);
";
            VerifyOutputInMain(source, "2", "System", "System.Threading.Tasks");
        }

        [Fact]
        [CompilerTrait(CompilerFeature.Async)]
        public void GenericAsync()
        {
            var source = @"
async Task<T> LocalGeneric<T>(T x)
{
    return await Task.FromResult(x);
}
Console.Write(LocalGeneric(2).Result);
";
            VerifyOutputInMain(source, "2", "System", "System.Threading.Tasks");
        }

        [Fact]
        [CompilerTrait(CompilerFeature.Async)]
        public void Void()
        {
            var source = @"
// had bug with parser where 'async [keyword]' didn't parse.
async void LocalVoid()
{
    Console.Write(2);
    await Task.Yield();
}
LocalVoid();
";
            VerifyOutputInMain(source, "2", "System", "System.Threading.Tasks");
        }

        [Fact]
        [CompilerTrait(CompilerFeature.Async)]
        public void AwaitAwait()
        {
            var source = @"
Task<int> Fun(int x)
{
    return Task.FromResult(x);
}
async Task<int> AwaitAwait()
{
    var a = Fun(2);
    await Fun(await a);
    return await Fun(await a);
}
Console.WriteLine(AwaitAwait().Result);
";
            VerifyOutputInMain(source, "2", "System", "System.Threading.Tasks");
        }

        [Fact]
        [CompilerTrait(CompilerFeature.Async)]
        public void Keyword()
        {
            var source = @"
using System;

struct async
{
    public override string ToString() => ""2"";
}
struct await
{
    public override string ToString() => ""2"";
}

class Program
{
    static string A()
    {
        async async()
        {
            return new async();
        }
        return async().ToString();
    }
    static string B()
    {
        string async()
        {
            return ""2"";
        }
        return async();
    }
    static string C()
    {
        async Goo()
        {
            return new async();
        }
        return Goo().ToString();
    }
    static string D()
    {
        await Fun(await x)
        {
            return x;
        }
        return Fun(new await()).ToString();
    }

    static void Main(string[] args)
    {
        Console.WriteLine(A());
        Console.WriteLine(B());
        Console.WriteLine(C());
        Console.WriteLine(D());
    }
}
";
            var output = @"
2
2
2
2
";
            VerifyOutput(source, output);
        }

        [Fact]
        [CompilerTrait(CompilerFeature.Async)]
        public void UnsafeKeyword()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program
{
    static string A()
    {
        async unsafe Task<int> async()
        {
            return 2;
        }
        return async().Result.ToString();
    }
    static string B()
    {
        unsafe async Task<int> async()
        {
            return 2;
        }
        return async().Result.ToString();
    }

    static void Main(string[] args)
    {
        Console.WriteLine(A());
        Console.WriteLine(B());
    }
}
";
            var output = @"
2
2
";
            VerifyOutput(source, output, TestOptions.ReleaseExe.WithAllowUnsafe(true).WithWarningLevel(0), verify: Verification.Passes);
        }

        [Fact]
        public void UnsafeBasic()
        {
            var source = @"
using System;

class Program
{
    static void A()
    {
        unsafe void Local()
        {
            int x = 2;
            Console.Write(*&x);
        }
        Local();
    }
    static void Main(string[] args)
    {
        A();
    }
}
";
            VerifyOutput(source, "2", TestOptions.ReleaseExe.WithAllowUnsafe(true), verify: Verification.Fails);
        }

        [Fact]
        public void UnsafeParameter()
        {
            var source = @"
using System;

class Program
{
    static unsafe void B()
    {
        int x = 2;
        unsafe void Local(int* y)
        {
            Console.Write(*y);
        }
        Local(&x);
    }
    static void Main(string[] args)
    {
        B();
    }
}
";
            VerifyOutput(source, "2", TestOptions.ReleaseExe.WithAllowUnsafe(true), verify: Verification.Fails);
        }

        [Fact]
        public void UnsafeClosure()
        {
            var source = @"
using System;

class Program
{
    static unsafe void C()
    {
        int y = 2;
        int* x = &y;
        unsafe void Local()
        {
            Console.Write(*x);
        }
        Local();
    }
    static void Main(string[] args)
    {
        C();
    }
}
";
            VerifyOutput(source, "2", TestOptions.ReleaseExe.WithAllowUnsafe(true), verify: Verification.Fails);
        }

        [Fact]
        public void UnsafeCalls()
        {
            var src = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        int x = 2;
        int y = 0;

        unsafe void Local(ref int x2)
        {
            fixed (int* ptr = &x2)
            {
                Local2(ptr);
            }
        }
        unsafe int* Local2(int* ptr)
        {
            (*ptr)++;
            y++;

            return null;
        }

        while (x < 10)
        {
            Local(ref x);
            x++;
        }

        Console.WriteLine(x);
        Console.WriteLine(y);
    }
}";
            VerifyOutput(src, $"10{Environment.NewLine}4", TestOptions.ReleaseExe.WithAllowUnsafe(true), verify: Verification.Fails);
        }

        [Fact]
        [WorkItem(15322, "https://github.com/dotnet/roslyn/issues/15322")]
        public void UseBeforeDeclaration()
        {
            var src = @"
Assign();
Local();
int x;
void Local() => System.Console.WriteLine(x);
void Assign() { x = 5; }";

            VerifyOutputInMain(src, "5");
        }

        [Fact]
        [WorkItem(15558, "https://github.com/dotnet/roslyn/issues/15558")]
        public void CapturingSharesVar()
        {
            var src = @"
int i = 0;

int oldi<T>()
    where T : struct
    => (i += @sizeof<T>()) - @sizeof<T>();

int @sizeof<T>()
    where T : struct
    => typeof(T).IsAssignableFrom(typeof(long))
        ? sizeof(long)
        : 1;

while (i < 10)
    System.Console.WriteLine(oldi<byte>());";

            VerifyOutputInMain(src, @"0
1
2
3
4
5
6
7
8
9");
        }

        [Fact]
        [WorkItem(15599, "https://github.com/dotnet/roslyn/issues/15599")]
        public void NestedLocalFuncCapture()
        {
            var src = @"
using System;
public class C {
    int instance = 11;
    public void M() {
        int M() => instance;

        {
            int local = 11;
            bool M2() => local == M();
            Console.WriteLine(M2());
        }
    }

    public static void Main() => new C().M();
}";
            VerifyOutput(src, "True");
        }

        [Fact]
        [WorkItem(15599, "https://github.com/dotnet/roslyn/issues/15599")]
        public void NestedLocalFuncCapture2()
        {
            var src = @"
using System;
public class C {
    int instance = 0b1;
    public void M() {
        int var1 = 0b10;
        int M() => var1 + instance;

        {
            int local = 0b100;
            int M2() => local + M();
            Console.WriteLine(M2());
        }
    }

    public static void Main() => new C().M();
}";
            VerifyOutput(src, "7");
        }

        [Fact]
        [WorkItem(15751, "https://github.com/dotnet/roslyn/issues/15751")]
        public void RecursiveGenericLocalFunction()
        {
            var src = @"
void Local<T>(T t, int count)
{
    if (count > 0)
    {
        Console.Write(t);
        Local(t, count - 1);
    }
}

Local(""A"", 5);
";
            VerifyOutputInMain(src, "AAAAA", "System");
        }

        [Fact]
        [WorkItem(15751, "https://github.com/dotnet/roslyn/issues/15751")]
        public void RecursiveGenericLocalFunction2()
        {
            var src = @"
void Local<T>(T t, int count)
{
    if (count > 0)
    {
        Console.Write(t);
        var action = new Action<T, int>(Local);
        action(t, count - 1);
    }
}

Local(""A"", 5);
";
            VerifyOutputInMain(src, "AAAAA", "System");
        }

        [Fact]
        [WorkItem(15751, "https://github.com/dotnet/roslyn/issues/15751")]
        public void RecursiveGenericLocalFunction3()
        {
            var src = @"
void Local<T>(T t, int count)
{
    if (count > 0)
    {
        Console.Write(t);
        var action = (Action<T, int>)Local;
        action(t, count - 1);
    }
}

Local(""A"", 5);
";
            VerifyOutputInMain(src, "AAAAA", "System");
        }

        [Fact]
        [WorkItem(15751, "https://github.com/dotnet/roslyn/issues/15751")]
        public void RecursiveGenericLocalFunction4()
        {
            var src = @"
using System;
class C
{
    public static void M<T>(T t)
    {
        void Local<U>(U u, int count)
        {
            if (count > 0)
            {
                Console.Write(t);
                Console.Write(u);
                Local(u, count - 1);
            }
        }
        Local(""A"", 5);
    }

    public static void Main()
    {
        C.M(""B"");
    }
}";
            VerifyOutput(src, "BABABABABA");
        }

        [Fact]
        [WorkItem(15751, "https://github.com/dotnet/roslyn/issues/15751")]
        public void RecursiveGenericLocalFunction5()
        {
            var src = @"
using System;
class C<T1>
{
    T1 t1;

    public C(T1 t1)
    {
        this.t1 = t1;
    }

    public void M<T2>(T2 t2)
    {
        void L1<T3>(T3 t3)
        {
            void L2<T4>(T4 t4)
            {
                void L3<U>(U u, int count)
                {
                    if (count > 0)
                    {
                        Console.Write(t1);
                        Console.Write(t2);
                        Console.Write(t3);
                        Console.Write(t4);
                        Console.Write(u);
                        L3(u, count - 1);
                    }
                }
                L3(""A"", 5);
            }
            L2(""B"");
        }
        L1(""C"");
    }

}

class Program
{
    public static void Main()
    {
        var c = new C<string>(""D"");
        c.M(""E"");
    }
}";
            VerifyOutput(src, "DECBADECBADECBADECBADECBA");
        }

        [Fact]
        [WorkItem(15751, "https://github.com/dotnet/roslyn/issues/15751")]
        public void RecursiveGenericLocalFunction6()
        {
            var src = @"
using System;
class C<T1>
{
    T1 t1;

    public C(T1 t1)
    {
        this.t1 = t1;
    }

    public void M<T2>(T2 t2)
    {
        void L1<T3>(T3 t3)
        {
            void L2<T4>(T4 t4)
            {
                void L3<U>(U u, int count)
                {
                    if (count > 0)
                    {
                        Console.Write(t1);
                        Console.Write(t2);
                        Console.Write(t3);
                        Console.Write(t4);
                        Console.Write(u);
                        var a = new Action<U, int>(L3);
                        a(u, count - 1);
                    }
                }
                var b = new Action<string, int>(L3);
                b(""A"", 5);
            }
            var c = new Action<string>(L2);
            c(""B"");
        }
        var d = new Action<string>(L1);
        d(""C"");
    }

}

class Program
{
    public static void Main()
    {
        var c = new C<string>(""D"");
        c.M(""E"");
    }
}";
            VerifyOutput(src, "DECBADECBADECBADECBADECBA");
        }

        [Fact]
        [WorkItem(15751, "https://github.com/dotnet/roslyn/issues/15751")]
        public void RecursiveGenericLocalFunction7()
        {
            var src = @"
using System;
class C<T1>
{
    T1 t1;

    public C(T1 t1)
    {
        this.t1 = t1;
    }

    public void M<T2>(T2 t2)
    {
        void L1<T3>(T3 t3)
        {
            void L2<T4>(T4 t4)
            {
                void L3<U>(U u, int count)
                {
                    if (count > 0)
                    {
                        Console.Write(t1);
                        Console.Write(t2);
                        Console.Write(t3);
                        Console.Write(t4);
                        Console.Write(u);
                        var a = (Action<U, int>)(L3);
                        a(u, count - 1);
                    }
                }
                var b = (Action<string, int>)(L3);
                b(""A"", 5);
            }
            var c = (Action<string>)(L2);
            c(""B"");
        }
        var d = (Action<string>)(L1);
        d(""C"");
    }

}

class Program
{
    public static void Main()
    {
        var c = new C<string>(""D"");
        c.M(""E"");
    }
}";
            VerifyOutput(src, "DECBADECBADECBADECBADECBA");
        }

        [Fact]
        [WorkItem(16038, "https://github.com/dotnet/roslyn/issues/16038")]
        public void RecursiveGenericLocalFunction8()
        {
            var src = @"
using System;
class C<T0>
{
    T0 t0;

    public C(T0 t0)
    {
        this.t0 = t0;
    }

    public void M<T1>(T1 t1)
    {
        (T0, T1, T2) L1<T2>(T2 t2)
        {
            (T0, T1, T2, T3) L2<T3>(T3 t3, int count)
            {
                if (count > 0)
                {
                    Console.Write(t0);
                    Console.Write(t1);
                    Console.Write(t2);
                    Console.Write(t3);
                    return L2(t3, count - 1);
                }
                return (t0, t1, t2, t3);
            }
            var (t4, t5, t6, t7) = L2(""A"", 5);
            return (t4, t5, t6);
        }
        L1(""B"");
    }
}

class Program
{
    public static void Main()
    {
        var c = new C<string>(""C"");
        c.M(""D"");
    }
}";
            CompileAndVerify(src, expectedOutput: "CDBACDBACDBACDBACDBA");
        }

        [Fact]
        [WorkItem(19119, "https://github.com/dotnet/roslyn/issues/19119")]
        public void StructFrameInitUnnecessary()
        {
            var c = CompileAndVerify(@"
    class Program
    {
        static void Main(string[] args)
        {
            int q = 1;

            if (q > 0)
            {
                int w = 2;
                if (w > 0)
                {
                    int e = 3;
                    if (e > 0)
                    {
                        void Print() => System.Console.WriteLine(q + w + e);

                        Print();
                    }
                }
            }
        }
    }", expectedOutput: "6");

            //NOTE: the following code should not have "initobj" instructions.

            c.VerifyIL("Program.Main", @"
{
  // Code size       63 (0x3f)
  .maxstack  3
  .locals init (Program.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                Program.<>c__DisplayClass0_1 V_1, //CS$<>8__locals1
                Program.<>c__DisplayClass0_2 V_2) //CS$<>8__locals2
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  stfld      ""int Program.<>c__DisplayClass0_0.q""
  IL_0008:  ldloc.0
  IL_0009:  ldfld      ""int Program.<>c__DisplayClass0_0.q""
  IL_000e:  ldc.i4.0
  IL_000f:  ble.s      IL_003e
  IL_0011:  ldloca.s   V_1
  IL_0013:  ldc.i4.2
  IL_0014:  stfld      ""int Program.<>c__DisplayClass0_1.w""
  IL_0019:  ldloc.1
  IL_001a:  ldfld      ""int Program.<>c__DisplayClass0_1.w""
  IL_001f:  ldc.i4.0
  IL_0020:  ble.s      IL_003e
  IL_0022:  ldloca.s   V_2
  IL_0024:  ldc.i4.3
  IL_0025:  stfld      ""int Program.<>c__DisplayClass0_2.e""
  IL_002a:  ldloc.2
  IL_002b:  ldfld      ""int Program.<>c__DisplayClass0_2.e""
  IL_0030:  ldc.i4.0
  IL_0031:  ble.s      IL_003e
  IL_0033:  ldloca.s   V_0
  IL_0035:  ldloca.s   V_1
  IL_0037:  ldloca.s   V_2
  IL_0039:  call       ""void Program.<Main>g__Print|0_0(ref Program.<>c__DisplayClass0_0, ref Program.<>c__DisplayClass0_1, ref Program.<>c__DisplayClass0_2)""
  IL_003e:  ret
}
");
        }

        [Fact]
        public void LocalFunctionAttribute()
        {
            var source = @"
class A : System.Attribute { }

class C
{
    public void M()
    {
        [A]
        void local1()
        {
        }

        [return: A]
        void local2()
        {
        }

        void local3([A] int i)
        {
        }

        void local4<[A] T>()
        {
        }
    }
}
";
            CompileAndVerify(
                source,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                parseOptions: TestOptions.RegularPreview,
                symbolValidator: validate);

            void validate(ModuleSymbol module)
            {
                var cClass = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                var aAttribute = module.GlobalNamespace.GetMember<NamedTypeSymbol>("A");

                var localFn1 = cClass.GetMethod("<M>g__local1|0_0");
                var attrs1 = localFn1.GetAttributes();
                Assert.Equal(
                    expected: new[]
                    {
                        module.CorLibrary().GetTypeByMetadataName("System.Runtime.CompilerServices.CompilerGeneratedAttribute"),
                        aAttribute
                    },
                    actual: attrs1.Select(a => a.AttributeClass));

                var localFn2 = cClass.GetMethod("<M>g__local2|0_1");
                var attrs2 = localFn2.GetReturnTypeAttributes();
                Assert.Equal(aAttribute, attrs2.Single().AttributeClass);

                var localFn3 = cClass.GetMethod("<M>g__local3|0_2");
                var attrs3 = localFn3.GetParameters().Single().GetAttributes();
                Assert.Equal(aAttribute, attrs3.Single().AttributeClass);

                var localFn4 = cClass.GetMethod("<M>g__local4|0_3");
                var attrs4 = localFn4.TypeParameters.Single().GetAttributes();
                Assert.Equal(aAttribute, attrs4.Single().AttributeClass);
            }
        }

        internal CompilationVerifier VerifyOutput(string source, string output, CSharpCompilationOptions options, Verification verify = Verification.Passes)
        {
            var comp = CreateCompilationWithMscorlib45AndCSharp(source, options: options);
            return CompileAndVerify(comp, expectedOutput: output, verify: verify).VerifyDiagnostics(); // no diagnostics
        }

        internal CompilationVerifier VerifyOutput(string source, string output)
        {
            var comp = CreateCompilationWithMscorlib45AndCSharp(source, options: TestOptions.ReleaseExe);
            return CompileAndVerify(comp, expectedOutput: output).VerifyDiagnostics(); // no diagnostics
        }

        internal CompilationVerifier VerifyOutputInMain(string methodBody, string output, params string[] usings)
        {
            for (var i = 0; i < usings.Length; i++)
            {
                usings[i] = "using " + usings[i] + ";";
            }
            var usingBlock = string.Join(Environment.NewLine, usings);
            var source = usingBlock + @"
class Program
{
    static void Main()
    {
" + methodBody + @"
    }
}";
            return VerifyOutput(source, output);
        }
    }
}
