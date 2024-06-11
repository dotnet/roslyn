// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.Configuration.ConfigureSeverity;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.TypeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.Configuration.ConfigureSeverity
{
    public sealed class VarForBuiltInTypesSeverityConfigurationTests : AbstractSuppressionDiagnosticTest
    {
        protected internal override string GetLanguage() => LanguageNames.CSharp;

        protected override ParseOptions GetScriptOptions() => Options.Regular;

        internal override Tuple<DiagnosticAnalyzer, IConfigurationFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
            => new(new CSharpUseImplicitTypeDiagnosticAnalyzer(), new ConfigureSeverityLevelCodeFixProvider());

        protected override int CodeActionIndex => 0;

        [Fact]
        public async Task MaintainValue()
        {
            // make sure we start with an existing value of csharp_style_var_for_built_in_types option
            // specified in the editorconfig that's different from the default value:
            Assert.False(CSharpCodeStyleOptions.VarForBuiltInTypes.DefaultValue.Value);
            Assert.Equal(NotificationOption2.Silent, CSharpCodeStyleOptions.VarForBuiltInTypes.DefaultValue.Notification);

            var input = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
                [|int|] x = 1;
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">
                [*.cs]
                csharp_style_var_for_built_in_types = true:suggestion
                dotnet_diagnostic.IDE0007.severity = suggestion
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            var expected = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                         <Document FilePath="z:\\file.cs">
                int x = 1;
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">
                [*.cs]
                csharp_style_var_for_built_in_types = true:none
                dotnet_diagnostic.IDE0007.severity = none
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
        }
    }
}
