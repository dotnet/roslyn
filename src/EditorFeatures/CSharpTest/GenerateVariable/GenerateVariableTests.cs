// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.GenerateVariable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.GenerateVariable
{
    public class GenerateVariableTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        private const int FieldIndex = 0;
        private const int ReadonlyFieldIndex = 1;
        private const int PropertyIndex = 2;
        private const int LocalIndex = 3;
        private const int Parameter = 4;
        private const int ParameterAndOverrides = 5;

        public GenerateVariableTests(ITestOutputHelper logger)
          : base(logger)
        {
        }

        internal override (DiagnosticAnalyzer?, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpGenerateVariableCodeFixProvider());

        private readonly CodeStyleOption2<bool> onWithInfo = new CodeStyleOption2<bool>(true, NotificationOption2.Suggestion);

        // specify all options explicitly to override defaults.
        private OptionsCollection ImplicitTypingEverywhere()
            => new OptionsCollection(GetLanguage())
            {
                { CSharpCodeStyleOptions.VarElsewhere, onWithInfo },
                { CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithInfo },
                { CSharpCodeStyleOptions.VarForBuiltInTypes, onWithInfo },
            };

        protected override ImmutableArray<CodeAction> MassageActions(ImmutableArray<CodeAction> actions)
            => FlattenActions(actions);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestSimpleLowercaseIdentifier1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|goo|];
    }
}",
@"class Class
{
    private object goo;

    void Method()
    {
        goo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestSimpleLowercaseIdentifierAllOptionsOffered()
        {
            await TestExactActionSetOfferedAsync(
@"class Class
{
    void Method()
    {
        [|goo|];
    }
}",
new[]
{
    string.Format(FeaturesResources.Generate_field_0, "goo"),
    string.Format(FeaturesResources.Generate_read_only_field_0, "goo"),
    string.Format(FeaturesResources.Generate_property_0, "goo"),
    string.Format(FeaturesResources.Generate_local_0, "goo"),
    string.Format(FeaturesResources.Generate_parameter_0, "goo"),
});
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestUnderscorePrefixAllOptionsOffered()
        {
            await TestExactActionSetOfferedAsync(
@"class Class
{
    void Method()
    {
        [|_goo|];
    }
}",
new[]
{
    string.Format(FeaturesResources.Generate_field_0, "_goo"),
    string.Format(FeaturesResources.Generate_read_only_field_0, "_goo"),
});
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestSimpleLowercaseIdentifier2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|goo|];
    }
}",
@"class Class
{
    private readonly object goo;

    void Method()
    {
        goo;
    }
}",
index: ReadonlyFieldIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestTestSimpleLowercaseIdentifier3()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|goo|];
    }
}",
@"class Class
{
    public object goo { get; private set; }

    void Method()
    {
        goo;
    }
}",
index: PropertyIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestSimpleUppercaseIdentifier1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|Goo|];
    }
}",
@"class Class
{
    public object Goo { get; private set; }

    void Method()
    {
        Goo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestSimpleUppercaseIdentifier2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|Goo|];
    }
}",
@"class Class
{
    private object Goo;

    void Method()
    {
        Goo;
    }
}",
index: ReadonlyFieldIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestSimpleUppercaseIdentifier3()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|Goo|];
    }
}",
@"class Class
{
    private readonly object Goo;

    void Method()
    {
        Goo;
    }
}",
index: PropertyIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestSimpleRead1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method(int i)
    {
        Method([|goo|]);
    }
}",
@"class Class
{
    private int goo;

    void Method(int i)
    {
        Method(goo);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestSimpleReadWithTopLevelNullability()
        {
            await TestInRegularAndScriptAsync(
@"#nullable enable

class Class
{
    void Method(string? s)
    {
        Method([|goo|]);
    }
}",
@"#nullable enable

class Class
{
    private string? goo;

    void Method(string? s)
    {
        Method(goo);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestSimpleReadWithNestedNullability()
        {
            await TestInRegularAndScriptAsync(
@"#nullable enable

using System.Collections.Generic;

class Class
{
    void Method(IEnumerable<string?> s)
    {
        Method([|goo|]);
    }
}",
@"#nullable enable

using System.Collections.Generic;

class Class
{
    private IEnumerable<string?> goo;

    void Method(IEnumerable<string?> s)
    {
        Method(goo);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestSimpleWriteCount()
        {
            await TestExactActionSetOfferedAsync(
@"class Class
{
    void Method(int i)
    {
        [|goo|] = 1;
    }
}",
new[] { string.Format(FeaturesResources.Generate_field_0, "goo"), string.Format(FeaturesResources.Generate_property_0, "goo"), string.Format(FeaturesResources.Generate_local_0, "goo"), string.Format(FeaturesResources.Generate_parameter_0, "goo") });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestSimpleWriteInOverrideCount()
        {
            await TestExactActionSetOfferedAsync(
@"
abstract class Base
{
    public abstract void Method(int i);
}

class Class : Base
{
    public override void Method(int i)
    {
        [|goo|] = 1;
    }
}",
new[] { string.Format(FeaturesResources.Generate_field_0, "goo"), string.Format(FeaturesResources.Generate_property_0, "goo"), string.Format(FeaturesResources.Generate_local_0, "goo"), string.Format(FeaturesResources.Generate_parameter_0, "goo"), string.Format(FeaturesResources.Generate_parameter_0_and_overrides_implementations, "goo") });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestSimpleWrite1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method(int i)
    {
        [|goo|] = 1;
    }
}",
@"class Class
{
    private int goo;

    void Method(int i)
    {
        goo = 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestSimpleWrite2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method(int i)
    {
        [|goo|] = 1;
    }
}",
@"class Class
{
    public int goo { get; private set; }

    void Method(int i)
    {
        goo = 1;
    }
}",
index: ReadonlyFieldIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFieldInRef()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method(ref int i)
    {
        Method(ref this.[|goo|]);
    }
}",
@"class Class
{
    private int goo;

    void Method(ref int i)
    {
        Method(ref this.[|goo|]);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGeneratePropertyInRef()
        {
            await TestInRegularAndScriptAsync(
@"
using System;
class Class
{
    void Method(ref int i)
    {
        Method(ref this.[|goo|]);
    }
}",
@"
using System;
class Class
{
    public ref int goo => throw new NotImplementedException();

    void Method(ref int i)
    {
        Method(ref this.goo);
    }
}", index: ReadonlyFieldIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGeneratePropertyInIn()
        {
            await TestInRegularAndScriptAsync(
@"
using System;
class Class
{
    void Method(in int i)
    {
        Method(in this.[|goo|]);
    }
}",
@"
using System;
class Class
{
    public ref readonly int goo => throw new NotImplementedException();

    void Method(in int i)
    {
        Method(in this.goo);
    }
}", index: PropertyIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInRef1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method(ref int i)
    {
        Method(ref [|goo|]);
    }
}",
@"class Class
{
    private int goo;

    void Method(ref int i)
    {
        Method(ref goo);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInOutCodeActionCount()
        {
            await TestExactActionSetOfferedAsync(
@"class Class
{
    void Method(out int i)
    {
        Method(out [|goo|]);
    }
}",
new[] { string.Format(FeaturesResources.Generate_field_0, "goo"), string.Format(FeaturesResources.Generate_local_0, "goo"), string.Format(FeaturesResources.Generate_parameter_0, "goo") });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInOut1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method(out int i)
    {
        Method(out [|goo|]);
    }
}",
@"class Class
{
    private int goo;

    void Method(out int i)
    {
        Method(out goo);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateInStaticMember1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    static void Method()
    {
        [|goo|];
    }
}",
@"class Class
{
    private static object goo;

    static void Method()
    {
        goo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateInStaticMember2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    static void Method()
    {
        [|goo|];
    }
}",
@"class Class
{
    private static readonly object goo;

    static void Method()
    {
        goo;
    }
}",
index: ReadonlyFieldIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateInStaticMember3()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    static void Method()
    {
        [|goo|];
    }
}",
@"class Class
{
    public static object goo { get; private set; }

    static void Method()
    {
        goo;
    }
}",
index: PropertyIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateOffInstance1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        this.[|goo|];
    }
}",
@"class Class
{
    private object goo;

    void Method()
    {
        this.goo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateOffInstance2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        this.[|goo|];
    }
}",
@"class Class
{
    private readonly object goo;

    void Method()
    {
        this.goo;
    }
}",
index: ReadonlyFieldIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateOffInstance3()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        this.[|goo|];
    }
}",
@"class Class
{
    public object goo { get; private set; }

    void Method()
    {
        this.goo;
    }
}",
index: PropertyIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateOffWrittenInstance1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        this.[|goo|] = 1;
    }
}",
@"class Class
{
    private int goo;

    void Method()
    {
        this.goo = 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateOffWrittenInstance2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        this.[|goo|] = 1;
    }
}",
@"class Class
{
    public int goo { get; private set; }

    void Method()
    {
        this.goo = 1;
    }
}",
index: ReadonlyFieldIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateOffStatic1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        Class.[|goo|];
    }
}",
@"class Class
{
    private static object goo;

    void Method()
    {
        Class.goo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateOffStatic2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        Class.[|goo|];
    }
}",
@"class Class
{
    private static readonly object goo;

    void Method()
    {
        Class.goo;
    }
}",
index: ReadonlyFieldIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateOffStatic3()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        Class.[|goo|];
    }
}",
@"class Class
{
    public static object goo { get; private set; }

    void Method()
    {
        Class.goo;
    }
}",
index: PropertyIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateOffWrittenStatic1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        Class.[|goo|] = 1;
    }
}",
@"class Class
{
    private static int goo;

    void Method()
    {
        Class.goo = 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateOffWrittenStatic2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        Class.[|goo|] = 1;
    }
}",
@"class Class
{
    public static int goo { get; private set; }

    void Method()
    {
        Class.goo = 1;
    }
}",
index: ReadonlyFieldIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateInstanceIntoSibling1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        new D().[|goo|];
    }
}

class D
{
}",
@"class Class
{
    void Method()
    {
        new D().goo;
    }
}

class D
{
    internal object goo;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateInstanceIntoOuter1()
        {
            await TestInRegularAndScriptAsync(
@"class Outer
{
    class Class
    {
        void Method()
        {
            new Outer().[|goo|];
        }
    }
}",
@"class Outer
{
    private object goo;

    class Class
    {
        void Method()
        {
            new Outer().goo;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateInstanceIntoDerived1()
        {
            await TestInRegularAndScriptAsync(
@"class Class : Base
{
    void Method(Base b)
    {
        b.[|goo|];
    }
}

class Base
{
}",
@"class Class : Base
{
    void Method(Base b)
    {
        b.goo;
    }
}

class Base
{
    internal object goo;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateStaticIntoDerived1()
        {
            await TestInRegularAndScriptAsync(
@"class Class : Base
{
    void Method(Base b)
    {
        Base.[|goo|];
    }
}

class Base
{
}",
@"class Class : Base
{
    void Method(Base b)
    {
        Base.goo;
    }
}

class Base
{
    protected static object goo;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateIntoInterfaceFixCount()
        {
            await TestActionCountAsync(
@"class Class
{
    void Method(I i)
    {
        i.[|goo|];
    }
}

interface I
{
}",
count: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateIntoInterface1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method(I i)
    {
        i.[|Goo|];
    }
}

interface I
{
}",
@"class Class
{
    void Method(I i)
    {
        i.Goo;
    }
}

interface I
{
    object Goo { get; set; }
}", index: ReadonlyFieldIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateIntoInterface2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method(I i)
    {
        i.[|Goo|];
    }
}

interface I
{
}",
@"class Class
{
    void Method(I i)
    {
        i.Goo;
    }
}

interface I
{
    object Goo { get; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateStaticIntoInterfaceMissing()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    void Method(I i)
    {
        I.[|Goo|];
    }
}

interface I
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateWriteIntoInterfaceFixCount()
        {
            await TestActionCountAsync(
@"class Class
{
    void Method(I i)
    {
        i.[|Goo|] = 1;
    }
}

interface I
{
}",
count: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateWriteIntoInterface1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method(I i)
    {
        i.[|Goo|] = 1;
    }
}

interface I
{
}",
@"class Class
{
    void Method(I i)
    {
        i.Goo = 1;
    }
}

interface I
{
    int Goo { get; set; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateInGenericType()
        {
            await TestInRegularAndScriptAsync(
@"class Class<T>
{
    void Method(T t)
    {
        [|goo|] = t;
    }
}",
@"class Class<T>
{
    private T goo;

    void Method(T t)
    {
        goo = t;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateInGenericMethod1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method<T>(T t)
    {
        [|goo|] = t;
    }
}",
@"class Class
{
    private object goo;

    void Method<T>(T t)
    {
        goo = t;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateInGenericMethod2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method<T>(IList<T> t)
    {
        [|goo|] = t;
    }
}",
@"class Class
{
    private IList<object> goo;

    void Method<T>(IList<T> t)
    {
        goo = t;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFieldBeforeFirstField()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    int i;

    void Method()
    {
        [|goo|];
    }
}",
@"class Class
{
    int i;
    private object goo;

    void Method()
    {
        goo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFieldAfterLastField()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|goo|];
    }

    int i;
}",
@"class Class
{
    void Method()
    {
        goo;
    }

    int i;
    private object goo;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGeneratePropertyAfterLastField1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    int Bar;

    void Method()
    {
        [|Goo|];
    }
}",
@"class Class
{
    int Bar;

    public object Goo { get; private set; }

    void Method()
    {
        Goo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGeneratePropertyAfterLastField2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|Goo|];
    }

    int Bar;
}",
@"class Class
{
    void Method()
    {
        Goo;
    }

    int Bar;

    public object Goo { get; private set; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGeneratePropertyBeforeFirstProperty()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    int Quux { get; }

    void Method()
    {
        [|Goo|];
    }
}",
@"class Class
{
    public object Goo { get; private set; }
    int Quux { get; }

    void Method()
    {
        Goo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGeneratePropertyBeforeFirstPropertyEvenWithField1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    int Bar;

    int Quux { get; }

    void Method()
    {
        [|Goo|];
    }
}",
@"class Class
{
    int Bar;

    public object Goo { get; private set; }
    int Quux { get; }

    void Method()
    {
        Goo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGeneratePropertyAfterLastPropertyEvenWithField2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    int Quux { get; }

    int Bar;

    void Method()
    {
        [|Goo|];
    }
}",
@"class Class
{
    int Quux { get; }
    public object Goo { get; private set; }

    int Bar;

    void Method()
    {
        Goo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMissingInInvocation()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|Goo|]();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMissingInObjectCreation()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        new [|Goo|]();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMissingInTypeDeclaration()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|A|] a;
    }
}");

            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|A.B|] a;
    }
}");

            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|A|].B a;
    }
}");

            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        A.[|B|] a;
    }
}");

            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|A.B.C|] a;
    }
}");

            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|A.B|].C a;
    }
}");

            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        A.B.[|C|] a;
    }
}");

            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|A|].B.C a;
    }
}");

            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        A.[|B|].C a;
    }
}");
        }

        [WorkItem(539336, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539336")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMissingInAttribute()
        {
            await TestMissingInRegularAndScriptAsync(
@"[[|A|]]
class Class
{
}");

            await TestMissingInRegularAndScriptAsync(
@"[[|A.B|]]
class Class
{
}");

            await TestMissingInRegularAndScriptAsync(
@"[[|A|].B]
class Class
{
}");

            await TestMissingInRegularAndScriptAsync(
@"[A.[|B|]]
class Class
{
}");

            await TestMissingInRegularAndScriptAsync(
@"[[|A.B.C|]]
class Class
{
}");

            await TestMissingInRegularAndScriptAsync(
@"[[|A.B|].C]
class Class
{
}");

            await TestMissingInRegularAndScriptAsync(
@"[A.B.[|C|]]
class Class
{
}");

            await TestMissingInRegularAndScriptAsync(
@"[[|A|].B.C]
class Class
{
}");

            await TestMissingInRegularAndScriptAsync(
@"[A.B.[|C|]]
class Class
{
}");
        }

        [WorkItem(539340, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539340")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestSpansField()
        {
            await TestSpansAsync(
@"class C
{
    void M()
    {
        this.[|Goo|] }");

            await TestSpansAsync(
@"class C
{
    void M()
    {
        this.[|Goo|];
    }");

            await TestSpansAsync(
@"class C
{
    void M()
    {
        this.[|Goo|] = 1 }");

            await TestSpansAsync(
@"class C
{
    void M()
    {
        this.[|Goo|] = 1 + 2 }");

            await TestSpansAsync(
@"class C
{
    void M()
    {
        this.[|Goo|] = 1 + 2;
    }");

            await TestSpansAsync(
@"class C
{
    void M()
    {
        this.[|Goo|] += Bar() }");

            await TestSpansAsync(
@"class C
{
    void M()
    {
        this.[|Goo|] += Bar();
    }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFieldInSimpleLambda()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class Program
{
    static void Main(string[] args)
    {
        Func<string, int> f = x => [|goo|];
    }
}",
@"using System;

class Program
{
    private static int goo;

    static void Main(string[] args)
    {
        Func<string, int> f = x => goo;
    }
}", FieldIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFieldInParenthesizedLambda()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class Program
{
    static void Main(string[] args)
    {
        Func<int> f = () => [|goo|];
    }
}",
@"using System;

class Program
{
    private static int goo;

    static void Main(string[] args)
    {
        Func<int> f = () => goo;
    }
}", FieldIndex);
        }

        [WorkItem(30232, "https://github.com/dotnet/roslyn/issues/30232")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFieldInAsyncTaskOfTSimpleLambda()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        Func<string, Task<int>> f = async x => [|goo|];
    }
}",
@"using System;
using System.Threading.Tasks;

class Program
{
    private static int goo;

    static void Main(string[] args)
    {
        Func<string, Task<int>> f = async x => goo;
    }
}", FieldIndex);
        }

        [WorkItem(30232, "https://github.com/dotnet/roslyn/issues/30232")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFieldInAsyncTaskOfTParenthesizedLambda()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        Func<Task<int>> f = async () => [|goo|];
    }
}",
@"using System;
using System.Threading.Tasks;

class Program
{
    private static int goo;

    static void Main(string[] args)
    {
        Func<Task<int>> f = async () => goo;
    }
}", FieldIndex);
        }

        [WorkItem(539427, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539427")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFromLambda()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method(int i)
    {
        [|goo|] = () => {
            return 2 };
    }
}",
@"using System;

class Class
{
    private Func<int> goo;

    void Method(int i)
    {
        goo = () => {
            return 2 };
    }
}");
        }

        // TODO: Move to TypeInferrer.InferTypes, or something
        [WorkItem(539466, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539466")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateInMethodOverload1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method(int i)
    {
        System.Console.WriteLine([|goo|]);
    }
}",
@"class Class
{
    private bool goo;

    void Method(int i)
    {
        System.Console.WriteLine(goo);
    }
}");
        }

        // TODO: Move to TypeInferrer.InferTypes, or something
        [WorkItem(539466, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539466")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateInMethodOverload2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method(int i)
    {
        System.Console.WriteLine(this.[|goo|]);
    }
}",
@"class Class
{
    private bool goo;

    void Method(int i)
    {
        System.Console.WriteLine(this.goo);
    }
}");
        }

        [WorkItem(539468, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539468")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestExplicitProperty1()
        {
            await TestInRegularAndScriptAsync(
@"class Class : ITest
{
    bool ITest.[|SomeProp|] { get; set; }
}

interface ITest
{
}",
@"class Class : ITest
{
    bool ITest.SomeProp { get; set; }
}

interface ITest
{
    bool SomeProp { get; set; }
}");
        }

        [WorkItem(539468, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539468")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestExplicitProperty2()
        {
            await TestInRegularAndScriptAsync(
@"class Class : ITest
{
    bool ITest.[|SomeProp|] { }
}

interface ITest
{
}",
@"class Class : ITest
{
    bool ITest.SomeProp { }
}

interface ITest
{
    bool SomeProp { get; set; }
}", index: ReadonlyFieldIndex);
        }

        [WorkItem(539468, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539468")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestExplicitProperty3()
        {
            await TestInRegularAndScriptAsync(
@"class Class : ITest
{
    bool ITest.[|SomeProp|] { }
}

interface ITest
{
}",
@"class Class : ITest
{
    bool ITest.SomeProp { }
}

interface ITest
{
    bool SomeProp { get; }
}");
        }

        [WorkItem(539468, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539468")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestExplicitProperty4()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    bool ITest.[|SomeProp|] { }
}

interface ITest
{
}");
        }

        [WorkItem(539468, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539468")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestExplicitProperty5()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Class : ITest
{
    bool ITest.[|SomeProp|] { }
}

interface ITest
{
    bool SomeProp { get; }
}");
        }

        [WorkItem(539489, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539489")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestEscapedName()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|@goo|];
    }
}",
@"class Class
{
    private object goo;

    void Method()
    {
        @goo;
    }
}");
        }

        [WorkItem(539489, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539489")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestEscapedKeyword()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|@int|];
    }
}",
@"class Class
{
    private object @int;

    void Method()
    {
        @int;
    }
}");
        }

        [WorkItem(539529, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539529")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestRefLambda()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|test|] = (ref int x) => x = 10;
    }
}",
@"class Class
{
    private object test;

    void Method()
    {
        test = (ref int x) => x = 10;
    }
}");
        }

        [WorkItem(539595, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539595")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestNotOnError()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    void F<U, V>(U u1, V v1)
    {
        Goo<string, int>([|u1|], u2);
    }
}");
        }

        [WorkItem(539571, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539571")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestNameSimplification()
        {
            await TestInRegularAndScriptAsync(
@"namespace TestNs
{
    class Program
    {
        class Test
        {
            void Meth()
            {
                Program.[|blah|] = new Test();
            }
        }
    }
}",
@"namespace TestNs
{
    class Program
    {
        private static Test blah;

        class Test
        {
            void Meth()
            {
                Program.blah = new Test();
            }
        }
    }
}");
        }

        [WorkItem(539717, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539717")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestPostIncrement()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    static void Main(string[] args)
    {
        [|i|]++;
    }
}",
@"class Program
{
    private static int i;

    static void Main(string[] args)
    {
        i++;
    }
}");
        }

        [WorkItem(539717, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539717")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestPreDecrement()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    static void Main(string[] args)
    {
        --[|i|];
    }
}",
@"class Program
{
    private static int i;

    static void Main(string[] args)
    {
        --i;
    }
}");
        }

        [WorkItem(539738, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539738")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateIntoScript()
        {
            await TestAsync(
@"using C;

static class C
{
}

C.[|i|] ++ ;",
@"using C;

static class C
{
    internal static int i;
}

C.i ++ ;",
parseOptions: Options.Script);
        }

        [WorkItem(539558, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539558")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task BugFix5565()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        [|Goo|]#();
    }
}",
@"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    public static object Goo { get; private set; }

    static void Main(string[] args)
    {
        Goo#();
    }
}");
        }

        [WorkItem(539536, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539536")]
        [Fact(Skip = "Tuples"), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task BugFix5538()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        new([|goo|])();
    }
}",
@"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    public static object goo { get; private set; }

    static void Main(string[] args)
    {
        new(goo)();
    }
}",
index: PropertyIndex);
        }

        [WorkItem(539665, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539665")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task BugFix5697()
        {
            await TestInRegularAndScriptAsync(
@"class C { }
class D
{
    void M()
    {
        C.[|P|] = 10;
    }
}
",
@"class C
{
    public static int P { get; internal set; }
}
class D
{
    void M()
    {
        C.P = 10;
    }
}
");
        }

        [WorkItem(539793, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539793")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestIncrement()
        {
            await TestExactActionSetOfferedAsync(
@"class Program
{
    static void Main()
    {
        [|p|]++;
    }
}",
new[] { string.Format(FeaturesResources.Generate_field_0, "p"), string.Format(FeaturesResources.Generate_property_0, "p"), string.Format(FeaturesResources.Generate_local_0, "p"), string.Format(FeaturesResources.Generate_parameter_0, "p") });

            await TestInRegularAndScriptAsync(
@"class Program
{
    static void Main()
    {
        [|p|]++;
    }
}",
@"class Program
{
    private static int p;

    static void Main()
    {
        p++;
    }
}");
        }

        [WorkItem(539834, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539834")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestNotInGoto()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Program
{
    static void Main()
    {
        goto [|goo|];
    }
}");
        }

        [WorkItem(539826, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539826")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestOnLeftOfDot()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    static void Main()
    {
        [|goo|].ToString();
    }
}",
@"class Program
{
    private static object goo;

    static void Main()
    {
        goo.ToString();
    }
}");
        }

        [WorkItem(539840, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539840")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestNotBeforeAlias()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        [|global|]::System.String s;
    }
}");
        }

        [WorkItem(539871, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539871")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMissingOnGenericName()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C<T>
{
    public delegate void Goo<R>(R r);

    static void M()
    {
        Goo<T> r = [|Goo<T>|];
    }
}");
        }

        [WorkItem(539934, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539934")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestOnDelegateAddition()
        {
            await TestAsync(
@"class C
{
    delegate void D();

    void M()
    {
        D d = [|M1|] + M2;
    }
}",
@"class C
{
    private D M1 { get; set; }

    delegate void D();

    void M()
    {
        D d = M1 + M2;
    }
}",
parseOptions: null);
        }

        [WorkItem(539986, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539986")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestReferenceTypeParameter1()
        {
            await TestInRegularAndScriptAsync(
@"class C<T>
{
    public void Test()
    {
        C<T> c = A.[|M|];
    }
}

class A
{
}",
@"class C<T>
{
    public void Test()
    {
        C<T> c = A.M;
    }
}

class A
{
    public static C<object> M { get; internal set; }
}");
        }

        [WorkItem(539986, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539986")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestReferenceTypeParameter2()
        {
            await TestInRegularAndScriptAsync(
@"class C<T>
{
    public void Test()
    {
        C<T> c = A.[|M|];
    }

    class A
    {
    }
}",
@"class C<T>
{
    public void Test()
    {
        C<T> c = A.M;
    }

    class A
    {
        public static C<T> M { get; internal set; }
    }
}");
        }

        [WorkItem(540159, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540159")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestEmptyIdentifierName()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    static void M()
    {
        int i = [|@|] }
}");
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    static void M()
    {
        int i = [|@|]}
}");
        }

        [WorkItem(541194, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541194")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestForeachVar()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        foreach (var v in [|list|])
        {
        }
    }
}",
@"using System.Collections.Generic;

class C
{
    private IEnumerable<object> list;

    void M()
    {
        foreach (var v in list)
        {
        }
    }
}");
        }

        [WorkItem(541265, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541265")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestExtensionMethodUsedAsInstance()
        {
            await TestAsync(
@"using System;

class C
{
    public static void Main()
    {
        string s = ""Hello"";
        [|f|] = s.ExtensionMethod;
    }
}

public static class MyExtension
{
    public static int ExtensionMethod(this String s)
    {
        return s.Length;
    }
}",
@"using System;

class C
{
    private static Func<int> f;

    public static void Main()
    {
        string s = ""Hello"";
        f = s.ExtensionMethod;
    }
}

public static class MyExtension
{
    public static int ExtensionMethod(this String s)
    {
        return s.Length;
    }
}",
parseOptions: null);
        }

        [WorkItem(541549, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541549")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestDelegateInvoke()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class Program
{
    static void Main(string[] args)
    {
        Func<int, int> f = x => x + 1;
        f([|x|]);
    }
}",
@"using System;

class Program
{
    private static int x;

    static void Main(string[] args)
    {
        Func<int, int> f = x => x + 1;
        f(x);
    }
}");
        }

        [WorkItem(541597, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541597")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestComplexAssign1()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    static void Main(string[] args)
    {
        [|a|] = a + 10;
    }
}",
@"class Program
{
    private static int a;

    static void Main(string[] args)
    {
        a = a + 10;
    }
}");
        }

        [WorkItem(541597, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541597")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestComplexAssign2()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    static void Main(string[] args)
    {
        a = [|a|] + 10;
    }
}",
@"class Program
{
    private static int a;

    static void Main(string[] args)
    {
        a = a + 10;
    }
}");
        }

        [WorkItem(541659, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541659")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestTypeNamedVar()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class Program
{
    public static void Main()
    {
        var v = [|p|];
    }
}

class var
{
}",
@"using System;

class Program
{
    private static var p;

    public static void Main()
    {
        var v = p;
    }
}

class var
{
}");
        }

        [WorkItem(541675, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541675")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestStaticExtensionMethodArgument()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class Program
{
    static void Main(string[] args)
    {
        MyExtension.ExMethod([|ss|]);
    }
}

static class MyExtension
{
    public static int ExMethod(this string s)
    {
        return s.Length;
    }
}",
@"using System;

class Program
{
    private static string ss;

    static void Main(string[] args)
    {
        MyExtension.ExMethod(ss);
    }
}

static class MyExtension
{
    public static int ExMethod(this string s)
    {
        return s.Length;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task SpeakableTopLevelStatementType()
        {
            await TestMissingAsync(@"
[|P|] = 10;

partial class Program
{
}");
        }

        [WorkItem(539675, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539675")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task AddBlankLineBeforeCommentBetweenMembers1()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    //method
    static void Main(string[] args)
    {
        [|P|] = 10;
    }
}",
@"class Program
{
    public static int P { get; private set; }

    //method
    static void Main(string[] args)
    {
        P = 10;
    }
}");
        }

        [WorkItem(539675, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539675")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task AddBlankLineBeforeCommentBetweenMembers2()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    //method
    static void Main(string[] args)
    {
        [|P|] = 10;
    }
}",
@"class Program
{
    private static int P;

    //method
    static void Main(string[] args)
    {
        P = 10;
    }
}",
index: ReadonlyFieldIndex);
        }

        [WorkItem(543813, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543813")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task AddBlankLineBetweenMembers1()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    static void Main(string[] args)
    {
        [|P|] = 10;
    }
}",
@"class Program
{
    private static int P;

    static void Main(string[] args)
    {
        P = 10;
    }
}",
index: ReadonlyFieldIndex);
        }

        [WorkItem(543813, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543813")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task AddBlankLineBetweenMembers2()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    static void Main(string[] args)
    {
        [|P|] = 10;
    }
}",
@"class Program
{
    public static int P { get; private set; }

    static void Main(string[] args)
    {
        P = 10;
    }
}",
index: 0);
        }

        [WorkItem(543813, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543813")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task DontAddBlankLineBetweenFields()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    private static int P;

    static void Main(string[] args)
    {
        P = 10;
        [|A|] = 9;
    }
}",
@"class Program
{
    private static int P;
    private static int A;

    static void Main(string[] args)
    {
        P = 10;
        A = 9;
    }
}",
index: ReadonlyFieldIndex);
        }

        [WorkItem(543813, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543813")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task DontAddBlankLineBetweenAutoProperties()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    public static int P { get; private set; }

    static void Main(string[] args)
    {
        P = 10;
        [|A|] = 9;
    }
}",
@"class Program
{
    public static int P { get; private set; }
    public static int A { get; private set; }

    static void Main(string[] args)
    {
        P = 10;
        A = 9;
    }
}",
index: 0);
        }

        [WorkItem(539665, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539665")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestIntoEmptyClass()
        {
            await TestInRegularAndScriptAsync(
@"class C { }
class D
{
    void M()
    {
        C.[|P|] = 10;
    }
}",
@"class C
{
    public static int P { get; internal set; }
}
class D
{
    void M()
    {
        C.P = 10;
    }
}");
        }

        [WorkItem(540595, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540595")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGeneratePropertyInScript()
        {
            await TestAsync(
@"[|Goo|]",
@"object Goo { get; private set; }

Goo",
parseOptions: Options.Script);
        }

        [WorkItem(542535, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542535")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestConstantInParameterValue()
        {
            const string Initial =
@"class C
{   
    const int y = 1 ; 
    public void Goo ( bool x = [|undeclared|] ) { }
} ";

            await TestActionCountAsync(
Initial,
count: 1);

            await TestInRegularAndScriptAsync(
Initial,
@"class C
{   
    const int y = 1 ;
    private const bool undeclared;

    public void Goo ( bool x = undeclared ) { }
} ");
        }

        [WorkItem(542900, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542900")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFromAttributeNamedArgument1()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class ProgramAttribute : Attribute
{
    [Program([|Name|] = 0)]
    static void Main(string[] args)
    {
    }
}",
@"using System;

class ProgramAttribute : Attribute
{
    public int Name { get; set; }

    [Program(Name = 0)]
    static void Main(string[] args)
    {
    }
}");
        }

        [WorkItem(542900, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542900")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFromAttributeNamedArgument2()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class ProgramAttribute : Attribute
{
    [Program([|Name|] = 0)]
    static void Main(string[] args)
    {
    }
}",
@"using System;

class ProgramAttribute : Attribute
{
    public int Name;

    [Program(Name = 0)]
    static void Main(string[] args)
    {
    }
}",
index: ReadonlyFieldIndex);
        }

        [WorkItem(541698, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMinimalAccessibility1_InternalPrivate()
        {
            await TestAsync(
@"class Program
{
    public static void Main()
    {
        C c = [|P|];
    }

    private class C
    {
    }
}",
@"class Program
{
    private static C P { get; set; }

    public static void Main()
    {
        C c = P;
    }

    private class C
    {
    }
}",
parseOptions: null);
        }

        [WorkItem(541698, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMinimalAccessibility2_InternalProtected()
        {
            await TestAsync(
@"class Program
{
    public static void Main()
    {
        C c = [|P|];
    }

    protected class C
    {
    }
}",
@"class Program
{
    protected static C P { get; private set; }

    public static void Main()
    {
        C c = P;
    }

    protected class C
    {
    }
}",
parseOptions: null);
        }

        [WorkItem(541698, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMinimalAccessibility3_InternalInternal()
        {
            await TestAsync(
@"class Program
{
    public static void Main()
    {
        C c = [|P|];
    }

    internal class C
    {
    }
}",
@"class Program
{
    public static C P { get; private set; }

    public static void Main()
    {
        C c = P;
    }

    internal class C
    {
    }
}",
parseOptions: null);
        }

        [WorkItem(541698, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMinimalAccessibility4_InternalProtectedInternal()
        {
            await TestAsync(
@"class Program
{
    public static void Main()
    {
        C c = [|P|];
    }

    protected internal class C
    {
    }
}",
@"class Program
{
    public static C P { get; private set; }

    public static void Main()
    {
        C c = P;
    }

    protected internal class C
    {
    }
}",
parseOptions: null);
        }

        [WorkItem(541698, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMinimalAccessibility5_InternalPublic()
        {
            await TestAsync(
@"class Program
{
    public static void Main()
    {
        C c = [|P|];
    }

    public class C
    {
    }
}",
@"class Program
{
    public static C P { get; private set; }

    public static void Main()
    {
        C c = P;
    }

    public class C
    {
    }
}",
parseOptions: null);
        }

        [WorkItem(541698, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMinimalAccessibility6_PublicInternal()
        {
            await TestAsync(
@"public class Program
{
    public static void Main()
    {
        C c = [|P|];
    }

    internal class C
    {
    }
}",
@"public class Program
{
    internal static C P { get; private set; }

    public static void Main()
    {
        C c = P;
    }

    internal class C
    {
    }
}",
parseOptions: null);
        }

        [WorkItem(541698, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMinimalAccessibility7_PublicProtectedInternal()
        {
            await TestAsync(
@"public class Program
{
    public static void Main()
    {
        C c = [|P|];
    }

    protected internal class C
    {
    }
}",
@"public class Program
{
    protected internal static C P { get; private set; }

    public static void Main()
    {
        C c = P;
    }

    protected internal class C
    {
    }
}",
parseOptions: null);
        }

        [WorkItem(541698, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMinimalAccessibility8_PublicProtected()
        {
            await TestAsync(
@"public class Program
{
    public static void Main()
    {
        C c = [|P|];
    }

    protected class C
    {
    }
}",
@"public class Program
{
    protected static C P { get; private set; }

    public static void Main()
    {
        C c = P;
    }

    protected class C
    {
    }
}",
parseOptions: null);
        }

        [WorkItem(541698, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMinimalAccessibility9_PublicPrivate()
        {
            await TestAsync(
@"public class Program
{
    public static void Main()
    {
        C c = [|P|];
    }

    private class C
    {
    }
}",
@"public class Program
{
    private static C P { get; set; }

    public static void Main()
    {
        C c = P;
    }

    private class C
    {
    }
}",
parseOptions: null);
        }

        [WorkItem(541698, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMinimalAccessibility10_PrivatePrivate()
        {
            await TestAsync(
@"class outer
{
    private class Program
    {
        public static void Main()
        {
            C c = [|P|];
        }

        private class C
        {
        }
    }
}",
@"class outer
{
    private class Program
    {
        public static C P { get; private set; }

        public static void Main()
        {
            C c = P;
        }

        private class C
        {
        }
    }
}",
parseOptions: null);
        }

        [WorkItem(541698, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMinimalAccessibility11_PrivateProtected()
        {
            await TestAsync(
@"class outer
{
    private class Program
    {
        public static void Main()
        {
            C c = [|P|];
        }

        protected class C
        {
        }
    }
}",
@"class outer
{
    private class Program
    {
        public static C P { get; private set; }

        public static void Main()
        {
            C c = P;
        }

        protected class C
        {
        }
    }
}",
parseOptions: null);
        }

        [WorkItem(541698, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMinimalAccessibility12_PrivateProtectedInternal()
        {
            await TestAsync(
@"class outer
{
    private class Program
    {
        public static void Main()
        {
            C c = [|P|];
        }

        protected internal class C
        {
        }
    }
}",
@"class outer
{
    private class Program
    {
        public static C P { get; private set; }

        public static void Main()
        {
            C c = P;
        }

        protected internal class C
        {
        }
    }
}",
parseOptions: null);
        }

        [WorkItem(541698, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMinimalAccessibility13_PrivateInternal()
        {
            await TestAsync(
@"class outer
{
    private class Program
    {
        public static void Main()
        {
            C c = [|P|];
        }

        internal class C
        {
        }
    }
}",
@"class outer
{
    private class Program
    {
        public static C P { get; private set; }

        public static void Main()
        {
            C c = P;
        }

        internal class C
        {
        }
    }
}",
parseOptions: null);
        }

        [WorkItem(541698, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMinimalAccessibility14_ProtectedPrivate()
        {
            await TestAsync(
@"class outer
{
    protected class Program
    {
        public static void Main()
        {
            C c = [|P|];
        }

        private class C
        {
        }
    }
}",
@"class outer
{
    protected class Program
    {
        private static C P { get; set; }

        public static void Main()
        {
            C c = P;
        }

        private class C
        {
        }
    }
}",
parseOptions: null);
        }

        [WorkItem(541698, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMinimalAccessibility15_ProtectedInternal()
        {
            await TestAsync(
@"class outer
{
    protected class Program
    {
        public static void Main()
        {
            C c = [|P|];
        }

        internal class C
        {
        }
    }
}",
@"class outer
{
    protected class Program
    {
        public static C P { get; private set; }

        public static void Main()
        {
            C c = P;
        }

        internal class C
        {
        }
    }
}",
parseOptions: null);
        }

        [WorkItem(541698, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMinimalAccessibility16_ProtectedInternalProtected()
        {
            await TestAsync(
@"class outer
{
    protected internal class Program
    {
        public static void Main()
        {
            C c = [|P|];
        }

        protected class C
        {
        }
    }
}",
@"class outer
{
    protected internal class Program
    {
        protected static C P { get; private set; }

        public static void Main()
        {
            C c = P;
        }

        protected class C
        {
        }
    }
}",
parseOptions: null);
        }

        [WorkItem(541698, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMinimalAccessibility17_ProtectedInternalInternal()
        {
            await TestAsync(
@"class outer
{
    protected internal class Program
    {
        public static void Main()
        {
            C c = [|P|];
        }

        internal class C
        {
        }
    }
}",
@"class outer
{
    protected internal class Program
    {
        public static C P { get; private set; }

        public static void Main()
        {
            C c = P;
        }

        internal class C
        {
        }
    }
}",
parseOptions: null);
        }

        [WorkItem(543153, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543153")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestAnonymousObjectInitializer1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var a = new { x = 5 };
        a = new { x = [|HERE|] };
    }
}",
@"class C
{
    private int HERE;

    void M()
    {
        var a = new { x = 5 };
        a = new { x = HERE };
    }
}",
index: ReadonlyFieldIndex);
        }

        [WorkItem(543124, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543124")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestNoGenerationIntoAnonymousType()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Program
{
    static void Main(string[] args)
    {
        var v = new { };
        bool b = v.[|Bar|];
    }
}");
        }

        [WorkItem(543543, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543543")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestNotOfferedForBoundParametersOfOperators()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Program
{
    public Program(string s)
    {
    }

    static void Main(string[] args)
    {
        Program p = """";
    }

    public static implicit operator Program(string str)
    {
        return new Program([|str|]);
    }
}");
        }

        [WorkItem(544175, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544175")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestNotOnNamedParameterName1()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class class1
{
    public void Test()
    {
        Goo([|x|]: x);
    }

    public string Goo(int x)
    {
    }
}");
        }

        [WorkItem(544271, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544271")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestNotOnNamedParameterName2()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Goo
{
    public Goo(int a = 42)
    {
    }
}

class DogBed : Goo
{
    public DogBed(int b) : base([|a|]: b)
    {
    }
}");
        }

        [WorkItem(544164, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544164")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestPropertyOnObjectInitializer()
        {
            await TestInRegularAndScriptAsync(
@"class Goo
{
}

class Bar
{
    void goo()
    {
        var c = new Goo { [|Gibberish|] = 24 };
    }
}",
@"class Goo
{
    public int Gibberish { get; internal set; }
}

class Bar
{
    void goo()
    {
        var c = new Goo { Gibberish = 24 };
    }
}");
        }

        [WorkItem(49294, "https://github.com/dotnet/roslyn/issues/49294")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestPropertyInWithInitializer()
        {
            await TestInRegularAndScriptAsync(
@"record Goo
{
}

class Bar
{
    void goo(Goo g)
    {
        var c = g with { [|Gibberish|] = 24 };
    }
}",
@"record Goo
{
    public int Gibberish { get; internal set; }
}

class Bar
{
    void goo(Goo g)
    {
        var c = g with { Gibberish = 24 };
    }
}");
        }

        [WorkItem(13166, "https://github.com/dotnet/roslyn/issues/13166")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestPropertyOnNestedObjectInitializer()
        {
            await TestInRegularAndScriptAsync(
@"public class Inner
{
}

public class Outer
{
    public Inner Inner { get; set; } = new Inner();

    public static Outer X() => new Outer { Inner = { [|InnerValue|] = 5 } };
}",
@"public class Inner
{
    public int InnerValue { get; internal set; }
}

public class Outer
{
    public Inner Inner { get; set; } = new Inner();

    public static Outer X() => new Outer { Inner = { InnerValue = 5 } };
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestPropertyOnObjectInitializer1()
        {
            await TestInRegularAndScriptAsync(
@"class Goo
{
}

class Bar
{
    void goo()
    {
        var c = new Goo { [|Gibberish|] = Gibberish };
    }
}",
@"class Goo
{
    public object Gibberish { get; internal set; }
}

class Bar
{
    void goo()
    {
        var c = new Goo { Gibberish = Gibberish };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestPropertyOnObjectInitializer2()
        {
            await TestInRegularAndScriptAsync(
@"class Goo
{
}

class Bar
{
    void goo()
    {
        var c = new Goo { Gibberish = [|Gibberish|] };
    }
}",
@"class Goo
{
}

class Bar
{
    public object Gibberish { get; private set; }

    void goo()
    {
        var c = new Goo { Gibberish = Gibberish };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestFieldOnObjectInitializer()
        {
            await TestInRegularAndScriptAsync(
@"class Goo
{
}

class Bar
{
    void goo()
    {
        var c = new Goo { [|Gibberish|] = 24 };
    }
}",
@"class Goo
{
    internal int Gibberish;
}

class Bar
{
    void goo()
    {
        var c = new Goo { Gibberish = 24 };
    }
}",
index: ReadonlyFieldIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestFieldOnObjectInitializer1()
        {
            await TestInRegularAndScriptAsync(
@"class Goo
{
}

class Bar
{
    void goo()
    {
        var c = new Goo { [|Gibberish|] = Gibberish };
    }
}",
@"class Goo
{
    internal object Gibberish;
}

class Bar
{
    void goo()
    {
        var c = new Goo { Gibberish = Gibberish };
    }
}",
index: ReadonlyFieldIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestFieldOnObjectInitializer2()
        {
            await TestInRegularAndScriptAsync(
@"class Goo
{
}

class Bar
{
    void goo()
    {
        var c = new Goo { Gibberish = [|Gibberish|] };
    }
}",
@"class Goo
{
}

class Bar
{
    private object Gibberish;

    void goo()
    {
        var c = new Goo { Gibberish = Gibberish };
    }
}",
index: ReadonlyFieldIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestOnlyPropertyAndFieldOfferedForObjectInitializer()
        {
            await TestActionCountAsync(
@"class Goo
{
}

class Bar
{
    void goo()
    {
        var c = new Goo { . [|Gibberish|] = 24 };
    }
}",
2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateLocalInObjectInitializerValue()
        {
            await TestInRegularAndScriptAsync(
@"class Goo
{
}

class Bar
{
    void goo()
    {
        var c = new Goo { Gibberish = [|blah|] };
    }
}",
@"class Goo
{
}

class Bar
{
    void goo()
    {
        object blah = null;
        var c = new Goo { Gibberish = blah };
    }
}",
index: LocalIndex);
        }

        [WorkItem(544319, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544319")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestNotOnIncompleteMember1()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Class1
{
    Console.[|WriteLine|](); }");
        }

        [WorkItem(544319, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544319")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestNotOnIncompleteMember2()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Class1
{ [|WriteLine|]();
}");
        }

        [WorkItem(544319, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544319")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestNotOnIncompleteMember3()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Class1
{
    [|WriteLine|]
}");
        }

        [WorkItem(544384, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544384")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestPointerType()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    static int x;

    unsafe static void F(int* p)
    {
        *p = 1;
    }

    static unsafe void Main(string[] args)
    {
        int[] a = new int[10];
        fixed (int* p2 = &x, int* p3 = ) F(GetP2([|p2|]));
    }

    unsafe private static int* GetP2(int* p2)
    {
        return p2;
    }
}",
@"class Program
{
    static int x;
    private static unsafe int* p2;

    unsafe static void F(int* p)
    {
        *p = 1;
    }

    static unsafe void Main(string[] args)
    {
        int[] a = new int[10];
        fixed (int* p2 = &x, int* p3 = ) F(GetP2(p2));
    }

    unsafe private static int* GetP2(int* p2)
    {
        return p2;
    }
}");
        }

        [WorkItem(544510, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544510")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestNotOnUsingAlias()
        {
            await TestMissingInRegularAndScriptAsync(
@"using [|S|] = System ; S . Console . WriteLine ( ""hello world"" ) ; ");
        }

        [WorkItem(544907, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544907")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestExpressionTLambda()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Linq.Expressions;

class C
{
    static void Main()
    {
        Expression<Func<int, int>> e = x => [|Goo|];
    }
}",
@"using System;
using System.Linq.Expressions;

class C
{
    public static int Goo { get; private set; }

    static void Main()
    {
        Expression<Func<int, int>> e = x => Goo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestNoGenerationIntoEntirelyHiddenType()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void Goo()
    {
        int i = D.[|Bar|];
    }
}

#line hidden
class D
{
}
#line default");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInReturnStatement()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        return [|goo|];
    }
}",
@"class Program
{
    private object goo;

    void Main()
    {
        return goo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestLocal1()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        Goo([|bar|]);
    }

    static void Goo(int i)
    {
    }
}",
@"class Program
{
    void Main()
    {
        int bar = 0;
        Goo(bar);
    }

    static void Goo(int i)
    {
    }
}",
index: LocalIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestLocalTopLevelNullability()
        {
            await TestInRegularAndScriptAsync(
@"#nullable enable

class Program
{
    void Main()
    {
        Goo([|bar|]);
    }

    static void Goo(string? s)
    {
    }
}",
@"#nullable enable

class Program
{
    void Main()
    {
        string? bar = null;
        Goo(bar);
    }

    static void Goo(string? s)
    {
    }
}",
index: LocalIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestLocalNestedNullability()
        {
            await TestInRegularAndScriptAsync(
@"#nullable enable

class Program
{
    void Main()
    {
        Goo([|bar|]);
    }

    static void Goo(IEnumerable<string?> s)
    {
    }
}",
@"#nullable enable

class Program
{
    void Main()
    {
        IEnumerable<string?> bar = null;
        Goo(bar);
    }

    static void Goo(IEnumerable<string?> s)
    {
    }
}",
index: LocalIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestLocalMissingForVar()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        var x = [|var|];
    }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestOutLocal1()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        Goo(out [|bar|]);
    }

    static void Goo(out int i)
    {
    }
}",
@"class Program
{
    void Main()
    {
        int bar;
        Goo(out bar);
    }

    static void Goo(out int i)
    {
    }
}",
index: ReadonlyFieldIndex);
        }

        [WorkItem(809542, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/809542")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestLocalBeforeComment()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
#if true
        // Banner Line 1
        // Banner Line 2
        int.TryParse(""123"", out [|local|]);
#endif
    }
}",
@"class Program
{
    void Main()
    {
#if true
        int local;
        // Banner Line 1
        // Banner Line 2
        int.TryParse(""123"", out [|local|]);
#endif
    }
}",
index: ReadonlyFieldIndex);
        }

        [WorkItem(809542, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/809542")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestLocalAfterComment()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
#if true
        // Banner Line 1
        // Banner Line 2

        int.TryParse(""123"", out [|local|]);
#endif
    }
}",
@"class Program
{
    void Main()
    {
#if true
        // Banner Line 1
        // Banner Line 2

        int local;
        int.TryParse(""123"", out [|local|]);
#endif
    }
}",
index: ReadonlyFieldIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateIntoVisiblePortion()
        {
            await TestInRegularAndScriptAsync(
@"using System;

#line hidden
class Program
{
    void Main()
    {
#line default
        Goo(Program.[|X|])
    }
}",
@"using System;

#line hidden
class Program
{
    void Main()
    {
#line default
        Goo(Program.X)
    }

    public static object X { get; private set; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMissingWhenNoAvailableRegionToGenerateInto()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

#line hidden
class Program
{
    void Main()
    {
#line default
        Goo(Program.[|X|])


#line hidden
    }
}
#line default");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateLocalAvailableIfBlockIsNotHidden()
        {
            await TestInRegularAndScriptAsync(
@"using System;

#line hidden
class Program
{
#line default
    void Main()
    {
        Goo([|x|]);
    }
#line hidden
}
#line default",
@"using System;

#line hidden
class Program
{
#line default
    void Main()
    {
        object x = null;
        Goo(x);
    }
#line hidden
}
#line default");
        }

        [WorkItem(545217, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545217")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateLocalNameSimplificationCSharp7()
        {
            await TestAsync(
@"class Program
{
    void goo()
    {
        bar([|xyz|]);
    }

    struct sfoo
    {
    }

    void bar(sfoo x)
    {
    }
}",
@"class Program
{
    void goo()
    {
        sfoo xyz = default(sfoo);
        bar(xyz);
    }

    struct sfoo
    {
    }

    void bar(sfoo x)
    {
    }
}",
index: 3, parseOptions: new CSharpParseOptions(LanguageVersion.CSharp7));
        }

        [WorkItem(545217, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545217")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateLocalNameSimplification()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void goo()
    {
        bar([|xyz|]);
    }

    struct sfoo
    {
    }

    void bar(sfoo x)
    {
    }
}",
@"class Program
{
    void goo()
    {
        sfoo xyz = default;
        bar(xyz);
    }

    struct sfoo
    {
    }

    void bar(sfoo x)
    {
    }
}",
index: LocalIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestParenthesizedExpression()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        int v = 1 + ([|k|]);
    }
}",
@"class Program
{
    private int k;

    void Main()
    {
        int v = 1 + (k);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInSelect()
        {
            await TestInRegularAndScriptAsync(
@"using System.Linq;

class Program
{
    void Main(string[] args)
    {
        var q = from a in args
                select [|v|];
    }
}",
@"using System.Linq;

class Program
{
    private object v;

    void Main(string[] args)
    {
        var q = from a in args
                select v;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInChecked()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        int[] a = null;
        int[] temp = checked([|goo|]);
    }
}",
@"class Program
{
    private int[] goo;

    void Main()
    {
        int[] a = null;
        int[] temp = checked(goo);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInArrayRankSpecifier()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        var v = new int[[|k|]];
    }
}",
@"class Program
{
    private int k;

    void Main()
    {
        var v = new int[k];
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInConditional1()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    static void Main()
    {
        int i = [|goo|] ? bar : baz;
    }
}",
@"class Program
{
    private static bool goo;

    static void Main()
    {
        int i = goo ? bar : baz;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInConditional2()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    static void Main()
    {
        int i = goo ? [|bar|] : baz;
    }
}",
@"class Program
{
    private static int bar;

    static void Main()
    {
        int i = goo ? bar : baz;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInConditional3()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    static void Main()
    {
        int i = goo ? bar : [|baz|];
    }
}",
@"class Program
{
    private static int baz;

    static void Main()
    {
        int i = goo ? bar : baz;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInCast()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        var x = (int)[|y|];
    }
}",
@"class Program
{
    private int y;

    void Main()
    {
        var x = (int)y;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInIf()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        if ([|goo|])
        {
        }
    }
}",
@"class Program
{
    private bool goo;

    void Main()
    {
        if (goo)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInSwitch()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        switch ([|goo|])
        {
        }
    }
}",
@"class Program
{
    private int goo;

    void Main()
    {
        switch (goo)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMissingOnNamespace()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        [|System|].Console.WriteLine(4);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMissingOnType()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        [|System.Console|].WriteLine(4);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMissingOnBase()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        [|base|].ToString();
    }
}");
        }

        [WorkItem(545273, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545273")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFromAssign1()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        [|undefined|] = 1;
    }
}",
@"class Program
{
    void Main()
    {
        var undefined = 1;
    }
}",
index: PropertyIndex, options: ImplicitTypingEverywhere());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestFuncAssignment()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        [|undefined|] = (x) => 2;
    }
}",
@"class Program
{
    void Main()
    {
        System.Func<object, int> undefined = (x) => 2;
    }
}",
index: PropertyIndex);
        }

        [WorkItem(545273, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545273")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFromAssign1NotAsVar()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        [|undefined|] = 1;
    }
}",
@"class Program
{
    void Main()
    {
        int undefined = 1;
    }
}",
index: PropertyIndex);
        }

        [WorkItem(545273, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545273")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFromAssign2()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        [|undefined|] = new { P = ""1"" };
    }
}",
@"class Program
{
    void Main()
    {
        var undefined = new { P = ""1"" };
    }
}",
index: PropertyIndex);
        }

        [WorkItem(545269, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545269")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateInVenus1()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
#line 1 ""goo""
    void Goo()
    {
        this.[|Bar|] = 1;
    }
#line default
#line hidden
}");
        }

        [WorkItem(545269, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545269")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateInVenus2()
        {
            var code = @"
class C
{
#line 1 ""goo""
    void Goo()
    {
        [|Bar|] = 1;
    }
#line default
#line hidden
}
";
            await TestExactActionSetOfferedAsync(code, new[] { string.Format(FeaturesResources.Generate_local_0, "Bar"), string.Format(FeaturesResources.Generate_parameter_0, "Bar") });

            await TestInRegularAndScriptAsync(code,
@"
class C
{
#line 1 ""goo""
    void Goo()
    {
        var [|Bar|] = 1;
    }
#line default
#line hidden
}
", options: ImplicitTypingEverywhere());
        }

        [WorkItem(546027, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546027")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGeneratePropertyFromAttribute()
        {
            await TestInRegularAndScriptAsync(
@"using System;

[AttributeUsage(AttributeTargets.Class)]
class MyAttrAttribute : Attribute
{
}

[MyAttr(123, [|Value|] = 1)]
class D
{
}",
@"using System;

[AttributeUsage(AttributeTargets.Class)]
class MyAttrAttribute : Attribute
{
    public int Value { get; set; }
}

[MyAttr(123, Value = 1)]
class D
{
}");
        }

        [WorkItem(545232, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545232")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestNewLinePreservationBeforeInsertingLocal()
        {
            await TestInRegularAndScriptAsync(
@"using System;
namespace CSharpDemoApp
{
    class Program
    {
        static void Main(string[] args)
        {
            const int MEGABYTE = 1024 * 1024;
            Console.WriteLine(MEGABYTE);
 
            Calculate([|multiplier|]);
        }
        static void Calculate(double multiplier = Math.PI)
        {
        }
    }
}
",
@"using System;
namespace CSharpDemoApp
{
    class Program
    {
        static void Main(string[] args)
        {
            const int MEGABYTE = 1024 * 1024;
            Console.WriteLine(MEGABYTE);

            double multiplier = 0;
            Calculate(multiplier);
        }
        static void Calculate(double multiplier = Math.PI)
        {
        }
    }
}
",
index: LocalIndex);
        }

        [WorkItem(863346, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/863346")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateInGenericMethod_Local()
        {
            await TestInRegularAndScriptAsync(
@"using System;
class TestClass<T1>
{
    static T TestMethod<T>(T item)
    {
        T t = WrapFunc<T>([|NewLocal|]);
        return t;
    }

    private static T WrapFunc<T>(Func<T1, T> function)
    {
        T1 zoo = default(T1);
        return function(zoo);
    }
}
",
@"using System;
class TestClass<T1>
{
    static T TestMethod<T>(T item)
    {
        Func<T1, T> NewLocal = null;
        T t = WrapFunc<T>(NewLocal);
        return t;
    }

    private static T WrapFunc<T>(Func<T1, T> function)
    {
        T1 zoo = default(T1);
        return function(zoo);
    }
}
",
index: LocalIndex);
        }

        [WorkItem(863346, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/863346")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateInGenericMethod_Property()
        {
            await TestInRegularAndScriptAsync(
@"using System;
class TestClass<T1>
{
    static T TestMethod<T>(T item)
    {
        T t = WrapFunc<T>([|NewLocal|]);
        return t;
    }

    private static T WrapFunc<T>(Func<T1, T> function)
    {
        T1 zoo = default(T1);
        return function(zoo);
    }
}
",
@"using System;
class TestClass<T1>
{
    public static Func<T1, object> NewLocal { get; private set; }

    static T TestMethod<T>(T item)
    {
        T t = WrapFunc<T>(NewLocal);
        return t;
    }

    private static T WrapFunc<T>(Func<T1, T> function)
    {
        T1 zoo = default(T1);
        return function(zoo);
    }
}
");
        }

        [WorkItem(865067, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/865067")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestWithYieldReturnInMethod()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;

class Program
{
    IEnumerable<DayOfWeek> Goo()
    {
        yield return [|abc|];
    }
}",
@"using System;
using System.Collections.Generic;

class Program
{
    private DayOfWeek abc;

    IEnumerable<DayOfWeek> Goo()
    {
        yield return abc;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestWithYieldReturnInAsyncMethod()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;

class Program
{
    async IAsyncEnumerable<DayOfWeek> Goo()
    {
        yield return [|abc|];
    }
}",
@"using System;
using System.Collections.Generic;

class Program
{
    private DayOfWeek abc;

    async IAsyncEnumerable<DayOfWeek> Goo()
    {
        yield return abc;
    }
}");
        }

        [WorkItem(30235, "https://github.com/dotnet/roslyn/issues/30235")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestWithYieldReturnInLocalFunction()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;

class Program
{
    void M()
    {
        IEnumerable<DayOfWeek> F()
        {
            yield return [|abc|];
        }
    }
}",
@"using System;
using System.Collections.Generic;

class Program
{
    private DayOfWeek abc;

    void M()
    {
        IEnumerable<DayOfWeek> F()
        {
            yield return abc;
        }
    }
}");
        }

        [WorkItem(877580, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/877580")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestWithThrow()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class Program
{
    void Goo()
    {
        throw [|MyExp|];
    }
}",
@"using System;

class Program
{
    private Exception MyExp;

    void Goo()
    {
        throw MyExp;
    }
}", index: ReadonlyFieldIndex);
        }

        [WorkItem(530177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestUnsafeField()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|int* a = goo|];
    }
}",
@"class Class
{
    private unsafe int* goo;

    void Method()
    {
        int* a = goo;
    }
}");
        }

        [WorkItem(530177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestUnsafeField2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|int*[] a = goo|];
    }
}",
@"class Class
{
    private unsafe int*[] goo;

    void Method()
    {
        int*[] a = goo;
    }
}");
        }

        [WorkItem(530177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestUnsafeFieldInUnsafeClass()
        {
            await TestInRegularAndScriptAsync(
@"unsafe class Class
{
    void Method()
    {
        [|int* a = goo|];
    }
}",
@"unsafe class Class
{
    private int* goo;

    void Method()
    {
        int* a = goo;
    }
}");
        }

        [WorkItem(530177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestUnsafeFieldInNestedClass()
        {
            await TestInRegularAndScriptAsync(
@"unsafe class Class
{
    class MyClass
    {
        void Method()
        {
            [|int* a = goo|];
        }
    }
}",
@"unsafe class Class
{
    class MyClass
    {
        private int* goo;

        void Method()
        {
            int* a = goo;
        }
    }
}");
        }

        [WorkItem(530177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestUnsafeFieldInNestedClass2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    unsafe class MyClass
    {
        void Method()
        {
            [|int* a = Class.goo|];
        }
    }
}",
@"class Class
{
    private static unsafe int* goo;

    unsafe class MyClass
    {
        void Method()
        {
            int* a = Class.goo;
        }
    }
}");
        }

        [WorkItem(530177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestUnsafeReadOnlyField()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|int* a = goo|];
    }
}",
@"class Class
{
    private readonly unsafe int* goo;

    void Method()
    {
        int* a = goo;
    }
}",
index: ReadonlyFieldIndex);
        }

        [WorkItem(530177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestUnsafeReadOnlyField2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|int*[] a = goo|];
    }
}",
@"class Class
{
    private readonly unsafe int*[] goo;

    void Method()
    {
        int*[] a = goo;
    }
}",
index: ReadonlyFieldIndex);
        }

        [WorkItem(530177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestUnsafeReadOnlyFieldInUnsafeClass()
        {
            await TestInRegularAndScriptAsync(
@"unsafe class Class
{
    void Method()
    {
        [|int* a = goo|];
    }
}",
@"unsafe class Class
{
    private readonly int* goo;

    void Method()
    {
        int* a = goo;
    }
}",
index: ReadonlyFieldIndex);
        }

        [WorkItem(530177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestUnsafeReadOnlyFieldInNestedClass()
        {
            await TestInRegularAndScriptAsync(
@"unsafe class Class
{
    class MyClass
    {
        void Method()
        {
            [|int* a = goo|];
        }
    }
}",
@"unsafe class Class
{
    class MyClass
    {
        private readonly int* goo;

        void Method()
        {
            int* a = goo;
        }
    }
}",
index: ReadonlyFieldIndex);
        }

        [WorkItem(530177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestUnsafeReadOnlyFieldInNestedClass2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    unsafe class MyClass
    {
        void Method()
        {
            [|int* a = Class.goo|];
        }
    }
}",
@"class Class
{
    private static readonly unsafe int* goo;

    unsafe class MyClass
    {
        void Method()
        {
            int* a = Class.goo;
        }
    }
}",
index: ReadonlyFieldIndex);
        }

        [WorkItem(530177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestUnsafeProperty()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|int* a = goo|];
    }
}",
@"class Class
{
    public unsafe int* goo { get; private set; }

    void Method()
    {
        int* a = goo;
    }
}",
index: PropertyIndex);
        }

        [WorkItem(530177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestUnsafeProperty2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|int*[] a = goo|];
    }
}",
@"class Class
{
    public unsafe int*[] goo { get; private set; }

    void Method()
    {
        int*[] a = goo;
    }
}",
index: PropertyIndex);
        }

        [WorkItem(530177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestUnsafePropertyInUnsafeClass()
        {
            await TestInRegularAndScriptAsync(
@"unsafe class Class
{
    void Method()
    {
        [|int* a = goo|];
    }
}",
@"unsafe class Class
{
    public int* goo { get; private set; }

    void Method()
    {
        int* a = goo;
    }
}",
index: PropertyIndex);
        }

        [WorkItem(530177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestUnsafePropertyInNestedClass()
        {
            await TestInRegularAndScriptAsync(
@"unsafe class Class
{
    class MyClass
    {
        void Method()
        {
            [|int* a = goo|];
        }
    }
}",
@"unsafe class Class
{
    class MyClass
    {
        public int* goo { get; private set; }

        void Method()
        {
            int* a = goo;
        }
    }
}",
index: PropertyIndex);
        }

        [WorkItem(530177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestUnsafePropertyInNestedClass2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    unsafe class MyClass
    {
        void Method()
        {
            [|int* a = Class.goo|];
        }
    }
}",
@"class Class
{
    public static unsafe int* goo { get; private set; }

    unsafe class MyClass
    {
        void Method()
        {
            int* a = Class.goo;
        }
    }
}",
index: PropertyIndex);
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInsideNameOfProperty()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = nameof([|Z|]);
    }
}",
@"class C
{
    public object Z { get; private set; }

    void M()
    {
        var x = nameof(Z);
    }
}");
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInsideNameOfField()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = nameof([|Z|]);
    }
}",
@"class C
{
    private object Z;

    void M()
    {
        var x = nameof(Z);
    }
}",
index: ReadonlyFieldIndex);
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInsideNameOfReadonlyField()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = nameof([|Z|]);
    }
}",
@"class C
{
    private readonly object Z;

    void M()
    {
        var x = nameof(Z);
    }
}",
index: PropertyIndex);
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInsideNameOfLocal()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = nameof([|Z|]);
    }
}",
@"class C
{
    void M()
    {
        object Z = null;
        var x = nameof(Z);
    }
}",
index: LocalIndex);
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInsideNameOfProperty2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = nameof([|Z.X|]);
    }
}",
@"class C
{
    public object Z { get; private set; }

    void M()
    {
        var x = nameof(Z.X);
    }
}");
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInsideNameOfField2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = nameof([|Z.X|]);
    }
}",
@"class C
{
    private object Z;

    void M()
    {
        var x = nameof(Z.X);
    }
}",
index: ReadonlyFieldIndex);
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInsideNameOfReadonlyField2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = nameof([|Z.X|]);
    }
}",
@"class C
{
    private readonly object Z;

    void M()
    {
        var x = nameof(Z.X);
    }
}",
index: PropertyIndex);
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInsideNameOfLocal2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = nameof([|Z.X|]);
    }
}",
@"class C
{
    void M()
    {
        object Z = null;
        var x = nameof(Z.X);
    }
}",
index: LocalIndex);
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInsideNameOfProperty3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = nameof([|Z.X.Y|]);
    }
}",
@"class C
{
    public object Z { get; private set; }

    void M()
    {
        var x = nameof(Z.X.Y);
    }
}");
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInsideNameOfField3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = nameof([|Z.X.Y|]);
    }
}",
@"class C
{
    private object Z;

    void M()
    {
        var x = nameof(Z.X.Y);
    }
}",
index: ReadonlyFieldIndex);
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInsideNameOfReadonlyField3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = nameof([|Z.X.Y|]);
    }
}",
@"class C
{
    private readonly object Z;

    void M()
    {
        var x = nameof(Z.X.Y);
    }
}",
index: PropertyIndex);
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInsideNameOfLocal3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = nameof([|Z.X.Y|]);
    }
}",
@"class C
{
    void M()
    {
        object Z = null;
        var x = nameof(Z.X.Y);
    }
}",
index: LocalIndex);
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInsideNameOfMissing()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = [|nameof(1 + 2)|];
    }
}");
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInsideNameOfMissing2()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var y = 1 + 2;
        var x = [|nameof(y)|];
    }
}");
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInsideNameOfMissing3()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var y = 1 + 2;
        var z = """";
        var x = [|nameof(y, z)|];
    }
}");
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInsideNameOfProperty4()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = nameof([|y|], z);
    }
}",
@"class C
{
    public object y { get; private set; }

    void M()
    {
        var x = nameof(y, z);
    }
}",
index: PropertyIndex);
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInsideNameOfField4()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = nameof([|y|], z);
    }
}",
@"class C
{
    private object y;

    void M()
    {
        var x = nameof(y, z);
    }
}");
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInsideNameOfReadonlyField4()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = nameof([|y|], z);
    }
}",
@"class C
{
    private readonly object y;

    void M()
    {
        var x = nameof(y, z);
    }
}",
index: ReadonlyFieldIndex);
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInsideNameOfLocal4()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = nameof([|y|], z);
    }
}",
@"class C
{
    void M()
    {
        object y = null;
        var x = nameof(y, z);
    }
}",
index: LocalIndex);
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInsideNameOfProperty5()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = nameof([|y|]);
    }

    private object nameof(object y)
    {
        return null;
    }
}",
@"class C
{
    public object y { get; private set; }

    void M()
    {
        var x = nameof(y);
    }

    private object nameof(object y)
    {
        return null;
    }
}",
index: PropertyIndex);
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInsideNameOfField5()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = nameof([|y|]);
    }

    private object nameof(object y)
    {
        return null;
    }
}",
@"class C
{
    private object y;

    void M()
    {
        var x = nameof(y);
    }

    private object nameof(object y)
    {
        return null;
    }
}");
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInsideNameOfReadonlyField5()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = nameof([|y|]);
    }

    private object nameof(object y)
    {
        return null;
    }
}",
@"class C
{
    private readonly object y;

    void M()
    {
        var x = nameof(y);
    }

    private object nameof(object y)
    {
        return null;
    }
}",
index: ReadonlyFieldIndex);
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInsideNameOfLocal5()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = nameof([|y|]);
    }

    private object nameof(object y)
    {
        return null;
    }
}",
@"class C
{
    void M()
    {
        object y = null;
        var x = nameof(y);
    }

    private object nameof(object y)
    {
        return null;
    }
}",
index: LocalIndex);
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestConditionalAccessProperty()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Main(C a)
    {
        C x = a?[|.Instance|];
    }
}",
@"class C
{
    public C Instance { get; private set; }

    void Main(C a)
    {
        C x = a?.Instance;
    }
}");
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestConditionalAccessField()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Main(C a)
    {
        C x = a?[|.Instance|];
    }
}",
@"class C
{
    private C Instance;

    void Main(C a)
    {
        C x = a?.Instance;
    }
}",
index: ReadonlyFieldIndex);
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestConditionalAccessReadonlyField()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Main(C a)
    {
        C x = a?[|.Instance|];
    }
}",
@"class C
{
    private readonly C Instance;

    void Main(C a)
    {
        C x = a?.Instance;
    }
}",
index: PropertyIndex);
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestConditionalAccessVarProperty()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Main(C a)
    {
        var x = a?[|.Instance|];
    }
}",
@"class C
{
    public object Instance { get; private set; }

    void Main(C a)
    {
        var x = a?.Instance;
    }
}");
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestConditionalAccessVarField()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Main(C a)
    {
        var x = a?[|.Instance|];
    }
}",
@"class C
{
    private object Instance;

    void Main(C a)
    {
        var x = a?.Instance;
    }
}",
index: ReadonlyFieldIndex);
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestConditionalAccessVarReadOnlyField()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Main(C a)
    {
        var x = a?[|.Instance|];
    }
}",
@"class C
{
    private readonly object Instance;

    void Main(C a)
    {
        var x = a?.Instance;
    }
}",
index: PropertyIndex);
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestConditionalAccessNullableProperty()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Main(C a)
    {
        int? x = a?[|.B|];
    }
}",
@"class C
{
    public int B { get; private set; }

    void Main(C a)
    {
        int? x = a?.B;
    }
}");
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestConditionalAccessNullableField()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Main(C a)
    {
        int? x = a?[|.B|];
    }
}",
@"class C
{
    private int B;

    void Main(C a)
    {
        int? x = a?.B;
    }
}",
index: ReadonlyFieldIndex);
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestConditionalAccessNullableReadonlyField()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Main(C a)
    {
        int? x = a?[|.B|];
    }
}",
@"class C
{
    private readonly int B;

    void Main(C a)
    {
        int? x = a?.B;
    }
}",
index: PropertyIndex);
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGeneratePropertyInConditionalAccessExpression()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        C x = a?.B.[|C|];
    }

    public class E
    {
    }
}",
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        C x = a?.B.C;
    }

    public class E
    {
        public C C { get; internal set; }
    }
}");
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGeneratePropertyInConditionalAccessExpression2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        int x = a?.B.[|C|];
    }

    public class E
    {
    }
}",
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        int x = a?.B.C;
    }

    public class E
    {
        public int C { get; internal set; }
    }
}");
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGeneratePropertyInConditionalAccessExpression3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        int? x = a?.B.[|C|];
    }

    public class E
    {
    }
}",
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        int? x = a?.B.C;
    }

    public class E
    {
        public int C { get; internal set; }
    }
}");
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGeneratePropertyInConditionalAccessExpression4()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        var x = a?.B.[|C|];
    }

    public class E
    {
    }
}",
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        var x = a?.B.C;
    }

    public class E
    {
        public object C { get; internal set; }
    }
}");
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFieldInConditionalAccessExpression()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        C x = a?.B.[|C|];
    }

    public class E
    {
    }
}",
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        C x = a?.B.C;
    }

    public class E
    {
        internal C C;
    }
}",
index: ReadonlyFieldIndex);
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFieldInConditionalAccessExpression2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        int x = a?.B.[|C|];
    }

    public class E
    {
    }
}",
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        int x = a?.B.C;
    }

    public class E
    {
        internal int C;
    }
}",
index: ReadonlyFieldIndex);
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFieldInConditionalAccessExpression3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        int? x = a?.B.[|C|];
    }

    public class E
    {
    }
}",
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        int? x = a?.B.C;
    }

    public class E
    {
        internal int C;
    }
}",
index: ReadonlyFieldIndex);
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFieldInConditionalAccessExpression4()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        var x = a?.B.[|C|];
    }

    public class E
    {
    }
}",
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        var x = a?.B.C;
    }

    public class E
    {
        internal object C;
    }
}",
index: ReadonlyFieldIndex);
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateReadonlyFieldInConditionalAccessExpression()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        C x = a?.B.[|C|];
    }

    public class E
    {
    }
}",
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        C x = a?.B.C;
    }

    public class E
    {
        internal readonly C C;
    }
}",
index: PropertyIndex);
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateReadonlyFieldInConditionalAccessExpression2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        int x = a?.B.[|C|];
    }

    public class E
    {
    }
}",
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        int x = a?.B.C;
    }

    public class E
    {
        internal readonly int C;
    }
}",
index: PropertyIndex);
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateReadonlyFieldInConditionalAccessExpression3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        int? x = a?.B.[|C|];
    }

    public class E
    {
    }
}",
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        int? x = a?.B.C;
    }

    public class E
    {
        internal readonly int C;
    }
}",
index: PropertyIndex);
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateReadonlyFieldInConditionalAccessExpression4()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        var x = a?.B.[|C|];
    }

    public class E
    {
    }
}",
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        var x = a?.B.C;
    }

    public class E
    {
        internal readonly object C;
    }
}",
index: PropertyIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFieldInPropertyInitializers()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    public int MyProperty { get; } = [|y|];
}",
@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    private static int y;

    public int MyProperty { get; } = y;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateReadonlyFieldInPropertyInitializers()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    public int MyProperty { get; } = [|y|];
}",
@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    private static readonly int y;

    public int MyProperty { get; } = y;
}",
index: ReadonlyFieldIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGeneratePropertyInPropertyInitializers()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    public int MyProperty { get; } = [|y|];
}",
@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    public static int y { get; private set; }
    public int MyProperty { get; } = y;
}",
index: PropertyIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFieldInExpressionBodiedProperty()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    public int Y => [|y|];
}",
@"class Program
{
    private int y;

    public int Y => y;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateReadonlyFieldInExpressionBodiedProperty()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    public int Y => [|y|];
}",
@"class Program
{
    private readonly int y;

    public int Y => y;
}",
index: ReadonlyFieldIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGeneratePropertyInExpressionBodiedProperty()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    public int Y => [|y|];
}",
@"class Program
{
    public int Y => y;

    public int y { get; private set; }
}",
index: PropertyIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFieldInExpressionBodiedOperator()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public static C operator --(C p) => [|x|];
}",
@"class C
{
    private static C x;

    public static C operator --(C p) => x;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateReadOnlyFieldInExpressionBodiedOperator()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public static C operator --(C p) => [|x|];
}",
@"class C
{
    private static readonly C x;

    public static C operator --(C p) => x;
}",
index: ReadonlyFieldIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGeneratePropertyInExpressionBodiedOperator()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public static C operator --(C p) => [|x|];
}",
@"class C
{
    public static C x { get; private set; }

    public static C operator --(C p) => x;
}",
index: PropertyIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFieldInExpressionBodiedMethod()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public static C GetValue(C p) => [|x|];
}",
@"class C
{
    private static C x;

    public static C GetValue(C p) => x;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateReadOnlyFieldInExpressionBodiedMethod()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public static C GetValue(C p) => [|x|];
}",
@"class C
{
    private static readonly C x;

    public static C GetValue(C p) => x;
}",
index: ReadonlyFieldIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGeneratePropertyInExpressionBodiedMethod()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public static C GetValue(C p) => [|x|];
}",
@"class C
{
    public static C x { get; private set; }

    public static C GetValue(C p) => x;
}",
index: PropertyIndex);
        }

        [WorkItem(27647, "https://github.com/dotnet/roslyn/issues/27647")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGeneratePropertyInExpressionBodiedAsyncTaskOfTMethod()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public static async System.Threading.Tasks.Task<C> GetValue(C p) => [|x|];
}",
@"class C
{
    public static C x { get; private set; }

    public static async System.Threading.Tasks.Task<C> GetValue(C p) => x;
}",
index: PropertyIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFieldInDictionaryInitializer()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        var x = new Dictionary<string, int> { [[|key|]] = 0 };
    }
}",
@"using System.Collections.Generic;

