// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Emit
{
    public class ScriptTests : CSharpTestBase
    {
        [Fact]
        public void NoReferences()
        {
            var submission = CSharpCompilation.CreateSubmission("test", syntaxTree: SyntaxFactory.ParseSyntaxTree("1", options: TestOptions.Interactive), returnType: typeof(int));
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
                // (1,1): error CS0518: Predefined type 'System.Int32' is not defined or imported
                // 1
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "1").WithArguments("System.Int32").WithLocation(1, 1),
                // error CS0518: Predefined type 'System.Object' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Object").WithLocation(1, 1));
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
            // to a compilation with CompilationOptions(scriptClassName: "Foo.Script").
            //
            // public class A { }
            // namespace Foo
            // {
            //     public class B : Script.A { }
            // }
            //
            // The resulting compilation will contain the following types in the global namespace.
            //
            //   * Foo.Script
            //   * Foo.Script.A
            //   * Foo.B : Foo.Script.A

            string test = @"
public class A { }
namespace Foo
{
    public class B : Script.A { }
}
";
            var tree = SyntaxFactory.ParseSyntaxTree(test, options: TestOptions.Script);

            var compilation = CSharpCompilation.Create(
                assemblyName: "Test",
                options: TestOptions.ReleaseExe.WithScriptClassName("Foo.Script"),
                syntaxTrees: new[] { tree });

            var global = compilation.GlobalNamespace;

            var foo = global.GetMembers().Single() as NamespaceSymbol;
            Assert.Equal("Foo", foo.Name);

            var script = foo.GetTypeMembers("Script").Single();
            Assert.Equal("Foo.Script", script.ToTestDisplayString());

            var a = script.GetTypeMembers("A").Single();
            Assert.Equal("Foo.Script.A", a.ToTestDisplayString());

            var b = foo.GetTypeMembers("B").Single();
            Assert.Equal("Foo.B", b.ToTestDisplayString());
            Assert.Same(a, b.BaseType);
        }

        [Fact]
        public void NamespaceWithBothInteractiveAndNoninteractiveImplicitTypes()
        {
            string test = @"
namespace Foo { void F() { } }
void G() { }
G();
";
            var tree = SyntaxFactory.ParseSyntaxTree(test, options: TestOptions.Script);

            var compilation = CSharpCompilation.Create(
                assemblyName: "Test",
                options: TestOptions.ReleaseExe.WithScriptClassName("Foo.Script"),
                syntaxTrees: new[] { tree });

            var global = compilation.GlobalNamespace;
            var members = global.GetMembers();

            Assert.Equal(1, members.Length);
            Assert.Equal("Foo", members[0].Name);
            Assert.IsAssignableFrom(typeof(NamespaceSymbol), members[0]);
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

            var compilation = CSharpCompilation.Create(
                assemblyName: "Test",
                options: TestOptions.ReleaseExe.WithScriptClassName("Script"),
                syntaxTrees: new[] { tree });

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

            var compilation = CSharpCompilation.Create(
                assemblyName: "Test",
                options: TestOptions.ReleaseExe.WithScriptClassName("Script"),
                syntaxTrees: new[] { tree },
                references: new[] { MscorlibRef });

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


        [Fact]
        public void AnonymousTypes_TopLevelVar()
        {
            string test = @"
using System;
var o = new { a = 1 };
Console.WriteLine(o.ToString());
";
            var tree = SyntaxFactory.ParseSyntaxTree(test, options: TestOptions.Script);

            CompileAndVerify(
                CSharpCompilation.Create(
                    assemblyName: "Test",
                    options: TestOptions.ReleaseExe.WithScriptClassName("Script"),
                    references: new[] { MscorlibRef_v4_0_30316_17626, SystemCoreRef },
                    syntaxTrees: new[] { tree }),
                expectedOutput: "{ a = 1 }"
            );
        }

        [Fact]
        public void AnonymousTypes_TopLevel_Object()
        {
            string test = @"
using System;
object o = new { a = 1 };
Console.WriteLine(o.ToString());
";
            var tree = SyntaxFactory.ParseSyntaxTree(test, options: TestOptions.Script);

            CompileAndVerify(
                CSharpCompilation.Create(
                    assemblyName: "Test",
                    options: TestOptions.ReleaseExe.WithScriptClassName("Script"),
                    references: new[] { MscorlibRef_v4_0_30316_17626, SystemCoreRef },
                    syntaxTrees: new[] { tree }),
                expectedOutput: "{ a = 1 }"
            );
        }

        [Fact]
        public void AnonymousTypes_TopLevel_NoLocal()
        {
            string test = @"
using System;
Console.WriteLine(new { a = 1 }.ToString());
";
            var tree = SyntaxFactory.ParseSyntaxTree(test, options: TestOptions.Script);

            CompileAndVerify(
                CSharpCompilation.Create(
                    assemblyName: "Test",
                    options: TestOptions.ReleaseExe.WithScriptClassName("Script"),
                    references: new[] { MscorlibRef_v4_0_30316_17626, SystemCoreRef },
                    syntaxTrees: new[] { tree }),
                expectedOutput: "{ a = 1 }"
            );
        }

        [Fact]
        public void AnonymousTypes_NestedClass_Method()
        {
            string test = @"
using System;
class CLS 
{
    public void M()
    {
        Console.WriteLine(new { a = 1 }.ToString());
    }
}

new CLS().M();
";
            var tree = SyntaxFactory.ParseSyntaxTree(test, options: TestOptions.Script);

            CompileAndVerify(
                CSharpCompilation.Create(
                    assemblyName: "Test",
                    options: TestOptions.ReleaseExe.WithScriptClassName("Script"),
                    references: new[] { MscorlibRef_v4_0_30316_17626, SystemCoreRef },
                    syntaxTrees: new[] { tree }),
                expectedOutput: "{ a = 1 }"
            );
        }

        [Fact]
        public void AnonymousTypes_NestedClass_MethodParamDefValue()
        {
            string test = @"
using System;
class CLS 
{
    public void M(object p = new { a = 1 })
    {
        Console.WriteLine(""OK"");
    }
}
new CLS().M();
";
            var tree = SyntaxFactory.ParseSyntaxTree(test, options: TestOptions.Script);

            var compilation = CSharpCompilation.Create(
                                assemblyName: "Test",
                                options: TestOptions.ReleaseExe.WithScriptClassName("Script"),
                                references: new[] { MscorlibRef },
                                syntaxTrees: new[] { tree });

            compilation.VerifyDiagnostics(
                // (5,30): error CS1736: Default parameter value for 'p' must be a compile-time constant
                //     public void M(object p = new { a = 1 })
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "new { a = 1 }").WithArguments("p"));
        }

        [Fact]
        public void AnonymousTypes_TopLevel_MethodParamDefValue()
        {
            string test = @"
using System;

public void M(object p = new { a = 1 })
{
    Console.WriteLine(""OK"");
}

M();
";
            var tree = SyntaxFactory.ParseSyntaxTree(test, options: TestOptions.Script);

            var compilation = CSharpCompilation.Create(
                                assemblyName: "Test",
                                options: TestOptions.ReleaseExe.WithScriptClassName("Script"),
                                references: new[] { MscorlibRef },
                                syntaxTrees: new[] { tree });

            compilation.VerifyDiagnostics(
                // (4,26): error CS1736: Default parameter value for 'p' must be a compile-time constant
                // public void M(object p = new { a = 1 })
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "new { a = 1 }").WithArguments("p"));
        }

        [Fact]
        public void AnonymousTypes_TopLevel_MethodAttribute()
        {
            string test = @"
using System;

class A: Attribute
{
    public object P;
}

[A(P = new { a = 1 })]
public void M()
{
    Console.WriteLine(""OK"");
}

M();
";
            var tree = SyntaxFactory.ParseSyntaxTree(test, options: TestOptions.Script);

            var compilation = CSharpCompilation.Create(
                                assemblyName: "Test",
                                options: TestOptions.ReleaseExe.WithScriptClassName("Script"),
                                references: new[] { MscorlibRef },
                                syntaxTrees: new[] { tree });

            compilation.VerifyDiagnostics(
                // (9,8): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                // [A(P = new { a = 1 })]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "new { a = 1 }"));
        }

        [Fact]
        public void AnonymousTypes_NestedTypeAttribute()
        {
            string test = @"
using System;

class A: Attribute
{
    public object P;
}

[A(P = new { a = 1 })]
class CLS 
{
}
";
            var tree = SyntaxFactory.ParseSyntaxTree(test, options: TestOptions.Script);

            var compilation = CSharpCompilation.Create(
                                assemblyName: "Test",
                                options: TestOptions.ReleaseExe.WithScriptClassName("Script"),
                                references: new[] { MscorlibRef },
                                syntaxTrees: new[] { tree });

            compilation.VerifyDiagnostics(
                // (9,8): error CS0836: Cannot use anonymous type in a constant expression
                // [A(P = new { a = 1 })]
                Diagnostic(ErrorCode.ERR_AnonymousTypeNotAvailable, "new"));
        }

        [Fact]
        public void CompilationChain_AnonymousTypeTemplates()
        {
            MetadataReference[] references = { MscorlibRef_v4_0_30316_17626, SystemCoreRef };

            var parseOptions = TestOptions.Interactive;
            var s0 = CSharpCompilation.CreateSubmission("s0.dll",
                                                  syntaxTree: SyntaxFactory.ParseSyntaxTree("var x = new { a = 1 }; ", options: parseOptions),
                                                  references: references,
                                                  returnType: typeof(object));

            var sx = CSharpCompilation.CreateSubmission("sx.dll",
                                                  syntaxTree: SyntaxFactory.ParseSyntaxTree("var y = new { b = 2 }; ", options: parseOptions),
                                                  previousSubmission: s0,
                                                  references: references,
                                                  returnType: typeof(object));

            var s1 = CSharpCompilation.CreateSubmission("s1.dll",
                                                  syntaxTree: SyntaxFactory.ParseSyntaxTree("var y = new { b = new { a = 3 } }; ", options: parseOptions),
                                                  previousSubmission: s0,
                                                  references: references,
                                                  returnType: typeof(object));

            var s2 = CSharpCompilation.CreateSubmission("s2.dll",
                                                  syntaxTree: SyntaxFactory.ParseSyntaxTree("x = y.b; ", options: parseOptions),
                                                  previousSubmission: s1,
                                                  references: references,
                                                  returnType: typeof(object));

            var diagnostics = s2.GetDiagnostics().ToArray();
            Assert.Equal(0, diagnostics.Length);

            using (MemoryStream stream = new MemoryStream())
            {
                s2.Emit(stream);
            }

            Assert.True(s2.AnonymousTypeManager.AreTemplatesSealed);
            Assert.Equal(0, s2.AnonymousTypeManager.GetAllCreatedTemplates().Length);

            Assert.True(s1.AnonymousTypeManager.AreTemplatesSealed);
            Assert.Equal(1, s1.AnonymousTypeManager.GetAllCreatedTemplates().Length);

            Assert.True(s0.AnonymousTypeManager.AreTemplatesSealed);
            Assert.Equal(1, s0.AnonymousTypeManager.GetAllCreatedTemplates().Length);

            Assert.False(sx.AnonymousTypeManager.AreTemplatesSealed);
        }

        [Fact]
        public void CompilationChain_DynamicSiteDelegates()
        {
            MetadataReference[] references = { MscorlibRef, SystemCoreRef, CSharpRef };

            var parseOptions = TestOptions.Interactive;
            var s0 = CSharpCompilation.CreateSubmission("s0.dll",
                                                  syntaxTree: SyntaxFactory.ParseSyntaxTree("var i = 1; dynamic d = null; d.m(ref i);", options: parseOptions),
                                                  references: references,
                                                  returnType: typeof(object));

            var sx = CSharpCompilation.CreateSubmission("sx.dll",
                                                  syntaxTree: SyntaxFactory.ParseSyntaxTree("var i = 1; dynamic d = null; d.m(ref i, ref i);", options: parseOptions),
                                                  previousSubmission: s0,
                                                  references: references,
                                                  returnType: typeof(object));

            var s1 = CSharpCompilation.CreateSubmission("s1.dll",
                                                  syntaxTree: SyntaxFactory.ParseSyntaxTree("var i = 1; dynamic d = null; d.m(out i);", options: parseOptions),
                                                  previousSubmission: s0,
                                                  references: references,
                                                  returnType: typeof(object));

            var diagnostics = s1.GetDiagnostics().ToArray();
            diagnostics.Verify();

            using (MemoryStream stream = new MemoryStream())
            {
                s1.Emit(stream);
            }

            // no new delegates should have been created:
            Assert.True(s1.AnonymousTypeManager.AreTemplatesSealed);
            Assert.Equal(0, s1.AnonymousTypeManager.GetAllCreatedTemplates().Length);

            // delegate for (ref)
            Assert.True(s0.AnonymousTypeManager.AreTemplatesSealed);
            Assert.Equal(1, s0.AnonymousTypeManager.GetAllCreatedTemplates().Length);

            Assert.False(sx.AnonymousTypeManager.AreTemplatesSealed);
        }

        [Fact]
        public void Submissions_EmitToPeStream()
        {
            var references = new[] { MetadataReference.CreateFromAssemblyInternal(typeof(object).Assembly) };

            CSharpCompilation s0 = CSharpCompilation.CreateSubmission("s0", syntaxTree: SyntaxFactory.ParseSyntaxTree("int a = 1;", options: TestOptions.Interactive), references: references, returnType: typeof(object));
            CSharpCompilation s11 = CSharpCompilation.CreateSubmission("s11", syntaxTree: SyntaxFactory.ParseSyntaxTree("a + 1", options: TestOptions.Interactive), previousSubmission: s0, references: references, returnType: typeof(object));
            CSharpCompilation s12 = CSharpCompilation.CreateSubmission("s12", syntaxTree: SyntaxFactory.ParseSyntaxTree("a + 2", options: TestOptions.Interactive), previousSubmission: s0, references: references, returnType: typeof(object));

            CompileAndVerify(s11);
            CompileAndVerify(s12);
        }

        /// <summary>
        /// Previous submission has to have no errors.
        /// </summary>
        [Fact]
        public void Submissions_ExecutionOrder3()
        {
            var references = new[] { MetadataReference.CreateFromAssemblyInternal(typeof(object).Assembly) };

            CSharpCompilation s0 = CSharpCompilation.CreateSubmission("s0.dll", syntaxTree: SyntaxFactory.ParseSyntaxTree("int a = \"x\";", options: TestOptions.Interactive), references: references, returnType: typeof(object));

            Assert.Throws<InvalidOperationException>(() =>
            {
                CSharpCompilation.CreateSubmission("s11.dll", syntaxTree: SyntaxFactory.ParseSyntaxTree("a + 1", options: TestOptions.Interactive), previousSubmission: s0, references: references, returnType: typeof(object));
            });
        }

        [Fact]
        public void SubmissionCompilation_Errors()
        {
            var genericParameter = typeof(List<>).GetGenericArguments()[0];
            var open = typeof(Dictionary<,>).MakeGenericType(typeof(int), genericParameter);
            var ptr = typeof(int).MakePointerType();
            var byref = typeof(int).MakeByRefType();

            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateSubmission("a", returnType: genericParameter));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateSubmission("a", returnType: open));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateSubmission("a", returnType: typeof(void)));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateSubmission("a", returnType: byref));

            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateSubmission("a", hostObjectType: genericParameter));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateSubmission("a", hostObjectType: open));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateSubmission("a", hostObjectType: typeof(void)));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateSubmission("a", hostObjectType: typeof(int)));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateSubmission("a", hostObjectType: ptr));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateSubmission("a", hostObjectType: byref));

            var s0 = CSharpCompilation.CreateSubmission("a0", hostObjectType: typeof(List<int>));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateSubmission("a1", previousSubmission: s0, hostObjectType: typeof(List<bool>)));

            // invalid options:
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateSubmission("a", options: TestOptions.ReleaseExe));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateSubmission("a", options: TestOptions.ReleaseDll.WithOutputKind(OutputKind.NetModule)));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateSubmission("a", options: TestOptions.ReleaseDll.WithOutputKind(OutputKind.WindowsRuntimeMetadata)));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateSubmission("a", options: TestOptions.ReleaseDll.WithOutputKind(OutputKind.WindowsRuntimeApplication)));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateSubmission("a", options: TestOptions.ReleaseDll.WithOutputKind(OutputKind.WindowsApplication)));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateSubmission("a", options: TestOptions.ReleaseDll.WithCryptoKeyContainer("foo")));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateSubmission("a", options: TestOptions.ReleaseDll.WithCryptoKeyFile("foo.snk")));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateSubmission("a", options: TestOptions.ReleaseDll.WithDelaySign(true)));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateSubmission("a", options: TestOptions.ReleaseDll.WithDelaySign(false)));
        }

        [WorkItem(3795, "https://github.com/dotnet/roslyn/issues/3795")]
        [Fact]
        public void ErrorInUsing()
        {
            var submission = CSharpCompilation.CreateSubmission("sub1", Parse("using Unknown;", options: TestOptions.Script), new[] { MscorlibRef });

            var expectedDiagnostics = new[]
            {
                    // (1,7): error CS0246: The type or namespace name 'Unknown' could not be found (are you missing a using directive or an assembly reference?)
                    // using Unknown;
                    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Unknown").WithArguments("Unknown").WithLocation(1, 7),
                    // (1,1): hidden CS8019: Unnecessary using directive.
                    // using Unknown;
                    Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using Unknown;").WithLocation(1, 1),
            };

            // Emit produces the same diagnostics as GetDiagnostics (below).
            using (var stream = new MemoryStream())
            {
                var emitResult = submission.Emit(stream);
                Assert.False(emitResult.Success);
                emitResult.Diagnostics.Verify(expectedDiagnostics);
            }

            submission.GetDiagnostics().Verify(expectedDiagnostics);
        }

        [WorkItem(3817, "https://github.com/dotnet/roslyn/issues/3817")]
        [Fact]
        public void LabelLookup()
        {
            const string source = "using System; 1";
            var tree = Parse(source, options: TestOptions.Script);
            var submission = CSharpCompilation.CreateSubmission("sub1", tree, new[] { MscorlibRef });
            var model = submission.GetSemanticModel(tree);
            Assert.Empty(model.LookupLabels(source.Length - 1)); // Used to assert.
        }

        private CSharpCompilation CreateSubmission(string code, CSharpParseOptions options, int expectedErrorCount = 0)
        {
            var submission = CSharpCompilation.CreateSubmission("sub",
                references: new[] { MetadataReference.CreateFromAssemblyInternal(typeof(object).Assembly) },
                syntaxTree: Parse(code, options: options));

            Assert.Equal(expectedErrorCount, submission.GetDiagnostics(CompilationStage.Declare, true, CancellationToken.None).Count());

            return submission;
        }

        private static void TestResult(CSharpCompilation s, SpecialType? expectedType, bool expectedHasValue)
        {
            bool hasValue;
            var type = s.GetSubmissionResultType(out hasValue);
            Assert.Equal(expectedType, type != null ? type.SpecialType : (SpecialType?)null);
            Assert.Equal(expectedHasValue, hasValue);
        }

        [Fact]
        public void SubmissionResultType()
        {
            var submission = CSharpCompilation.CreateSubmission("sub");
            bool hasValue;
            Assert.Equal(SpecialType.System_Void, submission.GetSubmissionResultType(out hasValue).SpecialType);
            Assert.False(hasValue);

            TestResult(CreateSubmission("1", TestOptions.Script, expectedErrorCount: 1), expectedType: SpecialType.System_Void, expectedHasValue: false);
            TestResult(CreateSubmission("1", TestOptions.Interactive), expectedType: SpecialType.System_Int32, expectedHasValue: true);
            TestResult(CreateSubmission("1;", TestOptions.Interactive), expectedType: SpecialType.System_Void, expectedHasValue: false);
            TestResult(CreateSubmission("void foo() { }", TestOptions.Interactive), expectedType: SpecialType.System_Void, expectedHasValue: false);
            TestResult(CreateSubmission("using System;", TestOptions.Interactive), expectedType: SpecialType.System_Void, expectedHasValue: false);
            TestResult(CreateSubmission("int i;", TestOptions.Interactive), expectedType: SpecialType.System_Void, expectedHasValue: false);
            TestResult(CreateSubmission("System.Console.WriteLine();", TestOptions.Interactive), expectedType: SpecialType.System_Void, expectedHasValue: false);
            TestResult(CreateSubmission("System.Console.WriteLine()", TestOptions.Interactive), expectedType: SpecialType.System_Void, expectedHasValue: true);
            TestResult(CreateSubmission("null", TestOptions.Interactive), expectedType: null, expectedHasValue: true);
            TestResult(CreateSubmission("System.Console.WriteLine", TestOptions.Interactive), expectedType: null, expectedHasValue: true);
        }

        [WorkItem(543890)]
        [Fact]
        public void ThisIndexerAccessInSubmission()
        {
            string test = @"
this[1]
";
            var compilation = CreateSubmission(test, TestOptions.Interactive);
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
        [WorkItem(530986)]
        [Fact]
        public void LookupSymbols()
        {
            var text = "1 + ";
            var compilation = CreateSubmission(text, TestOptions.Interactive, expectedErrorCount: 1);
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
                    Assert.NotEqual(type.TypeKind, TypeKind.Submission);
                }
            }

            // #1010871
            //Assert.False(symbols.Any(s => s.Name == "Roslyn"));
        }

        public class B
        {
            public int x = 1, w = 4;
        }

        [Fact]
        public void HostObjectBinding_Diagnostics()
        {
            var submission = CSharpCompilation.CreateSubmission("foo",
                syntaxTree: SyntaxFactory.ParseSyntaxTree("x", options: TestOptions.Interactive),
                references: new[]
                {
                    MscorlibRef,
                    MetadataReference.CreateFromAssemblyInternal(typeof(ScriptTests).Assembly)
                },
                hostObjectType: typeof(B));

            submission.VerifyDiagnostics();
        }

        [WorkItem(543370)]
        [Fact]
        public void CheckedDecimalAddition()
        {
            string source = @"
decimal d = checked(2M + 1M);
";

            var tree = SyntaxFactory.ParseSyntaxTree(source, options: TestOptions.Script);

            var compilation = CSharpCompilation.Create(
                "Test",
                new[] { tree },
                new[] { MscorlibRef, SystemRef },
                TestOptions.ReleaseExe
                    .WithScriptClassName("Foo.Script"));

            compilation.VerifyDiagnostics();
        }

        [WorkItem(543370)]
        [Fact]
        public void CheckedEnumAddition()
        {
            string source = @"
using System.IO;
FileAccess fa = checked(FileAccess.Read + 1);
";

            var tree = SyntaxFactory.ParseSyntaxTree(source, options: TestOptions.Script);

            var compilation = CSharpCompilation.Create(
                "Test",
                new[] { tree },
                new[] { MscorlibRef, SystemRef },
                TestOptions.ReleaseExe
                    .WithScriptClassName("Foo.Script"));

            compilation.VerifyDiagnostics();
        }

        [WorkItem(543370)]
        [Fact]
        public void DelegateAddition()
        {
            string source = @"
System.Action a = null;
a += null;
";

            var tree = SyntaxFactory.ParseSyntaxTree(source, options: TestOptions.Script);

            var compilation = CSharpCompilation.Create(
                "Test",
                new[] { tree },
                new[] { MscorlibRef, SystemRef },
                TestOptions.ReleaseExe
                    .WithScriptClassName("Foo.Script"));

            compilation.VerifyDiagnostics();
        }

        [WorkItem(870885)]
        [Fact]
        public void Bug870885()
        {
            var source = @"var o = o.F;";
            var syntaxTree = SyntaxFactory.ParseSyntaxTree(source, options: TestOptions.Interactive);
            var compilation = CSharpCompilation.CreateSubmission(
                GetUniqueName(),
                syntaxTree: syntaxTree,
                references: new MetadataReference[] { MscorlibRef, SystemCoreRef },
                returnType: typeof(object));
            compilation.VerifyDiagnostics(
                // (1,5): error CS7019: Type of 'o' cannot be inferred since its initializer directly or indirectly refers to the definition.
                // var o = o.F;
                Diagnostic(ErrorCode.ERR_RecursivelyTypedVariable, "o").WithArguments("o").WithLocation(1, 5));
        }
    }
}
