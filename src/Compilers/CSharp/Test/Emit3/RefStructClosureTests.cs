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
                Assert.Contains(closure.GetMembers(), m => m.Name == "Invoke");
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
                // (8,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.M(System.Func<int, int>)' and 'Program.M<TFunc>(TFunc)'
                //         M(x => x);
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("Program.M(System.Func<int, int>)", "Program.M<TFunc>(TFunc)").WithLocation(8, 9));
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
