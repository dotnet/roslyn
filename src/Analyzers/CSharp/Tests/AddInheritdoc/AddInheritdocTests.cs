// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.AddInheritdoc;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.AddInheritdoc;

using VerifyCS = CSharpCodeFixVerifier<
    EmptyDiagnosticAnalyzer,
    AddInheritdocCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsAddInheritdoc)]
public class AddInheritdocTests
{
    private static async Task TestAsync(string initialMarkup, string expectedMarkup)
    {
        var test = new VerifyCS.Test
        {
            TestCode = initialMarkup,
            FixedCode = expectedMarkup,
            CodeActionValidationMode = CodeActionValidationMode.Full,
        };
        await test.RunAsync();
    }

    private static async Task TestMissingAsync(string initialMarkup)
        => await VerifyCS.VerifyCodeFixAsync(initialMarkup, initialMarkup);

    [Fact]
    public async Task AddMissingInheritdocOnOverridenMethod()
    {
        await TestAsync(
            """
            /// Some doc.
            public class BaseClass
            {
                /// Some doc.
                public virtual void M() { }
            }
            /// Some doc.
            public class Derived: BaseClass
            {
                public override void {|CS1591:M|}() { }
            }
            """,
            """
            /// Some doc.
            public class BaseClass
            {
                /// Some doc.
                public virtual void M() { }
            }
            /// Some doc.
            public class Derived: BaseClass
            {
                /// <inheritdoc/>
                public override void M() { }
            }
            """);
    }

    [Theory]
    [InlineData("public void {|CS1591:OtherMethod|}() { }")]
    [InlineData("public void {|CS1591:M|}() { }")]
    [InlineData("public new void {|CS1591:M|}() { }")]
    public async Task DoNotOfferOnNotOverridenMethod(string methodDefintion)
    {
        await TestMissingAsync(
        $$"""
        /// Some doc.
        public class BaseClass
        {
            /// Some doc.
            public virtual void M() { }
        }
        /// Some doc.
        public class Derived: BaseClass
        {
            {{methodDefintion}}
        }
        """);
    }

    [Fact]
    public async Task AddMissingInheritdocOnImplicitInterfaceMethod()
    {
        await TestAsync(
            """
            /// Some doc.
            public interface IInterface
            {
                /// Some doc.
                void M();
            }
            /// Some doc.
            public class MyClass: IInterface
            {
                public void {|CS1591:M|}() { }
            }
            """,
            """
            /// Some doc.
            public interface IInterface
            {
                /// Some doc.
                void M();
            }
            /// Some doc.
            public class MyClass: IInterface
            {
                /// <inheritdoc/>
                public void M() { }
            }
            """);
    }

    [Fact]
    public async Task DoNotOfferOnExplicitInterfaceMethod()
    {
        await TestMissingAsync(
            """
            /// Some doc.
            public interface IInterface
            {
                /// Some doc.
                void M();
            }
            /// Some doc.
            public class MyClass: IInterface
            {
                void IInterface.M() { }
            }
            """);
    }
    [Fact]
    public async Task AddMissingInheritdocOnOverridenProperty()
    {
        await TestAsync(
            """
            /// Some doc.
            public class BaseClass
            {
                /// Some doc.
                public virtual string P { get; set; }
            }
            /// Some doc.
            public class Derived: BaseClass
            {
                public override string {|CS1591:P|} { get; set; }
            }
            """,
            """
            /// Some doc.
            public class BaseClass
            {
                /// Some doc.
                public virtual string P { get; set; }
            }
            /// Some doc.
            public class Derived: BaseClass
            {
                /// <inheritdoc/>
                public override string P { get; set; }
            }
            """);
    }

    [Fact]
    public async Task AddMissingInheritdocOnImplicitInterfaceProperty()
    {
        await TestAsync(
            """
            /// Some doc.
            public interface IInterface
            {
                /// Some doc.
                string P { get; }
            }
            /// Some doc.
            public class MyClass: IInterface
            {
                public string {|CS1591:P|} { get; }
            }
            """,
            """
            /// Some doc.
            public interface IInterface
            {
                /// Some doc.
                string P { get; }
            }
            /// Some doc.
            public class MyClass: IInterface
            {
                /// <inheritdoc/>
                public string P { get; }
            }
            """);
    }

