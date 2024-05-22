// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.Configuration.ConfigureSeverity;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.Configuration.ConfigureSeverity;

public abstract partial class CategoryBasedSeverityConfigurationTests : AbstractSuppressionDiagnosticTest_NoEditor
{
    private sealed class CustomDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            id: "XYZ0001",
            title: "Title",
            messageFormat: "Message",
            category: "CustomCategory",
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(
                c => c.ReportDiagnostic(Diagnostic.Create(Rule, c.Node.GetLocation())),
                SyntaxKind.ClassDeclaration);
        }
    }

    protected internal override string GetLanguage() => LanguageNames.CSharp;

    protected override ParseOptions GetScriptOptions() => Options.Script;

    internal override Tuple<DiagnosticAnalyzer, IConfigurationFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
    {
        return new Tuple<DiagnosticAnalyzer, IConfigurationFixProvider>(
                    new CustomDiagnosticAnalyzer(), new ConfigureSeverityLevelCodeFixProvider());
    }

    [Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
    public sealed class SilentConfigurationTests : CategoryBasedSeverityConfigurationTests
    {
        /// <summary>
        /// Code action ranges:
        ///     1. (0 - 4) => Code actions for diagnostic "ID" configuration with severity None, Silent, Suggestion, Warning and Error
        ///     2. (5 - 9) => Code actions for diagnostic "Category" configuration with severity None, Silent, Suggestion, Warning and Error
        ///     3. (10 - 14) => Code actions for all analyzer diagnostics configuration with severity None, Silent, Suggestion, Warning and Error
        /// </summary>
        protected override int CodeActionIndex => 6;

        [ConditionalFact(typeof(IsEnglishLocal))]
        public async Task ConfigureEditorconfig_Empty()
        {
            var input = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true" FilePath="z:\\Assembly1.csproj">
                        <Document FilePath="z:\\file.cs">
                [|class Program1 { }|]
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig"></AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            var expected = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true" FilePath="z:\\Assembly1.csproj">
                         <Document FilePath="z:\\file.cs">
                class Program1 { }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]

                # Default severity for analyzer diagnostics with category 'CustomCategory'
                dotnet_analyzer_diagnostic.category-CustomCategory.severity = silent
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
        }

        [Fact]
        public async Task ConfigureEditorconfig_RuleExists()
        {
            var input = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true" FilePath="z:\\Assembly1.csproj">
                        <Document FilePath="z:\\file.cs">
                [|class Program1 { }|]
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                dotnet_analyzer_diagnostic.category-CustomCategory.severity = suggestion   # Comment
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            var expected = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true" FilePath="z:\\Assembly1.csproj">
                         <Document FilePath="z:\\file.cs">
                class Program1 { }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                dotnet_analyzer_diagnostic.category-CustomCategory.severity = silent   # Comment
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
        }

        [Fact]
        public async Task ConfigureEditorconfig_RuleIdEntryExists()
        {
            var input = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true" FilePath="z:\\Assembly1.csproj">
                        <Document FilePath="z:\\file.cs">
                [|class Program1 { }|]
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                dotnet_diagnostic.XYZ0001.severity = suggestion   # Comment
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            var expected = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true" FilePath="z:\\Assembly1.csproj">
                         <Document FilePath="z:\\file.cs">
                class Program1 { }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                dotnet_diagnostic.XYZ0001.severity = suggestion   # Comment

                # Default severity for analyzer diagnostics with category 'CustomCategory'
                dotnet_analyzer_diagnostic.category-CustomCategory.severity = silent
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
        }

        [ConditionalFact(typeof(IsEnglishLocal))]
        public async Task ConfigureEditorconfig_InvalidHeader()
        {
            var input = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true" FilePath="z:\\Assembly1.csproj">
                        <Document FilePath="z:\\file.cs">
                [|class Program1 { }|]
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.vb]
                dotnet_analyzer_diagnostic.category-CustomCategory.severity = suggestion
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            var expected = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true" FilePath="z:\\Assembly1.csproj">
                        <Document FilePath="z:\\file.cs">
                class Program1 { }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.vb]
                dotnet_analyzer_diagnostic.category-CustomCategory.severity = suggestion

                [*.cs]

                # Default severity for analyzer diagnostics with category 'CustomCategory'
                dotnet_analyzer_diagnostic.category-CustomCategory.severity = silent
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
        }

        [Fact]
        public async Task ConfigureEditorconfig_MaintainExistingEntry()
        {
            var input = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true" FilePath="z:\\Assembly1.csproj">
                        <Document FilePath="z:\\file.cs">
                [|class Program1 { }|]
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.{vb,cs}]
                dotnet_analyzer_diagnostic.category-CustomCategory.severity = silent
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            await TestInRegularAndScriptAsync(input, input, CodeActionIndex);
        }

        [Fact]
        public async Task ConfigureEditorconfig_DiagnosticsSuppressed()
        {
            var input = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true" FilePath="z:\\Assembly1.csproj">
                        <Document FilePath="z:\\file.cs">
                [|class Program1 { }|]
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.{vb,cs}]
                dotnet_analyzer_diagnostic.category-CustomCategory.severity = none
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            await TestMissingInRegularAndScriptAsync(input);
        }

        [ConditionalFact(typeof(IsEnglishLocal))]
        public async Task ConfigureEditorconfig_InvalidRule()
        {
            var input = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true" FilePath="z:\\Assembly1.csproj">
                        <Document FilePath="z:\\file.cs">
                [|class Program1 { }|]
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.{vb,cs}]
                dotnet_analyzer_diagnostic.category-XYZ1111Category.severity = suggestion
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            var expected = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true" FilePath="z:\\Assembly1.csproj">
                        <Document FilePath="z:\\file.cs">
                [|class Program1 { }|]
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.{vb,cs}]
                dotnet_analyzer_diagnostic.category-XYZ1111Category.severity = suggestion

                # Default severity for analyzer diagnostics with category 'CustomCategory'
                dotnet_analyzer_diagnostic.category-CustomCategory.severity = silent
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
        }

        [ConditionalFact(typeof(IsEnglishLocal))]
        public async Task ConfigureEditorconfig_RegexHeaderMatch()
        {
            // NOTE: Even though we have a regex match, bulk configuration code fix is always applied to all files
            // within the editorconfig cone, so it generates a new entry.
            var input = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true" FilePath="z:\\Assembly1.csproj">
                        <Document FilePath="z:\\Program/file.cs">
                [|class Program1 { }|]
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*am/fi*e.cs]
                # Default severity for analyzer diagnostics with category 'CustomCategory'
                dotnet_analyzer_diagnostic.category-CustomCategory.severity = warning
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            var expected = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true" FilePath="z:\\Assembly1.csproj">
                         <Document FilePath="z:\\Program/file.cs">
                class Program1 { }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*am/fi*e.cs]
                # Default severity for analyzer diagnostics with category 'CustomCategory'
                dotnet_analyzer_diagnostic.category-CustomCategory.severity = warning

                [*.cs]

                # Default severity for analyzer diagnostics with category 'CustomCategory'
                dotnet_analyzer_diagnostic.category-CustomCategory.severity = silent
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
        }

        [ConditionalFact(typeof(IsEnglishLocal))]
        public async Task ConfigureEditorconfig_RegexHeaderNonMatch()
        {
            var input = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true" FilePath="z:\\Assembly1.csproj">
                        <Document FilePath="z:\\Program/file.cs">
                [|class Program1 { }|]
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*am/fii*e.cs]
                # Default severity for analyzer diagnostics with category 'CustomCategory'
                dotnet_analyzer_diagnostic.category-CustomCategory.severity = warning
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            var expected = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true" FilePath="z:\\Assembly1.csproj">
                         <Document FilePath="z:\\Program/file.cs">
                class Program1 { }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*am/fii*e.cs]
                # Default severity for analyzer diagnostics with category 'CustomCategory'
                dotnet_analyzer_diagnostic.category-CustomCategory.severity = warning

                [*.cs]

                # Default severity for analyzer diagnostics with category 'CustomCategory'
                dotnet_analyzer_diagnostic.category-CustomCategory.severity = silent
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
        }
    }
}
