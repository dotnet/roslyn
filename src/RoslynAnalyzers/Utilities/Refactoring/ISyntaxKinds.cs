// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

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
