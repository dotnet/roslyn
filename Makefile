SHELL = /usr/bin/env bash
OS_NAME = $(shell uname -s)
BUILD_CONFIGURATION = Debug
BINARIES_PATH = $(shell pwd)/Binaries
SRC_PATH = $(shell pwd)/src
BOOTSTRAP_PATH = $(BINARIES_PATH)/Bootstrap
BUILD_LOG_PATH =
HOME_DIR = $(shell cd ~ && pwd)
DOTNET_VERSION = 1.0.1
DOTNET = $(BINARIES_PATH)/dotnet-cli/dotnet
TARGET_FX = netcoreapp1.1

MSBUILD_ADDITIONALARGS := /v:m /fl /fileloggerparameters:Verbosity=normal /p:Configuration=$(BUILD_CONFIGURATION)

ifeq ($(OS_NAME),Linux)
	RUNTIME_ID := $(shell . /etc/os-release && echo $$ID.$$VERSION_ID)-x64
else ifeq ($(OS_NAME),Darwin)
	RUNTIME_ID := osx.10.12-x64
endif

ifneq ($(BUILD_LOG_PATH),)
	MSBUILD_ADDITIONALARGS := $(MSBUILD_ADDITIONALARGS) /fileloggerparameters:LogFile=$(BUILD_LOG_PATH)
endif

ifeq ($(BOOTSTRAP),true)
	MSBUILD_ARGS = $(MSBUILD_ADDITIONALARGS) /p:CscToolPath=$(BOOTSTRAP_PATH)/csc /p:CscToolExe=csc /p:VbcToolPath=$(BOOTSTRAP_PATH)/vbc /p:VbcToolExe=vbc
else
	MSBUILD_ARGS = $(MSBUILD_ADDITIONALARGS)
endif

BUILD_CMD = dotnet build $(MSBUILD_ARGS)

.PHONY: all bootstrap test toolset

all: restore
	@export PATH="$(BINARIES_PATH)/dotnet-cli:$(PATH)" ; \
	export HOME="$(HOME_DIR)" ; \
	$(BUILD_CMD) CrossPlatform.sln

bootstrap: restore
	export HOME="$(HOME_DIR)" ; \
	export PATH="$(BINARIES_PATH)/dotnet-cli:$(PATH)" ; \
	$(BUILD_CMD) src/Compilers/CSharp/CscCore && \
	$(BUILD_CMD) src/Compilers/VisualBasic/VbcCore && \
	mkdir -p $(BOOTSTRAP_PATH)/csc && mkdir -p $(BOOTSTRAP_PATH)/vbc && \
	dotnet publish -c $(BUILD_CONFIGURATION) -r $(RUNTIME_ID) src/Compilers/CSharp/CscCore -o $(BOOTSTRAP_PATH)/csc && \
	dotnet publish -c $(BUILD_CONFIGURATION) -r $(RUNTIME_ID) src/Compilers/VisualBasic/VbcCore -o $(BOOTSTRAP_PATH)/vbc
	rm -rf Binaries/$(BUILD_CONFIGURATION)

test:
	@export PATH="$(BINARIES_PATH)/dotnet-cli:$(PATH)" ; \
	export HOME="$(HOME_DIR)" ; \
	dotnet publish -r $(RUNTIME_ID) src/Test/DeployCoreClrTestRuntime -o $(BINARIES_PATH)/$(BUILD_CONFIGURATION)/CoreClrTest -p:RoslynRuntimeIdentifier=$(RUNTIME_ID) && \
	build/scripts/tests.sh $(BUILD_CONFIGURATION)

restore: $(DOTNET)
	export PATH="$(BINARIES_PATH)/dotnet-cli:$(PATH)" ; \
	./build/scripts/restore.sh

$(DOTNET):
	mkdir -p $(BINARIES_PATH) ; \
	pushd $(BINARIES_PATH) ; \
	curl -O https://raw.githubusercontent.com/dotnet/cli/rel/1.0.0/scripts/obtain/dotnet-install.sh && \
	chmod +x dotnet-install.sh && \
	./dotnet-install.sh --version "$(DOTNET_VERSION)" --install-dir "$(BINARIES_PATH)/dotnet-cli"


clean:
	@rm -rf Binaries
