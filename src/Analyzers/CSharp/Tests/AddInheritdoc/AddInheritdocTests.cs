// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.AddInheritdoc;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.AddInheritdoc;

using VerifyCS = CSharpCodeFixVerifier<
    EmptyDiagnosticAnalyzer,
    AddInheritdocCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsAddInheritdoc)]
public sealed class AddInheritdocTests
{
    private static Task TestAsync(string initialMarkup, string expectedMarkup)
        => new VerifyCS.Test
        {
            TestCode = initialMarkup,
            FixedCode = expectedMarkup,
            CodeActionValidationMode = CodeActionValidationMode.Full,
        }.RunAsync();

    private static Task TestMissingAsync(string initialMarkup)
        => VerifyCS.VerifyCodeFixAsync(initialMarkup, initialMarkup);

    [Fact]
    public Task AddMissingInheritdocOnOverriddenMethod()
        => TestAsync(
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

    [Theory]
    [InlineData("public void {|CS1591:OtherMethod|}() { }")]
    [InlineData("public void {|CS1591:M|}() { }")]
    [InlineData("public new void {|CS1591:M|}() { }")]
    public Task DoNotOfferOnNotOverriddenMethod(string methodDefinition)
        => TestMissingAsync(
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
            {{methodDefinition}}
        }
        """);

    [Fact]
    public Task AddMissingInheritdocOnImplicitInterfaceMethod()
        => TestAsync(
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

    [Fact]
    public Task DoNotOfferOnExplicitInterfaceMethod()
        => TestMissingAsync(
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
    [Fact]
    public Task AddMissingInheritdocOnOverriddenProperty()
        => TestAsync(
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

    [Fact]
    public Task AddMissingInheritdocOnImplicitInterfaceProperty()
        => TestAsync(
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

    [Fact]
    public Task AddMissingInheritdocOnImplicitInterfaceEvent()
        => TestAsync(
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

    [Fact]
    public Task AddMissingInheritdocTriviaTest_1()
        => TestAsync(
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
                // Comment
                /// <inheritdoc/>
                public override void M() { }
            }
            """);

    [Fact]
    public Task AddMissingInheritdocTriviaTest_2()
        => TestAsync(
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
                               // Comment 1
              /* Comment 2 */  /// <inheritdoc/>
              public /* Comment 3 */ override void M /* Comment 4 */ ()  /* Comment 5 */ { } /* Comment 6 */
            }
            """);

    [Fact]
    public Task AddMissingInheritdocMethodWithAttribute()
        => TestAsync(
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

    [Fact]
    public Task AddMissingInheritdocFixAll()
        => TestAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61562")]
    public Task TestOverrideBelowMember()
        => TestAsync(
            """
            using System;

            /// <summary>
            /// Hello C1
            /// </summary>
            public abstract class Class1
            {
                /// <summary>
                /// Hello C1.DoStuff
                /// </summary>
                public abstract void DoStuff();
            }

            /// <summary>
            /// Hello C2
            /// </summary>
            public class Class2 : Class1
            {
                private const int Number = 1;

                public override void {|CS1591:DoStuff|}()
                {
                    throw new NotImplementedException();
                }
            }
            """,
            """
            using System;

            /// <summary>
            /// Hello C1
            /// </summary>
            public abstract class Class1
            {
                /// <summary>
                /// Hello C1.DoStuff
                /// </summary>
                public abstract void DoStuff();
            }
            
            /// <summary>
            /// Hello C2
            /// </summary>
            public class Class2 : Class1
            {
                private const int Number = 1;
            
                /// <inheritdoc/>
                public override void {|CS1591:DoStuff|}()
                {
                    throw new NotImplementedException();
                }
            }
            """);
}
