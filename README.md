This repository builds the Nancy documentation. While the published version appears to be a single site, the Nancy docs are actually split into a single site for primary content and individual sites for each API version. This allows atomic publishing on platforms like Netlify for new versions without having to republish every individual version. The content site and the API version sites are combined using proxy settings from API URLs to each version site.

# Building

Both the content documentation and the API documentation are generated by [Wyam](https://wyam.io). [Cake](https://cakebuild.net) is also used to orchestrate the build and perform tasks like fetching tag names, downloading source code, and generating host files like the proxy definitions.

There are two Wyam configuration files: `config.wyam` for the main content site and `api.wyam` for the API sites. These are used when needed by the Cake build script.

The following Cake targets are defined:

* `GetTags` downloads the full set of known source control tags from GitHub.
* `GetSource` downloads source code for each tag.
* `BuildApi` generates the API documentation for each tag.
* `UploadApi` uploads the API documentation for each tag, creating the hosting site as needed.
* `BuildDocs` generates the main content site.
* `UploadDocs` uploads the main content site.
* `Debug` uses a local version of Wyam to generate the main content site.
* `Preview` previews a local version of the main content site (must have already been built).

All of the targets that deal with the API are smart enough not to duplicate work. That is, if the source code for a given tag has already been downloaded it will not be downloaded again. However, there are times when you may want to run the task even if the job has already been performed such as when the layout files change and you want to regenerate API documentation. Additionally, you may want to only perform the task for a specific tag and not all tags. These options can be controlled with the `tag` and `force` arguments:

```
build -Target UploadApi -ScriptArgs '-tag=v0.7.0 -force=true'
```

# Folder Structure

Because both the main content site and the API sites need to share common assets and layouts, the input folders are structured in a way to promote sharing:

* `api` contains layout overrides specific to the API documentation.
* `common` contains assets and layout overrides common to both types of sites.
* `input` contains files for the main content site.

The following folders are created as a result of the various build targets and steps:

* `output` contains the generated output of the main content site.
* `releases` contains the source code for each version.
* `versions` contains the generated API documentation for each version.