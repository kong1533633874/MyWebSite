using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace JWT
{
	public class TokenService
	{
		public static string BuildToken(IEnumerable<Claim> claims, JWTOptions options)
		{
			TimeSpan ExpiryDuration = TimeSpan.FromSeconds(options.ExpireSeconds);
			var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key));
			// 不设 KeyId，让 JwtSecurityTokenHandler 不往 header 写 kid，
			// 避免 .NET 8 的 kid 严格匹配导致 IDX10517
			var tokenDescriptor = new JwtSecurityToken(options.Issuer, options.Audience, claims,
				expires: DateTime.UtcNow.Add(ExpiryDuration),
				signingCredentials: new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature));
			return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
		}
	}
}
