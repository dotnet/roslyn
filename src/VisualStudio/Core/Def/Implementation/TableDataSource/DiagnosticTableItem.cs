// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal sealed class DiagnosticTableItem : TableItem
    {
        public readonly DiagnosticData Data;

        public DiagnosticTableItem(Workspace workspace, SharedInfoCache cache, DiagnosticData data)
            : base(workspace, cache)
        {
            Contract.ThrowIfNull(data);
            Data = data;
        }

        public override TableItem WithCache(SharedInfoCache cache)
            => new DiagnosticTableItem(Workspace, cache, Data);

        public override DocumentId PrimaryDocumentId
            => Data.DocumentId;

        public override ProjectId ProjectId
            => Data.ProjectId;

        public override LinePosition GetTrackingPosition()
            => new LinePosition(Data.DataLocation?.OriginalStartLine ?? 0, Data.DataLocation?.OriginalStartColumn ?? 0);

        public override int GetDeduplicationKey()
        {
            var diagnostic = Data;

            // location-less or project level diagnostic:
            if (diagnostic.DataLocation == null ||
                diagnostic.DataLocation.OriginalFilePath == null ||
                diagnostic.DocumentId == null)
            {
                return diagnostic.GetHashCode();
            }

            return
                Hash.Combine(diagnostic.DataLocation.OriginalStartColumn,
                Hash.Combine(diagnostic.DataLocation.OriginalStartLine,
                Hash.Combine(diagnostic.DataLocation.OriginalEndColumn,
                Hash.Combine(diagnostic.DataLocation.OriginalEndLine,
                Hash.Combine(diagnostic.DataLocation.OriginalFilePath,
                Hash.Combine(diagnostic.IsSuppressed,
                Hash.Combine(diagnostic.Id.GetHashCode(), diagnostic.Message.GetHashCode())))))));
        }

        public override LinePosition GetOriginalPosition()
            => new LinePosition(Data.DataLocation?.OriginalStartLine ?? 0, Data.DataLocation?.OriginalStartColumn ?? 0);

        public override string GetOriginalFilePath()
            => Data.DataLocation?.OriginalFilePath;

        public override bool EqualsModuloLocation(TableItem other)
        {
            if (!(other is DiagnosticTableItem otherDiagnosticItem))
            {
                return false;
            }

            var diagnostic = Data;
            var otherDiagnostic = otherDiagnosticItem.Data;

            // everything same except location
            return diagnostic.Id == otherDiagnostic.Id &&
                   diagnostic.ProjectId == otherDiagnostic.ProjectId &&
                   diagnostic.DocumentId == otherDiagnostic.DocumentId &&
                   diagnostic.Category == otherDiagnostic.Category &&
                   diagnostic.Severity == otherDiagnostic.Severity &&
                   diagnostic.WarningLevel == otherDiagnostic.WarningLevel &&
                   diagnostic.Message == otherDiagnostic.Message;
        }
    }
}
