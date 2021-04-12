// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

using namespace System;

namespace CLibraryShim {

    public ref class RealConversions
    {
    public:
        static double atod(String^ s);
        static float atof(String^ s);
    };
}
