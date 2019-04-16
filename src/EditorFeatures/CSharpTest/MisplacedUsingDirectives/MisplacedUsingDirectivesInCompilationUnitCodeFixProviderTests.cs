// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.MisplacedUsingDirectives;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MisplacedUsingDirectives
{
    /// <summary>
    /// Unit tests for the <see cref="MisplacedUsingDirectivesInCompilationDiagnosticAnalyzer"/> and <see cref="MisplacedUsingDirectivesCodeFixProvider"/>.
    /// </summary>
    public class MisplacedUsingDirectivesInCompilationUnitCodeFixProviderTests : AbstractMisplacedUsingDirectivesCodeFixProviderTests
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return (new MisplacedUsingDirectivesInCompilationDiagnosticAnalyzer(), new MisplacedUsingDirectivesCodeFixProvider());
        }

        #region Test Preserve

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
        public Task WhenPreserve_UsingsInCompilationUnitWithTypeDefinition_ValidUsingStatements(string typeDefinition)
        {
            var testCode = $@"[|using System;|]

{typeDefinition}
";

            return TestDiagnosticMissingAsync(testCode, InsidePreferPreservationOption);
        }

        /// <summary>
        /// Verifies that having using statements in the compilation unit will not produce any diagnostics when there are attributes present.
        /// </summary>
        [Fact]
        public Task WhenPreserve_UsingsInCompilationUnitWithAttributes_ValidUsingStatements()
        {
            var testCode = @"[|using System.Reflection;|]

[assembly: AssemblyVersion(""1.0.0.0"")]

namespace TestNamespace
{
    using System;
    using System.Threading;
}
";

            return TestDiagnosticMissingAsync(testCode, InsidePreferPreservationOption);
        }

        /// <summary>
        /// Verifies that having using statements in the compilation unit will not produce any diagnostics, even if they could be
        /// moved inside a namespace.
        /// </summary>
        [Fact]
        public Task WhenPreserve_UsingsInCompilationUnit_ValidUsingStatements()
        {
            var testCode = @"[|using System;
using System.Threading;|]

namespace TestNamespace
{
}
";

            return TestDiagnosticMissingAsync(testCode, InsidePreferPreservationOption);
        }

        /// <summary>
        /// Verifies that having using statements in the compilation unit will not produce any diagnostics, nor will
        /// having using statements inside a namespace.
        /// </summary>
        [Fact]
        public Task WhenPreserve_UsingsInCompilationUnitAndNamespace_ValidUsingStatements()
        {
            var testCode = @"[|using System;|]

namespace TestNamespace
{
    using System.Threading;
}
";

            return TestDiagnosticMissingAsync(testCode, InsidePreferPreservationOption);
        }

        #endregion

        #region Test InsideNamespace

        /// <summary>
        /// Verifies that valid using statements in a namespace does not produce any diagnostics.
        /// </summary>
        [Fact]
        public Task WhenInsidePreferred_UsingsInNamespace_ValidUsingStatements()
        {
            var testCode = @"namespace TestNamespace
{
    [|using System;
    using System.Threading;|]
}
";

            return TestDiagnosticMissingAsync(testCode, InsideNamespaceOption);
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
        public Task WhenInsidePreferred_UsingsInCompilationUnitWithTypeDefinition_ValidUsingStatements(string typeDefinition)
        {
            var testCode = $@"[|using System;|]

{typeDefinition}
";

            return TestDiagnosticMissingAsync(testCode, InsideNamespaceOption);
        }

        /// <summary>
        /// Verifies that having using statements in the compilation unit will not produce any diagnostics when there are attributes present.
        /// </summary>
        [Fact]
        public Task WhenInsidePreferred_UsingsInCompilationUnitWithAttributes_ValidUsingStatements()
        {
            var testCode = @"[|using System.Reflection;|]

[assembly: AssemblyVersion(""1.0.0.0"")]

namespace TestNamespace
{
    using System;
    using System.Threading;
}
";

            return TestDiagnosticMissingAsync(testCode, InsideNamespaceOption);
        }

        /// <summary>
        /// Verifies that the code fix will move the using directives and not place System directives first.
        /// </summary>
        [Fact]
        public Task WhenInsidePreferred_UsingsInCompilationUnit_UsingsMovedAndSystemPlacedFirstIgnored()
        {
            var testCode = @"[|using Microsoft.CodeAnalysis;
using SystemAction = System.Action;
using static System.Math;
using System;

using static System.String;
using MyFunc = System.Func<int, bool>;

using System.Collections.Generic;
using System.Collections;|]

namespace Foo
{
    public class Bar
    {
    }
}
";

            var fixedTestCode = @"namespace Foo
{
    using Microsoft.CodeAnalysis;
    using SystemAction = System.Action;
    using static System.Math;
    using System;

    using static System.String;
    using MyFunc = System.Func<int, bool>;

    using System.Collections.Generic;
    using System.Collections;

    public class Bar
    {
    }
}
";

            return TestInRegularAndScriptAsync(testCode, fixedTestCode, InsideNamespaceOption, placeSystemNamespaceFirst: true);
        }

        /// <summary>
        /// Verifies that the code fix will move the using directives and not sort them alphabetically.
        /// </summary>
        [Fact]
        public Task WhenInsidePreferred_UsingsInCompilationUnit_UsingsAndWithAlphaSortIgnored()
        {
            var testCode = @"[|using Microsoft.CodeAnalysis;
using SystemAction = System.Action;
using static System.Math;
using System;

using static System.String;
using MyFunc = System.Func<int, bool>;

using System.Collections.Generic;
using System.Collections;|]

