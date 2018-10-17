// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    /// <summary>
    /// This analyzer implements <see cref="DiagnosticAnalyzer"/>, but wraps the true formatting analyzer
    /// implementations. Since the compiler doesn't provide the use of workspace-layer APIs for analyzers, we need to
    /// provide our own copy of the workspace assemblies and load them using reflection if they are not already loaded
    /// in the current process.
    /// </summary>
    internal abstract class AbstractFormattingAnalyzer
        : AbstractCodeStyleDiagnosticAnalyzer
    {
        static AbstractFormattingAnalyzer()
        {
            AppDomain.CurrentDomain.AssemblyResolve += HandleAssemblyResolve;
        }

        protected AbstractFormattingAnalyzer()
            : base(
                IDEDiagnosticIds.FormattingDiagnosticId,
                new LocalizableResourceString(nameof(CodeStyleResources.Fix_formatting), CodeStyleResources.ResourceManager, typeof(CodeStyleResources)),
                new LocalizableResourceString(nameof(CodeStyleResources.Fix_formatting), CodeStyleResources.ResourceManager, typeof(CodeStyleResources)))
        {
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Descriptor);

        protected abstract Type GetAnalyzerImplType();

        protected override void InitializeWorker(AnalysisContext context)
        {
            // Create an instance of the true formatting analyzer (which depends on workspace-layer APIs) only after the
            // custom assembly resolver is in place to provide the assemblies if necessary.
            var analyzer = (AbstractFormattingAnalyzerImpl)Activator.CreateInstance(GetAnalyzerImplType(), Descriptor);
            analyzer.InitializeWorker(context);
        }

        private static Assembly HandleAssemblyResolve(object sender, ResolveEventArgs args)
        {
            switch (new AssemblyName(args.Name).Name)
            {
                case "Microsoft.CodeAnalysis.Workspaces":
                case "Microsoft.CodeAnalysis.CSharp.Workspaces":
                case "Microsoft.VisualStudio.CodingConventions":
                    var result = Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(typeof(AbstractFormattingAnalyzer).Assembly.Location), "..\\workspaces", new AssemblyName(args.Name).Name + ".dll"));
                    return result;

                default:
                    return null;
            }
        }
    }
}