class Program
{
    private static string key;

    static void Main(string[] args)
    {
        var x = new Dictionary<string, int> { [key] = 0 };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGeneratePropertyInDictionaryInitializer()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        var x = new Dictionary<string, int> { [""Zero""] = 0, [[|One|]] = 1, [""Two""] = 2 };
    }
}",
@"using System.Collections.Generic;

class Program
{
    public static string One { get; private set; }

    static void Main(string[] args)
    {
        var x = new Dictionary<string, int> { [""Zero""] = 0, [One] = 1, [""Two""] = 2 };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFieldInDictionaryInitializer2()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        var x = new Dictionary<string, int> { [""Zero""] = [|i|] };
    }
}",
@"using System.Collections.Generic;

class Program
{
    private static int i;

    static void Main(string[] args)
    {
        var x = new Dictionary<string, int> { [""Zero""] = i };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateReadOnlyFieldInDictionaryInitializer()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        var x = new Dictionary<string, int> { [[|key|]] = 0 };
    }
}",
@"using System.Collections.Generic;

class Program
{
    private static readonly string key;

    static void Main(string[] args)
    {
        var x = new Dictionary<string, int> { [key] = 0 };
    }
}",
index: ReadonlyFieldIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFieldInDictionaryInitializer3()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        var x = new Dictionary<string, int> { [""Zero""] = 0, [[|One|]] = 1, [""Two""] = 2 };
    }
}",
@"using System.Collections.Generic;

