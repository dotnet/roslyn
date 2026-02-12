// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
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
    public Task TestInvocationAfterCloseParen()
        => TestAsync("""
            class C
            {
                int Goo(int x)
                {
                    [|Goo(Goo(x)$$|]);
                }
            }
            """, [new SignatureHelpTestItem("int C.Goo(int x)", currentParameterIndex: 0)]);

    [Fact]
    public Task TestInvocationInsideLambda()
        => TestAsync("""
            using System;

            class C
            {
                void Goo(Action<int> f)
                {
                    [|Goo(i => Console.WriteLine(i)$$|]);
                }
            }
            """, [new SignatureHelpTestItem("void C.Goo(Action<int> f)", currentParameterIndex: 0)]);

    [Fact]
    public Task TestInvocationInsideLambda2()
        => TestAsync("""
            using System;

            class C
            {
                void Goo(Action<int> f)
                {
                    [|Goo(i => Con$$sole.WriteLine(i)|]);
                }
            }
            """, [new SignatureHelpTestItem("void C.Goo(Action<int> f)", currentParameterIndex: 0)]);

    [Fact]
    public Task TestInvocationWithoutParameters()
        => TestAsync("""
            class C
            {
                void Goo()
                {
                    [|Goo($$|]);
                }
            }
            """, [new SignatureHelpTestItem("void C.Goo()", string.Empty, null, currentParameterIndex: 0)]);

    [Fact]
    public Task TestInvocationWithoutParametersMethodXmlComments()
        => TestAsync("""
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
            """, [new SignatureHelpTestItem("void C.Goo()", "Summary for goo", null, currentParameterIndex: 0)]);

    [Fact]
    public Task TestInvocationWithParametersOn1()
        => TestAsync("""
            class C
            {
                void Goo(int a, int b)
                {
                    [|Goo($$a, b|]);
                }
            }
            """, [new SignatureHelpTestItem("void C.Goo(int a, int b)", string.Empty, string.Empty, currentParameterIndex: 0)]);

    [Fact]
    public Task TestInvocationWithParametersXmlCommentsOn1()
        => TestAsync("""
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
            """, [new SignatureHelpTestItem("void C.Goo(int a, int b)", "Summary for Goo", "Param a", currentParameterIndex: 0)]);

    [Fact]
    public Task TestInvocationWithParametersOn2()
        => TestAsync("""
            class C
            {
                void Goo(int a, int b)
                {
                    [|Goo(a, $$b|]);
                }
            }
            """, [new SignatureHelpTestItem("void C.Goo(int a, int b)", string.Empty, string.Empty, currentParameterIndex: 1)]);

    [Fact]
    public Task TestInvocationWithParametersXmlComentsOn2()
        => TestAsync("""
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
            """, [new SignatureHelpTestItem("void C.Goo(int a, int b)", "Summary for Goo", "Param b", currentParameterIndex: 1)]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26713")]
    public Task TestDelegateParameterWithDocumentation_Invoke()
        => TestAsync("""
            class C
            {
                /// <param name="a">Parameter docs</param>
                delegate void SomeDelegate(int a);

                void M(SomeDelegate theDelegate)
                {
                    [|theDelegate($$|]);
                }
            }
            """, [new SignatureHelpTestItem("void SomeDelegate(int a)", parameterDocumentation: "Parameter docs", currentParameterIndex: 0)]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26713")]
    public Task TestDelegateParameterWithDocumentation_Invoke2()
        => TestAsync("""
            class C
            {
                /// <param name="a">Parameter docs</param>
                delegate void SomeDelegate(int a);

                void M(SomeDelegate theDelegate)
                {
                    [|theDelegate.Invoke($$|]);
                }
            }
            """, [new SignatureHelpTestItem("void SomeDelegate.Invoke(int a)", parameterDocumentation: "Parameter docs", currentParameterIndex: 0)]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26713")]
    public Task TestDelegateParameterWithDocumentation_BeginInvoke()
        => TestAsync("""
            class C
            {
                /// <param name="a">Parameter docs</param>
                delegate void SomeDelegate(int a);

                void M(SomeDelegate theDelegate)
                {
                    [|theDelegate.BeginInvoke($$|]);
                }
            }
            """, [new SignatureHelpTestItem("System.IAsyncResult SomeDelegate.BeginInvoke(int a, System.AsyncCallback callback, object @object)", parameterDocumentation: "Parameter docs", currentParameterIndex: 0)]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26713")]
    public Task TestDelegateParameterWithDocumentation_BeginInvoke2()
        => TestAsync("""
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
            """, [new SignatureHelpTestItem("System.IAsyncResult SomeDelegate.BeginInvoke(int a, System.AsyncCallback callback, object @object)", parameterDocumentation: null, currentParameterIndex: 1)]);

    [Fact]
    public Task TestInvocationWithoutClosingParen()
        => TestAsync("""
            class C
            {
                void Goo()
                {
                    [|Goo($$
                |]}
            }
            """, [new SignatureHelpTestItem("void C.Goo()", string.Empty, null, currentParameterIndex: 0)]);

    [Fact]
    public Task TestInvocationWithoutClosingParenWithParameters()
        => TestAsync("""
            class C
            {
                void Goo(int a, int b)
                {
                    [|Goo($$a, b
                |]}
            }
            """, [new SignatureHelpTestItem("void C.Goo(int a, int b)", string.Empty, string.Empty, currentParameterIndex: 0)]);

    [Fact]
    public Task TestInvocationWithoutClosingParenWithParametersOn2()
        => TestAsync("""
            class C
            {
                void Goo(int a, int b)
                {
                    [|Goo(a, $$b
                |]}
            }
            """, [new SignatureHelpTestItem("void C.Goo(int a, int b)", string.Empty, string.Empty, currentParameterIndex: 1)]);

    [Fact]
    public Task TestInvocationOnLambda()
        => TestAsync("""
            using System;

            class C
            {
                void Goo()
                {
                    Action<int> f = (i) => Console.WriteLine(i);
                    [|f($$
                |]}
            }
            """, [new SignatureHelpTestItem("void Action<int>(int obj)", string.Empty, string.Empty, currentParameterIndex: 0)]);

    [Fact]
    public Task TestInvocationOnMemberAccessExpression()
        => TestAsync("""
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
            """, [new SignatureHelpTestItem("void C.Bar(int a)", string.Empty, string.Empty, currentParameterIndex: 0)]);

    [Fact]
    public Task TestExtensionMethod1()
        => TestAsync("""
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
            """, [new SignatureHelpTestItem($"({CSharpFeaturesResources.extension}) int string.ExtensionMethod(int x)", string.Empty, string.Empty, currentParameterIndex: 0)], sourceCodeKind: SourceCodeKind.Regular);

    [Fact]
    public Task TestOptionalParameters()
        => TestAsync("""
            class Class1
            {
                void Test()
                {
                    Goo($$
                }

                void Goo(int a = 42)
                { }

            }
            """, [new SignatureHelpTestItem("void Class1.Goo([int a = 42])", string.Empty, string.Empty, currentParameterIndex: 0)]);

    [Fact]
    public Task TestNoInvocationOnEventNotInCurrentClass()
        => TestAsync("""
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539712")]
    public Task TestInvocationOnNamedType()
        => TestAsync("""
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
            """, [new SignatureHelpTestItem("double C.Goo(double x)", string.Empty, string.Empty, currentParameterIndex: 0)]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539712")]
    public Task TestInvocationOnInstance()
        => TestAsync("""
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
            """, [new SignatureHelpTestItem("double C.Goo(double x, double y)", string.Empty, string.Empty, currentParameterIndex: 0)]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545118")]
    public Task TestStatic1()
        => TestAsync("""
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
            """, [new SignatureHelpTestItem("void C.Bar()", currentParameterIndex: 0)]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545118")]
    public Task TestStatic2()
        => TestAsync("""
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
            """, [
            new SignatureHelpTestItem("void C.Bar()", currentParameterIndex: 0),
            new SignatureHelpTestItem("void C.Bar(int i)", currentParameterIndex: 0)]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543117")]
    public Task TestInvocationOnAnonymousType()
        => TestAsync("""
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
            """, [
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/968188")]
    public Task TestInvocationOnBaseExpression_ProtectedAccessibility()
        => TestAsync("""
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
            """, [new SignatureHelpTestItem(
            @"void Base.Goo(int x)",
            methodDocumentation: string.Empty,
            parameterDocumentation: string.Empty,
            currentParameterIndex: 0,
            description: string.Empty)]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/968188")]
    public Task TestInvocationOnBaseExpression_AbstractBase()
        => TestAsync("""
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
            """, [new SignatureHelpTestItem(
            @"void Base.Goo(int x)",
            methodDocumentation: string.Empty,
            parameterDocumentation: string.Empty,
            currentParameterIndex: 0,
            description: string.Empty)]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/968188")]
    public Task TestInvocationOnThisExpression_ProtectedAccessibility()
        => TestAsync("""
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
            """, [new SignatureHelpTestItem(
            @"void Base.Goo(int x)",
            methodDocumentation: string.Empty,
            parameterDocumentation: string.Empty,
            currentParameterIndex: 0,
            description: string.Empty)]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/968188")]
    public Task TestInvocationOnThisExpression_ProtectedAccessibility_Overridden()
        => TestAsync("""
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
            """, [new SignatureHelpTestItem(
            @"void Derived.Goo(int x)",
            methodDocumentation: string.Empty,
            parameterDocumentation: string.Empty,
            currentParameterIndex: 0,
            description: string.Empty)]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/968188")]
    public Task TestInvocationOnThisExpression_ProtectedAccessibility_AbstractBase()
        => TestAsync("""
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
            """, [new SignatureHelpTestItem(
            @"void Base.Goo(int x)",
            methodDocumentation: string.Empty,
            parameterDocumentation: string.Empty,
            currentParameterIndex: 0,
            description: string.Empty)]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/968188")]
    public Task TestInvocationOnThisExpression_ProtectedAccessibility_AbstractBase_Overridden()
        => TestAsync("""
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
            """, [new SignatureHelpTestItem(
            @"void Derived.Goo(int x)",
            methodDocumentation: string.Empty,
            parameterDocumentation: string.Empty,
            currentParameterIndex: 0,
            description: string.Empty)]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/968188")]
    public Task TestInvocationOnBaseExpression_ProtectedInternalAccessibility()
        => TestAsync("""
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
            """, [new SignatureHelpTestItem(
            @"void Base.Goo(int x)",
            methodDocumentation: string.Empty,
            parameterDocumentation: string.Empty,
            currentParameterIndex: 0,
            description: string.Empty)]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/968188")]
    public Task TestInvocationOnBaseMember_ProtectedAccessibility_ThroughType()
        => TestAsync("""
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
            """, null);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/968188")]
    public Task TestInvocationOnBaseExpression_PrivateAccessibility()
        => TestAsync("""
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
            """, null);

    #endregion

    #region "Current Parameter Name"

    [Fact]
    public Task TestCurrentParameterName()
        => VerifyCurrentParameterNameAsync("""
            class C
            {
                void Goo(int someParameter, bool something)
                {
                    Goo(something: false, someParameter: $$)
                }
            }
            """, "someParameter");

    #endregion

    #region "Trigger tests"

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47364")]
    public Task TestInvocationOnTriggerParens_OptionalDefaultStruct()
        => TestAsync(
            """
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
            """, [new SignatureHelpTestItem("void Program.SomeMethod([CancellationToken token = default])", string.Empty, null, currentParameterIndex: 0)], usePreviousCharAsTrigger: true);

    [Fact]
    public Task TestInvocationOnTriggerParens()
        => TestAsync("""
            class C
            {
                void Goo()
                {
                    [|Goo($$|]);
                }
            }
            """, [new SignatureHelpTestItem("void C.Goo()", string.Empty, null, currentParameterIndex: 0)], usePreviousCharAsTrigger: true);

    [Fact]
    public Task TestInvocationOnTriggerComma()
        => TestAsync("""
            class C
            {
                void Goo(int a, int b)
                {
                    [|Goo(23,$$|]);
                }
            }
            """, [new SignatureHelpTestItem("void C.Goo(int a, int b)", string.Empty, string.Empty, currentParameterIndex: 1)], usePreviousCharAsTrigger: true);

    [Fact]
    public Task TestNoInvocationOnSpace()
        => TestAsync("""
            class C
            {
                void Goo(int a, int b)
                {
                    [|Goo(23, $$|]);
                }
            }
            """, usePreviousCharAsTrigger: true);

    [Fact]
    public Task TestTriggerCharacterInComment01()
        => TestAsync("""
            class C
            {
                void Goo(int a)
                {
                    Goo(/*,$$*/);
                }
            }
            """, [], usePreviousCharAsTrigger: true);

    [Fact]
    public Task TestTriggerCharacterInComment02()
        => TestAsync("""
            class C
            {
                void Goo(int a)
                {
                    Goo(//,$$
                        );
                }
            }
            """, [], usePreviousCharAsTrigger: true);

    [Fact]
    public Task TestTriggerCharacterInString01()
        => TestAsync("""
            class C
            {
                void Goo(int a)
                {
                    Goo(",$$");
                }
            }
            """, [], usePreviousCharAsTrigger: true);

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
            new("void B.Goo()", string.Empty, null, currentParameterIndex: 0),
            new("void D.Goo(int x)", string.Empty, string.Empty, currentParameterIndex: 0),
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
    public Task AwaitableMethod()
        => TestSignatureHelpWithMscorlib45Async("""
            using System.Threading.Tasks;
            class C
            {
                async Task Goo()
                {
                    [|Goo($$|]);
                }
            }
            """, [new SignatureHelpTestItem($"({CSharpFeaturesResources.awaitable}) Task C.Goo()", methodDocumentation: string.Empty, currentParameterIndex: 0)], "C#");

    [Fact]
    public Task AwaitableMethod2()
        => TestSignatureHelpWithMscorlib45Async("""
            using System.Threading.Tasks;
            class C
            {
                async Task<Task<int>> Goo()
                {
                    [|Goo($$|]);
                }
            }
            """, [new SignatureHelpTestItem($"({CSharpFeaturesResources.awaitable}) Task<Task<int>> C.Goo()", methodDocumentation: string.Empty, currentParameterIndex: 0)], "C#");

    #endregion

    [Fact, WorkItem(13849, "DevDiv_Projects/Roslyn")]
    public Task TestSpecificity1()
        => TestAsync("""
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
            """, [new SignatureHelpTestItem("void C<int>.M(int t)", string.Empty, "Real t", currentParameterIndex: 0)]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530017")]
    public Task LongSignature()
        => TestAsync("""
            class C
            {
                void Goo(string a, string b, string c, string d, string e, string f, string g, string h, string i, string j, string k, string l, string m, string n, string o, string p, string q, string r, string s, string t, string u, string v, string w, string x, string y, string z)
                {
                    [|Goo($$|])
                }
            }
            """, [
            new SignatureHelpTestItem(
                signature: "void C.Goo(string a, string b, string c, string d, string e, string f, string g, string h, string i, string j, string k, string l, string m, string n, string o, string p, string q, string r, string s, string t, string u, string v, string w, string x, string y, string z)",
                prettyPrintedSignature: """
                void C.Goo(string a, string b, string c, string d, string e, string f, string g, string h, string i, string j, 
                           string k, string l, string m, string n, string o, string p, string q, string r, string s, string t, string u, 
                           string v, string w, string x, string y, string z)
                """,
                currentParameterIndex: 0)]);

    [Fact]
    public Task GenericExtensionMethod()
        => TestAsync("""
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
            """, [
            new SignatureHelpTestItem("void IGoo.Bar<T>()", currentParameterIndex: 0),
            new SignatureHelpTestItem($"({CSharpFeaturesResources.extension}) void IGoo.Bar<T1, T2>()", currentParameterIndex: 0)], sourceCodeKind: SourceCodeKind.Regular);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25830")]
    public Task PickCorrectOverload_PickInt()
        => TestAsync("""
            class Program
            {
                static void Main()
                {
                    [|M(1$$|]);
                }
                static void M(int i) { }
                static void M(string s) { }
            }
            """, [
            new SignatureHelpTestItem("void Program.M(int i)", currentParameterIndex: 0, isSelected: true),
            new SignatureHelpTestItem($"void Program.M(string s)", currentParameterIndex: 0)]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25830")]
    public Task PickCorrectOverload_PickInt_ReverseOrder()
        => TestAsync("""
            class Program
            {
                static void Main()
                {
                    [|M(1$$|]);
                }
                static void M(string s) { }
                static void M(int i) { }
            }
            """, [
            new SignatureHelpTestItem("void Program.M(int i)", currentParameterIndex: 0, isSelected: true),
            new SignatureHelpTestItem($"void Program.M(string s)", currentParameterIndex: 0)]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25830")]
    public Task PickCorrectOverload_PickSecond()
        => TestAsync("""
            class Program
            {
                static void Main()
                {
                    [|M(null$$|]);
                }
                static void M(int i) { }
                static void M(string s) { }
            }
            """, [
            new SignatureHelpTestItem("void Program.M(int i)", currentParameterIndex: 0),
            new SignatureHelpTestItem($"void Program.M(string s)", currentParameterIndex: 0, isSelected: true)]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25830")]
    public Task PickCorrectOverload_OtherName_PickIntRemaining()
        => TestAsync("""
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
            """, [
            new SignatureHelpTestItem("void D.M(int i)", currentParameterIndex: 0, isSelected: true),
            new SignatureHelpTestItem("void D.M(string i)", currentParameterIndex: 0)]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25830")]
    public Task PickCorrectOverload_OtherName_PickIntRemaining_ConversionToD()
        => TestAsync("""
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
            """, [
            new SignatureHelpTestItem("void D.M(int i)", currentParameterIndex: 0, isSelected: true),
            new SignatureHelpTestItem("void D.M(string i)", currentParameterIndex: 0)]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25830")]
    public Task PickCorrectOverload_OtherName_PickIntRemaining_ReversedOrder()
        => TestAsync("""
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
            """, [
            new SignatureHelpTestItem("void D.M(int i)", currentParameterIndex: 0, isSelected: true),
            new SignatureHelpTestItem("void D.M(string i)", currentParameterIndex: 0)]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25830")]
    public Task PickCorrectOverload_OtherName_PickStringRemaining()
        => TestAsync("""
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
            """, [
            new SignatureHelpTestItem("void D.M(int i)", currentParameterIndex: 0),
            new SignatureHelpTestItem("void D.M(string i)", currentParameterIndex: 0, isSelected: true)]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25830")]
    public Task PickCorrectOverload_RefKind()
        => TestAsync("""
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
            """, [
            new SignatureHelpTestItem("void D.M(ref int a, int i)", currentParameterIndex: 0),
            new SignatureHelpTestItem("void D.M(out int b, int i)", currentParameterIndex: 0, isSelected: true)]);

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
    public Task PickCorrectOverload_Params_NonArrayType()
        => TestAsync("""
            class Program
            {
                void Main()
                {
                    [|M(1, 2$$|]);
                }
                void M(int i1, params int i2) { }
            }
            """, [new SignatureHelpTestItem("void Program.M(int i1, params int i2)", currentParameterIndex: 1, isSelected: true)]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/6713")]
    public Task PickCorrectOverload_Incomplete_OutOfPositionArgument()
        => TestAsync("""
            class Program
            {
                static void Main()
                {
                    [|M(string.Empty, s3: string.Empty, $$|]);
                }
                static void M(string s1, string s2, string s3) { }
            }
            """, [new SignatureHelpTestItem($"void Program.M(string s1, string s2, string s3)", currentParameterIndex: 1, isSelected: true)]);

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
    public Task TestInvocationWithCrefXmlComments()
        => TestAsync("""
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
            """, [new SignatureHelpTestItem("void C.Goo()", "Summary for goo. See method C.Bar()", null, currentParameterIndex: 0)]);

    [Fact]
    public Task FieldUnavailableInOneLinkedFile()
        => VerifyItemWithReferenceWorkerAsync("""
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
            """, [new SignatureHelpTestItem($"""
            void C.bar()

                {string.Format(FeaturesResources._0_1, "Proj1", FeaturesResources.Available)}
                {string.Format(FeaturesResources._0_1, "Proj2", FeaturesResources.Not_Available)}

            {FeaturesResources.You_can_use_the_navigation_bar_to_switch_contexts}
            """, currentParameterIndex: 0)], hideAdvancedMembers: false);

    [Fact]
    public Task ExcludeFilesWithInactiveRegions()
        => VerifyItemWithReferenceWorkerAsync("""
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
            """, [new SignatureHelpTestItem($"""
            void C.bar()

                {string.Format(FeaturesResources._0_1, "Proj1", FeaturesResources.Available)}
                {string.Format(FeaturesResources._0_1, "Proj3", FeaturesResources.Not_Available)}

            {FeaturesResources.You_can_use_the_navigation_bar_to_switch_contexts}
            """, currentParameterIndex: 0)], hideAdvancedMembers: false);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768697")]
    public Task InstanceAndStaticMethodsShown1()
        => TestAsync("""
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
            """, [
            new SignatureHelpTestItem("void Goo.Bar(int x)", currentParameterIndex: 0),
            new SignatureHelpTestItem("void Goo.Bar(string s)", currentParameterIndex: 0)]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768697")]
    public Task InstanceAndStaticMethodsShown2()
        => TestAsync("""
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
            """, [
            new SignatureHelpTestItem("void Goo.Bar(int x)", currentParameterIndex: 0),
            new SignatureHelpTestItem("void Goo.Bar(string s)", currentParameterIndex: 0)]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768697")]
    public Task InstanceAndStaticMethodsShown3()
        => TestAsync("""
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
            """, [
            new SignatureHelpTestItem("void Goo.Bar(int x)", currentParameterIndex: 0),
            new SignatureHelpTestItem("void Goo.Bar(string s)", currentParameterIndex: 0)]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768697")]
    public Task InstanceAndStaticMethodsShown4()
        => TestAsync("""
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
            """, []);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768697")]
    public Task InstanceAndStaticMethodsShown5()
        => TestAsync("""
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
            """, []);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33549")]
    public Task ShowOnlyStaticMethodsForBuildInTypes()
        => TestAsync("""
            class C
            {
                void M()
                {
                    string.Equals($$
                }
            }
            """, [
            new SignatureHelpTestItem("bool object.Equals(object objA, object objB)"),
            new SignatureHelpTestItem("bool string.Equals(string a, string b)"),
            new SignatureHelpTestItem("bool string.Equals(string a, string b, System.StringComparison comparisonType)")]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23133")]
    public Task ShowOnlyStaticMethodsForNotImportedTypes()
        => TestAsync("""
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
            """, [new SignatureHelpTestItem("void Test.Goo.Bar(string s)")]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1067933")]
    public Task InvokedWithNoToken()
        => TestAsync("""
            // goo($$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public Task MethodOverloadDifferencesIgnored()
        => VerifyItemWithReferenceWorkerAsync("""
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
            """, [new SignatureHelpTestItem($"""
            void C.Do(int x)

                {string.Format(FeaturesResources._0_1, "Proj1", FeaturesResources.Available)}
                {string.Format(FeaturesResources._0_1, "Proj2", FeaturesResources.Not_Available)}

            {FeaturesResources.You_can_use_the_navigation_bar_to_switch_contexts}
            """, currentParameterIndex: 0)], hideAdvancedMembers: false);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/699")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1068424")]
    public Task TestGenericParameters1()
        => TestAsync("""
            class C
            {
                void M()
                {
                    Goo(""$$);
                }

                void Goo<T>(T a) { }
                void Goo<T, U>(T a, U b) { }
            }
            """, [
            new SignatureHelpTestItem("void C.Goo<string>(string a)", string.Empty, string.Empty, currentParameterIndex: 0),
            new SignatureHelpTestItem("void C.Goo<T, U>(T a, U b)", string.Empty)]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79733")]
    public Task NoSignatureHelpInsideLambdaStatementBlock()
        => TestAsync("""
            class C
            {
                void M1(Action a) { }
                void M2()
                {
                    M1(() => 
                    {
                        $$
                    });
                }
            }
            """, []);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79733")]
    public Task NoSignatureHelpInsideAnonymousMethodBlock()
        => TestAsync("""
            class C
            {
                void M1(Action a) { }
                void M2()
                {
                    M1(delegate 
                    {
                        $$
                    });
                }
            }
            """, []);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79733")]
    public Task NoSignatureHelpInsideLambdaExpressionBody()
        => TestAsync("""
            class C
            {
                void M1(Action a) { }
                void M2()
                {
                    M1(() => ($$);
                }
            }
            """, []);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/699")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1068424")]
    public Task TestGenericParameters2()
        => TestAsync("""
            class C
            {
                void M()
                {
                    Goo("", $$);
                }

                void Goo<T>(T a) { }
                void Goo<T, U>(T a, U b) { }
            }
            """, [
            new SignatureHelpTestItem("void C.Goo<T>(T a)", string.Empty),
            new SignatureHelpTestItem("void C.Goo<T, U>(T a, U b)", string.Empty, string.Empty, currentParameterIndex: 1)]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4144")]
    public Task TestSigHelpIsVisibleOnInaccessibleItem()
        => TestAsync("""
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
            """, [new SignatureHelpTestItem("void List<int>.Add(int item)")]);

    [Fact]
    public Task TypingTupleDoesNotDismiss1()
        => TestAsync("""
            class C
            {
                int Goo(object x)
                {
                    [|Goo(($$)|];
                }
            }
            """, [new SignatureHelpTestItem("int C.Goo(object x)", currentParameterIndex: 0)], usePreviousCharAsTrigger: true);

    [Fact]
    public Task TypingTupleDoesNotDismiss2()
        => TestAsync("""
            class C
            {
                int Goo(object x)
                {
                    [|Goo((1,$$)|];
                }
            }
            """, [new SignatureHelpTestItem("int C.Goo(object x)", currentParameterIndex: 0)], usePreviousCharAsTrigger: true);

    [Fact]
    public Task TypingTupleDoesNotDismiss3()
        => TestAsync("""
            class C
            {
                int Goo(object x)
                {
                    [|Goo((1, ($$)|];
                }
            }
            """, [new SignatureHelpTestItem("int C.Goo(object x)", currentParameterIndex: 0)], usePreviousCharAsTrigger: true);

    [Fact]
    public Task TypingTupleDoesNotDismiss4()
        => TestAsync("""
            class C
            {
                int Goo(object x)
                {
                    [|Goo((1, (2,$$)|];
                }
            }
            """, [new SignatureHelpTestItem("int C.Goo(object x)", currentParameterIndex: 0)], usePreviousCharAsTrigger: true);

    [Fact]
    public Task PickCorrectOverload_WithCorrectSelectionAfterFilteringOutNoApplicableItems()
        => TestAsync("""
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
            """, [
            new SignatureHelpTestItem("bool Comparer.Equals(object x)", currentParameterIndex: 1),
            new SignatureHelpTestItem("bool Comparer.Equals(string x, string y)", currentParameterIndex: 1, isSelected: true)]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38074")]
    [CompilerTrait(CompilerFeature.LocalFunctions)]
    public Task TestLocalFunction()
        => TestAsync("""
            class C
            {
                void M()
                {
                    void Local() { }
                    Local($$);
                }
            }
            """, [new SignatureHelpTestItem("void Local()")]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38074")]
    [CompilerTrait(CompilerFeature.LocalFunctions)]
    public Task TestLocalFunctionInStaticMethod()
        => TestAsync("""
            class C
            {
                static void M()
                {
                    void Local() { }
                    Local($$);
                }
            }
            """, [new SignatureHelpTestItem("void Local()")]);

    [Fact]
    [CompilerTrait(CompilerFeature.FunctionPointers)]
    public Task TestFunctionPointer()
        => TestAsync("""
            class C
            {
                unsafe static void M()
                {
                    delegate*<int, int> functionPointer;
                    functionPointer($$);
                }
            }
            """, [new SignatureHelpTestItem("int delegate*(int)", currentParameterIndex: 0)]);

    [Fact]
    [CompilerTrait(CompilerFeature.FunctionPointers)]
    public Task TestFunctionPointerMultipleArguments()
        => TestAsync("""
            class C
            {
                unsafe static void M()
                {
                    delegate*<string, long, int> functionPointer;
                    functionPointer("", $$);
                }
            }
            """, [new SignatureHelpTestItem("int delegate*(string, long)", currentParameterIndex: 1)]);

    [Theory, CombinatorialData]
    public async Task ShowWarningForOverloadUnavailableInRelatedDocument(bool typeParameterProvided)
    {
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

        await VerifyItemWithReferenceWorkerAsync($$"""
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
            """, expectedItems, hideAdvancedMembers: false);
    }

    [Fact]
    public Task TestLightweightOverloadResolution1()
        => TestAsync(
            """
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
            """, [new SignatureHelpTestItem($"({CSharpFeaturesResources.extension}) IResourceBuilder<C> IResourceBuilder<C>.WithServiceBinding<C>(int containerPort, [int? hostPort = null], [string? scheme = null], [string? name = null], [string? env = null])", currentParameterIndex: 0)],
            sourceCodeKind: SourceCodeKind.Regular);

    [Fact]
    public Task TestLightweightOverloadResolution2()
        => TestAsync(
            """
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
            """, [new SignatureHelpTestItem($"({CSharpFeaturesResources.extension}) IResourceBuilder<C> IResourceBuilder<C>.WithServiceBinding<C>(int containerPort, [int? hostPort = null], [string? scheme = null], [string? name = null], [string? env = null])", currentParameterIndex: 0)],
            sourceCodeKind: SourceCodeKind.Regular);
}
