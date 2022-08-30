// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis
{
    internal class SyntaxTreeException : Exception
    {
        // Used for analyzing dumps
#pragma warning disable IDE0052 // Remove unread private members
        private readonly SyntaxTree _syntaxTree;
#pragma warning restore IDE0052 // Remove unread private members

        public SyntaxTreeException(string message, SyntaxTree syntaxTree) : base(message)
        {
            _syntaxTree = syntaxTree;
        }
    }
}
