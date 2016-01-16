// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.OrganizeImports;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Organizing
{
    public class OrganizeUsingsTests
    {
        protected async Task CheckAsync(string initial, string final, bool specialCaseSystem, CSharpParseOptions options = null)
        {
            using (var workspace = await TestWorkspaceFactory.CreateCSharpAsync(initial))
            {
                var document = workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id);
                var newRoot = await (await OrganizeImportsService.OrganizeImportsAsync(document, specialCaseSystem)).GetSyntaxRootAsync();
                Assert.Equal(final.NormalizeLineEndings(), newRoot.ToFullString());
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task EmptyFile()
        {
            await CheckAsync(string.Empty, string.Empty, true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task SingleUsingStatement()
        {
            var initial = @"using A;";
            var final = initial;
            await CheckAsync(initial, final, true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task AliasesAtBottom()
        {
            var initial =
@"using A = B;
using C;
using D = E;
using F;";

            var final =
@"using C;
using F;
using A = B;
using D = E;
";

            await CheckAsync(initial, final, false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task UsingStaticsBetweenUsingsAndAliases()
        {
            var initial =
@"using static System.Convert;
using A = B;
using C;
using Z;
using D = E;
using static System.Console;
using F;";

            var final =
@"using C;
using F;
using Z;
using static System.Console;
using static System.Convert;
using A = B;
using D = E;
";

            await CheckAsync(initial, final, false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task NestedStatements()
        {
            var initial =
@"using B;
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
}";

            var final =
@"using A;
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
}";
            await CheckAsync(initial, final, true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task SpecialCaseSystem()
        {
            var initial =
@"using M2;
using M1;
using System.Linq;
using System;";

            var final =
@"using System;
using System.Linq;
using M1;
using M2;
";
            await CheckAsync(initial, final, true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task SpecialCaseSystemWithUsingStatic()
        {
            var initial =
@"using M2;
using M1;
using System.Linq;
using System;
using static Microsoft.Win32.Registry;
using static System.BitConverter;";

            var final =
@"using System;
using System.Linq;
using M1;
using M2;
using static System.BitConverter;
using static Microsoft.Win32.Registry;
";
            await CheckAsync(initial, final, true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task DoNotSpecialCaseSystem()
        {
            var initial =
@"using M2;
using M1;
using System.Linq;
using System;";

            var final =
@"using M1;
using M2;
using System;
using System.Linq;
";

            await CheckAsync(initial, final, false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task DoNotSpecialCaseSystemWithUsingStatics()
        {
            var initial =
@"using M2;
using M1;
using System.Linq;
using System;
using static Microsoft.Win32.Registry;
using static System.BitConverter;";

            var final =
@"using M1;
using M2;
using System;
using System.Linq;
using static Microsoft.Win32.Registry;
using static System.BitConverter;";
            await CheckAsync(initial, final, false);
        }

        [WpfFact(Skip = "752024"), Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task MissingSemicolons()
        {
            var initial =
@"using B
using A
using C";

            var final =
@"using A
using B
using C";
            await CheckAsync(initial, final, true);
        }

        [WpfFact(Skip = "752024"), Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task MissingNamesAndSemicolons()
        {
            var initial =
@"using B
using 
using A";

            var final =
@"using
using A
using B
";
            await CheckAsync(initial, final, true);
        }

        [WpfFact(Skip = "752024"), Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task MissingEverything()
        {
            var initial =
@"extern alias C
extern alias;
extern alias A
extern alias
extern alias 
extern alias B
using
using C
using 
using B
using;
using A
using D = 
using E = X
using;
using F = X.Y
using 
using
using D = Z";

            var final =
@"extern alias;
extern alias
extern alias
extern alias A
extern alias B
extern alias C
using
using
using;
using;
using
using
using A
using B
using C
using D =
using D = Z
using E = X
using F = X.Y
";
            await CheckAsync(initial, final, true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task IndentationAfterSorting()
        {
            var initial =
@"namespace A
{
    using V.W;
    using U;
    using X.Y.Z;

    class B { }
}

namespace U { }
namespace V.W { }
namespace X.Y.Z { }";

            var final =
@"namespace A
{
    using U;
    using V.W;
    using X.Y.Z;

    class B { }
}

namespace U { }
namespace V.W { }
namespace X.Y.Z { }";

            await CheckAsync(initial, final, true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task DoNotTouchCommentsAtBeginningOfFile1()
        {
            var initial =
@"// Copyright (c) Microsoft Corporation.  All rights reserved.

using B;
// I like namespace A
using A;

namespace A { }
namespace B { }";

            var final =
@"// Copyright (c) Microsoft Corporation.  All rights reserved.

// I like namespace A
using A;
using B;

namespace A { }
namespace B { }";

            await CheckAsync(initial, final, true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task DoNotTouchCommentsAtBeginningOfFile2()
        {
            var initial =
@"/* Copyright (c) Microsoft Corporation.  All rights reserved. */

using B;
/* I like namespace A */
using A;

namespace A { }
namespace B { }";

            var final =
@"/* Copyright (c) Microsoft Corporation.  All rights reserved. */

/* I like namespace A */
using A;
using B;

namespace A { }
namespace B { }";

            await CheckAsync(initial, final, true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task DoNotTouchCommentsAtBeginningOfFile3()
        {
            var initial =
@"// Copyright (c) Microsoft Corporation.  All rights reserved.

using B;
/// I like namespace A
using A;

namespace A { }
namespace B { }";

            var final =
@"// Copyright (c) Microsoft Corporation.  All rights reserved.

/// I like namespace A
using A;
using B;

namespace A { }
namespace B { }";

            await CheckAsync(initial, final, true);
        }

        [WorkItem(2480, "https://github.com/dotnet/roslyn/issues/2480")]
        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task DoTouchCommentsAtBeginningOfFile1()
        {
            var initial =
@"// Copyright (c) Microsoft Corporation.  All rights reserved.
using B;
// I like namespace A
using A;

namespace A { }
namespace B { }";

            var final =
@"// Copyright (c) Microsoft Corporation.  All rights reserved.
// I like namespace A
using A;
using B;

namespace A { }
namespace B { }";

            await CheckAsync(initial, final, true);
        }

        [WorkItem(2480, "https://github.com/dotnet/roslyn/issues/2480")]
        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task DoTouchCommentsAtBeginningOfFile2()
        {
            var initial =
@"/* Copyright (c) Microsoft Corporation.  All rights reserved. */
using B;
/* I like namespace A */
using A;

namespace A { }
namespace B { }";

            var final =
@"/* Copyright (c) Microsoft Corporation.  All rights reserved. */
/* I like namespace A */
using A;
using B;

namespace A { }
namespace B { }";

            await CheckAsync(initial, final, true);
        }

        [WorkItem(2480, "https://github.com/dotnet/roslyn/issues/2480")]
        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task DoTouchCommentsAtBeginningOfFile3()
        {
            var initial =
@"/// Copyright (c) Microsoft Corporation.  All rights reserved.
using B;
/// I like namespace A
using A;

namespace A { }
namespace B { }";

            var final =
@"/// I like namespace A
using A;
/// Copyright (c) Microsoft Corporation.  All rights reserved.
using B;

namespace A { }
namespace B { }";

            await CheckAsync(initial, final, true);
        }

        [WorkItem(2480, "https://github.com/dotnet/roslyn/issues/2480")]
        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task CommentsNotAtTheStartOfTheFile1()
        {
            var initial =
@"namespace N
{
    // attached to System.Text
    using System.Text;
    // attached to System
    using System;
}";

            var final =
@"namespace N
{
    // attached to System
    using System;
    // attached to System.Text
    using System.Text;
}";

            await CheckAsync(initial, final, true);
        }

        [WorkItem(2480, "https://github.com/dotnet/roslyn/issues/2480")]
        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task CommentsNotAtTheStartOfTheFile2()
        {
            var initial =
@"namespace N
{
    // not attached to System.Text

    using System.Text;
    // attached to System
    using System;
}";

            var final =
@"namespace N
{
    // not attached to System.Text

    // attached to System
    using System;
    using System.Text;
}";

            await CheckAsync(initial, final, true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task DoNotSortIfEndIfBlocks()
        {
            var initial =
@"using D;
#if MYCONFIG
using C;
#else
using B;
#endif
using A;

namespace A { }
namespace B { }
namespace C { }
namespace D { }";

            var final = initial;
            await CheckAsync(initial, final, true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task ExternAliases()
        {
            var initial =
@"extern alias Z;
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
}";

            var final =
@"extern alias X;
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
}";

            await CheckAsync(initial, final, true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task DuplicateUsings()
        {
            var initial =
@"using A;
using A;";

            var final = initial;

            await CheckAsync(initial, final, true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task InlineComments()
        {
            var initial =
@"/*00*/using/*01*/D/*02*/;/*03*/
/*04*/using/*05*/C/*06*/;/*07*/
/*08*/using/*09*/A/*10*/;/*11*/
/*12*/using/*13*/B/*14*/;/*15*/
/*16*/";

            var final =
@"/*08*/using/*09*/A/*10*/;/*11*/
/*12*/using/*13*/B/*14*/;/*15*/
/*04*/using/*05*/C/*06*/;/*07*/
/*00*/using/*01*/D/*02*/;/*03*/
/*16*/";

            await CheckAsync(initial, final, true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task AllOnOneLine()
        {
            var initial =
@"using C; using B; using A;";

            var final =
@"using A;
using B; 
using C; ";

            await CheckAsync(initial, final, true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task InsideRegionBlock()
        {
            var initial =
@"#region Using directives
using C;
using A;
using B;
#endregion

class Class1
{
}";
            var final =
@"#region Using directives
using A;
using B;
using C;
#endregion

class Class1
{
}";

            await CheckAsync(initial, final, true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task NestedRegionBlock()
        {
            var initial =
@"using C;
#region Z
using A;
#endregion
using B;";

            var final = initial;

            await CheckAsync(initial, final, true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task MultipleRegionBlocks()
        {
            var initial =
@"#region Using directives
using C;
#region Z
using A;
#endregion
using B;
#endregion";

            var final = initial;

            await CheckAsync(initial, final, true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task InterleavedNewlines()
        {
            var initial =
@"using B;

using A;

using C;

class D { }";

            var final =
@"using A;
using B;
using C;

class D { }";

            await CheckAsync(initial, final, true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task InsideIfEndIfBlock()
        {
            var initial =
@"#if !X
using B;
using A;
using C;
#endif";

            var final =
@"#if !X
using A;
using B;
using C;
#endif";

            await CheckAsync(initial, final, true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task IfEndIfBlockAbove()
        {
            var initial =
@"#if !X
using C;
using B;
using F;
#endif
using D;
using A;
using E;";

            var final = initial;
            await CheckAsync(initial, final, true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task IfEndIfBlockMiddle()
        {
            var initial =
@"using D;
using A;
using H;
#if !X
using C;
using B;
using I;
#endif
using F;
using E;
using G;";

            var final = initial;
            await CheckAsync(initial, final, true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task IfEndIfBlockBelow()
        {
            var initial =
@"using D;
using A;
using E;
#if !X
using C;
using B;
using F;
#endif";

            var final = initial;
            await CheckAsync(initial, final, true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task Korean()
        {
            var initial =
@"using 하;
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
using 가;";

            var final =
@"using 가;
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
";

            await CheckAsync(initial, final, true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task DoNotSpecialCaseSystem1()
        {
            var initial =
@"using B;
using System.Collections.Generic;
using C;
using _System;
using SystemZ;
using D.System;
using System;
using System.Collections;
using A;";

            var final =
@"using _System;
using A;
using B;
using C;
using D.System;
using System;
using System.Collections;
using System.Collections.Generic;
using SystemZ;
";

            await CheckAsync(initial, final, specialCaseSystem: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task DoNotSpecialCaseSystem2()
        {
            var initial =
@"extern alias S;
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
using A;";

            var final =
@"extern alias R;
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
";

            await CheckAsync(initial, final, specialCaseSystem: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task CaseSensitivity1()
        {
            var initial =
@"using Bb;
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

// If Kana is sensitive あ != ア, if Kana is insensitive あ == ア.
// If Width is sensitiveア != ｱ, if Width is insensitive ア == ｱ.";

            var final =
@"using a;
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

// If Kana is sensitive あ != ア, if Kana is insensitive あ == ア.
// If Width is sensitiveア != ｱ, if Width is insensitive ア == ｱ.";
            await CheckAsync(initial, final, specialCaseSystem: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task CaseSensitivity2()
        {
            var initial =
@"using あ;
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
using ｱｱ;";

            var final =
@"using ア;
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
";

            await CheckAsync(initial, final, specialCaseSystem: true);
        }
    }
}
