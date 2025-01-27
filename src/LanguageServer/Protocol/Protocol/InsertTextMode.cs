// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    /// <summary>
    /// How whitespace and indentation is handled during completion item insertion.
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#insertTextMode">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    internal enum InsertTextMode
    {
        /// <summary>
        /// The insertion or replace string is taken as-is.
        /// <para>
        /// If the value is multi-line the lines below the cursor will be
        /// inserted using the indentation defined in the string value.
        /// The client will not apply any kind of adjustments to the string.
        /// </para>
        /// </summary>
        AsIs = 1,

        /// <summary>
        /// The editor adjusts leading whitespace of new lines so that
        /// they match the indentation up to the cursor of the line for
        /// which the item is accepted.
        /// <para>
        /// Consider a line like this: &lt;2tabs>&lt;cursor>&lt;3tabs>foo. Accepting a
        /// multi line completion item is indented using 2 tabs and all
        /// following lines inserted will be indented using 2 tabs as well.
        /// </para>
        /// </summary>
        AdjustIndentation = 2,
    }
}
