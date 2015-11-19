// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal sealed class DiagnosticDataLocation
    {
        public readonly DocumentId DocumentId;

        // text can be either given or calculated from original line/column
        public readonly TextSpan? SourceSpan;

        /// <summary>
        /// Null if path is not mapped and <see cref="OriginalFilePath"/> contains the actual path.
        /// Note that the value might be a relative path. In that case <see cref="OriginalFilePath"/> should be used
        /// as a base path for path resolution.
        /// </summary>
        public readonly string MappedFilePath;
        public readonly int MappedStartLine;
        public readonly int MappedStartColumn;
        public readonly int MappedEndLine;
        public readonly int MappedEndColumn;
        public readonly string OriginalFilePath;
        public readonly int OriginalStartLine;
        public readonly int OriginalStartColumn;
        public readonly int OriginalEndLine;
        public readonly int OriginalEndColumn;

        public DiagnosticDataLocation(
            DocumentId documentId = null,
            TextSpan? sourceSpan = null,
            string originalFilePath = null,
            int originalStartLine = 0,
            int originalStartColumn = 0,
            int originalEndLine = 0,
            int originalEndColumn = 0,
            string mappedFilePath = null,
            int mappedStartLine = 0,
            int mappedStartColumn = 0,
            int mappedEndLine = 0,
            int mappedEndColumn = 0)
        {
            DocumentId = documentId;
            SourceSpan = sourceSpan;
            MappedFilePath = mappedFilePath;
            MappedStartLine = mappedStartLine;
            MappedStartColumn = mappedStartColumn;
            MappedEndLine = mappedEndLine;
            MappedEndColumn = mappedEndColumn;
            OriginalFilePath = originalFilePath;
            OriginalStartLine = originalStartLine;
            OriginalStartColumn = originalStartColumn;
            OriginalEndLine = originalEndLine;
            OriginalEndColumn = originalEndColumn;
        }

        internal DiagnosticDataLocation WithCalculatedSpan(TextSpan newSourceSpan)
        {
            Contract.ThrowIfTrue(this.SourceSpan.HasValue);

            return new DiagnosticDataLocation(this.DocumentId,
                newSourceSpan, this.OriginalFilePath,
                this.OriginalStartLine, this.OriginalStartColumn,
                this.OriginalEndLine, this.OriginalEndColumn,
                this.MappedFilePath, this.MappedStartLine, this.MappedStartColumn,
                this.MappedEndLine, this.MappedEndColumn);
        }
    }

    internal sealed class DiagnosticData
    {
        private static readonly ImmutableDictionary<string, string> s_Properties = ImmutableDictionary<string, string>.Empty.Add(WellKnownDiagnosticPropertyNames.Origin, WellKnownDiagnosticTags.Build);

        public readonly string Id;
        public readonly string Category;

        public readonly string Message;
        public readonly string Description;
        public readonly string Title;
        public readonly string HelpLink;
        public readonly DiagnosticSeverity Severity;
        public readonly DiagnosticSeverity DefaultSeverity;
        public readonly bool IsEnabledByDefault;
        public readonly int WarningLevel;
        public readonly IReadOnlyList<string> CustomTags;
        public readonly ImmutableDictionary<string, string> Properties;
        public readonly bool IsSuppressed;

        public readonly string ENUMessageForBingSearch;

        public readonly Workspace Workspace;
        public readonly ProjectId ProjectId;
        public DocumentId DocumentId => this.DataLocation?.DocumentId;

        public readonly DiagnosticDataLocation DataLocation;
        public readonly IReadOnlyCollection<DiagnosticDataLocation> AdditionalLocations;

        public DiagnosticData(
            string id,
            string category,
            string message,
            string enuMessageForBingSearch,
            DiagnosticSeverity severity,
            bool isEnabledByDefault,
            int warningLevel,
            Workspace workspace,
            ProjectId projectId,
            DiagnosticDataLocation location = null,
            IReadOnlyCollection<DiagnosticDataLocation> additionalLocations = null,
            string title = null,
            string description = null,
            string helpLink = null,
            bool isSuppressed = false,
            IReadOnlyList<string> customTags = null,
            ImmutableDictionary<string, string> properties = null) :
                this(
                    id, category, message, enuMessageForBingSearch,
                    severity, severity, isEnabledByDefault, warningLevel,
                    customTags ?? ImmutableArray<string>.Empty, properties ?? ImmutableDictionary<string, string>.Empty,
                    workspace, projectId, location, additionalLocations, title, description, helpLink, isSuppressed)
        {
        }

        public DiagnosticData(
            string id,
            string category,
            string message,
            string enuMessageForBingSearch,
            DiagnosticSeverity severity,
            DiagnosticSeverity defaultSeverity,
            bool isEnabledByDefault,
            int warningLevel,
            IReadOnlyList<string> customTags,
            ImmutableDictionary<string, string> properties,
            Workspace workspace,
            ProjectId projectId,
            DiagnosticDataLocation location = null,
            IReadOnlyCollection<DiagnosticDataLocation> additionalLocations = null,
            string title = null,
            string description = null,
            string helpLink = null,
            bool isSuppressed = false)
        {
            this.Id = id;
            this.Category = category;
            this.Message = message;
            this.ENUMessageForBingSearch = enuMessageForBingSearch;

            this.Severity = severity;
            this.DefaultSeverity = defaultSeverity;
            this.IsEnabledByDefault = isEnabledByDefault;
            this.WarningLevel = warningLevel;
            this.CustomTags = customTags;
            this.Properties = properties;

            this.Workspace = workspace;
            this.ProjectId = projectId;
            this.DataLocation = location;
            this.AdditionalLocations = additionalLocations;

            this.Title = title;
            this.Description = description;
            this.HelpLink = helpLink;
            this.IsSuppressed = isSuppressed;
        }

        public bool HasTextSpan { get { return (DataLocation?.SourceSpan).HasValue; } }

        /// <summary>
        /// return TextSpan if it exists, otherwise it will throw
        /// 
        /// some diagnostic data such as created from build will have original line/column but not text span
        /// in those cases, use GetTextSpan method instead to calculate one from original line/column
        /// </summary>
        public TextSpan TextSpan { get { return (DataLocation?.SourceSpan).Value; } }

        public override bool Equals(object obj)
        {
            DiagnosticData other = obj as DiagnosticData;
            if (other == null)
            {
                return false;
            }

            return Id == other.Id &&
                    Category == other.Category &&
                    Message == other.Message &&
                    Severity == other.Severity &&
                    WarningLevel == other.WarningLevel &&
                    IsSuppressed == other.IsSuppressed &&
                    ProjectId == other.ProjectId &&
                    DocumentId == other.DocumentId &&
                    DataLocation?.OriginalStartLine == other?.DataLocation?.OriginalStartLine &&
                    DataLocation?.OriginalStartColumn == other?.DataLocation?.OriginalStartColumn;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(this.Id,
                   Hash.Combine(this.Category,
                   Hash.Combine(this.Message,
                   Hash.Combine(this.WarningLevel,
                   Hash.Combine(this.IsSuppressed,
                   Hash.Combine(this.ProjectId,
                   Hash.Combine(this.DocumentId,
                   Hash.Combine(this.DataLocation?.OriginalStartLine ?? 0,
                   Hash.Combine(this.DataLocation?.OriginalStartColumn ?? 0, (int)this.Severity)))))))));
        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2} {3} {4} {5} ({5}, {6}) [original: {7} ({8}, {9})]",
                Id,
                Severity,
                Message,
                ProjectId,
                DataLocation?.MappedFilePath ?? "",
                DataLocation?.MappedStartLine,
                DataLocation?.MappedStartColumn,
                DataLocation?.OriginalFilePath ?? "",
                DataLocation?.OriginalStartLine,
                DataLocation?.OriginalStartColumn);
        }

        public TextSpan GetExistingOrCalculatedTextSpan(SourceText text)
        {
            return HasTextSpan ? TextSpan : GetTextSpan(this.DataLocation, text);
        }

        public DiagnosticData WithCalculatedSpan(SourceText text)
        {
            Contract.ThrowIfNull(this.DocumentId);
            Contract.ThrowIfNull(this.DataLocation);
            Contract.ThrowIfTrue(HasTextSpan);

            var span = GetTextSpan(this.DataLocation, text);
            var newLocation = this.DataLocation.WithCalculatedSpan(span);
            return new DiagnosticData(this.Id, this.Category, this.Message, this.ENUMessageForBingSearch,
                this.Severity, this.DefaultSeverity, this.IsEnabledByDefault, this.WarningLevel, 
                this.CustomTags, this.Properties, this.Workspace, this.ProjectId,
                newLocation, this.AdditionalLocations, this.Title, this.Description, this.HelpLink, this.IsSuppressed);
        }

        public async Task<Diagnostic> ToDiagnosticAsync(Project project, CancellationToken cancellationToken)
        {
            var location = await this.DataLocation.ConvertLocationAsync(project, cancellationToken).ConfigureAwait(false);
            var additionalLocations = await this.AdditionalLocations.ConvertLocationsAsync(project, cancellationToken).ConfigureAwait(false);

            return Diagnostic.Create(
                this.Id, this.Category, this.Message, this.Severity, this.DefaultSeverity,
                this.IsEnabledByDefault, this.WarningLevel, this.IsSuppressed, this.Title, this.Description, this.HelpLink,
                location, additionalLocations, customTags: this.CustomTags, properties: this.Properties);
        }

        public static TextSpan GetTextSpan(DiagnosticDataLocation dataLocation, SourceText text)
        {
            var lines = text.Lines;
            if (lines.Count == 0)
            {
                return default(TextSpan);
            }

            var originalStartLine = dataLocation?.OriginalStartLine ?? 0;
            if (originalStartLine >= lines.Count)
            {
                return new TextSpan(text.Length, 0);
            }

            int startLine, startColumn, endLine, endColumn;
            AdjustBoundaries(dataLocation, lines, out startLine, out startColumn, out endLine, out endColumn);

            var startLinePosition = new LinePosition(startLine, startColumn);
            var endLinePosition = new LinePosition(endLine, endColumn);
            SwapIfNeeded(ref startLinePosition, ref endLinePosition);

            var span = text.Lines.GetTextSpan(new LinePositionSpan(startLinePosition, endLinePosition));
            return TextSpan.FromBounds(Math.Min(Math.Max(span.Start, 0), text.Length), Math.Min(Math.Max(span.End, 0), text.Length));
        }

        private static void AdjustBoundaries(DiagnosticDataLocation dataLocation,
            TextLineCollection lines, out int startLine, out int startColumn, out int endLine, out int endColumn)
        {
            startLine = dataLocation?.OriginalStartLine ?? 0;
            var originalStartColumn = dataLocation?.OriginalStartColumn ?? 0;

            startColumn = Math.Max(originalStartColumn, 0);
            if (startLine < 0)
            {
                startLine = 0;
                startColumn = 0;
            }

            endLine = dataLocation?.OriginalEndLine ?? 0;
            var originalEndColumn = dataLocation?.OriginalEndColumn ?? 0;

            endColumn = Math.Max(originalEndColumn, 0);
            if (endLine < 0)
            {
                endLine = startLine;
                endColumn = startColumn;
            }
            else if (endLine >= lines.Count)
            {
                endLine = lines.Count - 1;
                endColumn = lines[endLine].EndIncludingLineBreak;
            }
        }

        private static void SwapIfNeeded(ref LinePosition startLinePosition, ref LinePosition endLinePosition)
        {
            if (endLinePosition < startLinePosition)
            {
                var temp = startLinePosition;
                startLinePosition = endLinePosition;
                endLinePosition = temp;
            }
        }

        public static DiagnosticData Create(Workspace workspace, Diagnostic diagnostic)
        {
            Contract.Requires(diagnostic.Location == null || !diagnostic.Location.IsInSource);

            return new DiagnosticData(
                diagnostic.Id,
                diagnostic.Descriptor.Category,
                diagnostic.GetMessage(CultureInfo.CurrentUICulture),
                diagnostic.GetBingHelpMessage(),
                diagnostic.Severity,
                diagnostic.DefaultSeverity,
                diagnostic.Descriptor.IsEnabledByDefault,
                diagnostic.WarningLevel,
                diagnostic.Descriptor.CustomTags.AsImmutableOrEmpty(),
                diagnostic.Properties,
                workspace,
                projectId: null,
                title: diagnostic.Descriptor.Title.ToString(CultureInfo.CurrentUICulture),
                description: diagnostic.Descriptor.Description.ToString(CultureInfo.CurrentUICulture),
                helpLink: diagnostic.Descriptor.HelpLinkUri,
                isSuppressed: diagnostic.IsSuppressed);
        }

        public static DiagnosticData Create(Project project, Diagnostic diagnostic)
        {
            Contract.Requires(diagnostic.Location == null || !diagnostic.Location.IsInSource);

            return new DiagnosticData(
                diagnostic.Id,
                diagnostic.Descriptor.Category,
                diagnostic.GetMessage(CultureInfo.CurrentUICulture),
                diagnostic.GetBingHelpMessage(),
                diagnostic.Severity,
                diagnostic.DefaultSeverity,
                diagnostic.Descriptor.IsEnabledByDefault,
                diagnostic.WarningLevel,
                diagnostic.Descriptor.CustomTags.AsImmutableOrEmpty(),
                diagnostic.Properties,
                project.Solution.Workspace,
                project.Id,
                title: diagnostic.Descriptor.Title.ToString(CultureInfo.CurrentUICulture),
                description: diagnostic.Descriptor.Description.ToString(CultureInfo.CurrentUICulture),
                helpLink: diagnostic.Descriptor.HelpLinkUri,
                isSuppressed: diagnostic.IsSuppressed);
        }

        private static DiagnosticDataLocation CreateLocation(Document document, Location location)
        {
            if (document == null)
            {
                return null;
            }

            TextSpan sourceSpan;
            FileLinePositionSpan mappedLineInfo;
            FileLinePositionSpan originalLineInfo;
            GetLocationInfo(document, location, out sourceSpan, out originalLineInfo, out mappedLineInfo);

            var mappedStartLine = mappedLineInfo.StartLinePosition.Line;
            var mappedStartColumn = mappedLineInfo.StartLinePosition.Character;
            var mappedEndLine = mappedLineInfo.EndLinePosition.Line;
            var mappedEndColumn = mappedLineInfo.EndLinePosition.Character;

            var originalStartLine = originalLineInfo.StartLinePosition.Line;
            var originalStartColumn = originalLineInfo.StartLinePosition.Character;
            var originalEndLine = originalLineInfo.EndLinePosition.Line;
            var originalEndColumn = originalLineInfo.EndLinePosition.Character;

            return new DiagnosticDataLocation(document.Id, sourceSpan,
                originalLineInfo.Path, originalStartLine, originalStartColumn, originalEndLine, originalEndColumn,
                mappedLineInfo.GetMappedFilePathIfExist(), mappedStartLine, mappedStartColumn, mappedEndLine, mappedEndColumn);
        }

        public static DiagnosticData Create(Document document, Diagnostic diagnostic)
        {
            var location = CreateLocation(document, diagnostic.Location);

            var additionalLocations = diagnostic.AdditionalLocations.Count == 0
                ? (IReadOnlyCollection<DiagnosticDataLocation>)SpecializedCollections.EmptyArray<DiagnosticDataLocation>()
                : diagnostic.AdditionalLocations.Where(loc => loc.IsInSource)
                                                .Select(loc => CreateLocation(document.Project.GetDocument(loc.SourceTree), loc))
                                                .WhereNotNull()
                                                .ToReadOnlyCollection();

            return new DiagnosticData(
                diagnostic.Id,
                diagnostic.Descriptor.Category,
                diagnostic.GetMessage(CultureInfo.CurrentUICulture),
                diagnostic.GetBingHelpMessage(),
                diagnostic.Severity,
                diagnostic.DefaultSeverity,
                diagnostic.Descriptor.IsEnabledByDefault,
                diagnostic.WarningLevel,
                diagnostic.Descriptor.CustomTags.AsImmutableOrEmpty(),
                diagnostic.Properties,
                document.Project.Solution.Workspace,
                document.Project.Id,
                location,
                additionalLocations,
                title: diagnostic.Descriptor.Title.ToString(CultureInfo.CurrentUICulture),
                description: diagnostic.Descriptor.Description.ToString(CultureInfo.CurrentUICulture),
                helpLink: diagnostic.Descriptor.HelpLinkUri,
                isSuppressed: diagnostic.IsSuppressed);
        }

        private static void GetLocationInfo(Document document, Location location, out TextSpan sourceSpan, out FileLinePositionSpan originalLineInfo, out FileLinePositionSpan mappedLineInfo)
        {
            var diagnosticSpanMappingService = document.Project.Solution.Workspace.Services.GetService<IWorkspaceVenusSpanMappingService>();
            if (diagnosticSpanMappingService != null)
            {
                diagnosticSpanMappingService.GetAdjustedDiagnosticSpan(document.Id, location, out sourceSpan, out originalLineInfo, out mappedLineInfo);
                return;
            }

            sourceSpan = location.SourceSpan;
            originalLineInfo = location.GetLineSpan();
            mappedLineInfo = location.GetMappedLineSpan();
        }

        /// <summary>
        /// Properties for a diagnostic generated by an explicit build.
        /// </summary>
        internal static ImmutableDictionary<string, string> PropertiesForBuildDiagnostic => s_Properties;

        /// <summary>
        /// Returns true if the diagnostic was generated by an explicit build, not live analysis.
        /// </summary>
        /// <returns></returns>
        internal bool IsBuildDiagnostic()
        {
            string value;
            return this.Properties.TryGetValue(WellKnownDiagnosticPropertyNames.Origin, out value) &&
                value == WellKnownDiagnosticTags.Build;
        }
    }
}
