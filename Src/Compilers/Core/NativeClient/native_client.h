#pragma once

#include <exception>
#include <list>
#include "logging.h"
#include "protocol.h"

CompletedResponse Run(
	RequestLanguage language,
	LPCWSTR currentDirectory,
	LPCWSTR commandLineArgs[],
	int argsCount,
	LPCWSTR libEnvVar);

int Run(RequestLanguage language, LPCWSTR uiDllname);

void ParseAndValidateClientArguments(
	list<wstring>& arguments,
	wstring& keepAliveValue);