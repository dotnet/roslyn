// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// An <c>IProgress&lt;WorkDoneProgress></c> is used to report progress to the server via the <c>$/progress</c> notification.
/// <para>
/// The derived classes <see cref="WorkDoneProgressBegin"/>, <see cref="WorkDoneProgressReport"/> and <see cref="WorkDoneProgressEnd"/>
/// are used to report the beginning, progression, and end of the operation.
/// </para>
/// </summary>
[JsonDerivedType(typeof(WorkDoneProgressBegin), "begin")]
[JsonDerivedType(typeof(WorkDoneProgressReport), "report")]
[JsonDerivedType(typeof(WorkDoneProgressEnd), "end")]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
internal abstract class WorkDoneProgress
{
}
