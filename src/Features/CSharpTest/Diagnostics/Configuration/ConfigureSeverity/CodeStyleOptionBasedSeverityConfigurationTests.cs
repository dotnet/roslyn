// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.Configuration.ConfigureSeverity;
using Microsoft.CodeAnalysis.CSharp.UseObjectInitializer;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.Configuration.ConfigureSeverity;

public abstract partial class CodeStyleOptionBasedSeverityConfigurationTests : AbstractSuppressionDiagnosticTest_NoEditor
{
    protected internal override string GetLanguage() => LanguageNames.CSharp;

    protected override ParseOptions GetScriptOptions() => Options.Script;

    internal override Tuple<DiagnosticAnalyzer, IConfigurationFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
    {
        return new Tuple<DiagnosticAnalyzer, IConfigurationFixProvider>(
                    new CSharpUseObjectInitializerDiagnosticAnalyzer(), new ConfigureSeverityLevelCodeFixProvider());
    }

    [Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
    public sealed class NoneConfigurationTests : CodeStyleOptionBasedSeverityConfigurationTests
    {
        protected override int CodeActionIndex => 0;

        [ConditionalFact(typeof(IsEnglishLocal))]
        public Task ConfigureEditorconfig_Empty_None()
            => TestInRegularAndScriptAsync("""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.{cs,vb}]

                # IDE0017: Simplify object initialization
                dotnet_diagnostic.IDE0017.severity = none
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, CodeActionIndex);

        [Fact]
        public Task ConfigureEditorconfig_RuleExists_None()
            => TestInRegularAndScriptAsync("""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]    # Comment1
                dotnet_style_object_initializer = true:suggestion    ; Comment2
                dotnet_diagnostic.IDE0017.severity = suggestion    ;; Comment3
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]    # Comment1
                dotnet_style_object_initializer = true:none    ; Comment2
                dotnet_diagnostic.IDE0017.severity = none    ;; Comment3
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, CodeActionIndex);

