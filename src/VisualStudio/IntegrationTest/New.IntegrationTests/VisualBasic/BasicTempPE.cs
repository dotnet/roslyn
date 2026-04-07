// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim.Interop;
using Microsoft.VisualStudio.Shell;
using Roslyn.VisualStudio.IntegrationTests;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.VisualBasic;

public sealed class BasicTempPE : AbstractIntegrationTest
{
    [IdeFact]
    public async Task TempPEServiceWorks()
    {
        using TempRoot root = new();
        var outputPath = root.CreateFile().Path;

        var factory = await AsyncServiceProvider.GlobalProvider.GetServiceAsync<IVbTempPECompilerFactory, IVbTempPECompilerFactory>();
        var compiler = factory.CreateCompiler();

        Assert.NotNull(compiler);

        // Create a temporary project to test compilation
        var project = compiler.CreateProject("TestProject", null, null, new VbCompilerHost());
        Assert.NotNull(project);

        var sourceFile = root.CreateFile("Test.vb").WriteAllText("Module Program\r\n    Sub Main()\r\n    End Sub\r\nEnd Module").Path;
        project.AddFile(sourceFile, (uint)VSConstants.VSITEMID.Nil, fAddDuringOpen: false);

        project.SetCompilerOptions(new VBCompilerOptions() { wszOutputPath = Path.GetDirectoryName(outputPath), wszExeName = Path.GetFileName(outputPath) });

        // Compile the project
        var result = compiler.Compile(IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        Assert.Equal(VSConstants.S_OK, result);

        Assert.True(File.Exists(outputPath));
    }

    private class VbCompilerHost : IVbCompilerHost
    {
        public void OutputString(string @string)
        {
        }

        public int GetSdkPath(out string sdkPath)
        {
            sdkPath = "";
            return VSConstants.E_NOTIMPL;
        }

        public VBTargetLibraryType GetTargetLibraryType()
        {
            throw new NotImplementedException();
        }
    }
}
