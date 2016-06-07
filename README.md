# JoshuaKearney.FileSystem #

This project was created out of some pieces of code that I found were duplicated across four or more of my projects, so I decided to create a repo. The goal of this project is not to replace System.IO, it is simply to supplement the existing APIs and to make file IO several times easier.


### Path simplification / normalization ###
    string someCompletelyMalformedPath = "\\folder///next\foo//\/bar/../file.txt";
    StoragePath normalized = new StoragePath(someCompletelyMalformedPath);

    Console.WriteLine(normalized); // folder\next\foo\file.txt

### Path utilities ##
    Console.WriteLine(normalized.Name); // file.txt
    Console.WriteLine(normalized.NameWithoutExtension); // file
    Console.WriteLine(normalized.Extension); // .txt
    Console.WriteLine(normalized.ParentDirectory); // folder\next\foo
    Console.WriteLine(normalized.IsAbsolute); // False

    // Same as normalized.ParentDirectory.Combine("new.txt")
    Console.WriteLine(normalized.ParentDirectory + "new.txt"); // folder\next\foo\new.txt
    Console.WriteLine(normalized.ParentDirectory.Combine("\\this/other\\")); // folder\next\foo\this.other

### DirectoryBuilder ###
    private static async void MakeDirectory() {
        DirectoryBuilder b = new DirectoryBuilder(@"your/path/here");
        b.ConflictResolution = NameConflictOption.Rename;

        // Note - all methods that recieve a string path can also recieve a StoragePath
        b.AppendFile("this.dat");
        b.AppendFile("this/other/that.txt", "This contents there");
        b.AppendDirectory("some");
        b.AppendFile("info.dat", new byte[] { 0xf, 0x8, 0xa });

        // If this is a file, copy it. If its a directory, deep copy it
        b.AppendExisting("other/path");

        // Extract this zip contents to the target directory
        b.AppendZipContents("zip/path");

        // Builds the directory specified above in "your/path/here"
        await b.BuildAsync();

        Console.WriteLine("Done");
    }

# NuGet #
This project is on nuget, and can be found [here](https://www.nuget.org/packages/JoshuaKearney.FileSystem/).


# Liscense #
This project is licensed with the MIT open source license, and you may use, contribute, copy, ect this software as you please.
