// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ConflictMarkerResolution;
using Microsoft.CodeAnalysis.CSharp.ConflictMarkerResolution;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConflictMarkerResolution;

using VerifyCS = CSharpCodeFixVerifier<EmptyDiagnosticAnalyzer, CSharpResolveConflictMarkerCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsResolveConflictMarker)]
public sealed class ConflictMarkerResolutionTests
{
    [Fact]
    public Task TestTakeTop1()
        => new VerifyCS.Test
        {
            TestCode = """
            using System;

            namespace N
            {
            {|CS8300:<<<<<<<|} This is mine!
                class Program
                {
                    static void Main(string[] args)
                    {
                        Program p;
                        Console.WriteLine("My section");
                    }
                }
            {|CS8300:=======|}
                class Program2
                {
                    static void Main2(string[] args)
                    {
                        Program2 p;
                        Console.WriteLine("Their section");
                    }
                }
            {|CS8300:>>>>>>>|} This is theirs!
            }
            """,
            FixedCode = """
            using System;

            namespace N
            {
                class Program
                {
                    static void Main(string[] args)
                    {
                        Program p;
                        Console.WriteLine("My section");
                    }
                }
            }
            """,
            NumberOfIncrementalIterations = 1,
            CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeTopEquivalenceKey,
        }.RunAsync();

    [Fact]
    public Task TestTakeBottom1()
        => new VerifyCS.Test
        {
            TestCode = """
            using System;

            namespace N
            {
            {|CS8300:<<<<<<<|} This is mine!
                class Program
                {
                    static void Main(string[] args)
                    {
                        Program p;
                        Console.WriteLine("My section");
                    }
                }
            {|CS8300:=======|}
                class Program2
                {
                    static void Main2(string[] args)
                    {
                        Program2 p;
                        Console.WriteLine("Their section");
                    }
                }
            {|CS8300:>>>>>>>|} This is theirs!
            }
            """,
            FixedCode = """
            using System;

            namespace N
            {
                class Program2
                {
                    static void Main2(string[] args)
                    {
                        Program2 p;
                        Console.WriteLine("Their section");
                    }
                }
            }
            """,
            NumberOfIncrementalIterations = 1,
            CodeActionIndex = 1,
            CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeBottomEquivalenceKey,
        }.RunAsync();

    [Fact]
    public Task TestTakeBoth1()
        => new VerifyCS.Test
        {
            TestCode = """
            using System;

            namespace N
            {
            {|CS8300:<<<<<<<|} This is mine!
                class Program
                {
                    static void Main(string[] args)
                    {
                        Program p;
                        Console.WriteLine("My section");
                    }
                }
            {|CS8300:=======|}
                class Program2
                {
                    static void Main2(string[] args)
                    {
                        Program2 p;
                        Console.WriteLine("Their section");
                    }
                }
            {|CS8300:>>>>>>>|} This is theirs!
            }
            """,
            FixedCode = """
            using System;

            namespace N
            {
                class Program
                {
                    static void Main(string[] args)
                    {
                        Program p;
                        Console.WriteLine("My section");
                    }
                }
                class Program2
                {
                    static void Main2(string[] args)
                    {
                        Program2 p;
                        Console.WriteLine("Their section");
                    }
                }
            }
            """,
            NumberOfIncrementalIterations = 1,
            CodeActionIndex = 2,
            CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeBothEquivalenceKey,
        }.RunAsync();

    [Fact]
    public Task TestEmptyTop_TakeTop()
        => new VerifyCS.Test
        {
            TestCode = """
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
                        Console.WriteLine("Their section");
                    }
                }
            {|CS8300:>>>>>>>|} This is theirs!
            }
            """,
            FixedCode = """
            using System;

            namespace N
            {
            }
            """,
            NumberOfIncrementalIterations = 1,
            CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeTopEquivalenceKey,
        }.RunAsync();

    [Fact]
    public Task TestEmptyTop_TakeBottom()
        => new VerifyCS.Test
        {
            TestCode = """
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
                        Console.WriteLine("Their section");
                    }
                }
            {|CS8300:>>>>>>>|} This is theirs!
            }
            """,
            FixedCode = """
            using System;

            namespace N
            {
                class Program2
                {
                    static void Main2(string[] args)
                    {
                        Program2 p;
                        Console.WriteLine("Their section");
                    }
                }
            }
            """,
            NumberOfIncrementalIterations = 1,
            CodeActionIndex = 1,
            CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeBottomEquivalenceKey,
        }.RunAsync();

