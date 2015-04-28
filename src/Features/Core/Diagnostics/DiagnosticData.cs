// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal sealed class DiagnosticData
    {
        public static readonly CultureInfo USCultureInfo = CultureInfo.GetCultureInfo("en-US");

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

        public readonly string ENUMessageForBingSearch;

        public readonly Workspace Workspace;
        public readonly ProjectId ProjectId;
        public readonly DocumentId DocumentId;

        /// <summary>
        /// Null if path is not mapped and <see cref="OriginalFilePath"/> contains the actual path.
        /// Note that the value might be a relative path. In that case <see cref="OriginalFilePath"/> should be used
        /// as a base path for path resolution.
        /// </summary>
        public readonly string MappedFilePath;
        public readonly string OriginalFilePath;

        public readonly int MappedStartLine;
        public readonly int MappedStartColumn;
        public readonly int MappedEndLine;
        public readonly int MappedEndColumn;

        public readonly int OriginalStartLine;
        public readonly int OriginalStartColumn;
        public readonly int OriginalEndLine;
        public readonly int OriginalEndColumn;

        // text can be either given or calculated from original line/column
        private readonly TextSpan? _textSpan;

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
            DocumentId documentId = null,
            TextSpan? span = null,
            string originalFilePath = null,
            int originalStartLine = 0,
            int originalStartColumn = 0,
            int originalEndLine = 0,
            int originalEndColumn = 0,
            string title = null,
            string description = null,
            string helpLink = null) :
                this(
                    id, category, message, enuMessageForBingSearch,
                    severity, severity, isEnabledByDefault, warningLevel,
                    ImmutableArray<string>.Empty, ImmutableDictionary<string, string>.Empty,
                    workspace, projectId, documentId, span,
                    null, originalStartLine, originalStartColumn, originalEndLine, originalEndColumn,
                    originalFilePath, originalStartLine, originalStartColumn, originalEndLine, originalEndColumn,
                    title, description, helpLink)
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
            DocumentId documentId = null,
            TextSpan? span = null,
            string mappedFilePath = null,
            int mappedStartLine = 0,
            int mappedStartColumn = 0,
            int mappedEndLine = 0,
            int mappedEndColumn = 0,
            string originalFilePath = null,
            int originalStartLine = 0,
            int originalStartColumn = 0,
            int originalEndLine = 0,
            int originalEndColumn = 0,
            string title = null,
            string description = null,
            string helpLink = null)
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
            this.DocumentId = documentId;

            this.MappedStartLine = mappedStartLine;
            this.MappedStartColumn = mappedStartColumn;
            this.MappedEndLine = mappedEndLine;
            this.MappedEndColumn = mappedEndColumn;
            this.MappedFilePath = mappedFilePath;

            this.OriginalStartLine = originalStartLine;
            this.OriginalStartColumn = originalStartColumn;
            this.OriginalEndLine = originalEndLine;
            this.OriginalEndColumn = originalEndColumn;
            this.OriginalFilePath = originalFilePath;

            _textSpan = span;

            this.Title = title;
            this.Description = description;
            this.HelpLink = helpLink;
        }

        public bool HasTextSpan { get { return _textSpan.HasValue; } }

        /// <summary>
        /// return TextSpan if it exists, otherwise it will throw
        /// 
        /// some diagnostic data such as created from build will have original line/column but not text span
        /// in those cases, use GetTextSpan method instead to calculate one from original line/column
        /// </summary>
        public TextSpan TextSpan { get { return _textSpan.Value; } }

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
                    ProjectId == other.ProjectId &&
                    DocumentId == other.DocumentId &&
                    OriginalStartLine == other.OriginalStartLine &&
                    OriginalStartColumn == other.OriginalStartColumn;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(this.Id,
                   Hash.Combine(this.Category,
                   Hash.Combine(this.Message,
                   Hash.Combine(this.WarningLevel,
                   Hash.Combine(this.ProjectId,
                   Hash.Combine(this.DocumentId,
                   Hash.Combine(this.OriginalStartLine,
                   Hash.Combine(this.OriginalStartColumn, (int)this.Severity))))))));
        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2} {3} {4} {5} ({5}, {6}) [original: {7} ({8}, {9})]",
                Id,
                Severity,
                Message,
                ProjectId,
                MappedFilePath ?? "",
                MappedStartLine,
                MappedStartColumn,
                OriginalFilePath ?? "",
                OriginalStartLine,
                OriginalStartColumn);
        }

        public Diagnostic ToDiagnostic(SyntaxTree tree)
        {
            var location = Location.None;
            if (tree != null)
            {
                var span = _textSpan.HasValue ? _textSpan.Value : GetTextSpan(tree.GetText());
                location = tree.GetLocation(span);
            }
            else if (OriginalFilePath != null && _textSpan != null)
            {
                var span = _textSpan.Value;
                location = Location.Create(OriginalFilePath, span, new LinePositionSpan(
                    new LinePosition(OriginalStartLine, OriginalStartColumn),
                    new LinePosition(OriginalEndLine, OriginalEndColumn)));
            }

            return Diagnostic.Create(this.Id, this.Category, this.Message, this.Severity, this.DefaultSeverity, this.IsEnabledByDefault, this.WarningLevel, this.Title, this.Description, this.HelpLink, location, customTags: this.CustomTags, properties: this.Properties);
        }

        public TextSpan GetExistingOrCalculatedTextSpan(SourceText text)
        {
            return _textSpan.HasValue ? _textSpan.Value : GetTextSpan(text);
        }

        public TextSpan GetTextSpan(SourceText text)
        {
            var lines = text.Lines;
            if (lines.Count == 0)
            {
                return default(TextSpan);
            }

            if (this.OriginalStartLine >= lines.Count)
            {
                return new TextSpan(text.Length, 0);
            }

            int startLine, startColumn, endLine, endColumn;
            AdjustBoundaries(lines, out startLine, out startColumn, out endLine, out endColumn);

            var startLinePosition = new LinePosition(startLine, startColumn);
            var endLinePosition = new LinePosition(endLine, endColumn);
            SwapIfNeeded(ref startLinePosition, ref endLinePosition);

            var span = text.Lines.GetTextSpan(new LinePositionSpan(startLinePosition, endLinePosition));
            return TextSpan.FromBounds(Math.Min(Math.Max(span.Start, 0), text.Length), Math.Min(Math.Max(span.End, 0), text.Length));
        }

        private void AdjustBoundaries(
            TextLineCollection lines, out int startLine, out int startColumn, out int endLine, out int endColumn)
        {
            startLine = this.OriginalStartLine;
            startColumn = Math.Max(this.OriginalStartColumn, 0);
            if (startLine < 0)
            {
                startLine = 0;
                startColumn = 0;
            }

            endLine = this.OriginalEndLine;
            endColumn = Math.Max(this.OriginalEndColumn, 0);
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
                diagnostic.GetMessage(USCultureInfo), // We use the ENU version of the message for bing search.
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
                helpLink: diagnostic.Descriptor.HelpLinkUri);
        }

        public static DiagnosticData Create(Project project, Diagnostic diagnostic)
        {
            Contract.Requires(diagnostic.Location == null || !diagnostic.Location.IsInSource);

            return new DiagnosticData(
                diagnostic.Id,
                diagnostic.Descriptor.Category,
                diagnostic.GetMessage(CultureInfo.CurrentUICulture),
                diagnostic.GetMessage(USCultureInfo), // We use the ENU version of the message for bing search.
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
                helpLink: diagnostic.Descriptor.HelpLinkUri);
        }

        public static DiagnosticData Create(Document document, Diagnostic diagnostic)
        {
            var location = diagnostic.Location;

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

            return new DiagnosticData(
                diagnostic.Id,
                diagnostic.Descriptor.Category,
                diagnostic.GetMessage(CultureInfo.CurrentUICulture),
                diagnostic.GetMessage(USCultureInfo), // We use the ENU version of the message for bing search.
                diagnostic.Severity,
                diagnostic.DefaultSeverity,
                diagnostic.Descriptor.IsEnabledByDefault,
                diagnostic.WarningLevel,
                diagnostic.Descriptor.CustomTags.AsImmutableOrEmpty(),
                diagnostic.Properties,
                document.Project.Solution.Workspace,
                document.Project.Id,
                document.Id,
                sourceSpan,
                mappedLineInfo.GetMappedFilePathIfExist(),
                mappedStartLine,
                mappedStartColumn,
                mappedEndLine,
                mappedEndColumn,
                originalLineInfo.Path,
                originalStartLine,
                originalStartColumn,
                originalEndLine,
                originalEndColumn,
                title: diagnostic.Descriptor.Title.ToString(CultureInfo.CurrentUICulture),
                description: diagnostic.Descriptor.Description.ToString(CultureInfo.CurrentUICulture),
                helpLink: diagnostic.Descriptor.HelpLinkUri);
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
    }
}
