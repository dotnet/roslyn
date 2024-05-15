// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.SignatureHelp;
using Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SignatureHelp;

[Trait(Traits.Feature, Traits.Features.SignatureHelp)]
public sealed class InvocationExpressionSignatureHelpProviderTests : AbstractCSharpSignatureHelpProviderTests
{
    internal override Type GetSignatureHelpProviderType()
        => typeof(InvocationExpressionSignatureHelpProvider);

    #region "Regular tests"

    [Fact]
    public async Task TestInvocationAfterCloseParen()
    {
        var markup = """
            class C
            {
                int Goo(int x)
                {
                    [|Goo(Goo(x)$$|]);
                }
            }
            """;

        await TestAsync(markup, [new SignatureHelpTestItem("int C.Goo(int x)", currentParameterIndex: 0)]);
    }

    [Fact]
    public async Task TestInvocationInsideLambda()
    {
        var markup = """
            using System;

            class C
            {
                void Goo(Action<int> f)
                {
                    [|Goo(i => Console.WriteLine(i)$$|]);
                }
            }
            """;

        await TestAsync(markup, [new SignatureHelpTestItem("void C.Goo(Action<int> f)", currentParameterIndex: 0)]);
    }

    [Fact]
    public async Task TestInvocationInsideLambda2()
    {
        var markup = """
            using System;

            class C
            {
                void Goo(Action<int> f)
                {
                    [|Goo(i => Con$$sole.WriteLine(i)|]);
                }
            }
            """;

        await TestAsync(markup, [new SignatureHelpTestItem("void C.Goo(Action<int> f)", currentParameterIndex: 0)]);
    }

    [Fact]
    public async Task TestInvocationWithoutParameters()
    {
        var markup = """
            class C
            {
                void Goo()
                {
                    [|Goo($$|]);
                }
            }
            """;

        await TestAsync(markup, [new SignatureHelpTestItem("void C.Goo()", string.Empty, null, currentParameterIndex: 0)]);
    }

    [Fact]
    public async Task TestInvocationWithoutParametersMethodXmlComments()
    {
        var markup = """
            class C
            {
                /// <summary>
                /// Summary for goo
                /// </summary>
                void Goo()
                {
                    [|Goo($$|]);
                }
            }
            """;

        await TestAsync(markup, [new SignatureHelpTestItem("void C.Goo()", "Summary for goo", null, currentParameterIndex: 0)]);
    }

    [Fact]
    public async Task TestInvocationWithParametersOn1()
    {
        var markup = """
            class C
            {
                void Goo(int a, int b)
                {
                    [|Goo($$a, b|]);
                }
            }
            """;

        await TestAsync(markup, [new SignatureHelpTestItem("void C.Goo(int a, int b)", string.Empty, string.Empty, currentParameterIndex: 0)]);
    }

    [Fact]
    public async Task TestInvocationWithParametersXmlCommentsOn1()
    {
        var markup = """
            class C
            {
                /// <summary>
                /// Summary for Goo
                /// </summary>
                /// <param name="a">Param a</param>
                /// <param name="b">Param b</param>
                void Goo(int a, int b)
                {
                    [|Goo($$a, b|]);
                }
            }
            """;

        await TestAsync(markup, [new SignatureHelpTestItem("void C.Goo(int a, int b)", "Summary for Goo", "Param a", currentParameterIndex: 0)]);
    }

    [Fact]
    public async Task TestInvocationWithParametersOn2()
    {
        var markup = """
            class C
            {
                void Goo(int a, int b)
                {
                    [|Goo(a, $$b|]);
                }
            }
            """;

        await TestAsync(markup, [new SignatureHelpTestItem("void C.Goo(int a, int b)", string.Empty, string.Empty, currentParameterIndex: 1)]);
    }

