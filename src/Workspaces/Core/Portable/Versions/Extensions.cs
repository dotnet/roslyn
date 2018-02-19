// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    }
}
