using AutoMapper;
using JobTracker.Core.DTOs;
using JobTracker.Core.Interfaces;
using JobTracker.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace JobTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
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
    public async Task<IActionResult> GetAll()
    {
        var apps = await _repo.GetAllAsync();
        return Ok(_mapper.Map<IEnumerable<ApplicationDto>>(apps));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var app = await _repo.GetByIdAsync(id);
        return app is null ? NotFound() : Ok(_mapper.Map<ApplicationDto>(app));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateApplicationDto dto)
    {
        var app = _mapper.Map<Application>(dto);
        var created = await _repo.CreateAsync(app);
        return CreatedAtAction(nameof(GetById), new { id = created.Id },
            _mapper.Map<ApplicationDto>(created));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, ApplicationDto dto)
    {
        var app = _mapper.Map<Application>(dto);
        app.Id = id;
        var updated = await _repo.UpdateAsync(app);
        return Ok(_mapper.Map<ApplicationDto>(updated));
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> PatchStatus(int id, PatchStatusDto dto)
    {
        if (!Enum.TryParse<ApplicationStatus>(dto.Status, out var status))
            return BadRequest($"Invalid status: {dto.Status}");

        var patched = await _repo.PatchStatusAsync(id, status);
        return Ok(_mapper.Map<ApplicationDto>(patched));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _repo.DeleteAsync(id);
        return NoContent();
    }
}