// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal abstract class AbstractFormattingAnalyzer
        : AbstractCodeStyleDiagnosticAnalyzer
    {
        internal const string FormattingDiagnosticId = "IDE0051";

        static AbstractFormattingAnalyzer()
        {
            AppDomain.CurrentDomain.AssemblyResolve += HandleAssemblyResolve;
        }

        protected AbstractFormattingAnalyzer()
            : base(
                FormattingDiagnosticId,
                new LocalizableResourceString(nameof(CodeStyleResources.Formatting_analyzer_title), CodeStyleResources.ResourceManager, typeof(CodeStyleResources)),
                new LocalizableResourceString(nameof(CodeStyleResources.Formatting_analyzer_message), CodeStyleResources.ResourceManager, typeof(CodeStyleResources)))
        {
        }

        protected abstract Type GetAnalyzerImplType();

        protected override void InitializeWorker(AnalysisContext context)
        {
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
