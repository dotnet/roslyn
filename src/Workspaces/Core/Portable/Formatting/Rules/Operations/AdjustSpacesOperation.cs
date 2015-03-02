﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting.Rules
{
    /// <summary>
    /// indicate how many spaces are needed between two spaces
    /// </summary>
    internal sealed class AdjustSpacesOperation
    {
        internal AdjustSpacesOperation(int space, AdjustSpacesOption option)
        {
            Contract.ThrowIfFalse(space >= 0);

            this.Space = space;
            this.Option = option;
        }

        public int Space { get; }
        public AdjustSpacesOption Option { get; }
    }
}
