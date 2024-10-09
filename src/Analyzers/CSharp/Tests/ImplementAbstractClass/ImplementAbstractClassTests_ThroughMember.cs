// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.ImplementAbstractClass;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ImplementAbstractClass;

[Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
public sealed class ImplementAbstractClassTests_ThroughMemberTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
{
    public ImplementAbstractClassTests_ThroughMemberTests(ITestOutputHelper logger)
      : base(logger)
    {
    }

    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (null, new CSharpImplementAbstractClassCodeFixProvider());

    private OptionsCollection AllOptionsOff
        => new OptionsCollection(GetLanguage())
        {
             { CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
             { CSharpCodeStyleOptions.PreferExpressionBodiedConstructors, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
             { CSharpCodeStyleOptions.PreferExpressionBodiedOperators, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
             { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
             { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
             { CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
        };

    internal Task TestAllOptionsOffAsync(
        string initialMarkup,
        string expectedMarkup,
        OptionsCollection options = null,
        ParseOptions parseOptions = null)
    {
        options ??= new OptionsCollection(GetLanguage());
        options.AddRange(AllOptionsOff);

        return TestInRegularAndScriptAsync(
            initialMarkup,
            expectedMarkup,
            index: 1,
            options: options,
            parseOptions: parseOptions);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41420")]
    public async Task FieldInBaseClassIsNotSuggested()
    {
        await TestExactActionSetOfferedAsync(
            """
            abstract class Base
            {
                public Base Inner;

                public abstract void Method();
            }

            class [|Derived|] : Base
            {
            }
            """, [AnalyzersResources.Implement_abstract_class]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41420")]
    public async Task FieldInMiddleClassIsNotSuggested()
    {
        await TestExactActionSetOfferedAsync(
            """
            abstract class Base
            {
                public abstract void Method();
            }

            abstract class Middle : Base
            {
                public Base Inner;
            }

            class [|Derived|] : Base
            {
            }
            """, [AnalyzersResources.Implement_abstract_class]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41420")]
    public async Task FieldOfSameDerivedTypeIsSuggested()
    {
        await TestInRegularAndScriptAsync(
            """
            abstract class Base
            {
                public abstract void Method();
            }

            class [|Derived|] : Base
            {
                Derived inner;
            }
            """,
            """
            abstract class Base
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
            }
            """, index: 1, title: string.Format(AnalyzersResources.Implement_through_0, "inner"));
    }

    [Fact]
    public async Task RefParameters_Method()
    {
        await TestInRegularAndScriptAsync(
            """
            abstract class Base
            {
                public abstract void Method(int a, ref int b, in int c, ref readonly int d, out int e);
            }

            class [|Derived|] : Base
            {
                Derived inner;
            }
            """,
            """
            abstract class Base
            {
                public abstract void Method(int a, ref int b, in int c, ref readonly int d, out int e);
            }

            class Derived : Base
            {
                Derived inner;

                public override void Method(int a, ref int b, in int c, ref readonly int d, out int e)
                {
                    inner.Method(a, ref b, c, in d, out e);
                }
            }
            """, index: 1, title: string.Format(AnalyzersResources.Implement_through_0, "inner"));
    }

    [Fact]
    public async Task RefParameters_Indexer()
    {
        await TestInRegularAndScriptAsync(
            """
            abstract class Base
            {
                public abstract int this[int a, in int b, ref readonly int c, out int d] { get; }
            }

            class [|Derived|] : Base
            {
                Derived inner;
            }
            """,
            """
            abstract class Base
            {
                public abstract int this[int a, in int b, ref readonly int c, out int d] { get; }
            }

            class Derived : Base
            {
                Derived inner;

                public override int this[int a, in int b, ref readonly int c, out int d] => inner[a, b, in c, out d];
            }
            """, index: 1, title: string.Format(AnalyzersResources.Implement_through_0, "inner"));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41420")]
    public async Task SkipInaccessibleMember()
    {
        await TestInRegularAndScriptAsync(
            """
            abstract class Base
            {
                public abstract void Method1();
                protected abstract void Method2();
            }

            class [|Derived|] : Base
            {
                Base inner;
            }
            """,
            """
            abstract class Base
            {
                public abstract void Method1();
                protected abstract void Method2();
            }

            class {|Conflict:Derived|} : Base
            {
                Base inner;

                public override void Method1()
                {
                    inner.Method1();
                }
            }
            """, index: 1, title: string.Format(AnalyzersResources.Implement_through_0, "inner"));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41420")]
    public async Task TestNotOfferedWhenOnlyUnimplementedMemberIsInaccessible()
    {
        await TestExactActionSetOfferedAsync(
            """
            abstract class Base
            {
                public abstract void Method1();
                protected abstract void Method2();
            }

            class [|Derived|] : Base
            {
                Base inner;

                public override void Method1()
                {
                    inner.Method1();
                }
            }
            """, new string[] { AnalyzersResources.Implement_abstract_class });
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41420")]
    public async Task FieldOfMoreSpecificTypeIsSuggested()
    {
        await TestInRegularAndScriptAsync(
            """
            abstract class Base
            {
                public abstract void Method();
            }

            class [|Derived|] : Base
            {
                DerivedAgain inner;
            }

            class DerivedAgain : Derived
            {
            }
            """,
            """
            abstract class Base
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
            }
            """, index: 1, title: string.Format(AnalyzersResources.Implement_through_0, "inner"));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41420")]
    public async Task FieldOfConstrainedGenericTypeIsSuggested()
    {
        await TestInRegularAndScriptAsync(
            """
            abstract class Base
            {
                public abstract void Method();
            }

            class [|Derived|]<T> : Base where T : Base
            {
                T inner;
            }
            """,
            """
            abstract class Base
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
            }
            """, index: 1, title: string.Format(AnalyzersResources.Implement_through_0, "inner"));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41420")]
    public async Task DistinguishableOptionsAreShownForExplicitPropertyWithSameName()
    {
        await TestExactActionSetOfferedAsync(
            """
            abstract class Base
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
            }
            """,
            [
                AnalyzersResources.Implement_abstract_class,
                string.Format(AnalyzersResources.Implement_through_0, "Inner"),
                string.Format(AnalyzersResources.Implement_through_0, "IInterface.Inner"),
            ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41420")]
    public async Task NotOfferedForDynamicFields()
    {
        await TestExactActionSetOfferedAsync(
            """
            abstract class Base
            {
                public abstract void Method();
            }

            class [|Derived|] : Base
            {
                dynamic inner;
            }
            """, [AnalyzersResources.Implement_abstract_class]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41420")]
    public async Task OfferedForStaticFields()
    {
        await TestInRegularAndScriptAsync(
            """
            abstract class Base
            {
                public abstract void Method();
            }

            class [|Derived|] : Base
            {
                static Base inner;
            }
            """,
            """
            abstract class Base
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
            }
            """, index: 1, title: string.Format(AnalyzersResources.Implement_through_0, "inner"));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41420")]
    public async Task PropertyIsDelegated()
    {
        await TestInRegularAndScriptAsync(
            """
            abstract class Base
            {
                public abstract int Property { get; set; }
            }

            class [|Derived|] : Base
            {
                Base inner;
            }
            """,
            """
            abstract class Base
            {
                public abstract int Property { get; set; }
            }

            class Derived : Base
            {
                Base inner;

                public override int Property { get => inner.Property; set => inner.Property = value; }
            }
            """, index: 1, title: string.Format(AnalyzersResources.Implement_through_0, "inner"));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41420")]
    public async Task PropertyIsDelegated_AllOptionsOff()
    {
        await TestAllOptionsOffAsync(
            """
            abstract class Base
            {
                public abstract int Property { get; set; }
            }

            class [|Derived|] : Base
            {
                Base inner;
            }
            """,
            """
            abstract class Base
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
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41420")]
    public async Task PropertyWithSingleAccessorIsDelegated()
    {
        await TestInRegularAndScriptAsync(
            """
            abstract class Base
            {
                public abstract int GetOnly { get; }
                public abstract int SetOnly { set; }
            }

            class [|Derived|] : Base
            {
                Base inner;
            }
            """,
            """
            abstract class Base
            {
                public abstract int GetOnly { get; }
                public abstract int SetOnly { set; }
            }

            class Derived : Base
            {
                Base inner;

                public override int GetOnly => inner.GetOnly;

                public override int SetOnly { set => inner.SetOnly = value; }
            }
            """, index: 1, title: string.Format(AnalyzersResources.Implement_through_0, "inner"));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41420")]
    public async Task PropertyWithSingleAccessorIsDelegated_AllOptionsOff()
    {
        await TestAllOptionsOffAsync(
            """
            abstract class Base
            {
                public abstract int GetOnly { get; }
                public abstract int SetOnly { set; }
            }

            class [|Derived|] : Base
            {
                Base inner;
            }
            """,
            """
            abstract class Base
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
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41420")]
    public async Task EventIsDelegated()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;

            abstract class Base
            {
                public abstract event Action Event;
            }

            class [|Derived|] : Base
            {
                Base inner;
            }
            """,
            """
            using System;

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
            }
            """, index: 1, title: string.Format(AnalyzersResources.Implement_through_0, "inner"));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41420")]
    public async Task OnlyOverridableMethodsAreOverridden()
    {
        await TestInRegularAndScriptAsync(
            """
            abstract class Base
            {
                public abstract void Method();

                public void NonVirtualMethod();
            }

            class [|Derived|] : Base
            {
                Base inner;
            }
            """,
            """
            abstract class Base
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
            }
            """, index: 1, title: string.Format(AnalyzersResources.Implement_through_0, "inner"));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41420")]
    public async Task ProtectedMethodsCannotBeDelegatedThroughBaseType()
    {
        await TestExactActionSetOfferedAsync(
            """
            abstract class Base
            {
                protected abstract void Method();
            }

            class [|Derived|] : Base
            {
                Base inner;
            }
            """, [AnalyzersResources.Implement_abstract_class]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41420")]
    public async Task ProtectedMethodsCanBeDelegatedThroughSameType()
    {
        await TestInRegularAndScriptAsync(
            """
            abstract class Base
            {
                protected abstract void Method();
            }

            class [|Derived|] : Base
            {
                Derived inner;
            }
            """,
            """
            abstract class Base
            {
                protected abstract void Method();
            }

            class Derived : Base
            {
                Derived inner;

                protected override void Method()
                {
                    inner.Method();
                }
            }
            """, index: 1, title: string.Format(AnalyzersResources.Implement_through_0, "inner"));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41420")]
    public async Task ProtectedInternalMethodsAreOverridden()
    {
        await TestInRegularAndScriptAsync(
            """
            abstract class Base
            {
                protected internal abstract void Method();
            }

            class [|Derived|] : Base
            {
                Base inner;
            }
            """,
            """
            abstract class Base
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
            }
            """, index: 1, title: string.Format(AnalyzersResources.Implement_through_0, "inner"));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41420")]
    public async Task InternalMethodsAreOverridden()
    {
        await TestInRegularAndScriptAsync(
            """
            abstract class Base
            {
                internal abstract void Method();
            }

            class [|Derived|] : Base
            {
                Base inner;
            }
            """,
            """
            abstract class Base
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
            }
            """, index: 1, title: string.Format(AnalyzersResources.Implement_through_0, "inner"));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41420")]
    public async Task PrivateProtectedMethodsCannotBeDelegatedThroughBaseType()
    {
        await TestExactActionSetOfferedAsync(
            """
            abstract class Base
            {
                private protected abstract void Method();
            }

            class [|Derived|] : Base
            {
                Base inner;
            }
            """, [AnalyzersResources.Implement_abstract_class]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41420")]
    public async Task PrivateProtectedMethodsCanBeDelegatedThroughSameType()
    {
        await TestInRegularAndScriptAsync(
            """
            abstract class Base
            {
                private protected abstract void Method();
            }

            class [|Derived|] : Base
            {
                Derived inner;
            }
            """,
            """
            abstract class Base
            {
                private protected abstract void Method();
            }

            class Derived : Base
            {
                Derived inner;

                private protected override void Method()
                {
                    inner.Method();
                }
            }
            """, index: 1, title: string.Format(AnalyzersResources.Implement_through_0, "inner"));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41420")]
    public async Task AccessorsWithDifferingVisibilityAreGeneratedCorrectly()
    {
        await TestInRegularAndScriptAsync(
            """
            abstract class Base
            {
                public abstract int InternalGet { internal get; set; }
                public abstract int InternalSet { get; internal set; }
            }

            class [|Derived|] : Base
            {
                Base inner;
            }
            """,
            """
            abstract class Base
            {
                public abstract int InternalGet { internal get; set; }
                public abstract int InternalSet { get; internal set; }
            }

            class Derived : Base
            {
                Base inner;

                public override int InternalGet { internal get => inner.InternalGet; set => inner.InternalGet = value; }
                public override int InternalSet { get => inner.InternalSet; internal set => inner.InternalSet = value; }
            }
            """, index: 1, title: string.Format(AnalyzersResources.Implement_through_0, "inner"));
    }

    [Fact]
    public async Task TestCrossProjectWithInaccessibleMemberInCase()
    {
        await TestInRegularAndScriptAsync(
            """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            public abstract class Base
            {
                public abstract void Method1();
                internal abstract void Method2();
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <ProjectReference>Assembly1</ProjectReference>
                    <Document>
            class [|Derived|] : Base
            {
                Base inner;
            }
                    </Document>
                </Project>
            </Workspace>
            """,
            """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            public abstract class Base
            {
                public abstract void Method1();
                internal abstract void Method2();
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <ProjectReference>Assembly1</ProjectReference>
                    <Document>
            class {|Conflict:Derived|} : Base
            {
                Base inner;

                public override void Method1()
                {
                    inner.Method1();
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """, index: 1);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69177")]
    public async Task TestImplementThroughPrimaryConstructorParam1()
    {
        await TestInRegularAndScriptAsync(
            """
            abstract class Base
            {
                public abstract int Method();
            }

            class [|Program|](Base base1) : Base
            {
            }
            """,
            """
            abstract class Base
            {
                public abstract int Method();
            }

            class Program(Base base1) : Base
            {
                public override int Method()
                {
                    return base1.Method();
                }
            }
            """, index: 1);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69177")]
    public async Task TestImplementThroughPrimaryConstructorParam2()
    {
        // Don't offer "implement through 'base1'" since this PC parameter is captured as a field.
        await TestExactActionSetOfferedAsync(
            """
            abstract class Base
            {
                public abstract int Method();
            }

            class [|Program|](Base base1) : Base
            {
                private Base _base = base1;
            }
            """, [AnalyzersResources.Implement_abstract_class, string.Format(AnalyzersResources.Implement_through_0, "_base")]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69177")]
    public async Task TestImplementThroughPrimaryConstructorParam2_B()
    {
        // Don't offer "implement through 'base1'" since this PC parameter is captured as a field.
        await TestExactActionSetOfferedAsync(
            """
            abstract class Base
            {
                public abstract int Method();
            }

            class [|Program|](Base base1) : Base
            {
                private Base _base = (base1);
            }
            """, [AnalyzersResources.Implement_abstract_class, string.Format(AnalyzersResources.Implement_through_0, "_base")]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69177")]
    public async Task TestImplementThroughPrimaryConstructorParam3()
    {
        // Don't offer "implement through 'base1'" since this PC parameter is captured as a field.
        await TestExactActionSetOfferedAsync(
            """
            abstract class Base
            {
                public abstract int Method();
            }

            class [|Program|](Base base1) : Base
            {
                private Base B { get; } = base1;
            }
            """, [AnalyzersResources.Implement_abstract_class, string.Format(AnalyzersResources.Implement_through_0, "B")]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69177")]
    public async Task TestImplementThroughPrimaryConstructorParam3_B()
    {
        // Don't offer "implement through 'base1'" since this PC parameter is captured as a field.
        await TestExactActionSetOfferedAsync(
            """
            abstract class Base
            {
                public abstract int Method();
            }

            class [|Program|](Base base1) : Base
            {
                private Base B { get; } = (base1);
            }
            """, [AnalyzersResources.Implement_abstract_class, string.Format(AnalyzersResources.Implement_through_0, "B")]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69177")]
    public async Task TestImplementThroughPrimaryConstructorParam4()
    {
        await TestInRegularAndScriptAsync(
            """
            abstract class Base
            {
                public abstract int Method();
            }

            class [|Program|](Base base1) : Base
            {
                private readonly int base1Hash = base1.GetHashCode();
            }
            """,
            """
            abstract class Base
            {
                public abstract int Method();
            }

            class Program(Base base1) : Base
            {
                private readonly int base1Hash = base1.GetHashCode();

                public override int Method()
                {
                    return base1.Method();
                }
            }
            """, index: 1);
    }
}
