// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using static Microsoft.VisualStudio.LanguageServices.Options.NamingSettingsConstants;

namespace Microsoft.VisualStudio.LanguageServices.Options;

internal static class NamingSettingsIdentity
{
    private static readonly Dictionary<Guid, string> s_builtinIds = new()
    {
        [new("5c545a62-b14d-460a-88d8-e936c0a39316")] = "class",
        [new("23d856b4-5089-4405-83ce-749aada99153")] = "interface",
        [new("d1796e78-ff66-463f-8576-eb46416060c0")] = "struct",
        [new("d8af8dc6-1ade-441d-9947-8946922e198a")] = "enum",
        [new("408a3347-b908-4b54-a954-1355e64c1de3")] = "delegate",
        [new("830657f6-e7e5-4830-b328-f109d3b6c165")] = "event",
        [new("390caed4-f0a9-42bb-adbb-b44c4a302a22")] = "method",
        [new("af410767-f189-47c6-b140-aeccf1ff242e")] = "private_method",
        [new("8076757e-6a4a-47f1-9b4b-ae8a3284e987")] = "abstract_method",
        [new("16133061-a8e7-4392-92c3-1d93cd54c218")] = "static_method",
        [new("da6a2919-5aa6-4ad1-a24d-576776ed3974")] = "property",
        [new("b24a91ce-3501-4799-b6df-baf044156c83")] = "public_or_protected_field",
        [new("70af42cb-1741-4027-969c-9edc4877d965")] = "static_field",
        [new("10790aa6-0a0b-432d-a52d-d252ca92302b")] = "private_or_internal_field",
        [new("ac995be4-88de-4771-9dcc-a456a7c02d89")] = "private_or_internal_static_field",
        [new("2c07f5bf-bc81-4c2b-82b4-ae9b3ffd0ba4")] = "types",
        [new("5f3ddba1-279f-486c-801e-5c097c36dd85")] = "non_field_members",
        [new("87e7c501-9948-4b53-b1eb-a6cbe918feee")] = "pascal_case",
        [new("1ecc5eb6-b5fc-49a5-a9f1-a980f3e48c92")] = "i_prefix_pascal_case",
    };

    public static bool TryGetBuiltInId(Guid legacyId, out string? id)
        => s_builtinIds.TryGetValue(legacyId, out id);

    public static IEnumerable<string> GetBuiltInIds(string identityKind)
        => s_builtinIds.Values.Where(id => IsStyleId(id) == (identityKind == IdentityKinds.NamingStyle));

    public static Guid GetLegacyId(string id, string identityKind)
    {
        foreach (var pair in s_builtinIds)
        {
            if (pair.Value == id && IsStyleId(id) == (identityKind == IdentityKinds.NamingStyle))
                return pair.Key;
        }

        using var hash = SHA256.Create();
        var bytes = hash.ComputeHash(Encoding.UTF8.GetBytes("Roslyn.Naming." + identityKind + ":" + id));
        var guidBytes = new byte[16];
        Array.Copy(bytes, guidBytes, guidBytes.Length);
        return new Guid(guidBytes);
    }

    private static bool IsStyleId(string id) => id is "pascal_case" or "i_prefix_pascal_case";

    public static string CreateId(Guid legacyId, string name, ISet<string> usedIds)
    {
        var stem = TryGetBuiltInId(legacyId, out var builtInId) ? builtInId! : Sanitize(name);
        var id = stem;
        for (var suffix = 2; !usedIds.Add(id); suffix++)
            id = stem + "_" + suffix.ToString(System.Globalization.CultureInfo.InvariantCulture);

        return id;
    }

    public static bool IsValidId(string id)
    {
        if (id.Length == 0 || id[0] is < 'a' or > 'z')
            return false;

        foreach (var character in id)
        {
            if (character is not (>= 'a' and <= 'z') and not (>= '0' and <= '9') and not '_')
                return false;
        }

        return true;
    }

    private static string Sanitize(string name)
    {
        var builder = new StringBuilder();
        foreach (var character in name)
        {
            var lower = char.ToLowerInvariant(character);
            if (lower is (>= 'a' and <= 'z') or (>= '0' and <= '9'))
            {
                builder.Append(lower);
            }
            else if (builder.Length > 0 && builder[builder.Length - 1] != '_')
            {
                builder.Append('_');
            }
        }

        var id = builder.ToString().TrimEnd('_');
        return id.Length == 0 ? "item" : id[0] is >= 'a' and <= 'z' ? id : "item_" + id;
    }
}
