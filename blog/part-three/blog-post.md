# Support two authentication providers with .NET 5

## dotnet, firebase, jwt, vscode

Hi folks 👋🏻! I'm super happy to have you here for the last episode of the series about **Authentication** in .NET 5.  
This post will be our icing on the cake 🍰.  
**🔴IMPORTANT❗🔴**: this post heavily relies on stuff made in the previous two episodes. So I strongly encourage you to checkout the links above and then jump back here again.  
As always, if u get in trouble in following this tutorial u can check the final solution in GitHub at this [link](https://github.com/ivan-pesenti/auth-series).  
## Rewind the tape 🔙
Up to now, we have built out what follows:
1. A Google Firebase proj (create with Google Developer console) with a test user who can sign-in with username and password. You can open the Google console with this [link](https://console.firebase.google.com/)
1. Built a web api proj that makes use of token-based authentication with Jwt format
1. Connected our web api with the Firebase proj so users can access protected resources (after a successful sign-in on Firebase)
1. Implemented our custom sign-in mechanism by exposing an endpoint to issue a Jwt token to users  

👀*NOTE*👀: if you've not followed the previous posts but you have a similar solution to the one presented here (maybe with different auth providers) the following approach still works for you even if you probably have to make some adjustments.  
## Final challenge 🎯
During this post we're going to change our web api in order to support two authentication providers at the same time. What this means is that a user _can choice_ how to **sign-in** against our web api. The user could authenticates himself against Google Firebase or against our custom endpoint. If the sign-in phase is successful the user will got back a valid Jwt token which can be used to access our protected resources. As before, if the sign-in is not successful he'll got back an error response and cannot access our super-secure resources 😎.
## Requirements
To follow this post on your machine you must have installed the following:
1. NET 5 Runtime. You can download from [here](https://dotnet.microsoft.com/download/dotnet/5.0)
1. Visual Studio Code (you can use another IDE if you wish). Download can be found [here](https://code.visualstudio.com/download)
1. Postman (you can use another program to consume REST-api if you wish). Download can be found [here](https://www.postman.com/downloads/)
1. C# extension for VSCode (powered by Omnisharp)  


## Let's start 🚀
To complete the task we've to carry about two sections: the settings and authentication's registration. After these tasks we'll do the final test.  
## Check settings 🔧
First open up the "appsettings.json" file and check that you have an identical structure like me (obviously replace the value with yours):  
```json
"Jwt": {
    "Firebase": {
      "ValidIssuer": "https://securetoken.google.com/auth-series",
      "ValidAudience": "auth-series"
    },
    "AuthDemo": {
      "Key": "This is where you should specify your secret key, which is used to sign and verify Jwt tokens.",
      "ValidIssuer": "localhost:5001",
      "ValidAudience": "localhost:5001"
    }
  }
```
**⚠️WARNING⚠️**: in a real-world app you must not store these **sensitive data** in a non-secure location such as appsettings.json. Moreover the key should not be so easy to guess 😋.  
Here you see that we're going to support two authentication providers: "Firebase" and "AuthDemo" (that is our custom endpoint).

## Providers Registration 🎙
The next file you have to open is "Startup.cs" and look at the ConfigureServices() method.  
After the Firebase registration you have to adjust the code in a way that looks similar to the one below:  
```csharp
    ...
    services.AddTransient<ITokenService, TokenService>();

    // firebase auth
    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer("Firebase", opt =>
    {
        opt.Authority = Configuration["Jwt:Firebase:ValidIssuer"];
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = Configuration["Jwt:Firebase:ValidIssuer"],
            ValidAudience = Configuration["Jwt:Firebase:ValidAudience"]
        };
    });

    // auth demo
    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer("AuthDemo", opt =>
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
```
The main change in this code is that we're going to use another overload of the method _AddJwtBearer_. This overload accepts as the first parameter the **authentication schema name** which is used to identify the providers uniquely.  
🧐 _NOTE_ 🧐: you should not read the settings in this way as it's not **strongly-typed** and so it's a more **error-prone** method. Consider using the **Options pattern**. More on this [here](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-5.0).
## Add policy to manage multiple schemas 📜
The last step left is to add a policy to our web api that allow it to _manage_ multiple authentication schemas at the same time. This policy must be written right below the code of the previous section. So open up "Startup.cs" and locate the "ConfigureServices()" method and write the code:  
```csharp
services.AddAuthorization(opt =>
{
    opt.DefaultPolicy = new AuthorizationPolicyBuilder()
    .AddAuthenticationSchemes("Firebase", "AuthDemo")
    .RequireAuthenticatedUser()
    .Build();
});
```
This code snippet is self-explanatory. Its purposes are to register the schemas declared above as you should notice from the command `.AddAuthenticationSchemes("Firebase", "AuthDemo")` and to require the users to be authenticated in order to access our resources.
## Final Test 🐲
We've finished the coding phase so we can finally give a try to our work ⚒. In your terminal go into the folder where it's contained the .csproj file.  
Issue a `dotnet run` command and wait for the web api to start properly.
### Postman
To test our software we need of three requests (that you can build by following the recipes from the previous posts). Below you can find the three requests with their titles and purposes: 
1. FirebaseSignIn: used to sign-in a user in Firebase platform
1. AuthDemoSignIn: used to sign-in a user against our custom endpoint
1. WeatherForecast: used to access our restricted resource  

Foremost, try to execute the **WeatherForecast** without authentication configured:  

<p align="center">
    <img src="https://github.com/ivan-pesenti/auth-series/blob/main/blog/part-three/img/weather-forecast-no-auth.png?raw=true" alt="Postman request without authentication" width="650px">
</p>

You should get back a **401 Unauthorized** error.  
Now execute the **FirebaseSignIn** request and copy the "idToken" value returned. Then switch back to the WeatherForecast request and change the authorization type to "Bearer Token" and paste in the token as you can see below:

<p align="center">
    <img src="https://github.com/ivan-pesenti/auth-series/blob/main/blog/part-three/img/weather-forecast-with-auth.png?raw=true" alt="Postman request with Bearer authentication" width="650px">
</p>

Execute again this request and check if you receive a **200 OK** response together with the requested data.  
Now open the **AuthDemoSignIn** request and execute it to get back the Jwt token. Copy the returned token.
Switch back to **WeatherForecast** request and replace the Firebase token with the latter one. Execute again and now it should works as expected 🌟!

## Final thoughts 💭
Now we reached the end 🏁. During this series you learned a bunch of things about **authentication** 🔐 within .NET 5. To show off these capabilities we make use of web api project template but the same still applies to other projs such as MVC or Single Page Application.  
Supporting multiple authentication providers could be a _transient_ phase such as when you're migrating from one auth provider to another or _permanent_ as you would like to let users decide which authentication provider utilize.  
In both cases this series will provide you some guidelines about the steps you have to follow.  

## Greetings 👋🏻
Now, it's time to say goodbye 😄.  
**🔵IMPORTANT🔵**: remember that authentication is one **CRUCIAL** aspect of software development, so don't joke with it as you'll pay the consequences for sure 🤕.  
I hope you enjoy this post and find it useful. If you have any questions or you want to spot me some errors I really appreciate it and I'll make my best to follow up. If you enjoy it and would like to sustain me consider giving a like and sharing on your favorite socials. If u want u can add me on your socials this makes me very very happy!

Stay safe and see you soon! 😎
