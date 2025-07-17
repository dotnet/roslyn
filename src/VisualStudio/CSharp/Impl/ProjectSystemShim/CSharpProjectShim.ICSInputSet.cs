// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim;

internal partial class CSharpProjectShim : ICSInputSet
{
    public ICSCompiler GetCompiler()
        => throw new NotImplementedException();

    public void AddSourceFile(string filename)
    {
        // Nothing to do here. We watch addition/removal of source files via the ICSharpProjectSite methods.
    }

    public void RemoveSourceFile(string filename)
    {
        // Nothing to do here. We watch addition/removal of source files via the ICSharpProjectSite methods.
    }

    public void RemoveAllSourceFiles()
        => throw new NotImplementedException();

    public void AddResourceFile(string filename, string ident, bool embed, bool vis)
        => throw new NotImplementedException();

    public void RemoveResourceFile(string filename, string ident, bool embed, bool vis)
        => throw new NotImplementedException();

    public void SetWin32Resource(string filename)
    {
        // This file is used only during emit. Since we no longer use our in-proc workspace to emit, we can ignore this value.
    }

    public void SetOutputFileName(string filename)
    {
        // Some projects like web projects give us just a filename; those aren't really useful (they're just filler) so we'll ignore them for purposes of tracking the path
        if (PathUtilities.IsAbsolute(filename))
        {
            ProjectSystemProject.CompilationOutputAssemblyFilePath = filename;
        }

        if (filename != null)
        {
            ProjectSystemProject.AssemblyName = Path.GetFileNameWithoutExtension(filename);
        }

        RefreshBinOutputPath();
    }

    public void SetOutputFileType(OutputFileType fileType)
        => ProjectSystemProjectOptionsProcessor.SetOutputFileType(fileType);

    public void SetImageBase(uint imageBase)
    {
        // This option is used only during emit. Since we no longer use our in-proc workspace to emit, we can ignore this value.
    }

    public void SetMainClass(string fullyQualifiedClassName)
        => ProjectSystemProjectOptionsProcessor.SetMainTypeName(fullyQualifiedClassName);

    public void SetWin32Icon(string iconFileName)
    {
        // This option is used only during emit. Since we no longer use our in-proc workspace to emit, we can ignore this value.
    }

    public void SetFileAlignment(uint align)
    {
        // This option is used only during emit. Since we no longer use our in-proc workspace to emit, we can ignore this value.
    }

    public void SetImageBase2(ulong imageBase)
    {
        // This option is used only during emit. Since we no longer use our in-proc workspace to emit, we can ignore this value.
    }

    public void SetPdbFileName(string filename)
    {
        // This option is used only during emit. Since we no longer use our in-proc workspace to emit, we can ignore this value.
    }

    public string GetWin32Resource()
        => throw new NotImplementedException();

    public void SetWin32Manifest(string manifestFileName)
    {
        // This option is used only during emit. Since we no longer use our in-proc workspace to emit, we can ignore this value.
    }
}
