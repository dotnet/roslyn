// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.UnitTests.ProjectSystemShim
{
    public class TempPECompilerServiceTests
    {
        [Fact]
        public void TempPECompilationWithInvalidReferenceDoesNotCrash()
        {
            var tempPEService = new TempPECompilerService(new TrivialMetadataService());

            using var tempRoot = new TempRoot();
            var directory = tempRoot.CreateDirectory();

            // This should not crash. Visual inspection of the Dev12 codebase implied we might return
            // S_FALSE in this case, but it wasn't very clear. In any case, it's not expected to throw,m
            // so S_FALSE seems fine.
            var hr = tempPEService.CompileTempPE(
                pszOutputFileName: Path.Combine(directory.Path, "Output.dll"),
                sourceCount: 0,
                fileNames: [],
                fileContents: [],
                optionCount: 1,
                optionNames: ["r"],
                optionValues: new[] { Path.Combine(directory.Path, "MissingReference.dll") });

            Assert.Equal(VSConstants.S_FALSE, hr);
        }

        private class TrivialMetadataService : IMetadataService
        {
            public PortableExecutableReference GetReference(string resolvedPath, MetadataReferenceProperties properties)
            {
                return MetadataReference.CreateFromFile(resolvedPath, properties);
            }
        }
    }
}
