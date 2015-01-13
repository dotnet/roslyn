#include "stdafx.h"
#include "satellite.h"

#define UILANGUAGE_REG_KEY "Software\\Microsoft\\VisualStudio\\9.0\\General"
#define UILANGUAGE_REG_VALUE "UILanguage"

// Define lengthof macro - length of an array.
#define lengthof(a) (sizeof(a) / sizeof((a)[0]))

// List of fallback langids we try (in order) if we can't find the messages DLL by normal means.
// These are just a bunch of common languages -- not necessarily all languages we localize to.
// This list should never be used in usual course of things -- it's just an "emergency" fallback.
const LANGID g_fallbackLangs[] =
{
    MAKELANGID(LANG_NEUTRAL, SUBLANG_NEUTRAL),
    MAKELANGID(LANG_ENGLISH, SUBLANG_ENGLISH_US),
    MAKELANGID(LANG_ENGLISH, SUBLANG_ENGLISH_UK),
    MAKELANGID(LANG_ENGLISH, SUBLANG_ENGLISH_AUS),
    MAKELANGID(LANG_ENGLISH, SUBLANG_ENGLISH_CAN),
    //    MAKELANGID(LANG_CHINESE, SUBLANG_DEFAULT), // Same as below
    MAKELANGID(LANG_CHINESE, SUBLANG_CHINESE_TRADITIONAL),
    MAKELANGID(LANG_CHINESE, SUBLANG_CHINESE_SIMPLIFIED),
    MAKELANGID(LANG_FRENCH, SUBLANG_DEFAULT),
    MAKELANGID(LANG_FRENCH, SUBLANG_FRENCH),
    MAKELANGID(LANG_FRENCH, SUBLANG_FRENCH_CANADIAN),
    //    MAKELANGID(LANG_GERMAN, SUBLANG_DEFAULT), // Same as below
    MAKELANGID(LANG_GERMAN, SUBLANG_GERMAN),
    MAKELANGID(LANG_GREEK, SUBLANG_DEFAULT),
    MAKELANGID(LANG_HEBREW, SUBLANG_DEFAULT),
    //    MAKELANGID(LANG_ITALIAN, SUBLANG_DEFAULT), // Same as below
    MAKELANGID(LANG_ITALIAN, SUBLANG_ITALIAN),
    MAKELANGID(LANG_JAPANESE, SUBLANG_DEFAULT),
    //    MAKELANGID(LANG_KOREAN, SUBLANG_DEFAULT), // Same as below
    MAKELANGID(LANG_KOREAN, SUBLANG_KOREAN),
    //    MAKELANGID(LANG_PORTUGUESE, SUBLANG_DEFAULT), // Same as below
    MAKELANGID(LANG_PORTUGUESE, SUBLANG_PORTUGUESE),
    MAKELANGID(LANG_PORTUGUESE, SUBLANG_PORTUGUESE_BRAZILIAN),
    MAKELANGID(LANG_RUSSIAN, SUBLANG_DEFAULT),
    //    MAKELANGID(LANG_SPANISH, SUBLANG_DEFAULT), // Same as below
    MAKELANGID(LANG_SPANISH, SUBLANG_SPANISH),
    MAKELANGID(LANG_SPANISH, SUBLANG_SPANISH_MEXICAN),
    MAKELANGID(LANG_SPANISH, SUBLANG_SPANISH_MODERN),
};


