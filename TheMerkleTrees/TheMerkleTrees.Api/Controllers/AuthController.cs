using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using TheMerkleTrees.Domain.Interfaces.Repositories;
using TheMerkleTrees.Domain.Models;
using Microsoft.Extensions.Logging;
using Prometheus;
using TheMerkleTrees.Api.Services;

namespace TheMerkleTrees.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;
        private readonly JwtSettings _jwtSettings;
        private readonly IMasterKeyService _masterKeyService;
        private readonly string _masterKeySalt;
        private readonly int _keyDerivationIterations;
        
        private static readonly Counter RegistrationCounter = Metrics
            .CreateCounter("auth_registrations_total", "Total number of successful registrations.");

        public AuthController(
            IUserRepository userRepository,
            IConfiguration configuration,
            ILogger<AuthController> logger,
            IMasterKeyService masterKeyService)
        {
            _userRepository = userRepository;
            _configuration = configuration;
            _logger = logger;
            _masterKeyService = masterKeyService;
            
            _jwtSettings = new JwtSettings
            {
                Key = Environment.GetEnvironmentVariable("JWT_KEY") ?? configuration["Jwt:Key"],
                Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? configuration["Jwt:Issuer"],
                Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? configuration["Jwt:Audience"],
                ExpirationMinutes = int.TryParse(configuration["Jwt:ExpirationMinutes"], out var exp) ? exp : 120
            };
            
            // Récupérer les paramètres de dérivation de clé
            _masterKeySalt = Environment.GetEnvironmentVariable("MASTER_KEY_SALT") ?? 
                             configuration["Crypto:MasterKeySalt"] ?? 
                             "TheMerkleTreesDefaultSalt";
                             
            _keyDerivationIterations = int.TryParse(
                Environment.GetEnvironmentVariable("KEY_DERIVATION_ITERATIONS") ?? 
                configuration["Crypto:KeyDerivationIterations"], 
                out var iterations) ? iterations : 10000;

            try
            {
                ValidateJwtSettings();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la validation des paramètres JWT");
                // Ne pas lever d'exception ici, permettre à l'application de démarrer
                // Les endpoints vérifieront les paramètres avant utilisation
            }
        }

        private void ValidateJwtSettings()
        {
            if (string.IsNullOrEmpty(_jwtSettings.Key) || _jwtSettings.Key.Length < 32)
                _logger.LogWarning("JWT Key doit contenir au moins 32 caractères");

            if (string.IsNullOrEmpty(_jwtSettings.Issuer))
                _logger.LogWarning("JWT Issuer est requis");

            if (string.IsNullOrEmpty(_jwtSettings.Audience))
                _logger.LogWarning("JWT Audience est requis");
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] Authentication newUser)
        {
            try
            {
                _logger.LogInformation("Register endpoint called for email: {Email}", newUser.Email);

                if (string.IsNullOrEmpty(newUser.Email) || string.IsNullOrEmpty(newUser.Password))
                {
                    return BadRequest(new { message = "Email et mot de passe sont requis" });
                }

                var existingUser = await _userRepository.GetUserByEmailAsync(newUser.Email);
                if (existingUser != null)
                {
                    _logger.LogWarning("Registration failed - Email already in use: {Email}", newUser.Email);
                    return BadRequest(new { message = "Email already in use" });
                }

                var user = new User
                {
                    Email = newUser.Email,
                    PasswordHash = HashPassword(newUser.Password)
                };

                await _userRepository.CreateUserAsync(user);
                _logger.LogInformation("User registered successfully: {Email}", newUser.Email);
                
                RegistrationCounter.Inc();
                
                return Ok(new { message = "User registered successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed for email: {Email}", newUser.Email);
                return StatusCode(500, new { message = "Une erreur est survenue lors de l'inscription", error = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] Authentication currentUser)
        {
            try
            {
                _logger.LogInformation("Login attempt for email: {Email}", currentUser.Email);

                if (string.IsNullOrEmpty(currentUser.Email) || string.IsNullOrEmpty(currentUser.Password))
                {
                    return BadRequest(new { message = "Email et mot de passe sont requis" });
                }

                var user = await _userRepository.GetUserByEmailAsync(currentUser.Email);
                if (user == null || !VerifyPassword(currentUser.Password, user.PasswordHash))
                {
                    _logger.LogWarning("Invalid login attempt for email: {Email}", currentUser.Email);
                    return Unauthorized(new { message = "Invalid email or password" });
                }

                // Vérifier les paramètres JWT avant de générer le token
                if (string.IsNullOrEmpty(_jwtSettings.Key) || _jwtSettings.Key.Length < 32)
                {
                    _logger.LogError("JWT Key non configurée ou trop courte");
                    return StatusCode(500, new { message = "Erreur de configuration du serveur" });
                }

                // Générer la clé maîtresse à partir du mot de passe
                byte[] masterKey = CryptoUtils.DeriveKeyFromPassword(
                    currentUser.Password, 
                    currentUser.Email, 
                    _masterKeySalt, 
                    _keyDerivationIterations);
                
                // Générer le token JWT
                var token = GenerateJwtToken(user);
                
                // Calculer la date d'expiration
                var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);
                
                // Stocker la clé maîtresse en mémoire
                _masterKeyService.StoreKey(user.Email, masterKey, expiresAt);
                
                _logger.LogInformation("Successful login for email: {Email}, master key stored", currentUser.Email);
                
                return Ok(new { 
                    access_token = token,
                    expires_in = _jwtSettings.ExpirationMinutes * 60
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for email: {Email}", currentUser.Email);
                return StatusCode(500, new { message = "Une erreur est survenue lors de la connexion", error = ex.Message });
            }
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        private bool VerifyPassword(string password, string hashedPassword)
        {
            var hash = HashPassword(password);
            return hash == hashedPassword;
        }

        private string GenerateJwtToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private class JwtSettings
        {
            public string Key { get; set; } = string.Empty;
            public string Issuer { get; set; } = string.Empty;
            public string Audience { get; set; } = string.Empty;
            public int ExpirationMinutes { get; set; }
        }
    }
}
