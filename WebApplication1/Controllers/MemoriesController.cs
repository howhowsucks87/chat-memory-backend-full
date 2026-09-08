using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ChatMemoryApi.Data;
using ChatMemoryApi.DTOs;
using ChatMemoryApi.Extensions;
using ChatMemoryApi.Models;

namespace ChatMemoryApi.Controllers;

[ApiController]
[Route("api/memories")]
[Authorize]
public class MemoriesController : ControllerBase
{
    private readonly AppDbContext _db;

    public MemoriesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> CreateMemory(
    CreateMemoryDto dto)
    {
        var userId = User.GetUserId();

        var memory = new Memory
        {
            UserId = userId,
            Summary = dto.Summary,
            CreatedAt = DateTime.UtcNow
        };

        _db.Memories.Add(memory);

        await _db.SaveChangesAsync();

        return Ok(new ApiResponse<MemoryDto>
        {
            Success = true,
            Message = "Memory created",
            Data = new MemoryDto
            {
                Id = memory.Id,
                Summary = memory.Summary,
                CreatedAt = memory.CreatedAt
            }
        });

    }

    [HttpGet]
    public async Task<IActionResult> GetMemories(
    int page = 1,
    int pageSize = 20)
    {
        var userId = User.GetUserId();

        if (page < 1)
            page = 1;

        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Memories
            .AsNoTracking()
            .Where(x => x.UserId == userId);

        var totalCount = await query.CountAsync();

        var memories = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new MemoryDto
            {
                Id = x.Id,
                Summary = x.Summary,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        return Ok(new ApiResponse<PagedResult<MemoryDto>>
        {
            Success = true,
            Message = "memories retrieved",
            Data = new PagedResult<MemoryDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                Items = memories
            }
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetMemory(int id)
    {
        var userId = User.GetUserId();

        var memory = await _db.Memories
            .AsNoTracking()
            .Where(x =>
                x.Id == id &&
                x.UserId == userId)
            .Select(x => new MemoryDto
            {
                Id = x.Id,
                Summary = x.Summary,
                CreatedAt = x.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (memory == null)
            return NotFound(new ErrorResponse
            {
                Success = false,
                Message = "Memory not found"
            });

        return Ok(new ApiResponse<MemoryDto>
        {
            Success = true,
            Message = "Memory retrieved",
            Data = memory
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteMemory(int id)
    {
        var userId = User.GetUserId();

        var memory = await _db.Memories
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.UserId == userId);

        if (memory == null)
            return NotFound(new ErrorResponse
            {
                Success = false,
                Message = "Memory not found"
            });

        _db.Memories.Remove(memory);

        await _db.SaveChangesAsync();

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Memory deleted",
            Data = null
        });

    }
}