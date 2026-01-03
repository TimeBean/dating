using DatingAPI.Data;
using DatingContracts;
using Microsoft.EntityFrameworkCore;

namespace DatingAPI.Endpoints
{
    public static class UserEndpoints
    {
        public static void MapUserEndpoints(this WebApplication app)
        {
            app.MapGet("/api/users", async (AppDatabaseContext db) =>
                await db.Users.ToListAsync());

            app.MapGet("/api/users/{id:int}", async (int id, AppDatabaseContext db) =>
            {
                var user = await db.Users.FindAsync(id);
                
                return user is not null ? Results.Ok(user) : Results.NotFound();
            });

            app.MapPost("/api/users", async (UserDto userDto, AppDatabaseContext db) =>
            {
                db.Users.Add(userDto);
                await db.SaveChangesAsync();
                
                return Results.Created($"/api/users/{userDto.Id}", userDto);
            });

            app.MapPut("/api/users/{id:int}", async (int id, UserDto updatedUserDto, AppDatabaseContext db) =>
            {
                var user = await db.Users.FindAsync(id);
                if (user is null) 
                    return Results.NotFound();

                user.Name = updatedUserDto.Name;
                user.Description = updatedUserDto.Description;
                user.Age = updatedUserDto.Age;
                user.Latitude = updatedUserDto.Latitude;
                user.Longitude = updatedUserDto.Longitude;

                await db.SaveChangesAsync();
                
                return Results.NoContent();
            });

            app.MapDelete("/api/users/{id:int}", async (int id, AppDatabaseContext db) =>
            {
                var user = await db.Users.FindAsync(id);
                if (user is null) 
                    return Results.NotFound();

                db.Users.Remove(user);
                await db.SaveChangesAsync();
                
                return Results.NoContent();
            });
        }
    }
}