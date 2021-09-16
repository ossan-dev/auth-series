using AuthSeries.Models;

namespace AuthSeries.Services
{
    public interface ITokenService
    {
        string BuildToken(string key, string issuer, UserModel userModel);
    }
}
