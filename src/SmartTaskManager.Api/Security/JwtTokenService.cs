using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SmartTaskManager.Api.Contracts.Responses;
using SmartTaskManager.Domain.Entities;

namespace SmartTaskManager.Api.Security;

public sealed class JwtTokenService
{
    private readonly JwtOptions _jwtOptions;
    private readonly SigningCredentials _signingCredentials;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public JwtTokenService(JwtOptions jwtOptions)
    {
        _jwtOptions = jwtOptions ?? throw new ArgumentNullException(nameof(jwtOptions));

        byte[] signingKeyBytes = Encoding.UTF8.GetBytes(_jwtOptions.SigningKey);
        SymmetricSecurityKey signingKey = new(signingKeyBytes);

        _signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    public AccessTokenResponse CreateToken(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        DateTime issuedAtUtc = DateTime.UtcNow;
        DateTime expiresAtUtc = issuedAtUtc.AddMinutes(_jwtOptions.TokenLifetimeMinutes);

        List<Claim> claims = new()
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName)
        };

        JwtSecurityToken securityToken = new(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: issuedAtUtc,
            expires: expiresAtUtc,
            signingCredentials: _signingCredentials);

        string accessToken = _tokenHandler.WriteToken(securityToken);

        return new AccessTokenResponse(
            accessToken,
            "Bearer",
            expiresAtUtc,
            user.Id,
            user.UserName);
    }
}
