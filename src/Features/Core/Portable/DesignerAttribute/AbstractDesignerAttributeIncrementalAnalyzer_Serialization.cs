// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.DesignerAttribute;
using Microsoft.CodeAnalysis.ErrorReporting;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.DesignerAttribute
{
    internal partial class AbstractDesignerAttributeIncrementalAnalyzer
    {
        /// <summary>
        /// Our current serialization format.  Increment whenever it changes so that we don't read
        /// bogus data when reading older persisted data.
        /// </summary>
        private const string SerializationFormat = "1";

        private static (string? category, VersionStamp projectVersion) TryReadPersistedInfo(ObjectReader reader)
        {
            try
            {
                // if we couldn't get a reader then we have no persisted category/version to read out.
                if (reader == null)
                    return default;

                // check to make sure whatever we persisted matches our current format for this info
                // type. If not, then we can't read this out.
                var format = reader.ReadString();
                if (format != SerializationFormat)
                    return default;

                // Looks good, pull out the stored data.
                var category = reader.ReadString();
                var projectVersion = VersionStamp.ReadFrom(reader);

                return (category, projectVersion);
            }
            catch (Exception e) when (FatalError.ReportWithoutCrash(e))
            {
                // can happen if the DB got edited outside of our control.
                return default;
            }
        }

        private static void PersistInfoTo(ObjectWriter writer, DesignerAttributeData data, VersionStamp projectVersion)
        {
            writer.WriteString(SerializationFormat);
            writer.WriteString(data.Category);
            projectVersion.WriteTo(writer);
        }
    }
}
