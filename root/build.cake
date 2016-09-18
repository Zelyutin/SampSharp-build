#load "./cake/SaBuild.cake"

//////////////////////////////////////////////////////////////////////
// SABUILD
//////////////////////////////////////////////////////////////////////

var build = new SaBuild(
    Context,                                // cake context
    Argument("configuration", "Release"),   // configuration
    "./<DEBUG_BUILD_PATH_HERE>",            // debug build directory
    "./bin",                                // release build directory
    "./<PROJECT_SLN_HERE>.sln",             // solution
    new[] {                                 // assembly infos
        "./src/<PROJECT_HERE>/Properties/AssemblyInfo.cs",
    },
    new[] {                                 // nuget packages
        "<PROJECT_HERE>",
    },
    "<PROJECT_GH_USER_HERE>",               // github username
    "<PROJECT_GH_REPO_HERE>",               // github repository
    EnvironmentVariable("LAGET_KEY"),       // nuget key
    "http://timpotze.nl/upload",            // nuget source
    EnvironmentVariable("GITHUB_USERNAME"), // github release username
    EnvironmentVariable("GITHUB_PASSWORD"), // github release password
    new[] {                                 // nuget sources
        "http://nuget.timpotze.nl/api/v2/"
    }
);
   
//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("__Clean")
    .Does(() => build.Clean());

Task("__RestoreNuGetPackages")
    .Does(() => build.RestoreNuGetPackages());

Task("__BuildSolution")
    .IsDependentOn("__RestoreNuGetPackages")
    .Does(() => build.Build());

Task("__RunUnitTests")
    .IsDependentOn("__BuildSolution")
    .Does(() => build.Test());
    
Task("__CreateNuGetPackagesIfAppVeyorTag")
    .WithCriteria(() => build.IsAppVeyorTag)
    .IsDependentOn("__BuildSolution")
    .Does(() => build.CreateNuGetPackages());

Task("__PublishNuGetPackagesIfAppVeyorTag")
    .WithCriteria(() => build.IsAppVeyorTag)
    .Does(() => build.PublisNuGetPackages());


Task("__PublishGitHubReleaseIfAppVeyorTag")
    .WithCriteria(() => build.IsAppVeyorTag)
    .Does(() => build.PublishGitHubRelease())
    .OnError(exception =>
    {
        Information("__PublishGitHubReleaseIfAppVeyorTag Task failed, but continuing with next task...");
    });

Task("__DisplayVersion")
    .Does(() =>
{
    if(!build.IsRelease)
    {
        throw new Exception("Build in release before you tag!");
    }
    
    Console.WriteLine("Suggested tag for the next release is: " + build.Version);
});

Task("Build")
    .IsDependentOn("__Clean")
    .IsDependentOn("__RestoreNuGetPackages")
    .IsDependentOn("__BuildSolution")
    .IsDependentOn("__RunUnitTests")
    ;

Task("PublishToNuGetIfAppVeyorTag")
    .WithCriteria(() => build.IsAppVeyorTag)
    .IsDependentOn("__CreateNuGetPackagesIfAppVeyorTag")
    .IsDependentOn("__PublishNuGetPackagesIfAppVeyorTag")
    .IsDependentOn("__PublishGitHubReleaseIfAppVeyorTag")
    ;

///////////////////////////////////////////////////////////////////////////////
// PRIMARY TARGETS
///////////////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Build")
    ;

Task("AppVeyor")
    .IsDependentOn("Build")
    .IsDependentOn("PublishToNuGetIfAppVeyorTag")
    ;

Task("GenerateTag")
    .IsDependentOn("Build")
    .IsDependentOn("__DisplayVersion");
 
///////////////////////////////////////////////////////////////////////////////
// EXECUTION
///////////////////////////////////////////////////////////////////////////////

RunTarget(Argument("target", "Default"));
