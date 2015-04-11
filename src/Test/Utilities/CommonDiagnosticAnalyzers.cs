// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public static class CommonDiagnosticAnalyzers
    {
        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        public class AnalyzerForErrorLogTest : DiagnosticAnalyzer
        {
            public static readonly DiagnosticDescriptor Descriptor1 = new DiagnosticDescriptor(
                "ID1",
                "Title1",
                "Message1",
                "Category1",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: "Description1",
                helpLinkUri: "HelpLink1",
                customTags: new[] { "1_CustomTag1", "1_CustomTag2" });

            public static readonly DiagnosticDescriptor Descriptor2 = new DiagnosticDescriptor(
                "ID2",
                "Title2",
                "Message2",
                "Category2",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: "Description2",
                helpLinkUri: "HelpLink2",
                customTags: new[] { "2_CustomTag1", "2_CustomTag2" });

            private static readonly ImmutableDictionary<string, string> _properties =
                new Dictionary<string, string> { { "Key1", "Value1" }, { "Key2", "Value2" } }.ToImmutableDictionary();

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    return ImmutableArray.Create(Descriptor1, Descriptor2);
                }
            }

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterCompilationAction(compilationContext =>
                {
                    // With location diagnostic.
                    var location = compilationContext.Compilation.SyntaxTrees.First().GetRoot().GetLocation();
                    compilationContext.ReportDiagnostic(Diagnostic.Create(Descriptor1, location, _properties));

                    // No location diagnostic.
                    compilationContext.ReportDiagnostic(Diagnostic.Create(Descriptor2, Location.None, _properties));
                });
            }

            private static string GetExpectedPropertiesMapText()
            {
                var isFirst = true;
                var expectedText = @",
        ""customProperties"": {";

                foreach (var kvp in _properties.OrderBy(kvp => kvp.Key))
                {
                    if (!isFirst)
                    {
                        expectedText += ",";
                    }

                    expectedText += string.Format(@"
          ""{0}"": ""{1}""", kvp.Key, kvp.Value);

                    isFirst = false;
                }

                expectedText += @"
        }";
                return expectedText;
            }

            public static string GetExpectedErrorLogIssuesText(Compilation compilation, string outputFilePath)
            {
                var tree = compilation.SyntaxTrees.First();
                var root = tree.GetRoot();
                var expectedLineSpan = root.GetLocation().GetLineSpan();

                return @"
  ""issues"": [
    {
      ""ruleId"": """ + Descriptor1.Id + @""",
      ""locations"": [
        {
          ""analysisTarget"": [
            {
              ""uri"": """ + tree.FilePath + @""",
              ""region"": {
                ""startLine"": " + expectedLineSpan.StartLinePosition.Line + @",
                ""startColumn"": " + expectedLineSpan.StartLinePosition.Character + @",
                ""endLine"": " + expectedLineSpan.EndLinePosition.Line + @",
                ""endColumn"": " + expectedLineSpan.EndLinePosition.Character + @"
              }
            }
          ]
        }
      ],
      ""shortMessage"": """ + Descriptor1.MessageFormat + @""",
      ""fullMessage"": """ + Descriptor1.Description + @""",
      ""properties"": {
        ""severity"": """ + Descriptor1.DefaultSeverity + @""",
        ""warningLevel"": ""1"",
        ""defaultSeverity"": """ + Descriptor1.DefaultSeverity + @""",
        ""title"": """ + Descriptor1.Title + @""",
        ""category"": """ + Descriptor1.Category + @""",
        ""helpLink"": """ + Descriptor1.HelpLinkUri + @""",
        ""isEnabledByDefault"": """ + Descriptor1.IsEnabledByDefault + @""",
        ""customTags"": """ + Descriptor1.CustomTags.Join(";") + @"""" +
        GetExpectedPropertiesMapText() + @"
      }
    },
    {
      ""ruleId"": """ + Descriptor2.Id + @""",
      ""locations"": [
      ],
      ""shortMessage"": """ + Descriptor2.MessageFormat + @""",
      ""fullMessage"": """ + Descriptor2.Description + @""",
      ""properties"": {
        ""severity"": """ + Descriptor2.DefaultSeverity + @""",
        ""defaultSeverity"": """ + Descriptor2.DefaultSeverity + @""",
        ""title"": """ + Descriptor2.Title + @""",
        ""category"": """ + Descriptor2.Category + @""",
        ""helpLink"": """ + Descriptor2.HelpLinkUri + @""",
        ""isEnabledByDefault"": """ + Descriptor2.IsEnabledByDefault + @""",
        ""customTags"": """ + Descriptor2.CustomTags.Join(";") + @"""" +
        GetExpectedPropertiesMapText() + @"
      }
    }
  ]
}";
            }

        }

        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        public class NotConfigurableDiagnosticAnalyzer : DiagnosticAnalyzer
        {
            public static readonly DiagnosticDescriptor EnabledRule = new DiagnosticDescriptor(
                "ID1",
                "Title1",
                "Message1",
                "Category1",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                customTags: WellKnownDiagnosticTags.NotConfigurable);

            public static readonly DiagnosticDescriptor DisabledRule = new DiagnosticDescriptor(
                "ID2",
                "Title2",
                "Message2",
                "Category2",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: false,
                customTags: WellKnownDiagnosticTags.NotConfigurable);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    return ImmutableArray.Create(EnabledRule, DisabledRule);
                }
            }

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterCompilationAction(compilationContext =>
                {
                    // Report enabled diagnostic.
                    compilationContext.ReportDiagnostic(Diagnostic.Create(EnabledRule, Location.None));

                    // Try to report disabled diagnostic.
                    compilationContext.ReportDiagnostic(Diagnostic.Create(DisabledRule, Location.None));
                });
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        public class CodeBlockActionAnalyzer : DiagnosticAnalyzer
        {
            public static readonly DiagnosticDescriptor CodeBlockTopLevelRule = new DiagnosticDescriptor(
                "CodeBlockTopLevelRuleId",
                "CodeBlockTopLevelRuleTitle",
                "CodeBlock : {0}",
                "Category",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            public static readonly DiagnosticDescriptor CodeBlockPerCompilationRule = new DiagnosticDescriptor(
                "CodeBlockPerCompilationRuleId",
                "CodeBlockPerCompilationRuleTitle",
                "CodeBlock : {0}",
                "Category",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    return ImmutableArray.Create(CodeBlockTopLevelRule, CodeBlockPerCompilationRule);
                }
            }

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterCodeBlockAction(codeBlockContext =>
                {
                    codeBlockContext.ReportDiagnostic(Diagnostic.Create(CodeBlockTopLevelRule, codeBlockContext.OwningSymbol.Locations[0], codeBlockContext.OwningSymbol.Name));
                });

                context.RegisterCompilationStartAction(compilationStartContext =>
                {
                    compilationStartContext.RegisterCodeBlockAction(codeBlockContext =>
                    {
                        codeBlockContext.ReportDiagnostic(Diagnostic.Create(CodeBlockPerCompilationRule, codeBlockContext.OwningSymbol.Locations[0], codeBlockContext.OwningSymbol.Name));
                    });
                });
            }
        }
    }
}