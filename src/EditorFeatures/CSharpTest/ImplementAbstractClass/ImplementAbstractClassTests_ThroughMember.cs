// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.ImplementAbstractClass;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ImplementAbstractClass
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
    public sealed class ImplementAbstractClassTests_ThroughMemberTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpImplementAbstractClassCodeFixProvider());

        private IDictionary<OptionKey, object> AllOptionsOff =>
            OptionsSet(
                 SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.NeverWithSilentEnforcement),
                 SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedConstructors, CSharpCodeStyleOptions.NeverWithSilentEnforcement),
                 SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedOperators, CSharpCodeStyleOptions.NeverWithSilentEnforcement),
                 SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithSilentEnforcement),
                 SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.NeverWithSilentEnforcement),
                 SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, CSharpCodeStyleOptions.NeverWithSilentEnforcement));

        internal Task TestAllOptionsOffAsync(
            string initialMarkup,
            string expectedMarkup,
            IDictionary<OptionKey, object> options = null,
            ParseOptions parseOptions = null)
        {
            options ??= new Dictionary<OptionKey, object>();
            foreach (var kvp in AllOptionsOff)
            {
                options.Add(kvp);
            }

            return TestInRegularAndScriptAsync(
                initialMarkup,
                expectedMarkup,
                index: 1,
                options: options,
                parseOptions: parseOptions);
        }

        [Fact, WorkItem(41420, "https://github.com/dotnet/roslyn/issues/41420")]
        public async Task FieldInBaseClassIsNotSuggested()
        {
            await TestExactActionSetOfferedAsync(
@"abstract class Base
{
    public Base Inner;

    public abstract void Method();
}

class [|Derived|] : Base
{
}", new[] { "Implement Abstract Class" });
        }

        [Fact, WorkItem(41420, "https://github.com/dotnet/roslyn/issues/41420")]
        public async Task FieldInMiddleClassIsNotSuggested()
        {
            await TestExactActionSetOfferedAsync(
@"abstract class Base
{
    public abstract void Method();
}

abstract class Middle : Base
{
    public Base Inner;
}

class [|Derived|] : Base
{
}", new[] { "Implement Abstract Class" });
        }

        [Fact, WorkItem(41420, "https://github.com/dotnet/roslyn/issues/41420")]
        public async Task FieldOfSameDerivedTypeIsSuggested()
        {
            await TestAllOptionsOffAsync(
@"abstract class Base
{
    public abstract void Method();
}

class [|Derived|] : Base
{
    Derived inner;
}",
@"abstract class Base
{
    public abstract void Method();
}

class Derived : Base
{
    Derived inner;

    public override void Method()
    {
        inner.Method();
    }
}");
        }

        [Fact, WorkItem(41420, "https://github.com/dotnet/roslyn/issues/41420")]
        public async Task FieldOfMoreSpecificTypeIsSuggested()
        {
            await TestAllOptionsOffAsync(
@"abstract class Base
{
    public abstract void Method();
}

class [|Derived|] : Base
{
    DerivedAgain inner;
}

class DerivedAgain : Derived
{
}",
@"abstract class Base
{
    public abstract void Method();
}

class Derived : Base
{
    DerivedAgain inner;

    public override void Method()
    {
        inner.Method();
    }
}

class DerivedAgain : Derived
{
}");
        }

        [Fact, WorkItem(41420, "https://github.com/dotnet/roslyn/issues/41420")]
        public async Task FieldOfConstrainedGenericTypeIsSuggested()
        {
            await TestAllOptionsOffAsync(
@"abstract class Base
{
    public abstract void Method();
}

class [|Derived|]<T> : Base where T : Base
{
    T inner;
}",
@"abstract class Base
{
    public abstract void Method();
}

class Derived<T> : Base where T : Base
{
    T inner;

    public override void Method()
    {
        inner.Method();
    }
}");
        }

        [Fact, WorkItem(41420, "https://github.com/dotnet/roslyn/issues/41420")]
        public async Task DistinguishableOptionsAreShownForExplicitPropertyWithSameName()
        {
            await TestExactActionSetOfferedAsync(
@"abstract class Base
{
    public abstract void Method();
}

interface IInterface
{
    Inner { get; }
}

class [|Derived|] : Base, IInterface
{
    Base Inner { get; }

    Base IInterface.Inner { get; }
}", new[] { "Implement Abstract Class", "Implement through 'Inner'", "Implement through 'IInterface.Inner'" });
        }

        [Fact, WorkItem(41420, "https://github.com/dotnet/roslyn/issues/41420")]
        public async Task NotOfferedForDynamicFields()
        {
            await TestExactActionSetOfferedAsync(
@"abstract class Base
{
    public abstract void Method();
}

class [|Derived|] : Base
{
    dynamic inner;
}", new[] { "Implement Abstract Class" });
        }

        [Fact, WorkItem(41420, "https://github.com/dotnet/roslyn/issues/41420")]
        public async Task OfferedForStaticFields()
        {
            await TestAllOptionsOffAsync(
@"abstract class Base
{
    public abstract void Method();
}

class [|Derived|] : Base
{
    static Base inner;
}",
@"abstract class Base
{
    public abstract void Method();
}

class Derived : Base
{
    static Base inner;

    public override void Method()
    {
        inner.Method();
    }
}");
        }

        [Fact, WorkItem(41420, "https://github.com/dotnet/roslyn/issues/41420")]
        public async Task PropertyIsDelegated()
        {
            await TestAllOptionsOffAsync(
@"abstract class Base
{
    public abstract int Property { get; set; }
}

class [|Derived|] : Base
{
    Base inner;
}",
@"abstract class Base
{
    public abstract int Property { get; set; }
}

class Derived : Base
{
    Base inner;

    public override int Property
    {
        get
        {
            return inner.Property;
        }

        set
        {
            inner.Property = value;
        }
    }
}");
        }

        [Fact, WorkItem(41420, "https://github.com/dotnet/roslyn/issues/41420")]
        public async Task PropertyWithSingleAccessorIsDelegated()
        {
            await TestAllOptionsOffAsync(
@"abstract class Base
{
    public abstract int GetOnly { get; }
    public abstract int SetOnly { set; }
}

class [|Derived|] : Base
{
    Base inner;
}",
@"abstract class Base
{
    public abstract int GetOnly { get; }
    public abstract int SetOnly { set; }
}

class Derived : Base
{
    Base inner;

    public override int GetOnly
    {
        get
        {
            return inner.GetOnly;
        }
    }

    public override int SetOnly
    {
        set
        {
            inner.SetOnly = value;
        }
    }
}");
        }

        [Fact, WorkItem(41420, "https://github.com/dotnet/roslyn/issues/41420")]
        public async Task EventIsDelegated()
        {
            await TestAllOptionsOffAsync(
@"using System;

abstract class Base
{
    public abstract event Action Event;
}

class [|Derived|] : Base
{
    Base inner;
}",
@"using System;

abstract class Base
{
    public abstract event Action Event;
}

class Derived : Base
{
    Base inner;

    public override event Action Event
    {
        add
        {
            inner.Event += value;
        }

        remove
        {
            inner.Event -= value;
        }
    }
}");
        }

        [Fact, WorkItem(41420, "https://github.com/dotnet/roslyn/issues/41420")]
        public async Task OnlyOverridableMethodsAreOverridden()
        {
            await TestAllOptionsOffAsync(
@"abstract class Base
{
    public abstract void Method();

    public void NonVirtualMethod();
}

class [|Derived|] : Base
{
    Base inner;
}",
@"abstract class Base
{
    public abstract void Method();

    public void NonVirtualMethod();
}

class Derived : Base
{
    Base inner;

    public override void Method()
    {
        inner.Method();
    }
}");
        }

        [Fact, WorkItem(41420, "https://github.com/dotnet/roslyn/issues/41420")]
        public async Task ProtectedMethodsAreOverridden()
        {
            await TestAllOptionsOffAsync(
@"abstract class Base
{
    protected abstract void Method();
}

class [|Derived|] : Base
{
    Base inner;
}",
@"abstract class Base
{
    protected abstract void Method();
}

class Derived : Base
{
    Base inner;

    protected override void Method()
    {
        inner.Method();
    }
}");
        }

        [Fact, WorkItem(41420, "https://github.com/dotnet/roslyn/issues/41420")]
        public async Task ProtectedInternalMethodsAreOverridden()
        {
            await TestAllOptionsOffAsync(
@"abstract class Base
{
    protected internal abstract void Method();
}

class [|Derived|] : Base
{
    Base inner;
}",
@"abstract class Base
{
    protected internal abstract void Method();
}

class Derived : Base
{
    Base inner;

    protected internal override void Method()
    {
        inner.Method();
    }
}");
        }

        [Fact, WorkItem(41420, "https://github.com/dotnet/roslyn/issues/41420")]
        public async Task InternalMethodsAreOverridden()
        {
            await TestAllOptionsOffAsync(
@"abstract class Base
{
    internal abstract void Method();
}

class [|Derived|] : Base
{
    Base inner;
}",
@"abstract class Base
{
    internal abstract void Method();
}

class Derived : Base
{
    Base inner;

    internal override void Method()
    {
        inner.Method();
    }
}");
        }

        [Fact, WorkItem(41420, "https://github.com/dotnet/roslyn/issues/41420")]
        public async Task PrivateProtectedMethodsAreOverridden()
        {
            await TestAllOptionsOffAsync(
@"abstract class Base
{
    private protected abstract void Method();
}

class [|Derived|] : Base
{
    Base inner;
}",
@"abstract class Base
{
    private protected abstract void Method();
}

class Derived : Base
{
    Base inner;

    private protected override void Method()
    {
        inner.Method();
    }
}");
        }
    }
}
