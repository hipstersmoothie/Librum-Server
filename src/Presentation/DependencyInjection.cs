using System.Net;
using System.Text;
using Application.Common.DTOs;
using Application.Interfaces.Managers;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Application.Managers;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repository;
using Application.Services;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using AuthenticationManager = Application.Managers.AuthenticationManager;


namespace Presentation;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IAuthenticationManager, AuthenticationManager>();
        services.AddScoped<IBookService, BookService>();
        services.AddScoped<IBookRepository, BookRepository>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddSingleton<IBookBlobStorageManager, BookBlobStorageManager>();
        services.AddSingleton(x => new BlobServiceClient(
                                  configuration.GetValue<string>(
                                      "AzureBlobStorageConnectionString")));
        services.AddHttpContextAccessor();
        services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
        services.AddScoped<IUrlHelper>(x =>
        {
            var actionContext = x.GetRequiredService<IActionContextAccessor>().ActionContext;
            var factory = x.GetRequiredService<IUrlHelperFactory>();
            return factory.GetUrlHelper(actionContext!);
        });
        
        services.AddLogging();
        services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
        services.AddCustomInvalidModelStateResponseMessage();
        services.AddDbContext<DataContext>(options =>
        {
            var sqliteConnection = configuration["DBConnectionString"];
            if (sqliteConnection == null)
            {
                throw new InvalidDataException("Failed getting the DBConnectionString");
            }
            
            options.UseSqlServer(sqliteConnection);
        });

        return services;
    }

    public static void ConfigureIdentity(this IServiceCollection services)
    {
        var builder = services.AddIdentityCore<User>(opt =>
        {
            opt.Password.RequireDigit = false;
            opt.Password.RequireNonAlphanumeric = false;
            opt.Password.RequiredLength = 4;
            opt.User.RequireUniqueEmail = true;
            opt.SignIn.RequireConfirmedEmail = true;
        });

        builder = new IdentityBuilder(builder.UserType, 
                                      typeof(IdentityRole), 
                                      builder.Services);
        builder.AddEntityFrameworkStores<DataContext>().AddDefaultTokenProviders();
    }

    public static void ConfigureJwt(this IServiceCollection services, 
                                    IConfiguration configuration)
    {
        var secret = configuration["JWTKey"]!;
        if (secret.IsNullOrEmpty())
        {
            throw new InvalidDataException("Failed getting the JWT Key");
        }
        
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        
        services.AddAuthentication(opt =>
        {
            opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(opt =>
        {
            opt.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = configuration["JWTValidIssuer"],
                IssuerSigningKey = signingKey
            };
        });
    }

    /// <summary>
    /// A handler that adds custom error messages to DataAnnotation failures.
    /// The "ErrorMessage" strings passed to DataAnnotations look like:
    /// "6 Some Error Message", where 6 is the code and the rest is the error message.
    /// </summary>
    public static void AddCustomInvalidModelStateResponseMessage(
        this IServiceCollection services)
    {
        services.AddMvcCore().ConfigureApiBehaviorOptions(options => {
            options.InvalidModelStateResponseFactory = (errorContext) =>
            {
                var errorString = errorContext.ModelState.Values.First().Errors.First().ErrorMessage;
                var res = GetCodeAndMessageFromErrorString(errorString);
                var (code, message) = res;
                
                var error = new CommonErrorDto((int)HttpStatusCode.BadRequest,
                                               message,
                                               code);
                return new BadRequestObjectResult(error);
            };
        });
    }

    /// <summary>
    /// Parses the code and the message from strings that look like this:
    /// "6 Some Error Message", where 6 is the code and the rest is the error message.
    /// </summary>
    private static ValueTuple<int, string> GetCodeAndMessageFromErrorString(
        string errorString)
    {
        int endOfDigits = 0;
        foreach(char c in errorString)
        {
            if (char.IsDigit(c))
                endOfDigits++;
            else
                break;
        }

        int code = 0;
        if (endOfDigits > 0)
        {
            var codeAsString = errorString.Substring(0, endOfDigits + 1);
            code = int.Parse(codeAsString);
        }
        var message = errorString[(endOfDigits+1) ..];

        return (code, message);
    }
}