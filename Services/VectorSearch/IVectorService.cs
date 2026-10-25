using Portal.Services.VectorSearch.Models;

namespace Portal.Services.VectorSearch
{
    public interface IVectorService
    {
        VectorDbScheduleSettings? GetScheduleSetting();
        int SetScheduleSetting(VectorDbScheduleSettings setting);
    }
}
