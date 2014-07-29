// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#pragma once

#include <Windows.h>
#include <exception>
#include <string>

const wchar_t * const LOGGING_ENV_VAR = L"RoslynCommandLineLogFile";

class FatalError : public std::exception
{
public:
	std::wstring message;

	FatalError(std::wstring&& message)
	: message(message) {}
};

bool HaveLogFile();
bool GetEnvVar(LPCWSTR name, std::wstring &value);
void InitializeLogging();
void Log(LPCWSTR message);
void LogFormatted(LPCWSTR message, ...);
void LogTime();
void LogWin32Error(LPCWSTR message);
void FailWithGetLastError(LPWSTR optionalPrefix = nullptr);
void FailFormatted(LPCWSTR message, ...);