// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.ProjectTelemetry
{
    /// <summary>
    /// Serialization typed used to pass information to/from OOP and VS.
    /// </summary>
    [DataContract]
    internal readonly struct ProjectTelemetryData
    {
        [DataMember(Order = 0)]
        public readonly ProjectId ProjectId;

        [DataMember(Order = 1)]
        public readonly string Language;

        [DataMember(Order = 2)]
        public readonly int AnalyzerReferencesCount;

        [DataMember(Order = 3)]
        public readonly int ProjectReferencesCount;

        [DataMember(Order = 4)]
        public readonly int MetadataReferencesCount;

        [DataMember(Order = 5)]
        public readonly int DocumentsCount;

        [DataMember(Order = 6)]
        public readonly int AdditionalDocumentsCount;

        public ProjectTelemetryData(ProjectId projectId, string language, int analyzerReferencesCount, int projectReferencesCount, int metadataReferencesCount, int documentsCount, int additionalDocumentsCount)
        {
            ProjectId = projectId;
            Language = language;
            AnalyzerReferencesCount = analyzerReferencesCount;
            ProjectReferencesCount = projectReferencesCount;
            MetadataReferencesCount = metadataReferencesCount;
            DocumentsCount = documentsCount;
            AdditionalDocumentsCount = additionalDocumentsCount;
        }

        public bool Equals(ProjectTelemetryData other)
            => Language.Equals(other.Language) &&
               AnalyzerReferencesCount == other.AnalyzerReferencesCount &&
               ProjectReferencesCount == other.ProjectReferencesCount &&
               MetadataReferencesCount == other.MetadataReferencesCount &&
               DocumentsCount == other.DocumentsCount &&
               AdditionalDocumentsCount == other.AdditionalDocumentsCount;
    }
}
