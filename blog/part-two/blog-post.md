# Custom Jwt Authentication .NET 5

## dotnet, jwt, authentication, webapi, vscode

Hi guys! Welcome back to my Authentication series. It's a pleasure for me to have you here ☺.  
🔴IMPORTANT❗🔴: this is the second post of a series so, if you missed the previous one, I strongly suggest you to check it out from the link above.  
As always, if u get in trouble in following this tutorial u can check the final solution in GitHub at this [link](https://github.com/ivan-pesenti/auth-series).

## Quick recap of the previous episode
In the previous blog post we achieved what follows:
1. Develop our web api with VSCode and test it through Postman
1. Use Firebase Admin Sdk to manage our Firebase proj directly from the web api
1. Use token-based authentication in our web api with the format of JWT
1. Set up logic to verify the JWT token validity

## What's next
In this episode we're going to write an authentication endpoint which will be responsible for issuing JWT token to the users that will provide valid sign-in credentials.  
This means that our web api is in charge of issuing the JWT token and verifying it.  
As before, if a user doesn't receive a valid JWT token, he can't access our restricted resource (e.g. our WeatherForecast endpoint).

## Preamble
This will be a demo application to show off a way to implement authentication in .NET. U can take away these concepts and adapt them to your real-world requirements.  
🔵IMPORTANT🔵: I'm not going to follow every best practices to save time but I'll do my best to spot things that are not "real-world ready" to use.  
👀*NOTE*👀: if you only care about this particular topic you can follow directly this blog post without having to deal with the previous one.

## Let's start 🚀
Without much delay, let's jump into the coding part 💻

### Prerequisites
To follow this tutorial u must install on your machine some tools and programs:
1. NET 5 Runtime. U can download from [here](https://dotnet.microsoft.com/download/dotnet/5.0)
1. Visual Studio Code (u can use another IDE if u wish). Download can be found [here](https://code.visualstudio.com/download)
1. Postman (u can use another program to consume REST-api if u wish). Download can be found [here](https://www.postman.com/downloads/)
1. C# extension for VSCode (powered by Omnisharp)

## Prepare configs
The first thing we've to do is to add keys about JWT authentication in our web api. Please be sure to add the following code in appsettings.json:  
```json
"AuthDemo": {
    "Key": "This is where you should specify your secret key, which is used to sign and verify Jwt tokens.",
    "ValidIssuer": "localhost:5001",
    "ValidAudience": "localhost:5001"
}
```
This section must be inside a parent node called "Jwt".  
The only difference here compared to Firebase is the "Key" setting that now we must specify in order to sign the JWT token when we're going to issue it.  
⚠️WARNING⚠️: in a real-world app you must not store these sensitive data in a non-secure location such as appsettings.json.

## User model
Create a "Models" folder within your web api proj. After that create a class called "UserModel" with the following code: 
```csharp
using System.ComponentModel.DataAnnotations;

namespace AuthSeries.Models
{
    public class UserModel
    {
        [Required]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
    }
}
```
This class will hold the sign-in credentials of the users that will be passed to our authentication endpoint.  
🔎HINT🔎: we've used a "Required" data annotation that will enforce the properties to be present in the input model. 

## Implement token service feature
Create a "Services" folder. In this folder you're going to create two files.

### ITokenService.cs
```csharp
using AuthSeries.Models;

namespace AuthSeries.Services
{
    public interface ITokenService
    {
        string BuildToken(string key, string issuer, UserModel userModel);
    }
}
```
This interface will be implemented by our concrete implementation that we're going to use in our controller to issue a JWT token.
### TokenService.cs
```csharp
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthSeries.Models;
using Microsoft.IdentityModel.Tokens;

namespace AuthSeries.Services
{
    public class TokenService : ITokenService
    {
        private const double EXP_DURATION_MINUTES = 30;

        public string BuildToken(string key, string issuer, UserModel userModel)
        {
            // TODO: put real-world logic to validate sign-in credetials
            // ...
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, userModel.Email),
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
            };

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);
            var tokenDescriptor = new JwtSecurityToken(issuer: issuer, audience: issuer, claims, expires: DateTime.Now.AddMinutes(EXP_DURATION_MINUTES), signingCredentials: credentials);
            return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        }
    }
}
```
This class is in charge of issuing a JWT token by taking in a secret key, a valid issuer and the user's sign-in credentials.
⚠️WARNING⚠️: this code is not real-world as we must check the sign-in credentials against a valid source such as a database, a txt file, a call to an external system and so on.

## Users endpoint
Now, it's time to take advantage of what we've done up to now. We create the endpoint that will be called from the client to get back a valid JWT token issued with the logic above.  
In the "Controllers" folder create a file called "UsersController.cs" with the following code:
```csharp
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
```
This class simply have a constructor with two dependencies (more on "ITokenService" soon) and expose a single action that will return a valid JWT token to a user.  
🧐NOTE🧐: you should not read the settings in this way as it's not strongly-typed and so it's a more error-prone method. Consider using the Options pattern. More on this [here](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-5.0).

## Connect the dots
One of the major changes that we must do is located inside the "Startup.cs" in the "ConfigureServices()" method. The method will look like this:
```csharp
public void ConfigureServices(IServiceCollection services)
{

    services.AddControllers();
    services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "AuthSeries", Version = "v1" });
    });
    
    // Auth.Demo section

    // here we register our service
    services.AddTransient<ITokenService, TokenService>();

    // here we specify our authentication settings to validate the JWT token
    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = Configuration["Jwt:AuthDemo:ValidIssuer"],
            ValidAudience = Configuration["Jwt:AuthDemo:ValidAudience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Jwt:AuthDemo:Key"]))
        };
    });
}
```
Here there are two important thing that you must care about: 
1. The service registration. We've registered our TokenService in the IOC container provided by .NET. The service lifetime used is **transient** that is every time we need of an instance of ITokenService, a fresh new one is returned to us by the built-in IOC container.  
More about the service lifetime [here](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-5.0#service-lifetimes).
1. The JWT token parameters used to verify a token.  
🔎_NOTE_🔎: this code is not real-world ready for a couple of reasons. First, the secret key must not be saved in appsettings.json and must not be a tense so easy to guess. Last, but not least, you should not read the keys in this way but it would be better if you take advantage of Options pattern for example.  

If you started directly from this post you must add the following two Nuget packages by issuing these commands (⚠️WARNING⚠️: be sure to run these commands inside the proj folder):
1. `dotnet add package Microsoft.AspNetCore.Authentication`
1. `dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer`

## Final touch
If you've started from this post you must do two brief final steps in "WeatherForecastController.cs": 
1. Decorate the "Get()" method with this attribute `[Authorize]`
1. Add this using statement: `using Microsoft.AspNetCore.Authorization;`

## Moment of truth
Now the last thing left is to check our work.  
Issue a `dotnet run` in your preferred terminal and wait for the application to start.  
Bring up Postman and try a simple request at the endpoint https://localhost:5001/weatherforecast. This should give you a 401 unauthorized error.  
As expected, we must sign in ourself before try to get a protected resource. Create a new Postman request with the following parameters:
1. type: POST
1. url: https://localhost:5001/users/sign-in
1. body:
```json
{
    "email": "test@test.com",
    "password": "password"
}       
```
When you run this request you get back a JWT token in the output console. Copy it to your clipboard and switch back to the WeatherForecast request.  
In the Authorization tab select:
1. Type: Bearer Token
1. Token: the token in your clipboard

Finally repeat the test again... et voilà 😍. You should get back a 200 OK response along with the requested data.

## Let's recap
Congrats 🏆❗ You successfully did this blog post. By following this tutorial you're able to issue your own JWT token with your custom logic. 

## The final step 🐱‍🏍
In the next blog post (that will be the last of this series), we're going to merge these two posts by allowing the users to authenticate either with Firebase or with our custom endpoint. Stay tuned!

I hope you enjoy this post and find it useful. If you have any questions or you want to spot me some errors I really appreciate it and I'll make my best to follow up. If you enjoy it and would like to sustain me consider giving a like and sharing on your favorite socials. If u want u can add me on your socials this makes me very very happy!

Stay safe and see you soon! 😎
