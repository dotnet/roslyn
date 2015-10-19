// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#include <stdlib.h>     /* strtod, strtof */

#include "CLibraryShim.h"

double CLibraryShim::RealConversions::atod(String^ s)
{
    int length = s->Length;
    wchar_t *chars = new wchar_t[length + 1];
    for (int i = 0; i < length; i++) chars[i] = (*s)[i];
    chars[length] = 0;
    wchar_t *end = 0;
    double d = wcstod(chars, &end);
    delete chars;
    return d;
}

float CLibraryShim::RealConversions::atof(String^ s)
{
    int length = s->Length;
    wchar_t *chars = new wchar_t[length + 1];
    for (int i = 0; i < length; i++) chars[i] = (*s)[i];
    chars[length] = 0;
    wchar_t *end = 0;
    float f = wcstof(chars, &end);
    delete chars;
    return f;
}