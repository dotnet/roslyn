// Groovy Script: http://www.groovy-lang.org/syntax.html
// Jenkins DSL: https://github.com/jenkinsci/job-dsl-plugin/wiki

import jobs.generation.*;

// The input project name (e.g. dotnet/corefx)
def projectName = GithubProject
// The input branch name (e.g. master)
def branchName = GithubBranchName
// Folder that the project jobs reside in (project/branch)
def projectFoldername = Utilities.getFolderName(projectName) + '/' + Utilities.getFolderName(branchName)

static void addRoslynJob(def myJob, String jobName, String branchName, Boolean isPr, String triggerPhraseExtra, Boolean triggerPhraseOnly = false) {
  def archiveSettings = new ArchivalSettings()
  archiveSettings.addFiles('Binaries/**/*.pdb')
  archiveSettings.addFiles('Binaries/**/*.xml')
  archiveSettings.addFiles('Binaries/**/*.log')
  archiveSettings.addFiles('Binaries/**/*.dmp')
  archiveSettings.addFiles('Binaries/**/*.zip')
  archiveSettings.addFiles('Binaries/**/*.png')
  archiveSettings.addFiles('Binaries/**/*.xml')
  archiveSettings.excludeFiles('Binaries/Obj/**')
  archiveSettings.excludeFiles('Binaries/Bootstrap/**')
  archiveSettings.excludeFiles('Binaries/**/nuget*.zip')
  // Only archive if failed/aborted
  archiveSettings.setArchiveOnFailure()
  archiveSettings.setFailIfNothingArchived()
  Utilities.addArchival(myJob, archiveSettings)

  // Create the standard job.  This will setup parameter, SCM, timeout, etc ...
  def projectName = 'dotnet/roslyn'
  def defaultBranch = "*/${branchName}"
  Utilities.standardJobSetup(myJob, projectName, isPr, defaultBranch)

  // Need to setup the triggers for the job
  if (isPr) {
    def triggerCore = "open|all|${jobName}"
    if (triggerPhraseExtra) {
      triggerCore = "${triggerCore}|${triggerPhraseExtra}"
    }
    def triggerPhrase = "(?i)^\\s*(@?dotnet-bot\\s+)?(re)?test\\s+(${triggerCore})(\\s+please)?\\s*\$";
    def contextName = jobName
    Utilities.addGithubPRTriggerForBranch(myJob, branchName, contextName, triggerPhrase, triggerPhraseOnly)
  } else {
    Utilities.addGithubPushTrigger(myJob)
    // TODO: Add once external email sending is available again
    // addEmailPublisher(myJob)
  }
}

// True when this is a PR job, false for commit.  On feature branches we do PR jobs only. 
def commitPullList = [false, true]
if (branchName.startsWith("features/")) {
  commitPullList = [true]
} 

// Windows     
commitPullList.each { isPr -> 
  ['debug', 'release'].each { configuration ->
        ['unit32', 'unit64'].each { buildTarget ->
      def jobName = Utilities.getFullJobName(projectName, "windows_${configuration}_${buildTarget}", isPr)
            def myJob = job(jobName) {
        description("Windows ${configuration} tests on ${buildTarget}")
                  steps {
                    batchFile("""set TEMP=%WORKSPACE%\\Binaries\\Temp
mkdir %TEMP%
set TMP=%TEMP%
.\\cibuild.cmd ${(configuration == 'debug') ? '/debug' : '/release'} ${(buildTarget == 'unit32') ? '/test32' : '/test64'}""")
                  }
                }

      def triggerPhraseOnly = false
      def triggerPhraseExtra = ""
      Utilities.setMachineAffinity(myJob, 'Windows_NT', 'latest-or-auto-dev15')
      Utilities.addXUnitDotNETResults(myJob, '**/xUnitResults/*.xml')
      addRoslynJob(myJob, jobName, branchName, isPr, triggerPhraseExtra, triggerPhraseOnly)
    }
  }
}

// Linux
commitPullList.each { isPr -> 
  def jobName = Utilities.getFullJobName(projectName, "linux_debug", isPr)
  def myJob = job(jobName) {
    description("Linux tests")
                  steps {
                    shell("./cibuild.sh --nocache --debug")
                  }
                }

  def triggerPhraseOnly = false
  def triggerPhraseExtra = "linux"
  Utilities.setMachineAffinity(myJob, 'Ubuntu14.04', 'latest-or-auto')
  Utilities.addXUnitDotNETResults(myJob, '**/xUnitResults/*.xml')
  addRoslynJob(myJob, jobName, branchName, isPr, triggerPhraseExtra, triggerPhraseOnly)
}

