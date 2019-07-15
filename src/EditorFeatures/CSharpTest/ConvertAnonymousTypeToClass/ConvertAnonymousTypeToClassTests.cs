// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.ConvertAnonymousTypeToClass;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertAnonymousTypeToClass
{
    public class ConvertAnonymousTypeToClassTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpConvertAnonymousTypeToClassCodeRefactoringProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToClass)]
        public async Task ConvertSingleAnonymousType()
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = [||]new { a = 1, b = 2 };
    }
}
";
            var expected = @"
class Test
{
    void Method()
    {
        var t1 = new {|Rename:NewClass|}(1, 2);
    }
}

internal class NewClass
{
    public int A { get; }
    public int B { get; }

    public NewClass(int a, int b)
    {
        A = a;
        B = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewClass other &&
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
}";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToClass)]
        public async Task OnEmptyAnonymousType()
        {
            await TestInRegularAndScriptAsync(@"
class Test
{
    void Method()
    {
        var t1 = [||]new { };
    }
}
",
@"
class Test
{
    void Method()
    {
        var t1 = new {|Rename:NewClass|}();
    }
}

internal class NewClass
{
    public NewClass()
    {
    }

    public override bool Equals(object obj)
    {
        return obj is NewClass other;
    }

    public override int GetHashCode()
    {
        return 0;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToClass)]
        public async Task OnSingleFieldAnonymousType()
        {
            await TestInRegularAndScriptAsync(@"
class Test
{
    void Method()
    {
        var t1 = [||]new { a = 1 };
    }
}
",
@"
class Test
{
    void Method()
    {
        var t1 = new {|Rename:NewClass|}(1);
    }
}

internal class NewClass
{
    public int A { get; }

    public NewClass(int a)
    {
        A = a;
    }

    public override bool Equals(object obj)
    {
        return obj is NewClass other &&
               A == other.A;
    }

    public override int GetHashCode()
    {
        return -862436692 + A.GetHashCode();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToClass)]
        public async Task ConvertSingleAnonymousTypeWithInferredName()
        {
            var text = @"
class Test
{
    void Method(int b)
    {
        var t1 = [||]new { a = 1, b };
    }
}
";
            var expected = @"
class Test
{
    void Method(int b)
    {
        var t1 = new {|Rename:NewClass|}(1, b);
    }
}

internal class NewClass
{
    public int A { get; }
    public int B { get; }

    public NewClass(int a, int b)
    {
        A = a;
        B = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewClass other &&
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
}";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToClass)]
        public async Task ConvertMultipleInstancesInSameMethod()
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = [||]new { a = 1, b = 2 };
        var t2 = new { a = 3, b = 4 };
    }
}
";
            var expected = @"
class Test
{
    void Method()
    {
        var t1 = new {|Rename:NewClass|}(1, 2);
        var t2 = new NewClass(3, 4);
    }
}

internal class NewClass
{
    public int A { get; }
    public int B { get; }

    public NewClass(int a, int b)
    {
        A = a;
        B = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewClass other &&
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
}";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToClass)]
        public async Task ConvertMultipleInstancesAcrossMethods()
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = [||]new { a = 1, b = 2 };
        var t2 = new { a = 3, b = 4 };
    }

    void Method2()
    {
        var t1 = new { a = 1, b = 2 };
        var t2 = new { a = 3, b = 4 };
    }
}
";
            var expected = @"
class Test
{
    void Method()
    {
        var t1 = new {|Rename:NewClass|}(1, 2);
        var t2 = new NewClass(3, 4);
    }

    void Method2()
    {
        var t1 = new { a = 1, b = 2 };
        var t2 = new { a = 3, b = 4 };
    }
}

internal class NewClass
{
    public int A { get; }
    public int B { get; }

    public NewClass(int a, int b)
    {
        A = a;
        B = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewClass other &&
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
}";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToClass)]
        public async Task OnlyConvertMatchingTypesInSameMethod()
        {
            var text = @"
class Test
{
    void Method(int b)
    {
        var t1 = [||]new { a = 1, b = 2 };
        var t2 = new { a = 3, b };
        var t3 = new { a = 4 };
        var t4 = new { b = 5, a = 6 };
    }
}
";
            var expected = @"
class Test
{
    void Method(int b)
    {
        var t1 = new {|Rename:NewClass|}(1, 2);
        var t2 = new NewClass(3, b);
        var t3 = new { a = 4 };
        var t4 = new { b = 5, a = 6 };
    }
}

internal class NewClass
{
    public int A { get; }
    public int B { get; }

    public NewClass(int a, int b)
    {
        A = a;
        B = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewClass other &&
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
}";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToClass)]
        public async Task TestFixAllMatchesInSingleMethod()
        {
            var text = @"
class Test
{
    void Method(int b)
    {
        var t1 = [||]new { a = 1, b = 2 };
        var t2 = new { a = 3, b };
        var t3 = new { a = 4 };
        var t4 = new { b = 5, a = 6 };
    }
}
";
            var expected = @"
class Test
{
    void Method(int b)
    {
        var t1 = new {|Rename:NewClass|}(1, 2);
        var t2 = new NewClass(3, b);
        var t3 = new { a = 4 };
        var t4 = new { b = 5, a = 6 };
    }
}

internal class NewClass
{
    public int A { get; }
    public int B { get; }

    public NewClass(int a, int b)
    {
        A = a;
        B = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewClass other &&
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
}";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToClass)]
        public async Task TestFixNotAcrossMethods()
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = [||]new { a = 1, b = 2 };
        var t2 = new { a = 3, b = 4 };
    }

    void Method2()
    {
        var t1 = new { a = 1, b = 2 };
        var t2 = new { a = 3, b = 4 };
    }
}
";
            var expected = @"
class Test
{
    void Method()
    {
        var t1 = new {|Rename:NewClass|}(1, 2);
        var t2 = new NewClass(3, 4);
    }

    void Method2()
    {
        var t1 = new { a = 1, b = 2 };
        var t2 = new { a = 3, b = 4 };
    }
}

internal class NewClass
{
    public int A { get; }
    public int B { get; }

    public NewClass(int a, int b)
    {
        A = a;
        B = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewClass other &&
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
}";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToClass)]
        public async Task TestTrivia()
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = /*1*/ [||]new /*2*/ { /*3*/ a /*4*/ = /*5*/ 1 /*7*/ , /*8*/ b /*9*/ = /*10*/ 2 /*11*/ } /*12*/ ;
    }
}
";
            var expected = @"
class Test
{
    void Method()
    {
        var t1 = /*1*/ new {|Rename:NewClass|}( /*3*/ 1 /*7*/ , /*8*/ 2 /*11*/ ) /*12*/ ;
    }
}

internal class NewClass
{
    public int A { get; }
    public int B { get; }

    public NewClass(int a, int b)
    {
        A = a;
        B = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewClass other &&
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
}";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToClass)]
        public async Task TestTrivia2()
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = /*1*/ [||]new /*2*/ { /*3*/ a /*4*/ = /*5*/ 1 /*7*/ , /*8*/ b /*9*/ = /*10*/ 2 /*11*/ } /*12*/ ;
        var t2 = /*1*/ new /*2*/ { /*3*/ a /*4*/ = /*5*/ 1 /*7*/ , /*8*/ b /*9*/ = /*10*/ 2 /*11*/ } /*12*/ ;
    }
}
";
            var expected = @"
class Test
{
    void Method()
    {
        var t1 = /*1*/ new {|Rename:NewClass|}( /*3*/ 1 /*7*/ , /*8*/ 2 /*11*/ ) /*12*/ ;
        var t2 = /*1*/ new NewClass( /*3*/ 1 /*7*/ , /*8*/ 2 /*11*/ ) /*12*/ ;
    }
}

internal class NewClass
{
    public int A { get; }
    public int B { get; }

    public NewClass(int a, int b)
    {
        A = a;
        B = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewClass other &&
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
}";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToClass)]
        public async Task NotIfReferencesAnonymousTypeInternally()
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = [||]new { a = 1, b = new { c = 1, d = 2 } };
    }
}
";

            await TestMissingInRegularAndScriptAsync(text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToClass)]
        public async Task ConvertMultipleNestedInstancesInSameMethod()
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = [||]new { a = 1, b = (object)new { a = 1, b = default(object) } };
    }
}
";
            var expected = @"
using System.Collections.Generic;

class Test
{
    void Method()
    {
        var t1 = new {|Rename:NewClass|}(1, (object)new NewClass(1, default(object)));
    }
}

internal class NewClass
{
    public int A { get; }
    public object B { get; }

