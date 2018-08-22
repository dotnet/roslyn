using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes.Suppression;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.Suppression;
using Microsoft.CodeAnalysis.CSharp.UseObjectInitializer;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.ConfigureSeverityLevel
{
    public abstract partial class ConfigurationTestsBase : AbstractSuppressionDiagnosticTest
    {
        protected override TestWorkspace CreateWorkspaceFromFile(string initialMarkup, TestParameters parameters)
            => TestWorkspace.CreateCSharp(initialMarkup, parameters.parseOptions, parameters.compilationOptions);

        protected override ImmutableArray<CodeAction> MassageActions(ImmutableArray<CodeAction> actions)
        {
            return actions[0].NestedCodeActions;
        }

        protected override string GetLanguage() => LanguageNames.CSharp;

        protected override ParseOptions GetScriptOptions() => Options.Script;

        internal override Tuple<DiagnosticAnalyzer, ISuppressionOrConfigurationFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return new Tuple<DiagnosticAnalyzer, ISuppressionOrConfigurationFixProvider>(
                        new CSharpUseObjectInitializerDiagnosticAnalyzer(), new CSharpConfigureSeverityLevel());
        }

        public class NoneConfigurationTests : ConfigurationTestsBase
        {
            protected override int CodeActionIndex => 0;

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            public async Task ConfigureEditorconfig_Empty_None()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig""></AdditionalDocument>
    </Project>
</Workspace>";

                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.cs]
dotnet_style_object_initializer = true:none
</AdditionalDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            public async Task ConfigureEditorconfig_RuleExists_None()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.cs]
dotnet_style_object_initializer = true:suggestion
</AdditionalDocument>
    </Project>
</Workspace>";

                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.cs]
dotnet_style_object_initializer = true:none
</AdditionalDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            public async Task ConfigureEditorconfig_InvalidHeader_None()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.vb]
dotnet_style_object_initializer = true:suggestion
</AdditionalDocument>
    </Project>
</Workspace>";

                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.vb]
dotnet_style_object_initializer = true:suggestion

[*.cs]
dotnet_style_object_initializer = true:none
</AdditionalDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            public async Task ConfigureEditorconfig_MaintainOption_None()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.{vb,cs}]
dotnet_style_object_initializer = false:suggestion
</AdditionalDocument>
    </Project>
</Workspace>";

                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.{vb,cs}]
dotnet_style_object_initializer = false:none
</AdditionalDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            public async Task ConfigureEditorconfig_InvalidRule_None()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.cs]
dotnet_style_object_initializerr = true:suggestion
</AdditionalDocument>
    </Project>
</Workspace>";

                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.cs]
dotnet_style_object_initializerr = true:suggestion
dotnet_style_object_initializer = true:none
</AdditionalDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }
        }

        public class SilentConfigurationTests : ConfigurationTestsBase
        {
            protected override int CodeActionIndex => 1;

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            public async Task ConfigureEditorconfig_Empty_Silent()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig""></AdditionalDocument>
    </Project>
</Workspace>";

                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.cs]
dotnet_style_object_initializer = true:silent
</AdditionalDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            public async Task ConfigureEditorconfig_RuleExists_Silent()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.cs]
dotnet_style_object_initializer = true:suggestion
</AdditionalDocument>
    </Project>
</Workspace>";

                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.cs]
dotnet_style_object_initializer = true:silent
</AdditionalDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            public async Task ConfigureEditorconfig_InvalidHeader_Silent()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.vb]
dotnet_style_object_initializer = true:suggestion
</AdditionalDocument>
    </Project>
</Workspace>";

                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.vb]
dotnet_style_object_initializer = true:suggestion

[*.cs]
dotnet_style_object_initializer = true:silent
</AdditionalDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            public async Task ConfigureEditorconfig_MaintainOption_Silent()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.{vb,cs}]
dotnet_style_object_initializer = false:suggestion
</AdditionalDocument>
    </Project>
</Workspace>";

                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.{vb,cs}]
dotnet_style_object_initializer = false:silent
</AdditionalDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            public async Task ConfigureEditorconfig_InvalidRule_Silent()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.cs]
dotnet_style_object_initializerr = true:suggestion
</AdditionalDocument>
    </Project>
</Workspace>";

                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.cs]
dotnet_style_object_initializerr = true:suggestion
dotnet_style_object_initializer = true:silent
</AdditionalDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }
        }

        public class SuggestionConfigurationTests : ConfigurationTestsBase
        {
            protected override int CodeActionIndex => 2;

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            public async Task ConfigureEditorconfig_Empty_Suggestion()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig""></AdditionalDocument>
    </Project>
</Workspace>";

                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.cs]
