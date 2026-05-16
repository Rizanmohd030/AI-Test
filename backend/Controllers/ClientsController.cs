using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientsController : ControllerBase
{
    private readonly IClientService _clientService;
    private readonly ILogger<ClientsController> _logger;

    public ClientsController(IClientService clientService, ILogger<ClientsController> logger)
    {
        _clientService = clientService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var clients = await _clientService.GetAllClientsAsync();
        return Ok(clients);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var client = await _clientService.GetClientByIdAsync(id);
        return client == null
            ? NotFound(new { error = $"Client with ID {id} not found." })
            : Ok(client);
    }

    [HttpGet("last")]
    public async Task<IActionResult> GetLast()
    {
        var client = await _clientService.GetLastClientAsync();
        return client == null
            ? NotFound(new { error = "No clients found." })
            : Ok(client);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ClientCreateDto dto)
    {
        try
        {
            var client = await _clientService.CreateClientAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = client.Id }, client);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] ClientUpdateDto dto)
    {
        var client = await _clientService.UpdateClientAsync(id, dto);
        return client == null
            ? NotFound(new { error = $"Client with ID {id} not found." })
            : Ok(client);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _clientService.DeleteClientAsync(id);
        return deleted
            ? NoContent()
            : NotFound(new { error = $"Client with ID {id} not found." });
    }

    [HttpPost("prompt")]
    public async Task<IActionResult> Prompt([FromBody] ClientPromptRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
            return BadRequest(new { error = "Prompt is required." });

        try
        {
            var result = await _clientService.HandlePromptAsync(request.Prompt);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Client prompt failed for prompt: {Prompt}", request.Prompt);
            return StatusCode(500, new { error = "Client prompt failed.", details = ex.Message });
        }
    }
}