// Returns true iff the language specificed by langid is displayable in the current console code-page.
// If the user has their language settings set to something which is not displayable in the console, then
// we want to load resources for a different language.
bool static LanguageMatchesCP(LANGID langid)
{
    // Note:  This function is also implemented in alinklib.cpp for alink.

    // Eliminate bi-directional languages
    if (PRIMARYLANGID(langid) == LANG_ARABIC || PRIMARYLANGID(langid) == LANG_HEBREW)
        return false;

    // need to use ANSI version of GetLocaleInfo() for Win9x support.
    char localeInfo[MAX_PATH];
    int consoleCP = GetConsoleCP();
    if (consoleCP == 0)
        return true;       // If we fail to get the ConsoleCP, then we're in an IDE scenario, so assume the langid is valid.

    GetLocaleInfoA(MAKELCID(langid, SORT_DEFAULT), LOCALE_IDEFAULTCODEPAGE, localeInfo, MAX_PATH);
    int langOEMCodepage = atoi(localeInfo);
    GetLocaleInfoA(MAKELCID(langid, SORT_DEFAULT), LOCALE_IDEFAULTANSICODEPAGE, localeInfo, MAX_PATH);
    int langANSICodepage = atoi(localeInfo);

    // 65001 is the UTF-8 code-page, which is specifically set up to accept any unicode character.
    if (consoleCP != 65001 && consoleCP != langOEMCodepage && consoleCP != langANSICodepage)
        return false;

    return true;
}

// Try to load a message DLL from a location, in a subdirectory of mine given by the LANGID
// passed in (or the same directory if -1).
static HINSTANCE FindMessageDll(LANGID langid, bool fCheckLangID, LPCWSTR messageDllName)
{
    // The specified language ID is not valid for the current console code page,
    // so don't search for the resource dll
    if (langid != (LANGID)-1 && fCheckLangID && !LanguageMatchesCP(langid))
        return 0;

    WCHAR path[MAX_PATH];
    WCHAR *pEnd;

    if (!GetModuleFileName(GetModuleHandle(NULL), path, lengthof(path)))
        return 0;

    // Force null termination
    path[lengthof(path) - 1] = L'\0';

    pEnd = wcsrchr(path, L'\\');

    if (!pEnd)
        return 0;

    ++pEnd;
    *pEnd = L'\0';  // nul terminate and point to the nul

    // Append language ID
    if (langid != (LANGID)-1)
    {
        if (FAILED(StringCchPrintfW(pEnd, lengthof(path) - (pEnd - path), L"%d\\", langid)))
            return 0;
    }

    // Append message DLL name.
    if (FAILED(StringCchCatW(path, lengthof(path), messageDllName)))
        return 0;

    return LoadLibraryEx(path, NULL, LOAD_LIBRARY_AS_DATAFILE);
}

LANGID GetUsersPreferredUILanguage()
{
    // first try GetUserDefaultUILanguage if we're on WinXP
    LANGID langid = 0;

    HMODULE hKernel = GetModuleHandleA("kernel32.dll");
    if (hKernel)
    {
        FARPROC pfnGetUserDefaultUILanguage = GetProcAddress(hKernel, "GetUserDefaultUILanguage");
        if (pfnGetUserDefaultUILanguage != NULL)
        {
            langid = (LANGID)((pfnGetUserDefaultUILanguage)());
        }
    }
    if (langid)
        return langid;

    // Read UILanguage from registry.
    HKEY hkey;
    if (ERROR_SUCCESS == RegOpenKeyExA(HKEY_CURRENT_USER, UILANGUAGE_REG_KEY, 0, KEY_READ, &hkey))
    {
        DWORD data = 0;
        DWORD cbData = sizeof(DWORD);
        DWORD regType = 0;
        if (ERROR_SUCCESS == RegQueryValueExA(hkey, UILANGUAGE_REG_VALUE, 0, &regType, (LPBYTE)&data, &cbData) &&
            cbData == sizeof(DWORD) && regType == REG_DWORD)
        {
            langid = LANGIDFROMLCID(data);
        }
        RegCloseKey(hkey);

    }
    if (langid)
        return langid;

    // Next try user locale.
    langid = GetUserDefaultLangID();
    if (langid)
        return langid;

    // UNDONE: not sure if we can ever get past here ...

    // Next try current locale.
    langid = LANGIDFROMLCID(GetThreadLocale());
    if (langid)
        return langid;

    // and finally System
    langid = GetSystemDefaultLangID();
    return langid;
}

