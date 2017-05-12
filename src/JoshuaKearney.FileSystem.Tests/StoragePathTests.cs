using JoshuaKearney.FileSystem;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JoshuaKearney.FileSystem.Tests {

    [TestClass]
    public class StoragePathTests {

        [TestMethod]
        public void Printing() {
            StoragePath path = new StoragePath("////some\\..\\malformed/path\\\\here");

            Assert.AreEqual(path.ToString(), @"malformed\path\here");
            Assert.AreEqual(path.ToString(PathSeparator.ForwardSlash, true, true), "/malformed/path/here/");
            Assert.AreEqual(path.ToString(PathSeparator.BackSlash, false, true), @"malformed\path\here\");
        }

        [TestMethod]
        public void Combining() {
            StoragePath path = new StoragePath();

            path += "C:/";
            Assert.AreEqual(@"C:", path.ToString());

            path += (StoragePath)(new Uri("some/other", UriKind.RelativeOrAbsolute));
            Assert.AreEqual(@"C:\some\other", path.ToString());

            path = path.Combine("some///malformed\\\\other.some").SetExtension(".txt");
            Assert.AreEqual(@"C:\some\other\some\malformed\other.txt", path.ToString());

            path = path.SetExtension("txt");
            Assert.AreEqual(@"C:\some\other\some\malformed\other.txt", path.ToString());

            Assert.AreEqual("other.txt", path.ScopeToName().ToString());
            Assert.AreEqual("other", path.ScopeToNameWithoutExtension().ToString());
            Assert.AreEqual(@"C:\some\other\some\malformed", path.ParentDirectory.ToString());
            Assert.AreEqual(@"C:\some\other\some", path.GetNthParentDirectory(2).ToString());
        }

        [TestMethod]
        public void Properties() {
            StoragePath path = new StoragePath("////some\\malformed/path\\\\here");

            Assert.AreEqual("here", path.Name);
            Assert.AreEqual("", path.Extension);

            path = path.SetExtension("txt");

            Assert.AreEqual(".txt", path.Extension);
            Assert.AreEqual("here.txt", path.Name);
            Assert.AreEqual(false, path.IsAbsolute);

            path = new StoragePath("C:\\") + path;

            Assert.AreEqual(true, path.IsAbsolute);
            Assert.AreEqual("C:somemalformedpathhere.txt", string.Join("", path.Segments));
            Assert.AreEqual(new Uri(path.ToString()), path.ToUri());
        }

        //[TestMethod]
        //public void NullTests() {
        //    //StoragePath nullPath = null;
        //   // Assert.AreEqual(true, nullPath == null);

        //    StoragePath path = new StoragePath();
        //    string s1 = path.Extension;
        //    bool equals = path.IsAbsolute;

        //    Assert.AreEqual(s1, string.Empty);
        //    Assert.AreEqual(equals, false);

        //    StoragePath path2 = path + (StoragePath)null;
        //    Assert.AreEqual(new StoragePath(), path2);

        //    Assert.AreEqual(false, ((StoragePath)null) != null);
        //}
    }
}