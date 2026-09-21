using JobTracker.Core.Interfaces;
using JobTracker.Infrastructure.Data;
using JobTracker.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using JobTracker.Api.Mappings;
// ...


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAutoMapper(typeof(ApplicationProfile).Assembly);
builder.Services.AddControllers();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IApplicationRepository, ApplicationRepository>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

builder.Services.AddAutoMapper(typeof(ApplicationProfile).Assembly);

app.Run();