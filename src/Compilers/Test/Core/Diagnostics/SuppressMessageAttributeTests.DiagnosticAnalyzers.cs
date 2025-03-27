// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UnitTests.Diagnostics
{
    public partial class SuppressMessageAttributeTests
    {
        protected const string TestDiagnosticCategory = "Test";
        protected const string TestDiagnosticMessageTemplate = "{0}";

        protected class WarningOnCompilationEndedAnalyzer : DiagnosticAnalyzer
        {
            public const string Id = "CompilationEnded";
            private static readonly DiagnosticDescriptor s_rule = GetRule(Id);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    return ImmutableArray.Create(s_rule);
                }
            }

            public override void Initialize(AnalysisContext analysisContext)
            {
                analysisContext.RegisterCompilationAction(
                    (context) =>
                        {
                            context.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(s_rule, Location.None, messageArgs: Id));
                        }
                    );
            }
        }

        // Produces a warning on the declaration of any symbol whose name starts with a specified prefix
        protected class WarningOnNamePrefixDeclarationAnalyzer : DiagnosticAnalyzer
        {
            public const string Id = "Declaration";
            private static readonly DiagnosticDescriptor s_rule = GetRule(Id);

            private readonly string _errorSymbolPrefix;

            public WarningOnNamePrefixDeclarationAnalyzer(string errorSymbolPrefix)
            {
                _errorSymbolPrefix = errorSymbolPrefix;
            }

            public override void Initialize(AnalysisContext analysisContext)
            {
                analysisContext.RegisterSymbolAction(
                    (context) =>
                        {
                            if (context.Symbol.Name.StartsWith(_errorSymbolPrefix, StringComparison.Ordinal))
                            {
                                context.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(s_rule, context.Symbol.Locations.First(), messageArgs: context.Symbol.Name));
                            }
                        },
                    SymbolKind.Event,
                    SymbolKind.Field,
                    SymbolKind.Method,
                    SymbolKind.NamedType,
                    SymbolKind.Namespace,
                    SymbolKind.Property);
            }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    return ImmutableArray.Create(s_rule);
                }
            }
        }

        // Produces a warning on the declaration of any named type
        protected class WarningOnTypeDeclarationAnalyzer : DiagnosticAnalyzer
        {
            public const string TypeId = "TypeDeclaration";
            private static readonly DiagnosticDescriptor s_rule = GetRule(TypeId);

            public override void Initialize(AnalysisContext analysisContext)
            {
                analysisContext.RegisterSymbolAction(
                    (context) =>
                        {
                            context.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(s_rule, context.Symbol.Locations.First(), messageArgs: context.Symbol.Name));
                        },
                    SymbolKind.NamedType);
            }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    return ImmutableArray.Create(s_rule);
                }
            }
        }

        // Produces a warning for the end of every code body and every invocation expression within that code body
        protected class WarningOnCodeBodyAnalyzer : DiagnosticAnalyzer
        {
            public const string Id = "CodeBody";
            private static readonly DiagnosticDescriptor s_rule = GetRule(Id);

            private readonly string _language;

            public WarningOnCodeBodyAnalyzer(string language)
            {
                _language = language;
            }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    return ImmutableArray.Create(s_rule);
                }
            }

            public override void Initialize(AnalysisContext analysisContext)
            {
                if (_language == LanguageNames.CSharp)
                {
                    analysisContext.RegisterCodeBlockStartAction<CSharp.SyntaxKind>(new CSharpCodeBodyAnalyzer().Initialize);
                }
                else
                {
                    analysisContext.RegisterCodeBlockStartAction<VisualBasic.SyntaxKind>(new BasicCodeBodyAnalyzer().Initialize);
                }
            }

            protected class CSharpCodeBodyAnalyzer
            {
                public void Initialize(CodeBlockStartAnalysisContext<CSharp.SyntaxKind> analysisContext)
                {
                    analysisContext.RegisterCodeBlockEndAction(
                        (context) =>
                            {
                                context.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(s_rule, context.OwningSymbol.Locations.First(), messageArgs: context.OwningSymbol.Name + ":end"));
                            });

                    analysisContext.RegisterSyntaxNodeAction(
                        (context) =>
                            {
                                context.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(s_rule, context.Node.GetLocation(), messageArgs: context.Node.ToFullString()));
                            },
                        CSharp.SyntaxKind.InvocationExpression);
                }
            }

            protected class BasicCodeBodyAnalyzer
            {
                public void Initialize(CodeBlockStartAnalysisContext<VisualBasic.SyntaxKind> analysisContext)
                {
                    analysisContext.RegisterCodeBlockEndAction(
                        (context) =>
                            {
                                context.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(s_rule, context.OwningSymbol.Locations.First(), messageArgs: context.OwningSymbol.Name + ":end"));
                            });

                    analysisContext.RegisterSyntaxNodeAction(
                        (context) =>
                            {
                                context.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(s_rule, context.Node.GetLocation(), messageArgs: context.Node.ToFullString()));
                            },
                        VisualBasic.SyntaxKind.InvocationExpression);
                }
            }
        }

        // Produces a warning for each comment trivium in a syntax tree
        protected class WarningOnCommentAnalyzer : DiagnosticAnalyzer
        {
            public const string Id = "Comment";
            private static readonly DiagnosticDescriptor s_rule = GetRule(Id);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    return ImmutableArray.Create(s_rule);
                }
            }

            public override void Initialize(AnalysisContext analysisContext)
            {
                analysisContext.RegisterSyntaxTreeAction(
                    (context) =>
                        {
                            var comments = context.Tree.GetRoot().DescendantTrivia()
                               .Where(t =>
                                   t.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
                                   t.IsKind(SyntaxKind.MultiLineCommentTrivia) ||
                                   t.IsKind(VisualBasic.SyntaxKind.CommentTrivia));

                            foreach (var comment in comments)
                            {
                                context.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(s_rule, comment.GetLocation(), messageArgs: comment.ToFullString()));
                            }
                        });
            }
        }

        // Produces a warning for each token overlapping the given span in a syntax tree
        protected class WarningOnTokenAnalyzer : DiagnosticAnalyzer
        {
            public const string Id = "Token";
            private static readonly DiagnosticDescriptor s_rule = GetRule(Id);
            private readonly IList<TextSpan> _spans;

            public WarningOnTokenAnalyzer(IList<TextSpan> spans)
            {
                _spans = spans;
            }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    return ImmutableArray.Create(s_rule);
                }
            }

            public override void Initialize(AnalysisContext analysisContext)
            {
                analysisContext.RegisterSyntaxTreeAction(
                    (context) =>
                        {
                            foreach (var nodeOrToken in context.Tree.GetRoot().DescendantNodesAndTokens())
                            {
                                if (nodeOrToken.IsToken && _spans.Any(s => s.OverlapsWith(nodeOrToken.FullSpan)))
                                {
                                    context.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(s_rule, nodeOrToken.GetLocation(), messageArgs: nodeOrToken.ToString()));
                                }
                            }
                        });
            }
        }

        // Throws an exception on every AnalyzeSymbol on named types
        protected class ThrowExceptionForEachNamedTypeAnalyzer : DiagnosticAnalyzer
        {
            public const string Id = "ThrowException";
            private static readonly DiagnosticDescriptor s_rule = GetRule(Id);
            private readonly ExceptionDispatchInfo _exceptionDispatchInfo;

            public ThrowExceptionForEachNamedTypeAnalyzer(ExceptionDispatchInfo exceptionDispatchInfo)
            {
                _exceptionDispatchInfo = exceptionDispatchInfo;
            }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    return ImmutableArray.Create(s_rule);
                }
            }

            public string AssemblyName { get; private set; }

            public override void Initialize(AnalysisContext analysisContext)
            {
                analysisContext.RegisterCompilationStartAction(context => AssemblyName = context.Compilation.AssemblyName);

                analysisContext.RegisterSymbolAction(
                    (context) =>
                    {
                        _exceptionDispatchInfo.Throw();
                    },
                    SymbolKind.NamedType);
            }
        }

        protected class ThrowExceptionFromSupportedDiagnostics : DiagnosticAnalyzer
        {
            private readonly Exception _exception;

            public ThrowExceptionFromSupportedDiagnostics(Exception exception)
            {
                _exception = exception;
            }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    ExceptionDispatchInfo.Capture(_exception).Throw();
                    throw ExceptionUtilities.Unreachable();
                }
            }

            public override void Initialize(AnalysisContext analysisContext)
                => throw ExceptionUtilities.Unreachable();
        }

        protected static DiagnosticDescriptor GetRule(string id)
        {
            return new DiagnosticDescriptor(
                id,
                id,
                TestDiagnosticMessageTemplate,
                TestDiagnosticCategory,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);
        }
    }
}