    public NewClass(int a, object b)
    {
        A = a;
        B = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewClass other &&
               A == other.A &&
               EqualityComparer<object>.Default.Equals(B, other.B);
    }

    public override int GetHashCode()
    {
        var hashCode = -1817952719;
        hashCode = hashCode * -1521134295 + A.GetHashCode();
        hashCode = hashCode * -1521134295 + EqualityComparer<object>.Default.GetHashCode(B);
        return hashCode;
    }
}";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToClass)]
        public async Task RenameAnnotationOnStartingPoint()
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = new { a = 1, b = 2 };
        var t2 = [||]new { a = 3, b = 4 };
    }
}
";
            var expected = @"
class Test
{
    void Method()
    {
        var t1 = new NewClass(1, 2);
        var t2 = new {|Rename:NewClass|}(3, 4);
    }
}

internal class NewClass
{
    public int A { get; }
    public int B { get; }

    public NewClass(int a, int b)
    {
        A = a;
        B = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewClass other &&
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
}";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToClass)]
        public async Task UpdateReferences()
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = [||]new { a = 1, b = 2 };
        Console.WriteLine(t1.a + t1?.b);
    }
}
";
            var expected = @"
class Test
{
    void Method()
    {
        var t1 = new {|Rename:NewClass|}(1, 2);
        Console.WriteLine(t1.A + t1?.B);
    }
}

