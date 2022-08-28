// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim
{
    internal class EntryPointFinder : AbstractEntryPointFinder
    {
        private readonly INamedTypeSymbol? _task;
        private readonly INamedTypeSymbol? _taskOf;

        public EntryPointFinder(Compilation? compilation)
        {
            _task = compilation?.TaskType();
            _taskOf = compilation?.TaskOfTType();
        }

        [Obsolete("FindEntryPoints on a INamespaceSymbol is deprecated, please pass in the Compilation instead.")]
        public static IEnumerable<INamedTypeSymbol> FindEntryPoints(INamespaceSymbol symbol)
        {
            // This differs from the VB implementation (Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim.EntryPointFinder)
            // because we don't ever consider forms entry points.
            // Techinically, this is wrong but it just doesn't matter since the
            // ref assemblies are unlikely to have a random Main() method that matches
            var visitor = new EntryPointFinder(symbol.ContainingCompilation);
            visitor.Visit(symbol);
            return visitor.EntryPoints;
        }

        public static IEnumerable<INamedTypeSymbol> FindEntryPoints(Compilation compilation)
        {
            var visitor = new EntryPointFinder(compilation);
            visitor.Visit(compilation.SourceModule.GlobalNamespace);
            return visitor.EntryPoints;
        }

        protected override bool IsEntryPoint(IMethodSymbol methodSymbol)
            => methodSymbol.IsCSharpEntryPoint(_task, _taskOf);
    }
}
