// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics
{

    internal abstract class AbstractDiagnosticPropertiesService : IDiagnosticPropertiesService
    {
        public ImmutableDictionary<string, string> GetAdditionalProperties(Diagnostic diagnostic)
            => GetAdditionalProperties(diagnostic, GetCompilation());

        protected abstract Compilation GetCompilation();

        private ImmutableDictionary<string, string> GetAdditionalProperties(
            Diagnostic diagnostic,
            Compilation compilation)
        {
            var assemblyIds = compilation.GetUnreferencedAssemblyIdentities(diagnostic);
            var requiredVersion = Compilation.GetRequiredLanguageVersion(diagnostic);
            if (assemblyIds.IsDefaultOrEmpty && requiredVersion == null)
            {
                return null;
            }

            var result = ImmutableDictionary.CreateBuilder<string, string>();
            if (!assemblyIds.IsDefaultOrEmpty)
            {
                result.Add(
                    DiagnosticPropertyConstants.UnreferencedAssemblyIdentity,
                    assemblyIds[0].GetDisplayName());
            }

            if (requiredVersion != null)
            {
                result.Add(
                    DiagnosticPropertyConstants.RequiredLanguageVersion,
                    requiredVersion);
            }

            return result.ToImmutable();
        }
    }
}
