// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    /// <summary>
    /// Defines the known values for <see cref="CSharpCodeStyleOptions.PreferBraces"/>.
    /// </summary>
    internal enum PreferBracesPreference
    {
        /// <summary>
        /// Braces are allowed, but not preferred.
        /// </summary>
        /// <remarks>
        /// <para>The value <c>0</c> is important for serialization compatibility in
        /// <see cref="CodeStyleOption{T}.FromXElement(XElement)"/>. Prior to the use of this enum, the serialized value
        /// was the <see cref="bool"/> value <see langword="false"/>.</para>
        /// </remarks>
        None = 0,

        /// <summary>
        /// <para>Braces are preferred where allowed except in the following limited situations:</para>
        ///
        /// <list type="bullet">
        /// <item><description>Braces are not required for the embedded statement of an <c>else</c> clause when the embedded statement is an <c>if</c> statement.</description></item>
        /// <item><description>In a sequence of consecutive <c>using</c> statements, only the last statement requires braces.</description></item>
        /// <item><description>In a sequence of consecutive <c>lock</c> statements, only the last statement requires braces.</description></item>
        /// <item><description>In a sequence of consecutive <c>fixed</c> statements, only the last statement requires braces.</description></item>
        /// </list>
        /// </summary>
        /// <remarks>
        /// <para>The value <c>1</c> is important for serialization compatibility in
        /// <see cref="CodeStyleOption{T}.FromXElement(XElement)"/>. Prior to the use of this enum, the serialized value
        /// was the <see cref="bool"/> value <see langword="true"/>.</para>
        /// </remarks>
        Always = 1,

        /// <summary>
        /// <para>Braces are always allowed, and generally preferred except in limited situations involving single-line
        /// statements and expressions:</para>
        ///
        /// <list type="bullet">
        /// <item><description>Braces may be omitted in the cases described for <see cref="Always"/>.</description></item>
        /// <item><description>Braces may be omitted when the entire statement is placed on one line.</description></item>
        /// <item><description>For a statement that contains one or more embedded statements, braces may be omitted when
        /// every embedded statement fits on one line, and the part preceding the embedded statement is placed on one
        /// line. If any embedded statement uses braces, braces are preferred for all embedded statements of the same
        /// parent statement. For the purposes of evaluating this rule, if the embedded statement following an
        /// <c>else</c> keyword is an if statement, the embedded statements of the nested if statement are treated as
        /// children of the parent statement of the <c>else</c> keyword.</description></item>
        /// </list>
        /// </summary>
        WhenMultiline,
    }
}