class Program
{
    private static string One;

    static void Main(string[] args)
    {
        var x = new Dictionary<string, int> { [""Zero""] = 0, [One] = 1, [""Two""] = 2 };
    }
}",
index: ReadonlyFieldIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateReadOnlyFieldInDictionaryInitializer2()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        var x = new Dictionary<string, int> { [""Zero""] = [|i|] };
    }
}",
@"using System.Collections.Generic;

class Program
{
    private static readonly int i;

    static void Main(string[] args)
    {
        var x = new Dictionary<string, int> { [""Zero""] = i };
    }
}",
index: ReadonlyFieldIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGeneratePropertyInDictionaryInitializer2()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        var x = new Dictionary<string, int> { [[|key|]] = 0 };
    }
}",
@"using System.Collections.Generic;

class Program
{
    public static string key { get; private set; }

    static void Main(string[] args)
    {
        var x = new Dictionary<string, int> { [key] = 0 };
    }
}",
index: PropertyIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateReadOnlyFieldInDictionaryInitializer3()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        var x = new Dictionary<string, int> { [""Zero""] = 0, [[|One|]] = 1, [""Two""] = 2 };
    }
}",
@"using System.Collections.Generic;

class Program
{
    private static readonly string One;

    static void Main(string[] args)
    {
        var x = new Dictionary<string, int> { [""Zero""] = 0, [One] = 1, [""Two""] = 2 };
    }
}",
index: PropertyIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGeneratePropertyInDictionaryInitializer3()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        var x = new Dictionary<string, int> { [""Zero""] = [|i|] };
    }
}",
@"using System.Collections.Generic;

