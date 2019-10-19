using PhotoRecon;

namespace photo_recon.Filters
{
    public interface IFileFilter
    {
        bool Include(string filePath);
    }
}
