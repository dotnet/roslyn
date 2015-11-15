﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;
using System.IO;

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
                context.RegisterCompilationAction(compilationContext =>
                {
                    // With location diagnostic.
                    var location = compilationContext.Compilation.SyntaxTrees.First().GetRoot().GetLocation();
                    compilationContext.ReportDiagnostic(Diagnostic.Create(Descriptor1, location, s_properties));

                    // No location diagnostic.
                    compilationContext.ReportDiagnostic(Diagnostic.Create(Descriptor2, Location.None, s_properties));
                });
            }

            private static string GetExpectedPropertiesMapText()
            {
                var expectedText = "";

                foreach (var kvp in s_properties.OrderBy(kvp => kvp.Key))
                {
                    expectedText += ",";
                    expectedText += string.Format(@"
            ""customProperties.{0}"": ""{1}""", kvp.Key, kvp.Value);
                }

                return expectedText;
            }

            public static string GetExpectedErrorLogIssuesText(Compilation compilation)
            {
                var tree = compilation.SyntaxTrees.First();
                var root = tree.GetRoot();
                var expectedLineSpan = root.GetLocation().GetLineSpan();
                var filePath = GetEscapedUriForPath(tree.FilePath);

                return @"
      ""issues"": [
        {
          ""ruleId"": """ + Descriptor1.Id + @""",
          ""locations"": [
            {
              ""analysisTarget"": [
                {
                  ""uri"": """ + filePath + @""",
                  ""region"": {
                    ""startLine"": " + (expectedLineSpan.StartLinePosition.Line + 1) + @",
                    ""startColumn"": " + (expectedLineSpan.StartLinePosition.Character + 1) + @",
                    ""endLine"": " + (expectedLineSpan.EndLinePosition.Line + 1) + @",
                    ""endColumn"": " + (expectedLineSpan.EndLinePosition.Character + 1) + @"
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
            ""isSuppressedInSource"": ""False"",
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
            ""isSuppressedInSource"": ""False"",
            ""customTags"": """ + Descriptor2.CustomTags.Join(";") + @"""" +
            GetExpectedPropertiesMapText() + @"
          }
        }
      ]
    }
  ]
}";
            }

            public static string GetEscapedUriForPath(string path)
            {
                return new Uri(path, UriKind.RelativeOrAbsolute).ToString().Replace("/", "\\/");
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
            public const string DiagnosticId = "DiagnosticId";
            public const string Title = "Title";
            public const string Message = "Message";
            public const string Category = "Category";
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
            public const string DiagnosticId = "DiagnosticId";
            public const string Title = "Title";
            public const string Message = "Message";
            public const string Category = "Category";
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
        public class HiddenDiagnosticsCompilationAnalyzer : DiagnosticAnalyzer
        {
            public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
                "ID1000",
                "Description1",
                string.Empty,
                "Analysis",
                DiagnosticSeverity.Hidden,
                true,
                customTags: WellKnownDiagnosticTags.NotConfigurable);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterCompilationAction(this.OnCompilation);
            }

            private void OnCompilation(CompilationAnalysisContext context)
            {
                // Report the hidden diagnostic on all trees in compilation.
                foreach (var tree in context.Compilation.SyntaxTrees)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, tree.GetRoot().GetLocation()));
                }
            }
        }
    }
}