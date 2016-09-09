
# Artifactory

Automatically makes cool documents from your code.

## About

Artifactory uses a **builder** to create a **view model**, then create a **view** to pretty-format the view model as a 
document.

The most mature Artifactory component right now is the Web API documentation builder. It uses Roslyn to document the 
endpoints in a Web API project in a standalone html file.

## Usage

### Setup

First, set up the builder you want to use in the `App.config` of the `Artifactory.Console` project:

	<webApiBuilderSection
		SolutionPath="c:\somewhere\my-code\my-solution.sln"
		ControllerFilterRegex="" 
		ApiControllerSubclassesRegex="" />
		
- `SolutionPath` should be set to the location on disk of your `.sln` file.

- If `ControllerFilterRegex` is provided, controllers will only be documented if their class name matches the pattern.
    For example a pattern of `HomeController|AccountController` could be used to match only those two controllers. 
	Leave this blank to document all controllers in the solution.

- If `ApiControllerSubclassesRegex` is provided, a controller will be documented if its base class matches the pattern.
    Leave this blank and only controllers inheriting from `ApiController` will be documented.

### Run

Next, run the `Artifactory.Console` application with the name of the builder you would like:

    Artifactory.Console.exe web-api

Once it has run, it will launch the new html document in a browser tab.
