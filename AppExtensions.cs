using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Indusoft.CalendarPlanning.Common
{
    public static class AppExtensions
    {
        private static void PerformMigrations<T>(this IApplicationBuilder app) where T : DbContext
        {
            using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                var ctx = serviceScope.ServiceProvider.GetRequiredService<T>();
                ctx.Database.Migrate();
            }
        }
    }
}
