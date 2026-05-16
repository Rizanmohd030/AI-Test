namespace Backend.DTOs;

public class ClientCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Notes { get; set; }
}

public class ClientUpdateDto
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Notes { get; set; }
}

public class ClientResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ClientPromptRequest
{
    public string Prompt { get; set; } = string.Empty;
}

public class ClientCommandAnalysis
{
    public string Intent { get; set; } = string.Empty;
    public int? ClientId { get; set; }
    public string? SearchTerm { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Notes { get; set; }
}

public class ClientPromptResponse
{
    public string Intent { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public ClientResponse? Client { get; set; }
    public List<ClientResponse> Clients { get; set; } = new();
}