namespace NamespaceName
{
    public class Bar
    {
    }
}
";

            var fixedTestCode = @"namespace NamespaceName
{
    using Microsoft.CodeAnalysis;
    using SystemAction = System.Action;
    using static System.Math;
    using System;

    using static System.String;
    using MyFunc = System.Func<int, bool>;

    using System.Collections.Generic;
    using System.Collections;

    public class Bar
    {
    }
}
";

            return TestInRegularAndScriptAsync(testCode, fixedTestCode, InsideNamespaceOption, placeSystemNamespaceFirst: false);
        }

        /// <summary>
        /// Verifies that the code fix will move the using directives, but will not move a file header comment separated by an new line.
        /// </summary>
        [Fact]
        public Task WhenInsidePreferred_UsingsInCompilationUnitWithFileHeader_UsingsMovedNotHeader()
        {
            var testCode = @"// This is a file header.
[|using Microsoft.CodeAnalysis;
using System;|]

namespace TestNamespace
{
}
";

            var fixedTestCode = @"// This is a file header.
namespace TestNamespace
{
    using Microsoft.CodeAnalysis;
    using System;
}
";

            return TestInRegularAndScriptAsync(testCode, fixedTestCode, InsideNamespaceOption, placeSystemNamespaceFirst: true);
        }

        /// <summary>
        /// Verifies that the code fix will properly move separated trivia, but will not move a file header comment.
        /// </summary>
        [Fact]
        public Task WhenInsidePreferred_UsingsInCompilationUnitWithFileHeaderAndTrivia_UsingsAndTriviaMovedNotHeader()
        {
            var testCode = @"// File Header

// Leading Comment

[|using Microsoft.CodeAnalysis;
using System;|]

namespace TestNamespace
{
}
";

            var fixedTestCode = @"// File Header

namespace TestNamespace
{
    // Leading Comment

    using Microsoft.CodeAnalysis;
    using System;
}
";

            return TestInRegularAndScriptAsync(testCode, fixedTestCode, InsideNamespaceOption, placeSystemNamespaceFirst: true);
        }

        /// <summary>
        /// Verifies that a code fix will not be offered for MisplacedUsing diagnostics when multiple namespaces are present.
        /// </summary>
        [Fact]
        public Task WhenInsidePreferred_UsingsInCompilationUnitWithMultipleNamespaces_NoCodeFixOffered()
        {
            var testCode = @"[|using System;|]

namespace TestNamespace1
{
    public class TestClass1
    {
    }
}

namespace TestNamespace2
{
}
";

            return TestMissingAsync(testCode, InsideNamespaceOption);
        }

        /// <summary>
        /// Verifies that the code fix will properly move pragmas.
        /// </summary>
        [Fact]
        public Task WhenInsidePreferred_UsingsInCompilationUnitWithPragma_PragmaMoved()
        {
            var testCode = @"#pragma warning disable 1573 // Comment
[|using System;
using System.Threading;|]

namespace TestNamespace
{
}
";

            var fixedTestCode = @"namespace TestNamespace
{
#pragma warning disable 1573 // Comment
    using System;
    using System.Threading;
}
";

            return TestInRegularAndScriptAsync(testCode, fixedTestCode, InsideNamespaceOption, placeSystemNamespaceFirst: true);
        }

        /// <summary>
        /// Verifies that the code fix will properly move regions.
        /// </summary>
        [Fact]
        public Task WhenInsidePreferred_UsingsInCompilationUnitWithRegion_RegionMoved()
        {
            var testCode = @"#region Comment
#endregion Comment
[|using System;
using System.Threading;|]

namespace TestNamespace
{
}
";

            var fixedTestCode = @"namespace TestNamespace
{
    #region Comment
    #endregion Comment
    using System;
    using System.Threading;
}
";

            return TestInRegularAndScriptAsync(testCode, fixedTestCode, InsideNamespaceOption, placeSystemNamespaceFirst: true);
        }

        /// <summary>
        /// Verifies that the code fix will properly move comment trivia.
        /// </summary>
        [Fact]
        public Task WhenInsidePreferred_UsingsInCompilationUnitWithCommentTrivia_TriviaMoved()
        {
            var testCode = @"
// Some comment
[|using System;
using System.Threading;|]

namespace TestNamespace
{
}
";

            var fixedTestCode = @"namespace TestNamespace
{

    // Some comment
    using System;
    using System.Threading;
}
";

            return TestInRegularAndScriptAsync(testCode, fixedTestCode, InsideNamespaceOption, placeSystemNamespaceFirst: true);
        }

        #endregion
    }
}
