// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ChangeSignature
{
    internal sealed class SignatureChange
    {
        public readonly ParameterConfiguration OriginalConfiguration;
        public readonly ParameterConfiguration UpdatedConfiguration;

        private readonly Dictionary<int, int?> _originalIndexToUpdatedIndexMap = new Dictionary<int, int?>();

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
            => new SignatureChange(OriginalConfiguration, UpdatedConfiguration.WithoutAddedParameters());
    }
}
