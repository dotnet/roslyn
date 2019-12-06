// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.SignatureHelp;
using Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

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
    int Goo(int x)
    {
        [|Goo(Goo(x)$$|]);
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("int C.Goo(int x)", currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationInsideLambda()
        {
            var markup = @"
using System;

class C
{
    void Goo(Action<int> f)
    {
        [|Goo(i => Console.WriteLine(i)$$|]);
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C.Goo(Action<int> f)", currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationInsideLambda2()
        {
            var markup = @"
using System;

class C
{
    void Goo(Action<int> f)
    {
        [|Goo(i => Con$$sole.WriteLine(i)|]);
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C.Goo(Action<int> f)", currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationWithoutParameters()
        {
            var markup = @"
class C
{
    void Goo()
    {
        [|Goo($$|]);
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C.Goo()", string.Empty, null, currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationWithoutParametersMethodXmlComments()
        {
            var markup = @"
class C
{
    /// <summary>
    /// Summary for goo
    /// </summary>
    void Goo()
    {
        [|Goo($$|]);
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C.Goo()", "Summary for goo", null, currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationWithParametersOn1()
        {
            var markup = @"
class C
{
    void Goo(int a, int b)
    {
        [|Goo($$a, b|]);
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C.Goo(int a, int b)", string.Empty, string.Empty, currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationWithParametersXmlCommentsOn1()
        {
            var markup = @"
class C
{
    /// <summary>
    /// Summary for Goo
    /// </summary>
    /// <param name=" + "\"a\">Param a</param>" + @"
    /// <param name=" + "\"b\">Param b</param>" + @"
    void Goo(int a, int b)
    {
        [|Goo($$a, b|]);
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C.Goo(int a, int b)", "Summary for Goo", "Param a", currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationWithParametersOn2()
        {
            var markup = @"
class C
{
    void Goo(int a, int b)
    {
        [|Goo(a, $$b|]);
    }
}";
            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C.Goo(int a, int b)", string.Empty, string.Empty, currentParameterIndex: 1));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationWithParametersXmlComentsOn2()
        {
            var markup = @"
class C
{
    /// <summary>
    /// Summary for Goo
    /// </summary>
    /// <param name=" + "\"a\">Param a</param>" + @"
    /// <param name=" + "\"b\">Param b</param>" + @"
    void Goo(int a, int b)
    {
        [|Goo(a, $$b|]);
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C.Goo(int a, int b)", "Summary for Goo", "Param b", currentParameterIndex: 1));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        [WorkItem(26713, "https://github.com/dotnet/roslyn/issues/26713")]
        public async Task TestDelegateParameterWithDocumentation_Invoke()
        {
            var markup = @"
class C
{
    /// <param name=""a"">Parameter docs</param>
    delegate void SomeDelegate(int a);

    void M(SomeDelegate theDelegate)
    {
        [|theDelegate($$|]);
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("void SomeDelegate(int a)", parameterDocumentation: "Parameter docs", currentParameterIndex: 0),
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        [WorkItem(26713, "https://github.com/dotnet/roslyn/issues/26713")]
        public async Task TestDelegateParameterWithDocumentation_Invoke2()
        {
            var markup = @"
class C
{
    /// <param name=""a"">Parameter docs</param>
    delegate void SomeDelegate(int a);

    void M(SomeDelegate theDelegate)
    {
        [|theDelegate.Invoke($$|]);
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("void SomeDelegate.Invoke(int a)", parameterDocumentation: "Parameter docs", currentParameterIndex: 0),
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        [WorkItem(26713, "https://github.com/dotnet/roslyn/issues/26713")]
        public async Task TestDelegateParameterWithDocumentation_BeginInvoke()
        {
            var markup = @"
class C
{
    /// <param name=""a"">Parameter docs</param>
    delegate void SomeDelegate(int a);

    void M(SomeDelegate theDelegate)
    {
        [|theDelegate.BeginInvoke($$|]);
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("System.IAsyncResult SomeDelegate.BeginInvoke(int a, System.AsyncCallback callback, object @object)", parameterDocumentation: "Parameter docs", currentParameterIndex: 0)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        [WorkItem(26713, "https://github.com/dotnet/roslyn/issues/26713")]
        public async Task TestDelegateParameterWithDocumentation_BeginInvoke2()
        {
            var markup = @"
class C
{
    /// <param name=""a"">Parameter docs</param>
    /// <param name=""callback"">This should not be displayed</param>
    delegate void SomeDelegate(int a);

    void M(SomeDelegate theDelegate)
    {
        [|theDelegate.BeginInvoke(0, $$|]);
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("System.IAsyncResult SomeDelegate.BeginInvoke(int a, System.AsyncCallback callback, object @object)", parameterDocumentation: null, currentParameterIndex: 1)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationWithoutClosingParen()
        {
            var markup = @"
class C
{
    void Goo()
    {
        [|Goo($$
    |]}
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C.Goo()", string.Empty, null, currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationWithoutClosingParenWithParameters()
        {
            var markup =
@"class C
{
    void Goo(int a, int b)
    {
        [|Goo($$a, b
    |]}
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C.Goo(int a, int b)", string.Empty, string.Empty, currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationWithoutClosingParenWithParametersOn2()
        {
            var markup = @"
class C
{
    void Goo(int a, int b)
    {
        [|Goo(a, $$b
    |]}
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C.Goo(int a, int b)", string.Empty, string.Empty, currentParameterIndex: 1));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationOnLambda()
        {
            var markup = @"
using System;

class C
{
    void Goo()
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

    void Goo()
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
            expectedOrderedItems.Add(new SignatureHelpTestItem($"({CSharpFeaturesResources.extension}) int string.ExtensionMethod(int x)", string.Empty, string.Empty, currentParameterIndex: 0));

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
        Goo($$
    }
 
    void Goo(int a = 42)
    { }
 
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void Class1.Goo([int a = 42])", string.Empty, string.Empty, currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestNoInvocationOnEventNotInCurrentClass()
        {
            var markup = @"
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
}";

            await TestAsync(markup);
        }

        [WorkItem(539712, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539712")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationOnNamedType()
        {
            var markup = @"
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
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("double C.Goo(double x)", string.Empty, string.Empty, currentParameterIndex: 0)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [WorkItem(539712, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539712")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationOnInstance()
        {
            var markup = @"
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
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("double C.Goo(double x, double y)", string.Empty, string.Empty, currentParameterIndex: 0)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [WorkItem(545118, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545118")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestStatic1()
        {
            var markup = @"
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
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("void C.Bar()", currentParameterIndex: 0)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [WorkItem(545118, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545118")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestStatic2()
        {
            var markup = @"
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
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("void C.Bar()", currentParameterIndex: 0),
                new SignatureHelpTestItem("void C.Bar(int i)", currentParameterIndex: 0)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [WorkItem(543117, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543117")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationOnAnonymousType()
        {
            var markup = @"
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
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem(
$@"void List<'a>.Add('a item)

{FeaturesResources.Anonymous_Types_colon}
    'a {FeaturesResources.is_} new {{ string Name, int Age }}",
                    methodDocumentation: string.Empty,
                    parameterDocumentation: string.Empty,
                    currentParameterIndex: 0,
                    description: $@"

{FeaturesResources.Anonymous_Types_colon}
    'a {FeaturesResources.is_} new {{ string Name, int Age }}")
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [WorkItem(968188, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/968188")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationOnBaseExpression_ProtectedAccessibility()
        {
            var markup = @"
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
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem(
@"void Base.Goo(int x)",
                    methodDocumentation: string.Empty,
                    parameterDocumentation: string.Empty,
                    currentParameterIndex: 0,
                    description: string.Empty)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [WorkItem(968188, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/968188")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationOnBaseExpression_AbstractBase()
        {
            var markup = @"
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
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem(
@"void Base.Goo(int x)",
                    methodDocumentation: string.Empty,
                    parameterDocumentation: string.Empty,
                    currentParameterIndex: 0,
                    description: string.Empty)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [WorkItem(968188, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/968188")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationOnThisExpression_ProtectedAccessibility()
        {
            var markup = @"
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
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem(
@"void Base.Goo(int x)",
                    methodDocumentation: string.Empty,
                    parameterDocumentation: string.Empty,
                    currentParameterIndex: 0,
                    description: string.Empty)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [WorkItem(968188, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/968188")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationOnThisExpression_ProtectedAccessibility_Overridden()
        {
            var markup = @"
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
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem(
@"void Derived.Goo(int x)",
                    methodDocumentation: string.Empty,
                    parameterDocumentation: string.Empty,
                    currentParameterIndex: 0,
                    description: string.Empty)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [WorkItem(968188, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/968188")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationOnThisExpression_ProtectedAccessibility_AbstractBase()
        {
            var markup = @"
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
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem(
@"void Base.Goo(int x)",
                    methodDocumentation: string.Empty,
                    parameterDocumentation: string.Empty,
                    currentParameterIndex: 0,
                    description: string.Empty)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [WorkItem(968188, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/968188")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationOnThisExpression_ProtectedAccessibility_AbstractBase_Overridden()
        {
            var markup = @"
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
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem(
@"void Derived.Goo(int x)",
                    methodDocumentation: string.Empty,
                    parameterDocumentation: string.Empty,
                    currentParameterIndex: 0,
                    description: string.Empty)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [WorkItem(968188, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/968188")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationOnBaseExpression_ProtectedInternalAccessibility()
        {
            var markup = @"
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
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem(
@"void Base.Goo(int x)",
                    methodDocumentation: string.Empty,
                    parameterDocumentation: string.Empty,
                    currentParameterIndex: 0,
                    description: string.Empty)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [WorkItem(968188, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/968188")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationOnBaseMember_ProtectedAccessibility_ThroughType()
        {
            var markup = @"
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
}";

            await TestAsync(markup, null);
        }

        [WorkItem(968188, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/968188")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationOnBaseExpression_PrivateAccessibility()
        {
            var markup = @"
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
    void Goo(int someParameter, bool something)
    {
        Goo(something: false, someParameter: $$)
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
    void Goo()
    {
        [|Goo($$|]);
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C.Goo()", string.Empty, null, currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationOnTriggerComma()
        {
            var markup = @"
class C
{
    void Goo(int a, int b)
    {
        [|Goo(23,$$|]);
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C.Goo(int a, int b)", string.Empty, string.Empty, currentParameterIndex: 1));

            await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestNoInvocationOnSpace()
        {
            var markup = @"
class C
{
    void Goo(int a, int b)
    {
        [|Goo(23, $$|]);
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
    void Goo(int a)
    {
        Goo(/*,$$*/);
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
    void Goo(int a)
    {
        Goo(//,$$
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
    void Goo(int a)
    {
        Goo("",$$"");
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
        Goo.Bar($$
    }
}";

            var referencedCode = @"
public class Goo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
    public static void Bar() 
    {
    }
}";
            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void Goo.Bar()", string.Empty, null, currentParameterIndex: 0));

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
        Goo.Bar($$
    }
}";

            var referencedCode = @"
public class Goo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public static void Bar() 
    {
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void Goo.Bar()", string.Empty, null, currentParameterIndex: 0));

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
        new Goo().Bar($$
    }
}";

            var referencedCode = @"
public class Goo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    public void Bar() 
    {
    }
}";
            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void Goo.Bar()", string.Empty, null, currentParameterIndex: 0));

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
        new Goo().Bar($$
    }
}";

            var referencedCode = @"
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
}";

            var expectedOrderedItemsMetadataReference = new List<SignatureHelpTestItem>();
            expectedOrderedItemsMetadataReference.Add(new SignatureHelpTestItem("void Goo.Bar()", string.Empty, null, currentParameterIndex: 0));

            var expectedOrderedItemsSameSolution = new List<SignatureHelpTestItem>();
            expectedOrderedItemsSameSolution.Add(new SignatureHelpTestItem("void Goo.Bar()", string.Empty, null, currentParameterIndex: 0));
            expectedOrderedItemsSameSolution.Add(new SignatureHelpTestItem("void Goo.Bar(int x)", string.Empty, string.Empty, currentParameterIndex: 0));

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
        new Goo().Bar($$
    }
}";

            var referencedCode = @"
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
}";
            var expectedOrderedItemsSameSolution = new List<SignatureHelpTestItem>();
            expectedOrderedItemsSameSolution.Add(new SignatureHelpTestItem("void Goo.Bar()", string.Empty, null, currentParameterIndex: 0));
            expectedOrderedItemsSameSolution.Add(new SignatureHelpTestItem("void Goo.Bar(int x)", string.Empty, string.Empty, currentParameterIndex: 0));

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
        new D().Goo($$
    }
}";

            var referencedCode = @"
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
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void D.Goo(int derived)", string.Empty, string.Empty, currentParameterIndex: 0));

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
        new C().Goo($$
    }
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
public class C
{
    public void Goo() 
    {
    }
}";
            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C.Goo()", string.Empty, null, currentParameterIndex: 0));

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
        new D().Goo($$
    }
}";

            var referencedCode = @"
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
}";
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

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task EditorBrowsable_BrowsableStateNeverMethodsInBaseClass()
        {
            var markup = @"
class Program : B
{
    void M()
    {
        Goo($$
    }
}";

            var referencedCode = @"
public class B
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public void Goo() 
    {
    }
}";
            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void B.Goo()", string.Empty, null, currentParameterIndex: 0));

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
        new C<int>().Goo($$
    }
}";

            var referencedCode = @"
public class C<T>
{
    public void Goo(T t) { }
    public void Goo(int i) { }
}";
            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C<int>.Goo(int t)", string.Empty, string.Empty, currentParameterIndex: 0));
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C<int>.Goo(int i)", string.Empty, string.Empty, currentParameterIndex: 0));

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
        new C<int>().Goo($$
    }
}";

            var referencedCode = @"
public class C<T>
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public void Goo(T t) { }
    public void Goo(int i) { }
}";

            var expectedOrderedItemsMetadataReference = new List<SignatureHelpTestItem>();
            expectedOrderedItemsMetadataReference.Add(new SignatureHelpTestItem("void C<int>.Goo(int i)", string.Empty, string.Empty, currentParameterIndex: 0));

            var expectedOrderedItemsSameSolution = new List<SignatureHelpTestItem>();
            expectedOrderedItemsSameSolution.Add(new SignatureHelpTestItem("void C<int>.Goo(int t)", string.Empty, string.Empty, currentParameterIndex: 0));
            expectedOrderedItemsSameSolution.Add(new SignatureHelpTestItem("void C<int>.Goo(int i)", string.Empty, string.Empty, currentParameterIndex: 0));

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
        new C<int>().Goo($$
    }
}";

            var referencedCode = @"
public class C<T>
{
    public void Goo(T t) { }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public void Goo(int i) { }
}";
            var expectedOrderedItemsMetadataReference = new List<SignatureHelpTestItem>();
            expectedOrderedItemsMetadataReference.Add(new SignatureHelpTestItem("void C<int>.Goo(int t)", string.Empty, string.Empty, currentParameterIndex: 0));

            var expectedOrderedItemsSameSolution = new List<SignatureHelpTestItem>();
            expectedOrderedItemsSameSolution.Add(new SignatureHelpTestItem("void C<int>.Goo(int t)", string.Empty, string.Empty, currentParameterIndex: 0));
            expectedOrderedItemsSameSolution.Add(new SignatureHelpTestItem("void C<int>.Goo(int i)", string.Empty, string.Empty, currentParameterIndex: 0));

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
        new C<int>().Goo($$
        
    }
}";

            var referencedCode = @"
public class C<T>
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public void Goo(T t) { }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public void Goo(int i) { }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C<int>.Goo(int t)", string.Empty, string.Empty, currentParameterIndex: 0));
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C<int>.Goo(int i)", string.Empty, string.Empty, currentParameterIndex: 0));

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
        new C<int, int>().Goo($$
    }
}";

            var referencedCode = @"
public class C<T, U>
{
    public void Goo(T t) { }
    public void Goo(U u) { }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C<int, int>.Goo(int t)", string.Empty, string.Empty, currentParameterIndex: 0));
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C<int, int>.Goo(int u)", string.Empty, string.Empty, currentParameterIndex: 0));

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
        new C<int, int>().Goo($$
    }
}";

            var referencedCode = @"
public class C<T, U>
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public void Goo(T t) { }
    public void Goo(U u) { }
}";
            var expectedOrderedItemsMetadataReference = new List<SignatureHelpTestItem>();
            expectedOrderedItemsMetadataReference.Add(new SignatureHelpTestItem("void C<int, int>.Goo(int u)", string.Empty, string.Empty, currentParameterIndex: 0));

            var expectedOrderedItemsSameSolution = new List<SignatureHelpTestItem>();
            expectedOrderedItemsSameSolution.Add(new SignatureHelpTestItem("void C<int, int>.Goo(int t)", string.Empty, string.Empty, currentParameterIndex: 0));
            expectedOrderedItemsSameSolution.Add(new SignatureHelpTestItem("void C<int, int>.Goo(int u)", string.Empty, string.Empty, currentParameterIndex: 0));

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
        new C<int, int>().Goo($$
    }
}";

            var referencedCode = @"
public class C<T, U>
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public void Goo(T t) { }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public void Goo(U u) { }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C<int, int>.Goo(int t)", string.Empty, string.Empty, currentParameterIndex: 0));
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C<int, int>.Goo(int u)", string.Empty, string.Empty, currentParameterIndex: 0));

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
    async Task Goo()
    {
        [|Goo($$|]);
    }
}";

            var description = $@"
{WorkspacesResources.Usage_colon}
  await Goo();";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem($"({CSharpFeaturesResources.awaitable}) Task C.Goo()", methodDocumentation: description, currentParameterIndex: 0));

            await TestSignatureHelpWithMscorlib45Async(markup, expectedOrderedItems, "C#");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task AwaitableMethod2()
        {
            var markup = @"
using System.Threading.Tasks;
class C
{
    async Task<Task<int>> Goo()
    {
        [|Goo($$|]);
    }
}";

            var description = $@"
{WorkspacesResources.Usage_colon}
  Task<int> x = await Goo();";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem($"({CSharpFeaturesResources.awaitable}) Task<Task<int>> C.Goo()", methodDocumentation: description, currentParameterIndex: 0));

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

        [WorkItem(530017, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530017")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task LongSignature()
        {
            var markup = @"
class C
{
    void Goo(string a, string b, string c, string d, string e, string f, string g, string h, string i, string j, string k, string l, string m, string n, string o, string p, string q, string r, string s, string t, string u, string v, string w, string x, string y, string z)
    {
        [|Goo($$|])
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem(
                    signature: "void C.Goo(string a, string b, string c, string d, string e, string f, string g, string h, string i, string j, string k, string l, string m, string n, string o, string p, string q, string r, string s, string t, string u, string v, string w, string x, string y, string z)",
                    prettyPrintedSignature: @"void C.Goo(string a, string b, string c, string d, string e, string f, string g, string h, string i, string j, 
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
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("void IGoo.Bar<T>()", currentParameterIndex: 0),
                new SignatureHelpTestItem($"({CSharpFeaturesResources.extension}) void IGoo.Bar<T1, T2>()", currentParameterIndex: 0),
            };

            // Extension methods are supported in Interactive/Script (yet).
            await TestAsync(markup, expectedOrderedItems, sourceCodeKind: SourceCodeKind.Regular);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        [WorkItem(25830, "https://github.com/dotnet/roslyn/issues/25830")]
        public async Task PickCorrectOverload_PickInt()
        {
            var markup = @"
class Program
{
    static void Main()
    {
        [|M(1$$|]);
    }
    static void M(int i) { }
    static void M(string s) { }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("void Program.M(int i)", currentParameterIndex: 0, isSelected: true),
                new SignatureHelpTestItem($"void Program.M(string s)", currentParameterIndex: 0),
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        [WorkItem(25830, "https://github.com/dotnet/roslyn/issues/25830")]
        public async Task PickCorrectOverload_PickInt_ReverseOrder()
        {
            var markup = @"
class Program
{
    static void Main()
    {
        [|M(1$$|]);
    }
    static void M(string s) { }
    static void M(int i) { }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("void Program.M(int i)", currentParameterIndex: 0, isSelected: true),
                new SignatureHelpTestItem($"void Program.M(string s)", currentParameterIndex: 0),
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        [WorkItem(25830, "https://github.com/dotnet/roslyn/issues/25830")]
        public async Task PickCorrectOverload_PickSecond()
        {
            var markup = @"
class Program
{
    static void Main()
    {
        [|M(null$$|]);
    }
    static void M(int i) { }
    static void M(string s) { }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("void Program.M(int i)", currentParameterIndex: 0),
                new SignatureHelpTestItem($"void Program.M(string s)", currentParameterIndex: 0, isSelected: true),
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        [WorkItem(25830, "https://github.com/dotnet/roslyn/issues/25830")]
        public async Task PickCorrectOverload_FilterFirst_PickIntRemaining()
        {
            var markup = @"
class D
{
    static void Main()
    {
        [|M(i: 42$$|]);
    }
    static void M(D filtered) { }
    static void M(int i) { }
    static void M(string i) { }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("void D.M(int i)", currentParameterIndex: 0, isSelected: true),
                new SignatureHelpTestItem($"void D.M(string i)", currentParameterIndex: 0),
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        [WorkItem(25830, "https://github.com/dotnet/roslyn/issues/25830")]
        public async Task PickCorrectOverload_FilterFirst_PickIntRemaining_ConversionToD()
        {
            var markup = @"
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
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("void D.M(int i)", currentParameterIndex: 0, isSelected: true),
                new SignatureHelpTestItem($"void D.M(string i)", currentParameterIndex: 0),
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        [WorkItem(25830, "https://github.com/dotnet/roslyn/issues/25830")]
        public async Task PickCorrectOverload_FilterFirst_PickIntRemaining_ReversedOrder()
        {
            var markup = @"
class D
{
    static void Main()
    {
        [|M(i: 42$$|]);
    }
    static void M(string i) { }
    static void M(int i) { }
    static void M(D filtered) { }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("void D.M(int i)", currentParameterIndex: 0, isSelected: true),
                new SignatureHelpTestItem($"void D.M(string i)", currentParameterIndex: 0),
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        [WorkItem(25830, "https://github.com/dotnet/roslyn/issues/25830")]
        public async Task PickCorrectOverload_FilterFirst_PickStringRemaining()
        {
            var markup = @"
class D
{
    static void Main()
    {
        [|M(i: null$$|]);
    }
    static void M(D filtered) { }
    static void M(int i) { }
    static void M(string i) { }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("void D.M(int i)", currentParameterIndex: 0),
                new SignatureHelpTestItem($"void D.M(string i)", currentParameterIndex: 0, isSelected: true),
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestInvocationWithCrefXmlComments()
        {
            var markup = @"
class C
{
    /// <summary>
    /// Summary for goo. See method <see cref=""Bar"" />
    /// </summary>
    void Goo()
    {
        [|Goo($$|]);
    }

    void Bar() { }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void C.Goo()", "Summary for goo. See method C.Bar()", null, currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task FieldUnavailableInOneLinkedFile()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""GOO"">
        <Document FilePath=""SourceDocument""><![CDATA[
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
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""SourceDocument""/>
    </Project>
</Workspace>";
            var expectedDescription = new SignatureHelpTestItem($"void C.bar()\r\n\r\n{string.Format(FeaturesResources._0_1, "Proj1", FeaturesResources.Available)}\r\n{string.Format(FeaturesResources._0_1, "Proj2", FeaturesResources.Not_Available)}\r\n\r\n{FeaturesResources.You_can_use_the_navigation_bar_to_switch_context}", currentParameterIndex: 0);
            await VerifyItemWithReferenceWorkerAsync(markup, new[] { expectedDescription }, false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task ExcludeFilesWithInactiveRegions()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""GOO,BAR"">
        <Document FilePath=""SourceDocument""><![CDATA[
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
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""SourceDocument"" />
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj3"" PreprocessorSymbols=""BAR"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""SourceDocument""/>
    </Project>
</Workspace>";

            var expectedDescription = new SignatureHelpTestItem($"void C.bar()\r\n\r\n{string.Format(FeaturesResources._0_1, "Proj1", FeaturesResources.Available)}\r\n{string.Format(FeaturesResources._0_1, "Proj3", FeaturesResources.Not_Available)}\r\n\r\n{FeaturesResources.You_can_use_the_navigation_bar_to_switch_context}", currentParameterIndex: 0);
            await VerifyItemWithReferenceWorkerAsync(markup, new[] { expectedDescription }, false);
        }

        [WorkItem(768697, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768697")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task InstanceAndStaticMethodsShown1()
        {
            var markup = @"
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
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("void Goo.Bar(int x)", currentParameterIndex: 0),
                new SignatureHelpTestItem("void Goo.Bar(string s)", currentParameterIndex: 0)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [WorkItem(768697, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768697")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task InstanceAndStaticMethodsShown2()
        {
            var markup = @"
class C
{
    Goo Goo;
 
    void M()
    {
        Goo.Bar($$"");
    }
}

class Goo
{
    public void Bar(int x) { }
    public static void Bar(string s) { }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("void Goo.Bar(int x)", currentParameterIndex: 0),
                new SignatureHelpTestItem("void Goo.Bar(string s)", currentParameterIndex: 0)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [WorkItem(768697, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768697")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task InstanceAndStaticMethodsShown3()
        {
            var markup = @"
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
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("void Goo.Bar(int x)", currentParameterIndex: 0),
                new SignatureHelpTestItem("void Goo.Bar(string s)", currentParameterIndex: 0)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [WorkItem(768697, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768697")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task InstanceAndStaticMethodsShown4()
        {
            var markup = @"
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
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            await TestAsync(markup, expectedOrderedItems);
        }

        [WorkItem(768697, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768697")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task InstanceAndStaticMethodsShown5()
        {
            var markup = @"
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
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            await TestAsync(markup, expectedOrderedItems);
        }

        [WorkItem(1067933, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1067933")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task InvokedWithNoToken()
        {
            var markup = @"
// goo($$";

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
        [WorkItem(1068424, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1068424")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestGenericParameters1()
        {
            var markup = @"
class C
{
    void M()
    {
        Goo(""""$$);
    }

    void Goo<T>(T a) { }
    void Goo<T, U>(T a, U b) { }
}
";

            var expectedOrderedItems = new List<SignatureHelpTestItem>()
            {
                new SignatureHelpTestItem("void C.Goo<string>(string a)", string.Empty, string.Empty, currentParameterIndex: 0),
                new SignatureHelpTestItem("void C.Goo<T, U>(T a, U b)", string.Empty)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [WorkItem(699, "https://github.com/dotnet/roslyn/issues/699")]
        [WorkItem(1068424, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1068424")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TestGenericParameters2()
        {
            var markup = @"
class C
{
    void M()
    {
        Goo("""", $$);
    }

    void Goo<T>(T a) { }
    void Goo<T, U>(T a, U b) { }
}
";

            var expectedOrderedItems = new List<SignatureHelpTestItem>()
            {
                new SignatureHelpTestItem("void C.Goo<T>(T a)", string.Empty),
                new SignatureHelpTestItem("void C.Goo<T, U>(T a, U b)", string.Empty, string.Empty, currentParameterIndex: 1)
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

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TypingTupleDoesNotDismiss1()
        {
            var markup = @"
class C
{
    int Goo(object x)
    {
        [|Goo(($$)|];
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("int C.Goo(object x)", currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TypingTupleDoesNotDismiss2()
        {
            var markup = @"
class C
{
    int Goo(object x)
    {
        [|Goo((1,$$)|];
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("int C.Goo(object x)", currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TypingTupleDoesNotDismiss3()
        {
            var markup = @"
class C
{
    int Goo(object x)
    {
        [|Goo((1, ($$)|];
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("int C.Goo(object x)", currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task TypingTupleDoesNotDismiss4()
        {
            var markup = @"
class C
{
    int Goo(object x)
    {
        [|Goo((1, (2,$$)|];
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("int C.Goo(object x)", currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task PickCorrectOverload_WithCorrectSelectionAfterFilteringOutNoApplicableItems()
        {
            var markup = @"
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
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("bool Comparer.Equals(object x)", currentParameterIndex: 1),
                new SignatureHelpTestItem("bool Comparer.Equals(string x, string y)", currentParameterIndex: 1, isSelected: true),
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [WorkItem(38074, "https://github.com/dotnet/roslyn/issues/38074")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        public async Task TestLocalFunction()
        {
            var markup = @"
class C
{
    void M()
    {
        void Local() { }
        Local($$);
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem> { new SignatureHelpTestItem("void Local()") };

            await TestAsync(markup, expectedOrderedItems);
        }

        [WorkItem(38074, "https://github.com/dotnet/roslyn/issues/38074")]
        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        public async Task TestLocalFunctionInStaticMethod()
        {
            var markup = @"
class C
{
    static void M()
    {
        void Local() { }
        Local($$);
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem> { new SignatureHelpTestItem("void Local()") };

            await TestAsync(markup, expectedOrderedItems);
        }
    }
}
