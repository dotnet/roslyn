// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.ConvertTupleToStruct;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertTupleToStruct
{
    public class ConvertTupleToStructTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpConvertTupleToStructCodeRefactoringProvider();

        protected override ImmutableArray<CodeAction> MassageActions(ImmutableArray<CodeAction> actions)
            => FlattenActions(actions);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task ConvertSingleTupleType()
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
        if (!(obj is NewStruct))
        {
            return false;
        }

        var other = (NewStruct)obj;
        return a == other.a &&
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

    public static implicit operator (int a, int b) (NewStruct value)
    {
        return (value.a, value.b);
    }

    public static implicit operator NewStruct((int a, int b) value)
    {
        return new NewStruct(value.a, value.b);
    }
}";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task ConvertSingleTupleTypeInNamespace()
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
            if (!(obj is NewStruct))
            {
                return false;
            }

            var other = (NewStruct)obj;
            return a == other.a &&
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

        public static implicit operator (int a, int b) (NewStruct value)
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
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task TestNonLiteralNames()
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = [||](a: Foo(), b: Bar());
    }
}
";
            var expected = @"
class Test
{
    void Method()
    {
        var t1 = new {|Rename:NewStruct|}(Foo(), Bar());
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
        if (!(obj is NewStruct))
        {
            return false;
        }

        var other = (NewStruct)obj;
        return System.Collections.Generic.EqualityComparer<object>.Default.Equals(a, other.a) &&
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

    public static implicit operator (object a, object b) (NewStruct value)
    {
        return (value.a, value.b);
    }

    public static implicit operator NewStruct((object a, object b) value)
    {
        return new NewStruct(value.a, value.b);
    }
}";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task ConvertSingleTupleTypeWithInferredName()
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
        if (!(obj is NewStruct))
        {
            return false;
        }

        var other = (NewStruct)obj;
        return a == other.a &&
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

    public static implicit operator (int a, int b) (NewStruct value)
    {
        return (value.a, value.b);
    }

