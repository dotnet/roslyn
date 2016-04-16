// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Versions
{
    internal static class Extensions
    {
        public static bool CanReusePersistedTextVersion(this Document document, VersionStamp textVersion, VersionStamp persistedVersion)
        {
            var canReuse = VersionStamp.CanReusePersistedVersion(textVersion, persistedVersion);

            PersistedVersionStampLogger.LogPersistedTextVersionUsage(canReuse);
            return canReuse;
        }

        public static bool CanReusePersistedSyntaxTreeVersion(this Document document, VersionStamp syntaxVersion, VersionStamp persistedVersion)
        {
            var canReuse = VersionStamp.CanReusePersistedVersion(syntaxVersion, persistedVersion);

            PersistedVersionStampLogger.LogPersistedSyntaxTreeVersionUsage(canReuse);
            return canReuse;
        }

        public static bool CanReusePersistedProjectVersion(this Project project, VersionStamp projectVersion, VersionStamp persistedVersion)
        {
            var canReuse = VersionStamp.CanReusePersistedVersion(projectVersion, persistedVersion);

            PersistedVersionStampLogger.LogPersistedProjectVersionUsage(canReuse);
            return canReuse;
        }

        public static bool CanReusePersistedDependentProjectVersion(this Project project, VersionStamp dependentProjectVersion, VersionStamp persistedVersion)
        {
            var canReuse = VersionStamp.CanReusePersistedVersion(dependentProjectVersion, persistedVersion);

            PersistedVersionStampLogger.LogPersistedDependentProjectVersionUsage(canReuse);
            return canReuse;
        }

        public static bool CanReusePersistedSemanticVersion(
            this Project project, VersionStamp projectVersion, VersionStamp semanticVersion, VersionStamp persistedVersion)
        {
            var canReuse = CanReusePersistedSemanticVersionInternal(
                project, projectVersion, semanticVersion, persistedVersion, (s, p, v) => s.GetInitialProjectVersionFromSemanticVersion(p, v));

            PersistedVersionStampLogger.LogPersistedSemanticVersionUsage(canReuse);
            return canReuse;
        }

        public static bool CanReusePersistedDependentSemanticVersion(
            this Project project, VersionStamp dependentProjectVersion, VersionStamp dependentSemanticVersion, VersionStamp persistedVersion)
        {
            var canReuse = CanReusePersistedSemanticVersionInternal(
                project, dependentProjectVersion, dependentSemanticVersion, persistedVersion, (s, p, v) => s.GetInitialDependentProjectVersionFromDependentSemanticVersion(p, v));

            PersistedVersionStampLogger.LogPersistedDependentSemanticVersionUsage(canReuse);
            return canReuse;
        }

        private static bool CanReusePersistedSemanticVersionInternal(
            Project project,
            VersionStamp projectVersion,
            VersionStamp semanticVersion,
            VersionStamp persistedVersion,
            Func<ISemanticVersionTrackingService, Project, VersionStamp, VersionStamp> versionGetter)
        {
            // * NOTE * 
            // Disabled semantic version tracking
            // we need better version for it to reliably work.
            //
            // see tracking issue here : https://github.com/dotnet/roslyn/issues/2311
            return VersionStamp.CanReusePersistedVersion(semanticVersion, persistedVersion);
        }
    }
}