        [ConditionalFact(typeof(IsEnglishLocal))]
        public Task ConfigureEditorconfig_InvalidHeader_None()
            => TestInRegularAndScriptAsync("""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.vb]
                dotnet_diagnostic.IDE0017.severity = suggestion
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.vb]
                dotnet_diagnostic.IDE0017.severity = suggestion

                [*.{cs,vb}]

                # IDE0017: Simplify object initialization
                dotnet_diagnostic.IDE0017.severity = none
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, CodeActionIndex);

        [Fact]
        public Task ConfigureEditorconfig_MaintainOption_None()
            => TestInRegularAndScriptAsync("""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.{vb,cs}]
                dotnet_style_object_initializer = true:suggestion
                dotnet_diagnostic.IDE0017.severity = suggestion
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.{vb,cs}]
                dotnet_style_object_initializer = true:none
                dotnet_diagnostic.IDE0017.severity = none
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, CodeActionIndex);

        [ConditionalFact(typeof(IsEnglishLocal))]
        public Task ConfigureEditorconfig_InvalidRule_None()
            => TestInRegularAndScriptAsync("""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                dotnet_style_object_initializerr = true:suggestion
                dotnet_diagnostic.IDE0017.severityyy = suggestion
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                dotnet_style_object_initializerr = true:suggestion
                dotnet_diagnostic.IDE0017.severityyy = suggestion

                # IDE0017: Simplify object initialization
                dotnet_diagnostic.IDE0017.severity = none
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, CodeActionIndex);
    }

    [Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
    public sealed class SilentConfigurationTests : CodeStyleOptionBasedSeverityConfigurationTests
    {
        protected override int CodeActionIndex => 1;

        [ConditionalFact(typeof(IsEnglishLocal))]
        public Task ConfigureEditorconfig_Empty_Silent()
            => TestInRegularAndScriptAsync("""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.{cs,vb}]

                # IDE0017: Simplify object initialization
                dotnet_diagnostic.IDE0017.severity = silent
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, CodeActionIndex);

        [Fact]
        public Task ConfigureEditorconfig_RuleExists_Silent()
            => TestInRegularAndScriptAsync("""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                dotnet_diagnostic.IDE0017.severity = suggestion
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                dotnet_diagnostic.IDE0017.severity = silent
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, CodeActionIndex);
    }

    [Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
    public sealed class SuggestionConfigurationTests : CodeStyleOptionBasedSeverityConfigurationTests
    {
        protected override int CodeActionIndex => 2;

        [ConditionalFact(typeof(IsEnglishLocal))]
        public Task ConfigureEditorconfig_Empty_Suggestion()
            => TestInRegularAndScriptAsync("""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.{cs,vb}]

                # IDE0017: Simplify object initialization
                dotnet_diagnostic.IDE0017.severity = suggestion
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, CodeActionIndex);

        [Fact]
        public Task ConfigureEditorconfig_RuleExists_Suggestion()
            => TestInRegularAndScriptAsync("""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                dotnet_style_object_initializer = true:warning
                dotnet_diagnostic.IDE0017.severity = warning
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                dotnet_style_object_initializer = true:suggestion
                dotnet_diagnostic.IDE0017.severity = suggestion
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, CodeActionIndex);
    }

    [Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
    public sealed class WarningConfigurationTests : CodeStyleOptionBasedSeverityConfigurationTests
    {
        protected override int CodeActionIndex => 3;

        [ConditionalFact(typeof(IsEnglishLocal))]
        public Task ConfigureEditorconfig_Empty_Warning()
            => TestInRegularAndScriptAsync("""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.{cs,vb}]

                # IDE0017: Simplify object initialization
                dotnet_diagnostic.IDE0017.severity = warning
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, CodeActionIndex);

        [Fact]
        public Task ConfigureEditorconfig_RuleExists_Warning()
            => TestInRegularAndScriptAsync("""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                dotnet_diagnostic.IDE0017.severity = suggestion
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                dotnet_diagnostic.IDE0017.severity = warning
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, CodeActionIndex);
    }

    [Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
    public sealed class ErrorConfigurationTests : CodeStyleOptionBasedSeverityConfigurationTests
    {
        protected override int CodeActionIndex => 4;

        [ConditionalFact(typeof(IsEnglishLocal))]
        public Task ConfigureEditorconfig_Empty_Error()
            => TestInRegularAndScriptAsync("""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.{cs,vb}]

                # IDE0017: Simplify object initialization
                dotnet_diagnostic.IDE0017.severity = error
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, CodeActionIndex);

        [ConditionalFact(typeof(IsEnglishLocal))]
        public Task ConfigureEditorconfig_RuleExists_CodeStyleBased_Error()
            => TestInRegularAndScriptAsync("""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                dotnet_style_object_initializer = true:suggestion
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                dotnet_style_object_initializer = true:error

                # IDE0017: Simplify object initialization
                dotnet_diagnostic.IDE0017.severity = error
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, CodeActionIndex);

        [Fact]
        public Task ConfigureEditorconfig_RuleExists_SeverityBased_Error()
            => TestInRegularAndScriptAsync("""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                dotnet_diagnostic.IDE0017.severity = suggestion
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                dotnet_diagnostic.IDE0017.severity = error
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, CodeActionIndex);

        [Fact]
        public Task ConfigureEditorconfig_RuleExists_CodeStyleAndSeverityBased_Error()
            => TestInRegularAndScriptAsync("""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                dotnet_style_object_initializer = true:suggestion
                dotnet_diagnostic.IDE0017.severity = suggestion
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                dotnet_style_object_initializer = true:error
                dotnet_diagnostic.IDE0017.severity = error
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, CodeActionIndex);

        [ConditionalFact(typeof(IsEnglishLocal))]
        public Task ConfigureEditorconfig_InvalidHeader_Error()
            => TestInRegularAndScriptAsync("""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.vb]
                dotnet_diagnostic.IDE0017.severity = suggestion
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.vb]
                dotnet_diagnostic.IDE0017.severity = suggestion

                [*.{cs,vb}]

                # IDE0017: Simplify object initialization
                dotnet_diagnostic.IDE0017.severity = error
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, CodeActionIndex);

        [ConditionalFact(typeof(IsEnglishLocal))]
        public Task ConfigureEditorconfig_InvalidRule_Error()
            => TestInRegularAndScriptAsync("""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                dotnet_style_object_initializerr = true:warning
                dotnet_diagnostic.IDE0017.severityyy = suggestion
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                dotnet_style_object_initializerr = true:warning
                dotnet_diagnostic.IDE0017.severityyy = suggestion

                # IDE0017: Simplify object initialization
                dotnet_diagnostic.IDE0017.severity = error
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, CodeActionIndex);

        [ConditionalFact(typeof(IsEnglishLocal))]
        public Task ConfigureEditorconfig_ConcreteHeader_Error()
            => TestInRegularAndScriptAsync("""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\File.cs">
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[File.cs]
                dotnet_style_object_initializer = true:warning
                dotnet_diagnostic.IDE0017.severity = warning
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\File.cs">
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[File.cs]
                dotnet_style_object_initializer = true:error
                dotnet_diagnostic.IDE0017.severity = error
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, CodeActionIndex);

        [ConditionalFact(typeof(IsEnglishLocal))]
        public Task ConfigureEditorconfig_NestedDirectory_Error()
            => TestInRegularAndScriptAsync("""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\ParentFolder/File.cs">
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[File.cs]
                dotnet_style_object_initializer = true:warning
                dotnet_diagnostic.IDE0017.severity = warning
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\ParentFolder/File.cs">
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[File.cs]
                dotnet_style_object_initializer = true:error
                dotnet_diagnostic.IDE0017.severity = error
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, CodeActionIndex);

        [ConditionalFact(typeof(IsEnglishLocal))]
        public Task ConfigureEditorconfig_NestedDirectoryNestedHeader_Error()
            => TestInRegularAndScriptAsync("""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\ParentFolder/File.cs">
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[ParentFolder/File.cs]
                dotnet_style_object_initializer = true:warning
                dotnet_diagnostic.IDE0017.severity = suggestion
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\ParentFolder/File.cs">
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[ParentFolder/File.cs]
                dotnet_style_object_initializer = true:error
                dotnet_diagnostic.IDE0017.severity = error
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, CodeActionIndex);

        [ConditionalFact(typeof(IsEnglishLocal))]
        public Task ConfigureEditorconfig_NestedDirectoryIncorrectHeader_Error()
            => TestInRegularAndScriptAsync("""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\ParentFolder/File.cs">
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[ParentFolderr/File.cs]
                dotnet_style_object_initializer = true:error
                dotnet_diagnostic.IDE0017.severity = warning
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\ParentFolder/File.cs">
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[ParentFolderr/File.cs]
                dotnet_style_object_initializer = true:error
                dotnet_diagnostic.IDE0017.severity = warning

                [*.{cs,vb}]

                # IDE0017: Simplify object initialization
                dotnet_diagnostic.IDE0017.severity = error
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, CodeActionIndex);

        [ConditionalFact(typeof(IsEnglishLocal))]
        public Task ConfigureEditorconfig_IncorrectExtension_Error()
            => TestInRegularAndScriptAsync("""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\ParentFolder/File.cs">
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[File.vb]
                dotnet_style_object_initializer = true:warning
                dotnet_diagnostic.IDE0017.severity = warning
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\ParentFolder/File.cs">
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[File.vb]
                dotnet_style_object_initializer = true:warning
                dotnet_diagnostic.IDE0017.severity = warning

                [*.{cs,vb}]

                # IDE0017: Simplify object initialization
                dotnet_diagnostic.IDE0017.severity = error
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, CodeActionIndex);

        [ConditionalFact(typeof(IsEnglishLocal))]
        public Task ConfigureEditorconfig_HeaderRegex_Error()
            => TestInRegularAndScriptAsync("""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\ParentFolder/File.cs">
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[Parent*r/Fil*.cs]
                dotnet_style_object_initializer = true:warning
                dotnet_diagnostic.IDE0017.severity = warning
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\ParentFolder/File.cs">
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[Parent*r/Fil*.cs]
                dotnet_style_object_initializer = true:error
                dotnet_diagnostic.IDE0017.severity = error
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, CodeActionIndex);

        [ConditionalFact(typeof(IsEnglishLocal))]
        public Task ConfigureEditorconfig_HeaderAllFiles_Error()
            => TestInRegularAndScriptAsync("""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\ParentFolder/File.cs">
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*]
                dotnet_style_object_initializer = true:warning
                dotnet_diagnostic.IDE0017.severity = suggestion
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\ParentFolder/File.cs">
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*]
                dotnet_style_object_initializer = true:error
                dotnet_diagnostic.IDE0017.severity = error
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, CodeActionIndex);

        [ConditionalFact(typeof(IsEnglishLocal))]
        public Task ConfigureEditorconfig_MultipleHeaders_Error()
            => TestInRegularAndScriptAsync("""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\ParentFolder/File.cs">
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[File.vb]
                dotnet_diagnostic.IDE0017.severity = warning

                [File.cs]
                dotnet_style_object_initializer = true:warning
                dotnet_diagnostic.IDE0017.severity = warning

                [cs.vb]
                dotnet_diagnostic.IDE0017.severity = warning

                [test.test]
                dotnet_diagnostic.IDE0017.severity = warning

                [WrongName.cs]
                dotnet_diagnostic.IDE0017.severity = warning

                [WrongName2.cs]
                dotnet_diagnostic.IDE0017.severity = warning
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\ParentFolder/File.cs">
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[File.vb]
                dotnet_diagnostic.IDE0017.severity = warning

                [File.cs]
                dotnet_style_object_initializer = true:error
                dotnet_diagnostic.IDE0017.severity = error

                [cs.vb]
                dotnet_diagnostic.IDE0017.severity = warning

                [test.test]
                dotnet_diagnostic.IDE0017.severity = warning

                [WrongName.cs]
                dotnet_diagnostic.IDE0017.severity = warning

                [WrongName2.cs]
                dotnet_diagnostic.IDE0017.severity = warning
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, CodeActionIndex);

        [ConditionalFact(typeof(IsEnglishLocal))]
        public Task ConfigureEditorconfig_RegexPartialMatch_Error()
            => TestInRegularAndScriptAsync("""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\ParentFolder/Program.cs">
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[gram.cs]
                dotnet_diagnostic.IDE0017.severity = warning
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\ParentFolder/Program.cs">
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[gram.cs]
                dotnet_diagnostic.IDE0017.severity = warning

                [*.{cs,vb}]

                # IDE0017: Simplify object initialization
                dotnet_diagnostic.IDE0017.severity = error
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, CodeActionIndex);

        [ConditionalFact(typeof(IsEnglishLocal))]
        public Task ConfigureEditorconfig_VerifyCaseInsensitive_Warning()
            => TestInRegularAndScriptAsync("""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\PARENTfoldeR/ProGRAM.cs">
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[PROgram.cs]
                dotnet_diagnostic.IDE0017.severity = warning
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\PARENTfoldeR/ProGRAM.cs">
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[PROgram.cs]
                dotnet_diagnostic.IDE0017.severity = error
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, CodeActionIndex);

        [ConditionalFact(typeof(IsEnglishLocal))]
        public Task ConfigureEditorconfig_DuplicateRule_Error()
            => TestInRegularAndScriptAsync("""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\Program.cs">
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                dotnet_style_object_initializer = true:warning
                dotnet_diagnostic.IDE0017.severity = warning

                [Program.cs]
                dotnet_style_object_initializer = true:warning
                dotnet_diagnostic.IDE0017.severity = warning
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\Program.cs">
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                dotnet_style_object_initializer = true:warning
                dotnet_diagnostic.IDE0017.severity = warning

                [Program.cs]
                dotnet_style_object_initializer = true:error
                dotnet_diagnostic.IDE0017.severity = error
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, CodeActionIndex);

        [ConditionalFact(typeof(IsEnglishLocal))]
        public Task ConfigureEditorconfig_ChooseBestHeader_Error()
            => TestInRegularAndScriptAsync("""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\Program.cs">
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.{cs,vb}]
                dotnet_style_qualification_for_field = false:silent

                [*.cs]
                csharp_style_expression_bodied_methods = false:silent
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\Program.cs">
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.{cs,vb}]
                dotnet_style_qualification_for_field = false:silent

                # IDE0017: Simplify object initialization
                dotnet_diagnostic.IDE0017.severity = error

                [*.cs]
                csharp_style_expression_bodied_methods = false:silent
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, CodeActionIndex);

        [ConditionalFact(typeof(IsEnglishLocal))]
        public Task ConfigureEditorconfig_ChooseBestHeaderReversed_Error()
            => TestInRegularAndScriptAsync("""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\Program.cs">
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                csharp_style_expression_bodied_methods = false:silent

                [*.{cs,vb}]
                dotnet_style_qualification_for_field = false:silent
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\Program.cs">
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                csharp_style_expression_bodied_methods = false:silent

                [*.{cs,vb}]
                dotnet_style_qualification_for_field = false:silent

                # IDE0017: Simplify object initialization
                dotnet_diagnostic.IDE0017.severity = error
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, CodeActionIndex);

        [ConditionalFact(typeof(IsEnglishLocal))]
        public Task ConfigureEditorconfig_DotFileName_Error()
            => TestInRegularAndScriptAsync("""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\Program/Test.file.cs">
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[Test.file.cs]

                # IDE0017: Simplify object initialization
                dotnet_style_object_initializer = true:warning
                dotnet_diagnostic.IDE0017.severity = warning
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\Program/Test.file.cs">
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
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[Test.file.cs]

                # IDE0017: Simplify object initialization
                dotnet_style_object_initializer = true:error
                dotnet_diagnostic.IDE0017.severity = error
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, CodeActionIndex);
    }
}
