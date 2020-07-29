// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.ConvertTupleToStruct;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.NamingStyles;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertTupleToStruct
{
    public class ConvertTupleToStructTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpConvertTupleToStructCodeRefactoringProvider();

        protected override ImmutableArray<CodeAction> MassageActions(ImmutableArray<CodeAction> actions)
            => FlattenActions(actions);

        private OptionsCollection GetPreferImplicitTypeOptions(TestHost host)
        {
            var options = this.PreferImplicitTypeWithInfo();
            options.Add(RemoteTestHostOptions.RemoteHostTest, host != TestHost.InProcess);
            return options;
        }

        #region update containing member tests

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task ConvertSingleTupleType(TestHost host)
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = [||](a: 1, b: 2);
    }
}
";
            var expected = @"
class Test
{
    void Method()
    {
        var t1 = new {|Rename:NewStruct|}(a: 1, b: 2);
    }
}

internal struct NewStruct
{
    public int a;
    public int b;

    public NewStruct(int a, int b)
    {
        this.a = a;
        this.b = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewStruct other &&
               a == other.a &&
               b == other.b;
    }

    public override int GetHashCode()
    {
        var hashCode = 2118541809;
        hashCode = hashCode * -1521134295 + a.GetHashCode();
        hashCode = hashCode * -1521134295 + b.GetHashCode();
        return hashCode;
    }

    public void Deconstruct(out int a, out int b)
    {
        a = this.a;
        b = this.b;
    }

    public static implicit operator (int a, int b)(NewStruct value)
    {
        return (value.a, value.b);
    }

    public static implicit operator NewStruct((int a, int b) value)
    {
        return new NewStruct(value.a, value.b);
    }
}";
            await TestInRegularAndScriptAsync(text, expected, options: GetPreferImplicitTypeOptions(host));
        }

        [WorkItem(45451, "https://github.com/dotnet/roslyn/issues/45451")]
        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task ConvertSingleTupleType_ChangeArgumentNameCase(TestHost host)
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = [||](A: 1, B: 2);
    }
}
";
            var expected = @"
class Test
{
    void Method()
    {
        var t1 = new {|Rename:NewStruct|}(a: 1, b: 2);
    }
}

internal struct NewStruct
{
    public int A;
    public int B;

    public NewStruct(int a, int b)
    {
        A = a;
        B = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewStruct other &&
               A == other.A &&
               B == other.B;
    }

    public override int GetHashCode()
    {
        var hashCode = -1817952719;
        hashCode = hashCode * -1521134295 + A.GetHashCode();
        hashCode = hashCode * -1521134295 + B.GetHashCode();
        return hashCode;
    }

    public void Deconstruct(out int a, out int b)
    {
        a = A;
        b = B;
    }

    public static implicit operator (int A, int B)(NewStruct value)
    {
        return (value.A, value.B);
    }

    public static implicit operator NewStruct((int A, int B) value)
    {
        return new NewStruct(value.A, value.B);
    }
}";
            await TestInRegularAndScriptAsync(text, expected, options: GetPreferImplicitTypeOptions(host));
        }

        [WorkItem(45451, "https://github.com/dotnet/roslyn/issues/45451")]
        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task ConvertSingleTupleType_ChangeArgumentNameCase_Uppercase(TestHost host)
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = [||](A: 1, B: 2);
    }
}
";
            var expected = @"
class Test
{
    void Method()
    {
        var t1 = new {|Rename:NewStruct|}(p_a_: 1, p_b_: 2);
    }
}

internal struct NewStruct
{
    public int A;
    public int B;

    public NewStruct(int p_a_, int p_b_)
    {
        A = p_a_;
        B = p_b_;
    }

    public override bool Equals(object obj)
    {
        return obj is NewStruct other &&
               A == other.A &&
               B == other.B;
    }

    public override int GetHashCode()
    {
        var hashCode = -1817952719;
        hashCode = hashCode * -1521134295 + A.GetHashCode();
        hashCode = hashCode * -1521134295 + B.GetHashCode();
        return hashCode;
    }

    public void Deconstruct(out int p_a_, out int p_b_)
    {
        p_a_ = A;
        p_b_ = B;
    }

    public static implicit operator (int A, int B)(NewStruct value)
    {
        return (value.A, value.B);
    }

    public static implicit operator NewStruct((int A, int B) value)
    {
        return new NewStruct(value.A, value.B);
    }
}";
            var options = GetPreferImplicitTypeOptions(host);

            var symbolSpecification = new SymbolSpecification(
                null,
                "Name2",
                ImmutableArray.Create(new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Parameter)),
                accessibilityList: default,
                modifiers: default);

            var namingStyle = new NamingStyle(
                Guid.NewGuid(),
                capitalizationScheme: Capitalization.CamelCase,
                name: "Name2",
                prefix: "p_",
                suffix: "_",
                wordSeparator: "");

            var namingRule = new SerializableNamingRule()
            {
                SymbolSpecificationID = symbolSpecification.ID,
                NamingStyleID = namingStyle.ID,
                EnforcementLevel = ReportDiagnostic.Error
            };

            var info = new NamingStylePreferences(
                ImmutableArray.Create(symbolSpecification),
                ImmutableArray.Create(namingStyle),
                ImmutableArray.Create(namingRule));

            options.Add(NamingStyleOptions.NamingPreferences, info);

            await TestInRegularAndScriptAsync(text, expected, options: options);
        }

        [WorkItem(39916, "https://github.com/dotnet/roslyn/issues/39916")]
        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task ConvertSingleTupleType_Explicit(TestHost host)
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = [||](a: 1, b: 2);
    }
}
";
            var expected = @"
class Test
{
    void Method()
    {
        var t1 = new {|Rename:NewStruct|}(a: 1, b: 2);
    }
}

internal struct NewStruct
{
    public int a;
    public int b;

    public NewStruct(int a, int b)
    {
        this.a = a;
        this.b = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewStruct other &&
               a == other.a &&
               b == other.b;
    }

    public override int GetHashCode()
    {
        int hashCode = 2118541809;
        hashCode = hashCode * -1521134295 + a.GetHashCode();
        hashCode = hashCode * -1521134295 + b.GetHashCode();
        return hashCode;
    }

    public void Deconstruct(out int a, out int b)
    {
        a = this.a;
        b = this.b;
    }

    public static implicit operator (int a, int b)(NewStruct value)
    {
        return (value.a, value.b);
    }

    public static implicit operator NewStruct((int a, int b) value)
    {
        return new NewStruct(value.a, value.b);
    }
}";
            await TestInRegularAndScriptAsync(text, expected,
                options: Option(RemoteTestHostOptions.RemoteHostTest, host != TestHost.InProcess));
        }

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task ConvertSingleTupleTypeNoNames(TestHost host)
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = [||](1, 2);
    }
}
";
            var expected = @"
class Test
{
    void Method()
    {
        var t1 = new {|Rename:NewStruct|}(1, 2);
    }
}

internal struct NewStruct
{
    public int Item1;
    public int Item2;

    public NewStruct(int item1, int item2)
    {
        Item1 = item1;
        Item2 = item2;
    }

    public override bool Equals(object obj)
    {
        return obj is NewStruct other &&
               Item1 == other.Item1 &&
               Item2 == other.Item2;
    }

    public override int GetHashCode()
    {
        var hashCode = -1030903623;
        hashCode = hashCode * -1521134295 + Item1.GetHashCode();
        hashCode = hashCode * -1521134295 + Item2.GetHashCode();
        return hashCode;
    }

