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
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.Configuration.ConfigureSeverity
{
    public abstract partial class DotNetDiagnosticSeverityBasedSeverityConfigurationTests : AbstractSuppressionDiagnosticTest
    {
        private sealed class CustomDiagnosticAnalyzer : DiagnosticAnalyzer
        {
            private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
                id: "XYZ0001",
                title: "Title",
                messageFormat: "Message",
                category: "Category",
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
        public class NoneConfigurationTests : DotNetDiagnosticSeverityBasedSeverityConfigurationTests
        {
            protected override int CodeActionIndex => 0;

            [ConditionalFact(typeof(IsEnglishLocal))]
            public async Task ConfigureEditorconfig_Empty_None()
            {
                var input = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document FilePath="z:\\file.cs">
                    [|class Program1 { }|]
                            </Document>
                            <AnalyzerConfigDocument FilePath="z:\\.editorconfig"></AnalyzerConfigDocument>
                        </Project>
                    </Workspace>
                    """;

                var expected = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                             <Document FilePath="z:\\file.cs">
                    class Program1 { }
                            </Document>
                            <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]

                    # XYZ0001: Title
                    dotnet_diagnostic.XYZ0001.severity = none
                    </AnalyzerConfigDocument>
                        </Project>
                    </Workspace>
                    """;

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [Fact]
            public async Task ConfigureEditorconfig_RuleExists_None()
            {
                var input = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
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
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                             <Document FilePath="z:\\file.cs">
                    class Program1 { }
                            </Document>
                            <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                    dotnet_diagnostic.XYZ0001.severity = none   # Comment
                    </AnalyzerConfigDocument>
                        </Project>
                    </Workspace>
                    """;

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [ConditionalFact(typeof(IsEnglishLocal))]
            public async Task ConfigureEditorconfig_InvalidHeader_None()
            {
                var input = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document FilePath="z:\\file.cs">
                    [|class Program1 { }|]
                            </Document>
                            <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.vb]
                    dotnet_diagnostic.XYZ0001.severity = suggestion
                    </AnalyzerConfigDocument>
                        </Project>
                    </Workspace>
                    """;

                var expected = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document FilePath="z:\\file.cs">
                    class Program1 { }
                            </Document>
                            <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.vb]
                    dotnet_diagnostic.XYZ0001.severity = suggestion

                    [*.cs]

                    # XYZ0001: Title
                    dotnet_diagnostic.XYZ0001.severity = none
                    </AnalyzerConfigDocument>
                        </Project>
                    </Workspace>
                    """;

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [Fact]
            public async Task ConfigureEditorconfig_MaintainExistingEntry_None()
            {
                var input = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document FilePath="z:\\file.cs">
                    [|class Program1 { }|]
                            </Document>
                            <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.{vb,cs}]
                    dotnet_diagnostic.XYZ0001.severity = none
                    </AnalyzerConfigDocument>
                        </Project>
                    </Workspace>
                    """;

                await TestMissingInRegularAndScriptAsync(input);
            }

            [ConditionalFact(typeof(IsEnglishLocal))]
            public async Task ConfigureEditorconfig_InvalidRule_None()
            {
                var input = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document FilePath="z:\\file.cs">
                    [|class Program1 { }|]
                            </Document>
                            <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.{vb,cs}]
                    dotnet_diagnostic.XYZ1111.severity = none
                    </AnalyzerConfigDocument>
                        </Project>
                    </Workspace>
                    """;

                var expected = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document FilePath="z:\\file.cs">
                    [|class Program1 { }|]
                            </Document>
                            <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.{vb,cs}]
                    dotnet_diagnostic.XYZ1111.severity = none

                    # XYZ0001: Title
                    dotnet_diagnostic.XYZ0001.severity = none
                    </AnalyzerConfigDocument>
                        </Project>
                    </Workspace>
                    """;

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [ConditionalFact(typeof(IsEnglishLocal))]
            [WorkItem("https://github.com/dotnet/roslyn/issues/45446")]
            public async Task ConfigureEditorconfig_MissingRule_None()
            {
                var input = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document FilePath="z:\\file.cs">
                    [|class Program1 { }|]
                            </Document>
                            <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.{vb,cs}]
                    dotnet_diagnostic.severity = none
                    </AnalyzerConfigDocument>
                        </Project>
                    </Workspace>
                    """;

                var expected = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document FilePath="z:\\file.cs">
                    [|class Program1 { }|]
                            </Document>
                            <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.{vb,cs}]
                    dotnet_diagnostic.severity = none

                    # XYZ0001: Title
                    dotnet_diagnostic.XYZ0001.severity = none
                    </AnalyzerConfigDocument>
                        </Project>
                    </Workspace>
                    """;

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [ConditionalFact(typeof(IsEnglishLocal))]
            public async Task ConfigureEditorconfig_RegexHeaderMatch_None()
            {
                var input = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document FilePath="z:\\Program/file.cs">
                    [|class Program1 { }|]
                            </Document>
                            <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*am/fi*e.cs]
                    # XYZ0001: Title
                    dotnet_diagnostic.XYZ0001.severity = warning
                    </AnalyzerConfigDocument>
                        </Project>
                    </Workspace>
                    """;

                var expected = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                             <Document FilePath="z:\\Program/file.cs">
                    class Program1 { }
                            </Document>
                            <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*am/fi*e.cs]
                    # XYZ0001: Title
                    dotnet_diagnostic.XYZ0001.severity = none
                    </AnalyzerConfigDocument>
                        </Project>
                    </Workspace>
                    """;

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [ConditionalFact(typeof(IsEnglishLocal))]
            public async Task ConfigureEditorconfig_RegexHeaderNonMatch_None()
            {
                var input = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document FilePath="z:\\Program/file.cs">
                    [|class Program1 { }|]
                            </Document>
                            <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*am/fii*e.cs]
                    # XYZ0001: Title
                    dotnet_diagnostic.XYZ0001.severity = warning
                    </AnalyzerConfigDocument>
                        </Project>
                    </Workspace>
                    """;

                var expected = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                             <Document FilePath="z:\\Program/file.cs">
                    class Program1 { }
                            </Document>
                            <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*am/fii*e.cs]
                    # XYZ0001: Title
                    dotnet_diagnostic.XYZ0001.severity = warning

                    [*.cs]

                    # XYZ0001: Title
                    dotnet_diagnostic.XYZ0001.severity = none
                    </AnalyzerConfigDocument>
                        </Project>
                    </Workspace>
                    """;

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [ConditionalFact(typeof(IsEnglishLocal))]
            public async Task ConfigureGlobalconfig_Empty_None()
            {
                var input = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document FilePath="z:\\file.cs">
                    [|class Program1 { }|]
                            </Document>
                            <AnalyzerConfigDocument FilePath="z:\\.globalconfig">is_global = true</AnalyzerConfigDocument>
                        </Project>
                    </Workspace>
                    """;

                var expected = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                             <Document FilePath="z:\\file.cs">
                    class Program1 { }
                            </Document>
                            <AnalyzerConfigDocument FilePath="z:\\.globalconfig">is_global = true

                    # XYZ0001: Title
                    dotnet_diagnostic.XYZ0001.severity = none
                    </AnalyzerConfigDocument>
                        </Project>
                    </Workspace>
                    """;

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [Fact]
            public async Task ConfigureGlobalconfig_RuleExists_None()
            {
                var input = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document FilePath="z:\\file.cs">
                    [|class Program1 { }|]
                            </Document>
                            <AnalyzerConfigDocument FilePath="z:\\.globalconfig">is_global = true   # Comment
                    dotnet_diagnostic.XYZ0001.severity = suggestion   # Comment
                    </AnalyzerConfigDocument>
                        </Project>
                    </Workspace>
                    """;

                var expected = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                             <Document FilePath="z:\\file.cs">
                    class Program1 { }
                            </Document>
                            <AnalyzerConfigDocument FilePath="z:\\.globalconfig">is_global = true   # Comment
                    dotnet_diagnostic.XYZ0001.severity = none   # Comment
                    </AnalyzerConfigDocument>
                        </Project>
                    </Workspace>
                    """;

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [ConditionalFact(typeof(IsEnglishLocal))]
            public async Task ConfigureGlobalconfig_InvalidHeader_None()
            {
                var input = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document FilePath="z:\\file.cs">
                    [|class Program1 { }|]
                            </Document>
                            <AnalyzerConfigDocument FilePath="z:\\.globalconfig">[*.vb]
                    dotnet_diagnostic.XYZ0001.severity = suggestion
                    </AnalyzerConfigDocument>
                        </Project>
                    </Workspace>
                    """;

                var expected = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document FilePath="z:\\file.cs">
                    class Program1 { }
                            </Document>
                            <AnalyzerConfigDocument FilePath="z:\\.globalconfig">[*.vb]
                    dotnet_diagnostic.XYZ0001.severity = suggestion

                    [*.cs]

                    # XYZ0001: Title
                    dotnet_diagnostic.XYZ0001.severity = none
                    </AnalyzerConfigDocument>
                        </Project>
                    </Workspace>
                    """;

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }
        }
    }
}
