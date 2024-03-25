// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ChangeSignature;

internal sealed class SignatureChange
{
    public readonly ParameterConfiguration OriginalConfiguration;
    public readonly ParameterConfiguration UpdatedConfiguration;

    private readonly Dictionary<int, int?> _originalIndexToUpdatedIndexMap = [];

    public SignatureChange(ParameterConfiguration originalConfiguration, ParameterConfiguration updatedConfiguration)
    {
        OriginalConfiguration = originalConfiguration;
        UpdatedConfiguration = updatedConfiguration;

        // TODO: Could be better than O(n^2)
        var originalParameterList = originalConfiguration.ToListOfParameters();
        var updatedParameterList = updatedConfiguration.ToListOfParameters();

        for (var i = 0; i < originalParameterList.Length; i++)
        {
            int? index = null;
            var parameter = originalParameterList[i];
            if (parameter is ExistingParameter existingParameter)
            {
                var updatedIndex = updatedParameterList.IndexOf(p => p is ExistingParameter ep && ep.Symbol.Equals(existingParameter.Symbol));
                if (updatedIndex >= 0)
                {
                    index = updatedIndex;
                }
            }

            _originalIndexToUpdatedIndexMap.Add(i, index);
        }
    }

    public int? GetUpdatedIndex(int parameterIndex)
    {
        if (parameterIndex >= OriginalConfiguration.ToListOfParameters().Length)
        {
            return null;
        }

        return _originalIndexToUpdatedIndexMap[parameterIndex];
    }

    internal SignatureChange WithoutAddedParameters()
        => new(OriginalConfiguration, UpdatedConfiguration.WithoutAddedParameters());

    internal void LogTelemetry()
    {
        var originalListOfParameters = OriginalConfiguration.ToListOfParameters();
        var updatedListOfParameters = UpdatedConfiguration.ToListOfParameters();

        ChangeSignatureLogger.LogTransformationInformation(
            numOriginalParameters: originalListOfParameters.Length,
            numParametersAdded: updatedListOfParameters.Count(p => p is AddedParameter),
            numParametersRemoved: originalListOfParameters.Count(p => !updatedListOfParameters.Contains(p)),
            anyParametersReordered: AnyParametersReordered(originalListOfParameters, updatedListOfParameters));

        foreach (var addedParameter in updatedListOfParameters.OfType<AddedParameter>())
        {
            if (addedParameter.IsRequired)
            {
                ChangeSignatureLogger.LogAddedParameterRequired();
            }

            if (addedParameter.TypeBinds)
            {
                ChangeSignatureLogger.LogAddedParameterTypeBinds();
            }

            if (addedParameter.CallSiteKind == CallSiteKind.Todo)
            {
                ChangeSignatureLogger.LogAddedParameter_ValueTODO();
            }
            else if (addedParameter.CallSiteKind == CallSiteKind.Omitted)
            {
                ChangeSignatureLogger.LogAddedParameter_ValueOmitted();
            }
            else
            {
                if (addedParameter.CallSiteKind == CallSiteKind.ValueWithName)
                {
                    ChangeSignatureLogger.LogAddedParameter_ValueExplicitNamed();
                }
                else
                {
                    ChangeSignatureLogger.LogAddedParameter_ValueExplicit();
                }
            }
        }
    }

    private static bool AnyParametersReordered(ImmutableArray<Parameter> originalListOfParameters, ImmutableArray<Parameter> updatedListOfParameters)
    {
        var originalListWithoutRemovedOrAdded = originalListOfParameters.Where(updatedListOfParameters.Contains).ToImmutableArray();
        var updatedListWithoutRemovedOrAdded = updatedListOfParameters.Where(originalListOfParameters.Contains).ToImmutableArray();

        for (var i = 0; i < originalListWithoutRemovedOrAdded.Length; i++)
        {
            if (originalListWithoutRemovedOrAdded[i] != updatedListWithoutRemovedOrAdded[i])
            {
                return true;
            }
        }

        return false;
    }
}
