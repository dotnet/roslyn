// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.Configuration.ConfigureCodeStyle;
using Microsoft.CodeAnalysis.CSharp.RemoveUnusedParametersAndValues;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.Configuration.ConfigureCodeStyle;

public abstract partial class EnumCodeStyleOptionConfigurationTests : AbstractSuppressionDiagnosticTest_NoEditor
{
    protected internal override string GetLanguage() => LanguageNames.CSharp;

    protected override ParseOptions GetScriptOptions() => Options.Script;

    internal override Tuple<DiagnosticAnalyzer, IConfigurationFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
    {
        /*
            /// <summary>
            /// Assignment preference for unused values from expression statements and assignments.
            /// </summary>
            internal enum UnusedValuePreference
            {
                // Unused values must be explicitly assigned to a local variable
                // that is never read/used.
                UnusedLocalVariable = 1,

                // Unused values must be explicitly assigned to a discard '_' variable.
                DiscardVariable = 2,
            }
         */
        return new Tuple<DiagnosticAnalyzer, IConfigurationFixProvider>(
                    new CSharpRemoveUnusedParametersAndValuesDiagnosticAnalyzer(), new ConfigureCodeStyleOptionCodeFixProvider());
    }

    [Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
    public sealed class UnusedLocalVariableConfigurationTests : EnumCodeStyleOptionConfigurationTests
    {
        protected override int CodeActionIndex => 0;

        [ConditionalFact(typeof(IsEnglishLocal))]
        public Task ConfigureEditorconfig_Empty_UnusedLocalVariable()
            => TestInRegularAndScriptAsync("""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // csharp_style_unused_value_assignment_preference = { discard_variable, unused_local_variable }
                        [|var obj = new Program1();|]
                        obj = null;
                        var obj2 = obj;
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig"></AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                         <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // csharp_style_unused_value_assignment_preference = { discard_variable, unused_local_variable }
                        var obj = new Program1();
                        obj = null;
                        var obj2 = obj;
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]

                # IDE0059: Unnecessary assignment of a value
                csharp_style_unused_value_assignment_preference = unused_local_variable
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, CodeActionIndex);

        [Fact]
        public Task ConfigureEditorconfig_RuleExists_UnusedLocalVariable()
            => TestInRegularAndScriptAsync("""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // csharp_style_unused_value_assignment_preference = { discard_variable, unused_local_variable }
                        [|var obj = new Program1();|]
                        obj = null;
                        var obj2 = obj;
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]    # Comment1
                csharp_style_unused_value_assignment_preference = discard_variable:suggestion    ; Comment2
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                         <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // csharp_style_unused_value_assignment_preference = { discard_variable, unused_local_variable }
                        var obj = new Program1();
                        obj = null;
                        var obj2 = obj;
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]    # Comment1
                csharp_style_unused_value_assignment_preference = unused_local_variable:suggestion    ; Comment2
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, CodeActionIndex);

