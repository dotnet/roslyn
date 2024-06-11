// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.Pythia.Api
{
    internal readonly struct PythiaEditDistanceWrapper(string str) : IDisposable
    {
        private readonly EditDistance _underlyingObject = new EditDistance(str);

        public double GetEditDistance(string target)
            => _underlyingObject.GetEditDistance(target);

        public void Dispose()
            => _underlyingObject.Dispose();
    }
}
