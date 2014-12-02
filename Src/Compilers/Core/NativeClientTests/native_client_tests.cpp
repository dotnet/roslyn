#include "CppUnitTest.h"
#include "pipe_extensions.h"
#include <memory>
#include <sstream>

namespace Microsoft 
{
    namespace VisualStudio 
    {
        namespace CppUnitTestFramework 
        {
            template<>
            static wstring ToString<RequestLanguage>(const RequestLanguage& lang)
            {
                return lang == RequestLanguage::CSHARPCOMPILE
                    ? L"CSHARPCOMPILE" : L"VBCOMPILE";
            }

            template<>
            static wstring ToString <vector<Request::Argument>>(const vector<Request::Argument>& vec)
            {
                return L"";
            }

            template<>
            static wstring ToString <vector<BYTE>>(const vector<BYTE>& vec)
            {
                return L"";
            }
        }
    }
}

using namespace Microsoft::VisualStudio::CppUnitTestFramework;

namespace NativeClientTests
{       
    TEST_CLASS(MessageTests)
    {
    public:
        TEST_METHOD(SimpleRequestWithoutUtf8)
        {
            auto language = RequestLanguage::CSHARPCOMPILE;
            list<wstring> args = {
                L"test.cs"
            };
            wstring keepAlive;
            ParseAndValidateClientArguments(args, keepAlive);

            Assert::IsTrue(keepAlive.empty());

            auto request = Request(language, L"");
            request.AddCommandLineArguments(args);

            Assert::AreEqual(PROTOCOL_VERSION, request.ProtocolVersion);
            Assert::AreEqual(language, request.Language);

            vector<Request::Argument> expectedArgs = {
                Request::Argument(ArgumentId::CURRENTDIRECTORY, 0, L""),
                Request::Argument(ArgumentId::COMMANDLINEARGUMENT, 0, L"test.cs"),
            };

            Assert::AreEqual(expectedArgs, request.Arguments());

            vector<byte> expectedBytes = {
                0x32, 0x0, 0x0, 0x0, // Size of request
                0x1, 0x0, 0x0, 0x0,  // Protocol version
                0x21, 0x25, 0x53, 0x44, // C# compile token
                0x2, 0x0, 0x0, 0x0, // Number of arguments
                0x21, 0x72, 0x14, 0x51, // Current directory token
                0x0, 0x0, 0x0, 0x0, // Index
                0x0, 0x0, 0x0, 0x0, // Length of value string
                0x22, 0x72, 0x14, 0x51, // Command line arg token
                0x0, 0x0, 0x0, 0x0, // Index
                0x7, 0x0, 0x0, 0x0, // Length of value string in characters
                0x74, 0x0, 0x65, 0x0, 0x73, // 't', 'e', 's'
                0x0, 0x74, 0x0, 0x2e, 0x0, // 't', '.'
                0x63, 0x0, 0x73, 0x0 // 'c', 's'
            };


            WriteOnlyMemoryPipe pipe;
            Assert::IsTrue(request.WriteToPipe(pipe));

            Assert::AreEqual(expectedBytes, pipe.Bytes());
        }

        TEST_METHOD(SimpleRequestWithUtf8)
        {
            auto language = RequestLanguage::CSHARPCOMPILE;
            list<wstring> args = {
                L"/utf8output",
                L"test.cs"
            };
            wstring keepAlive;
            ParseAndValidateClientArguments(args, keepAlive);

            Assert::IsTrue(keepAlive.empty());

            auto request = Request(language, L"");
            request.AddCommandLineArguments(args);

            Assert::AreEqual(PROTOCOL_VERSION, request.ProtocolVersion);
            Assert::AreEqual(language, request.Language);

            vector<Request::Argument> expectedArgs = {
                Request::Argument(ArgumentId::CURRENTDIRECTORY, 0, L""),
                Request::Argument(ArgumentId::COMMANDLINEARGUMENT, 0, L"/utf8output"),
                Request::Argument(ArgumentId::COMMANDLINEARGUMENT, 1, L"test.cs"),
            };

            Assert::AreEqual(expectedArgs, request.Arguments());

            vector<byte> expectedBytes = {
                0x54, 0x0, 0x0, 0x0, // Size of request
                0x1, 0x0, 0x0, 0x0,  // Protocol version
                0x21, 0x25, 0x53, 0x44, // C# compile token
                0x3, 0x0, 0x0, 0x0, // Number of arguments
                0x21, 0x72, 0x14, 0x51, // Current directory token
                0x0, 0x0, 0x0, 0x0, // Index
                0x0, 0x0, 0x0, 0x0, // Length of value string
                0x22, 0x72, 0x14, 0x51, // Command line arg token
                0x0, 0x0, 0x0, 0x0, // Index
                0xb, 0x0, 0x0, 0x0, // Length of value string in characters
                0x2f, 0x0, 0x75, 0x0, // '/', 'u'
                0x74, 0x0, 0x66, 0x0, // 't', 'f'
                0x38, 0x0, 0x6f, 0x0, // '8, 'o'
                0x75, 0x0, 0x74, 0x0, // 'u', 't'
                0x70, 0x0, 0x75, 0x0, // 'p', 'u'
                0x74, 0x0, // 't'
                0x22, 0x72, 0x14, 0x51, // Command line arg token
                0x1, 0x0, 0x0, 0x0, // Index
                0x7, 0x0, 0x0, 0x0, // Length of value string in characters
                0x74, 0x0, 0x65, 0x0, 0x73, // 't', 'e', 's'
                0x0, 0x74, 0x0, 0x2e, 0x0, // 't', '.'
                0x63, 0x0, 0x73, 0x0 // 'c', 's'
            };

            WriteOnlyMemoryPipe pipe;
            request.WriteToPipe(pipe);

            Assert::AreEqual(expectedBytes, pipe.Bytes());
        }

        TEST_METHOD(RequestsWithKeepAlive)
        {
            list<wstring> args = { L"/keepalive:10" };
            wstring keepAlive;
            ParseAndValidateClientArguments(args, keepAlive);

            Assert::IsTrue(args.empty());
            Assert::AreEqual(L"10", keepAlive.c_str());

            auto language = RequestLanguage::CSHARPCOMPILE;
            auto request = Request(language, L"");
            request.AddKeepAlive(wstring(keepAlive));

            vector<Request::Argument> expected = {
                Request::Argument(ArgumentId::CURRENTDIRECTORY, 0, L""),
                Request::Argument(ArgumentId::KEEPALIVE, 0, L"10"),
            };

            Assert::AreEqual(expected, request.Arguments());

            args = { L"/keepalive=10" };
            ParseAndValidateClientArguments(args, keepAlive);

            Assert::IsTrue(args.empty());
            Assert::AreEqual(L"10", keepAlive.c_str());

            request = Request(language, L"");
            request.AddKeepAlive(wstring(keepAlive));

            Assert::AreEqual(expected, request.Arguments());
        }

        TEST_METHOD(NegativeValidKeepAlive)
        {
            list<wstring> args = { L"/keepalive:-1" };
            wstring keepAlive;
            ParseAndValidateClientArguments(args, keepAlive);

            Assert::IsTrue(args.empty());
            Assert::AreEqual(L"-1", keepAlive.c_str());
        }

        TEST_METHOD(ParseKeepAliveNoValue)
        {
            list<wstring> args = {
                L"/keepalive",
            };
            wstring keepAlive;

            try
            {
                ParseAndValidateClientArguments(args, keepAlive);
                Assert::Fail(L"Expected exception");
            }
            catch (FatalError& e)
            {
                Assert::AreEqual(
                    L"Missing argument for '/keepalive' option",
                    e.message.c_str());
            }
        }

        TEST_METHOD(ParseKeepAliveNoValue2)
        {
            list<wstring> args = {
                L"/keepalive:",
            };
            wstring keepAlive;

            try
            {
                ParseAndValidateClientArguments(args, keepAlive);
                Assert::Fail(L"Expected exception");
            }
            catch (FatalError& e)
            {
                Assert::AreEqual(
                    L"Missing argument for '/keepalive' option",
                    e.message.c_str());
            }
        }
        
        TEST_METHOD(ParseKeepAliveBadInteger)
        {
            list<wstring> args = {
                L"/keepalive",
            };
            wstring keepAlive;

            try
            {
                ParseAndValidateClientArguments(args, keepAlive);
                Assert::Fail(L"Expected exception");
            }
            catch (FatalError& e)
            {
                Assert::AreEqual(
                    L"Missing argument for '/keepalive' option",
                    e.message.c_str());
            }
        }

        TEST_METHOD(ParseKeepAliveIntegerOutOfRange)
        {
            list<wstring> args = {
                L"/keepalive:-2",
            };
            wstring keepAlive;

            try
            {
                ParseAndValidateClientArguments(args, keepAlive);
                Assert::Fail(L"Expected exception");
            }
            catch (FatalError& e)
            {
                Assert::AreEqual(
                    L"Arguments to '/keepalive' option below -1 are invalid",
                    e.message.c_str());
            }
        }
    };
}
