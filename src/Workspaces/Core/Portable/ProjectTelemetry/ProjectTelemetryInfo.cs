// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis.ProjectTelemetry
{
    internal struct ProjectTelemetryInfo
    {
        public ProjectId ProjectId;
        public string Language;
        public int AnalyzerReferencesCount;
        public int ProjectReferencesCount;
        public int MetadataReferencesCount;
        public int DocumentsCount;
        public int AdditionalDocumentsCount;

        public ProjectTelemetryInfo(ProjectId projectId, string language, int analyzerReferencesCount, int projectReferencesCount, int metadataReferencesCount, int documentsCount, int additionalDocumentsCount)
        {
            this.ProjectId = projectId;
            this.Language = language;
            this.AnalyzerReferencesCount = analyzerReferencesCount;
            this.ProjectReferencesCount = projectReferencesCount;
            this.MetadataReferencesCount = metadataReferencesCount;
            this.DocumentsCount = documentsCount;
            this.AdditionalDocumentsCount = additionalDocumentsCount;
        }

        public bool Equals(ProjectTelemetryInfo other)
        {
            return this.Language.Equals(other.Language) &&
                   this.AnalyzerReferencesCount == other.AnalyzerReferencesCount &&
                   this.ProjectReferencesCount == other.ProjectReferencesCount &&
                   this.MetadataReferencesCount == other.MetadataReferencesCount &&
                   this.DocumentsCount == other.DocumentsCount &&
                   this.AdditionalDocumentsCount == other.AdditionalDocumentsCount;
        }
    }
}
