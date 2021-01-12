# Cpk-Tmx-Tool
Extracts/Replaces TMX files from special CPK files found in the PS2 Persona Games.

```
Parameter overview:
     -In       [Required]     <path to file/folder>      Provides the input for extracting/replacing. Only takes in
                                                         file path if replacing. If a folder path is specified for
                                                         extracting, all .cpk files within will be extracted.
     -Out      [Optional]     <path to file/folder>      Provides the output for extracting/replacing. Only takes in
                                                         folder path if extracting.
     -Extract  [Action]                                  Extracts tmx's from the cpk file(s) specified from -In
     -Replace  [Action]       <path to file/folder>      Replaces the tmx file/files in folder in the cpk file
                                                         specified from -In.
```
