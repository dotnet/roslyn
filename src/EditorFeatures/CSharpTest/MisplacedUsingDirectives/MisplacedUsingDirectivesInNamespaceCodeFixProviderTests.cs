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
            var fixedTestCode = @"{|Warning:using System;|}
{|Warning:using System.Threading;|}

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
            var fixedTestCode = @"{|Warning:using System;|}
{|Warning:using System.Threading;|}
{|Warning:using System.Reflection;|}

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
            var fixedTestCode = @"{|Warning:using System.Threading;|}
{|Warning:using System.Reflection;|}
{|Warning:using Assembly = System.Reflection.Assembly;|}
{|Warning:using List = System.Collections.Generic.IList<int>;|}

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
{|Warning:using System;|}
{|Warning:using System.Threading;|}

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

{|Warning:using System;|}

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

{|Warning:using System.Collections;|}
// Comment
{|Warning:using System;|}

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

            var fixedTestCode = @"{|Warning:using Microsoft.CodeAnalysis;|}
{|Warning:using SystemAction = System.Action;|}
{|Warning:using static System.Math;|}
{|Warning:using System;|}

{|Warning:using static System.String;|}
{|Warning:using MyFunc = System.Func<int, bool>;|}

{|Warning:using System.Collections.Generic;|}
{|Warning:using System.Collections;|}

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

            var fixedTestCode = @"{|Warning:using Microsoft.CodeAnalysis;|}
{|Warning:using SystemAction = System.Action;|}
{|Warning:using static System.Math;|}
{|Warning:using System;|}

{|Warning:using static System.String;|}
{|Warning:using MyFunc = System.Func<int, bool>;|}

{|Warning:using System.Collections.Generic;|}
{|Warning:using System.Collections;|}

namespace Foo
{
    public class Bar
    {
    }
}
";

            return TestInRegularAndScriptAsync(testCode, fixedTestCode, OutsideNamespaceOption, placeSystemNamespaceFirst: false);
        }

        /// <summary>
        /// Verifies that simplified using statements in nested namespace are expanded during the code fix operation.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public Task WhenOutsidePreferred_UsingsInNestedNamespaces_UsingsMovedAndExpanded()
        {
            var testCode = @"using System;

namespace System.Namespace
{
    // Outer Comment
    [|using Threading;

    namespace OtherNamespace
    {
        // Inner Comment
        using Reflection;|]
    }
}
";
            var fixedTestCode = @"using System;
// Outer Comment
{|Warning:using System.Threading;|}
// Inner Comment
{|Warning:using System.Reflection;|}

namespace System.Namespace
{
    namespace OtherNamespace
    {
    }
}
";

            return TestInRegularAndScriptAsync(testCode, fixedTestCode, OutsideNamespaceOption, placeSystemNamespaceFirst: true);
        }

        /// <summary>
        /// Verifies that simplified using statements in multiple namespaces are expanded during the code fix operation.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public Task WhenOutsidePreferred_UsingsInMultipleNamespaces_UsingsMovedAndExpanded()
        {
            var testCode = @"using System;

namespace System.Namespace
{
    // A Comment
    [|using Threading;
}

namespace System.OtherNamespace
{
    // Another Comment
    using Reflection;|]
}
";
            var fixedTestCode = @"using System;
// A Comment
{|Warning:using System.Threading;|}
// Another Comment
{|Warning:using System.Reflection;|}

namespace System.Namespace
{
}

namespace System.OtherNamespace
{
}
";

            return TestInRegularAndScriptAsync(testCode, fixedTestCode, OutsideNamespaceOption, placeSystemNamespaceFirst: true);
        }

        /// <summary>
        /// Verifies that simplified using statements in multiple namespaces are deduplicated during the code fix operation.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public Task WhenOutsidePreferred_UsingsInMultipleNamespaces_UsingsMovedAndDeduplicated()
        {
            var testCode = @"using System;

namespace System.Namespace
{
    // Orphaned Comment 1
    [|using System;
    // A Comment
    using Threading;
}

namespace B
{
    // Orphaned Comment 2
    using System.Threading;|]
}
";
            var fixedTestCode = @"using System;
// Orphaned Comment 1
// A Comment
{|Warning:using System.Threading;|}
// Orphaned Comment 2

namespace System.Namespace
{
}

namespace B
{
}
";

            return TestInRegularAndScriptAsync(testCode, fixedTestCode, OutsideNamespaceOption, placeSystemNamespaceFirst: true);
        }

        #endregion
    }
}
