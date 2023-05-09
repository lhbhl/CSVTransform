# CSVTransform
A small tool for conversion of .csv files between two formats

## Prerequisites:
<br/>
Visual Studio 2022, .Net Desktop Development Packages, Windows Forms
</br></br>
MaterialDesign in Xaml:

```
PM> Install-Package MaterialDesignThemes
```
## Usage

1.Choose Files</br>
2.Convert</br>
3.Choose output Folder</br>
### Optional:
Conversion Rules for additional formats can be specified in an .xml file.
A sample file for conversion of a test format to CAD / CAQ Excel ASCII - Table feature interchange format Version 4.0 is supplied in Rules/toEAT40.xml
### Even more optional
Added option to ask openai text davinci 003 for a new .xml file by inputting example .csv source and target text. Untested, working API - Key required.
