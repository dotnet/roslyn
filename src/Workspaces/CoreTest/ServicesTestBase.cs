// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public abstract class ServicesTestBase : TestBase
    {
        public static Solution AddProjectWithMetadataReferences(Solution solution, string projectName, string languageName, string code, MetadataReference metadataReference, params ProjectId[] projectReferences)
        {
            var suffix = languageName == LanguageNames.CSharp ? "cs" : "vb";
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);
            var pi = ProjectInfo.Create(
                pid,
                VersionStamp.Default,
                projectName,
                projectName,
                languageName,
                metadataReferences: new[] { metadataReference },
                projectReferences: projectReferences.Select(p => new ProjectReference(p)));
            return solution.AddProject(pi).AddDocument(did, $"{projectName}.{suffix}", SourceText.From(code));
        }
    }
}
