using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace PhotoRecon
{
    class Program
    {
        static void Main(string[] args)
        {
            // Old
            var oldDirectories = GetDirectories(@"c:\pictures").Union(GetDirectories(@"d:\pictures"));
            var oldFiles = GetFiles(oldDirectories).ToList();
            var oldList = FilesToDictionary(oldFiles);

            Console.WriteLine($"Found {oldFiles.Count} files and {oldList.Count} unique files in old.");

            // var oldString = JsonConvert.SerializeObject(oldFiles);
            // File.WriteAllText(@".\original-index.json", oldString);

            // New
            var newDirectories = GetDirectories(@"D:\Lightroom\Lightroom CC\{libraryguid}\originals");
            var newFiles = GetFiles(newDirectories).ToList();
            var newList = FilesToDictionary(newFiles);

            Console.WriteLine($"Found {newFiles.Count} and {newList.Count} unique files in new.");

            // XOR. We want stuff that is in left or right but not both.
            var missingInNew = oldList.Keys.Except(newList.Keys).ToList();

            Console.WriteLine($"{missingInNew.Count} missing from new");

            foreach (var path in missingInNew)
            {
                var file = oldList[path];

                // Transformations
                var transformed = path.Replace(".jpg", "-2.jpg");
                if (newList.ContainsKey(transformed))
                {
                    // Console.WriteLine($"Found {path} at {transformed}");
                    continue;
                }

                Console.WriteLine(file);
            }

            var missingInOld = newList.Keys.Except(oldList.Keys).ToList();

            Console.WriteLine($"{missingInOld.Count} missing from old");

            foreach (var path in missingInOld)
            {
                var file = newList[path];

                // Transformations
                var transformed = path.Replace("-2.", ".").Replace("-3.", ".");
                if (oldList.ContainsKey(transformed))
                {
                    //Console.WriteLine($"Found {path} at {transformed}");
                    continue;
                }

                Console.WriteLine(file);
            }
        }

        public static Dictionary<string, Photo> FilesToDictionary(IEnumerable<Photo> photos)
        {
            var d = new Dictionary<string, Photo>();

            foreach (var p in photos)
            {
                // Allow duplicates. It's only important that we've transferred the file.
                if (d.ContainsKey(p.UniqueId))
                {
                    Console.WriteLine($"WARN: Already found {p} at {d[p.UniqueId]}");
                }
                d[p.UniqueId] = p;
            }

            return d;
        }

        public static IEnumerable<Photo> GetFiles(IEnumerable<Dir> directories)
        {
            return from directory in directories.AsParallel()
            let files = GetFiles(directory)
            from f in files
            select f;
        }

        public static IEnumerable<Photo> GetFiles(Dir directory)
        {
            var directories = new Queue<Dir>();
            directories.Enqueue(directory);

            while (directories.TryDequeue(out Dir dir))
            {
                // Console.WriteLine($"Looking for files in {directory.FullPath}");
                foreach (var d in Directory.GetDirectories(dir.FullPath))
                {
                    directories.Enqueue(new Dir()
                    {
                        Root = dir.FullPath,
                            FullPath = d,
                    });
                }

                foreach (var file in Directory.GetFiles(dir.FullPath))
                {
                    if (file.EndsWith("AVI", StringComparison.InvariantCultureIgnoreCase) ||
                        file.EndsWith("MPG", StringComparison.InvariantCultureIgnoreCase))
                    {
                        continue;
                    }

                    yield return new Photo()
                    {
                        Directory = dir,
                            FullPath = file,
                    };;
                }
            }
        }

        public static IEnumerable<Dir> GetDirectories(string root)
        {
            var directories = Directory.GetDirectories(root);
            foreach (var directory in directories)
            {
                if (directory.StartsWith(root + @"\00", StringComparison.InvariantCultureIgnoreCase) ||
                    directory.StartsWith(root + @"\20", StringComparison.InvariantCultureIgnoreCase))
                {
                    foreach (var subDirectory in Directory.GetDirectories(directory))
                    {
                        yield return new Dir()
                        {
                            Root = root,
                            FullPath = subDirectory,
                        };
                    }
                }

                if (directory.StartsWith(root + @"\Grow", StringComparison.InvariantCultureIgnoreCase) ||
                    directory.StartsWith(root + @"\David", StringComparison.InvariantCultureIgnoreCase))
                {
                    yield return new Dir
                    {
                        Root = root,
                        FullPath = directory,
                    };
                }
            }
        }
    }

    public class Dir
    {
        [JsonIgnore]
        public string Root { get; set; }
        public string FullPath { get; set; }

        [JsonIgnore]
        public string RelativePath
        {
            get
            {
                return FullPath.Substring(Root.Length);
            }
        }

        public override string ToString() => RelativePath;
    }

    public class Photo
    {
        [JsonIgnore]
        public Dir Directory { get; set; }

        public string FullPath { get; set; }

        [JsonIgnore]
        public string RelativePath => FullPath.Substring(Directory.FullPath.Length);

        /// <summary>
        /// Unique Id for the file; should be constant regardless of where the file lives.
        /// </summary>
        /// <remarks>
        /// So that this remains performant, we'll simply use file size as an approximation
        /// for hashing the file contents; it's not likely two photos have exactly the same size.
        /// </remarks>
        [JsonIgnore]
        public string UniqueId
        {
            get
            {
                var size = new System.IO.FileInfo(FullPath).Length;
                return $"{RelativePath},{size}".ToLowerInvariant();
            }
        }

        public override string ToString()
        {
            return $"Path={FullPath}, UniqueId={UniqueId}, DirectoryRelativePath={Directory.RelativePath}";
        }
    }
}
