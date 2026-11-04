// the JWTBearDefaults and the AddJWTBearer
using Microsoft.AspNetCore.Authentication.BearerToken;
// TokenValidationParameters and SymmetricSecurityKey
using Microsoft.IdentityModel.Tokens;
// encoding for signature key, to turn the signing key into bytes
using System.Text;

using Day21Demo.Server.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();

// make JWT bearer the default scheme for every token
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Issuer: who created the token
            ValidateIssuer = true,
            ValidIssuer = "https://demo-issuer.example.com",
            // Audience: was the token meant for this server
            ValidateAudience = true,
            ValidAudience = "grpc-demo-api",
            // Lifetime: reject expired token
            ValidateLifetime = true,
            // No grade period: expired token rejected immediately
            // default is 5 mins
            ClockSkew = TimeSpan.Zero,
            // Signature: was the token signed with this key?
            ValidateIssuerSigningKey = true,
            // same key that signed the token
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("ThisIsADemoSigningKeyForDay21AtLeast32Chars!")),
        };
    });
// Register this authorization; named policies chain onto it

builder.Services.AddAuthorizationBuilder()
    // add a named policy that [Authorize(Policy = ...)] refers to
    .AddPolicy("AdminOnly", policy =>
    policy.RequireClaim("role", "admin"));

var app = builder.Build();

// read the token and set the user first
app.UseAuthentication();
// Then check [Authorized] attribute against user
app.UseAuthorization();

app.MapGrpcService<ProductCatalogService>();

app.Run();
