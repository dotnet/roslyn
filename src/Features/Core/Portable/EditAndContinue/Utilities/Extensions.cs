// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal static partial class Extensions
    {
        internal static LinePositionSpan AddLineDelta(this LinePositionSpan span, int lineDelta)
            => new(new LinePosition(span.Start.Line + lineDelta, span.Start.Character), new LinePosition(span.End.Line + lineDelta, span.End.Character));

        internal static SourceFileSpan AddLineDelta(this SourceFileSpan span, int lineDelta)
            => new(span.Path, span.Span.AddLineDelta(lineDelta));

        internal static int GetLineDelta(this LinePositionSpan oldSpan, LinePositionSpan newSpan)
            => newSpan.Start.Line - oldSpan.Start.Line;

        internal static bool Contains(this LinePositionSpan container, LinePositionSpan span)
            => span.Start >= container.Start && span.End <= container.End;

        public static LinePositionSpan ToLinePositionSpan(this SourceSpan span)
            => new(new(span.StartLine, span.StartColumn), new(span.EndLine, span.EndColumn));

        public static SourceSpan ToSourceSpan(this LinePositionSpan span)
            => new(span.Start.Line, span.Start.Character, span.End.Line, span.End.Character);

        public static ActiveStatement GetStatement(this ImmutableArray<ActiveStatement> statements, int ordinal)
        {
            foreach (var item in statements)
            {
                if (item.Ordinal == ordinal)
                {
                    return item;
                }
            }

            throw ExceptionUtilities.UnexpectedValue(ordinal);
        }

        public static ActiveStatementSpan GetStatement(this ImmutableArray<ActiveStatementSpan> statements, int ordinal)
        {
            foreach (var item in statements)
            {
                if (item.Ordinal == ordinal)
                {
                    return item;
                }
            }

            throw ExceptionUtilities.UnexpectedValue(ordinal);
        }

        public static UnmappedActiveStatement GetStatement(this ImmutableArray<UnmappedActiveStatement> statements, int ordinal)
        {
            foreach (var item in statements)
            {
                if (item.Statement.Ordinal == ordinal)
                {
                    return item;
                }
            }

            throw ExceptionUtilities.UnexpectedValue(ordinal);
        }

        /// <summary>
        /// True if the project supports Edit and Continue.
        /// Only depends on the language of the project and never changes.
        /// </summary>
        public static bool SupportsEditAndContinue(this Project project)
            => project.Services.GetService<IEditAndContinueAnalyzer>() != null;

        // Note: source generated files have relative paths: https://github.com/dotnet/roslyn/issues/51998
        public static bool SupportsEditAndContinue(this TextDocumentState textDocumentState)
        {
            if (textDocumentState.Attributes.DesignTimeOnly)
            {
                return false;
            }

            if (textDocumentState is SourceGeneratedDocumentState { FilePath: not null })
            {
                return true;
            }

            if (!PathUtilities.IsAbsolute(textDocumentState.FilePath))
            {
                return false;
            }

            if (textDocumentState is DocumentState documentState)
            {
                if (!documentState.SupportsSyntaxTree)
                {
                    return false;
                }

                // WPF design time documents are added to the Workspace by the Project System as regular documents,
                // although they are not compiled into the binary.
                if (IsWpfDesignTimeOnlyDocument(textDocumentState.FilePath, documentState.LanguageServices.Language))
                {
                    return false;
                }

                // Razor generated documents are added to the Workspace by the Web Tools editor but aren't used at runtime,
                // so don't need to be considered for edit and continue.
                if (IsRazorDesignTimeOnlyDocument(textDocumentState.FilePath))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsWpfDesignTimeOnlyDocument(string filePath, string language)
            => language switch
            {
                LanguageNames.CSharp => filePath.EndsWith(".g.i.cs", StringComparison.OrdinalIgnoreCase),
                LanguageNames.VisualBasic => filePath.EndsWith(".g.i.vb", StringComparison.OrdinalIgnoreCase),
                _ => false
            };

        private static bool IsRazorDesignTimeOnlyDocument(string filePath)
            => filePath.EndsWith(".razor.g.cs", StringComparison.OrdinalIgnoreCase) ||
                filePath.EndsWith(".cshtml.g.cs", StringComparison.OrdinalIgnoreCase);

        public static ManagedHotReloadDiagnostic ToHotReloadDiagnostic(this DiagnosticData data, ModuleUpdateStatus updateStatus)
        {
            var fileSpan = data.DataLocation.MappedFileSpan;

            return new(
                data.Id,
                data.Message ?? FeaturesResources.Unknown_error_occurred,
                updateStatus == ModuleUpdateStatus.RestartRequired ?
                    ManagedHotReloadDiagnosticSeverity.RestartRequired :
                    (data.Severity == DiagnosticSeverity.Error) ?
                        ManagedHotReloadDiagnosticSeverity.Error :
                        ManagedHotReloadDiagnosticSeverity.Warning,
                fileSpan.Path ?? "",
                fileSpan.Span.ToSourceSpan());
        }
    }
}
