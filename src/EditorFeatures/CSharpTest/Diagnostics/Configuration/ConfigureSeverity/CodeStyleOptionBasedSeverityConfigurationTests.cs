// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.Configuration.ConfigureSeverity;
using Microsoft.CodeAnalysis.CSharp.UseObjectInitializer;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.Configuration.ConfigureSeverity
{
    public abstract partial class CodeStyleOptionBasedSeverityConfigurationTests : AbstractSuppressionDiagnosticTest
    {
        protected override TestWorkspace CreateWorkspaceFromFile(string initialMarkup, TestParameters parameters)
            => TestWorkspace.CreateCSharp(initialMarkup, parameters.parseOptions, parameters.compilationOptions);

        protected override string GetLanguage() => LanguageNames.CSharp;

        protected override ParseOptions GetScriptOptions() => Options.Script;

        internal override Tuple<DiagnosticAnalyzer, IConfigurationFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return new Tuple<DiagnosticAnalyzer, IConfigurationFixProvider>(
                        new CSharpUseObjectInitializerDiagnosticAnalyzer(), new ConfigureSeverityLevelCodeFixProvider());
        }

        public class NoneConfigurationTests : CodeStyleOptionBasedSeverityConfigurationTests
        {
            protected override int CodeActionIndex => 0;

            [ConditionalFact(typeof(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_Empty_None()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.cs"">
class Program1
{
    static void Main()
    {
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = [|new Customer()|];
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

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
class Program1
{
    static void Main()
    {
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = new Customer();
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.cs]

# IDE0017: Simplify object initialization
dotnet_style_object_initializer = true:none
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_RuleExists_None()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.cs"">
class Program1
{
    static void Main()
    {
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = [|new Customer()|];
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.cs]    # Comment1
dotnet_style_object_initializer = true:suggestion    ; Comment2
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
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = new Customer();
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.cs]    # Comment1
dotnet_style_object_initializer = true:none    ; Comment2
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [ConditionalFact(typeof(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_InvalidHeader_None()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.cs"">
class Program1
{
    static void Main()
    {
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = [|new Customer()|];
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.vb]
dotnet_style_object_initializer = true:suggestion
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
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = [|new Customer()|];
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.vb]
dotnet_style_object_initializer = true:suggestion

[*.cs]

# IDE0017: Simplify object initialization
dotnet_style_object_initializer = true:none
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_MaintainOption_None()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.cs"">
class Program1
{
    static void Main()
    {
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = [|new Customer()|];
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.{vb,cs}]
dotnet_style_object_initializer = false:suggestion
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
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = new Customer();
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.{vb,cs}]
dotnet_style_object_initializer = false:none
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [ConditionalFact(typeof(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_InvalidRule_None()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.cs"">
class Program1
{
    static void Main()
    {
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = [|new Customer()|];
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.cs]
dotnet_style_object_initializerr = true:suggestion
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
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = [|new Customer()|];
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.cs]
dotnet_style_object_initializerr = true:suggestion

# IDE0017: Simplify object initialization
dotnet_style_object_initializer = true:none
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }
        }

        public class SilentConfigurationTests : CodeStyleOptionBasedSeverityConfigurationTests
        {
            protected override int CodeActionIndex => 1;

            [ConditionalFact(typeof(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_Empty_Silent()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.cs"">
class Program1
{
    static void Main()
    {
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = [|new Customer()|];
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

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
class Program1
{
    static void Main()
    {
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = new Customer();
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.cs]

# IDE0017: Simplify object initialization
dotnet_style_object_initializer = true:silent
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_RuleExists_Silent()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.cs"">
class Program1
{
    static void Main()
    {
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = [|new Customer()|];
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.cs]
dotnet_style_object_initializer = true:suggestion
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
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = new Customer();
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.cs]
dotnet_style_object_initializer = true:silent
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [ConditionalFact(typeof(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_InvalidHeader_Silent()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.cs"">
class Program1
{
    static void Main()
    {
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = [|new Customer()|];
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.vb]
dotnet_style_object_initializer = true:suggestion
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
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = [|new Customer()|];
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.vb]
dotnet_style_object_initializer = true:suggestion

[*.cs]

# IDE0017: Simplify object initialization
dotnet_style_object_initializer = true:silent
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_MaintainOption_Silent()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.cs"">
class Program1
{
    static void Main()
    {
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = [|new Customer()|];
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.{vb,cs}]
dotnet_style_object_initializer = false:suggestion
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
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = new Customer();
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.{vb,cs}]
dotnet_style_object_initializer = false:silent
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [ConditionalFact(typeof(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_InvalidRule_Silent()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.cs"">
class Program1
{
    static void Main()
    {
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = [|new Customer()|];
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.cs]
dotnet_style_object_initializerr = true:suggestion
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
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = [|new Customer()|];
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.cs]
dotnet_style_object_initializerr = true:suggestion

# IDE0017: Simplify object initialization
dotnet_style_object_initializer = true:silent
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }
        }

        public class SuggestionConfigurationTests : CodeStyleOptionBasedSeverityConfigurationTests
        {
            protected override int CodeActionIndex => 2;

            [ConditionalFact(typeof(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_Empty_Suggestion()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.cs"">
class Program1
{
    static void Main()
    {
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = [|new Customer()|];
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

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
class Program1
{
    static void Main()
    {
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = new Customer();
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.cs]

# IDE0017: Simplify object initialization
dotnet_style_object_initializer = true:suggestion
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_RuleExists_Suggestion()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.cs"">
class Program1
{
    static void Main()
    {
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = [|new Customer()|];
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.cs]
dotnet_style_object_initializer = true:warning
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
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = new Customer();
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.cs]
dotnet_style_object_initializer = true:suggestion
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [ConditionalFact(typeof(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_InvalidHeader_Suggestion()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.cs"">
class Program1
{
    static void Main()
    {
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = [|new Customer()|];
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.vb]
dotnet_style_object_initializer = true:suggestion
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
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = [|new Customer()|];
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.vb]
dotnet_style_object_initializer = true:suggestion

[*.cs]

# IDE0017: Simplify object initialization
dotnet_style_object_initializer = true:suggestion
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_MaintainOption_Suggestion()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.cs"">
class Program1
{
    static void Main()
    {
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = [|new Customer()|];
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.{vb,cs}]
dotnet_style_object_initializer = false:error
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
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = new Customer();
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.{vb,cs}]
dotnet_style_object_initializer = false:suggestion
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [ConditionalFact(typeof(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_InvalidRule_Suggestion()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.cs"">
class Program1
{
    static void Main()
    {
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = [|new Customer()|];
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.cs]
dotnet_style_object_initializerr = true:warning
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
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = [|new Customer()|];
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.cs]
dotnet_style_object_initializerr = true:warning

# IDE0017: Simplify object initialization
dotnet_style_object_initializer = true:suggestion
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }
        }

        public class WarningConfigurationTests : CodeStyleOptionBasedSeverityConfigurationTests
        {
            protected override int CodeActionIndex => 3;

            [ConditionalFact(typeof(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_Empty_Warning()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.cs"">
class Program1
{
    static void Main()
    {
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = [|new Customer()|];
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

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
class Program1
{
    static void Main()
    {
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = new Customer();
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.cs]

# IDE0017: Simplify object initialization
dotnet_style_object_initializer = true:warning
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_RuleExists_Warning()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.cs"">
class Program1
{
    static void Main()
    {
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = [|new Customer()|];
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.cs]
dotnet_style_object_initializer = true:suggestion
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
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = new Customer();
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.cs]
dotnet_style_object_initializer = true:warning
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [ConditionalFact(typeof(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_InvalidHeader_Warning()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.cs"">
class Program1
{
    static void Main()
    {
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = [|new Customer()|];
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.vb]
dotnet_style_object_initializer = true:suggestion
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
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = [|new Customer()|];
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.vb]
dotnet_style_object_initializer = true:suggestion

[*.cs]

# IDE0017: Simplify object initialization
dotnet_style_object_initializer = true:warning
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_MaintainOption_Warning()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.cs"">
class Program1
{
    static void Main()
    {
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = [|new Customer()|];
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.{vb,cs}]
dotnet_style_object_initializer = false:error
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
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = new Customer();
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.{vb,cs}]
dotnet_style_object_initializer = false:warning
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [ConditionalFact(typeof(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_InvalidRule_Warning()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.cs"">
class Program1
{
    static void Main()
    {
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = [|new Customer()|];
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.cs]
dotnet_style_object_initializerr = true:warning
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
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = [|new Customer()|];
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.cs]
dotnet_style_object_initializerr = true:warning

# IDE0017: Simplify object initialization
dotnet_style_object_initializer = true:warning
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }
        }

        public class ErrorConfigurationTests : CodeStyleOptionBasedSeverityConfigurationTests
        {
            protected override int CodeActionIndex => 4;

            [ConditionalFact(typeof(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_Empty_Error()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.cs"">
class Program1
{
    static void Main()
    {
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = [|new Customer()|];
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

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
class Program1
{
    static void Main()
    {
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = new Customer();
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.cs]

# IDE0017: Simplify object initialization
dotnet_style_object_initializer = true:error
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_RuleExists_Error()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.cs"">
class Program1
{
    static void Main()
    {
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = [|new Customer()|];
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.cs]
dotnet_style_object_initializer = true:suggestion
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
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = new Customer();
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.cs]
dotnet_style_object_initializer = true:error
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [ConditionalFact(typeof(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_InvalidHeader_Error()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.cs"">
class Program1
{
    static void Main()
    {
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = [|new Customer()|];
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.vb]
dotnet_style_object_initializer = true:suggestion
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
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = [|new Customer()|];
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.vb]
dotnet_style_object_initializer = true:suggestion

[*.cs]

# IDE0017: Simplify object initialization
dotnet_style_object_initializer = true:error
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_MaintainOption_Error()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.cs"">
class Program1
{
    static void Main()
    {
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = [|new Customer()|];
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.{vb,cs}]
dotnet_style_object_initializer = false:suggestion
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
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = new Customer();
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.{vb,cs}]
dotnet_style_object_initializer = false:error
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [ConditionalFact(typeof(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_InvalidRule_Error()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.cs"">
class Program1
{
    static void Main()
    {
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = [|new Customer()|];
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.cs]
dotnet_style_object_initializerr = true:warning
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
        // dotnet_style_object_initializer = true
        var obj = new Customer() { _age = 21 };

        // dotnet_style_object_initializer = false
        Customer obj2 = [|new Customer()|];
        obj2._age = 21;
    }

    internal class Customer
    {
        public int _age;

        public Customer()
        {

        }
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.cs]
dotnet_style_object_initializerr = true:warning

# IDE0017: Simplify object initialization
dotnet_style_object_initializer = true:error
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }
        }
    }
}
