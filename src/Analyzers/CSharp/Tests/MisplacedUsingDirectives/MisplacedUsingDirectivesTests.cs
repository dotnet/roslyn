// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
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

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MisplacedUsingDirectives;

public sealed class MisplacedUsingDirectivesTests(ITestOutputHelper logger)
    : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor(logger)
{
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

    internal static readonly CodeStyleOption2<AddImportPlacement> OutsideNamespaceIgnoringAliasesOption =
        new(AddImportPlacement.OutsideNamespaceIgnoringAliases, NotificationOption2.Error);

    private const string ClassDefinition = """
        public class TestClass
        {
        }
        """;

    private const string StructDefinition = """
        public struct TestStruct
        {
        }
        """;

    private const string InterfaceDefinition = """
        public interface TestInterface
        {
        }
        """;

    private const string EnumDefinition = """
        public enum TestEnum
        {
            TestValue
        }
        """;

    private const string DelegateDefinition = @"public delegate void TestDelegate();";

    private TestParameters GetTestParameters(CodeStyleOption2<AddImportPlacement> preferredPlacementOption)
        => new(options: new(GetLanguage()) { { CSharpCodeStyleOptions.PreferredUsingDirectivePlacement, preferredPlacementOption } });

    private Task TestDiagnosticMissingAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string initialMarkup,
        CodeStyleOption2<AddImportPlacement> preferredPlacementOption)
        => TestDiagnosticMissingAsync(initialMarkup, GetTestParameters(preferredPlacementOption));

    private Task TestMissingAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string initialMarkup,
        CodeStyleOption2<AddImportPlacement> preferredPlacementOption)
        => TestMissingAsync(initialMarkup, GetTestParameters(preferredPlacementOption));

    private Task TestInRegularAndScriptAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string initialMarkup,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string expectedMarkup,
        CodeStyleOption2<AddImportPlacement> preferredPlacementOption,
        bool placeSystemNamespaceFirst)
    {
        var options = new OptionsCollection(GetLanguage())
        {
            { CSharpCodeStyleOptions.PreferredUsingDirectivePlacement, preferredPlacementOption },
            { GenerationOptions.PlaceSystemNamespaceFirst, placeSystemNamespaceFirst },
        };
        return TestInRegularAndScriptAsync(
            initialMarkup, expectedMarkup, new(options: options, parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp10)));
    }

    #region Test Preserve

    /// <summary>
    /// Verifies that valid using statements in a namespace does not produce any diagnostics.
    /// </summary>
    [Fact]
    public Task WhenPreserve_UsingsInNamespace_ValidUsingStatements()
        => TestDiagnosticMissingAsync("""
            namespace TestNamespace
            {
                [|using System;
                using System.Threading;|]
            }
            """, OutsidePreferPreservationOption);

    [Fact]
    public Task WhenPreserve_UsingsInNamespace_ValidUsingStatements_FileScopedNamespace()
        => TestDiagnosticMissingAsync("""
            namespace TestNamespace;

            [|using System;
            using System.Threading;|]
            """, OutsidePreferPreservationOption);

    /// <summary>
    /// Verifies that having using statements in the compilation unit will not produce any diagnostics, nor will
    /// having using statements inside a namespace.
    /// </summary>
    [Fact]
    public Task WhenPreserve_UsingsInCompilationUnitAndNamespace_ValidUsingStatements()
        => TestDiagnosticMissingAsync("""
            using System;

            namespace TestNamespace
            {
                [|using System.Threading;|]
            }
            """, OutsidePreferPreservationOption);

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
        => TestDiagnosticMissingAsync($"""
            [|using System;|]

            {typeDefinition}
            """, InsidePreferPreservationOption);

    /// <summary>
    /// Verifies that having using statements in the compilation unit will not produce any diagnostics when there are attributes present.
    /// </summary>
    [Fact]
    public Task WhenPreserve_UsingsInCompilationUnitWithAttributes_ValidUsingStatements()
        => TestDiagnosticMissingAsync("""
            [|using System.Reflection;|]

            [assembly: AssemblyVersion("1.0.0.0")]

            namespace TestNamespace
            {
                using System;
                using System.Threading;
            }
            """, InsidePreferPreservationOption);

    /// <summary>
    /// Verifies that having using statements in the compilation unit will not produce any diagnostics, even if they could be
    /// moved inside a namespace.
    /// </summary>
    [Fact]
    public Task WhenPreserve_UsingsInCompilationUnit_ValidUsingStatements()
        => TestDiagnosticMissingAsync("""
            [|using System;
            using System.Threading;|]

            namespace TestNamespace
            {
            }
            """, InsidePreferPreservationOption);

    #endregion

    #region Test OutsideNamespace

    /// <summary>
    /// Verifies that valid using statements in the compilation unit does not produce any diagnostics.
    /// </summary>
    [Fact]
    public Task WhenOutsidePreferred_UsingsInCompilationUnit_ValidUsingStatements()
        => TestDiagnosticMissingAsync("""
            [|using System;
            using System.Threading;|]

            namespace TestNamespace
            {
            }
            """, OutsideNamespaceOption);

    [Fact]
    public Task WhenOutsidePreferred_UsingsInCompilationUnit_ValidUsingStatements_FileScopedNamespace()
        => TestDiagnosticMissingAsync("""
            [|using System;
            using System.Threading;|]

            namespace TestNamespace;
            """, OutsideNamespaceOption);

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
        => TestDiagnosticMissingAsync($"""
            [|using System;|]

            {typeDefinition}
            """, OutsideNamespaceOption);

    /// <summary>
    /// Verifies that using statements in a namespace produces the expected diagnostics.
    /// </summary>
    [Fact]
    public Task WhenOutsidePreferred_UsingsInNamespace_UsingsMoved()
        => TestInRegularAndScriptAsync("""
            namespace TestNamespace
            {
                [|using System;
                using System.Threading;|]
            }
            """, """
            {|Warning:using System;|}
            {|Warning:using System.Threading;|}

            namespace TestNamespace
            {
            }
            """, OutsideNamespaceOption, placeSystemNamespaceFirst: true);

    [Fact]
    public Task WhenOutsidePreferred_UsingsInNamespace_UsingsMoved_FileScopedNamespace()
        => TestInRegularAndScriptAsync("""
            namespace TestNamespace;

            [|using System;
            using System.Threading;|]
            """, """

            {|Warning:using System;|}
            {|Warning:using System.Threading;|}
            namespace TestNamespace;

            """, OutsideNamespaceOption, placeSystemNamespaceFirst: true);

    /// <summary>
    /// Verifies that simplified using statements in a namespace are expanded during the code fix operation.
    /// </summary>
    [Fact]
    public Task WhenOutsidePreferred_SimplifiedUsingInNamespace_UsingsMovedAndExpanded()
        => TestInRegularAndScriptAsync("""
            namespace System
            {
                [|using System;
                using System.Threading;
                using Reflection;|]
            }
            """, """
            {|Warning:using System;|}
            {|Warning:using System.Threading;|}
            {|Warning:using System.Reflection;|}

            namespace System
            {
            }
            """, OutsideNamespaceOption, placeSystemNamespaceFirst: true);

    /// <summary>
    /// Verifies that the code fix will move the using directives when they are present in both the compilation unit and namespace.
    /// </summary>
    [Fact]
    public Task WhenOutsidePreferred_UsingsInBoth_UsingsMoved()
        => TestInRegularAndScriptAsync("""
            using Microsoft.CodeAnalysis;

            namespace TestNamespace
            {
                [|using System;|]
            }
            """, """
            using Microsoft.CodeAnalysis;
            {|Warning:using System;|}

            namespace TestNamespace
            {
            }
            """, OutsideNamespaceOption, placeSystemNamespaceFirst: true);

    /// <summary>
    /// Verifies that simplified using statements in a namespace are expanded during the code fix operation.
    /// </summary>
    [Fact]
    public Task WhenOutsidePreferred_SimplifiedUsingAliasInNamespace_UsingsMovedAndExpanded()
        => TestInRegularAndScriptAsync("""
            namespace System.MyExtension
            {
                [|using System.Threading;
                using Reflection;
                using Assembly = Reflection.Assembly;
                using List = Collections.Generic.IList<int>;|]
            }
            """, """
            {|Warning:using System.Threading;|}
            {|Warning:using System.Reflection;|}
            {|Warning:using Assembly = System.Reflection.Assembly;|}
            {|Warning:using List = System.Collections.Generic.IList<int>;|}

            namespace System.MyExtension
            {
            }
            """, OutsideNamespaceOption, placeSystemNamespaceFirst: true);

    /// <summary>
    /// Verifies that having using statements in the compilation unit will not produce any diagnostics when there are attributes present.
    /// </summary>
    [Fact]
    public Task WhenOutsidePreferred_UsingsInNamespaceAndCompilationUnitWithAttributes_UsingsMoved()
        => TestInRegularAndScriptAsync("""
            using System.Reflection;

            [assembly: AssemblyVersion("1.0.0.0")]

            namespace TestNamespace
            {
                [|using System;
                using System.Threading;|]
            }
            """, """
            using System.Reflection;
            {|Warning:using System;|}
            {|Warning:using System.Threading;|}

            [assembly: AssemblyVersion("1.0.0.0")]

            namespace TestNamespace
            {
            }
            """, OutsideNamespaceOption, placeSystemNamespaceFirst: true);

    /// <summary>
    /// Verifies that the file header of a file is properly preserved when moving using statements out of a namespace.
    /// </summary>
    [Fact]
    public Task WhenOutsidePreferred_UsingsInNamespaceAndCompilationUnitHasFileHeader_UsingsMovedAndHeaderPreserved()
        => TestInRegularAndScriptAsync("""
            // Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
            // Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

            namespace TestNamespace
            {
                [|using System;|]
            }
            """, """
            // Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
            // Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

            {|Warning:using System;|}

            namespace TestNamespace
            {
            }
            """, OutsideNamespaceOption, placeSystemNamespaceFirst: true);

    [Fact]
    public Task WhenOutsidePreferred_UsingsInNamespaceWithCommentsAndCompilationUnitHasFileHeader_UsingsMovedWithCommentsAndHeaderPreserved()
        => TestInRegularAndScriptAsync("""
            // Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
            // Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

            namespace TestNamespace
            {
                // Separated Comment

                [|using System.Collections;
                // Comment
                using System;|]
            }
            """, """
            // Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
            // Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

            // Separated Comment

            {|Warning:using System.Collections;|}
            // Comment
            {|Warning:using System;|}

            namespace TestNamespace
            {
            }
            """, OutsideNamespaceOption, placeSystemNamespaceFirst: true);

    [Fact]
    public Task WhenOutsidePreferred_UsingsInNamespace_UsingsMovedAndSystemPlacedFirstIgnored()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """, OutsideNamespaceOption, placeSystemNamespaceFirst: true);

    [Fact]
    public Task WhenOutsidePreferred_UsingsInNamespace_UsingsMovedAndAlphaSortIgnored()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """, OutsideNamespaceOption, placeSystemNamespaceFirst: false);

    /// <summary>
    /// Verifies that simplified using statements in nested namespace are expanded during the code fix operation.
    /// </summary>
    [Fact]
    public Task WhenOutsidePreferred_UsingsInNestedNamespaces_UsingsMovedAndExpanded()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """, OutsideNamespaceOption, placeSystemNamespaceFirst: true);

    /// <summary>
    /// Verifies that simplified using statements in multiple namespaces are expanded during the code fix operation.
    /// </summary>
    [Fact]
    public Task WhenOutsidePreferred_UsingsInMultipleNamespaces_UsingsMovedAndExpanded()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """, OutsideNamespaceOption, placeSystemNamespaceFirst: true);

    /// <summary>
    /// Verifies that simplified using statements in multiple namespaces are deduplicated during the code fix operation.
    /// </summary>
    [Fact]
    public Task WhenOutsidePreferred_UsingsInMultipleNamespaces_UsingsMovedAndDeduplicated()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """, OutsideNamespaceOption, placeSystemNamespaceFirst: true);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61773")]
    public Task WhenOutsidePreferred_MoveGlobalUsing1()
        => TestInRegularAndScriptAsync("""
            namespace N1
            {
                [|global using System;|]
            }
            """, """
            {|Warning:global using System;|}

            namespace N1
            {
            }
            """, OutsideNamespaceOption, placeSystemNamespaceFirst: true);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75961")]
    public Task WhenOutsidePreferred_AliasToLocalType_FileScopedNamespace()
        => TestInRegularAndScriptAsync("""
            namespace Goo;

            [|using Alias = C;|]

            class C;
            """, """

            {|Warning:using Alias = Goo.C;|}

            namespace Goo;
            class C;
            """, OutsideNamespaceOption, placeSystemNamespaceFirst: true);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75961")]
    public Task WhenOutsidePreferred_AliasToLocalType_BlockNamespace()
        => TestInRegularAndScriptAsync("""
            namespace Goo
            {
                [|using Alias = C;|]

                class C;
            }
            """, """
            {|Warning:using Alias = Goo.C;|}

            namespace Goo
            {
                class C;
            }
            """, OutsideNamespaceOption, placeSystemNamespaceFirst: true);

    #endregion

    #region OutsideNamespaceIgnoringAliases

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43271")]
    public Task WhenOutsideIgnoringAliasesPreferred_UsingsInNamespace_UsingsMoved()
        => TestInRegularAndScriptAsync("""
            namespace TestNamespace
            {
                [|using System;
                using System.Threading;|]
                using SCG = System.Collections.Generic;
            }
            """, """
            {|Warning:using System;|}
            {|Warning:using System.Threading;|}

            namespace TestNamespace
            {
                using SCG = System.Collections.Generic;
            }
            """, OutsideNamespaceIgnoringAliasesOption, placeSystemNamespaceFirst: true);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43271")]
    public Task WhenOutsideIgnoringAliasesPreferred_UsingsInNamespace_UsingsMoved_InnerType()
        => TestInRegularAndScriptAsync("""
            namespace TestNamespace
            {
                [|using System;
                using System.Threading;|]
                using SCG = System.Collections.Generic;

                class C
                {
                }
            }
            """, """
            {|Warning:using System;|}
            {|Warning:using System.Threading;|}

            namespace TestNamespace
            {
                using SCG = System.Collections.Generic;

                class C
                {
                }
            }
            """, OutsideNamespaceIgnoringAliasesOption, placeSystemNamespaceFirst: true);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43271")]
    public Task WhenOutsideIgnoringAliasesPreferred_UsingsInNamespace_UsingsMoved_AliasInMiddle()
        => TestInRegularAndScriptAsync("""
            namespace TestNamespace
            {
                [|using System;
                using SCG = System.Collections.Generic;
                using System.Threading;|]

                class C
                {
                }
            }
            """, """
            {|Warning:using System;|}
            {|Warning:using System.Threading;|}

            namespace TestNamespace
            {
                using SCG = System.Collections.Generic;

                class C
                {
                }
            }
            """, OutsideNamespaceIgnoringAliasesOption, placeSystemNamespaceFirst: true);

    #endregion

    #region Test InsideNamespace

    /// <summary>
    /// Verifies that valid using statements in a namespace does not produce any diagnostics.
    /// </summary>
    [Fact]
    public Task WhenInsidePreferred_UsingsInNamespace_ValidUsingStatements()
        => TestDiagnosticMissingAsync("""
            namespace TestNamespace
            {
                [|using System;
                using System.Threading;|]
            }
            """, InsideNamespaceOption);

    [Fact]
    public Task WhenInsidePreferred_UsingsInNamespace_ValidUsingStatements_FileScopedNamespace()
        => TestDiagnosticMissingAsync("""
            namespace TestNamespace;

            [|using System;
            using System.Threading;|]
            """, InsideNamespaceOption);

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
        => TestDiagnosticMissingAsync($"""
            [|using System;|]

            {typeDefinition}
            """, InsideNamespaceOption);

    /// <summary>
    /// Verifies that having using statements in the compilation unit will not produce any diagnostics when there are attributes present.
    /// </summary>
    [Fact]
    public Task WhenInsidePreferred_UsingsInCompilationUnitWithAttributes_ValidUsingStatements()
        => TestDiagnosticMissingAsync("""
            [|using System.Reflection;|]

            [assembly: AssemblyVersion("1.0.0.0")]

            namespace TestNamespace
            {
                using System;
                using System.Threading;
            }
            """, InsideNamespaceOption);

    /// <summary>
    /// Verifies that the code fix will move the using directives and not place System directives first.
    /// </summary>
    [Fact]
    public Task WhenInsidePreferred_UsingsInCompilationUnit_UsingsMovedAndSystemPlacedFirstIgnored()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """, InsideNamespaceOption, placeSystemNamespaceFirst: true);

    /// <summary>
    /// Verifies that the code fix will move the using directives and not sort them alphabetically.
    /// </summary>
    [Fact]
    public Task WhenInsidePreferred_UsingsInCompilationUnit_UsingsAndWithAlphaSortIgnored()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """, InsideNamespaceOption, placeSystemNamespaceFirst: false);

    /// <summary>
    /// Verifies that the code fix will move the using directives, but will not move a file header comment separated by an new line.
    /// </summary>
    [Fact]
    public Task WhenInsidePreferred_UsingsInCompilationUnitWithFileHeader_UsingsMovedNotHeader()
        => TestInRegularAndScriptAsync("""
            // This is a file header.
            [|using Microsoft.CodeAnalysis;
            using System;|]

            namespace TestNamespace
            {
            }
            """, """
            // This is a file header.
            namespace TestNamespace
            {
                {|Warning:using Microsoft.CodeAnalysis;|}
                {|Warning:using System;|}
            }
            """, InsideNamespaceOption, placeSystemNamespaceFirst: true);

    /// <summary>
    /// Verifies that the code fix will move the using directives when they are present in both the compilation unit and namespace.
    /// </summary>
    [Fact]
    public Task WhenInsidePreferred_UsingsInBoth_UsingsMoved()
        => TestInRegularAndScriptAsync("""
            [|using Microsoft.CodeAnalysis;|]

            namespace TestNamespace
            {
                using System;
            }
            """, """
            namespace TestNamespace
            {
                {|Warning:using Microsoft.CodeAnalysis;|}
                using System;
            }
            """, InsideNamespaceOption, placeSystemNamespaceFirst: true);

    [Fact]
    public Task WhenInsidePreferred_UsingsInBoth_UsingsMoved_FileScopedNamespace()
        => TestInRegularAndScriptAsync("""
            [|using Microsoft.CodeAnalysis;|]

            namespace TestNamespace;

            using System;
            """, """
            namespace TestNamespace;

            {|Warning:using Microsoft.CodeAnalysis;|}

            using System;
            """, InsideNamespaceOption, placeSystemNamespaceFirst: true);

    /// <summary>
    /// Verifies that the code fix will properly move separated trivia, but will not move a file header comment.
    /// </summary>
    [Fact]
    public Task WhenInsidePreferred_UsingsInCompilationUnitWithFileHeaderAndTrivia_UsingsAndTriviaMovedNotHeader()
        => TestInRegularAndScriptAsync("""
            // File Header

            // Leading Comment

            [|using Microsoft.CodeAnalysis;
            using System;|]

            namespace TestNamespace
            {
            }
            """, """
            // File Header

            namespace TestNamespace
            {
                // Leading Comment

                {|Warning:using Microsoft.CodeAnalysis;|}
                {|Warning:using System;|}
            }
            """, InsideNamespaceOption, placeSystemNamespaceFirst: true);

    /// <summary>
    /// Verifies that a code fix will not be offered for MisplacedUsing diagnostics when multiple namespaces are present.
    /// </summary>
    [Fact]
    public Task WhenInsidePreferred_UsingsInCompilationUnitWithMultipleNamespaces_NoCodeFixOffered()
        => TestMissingAsync("""
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
            """, InsideNamespaceOption);

    /// <summary>
    /// Verifies that the code fix will properly move pragmas.
    /// </summary>
    [Fact]
    public Task WhenInsidePreferred_UsingsInCompilationUnitWithPragma_PragmaMoved()
        => TestInRegularAndScriptAsync("""
            #pragma warning disable 1573 // Comment
            [|using System;
            using System.Threading;|]

            namespace TestNamespace
            {
            }
            """, """
            namespace TestNamespace
            {
            #pragma warning disable 1573 // Comment
                {|Warning:using System;|}
                {|Warning:using System.Threading;|}
            }
            """, InsideNamespaceOption, placeSystemNamespaceFirst: true);

    /// <summary>
    /// Verifies that the code fix will properly move regions.
    /// </summary>
    [Fact]
    public Task WhenInsidePreferred_UsingsInCompilationUnitWithRegion_RegionMoved()
        => TestInRegularAndScriptAsync("""
            #region Comment
            #endregion Comment
            [|using System;
            using System.Threading;|]

            namespace TestNamespace
            {
            }
            """, """
            namespace TestNamespace
            {
                #region Comment
                #endregion Comment
                {|Warning:using System;|}
                {|Warning:using System.Threading;|}
            }
            """, InsideNamespaceOption, placeSystemNamespaceFirst: true);

    /// <summary>
    /// Verifies that the code fix will properly move comment trivia.
    /// </summary>
    [Fact]
    public Task WhenInsidePreferred_UsingsInCompilationUnitWithCommentTrivia_TriviaMoved()
        => TestInRegularAndScriptAsync("""

            // Some comment
            [|using System;
            using System.Threading;|]

            namespace TestNamespace
            {
            }
            """, """
            namespace TestNamespace
            {

                // Some comment
                {|Warning:using System;|}
                {|Warning:using System.Threading;|}
            }
            """, InsideNamespaceOption, placeSystemNamespaceFirst: true);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61773")]
    public Task WhenInsidePreferred_DoNotMoveGlobalUsings1()
        => TestMissingAsync("""
            [|global using System;|]

            namespace TestNamespace
            {
            }
            """, InsideNamespaceOption);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61773")]
    public Task WhenInsidePreferred_DoNotMoveGlobalUsings2()
        => TestInRegularAndScriptAsync("""
            [|global using System;
            using System.Threading;|]

            namespace TestNamespace
            {
            }
            """, """
            global using System;

            namespace TestNamespace
            {
                {|Warning:using System.Threading;|}
            }
            """, InsideNamespaceOption, placeSystemNamespaceFirst: true);

    #endregion

    #region Preprocessor Directives

    /// <summary>
    /// Verifies that preprocessor directives surrounding using statements are moved correctly.
    /// This tests the scenario from https://github.com/dotnet/roslyn/issues/31249
    /// </summary>
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31249")]
    public Task WhenOutsidePreferred_UsingsWithPreprocessorDirectives_DirectivesMovedCorrectly()
        => TestInRegularAndScriptAsync("""
            using System;

            namespace MyNamespace
            {
            #if !FOO
                [|using System.Runtime.CompilerServices;|]
            #endif

                class Program
                {
                    static void Main() { }
                }
            }
            """, """
            using System;
            #if !FOO
            {|Warning:using System.Runtime.CompilerServices;|}
            #endif

            namespace MyNamespace
            {
                class Program
                {
                    static void Main() { }
                }
            }
            """, OutsideNamespaceOption, placeSystemNamespaceFirst: true);

    /// <summary>
    /// Verifies that preprocessor directives at the start of a namespace are handled correctly.
    /// This tests the scenario from https://github.com/dotnet/roslyn/issues/31249
    /// </summary>
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31249")]
    public Task WhenOutsidePreferred_NamespaceWithPreprocessorDirectives_DirectivesMovedCorrectly()
        => TestInRegularAndScriptAsync("""
            #if NET6_0

            namespace ConsoleApp
            {
                [|using System;|]

                internal class Class1
                {
                    public void M()
                    {
                        Console.WriteLine("");
                    }
                }
            }

            #endif
            """, """
            #if NET6_0

            {|Warning:using System;|}

            namespace ConsoleApp
            {
                internal class Class1
                {
                    public void M()
                    {
                        Console.WriteLine("");
                    }
                }
            }

            #endif
            """, OutsideNamespaceOption, placeSystemNamespaceFirst: true);

    /// <summary>
    /// Verifies that multiple using statements wrapped in preprocessor directives are handled correctly.
    /// </summary>
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31249")]
    public Task WhenOutsidePreferred_MultipleUsingsWithPreprocessorDirectives_DirectivesMovedCorrectly()
        => TestInRegularAndScriptAsync("""
            using System;

            namespace MyNamespace
            {
            #if DEBUG
                [|using System.Diagnostics;
                using System.Runtime.CompilerServices;|]
            #endif

                class Program
                {
                    static void Main() { }
                }
            }
            """, """
            using System;
            #if DEBUG
            {|Warning:using System.Diagnostics;|}
            {|Warning:using System.Runtime.CompilerServices;|}
            #endif

            namespace MyNamespace
            {
                class Program
                {
                    static void Main() { }
                }
            }
            """, OutsideNamespaceOption, placeSystemNamespaceFirst: true);

    #endregion
}
