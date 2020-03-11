// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.DesignerAttribute
{
    /// <summary>
    /// Interface to allow host (VS) to inform the OOP service to start incrementally analyzing and
    /// reporting results back to the host.
    /// </summary>
    internal interface IRemoteNewDesignerAttributeService
    {
        Task ScanForDesignerAttributesAsync(CancellationToken cancellation);
    }

    /// <summary>
    /// Callback the host (VS) passes to the OOP service to allow it to send batch notifications
    /// about designer attribute info.  There is no guarantee that the host will have done anything
    /// with this data when the callback returns, only that it will try to inform the project system
    /// about the designer attribute info in the future.
    /// </summary>
    internal interface INewDesignerAttributeServiceCallback
    {
        Task RegisterDesignerAttributesAsync(IList<DesignerInfo> infos, CancellationToken cancellationToken);
    }

    internal struct DesignerInfo
    {
        /// <summary>
        /// Our current serialization format.  Increment whenever it changes so that we don't read
        /// bogus data when reading older persisted data.
        /// </summary>
        private const string SerializationFormat = "1";

        /// <summary>
        /// The category specified in a <c>[DesignerCategory("...")]</c> attribute.
        /// </summary>
        public string? Category;

        /// <summary>
        /// The document this <see cref="Category"/> applies to.
        /// </summary>
        public DocumentId DocumentId;

        public static (string? category, VersionStamp projectVersion) TryRead(ObjectReader reader)
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

        public void WriteTo(ObjectWriter writer, VersionStamp projectVersion)
        {
            writer.WriteString(SerializationFormat);
            writer.WriteString(Category);
            projectVersion.WriteTo(writer);
        }
    }
}
