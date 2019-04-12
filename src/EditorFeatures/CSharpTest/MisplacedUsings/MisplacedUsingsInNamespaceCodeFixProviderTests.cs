// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.MisplacedUsings;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MisplacedUsings
{
    using Verify = CSharpCodeFixVerifier<MisplacedUsingsInNamespaceDiagnosticAnalyzer, MisplacedUsingsCodeFixProvider, XUnitVerifier>;

    /// <summary>
    /// Unit tests for the <see cref="MisplacedUsingsInNamespaceDiagnosticAnalyzer"/> and <see cref="MisplacedUsingsCodeFixProvider"/>.
    /// </summary>
    public class MisplacedUsingsInNamespaceCodeFixProviderTests : MisplacedUsingsCodeFixProviderTests
    {
        protected override DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
        {
            return Verify.Diagnostic(descriptor);
        }

        protected override CodeFixTest<XUnitVerifier> CreateTest((string filename, string content) sourceFile, string fixedSource)
        {
            return new CSharpCodeFixTest<MisplacedUsingsInNamespaceDiagnosticAnalyzer, MisplacedUsingsCodeFixProvider, XUnitVerifier>
            {
                TestState = { Sources = { sourceFile } },
                FixedCode = fixedSource
            };
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
    using System;
    using System.Threading;
}
";

            return VerifyAnalyzerAsync(testCode, PreservePlacementOption, DiagnosticResult.EmptyDiagnosticResults);
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

            return VerifyAnalyzerAsync(testCode, PreservePlacementOption, DiagnosticResult.EmptyDiagnosticResults);
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

            return VerifyAnalyzerAsync(testCode, OutsideNamespaceOption, DiagnosticResult.EmptyDiagnosticResults);
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

            return VerifyAnalyzerAsync(testCode, OutsideNamespaceOption, DiagnosticResult.EmptyDiagnosticResults);
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
                Diagnostic(MisplacedUsingsInNamespaceDiagnosticAnalyzer.OutsideDescriptor).WithLocation(3, 5),
                Diagnostic(MisplacedUsingsInNamespaceDiagnosticAnalyzer.OutsideDescriptor).WithLocation(4, 5),
            };

            return VerifyCodeFixAsync(testCode, OutsideNamespaceOption, expected, fixedTestCode, placeSystemNamespaceFirst: true);
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
                Diagnostic(MisplacedUsingsInNamespaceDiagnosticAnalyzer.OutsideDescriptor).WithLocation(3, 5),
                Diagnostic(MisplacedUsingsInNamespaceDiagnosticAnalyzer.OutsideDescriptor).WithLocation(4, 5),
                Diagnostic(MisplacedUsingsInNamespaceDiagnosticAnalyzer.OutsideDescriptor).WithLocation(5, 5),
            };

            return VerifyCodeFixAsync(testCode, OutsideNamespaceOption, expected, fixedTestCode, placeSystemNamespaceFirst: true);
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
                Diagnostic(MisplacedUsingsInNamespaceDiagnosticAnalyzer.OutsideDescriptor).WithLocation(3, 5),
                Diagnostic(MisplacedUsingsInNamespaceDiagnosticAnalyzer.OutsideDescriptor).WithLocation(4, 5),
                Diagnostic(MisplacedUsingsInNamespaceDiagnosticAnalyzer.OutsideDescriptor).WithLocation(5, 5),
                Diagnostic(MisplacedUsingsInNamespaceDiagnosticAnalyzer.OutsideDescriptor).WithLocation(6, 5),
            };

            return VerifyCodeFixAsync(testCode, OutsideNamespaceOption, expected, fixedTestCode, placeSystemNamespaceFirst: true);
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
                Diagnostic(MisplacedUsingsInNamespaceDiagnosticAnalyzer.OutsideDescriptor).WithLocation(7, 5),
                Diagnostic(MisplacedUsingsInNamespaceDiagnosticAnalyzer.OutsideDescriptor).WithLocation(8, 5),
            };

            return VerifyCodeFixAsync(testCode, OutsideNamespaceOption, expected, fixedTestCode, placeSystemNamespaceFirst: true);
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
                Diagnostic(MisplacedUsingsInNamespaceDiagnosticAnalyzer.OutsideDescriptor).WithLocation(6, 5),
            };

            return VerifyCodeFixAsync(testCode, OutsideNamespaceOption, expected, fixedTestCode, placeSystemNamespaceFirst: true);
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
                Diagnostic(MisplacedUsingsInNamespaceDiagnosticAnalyzer.OutsideDescriptor).WithLocation(8, 5),
                Diagnostic(MisplacedUsingsInNamespaceDiagnosticAnalyzer.OutsideDescriptor).WithLocation(10, 5),
            };

            return VerifyCodeFixAsync(testCode, OutsideNamespaceOption, expected, fixedTestCode, placeSystemNamespaceFirst: true);
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
                Diagnostic(MisplacedUsingsInNamespaceDiagnosticAnalyzer.OutsideDescriptor).WithLocation(3, 5),
                Diagnostic(MisplacedUsingsInNamespaceDiagnosticAnalyzer.OutsideDescriptor).WithLocation(4, 5),
                Diagnostic(MisplacedUsingsInNamespaceDiagnosticAnalyzer.OutsideDescriptor).WithLocation(5, 5),
                Diagnostic(MisplacedUsingsInNamespaceDiagnosticAnalyzer.OutsideDescriptor).WithLocation(6, 5),
                Diagnostic(MisplacedUsingsInNamespaceDiagnosticAnalyzer.OutsideDescriptor).WithLocation(8, 5),
                Diagnostic(MisplacedUsingsInNamespaceDiagnosticAnalyzer.OutsideDescriptor).WithLocation(9, 5),
                Diagnostic(MisplacedUsingsInNamespaceDiagnosticAnalyzer.OutsideDescriptor).WithLocation(11, 5),
                Diagnostic(MisplacedUsingsInNamespaceDiagnosticAnalyzer.OutsideDescriptor).WithLocation(12, 5),
            };

            return VerifyCodeFixAsync(testCode, OutsideNamespaceOption, expected, fixedTestCode, placeSystemNamespaceFirst: true);
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
                Diagnostic(MisplacedUsingsInNamespaceDiagnosticAnalyzer.OutsideDescriptor).WithLocation(3, 5),
                Diagnostic(MisplacedUsingsInNamespaceDiagnosticAnalyzer.OutsideDescriptor).WithLocation(4, 5),
                Diagnostic(MisplacedUsingsInNamespaceDiagnosticAnalyzer.OutsideDescriptor).WithLocation(5, 5),
                Diagnostic(MisplacedUsingsInNamespaceDiagnosticAnalyzer.OutsideDescriptor).WithLocation(6, 5),
                Diagnostic(MisplacedUsingsInNamespaceDiagnosticAnalyzer.OutsideDescriptor).WithLocation(8, 5),
                Diagnostic(MisplacedUsingsInNamespaceDiagnosticAnalyzer.OutsideDescriptor).WithLocation(9, 5),
                Diagnostic(MisplacedUsingsInNamespaceDiagnosticAnalyzer.OutsideDescriptor).WithLocation(11, 5),
                Diagnostic(MisplacedUsingsInNamespaceDiagnosticAnalyzer.OutsideDescriptor).WithLocation(12, 5),
            };

            return VerifyCodeFixAsync(testCode, OutsideNamespaceOption, expected, fixedTestCode, placeSystemNamespaceFirst: false);
        }

        #endregion
    }
}
