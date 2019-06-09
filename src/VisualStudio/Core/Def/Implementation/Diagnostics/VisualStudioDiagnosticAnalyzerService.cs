// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics
{
    [Export(typeof(IVisualStudioDiagnosticAnalyzerService))]
    internal partial class VisualStudioDiagnosticAnalyzerService : IVisualStudioDiagnosticAnalyzerService
    {
        private readonly VisualStudioWorkspace _workspace;
        private readonly IDiagnosticAnalyzerService _diagnosticService;

        [ImportingConstructor]
        public VisualStudioDiagnosticAnalyzerService(VisualStudioWorkspace workspace, IDiagnosticAnalyzerService diagnosticService)
        {
            _workspace = workspace;
            _diagnosticService = diagnosticService;
        }

        // *DO NOT DELETE*
        // This is used by Ruleset Editor from ManagedSourceCodeAnalysis.dll.
        public IReadOnlyDictionary<string, IEnumerable<DiagnosticDescriptor>> GetAllDiagnosticDescriptors(IVsHierarchy hierarchyOpt)
            => GetAllDiagnosticDescriptors(hierarchyOpt != null && hierarchyOpt.TryGetProjectGuid(out var guid) ? guid : Guid.Empty);

        private IReadOnlyDictionary<string, IEnumerable<DiagnosticDescriptor>> GetAllDiagnosticDescriptors(Guid projectGuid)
        {
            if (projectGuid == Guid.Empty)
            {
                return Transform(_diagnosticService.CreateDiagnosticDescriptorsPerReference(projectOpt: null));
            }

            // Analyzers are only supported for C# and VB currently.
            var projects = _workspace.CurrentSolution.Projects
                .Where(p => p.Language == LanguageNames.CSharp || p.Language == LanguageNames.VisualBasic)
                .Where(p => _workspace.GetProjectGuid(p.Id) == projectGuid);

            if (projects.Count() <= 1)
            {
                return Transform(_diagnosticService.CreateDiagnosticDescriptorsPerReference(projects.FirstOrDefault()));
            }
            else
            {
                // Multiple workspace projects map to the same project guid, return a union of descriptors for all projects.
                // For example, this can happen for web projects where we create on the fly projects for aspx files.
                var descriptorsMap = ImmutableDictionary.CreateBuilder<string, IEnumerable<DiagnosticDescriptor>>();
                foreach (var project in projects)
                {
                    var newDescriptorTuples = _diagnosticService.CreateDiagnosticDescriptorsPerReference(project);
                    foreach (var kvp in newDescriptorTuples)
                    {
                        if (descriptorsMap.TryGetValue(kvp.Key, out var existingDescriptors))
                        {
                            descriptorsMap[kvp.Key] = existingDescriptors.Concat(kvp.Value).Distinct();
                        }
                        else
                        {
                            descriptorsMap[kvp.Key] = kvp.Value;
                        }
                    }
                }

                return descriptorsMap.ToImmutable();
            }
        }

        private IReadOnlyDictionary<string, IEnumerable<DiagnosticDescriptor>> Transform(
            ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>> map)
        {
            // unfortunately, we had to do this since ruleset editor and us are set to use this signature
            return map.ToDictionary(kv => kv.Key, kv => (IEnumerable<DiagnosticDescriptor>)kv.Value);
        }
    }
}
