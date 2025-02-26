// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Analyzer.Utilities
{
    internal interface ISyntaxKinds
    {
        int EndOfFileToken { get; }

        int ExpressionStatement { get; }
        int LocalDeclarationStatement { get; }

        int VariableDeclarator { get; }
    }
}
