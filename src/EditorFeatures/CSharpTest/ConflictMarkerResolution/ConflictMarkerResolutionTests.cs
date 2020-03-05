// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ConflictMarkerResolution;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.Testing.EmptyDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.CSharp.ConflictMarkerResolution.CSharpResolveConflictMarkerCodeFixProvider>;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConflictMarkerResolution
{
    public class ConflictMarkerResolutionTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsResolveConflictMarker)]
        public async Task TestTakeTop1()
        {
            var source = @"
using System;

namespace N
{
{|CS8300:<<<<<<<|} This is mine!
    class Program
    {
        static void Main(string[] args)
        {
            Program p;
            Console.WriteLine(""My section"");
        }
    }
{|CS8300:=======|}
    class Program2
    {
        static void Main2(string[] args)
        {
            Program2 p;
            Console.WriteLine(""Their section"");
        }
    }
{|CS8300:>>>>>>>|} This is theirs!
}";
            var fixedSource = @"
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
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                NumberOfIncrementalIterations = 1,
                CodeActionIndex = 0,
                CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeTopEquivalenceKey,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsResolveConflictMarker)]
        public async Task TestTakeBottom1()
        {
            var source = @"
using System;

namespace N
{
{|CS8300:<<<<<<<|} This is mine!
    class Program
    {
        static void Main(string[] args)
        {
            Program p;
            Console.WriteLine(""My section"");
        }
    }
{|CS8300:=======|}
    class Program2
    {
        static void Main2(string[] args)
        {
            Program2 p;
            Console.WriteLine(""Their section"");
        }
    }
{|CS8300:>>>>>>>|} This is theirs!
}";
            var fixedSource = @"
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
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                NumberOfIncrementalIterations = 1,
                CodeActionIndex = 1,
                CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeBottomEquivalenceKey,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsResolveConflictMarker)]
        public async Task TestTakeBoth1()
        {
            var source = @"
using System;

namespace N
{
{|CS8300:<<<<<<<|} This is mine!
    class Program
    {
        static void Main(string[] args)
        {
            Program p;
            Console.WriteLine(""My section"");
        }
    }
{|CS8300:=======|}
    class Program2
    {
        static void Main2(string[] args)
        {
            Program2 p;
            Console.WriteLine(""Their section"");
        }
    }
{|CS8300:>>>>>>>|} This is theirs!
}";
            var fixedSource = @"
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
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                NumberOfIncrementalIterations = 1,
                CodeActionIndex = 2,
                CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeBothEquivalenceKey,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsResolveConflictMarker)]
        public async Task TestEmptyTop_TakeTop()
        {
            var source = @"
using System;

namespace N
{
{|CS8300:<<<<<<<|} This is mine!
{|CS8300:=======|}
    class Program2
    {
        static void Main2(string[] args)
        {
            Program2 p;
            Console.WriteLine(""Their section"");
        }
    }
{|CS8300:>>>>>>>|} This is theirs!
}";
            var fixedSource = @"
using System;

namespace N
{
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                NumberOfIncrementalIterations = 1,
                CodeActionIndex = 0,
                CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeTopEquivalenceKey,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsResolveConflictMarker)]
        public async Task TestEmptyTop_TakeBottom()
        {
            var source = @"
using System;

namespace N
{
{|CS8300:<<<<<<<|} This is mine!
{|CS8300:=======|}
    class Program2
    {
        static void Main2(string[] args)
        {
            Program2 p;
            Console.WriteLine(""Their section"");
        }
    }
{|CS8300:>>>>>>>|} This is theirs!
}";
            var fixedSource = @"
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
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                NumberOfIncrementalIterations = 1,
                CodeActionIndex = 1,
                CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeBottomEquivalenceKey,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsResolveConflictMarker)]
        public async Task TestEmptyBottom_TakeTop()
        {
            var source = @"
using System;

namespace N
{
{|CS8300:<<<<<<<|} This is mine!
    class Program
    {
        static void Main(string[] args)
        {
            Program p;
            Console.WriteLine(""My section"");
        }
    }
{|CS8300:=======|}
{|CS8300:>>>>>>>|} This is theirs!
}";
            var fixedSource = @"
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
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                NumberOfIncrementalIterations = 1,
                CodeActionIndex = 0,
                CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeTopEquivalenceKey,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsResolveConflictMarker)]
        public async Task TestEmptyBottom_TakeBottom()
        {
            var source = @"
using System;

namespace N
{
{|CS8300:<<<<<<<|} This is mine!
    class Program
    {
        static void Main(string[] args)
        {
            Program p;
            Console.WriteLine(""My section"");
        }
    }
{|CS8300:=======|}
{|CS8300:>>>>>>>|} This is theirs!
}";
            var fixedSource = @"
using System;

namespace N
{
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                NumberOfIncrementalIterations = 1,
                CodeActionIndex = 1,
                CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeBottomEquivalenceKey,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsResolveConflictMarker)]
        public async Task TestTakeTop_WhitespaceInSection()
        {
            var source = @"
using System;

namespace N
{
{|CS8300:<<<<<<<|} This is mine!

    class Program
    {
        static void Main(string[] args)
        {
            Program p;
            Console.WriteLine(""My section"");
        }
    }

{|CS8300:=======|}
    class Program2
    {
        static void Main2(string[] args)
        {
            Program2 p;
            Console.WriteLine(""Their section"");
        }
    }
{|CS8300:>>>>>>>|} This is theirs!
}";
            var fixedSource = @"
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

}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                NumberOfIncrementalIterations = 1,
                CodeActionIndex = 0,
                CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeTopEquivalenceKey,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsResolveConflictMarker)]
        public async Task TestTakeBottom1_WhitespaceInSection()
        {
            var source = @"
using System;

namespace N
{
{|CS8300:<<<<<<<|} This is mine!
    class Program
    {
        static void Main(string[] args)
        {
            Program p;
            Console.WriteLine(""My section"");
        }
    }
{|CS8300:=======|}

    class Program2
    {
        static void Main2(string[] args)
        {
            Program2 p;
            Console.WriteLine(""Their section"");
        }
    }

{|CS8300:>>>>>>>|} This is theirs!
}";
            var fixedSource = @"
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

}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                NumberOfIncrementalIterations = 1,
                CodeActionIndex = 1,
                CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeBottomEquivalenceKey,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsResolveConflictMarker)]
        public async Task TestTakeBoth_WhitespaceInSection()
        {
            var source = @"
using System;

namespace N
{
{|CS8300:<<<<<<<|} This is mine!

    class Program
    {
        static void Main(string[] args)
        {
            Program p;
            Console.WriteLine(""My section"");
        }
    }

{|CS8300:=======|}

    class Program2
    {
        static void Main2(string[] args)
        {
            Program2 p;
            Console.WriteLine(""Their section"");
        }
    }

{|CS8300:>>>>>>>|} This is theirs!
}";
            var fixedSource = @"
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

}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                NumberOfIncrementalIterations = 1,
                CodeActionIndex = 2,
                CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeBothEquivalenceKey,
            }.RunAsync();
        }

        [WorkItem(21107, "https://github.com/dotnet/roslyn/issues/21107")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsResolveConflictMarker)]
        public async Task TestFixAll1()
        {
            var source = @"
using System;

namespace N
{
{|CS8300:<<<<<<<|} This is mine!
    class Program
    {
    }
{|CS8300:=======|}
    class Program2
    {
    }
{|CS8300:>>>>>>>|} This is theirs!

{|CS8300:<<<<<<<|} This is mine!
    class Program3
    {
    }
{|CS8300:=======|}
    class Program4
    {
    }
{|CS8300:>>>>>>>|} This is theirs!
}";
            var fixedSource = @"
using System;

namespace N
{
    class Program
    {
    }

    class Program3
    {
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                NumberOfIncrementalIterations = 2,
                CodeActionIndex = 0,
                CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeTopEquivalenceKey,
            }.RunAsync();
        }

        [WorkItem(21107, "https://github.com/dotnet/roslyn/issues/21107")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsResolveConflictMarker)]
        public async Task TestFixAll2()
        {
            var source = @"
using System;

namespace N
{
{|CS8300:<<<<<<<|} This is mine!
    class Program
    {
    }
{|CS8300:=======|}
    class Program2
    {
    }
{|CS8300:>>>>>>>|} This is theirs!

{|CS8300:<<<<<<<|} This is mine!
    class Program3
    {
    }
{|CS8300:=======|}
    class Program4
    {
    }
{|CS8300:>>>>>>>|} This is theirs!
}";
            var fixedSource = @"
using System;

namespace N
{
    class Program2
    {
    }

    class Program4
    {
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                NumberOfIncrementalIterations = 2,
                CodeActionIndex = 1,
                CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeBottomEquivalenceKey,
            }.RunAsync();
        }

        [WorkItem(21107, "https://github.com/dotnet/roslyn/issues/21107")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsResolveConflictMarker)]
        public async Task TestFixAll3()
        {
            var source = @"
using System;

namespace N
{
{|CS8300:<<<<<<<|} This is mine!
    class Program
    {
    }
{|CS8300:=======|}
    class Program2
    {
    }
{|CS8300:>>>>>>>|} This is theirs!

{|CS8300:<<<<<<<|} This is mine!
    class Program3
    {
    }
{|CS8300:=======|}
    class Program4
    {
    }
{|CS8300:>>>>>>>|} This is theirs!
}";
            var fixedSource = @"
using System;

namespace N
{
    class Program
    {
    }
    class Program2
    {
    }

    class Program3
    {
    }
    class Program4
    {
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                NumberOfIncrementalIterations = 2,
                CodeActionIndex = 2,
                CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeBothEquivalenceKey,
            }.RunAsync();
        }
    }
}
