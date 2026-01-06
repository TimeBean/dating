using DatingAPI.Data;
using DatingAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace DatingAPI.Endpoints
{
    public static class UserEndpoints
    {
        public static void MapUserEndpoints(this WebApplication app)
        {
            app.MapGet("/api/users", async (AppDatabaseContext db) =>
                await db.Users.ToListAsync());

            app.MapGet("/api/users/{chatId:long}", async (long chatId, AppDatabaseContext db) =>
            {
                var user = await db.Users.FindAsync(chatId);
                
                return user is not null ? Results.Ok(user) : Results.NotFound();
            });

            app.MapPost("/api/users", async (User user, AppDatabaseContext db) =>
            {
                db.Users.Add(user);
                await db.SaveChangesAsync();
                
                return Results.Created($"/api/users/{user.ChatId}", user);
            });

            app.MapPut("/api/users/{chatId:long}", async (long id, AppDatabaseContext db) =>
            {
                var user = await db.Users.FindAsync(id);
                if (user is null) 
                    return Results.NotFound();

                user.Name = user.Name;
                user.Description = user.Description;
                user.Age = user.Age;
                user.Latitude = user.Latitude;
                user.Longitude = user.Longitude;

                await db.SaveChangesAsync();
                
                return Results.NoContent();
            });

            app.MapDelete("/api/users/{chatId:long}", async (long id, AppDatabaseContext db) =>
            {
                var user = await db.Users.FindAsync(id);
                if (user is null) 
                    return Results.NotFound();

                db.Users.Remove(user);
                await db.SaveChangesAsync();
                
                return Results.NoContent();
            });
            
            app.MapPatch("/api/users/{chatId:long}", async (long chatId, UpdateUser updateUser, AppDatabaseContext db) =>
            {
                var user = await db.Users.FindAsync(chatId);
                if (user is null)
                    return Results.NotFound();

                if (updateUser.Name is not null)
                    user.Name = updateUser.Name;

                if (updateUser.Description is not null)
                    user.Description = updateUser.Description;

                if (updateUser.Age.HasValue)
                    user.Age = updateUser.Age.Value;

                if (updateUser.Latitude.HasValue)
                    user.Latitude = updateUser.Latitude.Value;

                if (updateUser.Longitude.HasValue)
                    user.Longitude = updateUser.Longitude.Value;

                if (updateUser.State.HasValue)
                    user.State = updateUser.State.Value;

                await db.SaveChangesAsync();
                return Results.NoContent();
            });
        }
    }
}