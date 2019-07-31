// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.Configuration.ConfigureCodeStyle;
using Microsoft.CodeAnalysis.CSharp.UseObjectInitializer;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.Configuration.ConfigureCodeStyle
{
    public abstract partial class BooleanCodeStyleOptionConfigurationTests : AbstractSuppressionDiagnosticTest
    {
        protected override TestWorkspace CreateWorkspaceFromFile(string initialMarkup, TestParameters parameters)
        => TestWorkspace.CreateCSharp(initialMarkup, parameters.parseOptions, parameters.compilationOptions);

        protected override string GetLanguage() => LanguageNames.CSharp;

        protected override ParseOptions GetScriptOptions() => Options.Script;

        internal override Tuple<DiagnosticAnalyzer, IConfigurationFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return new Tuple<DiagnosticAnalyzer, IConfigurationFixProvider>(
                        new CSharpUseObjectInitializerDiagnosticAnalyzer(), new ConfigureCodeStyleOptionCodeFixProvider(performExperimentCheck: false));
        }

        public class TrueConfigurationTests : BooleanCodeStyleOptionConfigurationTests
        {
            protected override int CodeActionIndex => 0;

            [ConditionalFact(typeof(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_Empty_True()
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
            public async Task ConfigureEditorconfig_RuleExists_True()
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
dotnet_style_object_initializer = false:suggestion    ; Comment2
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
dotnet_style_object_initializer = true:suggestion    ; Comment2
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [ConditionalFact(typeof(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_InvalidHeader_True()
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
dotnet_style_object_initializer = false:suggestion

[*.cs]

# IDE0017: Simplify object initialization
dotnet_style_object_initializer = true:suggestion
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_MaintainSeverity_True()
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
dotnet_style_object_initializer = true:suggestion
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [ConditionalFact(typeof(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_InvalidRule_True()
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
dotnet_style_object_initializerr = false:suggestion
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
dotnet_style_object_initializerr = false:suggestion

# IDE0017: Simplify object initialization
dotnet_style_object_initializer = true:suggestion
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }
        }

        public class FalseConfigurationTests : BooleanCodeStyleOptionConfigurationTests
        {
            protected override int CodeActionIndex => 1;

            [ConditionalFact(typeof(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_Empty_False()
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
dotnet_style_object_initializer = false:suggestion
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_RuleExists_False()
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
dotnet_style_object_initializer = false:suggestion
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [ConditionalFact(typeof(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_InvalidHeader_False()
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
dotnet_style_object_initializer = false:suggestion
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_MaintainSeverity_False()
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
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.{vb,cs}]
dotnet_style_object_initializer = false:suggestion
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [ConditionalFact(typeof(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
            public async Task ConfigureEditorconfig_InvalidRule_False()
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
dotnet_style_object_initializerr = false:suggestion
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
dotnet_style_object_initializerr = false:suggestion

# IDE0017: Simplify object initialization
dotnet_style_object_initializer = false:suggestion
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }
        }
    }
}
