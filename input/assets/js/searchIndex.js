// Dynamically load the version-specific search index based on the URL
var versionRegex = /(\/api\/(.*?)(\/|$))/;
var currentVersion = versionRegex.exec(window.location.href)[2];
$.getScript( "https://nancy-api-" + currentVersion.split('.').join('-') + ".netlify.com/assets/js/searchIndex.js" );