class Program
{
    public static int i { get; private set; }

    static void Main(string[] args)
    {
        var x = new Dictionary<string, int> { [""Zero""] = i };
    }
}",
index: PropertyIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateLocalInDictionaryInitializer()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        var x = new Dictionary<string, int> { [[|key|]] = 0 };
    }
}",
@"using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        string key = null;
        var x = new Dictionary<string, int> { [key] = 0 };
    }
}",
index: LocalIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateLocalInDictionaryInitializer2()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        var x = new Dictionary<string, int> { [""Zero""] = 0, [[|One|]] = 1, [""Two""] = 2 };
    }
}",
@"using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        string One = null;
        var x = new Dictionary<string, int> { [""Zero""] = 0, [One] = 1, [""Two""] = 2 };
    }
}",
index: LocalIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateLocalInDictionaryInitializer3()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        var x = new Dictionary<string, int> { [""Zero""] = [|i|] };
    }
}",
@"using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        int i = 0;
        var x = new Dictionary<string, int> { [""Zero""] = i };
    }
}",
index: LocalIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateVariableFromLambda()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class Program
{
    static void Main(string[] args)
    {
        [|goo|] = () => {
            return 0;
        };
    }
}",
@"using System;

class Program
{
    private static Func<int> goo;

    static void Main(string[] args)
    {
        goo = () => {
            return 0;
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateVariableFromLambda2()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class Program
{
    static void Main(string[] args)
    {
        [|goo|] = () => {
            return 0;
        };
    }
}",
@"using System;

class Program
{
    public static Func<int> goo { get; private set; }

    static void Main(string[] args)
    {
        goo = () => {
            return 0;
        };
    }
}",
index: ReadonlyFieldIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateVariableFromLambda3()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class Program
{
    static void Main(string[] args)
    {
        [|goo|] = () => {
            return 0;
        };
    }
}",
@"using System;

class Program
{
    static void Main(string[] args)
    {
        Func<int> goo = () =>
        {
            return 0;
        };
    }
}",
index: PropertyIndex);
        }

