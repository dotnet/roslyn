// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.OrganizeImports;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Workspaces.UnitTests.OrganizeImports;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.Organizing)]
public sealed class OrganizeUsingsTests
{
    private static async Task CheckAsync(
        string initial,
        string final,
        bool placeSystemNamespaceFirst = false,
        bool separateImportGroups = false,
        string? endOfLine = null)
    {
        using var workspace = new AdhocWorkspace();
        var project = workspace.CurrentSolution.AddProject("Project", "Project.dll", LanguageNames.CSharp);
        var document = project.AddDocument("Document", initial.ReplaceLineEndings(endOfLine ?? Environment.NewLine));

        var options = new OrganizeImportsOptions()
        {
            PlaceSystemNamespaceFirst = placeSystemNamespaceFirst,
            SeparateImportDirectiveGroups = separateImportGroups,
            NewLine = endOfLine ?? OrganizeImportsOptions.Default.NewLine
        };

        var organizeImportsService = document.GetRequiredLanguageService<IOrganizeImportsService>();
        var newDocument = await organizeImportsService.OrganizeImportsAsync(document, options, CancellationToken.None);
        var newRoot = await newDocument.GetRequiredSyntaxRootAsync(default);
        Assert.Equal(final.ReplaceLineEndings(endOfLine ?? Environment.NewLine), newRoot.ToFullString());
    }

    [Fact]
    public async Task EmptyFile()
        => await CheckAsync(string.Empty, string.Empty);

    [Fact]
    public async Task SingleUsingStatement()
    {
        var initial = @"using A;";
        var final = initial;
        await CheckAsync(initial, final);
    }

