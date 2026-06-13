using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.DTOs;
using WebApplication1.Extensions;
using WebApplication1.Models;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly AppDbContext _db;

    public ChatController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("messages")]
    public async Task<IActionResult> SendMessage(
    CreateMessageDto dto)
    {
        var userId = User.GetUserId();

        var message = new ChatMessage
        {
            UserId = userId,
            Content = dto.Content,
            CreatedAt = DateTime.UtcNow
        };

        _db.ChatMessages.Add(message);

        await _db.SaveChangesAsync();

        return Ok(new ApiResponse<ChatMessageDto>
        {
            Success = true,
            Message = "Message created",
            Data = new ChatMessageDto
            {
                Id = message.Id,
                Content = message.Content,
                CreatedAt = message.CreatedAt
            }
        });

    }

    [HttpGet("messages")]
    public async Task<IActionResult> GetMessages(
        int page = 1,
        int pageSize = 20)
    {
        var userId = User.GetUserId();

        if (page < 1)
            page = 1;

        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.ChatMessages
            .AsNoTracking()
            .Where(x => x.UserId == userId);

        var totalCount = await query.CountAsync();

        var messages = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ChatMessageDto
            {
                Id = x.Id,
                Content = x.Content,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        return Ok(new ApiResponse<PagedResult<ChatMessageDto>>
        {
            Success = true,
            Message = "Messages retrieved",
            Data = new PagedResult<ChatMessageDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                Items = messages
            }
        });

    }

    [HttpGet("messages/{id:int}")]
    public async Task<IActionResult> GetMessage(int id)
    {
        var userId = User.GetUserId();

        var message = await _db.ChatMessages
            .AsNoTracking()
            .Where(x =>
                x.Id == id &&
                x.UserId == userId)
            .Select(x => new ChatMessageDto
            {
                Id = x.Id,
                Content = x.Content,
                CreatedAt = x.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (message == null)
        {
            return NotFound(new ErrorResponse
            {
                Success = false,
                Message = "Message not found"
            });

        }

        return Ok(new ApiResponse<ChatMessageDto>
        {
            Success = true,
            Message = "Message retrieved",
            Data = message
        });

    }

    [HttpDelete("messages/{id:int}")]
    public async Task<IActionResult> DeleteMessage(int id)
    {
        var userId = User.GetUserId();

        var message = await _db.ChatMessages
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.UserId == userId);

        if (message == null)
        {
            return NotFound(new ErrorResponse
            {
                Success = false,
                Message = "Message not found"
            });

        }

        _db.ChatMessages.Remove(message);

        await _db.SaveChangesAsync();

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Message deleted",
            Data = null
        });

    }
}