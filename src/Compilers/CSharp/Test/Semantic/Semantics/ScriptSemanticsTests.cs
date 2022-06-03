// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    using static ScriptTestFixtures;

    public class ScriptSemanticsTests : CSharpTestBase
    {
        private static CompilationReference TaskFacadeAssembly(bool includeNamespaceAroundTaskExtension = true)
        {
            var taskAssembly = @"
namespace System.Runtime.CompilerServices {
    public interface IAsyncStateMachine {
        void MoveNext();
        void SetStateMachine(IAsyncStateMachine iasm);
    }
    public struct AsyncTaskMethodBuilder {
        public static AsyncTaskMethodBuilder Create() { return default(AsyncTaskMethodBuilder); }
        public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    }
    public struct AsyncTaskMethodBuilder<T> {
        public static AsyncTaskMethodBuilder<T> Create() { return default(AsyncTaskMethodBuilder<T>); }
        public void SetStateMachine(IAsyncStateMachine stateMachine) { }
        public System.Threading.Tasks.Task<T> Task => null;
        public void SetException(Exception e) { }
        public void SetResult(T result) { }
        public void AwaitOnCompleted<A, B>(ref A a, ref B b) { }
        public void AwaitUnsafeOnCompleted<A, B>(ref A a, ref B b) { }
        public void Start<A>(ref A a) { }
    }
}

namespace System.Runtime.CompilerServices {
    public class ExtensionAttribute {}
}

namespace System {
    public delegate void Action();
}
namespace System.Runtime.CompilerServices {
    public interface INotifyCompletion {
        void OnCompleted(System.Action action);
    }
}

namespace System.Threading.Tasks {
    public class Awaiter<T>: System.Runtime.CompilerServices.INotifyCompletion {
        public bool IsCompleted  => true;
        public void OnCompleted(System.Action action) {}
        public T GetResult() { throw new Exception(); }
    }
    public class Task<T> {}
}
";
            var extensionSource = @"
    public static class MyExtensions {
        public static System.Threading.Tasks.Awaiter<T> GetAwaiter<T>(this System.Threading.Tasks.Task<T> task) {
            return null;
        }
    }
";

            if (includeNamespaceAroundTaskExtension)
            {
                taskAssembly = taskAssembly + $"\nnamespace Hidden {{\n{extensionSource}\n }}";
            }
            else
            {
                taskAssembly = taskAssembly + "\n" + extensionSource;
            }


            var taskCompilation = CreateEmptyCompilation(taskAssembly, references: new[] { MscorlibRef_v20 });
            taskCompilation.VerifyDiagnostics();
            return taskCompilation.ToMetadataReference();
        }

        [WorkItem(19048, "https://github.com/dotnet/roslyn/pull/19623")]
        [Fact]
        public void NonStandardTaskImplementation_NoGlobalUsing_NoScriptUsing()
        {

            var script = CreateEmptyCompilation(
                source: @" System.Console.Write(""complete"");",
                parseOptions: TestOptions.Script,
                options: TestOptions.DebugExe,
                references: new MetadataReference[] { TaskFacadeAssembly(), MscorlibRef_v20 });

            script.VerifyEmitDiagnostics(
                // error CS1061: 'Task<object>' does not contain a definition for 'GetAwaiter' and no extension method 'GetAwaiter' accepting a first argument of type 'Task<object>' could be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "").WithArguments("System.Threading.Tasks.Task<object>", "GetAwaiter").WithLocation(1, 1));

        }

        [WorkItem(19048, "https://github.com/dotnet/roslyn/pull/19623")]
        [Fact]
        public void NonStandardTaskImplementation_NoGlobalUsing_NoScriptUsing_NoNamespace()
        {

            var script = CreateEmptyCompilation(
                source: @" System.Console.Write(""complete"");",
                parseOptions: TestOptions.Script,
                options: TestOptions.DebugExe,
                references: new MetadataReference[] { TaskFacadeAssembly(false), MscorlibRef_v20 });

            script.VerifyEmitDiagnostics();
            var compiled = CompileAndVerify(script);
            compiled.VerifyIL("<Main>", @"
{
  // Code size       22 (0x16)
  .maxstack  1
  IL_0000:  newobj     "".ctor()""
  IL_0005:  callvirt   ""System.Threading.Tasks.Task<object> <Initialize>()""
  IL_000a:  call       ""System.Threading.Tasks.Awaiter<object> MyExtensions.GetAwaiter<object>(System.Threading.Tasks.Task<object>)""
  IL_000f:  callvirt   ""object System.Threading.Tasks.Awaiter<object>.GetResult()""
  IL_0014:  pop
  IL_0015:  ret
}");
        }

        [WorkItem(19048, "https://github.com/dotnet/roslyn/pull/19623")]
        [Fact]
        public void NonStandardTaskImplementation_GlobalUsing_NoScriptUsing_VoidHidden()
        {
            var script = CreateEmptyCompilation(
                source: @"interface I {}",
                parseOptions: TestOptions.Script,
                options: TestOptions.DebugExe.WithUsings("Hidden"),
                references: new MetadataReference[] { TaskFacadeAssembly() });
            script.VerifyEmitDiagnostics(
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1),
                // (1,1): error CS0518: Predefined type 'System.Object' is not defined or imported
                // interface I {}
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "interface I {}").WithArguments("System.Object").WithLocation(1, 1),
                // error CS0518: Predefined type 'System.Void' is not defined or imported
                //
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "").WithArguments("System.Void").WithLocation(1, 1));
        }

        [WorkItem(19048, "https://github.com/dotnet/roslyn/pull/19623")]
        [Fact]
        public void NonStandardTaskImplementation_GlobalUsing_NoScriptUsing()
        {
            var script = CreateEmptyCompilation(
                source: @" System.Console.Write(""complete"");",
                parseOptions: TestOptions.Script,
                options: TestOptions.DebugExe.WithUsings("Hidden"),
                references: new MetadataReference[] { TaskFacadeAssembly(), MscorlibRef_v20 });

            script.VerifyEmitDiagnostics();

            var compiled = CompileAndVerify(script);
            compiled.VerifyIL("<Main>", @"
{
  // Code size       22 (0x16)
  .maxstack  1
  IL_0000:  newobj     "".ctor()""
  IL_0005:  callvirt   ""System.Threading.Tasks.Task<object> <Initialize>()""
  IL_000a:  call       ""System.Threading.Tasks.Awaiter<object> Hidden.MyExtensions.GetAwaiter<object>(System.Threading.Tasks.Task<object>)""
  IL_000f:  callvirt   ""object System.Threading.Tasks.Awaiter<object>.GetResult()""
  IL_0014:  pop
  IL_0015:  ret
}");
        }

        [WorkItem(19048, "https://github.com/dotnet/roslyn/pull/19623")]
        [Fact]
        public void NonStandardTaskImplementation_NoGlobalUsing_ScriptUsing()
        {
            var script = CreateEmptyCompilation(
                source: @"
using Hidden;
new System.Threading.Tasks.Task<int>().GetAwaiter();
System.Console.Write(""complete"");",
                parseOptions: TestOptions.Script,
                options: TestOptions.DebugExe,
                references: new MetadataReference[] { TaskFacadeAssembly(), MscorlibRef_v20 });

            script.VerifyEmitDiagnostics(
                // error CS1061: 'Task<object>' does not contain a definition for 'GetAwaiter' and no extension method 'GetAwaiter' accepting a first argument of type 'Task<object>' could be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "").WithArguments("System.Threading.Tasks.Task<object>", "GetAwaiter").WithLocation(1, 1));

        }

        [WorkItem(19048, "https://github.com/dotnet/roslyn/pull/19623")]
        [Fact]
        public void NonStandardTaskImplementation_GlobalUsing_ScriptUsing()
        {
            var script = CreateEmptyCompilation(
                source: @"
using Hidden;
new System.Threading.Tasks.Task<int>().GetAwaiter();
System.Console.Write(""complete"");",
                parseOptions: TestOptions.Script,
                options: TestOptions.DebugExe.WithUsings("Hidden"),
                references: new MetadataReference[] { TaskFacadeAssembly(), MscorlibRef_v20 });

            var compiled = CompileAndVerify(script);
            compiled.VerifyIL("<Main>", @"
{
  // Code size       22 (0x16)
  .maxstack  1
  IL_0000:  newobj     "".ctor()""
  IL_0005:  callvirt   ""System.Threading.Tasks.Task<object> <Initialize>()""
  IL_000a:  call       ""System.Threading.Tasks.Awaiter<object> Hidden.MyExtensions.GetAwaiter<object>(System.Threading.Tasks.Task<object>)""
  IL_000f:  callvirt   ""object System.Threading.Tasks.Awaiter<object>.GetResult()""
  IL_0014:  pop
  IL_0015:  ret
}");

        }

        [WorkItem(543890, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543890")]
        [Fact]
        public void ThisIndexerAccessInScript()
        {
            string test = @"
this[1]
";
            var compilation = CreateCompilationWithMscorlib45(test, parseOptions: TestOptions.Script);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            compilation.VerifyDiagnostics(
                // (2,1): error CS0027: Keyword 'this' is not available in the current context
                // this[1]
                Diagnostic(ErrorCode.ERR_ThisInBadContext, "this"));

            var syntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ExpressionSyntax>().First();
            Assert.Equal(SyntaxKind.ElementAccessExpression, syntax.Kind());

            var summary = model.GetSemanticInfoSummary(syntax);
            Assert.Null(summary.Symbol);
            Assert.Equal(0, summary.CandidateSymbols.Length);
            Assert.Equal(CandidateReason.None, summary.CandidateReason);
            Assert.Equal(TypeKind.Error, summary.Type.TypeKind);
            Assert.Equal(TypeKind.Error, summary.ConvertedType.TypeKind);
            Assert.Equal(Conversion.Identity, summary.ImplicitConversion);
            Assert.Equal(0, summary.MethodGroup.Length);
        }

        [WorkItem(540875, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540875")]
        [Fact]
        public void MainInScript2()
        {
            var text = @"static void Main() { }";

            var tree = SyntaxFactory.ParseSyntaxTree(text, options: TestOptions.Script);

            var compilation = CreateCompilationWithMscorlib45(new[] { tree }, options: TestOptions.ReleaseExe.WithScriptClassName("Script"));

            compilation.VerifyDiagnostics(
                // (1,13): warning CS7022: The entry point of the program is global script code; ignoring 'Main()' entry point.
                Diagnostic(ErrorCode.WRN_MainIgnored, "Main").WithArguments("Main()"));
        }

        [Fact]
        public void Submission_TypeDisambiguationBasedUponAssemblyName()
        {
            var compilation = CreateCompilation("namespace System { public struct Int32 { } }");

            compilation.VerifyDiagnostics();
        }

        [WorkItem(540875, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540875")]
        [Fact]
        public void MainInScript1()
        {
            var text = @"static void Main() { }";

            var tree = SyntaxFactory.ParseSyntaxTree(text, options: TestOptions.Script);

            var compilation = CreateCompilationWithMscorlib45(new[] { tree }, options: TestOptions.ReleaseExe.WithScriptClassName("Script"));

            compilation.VerifyDiagnostics(
                // (1,13): warning CS7022: The entry point of the program is global script code; ignoring 'Main()' entry point.
                Diagnostic(ErrorCode.WRN_MainIgnored, "Main").WithArguments("Main()"));
        }

        [ConditionalFact(typeof(DesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/28001")]
        public void NoReferences()
        {
            var submission = CSharpCompilation.CreateScriptCompilation("test", syntaxTree: SyntaxFactory.ParseSyntaxTree("1", options: TestOptions.Script), returnType: typeof(int));
            submission.VerifyDiagnostics(
                // (1,1): error CS0518: Predefined type 'System.Object' is not defined or imported
                // 1
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "1").WithArguments("System.Object").WithLocation(1, 1),
                // error CS0518: Predefined type 'System.Object' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Object").WithLocation(1, 1),
                // error CS0518: Predefined type 'System.Threading.Tasks.Task`1' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Threading.Tasks.Task`1").WithLocation(1, 1),
                // error CS0400: The type or namespace name 'System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089' could not be found in the global namespace (are you missing an assembly reference?)
                Diagnostic(ErrorCode.ERR_GlobalSingleTypeNameNotFound).WithArguments("System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089").WithLocation(1, 1),
                // error CS0518: Predefined type 'System.Object' is not defined or imported
                //
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "").WithArguments("System.Object").WithLocation(1, 1),
                // error CS0518: Predefined type 'System.Object' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Object").WithLocation(1, 1),
                // (1,1): error CS0518: Predefined type 'System.Int32' is not defined or imported
                // 1
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "1").WithArguments("System.Int32").WithLocation(1, 1));
        }

        [Fact]
        public void Namespaces()
        {
            var c = CreateSubmission(@"
namespace N1
{
   class A { public int Goo() { return 2; }}
}
");
            c.VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_NamespaceNotAllowedInScript, "namespace"));
        }

        [Fact]
        public void NestedNamespace()
        {
            // NOTE: Namespace declarations are currently *cut* from interactive code. However, the code below
            // ensures that they continue to bind correctly in case the feature comes back.
            //
            // At the moment, namespaces in interactive code are a little funky. Essentially, nested namespaces
            // are not bound within the Script class. However, the Script class itself may be defined within a
            // namespace. In fact, the same namespace that the Script class is contained within can also
            // be declared within the Script class.
            //
            // Consider the following code parsed with ParseOptions(kind: SourceCodeKind.Script) and added
            // to a compilation with CompilationOptions(scriptClassName: "Goo.Script").
            //
            // public class A { }
            // namespace Goo
            // {
            //     public class B : Script.A { }
            // }
            //
            // The resulting compilation will contain the following types in the global namespace.
            //
            //   * Goo.Script
            //   * Goo.Script.A
            //   * Goo.B : Goo.Script.A

            string test = @"
public class A { }
namespace Goo
{
    public class B : Script.A { }
}
";
            var tree = SyntaxFactory.ParseSyntaxTree(test, options: TestOptions.Script);

            var compilation = CSharpCompilation.Create(
                assemblyName: "Test",
                options: TestOptions.ReleaseExe.WithScriptClassName("Goo.Script"),
                syntaxTrees: new[] { tree });

            var global = compilation.GlobalNamespace;

            var goo = global.GetMembers("Goo").Single() as NamespaceSymbol;
            Assert.Equal("Goo", goo.Name);

            var script = goo.GetTypeMembers("Script").Single();
            Assert.Equal("Goo.Script", script.ToTestDisplayString());

            var a = script.GetTypeMembers("A").Single();
            Assert.Equal("Goo.Script.A", a.ToTestDisplayString());

            var b = goo.GetTypeMembers("B").Single();
            Assert.Equal("Goo.B", b.ToTestDisplayString());
            Assert.Same(a, b.BaseType());
        }

        [Fact]
        public void NamespaceWithBothInteractiveAndNoninteractiveImplicitTypes()
        {
            string test = @"
namespace Goo { void F() { } }
void G() { }
G();
";
            var tree = SyntaxFactory.ParseSyntaxTree(test, options: TestOptions.Script);

            var compilation = CSharpCompilation.Create(
                assemblyName: "Test",
                options: TestOptions.ReleaseExe.WithScriptClassName("Goo.Script"),
                syntaxTrees: new[] { tree });

            var global = compilation.GlobalNamespace;
            var members = global.GetMembers().Where(m => !m.IsImplicitlyDeclared).AsImmutable();

            Assert.Equal(1, members.Length);
            Assert.Equal("Goo", members[0].Name);
            Assert.IsAssignableFrom<NamespaceSymbol>(members[0]);
            var ns = (NamespaceSymbol)members[0];
            members = ns.GetMembers();

            Assert.Equal(2, members.Length);
            foreach (var member in members)
            {
                var cls = (ImplicitNamedTypeSymbol)member;
                var methods = cls.GetMembers();
                if (cls.IsScriptClass)
                {
                    Assert.False(cls.IsImplicitClass);
                    AssertEx.SetEqual(new[] { "<Initialize>", "G", ".ctor", "<Main>" }, methods.Select(m => m.Name));
                }
                else
                {
                    Assert.False(cls.IsScriptClass);
                    Assert.Equal(TypeSymbol.ImplicitTypeName, member.Name);
                    AssertEx.SetEqual(new[] { "F", ".ctor" }, methods.Select(m => m.Name));
                }
                Assert.True(methods.All(m => m is MethodSymbol));
            }
        }

        [Fact]
        public void TopLevelTypesShouldBeNestedInScriptClass()
        {
            string test = @"
partial class C { }
partial class C { }
interface D { }
struct E { }
enum F { }
delegate void G();
";
            var tree = SyntaxFactory.ParseSyntaxTree(test, options: TestOptions.Script);

            var compilation = CreateCompilationWithMscorlib45(
                new[] { tree },
                options: TestOptions.ReleaseExe.WithScriptClassName("Script"));

            var global = compilation.GlobalNamespace;
            ImmutableArray<NamedTypeSymbol> members;

            members = global.GetTypeMembers("Script");
            Assert.Equal(1, members.Length);
            Assert.Equal(TypeKind.Class, members[0].TypeKind);
            var script = members[0];

            members = script.GetTypeMembers("C");
            Assert.Equal(1, members.Length);
            Assert.Equal(TypeKind.Class, members[0].TypeKind);

            members = script.GetTypeMembers("D");
            Assert.Equal(1, members.Length);
            Assert.Equal(TypeKind.Interface, members[0].TypeKind);

            members = script.GetTypeMembers("E");
            Assert.Equal(1, members.Length);
            Assert.Equal(TypeKind.Struct, members[0].TypeKind);

            members = script.GetTypeMembers("F");
            Assert.Equal(1, members.Length);
            Assert.Equal(TypeKind.Enum, members[0].TypeKind);

            members = script.GetTypeMembers("G");
            Assert.Equal(1, members.Length);
            Assert.Equal(TypeKind.Delegate, members[0].TypeKind);
        }

        [Fact]
        public void UsingStaticClass()
        {
            string test = @"
using static System.Console;
WriteLine(""hello"");
";
            var tree = SyntaxFactory.ParseSyntaxTree(test, options: TestOptions.Script.WithLanguageVersion(LanguageVersion.CSharp6));

            var compilation = CreateCompilationWithMscorlib45(
                new[] { tree },
                options: TestOptions.ReleaseExe.WithScriptClassName("Script"));

            var expr = (((tree.
                GetCompilationUnitRoot() as CompilationUnitSyntax).
                Members[0] as GlobalStatementSyntax).
                Statement as ExpressionStatementSyntax).
                Expression;

            var model = compilation.GetSemanticModel(tree);
            var info = model.GetSymbolInfo(expr);
            Assert.NotNull(info.Symbol);
            Assert.Equal("void System.Console.WriteLine(System.String value)", info.Symbol.ToTestDisplayString());
        }

        [WorkItem(3817, "https://github.com/dotnet/roslyn/issues/3817")]
        [Fact]
        public void LabelLookup()
        {
            var source = "using System; 1";
            var submission = CreateSubmission(source);
            var model = submission.GetSemanticModel(submission.SyntaxTrees.Single());
            Assert.Empty(model.LookupLabels(source.Length - 1)); // Used to assert.
        }

        [Fact]
        public void Labels()
        {
            string source =
@"L0: ;
goto L0;";
            var tree = Parse(source, options: TestOptions.Script);
            var model = CreateCompilationWithMscorlib45(new[] { tree }).GetSemanticModel(tree, ignoreAccessibility: false);
            var root = tree.GetCompilationUnitRoot();
            var statements = root.ChildNodes().Select(n => ((GlobalStatementSyntax)n).Statement).ToArray();
            var symbol0 = model.GetDeclaredSymbol((LabeledStatementSyntax)statements[0]);
            Assert.NotNull(symbol0);
            var symbol1 = model.GetSymbolInfo(((GotoStatementSyntax)statements[1]).Expression).Symbol;
            Assert.Same(symbol0, symbol1);
        }

        [Fact]
        public void Variables()
        {
            string source =
@"int x = 1;
object y = x;";
            var tree = Parse(source, options: TestOptions.Script);
            var model = CreateCompilationWithMscorlib45(new[] { tree }).GetSemanticModel(tree, ignoreAccessibility: false);
            var root = tree.GetCompilationUnitRoot();
            var declarations = root.ChildNodes().Select(n => ((FieldDeclarationSyntax)n).Declaration.Variables[0]).ToArray();
            var symbol0 = model.GetDeclaredSymbol(declarations[0]);
            Assert.NotNull(symbol0);
            var symbol1 = model.GetSymbolInfo(declarations[1].Initializer.Value).Symbol;
            Assert.Same(symbol0, symbol1);
        }

        [WorkItem(543890, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543890")]
        [Fact]
        public void ThisIndexerAccessInSubmission()
        {
            string test = @"
this[1]
";
            var compilation = CreateSubmission(test);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            compilation.VerifyDiagnostics(
                // (2,1): error CS0027: Keyword 'this' is not available in the current context
                // this[1]
                Diagnostic(ErrorCode.ERR_ThisInBadContext, "this"));

            var syntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ExpressionSyntax>().First();
            Assert.Equal(SyntaxKind.ElementAccessExpression, syntax.Kind());

            var summary = model.GetSemanticInfoSummary(syntax);
            Assert.Null(summary.Symbol);
            Assert.Equal(0, summary.CandidateSymbols.Length);
            Assert.Equal(CandidateReason.None, summary.CandidateReason);
            Assert.Equal(TypeKind.Error, summary.Type.TypeKind);
            Assert.Equal(TypeKind.Error, summary.ConvertedType.TypeKind);
            Assert.Equal(Conversion.Identity, summary.ImplicitConversion);
            Assert.Equal(0, summary.MethodGroup.Length);
        }

        /// <summary>
        /// LookupSymbols should not include the submission class.
        /// </summary>
        [WorkItem(530986, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530986")]
        [WorkItem(1010871, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1010871")]
        [Fact]
        public void LookupSymbols()
        {
            var text = "1 + ";
            var compilation = CreateSubmission(text);

            compilation.VerifyDiagnostics(
                // (1,5): error CS1733: Expected expression
                Diagnostic(ErrorCode.ERR_ExpressionExpected, ""));

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);
            var symbols = model.LookupSymbols(text.Length);

            // Should return some symbols, but not the submission class.
            Assert.True(symbols.Length > 0);
            foreach (var symbol in symbols)
            {
                if (symbol.Kind == SymbolKind.NamedType)
                {
                    var type = (NamedTypeSymbol)symbol;
                    Assert.False(type.IsScriptClass);
                    Assert.False(type.IsSubmissionClass);
                    Assert.NotEqual(TypeKind.Submission, type.TypeKind);
                }
            }

            Assert.False(symbols.Any(s => s.Name == "Roslyn"));
        }

        [Fact]
        public void HostObjectBinding_Diagnostics()
        {
            var submission = CreateSubmission("x",
                new[] { MetadataReference.CreateFromAssemblyInternal(typeof(B2).GetTypeInfo().Assembly) },
                hostObjectType: typeof(B2));

            submission.VerifyDiagnostics();
        }

        [WorkItem(543370, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543370")]
        [Fact]
        public void CheckedDecimalAddition()
        {
            string source = @"
decimal d = checked(2M + 1M);
";

            var compilation = CreateCompilationWithMscorlib45(new[] { Parse(source, options: TestOptions.Script) });
            compilation.VerifyDiagnostics();
        }

        [WorkItem(543370, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543370")]
        [Fact]
        public void CheckedEnumAddition()
        {
            string source = @"
using System.IO;
FileAccess fa = checked(FileAccess.Read + 1);
";

            var compilation = CreateCompilationWithMscorlib45(new[] { Parse(source, options: TestOptions.Script) });
            compilation.VerifyDiagnostics();
        }

        [WorkItem(543370, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543370")]
        [Fact]
        public void DelegateAddition()
        {
            string source = @"
System.Action a = null;
a += null;
";
            var compilation = CreateCompilationWithMscorlib45(new[] { Parse(source, options: TestOptions.Script) });
            compilation.VerifyDiagnostics();
        }

        [WorkItem(870885, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/870885")]
        [Fact]
        public void Bug870885()
        {
            var source = @"var o = o.F;";
            var compilation = CreateSubmission(source);

            compilation.VerifyDiagnostics(
                // (1,5): error CS7019: Type of 'o' cannot be inferred since its initializer directly or indirectly refers to the definition.
                // var o = o.F;
                Diagnostic(ErrorCode.ERR_RecursivelyTypedVariable, "o").WithArguments("o"));
        }

        [WorkItem(949595, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/949595")]
        [Fact]
        public void GlobalAttributes()
        {
            var source = @"
[assembly: System.Reflection.AssemblyVersion(""4.0.0.0"")]
[module: System.Security.UnverifiableCode]";

            var compilation = CreateSubmission(source);

            compilation.VerifyDiagnostics(
                // (2,2): error CS7026: Assembly and module attributes are not allowed in this context
                Diagnostic(ErrorCode.ERR_GlobalAttributesNotAllowed, "assembly"),
                // (3,2): error CS7026: Assembly and module attributes are not allowed in this context
                Diagnostic(ErrorCode.ERR_GlobalAttributesNotAllowed, "module"));
        }

        [Fact]
        public void SealedOverride()
        {
            var source0 = @"
class M
{
    protected virtual void F() { }
}
";
            var c0 = CreateSubmission(source0);

            var source1 = @"
class Y : M
{
    sealed protected override void F() {  }
}
";
            var c1 = CreateSubmission(source1, previous: c0);

            CompileAndVerify(c0);
            CompileAndVerify(c1);
        }

        [Fact]
        public void PrivateNested()
        {
            var c0 = CreateSubmission(@"public class C { private static int goo() { return 1; } }");
            var c1 = CreateSubmission(@"C.goo()", previous: c0);

            c1.VerifyDiagnostics(
                // error CS0122: '{0}' is inaccessible due to its protection level
                Diagnostic(ErrorCode.ERR_BadAccess, "goo").WithArguments("C.goo()"));
        }

        [Fact]
        public void InconsistentAccessibilityChecks()
        {
            var c0 = CreateSubmission(@"
private class A { }
protected class B { }
internal class C { }
internal protected class D { }
public class E { }
");

            var c1 = CreateSubmission(@"
private A a0;

private B b0;
protected B b1;

private C c0;
protected C c1;             // allowed since the submission is an internal class
internal C c2;
internal protected C c3;    // allowed since the submission is an internal class
public C c4;                // allowed since the submission is an internal class

private D d0;
protected D d1;             
internal D d2;
internal protected D d3;    
public D d4;                // allowed since the submission is an internal class

private E e0;
protected E e1;
internal E e2;
internal protected E e3;
public E e4;
", previous: c0);


            CreateSubmission(@"protected A x;", previous: c1).VerifyDiagnostics(
                // (1,10): error CS0052: Inconsistent accessibility: field type 'A' is less accessible than field 'x'
                Diagnostic(ErrorCode.ERR_BadVisFieldType, "x").WithArguments("x", "A"),
                // (1,13): warning CS0628: 'x': new protected member declared in sealed type
                Diagnostic(ErrorCode.WRN_ProtectedInSealed, "x").WithArguments("x"));

            CreateSubmission(@"internal A x;", previous: c1).VerifyDiagnostics(
                // (1,10): error CS0052: Inconsistent accessibility: field type 'A' is less accessible than field 'x'
                Diagnostic(ErrorCode.ERR_BadVisFieldType, "x").WithArguments("x", "A"));

            CreateSubmission(@"internal protected A x;", previous: c1).VerifyDiagnostics(
                // (1,10): error CS0052: Inconsistent accessibility: field type 'A' is less accessible than field 'x'
                Diagnostic(ErrorCode.ERR_BadVisFieldType, "x").WithArguments("x", "A"),
                // (1,13): warning CS0628: 'x': new protected member declared in sealed type
                Diagnostic(ErrorCode.WRN_ProtectedInSealed, "x").WithArguments("x"));

            CreateSubmission(@"public A x;", previous: c1).VerifyDiagnostics(
                // (1,10): error CS0052: Inconsistent accessibility: field type 'A' is less accessible than field 'x'
                Diagnostic(ErrorCode.ERR_BadVisFieldType, "x").WithArguments("x", "A"));

            CreateSubmission(@"internal B x;", previous: c1).VerifyDiagnostics(
                // (1,10): error CS0052: Inconsistent accessibility: field type 'B' is less accessible than field 'x'
                Diagnostic(ErrorCode.ERR_BadVisFieldType, "x").WithArguments("x", "B"));

            CreateSubmission(@"internal protected B x;", previous: c1).VerifyDiagnostics(
                // (1,10): error CS0052: Inconsistent accessibility: field type 'B' is less accessible than field 'x'
                Diagnostic(ErrorCode.ERR_BadVisFieldType, "x").WithArguments("x", "B"),
                // (1,13): warning CS0628: 'x': new protected member declared in sealed type
                Diagnostic(ErrorCode.WRN_ProtectedInSealed, "x").WithArguments("x"));

            CreateSubmission(@"public B x;", previous: c1).VerifyDiagnostics(
                // (1,10): error CS0052: Inconsistent accessibility: field type 'B' is less accessible than field 'x'
                Diagnostic(ErrorCode.ERR_BadVisFieldType, "x").WithArguments("x", "B"));
        }

        [ConditionalFact(typeof(DesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/28001")]
        public void CompilationChain_Fields()
        {
            var c0 = CreateSubmission(@"
static int s = 1;
int i = 2;
");

            var c1 = CreateSubmission("s + i", previous: c0, returnType: typeof(int));
            var c2 = CreateSubmission("static int t = i;", previous: c1);

            c2.VerifyDiagnostics(Diagnostic(ErrorCode.ERR_ObjectRequired, "i").WithArguments("i"));
        }

        [Fact]
        public void CompilationChain_InStaticContext()
        {
            var c0 = CreateSubmission(@"
int x = 1;
int y = 2;
int z() { return 3; }
int w = 4;
");
            var c1 = CreateSubmission(@"
static int Goo() { return x; }
static int Bar { get { return y; } set { return z(); } }
static int Baz = w;
", previous: c0);

            c1.VerifyDiagnostics(
                // error CS0120: An object reference is required for the non-static field, method, or property '{0}'
                Diagnostic(ErrorCode.ERR_ObjectRequired, "w").WithArguments("w"),
                Diagnostic(ErrorCode.ERR_ObjectRequired, "x").WithArguments("x"),
                Diagnostic(ErrorCode.ERR_ObjectRequired, "y").WithArguments("y"),
                Diagnostic(ErrorCode.ERR_ObjectRequired, "z").WithArguments("z()"));
        }

        [Fact]
        public void AccessToGlobalMemberFromNestedClass1()
        {
            var c0 = CreateSubmission(@"
int goo() { return 1; }

class D 
{
    int bar() { return goo(); }
}
");

            c0.VerifyDiagnostics(
                // (6,24): error CS0120: An object reference is required for the non-static field, method, or property 'goo()'
                Diagnostic(ErrorCode.ERR_ObjectRequired, "goo").WithArguments("goo()"));
        }

        [Fact]
        public void AccessToGlobalMemberFromNestedClass2()
        {
            var c0 = CreateSubmission(@"
int goo() { return 1; }
");
            var c1 = CreateSubmission(@"
class D 
{
    int bar() { return goo(); }
}
", previous: c0);

            c1.VerifyDiagnostics(
                // (4,24): error CS0120: An object reference is required for the non-static field, method, or property 'goo()'
                Diagnostic(ErrorCode.ERR_ObjectRequired, "goo").WithArguments("goo()"));
        }

        /// <summary>
        /// Previous submission has to have no errors.
        /// </summary>
        [Fact]
        public void Submissions_ExecutionOrder3()
        {
            var s0 = CreateSubmission("int a = \"x\";");
            s0.VerifyDiagnostics(
                // (1,9): error CS0029: Cannot implicitly convert type 'string' to 'int'
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""x""").WithArguments("string", "int"));

            Assert.Throws<InvalidOperationException>(() => CreateSubmission("a + 1", previous: s0));
        }

        [WorkItem(3795, "https://github.com/dotnet/roslyn/issues/3795")]
        [Fact]
        public void ErrorInUsing()
        {
            var submission = CreateSubmission("using Unknown;");

            submission.VerifyDiagnostics(
                // (1,7): error CS0246: The type or namespace name 'Unknown' could not be found (are you missing a using directive or an assembly reference?)
                // using Unknown;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Unknown").WithArguments("Unknown"));

            submission.VerifyEmitDiagnostics(
                // (1,7): error CS0246: The type or namespace name 'Unknown' could not be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Unknown").WithArguments("Unknown"));
        }

        [Fact]
        public void HostObjectBinding_MissingHostObjectContext()
        {
            var c = CreateSubmission("Z()", new[] { HostRef });

            c.VerifyDiagnostics(
                // (1,1): error CS0103: The name 'z' does not exist in the current context
                Diagnostic(ErrorCode.ERR_NameNotInContext, "Z").WithArguments("Z"));
        }

        [ConditionalFact(typeof(DesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/28001")]
        public void HostObjectBinding_InStaticContext()
        {
            var source = @"
static int Goo() { return x; }
static int Bar { get { return Y; } set { return Z(); } }
static int Baz = w;
";

            var c = CreateSubmission(source, new[] { HostRef }, hostObjectType: typeof(C));

            var typeName = typeof(ScriptTestFixtures).FullName;

            c.VerifyDiagnostics(
                // (4,18): error CS0120: An object reference is required for the non-static field, method, or property 'Roslyn.Compilers.CSharp.UnitTests.Symbols.Source.InteractiveSessionTests.B.w'
                Diagnostic(ErrorCode.ERR_ObjectRequired, "w").WithArguments(typeName + ".B.w"),
                // (2,27): error CS0120: An object reference is required for the non-static field, method, or property 'Roslyn.Compilers.CSharp.UnitTests.Symbols.Source.InteractiveSessionTests.B.x'
                Diagnostic(ErrorCode.ERR_ObjectRequired, "x").WithArguments(typeName + ".B.x"),
                // (3,31): error CS0120: An object reference is required for the non-static field, method, or property 'Roslyn.Compilers.CSharp.UnitTests.Symbols.Source.InteractiveSessionTests.C.Y'
                Diagnostic(ErrorCode.ERR_ObjectRequired, "Y").WithArguments(typeName + ".C.Y"),
                // (3,49): error CS0120: An object reference is required for the non-static field, method, or property 'Roslyn.Compilers.CSharp.UnitTests.Symbols.Source.InteractiveSessionTests.C.Z()'
                Diagnostic(ErrorCode.ERR_ObjectRequired, "Z").WithArguments(typeName + ".C.Z()"));

        }

        [Fact]
        public void WRN_LowercaseEllSuffix()
        {
            var c = CreateSubmission("int i = 42l;");

            c.VerifyDiagnostics(
                // (1,11): warning CS0078: The 'l' suffix is easily confused with the digit '1' -- use 'L' for clarity
                Diagnostic(ErrorCode.WRN_LowercaseEllSuffix, "l"),
                // (1,9): error CS0266: Cannot implicitly convert type 'long' to 'int'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "42l").WithArguments("long", "int"));
        }

        [Fact]
        public void ERR_RecursivelyTypedVariable()
        {
            var c = CreateSubmission("var x = x;");

            c.VerifyDiagnostics(
                // (1,5): error CS7019: Type of 'x' cannot be inferred since its initializer directly or indirectly refers to the definition.
                Diagnostic(ErrorCode.ERR_RecursivelyTypedVariable, "x").WithArguments("x"));
        }

        [Fact]
        public void ERR_VariableUsedBeforeDeclaration_01()
        {
            var c = CreateSubmission("var x = 1; { var x = x;}");
            c.VerifyDiagnostics(
                // (1,22): error CS0841: Cannot use local variable 'x' before it is declared
                // var x = 1; { var x = x;}
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x").WithArguments("x").WithLocation(1, 22)
                );
        }

        [WorkItem(550, "https://github.com/dotnet/roslyn/issues/550")]
        [Fact]
        public void ERR_VariableUsedBeforeDeclaration_02()
        {
            var c = CreateSubmission(
@"object b = a;
object a;
void F()
{
    object d = c;
    object c;
}
{
    object f = e;
    object e;
}");
            c.VerifyDiagnostics(
                // (9,16): error CS0841: Cannot use local variable 'e' before it is declared
                //     object f = e;
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "e").WithArguments("e").WithLocation(9, 16),
                // (5,16): error CS0841: Cannot use local variable 'c' before it is declared
                //     object d = c;
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "c").WithArguments("c").WithLocation(5, 16));
        }

        [WorkItem(550, "https://github.com/dotnet/roslyn/issues/550")]
        [Fact]
        public void ERR_UseDefViolation()
        {
            var c = CreateSubmission(
@"int a;
int b = a;
void F()
{
    int c;
    int d = c;
}
{
    int e;
    int f = e;
}");
            c.VerifyDiagnostics(
                // (10,13): error CS0165: Use of unassigned local variable 'e'
                //     int f = e;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "e").WithArguments("e").WithLocation(10, 13),
                // (6,13): error CS0165: Use of unassigned local variable 'c'
                //     int d = c;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "c").WithArguments("c").WithLocation(6, 13));
        }

        [Fact]
        public void ERR_FieldCantBeRefAny()
        {
            var c = CreateSubmission(@"
System.RuntimeArgumentHandle a;
System.ArgIterator b;
System.TypedReference c;
");
            c.VerifyDiagnostics(
                // (2,1): error CS0610: Field or property cannot be of type 'System.RuntimeArgumentHandle'
                Diagnostic(ErrorCode.ERR_FieldCantBeRefAny, "System.RuntimeArgumentHandle").WithArguments("System.RuntimeArgumentHandle"),
                // (3,1): error CS0610: Field or property cannot be of type 'System.ArgIterator'
                Diagnostic(ErrorCode.ERR_FieldCantBeRefAny, "System.ArgIterator").WithArguments("System.ArgIterator"),
                // (4,1): error CS0610: Field or property cannot be of type 'System.TypedReference'
                Diagnostic(ErrorCode.ERR_FieldCantBeRefAny, "System.TypedReference").WithArguments("System.TypedReference"));
        }

        [WorkItem(529387, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529387")]
        [Fact]
        public void IsVariable_PreviousSubmission()
        {
            var c0 = CreateSubmission("var x = 1;");
            var c1 = CreateSubmission("&x", previous: c0);

            c1.VerifyDiagnostics(
                // (1,1): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                // &x
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&x").WithLocation(1, 1));
        }

        [Fact]
        public void IsVariable_HostObject()
        {
            var c0 = CreateSubmission("&x", new[] { HostRef }, hostObjectType: typeof(B2));

            c0.VerifyDiagnostics(
                // (1,1): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                // &x
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&x").WithLocation(1, 1));
        }

        [WorkItem(530404, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530404")]
        [Fact]
        public void DiagnosticsPass()
        {
            var source = "(System.Linq.Expressions.Expression<System.Func<object>>)(() => null ?? new object())";

            var c0 = CreateSubmission(source, new[] { SystemCoreRef });

            c0.VerifyDiagnostics(
                // (1,65): error CS0845: An expression tree lambda may not contain a coalescing operator with a null literal left-hand side
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsBadCoalesce, "null").WithLocation(1, 65));
        }

        [WorkItem(527850, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527850")]
        [Fact]
        public void ArithmeticOperators_MultiplicationExpression()
        {
            var s0 = CreateSubmission("int i = 5;");
            var s1 = CreateSubmission("i* i", previous: s0);
            var s2 = CreateSubmission("i* i;", previous: s1);

            s1.VerifyEmitDiagnostics();

            s2.VerifyDiagnostics(
                // (1,1): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                Diagnostic(ErrorCode.ERR_IllegalStatement, "i* i"));
        }

        [WorkItem(527850, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527850")]
        [WorkItem(522569, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/522569")]
        [WorkItem(4737, "https://github.com/dotnet/roslyn/issues/4737")]
        [Fact(Skip = "4737")]
        public void TopLevelLabel()
        {
            var s0 = CreateSubmission(@"
Label: ; 
goto Label;");

            s0.VerifyDiagnostics();
        }

        [WorkItem(541210, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541210")]
        [Fact]
        public void TopLevelGoto()
        {
            var s0 = CreateSubmission("goto Object;");

            s0.VerifyDiagnostics(
                // (1,1): error CS0159: No such label 'Object' within the scope of the goto statement
                Diagnostic(ErrorCode.ERR_LabelNotFound, "Object").WithArguments("Object"));
        }

        [WorkItem(541166, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541166")]
        [Fact]
        public void DefineExtensionMethods()
        {
            var references = new[] { TestMetadata.Net451.SystemCore };

            // No error for extension method defined in interactive session.
            var s0 = CreateSubmission("static void E(this object o) { }", references);

            var s1 = CreateSubmission("void F(this object o) { }", references, previous: s0);

            s1.VerifyDiagnostics(
                // (1,6): error CS1105: Extension method must be static
                // void F(this object o) { }
                Diagnostic(ErrorCode.ERR_BadExtensionMeth, "F"));

            var s2 = CreateSubmission("static void G(this dynamic o) { }", references, previous: s0);

            s2.VerifyDiagnostics(
                // error CS1103: The first parameter of an extension method cannot be of type 'dynamic'
                Diagnostic(ErrorCode.ERR_BadTypeforThis, "dynamic").WithArguments("dynamic"));
        }

        [Fact]
        [WorkItem(13590, "https://github.com/dotnet/roslyn/issues/13590")]
        public void FixedBuffer_01()
        {
            string source =
@"fixed int x[3];
";
            var tree = Parse(source, options: TestOptions.Script);
            var compilation = CreateCompilationWithMscorlib45(new[] { tree });

            compilation.VerifyDiagnostics(
                // (1,11): error CS1642: Fixed size buffer fields may only be members of structs
                // fixed int x[3];
                Diagnostic(ErrorCode.ERR_FixedNotInStruct, "x").WithLocation(1, 11),
                // (1,11): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                // fixed int x[3];
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "x[3]").WithLocation(1, 11)
                );
        }

        [Fact]
        [WorkItem(13590, "https://github.com/dotnet/roslyn/issues/13590")]
        public void FixedBuffer_02()
        {
            string source =
@"fixed var x[3] = 1;
";
            var tree = Parse(source, options: TestOptions.Script);
            var compilation = CreateCompilationWithMscorlib45(new[] { tree });

            compilation.VerifyDiagnostics(
                // (1,16): error CS1003: Syntax error, ',' expected
                // fixed var x[3] = 1;
                Diagnostic(ErrorCode.ERR_SyntaxError, "=").WithArguments(",").WithLocation(1, 16),
                // (1,11): error CS1642: Fixed size buffer fields may only be members of structs
                // fixed var x[3] = 1;
                Diagnostic(ErrorCode.ERR_FixedNotInStruct, "x").WithLocation(1, 11),
                // (1,7): error CS1663: Fixed size buffer type must be one of the following: bool, byte, short, int, long, char, sbyte, ushort, uint, ulong, float or double
                // fixed var x[3] = 1;
                Diagnostic(ErrorCode.ERR_IllegalFixedType, "var").WithLocation(1, 7),
                // (1,11): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                // fixed var x[3] = 1;
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "x[3]").WithLocation(1, 11)
                );
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/44418")]
        [WorkItem(10023, "https://github.com/dotnet/roslyn/issues/10023")]
        [WorkItem(44418, "https://github.com/dotnet/roslyn/issues/44418")]
        public void Errors_01()
        {
            var code = "System.Console.WriteLine(1);";
            var compilationUnit = CSharp.SyntaxFactory.ParseCompilationUnit(code, options: new CSharp.CSharpParseOptions(kind: SourceCodeKind.Script));
            var syntaxTree = compilationUnit.SyntaxTree;
            var compilation = CreateCompilationWithMscorlib45(new[] { syntaxTree });
            var semanticModel = compilation.GetSemanticModel(syntaxTree, true);
            MemberAccessExpressionSyntax node5 = ErrorTestsGetNode(syntaxTree);
            Assert.Equal("WriteLine", node5.Name.ToString());
            Assert.Null(semanticModel.GetSymbolInfo(node5.Name).Symbol);

            compilation.VerifyDiagnostics(
                // (1,1): error CS7006: Expressions and statements can only occur in a method body
                // System.Console.WriteLine(1);
                Diagnostic(ErrorCode.ERR_GlobalStatement, "System.Console.WriteLine(1);").WithLocation(1, 1)
                );

            compilation = CreateCompilationWithMscorlib45(new[] { syntaxTree }, options: TestOptions.ReleaseExe.WithScriptClassName("Script"));
            semanticModel = compilation.GetSemanticModel(syntaxTree, true);
            node5 = ErrorTestsGetNode(syntaxTree);
            Assert.Equal("WriteLine", node5.Name.ToString());
            Assert.Null(semanticModel.GetSymbolInfo(node5.Name).Symbol);

            compilation.VerifyDiagnostics(
                // (1,1): error CS7006: Expressions and statements can only occur in a method body
                // System.Console.WriteLine(1);
                Diagnostic(ErrorCode.ERR_GlobalStatement, "System.Console.WriteLine(1);").WithLocation(1, 1),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1)
                );

            syntaxTree = SyntaxFactory.ParseSyntaxTree(code, options: new CSharp.CSharpParseOptions(kind: SourceCodeKind.Script));
            compilation = CreateCompilationWithMscorlib45(new[] { syntaxTree }, options: TestOptions.ReleaseExe);
            semanticModel = compilation.GetSemanticModel(syntaxTree, true);
            node5 = ErrorTestsGetNode(syntaxTree);
            Assert.Equal("WriteLine", node5.Name.ToString());
            Assert.Equal("void System.Console.WriteLine(System.Int32 value)", semanticModel.GetSymbolInfo(node5.Name).Symbol.ToTestDisplayString());

            CompileAndVerify(compilation, expectedOutput: "1").VerifyDiagnostics();

            syntaxTree = SyntaxFactory.ParseSyntaxTree(code, options: new CSharp.CSharpParseOptions(kind: SourceCodeKind.Script));
            compilation = CreateCompilationWithMscorlib45(new[] { syntaxTree }, options: TestOptions.ReleaseExe.WithScriptClassName("Script"));
            semanticModel = compilation.GetSemanticModel(syntaxTree, true);
            node5 = ErrorTestsGetNode(syntaxTree);
            Assert.Equal("WriteLine", node5.Name.ToString());
            Assert.Equal("void System.Console.WriteLine(System.Int32 value)", semanticModel.GetSymbolInfo(node5.Name).Symbol.ToTestDisplayString());

            CompileAndVerify(compilation, expectedOutput: "1").VerifyDiagnostics();

            syntaxTree = SyntaxFactory.ParseSyntaxTree(code, options: new CSharp.CSharpParseOptions(kind: SourceCodeKind.Script));
            compilation = CreateCompilationWithMscorlib45(new[] { syntaxTree }, options: TestOptions.ReleaseExe.WithScriptClassName(""));
            semanticModel = compilation.GetSemanticModel(syntaxTree, true);
            node5 = ErrorTestsGetNode(syntaxTree);
            Assert.Equal("WriteLine", node5.Name.ToString());
            Assert.Equal("void System.Console.WriteLine(System.Int32 value)", semanticModel.GetSymbolInfo(node5.Name).Symbol.ToTestDisplayString());

            compilation.VerifyDiagnostics(
                // error CS7088: Invalid 'ScriptClassName' value: ''.
                Diagnostic(ErrorCode.ERR_BadCompilationOptionValue).WithArguments("ScriptClassName", "").WithLocation(1, 1)
                );

            syntaxTree = SyntaxFactory.ParseSyntaxTree(code, options: new CSharp.CSharpParseOptions(kind: SourceCodeKind.Script));
            compilation = CreateCompilationWithMscorlib45(new[] { syntaxTree }, options: TestOptions.ReleaseExe.WithScriptClassName(null));
            semanticModel = compilation.GetSemanticModel(syntaxTree, true);
            node5 = ErrorTestsGetNode(syntaxTree);
            Assert.Equal("WriteLine", node5.Name.ToString());
            Assert.Equal("void System.Console.WriteLine(System.Int32 value)", semanticModel.GetSymbolInfo(node5.Name).Symbol.ToTestDisplayString());

            compilation.VerifyDiagnostics(
                // error CS7088: Invalid 'ScriptClassName' value: 'null'.
                Diagnostic(ErrorCode.ERR_BadCompilationOptionValue).WithArguments("ScriptClassName", "null")
                );

            syntaxTree = SyntaxFactory.ParseSyntaxTree(code, options: new CSharp.CSharpParseOptions(kind: SourceCodeKind.Script));
            compilation = CreateCompilationWithMscorlib45(new[] { syntaxTree }, options: TestOptions.ReleaseExe.WithScriptClassName("a\0b"));
            semanticModel = compilation.GetSemanticModel(syntaxTree, true);
            node5 = ErrorTestsGetNode(syntaxTree);
            Assert.Equal("WriteLine", node5.Name.ToString());
            Assert.Equal("void System.Console.WriteLine(System.Int32 value)", semanticModel.GetSymbolInfo(node5.Name).Symbol.ToTestDisplayString());

            compilation.VerifyDiagnostics(
                // error CS7088: Invalid 'ScriptClassName' value: 'a\0b'.
                Diagnostic(ErrorCode.ERR_BadCompilationOptionValue).WithArguments("ScriptClassName", "a\0b")
                );
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/44418")]
        [WorkItem(10023, "https://github.com/dotnet/roslyn/issues/10023")]
        [WorkItem(44418, "https://github.com/dotnet/roslyn/issues/44418")]
        public void Errors_02()
        {
            var compilationUnit = CSharp.SyntaxFactory.ParseCompilationUnit("\nSystem.Console.WriteLine(1);", options: new CSharp.CSharpParseOptions(kind: SourceCodeKind.Script));
            var syntaxTree1 = compilationUnit.SyntaxTree;
            var syntaxTree2 = SyntaxFactory.ParseSyntaxTree("System.Console.WriteLine(2);", options: new CSharp.CSharpParseOptions(kind: SourceCodeKind.Script));
            MemberAccessExpressionSyntax node1 = ErrorTestsGetNode(syntaxTree1);
            MemberAccessExpressionSyntax node2 = ErrorTestsGetNode(syntaxTree2);
            Assert.Equal("WriteLine", node1.Name.ToString());
            Assert.Equal("WriteLine", node2.Name.ToString());

            var compilation = CreateCompilationWithMscorlib45(new[] { syntaxTree1, syntaxTree2 });
            var semanticModel1 = compilation.GetSemanticModel(syntaxTree1, true);
            var semanticModel2 = compilation.GetSemanticModel(syntaxTree2, true);
            Assert.Null(semanticModel1.GetSymbolInfo(node1.Name).Symbol);
            Assert.Equal("void System.Console.WriteLine(System.Int32 value)", semanticModel2.GetSymbolInfo(node2.Name).Symbol.ToTestDisplayString());

            compilation.VerifyDiagnostics(
                // (2,1): error CS7006: Expressions and statements can only occur in a method body
                // System.Console.WriteLine(1);
                Diagnostic(ErrorCode.ERR_GlobalStatement, "System.Console.WriteLine(1);").WithLocation(2, 1)
                );

            compilation = CreateCompilationWithMscorlib45(new[] { syntaxTree2, syntaxTree1 });
            semanticModel1 = compilation.GetSemanticModel(syntaxTree1, true);
            semanticModel2 = compilation.GetSemanticModel(syntaxTree2, true);
            Assert.Null(semanticModel1.GetSymbolInfo(node1.Name).Symbol);
            Assert.Equal("void System.Console.WriteLine(System.Int32 value)", semanticModel2.GetSymbolInfo(node2.Name).Symbol.ToTestDisplayString());

            compilation.VerifyDiagnostics(
                // (2,1): error CS7006: Expressions and statements can only occur in a method body
                // System.Console.WriteLine(1);
                Diagnostic(ErrorCode.ERR_GlobalStatement, "System.Console.WriteLine(1);").WithLocation(2, 1)
                );
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/44418")]
        [WorkItem(10023, "https://github.com/dotnet/roslyn/issues/10023")]
        [WorkItem(44418, "https://github.com/dotnet/roslyn/issues/44418")]
        public void Errors_03()
        {
            var code = "System.Console.WriteLine(out var x, x);";
            var compilationUnit = CSharp.SyntaxFactory.ParseCompilationUnit(code, options: new CSharp.CSharpParseOptions(kind: SourceCodeKind.Script));
            var syntaxTree = compilationUnit.SyntaxTree;
            var compilation = CreateCompilationWithMscorlib45(new[] { syntaxTree });
            var semanticModel = compilation.GetSemanticModel(syntaxTree, true);
            MemberAccessExpressionSyntax node5 = ErrorTestsGetNode(syntaxTree);
            Assert.Equal("WriteLine", node5.Name.ToString());
            Assert.Null(semanticModel.GetSymbolInfo(node5.Name).Symbol);
            var x = syntaxTree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x").Single();
            Assert.Null(semanticModel.GetSymbolInfo(x).Symbol);

            compilation.VerifyDiagnostics(
                // (1,1): error CS7006: Expressions and statements can only occur in a method body
                // System.Console.WriteLine(out var x, x);
                Diagnostic(ErrorCode.ERR_GlobalStatement, "System.Console.WriteLine(out var x, x);").WithLocation(1, 1)
                );

            compilation = CreateCompilationWithMscorlib45(new[] { syntaxTree }, options: TestOptions.ReleaseExe.WithScriptClassName("Script1"));
            semanticModel = compilation.GetSemanticModel(syntaxTree, true);
            node5 = ErrorTestsGetNode(syntaxTree);
            Assert.Equal("WriteLine", node5.Name.ToString());
            Assert.Null(semanticModel.GetSymbolInfo(node5.Name).Symbol);

            compilation.VerifyDiagnostics(
                // (1,1): error CS7006: Expressions and statements can only occur in a method body
                // System.Console.WriteLine(out var x, x);
                Diagnostic(ErrorCode.ERR_GlobalStatement, "System.Console.WriteLine(out var x, x);").WithLocation(1, 1),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1)
                );

            syntaxTree = SyntaxFactory.ParseSyntaxTree(code, options: new CSharp.CSharpParseOptions(kind: SourceCodeKind.Script));
            compilation = CreateCompilationWithMscorlib45(new[] { syntaxTree });
            semanticModel = compilation.GetSemanticModel(syntaxTree, true);
            node5 = ErrorTestsGetNode(syntaxTree);
            Assert.Equal("WriteLine", node5.Name.ToString());
            Assert.Null(semanticModel.GetSymbolInfo(node5.Name).Symbol);
            x = syntaxTree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x").Single();
            Assert.Equal("var Script.x", semanticModel.GetSymbolInfo(x).Symbol.ToTestDisplayString());

            compilation.VerifyDiagnostics(
                // (1,34): error CS7019: Type of 'x' cannot be inferred since its initializer directly or indirectly refers to the definition.
                // System.Console.WriteLine(out var x, x);
                Diagnostic(ErrorCode.ERR_RecursivelyTypedVariable, "x").WithArguments("x").WithLocation(1, 34)
                );

            syntaxTree = SyntaxFactory.ParseSyntaxTree(code, options: new CSharp.CSharpParseOptions(kind: SourceCodeKind.Script));
            compilation = CreateCompilationWithMscorlib45(new[] { syntaxTree }, options: TestOptions.ReleaseExe.WithScriptClassName("Script1"));
            semanticModel = compilation.GetSemanticModel(syntaxTree, true);
            node5 = ErrorTestsGetNode(syntaxTree);
            Assert.Equal("WriteLine", node5.Name.ToString());
            Assert.Null(semanticModel.GetSymbolInfo(node5.Name).Symbol);
            x = syntaxTree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x").Single();
            Assert.Equal("var Script1.x", semanticModel.GetSymbolInfo(x).Symbol.ToTestDisplayString());

            compilation.VerifyDiagnostics(
                // (1,34): error CS7019: Type of 'x' cannot be inferred since its initializer directly or indirectly refers to the definition.
                // System.Console.WriteLine(out var x, x);
                Diagnostic(ErrorCode.ERR_RecursivelyTypedVariable, "x").WithArguments("x").WithLocation(1, 34)
                );
        }

        [Fact]
        [WorkItem(17779, "https://github.com/dotnet/roslyn/issues/17779")]
        public void TestScriptWithConstVar()
        {
            var script = CreateEmptyCompilation(
                source: @"string F() => null; const var x = F();",
                parseOptions: TestOptions.Script,
                options: TestOptions.DebugExe,
                references: new MetadataReference[] { TaskFacadeAssembly(), MscorlibRef_v20 });

            script.VerifyDiagnostics(
                // (1,27): error CS0822: Implicitly-typed variables cannot be constant
                // string F() => null; const var x = F();
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableCannotBeConst, "var").WithLocation(1, 27),
                // (1,35): error CS0236: A field initializer cannot reference the non-static field, method, or property 'F()'
                // string F() => null; const var x = F();
                Diagnostic(ErrorCode.ERR_FieldInitRefNonstatic, "F").WithArguments("F()").WithLocation(1, 35)
                );
        }

        private static MemberAccessExpressionSyntax ErrorTestsGetNode(SyntaxTree syntaxTree)
        {
            var node1 = (CompilationUnitSyntax)syntaxTree.GetRoot();
            var node2 = (GlobalStatementSyntax)node1.Members.First();
            var node3 = (ExpressionStatementSyntax)node2.Statement;
            var node4 = (InvocationExpressionSyntax)node3.Expression;
            var node5 = (MemberAccessExpressionSyntax)node4.Expression;
            return node5;
        }
    }
}
