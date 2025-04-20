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
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

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
        private readonly int _pbkdf2Iterations = 100000;

        private static readonly Counter RegistrationCounter = Metrics
            .CreateCounter("auth_registrations_total", "Total number of successful registrations.");

        public AuthController(
            IUserRepository userRepository,
            IConfiguration configuration,
            ILogger<AuthController> logger)
        {
            _userRepository = userRepository;
            _configuration = configuration;
            _logger = logger;

            _jwtSettings = new JwtSettings
            {
                Key = Environment.GetEnvironmentVariable("JWT_KEY") ?? configuration["Jwt:Key"],
                Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? configuration["Jwt:Issuer"],
                Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? configuration["Jwt:Audience"],
                ExpirationMinutes = int.TryParse(configuration["Jwt:ExpirationMinutes"], out var exp) ? exp : 120
            };

            ValidateJwtSettings();
        }

        private void ValidateJwtSettings()
        {
            if (string.IsNullOrEmpty(_jwtSettings.Key) || _jwtSettings.Key.Length < 32)
                throw new ArgumentException("JWT Key must be at least 32 characters long");

            if (string.IsNullOrEmpty(_jwtSettings.Issuer))
                throw new ArgumentException("JWT Issuer is required");

            if (string.IsNullOrEmpty(_jwtSettings.Audience))
                throw new ArgumentException("JWT Audience is required");
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] Authentication newUser)
        {
            _logger.LogInformation("Register endpoint called for email: {Email}", newUser.Email);

            var existingUser = await _userRepository.GetUserByEmailAsync(newUser.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("Registration failed - Email already in use: {Email}", newUser.Email);
                return BadRequest(new { message = "Email already in use" });
            }

            // Valider le mot de passe
            if (!IsPasswordValid(newUser.Password))
            {
                _logger.LogWarning("Registration failed - Password does not meet requirements: {Email}", newUser.Email);
                return BadRequest(new { message = "Password does not meet security requirements" });
            }

            var encryptionKey = GenerateEncryptionKey();

            var user = new User
            {
                Email = newUser.Email,
                PasswordHash = HashPassword(newUser.Password),
                EncryptionKey = encryptionKey
            };

            try
            {
                await _userRepository.CreateUserAsync(user);
                _logger.LogInformation("User registered successfully: {Email}", newUser.Email);

                RegistrationCounter.Inc();

                return Ok(new { message = "User registered successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed for email: {Email}", newUser.Email);
                return Problem("An error occurred during registration.");
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] Authentication currentUser)
        {
            _logger.LogInformation("Login attempt for email: {Email}", currentUser.Email);

            var user = await _userRepository.GetUserByEmailAsync(currentUser.Email);
            if (user == null)
            {
                _logger.LogWarning("Invalid login attempt - User not found: {Email}", currentUser.Email);
                return Unauthorized(new { message = "Invalid email or password" });
            }

            if (!VerifyPassword(currentUser.Password, user.PasswordHash))
            {
                _logger.LogWarning("Invalid login attempt - Incorrect password: {Email}", currentUser.Email);
                return Unauthorized(new { message = "Invalid email or password" });
            }

            var token = GenerateJwtToken(user);
            _logger.LogInformation("Successful login for email: {Email}", currentUser.Email);

            return Ok(new
            {
                access_token = token,
                expires_in = _jwtSettings.ExpirationMinutes * 60
            });
        }

        [HttpGet("encryption-key")]
        public async Task<IActionResult> GetEncryptionKey()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            var user = await _userRepository.GetUserByEmailAsync(email);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Vérifier que la clé est valide
            if (string.IsNullOrEmpty(user.EncryptionKey) || user.EncryptionKey.Length < 30)
            {
                return BadRequest(new { message = "Invalid encryption key" });
            }

            try
            {
                // Convertir la clé base64 en bytes
                var keyBytes = Convert.FromBase64String(user.EncryptionKey);

                // Convertir en base64url sans padding
                var base64UrlKey = Base64UrlEncoder.Encode(keyBytes);

                // Créer le JWK
                var jwk = new
                {
                    kty = "oct",
                    alg = "A256GCM",
                    k = base64UrlKey,
                    ext = true,
                    key_ops = new[] { "encrypt", "decrypt" }
                };

                return Ok(new { key = jwk });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing encryption key");
                return BadRequest(new { message = "Invalid key format" });
            }
        }

        private string GenerateEncryptionKey()
        {
            using var aes = Aes.Create();
            aes.KeySize = 256; // Explicitement 256 bits
            aes.GenerateKey();
            return Convert.ToBase64String(aes.Key);
        }

        private bool IsPasswordValid(string password)
        {
            // Vérifier la longueur minimale
            if (string.IsNullOrEmpty(password) || password.Length < 8)
                return false;

            // Vérifier la présence d'au moins un chiffre
            if (!password.Any(char.IsDigit))
                return false;

            // Vérifier la présence d'au moins une lettre minuscule
            if (!password.Any(char.IsLower))
                return false;

            // Vérifier la présence d'au moins une lettre majuscule
            if (!password.Any(char.IsUpper))
                return false;

            // Vérifier la présence d'au moins un caractère spécial
            if (!password.Any(c => !char.IsLetterOrDigit(c)))
                return false;

            // Vérifier que le mot de passe n'est pas dans une liste de mots de passe courants
            var commonPasswords = new[] { "Password123!", "Azerty123!", "Qwerty123!" };
            if (commonPasswords.Contains(password))
                return false;

            return true;
        }

        private string HashPassword(string password)
        {
            // Générer un sel aléatoire
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            
            // Dériver une clé à partir du mot de passe avec PBKDF2
            byte[] hash = KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: _pbkdf2Iterations,
                numBytesRequested: 32);
            
            // Combiner le sel et le hash pour le stockage
            byte[] hashWithSalt = new byte[48]; // 16 bytes salt + 32 bytes hash
            Array.Copy(salt, 0, hashWithSalt, 0, 16);
            Array.Copy(hash, 0, hashWithSalt, 16, 32);
            
            return Convert.ToBase64String(hashWithSalt);
        }

        private bool VerifyPassword(string password, string storedHash)
        {
            // Décoder le hash stocké
            byte[] hashWithSalt = Convert.FromBase64String(storedHash);
            
            // Extraire le sel (premiers 16 octets)
            byte[] salt = new byte[16];
            Array.Copy(hashWithSalt, 0, salt, 0, 16);
            
            // Calculer le hash avec le même sel
            byte[] hash = KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: _pbkdf2Iterations,
                numBytesRequested: 32);
            
            // Comparer les hash
            for (int i = 0; i < 32; i++)
            {
                if (hash[i] != hashWithSalt[i + 16])
                    return false;
            }
            
            return true;
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
