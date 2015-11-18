// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.Iterator;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.Iterator
{
    public class ChangeToIEnumerableTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return new Tuple<DiagnosticAnalyzer, CodeFixProvider>(null, new CSharpChangeToIEnumerableCodeFixProvider());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsChangeToIEnumerable)]
        public async Task TestChangeToIEnumerableObjectMethod()
        {
            var initial =
@"using System;
using System.Collections.Generic;

class Program
{
    static object [|M|]()
    {
        yield return 0;
    }
}";

            var expected =
@"using System;
using System.Collections.Generic;

class Program
{
    static IEnumerable<object> M()
    {
        yield return 0;
    }
}";
            await TestAsync(initial, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsChangeToIEnumerable)]
        public async Task TestChangeToIEnumerableTupleMethod()
        {
            var initial =
@"using System;
using System.Collections.Generic;

class Program
{
    static Tuple<int> [|M|]()
    {
        yield return 0;
    }
}";

            var expected =
@"using System;
using System.Collections.Generic;

class Program
{
    static IEnumerable<Tuple<int>> M()
    {
        yield return 0;
    }
}";
            await TestAsync(initial, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsChangeToIEnumerable)]
        public async Task TestChangeToIEnumerableListMethod()
        {
            var initial =
@"using System;
using System.Collections.Generic;

class Program
{
    static IList<int> [|M|]()
    {
        yield return 0;
    }
}";

            var expected =
@"using System;
using System.Collections.Generic;

class Program
{
    static IEnumerable<IList<int>> M()
    {
        yield return 0;
    }
}";
            await TestAsync(initial, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsChangeToIEnumerable)]
        public async Task TestChangeToIEnumerableGenericIEnumerableMethod()
        {
            var initial =
@"using System;
using System.Collections.Generic;

class Program
{
    static IEnumerable<int> [|M|]()
    {
        yield return 0;
    }
}";
            await TestMissingAsync(initial);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsChangeToIEnumerable)]
        public async Task TestChangeToIEnumerableGenericIEnumeratorMethod()
        {
            var initial =
@"using System;
using System.Collections.Generic;

class Program
{
    static IEnumerator<int> [|M|]()
    {
        yield return 0;
    }
}";
            await TestMissingAsync(initial);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsChangeToIEnumerable)]
        public async Task TestChangeToIEnumerableIEnumeratorMethod()
        {
            var initial =
@"using System;
using System.Collections;

class Program
{
    static IEnumerator [|M|]()
    {
        yield return 0;
    }
}";
            await TestMissingAsync(initial);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsChangeToIEnumerable)]
        public async Task TestChangeToIEnumerableIEnumerableMethod()
        {
            var initial =
@"using System;
using System.Collections;

class Program
{
    static IEnumerable [|M|]()
    {
        yield return 0;
    }
}";
            await TestMissingAsync(initial);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsChangeToIEnumerable)]
        public async Task TestChangeToIEnumerableVoidMethod()
        {
            var initial =
@"using System;
using System.Collections;

class Program
{
    static void [|M|]()
    {
        yield return 0;
    }
}";
            await TestMissingAsync(initial);
        }
    }
}
