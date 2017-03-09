#tool "nuget:?package=xunit.runner.console"
//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var uri = Argument("uri", "https://localhost:8081/");
var authKey = Argument("authKey", "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define directories.
var solutionFile = "../src/SimpleEventStore/SimpleEventStore.sln";
var documentDbTestConfigFile = File("../src/SimpleEventStore/SimpleEventStore.AzureDocumentDb.Tests/bin/" + configuration + "/SimpleEventStore.AzureDocumentDb.Tests.dll.config");

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Restore-NuGet-Packages")
    .Does(() =>
{
    NuGetRestore(solutionFile);
});

Task("Build")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() =>
{
    // Use MSBuild
    MSBuild(solutionFile, settings => settings.SetConfiguration(configuration));
});

Task("Transform-Unit-Test-Config")
    .Does(() =>
{
    XmlPoke(documentDbTestConfigFile, "/configuration/appSettings/add[@key = 'Uri']/@value", uri);
    XmlPoke(documentDbTestConfigFile, "/configuration/appSettings/add[@key = 'AuthKey']/@value", authKey);
});

Task("Run-Unit-Tests")
    .IsDependentOn("Build")
    .IsDependentOn("Transform-Unit-Test-Config")
    .Does(() =>
{
    XUnit2("../src/**/bin/" + configuration + "/*.Tests.dll");
});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Run-Unit-Tests");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
