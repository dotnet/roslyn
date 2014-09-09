#pragma once

#include <exception>
#include "logging.h"
#include "protocol.h"
#include "pipe_utils.h"

CompletedResponse Run(
	RequestLanguage language,
	LPCWSTR currentDirectory,
	LPCWSTR commandLineArgs [],
	int argsCount,
	LPCWSTR libEnvVar,
	bool &utf8Output);

int Run(RequestLanguage);