        [WorkItem(8010, "https://github.com/dotnet/roslyn/issues/8010")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerationFromStaticProperty_Field()
        {
            await TestInRegularAndScriptAsync(
@"using System;

public class Test
{
    public static int Property1
    {
        get
        {
            return [|_field|];
        }
    }
}",
@"using System;

public class Test
{
    private static int _field;

    public static int Property1
    {
        get
        {
            return _field;
        }
    }
}");
        }

        [WorkItem(8010, "https://github.com/dotnet/roslyn/issues/8010")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerationFromStaticProperty_ReadonlyField()
        {
            await TestInRegularAndScriptAsync(
@"using System;

public class Test
{
    public static int Property1
    {
        get
        {
            return [|_field|];
        }
    }
}",
@"using System;

public class Test
{
    private static readonly int _field;

    public static int Property1
    {
        get
        {
            return _field;
        }
    }
}",
index: ReadonlyFieldIndex);
        }

        [WorkItem(8010, "https://github.com/dotnet/roslyn/issues/8010")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerationFromStaticProperty_Property()
        {
            await TestInRegularAndScriptAsync(
@"using System;

public class Test
{
    public static int Property1
    {
        get
        {
            return [|goo|];
        }
    }
}",
@"using System;

public class Test
{
    public static int Property1
    {
        get
        {
            return goo;
        }
    }

    public static int goo { get; private set; }
}",
index: PropertyIndex);
        }

        [WorkItem(8010, "https://github.com/dotnet/roslyn/issues/8010")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerationFromStaticProperty_Local()
        {
            await TestInRegularAndScriptAsync(
@"using System;

public class Test
{
    public static int Property1
    {
        get
        {
            return [|goo|];
        }
    }
}",
@"using System;

public class Test
{
    public static int Property1
    {
        get
        {
            int goo = 0;
            return goo;
        }
    }
}",
index: LocalIndex);
        }

        [WorkItem(8358, "https://github.com/dotnet/roslyn/issues/8358")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestSameNameAsInstanceVariableInContainingType()
        {
            await TestInRegularAndScriptAsync(
@"class Outer
{
    int _field;

    class Inner
    {
        public Inner(int field)
        {
            [|_field|] = field;
        }
    }
}",
@"class Outer
{
    int _field;

    class Inner
    {
        private int _field;

        public Inner(int field)
        {
            _field = field;
        }
    }
}");
        }

        [WorkItem(8358, "https://github.com/dotnet/roslyn/issues/8358")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestNotOnStaticWithExistingInstance1()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    int _field;

    void M()
    {
        C.[|_field|] = 42;
    }
}");
        }

        [WorkItem(8358, "https://github.com/dotnet/roslyn/issues/8358")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestNotOnStaticWithExistingInstance2()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    int _field;

    static C()
    {
        [|_field|] = 42;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TupleRead()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method((int, string) i)
    {
        Method([|tuple|]);
    }
}",
@"class Class
{
    private (int, string) tuple;

    void Method((int, string) i)
    {
        Method(tuple);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TupleWithOneNameRead()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method((int a, string) i)
    {
        Method([|tuple|]);
    }
}",
@"class Class
{
    private (int a, string) tuple;

    void Method((int a, string) i)
    {
        Method(tuple);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TupleWrite()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|tuple|] = (1, ""hello"");
    }
}",
@"class Class
{
    private (int, string) tuple;

    void Method()
    {
        tuple = (1, ""hello"");
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TupleWithOneNameWrite()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|tuple|] = (a: 1, ""hello"");
    }
}",
@"class Class
{
    private (int a, string) tuple;

    void Method()
    {
        tuple = (a: 1, ""hello"");
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TupleRefReturnProperties()
        {
            await TestInRegularAndScriptAsync(
@"
using System;
class C
{
    public void Goo()
    {
        ref int i = ref this.[|Bar|];
    }
}",
@"
using System;
class C
{
    public ref int Bar => throw new NotImplementedException();

    public void Goo()
    {
        ref int i = ref this.Bar;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TupleRefWithField()
        {
            await TestInRegularAndScriptAsync(
@"
using System;
class C
{
    public void Goo()
    {
        ref int i = ref this.[|bar|];
    }
}",
@"
using System;
class C
{
    private int bar;

    public void Goo()
    {
        ref int i = ref this.bar;
    }
}");
        }

        [WorkItem(17621, "https://github.com/dotnet/roslyn/issues/17621")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestWithMatchingTypeName1()
        {
            await TestInRegularAndScript1Async(
@"
using System;

public class Goo
{
    public Goo(String goo)
    {
        [|String|] = goo;
    }
}",
@"
using System;

public class Goo
{
    public Goo(String goo)
    {
        String = goo;
    }

    public string String { get; }
}");
        }

        [WorkItem(17621, "https://github.com/dotnet/roslyn/issues/17621")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestWithMatchingTypeName2()
        {
            await TestInRegularAndScript1Async(
@"
using System;

public class Goo
{
    public Goo(String goo)
    {
        [|String|] = goo;
    }
}",
@"
using System;

public class Goo
{
    public Goo(String goo)
    {
        String = goo;
    }

    public string String { get; private set; }
}", index: ReadonlyFieldIndex);
        }

        [WorkItem(18275, "https://github.com/dotnet/roslyn/issues/18275")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestContextualKeyword1()
        {
            await TestMissingInRegularAndScriptAsync(
@"
namespace N
{
    class nameof
    {
    }
}

class C
{
    void M()
    {
        [|nameof|]
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestPreferReadOnlyIfAfterReadOnlyAssignment()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    private readonly int _goo;

    public Class()
    {
        _goo = 0;
        [|_bar|] = 1;
    }
}",
@"class Class
{
    private readonly int _goo;
    private readonly int _bar;

    public Class()
    {
        _goo = 0;
        _bar = 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestPreferReadOnlyIfBeforeReadOnlyAssignment()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    private readonly int _goo;

    public Class()
    {
        [|_bar|] = 1;
        _goo = 0;
    }
}",
@"class Class
{
    private readonly int _bar;
    private readonly int _goo;

    public Class()
    {
        _bar = 1;
        _goo = 0;
    }
}");
        }

        [WorkItem(19239, "https://github.com/dotnet/roslyn/issues/19239")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateReadOnlyPropertyInConstructor()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    public Class()
    {
        [|Bar|] = 1;
    }
}",
@"class Class
{
    public Class()
    {
        Bar = 1;
    }

    public int Bar { get; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestPlaceFieldBasedOnSurroundingStatements()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    private int _goo;
    private int _quux;

    public Class()
    {
        _goo = 0;
        [|_bar|] = 1;
        _quux = 2;
    }
}",
@"class Class
{
    private int _goo;
    private int _bar;
    private int _quux;

    public Class()
    {
        _goo = 0;
        _bar = 1;
        _quux = 2;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestPlaceFieldBasedOnSurroundingStatements2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    private int goo;
    private int quux;

    public Class()
    {
        this.goo = 0;
        this.[|bar|] = 1;
        this.quux = 2;
    }
}",
@"class Class
{
    private int goo;
    private int bar;
    private int quux;

    public Class()
    {
        this.goo = 0;
        this.bar = 1;
        this.quux = 2;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestPlacePropertyBasedOnSurroundingStatements()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    public int Goo { get; }
    public int Quuz { get; }

    public Class()
    {
        Goo = 0;
        [|Bar|] = 1;
        Quux = 2;
    }
}",
@"class Class
{
    public int Goo { get; }
    public int Bar { get; }
    public int Quuz { get; }

    public Class()
    {
        Goo = 0;
        Bar = 1;
        Quux = 2;
    }
}");
        }

        [WorkItem(19575, "https://github.com/dotnet/roslyn/issues/19575")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestNotOnGenericCodeParsedAsExpression()
        {
            await TestMissingAsync(@"
class C
{
    private void GetEvaluationRuleNames()
    {
        [|IEnumerable|] < Int32 >
        return ImmutableArray.CreateRange();
    }
}");
        }

        [WorkItem(19575, "https://github.com/dotnet/roslyn/issues/19575")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestOnNonGenericExpressionWithLessThan()
        {
            await TestInRegularAndScriptAsync(@"
class C
{
    private void GetEvaluationRuleNames()
    {
        [|IEnumerable|] < Int32
        return ImmutableArray.CreateRange();
    }
}",
@"
class C
{
    public int IEnumerable { get; private set; }

    private void GetEvaluationRuleNames()
    {
        IEnumerable < Int32
        return ImmutableArray.CreateRange();
    }
}");
        }

        [WorkItem(18988, "https://github.com/dotnet/roslyn/issues/18988")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task GroupNonReadonlyFieldsTogether()
        {
            await TestInRegularAndScriptAsync(@"
class C
{
    public bool isDisposed;

    public readonly int x;
    public readonly int m;

    public C()
    {
        this.[|y|] = 0;
    }
}",
@"
class C
{
    public bool isDisposed;
    private int y;
    public readonly int x;
    public readonly int m;

    public C()
    {
        this.y = 0;
    }
}");
        }

        [WorkItem(18988, "https://github.com/dotnet/roslyn/issues/18988")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task GroupReadonlyFieldsTogether()
        {
            await TestInRegularAndScriptAsync(@"
class C
{
    public readonly int x;
    public readonly int m;

    public bool isDisposed;

    public C()
    {
        this.[|y|] = 0;
    }
}",
@"
class C
{
    public readonly int x;
    public readonly int m;
    private readonly int y;
    public bool isDisposed;

    public C()
    {
        this.y = 0;
    }
}", index: ReadonlyFieldIndex);
        }

        [WorkItem(20791, "https://github.com/dotnet/roslyn/issues/20791")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestWithOutOverload1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        Goo(out [|goo|]);
    }

    void Goo(int i) { }
    void Goo(out bool b) { }
}",
@"class Class
{
    private bool goo;

    void Method()
    {
        Goo(out goo);
    }

    void Goo(int i) { }
    void Goo(out bool b) { }
}");
        }

        [WorkItem(20791, "https://github.com/dotnet/roslyn/issues/20791")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestWithOutOverload2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        Goo([|goo|]);
    }

    void Goo(out bool b) { }
    void Goo(int i) { }
}",
@"class Class
{
    private int goo;

    void Method()
    {
        Goo(goo);
    }

    void Goo(out bool b) { }
    void Goo(int i) { }
}");
        }

        [WorkItem(20791, "https://github.com/dotnet/roslyn/issues/20791")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestWithRefOverload1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        Goo(ref [|goo|]);
    }

    void Goo(int i) { }
    void Goo(ref bool b) { }
}",
@"class Class
{
    private bool goo;

    void Method()
    {
        Goo(ref goo);
    }

    void Goo(int i) { }
    void Goo(ref bool b) { }
}");
        }

        [WorkItem(20791, "https://github.com/dotnet/roslyn/issues/20791")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestWithRefOverload2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        Goo([|goo|]);
    }

    void Goo(ref bool b) { }
    void Goo(int i) { }
}",
@"class Class
{
    private int goo;

    void Method()
    {
        Goo(goo);
    }

    void Goo(ref bool b) { }
    void Goo(int i) { }
}");
        }

        [WorkItem(26993, "https://github.com/dotnet/roslyn/issues/26993")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFieldInExpressionBodiedGetter()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    public int Property
    {
        get => [|_field|];
    }
}",
@"class Program
{
    private int _field;

    public int Property
    {
        get => _field;
    }
}");
        }

        [WorkItem(26993, "https://github.com/dotnet/roslyn/issues/26993")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFieldInExpressionBodiedGetterWithDifferentAccessibility()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    public int Property
    {
        protected get => [|_field|];
        set => throw new System.NotImplementedException();
    }
}",
@"class Program
{
    private int _field;

    public int Property
    {
        protected get => _field;
        set => throw new System.NotImplementedException();
    }
}");
        }

        [WorkItem(26993, "https://github.com/dotnet/roslyn/issues/26993")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateReadonlyFieldInExpressionBodiedGetter()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    public int Property
    {
        get => [|_readonlyField|];
    }
}",
@"class Program
{
    private readonly int _readonlyField;

    public int Property
    {
        get => _readonlyField;
    }
}",
index: ReadonlyFieldIndex);
        }

        [WorkItem(26993, "https://github.com/dotnet/roslyn/issues/26993")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGeneratePropertyInExpressionBodiedGetter()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    public int Property
    {
        get => [|prop|];
    }
}",
@"class Program
{
    public int Property
    {
        get => prop;
    }
    public int prop { get; private set; }
}",
index: PropertyIndex);
        }

        [WorkItem(26993, "https://github.com/dotnet/roslyn/issues/26993")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFieldInExpressionBodiedSetterInferredFromType()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    public int Property
    {
        set => [|_field|] = value;
    }
}",
@"class Program
{
    private int _field;

    public int Property
    {
        set => _field = value;
    }
}");
        }

        [WorkItem(26993, "https://github.com/dotnet/roslyn/issues/26993")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFieldInExpressionBodiedLocalFunction()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    public void Method()
    {
        int Local() => [|_field|];
    }
}",
@"class Program
{
    private int _field;

    public void Method()
    {
        int Local() => _field;
    }
}");
        }

        [WorkItem(26993, "https://github.com/dotnet/roslyn/issues/26993")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateReadonlyFieldInExpressionBodiedLocalFunction()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    public void Method()
    {
        int Local() => [|_readonlyField|];
    }
}",
@"class Program
{
    private readonly int _readonlyField;

    public void Method()
    {
        int Local() => _readonlyField;
    }
}",
index: ReadonlyFieldIndex);
        }

        [WorkItem(26993, "https://github.com/dotnet/roslyn/issues/26993")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGeneratePropertyInExpressionBodiedLocalFunction()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    public void Method()
    {
        int Local() => [|prop|];
    }
}",
@"class Program
{
    public int prop { get; private set; }

    public void Method()
    {
        int Local() => prop;
    }
}",
index: PropertyIndex);
        }

        [WorkItem(27647, "https://github.com/dotnet/roslyn/issues/27647")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGeneratePropertyInExpressionBodiedAsyncTaskOfTLocalFunction()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    public void Method()
    {
        async System.Threading.Tasks.Task<int> Local() => [|prop|];
    }
}",
@"class Program
{
    public int prop { get; private set; }

    public void Method()
    {
        async System.Threading.Tasks.Task<int> Local() => prop;
    }
}",
index: PropertyIndex);
        }

        [WorkItem(26993, "https://github.com/dotnet/roslyn/issues/26993")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFieldInExpressionBodiedLocalFunctionInferredFromType()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    public void Method()
    {
        int Local() => [|_field|] = 12;
    }
}",
@"class Program
{
    private int _field;

    public void Method()
    {
        int Local() => _field = 12;
    }
}");
        }

        [WorkItem(26993, "https://github.com/dotnet/roslyn/issues/26993")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFieldInBlockBodiedLocalFunction()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    public void Method()
    {
        int Local()
        {
            return [|_field|];
        }
    }
}",
@"class Program
{
    private int _field;

    public void Method()
    {
        int Local()
        {
            return _field;
        }
    }
}");
        }

        [WorkItem(26993, "https://github.com/dotnet/roslyn/issues/26993")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateReadonlyFieldInBlockBodiedLocalFunction()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    public void Method()
    {
        int Local()
        {
            return [|_readonlyField|];
        }
    }
}",
@"class Program
{
    private readonly int _readonlyField;

    public void Method()
    {
        int Local()
        {
            return _readonlyField;
        }
    }
}",
index: ReadonlyFieldIndex);
        }

        [WorkItem(26993, "https://github.com/dotnet/roslyn/issues/26993")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGeneratePropertyInBlockBodiedLocalFunction()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    public void Method()
    {
        int Local()
        {
            return [|prop|];
        }
    }
}",
@"class Program
{
    public int prop { get; private set; }

    public void Method()
    {
        int Local()
        {
            return prop;
        }
    }
}",
index: PropertyIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGeneratePropertyInBlockBodiedAsyncTaskOfTLocalFunction()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    public void Method()
    {
        async System.Threading.Tasks.Task<int> Local()
        {
            return [|prop|];
        }
    }
}",
@"class Program
{
    public int prop { get; private set; }

    public void Method()
    {
        async System.Threading.Tasks.Task<int> Local()
        {
            return prop;
        }
    }
}",
index: PropertyIndex);
        }

        [WorkItem(26993, "https://github.com/dotnet/roslyn/issues/26993")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFieldInBlockBodiedLocalFunctionInferredFromType()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    public void Method()
    {
        int Local() 
        {
            return [|_field|] = 12;
        }
    }
}",
@"class Program
{
    private int _field;

    public void Method()
    {
        int Local() 
        {
            return _field = 12;
        }
    }
}");
        }

        [WorkItem(26993, "https://github.com/dotnet/roslyn/issues/26993")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFieldInBlockBodiedLocalFunctionInsideLambdaExpression()
        {
            await TestInRegularAndScriptAsync(
@"
using System;

class Program
{
    public void Method()
    {
        Action action = () => 
        {
            int Local()
            {
                return [|_field|];
            }
        };
    }
}",
@"
using System;

class Program
{
    private int _field;

    public void Method()
    {
        Action action = () => 
        {
            int Local()
            {
                return _field;
            }
        };
    }
}");
        }

        [WorkItem(26993, "https://github.com/dotnet/roslyn/issues/26993")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFieldInExpressionBodiedLocalFunctionInsideLambdaExpression()
        {
            await TestInRegularAndScriptAsync(
@"
using System;

class Program
{
    public void Method()
    {
        Action action = () => 
        {
            int Local() => [|_field|];
        };
    }
}",
@"
using System;

class Program
{
    private int _field;

    public void Method()
    {
        Action action = () => 
        {
            int Local() => _field;
        };
    }
}");
        }

        [WorkItem(26406, "https://github.com/dotnet/roslyn/issues/26406")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestIdentifierInsideLock1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        lock ([|goo|])
        {
        }
    }
}",
@"class Class
{
    private object goo;

    void Method()
    {
        lock (goo)
        {
        }
    }
}");
        }

        [WorkItem(26406, "https://github.com/dotnet/roslyn/issues/26406")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestIdentifierInsideLock2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        lock ([|goo|])
        {
        }
    }
}",
@"class Class
{
    private readonly object goo;

    void Method()
    {
        lock (goo)
        {
        }
    }
}", index: 1);
        }

        [WorkItem(26406, "https://github.com/dotnet/roslyn/issues/26406")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestIdentifierInsideLock3()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        lock ([|goo|])
        {
        }
    }
}",
@"class Class
{
    public object goo { get; private set; }

    void Method()
    {
        lock (goo)
        {
        }
    }
}", index: 2);
        }

        [WorkItem(9090, "https://github.com/dotnet/roslyn/issues/9090")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestPropertyPatternInIsPattern1()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    void M2()
    {
        object o = null;
        if (o is Blah { [|X|]: int i })
        {
        }
    }

    class Blah
    {
    }
}",
@"
class C
{
    void M2()
    {
        object o = null;
        if (o is Blah { X: int i })
        {
        }
    }

