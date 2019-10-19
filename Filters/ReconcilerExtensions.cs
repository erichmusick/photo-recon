using PhotoRecon;

namespace photo_recon.Filters
{
    public static class ReconcilerExtensions
    {
        public static Reconciler ExcludeExtensions(this Reconciler recon, params string[] extensions)
        {
            recon.AddFilter(new FileExtensionFilter(extensions));
            return recon;
        }
    }
}
