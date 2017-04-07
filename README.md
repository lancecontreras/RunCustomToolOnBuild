# Background
I created this because I am working on a multiple solution that share the same project that will build different platforms, .NET 2.0, .NET4.0 and UWP. This project contains some files that uses custom tool to generate another file. Every platform has their own version of the custom tool and it generates different output. So to prevent me from running the custom tool every time I switch from one solution to the other, I created this tool to automatically run the custom tools so I don't have to search for each files and run the custom tool manually.

# RunCustomToolOnBuild
![alt text](https://github.com/lancecontreras/RunCustomToolOnBuild/blob/master/RunTool.png)

RunCustomtoolOnBuild is a visual studio extension that tries to run the custom tools associated to project files on build. If you're working on a project that uses custom tool to generate a file (i.e. resx, resw and tt), this tool will run those custom tool automatically if it feels that the generated file is not updated. That it saves you time from running custom tool on each files.

Credits to [thomaslevesque](https://github.com/thomaslevesque), author of [AutoRunCustomTool](https://github.com/thomaslevesque/AutoRunCustomTool) where I got most of the codes from.

## How to Use:

1. Install the VSIX in Visual studio.
2. Right click on the Resource (*.resx) or TextTemplate (*.tt) file then click properties.
3. In the properties, set the property "RunCustomToolOnBuild" to true.
4. Build the project/solution.

## Download from [Visual Studio Gallery](https://marketplace.visualstudio.com/items?itemName=LanceContreras.RunCustomToolOnBuild)
1. In Visual Studio click on the Tools>Extensions and Updates menu.
2. On the left pane, click on "Online"
3. Then on the right pane search for "RunCustomToolOnBuild"
4. Then click download on RunCustomToolOnBuild
