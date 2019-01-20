using System;
using System.IO;
using LiteDB;
using MadMilkman.Ini;

namespace DSCore.Gen
{
    class Program
    {
        static void Main(string[] args)
        {
            string directory = Directory.GetCurrentDirectory();
            if (!File.Exists(directory + @"\EXE\Freelancer.exe"))
            {
                Console.WriteLine("Freelancer.exe not found. Assuming not in the correct directory. Please move this file to the root of your Freelancer install.");
                Console.ReadLine();
                return;
            }


        }
    }
}
