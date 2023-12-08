// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Xunit;

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

            private static readonly ImmutableDictionary<string, string> s_properties =
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
                context.RegisterCompilationStartAction(context =>
                {
                    // With location diagnostic.
                    context.RegisterSyntaxTreeAction(context =>
                    {
                        var location = context.Tree.GetRoot().GetLocation();
                        context.ReportDiagnostic(Diagnostic.Create(Descriptor1, location, s_properties));
                    });

                    // No location diagnostic.
                    context.RegisterCompilationEndAction(context =>
                        context.ReportDiagnostic(Diagnostic.Create(Descriptor2, Location.None, s_properties)));
                });
            }

            private static string GetExpectedPropertiesMapText()
            {
                var expectedText = @"
            ""customProperties"": {";

                foreach (var kvp in s_properties.OrderBy(kvp => kvp.Key))
                {
                    if (!expectedText.EndsWith("{"))
                    {
                        expectedText += ",";
                    }

                    expectedText += string.Format(@"
              ""{0}"": ""{1}""", kvp.Key, kvp.Value);
                }

                expectedText += @"
            }";

                return expectedText;
            }

            public static string GetExpectedV1ErrorLogResultsAndRulesText(Compilation compilation, bool warnAsError = false)
            {
                var tree = compilation.SyntaxTrees.First();
                var root = tree.GetRoot();
                var expectedLineSpan = root.GetLocation().GetLineSpan();
                var filePath = GetUriForPath(tree.FilePath);
                var effectiveSeverity1 = warnAsError || Descriptor1.DefaultSeverity == DiagnosticSeverity.Error ? "error" : "warning";
                var effectiveSeverity2 = warnAsError || Descriptor2.DefaultSeverity == DiagnosticSeverity.Error ? "error" : "warning";
                var warningLevelText = warnAsError ? string.Empty : @"
            ""warningLevel"": 1,";

                return @"
      ""results"": [
        {
          ""ruleId"": """ + Descriptor1.Id + @""",
          ""level"": """ + effectiveSeverity1 + @""",
          ""message"": """ + Descriptor1.MessageFormat + @""",
          ""locations"": [
            {
              ""resultFile"": {
                ""uri"": """ + filePath + @""",
                ""region"": {
                  ""startLine"": " + (expectedLineSpan.StartLinePosition.Line + 1) + @",
                  ""startColumn"": " + (expectedLineSpan.StartLinePosition.Character + 1) + @",
                  ""endLine"": " + (expectedLineSpan.EndLinePosition.Line + 1) + @",
                  ""endColumn"": " + (expectedLineSpan.EndLinePosition.Character + 1) + @"
                }
              }
            }
          ],
          ""properties"": {" +
             warningLevelText + GetExpectedPropertiesMapText() + @"
          }
        },
        {
          ""ruleId"": """ + Descriptor2.Id + @""",
          ""level"": """ + effectiveSeverity2 + @""",
          ""message"": """ + Descriptor2.MessageFormat + @""",
          ""properties"": {" +
             GetExpectedPropertiesMapText() + @"
          }
        }
      ],
      ""rules"": {
        """ + Descriptor1.Id + @""": {
          ""id"": """ + Descriptor1.Id + @""",
          ""shortDescription"": """ + Descriptor1.Title + @""",
          ""fullDescription"": """ + Descriptor1.Description + @""",
          ""defaultLevel"": """ + (Descriptor1.DefaultSeverity == DiagnosticSeverity.Error ? "error" : "warning") + @""",
          ""helpUri"": """ + Descriptor1.HelpLinkUri + @""",
          ""properties"": {
            ""category"": """ + Descriptor1.Category + @""",
            ""isEnabledByDefault"": " + (Descriptor2.IsEnabledByDefault ? "true" : "false") + @",
            ""tags"": [
              " + String.Join("," + Environment.NewLine + "              ", Descriptor1.CustomTags.Select(s => $"\"{s}\"")) + @"
            ]
          }
        },
        """ + Descriptor2.Id + @""": {
          ""id"": """ + Descriptor2.Id + @""",
          ""shortDescription"": """ + Descriptor2.Title + @""",
          ""fullDescription"": """ + Descriptor2.Description + @""",
          ""defaultLevel"": """ + (Descriptor2.DefaultSeverity == DiagnosticSeverity.Error ? "error" : "warning") + @""",
          ""helpUri"": """ + Descriptor2.HelpLinkUri + @""",
          ""properties"": {
            ""category"": """ + Descriptor2.Category + @""",
            ""isEnabledByDefault"": " + (Descriptor2.IsEnabledByDefault ? "true" : "false") + @",
            ""tags"": [
              " + String.Join("," + Environment.NewLine + "              ", Descriptor2.CustomTags.Select(s => $"\"{s}\"")) + @"
            ]
          }
        }
      }
    }
  ]
}";
            }

            public static string GetExpectedV1ErrorLogWithSuppressionResultsAndRulesText(Compilation compilation)
            {
                var tree = compilation.SyntaxTrees.First();
                var root = tree.GetRoot();
                var expectedLineSpan = root.GetLocation().GetLineSpan();
                var filePath = GetUriForPath(tree.FilePath);

                return @"
      ""results"": [
        {
          ""ruleId"": """ + Descriptor1.Id + @""",
          ""level"": """ + (Descriptor1.DefaultSeverity == DiagnosticSeverity.Error ? "error" : "warning") + @""",
          ""message"": """ + Descriptor1.MessageFormat + @""",
          ""suppressionStates"": [
            ""suppressedInSource""
          ],
          ""locations"": [
            {
              ""resultFile"": {
                ""uri"": """ + filePath + @""",
                ""region"": {
                  ""startLine"": " + (expectedLineSpan.StartLinePosition.Line + 1) + @",
                  ""startColumn"": " + (expectedLineSpan.StartLinePosition.Character + 1) + @",
                  ""endLine"": " + (expectedLineSpan.EndLinePosition.Line + 1) + @",
                  ""endColumn"": " + (expectedLineSpan.EndLinePosition.Character + 1) + @"
                }
              }
            }
          ],
          ""properties"": {
            ""warningLevel"": 1," + GetExpectedPropertiesMapText() + @"
          }
        },
        {
          ""ruleId"": """ + Descriptor2.Id + @""",
          ""level"": """ + (Descriptor2.DefaultSeverity == DiagnosticSeverity.Error ? "error" : "warning") + @""",
          ""message"": """ + Descriptor2.MessageFormat + @""",
          ""properties"": {" +
             GetExpectedPropertiesMapText() + @"
          }
        }
      ],
      ""rules"": {
        """ + Descriptor1.Id + @""": {
          ""id"": """ + Descriptor1.Id + @""",
          ""shortDescription"": """ + Descriptor1.Title + @""",
          ""fullDescription"": """ + Descriptor1.Description + @""",
          ""defaultLevel"": """ + (Descriptor1.DefaultSeverity == DiagnosticSeverity.Error ? "error" : "warning") + @""",
          ""helpUri"": """ + Descriptor1.HelpLinkUri + @""",
          ""properties"": {
            ""category"": """ + Descriptor1.Category + @""",
            ""isEnabledByDefault"": " + (Descriptor2.IsEnabledByDefault ? "true" : "false") + @",
            ""tags"": [
              " + String.Join("," + Environment.NewLine + "              ", Descriptor1.CustomTags.Select(s => $"\"{s}\"")) + @"
            ]
          }
        },
        """ + Descriptor2.Id + @""": {
          ""id"": """ + Descriptor2.Id + @""",
          ""shortDescription"": """ + Descriptor2.Title + @""",
          ""fullDescription"": """ + Descriptor2.Description + @""",
          ""defaultLevel"": """ + (Descriptor2.DefaultSeverity == DiagnosticSeverity.Error ? "error" : "warning") + @""",
          ""helpUri"": """ + Descriptor2.HelpLinkUri + @""",
          ""properties"": {
            ""category"": """ + Descriptor2.Category + @""",
            ""isEnabledByDefault"": " + (Descriptor2.IsEnabledByDefault ? "true" : "false") + @",
            ""tags"": [
              " + String.Join("," + Environment.NewLine + "              ", Descriptor2.CustomTags.Select(s => $"\"{s}\"")) + @"
            ]
          }
        }
      }
    }
  ]
}";
            }

            public static string GetExpectedV2ErrorLogResultsText(Compilation compilation, bool warnAsError = false)
            {
                var tree = compilation.SyntaxTrees.First();
                var root = tree.GetRoot();
                var expectedLineSpan = root.GetLocation().GetLineSpan();
                var filePath = GetUriForPath(tree.FilePath);
                var effectiveSeverity1 = warnAsError || Descriptor1.DefaultSeverity == DiagnosticSeverity.Error ? "error" : "warning";
                var effectiveSeverity2 = warnAsError || Descriptor2.DefaultSeverity == DiagnosticSeverity.Error ? "error" : "warning";
                var warningLevelText = warnAsError ? string.Empty : @"
            ""warningLevel"": 1,";

                return
@"      ""results"": [
        {
          ""ruleId"": """ + Descriptor1.Id + @""",
          ""ruleIndex"": 0,
          ""level"": """ + effectiveSeverity1 + @""",
          ""message"": {
            ""text"": """ + Descriptor1.MessageFormat + @"""
          },
          ""locations"": [
            {
              ""physicalLocation"": {
                ""artifactLocation"": {
                  ""uri"": """ + filePath + @"""
                },
                ""region"": {
                  ""startLine"": " + (expectedLineSpan.StartLinePosition.Line + 1) + @",
                  ""startColumn"": " + (expectedLineSpan.StartLinePosition.Character + 1) + @",
                  ""endLine"": " + (expectedLineSpan.EndLinePosition.Line + 1) + @",
                  ""endColumn"": " + (expectedLineSpan.EndLinePosition.Character + 1) + @"
                }
              }
            }
          ],
          ""properties"": {" +
             warningLevelText + GetExpectedPropertiesMapText() + @"
          }
        },
        {
          ""ruleId"": """ + Descriptor2.Id + @""",
          ""ruleIndex"": 1,
          ""level"": """ + effectiveSeverity2 + @""",
          ""message"": {
            ""text"": """ + Descriptor2.MessageFormat + @"""
          },
          ""properties"": {" +
             GetExpectedPropertiesMapText() + @"
          }
        }
      ]";
            }

            public static string GetExpectedV2ErrorLogWithSuppressionResultsText(Compilation compilation, string justification, string suppressionType)
            {
                var tree = compilation.SyntaxTrees.First();
                var root = tree.GetRoot();
                var expectedLineSpan = root.GetLocation().GetLineSpan();
                var filePath = GetUriForPath(tree.FilePath);
                var expectedSuppressionPropertyMap = @",
              ""properties"": {
                ""suppressionType"": """ + suppressionType + @"""
              }";
                return
@"      ""results"": [
        {
          ""ruleId"": """ + Descriptor1.Id + @""",
          ""ruleIndex"": 0,
          ""level"": """ + (Descriptor1.DefaultSeverity == DiagnosticSeverity.Error ? "error" : "warning") + @""",
          ""message"": {
            ""text"": """ + Descriptor1.MessageFormat + @"""
          },
          ""suppressions"": [
            {
              ""kind"": ""inSource""" + (justification == null ? "" : @",
              ""justification"": """ + (justification) + @"""") + (suppressionType == null ? "" : expectedSuppressionPropertyMap) + @"
            }
          ],
          ""locations"": [
            {
              ""physicalLocation"": {
                ""artifactLocation"": {
                  ""uri"": """ + filePath + @"""
                },
                ""region"": {
                  ""startLine"": " + (expectedLineSpan.StartLinePosition.Line + 1) + @",
                  ""startColumn"": " + (expectedLineSpan.StartLinePosition.Character + 1) + @",
                  ""endLine"": " + (expectedLineSpan.EndLinePosition.Line + 1) + @",
                  ""endColumn"": " + (expectedLineSpan.EndLinePosition.Character + 1) + @"
                }
              }
            }
          ],
          ""properties"": {
            ""warningLevel"": 1," + GetExpectedPropertiesMapText() + @"
          }
        },
        {
          ""ruleId"": """ + Descriptor2.Id + @""",
          ""ruleIndex"": 1,
          ""level"": """ + (Descriptor2.DefaultSeverity == DiagnosticSeverity.Error ? "error" : "warning") + @""",
          ""message"": {
            ""text"": """ + Descriptor2.MessageFormat + @"""
          },
          ""properties"": {" +
             GetExpectedPropertiesMapText() + @"
          }
        }
      ]";
            }

            public static string GetExpectedV2SuppressionTextForRulesSection(string[] suppressionKinds)
            {
                if (suppressionKinds?.Length > 0)
                {
                    return @",
                ""isEverSuppressed"": ""true"",
                ""suppressionKinds"": [
                  " + string.Join("," + Environment.NewLine + "                  ", suppressionKinds.Select(s => $"\"{s}\"")) + @"
                ]";
                }

                return string.Empty;
            }

            public static string GetExpectedV2ErrorLogInvocationsText(
                params (string DescriptorId, int DescriptorIndex, ImmutableHashSet<ReportDiagnostic> EffectiveSeverities)[] overriddenEffectiveSeveritiesWithIndex)
            {
                if (overriddenEffectiveSeveritiesWithIndex.Length == 0)
                {
                    return string.Empty;
                }

                var first = true;
                var overridesContent = string.Empty;
                foreach (var (id, index, effectiveSeverities) in overriddenEffectiveSeveritiesWithIndex)
                {
                    foreach (var effectiveSeverity in effectiveSeverities.OrderBy(Comparer<ReportDiagnostic>.Default))
                    {
                        if (first)
                        {
                            first = false;
                        }
                        else
                        {
                            overridesContent += ",";
                        }

                        overridesContent += $@"
            {{
              ""descriptor"": {{
                ""id"": ""{id}"",
                ""index"": {index}
              }},
              ""configuration"": {{
                {GetConfigurationPropertyString(effectiveSeverity)}
              }}
            }}";
                    }
                }

                return $@"""invocations"": [
        {{
          ""executionSuccessful"": true,
          ""ruleConfigurationOverrides"": [{overridesContent}
          ]
        }}
      ]";
            }

            private static string GetConfigurationPropertyString(ReportDiagnostic severity)
            {
                if (severity == ReportDiagnostic.Suppress)
                    return @"""enabled"": false";

                var severityString = severity switch
                {
                    ReportDiagnostic.Error => "error",
                    ReportDiagnostic.Warn => "warning",
                    ReportDiagnostic.Info or ReportDiagnostic.Hidden => "note",
                    _ => throw ExceptionUtilities.UnexpectedValue(severity)
                };

                return $@"""level"": ""{severityString}""";
            }

            internal static string GetExpectedV2ErrorLogRulesText(
                ImmutableArray<(DiagnosticDescriptor Descriptor, DiagnosticDescriptorErrorLoggerInfo Info)> descriptorsWithInfo,
                CultureInfo culture,
                string[] suppressionKinds1 = null,
                string[] suppressionKinds2 = null)
            {
                var descriptor1Info = descriptorsWithInfo.Single(d => d.Descriptor.Id == Descriptor1.Id).Info;
                var descriptor1ExecutionTime = ReportAnalyzerUtil.GetFormattedAnalyzerExecutionTime(descriptor1Info.ExecutionTime, culture).Trim();
                var descriptor1ExecutionPercentage = ReportAnalyzerUtil.GetFormattedAnalyzerExecutionPercentage(descriptor1Info.ExecutionPercentage, culture).Trim();

                var descriptor2Info = descriptorsWithInfo.Single(d => d.Descriptor.Id == Descriptor2.Id).Info;
                var descriptor2ExecutionTime = ReportAnalyzerUtil.GetFormattedAnalyzerExecutionTime(descriptor2Info.ExecutionTime, culture).Trim();
                var descriptor2ExecutionPercentage = ReportAnalyzerUtil.GetFormattedAnalyzerExecutionPercentage(descriptor2Info.ExecutionPercentage, culture).Trim();

                return
@"          ""rules"": [
            {
              ""id"": """ + Descriptor1.Id + @""",
              ""shortDescription"": {
                ""text"": """ + Descriptor1.Title + @"""
              },
              ""fullDescription"": {
                ""text"": """ + Descriptor1.Description + @"""
              },
              ""helpUri"": """ + Descriptor1.HelpLinkUri + @""",
              ""properties"": {
                ""category"": """ + Descriptor1.Category + @"""" + GetExpectedV2SuppressionTextForRulesSection(suppressionKinds1) + @",
                ""executionTimeInSeconds"": """ + descriptor1ExecutionTime + @""",
                ""executionTimeInPercentage"": """ + descriptor1ExecutionPercentage + @""",
                ""tags"": [
                  " + string.Join("," + Environment.NewLine + "                  ", Descriptor1.CustomTags.Select(s => $"\"{s}\"")) + @"
                ]
              }
            },
            {
              ""id"": """ + Descriptor2.Id + @""",
              ""shortDescription"": {
                ""text"": """ + Descriptor2.Title + @"""
              },
              ""fullDescription"": {
                ""text"": """ + Descriptor2.Description + @"""
              },
              ""defaultConfiguration"": {
                ""level"": """ + (Descriptor2.DefaultSeverity == DiagnosticSeverity.Error ? "error" : "warning") + @"""
              },
              ""helpUri"": """ + Descriptor2.HelpLinkUri + @""",
              ""properties"": {
                ""category"": """ + Descriptor2.Category + @"""" + GetExpectedV2SuppressionTextForRulesSection(suppressionKinds2) + @",
                ""executionTimeInSeconds"": """ + descriptor2ExecutionTime + @""",
                ""executionTimeInPercentage"": """ + descriptor2ExecutionPercentage + @""",
                ""tags"": [
                  " + String.Join("," + Environment.NewLine + "                  ", Descriptor2.CustomTags.Select(s => $"\"{s}\"")) + @"
                ]
              }
            }
          ]";
            }

            public static string GetUriForPath(string path)
            {
                var uri = new Uri(path, UriKind.RelativeOrAbsolute);
                return uri.IsAbsoluteUri
                    ? uri.AbsoluteUri
                    : WebUtility.UrlEncode(uri.ToString());
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        public class SuppressorForErrorLogTest : DiagnosticSuppressor
        {
            public static readonly SuppressionDescriptor Descriptor1 = new("SPR0001", AnalyzerForErrorLogTest.Descriptor1.Id, "SuppressorJustification1");
            public static readonly SuppressionDescriptor Descriptor2 = new("SPR0002", AnalyzerForErrorLogTest.Descriptor2.Id, "SuppressorJustification2");

            public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions
                => ImmutableArray.Create(Descriptor1, Descriptor2);

            public override void ReportSuppressions(SuppressionAnalysisContext context)
            {
                foreach (var diagnostic in context.ReportedDiagnostics)
                {
                    var descriptor = diagnostic.Id == Descriptor1.SuppressedDiagnosticId ? Descriptor1 : Descriptor2;
                    context.ReportSuppression(Suppression.Create(descriptor, diagnostic));
                }
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
            private readonly bool _onlyStatelessAction;

            public CodeBlockActionAnalyzer(bool onlyStatelessAction = false)
            {
                _onlyStatelessAction = onlyStatelessAction;
            }

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

                if (!_onlyStatelessAction)
                {
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

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        public class CSharpCodeBlockObjectCreationAnalyzer : CodeBlockObjectCreationAnalyzer<SyntaxKind>
        {
            protected override SyntaxKind ObjectCreationExpressionKind => SyntaxKind.ObjectCreationExpression;
        }

        [DiagnosticAnalyzer(LanguageNames.VisualBasic)]
        public class VisualBasicCodeBlockObjectCreationAnalyzer : CodeBlockObjectCreationAnalyzer<VisualBasic.SyntaxKind>
        {
            protected override VisualBasic.SyntaxKind ObjectCreationExpressionKind => VisualBasic.SyntaxKind.ObjectCreationExpression;
        }

        public abstract class CodeBlockObjectCreationAnalyzer<TLanguageKindEnum> : DiagnosticAnalyzer
            where TLanguageKindEnum : struct
        {
            public static readonly DiagnosticDescriptor DiagnosticDescriptor = new DiagnosticDescriptor(
                "Id",
                "Title",
                "Message",
                "Category",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptor);
            protected abstract TLanguageKindEnum ObjectCreationExpressionKind { get; }

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterCodeBlockStartAction<TLanguageKindEnum>(codeBlockStartContext =>
                {
                    codeBlockStartContext.RegisterSyntaxNodeAction(syntaxNodeContext =>
                    {
                        syntaxNodeContext.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptor, syntaxNodeContext.Node.GetLocation()));
                    },
                    ObjectCreationExpressionKind);
                });
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        public class CSharpGenericNameAnalyzer : DiagnosticAnalyzer
        {
            public const string DiagnosticId = nameof(DiagnosticId);
            public const string Title = nameof(Title);
            public const string Message = nameof(Message);
            public const string Category = nameof(Category);
            public const DiagnosticSeverity Severity = DiagnosticSeverity.Warning;

            internal static DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticId, Title, Message,
                                         Category, Severity, isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
                 ImmutableArray.Create(Rule);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.GenericName);
            }

            private void AnalyzeNode(SyntaxNodeAnalysisContext context)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation()));
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        public class CSharpNamespaceDeclarationAnalyzer : AbstractNamespaceDeclarationAnalyzer<SyntaxKind>
        {
            protected override SyntaxKind NamespaceDeclarationSyntaxKind => SyntaxKind.NamespaceDeclaration;
        }

        [DiagnosticAnalyzer(LanguageNames.VisualBasic)]
        public class VisualBasicNamespaceDeclarationAnalyzer : AbstractNamespaceDeclarationAnalyzer<VisualBasic.SyntaxKind>
        {
            protected override VisualBasic.SyntaxKind NamespaceDeclarationSyntaxKind => VisualBasic.SyntaxKind.NamespaceStatement;
        }

        public abstract class AbstractNamespaceDeclarationAnalyzer<TLanguageKindEnum> : DiagnosticAnalyzer
            where TLanguageKindEnum : struct
        {
            public const string DiagnosticId = nameof(DiagnosticId);
            public const string Title = nameof(Title);
            public const string Message = nameof(Message);
            public const string Category = nameof(Category);
            public const DiagnosticSeverity Severity = DiagnosticSeverity.Warning;

            internal static DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticId, Title, Message,
                                         Category, Severity, isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
                 ImmutableArray.Create(Rule);
            protected abstract TLanguageKindEnum NamespaceDeclarationSyntaxKind { get; }

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSyntaxNodeAction(AnalyzeNode, NamespaceDeclarationSyntaxKind);
            }

            private void AnalyzeNode(SyntaxNodeAnalysisContext context)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation()));
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        public sealed class AnalyzerWithNoActions : DiagnosticAnalyzer
        {
            public static readonly DiagnosticDescriptor DummyRule = new DiagnosticDescriptor(
                "ID1",
                "Title1",
                "Message1",
                "Category1",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DummyRule);
            public override void Initialize(AnalysisContext context) { }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        public sealed class AnalyzerWithDisabledRules : DiagnosticAnalyzer
        {
            public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
                "ID1",
                "Title1",
                "Message1",
                "Category1",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: false);

            internal readonly ConcurrentSet<ISymbol> CallbackSymbols = new();

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);
            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSymbolAction(context =>
                {
                    CallbackSymbols.Add(context.Symbol);
                    context.ReportDiagnostic(Diagnostic.Create(Rule, context.Symbol.Locations[0]));
                }, SymbolKind.NamedType);
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        public sealed class EnsureNoMergedNamespaceSymbolAnalyzer : DiagnosticAnalyzer
        {
            public const string DiagnosticId = nameof(DiagnosticId);
            public const string Title = nameof(Title);
            public const string Message = nameof(Message);
            public const string Category = nameof(Category);
            public const DiagnosticSeverity Severity = DiagnosticSeverity.Warning;

            internal static DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticId, Title, Message,
                                         Category, Severity, isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
                 ImmutableArray.Create(Rule);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Namespace);
            }

            private void AnalyzeSymbol(SymbolAnalysisContext context)
            {
                // Ensure we are not invoked for merged namespace symbol, but instead for constituent namespace scoped to the source assembly.
                var ns = (INamespaceSymbol)context.Symbol;
                if (ns.ContainingAssembly != context.Compilation.Assembly || ns.ConstituentNamespaces.Length > 1)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, ns.Locations[0]));
                }
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        public sealed class AnalyzerWithNoSupportedDiagnostics : DiagnosticAnalyzer
        {
            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
            public override void Initialize(AnalysisContext context) { }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        public sealed class AnalyzerWithInvalidDiagnosticId : DiagnosticAnalyzer
        {
            public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
                "Invalid ID",
                "Title1",
                "Message1",
                "Category1",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);
            public override void Initialize(AnalysisContext context)
            {
                context.RegisterCompilationAction(compilationContext =>
                    compilationContext.ReportDiagnostic(Diagnostic.Create(Descriptor, Location.None)));
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        public sealed class AnalyzerWithNullDescriptor : DiagnosticAnalyzer
        {
            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create((DiagnosticDescriptor)null);
            public override void Initialize(AnalysisContext context)
            {
                context.RegisterCompilationAction(_ => { });
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        public sealed class AnalyzerWithCSharpCompilerDiagnosticId : DiagnosticAnalyzer
        {
            public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
#pragma warning disable RS1029 // Do not use reserved diagnostic IDs.
                "CS101",
#pragma warning restore RS1029 // Do not use reserved diagnostic IDs.
                "Title1",
                "Message1",
                "Category1",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);
            public override void Initialize(AnalysisContext context)
            {
                context.RegisterCompilationAction(compilationContext =>
                    compilationContext.ReportDiagnostic(Diagnostic.Create(Descriptor, Location.None)));
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        public sealed class AnalyzerWithBasicCompilerDiagnosticId : DiagnosticAnalyzer
        {
            public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
#pragma warning disable RS1029 // Do not use reserved diagnostic IDs.
                "BC101",
#pragma warning restore RS1029 // Do not use reserved diagnostic IDs.
                "Title1",
                "Message1",
                "Category1",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);
            public override void Initialize(AnalysisContext context)
            {
                context.RegisterCompilationAction(compilationContext =>
                    compilationContext.ReportDiagnostic(Diagnostic.Create(Descriptor, Location.None)));
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        public sealed class AnalyzerWithInvalidDiagnosticSpan : DiagnosticAnalyzer
        {
            private readonly TextSpan _badSpan;

            public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
                "ID",
                "Title1",
                "Message",
                "Category1",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            public AnalyzerWithInvalidDiagnosticSpan(TextSpan badSpan) => _badSpan = badSpan;
            public Exception ThrownException { get; set; }
            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);
            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSyntaxTreeAction(c =>
                {
                    try
                    {
                        ThrownException = null;
                        c.ReportDiagnostic(Diagnostic.Create(Descriptor, SourceLocation.Create(c.Tree, _badSpan)));
                    }
                    catch (Exception e)
                    {
                        ThrownException = e;
                        throw;
                    }
                });
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        public sealed class AnalyzerWithInvalidDiagnosticLocation : DiagnosticAnalyzer
        {
            private readonly Location _invalidLocation;
            private readonly ActionKind _actionKind;
            private readonly bool _testInvalidAdditionalLocation;

            public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
                "ID",
                "Title1",
                "Message {0}",
                "Category1",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            public enum ActionKind
            {
                Symbol,
                CodeBlock,
                Operation,
                OperationBlockEnd,
                Compilation,
                CompilationEnd,
                SyntaxTree
            }

            public AnalyzerWithInvalidDiagnosticLocation(SyntaxTree treeInAnotherCompilation, ActionKind actionKind, bool testInvalidAdditionalLocation)
            {
                _invalidLocation = treeInAnotherCompilation.GetRoot().GetLocation();
                _actionKind = actionKind;
                _testInvalidAdditionalLocation = testInvalidAdditionalLocation;
            }

            private void ReportDiagnostic(Action<Diagnostic> addDiagnostic, ActionKind actionKindBeingRun)
            {
                if (_actionKind == actionKindBeingRun)
                {
                    var diagnostic = _testInvalidAdditionalLocation ?
                        Diagnostic.Create(Descriptor, Location.None, additionalLocations: new[] { _invalidLocation }) :
                        Diagnostic.Create(Descriptor, _invalidLocation);
                    addDiagnostic(diagnostic);
                }
            }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);
            public override void Initialize(AnalysisContext context)
            {
                context.RegisterCompilationStartAction(cc =>
                {
                    cc.RegisterSymbolAction(c => ReportDiagnostic(c.ReportDiagnostic, ActionKind.Symbol), SymbolKind.NamedType);
                    cc.RegisterCodeBlockAction(c => ReportDiagnostic(c.ReportDiagnostic, ActionKind.CodeBlock));
                    cc.RegisterCompilationEndAction(c => ReportDiagnostic(c.ReportDiagnostic, ActionKind.CompilationEnd));

                    cc.RegisterOperationBlockStartAction(oc =>
                    {
                        oc.RegisterOperationAction(c => ReportDiagnostic(c.ReportDiagnostic, ActionKind.Operation), OperationKind.VariableDeclarationGroup);
                        oc.RegisterOperationBlockEndAction(c => ReportDiagnostic(c.ReportDiagnostic, ActionKind.OperationBlockEnd));
                    });
                });

                context.RegisterSyntaxTreeAction(c => ReportDiagnostic(c.ReportDiagnostic, ActionKind.SyntaxTree));
                context.RegisterCompilationAction(cc => ReportDiagnostic(cc.ReportDiagnostic, ActionKind.Compilation));
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        public sealed class AnalyzerThatThrowsInSupportedDiagnostics : DiagnosticAnalyzer
        {
            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => throw new NotImplementedException();
            public override void Initialize(AnalysisContext context) { }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        public sealed class AnalyzerThatThrowsInGetMessage : DiagnosticAnalyzer
        {
            public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
                "ID1",
                "Title1",
                new MyLocalizableStringThatThrows(),
                "Category1",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);
            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSymbolAction(symbolContext =>
                {
                    symbolContext.ReportDiagnostic(Diagnostic.Create(Rule, symbolContext.Symbol.Locations[0]));
                }, SymbolKind.NamedType);
            }

            private sealed class MyLocalizableStringThatThrows : LocalizableString
            {
                protected override bool AreEqual(object other)
                {
                    return ReferenceEquals(this, other);
                }

                protected override int GetHash()
                {
                    return 0;
                }

                protected override string GetText(IFormatProvider formatProvider)
                {
                    throw new NotImplementedException();
                }
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        public sealed class AnalyzerReportingMisformattedDiagnostic : DiagnosticAnalyzer
        {
            public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
                "ID1",
                "Title1",
                "Symbol Name: {0}, Extra argument: {1}",
                "Category1",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);
            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSymbolAction(symbolContext =>
                {
                    // Report diagnostic with incorrect number of message format arguments.
                    symbolContext.ReportDiagnostic(Diagnostic.Create(Rule, symbolContext.Symbol.Locations[0], symbolContext.Symbol.Name));
                }, SymbolKind.NamedType);
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        public class CompilationAnalyzerWithSeverity : DiagnosticAnalyzer
        {
            public const string DiagnosticId = "ID1000";
            public CompilationAnalyzerWithSeverity(
                DiagnosticSeverity severity,
                bool configurable)
            {
                var customTags = !configurable ? new[] { WellKnownDiagnosticTags.NotConfigurable } : Array.Empty<string>();
                Descriptor = new DiagnosticDescriptor(
                    DiagnosticId,
                    "Description1",
                    string.Empty,
                    "Analysis",
                    severity,
                    true,
                    customTags: customTags);
            }

            public DiagnosticDescriptor Descriptor { get; }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterCompilationAction(this.OnCompilation);
            }

            private void OnCompilation(CompilationAnalysisContext context)
            {
                // Report the diagnostic on all trees in compilation.
                foreach (var tree in context.Compilation.SyntaxTrees)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, tree.GetRoot().GetLocation()));
                }
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        public class SemanticModelAnalyzerWithId : DiagnosticAnalyzer
        {
            public SemanticModelAnalyzerWithId(string diagnosticId)
            {
                Descriptor = new DiagnosticDescriptor(
                    diagnosticId,
                    "Description1",
                    string.Empty,
                    "Analysis",
                    DiagnosticSeverity.Warning,
                    isEnabledByDefault: true);
            }

            public DiagnosticDescriptor Descriptor { get; }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context) =>
                context.RegisterSemanticModelAction(context =>
                    context.ReportDiagnostic(
                        Diagnostic.Create(Descriptor, context.SemanticModel.SyntaxTree.GetRoot().GetLocation())));
        }

        /// <summary>
        /// This analyzer is intended to be used only when concurrent execution is enabled for analyzers.
        /// This analyzer will deadlock if the driver runs analyzers on a single thread OR takes a lock around callbacks into this analyzer to prevent concurrent analyzer execution
        /// Former indicates a bug in the test using this analyzer and the latter indicates a bug in the analyzer driver.
        /// </summary>
        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        public class ConcurrentAnalyzer : DiagnosticAnalyzer
        {
            private readonly ImmutableHashSet<string> _symbolNames;
            private int _token;

            public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
                "ConcurrentAnalyzerId",
                "Title",
                "ConcurrentAnalyzerMessage for symbol '{0}'",
                "Category",
                DiagnosticSeverity.Warning,
                true);

            public ConcurrentAnalyzer(IEnumerable<string> symbolNames)
            {
                Assert.True(Environment.ProcessorCount > 1, "This analyzer is intended to be used only in a concurrent environment.");
                _symbolNames = symbolNames.ToImmutableHashSet();
                _token = 0;
            }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);
            public override void Initialize(AnalysisContext context)
            {
                context.RegisterCompilationStartAction(this.OnCompilationStart);

                // Enable concurrent action callbacks on analyzer.
                context.EnableConcurrentExecution();
            }

            private void OnCompilationStart(CompilationStartAnalysisContext context)
            {
                Assert.True(context.Compilation.Options.ConcurrentBuild, "This analyzer is intended to be used only when concurrent build is enabled.");

                var pendingSymbols = new ConcurrentSet<INamedTypeSymbol>();
                foreach (var type in context.Compilation.GlobalNamespace.GetTypeMembers())
                {
                    if (_symbolNames.Contains(type.Name))
                    {
                        pendingSymbols.Add(type);
                    }
                }

                context.RegisterSymbolAction(symbolContext =>
                {
                    if (!pendingSymbols.Remove((INamedTypeSymbol)symbolContext.Symbol))
                    {
                        return;
                    }

                    var myToken = Interlocked.Increment(ref _token);
                    if (myToken == 1)
                    {
                        // Wait for all symbol callbacks to execute.
                        // This analyzer will deadlock if the driver doesn't attempt concurrent callbacks.
                        while (pendingSymbols.Any())
                        {
                            Thread.Sleep(10);
                        }
                    }

                    // ok, now report diagnostic on the symbol.
                    var diagnostic = Diagnostic.Create(Descriptor, symbolContext.Symbol.Locations[0], symbolContext.Symbol.Name);
                    symbolContext.ReportDiagnostic(diagnostic);
                }, SymbolKind.NamedType);
            }
        }

        /// <summary>
        /// This analyzer will report diagnostics only if it receives any concurrent action callbacks, which would be a
        /// bug in the analyzer driver as this analyzer doesn't invoke <see cref="AnalysisContext.EnableConcurrentExecution"/>.
        /// </summary>
        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        public class NonConcurrentAnalyzer : DiagnosticAnalyzer
        {
            public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
                "NonConcurrentAnalyzerId",
                "Title",
                "Analyzer driver made concurrent action callbacks, when analyzer didn't register for concurrent execution",
                "Category",
                DiagnosticSeverity.Warning,
                true);
            private const int registeredActionCounts = 1000;

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);
            public override void Initialize(AnalysisContext context)
            {
                SemaphoreSlim gate = new SemaphoreSlim(initialCount: registeredActionCounts);
                for (var i = 0; i < registeredActionCounts; i++)
                {
                    context.RegisterSymbolAction(symbolContext =>
                    {
                        using (gate.DisposableWait(symbolContext.CancellationToken))
                        {
                            ReportDiagnosticIfActionInvokedConcurrently(gate, symbolContext);
                        }
                    }, SymbolKind.NamedType);
                }
            }

            private void ReportDiagnosticIfActionInvokedConcurrently(SemaphoreSlim gate, SymbolAnalysisContext symbolContext)
            {
                if (gate.CurrentCount != registeredActionCounts - 1)
                {
                    var diagnostic = Diagnostic.Create(Descriptor, symbolContext.Symbol.Locations[0]);
                    symbolContext.ReportDiagnostic(diagnostic);
                }
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        public sealed class OperationAnalyzer : DiagnosticAnalyzer
        {
            private readonly ActionKind _actionKind;
            private readonly bool _verifyGetControlFlowGraph;
            private readonly ConcurrentDictionary<IOperation, (ControlFlowGraph Graph, ISymbol AssociatedSymbol)> _controlFlowGraphMapOpt;

            public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
                "ID",
                "Title1",
                "{0} diagnostic",
                "Category1",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            public enum ActionKind
            {
                Operation,
                OperationInOperationBlockStart,
                OperationBlock,
                OperationBlockEnd
            }

            public OperationAnalyzer(ActionKind actionKind, bool verifyGetControlFlowGraph = false)
            {
                _actionKind = actionKind;
                _verifyGetControlFlowGraph = verifyGetControlFlowGraph;
                _controlFlowGraphMapOpt = verifyGetControlFlowGraph ? new ConcurrentDictionary<IOperation, (ControlFlowGraph, ISymbol)>() : null;
            }

            public ImmutableArray<(ControlFlowGraph Graph, ISymbol AssociatedSymbol)> GetControlFlowGraphs()
            {
                Assert.True(_verifyGetControlFlowGraph);
                return _controlFlowGraphMapOpt.Values.OrderBy(flowGraphAndSymbol => flowGraphAndSymbol.Graph.OriginalOperation.Syntax.SpanStart).ToImmutableArray();
            }

            private void ReportDiagnostic(Action<Diagnostic> addDiagnostic, Location location)
            {
                var diagnostic = Diagnostic.Create(Descriptor, location, _actionKind);
                addDiagnostic(diagnostic);
            }

            private void CacheAndVerifyControlFlowGraph(ImmutableArray<IOperation> operationBlocks, Func<IOperation, (ControlFlowGraph Graph, ISymbol AssociatedSymbol)> getControlFlowGraph)
            {
                if (_verifyGetControlFlowGraph)
                {
                    foreach (var operationBlock in operationBlocks)
                    {
                        var controlFlowGraphAndSymbol = getControlFlowGraph(operationBlock);
                        Assert.NotNull(controlFlowGraphAndSymbol);
                        Assert.Same(operationBlock.GetRootOperation(), controlFlowGraphAndSymbol.Graph.OriginalOperation);

                        _controlFlowGraphMapOpt.Add(controlFlowGraphAndSymbol.Graph.OriginalOperation, controlFlowGraphAndSymbol);

                        // Verify analyzer driver caches the flow graph.
                        Assert.Same(controlFlowGraphAndSymbol.Graph, getControlFlowGraph(operationBlock).Graph);

                        // Verify exceptions for invalid inputs.
                        try
                        {
                            _ = getControlFlowGraph(null);
                        }
                        catch (ArgumentNullException ex)
                        {
                            Assert.Equal(new ArgumentNullException("operationBlock").Message, ex.Message);
                        }

                        try
                        {
                            _ = getControlFlowGraph(operationBlock.Descendants().First());
                        }
                        catch (ArgumentException ex)
                        {
                            Assert.Equal(new ArgumentException(CodeAnalysisResources.InvalidOperationBlockForAnalysisContext, "operationBlock").Message, ex.Message);
                        }
                    }
                }
            }

            private void VerifyControlFlowGraph(OperationAnalysisContext operationContext, bool inBlockAnalysisContext)
            {
                if (_verifyGetControlFlowGraph)
                {
                    var controlFlowGraph = operationContext.GetControlFlowGraph();
                    Assert.NotNull(controlFlowGraph);

                    // Verify analyzer driver caches the flow graph.
                    Assert.Same(controlFlowGraph, operationContext.GetControlFlowGraph());

                    var rootOperation = operationContext.Operation.GetRootOperation();
                    if (inBlockAnalysisContext)
                    {
                        // Verify same flow graph returned from containing block analysis context.
                        Assert.Same(controlFlowGraph, _controlFlowGraphMapOpt[rootOperation].Graph);
                    }
                    else
                    {
                        _controlFlowGraphMapOpt[rootOperation] = (controlFlowGraph, operationContext.ContainingSymbol);
                    }
                }
            }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);
            public override void Initialize(AnalysisContext context)
            {
                switch (_actionKind)
                {
                    case ActionKind.OperationBlockEnd:
                        context.RegisterOperationBlockStartAction(blockStartContext =>
                        {
                            blockStartContext.RegisterOperationBlockEndAction(c => ReportDiagnostic(c.ReportDiagnostic, c.OwningSymbol.Locations[0]));
                            CacheAndVerifyControlFlowGraph(blockStartContext.OperationBlocks, op => (blockStartContext.GetControlFlowGraph(op), blockStartContext.OwningSymbol));
                        });

                        break;

                    case ActionKind.OperationBlock:
                        context.RegisterOperationBlockAction(blockContext =>
                        {
                            ReportDiagnostic(blockContext.ReportDiagnostic, blockContext.OwningSymbol.Locations[0]);
                            CacheAndVerifyControlFlowGraph(blockContext.OperationBlocks, op => (blockContext.GetControlFlowGraph(op), blockContext.OwningSymbol));
                        });

                        break;

                    case ActionKind.Operation:
                        context.RegisterOperationAction(operationContext =>
                        {
                            ReportDiagnostic(operationContext.ReportDiagnostic, operationContext.Operation.Syntax.GetLocation());
                            VerifyControlFlowGraph(operationContext, inBlockAnalysisContext: false);
                        }, OperationKind.Literal);

                        break;

                    case ActionKind.OperationInOperationBlockStart:
                        context.RegisterOperationBlockStartAction(blockContext =>
                        {
                            CacheAndVerifyControlFlowGraph(blockContext.OperationBlocks, op => (blockContext.GetControlFlowGraph(op), blockContext.OwningSymbol));
                            blockContext.RegisterOperationAction(operationContext =>
                            {
                                ReportDiagnostic(operationContext.ReportDiagnostic, operationContext.Operation.Syntax.GetLocation());
                                VerifyControlFlowGraph(operationContext, inBlockAnalysisContext: true);
                            }, OperationKind.Literal);
                        });
                        break;
                }
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        public sealed class OperationBlockAnalyzer : DiagnosticAnalyzer
        {
            public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
                "ID",
                "Title1",
                "OperationBlock for {0}: {1}",
                "Category1",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);
            public override void Initialize(AnalysisContext context)
            {
                context.RegisterOperationBlockAction(c =>
                {
                    foreach (var operationRoot in c.OperationBlocks)
                    {
                        var diagnostic = Diagnostic.Create(Descriptor, c.OwningSymbol.Locations[0], c.OwningSymbol.Name, operationRoot.Kind);
                        c.ReportDiagnostic(diagnostic);
                    }
                });
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        public sealed class FieldReferenceOperationAnalyzer : DiagnosticAnalyzer
        {
            private readonly bool _doOperationBlockAnalysis;
            public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
                "ID",
                "Title",
                "Field {0} = {1}",
                "Category",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            public FieldReferenceOperationAnalyzer(bool doOperationBlockAnalysis)
            {
                _doOperationBlockAnalysis = doOperationBlockAnalysis;
            }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);
            public override void Initialize(AnalysisContext context)
            {
                if (_doOperationBlockAnalysis)
                {
                    context.RegisterOperationBlockAction(operationBlockAnalysisContext =>
                    {
                        foreach (var operationBlock in operationBlockAnalysisContext.OperationBlocks)
                        {
                            foreach (var operation in operationBlock.DescendantsAndSelf().OfType<IFieldReferenceOperation>())
                            {
                                AnalyzerFieldReferenceOperation(operation, operationBlockAnalysisContext.ReportDiagnostic);
                            }
                        }
                    });
                }
                else
                {
                    context.RegisterOperationAction(AnalyzerOperation, OperationKind.FieldReference);
                }
            }

            private static void AnalyzerOperation(OperationAnalysisContext operationAnalysisContext)
            {
                AnalyzerFieldReferenceOperation((IFieldReferenceOperation)operationAnalysisContext.Operation, operationAnalysisContext.ReportDiagnostic);
            }

            private static void AnalyzerFieldReferenceOperation(IFieldReferenceOperation operation, Action<Diagnostic> reportDiagnostic)
            {
                var diagnostic = Diagnostic.Create(Descriptor, operation.Syntax.GetLocation(), operation.Field.Name, operation.Field.ConstantValue);
                reportDiagnostic(diagnostic);
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        public sealed class MethodOrConstructorBodyOperationAnalyzer : DiagnosticAnalyzer
        {
            public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
                "ID",
                "Title",
                "Method {0}",
                "Category",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);
            public override void Initialize(AnalysisContext context)
            {
                context.RegisterOperationAction(operationContext =>
                {
                    var diagnostic = Diagnostic.Create(Descriptor, operationContext.Operation.Syntax.GetLocation(), operationContext.ContainingSymbol.Name);
                    operationContext.ReportDiagnostic(diagnostic);
                }, OperationKind.MethodBody, OperationKind.ConstructorBody);
            }
        }

        public abstract class AbstractGeneratedCodeAnalyzer<TSyntaxKind> : DiagnosticAnalyzer
            where TSyntaxKind : struct
        {
            private readonly GeneratedCodeAnalysisFlags? _generatedCodeAnalysisFlagsOpt;
            private readonly bool _testIsGeneratedCodeInCallbacks;

            public static readonly DiagnosticDescriptor Warning = new DiagnosticDescriptor(
                "GeneratedCodeAnalyzerWarning",
                "Title",
                "GeneratedCodeAnalyzerMessage for '{0}'",
                "Category",
                DiagnosticSeverity.Warning,
                true);

            public static readonly DiagnosticDescriptor Error = new DiagnosticDescriptor(
                "GeneratedCodeAnalyzerError",
                "Title",
                "GeneratedCodeAnalyzerMessage for '{0}'",
                "Category",
                DiagnosticSeverity.Error,
                true);

            public static readonly DiagnosticDescriptor Summary = new DiagnosticDescriptor(
                "GeneratedCodeAnalyzerSummary",
                "Title2",
                "GeneratedCodeAnalyzer received callbacks for: '{0}' types and '{1}' files",
                "Category",
                DiagnosticSeverity.Warning,
                true);

            public static readonly DiagnosticDescriptor Summary2 = new DiagnosticDescriptor(
                "GeneratedCodeAnalyzerSummary",
                "Title2",
                "GeneratedCodeAnalyzer received callbacks for: '{0}' types and '{1}' files and '{2}' additional IsGeneratedCode callbacks",
                "Category",
                DiagnosticSeverity.Warning,
                true);

            protected AbstractGeneratedCodeAnalyzer(GeneratedCodeAnalysisFlags? generatedCodeAnalysisFlagsOpt, bool testIsGeneratedCodeInCallbacks)
            {
                _generatedCodeAnalysisFlagsOpt = generatedCodeAnalysisFlagsOpt;
                _testIsGeneratedCodeInCallbacks = testIsGeneratedCodeInCallbacks;
            }

            protected abstract TSyntaxKind ClassDeclarationSyntaxKind { get; }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Warning, Error, Summary, Summary2);
            public override void Initialize(AnalysisContext context)
            {
                context.RegisterCompilationStartAction(this.OnCompilationStart);

                if (_generatedCodeAnalysisFlagsOpt.HasValue)
                {
                    // Configure analysis on generated code.
                    context.ConfigureGeneratedCodeAnalysis(_generatedCodeAnalysisFlagsOpt.Value);
                }
            }

            private void OnCompilationStart(CompilationStartAnalysisContext context)
            {
                var sortedCallbackSymbolNames = new SortedSet<string>();
                var sortedCallbackTreePaths = new SortedSet<string>();
                context.RegisterSymbolAction(symbolContext =>
                {
                    sortedCallbackSymbolNames.Add($"{symbolContext.Symbol.Name}(IsGeneratedCode:{symbolContext.IsGeneratedCode})");
                    ReportSymbolDiagnostics(symbolContext.Symbol, symbolContext.ReportDiagnostic);
                }, SymbolKind.NamedType);

                context.RegisterSyntaxTreeAction(treeContext =>
                {
                    sortedCallbackTreePaths.Add($"{treeContext.Tree.FilePath}(IsGeneratedCode:{treeContext.IsGeneratedCode})");
                    ReportTreeDiagnostics(treeContext.Tree, treeContext.ReportDiagnostic);
                });

                var sortedCallbackSyntaxNodeNames = new SortedSet<string>();
                var sortedCallbackOperationNames = new SortedSet<string>();
                var sortedCallbackSemanticModelPaths = new SortedSet<string>();
                var sortedCallbackSymbolStartNames = new SortedSet<string>();
                var sortedCallbackSymbolEndNames = new SortedSet<string>();
                var sortedCallbackOperationBlockStartNames = new SortedSet<string>();
                var sortedCallbackOperationBlockEndNames = new SortedSet<string>();
                var sortedCallbackOperationBlockNames = new SortedSet<string>();
                var sortedCallbackCodeBlockStartNames = new SortedSet<string>();
                var sortedCallbackCodeBlockEndNames = new SortedSet<string>();
                var sortedCallbackCodeBlockNames = new SortedSet<string>();
                if (_testIsGeneratedCodeInCallbacks)
                {
                    // Test all remaining analysis contexts that expose "IsGeneratdCode" flag
                    context.RegisterSyntaxNodeAction(context =>
                        sortedCallbackSyntaxNodeNames.Add($"{context.ContainingSymbol.Name}(IsGeneratedCode:{context.IsGeneratedCode})"),
                        ImmutableArray.Create(ClassDeclarationSyntaxKind));

                    context.RegisterSemanticModelAction(context =>
                        sortedCallbackSemanticModelPaths.Add($"{context.SemanticModel.SyntaxTree.FilePath}(IsGeneratedCode:{context.IsGeneratedCode})"));

                    context.RegisterSymbolStartAction(context =>
                    {
                        sortedCallbackSymbolStartNames.Add($"{context.Symbol.Name}(IsGeneratedCode:{context.IsGeneratedCode})");

                        context.RegisterOperationBlockStartAction(context =>
                        {
                            if (context.OwningSymbol.Kind != SymbolKind.Method ||
                                context.OperationBlocks.IsEmpty)
                            {
                                return;
                            }

                            sortedCallbackOperationBlockStartNames.Add($"{context.OwningSymbol.ContainingType.Name}(IsGeneratedCode:{context.IsGeneratedCode})");

                            context.RegisterOperationAction(context =>
                                sortedCallbackOperationNames.Add($"{context.ContainingSymbol.ContainingType.Name}(IsGeneratedCode:{context.IsGeneratedCode})"),
                                OperationKind.Invocation);

                            context.RegisterOperationBlockEndAction(context =>
                                sortedCallbackOperationBlockEndNames.Add($"{context.OwningSymbol.ContainingType.Name}(IsGeneratedCode:{context.IsGeneratedCode})"));
                        });

                        context.RegisterOperationBlockAction(context =>
                        {
                            if (context.OwningSymbol.Kind != SymbolKind.Method ||
                                context.OperationBlocks.IsEmpty)
                            {
                                return;
                            }

                            sortedCallbackOperationBlockNames.Add($"{context.OwningSymbol.ContainingType.Name}(IsGeneratedCode:{context.IsGeneratedCode})");
                        });

                        context.RegisterSymbolEndAction(context =>
                            sortedCallbackSymbolEndNames.Add($"{context.Symbol.Name}(IsGeneratedCode:{context.IsGeneratedCode})"));
                    }, SymbolKind.NamedType);

                    context.RegisterCodeBlockStartAction<TSyntaxKind>(context =>
                    {
                        if (context.OwningSymbol.Kind != SymbolKind.Method)
                        {
                            return;
                        }

                        sortedCallbackCodeBlockStartNames.Add($"{context.OwningSymbol.ContainingType.Name}(IsGeneratedCode:{context.IsGeneratedCode})");

                        context.RegisterCodeBlockEndAction(context =>
                            sortedCallbackCodeBlockEndNames.Add($"{context.OwningSymbol.ContainingType.Name}(IsGeneratedCode:{context.IsGeneratedCode})"));
                    });

                    context.RegisterCodeBlockAction(context =>
                    {
                        if (context.OwningSymbol.Kind != SymbolKind.Method)
                        {
                            return;
                        }

                        sortedCallbackCodeBlockNames.Add($"{context.OwningSymbol.ContainingType.Name}(IsGeneratedCode:{context.IsGeneratedCode})");
                    });
                }

                context.RegisterCompilationEndAction(endContext =>
                {
                    var arg1 = sortedCallbackSymbolNames.Join(",");
                    var arg2 = sortedCallbackTreePaths.Join(",");
                    var args = new object[] { arg1, arg2 };
                    var rule = Summary;

                    if (_testIsGeneratedCodeInCallbacks)
                    {
                        var arg3 = sortedCallbackSyntaxNodeNames.Join(",") + ";" +
                            sortedCallbackOperationNames.Join(",") + ";" +
                            sortedCallbackSemanticModelPaths.Join(",") + ";" +
                            sortedCallbackSymbolStartNames.Join(",") + ";" +
                            sortedCallbackSymbolEndNames.Join(",") + ";" +
                            sortedCallbackOperationBlockStartNames.Join(",") + ";" +
                            sortedCallbackOperationBlockEndNames.Join(",") + ";" +
                            sortedCallbackOperationBlockNames.Join(",") + ";" +
                            sortedCallbackCodeBlockStartNames.Join(",") + ";" +
                            sortedCallbackCodeBlockEndNames.Join(",") + ";" +
                            sortedCallbackCodeBlockNames.Join(",");
                        args = new object[] { arg1, arg2, arg3 };
                        rule = Summary2;
                    }

                    // Summary diagnostics about received callbacks.
                    var diagnostic = Diagnostic.Create(rule, Location.None, args);
                    endContext.ReportDiagnostic(diagnostic);
                });
            }

            private void ReportSymbolDiagnostics(ISymbol symbol, Action<Diagnostic> addDiagnostic)
            {
                ReportDiagnosticsCore(addDiagnostic, symbol.Locations[0], symbol.Name);
            }

            private void ReportTreeDiagnostics(SyntaxTree tree, Action<Diagnostic> addDiagnostic)
            {
                ReportDiagnosticsCore(addDiagnostic, tree.GetRoot().GetLastToken().GetLocation(), tree.FilePath);
            }

            private void ReportDiagnosticsCore(Action<Diagnostic> addDiagnostic, Location location, params object[] messageArguments)
            {
                // warning diagnostic
                var diagnostic = Diagnostic.Create(Warning, location, messageArguments);
                addDiagnostic(diagnostic);

                // error diagnostic
                diagnostic = Diagnostic.Create(Error, location, messageArguments);
                addDiagnostic(diagnostic);
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        public class GeneratedCodeAnalyzer2 : DiagnosticAnalyzer
        {
            public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
                "GeneratedCodeAnalyzer2Warning",
                "Title",
                "GeneratedCodeAnalyzer2Message for '{0}'; Total types analyzed: '{1}'",
                "Category",
                DiagnosticSeverity.Warning,
                true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);
            public override void Initialize(AnalysisContext context)
            {
                // Analyze but don't report diagnostics on generated code.
                context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);

                context.RegisterCompilationStartAction(compilationStartContext =>
                {
                    var namedTypes = new HashSet<ISymbol>();
                    compilationStartContext.RegisterSymbolAction(symbolContext => namedTypes.Add(symbolContext.Symbol), SymbolKind.NamedType);

                    compilationStartContext.RegisterCompilationEndAction(compilationEndContext =>
                    {
                        foreach (var namedType in namedTypes)
                        {
                            var diagnostic = Diagnostic.Create(Rule, namedType.Locations[0], namedType.Name, namedTypes.Count);
                            compilationEndContext.ReportDiagnostic(diagnostic);
                        }
                    });
                });
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        public class SharedStateAnalyzer : DiagnosticAnalyzer
        {
            private readonly SyntaxTreeValueProvider<bool> _treeValueProvider;
            private readonly HashSet<SyntaxTree> _treeCallbackSet;

            private readonly SourceTextValueProvider<int> _textValueProvider;
            private readonly HashSet<SourceText> _textCallbackSet;

            public static readonly DiagnosticDescriptor GeneratedCodeDescriptor = new DiagnosticDescriptor(
                "GeneratedCodeDiagnostic",
                "Title1",
                "GeneratedCodeDiagnostic {0}",
                "Category",
                DiagnosticSeverity.Warning,
                true);

            public static readonly DiagnosticDescriptor NonGeneratedCodeDescriptor = new DiagnosticDescriptor(
                "UserCodeDiagnostic",
                "Title2",
                "UserCodeDiagnostic {0}",
                "Category",
                DiagnosticSeverity.Warning,
                true);

            public static readonly DiagnosticDescriptor UniqueTextFileDescriptor = new DiagnosticDescriptor(
                "UniqueTextFileDiagnostic",
                "Title3",
                "UniqueTextFileDiagnostic {0}",
                "Category",
                DiagnosticSeverity.Warning,
                true);

            public static readonly DiagnosticDescriptor NumberOfUniqueTextFileDescriptor = new DiagnosticDescriptor(
                "NumberOfUniqueTextFileDescriptor",
                "Title4",
                "NumberOfUniqueTextFileDescriptor {0}",
                "Category",
                DiagnosticSeverity.Warning,
                true);

            public SharedStateAnalyzer()
            {
                _treeValueProvider = new SyntaxTreeValueProvider<bool>(IsGeneratedCode);
                _treeCallbackSet = new HashSet<SyntaxTree>(SyntaxTreeComparer.Instance);

                _textValueProvider = new SourceTextValueProvider<int>(GetCharacterCount);
                _textCallbackSet = new HashSet<SourceText>(SourceTextComparer.Instance);
            }

            private bool IsGeneratedCode(SyntaxTree tree)
            {
                lock (_treeCallbackSet)
                {
                    if (!_treeCallbackSet.Add(tree))
                    {
                        throw new Exception("Expected driver to make a single callback per tree");
                    }
                }

                var fileNameWithoutExtension = PathUtilities.GetFileName(tree.FilePath, includeExtension: false);
                return fileNameWithoutExtension.EndsWith(".designer", StringComparison.OrdinalIgnoreCase) ||
                    fileNameWithoutExtension.EndsWith(".generated", StringComparison.OrdinalIgnoreCase);
            }

            private int GetCharacterCount(SourceText text)
            {
                lock (_textCallbackSet)
                {
                    if (!_textCallbackSet.Add(text))
                    {
                        throw new Exception("Expected driver to make a single callback per text");
                    }
                }

                return text.Length;
            }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(GeneratedCodeDescriptor, NonGeneratedCodeDescriptor, UniqueTextFileDescriptor, NumberOfUniqueTextFileDescriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterCompilationStartAction(this.OnCompilationStart);
            }

            private void OnCompilationStart(CompilationStartAnalysisContext context)
            {
                context.RegisterSymbolAction(symbolContext =>
                {
                    var descriptor = GeneratedCodeDescriptor;
                    foreach (var location in symbolContext.Symbol.Locations)
                    {
                        context.TryGetValue(location.SourceTree, _treeValueProvider, out var isGeneratedCode);
                        if (!isGeneratedCode)
                        {
                            descriptor = NonGeneratedCodeDescriptor;
                            break;
                        }
                    }

                    var diagnostic = Diagnostic.Create(descriptor, symbolContext.Symbol.Locations[0], symbolContext.Symbol.Name);
                    symbolContext.ReportDiagnostic(diagnostic);
                }, SymbolKind.NamedType);

                context.RegisterSyntaxTreeAction(treeContext =>
                {
                    context.TryGetValue(treeContext.Tree, _treeValueProvider, out var isGeneratedCode);
                    var descriptor = isGeneratedCode ? GeneratedCodeDescriptor : NonGeneratedCodeDescriptor;

                    var diagnostic = Diagnostic.Create(descriptor, Location.None, treeContext.Tree.FilePath);
                    treeContext.ReportDiagnostic(diagnostic);

                    context.TryGetValue(treeContext.Tree.GetText(), _textValueProvider, out var length);
                    diagnostic = Diagnostic.Create(UniqueTextFileDescriptor, Location.None, treeContext.Tree.FilePath);
                    treeContext.ReportDiagnostic(diagnostic);
                });

                context.RegisterCompilationEndAction(endContext =>
                {
                    if (_treeCallbackSet.Count != endContext.Compilation.SyntaxTrees.Count())
                    {
                        throw new Exception("Expected driver to make a callback for every tree");
                    }

                    var diagnostic = Diagnostic.Create(NumberOfUniqueTextFileDescriptor, Location.None, _textCallbackSet.Count);
                    endContext.ReportDiagnostic(diagnostic);
                });
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        public class AnalyzerForParameters : DiagnosticAnalyzer
        {
            public static readonly DiagnosticDescriptor ParameterDescriptor = new DiagnosticDescriptor(
                "Parameter_ID",
                "Parameter_Title",
                "Parameter_Message",
                "Parameter_Category",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(ParameterDescriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSymbolAction(SymbolAction, SymbolKind.Parameter);
            }

            private void SymbolAction(SymbolAnalysisContext context)
            {
                context.ReportDiagnostic(Diagnostic.Create(ParameterDescriptor, context.Symbol.Locations[0]));
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        public class SymbolStartAnalyzer : DiagnosticAnalyzer
        {
            private readonly SymbolKind _symbolKind;
            private readonly bool _topLevelAction;
            private readonly OperationKind? _operationKind;
            private readonly string _analyzerId;

            public SymbolStartAnalyzer(bool topLevelAction, SymbolKind symbolKind, OperationKind? operationKindOpt = null, int? analyzerId = null)
            {
                _topLevelAction = topLevelAction;
                _symbolKind = symbolKind;
                _operationKind = operationKindOpt;
                _analyzerId = $"Analyzer{(analyzerId.HasValue ? analyzerId.Value : 1)}";
                SymbolsStarted = new ConcurrentSet<ISymbol>();
            }

            internal ConcurrentSet<ISymbol> SymbolsStarted { get; }

            public static readonly DiagnosticDescriptor SymbolStartTopLevelRule = new DiagnosticDescriptor(
                "SymbolStartTopLevelRuleId",
                "SymbolStartTopLevelRuleTitle",
                "Symbol : {0}, Analyzer: {1}",
                "Category",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            public static readonly DiagnosticDescriptor SymbolStartCompilationLevelRule = new DiagnosticDescriptor(
                "SymbolStartRuleId",
                "SymbolStartRuleTitle",
                "Symbol : {0}, Analyzer: {1}",
                "Category",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            public static readonly DiagnosticDescriptor SymbolStartedEndedDifferRule = new DiagnosticDescriptor(
                "SymbolStartedEndedDifferRuleId",
                "SymbolStartedEndedDifferRuleTitle",
                "Symbols Started: '{0}', Symbols Ended: '{1}', Analyzer: {2}",
                "Category",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            public static readonly DiagnosticDescriptor SymbolStartedOrderingRule = new DiagnosticDescriptor(
               "SymbolStartedOrderingRuleId",
               "SymbolStartedOrderingRuleTitle",
               "Member '{0}' started before container '{1}', Analyzer: {2}",
               "Category",
               defaultSeverity: DiagnosticSeverity.Warning,
               isEnabledByDefault: true);

            public static readonly DiagnosticDescriptor SymbolEndedOrderingRule = new DiagnosticDescriptor(
               "SymbolEndedOrderingRuleId",
               "SymbolEndedOrderingRuleTitle",
               "Container '{0}' ended before member '{1}', Analyzer: {2}",
               "Category",
               defaultSeverity: DiagnosticSeverity.Warning,
               isEnabledByDefault: true);

            public static readonly DiagnosticDescriptor OperationOrderingRule = new DiagnosticDescriptor(
               "OperationOrderingRuleId",
               "OperationOrderingRuleTitle",
               "Container '{0}' started after operation '{1}', Analyzer: {2}",
               "Category",
               defaultSeverity: DiagnosticSeverity.Warning,
               isEnabledByDefault: true);

            public static readonly DiagnosticDescriptor DuplicateStartActionRule = new DiagnosticDescriptor(
               "DuplicateStartActionRuleId",
               "DuplicateStartActionRuleTitle",
               "Symbol : {0}, Analyzer: {1}",
               "Category",
               defaultSeverity: DiagnosticSeverity.Warning,
               isEnabledByDefault: true);

            public static readonly DiagnosticDescriptor DuplicateEndActionRule = new DiagnosticDescriptor(
              "DuplicateEndActionRuleId",
              "DuplicateEndActionRuleTitle",
              "Symbol : {0}, Analyzer: {1}",
              "Category",
              defaultSeverity: DiagnosticSeverity.Warning,
              isEnabledByDefault: true);

            public static readonly DiagnosticDescriptor OperationRule = new DiagnosticDescriptor(
                "OperationRuleId",
                "OperationRuleTitle",
                "Symbol Started: '{0}', Owning Symbol: '{1}' Operation : {2}, Analyzer: {3}",
                "Category",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    return ImmutableArray.Create(
                        SymbolStartTopLevelRule,
                        SymbolStartCompilationLevelRule,
                        SymbolStartedEndedDifferRule,
                        SymbolStartedOrderingRule,
                        SymbolEndedOrderingRule,
                        DuplicateStartActionRule,
                        DuplicateEndActionRule,
                        OperationRule,
                        OperationOrderingRule);
                }
            }

            public override void Initialize(AnalysisContext context)
            {
                var diagnostics = new ConcurrentBag<Diagnostic>();
                var symbolsEnded = new ConcurrentSet<ISymbol>();
                var seenOperationContainers = new ConcurrentDictionary<OperationAnalysisContext, ISet<ISymbol>>();

                if (_topLevelAction)
                {
                    context.RegisterSymbolStartAction(onSymbolStart, _symbolKind);

                    context.RegisterCompilationStartAction(compilationStartContext =>
                    {
                        compilationStartContext.RegisterCompilationEndAction(compilationEndContext =>
                        {
                            reportDiagnosticsAtCompilationEnd(compilationEndContext);
                        });
                    });
                }
                else
                {
                    context.RegisterCompilationStartAction(compilationStartContext =>
                    {
                        compilationStartContext.RegisterSymbolStartAction(onSymbolStart, _symbolKind);

                        compilationStartContext.RegisterCompilationEndAction(compilationEndContext =>
                        {
                            reportDiagnosticsAtCompilationEnd(compilationEndContext);
                        });
                    });
                }

                return;

                void onSymbolStart(SymbolStartAnalysisContext symbolStartContext)
                {
                    performSymbolStartActionVerification(symbolStartContext);

                    if (_operationKind.HasValue)
                    {
                        symbolStartContext.RegisterOperationAction(operationContext =>
                        {
                            performOperationActionVerification(operationContext, symbolStartContext);
                        }, _operationKind.Value);
                    }

                    symbolStartContext.RegisterSymbolEndAction(symbolEndContext =>
                    {
                        performSymbolEndActionVerification(symbolEndContext, symbolStartContext);
                    });
                }

                void reportDiagnosticsAtCompilationEnd(CompilationAnalysisContext compilationEndContext)
                {
                    if (!SymbolsStarted.SetEquals(symbolsEnded))
                    {
                        // Symbols Started: '{0}', Symbols Ended: '{1}', Analyzer: {2}
                        var symbolsStartedStr = string.Join(", ", SymbolsStarted.Select(s => s.ToDisplayString().Order()));
                        var symbolsEndedStr = string.Join(", ", symbolsEnded.Select(s => s.ToDisplayString().Order()));
                        compilationEndContext.ReportDiagnostic(Diagnostic.Create(SymbolStartedEndedDifferRule, Location.None, symbolsStartedStr, symbolsEndedStr, _analyzerId));
                    }

                    foreach (var diagnostic in diagnostics)
                    {
                        compilationEndContext.ReportDiagnostic(diagnostic);
                    }
                }

                void performSymbolStartActionVerification(SymbolStartAnalysisContext symbolStartContext)
                {
                    verifySymbolStartOrdering(symbolStartContext);
                    verifySymbolStartAndOperationOrdering(symbolStartContext);
                    if (!SymbolsStarted.Add(symbolStartContext.Symbol))
                    {
                        diagnostics.Add(Diagnostic.Create(DuplicateStartActionRule, Location.None, symbolStartContext.Symbol.Name, _analyzerId));
                    }
                }

                void performSymbolEndActionVerification(SymbolAnalysisContext symbolEndContext, SymbolStartAnalysisContext symbolStartContext)
                {
                    Assert.Equal(symbolStartContext.Symbol, symbolEndContext.Symbol);
                    verifySymbolEndOrdering(symbolEndContext);
                    if (!symbolsEnded.Add(symbolEndContext.Symbol))
                    {
                        diagnostics.Add(Diagnostic.Create(DuplicateEndActionRule, Location.None, symbolEndContext.Symbol.Name, _analyzerId));
                    }

                    Assert.False(symbolEndContext.Symbol.IsImplicitlyDeclared);
                    var rule = _topLevelAction ? SymbolStartTopLevelRule : SymbolStartCompilationLevelRule;
                    symbolEndContext.ReportDiagnostic(Diagnostic.Create(rule, Location.None, symbolStartContext.Symbol.Name, _analyzerId));
                }

                void performOperationActionVerification(OperationAnalysisContext operationContext, SymbolStartAnalysisContext symbolStartContext)
                {
                    var containingSymbols = GetContainingSymbolsAndThis(operationContext.ContainingSymbol).ToSet();
                    seenOperationContainers.Add(operationContext, containingSymbols);
                    Assert.Contains(symbolStartContext.Symbol, containingSymbols);
                    Assert.All(containingSymbols, s => Assert.DoesNotContain(s, symbolsEnded));
                    // Symbol Started: '{0}', Owning Symbol: '{1}' Operation : {2}, Analyzer: {3}
                    operationContext.ReportDiagnostic(Diagnostic.Create(OperationRule, Location.None, symbolStartContext.Symbol.Name, operationContext.ContainingSymbol.Name, operationContext.Operation.Syntax.ToString(), _analyzerId));
                }

                IEnumerable<ISymbol> GetContainingSymbolsAndThis(ISymbol symbol)
                {
                    do
                    {
                        yield return symbol;
                        symbol = symbol.ContainingSymbol;
                    }
                    while (symbol != null && !symbol.IsImplicitlyDeclared);
                }

                void verifySymbolStartOrdering(SymbolStartAnalysisContext symbolStartContext)
                {
                    ISymbol symbolStarted = symbolStartContext.Symbol;
                    IEnumerable<ISymbol> members;
                    switch (symbolStarted)
                    {
                        case INamedTypeSymbol namedType:
                            members = namedType.GetMembers();
                            break;

                        case INamespaceSymbol namespaceSym:
                            members = namespaceSym.GetMembers();
                            break;

                        default:
                            return;
                    }

                    foreach (var member in members.Where(m => !m.IsImplicitlyDeclared))
                    {
                        if (SymbolsStarted.Contains(member))
                        {
                            // Member '{0}' started before container '{1}', Analyzer {2}
                            diagnostics.Add(Diagnostic.Create(SymbolStartedOrderingRule, Location.None, member, symbolStarted, _analyzerId));
                        }
                    }
                }

                void verifySymbolEndOrdering(SymbolAnalysisContext symbolEndContext)
                {
                    ISymbol symbolEnded = symbolEndContext.Symbol;
                    IList<ISymbol> containersToVerify = new List<ISymbol>();
                    if (symbolEnded.ContainingType != null)
                    {
                        containersToVerify.Add(symbolEnded.ContainingType);
                    }

                    if (symbolEnded.ContainingNamespace != null)
                    {
                        containersToVerify.Add(symbolEnded.ContainingNamespace);
                    }

                    foreach (var container in containersToVerify)
                    {
                        if (symbolsEnded.Contains(container))
                        {
                            // Container '{0}' ended before member '{1}', Analyzer {2}
                            diagnostics.Add(Diagnostic.Create(SymbolEndedOrderingRule, Location.None, container, symbolEnded, _analyzerId));
                        }
                    }
                }

                void verifySymbolStartAndOperationOrdering(SymbolStartAnalysisContext symbolStartContext)
                {
                    foreach (var kvp in seenOperationContainers)
                    {
                        OperationAnalysisContext operationContext = kvp.Key;
                        ISet<ISymbol> containers = kvp.Value;

                        if (containers.Contains(symbolStartContext.Symbol))
                        {
                            // Container '{0}' started after operation '{1}', Analyzer {2}
                            diagnostics.Add(Diagnostic.Create(OperationOrderingRule, Location.None, symbolStartContext.Symbol, operationContext.Operation.Syntax.ToString(), _analyzerId));
                        }
                    }
                }
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        public sealed class DiagnosticSuppressorForId : DiagnosticSuppressor
        {
            public SuppressionDescriptor SuppressionDescriptor { get; }
            public DiagnosticSuppressorForId(string suppressedDiagnosticId, string suppressionId = null)
            {
                SuppressionDescriptor = new SuppressionDescriptor(
                    id: suppressionId ?? "SPR0001",
                    suppressedDiagnosticId: suppressedDiagnosticId,
                    justification: $"Suppress {suppressedDiagnosticId}");
            }

            public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions
                => ImmutableArray.Create(SuppressionDescriptor);

            public override void ReportSuppressions(SuppressionAnalysisContext context)
            {
                foreach (var diagnostic in context.ReportedDiagnostics)
                {
                    Assert.Equal(SuppressionDescriptor.SuppressedDiagnosticId, diagnostic.Id);
                    context.ReportSuppression(Suppression.Create(SuppressionDescriptor, diagnostic));
                }
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        public sealed class DiagnosticSuppressorForMultipleIds : DiagnosticSuppressor
        {
            public DiagnosticSuppressorForMultipleIds(params string[] suppressedDiagnosticIds)
            {
                var builder = ImmutableArray.CreateBuilder<SuppressionDescriptor>();
                int i = 1;
                foreach (var suppressedDiagnosticId in suppressedDiagnosticIds)
                {
                    var descriptor = new SuppressionDescriptor(
                        id: $"SPR000{i++}",
                        suppressedDiagnosticId: suppressedDiagnosticId,
                        justification: $"Suppress {suppressedDiagnosticId}");
                    builder.Add(descriptor);
                }

                SupportedSuppressions = builder.ToImmutable();
            }

            public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; }

            public override void ReportSuppressions(SuppressionAnalysisContext context)
            {
                foreach (var diagnostic in context.ReportedDiagnostics)
                {
                    foreach (var suppressionDescriptor in SupportedSuppressions)
                    {
                        if (suppressionDescriptor.SuppressedDiagnosticId == diagnostic.Id)
                        {
                            context.ReportSuppression(Suppression.Create(suppressionDescriptor, diagnostic));
                        }
                    }
                }
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        public sealed class DiagnosticSuppressorForId_ThrowsOperationCancelledException : DiagnosticSuppressor
        {
            public CancellationTokenSource CancellationTokenSource { get; } = new CancellationTokenSource();
            public SuppressionDescriptor SuppressionDescriptor { get; }
            public DiagnosticSuppressorForId_ThrowsOperationCancelledException(string suppressedDiagnosticId)
            {
                SuppressionDescriptor = new SuppressionDescriptor(
                    id: "SPR0001",
                    suppressedDiagnosticId: suppressedDiagnosticId,
                    justification: $"Suppress {suppressedDiagnosticId}");
            }

            public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions
                => ImmutableArray.Create(SuppressionDescriptor);

            public override void ReportSuppressions(SuppressionAnalysisContext context)
            {
                CancellationTokenSource.Cancel();
                context.CancellationToken.ThrowIfCancellationRequested();
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        public sealed class DiagnosticSuppressorThrowsExceptionFromSupportedSuppressions : DiagnosticSuppressor
        {
            private readonly NotImplementedException _exception;

            public DiagnosticSuppressorThrowsExceptionFromSupportedSuppressions(NotImplementedException exception)
            {
                _exception = exception;
            }

            public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions
                => throw _exception;

            public override void ReportSuppressions(SuppressionAnalysisContext context)
            {
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        public sealed class DiagnosticSuppressorThrowsExceptionFromReportedSuppressions : DiagnosticSuppressor
        {
            private readonly SuppressionDescriptor _descriptor;
            private readonly NotImplementedException _exception;

            public DiagnosticSuppressorThrowsExceptionFromReportedSuppressions(string suppressedDiagnosticId, NotImplementedException exception)
            {
                _descriptor = new SuppressionDescriptor(
                    "SPR0001",
                    suppressedDiagnosticId,
                    $"Suppress {suppressedDiagnosticId}");
                _exception = exception;
            }

            public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions
                => ImmutableArray.Create(_descriptor);

            public override void ReportSuppressions(SuppressionAnalysisContext context)
            {
                throw _exception;
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        public sealed class DiagnosticSuppressor_UnsupportedSuppressionReported : DiagnosticSuppressor
        {
            private readonly SuppressionDescriptor _supportedDescriptor;
            private readonly SuppressionDescriptor _unsupportedDescriptor;

            public DiagnosticSuppressor_UnsupportedSuppressionReported(string suppressedDiagnosticId, string supportedSuppressionId, string unsupportedSuppressionId)
            {
                _supportedDescriptor = new SuppressionDescriptor(
                    supportedSuppressionId,
                    suppressedDiagnosticId,
                    $"Suppress {suppressedDiagnosticId}");

                _unsupportedDescriptor = new SuppressionDescriptor(
                    unsupportedSuppressionId,
                    suppressedDiagnosticId,
                    $"Suppress {suppressedDiagnosticId}");
            }

            public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions
                => ImmutableArray.Create(_supportedDescriptor);

            public override void ReportSuppressions(SuppressionAnalysisContext context)
            {
                foreach (var diagnostic in context.ReportedDiagnostics)
                {
                    Assert.Equal(_unsupportedDescriptor.SuppressedDiagnosticId, diagnostic.Id);
                    context.ReportSuppression(Suppression.Create(_unsupportedDescriptor, diagnostic));
                }
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        public sealed class DiagnosticSuppressor_InvalidDiagnosticSuppressionReported : DiagnosticSuppressor
        {
            private readonly SuppressionDescriptor _supportedDescriptor;
            private readonly SuppressionDescriptor _unsupportedDescriptor;

            public DiagnosticSuppressor_InvalidDiagnosticSuppressionReported(string suppressedDiagnosticId, string unsupportedSuppressedDiagnosticId)
            {
                _supportedDescriptor = new SuppressionDescriptor(
                    "SPR0001",
                    suppressedDiagnosticId,
                    $"Suppress {suppressedDiagnosticId}");

                _unsupportedDescriptor = new SuppressionDescriptor(
                    "SPR0002",
                    unsupportedSuppressedDiagnosticId,
                    $"Suppress {unsupportedSuppressedDiagnosticId}");
            }

            public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions
                => ImmutableArray.Create(_supportedDescriptor);

            public override void ReportSuppressions(SuppressionAnalysisContext context)
            {
                foreach (var diagnostic in context.ReportedDiagnostics)
                {
                    Assert.Equal(_supportedDescriptor.SuppressedDiagnosticId, diagnostic.Id);
                    context.ReportSuppression(Suppression.Create(_unsupportedDescriptor, diagnostic));
                }
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        public sealed class DiagnosticSuppressor_NonReportedDiagnosticCannotBeSuppressed : DiagnosticSuppressor
        {
            private readonly SuppressionDescriptor _descriptor1, _descriptor2;
            private readonly string _nonReportedDiagnosticId;

            public DiagnosticSuppressor_NonReportedDiagnosticCannotBeSuppressed(string reportedDiagnosticId, string nonReportedDiagnosticId)
            {
                _descriptor1 = new SuppressionDescriptor(
                    "SPR0001",
                    reportedDiagnosticId,
                    $"Suppress {reportedDiagnosticId}");
                _descriptor2 = new SuppressionDescriptor(
                    "SPR0002",
                    nonReportedDiagnosticId,
                    $"Suppress {nonReportedDiagnosticId}");
                _nonReportedDiagnosticId = nonReportedDiagnosticId;
            }

            public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions
                => ImmutableArray.Create(_descriptor1, _descriptor2);

            public override void ReportSuppressions(SuppressionAnalysisContext context)
            {
                var nonReportedDiagnostic = Diagnostic.Create(
                    id: _nonReportedDiagnosticId,
                    category: "Category",
                    message: "Message",
                    severity: DiagnosticSeverity.Warning,
                    defaultSeverity: DiagnosticSeverity.Warning,
                    isEnabledByDefault: true,
                    warningLevel: 1);
                context.ReportSuppression(Suppression.Create(_descriptor2, nonReportedDiagnostic));
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        public sealed class NamedTypeAnalyzer : DiagnosticAnalyzer
        {
            public enum AnalysisKind
            {
                Symbol,
                SymbolStartEnd,
                CompilationStartEnd
            }

            public const string RuleId = "ID1";
            public const string RuleCategory = "Category1";
            private readonly DiagnosticDescriptor _rule;
            private readonly AnalysisKind _analysisKind;
            private readonly GeneratedCodeAnalysisFlags _analysisFlags;
            private readonly ConcurrentSet<ISymbol> _symbolCallbacks;

            public NamedTypeAnalyzer(AnalysisKind analysisKind, GeneratedCodeAnalysisFlags analysisFlags = GeneratedCodeAnalysisFlags.None, bool configurable = true)
            {
                _analysisKind = analysisKind;
                _analysisFlags = analysisFlags;
                _symbolCallbacks = new ConcurrentSet<ISymbol>();

                var customTags = configurable ? Array.Empty<string>() : new[] { WellKnownDiagnosticTags.NotConfigurable };
                _rule = new DiagnosticDescriptor(
                    RuleId,
                    "Title1",
                    "Symbol: {0}",
                    RuleCategory,
                    defaultSeverity: DiagnosticSeverity.Warning,
                    isEnabledByDefault: true,
                    customTags: customTags);
            }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(_rule);
            public string GetSortedSymbolCallbacksString() => string.Join(", ", _symbolCallbacks.Select(s => s.Name).Order());

            public override void Initialize(AnalysisContext context)
            {
                context.ConfigureGeneratedCodeAnalysis(_analysisFlags);

                switch (_analysisKind)
                {
                    case AnalysisKind.Symbol:
                        context.RegisterSymbolAction(c =>
                            {
                                _symbolCallbacks.Add(c.Symbol);
                                ReportDiagnostic(c.Symbol, c.ReportDiagnostic);
                            }, SymbolKind.NamedType);
                        break;

                    case AnalysisKind.SymbolStartEnd:
                        context.RegisterSymbolStartAction(symbolStartContext =>
                        {
                            symbolStartContext.RegisterSymbolEndAction(symbolEndContext =>
                            {
                                _symbolCallbacks.Add(symbolEndContext.Symbol);
                                ReportDiagnostic(symbolEndContext.Symbol, symbolEndContext.ReportDiagnostic);
                            });
                        }, SymbolKind.NamedType);

                        break;

                    case AnalysisKind.CompilationStartEnd:
                        context.RegisterCompilationStartAction(compilationStartContext =>
                        {
                            compilationStartContext.RegisterSymbolAction(c =>
                            {
                                _symbolCallbacks.Add(c.Symbol);
                            }, SymbolKind.NamedType);

                            compilationStartContext.RegisterCompilationEndAction(
                                compilationEndContext => compilationEndContext.ReportDiagnostic(
                                    Diagnostic.Create(_rule, Location.None, GetSortedSymbolCallbacksString())));
                        });

                        break;
                }
            }

            private void ReportDiagnostic(ISymbol symbol, Action<Diagnostic> reportDiagnostic)
                => reportDiagnostic(Diagnostic.Create(_rule, symbol.Locations[0], symbol.Name));
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        public sealed class AnalyzerWithNoLocationDiagnostics : DiagnosticAnalyzer
        {
            public AnalyzerWithNoLocationDiagnostics(bool isEnabledByDefault)
            {
                Descriptor = new DiagnosticDescriptor(
                    "ID0001",
                    "Title1",
                    "Message1",
                    "Category1",
                    defaultSeverity: DiagnosticSeverity.Warning,
                    isEnabledByDefault);
            }

            public DiagnosticDescriptor Descriptor { get; }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);
            public override void Initialize(AnalysisContext context)
            {
                context.RegisterCompilationAction(compilationContext =>
                    compilationContext.ReportDiagnostic(Diagnostic.Create(Descriptor, Location.None)));
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        public sealed class NamedTypeAnalyzerWithConfigurableEnabledByDefault : DiagnosticAnalyzer
        {
            private readonly bool _throwOnAllNamedTypes;
            private readonly DiagnosticSeverity _reportedSeverity;

            public NamedTypeAnalyzerWithConfigurableEnabledByDefault(bool isEnabledByDefault, DiagnosticSeverity defaultSeverity, bool throwOnAllNamedTypes = false)
                : this(isEnabledByDefault, defaultSeverity, customConfigurable: false, throwOnAllNamedTypes)
            {
            }

            public NamedTypeAnalyzerWithConfigurableEnabledByDefault(bool isEnabledByDefault, DiagnosticSeverity defaultSeverity, bool customConfigurable, bool throwOnAllNamedTypes)
                : this(isEnabledByDefault, defaultSeverity, defaultSeverity, customConfigurable, throwOnAllNamedTypes)
            {
            }

            public NamedTypeAnalyzerWithConfigurableEnabledByDefault(
                bool isEnabledByDefault,
                DiagnosticSeverity defaultSeverity,
                DiagnosticSeverity reportedSeverity,
                bool customConfigurable,
                bool throwOnAllNamedTypes)
            {
                var customTags = Array.Empty<string>();
                if (customConfigurable)
                    customTags = [WellKnownDiagnosticTags.CustomSeverityConfigurable];

                Descriptor = new DiagnosticDescriptor(
                    "ID0001",
                    "Title1",
                    "Message1",
                    "Category1",
                    defaultSeverity,
                    isEnabledByDefault,
                    customTags: customTags);

                _throwOnAllNamedTypes = throwOnAllNamedTypes;
                _reportedSeverity = reportedSeverity;
            }

            public DiagnosticDescriptor Descriptor { get; }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);
            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSymbolAction(context =>
                {
                    if (_throwOnAllNamedTypes)
                    {
                        throw new NotImplementedException();
                    }

                    var diagnostic = Diagnostic.Create(Descriptor, context.Symbol.Locations[0], _reportedSeverity, additionalLocations: null, properties: null, messageArgs: null);
                    context.ReportDiagnostic(diagnostic);
                },
                SymbolKind.NamedType);
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        public sealed class RegisterOperationBlockAndOperationActionAnalyzer : DiagnosticAnalyzer
        {
            private static readonly DiagnosticDescriptor s_descriptor = new DiagnosticDescriptor(
                "ID0001",
                "Title",
                "Message",
                "Category",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_descriptor);
            public override void Initialize(AnalysisContext analysisContext)
            {
                analysisContext.RegisterOperationAction(_ => { }, OperationKind.Invocation);
                analysisContext.RegisterOperationBlockStartAction(OnOperationBlockStart);
            }

            private void OnOperationBlockStart(OperationBlockStartAnalysisContext context)
            {
                context.RegisterOperationBlockEndAction(
                    endContext => endContext.ReportDiagnostic(Diagnostic.Create(s_descriptor, context.OwningSymbol.Locations[0])));
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        public sealed class FieldAnalyzer : DiagnosticAnalyzer
        {
            private readonly bool _syntaxTreeAction;
            public FieldAnalyzer(string diagnosticId, bool syntaxTreeAction)
            {
                _syntaxTreeAction = syntaxTreeAction;
                Descriptor = new DiagnosticDescriptor(
                    diagnosticId,
                    "Title",
                    "Message",
                    "Category",
                    defaultSeverity: DiagnosticSeverity.Warning,
                    isEnabledByDefault: true);
            }

            public DiagnosticDescriptor Descriptor { get; }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                if (_syntaxTreeAction)
                {
                    context.RegisterSyntaxTreeAction(context =>
                    {
                        var fields = context.Tree.GetRoot().DescendantNodes().OfType<CSharp.Syntax.FieldDeclarationSyntax>();
                        foreach (var variable in fields.SelectMany(f => f.Declaration.Variables))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(Descriptor, variable.Identifier.GetLocation()));
                        }
                    });
                }
                else
                {
                    context.RegisterSymbolAction(
                        context => context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Symbol.Locations[0])),
                        SymbolKind.Field);
                }
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        public class AdditionalFileAnalyzer : DiagnosticAnalyzer
        {
            private readonly bool _registerFromInitialize;
            private readonly TextSpan _diagnosticSpan;

            public AdditionalFileAnalyzer(bool registerFromInitialize, TextSpan diagnosticSpan, string id = "ID0001")
            {
                _registerFromInitialize = registerFromInitialize;
                _diagnosticSpan = diagnosticSpan;

                Descriptor = new DiagnosticDescriptor(
                    id,
                    "Title1",
                    "Message1",
                    "Category1",
                    defaultSeverity: DiagnosticSeverity.Warning,
                    isEnabledByDefault: true);
            }

            public DiagnosticDescriptor Descriptor { get; }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);
            public override void Initialize(AnalysisContext context)
            {
                if (_registerFromInitialize)
                {
                    context.RegisterAdditionalFileAction(AnalyzeAdditionalFile);
                }
                else
                {
                    context.RegisterCompilationStartAction(context =>
                        context.RegisterAdditionalFileAction(AnalyzeAdditionalFile));
                }
            }

            private void AnalyzeAdditionalFile(AdditionalFileAnalysisContext context)
            {
                if (context.AdditionalFile.Path == null)
                {
                    return;
                }

                var text = context.AdditionalFile.GetText();
                var location = Location.Create(context.AdditionalFile.Path, _diagnosticSpan, text.Lines.GetLinePositionSpan(_diagnosticSpan));
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, location));
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        public sealed class RegisterSyntaxTreeCancellationAnalyzer : DiagnosticAnalyzer
        {
            public const string DiagnosticId = "ID0001";
            private static readonly DiagnosticDescriptor s_descriptor = new DiagnosticDescriptor(
                DiagnosticId,
                "Title",
                "Message",
                "Category",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);
            private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
            public CancellationToken CancellationToken => _cancellationTokenSource.Token;

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_descriptor);
            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSyntaxTreeAction(context =>
                {
                    // Mimic cancellation by throwing an OperationCanceledException in first callback.
                    if (!_cancellationTokenSource.IsCancellationRequested)
                    {
                        _cancellationTokenSource.Cancel();

                        while (true)
                        {
                            context.CancellationToken.ThrowIfCancellationRequested();
                        }

                        throw ExceptionUtilities.Unreachable();
                    }

                    context.ReportDiagnostic(Diagnostic.Create(s_descriptor, context.Tree.GetRoot().GetLocation()));
                });
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        public sealed class LocalNonLocalDiagnosticsAnalyzer : DiagnosticAnalyzer
        {
            private static readonly DiagnosticDescriptor s_descriptor = new("ID0001", "Title", "{0}", "Category", DiagnosticSeverity.Warning, isEnabledByDefault: true);
            private readonly ActionKind _actionKind;

            public enum ActionKind
            {
                SyntaxTreeAction,
                SemanticModelAction,
                SymbolAction,
                SymbolStartEndAction,
                OperationAction,
                OperationBlockAction,
                OperationBlockStartEndAction,
                SyntaxNodeAction,
                CodeBlockAction,
                CodeBlockStartEndAction
            }

            public LocalNonLocalDiagnosticsAnalyzer(ActionKind actionKind)
            {
                _actionKind = actionKind;
            }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterCompilationStartAction(AnalyzeCompilation);
            }

            private void AnalyzeCompilation(CompilationStartAnalysisContext context)
            {
                switch (_actionKind)
                {
                    case ActionKind.SyntaxTreeAction:
                        context.RegisterSyntaxTreeAction(treeContext =>
                            ReportDiagnosticsInAllTrees("RegisterSyntaxTreeAction", treeContext.Tree, treeContext.Compilation, treeContext.ReportDiagnostic));
                        break;

                    case ActionKind.SemanticModelAction:
                        context.RegisterSemanticModelAction(semanticModelContext =>
                            ReportDiagnosticsInAllTrees("RegisterSemanticModelAction", semanticModelContext.SemanticModel.SyntaxTree, semanticModelContext.SemanticModel.Compilation, semanticModelContext.ReportDiagnostic));
                        break;

                    case ActionKind.SymbolAction:
                        context.RegisterSymbolAction(symbolContext =>
                            ReportSymbolDiagnostics("RegisterSymbolAction", symbolContext.Symbol, symbolContext.Compilation, symbolContext.ReportDiagnostic),
                            SymbolKind.NamedType, SymbolKind.Method);
                        break;

                    case ActionKind.SymbolStartEndAction:
                        context.RegisterSymbolStartAction(symbolStartContext =>
                        {
                            symbolStartContext.RegisterOperationAction(operationContext =>
                                ReportDiagnostics($"RegisterOperationAction({operationContext.Operation.Syntax}) in RegisterSymbolStartAction", operationContext.Operation.Syntax.SyntaxTree, operationContext.ContainingSymbol, operationContext.ReportDiagnostic),
                                OperationKind.VariableDeclarationGroup);

                            symbolStartContext.RegisterSyntaxNodeAction(syntaxNodeContext =>
                                ReportDiagnostics($"RegisterSyntaxNodeAction({syntaxNodeContext.Node}) in RegisterSymbolStartAction", syntaxNodeContext.Node.SyntaxTree, syntaxNodeContext.ContainingSymbol, syntaxNodeContext.ReportDiagnostic),
                                SyntaxKind.LocalDeclarationStatement);

                            symbolStartContext.RegisterSymbolEndAction(symbolEndContext =>
                                ReportSymbolDiagnostics("RegisterSymbolEndAction", symbolEndContext.Symbol, symbolEndContext.Compilation, symbolEndContext.ReportDiagnostic));
                        }, SymbolKind.NamedType);
                        break;

                    case ActionKind.OperationAction:
                        context.RegisterOperationAction(operationContext =>
                            ReportDiagnostics($"RegisterOperationAction({operationContext.Operation.Syntax})", operationContext.Operation.Syntax.SyntaxTree, operationContext.ContainingSymbol, operationContext.ReportDiagnostic),
                            OperationKind.VariableDeclarationGroup);
                        break;

                    case ActionKind.OperationBlockAction:
                        context.RegisterOperationBlockAction(operationBlockContext =>
                            ReportDiagnostics("RegisterOperationBlockAction", operationBlockContext.OwningSymbol.DeclaringSyntaxReferences[0].SyntaxTree, operationBlockContext.OwningSymbol, operationBlockContext.ReportDiagnostic));
                        break;

                    case ActionKind.OperationBlockStartEndAction:
                        context.RegisterOperationBlockStartAction(operationBlockStartContext =>
                        {
                            operationBlockStartContext.RegisterOperationAction(operationContext =>
                                ReportDiagnostics($"RegisterOperationAction({operationContext.Operation.Syntax}) in RegisterOperationBlockStartAction", operationContext.Operation.Syntax.SyntaxTree, operationContext.ContainingSymbol, operationContext.ReportDiagnostic),
                                OperationKind.VariableDeclarationGroup);

                            operationBlockStartContext.RegisterOperationBlockEndAction(operationBlockEndContext =>
                                ReportDiagnostics("RegisterOperationBlockEndAction", operationBlockEndContext.OwningSymbol.DeclaringSyntaxReferences[0].SyntaxTree, operationBlockEndContext.OwningSymbol, operationBlockEndContext.ReportDiagnostic));
                        });
                        break;

                    case ActionKind.SyntaxNodeAction:
                        context.RegisterSyntaxNodeAction(syntaxNodeContext =>
                            ReportDiagnostics($"RegisterSyntaxNodeAction({syntaxNodeContext.Node})", syntaxNodeContext.Node.SyntaxTree, syntaxNodeContext.ContainingSymbol, syntaxNodeContext.ReportDiagnostic),
                            SyntaxKind.LocalDeclarationStatement);
                        break;

                    case ActionKind.CodeBlockAction:
                        context.RegisterCodeBlockAction(codeBlockContext =>
                            ReportDiagnostics("RegisterCodeBlockAction", codeBlockContext.CodeBlock.SyntaxTree, codeBlockContext.OwningSymbol, codeBlockContext.ReportDiagnostic));
                        break;

                    case ActionKind.CodeBlockStartEndAction:
                        context.RegisterCodeBlockStartAction<SyntaxKind>(codeBlockStartContext =>
                        {
                            codeBlockStartContext.RegisterSyntaxNodeAction(syntaxNodeContext =>
                                ReportDiagnostics($"RegisterSyntaxNodeAction({syntaxNodeContext.Node}) in RegisterCodeBlockStartAction", syntaxNodeContext.Node.SyntaxTree, syntaxNodeContext.ContainingSymbol, syntaxNodeContext.ReportDiagnostic),
                                SyntaxKind.LocalDeclarationStatement);

                            codeBlockStartContext.RegisterCodeBlockEndAction(codeBlockEndContext =>
                                ReportDiagnostics("RegisterCodeBlockEndAction", codeBlockEndContext.CodeBlock.SyntaxTree, codeBlockEndContext.OwningSymbol, codeBlockEndContext.ReportDiagnostic));
                        });
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(_actionKind);
                }
            }

            private static void ReportSymbolDiagnostics(string actionName, ISymbol symbol, Compilation compilation, Action<Diagnostic> reportDiagnostic)
            {
                var arg = $"{actionName}({symbol.Name})";
                var trees = symbol.DeclaringSyntaxReferences.Select(syntaxRef => syntaxRef.SyntaxTree).Distinct();
                foreach (var tree in trees)
                {
                    var index = compilation.SyntaxTrees.IndexOf(tree) + 1;
                    ReportDiagnosticsInTreeCore($"{arg}(File{index})", tree, reportDiagnostic);
                }
            }

            private static void ReportDiagnosticsInAllTrees(string actionName, SyntaxTree analyzedTree, Compilation compilation, Action<Diagnostic> reportDiagnostic)
            {
                var index = compilation.SyntaxTrees.IndexOf(analyzedTree) + 1;
                foreach (var tree in compilation.SyntaxTrees)
                {
                    ReportDiagnosticsInTreeCore($"{actionName}(File{index})", tree, reportDiagnostic);
                }
            }

            private static void ReportDiagnostics(string actionName, SyntaxTree tree, ISymbol containingSymbol, Action<Diagnostic> reportDiagnostic)
            {
                var arg = $"{actionName}({containingSymbol.Name})";
                ReportDiagnosticsInTreeCore(arg, tree, reportDiagnostic);
            }

            private static void ReportDiagnosticsInTreeCore(string arg, SyntaxTree tree, Action<Diagnostic> reportDiagnostic)
            {
                var root = tree.GetRoot();
                foreach (var localDecl in root.DescendantNodes().OfType<CSharp.Syntax.LocalDeclarationStatementSyntax>())
                {
                    reportDiagnostic(Diagnostic.Create(s_descriptor, localDecl.GetLocation(), arg));
                }
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        public sealed class VariableDeclarationAnalyzer : DiagnosticAnalyzer
        {
            private readonly bool _testSyntaxNodeAction;
            public VariableDeclarationAnalyzer(string diagnosticId, bool testSyntaxNodeAction)
            {
                _testSyntaxNodeAction = testSyntaxNodeAction;
                Descriptor = new DiagnosticDescriptor(
                    diagnosticId,
                    "Title",
                    "Message",
                    "Category",
                    defaultSeverity: DiagnosticSeverity.Warning,
                    isEnabledByDefault: true);
            }

            public DiagnosticDescriptor Descriptor { get; }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                if (_testSyntaxNodeAction)
                {
                    context.RegisterSyntaxNodeAction<SyntaxKind>(
                        context => context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Node.GetLocation())),
                        SyntaxKind.VariableDeclaration);
                }
                else
                {
                    context.RegisterOperationAction(
                        context => context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Operation.Syntax.GetLocation())),
                        OperationKind.VariableDeclaration);
                }
            }
        }

        internal enum AnalyzerRegisterActionKind
        {
            SyntaxTree,
            SyntaxNode,
            Symbol,
            Operation,
            SemanticModel,
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        internal sealed class CancellationTestAnalyzer : DiagnosticAnalyzer
        {
            public const string DiagnosticId = "DiagnosticId";
            private readonly DiagnosticDescriptor s_descriptor =
                new DiagnosticDescriptor(DiagnosticId, "test", "test", "test", DiagnosticSeverity.Warning, isEnabledByDefault: true);

            private readonly AnalyzerRegisterActionKind _actionKind;
            private readonly CancellationTokenSource _cancellationTokenSource = new();

            public CancellationTestAnalyzer(AnalyzerRegisterActionKind actionKind)
            {
                _actionKind = actionKind;
                CanceledCompilations = new ConcurrentSet<Compilation>();
            }

            public CancellationToken CancellationToken => _cancellationTokenSource.Token;
            public ConcurrentSet<Compilation> CanceledCompilations { get; }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterCompilationStartAction(OnCompilationStart);
            }

            private void OnCompilationStart(CompilationStartAnalysisContext context)
            {
                switch (_actionKind)
                {
                    case AnalyzerRegisterActionKind.SyntaxTree:
                        context.RegisterSyntaxTreeAction(syntaxContext => HandleCallback(syntaxContext.Tree.GetRoot().GetLocation(), context.Compilation, syntaxContext.ReportDiagnostic, syntaxContext.CancellationToken));
                        break;
                    case AnalyzerRegisterActionKind.SyntaxNode:
                        context.RegisterSyntaxNodeAction(context => HandleCallback(context.Node.GetLocation(), context.Compilation, context.ReportDiagnostic, context.CancellationToken), CodeAnalysis.CSharp.SyntaxKind.ClassDeclaration);
                        break;
                    case AnalyzerRegisterActionKind.Symbol:
                        context.RegisterSymbolAction(context => HandleCallback(context.Symbol.Locations[0], context.Compilation, context.ReportDiagnostic, context.CancellationToken), SymbolKind.NamedType);
                        break;
                    case AnalyzerRegisterActionKind.Operation:
                        context.RegisterOperationAction(context => HandleCallback(context.Operation.Syntax.GetLocation(), context.Compilation, context.ReportDiagnostic, context.CancellationToken), OperationKind.VariableDeclaration);
                        break;
                    case AnalyzerRegisterActionKind.SemanticModel:
                        context.RegisterSemanticModelAction(context => HandleCallback(context.SemanticModel.SyntaxTree.GetRoot().GetLocation(), context.SemanticModel.Compilation, context.ReportDiagnostic, context.CancellationToken));
                        break;
                }
            }

            private void HandleCallback(Location analysisLocation, Compilation compilation, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken)
            {
                // Mimic cancellation by throwing an OperationCanceledException in first callback.
                if (!_cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource.Cancel();
                    CanceledCompilations.Add(compilation);

                    while (true)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    throw ExceptionUtilities.Unreachable();
                }

                // Report diagnostic in the second callback.
                reportDiagnostic(Diagnostic.Create(s_descriptor, analysisLocation));
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        public sealed class AllActionsAnalyzer : DiagnosticAnalyzer
        {
            private static readonly DiagnosticDescriptor s_descriptor = new("ID0001", "Title", "Message", "Category", DiagnosticSeverity.Warning, isEnabledByDefault: true);

            private readonly bool _testSyntaxTreeAction;
            private readonly bool _testSemanticModelAction;
            private readonly bool _testSymbolStartAction;
            private readonly bool _testBlockActions;

            public readonly List<SyntaxTree> AnalyzedTrees = new();
            public readonly List<SemanticModel> AnalyzedSemanticModels = new();
            public readonly List<ISymbol> AnalyzedSymbols = new();
            public readonly List<ISymbol> AnalyzedSymbolStartSymbols = new();
            public readonly List<ISymbol> AnalyzedSymbolEndSymbols = new();
            public readonly List<IOperation> AnalyzedOperations = new();
            public readonly List<ISymbol> AnalyzedOperationBlockSymbols = new();
            public readonly List<IOperation> AnalyzedOperationsInsideOperationBlock = new();
            public readonly List<ISymbol> AnalyzedOperationBlockStartSymbols = new();
            public readonly List<ISymbol> AnalyzedOperationBlockEndSymbols = new();
            public readonly List<SyntaxNode> AnalyzedSyntaxNodes = new();
            public readonly List<ISymbol> AnalyzedCodeBlockSymbols = new();
            public readonly List<SyntaxNode> AnalyzedSyntaxNodesInsideCodeBlock = new();
            public readonly List<ISymbol> AnalyzedCodeBlockStartSymbols = new();
            public readonly List<ISymbol> AnalyzedCodeBlockEndSymbols = new();

            public AllActionsAnalyzer(bool testSyntaxTreeAction, bool testSemanticModelAction, bool testSymbolStartAction, bool testBlockActions)
            {
                _testSyntaxTreeAction = testSyntaxTreeAction;
                _testSemanticModelAction = testSemanticModelAction;
                _testSymbolStartAction = testSymbolStartAction;
                _testBlockActions = testBlockActions;
            }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterCompilationStartAction(AnalyzeCompilation);
            }

            private void AnalyzeCompilation(CompilationStartAnalysisContext context)
            {
                // Unconditionally test symbol/operation/node actions.
                context.RegisterSymbolAction(symbolContext => AnalyzedSymbols.Add(symbolContext.Symbol), SymbolKind.NamedType, SymbolKind.Method);
                context.RegisterOperationAction(operationContext => AnalyzedOperations.Add(operationContext.Operation), OperationKind.VariableDeclaration);
                context.RegisterSyntaxNodeAction(syntaxNodeContext => AnalyzedSyntaxNodes.Add(syntaxNodeContext.Node), SyntaxKind.LocalDeclarationStatement);

                if (_testSyntaxTreeAction)
                {
                    context.RegisterSyntaxTreeAction(treeContext => AnalyzedTrees.Add(treeContext.Tree));
                }

                if (_testSemanticModelAction)
                {
                    context.RegisterSemanticModelAction(semanticModelContext => AnalyzedSemanticModels.Add(semanticModelContext.SemanticModel));
                }

                if (_testSymbolStartAction)
                {
                    context.RegisterSymbolStartAction(symbolStartContext =>
                    {
                        AnalyzedSymbolStartSymbols.Add(symbolStartContext.Symbol);
                        symbolStartContext.RegisterSymbolEndAction(symbolEndContext => AnalyzedSymbolEndSymbols.Add(symbolEndContext.Symbol));
                    }, SymbolKind.NamedType);
                }

                if (_testBlockActions)
                {
                    context.RegisterOperationBlockAction(operationBlockContext => AnalyzedOperationBlockSymbols.Add(operationBlockContext.OwningSymbol));
                    context.RegisterOperationBlockStartAction(operationBlockStartContext =>
                    {
                        AnalyzedOperationBlockStartSymbols.Add(operationBlockStartContext.OwningSymbol);
                        operationBlockStartContext.RegisterOperationAction(operationContext => AnalyzedOperationsInsideOperationBlock.Add(operationContext.Operation), OperationKind.VariableDeclaration);
                        operationBlockStartContext.RegisterOperationBlockEndAction(operationBlockEndContext => AnalyzedOperationBlockEndSymbols.Add(operationBlockEndContext.OwningSymbol));
                    });
                    context.RegisterCodeBlockAction(codeBlockContext => AnalyzedCodeBlockSymbols.Add(codeBlockContext.OwningSymbol));
                    context.RegisterCodeBlockStartAction<SyntaxKind>(codeBlockStartContext =>
                    {
                        AnalyzedCodeBlockStartSymbols.Add(codeBlockStartContext.OwningSymbol);
                        codeBlockStartContext.RegisterSyntaxNodeAction(syntaxNodeContext => AnalyzedSyntaxNodesInsideCodeBlock.Add(syntaxNodeContext.Node), SyntaxKind.LocalDeclarationStatement);
                        codeBlockStartContext.RegisterCodeBlockEndAction(codeBlockEndContext => AnalyzedCodeBlockEndSymbols.Add(codeBlockEndContext.OwningSymbol));
                    });
                }
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        public sealed class FilterSpanTestAnalyzer : DiagnosticAnalyzer
        {
            public enum AnalysisKind
            {
                SyntaxTree,
                AdditionalFile,
                SemanticModel,
                Symbol,
                SymbolStart,
                Operation,
                OperationBlockStart,
                OperationBlock,
                SyntaxNode,
                CodeBlockStart,
                CodeBlock,
            }

            private static readonly DiagnosticDescriptor s_descriptor = new DiagnosticDescriptor(
                    "ID0001",
                    "Title",
                    "Message",
                    "Category",
                    defaultSeverity: DiagnosticSeverity.Warning,
                    isEnabledByDefault: true);

            private readonly AnalysisKind _analysisKind;

            public FilterSpanTestAnalyzer(AnalysisKind analysisKind)
            {
                _analysisKind = analysisKind;
            }

            public TextSpan? CallbackFilterSpan { get; private set; }
            public SyntaxTree CallbackFilterTree { get; private set; }
            public AdditionalText CallbackFilterFile { get; private set; }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_descriptor);

            public override void Initialize(AnalysisContext context)
            {
                switch (_analysisKind)
                {
                    case AnalysisKind.SyntaxTree:
                        context.RegisterSyntaxTreeAction(context =>
                        {
                            CallbackFilterSpan = context.FilterSpan;
                            CallbackFilterTree = context.Tree;
                        });
                        break;

                    case AnalysisKind.AdditionalFile:
                        context.RegisterAdditionalFileAction(context =>
                        {
                            CallbackFilterSpan = context.FilterSpan;
                            CallbackFilterFile = context.AdditionalFile;
                        });
                        break;

                    case AnalysisKind.SemanticModel:
                        context.RegisterSemanticModelAction(context =>
                        {
                            CallbackFilterSpan = context.FilterSpan;
                            CallbackFilterTree = context.FilterTree;
                        });
                        break;

                    case AnalysisKind.Symbol:
                        context.RegisterSymbolAction(context =>
                        {
                            CallbackFilterSpan = context.FilterSpan;
                            CallbackFilterTree = context.FilterTree;
                        }, SymbolKind.NamedType);
                        break;

                    case AnalysisKind.SymbolStart:
                        context.RegisterSymbolStartAction(startContext =>
                        {
                            CallbackFilterSpan = startContext.FilterSpan;
                            CallbackFilterTree = startContext.FilterTree;

                            startContext.RegisterSymbolEndAction(endContext =>
                            {
                                if (startContext.FilterTree != endContext.FilterTree)
                                    throw new InvalidOperationException("Mismatched FilterTree");

                                if (startContext.FilterSpan != endContext.FilterSpan)
                                    throw new InvalidOperationException("Mismatched FilterSpan");
                            });
                        }, SymbolKind.NamedType);
                        break;

                    case AnalysisKind.Operation:
                        context.RegisterOperationAction(context =>
                        {
                            CallbackFilterSpan = context.FilterSpan;
                            CallbackFilterTree = context.FilterTree;
                        }, OperationKind.VariableDeclarationGroup);
                        break;

                    case AnalysisKind.OperationBlockStart:
                        context.RegisterOperationBlockStartAction(startContext =>
                        {
                            CallbackFilterSpan = startContext.FilterSpan;
                            CallbackFilterTree = startContext.FilterTree;

                            startContext.RegisterOperationBlockEndAction(endContext =>
                            {
                                if (startContext.FilterTree != endContext.FilterTree)
                                    throw new InvalidOperationException("Mismatched FilterTree");

                                if (startContext.FilterSpan != endContext.FilterSpan)
                                    throw new InvalidOperationException("Mismatched FilterSpan");
                            });
                        });
                        break;

                    case AnalysisKind.OperationBlock:
                        context.RegisterOperationBlockAction(context =>
                        {
                            CallbackFilterSpan = context.FilterSpan;
                            CallbackFilterTree = context.FilterTree;
                        });
                        break;

                    case AnalysisKind.SyntaxNode:
                        context.RegisterSyntaxNodeAction<SyntaxKind>(context =>
                        {
                            CallbackFilterSpan = context.FilterSpan;
                            CallbackFilterTree = context.FilterTree;
                        }, SyntaxKind.LocalDeclarationStatement);
                        break;

                    case AnalysisKind.CodeBlockStart:
                        context.RegisterCodeBlockStartAction<SyntaxKind>(startContext =>
                        {
                            CallbackFilterSpan = startContext.FilterSpan;
                            CallbackFilterTree = startContext.FilterTree;

                            startContext.RegisterCodeBlockEndAction(endContext =>
                            {
                                if (startContext.FilterTree != endContext.FilterTree)
                                    throw new InvalidOperationException("Mismatched FilterTree");

                                if (startContext.FilterSpan != endContext.FilterSpan)
                                    throw new InvalidOperationException("Mismatched FilterSpan");
                            });
                        });
                        break;

                    case AnalysisKind.CodeBlock:
                        context.RegisterCodeBlockAction(context =>
                        {
                            CallbackFilterSpan = context.FilterSpan;
                            CallbackFilterTree = context.FilterTree;
                        });
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(_analysisKind);
                }
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        public class MinimumReportedSeverityAnalyzer : DiagnosticAnalyzer
        {
            public static readonly DiagnosticDescriptor _descriptor = new DiagnosticDescriptor(
                "ID1",
                "Title1",
                "Message1",
                "Category1",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            public DiagnosticSeverity MinimumReportedSeverity { get; private set; }
            public bool AnalyzerInvoked { get; private set; }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(_descriptor);
            public override void Initialize(AnalysisContext context)
            {
                MinimumReportedSeverity = context.MinimumReportedSeverity;
                AnalyzerInvoked = true;
            }
        }
    }
}
