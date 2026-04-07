// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim.Interop;
using Microsoft.VisualStudio.Shell;
using Roslyn.VisualStudio.IntegrationTests;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp;

public sealed class CSharpTempPE : AbstractIntegrationTest
{
    [IdeFact]
    public async Task TempPEServiceWorks()
    {
        using TempRoot root = new();
        var outputPath = root.CreateFile().Path;

        var service = await AsyncServiceProvider.GlobalProvider.GetServiceAsync<ICSharpTempPECompilerService, ICSharpTempPECompilerService>();
        var result = service.CompileTempPE(outputPath, 1, ["Test.cs"], ["class Program { static void Main() { } }"], 0, [], []);
        Assert.Equal(VSConstants.S_OK, result);

        Assert.True(File.Exists(outputPath));
    }
}
