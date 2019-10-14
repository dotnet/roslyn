// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.Configuration.ConfigureSeverity;
using Microsoft.CodeAnalysis.CSharp.RemoveUnusedParametersAndValues;
using Microsoft.CodeAnalysis.CSharp.UseObjectInitializer;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.Configuration.ConfigureSeverity
{
    public abstract partial class CSharpCodeStyleOptionBasedSeverityConfigurationTests : AbstractSuppressionDiagnosticTest
    {
        protected override TestWorkspace CreateWorkspaceFromFile(string initialMarkup, TestParameters parameters)
            => TestWorkspace.CreateCSharp(initialMarkup, parameters.parseOptions, parameters.compilationOptions);

        protected override string GetLanguage() => LanguageNames.CSharp;

        protected override ParseOptions GetScriptOptions() => Options.Script;

        internal override Tuple<DiagnosticAnalyzer, IConfigurationFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return new Tuple<DiagnosticAnalyzer, IConfigurationFixProvider>(
                        new CSharpRemoveUnusedParametersAndValuesDiagnosticAnalyzer(), new ConfigureSeverityLevelCodeFixProvider());
        }

        public class ErrorConfigurationTests : CSharpCodeStyleOptionBasedSeverityConfigurationTests
        {
            protected override int CodeActionIndex => 4;

            [ConditionalFact(typeof(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_Empty_Error()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\Program.cs"">
public class Class1
{
    public int Test()
    {
        var o = 1;
        // csharp_style_unused_value_assignment_preference = discard_variable
        var [|unused|] = o;
        return 1;
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\Program.cs"">
public class Class1
{
    public int Test()
    {
        var o = 1;
        // csharp_style_unused_value_assignment_preference = discard_variable
        var [|unused|] = o;
        return 1;
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.cs]

# IDE0059: Unnecessary assignment of a value
csharp_style_unused_value_assignment_preference = discard_variable:error
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [ConditionalFact(typeof(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_ExistingRule_Error()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\Program.cs"">
public class Class1
{
    public int Test()
    {
        var o = 1;
        // csharp_style_unused_value_assignment_preference = discard_variable
        var [|unused|] = o;
        return 1;
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.cs]

# IDE0059: Unnecessary assignment of a value
csharp_style_unused_value_assignment_preference = discard_variable:warning
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\Program.cs"">
public class Class1
{
    public int Test()
    {
        var o = 1;
        // csharp_style_unused_value_assignment_preference = discard_variable
        var [|unused|] = o;
        return 1;
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.cs]

# IDE0059: Unnecessary assignment of a value
csharp_style_unused_value_assignment_preference = discard_variable:error
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [ConditionalFact(typeof(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_ExistingRuleDotNetHeader_Error()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\Program.cs"">
public class Class1
{
    public int Test()
    {
        var o = 1;
        // csharp_style_unused_value_assignment_preference = discard_variable
        var [|unused|] = o;
        return 1;
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.{vb,cs}]

# IDE0059: Unnecessary assignment of a value
csharp_style_unused_value_assignment_preference = discard_variable:warning
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\Program.cs"">
public class Class1
{
    public int Test()
    {
        var o = 1;
        // csharp_style_unused_value_assignment_preference = discard_variable
        var [|unused|] = o;
        return 1;
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.{vb,cs}]

# IDE0059: Unnecessary assignment of a value
csharp_style_unused_value_assignment_preference = discard_variable:error
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [ConditionalFact(typeof(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_ChooseBestHeader_Error()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\Program.cs"">
public class Class1
{
    public int Test()
    {
        var o = 1;
        // csharp_style_unused_value_assignment_preference = discard_variable
        var [|unused|] = o;
        return 1;
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.cs]
csharp_style_expression_bodied_methods = false:silent

[*.{vb,cs}]
dotnet_style_qualification_for_field = false:silent
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\Program.cs"">
public class Class1
{
    public int Test()
    {
        var o = 1;
        // csharp_style_unused_value_assignment_preference = discard_variable
        var [|unused|] = o;
        return 1;
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.cs]
csharp_style_expression_bodied_methods = false:silent

# IDE0059: Unnecessary assignment of a value
csharp_style_unused_value_assignment_preference = discard_variable:error

[*.{vb,cs}]
dotnet_style_qualification_for_field = false:silent
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [ConditionalFact(typeof(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_ChooseBestHeaderReversed_Error()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\Program.cs"">
public class Class1
{
    public int Test()
    {
        var o = 1;
        // csharp_style_unused_value_assignment_preference = discard_variable
        var [|unused|] = o;
        return 1;
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.{vb,cs}]
dotnet_style_qualification_for_field = false:silent

[*.cs]
csharp_style_expression_bodied_methods = false:silent
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\Program.cs"">
public class Class1
{
    public int Test()
    {
        var o = 1;
        // csharp_style_unused_value_assignment_preference = discard_variable
        var [|unused|] = o;
        return 1;
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.{vb,cs}]
dotnet_style_qualification_for_field = false:silent

[*.cs]
csharp_style_expression_bodied_methods = false:silent

# IDE0059: Unnecessary assignment of a value
csharp_style_unused_value_assignment_preference = discard_variable:error
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }
        }
    }
}
