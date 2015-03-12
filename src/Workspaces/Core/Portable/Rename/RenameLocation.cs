// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Rename
{
    internal struct RenameLocation : IEquatable<RenameLocation>
    {
        public readonly Location Location;
        public readonly DocumentId DocumentId;
        public readonly bool IsCandidateLocation;
        public readonly bool IsRenamableAliasUsage;
        public readonly bool IsRenamableAccessor;
        public readonly TextSpan ContainingLocationForStringOrComment;

        public bool IsRenameInStringOrComment { get { return ContainingLocationForStringOrComment != default(TextSpan); } }

        public bool IsMethodGroupReference { get; private set; }

        public RenameLocation(
            Location location,
            DocumentId documentId,
            bool isCandidateLocation = false,
            bool isMethodGroupReference = false,
            bool isRenamableAliasUsage = false,
            bool isRenamableAccessor = false,
            TextSpan containingLocationForStringOrComment = default(TextSpan))
        {
            this.Location = location;
            this.DocumentId = documentId;
            this.IsCandidateLocation = isCandidateLocation;
            this.IsMethodGroupReference = isMethodGroupReference;
            this.IsRenamableAliasUsage = isRenamableAliasUsage;
            this.IsRenamableAccessor = isRenamableAccessor;
            this.ContainingLocationForStringOrComment = containingLocationForStringOrComment;
        }

        public RenameLocation(ReferenceLocation referenceLocation, DocumentId documentId)
        {
            this.Location = referenceLocation.Location;
            this.DocumentId = documentId;
            this.IsCandidateLocation = referenceLocation.IsCandidateLocation && referenceLocation.CandidateReason != CandidateReason.LateBound;
            this.IsMethodGroupReference = referenceLocation.IsCandidateLocation && referenceLocation.CandidateReason == CandidateReason.MemberGroup;
            this.IsRenamableAliasUsage = false;
            this.IsRenamableAccessor = false;
            this.ContainingLocationForStringOrComment = default(TextSpan);
        }

        public bool Equals(RenameLocation other)
        {
            return Location == other.Location;
        }

        public override bool Equals(object obj)
        {
            if (obj is RenameLocation)
            {
                return Equals((RenameLocation)obj);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return Location.GetHashCode();
        }
    }
}
