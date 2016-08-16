using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoshuaKearney.FileSystem {

    using some = List<Tuple<string, int>>;

    /// <summary>
    /// Provides various extensions to assist with IO operations
    /// </summary>
    public static class Extensions {
        /// <summary>
        /// Gets the character representation of the current PathSeparator
        /// </summary>

        public static char GetCharacter(this PathSeparator seperator) => (char)seperator;

        public static bool ContainsInvalidPathChars(this string str) => StoragePath.InvalidPathCharacters.Any(x => str.Contains(x));

        //internal static string AsFileSafeString(this string str) {
        //    Contract.Assume(!str.ContainsInvalidPathChars());
        //    Contract.Assume(!Contract.Result<string>().ContainsInvalidPathChars());
        //    //Contract.Ensures(!Contract.Result<string>().ContainsInvalidPathChars());

        //    return str;
        //}
    }
}