﻿namespace CarvedRock.Api;

public class UseScopeMiddleware
{
    private readonly RequestDelegate _next;

    private readonly ILogger<UseScopeMiddleware> _logger;

    public UseScopeMiddleware(RequestDelegate next, ILogger<UseScopeMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity is {IsAuthenticated:true})
        {
            var user = context.User;
            using (_logger.BeginScope("User: {user}",user.Identity.Name))
            {
                await _next(context);
            }
        }
        else
        {
            await _next(context);
        }
    }
}