internal class NewClass
{
    public int A { get; }
    public int B { get; }

    public NewClass(int a, int b)
    {
        A = a;
        B = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewClass other &&
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
}";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToClass)]
        public async Task CapturedTypeParameters()
        {
            var text = @"
class Test<X> where X : struct
{
    void Method<Y>(List<X> x, Y[] y) where Y : class, new()
    {
        var t1 = [||]new { a = x, b = y };
    }
}
";
            var expected = @"
class Test<X> where X : struct
{
    void Method<Y>(List<X> x, Y[] y) where Y : class, new()
    {
        var t1 = new {|Rename:NewClass|}<X, Y>(x, y);
    }
}

internal class NewClass<X, Y>
    where X : struct
    where Y : class, new()
{
    public List<X> A { get; }
    public Y[] B { get; }

    public NewClass(List<X> a, Y[] b)
    {
        A = a;
        B = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewClass<X, Y> other &&
               System.Collections.Generic.EqualityComparer<List<X>>.Default.Equals(A, other.A) &&
               System.Collections.Generic.EqualityComparer<Y[]>.Default.Equals(B, other.B);
    }

    public override int GetHashCode()
    {
        var hashCode = -1817952719;
        hashCode = hashCode * -1521134295 + System.Collections.Generic.EqualityComparer<List<X>>.Default.GetHashCode(A);
        hashCode = hashCode * -1521134295 + System.Collections.Generic.EqualityComparer<Y[]>.Default.GetHashCode(B);
        return hashCode;
    }
}";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToClass)]
        public async Task NewTypeNameCollision()
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = [||]new { a = 1, b = 2 };
    }
}

class NewClass
{
}
";
            var expected = @"
class Test
{
    void Method()
    {
        var t1 = new {|Rename:NewClass1|}(1, 2);
    }
}

class NewClass
{
}

internal class NewClass1
{
    public int A { get; }
    public int B { get; }

    public NewClass1(int a, int b)
    {
        A = a;
        B = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewClass1 other &&
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
}";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToClass)]
        public async Task TestDuplicatedName()
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = [||]new { a = 1, a = 2 };
    }
}
";
            var expected = @"
class Test
{
    void Method()
    {
        var t1 = new {|Rename:NewClass|}(1, 2);
    }
}

internal class NewClass
{
    public int A { get; }
    public int Item { get; }

    public NewClass(int a, int item)
    {
        A = a;
        Item = item;
    }

    public override bool Equals(object obj)
    {
        return obj is NewClass other &&
               A == other.A &&
               Item == other.Item;
    }

    public override int GetHashCode()
    {
        var hashCode = -335756622;
        hashCode = hashCode * -1521134295 + A.GetHashCode();
        hashCode = hashCode * -1521134295 + Item.GetHashCode();
        return hashCode;
    }
}";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToClass)]
        public async Task TestNewSelection()
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = [|new|] { a = 1, b = 2 };
    }
}
";
            var expected = @"
class Test
{
    void Method()
    {
        var t1 = new {|Rename:NewClass|}(1, 2);
    }
}

