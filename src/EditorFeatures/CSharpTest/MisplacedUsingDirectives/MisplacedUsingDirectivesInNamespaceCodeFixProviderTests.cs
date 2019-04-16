// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.MisplacedUsingDirectives;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MisplacedUsingDirectives
{
    /// <summary>
    /// Unit tests for the <see cref="MisplacedUsingDirectivesInNamespaceDiagnosticAnalyzer"/> and <see cref="MisplacedUsingDirectivesCodeFixProvider"/>.
    /// </summary>
    public class MisplacedUsingDirectivesInNamespaceCodeFixProviderTests : AbstractMisplacedUsingDirectivesCodeFixProviderTests
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return (new MisplacedUsingDirectivesInNamespaceDiagnosticAnalyzer(), new MisplacedUsingDirectivesCodeFixProvider());
        }

        #region Test Preserve

        /// <summary>
        /// Verifies that valid using statements in a namespace does not produce any diagnostics.
        /// </summary>
        [Fact]
        public Task WhenPreserve_UsingsInNamespace_ValidUsingStatements()
        {
            var testCode = @"namespace TestNamespace
{
    [|using System;
    using System.Threading;|]
}
";

            return TestDiagnosticMissingAsync(testCode, OutsidePreferPreservationOption);
        }

        /// <summary>
        /// Verifies that having using statements in the compilation unit will not produce any diagnostics, nor will
        /// having using statements inside a namespace.
        /// </summary>
        [Fact]
        public Task WhenPreserve_UsingsInCompilationUnitAndNamespace_ValidUsingStatements()
        {
            var testCode = @"using System;

namespace TestNamespace
{
    [|using System.Threading;|]
}
";

            return TestDiagnosticMissingAsync(testCode, OutsidePreferPreservationOption);
        }

        #endregion

        #region Test OutsideNamespace


        /// <summary>
        /// Verifies that valid using statements in the compilation unit does not produce any diagnostics.
        /// </summary>
        [Fact]
        public Task WhenOutsidePreferred_UsingsInCompilationUnit_ValidUsingStatements()
        {
            var testCode = @"[|using System;
using System.Threading;|]

