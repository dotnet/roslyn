// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
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
        public sealed class EnsureNoMergedNamespaceSymbolAnalyzer : DiagnosticAnalyzer
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
                        oc.RegisterOperationAction(c => ReportDiagnostic(c.ReportDiagnostic, ActionKind.Operation), Semantics.OperationKind.VariableDeclarationStatement);
                        oc.RegisterOperationBlockEndAction(c => ReportDiagnostic(c.ReportDiagnostic, ActionKind.OperationBlockEnd));
                    });
                });

                context.RegisterSyntaxTreeAction(c => ReportDiagnostic(c.ReportDiagnostic, ActionKind.SyntaxTree));
                context.RegisterCompilationAction(cc => ReportDiagnostic(cc.ReportDiagnostic, ActionKind.Compilation));
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
        /// bug in the analyzer driver as this analyzer doesn't invoke <see cref="AnalysisContext.RegisterConcurrentExecution"/>.
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
                    context.RegisterOperationAction(c => ReportDiagnostic(c.ReportDiagnostic, c.Operation.Syntax.GetLocation()), Semantics.OperationKind.VariableDeclarationStatement);
                }
                else
                {
                    context.RegisterOperationBlockAction(c => ReportDiagnostic(c.ReportDiagnostic, c.OwningSymbol.Locations[0]));
                }
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
                        bool isGeneratedCode;
                        context.TryGetValue(location.SourceTree, _treeValueProvider, out isGeneratedCode);
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
                    bool isGeneratedCode;
                    context.TryGetValue(treeContext.Tree, _treeValueProvider, out isGeneratedCode);
                    var descriptor = isGeneratedCode ? GeneratedCodeDescriptor : NonGeneratedCodeDescriptor;

                    var diagnostic = Diagnostic.Create(descriptor, Location.None, treeContext.Tree.FilePath);
                    treeContext.ReportDiagnostic(diagnostic);

                    int length;
                    context.TryGetValue(treeContext.Tree.GetText(), _textValueProvider, out length);
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
    }
}