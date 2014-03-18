// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Describes a command line analyzer assembly specification.
    /// </summary>
    public struct CommandLineAnalyzer
    {
        private readonly string analyzer;

        public CommandLineAnalyzer(string analyzer)
        {
            this.analyzer = analyzer;
        }

        /// <summary>
        /// Assembly file path.
        /// </summary>
        public string Analyzer
        {
            get
            {
                return analyzer;
            }
        }

        /// <summary>
        /// Resolves this command line analyzer path to a <see cref="IEnumerable{IDiagnosticAnalyzer}"/> using given file resolver.
        /// </summary>
        /// <param name="fileResolver">The file resolver to use for assembly name and relative path resolution.</param>
        /// <param name="diagnosticsOpt">Optional diagnostics list for storing diagnostics.</param>
        /// <param name="messageProviderOpt">Optional <see cref="CommonMessageProvider"/> for generating diagnostics.</param>
        /// <returns>Returns null if the path is invalid. Otherwise returns the list of analyzer factories from the dll.</returns>
        internal IEnumerable<IDiagnosticAnalyzer> Resolve(FileResolver fileResolver, List<DiagnosticInfo> diagnosticsOpt, CommonMessageProvider messageProviderOpt)
        {
            Debug.Assert(fileResolver != null);

            string fullPath = fileResolver.ResolveMetadataFileChecked(this.analyzer, baseFilePath: null);
            if (fullPath == null)
            {
                if (diagnosticsOpt != null)
                {
                    diagnosticsOpt.Add(new DiagnosticInfo(messageProviderOpt, messageProviderOpt.ERR_MetadataFileNotFound, this.analyzer));
                }

                return null;
            }
            else
            {
                return ResolveAnalyzers(fullPath, this.analyzer, diagnosticsOpt, messageProviderOpt);
            }
        }

        private IEnumerable<IDiagnosticAnalyzer> ResolveAnalyzers(string fullPath, string analyzer, List<DiagnosticInfo> diagnosticsOpt, CommonMessageProvider messageProviderOpt)
        {
            // Using Assembly.LoadFrom to load into the Load-From context. This ensures that:
            // 1 . The analyzer and it's dependencies don't have to be in the probing path of this process
            // 2 . When multiple assemblies with the same identity are loaded (even from different paths), we return
            // the same assembly and avoid bloat. This does mean that strong identity for analyzers is important.
            Type[] types;
            try
            {
                Assembly analyzerAssembly = Assembly.LoadFrom(fullPath);
                types= analyzerAssembly.GetTypes();
            }
            catch (Exception e)
            {
                diagnosticsOpt.Add(new DiagnosticInfo(messageProviderOpt, messageProviderOpt.WRN_UnableToLoadAnalyzer, analyzer, e.Message));
                return null;
            }
            
            var analyzers = new List<IDiagnosticAnalyzer>();
            foreach (var type in types)
            {
                if (type.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IDiagnosticAnalyzer)) && type.IsDefined(typeof(DiagnosticAnalyzerAttribute)))
                {
                    try
                    {
                        analyzers.Add((IDiagnosticAnalyzer)Activator.CreateInstance(type));
                    }
                    catch (Exception e)
                    {
                        diagnosticsOpt.Add(new DiagnosticInfo(messageProviderOpt, messageProviderOpt.WRN_AnalyzerCannotBeCreated, type, analyzer, e.Message));
                    }
                }
            }

            return analyzers;
        }
    }
}
