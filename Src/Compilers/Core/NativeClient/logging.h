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
std::wstring GetResourceString(UINT);
bool GetEnvVar(LPCWSTR name, std::wstring &value);
void InitializeLogging();
void Log(UINT loadResource);
void Log(LPCWSTR message);
void LogFormatted(UINT loadResource, ...);
void LogFormatted(LPCWSTR message, ...);
void LogTime();
void LogWin32Error(UINT loadResource);
void LogWin32Error(LPCWSTR message);
void FailWithGetLastError(UINT loadResource);
void FailWithGetLastError(LPCWSTR optionalPrefix = nullptr);
void FailFormatted(UINT loadResource, ...);
void FailFormatted(LPCWSTR message, ...);