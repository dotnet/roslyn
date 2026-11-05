// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Roslyn.Utilities;

namespace Roslyn.LanguageServer.Protocol;

internal partial class CodeDescription : IEquatable<CodeDescription>
{
    public static bool operator ==(CodeDescription? left, CodeDescription? right) => Equals(left, right);
    public static bool operator !=(CodeDescription? left, CodeDescription? right) => !Equals(left, right);
    public bool Equals(CodeDescription? other) => other is not null && Href == other.Href;
    public override bool Equals(object? obj) => obj is CodeDescription other && Equals(other);
    public override int GetHashCode() => Href?.GetHashCode() ?? 53;
}

internal partial class Diagnostic : IEquatable<Diagnostic>
{
    public static bool operator ==(Diagnostic? left, Diagnostic? right) => Equals(left, right);
    public static bool operator !=(Diagnostic? left, Diagnostic? right) => !Equals(left, right);

    public bool Equals(Diagnostic? other)
        => other is not null
        && Range == other.Range
        && Severity == other.Severity
        && Equals(Code?.Value, other.Code?.Value)
        && CodeDescription == other.CodeDescription
        && string.Equals(Source, other.Source, StringComparison.Ordinal)
        && Equals(Message?.Value, other.Message?.Value)
        && (Tags is null ? other.Tags is null : other.Tags is not null && Tags.SequenceEqual(other.Tags))
        && Equals(Data, other.Data);

    public override bool Equals(object? obj) => obj is Diagnostic other && Equals(other);

    public override int GetHashCode() =>
#if NET
        HashCode.Combine(Range, Severity, Code?.Value, Source, Message?.Value, Hash.CombineValues(Tags), CodeDescription, Data);
#else
        Hash.Combine(Range,
        Hash.Combine((int)(Severity ?? 0),
        Hash.Combine(Code?.Value.GetHashCode() ?? 0,
        Hash.Combine(Source,
        Hash.Combine(Message?.Value,
        Hash.Combine(Hash.CombineValues(Tags),
        Hash.Combine(CodeDescription?.GetHashCode() ?? 0, Data?.GetHashCode() ?? 0)))))));
#endif
}

internal partial class Location : IEquatable<Location>
{
    public bool Equals(Location? other)
        => other is not null && Uri is not null && other.Uri is not null
        && Uri.Equals(other.Uri) && EqualityComparer<Range>.Default.Equals(Range, other.Range);

    public override bool Equals(object? obj) => obj is Location other && Equals(other);

    public override int GetHashCode()
    {
        var hashCode = 1486144663;
        hashCode = (hashCode * -1521134295) + Uri.GetHashCode();
        return (hashCode * -1521134295) + EqualityComparer<Range>.Default.GetHashCode(Range);
    }
}

internal partial class LocationLink : IEquatable<LocationLink>
{
    public bool Equals(LocationLink? other)
        => other is not null
        && EqualityComparer<Range?>.Default.Equals(OriginSelectionRange, other.OriginSelectionRange)
        && EqualityComparer<DocumentUri>.Default.Equals(TargetUri, other.TargetUri)
        && EqualityComparer<Range>.Default.Equals(TargetRange, other.TargetRange)
        && EqualityComparer<Range>.Default.Equals(TargetSelectionRange, other.TargetSelectionRange);

    public override bool Equals(object? obj) => obj is LocationLink other && Equals(other);

    public override int GetHashCode() =>
#if NET
        HashCode.Combine(OriginSelectionRange, TargetUri, TargetRange, TargetSelectionRange);
#else
        Hash.Combine(OriginSelectionRange,
        Hash.Combine(TargetUri.GetHashCode(),
        Hash.Combine(TargetRange, TargetSelectionRange.GetHashCode())));
#endif
}

internal partial class SymbolInformation : IEquatable<SymbolInformation>
{
    public bool Equals(SymbolInformation? other)
        => other is not null && Name == other.Name && Kind == other.Kind
        && (Tags is null ? other.Tags is null : other.Tags is not null && Tags.SequenceEqual(other.Tags))
        && Deprecated == other.Deprecated
        && EqualityComparer<Location>.Default.Equals(Location, other.Location)
        && ContainerName == other.ContainerName;

    public override bool Equals(object? obj) => obj is SymbolInformation other && Equals(other);

