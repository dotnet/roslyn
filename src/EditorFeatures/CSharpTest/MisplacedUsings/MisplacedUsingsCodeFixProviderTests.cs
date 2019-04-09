﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImports;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.MisplacedUsings;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MisplacedUsings
{
    using Verify = CSharpCodeFixVerifier<MisplacedUsingsDiagnosticAnalyzer, MisplacedUsingsCodeFixProvider, XUnitVerifier>;

    /// <summary>
    /// Unit tests for the <see cref="MisplacedUsingsDiagnosticAnalyzer"/> and <see cref="MisplacedUsingsCodeFixProvider"/>.
    /// </summary>
    public class MisplacedUsingsCodeFixProviderTests
    {
        private static readonly CodeStyleOption<AddImportPlacement> s_preservePlacementOption =
           new CodeStyleOption<AddImportPlacement>(AddImportPlacement.Preserve, NotificationOption.None);

        private static readonly CodeStyleOption<AddImportPlacement> s_insideNamespaceOption =
            new CodeStyleOption<AddImportPlacement>(AddImportPlacement.InsideNamespace, NotificationOption.Error);

        private static readonly CodeStyleOption<AddImportPlacement> s_outsideNamespaceOption =
            new CodeStyleOption<AddImportPlacement>(AddImportPlacement.OutsideNamespace, NotificationOption.Error);


        private const string ClassDefinition = @"public class TestClass
{
}";

        private const string StructDefinition = @"public struct TestStruct
{
}";

        private const string InterfaceDefinition = @"public interface TestInterface
{
}";

        private const string EnumDefinition = @"public enum TestEnum
{
    TestValue
}";

        private const string DelegateDefinition = @"public delegate void TestDelegate();";

        #region Test Preserve

        /// <summary>
        /// Verifies that valid using statements in a namespace does not produce any diagnostics.
        /// </summary>
        [Fact]
        public Task WhenPreserve_UsingsInNamespace_ValidUsingStatements()
        {
            var testCode = @"namespace TestNamespace
{
    using System;
    using System.Threading;
}
";

            return VerifyAnalyzerAsync(testCode, s_preservePlacementOption, DiagnosticResult.EmptyDiagnosticResults);
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
        public Task WhenPreserve_UsingsInCompilationUnitWithTypeDefinition_ValidUsingStatements(string typeDefinition)
        {
            var testCode = $@"using System;

{typeDefinition}
";

            return VerifyAnalyzerAsync(testCode, s_preservePlacementOption, DiagnosticResult.EmptyDiagnosticResults);
        }

        /// <summary>
        /// Verifies that having using statements in the compilation unit will not produce any diagnostics when there are attributes present.
        /// </summary>
        [Fact]
        public Task WhenPreserve_UsingsInCompilationUnitWithAttributes_ValidUsingStatements()
        {
            var testCode = @"using System.Reflection;

[assembly: AssemblyVersion(""1.0.0.0"")]

namespace TestNamespace
{
    using System;
    using System.Threading;
}
";

            return VerifyAnalyzerAsync(testCode, s_preservePlacementOption, DiagnosticResult.EmptyDiagnosticResults);
        }

        /// <summary>
        /// Verifies that having using statements in the compilation unit will not produce any diagnostics, even if they could be
        /// moved inside a namespace.
        /// </summary>
        [Fact]
        public Task WhenPreserve_UsingsInCompilationUnit_ValidUsingStatements()
        {
            var testCode = @"using System;
using System.Threading;

namespace TestNamespace
{
}
";

            return VerifyAnalyzerAsync(testCode, s_preservePlacementOption, DiagnosticResult.EmptyDiagnosticResults);
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
    using System.Threading;
}
";

            return VerifyAnalyzerAsync(testCode, s_preservePlacementOption, DiagnosticResult.EmptyDiagnosticResults);
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
    using System;
    using System.Threading;
}
";

            return VerifyAnalyzerAsync(testCode, s_insideNamespaceOption, DiagnosticResult.EmptyDiagnosticResults);
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
            var testCode = $@"using System;

{typeDefinition}
";

            return VerifyAnalyzerAsync(testCode, s_insideNamespaceOption, DiagnosticResult.EmptyDiagnosticResults);
        }

        /// <summary>
        /// Verifies that having using statements in the compilation unit will not produce any diagnostics when there are attributes present.
        /// </summary>
        [Fact]
        public Task WhenInsidePreferred_UsingsInCompilationUnitWithAttributes_ValidUsingStatements()
        {
            var testCode = @"using System.Reflection;

[assembly: AssemblyVersion(""1.0.0.0"")]

namespace TestNamespace
{
    using System;
    using System.Threading;
}
";

            return VerifyAnalyzerAsync(testCode, s_insideNamespaceOption, DiagnosticResult.EmptyDiagnosticResults);
        }

        /// <summary>
        /// Verifies that the code fix will move the using directives and not place System directives first.
        /// </summary>
        [Fact]
        public Task WhenInsidePreferred_UsingsInCompilationUnit_UsingsMovedAndSystemPlacedFirstIgnored()
        {
            var testCode = @"using Microsoft.CodeAnalysis;
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

            var expected = new DiagnosticResult[]
            {
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._insideDescriptor).WithLocation(1, 1),
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._insideDescriptor).WithLocation(2, 1),
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._insideDescriptor).WithLocation(3, 1),
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._insideDescriptor).WithLocation(4, 1),
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._insideDescriptor).WithLocation(6, 1),
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._insideDescriptor).WithLocation(7, 1),
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._insideDescriptor).WithLocation(9, 1),
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._insideDescriptor).WithLocation(10, 1),
            };

            return VerifyCodeFixAsync(testCode, s_insideNamespaceOption, expected, fixedTestCode, placeSystemNamespaceFirst: true);
        }

        /// <summary>
        /// Verifies that the code fix will move the using directives and not sort them alphabetically.
        /// </summary>
        [Fact]
        public Task WhenInsidePreferred_UsingsInCompilationUnit_UsingsAndWithAlphaSortIgnored()
        {
            var testCode = @"using Microsoft.CodeAnalysis;
using SystemAction = System.Action;
using static System.Math;
using System;

using static System.String;
using MyFunc = System.Func<int, bool>;

using System.Collections.Generic;
using System.Collections;

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

            var expected = new DiagnosticResult[]
            {
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._insideDescriptor).WithLocation(1, 1),
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._insideDescriptor).WithLocation(2, 1),
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._insideDescriptor).WithLocation(3, 1),
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._insideDescriptor).WithLocation(4, 1),
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._insideDescriptor).WithLocation(6, 1),
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._insideDescriptor).WithLocation(7, 1),
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._insideDescriptor).WithLocation(9, 1),
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._insideDescriptor).WithLocation(10, 1),
            };

            return VerifyCodeFixAsync(testCode, s_insideNamespaceOption, expected, fixedTestCode, placeSystemNamespaceFirst: false);
        }

        /// <summary>
        /// Verifies that the code fix will move the using directives, but will not move a file header comment separated by an new line.
        /// </summary>
        [Fact]
        public Task WhenInsidePreferred_UsingsInCompilationUnitWithFileHeader_UsingsMovedNotHeader()
        {
            var testCode = @"// This is a file header.
using Microsoft.CodeAnalysis;
using System;

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

            var expected = new DiagnosticResult[]
            {
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._insideDescriptor).WithLocation(2, 1),
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._insideDescriptor).WithLocation(3, 1),
            };

            return VerifyCodeFixAsync(testCode, s_insideNamespaceOption, expected, fixedTestCode, placeSystemNamespaceFirst: true);
        }

        /// <summary>
        /// Verifies that the code fix will properly move separated trivia, but will not move a file header comment.
        /// </summary>
        [Fact]
        public Task WhenInsidePreferred_UsingsInCompilationUnitWithFileHeaderAndTrivia_UsingsAndTriviaMovedNotHeader()
        {
            var testCode = @"// File Header

// Leading Comment

using Microsoft.CodeAnalysis;
using System;

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

            var expected = new DiagnosticResult[]
            {
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._insideDescriptor).WithLocation(5, 1),
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._insideDescriptor).WithLocation(6, 1),
            };

            return VerifyCodeFixAsync(testCode, s_insideNamespaceOption, expected, fixedTestCode, placeSystemNamespaceFirst: true);
        }

        /// <summary>
        /// Verifies that a code fix will not be offered for MisplacedUsing diagnostics when multiple namespaces are present.
        /// </summary>
        [Fact]
        public Task WhenInsidePreferred_UsingsInCompilationUnitWithMultipleNamespaces_NoCodeFixOffered()
        {
            var testCode = @"using System;

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
            var expected = new DiagnosticResult[]
            {
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._insideDescriptor).WithLocation(1, 1)
            };

            return VerifyCodeFixAsync(testCode, s_insideNamespaceOption, expected, fixedSource: testCode, remaining: expected, placeSystemNamespaceFirst: true);
        }

        /// <summary>
        /// Verifies that the code fix will properly move pragmas.
        /// </summary>
        [Fact]
        public Task WhenInsidePreferred_UsingsInCompilationUnitWithPragma_PragmaMoved()
        {
            var testCode = @"#pragma warning disable 1573 // Comment
using System;
using System.Threading;

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

            var expected = new DiagnosticResult[]
            {
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._insideDescriptor).WithLocation(2, 1),
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._insideDescriptor).WithLocation(3, 1),
            };

            return VerifyCodeFixAsync(testCode, s_insideNamespaceOption, expected, fixedTestCode, placeSystemNamespaceFirst: true);
        }

        /// <summary>
        /// Verifies that the code fix will properly move regions.
        /// </summary>
        [Fact]
        public Task WhenInsidePreferred_UsingsInCompilationUnitWithRegion_RegionMoved()
        {
            var testCode = @"#region Comment
#endregion Comment
using System;
using System.Threading;

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

            var expected = new DiagnosticResult[]
            {
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._insideDescriptor).WithLocation(3, 1),
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._insideDescriptor).WithLocation(4, 1),
            };

            return VerifyCodeFixAsync(testCode, s_insideNamespaceOption, expected, fixedTestCode, placeSystemNamespaceFirst: true);
        }

        /// <summary>
        /// Verifies that the code fix will properly move comment trivia.
        /// </summary>
        [Fact]
        public Task WhenInsidePreferred_UsingsInCompilationUnitWithCommentTrivia_TriviaMoved()
        {
            var testCode = @"
// Some comment
using System;
using System.Threading;

namespace TestNamespace
{
}
";

            var fixedTestCode = @"
namespace TestNamespace
{
    // Some comment
    using System;
    using System.Threading;
}
";

            var expected = new DiagnosticResult[]
            {
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._insideDescriptor).WithLocation(3, 1),
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._insideDescriptor).WithLocation(4, 1),
            };

            return VerifyCodeFixAsync(testCode, s_insideNamespaceOption, expected, fixedTestCode, placeSystemNamespaceFirst: true);
        }

        #endregion

        #region Test OutsideNamespace


        /// <summary>
        /// Verifies that valid using statements in the compilation unit does not produce any diagnostics.
        /// </summary>
        [Fact]
        public Task WhenOutsidePreferred_UsingsInCompilationUnit_ValidUsingStatements()
        {
            var testCode = @"using System;
using System.Threading;

namespace TestNamespace
{
}
";

            return VerifyAnalyzerAsync(testCode, s_outsideNamespaceOption, DiagnosticResult.EmptyDiagnosticResults);
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
            var testCode = $@"using System;

{typeDefinition}
";

            return VerifyAnalyzerAsync(testCode, s_outsideNamespaceOption, DiagnosticResult.EmptyDiagnosticResults);
        }

        /// <summary>
        /// Verifies that using statements in a namespace produces the expected diagnostics.
        /// </summary>
        [Fact]
        public Task WhenOutsidePreferred_UsingsInNamespace_UsingsMoved()
        {
            var testCode = @"namespace TestNamespace
{
    using System;
    using System.Threading;
}
";
            var fixedTestCode = @"using System;
using System.Threading;

namespace TestNamespace
{
}
";

            var expected = new DiagnosticResult[]
            {
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._outsideDescriptor).WithLocation(3, 5),
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._outsideDescriptor).WithLocation(4, 5),
            };

            return VerifyCodeFixAsync(testCode, s_outsideNamespaceOption, expected, fixedTestCode, placeSystemNamespaceFirst: true);
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
    using System;
    using System.Threading;
    using Reflection;
}
";
            var fixedTestCode = @"using System;