    public void Deconstruct(out int item1, out int item2)
    {
        item1 = Item1;
        item2 = Item2;
    }

    public static implicit operator (int, int)(NewStruct value)
    {
        return (value.Item1, value.Item2);
    }

    public static implicit operator NewStruct((int, int) value)
    {
        return new NewStruct(value.Item1, value.Item2);
    }
}";
            await TestInRegularAndScriptAsync(text, expected, options: GetPreferImplicitTypeOptions(host));
        }

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task ConvertSingleTupleTypePartialNames(TestHost host)
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = [||](1, b: 2);
    }
}
";
            var expected = @"
class Test
{
    void Method()
    {
        var t1 = new {|Rename:NewStruct|}(1, b: 2);
    }
}

internal struct NewStruct
{
    public int Item1;
    public int b;

    public NewStruct(int item1, int b)
    {
        Item1 = item1;
        this.b = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewStruct other &&
               Item1 == other.Item1 &&
               b == other.b;
    }

    public override int GetHashCode()
    {
        var hashCode = 174326978;
        hashCode = hashCode * -1521134295 + Item1.GetHashCode();
        hashCode = hashCode * -1521134295 + b.GetHashCode();
        return hashCode;
    }

    public void Deconstruct(out int item1, out int b)
    {
        item1 = Item1;
        b = this.b;
    }

    public static implicit operator (int, int b)(NewStruct value)
    {
        return (value.Item1, value.b);
    }

    public static implicit operator NewStruct((int, int b) value)
    {
        return new NewStruct(value.Item1, value.b);
    }
}";
            await TestInRegularAndScriptAsync(text, expected, options: GetPreferImplicitTypeOptions(host));
        }

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task ConvertFromType(TestHost host)
        {
            var text = @"
class Test
{
    void Method()
    {
        [||](int a, int b) t1 = (a: 1, b: 2);
        (int a, int b) t2 = (a: 1, b: 2);
    }
}
";
            var expected = @"
class Test
{
    void Method()
    {
        {|Rename:NewStruct|} t1 = new NewStruct(a: 1, b: 2);
        NewStruct t2 = new NewStruct(a: 1, b: 2);
    }
}

internal struct NewStruct
{
    public int a;
    public int b;

    public NewStruct(int a, int b)
    {
        this.a = a;
        this.b = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewStruct other &&
               a == other.a &&
               b == other.b;
    }

    public override int GetHashCode()
    {
        var hashCode = 2118541809;
        hashCode = hashCode * -1521134295 + a.GetHashCode();
        hashCode = hashCode * -1521134295 + b.GetHashCode();
        return hashCode;
    }

    public void Deconstruct(out int a, out int b)
    {
        a = this.a;
        b = this.b;
    }

    public static implicit operator (int a, int b)(NewStruct value)
    {
        return (value.a, value.b);
    }

    public static implicit operator NewStruct((int a, int b) value)
    {
        return new NewStruct(value.a, value.b);
    }
}";
            await TestInRegularAndScriptAsync(text, expected, options: GetPreferImplicitTypeOptions(host));
        }

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task ConvertFromType2(TestHost host)
        {
            var text = @"
class Test
{
    (int a, int b) Method()
    {
        [||](int a, int b) t1 = (a: 1, b: 2);
        (int a, int b) t2 = (a: 1, b: 2);
    }
}
";
            var expected = @"
class Test
{
    NewStruct Method()
    {
        {|Rename:NewStruct|} t1 = new NewStruct(a: 1, b: 2);
        NewStruct t2 = new NewStruct(a: 1, b: 2);
    }
}

internal struct NewStruct
{
    public int a;
    public int b;

    public NewStruct(int a, int b)
    {
        this.a = a;
        this.b = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewStruct other &&
               a == other.a &&
               b == other.b;
    }

    public override int GetHashCode()
    {
        var hashCode = 2118541809;
        hashCode = hashCode * -1521134295 + a.GetHashCode();
        hashCode = hashCode * -1521134295 + b.GetHashCode();
        return hashCode;
    }

    public void Deconstruct(out int a, out int b)
    {
        a = this.a;
        b = this.b;
    }

    public static implicit operator (int a, int b)(NewStruct value)
    {
        return (value.a, value.b);
    }

    public static implicit operator NewStruct((int a, int b) value)
    {
        return new NewStruct(value.a, value.b);
    }
}";
            await TestInRegularAndScriptAsync(text, expected, options: GetPreferImplicitTypeOptions(host));
        }

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task ConvertFromType3(TestHost host)
        {
            var text = @"
class Test
{
    (int a, int b) Method()
    {
        [||](int a, int b) t1 = (a: 1, b: 2);
        (int b, int a) t2 = (b: 1, a: 2);
    }
}
";
            var expected = @"
class Test
{
    NewStruct Method()
    {
        {|Rename:NewStruct|} t1 = new NewStruct(a: 1, b: 2);
        (int b, int a) t2 = (b: 1, a: 2);
    }
}

internal struct NewStruct
{
    public int a;
    public int b;

    public NewStruct(int a, int b)
    {
        this.a = a;
        this.b = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewStruct other &&
               a == other.a &&
               b == other.b;
    }

    public override int GetHashCode()
    {
        var hashCode = 2118541809;
        hashCode = hashCode * -1521134295 + a.GetHashCode();
        hashCode = hashCode * -1521134295 + b.GetHashCode();
        return hashCode;
    }

    public void Deconstruct(out int a, out int b)
    {
        a = this.a;
        b = this.b;
    }

    public static implicit operator (int a, int b)(NewStruct value)
    {
        return (value.a, value.b);
    }

    public static implicit operator NewStruct((int a, int b) value)
    {
        return new NewStruct(value.a, value.b);
    }
}";
            await TestInRegularAndScriptAsync(text, expected, options: GetPreferImplicitTypeOptions(host));
        }

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task ConvertFromType4(TestHost host)
        {
            var text = @"
class Test
{
    void Method()
    {
        (int a, int b) t1 = (a: 1, b: 2);
        [||](int a, int b) t2 = (a: 1, b: 2);
    }
}
";
            var expected = @"
class Test
{
    void Method()
    {
        NewStruct t1 = new NewStruct(a: 1, b: 2);
        {|Rename:NewStruct|} t2 = new NewStruct(a: 1, b: 2);
    }
}

internal struct NewStruct
{
    public int a;
    public int b;

    public NewStruct(int a, int b)
    {
        this.a = a;
        this.b = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewStruct other &&
               a == other.a &&
               b == other.b;
    }

    public override int GetHashCode()
    {
        var hashCode = 2118541809;
        hashCode = hashCode * -1521134295 + a.GetHashCode();
        hashCode = hashCode * -1521134295 + b.GetHashCode();
        return hashCode;
    }

    public void Deconstruct(out int a, out int b)
    {
        a = this.a;
        b = this.b;
    }

    public static implicit operator (int a, int b)(NewStruct value)
    {
        return (value.a, value.b);
    }

    public static implicit operator NewStruct((int a, int b) value)
    {
        return new NewStruct(value.a, value.b);
    }
}";
            await TestInRegularAndScriptAsync(text, expected, options: GetPreferImplicitTypeOptions(host));
        }

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task ConvertSingleTupleTypeInNamespace(TestHost host)
        {
            var text = @"
namespace N
{
    class Test
    {
        void Method()
        {
            var t1 = [||](a: 1, b: 2);
        }
    }
}
";
            var expected = @"
namespace N
{
    class Test
    {
        void Method()
        {
            var t1 = new {|Rename:NewStruct|}(a: 1, b: 2);
        }
    }

    internal struct NewStruct
    {
        public int a;
        public int b;

        public NewStruct(int a, int b)
        {
            this.a = a;
            this.b = b;
        }

        public override bool Equals(object obj)
        {
            return obj is NewStruct other &&
                   a == other.a &&
                   b == other.b;
        }

        public override int GetHashCode()
        {
            var hashCode = 2118541809;
            hashCode = hashCode * -1521134295 + a.GetHashCode();
            hashCode = hashCode * -1521134295 + b.GetHashCode();
            return hashCode;
        }

        public void Deconstruct(out int a, out int b)
        {
            a = this.a;
            b = this.b;
        }

        public static implicit operator (int a, int b)(NewStruct value)
        {
            return (value.a, value.b);
        }

        public static implicit operator NewStruct((int a, int b) value)
        {
            return new NewStruct(value.a, value.b);
        }
    }
}
";
            await TestInRegularAndScriptAsync(text, expected, options: GetPreferImplicitTypeOptions(host));
        }

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task TestNonLiteralNames(TestHost host)
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = [||](a: Goo(), b: Bar());
    }
}
";
            var expected = @"
