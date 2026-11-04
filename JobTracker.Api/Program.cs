using JobTracker.Api.Mappings;
using JobTracker.Api.Middleware;
using JobTracker.Core.Interfaces;
using JobTracker.Infrastructure.Data;
using JobTracker.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddAutoMapper(typeof(ApplicationProfile).Assembly);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IApplicationRepository, ApplicationRepository>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
    options.AddPolicy("AngularClient", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()));

var app = builder.Build();

// Middleware pipeline — order matters.
// Requests flow top to bottom; responses flow back bottom to top.
app.UseMiddleware<RequestLoggingMiddleware>();      // 1. outermost: times and logs every request
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseCors("AngularClient");                       // 2. adds CORS headers before going inward
app.UseMiddleware<ExceptionHandlingMiddleware>();   // 3. catches errors without clearing CORS headers
app.UseAuthorization();
app.MapControllers();                               // 4. innermost: your controllers

app.Run();