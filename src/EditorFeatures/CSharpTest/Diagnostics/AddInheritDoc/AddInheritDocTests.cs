// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.AddInheritDoc;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.AddInheritDoc
{

    using VerifyCS = CSharpCodeFixVerifier<
        EmptyDiagnosticAnalyzer,
        AddInheritDocCodeFixProvider>;

    public class AddInheritDocTests
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
        {
            var test = new VerifyCS.Test
            {
                TestCode = initialMarkup,
                FixedCode = initialMarkup,
            };
            await test.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddInheritDoc)]
        public async Task AddMissingInheritDocOnOverridenMethod()
        {
            await TestAsync(
            @"
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
}",
            @"
/// Some doc.
public class BaseClass
{
    /// Some doc.
    public virtual void M() { }
}
/// Some doc.
public class Derived: BaseClass
{
    ///<inheritdoc/>
    public override void M() { }
}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsAddInheritDoc)]
        [InlineData("public void {|CS1591:OtherMethod|}() { }")]
        [InlineData("public void {|CS1591:M|}() { }")]
        [InlineData("public new void {|CS1591:M|}() { }")]
        public async Task DontOfferOnNotOverridenMethod(string methodDefintion)
        {
            await TestMissingAsync(
            $@"
/// Some doc.
public class BaseClass
{{
    /// Some doc.
    public virtual void M() {{ }}
}}
/// Some doc.
public class Derived: BaseClass
{{
    {methodDefintion}
}}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddInheritDoc)]
        public async Task AddMissingInheritDocOnImplicitInterfaceMethod()
        {
            await TestAsync(
            @"
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
}",
            @"
/// Some doc.
public interface IInterface
{
    /// Some doc.
    void M();
}
/// Some doc.
public class MyClass: IInterface
{
    ///<inheritdoc/>
    public void M() { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddInheritDoc)]
        public async Task DontOfferOnExplicitInterfaceMethod()
        {
            await TestMissingAsync(
            @"
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
}");
        }
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddInheritDoc)]
        public async Task AddMissingInheritDocOnOverridenProperty()
        {
            await TestAsync(
            @"
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
}",
            @"
/// Some doc.
public class BaseClass
{
    /// Some doc.
    public virtual string P { get; set; }
}
/// Some doc.
public class Derived: BaseClass
{
    ///<inheritdoc/>
    public override string P { get; set; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddInheritDoc)]
        public async Task AddMissingInheritDocOnImplicitInterfaceProperty()
        {
            await TestAsync(
            @"
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
}",
            @"
/// Some doc.
public interface IInterface
{
    /// Some doc.
    string P { get; }
}
/// Some doc.
public class MyClass: IInterface
{
    ///<inheritdoc/>
    public string P { get; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddInheritDoc)]
        public async Task AddMissingInheritDocTriviaTest_1()
        {
            await TestAsync(
            @"
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
}",
            @"
/// Some doc.
public class BaseClass
{
    /// Some doc.
    public virtual void M() { }
}
/// Some doc.
public class Derived: BaseClass
{
    ///<inheritdoc/>
    // Comment
    public override void M() { }
}");
        }
    }
}
