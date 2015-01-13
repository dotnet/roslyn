// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#pragma once

#include <exception>
#include <list>
#include "logging.h"
#include "protocol.h"

int Run(RequestLanguage language);

bool ParseAndValidateClientArguments(
    _Inout_ list<wstring>& arguments,
    _Out_ wstring& keepAliveValue,
    _Out_ int& errorId);