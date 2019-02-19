# DSCore - A FLStat/Wiki Replacement

DSCore, or Discovery Core, is a FLStat/Wiki replacement written in C# using the ASP.Net Core Framework (hence the name). It has been designed to allow for ease of access to new players and act as an information store, similar to the wiki. The major difference, is that the database has been designed to be extremely easy to update with the DSCore.Gen tool provided in this repo.

## Building

If you wish to build the project yourself, follow the steps below.


 1. Install .NET Core SDK 2.2, if you don't already have it
 2. Clone/Fork this repository
 3. Open a command prompt/terminal where you cloned the repo to
 4. Run ``dotnet build --configuration Release``
 5. Copy an install of Freelancer into the release directory of DSCore.Gen
 6. Go to DSCore/DSCore.Gen/bin/Release/netcoreapp2.2 inside the Console/Terminal and run ``dotnet DSCore.Gen.dll``
 7. Go inside the Output directory that is created by the application
 8. Copy FLData.db to DSCore/DSCore/bin/Release/netcoreapp2.2
 9. Copy the `DATA` folder to DSCore/DSCore/wwwroot
 10. Run ``dotnet DSCore.dll``

Done 
___

## Finish Writing Stuff Later
aka not now