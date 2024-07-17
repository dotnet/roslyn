// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditorConfig.Parsing;

/// <summary>
/// Base option that all editorconfig option inherit from.
/// </summary>
internal record class EditorConfigOption(Section Section, TextSpan? Span);
