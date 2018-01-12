using System;
using System.Threading.Tasks;
using Kroeg.EntityStore.Services;
using Microsoft.AspNetCore.Http;

namespace Kroeg.Server.Middleware
{
    public class EntityStorePrimer
    {
        private readonly RequestDelegate _next;

        public EntityStorePrimer(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, ServerConfig entityData)
        {
            if (entityData.RewriteRequestScheme) context.Request.Scheme = "https";

            var fullpath = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}";
            await entityData.Prepare(new Uri(fullpath));

            await _next.Invoke(context);
        }
    }
}