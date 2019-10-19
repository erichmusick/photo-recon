using System.Collections.Generic;
using System.IO;
using PhotoRecon;

namespace photo_recon.Filters
{
    public class FileExtensionFilter : IFileFilter
    {
        private readonly HashSet<string> _excludedExtensions;

        public FileExtensionFilter(params string[] exclude)
        {
            _excludedExtensions = new HashSet<string>(exclude);
        }

        public bool Include(string path)
        {
            var extension = Path.GetExtension(path);
            return !_excludedExtensions.Contains(extension);
        }
    }
}