    [Fact]
    public Task AliasesAtBottom()
        => CheckAsync("""
            using A = B;
            using C;
            using D = E;
            using F;
            """, """
            using C;
            using F;
            using A = B;
            using D = E;

            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/44136")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public async Task PreserveExistingEndOfLine(string fallbackEndOfLine)
    {
        var initial = "using A = B;\nusing C;\nusing D = E;\nusing F;\n";
        using var workspace = new AdhocWorkspace();
        var project = workspace.CurrentSolution.AddProject("Project", "Project.dll", LanguageNames.CSharp);
        var document = project.AddDocument("Document", initial);

        var options = new OrganizeImportsOptions()
        {
            PlaceSystemNamespaceFirst = false,
            SeparateImportDirectiveGroups = false,
            NewLine = fallbackEndOfLine,
        };

        var organizeImportsService = document.GetRequiredLanguageService<IOrganizeImportsService>();
        var newDocument = await organizeImportsService.OrganizeImportsAsync(document, options, CancellationToken.None);
        var newRoot = await newDocument.GetRequiredSyntaxRootAsync(default);
        Assert.Equal("using C;\nusing F;\nusing A = B;\nusing D = E;\n", newRoot.ToFullString());
    }

    [Fact]
    public Task UsingStaticsBetweenUsingsAndAliases()
        => CheckAsync("""
            using static System.Convert;
            using A = B;
            using C;
            using Z;
            using D = E;
            using static System.Console;
            using F;
            """, """
            using C;
            using F;
            using Z;
            using static System.Console;
            using static System.Convert;
            using A = B;
            using D = E;

            """);

    [Fact]
    public Task NestedStatements()
        => CheckAsync("""
            using B;
            using A;

            namespace N
            {
              using D;
              using C;

              namespace N1
              {
                using F;
                using E;
              }

              namespace N2
              {
                using H;
                using G;
              }
            }

            namespace N3
            {
              using J;
              using I;

              namespace N4
              {
                using L;
                using K;
              }

              namespace N5
              {
                using N;
                using M;
              }
            }
            """, """
            using A;
            using B;

            namespace N
            {
              using C;
              using D;

              namespace N1
              {
                using E;
                using F;
              }

              namespace N2
              {
                using G;
                using H;
              }
            }

            namespace N3
            {
              using I;
              using J;

              namespace N4
              {
                using K;
                using L;
              }

              namespace N5
              {
                using M;
                using N;
              }
            }
            """);

    [Fact]
    public Task FileScopedNamespace()
        => CheckAsync("""
            using B;
            using A;

            namespace N;

            using D;
            using C;

            """, """
            using A;
            using B;

            namespace N;

            using C;
            using D;

            """);

    [Fact]
    public Task SpecialCaseSystem()
        => CheckAsync("""
            using M2;
            using M1;
            using System.Linq;
            using System;
            """, """
            using System;
            using System.Linq;
            using M1;
            using M2;

            """, placeSystemNamespaceFirst: true);

    [Fact]
    public Task SpecialCaseSystemWithUsingStatic()
        => CheckAsync("""
            using M2;
            using M1;
            using System.Linq;
            using System;
            using static Microsoft.Win32.Registry;
            using static System.BitConverter;
            """, """
            using System;
            using System.Linq;
            using M1;
            using M2;
            using static System.BitConverter;
            using static Microsoft.Win32.Registry;

            """, placeSystemNamespaceFirst: true);

    [Fact]
    public Task DoNotSpecialCaseSystem()
        => CheckAsync("""
            using M2;
            using M1;
            using System.Linq;
            using System;
            """, """
            using M1;
            using M2;
            using System;
            using System.Linq;

            """);

    [Fact]
    public Task DoNotSpecialCaseSystemWithUsingStatics()
        => CheckAsync("""
            using M2;
            using M1;
            using System.Linq;
            using System;
            using static Microsoft.Win32.Registry;
            using static System.BitConverter;
            """, """
            using M1;
            using M2;
            using System;
            using System.Linq;
            using static Microsoft.Win32.Registry;
            using static System.BitConverter;
            """);

    [Fact]
    public Task IndentationAfterSorting()
        => CheckAsync("""
            namespace A
            {
                using V.W;
                using U;
                using X.Y.Z;

                class B { }
            }

            namespace U { }
            namespace V.W { }
            namespace X.Y.Z { }
            """, """
            namespace A
            {
                using U;
                using V.W;
                using X.Y.Z;

                class B { }
            }

            namespace U { }
            namespace V.W { }
            namespace X.Y.Z { }
            """);

    [Fact]
    public Task DoNotTouchCommentsAtBeginningOfFile1()
        => CheckAsync("""
            // Copyright (c) Microsoft Corporation.  All rights reserved.

            using B;
            // I like namespace A
            using A;

            namespace A { }
            namespace B { }
            """, """
            // Copyright (c) Microsoft Corporation.  All rights reserved.

            // I like namespace A
            using A;
            using B;

            namespace A { }
            namespace B { }
            """);

    [Fact]
    public Task DoNotTouchCommentsAtBeginningOfFile2()
        => CheckAsync("""
            /* Copyright (c) Microsoft Corporation.  All rights reserved. */

            using B;
            /* I like namespace A */
            using A;

            namespace A { }
            namespace B { }
            """, """
            /* Copyright (c) Microsoft Corporation.  All rights reserved. */

            /* I like namespace A */
            using A;
            using B;

            namespace A { }
            namespace B { }
            """);

    [Fact]
    public Task DoNotTouchCommentsAtBeginningOfFile3()
        => CheckAsync("""
            // Copyright (c) Microsoft Corporation.  All rights reserved.

            using B;
            /// I like namespace A
            using A;

            namespace A { }
            namespace B { }
            """, """
            // Copyright (c) Microsoft Corporation.  All rights reserved.

            /// I like namespace A
            using A;
            using B;

            namespace A { }
            namespace B { }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33251")]
    public Task DoNotTouchCommentsAtBeginningOfFile4()
        => CheckAsync("""
            /// Copyright (c) Microsoft Corporation.  All rights reserved.

            using B;
            /// I like namespace A
            using A;

            namespace A { }
            namespace B { }
            """, """
            /// Copyright (c) Microsoft Corporation.  All rights reserved.

            /// I like namespace A
            using A;
            using B;

            namespace A { }
            namespace B { }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33251")]
    public Task DoNotTouchCommentsAtBeginningOfFile5()
        => CheckAsync("""
            /** Copyright (c) Microsoft Corporation.  All rights reserved.
            */

            using B;
            /// I like namespace A
            using A;

            namespace A { }
            namespace B { }
            """, """
            /** Copyright (c) Microsoft Corporation.  All rights reserved.
            */

            /// I like namespace A
            using A;
            using B;

            namespace A { }
            namespace B { }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2480")]
    public Task DoTouchCommentsAtBeginningOfFile1()
        => CheckAsync("""
            // Copyright (c) Microsoft Corporation.  All rights reserved.
            using B;
            // I like namespace A
            using A;

            namespace A { }
            namespace B { }
            """, """
            // Copyright (c) Microsoft Corporation.  All rights reserved.
            // I like namespace A
            using A;
            using B;

            namespace A { }
            namespace B { }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2480")]
    public Task DoTouchCommentsAtBeginningOfFile2()
        => CheckAsync("""
            /* Copyright (c) Microsoft Corporation.  All rights reserved. */
            using B;
            /* I like namespace A */
            using A;

            namespace A { }
            namespace B { }
            """, """
            /* Copyright (c) Microsoft Corporation.  All rights reserved. */
            /* I like namespace A */
            using A;
            using B;

            namespace A { }
            namespace B { }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2480")]
    public Task DoTouchCommentsAtBeginningOfFile3()
        => CheckAsync("""
            /// Copyright (c) Microsoft Corporation.  All rights reserved.
            using B;
            /// I like namespace A
            using A;

            namespace A { }
            namespace B { }
            """, """
            /// I like namespace A
            using A;
            /// Copyright (c) Microsoft Corporation.  All rights reserved.
            using B;

            namespace A { }
            namespace B { }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2480")]
    public Task CommentsNotAtTheStartOfTheFile1()
        => CheckAsync("""
            namespace N
            {
                // attached to System.Text
                using System.Text;
                // attached to System
                using System;
            }
            """, """
            namespace N
            {
                // attached to System
                using System;
                // attached to System.Text
                using System.Text;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2480")]
    public Task CommentsNotAtTheStartOfTheFile2()
        => CheckAsync("""
            namespace N
            {
                // not attached to System.Text

                using System.Text;
                // attached to System
                using System;
            }
            """, """
            namespace N
            {
                // not attached to System.Text

                // attached to System
                using System;
                using System.Text;
            }
            """);

    [Fact]
    public async Task DoNotSortIfEndIfBlocks()
    {
        var initial =
            """
            using D;
            #if MYCONFIG
            using C;
            #else
            using B;
            #endif
            using A;

            namespace A { }
            namespace B { }
            namespace C { }
            namespace D { }
            """;

        var final = initial;
        await CheckAsync(initial, final);
    }

    [Fact]
    public Task ExternAliases()
        => CheckAsync("""
            extern alias Z;
            extern alias Y;
            extern alias X;
            using C;
            using U = C.L.T;
            using O = A.J;
            using A;
            using W = A.J.R;
            using N = B.K;
            using V = B.K.S;
            using M = C.L;
            using B;

            namespace A
            {
                namespace J
                {
                    class R { }
                }

            }
            namespace B
            {
                namespace K
                {
                    struct S { }
                }
            }
            namespace C
            {
                namespace L
                {
                    struct T { }
                }
            }
            """, """
            extern alias X;
            extern alias Y;
            extern alias Z;
            using A;
            using B;
            using C;
            using M = C.L;
            using N = B.K;
            using O = A.J;
            using U = C.L.T;
            using V = B.K.S;
            using W = A.J.R;

            namespace A
            {
                namespace J
                {
                    class R { }
                }

            }
            namespace B
            {
                namespace K
                {
                    struct S { }
                }
            }
            namespace C
            {
                namespace L
                {
                    struct T { }
                }
            }
            """);

    [Fact]
    public async Task DuplicateUsings()
    {
        var initial =
            """
            using A;
            using A;
            """;

        var final = initial;

        await CheckAsync(initial, final);
    }

    [Fact]
    public Task InlineComments()
        => CheckAsync("""
            /*00*/using/*01*/D/*02*/;/*03*/
            /*04*/using/*05*/C/*06*/;/*07*/
            /*08*/using/*09*/A/*10*/;/*11*/
            /*12*/using/*13*/B/*14*/;/*15*/
            /*16*/
            """, """
            /*08*/using/*09*/A/*10*/;/*11*/
            /*12*/using/*13*/B/*14*/;/*15*/
            /*04*/using/*05*/C/*06*/;/*07*/
            /*00*/using/*01*/D/*02*/;/*03*/
            /*16*/
            """);

    [Fact]
    public Task AllOnOneLine()
        => CheckAsync(@"using C; using B; using A;", """
            using A;
            using B; 
            using C; 
            """);

    [Fact]
    public Task InsideRegionBlock()
        => CheckAsync("""
            #region Using directives
            using C;
            using A;
            using B;
            #endregion

            class Class1
            {
            }
            """, """
            #region Using directives
            using A;
            using B;
            using C;
            #endregion

            class Class1
            {
            }
            """);

    [Fact]
    public async Task NestedRegionBlock()
    {
        var initial =
            """
            using C;
            #region Z
            using A;
            #endregion
            using B;
            """;

        var final = initial;

        await CheckAsync(initial, final);
    }

    [Fact]
    public async Task MultipleRegionBlocks()
    {
        var initial =
            """
            #region Using directives
            using C;
            #region Z
            using A;
            #endregion
            using B;
            #endregion
            """;

        var final = initial;

        await CheckAsync(initial, final);
    }

    [Fact]
    public Task InterleavedNewlines()
        => CheckAsync("""
            using B;

            using A;

            using C;

            class D { }
            """, """
            using A;
            using B;
            using C;

            class D { }
            """);

    [Fact]
    public Task InsideIfEndIfBlock()
        => CheckAsync("""
            #if !X
            using B;
            using A;
            using C;
            #endif
            """, """
            #if !X
            using A;
            using B;
            using C;
            #endif
            """);

    [Fact]
    public async Task IfEndIfBlockAbove()
    {
        var initial =
            """
            #if !X
            using C;
            using B;
            using F;
            #endif
            using D;
            using A;
            using E;
            """;

        var final = initial;
        await CheckAsync(initial, final);
    }

    [Fact]
    public async Task IfEndIfBlockMiddle()
    {
        var initial =
            """
            using D;
            using A;
            using H;
            #if !X
            using C;
            using B;
            using I;
            #endif
            using F;
            using E;
            using G;
            """;

        var final = initial;
        await CheckAsync(initial, final);
    }

    [Fact]
    public async Task IfEndIfBlockBelow()
    {
        var initial =
            """
            using D;
            using A;
            using E;
            #if !X
            using C;
            using B;
            using F;
            #endif
            """;

        var final = initial;
        await CheckAsync(initial, final);
    }

    [Fact]
    public Task Korean()
        => CheckAsync("""
            using 하;
            using 파;
            using 타;
            using 카;
            using 차;
            using 자;
            using 아;
            using 사;
            using 바;
            using 마;
            using 라;
            using 다;
            using 나;
            using 가;
            """, """
            using 가;
            using 나;
            using 다;
            using 라;
            using 마;
            using 바;
            using 사;
            using 아;
            using 자;
            using 차;
            using 카;
            using 타;
            using 파;
            using 하;

            """);

    [Fact]
    public Task DoNotSpecialCaseSystem1()
        => CheckAsync("""
            using B;
            using System.Collections.Generic;
            using C;
            using _System;
            using SystemZ;
            using D.System;
            using System;
            using System.Collections;
            using A;
            """, """
            using _System;
            using A;
            using B;
            using C;
            using D.System;
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using SystemZ;

            """, placeSystemNamespaceFirst: false);

    [Fact]
    public Task DoNotSpecialCaseSystem2()
        => CheckAsync("""
            extern alias S;
            extern alias R;
            extern alias T;
            using B;
            using System.Collections.Generic;
            using C;
            using _System;
            using SystemZ;
            using Y = System.UInt32;
            using Z = System.Int32;
            using D.System;
            using System;
            using N = System;
            using M = System.Collections;
            using System.Collections;
            using A;
            """, """
            extern alias R;
            extern alias S;
            extern alias T;
            using _System;
            using A;
            using B;
            using C;
            using D.System;
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using SystemZ;
            using M = System.Collections;
            using N = System;
            using Y = System.UInt32;
            using Z = System.Int32;

            """, placeSystemNamespaceFirst: false);

    [Fact]
    public async Task CaseSensitivity1()
    {
        string sortedKana;
        if (GlobalizationUtilities.ICUMode())
        {
            sortedKana =
                """
                using あ;
                using ｱ;
                using ああ;
                using あｱ;
                using ｱあ;
                using ｱｱ;
                using あア;
                using ｱア;
                using ア;
                using アあ;
                using アｱ;
                using アア;
                """;
        }
        else
        {
            sortedKana =
                """
                using ア;
                using ｱ;
                using あ;
                using アア;
                using アｱ;
                using ｱア;
                using ｱｱ;
                using アあ;
                using ｱあ;
                using あア;
                using あｱ;
                using ああ;
                """;
        }

        await CheckAsync("""
            using Bb;
            using B;
            using bB;
            using b;
            using Aa;
            using a;
            using A;
            using aa;
            using aA;
            using AA;
            using bb;
            using BB;
            using bBb;
            using bbB;
            using あ;
            using ア;
            using ｱ;
            using ああ;
            using あア;
            using あｱ;
            using アあ;
            using cC;
            using Cc;
            using アア;
            using アｱ;
            using ｱあ;
            using ｱア;
            using ｱｱ;
            using BBb;
            using BbB;
            using bBB;
            using BBB;
            using c;
            using C;
            using bbb;
            using Bbb;
            using cc;
            using cC;
            using CC;
            """, $"""
            using a;
            using A;
            using aa;
            using aA;
            using Aa;
            using AA;
            using b;
            using B;
            using bb;
            using bB;
            using Bb;
            using BB;
            using bbb;
            using bbB;
            using bBb;
            using bBB;
            using Bbb;
            using BbB;
            using BBb;
            using BBB;
            using c;
            using C;
            using cc;
            using cC;
            using cC;
            using Cc;
            using CC;
            {sortedKana}

            """);
    }

    [Fact]
    public async Task CaseSensitivity2()
    {
        var initial =
            """
            using あ;
            using ア;
            using ｱ;
            using ああ;
            using あア;
            using あｱ;
            using アあ;
            using アア;
            using アｱ;
            using ｱあ;
            using ｱア;
            using ｱｱ;
            """;

        if (GlobalizationUtilities.ICUMode())
        {
            await CheckAsync(initial,
                """
                using あ;
                using ｱ;
                using ああ;
                using あｱ;
                using ｱあ;
                using ｱｱ;
                using あア;
                using ｱア;
                using ア;
                using アあ;
                using アｱ;
                using アア;

                """);
        }
        else
        {
            await CheckAsync(initial,
                """
                using ア;
                using ｱ;
                using あ;
                using アア;
                using アｱ;
                using ｱア;
                using ｱｱ;
                using アあ;
                using ｱあ;
                using あア;
                using あｱ;
                using ああ;

                """);
        }
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20988")]
    public Task TestGrouping()
        => CheckAsync("""
            // Banner

            using Microsoft.CodeAnalysis.CSharp.Extensions;
            using Microsoft.CodeAnalysis.CSharp.Syntax;

            using System.Collections.Generic;
            using System.Linq;
            using Microsoft.CodeAnalysis.Shared.Extensions;
            using Roslyn.Utilities;
            using IntList = System.Collections.Generic.List<int>;
            using static System.Console;
            """, """
            // Banner

            using System.Collections.Generic;
            using System.Linq;

            using Microsoft.CodeAnalysis.CSharp.Extensions;
            using Microsoft.CodeAnalysis.CSharp.Syntax;
            using Microsoft.CodeAnalysis.Shared.Extensions;

            using Roslyn.Utilities;

            using static System.Console;

            using IntList = System.Collections.Generic.List<int>;

            """, placeSystemNamespaceFirst: true, separateImportGroups: true);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20988")]
    public Task TestGrouping2()
        => CheckAsync("""
            // Banner

            using System.Collections.Generic;
            using System.Linq;

            using Microsoft.CodeAnalysis.CSharp.Extensions;
            using Microsoft.CodeAnalysis.CSharp.Syntax;
            using Microsoft.CodeAnalysis.Shared.Extensions;

            using Roslyn.Utilities;

            using static System.Console;

            using IntList = System.Collections.Generic.List<int>;

            """, """
            // Banner

            using System.Collections.Generic;
            using System.Linq;

            using Microsoft.CodeAnalysis.CSharp.Extensions;
            using Microsoft.CodeAnalysis.CSharp.Syntax;
            using Microsoft.CodeAnalysis.Shared.Extensions;

            using Roslyn.Utilities;

            using static System.Console;

            using IntList = System.Collections.Generic.List<int>;

            """, placeSystemNamespaceFirst: true, separateImportGroups: true);

    [Theory, WorkItem(20988, "https://github.com/dotnet/roslyn/issues/19306")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public Task TestGrouping3(string endOfLine)
        => CheckAsync("""
            // Banner

            using Microsoft.CodeAnalysis.CSharp.Extensions;
            using Microsoft.CodeAnalysis.CSharp.Syntax;

            using System.Collections.Generic;
            using System.Linq;
            using Microsoft.CodeAnalysis.Shared.Extensions;
            using Roslyn.Utilities;
            using IntList = System.Collections.Generic.List<int>;
            using static System.Console;
            """, """
            // Banner

            using System.Collections.Generic;
            using System.Linq;

            using Microsoft.CodeAnalysis.CSharp.Extensions;
            using Microsoft.CodeAnalysis.CSharp.Syntax;
            using Microsoft.CodeAnalysis.Shared.Extensions;

            using Roslyn.Utilities;

            using static System.Console;

            using IntList = System.Collections.Generic.List<int>;

            """, placeSystemNamespaceFirst: true, separateImportGroups: true, endOfLine: endOfLine);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71502")]
    public Task GlobalsBeforeNonGlobals()
        => CheckAsync("""
            using A = B;
            using static C;
            using X;
            global using A = B;
            global using static C;
            global using X;
            """, """
            global using X;
            global using static C;
            global using A = B;
            using X;
            using static C;
            using A = B;

            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71502")]
    public Task GlobalNonNamedAliases1()
        => CheckAsync("""
            global using B = (int, string);
            global using A = int;
            """, """
            global using A = int;
            global using B = (int, string);

            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71502")]
    public Task GlobalNonNamedAliases2()
        => CheckAsync("""
            global using unsafe DataLogWriteFunc = delegate* unmanaged[Cdecl]<void*, byte*, nuint, void>;
            global using unsafe DataLogHandle = void*;
            """, """
            global using unsafe DataLogHandle = void*;
            global using unsafe DataLogWriteFunc = delegate* unmanaged[Cdecl]<void*, byte*, nuint, void>;

            """);
}
