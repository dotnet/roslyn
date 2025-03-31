// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundCollectionExpressionWithElement
    {
        internal void GetArguments(AnalyzedArguments analyzedArguments)
        {
            Debug.Assert(analyzedArguments.Arguments.IsEmpty);
            analyzedArguments.Arguments.AddRange(Arguments);
            if (!ArgumentNamesOpt.IsDefault)
            {
                analyzedArguments.Names.AddRange(ArgumentNamesOpt);
            }
            if (!ArgumentRefKindsOpt.IsDefault)
            {
                analyzedArguments.RefKinds.AddRange(ArgumentRefKindsOpt);
            }
        }
    }
}
