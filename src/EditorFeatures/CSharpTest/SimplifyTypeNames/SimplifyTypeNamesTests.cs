// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.SimplifyTypeNames;
using Microsoft.CodeAnalysis.CSharp.SimplifyTypeNames;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SimplifyTypeNames
{
    public partial class SimplifyTypeNamesTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpSimplifyTypeNamesDiagnosticAnalyzer(), new SimplifyTypeNamesCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task SimplifyGenericName()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    static T Goo<T>(T x, T y)
    {
        return default(T);
    }

    static void M()
    {
        var c = [|Goo<int>|](1, 1);
    }
}",
@"using System;

class C
{
    static T Goo<T>(T x, T y)
    {
        return default(T);
    }

    static void M()
    {
        var c = Goo(1, 1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task UseAlias0()
        {
            await TestWithPredefinedTypeOptionsAsync(
@"using Goo = System;

namespace Root
{
    class A
    {
    }

    class B
    {
        public [|Goo::Int32|] a;
    }
}",
@"using Goo = System;

namespace Root
{
    class A
    {
    }

    class B
    {
        public int a;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task UseAlias00()
        {
            await TestInRegularAndScriptAsync(
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
}");
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

            await TestInRegularAndScriptAsync(source,
@"using MyType = System.Exception;

class A
{
    MyType c;
}");

            await TestActionCountAsync(source, 1);
            await TestSpansAsync(
@"using MyType = System.Exception;

class A
{
    [|System.Exception|] c;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task UseAlias1()
        {
            await TestInRegularAndScriptAsync(
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task UseAlias2()
        {
            await TestInRegularAndScriptAsync(
@"using MyType = System.Exception;

namespace Root
{
    class A
    {
        [|System.Exception|] c;
    }
}",
@"using MyType = System.Exception;

namespace Root
{
    class A
    {
        MyType c;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task UseAlias3()
        {
            await TestInRegularAndScriptAsync(
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
}",
@"using MyType = System.Exception;

namespace Root
{
    namespace Nested
    {
        class A
        {
            MyType c;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task UseAlias4()
        {
            await TestInRegularAndScriptAsync(
@"using MyType = System.Exception;

class A
{
    [|System.Exception|] c;
}",
@"using MyType = System.Exception;

class A
{
    MyType c;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task UseAlias5()
        {
            await TestInRegularAndScriptAsync(
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task UseAlias6()
        {
            await TestInRegularAndScriptAsync(
@"using MyType = System.Exception;

namespace Root
{
    class A
    {
        [|System.Exception|] c;
    }
}",
@"using MyType = System.Exception;

namespace Root
{
    class A
    {
        MyType c;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task UseAlias7()
        {
            await TestInRegularAndScriptAsync(
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
}",
@"using MyType = System.Exception;

namespace Root
{
    namespace Nested
    {
        class A
        {
            MyType c;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task UseAlias8()
        {
            await TestInRegularAndScriptAsync(
@"using Goo = System.Int32;

namespace Root
{
    namespace Nested
    {
        class A
        {
            var c = [|System.Int32|].MaxValue;
        }
    }
}",
@"using Goo = System.Int32;

namespace Root
{
    namespace Nested
    {
        class A
        {
            var c = Goo.MaxValue;
        }
    }
}");
        }

        [WorkItem(21449, "https://github.com/dotnet/roslyn/issues/21449")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task DoNotChangeToAliasInNameOfIfItChangesNameOfName()
        {
            await TestInRegularAndScript1Async(
@"using System;
using Foo = SimplifyInsideNameof.Program;

namespace SimplifyInsideNameof
{
  class Program
  {
    static void Main(string[] args)
    {
      Console.WriteLine(nameof([|SimplifyInsideNameof.Program|]));
    }
  }
}",
@"using System;
using Foo = SimplifyInsideNameof.Program;

namespace SimplifyInsideNameof
{
  class Program
  {
    static void Main(string[] args)
    {
      Console.WriteLine(nameof(Program));
    }
  }
}");
        }

        [WorkItem(21449, "https://github.com/dotnet/roslyn/issues/21449")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task DoChangeToAliasInNameOfIfItDoesNotAffectName1()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using Goo = SimplifyInsideNameof.Program;

namespace SimplifyInsideNameof
{
  class Program
  {
    static void Main(string[] args)
    {
      Console.WriteLine(nameof([|SimplifyInsideNameof.Program|].Main));
    }
  }
}",

@"using System;
using Goo = SimplifyInsideNameof.Program;

namespace SimplifyInsideNameof
{
  class Program
  {
    static void Main(string[] args)
    {
      Console.WriteLine(nameof(Goo.Main));
    }
  }
}");
        }

        [WorkItem(21449, "https://github.com/dotnet/roslyn/issues/21449")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task DoChangeToAliasInNameOfIfItDoesNotAffectName2()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using Goo = N.Goo;

namespace N {
    class Goo { }
}

namespace SimplifyInsideNameof
{
  class Program
  {
    static void Main(string[] args)
    {
      Console.WriteLine(nameof([|N.Goo|]));
    }
  }
}",
@"using System;
using Goo = N.Goo;

namespace N {
    class Goo { }
}

namespace SimplifyInsideNameof
{
  class Program
  {
    static void Main(string[] args)
    {
      Console.WriteLine(nameof(Goo));
    }
  }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TwoAliases()
        {
            await TestInRegularAndScriptAsync(
@"using MyType1 = System.Exception;

namespace Root
{
    using MyType2 = Exception;

    class A
    {
        [|System.Exception|] c;
    }
}",
@"using MyType1 = System.Exception;

namespace Root
{
    using MyType2 = Exception;

    class A
    {
        MyType1 c;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TwoAliases2()
        {
            await TestInRegularAndScriptAsync(
@"using MyType1 = System.Exception;

namespace Root
{
    using MyType2 = [|System.Exception|];

    class A
    {
        System.Exception c;
    }
}",
@"using MyType1 = System.Exception;

namespace Root
{
    using MyType2 = MyType1;

    class A
    {
        System.Exception c;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TwoAliasesConflict()
        {
            await TestMissingInRegularAndScriptAsync(
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
            await TestInRegularAndScriptAsync(
@"using MyType = System.Exception;

namespace Root
{
    using MyType = [|System.Exception|];

    class A
    {
        System.Exception c;
    }
}",
@"using MyType = System.Exception;

namespace Root
{
    using MyType = MyType;

    class A
    {
        System.Exception c;
    }
}");
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
            await TestMissingInRegularAndScriptAsync(content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task KeywordInt32()
        {
            var source =
@"class A
{
    [|System.Int32|] i;
}";
            var featureOptions = PreferIntrinsicTypeEverywhere;
            await TestInRegularAndScriptAsync(source,
@"class A
{
    int i;
}", options: featureOptions);
            await TestActionCountAsync(
                source, count: 1, parameters: new TestParameters(options: featureOptions));
            await TestSpansAsync(
@"class A
{
    [|System.Int32|] i;
}", parameters: new TestParameters(options: featureOptions));
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
                var position = content.IndexOf(@"[||]", StringComparison.Ordinal);
                var newContent = content.Replace(@"[||]", pair.Key);
                var expected = content.Replace(@"[||]", pair.Value);
                await TestWithPredefinedTypeOptionsAsync(newContent, expected);
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
            await TestMissingInRegularAndScriptAsync(content);
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

            await TestInRegularAndScriptAsync(source,
@"using System;

namespace Root
{
    class A
    {
        Exception c;
    }
}");
            await TestActionCountAsync(source, 1);
            await TestSpansAsync(
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
            await TestInRegularAndScriptAsync(
@"namespace System
{
    class A
    {
        [|System.Exception|] c;
    }
}",
@"namespace System
{
    class A
    {
        Exception c;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task SimplifyTypeName3()
        {
            await TestInRegularAndScriptAsync(
@"namespace N1
{
    public class A1
    {
    }

    namespace N2
    {
        public class A2
        {
            [|N1.A1|] a;
        }
    }
}",
@"namespace N1
{
    public class A1
    {
    }

    namespace N2
    {
        public class A2
        {
            A1 a;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task SimplifyTypeName4()
        {
            // this is failing since we can't speculatively bind namespace yet
            await TestInRegularAndScriptAsync(
@"namespace N1
{
    namespace N2
    {
        public class A1
        {
        }
    }

    public class A2
    {
        [|N1.N2.A1|] a;
    }
}",
@"namespace N1
{
    namespace N2
    {
        public class A1
        {
        }
    }

    public class A2
    {
        N2.A1 a;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task SimplifyTypeName5()
        {
            await TestInRegularAndScriptAsync(
@"namespace N1
{
    class NC1
    {
        public class A1
        {
        }
    }

    public class A2
    {
        [|N1.NC1.A1|] a;
    }
}",
@"namespace N1
{
    class NC1
    {
        public class A1
        {
        }
    }

    public class A2
    {
        NC1.A1 a;
    }
}");
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
            await TestMissingInRegularAndScriptAsync(content);
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

            await TestInRegularAndScriptAsync(source,
@"namespace N1
{
    namespace N2
    {
        public class A2
        {
            public class A1 { }

            A1 a;
        }
    }
}");

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
            await TestMissingInRegularAndScriptAsync(content);
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

            await TestInRegularAndScriptAsync(source,
@"using System;

namespace N1
{
    public class A1
    {
        EventHandler<EventArgs> a;
    }
}");

            await TestActionCountAsync(source, 1);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/9877"), Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task SimplifyGenericTypeName3()
        {
            await TestInRegularAndScriptAsync(
@"using System;

namespace N1
{
    public class A1
    {
        {|FixAllInDocument:System.Action|}<System.Action<System.Action<System.EventArgs>, System.Action<System.Action<System.EventArgs, System.Action<System.EventArgs>, System.Action<System.Action<System.Action<System.Action<System.EventArgs>, System.Action<System.EventArgs>>>>>>>> a;
    }
}",
@"using System;

namespace N1
{
    public class A1
    {
        Action<Action<Action<EventArgs>, Action<Action<EventArgs, Action<EventArgs>, Action<Action<Action<Action<EventArgs>, Action<EventArgs>>>>>>>> a;
    }
}");
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
            await TestMissingInRegularAndScriptAsync(content);
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

            await TestInRegularAndScriptAsync(source,
@"using MyHandler = System.EventHandler<System.EventArgs>;

namespace N1
{
    public class A1
    {
        System.EventHandler<MyHandler> a;
    }
}");
            await TestActionCountAsync(source, 1);
            await TestSpansAsync(
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
            await TestInRegularAndScriptAsync(
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
}",
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
        MyType a;
    }
}");
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/9877"), Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task SimplifyGenericTypeName7()
        {
            await TestInRegularAndScriptAsync(
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
}",
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
        N2.A1<MyType> a;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task Array1()
        {
            await TestWithPredefinedTypeOptionsAsync(
@"using System.Collections.Generic;

namespace N1
{
    class Test
    {
        [|System.Collections.Generic.List<System.String[]>|] a;
    }
}",
@"using System.Collections.Generic;

namespace N1
{
    class Test
    {
        List<string[]> a;
    }
}");

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
            ////}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task Array2()
        {
            await TestWithPredefinedTypeOptionsAsync(
@"using System.Collections.Generic;

namespace N1
{
    class Test
    {
        [|System.Collections.Generic.List<System.String[][,][,,,]>|] a;
    }
}",
@"using System.Collections.Generic;

namespace N1
{
    class Test
    {
        List<string[][,][,,,]> a;
    }
}");
        }

        [WorkItem(995168, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/995168"), WorkItem(1073099, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1073099")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task SimplifyToPredefinedTypeNameShouldNotBeOfferedInsideNameOf1()
        {
            await TestMissingInRegularAndScriptAsync(
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
            await TestMissingInRegularAndScriptAsync(
@"class Program
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
            await TestMissingInRegularAndScriptAsync(
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
        public async Task SimplifyToPredefinedTypeNameShouldBeOfferedInsideFunctionCalledNameOf()
        {
            await TestInRegularAndScriptAsync(
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
            await TestInRegularAndScriptAsync(
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
            await TestInRegularAndScriptAsync(
@"namespace N1
{
    public class C1
    {
        /// <see cref=""[|System.Int32|]""/>
        public C1()
        {
        }
    }
}",
@"namespace N1
{
    public class C1
    {
        /// <see cref=""int""/>
        public C1()
        {
        }
    }
}", options: PreferIntrinsicTypeEverywhere);
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

            await TestMissingInRegularAndScriptAsync(content);
        }

        [WorkItem(538727, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538727")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task SimplifyAlias2()
        {
            await TestWithPredefinedTypeOptionsAsync(
@"using I64 = System.Int64;
using Goo = System.Collections.Generic.IList<[|System.Int64|]>;

namespace N1
{
    class Test
    {
    }
}",
@"using I64 = System.Int64;
using Goo = System.Collections.Generic.IList<long>;

namespace N1
{
    class Test
    {
    }
}");
        }

        [WorkItem(538727, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538727")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task SimplifyAlias3()
        {
            await TestWithPredefinedTypeOptionsAsync(
@"namespace Outer
{
    using I64 = System.Int64;
    using Goo = System.Collections.Generic.IList<[|System.Int64|]>;

    namespace N1
    {
        class Test
        {
        }
    }
}",
@"namespace Outer
{
    using I64 = System.Int64;
    using Goo = System.Collections.Generic.IList<long>;

    namespace N1
    {
        class Test
        {
        }
    }
}");
        }

        [WorkItem(538727, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538727")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task SimplifyAlias4()
        {
            await TestWithPredefinedTypeOptionsAsync(
@"using I64 = System.Int64;

namespace Outer
{
    using Goo = System.Collections.Generic.IList<[|System.Int64|]>;

    namespace N1
    {
        class Test
        {
        }
    }
}",
@"using I64 = System.Int64;

namespace Outer
{
    using Goo = System.Collections.Generic.IList<long>;

    namespace N1
    {
        class Test
        {
        }
    }
}");
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
            await TestInRegularAndScriptAsync(content, result);
        }

        [WorkItem(919815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/919815")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task SimplifyReturnTypeOnMethodCallToAlias()
        {
            await TestInRegularAndScriptAsync(
@"using alias1 = A;

class A
{
    public [|A|] M()
    {
        return null;
    }
}",
@"using alias1 = A;

class A
{
    public alias1 M()
    {
        return null;
    }
}");
        }

        [WorkItem(538949, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538949")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task SimplifyComplexGeneric1()
        {
            await TestMissingInRegularAndScriptAsync(
@"class A<T>
{
    class B : A<B>
    {
    }

    class C : I<B>, I<[|B.B|]>
    {
    }
}

interface I<T>
{
}");
        }

        [WorkItem(538949, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538949")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task SimplifyComplexGeneric2()
        {
            await TestMissingInRegularAndScriptAsync(
@"class A<T>
{
    class B : A<B>
    {
    }

    class C : I<B>, [|B.B|]
    {
    }
}

interface I<T>
{
}");
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

            await TestMissingInRegularAndScriptAsync(content);
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

            await TestMissingInRegularAndScriptAsync(content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task SimplifyErrorTypeParameter()
        {
            await TestMissingInRegularAndScriptAsync(
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
            await TestMissingInRegularAndScriptAsync(
@"class A<T>
{
    class D : A<T[]>
    {
    }

    class B
    {
    }

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
            await TestMissingInRegularAndScriptAsync(
@"class A<T>
{
    class D : A<T[]>
    {
    }

    class B
    {
    }

    class C<T>
    {
        D.B x = new [|D.B|]();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestGlobalAlias()
        {
            await TestWithPredefinedTypeOptionsAsync(
@"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        [|global::System|].String s;
    }
}",
@"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        string s;
    }
}");
        }

        [WorkItem(541748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestOnErrorInScript()
        {
            await TestMissingAsync(
@"[|Console.WrieLine();|]",
new TestParameters(Options.Script));
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/9877"), Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestConflicts()
        {
            await TestInRegularAndScriptAsync(
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
index: 1);
        }

        [WorkItem(542100, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542100")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestPreventSimplificationThatWouldCauseConflict()
        {
            await TestInRegularAndScriptAsync(
@"namespace N
{
    class Program
    {
        class Goo
        {
            public static void Bar()
            {
            }
        }

        static void Main()
        {
            [|N.Program.Goo.Bar|]();
            {
                int Goo;
            }
        }
    }
}",
@"namespace N
{
    class Program
    {
        class Goo
        {
            public static void Bar()
            {
            }
        }

        static void Main()
        {
            Program.Goo.Bar();
            {
                int Goo;
            }
        }
    }
}");

            await TestMissingInRegularAndScriptAsync(
@"namespace N
{
    class Program
    {
        class Goo
        {
            public static void Bar()
            {
            }
        }

        static void Main()
        {
            [|Program.Goo.Bar|]();
            {
                int Goo;
            }
        }
    }
}");
        }

        [WorkItem(541929, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541929")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestOnOpenType1()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Program<T>
{
    public class Inner
    {
        [Bar(typeof([|Program<>.Inner|]))]
        void Goo()
        {
        }
    }
}");
        }

        [WorkItem(541929, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541929")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestOnOpenType2()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    public class Inner<T>
    {
        [Bar(typeof([|Program.Inner<>|]))]
        void Goo()
        {
        }
    }
}",
@"class Program
{
    public class Inner<T>
    {
        [Bar(typeof(Inner<>))]
        void Goo()
        {
        }
    }
}");
        }

        [WorkItem(541929, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541929")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestOnOpenType3()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Program<X>
{
    public class Inner<Y>
    {
        [Bar(typeof([|Program<>.Inner<>|]))]
        void Goo()
        {
        }
    }
}");
        }

        [WorkItem(541929, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541929")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestOnOpenType4()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Program<X>
{
    public class Inner<Y>
    {
        [Bar(typeof([|Program<X>.Inner<>|]))]
        void Goo()
        {
        }
    }
}");
        }

        [WorkItem(541929, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541929")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestOnOpenType5()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Program<X>
{
    public class Inner<Y>
    {
        [Bar(typeof([|Program<>.Inner<Y>|]))]
        void Goo()
        {
        }
    }
}");
        }

        [WorkItem(541929, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541929")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestOnOpenType6()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Program<X>
{
    public class Inner<Y>
    {
        [Bar(typeof([|Program<Y>.Inner<X>|]))]
        void Goo()
        {
        }
    }
}");
        }

        [WorkItem(541929, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541929")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestOnNonOpenType1()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    public class Inner
    {
        [Bar(typeof([|Program.Inner|]))]
        void Goo()
        {
        }
    }
}",
@"class Program
{
    public class Inner
    {
        [Bar(typeof(Inner))]
        void Goo()
        {
        }
    }
}");
        }

        [WorkItem(541929, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541929")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestOnNonOpenType2()
        {
            await TestInRegularAndScriptAsync(
@"class Program<T>
{
    public class Inner
    {
        [Bar(typeof([|Program<T>.Inner|]))]
        void Goo()
        {
        }
    }
}",
@"class Program<T>
{
    public class Inner
    {
        [Bar(typeof(Inner))]
        void Goo()
        {
        }
    }
}");
        }

        [WorkItem(541929, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541929")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestOnNonOpenType3()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    public class Inner<T>
    {
        [Bar(typeof([|Program.Inner<>|]))]
        void Goo()
        {
        }
    }
}",
@"class Program
{
    public class Inner<T>
    {
        [Bar(typeof(Inner<>))]
        void Goo()
        {
        }
    }
}");
        }

        [WorkItem(541929, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541929")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestOnNonOpenType4()
        {
            await TestInRegularAndScriptAsync(
@"class Program<X>
{
    public class Inner<Y>
    {
        [Bar(typeof([|Program<X>.Inner<Y>|]))]
        void Goo()
        {
        }
    }
}",
@"class Program<X>
{
    public class Inner<Y>
    {
        [Bar(typeof(Inner<Y>))]
        void Goo()
        {
        }
    }
}");
        }

        [WorkItem(541929, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541929")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestOnNonOpenType5()
        {
            await TestInRegularAndScriptAsync(
@"class Program<X>
{
    public class Inner<Y>
    {
        [Bar(typeof([|Program<X>.Inner<X>|]))]
        void Goo()
        {
        }
    }
}",
@"class Program<X>
{
    public class Inner<Y>
    {
        [Bar(typeof(Inner<X>))]
        void Goo()
        {
        }
    }
}");
        }

        [WorkItem(541929, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541929")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestOnNonOpenType6()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Program<X>
{
    public class Inner<Y>
    {
        [Bar(typeof([|Program<Y>.Inner<Y>|]))]
        void Goo()
        {
        }
    }
}");
        }

        [WorkItem(542650, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542650")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestWithInterleavedDirective1()
        {
            await TestMissingInRegularAndScriptAsync(
@"#if true
class A
#else
class B
#endif
{
    class C
    {
    }

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
            await TestMissingInRegularAndScriptAsync(
@"class Program
{
    class System
    {
    }

    int Console = 7;

    void Main()
    {
        string v = null;
        [|global::System.Console.WriteLine(v)|];
    }
}");
        }

        [WorkItem(544615, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544615")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestMissingOnAmbiguousCast()
        {
            await TestMissingInRegularAndScriptAsync(
@"enum E
{
}

class C
{
    void Main()
    {
        var x = ([|global::E|])-1;
    }
}");
        }

        [WorkItem(544616, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544616")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task ParenthesizeIfParseChanges()
        {
            await TestInRegularAndScriptAsync(
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
}");
        }

        [WorkItem(544974, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544974")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestNullableSimplification1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    static void Main()
    {
        [|System.Nullable<int>.Equals|](1, 1);
    }
}",
@"class C
{
    static void Main()
    {
        Equals(1, 1);
    }
}");
        }

        [WorkItem(544974, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544974")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestNullableSimplification3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    static void Main([|System.Nullable<int>|] i)
    {
    }
}",
@"class C
{
    static void Main(int? i)
    {
    }
}");
        }

        [WorkItem(544974, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544974")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestNullableSimplification4()
        {
            await TestWithPredefinedTypeOptionsAsync(
@"class C
{
    static void Main([|System.Nullable<System.Int32>|] i)
    {
    }
}",
@"class C
{
    static void Main(int? i)
    {
    }
}");
        }

        [WorkItem(544977, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544977")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestNullableSimplification5()
        {
            await TestInRegularAndScriptAsync(
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
}");
        }

        [WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestMissingNullableSimplificationInsideCref()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;
/// <summary>
/// <see cref=""[|Nullable{T}|]""/>
/// </summary>
class A
{
}");
        }

        [WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestMissingNullableSimplificationInsideCref2()
        {
            await TestMissingInRegularAndScriptAsync(
@"/// <summary>
/// <see cref=""[|System.Nullable{T}|]""/>
/// </summary>
class A
{
}");
        }

        [WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestMissingNullableSimplificationInsideCref3()
        {
            await TestMissingInRegularAndScriptAsync(
@"/// <summary>
/// <see cref=""[|System.Nullable{T}|].Value""/>
/// </summary>
class A
{
}");
        }

        [WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestNullableInsideCref_AllowedIfReferencingActualTypeParameter()
        {
            await TestInRegularAndScriptAsync(
@"using System;
/// <summary>
/// <see cref=""C{[|Nullable{T}|]}""/>
/// </summary>
class C<T>
{
}",
@"using System;
/// <summary>
/// <see cref=""C{T?}""/>
/// </summary>
class C<T>
{
}");
        }

        [WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestMissingNullableSimplificationInsideCref5()
        {
            await TestMissingInRegularAndScriptAsync(
@"/// <summary>
/// <see cref=""A.M{[|Nullable{T}|]}()""/>
/// </summary>
class A
{
    public void M<U>() where U : struct
    {
    }
}");
        }

        [WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestNullableInsideCref_AllowedIfReferencingActualType()
        {
            await TestInRegularAndScriptAsync(
@"using System;
/// <summary>
/// <see cref=""[|Nullable{int}|]""/>
/// </summary>
class A
{
}",
@"using System;
/// <summary>
/// <see cref=""int?""/>
/// </summary>
class A
{
}");
        }

        [WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestNullableInsideCref_AllowedIfReferencingActualType_AsTypeArgument()
        {
            await TestInRegularAndScriptAsync(
@"using System;
/// <summary>
/// <see cref=""C{[|Nullable{int}|]}""/>
/// </summary>
class C<T>
{
}",
@"using System;
/// <summary>
/// <see cref=""C{int?}""/>
/// </summary>
class C<T>
{
}");
        }

        [WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestMissingNullableSimplificationInsideCref8()
        {
            await TestMissingInRegularAndScriptAsync(
@"/// <summary>
/// <see cref=""A.M{[|Nullable{int}|]}()""/>
/// </summary>
class A
{
    public void M<U>() where U : struct
    {
    }
}");
        }

        [WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestNullableSimplificationInsideCref()
        {
            await TestInRegularAndScriptAsync(
@"/// <summary>
/// <see cref=""A.M([|System.Nullable{A}|])""/>
/// </summary>
struct A
{
    public void M(A? x)
    {
    }
}",
@"/// <summary>
/// <see cref=""A.M(A?)""/>
/// </summary>
struct A
{
    public void M(A? x)
    {
    }
}");
        }

        [WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestNullableSimplificationInsideCref2()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;
/// <summary>
/// <see cref=""A.M(List{[|Nullable{int}|]})""/>
/// </summary>
class A
{
    public void M(List<int?> x)
    {
    }
}",
@"using System;
using System.Collections.Generic;
/// <summary>
/// <see cref=""A.M(List{int?})""/>
/// </summary>
class A
{
    public void M(List<int?> x)
    {
    }
}");
        }

        [WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestNullableSimplificationInsideCref3()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;
/// <summary>
/// <see cref=""A.M{U}(List{[|Nullable{U}|]})""/>
/// </summary>
class A
{
    public void M<U>(List<U?> x) where U : struct
    {
    }
}",
@"using System;
using System.Collections.Generic;
/// <summary>
/// <see cref=""A.M{U}(List{U?})""/>
/// </summary>
class A
{
    public void M<U>(List<U?> x) where U : struct
    {
    }
}");
        }

        [WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestNullableSimplificationInsideCref4()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;
/// <summary>
/// <see cref=""A.M{T}(List{Nullable{T}}, [|Nullable{T}|])""/>
/// </summary>
class A
{
    public void M<U>(List<U?> x, U? y) where U : struct
    {
    }
}",
@"using System;
using System.Collections.Generic;
/// <summary>
/// <see cref=""A.M{T}(List{Nullable{T}}, T?)""/>
/// </summary>
class A
{
    public void M<U>(List<U?> x, U? y) where U : struct
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestColorColorCase1()
        {
            await TestInRegularAndScriptAsync(
@"using N;

namespace N
{
    class Color
    {
        public static void Goo()
        {
        }

        public void Bar()
        {
        }
    }
}

class Program
{
    Color Color;

    void Main()
    {
        [|N.Color|].Goo();
    }
}",
@"using N;

namespace N
{
    class Color
    {
        public static void Goo()
        {
        }

        public void Bar()
        {
        }
    }
}

class Program
{
    Color Color;

    void Main()
    {
        Color.Goo();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestColorColorCase2()
        {
            await TestMissingInRegularAndScriptAsync(
@"using N;

namespace N
{
    class Color
    {
        public static void Goo()
        {
        }

        public void Bar()
        {
        }
    }
}

class Program
{
    Color Color;

    void Main()
    {
        [|Color.Goo|]();
    }
}");
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
@"class Program
{
    static void Main()
    {
        Program a = null; 
    }
}", parseOptions: null);

            await TestMissingAsync(source, new TestParameters(GetScriptOptions()));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestSimplifyExpression()
        {
            await TestInRegularAndScriptAsync(
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
}");
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
            await TestInRegularAndScriptAsync(source,
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
}");
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
            await TestInRegularAndScriptAsync(source,
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
}");
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

            await TestMissingInRegularAndScriptAsync(markup);
        }

        [WorkItem(566749, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/566749")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestMethodGroups1()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

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
            await TestMissingInRegularAndScriptAsync(
@"using System;

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
            await TestInRegularAndScriptAsync(
@"using System;

class Program
{
    static void Main()
    {
        Action a = [|System.Console.WriteLine|];
    }
}",
@"using System;

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
            await TestInRegularAndScriptAsync(
@"using goo = A.B;
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
@"using goo = A.B;
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
            await TestMissingInRegularAndScriptAsync(
@"using System.Collections.Generic;
using System.Linq;

namespace NSA
{
    class DuplicateClassName
    {
    }
}

namespace NSB
{
    class DuplicateClassName
    {
    }
}

namespace Test
{
    using AliasA = NSA.DuplicateClassName;
    using AliasB = NSB.DuplicateClassName;

    class TestClass
    {
        static void Main(string[] args)
        {
            var localA = new NSA.DuplicateClassName();
            var localB = new NSB.DuplicateClassName();
            new List<NoAlias.Goo>().Where(m => [|m.InnocentProperty|] == null);
        }
    }
}

namespace NoAlias
{
    class Goo
    {
        public NSB.DuplicateClassName InnocentProperty { get; set; }
    }
}");
        }

        [WorkItem(577169, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/577169")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task SuitablyReplaceNullables1()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

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
            await TestMissingInRegularAndScriptAsync(
@"using System;

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
            await TestMissingInRegularAndScriptAsync(
@"using System;

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
}");
        }

        [WorkItem(608190, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/608190")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task Bugfix_608190_1()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

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
}");
        }

        [WorkItem(608932, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/608932")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task Bugfix_608932()
        {
            await TestMissingInRegularAndScriptAsync(
@"using S = X;

class Program
{
    static void Main(string[] args)
    {
    }
}

namespace X
{
    using S = System;

    enum E
    {
    }

    class C<E>
    {
        [|X|].E e; // Simplify type name as suggested
    }
}");
        }

        [WorkItem(635933, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/635933")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task Bugfix_635933()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class B
{
    public static void Goo(int x, object y)
    {
    }

    static void Main()
    {
        C<string>.D.Goo(0);
    }
}

class C<T> : B
{
    public class D : C<T> // Start rename session and try to rename D to T
    {
        public static void Goo(dynamic x)
        {
            Console.WriteLine([|D.Goo(x, "")|]);
        }
    }

    public static string Goo(int x, T y)
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
            await TestSpansAsync(@"
using goo = System.Console;
class Program
{
    static void Main(string[] args)
    {
        [|System.Console|].Read();
    }
}
");
        }

        [WorkItem(579172, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/579172")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task Bugfix_579172()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C<T, S>
{
    class D : C<[|D.D|], D.D.D>
    {
    }
}");
        }

        [WorkItem(633182, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/633182")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task Bugfix_633182()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void Goo()
    {
        ([|this.Goo|])();
    }
}");
        }

        [WorkItem(627102, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627102")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task Bugfix_627102()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class B
{
    static void Goo(int x, object y)
    {
    }

    static void Goo<T>(dynamic x)
    {
        Console.WriteLine([|C<T>.Goo|](x, ""));
    }

    static void Main()
    {
        Goo<string>(0);
    }
}

class C<T> : B
{
    public static string Goo(int x, T y)
    {
        return ""Hello world"";
    }
}");
        }

        [WorkItem(629572, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/629572")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task DoNotIncludeAliasNameIfLastTargetNameIsTheSame_1()
        {
            await TestSpansAsync(@"
using Generic = System.Collections.Generic;
class Program
{
    static void Main(string[] args)
    {
        var x = new [|System.Collections|].Generic.List<int>();
    }
}
");

            await TestInRegularAndScriptAsync(
@"
using Generic = System.Collections.Generic;
class Program
{
    static void Main(string[] args)
    {
        var x = new [|System.Collections|].Generic.List<int>();
    }
}
",
@"
using Generic = System.Collections.Generic;
class Program
{
    static void Main(string[] args)
    {
        var x = new Generic.List<int>();
    }
}
");
        }

        [WorkItem(629572, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/629572")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task DoNotIncludeAliasNameIfLastTargetNameIsTheSame_2()
        {
            await TestSpansAsync(@"
using Console = System.Console;
class Program
{
    static void Main(string[] args)
    {
        [|System|].Console.WriteLine(""goo"");
    }
}
");

            await TestInRegularAndScriptAsync(
@"
using Console = System.Console;
class Program
{
    static void Main(string[] args)
    {
        [|System|].Console.WriteLine(""goo"");
    }
}
",
@"
using Console = System.Console;
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine(""goo"");
    }
}
");
        }

        [WorkItem(736377, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/736377")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task DontSimplifyTypeNameBrokenCode()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;

class Program
{
    public static void GetA

    [[|System.Diagnostics|].CodeAnalysis.SuppressMessage(""Microsoft.Design"", ""CA1024:UsePropertiesWhereAppropriate"")]
    public static ISet<string> GetAllFilesInSolution()
    {
        return null;
    }
}");
        }

        [WorkItem(813385, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/813385")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task DontSimplifyAliases()
        {
            await TestMissingInRegularAndScriptAsync(
@"using Goo = System.Int32;

class C
{
    [|Goo|] f;
}");
        }

        [WorkItem(825541, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/825541")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task ShowOnlyRelevantSpanForReductionOfGenericName()
        {
            await TestSpansAsync(@"
namespace A
{
    class Program
    {
        static void Main(string[] args)
        {
            var x = A.B.OtherClass.Test[|<int>|](5);
        }
    }
 
    namespace B
    {
        class OtherClass
        {
            public static int Test<T>(T t) { return 5; }
        }
    }
}");
        }

        [WorkItem(878773, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/878773")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task DontSimplifyAttributeNameWithJustAttribute()
        {
            await TestMissingInRegularAndScriptAsync(
@"[[|Attribute|]]
class Attribute : System.Attribute
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task ThisQualificationOnFieldOption()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    int x;

    public void z()
    {
        [|this|].x = 4;
    }
}", new TestParameters(options: Option(CodeStyleOptions.QualifyFieldAccess, true, NotificationOption.Error)));
        }

        [WorkItem(942568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestIntrinsicTypesInLocalDeclarationDefaultValue1()
        {
            await TestWithPredefinedTypeOptionsAsync(
@"class C
{
    [|System.Int32|] x;

    public void z()
    {
    }
}",
@"class C
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
            await TestInRegularAndScriptAsync(
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
}", options: PreferIntrinsicTypeEverywhere);
        }

        [WorkItem(942568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestIntrinsicTypesInsideCref_Default_1()
        {
            await TestInRegularAndScriptAsync(
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
}", options: PreferIntrinsicTypeInMemberAccess);
        }

        [WorkItem(942568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestIntrinsicTypesInsideCref_Default_2()
        {
            await TestInRegularAndScriptAsync(
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
}", options: PreferIntrinsicTypeEverywhere);
        }

        [WorkItem(942568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestIntrinsicTypesInsideCref_Default_3()
        {
            await TestInRegularAndScriptAsync(
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
}", options: PreferIntrinsicTypeEverywhere);
        }

        [WorkItem(942568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")]
        [WorkItem(954536, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/954536")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestIntrinsicTypesInsideCref_NonDefault_1()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    /// <see cref=""[|Int32|]""/>
    public void z()
    {
    }
}", new TestParameters(options: Option(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, false, NotificationOption.Error)));
        }

        [WorkItem(942568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")]
        [WorkItem(954536, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/954536")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestIntrinsicTypesInsideCref_NonDefault_2()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    /// <see cref=""[|Int32|]""/>
    public void z()
    {
    }
}",
@"using System;

class C
{
    /// <see cref=""int""/>
    public void z()
    {
    }
}", options: PreferIntrinsicTypeInMemberAccess);
        }

        [WorkItem(942568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")]
        [WorkItem(954536, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/954536")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestIntrinsicTypesInsideCref_NonDefault_3()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    /// <see cref=""[|Int32|].MaxValue""/>
    public void z()
    {
    }
}", new TestParameters(options: Option(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, false, NotificationOption.Error)));
        }

        [WorkItem(954536, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/954536")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestIntrinsicTypesInsideCref_NonDefault_4()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    /// <see cref=""[|Int32|].MaxValue""/>
    public void z()
    {
    }
}",
@"using System;

class C
{
    /// <see cref=""int.MaxValue""/>
    public void z()
    {
    }
}",
options: PreferIntrinsicTypeInMemberAccess);
        }

        [WorkItem(954536, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/954536")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestIntrinsicTypesInsideCref_NonDefault_5()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    /// <see cref=""System.Collections.Generic.List{T}.CopyTo([|System.Int32|], T[], int, int)""/>
    public void z()
    {
    }
}", new TestParameters(options: Option(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, false, NotificationOption.Error)));
        }

        [WorkItem(954536, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/954536")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestIntrinsicTypesInsideCref_NonDefault_6()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    /// <see cref=""System.Collections.Generic.List{T}.CopyTo([|System.Int32|], T[], int, int)""/>
    public void z()
    {
    }
}",
@"class C
{
    /// <see cref=""System.Collections.Generic.List{T}.CopyTo(int, T[], int, int)""/>
    public void z()
    {
    }
}",
options: PreferIntrinsicTypeInMemberAccess);
        }

        [WorkItem(942568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestIntrinsicTypesInLocalDeclarationNonDefaultValue_1()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    [|System.Int32|] x;

    public void z(System.Int32 y)
    {
        System.Int32 z = 9;
    }
}", new TestParameters(options: Option(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, false, NotificationOption.Error)));
        }

        [WorkItem(942568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestIntrinsicTypesInLocalDeclarationNonDefaultValue_2()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    System.Int32 x;

    public void z([|System.Int32|] y)
    {
        System.Int32 z = 9;
    }
}", new TestParameters(options: Option(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, false, NotificationOption.Error)));
        }

        [WorkItem(942568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestIntrinsicTypesInLocalDeclarationNonDefaultValue_3()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    System.Int32 x;

    public void z(System.Int32 y)
    {
        [|System.Int32|] z = 9;
    }
}", new TestParameters(options: Option(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, false, NotificationOption.Error)));
        }

        [WorkItem(942568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestIntrinsicTypesInMemberAccess_Default_1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public void z()
    {
        var sss = [|System.Int32|].MaxValue;
    }
}",
@"class C
{
    public void z()
    {
        var sss = int.MaxValue;
    }
}", options: PreferIntrinsicTypeInMemberAccess);
        }

        [WorkItem(942568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestIntrinsicTypesInMemberAccess_Default_2()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    public void z()
    {
        var sss = [|Int32|].MaxValue;
    }
}",
@"using System;

class C
{
    public void z()
    {
        var sss = int.MaxValue;
    }
}", options: PreferIntrinsicTypeInMemberAccess);
        }

        [WorkItem(956667, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/956667")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestIntrinsicTypesInMemberAccess_Default_3()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

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
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    public void z()
    {
        var sss = [|Int32|].MaxValue;
    }
}", new TestParameters(options: Option(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, false, NotificationOption.Error)));
        }

        [WorkItem(942568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestIntrinsicTypesInMemberAccess_NonDefault_2()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    public void z()
    {
        var sss = [|System.Int32|].MaxValue;
    }
}", new TestParameters(options: Option(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, false, NotificationOption.Error)));
        }

        [WorkItem(965208, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/965208")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestSimplifyDiagnosticId()
        {
            await TestInRegularAndScriptAsync(
@"
using System;

class C
{
    public void z()
    {
        [|System.Console.WriteLine|]("");
    }
}",
@"
using System;

class C
{
    public void z()
    {
        Console.WriteLine("");
    }
}");

            await TestInRegularAndScript1Async(
@"
using System;

class C
{
    public void z()
    {
        [|System.Int32|] a;
    }
}",
@"
using System;

class C
{
    public void z()
    {
        int a;
    }
}", parameters: new TestParameters(options: PreferIntrinsicTypeEverywhere));
        }

        [WorkItem(1019276, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1019276")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestSimplifyTypeNameDoesNotAddUnnecessaryParens()
        {
            await TestWithPredefinedTypeOptionsAsync(
@"using System;

class Program
{
    static void F()
    {
        object o = null;
        if (![|(o is Byte)|])
        {
        }
    }
}",
@"using System;

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
            await TestWithPredefinedTypeOptionsAsync(
@"namespace ClassLibrary2
{
    public class Class1
    {
        public object X => ([|System.Int32|])0;
    }
}",
@"namespace ClassLibrary2
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
            await TestWithPredefinedTypeOptionsAsync(
@"class C
{
    public string Goo() => ([|System.String|])"";
}",
@"class C
{
    public string Goo() => (string)"";
}");
        }

        [WorkItem(1068445, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1068445")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestSimplifyTypeNameInIndexerLambda()
        {
            await TestWithPredefinedTypeOptionsAsync(
@"class C
{
    public int this[int index] => ([|System.Int32|])0;
}",
@"class C
{
    public int this[int index] => (int)0;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        [WorkItem(388744, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=388744")]
        public async Task SimplifyTypeNameWithOutDiscard()
        {
            await TestAsync(
@"class C
{
    static void F()
    {
        [|C.G|](out _);
    }
    static void G(out object o)
    {
        o = null;
    }
}",
@"class C
{
    static void F()
    {
        G(out _);
    }
    static void G(out object o)
    {
        o = null;
    }
}",
                parseOptions: CSharpParseOptions.Default);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        [WorkItem(388744, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=388744")]
        public async Task SimplifyTypeNameWithOutDiscard_FeatureDisabled()
        {
            await TestAsync(
@"class C
{
    static void F()
    {
        [|C.G|](out _);
    }
    static void G(out object o)
    {
        o = null;
    }
}",
@"class C
{
    static void F()
    {
        G(out _);
    }
    static void G(out object o)
    {
        o = null;
    }
}",
                parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        [WorkItem(15996, "https://github.com/dotnet/roslyn/issues/15996")]
        public async Task TestMemberOfBuiltInType1()
        {
            await TestAsync(
@"using System;
class C
{
    void Main()
    {
        [|UInt32|] value = UInt32.MaxValue;
    }
}",
@"using System;
class C
{
    void Main()
    {
        uint value = UInt32.MaxValue;
    }
}",
                parseOptions: CSharpParseOptions.Default,
                options: PreferIntrinsicTypeInDeclaration);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        [WorkItem(15996, "https://github.com/dotnet/roslyn/issues/15996")]
        public async Task TestMemberOfBuiltInType2()
        {
            await TestAsync(
@"using System;
class C
{
    void Main()
    {
        UInt32 value = [|UInt32|].MaxValue;
    }
}",
@"using System;
class C
{
    void Main()
    {
        UInt32 value = uint.MaxValue;
    }
}",
                parseOptions: CSharpParseOptions.Default,
                options: PreferIntrinsicTypeInMemberAccess);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        [WorkItem(15996, "https://github.com/dotnet/roslyn/issues/15996")]
        public async Task TestMemberOfBuiltInType3()
        {
            await TestAsync(
@"using System;
class C
{
    void Main()
    {
        [|UInt32|].Parse(""goo"");
    }
}",
@"using System;
class C
{
    void Main()
    {
        uint.Parse(""goo"");
    }
}",
                parseOptions: CSharpParseOptions.Default,
                options: PreferIntrinsicTypeInMemberAccess);
        }

        [Fact, WorkItem(26923, "https://github.com/dotnet/roslyn/issues/26923")]
        public async Task NoSuggestionOnForeachCollectionExpression()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;

class C
{
    static void Main(string[] args)
    {
        foreach (string arg in [|args|])
        {

        }
    }
}", new TestParameters(options: PreferImplicitTypeWithSilent));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        [WorkItem(20377, "https://github.com/dotnet/roslyn/issues/20377")]
        public async Task TestWarningLevel(int warningLevel)
        {
            await TestInRegularAndScriptAsync(
@"using System;

namespace Root
{
    class A
    {
        [|System.Exception|] c;
    }
}",
@"using System;

namespace Root
{
    class A
    {
        Exception c;
    }
}", compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, warningLevel: warningLevel));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestGlobalAliasSimplifiesInUsingDirective()
        {
            await TestInRegularAndScriptAsync(
                "using [|global::System.IO|];",
                "using System.IO;");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        [InlineData("Boolean")]
        [InlineData("Char")]
        [InlineData("String")]
        [InlineData("Int8")]
        [InlineData("UInt8")]
        [InlineData("Int16")]
        [InlineData("UInt16")]
        [InlineData("Int32")]
        [InlineData("UInt32")]
        [InlineData("Int64")]
        [InlineData("UInt64")]
        [InlineData("Float32")]
        [InlineData("Float64")]
        public async Task TestGlobalAliasSimplifiesInUsingAliasDirective(string typeName)
        {
            await TestInRegularAndScriptAsync(
                $"using My{typeName} = [|global::System.{typeName}|];",
                $"using My{typeName} = System.{typeName};");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestGlobalAliasSimplifiesInUsingStaticDirective()
        {
            await TestInRegularAndScriptAsync(
                "using static [|global::System.Math|];",
                "using static System.Math;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestGlobalAliasSimplifiesInUsingDirectiveInNamespace()
        {
            await TestInRegularAndScriptAsync(
@"using System;
namespace N
{
    using [|global::System.IO|];
}",
@"using System;
namespace N
{
    using System.IO;
}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        [InlineData("Boolean")]
        [InlineData("Char")]
        [InlineData("String")]
        [InlineData("Int8")]
        [InlineData("UInt8")]
        [InlineData("Int16")]
        [InlineData("UInt16")]
        [InlineData("Int32")]
        [InlineData("UInt32")]
        [InlineData("Int64")]
        [InlineData("UInt64")]
        [InlineData("Float32")]
        [InlineData("Float64")]
        public async Task TestGlobalAliasSimplifiesInUsingAliasDirectiveWithinNamespace(string typeName)
        {
            await TestInRegularAndScriptAsync(
$@"using System;
namespace N
{{
    using My{typeName} = [|global::System.{typeName}|];
}}",
$@"using System;
namespace N
{{
    using My{typeName} = System.{typeName};
}}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        public async Task TestGlobalAliasSimplifiesInUsingStaticDirectiveInNamespace()
        {
            await TestInRegularAndScriptAsync(
@"using System;
namespace N
{
    using static [|global::System.Math|];
}",
@"using System;
namespace N
{
    using static System.Math;
}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        [InlineData("Boolean")]
        [InlineData("Char")]
        [InlineData("String")]
        [InlineData("Int8")]
        [InlineData("UInt8")]
        [InlineData("Int16")]
        [InlineData("UInt16")]
        [InlineData("Int32")]
        [InlineData("UInt32")]
        [InlineData("Int64")]
        [InlineData("UInt64")]
        [InlineData("Float32")]
        [InlineData("Float64")]
        public async Task TestDoesNotSimplifyUsingAliasDirectiveToPrimitiveType(string typeName)
        {
            await TestMissingAsync(
$@"using System;
namespace N
{{
    using My{typeName} = [|{typeName}|];
}}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        [InlineData("Boolean")]
        [InlineData("Char")]
        [InlineData("String")]
        [InlineData("Int8")]
        [InlineData("UInt8")]
        [InlineData("Int16")]
        [InlineData("UInt16")]
        [InlineData("Int32")]
        [InlineData("UInt32")]
        [InlineData("Int64")]
        [InlineData("UInt64")]
        [InlineData("Float32")]
        [InlineData("Float64")]
        public async Task TestDoesNotSimplifyUsingAliasDirectiveToPrimitiveType2(string typeName)
        {
            await TestMissingAsync(
$@"using System;
namespace N
{{
    using My{typeName} = [|System.{typeName}|];
}}");
        }

        private async Task TestWithPredefinedTypeOptionsAsync(string code, string expected, int index = 0)
        {
            await TestInRegularAndScriptAsync(code, expected, index: index, options: PreferIntrinsicTypeEverywhere);
        }

        private IDictionary<OptionKey, object> PreferIntrinsicTypeEverywhere => OptionsSet(
            SingleOption(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, true, NotificationOption.Error),
            SingleOption(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, this.onWithError, GetLanguage()));

        private IDictionary<OptionKey, object> PreferIntrinsicTypeEverywhereAsWarning => OptionsSet(
            SingleOption(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, true, NotificationOption.Warning),
            SingleOption(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, this.onWithWarning, GetLanguage()));

        private IDictionary<OptionKey, object> PreferIntrinsicTypeInDeclaration => OptionsSet(
            SingleOption(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, true, NotificationOption.Error),
            SingleOption(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, this.offWithSilent, GetLanguage()));

        private IDictionary<OptionKey, object> PreferIntrinsicTypeInMemberAccess => OptionsSet(
            SingleOption(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, true, NotificationOption.Error),
            SingleOption(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, this.offWithSilent, GetLanguage()));

        private IDictionary<OptionKey, object> PreferImplicitTypeWithSilent => OptionsSet(
            SingleOption(CSharpCodeStyleOptions.VarElsewhere, onWithSilent),
            SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithSilent),
            SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, onWithSilent));

        private readonly CodeStyleOption<bool> onWithSilent = new CodeStyleOption<bool>(true, NotificationOption.Silent);
        private readonly CodeStyleOption<bool> offWithSilent = new CodeStyleOption<bool>(false, NotificationOption.Silent);
        private readonly CodeStyleOption<bool> onWithInfo = new CodeStyleOption<bool>(true, NotificationOption.Suggestion);
        private readonly CodeStyleOption<bool> offWithInfo = new CodeStyleOption<bool>(false, NotificationOption.Suggestion);
        private readonly CodeStyleOption<bool> onWithWarning = new CodeStyleOption<bool>(true, NotificationOption.Warning);
        private readonly CodeStyleOption<bool> offWithWarning = new CodeStyleOption<bool>(false, NotificationOption.Warning);
        private readonly CodeStyleOption<bool> onWithError = new CodeStyleOption<bool>(true, NotificationOption.Error);
        private readonly CodeStyleOption<bool> offWithError = new CodeStyleOption<bool>(false, NotificationOption.Error);
    }
}
