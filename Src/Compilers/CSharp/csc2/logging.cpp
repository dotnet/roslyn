// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


#include "stdafx.h"
#include "logging.h"

// This file implements some logging function that make debugging a lot easier
// The format and environment variable is shared by the client and server pieces
// so a single log file is written to by both processes.

static FILE * logFile;

bool HaveLogFile()
{
	return logFile != NULL;
}

void InitializeLogging()
{
	char * loggingFileName = NULL;
	size_t loggingFileNameLength = 0;
	_dupenv_s(& loggingFileName, & loggingFileNameLength, LOGGING_ENV_VAR);
	if (loggingFileName != NULL) 
	{
		// If the environment variable contains the path of a currently existing directory,
		// then use a process-specific name for the log file and put it in that directory.
		// Otherwise, assume that the environment variable specifies the name of the log file.
		DWORD attributes = GetFileAttributesA(loggingFileName);
		if (attributes != INVALID_FILE_ATTRIBUTES && ((attributes & FILE_ATTRIBUTE_DIRECTORY) == FILE_ATTRIBUTE_DIRECTORY))
		{
			char* loggingDirName = loggingFileName;
			size_t bufferSize = loggingFileNameLength + 30; // Need space for PID and tick count
			loggingFileName = new char[bufferSize];
			_snprintf_s(loggingFileName, bufferSize, _TRUNCATE, "%s\\client.%d.%d.log", loggingDirName, GetCurrentProcessId(), GetTickCount());
			delete[] loggingDirName;
		}

		logFile = _fsopen(loggingFileName, "at", _SH_DENYNO);
		delete[] loggingFileName;
	}
}

static void LogPrefix()
{
	if (logFile != NULL) 
	{
		fprintf(logFile, "CLI PID=%d TID=%d Ticks=%d: ", GetCurrentProcessId(), GetCurrentThreadId(), GetTickCount());
	}
}

void Log(char * message)
{
	if (logFile != NULL) 
	{
		LogPrefix();
		fprintf(logFile, message);
		fprintf(logFile, "\n");
		fflush(logFile);
	}
}

void LogFormatted(char * message, ...)
{
	if (logFile != NULL) 
	{
		va_list varargs;
		va_start(varargs, message);

		LogPrefix();
		vfprintf(logFile, message, varargs);
		fprintf(logFile, "\n");
		fflush(logFile);
	}
}

void LogTime()
{
	if (logFile != NULL) 
	{
		SYSTEMTIME time;
		GetLocalTime(&time);
		LogFormatted("Local time = %02d:%02d:%02d.%03d", time.wHour, time.wMinute, time.wSecond, time.wMilliseconds);
	}
}

void LogWin32Error(char * message)
{
	LogFormatted("Win32 Error Code %X during %s", GetLastError(), message);
}

void Exit(int exitCode)
{
	LogTime();
	LogFormatted("Exiting with code %d", exitCode);
	exit(exitCode);
}