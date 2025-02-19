using Microsoft.IdentityModel.Tokens;
using NUnit.Framework;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SCIM.AutoTests
{
	[TestFixture]
	public class JwtValidation
	{

		static byte[] CreateBytes(int length)
		{
			var result = new byte[length];
			for(int i = 0; i < length; ++i)
			{
				result[i] = (byte)i;
			}
			return result;
		}


		[Test]
		public void CreateAndValidate()
		{
			var tokenBytes = CreateBytes(32);
			SymmetricSecurityKey securityKey =
				new SymmetricSecurityKey(
					tokenBytes
			);
			SigningCredentials credentials =
				new SigningCredentials(
					securityKey,
					SecurityAlgorithms.HmacSha256
			);
			DateTime now = DateTime.Now;
			DateTime expiry = now.AddMinutes(120);

			var token = new JwtSecurityToken(
				"https://identity.datmedia.com",
				"token_audience",
				null, // Claims
				notBefore: now,
				expires: expiry,
				signingCredentials: credentials
			);

			Assert.That(token, Is.Not.Null);

			var handler = new JwtSecurityTokenHandler();
			string asString = new JwtSecurityTokenHandler().WriteToken(token);
			Assert.That(string.IsNullOrEmpty(asString), Is.False);

			SecurityToken? validatedToken = null;

			ClaimsPrincipal claimsPrincipal = handler.ValidateToken(
				asString,
				new TokenValidationParameters
				{
					ValidIssuer = "https://identity.datmedia.com",
					ValidAudience = "token_audience",
					IssuerSigningKey = securityKey,
				},
				out validatedToken
			);

		}
	}
}
