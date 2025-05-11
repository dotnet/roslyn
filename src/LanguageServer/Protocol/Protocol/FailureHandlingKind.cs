// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Describes the client's failure handling strategy for workspace changes.
/// </summary>
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#failureHandlingKind">Language Server Protocol specification</see> for additional information.
/// </para>
/// <remarks>Since 3.13</remarks>
[JsonConverter(typeof(StringEnumConverter<FailureHandlingKind>))]
[TypeConverter(typeof(StringEnumConverter<FailureHandlingKind>.TypeConverter))]
internal readonly record struct FailureHandlingKind(string Value) : IStringEnum
{
    /// <summary>
    /// Applying the workspace change is simply aborted if one of the changes
    /// provided fails. All operations executed before the failing operation
    /// stay executed.
    /// </summary>
    public static readonly FailureHandlingKind Abort = new("abort");

    /// <summary>
    /// All operations are executed transactionally. That means they either all
    /// succeed or no changes at all are applied to the workspace.
    /// </summary>
    public static readonly FailureHandlingKind Transactional = new("transactional");

    /// <summary>
    /// If the workspace edit contains only textual file changes they are
    /// executed transactional. If resource changes (create, rename or delete
    /// file) are part of the change the failure handling strategy is <see cref="Abort"/>.
    /// </summary>
    public static readonly FailureHandlingKind TextOnlyTransactional = new("textOnlyTransactional");

    /// <summary>
    /// The client tries to undo the operations already executed, but there is no
    /// guarantee that will succeed.
    /// </summary>
    public static readonly FailureHandlingKind Undo = new("undo");
}
