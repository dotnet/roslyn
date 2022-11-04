// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Roslyn.Utilities;

namespace Roslyn.VisualStudio.CSharp.UnitTests.ProjectSystemShim;

internal sealed class TestEvaluationData : EvaluationData
{
    public string ProjectFilePath { get; }
    public string TargetPath { get; }
    public string AssemblyName { get; }

    public TestEvaluationData(string projectFilePath, string targetPath, string assemblyName)
    {
        ProjectFilePath = projectFilePath;
        TargetPath = targetPath;
        AssemblyName = assemblyName;
    }

    public override string GetPropertyValue(string name)
        => name switch
        {
            "MSBuildProjectFullPath" => ProjectFilePath,
            "TargetPath" => TargetPath,
            "AssemblyName" => AssemblyName,
            _ => throw ExceptionUtilities.UnexpectedValue(name)
        };
}
