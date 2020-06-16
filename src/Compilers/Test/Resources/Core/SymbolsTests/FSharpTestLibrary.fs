// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace FSharpTestLibrary

open System.Runtime.CompilerServices

[<Extension>]
module public OptionExtensions =

    [<Extension>]
    let GetQuestion this = if this = 42 then "What is the meaning of life, the Universe, and everything?" else "*shrug*"

