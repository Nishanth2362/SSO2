var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.SSO_WebApplication>("sso-webapplication");

builder.AddProject<Projects.SSO_InternalApi>("sso-internalapi");

builder.Build().Run();