    [Fact]
    public async Task AddMissingInheritdocOnImplicitInterfaceEvent()
    {
        await TestAsync(
            """
            /// Some doc.
            public interface IInterface
            {
                /// Some doc.
                event System.Action SomeEvent;
            }
            /// Some doc.
            public class MyClass: IInterface
            {
                public event System.Action {|CS1591:SomeEvent|};

                void OnSomething() => SomeEvent?.Invoke();
            }
            """,
            """
            /// Some doc.
            public interface IInterface
            {
                /// Some doc.
                event System.Action SomeEvent;
            }
            /// Some doc.
            public class MyClass: IInterface
            {
                /// <inheritdoc/>
                public event System.Action SomeEvent;

                void OnSomething() => SomeEvent?.Invoke();
            }
            """);
    }

    [Fact]
    public async Task AddMissingInheritdocTriviaTest_1()
    {
        await TestAsync(
            """
            /// Some doc.
            public class BaseClass
            {
                /// Some doc.
                public virtual void M() { }
            }
            /// Some doc.
            public class Derived: BaseClass
            {
                // Comment
                public override void {|CS1591:M|}() { }
            }
            """,
            """
            /// Some doc.
            public class BaseClass
            {
                /// Some doc.
                public virtual void M() { }
            }
            /// Some doc.
            public class Derived: BaseClass
            {
                /// <inheritdoc/>
                // Comment
                public override void M() { }
            }
            """);
    }

    [Fact]
    public async Task AddMissingInheritdocTriviaTest_2()
    {
        await TestAsync(
            """
            /// Some doc.
            public class BaseClass
            {
                /// Some doc.
                public virtual void M() { }
            }
            /// Some doc.
            public class Derived: BaseClass
            {
                               // Comment 1
              /* Comment 2 */  public /* Comment 3 */ override void {|CS1591:M|} /* Comment 4 */ ()  /* Comment 5 */ { } /* Comment 6 */
            }
            """,
            """
            /// Some doc.
            public class BaseClass
            {
                /// Some doc.
                public virtual void M() { }
            }
            /// Some doc.
            public class Derived: BaseClass
            {
                               /// <inheritdoc/>
                               // Comment 1
              /* Comment 2 */  public /* Comment 3 */ override void M /* Comment 4 */ ()  /* Comment 5 */ { } /* Comment 6 */
            }
            """);
    }

    [Fact]
    public async Task AddMissingInheritdocMethodWithAttribute()
    {
        await TestAsync(
            """
            /// Some doc.
            [System.AttributeUsage(System.AttributeTargets.Method)]
            public sealed class DummyAttribute: System.Attribute
            {
            }

            /// Some doc.
            public class BaseClass
            {
                /// Some doc.
                public virtual void M() { }
            }
            /// Some doc.
            public class Derived: BaseClass
            {
                [Dummy]
                public override void {|CS1591:M|}() { }
            }
            """,
            """
            /// Some doc.
            [System.AttributeUsage(System.AttributeTargets.Method)]
            public sealed class DummyAttribute: System.Attribute
            {
            }

            /// Some doc.
            public class BaseClass
            {
                /// Some doc.
                public virtual void M() { }
            }
            /// Some doc.
            public class Derived: BaseClass
            {
                /// <inheritdoc/>
                [Dummy]
                public override void M() { }
            }
            """);
    }

    [Fact]
    public async Task AddMissingInheritdocFixAll()
    {
        await TestAsync(
            """
            /// Some doc.
            public class BaseClass
            {
                /// Some doc.
                public virtual void M() { }
                /// Some doc.
                public virtual string P { get; }
            }
            /// Some doc.
            public class Derived: BaseClass
            {
                public override void {|CS1591:M|}() { }
                public override string {|CS1591:P|} { get; }
            }
            """,
            """
            /// Some doc.
            public class BaseClass
            {
                /// Some doc.
                public virtual void M() { }
                /// Some doc.
                public virtual string P { get; }
            }
            /// Some doc.
            public class Derived: BaseClass
            {
                /// <inheritdoc/>
                public override void M() { }
                /// <inheritdoc/>
                public override string P { get; }
            }
            """);
    }
}
