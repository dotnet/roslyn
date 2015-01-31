// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    /// <summary>
    /// Represents a debugging session.
    /// </summary>
    internal sealed class DebuggingSession
    {
        private readonly Solution _initialSolution;

        internal DebuggingSession(Solution initialSolution)
        {
            Debug.Assert(initialSolution != null);

            _initialSolution = initialSolution;
        }

        internal Solution InitialSolution
        {
            get { return _initialSolution; }
        }
    }
}