class Test
{
    void Method()
    {
        var t1 = new {|Rename:NewStruct|}(Goo(), Bar());
    }
}

internal struct NewStruct
{
    public object a;
    public object b;

    public NewStruct(object a, object b)
    {
        this.a = a;
        this.b = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewStruct other &&
               System.Collections.Generic.EqualityComparer<object>.Default.Equals(a, other.a) &&
               System.Collections.Generic.EqualityComparer<object>.Default.Equals(b, other.b);
    }

    public override int GetHashCode()
    {
        var hashCode = 2118541809;
        hashCode = hashCode * -1521134295 + System.Collections.Generic.EqualityComparer<object>.Default.GetHashCode(a);
        hashCode = hashCode * -1521134295 + System.Collections.Generic.EqualityComparer<object>.Default.GetHashCode(b);
        return hashCode;
    }

    public void Deconstruct(out object a, out object b)
    {
        a = this.a;
        b = this.b;
    }

    public static implicit operator (object a, object b)(NewStruct value)
    {
        return (value.a, value.b);
    }

    public static implicit operator NewStruct((object a, object b) value)
    {
        return new NewStruct(value.a, value.b);
    }
}";
            await TestInRegularAndScriptAsync(text, expected, options: GetPreferImplicitTypeOptions(host));
        }

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task ConvertSingleTupleTypeWithInferredName(TestHost host)
        {
            var text = @"
class Test
{
    void Method(int b)
    {
        var t1 = [||](a: 1, b);
    }
}
";
            var expected = @"
class Test
{
    void Method(int b)
    {
        var t1 = new {|Rename:NewStruct|}(a: 1, b);
    }
}

internal struct NewStruct
{
    public int a;
    public int b;

    public NewStruct(int a, int b)
    {
        this.a = a;
        this.b = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewStruct other &&
               a == other.a &&
               b == other.b;
    }

    public override int GetHashCode()
    {
        var hashCode = 2118541809;
        hashCode = hashCode * -1521134295 + a.GetHashCode();
        hashCode = hashCode * -1521134295 + b.GetHashCode();
        return hashCode;
    }

    public void Deconstruct(out int a, out int b)
    {
        a = this.a;
        b = this.b;
    }

    public static implicit operator (int a, int b)(NewStruct value)
    {
        return (value.a, value.b);
    }

    public static implicit operator NewStruct((int a, int b) value)
    {
        return new NewStruct(value.a, value.b);
    }
}";
            await TestInRegularAndScriptAsync(text, expected, options: GetPreferImplicitTypeOptions(host));
        }

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task ConvertMultipleInstancesInSameMethod(TestHost host)
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = [||](a: 1, b: 2);
        var t2 = (a: 3, b: 4);
    }
}
";
            var expected = @"
class Test
{
    void Method()
    {
        var t1 = new {|Rename:NewStruct|}(a: 1, b: 2);
        var t2 = new NewStruct(a: 3, b: 4);
    }
}

internal struct NewStruct
{
    public int a;
    public int b;

    public NewStruct(int a, int b)
    {
        this.a = a;
        this.b = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewStruct other &&
               a == other.a &&
               b == other.b;
    }

    public override int GetHashCode()
    {
        var hashCode = 2118541809;
        hashCode = hashCode * -1521134295 + a.GetHashCode();
        hashCode = hashCode * -1521134295 + b.GetHashCode();
        return hashCode;
    }

    public void Deconstruct(out int a, out int b)
    {
        a = this.a;
        b = this.b;
    }

    public static implicit operator (int a, int b)(NewStruct value)
    {
        return (value.a, value.b);
    }

    public static implicit operator NewStruct((int a, int b) value)
    {
        return new NewStruct(value.a, value.b);
    }
}";
            await TestInRegularAndScriptAsync(text, expected, options: GetPreferImplicitTypeOptions(host));
        }

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task ConvertMultipleInstancesAcrossMethods(TestHost host)
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = [||](a: 1, b: 2);
        var t2 = (a: 3, b: 4);
    }

    void Method2()
    {
        var t1 = (a: 1, b: 2);
        var t2 = (a: 3, b: 4);
    }
}
";
            var expected = @"
class Test
{
    void Method()
    {
        var t1 = new {|Rename:NewStruct|}(a: 1, b: 2);
        var t2 = new NewStruct(a: 3, b: 4);
    }

    void Method2()
    {
        var t1 = (a: 1, b: 2);
        var t2 = (a: 3, b: 4);
    }
}

internal struct NewStruct
{
    public int a;
    public int b;

    public NewStruct(int a, int b)
    {
        this.a = a;
        this.b = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewStruct other &&
               a == other.a &&
               b == other.b;
    }

    public override int GetHashCode()
    {
        var hashCode = 2118541809;
        hashCode = hashCode * -1521134295 + a.GetHashCode();
        hashCode = hashCode * -1521134295 + b.GetHashCode();
        return hashCode;
    }

    public void Deconstruct(out int a, out int b)
    {
        a = this.a;
        b = this.b;
    }

    public static implicit operator (int a, int b)(NewStruct value)
    {
        return (value.a, value.b);
    }

