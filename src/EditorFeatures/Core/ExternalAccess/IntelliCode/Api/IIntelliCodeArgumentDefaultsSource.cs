// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.IntelliCode.Api;

/// <summary>
/// Provides a list of possible default arguments for method calls.
/// </summary>
/// <remarks>
/// This is a MEF component and should be exported with <see cref="ContentTypeAttribute"/> and <see cref="NameAttribute"/> attributes
/// and optional <see cref="OrderAttribute"/> and <see cref="TextViewRoleAttribute"/> attributes.
/// An instance of <see cref="IIntelliCodeArgumentDefaultsSource"/> is selected
/// first by matching ContentType with content type of the <see cref="ITextView.TextBuffer"/>, and then by order.
/// Only one <see cref="IIntelliCodeArgumentDefaultsSource"/> is used in a given view.
/// <para>
/// Only one <see cref="IIntelliCodeArgumentDefaultsSource"/> will used for any given <see cref="ITextView"/>. The sources are
/// ordered by the Order attribute. The first source (if any) that satisfies the ContentType and TextViewRoles
/// attributes will be the source used to provide defaults.
/// </para>
/// <example>
/// <code>
///     [Export(typeof(IIntelliCodeArgumentDefaultsSource))]
///     [Name(nameof(IntelliCodeArgumentDefaultsSource))]
///     [ContentType("text")]
///     [TextViewRoles(PredefinedTextViewRoles.Editable)]
///     [Order(Before = "OtherCompletionDefaultsSource")]
///     public class IntelliCodeArgumentDefaultsSource : IIntelliCodeArgumentDefaultsSource
/// </code>
/// </example>
/// </remarks>
internal interface IIntelliCodeArgumentDefaultsSource
{
    /// <summary>
    /// Gets a list of possible default arguments for a method signature.
    /// </summary>
    /// <param name="view">View for which the defaults are desired.</param>
    /// <returns>A list of possible default arguments for a method signature.</returns>
    /// <remarks>
    /// <para>The returned value will always be in the form of a "complete" set of arguments, including the leading and trailing parenthesis.</para>
    /// <para>For example:
    /// <code>
    /// ()
    /// (args[0])
    /// (args.Length)
    /// (value: args.Length)
    /// </code>
    /// </para>
    /// <para>Some of the proposals may be syntactically/semantically invalid (and can be ignored by the caller).</para>
    /// </remarks>
    Task<ImmutableArray<string>> GetArgumentDefaultsAsync(ITextView view);
}
