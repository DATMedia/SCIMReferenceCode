//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.SCIM.WebHostSample.Controllers
{
    using System;
    using System.IdentityModel.Tokens.Jwt;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.IdentityModel.Tokens;

    // Controller for generating a bearer token for authorization during testing.
    // This is not meant to replace proper Oauth for authentication purposes.
    [Route("scim/token")]
    [ApiController]
    public class TokenController(SecurityConfigurationProvider securityConfigurationProvider) : ControllerBase
    {
        private string GenerateJSONWebToken()
        {
            var securityConfiguration = securityConfigurationProvider.GetSecurityConfiguration();


            SigningCredentials credentials =
                new SigningCredentials(
					securityConfiguration.SigningKey,
					securityConfiguration.SecurityAlgorithm
            );

            DateTime startTime = DateTime.UtcNow;
            DateTime expiryTime = startTime.Add(securityConfiguration.TokenLifetime);

            JwtSecurityToken token = new (
		        securityConfiguration.TokenIssuer,
                securityConfiguration.TokenAudience,
                null,
                notBefore: startTime,
                expires: expiryTime,
                signingCredentials: credentials
             );

            string result = new JwtSecurityTokenHandler().WriteToken(token);
            return result;
        }

        [HttpGet]
        public ActionResult Get()
        {
            string tokenString = this.GenerateJSONWebToken();
            return this.Ok(new { token = tokenString });
        }

    }
}
