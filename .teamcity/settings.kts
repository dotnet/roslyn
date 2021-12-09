// This is the standard TeamCity script for all projects. Our objective is that this script should not contain
// per-repo customizations. All customizations should go to patches.

import jetbrains.buildServer.configs.kotlin.v2019_2.*
import jetbrains.buildServer.configs.kotlin.v2019_2.buildSteps.powerShell
import jetbrains.buildServer.configs.kotlin.v2019_2.triggers.vcs

version = "2019.2"

project {
    buildType(DebugBuild)
    buildType(ReleaseBuild)
    buildType(PublicBuild)
    buildType(Deploy)
}

// Debug build (a numbered build)
object DebugBuild : BuildType({
    name = "Build [Debug]"

    artifactRules = "+:artifacts/publish/**/*=>artifacts/publish"

    vcs {
        root(DslContext.settingsRoot)
    }

    steps {
        powerShell {
            scriptMode = file {
                path = "Build.ps1"
            }
            noProfile = false
            param("jetbrains_powershell_scriptArguments", "test --numbered %build.number%")
        }
    }

    triggers {
        vcs {
        }
    }

    requirements {
        equals("env.BuildAgentType", "caravela02")
    }
})

// Release build (with unsuffixed version number)
object ReleaseBuild : BuildType({
    name = "Build [Release]"

    artifactRules = "+:artifacts/publish/**/*=>artifacts/publish"

    vcs {
        root(DslContext.settingsRoot)
    }

    steps {
        powerShell {
            scriptMode = file {
                path = "Build.ps1"
            }
            noProfile = false
            param("jetbrains_powershell_scriptArguments", "test  --numbered %build.number% --configuration Release --sign")
        }
    }

    triggers {
        vcs {
        }
    }

    requirements {
        equals("env.BuildAgentType", "caravela02")
    }
})

// Public build (a release build with unsuffixed version number)
object PublicBuild : BuildType({
    name = "Build [Public]"

    artifactRules = "+:artifacts/publish/**/*=>artifacts/publish"

    vcs {
        root(DslContext.settingsRoot)
    }

    steps {
        powerShell {
            scriptMode = file {
                path = "Build.ps1"
            }
            noProfile = false
            param("jetbrains_powershell_scriptArguments", "test --public --configuration Release --sign")
        }
    }

    triggers {
        vcs {
        }
    }
})

// Publish the release build to public feeds
object Deploy : BuildType({
    name = "Deploy [Public]"
    type = BuildTypeSettings.Type.DEPLOYMENT

    vcs {
        root(DslContext.settingsRoot)
    }

    steps {
        powerShell {
            scriptMode = file {
                path = "Build.ps1"
            }
            noProfile = false
            param("jetbrains_powershell_scriptArguments", "publish --public")
        }
    }
    
  dependencies {
        dependency(PublicBuild) {
            snapshot {
            }

            artifacts {
                cleanDestination = true
                artifactRules = "+:artifacts/publish/**/*=>artifacts/publish"
            }
        }
    }
})
