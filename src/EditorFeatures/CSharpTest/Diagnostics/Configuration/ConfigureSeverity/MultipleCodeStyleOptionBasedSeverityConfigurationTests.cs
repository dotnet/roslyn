// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.Configuration.ConfigureSeverity;
using Microsoft.CodeAnalysis.CSharp.UseInferredMemberName;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.Configuration.ConfigureSeverity
{
    public abstract partial class MultipleCodeStyleOptionBasedSeverityConfigurationTests : AbstractSuppressionDiagnosticTest
    {
        protected override TestWorkspace CreateWorkspaceFromFile(string initialMarkup, TestParameters parameters)
            => TestWorkspace.CreateCSharp(initialMarkup, parameters.parseOptions, parameters.compilationOptions);

        protected override string GetLanguage() => LanguageNames.CSharp;

        protected override ParseOptions GetScriptOptions() => Options.Script;

        internal override Tuple<DiagnosticAnalyzer, IConfigurationFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return new Tuple<DiagnosticAnalyzer, IConfigurationFixProvider>(
                        new CSharpUseInferredMemberNameDiagnosticAnalyzer(), new ConfigureSeverityLevelCodeFixProvider());
        }

        public class ErrorConfigurationTests : MultipleCodeStyleOptionBasedSeverityConfigurationTests
        {
            protected override int CodeActionIndex => 4;

            [WorkItem(39664, "https://github.com/dotnet/roslyn/issues/39664")]
            [ConditionalFact(typeof(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_Empty_Error()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.cs"">
using System;

namespace ConsoleApp5
{
    class Foo 
    {
        public string RuleName = ""test"";
    }

    class Bar
    {
        static void Main(string[] args)
        {
            var foo = new Foo();

            var bar = new { [|RuleName|] = foo.RuleName };

            Console.WriteLine(bar.RuleName);
        }
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
using System;

namespace ConsoleApp5
{
    class Foo 
    {
        public string RuleName = ""test"";
    }

    class Bar
    {
        static void Main(string[] args)
        {
            var foo = new Foo();

            var bar = new { RuleName = foo.RuleName };

            Console.WriteLine(bar.RuleName);
        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.{cs,vb}]

# IDE0037: Use inferred member name
dotnet_style_prefer_inferred_anonymous_type_member_names = true:error

# IDE0037: Use inferred member name
dotnet_style_prefer_inferred_tuple_names = true:error
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [WorkItem(39664, "https://github.com/dotnet/roslyn/issues/39664")]
            [ConditionalFact(typeof(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_BothRulesExist_Error()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.cs"">
using System;

namespace ConsoleApp5
{
    class Foo 
    {
        public string RuleName = ""test"";
    }

    class Bar
    {
        static void Main(string[] args)
        {
            var foo = new Foo();

            var bar = new { [|RuleName|] = foo.RuleName };

            Console.WriteLine(bar.RuleName);
        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.{cs,vb}]

# IDE0037: Use inferred member name
dotnet_style_prefer_inferred_anonymous_type_member_names = true:warning

# IDE0037: Use inferred member name
dotnet_style_prefer_inferred_tuple_names = true:suggestion
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document FilePath=""z:\\file.cs"">
using System;

namespace ConsoleApp5
{
    class Foo 
    {
        public string RuleName = ""test"";
    }

    class Bar
    {
        static void Main(string[] args)
        {
            var foo = new Foo();

            var bar = new { RuleName = foo.RuleName };

            Console.WriteLine(bar.RuleName);
        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.{cs,vb}]

# IDE0037: Use inferred member name
dotnet_style_prefer_inferred_anonymous_type_member_names = true:error

# IDE0037: Use inferred member name
dotnet_style_prefer_inferred_tuple_names = true:error
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [WorkItem(39664, "https://github.com/dotnet/roslyn/issues/39664")]
            [ConditionalFact(typeof(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_OneRuleExists_Error()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.cs"">
using System;

namespace ConsoleApp5
{
    class Foo 
    {
        public string RuleName = ""test"";
    }

    class Bar
    {
        static void Main(string[] args)
        {
            var foo = new Foo();

            var bar = new { [|RuleName|] = foo.RuleName };

            Console.WriteLine(bar.RuleName);
        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.{cs,vb}]

# IDE0037: Use inferred member name
dotnet_style_prefer_inferred_anonymous_type_member_names = true:warning
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document FilePath=""z:\\file.cs"">
using System;

namespace ConsoleApp5
{
    class Foo 
    {
        public string RuleName = ""test"";
    }

    class Bar
    {
        static void Main(string[] args)
        {
            var foo = new Foo();

            var bar = new { RuleName = foo.RuleName };

            Console.WriteLine(bar.RuleName);
        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.{cs,vb}]

# IDE0037: Use inferred member name
dotnet_style_prefer_inferred_anonymous_type_member_names = true:error

# IDE0037: Use inferred member name
dotnet_style_prefer_inferred_tuple_names = true:error
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }
        }
    }
}
