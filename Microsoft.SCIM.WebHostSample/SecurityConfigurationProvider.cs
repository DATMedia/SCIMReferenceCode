#nullable enable


namespace Microsoft.SCIM.WebHostSample
{
	using Microsoft.Extensions.Configuration;
	using Microsoft.IdentityModel.Tokens;
	using System;
	using System.Text;

	public class SecurityConfiguration
	{
		public required SecurityKey SigningKey { get; init; }
		public required string SecurityAlgorithm { get; init; }
		public required TimeSpan TokenLifetime { get; init; } = TimeSpan.FromMinutes(120);

		public required string TokenIssuer { get; init; }
		public required string TokenAudience { get; init; }
	}

	public class SecurityConfigurationProvider(IConfiguration configuration)
	{
		// https://stackoverflow.com/questions/49875167/jwt-error-idx10634-unable-to-create-the-signatureprovider-c-sharp
		// The key must have at least 32 characters
		private SecurityKey GenerateSecurityKey()
		{
			string? tokenString = configuration["Token:IssuerSigningKey"];
			if (string.IsNullOrEmpty(tokenString))
			{
				throw new Exception("The value Token:IssuerSigningKey must be set");
			}
			byte[] tokenBytes = Encoding.UTF8.GetBytes(tokenString);
			return new SymmetricSecurityKey(tokenBytes);
		}

		public SecurityConfiguration GetSecurityConfiguration()
		{
			TimeSpan tokenLifetime = double.TryParse(configuration["Token:TokenLifetimeInMins"], out double tokenExpiration)
				? TimeSpan.FromMinutes(tokenExpiration)
				: TimeSpan.FromMinutes(120);

			return new SecurityConfiguration
			{
				SigningKey = GenerateSecurityKey(),
				SecurityAlgorithm = SecurityAlgorithms.HmacSha256,
				TokenLifetime = tokenLifetime,
				TokenIssuer = configuration["Token:TokenIssuer"] ?? throw new Exception("The key Token:TokenIssuer has not been set"),
				TokenAudience = configuration["Token:TokenAudience"] ?? throw new Exception("The key Token:TokenAudience has not been set"),
			};
		}
	}
}
