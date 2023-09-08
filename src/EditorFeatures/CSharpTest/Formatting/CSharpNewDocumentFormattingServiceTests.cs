﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities.Formatting;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Formatting
{
    public class CSharpNewDocumentFormattingServiceTests : AbstractNewDocumentFormattingServiceTests
    {
        public static IEnumerable<object[]> EndOfDocumentSequences
        {
            get
            {
                yield return new object[] { "" };
                yield return new object[] { """

                    """ };
            }
        }

        protected override string Language => LanguageNames.CSharp;
        protected override TestWorkspace CreateTestWorkspace(string testCode, ParseOptions? parseOptions)
            => TestWorkspace.CreateCSharp(testCode, parseOptions);

        [Fact]
        public async Task TestFileScopedNamespaces()
        {
            await TestAsync(testCode: """
                namespace Goo
                {
                    internal class C
                    {
                    }
                }
                """,
            expected: """
            namespace Goo;

            internal class C
            {
            }
            """,
            options: new OptionsCollection(LanguageNames.CSharp)
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, new CodeStyleOption2<NamespaceDeclarationPreference>(NamespaceDeclarationPreference.FileScoped, NotificationOption2.Error) }
            },
            parseOptions: new CSharpParseOptions(LanguageVersion.CSharp10));
        }

        [Fact]
        public async Task TestFileScopedNamespaces_Invalid_MultipleNamespaces()
        {
            var testCode = """
                namespace Goo
                {
                }

                namespace Bar
                {
                }
                """;

            await TestAsync(
                testCode: testCode,
                expected: testCode,
                options: new OptionsCollection(LanguageNames.CSharp)
                {
                    { CSharpCodeStyleOptions.NamespaceDeclarations, new CodeStyleOption2<NamespaceDeclarationPreference>(NamespaceDeclarationPreference.FileScoped, NotificationOption2.Error) }
                },
                parseOptions: new CSharpParseOptions(LanguageVersion.CSharp10));
        }

        [Fact]
        public async Task TestFileScopedNamespaces_Invalid_WrongLanguageVersion()
        {
            var testCode = """
                namespace Goo
                {
                    internal class C
                    {
                    }
                }
                """;

            await TestAsync(
                testCode: testCode,
                expected: testCode,
                options: new OptionsCollection(LanguageNames.CSharp)
                {
                    { CSharpCodeStyleOptions.NamespaceDeclarations, new CodeStyleOption2<NamespaceDeclarationPreference>(NamespaceDeclarationPreference.FileScoped, NotificationOption2.Error) }
                },
                parseOptions: new CSharpParseOptions(LanguageVersion.CSharp9));
        }

        [Theory]
        [MemberData(nameof(EndOfDocumentSequences))]
        public async Task TestBlockScopedNamespaces(string endOfDocumentSequence)
        {
            await TestAsync(testCode: $@"
namespace Goo;

internal class C
{{
}}{endOfDocumentSequence}",
            expected: $@"
namespace Goo
{{
    internal class C
    {{
    }}
}}{endOfDocumentSequence}",
            options: new OptionsCollection(LanguageNames.CSharp)
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, new CodeStyleOption2<NamespaceDeclarationPreference>(NamespaceDeclarationPreference.BlockScoped, NotificationOption2.Error) }
            });
        }

        [Fact]
        public async Task TestOrganizeUsingsWithNoUsings()
        {
            var testCode = """
                namespace Goo
                {
                }
                """;
            await TestAsync(
                testCode: testCode,
                expected: testCode,
                options: new OptionsCollection(LanguageNames.CSharp)
                {
                    { CSharpCodeStyleOptions.PreferredUsingDirectivePlacement, new CodeStyleOption2<AddImportPlacement>(AddImportPlacement.OutsideNamespace, NotificationOption2.Error) }
                });
        }

        [Fact]
        public async Task TestFileBanners()
        {
            await TestAsync(testCode: """
                using System;

                namespace Goo
                {
                }
                """,
            expected: """
            // This is a banner.

            using System;

            namespace Goo
            {
            }
            """,
            options: new OptionsCollection(LanguageNames.CSharp)
            {
                { CodeStyleOptions2.FileHeaderTemplate, "This is a banner." }
            });
        }

        [Fact]
        public async Task TestAccessibilityModifiers()
        {
            await TestAsync(testCode: """
                using System;

                namespace Goo
                {
                    class C
                    {
                    }
                }
                """,
            expected: """
            using System;

            namespace Goo
            {
                internal class C
                {
                }
            }
            """,
            options: new OptionsCollection(LanguageNames.CSharp)
            {
                { CodeStyleOptions2.AccessibilityModifiersRequired, new CodeStyleOption2<AccessibilityModifiersRequired>(AccessibilityModifiersRequired.Always, NotificationOption2.Error) }
            });
        }

        [Fact]
        public async Task TestAccessibilityModifiers_FileScopedNamespace()
        {
            await TestAsync(testCode: """
                using System;

                namespace Goo
                {
                    class C
                    {
                    }
                }
                """,
            expected: """
            using System;

            namespace Goo;
            internal class C
            {
            }
            """,
            options: new OptionsCollection(LanguageNames.CSharp)
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, new CodeStyleOption2<NamespaceDeclarationPreference>(NamespaceDeclarationPreference.FileScoped, NotificationOption2.Error) },
                { CodeStyleOptions2.AccessibilityModifiersRequired, new CodeStyleOption2<AccessibilityModifiersRequired>(AccessibilityModifiersRequired.Always, NotificationOption2.Error) }
            });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/55703")]
        public async Task TestAccessibilityModifiers_IgnoresPartial()
        {
            await TestAsync(
                testCode: """
                using System;

                namespace Goo
                {
                    class E
                    {
                    }

                    partial class C
                    {
                    }

                    class D
                    {
                    }
                }
                """,
                expected: """
                using System;

                namespace Goo
                {
                    internal class E
                    {
                    }

                    partial class C
                    {
                    }

                    internal class D
                    {
                    }
                }
                """,
                options: new OptionsCollection(LanguageNames.CSharp)
                {
                    { CodeStyleOptions2.AccessibilityModifiersRequired, new CodeStyleOption2<AccessibilityModifiersRequired>(AccessibilityModifiersRequired.Always, NotificationOption2.Error) }
                });
        }

        [Fact]
        public async Task TestUsingDirectivePlacement()
        {
            await TestAsync(testCode: """
                using System;

                namespace Goo
                {
                }
                """,
            expected: """
            namespace Goo
            {
                using System;
            }
            """,
            options: new OptionsCollection(LanguageNames.CSharp)
            {
                { CSharpCodeStyleOptions.PreferredUsingDirectivePlacement, new CodeStyleOption2<AddImportPlacement>(AddImportPlacement.InsideNamespace, NotificationOption2.Error) }
            });
        }

        [Fact]
        public async Task TestPreferTopLevelStatements()
        {
            await TestAsync(testCode: """
                using System;

                // See https://aka.ms/new-console-template for more information
                Console.WriteLine("Hello, World!");
                """,
            expected: """
            using System;

            // See https://aka.ms/new-console-template for more information
            Console.WriteLine("Hello, World!");
            """,
            options: new OptionsCollection(LanguageNames.CSharp)
            {
                { CSharpCodeStyleOptions.PreferTopLevelStatements, new CodeStyleOption2<bool>(value: true, notification: NotificationOption2.Suggestion) }
            });
        }

        [Fact]
        public async Task TestPreferProgramMain()
        {
            await TestAsync(testCode: """
                using System;

                // See https://aka.ms/new-console-template for more information
                Console.WriteLine("Hello, World!");
                """,
            expected: """
            using System;

            internal class Program
            {
                private static void Main(string[] args)
                {
                    Console.WriteLine("Hello, World!");
                }
            }
            """,
            options: new OptionsCollection(LanguageNames.CSharp)
            {
                { CSharpCodeStyleOptions.PreferTopLevelStatements, new CodeStyleOption2<bool>(value: false, notification: NotificationOption2.Suggestion) }
            });
        }
    }
}
