# See here for image contents: https://github.com/microsoft/vscode-dev-containers/tree/v0.192.0/containers/dotnet/.devcontainer/base.Dockerfile

FROM mcr.microsoft.com/dotnet/sdk:8.0

ARG DOTNET_SDK_INSTALL_URL=https://dot.net/v1/dotnet-install.sh
# Install the correct Roslyn SDK into the same directory that the base image installs the SDK in.
ENV DOTNET_INSTALL_DIR=/usr/share/dotnet

# Copy the global.json file so its available in the image before the repo is cloned
COPY global.json /tmp/

RUN cd /tmp \
  && curl --location --output dotnet-install.sh "${DOTNET_SDK_INSTALL_URL}" \
  && chmod +x dotnet-install.sh \
  && mkdir -p "${DOTNET_INSTALL_DIR}" \
  && ./dotnet-install.sh --jsonfile "./global.json" --install-dir "${DOTNET_INSTALL_DIR}" \
  && rm dotnet-install.sh

# Set up machine requirements to build the repo
RUN apt-get update && export DEBIAN_FRONTEND=noninteractive \
    && apt-get -y install --no-install-recommends curl git
