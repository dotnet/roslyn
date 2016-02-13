// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        public async Task SimplifyGenericName()
        {
            await TestAsync(
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
        public async Task UseAlias0()
        {
            await TestAsync(
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
        public async Task UseAlias00()
        {
            await TestAsync(
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
        public async Task UseAlias()
        {
            var source =
@"using MyType = System.Exception;

class A 
{
    [|System.Exception|] c;
}";

            await TestAsync(source,
@"using MyType = System.Exception;

class A 
{
    MyType c;
}", index: 0);

            await TestActionCountAsync(source, 1);
            await TestSpansAsync(source,
@"using MyType = System.Exception;

class A 
{
    [|System.Exception|] c;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task UseAlias1()
        {
            await TestAsync(
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
        public async Task UseAlias2()
        {
            await TestAsync(
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
        public async Task UseAlias3()
        {
            await TestAsync(
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
        public async Task UseAlias4()
        {
            await TestAsync(
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
        public async Task UseAlias5()
        {
            await TestAsync(
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
        public async Task UseAlias6()
        {
            await TestAsync(
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
        public async Task UseAlias7()
        {
            await TestAsync(
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
        public async Task UseAlias8()
        {
            await TestAsync(
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
        public async Task TwoAliases()
        {
            await TestAsync(
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
        public async Task TwoAliases2()
        {
            await TestAsync(
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
        public async Task TwoAliasesConflict()
        {
            await TestMissingAsync(
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
        public async Task TwoAliasesConflict2()
        {
            await TestAsync(
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
        public async Task AliasInSiblingNamespace()
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
            await TestMissingAsync(content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task KeywordInt32()
        {
            var source =
@"class A
{
    [|System.Int32|] i;
}";
            await TestAsync(source,
@"
class A
{
    int i;
}", index: 0);
            await TestActionCountAsync(source, 1);
            await TestSpansAsync(source,
@"class A
{
    [|System.Int32|] i;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task Keywords()
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
                await TestAsync(newContent, expected, index: 0);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task SimplifyTypeName()
        {
            var content =
@"namespace Root 
{
    class A 
    {
        [|System.Exception|] c;
    }
}";
            await TestMissingAsync(content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task SimplifyTypeName1()
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

            await TestAsync(source, @"
using System;

namespace Root 
{
    class A 
    {
        Exception c;
    }
}", index: 0);
            await TestActionCountAsync(source, 1);
            await TestSpansAsync(source,
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
        public async Task SimplifyTypeName2()
        {
            await TestAsync(
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
        public async Task SimplifyTypeName3()
        {
            await TestAsync(
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
        public async Task SimplifyTypeName4()
        {
            // this is failing since we can't speculatively bind namespace yet
            await TestAsync(
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
        public async Task SimplifyTypeName5()
        {
            await TestAsync(
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
        public async Task SimplifyTypeName6()
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
            await TestMissingAsync(content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task SimplifyTypeName7()
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

            await TestAsync(source, @"
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

            await TestActionCountAsync(source, 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task SimplifyGenericTypeName1()
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
            await TestMissingAsync(content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task SimplifyGenericTypeName2()
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

            await TestAsync(source, @"
using System;

namespace N1
{
    public class A1
    {
        EventHandler<EventArgs> a;
    }
}", index: 0);

            await TestActionCountAsync(source, 1);
        }

        [Fact(Skip = "1033012"), Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task SimplifyGenericTypeName3()
        {
            var fixAllActionId = SimplifyTypeNamesCodeFixProvider.GetCodeActionId(IDEDiagnosticIds.SimplifyMemberAccessDiagnosticId, "System.Action");
            await TestAsync(
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
        public async Task SimplifyGenericTypeName4()
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
            await TestMissingAsync(content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task SimplifyGenericTypeName5()
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

            await TestAsync(source, @"
using MyHandler = System.EventHandler<System.EventArgs>;

namespace N1
{
    public class A1
    {
        System.EventHandler<MyHandler> a;
    }
}", index: 0);
            await TestActionCountAsync(source, 1);
            await TestSpansAsync(source,
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
        public async Task SimplifyGenericTypeName6()
        {
            await TestAsync(
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
        public async Task SimplifyGenericTypeName7()
        {
            await TestAsync(
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
        public async Task Array1()
        {
            await TestAsync(
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
        public async Task Array2()
        {
            await TestAsync(
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

        [WorkItem(995168, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/995168"), WorkItem(1073099, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1073099")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task SimplifyToPredefinedTypeNameShouldNotBeOfferedInsideNameOf1()
        {
            await TestMissingAsync(
@"using System;
class Program
{
    static void Main(string[] args)
    {
        var x = nameof([|Int32|]);
    }
}");
        }

        [WorkItem(995168, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/995168")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task SimplifyToPredefinedTypeNameShouldNotBeOfferedInsideNameOf2()
        {
            await TestMissingAsync(
@"
class Program
{
    static void Main(string[] args)
    {
        var x = nameof([|System.Int32|]);
    }
}");
        }

        [WorkItem(995168, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/995168"), WorkItem(1073099, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1073099")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task SimplifyToPredefinedTypeNameShouldNotBeOfferedInsideNameOf3()
        {
            await TestMissingAsync(
@"using System;
class Program
{
    static void Main(string[] args)
    {
        var x = nameof([|Int32|].MaxValue);
    }
}");
        }

        [WorkItem(995168, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/995168"), WorkItem(1073099, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1073099")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task SimplifyToPredefinedTypeNameShouldNotBeOfferedInsideNameOf4()
        {
            await TestAsync(
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
        public async Task SimplifyTypeNameInsideNameOf()
        {
            await TestAsync(
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

        [WorkItem(995168, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/995168")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task SimplifyCrefAliasPredefinedType()
        {
            await TestAsync(
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

        [WorkItem(538727, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538727")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task SimplifyAlias1()
        {
            var content =
@"using I64 = [|System.Int64|];

namespace N1
{
    class Test
    {
    }
}";

            await TestMissingAsync(content);
        }

        [WorkItem(538727, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538727")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task SimplifyAlias2()
        {
            await TestAsync(
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

        [WorkItem(538727, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538727")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task SimplifyAlias3()
        {
            await TestAsync(
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

        [WorkItem(538727, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538727")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task SimplifyAlias4()
        {
            await TestAsync(
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

        [WorkItem(544631, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544631")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task SimplifyAlias5()
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
            await TestAsync(content, result);
        }

        [WorkItem(919815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/919815")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task SimplifyReturnTypeOnMethodCallToAlias()
        {
            await TestAsync(
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

        [WorkItem(538949, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538949")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task SimplifyComplexGeneric1()
        {
            await TestMissingAsync(
@"class A<T>
{
    class B : A<B> { }
 
    class C : I<B>, I<[|B.B|]> { }
}
 
interface I<T> { }");
        }

        [WorkItem(538949, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538949")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task SimplifyComplexGeneric2()
        {
            await TestMissingAsync(
@"class A<T>
{
    class B : A<B> { }
 
    class C : I<B>, [|B.B|] { }
}
 
interface I<T> { }");
        }

        [WorkItem(538991, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538991")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task SimplifyMissingOnGeneric()
        {
            var content =
@"class A<T, S>
{
    class B : [|A<B, B>|] { }
}";

            await TestMissingAsync(content);
        }

        [WorkItem(539000, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539000")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task SimplifyMissingOnUnmentionableTypeParameter1()
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

            await TestMissingAsync(content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task SimplifyErrorTypeParameter()
        {
            await TestMissingAsync(
@"using System.Collections.Generic;
using M = System.Collections.Generic.IList<[|System.Collections.Generic.IList<>|]>;
class C
{
}");
        }

        [WorkItem(539000, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539000")]
        [WorkItem(838109, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/838109")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task SimplifyUnmentionableTypeParameter2()
        {
            await TestMissingAsync(
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

        [WorkItem(539000, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539000")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task SimplifyUnmentionableTypeParameter2_1()
        {
            await TestMissingAsync(
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
        public async Task TestGlobalAlias()
        {
            await TestAsync(
@"using System ; using System . Collections . Generic ; using System . Linq ; class Program { static void Main ( string [ ] args ) { [|global :: System |]. String s ; } } ",
@"using System ; using System . Collections . Generic ; using System . Linq ; class Program { static void Main ( string [ ] args ) { string s ; } } ",
index: 0);
        }

        [WorkItem(541748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestOnErrorInScript()
        {
            await TestMissingAsync(
@"[|Console.WrieLine();|]",
Options.Script);
        }

        [Fact(Skip = "1033012"), Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestConflicts()
        {
            await TestAsync(
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

        [WorkItem(542100, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542100")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestPreventSimplificationThatWouldCauseConflict()
        {
            await TestAsync(
@"namespace N { class Program { class Foo { public static void Bar ( ) { } } static void Main ( ) { [|N . Program . Foo . Bar |]( ) ; { int Foo ; } } } } ",
@"namespace N { class Program { class Foo { public static void Bar ( ) { } } static void Main ( ) { Program . Foo . Bar ( ) ; { int Foo ; } } } } ");

            await TestMissingAsync(
@"namespace N { class Program { class Foo { public static void Bar ( ) { } } static void Main ( ) { [|Program . Foo . Bar |]( ) ; { int Foo ; } } } } ");
        }

        [WorkItem(541929, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541929")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestOnOpenType1()
        {
            await TestMissingAsync(
@"class Program < T > { public class Inner { [ Bar ( typeof ( [|Program < > . Inner|] ) ) ] void Foo ( ) { } } } ");
        }

        [WorkItem(541929, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541929")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestOnOpenType2()
        {
            await TestAsync(
@"class Program { public class Inner < T > { [ Bar ( typeof ( [|Program . Inner < >|] ) ) ] void Foo ( ) { } } } ",
@"class Program { public class Inner < T > { [ Bar ( typeof ( Inner < > ) ) ] void Foo ( ) { } } } ");
        }

        [WorkItem(541929, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541929")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestOnOpenType3()
        {
            await TestMissingAsync(
@"class Program < X > { public class Inner < Y > { [ Bar ( typeof ( [|Program < > . Inner < >|] ) ) ] void Foo ( ) { } } } ");
        }

        [WorkItem(541929, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541929")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestOnOpenType4()
        {
            await TestMissingAsync(@"class Program < X > { public class Inner < Y > { [ Bar ( typeof ( [|Program <X > . Inner < >|] ) ) ] void Foo ( ) { } } } ");
        }

        [WorkItem(541929, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541929")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestOnOpenType5()
        {
            await TestMissingAsync(
@"class Program < X > { public class Inner < Y > { [ Bar ( typeof ( [|Program < > . Inner < Y >|] ) ) ] void Foo ( ) { } } } ");
        }

        [WorkItem(541929, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541929")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestOnOpenType6()
        {
            await TestMissingAsync(
@"class Program < X > { public class Inner < Y > { [ Bar ( typeof ( [|Program < Y > . Inner < X >|] ) ) ] void Foo ( ) { } } } ");
        }

        [WorkItem(541929, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541929")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestOnNonOpenType1()
        {
            await TestAsync(
@"class Program { public class Inner { [ Bar ( typeof ( [|Program . Inner|] ) ) ] void Foo ( ) { } } } ",
@"class Program { public class Inner { [ Bar ( typeof ( Inner ) ) ] void Foo ( ) { } } } ",
index: 0);
        }

        [WorkItem(541929, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541929")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestOnNonOpenType2()
        {
            await TestAsync(
@"class Program < T > { public class Inner { [ Bar ( typeof ( [|Program < T > . Inner |]) ) ] void Foo ( ) { } } } ",
@"class Program < T > { public class Inner { [ Bar ( typeof ( Inner ) ) ] void Foo ( ) { } } } ",
index: 0);
        }

        [WorkItem(541929, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541929")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestOnNonOpenType3()
        {
            await TestAsync(
@"class Program { public class Inner < T > { [ Bar ( typeof ( [|Program . Inner < >|] ) ) ] void Foo ( ) { } } }",
@"class Program { public class Inner < T > { [ Bar ( typeof ( Inner < > ) ) ] void Foo ( ) { } } } ",
index: 0);
        }

        [WorkItem(541929, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541929")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestOnNonOpenType4()
        {
            await TestAsync(
@"class Program < X > { public class Inner < Y > { [ Bar ( typeof ( [|Program < X > . Inner < Y > |]) ) ] void Foo ( ) { } } } ",
@"class Program < X > { public class Inner < Y > { [ Bar ( typeof ( Inner < Y > ) ) ] void Foo ( ) { } } } ",
index: 0);
        }

        [WorkItem(541929, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541929")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestOnNonOpenType5()
        {
            await TestAsync(
@"class Program < X > { public class Inner < Y > { [ Bar ( typeof ( [|Program < X > . Inner < X > |]) ) ] void Foo ( ) { } } } ",
@"class Program < X > { public class Inner < Y > { [ Bar ( typeof ( Inner < X > ) ) ] void Foo ( ) { } } } ",
index: 0);
        }

        [WorkItem(541929, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541929")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestOnNonOpenType6()
        {
            await TestMissingAsync(
@"class Program < X > { public class Inner < Y > { [ Bar ( typeof ( [|Program < Y > . Inner < Y >|] ) ) ] void Foo ( ) { } } } ");
        }

        [WorkItem(542650, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542650")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestWithInterleavedDirective1()
        {
            await TestMissingAsync(
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

        [WorkItem(542719, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542719")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestGlobalMissing1()
        {
            await TestMissingAsync(
@"class Program { class System { } int Console = 7; void Main() { string v = null; [|global::System.Console.WriteLine(v)|]; } } ");
        }

        [WorkItem(544615, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544615")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestMissingOnAmbiguousCast()
        {
            await TestMissingAsync(
@"enum E { } class C { void Main() { var x = ([|global::E|])-1; } } ");
        }

        [WorkItem(544616, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544616")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task ParenthesizeIfParseChanges()
        {
            await TestAsync(
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

        [WorkItem(544974, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544974")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestNullableSimplification1()
        {
            await TestAsync(
@"class C { static void Main ( ) { [|System . Nullable < int > . Equals |]( 1 , 1 ) ; } }",
@"class C { static void Main ( ) { Equals ( 1 , 1 ) ; } }",
index: 0);
        }

        [WorkItem(544974, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544974")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestNullableSimplification3()
        {
            await TestAsync(
@"class C { static void Main ([|System . Nullable < int >|] i) { } }",
@"class C { static void Main (int? i) { } }");
        }

        [WorkItem(544974, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544974")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestNullableSimplification4()
        {
            await TestAsync(
@"class C { static void Main ([|System . Nullable < System.Int32 >|] i) { } }",
@"class C { static void Main (int? i) { } }");
        }

        [WorkItem(544977, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544977")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestNullableSimplification5()
        {
            await TestAsync(
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
        public async Task TestMissingNullableSimplificationInsideCref()
        {
            await TestMissingAsync(
@"using System;
/// <summary>
/// <see cref=""[|Nullable{T}|]""/>
/// </summary>
class A { }");
        }

        [WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestMissingNullableSimplificationInsideCref2()
        {
            await TestMissingAsync(
@"/// <summary>
/// <see cref=""[|System.Nullable{T}|]""/>
/// </summary>
class A { }");
        }

        [WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestMissingNullableSimplificationInsideCref3()
        {
            await TestMissingAsync(
@"/// <summary>
/// <see cref=""[|System.Nullable{T}|].Value""/>
/// </summary>
class A { }");
        }

        [WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestNullableInsideCref_AllowedIfReferencingActualTypeParameter()
        {
            await TestAsync(
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
        public async Task TestMissingNullableSimplificationInsideCref5()
        {
            await TestMissingAsync(
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
        public async Task TestNullableInsideCref_AllowedIfReferencingActualType()
        {
            await TestAsync(
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
        public async Task TestNullableInsideCref_AllowedIfReferencingActualType_AsTypeArgument()
        {
            await TestAsync(
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
        public async Task TestMissingNullableSimplificationInsideCref8()
        {
            await TestMissingAsync(
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
        public async Task TestNullableSimplificationInsideCref()
        {
            await TestAsync(
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
        public async Task TestNullableSimplificationInsideCref2()
        {
            await TestAsync(
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
        public async Task TestNullableSimplificationInsideCref3()
        {
            await TestAsync(
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
        public async Task TestNullableSimplificationInsideCref4()
        {
            await TestAsync(
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
        public async Task TestColorColorCase1()
        {
            await TestAsync(
@"using N ; namespace N { class Color { public static void Foo ( ) { } public void Bar ( ) { } } } class Program { Color Color ; void Main ( ) { [|N . Color |]. Foo ( ) ; } } ",
@"using N ; namespace N { class Color { public static void Foo ( ) { } public void Bar ( ) { } } } class Program { Color Color ; void Main ( ) { Color . Foo ( ) ; } } ", index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestColorColorCase2()
        {
            await TestMissingAsync(
@"using N ; namespace N { class Color { public static void Foo ( ) { } public void Bar ( ) { } } } class Program { Color Color ; void Main ( ) { [|Color . Foo |]( ) ; } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestAliasQualifiedType()
        {
            var source =
@"class Program
{
    static void Main()
    {
        [|global::Program|] a = null; 
    }
}";
            await TestAsync(source,
@"
class Program
{
    static void Main()
    {
        Program a = null; 
    }
}", null, 0);

            await TestMissingAsync(source, GetScriptOptions());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestSimplifyExpression()
        {
            await TestAsync(
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

        [WorkItem(551040, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/551040")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestSimplifyStaticMemberAccess()
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
            await TestAsync(source,
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

        [WorkItem(551040, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/551040")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestSimplifyNestedType()
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
            await TestAsync(source,
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

        [WorkItem(568043, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/568043")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task DontSimplifyNamesWhenThereAreParseErrors()
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

            await TestMissingAsync(markup);
        }

        [WorkItem(566749, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/566749")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestMethodGroups1()
        {
            await TestMissingAsync(@"
using System;

class Program
{
    static void Main()
    {
        Action a = [|Console.WriteLine|];
    }
}");
        }

        [WorkItem(566749, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/566749")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestMethodGroups2()
        {
            await TestMissingAsync(@"
using System;

class Program
{
    static void Main()
    {
        Action a = [|Console.Blah|];
    }
}");
        }

        [WorkItem(554010, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/554010")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestMethodGroups3()
        {
            await TestAsync(@"
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

        [WorkItem(578686, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578686")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task FixAllOccurrences1()
        {
            await TestAsync(
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

        [WorkItem(578686, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578686")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task DontUseAlias1()
        {
            await TestMissingAsync(
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

        [WorkItem(577169, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/577169")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task SuitablyReplaceNullables1()
        {
            await TestMissingAsync(
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

        [WorkItem(577169, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/577169")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task SuitablyReplaceNullables2()
        {
            await TestMissingAsync(
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

        [WorkItem(608190, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/608190")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task Bugfix_608190()
        {
            await TestMissingAsync(
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

        [WorkItem(608190, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/608190")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task Bugfix_608190_1()
        {
            await TestMissingAsync(
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

        [WorkItem(608932, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/608932")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task Bugfix_608932()
        {
            await TestMissingAsync(
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

        [WorkItem(635933, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/635933")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task Bugfix_635933()
        {
            await TestMissingAsync(@"
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

        [WorkItem(547246, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547246")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task CodeIssueAtRightSpan()
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

            using (var workspace = await CreateWorkspaceFromFileAsync(code, null, null))
            {
                var diagnosticAndFix = await GetDiagnosticAndFixAsync(workspace);
                var span = diagnosticAndFix.Item1.Location.SourceSpan;
                Assert.NotEqual(span.Start, 0);
                Assert.NotEqual(span.End, 0);
            }
        }

        [WorkItem(579172, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/579172")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task Bugfix_579172()
        {
            await TestMissingAsync(
@"
class C<T, S>
{
    class D : C<[|D.D|], D.D.D> { }
}
");
        }

        [WorkItem(633182, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/633182")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task Bugfix_633182()
        {
            await TestMissingAsync(
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

        [WorkItem(627102, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627102")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task Bugfix_627102()
        {
            await TestMissingAsync(
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

        [WorkItem(629572, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/629572")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task DoNotIncludeAliasNameIfLastTargetNameIsTheSame_1()
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
            await TestAsync(code, expected);

            using (var workspace = await CreateWorkspaceFromFileAsync(code, null, null))
            {
                var diagnosticAndFix = await GetDiagnosticAndFixAsync(workspace);
                var span = diagnosticAndFix.Item1.Location.SourceSpan;
                Assert.Equal(span.Start, expected.IndexOf(@"Generic.List<int>()", StringComparison.Ordinal));
                Assert.Equal(span.Length, "System.Collections".Length);
            }
        }

        [WorkItem(629572, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/629572")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task DoNotIncludeAliasNameIfLastTargetNameIsTheSame_2()
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
            await TestAsync(code, expected);

            using (var workspace = await CreateWorkspaceFromFileAsync(code, null, null))
            {
                var diagnosticAndFix = await GetDiagnosticAndFixAsync(workspace);
                var span = diagnosticAndFix.Item1.Location.SourceSpan;
                Assert.Equal(span.Start, expected.IndexOf(@"Console.WriteLine(""foo"")", StringComparison.Ordinal));
                Assert.Equal(span.Length, "System".Length);
            }
        }

        [WorkItem(736377, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/736377")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task DontSimplifyTypeNameBrokenCode()
        {
            await TestMissingAsync(
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

        [WorkItem(813385, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/813385")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task DontSimplifyAliases()
        {
            await TestMissingAsync(
@"
using Foo = System.Int32;
 
class C
{
    [|Foo|] f;
}");
        }

        [WorkItem(825541, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/825541")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task ShowOnlyRelevantSpanForReductionOfGenericName()
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
            using (var workspace = await CreateWorkspaceFromFileAsync(code, null, null))
            {
                var diagnosticAndFix = await GetDiagnosticAndFixAsync(workspace);
                var span = diagnosticAndFix.Item1.Location.SourceSpan;
                Assert.Equal(span, new TextSpan(135, 5));
            }
        }

        [WorkItem(878773, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/878773")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task DontSimplifyAttributeNameWithJustAttribute()
        {
            await TestMissingAsync(
@"
[[|Attribute|]]
class Attribute : System.Attribute
{

}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task ThisQualificationOption()
        {
            await TestMissingAsync(
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

        [WorkItem(942568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestIntrinsicTypesInLocalDeclarationDefaultValue1()
        {
            await TestAsync(
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

        [WorkItem(942568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestIntrinsicTypesInLocalDeclarationDefaultValue2()
        {
            await TestAsync(
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

        [WorkItem(942568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestIntrinsicTypesInsideCref_Default_1()
        {
            await TestAsync(
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

        [WorkItem(942568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestIntrinsicTypesInsideCref_Default_2()
        {
            await TestAsync(
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

        [WorkItem(942568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestIntrinsicTypesInsideCref_Default_3()
        {
            await TestAsync(
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

        [WorkItem(942568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")]
        [WorkItem(954536, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/954536")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestIntrinsicTypesInsideCref_NonDefault_1()
        {
            await TestMissingAsync(
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

        [WorkItem(942568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")]
        [WorkItem(954536, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/954536")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestIntrinsicTypesInsideCref_NonDefault_2()
        {
            await TestAsync(
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

        [WorkItem(942568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")]
        [WorkItem(954536, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/954536")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestIntrinsicTypesInsideCref_NonDefault_3()
        {
            await TestMissingAsync(
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

        [WorkItem(954536, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/954536")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestIntrinsicTypesInsideCref_NonDefault_4()
        {
            await TestAsync(
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

        [WorkItem(954536, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/954536")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestIntrinsicTypesInsideCref_NonDefault_5()
        {
            await TestMissingAsync(
@"
class C
{
    /// <see cref=""System.Collections.Generic.List{T}.CopyTo([|System.Int32|], T[], int, int)""/>
    public void z()
    {
    }
}", options: new Dictionary<OptionKey, object> { { new OptionKey(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, "C#"), false } });
        }

        [WorkItem(954536, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/954536")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestIntrinsicTypesInsideCref_NonDefault_6()
        {
            await TestAsync(
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

        [WorkItem(942568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestIntrinsicTypesInLocalDeclarationNonDefaultValue_1()
        {
            await TestMissingAsync(
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

        [WorkItem(942568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestIntrinsicTypesInLocalDeclarationNonDefaultValue_2()
        {
            await TestMissingAsync(
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

        [WorkItem(942568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestIntrinsicTypesInLocalDeclarationNonDefaultValue_3()
        {
            await TestMissingAsync(
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

        [WorkItem(942568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestIntrinsicTypesInMemberAccess_Default_1()
        {
            await TestAsync(
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

        [WorkItem(942568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestIntrinsicTypesInMemberAccess_Default_2()
        {
            await TestAsync(
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

        [WorkItem(956667, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/956667")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestIntrinsicTypesInMemberAccess_Default_3()
        {
            await TestMissingAsync(
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

        [WorkItem(942568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestIntrinsicTypesInMemberAccess_NonDefault_1()
        {
            await TestMissingAsync(
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

        [WorkItem(942568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestIntrinsicTypesInMemberAccess_NonDefault_2()
        {
            await TestMissingAsync(
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

        [WorkItem(965208, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/965208")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestSimplifyDiagnosticId()
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
            using (var workspace = await CreateWorkspaceFromFileAsync(source, null, null))
            {
                var diagnostics = (await GetDiagnosticsAsync(workspace)).Where(d => d.Id == IDEDiagnosticIds.SimplifyMemberAccessDiagnosticId);
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
            using (var workspace = await CreateWorkspaceFromFileAsync(source, null, null))
            {
                var diagnostics = (await GetDiagnosticsAsync(workspace)).Where(d => d.Id == IDEDiagnosticIds.SimplifyNamesDiagnosticId);
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
            using (var workspace = await CreateWorkspaceFromFileAsync(source, null, null))
            {
                var diagnostics = (await GetDiagnosticsAsync(workspace)).Where(d => d.Id == IDEDiagnosticIds.SimplifyThisOrMeDiagnosticId);
                Assert.Equal(1, diagnostics.Count());
            }
        }

        [WorkItem(1019276, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1019276")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestSimplifyTypeNameDoesNotAddUnnecessaryParens()
        {
            await TestAsync(
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

        [WorkItem(1068445, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1068445")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestSimplifyTypeNameInPropertyLambda()
        {
            await TestAsync(
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

        [WorkItem(1068445, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1068445")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestSimplifyTypeNameInMethodLambda()
        {
            await TestAsync(
@"class C
{
    public string Foo() => ([|System.String|])"";
}", @"class C
{
    public string Foo() => (string)"";
}");
        }

        [WorkItem(1068445, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1068445")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestSimplifyTypeNameInIndexerLambda()
        {
            await TestAsync(
@"class C
{
    public int this[int index] => ([|System.Int32|])0;
}", @"class C
{
    public int this[int index] => (int)0;
}");
        }

        [WorkItem(6682, "https://github.com/dotnet/roslyn/issues/6682")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestThisWithNoType()
        {
            await TestAsync(
@"class Program { dynamic x = 7 ; static void Main ( string [ ] args ) { [|this|] . x = default(dynamic) ; } } ",
@"class Program { dynamic x = 7 ; static void Main ( string [ ] args ) { x = default(dynamic) ; } } ");
        }
    }
}
