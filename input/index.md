Title: Lightweight Web Framework For .NET
NoSidebar: true
NoContainer: true
NoGutter: true
---
<div class="jumbotron jumbotron-intro">
	<div class="container-background">
		<div class="container">
			<h1>Lightweight web framework for .NET</h1>
            <div class="row">
                <div class="col-md-6" id="instructions">
                    <h2>Install</h2>
                    <pre>PM&gt; Install-Package Nancy</pre>
                    <h2>Write</h2>
<pre>public class SampleModule : Nancy.NancyModule
{
    public SampleModule()
    {
        Get["/"] = _ =&gt; "Hello World!";
    }
}</pre>
                    <h2>Go!</h2>
                </div>
                <div class="col-md-6">
                    <ul id="navigation">
                        <li><a href="http://blog.nancyfx.org/" target="_blank">Blog</a></li>
                        <li><a href="https://github.com/NancyFx" target="_blank">Source Code</a></li>
                        <li><a href="/docs">Documentation</a></li>
                        <li><a href="/mvm">MVM Program</a></li>
                        <li><a href="https://slack.nancyfx.org/" target="_blank">Chat</a></li>
                        <li><a href="/contribs">Contributors</a></li>
                        <li><a href="http://nancyfx.spreadshirt.net/" target="_blank">Swag<sup>[EU]</sup></a> / <a href="http://nancyfx.spreadshirt.com/" target="_blank">Swag<sup>[US]</sup></a></li>
                    </ul>
                </div>
            </div>
		</div>
	</div>
</div>

<div class="container">
<div class="row">
<div class="col-md-6">

# Documentation

[Documentation](/docs)

[Breaking Changes](/docs/resources/breaking-changes)

[Statement on strong naming](/docs/resources/statement-on-strong-naming)

# Community

[(Slack) For realtime question / answers](http://slack.nancyfx.org/)

[Nancy tag](http://stackoverflow.com/questions/tagged/nancy) on Stack Overflow for question / answers

[Nancy Community Blog](http://blog.nancyfx.org/)

You can also follow the discussion on Twitter using the [#NancyFx](http://twitter.com/search?q=%23Nancyfx) hashtag.

[Blog Posts, Video & Audio](/docs/resources/blog-post-video-and-audio)

# Extensions

## Logging 
- [Nancy and New Relic](/docs/nancy-and-new-relic)
- [Nancy Elmah](https://github.com/creamdog/Nancy.Elmah) (Exception logging and viewing using [Elmah](https://code.google.com/p/elmah/))
- [Nancy.Raygun](http://nuget.org/packages/Nancy.Raygun/) ([raygun.io](http://www.raygun.io) provider)
- [Nancy.Serilog](https://github.com/Zaid-Ajaj/Nancy.Serilog) Serilog provider for application-wide logging

## Caching
- [Nancy LightningCache](https://github.com/creamdog/Nancy.LightningCache) (Asynchronous route-specific caching)

## Front-end performance 
- [Nancy.AspNetSprites.Razor](https://github.com/JefClaes/Nancy.AspNetSprites.Razor) 
- [Cassette.Nancy](https://github.com/ChrisMH/Cassette.Nancy)
- [Squishit with Nancy](/docs/static-content/squishit-with-nancy)
- [System.Web.Optimization with Nancy](/docs/how-to/how-to-use-system-web-optimization-bundling-with-nancy)

## Authentication
- [SimpleAuthentication (Manual Setup)](https://github.com/SimpleAuthentication/SimpleAuthentication/wiki/NancyFX-Manual-Setup)
- [SimpleAuthentication (Automatic Setup)](https://github.com/SimpleAuthentication/SimpleAuthentication/wiki/NancyFX-Automatic-Setup)

## Serialization
- [Extending Serialization with Converters](/docs/using-models/extending-serialization-with-converters)

## Enhancements
- [Nancy plugin for ReSharper](https://github.com/NancyFx/Nancy.ReSharper)

## Contributing
* [What can you help us out with](/docs/contributing/what-can-you-help-us-out-with)
* [Getting the source code](/docs/contributing/getting-the-source-code)
* [Make sure line endings doesn't bite you](/docs/contributing/make-sure-line-endings-doesnt-bite-you)
* [There are a couple of frameworks you should know about](/docs/contributing/there-are-a-couple-of-frameworks-you-should-know-about)
* [Managing dependencies the right way](/docs/contributing/managing-dependencies-the-right-way)
* [Git Workflow](/docs/contributing/git-workflow)
* [Having trouble with rake?](/docs/contributing/having-trouble-with-rake)
* Coding styles (+ ReSharper file and test naming)
* SharedAssemblyInfo.cs / Breaking all your projects on commit ;-)

</div>
<div class="col-md-6">

# API Documentation

**[Latest](/api)**

<span id="versions" />

</div>
</div>
</div>

<script>
    (function() {
    $.ajax({
        url: "/versions.json",
        type: "GET",
        dataType: "json",
        success: function( data ) {
            $.each(data, function( i, item ) {
                $("#versions").append('<p><a href="/api/' + item + '">' + item + '</a></p>');
            });
        }
        })
    })();
</script> 