    public static implicit operator NewStruct((int a, int b) value)
    {
        return new NewStruct(value.a, value.b);
    }
}";
            await TestInRegularAndScriptAsync(text, expected, options: GetPreferImplicitTypeOptions(host));
        }

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task OnlyConvertMatchingTypesInSameMethod(TestHost host)
        {
            var text = @"
class Test
{
    void Method(int b)
    {
        var t1 = [||](a: 1, b: 2);
        var t2 = (a: 3, b);
        var t3 = (a: 4, b: 5, c: 6);
        var t4 = (b: 5, a: 6);
    }
}
";
            var expected = @"
class Test
{
    void Method(int b)
    {
        var t1 = new {|Rename:NewStruct|}(a: 1, b: 2);
        var t2 = new NewStruct(a: 3, b);
        var t3 = (a: 4, b: 5, c: 6);
        var t4 = (b: 5, a: 6);
    }
}

internal struct NewStruct
{
    public int a;
    public int b;

    public NewStruct(int a, int b)
    {
        this.a = a;
        this.b = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewStruct other &&
               a == other.a &&
               b == other.b;
    }

    public override int GetHashCode()
    {
        var hashCode = 2118541809;
        hashCode = hashCode * -1521134295 + a.GetHashCode();
        hashCode = hashCode * -1521134295 + b.GetHashCode();
        return hashCode;
    }

    public void Deconstruct(out int a, out int b)
    {
        a = this.a;
        b = this.b;
    }

    public static implicit operator (int a, int b)(NewStruct value)
    {
        return (value.a, value.b);
    }

    public static implicit operator NewStruct((int a, int b) value)
    {
        return new NewStruct(value.a, value.b);
    }
}";
            await TestInRegularAndScriptAsync(text, expected, options: GetPreferImplicitTypeOptions(host));
        }

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task TestFixAllMatchesInSingleMethod(TestHost host)
        {
            var text = @"
class Test
{
    void Method(int b)
    {
        var t1 = [||](a: 1, b: 2);
        var t2 = (a: 3, b);
        var t3 = (a: 4, b: 5, c: 6);
        var t4 = (b: 5, a: 6);
    }
}
";
            var expected = @"
class Test
{
    void Method(int b)
    {
        var t1 = new {|Rename:NewStruct|}(a: 1, b: 2);
        var t2 = new NewStruct(a: 3, b);
        var t3 = (a: 4, b: 5, c: 6);
        var t4 = (b: 5, a: 6);
    }
}

internal struct NewStruct
{
    public int a;
    public int b;

    public NewStruct(int a, int b)
    {
        this.a = a;
        this.b = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewStruct other &&
               a == other.a &&
               b == other.b;
    }

    public override int GetHashCode()
    {
        var hashCode = 2118541809;
        hashCode = hashCode * -1521134295 + a.GetHashCode();
        hashCode = hashCode * -1521134295 + b.GetHashCode();
        return hashCode;
    }

    public void Deconstruct(out int a, out int b)
    {
        a = this.a;
        b = this.b;
    }

    public static implicit operator (int a, int b)(NewStruct value)
    {
        return (value.a, value.b);
    }

    public static implicit operator NewStruct((int a, int b) value)
    {
        return new NewStruct(value.a, value.b);
    }
}";
            await TestInRegularAndScriptAsync(text, expected, options: GetPreferImplicitTypeOptions(host));
        }

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task TestFixNotAcrossMethods(TestHost host)
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = [||](a: 1, b: 2);
        var t2 = (a: 3, b: 4);
    }

    void Method2()
    {
        var t1 = (a: 1, b: 2);
        var t2 = (a: 3, b: 4);
    }
}
";
            var expected = @"
class Test
{
    void Method()
    {
        var t1 = new {|Rename:NewStruct|}(a: 1, b: 2);
        var t2 = new NewStruct(a: 3, b: 4);
    }

    void Method2()
    {
        var t1 = (a: 1, b: 2);
        var t2 = (a: 3, b: 4);
    }
}

internal struct NewStruct
{
    public int a;
    public int b;

    public NewStruct(int a, int b)
    {
        this.a = a;
        this.b = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewStruct other &&
               a == other.a &&
               b == other.b;
    }

    public override int GetHashCode()
    {
        var hashCode = 2118541809;
        hashCode = hashCode * -1521134295 + a.GetHashCode();
        hashCode = hashCode * -1521134295 + b.GetHashCode();
        return hashCode;
    }

    public void Deconstruct(out int a, out int b)
    {
        a = this.a;
        b = this.b;
    }

    public static implicit operator (int a, int b)(NewStruct value)
    {
        return (value.a, value.b);
    }

    public static implicit operator NewStruct((int a, int b) value)
    {
        return new NewStruct(value.a, value.b);
    }
}";
            await TestInRegularAndScriptAsync(text, expected, options: GetPreferImplicitTypeOptions(host));
        }

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task TestTrivia(TestHost host)
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = /*1*/ [||]( /*2*/ a /*3*/ : /*4*/ 1 /*5*/ , /*6*/ b /*7*/ = /*8*/ 2 /*9*/ ) /*10*/ ;
    }
}
";
            var expected = @"
class Test
{
    void Method()
    {
        var t1 = /*1*/ new {|Rename:NewStruct|}( /*2*/ a /*3*/ : /*4*/ 1 /*5*/ , /*6*/ b /*7*/ = /*8*/ 2 /*9*/ ) /*10*/ ;
    }
}

internal struct NewStruct
{
    public int a;
    public object Item2;

    public NewStruct(int a, object item2)
    {
        this.a = a;
        Item2 = item2;
    }

    public override bool Equals(object obj)
    {
        return obj is NewStruct other &&
               a == other.a &&
               System.Collections.Generic.EqualityComparer<object>.Default.Equals(Item2, other.Item2);
    }

    public override int GetHashCode()
    {
        var hashCode = 913311208;
        hashCode = hashCode * -1521134295 + a.GetHashCode();
        hashCode = hashCode * -1521134295 + System.Collections.Generic.EqualityComparer<object>.Default.GetHashCode(Item2);
        return hashCode;
    }

    public void Deconstruct(out int a, out object item2)
    {
        a = this.a;
        item2 = Item2;
    }

    public static implicit operator (int a, object)(NewStruct value)
    {
        return (value.a, value.Item2);
    }

    public static implicit operator NewStruct((int a, object) value)
    {
        return new NewStruct(value.a, value.Item2);
    }
}";
            await TestInRegularAndScriptAsync(text, expected, options: GetPreferImplicitTypeOptions(host));
        }

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task NotIfReferencesAnonymousTypeInternally(TestHost host)
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = [||](a: 1, b: new { c = 1, d = 2 });
    }
}
";

            await TestMissingInRegularAndScriptAsync(text,
                parameters: new TestParameters(options: Option(RemoteTestHostOptions.RemoteHostTest, host != TestHost.InProcess)));
        }

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task ConvertMultipleNestedInstancesInSameMethod1(TestHost host)
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = [||](a: 1, b: (object)(a: 1, b: default(object)));
    }
}
";
            var expected = @"
using System.Collections.Generic;

class Test
{
    void Method()
    {
        var t1 = new {|Rename:NewStruct|}(a: 1, (object)new NewStruct(a: 1, default(object)));
    }
}

internal struct NewStruct
{
    public int a;
    public object b;

    public NewStruct(int a, object b)
    {
        this.a = a;
        this.b = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewStruct other &&
               a == other.a &&
               EqualityComparer<object>.Default.Equals(b, other.b);
    }

    public override int GetHashCode()
    {
        var hashCode = 2118541809;
        hashCode = hashCode * -1521134295 + a.GetHashCode();
        hashCode = hashCode * -1521134295 + EqualityComparer<object>.Default.GetHashCode(b);
        return hashCode;
    }

    public void Deconstruct(out int a, out object b)
    {
        a = this.a;
        b = this.b;
    }

    public static implicit operator (int a, object b)(NewStruct value)
    {
        return (value.a, value.b);
    }

    public static implicit operator NewStruct((int a, object b) value)
    {
        return new NewStruct(value.a, value.b);
    }
}";
            await TestInRegularAndScriptAsync(text, expected, options: GetPreferImplicitTypeOptions(host));
        }

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task ConvertMultipleNestedInstancesInSameMethod2(TestHost host)
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = (a: 1, b: (object)[||](a: 1, b: default(object)));
    }
}
";
            var expected = @"
using System.Collections.Generic;

class Test
{
    void Method()
    {
        var t1 = new NewStruct(a: 1, (object)new {|Rename:NewStruct|}(a: 1, default(object)));
    }
}

