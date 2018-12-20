﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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
            => HasTextSpan ? EnsureInBounds(TextSpan, text) : GetTextSpan(this.DataLocation, text);

        private static TextSpan EnsureInBounds(TextSpan textSpan, SourceText text)
            => TextSpan.FromBounds(
                Math.Min(textSpan.Start, text.Length),
                Math.Min(textSpan.End, text.Length));

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
                return default;
            }

            var originalStartLine = dataLocation?.OriginalStartLine ?? 0;
            if (originalStartLine >= lines.Count)
            {
                return new TextSpan(text.Length, 0);
            }

            AdjustBoundaries(dataLocation, lines,
                out var startLine, out var startColumn, out var endLine, out var endColumn);

            var startLinePosition = new LinePosition(startLine, startColumn);
            var endLinePosition = new LinePosition(endLine, endColumn);
            SwapIfNeeded(ref startLinePosition, ref endLinePosition);

            var span = text.Lines.GetTextSpan(new LinePositionSpan(startLinePosition, endLinePosition));
            return EnsureInBounds(TextSpan.FromBounds(Math.Max(span.Start, 0), Math.Max(span.End, 0)), text);
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
            Debug.Assert(diagnostic.Location == null || !diagnostic.Location.IsInSource);

            return new DiagnosticData(
                diagnostic.Id,
                diagnostic.Descriptor.Category,
                diagnostic.GetMessage(CultureInfo.CurrentUICulture),
                diagnostic.GetBingHelpMessage(workspace),
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
            Debug.Assert(diagnostic.Location == null || !diagnostic.Location.IsInSource);

            return new DiagnosticData(
                diagnostic.Id,
                diagnostic.Descriptor.Category,
                diagnostic.GetMessage(CultureInfo.CurrentUICulture),
                diagnostic.GetBingHelpMessage(project.Solution.Workspace),
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

            GetLocationInfo(document, location, out var sourceSpan, out var originalLineInfo, out var mappedLineInfo);

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
                ? (IReadOnlyCollection<DiagnosticDataLocation>)Array.Empty<DiagnosticDataLocation>()
                : diagnostic.AdditionalLocations.Where(loc => loc.IsInSource)
                                                .Select(loc => CreateLocation(document.Project.GetDocument(loc.SourceTree), loc))
                                                .WhereNotNull()
                                                .ToReadOnlyCollection();

            var properties = GetProperties(document, diagnostic);

            return new DiagnosticData(
                diagnostic.Id,
                diagnostic.Descriptor.Category,
                diagnostic.GetMessage(CultureInfo.CurrentUICulture),
                diagnostic.GetBingHelpMessage(document.Project.Solution.Workspace),
                diagnostic.Severity,
                diagnostic.DefaultSeverity,
                diagnostic.Descriptor.IsEnabledByDefault,
                diagnostic.WarningLevel,
                diagnostic.Descriptor.CustomTags.AsImmutableOrEmpty(),
                properties,
                document.Project.Solution.Workspace,
                document.Project.Id,
                location,
                additionalLocations,
                title: diagnostic.Descriptor.Title.ToString(CultureInfo.CurrentUICulture),
                description: diagnostic.Descriptor.Description.ToString(CultureInfo.CurrentUICulture),
                helpLink: diagnostic.Descriptor.HelpLinkUri,
                isSuppressed: diagnostic.IsSuppressed);
        }

        private static ImmutableDictionary<string, string> GetProperties(
            Document document, Diagnostic diagnostic)
        {
            var properties = diagnostic.Properties;
            var service = document.GetLanguageService<IDiagnosticPropertiesService>();
            var additionalProperties = service?.GetAdditionalProperties(diagnostic);

            return additionalProperties == null
                ? properties
                : properties.AddRange(additionalProperties);
            throw new NotImplementedException();
        }

        /// <summary>
        /// Create a host/VS specific diagnostic with the given descriptor and message arguments for the given project.
        /// Note that diagnostic created through this API cannot be suppressed with in-source suppression due to performance reasons (see the PERF remark below for details).
        /// </summary>
        public static bool TryCreate(DiagnosticDescriptor descriptor, string[] messageArguments, ProjectId projectId, Workspace workspace, out DiagnosticData diagnosticData, CancellationToken cancellationToken = default)
        {
            diagnosticData = null;
            var project = workspace.CurrentSolution.GetProject(projectId);
            if (project == null)
            {
                return false;
            }

            DiagnosticSeverity effectiveSeverity;
            if (project.SupportsCompilation)
            {
                // Get the effective severity of the diagnostic from the compilation options.
                // PERF: We do not check if the diagnostic was suppressed by a source suppression, as this requires us to force complete the assembly attributes, which is very expensive.
                ReportDiagnostic reportDiagnostic = descriptor.GetEffectiveSeverity(project.CompilationOptions);
                if (reportDiagnostic == ReportDiagnostic.Suppress)
                {
                    // Rule is disabled by compilation options.
                    return false;
                }

                effectiveSeverity = GetEffectiveSeverity(reportDiagnostic, descriptor.DefaultSeverity);
            }
            else
            {
                effectiveSeverity = descriptor.DefaultSeverity;
            }

            var diagnostic = Diagnostic.Create(descriptor, Location.None, effectiveSeverity, additionalLocations: null, properties: null, messageArgs: messageArguments);
            diagnosticData = diagnostic.ToDiagnosticData(project);
            return true;
        }

        private static DiagnosticSeverity GetEffectiveSeverity(ReportDiagnostic effectiveReportDiagnostic, DiagnosticSeverity defaultSeverity)
        {
            switch (effectiveReportDiagnostic)
            {
                case ReportDiagnostic.Default:
                    return defaultSeverity;

                case ReportDiagnostic.Error:
                    return DiagnosticSeverity.Error;

                case ReportDiagnostic.Hidden:
                    return DiagnosticSeverity.Hidden;

                case ReportDiagnostic.Info:
                    return DiagnosticSeverity.Info;

                case ReportDiagnostic.Warn:
                    return DiagnosticSeverity.Warning;

                default:
                    throw ExceptionUtilities.Unreachable;
            }
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
            return this.Properties.TryGetValue(WellKnownDiagnosticPropertyNames.Origin, out var value) &&
                value == WellKnownDiagnosticTags.Build;
        }
    }
}