// Mac
commitPullList.each { isPr -> 
  def jobName = Utilities.getFullJobName(projectName, "mac_debug", isPr)
  def myJob = job(jobName) {
    description("Mac tests")
    label('mac-roslyn')
    steps {
      shell("./cibuild.sh --nocache --debug")
    }
  }

  def triggerPhraseOnly = true
  def triggerPhraseExtra = "mac"
  Utilities.addXUnitDotNETResults(myJob, '**/xUnitResults/*.xml')
  addRoslynJob(myJob, jobName, branchName, isPr, triggerPhraseExtra, triggerPhraseOnly)
  }

// Determinism
commitPullList.each { isPr -> 
  def jobName = Utilities.getFullJobName(projectName, "windows_determinism", isPr)
  def myJob = job(jobName) {
    description('Determinism tests')
    steps {
      batchFile("""set TEMP=%WORKSPACE%\\Binaries\\Temp
mkdir %TEMP%
set TMP=%TEMP%
.\\cibuild.cmd /testDeterminism""")
    }
  }

  def triggerPhraseOnly = false
  def triggerPhraseExtra = "determinism"
  Utilities.setMachineAffinity(myJob, 'Windows_NT', 'latest-or-auto-dev15')
  addRoslynJob(myJob, jobName, branchName, isPr, triggerPhraseExtra, triggerPhraseOnly)
}

// Build correctness tests
commitPullList.each { isPr -> 
  def jobName = Utilities.getFullJobName(projectName, "windows_build_correctness", isPr)
  def myJob = job(jobName) {
    description('Build correctness tests')
    steps {
      batchFile("""set TEMP=%WORKSPACE%\\Binaries\\Temp
mkdir %TEMP%
set TMP=%TEMP%
.\\cibuild.cmd /testBuildCorrectness""")
    }
  }

  def triggerPhraseOnly = false
  def triggerPhraseExtra = ""
  Utilities.setMachineAffinity(myJob, 'Windows_NT', 'latest-or-auto-dev15')
  addRoslynJob(myJob, jobName, branchName, isPr, triggerPhraseExtra, triggerPhraseOnly)
}

// Perf Correctness
commitPullList.each { isPr ->
  def jobName = Utilities.getFullJobName(projectName, "perf_correctness", isPr)
  def myJob = job(jobName) {
    description('perf test correctness')
    steps {
      batchFile(""".\\cibuild.cmd /testPerfCorrectness""")
    }
  }

  def triggerPhraseOnly = false
  def triggerPhraseExtra = "perf-correctness"
  Utilities.setMachineAffinity(myJob, 'Windows_NT', 'latest-or-auto-dev15')
  addRoslynJob(myJob, jobName, branchName, isPr, triggerPhraseExtra, triggerPhraseOnly)
}

// Microbuild
commitPullList.each { isPr ->
  def jobName = Utilities.getFullJobName(projectName, "microbuild", isPr)
  def myJob = job(jobName) {
    description('MicroBuild test')
    steps {
      batchFile(""".\\src\\Tools\\MicroBuild\\cibuild.cmd""")
    }
  }

  def triggerPhraseOnly = false
  def triggerPhraseExtra = "microbuild"
  Utilities.setMachineAffinity(myJob, 'Windows_NT', 'latest-or-auto-dev15')
  addRoslynJob(myJob, jobName, branchName, isPr, triggerPhraseExtra, triggerPhraseOnly)
}

// Open Integration Tests
commitPullList.each { isPr ->
  def jobName = Utilities.getFullJobName(projectName, "open-vsi", isPr)
  def myJob = job(jobName) {
    description('open integration tests')
    steps {
      batchFile("""set TEMP=%WORKSPACE%\\Binaries\\Temp
mkdir %TEMP%
set TMP=%TEMP%
.\\cibuild.cmd /debug /testVsi""")
    }
  }

  def triggerPhraseOnly = true
  def triggerPhraseExtra = "open-vsi"
  Utilities.setMachineAffinity(myJob, 'Windows_NT', 'latest-or-auto-dev15-rc')
  addRoslynJob(myJob, jobName, branchName, isPr, triggerPhraseExtra, triggerPhraseOnly)
}

JobReport.Report.generateJobReport(out)

// Make the call to generate the help job
Utilities.createHelperJob(this, projectName, branchName,
    "Welcome to the ${projectName} Repository",  // This is prepended to the help message
    "Have a nice day!")  // This is appended to the help message.  You might put known issues here.