internal class NewClass
{
    public int A { get; }
    public int B { get; }

    public NewClass(int a, int b)
    {
        A = a;
        B = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewClass other &&
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
}";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToClass)]
        public async Task TestInLambda1()
        {
            var text = @"
using System;

class Test
{
    void Method()
    {
        var t1 = [||]new { a = 1, b = 2 };
        Action a = () =>
        {
            var t2 = new { a = 3, b = 4 };
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
        var t1 = new {|Rename:NewClass|}(1, 2);
        Action a = () =>
        {
            var t2 = new NewClass(3, 4);
        };
    }
}

internal class NewClass
{
    public int A { get; }
    public int B { get; }

    public NewClass(int a, int b)
    {
        A = a;
        B = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewClass other &&
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
}";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToClass)]
        public async Task TestInLambda2()
        {
            var text = @"
using System;

class Test
{
    void Method()
    {
        var t1 = new { a = 1, b = 2 };
        Action a = () =>
        {
            var t2 = [||]new { a = 3, b = 4 };
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
        var t1 = new NewClass(1, 2);
        Action a = () =>
        {
            var t2 = new {|Rename:NewClass|}(3, 4);
        };
    }
}

internal class NewClass
{
    public int A { get; }
    public int B { get; }

    public NewClass(int a, int b)
    {
        A = a;
        B = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewClass other &&
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
}";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToClass)]
        public async Task TestInLocalFunction1()
        {
            var text = @"
using System;

class Test
{
    void Method()
    {
        var t1 = [||]new { a = 1, b = 2 };
        void Goo()
        {
            var t2 = new { a = 3, b = 4 };
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
        var t1 = new {|Rename:NewClass|}(1, 2);
        void Goo()
        {
            var t2 = new NewClass(3, 4);
        }
    }
}

internal class NewClass
{
    public int A { get; }
    public int B { get; }

    public NewClass(int a, int b)
    {
        A = a;
        B = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewClass other &&
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
}";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToClass)]
        public async Task TestInLocalFunction2()
        {
            var text = @"
using System;

class Test
{
    void Method()
    {
        var t1 = new { a = 1, b = 2 };
        void Goo()
        {
            var t2 = [||]new { a = 3, b = 4 };
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
        var t1 = new NewClass(1, 2);
        void Goo()
        {
            var t2 = new {|Rename:NewClass|}(3, 4);
        }
    }
}

internal class NewClass
{
    public int A { get; }
    public int B { get; }

    public NewClass(int a, int b)
    {
        A = a;
        B = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewClass other &&
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
}";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [WorkItem(35180, "https://github.com/dotnet/roslyn/issues/35180")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToClass)]
        public async Task ConvertSingleAnonymousTypeSelection1()
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = [|new { a = 1, b = 2 }|];
    }
}
";
            var expected = @"
class Test
{
    void Method()
    {
        var t1 = new {|Rename:NewClass|}(1, 2);
    }
}

internal class NewClass
{
    public int A { get; }
    public int B { get; }

    public NewClass(int a, int b)
    {
        A = a;
        B = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewClass other &&
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
}";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [WorkItem(35180, "https://github.com/dotnet/roslyn/issues/35180")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToClass)]
        public async Task ConvertSingleAnonymousTypeSelection2()
        {
            var text = @"
class Test
{
    void Method()
    {
        [|var t1 = new { a = 1, b = 2 };|]
    }
}
";
            var expected = @"
class Test
{
    void Method()
    {
        var t1 = new {|Rename:NewClass|}(1, 2);
    }
}

internal class NewClass
{
    public int A { get; }
    public int B { get; }

    public NewClass(int a, int b)
    {
        A = a;
        B = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewClass other &&
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
}";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [WorkItem(35180, "https://github.com/dotnet/roslyn/issues/35180")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToClass)]
        public async Task ConvertSingleAnonymousTypeSelection3()
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = [|new { a = 1, b = 2 };|]
    }
}
";
            var expected = @"
class Test
{
    void Method()
    {
        var t1 = new {|Rename:NewClass|}(1, 2);
    }
}

internal class NewClass
{
    public int A { get; }
    public int B { get; }

    public NewClass(int a, int b)
    {
        A = a;
        B = b;
    }

    public override bool Equals(object obj)
    {
        return obj is NewClass other &&
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
}";
            await TestInRegularAndScriptAsync(text, expected);
        }
    }
}
