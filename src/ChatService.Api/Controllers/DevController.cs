using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ChatService.Api.Filters;
using ChatService.Application.Commands;
using ChatService.Application.Dto;
using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace ChatService.Api.Controllers;

[DevelopmentOnly]
[ApiController]
[Route("api/dev")]
public class DevController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;

    public DevController(IMediator mediator, IConfiguration configuration)
    {
        _mediator = mediator;
        _configuration = configuration;
    }

    [HttpPost("token")]
    public IActionResult GetToken([FromBody] TokenRequest request)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, request.UserId.ToString()),
            new Claim("sub", request.UserId.ToString()),
            new Claim("nameid", request.UserId.ToString()),
            new Claim(ClaimTypes.Role, request.Role),
            new Claim("role", request.Role),
            new Claim("tenant", request.TenantId)
        };

        var jwtSection = _configuration.GetSection("JwtOptions");
        var secretKey = jwtSection["SecretKey"]
            ?? throw new InvalidOperationException("JwtOptions:SecretKey not configured");
        var issuer = jwtSection["Issuer"] ?? "ChatService";
        var audience = jwtSection["Audience"] ?? "ChatService";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
    }

    [HttpPost("support-tickets")]
    public async Task<ActionResult<SupportTicketDto>> CreateSupportTicket(
        [FromBody] DevCreateSupportTicketRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Theme))
            return BadRequest(new { message = "Theme is required." });

        if (request.InitiatorType is not (ParticipantType.Customer or ParticipantType.Executor))
            return BadRequest(new { message = "InitiatorType must be Customer or Executor." });

        var ticket = await _mediator.Send(new CreateSupportTicketCommand(
            request.InitiatorId,
            request.InitiatorType,
            request.Theme.Trim(),
            request.AssignedManagerId));

        return Ok(ticket);
    }
}

public record TokenRequest(long UserId, string Role, string TenantId);

public record DevCreateSupportTicketRequest(
    ChatUserId InitiatorId,
    ParticipantType InitiatorType,
    string Theme,
    long? AssignedManagerId);