#pragma once

#include <exception>
#include <list>
#include "logging.h"
#include "protocol.h"

CompletedResponse Run(
	RequestLanguage language,
	_In_z_ LPCWSTR currentDirectory,
	_In_reads_(argsCount) LPCWSTR commandLineArgs[],
	int argsCount,
	_In_opt_z_ LPCWSTR libEnvVar);

int Run(RequestLanguage language, _In_z_ LPCWSTR uiDllname);

void ParseAndValidateClientArguments(
	_Inout_ list<wstring>& arguments,
	_Out_ wstring& keepAliveValue);