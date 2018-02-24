# Jenkins in Feature Branches
This document describes the process for setting up CI on a feature branch of roslyn.

## Push the branch
The first step is to create the branch seeded with the initial change on roslyn. This branch should have the name `features/<feature name>`. For example: `features/mono` for working on mono work. 

Assuming the branch should start with the contents of `master` the branch can be created by doing the following:

Note: these steps assume the remote `origin` points to the official [roslyn repository](https://github.com/dotnet/roslyn).

``` cmd
> git fetch origin
> git checkout -B init origin/master
> git push origin init:features/mono
```

## Adding branch to Jenkins
Our Jenkins server manages branches on an opt-in bases. The set of branches that it monitors is kept in the [repolist.txt](https://github.com/dotnet/dotnet-ci/blob/master/data/repolist.txt) file in the [dotnet-ci](https://github.com/dotnet/dotnet-ci) repositiory. To add a branch do the following:

- Check out the repolist.txt file on your local machine
- Add a line for your branch: `dotnet/roslyn branch=features/mono server=dotnet-ci`
- Send a PR to update this file. CC @jaredpar, @mmitche, @jasonmalinowski and one of us will get it merged. 

Once that is merged Jenkins will schedule a task to add the new branches into the system. This can take up to 30 minutes to complete if left on it's own. Generally you want to force this to happen immediately by doing the following:

- Navgiate to https://ci.dot.net/job/dotnet_dotnet-ci_generator/
- Hit "Login" in the top right corner. This will use Oauth and GitHub to log you in. 
- Click the "Build with Parameters" link
- Click the "Build" button 

Once that job completes the branch folder will now be listed under the roslyn folder in Jenkins.

https://ci.dot.net/job/dotnet_roslyn/

## Changing the netci.groovy file
This step is necessary both at the point the branch is initially created and for any future changes to the netci.groovy file in our repo. 

Jenkins will monitor branches for changes and anytime it sees a change to netci.groovy it will schedule a change to re-generate all of the PR jobs. This scheduling can take a considerable amount of time to complete. Often you want to trigger the re-generation of jobs manually in order to make rapid progress here. 

To regenerate the jobs do the following:

- Navigate to the Roslyn folder in Jenkins: https://ci.dot.net/job/dotnet_roslyn/
- Click on the feature folder. Example https://ci.dot.net/job/dotnet_roslyn/job/features_mono
- Click on the generator job link 
- Click the "Build with Parameters" link (that link only appears if you are logged in)
- Click the "Build" button

Once that job completes new PRs into dotnet/roslyn will reflect the changes to the netci.groovy script.