    class Blah
    {
        public int X { get; internal set; }
    }
}");
        }

        [WorkItem(9090, "https://github.com/dotnet/roslyn/issues/9090")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestPropertyPatternInIsPattern2()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    void M2()
    {
        Blah o = null;
        if (o is { [|X|]: int i })
        {
        }
    }

    class Blah
    {
    }
}",
@"
class C
{
    void M2()
    {
        Blah o = null;
        if (o is { X: int i })
        {
        }
    }

    class Blah
    {
        public int X { get; internal set; }
    }
}");
        }

        [WorkItem(9090, "https://github.com/dotnet/roslyn/issues/9090")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestPropertyPatternInIsPattern3()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    void M2()
    {
        object o = null;
        if (o is Blah { X: { [|Y|]: int i } })
        {
        }
    }

    class Frob
    {
    }

    class Blah
    {
        public Frob X;
    }
}",
@"
class C
{
    void M2()
    {
        object o = null;
        if (o is Blah { X: { Y: int i } })
        {
        }
    }

    class Frob
    {
        public int Y { get; internal set; }
    }

    class Blah
    {
        public Frob X;
    }
}");
        }

        [WorkItem(9090, "https://github.com/dotnet/roslyn/issues/9090")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestPropertyPatternInIsPattern4()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    void M2()
    {
        object o = null;
        if (o is Blah { [|X|]: })
        {
        }
    }

    class Blah
    {
    }
}",
@"
class C
{
    void M2()
    {
        object o = null;
        if (o is Blah { X: })
        {
        }
    }