dotnet_style_object_initializer = true:suggestion
</AdditionalDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            public async Task ConfigureEditorconfig_RuleExists_Suggestion()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.cs]
dotnet_style_object_initializer = true:warning
</AdditionalDocument>
    </Project>
</Workspace>";

                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.cs]
dotnet_style_object_initializer = true:suggestion
</AdditionalDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            public async Task ConfigureEditorconfig_InvalidHeader_Suggestion()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.vb]
dotnet_style_object_initializer = true:suggestion
</AdditionalDocument>
    </Project>
</Workspace>";

                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.vb]
dotnet_style_object_initializer = true:suggestion

[*.cs]
dotnet_style_object_initializer = true:suggestion
</AdditionalDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            public async Task ConfigureEditorconfig_MaintainOption_Suggestion()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.{vb,cs}]
dotnet_style_object_initializer = false:error
</AdditionalDocument>
    </Project>
</Workspace>";

                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.{vb,cs}]
dotnet_style_object_initializer = false:suggestion
</AdditionalDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            public async Task ConfigureEditorconfig_InvalidRule_Suggestion()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.cs]
dotnet_style_object_initializerr = true:warning
</AdditionalDocument>
    </Project>
</Workspace>";

                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.cs]
dotnet_style_object_initializerr = true:warning
dotnet_style_object_initializer = true:suggestion
</AdditionalDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }
        }

        public class WarningConfigurationTests : ConfigurationTestsBase
        {
            protected override int CodeActionIndex => 3;

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            public async Task ConfigureEditorconfig_Empty_Warning()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig""></AdditionalDocument>
    </Project>
</Workspace>";

                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.cs]
dotnet_style_object_initializer = true:warning
</AdditionalDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            public async Task ConfigureEditorconfig_RuleExists_Warning()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.cs]
dotnet_style_object_initializer = true:suggestion
</AdditionalDocument>
    </Project>
</Workspace>";

                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.cs]
dotnet_style_object_initializer = true:warning
</AdditionalDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            public async Task ConfigureEditorconfig_InvalidHeader_Warning()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.vb]
dotnet_style_object_initializer = true:suggestion
</AdditionalDocument>
    </Project>
</Workspace>";

                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.vb]
dotnet_style_object_initializer = true:suggestion

[*.cs]
dotnet_style_object_initializer = true:warning
</AdditionalDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            public async Task ConfigureEditorconfig_MaintainOption_Warning()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.{vb,cs}]
dotnet_style_object_initializer = false:error
</AdditionalDocument>
    </Project>
</Workspace>";

                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.{vb,cs}]
dotnet_style_object_initializer = false:warning
</AdditionalDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            public async Task ConfigureEditorconfig_InvalidRule_Warning()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.cs]
dotnet_style_object_initializerr = true:warning
</AdditionalDocument>
    </Project>
</Workspace>";

                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.cs]
dotnet_style_object_initializerr = true:warning
dotnet_style_object_initializer = true:warning
</AdditionalDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }
        }

        public class ErrorConfigurationTests : ConfigurationTestsBase
        {
            protected override int CodeActionIndex => 4;

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            public async Task ConfigureEditorconfig_Empty_Error()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig""></AdditionalDocument>
    </Project>
</Workspace>";

                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.cs]
dotnet_style_object_initializer = true:error
</AdditionalDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            public async Task ConfigureEditorconfig_RuleExists_Error()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.cs]
dotnet_style_object_initializer = true:suggestion
</AdditionalDocument>
    </Project>
</Workspace>";

                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.cs]
dotnet_style_object_initializer = true:error
</AdditionalDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            public async Task ConfigureEditorconfig_InvalidHeader_Error()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.vb]
dotnet_style_object_initializer = true:suggestion
</AdditionalDocument>
    </Project>
</Workspace>";

                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.vb]
dotnet_style_object_initializer = true:suggestion

[*.cs]
dotnet_style_object_initializer = true:error
</AdditionalDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            public async Task ConfigureEditorconfig_MaintainOption_Error()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.{vb,cs}]
dotnet_style_object_initializer = false:suggestion
</AdditionalDocument>
    </Project>
</Workspace>";

                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.{vb,cs}]
dotnet_style_object_initializer = false:error
</AdditionalDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            public async Task ConfigureEditorconfig_InvalidRule_Error()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.cs]
dotnet_style_object_initializerr = true:warning
</AdditionalDocument>
    </Project>
</Workspace>";

                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.cs]
dotnet_style_object_initializerr = true:warning
dotnet_style_object_initializer = true:error
</AdditionalDocument>
    </Project>
</Workspace>";

                await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
            }
        }
    }
}
