// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal abstract partial class CompilerDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        private const string Origin = nameof(Origin);
        private const string Syntactic = nameof(Syntactic);
        private const string Declaration = nameof(Declaration);

        private static readonly ImmutableDictionary<string, string> s_syntactic = ImmutableDictionary<string, string>.Empty.Add(Origin, Syntactic);
        private static readonly ImmutableDictionary<string, string> s_declaration = ImmutableDictionary<string, string>.Empty.Add(Origin, Declaration);

        /// <summary>
        /// Per-compilation DiagnosticAnalyzer for compiler's syntax/semantic/compilation diagnostics.
        /// </summary>
        private class CompilationAnalyzer
        {
            private readonly Compilation _compilation;

            public CompilationAnalyzer(Compilation compilation)
            {
                _compilation = compilation;
            }

            public void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
            {
                var semanticModel = _compilation.GetSemanticModel(context.Tree);
                var diagnostics = semanticModel.GetSyntaxDiagnostics(cancellationToken: context.CancellationToken);
                ReportDiagnostics(diagnostics, context.ReportDiagnostic, IsSourceLocation, s_syntactic);
            }

            public static void AnalyzeSemanticModel(SemanticModelAnalysisContext context)
            {
                var declDiagnostics = context.SemanticModel.GetDeclarationDiagnostics(cancellationToken: context.CancellationToken);
                ReportDiagnostics(declDiagnostics, context.ReportDiagnostic, IsSourceLocation, s_declaration);

                var bodyDiagnostics = context.SemanticModel.GetMethodBodyDiagnostics(cancellationToken: context.CancellationToken);
                ReportDiagnostics(bodyDiagnostics, context.ReportDiagnostic, IsSourceLocation);
            }

            public static void AnalyzeCompilation(CompilationAnalysisContext context)
            {
                var diagnostics = context.Compilation.GetDeclarationDiagnostics(cancellationToken: context.CancellationToken);
                ReportDiagnostics(diagnostics, context.ReportDiagnostic, location => !IsSourceLocation(location), s_declaration);
            }

            private static bool IsSourceLocation(Location location)
            {
                return location is { Kind: LocationKind.SourceFile };
            }

            private static void ReportDiagnostics(
                ImmutableArray<Diagnostic> diagnostics,
                Action<Diagnostic> reportDiagnostic,
                Func<Location, bool> locationFilter,
                ImmutableDictionary<string, string> properties = null)
            {
                foreach (var diagnostic in diagnostics)
                {
                    if (locationFilter(diagnostic.Location) &&
                        diagnostic.Severity != DiagnosticSeverity.Hidden)
                    {
                        var current = properties == null ? diagnostic : new CompilerDiagnostic(diagnostic, properties);
                        reportDiagnostic(current);
                    }
                }
            }

            private class CompilerDiagnostic : Diagnostic
            {
                private readonly Diagnostic _original;
                private readonly ImmutableDictionary<string, string> _properties;

                public CompilerDiagnostic(Diagnostic original, ImmutableDictionary<string, string> properties)
                {
                    _original = original;
                    _properties = properties;
                }

#pragma warning disable RS0013 // we are delegating so it is okay here
                public override DiagnosticDescriptor Descriptor => _original.Descriptor;
#pragma warning restore RS0013 

                internal override int Code => _original.Code;
                internal override IReadOnlyList<object> Arguments => _original.Arguments;

                public override string Id => _original.Id;
                public override DiagnosticSeverity Severity => _original.Severity;
                public override int WarningLevel => _original.WarningLevel;
                public override Location Location => _original.Location;
                public override IReadOnlyList<Location> AdditionalLocations => _original.AdditionalLocations;
                public override bool IsSuppressed => _original.IsSuppressed;
                public override ImmutableDictionary<string, string> Properties => _properties;

                public override string GetMessage(IFormatProvider formatProvider = null)
                {
                    return _original.GetMessage(formatProvider);
                }

                public override bool Equals(object obj)
                {
                    return _original.Equals(obj);
                }

                public override int GetHashCode()
                {
                    return _original.GetHashCode();
                }

                public override bool Equals(Diagnostic obj)
                {
                    return _original.Equals(obj);
                }

                internal override Diagnostic WithLocation(Location location)
                {
                    return new CompilerDiagnostic(_original.WithLocation(location), _properties);
                }

                internal override Diagnostic WithSeverity(DiagnosticSeverity severity)
                {
                    return new CompilerDiagnostic(_original.WithSeverity(severity), _properties);
                }

                internal override Diagnostic WithIsSuppressed(bool isSuppressed)
                {
                    return new CompilerDiagnostic(_original.WithIsSuppressed(isSuppressed), _properties);
                }
            }
        }
    }
}
