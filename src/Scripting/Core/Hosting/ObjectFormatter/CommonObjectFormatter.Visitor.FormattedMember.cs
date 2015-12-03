// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    public abstract partial class CommonObjectFormatter
    {
        private sealed partial class Visitor
        {
            private struct FormattedMember
            {
                // Non-negative if the member is an inlined element of an array (DebuggerBrowsableState.RootHidden applied on a member of array type).
                public readonly int Index;

                // Formatted name of the member or null if it doesn't have a name (Index is >=0 then).
                public readonly string Name;

                // Formatted value of the member.
                public readonly string Value;

                public FormattedMember(int index, string name, string value)
                {
                    Debug.Assert((name != null) || (index >= 0));
                    Name = name;
                    Index = index;
                    Value = value;
                }

                /// <remarks>
                /// Doesn't (and doesn't need to) reflect the number of digits in <see cref="Index"/> since
                /// it's only used for a conservative approximation (shorter is more conservative when trying
                /// to determine the minimum number of members that will fill the output).
                /// </remarks>
                public int MinimalLength
                {
                    get { return (Name != null ? Name.Length : "[0]".Length) + Value.Length; }
                }

                public string GetDisplayName()
                {
                    return Name ?? "[" + Index.ToString() + "]";
                }

                public bool HasKeyName()
                {
                    return Index >= 0 && Name != null && Name.Length >= 2 && Name[0] == '[' && Name[Name.Length - 1] == ']';
                }

                public bool AppendAsCollectionEntry(Builder result)
                {
                    // Some BCL collections use [{key.ToString()}]: {value.ToString()} pattern to display collection entries.
                    // We want them to be printed initializer-style, i.e. { <key>, <value> } 
                    if (HasKeyName())
                    {
                        result.AppendGroupOpening();
                        result.AppendCollectionItemSeparator(isFirst: true, inline: true);
                        result.Append(Name, 1, Name.Length - 2);
                        result.AppendCollectionItemSeparator(isFirst: false, inline: true);
                        result.Append(Value);
                        result.AppendGroupClosing(inline: true);
                    }
                    else
                    {
                        result.Append(Value);
                    }

                    return true;
                }

                public bool Append(Builder result, string separator)
                {
                    result.Append(GetDisplayName());
                    result.Append(separator);
                    result.Append(Value);
                    return true;
                }
            }
        }
    }
}