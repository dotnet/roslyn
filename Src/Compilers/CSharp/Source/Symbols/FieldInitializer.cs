// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a field initializer or a global statement in script code.
    /// </summary>
    internal struct FieldInitializer
    {
        /// <summary>
        /// The field being initialized or null if this is a global statement.
        /// </summary>
        internal readonly FieldSymbol Field;

        /// <summary>
        /// A reference to <see cref="EqualsValueClauseSyntax"/> or <see cref="GlobalStatementSyntax"/>.
        /// </summary>
        internal readonly SyntaxReference Syntax;

        public FieldInitializer(FieldSymbol field, SyntaxReference syntax)
        {
            Debug.Assert(((object)field != null) || (syntax != null));
            Field = field;
            Syntax = syntax;
        }
    }

    internal struct FieldInitializerInfo
    {
        public readonly FieldInitializer Initializer;
        public readonly Binder Binder;
        public readonly EqualsValueClauseSyntax EqualsValue;

        public FieldInitializerInfo(FieldInitializer initializer, Binder binder, EqualsValueClauseSyntax equalsValue)
        {
            Initializer = initializer;
            Binder = binder;
            EqualsValue = equalsValue;
        }
    }
}
