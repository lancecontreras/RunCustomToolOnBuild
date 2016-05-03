# RunCustomToolOnBuild
This is a visual studio 2015 extension that will Allow the custom tool on every project item to run during build so that there's no need to run/save each custom tools manually. This can be very helpful on projects that are using resource(resx) and TextTemplate (tt) files. 

I also give credit to thomaslevesque, author of AutoRunCustomTool where I got most of the codes here. 
https://github.com/thomaslevesque/AutoRunCustomTool

Background : I created this because I am working on a project that will run in multiple platforms, which is .NET 2.0, .NET4 and UWP. I am sharing a resource file across those projects and each project has their own way or version of custom tool which gives me different output when i ran them individually. Whenever I will switch from working on one project to another, I have to run each custom tools on every TT and resource files in each projects. This tool saved me from that effort. 

How to Use: 
1. Install the VSIX in Visual studio. 
2. Right click on the Resource (*.resx) or TextTemplate (*.tt) file then click properties. 
3. In the properties, set the property "RunCustomToolOnBuild" to true. 
4. Build the project/solution. 

