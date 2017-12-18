// The following environment variables need to be set for Publish target:
// NANCY_NETLIFY_TOKEN

#tool "nuget:https://api.nuget.org/v3/index.json?package=Wyam&version=1.1.0"
#addin "nuget:https://api.nuget.org/v3/index.json?package=Cake.Wyam&version=1.1.0"
#addin "nuget:https://api.nuget.org/v3/index.json?package=Octokit&version=0.28.0"
#addin "nuget:https://api.nuget.org/v3/index.json?package=NetlifySharp"
#addin "nuget:https://api.nuget.org/v3/index.json?package=Newtonsoft.Json"
#addin "nuget:https://api.nuget.org/v3/index.json?package=System.Runtime.Serialization.Formatters"
#addin "nuget:https://api.nuget.org/v3/index.json?package=Cake.FileHelpers"

using Octokit;
using NetlifySharp;
using NetlifySharp.Models;

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var force = Argument("force", false);
var tagArgument = Argument<string>("tag", null);
RepositoryTag tag = null;

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
List<RepositoryTag> allTags = new List<RepositoryTag>();
List<RepositoryTag> tags = new List<RepositoryTag>();

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(context =>
{
    if(tagArgument == null)
    {
        Information("Processing all tags");
    }
    else
    {
        Information($"Processing tag {tagArgument}");
    }
    
    if(force)
    {
        Information("Force is true");
    }
});

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
        allTags = github.Repository.GetAllTags("NancyFx", "Nancy").Result.ToList();

        // Try to match the tag argument if one was specified
        if(tagArgument != null)
        {        
            tags = allTags.Where(x => x.Name == tagArgument).ToList();
            if(tags.Count != 1)
            {
                throw new ArgumentException($"Could not find tag {tagArgument}");
            }        
        }
        else
        {
            tags = allTags;
        }
    });

Task("GetSource")
    .IsDependentOn("GetTags")
    .Does(() =>
    {
        foreach(RepositoryTag tag in tags)
        {   
            Information($"Getting source code for tag {tag.Name}");

            // We only need to get source for releases that haven't been built yet 
            var releaseDir = releasesDir + Directory(tag.Name); 
            var versionDir = versionsDir + Directory(tag.Name);     
            if(DirectoryExists(releaseDir))
            {   
                if(force)
                {
                    DeleteDirectory(releaseDir, new DeleteDirectorySettings
                    {
                        Force = true,
                        Recursive = true
                    });
                }
                else
                {
                    Information($"Skipping tag {tag.Name}, source already downloaded");
                    continue;
                }
            }  
            if(DirectoryExists(versionDir) && !force)
            {   
                Information($"Skipping tag {tag.Name}, docs already built");
                continue;
            }

            Information($"Downloading source for {tag.Name}");                         
            FilePath releaseZip = DownloadFile(tag.ZipballUrl);
            Unzip(releaseZip, releaseDir);
        }
    });

