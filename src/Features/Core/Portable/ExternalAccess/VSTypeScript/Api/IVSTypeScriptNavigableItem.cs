// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal interface IVSTypeScriptNavigableItem
    {
        Glyph Glyph { get; }

        /// <summary>
        /// The tagged parts to display for this item. If default, the line of text from <see cref="Document"/> is used.
        /// </summary>
        ImmutableArray<TaggedText> DisplayTaggedParts { get; }

        /// <summary>
        /// Return true to display the file path of <see cref="Document"/> and the span of <see cref="SourceSpan"/> when displaying this item.
        /// </summary>
        bool DisplayFileLocation { get; }

        /// <summary>
        /// his is intended for symbols that are ordinary symbols in the language sense, and may be
        /// used by code, but that are simply declared implicitly rather than with explicit language
        /// syntax.  For example, a default synthesized constructor in C# when the class contains no
        /// explicit constructors.
        /// </summary>
        bool IsImplicitlyDeclared { get; }

        Document Document { get; }
        TextSpan SourceSpan { get; }

        ImmutableArray<IVSTypeScriptNavigableItem> ChildItems { get; }
    }
}
