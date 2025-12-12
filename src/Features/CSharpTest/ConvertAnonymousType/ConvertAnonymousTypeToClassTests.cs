// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.ConvertAnonymousType;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertAnonymousType;

[Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToClass)]
public sealed class ConvertAnonymousTypeToClassTests : AbstractCSharpCodeActionTest_NoEditor
{
    private static readonly ParseOptions CSharp8 = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp8);

    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
        => new CSharpConvertAnonymousTypeToClassCodeRefactoringProvider();

    [Fact]
    public Task ConvertSingleAnonymousType()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var t1 = [||]new { a = 1, b = 2 };
                }
            }
            """, """
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
            }
            """, new(options: this.PreferImplicitTypeWithInfo(), parseOptions: CSharp8));

    [Fact]
    public Task ConvertSingleAnonymousType_FileScopedNamespace()
        => TestInRegularAndScriptAsync("""
            namespace N;

            class Test
            {
                void Method()
                {
                    var t1 = [||]new { a = 1, b = 2 };
                }
            }
            """, """
            namespace N;

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
            }
            """, new(options: this.PreferImplicitTypeWithInfo(), parseOptions: CSharp8));

    [Fact]
    public Task ConvertSingleAnonymousType_CSharp9()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var t1 = [||]new { a = 1, b = 2 };
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    var t1 = new {|Rename:NewRecord|}(1, 2);
                }
            }

            internal record NewRecord(int A, int B);

            """, new(options: this.PreferImplicitTypeWithInfo()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39916")]
    public Task ConvertSingleAnonymousType_Explicit()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var t1 = [||]new { a = 1, b = 2 };
                }
            }
            """, """
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
                    int hashCode = -1817952719;
                    hashCode = hashCode * -1521134295 + A.GetHashCode();
                    hashCode = hashCode * -1521134295 + B.GetHashCode();
                    return hashCode;
                }
            }
            """, new(parseOptions: CSharp8));

    [Fact]
    public Task OnEmptyAnonymousType()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var t1 = [||]new { };
                }
            }
            """,
            """
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
            }
            """, new(parseOptions: CSharp8));

    [Fact]
    public Task OnEmptyAnonymousType_CSharp9()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var t1 = [||]new { };
                }
            }
            """,
            """
            class Test
            {
                void Method()
                {
                    var t1 = new {|Rename:NewRecord|}();
                }
            }

            internal record NewRecord();

            """);

    [Fact]
    public Task OnSingleFieldAnonymousType()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var t1 = [||]new { a = 1 };
                }
            }
            """,
            """
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
            }
            """, new(parseOptions: CSharp8));

    [Fact]
    public Task OnSingleFieldAnonymousType_CSharp9()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var t1 = [||]new { a = 1 };
                }
            }
            """,
            """
            class Test
            {
                void Method()
                {
                    var t1 = new {|Rename:NewRecord|}(1);
                }
            }

            internal record NewRecord(int A);

            """);

    [Fact]
    public Task ConvertSingleAnonymousTypeWithInferredName()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method(int b)
                {
                    var t1 = [||]new { a = 1, b };
                }
            }
            """, """
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
            }
            """, new(options: this.PreferImplicitTypeWithInfo(), parseOptions: CSharp8));

    [Fact]
    public Task ConvertSingleAnonymousTypeWithInferredName_CSharp9()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method(int b)
                {
                    var t1 = [||]new { a = 1, b };
                }
            }
            """, """
            class Test
            {
                void Method(int b)
                {
                    var t1 = new {|Rename:NewRecord|}(1, b);
                }
            }

            internal record NewRecord(int A, int B);

            """, new(options: this.PreferImplicitTypeWithInfo()));

    [Fact]
    public Task ConvertMultipleInstancesInSameMethod()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var t1 = [||]new { a = 1, b = 2 };
                    var t2 = new { a = 3, b = 4 };
                }
            }
            """, """
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
            }
            """, new(options: this.PreferImplicitTypeWithInfo(), parseOptions: CSharp8));

    [Fact]
    public Task ConvertMultipleInstancesInSameMethod_CSharp9()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var t1 = [||]new { a = 1, b = 2 };
                    var t2 = new { a = 3, b = 4 };
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    var t1 = new {|Rename:NewRecord|}(1, 2);
                    var t2 = new NewRecord(3, 4);
                }
            }

            internal record NewRecord(int A, int B);

            """, new(options: this.PreferImplicitTypeWithInfo()));

    [Fact]
    public Task ConvertMultipleInstancesAcrossMethods()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            }
            """, new(options: this.PreferImplicitTypeWithInfo(), parseOptions: CSharp8));

    [Fact]
    public Task OnlyConvertMatchingTypesInSameMethod()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            }
            """, new(options: this.PreferImplicitTypeWithInfo(), parseOptions: CSharp8));

    [Fact]
    public Task TestFixAllMatchesInSingleMethod()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            }
            """, new(options: this.PreferImplicitTypeWithInfo(), parseOptions: CSharp8));

    [Fact]
    public Task TestFixNotAcrossMethods()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            }
            """, new(options: this.PreferImplicitTypeWithInfo(), parseOptions: CSharp8));

    [Fact]
    public Task TestTrivia()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var t1 = /*1*/ [||]new /*2*/ { /*3*/ a /*4*/ = /*5*/ 1 /*7*/ , /*8*/ b /*9*/ = /*10*/ 2 /*11*/ } /*12*/ ;
                }
            }
            """, """
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
            }
            """, new(options: this.PreferImplicitTypeWithInfo(), parseOptions: CSharp8));

    [Fact]
    public Task TestTrivia2()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var t1 = /*1*/ [||]new /*2*/ { /*3*/ a /*4*/ = /*5*/ 1 /*7*/ , /*8*/ b /*9*/ = /*10*/ 2 /*11*/ } /*12*/ ;
                    var t2 = /*1*/ new /*2*/ { /*3*/ a /*4*/ = /*5*/ 1 /*7*/ , /*8*/ b /*9*/ = /*10*/ 2 /*11*/ } /*12*/ ;
                }
            }
            """, """
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
            }
            """, new(options: this.PreferImplicitTypeWithInfo(), parseOptions: CSharp8));

    [Fact]
    public Task NotIfReferencesAnonymousTypeInternally()
        => TestMissingInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var t1 = [||]new { a = 1, b = new { c = 1, d = 2 } };
                }
            }
            """);

    [Fact]
    public Task ConvertMultipleNestedInstancesInSameMethod()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var t1 = [||]new { a = 1, b = (object)new { a = 1, b = default(object) } };
                }
            }
            """, """
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
            }
            """, new(options: this.PreferImplicitTypeWithInfo(), parseOptions: CSharp8));

    [Fact]
    public Task RenameAnnotationOnStartingPoint()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var t1 = new { a = 1, b = 2 };
                    var t2 = [||]new { a = 3, b = 4 };
                }
            }
            """, """
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
            }
            """, new(options: this.PreferImplicitTypeWithInfo(), parseOptions: CSharp8));

    [Fact]
    public Task UpdateReferences()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var t1 = [||]new { a = 1, b = 2 };
                    Console.WriteLine(t1.a + t1?.b);
                }
            }
            """, """
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
            }
            """, new(options: this.PreferImplicitTypeWithInfo(), parseOptions: CSharp8));

    [Fact]
    public Task CapturedTypeParameters()
        => TestInRegularAndScriptAsync("""
            class Test<X> where X : struct
            {
                void Method<Y>(List<X> x, Y[] y) where Y : class, new()
                {
                    var t1 = [||]new { a = x, b = y };
                }
            }
            """, """
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
            }
            """, new(options: this.PreferImplicitTypeWithInfo(), parseOptions: CSharp8));

    [Fact]
    public Task CapturedTypeParameters_CSharp9()
        => TestInRegularAndScriptAsync("""
            class Test<X> where X : struct
            {
                void Method<Y>(List<X> x, Y[] y) where Y : class, new()
                {
                    var t1 = [||]new { a = x, b = y };
                }
            }
            """, """
            class Test<X> where X : struct
            {
                void Method<Y>(List<X> x, Y[] y) where Y : class, new()
                {
                    var t1 = new {|Rename:NewRecord|}<X, Y>(x, y);
                }
            }

            internal record NewRecord<X, Y>(List<X> A, Y[] B)
                where X : struct
                where Y : class, new();

            """, new(options: this.PreferImplicitTypeWithInfo()));

    [Fact]
    public Task NewTypeNameCollision()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            }
            """, new(options: this.PreferImplicitTypeWithInfo(), parseOptions: CSharp8));

    [Fact]
    public Task TestDuplicatedName()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var t1 = [||]new { a = 1, a = 2 };
                }
            }
            """, """
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
            }
            """, new(options: this.PreferImplicitTypeWithInfo(), parseOptions: CSharp8));

    [Fact]
    public Task TestDuplicatedName_CSharp9()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var t1 = [||]new { a = 1, a = 2 };
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    var t1 = new {|Rename:NewRecord|}(1, 2);
                }
            }

            internal record NewRecord(int A, int Item);

            """, new(options: this.PreferImplicitTypeWithInfo()));

    [Fact]
    public Task TestNewSelection()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var t1 = [|new|] { a = 1, b = 2 };
                }
            }
            """, """
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
            }
            """, new(options: this.PreferImplicitTypeWithInfo(), parseOptions: CSharp8));

    [Fact]
    public Task TestInLambda1()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            }
            """, new(options: this.PreferImplicitTypeWithInfo(), parseOptions: CSharp8));

    [Fact]
    public Task TestInLambda2()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            }
            """, new(options: this.PreferImplicitTypeWithInfo(), parseOptions: CSharp8));

    [Fact]
    public Task TestInLocalFunction1()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            }
            """, new(options: this.PreferImplicitTypeWithInfo(), parseOptions: CSharp8));

    [Fact]
    public Task TestInLocalFunction2()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            }
            """, new(options: this.PreferImplicitTypeWithInfo(), parseOptions: CSharp8));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public Task ConvertSingleAnonymousTypeSelection1()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var t1 = [|new { a = 1, b = 2 }|];
                }
            }
            """, """
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
            }
            """, new(options: this.PreferImplicitTypeWithInfo(), parseOptions: CSharp8));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public Task ConvertSingleAnonymousTypeSelection2()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    [|var t1 = new { a = 1, b = 2 };|]
                }
            }
            """, """
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
            }
            """, new(options: this.PreferImplicitTypeWithInfo(), parseOptions: CSharp8));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public Task ConvertSingleAnonymousTypeSelection3()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var t1 = [|new { a = 1, b = 2 };|]
                }
            }
            """, """
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
            }
            """, new(options: this.PreferImplicitTypeWithInfo(), parseOptions: CSharp8));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/45747")]
    public Task ConvertOmittingTrailingComma()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var t1 = [||]new
                    {
                        a = 1,
                        b = 2,
                    };
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    var t1 = new {|Rename:NewClass|}(
            1,
            2
                    );
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
            }
            """, new(options: this.PreferImplicitTypeWithInfo(), parseOptions: CSharp8));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/45747")]
    public Task ConvertOmittingTrailingCommaButPreservingTrivia()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var t1 = [||]new
                    {
                        a = 1,
                        b = 2 // and
                              // more
                        ,
                    };
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    var t1 = new {|Rename:NewClass|}(
            1,
            2 // and
              // more

                    );
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
            }
            """, new(options: this.PreferImplicitTypeWithInfo(), parseOptions: CSharp8));
}
