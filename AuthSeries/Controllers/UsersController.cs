using AuthSeries.Models;
using AuthSeries.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace AuthSeries.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private readonly ITokenService tokenService;

        public UsersController(IConfiguration configuration, ITokenService tokenService)
        {
            this.configuration = configuration;
            this.tokenService = tokenService;
        }

        [HttpPost]
        [Route("sign-in")]
        public IActionResult Post(UserModel userModel)
        {
            return Ok(tokenService.BuildToken(configuration["Jwt:AuthDemo:Key"], configuration["Jwt:AuthDemo:ValidIssuer"], userModel));
        }
    }
}
