// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.FullyQualify;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.FullyQualify
{
    public class FullyQualifyTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpFullyQualifyCodeFixProvider());

        protected override ImmutableArray<CodeAction> MassageActions(ImmutableArray<CodeAction> actions)
            => FlattenActions(actions);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task TestTypeFromMultipleNamespaces1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    [|IDictionary|] Method()
    {
        Foo();
    }
}",
@"class Class
{
    System.Collections.IDictionary Method()
    {
        Foo();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task TestTypeFromMultipleNamespaces2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    [|IDictionary|] Method()
    {
        Foo();
    }
}",
@"class Class
{
    System.Collections.Generic.IDictionary Method()
    {
        Foo();
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task TestGenericWithNoArgs()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    [|List|] Method()
    {
        Foo();
    }
}",
@"class Class
{
    System.Collections.Generic.List Method()
    {
        Foo();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task TestGenericWithCorrectArgs()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    [|List<int>|] Method()
    {
        Foo();
    }
}",
@"class Class
{
    System.Collections.Generic.List<int> Method()
    {
        Foo();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task TestSmartTagDisplayText()
        {
            await TestSmartTagTextAsync(
@"class Class
{
    [|List<int>|] Method()
    {
        Foo();
    }
}",
"System.Collections.Generic.List");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task TestGenericWithWrongArgs()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    [|List<int, string>|] Method()
    {
        Foo();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task TestNotOnVar1()
        {
            await TestMissingInRegularAndScriptAsync(
@"namespace N
{
    class var { }
}

class C
{
    void M()
    {
        [|var|]
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task TestNotOnVar2()
        {
            await TestMissingInRegularAndScriptAsync(
@"namespace N
{
    class Bar { }
}

class C
{
    void M()
    {
        [|var|]
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task TestGenericInLocalDeclaration()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Foo()
    {
        [|List<int>|] a = new List<int>();
    }
}",
@"class Class
{
    void Foo()
    {
        System.Collections.Generic.List<int> a = new List<int>();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task TestGenericItemType()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Class
{
    List<[|Int32|]> l;
}",
@"using System.Collections.Generic;

class Class
{
    List<System.Int32> l;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task TestGenerateWithExistingUsings()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class Class
{
    [|List<int>|] Method()
    {
        Foo();
    }
}",
@"using System;

class Class
{
    System.Collections.Generic.List<int> Method()
    {
        Foo();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task TestGenerateInNamespace()
        {
            await TestInRegularAndScriptAsync(
@"namespace N
{
    class Class
    {
        [|List<int>|] Method()
        {
            Foo();
        }
    }
}",
@"namespace N
{
    class Class
    {
        System.Collections.Generic.List<int> Method()
        {
            Foo();
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task TestGenerateInNamespaceWithUsings()
        {
            await TestInRegularAndScriptAsync(
@"namespace N
{
    using System;

    class Class
    {
        [|List<int>|] Method()
        {
            Foo();
        }
    }
}",
@"namespace N
{
    using System;

    class Class
    {
        System.Collections.Generic.List<int> Method()
        {
            Foo();
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task TestExistingUsing()
        {
            await TestActionCountAsync(
@"using System.Collections.Generic;

class Class
{
    [|IDictionary|] Method()
    {
        Foo();
    }
}",
count: 1);

            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Class
{
    [|IDictionary|] Method()
    {
        Foo();
    }
}",
@"using System.Collections.Generic;

class Class
{
    System.Collections.IDictionary Method()
    {
        Foo();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task TestMissingIfUniquelyBound()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Class
{
    [|String|] Method()
    {
        Foo();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task TestMissingIfUniquelyBoundGeneric()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Class
{
    [|List<int>|] Method()
    {
        Foo();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task TestOnEnum()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Foo()
    {
        var a = [|Colors|].Red;
    }
}

namespace A
{
    enum Colors
    {
        Red,
        Green,
        Blue
    }
}",
@"class Class
{
    void Foo()
    {
        var a = A.Colors.Red;
    }
}

namespace A
{
    enum Colors
    {
        Red,
        Green,
        Blue
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task TestOnClassInheritance()
        {
            await TestInRegularAndScriptAsync(
@"class Class : [|Class2|]
{
}

namespace A
{
    class Class2
    {
    }
}",
@"class Class : A.Class2
{
}

namespace A
{
    class Class2
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task TestOnImplementedInterface()
        {
            await TestInRegularAndScriptAsync(
@"class Class : [|IFoo|]
{
}

namespace A
{
    interface IFoo
    {
    }
}",
@"class Class : A.IFoo
{
}

namespace A
{
    interface IFoo
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task TestAllInBaseList()
        {
            await TestInRegularAndScriptAsync(
@"class Class : [|IFoo|], Class2
{
}

namespace A
{
    class Class2
    {
    }
}

namespace B
{
    interface IFoo
    {
    }
}",
@"class Class : B.IFoo, Class2
{
}

namespace A
{
    class Class2
    {
    }
}

namespace B
{
    interface IFoo
    {
    }
}");

            await TestInRegularAndScriptAsync(
@"class Class : B.IFoo, [|Class2|]
{
}

namespace A
{
    class Class2
    {
    }
}

namespace B
{
    interface IFoo
    {
    }
}",
@"class Class : B.IFoo, A.Class2
{
}

namespace A
{
    class Class2
    {
    }
}

namespace B
{
    interface IFoo
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task TestAttributeUnexpanded()
        {
            await TestInRegularAndScriptAsync(
@"[[|Obsolete|]]
class Class
{
}",
@"[System.Obsolete]
class Class
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task TestAttributeExpanded()
        {
            await TestInRegularAndScriptAsync(
@"[[|ObsoleteAttribute|]]
class Class
{
}",
@"[System.ObsoleteAttribute]
class Class
{
}");
        }

        [WorkItem(527360, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527360")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task TestExtensionMethods()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Foo
{
    void Bar()
    {
        var values = new List<int>() { 1, 2, 3 };
        values.[|Where|](i => i > 1);
    }
}");
        }

        [WorkItem(538018, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538018")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task TestAfterNew()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Foo()
    {
        List<int> l;
        l = new [|List<int>|]();
    }
}",
@"class Class
{
    void Foo()
    {
        List<int> l;
        l = new System.Collections.Generic.List<int>();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task TestArgumentsInMethodCall()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Test()
    {
        Console.WriteLine([|DateTime|].Today);
    }
}",
@"class Class
{
    void Test()
    {
        Console.WriteLine(System.DateTime.Today);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task TestCallSiteArgs()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Test([|DateTime|] dt)
    {
    }
}",
@"class Class
{
    void Test(System.DateTime dt)
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task TestUsePartialClass()
        {
            await TestInRegularAndScriptAsync(
@"namespace A
{
    public class Class
    {
        [|PClass|] c;
    }
}

namespace B
{
    public partial class PClass
    {
    }
}",
@"namespace A
{
    public class Class
    {
        B.PClass c;
    }
}

namespace B
{
    public partial class PClass
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task TestGenericClassInNestedNamespace()
        {
            await TestInRegularAndScriptAsync(
@"namespace A
{
    namespace B
    {
        class GenericClass<T>
        {
        }
    }
}

namespace C
{
    class Class
    {
        [|GenericClass<int>|] c;
    }
}",
@"namespace A
{
    namespace B
    {
        class GenericClass<T>
        {
        }
    }
}

namespace C
{
    class Class
    {
        A.B.GenericClass<int> c;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task TestBeforeStaticMethod()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Test()
    {
        [|Math|].Sqrt();
    }",
@"class Class
{
    void Test()
    {
        System.Math.Sqrt();
    }");
        }

        [WorkItem(538136, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538136")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task TestBeforeNamespace()
        {
            await TestInRegularAndScriptAsync(
@"namespace A
{
    class Class
    {
        [|C|].Test t;
    }
}

namespace B
{
    namespace C
    {
        class Test
        {
        }
    }
}",
@"namespace A
{
    class Class
    {
        B.C.Test t;
    }
}

namespace B
{
    namespace C
    {
        class Test
        {
        }
    }
}");
        }

        [WorkItem(527395, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527395")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task TestSimpleNameWithLeadingTrivia()
        {
            await TestInRegularAndScriptAsync(
@"class Class { void Test() { /*foo*/[|Int32|] i; } }",
@"class Class { void Test() { /*foo*/System.Int32 i; } }",
ignoreTrivia: false);
        }

        [WorkItem(527395, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527395")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task TestGenericNameWithLeadingTrivia()
        {
            await TestInRegularAndScriptAsync(
@"class Class { void Test() { /*foo*/[|List<int>|] l; } }",
@"class Class { void Test() { /*foo*/System.Collections.Generic.List<int> l; } }",
ignoreTrivia: false);
        }

        [WorkItem(538740, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538740")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task TestFullyQualifyTypeName()
        {
            await TestInRegularAndScriptAsync(
@"public class Program
{
    public class Inner
    {
    }
}

class Test
{
    [|Inner|] i;
}",
@"public class Program
{
    public class Inner
    {
    }
}

class Test
{
    Program.Inner i;
}");
        }

        [WorkItem(538740, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538740")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task TestFullyQualifyTypeName_NotForGenericType()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Program<T>
{
    public class Inner
    {
    }
}

class Test
{
    [|Inner|] i;
}");
        }

        [WorkItem(538764, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538764")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task TestFullyQualifyThroughAlias()
        {
            await TestInRegularAndScriptAsync(
@"using Alias = System;

class C
{
    [|Int32|] i;
}",
@"using Alias = System;

class C
{
    Alias.Int32 i;
}");
        }

        [WorkItem(538763, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538763")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task TestFullyQualifyPrioritizeTypesOverNamespaces1()
        {
            await TestInRegularAndScriptAsync(
@"namespace Outer
{
    namespace C
    {
        class C
        {
        }
    }
}

class Test
{
    [|C|] c;
}",
@"namespace Outer
{
    namespace C
    {
        class C
        {
        }
    }
}

class Test
{
    Outer.C.C c;
}");
        }

        [WorkItem(538763, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538763")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task TestFullyQualifyPrioritizeTypesOverNamespaces2()
        {
            await TestInRegularAndScriptAsync(
@"namespace Outer
{
    namespace C
    {
        class C
        {
        }
    }
}

class Test
{
    [|C|] c;
}",
@"namespace Outer
{
    namespace C
    {
        class C
        {
        }
    }
}

class Test
{
    Outer.C c;
}",
index: 1);
        }

        [WorkItem(539853, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539853")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task BugFix5950()
        {
            await TestAsync(
@"using System.Console; WriteLine([|Expression|].Constant(123));",
@"using System.Console; WriteLine(System.Linq.Expressions.Expression.Constant(123));",
parseOptions: GetScriptOptions());
        }

        [WorkItem(540318, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540318")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task TestAfterAlias()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        System::[|Console|] :: WriteLine(""TEST"");
    }
}");
        }

        [WorkItem(540942, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540942")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task TestMissingOnIncompleteStatement()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;
using System.IO;

class C
{
    static void Main(string[] args)
    {
        [|Path|] }
}");
        }

        [WorkItem(542643, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542643")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task TestAssemblyAttribute()
        {
            await TestInRegularAndScriptAsync(
@"[assembly: [|InternalsVisibleTo|](""Project"")]",
@"[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""Project"")]");
        }

        [WorkItem(543388, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543388")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task TestMissingOnAliasName()
        {
            await TestMissingInRegularAndScriptAsync(
@"using [|GIBBERISH|] = Foo.GIBBERISH;

class Program
{
    static void Main(string[] args)
    {
        GIBBERISH x;
    }
}

namespace Foo
{
    public class GIBBERISH
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task TestMissingOnAttributeOverloadResolutionError()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System.Runtime.InteropServices;

class M
{
    [[|DllImport|]()]
    static extern int? My();
}");
        }

        [WorkItem(544950, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544950")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task TestNotOnAbstractConstructor()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System.IO;

class Program
{
    static void Main(string[] args)
    {
        var s = new [|Stream|]();
    }
}");
        }

        [WorkItem(545774, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545774")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task TestAttribute()
        {
            var input = @"[ assembly : [|Guid|] ( ""9ed54f84-a89d-4fcd-a854-44251e925f09"" ) ] ";
            await TestActionCountAsync(input, 1);

            await TestInRegularAndScriptAsync(
input,
@"[assembly: System.Runtime.InteropServices.Guid(""9ed54f84-a89d-4fcd-a854-44251e925f09"")]");
        }

        [WorkItem(546027, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546027")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task TestGeneratePropertyFromAttribute()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

[AttributeUsage(AttributeTargets.Class)]
class MyAttrAttribute : Attribute
{
}

[MyAttr(123, [|Version|] = 1)]
class D
{
}");
        }

        [WorkItem(775448, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/775448")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task ShouldTriggerOnCS0308()
        {
            // CS0308: The non-generic type 'A' cannot be used with type arguments
            await TestInRegularAndScriptAsync(
@"using System.Collections;

class Test
{
    static void Main(string[] args)
    {
        [|IEnumerable<int>|] f;
    }
}",
@"using System.Collections;

class Test
{
    static void Main(string[] args)
    {
        System.Collections.Generic.IEnumerable<int> f;
    }
}");
        }

        [WorkItem(947579, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/947579")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task AmbiguousTypeFix()
        {
            await TestInRegularAndScriptAsync(
@"using n1;
using n2;

class B
{
    void M1()
    {
        [|var a = new A();|]
    }
}

namespace n1
{
    class A
    {
    }
}

namespace n2
{
    class A
    {
    }
}",
@"using n1;
using n2;

class B
{
    void M1()
    {
        var a = new n1.A();
    }
}

namespace n1
{
    class A
    {
    }
}

namespace n2
{
    class A
    {
    }
}");
        }

        [WorkItem(995857, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/995857")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task NonPublicNamespaces()
        {
            await TestInRegularAndScriptAsync(
@"namespace MS.Internal.Xaml
{
    private class A
    {
    }
}

namespace System.Xaml
{
    public class A
    {
    }
}

public class Program
{
    static void M()
    {
        [|Xaml|]
    }
}",
@"namespace MS.Internal.Xaml
{
    private class A
    {
    }
}

namespace System.Xaml
{
    public class A
    {
    }
}

public class Program
{
    static void M()
    {
        System.Xaml
    }
}");

            await TestInRegularAndScriptAsync(
@"namespace MS.Internal.Xaml
{
    public class A
    {
    }
}

namespace System.Xaml
{
    public class A
    {
    }
}

public class Program
{
    static void M()
    {
        [|Xaml|]
    }
}",
@"namespace MS.Internal.Xaml
{
    public class A
    {
    }
}

namespace System.Xaml
{
    public class A
    {
    }
}

public class Program
{
    static void M()
    {
        MS.Internal.Xaml
    }
}", index: 1);
        }

        [WorkItem(11071, "https://github.com/dotnet/roslyn/issues/11071")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task AmbiguousFixOrdering()
        {
            await TestInRegularAndScriptAsync(
@"using n1;
using n2;

[[|Inner|].C]
class B
{
}

namespace n1
{
    namespace Inner
    {
    }
}

namespace n2
{
    namespace Inner
    {
        class CAttribute
        {
        }
    }
}",
@"using n1;
using n2;

[n2.Inner.C]
class B
{
}

namespace n1
{
    namespace Inner
    {
    }
}

namespace n2
{
    namespace Inner
    {
        class CAttribute
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task TupleTest()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    ([|IDictionary|], string) Method()
    {
        Foo();
    }
}",
@"class Class
{
    (System.Collections.IDictionary, string) Method()
    {
        Foo();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task TupleWithOneName()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    ([|IDictionary|] a, string) Method()
    {
        Foo();
    }
}",
@"class Class
{
    (System.Collections.IDictionary a, string) Method()
    {
        Foo();
    }
}");
        }

        [WorkItem(18275, "https://github.com/dotnet/roslyn/issues/18275")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
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

        [WorkItem(18623, "https://github.com/dotnet/roslyn/issues/18623")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public async Task TestDoNotQualifyToTheSameTypeToFixWrongArity()
        {
            await TestMissingInRegularAndScriptAsync(
@"
using System.Collections.Generic;

class Program : [|IReadOnlyCollection|]
{
}");
        }
    }
}