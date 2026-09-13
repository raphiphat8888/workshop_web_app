using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Scalar.AspNetCore;

using TodoApi.Dtos;
using TodoApi.data;
using TodoApi.Model;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Components ??= new Microsoft.OpenApi.OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, Microsoft.OpenApi.IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new Microsoft.OpenApi.OpenApiSecurityScheme
        {
            Type = Microsoft.OpenApi.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\""
        };

        return Task.CompletedTask;
    });
});

builder.Services.AddDbContext<AppdbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")
));

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();



using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppdbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

var todoGroup = app
    .MapGroup("/api/todos")
    .WithTags("Todos")
    .RequireAuthorization().WithTags();

#region  In-memory Endpoints

    
// var todos = new List<TodoGetDto>
// {
//     new(1, "Learn Minimal API", false),
//     new(2, "Learn Vue", false)
// };

// todoGroup.MapGet("/", () =>
//     Results.Ok(todos));

// todoGroup.MapGet("/{id}", (int id) =>
// {
//     var todo = todos.FirstOrDefault(todo => todo.Id == id);

//     return todo is null
//         ? Results.NotFound()
//         : Results.Ok(todo);
// });

// todoGroup.MapPost("/", (TodoPostDto request) =>
// {
//     var nextId = todos.Count == 0 ? 1 : todos.Max(todo => todo.Id) + 1;
//     var todo = new TodoGetDto(nextId, request.Title, request.IsCompleted);

//     todos.Add(todo);

//     return Results.Created($"/api/todos/{todo.Id}", todo);
// });

// todoGroup.MapPut("/{id}", (int id, TodoPutDto request) =>
// {
//     try
//     {
//         var todoIndex = todos.FindIndex(todo => todo.Id == id);

//         if (todoIndex == -1)
//         {
//             return Results.NotFound();
//         }

//         todos[todoIndex] = new TodoGetDto(id, request.Title, request.IsCompleted);

//         return Results.Ok(todos[todoIndex]);
//     }
//     catch
//     {
//         return Results.Problem("Unable to update the todo item.");
//     }
// });

// todoGroup.MapDelete("/{id}", (int id) =>
// {
//     try
//     {
//         var todo = todos.FirstOrDefault(todo => todo.Id == id);

//         if (todo is null)
//         {
//             return Results.NotFound();
//         }

//         todos.Remove(todo);

//         return Results.NoContent();
//     }
//     catch
//     {
//         return Results.Problem("Unable to delete the todo item.");
//     }
// });

    
#endregion

#region Database Enpoints
    
todoGroup.MapGet("/", async (AppdbContext db) =>
{
    try
    {
        var todos = await db.TodoItems.ToListAsync();
        var todoGetDto = todos.Select(t =>     
        new TodoGetDto(t.Id, t.Title, t.IsComplete));
        return todos.Count == 0 ? Results.NotFound() : Results.Ok(todos);
      
    }
    catch
    {
        return Results.Problem("Unable to retrieve todo items.");
    }
})
    .WithName("GetTodos")
    .WithSummary("Get all Todos")
    .Produces<List<TodoGetDto>>();

todoGroup.MapPost("/", async (AppdbContext db, TodoPostDto dto) =>
{
    try
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            return Results.BadRequest(new
            {
                Message = "Title is required."
            });
        }

        var todo = new TodoItem
        {
            Title = dto.Title,
            IsComplete = dto.IsCompleted,
            CreatedAt = DateTime.UtcNow
        };

        db.TodoItems.Add(todo);
        await db.SaveChangesAsync();

        var todoGetDto = new TodoGetDto(todo.Id, todo.Title, todo.IsComplete);

        return Results.Created($"/api/todos/{todo.Id}", todoGetDto);
    }
    catch
    {
        return Results.Problem("Unable to create the todo item.");
    }
});
    
#endregion

#region Authentication Endpoint

app.MapPost("/api/auth/login", (LoginDto login, IConfiguration configuration) =>
{
    if (login.Username != "admin" || login.Password != "password")
    {
        return Results.Unauthorized();
    }

    var claims = new[]
    {
        new Claim(ClaimTypes.Name, login.Username)
    };

    var key = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));

    var credentials = new SigningCredentials(
        key,
        SecurityAlgorithms.HmacSha256);

    var expiration = DateTime.UtcNow.AddHours(1);

    var token = new JwtSecurityToken(
        issuer: configuration["Jwt:Issuer"],
        audience: configuration["Jwt:Audience"],
        claims: claims,
        expires: DateTime.UtcNow.AddDays(1),
        signingCredentials: credentials);

    var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

    return Results.Ok(new LoginResponseDto(tokenString, expiration));
}).WithTags("Authentication").WithName("login").Produces<LoginResponseDto>(StatusCodes.Status200OK).Produces(StatusCodes.Status401Unauthorized);

#endregion

app.Run();