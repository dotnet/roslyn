// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    /// <summary>
    /// Represents a debugging session.
    /// </summary>
    internal sealed class DebuggingSession
    {
        public readonly Solution InitialSolution;

        private readonly Dictionary<ActiveInstructionId, LinePositionSpan> _activeStatementRemaps = new Dictionary<ActiveInstructionId, LinePositionSpan>();

        internal DebuggingSession(Solution initialSolution)
        {
            Debug.Assert(initialSolution != null);
            InitialSolution = initialSolution;
        }
    }
}
