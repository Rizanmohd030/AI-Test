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
    [System.Text.Json.Serialization.JsonPropertyName("intent")]
    public string Intent { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("clientId")]
    public int? ClientId { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("searchTerm")]
    public string? SearchTerm { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string? Name { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("email")]
    public string? Email { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("notes")]
    public string? Notes { get; set; }
}

public class ClientPromptResponse
{
    public string Intent { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public ClientResponse? Client { get; set; }
    public List<ClientResponse> Clients { get; set; } = new();
}
