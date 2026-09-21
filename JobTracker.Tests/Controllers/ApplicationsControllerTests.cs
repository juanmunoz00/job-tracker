using AutoMapper;
using FluentAssertions;
using JobTracker.Api.Controllers;
using JobTracker.Core.DTOs;
using JobTracker.Core.Interfaces;
using JobTracker.Core.Models;
using JobTracker.Api.Mappings;  // was JobTracker.Core.Mappings
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace JobTracker.Tests.Controllers;

public class ApplicationsControllerTests
{
    private readonly Mock<IApplicationRepository> _repoMock;
    private readonly IMapper _mapper;
    private readonly ApplicationsController _controller;

    public ApplicationsControllerTests()
    {
        _repoMock = new Mock<IApplicationRepository>();
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ApplicationProfile>());
        _mapper = config.CreateMapper();
        _controller = new ApplicationsController(_repoMock.Object, _mapper);
    }

    private static Application MakeApp(int id = 1) => new()
    {
        Id = id, Company = "Acme Corp", Country = "US",
        Position = "Senior Software Engineer",
        AppliedDate = new DateOnly(2026, 9, 21),
        Status = ApplicationStatus.Applied,
        StatusDate = DateTime.UtcNow
    };

    [Fact]
    public async Task GetAll_ReturnsOkWithList()
    {
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([MakeApp()]);
        var result = await _controller.GetAll();
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IEnumerable<ApplicationDto>>();
    }

    [Fact]
    public async Task GetById_ExistingId_ReturnsOk()
    {
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeApp());
        var result = await _controller.GetById(1);
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_MissingId_ReturnsNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Application?)null);
        var result = await _controller.GetById(999);
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Create_ValidDto_ReturnsCreatedAtAction()
    {
        var dto = new CreateApplicationDto { Company = "Acme", Country = "US",
            Position = "Engineer", AppliedDate = new DateOnly(2026, 9, 21) };
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Application>()))
                 .ReturnsAsync(MakeApp());
        var result = await _controller.Create(dto);
        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task PatchStatus_ValidStatus_ReturnsOk()
    {
        _repoMock.Setup(r => r.PatchStatusAsync(1, ApplicationStatus.Interview))
                 .ReturnsAsync(MakeApp());
        var result = await _controller.PatchStatus(1, new PatchStatusDto { Status = "Interview" });
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task PatchStatus_InvalidStatus_ReturnsBadRequest()
    {
        var result = await _controller.PatchStatus(1, new PatchStatusDto { Status = "NotReal" });
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Delete_ExistingId_ReturnsNoContent()
    {
        _repoMock.Setup(r => r.DeleteAsync(1)).Returns(Task.CompletedTask);
        var result = await _controller.Delete(1);
        result.Should().BeOfType<NoContentResult>();
    }
}