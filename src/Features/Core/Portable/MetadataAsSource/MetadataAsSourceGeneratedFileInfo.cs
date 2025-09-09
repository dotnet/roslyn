// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.MetadataAsSource;

internal sealed class MetadataAsSourceGeneratedFileInfo
{
    public string TemporaryFilePath { get; }
    public Workspace Workspace { get; }
    public ProjectId SourceProjectId { get; }
    public bool SignaturesOnly { get; }
    public string LanguageName { get; }
    public string Extension { get; }

    public static Encoding Encoding => Encoding.UTF8;
    public static SourceHashAlgorithm ChecksumAlgorithm => SourceHashAlgorithms.Default;

    public MetadataAsSourceGeneratedFileInfo(string rootPath, Workspace sourceWorkspace, Project sourceProject, INamedTypeSymbol topLevelNamedType, bool signaturesOnly)
    {
        this.SourceProjectId = sourceProject.Id;
        this.Workspace = sourceWorkspace;
        this.LanguageName = signaturesOnly ? sourceProject.Language : LanguageNames.CSharp;
        this.SignaturesOnly = signaturesOnly;

        this.Extension = LanguageName == LanguageNames.CSharp ? ".cs" : ".vb";

        var directoryName = Guid.NewGuid().ToString("N");
        this.TemporaryFilePath = Path.Combine(rootPath, directoryName, topLevelNamedType.Name + Extension);
    }
}
