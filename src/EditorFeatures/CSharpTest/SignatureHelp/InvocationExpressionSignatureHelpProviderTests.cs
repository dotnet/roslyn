// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Editor.CSharp.SignatureHelp;
using Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Roslyn.Test.Utilities;
using Xunit;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SignatureHelp
{
    public class InvocationExpressionSignatureHelpProviderTests : AbstractCSharpSignatureHelpProviderTests
    {
        public InvocationExpressionSignatureHelpProviderTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
        }

        internal override ISignatureHelpProvider CreateSignatureHelpProvider()
        {
            return new InvocationExpressionSignatureHelpProvider();
        }

        #region "Regular tests"

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationAfterCloseParen()
        {
            var markup = @"
class C
{
    int Foo(int x)
    {
        [|Foo(Foo(x)$$|]);
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("int C.Foo(int x)", currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationInsideLambda()
        {
            var markup = @"
using System;

class C
{
    void Foo(Action<int> f)
    {
        [|Foo(i => Console.WriteLine(i)$$|]);
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C.Foo(Action<int> f)", currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationInsideLambda2()
        {
            var markup = @"
using System;

class C
{
    void Foo(Action<int> f)
    {
        [|Foo(i => Con$$sole.WriteLine(i)|]);
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C.Foo(Action<int> f)", currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationWithoutParameters()
        {
            var markup = @"
class C
{
    void Foo()
    {
        [|Foo($$|]);
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C.Foo()", string.Empty, null, currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationWithoutParametersMethodXmlComments()
        {
            var markup = @"
class C
{
    /// <summary>
    /// Summary for foo
    /// </summary>
    void Foo()
    {
        [|Foo($$|]);
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C.Foo()", "Summary for foo", null, currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationWithParametersOn1()
        {
            var markup = @"
class C
{
    void Foo(int a, int b)
    {
        [|Foo($$a, b|]);
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C.Foo(int a, int b)", string.Empty, string.Empty, currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationWithParametersXmlCommentsOn1()
        {
            var markup = @"
class C
{
    /// <summary>
    /// Summary for Foo
    /// </summary>
    /// <param name=" + "\"a\">Param a</param>" + @"
    /// <param name=" + "\"b\">Param b</param>" + @"
    void Foo(int a, int b)
    {
        [|Foo($$a, b|]);
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C.Foo(int a, int b)", "Summary for Foo", "Param a", currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationWithParametersOn2()
        {
            var markup = @"
class C
{
    void Foo(int a, int b)
    {
        [|Foo(a, $$b|]);
    }
}";
            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C.Foo(int a, int b)", string.Empty, string.Empty, currentParameterIndex: 1));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationWithParametersXmlComentsOn2()
        {
            var markup = @"
class C
{
    /// <summary>
    /// Summary for Foo
    /// </summary>
    /// <param name=" + "\"a\">Param a</param>" + @"
    /// <param name=" + "\"b\">Param b</param>" + @"
    void Foo(int a, int b)
    {
        [|Foo(a, $$b|]);
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C.Foo(int a, int b)", "Summary for Foo", "Param b", currentParameterIndex: 1));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationWithoutClosingParen()
        {
            var markup = @"
class C
{
    void Foo()
    {
        [|Foo($$
    |]}
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C.Foo()", string.Empty, null, currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationWithoutClosingParenWithParameters()
        {
            var markup =
@"class C
{
    void Foo(int a, int b)
    {
        [|Foo($$a, b
    |]}
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C.Foo(int a, int b)", string.Empty, string.Empty, currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationWithoutClosingParenWithParametersOn2()
        {
            var markup = @"
class C
{
    void Foo(int a, int b)
    {
        [|Foo(a, $$b
    |]}
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C.Foo(int a, int b)", string.Empty, string.Empty, currentParameterIndex: 1));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationOnLambda()
        {
            var markup = @"
using System;

class C
{
    void Foo()
    {
        Action<int> f = (i) => Console.WriteLine(i);
        [|f($$
    |]}
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void Action<int>(int obj)", string.Empty, string.Empty, currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationOnMemberAccessExpression()
        {
            var markup = @"
class C
{
    static void Bar(int a)
    {
    }

    void Foo()
    {
        [|C.Bar($$
    |]}
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C.Bar(int a)", string.Empty, string.Empty, currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestExtensionMethod1()
        {
            var markup = @"
using System;

class C
{
    void Method()
    {
        string s = ""Text"";
        [|s.ExtensionMethod($$
    |]}
}

public static class MyExtension
{
    public static int ExtensionMethod(this string s, int x)
    {
        return s.Length;
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem($"({CSharpFeaturesResources.Extension}) int string.ExtensionMethod(int x)", string.Empty, string.Empty, currentParameterIndex: 0));

            // TODO: Once we do the work to allow extension methods in nested types, we should change this.
            await TestAsync(markup, expectedOrderedItems, sourceCodeKind: SourceCodeKind.Regular);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestOptionalParameters()
        {
            var markup = @"
class Class1
{
    void Test()
    {
        Foo($$
    }
 
    void Foo(int a = 42)
    { }
 
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void Class1.Foo([int a = 42])", string.Empty, string.Empty, currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestNoInvocationOnEventNotInCurrentClass()
        {
            var markup = @"
using System;

class C
{
    void Foo()
    {
        D d;
        [|d.evt($$
    |]}
}

public class D
{
    public event Action evt;
}";

            await TestAsync(markup);
        }

        [WorkItem(539712)]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationOnNamedType()
        {
            var markup = @"
class Program
{
    void Main()
    {
        C.Foo($$
    }
}
class C
{
    public static double Foo(double x)
    {
        return x;
    }
    public double Foo(double x, double y)
    {
        return x + y;
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("double C.Foo(double x)", string.Empty, string.Empty, currentParameterIndex: 0)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [WorkItem(539712)]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationOnInstance()
        {
            var markup = @"
class Program
{
    void Main()
    {
        new C().Foo($$
    }
}
class C
{
    public static double Foo(double x)
    {
        return x;
    }
    public double Foo(double x, double y)
    {
        return x + y;
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("double C.Foo(double x, double y)", string.Empty, string.Empty, currentParameterIndex: 0)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [WorkItem(545118)]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestStatic1()
        {
            var markup = @"
class C
{
    static void Foo()
    {
        Bar($$
    }

    static void Bar()
    {
    }

    void Bar(int i)
    {
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("void C.Bar()", currentParameterIndex: 0)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [WorkItem(545118)]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestStatic2()
        {
            var markup = @"
class C
{
    void Foo()
    {
        Bar($$
    }

    static void Bar()
    {
    }

    void Bar(int i)
    {
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("void C.Bar()", currentParameterIndex: 0),
                new SignatureHelpTestItem("void C.Bar(int i)", currentParameterIndex: 0)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [WorkItem(543117)]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationOnAnonymousType()
        {
            var markup = @"
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        var foo = new { Name = string.Empty, Age = 30 };
        Foo(foo).Add($$);
    }

    static List<T> Foo<T>(T t)
    {
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem(
$@"void List<'a>.Add('a item)

{FeaturesResources.AnonymousTypes}
    'a {FeaturesResources.Is} new {{ string Name, int Age }}",
                    methodDocumentation: string.Empty,
                    parameterDocumentation: string.Empty,
                    currentParameterIndex: 0,
                    description: $@"

{FeaturesResources.AnonymousTypes}
    'a {FeaturesResources.Is} new {{ string Name, int Age }}")
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [WorkItem(968188)]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationOnBaseExpression_ProtectedAccessibility()
        {
            var markup = @"
using System;
public class Base
{
    protected virtual void Foo(int x) { }
}

public class Derived : Base
{
    void Test()
    {
        base.Foo($$);
    }

    protected override void Foo(int x)
    {
        throw new NotImplementedException();
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem(
@"void Base.Foo(int x)",
                    methodDocumentation: string.Empty,
                    parameterDocumentation: string.Empty,
                    currentParameterIndex: 0,
                    description: string.Empty)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [WorkItem(968188)]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationOnBaseExpression_AbstractBase()
        {
            var markup = @"
using System;
public abstract class Base
{
    protected abstract void Foo(int x);
}

public class Derived : Base
{
    void Test()
    {
        base.Foo($$);
    }

    protected override void Foo(int x)
    {
        throw new NotImplementedException();
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem(
@"void Base.Foo(int x)",
                    methodDocumentation: string.Empty,
                    parameterDocumentation: string.Empty,
                    currentParameterIndex: 0,
                    description: string.Empty)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [WorkItem(968188)]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationOnThisExpression_ProtectedAccessibility()
        {
            var markup = @"
using System;
public class Base
{
    protected virtual void Foo(int x) { }
}

public class Derived : Base
{
    void Test()
    {
        this.Foo($$);
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem(
@"void Base.Foo(int x)",
                    methodDocumentation: string.Empty,
                    parameterDocumentation: string.Empty,
                    currentParameterIndex: 0,
                    description: string.Empty)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [WorkItem(968188)]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationOnThisExpression_ProtectedAccessibility_Overridden()
        {
            var markup = @"
using System;
public class Base
{
    protected virtual void Foo(int x) { }
}

public class Derived : Base
{
    void Test()
    {
        this.Foo($$);
    }

    protected override void Foo(int x)
    {
        throw new NotImplementedException();
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem(
@"void Derived.Foo(int x)",
                    methodDocumentation: string.Empty,
                    parameterDocumentation: string.Empty,
                    currentParameterIndex: 0,
                    description: string.Empty)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [WorkItem(968188)]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationOnThisExpression_ProtectedAccessibility_AbstractBase()
        {
            var markup = @"
using System;
public abstract class Base
{
    protected abstract void Foo(int x);
}

public class Derived : Base
{
    void Test()
    {
        this.Foo($$);
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem(
@"void Base.Foo(int x)",
                    methodDocumentation: string.Empty,
                    parameterDocumentation: string.Empty,
                    currentParameterIndex: 0,
                    description: string.Empty)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [WorkItem(968188)]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationOnThisExpression_ProtectedAccessibility_AbstractBase_Overridden()
        {
            var markup = @"
using System;
public abstract class Base
{
    protected abstract void Foo(int x);
}

public class Derived : Base
{
    void Test()
    {
        this.Foo($$);
    }

    protected override void Foo(int x)
    {
        throw new NotImplementedException();
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem(
@"void Derived.Foo(int x)",
                    methodDocumentation: string.Empty,
                    parameterDocumentation: string.Empty,
                    currentParameterIndex: 0,
                    description: string.Empty)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [WorkItem(968188)]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationOnBaseExpression_ProtectedInternalAccessibility()
        {
            var markup = @"
using System;
public class Base
{
    protected internal void Foo(int x) { }
}

public class Derived : Base
{
    void Test()
    {
        base.Foo($$);
    }

    protected override void Foo(int x)
    {
        throw new NotImplementedException();
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem(
@"void Base.Foo(int x)",
                    methodDocumentation: string.Empty,
                    parameterDocumentation: string.Empty,
                    currentParameterIndex: 0,
                    description: string.Empty)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [WorkItem(968188)]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationOnBaseMember_ProtectedAccessibility_ThroughType()
        {
            var markup = @"
using System;
public class Base
{
    protected virtual void Foo(int x) { }
}

public class Derived : Base
{
    void Test()
    {
        new Base().Foo($$);
    }

    protected override void Foo(int x)
    {
        throw new NotImplementedException();
    }
}";

            await TestAsync(markup, null);
        }

        [WorkItem(968188)]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationOnBaseExpression_PrivateAccessibility()
        {
            var markup = @"
using System;
public class Base
{
    private void Foo(int x) { }
}

public class Derived : Base
{
    void Test()
    {
        base.Foo($$);
    }

    protected override void Foo(int x)
    {
        throw new NotImplementedException();
    }
}";

            await TestAsync(markup, null);
        }

        #endregion

        #region "Current Parameter Name"

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestCurrentParameterName()
        {
            var markup = @"
class C
{
    void Foo(int someParameter, bool something)
    {
        Foo(something: false, someParameter: $$)
    }
}";

            await VerifyCurrentParameterNameAsync(markup, "someParameter");
        }

        #endregion

        #region "Trigger tests"

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationOnTriggerParens()
        {
            var markup = @"
class C
{
    void Foo()
    {
        [|Foo($$|]);
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C.Foo()", string.Empty, null, currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationOnTriggerComma()
        {
            var markup = @"
class C
{
    void Foo(int a, int b)
    {
        [|Foo(23,$$|]);
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C.Foo(int a, int b)", string.Empty, string.Empty, currentParameterIndex: 1));

            await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestNoInvocationOnSpace()
        {
            var markup = @"
class C
{
    void Foo(int a, int b)
    {
        [|Foo(23, $$|]);
    }
}";

            await TestAsync(markup, usePreviousCharAsTrigger: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestTriggerCharacterInComment01()
        {
            var markup = @"
class C
{
    void Foo(int a)
    {
        Foo(/*,$$*/);
    }
}";
            await TestAsync(markup, Enumerable.Empty<SignatureHelpTestItem>(), usePreviousCharAsTrigger: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestTriggerCharacterInComment02()
        {
            var markup = @"
class C
{
    void Foo(int a)
    {
        Foo(//,$$
            );
    }
}";
            await TestAsync(markup, Enumerable.Empty<SignatureHelpTestItem>(), usePreviousCharAsTrigger: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestTriggerCharacterInString01()
        {
            var markup = @"
class C
{
    void Foo(int a)
    {
        Foo("",$$"");
    }
}";
            await TestAsync(markup, Enumerable.Empty<SignatureHelpTestItem>(), usePreviousCharAsTrigger: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void TestTriggerCharacters()
        {
            char[] expectedCharacters = { ',', '(' };
            char[] unexpectedCharacters = { ' ', '[', '<' };

            VerifyTriggerCharacters(expectedCharacters, unexpectedCharacters);
        }

        #endregion

        #region "EditorBrowsable tests"

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task EditorBrowsable_Method_BrowsableStateAlways()
        {
            var markup = @"
class Program
{
    void M()
    {
        Foo.Bar($$
    }
}";

            var referencedCode = @"
public class Foo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
    public static void Bar() 
    {
    }
}";
            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void Foo.Bar()", string.Empty, null, currentParameterIndex: 0));

            await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                                referencedCode: referencedCode,
                                                expectedOrderedItemsMetadataReference: expectedOrderedItems,
                                                expectedOrderedItemsSameSolution: expectedOrderedItems,
                                                sourceLanguage: LanguageNames.CSharp,
                                                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task EditorBrowsable_Method_BrowsableStateNever()
        {
            var markup = @"
class Program
{
    void M()
    {
        Foo.Bar($$
    }
}";

            var referencedCode = @"
public class Foo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public static void Bar() 
    {
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void Foo.Bar()", string.Empty, null, currentParameterIndex: 0));

            await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                                referencedCode: referencedCode,
                                                expectedOrderedItemsMetadataReference: new List<SignatureHelpTestItem>(),
                                                expectedOrderedItemsSameSolution: expectedOrderedItems,
                                                sourceLanguage: LanguageNames.CSharp,
                                                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task EditorBrowsable_Method_BrowsableStateAdvanced()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Foo().Bar($$
    }
}";

            var referencedCode = @"
public class Foo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    public void Bar() 
    {
    }
}";
            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void Foo.Bar()", string.Empty, null, currentParameterIndex: 0));

            await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                                referencedCode: referencedCode,
                                                expectedOrderedItemsMetadataReference: new List<SignatureHelpTestItem>(),
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

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task EditorBrowsable_Method_Overloads_OneBrowsableAlways_OneBrowsableNever()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Foo().Bar($$
    }
}";

            var referencedCode = @"
public class Foo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
    public void Bar() 
    {
    }

    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public void Bar(int x) 
    {
    }
}";

            var expectedOrderedItemsMetadataReference = new List<SignatureHelpTestItem>();
            expectedOrderedItemsMetadataReference.Add(new SignatureHelpTestItem("void Foo.Bar()", string.Empty, null, currentParameterIndex: 0));

            var expectedOrderedItemsSameSolution = new List<SignatureHelpTestItem>();
            expectedOrderedItemsSameSolution.Add(new SignatureHelpTestItem("void Foo.Bar()", string.Empty, null, currentParameterIndex: 0));
            expectedOrderedItemsSameSolution.Add(new SignatureHelpTestItem("void Foo.Bar(int x)", string.Empty, string.Empty, currentParameterIndex: 0));

            await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                                referencedCode: referencedCode,
                                                expectedOrderedItemsMetadataReference: expectedOrderedItemsMetadataReference,
                                                expectedOrderedItemsSameSolution: expectedOrderedItemsSameSolution,
                                                sourceLanguage: LanguageNames.CSharp,
                                                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task EditorBrowsable_Method_Overloads_BothBrowsableNever()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Foo().Bar($$
    }
}";

            var referencedCode = @"
public class Foo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public void Bar() 
    {
    }

    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public void Bar(int x) 
    {
    }
}";
            var expectedOrderedItemsSameSolution = new List<SignatureHelpTestItem>();
            expectedOrderedItemsSameSolution.Add(new SignatureHelpTestItem("void Foo.Bar()", string.Empty, null, currentParameterIndex: 0));
            expectedOrderedItemsSameSolution.Add(new SignatureHelpTestItem("void Foo.Bar(int x)", string.Empty, string.Empty, currentParameterIndex: 0));

            await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                                referencedCode: referencedCode,
                                                expectedOrderedItemsMetadataReference: new List<SignatureHelpTestItem>(),
                                                expectedOrderedItemsSameSolution: expectedOrderedItemsSameSolution,
                                                sourceLanguage: LanguageNames.CSharp,
                                                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task OverriddenSymbolsFilteredFromSigHelp()
        {
            var markup = @"
class Program
{
    void M()
    {
        new D().Foo($$
    }
}";

            var referencedCode = @"
public class B
{
    public virtual void Foo(int original) 
    {
    }
}

public class D : B
{
    public override void Foo(int derived) 
    {
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void D.Foo(int derived)", string.Empty, string.Empty, currentParameterIndex: 0));

            await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                                referencedCode: referencedCode,
                                                expectedOrderedItemsMetadataReference: expectedOrderedItems,
                                                expectedOrderedItemsSameSolution: expectedOrderedItems,
                                                sourceLanguage: LanguageNames.CSharp,
                                                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task EditorBrowsable_BrowsableStateAlwaysMethodInBrowsableStateNeverClass()
        {
            var markup = @"
class Program
{
    void M()
    {
        new C().Foo($$
    }
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
public class C
{
    public void Foo() 
    {
    }
}";
            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C.Foo()", string.Empty, null, currentParameterIndex: 0));

            await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                                referencedCode: referencedCode,
                                                expectedOrderedItemsMetadataReference: expectedOrderedItems,
                                                expectedOrderedItemsSameSolution: expectedOrderedItems,
                                                sourceLanguage: LanguageNames.CSharp,
                                                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task EditorBrowsable_BrowsableStateAlwaysMethodInBrowsableStateNeverBaseClass()
        {
            var markup = @"
class Program
{
    void M()
    {
        new D().Foo($$
    }
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
public class B
{
    public void Foo() 
    {
    }
}

public class D : B
{
    public void Foo(int x)
    {
    }
}";
            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("void B.Foo()", string.Empty, null, currentParameterIndex: 0),
                new SignatureHelpTestItem("void D.Foo(int x)", string.Empty, string.Empty, currentParameterIndex: 0),
            };

            await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                                referencedCode: referencedCode,
                                                expectedOrderedItemsMetadataReference: expectedOrderedItems,
                                                expectedOrderedItemsSameSolution: expectedOrderedItems,
                                                sourceLanguage: LanguageNames.CSharp,
                                                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task EditorBrowsable_BrowsableStateNeverMethodsInBaseClass()
        {
            var markup = @"
class Program : B
{
    void M()
    {
        Foo($$
    }
}";

            var referencedCode = @"
public class B
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public void Foo() 
    {
    }
}";
            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void B.Foo()", string.Empty, null, currentParameterIndex: 0));

            await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                                referencedCode: referencedCode,
                                                expectedOrderedItemsMetadataReference: new List<SignatureHelpTestItem>(),
                                                expectedOrderedItemsSameSolution: expectedOrderedItems,
                                                sourceLanguage: LanguageNames.CSharp,
                                                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task EditorBrowsable_GenericTypeCausingMethodSignatureEquality_BothBrowsableAlways()
        {
            var markup = @"
class Program
{
    void M()
    {
        new C<int>().Foo($$
    }
}";

            var referencedCode = @"
public class C<T>
{
    public void Foo(T t) { }
    public void Foo(int i) { }
}";
            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C<int>.Foo(int t)", string.Empty, string.Empty, currentParameterIndex: 0));
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C<int>.Foo(int i)", string.Empty, string.Empty, currentParameterIndex: 0));

            await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                                referencedCode: referencedCode,
                                                expectedOrderedItemsMetadataReference: expectedOrderedItems,
                                                expectedOrderedItemsSameSolution: expectedOrderedItems,
                                                sourceLanguage: LanguageNames.CSharp,
                                                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task EditorBrowsable_GenericTypeCausingMethodSignatureEquality_BrowsableMixed1()
        {
            var markup = @"
class Program
{
    void M()
    {
        new C<int>().Foo($$
    }
}";

            var referencedCode = @"
public class C<T>
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public void Foo(T t) { }
    public void Foo(int i) { }
}";

            var expectedOrderedItemsMetadataReference = new List<SignatureHelpTestItem>();
            expectedOrderedItemsMetadataReference.Add(new SignatureHelpTestItem("void C<int>.Foo(int i)", string.Empty, string.Empty, currentParameterIndex: 0));

            var expectedOrderedItemsSameSolution = new List<SignatureHelpTestItem>();
            expectedOrderedItemsSameSolution.Add(new SignatureHelpTestItem("void C<int>.Foo(int t)", string.Empty, string.Empty, currentParameterIndex: 0));
            expectedOrderedItemsSameSolution.Add(new SignatureHelpTestItem("void C<int>.Foo(int i)", string.Empty, string.Empty, currentParameterIndex: 0));

            await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                                referencedCode: referencedCode,
                                                expectedOrderedItemsMetadataReference: expectedOrderedItemsMetadataReference,
                                                expectedOrderedItemsSameSolution: expectedOrderedItemsSameSolution,
                                                sourceLanguage: LanguageNames.CSharp,
                                                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task EditorBrowsable_GenericTypeCausingMethodSignatureEquality_BrowsableMixed2()
        {
            var markup = @"
class Program
{
    void M()
    {
        new C<int>().Foo($$
    }
}";

            var referencedCode = @"
public class C<T>
{
    public void Foo(T t) { }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public void Foo(int i) { }
}";
            var expectedOrderedItemsMetadataReference = new List<SignatureHelpTestItem>();
            expectedOrderedItemsMetadataReference.Add(new SignatureHelpTestItem("void C<int>.Foo(int t)", string.Empty, string.Empty, currentParameterIndex: 0));

            var expectedOrderedItemsSameSolution = new List<SignatureHelpTestItem>();
            expectedOrderedItemsSameSolution.Add(new SignatureHelpTestItem("void C<int>.Foo(int t)", string.Empty, string.Empty, currentParameterIndex: 0));
            expectedOrderedItemsSameSolution.Add(new SignatureHelpTestItem("void C<int>.Foo(int i)", string.Empty, string.Empty, currentParameterIndex: 0));

            await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                                referencedCode: referencedCode,
                                                expectedOrderedItemsMetadataReference: expectedOrderedItemsMetadataReference,
                                                expectedOrderedItemsSameSolution: expectedOrderedItemsSameSolution,
                                                sourceLanguage: LanguageNames.CSharp,
                                                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task EditorBrowsable_GenericTypeCausingMethodSignatureEquality_BothBrowsableNever()
        {
            var markup = @"
class Program
{
    void M()
    {
        new C<int>().Foo($$
        
    }
}";

            var referencedCode = @"
public class C<T>
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public void Foo(T t) { }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public void Foo(int i) { }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C<int>.Foo(int t)", string.Empty, string.Empty, currentParameterIndex: 0));
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C<int>.Foo(int i)", string.Empty, string.Empty, currentParameterIndex: 0));

            await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                                referencedCode: referencedCode,
                                                expectedOrderedItemsMetadataReference: new List<SignatureHelpTestItem>(),
                                                expectedOrderedItemsSameSolution: expectedOrderedItems,
                                                sourceLanguage: LanguageNames.CSharp,
                                                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task EditorBrowsable_GenericType2CausingMethodSignatureEquality_BothBrowsableAlways()
        {
            var markup = @"
class Program
{
    void M()
    {
        new C<int, int>().Foo($$
    }
}";

            var referencedCode = @"
public class C<T, U>
{
    public void Foo(T t) { }
    public void Foo(U u) { }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C<int, int>.Foo(int t)", string.Empty, string.Empty, currentParameterIndex: 0));
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C<int, int>.Foo(int u)", string.Empty, string.Empty, currentParameterIndex: 0));

            await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                                referencedCode: referencedCode,
                                                expectedOrderedItemsMetadataReference: expectedOrderedItems,
                                                expectedOrderedItemsSameSolution: expectedOrderedItems,
                                                sourceLanguage: LanguageNames.CSharp,
                                                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task EditorBrowsable_GenericType2CausingMethodSignatureEquality_BrowsableMixed()
        {
            var markup = @"
class Program
{
    void M()
    {
        new C<int, int>().Foo($$
    }
}";

            var referencedCode = @"
public class C<T, U>
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public void Foo(T t) { }
    public void Foo(U u) { }
}";
            var expectedOrderedItemsMetadataReference = new List<SignatureHelpTestItem>();
            expectedOrderedItemsMetadataReference.Add(new SignatureHelpTestItem("void C<int, int>.Foo(int u)", string.Empty, string.Empty, currentParameterIndex: 0));

            var expectedOrderedItemsSameSolution = new List<SignatureHelpTestItem>();
            expectedOrderedItemsSameSolution.Add(new SignatureHelpTestItem("void C<int, int>.Foo(int t)", string.Empty, string.Empty, currentParameterIndex: 0));
            expectedOrderedItemsSameSolution.Add(new SignatureHelpTestItem("void C<int, int>.Foo(int u)", string.Empty, string.Empty, currentParameterIndex: 0));

            await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                                referencedCode: referencedCode,
                                                expectedOrderedItemsMetadataReference: expectedOrderedItemsMetadataReference,
                                                expectedOrderedItemsSameSolution: expectedOrderedItemsSameSolution,
                                                sourceLanguage: LanguageNames.CSharp,
                                                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task EditorBrowsable_GenericType2CausingMethodSignatureEquality_BothBrowsableNever()
        {
            var markup = @"
class Program
{
    void M()
    {
        new C<int, int>().Foo($$
    }
}";

            var referencedCode = @"
public class C<T, U>
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public void Foo(T t) { }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public void Foo(U u) { }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C<int, int>.Foo(int t)", string.Empty, string.Empty, currentParameterIndex: 0));
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C<int, int>.Foo(int u)", string.Empty, string.Empty, currentParameterIndex: 0));

            await TestSignatureHelpInEditorBrowsableContextsAsync(markup: markup,
                                                referencedCode: referencedCode,
                                                expectedOrderedItemsMetadataReference: new List<SignatureHelpTestItem>(),
                                                expectedOrderedItemsSameSolution: expectedOrderedItems,
                                                sourceLanguage: LanguageNames.CSharp,
                                                referencedLanguage: LanguageNames.CSharp);
        }
        #endregion

        #region "Awaitable tests"
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task AwaitableMethod()
        {
            var markup = @"
using System.Threading.Tasks;
class C
{
    async Task Foo()
    {
        [|Foo($$|]);
    }
}";

            var description = $@"
{WorkspacesResources.Usage}
  await Foo();";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem($"({CSharpFeaturesResources.Awaitable}) Task C.Foo()", methodDocumentation: description, currentParameterIndex: 0));

            await TestSignatureHelpWithMscorlib45Async(markup, expectedOrderedItems, "C#");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task AwaitableMethod2()
        {
            var markup = @"
using System.Threading.Tasks;
class C
{
    async Task<Task<int>> Foo()
    {
        [|Foo($$|]);
    }
}";

            var description = $@"
{WorkspacesResources.Usage}
  Task<int> x = await Foo();";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem($"({CSharpFeaturesResources.Awaitable}) Task<Task<int>> C.Foo()", methodDocumentation: description, currentParameterIndex: 0));

            await TestSignatureHelpWithMscorlib45Async(markup, expectedOrderedItems, "C#");
        }

        #endregion 

        [WorkItem(13849, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestSpecificity1()
        {
            var markup = @"
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
    /// <param name=""t"">Generic t</param>
    public void M(T t) { }

    /// <param name=""t"">Real t</param>
    public void M(int t) { }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("void C<int>.M(int t)", string.Empty, "Real t", currentParameterIndex: 0)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [WorkItem(530017)]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task LongSignature()
        {
            var markup = @"
class C
{
    void Foo(string a, string b, string c, string d, string e, string f, string g, string h, string i, string j, string k, string l, string m, string n, string o, string p, string q, string r, string s, string t, string u, string v, string w, string x, string y, string z)
    {
        [|Foo($$|])
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem(
                    signature: "void C.Foo(string a, string b, string c, string d, string e, string f, string g, string h, string i, string j, string k, string l, string m, string n, string o, string p, string q, string r, string s, string t, string u, string v, string w, string x, string y, string z)",
                    prettyPrintedSignature: @"void C.Foo(string a, string b, string c, string d, string e, string f, string g, string h, string i, string j, 
           string k, string l, string m, string n, string o, string p, string q, string r, string s, string t, string u, 
           string v, string w, string x, string y, string z)",
                    currentParameterIndex: 0)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task GenericExtensionMethod()
        {
            var markup = @"
interface IFoo
{
    void Bar<T>();
}

static class FooExtensions
{
    public static void Bar<T1, T2>(this IFoo foo) { }
}

class Program
{
    static void Main()
    {
        IFoo f = null;
        [|f.Bar($$
    |]}
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("void IFoo.Bar<T>()", currentParameterIndex: 0),
                new SignatureHelpTestItem($"({CSharpFeaturesResources.Extension}) void IFoo.Bar<T1, T2>()", currentParameterIndex: 0),
            };

            // Extension methods are supported in Interactive/Script (yet).
            await TestAsync(markup, expectedOrderedItems, sourceCodeKind: SourceCodeKind.Regular);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationWithCrefXmlComments()
        {
            var markup = @"
class C
{
    /// <summary>
    /// Summary for foo. See method <see cref=""Bar"" />
    /// </summary>
    void Foo()
    {
        [|Foo($$|]);
    }

    void Bar() { }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C.Foo()", "Summary for foo. See method C.Bar()", null, currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task FieldUnavailableInOneLinkedFile()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""FOO"">
        <Document FilePath=""SourceDocument""><![CDATA[
class C
{
#if FOO
    void bar()
    {
    }
#endif
    void foo()
    {
        bar($$
    }
}
]]>
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""SourceDocument""/>
    </Project>
</Workspace>";
            var expectedDescription = new SignatureHelpTestItem($"void C.bar()\r\n\r\n{string.Format(FeaturesResources.ProjectAvailability, "Proj1", FeaturesResources.Available)}\r\n{string.Format(FeaturesResources.ProjectAvailability, "Proj2", FeaturesResources.NotAvailable)}\r\n\r\n{FeaturesResources.UseTheNavigationBarToSwitchContext}", currentParameterIndex: 0);
            await VerifyItemWithReferenceWorkerAsync(markup, new[] { expectedDescription }, false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task ExcludeFilesWithInactiveRegions()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""FOO,BAR"">
        <Document FilePath=""SourceDocument""><![CDATA[
class C
{
#if FOO
    void bar()
    {
    }
#endif

#if BAR
    void foo()
    {
        bar($$
    }
#endif
}
]]>
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""SourceDocument"" />
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj3"" PreprocessorSymbols=""BAR"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""SourceDocument""/>
    </Project>
</Workspace>";

            var expectedDescription = new SignatureHelpTestItem($"void C.bar()\r\n\r\n{string.Format(FeaturesResources.ProjectAvailability, "Proj1", FeaturesResources.Available)}\r\n{string.Format(FeaturesResources.ProjectAvailability, "Proj3", FeaturesResources.NotAvailable)}\r\n\r\n{FeaturesResources.UseTheNavigationBarToSwitchContext}", currentParameterIndex: 0);
            await VerifyItemWithReferenceWorkerAsync(markup, new[] { expectedDescription }, false);
        }

        [WorkItem(768697)]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task InstanceAndStaticMethodsShown1()
        {
            var markup = @"
class C
{
    Foo Foo;
 
    void M()
    {
        Foo.Bar($$
    }
}

class Foo
{
    public void Bar(int x) { }
    public static void Bar(string s) { }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("void Foo.Bar(int x)", currentParameterIndex: 0),
                new SignatureHelpTestItem("void Foo.Bar(string s)", currentParameterIndex: 0)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [WorkItem(768697)]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task InstanceAndStaticMethodsShown2()
        {
            var markup = @"
class C
{
    Foo Foo;
 
    void M()
    {
        Foo.Bar($$"");
    }
}

class Foo
{
    public void Bar(int x) { }
    public static void Bar(string s) { }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("void Foo.Bar(int x)", currentParameterIndex: 0),
                new SignatureHelpTestItem("void Foo.Bar(string s)", currentParameterIndex: 0)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [WorkItem(768697)]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task InstanceAndStaticMethodsShown3()
        {
            var markup = @"
class C
{
    Foo Foo;
 
    void M()
    {
        Foo.Bar($$
    }
}

class Foo
{
    public static void Bar(int x) { }
    public static void Bar(string s) { }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("void Foo.Bar(int x)", currentParameterIndex: 0),
                new SignatureHelpTestItem("void Foo.Bar(string s)", currentParameterIndex: 0)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [WorkItem(768697)]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task InstanceAndStaticMethodsShown4()
        {
            var markup = @"
class C
{
    void M()
    {
        Foo x;
        x.Bar($$
    }
}

class Foo
{
    public static void Bar(int x) { }
    public static void Bar(string s) { }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            await TestAsync(markup, expectedOrderedItems);
        }

        [WorkItem(768697)]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task InstanceAndStaticMethodsShown5()
        {
            var markup = @"
class C
{
    void M()
    {
        Foo x;
        x.Bar($$
    }
}
class x { }
class Foo
{
    public static void Bar(int x) { }
    public static void Bar(string s) { }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            await TestAsync(markup, expectedOrderedItems);
        }

        [WorkItem(1067933)]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task InvokedWithNoToken()
        {
            var markup = @"
// foo($$";

            await TestAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task MethodOverloadDifferencesIgnored()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""ONE"">
        <Document FilePath=""SourceDocument""><![CDATA[
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
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"" PreprocessorSymbols=""TWO"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""SourceDocument""/>
    </Project>
</Workspace>";

            var expectedDescription = new SignatureHelpTestItem($"void C.Do(int x)", currentParameterIndex: 0);
            await VerifyItemWithReferenceWorkerAsync(markup, new[] { expectedDescription }, false);
        }

        [WorkItem(699, "https://github.com/dotnet/roslyn/issues/699")]
        [WorkItem(1068424)]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestGenericParameters1()
        {
            var markup = @"
class C
{
    void M()
    {
        Foo(""""$$);
    }

    void Foo<T>(T a) { }
    void Foo<T, U>(T a, U b) { }
}
";

            var expectedOrderedItems = new List<SignatureHelpTestItem>()
            {
                new SignatureHelpTestItem("void C.Foo<string>(string a)", string.Empty, string.Empty, currentParameterIndex: 0),
                new SignatureHelpTestItem("void C.Foo<T, U>(T a, U b)", string.Empty)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [WorkItem(699, "https://github.com/dotnet/roslyn/issues/699")]
        [WorkItem(1068424)]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestGenericParameters2()
        {
            var markup = @"
class C
{
    void M()
    {
        Foo("""", $$);
    }

    void Foo<T>(T a) { }
    void Foo<T, U>(T a, U b) { }
}
";

            var expectedOrderedItems = new List<SignatureHelpTestItem>()
            {
                new SignatureHelpTestItem("void C.Foo<T>(T a)", string.Empty),
                new SignatureHelpTestItem("void C.Foo<T, U>(T a, U b)", string.Empty, string.Empty, currentParameterIndex: 1)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [WorkItem(4144, "https://github.com/dotnet/roslyn/issues/4144")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestSigHelpIsVisibleOnInaccessibleItem()
        {
            var markup = @"
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
";

            await TestAsync(markup, new[] { new SignatureHelpTestItem("void List<int>.Add(int item)") });
        }
    }
}
