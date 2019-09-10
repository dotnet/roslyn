// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
