using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using project.utils.interfaces;

namespace project.utils.services
{
    public class interceptorDb : SaveChangesInterceptor
    {
        private readonly HttpContext httpContext;
        public interceptorDb(IHttpContextAccessor httpContextAccessor)
        {
            httpContext = httpContextAccessor.HttpContext;
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            addUpdate(eventData);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            addUpdate(eventData);
            return base.SavingChanges(eventData, result);
        }
        private void addUpdate(DbContextEventData eventData)
        {
            string id = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            foreach (var entry in eventData.Context.ChangeTracker.Entries<ICommonModelHeader>())
            {
                if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
                {
                    entry.Entity.userUpdateId = id;
                }
            }
        }

    }
}