    public static implicit operator NewStruct((int a, int b) value)
    {
        return new NewStruct(value.a, value.b);
    }
}";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task ConvertMultipleInstancesInSameMethod()
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
        if (!(obj is NewStruct))
        {
            return false;
        }

        var other = (NewStruct)obj;
        return a == other.a &&
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

    public static implicit operator (int a, int b) (NewStruct value)
    {
        return (value.a, value.b);
    }

    public static implicit operator NewStruct((int a, int b) value)
    {
        return new NewStruct(value.a, value.b);
    }
}";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task ConvertMultipleInstancesAcrossMethods()
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
        if (!(obj is NewStruct))
        {
            return false;
        }

        var other = (NewStruct)obj;
        return a == other.a &&
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

    public static implicit operator (int a, int b) (NewStruct value)
    {
        return (value.a, value.b);
    }

    public static implicit operator NewStruct((int a, int b) value)
    {
        return new NewStruct(value.a, value.b);
    }
}";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task OnlyConvertMatchingTypesInSameMethod()
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
        if (!(obj is NewStruct))
        {
            return false;
        }

        var other = (NewStruct)obj;
        return a == other.a &&
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

    public static implicit operator (int a, int b) (NewStruct value)
    {
        return (value.a, value.b);
    }

    public static implicit operator NewStruct((int a, int b) value)
    {
        return new NewStruct(value.a, value.b);
    }
}";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task TestFixAllMatchesInSingleMethod()
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
        if (!(obj is NewStruct))
        {
            return false;
        }

        var other = (NewStruct)obj;
        return a == other.a &&
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

    public static implicit operator (int a, int b) (NewStruct value)
    {
        return (value.a, value.b);
    }

    public static implicit operator NewStruct((int a, int b) value)
    {
        return new NewStruct(value.a, value.b);
    }
}";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task TestFixNotAcrossMethods()
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
        if (!(obj is NewStruct))
        {
            return false;
        }

        var other = (NewStruct)obj;
        return a == other.a &&
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

    public static implicit operator (int a, int b) (NewStruct value)
    {
        return (value.a, value.b);
    }

    public static implicit operator NewStruct((int a, int b) value)
    {
        return new NewStruct(value.a, value.b);
    }
}";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task TestTrivia()
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

    public NewStruct(int a, object item2)
    {
        this.a = a;
        this.Item2 = item2;
    }

    public override bool Equals(object obj)
    {
        if (!(obj is NewStruct))
        {
            return false;
        }

        var other = (NewStruct)obj;
        return a == other.a &&
               System.Collections.Generic.EqualityComparer<object>.Default.Equals(this.Item2, other.Item2);
    }

    public override int GetHashCode()
    {
        var hashCode = 913311208;
        hashCode = hashCode * -1521134295 + a.GetHashCode();
        hashCode = hashCode * -1521134295 + System.Collections.Generic.EqualityComparer<object>.Default.GetHashCode(this.Item2);
        return hashCode;
    }

    public void Deconstruct(out int a, out object item2)
    {
        a = this.a;
        item2 = this.Item2;
    }

    public static implicit operator (int a, object) (NewStruct value)
    {
        return (value.a, value.Item2);
    }

    public static implicit operator NewStruct((int a, object) value)
    {
        return new NewStruct(value.a, value.Item2);
    }
}";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task NotIfReferencesAnonymousTypeInternally()
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

            await TestMissingInRegularAndScriptAsync(text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task ConvertMultipleNestedInstancesInSameMethod1()
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
        if (!(obj is NewStruct))
        {
            return false;
        }

        var other = (NewStruct)obj;
        return a == other.a &&
               System.Collections.Generic.EqualityComparer<object>.Default.Equals(b, other.b);
    }

    public override int GetHashCode()
    {
        var hashCode = 2118541809;
        hashCode = hashCode * -1521134295 + a.GetHashCode();
        hashCode = hashCode * -1521134295 + System.Collections.Generic.EqualityComparer<object>.Default.GetHashCode(b);
        return hashCode;
    }

    public void Deconstruct(out int a, out object b)
    {
        a = this.a;
        b = this.b;
    }

    public static implicit operator (int a, object b) (NewStruct value)
    {
        return (value.a, value.b);
    }

    public static implicit operator NewStruct((int a, object b) value)
    {
        return new NewStruct(value.a, value.b);
    }
}";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task ConvertMultipleNestedInstancesInSameMethod2()
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
        if (!(obj is NewStruct))
        {
            return false;
        }

        var other = (NewStruct)obj;
        return a == other.a &&
               System.Collections.Generic.EqualityComparer<object>.Default.Equals(b, other.b);
    }

    public override int GetHashCode()
    {
        var hashCode = 2118541809;
        hashCode = hashCode * -1521134295 + a.GetHashCode();
        hashCode = hashCode * -1521134295 + System.Collections.Generic.EqualityComparer<object>.Default.GetHashCode(b);
        return hashCode;
    }

    public void Deconstruct(out int a, out object b)
    {
        a = this.a;
        b = this.b;
    }

    public static implicit operator (int a, object b) (NewStruct value)
    {
        return (value.a, value.b);
    }

    public static implicit operator NewStruct((int a, object b) value)
    {
        return new NewStruct(value.a, value.b);
    }
}";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task RenameAnnotationOnStartingPoint()
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
        if (!(obj is NewStruct))
        {
            return false;
        }

        var other = (NewStruct)obj;
        return a == other.a &&
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

    public static implicit operator (int a, int b) (NewStruct value)
    {
        return (value.a, value.b);
    }

    public static implicit operator NewStruct((int a, int b) value)
    {
        return new NewStruct(value.a, value.b);
    }
}";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task CapturedMethodTypeParameters()
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
        if (!(obj is NewStruct<X, Y>))
        {
            return false;
        }

        var other = (NewStruct<X, Y>)obj;
        return System.Collections.Generic.EqualityComparer<List<X>>.Default.Equals(a, other.a) &&
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

    public static implicit operator (List<X> a, Y[] b) (NewStruct<X, Y> value)
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
                FeaturesResources.and_update_usages_in_containing_member
            });
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task NewTypeNameCollision()
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
        if (!(obj is NewStruct1))
        {
            return false;
        }

        var other = (NewStruct1)obj;
        return a == other.a &&
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

    public static implicit operator (int a, int b) (NewStruct1 value)
    {
        return (value.a, value.b);
    }

    public static implicit operator NewStruct1((int a, int b) value)
    {
        return new NewStruct1(value.a, value.b);
    }
}";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task TestDuplicatedName()
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
        if (!(obj is NewStruct))
        {
            return false;
        }

        var other = (NewStruct)obj;
        return this.a == other.a &&
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

    public static implicit operator (int a, int a) (NewStruct value)
    {
        return (value.a, value.a);
    }

    public static implicit operator NewStruct((int a, int a) value)
    {
        return new NewStruct(value.a, value.a);
    }
}";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task TestInLambda1()
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
        if (!(obj is NewStruct))
        {
            return false;
        }

        var other = (NewStruct)obj;
        return a == other.a &&
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

    public static implicit operator (int a, int b) (NewStruct value)
    {
        return (value.a, value.b);
    }

    public static implicit operator NewStruct((int a, int b) value)
    {
        return new NewStruct(value.a, value.b);
    }
}";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task TestInLambda2()
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
        if (!(obj is NewStruct))
        {
            return false;
        }

        var other = (NewStruct)obj;
        return a == other.a &&
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

    public static implicit operator (int a, int b) (NewStruct value)
    {
        return (value.a, value.b);
    }

    public static implicit operator NewStruct((int a, int b) value)
    {
        return new NewStruct(value.a, value.b);
    }
}";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task TestInLocalFunction1()
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
        if (!(obj is NewStruct))
        {
            return false;
        }

        var other = (NewStruct)obj;
        return a == other.a &&
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

    public static implicit operator (int a, int b) (NewStruct value)
    {
        return (value.a, value.b);
    }

    public static implicit operator NewStruct((int a, int b) value)
    {
        return new NewStruct(value.a, value.b);
    }
}";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task TestInLocalFunction2()
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
        if (!(obj is NewStruct))
        {
            return false;
        }

        var other = (NewStruct)obj;
        return a == other.a &&
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

    public static implicit operator (int a, int b) (NewStruct value)
    {
        return (value.a, value.b);
    }

    public static implicit operator NewStruct((int a, int b) value)
    {
        return new NewStruct(value.a, value.b);
    }
}";
            await TestInRegularAndScriptAsync(text, expected);
        }

        protected override ParseOptions GetScriptOptions()
            => null;
    }
}
