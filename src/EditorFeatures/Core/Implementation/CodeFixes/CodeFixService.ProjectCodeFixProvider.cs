// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    using TypeInfo = System.Reflection.TypeInfo;

    internal partial class CodeFixService
    {
        private class ProjectCodeFixProvider
        {
            private readonly AnalyzerReference _reference;
            private ImmutableDictionary<string, ImmutableArray<CodeFixProvider>> _fixersPerLanguage;

            public ProjectCodeFixProvider(AnalyzerReference reference)
            {
                _reference = reference;
                _fixersPerLanguage = ImmutableDictionary<string, ImmutableArray<CodeFixProvider>>.Empty;
            }

            public ImmutableArray<CodeFixProvider> GetFixers(string language)
            {
                return ImmutableInterlocked.GetOrAdd(ref _fixersPerLanguage, language, CreateFixers);
            }

            private ImmutableArray<CodeFixProvider> CreateFixers(string language)
            {
                // check whether the analyzer reference knows how to return fixers directly.
                if (_reference is ICodeFixProviderFactory codeFixProviderFactory)
                {
                    return codeFixProviderFactory.GetFixers();
                }

                // otherwise, see whether we can pick it up from reference itself
                if (!(_reference is AnalyzerFileReference analyzerFileReference))
                {
                    return ImmutableArray<CodeFixProvider>.Empty;
                }

                var builder = ArrayBuilder<CodeFixProvider>.GetInstance();

                try
                {
                    var analyzerAssembly = analyzerFileReference.GetAssembly();
                    var typeInfos = analyzerAssembly.DefinedTypes;

                    foreach (var typeInfo in typeInfos)
                    {
                        if (typeInfo.IsSubclassOf(typeof(CodeFixProvider)))
                        {
                            try
                            {
                                var attribute = typeInfo.GetCustomAttribute<ExportCodeFixProviderAttribute>();
                                if (attribute != null)
                                {
                                    if (attribute.Languages == null ||
                                        attribute.Languages.Length == 0 ||
                                        attribute.Languages.Contains(language))
                                    {
                                        builder.Add((CodeFixProvider)Activator.CreateInstance(typeInfo.AsType()));
                                    }
                                }
                            }
                            catch
                            {
                            }
                        }
                    }
                }
                catch
                {
                    // REVIEW: is the below message right?
                    // NOTE: We could report "unable to load analyzer" exception here but it should have been already reported by DiagnosticService.
                }

                return builder.ToImmutableAndFree();
            }
        }
    }
}
