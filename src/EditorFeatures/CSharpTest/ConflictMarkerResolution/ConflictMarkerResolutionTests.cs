// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.ConflictMarkerResolution;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConflictMarkerResolution
{
    public class ConflictMarkerResolutionTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpResolveConflictMarkerCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsResolveConflictMarker)]
        public async Task TestTakeTop1()
        {
            await TestInRegularAndScript1Async(
@"
using System;

namespace N
{
[|<<<<<<<|] This is mine!
    class Program
    {
        static void Main(string[] args)
        {
            Program p;
            Console.WriteLine(""My section"");
        }
    }
=======
    class Program2
    {
        static void Main2(string[] args)
        {
            Program2 p;
            Console.WriteLine(""Their section"");
        }
    }
>>>>>>> This is theirs!
}",
@"
using System;

namespace N
{
    class Program
    {
        static void Main(string[] args)
        {
            Program p;
            Console.WriteLine(""My section"");
        }
    }
}", index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsResolveConflictMarker)]
        public async Task TestTakeBottom1()
        {
            await TestInRegularAndScript1Async(
@"
using System;

namespace N
{
[|<<<<<<<|] This is mine!
    class Program
    {
        static void Main(string[] args)
        {
            Program p;
            Console.WriteLine(""My section"");
        }
    }
=======
    class Program2
    {
        static void Main2(string[] args)
        {
            Program2 p;
            Console.WriteLine(""Their section"");
        }
    }
>>>>>>> This is theirs!
}",
@"
using System;

namespace N
{
    class Program2
    {
        static void Main2(string[] args)
        {
            Program2 p;
            Console.WriteLine(""Their section"");
        }
    }
}", index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsResolveConflictMarker)]
        public async Task TestTakeBoth1()
        {
            await TestInRegularAndScript1Async(
@"
using System;

namespace N
{
[|<<<<<<<|] This is mine!
    class Program
    {
        static void Main(string[] args)
        {
            Program p;
            Console.WriteLine(""My section"");
        }
    }
=======
    class Program2
    {
        static void Main2(string[] args)
        {
            Program2 p;
            Console.WriteLine(""Their section"");
        }
    }
>>>>>>> This is theirs!
}",
@"
using System;

namespace N
{
    class Program
    {
        static void Main(string[] args)
        {
            Program p;
            Console.WriteLine(""My section"");
        }
    }
    class Program2
    {
        static void Main2(string[] args)
        {
            Program2 p;
            Console.WriteLine(""Their section"");
        }
    }
}", index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsResolveConflictMarker)]
        public async Task TestEmptyTop_TakeTop()
        {
            await TestInRegularAndScript1Async(
@"
using System;

namespace N
{
[|<<<<<<<|] This is mine!
=======
    class Program2
    {
        static void Main2(string[] args)
        {
            Program2 p;
            Console.WriteLine(""Their section"");
        }
    }
>>>>>>> This is theirs!
}",
@"
using System;

namespace N
{
}", index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsResolveConflictMarker)]
        public async Task TestEmptyTop_TakeBottom()
        {
            await TestInRegularAndScript1Async(
@"
using System;

namespace N
{
[|<<<<<<<|] This is mine!
=======
    class Program2
    {
        static void Main2(string[] args)
        {
            Program2 p;
            Console.WriteLine(""Their section"");
        }
    }
>>>>>>> This is theirs!
}",
@"
using System;

namespace N
{
    class Program2
    {
        static void Main2(string[] args)
        {
            Program2 p;
            Console.WriteLine(""Their section"");
        }
    }
}", index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsResolveConflictMarker)]
        public async Task TestEmptyBottom_TakeTop()
        {
            await TestInRegularAndScript1Async(
@"
using System;

namespace N
{
[|<<<<<<<|] This is mine!
    class Program
    {
        static void Main(string[] args)
        {
            Program p;
            Console.WriteLine(""My section"");
        }
    }
=======
>>>>>>> This is theirs!
}",
@"
using System;

namespace N
{
    class Program
    {
        static void Main(string[] args)
        {
            Program p;
            Console.WriteLine(""My section"");
        }
    }
}", index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsResolveConflictMarker)]
        public async Task TestEmptyBottom_TakeBottom()
        {
            await TestInRegularAndScript1Async(
@"
using System;

namespace N
{
[|<<<<<<<|] This is mine!
    class Program
    {
        static void Main(string[] args)
        {
            Program p;
            Console.WriteLine(""My section"");
        }
    }
=======
>>>>>>> This is theirs!
}",
@"
using System;

namespace N
{
}", index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsResolveConflictMarker)]
        public async Task TestTakeTop_WhitespaceInSection()
        {
            await TestInRegularAndScript1Async(
@"
using System;

namespace N
{
[|<<<<<<<|] This is mine!

    class Program
    {
        static void Main(string[] args)
        {
            Program p;
            Console.WriteLine(""My section"");
        }
    }

=======
    class Program2
    {
        static void Main2(string[] args)
        {
            Program2 p;
            Console.WriteLine(""Their section"");
        }
    }
>>>>>>> This is theirs!
}",
@"
using System;

namespace N
{

    class Program
    {
        static void Main(string[] args)
        {
            Program p;
            Console.WriteLine(""My section"");
        }
    }

}", index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsResolveConflictMarker)]
        public async Task TestTakeBottom1_WhitespaceInSection()
        {
            await TestInRegularAndScript1Async(
@"
using System;

namespace N
{
[|<<<<<<<|] This is mine!
    class Program
    {
        static void Main(string[] args)
        {
            Program p;
            Console.WriteLine(""My section"");
        }
    }
=======

    class Program2
    {
        static void Main2(string[] args)
        {
            Program2 p;
            Console.WriteLine(""Their section"");
        }
    }

>>>>>>> This is theirs!
}",
@"
using System;

namespace N
{

    class Program2
    {
        static void Main2(string[] args)
        {
            Program2 p;
            Console.WriteLine(""Their section"");
        }
    }

}", index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsResolveConflictMarker)]
        public async Task TestTakeBoth_WhitespaceInSection()
        {
            await TestInRegularAndScript1Async(
@"
using System;

namespace N
{
[|<<<<<<<|] This is mine!

    class Program
    {
        static void Main(string[] args)
        {
            Program p;
            Console.WriteLine(""My section"");
        }
    }

=======

    class Program2
    {
        static void Main2(string[] args)
        {
            Program2 p;
            Console.WriteLine(""Their section"");
        }
    }

>>>>>>> This is theirs!
}",
@"
using System;

namespace N
{

    class Program
    {
        static void Main(string[] args)
        {
            Program p;
            Console.WriteLine(""My section"");
        }
    }


    class Program2
    {
        static void Main2(string[] args)
        {
            Program2 p;
            Console.WriteLine(""Their section"");
        }
    }

}", index: 2);
        }
    }
}
