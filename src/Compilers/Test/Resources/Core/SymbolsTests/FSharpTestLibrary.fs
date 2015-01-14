// Copyright (c)  Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace FSharpTestLibrary

open System.Runtime.CompilerServices

[<Extension>]
module public OptionExtensions =

    [<Extension>]
    let GetQuestion this = if this = 42 then "What is the meaning of life, the Universe, and everything?" else "*shrug*"