namespace TestNamespace
{
}
";

            return TestDiagnosticMissingAsync(testCode, OutsideNamespaceOption);
        }

        /// <summary>
        /// Verifies that having using statements in the compilation unit will not produce any diagnostics when there are type definition present.
        /// </summary>
        /// <param name="typeDefinition">The type definition to test.</param>
        [Theory]
        [InlineData(ClassDefinition)]
        [InlineData(StructDefinition)]
        [InlineData(InterfaceDefinition)]
        [InlineData(EnumDefinition)]
        [InlineData(DelegateDefinition)]
        public Task WhenOutsidePreferred_UsingsInCompilationUnitWithMember_ValidUsingStatements(string typeDefinition)
        {
            var testCode = $@"[|using System;|]

{typeDefinition}
";

            return TestDiagnosticMissingAsync(testCode, OutsideNamespaceOption);
        }

        /// <summary>
        /// Verifies that using statements in a namespace produces the expected diagnostics.
        /// </summary>
        [Fact]
        public Task WhenOutsidePreferred_UsingsInNamespace_UsingsMoved()
        {
            var testCode = @"namespace TestNamespace
{
    [|using System;
    using System.Threading;|]
}
";
            var fixedTestCode = @"using System;
using System.Threading;

namespace TestNamespace
{
}
";

            return TestInRegularAndScriptAsync(testCode, fixedTestCode, OutsideNamespaceOption, placeSystemNamespaceFirst: true);
        }

        /// <summary>
        /// Verifies that simplified using statements in a namespace are expanded during the code fix operation.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public Task WhenOutsidePreferred_SimplifiedUsingInNamespace_UsingsMovedAndExpanded()
        {
            var testCode = @"namespace System
{
    [|using System;
    using System.Threading;
    using Reflection;|]
}
";
            var fixedTestCode = @"using System;
using System.Threading;
using System.Reflection;

namespace System
{
}
";

            return TestInRegularAndScriptAsync(testCode, fixedTestCode, OutsideNamespaceOption, placeSystemNamespaceFirst: true);
        }

        /// <summary>
        /// Verifies that simplified using statements in a namespace are expanded during the code fix operation.
        /// </summary>
        [Fact]
        public Task WhenOutsidePreferred_SimplifiedUsingAliasInNamespace_UsingsMovedAndExpanded()
        {
            var testCode = @"namespace System.MyExtension
{
    [|using System.Threading;
    using Reflection;
    using Assembly = Reflection.Assembly;
    using List = Collections.Generic.IList<int>;|]
}
";
            var fixedTestCode = @"using System.Threading;
using System.Reflection;
using Assembly = System.Reflection.Assembly;
using List = System.Collections.Generic.IList<int>;

namespace System.MyExtension
{
}
";

            return TestInRegularAndScriptAsync(testCode, fixedTestCode, OutsideNamespaceOption, placeSystemNamespaceFirst: true);
        }

        /// <summary>
        /// Verifies that having using statements in the compilation unit will not produce any diagnostics when there are attributes present.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public Task WhenOutsidePreferred_UsingsInNamespaceAndCompilationUnitWithAttributes_UsingsMoved()
        {
            var testCode = @"using System.Reflection;

[assembly: AssemblyVersion(""1.0.0.0"")]

namespace TestNamespace
{
    [|using System;
    using System.Threading;|]
}
";
            var fixedTestCode = @"using System.Reflection;
using System;
using System.Threading;

[assembly: AssemblyVersion(""1.0.0.0"")]

namespace TestNamespace
{
}
";

            return TestInRegularAndScriptAsync(testCode, fixedTestCode, OutsideNamespaceOption, placeSystemNamespaceFirst: true);
        }

        /// <summary>
        /// Verifies that the file header of a file is properly preserved when moving using statements out of a namespace.
        /// This is a regression test for #1941.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public Task WhenOutsidePreferred_UsingsInNamespaceAndCompilationUnitHasFileHeader_UsingsMovedAndHeaderPreserved()
        {
            var testCode = @"// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace TestNamespace
{
    [|using System;|]
}
";
            var fixedTestCode = @"// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;

namespace TestNamespace
{
}
";

            return TestInRegularAndScriptAsync(testCode, fixedTestCode, OutsideNamespaceOption, placeSystemNamespaceFirst: true);
        }

        [Fact]
        public Task WhenOutsidePreferred_UsingsInNamespaceWithCommentsAndCompilationUnitHasFileHeader_UsingsMovedWithCommentsAndHeaderPreserved()
        {
            var testCode = @"// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace TestNamespace
{
    // Separated Comment

    [|using System.Collections;
    // Comment
    using System;|]
}
";
            var fixedTestCode = @"// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

// Separated Comment

using System.Collections;
// Comment
using System;

namespace TestNamespace
{
}
";

            return TestInRegularAndScriptAsync(testCode, fixedTestCode, OutsideNamespaceOption, placeSystemNamespaceFirst: true);
        }

        [Fact]
        public Task WhenOutsidePreferred_UsingsInNamespace_UsingsMovedAndSystemPlacedFirstIgnored()
        {
            var testCode = @"namespace Foo
{
    [|using Microsoft.CodeAnalysis;
    using SystemAction = System.Action;
    using static System.Math;
    using System;

    using static System.String;
    using MyFunc = System.Func<int, bool>;

    using System.Collections.Generic;
    using System.Collections;|]

    public class Bar
    {
    }
}
";

            var fixedTestCode = @"using Microsoft.CodeAnalysis;
using SystemAction = System.Action;
using static System.Math;
using System;

using static System.String;
using MyFunc = System.Func<int, bool>;

using System.Collections.Generic;
using System.Collections;

namespace Foo
{
    public class Bar
    {
    }
}
";

            return TestInRegularAndScriptAsync(testCode, fixedTestCode, OutsideNamespaceOption, placeSystemNamespaceFirst: true);
        }

        [Fact]
        public Task WhenOutsidePreferred_UsingsInNamespace_UsingsMovedAndAlphaSortIgnored()
        {
            var testCode = @"namespace Foo
{
    [|using Microsoft.CodeAnalysis;
    using SystemAction = System.Action;
    using static System.Math;
    using System;

    using static System.String;
    using MyFunc = System.Func<int, bool>;

    using System.Collections.Generic;
    using System.Collections;|]

    public class Bar
    {
    }
}
";

            var fixedTestCode = @"using Microsoft.CodeAnalysis;
using SystemAction = System.Action;
using static System.Math;
using System;

using static System.String;
using MyFunc = System.Func<int, bool>;

using System.Collections.Generic;
using System.Collections;

namespace Foo
{
    public class Bar
    {
    }
}
";

            return TestInRegularAndScriptAsync(testCode, fixedTestCode, OutsideNamespaceOption, placeSystemNamespaceFirst: false);
        }

        #endregion
    }
}
