// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

// NOTE: This whole file should be removed once the experimental attribute is not needed
// (the source generator should be used to emit this code instead).

namespace Microsoft.CodeAnalysis.CSharp.Syntax;

[Experimental(RoslynExperiments.IgnoredDirectives, UrlFormat = RoslynExperiments.IgnoredDirectives_Url)]
partial class IgnoredDirectiveTriviaSyntax;
