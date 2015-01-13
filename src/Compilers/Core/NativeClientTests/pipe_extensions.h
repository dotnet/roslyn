#include "native_client.h"

#pragma once

// Writes to a buffer in memory for testing
class WriteOnlyMemoryPipe : public IPipe
{
private:
    std::vector<BYTE> buffer;

public:
    virtual bool Write(LPCVOID data, unsigned toWrite)
    {
        auto byteData = static_cast<LPCBYTE>(data);
        buffer.insert(buffer.end(), byteData, byteData + toWrite);
        return true;
    }
    virtual bool Read(_Out_ LPVOID data, unsigned size)
    {
        return false;
    }
    std::vector<BYTE> Bytes()
    {
        return buffer;
    }
};