using System.Threading;
using System.Reflection;

namespace System
{
}
";

            var expected = new DiagnosticResult[]
            {
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._outsideDescriptor).WithLocation(3, 5),
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._outsideDescriptor).WithLocation(4, 5),
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._outsideDescriptor).WithLocation(5, 5),
            };

            return VerifyCodeFixAsync(testCode, s_outsideNamespaceOption, expected, fixedTestCode, placeSystemNamespaceFirst: true);
        }


        /// <summary>
        /// Verifies that simplified using statements in a namespace are expanded during the code fix operation.
        /// </summary>
        [Fact]
        public Task WhenOutsidePreferred_SimplifiedUsingAliasInNamespace_UsingsMovedAndExpanded()
        {
            var testCode = @"namespace System.MyExtension
{
    using System.Threading;
    using Reflection;
    using Assembly = Reflection.Assembly;
    using List = Collections.Generic.IList<int>;
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

            var expected = new DiagnosticResult[]
            {
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._outsideDescriptor).WithLocation(3, 5),
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._outsideDescriptor).WithLocation(4, 5),
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._outsideDescriptor).WithLocation(5, 5),
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._outsideDescriptor).WithLocation(6, 5),
            };

            return VerifyCodeFixAsync(testCode, s_outsideNamespaceOption, expected, fixedTestCode, placeSystemNamespaceFirst: true);
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
    using System;
    using System.Threading;
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

            var expected = new DiagnosticResult[]
            {
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._outsideDescriptor).WithLocation(7, 5),
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._outsideDescriptor).WithLocation(8, 5),
            };

            return VerifyCodeFixAsync(testCode, s_outsideNamespaceOption, expected, fixedTestCode, placeSystemNamespaceFirst: true);
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
    using System;
}
";
            var fixedTestCode = @"// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;