HINSTANCE FindMessageDllTryDefaultLang(LANGID *langid, bool fCheckLangID, LPCWSTR messageDllName)
{
    HINSTANCE hModuleMessagesLocal = FindMessageDll(*langid, fCheckLangID, messageDllName);
    if (!hModuleMessagesLocal)
    {
        *langid = MAKELANGID(PRIMARYLANGID(*langid), SUBLANG_DEFAULT);
        hModuleMessagesLocal = FindMessageDll(*langid, fCheckLangID, messageDllName);
    }

    return hModuleMessagesLocal;
}

typedef  BOOL(__stdcall * GET_PREFERRED_UI_LANGUAGES_PROTOTYPE) (DWORD, PULONG, PCWSTR, PULONG);

HINSTANCE FindMessageDllTryProcessPreferredUILangs(LANGID *langid, LPCWSTR messageDllName)
{
    HINSTANCE hModuleMessages = 0;

    HMODULE hKernel = GetModuleHandleA("kernel32.dll");
    if (hKernel)
    {
        ULONG size = 0;
        GET_PREFERRED_UI_LANGUAGES_PROTOTYPE pfnGetProcessPreferredUILanguages =
            (GET_PREFERRED_UI_LANGUAGES_PROTOTYPE)GetProcAddress(hKernel, "GetProcessPreferredUILanguages");
        if (pfnGetProcessPreferredUILanguages != NULL)
        {
            ULONG numLangs;
            // Call the method once to get the size of the buffer we'd need to allocate.
            pfnGetProcessPreferredUILanguages(MUI_LANGUAGE_ID, &numLangs, NULL, &size);
            WCHAR *langids = new WCHAR[size];

            // Get the langids for the process. If no langids are set the call will succeed but langids will 
            // be set to '\0\0'.
            BOOL success = pfnGetProcessPreferredUILanguages(MUI_LANGUAGE_ID, &numLangs, langids, &size);
            if (success && *langids != '\0')
            {
                // Go through all the languages and try to find a resource dll.
                WCHAR *curLangId = langids;
                for (ULONG i = 0; i < numLangs; i++)
                {
                    // The string returned will be in hex and each lang is delimited by a '\0'. 
                    // For en-us(1033) the string will be "0409\0\0".
                    WCHAR *nextLangId = NULL;
                    *langid = (LANGID)wcstol(curLangId, &nextLangId, 16);

                    if (*langid)
                    {
                        hModuleMessages = FindMessageDllTryDefaultLang(langid, false, messageDllName);

                        if (hModuleMessages)
                        {
                            break;
                        }
                    }
                    curLangId = nextLangId + 1;
                }
            }
            delete[] langids;
        }
    }
    return hModuleMessages;
}

HINSTANCE GetMessageDll(LPCWSTR uiDllname)
{
    LANGID langid;
    HINSTANCE hModuleMessagesLocal = 0;

    // Try the process preferred ui langs. csc.exe set this when /preferreduilang switch is passed in.
    hModuleMessagesLocal = FindMessageDllTryProcessPreferredUILangs(&langid, uiDllname);

    // Next try user's preferred language
    if (!hModuleMessagesLocal)
    {
        langid = GetUsersPreferredUILanguage();
        hModuleMessagesLocal = FindMessageDllTryDefaultLang(&langid, true, uiDllname);
    }

    // Try a fall-back list of locales.
    if (!hModuleMessagesLocal)
    {
        for (unsigned int i = 0; i < lengthof(g_fallbackLangs); ++i)
        {
            langid = g_fallbackLangs[i];
            hModuleMessagesLocal = FindMessageDll(langid, false, uiDllname);
            if (hModuleMessagesLocal)
                break;
        }
    }

    // Try current directory.
    if (!hModuleMessagesLocal)
    {
        langid = (LANGID)-1;
        hModuleMessagesLocal = FindMessageDll((LANGID)-1, false, uiDllname);
    }

    return hModuleMessagesLocal;
}
