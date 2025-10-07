// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp;

/// <param name="Method">
/// The actual collection builder method to eventually call.  If the method is generic this is the constructed form
/// with the appropriate type arguments filled in.</param>
/// <param name="ProjectionCall">
/// Call to the projected builder method with all arguments it requires.  This will be where the with(arguments)
/// are placed.  The lowering phase will combine the arguments here with the elements to create the final set
/// of arguments to actually invoke <see cref="Method"/> with.</param>
/// <param name="CallPlaceHolder">
/// Because we will be generating the final call on demand, and because we potentially need a conversion to convert
/// the return type to the target type, we may represent this construct in the bound tree as a conversion wrapping a
/// placeholder.  This is that placeholder.  The placeholder will be replaced in the lowering phase with the actual
/// final call that is generated to the real (non-projection) builder method.</param>
/// <param name="Conversion">
/// Possible conversion from the return type of the builder method to the target type.  This will be be the <see
/// cref="CallPlaceHolder"/> itself if the binder decided it didn't need a conversion (for example, a pointless
/// identity conversion).  Otherwise it will be an actual conversion node.</param>
internal readonly record struct CollectionBuilderInfo(
    MethodSymbol Method,
    BoundCall ProjectionCall,
    BoundValuePlaceholder CallPlaceHolder,
    BoundExpression Conversion);
