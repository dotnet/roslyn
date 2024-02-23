// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Classification;

internal interface IClassificationConfigurationService
{
    ClassificationOptions Options { get; }
}

[ExportWorkspaceService(typeof(IWorkspaceConfigurationService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DefaultClassificationConfigurationService() : IClassificationConfigurationService
{
    public ClassificationOptions Options => ClassificationOptions.Default;
}

[DataContract]
internal readonly record struct ClassificationOptions(
    [property: DataMember(Order = 1)] bool ClassifyReassignedVariables = false,
    [property: DataMember(Order = 2)] bool ColorizeRegexPatterns = true,
    [property: DataMember(Order = 3)] bool ColorizeJsonPatterns = true,
    [property: DataMember(Order = 4)] bool ForceFrozenPartialSemanticsForCrossProcessOperations = false,
    [property: DataMember(Order = 5)] bool DisableNullableAnalysisInClassification = false)
{
    public static readonly ClassificationOptions Default = new();
}

internal static class ClassificationConfigurationOptions
{
    public static ClassificationOptions GetClassificationConfigurationOptions(this IGlobalOptionService globalOptions)
        => new ClassificationOptions()
        {
            DisableNullableAnalysisInClassification = globalOptions.GetOption(DisableNullableAnalysisInClassification)
        };

    public static readonly Option2<bool> DisableNullableAnalysisInClassification = new(
        "dotnet_disable_nullable_analysis_in_classification", ClassificationOptions.Default.DisableNullableAnalysisInClassification);
}
