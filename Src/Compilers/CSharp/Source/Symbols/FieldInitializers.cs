// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a field initializers from particular type declaration.
    /// </summary>
    internal struct FieldInitializers
    {
        /// <summary>
        /// A reference to the <see cref="TypeDeclarationSyntax"/>.
        /// </summary>
        internal readonly SyntaxReference TypeDeclarationSyntax;

        internal readonly ImmutableArray<FieldInitializer> Initializers;

        public FieldInitializers(SyntaxReference typeDeclarationSyntax, ImmutableArray<FieldInitializer> initializers)
        {
            Debug.Assert(!initializers.IsDefaultOrEmpty);
            TypeDeclarationSyntax = typeDeclarationSyntax;
            Initializers = initializers;
        }
    }
}
