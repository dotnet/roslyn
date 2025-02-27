# See comment in global.json for why the RC is still necessary
FROM mcr.microsoft.com/dotnet/sdk:9.0.100-rc.1 AS build

WORKDIR /src

COPY . /src/

RUN pwsh ./Build.ps1 build

FROM scratch

COPY --from=build /src/artifacts /artifacts