    class Blah
    {
        public object X { get; internal set; }
    }
}");
        }

        [WorkItem(9090, "https://github.com/dotnet/roslyn/issues/9090")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestPropertyPatternInIsPattern5()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    void M2()
    {
        object o = null;
        if (o is Blah { [|X|]: Frob { } })
        {
        }
    }

    class Blah
    {
    }

    class Frob
    {
    }
}",
@"
class C
{
    void M2()
    {
        object o = null;
        if (o is Blah { X: Frob { } })
        {
        }
    }

    class Blah
    {
        public Frob X { get; internal set; }
    }

    class Frob
    {
    }
}");
        }

        [WorkItem(9090, "https://github.com/dotnet/roslyn/issues/9090")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestPropertyPatternInIsPattern6()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    void M2()
    {
        object o = null;
        if (o is Blah { [|X|]: (1, 2) })
        {
        }
    }

    class Blah
    {
    }
}",
@"
class C
{
    void M2()
    {
        object o = null;
        if (o is Blah { X: (1, 2) })
        {
        }
    }

    class Blah
    {
        public (int, int) X { get; internal set; }
    }
}");
        }

        [WorkItem(9090, "https://github.com/dotnet/roslyn/issues/9090")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestPropertyPatternInIsPattern7()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    void M2()
    {
        object o = null;
        if (o is Blah { [|X|]: (y: 1, z: 2) })
        {
        }
    }

    class Blah
    {
    }
}
" + TestResources.NetFX.ValueTuple.tuplelib_cs,
@"
class C
{
    void M2()
    {
        object o = null;
        if (o is Blah { X: (y: 1, z: 2) })
        {
        }
    }

