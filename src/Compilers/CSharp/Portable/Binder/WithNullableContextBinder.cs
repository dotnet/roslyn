// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class WithNullableContextBinder : Binder
    {
        private readonly SyntaxTree _syntaxTree;
        private readonly int _position;

        internal WithNullableContextBinder(SyntaxTree syntaxTree, int position, Binder next)
            : base(next)
        {
            Debug.Assert(syntaxTree != null);
            Debug.Assert(position >= 0);
            _syntaxTree = syntaxTree;
            _position = position;
        }

        internal override bool AreNullableAnnotationsGloballyEnabled()
        {
            return Next.AreNullableAnnotationsEnabled(_syntaxTree, _position);
        }
    }
}
