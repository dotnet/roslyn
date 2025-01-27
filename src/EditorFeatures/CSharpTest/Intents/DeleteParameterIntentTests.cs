// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Features.Intents;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Intents;

[UseExportProvider]
public class DeleteParameterIntentTests : IntentTestsBase
{
    [Fact]
    public async Task TestDeleteParameterIntentAsync()
    {
        var initialText =
            """
            class C
            {
                void M()
                {
                    Method({|priorSelection:1|});
                }

                void Method(int value)
                {
                }
            }
            """;
        var currentText =
            """
            class C
            {
                void M()
                {
                    Method();
                }

                void Method(int value)
                {
                }
            }
            """;
        var expectedText =
            """
            class C
            {
                void M()
                {
                    Method();
                }

                void Method()
                {
                }
            }
            """;

        await VerifyExpectedTextAsync(WellKnownIntents.DeleteParameter, initialText, currentText, expectedText).ConfigureAwait(false);
    }

    [Fact]
    public async Task TestDeleteParameterOnDefinitionIntentAsync()
    {
        var initialText =
            """
            class C
            {
                void M()
                {
                    Method(1);
                }

                void Method(int {|priorSelection:value|})
                {
                }
            }
            """;
        var currentText =
            """
            class C
            {
                void M()
                {
                    Method();
                }

                void Method(int value)
                {
                }
            }
            """;
        var expectedText =
            """
            class C
            {
                void M()
                {
                    Method();
                }

                void Method()
                {
                }
            }
            """;

        await VerifyExpectedTextAsync(WellKnownIntents.DeleteParameter, initialText, currentText, expectedText).ConfigureAwait(false);
    }

    [Fact]
    public async Task TestDeleteSecondParameterIntentAsync()
    {
        var initialText = """
            class C
            {
                void M()
                {
                    Method(1, {|priorSelection:2|}, 3);
                }

                void Method(int value1, int value2, int value3)
                {
                }
            }
            """;
        var currentText =
            """
            class C
            {
                void M()
                {
                    Method(1, 3);
                }

                void Method(int value1, int value2, int value3)
                {
                }
            }
            """;
        var expectedText =
            """
            class C
            {
                void M()
                {
                    Method(1, 3);
                }

                void Method(int value1, int value3)
                {
                }
            }
            """;

        await VerifyExpectedTextAsync(WellKnownIntents.DeleteParameter, initialText, currentText, expectedText).ConfigureAwait(false);
    }

    [Fact]
    public async Task TestDeleteLastParameterAsync()
    {
        var initialText = """
            class C
            {
                void M()
                {
                    Method(1, 2, {|priorSelection:3|});
                }

                void Method(int value1, int value2, int value3)
                {
                }
            }
            """;
        var currentText =
            """
            class C
            {
                void M()
                {
                    Method(1, 2);
                }

                void Method(int value1, int value2, int value3)
                {
                }
            }
            """;
        var expectedText =
            """
            class C
            {
                void M()
                {
                    Method(1, 2);
                }

                void Method(int value1, int value2)
                {
                }
            }
            """;

        await VerifyExpectedTextAsync(WellKnownIntents.DeleteParameter, initialText, currentText, expectedText).ConfigureAwait(false);
    }

    [Fact]
    public async Task TestDeleteThisParameterAsync()
    {
        var initialText = """
            public class Foo
            {
                static void Bar()
                {
                    var f = new Foo();
                    f.DoFoo();
                }
            }

            public static class FooExtensions
            {
                public static void DoFoo(this {|priorSelection:Foo|} foo)
                {

                }
            }
            """;
        var currentText =
            """
            public class Foo
            {
                static void Bar()
                {
                    var f = new Foo();
                    f.DoFoo();
                }
            }

            public static class FooExtensions
            {
                public static void DoFoo()
                {

                }
            }
            """;

        await VerifyIntentMissingAsync(WellKnownIntents.DeleteParameter, initialText, currentText).ConfigureAwait(false);
    }

    [Fact]
    public async Task TestDeleteParameterInExtensionMethodAsync()
    {
        var initialText = """
            public class Foo
            {
                static void Bar()
                {
                    var f = new Foo();
                    f.DoFoo({|priorSelection:1|}, 2);
                }
            }

            public static class FooExtensions
            {
                public static void DoFoo(this Foo foo, int value1, int value2)
                {

                }
            }
            """;
        var currentText =
            """
            public class Foo
            {
                static void Bar()
                {
                    var f = new Foo();
                    f.DoFoo(2);
                }
            }

            public static class FooExtensions
            {
                public static void DoFoo(this Foo foo, int value1, int value2)
                {

                }
            }
            """;
        var expectedText =
            """
            public class Foo
            {
                static void Bar()
                {
                    var f = new Foo();
                    f.DoFoo(2);
                }
            }

            public static class FooExtensions
            {
                public static void DoFoo(this Foo foo, int value2)
                {

                }
            }
            """;

        await VerifyExpectedTextAsync(WellKnownIntents.DeleteParameter, initialText, currentText, expectedText).ConfigureAwait(false);
    }

