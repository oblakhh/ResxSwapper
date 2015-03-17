# ResxSwapper
Utility for swapping the neutral language of a WinForms project with one of the already translated languages.

## General

This command line utility is intended for C# / WinForms developers who want to swap the neutral language of their application with values from an already existing translation.

### The Problem

You have an existing project that you may have started locally, in your native language and over time you went international. In my case I started with German with one of my projects. When the project got friction I soon regretted not to have started with English. I had an English translation e.g. `FormMain.en.resx` but if the app was started in an environment where no direct translation was avaiable (e.g. Russian) the app would fall back to German.

This is because, if the runtime does not find a matching translation, e.g. `FormMain.ru.resx`, it will fall back to the neutral cultured which strings are defined in `FormMain.resx`.

> Of course during runtime, the values will not be read from the satellite assemblies and not the resource files, but let's stick with the analogy for the explanation of the problem.

### What to do about it ###

There are things like `Thread.CurrentCulture`, but these are only workarounds to the basic problem: You've chosen the wrong neutral culture. In fact, there may only be two cultures world wide which I'd consider as neutral: English and Chinese. As soon as you have an international audience you should decide for one of the two.

So, given the example of a WinForm `FormMain` which neutral language is `de` and a given translation `en`, what you want to do is:

1. Create a new `FormMain.de.resx`
2. Copy all German resource values from `FormMain.resx` to `FormMain.de.resx`
3. Copy all English resource values from `FormMain.en.resx` to `FormMain.resx`
4. Move `FormMain.en.resx` to recycle bin

Now the neutral language will be English and you have a German translation.

### What this utility does ###

There are tons (hundreds in my case) of Forms and Message files that I would need to swap. Visual Studio does not help you with this particular problem.

Basically the current version does what is proposed on the above list, with one **exception**: To determine the list of values to export from the neutral to the new satellite translation (e.g. `de`), only the translated values (e.g. `en`) are evaluated. 
This is mainly done because I did not want to dig too deep into resx handling. So, only values that are translated on your new neutral language will be swapped. So be sure that the translation is complete. Otherwise some old values will stay in the neutral resx file and to be adjusted manually. 
This could be improved in the future.  


## Usage ##

Please refer to the source code to get an idea. An expierienced developer will unterstand easily what this tool does. I think the tool is not tested enough to provide a manual for non-developers yet. This may be added in the future.