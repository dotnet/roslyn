// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static class Extensions
    {
        public static readonly CultureInfo s_USCultureInfo = new CultureInfo("en-US");

        public static string GetBingHelpMessage(this Diagnostic diagnostic, Workspace workspace = null)
        {
            var option = GetCustomTypeInBingSearchOption(workspace);

            // We use the ENU version of the message for bing search.
            return option ? diagnostic.GetMessage(s_USCultureInfo) : diagnostic.Descriptor.GetBingHelpMessage();
        }

        public static string GetBingHelpMessage(this DiagnosticDescriptor descriptor)
        {
            // We use the ENU version of the message for bing search.
            return descriptor.MessageFormat.ToString(s_USCultureInfo);
        }

        private static bool GetCustomTypeInBingSearchOption(Workspace workspace)
        {
            var workspaceForOptions = workspace ?? PrimaryWorkspace.Workspace;
            if (workspaceForOptions == null)
            {
                return false;
            }

            return workspaceForOptions.Options.GetOption(InternalDiagnosticsOptions.PutCustomTypeInBingSearch);
        }

        public static DiagnosticData GetPrimaryDiagnosticData(this CodeFix fix)
        {
            return fix.PrimaryDiagnostic.ToDiagnosticData(fix.Project);
        }

        public static ImmutableArray<DiagnosticData> GetDiagnosticData(this CodeFix fix)
        {
            return fix.Diagnostics.SelectAsArray(d => d.ToDiagnosticData(fix.Project));
        }

        public static DiagnosticData ToDiagnosticData(this Diagnostic diagnostic, Project project)
        {
            if (diagnostic.Location.IsInSource)
            {
                return DiagnosticData.Create(project.GetDocument(diagnostic.Location.SourceTree), diagnostic);
            }

            if (diagnostic.Location.Kind == LocationKind.ExternalFile)
            {
                var document = project.Documents.FirstOrDefault(d => d.FilePath == diagnostic.Location.GetLineSpan().Path);
                if (document != null)
                {
                    return DiagnosticData.Create(document, diagnostic);
                }
            }

            return DiagnosticData.Create(project, diagnostic);
        }

        public static async Task<IEnumerable<Diagnostic>> ToDiagnosticsAsync(this IEnumerable<DiagnosticData> diagnostics, Project project, CancellationToken cancellationToken)
        {
            var result = new List<Diagnostic>();
            foreach (var diagnostic in diagnostics)
            {
                result.Add(await diagnostic.ToDiagnosticAsync(project, cancellationToken).ConfigureAwait(false));
            }

            return result;
        }

        public static async Task<IList<Location>> ConvertLocationsAsync(
            this IReadOnlyCollection<DiagnosticDataLocation> locations, Project project, CancellationToken cancellationToken)
        {
            if (locations == null || locations.Count == 0)
            {
                return SpecializedCollections.EmptyList<Location>();
            }

            var result = new List<Location>();
            foreach (var data in locations)
            {
                var location = await data.ConvertLocationAsync(project, cancellationToken).ConfigureAwait(false);
                result.Add(location);
            }

            return result;
        }

        public static async Task<Location> ConvertLocationAsync(
            this DiagnosticDataLocation dataLocation, Project project, CancellationToken cancellationToken)
        {
            if (dataLocation?.DocumentId == null)
            {
                return Location.None;
            }

            var document = project.GetDocument(dataLocation?.DocumentId);
            if (document == null)
            {
                return Location.None;
            }


            if (document.SupportsSyntaxTree)
            {
                var syntacticDocument = await SyntacticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);
                return dataLocation.ConvertLocation(syntacticDocument);
            }

            return dataLocation.ConvertLocation();
        }

        public static Location ConvertLocation(
            this DiagnosticDataLocation dataLocation, SyntacticDocument document = null)
        {
            if (dataLocation?.DocumentId == null)
            {
                return Location.None;
            }

            if (document == null)
            {
                if (dataLocation?.OriginalFilePath == null || dataLocation.SourceSpan == null)
                {
                    return Location.None;
                }

                var span = dataLocation.SourceSpan.Value;
                return Location.Create(dataLocation?.OriginalFilePath, span, new LinePositionSpan(
                    new LinePosition(dataLocation.OriginalStartLine, dataLocation.OriginalStartColumn),
                    new LinePosition(dataLocation.OriginalEndLine, dataLocation.OriginalEndColumn)));
            }

            Contract.ThrowIfFalse(dataLocation.DocumentId == document.Document.Id);

            var syntaxTree = document.SyntaxTree;
            return syntaxTree.GetLocation(dataLocation.SourceSpan ?? DiagnosticData.GetTextSpan(dataLocation, document.Text));
        }
    }
}
