// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Roslyn.Utilities;

namespace Roslyn.VisualStudio.CSharp.UnitTests.ProjectSystemShim;

internal sealed class TestEvaluationData : EvaluationData
{
    public string ProjectFilePath { get; }
    public string TargetPath { get; }
    public string AssemblyName { get; }
    public string OutputAssembly { get; }
    public string ChecksumAlgorithm { get; }

    public TestEvaluationData(string projectFilePath, string targetPath, string assemblyName, string outputAssembly, string checksumAlgorithm)
    {
        ProjectFilePath = projectFilePath;
        TargetPath = targetPath;
        AssemblyName = assemblyName;
        OutputAssembly = outputAssembly;
        ChecksumAlgorithm = checksumAlgorithm;
    }

    public override string GetPropertyValue(string name)
        => name switch
        {
            "MSBuildProjectFullPath" => ProjectFilePath,
            "TargetPath" => TargetPath,
            "AssemblyName" => AssemblyName,
            "CommandLineArgsForDesignTimeEvaluation" => "-checksumalgorithm:" + ChecksumAlgorithm,
            _ => throw ExceptionUtilities.UnexpectedValue(name)
        };

    public override ImmutableArray<string> GetItemValues(string name)
        => name switch
        {
            "IntermediateAssembly" => ImmutableArray.Create(OutputAssembly),
            _ => throw ExceptionUtilities.UnexpectedValue(name)
        };
}