    public override int GetHashCode() =>
#if NET
        HashCode.Combine(Name, Kind, Hash.CombineValues(Tags), Deprecated, Location, ContainerName);
#else
        Hash.Combine(Name,
        Hash.Combine((int)Kind,
        Hash.Combine(Hash.CombineValues(Tags),
        Hash.Combine(Deprecated,
        Hash.Combine(ContainerName, Location?.GetHashCode() ?? 0)))));
#endif
}

internal partial class TextDocumentIdentifier : IEquatable<TextDocumentIdentifier>
{
    public static bool operator ==(TextDocumentIdentifier? left, TextDocumentIdentifier? right) => Equals(left, right);
    public static bool operator !=(TextDocumentIdentifier? left, TextDocumentIdentifier? right) => !Equals(left, right);
    public bool Equals(TextDocumentIdentifier? other) => other is not null && Uri == other.Uri;
    public override bool Equals(object? obj) => obj is TextDocumentIdentifier other && Equals(other);
    public override int GetHashCode() => Uri?.GetHashCode() ?? 89;
}

internal partial class VersionedTextDocumentIdentifier : IEquatable<VersionedTextDocumentIdentifier>
{
    public static bool operator ==(VersionedTextDocumentIdentifier? left, VersionedTextDocumentIdentifier? right) => Equals(left, right);
    public static bool operator !=(VersionedTextDocumentIdentifier? left, VersionedTextDocumentIdentifier? right) => !Equals(left, right);

    public bool Equals(VersionedTextDocumentIdentifier? other)
        => other is not null && Version == other.Version && base.Equals(other);

    public override bool Equals(object? obj) => obj is VersionedTextDocumentIdentifier other && Equals(other);
    public override int GetHashCode() => Version.GetHashCode() ^ (base.GetHashCode() * 79);
    public override string ToString() => base.ToString() + "|" + Version.ToString(CultureInfo.InvariantCulture);
}

internal partial class OptionalVersionedTextDocumentIdentifier : IEquatable<OptionalVersionedTextDocumentIdentifier>
{
    public static bool operator ==(OptionalVersionedTextDocumentIdentifier? left, OptionalVersionedTextDocumentIdentifier? right) => Equals(left, right);
    public static bool operator !=(OptionalVersionedTextDocumentIdentifier? left, OptionalVersionedTextDocumentIdentifier? right) => !Equals(left, right);

    public bool Equals(OptionalVersionedTextDocumentIdentifier? other)
        => other is not null && Version == other.Version && base.Equals(other);

    public override bool Equals(object? obj) => obj is OptionalVersionedTextDocumentIdentifier other && Equals(other);
    public override int GetHashCode() => Version is null ? 89 : Version.GetHashCode() ^ (base.GetHashCode() * 79);
    public override string ToString() => base.ToString() + "|" + Version?.ToString(CultureInfo.InvariantCulture);
}

internal partial class VSInternalIconMapping : IEquatable<VSInternalIconMapping>
{
    public bool Equals(VSInternalIconMapping? other)
    {
        if (other is null || (Images is null) != (other.Images is null) || (Tags is null) != (other.Tags is null))
            return false;

        if (Images is not null && other.Images is not null && Images.Length == other.Images.Length)
        {
            for (var i = 0; i < Images.Length; i++)
            {
                if (Images[i].Id != other.Images[i].Id || Images[i].Guid != other.Images[i].Guid)
                    return false;
            }
        }

        if (Tags is not null && other.Tags is not null && Tags.Length == other.Tags.Length)
        {
            for (var i = 0; i < Tags.Length; i++)
            {
                if (!string.Equals(Tags[i], other.Tags[i], StringComparison.Ordinal))
                    return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj) => obj is VSInternalIconMapping other && Equals(other);

    public override int GetHashCode()
    {
        var hashCode = 1825600323;
        if (Images is not null)
        {
            foreach (var image in Images)
            {
                hashCode = (hashCode * -1521134295) + image.Guid.GetHashCode();
                hashCode = (hashCode * -1521134295) + image.Id.GetHashCode();
            }
        }

        if (Tags is not null)
        {
            foreach (var tag in Tags)
                hashCode = (hashCode * -1521134295) + StringComparer.Ordinal.GetHashCode(tag);
        }

        return hashCode;
    }
}
