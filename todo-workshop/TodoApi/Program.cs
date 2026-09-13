using Microsoft.EntityFrameworkCore;
using TodoApi.Dtos;
using TodoApi.data;
using TodoApi.Model;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppdbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")
));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppdbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var todoGroup = app.MapGroup("/api/todos").WithTags("Todos");

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
        return todos.Count == 0 ? Results.NotFound() : Results.Ok(todos);
    }
    catch
    {
        return Results.Problem("Unable to retrieve todo items.");
    }

});

todoGroup.MapPost("/", async (AppdbContext db, TodoPostDto dto) =>
{
    try
    {
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
app.Run();