Task("BuildApi")
    .IsDependentOn("GetTags")
    .IsDependentOn("GetSource")
    .Does(() =>
    {
        foreach(RepositoryTag tag in tags)
        {    
            Information($"Building API docs for tag {tag.Name}");

            // Ensure we have source and haven't already built the API docs
            var releaseDir = releasesDir + Directory(tag.Name); 
            var versionDir = versionsDir + Directory(tag.Name);   
            if(!DirectoryExists(releaseDir))
            {
                Information($"Skipping tag {tag.Name}, no release source available");
                continue;
            }
            if(DirectoryExists(versionDir) && !force)
            {
                Information($"Skipping tag {tag.Name}, API docs already built");
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
            //        { "SourceFiles",  MakeAbsolute(releaseDir).ToString() + "/*/src/**/*.cs" }
            //        { "ApiPath", $"api/{tag.Name}" }
            //    }
            //});            

            // Can remove once new version out
            StartProcess("../Wyam/src/clients/Wyam/bin/Debug/net462/wyam.exe",
                "-a \"../Wyam/src/**/bin/Debug/**/*.dll\" -r \"docs -i\" -t \"../Wyam/themes/Docs/Samson\""
                + $" --setting SourceFiles=\"{MakeAbsolute(releaseDir).ToString()}/*/src/{{*,!*.Tests}}/**/*.cs\""
                + $" --setting ApiPath=\"api/{tag.Name}\""
                + $" --config \"api.wyam\""
                + $" --output \"{MakeAbsolute(versionDir).ToString()}\""
                + $" -p");
        }
    });

Task("UploadApi")
    .IsDependentOn("BuildApi")
    .Does(() =>
    {
        var netlifyToken = EnvironmentVariable("NANCY_NETLIFY_TOKEN");
        var client = new NetlifyClient(netlifyToken);
        client.RequestHandler = x =>
        {
            Verbose($"{x.Method.Method} {x.RequestUri}");
        };
        var sites = client.ListSites().SendAsync().Result;
        Information("Got existing sites");

        foreach(RepositoryTag tag in tags)
        {           
            Information($"Uploading API docs for tag {tag.Name}");

            // Make sure we've actually built the docs
            var versionDir = versionsDir + Directory(tag.Name);
            if(!DirectoryExists(versionDir))
            {
                Information($"Skipping tag {tag.Name}, API docs have not been built");
                continue;
            }

            // Check if the site exists
            var siteName = $"nancy-api-{tag.Name.Replace(".", "-")}";
            Site site = sites.FirstOrDefault(x => x.Name == siteName);
            if(site != null && !force)
            {
                Information($"Skipping tag {tag.Name}, API docs have already been uploaded");
                continue;
            }

            // Create the site if it doesn't exist
            if(site == null)
            { 
                Information($"Creating site {siteName}.netlify.com");
                site = client.CreateSite(new SiteSetup(client)
                {
                    Name = siteName
                }).SendAsync().Result;
            }

            // Upload the content
            site.UpdateSite(MakeAbsolute(versionDir).FullPath).SendAsync().Wait();      
            Information($"Uploaded {siteName}.netlify.com");
        }
    });    

Task("BuildDocs")
    .IsDependentOn("GetTags")
    .Does(() =>
    {
        // Build the docs
        Wyam(new WyamSettings
        {
            Recipe = "Docs",
            Theme = "Samson",
            UpdatePackages = true
        });

        // Create the versions JSON file
        FileWriteText(outputDir + File("versions.json"), "[" + string.Join(",", allTags.Select(x => "\"" + x.Name + "\"")) + "]");

        // Create the version redirects
        FileWriteText(outputDir + File("_redirects"), string.Join(Environment.NewLine, allTags.Select(x => $"/api/{x.Name}/* http://nancy-api-{x.Name.Replace(".", "-")}.netlify.com/api/{x.Name}/:splat 200")));
    });

Task("UploadDocs")
    .IsDependentOn("BuildDocs")
    .Does(() =>
    {
        var netlifyToken = EnvironmentVariable("NANCY_NETLIFY_TOKEN");
        var client = new NetlifyClient(netlifyToken);
        client.RequestHandler = x =>
        {
            Verbose($"{x.Method.Method} {x.RequestUri}");
        };  
        Information($"Uploading docs");

        // Make sure we've actually built the docs
        if(!DirectoryExists(outputDir))
        {
            Information($"Skipping, docs have not been built");
            return;
        }

        // Check if the site exists        
        var sites = client.ListSites().SendAsync().Result;
        Information("Got existing sites");
        Site site = sites.FirstOrDefault(x => x.Name == "nancy");

        // Create the site if it doesn't exist
        if(site == null)
        { 
            Information($"Creating site nancy.netlify.com");
            site = client.CreateSite(new SiteSetup(client)
            {
                Name = "nancy"
            }).SendAsync().Result;
        }

        // Upload the content
        site.UpdateSite(MakeAbsolute(outputDir).FullPath).SendAsync().Wait();      
        Information($"Uploaded nancy.netlify.com");
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
    .IsDependentOn("BuildApi")
    .IsDependentOn("BuildDocs");

Task("Upload")
    .IsDependentOn("UploadApi")
    .IsDependentOn("UploadDocs");
    
Task("Default")
    .IsDependentOn("Build");
    
//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);