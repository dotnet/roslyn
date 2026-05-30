// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

#nullable disable

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    /// <summary>
    /// Tests for the "ref struct closures for lambdas" feature
    /// (https://github.com/dotnet/csharplang/issues/10209).
    ///
    /// A lambda expression is convertible to a generic type parameter that is
    /// constrained to one of the well-known <c>System.IFunc</c>/<c>System.IAction</c>
    /// function-interfaces and to <c>allows ref struct</c>. Rather than allocating a
    /// delegate, the compiler synthesizes a ref struct that implements the
    /// function-interface, with captured variables stored as (by-ref) fields.
    /// </summary>
    [CompilerTrait(CompilerFeature.RefLifetime)]
    public class RefStructClosureTests : CSharpTestBase
    {
        private static readonly TargetFramework s_targetFrameworkSupportingByRefLikeGenerics = TargetFramework.Net90;

        /// <summary>
        /// The well-known function-interfaces required by the feature. These do not yet
        /// exist in any shipping framework, so they are supplied in source. The compiler
        /// is expected to recognize them by their fully-qualified metadata names.
        /// </summary>
        private const string FunctionInterfacesDefinition = """
            namespace System
            {
                public interface IAction
                {
                    void Invoke();
                }

                public interface IAction<T> where T : allows ref struct
                {
                    void Invoke(T arg);
                }

                public interface IFunc<TResult> where TResult : allows ref struct
                {
                    TResult Invoke();
                }

                public interface IFunc<T, TResult>
                    where T : allows ref struct
                    where TResult : allows ref struct
                {
                    TResult Invoke(T arg);
                }
            }
            """;

        [Fact]
        public void WellKnownFunctionInterfaceTypesAreRecognized()
        {
            var comp = CreateCompilation(FunctionInterfacesDefinition, targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);
            comp.VerifyEmitDiagnostics();

            assertResolves(WellKnownType.System_IAction, "System.IAction");
            assertResolves(WellKnownType.System_IAction_T, "System.IAction<T>");
            assertResolves(WellKnownType.System_IFunc_T, "System.IFunc<TResult>");
            assertResolves(WellKnownType.System_IFunc_T2, "System.IFunc<T, TResult>");

            Assert.True(WellKnownType.System_IFunc_T.IsFunctionInterfaceType());
            Assert.True(WellKnownType.System_IFunc_TMax.IsFunctionInterfaceType());
            Assert.True(WellKnownType.System_IAction.IsFunctionInterfaceType());
            Assert.True(WellKnownType.System_IAction_TMax.IsFunctionInterfaceType());
            Assert.False(WellKnownType.System_Func_T.IsFunctionInterfaceType());
            Assert.False(WellKnownType.System_Action.IsFunctionInterfaceType());

            // IFunc`N has N-1 parameters; IAction`N has N parameters.
            Assert.Equal(WellKnownType.System_IFunc_T, WellKnownTypes.GetWellKnownFunctionInterface(invokeArgumentCount: 0));
            Assert.Equal(WellKnownType.System_IFunc_T2, WellKnownTypes.GetWellKnownFunctionInterface(invokeArgumentCount: 1));
            Assert.Equal(WellKnownType.System_IAction, WellKnownTypes.GetWellKnownActionInterface(invokeArgumentCount: 0));
            Assert.Equal(WellKnownType.System_IAction_T, WellKnownTypes.GetWellKnownActionInterface(invokeArgumentCount: 1));

            void assertResolves(WellKnownType wellKnownType, string expectedDisplay)
            {
                var symbol = comp.GetWellKnownType(wellKnownType);
                Assert.False(symbol.IsErrorType());
                Assert.Equal(expectedDisplay, symbol.ToTestDisplayString());
            }
        }

        [Fact]
        public void FunctionInterfaceConstraintIsRecognized()
        {
            var source = """
                class Program
                {
                    static void F0<T>(T t) where T : System.IAction, allows ref struct { }
                    static void F1<T>(T t) where T : System.IAction<int>, allows ref struct { }
                    static void G0<T>(T t) where T : System.IFunc<bool>, allows ref struct { }
                    static void G1<T>(T t) where T : System.IFunc<int, bool>, allows ref struct { }

                    // Not a function-interface: missing 'allows ref struct'.
                    static void H0<T>(T t) where T : System.IFunc<int, bool> { }
                    // Not a function-interface: a delegate, not one of the well-known interfaces.
                    static void H1<T>(T t) where T : allows ref struct { }
                    // Not a function-interface: a user interface of the same shape.
                    static void H2<T>(T t) where T : INotAFunc<int>, allows ref struct { }
                }

                interface INotAFunc<T> where T : allows ref struct
                {
                    bool Invoke(T arg);
                }
                """;
            var comp = CreateCompilation(
                new[] { source, FunctionInterfacesDefinition },
                targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);
            comp.VerifyEmitDiagnostics();

            var program = comp.GlobalNamespace.GetTypeMember("Program");

            assertFunctionInterface("F0", "System.IAction", "void System.IAction.Invoke()");
            assertFunctionInterface("F1", "System.IAction<System.Int32>", "void System.IAction<System.Int32>.Invoke(System.Int32 arg)");
            assertFunctionInterface("G0", "System.IFunc<System.Boolean>", "System.Boolean System.IFunc<System.Boolean>.Invoke()");
            assertFunctionInterface("G1", "System.IFunc<System.Int32, System.Boolean>", "System.Boolean System.IFunc<System.Int32, System.Boolean>.Invoke(System.Int32 arg)");

            assertNotFunctionInterface("H0");
            assertNotFunctionInterface("H1");
            assertNotFunctionInterface("H2");

            void assertFunctionInterface(string methodName, string expectedInterface, string expectedInvoke)
            {
                var typeParameter = (TypeParameterSymbol)((MethodSymbol)program.GetMember(methodName)).TypeParameters.Single();
                var functionInterface = typeParameter.GetFunctionInterfaceConstraint(comp, out var invokeMethod);
                Assert.NotNull(functionInterface);
                Assert.Equal(expectedInterface, functionInterface.ToTestDisplayString());
                Assert.NotNull(invokeMethod);
                Assert.Equal(expectedInvoke, invokeMethod.ToTestDisplayString());
            }

            void assertNotFunctionInterface(string methodName)
            {
                var typeParameter = (TypeParameterSymbol)((MethodSymbol)program.GetMember(methodName)).TypeParameters.Single();
                Assert.Null(typeParameter.GetFunctionInterfaceConstraint(comp, out var invokeMethod));
                Assert.Null(invokeMethod);
            }
        }

        [Fact]
        public void SpecExample()
        {
            var source = """
                using System;
                using System.Collections.Generic;

                class Program
                {
                    static void Main()
                    {
                        List<int> list = [1, 2, 3];
                        Console.Write(FirstOrNull(list, x => x % 2 == 0));
                    }

                    static int? FirstOrNull<TPred>(List<int> list, TPred pred)
                        where TPred : IFunc<int, bool>, allows ref struct
                    {
                        foreach (var item in list)
                        {
                            if (pred.Invoke(item))
                            {
                                return item;
                            }
                        }
                        return null;
                    }
                }
                """;
            var comp = CreateCompilation(
                new[] { source, FunctionInterfacesDefinition },
                targetFramework: s_targetFrameworkSupportingByRefLikeGenerics,
                options: TestOptions.ReleaseExe);
            CompileAndVerify(
                comp,
                expectedOutput: ExecutionConditionUtil.IsCoreClr ? "2" : null,
                verify: Verification.Skipped).VerifyDiagnostics();
        }

        [Fact]
        public void ExplicitlyTypedLambda()
        {
            var source = """
                using System;

                class Program
                {
                    static void Main()
                    {
                        Console.Write(Apply(21, (int x) => x * 2));
                    }

                    static int Apply<TFunc>(int value, TFunc func)
                        where TFunc : IFunc<int, int>, allows ref struct
                    {
                        return func.Invoke(value);
                    }
                }
                """;
            var comp = CreateCompilation(
                new[] { source, FunctionInterfacesDefinition },
                targetFramework: s_targetFrameworkSupportingByRefLikeGenerics,
                options: TestOptions.ReleaseExe);
            CompileAndVerify(
                comp,
                expectedOutput: ExecutionConditionUtil.IsCoreClr ? "42" : null,
                verify: Verification.Skipped).VerifyDiagnostics();
        }

        [Fact]
        public void Action_NoReturn()
        {
            var source = """
                using System;

                class Program
                {
                    static void Main()
                    {
                        Run(() => Console.Write("ran"));
                    }

                    static void Run<TAction>(TAction action)
                        where TAction : IAction, allows ref struct
                    {
                        action.Invoke();
                    }
                }
                """;
            var comp = CreateCompilation(
                new[] { source, FunctionInterfacesDefinition },
                targetFramework: s_targetFrameworkSupportingByRefLikeGenerics,
                options: TestOptions.ReleaseExe);
            CompileAndVerify(
                comp,
                expectedOutput: ExecutionConditionUtil.IsCoreClr ? "ran" : null,
                verify: Verification.Skipped).VerifyDiagnostics();
        }

        [Fact]
        public void CaptureLocalByRef()
        {
            // The synthesized closure must capture 'sum' by reference, so mutations
            // performed inside the lambda are observed by the enclosing method.
            var source = """
                using System;
                using System.Collections.Generic;

                class Program
                {
                    static void Main()
                    {
                        List<int> list = [1, 2, 3, 4];
                        int sum = 0;
                        ForEach(list, x => sum += x);
                        Console.Write(sum);
                    }

                    static void ForEach<TAction>(List<int> list, TAction action)
                        where TAction : IAction<int>, allows ref struct
                    {
                        foreach (var item in list)
                        {
                            action.Invoke(item);
                        }
                    }
                }
                """;
            var comp = CreateCompilation(
                new[] { source, FunctionInterfacesDefinition },
                targetFramework: s_targetFrameworkSupportingByRefLikeGenerics,
                options: TestOptions.ReleaseExe);
            CompileAndVerify(
                comp,
                expectedOutput: ExecutionConditionUtil.IsCoreClr ? "10" : null,
                verify: Verification.Skipped).VerifyDiagnostics();
        }

        [Fact]
        public void SynthesizedClosureIsRefStructImplementingInterface()
        {
            var source = """
                class Program
                {
                    static void Main()
                    {
                        M(x => x + 1);
                    }

                    static void M<TFunc>(TFunc func)
                        where TFunc : System.IFunc<int, int>, allows ref struct
                    {
                    }
                }
                """;
            var comp = CreateCompilation(
                new[] { source, FunctionInterfacesDefinition },
                targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);

            CompileAndVerify(comp, symbolValidator: module =>
            {
                var closure = module.GlobalNamespace
                    .GetTypeMembers()
                    .Concat(module.GlobalNamespace.GetTypeMember("Program").GetTypeMembers())
                    .Single(t => t.Name.Contains("DisplayClass") || t.Name.Contains("Closure"));

                Assert.True(closure.IsRefLikeType);
                Assert.Contains(closure.AllInterfaces(), i => i.Name == "IFunc");
                Assert.Contains(closure.GetMembers(), m => m.Name.Contains("Invoke"));
            }, verify: Verification.Skipped).VerifyDiagnostics();
        }

        [Fact]
        public void CaptureRefStructLocal()
        {
            // A captured ref struct (Span<int>) is only legal because the closure itself
            // is a ref struct.
            var source = """
                using System;

                class Program
                {
                    static void Main()
                    {
                        Span<int> span = [10, 20, 30];
                        Console.Write(Sum(span.Length, i => span[i]));
                    }

                    static int Sum<TFunc>(int count, TFunc func)
                        where TFunc : IFunc<int, int>, allows ref struct
                    {
                        int total = 0;
                        for (int i = 0; i < count; i++)
                        {
                            total += func.Invoke(i);
                        }
                        return total;
                    }
                }
                """;
            var comp = CreateCompilation(
                new[] { source, FunctionInterfacesDefinition },
                targetFramework: s_targetFrameworkSupportingByRefLikeGenerics,
                options: TestOptions.ReleaseExe);
            CompileAndVerify(
                comp,
                expectedOutput: ExecutionConditionUtil.IsCoreClr ? "60" : null,
                verify: Verification.Skipped).VerifyDiagnostics();
        }

        [Fact]
        public void CaptureMultipleLocals()
        {
            // Verifies that several distinct captures are wired independently and that
            // each ref field points back to the right enclosing local.
            var source = """
                using System;

                class Program
                {
                    static void Main()
                    {
                        int a = 1;
                        int b = 2;
                        int c = 3;
                        Run(() => { a *= 10; b *= 100; c *= 1000; });
                        Console.Write(a);
                        Console.Write('|');
                        Console.Write(b);
                        Console.Write('|');
                        Console.Write(c);
                    }

                    static void Run<TAction>(TAction action)
                        where TAction : IAction, allows ref struct
                    {
                        action.Invoke();
                    }
                }
                """;
            var comp = CreateCompilation(
                new[] { source, FunctionInterfacesDefinition },
                targetFramework: s_targetFrameworkSupportingByRefLikeGenerics,
                options: TestOptions.ReleaseExe);
            CompileAndVerify(
                comp,
                expectedOutput: ExecutionConditionUtil.IsCoreClr ? "10|200|3000" : null,
                verify: Verification.Skipped).VerifyDiagnostics();
        }

        [Fact]
        public void CaptureSeesEnclosingMutationsBetweenInvocations()
        {
            // Constructs the closure once, then mutates the captured local in the enclosing
            // method between invocations. Each invoke must observe the latest value because
            // the closure holds a ref, not a copy.
            var source = """
                using System;

                class Program
                {
                    static void Main()
                    {
                        int x = 1;
                        TwoCalls(() => Console.Write(x), ref x);
                    }

                    static void TwoCalls<TAction>(TAction action, ref int x)
                        where TAction : IAction, allows ref struct
                    {
                        action.Invoke();
                        x = 42;
                        action.Invoke();
                    }
                }
                """;
            var comp = CreateCompilation(
                new[] { source, FunctionInterfacesDefinition },
                targetFramework: s_targetFrameworkSupportingByRefLikeGenerics,
                options: TestOptions.ReleaseExe);
            CompileAndVerify(
                comp,
                expectedOutput: ExecutionConditionUtil.IsCoreClr ? "142" : null,
                verify: Verification.Skipped).VerifyDiagnostics();
        }

        [Fact]
        public void ClosureConstructionPreservesEvaluationOrder()
        {
            // The closure-construction sequence appears in the middle of an argument list
            // with side-effecting siblings. Arguments must evaluate strictly left-to-right:
            // before(), then capture-the-locals-and-build-closure, then after(), then call.
            var source = """
                using System;

                class Program
                {
                    static int Note(string s) { Console.Write(s); return 0; }

                    static void Main()
                    {
                        int sum = 0;
                        Apply(Note("[before]"), x => { sum += x; Console.Write("[invoke:"); Console.Write(sum); Console.Write(']'); }, Note("[after]"));
                    }

                    static void Apply<TAction>(int a, TAction action, int b)
                        where TAction : IAction<int>, allows ref struct
                    {
                        Console.Write("[call]");
                        action.Invoke(5);
                    }
                }
                """;
            var comp = CreateCompilation(
                new[] { source, FunctionInterfacesDefinition },
                targetFramework: s_targetFrameworkSupportingByRefLikeGenerics,
                options: TestOptions.ReleaseExe);
            CompileAndVerify(
                comp,
                expectedOutput: ExecutionConditionUtil.IsCoreClr ? "[before][after][call][invoke:5]" : null,
                verify: Verification.Skipped).VerifyDiagnostics();
        }

        [Fact]
        public void CaptureParameterByRef()
        {
            // Captures an outer method parameter (not a local). Mutations inside the lambda
            // must propagate back to the parameter slot in the enclosing method.
            var source = """
                using System;

                class Program
                {
                    static void Main()
                    {
                        int n = 10;
                        Bump(n);
                    }

                    static void Bump(int n)
                    {
                        Run(() => n += 5);
                        Console.Write(n);
                    }

                    static void Run<TAction>(TAction action)
                        where TAction : IAction, allows ref struct
                    {
                        action.Invoke();
                    }
                }
                """;
            var comp = CreateCompilation(
                new[] { source, FunctionInterfacesDefinition },
                targetFramework: s_targetFrameworkSupportingByRefLikeGenerics,
                options: TestOptions.ReleaseExe);
            CompileAndVerify(
                comp,
                expectedOutput: ExecutionConditionUtil.IsCoreClr ? "15" : null,
                verify: Verification.Skipped).VerifyDiagnostics();
        }

        [Fact]
        public void TypeInference_FromLambdaReturnAndParameters()
        {
            // TFunc itself must be inferred (fixed) to the synthesized ref struct type.
            var source = """
                using System;

                class Program
                {
                    static void Main()
                    {
                        Console.Write(Apply(21, x => x * 2));
                    }

                    static TResult Apply<TFunc, TResult>(int value, TFunc func)
                        where TFunc : IFunc<int, TResult>, allows ref struct
                        where TResult : allows ref struct
                    {
                        return func.Invoke(value);
                    }
                }
                """;
            var comp = CreateCompilation(
                new[] { source, FunctionInterfacesDefinition },
                targetFramework: s_targetFrameworkSupportingByRefLikeGenerics,
                options: TestOptions.ReleaseExe);
            CompileAndVerify(
                comp,
                expectedOutput: ExecutionConditionUtil.IsCoreClr ? "42" : null,
                verify: Verification.Skipped).VerifyDiagnostics();
        }

        [Fact]
        public void OverloadResolution_PrefersFunctionInterfaceWithPriority()
        {
            // With both a delegate and a function-interface overload present, the lambda
            // is applicable to both; [OverloadResolutionPriority] disambiguates.
            var source = """
                using System;
                using System.Runtime.CompilerServices;

                class Program
                {
                    static void Main()
                    {
                        M(x => x);
                    }

                    static void M(Func<int, int> f) => Console.Write("delegate");

                    [OverloadResolutionPriority(1)]
                    static void M<TFunc>(TFunc func)
                        where TFunc : IFunc<int, int>, allows ref struct
                        => Console.Write("function-interface");
                }
                """;
            var comp = CreateCompilation(
                new[] { source, FunctionInterfacesDefinition },
                targetFramework: s_targetFrameworkSupportingByRefLikeGenerics,
                options: TestOptions.ReleaseExe);
            CompileAndVerify(
                comp,
                expectedOutput: ExecutionConditionUtil.IsCoreClr ? "function-interface" : null,
                verify: Verification.Skipped).VerifyDiagnostics();
        }

        [Fact]
        public void OverloadResolution_AmbiguousWithoutPriority()
        {
            var source = """
                using System;

                class Program
                {
                    static void Main()
                    {
                        M(x => x);
                    }

                    static void M(Func<int, int> f) { }

                    static void M<TFunc>(TFunc func)
                        where TFunc : IFunc<int, int>, allows ref struct { }
                }
                """;
            var comp = CreateCompilation(
                new[] { source, FunctionInterfacesDefinition },
                targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);
            comp.VerifyDiagnostics(
                // (7,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.M(System.Func<int, int>)' and 'Program.M<TFunc>(TFunc)'
                //         M(x => x);
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("Program.M(System.Func<int, int>)", "Program.M<TFunc>(TFunc)").WithLocation(7, 9));
        }

        [Fact]
        public void LangVersion()
        {
            var source = """
                class Program
                {
                    static void Main()
                    {
                        M(x => x % 2 == 0);
                    }

                    static void M<TPred>(TPred pred)
                        where TPred : System.IFunc<int, bool>, allows ref struct
                    {
                    }
                }
                """;

            CreateCompilation(
                new[] { source, FunctionInterfacesDefinition },
                targetFramework: s_targetFrameworkSupportingByRefLikeGenerics,
                parseOptions: TestOptions.RegularNext).VerifyDiagnostics();

            CreateCompilation(
                new[] { source, FunctionInterfacesDefinition },
                targetFramework: s_targetFrameworkSupportingByRefLikeGenerics,
                parseOptions: TestOptions.Regular14).VerifyDiagnostics(
                // (5,11): error CS8652: The feature 'ref struct closures' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         M(x => x % 2 == 0);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "x => x % 2 == 0").WithArguments("ref struct closures").WithLocation(5, 11));
        }

        [Fact]
        public void RequiresByRefLikeGenericsRuntimeCapability()
        {
            // 'allows ref struct' requires a runtime that supports byref-like generics.
            // On a framework without that capability the constraint itself is an error,
            // so the lambda conversion cannot be used.
            var source = """
                namespace System
                {
                    public interface IFunc<T, TResult>
                    {
                        TResult Invoke(T arg);
                    }
                }

                class Program
                {
                    static void M<TPred>(TPred pred)
                        where TPred : System.IFunc<int, bool>, allows ref struct
                    {
                    }
                }
                """;
            var comp = CreateCompilation(
                source,
                targetFramework: TargetFramework.Net80);
            comp.VerifyDiagnostics(
                // (12,55): error CS9240: Target runtime doesn't support by-ref-like generics.
                //         where TPred : System.IFunc<int, bool>, allows ref struct
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportByRefLikeGenerics, "ref struct").WithLocation(12, 55));
        }

        [Fact]
        public void MissingWellKnownInterface_FallsBackToNoConversion()
        {
            // If the constraint interface is not one of the recognized function-interfaces,
            // there is no anonymous-function conversion and binding fails.
            var source = """
                interface INotAFunc<T> where T : allows ref struct
                {
                    bool Invoke(T arg);
                }

                class Program
                {
                    static void Main()
                    {
                        M(x => x % 2 == 0);
                    }

                    static void M<TPred>(TPred pred)
                        where TPred : INotAFunc<int>, allows ref struct
                    {
                    }
                }
                """;
            var comp = CreateCompilation(
                source,
                targetFramework: s_targetFrameworkSupportingByRefLikeGenerics);
            comp.VerifyDiagnostics(
                // (10,9): error CS0411: The type arguments for method 'Program.M<TPred>(TPred)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         M(x => x % 2 == 0);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M").WithArguments("Program.M<TPred>(TPred)").WithLocation(10, 9));
        }
    }
}
