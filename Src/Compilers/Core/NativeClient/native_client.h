#pragma once

#include <exception>
#include <list>
#include "logging.h"
#include "protocol.h"

int Run(RequestLanguage language);

void ParseAndValidateClientArguments(
    _Inout_ list<wstring>& arguments,
    _Out_ wstring& keepAliveValue);