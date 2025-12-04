using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WorkFlowBasic.Models;
using WorkFlowBasic.Models.ViewModels;

namespace WorkFlowBasic.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;

    public AuthController(UserManager<ApplicationUser> userManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }

    // ADRES: POST /api/Auth/Login (Mobil Uygulama Girişi)
    [HttpPost("Login")]
    public async Task<IActionResult> Login([FromBody] LoginViewModel model)
    {
        // 1. Kullanıcıyı ve şifreyi kontrol et
        var user = await _userManager.FindByEmailAsync(model.Email);

        if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
        {
            // Kullanıcı yoksa veya şifre hatalıysa
            return Unauthorized(new { Message = "Kullanıcı adı veya şifre hatalı." });
        }

        // 2. JWT Ayarlarını oku
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]!);

        // 3. Token Tanımlayıcıyı Oluştur
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] // Token'ın içine kullanıcı bilgilerini yazıyoruz
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName!),
                new Claim(ClaimTypes.GivenName, user.FirstName)
            }),
            Expires = DateTime.UtcNow.AddDays(int.Parse(jwtSettings["DurationInDays"]!)), // 7 Gün geçerli
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);

        // 4. Token'ı Mobil Uygulamaya gönder
        return Ok(new
        {
            Token = tokenHandler.WriteToken(token),
            UserId = user.Id,
            UserName = user.FullName,
            Expires = token.ValidTo
        });
    }
}