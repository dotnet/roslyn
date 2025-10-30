// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Emit
{
    public class EntryPointTests : EmitMetadataTestBase
    {
        private CSharpCompilation CompileConsoleApp(string source, CSharpParseOptions parseOptions = null, string mainTypeName = null)
        {
            return CreateCompilation(source, options: TestOptions.ReleaseExe.WithWarningLevel(5).WithMainTypeName(mainTypeName), parseOptions: parseOptions);
        }

        [Fact]
        public void MainOverloads()
        {
            string source = @"
public class C
{
  public static void Main(int goo) { System.Console.WriteLine(1); }
  public static void Main() { System.Console.WriteLine(2); }
}
";
            var compilation = CompileConsoleApp(source);
            var verifier = CompileAndVerify(compilation, expectedOutput: "2");

            verifier.VerifyDiagnostics(
                // (4,22): warning CS0028: 'C.Main(int)' has the wrong signature to be an entry point
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("C.Main(int)"));
        }

        [Fact]
        public void MainOverloads_Dll()
        {
            string source = @"
public class C
{
  public static void Main(int goo) { System.Console.WriteLine(1); }
  public static void Main() { System.Console.WriteLine(2); }
}
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);

            var verifier = CompileAndVerify(compilation);

            // Dev10 reports warning CS0028: 'C.Main(int)' has the wrong signature to be an entry point, 
            // but that seems like a bug since we are not compiling an exe.
            verifier.VerifyDiagnostics();
        }

        /// <summary>
        /// Dev10 reports the .exe full path in CS5001. We don't.
        /// </summary>
        [Fact]
        public void ERR_NoEntryPoint_Overloads()
        {
            string source = @"
public class C
{
  public static void Main(int goo) { System.Console.WriteLine(1); }
  public static void Main(double goo) { System.Console.WriteLine(2); }
  public static void Main(string[,] goo) { System.Console.WriteLine(3); }
}
";
            var compilation = CompileConsoleApp(source);

            compilation.VerifyDiagnostics(
                // (4,22): warning CS0028: 'C.Main(int)' has the wrong signature to be an entry point
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("C.Main(int)"),
                // (5,22): warning CS0028: 'C.Main(double)' has the wrong signature to be an entry point
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("C.Main(double)"),
                // (6,22): warning CS0028: 'C.Main(string[*,*])' has the wrong signature to be an entry point
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("C.Main(string[*,*])"),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint));
        }

        /// <summary>
        /// Dev10 reports the .exe full path in CS0017. We don't.
        /// </summary>
        [Fact]
        public void ERR_MultipleEntryPoints()
        {
            string source = @"
public class C
{
  public static void Main() { System.Console.WriteLine(1); }
  public static void Main(string[] a) { System.Console.WriteLine(2); }
}

public class D
{
  public static string Main() { System.Console.WriteLine(3); return null; }
}
";
            var compilation = CompileConsoleApp(source);

            compilation.VerifyDiagnostics(
                // (10,24): warning CS0028: 'D.Main()' has the wrong signature to be an entry point
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("D.Main()"),
                // (4,22): error CS0017: Program has more than one entry point defined. Compile with /main to specify the type that contains the entry point.
                Diagnostic(ErrorCode.ERR_MultipleEntryPoints, "Main"));
        }

        [Fact]
        [WorkItem(46831, "https://github.com/dotnet/roslyn/issues/46831")]
        public void WRN_SyncAndAsyncEntryPoints_CSharp71()
        {
            string source = @"
using System.Threading.Tasks;

public class C
{
  public static async Task Main() { await Task.Delay(1); }
  public static void Main(string[] a) { System.Console.WriteLine(2); }
}
";
            var compilation = CompileConsoleApp(source, parseOptions: TestOptions.Regular7_1);

            compilation.VerifyDiagnostics(
                // (6,28): warning CS8892: Method 'C.Main()' will not be used as an entry point because a synchronous entry point 'C.Main(string[])' was found.
                Diagnostic(ErrorCode.WRN_SyncAndAsyncEntryPoints, "Main").WithArguments("C.Main()", "C.Main(string[])").WithLocation(6, 28));
        }

        [Fact]
        public void SyncAndAsyncEntryPointsBeforeAsyncMainFeature_NoDiagnostics()
        {
            string source = @"
using System.Threading.Tasks;

public class C
{
  public static async Task Main() { await Task.Delay(1); }
  public static void Main(string[] a) { System.Console.WriteLine(2); }
}
";
            var compilation = CompileConsoleApp(source, parseOptions: TestOptions.Regular7);

            compilation.VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(46831, "https://github.com/dotnet/roslyn/issues/46831")]
        public void WRN_SyncAndAsyncEntryPointsCSharpLatest_SyncAndAsync()
        {
            string source = @"
using System.Threading.Tasks;

public class C
{
  public static async Task Main() { await Task.Delay(1); }
  public static void Main(string[] a) { System.Console.WriteLine(2); }
}
";
            var compilation = CompileConsoleApp(source);

            compilation.VerifyDiagnostics(
                // (6,28): warning CS8892: Method 'C.Main()' will not be used as an entry point because a synchronous entry point 'C.Main(string[])' was found.
                Diagnostic(ErrorCode.WRN_SyncAndAsyncEntryPoints, "Main").WithArguments("C.Main()", "C.Main(string[])").WithLocation(6, 28));
        }

        [Fact]
        [WorkItem(46831, "https://github.com/dotnet/roslyn/issues/46831")]
        public void ERR_And_WRN_MultipleEntryPointsCSharpLatest_TwoSyncAndOneAsync()
        {
            string source = @"
using System.Threading.Tasks;

public class C
{
  public static async Task Main() { await Task.Delay(1); }
  public static void Main(string[] a) { System.Console.WriteLine(2); }
}

public class D
{
  public static void Main() { System.Console.WriteLine(3); }
}
";
            var compilation = CompileConsoleApp(source);

            compilation.VerifyDiagnostics(
                // (7,22): error CS0017: Program has more than one entry point defined. Compile with /main to specify the type that contains the entry point.
                Diagnostic(ErrorCode.ERR_MultipleEntryPoints, "Main"),
                // (6,28): warning CS8892: Method 'C.Main()' will not be used as an entry point because a synchronous entry point 'C.Main(string[])' was found.
                Diagnostic(ErrorCode.WRN_SyncAndAsyncEntryPoints, "Main").WithArguments("C.Main()", "C.Main(string[])").WithLocation(6, 28));
        }

        [Fact]
        [WorkItem(46831, "https://github.com/dotnet/roslyn/issues/46831")]
        public void WRN_SyncAndAsyncEntryPointsCSharpLatest_TwoAsyncAndOneSync()
        {
            string source = @"
using System.Threading.Tasks;

public class C
{
  public static async Task Main() { await Task.Delay(1); }
  public static void Main(string[] a) { System.Console.WriteLine(2); }
}

public class D
{
  public static async Task Main() { await Task.Delay(1); }
}
";
            var compilation = CompileConsoleApp(source);

            compilation.VerifyDiagnostics(
                // (6,28): warning CS8892: Method 'C.Main()' will not be used as an entry point because a synchronous entry point 'C.Main(string[])' was found.
                Diagnostic(ErrorCode.WRN_SyncAndAsyncEntryPoints, "Main").WithArguments("C.Main()", "C.Main(string[])").WithLocation(6, 28),
                // (12,28): warning CS8892: Method 'D.Main()' will not be used as an entry point because a synchronous entry point 'C.Main(string[])' was found.
                Diagnostic(ErrorCode.WRN_SyncAndAsyncEntryPoints, "Main").WithArguments("D.Main()", "C.Main(string[])").WithLocation(12, 28));
        }

        [Fact]
        public void MultipleEntryPointsWithTypeDefined_NoDiagnostic()
        {
            string source = @"
using System.Threading.Tasks;

public class C
{
  public static void Main(string[] a) { System.Console.WriteLine(2); }
}

public class D
{
  public static async Task Main() { await Task.Delay(1); }
}
";
            var compilation = CompileConsoleApp(source, mainTypeName: "D");
            compilation.VerifyDiagnostics();

            var compilation2 = CompileConsoleApp(source, mainTypeName: "C");
            compilation2.VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(46831, "https://github.com/dotnet/roslyn/issues/46831")]
        public void WRN_SyncAndAsyncEntryPoints_WithTypeDefined()
        {
            string source = @"
using System.Threading.Tasks;

public class C
{
  public static void Main(string[] a) { System.Console.WriteLine(2); }

  public static async Task Main() { await Task.Delay(1); }
}
";
            var compilation = CompileConsoleApp(source, mainTypeName: "C");
            compilation.VerifyDiagnostics(
                // (8,28): warning CS8892: Method 'C.Main()' will not be used as an entry point because a synchronous entry point 'C.Main(string[])' was found.
                Diagnostic(ErrorCode.WRN_SyncAndAsyncEntryPoints, "Main").WithArguments("C.Main()", "C.Main(string[])").WithLocation(8, 28));
        }

        [Fact]
        public void ERR_MultipleEntryPoints_Script()
        {
            string csx = @"
public static void Main() { System.Console.WriteLine(1); }
";

            string cs = @"
public class C 
{
   public static void Main() { System.Console.WriteLine(2); }
}
";

            var compilation = CreateCompilationWithMscorlib461(
                new[]
                {
                    Parse(csx, options: TestOptions.Script),
                    Parse(cs, options: TestOptions.Regular)
                },
                options: TestOptions.ReleaseExe);

            compilation.VerifyDiagnostics(
                // (2,20): warning CS7022: The entry point of the program is global script code; ignoring 'Main()' entry point.
                Diagnostic(ErrorCode.WRN_MainIgnored, "Main").WithArguments("Main()"),
                // (4,23): warning CS7022: The entry point of the program is global script code; ignoring 'C.Main()' entry point.
                Diagnostic(ErrorCode.WRN_MainIgnored, "Main").WithArguments("C.Main()"));
        }

        [WorkItem(528677, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528677")]
        [Fact]
        public void ERR_OneEntryPointAndOverload()
        {
            string source = @"public class MyClass
{
    static int Main()
    {
        return 0;
    }
    static int Main(string[] args, int i)
    {
        return i;
    }
}
";
            var compilation = CompileConsoleApp(source);

            compilation.VerifyDiagnostics(
                // (7,16): warning CS0028: 'MyClass.Main(string[], int)' has the wrong signature to be an entry point
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("MyClass.Main(string[], int)")
                );
        }

        /// <summary>
        /// Unlike Dev10 we report a single error for Main method with incorrect signature defined in a partial class.
        /// </summary>
        [Fact]
        public void ERR_NoEntryPoint_PartialClass()
        {
            string source = @"
public partial class A
{
  static partial void Main(double d);
}

public partial class A
{
  static partial void Main(double d) { }
}
";
            var compilation = CompileConsoleApp(source);

            compilation.VerifyDiagnostics(
                // (9,23): warning CS0028: 'A.Main(double)' has the wrong signature to be an entry point
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("A.Main(double)"),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint));
        }

        [Fact]
        public void ERR_NoEntryPoint_PartialClass_1()
        {
            string source = @"
public partial class A
{
  static partial void Main(double d);
}

public partial class A
{
}
";
            CompileConsoleApp(source).VerifyDiagnostics(
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint));
        }

        [Fact]
        public void ScriptNonMethodMain()
        {
            string csx = @"
public static int Main = 1;
System.Console.WriteLine(Main);
";

            var compilation = CreateCompilationWithMscorlib461(
                new[]
                {
                    SyntaxFactory.ParseSyntaxTree(csx, options: TestOptions.Script),
                },
                options: TestOptions.ReleaseExe);

            compilation.VerifyDiagnostics();

            CompileAndVerify(compilation, expectedOutput: "1");
        }

        [Fact]
        public void ScriptInstanceMethodMain()
        {
            string csx = @"
int Main() { return 1; }
int Main(string[] x) { return 2; }
System.Console.WriteLine(Main());
";

            var compilation = CreateCompilationWithMscorlib461(
                new[]
                {
                    SyntaxFactory.ParseSyntaxTree(csx, options: TestOptions.Script),
                },
                options: TestOptions.ReleaseExe);

            compilation.VerifyDiagnostics();

            CompileAndVerify(compilation, expectedOutput: "1");
        }

        [Fact]
        public void Namespace()
        {
            string source = @"
namespace N { namespace M { } }
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe.WithMainTypeName("N.M"));
            compilation.VerifyDiagnostics(
                // (2,25): error CS1556: 'N.M' specified for Main method must be a non-generic class, record, struct, or interface
                Diagnostic(ErrorCode.ERR_MainClassNotClass, "M").WithArguments("N.M"));
        }

        [Fact]
        public void NestedGenericMainType()
        {
            string source = @"
class C<T> 
{ 
    struct D 
    {
        public static void Main() { }   
    }
}
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe.WithMainTypeName("C.D"));
            compilation.VerifyDiagnostics(
                // (4,12): error CS1556: 'C<T>.D' specified for Main method must be a non-generic class, record, struct, or interface
                Diagnostic(ErrorCode.ERR_MainClassNotClass, "D").WithArguments("C<T>.D"));
        }

        [Fact]
        public void Struct()
        {
            string source = @"
struct C
{ 
    struct D 
    {
        public static void Main() { }   
    }
}
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe.WithMainTypeName("C.D"));
            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void GenericMainMethods()
        {
            string cs = @"
using System;

public static class C 
{
    static void Main<T>() { Console.WriteLine(1); }

    public static class CC<T>
    {
        static void Main() { Console.WriteLine(2); }
    }
}

public static class D<T>
{
    static void Main() { Console.WriteLine(3); }

    public static class DD
    {
        static void Main() { Console.WriteLine(4); }
    }
}

public static class E
{
    static void Main() { Console.WriteLine(5); }
}
";
            var compilation = CreateCompilation(cs, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics(
                // (4,16): warning CS0402: 'C.Main<T>()': an entry point cannot be generic or in a generic type
                Diagnostic(ErrorCode.WRN_MainCantBeGeneric, "Main").WithArguments("C.Main<T>()"),
                // (8,20): warning CS0402: 'C.CC<T>.Main()': an entry point cannot be generic or in a generic type
                Diagnostic(ErrorCode.WRN_MainCantBeGeneric, "Main").WithArguments("C.CC<T>.Main()"),
                // (14,16): warning CS0402: 'D<T>.Main()': an entry point cannot be generic or in a generic type
                Diagnostic(ErrorCode.WRN_MainCantBeGeneric, "Main").WithArguments("D<T>.Main()"),
                // (18,20): warning CS0402: 'D<T>.DD.Main()': an entry point cannot be generic or in a generic type
                Diagnostic(ErrorCode.WRN_MainCantBeGeneric, "Main").WithArguments("D<T>.DD.Main()"));

            CompileAndVerify(compilation, expectedOutput: "5");

            compilation = CreateCompilation(cs, options: TestOptions.ReleaseExe.WithMainTypeName("C"));
            compilation.VerifyDiagnostics(
                // (6,17): warning CS0402: 'C.Main<T>()': an entry point cannot be generic or in a generic type
                Diagnostic(ErrorCode.WRN_MainCantBeGeneric, "Main").WithArguments("C.Main<T>()"),
                // (4,21): error CS1558: 'C' does not have a suitable static Main method
                Diagnostic(ErrorCode.ERR_NoMainInClass, "C").WithArguments("C"));

            // Dev10 reports: CS1555: Could not find 'D.DD' specified for Main method
            compilation = CreateCompilation(cs, options: TestOptions.ReleaseExe.WithMainTypeName("D.DD"));
            compilation.VerifyDiagnostics(
                // (18,25): error CS1556: 'D<T>.DD' specified for Main method must be a non-generic class, record, struct, or interface
                Diagnostic(ErrorCode.ERR_MainClassNotClass, "DD").WithArguments("D<T>.DD"));
        }

        [Fact]
        public void MultipleArities1()
        {
            string source = @"
public class A
{
  public class B
  {
    class C 
    {
     	public static void Main() { System.Console.WriteLine(1); }
    }
  }
  
  public class B<T>
  {
    class C 
    {
	   public static void Main() { System.Console.WriteLine(2); }
	}
  }
}";
            CompileAndVerify(source, options: TestOptions.ReleaseExe.WithMainTypeName("A.B.C"), expectedOutput: "1");
        }

        [Fact]
        public void MultipleArities2()
        {
            string source = @"
public class A
{
  public class B<T>
  {
    class C 
    {
	   public static void Main() { System.Console.WriteLine(2); }
	}
  }
  public class B
  {
    class C 
    {
     	public static void Main() { System.Console.WriteLine(1); }
    }
  }
}";
            CompileAndVerify(source, options: TestOptions.ReleaseExe.WithMainTypeName("A.B.C"), expectedOutput: "1");
        }

        [Fact]
        public void MultipleArities3()
        {
            string source = @"
public class A
{
  public class B<S,T>
  {
    class C 
    {
     	public static void Main() { System.Console.WriteLine(1); }
    }
  }
  
  public class B<T>
  {
    class C 
    {
	   public static void Main() { System.Console.WriteLine(2); }
	}
  }
}";
            // Dev10 reports CS1555: Could not find 'A.B.C' specified for Main method
            CreateCompilation(source, options: TestOptions.ReleaseExe.WithMainTypeName("A.B.C")).VerifyDiagnostics(
                // (14,11): error CS1556: 'A.B<T>.C' specified for Main method must be a non-generic class, record, struct, or interface
                Diagnostic(ErrorCode.ERR_MainClassNotClass, "C").WithArguments("A.B<T>.C"));
        }

        /// <summary>
        /// The nongeneric is used.
        /// </summary>
        [Fact]
        public void ExplicitMainTypeName_GenericAndNonGeneric()
        {
            string source = @"
class C<T> 
{
    static void Main() { }
}

class C 
{
    static void Main() { }
}
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe.WithMainTypeName("C"));
            compilation.VerifyDiagnostics();
        }

        /// <summary>
        /// Dev10: the first definition of C is reported (i.e. C{S,T}).
        /// We report the one with the least arity (i.e. C{T}).
        /// </summary>
        [Fact]
        public void ExplicitMainTypeName_GenericMultipleArities()
        {
            string source = @"
class C<S, T>
{
    static void Main() { }
}

class C<T> 
{
    static void Main() { }
}
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe.WithMainTypeName("C"));
            compilation.VerifyDiagnostics(
                // (7,7): error CS1556: 'C<T>' specified for Main method must be a non-generic class, record, struct, or interface
                Diagnostic(ErrorCode.ERR_MainClassNotClass, "C").WithArguments("C<T>"));
        }

        /// <summary>
        /// Warnings reported for all nonviable, "missing main" error reported.
        /// Dev10: reports a warning for instance Main(bool) but that's seems to be a bug 
        /// since the warning is not reported when main type name is not specified.
        /// </summary>
        [Fact]
        public void ExplicitMainTypeHasMultipleMains_NoViable()
        {
            string source = @"
public class C 
{
    int Main(bool b) { return 1; }
    static int Main(string a) { return 1; }
    static int Main(int a) { return 1; }  
    static int Main<T>() { return 1; }      
}
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe.WithMainTypeName("C"));
            compilation.VerifyDiagnostics(
                // (5,16): warning CS0028: 'C.Main(string)' has the wrong signature to be an entry point
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("C.Main(string)"),
                // (6,16): warning CS0028: 'C.Main(int)' has the wrong signature to be an entry point
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("C.Main(int)"),
                // (7,16): warning CS0402: 'C.Main<T>()': an entry point cannot be generic or in a generic type
                Diagnostic(ErrorCode.WRN_MainCantBeGeneric, "Main").WithArguments("C.Main<T>()"),
                // (2,14): error CS1558: 'C' does not have a suitable static Main method
                Diagnostic(ErrorCode.ERR_NoMainInClass, "C").WithArguments("C"));
        }

        /// <summary>
        /// No errors or warnings reported.
        /// </summary>
        [Fact]
        public void ExplicitMainTypeHasMultipleMains_SingleViable()
        {
            string source = @"
public class C 
{
    int Main(bool b) { return 1; }
    static int Main() { return 1; }
    static int Main(string a) { return 1; }
    static int Main(int a) { return 1; }  
    static int Main<T>() { return 1; }      
}
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe.WithMainTypeName("C"));
            compilation.VerifyDiagnostics();
        }

        /// <summary>
        /// Warnings reported for nonviable and "multiple mains" error reported for viable.
        /// </summary>
        [Fact]
        public void ExplicitMainTypeHasMultipleMains_MultipleViable()
        {
            string source = @"
public class C 
{
    int Main(bool b) { return 1; }
    static int Main() { return 1; }
    static int Main(string a) { return 1; }
    static int Main(int a) { return 1; }
    static int Main(string[] a) { return 1; }
    static int Main<T>() { return 1; }
}
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe.WithMainTypeName("C"));
            compilation.VerifyDiagnostics(
                // (5,16): error CS0017: Program has more than one entry point defined. Compile with /main to specify the type that contains the entry point.
                Diagnostic(ErrorCode.ERR_MultipleEntryPoints, "Main"));
        }

        [Fact]
        public void ERR_NoEntryPoint_NonMethod()
        {
            string source = @"
public class G 
{
   public static int Main = 1;
}
";
            var compilation = CompileConsoleApp(source);

            compilation.VerifyDiagnostics(
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint));
        }

        [Fact]
        public void Script()
        {
            string source = @"
System.Console.WriteLine(1);
";

            var compilation = CreateCompilationWithMscorlib461(
                new[] { SyntaxFactory.ParseSyntaxTree(source, options: TestOptions.Script) },
                options: TestOptions.ReleaseExe);

            CompileAndVerify(compilation, expectedOutput: "1");
        }

        [Fact]
        public void ScriptAndRegularFile_ExplicitMain()
        {
            string csx = @"
System.Console.WriteLine(1);
";

            string cs = @"
public class C 
{
   public static void Main() { System.Console.WriteLine(2); }
}
";

            var compilation = CreateCompilationWithMscorlib461(
                new[]
                {
                    SyntaxFactory.ParseSyntaxTree(csx, options: TestOptions.Script),
                    SyntaxFactory.ParseSyntaxTree(cs, options: TestOptions.Regular)
                },
                options: TestOptions.ReleaseExe);

            compilation.VerifyDiagnostics(
                // (4,23): warning CS7022: The entry point of the program is global script code; ignoring 'C.Main()' entry point.
                Diagnostic(ErrorCode.WRN_MainIgnored, "Main").WithArguments("C.Main()"));

            CompileAndVerify(compilation, expectedOutput: "1");
        }

        [Fact]
        public void ScriptAndRegularFile_ExplicitMains()
        {
            string csx = @"
System.Console.WriteLine(1);
";

            string cs = @"
public class C 
{
   public static void Main() { System.Console.WriteLine(2); }
}

public class D 
{
   public static void Main() { System.Console.WriteLine(3); }
}
";

            var compilation = CreateCompilationWithMscorlib461(
                new[]
                {
                    SyntaxFactory.ParseSyntaxTree(csx, options: TestOptions.Script),
                    SyntaxFactory.ParseSyntaxTree(cs, options: TestOptions.Regular)
                },
                options: TestOptions.ReleaseExe);

            compilation.VerifyDiagnostics(
                // (4,23): warning CS7022: The entry point of the program is global script code; ignoring 'C.Main()' entry point.
                Diagnostic(ErrorCode.WRN_MainIgnored, "Main").WithArguments("C.Main()"),
                // (9,23): warning CS7022: The entry point of the program is global script code; ignoring 'C.Main()' entry point.
                Diagnostic(ErrorCode.WRN_MainIgnored, "Main").WithArguments("D.Main()"));

            CompileAndVerify(compilation, expectedOutput: "1");
        }

        [Fact]
        public void ExplicitMain()
        {
            string source = @"
class C 
{
   static void Main() { System.Console.WriteLine(1); }
}

class D
{
   static void Main() { System.Console.WriteLine(2); }
}
";

            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe.WithMainTypeName("C"));
            CompileAndVerify(compilation, expectedOutput: "1");

            compilation = CreateCompilation(source, options: TestOptions.ReleaseExe.WithMainTypeName("D"));
            CompileAndVerify(compilation, expectedOutput: "2");
        }

        [Fact]
        public void ERR_MainClassNotFound()
        {
            string source = @"
class C 
{
   static void Main() { System.Console.WriteLine(1); }
}
";

            var compilation = CreateCompilation(
                source,
                options: TestOptions.ReleaseExe.WithMainTypeName("D"));

            compilation.VerifyDiagnostics(
                // error CS1555: Could not find 'D' specified for Main method
                Diagnostic(ErrorCode.ERR_MainClassNotFound).WithArguments("D"));
        }

        [Fact]
        public void ERR_MainClassNotClass()
        {
            string source = @"
enum C { }
delegate void D();
interface I { }
";

            var compilation = CreateCompilation(
                source,
                options: TestOptions.ReleaseExe.WithMainTypeName("C"));

            compilation.VerifyDiagnostics(
                // (2,6): error CS1556: 'C' specified for Main method must be a valid class or struct
                Diagnostic(ErrorCode.ERR_MainClassNotClass, "C").WithArguments("C"));

            compilation = CreateCompilation(
                source,
                options: TestOptions.ReleaseExe.WithMainTypeName("D"));

            compilation.VerifyDiagnostics(
                // (3,15): error CS1556: 'D' specified for Main method must be a valid class or struct
                Diagnostic(ErrorCode.ERR_MainClassNotClass, "D").WithArguments("D"));

            compilation = CreateCompilation(
                source,
                options: TestOptions.ReleaseExe.WithMainTypeName("I"));

            compilation.VerifyDiagnostics(
                // (4,11): error CS1558: 'I' does not have a suitable static Main method
                // interface I { }
                Diagnostic(ErrorCode.ERR_NoMainInClass, "I").WithArguments("I").WithLocation(4, 11)
                );
        }

        [Fact]
        public void ERR_NoMainInClass()
        {
            string source = @"
class C 
{
   void Main() { System.Console.WriteLine(1); }
}

class D
{
   static void Main(double args) { System.Console.WriteLine(1); }
}

class E
{
   int Main { get { System.Console.WriteLine(1); return 1; } }
}
";

            // Dev10: reports a warning for instance Main, we don't since the signature is correct:
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe.WithMainTypeName("C"));
            compilation.VerifyDiagnostics(
                // (2,7): error CS1558: 'C' does not have a suitable static Main method
                Diagnostic(ErrorCode.ERR_NoMainInClass, "C").WithArguments("C"));

            compilation = CreateCompilation(source, options: TestOptions.ReleaseExe.WithMainTypeName("D"));
            compilation.VerifyDiagnostics(
                // (9,16): warning CS0028: 'D.Main(double)' has the wrong signature to be an entry point
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("D.Main(double)"),
                // (7,7): error CS1558: 'D' does not have a suitable static Main method
                Diagnostic(ErrorCode.ERR_NoMainInClass, "D").WithArguments("D"));

            compilation = CreateCompilation(source, options: TestOptions.ReleaseExe.WithMainTypeName("E"));
            compilation.VerifyDiagnostics(
                // (2,7): error CS1558: 'E' does not have a suitable static Main method
                Diagnostic(ErrorCode.ERR_NoMainInClass, "E").WithArguments("E"));
        }

        [Fact]
        public void ERR_NoMainInClass_Script()
        {
            string csx = @"
System.Console.WriteLine(2);
";

            string cs = @"
class C 
{
   void Main() { System.Console.WriteLine(1); }
}
";

            var compilation = CreateCompilationWithMscorlib461(
                new[]
                {
                    SyntaxFactory.ParseSyntaxTree(csx, options: TestOptions.Script),
                    SyntaxFactory.ParseSyntaxTree(cs, options: TestOptions.Regular),
                },
                options: TestOptions.ReleaseExe.WithMainTypeName("C"));

            compilation.VerifyDiagnostics(
                // (2,7): warning CS7022: The entry point of the program is global script code; ignoring 'C' entry point.
                Diagnostic(ErrorCode.WRN_MainIgnored).WithArguments("C"));
        }

        [Fact]
        public void WRN_InvalidMainSig_MultiDimensionalArray()
        {
            string source = @"
class B
{
    public static void Main(string[,] args)
    {
    }
} 
";
            CompileConsoleApp(source).VerifyDiagnostics(// (4,24): warning CS0028: 'B.Main(string[*,*])' has the wrong signature to be an entry point
                                                        //     public static void Main(string[,] args)
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("B.Main(string[*,*])"),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint));
        }

        [Fact]
        public void WRN_InvalidMainSig_JaggedArray()
        {
            string source = @"
class B
{
    public static void Main(string[][] args)
    {
    }
} 
";
            CompileConsoleApp(source).VerifyDiagnostics(// (4,24): warning CS0028: 'B.Main(string[][])' has the wrong signature to be an entry point
                                                        //     public static void Main(string[][] args)
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("B.Main(string[][])"),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint));
        }

        [Fact]
        public void WRN_InvalidMainSig_Array()
        {
            string source = @"
using System;
class B
{
    public static void Main(Array args)
    {
    }
} 
";
            CompileConsoleApp(source).VerifyDiagnostics(// (5,24): warning CS0028: 'B.Main(System.Array)' has the wrong signature to be an entry point
                                                        //     public static void Main(Array args)
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("B.Main(System.Array)"),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint));
        }

        [Fact]
        public void ERR_NoEntryPoint_MainIsProperty()
        {
            string source = @"
class Program
{
    int Main { get; set; }
}
";
            CompileConsoleApp(source).VerifyDiagnostics(
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint));
        }

        [Fact]
        public void WRN_InvalidMainSig_ReturnTypeOtherThanIntVoid()
        {
            string source = @"
class B
{
    public static int[] Main(string[] args)
    {
        return null;
    }
}
";
            CompileConsoleApp(source).VerifyDiagnostics(
                // (4,25): warning CS0028: 'B.Main(string[])' has the wrong signature to be an entry point
                //     public static int[] Main(string[] args)
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("B.Main(string[])"),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint));
        }

        [Fact]
        public void ParamParameterForMain()
        {
            string source = @"
class B
{
    public static void Main(params string[] x)
    {
    }
}
";
            CompileConsoleApp(source).VerifyDiagnostics();
        }

        [Fact]
        public void ParamParameterForMain_1()
        {
            string source = @"
class B
{
    public static void Main(int x=1,params string[] str)
    {
    }
}
";
            CompileConsoleApp(source).VerifyDiagnostics(// (4,24): warning CS0028: 'B.Main(int, params string[])' has the wrong signature to be an entry point
                                                        //     public static void Main(int x=1,params string[] str)
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("B.Main(int, params string[])"),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint));
        }

        [Fact]
        public void ParamParameterForMain_2()
        {
            string source = @"
class B
{
    public static void Main(string[] str,params string[] str1)
    {
    }
}
";
            CompileConsoleApp(source).VerifyDiagnostics(// (4,24): warning CS0028: 'B.Main(string[], params string[])' has the wrong signature to be an entry point
                                                        //     public static void Main(string[] str,params string[] str1)
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("B.Main(string[], params string[])"),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint));
        }

        [Fact]
        public void ParamParameterForMain_3()
        {
            string source = @"
class B
{
    public static void Main(params int[] x)
    {
    }
}
";
            CompileConsoleApp(source).VerifyDiagnostics(// (4,24): warning CS0028: 'B.Main(params int[])' has the wrong signature to be an entry point
                                                        //     public static void Main(params int[] x)
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("B.Main(params int[])"),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint));
        }

        [Fact]
        public void OptionalParameterForMain()
        {
            string source = @"
class B
{
    public static void Main(string[] arg = null)
    {
    }
}
";
            CompileConsoleApp(source).VerifyDiagnostics();
        }

        [Fact]
        public void OptionalParameterForMain_1()
        {
            string source = @"
class B
{
    public static void Main(int x = 1)
    {
    }
}
";
            CompileConsoleApp(source).VerifyDiagnostics(// (4,24): warning CS0028: 'B.Main(int)' has the wrong signature to be an entry point
                                                        //     public static void Main(int x = 1)
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("B.Main(int)"),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint));
        }

        [Fact]
        public void OptionalParameterForMain_2()
        {
            string source = @"
class B
{
    public static void Main(string[,] arg = null)
    {
    }
}
";
            CompileConsoleApp(source).VerifyDiagnostics(// (4,24): warning CS0028: 'B.Main(string[*,*])' has the wrong signature to be an entry point
                                                        //     public static void Main(string[,] arg = null)
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("B.Main(string[*,*])"),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint));
        }

        [Fact]
        public void MainAsExtensionMethod()
        {
            string source = @"
class B
{
}
static class Extension
{
    public static void Main(this B x, string[] args)
    { }
}
";
            CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(// (8,24): warning CS0028: 'Extension.Main(B, string[])' has the wrong signature to be an entry point
                                                                                                                    //     public static void Main(this B x, string[] args)
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("Extension.Main(B, string[])"),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint));
        }

        [Fact]
        public void MainAsExtensionMethod_1()
        {
            string source = @"
class B
{
}
static class Extension
{
    public static int Main(this B x)
    { return 1; }
}
";
            CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(// (8,23): warning CS0028: 'Extension.Main(B)' has the wrong signature to be an entry point
                                                                                                                    //     public static int Main(this B x)
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("Extension.Main(B)"),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint));
        }

        [Fact]
        public void MainAsExtensionMethod_2()
        {
            string source = @"
static class Extension
{
    public static int Main(this string str)
    { return 1; }
}
";
            CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(// (5,23): warning CS0028: 'Extension.Main(string)' has the wrong signature to be an entry point
                                                                                                                    //     public static int Main(this string str)
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("Extension.Main(string)"),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint));
        }

        [Fact]
        public void MainIsCaseSensitive()
        {
            string source = @"
class B
{
    static void main() { }
    static void main(string[] args) { }
    static void MAIN() { }
    static void MAIN(string[] args) { }
    static void mAin() { }
    static void mAin(string[] args) { }
}
class C
{
    static int main() { return 1; }
    static int main(string[] args) { return 1; }
    static int MAIN() { return 1; }
    static int MAIN(string[] args) { return 1; }
    static int maiN() { return 1; }
    static int maiN(string[] args) { return 1; }
}
";
            CompileConsoleApp(source).VerifyDiagnostics(
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint));
        }

        [WorkItem(543468, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543468")]
        [Fact()]
        public void RefParameterForMain()
        {
            string source = @"
class C
{
    static void Main(ref string[] args) { }
}
";
            CompileConsoleApp(source).VerifyDiagnostics(
                // (4,17): warning CS0028: 'C.Main(ref string[])' has the wrong signature to be an entry point
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("C.Main(ref string[])"),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint));
        }

        [WorkItem(544478, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544478")]
        [Fact()]
        public void ArglistParameterForMain()
        {
            string source = @"
class C
{
    static void Main(__arglist) { }
}

class D
{
    static void Main(string[] array, __arglist) { }
}
";
            CompileConsoleApp(source).VerifyDiagnostics(
                // (4,17): warning CS0028: 'C.Main(__arglist)' has the wrong signature to be an entry point
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("C.Main(__arglist)"),
                // (9,17): warning CS0028: 'D.Main(string[], __arglist)' has the wrong signature to be an entry point
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("D.Main(string[], __arglist)"),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint));
        }

        [WorkItem(543467, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543467")]
        [Fact()]
        public void OutParameterForMain()
        {
            string source = @"
class Program
{
    static void Main(out string[] args) {args = null; }
}
";
            CompileConsoleApp(source).VerifyDiagnostics(
                // (4,17): warning CS0028: 'Program.Main(out string[])' has the wrong signature to be an entry point
                //     static void Main(out string[] args) {args = null; }
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("Program.Main(out string[])"),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint));
        }

        [Fact()]
        public void MainInPrivateClass()
        {
            string source = @"
class C1
{
    private struct C2
    {
        static void Main()
        {
        }
    }
}
";
            CompileConsoleApp(source).VerifyDiagnostics();
        }

        [Fact()]
        public void PrivateMain()
        {
            string source = @"
class C
{
    private static void Main(string[] args)
    {
    }
}
";
            CompileConsoleApp(source).VerifyDiagnostics();
        }

        [Fact()]
        public void MultipleEntryPoint_Inherit()
        {
            string source = @"
class BaseClass
{
    public static void Main()
    { }
}
class Derived : BaseClass
{
    public static void Main()
    { }
}
";
            CompileConsoleApp(source).VerifyDiagnostics(
                // (9,24): warning CS0108: 'Derived.Main()' hides inherited member 'BaseClass.Main()'. Use the new keyword if hiding was intended.
                //     public static void Main()
                Diagnostic(ErrorCode.WRN_NewRequired, "Main").WithArguments("Derived.Main()", "BaseClass.Main()"),
                // (4,24): error CS0017: Program has more than one entry point defined. Compile with /main to specify the type that contains the entry point.
                //     public static void Main()
                Diagnostic(ErrorCode.ERR_MultipleEntryPoints, "Main"));
        }

        [Fact()]
        public void ERR_NoEntryPoint_MainMustStatic()
        {
            string source = @"
class C
{
    private void Main(string[] args)
    {
    }
    private int Main()
    {
        return 1;
    }
}
";
            CompileConsoleApp(source).VerifyDiagnostics(
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint));
        }

        [Fact()]
        public void ERR_NoEntryPoint_MainAsConstructor()
        {
            string source = @"
static class Main
{
    static Main() { }
}
";
            CompileConsoleApp(source).VerifyDiagnostics(
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint));
        }

        [Fact()]
        public void ExplicitMainType_MainIsNotStatic()
        {
            string source = @"
class D
{
    void Main() { }
}
";
            CreateCompilation(source, options: TestOptions.ReleaseExe.WithMainTypeName("D")).VerifyDiagnostics(
                // (2,7): error CS1558: 'D' does not have a suitable static Main method
                // class D
                Diagnostic(ErrorCode.ERR_NoMainInClass, "D").WithArguments("D"));
        }

        [WorkItem(753028, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/753028")]
        [Fact]
        public void RootMemberNamedScript()
        {
            string source;

            source = @"namespace Script { }";
            CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint));

            source = @"class Script { }";
            CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint));

            source = @"struct Script { }";
            CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint));

            source = @"interface Script<T> { }";
            CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint));

            source = @"enum Script { }";
            CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint));

            source = @"delegate void Script();";
            CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint));
        }

        [Fact()]
        public void ExplicitMainType_ClassNameIsEmpty()
        {
            string source = @"
class D
{
    static void Main() { }
}
";
            CreateCompilation(source, options: TestOptions.ReleaseExe.WithMainTypeName(string.Empty)).VerifyDiagnostics(
    // error CS7088: Invalid 'MainTypeName' value: ''.
    Diagnostic(ErrorCode.ERR_BadCompilationOptionValue).WithArguments("MainTypeName", "")
                );
        }

        [Fact()]
        public void ExplicitMainType_NoMainInClass()
        {
            string source = @"
class D
{
}
";
            CreateCompilation(source, options: TestOptions.ReleaseExe.WithMainTypeName("D")).VerifyDiagnostics(
                // (2,7): error CS1558: 'D' does not have a suitable static Main method
                // class D
                Diagnostic(ErrorCode.ERR_NoMainInClass, "D").WithArguments("D"));
        }

        [Fact()]
        public void ExplicitMainType_CaseSensitive()
        {
            string source = @"
class D
{
    static void Main(){}
}
";
            CreateCompilation(source, options: TestOptions.ReleaseExe.WithMainTypeName("d")).VerifyDiagnostics(
                // error CS1555: Could not find 'd' specified for Main method
                Diagnostic(ErrorCode.ERR_MainClassNotFound).WithArguments("d"));
        }

        [Fact()]
        public void ExplicitMainType_Extension()
        {
            string source = @"
class B
{
}
static class @extension
{
    public static void Main(this B x, string[] args)
    { }
}
";
            CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.ReleaseExe.WithMainTypeName("B")).VerifyDiagnostics(
                // (2,7): error CS1558: 'B' does not have a suitable static Main method
                // class B
                Diagnostic(ErrorCode.ERR_NoMainInClass, "B").WithArguments("B"));

            CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.ReleaseExe.WithMainTypeName("extension")).VerifyDiagnostics(
                // (7,24): warning CS0028: 'extension.Main(B, string[])' has the wrong signature to be an entry point
                //     public static void Main(this B x, string[] args)
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("extension.Main(B, string[])").WithLocation(7, 24),
                // (5,14): error CS1558: 'extension' does not have a suitable static 'Main' method
                // static class @extension
                Diagnostic(ErrorCode.ERR_NoMainInClass, "@extension").WithArguments("extension").WithLocation(5, 14));
        }

        [Fact()]
        public void ExplicitMainType_MainAsConstructor()
        {
            string source = @"
static class Main
{
    static Main() { }
}
";
            CreateCompilation(source, options: TestOptions.ReleaseExe.WithMainTypeName("Main")).VerifyDiagnostics(
                // (2,14): error CS1558: 'Main' does not have a suitable static Main method
                // static class Main
                Diagnostic(ErrorCode.ERR_NoMainInClass, "Main").WithArguments("Main"));
        }

        [WorkItem(543511, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543511")]
        [Fact()]
        public void ExplicitMainType_OneDefineTwoDeclareValidMainForPartial()
        {
            string source = @"
partial class Program
{
}
partial class Program
{
    static partial void Main();
    static partial void Main(string[] args);
    static partial void Main(string[] args)
    { }
}
";
            CreateCompilation(source, options: TestOptions.ReleaseExe.WithMainTypeName("Program")).VerifyDiagnostics();
        }

        [Fact, WorkItem(543512, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543512")]
        public void DynamicParameterForMain()
        {
            // TODO: This should produce:
            // TODO: warning CS0028: 'Mybase.Main(dynamic)' has the wrong signature to be an entry point
            string source = @"
class Mybase
{
static void Main(dynamic x=null) { }
}
class Myderive : Mybase
{
    static void Main() { }
}
";
            CreateCompilation(source,
                options: TestOptions.ReleaseExe).
                VerifyDiagnostics(
                    Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("Mybase.Main(dynamic)"));
        }

        [WorkItem(630763, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/630763")]
        [Fact()]
        public void Bug630763()
        {
            var source = @"
public class C
{
    public static int Main()
    {
        return 0;
    }
}";

            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();

            var netModule = CreateCompilation(source, options: TestOptions.ReleaseModule);

            compilation = CreateCompilation("",
                                                        new MetadataReference[] { netModule.EmitToImageReference() },
                                                        options: TestOptions.ReleaseExe,
                                                        assemblyName: "Bug630763");

            compilation.VerifyDiagnostics(
    // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
    Diagnostic(ErrorCode.ERR_NoEntryPoint)
                );
        }

        [Fact, WorkItem(12113, "https://github.com/dotnet/roslyn/issues/12113")]
        public void LazyEntryPoint()
        {
            string source = @"
class Program
{
    public static void Main() {}
    public static void Main(string[] args) {}
}
";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees[0]);
            Assert.Empty(model.GetDiagnostics());
            compilation.VerifyDiagnostics(
                // (4,24): error CS0017: Program has more than one entry point defined. Compile with /main to specify the type that contains the entry point.
                //     public static void Main() {}
                Diagnostic(ErrorCode.ERR_MultipleEntryPoints, "Main").WithLocation(4, 24)
                );
        }

        [WorkItem(17923, "https://github.com/dotnet/roslyn/issues/17923")]
        [Fact]
        [CompilerTrait(CompilerFeature.RefLocalsReturns)]
        public void RefIntReturnMainEmpty()
        {
            var source = @"
class Program
{
    public static ref int Main() { throw new System.Exception(); }
}";

            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (4,27): warning CS0028: 'Program.Main()' has the wrong signature to be an entry point
                //     public static ref int Main() {}
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("Program.Main()").WithLocation(4, 27),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1));
        }

        [WorkItem(17923, "https://github.com/dotnet/roslyn/issues/17923")]
        [Fact]
        [CompilerTrait(CompilerFeature.RefLocalsReturns)]
        public void RefIntReturnMainWithParams()
        {
            var source = @"
class Program
{
    public static ref int Main(string[] args) { throw new System.Exception(); }
}";

            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (4,27): warning CS0028: 'Program.Main(string[])' has the wrong signature to be an entry point
                //     public static ref int Main(string[] args) { throw new System.Exception(); }
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("Program.Main(string[])"),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint));
        }
    }
}