    [Fact]
    public async Task TestInvocationWithParametersXmlComentsOn2()
    {
        var markup = """
            class C
            {
                /// <summary>
                /// Summary for Goo
                /// </summary>
                /// <param name="a">Param a</param>
                /// <param name="b">Param b</param>
                void Goo(int a, int b)
                {
                    [|Goo(a, $$b|]);
                }
            }
            """;

        await TestAsync(markup, [new SignatureHelpTestItem("void C.Goo(int a, int b)", "Summary for Goo", "Param b", currentParameterIndex: 1)]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26713")]
    public async Task TestDelegateParameterWithDocumentation_Invoke()
    {
        var markup = """
            class C
            {
                /// <param name="a">Parameter docs</param>
                delegate void SomeDelegate(int a);

                void M(SomeDelegate theDelegate)
                {
                    [|theDelegate($$|]);
                }
            }
            """;

        await TestAsync(markup, [new SignatureHelpTestItem("void SomeDelegate(int a)", parameterDocumentation: "Parameter docs", currentParameterIndex: 0)]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26713")]
    public async Task TestDelegateParameterWithDocumentation_Invoke2()
    {
        var markup = """
            class C
            {
                /// <param name="a">Parameter docs</param>
                delegate void SomeDelegate(int a);

                void M(SomeDelegate theDelegate)
                {
                    [|theDelegate.Invoke($$|]);
                }
            }
            """;

        await TestAsync(markup, [new SignatureHelpTestItem("void SomeDelegate.Invoke(int a)", parameterDocumentation: "Parameter docs", currentParameterIndex: 0)]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26713")]
    public async Task TestDelegateParameterWithDocumentation_BeginInvoke()
    {
        var markup = """
            class C
            {
                /// <param name="a">Parameter docs</param>
                delegate void SomeDelegate(int a);

                void M(SomeDelegate theDelegate)
                {
                    [|theDelegate.BeginInvoke($$|]);
                }
            }
            """;

        await TestAsync(markup, [new SignatureHelpTestItem("System.IAsyncResult SomeDelegate.BeginInvoke(int a, System.AsyncCallback callback, object @object)", parameterDocumentation: "Parameter docs", currentParameterIndex: 0)]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26713")]
    public async Task TestDelegateParameterWithDocumentation_BeginInvoke2()
    {
        var markup = """
            class C
            {
                /// <param name="a">Parameter docs</param>
                /// <param name="callback">This should not be displayed</param>
                delegate void SomeDelegate(int a);

                void M(SomeDelegate theDelegate)
                {
                    [|theDelegate.BeginInvoke(0, $$|]);
                }
            }
            """;

        await TestAsync(markup, [new SignatureHelpTestItem("System.IAsyncResult SomeDelegate.BeginInvoke(int a, System.AsyncCallback callback, object @object)", parameterDocumentation: null, currentParameterIndex: 1)]);
    }

    [Fact]
    public async Task TestInvocationWithoutClosingParen()
    {
        var markup = """
            class C
            {
                void Goo()
                {
                    [|Goo($$
                |]}
            }
            """;

        await TestAsync(markup, [new SignatureHelpTestItem("void C.Goo()", string.Empty, null, currentParameterIndex: 0)]);
    }

    [Fact]
    public async Task TestInvocationWithoutClosingParenWithParameters()
    {
        var markup =
            """
            class C
            {
                void Goo(int a, int b)
                {
                    [|Goo($$a, b
                |]}
            }
            """;

        await TestAsync(markup, [new SignatureHelpTestItem("void C.Goo(int a, int b)", string.Empty, string.Empty, currentParameterIndex: 0)]);
    }

    [Fact]
    public async Task TestInvocationWithoutClosingParenWithParametersOn2()
    {
        var markup = """
            class C
            {
                void Goo(int a, int b)
                {
                    [|Goo(a, $$b
                |]}
            }
            """;

        await TestAsync(markup, [new SignatureHelpTestItem("void C.Goo(int a, int b)", string.Empty, string.Empty, currentParameterIndex: 1)]);
    }

    [Fact]
    public async Task TestInvocationOnLambda()
    {
        var markup = """
            using System;

            class C
            {
                void Goo()
                {
                    Action<int> f = (i) => Console.WriteLine(i);
                    [|f($$
                |]}
            }
            """;

        await TestAsync(markup, [new SignatureHelpTestItem("void Action<int>(int obj)", string.Empty, string.Empty, currentParameterIndex: 0)]);
    }

    [Fact]
    public async Task TestInvocationOnMemberAccessExpression()
    {
        var markup = """
            class C
            {
                static void Bar(int a)
                {
                }

                void Goo()
                {
                    [|C.Bar($$
                |]}
            }
            """;

        await TestAsync(markup, [new SignatureHelpTestItem("void C.Bar(int a)", string.Empty, string.Empty, currentParameterIndex: 0)]);
    }

    [Fact]
    public async Task TestExtensionMethod1()
    {
        var markup = """
            using System;

            class C
            {
                void Method()
                {
                    string s = "Text";
                    [|s.ExtensionMethod($$
                |]}
            }

            public static class MyExtension
            {
                public static int ExtensionMethod(this string s, int x)
                {
                    return s.Length;
                }
            }
            """;

        // TODO: Once we do the work to allow extension methods in nested types, we should change this.
        await TestAsync(markup, [new SignatureHelpTestItem($"({CSharpFeaturesResources.extension}) int string.ExtensionMethod(int x)", string.Empty, string.Empty, currentParameterIndex: 0)], sourceCodeKind: SourceCodeKind.Regular);
    }

    [Fact]
    public async Task TestOptionalParameters()
    {
        var markup = """
            class Class1
            {
                void Test()
                {
                    Goo($$
                }

                void Goo(int a = 42)
                { }

            }
            """;

        await TestAsync(markup, [new SignatureHelpTestItem("void Class1.Goo([int a = 42])", string.Empty, string.Empty, currentParameterIndex: 0)]);
    }

    [Fact]
    public async Task TestNoInvocationOnEventNotInCurrentClass()
    {
        var markup = """
            using System;

            class C
            {
                void Goo()
                {
                    D d;
                    [|d.evt($$
                |]}
            }

            public class D
            {
                public event Action evt;
            }
            """;

        await TestAsync(markup);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539712")]
    public async Task TestInvocationOnNamedType()
    {
        var markup = """
            class Program
            {
                void Main()
                {
                    C.Goo($$
                }
            }
            class C
            {
                public static double Goo(double x)
                {
                    return x;
                }
                public double Goo(double x, double y)
                {
                    return x + y;
                }
            }
            """;

        await TestAsync(markup, [new SignatureHelpTestItem("double C.Goo(double x)", string.Empty, string.Empty, currentParameterIndex: 0)]);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539712")]
    public async Task TestInvocationOnInstance()
    {
        var markup = """
            class Program
            {
                void Main()
                {
                    new C().Goo($$
                }
            }
            class C
            {
                public static double Goo(double x)
                {
                    return x;
                }
                public double Goo(double x, double y)
                {
                    return x + y;
                }
            }
            """;

        await TestAsync(markup, [new SignatureHelpTestItem("double C.Goo(double x, double y)", string.Empty, string.Empty, currentParameterIndex: 0)]);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545118")]
    public async Task TestStatic1()
    {
        var markup = """
            class C
            {
                static void Goo()
                {
                    Bar($$
                }

                static void Bar()
                {
                }

                void Bar(int i)
                {
                }
            }
            """;

        await TestAsync(markup, [new SignatureHelpTestItem("void C.Bar()", currentParameterIndex: 0)]);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545118")]
    public async Task TestStatic2()
    {
        var markup = """
            class C
            {
                void Goo()
                {
                    Bar($$
                }

                static void Bar()
                {
                }

                void Bar(int i)
                {
                }
            }
            """;

        await TestAsync(markup, [
            new SignatureHelpTestItem("void C.Bar()", currentParameterIndex: 0),
            new SignatureHelpTestItem("void C.Bar(int i)", currentParameterIndex: 0)]);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543117")]
    public async Task TestInvocationOnAnonymousType()
    {
        var markup = """
            using System.Collections.Generic;

            class Program
            {
                static void Main(string[] args)
                {
                    var goo = new { Name = string.Empty, Age = 30 };
                    Goo(goo).Add($$);
                }

                static List<T> Goo<T>(T t)
                {
                }
            }
            """;

        await TestAsync(markup, [
            new SignatureHelpTestItem(
                $$"""
                void List<'a>.Add('a item)

                {{FeaturesResources.Types_colon}}
                    'a {{FeaturesResources.is_}} new { string Name, int Age }
                """,
                methodDocumentation: string.Empty,
                parameterDocumentation: string.Empty,
                currentParameterIndex: 0,
                description: $$"""


                {{FeaturesResources.Types_colon}}
                    'a {{FeaturesResources.is_}} new { string Name, int Age }
                """)]);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/968188")]
    public async Task TestInvocationOnBaseExpression_ProtectedAccessibility()
    {
        var markup = """
            using System;
            public class Base
            {
                protected virtual void Goo(int x) { }
            }

            public class Derived : Base
            {
                void Test()
                {
                    base.Goo($$);
                }

                protected override void Goo(int x)
                {
                    throw new NotImplementedException();
                }
            }
            """;

        await TestAsync(markup, [new SignatureHelpTestItem(
            @"void Base.Goo(int x)",
            methodDocumentation: string.Empty,
            parameterDocumentation: string.Empty,
            currentParameterIndex: 0,
            description: string.Empty)]);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/968188")]
    public async Task TestInvocationOnBaseExpression_AbstractBase()
    {
        var markup = """
            using System;
            public abstract class Base
            {
                protected abstract void Goo(int x);
            }

            public class Derived : Base
            {
                void Test()
                {
                    base.Goo($$);
                }

                protected override void Goo(int x)
                {
                    throw new NotImplementedException();
                }
            }
            """;

        await TestAsync(markup, [new SignatureHelpTestItem(
            @"void Base.Goo(int x)",
            methodDocumentation: string.Empty,
            parameterDocumentation: string.Empty,
            currentParameterIndex: 0,
            description: string.Empty)]);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/968188")]
    public async Task TestInvocationOnThisExpression_ProtectedAccessibility()
    {
        var markup = """
            using System;
            public class Base
            {
                protected virtual void Goo(int x) { }
            }

            public class Derived : Base
            {
                void Test()
                {
                    this.Goo($$);
                }
            }
            """;

        await TestAsync(markup, [new SignatureHelpTestItem(
            @"void Base.Goo(int x)",
            methodDocumentation: string.Empty,
            parameterDocumentation: string.Empty,
            currentParameterIndex: 0,
            description: string.Empty)]);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/968188")]
    public async Task TestInvocationOnThisExpression_ProtectedAccessibility_Overridden()
    {
        var markup = """
            using System;
            public class Base
            {
                protected virtual void Goo(int x) { }
            }

            public class Derived : Base
            {
                void Test()
                {
                    this.Goo($$);
                }

                protected override void Goo(int x)
                {
                    throw new NotImplementedException();
                }
            }
            """;

        await TestAsync(markup, [new SignatureHelpTestItem(
            @"void Derived.Goo(int x)",
            methodDocumentation: string.Empty,
            parameterDocumentation: string.Empty,
            currentParameterIndex: 0,
            description: string.Empty)]);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/968188")]
    public async Task TestInvocationOnThisExpression_ProtectedAccessibility_AbstractBase()
    {
        var markup = """
            using System;
            public abstract class Base
            {
                protected abstract void Goo(int x);
            }

            public class Derived : Base
            {
                void Test()
                {
                    this.Goo($$);
                }
            }
            """;

        await TestAsync(markup, [new SignatureHelpTestItem(
            @"void Base.Goo(int x)",
            methodDocumentation: string.Empty,
            parameterDocumentation: string.Empty,
            currentParameterIndex: 0,
            description: string.Empty)]);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/968188")]
    public async Task TestInvocationOnThisExpression_ProtectedAccessibility_AbstractBase_Overridden()
    {
        var markup = """
            using System;
            public abstract class Base
            {
                protected abstract void Goo(int x);
            }

            public class Derived : Base
            {
                void Test()
                {
                    this.Goo($$);
                }

                protected override void Goo(int x)
                {
                    throw new NotImplementedException();
                }
            }
            """;

        await TestAsync(markup, [new SignatureHelpTestItem(
            @"void Derived.Goo(int x)",
            methodDocumentation: string.Empty,
            parameterDocumentation: string.Empty,
            currentParameterIndex: 0,
            description: string.Empty)]);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/968188")]
    public async Task TestInvocationOnBaseExpression_ProtectedInternalAccessibility()
    {
        var markup = """
            using System;
            public class Base
            {
                protected internal void Goo(int x) { }
            }

            public class Derived : Base
            {
                void Test()
                {
                    base.Goo($$);
                }

                protected override void Goo(int x)
                {
                    throw new NotImplementedException();
                }
            }
            """;

        await TestAsync(markup, [new SignatureHelpTestItem(
            @"void Base.Goo(int x)",
            methodDocumentation: string.Empty,
            parameterDocumentation: string.Empty,
            currentParameterIndex: 0,
            description: string.Empty)]);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/968188")]
    public async Task TestInvocationOnBaseMember_ProtectedAccessibility_ThroughType()
    {
        var markup = """
            using System;
            public class Base
            {
                protected virtual void Goo(int x) { }
            }

            public class Derived : Base
            {
                void Test()
                {
                    new Base().Goo($$);
                }

                protected override void Goo(int x)
                {
                    throw new NotImplementedException();
                }
            }
            """;

        await TestAsync(markup, null);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/968188")]
    public async Task TestInvocationOnBaseExpression_PrivateAccessibility()
    {
        var markup = """
            using System;
            public class Base
            {
                private void Goo(int x) { }
            }

            public class Derived : Base
            {
                void Test()
                {
                    base.Goo($$);
                }

                protected override void Goo(int x)
                {
                    throw new NotImplementedException();
                }
            }
            """;

        await TestAsync(markup, null);
    }

    #endregion

    #region "Current Parameter Name"

    [Fact]
    public async Task TestCurrentParameterName()
    {
        var markup = """
            class C
            {
                void Goo(int someParameter, bool something)
                {
                    Goo(something: false, someParameter: $$)
                }
            }
            """;

        await VerifyCurrentParameterNameAsync(markup, "someParameter");
    }

    #endregion

    #region "Trigger tests"

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47364")]
    public async Task TestInvocationOnTriggerParens_OptionalDefaultStruct()
    {
        var markup = """
            using System;
            using System.Threading;

            class Program
            {
               static void SomeMethod(CancellationToken token = default) => throw new NotImplementedException();

                static void Main(string[] args)
                {
                    [|SomeMethod($$|]);
                }
            }
            """;

        await TestAsync(
            markup, [new SignatureHelpTestItem("void Program.SomeMethod([CancellationToken token = default])", string.Empty, null, currentParameterIndex: 0)], usePreviousCharAsTrigger: true);
    }

    [Fact]
    public async Task TestInvocationOnTriggerParens()
    {
        var markup = """
            class C
            {
                void Goo()
                {
                    [|Goo($$|]);
                }
            }
            """;

        await TestAsync(markup, [new SignatureHelpTestItem("void C.Goo()", string.Empty, null, currentParameterIndex: 0)], usePreviousCharAsTrigger: true);
    }

    [Fact]
    public async Task TestInvocationOnTriggerComma()
    {
        var markup = """
            class C
            {
                void Goo(int a, int b)
                {
                    [|Goo(23,$$|]);
                }
            }
            """;

        await TestAsync(markup, [new SignatureHelpTestItem("void C.Goo(int a, int b)", string.Empty, string.Empty, currentParameterIndex: 1)], usePreviousCharAsTrigger: true);
    }

    [Fact]
    public async Task TestNoInvocationOnSpace()
    {
        var markup = """
            class C
            {
                void Goo(int a, int b)
                {
                    [|Goo(23, $$|]);
                }
            }
            """;

        await TestAsync(markup, usePreviousCharAsTrigger: true);
    }

    [Fact]
    public async Task TestTriggerCharacterInComment01()
    {
        var markup = """
            class C
            {
                void Goo(int a)
                {
                    Goo(/*,$$*/);
                }
            }
            """;
        await TestAsync(markup, [], usePreviousCharAsTrigger: true);
    }

    [Fact]
    public async Task TestTriggerCharacterInComment02()
    {
        var markup = """
            class C
            {
                void Goo(int a)
                {
                    Goo(//,$$
                        );
                }
            }
            """;
        await TestAsync(markup, [], usePreviousCharAsTrigger: true);
    }

    [Fact]
    public async Task TestTriggerCharacterInString01()
    {
        var markup = """
            class C
            {
                void Goo(int a)
                {
                    Goo(",$$");
                }
            }
            """;
        await TestAsync(markup, [], usePreviousCharAsTrigger: true);
    }

    [Fact]
    public void TestTriggerCharacters()
    {
        char[] expectedCharacters = [',', '('];
        char[] unexpectedCharacters = [' ', '[', '<'];

        VerifyTriggerCharacters(expectedCharacters, unexpectedCharacters);
    }

    #endregion

    #region "EditorBrowsable tests"

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Method_BrowsableStateAlways()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    Goo.Bar($$
                }
            }
            """;

        var referencedCode = """
            public class Goo
            {
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
                public static void Bar()
                {
                }
            }
            """;
        List<SignatureHelpTestItem> expectedOrderedItems = [new SignatureHelpTestItem("void Goo.Bar()", string.Empty, null, currentParameterIndex: 0)];

        await TestSignatureHelpInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            expectedOrderedItemsMetadataReference: expectedOrderedItems,
            expectedOrderedItemsSameSolution: expectedOrderedItems,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Method_BrowsableStateNever()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    Goo.Bar($$
                }
            }
            """;

        var referencedCode = """
            public class Goo
            {
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
                public static void Bar()
                {
                }
            }
            """;

        await TestSignatureHelpInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            expectedOrderedItemsMetadataReference: [],
            expectedOrderedItemsSameSolution: [new SignatureHelpTestItem("void Goo.Bar()", string.Empty, null, currentParameterIndex: 0)],
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Method_BrowsableStateAdvanced()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new Goo().Bar($$
                }
            }
            """;

        var referencedCode = """
            public class Goo
            {
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
                public void Bar()
                {
                }
            }
            """;
        List<SignatureHelpTestItem> expectedOrderedItems = [new SignatureHelpTestItem("void Goo.Bar()", string.Empty, null, currentParameterIndex: 0)];

        await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
            referencedCode: referencedCode,
            expectedOrderedItemsMetadataReference: [],
            expectedOrderedItemsSameSolution: expectedOrderedItems,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp,
            hideAdvancedMembers: true);

        await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
            referencedCode: referencedCode,
            expectedOrderedItemsMetadataReference: expectedOrderedItems,
            expectedOrderedItemsSameSolution: expectedOrderedItems,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp,
            hideAdvancedMembers: false);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Method_Overloads_OneBrowsableAlways_OneBrowsableNever()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new Goo().Bar($$
                }
            }
            """;

        var referencedCode = """
            public class Goo
            {
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
                public void Bar()
                {
                }

                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
                public void Bar(int x)
                {
                }
            }
            """;

        await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
            referencedCode: referencedCode,
            expectedOrderedItemsMetadataReference: [new SignatureHelpTestItem("void Goo.Bar()", string.Empty, null, currentParameterIndex: 0)],
            expectedOrderedItemsSameSolution: [
                new SignatureHelpTestItem("void Goo.Bar()", string.Empty, null, currentParameterIndex: 0),
                new SignatureHelpTestItem("void Goo.Bar(int x)", string.Empty, string.Empty, currentParameterIndex: 0)],
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Method_Overloads_BothBrowsableNever()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new Goo().Bar($$
                }
            }
            """;

        var referencedCode = """
            public class Goo
            {
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
                public void Bar()
                {
                }

                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
                public void Bar(int x)
                {
                }
            }
            """;

        await TestSignatureHelpInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            expectedOrderedItemsMetadataReference: [],
            expectedOrderedItemsSameSolution: [
                new SignatureHelpTestItem("void Goo.Bar()", string.Empty, null, currentParameterIndex: 0),
                new SignatureHelpTestItem("void Goo.Bar(int x)", string.Empty, string.Empty, currentParameterIndex: 0)],
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task OverriddenSymbolsFilteredFromSigHelp()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new D().Goo($$
                }
            }
            """;

        var referencedCode = """
            public class B
            {
                public virtual void Goo(int original)
                {
                }
            }

            public class D : B
            {
                public override void Goo(int derived)
                {
                }
            }
            """;

        List<SignatureHelpTestItem> expectedOrderedItems =
        [
            new SignatureHelpTestItem("void D.Goo(int derived)", string.Empty, string.Empty, currentParameterIndex: 0),
        ];

        await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
            referencedCode: referencedCode,
            expectedOrderedItemsMetadataReference: expectedOrderedItems,
            expectedOrderedItemsSameSolution: expectedOrderedItems,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_BrowsableStateAlwaysMethodInBrowsableStateNeverClass()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new C().Goo($$
                }
            }
            """;

        var referencedCode = """
            [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
            public class C
            {
                public void Goo()
                {
                }
            }
            """;
        List<SignatureHelpTestItem> expectedOrderedItems = [new SignatureHelpTestItem("void C.Goo()", string.Empty, null, currentParameterIndex: 0)];

        await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
            referencedCode: referencedCode,
            expectedOrderedItemsMetadataReference: expectedOrderedItems,
            expectedOrderedItemsSameSolution: expectedOrderedItems,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_BrowsableStateAlwaysMethodInBrowsableStateNeverBaseClass()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new D().Goo($$
                }
            }
            """;

        var referencedCode = """
            [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
            public class B
            {
                public void Goo()
                {
                }
            }

            public class D : B
            {
                public void Goo(int x)
                {
                }
            }
            """;
        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new SignatureHelpTestItem("void B.Goo()", string.Empty, null, currentParameterIndex: 0),
            new SignatureHelpTestItem("void D.Goo(int x)", string.Empty, string.Empty, currentParameterIndex: 0),
        };

        await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
            referencedCode: referencedCode,
            expectedOrderedItemsMetadataReference: expectedOrderedItems,
            expectedOrderedItemsSameSolution: expectedOrderedItems,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_BrowsableStateNeverMethodsInBaseClass()
    {
        var markup = """
            class Program : B
            {
                void M()
                {
                    Goo($$
                }
            }
            """;

        var referencedCode = """
            public class B
            {
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
                public void Goo()
                {
                }
            }
            """;

        await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
            referencedCode: referencedCode,
            expectedOrderedItemsMetadataReference: [],
            expectedOrderedItemsSameSolution: [new SignatureHelpTestItem("void B.Goo()", string.Empty, null, currentParameterIndex: 0)],
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_GenericTypeCausingMethodSignatureEquality_BothBrowsableAlways()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new C<int>().Goo($$
                }
            }
            """;

        var referencedCode = """
            public class C<T>
            {
                public void Goo(T t) { }
                public void Goo(int i) { }
            }
            """;
        List<SignatureHelpTestItem> expectedOrderedItems =
        [
            new SignatureHelpTestItem("void C<int>.Goo(int t)", string.Empty, string.Empty, currentParameterIndex: 0),
            new SignatureHelpTestItem("void C<int>.Goo(int i)", string.Empty, string.Empty, currentParameterIndex: 0),
        ];

        await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
            referencedCode: referencedCode,
            expectedOrderedItemsMetadataReference: expectedOrderedItems,
            expectedOrderedItemsSameSolution: expectedOrderedItems,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_GenericTypeCausingMethodSignatureEquality_BrowsableMixed1()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new C<int>().Goo($$
                }
            }
            """;

        var referencedCode = """
            public class C<T>
            {
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
                public void Goo(T t) { }
                public void Goo(int i) { }
            }
            """;

        await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
            referencedCode: referencedCode,
            expectedOrderedItemsMetadataReference: [new SignatureHelpTestItem("void C<int>.Goo(int i)", string.Empty, string.Empty, currentParameterIndex: 0)],
            expectedOrderedItemsSameSolution: [
                new SignatureHelpTestItem("void C<int>.Goo(int t)", string.Empty, string.Empty, currentParameterIndex: 0),
                new SignatureHelpTestItem("void C<int>.Goo(int i)", string.Empty, string.Empty, currentParameterIndex: 0)],
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_GenericTypeCausingMethodSignatureEquality_BrowsableMixed2()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new C<int>().Goo($$
                }
            }
            """;

        var referencedCode = """
            public class C<T>
            {
                public void Goo(T t) { }
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
                public void Goo(int i) { }
            }
            """;

        await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
            referencedCode: referencedCode,
            expectedOrderedItemsMetadataReference: [new SignatureHelpTestItem("void C<int>.Goo(int t)", string.Empty, string.Empty, currentParameterIndex: 0)],
            expectedOrderedItemsSameSolution: [
                new SignatureHelpTestItem("void C<int>.Goo(int t)", string.Empty, string.Empty, currentParameterIndex: 0),
                new SignatureHelpTestItem("void C<int>.Goo(int i)", string.Empty, string.Empty, currentParameterIndex: 0)],
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_GenericTypeCausingMethodSignatureEquality_BothBrowsableNever()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new C<int>().Goo($$

                }
            }
            """;

        var referencedCode = """
            public class C<T>
            {
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
                public void Goo(T t) { }
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
                public void Goo(int i) { }
            }
            """;

        await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
            referencedCode: referencedCode,
            expectedOrderedItemsMetadataReference: [],
            expectedOrderedItemsSameSolution: [
                new SignatureHelpTestItem("void C<int>.Goo(int t)", string.Empty, string.Empty, currentParameterIndex: 0),
                new SignatureHelpTestItem("void C<int>.Goo(int i)", string.Empty, string.Empty, currentParameterIndex: 0)],
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_GenericType2CausingMethodSignatureEquality_BothBrowsableAlways()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new C<int, int>().Goo($$
                }
            }
            """;

        var referencedCode = """
            public class C<T, U>
            {
                public void Goo(T t) { }
                public void Goo(U u) { }
            }
            """;

        List<SignatureHelpTestItem> expectedOrderedItems =
        [
            new SignatureHelpTestItem("void C<int, int>.Goo(int t)", string.Empty, string.Empty, currentParameterIndex: 0),
            new SignatureHelpTestItem("void C<int, int>.Goo(int u)", string.Empty, string.Empty, currentParameterIndex: 0),
        ];

        await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
            referencedCode: referencedCode,
            expectedOrderedItemsMetadataReference: expectedOrderedItems,
            expectedOrderedItemsSameSolution: expectedOrderedItems,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_GenericType2CausingMethodSignatureEquality_BrowsableMixed()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new C<int, int>().Goo($$
                }
            }
            """;

        var referencedCode = """
            public class C<T, U>
            {
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
                public void Goo(T t) { }
                public void Goo(U u) { }
            }
            """;

        await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
            referencedCode: referencedCode,
            expectedOrderedItemsMetadataReference: [new SignatureHelpTestItem("void C<int, int>.Goo(int u)", string.Empty, string.Empty, currentParameterIndex: 0)],
            expectedOrderedItemsSameSolution: [
                new SignatureHelpTestItem("void C<int, int>.Goo(int t)", string.Empty, string.Empty, currentParameterIndex: 0),
                new SignatureHelpTestItem("void C<int, int>.Goo(int u)", string.Empty, string.Empty, currentParameterIndex: 0)],
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_GenericType2CausingMethodSignatureEquality_BothBrowsableNever()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new C<int, int>().Goo($$
                }
            }
            """;

        var referencedCode = """
            public class C<T, U>
            {
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
                public void Goo(T t) { }
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
                public void Goo(U u) { }
            }
            """;

        await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
            referencedCode: referencedCode,
            expectedOrderedItemsMetadataReference: [],
            expectedOrderedItemsSameSolution: [
                new SignatureHelpTestItem("void C<int, int>.Goo(int t)", string.Empty, string.Empty, currentParameterIndex: 0),
                new SignatureHelpTestItem("void C<int, int>.Goo(int u)", string.Empty, string.Empty, currentParameterIndex: 0)],
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }
    #endregion

    #region "Awaitable tests"
    [Fact]
    public async Task AwaitableMethod()
    {
        var markup = """
            using System.Threading.Tasks;
            class C
            {
                async Task Goo()
                {
                    [|Goo($$|]);
                }
            }
            """;

        await TestSignatureHelpWithMscorlib45Async(markup, [new SignatureHelpTestItem($"({CSharpFeaturesResources.awaitable}) Task C.Goo()", methodDocumentation: string.Empty, currentParameterIndex: 0)], "C#");
    }

    [Fact]
    public async Task AwaitableMethod2()
    {
        var markup = """
            using System.Threading.Tasks;
            class C
            {
                async Task<Task<int>> Goo()
                {
                    [|Goo($$|]);
                }
            }
            """;

        await TestSignatureHelpWithMscorlib45Async(markup, [new SignatureHelpTestItem($"({CSharpFeaturesResources.awaitable}) Task<Task<int>> C.Goo()", methodDocumentation: string.Empty, currentParameterIndex: 0)], "C#");
    }

    #endregion

    [Fact, WorkItem(13849, "DevDiv_Projects/Roslyn")]
    public async Task TestSpecificity1()
    {
        var markup = """
            class Class1
            {
                static void Main()
                {
                    var obj = new C<int>();
                    [|obj.M($$|])
                }
            }
            class C<T>
            {
                /// <param name="t">Generic t</param>
                public void M(T t) { }

                /// <param name="t">Real t</param>
                public void M(int t) { }
            }
            """;

        await TestAsync(markup, [new SignatureHelpTestItem("void C<int>.M(int t)", string.Empty, "Real t", currentParameterIndex: 0)]);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530017")]
    public async Task LongSignature()
    {
        var markup = """
            class C
            {
                void Goo(string a, string b, string c, string d, string e, string f, string g, string h, string i, string j, string k, string l, string m, string n, string o, string p, string q, string r, string s, string t, string u, string v, string w, string x, string y, string z)
                {
                    [|Goo($$|])
                }
            }
            """;

        await TestAsync(markup, [
            new SignatureHelpTestItem(
                signature: "void C.Goo(string a, string b, string c, string d, string e, string f, string g, string h, string i, string j, string k, string l, string m, string n, string o, string p, string q, string r, string s, string t, string u, string v, string w, string x, string y, string z)",
                prettyPrintedSignature: """
                void C.Goo(string a, string b, string c, string d, string e, string f, string g, string h, string i, string j, 
                           string k, string l, string m, string n, string o, string p, string q, string r, string s, string t, string u, 
                           string v, string w, string x, string y, string z)
                """,
                currentParameterIndex: 0)]);
    }

    [Fact]
    public async Task GenericExtensionMethod()
    {
        var markup = """
            interface IGoo
            {
                void Bar<T>();
            }

            static class GooExtensions
            {
                public static void Bar<T1, T2>(this IGoo goo) { }
            }

            class Program
            {
                static void Main()
                {
                    IGoo f = null;
                    [|f.Bar($$
                |]}
            }
            """;

        // Extension methods are supported in Interactive/Script (yet).
        await TestAsync(markup, [
            new SignatureHelpTestItem("void IGoo.Bar<T>()", currentParameterIndex: 0),
            new SignatureHelpTestItem($"({CSharpFeaturesResources.extension}) void IGoo.Bar<T1, T2>()", currentParameterIndex: 0)], sourceCodeKind: SourceCodeKind.Regular);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25830")]
    public async Task PickCorrectOverload_PickInt()
    {
        var markup = """
            class Program
            {
                static void Main()
                {
                    [|M(1$$|]);
                }
                static void M(int i) { }
                static void M(string s) { }
            }
            """;

        await TestAsync(markup, [
            new SignatureHelpTestItem("void Program.M(int i)", currentParameterIndex: 0, isSelected: true),
            new SignatureHelpTestItem($"void Program.M(string s)", currentParameterIndex: 0)]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25830")]
    public async Task PickCorrectOverload_PickInt_ReverseOrder()
    {
        var markup = """
            class Program
            {
                static void Main()
                {
                    [|M(1$$|]);
                }
                static void M(string s) { }
                static void M(int i) { }
            }
            """;

        await TestAsync(markup, [
            new SignatureHelpTestItem("void Program.M(int i)", currentParameterIndex: 0, isSelected: true),
            new SignatureHelpTestItem($"void Program.M(string s)", currentParameterIndex: 0)]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25830")]
    public async Task PickCorrectOverload_PickSecond()
    {
        var markup = """
            class Program
            {
                static void Main()
                {
                    [|M(null$$|]);
                }
                static void M(int i) { }
                static void M(string s) { }
            }
            """;

        await TestAsync(markup, [
            new SignatureHelpTestItem("void Program.M(int i)", currentParameterIndex: 0),
            new SignatureHelpTestItem($"void Program.M(string s)", currentParameterIndex: 0, isSelected: true)]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25830")]
    public async Task PickCorrectOverload_OtherName_PickIntRemaining()
    {
        var markup = """
            class D
            {
                static void Main()
                {
                    [|M(i: 42$$|]);
                }
                static void M(D filtered) { }
                static void M(int i) { }
                static void M(string i) { }
            }
            """;

        await TestAsync(markup, [
            new SignatureHelpTestItem("void D.M(int i)", currentParameterIndex: 0, isSelected: true),
            new SignatureHelpTestItem("void D.M(string i)", currentParameterIndex: 0)]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25830")]
    public async Task PickCorrectOverload_OtherName_PickIntRemaining_ConversionToD()
    {
        var markup = """
            class D
            {
                static void Main()
                {
                    [|M(i: 42$$|]);
                }
                static void M(D filtered) { }
                static void M(int i) { }
                static void M(string i) { }
                static implicit operator D(int i) => throw null;
            }
            """;

        await TestAsync(markup, [
            new SignatureHelpTestItem("void D.M(int i)", currentParameterIndex: 0, isSelected: true),
            new SignatureHelpTestItem("void D.M(string i)", currentParameterIndex: 0)]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25830")]
    public async Task PickCorrectOverload_OtherName_PickIntRemaining_ReversedOrder()
    {
        var markup = """
            class D
            {
                static void Main()
                {
                    [|M(i: 42$$|]);
                }
                static void M(string i) { }
                static void M(int i) { }
                static void M(D filtered) { }
            }
            """;

        await TestAsync(markup, [
            new SignatureHelpTestItem("void D.M(int i)", currentParameterIndex: 0, isSelected: true),
            new SignatureHelpTestItem("void D.M(string i)", currentParameterIndex: 0)]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25830")]
    public async Task PickCorrectOverload_OtherName_PickStringRemaining()
    {
        var markup = """
            class D
            {
                static void Main()
                {
                    [|M(i: null$$|]);
                }
                static void M(D filtered) { }
                static void M(int i) { }
                static void M(string i) { }
            }
            """;

        await TestAsync(markup, [
            new SignatureHelpTestItem("void D.M(int i)", currentParameterIndex: 0),
            new SignatureHelpTestItem("void D.M(string i)", currentParameterIndex: 0, isSelected: true)]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25830")]
    public async Task PickCorrectOverload_RefKind()
    {
        var markup = """
            class D
            {
                static void Main()
                {
                    int i = 0;
                    [|M(out i$$|]);
                }
                static void M(ref int a, int i) { }
                static void M(out int b, int i) { }
            }
            """;

        await TestAsync(markup, [
            new SignatureHelpTestItem("void D.M(ref int a, int i)", currentParameterIndex: 0),
            new SignatureHelpTestItem("void D.M(out int b, int i)", currentParameterIndex: 0, isSelected: true)]);
    }

    [Theory]
    [InlineData("1$$", 0)]
    [InlineData(",$$", 1)]
    [InlineData(",$$,", 1)]
    [InlineData(",,$$", 2)]
    [InlineData("i2: 1, $$,", 0)]
    [InlineData("i2: 1, i1: $$,", 0)]
    [InlineData("i2: 1, $$, i1: 2", 2)]
    [Trait(Traits.Feature, Traits.Features.SignatureHelp)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/6713")]
    public async Task PickCorrectOverload_NamesAndEmptyPositions(string arguments, int expectedParameterIndex)
    {
        var markup = """
            class Program
            {
                static void Main()
                {
                    [|M(ARGUMENTS|]);
                }
                static void M(int i1, int i2, int i3) { }
            }
            """;

        await TestAsync(markup.Replace("ARGUMENTS", arguments), [new SignatureHelpTestItem("void Program.M(int i1, int i2, int i3)", currentParameterIndex: expectedParameterIndex, isSelected: true)]);
    }

    [Theory]
    [InlineData("1$$", 0)]
    [InlineData(",$$", 1)]
    [InlineData(",$$,", 1)]
    [InlineData(",,$$", 2)]
    [InlineData("i2: 1, $$,", 0)]
    [InlineData("i2: 1, i1: $$,", 0)]
    [InlineData("i2: 1, $$, i1: 2", 2)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/6713")]
    public async Task PickCorrectOverload_NamesAndEmptyPositions_Delegate(string arguments, int expectedParameterIndex)
    {
        var markup = """
            class Program
            {
                delegate void Delegate(int i1, int i2, int i3);
                void Main(Delegate d)
                {
                    [|d(ARGUMENTS|]);
                }
            }
            """;

        await TestAsync(markup.Replace("ARGUMENTS", arguments), [new SignatureHelpTestItem("void Delegate(int i1, int i2, int i3)", currentParameterIndex: expectedParameterIndex, isSelected: true)]);
    }

    [Theory]
    [InlineData("1$$", 0, 0)]
    [InlineData("1$$, ", 0, 0)]
    [InlineData("1, $$", 1, 0)]
    [InlineData("s: $$", 1, 0)]
    [InlineData("s: string.Empty$$", 1, 0)]
    [InlineData("s: string.Empty$$, ", 1, 0)]
    [InlineData("s: string.Empty, $$", 0, 0)]
    [InlineData("string.Empty$$", 0, 1)]
    [InlineData("string.Empty$$, ", 0, 1)]
    [InlineData("string.Empty,$$", 1, 1)]
    [InlineData("$$, ", 0, 0)]
    [InlineData(",$$", 1, 0)]
    [InlineData("$$, s: string.Empty", 0, 0)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/6713")]
    public async Task PickCorrectOverload_Incomplete(string arguments, int expectedParameterIndex, int expectedSelectedIndex)
    {
        var markup = """
            class Program
            {
                static void Main()
                {
                    [|M(ARGUMENTS|]);
                }
                static void M(int i, string s) { }
                static void M(string s, string s2) { }
            }
            """;

        var index = 0;

        await TestAsync(markup.Replace("ARGUMENTS", arguments), [
            new SignatureHelpTestItem("void Program.M(int i, string s)", currentParameterIndex: expectedParameterIndex, isSelected: expectedSelectedIndex == index++),
            new SignatureHelpTestItem("void Program.M(string s, string s2)", currentParameterIndex: expectedParameterIndex, isSelected: expectedSelectedIndex == index++)]);
    }

    [Theory]
    [InlineData("s2: $$", 1)]
    [InlineData("s2: string.Empty$$", 1)]
    [InlineData("s2: string.Empty$$,", 1)]
    [InlineData("s2: string.Empty,$$", 0)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/6713")]
    public async Task PickCorrectOverload_Incomplete_WithNames(string arguments, int expectedParameterIndex)
    {
        var markup = """
            class Program
            {
                static void Main()
                {
                    [|M(ARGUMENTS|]);
                }
                static void M(int i, string s) { }
                static void M(string s, string s2) { }
            }
            """;

        await TestAsync(markup.Replace("ARGUMENTS", arguments), [new SignatureHelpTestItem($"void Program.M(string s, string s2)", currentParameterIndex: expectedParameterIndex, isSelected: true)]);
    }

    [Theory]
    [InlineData("1$$", 0)]
    [InlineData("1$$,", 0)]
    [InlineData("1$$, 2", 0)]
    [InlineData("1, $$", 1)]
    [InlineData("1, 2$$", 1)]
    [InlineData("1, 2$$, ", 1)]
    [InlineData("1, 2$$, 3", 1)]
    [InlineData("1, 2, 3$$", 2)]
    [InlineData("1, , 3$$", 2)]
    [InlineData(" , , 3$$", 2)]
    [InlineData("i1: 1, 2, 3$$", 2)]
    [InlineData("i1: 1$$, i2: new int[] { }", 0)]
    [InlineData("i2: new int[] { }$$, i1: 1", 1)]
    [InlineData("i1: 1, i2: new int[] { }$$", 1)]
    [InlineData("i2: new int[] { }, i1: 1$$", 0)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/6713")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/66984")]
    public async Task PickCorrectOverload_Params(string arguments, int expectedParameterIndex)
    {
        var markup = """
            class Program
            {
                void Main()
                {
                    [|M(ARGUMENTS|]);
                }
                void M(int i1, params int[] i2) { }
            }
            """;

        await TestAsync(markup.Replace("ARGUMENTS", arguments), [new SignatureHelpTestItem("void Program.M(int i1, params int[] i2)", currentParameterIndex: expectedParameterIndex, isSelected: true)]);
    }

    [Fact]
    public async Task PickCorrectOverload_Params_NonArrayType()
    {
        var source = """
            class Program
            {
                void Main()
                {
                    [|M(1, 2$$|]);
                }
                void M(int i1, params int i2) { }
            }
            """;

        await TestAsync(source, [new SignatureHelpTestItem("void Program.M(int i1, params int i2)", currentParameterIndex: 1, isSelected: true)]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/6713")]
    public async Task PickCorrectOverload_Incomplete_OutOfPositionArgument()
    {
        var markup = """
            class Program
            {
                static void Main()
                {
                    [|M(string.Empty, s3: string.Empty, $$|]);
                }
                static void M(string s1, string s2, string s3) { }
            }
            """;

        // The first unspecified parameter (s2) is selected
        await TestAsync(markup, [new SignatureHelpTestItem($"void Program.M(string s1, string s2, string s3)", currentParameterIndex: 1, isSelected: true)]);
    }

    [Theory]
    [InlineData("i: 1", 0)]
    [InlineData("i: 1, ", 1)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/6713")]
    public async Task PickCorrectOverload_IncompleteWithNameI(string arguments, int expectedParameterIndex)
    {
        var markup = """
            class Program
            {
                static void Main()
                {
                    [|M(ARGUMENTS$$|]);
                }
                static void M(int i, string s) { }
                static void M(string s, string s2) { }
            }
            """;

        await TestAsync(markup.Replace("ARGUMENTS", arguments), [new SignatureHelpTestItem("void Program.M(int i, string s)", currentParameterIndex: expectedParameterIndex, isSelected: true)]);
    }

    [Fact]
    public async Task TestInvocationWithCrefXmlComments()
    {
        var markup = """
            class C
            {
                /// <summary>
                /// Summary for goo. See method <see cref="Bar" />
                /// </summary>
                void Goo()
                {
                    [|Goo($$|]);
                }

                void Bar() { }
            }
            """;

        await TestAsync(markup, [new SignatureHelpTestItem("void C.Goo()", "Summary for goo. See method C.Bar()", null, currentParameterIndex: 0)]);
    }

    [Fact]
    public async Task FieldUnavailableInOneLinkedFile()
    {
        var markup = """
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1" PreprocessorSymbols="GOO">
                    <Document FilePath="SourceDocument"><![CDATA[
            class C
            {
            #if GOO
                void bar()
                {
                }
            #endif
                void goo()
                {
                    bar($$
                }
            }
            ]]>
                    </Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj2">
                    <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="SourceDocument"/>
                </Project>
            </Workspace>
            """;

        await VerifyItemWithReferenceWorkerAsync(markup, [new SignatureHelpTestItem($"""
            void C.bar()

            {string.Format(FeaturesResources._0_1, "Proj1", FeaturesResources.Available)}
            {string.Format(FeaturesResources._0_1, "Proj2", FeaturesResources.Not_Available)}

            {FeaturesResources.You_can_use_the_navigation_bar_to_switch_contexts}
            """, currentParameterIndex: 0)], hideAdvancedMembers: false);
    }

    [Fact]
    public async Task ExcludeFilesWithInactiveRegions()
    {
        var markup = """
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1" PreprocessorSymbols="GOO,BAR">
                    <Document FilePath="SourceDocument"><![CDATA[
            class C
            {
            #if GOO
                void bar()
                {
                }
            #endif

            #if BAR
                void goo()
                {
                    bar($$
                }
            #endif
            }
            ]]>
                    </Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj2">
                    <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="SourceDocument" />
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj3" PreprocessorSymbols="BAR">
                    <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="SourceDocument"/>
                </Project>
            </Workspace>
            """;

        await VerifyItemWithReferenceWorkerAsync(markup, [new SignatureHelpTestItem($"""
            void C.bar()

            {string.Format(FeaturesResources._0_1, "Proj1", FeaturesResources.Available)}
            {string.Format(FeaturesResources._0_1, "Proj3", FeaturesResources.Not_Available)}

            {FeaturesResources.You_can_use_the_navigation_bar_to_switch_contexts}
            """, currentParameterIndex: 0)], hideAdvancedMembers: false);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768697")]
    public async Task InstanceAndStaticMethodsShown1()
    {
        var markup = """
            class C
            {
                Goo Goo;

                void M()
                {
                    Goo.Bar($$
                }
            }

            class Goo
            {
                public void Bar(int x) { }
                public static void Bar(string s) { }
            }
            """;

        await TestAsync(markup, [
            new SignatureHelpTestItem("void Goo.Bar(int x)", currentParameterIndex: 0),
            new SignatureHelpTestItem("void Goo.Bar(string s)", currentParameterIndex: 0)]);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768697")]
    public async Task InstanceAndStaticMethodsShown2()
    {
        var markup = """
            class C
            {
                Goo Goo;

                void M()
                {
                    Goo.Bar($$");
                }
            }

            class Goo
            {
                public void Bar(int x) { }
                public static void Bar(string s) { }
            }
            """;

        await TestAsync(markup, [
            new SignatureHelpTestItem("void Goo.Bar(int x)", currentParameterIndex: 0),
            new SignatureHelpTestItem("void Goo.Bar(string s)", currentParameterIndex: 0)]);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768697")]
    public async Task InstanceAndStaticMethodsShown3()
    {
        var markup = """
            class C
            {
                Goo Goo;

                void M()
                {
                    Goo.Bar($$
                }
            }

            class Goo
            {
                public static void Bar(int x) { }
                public static void Bar(string s) { }
            }
            """;

        await TestAsync(markup, [
            new SignatureHelpTestItem("void Goo.Bar(int x)", currentParameterIndex: 0),
            new SignatureHelpTestItem("void Goo.Bar(string s)", currentParameterIndex: 0)]);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768697")]
    public async Task InstanceAndStaticMethodsShown4()
    {
        var markup = """
            class C
            {
                void M()
                {
                    Goo x;
                    x.Bar($$
                }
            }

            class Goo
            {
                public static void Bar(int x) { }
                public static void Bar(string s) { }
            }
            """;

        await TestAsync(markup, []);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768697")]
    public async Task InstanceAndStaticMethodsShown5()
    {
        var markup = """
            class C
            {
                void M()
                {
                    Goo x;
                    x.Bar($$
                }
            }
            class x { }
            class Goo
            {
                public static void Bar(int x) { }
                public static void Bar(string s) { }
            }
            """;

        await TestAsync(markup, []);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33549")]
    public async Task ShowOnlyStaticMethodsForBuildInTypes()
    {
        var markup = """
            class C
            {
                void M()
                {
                    string.Equals($$
                }
            }
            """;

        await TestAsync(markup, [
            new SignatureHelpTestItem("bool object.Equals(object objA, object objB)"),
            new SignatureHelpTestItem("bool string.Equals(string a, string b)"),
            new SignatureHelpTestItem("bool string.Equals(string a, string b, System.StringComparison comparisonType)")]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23133")]
    public async Task ShowOnlyStaticMethodsForNotImportedTypes()
    {
        var markup = """
            class C
            {
                void M()
                {
                    Test.Goo.Bar($$
                }
            }
            namespace Test
            {
                class Goo
                {
                    public void Bar(int x) { }
                    public static void Bar(string s) { }
                }
            }
            """;

        await TestAsync(markup, [new SignatureHelpTestItem("void Test.Goo.Bar(string s)")]);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1067933")]
    public async Task InvokedWithNoToken()
    {
        var markup = """
            // goo($$
            """;

        await TestAsync(markup);
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public async Task MethodOverloadDifferencesIgnored()
    {
        var markup = """
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1" PreprocessorSymbols="ONE">
                    <Document FilePath="SourceDocument"><![CDATA[
            class C
            {
            #if ONE
                void Do(int x){}
            #endif
            #if TWO
                void Do(string x){}
            #endif
                void Shared()
                {
                    this.Do($$
                }

            }]]></Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj2" PreprocessorSymbols="TWO">
                    <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="SourceDocument"/>
                </Project>
            </Workspace>
            """;

        await VerifyItemWithReferenceWorkerAsync(markup, [new SignatureHelpTestItem($"""
            void C.Do(int x)

            {string.Format(FeaturesResources._0_1, "Proj1", FeaturesResources.Available)}
            {string.Format(FeaturesResources._0_1, "Proj2", FeaturesResources.Not_Available)}

            {FeaturesResources.You_can_use_the_navigation_bar_to_switch_contexts}
            """, currentParameterIndex: 0)], hideAdvancedMembers: false);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/699")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1068424")]
    public async Task TestGenericParameters1()
    {
        var markup = """
            class C
            {
                void M()
                {
                    Goo(""$$);
                }

                void Goo<T>(T a) { }
                void Goo<T, U>(T a, U b) { }
            }
            """;

        await TestAsync(markup, [
            new SignatureHelpTestItem("void C.Goo<string>(string a)", string.Empty, string.Empty, currentParameterIndex: 0),
            new SignatureHelpTestItem("void C.Goo<T, U>(T a, U b)", string.Empty)]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/699")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1068424")]
    public async Task TestGenericParameters2()
    {
        var markup = """
            class C
            {
                void M()
                {
                    Goo("", $$);
                }

                void Goo<T>(T a) { }
                void Goo<T, U>(T a, U b) { }
            }
            """;

        await TestAsync(markup, [
            new SignatureHelpTestItem("void C.Goo<T>(T a)", string.Empty),
            new SignatureHelpTestItem("void C.Goo<T, U>(T a, U b)", string.Empty, string.Empty, currentParameterIndex: 1)]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4144")]
    public async Task TestSigHelpIsVisibleOnInaccessibleItem()
    {
        var markup = """
            using System.Collections.Generic;

            class A
            {
                List<int> args;
            }

            class B : A
            {
                void M()
                {
                    args.Add($$
                }
            }
            """;

        await TestAsync(markup, [new SignatureHelpTestItem("void List<int>.Add(int item)")]);
    }

    [Fact]
    public async Task TypingTupleDoesNotDismiss1()
    {
        var markup = """
            class C
            {
                int Goo(object x)
                {
                    [|Goo(($$)|];
                }
            }
            """;

        await TestAsync(markup, [new SignatureHelpTestItem("int C.Goo(object x)", currentParameterIndex: 0)], usePreviousCharAsTrigger: true);
    }

    [Fact]
    public async Task TypingTupleDoesNotDismiss2()
    {
        var markup = """
            class C
            {
                int Goo(object x)
                {
                    [|Goo((1,$$)|];
                }
            }
            """;

        await TestAsync(markup, [new SignatureHelpTestItem("int C.Goo(object x)", currentParameterIndex: 0)], usePreviousCharAsTrigger: true);
    }

    [Fact]
    public async Task TypingTupleDoesNotDismiss3()
    {
        var markup = """
            class C
            {
                int Goo(object x)
                {
                    [|Goo((1, ($$)|];
                }
            }
            """;

        await TestAsync(markup, [new SignatureHelpTestItem("int C.Goo(object x)", currentParameterIndex: 0)], usePreviousCharAsTrigger: true);
    }

    [Fact]
    public async Task TypingTupleDoesNotDismiss4()
    {
        var markup = """
            class C
            {
                int Goo(object x)
                {
                    [|Goo((1, (2,$$)|];
                }
            }
            """;

        await TestAsync(markup, [new SignatureHelpTestItem("int C.Goo(object x)", currentParameterIndex: 0)], usePreviousCharAsTrigger: true);
    }

    [Fact]
    public async Task PickCorrectOverload_WithCorrectSelectionAfterFilteringOutNoApplicableItems()
    {
        var markup = """
            class Comparer
            {
                public static bool Equals(object x, object y) => true;
                public bool Equals(object x) => true;
                public bool Equals(string x, string y) => true;
            }

            class Program
            {
                static void Main(string x, string y)
                {
                    var comparer = new Comparer();
                    comparer.Equals(x, y$$);
                }
            }
            """;

        await TestAsync(markup, [
            new SignatureHelpTestItem("bool Comparer.Equals(object x)", currentParameterIndex: 1),
            new SignatureHelpTestItem("bool Comparer.Equals(string x, string y)", currentParameterIndex: 1, isSelected: true)]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38074")]
    [CompilerTrait(CompilerFeature.LocalFunctions)]
    public async Task TestLocalFunction()
    {
        var markup = """
            class C
            {
                void M()
                {
                    void Local() { }
                    Local($$);
                }
            }
            """;

        await TestAsync(markup, [new SignatureHelpTestItem("void Local()")]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38074")]
    [CompilerTrait(CompilerFeature.LocalFunctions)]
    public async Task TestLocalFunctionInStaticMethod()
    {
        var markup = """
            class C
            {
                static void M()
                {
                    void Local() { }
                    Local($$);
                }
            }
            """;

        await TestAsync(markup, [new SignatureHelpTestItem("void Local()")]);
    }

    [Fact]
    [CompilerTrait(CompilerFeature.FunctionPointers)]
    public async Task TestFunctionPointer()
    {
        var markup = """
            class C
            {
                unsafe static void M()
                {
                    delegate*<int, int> functionPointer;
                    functionPointer($$);
                }
            }
            """;

        await TestAsync(markup, [new SignatureHelpTestItem("int delegate*(int)", currentParameterIndex: 0)]);
    }

    [Fact]
    [CompilerTrait(CompilerFeature.FunctionPointers)]
    public async Task TestFunctionPointerMultipleArguments()
    {
        var markup = """
            class C
            {
                unsafe static void M()
                {
                    delegate*<string, long, int> functionPointer;
                    functionPointer("", $$);
                }
            }
            """;

        await TestAsync(markup, [new SignatureHelpTestItem("int delegate*(string, long)", currentParameterIndex: 1)]);
    }

    [Theory, CombinatorialData]
    public async Task ShowWarningForOverloadUnavailableInRelatedDocument(bool typeParameterProvided)
    {
        var markup = $$"""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1" PreprocessorSymbols="TFM">
                    <Document FilePath="SourceDocument"><![CDATA[
            class C
            {
                public void M(object o) => false;
            #if TFM
                public void M<T>(Action<T> arg1, T arg2, bool flag) => false;
            #endif

                void goo()
                {
                    M{{(typeParameterProvided ? "<object>" : string.Empty)}}($$
                }
            }
            ]]>
                    </Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj2">
                    <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="SourceDocument" />
                </Project>
            </Workspace>
            """;

        var expectedItems = new List<SignatureHelpTestItem>();

        if (typeParameterProvided)
        {
            // If generic method is instantiated, non-generic overloads would be excluded (description would be instantiated as well, i.e. object instead of T)
            expectedItems.Add(new SignatureHelpTestItem($"""
                void C.M<object>(Action<object> arg1, object arg2, bool flag)

                {string.Format(FeaturesResources._0_1, "Proj1", FeaturesResources.Available)}
                {string.Format(FeaturesResources._0_1, "Proj2", FeaturesResources.Not_Available)}

                {FeaturesResources.You_can_use_the_navigation_bar_to_switch_contexts}
                """, currentParameterIndex: 0));
        }
        else
        {
            expectedItems.Add(new SignatureHelpTestItem($"void C.M(object o)", currentParameterIndex: 0));
            expectedItems.Add(new SignatureHelpTestItem($"""
                void C.M<T>(Action<T> arg1, T arg2, bool flag)

                {string.Format(FeaturesResources._0_1, "Proj1", FeaturesResources.Available)}
                {string.Format(FeaturesResources._0_1, "Proj2", FeaturesResources.Not_Available)}

                {FeaturesResources.You_can_use_the_navigation_bar_to_switch_contexts}
                """, currentParameterIndex: 0));
        }

        await VerifyItemWithReferenceWorkerAsync(markup, expectedItems, hideAdvancedMembers: false);
    }

    [Fact]
    public async Task TestLightweightOverloadResolution1()
    {
        var markup = """
            class C : IResource
            {
            }

            class X
            {
                void M()
                {
                    IResourceBuilder<C> builder = null;
                    builder.WithServiceBinding(scheme: "http", env: "PORT", $$);
                }
            }

            public static class Extensions
            {
                public static IResourceBuilder<T> WithServiceBinding<T>(
                    this IResourceBuilder<T> builder, int containerPort, int? hostPort = null, string? scheme = null, string? name = null, string? env = null) where T : IResource
                {
                    return builder;
                }
            }

            public interface IResource
            {
            }

            public interface IResourceBuilder<T> where T : IResource { }
            """;

        await TestAsync(
            markup, [new SignatureHelpTestItem($"({CSharpFeaturesResources.extension}) IResourceBuilder<C> IResourceBuilder<C>.WithServiceBinding<C>(int containerPort, [int? hostPort = null], [string? scheme = null], [string? name = null], [string? env = null])", currentParameterIndex: 0)],
            sourceCodeKind: SourceCodeKind.Regular);
    }

    [Fact]
    public async Task TestLightweightOverloadResolution2()
    {
        var markup = """
            class C : IResource
            {
            }

            class X
            {
                void M()
                {
                    IResourceBuilder<C> builder = null;
                    builder.WithServiceBinding(scheme: "http", env: "PORT", $$)
                           .WithServiceBinding();
                }
            }

            public static class Extensions
            {
                public static IResourceBuilder<T> WithServiceBinding<T>(
                    this IResourceBuilder<T> builder, int containerPort, int? hostPort = null, string? scheme = null, string? name = null, string? env = null) where T : IResource
                {
                    return builder;
                }
            }

            public interface IResource
            {
            }

            public interface IResourceBuilder<T> where T : IResource { }
            """;

        await TestAsync(
            markup, [new SignatureHelpTestItem($"({CSharpFeaturesResources.extension}) IResourceBuilder<C> IResourceBuilder<C>.WithServiceBinding<C>(int containerPort, [int? hostPort = null], [string? scheme = null], [string? name = null], [string? env = null])", currentParameterIndex: 0)],
            sourceCodeKind: SourceCodeKind.Regular);
    }
}
