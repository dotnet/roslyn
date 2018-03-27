﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
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

            public static string GetExpectedErrorLogResultsText(Compilation compilation)
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

            public static string GetUriForPath(string path)
            {
                return new Uri(path, UriKind.RelativeOrAbsolute).ToString();
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

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);
            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSymbolAction(_ => { }, SymbolKind.NamedType);
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
        public sealed class AnalyzerWithCSharpCompilerDiagnosticId : DiagnosticAnalyzer
        {
            public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
                "CS101",
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
                "BC101",
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
            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);
            public override void Initialize(AnalysisContext context)
                => context.RegisterSyntaxTreeAction(c => c.ReportDiagnostic(Diagnostic.Create(Descriptor, SourceLocation.Create(c.Tree, _badSpan))));
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        public sealed class AnalyzerWithInvalidDiagnosticLocation : DiagnosticAnalyzer
        {
            private readonly Location _invalidLocation;
            private readonly ActionKind _actionKind;

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

            public AnalyzerWithInvalidDiagnosticLocation(SyntaxTree treeInAnotherCompilation, ActionKind actionKind)
            {
                _invalidLocation = treeInAnotherCompilation.GetRoot().GetLocation();
                _actionKind = actionKind;
            }

            private void ReportDiagnostic(Action<Diagnostic> addDiagnostic, ActionKind actionKindBeingRun)
            {
                if (_actionKind == actionKindBeingRun)
                {
                    var diagnostic = Diagnostic.Create(Descriptor, _invalidLocation);
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
        public sealed class AnalyzerWithAsyncMethodRegistration : DiagnosticAnalyzer
        {
            public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
                "ID",
                "Title1",
                "Message1",
                "Category1",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);
            public override void Initialize(AnalysisContext context)
            {
                context.RegisterCompilationAction(AsyncAction);
            }

            private async void AsyncAction(CompilationAnalysisContext context)
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, Location.None));
                await Task.FromResult(true);
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        public sealed class AnalyzerWithAsyncLambdaRegistration : DiagnosticAnalyzer
        {
            public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
                "ID",
                "Title1",
                "Message1",
                "Category1",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);
            public override void Initialize(AnalysisContext context)
            {
                context.RegisterCompilationAction(async (compilationContext) =>
                {
                    compilationContext.ReportDiagnostic(Diagnostic.Create(Descriptor, Location.None));
                    await Task.FromResult(true);
                });
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
                OperationBlock,
                OperationBlockEnd
            }

            public OperationAnalyzer(ActionKind actionKind)
            {
                _actionKind = actionKind;
            }

            private void ReportDiagnostic(Action<Diagnostic> addDiagnostic, Location location)
            {
                var diagnostic = Diagnostic.Create(Descriptor, location, _actionKind);
                addDiagnostic(diagnostic);
            }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);
            public override void Initialize(AnalysisContext context)
            {
                if (_actionKind == ActionKind.OperationBlockEnd)
                {
                    context.RegisterOperationBlockStartAction(oc =>
                    {
                        oc.RegisterOperationBlockEndAction(c => ReportDiagnostic(c.ReportDiagnostic, c.OwningSymbol.Locations[0]));
                    });
                }
                else if (_actionKind == ActionKind.Operation)
                {
                    context.RegisterOperationAction(c => ReportDiagnostic(c.ReportDiagnostic, c.Operation.Syntax.GetLocation()), OperationKind.VariableDeclarationGroup);
                }
                else
                {
                    context.RegisterOperationBlockAction(c => ReportDiagnostic(c.ReportDiagnostic, c.OwningSymbol.Locations[0]));
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
                }, OperationKind.MethodBodyOperation, OperationKind.ConstructorBodyOperation);
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        public class GeneratedCodeAnalyzer : DiagnosticAnalyzer
        {
            private readonly GeneratedCodeAnalysisFlags? _generatedCodeAnalysisFlagsOpt;

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

            public GeneratedCodeAnalyzer(GeneratedCodeAnalysisFlags? generatedCodeAnalysisFlagsOpt)
            {
                _generatedCodeAnalysisFlagsOpt = generatedCodeAnalysisFlagsOpt;
            }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Warning, Error, Summary);
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
                    sortedCallbackSymbolNames.Add(symbolContext.Symbol.Name);
                    ReportSymbolDiagnostics(symbolContext.Symbol, symbolContext.ReportDiagnostic);
                }, SymbolKind.NamedType);

                context.RegisterSyntaxTreeAction(treeContext =>
                {
                    sortedCallbackTreePaths.Add(treeContext.Tree.FilePath);
                    ReportTreeDiagnostics(treeContext.Tree, treeContext.ReportDiagnostic);
                });

                context.RegisterCompilationEndAction(endContext =>
                {
                    var arg1 = sortedCallbackSymbolNames.Join(",");
                    var arg2 = sortedCallbackTreePaths.Join(",");

                    // Summary diagnostics about received callbacks.
                    var diagnostic = Diagnostic.Create(Summary, Location.None, arg1, arg2);
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
    }
}
