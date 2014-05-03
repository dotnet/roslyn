// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// This is used while computing the values of constant fields.  Since they can depend on each
    /// other, we need to keep track of which ones we are currently computing in order to avoid (and
    /// report) cycles.
    /// </summary>
    internal sealed class ConstantFieldsInProgress
    {
        private readonly SourceFieldSymbol fieldOpt;
        private readonly HashSet<SourceFieldSymbolWithSyntaxReference> dependencies;

        internal static readonly ConstantFieldsInProgress Empty = new ConstantFieldsInProgress(null, null);

        internal ConstantFieldsInProgress(
            SourceFieldSymbol fieldOpt,
            HashSet<SourceFieldSymbolWithSyntaxReference> dependencies)
        {
            this.fieldOpt = fieldOpt;
            this.dependencies = dependencies;
        }

        public bool IsEmpty
        {
            get { return (object)this.fieldOpt == null; }
        }

        internal void AddDependency(SourceFieldSymbolWithSyntaxReference field)
        {
            this.dependencies.Add(field);
        }
    }
}