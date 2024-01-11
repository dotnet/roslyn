// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.OrganizeImports;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Workspaces.UnitTests.OrganizeImports;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.Organizing)]
public class OrganizeUsingsTests
{
    protected static async Task CheckAsync(
        string initial, string final,
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
    public async Task AliasesAtBottom()
    {
        var initial =
            """
            using A = B;
            using C;
            using D = E;
            using F;
            """;

        var final =
            """
            using C;
            using F;
            using A = B;
            using D = E;

            """;

        await CheckAsync(initial, final);
    }

    [Fact]
    public async Task UsingStaticsBetweenUsingsAndAliases()
    {
        var initial =
            """
            using static System.Convert;
            using A = B;
            using C;
            using Z;
            using D = E;
            using static System.Console;
            using F;
            """;

        var final =
            """
            using C;
            using F;
            using Z;
            using static System.Console;
            using static System.Convert;
            using A = B;
            using D = E;

            """;

        await CheckAsync(initial, final);
    }

    [Fact]
    public async Task NestedStatements()
    {
        var initial =
            """
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
            """;

        var final =
            """
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
            """;
        await CheckAsync(initial, final);
    }

    [Fact]
    public async Task FileScopedNamespace()
    {
        var initial =
            """
            using B;
            using A;

            namespace N;

            using D;
            using C;

            """;

        var final =
            """
            using A;
            using B;

            namespace N;

            using C;
            using D;

            """;
        await CheckAsync(initial, final);
    }

    [Fact]
    public async Task SpecialCaseSystem()
    {
        var initial =
            """
            using M2;
            using M1;
            using System.Linq;
            using System;
            """;

        var final =
            """
            using System;
            using System.Linq;
            using M1;
            using M2;

            """;
        await CheckAsync(initial, final, placeSystemNamespaceFirst: true);
    }

    [Fact]
    public async Task SpecialCaseSystemWithUsingStatic()
    {
        var initial =
            """
            using M2;
            using M1;
            using System.Linq;
            using System;
            using static Microsoft.Win32.Registry;
            using static System.BitConverter;
            """;

        var final =
            """
            using System;
            using System.Linq;
            using M1;
            using M2;
            using static System.BitConverter;
            using static Microsoft.Win32.Registry;

            """;
        await CheckAsync(initial, final, placeSystemNamespaceFirst: true);
    }

    [Fact]
    public async Task DoNotSpecialCaseSystem()
    {
        var initial =
            """
            using M2;
            using M1;
            using System.Linq;
            using System;
            """;

        var final =
            """
            using M1;
            using M2;
            using System;
            using System.Linq;

            """;

        await CheckAsync(initial, final);
    }

    [Fact]
    public async Task DoNotSpecialCaseSystemWithUsingStatics()
    {
        var initial =
            """
            using M2;
            using M1;
            using System.Linq;
            using System;
            using static Microsoft.Win32.Registry;
            using static System.BitConverter;
            """;

        var final =
            """
            using M1;
            using M2;
            using System;
            using System.Linq;
            using static Microsoft.Win32.Registry;
            using static System.BitConverter;
            """;
        await CheckAsync(initial, final);
    }

    [Fact]
    public async Task IndentationAfterSorting()
    {
        var initial =
            """
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
            """;

        var final =
            """
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
            """;

        await CheckAsync(initial, final);
    }

    [Fact]
    public async Task DoNotTouchCommentsAtBeginningOfFile1()
    {
        var initial =
            """
            // Copyright (c) Microsoft Corporation.  All rights reserved.

            using B;
            // I like namespace A
            using A;

            namespace A { }
            namespace B { }
            """;

        var final =
            """
            // Copyright (c) Microsoft Corporation.  All rights reserved.

            // I like namespace A
            using A;
            using B;

            namespace A { }
            namespace B { }
            """;

        await CheckAsync(initial, final);
    }

    [Fact]
    public async Task DoNotTouchCommentsAtBeginningOfFile2()
    {
        var initial =
            """
            /* Copyright (c) Microsoft Corporation.  All rights reserved. */

            using B;
            /* I like namespace A */
            using A;

            namespace A { }
            namespace B { }
            """;

        var final =
            """
            /* Copyright (c) Microsoft Corporation.  All rights reserved. */

            /* I like namespace A */
            using A;
            using B;

            namespace A { }
            namespace B { }
            """;

        await CheckAsync(initial, final);
    }

    [Fact]
    public async Task DoNotTouchCommentsAtBeginningOfFile3()
    {
        var initial =
            """
            // Copyright (c) Microsoft Corporation.  All rights reserved.

            using B;
            /// I like namespace A
            using A;

            namespace A { }
            namespace B { }
            """;

        var final =
            """
            // Copyright (c) Microsoft Corporation.  All rights reserved.

            /// I like namespace A
            using A;
            using B;

            namespace A { }
            namespace B { }
            """;

        await CheckAsync(initial, final);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33251")]
    public async Task DoNotTouchCommentsAtBeginningOfFile4()
    {
        var initial =
            """
            /// Copyright (c) Microsoft Corporation.  All rights reserved.

            using B;
            /// I like namespace A
            using A;

            namespace A { }
            namespace B { }
            """;

        var final =
            """
            /// Copyright (c) Microsoft Corporation.  All rights reserved.

            /// I like namespace A
            using A;
            using B;

            namespace A { }
            namespace B { }
            """;

        await CheckAsync(initial, final);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33251")]
    public async Task DoNotTouchCommentsAtBeginningOfFile5()
    {
        var initial =
            """
            /** Copyright (c) Microsoft Corporation.  All rights reserved.
            */

            using B;
            /// I like namespace A
            using A;

            namespace A { }
            namespace B { }
            """;

        var final =
            """
            /** Copyright (c) Microsoft Corporation.  All rights reserved.
            */

            /// I like namespace A
            using A;
            using B;

            namespace A { }
            namespace B { }
            """;

        await CheckAsync(initial, final);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2480")]
    public async Task DoTouchCommentsAtBeginningOfFile1()
    {
        var initial =
            """
            // Copyright (c) Microsoft Corporation.  All rights reserved.
            using B;
            // I like namespace A
            using A;

            namespace A { }
            namespace B { }
            """;

        var final =
            """
            // Copyright (c) Microsoft Corporation.  All rights reserved.
            // I like namespace A
            using A;
            using B;

            namespace A { }
            namespace B { }
            """;

        await CheckAsync(initial, final);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2480")]
    public async Task DoTouchCommentsAtBeginningOfFile2()
    {
        var initial =
            """
            /* Copyright (c) Microsoft Corporation.  All rights reserved. */
            using B;
            /* I like namespace A */
            using A;

            namespace A { }
            namespace B { }
            """;

        var final =
            """
            /* Copyright (c) Microsoft Corporation.  All rights reserved. */
            /* I like namespace A */
            using A;
            using B;

            namespace A { }
            namespace B { }
            """;

        await CheckAsync(initial, final);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2480")]
    public async Task DoTouchCommentsAtBeginningOfFile3()
    {
        var initial =
            """
            /// Copyright (c) Microsoft Corporation.  All rights reserved.
            using B;
            /// I like namespace A
            using A;

            namespace A { }
            namespace B { }
            """;

        var final =
            """
            /// I like namespace A
            using A;
            /// Copyright (c) Microsoft Corporation.  All rights reserved.
            using B;

            namespace A { }
            namespace B { }
            """;

        await CheckAsync(initial, final);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2480")]
    public async Task CommentsNotAtTheStartOfTheFile1()
    {
        var initial =
            """
            namespace N
            {
                // attached to System.Text
                using System.Text;
                // attached to System
                using System;
            }
            """;

        var final =
            """
            namespace N
            {
                // attached to System
                using System;
                // attached to System.Text
                using System.Text;
            }
            """;

        await CheckAsync(initial, final);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2480")]
    public async Task CommentsNotAtTheStartOfTheFile2()
    {
        var initial =
            """
            namespace N
            {
                // not attached to System.Text

                using System.Text;
                // attached to System
                using System;
            }
            """;

        var final =
            """
            namespace N
            {
                // not attached to System.Text

                // attached to System
                using System;
                using System.Text;
            }
            """;

        await CheckAsync(initial, final);
    }

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
    public async Task ExternAliases()
    {
        var initial =
            """
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
            """;

        var final =
            """
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
            """;

        await CheckAsync(initial, final);
    }

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
    public async Task InlineComments()
    {
        var initial =
            """
            /*00*/using/*01*/D/*02*/;/*03*/
            /*04*/using/*05*/C/*06*/;/*07*/
            /*08*/using/*09*/A/*10*/;/*11*/
            /*12*/using/*13*/B/*14*/;/*15*/
            /*16*/
            """;

        var final =
            """
            /*08*/using/*09*/A/*10*/;/*11*/
            /*12*/using/*13*/B/*14*/;/*15*/
            /*04*/using/*05*/C/*06*/;/*07*/
            /*00*/using/*01*/D/*02*/;/*03*/
            /*16*/
            """;

        await CheckAsync(initial, final);
    }

    [Fact]
    public async Task AllOnOneLine()
    {
        var initial =
@"using C; using B; using A;";

        var final =
            """
            using A;
            using B; 
            using C; 
            """;

        await CheckAsync(initial, final);
    }

    [Fact]
    public async Task InsideRegionBlock()
    {
        var initial =
            """
            #region Using directives
            using C;
            using A;
            using B;
            #endregion

            class Class1
            {
            }
            """;
        var final =
            """
            #region Using directives
            using A;
            using B;
            using C;
            #endregion

            class Class1
            {
            }
            """;

        await CheckAsync(initial, final);
    }

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
    public async Task InterleavedNewlines()
    {
        var initial =
            """
            using B;

            using A;

            using C;

            class D { }
            """;

        var final =
            """
            using A;
            using B;
            using C;

            class D { }
            """;

        await CheckAsync(initial, final);
    }

    [Fact]
    public async Task InsideIfEndIfBlock()
    {
        var initial =
            """
            #if !X
            using B;
            using A;
            using C;
            #endif
            """;

        var final =
            """
            #if !X
            using A;
            using B;
            using C;
            #endif
            """;

        await CheckAsync(initial, final);
    }

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
    public async Task Korean()
    {
        var initial =
            """
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
            """;

        var final =
            """
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

            """;

        await CheckAsync(initial, final);
    }

    [Fact]
    public async Task DoNotSpecialCaseSystem1()
    {
        var initial =
            """
            using B;
            using System.Collections.Generic;
            using C;
            using _System;
            using SystemZ;
            using D.System;
            using System;
            using System.Collections;
            using A;
            """;

        var final =
            """
            using _System;
            using A;
            using B;
            using C;
            using D.System;
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using SystemZ;

            """;

        await CheckAsync(initial, final, placeSystemNamespaceFirst: false);
    }

    [Fact]
    public async Task DoNotSpecialCaseSystem2()
    {
        var initial =
            """
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
            """;

        var final =
            """
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

            """;

        await CheckAsync(initial, final, placeSystemNamespaceFirst: false);
    }

    [Fact]
    public async Task CaseSensitivity1()
    {
        var initial =
            """
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
            """;

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

        var final =
            $"""
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

            """;
        await CheckAsync(initial, final);
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
    public async Task TestGrouping()
    {
        var initial =
            """
            // Banner

            using Microsoft.CodeAnalysis.CSharp.Extensions;
            using Microsoft.CodeAnalysis.CSharp.Syntax;

            using System.Collections.Generic;
            using System.Linq;
            using Microsoft.CodeAnalysis.Shared.Extensions;
            using Roslyn.Utilities;
            using IntList = System.Collections.Generic.List<int>;
            using static System.Console;
            """;

        var final =
            """
            // Banner

            using System.Collections.Generic;
            using System.Linq;

            using Microsoft.CodeAnalysis.CSharp.Extensions;
            using Microsoft.CodeAnalysis.CSharp.Syntax;
            using Microsoft.CodeAnalysis.Shared.Extensions;

            using Roslyn.Utilities;

            using static System.Console;

            using IntList = System.Collections.Generic.List<int>;

            """;

        await CheckAsync(initial, final, placeSystemNamespaceFirst: true, separateImportGroups: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20988")]
    public async Task TestGrouping2()
    {
        // Make sure we don't insert extra newlines if they're already there.
        var initial =
            """
            // Banner

            using System.Collections.Generic;
            using System.Linq;

            using Microsoft.CodeAnalysis.CSharp.Extensions;
            using Microsoft.CodeAnalysis.CSharp.Syntax;
            using Microsoft.CodeAnalysis.Shared.Extensions;

            using Roslyn.Utilities;

            using static System.Console;

            using IntList = System.Collections.Generic.List<int>;

            """;

        var final =
            """
            // Banner

            using System.Collections.Generic;
            using System.Linq;

            using Microsoft.CodeAnalysis.CSharp.Extensions;
            using Microsoft.CodeAnalysis.CSharp.Syntax;
            using Microsoft.CodeAnalysis.Shared.Extensions;

            using Roslyn.Utilities;

            using static System.Console;

            using IntList = System.Collections.Generic.List<int>;

            """;

        await CheckAsync(initial, final, placeSystemNamespaceFirst: true, separateImportGroups: true);
    }

    [Theory, WorkItem(20988, "https://github.com/dotnet/roslyn/issues/19306")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public async Task TestGrouping3(string endOfLine)
    {
        var initial =
            """
            // Banner

            using Microsoft.CodeAnalysis.CSharp.Extensions;
            using Microsoft.CodeAnalysis.CSharp.Syntax;

            using System.Collections.Generic;
            using System.Linq;
            using Microsoft.CodeAnalysis.Shared.Extensions;
            using Roslyn.Utilities;
            using IntList = System.Collections.Generic.List<int>;
            using static System.Console;
            """;

        var final =
            """
            // Banner

            using System.Collections.Generic;
            using System.Linq;

            using Microsoft.CodeAnalysis.CSharp.Extensions;
            using Microsoft.CodeAnalysis.CSharp.Syntax;
            using Microsoft.CodeAnalysis.Shared.Extensions;

            using Roslyn.Utilities;

            using static System.Console;

            using IntList = System.Collections.Generic.List<int>;

            """;

        await CheckAsync(initial, final, placeSystemNamespaceFirst: true, separateImportGroups: true, endOfLine: endOfLine);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71502")]
    public async Task GlobalsBeforeNonGlobals()
    {
        var initial = """
            using A = B;
            using static C;
            using X;
            global using A = B;
            global using static C;
            global using X;
            """;

        var final = """
            global using X;
            global using static C;
            global using A = B;
            using X;
            using static C;
            using A = B;

            """;

        await CheckAsync(initial, final);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71502")]
    public async Task GlobalNonNamedAliases1()
    {
        var initial = """
            global using B = (int, string);
            global using A = int;
            """;

        var final = """
            global using A = int;
            global using B = (int, string);

            """;

        await CheckAsync(initial, final);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71502")]
    public async Task GlobalNonNamedAliases2()
    {
        var initial = """
            global using unsafe DataLogWriteFunc = delegate* unmanaged[Cdecl]<void*, byte*, nuint, void>;
            global using unsafe DataLogHandle = void*;
            """;

        var final = """
            global using unsafe DataLogHandle = void*;
            global using unsafe DataLogWriteFunc = delegate* unmanaged[Cdecl]<void*, byte*, nuint, void>;

            """;

        await CheckAsync(initial, final);
    }
}
