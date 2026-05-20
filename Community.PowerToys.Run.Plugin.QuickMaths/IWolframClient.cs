using System;
using System.Threading;
using System.Threading.Tasks;

namespace Community.PowerToys.Run.Plugin.QuickMaths
{
    public interface IWolframClient : IDisposable
    {
        void UpdateAppId(string appId);
        Task<string> QueryAsync(string query, CancellationToken token);
    }
}
