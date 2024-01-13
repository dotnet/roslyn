// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor
{
    internal interface IFSharpEditorFormattingServiceWithOptions : IFSharpEditorFormattingService
    {
        /// <summary>
        /// True if this service would like to format the document based on the user typing the
        /// provided character.
        /// </summary>
        bool SupportsFormattingOnTypedCharacter(Document document, AutoFormattingOptionsWrapper options, char ch);
    }
}