internal struct NewStruct
{
    public int a;
    public object b;

    public NewStruct(int a, object b)
    {
        this.a = a;
        this.b = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewStruct other &&
               a == other.a &&
               EqualityComparer<object>.Default.Equals(b, other.b);
    }

    public override int GetHashCode()
    {
        var hashCode = 2118541809;
        hashCode = hashCode * -1521134295 + a.GetHashCode();
        hashCode = hashCode * -1521134295 + EqualityComparer<object>.Default.GetHashCode(b);
        return hashCode;
    }

    public void Deconstruct(out int a, out object b)
    {
        a = this.a;
        b = this.b;
    }

    public static implicit operator (int a, object b)(NewStruct value)
    {
        return (value.a, value.b);
    }

    public static implicit operator NewStruct((int a, object b) value)
    {
        return new NewStruct(value.a, value.b);
    }
}";
            await TestInRegularAndScriptAsync(text, expected, options: GetPreferImplicitTypeOptions(host));
        }

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task RenameAnnotationOnStartingPoint(TestHost host)
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = (a: 1, b: 2);
        var t2 = [||](a: 3, b: 4);
    }
}
";
            var expected = @"
class Test
{
    void Method()
    {
        var t1 = new NewStruct(a: 1, b: 2);
        var t2 = new {|Rename:NewStruct|}(a: 3, b: 4);
    }
}

internal struct NewStruct
{
    public int a;
    public int b;

    public NewStruct(int a, int b)
    {
        this.a = a;
        this.b = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewStruct other &&
               a == other.a &&
               b == other.b;
    }

    public override int GetHashCode()
    {
        var hashCode = 2118541809;
        hashCode = hashCode * -1521134295 + a.GetHashCode();
        hashCode = hashCode * -1521134295 + b.GetHashCode();
        return hashCode;
    }

    public void Deconstruct(out int a, out int b)
    {
        a = this.a;
        b = this.b;
    }

    public static implicit operator (int a, int b)(NewStruct value)
    {
        return (value.a, value.b);
    }

    public static implicit operator NewStruct((int a, int b) value)
    {
        return new NewStruct(value.a, value.b);
    }
}";
            await TestInRegularAndScriptAsync(text, expected, options: GetPreferImplicitTypeOptions(host));
        }

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task CapturedMethodTypeParameters(TestHost host)
        {
            var text = @"
class Test<X> where X : struct
{
    void Method<Y>(List<X> x, Y[] y) where Y : class, new()
    {
        var t1 = [||](a: x, b: y);
    }
}
";
            var expected = @"
class Test<X> where X : struct
{
    void Method<Y>(List<X> x, Y[] y) where Y : class, new()
    {
        var t1 = new {|Rename:NewStruct|}<X, Y>(x, y);
    }
}

internal struct NewStruct<X, Y>
    where X : struct
    where Y : class, new()
{
    public List<X> a;
    public Y[] b;

    public NewStruct(List<X> a, Y[] b)
    {
        this.a = a;
        this.b = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewStruct<X, Y> other &&
               System.Collections.Generic.EqualityComparer<List<X>>.Default.Equals(a, other.a) &&
               System.Collections.Generic.EqualityComparer<Y[]>.Default.Equals(b, other.b);
    }

    public override int GetHashCode()
    {
        var hashCode = 2118541809;
        hashCode = hashCode * -1521134295 + System.Collections.Generic.EqualityComparer<List<X>>.Default.GetHashCode(a);
        hashCode = hashCode * -1521134295 + System.Collections.Generic.EqualityComparer<Y[]>.Default.GetHashCode(b);
        return hashCode;
    }

    public void Deconstruct(out List<X> a, out Y[] b)
    {
        a = this.a;
        b = this.b;
    }

    public static implicit operator (List<X> a, Y[] b)(NewStruct<X, Y> value)
    {
        return (value.a, value.b);
    }

    public static implicit operator NewStruct<X, Y>((List<X> a, Y[] b) value)
    {
        return new NewStruct<X, Y>(value.a, value.b);
    }
}";

            await TestExactActionSetOfferedAsync(text, new[]
            {
                FeaturesResources.updating_usages_in_containing_member
            });
            await TestInRegularAndScriptAsync(text, expected, options: GetPreferImplicitTypeOptions(host));
        }

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task NewTypeNameCollision(TestHost host)
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = [||](a: 1, b: 2);
    }
}

class NewStruct
{
}
";
            var expected = @"
class Test
{
    void Method()
    {
        var t1 = new {|Rename:NewStruct1|}(a: 1, b: 2);
    }
}

class NewStruct
{
}

internal struct NewStruct1
{
    public int a;
    public int b;

    public NewStruct1(int a, int b)
    {
        this.a = a;
        this.b = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewStruct1 other &&
               a == other.a &&
               b == other.b;
    }

    public override int GetHashCode()
    {
        var hashCode = 2118541809;
        hashCode = hashCode * -1521134295 + a.GetHashCode();
        hashCode = hashCode * -1521134295 + b.GetHashCode();
        return hashCode;
    }

    public void Deconstruct(out int a, out int b)
    {
        a = this.a;
        b = this.b;
    }

    public static implicit operator (int a, int b)(NewStruct1 value)
    {
        return (value.a, value.b);
    }

    public static implicit operator NewStruct1((int a, int b) value)
    {
        return new NewStruct1(value.a, value.b);
    }
}";
            await TestInRegularAndScriptAsync(text, expected, options: GetPreferImplicitTypeOptions(host));
        }

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task TestDuplicatedName(TestHost host)
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = [||](a: 1, a: 2);
    }
}
";
            var expected = @"
class Test
{
    void Method()
    {
        var t1 = new {|Rename:NewStruct|}(a: 1, a: 2);
    }
}

internal struct NewStruct
{
    public int a;
    public int a;

    public NewStruct(int a, int a)
    {
        this.a = a;
        this.a = a;
    }

    public override bool Equals(object obj)
    {
        return obj is NewStruct other &&
               this.a == other.a &&
               this.a == other.a;
    }

    public override int GetHashCode()
    {
        var hashCode = 2068208952;
        hashCode = hashCode * -1521134295 + this.a.GetHashCode();
        hashCode = hashCode * -1521134295 + this.a.GetHashCode();
        return hashCode;
    }

    public void Deconstruct(out int a, out int a)
    {
        a = this.a;
        a = this.a;
    }

    public static implicit operator (int a, int a)(NewStruct value)
    {
        return (value.a, value.a);
    }

