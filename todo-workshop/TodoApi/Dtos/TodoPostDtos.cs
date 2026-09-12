namespace TodoApi.Dtos;

public record TodoPostDto
(
    string Title,
    bool IsCompleted
);