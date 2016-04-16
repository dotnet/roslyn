// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