    public static implicit operator NewStruct((int a, int a) value)
    {
        return new NewStruct(value.a, value.a);
    }
}";
            await TestInRegularAndScriptAsync(text, expected, options: GetPreferImplicitTypeOptions(host));
        }

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task TestInLambda1(TestHost host)
        {
            var text = @"
using System;

class Test
{
    void Method()
    {
        var t1 = [||](a: 1, b: 2);
        Action a = () =>
        {
            var t2 = (a: 3, b: 4);
        };
    }
}
";
            var expected = @"
using System;

class Test
{
    void Method()
    {
        var t1 = new {|Rename:NewStruct|}(a: 1, b: 2);
        Action a = () =>
        {
            var t2 = new NewStruct(a: 3, b: 4);
        };
    }
}

internal struct NewStruct
{
    public int a;
    public int b;

    public NewStruct(int a, int b)
    {
        this.a = a;
        this.b = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewStruct other &&
               a == other.a &&
               b == other.b;
    }

    public override int GetHashCode()
    {
        var hashCode = 2118541809;
        hashCode = hashCode * -1521134295 + a.GetHashCode();
        hashCode = hashCode * -1521134295 + b.GetHashCode();
        return hashCode;
    }

    public void Deconstruct(out int a, out int b)
    {
        a = this.a;
        b = this.b;
    }

    public static implicit operator (int a, int b)(NewStruct value)
    {
        return (value.a, value.b);
    }

    public static implicit operator NewStruct((int a, int b) value)
    {
        return new NewStruct(value.a, value.b);
    }
}";
            await TestInRegularAndScriptAsync(text, expected, options: GetPreferImplicitTypeOptions(host));
        }

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task TestInLambda2(TestHost host)
        {
            var text = @"
using System;

class Test
{
    void Method()
    {
        var t1 = (a: 1, b: 2);
        Action a = () =>
        {
            var t2 = [||](a: 3, b: 4);
        };
    }
}
";
            var expected = @"
using System;

class Test
{
    void Method()
    {
        var t1 = new NewStruct(a: 1, b: 2);
        Action a = () =>
        {
            var t2 = new {|Rename:NewStruct|}(a: 3, b: 4);
        };
    }
}

internal struct NewStruct
{
    public int a;
    public int b;

    public NewStruct(int a, int b)
    {
        this.a = a;
        this.b = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewStruct other &&
               a == other.a &&
               b == other.b;
    }

    public override int GetHashCode()
    {
        var hashCode = 2118541809;
        hashCode = hashCode * -1521134295 + a.GetHashCode();
        hashCode = hashCode * -1521134295 + b.GetHashCode();
        return hashCode;
    }

    public void Deconstruct(out int a, out int b)
    {
        a = this.a;
        b = this.b;
    }

    public static implicit operator (int a, int b)(NewStruct value)
    {
        return (value.a, value.b);
    }

    public static implicit operator NewStruct((int a, int b) value)
    {
        return new NewStruct(value.a, value.b);
    }
}";
            await TestInRegularAndScriptAsync(text, expected, options: GetPreferImplicitTypeOptions(host));
        }

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task TestInLocalFunction1(TestHost host)
        {
            var text = @"
using System;

class Test
{
    void Method()
    {
        var t1 = [||](a: 1, b: 2);
        void Goo()
        {
            var t2 = (a: 3, b: 4);
        }
    }
}
";
            var expected = @"
using System;

class Test
{
    void Method()
    {
        var t1 = new {|Rename:NewStruct|}(a: 1, b: 2);
        void Goo()
        {
            var t2 = new NewStruct(a: 3, b: 4);
        }
    }
}

internal struct NewStruct
{
    public int a;
    public int b;

    public NewStruct(int a, int b)
    {
        this.a = a;
        this.b = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewStruct other &&
               a == other.a &&
               b == other.b;
    }

    public override int GetHashCode()
    {
        var hashCode = 2118541809;
        hashCode = hashCode * -1521134295 + a.GetHashCode();
        hashCode = hashCode * -1521134295 + b.GetHashCode();
        return hashCode;
    }

    public void Deconstruct(out int a, out int b)
    {
        a = this.a;
        b = this.b;
    }

    public static implicit operator (int a, int b)(NewStruct value)
    {
        return (value.a, value.b);
    }

    public static implicit operator NewStruct((int a, int b) value)
    {
        return new NewStruct(value.a, value.b);
    }
}";
            await TestInRegularAndScriptAsync(text, expected, options: GetPreferImplicitTypeOptions(host));
        }

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task TestInLocalFunction2(TestHost host)
        {
            var text = @"
using System;

class Test
{
    void Method()
    {
        var t1 = (a: 1, b: 2);
        void Goo()
        {
            var t2 = [||](a: 3, b: 4);
        }
    }
}
";
            var expected = @"
using System;

class Test
{
    void Method()
    {
        var t1 = new NewStruct(a: 1, b: 2);
        void Goo()
        {
            var t2 = new {|Rename:NewStruct|}(a: 3, b: 4);
        }
    }
}

internal struct NewStruct
{
    public int a;
    public int b;

    public NewStruct(int a, int b)
    {
        this.a = a;
        this.b = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewStruct other &&
               a == other.a &&
               b == other.b;
    }

    public override int GetHashCode()
    {
        var hashCode = 2118541809;
        hashCode = hashCode * -1521134295 + a.GetHashCode();
        hashCode = hashCode * -1521134295 + b.GetHashCode();
        return hashCode;
    }

    public void Deconstruct(out int a, out int b)
    {
        a = this.a;
        b = this.b;
    }

    public static implicit operator (int a, int b)(NewStruct value)
    {
        return (value.a, value.b);
    }

    public static implicit operator NewStruct((int a, int b) value)
    {
        return new NewStruct(value.a, value.b);
    }
}";
            await TestInRegularAndScriptAsync(text, expected, options: GetPreferImplicitTypeOptions(host));
        }

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task ConvertWithDefaultNames1(TestHost host)
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = [||](1, 2);
        var t2 = (1, 2);
        var t3 = (a: 1, b: 2);
        var t4 = (Item1: 1, Item2: 2);
        var t5 = (Item1: 1, Item2: 2);
    }
}
";
            var expected = @"
class Test
{
    void Method()
    {
        var t1 = new {|Rename:NewStruct|}(1, 2);
        var t2 = new NewStruct(1, 2);
        var t3 = (a: 1, b: 2);
        var t4 = new NewStruct(item1: 1, item2: 2);
        var t5 = new NewStruct(item1: 1, item2: 2);
    }
}

internal struct NewStruct
{
    public int Item1;
    public int Item2;

    public NewStruct(int item1, int item2)
    {
        Item1 = item1;
        Item2 = item2;
    }

    public override bool Equals(object obj)
    {
        return obj is NewStruct other &&
               Item1 == other.Item1 &&
               Item2 == other.Item2;
    }

    public override int GetHashCode()
    {
        var hashCode = -1030903623;
        hashCode = hashCode * -1521134295 + Item1.GetHashCode();
        hashCode = hashCode * -1521134295 + Item2.GetHashCode();
        return hashCode;
    }

    public void Deconstruct(out int item1, out int item2)
    {
        item1 = Item1;
        item2 = Item2;
    }

    public static implicit operator (int, int)(NewStruct value)
    {
        return (value.Item1, value.Item2);
    }

    public static implicit operator NewStruct((int, int) value)
    {
        return new NewStruct(value.Item1, value.Item2);
    }
}";
            await TestExactActionSetOfferedAsync(text, new[]
            {
                FeaturesResources.updating_usages_in_containing_member,
                FeaturesResources.updating_usages_in_containing_type,
            });
            await TestInRegularAndScriptAsync(text, expected, options: GetPreferImplicitTypeOptions(host));
        }

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task ConvertWithDefaultNames2(TestHost host)
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = (1, 2);
        var t2 = (1, 2);
        var t3 = (a: 1, b: 2);
        var t4 = [||](Item1: 1, Item2: 2);
        var t5 = (Item1: 1, Item2: 2);
    }
}";
            var expected = @"
class Test
{
    void Method()
    {
        var t1 = new NewStruct(1, 2);
        var t2 = new NewStruct(1, 2);
        var t3 = (a: 1, b: 2);
        var t4 = new {|Rename:NewStruct|}(item1: 1, item2: 2);
        var t5 = new NewStruct(item1: 1, item2: 2);
    }
}

