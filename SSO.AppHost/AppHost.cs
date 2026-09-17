var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.SSO_WebApplication>("sso-webapplication");

builder.Build().Run();