namespace TestNamespace
{
}
";

            var expected = new DiagnosticResult[]
            {
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._outsideDescriptor).WithLocation(6, 5),
            };

            return VerifyCodeFixAsync(testCode, s_outsideNamespaceOption, expected, fixedTestCode, placeSystemNamespaceFirst: true);
        }

        [Fact]
        public Task WhenOutsidePreferred_UsingsInNamespaceWithCommentsAndCompilationUnitHasFileHeader_UsingsMovedWithCommentsAndHeaderPreserved()
        {
            var testCode = @"// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace TestNamespace
{
    // Separated Comment

    using System.Collections;
    // Comment
    using System;
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

            var expected = new DiagnosticResult[]
            {
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._outsideDescriptor).WithLocation(8, 5),
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._outsideDescriptor).WithLocation(10, 5),
            };

            return VerifyCodeFixAsync(testCode, s_outsideNamespaceOption, expected, fixedTestCode, placeSystemNamespaceFirst: true);
        }

        [Fact]
        public Task WhenOutsidePreferred_UsingsInNamespace_UsingsMovedAndSystemPlacedFirstIgnored()
        {
            var testCode = @"namespace Foo
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

            var expected = new DiagnosticResult[]
            {
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._outsideDescriptor).WithLocation(3, 5),
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._outsideDescriptor).WithLocation(4, 5),
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._outsideDescriptor).WithLocation(5, 5),
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._outsideDescriptor).WithLocation(6, 5),
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._outsideDescriptor).WithLocation(8, 5),
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._outsideDescriptor).WithLocation(9, 5),
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._outsideDescriptor).WithLocation(11, 5),
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._outsideDescriptor).WithLocation(12, 5),
            };

            return VerifyCodeFixAsync(testCode, s_outsideNamespaceOption, expected, fixedTestCode, placeSystemNamespaceFirst: true);
        }

        [Fact]
        public Task WhenOutsidePreferred_UsingsInNamespace_UsingsMovedAndAlphaSortIgnored()
        {
            var testCode = @"namespace Foo
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

            var expected = new DiagnosticResult[]
            {
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._outsideDescriptor).WithLocation(3, 5),
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._outsideDescriptor).WithLocation(4, 5),
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._outsideDescriptor).WithLocation(5, 5),
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._outsideDescriptor).WithLocation(6, 5),
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._outsideDescriptor).WithLocation(8, 5),
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._outsideDescriptor).WithLocation(9, 5),
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._outsideDescriptor).WithLocation(11, 5),
                Diagnostic(MisplacedUsingsDiagnosticAnalyzer._outsideDescriptor).WithLocation(12, 5),
            };

            return VerifyCodeFixAsync(testCode, s_outsideNamespaceOption, expected, fixedTestCode, placeSystemNamespaceFirst: false);
        }

        #endregion

        private static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
        {
            return Verify.Diagnostic(descriptor);
        }

        private static Task VerifyAnalyzerAsync(string source, CodeStyleOption<AddImportPlacement> usingPlacement, DiagnosticResult[] expected)
        {
            return VerifyCodeFixAsync(source, usingPlacement, expected, fixedSource: null, placeSystemNamespaceFirst: false);
        }

        private static Task VerifyCodeFixAsync(string source, CodeStyleOption<AddImportPlacement> usingPlacement, DiagnosticResult[] expected, string fixedSource, bool placeSystemNamespaceFirst)
        {
            return VerifyCodeFixAsync(source, usingPlacement, expected, fixedSource, remaining: DiagnosticResult.EmptyDiagnosticResults, placeSystemNamespaceFirst);
        }

        private static async Task VerifyCodeFixAsync(string source, CodeStyleOption<AddImportPlacement> usingPlacement, DiagnosticResult[] expected, string fixedSource, DiagnosticResult[] remaining, bool placeSystemNamespaceFirst)
        {
            // Create the .editorconfig with the necessary code style settings.
            var editorConfig = $@"
root = true

[*.cs]
dotnet_sort_system_directives_first = {placeSystemNamespaceFirst}
csharp_using_directive_placement = {CSharpCodeStyleOptions.GetUsingDirectivesPlacementEditorConfigString(usingPlacement)}
";

            // Capture the working directory so it can be restored after the test runs.
            var workingDirectory = Environment.CurrentDirectory;

            // Create a temporary folder so that the coding conventions library can be
            // used to read code style preferences from an .editorconfig.
            var testDirectoryName = Path.GetRandomFileName();
            Directory.CreateDirectory(testDirectoryName);
            try
            {
                // Change the working directory to our test directory so that we 
                // can find the .editorconfig from the analyzer to get our 
                // code style settings.
                Environment.CurrentDirectory = testDirectoryName;

                File.WriteAllText(".editorconfig", editorConfig);

                // The contents of this file are ignored, but the coding conventions 
                // library checks for existence before .editorconfig is used.
                File.WriteAllText("Test0.cs", string.Empty);

                // Do not specify the full path to the source file since only the
                // filename will be copied causing mismatch when verifying the fixed state.
                var test = new CSharpCodeFixTest<MisplacedUsingsDiagnosticAnalyzer, MisplacedUsingsCodeFixProvider, XUnitVerifier>
                {
                    TestState = { Sources = { ("Test0.cs", source) } },
                    FixedCode = fixedSource
                };

                // Set code style settings in the OptionSet so that the CodeFix can
                // access the settings.
                test.OptionsTransforms.Add(
                    optionsSet => optionsSet.WithChangedOption(GenerationOptions.PlaceSystemNamespaceFirst, LanguageNames.CSharp, placeSystemNamespaceFirst)
                        .WithChangedOption(CSharpCodeStyleOptions.PreferredUsingDirectivesPlacement, usingPlacement));

                // Fix the severity of expected diagnostics.
                var fixedExpectedResults = expected.Select(
                    result => result.WithSeverity(usingPlacement.Notification.Severity.ToDiagnosticSeverity() ?? DiagnosticSeverity.Hidden));
                test.ExpectedDiagnostics.AddRange(fixedExpectedResults);

                // Fix the severity of remaining diagnostics.
                var fixedRemainingResults = remaining.Select(
                    result => result.WithSeverity(usingPlacement.Notification.Severity.ToDiagnosticSeverity() ?? DiagnosticSeverity.Hidden));
                test.FixedState.ExpectedDiagnostics.AddRange(fixedRemainingResults);

                await test.RunAsync();
            }
            finally
            {
                // Clean up by resetting the working directory and deleting
                // the temporary folder.
                Environment.CurrentDirectory = workingDirectory;
                try
                {
                    Directory.Delete(testDirectoryName, true);
                }
                catch { }
            }
        }
    }
}
