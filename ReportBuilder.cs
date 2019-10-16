using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace PhotoRecon
{
    public class ReportBuilder
    {
        private readonly Report _report;

        public ReportBuilder()
        {
            _report = new Report();
        }

        public void AddDuplicateFile(LocationType location, Photo photo, Photo other)
        {
            _report.Duplicates.Add(new Duplicate(location, photo, other));
        }

        public void AddMissingFile(LocationType location, Photo photo)
        {
            _report.Missing[location].Add(photo);
        }

        public void SaveReport(string path)
        {
            var reportString = JsonConvert.SerializeObject(_report);
            File.WriteAllText(path, reportString);
        }

        public class Report
        {
            public Report()
            {
                Duplicates = new List<Duplicate>();
                Missing = new Dictionary<LocationType, List<Photo>>();

                foreach (LocationType x in Enum.GetValues(typeof(LocationType)))
                {
                    Missing[x] = new List<Photo>();
                }
            }

            public List<Duplicate> Duplicates { get; }

            public Dictionary<LocationType, List<Photo>> Missing { get; }
        }
    }

    public struct Duplicate
    {
        public LocationType location;

        // TODO: Treat dupes as a set and group them all together.
        public Photo photo1;
        public Photo photo2;

        public Duplicate(LocationType location, Photo photo1, Photo photo2)
        {
            this.location = location;
            this.photo1 = photo1;
            this.photo2 = photo2;
        }
    }
}
