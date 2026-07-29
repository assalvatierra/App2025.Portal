using App2025.Portal.Models;

namespace Portal.DBServices
{
    public interface ICtaBoxService
    {
        /// <summary>
        /// Retrieves CTA Box configuration from the database
        /// </summary>
        /// <returns>CtaBoxViewModel or null if not found</returns>
        Task<CtaBoxViewModel> GetCtaBoxInfoAsync();
    }
}
