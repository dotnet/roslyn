// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.ChangeSignature
{
    internal sealed class SignatureChange
    {
        public readonly ParameterConfiguration OriginalConfiguration;
        public readonly ParameterConfiguration UpdatedConfiguration;

        private readonly Dictionary<int, int?> _originalIndexToUpdatedIndexMap = new Dictionary<int, int?>();

        public SignatureChange(ParameterConfiguration originalConfiguration, ParameterConfiguration updatedConfiguration)
        {
            this.OriginalConfiguration = originalConfiguration;
            this.UpdatedConfiguration = updatedConfiguration;

            // TODO: Could be better than O(n^2)
            var originalParameterList = originalConfiguration.ToListOfParameters();
            var updatedParameterList = updatedConfiguration.ToListOfParameters();

            for (int i = 0; i < originalParameterList.Count; i++)
            {
                var parameter = originalParameterList[i];
                var updatedIndex = updatedParameterList.IndexOf(parameter);
                _originalIndexToUpdatedIndexMap.Add(i, updatedIndex != -1 ? updatedIndex : (int?)null);
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
    }
}
