// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Rename;

internal readonly struct RenameLocation(
    Location location,
    DocumentId documentId,
    CandidateReason candidateReason = CandidateReason.None,
    bool isRenamableAliasUsage = false,
    bool isRenamableAccessor = false,
    bool isWrittenTo = false,
    TextSpan containingLocationForStringOrComment = default) : IEquatable<RenameLocation>
{
    public readonly Location Location = location;
    public readonly DocumentId DocumentId = documentId;
    public readonly CandidateReason CandidateReason = candidateReason;
    public readonly bool IsRenamableAliasUsage = isRenamableAliasUsage;
    public readonly bool IsRenamableAccessor = isRenamableAccessor;
    public readonly TextSpan ContainingLocationForStringOrComment = containingLocationForStringOrComment;
    public readonly bool IsWrittenTo = isWrittenTo;

    public bool IsRenameInStringOrComment => ContainingLocationForStringOrComment != default;

    public RenameLocation(ReferenceLocation referenceLocation, DocumentId documentId)
        : this(referenceLocation.Location, documentId,
               candidateReason: referenceLocation.CandidateReason,
               isWrittenTo: referenceLocation.IsWrittenTo)
    {
    }

    public bool Equals(RenameLocation other)
        => Location == other.Location;

    public override bool Equals(object? obj)
    {
        return obj is RenameLocation loc &&
               Equals(loc);
    }

    public override int GetHashCode()
        => Location.GetHashCode();

    internal static bool ShouldRename(RenameLocation location)
        => ShouldRename(location.CandidateReason);

    internal static bool ShouldRename(CandidateReason candidateReason)
    {
        if (candidateReason != CandidateReason.None)
        {
            // When we have a CandidateReason that means (for most reasons) the compiler 
            // encountered some sort of issue when binding the node.  This means we're 
            // less certain about what the code meant and if the node bound to the actual
            // symbol that we're trying to rename.  However, for many of these reasons,
            // even if the code is in error, we can still be confident enough that the 
            // node bound to the symbol we care about.

            switch (candidateReason)
            {
                case CandidateReason.NotATypeOrNamespace:
                    // We had a reference to the symbol in a location where we needed a 
                    // type or namespace.  This is usually a wildly broken situation.  i.e.
                    // now due to hiding, something like a field/property is being referenced
                    // in a type location.  It is highly likely that this should not be
                    // renamed.
                    return false;

                case CandidateReason.NotAnEvent:
                case CandidateReason.NotAWithEventsMember:
                    // it's unlikely that someone would be referencing a non-event in an 
                    // event context.  Likely this location should not be included.
                    return false;

                case CandidateReason.NotAnAttributeType:
                    // It's feasible that someone was referencing some type in an attribute
                    // location before making that type itself descend from System.Attribute.
                    // Still allow this type to be renamed.
                    return true;

                case CandidateReason.WrongArity:
                    // Someone may have provided the wrong number of type arguments to
                    // a type/method when calling it. We should still allow the reference
                    // to be updated.
                    return true;

                case CandidateReason.NotCreatable:
                    // Can happen when someone tries to do something like 'new' an 
                    // abstract type.  We still want to allow renaming this location.
                    return true;

                case CandidateReason.NotReferencable:
                    // Happens when the user does something like directly accessing
                    // the accessor of a normal property.  In this case, we do still
                    // want to allow the rename to happen.
                    return true;

                case CandidateReason.Inaccessible:
                    // Can trivially occur in code that is in an initially broken state
                    // where inaccessible members are being referenced.  We still want
                    // to update these references.
                    return true;

                case CandidateReason.NotAValue:
                case CandidateReason.NotAVariable:
                    // Happens with code like "NS = 1".  If "NS" is binding now to a 
                    // namespace, then it's likely something has gone very wrong (similar to
                    // NotATypeOrNamespace), and we shouldn't update this reference.
                    return false;

                case CandidateReason.NotInvocable:
                    // Happens when something like a variable is being invoked, but the variable
                    // isn't a delegate type.  This may be because the user intends to give 
                    // this value a delegate type, but hasn't done so yet.  We should still allow
                    // renaming this reference for now.
                    return true;

                case CandidateReason.StaticInstanceMismatch:
                    // Similar to 'Inaccessible', the code is currently broken, but it's fairly
                    // clear what the user's intent was.  In this case, we want to update the
                    // references, even though the code isn't valid.
                    return true;

                case CandidateReason.OverloadResolutionFailure:
                    // Here we were renaming a method, and have a reference location that didn't
                    // bind properly to any methods.  This case is simply hard to reason about.
                    // There are times where we might want to rename this, and times when we 
                    // don't.  As overloading methods is very common, we won't update this location
                    // as it might update code the user really doesn't want us to be touching.
                    return false;

                case CandidateReason.LateBound:
                    // This is a late bound call, so we should not update this location. 
                    return false;

                case CandidateReason.Ambiguous:
                    // We should not touch ambiguous code.  We have no way to feel confident that
                    // this really is a location that we should be updating.
                    return false;

                case CandidateReason.MemberGroup:
                    // MemberGroup is not an error case.  It happens in completely legal code,
                    // like nameof(int.ToString).  Because of that, we do want to update the
                    // reference here.
                    return true;

                default:
                    // For cases added in the future, conservatively presume we can't update them.
                    // If we need to we can just add a case above this.
                    return false;
            }
        }

        // If there is no candidate reason, we can rename this reference.
        return true;
    }
}
