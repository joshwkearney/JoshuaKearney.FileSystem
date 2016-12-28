using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoshuaKearney.FileSystem {
    using System.IO;
    using some = List<Tuple<string, int>>;

    /// <summary>
    /// Provides various extensions to assist with IO operations
    /// </summary>
    public static class Extensions {
        /// <summary>
        /// Gets the character representation of the current PathSeparator
        /// </summary>
        public static char GetCharacter(this PathSeparator seperator) => (char)seperator;
    }
}