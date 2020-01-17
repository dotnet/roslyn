// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

            for (var i = 0; i < originalParameterList.Count; i++)
            {
                int? index = null;
                var parameter = originalParameterList[i];
                if (parameter is ExistingParameter existingParameter)
                {
                    var updatedIndex = updatedParameterList.IndexOf(p => p is ExistingParameter ep && ep.Symbol == existingParameter.Symbol);
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
            if (parameterIndex >= OriginalConfiguration.ToListOfParameters().Count)
            {
                return null;
            }

            return _originalIndexToUpdatedIndexMap[parameterIndex];
        }

        internal SignatureChange WithoutAddedParameters()
            => new SignatureChange(OriginalConfiguration, UpdatedConfiguration.WithoutAddedParameters());
    }
}