internal struct NewStruct
{
    public int Item1;
    public int Item2;

    public NewStruct(int item1, int item2)
    {
        Item1 = item1;
        Item2 = item2;
    }

    public override bool Equals(object obj)
    {
        return obj is NewStruct other &&
               Item1 == other.Item1 &&
               Item2 == other.Item2;
    }

    public override int GetHashCode()
    {
        var hashCode = -1030903623;
        hashCode = hashCode * -1521134295 + Item1.GetHashCode();
        hashCode = hashCode * -1521134295 + Item2.GetHashCode();
        return hashCode;
    }

    public void Deconstruct(out int item1, out int item2)
    {
        item1 = Item1;
        item2 = Item2;
    }

    public static implicit operator (int Item1, int Item2)(NewStruct value)
    {
        return (value.Item1, value.Item2);
    }

    public static implicit operator NewStruct((int Item1, int Item2) value)
    {
        return new NewStruct(value.Item1, value.Item2);
    }
}";
            await TestExactActionSetOfferedAsync(text, new[]
            {
                FeaturesResources.updating_usages_in_containing_member,
                FeaturesResources.updating_usages_in_containing_type,
            });
            await TestInRegularAndScriptAsync(text, expected, options: GetPreferImplicitTypeOptions(host));
        }

        protected override ParseOptions GetScriptOptions()
            => null;

        #endregion

        #region update containing type tests

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task TestCapturedTypeParameter_UpdateType(TestHost host)
        {
            var text = @"
using System;

class Test<T>
{
    void Method(T t)
    {
        var t1 = [||](a: t, b: 2);
    }

    T t;
    void Goo()
    {
        var t2 = (a: t, b: 4);
    }

    void Blah<T>(T t)
    {
        var t2 = (a: t, b: 4);
    }
}
";
            var expected = @"
using System;
using System.Collections.Generic;

class Test<T>
{
    void Method(T t)
    {
        var t1 = new {|Rename:NewStruct|}<T>(t, b: 2);
    }

    T t;
    void Goo()
    {
        var t2 = new NewStruct<T>(t, b: 4);
    }

    void Blah<T>(T t)
    {
        var t2 = (a: t, b: 4);
    }
}

internal struct NewStruct<T>
{
    public T a;
    public int b;

    public NewStruct(T a, int b)
    {
        this.a = a;
        this.b = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewStruct<T> other &&
               EqualityComparer<T>.Default.Equals(a, other.a) &&
               b == other.b;
    }

    public override int GetHashCode()
    {
        var hashCode = 2118541809;
        hashCode = hashCode * -1521134295 + EqualityComparer<T>.Default.GetHashCode(a);
        hashCode = hashCode * -1521134295 + b.GetHashCode();
        return hashCode;
    }

    public void Deconstruct(out T a, out int b)
    {
        a = this.a;
        b = this.b;
    }

    public static implicit operator (T a, int b)(NewStruct<T> value)
    {
        return (value.a, value.b);
    }

    public static implicit operator NewStruct<T>((T a, int b) value)
    {
        return new NewStruct<T>(value.a, value.b);
    }
}";

            await TestExactActionSetOfferedAsync(text, new[]
            {
                FeaturesResources.updating_usages_in_containing_member,
                FeaturesResources.updating_usages_in_containing_type
            });
            await TestInRegularAndScriptAsync(text, expected, index: 1, options: GetPreferImplicitTypeOptions(host));
        }

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task UpdateAllInType_SinglePart_SingleFile(TestHost host)
        {
            var text = @"
using System;

class Test
{
    void Method()
    {
        var t1 = [||](a: 1, b: 2);
    }

    void Goo()
    {
        var t2 = (a: 3, b: 4);
    }
}

class Other
{
    void Method()
    {
        var t1 = (a: 1, b: 2);
    }
}
";
            var expected = @"
using System;

class Test
{
    void Method()
    {
        var t1 = new {|Rename:NewStruct|}(a: 1, b: 2);
    }

    void Goo()
    {
        var t2 = new NewStruct(a: 3, b: 4);
    }
}

class Other
{
    void Method()
    {
        var t1 = (a: 1, b: 2);
    }
}

internal struct NewStruct
{
    public int a;
    public int b;

    public NewStruct(int a, int b)
    {
        this.a = a;
        this.b = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewStruct other &&
               a == other.a &&
               b == other.b;
    }

    public override int GetHashCode()
    {
        var hashCode = 2118541809;
        hashCode = hashCode * -1521134295 + a.GetHashCode();
        hashCode = hashCode * -1521134295 + b.GetHashCode();
        return hashCode;
    }

    public void Deconstruct(out int a, out int b)
    {
        a = this.a;
        b = this.b;
    }

    public static implicit operator (int a, int b)(NewStruct value)
    {
        return (value.a, value.b);
    }

    public static implicit operator NewStruct((int a, int b) value)
    {
        return new NewStruct(value.a, value.b);
    }
}";
            await TestInRegularAndScriptAsync(text, expected, index: 1, options: GetPreferImplicitTypeOptions(host));
        }

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task UpdateAllInType_MultiplePart_SingleFile(TestHost host)
        {
            var text = @"
using System;

partial class Test
{
    void Method()
    {
        var t1 = [||](a: 1, b: 2);
    }
}

partial class Test
{
    (int a, int b) Goo()
    {
        var t2 = (a: 3, b: 4);
    }
}

class Other
{
    void Method()
    {
        var t1 = (a: 1, b: 2);
    }
}
";
            var expected = @"
using System;

partial class Test
{
    void Method()
    {
        var t1 = new {|Rename:NewStruct|}(a: 1, b: 2);
    }
}

partial class Test
{
    NewStruct Goo()
    {
        var t2 = new NewStruct(a: 3, b: 4);
    }
}

class Other
{
    void Method()
    {
        var t1 = (a: 1, b: 2);
    }
}

internal struct NewStruct
{
    public int a;
    public int b;

    public NewStruct(int a, int b)
    {
        this.a = a;
        this.b = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewStruct other &&
               a == other.a &&
               b == other.b;
    }

    public override int GetHashCode()
    {
        var hashCode = 2118541809;
        hashCode = hashCode * -1521134295 + a.GetHashCode();
        hashCode = hashCode * -1521134295 + b.GetHashCode();
        return hashCode;
    }

    public void Deconstruct(out int a, out int b)
    {
        a = this.a;
        b = this.b;
    }

    public static implicit operator (int a, int b)(NewStruct value)
    {
        return (value.a, value.b);
    }

    public static implicit operator NewStruct((int a, int b) value)
    {
        return new NewStruct(value.a, value.b);
    }
}";
            await TestInRegularAndScriptAsync(text, expected, index: 1, options: GetPreferImplicitTypeOptions(host));
        }

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task UpdateAllInType_MultiplePart_MultipleFile(TestHost host)
        {
            var text = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;

partial class Test
{
    void Method()
    {
        var t1 = [||](a: 1, b: 2);
    }
}

partial class Other
{
    void Method()
    {
        var t1 = (a: 1, b: 2);
    }
}
        </Document>
        <Document>
using System;

partial class Test
{
    (int a, int b) Goo()
    {
        var t2 = (a: 3, b: 4);
    }
}

partial class Other
{
    void Goo()
    {
        var t1 = (a: 1, b: 2);
    }
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;

partial class Test
{
    void Method()
    {
        var t1 = new {|Rename:NewStruct|}(a: 1, b: 2);
    }
}

partial class Other
{
    void Method()
    {
        var t1 = (a: 1, b: 2);
    }
}

internal struct NewStruct
{
    public int a;
    public int b;

    public NewStruct(int a, int b)
    {
        this.a = a;
        this.b = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewStruct other &amp;&amp;
               a == other.a &amp;&amp;
               b == other.b;
    }

    public override int GetHashCode()
    {
        var hashCode = 2118541809;
        hashCode = hashCode * -1521134295 + a.GetHashCode();
        hashCode = hashCode * -1521134295 + b.GetHashCode();
        return hashCode;
    }

    public void Deconstruct(out int a, out int b)
    {
        a = this.a;
        b = this.b;
    }

    public static implicit operator (int a, int b)(NewStruct value)
    {
        return (value.a, value.b);
    }

    public static implicit operator NewStruct((int a, int b) value)
    {
        return new NewStruct(value.a, value.b);
    }
}</Document>
        <Document>
using System;

partial class Test
{
    NewStruct Goo()
    {
        var t2 = new NewStruct(a: 3, b: 4);
    }
}

partial class Other
{
    void Goo()
    {
        var t1 = (a: 1, b: 2);
    }
}
        </Document>
    </Project>
</Workspace>";
            await TestInRegularAndScriptAsync(text, expected, index: 1, options: GetPreferImplicitTypeOptions(host));
        }

        #endregion update containing project tests

        #region update containing project tests

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task UpdateAllInProject_MultiplePart_MultipleFile_WithNamespace(TestHost host)
        {
            var text = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;

namespace N
{
    partial class Test
    {
        void Method()
        {
            var t1 = [||](a: 1, b: 2);
        }
    }

    partial class Other
    {
        void Method()
        {
            var t1 = (a: 1, b: 2);
        }
    }
}
        </Document>
        <Document>
using System;

partial class Test
{
    (int a, int b) Goo()
    {
        var t2 = (a: 3, b: 4);
    }
}

partial class Other
{
    void Goo()
    {
        var t1 = (a: 1, b: 2);
    }
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;

namespace N
{
    partial class Test
    {
        void Method()
        {
            var t1 = new {|Rename:NewStruct|}(a: 1, b: 2);
        }
    }

    partial class Other
    {
        void Method()
        {
            var t1 = new NewStruct(a: 1, b: 2);
        }
    }

    internal struct NewStruct
    {
        public int a;
        public int b;

        public NewStruct(int a, int b)
        {
            this.a = a;
            this.b = b;
        }

        public override bool Equals(object obj)
        {
            return obj is NewStruct other &amp;&amp;
                   a == other.a &amp;&amp;
                   b == other.b;
        }

        public override int GetHashCode()
        {
            var hashCode = 2118541809;
            hashCode = hashCode * -1521134295 + a.GetHashCode();
            hashCode = hashCode * -1521134295 + b.GetHashCode();
            return hashCode;
        }

        public void Deconstruct(out int a, out int b)
        {
            a = this.a;
            b = this.b;
        }

        public static implicit operator (int a, int b)(NewStruct value)
        {
            return (value.a, value.b);
        }

        public static implicit operator NewStruct((int a, int b) value)
        {
            return new NewStruct(value.a, value.b);
        }
    }
}
        </Document>
        <Document>
using System;

partial class Test
{
    N.NewStruct Goo()
    {
        var t2 = new N.NewStruct(a: 3, b: 4);
    }
}

partial class Other
{
    void Goo()
    {
        var t1 = new N.NewStruct(a: 1, b: 2);
    }
}
        </Document>
    </Project>
</Workspace>";
            await TestInRegularAndScriptAsync(text, expected, index: 2, options: GetPreferImplicitTypeOptions(host));
        }

        #endregion

        #region update dependent projects

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task UpdateDependentProjects_DirectDependency(TestHost host)
        {
            var text = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;

partial class Test
{
    void Method()
    {
        var t1 = [||](a: 1, b: 2);
    }
}

partial class Other
{
    void Method()
    {
        var t1 = (a: 1, b: 2);
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <ProjectReference>Assembly1</ProjectReference>
        <Document>
using System;

partial class Other
{
    void Goo()
    {
        var t1 = (a: 1, b: 2);
    }
}
        </Document>
    </Project>
</Workspace>";
            var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;

partial class Test
{
    void Method()
    {
        var t1 = new NewStruct(a: 1, b: 2);
    }
}

partial class Other
{
    void Method()
    {
        var t1 = new NewStruct(a: 1, b: 2);
    }
}

public struct NewStruct
{
    public int a;
    public int b;

    public NewStruct(int a, int b)
    {
        this.a = a;
        this.b = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewStruct other &amp;&amp;
               a == other.a &amp;&amp;
               b == other.b;
    }

    public override int GetHashCode()
    {
        var hashCode = 2118541809;
        hashCode = hashCode * -1521134295 + a.GetHashCode();
        hashCode = hashCode * -1521134295 + b.GetHashCode();
        return hashCode;
    }

    public void Deconstruct(out int a, out int b)
    {
        a = this.a;
        b = this.b;
    }

    public static implicit operator (int a, int b)(NewStruct value)
    {
        return (value.a, value.b);
    }

    public static implicit operator NewStruct((int a, int b) value)
    {
        return new NewStruct(value.a, value.b);
    }
}</Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <ProjectReference>Assembly1</ProjectReference>
        <Document>
using System;

partial class Other
{
    void Goo()
    {
        var t1 = new NewStruct(a: 1, b: 2);
    }
}
        </Document>
    </Project>
</Workspace>";
            await TestInRegularAndScriptAsync(text, expected, index: 3, options: GetPreferImplicitTypeOptions(host));
        }

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task UpdateDependentProjects_NoDependency(TestHost host)
        {
            var text = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;

partial class Test
{
    void Method()
    {
        var t1 = [||](a: 1, b: 2);
    }
}

partial class Other
{
    void Method()
    {
        var t1 = (a: 1, b: 2);
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
using System;

partial class Other
{
    void Goo()
    {
        var t1 = (a: 1, b: 2);
    }
}
        </Document>
    </Project>
</Workspace>";
            var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;

partial class Test
{
    void Method()
    {
        var t1 = new NewStruct(a: 1, b: 2);
    }
}

partial class Other
{
    void Method()
    {
        var t1 = new NewStruct(a: 1, b: 2);
    }
}

public struct NewStruct
{
    public int a;
    public int b;

    public NewStruct(int a, int b)
    {
        this.a = a;
        this.b = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewStruct other &amp;&amp;
               a == other.a &amp;&amp;
               b == other.b;
    }

    public override int GetHashCode()
    {
        var hashCode = 2118541809;
        hashCode = hashCode * -1521134295 + a.GetHashCode();
        hashCode = hashCode * -1521134295 + b.GetHashCode();
        return hashCode;
    }

    public void Deconstruct(out int a, out int b)
    {
        a = this.a;
        b = this.b;
    }

    public static implicit operator (int a, int b)(NewStruct value)
    {
        return (value.a, value.b);
    }

    public static implicit operator NewStruct((int a, int b) value)
    {
        return new NewStruct(value.a, value.b);
    }
}</Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
using System;

partial class Other
{
    void Goo()
    {
        var t1 = (a: 1, b: 2);
    }
}
        </Document>
    </Project>
</Workspace>";
            await TestInRegularAndScriptAsync(text, expected, index: 3, options: GetPreferImplicitTypeOptions(host));
        }

        #endregion
    }
}