        [ConditionalFact(typeof(IsEnglishLocal))]
        public Task ConfigureEditorconfig_RuleExists_DotnetDiagnosticEntry()
            => TestInRegularAndScriptAsync("""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // csharp_style_unused_value_assignment_preference = { discard_variable, unused_local_variable }
                        [|var obj = new Program1();|]
                        obj = null;
                        var obj2 = obj;
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]    # Comment1
                dotnet_diagnostic.IDE0059.severity = warning    ; Comment2
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                         <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // csharp_style_unused_value_assignment_preference = { discard_variable, unused_local_variable }
                        var obj = new Program1();
                        obj = null;
                        var obj2 = obj;
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]    # Comment1
                dotnet_diagnostic.IDE0059.severity = warning    ; Comment2

                # IDE0059: Unnecessary assignment of a value
                csharp_style_unused_value_assignment_preference = unused_local_variable
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, CodeActionIndex);

        [ConditionalFact(typeof(IsEnglishLocal))]
        public Task ConfigureEditorconfig_RuleExists_ConflictingDotnetDiagnosticEntry()
            => TestInRegularAndScriptAsync("""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // csharp_style_unused_value_assignment_preference = { discard_variable, unused_local_variable }
                        [|var obj = new Program1();|]
                        obj = null;
                        var obj2 = obj;
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]    # Comment1
                dotnet_diagnostic.IDE0059.severity = error    ; Comment2
                csharp_style_unused_value_assignment_preference = discard_variable:suggestion    ; Comment3
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                         <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // csharp_style_unused_value_assignment_preference = { discard_variable, unused_local_variable }
                        var obj = new Program1();
                        obj = null;
                        var obj2 = obj;
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]    # Comment1
                dotnet_diagnostic.IDE0059.severity = error    ; Comment2
                csharp_style_unused_value_assignment_preference = unused_local_variable:suggestion    ; Comment3
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, CodeActionIndex);

        [ConditionalFact(typeof(IsEnglishLocal))]
        public Task ConfigureEditorconfig_InvalidHeader_UnusedLocalVariable()
            => TestInRegularAndScriptAsync("""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // csharp_style_unused_value_assignment_preference = { discard_variable, unused_local_variable }
                        [|var obj = new Program1();|]
                        obj = null;
                        var obj2 = obj;
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.vb]
                csharp_style_unused_value_assignment_preference = discard_variable:suggestion
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // csharp_style_unused_value_assignment_preference = { discard_variable, unused_local_variable }
                        var obj = new Program1();
                        obj = null;
                        var obj2 = obj;
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.vb]
                csharp_style_unused_value_assignment_preference = discard_variable:suggestion

                [*.cs]

                # IDE0059: Unnecessary assignment of a value
                csharp_style_unused_value_assignment_preference = unused_local_variable
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, CodeActionIndex);

        [Fact]
        public Task ConfigureEditorconfig_MaintainSeverity_UnusedLocalVariable()
            => TestInRegularAndScriptAsync("""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // csharp_style_unused_value_assignment_preference = { discard_variable, unused_local_variable }
                        [|var obj = new Program1();|]
                        obj = null;
                        var obj2 = obj;
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.{vb,cs}]
                csharp_style_unused_value_assignment_preference = discard_variable:suggestion
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                         <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // csharp_style_unused_value_assignment_preference = { discard_variable, unused_local_variable }
                        var obj = new Program1();
                        obj = null;
                        var obj2 = obj;
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.{vb,cs}]
                csharp_style_unused_value_assignment_preference = unused_local_variable:suggestion
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, CodeActionIndex);

        [ConditionalFact(typeof(IsEnglishLocal))]
        public Task ConfigureEditorconfig_InvalidRule_UnusedLocalVariable()
            => TestInRegularAndScriptAsync("""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // csharp_style_unused_value_assignment_preference = { discard_variable, unused_local_variable }
                        [|var obj = new Program1();|]
                        obj = null;
                        var obj2 = obj;
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                csharp_style_unused_value_assignment_preferencer = discard_variable:suggestion
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // csharp_style_unused_value_assignment_preference = { discard_variable, unused_local_variable }
                        var obj = new Program1();
                        obj = null;
                        var obj2 = obj;
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                csharp_style_unused_value_assignment_preferencer = discard_variable:suggestion

                # IDE0059: Unnecessary assignment of a value
                csharp_style_unused_value_assignment_preference = unused_local_variable
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, CodeActionIndex);
    }

    [Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
    public sealed class DiscardVariableConfigurationTests : EnumCodeStyleOptionConfigurationTests
    {
        protected override int CodeActionIndex => 1;

        [ConditionalFact(typeof(IsEnglishLocal))]
        public Task ConfigureEditorconfig_Empty_DiscardVariable()
            => TestInRegularAndScriptAsync("""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // csharp_style_unused_value_assignment_preference = { discard_variable, unused_local_variable }
                        [|var obj = new Program1();|]
                        obj = null;
                        var obj2 = obj;
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig"></AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                         <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // csharp_style_unused_value_assignment_preference = { discard_variable, unused_local_variable }
                        var obj = new Program1();
                        obj = null;
                        var obj2 = obj;
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]

                # IDE0059: Unnecessary assignment of a value
                csharp_style_unused_value_assignment_preference = discard_variable
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, CodeActionIndex);

        [Fact]
        public Task ConfigureEditorconfig_RuleExists_DiscardVariable()
            => TestInRegularAndScriptAsync("""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // csharp_style_unused_value_assignment_preference = { discard_variable, unused_local_variable }
                        [|var obj = new Program1();|]
                        obj = null;
                        var obj2 = obj;
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                csharp_style_unused_value_assignment_preference = unused_local_variable:suggestion
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                         <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // csharp_style_unused_value_assignment_preference = { discard_variable, unused_local_variable }
                        var obj = new Program1();
                        obj = null;
                        var obj2 = obj;
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                csharp_style_unused_value_assignment_preference = discard_variable:suggestion
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, CodeActionIndex);

        [Fact]
        public Task ConfigureEditorconfig_RuleExists_DiscardVariable_WithoutSeveritySuffix()
            => TestInRegularAndScriptAsync("""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // csharp_style_unused_value_assignment_preference = { discard_variable, unused_local_variable }
                        [|var obj = new Program1();|]
                        obj = null;
                        var obj2 = obj;
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                csharp_style_unused_value_assignment_preference = unused_local_variable
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                         <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // csharp_style_unused_value_assignment_preference = { discard_variable, unused_local_variable }
                        var obj = new Program1();
                        obj = null;
                        var obj2 = obj;
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                csharp_style_unused_value_assignment_preference = discard_variable
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, CodeActionIndex);

        [ConditionalFact(typeof(IsEnglishLocal))]
        public Task ConfigureEditorconfig_InvalidHeader_DiscardVariable()
            => TestInRegularAndScriptAsync("""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // csharp_style_unused_value_assignment_preference = { discard_variable, unused_local_variable }
                        [|var obj = new Program1();|]
                        obj = null;
                        var obj2 = obj;
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.vb]
                csharp_style_unused_value_assignment_preference = unused_local_variable:suggestion
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // csharp_style_unused_value_assignment_preference = { discard_variable, unused_local_variable }
                        var obj = new Program1();
                        obj = null;
                        var obj2 = obj;
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.vb]
                csharp_style_unused_value_assignment_preference = unused_local_variable:suggestion

                [*.cs]

                # IDE0059: Unnecessary assignment of a value
                csharp_style_unused_value_assignment_preference = discard_variable
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, CodeActionIndex);

        [Fact]
        public Task ConfigureEditorconfig_MaintainSeverity_DiscardVariable()
            => TestInRegularAndScriptAsync("""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // csharp_style_unused_value_assignment_preference = { discard_variable, unused_local_variable }
                        [|var obj = new Program1();|]
                        obj = null;
                        var obj2 = obj;
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.{vb,cs}]
                csharp_style_unused_value_assignment_preference = unused_local_variable:suggestion
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                         <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // csharp_style_unused_value_assignment_preference = { discard_variable, unused_local_variable }
                        var obj = new Program1();
                        obj = null;
                        var obj2 = obj;
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.{vb,cs}]
                csharp_style_unused_value_assignment_preference = discard_variable:suggestion
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, CodeActionIndex);

        [ConditionalFact(typeof(IsEnglishLocal))]
        public Task ConfigureEditorconfig_InvalidRule_DiscardVariable()
            => TestInRegularAndScriptAsync("""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // csharp_style_unused_value_assignment_preference = { discard_variable, unused_local_variable }
                        [|var obj = new Program1();|]
                        obj = null;
                        var obj2 = obj;
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                csharp_style_unused_value_assignment_preference_error = discard_variable:suggestion
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // csharp_style_unused_value_assignment_preference = { discard_variable, unused_local_variable }
                        var obj = new Program1();
                        obj = null;
                        var obj2 = obj;
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                csharp_style_unused_value_assignment_preference_error = discard_variable:suggestion

                # IDE0059: Unnecessary assignment of a value
                csharp_style_unused_value_assignment_preference = discard_variable
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, CodeActionIndex);
    }
}
