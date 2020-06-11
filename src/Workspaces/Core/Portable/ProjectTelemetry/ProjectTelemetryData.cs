// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis.ProjectTelemetry
{
    /// <summary>
    /// Serialization typed used to pass information to/from OOP and VS.
    /// </summary>
    internal struct ProjectTelemetryData
    {
        public ProjectId ProjectId;
        public string Language;
        public int AnalyzerReferencesCount;
        public int ProjectReferencesCount;
        public int MetadataReferencesCount;
        public int DocumentsCount;
        public int AdditionalDocumentsCount;

        public bool Equals(ProjectTelemetryData other)
            => Language.Equals(other.Language) &&
               AnalyzerReferencesCount == other.AnalyzerReferencesCount &&
               ProjectReferencesCount == other.ProjectReferencesCount &&
               MetadataReferencesCount == other.MetadataReferencesCount &&
               DocumentsCount == other.DocumentsCount &&
               AdditionalDocumentsCount == other.AdditionalDocumentsCount;
    }
}
