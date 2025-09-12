// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.RemoveUnusedParametersAndValues;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using static Roslyn.Test.Utilities.TestHelpers;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveUnusedParametersAndValues;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpRemoveUnusedParametersAndValuesDiagnosticAnalyzer,
    CSharpRemoveUnusedValuesCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
public sealed class RemoveUnusedParametersTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
{
    public RemoveUnusedParametersTests(ITestOutputHelper logger)
      : base(logger)
    {
    }

    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new CSharpRemoveUnusedParametersAndValuesDiagnosticAnalyzer(), new CSharpRemoveUnusedValuesCodeFixProvider());

    private OptionsCollection NonPublicMethodsOnly
        => Option(CodeStyleOptions2.UnusedParameters,
            new CodeStyleOption2<UnusedParametersPreference>(UnusedParametersPreference.NonPublicMethods, NotificationOption2.Suggestion));

    // Ensure that we explicitly test missing UnusedParameterDiagnosticId, which has no corresponding code fix (non-fixable diagnostic).
    private static Task TestDiagnosticMissingAsync([StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string initialMarkup)
        => TestDiagnosticMissingAsync(initialMarkup, options: null);
    private static Task TestDiagnosticsAsync([StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string initialMarkup)
        => TestDiagnosticsAsync(initialMarkup, options: null);
    private static async Task TestDiagnosticMissingAsync([StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string initialMarkup, OptionsCollection? options)
    {
        var test = new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = initialMarkup,
            DisabledDiagnostics =
            {
                IDEDiagnosticIds.ExpressionValueIsUnusedDiagnosticId,
                IDEDiagnosticIds.ValueAssignedIsUnusedDiagnosticId,
            },
        };

        if (options is not null)
            test.Options.AddRange(options);

        await test.RunAsync();
    }

    private static async Task TestDiagnosticsAsync([StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string initialMarkup, OptionsCollection? options)
    {
        var test = new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = initialMarkup,
            DisabledDiagnostics =
            {
                IDEDiagnosticIds.ExpressionValueIsUnusedDiagnosticId,
                IDEDiagnosticIds.ValueAssignedIsUnusedDiagnosticId,
            },
        };

        if (options is not null)
            test.Options.AddRange(options);

        await test.RunAsync();
    }

    [Fact]
    public Task Parameter_Used()
        => TestDiagnosticMissingAsync(
            """
            class C
            {
                void M(int p)
                {
                    var x = p;
                }
            }
            """);

    [Fact]
    public Task Parameter_Unused()
        => TestDiagnosticsAsync(
            """
            class C
            {
                void M(int {|IDE0060:p|})
                {
                }
            }
            """);

    [Theory]
    [InlineData("public", "public")]
    [InlineData("public", "protected")]
    public Task Parameter_Unused_NonPrivate_NotApplicable(string typeAccessibility, string methodAccessibility)
        => TestDiagnosticMissingAsync(
            $$"""
            {{typeAccessibility}} class C
            {
                {{methodAccessibility}} void M(int p)
                {
                }
            }
            """, NonPublicMethodsOnly);

    [Theory]
    [InlineData("public", "private")]
    [InlineData("public", "internal")]
    [InlineData("internal", "private")]
    [InlineData("internal", "public")]
    [InlineData("internal", "internal")]
    [InlineData("internal", "protected")]
    public Task Parameter_Unused_NonPublicMethod(string typeAccessibility, string methodAccessibility)
        => TestDiagnosticsAsync(
            $$"""
            {{typeAccessibility}} class C
            {
                {{methodAccessibility}} void M(int {|IDE0060:p|})
                {
                }
            }
            """, NonPublicMethodsOnly);

    [Fact]
    public async Task Parameter_Unused_UnusedExpressionAssignment_PreferNone()
    {
        var unusedValueAssignmentOptionSuppressed = Option(CSharpCodeStyleOptions.UnusedValueAssignment,
            new CodeStyleOption2<UnusedValuePreference>(UnusedValuePreference.DiscardVariable, NotificationOption2.None));

        await TestDiagnosticMissingAsync(
            """
            class C
            {
                void M(int p)
                {
                    var x = p;
                }
            }
            """, options: unusedValueAssignmentOptionSuppressed);
    }

    [Fact]
    public Task Parameter_WrittenOnly()
        => TestDiagnosticsAsync(
            """
            class C
            {
                void M(int {|IDE0060:p|})
                {
                    p = 1;
                }
            }
            """);

    [Fact]
    public Task Parameter_WrittenThenRead()
        => TestDiagnosticsAsync(
            """
            class C
            {
                void M(int {|IDE0060:p|})
                {
                    p = 1;
                    var x = p;
                }
            }
            """);

    [Fact]
    public Task Parameter_WrittenOnAllControlPaths_BeforeRead()
        => TestDiagnosticsAsync(
            """
            class C
            {
                void M(int {|IDE0060:p|}, bool flag)
                {
                    if (flag)
                    {
                        p = 0;
                    }
                    else
                    {
                        p = 1;
                    }

                    var x = p;
                }
            }
            """);

    [Fact]
    public Task Parameter_WrittenOnSomeControlPaths_BeforeRead()
        => TestDiagnosticMissingAsync(
            """
            class C
            {
                void M(int p, bool flag, bool flag2)
                {
                    if (flag)
                    {
                        if (flag2)
                        {
                            p = 0;
                        }
                    }
                    else
                    {
                        p = 1;
                    }

                    var x = p;
                }
            }
            """);

    [Fact]
    public Task OptionalParameter_Unused()
        => TestDiagnosticsAsync(
            """
            class C
            {
                void M(int {|IDE0060:p|} = 0)
                {
                }
            }
            """);

    [Fact]
    public Task Parameter_UsedInConstructorInitializerOnly()
        => TestDiagnosticMissingAsync(
            """
            class B
            {
                protected B(int _) { }
            }

            class C: B
            {
                C(int p)
                : base(p)
                {
                }
            }
            """);

    [Fact]
    public Task Parameter_NotUsedInConstructorInitializer_UsedInConstructorBody()
        => TestDiagnosticMissingAsync(
            """
            class B
            {
                protected B(int _) { }
            }

            class C: B
            {
                C(int p)
                : base(0)
                {
                    var x = p;
                }
            }
            """);

    [Fact]
    public Task Parameter_UsedInConstructorInitializerAndConstructorBody()
        => TestDiagnosticMissingAsync(
            """
            class B
            {
                protected B(int _) { }
            }

            class C: B
            {
                C(int p)
                : base(p)
                {
                    var x = p;
                }
            }
            """);

    [Fact]
    public Task UnusedLocalFunctionParameter()
        => TestDiagnosticsAsync(
            """
            class C
            {
                void M(int y)
                {
                    LocalFunction(y);
                    void LocalFunction(int {|IDE0060:p|})
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task UnusedLocalFunctionParameter_02()
        => TestDiagnosticsAsync(
            """
            class C
            {
                void M()
                {
                    LocalFunction(0);
                    void LocalFunction(int {|IDE0060:p|})
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task UnusedLocalFunctionParameter_Discard()
        => TestDiagnosticMissingAsync(
            """
            class C
            {
                void M()
                {
                    LocalFunction(0);
                    void LocalFunction(int _)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task UnusedLocalFunctionParameter_PassedAsDelegateArgument()
        => TestDiagnosticMissingAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    M2(LocalFunction);
                    void LocalFunction(int p)
                    {
                    }
                }

                void M2(Action<int> a) => a(0);
            }
            """);

    [Fact]
    public Task UsedInLambda_ReturnsDelegate()
        => TestDiagnosticMissingAsync(
            """
            using System;

            class C
            {
                private static Action<int> M(object p, Action<object> myDelegate)
                {
                    return d => { myDelegate(p); };
                }
            }
            """);

    [Fact]
    public Task UnusedInLambda_ReturnsDelegate()
        => TestDiagnosticsAsync(
            """
            using System;

            class C
            {
                private static Action M(object {|IDE0060:p|})
                {
                    return () => { };
                }
            }
            """);

    [Fact]
    public Task UnusedInLambda_LambdaPassedAsArgument()
        => TestDiagnosticsAsync(
            """
            using System;

            class C
            {
                private static void M(object {|IDE0060:p|})
                {
                    M2(() => { });
                }

                private static void M2(Action _) { }
            }
            """);

    [Fact]
    public Task ReadInLambda_LambdaPassedAsArgument()
        => TestDiagnosticMissingAsync(
            """
            using System;

            class C
            {
                private static void M(object p)
                {
                    M2(() => { M3(p); });
                }

                private static void M2(Action _) { }

                private static void M3(object _) { }
            }
            """);

    [Fact]
    public Task OnlyWrittenInLambda_LambdaPassedAsArgument()
        => TestDiagnosticMissingAsync(
            """
            using System;

            class C
            {
                private static void M(object p)
                {
                    M2(() => { M3(out p); });
                }

                private static void M2(Action _) { }

                private static void M3(out object o) { o = null; }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31744")]
    public Task UnusedInExpressionTree_PassedAsArgument()
        => TestDiagnosticsAsync(
            """
            using System;
            using System.Linq.Expressions;

            class C
            {
                public static void M1(object {|IDE0060:p|})
                {
                    M2(x => x.M3());
                }

                private static C M2(Expression<Func<C, int>> _) { return null; }
                private int M3() { return 0; }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31744")]
    public Task ReadInExpressionTree_PassedAsArgument()
        => TestDiagnosticMissingAsync(
            """
            using System;
            using System.Linq.Expressions;

            class C
            {
                public static void M1(object p)
                {
                    M2(x => x.M3(p));
                }

                private static C M2(Expression<Func<C, int>> _) { return null; }
                private int M3(object _) { return 0; }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31744")]
    public Task OnlyWrittenInExpressionTree_PassedAsArgument()
        => TestDiagnosticMissingAsync(
            """
            using System;
            using System.Linq.Expressions;

            class C
            {
                public static void M1(object p)
                {
                    M2(x => x.M3(out p));
                }

                private static C M2(Expression<Func<C, int>> _) { return null; }
                private int M3(out object o) { o = null; return 0; }
            }
            """);

    [Fact]
    public Task UsedInLambda_AssignedToField()
        => TestDiagnosticMissingAsync(
            """
            using System;

            class C
            {
                private static Action _field;
                private static void M(object p)
                {
                    _field = () => { Console.WriteLine(p); };
                }
            }
            """);

    [Fact]
    public Task MethodWithLockAndControlFlow()
        => TestDiagnosticMissingAsync(
            """
            using System;

            class C
            {
                private static readonly object s_gate = new object();

                public static C M(object p, int flag, C c1, C c2)
                {
                    C c;
                    lock (s_gate)
                    {
                        c = flag > 0 ? c1 : c2;
                    }

                    c.M2(p);
                    return c;
                }

                private void M2(object _) { }
            }
            """);

    [Fact]
    public Task UnusedLambdaParameter()
        => TestDiagnosticMissingAsync(
            """
            using System;

            class C
            {
                void M(int y)
                {
                    Action<int> myLambda = p =>
                    {
                    };

                    myLambda(y);
                }
            }
            """);

    [Fact]
    public Task UnusedLambdaParameter_Discard()
        => TestDiagnosticMissingAsync(
            """
            using System;

            class C
            {
                void M(int y)
                {
                    Action<int> myLambda = _ =>
                    {
                    };

                    myLambda(y);
                }
            }
            """);

    [Fact]
    public Task UnusedLambdaParameter_DiscardTwo()
        => TestDiagnosticMissingAsync(
            """
            using System;

            class C
            {
                void M(int y)
                {
                    Action<int, int> myLambda = (_, _) =>
                    {
                    };

                    myLambda(y, y);
                }
            }
            """);

    [Fact]
    public Task UnusedLocalFunctionParameter_DiscardTwo()
        => TestDiagnosticMissingAsync(
            """
            using System;

            class C
            {
                void M(int y)
                {
                    void local(int _1, int _2)
                    {
                    }

                    local(y, y);
                }
            }
            """);

    [Fact]
    public Task UnusedMethodParameter_DiscardTwo()
        => TestDiagnosticMissingAsync(
            """
            using System;

            class C
            {
                void M(int _1, int _2)
                {
                }

                void M2(int y)
                {
                    M(y, y);
                }
            }
            """);

    [Fact]
    public Task UsedLocalFunctionParameter()
        => TestDiagnosticMissingAsync(
            """
            class C
            {
                void M(int y)
                {
                    LocalFunction(y);
                    void LocalFunction(int p)
                    {
                        var x = p;
                    }
                }
            }
            """);

    [Fact]
    public Task UsedLambdaParameter()
        => TestDiagnosticMissingAsync(
            """
            using System;

            class C
            {
                void M(int y)
                {
                    Action<int> myLambda = p =>
                    {
                        var x = p;
                    };

                    myLambda(y);
                }
            }
            """);

    [Fact]
    public Task OptionalParameter_Used()
        => TestDiagnosticMissingAsync(
            """
            class C
            {
                void M(int p = 0)
                {
                    var x = p;
                }
            }
            """);

    [Fact]
    public Task InParameter()
        => TestDiagnosticsAsync(
            """
            class C
            {
                void M(in int {|IDE0060:p|})
                {
                }
            }
            """);

    [Fact]
    public Task RefParameter_Unused()
        => TestDiagnosticsAsync(
            """
            class C
            {
                void M(ref int {|IDE0060:p|})
                {
                }
            }
            """);

    [Fact]
    public Task RefParameter_WrittenOnly()
        => TestDiagnosticMissingAsync(
            """
            class C
            {
                void M(ref int p)
                {
                    p = 0;
                }
            }
            """);

    [Fact]
    public Task RefParameter_ReadOnly()
        => TestDiagnosticMissingAsync(
            """
            class C
            {
                void M(ref int p)
                {
                    var x = p;
                }
            }
            """);

    [Fact]
    public Task RefParameter_ReadThenWritten()
        => TestDiagnosticMissingAsync(
            """
            class C
            {
                void M(ref int p)
                {
                    var x = p;
                    p = 1;
                }
            }
            """);

    [Fact]
    public Task RefParameter_WrittenAndThenRead()
        => TestDiagnosticMissingAsync(
            """
            class C
            {
                void M(ref int p)
                {
                    p = 1;
                    var x = p;
                }
            }
            """);

    [Fact]
    public Task RefParameter_WrittenTwiceNotRead()
        => TestDiagnosticMissingAsync(
            """
            class C
            {
                void M(ref int p)
                {
                    p = 0;
                    p = 1;
                }
            }
            """);

    [Fact]
    public Task OutParameter_Unused()
        => TestDiagnosticsAsync(
            """
            class C
            {
                void {|CS0177:M|}(out int {|IDE0060:p|})
                {
                }
            }
            """);

    [Fact]
    public Task OutParameter_WrittenOnly()
        => TestDiagnosticMissingAsync(
            """
            class C
            {
                void M(out int p)
                {
                    p = 0;
                }
            }
            """);

    [Fact]
    public Task OutParameter_WrittenAndThenRead()
        => TestDiagnosticMissingAsync(
            """
            class C
            {
                void M(out int p)
                {
                    p = 0;
                    var x = p;
                }
            }
            """);

    [Fact]
    public Task OutParameter_WrittenTwiceNotRead()
        => TestDiagnosticMissingAsync(
            """
            class C
            {
                void M(out int p)
                {
                    p = 0;
                    p = 1;
                }
            }
            """);

    [Fact]
    public Task Parameter_ExternMethod()
        => TestDiagnosticMissingAsync(
            """
            class C
            {
                [System.Runtime.InteropServices.DllImport(nameof(M))]
                static extern void M(int p);
            }
            """);

    [Fact]
    public Task Parameter_AbstractMethod()
        => TestDiagnosticMissingAsync(
            """
            abstract class C
            {
                protected abstract void M(int p);
            }
            """);

    [Fact]
    public Task Parameter_VirtualMethod()
        => TestDiagnosticMissingAsync(
            """
            class C
            {
                protected virtual void M(int p)
                {
                }
            }
            """);

    [Fact]
    public Task Parameter_OverriddenMethod()
        => TestDiagnosticMissingAsync(
            """
            class C
            {
                protected virtual void M(int p)
                {
                    var x = p;
                }
            }

            class D : C
            {
                protected override void M(int p)
                {
                }
            }
            """);

    [Fact]
    public Task Parameter_ImplicitInterfaceImplementationMethod()
        => TestDiagnosticMissingAsync(
            """
            interface I
            {
                void M(int p);
            }
            class C: I
            {
                public void M(int p)
                {
                }
            }
            """);

    [Fact]
    public Task Parameter_ExplicitInterfaceImplementationMethod()
        => TestDiagnosticMissingAsync(
            """
            interface I
            {
                void M(int p);
            }
            class C: I
            {
                void I.M(int p)
                {
                }
            }
            """);

    [Fact]
    public Task Parameter_IndexerMethod()
        => TestDiagnosticMissingAsync(
            """
            class C
            {
                int this[int p]
                {
                    get { return 0; }
                }
            }
            """);

    [Fact]
    public Task Parameter_ConditionalDirective()
        => TestDiagnosticMissingAsync(
            """
            class C
            {
                void M(int p)
                {
            #if DEBUG
                    System.Console.WriteLine(p);
            #endif
                }
            }
            """);

    [Fact]
    public Task Parameter_EventHandler_FirstParameter()
        => TestDiagnosticMissingAsync(
            """
            class C
            {
                public void MyHandler(object obj, System.EventArgs args)
                {
                }
            }
            """);

    [Fact]
    public Task Parameter_EventHandler_SecondParameter()
        => TestDiagnosticMissingAsync(
            """
            class C
            {
                public void MyHandler(object obj, System.EventArgs args)
                {
                }
            }
            """);

    [Fact]
    public Task Parameter_MethodUsedAsEventHandler()
        => TestDiagnosticMissingAsync(
            """
            using System;

            public delegate void MyDelegate(int x);

            class C
            {
                private event MyDelegate myDel;

                void M(C c)
                {
                    c.myDel += Handler;
                }

                void Handler(int x)
                {
                }
            }
            """);

    [Fact]
    public Task Parameter_CustomEventArgs()
        => TestDiagnosticMissingAsync(
            """
            class C
            {
                public class CustomEventArgs : System.EventArgs
                {
                }

                public void MyHandler(object obj, CustomEventArgs args)
                {
                }
            }
            """);

    [Theory]
    [InlineData(@"[System.Diagnostics.Conditional(nameof(M))]")]
    [InlineData(@"[System.Obsolete]")]
    [InlineData(@"[System.Runtime.Serialization.OnDeserializingAttribute]")]
    [InlineData(@"[System.Runtime.Serialization.OnDeserializedAttribute]")]
    [InlineData(@"[System.Runtime.Serialization.OnSerializingAttribute]")]
    [InlineData(@"[System.Runtime.Serialization.OnSerializedAttribute]")]
    public Task Parameter_MethodsWithSpecialAttributes(string attribute)
        => TestDiagnosticMissingAsync(
            $$"""
            class C
            {
                {{attribute}}
                void M(int p)
                {
                }
            }
            """);

    [Theory]
    [InlineData("System.Composition", "ImportingConstructorAttribute")]
    [InlineData("System.ComponentModel.Composition", "ImportingConstructorAttribute")]
    public Task Parameter_ConstructorsWithSpecialAttributes(string attributeNamespace, string attributeName)
        => TestDiagnosticMissingAsync(
            $$"""
            namespace {{attributeNamespace}}
            {
                public class {{attributeName}} : System.Attribute { }
            }

            class C
            {
                [{{attributeNamespace}}.{{attributeName}}()]
                public C(int p)
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32133")]
    public Task Parameter_SerializationConstructor()
        => TestDiagnosticMissingAsync(
            """
            using System;
            using System.Runtime.Serialization;

            internal sealed class NonSerializable
            {
                public NonSerializable(string value) => Value = value;

                public string Value { get; set; }
            }

            [Serializable]
            internal sealed class CustomSerializingType : ISerializable
            {
                private readonly NonSerializable _nonSerializable;

                public CustomSerializingType(SerializationInfo info, StreamingContext context)
                {
                    _nonSerializable = new NonSerializable(info.GetString("KEY"));
                }

                public void GetObjectData(SerializationInfo info, StreamingContext context)
                {
                    info.AddValue("KEY", _nonSerializable.Value);
                }
            }
            """);

    [ConditionalFact(typeof(IsEnglishLocal))]
    public async Task Parameter_DiagnosticMessages()
    {
        var source =
            """
            public class C
            {
                // p1 is unused.
                // p2 is written before read.
                [|int M(int p1, int p2)
                {
                    p2 = 0;
                    return p2;
                }

                // p3 is unused parameter of a public API.
                // p4 is written before read parameter of a public API.
                public int M2(int p3, int p4)
                {
                    p4 = 0;
                    return p4;
                }

                void M3(int p5)
                {
                    _ = nameof(p5);
                }|]
            }
            """;
        var testParameters = new TestParameters(retainNonFixableDiagnostics: true);
        using var workspace = CreateWorkspaceFromOptions(source, testParameters);
        var diagnostics = await GetDiagnosticsAsync(workspace, testParameters).ConfigureAwait(false);
        diagnostics.Verify(
            Diagnostic(IDEDiagnosticIds.UnusedParameterDiagnosticId, "p1").WithLocation(5, 15),
            Diagnostic(IDEDiagnosticIds.UnusedParameterDiagnosticId, "p2").WithLocation(5, 23),
            Diagnostic(IDEDiagnosticIds.UnusedParameterDiagnosticId, "p3").WithLocation(13, 23),
            Diagnostic(IDEDiagnosticIds.UnusedParameterDiagnosticId, "p4").WithLocation(13, 31),
            Diagnostic(IDEDiagnosticIds.UnusedParameterDiagnosticId, "p5").WithLocation(19, 17));
        var sortedDiagnostics = diagnostics.OrderBy(d => d.Location.SourceSpan.Start).ToArray();

        Assert.Equal("Remove unused parameter 'p1'", sortedDiagnostics[0].GetMessage());
        Assert.Equal("Parameter 'p2' can be removed; its initial value is never used", sortedDiagnostics[1].GetMessage());
        Assert.Equal("Remove unused parameter 'p3' if it is not part of a shipped public API", sortedDiagnostics[2].GetMessage());
        Assert.Equal("Parameter 'p4' can be removed if it is not part of a shipped public API; its initial value is never used", sortedDiagnostics[3].GetMessage());
        Assert.Equal("Parameter 'p5' can be removed; its initial value is never used", sortedDiagnostics[4].GetMessage());
    }

    [Theory]
    [InlineData("int[]")]
    [InlineData("Span<int>")]
    public Task Parameter_ArrayLikeUsedForReading(string arrayLikeType)
        => TestDiagnosticMissingAsync(
            $$"""
            using System;
            class C
            {
                void M({{arrayLikeType}} p)
                {
                    var x = p[0];
                }
            }
            """);

    [Theory]
    [InlineData("int[]")]
    [InlineData("Span<int>")]
    public Task Parameter_ArrayLikeUsedForWriting(string arrayLikeType)
        => TestDiagnosticMissingAsync(
            $$"""
            using System;
            class C
            {
                void M({{arrayLikeType}} p)
                {
                    p[0] = new();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32287")]
    public Task Parameter_DeclarationPatternWithNullDeclaredSymbol()
        => TestDiagnosticMissingAsync(
            """
            class C
            {
                void M(object o)
                {
                    if (o is int _)
                    {
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32851")]
    public Task Parameter_Unused_SpecialNames()
        => TestDiagnosticMissingAsync(
            """
            class C
            {
                void M(int _, char _1, C _3)
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32851")]
    public Task Parameter_Used_SemanticError()
        => TestDiagnosticMissingAsync(
            """
            class C
            {
                void M(int x)
                {
                    // CS1662: Cannot convert lambda expression to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type.
                    Invoke<string>(() => x);

                    T Invoke<T>({|CS0246:Func<T>|} a) { return a(); }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32851")]
    public Task Parameter_Unused_SemanticError()
        => TestDiagnosticsAsync(
            """
            class C
            {
                void M(int {|IDE0060:x|})
                {
                    // CS1662: Cannot convert lambda expression to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type.
                    Invoke<string>(() => 0);

                    T Invoke<T>({|CS0246:Func<T>|} a) { return a(); }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32973")]
    public Task OutParameter_LocalFunction()
        => TestDiagnosticMissingAsync(
            """
            class C
            {
                public static bool M(out int x)
                {
                    return LocalFunction(out x);

                    bool LocalFunction(out int y)
                    {
                        y = 0;
                        return true;
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32973")]
    public Task RefParameter_Unused_LocalFunction()
        => TestDiagnosticsAsync(
            """
            class C
            {
                public static bool M(ref int x)
                {
                    return LocalFunction(ref x);

                    bool LocalFunction(ref int {|IDE0060:y|})
                    {
                        return true;
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32973")]
    public Task RefParameter_Used_LocalFunction()
        => TestDiagnosticMissingAsync(
            """
            class C
            {
                public static bool M(ref int x)
                {
                    return LocalFunction(ref x);

                    bool LocalFunction(ref int y)
                    {
                        y = 0;
                        return true;
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33299")]
    public Task NullCoalesceAssignment()
        => TestDiagnosticMissingAsync(
            """
            class C
            {
                public static void M(C x)
                {
                    x ??= new C();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/34301")]
    public Task GenericLocalFunction()
        => TestDiagnosticsAsync(
            """
            class C
            {
                void M()
                {
                    LocalFunc(0);

                    void LocalFunc<T>(T {|IDE0060:value|})
                    {
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36715")]
    public Task GenericLocalFunction_02()
        => TestDiagnosticsAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M(object {|IDE0060:value|})
                {
                    try
                    {
                        value = LocalFunc(0);
                    }
                    finally
                    {
                        value = LocalFunc(0);
                    }

                    return;

                    IEnumerable<T> LocalFunc<T>(T value)
                    {
                        yield return value;
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36715")]
    public Task GenericLocalFunction_03()
        => TestDiagnosticMissingAsync(
            """
            using System;
            using System.Collections.Generic;

            class C
            {
                void M(object value)
                {
                    Func<object, IEnumerable<object>> myDel = LocalFunc;
                    try
                    {
                        value = myDel(value);
                    }
                    finally
                    {
                        value = myDel(value);
                    }

                    return;

                    IEnumerable<T> LocalFunc<T>(T value)
                    {
                        yield return value;
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/34830")]
    public async Task RegressionTest_ShouldReportUnusedParameter()
    {
        var options = Option(CodeStyleOptions2.UnusedParameters,
            new CodeStyleOption2<UnusedParametersPreference>(default, NotificationOption2.Suggestion));

        await TestDiagnosticMissingAsync(
            """
            using System;
            using System.Threading.Tasks;

            public interface I { event Action MyAction; }

            public sealed class C : IDisposable
            {
                private readonly Task<I> task;

                public C(Task<I> task)
                {
                    this.task = task;
                    Task.Run(async () => (await task).MyAction += myAction);
                }

                private void myAction() { }

                public void Dispose() => task.Result.MyAction -= myAction;
            }
            """, options);
    }

#if !CODE_STYLE // Below test is not applicable for CodeStyle layer as attempting to fetch an editorconfig string representation for this invalid option fails.
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37326")]
    public async Task RegressionTest_ShouldReportUnusedParameter_02()
    {
        var options = Option(CodeStyleOptions2.UnusedParameters,
            new CodeStyleOption2<UnusedParametersPreference>((UnusedParametersPreference)2, NotificationOption2.Suggestion));

        var parameters = new TestParameters(globalOptions: options, retainNonFixableDiagnostics: true);

        await TestDiagnosticMissingAsync(
            """
            using System;
            using System.Threading.Tasks;

            public interface I { event Action MyAction; }

            public sealed class C : IDisposable
            {
                private readonly Task<I> task;

                public C(Task<I> [|task|])
                {
                    this.task = task;
                    Task.Run(async () => (await task).MyAction += myAction);
                }

                private void myAction() { }

                public void Dispose() => task.Result.MyAction -= myAction;
            }
            """, parameters);
    }
#endif

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37483")]
    public Task MethodUsedAsDelegateInGeneratedCode_NoDiagnostic()
        => TestDiagnosticMissingAsync(
            """
            using System;

            public partial class C
            {
                private void M(int x)
                {
                }
            }

            public partial class C
            {
                [System.CodeDom.Compiler.GeneratedCodeAttribute("", "")]
                public void M2(out Action<int> a)
                {
                    a = M;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37483")]
    public Task UnusedParameterInGeneratedCode_NoDiagnostic()
        => TestDiagnosticMissingAsync(
            """
            public partial class C
            {
                [System.CodeDom.Compiler.GeneratedCodeAttribute("", "")]
                private void M(int x)
                {
                }
            }
            """);

    [WorkItem("https://github.com/dotnet/roslyn/issues/57814")]
    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
    public Task UnusedParameterInPartialMethodImplementation_NoDiagnostic()
        => TestDiagnosticMissingAsync(
            """
            public partial class C
            {
                public partial void M(int x);
            }

            public partial class C
            {
                public partial void M(int x)
                {
                }
            }
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)]
    public Task ParameterInPartialMethodDefinition_NoDiagnostic()
        => TestDiagnosticMissingAsync(
            """
            public partial class C
            {
                partial void M(int x);
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36817")]
    public Task ParameterWithoutName_NoDiagnostic()
        => TestDiagnosticMissingAsync(
            """
            public class C
            {
                public void M(int {|CS1001:)|}
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41236")]
    public Task NotImplementedException_NoDiagnostic1()
        => TestDiagnosticMissingAsync(
            """
            using System;

            class C
            {
                private void Goo(int i)
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41236")]
    public Task NotImplementedException_NoDiagnostic2()
        => TestDiagnosticMissingAsync(
            """
            using System;

            class C
            {
                private void Goo(int i)
                    => throw new NotImplementedException();
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41236")]
    public Task NotImplementedException_NoDiagnostic3()
        => TestDiagnosticMissingAsync(
            """
            using System;

            class C
            {
                public C(int i)
                    => throw new NotImplementedException();
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56317")]
    public Task NotImplementedException_NoDiagnostic4()
        => TestDiagnosticMissingAsync(
            """
            using System;

            class C
            {
                private int Goo(int i)
                    => throw new NotImplementedException();
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56317")]
    public Task NotImplementedException_NoDiagnostic5()
        => TestDiagnosticMissingAsync(
            """
            using System;

            class C
            {
                private int Goo(int i)
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41236")]
    public Task NotImplementedException_MultipleStatements1()
        => TestDiagnosticsAsync(
            """
            using System;

            class C
            {
                private void Goo(int {|IDE0060:i|})
                {
                    throw new NotImplementedException();
                    return;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41236")]
    public Task NotImplementedException_MultipleStatements2()
        => TestDiagnosticsAsync(
            """
            using System;

            class C
            {
                private void Goo(int {|IDE0060:i|})
                {
                    if (true)
                        throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47142")]
    public Task Record_PrimaryConstructorParameter()
        => TestMissingAsync(
            @"record A(int [|X|]);");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47142")]
    public Task Record_NonPrimaryConstructorParameter()
        => TestDiagnosticsAsync(
            """
            record A
            {
                public A(int {|IDE0060:X|})
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47142")]
    public Task Record_DelegatingPrimaryConstructorParameter()
        => TestDiagnosticMissingAsync(
            """
            record A(int X);
            record B(int X, int Y) : A(X);
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47174")]
    public Task RecordPrimaryConstructorParameter_PublicRecord()
        => TestDiagnosticMissingAsync(
            """
            public record Base(int I) { }
            public record Derived(string S) : Base(42) { }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/45743")]
    public Task RequiredGetInstanceMethodByICustomMarshaler()
        => TestDiagnosticMissingAsync("""
            using System;
            using System.Runtime.InteropServices;


            public class C : ICustomMarshaler
            {
                public void CleanUpManagedData(object ManagedObj)
                    => throw new NotImplementedException();

                public void CleanUpNativeData(IntPtr pNativeData)
                    => throw new NotImplementedException();

                public int GetNativeDataSize()
                    => throw new NotImplementedException();

                public IntPtr MarshalManagedToNative(object ManagedObj)
                    => throw new NotImplementedException();

                public object MarshalNativeToManaged(IntPtr pNativeData)
                    => throw new NotImplementedException();

                public static ICustomMarshaler GetInstance(string s)
                    => null;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/65275")]
    public Task TestMethodWithUnusedParameterThrowsExpressionBody()
        => TestDiagnosticMissingAsync(
            """
            public class Class
            {
                public void Method(int x) => throw new System.Exception();
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/65275")]
    public Task TestMethodWithUnusedParameterThrowsMethodBody()
        => TestDiagnosticMissingAsync(
            """
            public class Class
            {
                public void Method(int x)
                {
                    throw new System.Exception();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/65275")]
    public Task TestMethodWithUnusedParameterThrowsConstructorBody()
        => TestDiagnosticMissingAsync(
            """
            public class Class
            {
                public Class(int x)
                {
                    throw new System.Exception();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/65275")]
    public Task TestMethodWithUnusedParameterThrowsConstructorExpressionBody()
        => TestDiagnosticMissingAsync(
            """
            public class Class
            {
                public Class(int x) => throw new System.Exception();
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/65275")]
    public Task TestMethodWithUnusedParameterThrowsLocalFunctionExpressionBody()
        => TestDiagnosticMissingAsync(
            """
            public class Class
            {
                public void Method()
                {
                    void LocalMethod(int x) => throw new System.Exception();
                }
            }
            """);

    [Fact, WorkItem(67013, "https://github.com/dotnet/roslyn/issues/67013")]
    public Task Test_PrimaryConstructor1()
        => TestDiagnosticMissingAsync(
            """
            using System;

            class C(int a100)
            {
            }
            """);

    [Fact, WorkItem(67013, "https://github.com/dotnet/roslyn/issues/67013")]
    public Task Test_PrimaryConstructor2()
        => TestDiagnosticMissingAsync(
            """
            using System;

            class C(int a100) : Object()
            {
                int M1() => a100;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70276")]
    public Task TestMethodWithNameOf()
        => TestDiagnosticsAsync("""
            class C
            {
                void M(int {|IDE0060:x|})
                {
                    const string y = nameof(C);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58168")]
    public Task TestInterpolatedStringHandler_TwoIntParameters_FirstParameter()
        => TestDiagnosticMissingAsync("""
            using System.Runtime.CompilerServices;

            [InterpolatedStringHandler]
            public struct MyInterpolatedStringHandler
            {
                public MyInterpolatedStringHandler(int literalLength, int formattedCount)
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58168")]
    public Task TestInterpolatedStringHandler_TwoIntParameters_SecondParameter()
        => TestDiagnosticMissingAsync("""
            using System.Runtime.CompilerServices;

            [InterpolatedStringHandler]
            public struct MyInterpolatedStringHandler
            {
                public MyInterpolatedStringHandler(int literalLength, int formattedCount)
                {
                }
            }
            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/58168")]
    [MemberData(nameof(NonIntTypes))]
    public Task TestInterpolatedStringHandler_TwoParameters_FirstNonIntParameter(string nonIntType)
        => TestDiagnosticsAsync($$"""
            using System.Runtime.CompilerServices;

            [InterpolatedStringHandler]
            public struct MyInterpolatedStringHandler
            {
                public MyInterpolatedStringHandler({{nonIntType}} {|IDE0060:literalLength|}, int formattedCount)
                {
                }
            }
            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/58168")]
    [MemberData(nameof(NonIntTypes))]
    public Task TestInterpolatedStringHandler_TwoParameters_SecondNonIntParameter(string nonIntType)
        => TestDiagnosticsAsync($$"""
            using System.Runtime.CompilerServices;

            [InterpolatedStringHandler]
            public struct MyInterpolatedStringHandler
            {
                public MyInterpolatedStringHandler(int literalLength, {{nonIntType}} {|IDE0060:formattedCount|})
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58168")]
    public Task TestInterpolatedStringHandler_OneIntParameter()
        => TestDiagnosticMissingAsync("""
            using System.Runtime.CompilerServices;

            [InterpolatedStringHandler]
            public struct MyInterpolatedStringHandler
            {
                public MyInterpolatedStringHandler(int literalLength)
                {
                }
            }
            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/58168")]
    [MemberData(nameof(NonIntTypes))]
    public Task TestInterpolatedStringHandler_OneNonIntParameter(string nonIntType)
        => TestDiagnosticsAsync($$"""
            using System.Runtime.CompilerServices;

            [InterpolatedStringHandler]
            public struct MyInterpolatedStringHandler
            {
                public MyInterpolatedStringHandler({{nonIntType}} {|IDE0060:p|})
                {
                }
            }
            """);

    public static IEnumerable<object[]> NonIntTypes()
    {
        yield return ["byte"];
        yield return ["long"];
        yield return ["object"];
        yield return ["string"];
    }
}
