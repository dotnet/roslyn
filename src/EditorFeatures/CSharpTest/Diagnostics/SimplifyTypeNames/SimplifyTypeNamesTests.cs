// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.SimplifyTypeNames;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.SimplifyTypeNames;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.SimplifyTypeNames
{
    public partial class SimplifyTypeNamesTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return new Tuple<DiagnosticAnalyzer, CodeFixProvider>(
                new CSharpSimplifyTypeNamesDiagnosticAnalyzer(), new SimplifyTypeNamesCodeFixProvider());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void SimplifyGenericName()
        {
            Test(
@"using System;
class C
{
    static T Foo<T>(T x, T y) { return default(T); }

    static void M()
    {
        var c = [|Foo<int>|](1, 1);
    }
}",
@"using System;
class C
{
    static T Foo<T>(T x, T y) { return default(T); }

    static void M()
    {
        var c = Foo(1, 1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void UseAlias0()
        {
            Test(
@"using Foo = System;
namespace Root 
{
    class A 
    {
    }

    class B
    {
    public [|Foo::Int32|] a;
    }
}",
@"using Foo = System;
namespace Root 
{
    class A 
    {
    }

    class B
    {
    public int a;
    }
}", index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void UseAlias00()
        {
            Test(
@"namespace Root 
{
    using MyType = System.IO.File;

    class A 
    {
        [|System.IO.File|] c;
    }
}",
@"namespace Root 
{
    using MyType = System.IO.File;

    class A 
    {
        MyType c;
    }
}", index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void UseAlias()
        {
            var source =
@"using MyType = System.Exception;

class A 
{
    [|System.Exception|] c;
}";

            Test(source,
@"using MyType = System.Exception;

class A 
{
    MyType c;
}", index: 0);

            TestActionCount(source, 1);
            TestSpans(source,
@"using MyType = System.Exception;

class A 
{
    [|System.Exception|] c;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void UseAlias1()
        {
            Test(
@"namespace Root 
{
    using MyType = System.Exception;

    class A 
    {
        [|System.Exception|] c;
    }
}",
@"namespace Root 
{
    using MyType = System.Exception;

    class A 
    {
        MyType c;
    }
}", index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void UseAlias2()
        {
            Test(
@"using MyType = System.Exception;

namespace Root 
{
    class A 
    {
        [|System.Exception|] c;
    }
}", @"
using MyType = System.Exception;

namespace Root 
{
    class A 
    {
        MyType c;
    }
}", index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void UseAlias3()
        {
            Test(
@"using MyType = System.Exception;

namespace Root 
{
    namespace Nested
    {
        class A 
        {
            [|System.Exception|] c;
        }
    }
}", @"
using MyType = System.Exception;

namespace Root 
{
    namespace Nested
    {
        class A 
        {
            MyType c;
        }
    }
}", index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void UseAlias4()
        {
            Test(
@"using MyType = System.Exception;

class A 
{
    [|System.Exception|] c;
}",
@"using MyType = System.Exception;

class A 
{
    MyType c;
}", index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void UseAlias5()
        {
            Test(
@"namespace Root 
{
    using MyType = System.Exception;

    class A 
    {
        [|System.Exception|] c;
    }
}", @"
namespace Root 
{
    using MyType = System.Exception;

    class A 
    {
        MyType c;
    }
}", index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void UseAlias6()
        {
            Test(
@"using MyType = System.Exception;

namespace Root 
{
    class A 
    {
        [|System.Exception|] c;
    }
}", @"
using MyType = System.Exception;

namespace Root 
{
    class A 
    {
        MyType c;
    }
}", index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void UseAlias7()
        {
            Test(
@"using MyType = System.Exception;

namespace Root 
{
    namespace Nested
    {
        class A 
        {
            [|System.Exception|] c;
        }
    }
}", @"
using MyType = System.Exception;

namespace Root 
{
    namespace Nested
    {
        class A 
        {
            MyType c;
        }
    }
}", index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void UseAlias8()
        {
            Test(
@"
using Foo = System.Int32;

namespace Root 
{
    namespace Nested
    {
        class A 
        {
            var c = [|System.Int32|].MaxValue;
        }
    }
}", @"
using Foo = System.Int32;

namespace Root 
{
    namespace Nested
    {
        class A 
        {
            var c = Foo.MaxValue;
        }
    }
}", index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TwoAliases()
        {
            Test(
@"using MyType1 = System.Exception;

namespace Root 
{
    using MyType2 = Exception;

    class A 
    {
        [|System.Exception|] c;
    }
}", @"
using MyType1 = System.Exception;

namespace Root 
{
    using MyType2 = Exception;

    class A 
    {
        MyType1 c;
    }
}", index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TwoAliases2()
        {
            Test(
@"using MyType1 = System.Exception;

namespace Root 
{
    using MyType2 = [|System.Exception|];

    class A 
    {
        System.Exception c;
    }
}", @"
using MyType1 = System.Exception;

namespace Root 
{
    using MyType2 = MyType1;

    class A 
    {
        System.Exception c;
    }
}", index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TwoAliasesConflict()
        {
            TestMissing(
@"using MyType = System.Exception;

namespace Root 
{
    using MyType = Exception;

    class A 
    {
        [|System.Exception|] c;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TwoAliasesConflict2()
        {
            Test(
@"using MyType = System.Exception;

namespace Root 
{
    using MyType = [|System.Exception|];

    class A 
    {
        System.Exception c;
    }
}", @"
using MyType = System.Exception;

namespace Root 
{
    using MyType = MyType;

    class A 
    {
        System.Exception c;
    }
}", index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void AliasInSiblingNamespace()
        {
            var content =
@"[|namespace Root 
{
    namespace Sibling
    {
        using MyType = System.Exception;
    }

    class A 
    {
        System.Exception c;
    }
}|]";
            TestMissing(content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void KeywordInt32()
        {
            var source =
@"class A
{
    [|System.Int32|] i;
}";
            Test(source,
@"
class A
{
    int i;
}", index: 0);
            TestActionCount(source, 1);
            TestSpans(source,
@"class A
{
    [|System.Int32|] i;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void Keywords()
        {
            var builtInTypeMap = new Dictionary<string, string>()
            {
                { "System.Boolean", "bool" },
                { "System.SByte", "sbyte" },
                { "System.Byte", "byte" },
                { "System.Decimal", "decimal" },
                { "System.Single", "float" },
                { "System.Double", "double" },
                { "System.Int16", "short" },
                { "System.Int32", "int" },
                { "System.Int64", "long" },
                { "System.Char", "char" },
                { "System.String", "string" },
                { "System.UInt16", "ushort" },
                { "System.UInt32", "uint" },
                { "System.UInt64", "ulong" }
            };

            var content =
@"class A
{
     [|[||]|] i;
}
";

            foreach (var pair in builtInTypeMap)
            {
                int position = content.IndexOf(@"[||]", StringComparison.Ordinal);
                var newContent = content.Replace(@"[||]", pair.Key);
                var expected = content.Replace(@"[||]", pair.Value);
                Test(newContent, expected, index: 0);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void SimplifyTypeName()
        {
            var content =
@"namespace Root 
{
    class A 
    {
        [|System.Exception|] c;
    }
}";
            TestMissing(content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void SimplifyTypeName1()
        {
            var source =
@"using System;

namespace Root 
{
    class A 
    {
        [|System.Exception|] c;
    }
}";

            Test(source, @"
using System;

namespace Root 
{
    class A 
    {
        Exception c;
    }
}", index: 0);
            TestActionCount(source, 1);
            TestSpans(source,
@"using System;

namespace Root 
{
    class A 
    {
        [|System|].Exception c;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void SimplifyTypeName2()
        {
            Test(
@"namespace System
{
    class A 
    {
        [|System.Exception|] c;
    }
}", @"
namespace System
{
    class A 
    {
        Exception c;
    }
}", index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void SimplifyTypeName3()
        {
            Test(
@"namespace N1
{
    public class A1 { }

    namespace N2
    {
        public class A2
        {
            [|N1.A1|] a;
        }
    }
}", @"
namespace N1
{
    public class A1 { }

    namespace N2
    {
        public class A2
        {
            A1 a;
        }
    }
}", index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void SimplifyTypeName4()
        {
            // this is failing since we can't speculatively bind namespace yet
            Test(
@"namespace N1
{
    namespace N2
    {
        public class A1 { }
    }

    public class A2
    {
        [|N1.N2.A1|] a;
    }
}", @"
namespace N1
{
    namespace N2
    {
        public class A1 { }
    }

    public class A2
    {
        N2.A1 a;
    }
}", index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void SimplifyTypeName5()
        {
            Test(
@"namespace N1
{
    class NC1
    {
        public class A1 { }
    }

    public class A2
    {
        [|N1.NC1.A1|] a;
    }
}", @"
namespace N1
{
    class NC1
    {
        public class A1 { }
    }

    public class A2
    {
        NC1.A1 a;
    }
}", index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void SimplifyTypeName6()
        {
            var content =
@"namespace N1
{
    public class A1 { }

    namespace N2
    {
        public class A1 { }

        public class A2
        {
            [|N1.A1|] a;
        }
    }
}
";
            TestMissing(content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void SimplifyTypeName7()
        {
            var source =
@"namespace N1
{
    namespace N2
    {
        public class A2
        {
            public class A1 { }

            [|N1.N2|].A2.A1 a;
        }
    }
}";

            Test(source, @"
namespace N1
{
    namespace N2
    {
        public class A2
        {
            public class A1 { }

            A1 a;
        }
    }
}", index: 0);

            TestActionCount(source, 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void SimplifyGenericTypeName1()
        {
            var content =
@"namespace N1
{
    public class A1
    {
        [|System.EventHandler<System.EventArgs>|] a;
    }
}
";
            TestMissing(content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void SimplifyGenericTypeName2()
        {
            var source =
@"using System;

namespace N1
{
    public class A1
    {
        [|System.EventHandler<System.EventArgs>|] a;
    }
}";

            Test(source, @"
using System;

namespace N1
{
    public class A1
    {
        EventHandler<EventArgs> a;
    }
}", index: 0);

            TestActionCount(source, 1);
        }

        [Fact(Skip = "1033012"), Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public void SimplifyGenericTypeName3()
        {
            var fixAllActionId = SimplifyTypeNamesCodeFixProvider.GetCodeActionId(IDEDiagnosticIds.SimplifyMemberAccessDiagnosticId, "System.Action");
            Test(
@"using System;

namespace N1
{
    public class A1
    {
        {|FixAllInDocument:System.Action|}<System.Action<System.Action<System.EventArgs>, System.Action<System.Action<System.EventArgs, System.Action<System.EventArgs>, System.Action<System.Action<System.Action<System.Action<System.EventArgs>, System.Action<System.EventArgs>>>>>>>> a;
    }
}", @"
using System;

namespace N1
{
    public class A1
    {
        Action<Action<Action<EventArgs>, Action<Action<EventArgs, Action<EventArgs>, Action<Action<Action<Action<EventArgs>, Action<EventArgs>>>>>>>> a;
    }
}", fixAllActionEquivalenceKey: fixAllActionId);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void SimplifyGenericTypeName4()
        {
            var content =
@"using MyHandler = System.EventHandler;

namespace N1
{
    public class A1
    {
        [|System.EventHandler<System.EventHandler<System.EventArgs>>|] a;
    }
}
";
            TestMissing(content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void SimplifyGenericTypeName5()
        {
            var source =
@"using MyHandler = System.EventHandler<System.EventArgs>;

namespace N1
{
    public class A1
    {
        System.EventHandler<[|System.EventHandler<System.EventArgs>|]> a;
    }
}";

            Test(source, @"
using MyHandler = System.EventHandler<System.EventArgs>;

namespace N1
{
    public class A1
    {
        System.EventHandler<MyHandler> a;
    }
}", index: 0);
            TestActionCount(source, 1);
            TestSpans(source,
@"using MyHandler = System.EventHandler<System.EventArgs>;

namespace N1
{
    public class A1
    {
        System.EventHandler<[|System.EventHandler<System.EventArgs>|]> a;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void SimplifyGenericTypeName6()
        {
            Test(
@"using System;

namespace N1
{
    using MyType = N2.A1<Exception>;

    namespace N2
    {
        public class A1<T>
        {
        }
    }

    class Test
    {
        [|N1.N2.A1<System.Exception>|] a;
    }
}", @"
using System;

namespace N1
{
    using MyType = N2.A1<Exception>;

    namespace N2
    {
        public class A1<T>
        {
        }
    }

    class Test
    {
        MyType a;
    }
}", index: 0);
        }

        [Fact(Skip = "1033012"), Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public void SimplifyGenericTypeName7()
        {
            Test(
@"using System;

namespace N1
{
    using MyType = Exception;

    namespace N2
    {
        public class A1<T>
        {
        }
    }

    class Test
    {
        N1.N2.A1<[|System.Exception|]> a;
    }
}", @"
using System;

namespace N1
{
    using MyType = Exception;

    namespace N2
    {
        public class A1<T>
        {
        }
    }

    class Test
    {
        N2.A1<MyType> a;
    }
}", index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void Array1()
        {
            Test(
@"using System.Collections.Generic;

namespace N1
{
    class Test
    {
        [|System.Collections.Generic.List<System.String[]>|] a;
    }
}", @"
using System.Collections.Generic;

namespace N1
{
    class Test
    {
        List<string[]> a;
    }
}", index: 0);

            // TODO: The below test is currently disabled due to restrictions of the test framework, this needs to be fixed.

            ////            Test(
            ////    @"using System.Collections.Generic;

            ////namespace N1
            ////{
            ////    class Test
            ////    {
            ////        System.Collections.Generic.List<[|System.String|][]> a;
            ////    }
            ////}", @"
            ////using System.Collections.Generic;

            ////namespace N1
            ////{
            ////    class Test
            ////    {
            ////        System.Collections.Generic.List<string[]> a;
            ////    }
            ////}", index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void Array2()
        {
            Test(
@"using System.Collections.Generic;

namespace N1
{
    class Test
    {
        [|System.Collections.Generic.List<System.String[][,][,,,]>|] a;
    }
}", @"
using System.Collections.Generic;

namespace N1
{
    class Test
    {
        List<string[][,][,,,]> a;
    }
}", index: 0);
        }

        [WorkItem(995168), WorkItem(1073099)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void SimplifyToPredefinedTypeNameShouldNotBeOfferedInsideNameOf1()
        {
            TestMissing(
@"using System;
class Program
{
    static void Main(string[] args)
    {
        var x = nameof([|Int32|]);
    }
}");
        }

        [WorkItem(995168)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void SimplifyToPredefinedTypeNameShouldNotBeOfferedInsideNameOf2()
        {
            TestMissing(
@"
class Program
{
    static void Main(string[] args)
    {
        var x = nameof([|System.Int32|]);
    }
}");
        }

        [WorkItem(995168), WorkItem(1073099)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void SimplifyToPredefinedTypeNameShouldNotBeOfferedInsideNameOf3()
        {
            TestMissing(
@"using System;
class Program
{
    static void Main(string[] args)
    {
        var x = nameof([|Int32|].MaxValue);
    }
}");
        }

        [WorkItem(995168), WorkItem(1073099)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void SimplifyToPredefinedTypeNameShouldNotBeOfferedInsideNameOf4()
        {
            Test(
@"using System;

class Program
{
    static void Main(string[] args)
    {
        var x = nameof(typeof([|Int32|]));
    }

    static string nameof(Type t)
    {
        return string.Empty;
    }
}",
@"using System;

class Program
{
    static void Main(string[] args)
    {
        var x = nameof(typeof(int));
    }

    static string nameof(Type t)
    {
        return string.Empty;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void SimplifyTypeNameInsideNameOf()
        {
            Test(
@"using System;

class Program
{
    static void Main(string[] args)
    {
        var x = nameof([|System.Int32|]);
    }
}",
@"using System;

class Program
{
    static void Main(string[] args)
    {
        var x = nameof(Int32);
    }
}");
        }

        [WorkItem(995168)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void SimplifyCrefAliasPredefinedType()
        {
            Test(
@"namespace N1
{
    public class C1
    {
        /// <see cref=""[|System.Int32|]""/>
        public C1()
        {

        }
    }
}", @"namespace N1
{
    public class C1
    {
        /// <see cref=""int""/>
        public C1()
        {

        }
    }
}", index: 0);
        }

        [WorkItem(538727)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void SimplifyAlias1()
        {
            var content =
@"using I64 = [|System.Int64|];

namespace N1
{
    class Test
    {
    }
}";

            TestMissing(content);
        }

        [WorkItem(538727)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void SimplifyAlias2()
        {
            Test(
@"using I64 = System.Int64;
using Foo = System.Collections.Generic.IList<[|System.Int64|]>;

namespace N1
{
    class Test
    {
    }
}", @"
using I64 = System.Int64;
using Foo = System.Collections.Generic.IList<long>;

namespace N1
{
    class Test
    {
    }
}", index: 0);
        }

        [WorkItem(538727)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void SimplifyAlias3()
        {
            Test(
@"namespace Outer
{
    using I64 = System.Int64;
    using Foo = System.Collections.Generic.IList<[|System.Int64|]>;

    namespace N1
    {
        class Test
        {
        }
    }
}", @"
namespace Outer
{
    using I64 = System.Int64;
    using Foo = System.Collections.Generic.IList<long>;

    namespace N1
    {
        class Test
        {
        }
    }
}", index: 0);
        }

        [WorkItem(538727)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void SimplifyAlias4()
        {
            Test(
@"using I64 = System.Int64;

namespace Outer
{
    using Foo = System.Collections.Generic.IList<[|System.Int64|]>;

    namespace N1
    {
        class Test
        {
        }
    }
}", @"
using I64 = System.Int64;

namespace Outer
{
    using Foo = System.Collections.Generic.IList<long>;

    namespace N1
    {
        class Test
        {
        }
    }
}", index: 0);
        }

        [WorkItem(544631)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void SimplifyAlias5()
        {
            var content =
@"using System;

namespace N
{
    using X = [|System.Nullable<int>|];
}";

            var result =
@"using System;

namespace N
{
    using X = Nullable<int>;
}";
            Test(content, result);
        }

        [WorkItem(919815)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void SimplifyReturnTypeOnMethodCallToAlias()
        {
            Test(
@"using alias1 = A;
class A
{
    public [|A|] M()
    {
        return null;
    }
}", @"using alias1 = A;
class A
{
    public alias1 M()
    {
        return null;
    }
}", index: 0);
        }

        [WorkItem(538949)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void SimplifyComplexGeneric1()
        {
            TestMissing(
@"class A<T>
{
    class B : A<B> { }
 
    class C : I<B>, I<[|B.B|]> { }
}
 
interface I<T> { }");
        }

        [WorkItem(538949)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void SimplifyComplexGeneric2()
        {
            TestMissing(
@"class A<T>
{
    class B : A<B> { }
 
    class C : I<B>, [|B.B|] { }
}
 
interface I<T> { }");
        }

        [WorkItem(538991)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void SimplifyMissingOnGeneric()
        {
            var content =
@"class A<T, S>
{
    class B : [|A<B, B>|] { }
}";

            TestMissing(content);
        }

        [WorkItem(539000)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void SimplifyMissingOnUnmentionableTypeParameter1()
        {
            var content =
@"class A<T>
{
    class D : A<T[]> { }
    class B { }
 
    class C<T>
    {
        D.B x = new [|D.B|]();
    }
}";

            TestMissing(content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void SimplifyErrorTypeParameter()
        {
            TestMissing(
@"using System.Collections.Generic;
using M = System.Collections.Generic.IList<[|System.Collections.Generic.IList<>|]>;
class C
{
}");
        }

        [WorkItem(539000)]
        [WorkItem(838109)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void SimplifyUnmentionableTypeParameter2()
        {
            TestMissing(
@"class A<T>
{
    class D : A<T[]> { }
    class B { }
 
    class C<Y>
    {
        D.B x = new [|D.B|]();
    }
}");
        }

        [WorkItem(539000)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void SimplifyUnmentionableTypeParameter2_1()
        {
            TestMissing(
@"class A<T>
{
    class D : A<T[]> { }
    class B { }
 
    class C<T>
    {
        D.B x = new [|D.B|]();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestGlobalAlias()
        {
            Test(
@"using System ; using System . Collections . Generic ; using System . Linq ; class Program { static void Main ( string [ ] args ) { [|global :: System |]. String s ; } } ",
@"using System ; using System . Collections . Generic ; using System . Linq ; class Program { static void Main ( string [ ] args ) { string s ; } } ",
index: 0);
        }

        [WorkItem(541748)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestOnErrorInScript()
        {
            TestMissing(
@"[|Console.WrieLine();|]",
Options.Script);
        }

        [Fact(Skip = "1033012"), Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public void TestConflicts()
        {
            Test(
@"namespace OuterNamespace
{
    namespace InnerNamespace
    {
        class InnerClass1
        {
        }
    }

    class OuterClass1
    {
        OuterNamespace.OuterClass1 M1()
        {
            [|OuterNamespace.OuterClass1|] c1;
            OuterNamespace.OuterClass1.Equals(1, 2);
        }

        OuterNamespace.OuterClass2 M2()
        {
            OuterNamespace.OuterClass2 c1;
            OuterNamespace.OuterClass2.Equals(1, 2);
        }

        OuterNamespace.InnerNamespace.InnerClass1 M3()
        {
            OuterNamespace.InnerNamespace.InnerClass1 c1;
            OuterNamespace.InnerNamespace.InnerClass1.Equals(1, 2);
        }

        InnerNamespace.InnerClass1 M3()
        {
            InnerNamespace.InnerClass1 c1;
            global::OuterNamespace.InnerNamespace.InnerClass1.Equals(1, 2);
        }

        void OuterClass2()
        {
        }

        void InnerClass1()
        {
        }

        void InnerNamespace()
        {
        }
    }

    class OuterClass2
    {
        OuterNamespace.OuterClass1 M1()
        {
            OuterNamespace.OuterClass1 c1;
            OuterNamespace.OuterClass1.Equals(1, 2);
        }

        OuterNamespace.OuterClass2 M2()
        {
            OuterNamespace.OuterClass2 c1;
            OuterNamespace.OuterClass2.Equals(1, 2);
        }

        OuterNamespace.InnerNamespace.InnerClass1 M3()
        {
            OuterNamespace.InnerNamespace.InnerClass1 c1;
            OuterNamespace.InnerNamespace.InnerClass1.Equals(1, 2);
        }

        InnerNamespace.InnerClass1 M3()
        {
            InnerNamespace.InnerClass1 c1;
            InnerNamespace.InnerClass1.Equals(1, 2);
        }
    }
}",
@"namespace OuterNamespace
{
    namespace InnerNamespace
    {
        class InnerClass1
        {
        }
    }

    class OuterClass1
    {
        OuterClass1 M1()
        {
            OuterClass1 c1;
            Equals(1, 2);
        }

        OuterClass2 M2()
        {
            OuterClass2 c1;
            Equals(1, 2);
        }

        InnerNamespace.InnerClass1 M3()
        {
            InnerNamespace.InnerClass1 c1;
            Equals(1, 2);
        }

        InnerNamespace.InnerClass1 M3()
        {
            InnerNamespace.InnerClass1 c1;
            Equals(1, 2);
        }

        void OuterClass2()
        {
        }

        void InnerClass1()
        {
        }

        void InnerNamespace()
        {
        }
    }

    class OuterClass2
    {
        OuterClass1 M1()
        {
            OuterClass1 c1;
            Equals(1, 2);
        }

        OuterClass2 M2()
        {
            OuterClass2 c1;
            Equals(1, 2);
        }

        InnerNamespace.InnerClass1 M3()
        {
            InnerNamespace.InnerClass1 c1;
            Equals(1, 2);
        }

        InnerNamespace.InnerClass1 M3()
        {
            InnerNamespace.InnerClass1 c1;
            Equals(1, 2);
        }
    }
}",
index: 1,
compareTokens: false);
        }

        [WorkItem(542100)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestPreventSimplificationThatWouldCauseConflict()
        {
            Test(
@"namespace N { class Program { class Foo { public static void Bar ( ) { } } static void Main ( ) { [|N . Program . Foo . Bar |]( ) ; { int Foo ; } } } } ",
@"namespace N { class Program { class Foo { public static void Bar ( ) { } } static void Main ( ) { Program . Foo . Bar ( ) ; { int Foo ; } } } } ");

            TestMissing(
@"namespace N { class Program { class Foo { public static void Bar ( ) { } } static void Main ( ) { [|Program . Foo . Bar |]( ) ; { int Foo ; } } } } ");
        }

        [WorkItem(541929)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestOnOpenType1()
        {
            TestMissing(
@"class Program < T > { public class Inner { [ Bar ( typeof ( [|Program < > . Inner|] ) ) ] void Foo ( ) { } } } ");
        }

        [WorkItem(541929)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestOnOpenType2()
        {
            Test(
@"class Program { public class Inner < T > { [ Bar ( typeof ( [|Program . Inner < >|] ) ) ] void Foo ( ) { } } } ",
@"class Program { public class Inner < T > { [ Bar ( typeof ( Inner < > ) ) ] void Foo ( ) { } } } ");
        }

        [WorkItem(541929)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestOnOpenType3()
        {
            TestMissing(
@"class Program < X > { public class Inner < Y > { [ Bar ( typeof ( [|Program < > . Inner < >|] ) ) ] void Foo ( ) { } } } ");
        }

        [WorkItem(541929)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestOnOpenType4()
        {
            TestMissing(@"class Program < X > { public class Inner < Y > { [ Bar ( typeof ( [|Program <X > . Inner < >|] ) ) ] void Foo ( ) { } } } ");
        }

        [WorkItem(541929)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestOnOpenType5()
        {
            TestMissing(
@"class Program < X > { public class Inner < Y > { [ Bar ( typeof ( [|Program < > . Inner < Y >|] ) ) ] void Foo ( ) { } } } ");
        }

        [WorkItem(541929)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestOnOpenType6()
        {
            TestMissing(
@"class Program < X > { public class Inner < Y > { [ Bar ( typeof ( [|Program < Y > . Inner < X >|] ) ) ] void Foo ( ) { } } } ");
        }

        [WorkItem(541929)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestOnNonOpenType1()
        {
            Test(
@"class Program { public class Inner { [ Bar ( typeof ( [|Program . Inner|] ) ) ] void Foo ( ) { } } } ",
@"class Program { public class Inner { [ Bar ( typeof ( Inner ) ) ] void Foo ( ) { } } } ",
index: 0);
        }

        [WorkItem(541929)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestOnNonOpenType2()
        {
            Test(
@"class Program < T > { public class Inner { [ Bar ( typeof ( [|Program < T > . Inner |]) ) ] void Foo ( ) { } } } ",
@"class Program < T > { public class Inner { [ Bar ( typeof ( Inner ) ) ] void Foo ( ) { } } } ",
index: 0);
        }

        [WorkItem(541929)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestOnNonOpenType3()
        {
            Test(
@"class Program { public class Inner < T > { [ Bar ( typeof ( [|Program . Inner < >|] ) ) ] void Foo ( ) { } } }",
@"class Program { public class Inner < T > { [ Bar ( typeof ( Inner < > ) ) ] void Foo ( ) { } } } ",
index: 0);
        }

        [WorkItem(541929)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestOnNonOpenType4()
        {
            Test(
@"class Program < X > { public class Inner < Y > { [ Bar ( typeof ( [|Program < X > . Inner < Y > |]) ) ] void Foo ( ) { } } } ",
@"class Program < X > { public class Inner < Y > { [ Bar ( typeof ( Inner < Y > ) ) ] void Foo ( ) { } } } ",
index: 0);
        }

        [WorkItem(541929)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestOnNonOpenType5()
        {
            Test(
@"class Program < X > { public class Inner < Y > { [ Bar ( typeof ( [|Program < X > . Inner < X > |]) ) ] void Foo ( ) { } } } ",
@"class Program < X > { public class Inner < Y > { [ Bar ( typeof ( Inner < X > ) ) ] void Foo ( ) { } } } ",
index: 0);
        }

        [WorkItem(541929)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestOnNonOpenType6()
        {
            TestMissing(
@"class Program < X > { public class Inner < Y > { [ Bar ( typeof ( [|Program < Y > . Inner < Y >|] ) ) ] void Foo ( ) { } } } ");
        }

        [WorkItem(542650)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestWithInterleavedDirective1()
        {
            TestMissing(
@"#if true
class A
#else
class B
#endif
{
    class C { }
 
    static void Main()
    {
#if true
        [|A.
#else
        B.
#endif
            C|] x;
    }
}");
        }

        [WorkItem(542719)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestGlobalMissing1()
        {
            TestMissing(
@"class Program { class System { } int Console = 7; void Main() { string v = null; [|global::System.Console.WriteLine(v)|]; } } ");
        }

        [WorkItem(544615)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestMissingOnAmbiguousCast()
        {
            TestMissing(
@"enum E { } class C { void Main() { var x = ([|global::E|])-1; } } ");
        }

        [WorkItem(544616)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void ParenthesizeIfParseChanges()
        {
            Test(
@"using System;
class C
{
    void M()
    {
        object x = 1;
        var y = [|x as System.Nullable<int>|] + 1;
    }
}",
@"using System;
class C
{
    void M()
    {
        object x = 1;
        var y = (x as int?) + 1;
    }
}", index: 0, compareTokens: false);
        }

        [WorkItem(544974)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestNullableSimplification1()
        {
            Test(
@"class C { static void Main ( ) { [|System . Nullable < int > . Equals |]( 1 , 1 ) ; } }",
@"class C { static void Main ( ) { Equals ( 1 , 1 ) ; } }",
index: 0);
        }

        [WorkItem(544974)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestNullableSimplification3()
        {
            Test(
@"class C { static void Main ([|System . Nullable < int >|] i) { } }",
@"class C { static void Main (int? i) { } }");
        }

        [WorkItem(544974)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestNullableSimplification4()
        {
            Test(
@"class C { static void Main ([|System . Nullable < System.Int32 >|] i) { } }",
@"class C { static void Main (int? i) { } }");
        }

        [WorkItem(544977)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestNullableSimplification5()
        {
            Test(
@"using System;
 
class Program
{
    static void Main()
    {
        var x = [|1 is System.Nullable<int>|]? 2 : 3;
    }
}",
@"using System;
 
class Program
{
    static void Main()
    {
        var x = 1 is int? ? 2 : 3;
    }
}",
  compareTokens: false, index: 0);
        }

        [WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestMissingNullableSimplificationInsideCref()
        {
            TestMissing(
@"using System;
/// <summary>
/// <see cref=""[|Nullable{T}|]""/>
/// </summary>
class A { }");
        }

        [WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestMissingNullableSimplificationInsideCref2()
        {
            TestMissing(
@"/// <summary>
/// <see cref=""[|System.Nullable{T}|]""/>
/// </summary>
class A { }");
        }

        [WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestMissingNullableSimplificationInsideCref3()
        {
            TestMissing(
@"/// <summary>
/// <see cref=""[|System.Nullable{T}|].Value""/>
/// </summary>
class A { }");
        }

        [WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestNullableInsideCref_AllowedIfReferencingActualTypeParameter()
        {
            Test(
@"using System;
/// <summary>
/// <see cref=""C{[|Nullable{T}|]}""/>
/// </summary>
class C<T> {  }",
@"using System;
/// <summary>
/// <see cref=""C{T?}""/>
/// </summary>
class C<T> {  }");
        }

        [WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestMissingNullableSimplificationInsideCref5()
        {
            TestMissing(
@"/// <summary>
/// <see cref=""A.M{[|Nullable{T}|]}()""/>
/// </summary>
class A 
{
    public void M<U>() where U : struct { }
}");
        }

        [WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestNullableInsideCref_AllowedIfReferencingActualType()
        {
            Test(
@"using System;
/// <summary>
/// <see cref=""[|Nullable{int}|]""/>
/// </summary>
class A { }",
@"using System;
/// <summary>
/// <see cref=""int?""/>
/// </summary>
class A { }");
        }

        [WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestNullableInsideCref_AllowedIfReferencingActualType_AsTypeArgument()
        {
            Test(
@"using System;
/// <summary>
/// <see cref=""C{[|Nullable{int}|]}""/>
/// </summary>
class C<T> { }",
@"using System;
/// <summary>
/// <see cref=""C{int?}""/>
/// </summary>
class C<T> { }");
        }

        [WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestMissingNullableSimplificationInsideCref8()
        {
            TestMissing(
@"/// <summary>
/// <see cref=""A.M{[|Nullable{int}|]}()""/>
/// </summary>
class A 
{
    public void M<U>() where U : struct { }
}");
        }

        [WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestNullableSimplificationInsideCref()
        {
            Test(
@"/// <summary>
/// <see cref=""A.M([|System.Nullable{A}|])""/>
/// </summary>
struct A
{ 
    public void M(A? x) { }
}",
@"/// <summary>
/// <see cref=""A.M(A?)""/>
/// </summary>
struct A
{ 
    public void M(A? x) { }
}");
        }

        [WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestNullableSimplificationInsideCref2()
        {
            Test(
@"using System;
using System.Collections.Generic;
/// <summary>
/// <see cref=""A.M(List{[|Nullable{int}|]})""/>
/// </summary>
class A
{ 
    public void M(List<int?> x) { }
}",
@"using System;
using System.Collections.Generic;
/// <summary>
/// <see cref=""A.M(List{int?})""/>
/// </summary>
class A
{ 
    public void M(List<int?> x) { }
}");
        }

        [WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestNullableSimplificationInsideCref3()
        {
            Test(
@"using System;
using System.Collections.Generic;
/// <summary>
/// <see cref=""A.M{U}(List{[|Nullable{U}|]})""/>
/// </summary>
class A
{ 
    public void M<U>(List<U?> x) where U : struct { }
}",
@"using System;
using System.Collections.Generic;
/// <summary>
/// <see cref=""A.M{U}(List{U?})""/>
/// </summary>
class A
{ 
    public void M<U>(List<U?> x) where U : struct { }
}");
        }

        [WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestNullableSimplificationInsideCref4()
        {
            Test(
@"using System;
using System.Collections.Generic;
/// <summary>
/// <see cref=""A.M{T}(List{Nullable{T}}, [|Nullable{T}|])""/>
/// </summary>
class A
{ 
    public void M<U>(List<U?> x, U? y) where U : struct { }
}",
@"using System;
using System.Collections.Generic;
/// <summary>
/// <see cref=""A.M{T}(List{Nullable{T}}, T?})""/>
/// </summary>
class A
{ 
    public void M<U>(List<U?> x, U? y) where U : struct { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestColorColorCase1()
        {
            Test(
@"using N ; namespace N { class Color { public static void Foo ( ) { } public void Bar ( ) { } } } class Program { Color Color ; void Main ( ) { [|N . Color |]. Foo ( ) ; } } ",
@"using N ; namespace N { class Color { public static void Foo ( ) { } public void Bar ( ) { } } } class Program { Color Color ; void Main ( ) { Color . Foo ( ) ; } } ", index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestColorColorCase2()
        {
            TestMissing(
@"using N ; namespace N { class Color { public static void Foo ( ) { } public void Bar ( ) { } } } class Program { Color Color ; void Main ( ) { [|Color . Foo |]( ) ; } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestAliasQualifiedType()
        {
            var source =
@"class Program
{
    static void Main()
    {
        [|global::Program|] a = null; 
    }
}";
            Test(source,
@"
class Program
{
    static void Main()
    {
        Program a = null; 
    }
}", null, 0);

            TestMissing(source, GetScriptOptions());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestSimplifyExpression()
        {
            Test(
@"using System;

class Program
{
    static void Main()
    {
        int x = [|System.Console.Read|]() + System.Console.Read();
    }
}",
@"using System;

class Program
{
    static void Main()
    {
        int x = Console.Read() + System.Console.Read();
    }
}", 0);
        }

        [WorkItem(551040)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestSimplifyStaticMemberAccess()
        {
            var source =
@"class Preserve
{
	public static int Y;
}

class Z<T> : Preserve
{
}

static class M
{
	public static void Main()
	{
		int k = [|Z<float>.Y|];
	}
}";
            Test(source,
@"class Preserve
{
	public static int Y;
}

class Z<T> : Preserve
{
}

static class M
{
	public static void Main()
	{
		int k = Preserve.Y;
	}
}", 0);
        }

        [WorkItem(551040)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestSimplifyNestedType()
        {
            var source =
@"class Preserve
{
	public class X
	{
		public static int Y;
	}
}

class Z<T> : Preserve
{
}

class M
{
	public static void Main()
	{
		int k = [|Z<float>.X|].Y;
	}
}";
            Test(source,
@"class Preserve
{
	public class X
	{
		public static int Y;
	}
}

class Z<T> : Preserve
{
}

class M
{
	public static void Main()
	{
		int k = Preserve.X.Y;
	}
}", null, 0);
        }

        [WorkItem(568043)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void DontSimplifyNamesWhenThereAreParseErrors()
        {
            var markup =
@"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        Console.[||]
    }
}";

            TestMissing(markup);
        }

        [WorkItem(566749)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestMethodGroups1()
        {
            TestMissing(@"
using System;

class Program
{
    static void Main()
    {
        Action a = [|Console.WriteLine|];
    }
}");
        }

        [WorkItem(566749)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestMethodGroups2()
        {
            TestMissing(@"
using System;

class Program
{
    static void Main()
    {
        Action a = [|Console.Blah|];
    }
}");
        }

        [WorkItem(554010)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestMethodGroups3()
        {
            Test(@"
using System;

class Program
{
    static void Main()
    {
        Action a = [|System.Console.WriteLine|];
    }
}", @"
using System;

class Program
{
    static void Main()
    {
        Action a = Console.WriteLine;
    }
}");
        }

        [WorkItem(578686)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void FixAllOccurrences1()
        {
            Test(
@"
using foo = A.B;
using bar = C.D;

class Program
{
    static void Main(string[] args)
    {
        var s = [|new C.D().prop|];
    }
}

namespace A
{
    class B
    {
    }
}

namespace C
{
    class D
    {
        public A.B prop { get; set; }
    }
}",
@"
using foo = A.B;
using bar = C.D;

class Program
{
    static void Main(string[] args)
    {
        var s = new bar().prop;
    }
}

namespace A
{
    class B
    {
    }
}

namespace C
{
    class D
    {
        public A.B prop { get; set; }
    }
}");
        }

        [WorkItem(578686)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void DontUseAlias1()
        {
            TestMissing(
@"
using System.Collections.Generic;
using System.Linq; 

namespace NSA{
    class DuplicateClassName { }
} 

namespace NSB{
    class DuplicateClassName { }
} 

namespace Test{
    using AliasA = NSA.DuplicateClassName;
    using AliasB = NSB.DuplicateClassName;
     class TestClass
    {
        static void Main(string[] args)
        {
            var localA = new NSA.DuplicateClassName();
            var localB = new NSB.DuplicateClassName(); 
            new List<NoAlias.Foo>().Where(m => [|m.InnocentProperty|] == null);
        }
    }
}

namespace NoAlias{
    class Foo    {
        public NSB.DuplicateClassName InnocentProperty { get; set; }
    }
}");
        }

        [WorkItem(577169)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void SuitablyReplaceNullables1()
        {
            TestMissing(
@"
using System;

class Program
{
    static void Main(string[] args)
    {
        var w = new [|Nullable<>|].
    }
}");
        }

        [WorkItem(577169)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void SuitablyReplaceNullables2()
        {
            TestMissing(
@"
using System;

class Program
{
    static void Main(string[] args)
    {
        var x = typeof([|Nullable<>|]);
    }
}");
        }

        [WorkItem(608190)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void Bugfix_608190()
        {
            TestMissing(
@"
using System;

class Program
{
    static void Main(string[] args)
    {
    }
}

struct S
{
    int x;
    S(dynamic y)
    {
        [|object.Equals|](y, 0);
        x = y;
    }
}
");
        }

        [WorkItem(608190)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void Bugfix_608190_1()
        {
            TestMissing(
@"
using System;

class Program
{
    static void Main(string[] args)
    {
    }
}

struct S
{
    int x;
    S(dynamic y)
    {
        x = y;
        [|this.Equals|](y, 0);
    }
}
");
        }

        [WorkItem(608932)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void Bugfix_608932()
        {
            TestMissing(
@"
using S = X;

class Program
{
    static void Main(string[] args)
    {
    }
}

namespace X
{
    using S = System;
 
    enum E { }
 
    class C<E>
    {
        [|X|].E e; // Simplify type name as suggested
    }
}

");
        }

        [WorkItem(635933)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void Bugfix_635933()
        {
            TestMissing(@"
using System;
 
class B
{
    public static void Foo(int x, object y) { }
 
    static void Main()
    {
        C<string>.D.Foo(0);
    }
}
 
class C<T> : B
{
    public class D : C<T> // Start rename session and try to rename D to T
    {
        public static void Foo(dynamic x)
        {
            Console.WriteLine([|D.Foo(x, "")|]);
        }
    }
 
    public static string Foo(int x, T y)
    {
       string s = null;
        return s;
    }
}");
        }

        [WorkItem(547246)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void CodeIssueAtRightSpan()
        {
            var code = @"
using foo = System.Console;
class Program
{
    static void Main(string[] args)
    {
        [|System.Console|].Read();
    }
}
";

            using (var workspace = CreateWorkspaceFromFile(code, null, null))
            {
                var diagnosticAndFix = GetDiagnosticAndFix(workspace);
                var span = diagnosticAndFix.Item1.Location.SourceSpan;
                Assert.NotEqual(span.Start, 0);
                Assert.NotEqual(span.End, 0);
            }
        }

        [WorkItem(579172)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void Bugfix_579172()
        {
            TestMissing(
@"
class C<T, S>
{
    class D : C<[|D.D|], D.D.D> { }
}
");
        }

        [WorkItem(633182)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void Bugfix_633182()
        {
            TestMissing(
@"
class C
{
    void Foo()
    {
        ([|this.Foo|])();
    }
}
");
        }

        [WorkItem(627102)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void Bugfix_627102()
        {
            TestMissing(
@"
using System;
 
class B
{
    static void Foo(int x, object y) { }
 
    static void Foo<T>(dynamic x)
    {
       Console.WriteLine([|C<T>.Foo|](x, ""));
    }
 
    static void Main()
    {
        Foo<string>(0);
    }
}
 
class C<T> : B
{
    public static string Foo(int x, T y)
    {
        return ""Hello world"";
    }
    }

");
        }

        [WorkItem(629572)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void DoNotIncludeAliasNameIfLastTargetNameIsTheSame_1()
        {
            var code = @"
using Generic = System.Collections.Generic;
class Program
{
    static void Main(string[] args)
    {
        var x = new [|System.Collections.Generic|].List<int>();
    }
}
";

            var expected = @"
using Generic = System.Collections.Generic;
class Program
{
    static void Main(string[] args)
    {
        var x = new Generic.List<int>();
    }
}
";
            Test(code, expected);

            using (var workspace = CreateWorkspaceFromFile(code, null, null))
            {
                var diagnosticAndFix = GetDiagnosticAndFix(workspace);
                var span = diagnosticAndFix.Item1.Location.SourceSpan;
                Assert.Equal(span.Start, expected.IndexOf(@"Generic.List<int>()", StringComparison.Ordinal));
                Assert.Equal(span.Length, "System.Collections".Length);
            }
        }

        [WorkItem(629572)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void DoNotIncludeAliasNameIfLastTargetNameIsTheSame_2()
        {
            var code = @"
using Console = System.Console;
class Program
{
    static void Main(string[] args)
    {
        [|System.Console|].WriteLine(""foo"");
    }
}
";

            var expected = @"
using Console = System.Console;
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine(""foo"");
    }
}
";
            Test(code, expected);

            using (var workspace = CreateWorkspaceFromFile(code, null, null))
            {
                var diagnosticAndFix = GetDiagnosticAndFix(workspace);
                var span = diagnosticAndFix.Item1.Location.SourceSpan;
                Assert.Equal(span.Start, expected.IndexOf(@"Console.WriteLine(""foo"")", StringComparison.Ordinal));
                Assert.Equal(span.Length, "System".Length);
            }
        }

        [WorkItem(736377)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void DontSimplifyTypeNameBrokenCode()
        {
            TestMissing(
@"
using System;
using System.Collections.Generic;

class Program
{
    public static void GetA

    [[|System.Diagnostics|].CodeAnalysis.SuppressMessage(""Microsoft.Design"", ""CA1024:UsePropertiesWhereAppropriate"")]
    public static ISet<string> GetAllFilesInSolution()
    {
        return null;
    }
}
");
        }

        [WorkItem(813385)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void DontSimplifyAliases()
        {
            TestMissing(
@"
using Foo = System.Int32;
 
class C
{
    [|Foo|] f;
}");
        }

        [WorkItem(825541)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void ShowOnlyRelevantSpanForReductionOfGenericName()
        {
            var code = @"
namespace A
{
    class Program
    {
        static void Main(string[] args)
        {
            var x = A.B.OtherClass.[|Test<int>|](5);
        }
    }
 
    namespace B
    {
        class OtherClass
        {
            public static int Test<T>(T t) { return 5; }
        }
    }
}";
            using (var workspace = CreateWorkspaceFromFile(code, null, null))
            {
                var diagnosticAndFix = GetDiagnosticAndFix(workspace);
                var span = diagnosticAndFix.Item1.Location.SourceSpan;
                Assert.Equal(span, new TextSpan(135, 5));
            }
        }

        [WorkItem(878773)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void DontSimplifyAttributeNameWithJustAttribute()
        {
            TestMissing(
@"
[[|Attribute|]]
class Attribute : System.Attribute
{

}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void ThisQualificationOption()
        {
            TestMissing(
@"
class C
{
    int x;
    public void z()
    {
        [|this|].x = 4;
    }
}
", new Dictionary<OptionKey, object> { { new OptionKey(SimplificationOptions.QualifyMemberAccessWithThisOrMe, "C#"), true } });
        }

        [WorkItem(942568)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestIntrinsicTypesInLocalDeclarationDefaultValue1()
        {
            Test(
@"
class C
{
    [|System.Int32|] x;
    public void z()
    {
    }
}", @"
class C
{
    int x;
    public void z()
    {
    }
}");
        }

        [WorkItem(942568)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestIntrinsicTypesInLocalDeclarationDefaultValue2()
        {
            Test(
@"
class C
{
    [|System.Int32|]? x;
    public void z()
    {
    }
}", @"
class C
{
    int? x;
    public void z()
    {
    }
}", compareTokens: false);
        }

        [WorkItem(942568)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestIntrinsicTypesInsideCref_Default_1()
        {
            Test(
@"
using System;
class C
{
    /// <see cref=""[|Int32|]""/>
    public void z()
    {
    }
}", @"
using System;
class C
{
    /// <see cref=""int""/>
    public void z()
    {
    }
}", compareTokens: false);
        }

        [WorkItem(942568)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestIntrinsicTypesInsideCref_Default_2()
        {
            Test(
@"
class C
{
    /// <see cref=""[|System.Int32|]""/>
    public void z()
    {
    }
}", @"
class C
{
    /// <see cref=""int""/>
    public void z()
    {
    }
}", compareTokens: false);
        }

        [WorkItem(942568)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestIntrinsicTypesInsideCref_Default_3()
        {
            Test(
@"
using System;
class C
{
    /// <see cref=""[|Int32|].MaxValue""/>
    public void z()
    {
    }
}", @"
using System;
class C
{
    /// <see cref=""int.MaxValue""/>
    public void z()
    {
    }
}", compareTokens: false);
        }

        [WorkItem(942568)]
        [WorkItem(954536)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestIntrinsicTypesInsideCref_NonDefault_1()
        {
            TestMissing(
@"
using System;
class C
{
    /// <see cref=""[|Int32|]""/>
    public void z()
    {
    }
}", options: new Dictionary<OptionKey, object> { { new OptionKey(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, "C#"), false } });
        }

        [WorkItem(942568)]
        [WorkItem(954536)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestIntrinsicTypesInsideCref_NonDefault_2()
        {
            Test(
@"
using System;
class C
{
    /// <see cref=""[|Int32|]""/>
    public void z()
    {
    }
}",
@"
using System;
class C
{
    /// <see cref=""int""/>
    public void z()
    {
    }
}", options: new Dictionary<OptionKey, object> { { new OptionKey(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, "C#"), false } });
        }

        [WorkItem(942568)]
        [WorkItem(954536)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestIntrinsicTypesInsideCref_NonDefault_3()
        {
            TestMissing(
@"
using System;
class C
{
    /// <see cref=""[|Int32|].MaxValue""/>
    public void z()
    {
    }
}", options: new Dictionary<OptionKey, object> { { new OptionKey(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, "C#"), false } });
        }

        [WorkItem(954536)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestIntrinsicTypesInsideCref_NonDefault_4()
        {
            Test(
@"
using System;
class C
{
    /// <see cref=""[|Int32|].MaxValue""/>
    public void z()
    {
    }
}",
@"
using System;
class C
{
    /// <see cref=""int.MaxValue""/>
    public void z()
    {
    }
}",
options: new Dictionary<OptionKey, object> { { new OptionKey(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, "C#"), false } });
        }

        [WorkItem(954536)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestIntrinsicTypesInsideCref_NonDefault_5()
        {
            TestMissing(
@"
class C
{
    /// <see cref=""System.Collections.Generic.List{T}.CopyTo([|System.Int32|], T[], int, int)""/>
    public void z()
    {
    }
}", options: new Dictionary<OptionKey, object> { { new OptionKey(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, "C#"), false } });
        }

        [WorkItem(954536)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestIntrinsicTypesInsideCref_NonDefault_6()
        {
            Test(
@"
class C
{
    /// <see cref=""System.Collections.Generic.List{T}.CopyTo([|System.Int32|], T[], int, int)""/>
    public void z()
    {
    }
}",
@"
class C
{
    /// <see cref=""System.Collections.Generic.List{T}.CopyTo(int, T[], int, int)""/>
    public void z()
    {
    }
}",
options: new Dictionary<OptionKey, object> { { new OptionKey(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, "C#"), false } });
        }

        [WorkItem(942568)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestIntrinsicTypesInLocalDeclarationNonDefaultValue_1()
        {
            TestMissing(
@"
class C
{
    [|System.Int32|] x;
    public void z(System.Int32 y)
    {
        System.Int32 z = 9;
    }
}
", options: new Dictionary<OptionKey, object> { { new OptionKey(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, "C#"), false } });
        }

        [WorkItem(942568)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestIntrinsicTypesInLocalDeclarationNonDefaultValue_2()
        {
            TestMissing(
@"
class C
{
    System.Int32 x;
    public void z([|System.Int32|] y)
    {
        System.Int32 z = 9;
    }
}
", options: new Dictionary<OptionKey, object> { { new OptionKey(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, "C#"), false } });
        }

        [WorkItem(942568)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestIntrinsicTypesInLocalDeclarationNonDefaultValue_3()
        {
            TestMissing(
@"
class C
{
    System.Int32 x;
    public void z(System.Int32 y)
    {
        [|System.Int32|] z = 9;
    }
}
", options: new Dictionary<OptionKey, object> { { new OptionKey(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, "C#"), false } });
        }

        [WorkItem(942568)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestIntrinsicTypesInMemberAccess_Default_1()
        {
            Test(
@"
class C
{
    public void z()
    {
        var sss = [|System.Int32|].MaxValue;
    }
}", @"
class C
{
    public void z()
    {
        var sss = int.MaxValue;
    }
}");
        }

        [WorkItem(942568)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestIntrinsicTypesInMemberAccess_Default_2()
        {
            Test(
@"
using System;
class C
{
    public void z()
    {
        var sss = [|Int32|].MaxValue;
    }
}", @"
using System;
class C
{
    public void z()
    {
        var sss = int.MaxValue;
    }
}");
        }

        [WorkItem(956667)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestIntrinsicTypesInMemberAccess_Default_3()
        {
            TestMissing(
@"
using System;
class C1
{
    public static void z()
    {
        var sss = [|C2.Memb|].ToString();
    }
}

class C2
{
    public static int Memb;
}");
        }

        [WorkItem(942568)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestIntrinsicTypesInMemberAccess_NonDefault_1()
        {
            TestMissing(
@"
using System;
class C
{
    public void z()
    {
        var sss = [|Int32|].MaxValue;
    }
}
", options: new Dictionary<OptionKey, object> { { new OptionKey(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, "C#"), false } });
        }

        [WorkItem(942568)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestIntrinsicTypesInMemberAccess_NonDefault_2()
        {
            TestMissing(
@"
class C
{
    public void z()
    {
        var sss = [|System.Int32|].MaxValue;
    }
}
", options: new Dictionary<OptionKey, object> { { new OptionKey(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, "C#"), false } });
        }

        [WorkItem(965208)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestSimplifyDiagnosticId()
        {
            var source =
@"
using System;

class C
{
    public void z()
    {
        [|System.Console.WriteLine|]("");
    }
}";
            using (var workspace = CreateWorkspaceFromFile(source, null, null))
            {
                var diagnostics = GetDiagnostics(workspace).Where(d => d.Id == IDEDiagnosticIds.SimplifyMemberAccessDiagnosticId);
                Assert.Equal(1, diagnostics.Count());
            }

            source =
@"
using System;

class C
{
    public void z()
    {
        [|System.Int32|] a;
    }
}";
            using (var workspace = CreateWorkspaceFromFile(source, null, null))
            {
                var diagnostics = GetDiagnostics(workspace).Where(d => d.Id == IDEDiagnosticIds.SimplifyNamesDiagnosticId);
                Assert.Equal(1, diagnostics.Count());
            }

            source =
@"
using System;

class C
{
    private int x = 0;
    public void z()
    {
        var a = [|this.x|];
    }
}";
            using (var workspace = CreateWorkspaceFromFile(source, null, null))
            {
                var diagnostics = GetDiagnostics(workspace).Where(d => d.Id == IDEDiagnosticIds.SimplifyThisOrMeDiagnosticId);
                Assert.Equal(1, diagnostics.Count());
            }
        }

        [WorkItem(1019276)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestSimplifyTypeNameDoesNotAddUnnecessaryParens()
        {
            Test(
@"
using System;

class Program
{
    static void F()
    {
        object o = null;
        if (![|(o is Byte)|])
        {
        }
    }
}", @"
using System;

class Program
{
    static void F()
    {
        object o = null;
        if (!(o is byte))
        {
        }
    }
}");
        }

        [WorkItem(1068445)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestSimplifyTypeNameInPropertyLambda()
        {
            Test(
@"namespace ClassLibrary2
{
    public class Class1
    {
        public object X => ([|System.Int32|])0;
    }
}", @"namespace ClassLibrary2
{
    public class Class1
    {
        public object X => (int)0;
    }
}");
        }

        [WorkItem(1068445)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestSimplifyTypeNameInMethodLambda()
        {
            Test(
@"class C
{
    public string Foo() => ([|System.String|])"";
}", @"class C
{
    public string Foo() => (string)"";
}");
        }

        [WorkItem(1068445)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public void TestSimplifyTypeNameInIndexerLambda()
        {
            Test(
@"class C
{
    public int this[int index] => ([|System.Int32|])0;
}", @"class C
{
    public int this[int index] => (int)0;
}");
        }
    }
}
