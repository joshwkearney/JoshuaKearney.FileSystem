using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoshuaKearney.FileSystem {

    /// <summary>
    /// Provides various extensions to assist with IO operations
    /// </summary>
    public static class Extensions {

        /// <summary>
        /// Gets the character representation of the current PathSeparator
        /// </summary>
        public static char GetCharacter(this PathSeparator seperator) {
            return (char)seperator;
        }
    }
}