    [Fact]
    public Task TestEmptyBottom_TakeTop()
        => new VerifyCS.Test
        {
            TestCode = """
            using System;

            namespace N
            {
            {|CS8300:<<<<<<<|} This is mine!
                class Program
                {
                    static void Main(string[] args)
                    {
                        Program p;
                        Console.WriteLine("My section");
                    }
                }
            {|CS8300:=======|}
            {|CS8300:>>>>>>>|} This is theirs!
            }
            """,
            FixedCode = """
            using System;

            namespace N
            {
                class Program
                {
                    static void Main(string[] args)
                    {
                        Program p;
                        Console.WriteLine("My section");
                    }
                }
            }
            """,
            NumberOfIncrementalIterations = 1,
            CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeTopEquivalenceKey,
        }.RunAsync();

    [Fact]
    public Task TestEmptyBottom_TakeBottom()
        => new VerifyCS.Test
        {
            TestCode = """
            using System;

            namespace N
            {
            {|CS8300:<<<<<<<|} This is mine!
                class Program
                {
                    static void Main(string[] args)
                    {
                        Program p;
                        Console.WriteLine("My section");
                    }
                }
            {|CS8300:=======|}
            {|CS8300:>>>>>>>|} This is theirs!
            }
            """,
            FixedCode = """
            using System;

            namespace N
            {
            }
            """,
            NumberOfIncrementalIterations = 1,
            CodeActionIndex = 1,
            CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeBottomEquivalenceKey,
        }.RunAsync();

    [Fact]
    public Task TestTakeTop_WhitespaceInSection()
        => new VerifyCS.Test
        {
            TestCode = """
            using System;

            namespace N
            {
            {|CS8300:<<<<<<<|} This is mine!

                class Program
                {
                    static void Main(string[] args)
                    {
                        Program p;
                        Console.WriteLine("My section");
                    }
                }

            {|CS8300:=======|}
                class Program2
                {
                    static void Main2(string[] args)
                    {
                        Program2 p;
                        Console.WriteLine("Their section");
                    }
                }
            {|CS8300:>>>>>>>|} This is theirs!
            }
            """,
            FixedCode = """
            using System;

            namespace N
            {

                class Program
                {
                    static void Main(string[] args)
                    {
                        Program p;
                        Console.WriteLine("My section");
                    }
                }

            }
            """,
            NumberOfIncrementalIterations = 1,
            CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeTopEquivalenceKey,
        }.RunAsync();

    [Fact]
    public Task TestTakeBottom1_WhitespaceInSection()
        => new VerifyCS.Test
        {
            TestCode = """
            using System;

            namespace N
            {
            {|CS8300:<<<<<<<|} This is mine!
                class Program
                {
                    static void Main(string[] args)
                    {
                        Program p;
                        Console.WriteLine("My section");
                    }
                }
            {|CS8300:=======|}

                class Program2
                {
                    static void Main2(string[] args)
                    {
                        Program2 p;
                        Console.WriteLine("Their section");
                    }
                }

            {|CS8300:>>>>>>>|} This is theirs!
            }
            """,
            FixedCode = """
            using System;

            namespace N
            {

                class Program2
                {
                    static void Main2(string[] args)
                    {
                        Program2 p;
                        Console.WriteLine("Their section");
                    }
                }

            }
            """,
            NumberOfIncrementalIterations = 1,
            CodeActionIndex = 1,
            CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeBottomEquivalenceKey,
        }.RunAsync();

