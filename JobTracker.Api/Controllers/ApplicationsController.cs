using AutoMapper;
using JobTracker.Core.DTOs;
using JobTracker.Core.Interfaces;
using JobTracker.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
namespace JobTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ApplicationsController : ControllerBase
{
    private readonly IApplicationRepository _repo;
    private readonly IMapper _mapper;

    public ApplicationsController(IApplicationRepository repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] ApplicationQueryParameters query)
    {
        ApplicationStatus? status = null;
        if (!string.IsNullOrWhiteSpace(query.Status) &&
            !string.Equals(query.Status, "All", StringComparison.OrdinalIgnoreCase))
        {
            if (!Enum.TryParse<ApplicationStatus>(query.Status, out var parsed) ||
                !Enum.IsDefined(parsed))
                return BadRequest($"Invalid status: {query.Status}");

            status = parsed;
        }

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);

        var result = await _repo.GetPagedAsync(query.Search, status, page, pageSize);

        return Ok(new PagedApplicationsDto
        {
            Items = _mapper.Map<IEnumerable<ApplicationDto>>(result.Items),
            Page = page,
            PageSize = pageSize,
            TotalCount = result.TotalCount,
            TotalPages = (int)Math.Ceiling(result.TotalCount / (double)pageSize),
            AllCount = result.StatusCounts.Values.Sum(),
            StatusCounts = result.StatusCounts.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value)
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var app = await _repo.GetByIdAsync(id);
        return app is null ? NotFound() : Ok(_mapper.Map<ApplicationDto>(app));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(CreateApplicationDto dto)
    {
        var app = _mapper.Map<Application>(dto);
        var created = await _repo.CreateAsync(app);
        return CreatedAtAction(nameof(GetById), new { id = created.Id },
            _mapper.Map<ApplicationDto>(created));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, ApplicationDto dto)
    {
        if (!Enum.TryParse<ApplicationStatus>(dto.Status, out _))
            return BadRequest($"Invalid status: {dto.Status}");

        var existing = await _repo.GetByIdAsync(id);
        if (existing is null)
            return NotFound();

        _mapper.Map(dto, existing);
        var updated = await _repo.UpdateAsync(existing);
        return Ok(_mapper.Map<ApplicationDto>(updated));
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PatchStatus(int id, PatchStatusDto dto)
    {
        if (!Enum.TryParse<ApplicationStatus>(dto.Status, out var status))
            return BadRequest($"Invalid status: {dto.Status}");

        var patched = await _repo.PatchStatusAsync(id, status);
        return Ok(_mapper.Map<ApplicationDto>(patched));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _repo.DeleteAsync(id);
        return NoContent();
    }
}