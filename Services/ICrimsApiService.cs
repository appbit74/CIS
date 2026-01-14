using System.Collections.Generic;
using System.Threading.Tasks;

namespace CIS.Services
{
    public interface ICrimsApiService
    {
        Task<Dictionary<string, string>> GetSectionsAsync();
        Task<Dictionary<string, string>> GetPositionsAsync();
    }
}