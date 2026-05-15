namespace Backend.Services;

public interface IKeyRotationService
{
    string GetNextGroqKey();
    int GetAvailableKeyCount();
}

public class KeyRotationService : IKeyRotationService
{
    private readonly List<string> _groqApiKeys;
    private int _currentIndex = 0;
    private readonly ILogger<KeyRotationService> _logger;

    public KeyRotationService(ILogger<KeyRotationService> logger)
    {
        _logger = logger;
        _groqApiKeys = LoadApiKeysFromEnvironment();
        
        if (_groqApiKeys.Count == 0)
            throw new InvalidOperationException("No Groq API keys found. Add GROQ_API_KEY_1, GROQ_API_KEY_2, etc. to your .env file");

        _logger.LogInformation("Loaded {KeyCount} unique Groq API keys for rotation", _groqApiKeys.Count);
    }

    public string GetNextGroqKey()
    {
        var key = _groqApiKeys[_currentIndex];
        _currentIndex = (_currentIndex + 1) % _groqApiKeys.Count;
        _logger.LogDebug("Rotated to Groq API key index {Index}. {TotalKeys} keys available", (_currentIndex - 1 + _groqApiKeys.Count) % _groqApiKeys.Count, _groqApiKeys.Count);
        return key;
    }

    public int GetAvailableKeyCount() => _groqApiKeys.Count;

    private List<string> LoadApiKeysFromEnvironment()
    {
        var keys = new List<string>();
        var keySet = new HashSet<string>();

        // Look for GROQ_API_KEY_1, GROQ_API_KEY_2, etc.
        for (int i = 1; i <= 10; i++)
        {
            var key = Environment.GetEnvironmentVariable($"GROQ_API_KEY_{i}");
            if (!string.IsNullOrWhiteSpace(key) && keySet.Add(key))
            {
                keys.Add(key);
            }
        }

        // Also check for single GROQ_API_KEY (fallback)
        if (keys.Count == 0)
        {
            var singleKey = Environment.GetEnvironmentVariable("GROQ_API_KEY");
            if (!string.IsNullOrWhiteSpace(singleKey))
            {
                keys.Add(singleKey);
            }
        }

        return keys;
    }
}
