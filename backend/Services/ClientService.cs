using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public interface IClientService
{
    Task<ClientResponse> CreateClientAsync(ClientCreateDto dto);
    Task<ClientResponse?> GetClientByIdAsync(int id);
    Task<ClientResponse?> GetLastClientAsync();
    Task<List<ClientResponse>> GetAllClientsAsync();
    Task<List<ClientResponse>> FindClientsAsync(string searchTerm);
    Task<ClientResponse?> UpdateClientAsync(int id, ClientUpdateDto dto);
    Task<bool> DeleteClientAsync(int id);
    Task<Client> UpsertClientAsync(string name, string? email, string? phone, string? notes);
    Task<ClientPromptResponse> HandlePromptAsync(string prompt);
}

public class ClientService : IClientService
{
    private readonly AppDbContext _db;
    private readonly IGroqService _groqService;
    private readonly ILogger<ClientService> _logger;

    public ClientService(AppDbContext db, IGroqService groqService, ILogger<ClientService> logger)
    {
        _db = db;
        _groqService = groqService;
        _logger = logger;
    }

    public async Task<ClientResponse> CreateClientAsync(ClientCreateDto dto)
    {
        ValidateName(dto.Name);

        var client = new Client
        {
            Name = dto.Name.Trim(),
            Email = Normalize(dto.Email),
            Phone = Normalize(dto.Phone),
            Notes = Normalize(dto.Notes),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Clients.Add(client);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created client {ClientName} (ID: {ClientId})", client.Name, client.Id);
        return Map(client);
    }

    public async Task<ClientResponse?> GetClientByIdAsync(int id)
    {
        var client = await _db.Clients.FirstOrDefaultAsync(c => c.Id == id);
        return client == null ? null : Map(client);
    }

    public async Task<ClientResponse?> GetLastClientAsync()
    {
        var client = await _db.Clients
            .OrderByDescending(c => c.UpdatedAt)
            .FirstOrDefaultAsync();

        return client == null ? null : Map(client);
    }

    public async Task<List<ClientResponse>> GetAllClientsAsync()
    {
        var clients = await _db.Clients
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync();

        return clients.Select(Map).ToList();
    }

    public async Task<List<ClientResponse>> FindClientsAsync(string searchTerm)
    {
        var term = searchTerm.Trim().ToLowerInvariant();

        var clients = await _db.Clients
            .Where(c =>
                c.Name.ToLower().Contains(term) ||
                (c.Email != null && c.Email.ToLower().Contains(term)) ||
                (c.Phone != null && c.Phone.ToLower().Contains(term)))
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync();

        return clients.Select(Map).ToList();
    }

    public async Task<ClientResponse?> UpdateClientAsync(int id, ClientUpdateDto dto)
    {
        var client = await _db.Clients.FirstOrDefaultAsync(c => c.Id == id);
        if (client == null) return null;

        if (!string.IsNullOrWhiteSpace(dto.Name))
            client.Name = dto.Name.Trim();
        if (dto.Email != null)
            client.Email = Normalize(dto.Email);
        if (dto.Phone != null)
            client.Phone = Normalize(dto.Phone);
        if (dto.Notes != null)
            client.Notes = Normalize(dto.Notes);

        client.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Updated client {ClientName} (ID: {ClientId})", client.Name, client.Id);
        return Map(client);
    }

    public async Task<bool> DeleteClientAsync(int id)
    {
        var client = await _db.Clients.FirstOrDefaultAsync(c => c.Id == id);
        if (client == null) return false;

        _db.Clients.Remove(client);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Deleted client ID {ClientId}", id);
        return true;
    }

    public async Task<Client> UpsertClientAsync(string name, string? email, string? phone, string? notes)
    {
        ValidateName(name);

        var normalizedEmail = Normalize(email);
        var normalizedPhone = Normalize(phone);
        var normalizedNotes = Normalize(notes);

        Client? client = null;

        if (!string.IsNullOrWhiteSpace(normalizedEmail))
        {
            client = await _db.Clients.FirstOrDefaultAsync(c => c.Email == normalizedEmail);
        }

        if (client == null && !string.IsNullOrWhiteSpace(normalizedPhone))
        {
            client = await _db.Clients.FirstOrDefaultAsync(c => c.Phone == normalizedPhone);
        }

        if (client == null)
        {
            client = await _db.Clients.FirstOrDefaultAsync(c => c.Name == name.Trim());
        }

        if (client == null)
        {
            client = new Client
            {
                Name = name.Trim(),
                Email = normalizedEmail,
                Phone = normalizedPhone,
                Notes = normalizedNotes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Clients.Add(client);
        }
        else
        {
            client.Name = name.Trim();
            if (normalizedEmail != null) client.Email = normalizedEmail;
            if (normalizedPhone != null) client.Phone = normalizedPhone;
            if (normalizedNotes != null) client.Notes = normalizedNotes;
            client.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return client;
    }

    public async Task<ClientPromptResponse> HandlePromptAsync(string prompt)
    {
        var analysis = await _groqService.InterpretClientCommandAsync(prompt);

        return analysis.Intent switch
        {
            "get_last_client" => await HandleGetLastAsync(),
            "list_clients" => await HandleListAsync(),
            "get_client" => await HandleGetAsync(analysis),
            "create_client" => await HandleCreateAsync(analysis),
            "update_client" => await HandleUpdateAsync(analysis),
            "delete_client" => await HandleDeleteAsync(analysis),
            _ => throw new InvalidOperationException($"Unsupported client intent: {analysis.Intent}")
        };
    }

    private async Task<ClientPromptResponse> HandleGetLastAsync()
    {
        var client = await GetLastClientAsync();
        return new ClientPromptResponse
        {
            Intent = "get_last_client",
            Message = client == null ? "No clients found." : "Latest client retrieved.",
            Client = client
        };
    }

    private async Task<ClientPromptResponse> HandleListAsync()
    {
        var clients = await GetAllClientsAsync();
        return new ClientPromptResponse
        {
            Intent = "list_clients",
            Message = clients.Count == 0 ? "No clients found." : $"Found {clients.Count} clients.",
            Clients = clients
        };
    }

    private async Task<ClientPromptResponse> HandleGetAsync(ClientCommandAnalysis analysis)
    {
        if (analysis.ClientId.HasValue)
        {
            var client = await GetClientByIdAsync(analysis.ClientId.Value);
            return new ClientPromptResponse
            {
                Intent = "get_client",
                Message = client == null ? $"Client {analysis.ClientId.Value} not found." : "Client retrieved.",
                Client = client
            };
        }

        var searchTerm = GetSearchTerm(analysis);
        if (string.IsNullOrWhiteSpace(searchTerm))
            throw new InvalidOperationException("Client search term is required.");

        var clients = await FindClientsAsync(searchTerm);
        return new ClientPromptResponse
        {
            Intent = "get_client",
            Message = clients.Count == 0 ? $"No clients found for '{searchTerm}'." : $"Found {clients.Count} matching clients.",
            Client = clients.FirstOrDefault(),
            Clients = clients
        };
    }

    private async Task<ClientPromptResponse> HandleCreateAsync(ClientCommandAnalysis analysis)
    {
        if (string.IsNullOrWhiteSpace(analysis.Name))
            throw new InvalidOperationException("Client name is required to create a client.");

        var client = await CreateClientAsync(new ClientCreateDto
        {
            Name = analysis.Name,
            Email = analysis.Email,
            Phone = analysis.Phone,
            Notes = analysis.Notes
        });

        return new ClientPromptResponse
        {
            Intent = "create_client",
            Message = "Client created.",
            Client = client
        };
    }

    private async Task<ClientPromptResponse> HandleUpdateAsync(ClientCommandAnalysis analysis)
    {
        var target = await ResolveSingleClientAsync(analysis);
        if (target == null)
            throw new InvalidOperationException("Client not found for update.");

        var updated = await UpdateClientAsync(target.Id, new ClientUpdateDto
        {
            Name = analysis.Name,
            Email = analysis.Email,
            Phone = analysis.Phone,
            Notes = analysis.Notes
        });

        return new ClientPromptResponse
        {
            Intent = "update_client",
            Message = "Client updated.",
            Client = updated
        };
    }

    private async Task<ClientPromptResponse> HandleDeleteAsync(ClientCommandAnalysis analysis)
    {
        var target = await ResolveSingleClientAsync(analysis);
        if (target == null)
            throw new InvalidOperationException("Client not found for deletion.");

        var deleted = await DeleteClientAsync(target.Id);
        return new ClientPromptResponse
        {
            Intent = "delete_client",
            Message = deleted ? "Client deleted." : "Client not found.",
            Client = target
        };
    }

    private async Task<Client?> ResolveSingleClientAsync(ClientCommandAnalysis analysis)
    {
        if (analysis.ClientId.HasValue)
            return await _db.Clients.FirstOrDefaultAsync(c => c.Id == analysis.ClientId.Value);

        var searchTerm = GetSearchTerm(analysis);
        if (string.IsNullOrWhiteSpace(searchTerm))
            return null;

        var term = searchTerm.Trim().ToLowerInvariant();
        return await _db.Clients
            .Where(c =>
                c.Name.ToLower().Contains(term) ||
                (c.Email != null && c.Email.ToLower().Contains(term)) ||
                (c.Phone != null && c.Phone.ToLower().Contains(term)))
            .OrderByDescending(c => c.UpdatedAt)
            .FirstOrDefaultAsync();
    }

    private static string? GetSearchTerm(ClientCommandAnalysis analysis) =>
        !string.IsNullOrWhiteSpace(analysis.SearchTerm) ? analysis.SearchTerm :
        !string.IsNullOrWhiteSpace(analysis.Name) ? analysis.Name :
        !string.IsNullOrWhiteSpace(analysis.Email) ? analysis.Email :
        analysis.Phone;

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Client name is required.");
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ClientResponse Map(Client client) => new()
    {
        Id = client.Id,
        Name = client.Name,
        Email = client.Email,
        Phone = client.Phone,
        Notes = client.Notes,
        CreatedAt = client.CreatedAt,
        UpdatedAt = client.UpdatedAt
    };
}
