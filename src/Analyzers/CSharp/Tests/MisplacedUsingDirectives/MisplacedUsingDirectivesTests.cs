// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.MisplacedUsingDirectives;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MisplacedUsingDirectives
{
    public class MisplacedUsingDirectivesTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        public MisplacedUsingDirectivesTests(ITestOutputHelper logger)
          : base(logger)
        {
        }

        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new MisplacedUsingDirectivesDiagnosticAnalyzer(), new MisplacedUsingDirectivesCodeFixProvider());

        internal static readonly CodeStyleOption2<AddImportPlacement> OutsidePreferPreservationOption =
           new(AddImportPlacement.OutsideNamespace, NotificationOption2.None);

        internal static readonly CodeStyleOption2<AddImportPlacement> InsidePreferPreservationOption =
            new(AddImportPlacement.InsideNamespace, NotificationOption2.None);

        internal static readonly CodeStyleOption2<AddImportPlacement> InsideNamespaceOption =
            new(AddImportPlacement.InsideNamespace, NotificationOption2.Error);

        internal static readonly CodeStyleOption2<AddImportPlacement> OutsideNamespaceOption =
            new(AddImportPlacement.OutsideNamespace, NotificationOption2.Error);

        protected const string ClassDefinition = """
            public class TestClass
            {
            }
            """;

        protected const string StructDefinition = """
            public struct TestStruct
            {
            }
            """;

        protected const string InterfaceDefinition = """
            public interface TestInterface
            {
            }
            """;

        protected const string EnumDefinition = """
            public enum TestEnum
            {
                TestValue
            }
            """;

        protected const string DelegateDefinition = @"public delegate void TestDelegate();";

        private TestParameters GetTestParameters(CodeStyleOption2<AddImportPlacement> preferredPlacementOption)
            => new(options: new OptionsCollection(GetLanguage()) { { CSharpCodeStyleOptions.PreferredUsingDirectivePlacement, preferredPlacementOption } });

        private protected Task TestDiagnosticMissingAsync(string initialMarkup, CodeStyleOption2<AddImportPlacement> preferredPlacementOption)
            => TestDiagnosticMissingAsync(initialMarkup, GetTestParameters(preferredPlacementOption));

        private protected Task TestMissingAsync(string initialMarkup, CodeStyleOption2<AddImportPlacement> preferredPlacementOption)
            => TestMissingAsync(initialMarkup, GetTestParameters(preferredPlacementOption));

        private protected Task TestInRegularAndScriptAsync(string initialMarkup, string expectedMarkup, CodeStyleOption2<AddImportPlacement> preferredPlacementOption, bool placeSystemNamespaceFirst)
        {
            var options = new OptionsCollection(GetLanguage())
            {
                { CSharpCodeStyleOptions.PreferredUsingDirectivePlacement, preferredPlacementOption },
                { GenerationOptions.PlaceSystemNamespaceFirst, placeSystemNamespaceFirst },
            };
            return TestInRegularAndScriptAsync(
                initialMarkup, expectedMarkup, options: options, parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp10));
        }

        #region Test Preserve

        /// <summary>
        /// Verifies that valid using statements in a namespace does not produce any diagnostics.
        /// </summary>
        [Fact]
        public Task WhenPreserve_UsingsInNamespace_ValidUsingStatements()
        {
            var testCode = """
                namespace TestNamespace
                {
                    [|using System;
                    using System.Threading;|]
                }
                """;

            return TestDiagnosticMissingAsync(testCode, OutsidePreferPreservationOption);
        }

        [Fact]
        public Task WhenPreserve_UsingsInNamespace_ValidUsingStatements_FileScopedNamespace()
        {
            var testCode = """
                namespace TestNamespace;

                [|using System;
                using System.Threading;|]
                """;

            return TestDiagnosticMissingAsync(testCode, OutsidePreferPreservationOption);
        }

        /// <summary>
        /// Verifies that having using statements in the compilation unit will not produce any diagnostics, nor will
        /// having using statements inside a namespace.
        /// </summary>
        [Fact]
        public Task WhenPreserve_UsingsInCompilationUnitAndNamespace_ValidUsingStatements()
        {
            var testCode = """
                using System;

                namespace TestNamespace
                {
                    [|using System.Threading;|]
                }
                """;

            return TestDiagnosticMissingAsync(testCode, OutsidePreferPreservationOption);
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
            var testCode = """
                [|using System.Reflection;|]

                [assembly: AssemblyVersion("1.0.0.0")]

                namespace TestNamespace
                {
                    using System;
                    using System.Threading;
                }
                """;

            return TestDiagnosticMissingAsync(testCode, InsidePreferPreservationOption);
        }

        /// <summary>
        /// Verifies that having using statements in the compilation unit will not produce any diagnostics, even if they could be
        /// moved inside a namespace.
        /// </summary>
        [Fact]
        public Task WhenPreserve_UsingsInCompilationUnit_ValidUsingStatements()
        {
            var testCode = """
                [|using System;
                using System.Threading;|]

                namespace TestNamespace
                {
                }
                """;

            return TestDiagnosticMissingAsync(testCode, InsidePreferPreservationOption);
        }

        #endregion

        #region Test OutsideNamespace

        /// <summary>
        /// Verifies that valid using statements in the compilation unit does not produce any diagnostics.
        /// </summary>
        [Fact]
        public Task WhenOutsidePreferred_UsingsInCompilationUnit_ValidUsingStatements()
        {
            var testCode = """
                [|using System;
                using System.Threading;|]

                namespace TestNamespace
                {
                }
                """;

            return TestDiagnosticMissingAsync(testCode, OutsideNamespaceOption);
        }

        [Fact]
        public Task WhenOutsidePreferred_UsingsInCompilationUnit_ValidUsingStatements_FileScopedNamespace()
        {
            var testCode = """
                [|using System;
                using System.Threading;|]

                namespace TestNamespace;
                """;

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
            var testCode = """
                namespace TestNamespace
                {
                    [|using System;
                    using System.Threading;|]
                }
                """;
            var fixedTestCode = """
                {|Warning:using System;|}
                {|Warning:using System.Threading;|}

                namespace TestNamespace
                {
                }
                """;

            return TestInRegularAndScriptAsync(testCode, fixedTestCode, OutsideNamespaceOption, placeSystemNamespaceFirst: true);
        }

        [Fact]
        public Task WhenOutsidePreferred_UsingsInNamespace_UsingsMoved_FileScopedNamespace()
        {
            var testCode = """
                namespace TestNamespace;

                [|using System;
                using System.Threading;|]
                """;
            var fixedTestCode = """

                {|Warning:using System;|}
                {|Warning:using System.Threading;|}
                namespace TestNamespace;

                """;

            return TestInRegularAndScriptAsync(testCode, fixedTestCode, OutsideNamespaceOption, placeSystemNamespaceFirst: true);
        }

        /// <summary>
        /// Verifies that simplified using statements in a namespace are expanded during the code fix operation.
        /// </summary>
        [Fact]
        public Task WhenOutsidePreferred_SimplifiedUsingInNamespace_UsingsMovedAndExpanded()
        {
            var testCode = """
                namespace System
                {
                    [|using System;
                    using System.Threading;
                    using Reflection;|]
                }
                """;
            var fixedTestCode = """
                {|Warning:using System;|}
                {|Warning:using System.Threading;|}
                {|Warning:using System.Reflection;|}

                namespace System
                {
                }
                """;

            return TestInRegularAndScriptAsync(testCode, fixedTestCode, OutsideNamespaceOption, placeSystemNamespaceFirst: true);
        }

        /// <summary>
        /// Verifies that the code fix will move the using directives when they are present in both the compilation unit and namespace.
        /// </summary>
        [Fact]
        public Task WhenOutsidePreferred_UsingsInBoth_UsingsMoved()
        {
            var testCode = """
                using Microsoft.CodeAnalysis;

                namespace TestNamespace
                {
                    [|using System;|]
                }
                """;

            var fixedTestCode = """
                using Microsoft.CodeAnalysis;
                {|Warning:using System;|}

                namespace TestNamespace
                {
                }
                """;

            return TestInRegularAndScriptAsync(testCode, fixedTestCode, OutsideNamespaceOption, placeSystemNamespaceFirst: true);
        }

        /// <summary>
        /// Verifies that simplified using statements in a namespace are expanded during the code fix operation.
        /// </summary>
        [Fact]
        public Task WhenOutsidePreferred_SimplifiedUsingAliasInNamespace_UsingsMovedAndExpanded()
        {
            var testCode = """
                namespace System.MyExtension
                {
                    [|using System.Threading;
                    using Reflection;
                    using Assembly = Reflection.Assembly;
                    using List = Collections.Generic.IList<int>;|]
                }
                """;
            var fixedTestCode = """
                {|Warning:using System.Threading;|}
                {|Warning:using System.Reflection;|}
                {|Warning:using Assembly = System.Reflection.Assembly;|}
                {|Warning:using List = System.Collections.Generic.IList<int>;|}

                namespace System.MyExtension
                {
                }
                """;

            return TestInRegularAndScriptAsync(testCode, fixedTestCode, OutsideNamespaceOption, placeSystemNamespaceFirst: true);
        }

        /// <summary>
        /// Verifies that having using statements in the compilation unit will not produce any diagnostics when there are attributes present.
        /// </summary>
        [Fact]
        public Task WhenOutsidePreferred_UsingsInNamespaceAndCompilationUnitWithAttributes_UsingsMoved()
        {
            var testCode = """
                using System.Reflection;

                [assembly: AssemblyVersion("1.0.0.0")]

                namespace TestNamespace
                {
                    [|using System;
                    using System.Threading;|]
                }
                """;
            var fixedTestCode = """
                using System.Reflection;
                {|Warning:using System;|}
                {|Warning:using System.Threading;|}

                [assembly: AssemblyVersion("1.0.0.0")]

                namespace TestNamespace
                {
                }
                """;

            return TestInRegularAndScriptAsync(testCode, fixedTestCode, OutsideNamespaceOption, placeSystemNamespaceFirst: true);
        }

        /// <summary>
        /// Verifies that the file header of a file is properly preserved when moving using statements out of a namespace.
        /// </summary>
        [Fact]
        public Task WhenOutsidePreferred_UsingsInNamespaceAndCompilationUnitHasFileHeader_UsingsMovedAndHeaderPreserved()
        {
            var testCode = """
                // Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
                // Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

                namespace TestNamespace
                {
                    [|using System;|]
                }
                """;
            var fixedTestCode = """
                // Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
                // Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

                {|Warning:using System;|}

                namespace TestNamespace
                {
                }
                """;

            return TestInRegularAndScriptAsync(testCode, fixedTestCode, OutsideNamespaceOption, placeSystemNamespaceFirst: true);
        }

        [Fact]
        public Task WhenOutsidePreferred_UsingsInNamespaceWithCommentsAndCompilationUnitHasFileHeader_UsingsMovedWithCommentsAndHeaderPreserved()
        {
            var testCode = """
                // Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
                // Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

                namespace TestNamespace
                {
                    // Separated Comment

                    [|using System.Collections;
                    // Comment
                    using System;|]
                }
                """;
            var fixedTestCode = """
                // Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
                // Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

                // Separated Comment

                {|Warning:using System.Collections;|}
                // Comment
                {|Warning:using System;|}

                namespace TestNamespace
                {
                }
                """;

            return TestInRegularAndScriptAsync(testCode, fixedTestCode, OutsideNamespaceOption, placeSystemNamespaceFirst: true);
        }

        [Fact]
        public Task WhenOutsidePreferred_UsingsInNamespace_UsingsMovedAndSystemPlacedFirstIgnored()
        {
            var testCode = """
                namespace Foo
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
                """;

            var fixedTestCode = """
                {|Warning:using Microsoft.CodeAnalysis;|}
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
                """;

            return TestInRegularAndScriptAsync(testCode, fixedTestCode, OutsideNamespaceOption, placeSystemNamespaceFirst: true);
        }

        [Fact]
        public Task WhenOutsidePreferred_UsingsInNamespace_UsingsMovedAndAlphaSortIgnored()
        {
            var testCode = """
                namespace Foo
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
                """;

            var fixedTestCode = """
                {|Warning:using Microsoft.CodeAnalysis;|}
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
                """;

            return TestInRegularAndScriptAsync(testCode, fixedTestCode, OutsideNamespaceOption, placeSystemNamespaceFirst: false);
        }

        /// <summary>
        /// Verifies that simplified using statements in nested namespace are expanded during the code fix operation.
        /// </summary>
        [Fact]
        public Task WhenOutsidePreferred_UsingsInNestedNamespaces_UsingsMovedAndExpanded()
        {
            var testCode = """
                using System;

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
                """;
            var fixedTestCode = """
                using System;
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
                """;

            return TestInRegularAndScriptAsync(testCode, fixedTestCode, OutsideNamespaceOption, placeSystemNamespaceFirst: true);
        }

        /// <summary>
        /// Verifies that simplified using statements in multiple namespaces are expanded during the code fix operation.
        /// </summary>
        [Fact]
        public Task WhenOutsidePreferred_UsingsInMultipleNamespaces_UsingsMovedAndExpanded()
        {
            var testCode = """
                using System;

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
                """;
            var fixedTestCode = """
                using System;
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
                """;

            return TestInRegularAndScriptAsync(testCode, fixedTestCode, OutsideNamespaceOption, placeSystemNamespaceFirst: true);
        }

        /// <summary>
        /// Verifies that simplified using statements in multiple namespaces are deduplicated during the code fix operation.
        /// </summary>
        [Fact]
        public Task WhenOutsidePreferred_UsingsInMultipleNamespaces_UsingsMovedAndDeduplicated()
        {
            var testCode = """
                using System;

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
                """;
            var fixedTestCode = """
                using System;
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
                """;

            return TestInRegularAndScriptAsync(testCode, fixedTestCode, OutsideNamespaceOption, placeSystemNamespaceFirst: true);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61773")]
        public Task WhenOutsidePreferred_MoveGlobalUsing1()
        {
            var testCode = """
                namespace N1
                {
                    [|global using System;|]
                }
                """;
            var fixedTestCode =
                """
                {|Warning:global using System;|}

                namespace N1
                {
                }
                """;

            return TestInRegularAndScriptAsync(testCode, fixedTestCode, OutsideNamespaceOption, placeSystemNamespaceFirst: true);
        }

        #endregion

        #region Test InsideNamespace

        /// <summary>
        /// Verifies that valid using statements in a namespace does not produce any diagnostics.
        /// </summary>
        [Fact]
        public Task WhenInsidePreferred_UsingsInNamespace_ValidUsingStatements()
        {
            var testCode = """
                namespace TestNamespace
                {
                    [|using System;
                    using System.Threading;|]
                }
                """;

            return TestDiagnosticMissingAsync(testCode, InsideNamespaceOption);
        }

        [Fact]
        public Task WhenInsidePreferred_UsingsInNamespace_ValidUsingStatements_FileScopedNamespace()
        {
            var testCode = """
                namespace TestNamespace;

                [|using System;
                using System.Threading;|]
                """;

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
            var testCode = """
                [|using System.Reflection;|]

                [assembly: AssemblyVersion("1.0.0.0")]

                namespace TestNamespace
                {
                    using System;
                    using System.Threading;
                }
                """;

            return TestDiagnosticMissingAsync(testCode, InsideNamespaceOption);
        }

        /// <summary>
        /// Verifies that the code fix will move the using directives and not place System directives first.
        /// </summary>
        [Fact]
        public Task WhenInsidePreferred_UsingsInCompilationUnit_UsingsMovedAndSystemPlacedFirstIgnored()
        {
            var testCode = """
                [|using Microsoft.CodeAnalysis;
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
                """;

            var fixedTestCode = """
                namespace Foo
                {
                    {|Warning:using Microsoft.CodeAnalysis;|}
                    {|Warning:using SystemAction = System.Action;|}
                    {|Warning:using static System.Math;|}
                    {|Warning:using System;|}

                    {|Warning:using static System.String;|}
                    {|Warning:using MyFunc = System.Func<int, bool>;|}

                    {|Warning:using System.Collections.Generic;|}
                    {|Warning:using System.Collections;|}

                    public class Bar
                    {
                    }
                }
                """;

            return TestInRegularAndScriptAsync(testCode, fixedTestCode, InsideNamespaceOption, placeSystemNamespaceFirst: true);
        }

        /// <summary>
        /// Verifies that the code fix will move the using directives and not sort them alphabetically.
        /// </summary>
        [Fact]
        public Task WhenInsidePreferred_UsingsInCompilationUnit_UsingsAndWithAlphaSortIgnored()
        {
            var testCode = """
                [|using Microsoft.CodeAnalysis;
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
                """;

            var fixedTestCode = """
                namespace NamespaceName
                {
                    {|Warning:using Microsoft.CodeAnalysis;|}
                    {|Warning:using SystemAction = System.Action;|}
                    {|Warning:using static System.Math;|}
                    {|Warning:using System;|}

                    {|Warning:using static System.String;|}
                    {|Warning:using MyFunc = System.Func<int, bool>;|}

                    {|Warning:using System.Collections.Generic;|}
                    {|Warning:using System.Collections;|}

                    public class Bar
                    {
                    }
                }
                """;

            return TestInRegularAndScriptAsync(testCode, fixedTestCode, InsideNamespaceOption, placeSystemNamespaceFirst: false);
        }

        /// <summary>
        /// Verifies that the code fix will move the using directives, but will not move a file header comment separated by an new line.
        /// </summary>
        [Fact]
        public Task WhenInsidePreferred_UsingsInCompilationUnitWithFileHeader_UsingsMovedNotHeader()
        {
            var testCode = """
                // This is a file header.
                [|using Microsoft.CodeAnalysis;
                using System;|]

                namespace TestNamespace
                {
                }
                """;

            var fixedTestCode = """
                // This is a file header.
                namespace TestNamespace
                {
                    {|Warning:using Microsoft.CodeAnalysis;|}
                    {|Warning:using System;|}
                }
                """;

            return TestInRegularAndScriptAsync(testCode, fixedTestCode, InsideNamespaceOption, placeSystemNamespaceFirst: true);
        }

        /// <summary>
        /// Verifies that the code fix will move the using directives when they are present in both the compilation unit and namespace.
        /// </summary>
        [Fact]
        public Task WhenInsidePreferred_UsingsInBoth_UsingsMoved()
        {
            var testCode = """
                [|using Microsoft.CodeAnalysis;|]

                namespace TestNamespace
                {
                    using System;
                }
                """;

            var fixedTestCode = """
                namespace TestNamespace
                {
                    {|Warning:using Microsoft.CodeAnalysis;|}
                    using System;
                }
                """;

            return TestInRegularAndScriptAsync(testCode, fixedTestCode, InsideNamespaceOption, placeSystemNamespaceFirst: true);
        }

        [Fact]
        public Task WhenInsidePreferred_UsingsInBoth_UsingsMoved_FileScopedNamespaec()
        {
            var testCode = """
                [|using Microsoft.CodeAnalysis;|]

                namespace TestNamespace;

                using System;
                """;

            var fixedTestCode = """
                namespace TestNamespace;
                {|Warning:using Microsoft.CodeAnalysis;|}

                using System;
                """;

            return TestInRegularAndScriptAsync(testCode, fixedTestCode, InsideNamespaceOption, placeSystemNamespaceFirst: true);
        }

        /// <summary>
        /// Verifies that the code fix will properly move separated trivia, but will not move a file header comment.
        /// </summary>
        [Fact]
        public Task WhenInsidePreferred_UsingsInCompilationUnitWithFileHeaderAndTrivia_UsingsAndTriviaMovedNotHeader()
        {
            var testCode = """
                // File Header

                // Leading Comment

                [|using Microsoft.CodeAnalysis;
                using System;|]

                namespace TestNamespace
                {
                }
                """;

            var fixedTestCode = """
                // File Header

                namespace TestNamespace
                {
                    // Leading Comment

                    {|Warning:using Microsoft.CodeAnalysis;|}
                    {|Warning:using System;|}
                }
                """;

            return TestInRegularAndScriptAsync(testCode, fixedTestCode, InsideNamespaceOption, placeSystemNamespaceFirst: true);
        }

        /// <summary>
        /// Verifies that a code fix will not be offered for MisplacedUsing diagnostics when multiple namespaces are present.
        /// </summary>
        [Fact]
        public Task WhenInsidePreferred_UsingsInCompilationUnitWithMultipleNamespaces_NoCodeFixOffered()
        {
            var testCode = """
                [|using System;|]

                namespace TestNamespace1
                {
                    public class TestClass1
                    {
                    }
                }

                namespace TestNamespace2
                {
                }
                """;

            return TestMissingAsync(testCode, InsideNamespaceOption);
        }

        /// <summary>
        /// Verifies that the code fix will properly move pragmas.
        /// </summary>
        [Fact]
        public Task WhenInsidePreferred_UsingsInCompilationUnitWithPragma_PragmaMoved()
        {
            var testCode = """
                #pragma warning disable 1573 // Comment
                [|using System;
                using System.Threading;|]

                namespace TestNamespace
                {
                }
                """;

            var fixedTestCode = """
                namespace TestNamespace
                {
                #pragma warning disable 1573 // Comment
                    {|Warning:using System;|}
                    {|Warning:using System.Threading;|}
                }
                """;

            return TestInRegularAndScriptAsync(testCode, fixedTestCode, InsideNamespaceOption, placeSystemNamespaceFirst: true);
        }

        /// <summary>
        /// Verifies that the code fix will properly move regions.
        /// </summary>
        [Fact]
        public Task WhenInsidePreferred_UsingsInCompilationUnitWithRegion_RegionMoved()
        {
            var testCode = """
                #region Comment
                #endregion Comment
                [|using System;
                using System.Threading;|]

                namespace TestNamespace
                {
                }
                """;

            var fixedTestCode = """
                namespace TestNamespace
                {
                    #region Comment
                    #endregion Comment
                    {|Warning:using System;|}
                    {|Warning:using System.Threading;|}
                }
                """;

            return TestInRegularAndScriptAsync(testCode, fixedTestCode, InsideNamespaceOption, placeSystemNamespaceFirst: true);
        }

        /// <summary>
        /// Verifies that the code fix will properly move comment trivia.
        /// </summary>
        [Fact]
        public Task WhenInsidePreferred_UsingsInCompilationUnitWithCommentTrivia_TriviaMoved()
        {
            var testCode = """

                // Some comment
                [|using System;
                using System.Threading;|]

                namespace TestNamespace
                {
                }
                """;

            var fixedTestCode = """
                namespace TestNamespace
                {

                    // Some comment
                    {|Warning:using System;|}
                    {|Warning:using System.Threading;|}
                }
                """;

            return TestInRegularAndScriptAsync(testCode, fixedTestCode, InsideNamespaceOption, placeSystemNamespaceFirst: true);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61773")]
        public Task WhenInsidePreferred_DoNotMoveGlobalUsings1()
        {
            var testCode = """
                [|global using System;|]

                namespace TestNamespace
                {
                }
                """;

            return TestMissingAsync(testCode, InsideNamespaceOption);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61773")]
        public Task WhenInsidePreferred_DoNotMoveGlobalUsings2()
        {
            var testCode = """
                [|global using System;
                using System.Threading;|]

                namespace TestNamespace
                {
                }
                """;

            var fixedCode = """
                global using System;

                namespace TestNamespace
                {
                    {|Warning:using System.Threading;|}
                }
                """;

            return TestInRegularAndScriptAsync(testCode, fixedCode, InsideNamespaceOption, placeSystemNamespaceFirst: true);
        }

        #endregion
    }
}
