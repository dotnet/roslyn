// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.Configuration.ConfigureCodeStyle;
using Microsoft.CodeAnalysis.CSharp.RemoveUnusedParametersAndValues;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.Configuration.ConfigureCodeStyle
{
    public abstract partial class EnumCodeStyleOptionConfigurationTests : AbstractSuppressionDiagnosticTest
    {
        protected override TestWorkspace CreateWorkspaceFromFile(string initialMarkup, TestParameters parameters)
            => TestWorkspace.CreateCSharp(initialMarkup, parameters.parseOptions, parameters.compilationOptions);

        protected override string GetLanguage() => LanguageNames.CSharp;

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
                        new CSharpRemoveUnusedParametersAndValuesDiagnosticAnalyzer(), new ConfigureCodeStyleOptionCodeFixProvider(performExperimentCheck: false));
        }

        public class UnusedLocalVariableConfigurationTests : EnumCodeStyleOptionConfigurationTests
        {
            protected override int CodeActionIndex => 0;

            [ConditionalFact(typeof(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_Empty_UnusedLocalVariable()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.cs"">
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
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig""></AnalyzerConfigDocument>
    </Project>
</Workspace>";

                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document FilePath=""z:\\file.cs"">
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
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.cs]

# IDE0059: Unnecessary assignment of a value
csharp_style_unused_value_assignment_preference = unused_local_variable:suggestion
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_RuleExists_UnusedLocalVariable()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.cs"">
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
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.cs]    # Comment1
csharp_style_unused_value_assignment_preference = discard_variable:suggestion    ; Comment2
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document FilePath=""z:\\file.cs"">
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
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.cs]    # Comment1
csharp_style_unused_value_assignment_preference = unused_local_variable:suggestion    ; Comment2
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [ConditionalFact(typeof(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_InvalidHeader_UnusedLocalVariable()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.cs"">
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
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.vb]
csharp_style_unused_value_assignment_preference = discard_variable:suggestion
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.cs"">
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
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.vb]
csharp_style_unused_value_assignment_preference = discard_variable:suggestion

[*.cs]

# IDE0059: Unnecessary assignment of a value
csharp_style_unused_value_assignment_preference = unused_local_variable:suggestion
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_MaintainSeverity_UnusedLocalVariable()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.cs"">
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
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.{vb,cs}]
csharp_style_unused_value_assignment_preference = discard_variable:suggestion
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document FilePath=""z:\\file.cs"">
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
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.{vb,cs}]
csharp_style_unused_value_assignment_preference = unused_local_variable:suggestion
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [ConditionalFact(typeof(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_InvalidRule_UnusedLocalVariable()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.cs"">
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
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.cs]
csharp_style_unused_value_assignment_preferencer = discard_variable:suggestion
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.cs"">
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
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.cs]
csharp_style_unused_value_assignment_preferencer = discard_variable:suggestion

# IDE0059: Unnecessary assignment of a value
csharp_style_unused_value_assignment_preference = unused_local_variable:suggestion
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }
        }

        public class DiscardVariableConfigurationTests : EnumCodeStyleOptionConfigurationTests
        {
            protected override int CodeActionIndex => 1;

            [ConditionalFact(typeof(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_Empty_DiscardVariable()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.cs"">
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
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig""></AnalyzerConfigDocument>
    </Project>
</Workspace>";

                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document FilePath=""z:\\file.cs"">
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
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.cs]

# IDE0059: Unnecessary assignment of a value
csharp_style_unused_value_assignment_preference = discard_variable:suggestion
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_RuleExists_DiscardVariable()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.cs"">
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
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.cs]
csharp_style_unused_value_assignment_preference = unused_local_variable:suggestion
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document FilePath=""z:\\file.cs"">
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
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.cs]
csharp_style_unused_value_assignment_preference = discard_variable:suggestion
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [ConditionalFact(typeof(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_InvalidHeader_DiscardVariable()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.cs"">
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
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.vb]
csharp_style_unused_value_assignment_preference = unused_local_variable:suggestion
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.cs"">
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
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.vb]
csharp_style_unused_value_assignment_preference = unused_local_variable:suggestion

[*.cs]

# IDE0059: Unnecessary assignment of a value
csharp_style_unused_value_assignment_preference = discard_variable:suggestion
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_MaintainSeverity_DiscardVariable()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.cs"">
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
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.{vb,cs}]
csharp_style_unused_value_assignment_preference = unused_local_variable:suggestion
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document FilePath=""z:\\file.cs"">
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
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.{vb,cs}]
csharp_style_unused_value_assignment_preference = discard_variable:suggestion
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [ConditionalFact(typeof(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_InvalidRule_DiscardVariable()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.cs"">
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
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.cs]
csharp_style_unused_value_assignment_preference_error = discard_variable:suggestion
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.cs"">
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
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.cs]
csharp_style_unused_value_assignment_preference_error = discard_variable:suggestion

# IDE0059: Unnecessary assignment of a value
csharp_style_unused_value_assignment_preference = discard_variable:suggestion
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }
        }
    }
}
