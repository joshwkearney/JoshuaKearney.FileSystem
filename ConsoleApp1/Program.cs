using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JoshuaKearney.FileSystem;

namespace ConsoleApp1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            DirectoryBuilder b = new DirectoryBuilder("/Some/");
            b.Build();

            Console.WriteLine(new StoragePath("/Some/test.txt"));
            Console.Read();
        }
    }
}
