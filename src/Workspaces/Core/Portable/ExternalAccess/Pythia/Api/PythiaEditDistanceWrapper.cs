// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.Pythia.Api
{
    internal readonly struct PythiaEditDistanceWrapper : IDisposable
    {
        private readonly EditDistance _underlyingObject;

        public PythiaEditDistanceWrapper(string str)
        {
            _underlyingObject = new EditDistance(str);
        }

        public double GetEditDistance(string target)
            => _underlyingObject.GetEditDistance(target);

        public void Dispose()
            => _underlyingObject.Dispose();
    }
}