    [Fact]
    public async Task TestDeleteParameterOnDefinitionAsync()
    {
        var initialText = """
            public class Foo
            {
                static void Bar()
                {
                    var f = new Foo();
                    f.DoFoo(1, 2);
                }
            }

            public static class FooExtensions
            {
                public static void DoFoo(this Foo foo, int {|priorSelection:value1|}, int value2)
                {

                }
            }
            """;
        var currentText =
            """
            public class Foo
            {
                static void Bar()
                {
                    var f = new Foo();
                    f.DoFoo(2);
                }
            }

            public static class FooExtensions
            {
                public static void DoFoo(this Foo foo, int value1, int value2)
                {

                }
            }
            """;
        var expectedText =
            """
            public class Foo
            {
                static void Bar()
                {
                    var f = new Foo();
                    f.DoFoo(2);
                }
            }

            public static class FooExtensions
            {
                public static void DoFoo(this Foo foo, int value2)
                {

                }
            }
            """;

        await VerifyExpectedTextAsync(WellKnownIntents.DeleteParameter, initialText, currentText, expectedText).ConfigureAwait(false);
    }

    [Fact]
    public async Task TestDeleteParamsParameterAsync()
    {
        var initialText =
            """
            class C
            {
                void M()
                {
                    Method(new C(), {|priorSelection:1|}, 2, 3);
                }

                void Method(C c, params int[] values)
                {
                }
            }
            """;
        var currentText =
            """
            class C
            {
                void M()
                {
                    Method(new C(), 2, 3);
                }

                void Method(C c, params int[] values)
                {
                }
            }
            """;
        var expectedText =
            """
            class C
            {
                void M()
                {
                    Method(new C());
                }

                void Method(C c)
                {
                }
            }
            """;

        await VerifyExpectedTextAsync(WellKnownIntents.DeleteParameter, initialText, currentText, expectedText).ConfigureAwait(false);
    }

    [Fact]
    public async Task TestDeleteParameterBeforeParamsAsync()
    {
        var initialText =
            """
            class C
            {
                void M()
                {
                    Method(1.0f, 1, 2, 3);
                }

                void Method(float {|priorSelection:f|}, params int[] values)
                {
                }
            }
            """;
        var currentText =
            """
            class C
            {
                void M()
                {
                    Method(1, 2, 3);
                }

                void Method(float f, params int[] values)
                {
                }
            }
            """;
        var expectedText =
            """
            class C
            {
                void M()
                {
                    Method(1, 2, 3);
                }

                void Method(params int[] values)
                {
                }
            }
            """;

        await VerifyExpectedTextAsync(WellKnownIntents.DeleteParameter, initialText, currentText, expectedText).ConfigureAwait(false);
    }

    [Fact]
    public async Task TestDeleteParameterOnStaticExtensionInvocationAsync()
    {
        var initialText =
            """
            public static class AExtension
            {
                public static void Method(this A c, int i)
                {

                }
            }

            public class A
            {
                void M()
                {
                    AExtension.Method(new A(), {|priorSelection:1|});
                }
            }
            """;
        var currentText =
            """
            public static class AExtension
            {
                public static void Method(this A c, int i)
                {

                }
            }

            public class A
            {
                void M()
                {
                    AExtension.Method(new A());
                }
            }
            """;
        var expectedText =
            """
            public static class AExtension
            {
                public static void Method(this A c)
                {

                }
            }

            public class A
            {
                void M()
                {
                    AExtension.Method(new A());
                }
            }
            """;

        await VerifyExpectedTextAsync(WellKnownIntents.DeleteParameter, initialText, currentText, expectedText).ConfigureAwait(false);
    }

    [Fact]
    public async Task TestDeleteParameterOnConstructorInvocationAsync()
    {
        var initialText =
            """
            public class A
            {
                public A(int i, string s)
                {

                }

                static A M()
                {
                    return new A(1, {|priorSelection:"hello"|});
                }
            }
            """;
        var currentText =
            """
            public class A
            {
                public A(int i, string s)
                {

                }

                static A M()
                {
                    return new A(1);
                }
            }
            """;
        var expectedText =
            """
            public class A
            {
                public A(int i)
                {

                }

                static A M()
                {
                    return new A(1);
                }
            }
            """;

        await VerifyExpectedTextAsync(WellKnownIntents.DeleteParameter, initialText, currentText, expectedText).ConfigureAwait(false);
    }
}
