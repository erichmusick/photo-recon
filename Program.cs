using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaDevices;
using Newtonsoft.Json;
using photo_recon.Filters;

namespace PhotoRecon
{
    class Program
    {
        private const string reportFileName = "report.json";

        public static void Main(string[] args)
        {
            var sources = new[] {
                @"\Internal storage/WhatsApp/Media/WhatsApp Images dupes",
                //@"\Internal storage\Android\media\com.whatsapp\WhatsApp\Media\WhatsApp Images dupes",
            };

            var destination = @"\Internal storage\Android\media\com.whatsapp\WhatsApp\Media\WhatsApp Images";


            var recon = new Reconciler().ExcludeExtensions();
            var report = recon.Execute(sources, destination);

            report.SaveReport(reportFileName);
            Console.WriteLine($"Report saved to {reportFileName}");
        }
    }

    public class Reconciler
    {
        private ReportBuilder report;
        private readonly ICollection<IFileFilter> _fileFilters;

        public Reconciler()
        {
            _fileFilters = new List<IFileFilter>();
        }

        public void AddFilter(IFileFilter filter)
        {
            _fileFilters.Add(filter);
        }

        public ReportBuilder Execute(string[] sourceDirectories, string destinationDirectory)
        {
            report = new ReportBuilder();

            // Old
            //var oldDirectories = GetDirectories(sourceDirectories);
            var oldFiles = GetFilesFromDevice(sourceDirectories).ToList();
            var oldList = FilesToDictionary(LocationType.Source, oldFiles);

            Console.WriteLine($"Found {oldFiles.Count} files and {oldList.Count} unique files in old.");

            // New
            //var newDirectories = GetDirectories(destinationDirectory);
            var newFiles =  GetFilesFromDevice(new[] {destinationDirectory}).ToList();
            var newList = FilesToDictionary(LocationType.Destination, newFiles);

            Console.WriteLine($"Found {newFiles.Count} and {newList.Count} unique files in new.");

            // XOR. We want stuff that is in left or right but not both.
            var missingInNew = oldList.Keys.Except(newList.Keys).ToList();

            Console.WriteLine($"{missingInNew.Count} missing from new");

            foreach (var path in missingInNew)
            {
                var file = oldList[path];

                // Transformations
                // var transformed = path.Replace(".jpg", "-2.jpg");
                // if (newList.ContainsKey(transformed))
                // {
                //     if (newList[transformed].Length != path.)
                //     // Console.WriteLine($"Found {path} at {transformed}");
                //     continue;
                // }

                report.AddMissingFile(LocationType.Source, file);
            }

            // var missingInOld = newList.Keys.Except(oldList.Keys).ToList();

            // Console.WriteLine($"{missingInOld.Count} missing from old");

            // foreach (var path in missingInOld)
            // {
            //     var file = newList[path];

            //     // Transformations
            //     var transformed = path.Replace("-2.", ".").Replace("-3.", ".");
            //     if (oldList.ContainsKey(transformed))
            //     {
            //         //Console.WriteLine($"Found {path} at {transformed}");
            //         continue;
            //     }

            //     report.AddMissingFile(LocationType.Destination, file);
            // }

            return report;
        }

        public Dictionary<string, Photo> FilesToDictionary(LocationType location, IEnumerable<Photo> photos)
        {
            var d = new Dictionary<string, Photo>();

            foreach (var p in photos)
            {
                // Allow duplicates. It's only important that we've transferred the file.
                if (d.ContainsKey(p.UniqueId))
                {
                    report.AddDuplicateFile(location, p, d[p.UniqueId]);
                }
                d[p.UniqueId] = p;
            }

            return d;
        }

        private IEnumerable<Photo> GetFiles(IEnumerable<Dir> directories)
        {
            return from directory in directories.AsParallel()
            let files = GetFiles(directory)
            from f in files
            select f;
        }

        private IEnumerable<Photo> GetFilesFromDevice(IEnumerable<string> directories)
        {
            // return from directory in directories.AsParallel()
            // let files = GetFiles(directory)
            // from f in files
            // select f;
            var devices = MediaDevice.GetDevices();
            using (var device = devices.First(d => d.FriendlyName == "Galaxy S21 5G"))
            {
                device.Connect();
                foreach (var dir in directories)
                {
                    var photoDir = device.GetDirectoryInfo(dir);//@"\Phone\DCIM\Camera");
                    var files = photoDir.EnumerateFiles("*.*", SearchOption.TopDirectoryOnly);
                    foreach (var f in files)
                    {
                        yield return new Photo {
                            Directory = new Dir{ FullPath = dir },
                            FullPath = f.FullName,
                            Size = f.Length
                        };
                    }
                }

                device.Disconnect();
            }
        }

        private IEnumerable<Photo> GetFiles(Dir directory)
        {
            var directories = new Queue<Dir>();
            directories.Enqueue(directory);

            while (directories.TryDequeue(out Dir dir))
            {
                Console.WriteLine($"Looking for files in {directory.FullPath}");
                foreach (var d in Directory.GetDirectories(dir.FullPath))
                {
                    directories.Enqueue(new Dir()
                    {
                        FullPath = d,
                    });
                }

                foreach (var file in Directory.GetFiles(dir.FullPath))
                {
                    foreach (var filter in _fileFilters)
                    {
                        if (filter.Include(file))
                        {
                            yield return new Photo
                            {
                                Directory = dir,
                                FullPath = file,
                            };
                        }
                    }
                }
            }
        }

        private IEnumerable<Dir> GetDirectories(params string[] directories)
        {
            return directories.SelectMany(d => GetDirectories(d));
        }

        private IEnumerable<Dir> GetDirectories(string root)
        {
            // Include root itself.
            yield return new Dir { FullPath = root };

            var directories = Directory.GetDirectories(root);
            foreach (var directory in directories)
            {
                // if (directory.StartsWith(root + @"\00", StringComparison.InvariantCultureIgnoreCase) ||
                //     directory.StartsWith(root + @"\20", StringComparison.InvariantCultureIgnoreCase) ||
                //     directory.StartsWith(root + @"\Grow", StringComparison.InvariantCultureIgnoreCase) ||
                //     directory.StartsWith(root + @"\David", StringComparison.InvariantCultureIgnoreCase))
                // {
                    yield return new Dir
                    {
                        FullPath = directory,
                    };
                //}
            }
        }
    }

    public class Dir
    {
        public string FullPath { get; set; }
    }

    public class Photo
    {
        //private FileInfo _info;

        [JsonIgnore]
        public Dir Directory { get; set; }

        public string FullPath { get; set; }

        public ulong Size { get; set; }

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
        public string UniqueId => $"{RelativePath},{Size}".ToLowerInvariant();

        public override string ToString()
        {
            return $"Path={FullPath}, UniqueId={UniqueId}";
        }
    }
}
