// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.Configuration.ConfigureCodeStyle;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.TypeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.Configuration.ConfigureCodeStyle
{
    public abstract partial class MultipleCodeStyleOptionConfigurationTests : AbstractSuppressionDiagnosticTest_NoEditor
    {
        protected abstract int OptionIndex { get; }

        protected override ImmutableArray<CodeAction> MassageActions(ImmutableArray<CodeAction> actions)
        {
            Assert.Single(actions);
            var nestedActionForOptionIndex = ((AbstractConfigurationActionWithNestedActions)actions[0]).NestedActions[OptionIndex];
            return base.MassageActions(ImmutableArray.Create(nestedActionForOptionIndex));
        }

        protected internal override string GetLanguage() => LanguageNames.CSharp;

        protected override ParseOptions GetScriptOptions() => Options.Script;

        internal override Tuple<DiagnosticAnalyzer, IConfigurationFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            /*
                csharp_style_var_elsewhere
                csharp_style_var_for_built_in_types
                csharp_style_var_when_type_is_apparent                
             */
            return new Tuple<DiagnosticAnalyzer, IConfigurationFixProvider>(
                        new CSharpUseExplicitTypeDiagnosticAnalyzer(), new ConfigureCodeStyleOptionCodeFixProvider());
        }

        [Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
        public class VarElsewhere_TrueConfigurationTests : MultipleCodeStyleOptionConfigurationTests
        {
            protected override int OptionIndex => 0;

            protected override int CodeActionIndex => 0;

            [ConditionalFact(typeof(IsEnglishLocal))]
            public async Task ConfigureEditorconfig_Empty_True()
            {
                var input = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document FilePath="z:\\file.cs">
                    class Program1
                    {
                        static void Main()
                        {
                            // { csharp_style_var_when_type_is_apparent, csharp_style_var_for_built_in_types, csharp_style_var_elsewhere }
                            [|var obj = new Program1();|]
                        }
                    }
                            </Document>
                            <AnalyzerConfigDocument FilePath="z:\\.editorconfig"></AnalyzerConfigDocument>
                        </Project>
                    </Workspace>
                    """;

                var expected = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                             <Document FilePath="z:\\file.cs">
                    class Program1
                    {
                        static void Main()
                        {
                            // { csharp_style_var_when_type_is_apparent, csharp_style_var_for_built_in_types, csharp_style_var_elsewhere }
                            var obj = new Program1();
                        }
                    }
                            </Document>
                            <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]

                    # IDE0008: Use explicit type
                    csharp_style_var_elsewhere = true
                    </AnalyzerConfigDocument>
                        </Project>
                    </Workspace>
                    """;

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [Fact]
            public async Task ConfigureEditorconfig_RuleExists_True()
            {
                var input = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document FilePath="z:\\file.cs">
                    class Program1
                    {
                        static void Main()
                        {
                            // { csharp_style_var_when_type_is_apparent, csharp_style_var_for_built_in_types, csharp_style_var_elsewhere }
                            [|var obj = new Program1();|]
                        }
                    }
                            </Document>
                            <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]    # Comment1
                    csharp_style_var_elsewhere = false:suggestion    ; Comment2
                    csharp_style_var_for_built_in_types = true:suggestion    ; Comment3
                    </AnalyzerConfigDocument>
                        </Project>
                    </Workspace>
                    """;

                var expected = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                             <Document FilePath="z:\\file.cs">
                    class Program1
                    {
                        static void Main()
                        {
                            // { csharp_style_var_when_type_is_apparent, csharp_style_var_for_built_in_types, csharp_style_var_elsewhere }
                            var obj = new Program1();
                        }
                    }
                            </Document>
                            <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]    # Comment1
                    csharp_style_var_elsewhere = true:suggestion    ; Comment2
                    csharp_style_var_for_built_in_types = true:suggestion    ; Comment3
                    </AnalyzerConfigDocument>
                        </Project>
                    </Workspace>
                    """;

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [Fact]
            public async Task ConfigureEditorconfig_RuleExists_True_WithoutSeveritySuffix()
            {
                var input = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document FilePath="z:\\file.cs">
                    class Program1
                    {
                        static void Main()
                        {
                            // { csharp_style_var_when_type_is_apparent, csharp_style_var_for_built_in_types, csharp_style_var_elsewhere }
                            [|var obj = new Program1();|]
                        }
                    }
                            </Document>
                            <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]    # Comment1
                    csharp_style_var_elsewhere = false    ; Comment2
                    csharp_style_var_for_built_in_types = true    ; Comment3
                    </AnalyzerConfigDocument>
                        </Project>
                    </Workspace>
                    """;

                var expected = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                             <Document FilePath="z:\\file.cs">
                    class Program1
                    {
                        static void Main()
                        {
                            // { csharp_style_var_when_type_is_apparent, csharp_style_var_for_built_in_types, csharp_style_var_elsewhere }
                            var obj = new Program1();
                        }
                    }
                            </Document>
                            <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]    # Comment1
                    csharp_style_var_elsewhere = true    ; Comment2
                    csharp_style_var_for_built_in_types = true    ; Comment3
                    </AnalyzerConfigDocument>
                        </Project>
                    </Workspace>
                    """;

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [ConditionalFact(typeof(IsEnglishLocal))]
            public async Task ConfigureEditorconfig_InvalidHeader_True()
            {
                var input = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document FilePath="z:\\file.cs">
                    class Program1
                    {
                        static void Main()
                        {
                            // { csharp_style_var_when_type_is_apparent, csharp_style_var_for_built_in_types, csharp_style_var_elsewhere }
                            [|var obj = new Program1();|]
                        }
                    }
                            </Document>
                            <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.vb]
                    csharp_style_var_elsewhere = false:suggestion
                    </AnalyzerConfigDocument>
                        </Project>
                    </Workspace>
                    """;

                var expected = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document FilePath="z:\\file.cs">
                    class Program1
                    {
                        static void Main()
                        {
                            // { csharp_style_var_when_type_is_apparent, csharp_style_var_for_built_in_types, csharp_style_var_elsewhere }
                            var obj = new Program1();
                        }
                    }
                            </Document>
                            <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.vb]
                    csharp_style_var_elsewhere = false:suggestion

                    [*.cs]

                    # IDE0008: Use explicit type
                    csharp_style_var_elsewhere = true
                    </AnalyzerConfigDocument>
                        </Project>
                    </Workspace>
                    """;

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [Fact]
            public async Task ConfigureEditorconfig_MaintainSeverity_True()
            {
                var input = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document FilePath="z:\\file.cs">
                    class Program1
                    {
                        static void Main()
                        {
                            // { csharp_style_var_when_type_is_apparent, csharp_style_var_for_built_in_types, csharp_style_var_elsewhere }
                            [|var obj = new Program1();|]
                        }
                    }
                            </Document>
                            <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.{vb,cs}]
                    csharp_style_var_elsewhere = false:suggestion
                    </AnalyzerConfigDocument>
                        </Project>
                    </Workspace>
                    """;

                var expected = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                             <Document FilePath="z:\\file.cs">
                    class Program1
                    {
                        static void Main()
                        {
                            // { csharp_style_var_when_type_is_apparent, csharp_style_var_for_built_in_types, csharp_style_var_elsewhere }
                            var obj = new Program1();
                        }
                    }
                            </Document>
                            <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.{vb,cs}]
                    csharp_style_var_elsewhere = true:suggestion
                    </AnalyzerConfigDocument>
                        </Project>
                    </Workspace>
                    """;

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [ConditionalFact(typeof(IsEnglishLocal))]
            public async Task ConfigureEditorconfig_InvalidRule_True()
            {
                var input = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document FilePath="z:\\file.cs">
                    class Program1
                    {
                        static void Main()
                        {
                            // { csharp_style_var_when_type_is_apparent, csharp_style_var_for_built_in_types, csharp_style_var_elsewhere }
                            [|var obj = new Program1();|]
                        }
                    }
                            </Document>
                            <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                    csharp_style_var_when_type_is_apparent_error = false:suggestion
                    </AnalyzerConfigDocument>
                        </Project>
                    </Workspace>
                    """;

                var expected = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document FilePath="z:\\file.cs">
                    class Program1
                    {
                        static void Main()
                        {
                            // { csharp_style_var_when_type_is_apparent, csharp_style_var_for_built_in_types, csharp_style_var_elsewhere }
                            var obj = new Program1();
                        }
                    }
                            </Document>
                            <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                    csharp_style_var_when_type_is_apparent_error = false:suggestion

                    # IDE0008: Use explicit type
                    csharp_style_var_elsewhere = true
                    </AnalyzerConfigDocument>
                        </Project>
                    </Workspace>
                    """;

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }
        }

        [Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
        public class VarForBuiltInTypes_FalseConfigurationTests : MultipleCodeStyleOptionConfigurationTests
        {
            protected override int OptionIndex => 1;

            protected override int CodeActionIndex => 1;

            [ConditionalFact(typeof(IsEnglishLocal))]
            public async Task ConfigureEditorconfig_Empty_False()
            {
                var input = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document FilePath="z:\\file.cs">
                    class Program1
                    {
                        static void Main()
                        {
                            // { csharp_style_var_when_type_is_apparent, csharp_style_var_for_built_in_types, csharp_style_var_elsewhere }
                            [|var obj = new Program1();|]
                        }
                    }
                            </Document>
                            <AnalyzerConfigDocument FilePath="z:\\.editorconfig"></AnalyzerConfigDocument>
                        </Project>
                    </Workspace>
                    """;

                var expected = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                             <Document FilePath="z:\\file.cs">
                    class Program1
                    {
                        static void Main()
                        {
                            // { csharp_style_var_when_type_is_apparent, csharp_style_var_for_built_in_types, csharp_style_var_elsewhere }
                            var obj = new Program1();
                        }
                    }
                            </Document>
                            <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]

                    # IDE0008: Use explicit type
                    csharp_style_var_for_built_in_types = false
                    </AnalyzerConfigDocument>
                        </Project>
                    </Workspace>
                    """;

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [Fact]
            public async Task ConfigureEditorconfig_RuleExists_False()
            {
                var input = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document FilePath="z:\\file.cs">
                    class Program1
                    {
                        static void Main()
                        {
                            // { csharp_style_var_when_type_is_apparent, csharp_style_var_for_built_in_types, csharp_style_var_elsewhere }
                            [|var obj = new Program1();|]
                        }
                    }
                            </Document>
                            <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                    csharp_style_var_for_built_in_types = true:silent
                    </AnalyzerConfigDocument>
                        </Project>
                    </Workspace>
                    """;

                var expected = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                             <Document FilePath="z:\\file.cs">
                    class Program1
                    {
                        static void Main()
                        {
                            // { csharp_style_var_when_type_is_apparent, csharp_style_var_for_built_in_types, csharp_style_var_elsewhere }
                            var obj = new Program1();
                        }
                    }
                            </Document>
                            <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                    csharp_style_var_for_built_in_types = false:silent
                    </AnalyzerConfigDocument>
                        </Project>
                    </Workspace>
                    """;

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [Fact]
            public async Task ConfigureEditorconfig_RuleExists_False_WithoutSeveritySuffix()
            {
                var input = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document FilePath="z:\\file.cs">
                    class Program1
                    {
                        static void Main()
                        {
                            // { csharp_style_var_when_type_is_apparent, csharp_style_var_for_built_in_types, csharp_style_var_elsewhere }
                            [|var obj = new Program1();|]
                        }
                    }
                            </Document>
                            <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                    csharp_style_var_for_built_in_types = true
                    </AnalyzerConfigDocument>
                        </Project>
                    </Workspace>
                    """;

                var expected = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                             <Document FilePath="z:\\file.cs">
                    class Program1
                    {
                        static void Main()
                        {
                            // { csharp_style_var_when_type_is_apparent, csharp_style_var_for_built_in_types, csharp_style_var_elsewhere }
                            var obj = new Program1();
                        }
                    }
                            </Document>
                            <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                    csharp_style_var_for_built_in_types = false
                    </AnalyzerConfigDocument>
                        </Project>
                    </Workspace>
                    """;

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [ConditionalFact(typeof(IsEnglishLocal))]
            public async Task ConfigureEditorconfig_InvalidHeader_False()
            {
                var input = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document FilePath="z:\\file.cs">
                    class Program1
                    {
                        static void Main()
                        {
                            // { csharp_style_var_when_type_is_apparent, csharp_style_var_for_built_in_types, csharp_style_var_elsewhere }
                            [|var obj = new Program1();|]
                        }
                    }
                            </Document>
                            <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.vb]
                    csharp_style_var_for_built_in_types = true:silent
                    </AnalyzerConfigDocument>
                        </Project>
                    </Workspace>
                    """;

                var expected = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document FilePath="z:\\file.cs">
                    class Program1
                    {
                        static void Main()
                        {
                            // { csharp_style_var_when_type_is_apparent, csharp_style_var_for_built_in_types, csharp_style_var_elsewhere }
                            var obj = new Program1();
                        }
                    }
                            </Document>
                            <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.vb]
                    csharp_style_var_for_built_in_types = true:silent

                    [*.cs]

                    # IDE0008: Use explicit type
                    csharp_style_var_for_built_in_types = false
                    </AnalyzerConfigDocument>
                        </Project>
                    </Workspace>
                    """;

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [Fact]
            public async Task ConfigureEditorconfig_MaintainSeverity_False()
            {
                var input = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document FilePath="z:\\file.cs">
                    class Program1
                    {
                        static void Main()
                        {
                            // { csharp_style_var_when_type_is_apparent, csharp_style_var_for_built_in_types, csharp_style_var_elsewhere }
                            [|var obj = new Program1();|]
                        }
                    }
                            </Document>
                            <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.{vb,cs}]
                    csharp_style_var_for_built_in_types = true:suggestion
                    </AnalyzerConfigDocument>
                        </Project>
                    </Workspace>
                    """;

                var expected = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                             <Document FilePath="z:\\file.cs">
                    class Program1
                    {
                        static void Main()
                        {
                            // { csharp_style_var_when_type_is_apparent, csharp_style_var_for_built_in_types, csharp_style_var_elsewhere }
                            var obj = new Program1();
                        }
                    }
                            </Document>
                            <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.{vb,cs}]
                    csharp_style_var_for_built_in_types = false:suggestion
                    </AnalyzerConfigDocument>
                        </Project>
                    </Workspace>
                    """;

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [ConditionalFact(typeof(IsEnglishLocal))]
            public async Task ConfigureEditorconfig_InvalidRule_False()
            {
                var input = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document FilePath="z:\\file.cs">
                    class Program1
                    {
                        static void Main()
                        {
                            // { csharp_style_var_when_type_is_apparent, csharp_style_var_for_built_in_types, csharp_style_var_elsewhere }
                            [|var obj = new Program1();|]
                        }
                    }
                            </Document>
                            <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                    csharp_style_var_for_built_in_types_error = false:silent
                    </AnalyzerConfigDocument>
                        </Project>
                    </Workspace>
                    """;

                var expected = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document FilePath="z:\\file.cs">
                    class Program1
                    {
                        static void Main()
                        {
                            // { csharp_style_var_when_type_is_apparent, csharp_style_var_for_built_in_types, csharp_style_var_elsewhere }
                            var obj = new Program1();
                        }
                    }
                            </Document>
                            <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                    csharp_style_var_for_built_in_types_error = false:silent

                    # IDE0008: Use explicit type
                    csharp_style_var_for_built_in_types = false
                    </AnalyzerConfigDocument>
                        </Project>
                    </Workspace>
                    """;

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }
        }
    }
}
