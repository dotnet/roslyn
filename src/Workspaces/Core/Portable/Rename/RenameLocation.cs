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
        public readonly bool IsWrittenTo;

        public bool IsRenameInStringOrComment { get { return ContainingLocationForStringOrComment != default(TextSpan); } }

        public bool IsMethodGroupReference { get; }

        public RenameLocation(
            Location location,
            DocumentId documentId,
            bool isCandidateLocation = false,
            bool isMethodGroupReference = false,
            bool isRenamableAliasUsage = false,
            bool isRenamableAccessor = false,
            bool isWrittenTo = false,
            TextSpan containingLocationForStringOrComment = default(TextSpan))
        {
            Location = location;
            DocumentId = documentId;
            IsCandidateLocation = isCandidateLocation;
            IsMethodGroupReference = isMethodGroupReference;
            IsRenamableAliasUsage = isRenamableAliasUsage;
            IsRenamableAccessor = isRenamableAccessor;
            IsWrittenTo = isWrittenTo;
            ContainingLocationForStringOrComment = containingLocationForStringOrComment;
        }

        public RenameLocation(ReferenceLocation referenceLocation, DocumentId documentId)
            : this(referenceLocation.Location, documentId,
                   isCandidateLocation: referenceLocation.IsCandidateLocation && referenceLocation.CandidateReason != CandidateReason.LateBound,
                   isMethodGroupReference: referenceLocation.IsCandidateLocation && referenceLocation.CandidateReason == CandidateReason.MemberGroup,
                   isWrittenTo: referenceLocation.IsWrittenTo)
        {
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
