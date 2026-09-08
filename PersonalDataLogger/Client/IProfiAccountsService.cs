using System.Threading.Tasks;

namespace PersonalDataLogger.Client
{
    public interface IProfiAccountsService
    {
        Task<decimal> GetEnabledAccountsBalance();
    }
}