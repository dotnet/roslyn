// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using Microsoft.CodeAnalysis.ChangeSignature;

namespace Microsoft.CodeAnalysis.Test.Utilities.ChangeSignature
{
    internal sealed class AddedParameterOrExistingIndex
    {
        public bool IsExisting { get; }

        public int? OldIndex { get; }

        public AddedParameter? AddedParameter { get; }

        public AddedParameterOrExistingIndex(int index)
        {
            OldIndex = index;
            IsExisting = true;
            AddedParameter = null;
        }

        public AddedParameterOrExistingIndex(AddedParameter addedParameter)
        {
            OldIndex = null;
            IsExisting = false;
            AddedParameter = addedParameter;
        }

        public override string ToString()
            => IsExisting ? OldIndex.ToString() : (AddedParameter?.ToString() ?? string.Empty);
    }
}