    class Blah
    {
        public (int y, int z) X { get; internal set; }
    }
}
" + TestResources.NetFX.ValueTuple.tuplelib_cs);
        }

        [WorkItem(9090, "https://github.com/dotnet/roslyn/issues/9090")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestPropertyPatternInIsPattern8()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    void M2()
    {
        object o = null;
        if (o is Blah { [|X|]: () })
        {
        }
    }

    class Blah
    {
    }
}",
@"
class C
{
    void M2()
    {
        object o = null;
        if (o is Blah { X: () })
        {
        }
    }

    class Blah
    {
        public object X { get; internal set; }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestExtendedPropertyPatternInIsPattern()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    Blah SomeBlah { get; set; }

    void M2()
    {
        object o = null;
        if (o is C { SomeBlah.[|X|]: (y: 1, z: 2) })
        {
        }
    }

    class Blah
    {
    }
}
" + TestResources.NetFX.ValueTuple.tuplelib_cs,
@"
class C
{
    Blah SomeBlah { get; set; }

    void M2()
    {
        object o = null;
        if (o is C { SomeBlah.X: (y: 1, z: 2) })
        {
        }
    }

    class Blah
    {
        public (int y, int z) X { get; internal set; }
    }
}
" + TestResources.NetFX.ValueTuple.tuplelib_cs, parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestConstantPatternInPropertyPattern()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    Blah SomeBlah { get; set; }

    void M2()
    {
        object o = null;
        if (o is C { SomeBlah: [|MissingConstant|] })
        {
        }
    }

    class Blah
    {
    }
}
",
@"
class C
{
    private const Blah MissingConstant;

    Blah SomeBlah { get; set; }

    void M2()
    {
        object o = null;
        if (o is C { SomeBlah: MissingConstant })
        {
        }
    }

    class Blah
    {
    }
}
", parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestConstantPatternInExtendedPropertyPattern()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    C SomeC { get; set; }
    Blah SomeBlah { get; set; }

    void M2()
    {
        object o = null;
        if (o is C { SomeC.SomeBlah: [|MissingConstant|] })
        {
        }
    }

    class Blah
    {
    }
}
",
@"
class C
{
    private const Blah MissingConstant;

    C SomeC { get; set; }
    Blah SomeBlah { get; set; }

    void M2()
    {
        object o = null;
        if (o is C { SomeC.SomeBlah: MissingConstant })
        {
        }
    }

    class Blah
    {
    }
}
", parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview));
        }

        [WorkItem(9090, "https://github.com/dotnet/roslyn/issues/9090")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestPropertyPatternInIsPattern9()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    void M2()
    {
        object o = null;
        if (o is Blah { [|X|]: (1) })
        {
        }
    }

    class Blah
    {
    }
}",
@"
class C
{
    void M2()
    {
        object o = null;
        if (o is Blah { X: (1) })
        {
        }
    }

    class Blah
    {
        public int X { get; internal set; }
    }
}");
        }

        [WorkItem(9090, "https://github.com/dotnet/roslyn/issues/9090")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestPropertyPatternInIsPattern10()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    void M2()
    {
        object o = null;
        if (o is Blah { [|X|]: (y: 1) })
        {
        }
    }

    class Blah
    {
    }
}",
@"
class C
{
    void M2()
    {
        object o = null;
        if (o is Blah { X: (y: 1) })
        {
        }
    }

    class Blah
    {
        public object X { get; internal set; }
    }
}");
        }

        [WorkItem(9090, "https://github.com/dotnet/roslyn/issues/9090")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestPropertyPatternInIsPatternWithNullablePattern()
        {
            await TestInRegularAndScriptAsync(
@"#nullable enable

class C
{
    void M2()
    {
        object? o = null;
        object? zToMatch = null;
        if (o is Blah { [|X|]: (y: 1, z: zToMatch) })
        {
        }
    }

    class Blah
    {
    }
}",
@"#nullable enable

class C
{
    void M2()
    {
        object? o = null;
        object? zToMatch = null;
        if (o is Blah { X: (y: 1, z: zToMatch) })
        {
        }
    }

    class Blah
    {
        public (int y, object? z) X { get; internal set; }
    }
}");
        }

        [WorkItem(9090, "https://github.com/dotnet/roslyn/issues/9090")]
        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestPropertyPatternInCasePattern1()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    void M2()
    {
        object o = null;
        switch (o)
        {
            case Blah { [|X|]: int i }:
                break;
        }
    }

    class Blah
    {
    }
}",
@"
class C
{
    void M2()
    {
        object o = null;
        switch (o)
        {
            case Blah { X: int i }:
                break;
        }
    }

    class Blah
    {
        public int X { get; internal set; }
    }
}");
        }

        [WorkItem(9090, "https://github.com/dotnet/roslyn/issues/9090")]
        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestPropertyPatternInCasePattern2()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    void M2()
    {
        Blah o = null;
        switch (o)
        {
            case { [|X|]: int i }:
                break;
        }
    }

    class Blah
    {
    }
}",
@"
class C
{
    void M2()
    {
        Blah o = null;
        switch (o)
        {
            case { X: int i }:
                break;
        }
    }

    class Blah
    {
        public int X { get; internal set; }
    }
}");
        }

        [WorkItem(9090, "https://github.com/dotnet/roslyn/issues/9090")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestPropertyPatternInIsSwitchExpression1()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    void M2()
    {
        object o = null;
        _ = o switch { Blah { [|X|]: int i } => 0, _ => 0 };
    }

    class Blah
    {
    }
}",
@"
class C
{
    void M2()
    {
        object o = null;
        _ = o switch { Blah { X: int i } => 0, _ => 0 };
    }

    class Blah
    {
        public int X { get; internal set; }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestPropertyPatternGenerateConstant()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    void M2()
    {
        object o = null;
        _ = o switch { Blah { X: [|Y|] } => 0, _ => 0 };
    }

    class Blah
    {
        public int X;
    }
}",
@"
class C
{
    private const int Y;

    void M2()
    {
        object o = null;
        _ = o switch { Blah { X: Y } => 0, _ => 0 };
    }

    class Blah
    {
        public int X;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestAddParameter()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|goo|];
    }
}",
@"class Class
{
    void Method(object goo)
    {
        goo;
    }
}", index: Parameter);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestAddParameter_DoesntAddToInterface()
        {
            await TestInRegularAndScriptAsync(
@"interface Interface
{
    void Method();
}

class Class
{
    public void Method()
    {
        [|goo|];
    }
}",
@"interface Interface
{
    void Method();
}

class Class
{
    public void Method(object goo)
    {
        [|goo|];
    }
}", index: Parameter);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestAddParameterAndOverrides_AddsToInterface()
        {
            await TestInRegularAndScriptAsync(
@"interface Interface
{
    void Method();
}

class Class : Interface
{
    public void Method()
    {
        [|goo|];
    }
}",
@"interface Interface
{
    void Method(object goo);
}

class Class : Interface
{
    public void Method(object goo)
    {
        [|goo|];
    }
}", index: ParameterAndOverrides);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestAddParameterIsOfCorrectType()
        {
            await TestInRegularAndScriptAsync(
    @"class Class
{
    void Method()
    {
        M1([|goo|]);
    }

    void M1(int a);
}",
    @"class Class
{
    void Method(int goo)
    {
        M1(goo);
    }

    void M1(int a);
}", index: Parameter);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestAddParameterAndOverrides_IsOfCorrectType()
        {
            await TestInRegularAndScriptAsync(
@"interface Interface
{
    void Method();
}

class Class : Interface
{
    public void Method()
    {
        M1([|goo|]);
    }

    void M1(int a);
}",
@"interface Interface
{
    void Method(int goo);
}

class Class : Interface
{
    public void Method(int goo)
    {
        M1(goo);
    }

    void M1(int a);
}", index: ParameterAndOverrides);
        }

        [WorkItem(26502, "https://github.com/dotnet/roslyn/issues/26502")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestNoReadOnlyMembersWhenInLambdaInConstructor()
        {
            await TestExactActionSetOfferedAsync(
@"using System;

class C
{
    public C()
    {
        Action a = () =>
        {
            this.[|Field|] = 1;
        };
    }
}", new[]
{
    string.Format(FeaturesResources.Generate_property_0, "Field"),
    string.Format(FeaturesResources.Generate_field_0, "Field"),
});
        }

        [WorkItem(26502, "https://github.com/dotnet/roslyn/issues/26502")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestNoReadOnlyMembersWhenInLocalFunctionInConstructor()
        {
            await TestExactActionSetOfferedAsync(
@"using System;

class C
{
    public C()
    {
        void Goo()
        {
            this.[|Field|] = 1;
        };
    }
}", new[]
{
    string.Format(FeaturesResources.Generate_property_0, "Field"),
    string.Format(FeaturesResources.Generate_field_0, "Field"),
});
        }

        [WorkItem(45367, "https://github.com/dotnet/roslyn/issues/45367")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task DontOfferPropertyOrFieldInNamespace()
        {
            await TestExactActionSetOfferedAsync(
@"using System;

namespace ConsoleApp5
{
    class MyException: Exception
    
    internal MyException(int error, int offset, string message) : base(message)
    {
        [|Error|] = error;
        Offset = offset;
    }", new[]
{
    string.Format(FeaturesResources.Generate_local_0, "Error", "MyException"),
    string.Format(FeaturesResources.Generate_parameter_0, "Error", "MyException"),
});
        }

        [WorkItem(48172, "https://github.com/dotnet/roslyn/issues/48172")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMissingOfferParameterInTopLevel()
        {
            await TestMissingAsync("[|Console|].WriteLine();", new TestParameters(Options.Regular));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        [WorkItem(47586, "https://github.com/dotnet/roslyn/issues/47586")]
        public async Task TestGenerateParameterFromLambda()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Diagnostics;

class Class
{
    private static void AssertSomething()
    {
        Action<int> call = _ => Debug.Assert([|expected|]);
    }
}",
@"using System;
using System.Diagnostics;

class Class
{
    private static void AssertSomething(bool expected)
    {
        Action<int> call = _ => Debug.Assert(expected);
    }
}", index: Parameter);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        [WorkItem(47586, "https://github.com/dotnet/roslyn/issues/47586")]
        public async Task TestGenerateParameterFromLambdaInLocalFunction()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Diagnostics;

class Class
{
    private static void AssertSomething()
    {
        void M()
        {
            Action<int> call = _ => Debug.Assert([|expected|]);
        }
    }
}",
@"using System;
using System.Diagnostics;

class Class
{
    private static void AssertSomething()
    {
        void M(bool expected)
        {
            Action<int> call = _ => Debug.Assert(expected);
        }
    }
}", index: Parameter);
        }
    }
}
