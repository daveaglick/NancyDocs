#tool "nuget:https://api.nuget.org/v3/index.json?package=Wyam&version=1.1.0"
#addin "nuget:https://api.nuget.org/v3/index.json?package=Cake.Wyam&version=1.1.0"
#addin "nuget:https://api.nuget.org/v3/index.json?package=Octokit&version=0.28.0"

using Octokit;

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define environment
var isRunningOnAppVeyor = AppVeyor.IsRunningOnAppVeyor;
var isPullRequest       = AppVeyor.Environment.PullRequest.IsPullRequest;
var accessToken         = EnvironmentVariable("git_access_token");

// Define directories
var releasesDir         = Directory("./releases");  // contains the release source
var versionsDir         = Directory("./versions");  // contains built version output
var outputDir           = Directory("./output");

// Define variables
List<RepositoryTag> tags = new List<RepositoryTag>();

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("GetTags")
    .Does(() =>
    {
        // For some reason the Nancy repo isn't returning all versions via releases API
        // so we have to get them via tags - thankfully only releases are tagged
        GitHubClient github = new GitHubClient(new ProductHeaderValue("NancyDocs"));
        if (!string.IsNullOrEmpty(accessToken))
        {
            github.Credentials = new Credentials(accessToken);
        }
        tags = github.Repository.GetAllTags("NancyFx", "Nancy").Result.ToList();
    });

Task("GetSource")
    .IsDependentOn("GetTags")
    .Does(() =>
    {
        foreach(RepositoryTag tag in tags)
        {   
            // We only need to get source for releases that haven't been built yet 
            var releaseDir = releasesDir + Directory(tag.Name); 
            var versionDir = versionsDir + Directory(tag.Name);     
            if(DirectoryExists(releaseDir))
            {   
                Information($"Skipping version {tag.Name}, source already downloaded");
                continue;
            }  
            if(DirectoryExists(versionDir))
            {   
                Information($"Skipping version {tag.Name}, docs already built");
                continue;
            }

            Information($"Downloading source for {tag.Name}");                         
            FilePath releaseZip = DownloadFile(tag.ZipballUrl);
            Unzip(releaseZip, releaseDir);

            break;
        }
    });

Task("BuildApiDocs")
    .IsDependentOn("GetTags")
    .Does(() =>
    {
        foreach(RepositoryTag tag in tags)
        {    
            // Ensure we have source and haven't already built the API docs
            var releaseDir = releasesDir + Directory(tag.Name); 
            var versionDir = versionsDir + Directory(tag.Name);   
            if(!DirectoryExists(releaseDir))
            {
                Information($"Skipping version {tag.Name}, no release source available");
                continue;
            }
            if(DirectoryExists(versionDir))
            {
                Information($"Skipping version {tag.Name}, API docs already built");
                continue;
            }

            // Build the docs   
            //Wyam(new WyamSettings
            //{
            //    ConfigurationFile = File("./api.wyam"),
            //    Recipe = "Docs",
            //    Theme = "Samson",
            //    UpdatePackages = true,
            //    OutputPath = versionDir,                
            //    Settings = new Dictionary<string, object>
            //    {
            //        { "SourceFiles",  MakeAbsolute(releaseDir).ToString() + "/*/src/**/*.cs" },
            //        { "ApiPath", $"api/{tag.Name}" }
            //    }
            //});            

            // Can remove once new version out
            StartProcess("../Wyam/src/clients/Wyam/bin/Debug/net462/wyam.exe",
                "-a \"../Wyam/src/**/bin/Debug/**/*.dll\" -r \"docs -i\" -t \"../Wyam/themes/Docs/Samson\""
                + $" --setting SourceFiles=\"{MakeAbsolute(releaseDir).ToString()}/*/src/**/*.cs\""
                + $" --setting ApiPath=\"api/{tag.Name}\""
                + $" --config \"api.wyam\""
                + $" --output \"{MakeAbsolute(versionDir).ToString()}\"");
        }
    });

Task("BuildDocs")
    .Does(() =>
    {
        Wyam(new WyamSettings
        {
            Recipe = "Docs",
            Theme = "Samson",
            UpdatePackages = true
        });
    });

Task("CopyApiDocs")
    .IsDependentOn("GetTags")
    .Does(() =>
    {
        // Make sure we've got an output folder
        if(!DirectoryExists(outputDir))
        {
            throw new Exception("You must build the site before copying API docs");
        }

        foreach(RepositoryTag tag in tags)
        {    
            // Only copy if we have generated API docs and they don't already exist in the output
            var versionDir = versionsDir + Directory(tag.Name);
            var apiDir = outputDir + Directory("api") + Directory(tag.Name);
            if(!DirectoryExists(versionDir))
            {
                Information($"Skipping version {tag.Name}, API docs have not been built");
                continue;
            }
            if(DirectoryExists(apiDir))
            {
                Information($"Skipping version {tag.Name}, API docs have already been copied");
                continue;
            }

            CopyDirectory(versionDir + Directory("api") + Directory(tag.Name), apiDir);
        }
    });

// Assumes Wyam source is local and at ../Wyam
Task("Debug")
    .Does(() =>
    {
        StartProcess("../Wyam/src/clients/Wyam/bin/Debug/net462/wyam.exe",
            "-a \"../Wyam/src/**/bin/Debug/**/*.dll\" -r \"docs -i\" -t \"../Wyam/themes/Docs/Samson\" -p -w");
    });
    
// Assumes Wyam source is local and at ../Wyam
Task("Preview")
    .Does(() =>
    {
        StartProcess("../Wyam/src/clients/Wyam/bin/Debug/net462/wyam.exe", "preview");
    });

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Build")
    .IsDependentOn("GetSource")
    .IsDependentOn("BuildApiDocs")
    .IsDependentOn("BuildDocs")
    .IsDependentOn("CopyApiDocs");
    
Task("Default")
    .IsDependentOn("Build");
    
//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

if (!StringComparer.OrdinalIgnoreCase.Equals(target, "Deploy"))
{
    RunTarget(target);
}