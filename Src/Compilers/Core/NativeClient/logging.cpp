// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#include "stdafx.h"
#include "logging.h"
#include <memory>
#include <string>

using namespace std;

// This file implements some logging function that make debugging a lot easier
// The format and environment variable is shared by the client and server pieces
// so a single log file is written to by both processes.

static FILE * logFile = nullptr;

bool HaveLogFile()
{
	return logFile != nullptr;
}

bool GetEnvVar(LPCWSTR name, wstring &value)
{
	auto sizeNeeded = GetEnvironmentVariableW(name, nullptr, 0);
	if (sizeNeeded != 0)
	{
		value.resize(sizeNeeded);

		auto written = GetEnvironmentVariableW(name, &value[0], sizeNeeded);
		if (written != 0 && written <= sizeNeeded)
		{
			value.resize(written);
			return true;
		}
	}

	return false;
}

void InitializeLogging()
{
	wstring loggingFileName;
	if (GetEnvVar(LOGGING_ENV_VAR, loggingFileName))
	{
		// If the environment variable contains the path of a currently existing directory,
		// then use a process-specific name for the log file and put it in that directory.
		// Otherwise, assume that the environment variable specifies the name of the log file.
		DWORD attributes = GetFileAttributesW(loggingFileName.c_str());
		if (attributes != INVALID_FILE_ATTRIBUTES &&
			((attributes & FILE_ATTRIBUTE_DIRECTORY) == FILE_ATTRIBUTE_DIRECTORY))
		{
			loggingFileName += L"\\client.";
			loggingFileName += to_wstring((int)GetCurrentProcessId());
			loggingFileName += L".";
#pragma warning(suppress: 28159)
			loggingFileName += to_wstring(GetTickCount());
			loggingFileName += L".log";
		}
		logFile = _wfsopen(loggingFileName.c_str(), L"at", _SH_DENYNO);
	}
}

static void LogPrefix()
{
#pragma warning(suppress: 28159)
	fprintf(logFile, "CLI PID=%u TID=%u Ticks=%u: ", GetCurrentProcessId(), GetCurrentThreadId(), GetTickCount());
}

void Log(LPCWSTR message)
{
	if (logFile != nullptr) 
	{
		LogPrefix();
		fwprintf(logFile, message);
		fwprintf(logFile, L"\r\n");
		fflush(logFile);
	}
}
 
static void vLogFormatted(LPCWSTR message, va_list varargs)
{
	if (logFile != nullptr)
	{
		LogPrefix();
		vfwprintf(logFile, message, varargs);
		fprintf(logFile, "\r\n");
		fflush(logFile);
	}
}

void LogFormatted(LPCWSTR message, ...)
{
	if (logFile != nullptr) 
	{
		va_list varargs;
		va_start(varargs, message);

		vLogFormatted(message, varargs);
		va_end(varargs);
	}
}

void LogTime()
{
	if (logFile != nullptr) 
	{
		SYSTEMTIME time;
		GetLocalTime(&time);
		LogFormatted(L"Local time = %02d:%02d:%02d.%03d", time.wHour, time.wMinute, time.wSecond, time.wMilliseconds);
	}
}

void LogWin32Error(LPCWSTR message)
{
	LogFormatted(L"Win32 Error Code %X during %ws", GetLastError(), message);
}

void Exit(int exitCode)
{
	LogTime();
	LogFormatted(L"Exiting with code %d", exitCode);
	exit(exitCode);
}

void FailWithGetLastError(LPWSTR optionalPrefix)
{
	LPWSTR errorMsg;
	if (!FormatMessage(FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_ALLOCATE_BUFFER,
		               nullptr,
					   GetLastError(),
					   0,
					   (LPWSTR) &errorMsg,
					   0,
					   nullptr))
	{
		errorMsg = L"";
	}

	// TODO: How should this error be localized? Is there more information we could output for debugging purposes?
	if (optionalPrefix == nullptr)
		optionalPrefix = L"";

	wstring buffer(L"Internal Compiler Client Error: ");
	buffer += optionalPrefix;
	buffer += L" ";
	buffer += errorMsg;

	Log(buffer.c_str());
	LocalFree(errorMsg);
	throw FatalError(move(buffer));
}

void FailFormatted(LPCWSTR message, ...)
{
	va_list varargs;
	va_start(varargs, message);

	wstring fullMessage(L"Internal Compiler Client Error: ");

	int needed = _vscwprintf(message, varargs);
	auto buffer = std::make_unique <WCHAR []>(needed + 1);
	_vsnwprintf_s(buffer.get(), needed + 1, _TRUNCATE, message, varargs);
	va_end(varargs);

	fullMessage += buffer.release();
	fullMessage += L"\r\n";

	Log(fullMessage.c_str());
	throw FatalError(move(fullMessage));
}