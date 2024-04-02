// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Shared.Extensions;
using System.Linq;

namespace Microsoft.CodeAnalysis.SemanticSearch;

internal static class SemanticSearchUtilities
{
    public const string ReferenceAssemblyDirectoryName = "SemanticSearchRefs";
    public const string GlobalUsingsDocumentName = "GlobalUsings";
    public const string ConfigDocumentName = ".editorconfig";
    public const string FindMethodName = "Find";

    public static readonly string QueryProjectName = FeaturesResources.SemanticSearch;
    public static readonly string QueryDocumentName = FeaturesResources.Query;

    private static readonly string s_thisAssemblyDirectory = Path.GetDirectoryName(typeof(SemanticSearchUtilities).Assembly.Location!)!;
    public static readonly string ReferenceAssembliesDirectory = Path.Combine(s_thisAssemblyDirectory, ReferenceAssemblyDirectoryName);

    public static List<MetadataReference> GetMetadataReferences(IMetadataService metadataService, string directory)
    {
        // TODO: https://github.com/dotnet/roslyn/issues/72585

        var metadataReferences = new List<MetadataReference>();
        try
        {
            foreach (var path in Directory.EnumerateFiles(directory, "*.dll", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    metadataReferences.Add(metadataService.GetReference(path, MetadataReferenceProperties.Assembly));
                }
                catch
                {
                    continue;
                }
            }
        }
        catch (Exception e) when (FatalError.ReportAndCatch(e, ErrorSeverity.Diagnostic))
        {
            metadataReferences = [];
        }

        return metadataReferences;
    }

    public static string GetDocumentFilePath(string language)
        => Path.Combine(s_thisAssemblyDirectory, QueryDocumentName + (language == LanguageNames.CSharp ? ".cs" : ".vb"));

    public static string GetConfigDocumentFilePath()
        => Path.Combine(s_thisAssemblyDirectory, ConfigDocumentName);

    public static SourceText CreateSourceText(string query)
        => SourceText.From(query, Encoding.UTF8, SourceHashAlgorithm.Sha256);

    public static Document GetQueryDocument(Solution solution)
        => solution.GetRequiredDocument(GetQueryDocumentId(solution));

    public static ProjectId GetQueryProjectId(Solution solution)
        => solution.ProjectIds.Single();

    public static Project GetQueryProject(Solution solution)
        => solution.Projects.Single();

    public static DocumentId GetQueryDocumentId(Solution solution)
        => GetQueryProject(solution).DocumentIds[0];

    public static bool IsQueryDocument(Document document)
        => GetQueryDocumentId(document.Project.Solution) == document.Id;
}