    [Fact]
    public Task TestTakeBoth_WhitespaceInSection()
        => new VerifyCS.Test
        {
            TestCode = """
            using System;

            namespace N
            {
            {|CS8300:<<<<<<<|} This is mine!

                class Program
                {
                    static void Main(string[] args)
                    {
                        Program p;
                        Console.WriteLine("My section");
                    }
                }

            {|CS8300:=======|}

                class Program2
                {
                    static void Main2(string[] args)
                    {
                        Program2 p;
                        Console.WriteLine("Their section");
                    }
                }

            {|CS8300:>>>>>>>|} This is theirs!
            }
            """,
            FixedCode = """
            using System;

            namespace N
            {

                class Program
                {
                    static void Main(string[] args)
                    {
                        Program p;
                        Console.WriteLine("My section");
                    }
                }


                class Program2
                {
                    static void Main2(string[] args)
                    {
                        Program2 p;
                        Console.WriteLine("Their section");
                    }
                }

            }
            """,
            NumberOfIncrementalIterations = 1,
            CodeActionIndex = 2,
            CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeBothEquivalenceKey,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23847")]
    public Task TestTakeTop_TopCommentedOut()
        => new VerifyCS.Test
        {
            TestCode = """
            public class Class1
            {
                public void M()
                {
                    /*
            <<<<<<< dest
                     * a thing
                     */
            {|CS8300:=======|}
                     * another thing
                     */
            {|CS8300:>>>>>>>|} source
                    // */
                }
            }
            """,
            FixedCode = """
            public class Class1
            {
                public void M()
                {
                    /*
                     * a thing
                     */
                    // */
                }
            }
            """,
            NumberOfIncrementalIterations = 1,
            CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeTopEquivalenceKey,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23847")]
    public Task TestTakeTop_SecondMiddleAndBottomCommentedOut()
        => new VerifyCS.Test
        {
            TestCode = """
            public class Class1
            {
                public void M()
                {
            {|CS8300:<<<<<<<|} dest
                    /*
                     * a thing
            =======
                     *
                     * another thing
            >>>>>>> source
                     */
                }
            }
            """,
            FixedCode = """
            public class Class1
            {
                public void M()
                {
                    /*
                     * a thing
                     */
                }
            }
            """,
            NumberOfIncrementalIterations = 1,
            CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeTopEquivalenceKey,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23847")]
    public Task TestTakeTop_TopInString()
        => new VerifyCS.Test
        {
            TestCode = """
            class X {
              void x() {
                var x = @"
            <<<<<<< working copy
            a";
            {|CS8300:=======|}
            b";
            {|CS8300:>>>>>>>|} merge rev
              }
            }
            """,
            FixedCode = """
            class X {
              void x() {
                var x = @"
            a";
              }
            }
            """,
            NumberOfIncrementalIterations = 1,
            CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeTopEquivalenceKey,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23847")]
    public Task TestTakeBottom_TopInString()
        => new VerifyCS.Test
        {
            TestCode = """
            class X {
              void x() {
                var x = @"
            <<<<<<< working copy
            a";
            {|CS8300:=======|}
            b";
            {|CS8300:>>>>>>>|} merge rev
              }
            }
            """,
            FixedCode = """
            class X {
              void x() {
                var x = @"
            b";
              }
            }
            """,
            NumberOfIncrementalIterations = 1,
            CodeActionIndex = 1,
            CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeBottomEquivalenceKey,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23847")]
    public Task TestMissingWithMiddleMarkerAtTopOfFile()
        => new VerifyCS.Test
        {
            TestCode = """
            {|CS8300:=======|}
            class X {
            }
            {|CS8300:>>>>>>>|} merge rev
            """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23847")]
    public Task TestMissingWithMiddleMarkerAtBottomOfFile()
        => new VerifyCS.Test
        {
            TestCode = """
            {|CS8300:<<<<<<<|} working copy
            class X {
            }
            {|CS8300:=======|}
            """,
        }.RunAsync();

    [Fact]
    public Task TestMissingWithFirstMiddleMarkerAtBottomOfFile()
        => new VerifyCS.Test
        {
            TestCode = """
            {|CS8300:<<<<<<<|} working copy
            class X {
            }
            {|CS8300:||||||||}
            """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21107")]
    public Task TestFixAll1()
        => new VerifyCS.Test
        {
            TestCode = """
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
            }
            """,
            FixedCode = """
            using System;

            namespace N
            {
                class Program
                {
                }

                class Program3
                {
                }
            }
            """,
            NumberOfIncrementalIterations = 2,
            CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeTopEquivalenceKey,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21107")]
    public Task TestFixAll2()
        => new VerifyCS.Test
        {
            TestCode = """
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
            }
            """,
            FixedCode = """
            using System;

            namespace N
            {
                class Program2
                {
                }

                class Program4
                {
                }
            }
            """,
            NumberOfIncrementalIterations = 2,
            CodeActionIndex = 1,
            CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeBottomEquivalenceKey,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21107")]
    public Task TestFixAll3()
        => new VerifyCS.Test
        {
            TestCode = """
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
            }
            """,
            FixedCode = """
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
            }
            """,
            NumberOfIncrementalIterations = 2,
            CodeActionIndex = 2,
            CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeBothEquivalenceKey,
        }.RunAsync();

    [Fact]
    public Task TestTakeTop_WithBaseline()
        => new VerifyCS.Test
        {
            TestCode = """
            using System;

            namespace N
            {
            {|CS8300:<<<<<<<|} This is mine!
                class Program
                {
                    static void Main(string[] args)
                    {
                        Program p;
                        Console.WriteLine("My section");
                    }
                }
            {|CS8300:||||||||} Baseline!
                class Removed { }
            {|CS8300:=======|}
                class Program2
                {
                    static void Main2(string[] args)
                    {
                        Program2 p;
                        Console.WriteLine("Their section");
                    }
                }
            {|CS8300:>>>>>>>|} This is theirs!
            }
            """,
            FixedCode = """
            using System;

            namespace N
            {
                class Program
                {
                    static void Main(string[] args)
                    {
                        Program p;
                        Console.WriteLine("My section");
                    }
                }
            }
            """,
            NumberOfIncrementalIterations = 1,
            CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeTopEquivalenceKey,
        }.RunAsync();

    [Fact]
    public Task TestTakeBottom1_WithBaseline()
        => new VerifyCS.Test
        {
            TestCode = """
            using System;

            namespace N
            {
            {|CS8300:<<<<<<<|} This is mine!
                class Program
                {
                    static void Main(string[] args)
                    {
                        Program p;
                        Console.WriteLine("My section");
                    }
                }
            {|CS8300:||||||||} Baseline!
                class Removed { }
            {|CS8300:=======|}
                class Program2
                {
                    static void Main2(string[] args)
                    {
                        Program2 p;
                        Console.WriteLine("Their section");
                    }
                }
            {|CS8300:>>>>>>>|} This is theirs!
            }
            """,
            FixedCode = """
            using System;

            namespace N
            {
                class Program2
                {
                    static void Main2(string[] args)
                    {
                        Program2 p;
                        Console.WriteLine("Their section");
                    }
                }
            }
            """,
            NumberOfIncrementalIterations = 1,
            CodeActionIndex = 1,
            CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeBottomEquivalenceKey,
        }.RunAsync();

    [Fact]
    public Task TestTakeBoth1_WithBaseline()
        => new VerifyCS.Test
        {
            TestCode = """
            using System;

            namespace N
            {
            {|CS8300:<<<<<<<|} This is mine!
                class Program
                {
                    static void Main(string[] args)
                    {
                        Program p;
                        Console.WriteLine("My section");
                    }
                }
            {|CS8300:||||||||} Baseline!
                class Removed { }
            {|CS8300:=======|}
                class Program2
                {
                    static void Main2(string[] args)
                    {
                        Program2 p;
                        Console.WriteLine("Their section");
                    }
                }
            {|CS8300:>>>>>>>|} This is theirs!
            }
            """,
            FixedCode = """
            using System;

            namespace N
            {
                class Program
                {
                    static void Main(string[] args)
                    {
                        Program p;
                        Console.WriteLine("My section");
                    }
                }
                class Program2
                {
                    static void Main2(string[] args)
                    {
                        Program2 p;
                        Console.WriteLine("Their section");
                    }
                }
            }
            """,
            NumberOfIncrementalIterations = 1,
            CodeActionIndex = 2,
            CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeBothEquivalenceKey,
        }.RunAsync();

    [Fact]
    public Task TestEmptyTop_TakeTop_WithBaseline()
        => new VerifyCS.Test
        {
            TestCode = """
            using System;

            namespace N
            {
            {|CS8300:<<<<<<<|} This is mine!
            {|CS8300:||||||||} Baseline!
                class Removed { }
            {|CS8300:=======|}
                class Program2
                {
                    static void Main2(string[] args)
                    {
                        Program2 p;
                        Console.WriteLine("Their section");
                    }
                }
            {|CS8300:>>>>>>>|} This is theirs!
            }
            """,
            FixedCode = """
            using System;

            namespace N
            {
            }
            """,
            NumberOfIncrementalIterations = 1,
            CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeTopEquivalenceKey,
        }.RunAsync();

    [Fact]
    public Task TestEmptyTop_TakeBottom_WithBaseline()
        => new VerifyCS.Test
        {
            TestCode = """
            using System;

            namespace N
            {
            {|CS8300:<<<<<<<|} This is mine!
            {|CS8300:||||||||} Baseline!
                class Removed { }
            {|CS8300:=======|}
                class Program2
                {
                    static void Main2(string[] args)
                    {
                        Program2 p;
                        Console.WriteLine("Their section");
                    }
                }
            {|CS8300:>>>>>>>|} This is theirs!
            }
            """,
            FixedCode = """
            using System;

            namespace N
            {
                class Program2
                {
                    static void Main2(string[] args)
                    {
                        Program2 p;
                        Console.WriteLine("Their section");
                    }
                }
            }
            """,
            NumberOfIncrementalIterations = 1,
            CodeActionIndex = 1,
            CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeBottomEquivalenceKey,
        }.RunAsync();

    [Fact]
    public Task TestEmptyBottom_TakeTop_WithBaseline()
        => new VerifyCS.Test
        {
            TestCode = """
            using System;

            namespace N
            {
            {|CS8300:<<<<<<<|} This is mine!
                class Program
                {
                    static void Main(string[] args)
                    {
                        Program p;
                        Console.WriteLine("My section");
                    }
                }
            {|CS8300:||||||||} Baseline!
                class Removed { }
            {|CS8300:=======|}
            {|CS8300:>>>>>>>|} This is theirs!
            }
            """,
            FixedCode = """
            using System;

            namespace N
            {
                class Program
                {
                    static void Main(string[] args)
                    {
                        Program p;
                        Console.WriteLine("My section");
                    }
                }
            }
            """,
            NumberOfIncrementalIterations = 1,
            CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeTopEquivalenceKey,
        }.RunAsync();

    [Fact]
    public Task TestEmptyBottom_TakeBottom_WithBaseline()
        => new VerifyCS.Test
        {
            TestCode = """
            using System;

            namespace N
            {
            {|CS8300:<<<<<<<|} This is mine!
                class Program
                {
                    static void Main(string[] args)
                    {
                        Program p;
                        Console.WriteLine("My section");
                    }
                }
            {|CS8300:||||||||} Baseline!
                class Removed { }
            {|CS8300:=======|}
            {|CS8300:>>>>>>>|} This is theirs!
            }
            """,
            FixedCode = """
            using System;

            namespace N
            {
            }
            """,
            NumberOfIncrementalIterations = 1,
            CodeActionIndex = 1,
            CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeBottomEquivalenceKey,
        }.RunAsync();

    [Fact]
    public Task TestTakeTop_TopCommentedOut_WithBaseline()
        => new VerifyCS.Test
        {
            TestCode = """
            public class Class1
            {
                public void M()
                {
                    /*
            <<<<<<< dest
                     * a thing
                     */
            {|CS8300:||||||||} Baseline!
                     * previous thing
                     */
            {|CS8300:=======|}
                     * another thing
                     */
            {|CS8300:>>>>>>>|} source
                    // */
                }
            }
            """,
            FixedCode = """
            public class Class1
            {
                public void M()
                {
                    /*
                     * a thing
                     */
                    // */
                }
            }
            """,
            NumberOfIncrementalIterations = 1,
            CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeTopEquivalenceKey,
        }.RunAsync();

    [Fact]
    public Task TestTakeTop_FirstMiddleAndSecondMiddleAndBottomCommentedOut()
        => new VerifyCS.Test
        {
            TestCode = """
            public class Class1
            {
                public void M()
                {
            {|CS8300:<<<<<<<|} dest
                    /*
                     * a thing
            |||||||| Baseline!
                     * previous thing
            =======
                     *
                     * another thing
            >>>>>>> source
                     */
                }
            }
            """,
            FixedCode = """
            public class Class1
            {
                public void M()
                {
                    /*
                     * a thing
                     */
                }
            }
            """,
            NumberOfIncrementalIterations = 1,
            CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeTopEquivalenceKey,
        }.RunAsync();

    [Fact]
    public Task TestTakeTop_TopInString_WithBaseline()
        => new VerifyCS.Test
        {
            TestCode = """
            class X {
              void x() {
                var x = @"
            <<<<<<< working copy
            a";
            {|CS8300:||||||||} baseline
            previous";
            {|CS8300:=======|}
            b";
            {|CS8300:>>>>>>>|} merge rev
              }
            }
            """,
            FixedCode = """
            class X {
              void x() {
                var x = @"
            a";
              }
            }
            """,
            NumberOfIncrementalIterations = 1,
            CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeTopEquivalenceKey,
        }.RunAsync();

    [Fact]
    public Task TestTakeBottom_TopInString_WithBaseline()
        => new VerifyCS.Test
        {
            TestCode = """
            class X {
              void x() {
                var x = @"
            <<<<<<< working copy
            a";
            {|CS8300:||||||||} baseline
            previous";
            {|CS8300:=======|}
            b";
            {|CS8300:>>>>>>>|} merge rev
              }
            }
            """,
            FixedCode = """
            class X {
              void x() {
                var x = @"
            b";
              }
            }
            """,
            NumberOfIncrementalIterations = 1,
            CodeActionIndex = 1,
            CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeBottomEquivalenceKey,
        }.RunAsync();

    [Fact]
    public Task TestMissingWithFirstMiddleMarkerAtTopOfFile()
        => new VerifyCS.Test
        {
            TestCode = """
            {|CS8300:||||||||} baseline
            {|CS8300:=======|}
            class X {
            }
            {|CS8300:>>>>>>>|} merge rev
            """,
        }.RunAsync();

    [Fact]
    public Task TestFixAll1_WithBaseline()
        => new VerifyCS.Test
        {
            TestCode = """
            using System;

            namespace N
            {
            {|CS8300:<<<<<<<|} This is mine!
                class Program
                {
                }
            {|CS8300:||||||||} baseline
                class Removed { }
            {|CS8300:=======|}
                class Program2
                {
                }
            {|CS8300:>>>>>>>|} This is theirs!

            {|CS8300:<<<<<<<|} This is mine!
                class Program3
                {
                }
            {|CS8300:||||||||} baseline
                class Removed2 { }
            {|CS8300:=======|}
                class Program4
                {
                }
            {|CS8300:>>>>>>>|} This is theirs!
            }
            """,
            FixedCode = """
            using System;

            namespace N
            {
                class Program
                {
                }

                class Program3
                {
                }
            }
            """,
            NumberOfIncrementalIterations = 2,
            CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeTopEquivalenceKey,
        }.RunAsync();

    [Fact]
    public Task TestFixAll2_WithBaseline()
        => new VerifyCS.Test
        {
            TestCode = """
            using System;

            namespace N
            {
            {|CS8300:<<<<<<<|} This is mine!
                class Program
                {
                }
            {|CS8300:||||||||} baseline
                class Removed { }
            {|CS8300:=======|}
                class Program2
                {
                }
            {|CS8300:>>>>>>>|} This is theirs!

            {|CS8300:<<<<<<<|} This is mine!
                class Program3
                {
                }
            {|CS8300:||||||||} baseline
                class Removed2 { }
            {|CS8300:=======|}
                class Program4
                {
                }
            {|CS8300:>>>>>>>|} This is theirs!
            }
            """,
            FixedCode = """
            using System;

            namespace N
            {
                class Program2
                {
                }

                class Program4
                {
                }
            }
            """,
            NumberOfIncrementalIterations = 2,
            CodeActionIndex = 1,
            CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeBottomEquivalenceKey,
        }.RunAsync();

    [Fact]
    public Task TestFixAll3_WithBaseline()
        => new VerifyCS.Test
        {
            TestCode = """
            using System;

            namespace N
            {
            {|CS8300:<<<<<<<|} This is mine!
                class Program
                {
                }
            {|CS8300:||||||||} baseline
                class Removed { }
            {|CS8300:=======|}
                class Program2
                {
                }
            {|CS8300:>>>>>>>|} This is theirs!

            {|CS8300:<<<<<<<|} This is mine!
                class Program3
                {
                }
            {|CS8300:||||||||} baseline
                class Removed2 { }
            {|CS8300:=======|}
                class Program4
                {
                }
            {|CS8300:>>>>>>>|} This is theirs!
            }
            """,
            FixedCode = """
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
            }
            """,
            NumberOfIncrementalIterations = 2,
            CodeActionIndex = 2,
            CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeBothEquivalenceKey,
        }.RunAsync();
}
